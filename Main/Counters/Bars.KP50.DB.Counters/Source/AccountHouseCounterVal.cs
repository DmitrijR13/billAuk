using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;
using System.Data;

namespace STCLINE.KP50.DataBase
{
    public partial class DbCounter : DbCounterKernel
    {
        public List<CounterValLight> GetCountersValsForView(CounterValLight finder, out Returns ret) //найти и заполнить список показаний ПУ
        {
            ret = new Returns(true);
            
            using (ClassGetCounterValForView view = new ClassGetCounterValForView())
            {
                return view.GetCounterVal(finder, out ret);
            }
        }

        public List<CounterValLight> GetCountersValsForEdit(CounterValLight finder, out Returns ret)
        {
            ret = new Returns(true);
            
            using (ClassGetCounterValForEdit vals = new ClassGetCounterValForEdit())
            {
                return vals.GetCounterVal(finder, out ret);
            }
        }
    }
    
    /// <summary>
    /// Получить показания ПУ для редактирования
    /// </summary>
    public class ClassGetCounterValForEdit : ClassAccountHouseCounterVal
    {
        /// <summary>
        /// получить показания из charge
        /// </summary>
        private void ChargeCntValue()
        {
            string counters_vals = cur_pref + "_charge_" + (_calcMonth.year_ % 100).ToString("00") + DBManager.tableDelimiter + "counters_vals";

            string sql = " Insert into " + temp_val + " (ordering, nzp_cv, nzp_counter, dat_uchet, val_cnt, ngp_cnt, ngp_lift, nzp_user, dat_when, ist) " +
                " Select 1, a.nzp_cv, a.nzp_counter, a.dat_uchet, a.val_cnt, a.ngp_cnt, a.ngp_lift, a.nzp_user, a.dat_when, a.ist " + 
                " from " + temp_counter + " pu, " + counters_vals + " a " +
                " where pu.nzp_counter = a.nzp_counter " +
                    " and " + DBManager.sNvlWord + "(a.val_cnt, -1) > -1 " + 
                    " and a.ist = " + _finder.ist + 
                    (_finder.date_begin != "" ? " and a.dat_uchet >= " + Utils.EStrNull(_finder.date_begin) : "") +
                    " and a.month_ = (select max(d.month_) from " + counters_vals + " d " +
                        " where d.nzp_type = a.nzp_type " +
                            " and d.ist = a.ist " +
                            " and d.dat_uchet = a.dat_uchet) ";

            ExecSQLWE(_connDb, sql);
        }
        
        /// <summary>
        /// Получить показания
        /// </summary>
        protected override void GetCntValue()
        {
            if (_finder.prm == Constants.act_mode_view.ToString())
            {
                ChargeCntValue();
            }
            else
            {
                DateTime dat_uchet = DateTime.Parse(_finder.dat_uchet).AddMonths(-1);

                // получить текущие показания
                CurrentCntValue(dat_uchet);

                string counters_vals = "";
                string nzp_key = "";

                switch (_finder.nzp_type)
                {
                    case (int)CounterKinds.Kvar:
                        counters_vals = counters;
                        nzp_key = "nzp_cr";
                        break;
                    case (int)CounterKinds.Dom:
                        counters_vals = counters_dom;
                        nzp_key = "nzp_crd";
                        break;
                    default:
                        counters_vals = counters_group;
                        nzp_key = "nzp_cg";
                        break;
                }

                // получить предыдущие показания
                PreviousCntValue(counters_vals, nzp_key);

                // получить будущее показание
                NextCntValue(counters_vals, nzp_key);

                // строчки под новые показания
                string sql = " Insert into " + temp_val + " (ordering, nzp_counter, dat_uchet, is_new) " +
                    " Select -1, nzp_counter, " + Utils.EStrNull(dat_uchet.AddMonths(1).ToShortDateString()) + ", 1" + 
                    " From " + temp_counter;
                ExecSQLWE(_connDb, sql);
            }          
        }

        /// <summary>
        /// получить текущие показания (ordering = 1)
        /// </summary>
        private void CurrentCntValue(DateTime dat_uchet)
        {
            // получить текущие показания
            string sql = "";

            switch (_finder.nzp_type)
            {
                case (int)CounterKinds.Kvar:
                    sql = " Insert into " + temp_val + " (ordering, nzp_cv, nzp_counter, dat_uchet, val_cnt, ngp_cnt, nzp_user, dat_when, ist) " +
                        " Select 1, a.nzp_cr, a.nzp_counter, a.dat_uchet, a.val_cnt, " + (Points.IsIpuHasNgpCnt ? "a.ngp_cnt" : "0") + ", a.nzp_user, a.dat_when, a.ist " +
                        " from " + counters + " a ";                         
                    break;
                case (int)CounterKinds.Dom:
                    sql = " Insert into " + temp_val + " (ordering, nzp_cv, nzp_counter, dat_uchet, val_cnt, ngp_cnt, ngp_lift, nzp_user, dat_when) " +
                        " Select 1, a.nzp_crd, a.nzp_counter, a.dat_uchet, a.val_cnt, a.ngp_cnt, a.ngp_lift, a.nzp_user, a.dat_when " +
                        " from " + counters_dom + " a ";
                    break;
                default:
                    sql = " Insert into " + temp_val + " (ordering, nzp_cv, nzp_counter, dat_uchet, val_cnt, ngp_cnt, nzp_user, dat_when, ist) " +
                        " Select 1, a.nzp_cg, a.nzp_counter, a.dat_uchet, a.val_cnt, a.ngp_cnt, a.nzp_user, a.dat_when, " + (_finder.nzp_type == (int)CounterKinds.Communal ? "a.ist" : "0 ") +
                        " from " + counters_group + " a ";
                    break;
            }
            
            sql += "," + temp_counter + " pu " +
                " Where pu.nzp_counter = a.nzp_counter " +
                    " and " + DBManager.sNvlWord + "(a.val_cnt, -1) > -1 " + 
                    " and a.is_actual <> 100 " + 
                    (_finder.date_begin != "" ? " and a.dat_uchet >= " + Utils.EStrNull(_finder.date_begin) : "") + 
                    " and a.dat_uchet between " + Utils.EStrNull(dat_uchet.AddDays(1).ToShortDateString()) + 
                    " and " + Utils.EStrNull(dat_uchet.AddMonths(1).ToShortDateString());

            ExecSQLWE(_connDb, sql);

            // если нет показаний в текущем расчетном месяце, то добавить строки под них
            // чтобы:
            // 1) добавить строки под новые показания в текущем расчетном месяце
            // 2) на их основе правильно определить предыдущие показания
            sql = "drop table tmp_counter_without_curr_values";
            ExecSQL(_connDb, sql, false);

            // определить ПУ, у которых нет показаний в текущем расчетном месяце
            sql = "create temp table tmp_counter_without_curr_values (nzp_counter integer)";
            ExecSQLWE(_connDb, sql);

            sql = " insert into tmp_counter_without_curr_values (nzp_counter) " +
                " select nzp_counter " +
                " from " + temp_counter + " c " +
                " where not exists (select 1 from " + temp_val + " v  where v.nzp_counter = c.nzp_counter) ";
            ExecSQLWE(_connDb, sql);
            
            // вставить их в показания с текущим месяцем
            sql = " Insert into " + temp_val + " (ordering, nzp_counter, dat_uchet, is_new) " +
                " Select 1, nzp_counter, " + Utils.EStrNull(dat_uchet.AddMonths(1).ToShortDateString()) + ", 1" +
                " From tmp_counter_without_curr_values";
            ExecSQLWE(_connDb, sql);

            sql = "drop table tmp_counter_without_curr_values";
            ExecSQL(_connDb, sql, false);

        }

