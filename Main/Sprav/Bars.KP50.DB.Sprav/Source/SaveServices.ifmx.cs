using System;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;
using System.IO;
using System.Text;
using System.Collections.Generic;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;
using STCLINE.KP50.Utility;


namespace STCLINE.KP50.DataBase
{
    //----------------------------------------------------------------------
    public class DbSaveServices : DataBaseHead
    //----------------------------------------------------------------------
    {

        public Returns SaveService(Service finder, Service primfinder)
        {
            IDbConnection connDB = GetConnection(Constants.cons_Kernel);
            Returns ret = OpenDb(connDB, true);
            if (!ret.result) return ret;

            try
            {
                if (Utils.GetParams(finder.prms, Constants.page_services))
                {
                    using (var serviceToDictionary = new DBSaveServiceToDictionary(finder, connDB))
                    {
                        ret = serviceToDictionary.Save();
                    }
                }
                else if (Utils.GetParams(finder.prms, Constants.page_available_service) || Utils.GetParams(finder.prms, Constants.page_new_available_service))
                {
                    using (var serviceToLFoss = new DBSaveServiceToLFoss(finder, connDB))
                    {
                        ret = serviceToLFoss.Save();
                    }
                }
                else
                {
                    using (var dBSaveServices = new DBSaveServices(finder, primfinder, connDB))
                    {
                        ret = dBSaveServices.Save();
                    }
                }
            }
            catch (Exception ex)
            {
                ret.text = "Ошибка сохранения услуги " + ex.Message;
                return ret;
            }
            finally
            {
                connDB.Close();
            }
            return ret;
        }
    
    }

    public class DBSaveServiceToDictionary : IDisposable
    {
        
        /// <summary>
        /// Объект услуга
        /// </summary>
        private readonly Service _finder;

        /// <summary>
        /// Подключение к БД
        /// </summary>
        private readonly IDbConnection _connDB;
        /// <summary>
        /// Таблица услуг, которую необходимо поменять
        /// </summary>
        private string _table;

        /// <summary>
        /// Результат сохранения
        /// в ret.tag записан номер новой услугим
        /// </summary>
        private Returns _ret;
        private string _fieldIns = "";
        private string _valueIns = "";

        public DBSaveServiceToDictionary(Service finder, IDbConnection connDB)
        {
            _finder = finder;
            _connDB = connDB;
        }

        /// <summary>
        /// Сохранение одной услуги в справочник
        /// </summary>
        /// <returns></returns>
        public Returns Save()
        {

            try
            {
                CheckFinder();

                //CheckUniqueOrdering();

                PrepareFields();

                if (IsCentralBank())
                {

                    UpdateServiceComplex();
                }
                else
                {
                    AddServiceToCentral();
                }
            }
            catch (Exception ex)
            {

                _ret.text = ex.Message;
                _ret.result = false;
                _ret.tag = -1;
                MonitorLog.WriteLog(" Ошибка сохранения интервальных данных " + _ret.text,
                    MonitorLog.typelog.Error, 20, 201, true);
                return _ret;
            }


            return _ret;
        }


        /// <summary>
        /// Проверка на центральный банк
        /// Если пришла услуга в finder значит
        /// услуга уже есть, а выбрать ее можно 
        /// только в верхнем банке
        /// </summary>
        /// <returns></returns>
        private bool IsCentralBank()
        {
            return _finder.nzp_serv > 0;
        }


        /// <summary>
        /// Подготовка полей для Изменения
        /// в случае центрального банка
        /// </summary>
        private void PrepareFields()
        {
            if (_finder.pref == Points.Pref)
            {
                _fieldIns = ",ordering_std";
                _valueIns = ", " + _finder.ordering;
            }
        }


        /// <summary>
        /// Проверка на уникальность порядкового номера услуги
        /// </summary>
        private void CheckUniqueOrdering()
        {
            string sql = " select nzp_serv from " + _table +
                         " where ordering = " + _finder.ordering +
                         " and nzp_serv not in (6,7,8,9,15,25,510,511,512,513,514,515,516,517) and nzp_serv <> " + _finder.nzp_serv;

            MyDataReader reader;
            _ret = DBManager.ExecRead(_connDB, out reader, sql, true);
            if (!_ret.result)
            {
                throw new Exception("Ошибка поиска номера услуги");
            }

            try
            {
                if (reader.Read())
                {
                    throw new Exception("Порядковый номер услуги должен быть уникальным " + _finder.ordering +
                    " " + reader["nzp_serv"]);
                }
            }
            finally
            {
                reader.Close();
            }

        }


