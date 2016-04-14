using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Bars.KP50.Report.Base;
using Bars.KP50.Utils;
using Newtonsoft.Json;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;
using Bars.KP50.Report.Tula.Properties;

namespace Bars.KP50.Report.Tula.Reports
{
    public class Report71050031 : BaseSqlReport
    {
        public override string Name
        {
            get { return "71.5.3.1 Сальдовая ведомость по услугам по населенным пунктам"; }
        }

        public override string Description
        {
            get { return "Сальдовая ведомость"; }
        }

        public override IList<ReportGroup> ReportGroups
        {
            get
            {
                var result = new List<ReportGroup> {ReportGroup.Reports};
                return result;
            }
        }

        public override bool IsPreview
        {
            get { return false; }
        }

        protected override byte[] Template
        {
            get { return Resources.Report_71_5_3_1; }
        }

        public override IList<ReportKind> ReportKinds
        {
            get { return new List<ReportKind> { ReportKind.Base, ReportKind.ListLC }; }
        }


        /// <summary>Расчетный месяц</summary>
        protected int MonthS { get; set; }

        /// <summary>Расчетный год</summary>
        protected int YearS { get; set; }


        /// <summary>Расчетный месяц</summary>
        protected int MonthPo { get; set; }

        /// <summary>Расчетный год</summary>
        protected int YearPo { get; set; }

        /// <summary>Услуги</summary>
        protected List<int> Services { get; set; }

        /// <summary>Заголовок отчета</summary>
        protected string SupplierHeader { get; set; }

        /// <summary>Заголовок услуг</summary>
        protected string ServiceHeader { get; set; }

        /// <summary>Заголовок территории</summary>
        protected string TerritoryHeader { get; set; }

        /// <summary>Поставщики, Агенты, Принципалы  </summary>
        protected BankSupplierParameterValue BankSupplier { get; set; }

        public override List<UserParam> GetUserParams()
        {
            var curCalcMonthYear = DBManager.GetCurMonthYear();
            return new List<UserParam>
            {
                new MonthParameter {Name = "Месяц с", Value = curCalcMonthYear != null ? curCalcMonthYear.Rows[0]["month_"] : DateTime.Today.Month },
                new YearParameter {Name = "Год с", Value = curCalcMonthYear != null ? curCalcMonthYear.Rows[0]["yearr"] : DateTime.Today.Year },
                new MonthParameter {Name = "Месяц по", Code = "Month1", Value = curCalcMonthYear != null ? curCalcMonthYear.Rows[0]["month_"] : DateTime.Today.Month },
                new YearParameter {Name = "Год по", Code = "Year1", Value = curCalcMonthYear != null ? curCalcMonthYear.Rows[0]["yearr"] : DateTime.Today.Year },
                new BankSupplierParameter(),
                new ServiceParameter(),
            };
        }

        protected override void PrepareParams() {
            MonthS = UserParamValues["Month"].GetValue<int>();
            YearS = UserParamValues["Year"].Value.To<int>();
            MonthPo = UserParamValues["Month1"].GetValue<int>();
            YearPo = UserParamValues["Year1"].Value.To<int>();
            Services = UserParamValues["Services"].Value.To<List<int>>();

            BankSupplier = JsonConvert.DeserializeObject<BankSupplierParameterValue>(UserParamValues["BankSupplier"].Value.ToString());

        }

        protected override void PrepareReport(FastReport.Report report) {
            var months = new[] {"","Январь","Февраль",
                 "Март","Апрель","Май","Июнь","Июль","Август","Сентябрь",
                 "Октябрь","Ноябрь","Декабрь"};
            report.SetParameterValue("month", months[MonthS]);
            report.SetParameterValue("year", YearS);
            if ((MonthS == MonthPo) & (YearS == YearPo))
            {
                report.SetParameterValue("period_month", months[MonthS] + " " + YearS);
            }
            else
            {
                report.SetParameterValue("period_month", "период с " + months[MonthS] + " " + YearS +
                                                         "г. по " + months[MonthPo] + " " + YearPo);

            }

            string headerParam = !string.IsNullOrEmpty(TerritoryHeader) ? "Территория: " + TerritoryHeader + "\n" : string.Empty;
            headerParam += !string.IsNullOrEmpty(SupplierHeader) ? "Поставщики: " + SupplierHeader + "\n" : string.Empty;
            headerParam += !string.IsNullOrEmpty(ServiceHeader) ? "Услуги: " + ServiceHeader : string.Empty;
            headerParam = headerParam.TrimEnd('\n');
            report.SetParameterValue("headerParam", headerParam);
        }

