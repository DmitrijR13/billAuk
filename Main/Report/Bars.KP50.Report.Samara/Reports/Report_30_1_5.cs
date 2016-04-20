using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
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
    class Report3015 : BaseSqlReport
    {
        public override string Name
        {
            get { return "30.1.5 Карта аналитического учета"; }
        }

        public override string Description
        {
            get { return "Карта аналитического учета"; }
        }

        public override IList<ReportGroup> ReportGroups
        {
            get
            {
                var result = new List<ReportGroup> { ReportGroup.Reports };
                return result;
            }
        }

        public override bool IsPreview
        {
            get { return false; }
        }

        protected override byte[] Template
        {
            get { return Resources.Report_30_1_5; }
        }

        public override IList<ReportKind> ReportKinds
        {
            get { return new List<ReportKind> { ReportKind.Base, ReportKind.ListLC }; }
        }

        /// <summary>Расчетный месяц начала периода</summary>
        private int MonthS { get; set; }

        /// <summary>Расчетный год начала периода</summary>
        private int YearS { get; set; }


        /// <summary>Расчетный месяц окончания периода</summary>
        private int MonthPo { get; set; }

        /// <summary>Расчетный год окончания периода</summary>
        private int YearPo { get; set; }
        
        /// <summary>Управляющие компании</summary>
        private List<int> Areas { get; set; }

        /// <summary>Районы</summary>
        private List<int> Rajons { get; set; }

        /// <summary>Заголовок отчета</summary>
        private string SupplierHeader { get; set; }

        /// <summary>Заголовок отчета</summary>
        private string AreaHeader { get; set; }

        /// <summary>Поставщики</summary>
        private List<long> Suppliers { get; set; }

        /// <summary>Банки данных</summary>
        private List<int> Banks { get; set; }

        /// <summary>Исключать услуги капремонта</summary>
        private bool ExcludeKapr { get; set; }

        /// <summary>Фильтр</summary>
        private int Grouper { get; set; }

        /// <summary>  Получают отчет по закрытому или открытом месяцу  </summary>
        private bool IsCloseMonth { get; set; }

        public override List<UserParam> GetUserParams()
        {
            //var curCalcMonthYear = DBManager.GetCurMonthYear();
            return new List<UserParam>
            {
                new MonthParameter {Name = "Месяц с", Value = DateTime.Today.Month },
                new YearParameter {Name = "Год с", Value = DateTime.Today.Year },
                new MonthParameter {Name = "Месяц по", Code = "Month1", Value = DateTime.Today.Month },
                new YearParameter {Name = "Год по", Code = "Year1", Value = DateTime.Today.Year },

                new SupplierAndBankParameter(),
                new AreaParameter(),
                new RaionsParameter(),
                new ComboBoxParameter(false)
                {
                    Name = "Капремонт", 
                    Code = "Filter",
                    Value = 1,
                    Require = true,
                    StoreData = new List<object> {
                        new { Id = 1, Name = "Включать"},
                        new { Id = 2, Name = "Исключать"}
                    }
                },
                new ComboBoxParameter(false)
                {
                    Name = "Группировать по", 
                    Code = "Grouper",
                    Value = 1,
                    Require = true,
                    StoreData = new List<object> {
                        new { Id = 1, Name = "ЖЭУ"},
                        new { Id = 2, Name = "Жилое/Нежилое"}
                    }
                }
            };
        }

        protected override void PrepareParams()
        {
            MonthS = UserParamValues["Month"].GetValue<int>();
            YearS = UserParamValues["Year"].Value.To<int>();

            MonthPo = UserParamValues["Month1"].GetValue<int>();
            YearPo = UserParamValues["Year1"].Value.To<int>();

            Areas = UserParamValues["Areas"].Value.To<List<int>>();
            ExcludeKapr = UserParamValues["Filter"].GetValue<int>() == 2;
            Grouper = UserParamValues["Grouper"].GetValue<int>();

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
            var months = new[] {"","Январь","Февраль",
                 "Март","Апрель","Май","Июнь","Июль","Август","Сентябрь",
                 "Октябрь","Ноябрь","Декабрь"};
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
            report.SetParameterValue("supplier", SupplierHeader);
            report.SetParameterValue("area", AreaHeader);
            report.SetParameterValue("town", "");

            if (MonthS == MonthPo && YearS == YearPo)
                report.SetParameterValue("period_month", months[MonthS] + " " + YearS + " г.");
            else
                report.SetParameterValue("period_month", months[MonthS] + " " + YearS + " г. - " +
                    months[MonthPo] + " " + YearPo + " г.");
            report.SetParameterValue("DATE", DateTime.Now.ToShortDateString());
            report.SetParameterValue("TIME", DateTime.Now.ToLongTimeString());
            if (ReportParams.CurrentReportKind == ReportKind.ListLC)
            {
                report.SetParameterValue("spisok", "По списку лицевых счетов " + (IsCloseMonth ? "" : ", получен по открытому месяцу "));
            }
            else
            {
                report.SetParameterValue("spisok", IsCloseMonth ? "" : " получен по открытому месяцу ");
            }

        }

        public override DataSet GetData()
        {
            MyDataReader reader;

            string whereSupp = GetWhereSupp();
            string whereArea = GetWhereArea();
            bool listLc = GetSelectedKvars();
            IsCloseMonth = true;

            #region выборка в temp таблицу

            string sql = " SELECT bd_kernel as pref " +
                         " FROM " + ReportParams.Pref + DBManager.sKernelAliasRest + "s_point " +
                         " WHERE nzp_wp>1 " + GetwhereWp();

            ExecRead(out reader, sql);
            while (reader.Read())
            {
                string pref = reader["pref"].ToStr().Trim();
                IsCloseMonth = IsCloseMonth && CheckSaldoDate(pref);

                for (int i = YearS * 12 + MonthS; i < YearPo * 12 + MonthPo + 1; i++)
                {
                    int curMonth = i % 12;
                    int curYear = i / 12;
                    if (curMonth == 0)
                    {
                        curMonth = 12;
                        curYear--;
                    }
                    
                    CalcOneMonth(pref, curYear, curMonth, listLc, whereArea, whereSupp);
                 
                }

                sql = " INSERT INTO t_report_30_1_5 (pref)  VALUES ('" + pref + "') ";
                ExecSQL(sql);

                GetNameResponsible(pref, 80); // Имя агента
                GetNameResponsible(pref, 1291); // Наименование должности директора РЦ
                GetNameResponsible(pref, 1292); // Наименоваине должности начальника отдела начислений 
                GetNameResponsible(pref, 1293); // Наименование должности начальника отдела финансов
                GetNameResponsible(pref, 1294); // ФИО директора РЦ      
                GetNameResponsible(pref, 1295); // ФИО начальника отдела начислений
                GetNameResponsible(pref, 1296); // ФИО начальника отдела финансов

            }
            reader.Close();
            #endregion

            SetTown();



            sql = " INSERT INTO  tt_report_30_1_5(nzp_serv, pref, sort, town, " +
                                    "typek, nzp_geu, sum_insaldo, rsum_tarif, izm_tarif, vozv, " +
                                        " reval_k, reval_d, sum_ito, sum_otopl, sum_charge, sum_money, sum_outsaldo, sum_odn, sum_insaldo_odn ) " +
                  " SELECT ABS(nzp_serv) AS nzp_serv, " +
                        " pref, " +
                        " sort, " +
                        " TRIM(town) AS town, " +
                        " (CASE WHEN typek = 3 THEN 'Нежилое' ELSE 'Жилое' END) AS typek, " +
                        " (CASE WHEN nzp_geu IS NULL THEN -1 ELSE nzp_geu END) AS nzp_geu, " +
                        " SUM(sum_insaldo) AS sum_insaldo, " +
                        " SUM(rsum_tarif) AS rsum_tarif, " +
                        " SUM(0) AS izm_tarif, " +
                        " SUM(vozv) AS vozv, " +
                        " SUM(-1*" + DBManager.sNvlWord + "(reval_k,0)) AS reval_k, " +
                        " SUM(reval_d) AS reval_d, " +
                        " SUM(sum_ito) AS sum_ito, " +
                        " SUM(sum_otopl) AS sum_otopl, " +
                        " SUM(sum_charge) AS sum_charge, " +
                        " SUM(sum_money) AS sum_money, " +
                        " SUM(sum_outsaldo) AS sum_outsaldo, " +
                        " SUM(sum_odn) AS sum_odn, " +
                        " SUM(sum_insaldo_odn) AS sum_insaldo_odn " +
                  " FROM t_kart_anal_uch " +
                  " GROUP BY 1,2,3,4,5,6 ";     
            ExecSQL(sql);

            #region заполнение временной таблицы

            sql = " DELETE FROM tt_report_30_1_5 " +
                  " WHERE nzp_serv NOT IN (SELECT nzp_serv FROM " + ReportParams.Pref + DBManager.sKernelAliasRest + " services s) ";

            ExecSQL(sql);

            sql = " DELETE FROM tt_report_30_1_5 " +
                  " WHERE pref NOT IN (SELECT DISTINCT pref FROM t_report_30_1_5) ";

            ExecSQL(sql);

            sql = " UPDATE tt_report_30_1_5 " +
                  " SET service = " + DBManager.sNvlWord + "( (SELECT (CASE WHEN nzp_serv=9 THEN 'Горячая вода' ELSE TRIM(service) END)" +
                                                             " FROM " + ReportParams.Pref + DBManager.sKernelAliasRest + " services s" +
                                                             " WHERE s.nzp_serv = tt_report_30_1_5.nzp_serv ),'' )";
            ExecSQL(sql);

                sql = " UPDATE tt_report_30_1_5 " +
                      " SET nzp_geu = -1 " +
                      " WHERE nzp_geu NOT IN ( SELECT nzp_geu FROM " + ReportParams.Pref + DBManager.sDataAliasRest + " s_geu ) ";
                ExecSQL(sql);

                sql = " UPDATE tt_report_30_1_5 " +
                          " SET geu = " + DBManager.sNvlWord + "( (SELECT geu FROM " + ReportParams.Pref + DBManager.sDataAliasRest + " s_geu g " +
                                " WHERE g.nzp_geu = tt_report_30_1_5.nzp_geu ), 'ЖЭУ неопределенно' ) ";
                ExecSQL(sql);

            sql = " UPDATE tt_report_30_1_5 " +
                  " SET name_agent = " + DBManager.sNvlWord + "( (SELECT name_agent" +
                                                                " FROM t_report_30_1_5 t" +
                                                                " WHERE t.pref = tt_report_30_1_5.pref ), '' )";
            ExecSQL(sql);

            sql = " UPDATE tt_report_30_1_5 " +
                  " SET director_post = " + DBManager.sNvlWord + "( (SELECT director_post" +
                                                                " FROM t_report_30_1_5 t" +
                                                                " WHERE t.pref = tt_report_30_1_5.pref ), '' )";
            ExecSQL(sql);
            sql = " UPDATE tt_report_30_1_5 " +
                  " SET chief_charge_post = " + DBManager.sNvlWord + "( (SELECT chief_charge_post" +
                                                                            " FROM t_report_30_1_5 t" +
                                                                            " WHERE t.pref = tt_report_30_1_5.pref ), '' )";
            ExecSQL(sql);
            sql = " UPDATE tt_report_30_1_5 " +
                  " SET chief_finance_post = " + DBManager.sNvlWord + "( (SELECT chief_finance_post" +
                                                                            " FROM t_report_30_1_5 t" +
                                                                            " WHERE t.pref = tt_report_30_1_5.pref ), '' )";
            ExecSQL(sql);
            sql = " UPDATE tt_report_30_1_5 " +
                  " SET director_name = " + DBManager.sNvlWord + "( (SELECT director_name" +
                                                                            " FROM t_report_30_1_5 t" +
                                                                            " WHERE t.pref = tt_report_30_1_5.pref ), '' )";
            ExecSQL(sql);
            sql = " UPDATE tt_report_30_1_5 " +
                  " SET chief_charge_name = " + DBManager.sNvlWord + "( (SELECT chief_charge_name" +
                                                                            " FROM t_report_30_1_5 t" +
                                                                            " WHERE t.pref = tt_report_30_1_5.pref ), '' )";
            ExecSQL(sql);
            sql = " UPDATE tt_report_30_1_5 " +
                  " SET chief_finance_name = " + DBManager.sNvlWord + "( (SELECT chief_finance_name" +
                                                                            " FROM t_report_30_1_5 t" +
                                                                            " WHERE t.pref = tt_report_30_1_5.pref ), '' )";
            ExecSQL(sql);
            #endregion

            sql = " SELECT " +
                         " pref, " +
                         " sort, " +
                         " TRIM(town) AS town, " +
                         " TRIM(name_agent) AS name_agent, " +
                         " TRIM(director_post) AS director_post, " +
                         " TRIM(chief_charge_post) AS chief_charge_post, " +
                         " TRIM(chief_finance_post) AS chief_finance_post, " +
                         " TRIM(director_name) AS director_name, " +
                         " TRIM(chief_charge_name) AS chief_charge_name, " +
                         " TRIM(chief_finance_name) AS chief_finance_name, " +
                         " TRIM(executor_name) AS executor_name,  " +
                            (Grouper == 1 
                            ? " TRIM(geu) "
                            : " TRIM(typek) ") + " AS grouper," +
                         " TRIM(service) AS service, " +
                         " SUM(sum_insaldo) AS sum_insaldo, " +
                         " SUM(rsum_tarif) AS rsum_tarif, " +
                         " SUM(0) AS izm_tarif, " +
                         " SUM(vozv) AS vozv, " +
                         " SUM(reval_k) AS reval_k, " +
                         " SUM(reval_d) AS reval_d, " +
                         " SUM(sum_ito) AS sum_ito, " +
                         " SUM(sum_otopl) AS sum_otopl, " +
                         " SUM(sum_charge) AS sum_charge, " +
                         " SUM(sum_money) AS sum_money, " +
                         " SUM(sum_outsaldo) AS sum_outsaldo, " +
                         " SUM(sum_odn) AS sum_odn, " +
                         " SUM(sum_insaldo_odn) AS sum_insaldo_odn " +
                  " FROM tt_report_30_1_5 GROUP BY 1,2,3,4,5,6,7,8,9,10,11,12,13  ORDER BY 1,3,4, grouper, 2 ";
            #region старый вариант
            //sql = " SELECT ABS(a.nzp_serv) AS nzp_serv, " +
            //             " a.pref, " +
            //             " sort, " +
            //             " TRIM(town) AS town, " +
            //             " TRIM(name_agent) AS name_agent, " +
            //             " TRIM(director_post) AS director_post, " +
            //             " TRIM(chief_charge_post) AS chief_charge_post, " +
            //             " TRIM(chief_finance_post) AS chief_finance_post, " +
            //             " TRIM(director_name) AS director_name, " +
            //             " TRIM(chief_charge_name) AS chief_charge_name, " +
            //             " TRIM(chief_finance_name) AS chief_finance_name, " +
            //             " TRIM('" + ReportParams.User.uname + "') AS executor_name,  " +
            //                (Grouper == 1 
            //                ? " (CASE WHEN g.nzp_geu IS NULL THEN 'ЖЭУ неопределенно' ELSE geu END) " 
            //                : " (CASE WHEN typek == 3 THEN 'Нежилое' ELSE 'Жилое' END) ") + " AS grouper," +
            //             " (CASE WHEN a.nzp_serv=9 THEN 'Горячая вода' ELSE TRIM(service) END) AS service, " +
            //             " SUM(sum_insaldo) AS sum_insaldo, " +
            //             " SUM(rsum_tarif) AS rsum_tarif, " +
            //             " SUM(0) AS izm_tarif, " +
            //             " SUM(vozv) AS vozv, " +
            //             " SUM(-1*" + DBManager.sNvlWord + "(reval_k,0)) AS reval_k, " +
            //             " SUM(reval_d) AS reval_d, " +
            //             " SUM(sum_ito) AS sum_ito, " +
            //             " SUM(sum_otopl) AS sum_otopl, " +
            //             " SUM(sum_charge) AS sum_charge, " +
            //             " SUM(sum_money) AS sum_money, " +
            //             " SUM(sum_outsaldo) AS sum_outsaldo, " +
            //             " SUM(sum_odn) AS sum_odn, " +
            //             " SUM(sum_insaldo_odn) AS sum_insaldo_odn " +
            //      " FROM  t_kart_anal_uch a INNER JOIN " + ReportParams.Pref + DBManager.sKernelAliasRest + " services s ON a.nzp_serv=s.nzp_serv " +
            //                              " INNER JOIN t_report_30_1_5 t2 ON a.pref = t2.pref " +
            //                              (Grouper == 1 ? " LEFT OUTER JOIN " + ReportParams.Pref + DBManager.sDataAliasRest + " s_geu g ON a.nzp_geu=g.nzp_geu " : "") +     
            //      " GROUP BY 1, 2, 3 ,4,5,6,7,8,9,10,11,12,13,14 " +
            //      " ORDER BY 2, grouper, 3 ";
            #endregion
            DataTable dt = ExecSQLToTable(sql);
            dt.TableName = "Q_master";
            var ds = new DataSet();
            ds.Tables.Add(dt);


            sql = " SELECT " +
                         " t2.pref, " +
                         " sort, " +
                         " TRIM(town) AS town, " +
                         " TRIM(name_agent) AS name_agent, " +
                         " TRIM(director_post) AS director_post, " +
                         " TRIM(chief_charge_post) AS chief_charge_post, " +
                         " TRIM(chief_finance_post) AS chief_finance_post, " +
                         " TRIM(director_name) AS director_name, " +
                         " TRIM(chief_charge_name) AS chief_charge_name, " +
                         " TRIM(chief_finance_name) AS chief_finance_name, " +
                         " (CASE WHEN nzp_serv=9 THEN 'Горячее водоснабжение' ELSE TRIM(service) END) AS service, " +
                         " SUM(sum_insaldo) AS sum_insaldo, " +
                         " SUM(rsum_tarif) AS rsum_tarif, " +
                         " SUM(0) AS izm_tarif, " +
                         " SUM(vozv) AS vozv, " +
                         " SUM(reval_k) AS reval_k, " +
                         " SUM(reval_d) AS reval_d, " +
                         " SUM(sum_ito) AS sum_ito, " +
                         " SUM(sum_otopl) AS sum_otopl, " +
                         " SUM(sum_charge) AS sum_charge, " +
                         " SUM(sum_money) AS sum_money, " +
                         " SUM(sum_outsaldo) AS sum_outsaldo, " +
                         " SUM(sum_odn) AS sum_odn, " +
                         " SUM(sum_insaldo_odn) AS sum_insaldo_odn " +
                         " FROM  tt_report_30_1_5 t2 " +
                         " GROUP BY 1,2,3,4,5,6,7,8,9,10,11 " +
                         " ORDER BY 1,3,4,2 ";

            DataTable dt1 = ExecSQLToTable(sql);
            dt1.TableName = "Q_master1";
            ds.Tables.Add(dt1);

            if (YearS == 2016 && MonthS <= 3 && MonthPo >= 3)
            {
                sql = " SELECT CASE WHEN p.nzp_serv=9 THEN 'Горячее водоснабжение' ELSE TRIM(service) END AS service, sum(sum_rcl) as sum_rcl " +
                      " FROM bill01_charge_16.perekidka p" +
                      " INNER JOIN fbill_data.document_base d on d.nzp_doc_base = p.nzp_doc_base" +
                      " INNER JOIN fbill_kernel.services s on s.nzp_serv = p.nzp_serv" +
                      " where month_ = 3" +
                      " AND d.comment = 'Выравнивание сальдо'" +
                      " group by 1";

                DataTable dt2 = ExecSQLToTable(sql);
                for (int i = 0; i < dt1.Rows.Count; i++)
                {
                    for (int j = 0; j < dt2.Rows.Count; j++)
                    {
                        if (dt1.Rows[i]["service"].ToString() == dt2.Rows[j]["service"].ToString())
                        {
                            if (dt1.Rows[i]["service"].ToString() == "Пени")
                            {
                                dt1.Rows[i]["reval_d"] = Convert.ToDecimal(dt1.Rows[i]["reval_d"]) -
                                                         Convert.ToDecimal(dt2.Rows[j]["sum_rcl"]);
                            }
                            else
                            {
                                dt1.Rows[i]["reval_k"] = Convert.ToDecimal(dt1.Rows[i]["reval_k"]) +
                                                         Convert.ToDecimal(dt2.Rows[j]["sum_rcl"]);
                            }
                        }
                    }
                }
            }

            return ds;
        }

        private void SetTown()
        {
            string sql = " UPDATE t_kart_anal_uch " +
                         " SET sort = (SELECT MAX(ordering) " +
                         " FROM " + ReportParams.Pref + DBManager.sKernelAliasRest + " services" +
                         " WHERE nzp_serv = t_kart_anal_uch.nzp_serv ) ";
            ExecSQL(sql);

            sql = " UPDATE t_kart_anal_uch " +
                  " SET town = ( SELECT town   " +
                  " FROM " +
                  ReportParams.Pref + DBManager.sDataAliasRest + "kvar k, " +
                  ReportParams.Pref + DBManager.sDataAliasRest + "dom d,  " +
                  ReportParams.Pref + DBManager.sDataAliasRest + "s_ulica u,   " +
                  ReportParams.Pref + DBManager.sDataAliasRest + "s_rajon r,  " +
                  ReportParams.Pref + DBManager.sDataAliasRest + "s_town t  " +
                  " WHERE k.nzp_kvar = t_kart_anal_uch.nzp_kvar " +
                  "       AND k.nzp_dom = d.nzp_dom" +
                  "       AND d.nzp_ul = u.nzp_ul " +
                  "       AND r.nzp_raj = u.nzp_raj  " +
                  "       AND t.nzp_town = r.nzp_town   ) ";
            ExecSQL(sql);

         
        }


        /// <summary>
        /// Подсчет одного месяца
        /// </summary>
        /// <param name="pref">Префикс схемы БД</param>
        /// <param name="curYear">Год</param>
        /// <param name="curMonth">Месяц</param>
        /// <param name="listLc">Признак того, что отчет по списку ЛС</param>
        /// <param name="whereArea">Ограничение на УК </param>
        /// <param name="whereSupp">Ограничение на поставщиков</param>
        private void CalcOneMonth(string pref, int curYear, int curMonth, bool listLc, string whereArea, string whereSupp)
        {
            string chargeTable = pref + "_charge_" + (curYear - 2000).ToString("00") +
                                 DBManager.tableDelimiter + "charge_" +
                                 curMonth.ToString("00");
            string dataTable = pref + "_data" +
                                 DBManager.tableDelimiter + "kvar";

            string perekidkaTable = pref + "_charge_" + (curYear - 2000).ToString("00") +
                                    DBManager.tableDelimiter + "perekidka";

            string sumInsaldo = "0";
            string sumOutsaldo = "0";

            string dbFnSupplier = pref + "_charge_" + (curYear - 2000).ToString("00") +
                                         DBManager.tableDelimiter + "fn_supplier" + curMonth.ToString("00");
            string dbPackLs = "fbill_fin_" + (curYear - 2000).ToString("00") +
                              DBManager.tableDelimiter + "pack_ls ";
            string dbPack = "fbill_fin_" + (curYear - 2000).ToString("00") +
                            DBManager.tableDelimiter + "pack ";

            string monthYear = (curMonth + curYear).ToString(CultureInfo.InvariantCulture);
            if (curMonth == MonthS && curYear == YearS) sumInsaldo = "sum_insaldo";
            if (curMonth == MonthPo && curYear == YearPo) sumOutsaldo = "sum_outsaldo";
            if (TempTableInWebCashe(chargeTable))
            {
                #region старый вариант
                //string sql = " INSERT INTO t_kart_anal_uch(pref, nzp_kvar, typek, nzp_geu, nzp_serv, nzp_supp, sum_insaldo, rsum_tarif, izm_tarif, vozv, " +
                //             " real_charge, reval_k, reval_d, sum_ito, sum_charge, sum_odn, sum_insaldo_odn, sum_money, sum_outsaldo ) " +
                //             " SELECT '" + pref + "' AS pref, " +
                //             " kv.nzp_kvar, " +
                //             " MAX(kv.typek) AS typek, " +
                //             " MAX(kv.nzp_geu) AS nzp_geu, " +
                //             " (CASE WHEN nzp_serv=14 THEN 9 ELSE nzp_serv END) AS nzp_serv, " +
                //             " nzp_supp, " +
                //             " SUM(" + sumInsaldo + ") AS sum_insaldo, " +
                //             " SUM(rsum_tarif) AS rsum_tarif, " +
                //             " SUM(0) AS izm_tarif, " +
                //             " SUM(sum_nedop) AS vozv, " +
                //             " SUM(real_charge) AS real_charge, " +
                //             " SUM(CASE WHEN reval<0 THEN reval ELSE 0 END)+" +
                //             " SUM(CASE WHEN real_charge<0 THEN real_charge ELSE 0 END) AS reval_k, " +
                //             " SUM(CASE WHEN reval<0 THEN 0 ELSE reval END)+" +
                //             " SUM(CASE WHEN real_charge<0 THEN 0 ELSE real_charge END) AS reval_d, " +
                //             " SUM(sum_charge) AS sum_ito, " +
                //             " SUM(sum_charge) AS sum_charge, " +
                //             " SUM(0) AS sum_odn, " +
                //             " SUM(0) AS sum_insaldo_odn, " +
                //             " SUM(sum_money) AS sum_money, " +
                //             " SUM(" + sumOutsaldo + ") AS sum_outsaldo " +
                //             " FROM " + chargeTable + " ch, " +
                //             (listLc ? " selected_kvars kv, " : pref + DBManager.sDataAliasRest + "kvar kv ") +
                //             " WHERE ch.nzp_kvar=kv.nzp_kvar " +
                //             " AND dat_charge IS NULL  " +
                //             " AND nzp_serv>1 " +
                //             " AND ABS(sum_insaldo) + ABS(rsum_tarif) + ABS(real_charge) + " +
                //             " ABS(reval) + ABS(sum_money) + ABS(sum_charge) > 0.001 " +
                //             whereArea + whereSupp + (ExcludeKapr ? " and nzp_serv not in (206)" : "") +
                //             GetRajon("kv.") +
                //             " GROUP BY 1,2,5,6 ";
                //ExecSQL(sql);
                #endregion
                string sql = " INSERT INTO t_kart_anal_uch(pref, month_year, nzp_kvar, typek, nzp_geu, nzp_serv, nzp_supp, sum_insaldo, sum_outsaldo ) " +
                             " SELECT '" + pref + "' AS pref, " +
                                    " '" + monthYear + "' AS month_year, " +
                                    " kv.nzp_kvar, " +
                                    " MAX(kv.typek) AS typek, " +
                                    " MAX(kv.nzp_geu) AS nzp_geu, " +
                                    " (CASE WHEN nzp_serv=14 THEN 9 ELSE nzp_serv END) AS nzp_serv, " +
                                    " nzp_supp," +
                                    " SUM(" + sumInsaldo + ") AS sum_insaldo, " +
                                    " SUM(" + sumOutsaldo + ") AS sum_outsaldo " +
                             " FROM " + chargeTable + " ch, " +
                                    (listLc ? " selected_kvars kv, " : pref + DBManager.sDataAliasRest + "kvar kv ") +
                             " WHERE ch.nzp_kvar=kv.nzp_kvar " +
                               " AND dat_charge IS NULL  " +
                               " AND nzp_serv>1 " +
                               " AND ABS(sum_insaldo) + ABS(rsum_tarif) + ABS(real_charge) + " +
                               " ABS(reval) + ABS(sum_money) + ABS(sum_charge) > 0.001 " +
                               whereArea + whereSupp + GetRajon("kv.") +
                             " GROUP BY 1,2,3,6,7 ";
                ExecSQL(sql);



                if (ExcludeKapr)
                    ExecSQL(" DELETE FROM t_kart_anal_uch WHERE nzp_serv = 206 ");
                ExecSQL(DBManager.sUpdStat + " t_kart_anal_uch");

                #region заполнение временной таблицы  первоначальными данными

                sql = " UPDATE t_kart_anal_uch " +
                      " SET rsum_tarif = " + DBManager.sNvlWord + "((SELECT SUM(rsum_tarif) " +
                                                                    " FROM " + chargeTable + " ch " +
                                                                    " WHERE ch.nzp_kvar = t_kart_anal_uch.nzp_kvar " +
                                                                    " AND (CASE WHEN ch.nzp_serv=14 THEN 9 ELSE ch.nzp_serv END) = t_kart_anal_uch.nzp_serv " +
                                                                    " AND ch.nzp_supp = t_kart_anal_uch.nzp_supp " +
                                                                    " AND dat_charge IS NULL " +
                                                                    " AND nzp_serv>1 " + whereSupp +
                                                                    " AND ABS(sum_insaldo) + " +
                                                                            " ABS(rsum_tarif) + " +
                                                                                " ABS(real_charge) +  " +
                                                                                    " ABS(reval) + " +
                                                                                        " ABS(sum_money) + " +
                                                                                            " ABS(sum_charge) > 0.001),0) " +
                      " WHERE pref = '" + pref + "' AND month_year = '" + monthYear + "' ";
                ExecSQL(sql);


                sql = " UPDATE t_kart_anal_uch " +
                      " SET vozv = " + DBManager.sNvlWord + "((SELECT SUM(sum_nedop) " +
                                                                    " FROM " + chargeTable + " ch " +
                                                                    " WHERE ch.nzp_kvar = t_kart_anal_uch.nzp_kvar " +
                                                                    " AND (CASE WHEN ch.nzp_serv=14 THEN 9 ELSE ch.nzp_serv END) = t_kart_anal_uch.nzp_serv " +
                                                                    " AND ch.nzp_supp = t_kart_anal_uch.nzp_supp " +
                                                                    " AND dat_charge IS NULL " +
                                                                    " AND nzp_serv>1 " + whereSupp +
                                                                    " AND ABS(sum_insaldo) + " +
                                                                            " ABS(rsum_tarif) + " +
                                                                                " ABS(real_charge) +  " +
                                                                                    " ABS(reval) + " +
                                                                                        " ABS(sum_money) + " +
                                                                                            " ABS(sum_charge) > 0.001),0) " +
                      " WHERE pref = '" + pref + "' AND month_year = '" + monthYear + "' ";
                ExecSQL(sql);


                sql = " UPDATE t_kart_anal_uch " +
                      " SET real_charge = " + DBManager.sNvlWord + "((SELECT SUM(real_charge) " +
                                                                    " FROM " + chargeTable + " ch " +
                                                                    " WHERE ch.nzp_kvar = t_kart_anal_uch.nzp_kvar " +
                                                                    " AND (CASE WHEN ch.nzp_serv=14 THEN 9 ELSE ch.nzp_serv END) = t_kart_anal_uch.nzp_serv " +
                                                                    " AND ch.nzp_supp = t_kart_anal_uch.nzp_supp " +
                                                                    " AND dat_charge IS NULL " +
                                                                    " AND nzp_serv>1 " + whereSupp +
                                                                    " AND ABS(sum_insaldo) + " +
                                                                            " ABS(rsum_tarif) + " +
                                                                                " ABS(real_charge) +  " +
                                                                                    " ABS(reval) + " +
                                                                                        " ABS(sum_money) + " +
                                                                                            " ABS(sum_charge) > 0.001),0) " +
                      " WHERE pref = '" + pref + "' AND month_year = '" + monthYear + "' ";
                ExecSQL(sql);


                sql = " UPDATE t_kart_anal_uch " +
                      " SET reval_k = " + DBManager.sNvlWord + "((SELECT SUM(CASE WHEN reval<0 THEN reval ELSE 0 END) + SUM(CASE WHEN real_charge<0 THEN real_charge ELSE 0 END) " +
                                                                    " FROM " + chargeTable + " ch " +
                                                                    " WHERE ch.nzp_kvar = t_kart_anal_uch.nzp_kvar " +
                                                                    " AND (CASE WHEN ch.nzp_serv=14 THEN 9 ELSE ch.nzp_serv END) = t_kart_anal_uch.nzp_serv " +
                                                                    " AND ch.nzp_supp = t_kart_anal_uch.nzp_supp " +
                                                                    " AND dat_charge IS NULL " +
                                                                    " AND nzp_serv>1 " + whereSupp +
                                                                    " AND ABS(sum_insaldo) + " +
                                                                            " ABS(rsum_tarif) + " +
                                                                                " ABS(real_charge) +  " +
                                                                                    " ABS(reval) + " +
                                                                                        " ABS(sum_money) + " +
                                                                                            " ABS(sum_charge) > 0.001),0) " +
                      " WHERE pref = '" + pref + "' AND month_year = '" + monthYear + "' ";
                ExecSQL(sql);

                sql = " UPDATE t_kart_anal_uch " +
                      " SET reval_d = " + DBManager.sNvlWord + "((SELECT SUM(CASE WHEN reval<0 THEN 0 ELSE reval END) + SUM(CASE WHEN real_charge<0 THEN 0 ELSE real_charge END) " +
                                                                    " FROM " + chargeTable + " ch " +
                                                                    " WHERE ch.nzp_kvar = t_kart_anal_uch.nzp_kvar " +
                                                                    " AND (CASE WHEN ch.nzp_serv=14 THEN 9 ELSE ch.nzp_serv END) = t_kart_anal_uch.nzp_serv " +
                                                                    " AND ch.nzp_supp = t_kart_anal_uch.nzp_supp " +
                                                                    " AND dat_charge IS NULL " +
                                                                    " AND nzp_serv>1 " + whereSupp +
                                                                    " AND ABS(sum_insaldo) + " +
                                                                            " ABS(rsum_tarif) + " +
                                                                                " ABS(real_charge) +  " +
                                                                                    " ABS(reval) + " +
                                                                                        " ABS(sum_money) + " +
                                                                                            " ABS(sum_charge) > 0.001),0) " +
                      " WHERE pref = '" + pref + "' AND month_year = '" + monthYear + "' ";
                ExecSQL(sql);


                sql = " UPDATE t_kart_anal_uch " +
                      " SET sum_ito = " + DBManager.sNvlWord + "((SELECT SUM(sum_charge) " +
                                                                    " FROM " + chargeTable + " ch " +
                                                                    " WHERE ch.nzp_kvar = t_kart_anal_uch.nzp_kvar " +
                                                                    " AND (CASE WHEN ch.nzp_serv=14 THEN 9 ELSE ch.nzp_serv END) = t_kart_anal_uch.nzp_serv " +
                                                                    " AND ch.nzp_supp = t_kart_anal_uch.nzp_supp " +
                                                                    " AND dat_charge IS NULL " +
                                                                    " AND nzp_serv>1 " + whereSupp +
                                                                    " AND ABS(sum_insaldo) + " +
                                                                            " ABS(rsum_tarif) + " +
                                                                                " ABS(real_charge) +  " +
                                                                                    " ABS(reval) + " +
                                                                                        " ABS(sum_money) + " +
                                                                                            " ABS(sum_charge) > 0.001),0) " +
                      " WHERE pref = '" + pref + "' AND month_year = '" + monthYear + "' ";
                ExecSQL(sql);



                sql = " UPDATE t_kart_anal_uch " +
                      " SET sum_charge = " + DBManager.sNvlWord + "((SELECT SUM(sum_charge) " +
                                                                    " FROM " + chargeTable + " ch " +
                                                                    " WHERE ch.nzp_kvar = t_kart_anal_uch.nzp_kvar " +
                                                                    " AND (CASE WHEN ch.nzp_serv=14 THEN 9 ELSE ch.nzp_serv END) = t_kart_anal_uch.nzp_serv " +
                                                                    " AND ch.nzp_supp = t_kart_anal_uch.nzp_supp " +
                                                                    " AND dat_charge IS NULL " +
                                                                    " AND nzp_serv>1 " + whereSupp +
                                                                    " AND ABS(sum_insaldo) + " +
                                                                            " ABS(rsum_tarif) + " +
                                                                                " ABS(real_charge) +  " +
                                                                                    " ABS(reval) + " +
                                                                                        " ABS(sum_money) + " +
                                                                                            " ABS(sum_charge) > 0.001),0) " +
                      " WHERE pref = '" + pref + "' AND month_year = '" + monthYear + "' ";
                ExecSQL(sql);


                sql = " UPDATE t_kart_anal_uch " +
                      " SET sum_money = " + DBManager.sNvlWord + "((SELECT SUM(sum_prih) " +
                                                                    " FROM " + dbFnSupplier + " ch " +
                                                                    " INNER JOIN " + dataTable + " k on k.num_ls = ch.num_ls " +
                                                                    " WHERE k.nzp_kvar = t_kart_anal_uch.nzp_kvar " +
                                                                    " AND (CASE WHEN ch.nzp_serv=14 THEN 9 ELSE ch.nzp_serv END) = t_kart_anal_uch.nzp_serv " +
                                                                    " AND ch.nzp_supp = t_kart_anal_uch.nzp_supp " +
                                                                    //" AND dat_charge IS NULL " +
                                                                    " AND nzp_serv>1 " + whereSupp +
                                                                    //" AND ABS(sum_insaldo) + " +
                                                                            //" ABS(rsum_tarif) + " +
                                                                                //" ABS(real_charge) +  " +
                                                                                    //" ABS(reval) + " +
                                                                                        //" ABS(sum_money) + " +
                                                                                            //" ABS(sum_charge) > 0.001),0) " +
                      "),0) WHERE pref = '" + pref + "' AND month_year = '" + monthYear + "' ";
                ExecSQL(sql);
                #endregion

                sql =
                   " INSERT INTO t_kart_anal_uch(pref, nzp_kvar, typek, nzp_geu, nzp_serv, nzp_supp, " +
                   " sum_insaldo, rsum_tarif, izm_tarif, sum_otopl, " +
                   " reval_k, reval_d, sum_ito, sum_charge, sum_odn, sum_money, sum_outsaldo) " +
                   " SELECT '" + pref + "' AS pref, " +
                   " kv.nzp_kvar, " +
                   " MAX(kv.typek) AS typek, " +
                   " MAX(kv.nzp_geu) AS nzp_geu, " +
                   " (CASE WHEN nzp_serv=14 THEN 9 WHEN nzp_serv=514 THEN 513 ELSE nzp_serv END) AS nzp_serv, " +
                   " nzp_supp, " +
                   " SUM(0) as sum_insaldo, " +
                   " SUM(0) as rsum_tarif, " +
                   " SUM(0) as izm_tarif, " +
                   " SUM(sum_rcl) as sum_otopl, " +
                   " SUM(case when sum_rcl<0 then -sum_rcl else 0 end ) as reval_k, " +
                   " SUM(case when sum_rcl>0 then -sum_rcl else 0 end) as reval_d, " +
                   " -SUM(sum_rcl) as sum_ito, " +
                   " SUM(0) as sum_charge, " +
                   " sum(0) as sum_odn, " +
                   " SUM(0) as sum_money, " +
                   " SUM(0) as sum_outsaldo " +
                   " FROM " + perekidkaTable + " p, " +
                   (listLc ? " selected_kvars kv, " : pref + DBManager.sDataAliasRest + "kvar kv ") +
                   " WHERE p.nzp_kvar=kv.nzp_kvar " +
                   " AND type_rcl = 110 and nzp_serv=8  " +
                   " AND month_=" + curMonth +
                   " AND ABS(sum_rcl)>0.001 " +
                   whereArea + whereSupp + GetRajon("kv.") +
                   " GROUP BY 1,2,5,6 ";
                ExecSQL(sql);

                sql =
                    " INSERT INTO t_kart_anal_uch(pref, nzp_kvar, typek, nzp_geu, nzp_serv, nzp_supp, sum_insaldo, rsum_tarif, izm_tarif, vozv, " +
                    " reval_k, reval_d, sum_ito, sum_charge, sum_odn, sum_money, sum_outsaldo ) " +
                    " SELECT '" + pref + "' AS pref, " +
                    " kv.nzp_kvar, " +
                    " MAX(kv.typek) AS typek, " +
                    " MAX(kv.nzp_geu) AS nzp_geu, " +
                    " (CASE WHEN nzp_serv=14 THEN 9 WHEN nzp_serv=514 THEN 513 ELSE nzp_serv END) AS nzp_serv, " +
                    " nzp_supp, " +
                    " SUM(0) as sum_insaldo, " +
                    " SUM(0) as rsum_tarif, " +
                    " SUM(0) as izm_tarif, " +
                    " -SUM(sum_rcl) as vozv, " +
                    " -SUM(sum_rcl) as reval_k, " +
                    " SUM(0) as reval_d, " +
                    " SUM(0) as sum_ito, " +
                    " SUM(0) as sum_charge, " +
                    " sum(0) as sum_odn, " +
                    " SUM(0) as sum_money, " +
                    " SUM(0) as sum_outsaldo " +
                    " FROM " + perekidkaTable + " p, " +
                    (listLc ? " selected_kvars kv, " : pref + DBManager.sDataAliasRest + "kvar kv ") +
                    " WHERE p.nzp_kvar=kv.nzp_kvar " +
                    " AND type_rcl = 101 " +
                    " AND month_=" + curMonth +
                    " AND ABS(sum_rcl)>0.001 " +
                    whereArea + whereSupp + (ExcludeKapr ? " and nzp_serv not in (206)" : "") +
                    GetRajon("kv.") +
                    " GROUP BY 1,2,5,6 ";
                ExecSQL(sql);

                #region Выборка перерасчетов прошлого периода

                sql = " Create temp table t_nedop ( " +
                      " nzp_geu integer, " +
                      " typek integer, " +
                      " nzp_kvar integer, " +
                      " month_s integer, " +
                      " month_po integer)" + DBManager.sUnlogTempTable;
                ExecSQL(sql);

                sql = " insert into t_nedop(nzp_geu, typek, nzp_kvar, month_s, month_po) " +
                      " select MAX(b.nzp_geu) AS nzp_geu, MAX(b.typek) as typek, a.nzp_kvar, min(" +
                      DBManager.sYearFromDate + " dat_s)*12+ " + DBManager.sMonthFromDate +
                      " dat_s)) as month_s, " +
                      " max(" + DBManager.sYearFromDate + " dat_po)*12+ " + DBManager.sMonthFromDate +
                      " dat_po)) as month_po" +
                      " from " + pref + DBManager.sDataAliasRest + "nedop_kvar a, " +
                      (listLc ? " selected_kvars b, " : pref + DBManager.sDataAliasRest + "kvar b ") +
                      " where a.nzp_kvar=b.nzp_kvar and month_calc='01." + curMonth.ToString("00") + "." +
                      curYear.ToString("0000") + "' " +
                      GetRajon("b.") +
                      " group by 3 ";
                ExecSQL(sql, true);

                ExecSQL("create index ix_t_nedop_01 on t_nedop(nzp_kvar)");
                ExecSQL(DBManager.sUpdStat + " t_nedop");

                MyDataReader reader2;

                sql = " select month_, year_ " +
                      " from " + pref + "_charge_" + (curYear - 2000).ToString("00")
                      + DBManager.tableDelimiter + "lnk_charge_" + curMonth.ToString("00") + " b, t_nedop d " +
                      " where  b.nzp_kvar=d.nzp_kvar and year_*12+month_>=month_s and  year_*12+month_<=month_po" +
                      " group by 1,2";
                ExecRead(out reader2, sql);
                while (reader2.Read())
                {
                    string sTmpAlias = pref + "_charge_" +
                                       (Int32.Parse(reader2["year_"].ToString()) - 2000).ToString("00");


                    sql =
                        " insert into t_kart_anal_uch (pref, nzp_kvar, typek, nzp_geu, nzp_serv, nzp_supp, vozv, reval_k, reval_d) " +
                        " select '" + pref +
                        "' AS pref, b.nzp_kvar, MAX(typek) AS typek, MAX(nzp_geu) AS nzp_geu, " +
                        " (CASE WHEN nzp_serv=14 THEN 9 WHEN nzp_serv=514 THEN 513 ELSE nzp_serv END), " +
                        " nzp_supp, sum(sum_nedop-sum_nedop_p),  " +
                        " sum(case when (sum_nedop-sum_nedop_p)>0 " +
                        " then sum_nedop-sum_nedop_p else 0 end ) as reval_k," +
                        " sum(case when (sum_nedop-sum_nedop_p)>0 " +
                        " then 0 else sum_nedop-sum_nedop_p end ) as reval_d" +
                        " from " + sTmpAlias + DBManager.tableDelimiter + "charge_" +
                        Int32.Parse(reader2["month_"].ToString()).ToString("00") +
                        " b, t_nedop d " +
                        " where  b.nzp_kvar=d.nzp_kvar and dat_charge = date('28." + curMonth.ToString("00") + "." +
                        curYear + "')" +
                        " and abs(sum_nedop)+abs(sum_nedop_p)>0.001" +
                        whereSupp + (ExcludeKapr ? " and nzp_serv not in (206)" : "") +
                        " group by 1,2,5,6";
                    ExecSQL(sql);
                }
                reader2.Close();
                ExecSQL("drop table t_nedop");

                #endregion
            }
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
            whereArea = !String.IsNullOrEmpty(whereArea) ? " AND kv.nzp_area in (" + whereArea + ")" : String.Empty;
            if (!String.IsNullOrEmpty(whereArea))
            {
                string sql = " SELECT area from " + ReportParams.Pref + DBManager.sDataAliasRest + "s_area kv  WHERE kv.nzp_area > 0 " + whereArea;
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
            whereSupp = !String.IsNullOrEmpty(whereSupp) ? " AND nzp_supp in (" + whereSupp + ")" : String.Empty;
            if (!String.IsNullOrEmpty(whereSupp))
            {
                string sql = " SELECT name_supp from " + ReportParams.Pref + DBManager.sKernelAliasRest + "supplier  WHERE nzp_supp > 0 " + whereSupp;
                DataTable supp = ExecSQLToTable(sql);
                foreach (DataRow dr in supp.Rows)
                {
                    SupplierHeader += dr["name_supp"].ToString().Trim() + ", ";
                }
                SupplierHeader = string.IsNullOrEmpty(SupplierHeader) ? "" : SupplierHeader.TrimEnd(',', ' ');
            }
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
            int day = MonthPo == DateTime.Now.Month && YearPo == DateTime.Now.Year ? DateTime.Now.Day : 1;
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
                                                      " AND dat_s <='" + day + "." + MonthPo + "." + YearPo + "' " +
                                                      " AND dat_po >='" + day + "." + MonthPo + "." + YearPo + "' " +
                           " AND nzp_prm = " + nzpPrm);

            if (nameTable.Rows.Count == 1)
            {
                string sql = " UPDATE t_report_30_1_5 " +
                             " SET " + nameColumn + " = '" + nameTable.Rows[0][0].ToString().Trim() + "' " +
                         " WHERE t_report_30_1_5.pref = '" + pref + "' ";
            ExecSQL(sql);
            }
            else
            {
                //Подписи Excel
                string sql = " UPDATE t_report_30_1_5 " +
                             " SET " + nameColumn + " = '";
                switch (nzpPrm)
                {
                    case 80: sql += "ЖКХ"; break;
                    case 1291: sql += "Ген. директор  "; break;
                    case 1292: sql += "Исполнитель"; break;
                    //case 1293: sql += "Нач. отдела бюджетирования и учета"; break;
                    case 1294: sql += "Гордиевский П.А"; break;
                    //case 1295: sql += "Миллер Ю.А."; break;
                    //case 1296: sql += "Соковых И.А."; break;
                }
                sql += "' WHERE t_report_30_1_5.pref = '" + pref + "' ";
                ExecSQL(sql);
        }
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
                    string sql = " insert into selected_kvars (nzp_kvar, nzp_geu, typek) " +
                                 " select num_ls from " + tSpls;
                    ExecSQL(sql);
                    ExecSQL("create index ix_tmpsk_ls_01 in selected_kvars(nzp_kvar) ");
                    ExecSQL(DBManager.sUpdStat + " selected_kvars ");
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Определяет есть ли открытый месяц ранее конца периода
        /// выполнения отчета
        /// </summary>
        /// <param name="pref">Префикс схемы БД</param>
        /// <returns>True если нет</returns>
        private bool CheckSaldoDate(string pref)
        {
            string sql = " select 1 from " + pref + DBManager.sDataAliasRest + "saldo_date" +
                         " where  dat_saldo <=Date('" +
                         DateTime.DaysInMonth(YearPo, MonthPo).ToString("00") + "." +
                         MonthPo.ToString("00") + "." + YearPo + "') and iscurrent = 0 ";
            object obj = ExecScalar(sql);

            return obj == null;
        }


        protected override void CreateTempTable()
        {
            string sql = " Create temp table t_kart_anal_uch( " +
                               " pref CHARACTER(100)," +
                               " month_year CHARACTER(10), " +
                               " nzp_kvar integer, " +
                               " town CHARACTER(30), " +
                               " nzp_geu integer, " +
                               " typek integer, " +
                               " nzp_serv integer, " +
                               " sort INTEGER, " +
                               " nzp_supp integer, " +
                               " real_charge " + DBManager.sDecimalType + "(14,2)," +
                               " sum_otopl " + DBManager.sDecimalType + "(14,2)," +
                               " sum_insaldo " + DBManager.sDecimalType + "(14,2)," +
                               " rsum_tarif " + DBManager.sDecimalType + "(14,2)," +
                               " izm_tarif " + DBManager.sDecimalType + "(14,2) DEFAULT 0," +
                               " vozv " + DBManager.sDecimalType + "(14,2), " +
                               " reval_k " + DBManager.sDecimalType + "(14,2) DEFAULT 0, " +
                               " reval_d " + DBManager.sDecimalType + "(14,2) DEFAULT 0, " +           
                               " sum_ito " + DBManager.sDecimalType + "(14,2), " +
                               " sum_charge " + DBManager.sDecimalType + "(14,2), " +
                               " sum_odn " + DBManager.sDecimalType + "(14,2) DEFAULT 0, " +
                               " sum_insaldo_odn " + DBManager.sDecimalType + "(14,2) DEFAULT 0, " +
                               " sum_money " + DBManager.sDecimalType + "(14,2), " +
                               " sum_outsaldo " + DBManager.sDecimalType + "(14,2) DEFAULT 0)" + DBManager.sUnlogTempTable;

            ExecSQL(sql);

            ExecSQL(" CREATE INDEX ix_temp_kart_nzp_kvar ON t_kart_anal_uch(nzp_kvar) ");
            ExecSQL(" CREATE INDEX ix_temp_kart_nzp_serv ON t_kart_anal_uch(nzp_serv) ");
            ExecSQL(" CREATE INDEX ix_temp_kart_nzp_supp ON t_kart_anal_uch(nzp_supp) ");
            ExecSQL(" CREATE INDEX ix_temp_kart_nzp_pref0 ON t_kart_anal_uch(pref) ");
 

            sql = " CREATE TEMP TABLE t_report_30_1_5 ( " +
                        " pref CHARACTER(100)," +
                        " director_post CHARACTER(60), " +
                        " chief_charge_post CHARACTER(60), " +
                        " chief_finance_post CHARACTER(60), " +
                        " director_name CHARACTER(30), " +
                        " chief_charge_name CHARACTER(30), " +
                        " chief_finance_name CHARACTER(30), " +
                        " name_agent CHARACTER(60))" + DBManager.sUnlogTempTable;
            ExecSQL(sql);
            ExecSQL(" CREATE INDEX ix_temp_kart_nzp_pref1 ON t_report_30_1_5(pref) ");

            if (ReportParams.CurrentReportKind == ReportKind.ListLC)
            {
                sql = " create temp table selected_kvars(" +
                    " nzp_kvar integer," +
                      " nzp_geu integer," +
                      " typek integer ) " +
                DBManager.sUnlogTempTable;
                ExecSQL(sql);
            }

            sql = " CREATE TEMP TABLE tt_report_30_1_5( " +
                    " pref CHARACTER(100)," +
                    " nzp_serv INTEGER, " +
                    " sort INTEGER DEFAULT 1000000, " +
                    " town CHARACTER(100), " +
                    " nzp_geu integer, " +
                    " geu CHARACTER(60), " +
                    " typek CHARACTER(10), " +
                    " name_agent CHARACTER(60), " +
                    " director_post CHARACTER(60), " +
                    " chief_charge_post CHARACTER(60), " + 
                    " chief_finance_post CHARACTER(60), " +
                    " director_name CHARACTER(100), " +
                    " chief_charge_name CHARACTER(100), " + 
                    " chief_finance_name CHARACTER(100), " + 
                    " executor_name CHARACTER(100), " +
                    " grouper CHARACTER(60), " +
                    " service CHARACTER(100), " +
                    " sum_insaldo " + DBManager.sDecimalType + "(14,2), " +
                    " rsum_tarif " + DBManager.sDecimalType + "(14,2), " + 
                    " izm_tarif " + DBManager.sDecimalType + "(14,2), " + 
                    " vozv " + DBManager.sDecimalType + "(14,2), " + 
                    " reval_k " + DBManager.sDecimalType + "(14,2), " + 
                    " reval_d " + DBManager.sDecimalType + "(14,2), " +
                    " sum_ito " + DBManager.sDecimalType + "(14,2), " +
                    " sum_otopl " + DBManager.sDecimalType + "(14,2), " + 
                    " sum_charge " + DBManager.sDecimalType + "(14,2), " +
                    " sum_money " + DBManager.sDecimalType + "(14,2), " + 
                    " sum_outsaldo " + DBManager.sDecimalType + "(14,2), " +
                    " sum_odn " + DBManager.sDecimalType + "(14,2), " +
                    " sum_insaldo_odn " + DBManager.sDecimalType + "(14,2)) " + DBManager.sUnlogTempTable;
            ExecSQL(sql);
            ExecSQL(" CREATE INDEX ix_temp_kart_nzp_pref2 ON tt_report_30_1_5(pref) ");
        }

        protected override void DropTempTable()
        {
            ExecSQL(" DROP TABLE t_kart_anal_uch ");
            ExecSQL(" DROP TABLE t_report_30_1_5 ");
            ExecSQL(" DROP TABLE tt_report_30_1_5 ");

            if (TempTableInWebCashe("t_nedop"))
            {
                ExecSQL("drop table t_nedop");
            }
            if (ReportParams.CurrentReportKind == ReportKind.ListLC)
                ExecSQL(" drop table selected_kvars ", true);

        }
    }
}
