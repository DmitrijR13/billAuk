using System;
using System.Data;
using STCLINE.KP50.DataBase;

namespace STCLINE.KP50.IFMX.Report.SOURCE.Samara
{
    /// <summary>
    /// Класс отвечающий за получения данных по недопоставкам
    /// для Самары
    /// </summary>
    public class SamaraReportNedop : SamaraBaseReportClass
    {
        private string _curPref;
        private string _datMonth;
        private string _dbCharge;
        private int _month;
        private int _year;

        public SamaraReportNedop(IDbConnection connDb, string adrTempTable) :
            base(connDb, adrTempTable)
        {
     
        }


        /// <summary>
        /// Заполняет таблицу t_sum_nedo суммами
        /// текущей недопоставки
        /// </summary>
        private void SetCurrentMonthNedop()
        {
            string dateEnd = DateTime.DaysInMonth(_year, _month) + "." + _month + "." + _year;
            string sql = " INSERT INTO t_sum_nedo(nzp_kvar, nzp_supp, nzp_serv, nzp_frm, dat_month," +
                         " dat_month_end, tarif, sum_nedop)" +
                         " SELECT  s.nzp_kvar, c.nzp_supp, c.nzp_serv, c.nzp_frm, " +
                         "        '" + _datMonth + "' as dat_month,  " +
                         "        '" + dateEnd + "' as dat_month_end,  " +
                         "        max(tarif) as tarif,"+
                         "        sum(c.sum_nedop) as sum_nedop " +
                         " FROM " + AdrTempTable + " s , " +
                         _dbCharge + "charge_" + _month.ToString("00") + " c " +
                         " WHERE s.nzp_kvar = c.nzp_kvar " +
                         "       AND c.nzp_serv > 1  " +
                         "       AND c.dat_charge is null " +
                         "       AND c.sum_nedop > 0.001 " +
                         " GROUP BY 1,2,3,4,5,6 ";
            RunSql(sql, true);
        }

        /// <summary>
        /// Заполняет таблицу t_sum_nedo суммами
        /// недопоставки предыдущих периодов
        /// </summary>
        private void SetPreviosMonthNedop()
        {

            string insSql = " INSERT INTO t_sum_nedo (nzp_kvar, nzp_supp, nzp_serv, nzp_frm, " +
                            " dat_month, dat_month_end, tarif, sum_nedop, sum_nedop_p) " +
                            " SELECT d.nzp_kvar, b.nzp_supp, b.nzp_serv, b.nzp_frm, " +
                            "        '{0}', '{1}', max(tarif), " +
                            "        sum(sum_nedop-sum_nedop_p),  " +
                            "        sum(sum_nedop-sum_nedop_p)  " +
                            " FROM {2} b, " + AdrTempTable + " d " +
                            " WHERE  b.nzp_kvar=d.nzp_kvar " +
                            "        AND  b.dat_charge = date('28." + _month + "." + _year + "') " +
                            "        AND abs(b.sum_nedop - b.sum_nedop_p)>0.001" +
                            " GROUP BY 1,2,3,4,5,6";

            if (ConnDb == null)
            {
                RunSql(String.Format(insSql,"01."+Math.Max(_month-1,1)+".2013",
                    "30." + Math.Max(_month - 1, 1) + ".2013", _dbCharge + "charge_09"), true);
                return;
            }

            string sql = " SELECT month_, year_ " +
                         " FROM " + _dbCharge + "lnk_charge_" + _month.ToString("00") + " b, " +
                         AdrTempTable + " d " +
                         " WHERE  b.nzp_kvar=d.nzp_kvar " +
                         " GROUP BY 1,2";


            DataTable dt = DBManager.ExecSQLToTable(ConnDb, sql);
            foreach (DataRow dr in dt.Rows)
            {
                int revalYear = Int32.Parse(dr["year_"].ToString());
                int revalMonth = Int32.Parse(dr["month_"].ToString());
                string sTmpAlias = _curPref + "_charge_" + (revalYear - 2000).ToString("00") + DBManager.tableDelimiter;

                string dateStart = "'01." + _month + "." + _year + "'";
                string dateEnd = "'" + DateTime.DaysInMonth(revalYear, revalMonth) + "." + _month + "." + _year + "'";
                string tableCharge =sTmpAlias + "charge_" + revalMonth.ToString("00") ;

                RunSql(String.Format(insSql, dateStart, dateEnd, tableCharge), true);

            }

        }