        /// <summary>
        /// Добавление услуги в центральный банк
        /// </summary>
        /// <returns></returns>
        private void AddServiceToCentral()
        {

            IDbTransaction transaction = _connDB.BeginTransaction();
            int nzpServ;
            try
            {
                nzpServ = GetNextServNum(transaction);

                AddService(nzpServ, transaction);
            }
            catch (Exception)
            {

                transaction.Rollback();

                throw;
            }

            transaction.Commit();
            _finder.nzp_serv = nzpServ;
            _ret.tag = _finder.nzp_serv;

        }


        /// <summary>
        /// Добавление услуги в локальный банк
        /// </summary>
        private void AddServiceToLocal()
        {
           AddService(_finder.nzp_serv, null);
           _ret.tag = _finder.nzp_serv;
        }


        /// <summary>
        /// Добавление услуги
        /// </summary>
        /// <param name="nzpServ">Код услуги</param>
        /// <param name="transaction">Транзакция, если нет, то null</param>
        private void AddService(int nzpServ, IDbTransaction transaction)
        {
            string sql = "insert into " + _table +
                         " (nzp_serv, service, service_small, service_name, ed_izmer,nzp_measure, type_lgot, ordering" +
                          ")" +
                         " values (" + nzpServ +
                         ", " + Utils.EStrNull(_finder.service) +
                         ", " + Utils.EStrNull(_finder.service_small) +
                         ", " + Utils.EStrNull(_finder.service_name) +
                         ", " + Utils.EStrNull(_finder.ed_izmer) +
                         ", " + _finder.nzp_measure +
                         ", 2" +
                         ", " + _finder.ordering +
                         //   ", 0" +
                          ")";
            _ret = DBManager.ExecSQL(_connDB, transaction, sql, true);
            if (!_ret.result)
            {
                throw new Exception("Ошибка добавления услуги " + _table);
            }
        }


        /// <summary>
        /// Получение следующего по порядку номера услуги
        /// </summary>
        /// <param name="transaction">Транзакция, если нет, то null</param>
        /// <returns></returns>
        private int GetNextServNum(IDbTransaction transaction)
        {
            string sql = "select max(nzp_serv) from " + _table;

            object obj = DBManager.ExecScalar(_connDB, transaction, sql, out _ret, true);
            if (!_ret.result)
            {
                throw new Exception("Ошибка поиска максимального номера услуги " + _table);
            }


            if (obj == null) return 100000;

            int result;
            if (!Int32.TryParse(obj.ToString(), out result))
            {
                throw new Exception("Ошибка при определении кода услуги " + obj);
            }
            if (result < 100000) return 100000;
            
            return ++result;
        }


        /// <summary>
        /// Либо обновление существующего центрального банка
        /// либо спуск вниз
        /// </summary>
        private void UpdateServiceComplex()
        {

            if (IsServiceExists(_finder.nzp_serv))
            {
                
                UpdateService();
            }
            else
            {
                AddServiceToLocal();
            }

        }


        /// <summary>
        /// Проверка на существование услуги
        /// </summary>
        /// <returns></returns>
        private bool IsServiceExists(int nzpServ)
        {
            
            string sql = "select nzp_serv from " + _table + " where nzp_serv = " + nzpServ;
            
            object obj = DBManager.ExecScalar(_connDB, sql, out _ret, true);

            if (!_ret.result)
            {
                throw new Exception("Ошибка поиска услуги");
            }

            return obj != null;
        }

        /// <summary>
        /// Обновление атрибутов услуги
        /// </summary>
        private void UpdateService()
        {

            string fieldUpd = "";
            //if (_finder.pref == Points.Pref) fieldUpd = ", ordering_std = " + _finder.ordering;
            //else fieldUpd = "";

            StringBuilder sql = new StringBuilder("update " + _table + " set ");
            sql.Append(" service_name = " + Utils.EStrNull(_finder.service_name));
            sql.Append(", ordering = " + _finder.ordering);
            if (!(_finder.nzp_serv < 100000))
            {
                sql.Append(", service = " + Utils.EStrNull(_finder.service));
                sql.Append(", service_small = " + Utils.EStrNull(_finder.service_small));
                sql.Append(", ed_izmer = " + Utils.EStrNull(_finder.ed_izmer));
                sql.Append(", nzp_measure = " + _finder.nzp_measure);
                sql.Append(fieldUpd);            
            }
            sql.Append(" where nzp_serv = " + _finder.nzp_serv);
            _ret = DBManager.ExecSQL(_connDB, sql.ToString(), true);
            if (!_ret.result)
            {
               throw new Exception("Ошибка обновление атрибутов услуги");
            }
        }


