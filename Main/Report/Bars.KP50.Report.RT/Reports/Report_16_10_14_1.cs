using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using Bars.KP50.Report.Base;
using Bars.KP50.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using STCLINE.KP50.DataBase;
using Bars.KP50.Report.RT.Properties;
using STCLINE.KP50.Global;

namespace Bars.KP50.Report.RT.Reports
{
    class Report1610141 : BaseSqlReport
    {
        public override string Name
        {
            get { return "10.14.1 Сальдовая оборотная ведомость начислений и оплат по услугам в разрезе поставщиков"; }
        }

        public override string Description
        {
            get { return "10.14.1 Сальдовая оборотная ведомость начислений и оплат по услугам в разрезе поставщиков"; }
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
            get { return Resources._10_14_1_Saldo_nach_supp; }
        }

        public override IList<ReportKind> ReportKinds
        {
            get { return new List<ReportKind> { ReportKind.Base, ReportKind.ListLC }; }
        }

        /// <summary>Месяц</summary>
        private int Month { get; set; }

        /// <summary>Год</summary>
        private int Year { get; set; }

        /// <summary>Услуги</summary>
        private List<int> Services { get; set; }

        /// <summary>Поставщики</summary>
        private List<long> Suppliers { get; set; }

        /// <summary>Управляющие компании</summary>
        private List<int> Areas { get; set; }

        /// <summary>Услуги</summary>
        private string ServicesHeader { get; set; }

        /// <summary>Поставщики</summary>
        private string SuppliersHeader { get; set; }

        /// <summary>Управляющие компании</summary>
        private string AreasHeader { get; set; }

        /// <summary>Управляющие компании</summary>
        private List<int> Banks { get; set; }


        public override List<UserParam> GetUserParams()
        {
            var curCalcMonthYear = DBManager.GetCurMonthYear();
            return new List<UserParam>
            {
                new MonthParameter {Value = curCalcMonthYear != null ? curCalcMonthYear.Rows[0]["month_"] : DateTime.Today.Month },
                new YearParameter {Value = curCalcMonthYear != null ? curCalcMonthYear.Rows[0]["yearr"] : DateTime.Today.Year },
                new AreaParameter(),
                new SupplierAndBankParameter(),
                new ServiceParameter()
            };
        }

        public override DataSet GetData()
        {
            MyDataReader reader;
            var sql = new StringBuilder();
            string whereSupp = GetWhereSupp("");
            string whereArea = GetWhereAdr("k.");
            string whereServ = GetWhereServ("");
            bool listLc = GetSelectedKvars();


            sql.Remove(0, sql.Length);
            sql.Append(" SELECT * ");
            sql.Append(" FROM  " + ReportParams.Pref + DBManager.sKernelAliasRest + "s_point ");
            sql.Append(" WHERE nzp_wp>1 " + GetwhereWp());
            ExecRead(out reader, sql.ToString());
            while (reader.Read())
            {                
                string pref = reader["bd_kernel"].ToString().ToLower().Trim();

                string chargeTable = pref + "_charge_" + (Year - 2000).ToString("00") + DBManager.tableDelimiter +
                                     "charge_" + Month.ToString("00");

                if (TempTableInWebCashe(chargeTable))
                {
                    sql.Remove(0, sql.Length);
                    sql.Append(" insert into t_svod (nzp_area,nzp_geu,nzp_serv,nzp_supp,sum_insaldo, " +
                               " sum_2,rsum_lgota,sum_real,sum_5,sum_outsaldo,sum_nedop,sum_nedop_p, " +
                               " sum_money, money_from, money_del) " +
                               " select nzp_area, nzp_geu, (case when nzp_serv=210 then 25 else nzp_serv end), nzp_supp, " +
                               " sum(sum_insaldo) as sum_insaldo, " +
                               " sum(sum_tarif+sum_nedop+sum_lgota-rsum_lgota) as sum_2, " +
                               " sum(rsum_lgota) as rsum_lgota, " +
                               " sum(sum_real) as sum_real, " +
                               " sum(" + DBManager.sNvlWord + "(real_charge,0)+" + DBManager.sNvlWord +
                               "(reval,0)+sum_nedop_p) as sum_5, " +
                               " sum(sum_outsaldo) as sum_outsaldo, sum(sum_nedop+sum_lgota-rsum_lgota) as sum_nedop, " +
                               " sum(sum_nedop_p) as sum_last_nedop, " +
                               " sum(sum_money) as sum_money, " +
                               " sum(money_from) as money_from, " +
                               " sum(money_del) as money_del " +
                               " from " + chargeTable + " c " +
                               " left outer join ");
                    sql.Append(listLc
                             ? " selected_kvars k on (k.nzp_kvar = c.nzp_kvar) "
                             :   ReportParams.Pref + DBManager.sDataAliasRest +
                               " kvar k on (k.nzp_kvar = c.nzp_kvar) ");
                    sql.Append(" where nzp_serv>1 " +
                                 whereArea + whereServ + whereSupp +
                               " group by 1,2,3,4 ");
                    ExecSQL(sql.ToString());
                }
            }
            reader.Close();
            DataTable dt = ExecSQLToTable(
                  " select area, service, name_supp, " +
                  " sum(sum_insaldo) as sum_insaldo ,sum(sum_2) as sum_2 ,sum(rsum_lgota) as rsum_lgota ,sum(sum_real) as sum_real ,sum(sum_5) as sum_5, " +
                  " sum(sum_outsaldo) as sum_outsaldo, sum(sum_nedop) as sum_nedop ,sum(sum_nedop_p) as sum_nedop_p, " +
                  " sum(sum_money) as sum_money, sum(money_from) as money_from, sum(money_del)  as money_del " +
                  " from t_svod t, " + ReportParams.Pref + DBManager.sDataAliasRest + "s_area a, " +
                    ReportParams.Pref + DBManager.sKernelAliasRest + "services se, " + 
                    ReportParams.Pref + DBManager.sKernelAliasRest + "supplier su " + 
                  " where t.nzp_area = a.nzp_area and t.nzp_serv = se.nzp_serv and t.nzp_supp = su.nzp_supp " +
                  " group by 1,2,3 ");
            dt.TableName = "Q_master";

            var ds = new DataSet();
            ds.Tables.Add(dt);
            return ds;
        }