        /// <summary>
        /// Заполняет таблицу t_sum_nedo суммами
        /// недопоставки 
        /// </summary>
        private void CreateSumNedopTable()
        {
            RunSql(" Drop table t_sum_nedo ", false);


            const string sql = "CREATE TEMP TABLE t_sum_nedo (" +
                               " nzp_kvar integer, " +
                               " nzp_supp integer, " +
                               " nzp_serv integer, " +
                               " nzp_frm integer, " +
                               " dat_month date, " +
                               " dat_month_end date, " +
                               " tarif " + DBManager.sDecimalType + "(14,4), " +
                               " sum_nedop_p " + DBManager.sDecimalType + "(14,2), " + 
                               " sum_nedop "+DBManager.sDecimalType+"(14,2)) " + 
                               DBManager.sUnlogTempTable;
            RunSql(sql, true);

            SetCurrentMonthNedop();

            SetPreviosMonthNedop();

            RunSql("create index ix_tmp753 on t_sum_nedo(nzp_kvar, nzp_serv) ", true);
            RunSql(DBManager.sUpdStat + " t_sum_nedo ", true);
        }

        /// <summary>
        /// Определяется виновник недопоставки и продолжительность 
        /// недопоставки
        /// </summary>
        private void CreateVinovnikTable()
        {
            RunSql("drop table t_vinovnik", false);

            string sql = "CREATE TEMP TABLE t_vinovnik ("+
                " nzp_kvar integer, "+
                " nzp_serv integer, "+
                " vinovnik integer, "+
                " nzp_serv_sg integer, "+
                " dat_month date, "+
                " day_nedo "+DBManager.sDecimalType+"(14,2))"+
                DBManager.sUnlogTempTable;
            RunSql(sql, true);
#if PG
           sql ="insert into t_vinovnik(nzp_kvar, nzp_serv, vinovnik,nzp_serv_sg, dat_month, day_nedo) " +
                             " select  a.nzp_kvar, a.nzp_serv, a.nzp_supp as vinovnik, " +
                             " MDY(date_part('month',dat_s)::int,01,date_part('year',dat_s)::int) as dat_month," +
                             " round((((date_part('day',dat_po-dat_s)*24)+date_part('hour',dat_po-dat_s))*60+" +
                             " date_part('minute',dat_po-dat_s))::numeric/1440,2) as day_nedo " +
                             " from   " + _curPref + "_data.nedop_kvar a, " + AdrTempTable + " d " +
                             " where a.nzp_kvar=d.nzp_kvar " +
                             " and a.month_calc = date('" + _datMonth+ "') ";

#else
            sql = " INSERT INTO t_vinovnik(nzp_kvar, nzp_serv, nzp_serv_sg, vinovnik, dat_month, day_nedo) " +
                  " SELECT  a.nzp_kvar, a.nzp_serv, a.nzp_supp as vinovnik, " +
                  "         (case when a.nzp_serv=17 then u.step else  a.nzp_serv end ) as nzp_serv_sg, " +
                  "         MDY(month(dat_s),01,year(dat_s)) as dat_month," +
                  "         Round(((cast( dat_po  - dat_s " +
                  "         as interval minute(6) to minute)||'')+0)/1440,2) as day_nedo " +
                  " FROM   " + _curPref + DBManager.sDataAliasRest + "nedop_kvar a, " +
                  AdrTempTable + " d, " +
                  _curPref + DBManager.sDataAliasRest + "upg_s_nedop_type u" +
                  " WHERE  a.nzp_kvar=d.nzp_kvar " +
                  "         AND a.nzp_kind=u.nzp_nedop_type " +
                  "         AND a.month_calc = date('" + _datMonth + "') " +
                  "         AND is_actual = 1 ";
#endif
            RunSql(sql, true);
            
            
            RunSql("drop table t_alldaynedo", false);
            //Вычисляем общее число дней недопоставки
               sql = "CREATE TEMP TABLE t_alldaynedo (" +
                     " nzp_kvar integer, " +
                     " nzp_serv integer, " +
                     " dat_month date, " +
                     " count_daynedo " + DBManager.sDecimalType + "(14,2))" + 
                     DBManager.sUnlogTempTable;
            RunSql(sql, true);

            sql = " insert into t_alldaynedo (nzp_kvar, nzp_serv, dat_month, count_daynedo) " +
                  " select  nzp_kvar,nzp_serv, dat_month," +
                  "         sum(day_nedo) as count_daynedo " +
                  " from  t_vinovnik group by 1,2,3";
            RunSql(sql, true);
        }

