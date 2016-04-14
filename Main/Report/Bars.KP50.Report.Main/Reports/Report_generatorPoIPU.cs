using Bars.KP50.Report.Base;
using Bars.KP50.Report.Main.Properties;
using STCLINE.KP50.DataBase;
using System;
using System.Collections.Generic;
using System.Data;

namespace Bars.KP50.Report.Main.Reports
{
    class GenBigIPU : BaseSqlReport
    {
        public override string Name
        {
            get { return "Генератор по большим расходам ИПУ"; }
        }

        public override string Description
        {
            get { return "Генератор по большим расходам ИПУ"; }
        }

        public override IList<ReportGroup> ReportGroups
        {
            get
            {
                var result = new List<ReportGroup> { ReportGroup.Finans };
                return result;
            }
        }

        public override bool IsPreview
        {
            get { return false; }
        }

        protected override byte[] Template
        {
            get { return Resources.GenBigIPU; }
        }

        public override IList<ReportKind> ReportKinds
        {
            get { return new List<ReportKind> { ReportKind.Base }; }
        }

        /// <summary>Месяц</summary>
        private int Month { get; set; }

        /// <summary>Год</summary>
        private int Year { get; set; }

        /// <summary>С учетом среднего значения по расходу</summary>
        private bool IsAverageValue { get; set; }


        public override List<UserParam> GetUserParams()
        {
            var curCalcMonthYear = DBManager.GetCurMonthYear();
            return new List<UserParam>
            {
                new MonthParameter {Value = curCalcMonthYear != null ? curCalcMonthYear.Rows[0]["month_"] : DateTime.Today.Month },
                new YearParameter {Value = curCalcMonthYear != null ? curCalcMonthYear.Rows[0]["yearr"] : DateTime.Today.Year },
                new ComboBoxParameter(false)
                {
                    Name = "С учетом среднего значения по расходу?",
                    Code = "AverageValue",
                    TypeValue = typeof(int),
                    Value = 1,
                    StoreData = new List<object>
                    {
                        new { Id = "1", Name = "Да"},
                        new { Id = "0", Name = "Нет"}
                    }
                }
            };
        }

        protected override void PrepareReport(FastReport.Report report)
        {
            var months = new[] {"","Январь","Февраль",
                 "Март","Апрель","Май","Июнь","Июль","Август","Сентябрь",
                 "Октябрь","Ноябрь","Декабрь"};

            report.SetParameterValue("dat", DateTime.Now.ToShortDateString());
            report.SetParameterValue("time", DateTime.Now.ToShortTimeString());

            report.SetParameterValue("date", months[Month]);
        }


        protected override void PrepareParams()
        {
            Month = UserParamValues["Month"].GetValue<int>();
            Year = UserParamValues["Year"].GetValue<int>();
            IsAverageValue = Convert.ToBoolean(UserParamValues["AverageValue"].GetValue<int>());
        }

