using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;

namespace STCLINE.KP50.DataBase
{


    /// <summary>
    /// Класс сохранения недопоставок
    /// </summary>
    public class DbNedopSave : IDisposable
    {
        private readonly IDbConnection _connection;
        private Nedop _finder;
        private Nedop _additionalFinder;
        private Returns _ret = Utils.InitReturns();
        private readonly bool _hasOwnConnection;
        private string _domFilter;
        private string _kvarFilter;
        private string _pref;
        private DateTime _ds;
        private DateTime _dpo;

        public DbNedopSave(IDbConnection connection)
        {
            _connection = connection;
            _hasOwnConnection = false;
        }

        public DbNedopSave()
        {
            _connection = DBManager.GetConnection(Constants.cons_Kernel);
            if (!DBManager.OpenDb(_connection, true).result)
            {
                _connection = null;
            }

            _hasOwnConnection = true;
        }


        public virtual void Dispose()
        {
            if (_hasOwnConnection && _connection != null)
            {
                _connection.Close();
            }
        }

        public List<Nedop> GetNedopDependencies()
        {
            var lstNedop = new List<Nedop> { _finder };


            string datS = String.IsNullOrEmpty(_finder.dat_s.Trim())
                ? "'01.01.1990'"
                : "'" + Convert.ToDateTime(_finder.dat_s).ToString("dd.MM.yyyy") + "'";

            string datPo = String.IsNullOrEmpty(_finder.dat_po.Trim())
                ? "'01.01.3000'"
                : "'" + Convert.ToDateTime(_finder.dat_po).ToString("dd.MM.yyyy") + "'";
            var s_where = "";
            var sql = "";
            var list_area = new List<int>() { };
            //при добавлении недопоставок для одного лс
            if (_finder.nzp_kvar > 0)
            {
                sql = "SELECT nzp_area FROM " + _finder.pref + DBManager.sDataAliasRest + "kvar WHERE nzp_kvar=" + _finder.nzp_kvar;
            }
            else
                //при добавлении недопоставок для одного дома(в одном доме может быть несколько УК! :O)
                if (_finder.nzp_dom > 0)
                {
                    sql = "SELECT DISTINCT nzp_area FROM " + _finder.pref + DBManager.sDataAliasRest + "kvar WHERE nzp_dom=" + _finder.nzp_dom;
                }
                else
                //по списку лс или домов  (через групповые операции)
                {
                    var tXX_sp = TestSelectedTable();
                    sql = " SELECT nzp_area FROM " + tXX_sp + " WHERE mark=1 AND pref=" + Utils.EStrNull(_finder.pref) + " GROUP BY nzp_area";
                }

            var DT = ClassDBUtils.OpenSQL(sql, _connection).resultData;
            for (int i = 0; i < DT.Rows.Count; i++)
            {
                list_area.Add(DBManager.CastValue<int>(DT.Rows[i]["nzp_area"]));
            }
            //определяем зависимости на уровне УК, если nzp_area=0 в dep_servs, то зависимость действует на все УК
            foreach (var nzp_area in list_area)
            {
                s_where = " and nzp_area in (0," + nzp_area + ")";
                string strSqlQuiery = String.Format("SELECT nzp_serv_slave FROM {0}_data{4}dep_servs " +
                                                "WHERE nzp_serv = {1} AND is_actual = 1 " +
                                                "AND {2} >= dat_s AND {3} < dat_po and nzp_dep = 2 {5}",
                Points.Pref,
                _finder.nzp_serv, datS, datPo, DBManager.tableDelimiter, s_where);

                MyDataReader reader;
                _ret = DBManager.ExecRead(_connection, out reader, strSqlQuiery, true);

                if (!_ret.result) return lstNedop;

                try
                {
                    while (reader.Read())
                    {

                        if (reader["nzp_serv_slave"] == DBNull.Value) continue;

                        int nzpServSlave;
                        if (!Int32.TryParse(reader["nzp_serv_slave"].ToString(), out nzpServSlave)) continue;

                        var item = new Nedop();
                        _finder.CopyTo(item);
                        item.nzp_kvar = _finder.nzp_kvar;
                        item.prms = _finder.prms;
                        item.nzp_serv = nzpServSlave;
                        item.nzp_area = nzp_area;
                        item.nzp_dom = _finder.nzp_dom;
                        lstNedop.Add(item);
                    }
                }
                finally
                {
                    reader.Close();
                }

            }


            return lstNedop;
        }

