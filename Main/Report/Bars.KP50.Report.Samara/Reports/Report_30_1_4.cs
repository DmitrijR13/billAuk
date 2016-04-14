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
    class Report3014 : BaseSqlReport
    {
        public override string Name
        {
            get { return "30.1.4 Отчет по отапливаемым площадям"; }
        }

        public override string Description
        {
            get { return "Отчет по отапливаемым площадям"; }
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
            get { return Resources.Report_30_1_4; }
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
            report.SetParameterValue("town", "");
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
                string prefData = pref + DBManager.sDataAliasRest;

                if (TempTableInWebCashe(calcGkuTable) && TempTableInWebCashe(chargeTable))
                {
                    sql = " create temp table td (nzp_dom integer, is_dpu integer default 0, catel char(20)) " +
                          DBManager.sUnlogTempTable;
                    ExecSQL(sql);

                    sql = "insert into td(nzp_dom, catel)" +
                          " select nzp, val_prm from " +
                          prefData + "prm_2 p2 " +
                          " WHERE p2.nzp_prm = 1179 " +
                          " AND p2.is_actual<>100 " +
                          " and p2.dat_s<='01." + Month + "." + Year + "'" +
                          " and p2.dat_po>='01." + Month + "." + Year + "'"+
                          GetRajon("nzp");
                    ExecSQL(sql);

                   sql ="update td set is_dpu =1 where nzp_dom in (select nzp from  " +
                        prefData + "prm_2 p2 " +
                          " WHERE p2.nzp_prm = 361 " +
                          " AND p2.is_actual<>100 " +
                          " and p2.dat_s<='01." + Month + "." + Year + "'" +
                          " and p2.dat_po>='01." + Month + "." + Year + "')";
                   ExecSQL(sql);
 
                    
                    sql = " INSERT INTO t_otchet_otoplS(pref, nzp_kvar,is_gvc, catel,sum_m_counter,sum_m_norm,sum_gil) " +
                          " SELECT '" + pref + "' AS pref, " + 
                                 " nzp_kvar, "+
                                 " 1 AS is_gvc, " +
                                 "  catel, " +
                                 " (CASE WHEN is_dpu<>0 THEN squ ELSE 0 END) AS sum_m_counter, " +
                                 " (CASE WHEN is_dpu=0 THEN squ ELSE 0 END) AS sum_m_norm, " +
                                 " (ROUND(calc.gil)) AS sum_gil " +
                          " FROM td p2, " +
                               calcGkuTable + " calc " +
                          " WHERE  p2.nzp_dom = calc.nzp_dom " +
                          " AND calc.nzp_serv =8 " +
                          " AND calc.dat_charge IS NULL and calc.tarif>0 " +
                          " AND calc.nzp_dom IN (SELECT cl.nzp_dom " +
                                                " FROM " + calcGkuTable + " cl " +
                                                " WHERE cl.nzp_serv=9 " +
                                                " and cl.tarif > 0 " +
                                                " AND cl.nzp_dom=calc.nzp_dom) " + GetWhereSupp() +
                          " GROUP BY 1,2,3,4,5,6,7 ";
                    ExecSQL(sql);



                    sql = " INSERT INTO t_otchet_otoplS(pref, nzp_kvar,is_gvc, catel,sum_m_counter,sum_m_norm,sum_gil) " +
                          " SELECT '" + pref + "' AS pref, " +
                                 " nzp_kvar, " +
                                 " 0 AS is_gvc, " +
                                 "  catel, " +
                                 " (CASE WHEN is_dpu<>0 THEN squ ELSE 0 END) AS sum_m_counter, " +
                                 " (CASE WHEN is_dpu=0 THEN squ ELSE 0 END) AS sum_m_norm, " +
                                 " (ROUND(calc.gil)) AS sum_gil " +
                          " FROM td p2, " +
                               calcGkuTable + " calc " +
                          " WHERE p2.nzp_dom = calc.nzp_dom " +
                          " AND calc.nzp_serv = 8" +
                          " AND calc.dat_charge IS NULL and calc.tarif>0  " +
                          " AND calc.nzp_dom NOT IN (SELECT cl.nzp_dom " +
                                                " FROM " + calcGkuTable + " cl " +
                                                " WHERE cl.nzp_serv=9 " +
                                                " and cl.tarif > 0 " +
                                                " AND cl.nzp_dom=calc.nzp_dom) " + GetWhereSupp() +
                          " GROUP BY 1,2,3,4,5,6,7 ";
                    ExecSQL(sql);

                    ExecSQL("drop table td");


                    sql = " INSERT INTO t_names (pref) " +
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

            sql = " SELECT " + 
                  " t1.pref, " +
                  " TRIM(name_agent) AS name_agent, " +
                  " TRIM(director_post) AS director_post, " +
                  " TRIM(chief_charge_post) AS chief_charge_post, " +
                  " TRIM(chief_finance_post) AS chief_finance_post, " +
                  " TRIM(director_name) AS director_name, " +
                  " TRIM(chief_charge_name) AS chief_charge_name, " +
                  " TRIM(chief_finance_name) AS chief_finance_name, " +
                  " TRIM('" + ReportParams.User.uname + "') AS executor_name,  " +
                  " catel,is_gvc,SUM(sum_m_counter) AS sum_m_counter,SUM(sum_m_norm) AS sum_m_norm,SUM(sum_gil) AS sum_gil " +
                  " FROM t_otchet_otoplS t1 INNER JOIN t_names tn on (t1.pref = tn.pref) " +
                  " GROUP BY 1,2,3,4,5,6,7,8,9,10,11 " +
                  " ORDER BY is_gvc, catel";

            DataTable dt = ExecSQLToTable(sql);
            dt.TableName = "Q_master";
            var ds = new DataSet();
            ds.Tables.Add(dt);
            return ds;
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
                ? " AND " + filedPref + " in ( select nzp_dom " +
                  " from " + ReportParams.Pref + DBManager.sDataAliasRest + "dom d," +
                  ReportParams.Pref + DBManager.sDataAliasRest + "s_ulica su " +
                  " where d.nzp_ul=su.nzp_ul and su.nzp_raj in (" + whereRajon + "))"
                  : String.Empty;

            return whereRajon;
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
                string sql = " UPDATE t_names " +
                             " SET " + nameColumn + " = '" + nameTable.Rows[0][0].ToString().Trim() + "' " +
                             " WHERE t_names.pref = '" + pref + "' ";
                ExecSQL(sql);
            }
            else
            {
                string sql = " UPDATE t_names " +
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
                sql += "' WHERE t_names.pref = '" + pref + "' ";
                ExecSQL(sql);
            }
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

        protected override void CreateTempTable()
        {
            string sql = " CREATE TEMP TABLE t_otchet_otoplS ( " +
                               " pref CHARACTER(100)," +
                               " nzp_kvar INTEGER, " +
                               " is_gvc INTEGER, " +
                               " catel CHARACTER(20), " +
                               " sum_m_counter " + DBManager.sDecimalType + "(14,2), " +
                               " sum_m_norm " + DBManager.sDecimalType + "(14,2), " +
                               " sum_gil INTEGER) " + DBManager.sUnlogTempTable;
            ExecSQL(sql);


            sql = " CREATE TEMP TABLE t_names ( " +
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
            ExecSQL(" DROP TABLE t_otchet_otoplS ");
            ExecSQL(" DROP TABLE t_names ");
        }
    }
}