        /// <summary>
        /// Определяется количество проживающих
        /// </summary>
        private void SetCountGil()
        {
            //вставка количества жильцов
            string sql = " update  t_sprav_otkl_usl set col_gil = (   " +
                         " select  sum(cnt2+val3-val5)  " +
                         " from  " + _dbCharge + "gil_" + _month.ToString("00") +" a"+
                         " where t_sprav_otkl_usl.nzp_kvar = a.nzp_kvar and a.stek=3)  ";
            RunSql(sql, true);
        }

        /// <summary>
        /// Сторнируется "копейка" при наличии нескольких виновников
        /// или статей калькуляции
        /// </summary>
        private void SetStornoSumNedop()
        {
            RunSql("drop table t_corr", false);
            string sql = " CREATE TEMP TABLE t_corr (" +
                         " nzp_kvar integer, " +
                         " nzp_serv integer, " +
                         " nzp_serv_sg integer, " +
                         " max_vin integer, " +
                         " dat_month date, " +
                         " sum_nedop " + DBManager.sDecimalType + "(14,2), " +
                         " sum_nedop_p " + DBManager.sDecimalType + "(14,2), " +
                         " count_daynedo " + DBManager.sDecimalType + "(14,2))" + 
                         DBManager.sUnlogTempTable;
            RunSql(sql, true);

            sql = " INSERT INTO t_corr(nzp_kvar, nzp_serv,  dat_month, nzp_serv_sg," +
                  "         sum_nedop, sum_nedop_p, max_vin, count_daynedo )" +
                  " SELECT nzp_kvar, nzp_serv,  dat_month, max(nzp_serv_sg)," +
                  "        max(sum_nedop_all) - sum(sum_nedop) as sum_nedop," +
                  "        max(sum_nedop_all_p) - sum(sum_nedop_p) as sum_nedop_p," +
                  "        max(nzp_vinovnik) as max_vin, " +
                  "        max(count_day_all) - sum(count_daynedo) as day_nedo " +
                  " FROM t_sprav_otkl_usl " +
                  " GROUP BY 1,2,3 ";
            RunSql(sql, true);

            sql = " INSERT INTO t_sprav_otkl_usl(nzp_kvar, nzp_serv, nzp_serv_sg, dat_month, " +
                  "        sum_nedop, sum_nedop_p, nzp_vinovnik, count_daynedo, col_gil)" +
                  " SELECT nzp_kvar, nzp_serv, nzp_serv_sg, dat_month, " +
                  "         sum_nedop, sum_nedop_p, max_vin,  count_daynedo, 0 " +
                  " FROM t_corr ";
            RunSql(sql, true);

            RunSql("drop table t_corr", true);
        }