        public override DataSet GetData()
        {

            var ds = new DataSet();
            MyDataReader reader;
            string gPrefData = ReportParams.Pref + DBManager.sDataAliasRest,
                    gPrefKernel = ReportParams.Pref + DBManager.sKernelAliasRest;

            string datS = "1." + Month + "." + Year,
                    datPo = DateTime.DaysInMonth(Year,Month) + "." + Month + "." + Year;

//#if PG
//            const string first2 = "", limit2 = " LIMIT 2 " ;
//            const string skip1 = "", offset1 = " OFFSET 1 ";
//#else
//            const string first2 = " FIRST 2 ", limit2 = "" ;
//            const string skip1 = " SKIP 1 ", offset1 = "";
//#endif


            string sql = " SELECT TRIM(bd_kernel) AS bd_kernel " +
                         " FROM  " + ReportParams.Pref + DBManager.sKernelAliasRest + "s_point " +
                         " WHERE nzp_wp > 1";
            ExecRead(out reader, sql);

            while (reader.Read())
            {
                string pref = reader["bd_kernel"].ToString().ToLower().Trim();
                string chargeYY = pref + "_charge_" + (Year - 2000).ToString("00") + DBManager.tableDelimiter +
                                        "charge_" + Month.ToString("00");
                string lPrefData = pref + DBManager.sDataAliasRest;
                if (TempTableInWebCashe(chargeYY))
                {

                    sql = " INSERT INTO t_big_ipu (pref,service,rashod,nzp_serv,fio,nzp_kvar) " +
                          " SELECT DISTINCT '" + pref + "' AS pref, " +
                                 " s.service_name AS service, " +
                                 " cr.c_calc rashod, " +
                                 " cr.nzp_serv nzp_serv, " +
                                 " k.fio, " +
                                 " k.nzp_kvar " +
                          " FROM " + chargeYY + " cr LEFT OUTER JOIN " + gPrefData + "kvar k ON cr.nzp_kvar = k.nzp_kvar " +
                                                   " LEFT OUTER JOIN " + gPrefKernel + "services s ON s.nzp_serv = cr.nzp_serv " +
                          " WHERE cr.nzp_serv IN (6,9,10,25) " +
                            " AND EXISTS(SELECT * " +
                                       " FROM " + lPrefData + "counters c " +
                                       " WHERE is_actual <> 100 " +
                                         " AND c.nzp_kvar = cr.nzp_kvar " +
                                         " AND c.nzp_serv = cr.nzp_serv) " +
                            " AND cr.c_calc > (SELECT " + DBManager.First1 + " val_prm " + DBManager.sConvToNum +
                                             " FROM " + lPrefData + "prm_10 " +
                                             " WHERE is_actual <> 100 " +
                                               " AND dat_s <= DATE('" + datPo + "') " +
                                               " AND dat_po >= DATE('" + datS + "') " +
                                               " AND nzp_prm = (CASE cr.nzp_serv WHEN 6 THEN 2083 " +
                                                                               " WHEN 9 THEN 2082 " +
                                                                               " WHEN 10 THEN 2084 " +
                                                                               " WHEN 25 THEN 2081 END) " +
                                             " ORDER BY dat_s DESC " + DBManager.Limit1 + " ) ";
                    ExecSQL(sql);
                    ExecSQL(DBManager.sUpdStat + " t_big_ipu");

                    sql = " UPDATE t_big_ipu tbi SET address = " +
                          " (SELECT (CASE WHEN TRIM(rajon) <> '-' AND TRIM(rajon) <> '' THEN TRIM(rajon) ELSE TRIM(town) END) || " +
                                  " (CASE WHEN TRIM(ulica) <> '-' AND TRIM(ulica) <> '' THEN ', ' || TRIM(ulica) ELSE '' END) || " +
                                  " (CASE WHEN TRIM(ndom) IS NULL OR TRIM(ndom) = '' OR TRIM(ndom) = '-' THEN '' ELSE ', ' || TRIM(ndom) END) || " +
                                  " (CASE WHEN TRIM(nkor) IS NULL OR TRIM(nkor) = '' OR TRIM(nkor) = '-' THEN '' ELSE ' корп. ' || TRIM(nkor) END) || " +
                                  " (CASE WHEN TRIM(k.nkvar) IS NULL OR TRIM(k.nkvar) = '' OR TRIM(k.nkvar) = '-' THEN '' ELSE ', кв. ' || TRIM(k.nkvar) END) " +
                           " FROM " + gPrefData + "kvar k INNER JOIN " + gPrefData + "dom d  ON k.nzp_dom = d.nzp_dom " +
                                                        " INNER JOIN " + gPrefData + "s_ulica u ON u.nzp_ul = d.nzp_ul " +
                                                        " INNER JOIN  " + gPrefData + "s_rajon r ON r.nzp_raj = u.nzp_raj " +
                                                        " INNER JOIN  " + gPrefData + "s_town t ON t.nzp_town = r.nzp_town " +
                           " WHERE tbi.nzp_kvar = k.nzp_kvar ) " +
                          " WHERE pref = '" + pref + "' ";
                    ExecSQL(sql);

                    sql = " UPDATE t_big_ipu tbi SET initial_reading = " +
                          " (SELECT " + DBManager.First1 + " val_cnt" + DBManager.sConvToNum +
                           " FROM " + lPrefData + "counters " +
                           " WHERE nzp_kvar = tbi.nzp_kvar " +
                             " AND is_actual <> 100 " +
                             " AND nzp_serv = tbi.nzp_serv " +
                             " AND dat_uchet < DATE('" + datS + "') " +
                           " ORDER BY dat_uchet DESC, val_cnt DESC " + DBManager.Limit1 + " ) " +
                          " WHERE initial_reading IS NULL " +
                            " AND pref = '" + pref + "' ";
                    ExecSQL(sql);

                    sql = " UPDATE t_big_ipu tbi SET ending_reading = " +
                          " (SELECT " + DBManager.First1 + " val_cnt" + DBManager.sConvToNum +
                           " FROM " + lPrefData + "counters " +
                           " WHERE nzp_kvar = tbi.nzp_kvar " +
                            " AND is_actual <> 100 " +
                            " AND nzp_serv = tbi.nzp_serv " +
                            " AND dat_uchet BETWEEN DATE('" + datS + "') " +
                                              " AND DATE('" + datPo + "') " +
                           " ORDER BY dat_uchet DESC, val_cnt DESC " + DBManager.Limit1 + " ) " +
                          " WHERE ending_reading IS NULL " +
                            " AND pref = '" + pref + "' ";
                    ExecSQL(sql);

                    if (!IsAverageValue)
                        ExecSQL("DELETE FROM t_big_ipu WHERE ending_reading IS NULL");


                    //sql = " INSERT INTO t_big_ipu_pokaz(nzp_kvar, nzp_serv, val_cnt) " +
                    //      " SELECT nzp_kvar, nzp_serv, val_cnt " + DBManager.sConvToNum +
                    //      " FROM " + lPrefData + "counters gc " +
                    //      " WHERE is_actual <> 100 " +
                    //        " AND nzp_serv IN (6,9,10,25) " +
                    //        " AND nzp_cr IN (SELECT " + first2 + " nzp_cr " +
                    //                       " FROM " + lPrefData + "counters lc " +
                    //                       " WHERE is_actual <> 100 " +
                    //                         " AND lc.nzp_kvar = gc.nzp_kvar " +
                    //                         " AND lc.nzp_serv = gc.nzp_serv " +
                    //                       " ORDER BY dat_uchet DESC, val_cnt DESC " + limit2 + " ) " + 
                    //        " AND nzp_kvar IN (SELECT nzp_kvar FROM t_big_ipu) ";
                    //ExecSQL(sql);

                    //sql = " UPDATE t_big_ipu tbi SET initial_reading = " +
                    //      " (SELECT " + skip1 + first2 + " val_cnt " + 
                    //       " FROM t_big_ipu_pokaz t " +
                    //       " WHERE t.nzp_kvar = tbi.nzp_kvar " +
                    //         " AND t.nzp_serv = tbi.nzp_serv " +
                    //       " ORDER BY val_cnt DESC " + limit2 + offset1 + " ) " +
                    //      " WHERE initial_reading IS NULL " +
                    //        " AND pref = '" + pref + "' ";
                    //ExecSQL(sql);

                    //sql = " UPDATE t_big_ipu tbi SET ending_reading = " +
                    //      " (SELECT " + DBManager.First1 + " val_cnt " +
                    //       " FROM t_big_ipu_pokaz t " +
                    //       " WHERE t.nzp_kvar = tbi.nzp_kvar " +
                    //        " AND t.nzp_serv = tbi.nzp_serv " +
                    //       " ORDER BY val_cnt DESC " + DBManager.Limit1 + ") " +
                    //      " WHERE ending_reading IS NULL " +
                    //        " AND pref = '" + pref + "' ";
                    //ExecSQL(sql);

                    //ExecSQL(" DELETE FROM t_big_ipu_pokaz ");
                }
            }
            sql = "SELECT DISTINCT * FROM t_big_ipu WHERE nzp_kvar IS NOT NULL ORDER BY fio, address";
            DataTable dt = ExecSQLToTable(sql);

            dt.TableName = "Q_master";
            ds.Tables.Add(dt);
            return ds;
        }