        /// <summary>
        /// получить предыдущие показания (ordering = 2)
        /// </summary>
        private void PreviousCntValue(string counters_vals, string nzp_key)
        { 
            for (int i = 1; i <= 3; i++)
            {
                string sql = "insert into " + temp_val + "(nzp_counter, dat_uchet, define_value, ordering) " +
                    " select a.nzp_counter, max(a.dat_uchet), 1 as define_value, 2 as ordering " + 
                    " from " + counters_vals + " a, " + temp_val + " t " +
                     " where a.nzp_counter = t.nzp_counter " +
                        " and " + DBManager.sNvlWord + "(a.val_cnt, -1) > -1 " +
                        " and a.is_actual <> 100 " + 
                        " and a.dat_uchet < (select min(c.dat_uchet) from " + temp_val + " c where c.nzp_counter = t.nzp_counter)" + 
                    " group by 1";
                ExecSQLWE(_connDb, sql);

                // определить показания
                DefineCntValue(counters_vals, nzp_key);
            }
        }

        /// <summary>
        /// Получить будущее показание (ordering = -2)
        /// </summary>
        /// <param name="counters_vals"></param>
        /// <param name="nzp_key"></param>
        private void NextCntValue(string counters_vals, string nzp_key)
        {
            string sql = "insert into " + temp_val + "(nzp_counter, dat_uchet, define_value, ordering) " +
                " select a.nzp_counter, min(a.dat_uchet), 1 as define_value, -2 as ordering " + 
                " from " + counters_vals + " a, " + temp_val + " t " +
                    " where a.nzp_counter = t.nzp_counter " +
                    " and " + DBManager.sNvlWord + "(a.val_cnt, -1) > -1 " +
                    " and a.is_actual <> 100 " +
                    " and a.dat_uchet > (select max(c.dat_uchet) from " + temp_val + " c where c.nzp_counter = t.nzp_counter)" + 
                " group by 1";
            ExecSQLWE(_connDb, sql);
            
            // определить показания
            DefineCntValue(counters_vals, nzp_key);
        }

        /// <summary>
        /// определение показаний на дату учета
        /// </summary>
        /// <param name="counters_vals"></param>
        /// <param name="nzp_key"></param>
        private void DefineCntValue(string counters_vals, string nzp_key)
        {
            // получить показания
            string sql = " update " + temp_val + " set " +
                " val_cnt = (select max(a.val_cnt) from " + counters_vals + " a " +
                    " where a.nzp_counter = " + temp_val + ".nzp_counter " +
                        " and " + DBManager.sNvlWord + "(a.val_cnt, -1) > -1 " +
                        " and a.dat_uchet = " + temp_val + ".dat_uchet " +
                        " and a.is_actual <> 100) " + 
                " where define_value = 1";
            ExecSQLWE(_connDb, sql);
            
            // определить ключи показаний
            sql = "Update " + temp_val + " set " +
                " nzp_cv = (select max(a." + nzp_key + ") from " + counters_vals + " a " +
                    " where a.nzp_counter = " + temp_val + ".nzp_counter " +
                        " and a.dat_uchet = " + temp_val + ".dat_uchet " +
                        " and a.val_cnt = " + temp_val + ".val_cnt " +
                        " and " + DBManager.sNvlWord + "(a.val_cnt, -1) > -1 " +
                        " and a.is_actual <> 100) " +
                " where define_value = 1";
            ExecSQLWE(_connDb, sql);
            
            // получить пользователя, дату изменений, расход на нежилые помещения, расход на электороснабжение лифтов ОДПУ
            sql = "Update " + temp_val + " set " +
                    " define_value = 0, " +
                    " nzp_user = (select max(a.nzp_user) from " + counters_vals + " a where a." + nzp_key + " = nzp_cv), " +
                    // дата изменений
                    " dat_when = (select max(a.dat_when) from " + counters_vals + " a where a." + nzp_key + " = nzp_cv), " +
                    " ist = (select max(a.ist) from " + counters_vals + " a where a." + nzp_key + " = nzp_cv) ";

            // расход на нежилые помещения
            if ((_finder.nzp_type == (int)CounterKinds.Dom) || _finder.nzp_type == (int)CounterKinds.Group || 
                 _finder.nzp_type == (int)CounterKinds.Communal || (_finder.nzp_type == (int)CounterKinds.Kvar && Points.IsIpuHasNgpCnt))
            {
                sql += ", ngp_cnt = (select max(a.ngp_cnt) from " + counters_vals + " a where a." + nzp_key + " = nzp_cv) ";
            }

            // расход на электороснабжение лифтов ОДПУ, средний расход
            if (_finder.nzp_type == (int)CounterKinds.Dom)
            {
                // расход на электороснабжение лифтов ОДПУ
                sql += ", ngp_lift = (select max(a.ngp_lift) from " + counters_vals + " a where a." + nzp_key + " = nzp_cv) ";
            }

            sql += " where define_value = 1 ";

            ExecSQLWE(_connDb, sql);
        }

