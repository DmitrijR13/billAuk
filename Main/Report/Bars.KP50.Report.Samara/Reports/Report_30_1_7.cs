using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Bars.KP50.Report.Samara
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Data;
    using Report;
    using Base;
    using Properties;
    using Utils;

    using FastReport;

    using STCLINE.KP50.DataBase;
    using STCLINE.KP50.Global;
    class Sprav_nach_heat : BaseSqlReport
    {
        public override string Name
        {
            get { return "30.1.7 Справка по начислению платы по виду услуги: Отопление"; }
        }

        public override string Description
        {
            get { return "Справка по начислению платы по виду услуги: Отопление"; }
        }

        public override IList<ReportGroup> ReportGroups
        {
            get
            {
                var result = new List<ReportGroup> {ReportGroup.Finans};
                return result;
            }
        }

        public override bool IsPreview
        {
            get { return false; }
        }

        protected override byte[] Template
        {
            get { return Resources.Sprav_nach_heat; }
        }

        public override IList<ReportKind> ReportKinds
        {
            get { return new List<ReportKind> { ReportKind.Base }; }
        }

        /// <summary>Месяц</summary>
        protected int Month { get; set; }

        /// <summary>Год</summary>
        protected int Year { get; set; }

        /// <summary>УК</summary>
        protected List<int> Areas { get; set; }

        /// <summary>Заголовок отчета</summary>
        protected string AreaHeader { get; set; }

        /// <summary>Поставщики</summary>
        protected List<long> Suppliers { get; set; }

        /// <summary>Банки данных</summary>
        protected List<int> Banks { get; set; }


        public override List<UserParam> GetUserParams()
        {
            //var curCalcMonthYear = DBManager.GetCurMonthYear();
            return new List<UserParam>
            {
                new MonthParameter {Value = DateTime.Today.Month },
                new YearParameter {Value = DateTime.Today.Year },
                new SupplierAndBankParameter(),
                new AreaParameter()
            };
        }

        protected override void PrepareReport(Report report)
        {
            string[] months =
            {"","январь","февраль",
                "март","апрель","май","июнь","июль","август","сентябрь",
                "октябрь","ноябрь","декабрь"};
            report.SetParameterValue("dat", DateTime.Now.ToLongDateString());
            report.SetParameterValue("time", DateTime.Now.ToLongTimeString());
            report.SetParameterValue("month", months[Month]);
            report.SetParameterValue("year", Year);
            report.SetParameterValue("area", AreaHeader);
            report.SetParameterValue("town", "г.о. Жигулевск");
        }


        protected override void PrepareParams()
        {
            Month = UserParamValues["Month"].GetValue<int>();
            Year = UserParamValues["Year"].GetValue<int>();
            Areas = UserParamValues["Areas"].GetValue<List<int>>();

            var values = JsonConvert.DeserializeObject<Dictionary<string, object>>(UserParamValues["SupplierAndBank"].GetValue<string>());
            Suppliers = values["Streets"] != null
                ? values["Streets"].To<JArray>().Select(x => x.Value<long>()).ToList()
                : null;
            Banks = values["Raions"] != null
                ? values["Raions"].To<JArray>().Select(x => x.Value<int>()).ToList()
                : null;
        }

        public override DataSet GetData()
        {
            MyDataReader reader;
            string whereArea = GetWhereArea();
            string whereSupp = GetWhereSupp();

            string sql = " select bd_kernel as pref " +
                         " from " + DBManager.GetFullBaseName(Connection) + DBManager.tableDelimiter + "s_point "  +
                         " where nzp_wp>1 " + GetwhereWp();

            ExecRead(out reader, sql);
            while (reader.Read())
            {
                string pref = reader["pref"].ToStr().Trim();
                string prefCalGku = pref + "_charge_" + (Year - 2000).ToString("00") + DBManager.tableDelimiter +
                                    "calc_gku_" + Month.ToString("00");
                string prefCharge = pref + "_charge_" + (Year - 2000).ToString("00") + DBManager.tableDelimiter +
                                    "charge_" + Month.ToString("00");
                if (TempTableInWebCashe(prefCalGku) && TempTableInWebCashe(prefCharge))
                {
                    sql =
                        " insert into t_kdu (nzp_geu, nzp_kvar, nzp_dom, nzp_ul, nzp_area, nzp_raj, num_ls, ulica, idom, nkor, ikvar, nkvar) " +
                        " select k.nzp_geu, k.nzp_kvar, d.nzp_dom, u.nzp_ul, k.nzp_area, u.nzp_raj, k.num_ls, " +
                        " u.ulica, d.idom, d.nkor, k.ikvar, k.nkvar " +
                        " from " + pref + DBManager.sDataAliasRest + "kvar k, " +
                        pref + DBManager.sDataAliasRest + "dom d, " +
                        pref + DBManager.sDataAliasRest + "s_ulica u " +
                        " where k.nzp_dom = d.nzp_dom and d.nzp_ul = u.nzp_ul ";
                    ExecSQL(sql);

                    sql = " INSERT INTO t_svod (nzp_kvar, nzp_geu, nzp_area, ulica, idom, nkor,  " +
                          " gil, nach, post_nach, vozv, nach_red, nach_black, nach_all, payment, c_calc, vozv_gkal, nach_red_gkal, nach_black_gkal, tarif) " +
                          " select t.nzp_kvar, t.nzp_geu, t.nzp_area, ulica, idom, case when nkor<>'-' then nkor end as nkor, " +
                          " SUM(round(gil)) AS gil, " +
                          " SUM(rsum_tarif) AS nach, " +
                          " SUM(rsum_tarif) AS post_nach, " +
                          " SUM(sum_nedop) AS vozv, " +
                          " SUM(case when reval<0 then reval else 0 end) AS nach_red, " +
                          " SUM(case when reval>0 then reval else 0 end) AS nach_black, " +
                          " SUM(sum_charge) AS nach_all, " +
                          " SUM(sum_charge) AS payment, " +
                          " ROUND(SUM(c_calc),4) AS c_calc, " +
                          " ROUND(CASE WHEN MAX(ch.tarif) <> 0 THEN ( " +
                                                                    " SUM( " +
                                                                         " CASE WHEN ch.tarif<>0 THEN " + DBManager.sNvlWord + "(sum_nedop,0) ELSE 0 END" +
                                                                       " ) / MAX(ch.tarif)" +
                                                                  " ) ELSE 0 END,4) AS vozv_gkal, " +
                          " ROUND(CASE WHEN MAX(ch.tarif) <> 0 THEN ( " +
                                                                    " SUM( " +
                                                                         " CASE WHEN (reval<0) AND (ch.tarif<>0) THEN reval ELSE 0 END" +
                                                                       " ) / MAX(ch.tarif) " +
                                                                  " ) ELSE 0 END,4) AS nach_red_gkal, " +
                          " ROUND(CASE WHEN MAX(ch.tarif) <> 0 THEN ( " +
                                                                    " SUM( " +
                                                                         " CASE WHEN (reval>0) AND (ch.tarif<>0) THEN reval ELSE 0 END" +
                                                                       " ) / MAX(ch.tarif) " +
                                                                  " ) ELSE 0 END,4) AS nach_black_gkal, " +
                          " MAX(ch.tarif) AS tarif " +
                          " FROM t_kdu t, " + prefCalGku + " cg, " + prefCharge + " ch " +
                          " WHERE t.nzp_kvar = cg.nzp_kvar " +
                            " AND t.nzp_kvar = ch.nzp_kvar " +
                            " AND cg.nzp_serv = ch.nzp_serv " +
                            " AND ch.nzp_serv = 8 " +
                            " AND ch.dat_charge IS NULL " + whereArea + whereSupp +
                          " GROUP BY 1,2,3,4,5,6 ";
                    ExecSQL(sql);


                    sql = " UPDATE t_svod " +
                          " SET sum_otopl_gkal  = " + DBManager.sNvlWord + "((SELECT SUM(ROUND((CASE WHEN tarif <> 0 THEN sum_rcl / tarif ELSE 0 END),4)) " +
                                      " FROM " + pref + "_charge_" + (Year - 2000).ToString("00") + DBManager.tableDelimiter + "perekidka" + " a " +
                                      " WHERE a.nzp_kvar = t_svod.nzp_kvar " +
                                        " AND nzp_serv = 8 " +
                                        " AND type_rcl = 110 " +
                                        " AND month_ = " + Month.ToString("00") +
                                        " AND abs(sum_rcl) > 0.001 " + whereSupp + "),0) ";
                    ExecSQL(sql);
                    

                    sql = " UPDATE t_svod " +
                          " SET sum_otopl  = " + DBManager.sNvlWord + "((SELECT SUM(sum_rcl) " +
                                    " FROM " + pref + "_charge_" + (Year - 2000).ToString("00") + DBManager.tableDelimiter + "perekidka" + " a " +
                                    " WHERE a.nzp_kvar = t_svod.nzp_kvar " +
                                        " AND nzp_serv = 8 " +
                                        " AND type_rcl = 110 " +
                                        " AND month_ = " + Month.ToString("00") +
                                        " AND abs(sum_rcl) > 0.001 " + whereSupp + "),0) ";
                    ExecSQL(sql);

                    
                }

            }
            reader.Close();
            #region Выборка на экран
            sql = " select nzp_geu, ulica, idom, nkor, " +
                  " sum(gil) as gil, " +
                  " sum(nach) as nach, " +
                  " sum(post_nach) as post_nach, " +
                  " sum(vozv) as vozv, " +
                  " sum(-1*nach_red) as nach_red, " +
                  " sum(nach_black) as nach_black, " +
                  " sum(nach_all) as nach_all, " +
                  " sum(payment) as payment, " +
                  " sum(c_calc) as c_calc, " +
                  " sum(vozv_gkal) as vozv_gkal, " +
                  " sum(-1*nach_red_gkal) as nach_red_gkal, " +
                  " sum(nach_black_gkal) as nach_black_gkal, " +
                  " SUM(sum_otopl) AS sum_otopl, " +
                  " SUM(sum_otopl_gkal) AS sum_otopl_gkal " +
                  " from t_svod " +
                  " group by 1,2,3,4 " +
                  " order by 1,2,3,4 ";
            #endregion
            DataTable dt = ExecSQLToTable(sql);
            dt.TableName = "Q_master";
            DataSet ds = new DataSet();
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
            if (Areas != null)
            {
                whereArea = Areas.Aggregate(whereArea, (current, nzpArea) => current + (nzpArea + ","));
            }
            else
            {
                whereArea = ReportParams.GetRolesCondition(Constants.role_sql_area);
            }
            whereArea = whereArea.TrimEnd(',');
            whereArea = !String.IsNullOrEmpty(whereArea) ? " AND t.nzp_area in (" + whereArea + ")" : String.Empty;
            if (!String.IsNullOrEmpty(whereArea))
            {
                string sql = " SELECT area from " + ReportParams.Pref + DBManager.sDataAliasRest + "s_area t  WHERE t.nzp_area > 0 " + whereArea;
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
            if (Suppliers != null)
            {
                whereSupp = Suppliers.Aggregate(whereSupp, (current, nzpSupp) => current + (nzpSupp + ","));
            }
            else
            {
                whereSupp = ReportParams.GetRolesCondition(Constants.role_sql_supp);
            }
            whereSupp = whereSupp.TrimEnd(',');
            whereSupp = !String.IsNullOrEmpty(whereSupp) ? " AND ch.nzp_supp in (" + whereSupp + ")" : String.Empty;
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
            string sql = " create temp table t_svod( " +
                        " nzp_kvar integer, " +
                        " nzp_geu integer," +
                        " nzp_area integer," +
                        " ulica char(40), " +
                        " idom integer, " +
                        " nkor char(10), " +
                        " gil integer, " +
                        " tarif " + DBManager.sDecimalType + "(14,2)," +
                        " sum_otopl " + DBManager.sDecimalType + "(14,2)," +
                        " sum_otopl_gkal " + DBManager.sDecimalType + "(14,4)," +
                        " nach " + DBManager.sDecimalType + "(14,2)," +
                        " post_nach " + DBManager.sDecimalType + "(14,2)," +
                        " vozv " + DBManager.sDecimalType + "(14,2)," +
                        " nach_red " + DBManager.sDecimalType + "(14,2)," +
                        " nach_black " + DBManager.sDecimalType + "(14,2)," +
                        " nach_all " + DBManager.sDecimalType + "(14,2)," +
                        " payment " + DBManager.sDecimalType + "(14,2)," +
                        " c_calc " + DBManager.sDecimalType + "(14,4)," +
                        " vozv_gkal " + DBManager.sDecimalType + "(14,4)," +
                        " nach_red_gkal " + DBManager.sDecimalType + "(14,4)," +
                        " nach_black_gkal " + DBManager.sDecimalType + "(14,4))";
            ExecSQL(sql);
            sql = " create temp table t_kdu( " +
                        " nzp_geu integer," +
                        " nzp_kvar integer," +
                        " nzp_dom integer," +
                        " nzp_ul integer," +
                        " nzp_area integer," +
                        " nzp_raj integer," +
                        " num_ls integer," +
                        " ulica char(40), " +
                        " idom integer, " +
                        " nkor char(10), " +
                        " ikvar integer, " +
                        " nkvar char(10))";
            ExecSQL(sql);
        }

        protected override void DropTempTable()
        {
            ExecSQL(" drop table t_svod; drop table t_kdu ");
        }

    }
}
