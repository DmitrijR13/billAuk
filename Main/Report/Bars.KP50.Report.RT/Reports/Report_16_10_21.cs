using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using Bars.KP50.Report.Base;
using Bars.KP50.Report.RT.Properties;
using Bars.KP50.Utils;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;

namespace Bars.KP50.Report.RT.Reports
{
    class Report161021 : BaseSqlReport
    {
        public override string Name
        {
            get { return "16.10.21 Ведомость начислений и оплат по домам"; }
        }

        public override string Description
        {
            get { return "10.21 Ведомость начислений и оплат по домам"; }
        }

        public override IList<ReportGroup> ReportGroups
        {
            get
            {
                List<ReportGroup> result = new List<ReportGroup> {ReportGroup.Reports};
                return result;
            }
        }

        public override bool IsPreview
        {
            get { return false; }
        }

        protected override byte[] Template
        {
            get { return Resources.Report_16_10_21; }
        }

        public override IList<ReportKind> ReportKinds
        {
            get { return new List<ReportKind> { ReportKind.Base }; }
        }


        /// <summary>Расчетный месяц</summary>
        protected int MonthS { get; set; }

        /// <summary>Расчетный год</summary>
        protected int YearS { get; set; }


        /// <summary>Расчетный месяц</summary>
        protected int MonthPo { get; set; }

        /// <summary>Расчетный год</summary>
        protected int YearPo { get; set; }

        /// <summary>Управляющие компании</summary>
        protected List<int> Areas { get; set; }

        /// <summary>Услуги</summary>
        protected List<int> Service { get; set; }

        /// <summary>Поставщики</summary>
        protected List<long> Suppliers { get; set; }

        /// <summary>Заголовок отчета</summary>
        protected string ServicesHeader { get; set; }

        /// <summary>Заголовок отчета</summary>
        protected string SupplierHeader { get; set; }

        /// <summary>Территория</summary>
        protected string GeuHeader { get; set; }


        public override List<UserParam> GetUserParams()
        {
            var curCalcMonthYear = DBManager.GetCurMonthYear();
            return new List<UserParam>
            {
                new MonthParameter {Name = "Месяц с", Value = curCalcMonthYear != null ? curCalcMonthYear.Rows[0]["month_"] : DateTime.Today.Month,DefaultValue=DateTime.Today.Month },
                new YearParameter {Name = "Год с", Value = curCalcMonthYear != null ? curCalcMonthYear.Rows[0]["yearr"] : DateTime.Today.Year,DefaultValue=DateTime.Today.Year },
                new MonthParameter {Name = "Месяц по", Code = "Month1", Value = curCalcMonthYear != null ? curCalcMonthYear.Rows[0]["month_"] : DateTime.Today.Month,DefaultValue=DateTime.Today.Month },
                new YearParameter {Name = "Год по", Code = "Year1", Value = curCalcMonthYear != null ? curCalcMonthYear.Rows[0]["yearr"] : DateTime.Today.Year,DefaultValue=DateTime.Today.Year },
                new ServiceParameter(),
                new SupplierParameter(),
                new AreaParameter()
            };
        }