        /// <summary>
        /// Проверка Finder
        /// </summary>
        private void CheckFinder()
        {
            if (_finder.nzp_user < 1)
            {
                throw new Exception("Не определен пользователь");

            }
            if (_finder.service.Trim() == "")
            {
                throw new Exception("Не задано наименование услуги");
            }
            if (_finder.service_small.Trim() == "")
            {
                throw new Exception("Не задано краткое наименование услуги");
            }
            if (_finder.service_name.Trim() == "")
            {
                throw new Exception("Не задано наименование услуги для счетов на оплату");

            }
            //if (!Points.IsSmr)
            //    if (_finder.nzp_serv > 0 && _finder.nzp_serv < 100000 && _finder.pref == "")
            //    {
            //        throw new Exception("Редактировать можно только пользовательские услуги");
            //    }

            _finder.service = _finder.service.Trim();
            _finder.service_small = _finder.service_small.Trim();
            _finder.service_name = _finder.service_name.Trim();

            if (_finder.pref == "") _finder.pref = Points.Pref;

            _table = _finder.pref + "_kernel" + DBManager.tableDelimiter + "services";
        }

        public void Dispose()
        { }
    }


    public class DBSaveServiceToLFoss : IDisposable
    {
         /// <summary>
        /// Объект услуга
        /// </summary>
        private readonly Service _finder;

        /// <summary>
        /// Подключение к БД
        /// </summary>
        private readonly IDbConnection _connDB;

        private Returns _ret;

        public DBSaveServiceToLFoss(Service finder, IDbConnection connDB)
        {
            _finder = finder;
            _connDB = connDB;
        }

        /// <summary>
        /// Сохранение доступной услуги в l_foss
        /// </summary>
        /// <returns></returns>
        public Returns Save()
        {
            Returns ret = Utils.InitReturns();

            try
            {

            
            CheckFinder();

            if (Utils.GetParams(_finder.prms, Constants.act_add_serv))
            {
                SaveToLfoss();
            }
            else if (Utils.GetParams(_finder.prms, Constants.act_del_serv))
            {
                DeleteFromLfoss();
            }

            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;
                ret.tag = -1;
                return ret;
            }
            return ret;
        }


        /// <summary>
        /// Удаление периода действия услуги из l_foss
        /// </summary>
        private void DeleteFromLfoss()
        {
            string sql = "delete from " + _finder.pref + "_kernel" + DBManager.tableDelimiter + "l_foss where nzp_foss = " +
                         _finder.nzp_foss;
            _ret = DBManager.ExecSQL(_connDB, sql, true);
        }


        /// <summary>
        /// Сохранение данных у таблицу l_foss
        /// </summary>
        private void SaveToLfoss()
        {
            CheckServInLocalBank();
            CheckLfossPeriodCross();

            
            string sql = "insert into " + _finder.pref + "_kernel" + DBManager.tableDelimiter +
                         "l_foss (nzp_serv, nzp_supp, nzp_frm, dat_s, dat_po" +
                         (_finder.nzp_fd > 0 ? ",nzp_fd" : "") +
                         ") values (" +
                         _finder.nzp_serv + "," + _finder.nzp_supp + "," + _finder.nzp_frm + "," +
                         Utils.EStrNull(_finder.dat_s) + "," +
                         Utils.EStrNull(_finder.dat_po) +
                         (_finder.nzp_fd > 0 ? ", " + _finder.nzp_fd : "") +
                         ")";
            _ret = DBManager.ExecSQL(_connDB, sql, true);
            if (!_ret.result) throw new Exception("Ошибка добавление периода в l_foss");
        }

        /// <summary>
        /// Проверка на наличие услуги в выбранном для сохранения банке
        /// </summary>
        private void CheckServInLocalBank()
        {
            var sql = " select 1 from " + _finder.pref + "_kernel" + DBManager.tableDelimiter + "services" +
                         " where nzp_serv = " + _finder.nzp_serv + " limit 1";
            MyDataReader reader = null;
            try
            {
                _ret = DBManager.ExecRead(_connDB, out reader, sql, true);
                if (!_ret.result)
                {
                    throw new Exception("Ошибка проверки наличия услуги в локальном банке");
                }
            
                if (!reader.Read())
                {
                    throw new Exception("Услуга не добавлена в локальный банк (" + Points.GetPoint(_finder.pref).point.Trim()
                        + "). Просьба перейти в справочник \"Услуги\", добавить услугу в локальный банк (" + Points.GetPoint(_finder.pref).point.Trim()
                        + "), а затем повторить операцию.");
                }
            }
            finally
            {
                if (reader != null) reader.Close();
            }
        }


