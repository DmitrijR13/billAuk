

using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;

namespace Bars.KP50.Faktura.Source.Base
{
    /// <summary>
    /// Счетчики
    /// </summary>
    public class FakturaCounters
    {
        public int NzpServ;//Код услуги
        public int NzpCounter;//Код счетчика
        public int NzpCnttype;//Код типа счетчика
        public string ServiceName;//Наименование услуги
        public string ServiceSmall;//Наименование услуги сокращенное
        public string NumCounter;//Заводской номер счетчика
        public string Place;//Место подключения счетчика в квартире
        public decimal Value;//Показание счетчика
        public DateTime DatUchet;//Дата показания счетчика
        public decimal ValuePred;//Предыдущее показание счетчика
        public DateTime DatUchetPred;//Дата предыдущего показания счетчика
        public int CntStage; //Разрядность счетчика
        public decimal Mmnog;//Масштабный множитель
        public string DatProv;//Дата поверки счетчика
        public string DatProvPred;//Предыдущая дата поверки счетчика
        public bool IsGkal;//Признак Гигакаллорного счетчика
        public string Measure;//Единица измерения

        public int IsProv(DateTime d)
        {
            if (String.IsNullOrEmpty(DatProv)) return 1;
            return DateTime.Parse(DatProv) > d ? 1 : 0;
        }

         public FakturaCounters()
         {
             NzpServ = 0;
             NzpCounter = 0; 
             ServiceName = String.Empty;
             ServiceSmall = String.Empty;
             NumCounter = String.Empty;
             Place = String.Empty;
             Value = 0;
             DatUchet = DateTime.Now;
             ValuePred = 0;
             DatUchetPred = DateTime.Now;
             CntStage = 0;
             Mmnog = 0;
             DatProv = String.Empty;
             IsGkal = false;
             Measure = String.Empty;

         }

    }

    public class DbFakturaCounters 
    {
        /// <summary>
        /// Текущее подключение к баще данных
        /// </summary>
        private readonly IDbConnection _connection;

        /// <summary>
        /// Ссылка на системную схему/базу данных
        /// </summary>
        private string _baseKernel;
        /// <summary>
        /// Ссылка на схему/базу с показаниями счетчиков
        /// </summary>
        private string _baseData;
        /// <summary>
        /// Ссылка на схему/базу с учтенными показаниями счетчиков
        /// </summary>
        private string _baseCharge;


        /// <summary>
        /// Рассчетный месяц, для которого выбираются счетчики
        /// </summary>
        private readonly int _month;

        /// <summary>
        /// Расчетный год для которого выбираются счетчики
        /// </summary>
        private readonly int _year;

        /// <summary>
        /// Список счетчиков по услугам по ЛС
        /// </summary>
        public List<FakturaCounters> ListCounters;

        /// <summary>
        /// Список счетчиков по услугам по дому
        /// </summary>
        public List<FakturaCounters> ListDomCounters;

        private readonly StringBuilder _sql;
        private MyDataReader _reader;

        private DateTime _dateMonth;

        private string _pref;
        public string Pref
        {
            get
            {
                return _pref;
            }
            set
            {
                _baseKernel = value + DBManager.sKernelAliasRest;
                _baseData = value + DBManager.sDataAliasRest;
                _baseCharge = value + "_charge_" + (_year - 2000).ToString("00") + DBManager.tableDelimiter;
                _pref = value;
            }
        }

        private string _pkod; 

        public DbFakturaCounters(IDbConnection connDb, int month, int year)
        {
            _connection = connDb;
           
            _month = month;
            _year = year;
            ListCounters = new List<FakturaCounters>();
            ListDomCounters = new List<FakturaCounters>();
            _sql = new StringBuilder();
            _dateMonth = new DateTime(_year, _month, 1);
        }