        /// <summary>
        /// Выбирается основной массив данных 
        /// в таблицу t_sprav_otkl_usl
        /// </summary>
        private void FillMainNedopTable()
        {

            RunSql(" Drop table t_sprav_otkl_usl ", false);
            string sql = " create temp table t_sprav_otkl_usl (" +
                         " nzp_kvar          INTEGER, " +
                         " nzp_supp          INTEGER, " +
                         " nzp_vinovnik      INTEGER, " +
                         " nzp_serv          INTEGER, " +
                         " nzp_serv_sg       INTEGER, " +
                         " nzp_frm           INTEGER, " +
                         " dat_month         DATE, " +
                         " countKvar         INTEGER, " +
                         " tarif             " + DBManager.sDecimalType + "(14,4), " +
                         " count_daynedo     " + DBManager.sDecimalType + "(14,2), " +
                         " count_day_all     " + DBManager.sDecimalType + "(14,2), " +
                         " count_kvarchas    INTEGER DEFAULT 0, " +
                         " col_gil           INTEGER, " +
                         " sum_nedop         " + DBManager.sDecimalType + "(14,2), " +
                         " sum_nedop_all     " + DBManager.sDecimalType + "(14,2), " +
                         " sum_nedop_all_p   " + DBManager.sDecimalType + "(14,2), " +
                         " sum_nedop_p     " + DBManager.sDecimalType + "(14,2) " +
                         " )  " + DBManager.sUnlogTempTable;
            RunSql(sql, true);

            sql = " INSERT INTO t_sprav_otkl_usl (nzp_kvar, nzp_supp, nzp_vinovnik," +
                  "        nzp_serv, nzp_serv_sg, nzp_frm, count_daynedo, sum_nedop, sum_nedop_p, " +
                  "        sum_nedop_all, sum_nedop_all_p, tarif," +
                  "        count_day_all, dat_month) " +
                  " SELECT a.nzp_kvar, a.nzp_supp, b.vinovnik, a.nzp_serv, b.nzp_serv_sg, a.nzp_frm, b.day_nedo,  " +
                  "        (case when d.count_daynedo>0 then a.sum_nedop * b.day_nedo / d.count_daynedo else 0 end)," +
                  "        (case when d.count_daynedo>0 then a.sum_nedop_p * b.day_nedo / d.count_daynedo else 0 end)," +
                  "        a.sum_nedop, a.sum_nedop_p, a.tarif, d.count_daynedo, a.dat_month  " +
                  " FROM t_sum_nedo a, " +
                  "      t_vinovnik b, " +
                  "      t_alldaynedo d" +
                  " WHERE a.nzp_kvar=b.nzp_kvar " +
                  "         AND a.nzp_kvar=d.nzp_kvar " +
                  "         AND a.nzp_serv=d.nzp_serv " +
                  "         AND a.dat_month=d.dat_month " +
                  "         AND a.dat_month=b.dat_month " +
                  "         AND a.nzp_serv=b.nzp_serv ";
            RunSql(sql, true);

            RunSql("create index ix_tmp756 on t_sprav_otkl_usl(nzp_kvar) ", true);
            RunSql(DBManager.sUpdStat + " t_sprav_otkl_usl ", true);
        }


        private void SetHvsGvsAsGvs()
        {
            RunSql("drop table t_gvs", false);
            string sql = " CREATE TEMP TABLE  t_gvs(" +
                 " nzp_kvar integer, " +
                 " nzp_supp integer)" + DBManager.sUnlogTempTable;
            RunSql(sql, true);

            sql = " INSERT INTO t_gvs (nzp_kvar, nzp_supp)" +
                  " SELECT nzp_kvar, nzp_supp "+
                  " FROM  t_sprav_otkl_usl a " +
                  " WHERE nzp_serv=9 "+
                  " GROUP BY 1,2 ";
            RunSql(sql, true);

            sql = " UPDATE t_sprav_otkl_usl set nzp_serv=9 " +
                  " WHERE nzp_serv=14 and 0<(select count(*) " +
                  " FROM t_gvs  " +
                  " WHERE t_sprav_otkl_usl.nzp_kvar=t_gvs.nzp_kvar " +
                  "        AND t_sprav_otkl_usl.nzp_supp=t_gvs.nzp_supp) ";
            RunSql(sql, true);
            RunSql("drop table t_gvs", true);
        }