        public override DataSet GetData()
        {
            IDataReader reader;

            string whereArea = GetWhereArea("fn.");
            string whereService = GetWhereServ("fn.");
            string whereSupplier = GetWhereSupp("fn.");
            string whereGeu = GetWhereGeu("fn.");


            #region выборка в temp таблицу

            string sql = " SELECT bd_kernel as pref " +
                         " FROM " + ReportParams.Pref + DBManager.sKernelAliasRest + "s_point " +
                         " WHERE nzp_wp>1 " + GetWhereWp();

            ExecRead(out reader, sql);

            while (reader.Read())
            {
                if (reader["pref"] == null) continue;
                for (int year = YearS; year <= YearPo; year++)
                {
                    string pref = reader["pref"].ToStr().Trim();
                    string prefData = pref + DBManager.sDataAliasRest;
                    string tableFin = ReportParams.Pref + "_fin_" + (year - 2000).ToString("00") +
                                      DBManager.tableDelimiter + "fn_ukrgudom";
                    if (TempTableInWebCashe(tableFin))
                    {
                        sql = " INSERT INTO t_nach_opl(area,ulica,ndom,nkor,idom,nzp_dom,uch,sum_insaldo,sum_real,izm,sum_money,sum_outsaldo) " +
                              " SELECT area, u.ulica, d.ndom, d.nkor, idom, fn.nzp_dom, kvt.uch, " +
                              " SUM(CASE WHEN  MDY(month_,01,year_)=DATE('1." + MonthS + "." + YearS + "') THEN sum_insaldo ELSE 0 END) AS sum_insaldo, " +
                              " SUM(sum_real) AS sum_real, " +
                              " SUM(reval+real_charge) AS izm, " +
                              " SUM(sum_money) AS sum_money, " +
                              " SUM(CASE WHEN  MDY(month_,01,year_)=DATE('1." + MonthPo + "." + YearPo + "') THEN sum_outsaldo ELSE 0 END) AS sum_outsaldo " +
                              " FROM " + tableFin + " fn, " +
                              prefData + "s_ulica u, " +
                              prefData + "dom d, " +
                              prefData + "s_area a, " +
                              " (SELECT nzp_dom, MAX(" + DBManager.sNvlWord + "(uch,0)) AS uch " +
                              " FROM " + prefData + "kvar  " +
                              " GROUP BY 1) kvt " +
                              " WHERE fn.nzp_dom=d.nzp_dom " +
                              " AND u.nzp_ul=d.nzp_ul " +
                              " AND fn.nzp_area=a.nzp_area " +
                              " AND fn.nzp_dom=kvt.nzp_dom " +
                              " AND MDY(month_,01,year_)>=DATE('1." + MonthS + "." + YearS + "') " +
                              " AND MDY(month_,01,year_)<=DATE('1." + MonthPo + "." + YearPo + "') " +
                              whereArea + whereGeu + whereService + whereSupplier +
                              " GROUP BY 1,2,3,4,5,6,7 ";

                        ExecSQL(sql);
                    }
                }
            }

            reader.Close();
            #endregion

            sql = " SELECT area,ulica,ndom,nkor,idom,nzp_dom,uch,SUM(sum_insaldo) AS sum_insaldo,SUM(sum_real) AS sum_real,SUM(izm) AS izm,SUM(sum_money) AS sum_money,SUM(sum_outsaldo) AS sum_outsaldo," +
                            " SUM(CASE WHEN sum_insaldo+sum_real+izm=0 THEN 0 ELSE sum_money/(sum_insaldo+sum_real+izm) END) AS per_sent " +
                  " FROM t_nach_opl " +
                  " GROUP BY 1,2,3,4,5,6,7 " +
                  " ORDER BY 1,2,5,3,4 ";
            DataTable dt = ExecSQLToTable(sql);
            dt.TableName = "Q_master";
            var ds = new DataSet();
            ds.Tables.Add(dt);
            return ds;
        }

        /// <summary>
        /// Получить условия органичения по локальному банку
        /// </summary>
        private string GetWhereWp()
        {
            var result = ReportParams.GetRolesCondition(Constants.role_sql_wp);
            return !String.IsNullOrEmpty(result) ? " AND nzp_wp IN (" + result + ") " : "";
        }

        /// <summary>
        /// Получить условия органичения по поставщикам
        /// </summary>
        private string GetWhereGeu(string tablePrefix)
        {
            string whereGeu = ReportParams.GetRolesCondition(Constants.role_sql_geu);

            whereGeu = whereGeu.TrimEnd(',');
            if (!String.IsNullOrEmpty(whereGeu))
            {
                whereGeu = " AND " + tablePrefix + "nzp_geu in (" + whereGeu + ")";
                GeuHeader = String.Empty;

                string sql = " SELECT  geu FROM " +
                             ReportParams.Pref + DBManager.sDataAliasRest + "s_geu " + tablePrefix.TrimEnd('.') +
                             " WHERE " + tablePrefix + "nzp_geu > 0 " + whereGeu;
                DataTable geu = ExecSQLToTable(sql);
                foreach (DataRow dr in geu.Rows)
                {
                    GeuHeader += dr["geu"].ToString().Trim() + ",";
                }
                GeuHeader = GeuHeader.TrimEnd(',');
            }
            return whereGeu;
        }

        /// <summary>
        /// Получить условия органичения по поставщикам
        /// </summary>
        private string GetWhereSupp(string tablePrefix)
        {
            string whereSupp = String.Empty;

            if (Suppliers != null)
            {
                whereSupp = Suppliers.Aggregate(whereSupp, (current, nzpSupp) => current + (nzpSupp + ","));
            }
            else
            {
                whereSupp = ReportParams.GetRolesCondition(Constants.role_sql_supp);
            }

            whereSupp = whereSupp.TrimEnd(',');
            if (!String.IsNullOrEmpty(whereSupp))
            {
                whereSupp = " AND " + tablePrefix + "nzp_supp in (" + whereSupp + ")";
                SupplierHeader = String.Empty;

                string sql = " SELECT name_supp from " +
                             ReportParams.Pref + DBManager.sKernelAliasRest + "supplier " + tablePrefix.TrimEnd('.') +
                             " WHERE " + tablePrefix + "nzp_supp > 0 " + whereSupp;
                DataTable supp = ExecSQLToTable(sql);
                foreach (DataRow dr in supp.Rows)
                {
                    SupplierHeader += dr["name_supp"].ToString().Trim() + ",";
                }
                SupplierHeader = SupplierHeader.TrimEnd(',');
            }
            return whereSupp;
        }