        /// <summary>
        /// Проверка на пересечение интервалов в l_foss
        /// </summary>
        private void CheckLfossPeriodCross()
        {
            string sql = " select nzp_foss from " + _finder.pref + "_kernel" + DBManager.tableDelimiter + "l_foss" +
                         " where nzp_serv = " + _finder.nzp_serv + 
                         "      and nzp_supp = " + _finder.nzp_supp + 
                         "      and nzp_frm = " + _finder.nzp_frm +
                         "      and dat_s <= " + Utils.EStrNull(_finder.dat_po) + 
                         "      and dat_po >= " + Utils.EStrNull(_finder.dat_s);
            MyDataReader reader;
            _ret = DBManager.ExecRead(_connDB, out reader, sql, true);
            if (!_ret.result)
            {
                throw new Exception("Ошибка доступа к lfoss");
            }

            try
            {
                if (reader.Read())
                {
                    throw new Exception("Добавляемый период не должен пересекаться с имеющимися " +
                                        "периодами действия услуги с тем же поставщиком и формулой расчета");
                }
            }
            finally
            {
                reader.Close();
            }
        }


        /// <summary>
        /// Проверка входящих параметров
        /// </summary>
        private void CheckFinder()
        {
            if (_finder.nzp_user < 1)
            {
                throw new Exception("Не определен пользователь");
            }
            if (!Utils.GetParams(_finder.prms, Constants.act_add_serv) && 
                !Utils.GetParams(_finder.prms, Constants.act_del_serv))
            {
                throw new Exception("Не задана операция");
            }
            if (Utils.GetParams(_finder.prms, Constants.act_add_serv))
            {
                if (_finder.nzp_serv < 1 || _finder.nzp_supp < 1 || _finder.nzp_frm < 1)
                {
                    throw new Exception("Неверные входные параметры");
                }
            }
            else if (Utils.GetParams(_finder.prms, Constants.act_del_serv))
            {
                if (_finder.nzp_foss < 1)
                {
                    throw new Exception("Не задан период действия услуги");
                }
            }


            
        }

        public void Dispose()
        { }
    }


    public class DBSaveServices : IDisposable
    {

        private enum SaveModes
        {
            None = 0x00,
            ServiceOfOneKvar = 0x01,
            ServiceOfGroupKvar = 0x02,
            ServiceOfGroupDom = 0x03,
            ServiceOfOneDom = 0x04,
            ServiceOfDom = 0x05
        }

        /// <summary>
        /// Объект услуга
        /// </summary>
        private readonly Service _finder;


        /// <summary>
        /// Объект услуга
        /// </summary>
        private readonly Service _primfinder;

        /// <summary>
        /// Подключение к БД
        /// </summary>
        private readonly IDbConnection _connDB;

        private Returns _ret = Utils.InitReturns();

        private SaveModes _mode;

        private string _pref;

        private string _tXXSpls;
        private string _tXXSplsFull;
        private string _tXXSpdom;
        private string _tXXSpdomFull;
        private int _numLs;
        private string _kvar;
        private string _kvarFilter;
        private string _domFilter;
        private bool _one_actual_supp;

        public DBSaveServices(Service finder, Service primfinder, IDbConnection connDB)
        {
            _finder = finder;
            _primfinder = primfinder;
            _connDB = connDB;
        }


        /// <summary> 1. Добавление или удаление услуги лицевым счетам (одному ЛС или выбранным ЛС)
        /// 2. Добавление или удаление периода действия услуги с информацией о действующем в этом периоде поставщике и формуле расчета
        /// </summary>
        public Returns Save()
        {
            // Алгоритм
            // 1. Если это групповая операция
            // 1.1. Определить список префиксов БД
            // 1.2. Организовать цикл по префиксам
            // 1.2.1. Для каждого префикса выполнить сохранение
            // 2. Если это операция с одним домом или ЛС
            // 2.1. Выполнить сохранение

            try
            {
                CheckFinder();

                SetSaveMode();

                CheckPrimFinder();

                PrepareOperation();

                var editData = PrepareEditInterData();

                SaveToSysEvents(editData);

                SaveService(editData);

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
                    _ret.tag = 0;
                    MonitorLog.WriteLog(" Ошибка сохранения услуги DBSaveServices: " +
                                        Environment.NewLine + _ret.text,
                        MonitorLog.typelog.Error, 20, 201, true);
            }
            return _ret;
        }