        private bool GetSelectedKvars()
        {
            if (ReportParams.CurrentReportKind == ReportKind.ListLC)
            {
                int startIndex = Constants.cons_Webdata.IndexOf("Database=", System.StringComparison.Ordinal) + 9;
                int endIndex = Constants.cons_Webdata.Substring(startIndex, Constants.cons_Webdata.Length - startIndex).IndexOf(";");
                var tSpls = Constants.cons_Webdata.Substring(startIndex, endIndex) + DBManager.tableDelimiter + "t" + ReportParams.User.nzp_user + "_spls";
                if (TempTableInWebCashe(tSpls))
                {
                    string sql = " insert into selected_kvars (nzp_kvar, nzp_area, nzp_geu) " +
                                 " select nzp_kvar, nzp_area, nzp_geu from " + tSpls;
                    ExecSQL(sql);
                    return true;
                }
            }
            return false;
        }

        private string GetWhereServ(string tablePrefix)
        {
            var result = String.Empty;
            if (Services != null)
            {
                result = Services.Aggregate(result, (current, nzpServ) => current + (nzpServ + ","));
            }
            else
            {
                result = ReportParams.GetRolesCondition(Constants.role_sql_serv);
            }
            result = result.TrimEnd(',');

            if (!String.IsNullOrEmpty(result))
            {
                result = " AND " + tablePrefix + "nzp_serv in (" + result + ")";

                ServicesHeader = String.Empty;
                var sql = " SELECT service from " +
                            ReportParams.Pref + DBManager.sKernelAliasRest + "services " + tablePrefix.TrimEnd('.') +
                          " WHERE " + tablePrefix + "nzp_serv > 1 " + result;
                var area = ExecSQLToTable(sql);
                foreach (DataRow dr in area.Rows)
                {
                    ServicesHeader += dr["area"].ToString().Trim() + ",";
                }
                ServicesHeader = ServicesHeader.TrimEnd(',');
            }
            return result;
        }

        private string GetWhereAdr(string tablePrefix)
        {
            var result = String.Empty;
            if (Areas != null)
            {
                result = Areas.Aggregate(result, (current, nzpArea) => current + (nzpArea + ","));
            }
            else
            {
                result = ReportParams.GetRolesCondition(Constants.role_sql_area);
            }

            result = result.TrimEnd(',');
            if (!String.IsNullOrEmpty(result))
            {
                result = " AND " + tablePrefix + "nzp_area in (" + result + ")";

                AreasHeader = String.Empty;
                var sql = " SELECT area from " +
                            ReportParams.Pref + DBManager.sDataAliasRest + "s_area " + tablePrefix.TrimEnd('.') +
                          " WHERE " + tablePrefix + "nzp_area > 0 " + result;
                var area = ExecSQLToTable(sql);
                foreach (DataRow dr in area.Rows)
                {
                    AreasHeader += dr["area"].ToString().Trim() + ",";
                }
                AreasHeader = AreasHeader.TrimEnd(',');
            }
            return result;
        }