        /// <summary>
        /// Получение зависимых услуг
        /// </summary>
        /// <returns></returns>
        public List<Nedop> GetNedopDependencies1()
        {
            var lstNedop = new List<Nedop>();
            try
            {
                lstNedop = GetNedopDependencies();
            }
            catch (Exception ex)
            {
                _ret.text = ex.Message + " класс DbNedopSave";
                MonitorLog.WriteLog(" Ошибка GetNedopDependencies1 " + _ret.text,
                    MonitorLog.typelog.Error, 20, 201, true);
            }
            return lstNedop;
        }

        public Returns SaveNedop(Nedop finder, Nedop additionalFinder)
        {
            _finder = finder;
            _additionalFinder = additionalFinder;
            List<Nedop> lstNedop = GetNedopDependencies1();

            foreach (Nedop item in lstNedop)
                SaveNedop(item);

            return _ret;
        }

        /// <summary>
        /// групповая операция сохранение недопоставок
        /// </summary>
        /// <param name="item">Недопоставка</param>
        public Returns SaveNedop(Nedop item)
        {
            if (_hasOwnConnection) DBManager.OpenDb(_connection, true);

            try
            {
                CheckFinder();

                PrepareFilter(item);

                PrepareSelectedTable();

                var editData = PrepareEditData(item);

                SaveData(editData);

                AddSysEvents(editData);

                AddRevalReason(editData);

            }
            catch (Exception ex)
            {

                _ret.text = ex.Message + " класс DbNedopSave";
                MonitorLog.WriteLog(" Ошибка сохранения интервальных данных " + _ret.text,
                    MonitorLog.typelog.Error, 20, 201, true);
            }
            finally
            {
                DBManager.ExecSQL(_connection, "drop table t_nk_selected", false);
                if (_hasOwnConnection) _connection.Close();
            }

            return _ret;
        }

        private void SaveData(EditInterData editData)
        {
            var db = new DbSaverNew(_connection, editData);
            db.Saver();
        }


        /// <summary>
        /// Подготовка таблицы с выбранными ЛС
        /// </summary>
        private void PrepareSelectedTable()
        {
            DBManager.ExecSQL(_connection, "drop table t_nk_selected", false);

            string sql = " Create temp table t_nk_selected (" +
                         " nzp_kvar integer)" + DBManager.sUnlogTempTable;
            DBManager.ExecSQL(_connection, sql, true);

            sql = " insert into t_nk_selected(nzp_kvar) " +
                  " select nzp_kvar " +
                  " from " + _finder.pref + DBManager.sDataAliasRest + "nedop_kvar " +
                  " where " + _kvarFilter;

            _ret = DBManager.ExecSQL(_connection, sql, true);
            if (!_ret.result)
            {
                throw new Exception("Ошибка подготовки таблицы для выборки");
            }
            DBManager.ExecSQL(_connection, "create index ix_t_nk_selected_1 on " +
                                          " t_nk_selected (nzp_kvar) ", false);

            DBManager.ExecSQL(_connection, DBManager.sUpdStat +
                                          "  t_nk_selected", false);
        }


        /// <summary>
        /// Проверка входных параметров
        /// </summary>
        private void CheckFinder()
        {
            if (_finder.nzp_user < 1)
            {
                throw new Exception("Не определен пользователь");
            }
            if (_finder.pref.Trim() == "")
            {
                throw new Exception("Не задан префикс базы данных");
            }
            if (_finder.nzp_serv < 1)
            {
                throw new Exception("Не задана услуга");
            }
            if (!Utils.GetParams(_finder.prms, Constants.act_add_nedop) &&
                !Utils.GetParams(_finder.prms, Constants.act_del_nedop))
            {
                throw new Exception("Не задана операция");
            }
            if (Utils.GetParams(_finder.prms, Constants.act_add_nedop) && _finder.nzp_kind <= 0)
            {
                throw new Exception("Не задан тип недопоставки");
            }
            if (!Utils.GetParams(_finder.prms, Constants.page_groupnedop) &&
                !Utils.GetParams(_finder.prms, Constants.page_group_nedop_dom) &&
                !Utils.GetParams(_finder.prms, Constants.page_spisnddom) &&
                _finder.nzp_kvar < 1)
            {
                throw new Exception("Не задан режим работы");
            }

            _ds = DateTime.MinValue;
            _dpo = DateTime.MaxValue;
            if (Utils.GetParams(_finder.prms, Constants.page_spisnddom))
            {
                if (_additionalFinder == null)
                {
                    throw new Exception("Неверные параметры поиска");
                }
                if (_additionalFinder.nzp_dom < 1 ||
                    _additionalFinder.nzp_serv < 1 ||
                    //_additionalFinder.nzp_supp < 1 ||
                    _additionalFinder.dat_s == "" ||
                    _additionalFinder.dat_po == "" ||
                    _additionalFinder.nzp_kind < 1)
                {
                    throw new Exception("Не заданы условия поиска");

                }
                if (!DateTime.TryParse(_additionalFinder.dat_s, out _ds))
                {
                    throw new Exception("Неверная дата начала периода");

                }
                if (!DateTime.TryParse(_additionalFinder.dat_po, out _dpo))
                {
                    throw new Exception("Неверная дата окончания периода");

                }
            }


            _ret = Utils.InitReturns();
            _pref = _finder.pref.Trim();

        }


