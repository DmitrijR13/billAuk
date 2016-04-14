using System;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Collections.Generic;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;
using STCLINE.KP50.Utility;

namespace STCLINE.KP50.DataBase
{
    //----------------------------------------------------------------------
    public class DbSavePrm : IDisposable
    //----------------------------------------------------------------------
    {

        private enum SaveModes
        {
            None = 0x00,            // Не определен
            SingleEntity = 0x01,    // Редактирование характеристики, привязанной к какоу-либо сущности (параметры ЛС, дома, услуги и т.п.)
            SingleCommon = 0x02,    // редактирование системного параметра, на всю базу и др., не привязанных к конкретной сущности
            GroupKvar = 0x03,       // групповые операции с характистиками жилья выбранных ЛС
            GroupDom = 0x04,        // групповые операции с параметрами выбранных домов
            GroupKvarForDom = 0x05  // групповые операции с характеристиками жилья для лицевых счетов выбранных домов
        }

        /// <summary>
        /// Подключение к БД
        /// </summary>
        private readonly IDbConnection _connDB;

        private readonly IDbTransaction _transaction;

        private Param _finder;

        private SaveModes _mode;

        Returns _ret;

        private readonly bool _hasOwnConnection;


        public DbSavePrm()
        {
            _connDB = DBManager.GetConnection(Constants.cons_Kernel);
            _transaction = null;
            _ret = DBManager.OpenDb(_connDB, true);
            if (!_ret.result)
            {
                _connDB.Close();
                _connDB = null;
            }
            _hasOwnConnection = true;
        }


        public DbSavePrm(IDbConnection connDb)
        {
            _connDB = connDb;
            _transaction = null;
            _hasOwnConnection = false;
        }

        public DbSavePrm(IDbConnection connDb, IDbTransaction transaction)
        {
            _connDB = connDb;
            _transaction = transaction;
            _hasOwnConnection = false;
        }

        public void Dispose()
        {
            if (_hasOwnConnection && _connDB != null) _connDB.Close();
        }


        /// <summary> Сохранить или удалить значения параметров
        /// </summary>
        /// <returns></returns>
        public Returns Save(Param finder)
        {
            _finder = finder;
            try
            {
                CheckFinder();

                // Алгоритм
                // 1. Если это групповая операция
                //   1.1. Определить список префиксов БД
                //   1.2. Организовать цикл по префиксам
                //     1.2.1. Для каждого префикса выполнить сохранение
                // 2. Если это операция с одним домом или ЛС
                //   2.1. Выполнить сохранение

                if (IsGoupOperation()) // групповая операция по выбранным спискам ЛС или домов
                {
                    _ret = DoGroupOperation();
                }
                else // это операция с одним домом или ЛС
                {
                    DoSingleOperation();
                }

            }
            catch (UserException ex)
            {
                _ret.text = ex.Message;
                _ret.result = false;
                _ret.tag = -1;
            }
            catch (Exception ex)
            {
                _ret.text = ex.Message;
                _ret.result = false;
                _ret.tag = -2;
                MonitorLog.WriteLog(" Ошибка сохранения услуги DBSavePrm: " +
                                    Environment.NewLine + _ret.text,
                    MonitorLog.typelog.Error, 20, 201, true);
            }

            if (_ret.result)
            {
                using (var db = new DbLoadPoints())
                {
                    db.SetSetups(GlobalSettings.WorkOnlyWithCentralBank, finder.nzp_prm);
                }
            }
            return _ret;
        }


        /// <summary>
        /// Проверка является ли операция групповой
        /// </summary>
        /// <returns></returns>
        private bool IsGoupOperation()
        {
            return _mode == SaveModes.GroupKvar || _mode == SaveModes.GroupDom || _mode == SaveModes.GroupKvarForDom;
        }


        /// <summary>
        /// Сохранить параметр по 1 ЛС и 1 Дому
        /// </summary>
        private void DoSingleOperation()
        {
            var editData = new EditInterData();

            //укзываем таблицу для редактирования
            if (_finder.pref == "") _finder.pref = Points.Pref;
            editData.pref = _finder.pref;

            //DbSprav dbs = new DbSprav();
            //editData.nzp_wp = dbs.GetPoint(editData.pref).nzp_wp;
            //dbs.Close();
            editData.nzp_wp = Points.GetPoint(editData.pref).nzp_wp;

            editData.nzp_user = _finder.nzp_user;
            editData.webLogin = _finder.webLogin;
            editData.webUname = _finder.webUname;
            editData.primary = "nzp_key";
            editData.table = "prm_" + _finder.prm_num;

            //указываем вставляемый период
            if (_finder.dat_s == "") editData.dat_s = "01.01.1901";
            else editData.dat_s = _finder.dat_s;
            if (_finder.dat_po == "") editData.dat_po = "01.01.3000";
            else editData.dat_po = _finder.dat_po;

            var param = new ParamCommon();
            param.nzp_prm = _finder.nzp_prm;
            editData.intvType = _finder.intvtype;//param.intvtype;

            //условие выборки данных из целевой таблицы
            editData.dopFind = new List<string>();
            editData.dopFind.Add(" and nzp_prm = " + _finder.nzp_prm);
            editData.dopFind.Add(" and nzp = " + _finder.nzp);

            string mcFilter = " and p.nzp = " + _finder.nzp;

            //перечисляем ключевые поля и значения (со знаком сравнения!)
            var keys = new Dictionary<string, string>();
            switch (_finder.prm_num)
            {
                case 1:
                case 3:
                case 18:
                case 19:
                    {
                        keys.Add("nzp", "1|nzp_kvar|kvar|nzp_kvar = " + _finder.nzp); //ссылка на ключевую таблицу
                        break;
                    }
                case 2:
                case 4:
                    {
                        keys.Add("nzp", "1|nzp_dom|dom|nzp_dom = " + _finder.nzp);
                        break;
                    }
                case 6:
                    {
                        keys.Add("nzp", "1|nzp_ul|s_ulica|nzp_ul = " + _finder.nzp);
                        break;
                    }
                case 7:
                    {
                        keys.Add("nzp", "1|nzp_area|s_area|nzp_area = " + _finder.nzp);
                        break;
                    }
                case 8:
                    {
                        keys.Add("nzp", "1|nzp_geu|s_geu|nzp_geu = " + _finder.nzp);
                        break;
                    }
                case 9:
                    {
                        keys.Add("nzp", "1|nzp_payer|s_payer|nzp_payer = " + _finder.nzp);
                        break;
                    }
                case 11:
                    {
                        keys.Add("nzp", "1|nzp_supp|supplier|nzp_supp = " + _finder.nzp);
                        break;
                    }
                case 12:
                    {
                        keys.Add("nzp", "1|nzp_serv|services|nzp_serv = " + _finder.nzp);
                        break;
                    }
                case 17:
                    {
                        keys.Add("nzp", "1|nzp_counter|counters_spis|nzp_counter = " + _finder.nzp);
                        break;
                    }
                case 5:
                case 10:
                    {
                        keys.Add("nzp", "5|0");
                        mcFilter = "";
                        break;
                    }
            }
            keys.Add("nzp_prm", "2|" + _finder.nzp_prm);
            editData.keys = keys;

            //перечисляем поля и значения этих полей, которые вставляются
            var vals = new Dictionary<string, string>();
            if (isFloatPrm(_finder.nzp_prm))
            {
                vals.Add("val_prm", _finder.val_prm.Replace(",", "."));
            }
            else
            {
                vals.Add("val_prm", _finder.val_prm);
            }
            editData.vals = vals;
            editData.todelete = _finder.prms == Constants.act_del_val.ToString(CultureInfo.InvariantCulture);

            //вызов сервиса
            var db = new DbSaverNew(_connDB, editData, _transaction);
            Returns ret = db.Saver();

            if (ret.result)
            {
                if (AddRevalReason(editData, mcFilter))
                {
                    throw new Exception("Ошибка сохранения перерасчета " +
                                        Environment.NewLine + editData);
                }
            }
            _ret = ret;


            #region работа с событиями
            //--------------------------------------------------------------------------------
            if (_finder.nzp_user > 0)
            {
                _finder.nzp_user_main = _finder.nzp_user;

                /*var dbUser = new DbWorkUser();
                _finder.nzp_user_main = dbUser.GetLocalUser(_connDB, _transaction, _finder, out ret);
                dbUser.Close();
                if (!ret.result)
                {
                    return;
                }*/
            }

            if (_finder.prm_num == 3 && _finder.nzp_prm == 51)
            {
                #region Изменение в центральном банке данных статуса ЛС

                string sql = " update " + Points.Pref + DBManager.sDataAliasRest + "kvar " +
                             " set is_open=" + DBManager.sNvlWord + "(" +
                             " (select max(val_prm" + DBManager.sConvToInt + ")" +
                             " from " + _finder.pref + DBManager.sDataAliasRest + "prm_3 " +
                             " where nzp_prm=51 and is_actual=1 and dat_po>" + DBManager.sCurDate + " " +
                             " and nzp=" + _finder.nzp + "),2)" +
                             " where nzp_kvar =" + _finder.nzp;
                DBManager.ExecSQL(_connDB, sql, true);

                #endregion

                #region Добавление в sys_events события 'Закрытие лицевого счёта'

                if (_finder.val_prm == "2")
                {
                    DbAdmin.InsertSysEvent(new SysEvents
                    {
                        pref = editData.pref,
                        nzp_user = _finder.nzp_user_main,
                        nzp_dict = 6483,
                        nzp_obj = Convert.ToInt32(_finder.nzp),
                        note = "Лицевой счет был закрыт"
                    }, _connDB);
                }

                #endregion
            }

            if (_finder.prm_num == 17)
            {
                #region параметры приборов учета. Вставка в историю
                //--------------------------------------------------------------------------------
                ret = DBManager.ExecSQL(_connDB,
                    "insert into " + editData.pref + "_data" + DBManager.tableDelimiter + "counters_arx  " +
                    " (nzp_counter, pole, val_old, val_new, nzp_user, dat_calc, dat_when, nzp_prm) values " +
                    " (" + _finder.nzp + ", '', " + Utils.EStrNull(_finder.old_val_prm) + "," + Utils.EStrNull(_finder.val_prm) + "," + _finder.nzp_user_main + "," +
                    Utils.EStrNull(_finder.dat_s) + "," + DBManager.sCurDate + ", " + _finder.nzp_prm + ")",
                    true);

                if (!ret.result) throw new Exception("Ошибка сохранения параметра в историю изменений");

                DbAdmin.InsertSysEvent(new SysEvents()
                {
                    pref = editData.pref,
                    nzp_user = _finder.nzp_user,
                    nzp_dict = 6496,
                    nzp_obj = _finder.nzp,
                    note = "Параметры ПУ были изменены. Подробнее в истории изменений."
                }, _connDB);
                //--------------------------------------------------------------------------------
                #endregion
            }
            //--------------------------------------------------------------------------------
            #endregion

        }


        /// <summary>
        /// Проверка на число с плавающей точкой
        /// </summary>
        /// <param name="nzpPrm"></param>
        /// <returns></returns>
        private bool isFloatPrm(int nzpPrm)
        {
            return DBManager.ExecScalar<bool>(_connDB, null, " select count(nzp_prm)>0 " +
                                                       " from " + Points.Pref + DBManager.sKernelAliasRest + "prm_name " +
                                                       " where nzp_prm=" + nzpPrm +
                                                       " and type_prm='float'", true);         

        }


        /// <summary>
        /// Сохранение признаков перерасчета
        /// </summary>
        /// <param name="editData"></param>
        /// <param name="mcFilter"></param>
        /// <returns></returns>
        private bool AddRevalReason(EditInterData editData, string mcFilter)
        {
            if (Points.RecalcMode != RecalcModes.AutomaticWithCancelAbility) return false;

            var eid = new EditInterDataMustCalc();

            eid.mcalcType = enMustCalcType.None;

            if (ParamNums.lsParams.Contains(_finder.prm_num))
            {
                eid.mcalcType = enMustCalcType.mcalc_Prm1;
            }
            else if (ParamNums.domParams.Contains(_finder.prm_num))
            {
                eid.mcalcType = enMustCalcType.mcalc_Prm2;
            }
            else if (ParamNums.counterParams.Contains(_finder.prm_num))
            {
                //eid.mcalcType = enMustCalcType.Prm17;
            }

            if (eid.mcalcType != enMustCalcType.None)
            {
                eid.nzp_wp = editData.nzp_wp;
                eid.pref = editData.pref;
                eid.nzp_user = editData.local_user;
                eid.webLogin = editData.webLogin;
                eid.webUname = editData.webUname;
                eid.dat_s = "'" + new DateTime(Points.CalcMonth.year_, Points.CalcMonth.month_, 1).ToShortDateString() +
                            "'";
                eid.dat_po = "'" +
                             new DateTime(Points.CalcMonth.year_, Points.CalcMonth.month_, 1).AddMonths(1)
                                 .AddDays(-1)
                                 .ToShortDateString() + "'";
                eid.intvType = editData.intvType;
                eid.table = editData.table;
                eid.database = editData.database;
                eid.primary = editData.primary;
                eid.kod2 = _finder.nzp_prm;

                eid.keys = new Dictionary<string, string>();
                eid.vals = new Dictionary<string, string>();

                eid.dopFind = new List<string>();
                mcFilter += " and p.nzp_prm = " + _finder.nzp_prm;
                eid.dopFind.Add(mcFilter);
                eid.comment_action = _finder.comment_action;
                var dbMustCalcNew = new DbMustCalcNew(_connDB, _transaction);
                dbMustCalcNew.MustCalc(eid, out _ret);

                if (!_ret.result)
                {
                    throw new Exception("Ошибка сохранения признака перерасчета ");
                }
            }


            return false;
        }


        /// <summary>
        /// Сохранение параметров групповая операция
        /// </summary>
        private Returns DoGroupOperation()
        {
            //"_spls";
            string tXXSplsFull;
            string tXXSpdomFull;


            var prefics = new List<string>(); // список префиксов

            IDbConnection connWeb = DBManager.GetConnection(Constants.cons_Webdata);
            if (!DBManager.OpenDb(connWeb, true).result)
            {
                throw new Exception("Ошибка открытия Web базы");
            }

            try
            {
#if PG
                _ret = DBManager.ExecSQL(connWeb, "set search_path to 'public'", true);
                if (!_ret.result)
                {
                    throw new Exception("Ошибка смены текущей схемы");
                }
#endif

                string tXXSpls = "t" + Convert.ToString(_finder.nzp_user) + "_selectedls" + _finder.listNumber;
                //"_spls";
                tXXSplsFull = DBManager.GetFullBaseName(connWeb) + DBManager.tableDelimiter + tXXSpls;


                string tXXSpdom = "t" + Convert.ToString(_finder.nzp_user) + "_spdom";
                tXXSpdomFull = DBManager.GetFullBaseName(connWeb) + DBManager.tableDelimiter + tXXSpdom;



                if (_mode == SaveModes.GroupKvar && !DBManager.TableInWebCashe(connWeb, tXXSpls))
                {
                    {
                        throw new UserException("Не выбран список лицевых счетов");
                    }
                }

                if ((_mode == SaveModes.GroupDom || _mode == SaveModes.GroupKvarForDom) &&
                    !DBManager.TableInWebCashe(connWeb, tXXSpdom))
                {
                    {
                        throw new UserException("Не выбран список домов");
                    }
                }

                // Получить список префиксов из списка выбранных ЛС, домов
                string sql = "", wherePref = "";
                if (_finder.pref_sprav != "") wherePref = " and pref='" + _finder.pref_sprav + "'";

                if (_mode == SaveModes.GroupKvar)
                    sql = "select distinct pref from " + tXXSplsFull + " where mark = 1" + wherePref;
                else if (_mode == SaveModes.GroupDom || _mode == SaveModes.GroupKvarForDom)
                    sql = "select distinct pref from " + tXXSpdomFull + " where mark = 1" + wherePref;

                MyDataReader reader;
                _ret = DBManager.ExecRead(connWeb, out reader, sql, true);
                if (!_ret.result)
                {
                    throw new Exception("Ошибка выборки списка БД");
                }

                while (reader.Read())
                {
                    if (reader["pref"] != DBNull.Value)
                        prefics.Add(((string)reader["pref"]).Trim());
                }
                reader.Close();

            }
            finally
            {
                connWeb.Close();
            }



            foreach (string pref in prefics) // для каждого префикса вызвать функцию сохранения параметров
            {
                string mcFilter;
                var editData = PrepareEdDataForSingleOperation(pref, tXXSplsFull, tXXSpdomFull, out mcFilter);

                //вызов сервиса
                var dbi = new DbSaverNew(_connDB, editData);
                _ret = dbi.Saver();
                if (!_ret.result)
                {
                    throw new Exception("Ошибка сохранения параметров " +
                    Environment.NewLine + editData);
                }


                if (_finder.nzp_prm == 51)
                {
                    #region Изменение в центральном банке данных статуса ЛС

                    string sql = " update " + Points.Pref + DBManager.sDataAliasRest + "kvar " +
                                 " set is_open=" + DBManager.sNvlWord + "(" +
                                 " (select max(val_prm" + DBManager.sConvToInt + ")" +
                                 " from " + pref + DBManager.sDataAliasRest + "prm_3 p" +
                                 " where nzp_prm=51 and is_actual=1 and dat_po>" + DBManager.sCurDate + "" +
                                 " and p.nzp= " + Points.Pref + DBManager.sDataAliasRest + "kvar.nzp_kvar),2) ";


                    if (_mode == SaveModes.GroupKvar)
                        sql += " where nzp_kvar in (select nzp_kvar from " + tXXSplsFull +
                               " where pref =" + Utils.EStrNull(pref) + " and mark=1)";
                    //ссылка на ключевую таблицу
                    else if (_mode == SaveModes.GroupKvarForDom)
                        sql += " where nzp_kvar in (select nzp_kvar " +
                               " from " + Points.Pref + DBManager.sDataAliasRest + "kvar k,  " + tXXSpdomFull + " d" +
                               " where k.pref = " + Utils.EStrNull(pref) + " and k.nzp_dom=d.nzp_dom " +
                               " and mark = 1)"; //ссылка на ключевую таблицу
                    DBManager.ExecSQL(_connDB, sql, true);


                    #endregion
                }

                if (Points.RecalcMode != RecalcModes.AutomaticWithCancelAbility) continue;


                var eid = new EditInterDataMustCalc();

                if (ParamNums.lsParams.Contains(_finder.prm_num))
                {
                    eid.mcalcType = enMustCalcType.mcalc_Prm1;
                }
                else if (ParamNums.domParams.Contains(_finder.prm_num))
                {
                    eid.mcalcType = enMustCalcType.mcalc_Prm2;
                }
                else eid.mcalcType = enMustCalcType.None;

                if (eid.mcalcType == enMustCalcType.None) continue;

                eid.nzp_wp = editData.nzp_wp;
                eid.pref = editData.pref;
                eid.nzp_user = editData.local_user < 1 ? editData.nzp_user : editData.local_user;
                eid.webLogin = editData.webLogin;
                eid.webUname = editData.webUname;
                eid.dat_s = "'" + new DateTime(Points.CalcMonth.year_, Points.CalcMonth.month_, 1).ToShortDateString() +
                            "'";
                eid.dat_po = "'" +
                             new DateTime(Points.CalcMonth.year_, Points.CalcMonth.month_, 1).AddMonths(1)
                                 .AddDays(-1)
                                 .ToShortDateString() + "'";
                eid.intvType = editData.intvType;
                eid.table = editData.table;
                eid.database = editData.database;
                eid.primary = editData.primary;

                eid.keys = new Dictionary<string, string>();
                eid.vals = new Dictionary<string, string>();

                eid.dopFind = new List<string>();
                mcFilter += " and p.nzp_prm = " + _finder.nzp_prm;
                eid.dopFind.Add(mcFilter);
                eid.comment_action = _finder.comment_action;
                var dbMustCalcNew = new DbMustCalcNew(_connDB);
                Returns ret;
                dbMustCalcNew.MustCalc(eid, out ret);
                _ret.text += "\r\n" + ret.text;

                if (!ret.result)
                {
                    throw new Exception("Ошибка сохранения перерасчета");
                }
            }
            return _ret;
        }

        private EditInterData PrepareEdDataForSingleOperation(string pref, string tXXSplsFull, string tXXSpdomFull,
            out string mcFilter)
        {
            var editData = new EditInterData();

            editData.pref = pref; // префикс БД
            editData.nzp_wp = Points.GetPoint(editData.pref).nzp_wp;
            editData.nzp_user = _finder.nzp_user; // Пользователь, от имени которого ведется сохранение
            editData.webLogin = _finder.webLogin;
            editData.webUname = _finder.webUname;
            editData.primary = "nzp_key"; // Первичный ключ целевой таблицы
            editData.table = "prm_" + _finder.prm_num; // Указываем таблицу для редактирования

            //указываем вставляемый период
            if (_finder.dat_s == "") editData.dat_s = "01.01.1901";
            else editData.dat_s = _finder.dat_s;
            if (_finder.dat_po == "") editData.dat_po = "01.01.3000";
            else editData.dat_po = _finder.dat_po;

            var param = new Param();
            param.nzp_prm = _finder.nzp_prm;
            //editData.intvType = param.intvtype;
            editData.intvType = _finder.intvtype;

            string kvar = pref + DBManager.sDataAliasRest + "kvar";
            string kvarFilter = " 1=0 ";
            string domFilter = " 1=0 ";
            mcFilter = " 1=0 ";

            //условие выборки данных из целевой таблицы
            editData.dopFind = new List<string>();
            string sql = " and nzp_prm = " + _finder.nzp_prm;
            if (_mode == SaveModes.GroupKvar)
            {
                kvarFilter = " in (select nzp_kvar from " + tXXSplsFull + " where pref = " + Utils.EStrNull(pref) +
                             " and  mark = 1) ";
                sql += " and nzp" + kvarFilter;
                mcFilter = " and p.nzp" + kvarFilter;
            }
            else if (_mode == SaveModes.GroupDom)
            {
                mcFilter = " and p.nzp in (select nzp_dom from " + tXXSpdomFull + " where pref = " + Utils.EStrNull(pref) +
                           " and  mark = 1) ";
                sql += " and nzp in (select nzp_dom from " + tXXSpdomFull + " where pref = " + Utils.EStrNull(pref) +
                       " and  mark = 1) ";
            }
            else if (_mode == SaveModes.GroupKvarForDom)
            {
                kvarFilter = " in (select b.nzp_kvar from " + tXXSpdomFull + " a, " + kvar + " b where a.pref = " +
                             Utils.EStrNull(pref) + " and a.mark = 1 and a.nzp_dom = b.nzp_dom) ";
                domFilter = "nzp_dom in (select nzp_dom from " + tXXSpdomFull + " where pref = " + Utils.EStrNull(pref) +
                            " and mark = 1) ";
                sql += " and nzp " + kvarFilter;
                mcFilter = " and p.nzp " + kvarFilter;
            }
            editData.dopFind.Add(sql);

            //перечисляем ключевые поля и значения (со знаком сравнения!)
            var keys = new Dictionary<string, string>();
            switch (_finder.prm_num)
            {
                case 1:
                case 3:
                case 18:
                case 19:
                    {
                        if (_mode == SaveModes.GroupKvar)
                            keys.Add("nzp", "1|nzp_kvar|" + tXXSplsFull + "|nzp_kvar" + kvarFilter);
                        //ссылка на ключевую таблицу
                        else if (_mode == SaveModes.GroupKvarForDom)
                            keys.Add("nzp", "1|nzp_kvar|kvar|" + domFilter); //ссылка на ключевую таблицу
                        break;
                    }
                case 2:
                    {
                        keys.Add("nzp",
                            "1|nzp_dom|" + tXXSpdomFull + "|nzp_dom in (select nzp_dom from " + tXXSpdomFull +
                            " where pref = " + Utils.EStrNull(pref) + " and  mark = 1) ");
                        break;
                    }
            }
            keys.Add("nzp_prm", "2|" + _finder.nzp_prm);
            editData.keys = keys;

            //перечисляем поля и значения этих полей, которые вставляются
            var vals = new Dictionary<string, string>();
            if (isFloatPrm(_finder.nzp_prm))
            {
                vals.Add("val_prm", _finder.val_prm.Replace(",", "."));
            }
            else
            {
                vals.Add("val_prm", _finder.val_prm);
            }
            editData.vals = vals;
            editData.todelete = Utils.GetParams(_finder.prms, Constants.act_del_val);
            return editData;
        }


        /// <summary>
        /// Проверка входных параметров
        /// </summary>
        private void CheckFinder()
        {
            #region проверка входных параметров

            if (_finder.nzp_user < 1)
            {
                throw new Exception("Не определен пользователь");
            }

            #region проверка значений

            var prms = _finder.prms.Split(','); 

            bool toDelete = prms.Contains(Constants.act_del_val.ToString(CultureInfo.InvariantCulture));
            if (!toDelete)
            {
                string[] types = {"sprav", "bool", "date", "norm", "float", "int"};
                if (String.IsNullOrEmpty(_finder.val_prm) && (types.ToList().Exists(x => x == _finder.type_prm)))
                {
                    throw new Exception("Для данного типа параметра не может быть выставлено пуcтое значение");
                }

                List<string> boolVals = new List<string>() {"0", "1"};
                if (_finder.type_prm == "bool" && !boolVals.Exists(x => x == _finder.val_prm.Trim()))
                {
                    throw new Exception("Недопустимое значение " + _finder.val_prm.Trim() + " для логического типа");
                }

                int intVal;
                if (_finder.type_prm == "int" && !Int32.TryParse(_finder.val_prm.Trim(), out intVal))
                {
                    throw new Exception("Недопустимое значение " + _finder.val_prm.Trim() + " для целочисленного типа");
                }

                float floatVal;
                if (_finder.type_prm == "float" && !float.TryParse(_finder.val_prm.Trim(), out floatVal))
                {
                    throw new Exception("Недопустимое значение " + _finder.val_prm.Trim() + " для численного типа");
                }

                DateTime dateVal;
                if (_finder.type_prm == "date" && !DateTime.TryParse(_finder.val_prm, out dateVal))
                {
                    throw new Exception("Недопустимое значение " + _finder.val_prm.Trim() + " для значения типа дата");
                }
            }

            #endregion

            Returns ret = SetDatePeriodForNormParam(_connDB, _finder);
            if (!ret.result)
            {
                throw new Exception(ret.text);
            }

            _mode = SaveModes.None;
            if (ParamNums.lsParams.Contains(_finder.prm_num))
            {
                if (Utils.GetParams(_finder.prms, Constants.page_group_ls_prm_dom)) _mode = SaveModes.GroupKvarForDom;
                else if (_finder.nzp > 0) _mode = SaveModes.SingleEntity;
                else _mode = SaveModes.GroupKvar;
            }
            else if (_finder.prm_num == 2)
            {
                if (_finder.nzp > 0) _mode = SaveModes.SingleEntity;
                else _mode = SaveModes.GroupDom;
            }
            else if (ParamNums.generalParams.Contains(_finder.prm_num))
            {
                _mode = SaveModes.SingleCommon;
            }
            else if (_finder.prm_num == 4 || _finder.prm_num == 6 || _finder.prm_num == 7 || _finder.prm_num == 8 || _finder.prm_num == 9 ||
                     _finder.prm_num == 11 || _finder.prm_num == 12 || _finder.prm_num == 17)
            {
                _mode = SaveModes.SingleEntity;
            }

            if (_mode == SaveModes.None)
            {
                throw new Exception("Неверные входные параметры");
            }

            if (!(_finder.prm_num == 7 || _finder.prm_num == 8 || _finder.prm_num == 9 || _finder.prm_num == 12))
                if (_finder.nzp > 0 && _finder.pref.Trim() == "")
                {
                    throw new Exception("Не задан префикс базы данных");
                }

            if (_mode == SaveModes.GroupKvar || _mode == SaveModes.GroupDom || _mode == SaveModes.GroupKvarForDom)
                if (_finder.listNumber < 0)
                {
                    {
                        throw new UserException("Список лицевых счетов не сформирован");
                    }
                }

            #endregion

        }


        private Returns SetDatePeriodForNormParam(IDbConnection conn_db, Param finder)
        {
            Returns ret = Utils.InitReturns();
            int valPrm;
            if (finder.type_prm == "norm" && Int32.TryParse(finder.val_prm, out valPrm) && valPrm > 0)
            {
                try
                {
                    string sql =
                        " SELECT date_from, date_to " +
                        " FROM " + Points.Pref + DBManager.sKernelAliasRest + "norm_types " +
                        " WHERE id = " + finder.norm_type_id;
                    DataTable dt = ClassDBUtils.OpenSQL(sql, conn_db).resultData;
                    if (dt.Rows.Count != 1)
                    {
                        MonitorLog.WriteLog(
                            "Ошибка проверки периода действия норматива для сохранения характеристик жилья, количество периодов: " +
                            dt.Rows.Count, MonitorLog.typelog.Error, true);
                        return new Returns(false, "Ошибка получения периода действия параметра по периоду норматива");
                    }
                    DateTime normDateFrom;
                    DateTime normDateTo;
                    DateTime finderDateFrom;
                    DateTime finderDateTo;
                    if (DateTime.TryParse(dt.Rows[0]["date_from"].ToString().Substring(0, 10), out normDateFrom) &&
                        DateTime.TryParse(dt.Rows[0]["date_to"].ToString().Substring(0, 10), out normDateTo) &&
                        DateTime.TryParse(finder.dat_s, out finderDateFrom) &&
                        DateTime.TryParse(finder.dat_po == "" ? "01.01.3000" : finder.dat_po, out finderDateTo))
                    {
                        //if (!(normDateFrom <= finderDateFrom && normDateTo >= finderDateTo)) //задача #147339
                        if (!(normDateFrom <= finderDateFrom))
                        {
                            return new Returns(false, "Период действия параметра должен входить в период действия норматива");
                        }
                    }
                    else
                    {
                        return new Returns(false, "Ошибка проверки периода действия параметра по периоду норматива");
                    }
                    finder.is_day_uchet = 1;

                }
                catch (Exception ex)
                {
                    MonitorLog.WriteLog(
                        "Ошибка проверки периода дtйствия норматива для сохранения характеристик жилья." +
                        ex.Message + " " + ex.StackTrace, MonitorLog.typelog.Error, true);
                    return new Returns(false, "Ошибка проверки периода действия параметра по периоду норматива");
                }
            }
            return ret;
        }
    } //end class

} //end namespace