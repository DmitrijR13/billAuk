using System;
using System.Data;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using Bars.KP50.Report.Base.Parameters;
using Bars.KP50.Report.Tula.Properties;
using Bars.KP50.Report.Base;
using Bars.KP50.Utils;
using Newtonsoft.Json;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;


namespace Bars.KP50.Report.Tula.Reports
{
    class Report710307 : BaseSqlReport
    {
        public override string Name
        {
            get { return "71.3.7 Отчетная форма для Сбербанка"; }
        }

        public override string Description
        {
            get { return "Отчетная форма для Сбербанка"; }
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

        public override IList<ReportKind> ReportKinds
        {
            get { return new List<ReportKind> { ReportKind.Base, ReportKind.ListLC }; }
        }

        protected override byte[] Template
        {
            get { return Resources.Report_71_3_7_1; }
        }

        # region Параметры
        /// <summary>Расчетный месяц</summary>
        protected int MonthS { get; set; }

        /// <summary>Расчетный год</summary>
        protected int YearS { get; set; }


        /// <summary>Расчетный месяц</summary>
        protected int MonthPo { get; set; }

        /// <summary>Расчетный год</summary>
        protected int YearPo { get; set; }

        /// <summary>Районы</summary>
        protected List<int> Rajons { get; set; }

        /// <summary>Банки данных</summary>
        protected List<int> Banks { get; set; } 
        
        /// <summary>Заголовок территории</summary>
        protected string TerritoryHeader { get; set; }
        
        /// <summary>Список префиксов банков в БД</summary>  
        private List<string> PrefBanks { get; set; }

        private int CountBank { get; set; }

        /// <summary>Список банков </summary>  
        private List<string> NameBanks { get; set; }

        /// <summary>Список сумм собранных в банке </summary>  
        private List<double> SumBanks { get; set; }
        #endregion


        public override List<UserParam> GetUserParams()
        {
            var curCalcMonthYear = DBManager.GetCurMonthYear();
            return new List<UserParam>
            {
                new MonthParameter {Name = "Месяц с", Value = curCalcMonthYear != null ? curCalcMonthYear.Rows[0]["month_"] : DateTime.Today.Month },
                new YearParameter {Name = "Год с", Value = curCalcMonthYear != null ? curCalcMonthYear.Rows[0]["yearr"] : DateTime.Today.Year },
                new MonthParameter {Name = "Месяц по", Code = "Month1", Value = curCalcMonthYear != null ? curCalcMonthYear.Rows[0]["month_"] : DateTime.Today.Month },
                new YearParameter {Name = "Год по", Code = "Year1", Value = curCalcMonthYear != null ? curCalcMonthYear.Rows[0]["yearr"] : DateTime.Today.Year },
                new BankParameter(),
                new RaionsParameter(),
            };
        }

        protected override void PrepareParams()
        {
            MonthS = UserParamValues["Month"].GetValue<int>();
            YearS = UserParamValues["Year"].Value.To<int>();
            MonthPo = UserParamValues["Month1"].GetValue<int>();
            YearPo = UserParamValues["Year1"].Value.To<int>();
            Rajons = UserParamValues["Raions"].Value.To<List<int>>();
            Banks = UserParamValues["Banks"].Value.To<List<int>>();
        }

        protected override void PrepareReport(FastReport.Report report)
        {
            var months = new[] {"","Январь","Февраль",
                 "Март","Апрель","Май","Июнь","Июль","Август","Сентябрь",
                 "Октябрь","Ноябрь","Декабрь"};
            if ((MonthS == MonthPo) & (YearS == YearPo))
            {
                report.SetParameterValue("pPeriod", months[MonthS] + " " + YearS);
            }
            else
            {
                report.SetParameterValue("pPeriod", "период с " + months[MonthS] + " " + YearS +
                                                         "г. по " + months[MonthPo] + " " + YearPo); 
            }    
            int i = 1;
            foreach (var sum in SumBanks)
            {
                report.SetParameterValue("isum" + i, sum);
                ++i;
            }

            i = 1;
            foreach (var payer in NameBanks )
            {
                report.SetParameterValue("nbank" + i, payer);
                ++i;
            }

            report.SetParameterValue("CountBank", CountBank );
            report.SetParameterValue("date",DateTime.Now.ToShortDateString());
            report.SetParameterValue("time",DateTime.Now.ToShortTimeString());
        }

        public override DataSet GetData()
        {   
            string whereRajon = GetRajon("u.");
            string sql = "";
            string whereWp = GetwhereWp(" d.");
            string kvar;
            if (ReportParams.CurrentReportKind == ReportKind.ListLC)
                {
                    GetSelectedKvars();
                    kvar = " selected_kvars k,";
                }
                else
                    kvar = ReportParams.Pref + DBManager.sDataAliasRest + "kvar k,";
            string centralData = ReportParams.Pref + DBManager.sDataAliasRest;
            string raj = !string.IsNullOrEmpty(whereRajon) ? " u.nzp_raj, " : " -1, ";
            foreach (var pref in PrefBanks)
            {
                string localData = pref + DBManager.sDataAliasRest;
                for (var i = YearS * 12 + MonthS; i <= YearPo * 12 + MonthPo; i++)
                {
                    var year = i / 12;
                    var month = i % 12;
                    if (month == 0)
                    {
                        year--;
                        month = 12;
                    }
                    string charge = pref + "_charge_" + (year - 2000).ToString("00") + DBManager.tableDelimiter +
                                "charge_" + month.ToString("00");
                    string perekidka = pref + "_charge_" + (year - 2000).ToString("00") + DBManager.tableDelimiter +
                              "perekidka";
                    if (TempTableInWebCashe(charge) && TempTableInWebCashe(perekidka))
                    {
                        sql = " INSERT INTO t_nach_71_3_7 (nzp_wp, nzp_raj, sum_nach)" +
                              " SELECT d.nzp_wp, "+raj+" sum(sum_real+real_charge+reval) " +
                              " FROM " + charge + " ch, " + kvar +
                              centralData+"dom d,"+localData+"s_ulica u"+
                              " WHERE ch.num_ls=k.num_ls AND k.nzp_dom=d.nzp_dom AND d.nzp_ul=u.nzp_ul " +
                              " AND  dat_charge is null AND nzp_serv>1" + whereRajon + 
                              " GROUP BY 1,2 ";
                        ExecSQL(sql);

                        sql = " INSERT INTO t_nach_71_3_7 (nzp_wp, nzp_raj, sum_nach)" +
                              " SELECT d.nzp_wp, "+raj+" -sum(sum_rcl) " +
                              " FROM " + perekidka + " ch, " + kvar +
                              localData + "dom d," + localData + "s_ulica u " +
                              " WHERE ch.num_ls=k.num_ls AND k.nzp_dom=d.nzp_dom" +
                              " AND d.nzp_ul=u.nzp_ul AND month_=" + month +
                              " AND type_rcl in (100,20) AND nzp_serv>1" + whereRajon + 
                              " GROUP BY 1,2 ";
                        ExecSQL(sql);
                    } 
                }
            }


            for (var i = YearS; i <= YearPo+1; i++)
            {
                var year = i;

                string pack = ReportParams.Pref + "_fin_" + (year - 2000).ToString("00") + DBManager.tableDelimiter +
                              "pack";
                string packls = ReportParams.Pref + "_fin_" + (year - 2000).ToString("00") + DBManager.tableDelimiter +
                                "pack_ls ";

                DateTime DatS = new DateTime(YearS, MonthS, 1);

                DateTime DatPo = new DateTime(YearPo, MonthPo, DateTime.DaysInMonth(YearPo, MonthPo));

                string DateStr=   " AND pp.dat_uchet >= '" + DatS.ToShortDateString() + "' " +
                                  " AND pp.dat_uchet <= '" + DatPo.ToShortDateString() + "'" ;
                if (Points.DateOper >= DatS && Points.DateOper <= DatPo)
                    DateStr = " AND ((pp.dat_uchet >= '" + DatS.ToShortDateString() + "' " +
                              " AND pp.dat_uchet <= '" + DatPo.ToShortDateString() + "') OR pp.dat_uchet is null )";

                if (TempTableInWebCashe(pack) && TempTableInWebCashe(packls))
                {
                    if (ReportParams.CurrentReportKind == ReportKind.ListLC)
                        sql = " INSERT INTO t_report_71_3_7 ( nzp_wp, nzp_raj, nzp_bank, sum_oplat)" +
                              " SELECT d.nzp_wp,"+raj+" nzp_bank, sum(g_sum_ls) " +
                              " FROM " + pack + " pp," +
                              packls + " pl , " +
                              kvar + centralData + "dom d," + centralData + "s_ulica u" +
                              " WHERE  pack_type=10 AND pl.num_ls=k.num_ls AND " +
                              " AND k.nzp_dom=d.nzp_dom AND d.nzp_ul=u.nzp_ul" +
                              " pp.nzp_pack=pl.nzp_pack " + whereRajon + whereWp +
                              DateStr +
                              " GROUP BY 1,2,3";
                    else
                        sql = " INSERT INTO t_report_71_3_7 ( nzp_wp, nzp_raj, nzp_bank, sum_oplat)" +
                              " SELECT d.nzp_wp,"+raj+" nzp_bank, sum(g_sum_ls) " +
                              " FROM " + pack + " pp," +
                              packls + " pl left outer join " +
                              kvar.Trim(' ', ',' ) + " on pl.num_ls=k.num_ls LEFT OUTER JOIN " +
                              centralData + "dom d ON k.nzp_dom=d.nzp_dom  LEFT OUTER JOIN " + 
                              centralData + "s_ulica u ON d.nzp_ul=u.nzp_ul " +
                              " WHERE  pack_type=10 and " +
                              " pp.nzp_pack=pl.nzp_pack " + whereRajon + whereWp +
                              DateStr +
                              " GROUP BY 1,2,3";
                    ExecSQL(sql);
                }
            }   

            sql = " UPDATE t_report_71_3_7  SET nzp_wp = -1 WHERE nzp_wp IS NULL ";
            ExecSQL(sql);
            sql = " UPDATE t_report_71_3_7  SET nzp_raj = -1 WHERE nzp_raj IS NULL ";
            ExecSQL(sql);

            sql = " UPDATE t_report_71_3_7  SET payer = (" +
                  " select trim(payer) from " + ReportParams.Pref + DBManager.sKernelAliasRest + "s_bank sb," +
                  ReportParams.Pref + DBManager.sKernelAliasRest + "s_payer sp" +
                  " where sb.nzp_bank=t_report_71_3_7.nzp_bank AND sp.nzp_payer=sb.nzp_payer) ";
            ExecSQL(sql);

            sql = " UPDATE t_report_71_3_7 SET payer = 'Кассы' WHERE payer is null and nzp_bank is not null";
            ExecSQL(sql);

            sql = " UPDATE t_report_71_3_7 SET payer = 'Не определен' WHERE payer is null ";
            ExecSQL(sql);

            DataTable tempTableSums = ExecSQLToTable("select sum(sum_oplat) as isum from t_report_71_3_7 WHERE nzp_bank>0 group by payer order by payer ");
            SumBanks = new List<double>();
            foreach (DataRow row in tempTableSums.Rows)
            {
                SumBanks.Add(Convert.ToDouble(row["isum"]));
            }

            DataTable tempTableBanks = ExecSQLToTable("select distinct payer from t_report_71_3_7 WHERE nzp_bank>0 order by payer ");
            CountBank = tempTableBanks.Rows.Count;

            var res = new StringBuilder();
            res.Append(" CREATE TEMP TABLE  t_rep (  nzp_wp integer, nzp_raj integer, point char(100),");
            res.Append(" sum_nach " + DBManager.sDecimalType + "(14,2), ");
            for (int i = 1; i <= CountBank; ++i)
            {
                res.Append(" sum_bank"+i+" "+ DBManager.sDecimalType + "(14,2), ");  
            }
            res.Append(" sum_itog " + DBManager.sDecimalType + "(14,2)) " + DBManager.sUnlogTempTable);  
            ExecSQL(res.ToString());
            ExecSQL(" CREATE INDEX ix_t_report_3_76 ON t_rep(nzp_wp) ");
            ExecSQL(" CREATE INDEX ix_t_report_3_77 ON t_rep(nzp_raj) ");

            string sql1 =" INSERT INTO t_rep (nzp_wp, nzp_raj, sum_nach)" +
                         " SELECT nzp_wp, nzp_raj, sum(sum_nach) " +
                         " FROM t_nach_71_3_7 " +
                         " WHERE sum_nach<>0 or nzp_wp in( SELECT distinct nzp_wp FROM t_report_71_3_7 WHERE sum_oplat <> 0 )" +
                         " GROUP BY 1,2 ";
            ExecSQL(sql1);

            if ( Convert.ToInt32(ExecScalar(" select count(nzp_wp) from t_report_71_3_7 where nzp_wp=-1") )>0  )
            {
                sql1 = " INSERT INTO t_rep (nzp_wp, sum_nach)" +
                   " VALUES ( -1, null )" ;
            
                ExecSQL(sql1);              
            }
            if (!string.IsNullOrEmpty(whereRajon) && Convert.ToInt32(ExecScalar(" select count(nzp_raj) from t_report_71_3_7 where nzp_raj=-1")) > 0 )
            {
                sql1 = " INSERT INTO t_rep (nzp_raj, sum_nach)" +
                   " VALUES ( -1, null )";

                ExecSQL(sql1);
            }


            NameBanks = new List<string>();
            int j = 1;
            foreach (DataRow row in tempTableBanks.Rows)
            {
                string name_payer = row["payer"].ToString();
                string sqlb = " UPDATE t_rep SET sum_bank"+j+"=( " +
                              " SELECT sum(sum_oplat) " +
                              " FROM t_report_71_3_7 t " +
                              " WHERE t_rep.nzp_wp = t.nzp_wp " +
                              " AND t_rep.nzp_raj=t.nzp_raj" +
                              " AND payer='" + name_payer + "')";
                ExecSQL(sqlb);
                ++j;

                NameBanks.Add(name_payer);
            }

            string sqli = " UPDATE t_rep SET sum_itog=( " +
              " SELECT sum(sum_oplat) " +
              " FROM t_report_71_3_7 t " +
              " WHERE t_rep.nzp_wp = t.nzp_wp AND t_rep.nzp_raj=t.nzp_raj)";
            ExecSQL(sqli);


            if (!string.IsNullOrEmpty(whereRajon))
            {
                sql = " UPDATE t_rep SET point = (" +
                      " SELECT s.point||'/'||CASE WHEN TRIM(rajon)='-' THEN town ELSE TRIM(town)||'/'||TRIM(rajon) END " +
                      " FROM " + centralData + "s_rajon r," + centralData + "s_town t," +
                      ReportParams.Pref + DBManager.sKernelAliasRest + "s_point s" +
                      " WHERE r.nzp_raj=t_rep.nzp_raj AND t.nzp_town=r.nzp_town AND s.nzp_wp=t_rep.nzp_wp ) ";
                ExecSQL(sql);
            }
            else
            {
                sql = " UPDATE t_rep SET point = (" +
                      " SELECT point " +
                      " FROM " + ReportParams.Pref + DBManager.sKernelAliasRest + "s_point s" +
                      " WHERE s.nzp_wp=t_rep.nzp_wp) ";
                ExecSQL(sql);  
            }

            sql = " update t_rep   set point = 'Неопределен'" +
                  " where point is null";
            ExecSQL(sql);

            DataTable tempTableRes = ExecSQLToTable("select * from t_rep order by point ");
            tempTableRes.TableName = "Q_master";

            var ds = new DataSet();
            
            ds.Tables.Add(tempTableRes);
            return  ds;
        }

        protected override void CreateTempTable()
        {
            string sql = " CREATE TEMP TABLE t_report_71_3_7 ( " +           
                               " nzp_wp integer, " +
                               " nzp_raj integer, " +
                               " nzp_bank integer, " +
                               " payer CHARACTER(100), " +
                               " sum_oplat " + DBManager.sDecimalType + "(14,2) )" +
                               DBManager.sUnlogTempTable;
            ExecSQL(sql);
            ExecSQL(" CREATE INDEX ix_t_report_3_70 ON t_report_71_3_7(nzp_wp) ");
            ExecSQL(" CREATE INDEX ix_t_report_3_71 ON t_report_71_3_7(nzp_raj) ");
            ExecSQL(" CREATE INDEX ix_t_report_3_72 ON t_report_71_3_7(nzp_bank) ");
            
            sql = " CREATE TEMP TABLE t_nach_71_3_7 ( " +
                               " nzp_raj integer, " +
                               " nzp_wp integer, " +
                               " sum_nach " + DBManager.sDecimalType + "(14,2)) " +
                               DBManager.sUnlogTempTable;
            ExecSQL(sql);
            ExecSQL(" CREATE INDEX ix_t_report_3_73 ON t_nach_71_3_7(nzp_wp) ");
            ExecSQL(" CREATE INDEX ix_t_report_3_74 ON t_nach_71_3_7(nzp_raj) ");
            
            if (ReportParams.CurrentReportKind == ReportKind.ListLC)
            {
                ExecSQL(" create temp table selected_kvars(nzp_kvar integer, nzp_dom integer, nzp_wp integer, num_ls integer) " + DBManager.sUnlogTempTable);
            }   
        }

        protected override void DropTempTable()
        {
            ExecSQL(" DROP TABLE t_report_71_3_7 ");
            ExecSQL(" DROP TABLE t_nach_71_3_7 ");
            ExecSQL(" DROP TABLE t_rep ");
            if (ReportParams.CurrentReportKind == ReportKind.ListLC)
            {
                ExecSQL(" DROP TABLE  selected_kvars ");
            } 
        }

        /// <summary>
        /// Ограничение по районам
        /// </summary>
        /// <returns></returns>
        public string GetRajon(string filedPref)
        {
            string whereRajon = String.Empty;
            if (Rajons != null)
            {
                whereRajon = Rajons.Aggregate(whereRajon, (current, nzpRaj) => current + (nzpRaj + ","));
            }
            whereRajon = whereRajon.TrimEnd(',');
            whereRajon = !String.IsNullOrEmpty(whereRajon)
                ? " AND " + filedPref + ".nzp_raj in (" + whereRajon + ")"
                  : String.Empty;
            //if (!String.IsNullOrEmpty(whereRajon))
            //{
            //    RajonsHeader = String.Empty;
            //    string sql = " SELECT rajon from " + ReportParams.Pref + DBManager.sDataAliasRest + "s_rajon " +
            //                 filedPref.Trim('.') + "  WHERE " + filedPref + ".nzp_raj > 0 " + whereRajon;
            //    DataTable raj = ExecSQLToTable(sql);
            //    foreach (DataRow dr in raj.Rows)
            //    {
            //        RajonsHeader += dr["rajon"].ToString().Trim() + ", ";
            //    }
            //    RajonsHeader = RajonsHeader.TrimEnd(',', ' ');
            //}
            return whereRajon;
        }

        private string GetwhereWp(string filedPref)
        {
            string whereWp = String.Empty;
            if (Banks != null)
            {
                whereWp = Banks.Aggregate(whereWp, (current, nzpWp) => current + (nzpWp + ","));
            }
            else
            {
                whereWp = ReportParams.GetRolesCondition(Constants.role_sql_wp);
            }
            whereWp = whereWp.TrimEnd(',');
            whereWp = !String.IsNullOrEmpty(whereWp) ? " AND "+filedPref+"nzp_wp in (" + whereWp + ")" : String.Empty;
            PrefBanks = new List<string>();
            if (!string.IsNullOrEmpty(whereWp))
            {
                TerritoryHeader = String.Empty;
                string sql = " SELECT point,bd_kernel FROM " + ReportParams.Pref + DBManager.sKernelAliasRest + "s_point " + filedPref.Trim('.') + " WHERE " + filedPref + "nzp_wp > 0 " + whereWp;
                DataTable terrTable = ExecSQLToTable(sql);
                
                foreach (DataRow row in terrTable.Rows)
                {
                    TerritoryHeader += row["point"].ToString().Trim() + ", ";
                    PrefBanks.Add(row["bd_kernel"].ToString().Trim());
                }
                TerritoryHeader = TerritoryHeader.TrimEnd(',', ' ');
            }
            else
            {
                string sql = " SELECT bd_kernel FROM " + ReportParams.Pref + DBManager.sKernelAliasRest + "s_point " + filedPref.Trim('.') + " WHERE " + filedPref + "nzp_wp > 0 and flag=2";
                DataTable terrTable = ExecSQLToTable(sql);
                foreach (DataRow row in terrTable.Rows)
                {
                    PrefBanks.Add(row["bd_kernel"].ToString().Trim());
                }
            }

            return whereWp;
        }

        /// <summary>
        /// Выборка списка квартир в картотеке
        ///  </summary>
        /// <returns></returns>
        private bool GetSelectedKvars()
        {
            if (ReportParams.CurrentReportKind == ReportKind.ListLC)
            {
                using (IDbConnection connWeb = DBManager.GetConnection(Constants.cons_Webdata))
                {
                    if (!DBManager.OpenDb(connWeb, true).result) return false;

                    string tSpls = DBManager.GetFullBaseName(connWeb) + DBManager.tableDelimiter +
                                   "t" + ReportParams.User.nzp_user + "_spls";
                    if (TempTableInWebCashe(tSpls))
                    {
                        string sql = " insert into selected_kvars (nzp_kvar, num_ls, nzp_dom) " +
                                     " select nzp_kvar, num_ls, nzp_dom from " + tSpls;
                        ExecSQL(sql);
                        ExecSQL("create index ix_sel_kvar_09 on selected_kvars(nzp_kvar)");
                        ExecSQL(DBManager.sUpdStat + " selected_kvars");
                        return true;
                    }
                }
            }
            return false;
        }

    }
}