        /// <summary>
        /// Подготовка фильтра для выборки
        /// </summary>+
        private void PrepareFilter(Nedop item)
        {
            string tXXSpFull = TestSelectedTable();

            string kvar = _pref + DBManager.sDataAliasRest + "kvar";
            _kvarFilter = " 1=0 ";
            _domFilter = " 1=0 ";
            var s_area = "1=1";
            if (item.nzp_area > 0)
                s_area = "nzp_area=" + item.nzp_area;

            if (item.nzp_kvar > 0)
            {
                _kvarFilter = "nzp_kvar = " + item.nzp_kvar;
            }
            else if (Utils.GetParams(item.prms, Constants.page_groupnedop))
            {
                _kvarFilter = "nzp_kvar in (select nzp_kvar from " + tXXSpFull + " where pref = " + Utils.EStrNull(_pref) +
                              " and  mark = 1 and " + s_area + ") ";
            }
            else if (Utils.GetParams(item.prms, Constants.page_group_nedop_dom))
            {
                if (item.nzp_dom > 0)
                {
                    _kvarFilter = "nzp_kvar in (select b.nzp_kvar from " + kvar + " b where b.nzp_dom = " + item.nzp_dom +
                                  " and " + s_area + ") ";
                    _domFilter = "nzp_dom = " + item.nzp_dom + " and " + s_area + "";
                }
                else
                {
                    _kvarFilter = "nzp_kvar in (select b.nzp_kvar from " + tXXSpFull + " a, " + kvar + " b where a.pref = " +
                        Utils.EStrNull(_pref) + " and a.mark = 1 and a.nzp_dom = b.nzp_dom  " + (item.nzp_area > 0 ? "and a." + s_area : "") + ") ";
                    _domFilter = "nzp_dom in (select nzp_dom from " + tXXSpFull + " where pref = " + Utils.EStrNull(_pref) +
                                 " and mark = 1 and " + s_area + ") ";
                }
            }
            else if (Utils.GetParams(item.prms, Constants.page_spisnddom))
            {
                _kvarFilter = "nzp_kvar in (select nk.nzp_kvar " +
                              " from " + _pref + DBManager.sDataAliasRest + "nedop_kvar nk, " +
                              _pref + DBManager.sDataAliasRest + "kvar kv " +
                              " where kv.nzp_kvar = nk.nzp_kvar  " + (item.nzp_area > 0 ? "and kv." + s_area : "") + "" +
                              " and kv.nzp_dom = " + _additionalFinder.nzp_dom +
                              " and nk.nzp_serv = " + _additionalFinder.nzp_serv;

                if (_additionalFinder.nzp_supp > 0) _kvarFilter += " and nk.nzp_supp = " + _additionalFinder.nzp_supp;
                else _kvarFilter += " and (nk.nzp_supp is null or nk.nzp_supp = 0)";
                _kvarFilter += " and nk.nzp_kind = " + _additionalFinder.nzp_kind +
                               " and nk.dat_s = " + Utils.EStrNull(_ds.ToString("yyyy-MM-dd HH:00")) +
                               " and nk.dat_po = " + Utils.EStrNull(_dpo.ToString("yyyy-MM-dd HH:00"));
                if (_additionalFinder.tn.Trim() != "")
                    _kvarFilter += " and nk.tn = " + Utils.EStrNull(_additionalFinder.tn.Trim());
                else _kvarFilter += " and (nk.tn is null or nk.tn = '')";
                _kvarFilter += ") ";
            }
        }


