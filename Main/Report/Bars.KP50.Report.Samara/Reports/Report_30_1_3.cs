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
    class Report3001003 : BaseSqlReport
    {
        public override string Name
        {
            get { return "30.1.3. Отчет по начислению за горячее водоснабжение"; }
        }

        public override string Description
        {
            get { return "Отчет по начислению за горячее водоснабжение"; }
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
            get { return Resources.Report_30_1_3; }
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

        private string _pointHeader;


        //private string _agent;
        //private string _principal;
        //private string _chargesHead;
        //private string _financeHead;
        //private string _principalName;
        //private string _chargesName;
        //private string _financeName;

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

            report.SetParameterValue("period_month", months[Month] + " " + Year +" г.");

            report.SetParameterValue("DATE", DateTime.Now.ToShortDateString());
            report.SetParameterValue("TIME", DateTime.Now.ToLongTimeString());
            report.SetParameterValue("invisible_footer", isShow);
            report.SetParameterValue("town", _pointHeader);

            
        }



        public override DataSet GetData()
        {
            IDataReader reader;

            #region выборка в temp таблицу

            string sql = " SELECT bd_kernel as pref " +
                         " FROM " + ReportParams.Pref + DBManager.sKernelAliasRest + "s_point " +
                         " WHERE nzp_wp>1 " + GetwhereWp();

            var datS = new DateTime(Year, Month, 1);
            var datPo = datS.AddMonths(1).AddDays(-1);
            
            ExecRead(out reader, sql);
            while (reader.Read())
            {
                string pref = reader["pref"].ToStr().Trim();

                string calcGkuTable = pref + "_charge_" + (Year - 2000).ToString("00") + DBManager.tableDelimiter + "calc_gku_" + Month.ToString("00");
                string chargeTable = pref + "_charge_" + (Year - 2000).ToString("00") + DBManager.tableDelimiter + "charge_" + Month.ToString("00");
                string prefData = pref + DBManager.sDataAliasRest;

                if (TempTableInWebCashe(calcGkuTable) && TempTableInWebCashe(chargeTable) && TempColumnInWebCashe(calcGkuTable,"valm"))
                {

                    sql = " create temp table td (nzp_dom integer, catel char(20)) " + DBManager.sUnlogTempTable;
                    ExecSQL(sql);

                    sql = "insert into td(nzp_dom, catel)" +
                          " select nzp, val_prm from " +
                          prefData + "prm_2 p2 " +
                          " WHERE p2.nzp_prm = 1179 " +
                          " AND p2.is_actual<>100 " +
                          " and p2.dat_s<='" + datPo.ToShortDateString() + "'" +
                          " and p2.dat_po>='" + datS.ToShortDateString() + "'"+
                          GetRajon("nzp");
                    ExecSQL(sql);

                    ExecSQL("create index ix_calctd_01 on td(nzp_dom)");
                    ExecSQL(DBManager.sUpdStat + " td");


                    //Определяем список квартир со счетчиком
                    sql = "create temp table t_counters(nzp_kvar integer, is_device integer)" +
                          DBManager.sUnlogTempTable;
                    ExecSQL(sql);


                    sql = " insert into t_counters(nzp_kvar, is_device) " +
                          " SELECT k.nzp_kvar, " + DBManager.sNvlWord + "(val_prm" + DBManager.sConvToInt + ",0) " +
                          " from " + pref + DBManager.sDataAliasRest + "kvar k left outer join " +
                          pref + DBManager.sDataAliasRest + "prm_1 p on " +
                          " nzp_prm=103 and k.nzp_kvar=p.nzp and p.is_actual=1" +
                          " and p.dat_s<='" + datPo.ToShortDateString() + "'" +
                          " and p.dat_po>='" + datS.ToShortDateString() + "'" + 
                          " group by 1,2";
                    ExecSQL(sql);

                    
                    ExecSQL("create index ix_tmptc_01 on t_counters(nzp_kvar, is_device)");
                    ExecSQL(DBManager.sUpdStat +" t_counters");


                    sql = " INSERT INTO t_otchet_zavod(pref, nzp_dom,nzp_serv, name_catel,sum_m_counter, sum_gil_counter) " +
                          " SELECT '" + pref + "' as pref, calc.nzp_dom, " +
                          "         calc.nzp_serv, " +
                          "         catel as name_catel, " +
                          "         sum(case when valm+dlt_reval>=0 and  dop87<0 and dop87< -valm-dlt_reval then 0 else valm+dlt_reval+dop87 end) AS sum_m_counter, " +
                          "         SUM(CASE WHEN  calc.nzp_serv=9 THEN round(calc.gil) ELSE 0 END) AS sum_gil_counter " +
                          " FROM " + calcGkuTable + " calc, td p, t_counters t" +
                          " WHERE calc.nzp_serv IN (9,14,513,514) " +
                          "        AND calc.nzp_kvar=t.nzp_kvar  " +
                          "        AND calc.tarif>0  " +
                          "        AND t.is_device =1  " +
                          "        AND p.nzp_dom = calc.nzp_dom " + GetWhereSupp() +
                          " GROUP BY 1,2,3,4 ";
                    ExecSQL(sql);

                    sql = " INSERT INTO t_otchet_zavod(pref, nzp_dom,nzp_serv, name_catel,sum_m_norm, sum_gil_norm) " +
                          " SELECT '" + pref + "' as pref, calc.nzp_dom, " +
                          "         calc.nzp_serv, " +
                          "         catel as name_catel, " +
                          "         sum(case when valm+dlt_reval>=0 and  dop87<0 and dop87< -valm-dlt_reval then 0 else valm+dlt_reval+dop87 end) AS sum_m_norm, " +
                          "         SUM(CASE WHEN calc.nzp_serv=9 THEN round(calc.gil) ELSE 0 END) AS sum_gil_norm " +
                          " FROM " + calcGkuTable + " calc, td p, t_counters t" +
                          " WHERE calc.nzp_serv IN (9,14,513,514) " +
                          "     AND calc.nzp_kvar=t.nzp_kvar    " +
                          "       AND calc.tarif>0  " +
                          "       AND t.is_device = 0  " +
                          "       AND p.nzp_dom = calc.nzp_dom " + GetWhereSupp() +
                          " GROUP BY 1,2,3,4 ";
                    ExecSQL(sql);

                    sql = " INSERT INTO t_otchet_zavod(pref, nzp_dom,nzp_serv, name_catel,sum_r_counter, sum_r_norm) " +
                          " SELECT '" + pref + "' as pref, k.nzp_dom, " +
                          "         ch.nzp_serv, " +
                          "         catel as name_catel, " +
                          "         sum(case when t.is_device = 1 then rsum_tarif else 0 end) AS sum_r_counter, " +
                          "         sum(case when t.is_device = 0 then rsum_tarif else 0 end) AS sum_r_norm " +
                          " FROM " + chargeTable + " ch, td p, " +
                          "     t_counters t," + pref + DBManager.sDataAliasRest +"kvar k" +
                          " WHERE ch.nzp_serv IN (9,14,513,514)  and dat_charge is null and abs(rsum_tarif)>0.001" +
                          "       AND k.nzp_kvar = t.nzp_kvar   " +
                          "       AND ch.nzp_kvar=k.nzp_kvar " +
                          "       AND p.nzp_dom = k.nzp_dom " + GetWhereSupp() +
                          " GROUP BY 1,2,3,4 ";
                    ExecSQL(sql);


                    ExecSQL("drop table t_counters");
                    ExecSQL("drop table td");


                    ExecSQL(" INSERT INTO t_names (pref)  VALUES ('" + pref + "') ");
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

            //                                   " (CASE s.nzp_serv WHEN 14 THEN 'Компонент на хол.воду'  " +
            //                                   " WHEN 9 THEN 'Компонент на т/энергию'  END) AS service, " +

            sql = " SELECT (CASE WHEN nzp_serv in (14,514) THEN 'Компонент на хол.воду'  " +
                  "             WHEN nzp_serv in (9,513) THEN 'Компонент на т/энергию'  END) AS service," +
                  " (case when nzp_serv = 513 then 9" +
                  "       when nzp_serv=514 then 14 else nzp_serv end) nzp_serv, a.nzp_dom, ulica, nkor, ndom, rajon,town, idom, name_catel, " +
                  " SUM(sum_m_counter) AS sum_m_counter, " +
                  " SUM(sum_r_counter) AS sum_r_counter, " +
                  " SUM(sum_gil_counter) AS sum_gil_counter, " +
                  " SUM(sum_m_norm) AS sum_m_norm, " +
                  " SUM(sum_r_norm) AS sum_r_norm, " +
                  " SUM(sum_gil_norm) AS sum_gil_norm, " +
                  " a.pref, " +
                  " TRIM(name_agent) AS name_agent, " +
                  " TRIM(director_post) AS director_post, " +
                  " TRIM(chief_charge_post) AS chief_charge_post, " +
                  " TRIM(chief_finance_post) AS chief_finance_post, " +
                  " TRIM(director_name) AS director_name, " +
                  " TRIM(chief_charge_name) AS chief_charge_name, " +
                  " TRIM(chief_finance_name) AS chief_finance_name, " +
                  " TRIM('" + ReportParams.User.uname + "') AS executor_name  " +
                  " FROM t_otchet_zavod a INNER JOIN t_names n ON a.pref = n.pref, " +
                  ReportParams.Pref+DBManager.sDataAliasRest+"dom d, "+
                  ReportParams.Pref + DBManager.sDataAliasRest + "s_ulica s, " +
                  ReportParams.Pref + DBManager.sDataAliasRest + "s_rajon sr, " +
                  ReportParams.Pref + DBManager.sDataAliasRest + "s_town st " +
                  " where a.nzp_dom=d.nzp_dom and d.nzp_ul=s.nzp_ul and s.nzp_raj=sr.nzp_raj " +
                  " and sr.nzp_town = st.nzp_town " +
                  " GROUP BY 1,2,3,4,5,6,7,8,9,10, 17,18,19,20,21,22,23,24,25 " +
                  " ORDER BY town,rajon,name_catel,ulica,idom,nzp_dom,nkor, 1  ";

            DataTable dt = ExecSQLToTable(sql);
            dt.TableName = "Q_master";
            var ds = new DataSet();
            ds.Tables.Add(dt);
            return ds;
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
        /// Получение наименования районов
        /// </summary>
        /// <param name="where"></param>
        /// <returns></returns>
        private string GetPointHeader(string where)
        {
            string result = String.Empty;
            if (String.IsNullOrEmpty(where)) return result;
            string sql = " select point " +
                         " from " + ReportParams.Pref + DBManager.sKernelAliasRest + "s_point t" +
                         " where nzp_wp>1 " + where;
            using (DataTable dr = ExecSQLToTable(sql))
            {
                result = dr.Rows.Cast<DataRow>().Aggregate(result, (current, row) => current + (row["point"].ToString().Trim() + " ,"));
                result = result.TrimEnd(',');
            }
            return result;
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

            _pointHeader = GetPointHeader(whereWp);
            return whereWp;
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


        protected override void CreateTempTable()
        {
            string sql = " CREATE TEMP TABLE t_otchet_zavod (" +
                               " pref CHARACTER(10), " +
                               " nzp_dom INTEGER, " +
                               " nzp_serv INTEGER, " +
                               " service CHARACTER(100)," +
                               " sum_m_counter " + DBManager.sDecimalType + "(14,6) default 0, " +
                               " sum_r_counter " + DBManager.sDecimalType + "(14,6) default 0, " +
                               " sum_gil_counter " + DBManager.sDecimalType + "(14,6) default 0, " +
                               " sum_m_norm " + DBManager.sDecimalType + "(14,6) default 0, " +
                               " sum_r_norm " + DBManager.sDecimalType + "(14,6) default 0, " +
                               " sum_gil_norm  INTEGER default 0, " +
                               " name_catel CHARACTER(20))" + DBManager.sUnlogTempTable;
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
            ExecSQL(" DROP TABLE t_otchet_zavod ");
            ExecSQL(" DROP TABLE t_names ");
        }
    }
}
