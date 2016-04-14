using System;
using System.Collections.Generic;
using System.Data;
using STCLINE.KP50.Interfaces;
using STCLINE.KP50.Global;


namespace STCLINE.KP50.DataBase
{


    /// <summary>
    /// Класс отвечающий за сохранение cумм перерасчета
    /// </summary>
    public class DbRevalReason : IDisposable
    {

        //уточненные причины перерасчета
        enum KindReason
        {
            /// <summary>
            /// Перерасчет Недопоставки
            /// </summary>
            RNedop = 4, 
            /// <summary>
            /// Перерасчет тарифа
            /// </summary>
            RTarif = 11, 
            /// <summary>
            /// Перерасчет по показаниям ИПУ
            /// </summary>
            RChangePuVal = 12, 
            /// <summary>
            /// Перерасчет при закрытии ПУ
            /// </summary>
            RClosePu = 13, 
            /// <summary>
            /// Поломка/Поверка ИПУ
            /// </summary>
            RBrokenPu = 14, 
            /// <summary>
            /// Переход с норматива на ИПУ
            /// </summary>
            RInsertPuVal = 15, 
            /// <summary>
            /// Переход с ИПУ на норматив удаление показания
            /// </summary>
            RDeletePuVal = 16,  
            /// <summary>
            /// Начало действия услуги
            /// </summary>
            RStartService = 17, 
            /// <summary>
            /// Прекращение действия услуги
            /// </summary>
            RStopService = 18,   
            /// <summary>
            /// Временное выбытие жильца
            /// </summary>
            RGilVrVib = 19,   
            /// <summary>
            /// Изменение в составе проживающих
            /// </summary>
            RGilChange = 20,
            /// <summary>
            /// Изменение в параметрах
            /// </summary>
            RPrmChange = 21,
            /// <summary>
            /// Изменение в расходе по основной услуге
            /// </summary>
            RMainServChange = 22
        }

        enum Reason
        {
            /// <summary>
            /// Перерасчет по параметрам
            /// </summary>
            Prm = 1,
            /// <summary>
            /// Перерасчет по услугам
            /// </summary>
            Service = 2, 
            /// <summary>
            /// Перерасчет по недопоставкам
            /// </summary>
            Nedop = 3, 
            /// <summary>
            /// Перерасчет по счетчикам
            /// </summary>
            Counters = 4, 
            /// <summary>
            /// Перерасчет по счетчикам
            /// </summary>
            Gil = 6 
        }
        
        protected IDbConnection Connection;
        private int _month;
        private int _year;
        private List<string> logSql = new List<string>();

        private List<string> _tempTableList = new List<string>();

        public DbRevalReason(IDbConnection connection)
        {
            Connection = connection;
            CreateTempTable();
        }

        /// <summary>
        /// Пустой конструктор по умолчанию вызывать пока нельзя
        /// </summary>
        protected DbRevalReason()
        {

        }

        /// <summary>
        /// Выполнение запроса к БД
        /// </summary>
        /// <param name="sql">Текст запроса</param>
        /// <param name="withException">Выполнять ли исключение при неудачном выпонении</param>
        /// <returns></returns>
        private void ExecSql(string sql, bool withException = true)
        {
            logSql.Add(sql);
            if (!DBManager.ExecSQL(Connection, sql, true).result && withException)
                throw new Exception();
        }

        /// <summary>
        /// Создание временной таблицы для операций
        /// </summary>
        private void CreateTempTable()
        {
            string sql = "Create temp table t_selkv(nzp_kvar integer, nzp_wp integer)";
            ExecSql(sql);
            _tempTableList.Add("t_selkv");

            sql = "Create temp table t_reval_reason( " +
                  " year_ integer,            " +
                  " month_ integer,           " +
                  " nzp_kvar integer,         " +
                  " num_ls integer,           " +
                  " nzp_serv integer,         " +
                  " nzp_supp integer,         " +
                  " nzp_reason integer,       " + //Причина перерасчета
                  " kind_reason integer,      " + //Уточненная причина перерасчета
                  " reval Numeric(14,2),      " + //Сумма перерасчета
                  " c_reval Numeric(14,2),    " + //Объем перерасчета
                  " nzp_counter integer,      " +
                  " nzp_gil integer,          " +
                  " nzp_prm integer,          " +
                  " nzp_payer integer,        " +
                  " note varchar(200),        " +
                  " dat_s Date, " +
                  " dat_po Date) ";
            ExecSql(sql);
            _tempTableList.Add("t_reval_reason");
        }


        /// <summary>
        /// Удаление временных таблиц в случае ошибочного завершения 
        /// </summary>
        private void DropTempTable()
        {
            try
            {

                foreach (string table in _tempTableList)
                {
                    if (DBManager.TempTableInWebCashe(Connection, table))
                        ExecSql("DROP TABLE " + table);
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка! Удаление временных таблиц " + ex,
                    MonitorLog.typelog.Error, true);
            }

            for (int i = 0; i < logSql.Count; i++)
                MonitorLog.WriteLog(logSql[i], MonitorLog.typelog.Info, true);
        }

        /// <summary>
        /// Построение индексов
        /// </summary>
        private void BuildIndex()
        {
            ExecSql("CREATE INDEX ix_t_selkv_01 on t_selkv(nzp_wp)");
            ExecSql("CREATE INDEX ix_t_selkv_02 on t_selkv(nzp_kvar)");
            ExecSql(DBManager.sUpdStat + " t_selkv");
        }

        public void Dispose()
        {
            ExecSql("DROP TABLE t_selkv", false);
        }

        /// <summary>
        /// Геренация сумм перерасчетов в таблицу reval_reason_xx
        /// </summary>
        /// <param name="tableSelkvar">Таблица с nzp_kvar выбранными для заполнения</param>
        /// <param name="year">Расчетный год</param>
        /// <param name="month">Расчетный месяц</param>
        /// <returns>TRUE если успешно заполнены суммы</returns>
        public bool Generate(string tableSelkvar, int year, int month)
        {
            _tempTableList.Clear();
            try
            {
                string sql = " INSERT INTO t_selkv (nzp_kvar, nzp_wp) " +
                             " SELECT k.nzp_kvar, k.nzp_wp " +
                             " FROM " + tableSelkvar + " a, " +
                             Points.Pref + DBManager.sDataAliasRest + "kvar k " +
                             " WHERE a.nzp_kvar=k.nzp_kvar ";
                ExecSql(sql);
                _month = month;
                _year = year;
                BuildIndex();
                BuildRevalTable();
                return true;
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка! Генерация расшифровки причин перерасчета " +
                    month + "." + year + " : " + ex, 
                    MonitorLog.typelog.Error, true);
                DropTempTable();
                return false;
            }

        }

        /// <summary>
        /// Геренация сумм перерасчетов в таблицу reval_reason_xx
        /// </summary>
        /// <param name="nzpKvar">Код лицевого счета(Номер)</param>
        /// <param name="year">Расчетный год</param>
        /// <param name="month">Расчетный месяц</param>
        /// <returns>TRUE если успешно заполнены суммы</returns>
        public bool GenerateLs(Int64 nzpKvar, int year, int month)
        {
            _tempTableList.Clear();
            try
            {
                string sql = " INSERT INTO t_selkv (nzp_kvar, nzp_wp) " +
                             " SELECT k.nzp_kvar, k.nzp_wp " +
                             " FROM  " + Points.Pref + DBManager.sDataAliasRest + "kvar k " +
                             " WHERE k.nzp_kvar =" + nzpKvar;
                ExecSql(sql);
                _month = month;
                _year = year;
                BuildIndex();
                BuildRevalTable();
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка! Генерация расшифровки причин перерасчета " +
                                    " ЛС " + nzpKvar + " " + month + "." + year +  ": " + ex, 
                    MonitorLog.typelog.Error, true);
                DropTempTable();
                return false;
            }