        /// <summary>
        /// Проверка есть ли выбранные данные в кэше
        /// </summary>
        /// <returns></returns>
        private string TestSelectedTable()
        {
            string result;
            string tXXSp = "";
            if (Utils.GetParams(_finder.prms, Constants.page_groupnedop))
                tXXSp = "t" + Convert.ToString(_finder.nzp_user) + "_spls";
            else
                if (Utils.GetParams(_finder.prms, Constants.page_group_nedop_dom) && _finder.nzp_dom < 1)
                    tXXSp = "t" + Convert.ToString(_finder.nzp_user) + "_spdom";

            IDbConnection connWeb = DBManager.GetConnection(Constants.cons_Webdata);
            _ret = DBManager.OpenDb(connWeb, true);
            if (!_ret.result)
            {
                throw new Exception("Ошибка открытия БД");
            }

            try
            {
                if (tXXSp != "")
                    if (!DBManager.TableInWebCashe(connWeb, tXXSp))
                    {
                        throw new Exception("Данные не выбраны");
                    }
                result = DBManager.GetFullBaseName(connWeb, connWeb.Database, tXXSp);
            }
            finally
            {
                connWeb.Close();
            }

            return result;
        }


        private EditInterData PrepareEditData(Nedop nedop)
        {
            string sql;
            EditInterData editData = new EditInterData();

            //указываем таблицу для редактирования
            editData.pref = _pref;
            editData.nzp_wp = _finder.nzp_wp;
            editData.nzp_user = _finder.nzp_user;
            editData.webLogin = _finder.webLogin;
            editData.webUname = _finder.webUname;
            editData.primary = "nzp_nedop";
            editData.table = "nedop_kvar";
            editData.todelete = Utils.GetParams(_finder.prms, Constants.act_del_nedop);

            //указываем вставляемый период
            editData.dat_s = _finder.dat_s;
            editData.dat_po = _finder.dat_po;
            editData.intvType = enIntvType.intv_Hour;

            if (_finder.nzp_kvar > 0)
            {
                editData.dopFind = new List<string>();
                sql = " and nzp_kvar = " + _finder.nzp_kvar + " and nzp_serv = " + nedop.nzp_serv;
                if (editData.todelete && _finder.nzp_kind > 0)
                    sql += " and nzp_kind = " + _finder.nzp_kind;
                editData.dopFind.Add(sql);

                //перечисляем ключевые поля и значения (со знаком сравнения!)
                Dictionary<string, string> keys = new Dictionary<string, string>();
                keys.Add("nzp_kvar", "1|nzp_kvar|kvar|nzp_kvar=" + _finder.nzp_kvar); //ссылка на ключевую таблицу
                keys.Add("nzp_serv", "2|" + nedop.nzp_serv);
                if (editData.todelete && _finder.nzp_kind > 0)
                    keys.Add("nzp_kind", "2|" + _finder.nzp_kind);

                editData.keys = keys;

                //перечисляем поля и значения этих полей, которые вставляются
                var vals = new Dictionary<string, string>();
                vals.Add("tn", _finder.tn);
                vals.Add("comment", _finder.comment);
                if (!editData.todelete || _finder.nzp_kind <= 0)
                    vals.Add("nzp_kind", _finder.nzp_kind.ToString(CultureInfo.InvariantCulture));
                vals.Add("nzp_supp", _finder.nzp_supp.ToString(CultureInfo.InvariantCulture));
                editData.vals = vals;
            }
            else if (Utils.GetParams(_finder.prms, Constants.page_spisnddom))
            {
                //условие выборки данных из целевой таблицы           
                sql = " and nzp_serv = " + _additionalFinder.nzp_serv;
                if (_additionalFinder.nzp_supp > 0) sql += " and nzp_supp = " + _additionalFinder.nzp_supp;
                else sql += " and (nzp_supp is null or nzp_supp = 0)";
                sql += " and nzp_kind = " + _additionalFinder.nzp_kind +
                       " and dat_s = " + Utils.EStrNull(_ds.ToString("yyyy-MM-dd HH:00")) +
                       " and dat_po = " + Utils.EStrNull(_dpo.ToString("yyyy-MM-dd HH:00"));
                if (_additionalFinder.tn.Trim() != "") sql += " and tn = " + Utils.EStrNull(_additionalFinder.tn.Trim());
                else sql += " and (tn is null or tn = '')";

                sql += " and nzp_kvar in (select nzp_kvar " +
                       " from " + _pref + DBManager.sDataAliasRest + "kvar " +
                       " where nzp_dom = " + _additionalFinder.nzp_dom + ") ";

                editData.dopFind = new List<string>();
                editData.dopFind.Add(sql);

                //перечисляем ключевые поля и значения (со знаком сравнения!)
                editData.keys = new Dictionary<string, string>();
                editData.keys.Add("nzp_kvar", "1|nzp_kvar|kvar|" + _kvarFilter); //ссылка на ключевую таблицу

                //перечисляем поля и значения этих полей, которые вставляются
                editData.vals = new Dictionary<string, string>();
                editData.vals.Add("comment", _finder.comment);
                editData.vals.Add("nzp_serv", _finder.nzp_serv.ToString(CultureInfo.InvariantCulture));
                if (_finder.nzp_supp > 0) editData.vals.Add("nzp_supp", _finder.nzp_supp.ToString(CultureInfo.InvariantCulture));
                editData.vals.Add("nzp_kind", _finder.nzp_kind.ToString(CultureInfo.InvariantCulture));
                if (_finder.tn.Trim() != "") editData.vals.Add("tn", _finder.tn.Trim());
            }
            else
            {
                //условие выборки данных из целевой таблицы           
                sql = " and nzp_serv = " + nedop.nzp_serv;
                if (editData.todelete && _finder.nzp_kind > 0) sql = " and nzp_kind = " + _finder.nzp_kind;
                sql += " and " + _kvarFilter;
                editData.dopFind = new List<string> { sql };

                //перечисляем ключевые поля и значения (со знаком сравнения!)
                editData.keys = new Dictionary<string, string>();

                if (Utils.GetParams(_finder.prms, Constants.page_groupnedop))
                    editData.keys.Add("nzp_kvar", "1|nzp_kvar|kvar|" + _kvarFilter); //ссылка на ключевую таблицу
                else if (Utils.GetParams(_finder.prms, Constants.page_group_nedop_dom))
                    editData.keys.Add("nzp_kvar", "1|nzp_kvar|kvar|" + _domFilter); //ссылка на ключевую таблицу

                editData.keys.Add("nzp_serv", "2|" + nedop.nzp_serv);
                if (Utils.GetParams(_finder.prms, Constants.act_del_nedop) && _finder.nzp_kind > 0)
                    editData.keys.Add("nzp_kind", "2|" + _finder.nzp_kind);

                //перечисляем поля и значения этих полей, которые вставляются
                editData.vals = new Dictionary<string, string> { { "tn", _finder.tn } };
                editData.vals.Add("comment", _finder.comment);
                if (Utils.GetParams(_finder.prms, Constants.act_add_nedop) || _finder.nzp_kind <= 0)
                    editData.vals.Add("nzp_kind", _finder.nzp_kind.ToString(CultureInfo.InvariantCulture));
                editData.vals.Add("nzp_supp", _finder.nzp_supp.ToString(CultureInfo.InvariantCulture));
            }

            return editData;
        }


