using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Bars.KP50.Report.Base;
using Bars.KP50.Report.Samara.Properties;
using Bars.KP50.Utils;
using Castle.MicroKernel.Registration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;

namespace Bars.KP50.Report.Samara.Reports
{
    class Report3011 : BaseSqlReport
    {
        public override string Name
        {
            get { return "30.1.1 Отчет по льготам"; }
        }

        public override string Description
        {
            get { return "Отчет по льготам"; }
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
            get { return Resources.Report_30_1_1; }
        }

        public override IList<ReportKind> ReportKinds
        {
            get { return new List<ReportKind> { ReportKind.Base }; }
        }

        /// <summary>Расчетный месяц</summary>
        protected int Month { get; set; }

        /// <summary>Расчетный год</summary>
        protected int Year { get; set; }

        /// <summary>Поставщики</summary>
        protected List<long> Suppliers { get; set; }

        /// <summary>Банки данных</summary>
        protected List<int> Banks { get; set; }


        public override List<UserParam> GetUserParams()
        {

            return new List<UserParam>
            {
                new MonthParameter {Value = DateTime.Today.Month },
                new YearParameter {Value = DateTime.Today.Year },
                new SupplierAndBankParameter()
            };
        }

        protected override void PrepareParams()
        {
            Month = UserParamValues["Month"].GetValue<int>();
            Year = UserParamValues["Year"].Value.To<int>();

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

            report.SetParameterValue("period_month", months[Month] + " " + Year + " г.");

            report.SetParameterValue("DATE", DateTime.Now.ToShortDateString());
            report.SetParameterValue("TIME", DateTime.Now.ToLongTimeString()); 
            report.SetParameterValue("invisible_footer", isShow);

            report.SetParameterValue("executor_name",ReportParams.User.uname);
        }

        public override DataSet GetData()
        {
            IDataReader reader;

            #region выборка в temp таблицу

            string sql = " SELECT bd_kernel as pref " +
                         " FROM " + ReportParams.Pref + DBManager.sKernelAliasRest + "s_point " +
                         " WHERE nzp_wp>1 " + GetwhereWp();

            ExecRead(out reader, sql);
            while (reader.Read())
            {
                string pref = reader["pref"].ToStr().Trim();

                string perekidkaTable = pref + "_charge_" + (Year - 2000).ToString("00") + DBManager.tableDelimiter + "perekidka";
                string prefData = pref + DBManager.sDataAliasRest;

                if (TempTableInWebCashe(perekidkaTable))
                {
                    sql = " INSERT INTO t_otchet_polg(pref, nzp_kvar, num_ls, fio,town, rajon, ulica, ndom, nkor, nkvar, sum_otop, sum_hol, sum_gor) " +
                          "  SELECT '" + pref + "' AS pref, " +
                                  " p.nzp_kvar, " +
                                  " p.num_ls, " +
                                  " kv.fio, " +
                                  " town, " +
                                  " rajon, " +
                                  " ulica, " +
                                  " ndom, " +
                                  " nkor, " +
                                  " nkvar, " +
                                  " SUM(CASE WHEN nzp_serv=8 THEN  -sum_rcl ELSE 0 END) AS sum_otop, " +
                                  " SUM(CASE WHEN nzp_serv=14 THEN  -sum_rcl ELSE 0 END) AS sum_hol, " +
                                  " SUM(CASE WHEN nzp_serv=9 THEN  -sum_rcl ELSE 0 END) AS sum_gor " +
                          " FROM " + perekidkaTable + " p, " +
                                     prefData + "kvar kv, " +
                                     prefData + "dom d, " +
                                     prefData + "s_ulica u, " +
                                     prefData + "s_rajon r, " +
                                     prefData + "s_town t " +
                          " WHERE p.nzp_kvar=kv.nzp_kvar" +
                            " AND kv.nzp_dom=d.nzp_dom " +
                            " AND d.nzp_ul=u.nzp_ul " +
                            " AND u.nzp_raj=r.nzp_raj " +
                            " AND r.nzp_town = t.nzp_town " +
                            " AND p.nzp_serv IN (8,9,14) " +
                            " AND p.type_rcl=263 " +
                            " AND month_= " + Month + GetWhereSupp() +
                            " GROUP BY 1,2,3,4,5,6,7,8,9,10 ";
                    ExecSQL(sql);
                
                   sql = " INSERT INTO t_report_30_1_1 (pref) " +
                      " VALUES ('" + pref + "') ";
                ExecSQL(sql);

                GetNameResponsible(pref, 80); // Имя агента
                GetNameResponsible(pref, 1291); // Наименование должности директора РЦ
                GetNameResponsible(pref, 1292); // Наименоваине должности начальника отдела начислений 
                GetNameResponsible(pref, 1293); // Наименование должности начальника отдела финансов
                GetNameResponsible(pref, 1294); // ФИО директора РЦ      
                GetNameResponsible(pref, 1295); // ФИО начальника отдела начислений
                GetNameResponsible(pref, 1296); // ФИО начальника отдела финансов
                }

                
            }

            reader.Close();
            #endregion


            sql = " SELECT num_ls, fio,town, rajon, ulica, ndom, nkor, nkvar, " +
                         " t1.pref, " +
                         " TRIM(name_agent) AS name_agent, " +
                         " TRIM(director_post) AS director_post, " +
                         " TRIM(chief_charge_post) AS chief_charge_post, " +
                         " TRIM(chief_finance_post) AS chief_finance_post, " +
                         " TRIM(director_name) AS director_name, " +
                         " TRIM(chief_charge_name) AS chief_charge_name, " +
                         " TRIM(chief_finance_name) AS chief_finance_name, " +
                         " TRIM('" + ReportParams.User.uname + "') AS executor_name,  " +
                         " SUM(sum_otop) AS sum_otop, SUM(sum_hol) AS sum_hol, SUM(sum_gor) AS sum_gor" +
                  " FROM  t_otchet_polg t1 INNER JOIN t_report_30_1_1 t2 ON t1.pref = t2.pref " +
                  " WHERE  sum_otop<>0 OR sum_hol<>0 OR sum_gor<>0 " +
                  " GROUP BY  1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17 " +
                  " ORDER BY  pref, 1 ";

            DataTable dt = ExecSQLToTable(sql);
            dt.TableName = "Q_master";

            var ds = new DataSet();
            ds.Tables.Add(dt);
            return ds;
        }