        /// <summary>
        /// блокировка ПУ
        /// </summary>
        protected override void BlockCounters()
        {
            string sql = " Update " + counters_spis + " Set " + 
                " dat_block = null, user_block = null " +
                " Where user_block is not null and user_block = " + _nzpUser;
            ExecSQLWE(_connDb, sql);
            
            // заблокировать ПУ
            if (_finder.prm == Constants.act_mode_edit.ToString())
            {
                sql = " Update " + counters_spis + " Set " + 
                    " dat_block = " + DBManager.sCurDateTime + ", user_block = " + _nzpUser +
                    " Where exists (Select 1 From " + temp_counter + " t where t.nzp_counter = " + counters_spis + ".nzp_counter and t.blocked = 0) ";
                ExecSQLWE(_connDb, sql);
            }
        }

        /// <summary>
        /// проверка параметров
        /// </summary>
        /// <returns></returns>
        protected override Returns CheckInPrm()
        {
            Returns ret = CheckBaseInPrm();
            if (!ret.result) return ret;

            if (_finder.ist <= 0) return new Returns(false, "Не задан источник");

            if (_finder.ist != (int)CounterVal.Ist.Operator)
            {
                if (_finder.dat_uchet == "")
                {
                    return new Returns(false, "Не задан месяц за который редактируются показания");
                }

                DateTime dat;
                if (!DateTime.TryParse(_finder.dat_uchet, out dat))
                {
                    return new Returns(false, "Месяц за который редактируются показания, имеет неправильный формат");
                }   
            }
            
            return new Returns(true);
        }
    }
       
    /// <summary>
    /// Получить показания ПУ для просмотра
    /// </summary>
    public class ClassGetCounterValForView : ClassAccountHouseCounterVal
    {
        /// <summary>
        /// Cформировать условие для показаний ПУ
        /// </summary>
        /// <returns></returns>
        private string WhereCntValue()
        {
            string _where_val = "";

            // is_actual = 0 - не показывать архивные, > 0 - показывать и архивные и неархивные
            if (_finder.is_actual == 0) _where_val += " and a.is_actual <> 100 ";
            if (_finder.dat_uchet != "") _where_val += " and a.dat_uchet >= " + Utils.EStrNull(_finder.dat_uchet);
            if (_finder.dat_uchet_po != "") _where_val += " and a.dat_uchet <= " + Utils.EStrNull(_finder.dat_uchet_po);
            if (_finder.date_begin != "") _where_val += " and a.dat_uchet >= " + Utils.EStrNull(_finder.date_begin);

            return _where_val;
        }

        /// <summary>
        /// Получить показания
        /// </summary>
        protected override void GetCntValue()
        {
            string sql = "";

            switch (_finder.nzp_type)
            {
                case (int)CounterKinds.Kvar:
                    sql = " insert into " + temp_val + " (nzp_counter, is_actual, val_cnt, dat_uchet, dat_when, nzp_user," +
                            " nzp_cv, ist) " +
                        " Select b.nzp_counter, " +
                            " a.is_actual, a.val_cnt, a.dat_uchet, a.dat_when, a.nzp_user," +
                            " a.nzp_cr as nzp_cv, a.ist " +
                        " From " + temp_counter + " b, " + 
                        counters + " a " +
                        " Where a.nzp_counter = b.nzp_counter " + WhereCntValue();
                    break;
                case (int)CounterKinds.Dom:
                    sql = " insert into " + temp_val + " (nzp_counter, is_actual, val_cnt, dat_uchet, dat_when, nzp_user," +
                             " is_uchet_ls, ngp_cnt, ngp_lift, is_doit, name_uchet, nzp_cv) " +
                         " Select b.nzp_counter, " +
                             " a.is_actual, a.val_cnt, a.dat_uchet, a.dat_when, a.nzp_user," +
                             " a.is_uchet_ls, a.ngp_cnt, a.ngp_lift, a.is_doit, tu.name_uchet, a.nzp_crd as nzp_cv " +
                         " From " + temp_counter + " b, " + 
                         counters_dom + " a " +
                             " left outer join " + cur_pref + "_kernel" + DBManager.tableDelimiter + "s_typeuchet tu on a.is_pl = tu.is_pl " +
                         " Where a.nzp_counter = b.nzp_counter " + WhereCntValue();
                    break;
                case (int)CounterKinds.Group:
                case (int)CounterKinds.Communal:
                    sql = " insert into " + temp_val + " (nzp_counter, is_actual, val_cnt, dat_uchet, dat_when, nzp_user, " +
                            " is_uchet_ls, is_doit, name_uchet, ngp_cnt, nzp_cv, ist) " +
                        " Select b.nzp_counter, " +
                            " a.is_actual, a.val_cnt, a.dat_uchet, a.dat_when, a.nzp_user, " +
                            " a.is_uchet_ls, a.is_doit, tu.name_uchet, a.ngp_cnt, a.nzp_cg as nzp_cv, " + (_finder.nzp_type == (int)CounterKinds.Communal ? "a.ist" : "0 ") + " as ist " +
                        " From " + temp_counter + " b, " + 
                        counters_group + " a " +
                            " left outer join " + cur_pref + "_kernel" + DBManager.tableDelimiter + "s_typeuchet tu on a.is_pl = tu.is_pl " +
                        " Where a.nzp_counter = b.nzp_counter " + WhereCntValue();
                    break;
            }

            ExecSQLWE(_connDb, sql);
        }

        protected override void BlockCounters()
        {
            
        }

        protected override Returns CheckInPrm()
        {
            return CheckBaseInPrm();
        }
    }

