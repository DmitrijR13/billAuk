using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Bars.KP50.Report.Base;
using Bars.KP50.Report.Base.Parameters;
using Bars.KP50.Report.MariEl.Properties;
using Bars.KP50.Utils;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;
using Newtonsoft.Json;


namespace Bars.KP50.Report.MariEl.Reports
{
    public class Report120305 : BaseSqlReport
    {

        public override string Name
        {
            get { return "12.3.5 Сведения о принятых и перечисленных денежных средствах"; }
        }

        public override string Description
        {
            get { return "Сведения о принятых и перечисленных денежных средствах"; }
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
            get { return Resources.Report_12_3_5; }
        }

        public override IList<ReportKind> ReportKinds
        {
            get { return new List<ReportKind> { ReportKind.Base }; }
        }

        /// <summary>Расчетный месяц</summary>
        protected int Month { get; set; }

        /// <summary>Расчетный год</summary>
        protected int Year { get; set; }

        /// <summary> Банки данных </summary>
        protected List<int> Banks { get; set; }

        /// <summary>Управляющие компании</summary>
        protected List<int> Areas { get; set; }

        /// <summary> Принципалы </summary>
        protected List<int> Principals { get; set; }

        /// <summary>Заголовок территории</summary>
        protected string TerritoryHeader { get; set; }

        protected string AreaHeader { get; set; }

        protected string PrincipalHeader { get; set; }

        public override List<UserParam> GetUserParams()
        {
            var curCalcMonthYear = DBManager.GetCurMonthYear();

            return new List<UserParam>
            {
                new MonthParameter {Value = curCalcMonthYear != null ? curCalcMonthYear.Rows[0]["month_"] : DateTime.Today.Month },
                new YearParameter {Value = curCalcMonthYear != null ? curCalcMonthYear.Rows[0]["yearr"] : DateTime.Today.Year },
                new BankParameter(),
                new AreaParameter(),
                new PrincipalParameter()
            };
        }

        public override DataSet GetData()
        {
            var whereNzpWp = GetwhereWp();
            var whereNzpArea = GetWhereArea();
            var whereNzpPayer = GetWherePrincipal();

            string sql;

            var distribXx = ReportParams.Pref + "_fin_" + (Year - 2000).ToString("00") +
                        DBManager.tableDelimiter + "fn_distrib_dom_" + Month.ToString("00");

            if (TempTableInWebCashe(distribXx))
            {
                sql =
                    " INSERT INTO t_distrib (nzp_area, nzp_supp, name_supp, nzp_payer, sum_in, sum_rasp, sum_charge, sum_ud, sum_out)" +
                    " SELECT  d.nzp_area, f.nzp_supp, s.name_supp, s.nzp_payer_princip, SUM(sum_in) sum_in, SUM(sum_rasp) sum_rasp, " +
                    " SUM(sum_charge) sum_charge, SUM(sum_ud) sum_ud, SUM(sum_out) sum_out " +
                    " FROM " + distribXx + " f, " + ReportParams.Pref + "_data.dom d, " + ReportParams.Pref + "_kernel.supplier s " +
                    " WHERE f.nzp_dom = d.nzp_dom and f.nzp_supp = s.nzp_supp " + whereNzpWp + whereNzpArea + whereNzpPayer +
                    " GROUP BY 1,2,3,4; ";
                ExecSQL(sql);
            }

            sql = " SELECT a.area, p.payer, t.name_supp, sum_in, sum_rasp, sum_charge, sum_ud, sum_out " +
                  " FROM  t_distrib t, " + ReportParams.Pref + "_data.s_area a, " + ReportParams.Pref + "_kernel.s_payer p " +
                  " WHERE t.nzp_area = a.nzp_area and t.nzp_payer = p.nzp_payer " +
                  " ORDER BY 1,2,3;";

            DataTable dt = ExecSQLToTable(sql);
            dt.TableName = "Q_master";
            var ds = new DataSet();
            ds.Tables.Add(dt);

            return ds;
        }


        protected override void PrepareReport(FastReport.Report report)
        {
            var months = new[] {"","Январь","Февраль",
                 "Март","Апрель","Май","Июнь","Июль","Август","Сентябрь",
                 "Октябрь","Ноябрь","Декабрь"};

            report.SetParameterValue("month", months[Month] + " " + Year);

            report.SetParameterValue("Date", DateTime.Now.ToLongDateString());

            string headerParam = !string.IsNullOrEmpty(TerritoryHeader) 
                ? "Территория: " + TerritoryHeader + "\n" 
                : string.Empty;
            headerParam += !string.IsNullOrEmpty(AreaHeader) 
                ? "Управляющая организация: " + AreaHeader + "\n" 
                : string.Empty;
            headerParam += !string.IsNullOrEmpty(PrincipalHeader) 
                ? "Принципал: " + PrincipalHeader + "\n"
                : string.Empty;
            report.SetParameterValue("headerParam", headerParam);

        }