        private string GetWhereSupp(string tablePrefix)
        {
            string result = String.Empty;
            if (Suppliers != null)
            {
                result = Suppliers.Aggregate(result, (current, nzpSupp) => current + (nzpSupp + ","));
            }
            else
            {
                result = ReportParams.GetRolesCondition(Constants.role_sql_supp);
            }
            result = result.TrimEnd(',');


            if (!String.IsNullOrEmpty(result))
            {
                result = " AND " + tablePrefix + "nzp_supp in (" + result + ")";

                //Поставщики
                SuppliersHeader = String.Empty;
                var sql = " SELECT name_supp from " +
                            ReportParams.Pref + DBManager.sKernelAliasRest + "supplier " + tablePrefix.TrimEnd('.') +
                          " WHERE " + tablePrefix + "nzp_supp > 0 " + result;
                DataTable supp = ExecSQLToTable(sql);
                foreach (DataRow dr in supp.Rows)
                {
                    SuppliersHeader += dr["name_supp"].ToString().Trim() + ",";
                }
                SuppliersHeader = SuppliersHeader.TrimEnd(',');
            }
            return result;
        }


        private string GetwhereWp()
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

        protected override void PrepareReport(FastReport.Report report)
        {
            string[] months = new string[] { "","Январь", "Февраль", "Март",
            "Апрель", "Май", "Июнь", "Июль", "Август", "Сентябрь",
            "Октябрь","Ноябрь","Декабрь"};
            string[] months2 = new string[] { "","Январе", "Феврале", "Марте",
            "Апреле", "Мае", "Июне", "Июле", "Августе", "Сентябре",
            "Октябре","Ноябре","Декабре"};
            int prevYear = Year;
            int prevMonth = Month - 1;
            if (prevMonth == 0) { prevYear -= 1; prevMonth = 12; } 
            report.SetParameterValue("time", DateTime.Now.ToShortTimeString());
            report.SetParameterValue("date", DateTime.Now.ToShortDateString());
            report.SetParameterValue("supp", SuppliersHeader);
            report.SetParameterValue("serv", ServicesHeader);
            report.SetParameterValue("month", months2[Month]);
            report.SetParameterValue("year", Year);
            report.SetParameterValue("month1", months[prevMonth]);
            report.SetParameterValue("year1", prevYear);
            report.SetParameterValue("month2", months[Month]);
        }

        protected override void PrepareParams()
        {
            Month = UserParamValues["Month"].GetValue<int>();
            Year = UserParamValues["Year"].GetValue<int>();
            Areas = UserParamValues["Areas"].GetValue<List<int>>();
            Services = UserParamValues["Services"].GetValue<List<int>>(); 
            
            var values = JsonConvert.DeserializeObject<Dictionary<string, object>>(UserParamValues["SupplierAndBank"].GetValue<string>());
            Suppliers = values["Streets"] != null
                ? values["Streets"].To<JArray>().Select(x => x.Value<long>()).ToList()
                : null;
            Banks = values["Raions"] != null
                ? values["Raions"].To<JArray>().Select(x => x.Value<int>()).ToList()
                : null;

        }

        protected override void CreateTempTable()
        {
            string sql = " create temp table t_svod(" +
            " nzp_area integer, " +
            " nzp_geu integer, " +
            " nzp_serv integer, " +
            " nzp_supp integer, " +
            " sum_insaldo " + DBManager.sDecimalType + "(14,2), " +
            " sum_2 " + DBManager.sDecimalType + "(14,2), " +
            " rsum_lgota " + DBManager.sDecimalType + "(14,2), " +
            " sum_real " + DBManager.sDecimalType + "(14,2), " +
            " sum_5 " + DBManager.sDecimalType + "(14,2), " +
            " sum_nedop " + DBManager.sDecimalType + "(14,2), " +
            " sum_nedop_p " + DBManager.sDecimalType + "(14,2), " +
            " sum_money " + DBManager.sDecimalType + "(14,2), " +
            " money_from " + DBManager.sDecimalType + "(14,2), " +
            " money_del " + DBManager.sDecimalType + "(14,2), " +
            " sum_outsaldo " + DBManager.sDecimalType + "(14,2)) " +
            DBManager.sUnlogTempTable;
            ExecSQL(sql);

            if (ReportParams.CurrentReportKind == ReportKind.ListLC)
            {
                sql = " create temp table selected_kvars(" +
                    " nzp_kvar integer, " +
                    " nzp_geu integer, " +
                    " nzp_area integer) " +
                DBManager.sUnlogTempTable;
                ExecSQL(sql); 
            }
        }

        protected override void DropTempTable()
        {
            ExecSQL(" drop table t_svod ", true);
            if (ReportParams.CurrentReportKind == ReportKind.ListLC)
                ExecSQL(" drop table selected_kvars ", true);
        }

    }
}
