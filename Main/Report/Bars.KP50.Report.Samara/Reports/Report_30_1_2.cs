using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Bars.KP50.Report.Base;
using Bars.KP50.Report.Samara.Properties;
using Bars.KP50.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;

namespace Bars.KP50.Report.Samara.Reports
{
    class Report3012 : BaseSqlReport
    {
        public override string Name
        {
            get { return "30.1.2 Отчет по начислению за отопление"; }
        }

        public override string Description
        {
            get { return "Отчет по начислению за отопление"; }
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
            get { return Resources.Report_30_1_2; }
        }

        public override IList<ReportKind> ReportKinds
        {
            get { return new List<ReportKind> { ReportKind.Base }; }
        }


        /// <summary>Расчетный месяц</summary>
        protected int Month { get; set; }

        /// <summary>Расчетный год</summary>
        protected int Year { get; set; }

        /// <summary>Районы</summary>
        protected List<int> Rajons { get; set; }

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
                new SupplierAndBankParameter(),
                new RaionsParameter()
            };
        }


        protected override void PrepareParams()
        {
            Month = UserParamValues["Month"].GetValue<int>();
            Year = UserParamValues["Year"].Value.To<int>();
            Rajons = UserParamValues["Raions"].Value.To<List<int>>();

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

                string calcGkuTable = pref + "_charge_" + (Year - 2000).ToString("00") + DBManager.tableDelimiter + "calc_gku_" + Month.ToString("00");
                string chargeTable = pref + "_charge_" + (Year - 2000).ToString("00") + DBManager.tableDelimiter + "charge_" + Month.ToString("00");
                string prefData = pref + DBManager.sDataAliasRest,
                        prefKernel = pref + DBManager.sKernelAliasRest;

                if (TempTableInWebCashe(calcGkuTable) && TempTableInWebCashe(chargeTable))
                {

                    sql = " create temp table td (nzp_dom integer, catel char(20)) " +DBManager.sUnlogTempTable;
                    ExecSQL(sql);

                    sql = "insert into td(nzp_dom, catel)" +
                          " select nzp, val_prm from " +
                          prefData + "prm_2 p2 " +
                          " WHERE p2.nzp_prm = 1179 " +
                          " AND p2.is_actual<>100 " +
                          " and p2.dat_s<='01." + Month + "." + Year + "'" +
                          " and p2.dat_po>='01." + Month + "." + Year + "'";
                    ExecSQL(sql);

                    ExecSQL("create index ix_calctd_01 on td(nzp_dom)");
                    ExecSQL(DBManager.sUpdStat+ " td");


                    sql =
                        " INSERT INTO t_otchet_zaotop(pref, nzp_kvar,catel, service,sum_plos,sum_gil,rsum_tarif,sum_reval) " +
                        "  SELECT '" + pref + "' AS pref," +
                        " calc.nzp_kvar, " +
                        " catel, " +
                        " service, " +
                        " squ AS sum_plos, " +
                        " (ROUND(calc.gil)) AS sum_gil, " +
                        " 0 AS rsum_tarif, " +
                        " 0 AS sum_reval " +
                        " FROM td p2, " +
                        calcGkuTable + " calc, " +
                        prefKernel + "services s " +
                        " WHERE  p2.nzp_dom = calc.nzp_dom and calc.tarif>0  " +
                        " AND calc.nzp_serv =8 " +
                        " AND calc.dat_charge IS NULL " +
                        " AND s.nzp_serv = calc.nzp_serv " + GetWhereSupp() + GetRajon("p2.") +
                        " GROUP BY 1,2,3,4,5,6,7,8 ";
                    ExecSQL(sql);

                    sql = " INSERT INTO t_otchet_zaotop(pref, nzp_kvar,catel, service,sum_plos,sum_gil,rsum_tarif,sum_reval) " +
                          " SELECT '" + pref + "' AS pref, " +
                                  " ch.nzp_kvar, " +
                                  " catel, " +
                                  " service, " +
                                  " 0 AS sum_plos, " +
                                  " 0 AS sum_gil, " +
                                  " rsum_tarif AS rsum_tarif, " +
                                  " reval + real_charge - sum_nedop AS sum_reval " +
                          " FROM td p2, " +
                                     chargeTable + " ch," +
                                     prefData + "kvar kv, " +
                                     prefKernel + "services s " +
                          " WHERE p2.nzp_dom = kv.nzp_dom " +
                            " AND kv.nzp_kvar=ch.nzp_kvar" +
                            " AND ch.nzp_serv=8 " +
                            " AND ch.dat_charge IS NULL " +
                            " AND s.nzp_serv = ch.nzp_serv " + GetWhereSupp() + GetRajon("p2.") +
                          " GROUP BY 1,2,3,4,5,6,7,8 ";
                    ExecSQL(sql);

                    sql = " UPDATE t_otchet_zaotop " +
                          " SET town = ( SELECT town   " +
                                       " FROM " + prefData + "kvar k INNER JOIN " + prefData + "dom d ON k.nzp_dom = d.nzp_dom " +
                                                   " INNER JOIN " + prefData + "s_ulica u ON d.nzp_ul = u.nzp_ul " +
                                                   " INNER JOIN " + prefData + "s_rajon r ON r.nzp_raj = u.nzp_raj" +
                                                   " INNER JOIN " + prefData + "s_town t ON t.nzp_town = r.nzp_town" +
                                                   " WHERE k.nzp_kvar = t_otchet_zaotop.nzp_kvar ) " +
                          " WHERE t_otchet_zaotop.pref = '" + pref + "' ";
                    ExecSQL(sql);

                    ExecSQL("drop table td");

                    sql = " INSERT INTO t_report_30_1_2 (pref) " +
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


            sql = " SELECT catel,service," +
                         " t1.pref, " +
                         " TRIM(town) AS town, " +
                         " TRIM(name_agent) AS name_agent, " +
                         " TRIM(director_post) AS director_post, " +
                         " TRIM(chief_charge_post) AS chief_charge_post, " +
                         " TRIM(chief_finance_post) AS chief_finance_post, " +
                         " TRIM(director_name) AS director_name, " +
                         " TRIM(chief_charge_name) AS chief_charge_name, " +
                         " TRIM(chief_finance_name) AS chief_finance_name, " +
                         " TRIM('" + ReportParams.User.uname + "') AS executor_name,  " +
                         " SUM(sum_plos) AS sum_plos,SUM(sum_gil) AS sum_gil,SUM(rsum_tarif) AS rsum_tarif, SUM(sum_reval) AS sum_reval " +
                  " FROM t_otchet_zaotop t1 INNER JOIN t_report_30_1_2 t2 ON t1.pref = t2.pref " +
                  " GROUP BY catel, service,3,4,5,6,7,8,9,10,11,12 " +
                  " ORDER BY 3, catel";

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
        /// Ограничение по районам
        /// </summary>
        /// <returns></returns>
        public string GetRajon(string filedPref)
        {
            string whereRajon = String.Empty;
            if (Rajons != null)
            {
                whereRajon = Rajons.Aggregate(whereRajon, (current, nzpArea) => current + (nzpArea + ","));
            }
            whereRajon = whereRajon.TrimEnd(',');
            whereRajon = !String.IsNullOrEmpty(whereRajon)
                ? " AND " + filedPref + "nzp_dom in ( select nzp_dom " +
                  " from " + ReportParams.Pref + DBManager.sDataAliasRest + "dom d," +
                  ReportParams.Pref + DBManager.sDataAliasRest + "s_ulica su " +
                  " where d.nzp_ul=su.nzp_ul and su.nzp_raj in (" + whereRajon + "))"
                  : String.Empty;

            return whereRajon;
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
            
            var nameTable = ExecSQLToTable(" SELECT val_prm " + " FROM " + pref + DBManager.sDataAliasRest + "prm_10 " +
              " WHERE is_actual = 1 " +
              " AND dat_s <='" + day + "." + Month + "." + Year + "' " +
              " AND dat_po >='" + day + "." + Month + "." + Year + "' " +
              " AND nzp_prm = " + nzpPrm);

            if (nameTable.Rows.Count == 1)
            {
                string sql = " UPDATE t_report_30_1_2 " +
                             " SET " + nameColumn + " = '" + nameTable.Rows[0][0].ToString().Trim() + "' " +
                             " WHERE t_report_30_1_2.pref = '" + pref + "' ";
                ExecSQL(sql);
            }
            else
            {
                string sql = " UPDATE t_report_30_1_2 " +
                             " SET " + nameColumn + " = '";
                switch (nzpPrm)
                {
                    case 80: sql += "ЖКХ"; break;
                    case 1291: sql += "Врио директора  "; break;
                    case 1292: sql += "Нач. отдела по расщеплению платежей"; break;
                    case 1293: sql += "Нач. отдела бюджетирования и учета"; break;
                    case 1294: sql += "Звягинцев А.В."; break;
                    case 1295: sql += "Миллер Ю.А."; break;
                    case 1296: sql += "Соковых И.А."; break;
                }
                sql += "' WHERE t_report_30_1_2.pref = '" + pref + "' ";
                ExecSQL(sql);
            }
        }

        protected override void CreateTempTable()
        {
            string sql = " CREATE TEMP TABLE t_otchet_zaotop (" +
                               " pref CHARACTER(100)," +
                               " nzp_kvar INTEGER, " +
                               " town CHARACTER(30), " +
                               " catel CHARACTER(20), " +
                               " service CHARACTER(100), " +
                               " sum_plos " + DBManager.sDecimalType + "(14,6), " +
                               " sum_gil " + DBManager.sDecimalType + "(14,6), " +
                               " rsum_tarif " + DBManager.sDecimalType + "(14,2), " +
                               " sum_reval " + DBManager.sDecimalType + "(14,2)) " + DBManager.sUnlogTempTable;
            ExecSQL(sql);

            sql = " CREATE TEMP TABLE t_report_30_1_2 ( " +
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
            ExecSQL(" DROP TABLE t_otchet_zaotop ");
            ExecSQL(" DROP TABLE t_report_30_1_2 ");

            if (TempTableInWebCashe("td"))
            {
                ExecSQL("drop table td");
            }
        }
    }
}