        /// <summary>
        /// Формируется результирующая таблица 
        /// t_nedop_kvar по лицевым счетам
        /// </summary>
        private void CreateResultNedopTable()
        {
            RunSql("drop table t_nedop_kvar", false);
            string sql = " CREATE TEMP TABLE  t_nedop_kvar(" +
                 " nzp_kvar integer, " +
                 " nzp_serv integer, " +
                 " nzp_serv_sg integer, " +
                 " nzp_supp integer, " +
                 " nzp_frm integer, " +
                 " nzp_vinovnik integer," +
                 " tarif " + DBManager.sDecimalType + "(10,4)," +
                 " count_daynedo " + DBManager.sDecimalType + "(10,2)," +
                 " count_gil integer, " +
                 " sum_nedop " + DBManager.sDecimalType + "(14,2)," +
                 " sum_nedop_p " + DBManager.sDecimalType + "(14,2))" + 
                 DBManager.sUnlogTempTable;
            RunSql(sql, true);


            sql = " INSERT INTO t_nedop_kvar (nzp_kvar, nzp_serv, nzp_serv_sg, nzp_supp, nzp_vinovnik, " +
                  "         nzp_frm, count_daynedo, count_gil, tarif, sum_nedop, sum_nedop_p )" +
                  " SELECT nzp_kvar, nzp_serv, nzp_serv_sg, nzp_supp, nzp_vinovnik, nzp_frm,  " +
                  "         sum(count_daynedo) as count_daynedo, max(col_gil) as count_gil, " +
                  "         max(tarif) as tarif, sum(sum_nedop) as sum_nedop, " +
                  "         sum(sum_nedop_p) as sum_nedop_p " +
                  " FROM  t_sprav_otkl_usl a " +
                  " GROUP BY 1,2,3,4,5,6 ";
            RunSql(sql, true);
        }

        /// <summary>
        /// Удаление временных таблиц, используемых
        /// в работе
        /// </summary>
        private void DropTempTable()
        {
            RunSql("drop table t_sum_nedo", true);
            RunSql("drop table t_vinovnik", true);
            RunSql("drop table t_alldaynedo", true);
            RunSql("drop table t_sprav_otkl_usl", true);
        }

        /// <summary>
        /// Основная процедура получения данных по недопоставкам 
        /// </summary>
        /// <param name="pref"></param>
        /// <param name="year"></param>
        /// <param name="month"></param>
        private void GetSumNedop(string pref, int year, int month)
        {
            _month = month;
            _year = year;
            _datMonth = "01." + month.ToString("00") + "." + year;
            _curPref = pref;
            _dbCharge = _curPref + "_charge_" + (year - 2000) + DBManager.tableDelimiter;
            

            CreateSumNedopTable();
            CreateVinovnikTable();
            FillMainNedopTable();
            SetCountGil();
            SetStornoSumNedop();
            SetHvsGvsAsGvs();
            CreateResultNedopTable();
            DropTempTable();
        }

        /// <summary>
        /// Формирует временную таблицу t_nedop_kvar с недопоставками
        /// поля nzp_kvar, nzp_serv, nzp_serv_sg - статья калькуляции
        /// nzp_supp, nzp_vinovnik - виновник недопоставки
        /// count_daynedo -количество дней недопоставки
        /// count_gil -количество проживающих
        /// sum_nedop- сумма недопоставки
        /// </summary>
        /// <param name="pref"></param>
        /// <param name="year">Год</param>
        /// <param name="month">Месяц</param>
        public void GetNedopByKvar(string pref, int year, int month)
        {
            GetSumNedop(pref, year, month);
        }