        /// <summary>
        /// Сохранение услуги и признаков перерасчетов
        /// </summary>
        /// <param name="editData"></param>
        private void SaveService(EditInterData editData)
        {
            var db2 = new DbSaverNew(_connDB, editData);
            db2.Saver();

            if (Points.RecalcMode != RecalcModes.AutomaticWithCancelAbility) return;

            if (_mode != SaveModes.ServiceOfOneKvar && _mode != SaveModes.ServiceOfOneDom &&
                _mode != SaveModes.ServiceOfGroupKvar && _mode != SaveModes.ServiceOfGroupDom &&
                _mode != SaveModes.ServiceOfDom) return;
            
            var dbMustCalcNew = new DbMustCalcNew(_connDB);
            dbMustCalcNew.MustCalc(PrepareEditMustCalc(editData), out _ret);
        }


        /// <summary>
        /// Подготовка структуры данных для 
        /// сохранения признаков перерасчета
        /// </summary>
        /// <param name="editData"></param>
        /// <returns></returns>
        private EditInterDataMustCalc PrepareEditMustCalc(EditInterData editData)
        {
            var eid = new EditInterDataMustCalc
            {
                nzp_wp = editData.nzp_wp,
                pref = editData.pref,
                nzp_user = editData.local_user < 1 ? editData.nzp_user : editData.local_user,
                intvType = editData.intvType,
                table = editData.table,
                database = editData.database,
                primary = editData.primary,
                mcalcType = enMustCalcType.mcalc_Serv,
                dopFind = new List<string>(),
                keys = new Dictionary<string, string>(),
                vals = new Dictionary<string, string>(),
                dat_s = editData.dat_s,
                dat_po = editData.dat_po
            };

            switch (_mode)
            {
                case SaveModes.ServiceOfOneKvar:
                    eid.dopFind.Add(" and nzp_kvar = " + _finder.nzp_kvar + " and nzp_serv = " + _finder.nzp_serv);
                    break;

                case SaveModes.ServiceOfOneDom:
                    //eid.dopFind.Add(" and nzp_kvar in (select nzp_kvar from " + Points.Pref + "_data@" + conn_db.Server + ":kvar where nzp_dom = " + finder.nzp_dom + ") and nzp_serv = " + finder.nzp_serv);
                    var dat = new DateTime(_primfinder.year_, _primfinder.month_, 1);
                    eid.dopFind.Add(" and nzp_kvar in (Select k.nzp_kvar " +
                                    " From " + _pref + "_data" + DBManager.tableDelimiter + "tarif t, " +
                                    _pref + "_data" + DBManager.tableDelimiter + "kvar k " +
                                    " Where k.nzp_dom = " + _primfinder.nzp_dom +
                                    " and t.nzp_kvar=k.nzp_kvar " +
                                    " and t.nzp_serv = " + _primfinder.nzp_serv +
                                    " and t.nzp_supp = " + _primfinder.nzp_supp +
                                    " and t.nzp_frm = " + _primfinder.nzp_frm +                                  
                                    " and t.dat_s < '" + dat.AddMonths(1).ToShortDateString() + "' " +
                                    " and t.dat_po >= '" + dat.ToShortDateString() + "')");
                    break;

                case SaveModes.ServiceOfGroupKvar:
                    eid.dopFind.Add(" and nzp_kvar in (select nzp_kvar from " + _tXXSplsFull +
                                    " where mark = 1 and pref = " + Utils.EStrNull(_pref) + ") ");
                    break;

                case SaveModes.ServiceOfGroupDom:
                    eid.dopFind.Add(" and nzp_kvar in (select b.nzp_kvar from " + _tXXSpdomFull + " a, " + _kvar +
                                    " b where a.pref = " + Utils.EStrNull(_pref) +
                                    " and a.mark = 1 and a.nzp_dom = b.nzp_dom) ");
                    break;
            }
            eid.comment_action = _finder.comment_action;
            return eid;
        }


        /// <summary>
        /// Сохранение события в системе
        /// </summary>
        /// <param name="editData"></param>
        private void SaveToSysEvents(EditInterData editData)
        {
            int nzpDict = 6608;
            string comment = " была открыта";
            if (editData.todelete)
            {
                nzpDict = 6609;
                comment = " была закрыта";
            }

            var serv = DBManager.ExecScalar(_connDB,
                " select service " +
                " from " + Points.Pref + "_kernel" + DBManager.tableDelimiter + "services " +
                " where nzp_serv = " +
                _finder.nzp_serv, out _ret, true);
            DbAdmin.InsertSysEvent(new SysEvents()
            {
                pref = _finder.pref,
                nzp_user = _finder.nzp_user,
                nzp_dict = nzpDict,
                nzp_obj = _primfinder != null ? _primfinder.nzp_dom : _finder.nzp_dom,
                note = "Услуга " + (serv != null ? serv.ToString().Trim() : "") + comment
            }, _connDB);

        }