    /// <summary>
    /// Абстрактный класс для получения показаний ПУ
    /// </summary>
    public abstract class ClassAccountHouseCounterVal : DataBaseHead
    {
        protected CounterValLight _finder;
        protected IDbConnection _connDb = null;
        private IDataReader reader = null;

        protected string temp_counter = "tmp_account_house_counters";
        protected string temp_val = "tmp_account_house_cntr_values";
        protected string temp_prev_val = "tmp_account_house_cntr_prev_values";

        protected int _nzpUser = 0;

        protected string cur_pref = "";
        protected string interval = "";
        private List<CounterValLight> Spis = null;

        protected string counters_spis = "";
        protected string users = "";
        private string prm_17 = "";
        private string s_countsdop = "";
        private string s_counts = "";

        protected string counters = "";
        protected string counters_dom = "";
        protected string counters_group = "";

        protected RecordMonth _calcMonth;

        public List<CounterValLight> GetCounterVal(CounterValLight finder, out Returns ret)
        {
            _finder = finder; 
            ret = CheckInPrm();
            if (!ret.result) return null;
            
            cur_pref = _finder.pref;
            Initialize(cur_pref);

            try
            {
                _connDb = GetConnection(Points.GetConnByPref(finder.pref));
                ret = OpenDb(_connDb, true);
                if (!ret.result) throw new Exception(ret.text);

                //_nzpUser = GetUser();
                _nzpUser = _finder.nzp_user;

                CreateCounterTempTable();
                int counterCnt = GetCounters();

                if (counterCnt == 0)
                {
                    ret = new Returns(false, "Приборы учета не найдены", -1);
                }
                else
                {
                    BlockCounters();
                    GetCounterPrms();
                    CreateCntValueTempTable();
                    GetCntValue();
                    DefinePreviouCntVal();
                    GetCntValueAddInfo();
                    GetData();
                }
            }
            catch (Exception ex)
            {
                ret = new Returns(false, "Ошибка заполнения списка показаний ПУ");
                MonitorLog.WriteLog("Ошибка заполнения списка показаний ПУ " + (Constants.Viewerror ? "\n " + ex.Message : ""), MonitorLog.typelog.Error, 20, 201, true);
            }
            finally
            {
                if (reader != null) reader.Close();
                if (_connDb != null) _connDb.Close();
            }
            
            return Spis;
        }

        /// <summary>
        /// получить пользователя
        /// </summary>
        /// <returns></returns>
        /*private int GetUser()
        {
            int nzpUser = 0;

            Returns ret = new Returns(true);
            
            using (DbWorkUser db = new DbWorkUser())
            {
                Finder userFinder = new Finder();
                userFinder.pref = _finder.pref;
                userFinder.nzp_user = _finder.nzp_user;

                ret.tag = db.GetLocalUser(_connDb, userFinder, out ret);
                db.Close();
                if (!ret.result) throw new Exception(ret.text);
                nzpUser = ret.tag;
            }

            return nzpUser;
        }*/

        /// <summary>
        /// инициализация
        /// </summary>
        /// <param name="pref"></param>
        private void Initialize(string pref)
        {
#if PG
            interval = "now() -  INTERVAL '" + Constants.users_min + " minutes'";
#else
            interval = "current year to second - " + Constants.users_min + " units minute";
#endif

            counters_spis = pref + "_data" + DBManager.tableDelimiter + "counters_spis";
            users = Points.Pref + "_data" + DBManager.tableDelimiter + "users";
            prm_17 = pref + "_data" + DBManager.tableDelimiter + "prm_17";
            s_countsdop = pref + "_kernel" + DBManager.tableDelimiter + "s_countsdop";
            s_counts = pref + "_kernel" + DBManager.tableDelimiter + "s_counts";

            counters = pref + "_data" + DBManager.tableDelimiter + "counters";
            counters_dom = pref + "_data" + DBManager.tableDelimiter + "counters_dom";
            counters_group = pref + "_data" + DBManager.tableDelimiter + "counters_group";

            _calcMonth = Points.GetCalcMonth(new CalcMonthParams(pref));
        }

        /// <summary>
        /// Проверка входных параметров
        /// </summary>
        /// <returns></returns>
        protected abstract Returns CheckInPrm();

        /// <summary>
        /// Проверка основных входных параметров
        /// </summary>
        /// <returns></returns>
        protected Returns CheckBaseInPrm()
        {
            if (_finder.nzp_user < 1)
            {
                return new Returns(false, "Не определен пользователь");
            }

            if (_finder.pref == "") 
            {
                return new Returns(false, "Не задан префикс базы данных");
            }

            if (_finder.nzp_type <= 0)
            {
                return new Returns(false, "Не определен тип прибора учета");
            }

            if (_finder.nzp_type != (int)CounterKinds.Dom &&
                _finder.nzp_type != (int)CounterKinds.Kvar &&
                _finder.nzp_type != (int)CounterKinds.Group &&
                _finder.nzp_type != (int)CounterKinds.Communal)
            {
                return new Returns(false, "Неверено определен тип прибора учета");
            }
            
            if (_finder.nzp_type == (int)CounterKinds.Dom && _finder.nzp_dom <= 0)
            {
                return new Returns(false, "Не определен дом (домовые приборы учета)");
            }

            if (_finder.nzp_type == (int)CounterKinds.Kvar && _finder.nzp_kvar <= 0)
            {
                return new Returns(false, "Не определен лицевой счет (индивидуальные приборы учета)");
            }

            if (_finder.nzp_type == (int)CounterKinds.Group && _finder.nzp_kvar <= 0 && _finder.nzp_dom <= 0)
            {
                return new Returns(false, "Не определен лицевой счет или дом (групповые приборы учета)");
            }

            if (_finder.nzp_type == (int)CounterKinds.Communal && _finder.nzp_dom <= 0)
            {
                return new Returns(false, "Не определен дом (общеквартирные приборы учета)");
            }

            return new Returns(true);
        }

