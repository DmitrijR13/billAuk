using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Bars.KP50.Report.Base;
using Bars.KP50.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;
using Bars.KP50.Report.Main.Properties;

namespace Bars.KP50.Report.Main.Reports
{
    public class SaldoReportRajon : BaseSqlReport
    {
        public override string Name
        {
            get { return "Базовый - Сальдовая ведомость по услугам по населенным пунктам"; }
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
            get { return Resources.SaldoRepRajon; }
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

        /// <summary>Услуги</summary>
        protected List<int> Services { get; set; }

        /// <summary>Поставщики</summary>
        protected List<long> Suppliers { get; set; }

        /// <summary>Управляющие компании</summary>
        protected List<int> Areas { get; set; }

        /// <summary>Заголовок отчета</summary>
        protected string SupplierHeader { get; set; }

        /// <summary>Заголовок отчета</summary>
        protected string AreaHeader { get; set; }

        /// <summary>Банки данных</summary>
        protected List<int> Banks { get; set; }

        public override List<UserParam> GetUserParams()
        {
            var curCalcMonthYear = DBManager.GetCurMonthYear();
            return new List<UserParam>
            {
                new MonthParameter {Name = "Месяц с", Value = curCalcMonthYear != null ? curCalcMonthYear.Rows[0]["month_"] : DateTime.Today.Month },
                new YearParameter {Name = "Год с", Value = curCalcMonthYear != null ? curCalcMonthYear.Rows[0]["yearr"] : DateTime.Today.Year },
                new MonthParameter {Name = "Месяц по", Code = "Month1", Value = curCalcMonthYear != null ? curCalcMonthYear.Rows[0]["month_"] : DateTime.Today.Month },
                new YearParameter {Name = "Год по", Code = "Year1", Value = curCalcMonthYear != null ? curCalcMonthYear.Rows[0]["yearr"] : DateTime.Today.Year },
                new SupplierAndBankParameter(),
                new ServiceParameter(),
                new AreaParameter()
            };
        }

        public override DataSet GetData()
        {
            MyDataReader reader;
            string whereSupp = GetWhereSupp();
            string whereServ = GetWhereServ();
            string whereArea = GetWhereArea(); 


            string sql = " select bd_kernel as pref " +
                         " FROM " + ReportParams.Pref + DBManager.sKernelAliasRest + "s_point "+
                         " where nzp_wp>1 " + GetWhereWp();

            ExecRead(out reader, sql);

            while (reader.Read())
            {
                if (reader["pref"] == null) continue;
                for (int i = YearS*12 + MonthS; i < YearPo*12 + MonthPo+1; i++)
                {
                    int mo = i%12;
                    int ye = mo == 0 ? (i/12) - 1 : (i/12);
                    if (mo == 0) mo = 12;
                    string pref = reader["pref"].ToStr().Trim();
                    string baseData = pref + DBManager.sDataAliasRest;
                    string sumInsaldo = ((mo == MonthS) & (ye == YearS)) ? "sum_insaldo" : "0";
                    string sumOutsaldo = ((mo == MonthPo) & (ye == YearPo)) ? "sum_outsaldo" : "0";
                    string tableCharge = pref + "_charge_" + (ye - 2000).ToString("00") +
                                         DBManager.tableDelimiter + "charge_" +
                                         mo.ToString("00");

                    if (TempTableInWebCashe(tableCharge))
                    {
                        sql = " insert into t_svod(nzp_raj, sum_insaldo_k, sum_insaldo_d, " +
                              " sum_insaldo, sum_real, reval, real_charge, sum_money, " +
                              " sum_outsaldo_k, sum_outsaldo_d, sum_outsaldo)" +
                              " select su.nzp_raj, sum(case when sum_insaldo<0 then sum_insaldo else 0 end) as sum_insaldo_k," +
                              " sum(case when sum_insaldo<0 then 0 else sum_insaldo end) as sum_insaldo_d," +
                              " sum(" + sumInsaldo + ") as sum_insaldo," +
                              " sum(sum_real) as sum_real," +
                              " sum(reval) as reval," +
                              " sum(real_charge) as real_charge," +
                              " sum(sum_money) as sum_money," +
                              " sum(case when sum_outsaldo<0 then sum_outsaldo else 0 end) as sum_outsaldo_k," +
                              " sum(case when sum_outsaldo<0 then 0 else sum_outsaldo end) as sum_outsaldo_d," +
                              " sum(" + sumOutsaldo + ") as sum_outsaldo" +
                              " from " + tableCharge + " a, " + baseData + "kvar k, " + baseData +
                              "dom d, " + baseData + " s_ulica su " +
                              " where a.nzp_kvar=k.nzp_kvar and  nzp_serv >1 and dat_charge is null " +
                              " and k.nzp_dom=d.nzp_dom and d.nzp_ul=su.nzp_ul " +
                              whereArea + whereServ + whereSupp +
                              " group by 1";
                        ExecSQL(sql);
                    }
                }

            }

            reader.Close();


            sql = " select (case when rajon ='-' then town else rajon end) as rajon , sum(sum_insaldo_k) as sum_insaldo_k," +
                  " sum(sum_insaldo_d) as sum_insaldo_d," +
                  " sum(sum_insaldo) as sum_insaldo," +
                  " sum(sum_real) as sum_real," +
                  " sum(reval) as reval," +
                  " sum(real_charge) as real_charge," +
                  " sum(reval) + sum(real_charge) as reval_charge," +
                  " sum(sum_money) as sum_money," +
                  " sum(sum_outsaldo_k) as sum_outsaldo_k," +
                  " sum(sum_outsaldo_d) as sum_outsaldo_d," +
                  " sum(sum_outsaldo) as sum_outsaldo" +
                  " from t_svod a, " +
                  ReportParams.Pref+DBManager.sDataAliasRest+"s_rajon sr, " +
                  ReportParams.Pref + DBManager.sDataAliasRest + "s_town st " +
                  " where a.nzp_raj=sr.nzp_raj and sr.nzp_town=st.nzp_town " +
                  " group by 1 " +
                  " order by 1";
            DataTable dt = ExecSQLToTable(sql);
            dt.TableName = "Q_master";

            var ds = new DataSet();
            ds.Tables.Add(dt);

            return ds;
        }

        /// <summary>
        /// Получить условия органичения по УК
        /// </summary>
        /// <returns></returns>
        private string GetWhereArea()
        {
            string whereArea = String.Empty;
            whereArea = Areas != null ? Areas.Aggregate(whereArea, (current, nzpArea) => current + (nzpArea + ",")) : ReportParams.GetRolesCondition(Constants.role_sql_area);
            whereArea = whereArea.TrimEnd(',');
            whereArea = !String.IsNullOrEmpty(whereArea) ? " AND k.nzp_area in (" + whereArea + ")" : String.Empty;
            if (!String.IsNullOrEmpty(whereArea))
            {
                string sql = " SELECT area from " + ReportParams.Pref + DBManager.sDataAliasRest + "s_area k  WHERE k.nzp_area > 0 " + whereArea;
                DataTable area = ExecSQLToTable(sql);
                foreach (DataRow dr in area.Rows)
                {
                    AreaHeader += dr["area"].ToString().Trim() + ", ";
                }
                AreaHeader = AreaHeader.TrimEnd(',', ' ');
            }
            return whereArea;
        }

        /// <summary>
        /// Получить условия органичения по поставщикам
        /// </summary>
        /// <returns></returns>
        private string GetWhereSupp()
        {
            string whereSupp = String.Empty;
            whereSupp = Suppliers != null ? Suppliers.Aggregate(whereSupp, (current, nzpSupp) => current + (nzpSupp + ",")) : ReportParams.GetRolesCondition(Constants.role_sql_supp);
            whereSupp = whereSupp.TrimEnd(',');
            whereSupp = !String.IsNullOrEmpty(whereSupp) ? " AND nzp_supp in (" + whereSupp + ")" : String.Empty;
            if (!String.IsNullOrEmpty(whereSupp))
            {
                string sql = " SELECT name_supp from " + ReportParams.Pref + DBManager.sKernelAliasRest + "supplier  WHERE nzp_supp > 0 " + whereSupp;
                DataTable supp = ExecSQLToTable(sql);
                foreach (DataRow dr in supp.Rows)
                {
                    SupplierHeader += dr["name_supp"].ToString().Trim() + ", ";
                }
                SupplierHeader = SupplierHeader.TrimEnd(',', ' ');
            }
            return whereSupp;
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
            whereServ = !String.IsNullOrEmpty(whereServ) ? " AND nzp_serv in (" + whereServ + ")" : String.Empty;
            return whereServ;
        }

        protected override void CreateTempTable()
        {
            const string sql = " create temp table t_svod( " +
                               " nzp_raj integer," +
                               " sum_insaldo " + DBManager.sDecimalType + "(14,2)," +
                               " sum_insaldo_k " + DBManager.sDecimalType + "(14,2)," +
                               " sum_insaldo_d " + DBManager.sDecimalType + "(14,2)," +
                               " sum_real " + DBManager.sDecimalType + "(14,2)," +
                               " reval " + DBManager.sDecimalType + "(14,2)," +
                               " reval_charge " + DBManager.sDecimalType + "(14,2)," +
                               " real_charge " + DBManager.sDecimalType + "(14,2)," +
                               " sum_outsaldo " + DBManager.sDecimalType + "(14,2)," +
                               " sum_outsaldo_k " + DBManager.sDecimalType + "(14,2)," +
                               " sum_outsaldo_d " + DBManager.sDecimalType + "(14,2)," +
                               " sum_money " + DBManager.sDecimalType + "(14,2))";

            ExecSQL(sql);
        }

        protected override void DropTempTable()
        {
            ExecSQL("drop table t_svod");
        }


        protected override void PrepareReport(FastReport.Report report)
        {
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
            string headers = String.Empty;
            if (!String.IsNullOrEmpty(AreaHeader)) headers = AreaHeader;
            if (!String.IsNullOrEmpty(SupplierHeader))
            {
                if (SupplierHeader.Contains(","))
                {
                    headers += " поставщикам: " + SupplierHeader;
                }
                else
                {
                    headers += " поставщику: " + SupplierHeader;
                }
            }

            if (String.IsNullOrEmpty(headers)) headers = "Всем лс";
            report.SetParameterValue("headers", headers);
        }
        
        private string GetWhereWp()
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
            whereWp = !String.IsNullOrEmpty(whereWp) ? " AND nzp_wp in (" + whereWp + ")" : String.Empty;
            return whereWp;
        }

        protected override void PrepareParams()
        {
            MonthS = UserParamValues["Month"].GetValue<int>();
            YearS = UserParamValues["Year"].Value.To<int>();
            MonthPo = UserParamValues["Month1"].GetValue<int>();
            YearPo = UserParamValues["Year1"].Value.To<int>();
            
            Services = UserParamValues["Services"].Value.To<List<int>>();
            Areas = UserParamValues["Areas"].Value.To<List<int>>();

            var values = JsonConvert.DeserializeObject<Dictionary<string, object>>(UserParamValues["SupplierAndBank"].GetValue<string>());
            Suppliers = values["Streets"] != null
                ? values["Streets"].To<JArray>().Select(x => x.Value<long>()).ToList()
                : null;
            Banks = values["Raions"] != null
                ? values["Raions"].To<JArray>().Select(x => x.Value<int>()).ToList()
                : null;
    
        }
    }
}
