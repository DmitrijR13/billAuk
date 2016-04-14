using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Bars.KP50.Report.Base;
using Bars.KP50.Utils;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;
using Bars.KP50.Report.RT.Properties;

namespace Bars.KP50.Report.RT.Reports
{
    public class Report16510 : BaseSqlReport
    {
        public override string Name
        {
            get { return "16.5.10 Сальдовая ведомость"; }
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
            get { return Resources.Web_saldo_rep5_10; }
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

        /// <summary>Районы</summary>
        protected List<int> Rajons { get; set; }

        /// <summary>Заголовок отчета</summary>
        protected string SupplierHeader { get; set; }

        /// <summary>Заголовок отчета</summary>
        protected string AreaHeader { get; set; }

        /// <summary>Заголовок отчета</summary>
        protected string RajonHeader { get; set; }

        public override List<UserParam> GetUserParams()
        {
            var curCalcMonthYear = DBManager.GetCurMonthYear();
            return new List<UserParam>
            {
                new RaionsParameter(),
                new MonthParameter {Name = "Месяц с", Value = curCalcMonthYear != null ? curCalcMonthYear.Rows[0]["month_"] : DateTime.Today.Month },
                new YearParameter {Name = "Год с", Value = curCalcMonthYear != null ? curCalcMonthYear.Rows[0]["yearr"] : DateTime.Today.Year },
                new MonthParameter {Name = "Месяц по", Code = "Month1", Value = curCalcMonthYear != null ? curCalcMonthYear.Rows[0]["month_"] : DateTime.Today.Month },
                new YearParameter {Name = "Год по", Code = "Year1", Value = curCalcMonthYear != null ? curCalcMonthYear.Rows[0]["yearr"] : DateTime.Today.Year },

                new ServiceParameter(),
                new SupplierParameter(),
                new AreaParameter()
            };
        }

        public override DataSet GetData()
        {
            MyDataReader reader;
            string whereSupp = GetWhereSupp();
            string whereServ = GetWhereServ();
            string whereArea = GetWhereArea(); 
            string whereRaj = GetWhereRaj();

            string sql;


            sql = " select bd_kernel as pref " +
                  " FROM " + ReportParams.Pref + DBManager.sKernelAliasRest + "s_point " +
                  " where nzp_wp>1 ";

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
                    string baseData =ReportParams.Pref + DBManager.sDataAliasRest;
                    string sumInsaldo = ((mo == MonthS) & (ye == YearS)) ? "sum_insaldo" : "0";
                    string sumOutsaldo = ((mo == MonthPo) & (ye == YearPo)) ? "sum_outsaldo" : "0";
                    string tableCharge = pref + "_charge_" + (ye - 2000).ToString("00") +
                                         DBManager.tableDelimiter + "charge_" +
                                         mo.ToString("00");

                    if (TempTableInWebCashe(tableCharge))
                    {
                        sql = " insert into t_svod(nzp_serv, sum_insaldo_k, sum_insaldo_d, " +
                              " sum_insaldo, sum_real, reval, real_charge, sum_money, " +
                              " sum_outsaldo_k, sum_outsaldo_d, sum_outsaldo)" +
                              " select nzp_serv , sum(case when sum_insaldo<0 then sum_insaldo else 0 end) as sum_insaldo_k," +
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
                              " and k.nzp_dom=d.nzp_dom and d.nzp_ul=su.nzp_ul " + whereRaj +
                              whereArea + whereServ + whereSupp +
                              " group by 1";
                        ExecSQL(sql);
                    }
                }

            }

            reader.Close();


            sql = " select service , sum(sum_insaldo_k) as sum_insaldo_k," +
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
                  DBManager.GetFullBaseName(Connection) + DBManager.tableDelimiter + "services s " +
                  " where a.nzp_serv=s.nzp_serv" +
                  " group by service " +
                  " order by service ";
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
            string where_area = String.Empty;
            if (Areas != null)
            {
                where_area = Areas.Aggregate(where_area, (current, nzpArea) => current + (nzpArea + ","));
            }
            else
            {
                where_area = ReportParams.GetRolesCondition(Constants.role_sql_area);
            }
            where_area = where_area.TrimEnd(',');
            where_area = !String.IsNullOrEmpty(where_area) ? " AND k.nzp_area in (" + where_area + ")" : String.Empty;
            if (!String.IsNullOrEmpty(where_area))
            {
                string sql = " SELECT area from " + ReportParams.Pref + DBManager.sDataAliasRest + "s_area k  WHERE k.nzp_area > 0 " + where_area;
                DataTable area = ExecSQLToTable(sql.ToString());
                foreach (DataRow dr in area.Rows)
                {
                    AreaHeader += dr["area"].ToString().Trim() + ", ";
                }
                AreaHeader = AreaHeader.TrimEnd(',', ' ');
            }
            return where_area;
        }