        /// <summary>
        /// Возвращает список счетчиков по введенным показаниям
        /// с просмотром периода на 1 год назад
        /// </summary>
        /// <param name="apref"></param>
        /// <param name="nzpKvar"></param>
        /// <param name="apkod"></param>
        /// <returns></returns>
        public List<FakturaCounters> LoadSingeLsCounters(string apref, int nzpKvar, string apkod)
        {
            Pref = apref;
            _pkod = apkod;
            int numLs = -1;
            DateTime firstDayNextMonth = new DateTime(_year, _month, 1).AddMonths(1);
            
            _sql.Remove(0, _sql.Length);
            _sql.Append(" SELECT service_name as service, a.nzp_serv, sc.cnt_stage,");
            _sql.Append(" a.nzp_cnttype, sc.formula, a.num_cnt, a.dat_uchet, a.val_cnt, ");
            _sql.Append(" service_small, name_y, cs.dat_prov, cs.dat_provnext,a.num_ls, cs.nzp_counter ");
            _sql.Append(" FROM " + _baseKernel + "services s, ");
            _sql.Append(_baseData + "counters_spis cs,");
            _sql.Append(_baseKernel + "s_counttypes sc, ");
            _sql.Append(_baseData + "counters a left join ");
            _sql.Append("  " + _baseData + "prm_17 p on  p.is_actual=1 ");
            _sql.Append("                  AND p.dat_s<= " + DBManager.sCurDate);
            _sql.Append("                    AND p.dat_po>= " + DBManager.sCurDate);
            _sql.Append("                    AND a.nzp_counter=p.nzp ");
            _sql.Append("                    AND p.nzp_prm=974 ");
            _sql.Append("  left join " + _baseKernel + "res_y v on p.val_prm" + DBManager.sConvToNum + "=v.nzp_y ");
            _sql.Append("                              AND v.nzp_res=9990  ");
            _sql.Append("          Where a.nzp_serv=s.nzp_serv ");
            _sql.Append("                 AND cs.nzp_counter = a.nzp_counter");
            _sql.Append("                 AND a.nzp_cnttype = sc.nzp_cnttype ");
            _sql.Append("                 AND a.dat_close is null ");
            _sql.Append("                 AND a.is_actual = 1 ");
            _sql.Append("                 AND a.nzp_kvar = " + nzpKvar);
            _sql.Append("                 AND a.dat_uchet>=Date('01.01." + (_year - 1) + "') ");
            _sql.Append("                 AND a.dat_uchet<='" + firstDayNextMonth.ToShortDateString() + "'");
            _sql.Append("                 AND a.dat_uchet=( ");
            _sql.Append("                        SELECT max(dat_uchet) ");
            _sql.Append("                        FROM " + _baseData + "counters c ");
            _sql.Append("              Where a.nzp_counter = c.nzp_counter ");
            _sql.Append("                AND c.dat_uchet <= '" + firstDayNextMonth.ToShortDateString() + "'");
            _sql.Append("                AND c.is_actual = 1  ) ");
            _sql.Append("            AND 0=( ");
            _sql.Append("              SELECT count(*) FROM " + _baseData + "counters_spis d                                               ");
            _sql.Append("              Where a.nzp_counter=d.nzp_counter                                                                     ");
            _sql.Append("                     AND d.is_actual = 1 ");
            _sql.Append("                     AND d.dat_close is not null)"+
                " order by 2, num_cnt, nzp_counter, dat_uchet ");
            try
            {
                DBManager.ExecRead(_connection, out _reader, _sql.ToString(), true);
                while (_reader.Read())
                {
                    var counter = new FakturaCounters
                    {
                        NzpServ = Int32.Parse(_reader["nzp_serv"].ToString()),
                        NzpCounter = Int32.Parse(_reader["nzp_counter"].ToString()),
                        ServiceName = _reader["service"].ToString(),
                        Value = Decimal.Parse(_reader["val_cnt"].ToString()),
                        DatUchet = (DateTime) _reader["dat_uchet"]
                    };
                    counter.ValuePred = counter.Value;
                    counter.Place = _reader["name_y"] != DBNull.Value ? _reader["name_y"].ToString().Trim() : "";
                    counter.NumCounter = _reader["num_cnt"].ToString().Trim();
                    counter.NzpCnttype = Int32.Parse(_reader["nzp_cnttype"].ToString().Trim());
                    counter.DatUchetPred = counter.DatUchet;
                    counter.CntStage = Int32.Parse(_reader["cnt_stage"].ToString());
                    if (_reader["formula"] != DBNull.Value)
                        Decimal.TryParse(_reader["formula"].ToString(), out counter.Mmnog);
                    counter.Mmnog = counter.Mmnog ==0 ? 1 : counter.Mmnog;

                    if (_reader["dat_provnext"] != DBNull.Value)
                    {
                        counter.DatProv = ((DateTime)_reader["dat_provnext"]).ToShortDateString();
                    }
                    else
                    {
                        counter.DatProv = _reader["dat_prov"] != DBNull.Value ? ((DateTime)_reader["dat_prov"]).ToShortDateString() : "";
                    }
                    numLs = Int32.Parse(_reader["num_ls"].ToString());
                    ListCounters.Add(counter);
                }

            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выборки счетчиков /n " + _sql +
                    " /n" + ex.Message, MonitorLog.typelog.Error, 20, 201, true);
            }
            finally
            {
                if (_reader != null)
                    _reader.Close();
            }

            if (numLs > -1)
            {
                DeleteCountersOrd(numLs);
                SaveCountersOrd(numLs);
            }

            return ListCounters;
        }