        /// <summary>
        /// Cформировать условие для ПУ
        /// </summary>
        /// <returns></returns>
        private string WhereCounter()
        {
            string _where = " and b.nzp_type = " + _finder.nzp_type;

            // существующие ПУ
            _where += " and b.is_actual <> 100 ";

            // dat_close = "" - показывать только открытые, иначе - и открытые и закрытые
            if (_finder.dat_close == "") _where += " and (b.dat_close is null or b.dat_close > "+sCurDateTime+") ";
            if (_finder.nzp_counter > 0) _where += " and b.nzp_counter = " + _finder.nzp_counter.ToString();
            if (_finder.nzp_serv > 0) _where += " and b.nzp_serv = " + _finder.nzp_serv;

            if (_finder.RolesVal != null)
            {
                foreach (_RolesVal role in _finder.RolesVal)
                {
                    if (role.tip == Constants.role_sql && role.kod == Constants.role_sql_serv)
                    {
                        _where += " and b.nzp_serv in (" + role.val + ") ";
                    }
                }
            }

            switch (_finder.nzp_type)
            {
                case (int)CounterKinds.Kvar:
                    _where += " and b.nzp = " + _finder.nzp_kvar;
                    break;
                case (int)CounterKinds.Dom:
                    _where += " and b.nzp = " + _finder.nzp_dom;
                    break;
                case (int)CounterKinds.Group:
                case (int)CounterKinds.Communal:
                    if (_finder.nzp_kvar > 0)
                    {
                        _where += " and exists (select 1 from " + cur_pref + "_data" + DBManager.tableDelimiter + "counters_link cl " +
                            " where cl.nzp_counter = b.nzp_counter " +
                            " and cl.nzp_kvar = " + _finder.nzp_kvar + ") ";
                    }
                    else if (_finder.nzp_dom > 0)
                    {
                        _where += " and b.nzp = " + _finder.nzp_dom;
                    }
                    break;
            }

            return _where;
        }

        /// <summary>
        /// Создать временную таблицу для ПУ
        /// </summary>
        /// <returns></returns>
        private void CreateCounterTempTable()
        { 
            ExecSQL(_connDb, "drop table " + temp_counter, false);
            
            string sql = " create temp table " + temp_counter + " (" + 
                " nzp          integer, " +
                " nzp_counter  integer, " +
                " nzp_cnt      integer, " +
                " nzp_measure  integer, " +
                " nzp_serv     integer, " +
                " service      varchar(200), " +
                " measure      varchar(40), " +
                " num_cnt      varchar(100), " +
                " nzp_cnttype  integer, " +
                " dat_prov     date, " + 
                " dat_provnext date, " +
                " dat_oblom    date, " + 
                " dat_poch     date, " +
                " dat_close    date, " +
                " comment     varchar(120), " +
                " counter_place     varchar(255), " + // вид помещения ПУ
                " cnt_stage    integer, " +
                " mmnog       " + DBManager.sDecimalType + "(14,7)," +
                " name_type   varchar(100), " +               
                " normativ     " + DBManager.sDecimalType + "(14,7), " + // норматив
                " rashod_k_opl " + DBManager.sDecimalType + "(14,7), " + // расход к оплате
                " plan_rashod  varchar(100), " + // средний расход 
                " blocked      integer default 0, " +
                " dat_block    date, " +
                " blocked_by   varchar(200) " +
                ")";

            ExecSQLWE(_connDb, sql);
        }

        /// <summary>
        /// Создать временную таблицу для показаний ПУ
        /// </summary>
        /// <returns></returns>
        private void CreateCntValueTempTable()
        {
            ExecSQL(_connDb, "drop table " + temp_val, false);

            string sql = " create temp table " + temp_val + " (" +
                " ordering     integer default 0," +
                " define_value integer default 0, " +
                " nzp_cv       integer default 0, " +
                " nzp_counter  integer, " +
                " nzp_serv     integer, " +
                " is_actual    integer default 0, " + 
                " dat_uchet    date, " + 
                " val_cnt      float default 0, " +
                " dat_uchet_pred    date, " +
                " val_cnt_pred      float default 0, " +
                " ngp_cnt      " + DBManager.sDecimalType + "(14,7) default 0," +
                " ngp_lift     " + DBManager.sDecimalType + "(14,7) default 0, " +
                " ist          integer default 0, " +
                " dat_when     date, " + 
                " nzp_user     integer, " +
                " changed_by   varchar(200), " +
                " is_uchet_ls  integer, " + 
                " is_doit      integer, " + 
                " name_uchet   varchar(60)," + 
                " max_rashod   varchar(100), " +
                " control_comment varchar(250), " +
                " is_new       integer default 0" +
                ")";

            ExecSQLWE(_connDb, sql);
        }

