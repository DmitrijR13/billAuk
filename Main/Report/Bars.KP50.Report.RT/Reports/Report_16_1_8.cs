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
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;


namespace Bars.KP50.Report.RT.Reports
{
    class Report1618 : BaseSqlReport
    {
        public override string Name
        {
            get { return "16.1.8 Отчет по переплате по данным"; }
        }

        public override string Description
        {
            get { return "1.8 Отчет по переплате по данным"; }
        }

        public override IList<ReportGroup> ReportGroups
        {
            get { return new List<ReportGroup>(0); }
        }

        public override bool IsPreview
        {
            get { return false; }
        }

        protected override byte[] Template
        {
            get { return Resources.Report_16_1_8; }
        }

        public override IList<ReportKind> ReportKinds
        {
            get { return new List<ReportKind> { ReportKind.Base, ReportKind.ListLC }; }
        }


        /// <summary>Расчетный месяц</summary>
        protected int Month { get; set; }

        /// <summary>Расчетный год</summary>
        protected int Year { get; set; }

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
                new MonthParameter {Value = curCalcMonthYear != null ? curCalcMonthYear.Rows[0]["month_"] : DateTime.Today.Month },
                new YearParameter {Value = curCalcMonthYear != null ? curCalcMonthYear.Rows[0]["yearr"] : DateTime.Today.Year },
                new SupplierAndBankParameter(),
                new AreaParameter()
            };
        }

        protected override void PrepareParams()
        {
            Month = UserParamValues["Month"].GetValue<int>();
            Year = UserParamValues["Year"].Value.To<int>();
            Areas = UserParamValues["Areas"].Value.To<List<int>>();
            var values = JsonConvert.DeserializeObject<Dictionary<string, object>>(UserParamValues["SupplierAndBank"].GetValue<string>());
            Suppliers = values["Streets"] != null
                ? values["Streets"].To<JArray>().Select(x => x.Value<long>()).ToList()
                : null;
            Banks = values["Raions"] != null
                ? values["Raions"].To<JArray>().Select(x => x.Value<int>()).ToList()
                : null;
        }

        protected override void PrepareReport(FastReport.Report report)
        {
            const bool isShow = false;
            var months = new[] {"","Январь","Февраль",
                 "Март","Апрель","Май","Июнь","Июль","Август","Сентябрь",
                 "Октябрь","Ноябрь","Декабрь"};

            report.SetParameterValue("month", months[Month] + " месяц");

            report.SetParameterValue("DATE", DateTime.Now.ToShortDateString());
            report.SetParameterValue("TIME", DateTime.Now.ToLongTimeString());
            if (SupplierHeader == null && AreaHeader == null)
            {
                report.SetParameterValue("invisible_info", false);
            }
            else
            {
                report.SetParameterValue("invisible_info", true);
                SupplierHeader = SupplierHeader != null ? "Поставщик: " + SupplierHeader : SupplierHeader;
                AreaHeader = AreaHeader != null && SupplierHeader != null ? "Балансодержатель: " + AreaHeader + "\n" :
                    AreaHeader != null ? "Балансодержатель: " + AreaHeader : AreaHeader;
            }
            report.SetParameterValue("supplier",SupplierHeader );
            report.SetParameterValue("area", AreaHeader);
            report.SetParameterValue("invisible_footer", isShow);
        }

        public override DataSet GetData()
        {
            IDataReader reader;
            bool listLc = GetSelectedKvars();

            string whereSupp = GetWhereSupp();
            string whereArea = GetWhereArea("kv.");


            #region выборка в temp таблицу

            string sql = " SELECT bd_kernel as pref " +
                         " FROM " + ReportParams.Pref + DBManager.sKernelAliasRest + "s_point " +
                         " WHERE nzp_wp>1 " + GetWhereWp();
            
            ExecRead(out reader, sql);
            while (reader.Read())
            {
                string pref = reader["pref"].ToStr().Trim();

                string chargeTable = pref + "_charge_" + (Year - 2000).ToString("00") + DBManager.tableDelimiter + "charge_" + Month.ToString("00");
                string prefData = pref + DBManager.sDataAliasRest;

                if (TempTableInWebCashe(chargeTable))
                {
                    sql = " INSERT INTO Otchet_perep_dan(nzp_kvar, town, rajon, ulica, idom, ndom, nkor, ikvar, nkvar, fio, num_ls, sum_real, sum_outsaldo ) " +
                          " SELECT ch.nzp_kvar,  " + 
                                 " town, " +
                                 " rajon, " +
                                 " ulica, " +
                                 " idom,  " +
                                 " ndom, " +
                                 " nkor, " +
                                 " ikvar, " +
                                 " nkvar, " +
                                 " fio, " +
                                 " ch.num_ls, " +
                                 " SUM(sum_real) AS sum_real, " +
                                 " SUM(sum_outsaldo) AS sum_outsaldo " +
                          " FROM " + chargeTable + " ch, " +
                                     prefData + "s_town t, " +
                                     prefData + "s_rajon r, " +
                                     prefData + "s_ulica u, " +
                                     prefData + "dom d, " +
                                     prefData + "kvar kv " + (listLc ? " INNER JOIN selected_kvars k ON k.num_ls = kv.num_ls " : string.Empty) +
                       " WHERE ch.nzp_kvar=kv.nzp_kvar " +
                            " AND dat_charge IS NULL " +
                            " AND nzp_serv>1 " +
                            " AND ch.nzp_kvar = kv.nzp_kvar " +
                            " AND kv.nzp_dom = d.nzp_dom " +
                            " AND d.nzp_ul = u.nzp_ul " +
                            " AND u.nzp_raj = r.nzp_raj " +
                            " AND r.nzp_town = t.nzp_town " +
                              whereSupp + whereArea +
                          " GROUP BY 1,2,3,4,5,6,7,8,9,10,11 ";
                    ExecSQL(sql);
                }
            }
            reader.Close();
            #endregion

            sql = " SELECT town, rajon, ulica, idom, ndom, nkor, ikvar, nkvar, fio, num_ls, SUM(sum_real) AS sum_real, SUM(sum_outsaldo) AS sum_outsaldo, " +
                         " SUM(CASE WHEN sum_real=0 THEN 0 ELSE ROUND(-sum_outsaldo/sum_real,4) END) AS persent " +
                  " FROM Otchet_perep_dan " +
                  " GROUP BY 1,2,3,4,5,6,7,8,9,10" +
                  " ORDER BY town, rajon, ulica, idom, nkor, ikvar, nkvar, fio ";

            DataTable dt = ExecSQLToTable(sql);
            dt.TableName = "Q_master";
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
                int startIndex = Constants.cons_Webdata.IndexOf("Database=", StringComparison.Ordinal) + 9;
                int endIndex = Constants.cons_Webdata.Substring(startIndex, Constants.cons_Webdata.Length - startIndex).IndexOf(";", StringComparison.Ordinal);
                var tSpls = Constants.cons_Webdata.Substring(startIndex, endIndex) + DBManager.tableDelimiter + "t" + ReportParams.User.nzp_user + "_spls";
                if (TempTableInWebCashe(tSpls))
                {
                    string sql = " insert into selected_kvars (num_ls, nzp_area) " +
                                 " select num_ls, nzp_area from " + tSpls;
                    ExecSQL(sql);
                    ExecSQL("create index ix_tmpsk_ls_01 on selected_kvars(num_ls) ");
                    ExecSQL(DBManager.sUpdStat + " selected_kvars ");
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Получить условия органичения по локальному банку
        /// </summary>
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

        /// <summary>
        /// Получить условия органичения по поставщикам
        /// </summary>
        private string GetWhereSupp()
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
        /// Получить условия органичения по УК
        /// </summary>
        private string GetWhereArea(string tablePrefix)
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

                AreaHeader = String.Empty;
                var sql = " SELECT area from " +
                      ReportParams.Pref + DBManager.sDataAliasRest + "s_area " + tablePrefix.TrimEnd('.') +
                      " WHERE " + tablePrefix + "nzp_area > 0 " + result;
                var area = ExecSQLToTable(sql);
                foreach (DataRow dr in area.Rows)
                {
                    AreaHeader += dr["area"].ToString().Trim() + ",";
                }
                AreaHeader = AreaHeader.TrimEnd(',');
            }
            return result;
        }

        protected override void CreateTempTable()
        {
            var sql = new StringBuilder();            
            sql.Append(" CREATE TEMP TABLE Otchet_perep_dan ( ");
            sql.Append(" nzp_kvar INTEGER, ");
            sql.Append(" town CHARACTER(30), ");
            sql.Append(" rajon CHARACTER(30), ");
            sql.Append(" ulica CHARACTER(40), ");
            sql.Append(" idom INTEGER, ");
            sql.Append(" ndom CHARACTER(10), ");
            sql.Append(" nkor CHARACTER(3), ");
            sql.Append(" ikvar INTEGER, ");
            sql.Append(" nkvar CHARACTER(10), ");
            sql.Append(" fio CHARACTER(40), ");
            sql.Append(" num_ls INTEGER, ");
            sql.Append(" sum_real " + DBManager.sDecimalType + "(14,2), ");
            sql.Append(" sum_outsaldo " + DBManager.sDecimalType + "(14,2))" + DBManager.sUnlogTempTable); //Процент
            ExecSQL(sql.ToString());

            if (ReportParams.CurrentReportKind == ReportKind.ListLC)
            {
                string sql1 = " create temp table selected_kvars(" +
                    " num_ls integer, " +
                    " nzp_geu integer, " +
                    " nzp_area integer) " +
                DBManager.sUnlogTempTable;
                ExecSQL(sql1);
            }
            
        }

        protected override void DropTempTable()
        {
            ExecSQL(" DROP TABLE Otchet_perep_dan ");
            if (ReportParams.CurrentReportKind == ReportKind.ListLC)
                ExecSQL(" drop table selected_kvars ", true);
        }
    }
}