        /// <summary>
        /// Получить условия органичения по поставщикам
        /// </summary>
        /// <returns></returns>
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
            return whereSupp;
        }

        /// <summary>
        /// Получить условия органичения по банкам данных
        /// </summary>
        /// <returns></returns>
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

        /// <summary>Запись во временную таблицу фамилии ответсвенных</summary>
        /// <param name="pref"> Приставка локального банка в базе данных </param>
        /// <param name="nzpPrm"> Идентификатор параметра </param>
        private void GetNameResponsible(string pref, int nzpPrm)
        {
            int day = Month == DateTime.Now.Month && Year == DateTime.Now.Year ? DateTime.Now.Day : 1;
            string nameColumn = string.Empty;
            switch (nzpPrm)
            {
                case 80: nameColumn = "name_agent"; break;
                case 1291: nameColumn = "director_post"; break;
                case 1292: nameColumn = "chief_charge_post"; break;
                case 1293: nameColumn = "chief_finance_post"; break;
                case 1294: nameColumn = "director_name"; break;
                case 1295: nameColumn = "chief_charge_name"; break;
                case 1296: nameColumn = "chief_finance_name"; break;
            }

            string sql = " UPDATE t_report_30_1_1 " +
                         " SET " + nameColumn + " = ( SELECT val_prm " +
                                                    " FROM " + pref + DBManager.sDataAliasRest + "prm_10 " +
                                                    " WHERE is_actual = 1 " +
                                                      " AND dat_s <='" + day + "." + Month + "." + Year + "' " +
                                                      " AND dat_po >='" + day + "." + Month + "." + Year + "' " +
                                                      " AND nzp_prm = " + nzpPrm + ") " +
                         " WHERE t_report_30_1_1.pref = '" + pref + "' ";
            ExecSQL(sql);
        }

        protected override void CreateTempTable()
        {
            string sql = " CREATE TEMP TABLE t_otchet_polg ( " +
                               " pref CHARACTER(100)," +
                               " nzp_kvar INTEGER, " +
                               " num_ls INTEGER, " +
                               " fio CHARACTER(40), " +
                               " town CHARACTER(30), " +
                               " rajon CHARACTER(30), " +
                               " ulica CHARACTER(40), " +
                               " ndom CHARACTER(10), " +
                               " nkor CHARACTER(3), " +
                               " nkvar CHARACTER(10), " +
                               " sum_otop " + DBManager.sDecimalType + "(14,2), " +
                               " sum_hol " + DBManager.sDecimalType + "(14,2), " +
                               " sum_gor " + DBManager.sDecimalType + "(14,2)) " + DBManager.sUnlogTempTable;
            ExecSQL(sql);

            sql = " CREATE TEMP TABLE t_report_30_1_1 ( " +
                        " pref CHARACTER(100)," +
                        " director_post CHARACTER(60), " +
                        " chief_charge_post CHARACTER(60), " +
                        " chief_finance_post CHARACTER(60), " +
                        " director_name CHARACTER(30), " +
                        " chief_charge_name CHARACTER(30), " +
                        " chief_finance_name CHARACTER(30), " +
                        " name_agent CHARACTER(30))" + DBManager.sUnlogTempTable;
            ExecSQL(sql);
        }

        protected override void DropTempTable()
        {
            ExecSQL(" DROP TABLE t_otchet_polg ");
            ExecSQL(" DROP TABLE t_report_30_1_1 ");
        }
    }
}
