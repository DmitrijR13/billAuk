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
    class Sprav_nach_hot_water : BaseSqlReport
    {
        public override string Name
        {
            get { return "30.1.8 Справка по начислению платы по виду услуги: Горячее водоснабжение"; }
        }

        public override string Description
        {
            get { return "Справка по начислению платы по виду услуги: Горячее водоснабжение"; }
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
            get { return Resources.Sprav_nach_hot_water; }
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
                         " FROM " + ReportParams.Pref + DBManager.sKernelAliasRest + "s_point " +
                         " where nzp_wp>1 " + GetwhereWp();

            ExecRead(out reader, sql);
            while (reader.Read())
            {
                string pref = reader["pref"].ToStr().Trim();
                sql = " insert into t_kdu (nzp_geu,  nzp_kvar, nzp_dom, nzp_ul, nzp_area, nzp_raj, num_ls, ulica, idom, nkor, ikvar, nkvar) " +
                      " select k.nzp_geu, k.nzp_kvar, d.nzp_dom, u.nzp_ul, k.nzp_area, u.nzp_raj, k.num_ls, " +
                      " u.ulica, d.idom, d.nkor, k.ikvar, k.nkvar " +
                      " from " + pref + DBManager.sDataAliasRest + "kvar k, " +
                        pref + DBManager.sDataAliasRest + "dom d, " +
                        pref + DBManager.sDataAliasRest + "s_ulica u " +
                      " where k.nzp_dom = d.nzp_dom and d.nzp_ul = u.nzp_ul " + whereArea;
                ExecSQL(sql);

                sql = " insert into t_charge (nzp_kvar, " +
                      " gil_ipu, gil_no_ipu, nach, post_nach, vozv, nach_red, nach_black, nach_all, payment, " +
                      " c_calc_ipu, c_calc_no_ipu, m3_ipu, m3_no_ipu, vozv_gkal, nach_red_gkal, nach_black_gkal, vozv_m3, nach_red_m3, nach_black_m3) " +
                      " select distinct ch.nzp_kvar, " +
                      " round(case when cg.is_device=1 then gil else 0 end) as gil_ipu, " +
                      " round(case when cg.is_device<>1 then gil else 0 end) as gil_no_ipu, " +
                      " rsum_tarif as nach, " +
                      " rsum_tarif as post_nach, " +
                      " sum_nedop as vozv, " +
                      " case when reval<0 then reval else 0 end as nach_red, " +
                      " case when reval>0 then reval else 0 end as nach_black, " +
                      " sum_charge as nach_all, " +
                      " sum_charge as payment, " +
                      " round(case when (ch.nzp_serv in(9,513)) and (ch.is_device=1) then c_calc else 0 end,4) as c_calc_ipu, " +
                      " round(case when (ch.nzp_serv in(9,513)) and (ch.is_device<>1) then c_calc else 0 end,4) as c_calc_no_ipu, " +
                      " round(case when (ch.nzp_serv in(14,514)) and (ch.is_device=1) then c_calc else 0 end,4) as m3_ipu, " +
                      " round(case when (ch.nzp_serv in(14,514)) and (ch.is_device<>1) then c_calc else 0 end,4) as m3_no_ipu, " +
                      " round(case when (ch.nzp_serv in(9,513)) and (ch.tarif<>0) then " + DBManager.sNvlWord + "(sum_nedop,0) else 0 end/ch.tarif,4) as vozv_gkal, " +
                      " round(case when (ch.nzp_serv in(9,513)) and (reval<0) and (ch.tarif<>0) then reval else 0 end/ch.tarif,4) as nach_red_gkal, " +
                      " round(case when (ch.nzp_serv in(9,513)) and (reval>0) and (ch.tarif<>0) then reval else 0 end/ch.tarif,4) as nach_black_gkal, " +
                      " round(case when (ch.nzp_serv in(14,514)) and (ch.tarif<>0) then " + DBManager.sNvlWord + "(sum_nedop,0) else 0 end/ch.tarif,4) as vozv_m3, " +
                      " round(case when (ch.nzp_serv in(14,514)) and (reval<0) and (ch.tarif<>0) then reval else 0 end/ch.tarif,4) as nach_red_m3, " +
                      " round(case when (ch.nzp_serv in(14,514)) and (reval>0) and (ch.tarif<>0) then reval else 0 end/ch.tarif,4) as nach_black_m3 " +
                      " from " +
                        pref + "_charge_" + (Year - 2000) + DBManager.tableDelimiter + "calc_gku_" + Month.ToString("00") + " cg, " +
                        pref + "_charge_" + (Year - 2000) + DBManager.tableDelimiter + "charge_" + Month.ToString("00") + " ch " +
                      " where ch.nzp_kvar = cg.nzp_kvar and ch.nzp_serv = cg.nzp_serv and ch.nzp_supp = cg.nzp_supp and ch.nzp_frm = cg.nzp_frm " + whereSupp +
                      " and ch.nzp_serv in (9,14,513,514) and ch.dat_charge is null " +
                      " and ch.sum_nedop > 0.001 ";
                ExecSQL(sql);

                sql = " insert into t_svod (nzp_geu, nzp_area, ulica, idom, nkor, " +
                      " gil_ipu, gil_no_ipu, nach, post_nach, vozv, nach_red, nach_black, nach_all, payment, " +
                      " c_calc_ipu, c_calc_no_ipu, m3_ipu, m3_no_ipu, vozv_gkal, nach_red_gkal, nach_black_gkal, vozv_m3, nach_red_m3, nach_black_m3) " +
                      " select t.nzp_geu, t.nzp_area, ulica, idom, case when nkor<>'-' then nkor end as nkor, " +
                      " max(gil_ipu) as gil_ipu, " +
                      " max(gil_no_ipu) as gil_no_ipu, " +
                      " sum(nach) as nach, " +
                      " sum(post_nach) as post_nach, " +
                      " sum(vozv) as vozv, " +
                      " sum(nach_red) as nach_red, " +
                      " sum(nach_black) as nach_black, " +
                      " sum(nach_all) as nach_all, " +
                      " sum(payment) as payment, " +
                      " sum(c_calc_ipu) as c_calc_ipu, " +
                      " sum(c_calc_no_ipu) as c_calc_no_ipu, " +
                      " sum(m3_ipu) as m3_ipu, " +
                      " sum(m3_no_ipu) as m3_no_ipu, " +
                      " sum(vozv_gkal) as vozv_gkal, " +
                      " sum(nach_red_gkal) as nach_red_gkal, " +
                      " sum(nach_black_gkal) as nach_black_gkal, " +
                      " sum(vozv_m3) as vozv_m3, " +
                      " sum(nach_red_m3) as nach_red_m3, " +
                      " sum(nach_black_m3) as nach_black_m3 " +
                      " from t_kdu t, t_charge ch " +
                      " where t.nzp_kvar = ch.nzp_kvar " +
                      " group by 1,2,3,4,5 ";
                ExecSQL(sql);
                ExecSQL(" delete from t_kdu ");
                ExecSQL(" delete from t_charge ");
            }
            reader.Close();
            #region Выборка на экран
            sql = " select nzp_geu, ulica, idom, nkor, " +
                  " sum(gil_ipu) as gil_ipu, " +
                  " sum(gil_no_ipu) as gil_no_ipu, " +
                  " sum(nach) as nach, " +
                  " sum(post_nach) as post_nach, " +
                  " sum(vozv) as vozv, " +
                  " sum(-1*nach_red) as nach_red, " +
                  " sum(nach_black) as nach_black, " +
                  " sum(nach_all) as nach_all, " +
                  " sum(payment) as payment, " +
                  " sum(c_calc_ipu) as c_calc_ipu, " +
                  " sum(c_calc_no_ipu) as c_calc_no_ipu, " +
                  " sum(m3_ipu) as m3_ipu, " +
                  " sum(m3_no_ipu) as m3_no_ipu, " +
                  " sum(vozv_gkal) as vozv_gkal, " +
                  " sum(-1*nach_red_gkal) as nach_red_gkal, " +
                  " sum(nach_black_gkal) as nach_black_gkal, " +
                  " sum(vozv_m3) as vozv_m3, " +
                  " sum(-1*nach_red_m3) as nach_red_m3, " +
                  " sum(nach_black_m3) as nach_black_m3 " +
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
                        " nzp_geu integer, " +
                        " nzp_area integer, " +
                        " ulica char(40), " +
                        " idom integer, " +
                        " nkor char(10), " +
                        " gil_ipu integer, " +
                        " gil_no_ipu integer, " +
                        " nach " + DBManager.sDecimalType + "(14,2)," +
                        " post_nach " + DBManager.sDecimalType + "(14,2)," +
                        " vozv " + DBManager.sDecimalType + "(14,2)," +
                        " nach_red " + DBManager.sDecimalType + "(14,2)," +
                        " nach_black " + DBManager.sDecimalType + "(14,2)," +
                        " nach_all " + DBManager.sDecimalType + "(14,2)," +
                        " payment " + DBManager.sDecimalType + "(14,2)," +
                        " c_calc_ipu " + DBManager.sDecimalType + "(14,4)," +
                        " c_calc_no_ipu " + DBManager.sDecimalType + "(14,4)," +
                        " m3_ipu " + DBManager.sDecimalType + "(14,4)," +
                        " m3_no_ipu " + DBManager.sDecimalType + "(14,4)," +
                        " vozv_m3 " + DBManager.sDecimalType + "(14,4)," +
                        " nach_red_m3 " + DBManager.sDecimalType + "(14,4)," +
                        " nach_black_m3 " + DBManager.sDecimalType + "(14,4)," +
                        " vozv_gkal " + DBManager.sDecimalType + "(14,4)," +
                        " nach_red_gkal " + DBManager.sDecimalType + "(14,4)," +
                        " nach_black_gkal " + DBManager.sDecimalType + "(14,4))";
            ExecSQL(sql);
            sql = " create temp table t_charge( " +
                        " nzp_kvar integer, " +
                        " gil_ipu integer, " +
                        " gil_no_ipu integer, " +
                        " nach " + DBManager.sDecimalType + "(14,2)," +
                        " post_nach " + DBManager.sDecimalType + "(14,2)," +
                        " vozv " + DBManager.sDecimalType + "(14,2)," +
                        " nach_red " + DBManager.sDecimalType + "(14,2)," +
                        " nach_black " + DBManager.sDecimalType + "(14,2)," +
                        " nach_all " + DBManager.sDecimalType + "(14,2)," +
                        " payment " + DBManager.sDecimalType + "(14,2)," +
                        " c_calc_ipu " + DBManager.sDecimalType + "(14,4)," +
                        " c_calc_no_ipu " + DBManager.sDecimalType + "(14,4)," +
                        " m3_ipu " + DBManager.sDecimalType + "(14,4)," +
                        " m3_no_ipu " + DBManager.sDecimalType + "(14,4)," +
                        " vozv_m3 " + DBManager.sDecimalType + "(14,4)," +
                        " nach_red_m3 " + DBManager.sDecimalType + "(14,4)," +
                        " nach_black_m3 " + DBManager.sDecimalType + "(14,4)," +
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
            ExecSQL(" drop table t_svod ");
            ExecSQL(" drop table t_kdu ");
            ExecSQL(" drop table t_charge ");
        }

    }
}