        public override DataSet GetData()
        {
            MyDataReader reader;

            string centralData = ReportParams.Pref + DBManager.sDataAliasRest;

            string whereSupp = GetWhereSupp("a.nzp_supp");
            string whereServ = GetWhereServ();

            bool listLc = GetSelectedKvars();

            string sql = " SELECT bd_kernel AS pref " +
                         " FROM " + ReportParams.Pref + DBManager.sKernelAliasRest + "s_point "+
                         " WHERE nzp_wp > 1 " + GetWhereWp();

            ExecRead(out reader, sql);

            while (reader.Read())
            {
                if (reader["pref"] == null) continue;
                string pref = reader["pref"].ToStr().Trim();
                string prefData = pref + DBManager.sDataAliasRest;

                sql = " INSERT INTO t_kvraj_71_5_3_1(nzp_kvar, num_ls, nzp_raj) " +
                      " SELECT nzp_kvar, num_ls, u.nzp_raj " +
                      " FROM " + prefData + "kvar k INNER JOIN " + prefData + "dom d ON d.nzp_dom = k.nzp_dom " +
                                                  " INNER JOIN " + prefData + "s_ulica u ON u.nzp_ul = d.nzp_ul " +
                      " WHERE nzp_kvar > 1 " + (listLc ? " AND nzp_kvar IN (SELECT nzp_kvar FROM selected_kvars) " : string.Empty);
                ExecSQL(sql);

                ExecSQL(DBManager.sUpdStat + " t_kvraj_71_5_3_1");

                for (int i = YearS*12 + MonthS; i < YearPo*12 + MonthPo+1; i++)
                {
                    int mo = i%12;
                    int ye = mo == 0 ? (i/12) - 1 : (i/12);
                    if (mo == 0) mo = 12;

                    string sumInsaldo = ((mo == MonthS) & (ye == YearS)) ? "sum_insaldo" : "0";
                    string sumOutsaldo = ((mo == MonthPo) & (ye == YearPo)) ? "sum_outsaldo" : "0";
                    string chargeYY = pref + "_charge_" + (ye - 2000).ToString("00") + DBManager.tableDelimiter + "charge_" + mo.ToString("00");
                    string perekidka = pref + "_charge_" + (ye - 2000).ToString("00") + DBManager.tableDelimiter + "perekidka";
                    string fromSupplier = pref + "_charge_" + (ye - 2000).ToString("00") + DBManager.tableDelimiter + "from_supplier";

                    if (TempTableInWebCashe(chargeYY))
                    {
                        sql = " INSERT INTO t_report_71_5_3_1(nzp_raj, sum_insaldo_k, sum_insaldo_d, sum_insaldo, sum_real, reval, money_to, money_from, " +
                                                    " sum_outsaldo_k, sum_outsaldo_d, sum_outsaldo)" +
                              " SELECT nzp_raj, " +
                                     " SUM(CASE WHEN sum_insaldo<0 THEN " + sumInsaldo + " ELSE 0 END) AS sum_insaldo_k," +
                                     " SUM(case when sum_insaldo<0 then 0 else " + sumInsaldo + " end) AS sum_insaldo_d," +
                                     " SUM(" + sumInsaldo + ") AS sum_insaldo," +
                                     " SUM(sum_real) AS sum_real," +
                                     " SUM(reval) AS reval," +
                                     " SUM(money_to) AS money_to, " +
                                     " SUM(money_from) AS money_from, " +
                                     " SUM(CASE WHEN sum_outsaldo < 0 THEN " + sumOutsaldo + " else 0 END) AS sum_outsaldo_k," +
                                     " SUM(CASE WHEN sum_outsaldo < 0 THEN 0 else " + sumOutsaldo + " END) AS sum_outsaldo_d," +
                                     " SUM(" + sumOutsaldo + ") AS sum_outsaldo" +
                              " FROM " + chargeYY + " a INNER JOIN t_kvraj_71_5_3_1 k ON k.nzp_kvar = a.nzp_kvar " +
                              " WHERE nzp_serv > 1 " +
                                " AND dat_charge IS NULL " + whereServ + whereSupp +
                              " GROUP BY 1";
                        ExecSQL(sql);

                        ExecSQL(DBManager.sUpdStat + " t_report_71_5_3_1");

                        sql = " UPDATE t_report_71_5_3_1 " +
                              " SET real_charge = ( SELECT SUM(sum_rcl)" +
                                                  " FROM " + perekidka + " p INNER JOIN t_kvraj_71_5_3_1 k ON k.nzp_kvar = p.nzp_kvar " +
                                                  " WHERE p.type_rcl not in (100,20) " +
                                                    " AND k.nzp_raj = t_report_71_5_3_1.nzp_raj " + whereServ + GetWhereSupp("nzp_supp") +
                                                    " AND p.month_ = " + mo + ") ";
                        ExecSQL(sql);

                        sql = " UPDATE t_report_71_5_3_1 " +
                              " SET real_insaldo = ( SELECT SUM(sum_rcl)" +
                                                   " FROM " + perekidka + " p INNER JOIN t_kvraj_71_5_3_1 k ON k.nzp_kvar = p.nzp_kvar " +
                                                   " WHERE p.type_rcl in (100,20) " +
                                                     " AND k.nzp_raj = t_report_71_5_3_1.nzp_raj " + whereServ + GetWhereSupp("nzp_supp") +
                                                     " AND p.month_ = " + mo + ") ";
                        ExecSQL(sql);

                        sql = " INSERT INTO t_report_71_5_3_1(nzp_raj, money_from, money_supp)" +
                              " SELECT nzp_raj , -SUM(sum_prih) AS money_from , SUM(sum_prih) AS money_supp " +
                              " FROM " + fromSupplier + " a INNER JOIN t_kvraj_71_5_3_1 k ON k.num_ls = a.num_ls " +
                              " WHERE a.kod_sum in (49, 50, 35) " + whereServ + GetWhereSupp("a.nzp_supp") +
                                " AND dat_uchet >= '01." + mo + "." + ye + "' " +
                                " AND dat_uchet <= '" + DateTime.DaysInMonth(ye, mo) + "." + mo + "." + ye + "'" +
                              " GROUP BY 1";
                        ExecSQL(sql);

                        sql = " INSERT INTO t_all_71_5_3_1 " +
                              " SELECT * FROM t_report_71_5_3_1 ";
                        ExecSQL(sql);

                        ExecSQL(" DELETE FROM t_report_71_5_3_1 ");
                    }
                }
                ExecSQL(" DELETE FROM t_kvraj_71_5_3_1 ");
            }

            reader.Close();

            sql = " SELECT (CASE WHEN rajon ='-' THEN town ELSE rajon END) AS rajon , " +
                         " SUM(sum_insaldo_k) AS sum_insaldo_k," +
                         " SUM(sum_insaldo_d) AS sum_insaldo_d," +
                         " SUM(sum_insaldo) AS sum_insaldo," + 
                         " SUM(sum_real) AS sum_real," +
                         " SUM(reval) AS reval," +
                         " SUM(real_charge) AS real_charge," +
                         " SUM(real_insaldo) AS real_insaldo, " +
                         " SUM(money_to) AS money_to, " +
                         " SUM(money_from) AS money_from, " +
                         " SUM(money_supp) AS money_supp, " +
                         " SUM(sum_outsaldo_k) AS sum_outsaldo_k," +
                         " SUM(sum_outsaldo_d) AS sum_outsaldo_d," +
                         " SUM(sum_outsaldo) AS sum_outsaldo" +
                  " FROM t_all_71_5_3_1 a, " +
                         centralData + "s_rajon sr, " +
                         centralData + "s_town st " +
                  " WHERE a.nzp_raj = sr.nzp_raj " +
                    " AND sr.nzp_town = st.nzp_town " +
                  " GROUP BY 1 " +
                  " ORDER BY 1";
            DataTable dt = ExecSQLToTable(sql);
            dt.TableName = "Q_master";
            if (dt.Rows.Count > 65000 && ReportParams.ExportFormat == ExportFormat.Excel2007)
            {
                var dtr = dt.Rows.Cast<DataRow>().Skip(65000).ToArray();
                dtr.ForEach(dt.Rows.Remove);
            }
            var ds = new DataSet();
            ds.Tables.Add(dt);

            return ds;
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
                        string sql = " insert into selected_kvars (nzp_kvar, nzp_dom) " +
                                     " select nzp_kvar, nzp_dom from " + tSpls;
                        ExecSQL(sql);
                        ExecSQL("create index ix_sel_kvar_09 on selected_kvars(nzp_kvar)");
                        ExecSQL(DBManager.sUpdStat + " selected_kvars");
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Получить условия органичения по поставщикам
        /// </summary>
        /// <returns></returns>
        private string GetWhereSupp(string fieldPref)
        {
            string whereSupp = String.Empty;
            if (BankSupplier != null && BankSupplier.Suppliers != null)
            {

                string supp = string.Empty;
                supp = BankSupplier.Suppliers.Aggregate(supp, (current, nzpSupp) => current + (nzpSupp + ","));
                whereSupp += " AND nzp_payer_supp IN (" + supp.TrimEnd(',') + ")";
            }

            if (BankSupplier != null && BankSupplier.Principals != null)
            {

                string supp = string.Empty;
                supp = BankSupplier.Principals.Aggregate(supp, (current, nzpSupp) => current + (nzpSupp + ","));
                whereSupp += " AND nzp_payer_princip IN (" + supp.TrimEnd(',') + ")";
            }

            if (BankSupplier != null && BankSupplier.Agents != null)
            {

                string supp = string.Empty;
                supp = BankSupplier.Agents.Aggregate(supp, (current, nzpSupp) => current + (nzpSupp + ","));
                whereSupp += " AND nzp_payer_agent IN (" + supp.TrimEnd(',') + ")";
            }

            string oldsupp = ReportParams.GetRolesCondition(Constants.role_sql_supp);

            whereSupp = whereSupp.TrimEnd(',');


            if (!String.IsNullOrEmpty(whereSupp) || !String.IsNullOrEmpty(oldsupp))
            {
                if (!String.IsNullOrEmpty(oldsupp))
                    whereSupp += " AND nzp_supp IN (" + oldsupp + ")";

                //Поставщики
                if (String.IsNullOrEmpty(SupplierHeader))
                {
                    SupplierHeader = string.Empty;
                    string sql = " SELECT name_supp FROM " +
                                 ReportParams.Pref + DBManager.sKernelAliasRest + "supplier " +
                                 " WHERE nzp_supp > 0 " + whereSupp;
                    DataTable supp = ExecSQLToTable(sql);
                    foreach (DataRow dr in supp.Rows)
                    {
                        SupplierHeader += "(" + dr["name_supp"].ToString().Trim() + "), ";
                    }
                    SupplierHeader = SupplierHeader.TrimEnd(',', ' ');
                }
            }
            return " AND " + fieldPref + " IN (SELECT nzp_supp FROM " +
                   ReportParams.Pref + DBManager.sKernelAliasRest + "supplier " +
                   " WHERE nzp_supp>0 " + whereSupp + ")";
        }

        /// <summary>
        /// Получить условия органичения по услугам
        /// </summary>
        /// <returns></returns>
        private string GetWhereServ()
        {
            string whereServ = String.Empty;
            whereServ = Services != null ? Services.Aggregate(whereServ, (current, nzpServ) => current + (nzpServ + ",")) : ReportParams.GetRolesCondition(Constants.role_sql_serv);
            whereServ = whereServ.TrimEnd(',');
            whereServ = !String.IsNullOrEmpty(whereServ) ? " AND nzp_serv IN (" + whereServ + ")" : String.Empty;
            if (!string.IsNullOrEmpty(whereServ) && string.IsNullOrEmpty(ServiceHeader))
            {
                if (String.IsNullOrEmpty(ServiceHeader))
                {
                    ServiceHeader = string.Empty;
                    string sql = " SELECT service " +
                                 " FROM " + ReportParams.Pref + DBManager.sKernelAliasRest + "services " +
                                 " WHERE nzp_serv > 0 " + whereServ;
                    DataTable servTable = ExecSQLToTable(sql);
                    foreach (DataRow row in servTable.Rows)
                    {
                        ServiceHeader += row["service"].ToString().Trim() + ", ";
                    }
                    ServiceHeader = ServiceHeader.TrimEnd(',', ' ');
                }
            }
            return whereServ;
        }

        private string GetWhereWp()
        {
            string whereWp = String.Empty;
            if (BankSupplier != null && BankSupplier.Banks != null)
            {
                whereWp = BankSupplier.Banks.Aggregate(whereWp, (current, nzpWp) => current + (nzpWp + ","));
            }
            else
            {
                whereWp = ReportParams.GetRolesCondition(Constants.role_sql_wp);
            }
            whereWp = whereWp.TrimEnd(',');
            whereWp = !String.IsNullOrEmpty(whereWp) ? " AND nzp_wp IN (" + whereWp + ")" : String.Empty;
            if (!string.IsNullOrEmpty(whereWp))
            {
                TerritoryHeader = string.Empty;
                string sql = " SELECT point " +
                             " FROM " + ReportParams.Pref + DBManager.sKernelAliasRest + "s_point " +
                             " WHERE nzp_wp > 0 " + whereWp;
                DataTable terrTable = ExecSQLToTable(sql);
                foreach (DataRow row in terrTable.Rows)
                {
                    TerritoryHeader += row["point"].ToString().Trim() + ", ";
                }
                TerritoryHeader = TerritoryHeader.TrimEnd(',', ' ');
            }
            return whereWp;
        }

        protected override void CreateTempTable()
        {
             string sql = " CREATE TEMP TABLE t_report_71_5_3_1( " +
                               " nzp_raj INTEGER," +
                               " sum_insaldo " + DBManager.sDecimalType + "(14,2)," +
                               " sum_insaldo_k " + DBManager.sDecimalType + "(14,2)," +
                               " sum_insaldo_d " + DBManager.sDecimalType + "(14,2)," +
                               " sum_real " + DBManager.sDecimalType + "(14,2)," +
                               " reval " + DBManager.sDecimalType + "(14,2)," +
                               " reval_charge " + DBManager.sDecimalType + "(14,2)," +
                               " real_charge " + DBManager.sDecimalType + "(14,2)," +
                               " real_insaldo " + DBManager.sDecimalType + "(14,2), " +
                               " money_to " + DBManager.sDecimalType + "(14,2), " +
                               " money_supp " + DBManager.sDecimalType + "(14,2), " +
                               " money_from " + DBManager.sDecimalType + "(14,2), " +
                               " sum_outsaldo " + DBManager.sDecimalType + "(14,2)," +
                               " sum_outsaldo_k " + DBManager.sDecimalType + "(14,2)," +
                               " sum_outsaldo_d " + DBManager.sDecimalType + "(14,2))" + DBManager.sUnlogTempTable;
            ExecSQL(sql);

            ExecSQL(" CREATE INDEX ix_t_report_71_5_3_1 on t_report_71_5_3_1(nzp_raj) ");

            sql = " CREATE TEMP TABLE t_all_71_5_3_1( " +
                               " nzp_raj INTEGER," +
                               " sum_insaldo " + DBManager.sDecimalType + "(14,2)," +
                               " sum_insaldo_k " + DBManager.sDecimalType + "(14,2)," +
                               " sum_insaldo_d " + DBManager.sDecimalType + "(14,2)," +
                               " sum_real " + DBManager.sDecimalType + "(14,2)," +
                               " reval " + DBManager.sDecimalType + "(14,2)," +
                               " reval_charge " + DBManager.sDecimalType + "(14,2)," +
                               " real_charge " + DBManager.sDecimalType + "(14,2)," +
                               " real_insaldo " + DBManager.sDecimalType + "(14,2), " +
                               " money_to " + DBManager.sDecimalType + "(14,2), " +
                               " money_supp " + DBManager.sDecimalType + "(14,2), " +
                               " money_from " + DBManager.sDecimalType + "(14,2), " +
                               " sum_outsaldo " + DBManager.sDecimalType + "(14,2)," +
                               " sum_outsaldo_k " + DBManager.sDecimalType + "(14,2)," +
                               " sum_outsaldo_d " + DBManager.sDecimalType + "(14,2))" + DBManager.sUnlogTempTable; ;
            ExecSQL(sql);

            sql = " CREATE TEMP TABLE t_kvraj_71_5_3_1(" +
                        " nzp_kvar INTEGER, " +
                        " num_ls INTEGER, " +
                        " nzp_raj INTEGER) " + DBManager.sUnlogTempTable;
            ExecSQL(sql);

            ExecSQL(" CREATE INDEX ix_t_kvraj_71_5_3_1__1 on t_kvraj_71_5_3_1(nzp_kvar) ");
            ExecSQL(" CREATE INDEX ix_t_kvraj71_5_3_1__2 on t_kvraj_71_5_3_1(nzp_raj) ");

            if (ReportParams.CurrentReportKind == ReportKind.ListLC)
            {
                sql = " CREATE TEMP TABLE selected_kvars(" +
                      " nzp_kvar INTEGER," +
                      " nzp_dom INTEGER) " +
                DBManager.sUnlogTempTable;
                ExecSQL(sql);
            }
        }

        protected override void DropTempTable()
        {
            ExecSQL("DROP TABLE t_report_71_5_3_1");
            ExecSQL("DROP TABLE t_all_71_5_3_1");
            ExecSQL("DROP TABLE t_kvraj_71_5_3_1");
            if (ReportParams.CurrentReportKind == ReportKind.ListLC)
                ExecSQL(" DROP TABLE selected_kvars ", true);
        }

    }
}