        /// <summary>
        /// Заполнение класса EditInterData
        /// </summary>
        /// <returns></returns>
        private EditInterData PrepareEditInterData()
        {
            var editData = new EditInterData();
            editData.pref = _pref;
            editData.nzp_wp = Points.GetPoint(editData.pref).nzp_wp;
            editData.nzp_user = _finder.nzp_user;
            editData.webLogin = _finder.webLogin;
            editData.webUname = _finder.webUname;
            editData.primary = "nzp_tarif";
            editData.table = "tarif";
            editData.todelete = Utils.GetParams(_finder.prms, Constants.act_del_serv);
            editData.intvType = enIntvType.intv_Day;
            editData.dat_s = _finder.dat_s;
            editData.dat_po = _finder.dat_po == DateTime.MaxValue.ToString("dd.MM.yyyy") ? "01.01.3000" : _finder.dat_po;

            //условие выборки данных из целевой таблицы
            editData.dopFind = GetMainSQLCondition(editData);

            //перечисляем ключевые поля и значения (со знаком сравнения!)
            editData.keys = GetEditDataKeys(editData);

            //перечисляем поля и значения этих полей, которые вставляются
            editData.vals = GetEidtDataVals(editData);

            return editData;
        }

        /// <summary>
        /// Получаем поля и значения этих полей, которые вставляются
        /// </summary>
        /// <param name="editData"></param>
        /// <returns></returns>
        private Dictionary<string, string> GetEidtDataVals(EditInterData editData)
        {
            var vals = new Dictionary<string, string>();
            if (_finder.one_actual_supp)
            {
                if (!editData.todelete || _finder.nzp_supp <= 0)
                {
                    vals.Add("nzp_supp", _finder.nzp_supp.ToString(CultureInfo.InvariantCulture));
                }
            }
            vals.Add("nzp_frm", _finder.nzp_frm.ToString(CultureInfo.InvariantCulture));
            vals.Add("tarif", "0");
            if (_mode == SaveModes.ServiceOfOneKvar)
            {
                vals.Add("num_ls", _numLs.ToString(CultureInfo.InvariantCulture));
            }
            return vals;
        }

        /// <summary>
        /// Получаем ключевые поля и значения (со знаком сравнения!)
        /// </summary>
        /// <param name="editData"></param>
        /// <returns></returns>
        private Dictionary<string, string> GetEditDataKeys(EditInterData editData)
        {

            var keys = new Dictionary<string, string>();
            if (_mode == SaveModes.ServiceOfOneKvar)
            {
                keys.Add("nzp_kvar", "1|nzp_kvar|kvar|nzp_kvar = " + _finder.nzp_kvar); //ссылка на ключевую таблицу
            }
            else if (_mode == SaveModes.ServiceOfGroupKvar)
            {
                keys.Add("nzp_kvar", "1|nzp_kvar|" + _tXXSplsFull + "|" + _kvarFilter); //ссылка на ключевую таблицу
                keys.Add("num_ls",
                    "1|num_ls|" + _tXXSplsFull + "|num_ls in (select num_ls from " + _tXXSplsFull +
                    " where  mark = 1 and  pref = " + Utils.EStrNull(_pref) + ") "); //ссылка на ключевую таблицу
            }
            else if (_mode == SaveModes.ServiceOfGroupDom)
            {
                keys.Add("nzp_kvar", "1|nzp_kvar|kvar|" + _domFilter); //ссылка на ключевую таблицу
                keys.Add("num_ls", "1|num_ls|kvar|" + _domFilter); //ссылка на ключевую таблицу
            }
            else if (_mode == SaveModes.ServiceOfOneDom)
            {
                keys.Add("nzp_kvar", "1|nzp_kvar|kvar|" + _kvarFilter); //ссылка на ключевую таблицу
                keys.Add("num_ls", "1|num_ls|kvar|" + _kvarFilter); //ссылка на ключевую таблицу
            }
            else if (_mode == SaveModes.ServiceOfDom)
            {
                keys.Add("nzp_kvar", "1|nzp_kvar|kvar|" + _domFilter); //ссылка на ключевую таблицу
                keys.Add("num_ls", "1|num_ls|kvar|" + _domFilter); //ссылка на ключевую таблицу
            }
            keys.Add("nzp_serv", "2|" + _finder.nzp_serv);

            if (_finder.one_actual_supp)
            {
                if (editData.todelete && _finder.nzp_supp > 0)
                {
                    keys.Add("nzp_supp", "2|" + _finder.nzp_supp);
                }            
            }
            else keys.Add("nzp_supp", "2|" + _finder.nzp_supp);
            return keys;
        }