        /// <summary>
        /// Формирует временную таблицу t_nedop_kvar с недопоставками
        /// поля nzp_dom, nzp_serv, nzp_serv_sg - статья калькуляции
        /// nzp_supp, nzp_vinovnik - виновник недопоставки
        /// count_daynedo -количество дней недопоставки
        /// count_gil -количество проживающих
        /// sum_nedop- сумма недопоставки
        /// </summary>
        /// <param name="pref"></param>
        /// <param name="year">Год</param>
        /// <param name="month">Месяц</param>
        public void GetNedopByDom(string pref, int year, int month)
        {
            GetSumNedop(pref, year, month);
            RunSql("drop table t_nedop_dom", true);
            string sql = " CREATE TEMP TABLE  t_nedop_dom(" +
                 " nzp_dom integer, " +
                 " count_ls integer, " +
                 " nzp_serv integer, " +
                 " nzp_serv_sg integer, " +
                 " nzp_supp integer, " +
                 " nzp_vinovnik integer, " +
                 " nzp_frm integer, " +
                 " count_daynedo " + DBManager.sDecimalType + "(10,2)," +
                 " count_gil integer, " +
                 " sum_nedop " + DBManager.sDecimalType + "(14,2), " +
                 " sum_nedop_p " + DBManager.sDecimalType + "(14,2))" + 
                 DBManager.sUnlogTempTable;
            RunSql(sql, true);


            sql = " INSERT INTO t_nedop_dom (nzp_dom, nzp_serv, nzp_serv_sg, nzp_supp, nzp_vinovnik" +
                  "         nzp_frm, count_ls, count_daynedo, count_gil, sum_nedop, sum_nedop_p )" +
                  " SELECT  b.nzp_dom, a.nzp_serv, a.nzp_serv_sg, a.nzp_supp, a.nzp_vinovnik, a.nzp_frm, count(a.nzp_kvar) " +
                  "         sum(a.count_daynedo) as count_daynedo, sum(a.col_gil) as count_gil, " +
                  "         sum(a.sum_nedop) as sum_nedop, " +
                  "         sum(a.sum_nedop_p) as sum_nedop_p " +
                  " FROM  t_nedop_kvar a, " + AdrTempTable + " b" +
                  " WHERE a.nzp_kvar=b.nzp_kvar "+
                  " GROUP BY 1,2,3,4,5 ";
            RunSql(sql, true);
        }

        /// <summary>
        /// Формирует временную таблицу t_nedop_kvar с недопоставками
        /// поля nzp_area, nzp_geu, nzp_serv, nzp_serv_sg - статья калькуляции
        /// nzp_supp, nzp_vinovnik - виновник недопоставки
        /// count_daynedo -количество дней недопоставки
        /// count_gil -количество проживающих
        /// sum_nedop- сумма недопоставки
        /// </summary>
        /// <param name="pref"></param>
        /// <param name="year">Год</param>
        /// <param name="month">Месяц</param>
        public void GetNedopByGeu(string pref, int year, int month)
        {
            GetSumNedop(pref, year, month);
            RunSql("drop table t_nedop_geu", true);
            string sql = " CREATE TEMP TABLE  t_nedop_geu(" +
                 " nzp_area integer, " + 
                 " nzp_geu integer, " +
                 " count_ls integer, " +
                 " nzp_serv integer, " +
                 " nzp_serv_sg integer, " +
                 " nzp_supp integer, " +
                 " nzp_vinovnik integer," +
                 " nzp_frm integer, " +
                 " count_daynedo " + DBManager.sDecimalType + "(10,2)," +
                 " count_gil integer, " +
                 " sum_nedop " + DBManager.sDecimalType + "(14,2)," +
                 " sum_nedop_p " + DBManager.sDecimalType + "(14,2))" +
                 DBManager.sUnlogTempTable;
            RunSql(sql, true);


            sql = " INSERT INTO t_nedop_dom (nzp_area, nzp_geu, nzp_serv, nzp_serv_sg, nzp_supp, nzp_vinovnik" +
                  "         nzp_frm, count_ls, count_daynedo, count_gil, sum_nedop, sum_nedop_p )" +
                  " SELECT  b.nzp_area, b.nzp_geu, a.nzp_serv, a.nzp_serv_sg, a.nzp_supp, a.nzp_vinovnik, " +
                  "         a.nzp_frm,count(a.nzp_kvar), sum(a.count_daynedo) as count_daynedo, " +
                  "         sum(a.col_gil) as count_gil, " +
                  "         sum(a.sum_nedop) as sum_nedop, sum(0) " +
                  " FROM  t_nedop_kvar a, " + AdrTempTable + " b" +
                  " WHERE a.nzp_kvar=b.nzp_kvar " +
                  " GROUP BY 1,2,3,4,5,6,7 ";
            RunSql(sql, true);
        }

        /// <summary>
        /// Удаляет все временные таблицы 
        /// </summary>
        public void Close()
        {
            RunSql("drop table t_nedop_kvar", false);
            RunSql("drop table t_nedop_dom", false);
            RunSql("drop table t_nedop_geu", false);
        }


    }
}