        /// <summary>
        /// Получить приборы учета
        /// </summary>
        /// <returns></returns>
        private int GetCounters()
        {
            string sql = " insert into " + temp_counter + " (nzp, nzp_counter, nzp_cnt, nzp_serv, num_cnt, nzp_cnttype," +
                    " dat_prov, dat_provnext, dat_oblom, dat_poch, dat_close, dat_block, blocked_by, " +
                    " cnt_stage, mmnog, name_type, comment) " + 
                " Select b.nzp, b.nzp_counter, b.nzp_cnt, b.nzp_serv, b.num_cnt, b.nzp_cnttype, " +
                    " b.dat_prov, b.dat_provnext, b.dat_oblom, b.dat_poch, b.dat_close, b.dat_block, u.comment as blocked_by, " +
                    " sc.cnt_stage, sc.mmnog, sc.name_type, b.comment " + 
                " From " +
                    cur_pref + "_kernel" + DBManager.tableDelimiter + "s_counttypes sc, " + 
                        counters_spis + " b " +
                        " left outer join " + users + " u on b.user_block = u.nzp_user " + 
                 " Where b.nzp_cnttype = sc.nzp_cnttype " + WhereCounter();
            ExecSQLWE(_connDb, sql);

            ExecSQLWE(_connDb, "create index ix_" + temp_counter + "_nzp_counter on " + temp_counter + "(nzp_counter)");
            ExecSQLWE(_connDb, "create index ix_" + temp_counter + "_nzp_cnt on " + temp_counter + "(nzp_cnt)");
            ExecSQLWE(_connDb, DBManager.sUpdStat + " " + temp_counter);
            
            // получить признак блокировки
            sql = " Update " + temp_counter + " b set blocked = 1 Where " +
                " exists (select 1 From " + counters_spis + " css " +
                " where css.nzp_counter = b.nzp_counter " +
                    " and css.user_block is not null and css.user_block <> " + _nzpUser +
                    " and css.dat_block  is not null and (" + interval + ") < css.dat_block) ";
            ExecSQLWE(_connDb, sql);
            
            // единица измерения
            sql = " Update " + temp_counter + " b set nzp_measure = " +
                DBManager.sNvlWord + "((case when b.nzp_cnt in (15,16) " +
                    " then (select max(cntdop.nzp_measure) from " + s_countsdop + " cntdop where b.nzp_cnt = cntdop.nzp_cnt) " +
                    " else (select max(cnt.nzp_measure)    from " + s_counts + "    cnt    where b.nzp_cnt = cnt.nzp_cnt) end), 0) ";
            ExecSQLWE(_connDb, sql);
            
            sql = " Update " + temp_counter + " b set " +
               " measure = " + DBManager.sNvlWord + "((select max(m.measure) from " + cur_pref + "_kernel" + DBManager.tableDelimiter + "s_measure m where b.nzp_measure = m.nzp_measure), '') ";
            ExecSQLWE(_connDb, sql);
            
            // услуга
            sql = " Update " + temp_counter + " b set service = " +
                DBManager.sNvlWord + "((case when b.nzp_cnt in (15,16) " +
                    " then (select max(cntdop.name) from " + s_countsdop + " cntdop where b.nzp_cnt = cntdop.nzp_cnt) " +
                    " else (select max(cnt.name)    from " + s_counts + "    cnt    where b.nzp_cnt = cnt.nzp_cnt) end), '') ";
            ExecSQLWE(_connDb, sql);
            
            sql = " Update " + temp_counter + " set service = service || ' (' || measure || ')' where measure <> '' ";
            ExecSQLWE(_connDb, sql);

            // получить количество ПУ
            Returns ret = new Returns(true);
            return Convert.ToInt32(ExecScalar(_connDb, "select count(*) from " + temp_counter, out ret, true));
        }

        /// <summary>
        /// заблокировать ПУ
        /// </summary>
        protected abstract void BlockCounters();

        /// <summary>
        /// Получить параметры ПУ, норматив, расход к оплате, плановый расход
        /// </summary>
        private void GetCounterPrms()
        {
            // получить параметры ПУ
            DateTime dat = new DateTime(_calcMonth.year_, _calcMonth.month_, 1);

            // плановый (средний) расход
            string sql = "Update " + temp_counter + " b set " +
                " plan_rashod = (select a.val_prm from " + prm_17  + " a " +
                    " where a.nzp_prm = 979 " +
                    " and a.nzp = b.nzp_counter " +
                    " and is_actual <> 100 " +
                    " and " + Utils.EStrNull(dat.ToShortDateString()) + " between a.dat_s and a.dat_po)";
            ExecSQLWE(_connDb, sql);
            
            if (_finder.nzp_type == (int)CounterKinds.Kvar)
            {
                // вид помещения
                sql = " Update " + temp_counter + " b Set " + 
                    " counter_place = (select max(y.name_y) " + 
                    " from " + prm_17 + " p, " + 
                        cur_pref + "_kernel" + DBManager.tableDelimiter + "res_y y " +
                    " Where b.nzp_counter = p.nzp " + 
                        " and p.val_prm" + DBManager.sConvToInt + " = y.nzp_y " +
                        " and p.nzp_prm = 974 " + 
                        " and p.is_actual = 1 " + 
                        " and " + Utils.EStrNull(dat.ToShortDateString()) + " between p.dat_s and p.dat_po " +
                        " and y.nzp_res = 9990)";
                ExecSQLWE(_connDb, sql);

                // норматив
                string table = cur_pref + "_charge_" + (_calcMonth.year_ % 100).ToString("00") + DBManager.tableDelimiter + "counters_" + _calcMonth.month_.ToString("00");
                if (TempTableInWebCashe(_connDb, table))
                {
                    sql = "Update " + temp_counter + " b set " + 
                        " normativ = (select max(val1) from " + table + " a " + 
                        " Where a.stek = 30 " + 
                            " and a.nzp_type = " + _finder.nzp_type + 
                            " and a.nzp_serv = b.nzp_serv " + 
                            " and a.nzp_kvar = b.nzp)";
                    ExecSQLWE(_connDb, sql);
                }

                // расход к оплате
                table = cur_pref + "_charge_" + (_calcMonth.year_ % 100).ToString("00") + DBManager.tableDelimiter + "calc_gku_" + _calcMonth.month_.ToString("00");
                if (TempTableInWebCashe(_connDb, table))
                {
                    sql = "Update " + temp_counter + " b set " +
                        " rashod_k_opl = (select max(rashod) from " + table + " a " + 
                        " Where a.nzp_serv = b.nzp_serv " + 
                            " and a.nzp_kvar = b.nzp)";
                   ExecSQLWE(_connDb, sql);
                }
            }
        }

        /// <summary>
        /// Получить значения ПУ
        /// </summary>
        protected abstract void GetCntValue();