        /// <summary>
        /// Получить условия органичения по УК
        /// </summary>
        private string GetWhereArea(string tablePrefix)
        {
            string whereArea = String.Empty;
            if (Areas != null)
            {
                whereArea = Areas.Aggregate(whereArea, (current, nzpArea) => current + (nzpArea + ","));
            }
            else
            {
                whereArea = ReportParams.GetRolesCondition(Constants.role_sql_area);
            }

            whereArea = whereArea.TrimEnd(',');
            if (!String.IsNullOrEmpty(whereArea))
            {
                whereArea = " AND " + tablePrefix + "nzp_area in (" + whereArea + ")";
            }
            return whereArea;
        }

        /// <summary>
        /// Получить условия органичения по услугам
        /// </summary>
        private string GetWhereServ(string tablePrefix)
        {
            string whereServ = String.Empty;
            if (Service != null)
            {
                whereServ = Service.Aggregate(whereServ, (current, nzpServ) => current + (nzpServ + ","));
            }
            else
            {
                whereServ = ReportParams.GetRolesCondition(Constants.role_sql_serv);
            }
            whereServ = whereServ.TrimEnd(',');
            if (!String.IsNullOrEmpty(whereServ))
            {
                whereServ = " AND " + tablePrefix + "nzp_serv in (" + whereServ + ")";
                ServicesHeader = String.Empty;

                string sql = " SELECT service from " +
                             ReportParams.Pref + DBManager.sKernelAliasRest + "services " + tablePrefix.TrimEnd('.') + ", " +
                             ReportParams.Pref + DBManager.sKernelAliasRest + "s_counts cou  " +
                             " WHERE " + tablePrefix + "nzp_serv > 0 " +
                               " AND cou.nzp_serv=" + tablePrefix + "nzp_serv "+ whereServ;
                DataTable ser = ExecSQLToTable(sql);
                foreach (DataRow dr in ser.Rows)
                {
                    ServicesHeader += dr["service"].ToString().Trim() + ",";
                }
                ServicesHeader = ServicesHeader.TrimEnd(',');
            }
            return whereServ;
        }

        protected override void CreateTempTable()
        {
            var sql = new StringBuilder();
            sql.Append(" CREATE TEMP TABLE t_nach_opl( ");
            sql.Append(" area CHARACTER(40), ");
            sql.Append(" ulica CHARACTER(40), ");
            sql.Append(" ndom CHARACTER(10), ");
            sql.Append(" nkor CHARACTER(3), ");
            sql.Append(" idom INTEGER, ");
            sql.Append(" nzp_dom INTEGER, ");
            sql.Append(" uch INTEGER, ");
            sql.Append(" sum_insaldo " + DBManager.sDecimalType + "(14,2),");
            sql.Append(" sum_real " + DBManager.sDecimalType + "(14,2),");
            sql.Append(" izm " + DBManager.sDecimalType + "(14,2),");
            sql.Append(" sum_money " + DBManager.sDecimalType + "(14,2),");
            sql.Append(" sum_outsaldo " + DBManager.sDecimalType + "(14,2))" + DBManager.sUnlogTempTable);
            ExecSQL(sql.ToString());
        }

        protected override void DropTempTable()
        {
            ExecSQL("DROP TABLE t_nach_opl");
        }

        protected override void PrepareReport(FastReport.Report report)
        {
            report.SetParameterValue("service", ServicesHeader);
            report.SetParameterValue("supplier", SupplierHeader);
            report.SetParameterValue("geu", GeuHeader);
            report.SetParameterValue("dates", DateTime.Parse("1." + MonthS + "." + YearS).ToShortDateString());
            report.SetParameterValue("datepo", DateTime.Parse("1." + MonthPo + "." + YearPo).ToShortDateString());
            report.SetParameterValue("DATE", DateTime.Now.ToShortDateString());
            report.SetParameterValue("TIME", DateTime.Now.ToLongTimeString());
        }

        protected override void PrepareParams()
        {
            MonthS = UserParamValues["Month"].GetValue<int>();
            YearS = UserParamValues["Year"].Value.To<int>();
            MonthPo = UserParamValues["Month1"].GetValue<int>();
            YearPo = UserParamValues["Year1"].Value.To<int>();

            Service = UserParamValues["Services"].Value.To<List<int>>();
            Suppliers = UserParamValues["Suppliers"].Value.To<List<long>>();
            Areas = UserParamValues["Areas"].Value.To<List<int>>();

        }
    }
}