            return true;
        }



        /// <summary>
        /// Расшифровка и запись сумм перерасчета
        /// </summary>
        private void BuildRevalTable()
        {
            string sql = " SELECT sp.bd_kernel  " +
                         " FROM " + Points.Pref + DBManager.sKernelAliasRest + "s_point sp, t_selkv t" +
                         " WHERE sp.nzp_wp=t.nzp_wp" +
                         " GROUP BY 1 ";
            
            var preftable = DBManager.ExecSQLToTable(Connection, sql);
            
            foreach (DataRow dr in preftable.Rows)
            {
                string localPref = dr["bd_kernel"].ToString().Trim();

                string localCharge = localPref + "_charge_" + (_year - 2000).ToString("00") +
                                     DBManager.tableDelimiter + "lnk_charge_" + _month.ToString("00");

                //Определяем причины перерасчета
                sql = " SELECT a.nzp_serv, a.nzp_supp, a.nzp_kvar, a.dat_s, a.dat_po, a.kod1, a.kod2 " +
                      " INTO TEMP local_must " +
                      " FROM " + localPref + DBManager.sDataAliasRest + "must_calc a, t_selkv b " +
                      " WHERE a.nzp_kvar=b.nzp_kvar" +
                      "     AND month_=" + _month +
                      "     AND year_=" + _year;
                ExecSql(sql);
                _tempTableList.Add("local_must");

                sql = " SELECT year_, month_ " +
                      " FROM t_selkv t, " + localCharge + " l " +
                      " WHERE t.nzp_kvar=l.nzp_kvar "+
                      " GROUP BY 1,2";
                var prevChargeTables = DBManager.ExecSQLToTable(Connection, sql);

                foreach (DataRow dmonth in prevChargeTables.Rows)
                {
                    PrepareOneMonth(localPref, Convert.ToInt32(dmonth["year_"]), Convert.ToInt32(dmonth["month_"]));
                    AnalizeOneMonth(localPref, Convert.ToInt32(dmonth["year_"]), Convert.ToInt32(dmonth["month_"]));
                }

            }
        }


        /// <summary>
        /// Анализирует причины перерасчета
        /// </summary>
        /// <param name="localPref">Префикс схемы локального банка</param>
        /// <param name="revalYear">Перерасчитываемый год</param>
        /// <param name="revalMonth">Перерасчитываемый месяц</param>
        private void AnalizeOneMonth(string localPref, int revalYear, int revalMonth)
        {
            //Делим перерасчеты на группы
            //Нормативщики
            string sql = " UPDATE t_revalcharge set reval_group = 1" +
                         " where is_device=0 and is_device_p=0";
            ExecSql(sql);

            //прибористы
            sql = " UPDATE t_revalcharge set reval_group = 2" +
                  " where is_device>1 and is_device_p>1";
            ExecSql(sql);

            //из нормативщиков в прибористы
            sql = " UPDATE t_revalcharge set reval_group = 3" +
                  " where is_device>1 and is_device_p=0";
            ExecSql(sql);

            //из прибористов в нормативщики
            sql = " UPDATE t_revalcharge set reval_group = 4" +
                  " where is_device=0 and is_device_p>1";
            ExecSql(sql);

            ExecSql("CREATE INDEX ix_t_revalcharge_03 on t_revalcharge(reval_group)");
            ExecSql(DBManager.sUpdStat + " t_revalcharge");

            AnalizeTarif(revalYear, revalMonth);

            AnalizeNedop(localPref, revalYear, revalMonth);
           
           
            // Удаляем записи у которых сумма перерасчета равна 0
            ExecSql(" delete from t_revalcharge where abs(reval) < 0.001");

            AnalizeNormaNorma(localPref, revalYear, revalMonth);



            //Определяем счетчики, по которым произошли перерасчеты

            sql = " select a.nzp_kvar, a.nzp_serv, max(kod2) as nzp_counter " +
                  " into temp tc" +
                  " from local_must a, t_revalcharge b" +
                  " where a.nzp_kvar=b.nzp_kvar and a.nzp_serv=b.nzp_serv" +
                  "       and a.kod1 = 4 " +
                  " group by 1,2";
            ExecSql(sql);
            _tempTableList.Add("tc");
            ExecSql("create index ix_tc_tc_01 on tc(nzp_kvar, nzp_serv)");
            ExecSql(DBManager.sUpdStat + " tc");

            AnalizeNormaPu(revalYear, revalMonth);
            
            AnalizePuNorma(localPref, revalYear, revalMonth);
            
            AnalizePuPu(localPref, revalYear, revalMonth);

            ExecSql("drop table tc");
            string baseRevalReason = localPref + "_charge_" + (_year - 2000).ToString("00") +
                                     ".reval_reason_" + _month.ToString("00");

            //Финально проставляем параметры для тех услуг, у которых не определились параметры
            //но перерасчет из параметров (перерасчет по услуге ИТОГО)
            sql = " select nzp_kvar, max(kod2) as nzp_prm" +
                  " into temp tlmust " +
                  " from local_must a" +
                  " where a.nzp_serv=1 " +
                  "     and a.kod1 = 1 " +
                  " group by 1";
            ExecSql(sql);
            _tempTableList.Add("tlmust");
            ExecSql("Create index ix_tlmust_01 on tlmust(nzp_kvar)");
            ExecSql(DBManager.sUpdStat+" tlmust");

            sql = " update t_reval_reason set nzp_prm = a.nzp_prm" +
                  " from tlmust a" +
                  " where t_reval_reason.kind_reason=21 " +
                  "     and t_reval_reason.nzp_prm is null" +
                  "     and a.nzp_kvar=t_reval_reason.nzp_kvar ";
            ExecSql(sql);

            ExecSql("drop table tlmust");

            sql = " INSERT INTO " + baseRevalReason + "(year_,month_, nzp_kvar, num_ls, nzp_serv,         " +
                  " nzp_supp, nzp_reason, kind_reason , reval, c_reval , nzp_counter,      " +
                  " nzp_gil , nzp_prm , nzp_payer ,  note, dat_s , dat_po ) " +
                  " SELECT " + revalYear + "," + revalMonth + "_, nzp_kvar, num_ls, nzp_serv,         " +
                  " nzp_supp, nzp_reason, kind_reason, reval, c_reval , nzp_counter,      " +
                  " nzp_gil , nzp_prm , nzp_payer ,  note, dat_s , dat_po " +
                  " FROM t_reval_reason";
            ExecSql(sql);

            ExecSql("truncate t_reval_reason");
            ExecSql("DROP TABLE t_revalcharge");
        }


        /// <summary>
        /// Анализ изменения действия тарифа или услуги
        /// </summary>
        /// <param name="revalYear">Перерасчитываемый год</param>
        /// <param name="revalMonth">Перерасчитываемый месяц</param>
        private void AnalizeTarif(int revalYear, int revalMonth)
        {
            string periodS = "date('01." + revalMonth + "." + revalYear + "')";
            string periodPo = "date('"+DateTime.DaysInMonth(revalYear, revalMonth) + "." +
                         revalMonth + "." + revalYear+"')";

            // Заносим прекращение действия услуги
            string sql = " INSERT INTO t_reval_reason(year_, month_ , nzp_kvar, num_ls, nzp_serv , " +
                         "          nzp_supp, nzp_reason, kind_reason,  reval,c_reval, dat_s, dat_po)     " +
                         " SELECT " + revalYear + "," + revalMonth + ",nzp_kvar, nzp_kvar," +
                         "     nzp_serv , nzp_supp, " + (int) Reason.Prm + ", " + (int) KindReason.RStopService + "," +
                         "     reval, c_calc-c_calc_p, "+periodS+"," +periodPo +
                         " FROM t_revalcharge " +
                         " WHERE tarif = 0 " +
                         "      AND tarif_p <> 0 " +
                         "      AND abs(tarif - tarif_p) > 0.0001";
            ExecSql(sql);

            // Вся сумма перерасчета относится на прекращение услуги, остальным 0
            sql = " update t_revalcharge set reval = 0" +
                  " WHERE tarif = 0 " +
                  "      AND tarif_p <> 0 " +
                  "      AND abs(tarif - tarif_p) > 0.0001";
            ExecSql(sql);

            // Заносим появление услуги
            sql = " INSERT INTO t_reval_reason(year_, month_ , nzp_kvar, num_ls, nzp_serv , " +
                  "         nzp_supp, nzp_reason, kind_reason, reval,c_reval, dat_s, dat_po)     " +
                  " SELECT " + revalYear + "," + revalMonth + ",nzp_kvar, nzp_kvar," +
                  "     nzp_serv , nzp_supp, " + (int) Reason.Prm + ", " + (int) KindReason.RStartService + "," +
                  "     reval, c_calc-c_calc_p, " + periodS + "," + periodPo +
                  " FROM t_revalcharge " +
                  " WHERE tarif <> 0 " +
                  "     AND tarif_p = 0 " +
                  "     AND abs(tarif - tarif_p) > 0.0001";
            ExecSql(sql);
            // Вся сумма перерасчета относится на открытие услуги, остальным 0
            sql = " update t_revalcharge set reval = 0" +
                  " WHERE tarif <> 0 " +
                  "     AND tarif_p = 0 " +
                  "     AND abs(tarif - tarif_p) > 0.0001";
            ExecSql(sql);

            // Заносим изменения тарифа
            sql = " INSERT INTO t_reval_reason(year_, month_ , nzp_kvar, num_ls, nzp_serv , " +
                  "        nzp_supp, nzp_reason, kind_reason, reval,c_reval, dat_s, dat_po)     " +
                  " SELECT " + revalYear + "," + revalMonth + ",nzp_kvar, nzp_kvar," +
                  "     nzp_serv , nzp_supp, " + (int) Reason.Prm + ", " + (int) KindReason.RTarif + "," +
                  "     (tarif - tarif_p) * c_calc, 0, " + periodS + "," + periodPo +
                  " FROM t_revalcharge " +
                  " WHERE tarif > 0 " +
                  "     AND tarif_p > 0 " +
                  "     AND abs(tarif - tarif_p) > 0.0001";
            ExecSql(sql);
         
            //Уменьшаем сумму перерасчета на сумму изменения по тарифу
            sql = " update t_revalcharge set reval = reval - (tarif - tarif_p) * c_calc" +
                  " WHERE tarif > 0 " +
                  "     AND tarif_p > 0 " +
                  "     AND abs(tarif - tarif_p) > 0.0001";
            ExecSql(sql);

            // Удаляем записи у которых сумма перерасчета равна 0
            sql = " delete from t_revalcharge where abs(reval+sum_nedop-sum_nedop_p) < 0.001";
            
            ExecSql(sql);
        }


        /// <summary>
        /// Определение недопоставки
        /// </summary>
        /// <param name="localPref">Префикс схемы банка данных</param>
        /// <param name="revalYear">Перерасчитываемый год</param>
        /// <param name="revalMonth">Перерасчитываемый месяц</param>
        private void AnalizeNedop(string localPref, int revalYear, int revalMonth)
        {
            string sql = " create temp  table t_sprav_otkl_usl (" +
                         " nzp_kvar          INTEGER, " +
                         " nzp_supp          INTEGER, " +
                         " nzp_vinovnik      INTEGER, " +
                         " nzp_kind          INTEGER, " +
                         " note              VARCHAR(200), " +
                         " nzp_serv          INTEGER, " +
                         " countKvar         INTEGER, " +
                         " count_daynedo     NUMERIC(14,2), " +
                         " count_day_all     NUMERIC(14,2), " +
                         " count_kvarchas    INTEGER DEFAULT 0, " +
                         " col_gil           INTEGER, " +
                         " sum_nedop         NUMERIC(14,2), " +
                         " sum_nedop_all     NUMERIC(14,2) " +
                         " )   ";

            ExecSql(sql);
            _tempTableList.Add("t_sprav_otkl_usl");

            string lastDayM = revalYear + "-" + revalMonth.ToString("00") + "-" +
                                DateTime.DaysInMonth(revalYear, revalMonth) + " 23:59";
            string firstDayM = revalYear + "-" + revalMonth.ToString("00") + "-01 00:00";


            sql = 
                  " SELECT d.nzp_kvar, nzp_supp, nzp_serv, " +
                  "     sum(sum_nedop - sum_nedop_p) as sum_nedop  " +
                  " INTO TEMP t_sum_nedo" +
                  " FROM t_revalcharge d " +
                  " WHERE  abs(sum_nedop - sum_nedop_p) > 0.001" +
                  " GROUP BY 1,2,3";
            ExecSql(sql);
            ExecSql("CREATE INDEX ix_sum_nedo_01 ON t_sum_nedo(nzp_kvar)");
            ExecSql(DBManager.sUpdStat+ " t_sum_nedo");
            _tempTableList.Add("t_sum_nedo");



            //Выбираем недопоставки  за перерасчитываемый месяц!!!!
            sql = "CREATE TEMP TABLE t_vinovnik (" +
                  " nzp_kvar integer," +
                  " nzp_serv integer," +
                  " nzp_serv_sg integer," +
                  " vinovnik integer," +
                  " nzp_kind integer," +
                  " note varchar(200)," +
                  " dat_s timestamp," +
                  " dat_po timestamp," +
                  " day_interval interval, "+
                  " day_nedo Numeric(14,5))";
            ExecSql(sql);
            _tempTableList.Add("t_vinovnik");

            //Обрезаем перерасчетным месяцем дни недопоставок
            sql = " INSERT INTO t_vinovnik(nzp_kvar, nzp_serv, vinovnik,nzp_kind, note, dat_s, dat_po)" +
                  " SELECT  a.nzp_kvar, a.nzp_serv, a.nzp_supp as vinovnik, nzp_kind, comment, " +
                  " CASE WHEN dat_s < cast('"+firstDayM+"' as timestamp) THEN cast('"+firstDayM+"' as timestamp)  ELSE dat_s END, "+
                  " CASE WHEN dat_po > cast('" + lastDayM + "' as timestamp) THEN cast('" + lastDayM + "' as timestamp) ELSE dat_po end " +
                  " FROM   " + localPref + DBManager.sDataAliasRest + "nedop_kvar a, t_sum_nedo d " +
                  " WHERE  a.nzp_kvar = d.nzp_kvar " +
                  "     AND a.month_calc = date('01." + _month + "." + _year + "') " +
                  "     AND dat_s <= cast('" + lastDayM+"' as timestamp) " +
                  "     AND dat_po >= cast('" + firstDayM+"' as timestamp) ";
            ExecSql(sql);


            //Вычисляем прошедший интервал
            sql = " UPDATE t_vinovnik SET day_interval = age(dat_po, dat_s)";
            ExecSql(sql);

            //Вычисляем сколько в днях недопоставка
            sql = " UPDATE t_vinovnik SET day_nedo = Round(cast (extract(day from day_interval) * 1440 +" +
                  " EXTRACT(hour from day_interval) * 60 +" +
                  " EXTRACT(minute from day_interval) as numeric) / 1440, 2)";
            ExecSql(sql);


            #region Добавляем связанную недопоставку

            ExecSql("drop table t_v");

             sql = " CREATE TEMP table t_v (" +
                          " nzp_kvar integer, " +
                          " nzp_serv integer," +
                          " nzp_kind integer," +
                          " note varchar(200)," +
                          " vinovnik integer," +
                          " nzp_serv_sg integer," +
                          " day_nedo NUMERIC(14,2))" + DBManager.sUnlogTempTable;
            ExecSql(sql);
            _tempTableList.Add("t_v");

            sql = " INSERT INTO t_v (nzp_kvar, nzp_serv, vinovnik, nzp_kind, note, nzp_serv_sg, day_nedo)" +
                  " SELECT nzp_kvar, 14, vinovnik, nzp_kind, note, 14, day_nedo " +
                  " FROM t_vinovnik a " +
                  " WHERE a.nzp_serv = 9 " +
                  "  AND a.nzp_kvar NOT IN (SELECT nzp_kvar " +
                  "                         FROM t_vinovnik b " +
                  "                         WHERE b.nzp_serv=14) ";
            ExecSql(sql);


            sql = " INSERT INTO t_vinovnik (nzp_kvar, nzp_serv,nzp_kind, note,  vinovnik, nzp_serv_sg,  day_nedo)" +
                   " SELECT nzp_kvar, nzp_serv,nzp_kind,  note, vinovnik, nzp_serv_sg, day_nedo " +
                   " FROM t_v ";
            ExecSql(sql);

            ExecSql("DROP TABLE t_v");


            #endregion

            //Вычисляем общее число дней недопоставки
            sql =
                " SELECT  nzp_kvar,nzp_serv, " +
                " sum(day_nedo) as count_daynedo " +
                " INTO TEMP  t_alldaynedo " +
                " FROM  t_vinovnik " +
                " GROUP BY 1,2  ";
            ExecSql(sql);


            //Применяем корректировку сумм недопоставки по виновникам
            sql = " INSERT INTO t_sprav_otkl_usl (nzp_kvar, nzp_supp, nzp_vinovnik,nzp_kind, note, nzp_serv,  " +
                  " count_daynedo, sum_nedop, sum_nedop_all, count_day_all) " +
                  " select distinct a.nzp_kvar, a.nzp_supp, vinovnik, nzp_kind, note,   a.nzp_serv, day_nedo,  " +
                  " case when count_daynedo > 0 then a.sum_nedop * day_nedo / count_daynedo else 0 end, a.sum_nedop, " +
                  " count_daynedo  " +
                  " FROM t_sum_nedo a, t_vinovnik b, t_alldaynedo d" +
                  " WHERE a.nzp_kvar=b.nzp_kvar " +
                  "     AND a.nzp_kvar=d.nzp_kvar " +
                  "     AND a.nzp_serv=d.nzp_serv " +
                  "     AND a.nzp_serv=b.nzp_serv " +
                  "";
            ExecSql(sql);

            ExecSql("CREATE INDEX ix_tmp756 ON t_sprav_otkl_usl(nzp_kvar) ");
            ExecSql(DBManager.sUpdStat+" t_sprav_otkl_usl ");

           
            //Подсчитываем не потеряли ли 
            sql =
                " SELECT nzp_kvar, nzp_serv, max(sum_nedop_all)-sum(sum_nedop) as sum_nedop, max(nzp_vinovnik) as max_vin, " +
                " max(count_day_all) - sum(count_daynedo) as day_nedo " +
                " INTO TEMP t_corr " +
                " FROM t_sprav_otkl_usl " +
                " GROUP BY 1,2 ";
            ExecSql(sql);
            _tempTableList.Add("t_corr");
            
            // Корректируем потерянные дни и сумму недопоставки
            sql = " UPDATE t_sprav_otkl_usl SET " +
                  "     sum_nedop = sum_nedop + coalesce((" +
                  "             SELECT sum(sum_nedop) " +
                  "             FROM t_corr " +
                  "             WHERE t_sprav_otkl_usl.nzp_kvar=t_corr.nzp_kvar " +
                  "                 AND t_sprav_otkl_usl.nzp_serv=t_corr.nzp_serv ),0), " +
                  "     count_daynedo = count_daynedo + coalesce((" +
                  "             SELECT sum(day_nedo) " +
                  "             FROM t_corr " +
                  "             WHERE t_sprav_otkl_usl.nzp_kvar=t_corr.nzp_kvar " +
                  "                 AND t_sprav_otkl_usl.nzp_serv=t_corr.nzp_serv),0) " +
                  " WHERE sum_nedop>0 " +
                  "     AND EXISTS (SELECT 1 FROM t_corr " +
                  "                 WHERE t_sprav_otkl_usl.nzp_kvar=t_corr.nzp_kvar " +
                  "                 AND t_sprav_otkl_usl.nzp_serv=t_corr.nzp_serv " +
                  "                 AND t_sprav_otkl_usl.nzp_vinovnik=t_corr.max_vin)  ";

            ExecSql(sql);
            ExecSql("DROP TABLE t_corr");
            ExecSql("DROP TABLE t_sum_nedo");
            ExecSql("DROP TABLE t_vinovnik");
            ExecSql("DROP TABLE t_alldaynedo");

            string periodS = "date('01." + revalMonth + "." + revalYear + "')";
            string periodPo = "date('" + DateTime.DaysInMonth(revalYear, revalMonth) + "." +
                         revalMonth + "." + revalYear + "')";

            sql = " Insert into t_reval_reason(year_, month_ , nzp_kvar, num_ls,  " +
                      "     nzp_serv , nzp_supp, nzp_payer, note, nzp_reason, kind_reason, reval," +
                  " c_reval, dat_s, dat_po)     " +
                      " select " + revalYear + "," + revalMonth + ",nzp_kvar, nzp_kvar," +
                      "     nzp_serv , nzp_supp, nzp_vinovnik, note,  " + (int)Reason.Nedop + ", " +
                             (int)KindReason.RNedop + "," +
                      "     sum(-sum_nedop), 0, " + periodS + "," + periodPo +
                      " from t_sprav_otkl_usl " +
                  " group by 1,2,3,4,5,6,7,8,9,10,12,13,14" ;
            ExecSql(sql);
            ExecSql("DROP TABLE t_sprav_otkl_usl");


            sql = " Insert into t_reval_reason(year_, month_ , nzp_kvar, num_ls,  " +
                   "     nzp_serv , nzp_supp, nzp_payer, nzp_reason, kind_reason, reval," +
               " c_reval, dat_s, dat_po)     " +
                   " select " + revalYear + "," + revalMonth + ",nzp_kvar, nzp_kvar," +
                   "     nzp_serv , nzp_supp, 0, " + (int)Reason.Nedop + ", " +
                          (int)KindReason.RNedop + "," +
                   "     sum(-sum_nedop), 0, " + periodS + "," + periodPo +
                   " from t_revalcharge where nzp_serv=7 and abs(sum_nedop)>0.001" +
               " group by 1,2,3,4,5,6,7,8,9,11,12,13";
            ExecSql(sql);
        }


        /// <summary>
        /// Анализ перерасчета по прибористам
        /// </summary>
        private void AnalizePuPu(string localPref, int revalYear, int revalMonth)
        {
            string periodS = "date('01." + revalMonth + "." + revalYear + "')";
            string periodPo = "date('" + DateTime.DaysInMonth(revalYear, revalMonth) + "." +
                         revalMonth + "." + revalYear + "')";
            //Расшифровываются причины
            //Установка/замена ИПУ
            //Изменение показания ИПУ
            //Закрытие ИПУ задним числом
            
            
            //Три ситуации
            //1. Был ПУ стал ПУ - изменение показания ПУ
            // пока вместе
            //3. Было среднее стало ПУ - внесли показание
            //   либо починки
            //   либо поверки
            //   либо отменили закрытие
            string sql = " Insert into t_reval_reason(year_, month_ , nzp_kvar, num_ls,  " +
                         "     nzp_serv , nzp_supp, nzp_reason, kind_reason, reval,c_reval, " +
                         "      dat_s, dat_po, nzp_counter)     " +
                         " select " + revalYear + "," + revalMonth + ", a.nzp_kvar, a.nzp_kvar," +
                         "     a.nzp_serv , a.nzp_supp, " + (int)Reason.Counters + ", " +
                                (int)KindReason.RChangePuVal + "," +
                         "     a.reval, (case when a.tarif<>0 then a.reval/a.tarif else 0 end), " + periodS + "," + periodPo +
                         ", tc.nzp_counter from t_revalcharge a left outer join tc  " +
                         "      on a.nzp_kvar=tc.nzp_kvar and a.nzp_serv=tc.nzp_serv " +
                         " where is_device=1 and is_device_p>0 ";
            ExecSql(sql);

            //2. Был ПУ стало среднее - либо закрытие ПУ задним числом
            //определяем закрытые приборы учета по которым прошел перерасчет
            sql = " select a.nzp_kvar, a.nzp_serv, max(a.kod2) as nzp_counter " +
                  " into temp t_closepu " +
                  " from local_must a, "+localPref+DBManager.sDataAliasRest +"counters_spis cs "+
                  " where a.kod1=4 " +
                  "     and cs.is_actual = 1" +
                  "     and a.kod2=cs.nzp_counter " +
                  "     and a.nzp_serv=cs.nzp_serv   "+   
                  "     and cs.dat_close is not null " +
                  "     and cs.dat_close between a.dat_s and a.dat_po " +
                  " group by 1,2 ";
            ExecSql(sql);
            _tempTableList.Add("t_closepu");
            ExecSql("create index ix_tmp_tcl_01 on t_closepu(nzp_kvar, nzp_serv)");
            ExecSql(DBManager.sUpdStat+"  t_closepu");

            //Были закрытые счетчики
            sql = " Insert into t_reval_reason(year_, month_ , nzp_kvar, num_ls,  " +
                  "     nzp_serv , nzp_supp, nzp_reason, kind_reason, reval,c_reval, " +
                  "     dat_s, dat_po, nzp_counter )     " +
                  " select " + revalYear + "," + revalMonth + ", a.nzp_kvar, a.nzp_kvar," +
                  "     a.nzp_serv , a.nzp_supp, " + (int) Reason.Counters + ", " +
                  (int) KindReason.RClosePu + "," +
                  "     a.reval, (case when a.tarif<>0 then a.reval/a.tarif else 0 end) , " + periodS + "," + periodPo +
                  ", m.nzp_counter " +
                  " from t_revalcharge a , t_closepu m" +
                  " where is_device=9 and is_device_p=1 " +
                  "    and a.nzp_kvar=m.nzp_kvar and a.nzp_serv=m.nzp_serv ";
            ExecSql(sql);

            //   либо введена дата поломки
            //   либо введена дата поверки
            sql = " Insert into t_reval_reason(year_, month_ , nzp_kvar, num_ls,  " +
                      "     nzp_serv , nzp_supp, nzp_reason, kind_reason, reval,c_reval, dat_s, dat_po)     " +
                      " select " + revalYear + "," + revalMonth + ",nzp_kvar, nzp_kvar," +
                      "     nzp_serv , nzp_supp, " + (int)Reason.Counters + ", " +
                      (int)KindReason.RBrokenPu + "," +
                      "     reval, (case when tarif<>0 then reval/tarif else 0 end), " + periodS + "," + periodPo +
                      " from t_revalcharge a" +
                      " where is_device=9 and is_device_p=1 " +
               "  and not exists (select 1 " +//Не было закрытых счетчиков
               "              from t_closepu m" +
               "              where a.nzp_kvar=m.nzp_kvar and a.nzp_serv=m.nzp_serv) ";
            ExecSql(sql);

            ExecSql("drop table t_closepu");


        }


        /// <summary>
        /// Анализ перехода с ИПУ на норму
        /// </summary>
        /// <param name="localPref"></param>
        /// <param name="revalYear"></param>
        /// <param name="revalMonth"></param>
        private void AnalizePuNorma(string localPref, int revalYear, int revalMonth)
        {
            string periodS = "date('01." + revalMonth + "." + revalYear + "')";
            string periodPo = "date('" + DateTime.DaysInMonth(revalYear, revalMonth) + "." +
                         revalMonth + "." + revalYear + "')";

            //определяем закрытые приборы учета по которым прошел перерасчет
          string  sql = " select a.nzp_kvar, a.kod2 as nzp_counter " +
                  " into temp t_closepu " +
                  " from local_must a, " + localPref + DBManager.sDataAliasRest + "counters_spis cs " +
                  " where a.kod1=4 " +
                  "     and a.kod2=cs.nzp_counter " +
                  "     and dat_close is not null " +
                  "     and dat_close between a.dat_s and a.dat_po ";
            ExecSql(sql);
            _tempTableList.Add("t_closepu");
            ExecSql("create index ix_tmp_tcl_01 on t_closepu(nzp_kvar)");
            ExecSql(DBManager.sUpdStat + "  t_closepu");

            // Произошел переход со счетчика на норму, счетчик закрыт
            sql = " Insert into t_reval_reason(year_, month_ , nzp_kvar, num_ls,  " +
                  "     nzp_serv , nzp_supp, nzp_reason, kind_reason, reval,c_reval, dat_s, dat_po)     " +
                  " select " + revalYear + "," + revalMonth + ",nzp_kvar, nzp_kvar," +
                  "     nzp_serv , nzp_supp, " + (int) Reason.Counters + ", " +
                  (int) KindReason.RClosePu + "," +
                  "     reval, (case when tarif<>0 then reval/tarif else 0 end), " + periodS + "," + periodPo +
                  " from t_revalcharge a" +
                  " where is_device=0 and is_device_p>0 " +
                  "  and exists (select 1 " +
                  "              from t_closepu m" +
                  "              where a.nzp_kvar=m.nzp_kvar) ";
            ExecSql(sql);

            //Произошел переход со счетчика на норму, счетчик не закрыт, удалили показание
            sql = " Insert into t_reval_reason(year_, month_ , nzp_kvar, num_ls,  " +
                  "     nzp_serv , nzp_supp, nzp_reason, kind_reason, reval,c_reval, dat_s, dat_po)     " +
                  " select " + revalYear + "," + revalMonth + ",nzp_kvar, nzp_kvar," +
                  "     nzp_serv , nzp_supp, " + (int) Reason.Counters + ", " +
                  (int) KindReason.RDeletePuVal + "," +
                  "     reval, (case when tarif<>0 then reval/tarif else 0 end), " + periodS + "," + periodPo +
                  " from t_revalcharge a" +
                  " where is_device = 0 and is_device_p>0 " +
                  "  and not exists (select 1 " +// нет закрытых счетчиков
                  "              from t_closepu m" +
                  "              where a.nzp_kvar=m.nzp_kvar) ";
            ExecSql(sql);
            ExecSql("drop table t_closepu");
        }


        /// <summary>
        /// Смена расчета с норматива на ИПУ
        /// либо внесли показание, либо установили ПУ
        /// </summary>
        /// <param name="revalYear"></param>
        /// <param name="revalMonth"></param>
        private void AnalizeNormaPu(int revalYear, int revalMonth)
        {
            string periodS = "date('01." + revalMonth + "." + revalYear + "')";
            string periodPo = "date('" + DateTime.DaysInMonth(revalYear, revalMonth) + "." +
                         revalMonth + "." + revalYear + "')";

          
       

            // Для упрощения считаем, что при переходе от нормы к ПУ сдали показания, 
            // так как на данный момент не рассчитываются средние задним числом
           string  sql = " Insert into t_reval_reason(year_, month_ , nzp_kvar, num_ls,  " +
                         "     nzp_serv , nzp_supp, nzp_reason, kind_reason, reval,c_reval, " +
                         "     dat_s, dat_po, nzp_counter)     " +
                         " select " + revalYear + "," + revalMonth + ", a.nzp_kvar, a.nzp_kvar," +
                         "     a.nzp_serv , a.nzp_supp, " + (int) Reason.Counters + ", " +
                         (int) KindReason.RInsertPuVal + "," +
                         "     a.reval, (case when tarif<>0 then reval/tarif else 0 end)," + periodS + "," + periodPo +
                         ", tc.nzp_counter" +
                         " from t_revalcharge a left outer join tc " +
                         "  on a.nzp_kvar=tc.nzp_kvar and a.nzp_serv=tc.nzp_serv " +
                         " where is_device>0 and is_device_p=0 ";
            ExecSql(sql);


        }


        /// <summary>
        /// Перерасчет по нормативу
        /// </summary>
        private void AnalizeNormaNorma(string localPref, int revalYear, int revalMonth)
        {
            string periodS = "date('01." + revalMonth + "." + revalYear + "')";
            string periodPo = "date('" + DateTime.DaysInMonth(revalYear, revalMonth) + "." +
                         revalMonth + "." + revalYear + "')";
            //1. Временное убытие жильцов для коммунальных услуг
            AnalizeGilUbit(localPref, revalYear, revalMonth, periodS, periodPo);

            //По услуге канализация, изменения вследствии изменения основной услуги
            string sql = " Insert into t_reval_reason(year_, month_ , nzp_kvar, num_ls,  " +
                  "     nzp_serv , nzp_supp, nzp_reason, kind_reason, reval,c_reval, dat_s, dat_po)     " +
                  " select " + revalYear + "," + revalMonth + ",nzp_kvar, nzp_kvar," +
                  "     nzp_serv , nzp_supp, " + (int) Reason.Service + ", " + (int) KindReason.RMainServChange + "," +
                  "     reval, (case when tarif<>0 then reval/tarif else 0 end), " + periodS + "," + periodPo +
                  " from t_revalcharge a " +
                  " where is_device=0 and is_device_p=0 and nzp_serv=7  " + //костыль
                  " ";
            ExecSql(sql);


            sql = " select a.nzp_kvar, a.nzp_serv, max(kod2) as nzp_prm " +
                  " into temp tp" +
                  " from local_must a, t_revalcharge b" +
                  " where a.nzp_kvar=b.nzp_kvar and a.nzp_serv=b.nzp_serv" +
                  "       and is_device=0 and is_device_p=0 and a.nzp_serv not in (6,7,9,14,16,25,324) " +
                  "       and a.kod1 = 1 " +
                  " group by 1,2";
            ExecSql(sql);
            _tempTableList.Add("tp");
            ExecSql("create index ix_tp_tp_01 on tp(nzp_kvar, nzp_serv)");
            ExecSql(DBManager.sUpdStat + " tp");
            // Нужно бы обработать еще услугу Итого в перерасчетах, но как она влияет или не влияет
            // на каждую услугу в отдельности нужно проанализировать
         


            //1. Изменения по параметрам
            sql = " Insert into t_reval_reason(year_, month_ , nzp_kvar, num_ls,  " +
                  "     nzp_serv , nzp_supp, nzp_reason, kind_reason, reval,c_reval, dat_s, dat_po, nzp_prm)     " +
                  " select " + revalYear + "," + revalMonth + ", a.nzp_kvar, a.nzp_kvar," +
                  "     a.nzp_serv , a.nzp_supp, " + (int)Reason.Prm + ", " +(int)KindReason.RPrmChange + "," +
                  "     a.reval, (case when a.tarif<>0 then a.reval/a.tarif else 0 end), " + periodS + "," + periodPo +
                  ", tp.nzp_prm "+
                  " from t_revalcharge a left outer join tp" +
                  "     on a.nzp_kvar=tp.nzp_kvar and a.nzp_serv=tp.nzp_serv " +
                  " where " +
                  "     a.is_device=0 " +
                  "     and a.is_device_p=0 " +
                  "     and a.nzp_serv not in (6,7,9,14,16,25,324)  " +//костыль
                  " ";
            ExecSql(sql);

            ExecSql("drop table tp");
        }



        /// <summary>
        /// Анализ временного убытия жильцов
        /// </summary>
        /// <param name="localPref"></param>
        /// <param name="revalYear"></param>
        /// <param name="revalMonth"></param>
        /// <param name="periodS"></param>
        /// <param name="periodPo"></param>
        private void AnalizeGilUbit(string localPref, int revalYear, int revalMonth, string periodS, string periodPo)
        {
            //1. Выбираем перерасчеты и периоды по умолчанию

            string sql = " select " + revalYear + " as year_," + revalMonth + " as month_, nzp_kvar, nzp_kvar as num_ls," +
                         "     nzp_serv , nzp_supp, " + (int) Reason.Gil + " as nzp_reason, " +
                         (int) KindReason.RGilVrVib + " as kind_reason," +
                         "     reval, (case when tarif<>0 then reval/tarif else 0 end) as c_reval, " +
                         " " + periodS + " as dat_s," + periodPo + " as dat_po, 0 as prm130, 0 as nzp_gil " +
                         " INTO TEMP t_gil" +
                         " from t_revalcharge a " +
                         " where is_device=0 and is_device_p=0 and nzp_serv in (6,9,16,14,25,324)  ";
            ExecSql(sql);
            _tempTableList.Add("t_gil");
            ExecSql("create index ix_t_gil_01 on t_gil(nzp_kvar)");
            ExecSql(DBManager.sUpdStat+ " t_gil");

            //Проставляем признак тем, кто рассчитывался по паспортистке
            sql = "update t_gil set prm130 = 1 where  exists " +
                  " (select 1 from " + localPref + DBManager.sDataAliasRest + "prm_1 a" +
                  " where nzp_prm=130 and is_actual=1 and t_gil.nzp_kvar=nzp" +
                  " and a.dat_s<=t_gil.dat_po and a.dat_po>=t_gil.dat_s and val_prm='1')";
            ExecSql(sql);
            ExecSql("create index ix_t_gil_03 on t_gil(prm130)");

            //Пока ставим максимального жильца и всю сумму перерасчета на него
            //Временно, пока не сделали разбитие по жильцам
            //Предположение - если перерасчет, то ввели период выбытия
            sql = "update t_gil set nzp_gil = (select max(nzp_gilec) " +
                  " from "+ localPref + DBManager.sDataAliasRest +
                  "gil_periods b where t_gil.nzp_kvar=b.nzp_kvar" +
                  " and t_gil.dat_s<=b.dat_po and t_gil.dat_po>=b.dat_po and b.is_actual=1 )" +
                  " where prm130 = 1";
            ExecSql(sql);

            ExecSql("create index ix_t_gil_02 on t_gil(nzp_kvar,nzp_gil)");
            
            ExecSql(DBManager.sUpdStat + " t_gil");

            //Дата начала действия убытия жильца
            sql = "update t_gil set dat_s = (select min(b.dat_s) " +
                  " from " + localPref + DBManager.sDataAliasRest +
                  "gil_periods b where t_gil.nzp_gil=b.nzp_gilec " +
                  " and b.dat_s between t_gil.dat_s and t_gil.dat_po and b.is_actual=1 )";
            ExecSql(sql);

            //Дата начала окончания действия убытия жильца
            sql = "update t_gil set dat_po = (select max(b.dat_po) " +
                  " from " + localPref + DBManager.sDataAliasRest +
                  "gil_periods b where t_gil.nzp_gil=b.nzp_gilec " +
                  " and b.dat_s between t_gil.dat_s and t_gil.dat_po and b.is_actual=1 )";
            ExecSql(sql);

            sql = "update t_gil set nzp_gil = 0 where dat_s is null";
            ExecSql(sql);
            //Выставляем период убытия весь месяц, для тех у кого не заполнен
            //период временного выбытия
            sql = "update t_gil set dat_s = "+periodS+" where dat_s is null";
            ExecSql(sql);

            sql = "update t_gil set dat_po = " + periodPo + " where dat_po is null";
            ExecSql(sql);


            //Добавляем причины по паспортиcтке при временном выбытии
            sql = " Insert into t_reval_reason(year_, month_ , nzp_kvar, num_ls,  " +
                  "     nzp_serv , nzp_supp, nzp_reason, kind_reason, reval,c_reval, dat_s, dat_po, nzp_gil)     " +
                  " select year_, month_ , nzp_kvar, num_ls,  " +
                  "     nzp_serv , nzp_supp, nzp_reason, " + (int)KindReason.RGilVrVib + ", " +
                  "     reval,c_reval, dat_s, dat_po, nzp_gil " +
                  " from t_gil a " +
                  " where prm130 = 1 and nzp_gil >0 ";
            ExecSql(sql);
      
            AnalizeServChange();

            ExecSql("drop table t_gil");
        }


        /// <summary>
        /// Анализ изменения по жильцам или по параметрам произошел перерасчет
        /// </summary>
        private void AnalizeServChange()
        {

            // Определяем ЛС, где стоял признак перерасчета по жильцам
            ExecSql("drop table tg");
            string sql = "Create temp table tg (" +
                         " nzp_kvar integer, " +
                         " nzp_serv integer," +
                         " nzp_prm integer," +
                         " nzp_gil integer) ";
            ExecSql(sql);
            _tempTableList.Add("tg");

            sql = " insert into tg(nzp_kvar, nzp_serv, nzp_prm, nzp_gil)" +
                  " select a.nzp_kvar, a.nzp_serv, 0 as nzp_prm, 0 as nzp_gil " +
                  " from local_must a, t_gil b" +
                  " where a.nzp_kvar=b.nzp_kvar " +
                  "       and a.nzp_serv=b.nzp_serv" +
                  "       and a.kod1 = 6 " +
                  " group by 1,2";
            ExecSql(sql);
            // Добавляем те ЛС, которые были пересчитаны в результате
            // пересчета услуги Итого
            sql = " insert into tg (nzp_kvar, nzp_serv, nzp_prm, nzp_gil)" +
                  " select a.nzp_kvar, b.nzp_serv,0,0 " +
                  " from local_must a, t_gil b " +
                  " where a.nzp_kvar=b.nzp_kvar " +
                  "       and a.nzp_serv=1 " +
                  "       and a.kod1 = 6  " +
                  " group by 1,2";
            ExecSql(sql);

            // Определяем ЛС, в которых стоял признак перерасчета по параметрам
            // количество прописанных, врем. выбывших, врем. проживающих
            sql = " insert into tg (nzp_kvar, nzp_serv, nzp_gil, nzp_prm)" +
                  " select a.nzp_kvar, a.nzp_serv,0,max(kod2) " +
                  " from local_must a, t_gil b " +
                  " where a.nzp_kvar=b.nzp_kvar " +
                  "       and a.nzp_serv=b.nzp_serv " +
                  "       and a.kod1 = 1 and a.kod2 in (5,10,131) " +
                  " group by 1,2,3";
            ExecSql(sql);

            // Добавляем ЛС, в которых по услуге Итого произошел перерасчет из за
            // параметров количество прописанных, врем. выбывших, врем. проживающих
            //sql = " insert into tg (nzp_kvar, nzp_serv, nzp_gil, nzp_prm)" +
            //      " select a.nzp_kvar, b.nzp_serv,0, max(kod2) " +
            //      " from local_must a, t_gil b " +
            //      " where a.nzp_kvar=b.nzp_kvar " +
            //      "       and a.nzp_serv=1 " +
            //      "       and a.kod1 = 1 and a.kod2 in (5,10,131) " +
            //      " group by 1,2,3";
            //ExecSql(sql);

            ExecSql("create index ix_tmp_tg_01 on tg(nzp_kvar, nzp_serv)");
            ExecSql(DBManager.sUpdStat + " tg");
      //      DataTable dt = DBManager.ExecSQLToTable(Connection, "select * from tg");
      //      DataTable dt22 = DBManager.ExecSQLToTable(Connection, "select * from t_gil");
            //1. Изменения в составе прописанных
            sql = " Insert into t_reval_reason(year_, month_ , nzp_kvar, num_ls,  " +
                  "     nzp_serv , nzp_supp, nzp_reason, kind_reason, reval,c_reval, dat_s, dat_po, nzp_gil, nzp_prm)     " +
                  " select a.year_, a.month_ , a.nzp_kvar, a.num_ls,  " +
                  "     a.nzp_serv , a.nzp_supp,  a.nzp_reason, " + (int) KindReason.RGilChange +
                  ", a.reval, a.c_reval, a.dat_s, a.dat_po, tg.nzp_gil, tg.nzp_prm " +
                  " from t_gil a, tg " +
                  " where prm130=1 and a.nzp_gil = 0 " +
                  "        and a.nzp_kvar=tg.nzp_kvar " +
                  "                 and a.nzp_serv=tg.nzp_serv ";
            ExecSql(sql);
            //DataTable dt1 = DBManager.ExecSQLToTable(Connection, "select * from t_reval_reason");

            //Изменение в параметрах прописанных
            sql = " Insert into t_reval_reason(year_, month_ , nzp_kvar, num_ls,  " +
                  "     nzp_serv , nzp_supp, nzp_reason, kind_reason, reval,c_reval, dat_s, dat_po, nzp_gil, nzp_prm)     " +
                  " select a.year_, a.month_ , a.nzp_kvar, a.num_ls,  " +
                  "     a.nzp_serv , a.nzp_supp, " + (int) Reason.Prm + ", " + (int) KindReason.RGilChange +
                  ", a.reval, a.c_reval, a.dat_s, a.dat_po, tg.nzp_gil, tg.nzp_prm  " +
                  " from t_gil a, tg " +
                   " where prm130=0 " +
                  "        and a.nzp_kvar=tg.nzp_kvar " +
                  "                 and a.nzp_serv=tg.nzp_serv ";
            ExecSql(sql);

     //       DataTable dt2 = DBManager.ExecSQLToTable(Connection, "select * from t_reval_reason");
            //1. Изменения параметрах
            //Определяем максимальный параметр из тех, что не входят в параметры проживающих
            sql = " select a.nzp_kvar, a.nzp_serv,max(kod2) as nzp_prm " +
                  " into temp tp2"+
                  " from local_must a, t_gil b" +
                  " where a.nzp_kvar=b.nzp_kvar " +
                  "       and a.nzp_serv=b.nzp_serv" +
                  "       and a.kod1 = 1 " +
                  "       and prm130=0   " +
                  "       and not exists (select 1 from tg " +
                  "             where a.nzp_kvar=tg.nzp_kvar " +
                  "                 and a.nzp_serv=tg.nzp_serv)" +
                  " group by 1,2";
            ExecSql(sql);
            _tempTableList.Add("tp2");
      //      DataTable dt3 = DBManager.ExecSQLToTable(Connection, "select * from t_reval_reason");
            ExecSql("create index ix_tmp_tp2_01 on tp2(nzp_kvar, nzp_serv)");
            ExecSql(DBManager.sUpdStat + " tp2");
            // Надо бы обработать услугу ИТОГО


            sql = " Insert into t_reval_reason(year_, month_ , nzp_kvar, num_ls,  " +
                  "     nzp_serv , nzp_supp, nzp_reason, kind_reason, reval,c_reval, dat_s, dat_po, nzp_prm)     " +
                  " select a.year_, a.month_ , a.nzp_kvar, a.num_ls,  " +
                  "     a.nzp_serv , a.nzp_supp,  " + (int) Reason.Prm + ", " + (int) KindReason.RPrmChange +
                  ", a.reval, a.c_reval, a.dat_s, a.dat_po, tp2.nzp_prm " +
                  " from t_gil a left outer join tp2 " +
                  "     on a.nzp_kvar=tp2.nzp_kvar and a.nzp_serv=tp2.nzp_serv" +
                  " where  nzp_gil = 0 " +
                  " and not exists (select 1 from tg " +
                  "             where a.nzp_kvar=tg.nzp_kvar " +
                  "                 and a.nzp_serv=tg.nzp_serv)";
            ExecSql(sql);
       //     DataTable dt4 = DBManager.ExecSQLToTable(Connection, "select * from t_reval_reason");
            ExecSql("drop table tg");
            ExecSql("drop table tp2");
        }


        /// <summary>
        /// Подготавливает таблицу для анализа перерасчетов в одном месяце
        /// </summary>
        /// <param name="localPref">Префикс схемы локального банка</param>
        /// <param name="revalYear">Перерасчитываемый год</param>
        /// <param name="revalMonth">Перерасчитываемый месяц</param>
        private void PrepareOneMonth(string localPref, int revalYear, int revalMonth)
        {
            //1.Выбираем начисления
            string revalCharge = localPref + "_charge_" + (revalYear - 2000).ToString("00") +
                                 DBManager.tableDelimiter + "charge_" + revalMonth.ToString("00");

            //подготавливаем временную таблицу для Анализа
            string sql = "CREATE TEMP TABLE t_revalcharge(" +
                         " nzp_kvar integer," +
                         " nzp_serv integer," +
                         " nzp_supp integer," +
                         " tarif " + DBManager.sDecimalType + "(14,5) default 0," +
                         " tarif_p " + DBManager.sDecimalType + "(14,5) default 0," +
                         " rsum_tarif " + DBManager.sDecimalType + "(14,2) default 0," +
                         " rsum_tarif_p " + DBManager.sDecimalType + "(14,2) default 0," +
                         " sum_nedop " + DBManager.sDecimalType + "(14,2) default 0," +
                         " sum_nedop_p " + DBManager.sDecimalType + "(14,2) default 0," +
                         " reval " + DBManager.sDecimalType + "(14,2) default 0," +
                         " c_calc " + DBManager.sDecimalType + "(14,7) default 0," +
                         " c_calc_p " + DBManager.sDecimalType + "(14,7) default 0," +
                         " is_device integer default 0," +
                         " is_device_p integer default 0," +
                         " reval_group integer default 0)";
            ExecSql(sql);
            _tempTableList.Add("t_revalcharge");

            //Заносим суммы перерасчетов
            sql = "INSERT INTO t_revalcharge(nzp_kvar, nzp_serv, nzp_supp, tarif, tarif_p, " +
                  "  rsum_tarif, rsum_tarif_p, "+
                  "  sum_nedop, sum_nedop_p, reval, " +
                  "  c_calc, is_device)" +
                  " SELECT a.nzp_kvar, a.nzp_serv, a.nzp_supp, tarif, tarif_p, " +
                  " rsum_tarif, sum_tarif_p + sum_nedop_p , "+
                  " sum_nedop, sum_nedop_p, reval + sum_nedop - sum_nedop_p, " +
                  " c_calc, is_device "+
                  " FROM "+revalCharge+" a, t_selkv b " +
                  " WHERE a.nzp_kvar=b.nzp_kvar" +
                  "     AND a.dat_charge = Date('28."+_month+"."+_year+"')";
            ExecSql(sql);
            ExecSql("CREATE INDEX ix_t_revalcharge_01 on t_revalcharge(nzp_kvar, nzp_serv, nzp_supp)");
            ExecSql("CREATE INDEX ix_t_revalcharge_02 on t_revalcharge(nzp_kvar)");
            ExecSql(DBManager.sUpdStat + " t_revalcharge");


            sql = " SELECT nzp_kvar " +
                  " INTO TEMP t_lkv " +
                  " FROM t_revalcharge " +
                  " GROUP BY 1";
            ExecSql(sql);
            _tempTableList.Add("t_lkv");

            ExecSql("CREATE INDEX ix_t_lkv_01 on t_lkv(nzp_kvar)");
            ExecSql(DBManager.sUpdStat + " t_lkv");
            
            //Определяем первоначальный расход

            sql = " SELECT a.nzp_kvar, a.nzp_serv, a.nzp_supp, MAX(" + DBManager.sNvlWord + "(dat_charge, date('01.01.1900'))) as dat_charge " +
                  " INTO TEMP t_maxrev " +
                  " FROM " + revalCharge + " a, t_lkv b " +
                  " WHERE a.nzp_kvar = b.nzp_kvar " +
                  "      AND " + DBManager.sNvlWord + "(dat_charge, date('01.01.1900'))< " +
                  "         Date('28." + _month + "." + _year + "')" +
                  " GROUP BY 1,2,3";
            ExecSql(sql);
            _tempTableList.Add("t_maxrev");

            ExecSql("CREATE INDEX ix_t_maxrev_01 on t_maxrev(nzp_kvar, nzp_serv, nzp_supp, dat_charge)");
            ExecSql(DBManager.sUpdStat + " t_maxrev");

            sql = " SELECT a.nzp_kvar, a.nzp_serv, a.nzp_supp, max(c_calc) as c_calc, max(is_device) as is_device " +
                  " into t_prevRevCharge " +
                  " FROM " + revalCharge + " a, t_maxrev b " +
                  " WHERE a.nzp_kvar=b.nzp_kvar " +
                  "     AND a.nzp_serv=b.nzp_serv " +
                  "     AND a.nzp_supp=b.nzp_supp " +
                  "     AND " + DBManager.sNvlWord + "(a.dat_charge, date('01.01.1900'))= b.dat_charge" +
                  " GROUP BY 1,2,3";
            ExecSql(sql);
            _tempTableList.Add("t_prevRevCharge");

            ExecSql("CREATE INDEX ix_t_lkv_01 ON t_prevRevCharge(nzp_kvar, nzp_serv, nzp_supp)");
            ExecSql(DBManager.sUpdStat + " t_prevRevCharge");

            sql = " UPDATE t_revalcharge " +
                  " SET c_calc_p = a.c_calc, " +
                  " is_device_p = a.is_device" +
                  " FROM t_prevRevCharge a" +
                  " WHERE t_revalcharge.nzp_kvar=a.nzp_kvar " +
                  "     AND t_revalcharge.nzp_serv=a.nzp_serv " +
                  "     AND t_revalcharge.nzp_supp=a.nzp_supp ";
            ExecSql(sql);

            ExecSql("DROP TABLE t_prevRevCharge");
            ExecSql("DROP TABLE t_maxrev");
            ExecSql("DROP TABLE t_lkv");

     

        }


    }

}