        /// <summary>
        /// Возвращает список счетчиков по введенным показаниям
        /// с просмотром периода на 1 год назад
        /// </summary>
        /// <param name="apref"></param>
        /// <param name="nzpKvar"></param>
        /// <param name="apkod"></param>
        /// <returns></returns>
        public List<FakturaCounters> LoadSingeOldLsCounters(string apref, int nzpKvar, string apkod)
        {
            Pref = apref;
            _pkod = apkod;
            int numLs = -1;
          
            
            _sql.Remove(0, _sql.Length);
            _sql.Append(" SELECT service_name as service, a.nzp_serv, sc.cnt_stage,");
            _sql.Append(" a.nzp_cnttype, sc.formula, a.num_cnt, a.dat_uchet, a.val_cnt, ");
            _sql.Append(" service_small, name_y, cs.dat_prov, cs.dat_provnext,a.num_ls, cs.nzp_counter ");
            _sql.Append(" FROM " + _baseKernel + "services s, ");
            _sql.Append(_baseData + "counters_spis cs,");
            _sql.Append(_baseKernel + "s_counttypes sc, ");
            _sql.Append(_baseCharge + "counters_ord a left join ");
            _sql.Append("  " + _baseData + "prm_17 p on  p.is_actual=1 ");
            _sql.Append("                  AND p.dat_s<= " + DBManager.sCurDate);
            _sql.Append("                    AND p.dat_po>= " + DBManager.sCurDate);
            _sql.Append("                    AND a.nzp_counter=p.nzp ");
            _sql.Append("                    AND p.nzp_prm=974 ");
            _sql.Append("  left join " + _baseKernel + "res_y v on p.val_prm" + DBManager.sConvToNum + "=v.nzp_y ");
            _sql.Append("                              AND v.nzp_res=9990  ");
            _sql.Append("          Where a.nzp_serv=s.nzp_serv ");
            _sql.Append("                 AND cs.nzp_counter = a.nzp_counter");
            _sql.Append("                 AND a.nzp_cnttype = sc.nzp_cnttype ");
            _sql.Append("                 AND cs.nzp = " + nzpKvar);
            _sql.Append("                 AND cs.nzp_type = 3");
            _sql.Append("                 AND a.dat_month='01." + _month + "." + _year + "'" +
                        " order by nzp_serv, num_cnt, nzp_counter, dat_uchet ");
            try
            {
                DBManager.ExecRead(_connection, out _reader, _sql.ToString(), true);
                while (_reader.Read())
                {
                    var counter = new FakturaCounters
                    {
                        NzpServ = Int32.Parse(_reader["nzp_serv"].ToString()),
                        NzpCounter = Int32.Parse(_reader["nzp_counter"].ToString()),
                        ServiceName = _reader["service"].ToString(),
                        Value = Decimal.Parse(_reader["val_cnt"].ToString()),
                        DatUchet = (DateTime) _reader["dat_uchet"]
                    };
                    counter.ValuePred = counter.Value;
                    counter.Place = _reader["name_y"] != DBNull.Value ? _reader["name_y"].ToString().Trim() : "";
                    counter.NumCounter = _reader["num_cnt"].ToString().Trim();
                    counter.NzpCnttype = Int32.Parse(_reader["nzp_cnttype"].ToString().Trim());
                    counter.DatUchetPred = counter.DatUchet;
                    counter.CntStage = Int32.Parse(_reader["cnt_stage"].ToString());
                    if (_reader["formula"] != DBNull.Value)
                        Decimal.TryParse(_reader["formula"].ToString(), out counter.Mmnog);
                    counter.Mmnog = counter.Mmnog == 0 ? 1 : counter.Mmnog;

                    if (_reader["dat_provnext"] != DBNull.Value)
                    {
                        counter.DatProv = ((DateTime)_reader["dat_provnext"]).ToShortDateString();
                    }
                    else
                    {
                        counter.DatProv = _reader["dat_prov"] != DBNull.Value ? ((DateTime)_reader["dat_prov"]).ToShortDateString() : "";
                    }
                    numLs = Int32.Parse(_reader["num_ls"].ToString());
                    ListCounters.Add(counter);
                }

            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выборки счетчиков /n " + _sql +
                    " /n" + ex.Message, MonitorLog.typelog.Error, 20, 201, true);
            }
            finally
            {
                if (_reader != null)
                    _reader.Close();
            }

            if (numLs > -1)
            {
                DeleteCountersOrd(numLs);
                SaveCountersOrd(numLs);
            }