        /// <summary>
        /// условие выборки данных из целевой таблицы
        /// </summary>
        /// <param name="editData"></param>
        /// <returns></returns>
        private List<string> GetMainSQLCondition(EditInterData editData)
        {

            var result = new List<string>(); 
            string sql = " and nzp_serv = " + _finder.nzp_serv;
            if (_finder.one_actual_supp)
            {
                if (editData.todelete && _finder.nzp_supp > 0)
                    sql += " and nzp_supp = " + _finder.nzp_supp;
            }
            else sql += " and nzp_supp = " + _finder.nzp_supp;

            if (_mode == SaveModes.ServiceOfOneKvar) sql += " and nzp_kvar = " + _finder.nzp_kvar;
            else if (_mode == SaveModes.ServiceOfGroupKvar)
            {

                _kvarFilter = "nzp_kvar in (select nzp_kvar from " + _tXXSplsFull + " where mark = 1 and pref = " +
                              Utils.EStrNull(_pref) + ") ";
                sql += " and " + _kvarFilter;

            }
            else if (_mode == SaveModes.ServiceOfGroupDom)
            {
                _kvarFilter = "nzp_kvar in (select b.nzp_kvar from " + _tXXSpdomFull + " a, " + _kvar + " b where a.pref = " +
                              Utils.EStrNull(_pref) + " and a.mark = 1 and a.nzp_dom = b.nzp_dom) ";
                _domFilter = "nzp_dom in (select nzp_dom from " + _tXXSpdomFull + " where pref = " + Utils.EStrNull(_pref) +
                             " and mark = 1) ";
                sql += " and " + _kvarFilter;

            }
            else if (_mode == SaveModes.ServiceOfOneDom)
            {
                var dat = new DateTime(_primfinder.year_, _primfinder.month_, 1);
                _kvarFilter = "nzp_kvar in (Select k.nzp_kvar " +
                              " From " + _pref + "_data" + DBManager.tableDelimiter + "tarif t, " +
                              _pref + "_data" + DBManager.tableDelimiter + "kvar k " +
                              " Where k.nzp_dom = " + _primfinder.nzp_dom +
                              " and t.nzp_kvar=k.nzp_kvar " +
                              " and t.nzp_serv = " + _primfinder.nzp_serv +
                              " and t.nzp_supp = " + _primfinder.nzp_supp +
                              " and t.nzp_frm = " + _primfinder.nzp_frm +                           
                              " and t.dat_s < '" + dat.AddMonths(1).ToShortDateString() + "' " +
                              " and t.dat_po>='" + dat.ToShortDateString() + "')";
                sql += " and " + _kvarFilter;
            }
            else if (_mode == SaveModes.ServiceOfDom)
            {
                _kvarFilter = "nzp_kvar in (Select k.nzp_kvar " +
                              " From " + _pref + "_data" + DBManager.tableDelimiter + "kvar k " +
                              " Where k.nzp_dom = " + _finder.nzp_dom + ")";
                _domFilter = "nzp_dom = " + _finder.nzp_dom;
                sql += " and " + _kvarFilter;
            }
            result.Add(sql);
            return result;
        }


        /// <summary>
        /// Операция подготовки
        /// </summary>
        private void PrepareOperation()
        {
            if (_mode == SaveModes.ServiceOfGroupKvar || _mode == SaveModes.ServiceOfGroupDom)
                // групповая операция по выбранным спискам ЛС или домов
            {
                PrepareGroupOperation();
                
            }
            else if (_mode == SaveModes.ServiceOfOneKvar)
            {
                SetNumLs();
            }
            else if (_mode == SaveModes.ServiceOfOneDom && _primfinder == null)
            {
                throw new Exception("Ошибка при определении списка лицевых счетов");
            }
        }

      

        /// <summary>
        /// Подготовка для операции по одному лицевому счету
        /// Определение номера лицевого счета
        /// </summary>
        private void SetNumLs()
        {
            
            string s = " select max(num_ls) num_ls " +
                       " from " + _pref + "_data" + DBManager.tableDelimiter + "kvar " +
                       " where nzp_kvar = " + _finder.nzp_kvar;
            object id = DBManager.ExecScalar(_connDB, s, out _ret, true);
            if (!_ret.result)
            {
                throw new Exception(" Ошибка при определении номера " +
                                    " лицевого счета nzp_kvar=" + _finder.nzp_kvar);
            }

            if (!Int32.TryParse(id.ToString(), out _numLs))
            {
                throw new Exception(" Ошибка при определении номера " +
                                    " лицевого счета nzp_kvar=" + _finder.nzp_kvar);
            }
        }