        /// <summary>
        /// определить предыдущие показания
        /// </summary>
        private void DefinePreviouCntVal()
        {
            string sql = "";
            
            // получить предыдущие значения
            ExecSQL(_connDb, "drop table " + temp_prev_val, false);
            sql = " create temp table " + temp_prev_val + " (" +
                " nzp_counter    integer, " +
                " dat_uchet      date, " +
                " val_cnt        float, " +
                " is_actual      integer " +
                ")";
            ExecSQLWE(_connDb, sql);

            sql = "insert into " + temp_prev_val + " (nzp_counter, dat_uchet, val_cnt, is_actual) " +
                " select nzp_counter, dat_uchet, val_cnt, is_actual from " + temp_val;
            ExecSQLWE(_connDb, sql);

            // дата предыдущего показания
            sql = " update " + temp_val + " t set " +
                 " dat_uchet_pred = (case when t.is_actual <> 100 " +
                 " then (select max(b.dat_uchet) from " + temp_prev_val + " b  Where b.nzp_counter = t.nzp_counter and b.dat_uchet < t.dat_uchet and b.is_actual <> 100) " +
                 " else (select max(b.dat_uchet) from " + temp_prev_val + " b  Where b.nzp_counter = t.nzp_counter and b.dat_uchet < t.dat_uchet) end)";
            ExecSQLWE(_connDb, sql);

            // предыдущее показание
            sql = " update " + temp_val + " t set " +
                 " val_cnt_pred = (case when t.is_actual <> 100 " +
                 " then (select max(b.val_cnt) from " + temp_prev_val + " b  Where b.nzp_counter = t.nzp_counter and b.dat_uchet = t.dat_uchet_pred and t.is_actual <> 100) " +
                 " else (select max(b.val_cnt) from " + temp_prev_val + " b  Where b.nzp_counter = t.nzp_counter and b.dat_uchet = t.dat_uchet_pred) end)";
            ExecSQLWE(_connDb, sql); 
        }

        /// <summary>
        /// коментарии к контрольным значениям
        /// </summary>
        private void GetCntValueAddInfo()
        {
            string sql = "";

            // комментарии к контрольным значениям
            if (_finder.nzp_type == (int)CounterKinds.Kvar || _finder.nzp_type == (int)CounterKinds.Communal)
            {
                sql = " Update " + temp_val + " a Set " +
                    " control_comment = (select max(comment) " +
                    " from " + cur_pref + "_data" + DBManager.tableDelimiter + "counters_comment c " +
                    " where a.nzp_cv = c.nzp_cr " +
                        " and c.nzp_cnttype = " + _finder.nzp_type +
                        " and c.is_actual = 1) " +
                    " Where a.ist = " + (int)CounterVal.Ist.Controller;
                ExecSQLWE(_connDb, sql);
            }

            // пользователь, который менял показание
            sql = " Update " + temp_val + " a Set changed_by = (select max(comment) from " + users + " u where a.nzp_user = u.nzp_user)";
            ExecSQLWE(_connDb, sql);

            // проставить услугу, чтобы определить максимальный расход
            sql = " Update " + temp_val + " a Set " +
                " nzp_serv = (select max(nzp_serv) from " + temp_counter + " b " +
                " Where a.nzp_counter = b.nzp_counter) ";
            ExecSQLWE(_connDb, sql);

            // максимальный расход
            sql = " Update " + temp_val + " a Set " +
                 " max_rashod = (case " +
                 " when a.nzp_serv = 9 " +
                     " then (select max(val_prm) from " + cur_pref + "_data" + DBManager.tableDelimiter + "prm_10 p " + 
                     " Where p.nzp_prm = 2082 and a.dat_uchet between p.dat_s and p.dat_po and p.is_actual <> 100) " +
                 " when a.nzp_serv = 6 " +
                     " then (select max(val_prm) from " + cur_pref + "_data" + DBManager.tableDelimiter + "prm_10 p " + 
                     " Where p.nzp_prm = 2083 and a.dat_uchet between p.dat_s and p.dat_po and p.is_actual <> 100) " +
                 " else '' end)";
            ExecSQLWE(_connDb, sql);
        }

        /// <summary>
        /// Получить данные
        /// </summary>
        private void GetData()
        {
            string sql = " select * from " + temp_counter + " b, " + temp_val + " a " + 
                " Where a.nzp_counter = b.nzp_counter " +
                " order by b.service, b.num_cnt, b.name_type, b.nzp_cnttype, b.nzp_counter, a.ordering, a.dat_uchet desc, a.val_cnt desc, a.is_actual ";

            Returns ret = ExecRead(_connDb, out reader, sql.ToString(), true);
            if (!ret.result) throw new Exception(ret.text);
             
            int i = 0;
            long nzp_counter = Constants._ZERO_;
            long cur_nzp_counter = 0;
            int blocked = 0;
            string blocked_by = "";
            
            Spis = new List<CounterValLight>();
            while (reader.Read())
            {
                CounterValLight zap = new CounterValLight();

                zap.nzp_counter = Convert.ToInt32(reader["nzp_counter"]);
                cur_nzp_counter = zap.nzp_counter;

                zap.nzp_cv = Convert.ToInt32(reader["nzp_cv"]);
                blocked = Convert.ToInt32(reader["blocked"]);

                blocked_by = "";
                if (blocked > 0)
                {
                    blocked_by = "";

                    if (blocked > 0)
                    {
                        if (reader["blocked_by"] != DBNull.Value)
                        {
                            blocked_by = ((string)reader["blocked_by"]).Trim();
                        }

                        if (reader["dat_block"] != DBNull.Value)
                        {
                            blocked_by += " " + Convert.ToDateTime(reader["dat_block"]).ToString("dd.MM.yyyy в HH:mm");
                        }

                        zap.block = "Прибор учета заблокирован пользователем " + blocked_by + ". Редактировать данные запрещено.";
                    }
                }
                
                if (cur_nzp_counter != nzp_counter)
                {
                    nzp_counter = cur_nzp_counter; 
                    GetCounterData(ref zap);
                }

                GetCntValData(ref zap, blocked);

                i++;
                zap.num = i.ToString();
                Spis.Add(zap);
            }

            ExecSQL(_connDb, "drop table " + temp_val, false);
            ExecSQL(_connDb, "drop table " + temp_counter, false);
            ExecSQL(_connDb, "drop table " + temp_prev_val, false);
        }