            return ListCounters;
        }

        /// <summary>
        /// Возвращает список счетчиков по введенным показаниям
        /// с просмотром периода на 1 год назад
        /// </summary>
        /// <param name="apref"></param>
        /// <param name="nzpKvar"></param>
        /// <param name="apkod"></param>
        /// <returns></returns>
        public List<FakturaCounters> LoadDoubleLsCounters(string apref, int nzpKvar, string apkod)
        {
            Pref = apref;
            _pkod = apkod;
            int numLs = -1;
            DateTime firstDayNextMonth = new DateTime(_year, _month, 01).AddMonths(1);
            
            _sql.Remove(0, _sql.Length);
            _sql.Append(" Select s.ordering, s.service_small, s.service_name as service, a.nzp_serv, cs.num_cnt, ");
            _sql.Append("        a.nzp_cnttype, a.dat_uchet, a.val_cnt, ");
            _sql.Append("         b.dat_uchet as dat_uchet2, b.val_cnt as val_cnt2, ");
            _sql.Append("        sc.cnt_stage, formula, name_y, cs.dat_prov, cs.dat_provnext, a.num_ls, cs.nzp_counter  ");
            _sql.Append(" From " + _baseKernel + "services s, ");
            _sql.Append("                  " + _baseKernel + "s_counttypes sc, ");
            _sql.Append("                 " + _baseData + "counters a left join ");
            _sql.Append("                 " + _baseData + "counters b on a.nzp_counter=b.nzp_counter ");
            _sql.Append("                 AND b.dat_uchet<a.dat_uchet ");
            _sql.Append("                 AND a.is_actual=b.is_actual ");
            _sql.Append("                 AND b.dat_uchet=( ");
            _sql.Append("                        Select max(dat_uchet) ");
            _sql.Append("                        From " + _baseData + "counters c ");
            _sql.Append("                        WHERE a.nzp_counter=c.nzp_counter ");
            _sql.Append("                               AND c.dat_uchet<a.dat_uchet ");
            _sql.Append("                               AND c.is_actual = 1 ),  ");
            _sql.Append(_baseData + "counters_spis cs ");
            _sql.Append("                  left join " + _baseData + "prm_17 p ");
            _sql.Append("                  on cs.nzp_counter=p.nzp  ");
            _sql.Append("                        AND p.nzp_prm=974");
            _sql.Append("                        AND p.is_actual=1 ");
            _sql.Append("                        AND p.dat_s<=" + DBManager.sCurDate);
            _sql.Append("                        AND p.dat_po>=" + DBManager.sCurDate);
            _sql.Append("                  left join " + _baseKernel + "res_y v ");
            _sql.Append("                  on p.val_prm" + DBManager.sConvToNum + "=v.nzp_y ");
            _sql.Append("                              AND v.nzp_res=9990  ");
            _sql.Append("          WHERE a.nzp_serv=s.nzp_serv ");
            _sql.Append("                 AND a.nzp_counter=cs.nzp_counter ");
            _sql.Append("                 AND a.nzp_cnttype=sc.nzp_cnttype ");
            _sql.Append("                 AND a.dat_close is null ");
            _sql.Append("                 AND a.is_actual=1 ");
            _sql.Append("                 AND a.nzp_kvar=" + nzpKvar);
            _sql.Append("                 AND a.dat_uchet>=Date('01.01." + (_year - 1) + "') ");
            _sql.Append("                 AND a.dat_uchet<='" + firstDayNextMonth.ToShortDateString() + "'");
            _sql.Append("                 AND a.dat_uchet=( ");
            _sql.Append("              SELECT max(dat_uchet) From " + _baseData + "counters c ");
            _sql.Append("              WHERE a.nzp_counter=c.nzp_counter ");
            _sql.Append("                AND c.dat_uchet<='" + firstDayNextMonth.ToShortDateString() + "'");
            _sql.Append("                AND c.is_actual =  1) ");
            _sql.Append("            AND 0=( ");
            _sql.Append("              Select count(*) From " + _baseData + "counters_spis d                                               ");
            _sql.Append("              WHERE a.nzp_counter=d.nzp_counter                                                                    ");
            _sql.Append("                AND d.is_actual = 1  ");
            _sql.Append("                AND d.dat_close is not null) ");
            _sql.Append("  ORDER BY nzp_serv, num_cnt, nzp_counter, dat_uchet ");
            try
            {
                DBManager.ExecRead(_connection, out _reader, _sql.ToString(), true);
                while (_reader.Read())
                {
                    var counter = new FakturaCounters
                    {
                        NzpServ = Int32.Parse(_reader["nzp_serv"].ToString()),
                        NzpCounter = Int32.Parse(_reader["nzp_counter"].ToString()),
                        ServiceName = _reader["service"].ToString(),
                        ServiceSmall = _reader["service_small"].ToString(),
                        Place = _reader["name_y"] != DBNull.Value ? _reader["name_y"].ToString().Trim() : "",
                        Value = Decimal.Parse(_reader["val_cnt"].ToString()),
                        DatUchet = (DateTime) _reader["dat_uchet"],
                        ValuePred =
                            _reader["val_cnt2"] != DBNull.Value ? Decimal.Parse(_reader["val_cnt2"].ToString()) : 0,
                        NumCounter = _reader["num_cnt"].ToString().Trim(),
                        NzpCnttype = Int32.Parse(_reader["nzp_cnttype"].ToString().Trim()),
                        DatUchetPred = _reader["dat_uchet2"] != DBNull.Value
                            ? (DateTime) _reader["dat_uchet2"]
                            : DateTime.Parse("01.01.1900"),
                        CntStage = Int32.Parse(_reader["cnt_stage"].ToString())
                    };
                    if (_reader["formula"] != DBNull.Value)
                        Decimal.TryParse(_reader["formula"].ToString(), out counter.Mmnog);

                    counter.Mmnog = counter.Mmnog == 0 ? 1 : counter.Mmnog;

                    if (_reader["dat_provnext"] != DBNull.Value)
                        counter.DatProv = ((DateTime)_reader["dat_provnext"]).ToShortDateString();
                    else if (_reader["dat_prov"] != DBNull.Value)
                    {
                        counter.DatProv = ((DateTime)_reader["dat_prov"]).ToShortDateString();
                    }
                    numLs = Int32.Parse(_reader["num_ls"].ToString());
                    ListCounters.Add(counter);
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выборки счетчиков /n " + _sql +
                    " /n" + ex.Message, MonitorLog.typelog.Error, 20, 201, true);
            }
            finally
            {
                if (_reader != null)
                    _reader.Close();
            }

            if (numLs > -1)
            {
                DeleteCountersOrd(numLs);
                SaveCountersOrd(numLs);
            }
            return ListCounters;
        }

        /// <summary>
        /// Возвращает список счетчиков по введенным показаниям
        /// за выбранный месяц
        /// </summary>
        /// <param name="apref"></param>
        /// <param name="nzpKvar"></param>
        /// <param name="apkod"></param>
        /// <returns></returns>
        public List<FakturaCounters> LoadChosenMonthCounters(string apref, int nzpKvar, string apkod)
        {
            Pref = apref;
            _pkod = apkod;
            int numLs = -1;
            DateTime firstDayNextMonth = new DateTime(_year, _month, 01).AddMonths(1);
            try
            {

                _sql.Remove(0, _sql.Length);
                _sql.Append(" CREATE TEMP TABLE t_dates (num_ls int, nzp_serv int, nzp_counter int, prev_date date, cur_date date, prev_val decimal, cur_val decimal) ");
                DBManager.ExecSQL(_connection, _sql.ToString(), true);

                _sql.Remove(0, _sql.Length);
                _sql.Append(" INSERT INTO t_dates(num_ls, nzp_serv, nzp_counter, prev_date, prev_val) " +
                            " SELECT num_ls, nzp_serv, nzp_counter, dat_uchet as prev_date, val_cnt as prev_val " +
                            " FROM " + _baseData + "counters c " +
                            " WHERE c.dat_close IS NULL " +
                            " AND c.is_actual=1 " +
                            " AND c.nzp_kvar = " + nzpKvar +
                            " AND c.dat_uchet = ( " +
                                " SELECT MAX(dat_uchet) " +
                                " FROM " + _baseData + "counters d " +
                                " WHERE c.nzp_counter=d.nzp_counter " +
                                " AND d.is_actual = 1 " +
                                " AND d.dat_uchet <= '" + firstDayNextMonth.ToShortDateString() + "') ");
                DBManager.ExecSQL(_connection, _sql.ToString(), true);

                _sql.Remove(0, _sql.Length);
                _sql.Append(" UPDATE t_dates SET cur_date = ( " +
                            " SELECT MAX(dat_uchet) " +
                            " FROM " + _baseData + "counters d " +
                            " WHERE t_dates.nzp_counter=d.nzp_counter " +
                            " AND d.is_actual = 1 " +
                            " AND d.dat_uchet <= '" + firstDayNextMonth + "' " +
                            " AND d.dat_uchet > '" + firstDayNextMonth.AddMonths(-1).ToShortDateString() + "') ");
                DBManager.ExecSQL(_connection, _sql.ToString(), true);

                _sql.Remove(0, _sql.Length);
                _sql.Append(" UPDATE t_dates SET prev_date = " +
                            " CASE WHEN ( " +
                            " SELECT MAX(dat_uchet) " +
                            " FROM " + _baseData + "counters d " +
                            " WHERE t_dates.nzp_counter=d.nzp_counter " +
                            " AND d.is_actual = 1 " +
                            " AND d.dat_uchet <= '" + firstDayNextMonth.ToShortDateString() + "' " +
                            " AND t_dates.cur_date > d.dat_uchet) <> prev_date " +
                            " THEN ( " +
                            " SELECT MAX(dat_uchet) " +
                            " FROM " + _baseData + "counters d " +
                            " WHERE t_dates.nzp_counter=d.nzp_counter " +
                            " AND d.is_actual = 1 " +
                            " AND d.dat_uchet <= '" + firstDayNextMonth.ToShortDateString() + "' " +
                            " AND t_dates.cur_date > d.dat_uchet) END " +
                            " WHERE cur_date is not null ");
                DBManager.ExecSQL(_connection, _sql.ToString(), true);

                _sql.Remove(0, _sql.Length);
                _sql.Append(" UPDATE t_dates SET prev_val = ( " +
                            " SELECT val_cnt " +
                            " FROM " + _baseData + "counters d " +
                            " WHERE t_dates.nzp_counter = d.nzp_counter " +
                            " AND t_dates.nzp_serv = d.nzp_serv " +
                            " AND t_dates.prev_date = d.dat_uchet) ");
                DBManager.ExecSQL(_connection, _sql.ToString(), true);

                _sql.Remove(0, _sql.Length);
                _sql.Append(" UPDATE t_dates SET cur_val = ( " +
                            " SELECT val_cnt " +
                            " FROM " + _baseData + "counters d " +
                            " WHERE t_dates.nzp_counter = d.nzp_counter " +
                            " AND t_dates.nzp_serv = d.nzp_serv " +
                            " AND t_dates.cur_date = d.dat_uchet) ");
                DBManager.ExecSQL(_connection, _sql.ToString(), true);

                _sql.Remove(0, _sql.Length);
                _sql.Append(" Select s.service_small, s.service_name as service, cs.num_cnt, " +
                            " a.num_ls, a.nzp_serv, a.prev_date, a.cur_date, a.prev_val, a.cur_val " +
                            " FROM t_dates a, " + _baseKernel + "services s, " + _baseData + "counters_spis cs " +
                            " WHERE a.nzp_serv = s.nzp_serv AND a.nzp_counter = cs.nzp_counter ");
                DBManager.ExecRead(_connection, out _reader, _sql.ToString(), true);
                while (_reader.Read())
                {
                    var counter = new FakturaCounters
                    {
                        NzpServ = Int32.Parse(_reader["nzp_serv"].ToString()),
                        ServiceName = _reader["service"].ToString(),
                        ServiceSmall = _reader["service_small"].ToString(),
                        Value = _reader["cur_val"] != DBNull.Value ? Decimal.Parse(_reader["cur_val"].ToString()) : 0,
                        DatUchet = _reader["cur_date"] != DBNull.Value
                            ? (DateTime)_reader["cur_date"] : DateTime.MinValue,
                        ValuePred =
                            _reader["prev_val"] != DBNull.Value ? Decimal.Parse(_reader["prev_val"].ToString()) : 0,
                        DatUchetPred = _reader["prev_date"] != DBNull.Value
                            ? (DateTime)_reader["prev_date"]
                            : DateTime.MinValue,
                        NumCounter = _reader["num_cnt"].ToString().Trim(),
                    };

                    numLs = Int32.Parse(_reader["num_ls"].ToString());
                    ListCounters.Add(counter);
                }

                _sql.Remove(0, _sql.Length);
                _sql.Append(" drop table t_dates ");
                DBManager.ExecSQL(_connection, _sql.ToString(), true);
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выборки счетчиков /n " + _sql +
                    " /n" + ex.Message, MonitorLog.typelog.Error, 20, 201, true);
            }
            finally
            {
                if (_reader != null)
                    _reader.Close();
            }

            if (numLs > -1)
            {
                DeleteCountersOrd(numLs);
                SaveCountersOrd(numLs);
            }
            return ListCounters;
        }

        /// <summary>
        /// Возвращает показания счетчиков учтенные в расчете текущего месяца
        /// </summary>
        /// <param name="apref"></param>
        /// <param name="nzpKvar"></param>
        /// <param name="numLs"></param>
        /// <param name="apkod"></param>
        /// <returns></returns>
        public List<FakturaCounters> LoadCalcLsCounters(string apref, int nzpKvar, int numLs, string apkod)
        {
            Pref = apref;
            _pkod = apkod;
          
            
            _sql.Remove(0, _sql.Length);
            _sql.Append(" Select s.ordering, s.service_name as service, cs.nzp_serv, cs.num_cnt, " +
                       "        cs.nzp_cnttype, d.dat_s, d.val_s, d.dat_po, d.val_po, " +
                       "        sc.cnt_stage, sc.mmnog as formula, v.name_y, cs.dat_prov, cs.dat_provnext, cs.nzp_counter  " +
                       " FROM " + _baseKernel + "services s, " +
                       "        " + _baseKernel + "s_counttypes sc, " +
                       "        " + _baseData + "counters_spis cs  " +
                       "        left join " + _baseData + "prm_17 p on  " +
                       "                    p.is_actual=1 AND p.dat_s<=" + DBManager.sCurDate +
                       "                    AND p.dat_po>=" + DBManager.sCurDate +
                       "                           AND cs.nzp_counter=p.nzp AND p.nzp_prm=974 " +
                       "         left join " + _baseKernel + "res_y v on p.val_prm+0=v.nzp_y " +
                       "                                        AND v.nzp_res=9990 " +
                       "         left join " + _baseCharge+"counters_"+_month.ToString("00") +
                       "         d on cs.nzp_counter=d.nzp_counter " +
                       "                                        AND d.nzp_type=3 and d.stek=1 " +
                       " Where cs.nzp_serv=s.nzp_serv " +
                       "        AND sc.nzp_cnttype=cs.nzp_cnttype " +
                       "        AND cs.dat_close is null " +
                       "        AND cs.nzp = " + nzpKvar +
                       "        AND cs.nzp_type=3 " +
                       "  ORDER BY nzp_serv, num_cnt, nzp_counter ");

            try
            {
                DBManager.ExecRead(_connection, out _reader, _sql.ToString(), true);
                while (_reader.Read())
                {
                    var counter = new FakturaCounters
                    {
                        NzpServ = Int32.Parse(_reader["nzp_serv"].ToString()),
                        NzpCounter = Int32.Parse(_reader["nzp_counter"].ToString()),
                        ServiceName = _reader["service"].ToString(),
                        Place = _reader["name_y"] != DBNull.Value ? _reader["name_y"].ToString().Trim() : "",
                        Value = _reader["val_po"] != DBNull.Value ? Decimal.Parse(_reader["val_po"].ToString()) : 0,
                        DatUchet =
                            _reader["dat_s"] != DBNull.Value
                                ? (DateTime) _reader["dat_po"]
                                : DateTime.Parse("01.01.1900"),
                        ValuePred = _reader["val_s"] != DBNull.Value ? Decimal.Parse(_reader["val_s"].ToString()) : 0,
                        NumCounter = _reader["num_cnt"].ToString().Trim(),
                        NzpCnttype = Int32.Parse(_reader["nzp_cnttype"].ToString().Trim()),
                        DatUchetPred =
                            _reader["dat_s"] != DBNull.Value
                                ? (DateTime) _reader["dat_s"]
                                : DateTime.Parse("01.01.1900"),
                        CntStage = Int32.Parse(_reader["cnt_stage"].ToString()),
                        Mmnog = _reader["formula"] != DBNull.Value ? Decimal.Parse(_reader["formula"].ToString()) : 1,
                        DatProv =
                            _reader["dat_provnext"] != DBNull.Value
                                ? ((DateTime) _reader["dat_provnext"]).ToShortDateString()
                                : ""
                    };
                    if (counter.DatProv == "")
                        counter.DatProv = _reader["dat_prov"] != DBNull.Value ? ((DateTime)_reader["dat_prov"]).ToShortDateString() : "";

                    ListCounters.Add(counter);
                }
                _reader.Close();
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выборки счетчиков /n " + _sql +
                    " /n" + ex.Message, MonitorLog.typelog.Error, 20, 201, true);
            }
            finally
            {
                if (_reader != null)
                    _reader.Close();
            }

            if (numLs > -1)
            {
                DeleteCountersOrd(numLs);
                SaveCountersOrd(numLs);
            }
            return ListCounters;

        }

        /// <summary>
        /// Возвращает список счетчиков по введенным показаниям
        /// с просмотром периода на 1 год назад по дому
        /// </summary>
        /// <returns></returns>
        public List<FakturaCounters> LoadDoubleDomCounters(string apref, int nzpDom)
        {
            Pref = apref;
            DateTime firstDayNextMonth = new DateTime(_year, _month, 01).AddMonths(1);
            DateTime firstDayCurMonth = new DateTime(_year, _month, 01).AddMonths(1);
            
            _sql.Remove(0, _sql.Length);
            _sql.Append(" Select s.ordering, service_name as service, a.nzp_serv, a.num_cnt, ");
            _sql.Append(" a.nzp_cnttype, a.dat_uchet, a.val_cnt, ");
            _sql.Append(" b.num_cnt as num_cnt2, b.dat_uchet as dat_uchet2, ");
            _sql.Append(" b.val_cnt as val_cnt2, sc.cnt_stage, formula, cs.dat_prov, ");
            _sql.Append(" cs.dat_provnext, a.is_gkal, sm.measure, cs.nzp_counter  ");
            _sql.Append(" From " + _baseKernel + "services s, ");
            _sql.Append("                  " + _baseKernel + "s_counttypes sc,  ");
            _sql.Append(_baseData + "counters_spis cs,  ");
            _sql.Append(_baseKernel + "s_counts st,");
            _sql.Append(_baseKernel + "s_measure sm, ");
            _sql.Append("                 " + _baseData + "counters_dom a left join ");
            _sql.Append("                 " + _baseData + "counters_dom b on  ");
            _sql.Append("                    a.nzp_counter=b.nzp_counter ");
            _sql.Append("                AND b.dat_uchet<a.dat_uchet ");
            _sql.Append("                AND a.is_actual=b.is_actual ");
            _sql.Append("                AND b.dat_uchet=( ");
            _sql.Append("              Select max(dat_uchet) From " + _baseData + "counters_dom c ");
            _sql.Append("              Where a.nzp_counter=c.nzp_counter ");
            _sql.Append("                AND c.dat_uchet<a.dat_uchet ");
            _sql.Append("                AND c.is_actual = 1) ");
            _sql.Append("          Where a.nzp_serv=s.nzp_serv AND a.nzp_counter=cs.nzp_counter");
            _sql.Append("            AND cs.nzp_cnt=st.nzp_cnt AND st.nzp_measure=sm.nzp_measure ");
            _sql.Append("            AND a.nzp_cnttype=sc.nzp_cnttype ");
            _sql.Append("            AND a.dat_close is null ");
            _sql.Append("            AND a.is_actual=1 ");
            _sql.Append("            AND a.nzp_dom=" + nzpDom);
            _sql.Append("            AND a.dat_uchet>=Date('" + firstDayCurMonth.ToShortDateString() + "') ");
            _sql.Append("            AND a.dat_uchet<='" + firstDayNextMonth.ToShortDateString() + "'");
            _sql.Append("            AND a.dat_uchet=( ");
            _sql.Append("              Select max(dat_uchet) From " + _baseData + "counters_dom c ");
            _sql.Append("              Where a.nzp_counter=c.nzp_counter ");
            _sql.Append("                AND c.dat_uchet<='" + firstDayNextMonth.ToShortDateString() + "'");
            _sql.Append("                AND c.is_actual = 1 ) ");
            _sql.Append("            AND 0=( ");
            _sql.Append("              Select count(*) From " + _baseData + "counters_spis d                                          ");
            _sql.Append("              Where a.nzp_counter=d.nzp_counter                                                                     ");
            _sql.Append("                AND d.is_actual = 1 AND d.dat_close is not null)                                      ");
            _sql.Append("  ORDER BY ordering,2,4,5 ");
            try
            {
                DBManager.ExecRead(_connection, out _reader, _sql.ToString(), true);
                while (_reader.Read())
                {
                    var counter = new FakturaCounters
                    {
                        NzpServ = Int32.Parse(_reader["nzp_serv"].ToString()),
                        NzpCounter = Int32.Parse(_reader["nzp_counter"].ToString()),
                        Measure = _reader["measure"].ToString().Trim(),
                        ServiceName = _reader["service"].ToString().Trim(),
                        Value = Decimal.Parse(_reader["val_cnt"].ToString()),
                        DatUchet = (DateTime) _reader["dat_uchet"],
                        NzpCnttype = Int32.Parse(_reader["nzp_cnttype"].ToString().Trim()),
                        ValuePred =
                            _reader["val_cnt2"] != DBNull.Value ? Decimal.Parse(_reader["val_cnt2"].ToString()) : 0,
                        NumCounter = _reader["num_cnt"].ToString().Trim(),
                        DatUchetPred = _reader["dat_uchet2"] != DBNull.Value
                            ? (DateTime) _reader["dat_uchet2"]
                            : DateTime.Parse("01.01.1900"),
                        CntStage = Int32.Parse(_reader["cnt_stage"].ToString())
                    };

                    if (_reader["formula"] != DBNull.Value)
                          Decimal.TryParse(_reader["formula"].ToString(), out counter.Mmnog);

                    if (_reader["dat_provnext"] != DBNull.Value)
                        counter.DatProv = ((DateTime)_reader["dat_provnext"]).ToShortDateString();
                    else if (_reader["dat_prov"] != DBNull.Value)
                    {
                        counter.DatProv = ((DateTime)_reader["dat_prov"]).ToShortDateString();
                    }
                    counter.IsGkal = false;

                    if (_reader["is_gkal"] != DBNull.Value)
                        if (_reader["is_gkal"].ToString().Trim() == "1")
                            counter.IsGkal = true;
                    ListDomCounters.Add(counter);
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выборки счетчиков /n " + _sql +
                    " /n" + ex.Message, MonitorLog.typelog.Error, 20, 201, true);
            }
            finally
            {
                if (_reader != null)
                    _reader.Close();
            }
            return ListDomCounters;
        }

        /// <summary>
        /// Возвращает показания счетчиков учтенные в расчете текущего месяца
        /// по дому
        /// </summary>
        /// <param name="apref"></param>
        /// <param name="nzpDom"></param>
        /// <returns></returns>
        public List<FakturaCounters> LoadCalcDomCounters(string apref, int nzpDom)
        {
            Pref = apref;
       
            _sql.Remove(0, _sql.Length);
            _sql.Append(" Select s.ordering, service_name as service, cs.nzp_serv, cs.num_cnt, " +
                                 "        cs.nzp_cnttype, d.dat_s, d.val_s, d.dat_po, " +
                                 "        d.val_po, sc.cnt_stage, sc.mmnog as formula, cs.dat_prov, " +
                                 "        cs.dat_provnext, cs.is_gkal, sm.measure  " +
                                 " From " + _baseKernel + "services s, " +
                                         _baseKernel + "s_counttypes sc,  " +
                                         _baseKernel + "s_counts st, " +
                                         _baseKernel + "s_measure sm, " +
                                         _baseData + "counters_spis cs  " +
                                 " left join " + _baseCharge + "counters_" + _month.ToString("00") + " d " +
                                 " on cs.nzp_counter=d.nzp_counter and d.nzp_type=1 and d.stek = 1 " +
                                 " Where cs.nzp_serv=s.nzp_serv " +
                                 "       AND cs.nzp_cnt=st.nzp_cnt " +
                                 "       AND st.nzp_measure=sm.nzp_measure " +
                                 "       AND cs.nzp_cnttype=sc.nzp_cnttype " +
                                 "       AND cs.dat_close is null " +
                                 "       AND cs.nzp=" + nzpDom +
                                 "       AND cs.nzp_type=1 " +
                                 " ORDER BY ordering,2,4,5 ");

            try
            {
                DBManager.ExecRead(_connection, out _reader, _sql.ToString(), true);
                while (_reader.Read())
                {
                    var counter = new FakturaCounters
                    {
                        NzpServ = Int32.Parse(_reader["nzp_serv"].ToString()),
                        Measure = _reader["measure"].ToString().Trim(),
                        ServiceName = _reader["service"].ToString().Trim(),
                        Value = _reader["val_po"] != DBNull.Value ? Decimal.Parse(_reader["val_po"].ToString()) : 0,
                        DatUchet =
                            _reader["dat_s"] != DBNull.Value
                                ? (DateTime) _reader["dat_po"]
                                : DateTime.Parse("01.01.1900"),
                        ValuePred = _reader["val_s"] != DBNull.Value ? Decimal.Parse(_reader["val_po"].ToString()) : 0,
                        NumCounter = _reader["num_cnt"].ToString().Trim(),
                        NzpCnttype = Int32.Parse(_reader["nzp_cnttype"].ToString().Trim()),
                        DatUchetPred =
                            _reader["dat_s"] != DBNull.Value
                                ? (DateTime) _reader["dat_po"]
                                : DateTime.Parse("01.01.1900"),
                        CntStage = Int32.Parse(_reader["cnt_stage"].ToString()),
                        Mmnog = _reader["formula"] != DBNull.Value ? Decimal.Parse(_reader["formula"].ToString()) : 1,
                        DatProv =
                            _reader["dat_provnext"] != DBNull.Value
                                ? ((DateTime) _reader["dat_provnext"]).ToShortDateString()
                                : ""
                    };
                    if (counter.DatProv == "")
                        counter.DatProv = _reader["dat_prov"] != DBNull.Value ? ((DateTime)_reader["dat_prov"]).ToShortDateString() : "";
                    counter.IsGkal = false;

                    if (_reader["is_gkal"] != DBNull.Value)
                        if (_reader["is_gkal"].ToString().Trim() == "1")
                            counter.IsGkal = true;
                    ListDomCounters.Add(counter);
                }
                _reader.Close();
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выборки счетчиков /n " + _sql +
                    " /n" + ex.Message, MonitorLog.typelog.Error, 20, 201, true);
            }
            finally
            {
                if (_reader != null)
                    _reader.Close();
                
            }
            return ListCounters;

        }

        /// <summary>
        /// Сохраняет счетчики выбранные для счета квитанции в базу
        /// </summary>
        /// <param name="numLs"></param>
        public void SaveCountersOrd(int numLs)
        {
            int i = 1;
            foreach (FakturaCounters fc in ListCounters)
            {
                _sql.Remove(0, _sql.Length);
                _sql.Append(" insert into " + _baseCharge + "counters_ord " +
                                               " (num_ls, pkod, dat_month, cnt_stage, nzp_cnttype, formula, nzp_serv," +
                                               " num_cnt, dat_uchet, val_cnt, order_num, is_prov, dat_prov, dat_provnext, nzp_counter) " +
                                               " values(" + numLs + "," + _pkod + "," +
                                               " '" + _dateMonth.ToShortDateString() + "'," +
                                                fc.CntStage + "," +
                                                fc.NzpCnttype + ",'" +
                                                fc.Mmnog + "'," +
                                                fc.NzpServ + ",'" +
                                                fc.NumCounter + "','" +
                                                fc.DatUchet.ToShortDateString() + "'," +
                                                fc.Value + "," + i +
                                                "," + fc.IsProv(_dateMonth) + "," + (String.IsNullOrEmpty(fc.DatProv) ? "null" : STCLINE.KP50.Global.Utils.EStrNull(fc.DatProv))
                                                + "," + (String.IsNullOrEmpty(fc.DatProvPred) ? "null" : STCLINE.KP50.Global.Utils.EStrNull(fc.DatProvPred)) + "," + fc.NzpCounter + ")");

                
                DBManager.ExecSQL(_connection, _sql.ToString(), true);
                i++;
            }

        }

        /// <summary>
        /// Удаляет сохранненые записи о Счетчиках из квитанции
        /// </summary>
        /// <param name="numLs">Лицевой счет</param>
        public void DeleteCountersOrd(int numLs)
        {
            
            _sql.Remove(0, _sql.Length);
            _sql.Append("  delete from "+_baseCharge+"counters_ord "+
                       " where num_ls="+numLs+
                       " and dat_month='"+ _dateMonth.ToShortDateString() + "'");
            DBManager.ExecSQL(_connection, _sql.ToString(), false);
      
        }

        public void Clear()
        {
            ListCounters.Clear();
            ListDomCounters.Clear();
        }
    }
   
}