        protected override void PrepareParams()
        {
            Month = UserParamValues["Month"].GetValue<int>();
            Year = UserParamValues["Year"].GetValue<int>();
            Banks = UserParamValues["Banks"].GetValue<List<int>>();
            Areas = UserParamValues["Areas"].GetValue<List<int>>();
            Principals = UserParamValues["Principal"].GetValue<List<int>>();
        }

        protected override void CreateTempTable()
        {
            string sql = " DROP TABLE t_distrib ";

            ExecSQL(sql);

            sql = "CREATE TEMP TABLE t_distrib (" +
                  " nzp_area  INTEGER, " +
                  " nzp_supp  INTEGER, " +
                  " name_supp CHAR(100), " +
                  " nzp_payer INTEGER, " +
                  " sum_in     " + DBManager.sDecimalType + "(14,2) DEFAULT 0.00, " +
                  " sum_rasp   " + DBManager.sDecimalType + "(14,2) DEFAULT 0.00, " +
                  " sum_charge " + DBManager.sDecimalType + "(14,2) DEFAULT 0.00, " +
                  " sum_ud     " + DBManager.sDecimalType + "(14,2) DEFAULT 0.00, " +
                  " sum_out    " + DBManager.sDecimalType + "(14,2) DEFAULT 0.00 " +
                  ");";

            ExecSQL(sql);
        }
        
        protected override void DropTempTable()
        {
            ExecSQL( "DROP TABLE t_distrib;");
        }

        /// <summary>
        /// Ограничение по банкам данных
        /// </summary>
        /// <returns></returns>
        private string GetwhereWp()
        {
            string whereWp = String.Empty;
            if (Banks != null)
            {
                whereWp = String.Join(", ", Banks); 
            }
            else
            {
                whereWp = ReportParams.GetRolesCondition(Constants.role_sql_wp);
            }
            string whereWpsql = !String.IsNullOrEmpty(whereWp) ? " AND nzp_wp in (" + whereWp + ")" : String.Empty;
            if (!string.IsNullOrEmpty(whereWpsql))
            {
                TerritoryHeader = String.Empty;
                string sql = " SELECT point,bd_kernel FROM " + ReportParams.Pref + DBManager.sKernelAliasRest + "s_point WHERE nzp_wp > 0 " + whereWpsql;
                DataTable terrTable = ExecSQLToTable(sql);
                foreach (DataRow row in terrTable.Rows)
                {
                    TerritoryHeader += row["point"].ToString().Trim() + ", ";

                }
                TerritoryHeader = TerritoryHeader.TrimEnd(',', ' ');
            }

            return !String.IsNullOrEmpty(whereWp) ? " AND d.nzp_wp in ( " + whereWp + ") " : String.Empty;
        }

        /// <summary>
        /// Получить условия органичения по УК
        /// </summary>
        /// <returns></returns>
        private string GetWhereArea()
        {
            string whereArea = String.Empty;
            if (Areas != null)
            {
                whereArea = String.Join(", ", Areas);
            }
            else
            {
                whereArea = ReportParams.GetRolesCondition(Constants.role_sql_area);
            }
            var whereNzpArea = !String.IsNullOrEmpty(whereArea) ? " AND t.nzp_area in (" + whereArea + ")" : String.Empty;
            if (!String.IsNullOrEmpty(whereNzpArea))
            {
                AreaHeader = String.Empty;
                string sql = " SELECT area from " + ReportParams.Pref + DBManager.sDataAliasRest +
                             "s_area t  WHERE t.nzp_area > 0 " + whereNzpArea;
                DataTable area = ExecSQLToTable(sql);
                foreach (DataRow dr in area.Rows)
                {
                    AreaHeader += dr["area"].ToString().Trim() + ", ";
                }
                AreaHeader = AreaHeader.TrimEnd(',', ' ');
            }

            return !String.IsNullOrEmpty(whereArea) ? " AND d.nzp_area in ( " + whereArea + ") " : String.Empty;
        }

        /// <summary>
        /// Получить ограничение по принципалам
        /// </summary>
        /// <returns></returns>
        private string GetWherePrincipal()
        {
            string wherePrincip = String.Empty;
            if (Principals != null)
            {
                wherePrincip = String.Join(", ", Principals);
            }
            else
            {
                wherePrincip = ReportParams.GetRolesCondition(Constants.role_sql_payer);
            }
            var whereNzpPayer = !String.IsNullOrEmpty(wherePrincip) ? " AND p.nzp_payer in (" + wherePrincip + ")" : String.Empty;
            if (!String.IsNullOrEmpty(whereNzpPayer))
            {
                PrincipalHeader = String.Empty;
                string sql = " SELECT payer from " + ReportParams.Pref + DBManager.sKernelAliasRest +
                             "s_payer p  WHERE 1 = 1 " + whereNzpPayer;
                DataTable principal = ExecSQLToTable(sql);
                foreach (DataRow dr in principal.Rows)
                {
                    PrincipalHeader += dr["payer"].ToString().Trim() + ", ";
                }
                PrincipalHeader = PrincipalHeader.TrimEnd(',', ' ');
            }

            return !String.IsNullOrEmpty(wherePrincip) ?  "AND s.nzp_payer_princip in ( " + wherePrincip + ") " : String.Empty;
        }
    }
}