        /// <summary>
        /// Подготовка параметров для групповой операции
        /// </summary>
        /// <returns></returns>
        private void PrepareGroupOperation()
        {

            IDbConnection connWeb = DBManager.GetConnection(Constants.cons_Webdata);
            _ret = DBManager.OpenDb(connWeb, true);

            if (!_ret.result)
            {
                throw new Exception("Ошибка открытия БД web");
            }

            try
            {
#if PG
                string connWebDataBase = "public";
#else
                string connWebDataBase = connWeb.Database;
#endif

                _tXXSplsFull = DBManager.GetFullBaseName(connWeb, connWebDataBase, _tXXSpls);
                _tXXSpdomFull = DBManager.GetFullBaseName(connWeb, connWebDataBase, _tXXSpdom);

                if (_mode == SaveModes.ServiceOfGroupKvar &&
                    (_finder.listNumber < 0 || !DBManager.TempTableInWebCashe(connWeb, _tXXSplsFull)))
                {
                    throw new UserException("Не выбран список лицевых счетов");
                }

                if (_mode == SaveModes.ServiceOfGroupDom && !DBManager.TempTableInWebCashe(connWeb, _tXXSpdomFull))
                {
                    throw new UserException("Не выбран список домов");
                }



                
            }
            finally
            {
                connWeb.Close();
            }


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

            if (!Utils.GetParams(_finder.prms, Constants.act_add_serv) && !Utils.GetParams(_finder.prms, Constants.act_del_serv))
            {
                throw new Exception("Не задана операция");
            }

            if (_finder.pref.Trim() == "")
            {
                throw new Exception("Не задан префикс базы данных");
            }
            if (_finder.nzp_serv < 1)
            {
                throw new Exception("Не задана услуга");
                
            }
            if (Utils.GetParams(_finder.prms, Constants.act_add_serv) && (_finder.nzp_supp < 1 || _finder.nzp_frm < 1))
            {
                throw new Exception("Не задан поставщик или формула расчета");
            }

            #endregion

            if (_finder.pref.Trim() == "") _finder.pref = Points.Pref;

             _pref = _finder.pref.Trim();
             _tXXSpls = "t" + Convert.ToString(_finder.nzp_user) + "_selectedls" + _finder.listNumber;
             _tXXSpdom = "t" + Convert.ToString(_finder.nzp_user) + "_spdom";
             _numLs = 0;
             _kvar = _pref + "_data" + DBManager.tableDelimiter + "kvar";
             _kvarFilter = " 1=0 ";
             _domFilter = " 1=0 ";

        }


        /// <summary>
        /// Проверка входных параметров
        /// </summary>
        private void CheckPrimFinder()
        {
            if (_mode != SaveModes.ServiceOfOneDom) return;
            if (_primfinder.year_ < 0) throw new UserException("Не задан год");
            if (_primfinder.month_ < 0) throw new UserException("Не задан месяц");
            if (_primfinder.nzp_dom < 0) throw new UserException("Не указан дом");
            if (_primfinder.nzp_serv < 0) throw new UserException("Не указана услуга");
            if (_primfinder.nzp_supp < 0) throw new UserException("Не указан поставщик");
            if (_primfinder.nzp_frm < 0) throw new UserException("Не указана формула расчета");
        }

        private void SetSaveMode()
        {
            _mode = SaveModes.None;
            if (_finder.nzp_kvar > 0) _mode = SaveModes.ServiceOfOneKvar;

            else if (Utils.GetParams(_finder.prms, Constants.page_group_supp_formuls)
                || Utils.GetParams(_finder.prms, Constants.newpage_group_supp_formuls))
                _mode = SaveModes.ServiceOfGroupKvar;

            else if (Utils.GetParams(_finder.prms, Constants.page_group_supp_formuls_dom)
                || Utils.GetParams(_finder.prms, Constants.newpage_group_supp_formuls_dom)) 
                _mode = SaveModes.ServiceOfGroupDom;

            else if (Utils.GetParams(_finder.prms, Constants.page_spisservdom)
                || Utils.GetParams(_finder.prms, Constants.page_new_spisservdom))
                _mode = SaveModes.ServiceOfOneDom;

            else if (_finder.nzp_dom > 0) _mode = SaveModes.ServiceOfDom;
        }

        public void Dispose()
        { }
    }
}