        protected override void CreateTempTable()
        {
            string sql = " CREATE TEMP TABLE t_big_ipu ( " +
                         " pref CHAR(30), " +
                         " nzp_kvar INTEGER, " +
                         " address CHAR(200), " +
                         " fio CHAR(200), " +
                         " service CHAR(100)," +
                         " nzp_serv INTEGER," +
                         " rashod " + DBManager.sDecimalType + "(13,2)," +
                         " initial_reading " + DBManager.sDecimalType + "(13,2)," +
                         " ending_reading " + DBManager.sDecimalType + "(13,2))" + DBManager.sUnlogTempTable;
            ExecSQL(sql);

            ExecSQL("CREATE INDEX ix_t_big_ipu_01 ON t_big_ipu(nzp_kvar)");
            ExecSQL("CREATE INDEX ix_t_big_ipu_02 ON t_big_ipu(nzp_kvar, nzp_serv)");

            //sql = " CREATE TEMP TABLE t_big_ipu_pokaz( " +
            //      " nzp_kvar INTEGER, " +
            //      " nzp_serv INTEGER, " +
            //      " val_cnt " + DBManager.sDecimalType + "(13,2)) " + DBManager.sUnlogTempTable;
            //ExecSQL(sql);
        }

        protected override void DropTempTable()
        {
             ExecSQL(" DROP TABLE t_big_ipu ");
             //ExecSQL(" DROP TABLE t_big_ipu_pokaz ");
        }

    }
}