        /// <summary>
        /// Получить условия органичения по поставщикам
        /// </summary>
        /// <returns></returns>
        private string GetWhereSupp()
        {
            string where_supp = String.Empty;
            if (Suppliers != null)
            {
                where_supp = Suppliers.Aggregate(where_supp, (current, nzpSupp) => current + (nzpSupp + ","));
            }
            else
            {
                where_supp = ReportParams.GetRolesCondition(Constants.role_sql_supp);
            }
            where_supp = where_supp.TrimEnd(',');
            where_supp = !String.IsNullOrEmpty(where_supp) ? " AND nzp_supp in (" + where_supp + ")" : String.Empty;
            if (!String.IsNullOrEmpty(where_supp))
            {
                string sql = " SELECT name_supp from " + ReportParams.Pref + DBManager.sKernelAliasRest + "supplier  WHERE nzp_supp > 0 " + where_supp;
                DataTable supp = ExecSQLToTable(sql.ToString());
                foreach (DataRow dr in supp.Rows)
                {
                    SupplierHeader += dr["name_supp"].ToString().Trim() + ", ";
                }
                SupplierHeader = SupplierHeader.TrimEnd(',', ' ');
            }
            return where_supp;
        }

        /// <summary>
        /// Получить условия органичения по услугам
        /// </summary>
        /// <returns></returns>
        private string GetWhereServ()
        {
            string where_serv = String.Empty;
            if (Services != null)
            {
                where_serv = Services.Aggregate(where_serv, (current, nzpServ) => current + (nzpServ + ","));
            }
            else
            {
                where_serv = ReportParams.GetRolesCondition(Constants.role_sql_serv);
            }
            where_serv = where_serv.TrimEnd(',');
            where_serv = !String.IsNullOrEmpty(where_serv) ? " AND nzp_serv in (" + where_serv + ")" : String.Empty;
            return where_serv;
        }

        /// <summary>
        /// Получить условия органичения по районам
        /// </summary>
        /// <returns></returns>
        private string GetWhereRaj()
        {
            string where_Raj = String.Empty;
            if (Rajons != null)
            {
                where_Raj = Rajons.Aggregate(where_Raj, (current, nzpRaj) => current + (nzpRaj + ","));
                where_Raj = where_Raj.TrimEnd(',');
                where_Raj = !String.IsNullOrEmpty(where_Raj) ? " AND su.nzp_raj in (" + where_Raj + ")" : String.Empty;
                if (!String.IsNullOrEmpty(where_Raj))
                {
                    RajonHeader = String.Empty;
                    var sql =
                        " select trim(t.town) || '/' || trim(su.rajon) as rajon, su.nzp_raj " +
                        " from " + ReportParams.Pref + DBManager.sDataAliasRest + "s_rajon su, " +
                          ReportParams.Pref + DBManager.sDataAliasRest + "s_town t " +
                        " where t.nzp_town = su.nzp_town   " +
                          where_Raj +
                        " group by 1,2 " +
                        " order by 1,2";
                    DataTable supp = ExecSQLToTable(sql);
                    foreach (DataRow dr in supp.Rows)
                    {
                        RajonHeader += dr["rajon"].ToString().Trim() + ",";
                    }
                    RajonHeader = RajonHeader.TrimEnd(',');
                }
            }
            else
            {
                string sql = " select val_prm " +
                             " from " + ReportParams.Pref + DBManager.sDataAliasRest + "prm_10 " +
                             " where nzp_prm=80 and is_actual=1 and dat_s<=" + DBManager.sCurDate +
                             " and dat_po>=" + DBManager.sCurDate;
                DataTable erc = ExecSQLToTable(sql);
                if (erc != null)
                    if (erc.Rows.Count > 0)
                    {
                        RajonHeader = erc.Rows[0]["val_prm"].ToString().Trim();
                    }
                    else
                    {
                        RajonHeader = "Не определено наименование Расчетного центра";
                    }
            }
            return where_Raj;
        }
        protected override void CreateTempTable()
        {
            const string sql = " create temp table t_svod( " +
                               " nzp_serv integer," +
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
            if (!String.IsNullOrEmpty(RajonHeader))
            {
                if (RajonHeader.Contains(","))
                {
                    headers += " районам: " + RajonHeader;
                }
                else
                {
                    headers += " району: " + RajonHeader;
                }
            }
            if (String.IsNullOrEmpty(headers)) headers = "Всем лс";
            report.SetParameterValue("headers", headers);
        }

        protected override void PrepareParams()
        {
            MonthS = UserParamValues["Month"].GetValue<int>();
            YearS = UserParamValues["Year"].Value.To<int>();
            MonthPo = UserParamValues["Month1"].GetValue<int>();
            YearPo = UserParamValues["Year1"].Value.To<int>();
            
            Services = UserParamValues["Services"].Value.To<List<int>>();
            Suppliers = UserParamValues["Suppliers"].Value.To<List<long>>();
            Areas = UserParamValues["Areas"].Value.To<List<int>>();
            Rajons = UserParamValues["Raions"].Value.To<List<int>>();
    
        }
    }
}