        /// <summary>
        /// Добавление признаков перерасчетов
        /// </summary>
        /// <param name="editData"></param>
        private void AddRevalReason(EditInterData editData)
        {
            if (Points.RecalcMode != RecalcModes.AutomaticWithCancelAbility) return;

            var eid = new EditInterDataMustCalc
            {
                mcalcType = enMustCalcType.Nedop,
                nzp_wp = editData.nzp_wp,
                pref = editData.pref,
                nzp_user = editData.local_user < 1 ? editData.nzp_user : editData.local_user,
                webLogin = editData.webLogin,
                webUname = editData.webUname
            };

            RecordMonth rm = Points.GetCalcMonth(new CalcMonthParams(editData.pref));
            eid.dat_s = "'" + new DateTime(rm.year_, rm.month_, 1).ToShortDateString() + "'";
            eid.dat_po = "'" + new DateTime(rm.year_, rm.month_, 1).AddMonths(1).AddDays(-1).ToShortDateString() +
                         "'";
            eid.intvType = editData.intvType;
            eid.table = editData.table;
            eid.database = editData.database;
            eid.primary = editData.primary;
            eid.kod2 = 0;

            eid.keys = new Dictionary<string, string>();
            eid.vals = new Dictionary<string, string>();

            eid.dopFind = editData.dopFind;
            eid.comment_action = _finder.comment_action;


            var dbMustCalcNew = new DbMustCalcNew(_connection);
            dbMustCalcNew.MustCalc(eid, out _ret);
        }


        /// <summary>
        /// Добавление событий в лог
        /// </summary>
        /// <param name="editData"></param>
        private void AddSysEvents(EditInterData editData)
        {
            int nzpDict = 6602;
            if (editData.todelete)
            {
                nzpDict = 6603;
            }

            DbAdmin.InsertSysEvent(new SysEvents
            {
                pref = _finder.pref,
                nzp_user = _finder.nzp_user,
                nzp_dict = nzpDict,
                nzp_obj = _finder.nzp_kvar,
                note =
                    "Период с " + _finder.dat_s + " по " + _finder.dat_po + ". " +
                    (_finder.tn != "" ? "Процент снятия: " + _finder.tn + ". " : "")
            }, _connection);

        }
    }
}