        /// <summary>
        /// данные прибора учета
        /// </summary>
        /// <param name="zap"></param>
        private void GetCounterData(ref CounterValLight zap)
        {
            if (reader["dat_close"] != DBNull.Value) zap.dat_close = String.Format("{0:dd.MM.yyyy}", reader["dat_close"]);
            if (reader["nzp_serv"] != DBNull.Value) zap.nzp_serv = Convert.ToInt32(reader["nzp_serv"]);
            if (reader["service"] != DBNull.Value) zap.service = Convert.ToString(reader["service"]).Trim();

            if (reader["num_cnt"] != DBNull.Value) zap.num_cnt = Convert.ToString(reader["num_cnt"]).Trim();
            if (reader["dat_prov"] != DBNull.Value) zap.dat_prov = String.Format("{0:dd.MM.yyyy}", reader["dat_prov"]);
            if (reader["dat_provnext"] != DBNull.Value) zap.dat_provnext = String.Format("{0:dd.MM.yyyy}", reader["dat_provnext"]);
            if (reader["dat_oblom"] != DBNull.Value) zap.dat_oblom = String.Format("{0:dd.MM.yyyy}", reader["dat_oblom"]);
            if (reader["dat_poch"] != DBNull.Value) zap.dat_poch = String.Format("{0:dd.MM.yyyy}", reader["dat_poch"]);
            if (reader["name_type"] != DBNull.Value) zap.name_type = Convert.ToString(reader["name_type"]).Trim();
            if (reader["comment"] != DBNull.Value) zap.comment = Convert.ToString(reader["comment"]).Trim();
            if (reader["counter_place"] != DBNull.Value)
            {
                if (zap.comment == "") zap.comment += " Вид помещения для ПУ: " + Convert.ToString(reader["counter_place"]).Trim();
                else zap.comment += " (Вид помещения для ПУ: " + Convert.ToString(reader["counter_place"]).Trim() + ")";
            }

            zap.plan_rashod = "-";
            if (reader["plan_rashod"] != DBNull.Value) zap.plan_rashod = Convert.ToString(reader["plan_rashod"]).Trim();
            if (reader["normativ"] != DBNull.Value) zap.normativ = Convert.ToDecimal(reader["normativ"]).ToString("0.00");
            if (reader["rashod_k_opl"] != DBNull.Value) zap.rashod_k_opl = Convert.ToDecimal(reader["rashod_k_opl"]).ToString("0.0000");
        }

        /// <summary>
        /// показания
        /// </summary>
        /// <param name="zap"></param>
        /// <param name="blocked"></param>
        private void GetCntValData(ref CounterValLight zap, int blocked)
        {
            zap.nzp_type = _finder.nzp_type;
            zap.cnt_type = CounterKind.GetKindNameById(zap.nzp_type);
            
            if (reader["dat_close"] != DBNull.Value) zap.dat_close = String.Format("{0:dd.MM.yyyy}", reader["dat_close"]);
            
            if (reader["ngp_cnt"] != DBNull.Value) zap.ngp_cnt = Convert.ToDecimal(reader["ngp_cnt"]);
            if (reader["ngp_lift"] != DBNull.Value) zap.ngp_lift = Convert.ToDecimal(reader["ngp_lift"]);
            if (reader["dat_uchet"] != DBNull.Value) zap.dat_uchet = String.Format("{0:dd.MM.yyyy}", reader["dat_uchet"]);
            if (reader["dat_uchet_pred"] != DBNull.Value) zap.dat_uchet_pred = String.Format("{0:dd.MM.yyyy}", reader["dat_uchet_pred"]);
            if (reader["val_cnt_pred"] != DBNull.Value)
            {
                zap.val_cnt_pred = Convert.ToDecimal(reader["val_cnt_pred"]);
                zap.val_cnt_pred_s = Convert.ToString(reader["val_cnt_pred"]).Trim();
            }

            if (reader["cnt_stage"] != DBNull.Value) zap.cnt_stage = Convert.ToInt32(reader["cnt_stage"]);
            if (reader["mmnog"] != DBNull.Value) zap.mmnog = Convert.ToDecimal(reader["mmnog"]);

            if (reader["is_doit"] != DBNull.Value) zap.is_doit = Convert.ToInt32(reader["is_doit"]);
            if (reader["ist"] != DBNull.Value) zap.ist = Convert.ToInt32(reader["ist"]);
            if (reader["name_uchet"] != DBNull.Value) zap.name_uchet = Convert.ToString(reader["name_uchet"]).Trim();
            if (reader["is_uchet_ls"] != DBNull.Value) zap.is_uchet_ls = Convert.ToInt32(reader["is_uchet_ls"]);
            if (reader["is_actual"] != DBNull.Value) zap.is_actual = Convert.ToInt32(reader["is_actual"]);
            if (reader["dat_when"] == DBNull.Value) zap.dat_when = "";
            else
            {
                zap.dat_when = String.Format("{0:dd.MM.yyyy}", reader["dat_when"]);
                if (reader["changed_by"] != DBNull.Value)
                {
                    zap.dat_when += " (" + Convert.ToString(reader["changed_by"]).Trim() + ")";
                }
            }

            if (reader["max_rashod"] != DBNull.Value)
            {
                try
                { zap.max_rashod = Convert.ToDouble(reader["max_rashod"]); }
                catch
                { zap.max_rashod = 0; }
            }

            if (reader["ordering"] != DBNull.Value) zap.ordering = Convert.ToInt32(reader["ordering"]);
            if (zap.ordering == 1 || zap.ordering == -1) zap.is_editable = (blocked == 0);
            if (reader["is_new"] != DBNull.Value) zap.is_new = Convert.ToInt32(reader["is_new"]);
            
            // строки под новые показания
            if (zap.is_new == 1)
            {
                zap.val_cnt = 0;
                zap.val_cnt_s = "";
            }
            else
            {
                if (reader["val_cnt"] != DBNull.Value)
                {
                    zap.val_cnt = Convert.ToDecimal(reader["val_cnt"]);
                    zap.val_cnt_s = Convert.ToString(reader["val_cnt"]).Trim();
                }
            }

            if (zap.is_new != 1)
            {
                decimal mmnog = zap.mmnog;
                zap.mmnog = 1;
                zap.rashod_without_koef = zap.calculatedRashod.ToString();
                zap.mmnog = mmnog;
                zap.rashod = zap.calculatedRashod.ToString();
            }
            else
            {
                if (zap.ordering == -1)
                {
                    zap.dat_uchet = "";
                }
            }

            if (reader["control_comment"] != DBNull.Value) zap.comment_new = Convert.ToString(reader["control_comment"]).Trim();
        }
    }
}