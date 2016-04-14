using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Bars.KP50.Report.Base;
using Bars.KP50.Report.Tula.Properties;
using Bars.KP50.Utils;
using Newtonsoft.Json;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;

namespace Bars.KP50.Report.Tula.Reports
{
    public class Report7105003 : BaseSqlReport
    {
        public override string Name
        {
            get { return "71.5.3 Сальдовая ведомость по домам"; }
        }

        public override string Description
        {
            get { return "5.3 Сальдовая ведомость по домам"; }
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
            get { return Resources.Report_71_5_3; }
        }

        public override IList<ReportKind> ReportKinds
        {
            get { return new List<ReportKind> { ReportKind.Base, ReportKind.ListLC }; }
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

        /// <summary>Заголовок отчета</summary>
        protected string SupplierHeader { get; set; }

        /// <summary>Заголовок отчета</summary>
        protected string ServiceHeader { get; set; }

        /// <summary>Заголовок территории</summary>
        protected string TerritoryHeader { get; set; }

        /// <summary>Поставщики, Агенты, Принципалы  </summary>
        protected BankSupplierParameterValue BankSupplier { get; set; }

        public override List<UserParam> GetUserParams()
        {
            var curCalcMonthYear = DBManager.GetCurMonthYear();
            return new List<UserParam>
            {
                new MonthParameter {Name = "Месяц с", Value = curCalcMonthYear != null ? curCalcMonthYear.Rows[0]["month_"] : DateTime.Today.Month },
                new YearParameter {Name = "Год с", Value = curCalcMonthYear != null ? curCalcMonthYear.Rows[0]["yearr"] : DateTime.Today.Year },
                new MonthParameter {Name = "Месяц по", Code = "Month1", Value = curCalcMonthYear != null ? curCalcMonthYear.Rows[0]["month_"] : DateTime.Today.Month },
                new YearParameter {Name = "Год по", Code = "Year1", Value = curCalcMonthYear != null ? curCalcMonthYear.Rows[0]["yearr"] : DateTime.Today.Year },
                new BankSupplierParameter(),
                new ServiceParameter()
            };
        }

        protected override void PrepareParams() {
            MonthS = UserParamValues["Month"].GetValue<int>();
            YearS = UserParamValues["Year"].Value.To<int>();
            MonthPo = UserParamValues["Month1"].GetValue<int>();
            YearPo = UserParamValues["Year1"].Value.To<int>();
            Services = UserParamValues["Services"].Value.To<List<int>>(); 
            BankSupplier = JsonConvert.DeserializeObject<BankSupplierParameterValue>(UserParamValues["BankSupplier"].Value.ToString());
        }

        protected override void PrepareReport(FastReport.Report report) {
            string[] months = {"","Январь","Февраль",
                "Март","Апрель","Май","Июнь","Июль","Август","Сентябрь",
                "Октябрь","Ноябрь","Декабрь"};
            if ((MonthS == MonthPo) & (YearS == YearPo))
            {
                report.SetParameterValue("pPeriod", months[MonthS] + " " + YearS+"г.");
            }
            else
            {
                report.SetParameterValue("pPeriod", "период с " + months[MonthS] + " " + YearS +
                                                         "г. по " + months[MonthPo] + " " + YearPo + "г."); 
            }
            //report.SetParameterValue("gr", GroupsHeader);
            report.SetParameterValue("DATE", DateTime.Now.ToShortDateString());
            report.SetParameterValue("TIME", DateTime.Now.ToLongTimeString());

            string headerParam = !string.IsNullOrEmpty(TerritoryHeader) ? "Территория: " + TerritoryHeader + "\n" : string.Empty;
            headerParam += !string.IsNullOrEmpty(SupplierHeader) ? "Поставщики: " + SupplierHeader + "\n" : string.Empty;
            headerParam += !string.IsNullOrEmpty(ServiceHeader) ? "Услуги: " + ServiceHeader : string.Empty;
            headerParam = headerParam.TrimEnd('\n');
            report.SetParameterValue("headerParam", headerParam);
        }

        public override DataSet GetData()
        {
            MyDataReader reader;

            string centralData = ReportParams.Pref + DBManager.sDataAliasRest;

            string whereServ = GetWhereServ();

            bool listLc = GetSelectedKvars();

            #region выборка в temp таблицу

            string sql = " SELECT bd_kernel as pref " +
                         " FROM " + ReportParams.Pref + DBManager.sKernelAliasRest + "s_point " +
                         " WHERE nzp_wp > 1 " + GetWhereWp();
            ExecRead(out reader, sql);

            while (reader.Read())
            {
                if (reader["pref"] != null)
                {
                    string pref = reader["pref"].ToString().Trim();
                    string prefData = pref + DBManager.sDataAliasRest;
                    sql = " INSERT INTO t_kvdom_71_5_3(nzp_kvar, num_ls, nzp_dom) " +
                          " SELECT nzp_kvar, num_ls, nzp_dom " +
                          " FROM " + prefData + "kvar " +
                          " WHERE nzp_kvar > 1 " +
                          (listLc ? " AND nzp_kvar IN (SELECT nzp_kvar FROM selected_kvars) " : string.Empty);
                    ExecSQL(sql);
                    
                    for (int i = YearS * 12 + MonthS; i < YearPo * 12 + MonthPo + 1; i++)
                    {
                        int mo = i % 12;
                        int ye = mo == 0 ? (i / 12) - 1 : (i / 12);
                        if (mo == 0) mo = 12;

                        string sumInsaldo = ((mo == MonthS) & (ye == YearS)) ? "sum_insaldo" : "0";
                        string sumOutsaldo = ((mo == MonthPo) & (ye == YearPo)) ? "sum_outsaldo" : "0";
                    string chargeYY = pref + "_charge_" + (ye - 2000).ToString("00") + DBManager.tableDelimiter +
                                      "charge_" + mo.ToString("00");
                    string perekidka = pref + "_charge_" + (ye - 2000).ToString("00") + DBManager.tableDelimiter +
                                       "perekidka";
                    string fromSupplier = pref + "_charge_" + (ye - 2000).ToString("00") + DBManager.tableDelimiter +
                                          "from_supplier "; 

                    ExecSQL(DBManager.sUpdStat + " t_kvdom_71_5_3");

                    if (TempTableInWebCashe(chargeYY))
                    {
                        sql =
                            " INSERT INTO t_report_71_5_3(month_,nzp_dom, nzp_serv, nzp_kvar, sum_insaldo, sum_tarif, sum_lgota, sum_real, reval, " +
                            " money_to, money_from, sum_outsaldo_c,sum_outsaldo_d,sum_outsaldo) " +
                            " SELECT "+mo+", k.nzp_dom,nzp_serv, k.nzp_kvar, " +
                            " SUM(" + sumInsaldo + ") AS sum_insaldo, " +
                            " SUM(a.sum_tarif) AS sum_tarif, " +
                            " SUM(a.sum_lgota) AS sum_lgota, " +
                            " SUM(a.sum_real) AS sum_real, " +
                            " SUM(a.reval) AS reval, " +
                            " SUM(money_to) AS money_to, " +
                            " SUM(money_from) AS money_from, " +
                            " SUM(CASE WHEN a.sum_outsaldo > 0 THEN  " + sumOutsaldo + " ELSE 0 END) AS sum_outsaldo_c, " +
                            " SUM(CASE WHEN a.sum_outsaldo < 0 THEN  " + sumOutsaldo + " ELSE 0 END) AS sum_outsaldo_d, " +
                            " SUM( " + sumOutsaldo + ") AS sum_outsaldo " +
                            " FROM " + chargeYY + " a, t_kvdom_71_5_3 k " +
                            " WHERE a.dat_charge IS NULL " +
                            " AND nzp_supp > 0 " +
                            " AND a.nzp_kvar = k.nzp_kvar " +
                            " AND a.nzp_serv > 1 " + whereServ + GetWhereSupp("a.nzp_supp") +
                            " GROUP BY 1,2,3,4";
                        ExecSQL(sql);

                        ExecSQL(DBManager.sUpdStat + " t_report_71_5_3");

                        sql = " UPDATE t_report_71_5_3 " +
                              " SET real_charge = ( SELECT SUM(sum_rcl)" +
                              " FROM " + perekidka + " p INNER JOIN t_kvdom_71_5_3 k ON k.nzp_kvar = p.nzp_kvar " +
                              " WHERE p.type_rcl not  in (100,20) " +
                              " AND p.nzp_kvar > 0 " +
                              " AND p.nzp_serv = t_report_71_5_3.nzp_serv " +
                              " AND p.nzp_kvar = t_report_71_5_3.nzp_kvar " + 
                              " AND k.nzp_dom = t_report_71_5_3.nzp_dom " + whereServ + GetWhereSupp("nzp_supp") +
                              " AND p.month_ = " + mo + ") " +
                              " WHERE month_=" + mo;
                        ExecSQL(sql);

                        sql = " UPDATE t_report_71_5_3 " +
                              " SET real_insaldo = ( SELECT SUM(sum_rcl)" +
                              " FROM " + perekidka + " p INNER JOIN t_kvdom_71_5_3 k ON k.nzp_kvar = p.nzp_kvar " +
                              " WHERE p.type_rcl in (100,20) " +
                              " AND p.nzp_serv = t_report_71_5_3.nzp_serv " +
                              " AND p.nzp_kvar = t_report_71_5_3.nzp_kvar " + 
                              " AND k.nzp_dom = t_report_71_5_3.nzp_dom " + whereServ + GetWhereSupp("nzp_supp") +
                              " AND p.month_ = " + mo+ ") " +
                              " WHERE month_=" + mo;
                        ExecSQL(sql);

                        sql = " INSERT INTO t_report_71_5_3(nzp_dom, money_from, money_supp)" +
                              " SELECT nzp_dom , -SUM(sum_prih) AS money_from , SUM(sum_prih) AS money_supp " +
                              " FROM " + fromSupplier + " a INNER JOIN t_kvdom_71_5_3 k ON k.num_ls = a.num_ls " +
                              " WHERE a.kod_sum in (49, 50, 35) " + whereServ + GetWhereSupp("a.nzp_supp") +
                              " AND dat_uchet >= '01." + mo + "." + ye + "' " +
                              " AND dat_uchet <= '" + DateTime.DaysInMonth(ye, mo) + "." + mo + "." + ye + "'" +
                              " GROUP BY 1";
                        ExecSQL(sql);
                    }
                    
                }   
                    ExecSQL(" DELETE FROM t_kvdom_71_5_3 ");
                    sql =
                        " INSERT INTO t_all_71_5_3 (nzp_dom,rajon, ulica, ulicareg, idom, ndom, nkor, sum_insaldo, sum_tarif, sum_lgota, sum_real, reval,real_charge,real_insaldo, " +
                        " money_to, money_from, money_supp, sum_outsaldo_c,sum_outsaldo_d,sum_outsaldo,dolg,procent )" +
                        " SELECT " +
                        " d.nzp_dom," +
                        " CASE WHEN rajon='-' THEN town ELSE TRIM(town)||', '||TRIM(rajon) END AS rajon, " +
                        " u.ulica, u.ulicareg, idom, d.ndom, TRIM(CASE WHEN d.nkor='-' THEN '' ELSE  'к. ' ||''||d.nkor END) AS nkor, " +
                        " SUM(sum_insaldo) AS sum_insaldo, " +
                        " SUM(sum_tarif) AS sum_tarif, " +
                        " SUM(sum_lgota) AS sum_lgota, " +
                        " SUM(sum_real) AS sum_real, " +
                        " SUM(reval) AS reval, " +
                        " SUM(real_charge) AS real_charge, " +
                        " SUM(real_insaldo) AS real_insaldo, " +
                        " SUM(money_to) AS money_to, " +
                        " SUM(money_from) AS money_from, " +
                        " SUM(money_supp) AS money_supp, " +
                        " SUM(sum_outsaldo_c) AS sum_outsaldo_c, " +
                        " SUM(sum_outsaldo_d) AS sum_outsaldo_d, " +
                        " SUM(sum_outsaldo) AS sum_outsaldo, " +
                        " SUM(sum_outsaldo_c - sum_real) AS dolg, " +
                        " SUM(CASE WHEN sum_real <> 0 " +
                        " THEN (((sum_outsaldo_c - sum_real) / sum_real) * 100)  " +
                        " ELSE 0 END) AS procent " +
                        " FROM t_report_71_5_3 t, " +
                        centralData + "dom d, " +
                        centralData + "s_rajon sr, " +
                        centralData + "s_town st, " +
                        centralData + "s_ulica u" + 
                        " WHERE u.nzp_raj = sr.nzp_raj " +
                        " AND sr.nzp_town = st.nzp_town" +
                        " AND t.nzp_dom = d.nzp_dom " +
                        " AND u.nzp_ul = d.nzp_ul " +
                        " GROUP BY 1,2,3,4,5,6 ";
                    ExecSQL(sql);

                    ExecSQL(" DELETE FROM t_report_71_5_3 ");           
                }
            }

            reader.Close();
            #endregion

            sql = " SELECT * " +
                  " FROM t_all_71_5_3 c " +
                  " ORDER BY rajon, ulica, ulicareg, idom, ndom, nkor ";

            DataTable dt = ExecSQLToTable(sql);
            dt.TableName = "Q_master";
            if (dt.Rows.Count > 65000 && ReportParams.ExportFormat == ExportFormat.Excel2007)
            {
                var dtr = dt.Rows.Cast<DataRow>().Skip(65000).ToArray();
                dtr.ForEach(dt.Rows.Remove);
            }
            var ds = new DataSet();
            ds.Tables.Add(dt);
            return ds;
        }

        /// <summary>
        /// Получить условия органичения по поставщикам
        /// </summary>
        /// <returns></returns>
        private string GetWhereSupp(string fieldPref)
        {
            string whereSupp = String.Empty;
            if (BankSupplier != null && BankSupplier.Suppliers != null)
            {

                string supp = string.Empty;
                supp = BankSupplier.Suppliers.Aggregate(supp, (current, nzpSupp) => current + (nzpSupp + ","));
                whereSupp += " and nzp_payer_supp in (" + supp.TrimEnd(',') + ")";
            }

            if (BankSupplier != null && BankSupplier.Principals != null)
            {

                string supp = string.Empty;
                supp = BankSupplier.Principals.Aggregate(supp, (current, nzpSupp) => current + (nzpSupp + ","));
                whereSupp += " and nzp_payer_princip in (" + supp.TrimEnd(',') + ")";
            }

            if (BankSupplier != null && BankSupplier.Agents != null)
            {

                string supp = string.Empty;
                supp = BankSupplier.Agents.Aggregate(supp, (current, nzpSupp) => current + (nzpSupp + ","));
                whereSupp += " and nzp_payer_agent in (" + supp.TrimEnd(',') + ")";
            }

            string oldsupp = ReportParams.GetRolesCondition(Constants.role_sql_supp);

            whereSupp = whereSupp.TrimEnd(',');


            if (!String.IsNullOrEmpty(whereSupp) || !String.IsNullOrEmpty(oldsupp))
            {
                if (!String.IsNullOrEmpty(oldsupp))
                    whereSupp += " AND nzp_supp in (" + oldsupp + ")";

                //Поставщики
                if (String.IsNullOrEmpty(SupplierHeader))
                {
                    SupplierHeader = string.Empty;
                    string sql = " SELECT name_supp " + 
                                 " FROM " + ReportParams.Pref + DBManager.sKernelAliasRest + "supplier " +
                                 " WHERE nzp_supp > 0 " + whereSupp;
                    DataTable supp = ExecSQLToTable(sql);
                    foreach (DataRow dr in supp.Rows)
                    {
                        SupplierHeader += "(" + dr["name_supp"].ToString().Trim() + "), ";
                    }
                    SupplierHeader = SupplierHeader.TrimEnd(',', ' ');
                }
            }
            return " and " + fieldPref + " in (select nzp_supp from " +
                   ReportParams.Pref + DBManager.sKernelAliasRest + "supplier " +
                   " where nzp_supp>0 " + whereSupp + ")";
        }

        /// <summary>
        /// Получить условия органичения по услугам
        /// </summary>
        /// <returns></returns>
        private string GetWhereServ()
        {
            string whereServ = String.Empty;
            whereServ = Services != null ? Services.Aggregate(whereServ, (current, nzpServ) => current + (nzpServ + ",")) : ReportParams.GetRolesCondition(Constants.role_sql_serv);
            whereServ = whereServ.TrimEnd(',');
            whereServ = !String.IsNullOrEmpty(whereServ) ? " AND nzp_serv in (" + whereServ + ")" : String.Empty;
            if (!String.IsNullOrEmpty(whereServ))
            {
                if (String.IsNullOrEmpty(ServiceHeader))
                {
                    ServiceHeader = string.Empty;
                    string sql = " SELECT service from " + ReportParams.Pref + DBManager.sKernelAliasRest +
                                 "services  WHERE nzp_serv > 0 " + whereServ;
                    DataTable serv = ExecSQLToTable(sql);
                    foreach (DataRow dr in serv.Rows)
                    {
                        ServiceHeader += dr["service"].ToString().Trim() + ", ";
                    }
                    ServiceHeader = ServiceHeader.TrimEnd(',', ' ');
                }

            }
            return whereServ;
        }

        /// <summary>
        /// Выборка списка квартир в картотеке
        ///  </summary>
        /// <returns></returns>
        private bool GetSelectedKvars()
        {
            if (ReportParams.CurrentReportKind == ReportKind.ListLC)
            {
                using (IDbConnection connWeb = DBManager.GetConnection(Constants.cons_Webdata))
                {
                    if (!DBManager.OpenDb(connWeb, true).result) return false;

                    string tSpls = DBManager.GetFullBaseName(connWeb) + DBManager.tableDelimiter +
                                   "t" + ReportParams.User.nzp_user + "_spls";
                    if (TempTableInWebCashe(tSpls))
                    {
                        string sql = " insert into selected_kvars (nzp_kvar, nzp_dom) " +
                                     " select nzp_kvar, nzp_dom from " + tSpls;
                        ExecSQL(sql);
                        ExecSQL("create index ix_sel_kvar_09 on selected_kvars(nzp_kvar)");
                        ExecSQL(DBManager.sUpdStat + " selected_kvars");
                        return true;
                    }
                }
            }
            return false;
        }

        private string GetWhereWp()
        {
            string whereWp = String.Empty;
            if (BankSupplier != null &&  BankSupplier.Banks != null)
            {
                whereWp = BankSupplier.Banks.Aggregate(whereWp, (current, nzpWp) => current + (nzpWp + ","));
            }
            else
            {
                whereWp = ReportParams.GetRolesCondition(Constants.role_sql_wp);
            }
            whereWp = whereWp.TrimEnd(',');
            whereWp = !String.IsNullOrEmpty(whereWp) ? " AND nzp_wp in (" + whereWp + ")" : String.Empty;
            if (!string.IsNullOrEmpty(whereWp))
            {
                TerritoryHeader = string.Empty;
                string sql = " SELECT point FROM " + ReportParams.Pref + DBManager.sKernelAliasRest + "s_point WHERE nzp_wp > 0 " + whereWp;
                DataTable terrTable = ExecSQLToTable(sql);
                foreach (DataRow row in terrTable.Rows)
                {
                    TerritoryHeader += row["point"].ToString().Trim() + ", ";
                }
                TerritoryHeader = TerritoryHeader.TrimEnd(',', ' ');
            }
            return whereWp;
        }

        protected override void CreateTempTable()
        {

            string sql = " CREATE TEMP TABLE t_report_71_5_3( " +
                         " month_ INTEGER, " +
                         " nzp_dom INTEGER, " + //Уникальный код дома
                         " nzp_serv INTEGER, " + //Уникальный код дома
                         " nzp_kvar INTEGER, " + //Уникальный код дома
                         " adr CHARACTER(100) DEFAULT '', " + //Уникальный код дома
                         " sum_insaldo " + DBManager.sDecimalType + "(14,2), " + //Вхлдящее сальдо
                         " sum_tarif " + DBManager.sDecimalType + "(14,2), " + //Начислено по тарифу
                         " sum_lgota " + DBManager.sDecimalType + "(14,2), " + //Скидка по льготе
                         " sum_real " + DBManager.sDecimalType + "(14,2), " + //Начислено с учётом льгот
                         " reval " + DBManager.sDecimalType + "(14,2), " + //Перерасчёты
                         " real_charge " + DBManager.sDecimalType + "(14,2), " + //Корректировка начислений
                         " real_insaldo " + DBManager.sDecimalType + "(14,2), " + //Корректировка входящего сальдо
                         " money_to " + DBManager.sDecimalType + "(14,2), " + //Оплата через расчетный счет ЕРЦ
                         " money_supp " + DBManager.sDecimalType + "(14,2), " + //Оплата напрямую поставщикам
                         " money_from " + DBManager.sDecimalType + "(14,2), " + //Оплата предыдущих биллинговых систем
                         " sum_outsaldo_c " + DBManager.sDecimalType + "(14,2), " +
                         //Положительная часть исходящего сальдо
                         " sum_outsaldo_d " + DBManager.sDecimalType + "(14,2), " +
                         //Отрицательная часть исходящего сальдо
                         " sum_outsaldo " + DBManager.sDecimalType + "(14,2), " + //Исходящее сальдо
                         " dolg " + DBManager.sDecimalType + "(14,2) DEFAULT 0, " + //Задолжность
                         " procent " + DBManager.sDecimalType + "(14,2) DEFAULT 0) " + DBManager.sUnlogTempTable; //Процент
            ExecSQL(sql);
            ExecSQL(" CREATE INDEX ix_t_report_71_5_3 on t_report_71_5_3(nzp_dom) ");

            sql = " CREATE TEMP TABLE t_all_71_5_3( " +
                  " rajon CHAR(200)," +
                  " ulica CHAR(200)," +
                  " ulicareg CHAR(10)," +
                  " ndom CHAR(10)," +
                  " nkor CHAR(10)," +
                  " idom INTEGER, " + //Уникальный код дома
                  " nzp_dom INTEGER, " + //Уникальный код дома
                  " sum_insaldo " + DBManager.sDecimalType + "(14,2), " + //Вхлдящее сальдо
                  " sum_tarif " + DBManager.sDecimalType + "(14,2), " + //Начислено по тарифу
                  " sum_lgota " + DBManager.sDecimalType + "(14,2), " + //Скидка по льготе
                  " sum_real " + DBManager.sDecimalType + "(14,2), " + //Начислено с учётом льгот
                  " reval " + DBManager.sDecimalType + "(14,2), " + //Перерасчёты
                  " real_charge " + DBManager.sDecimalType + "(14,2), " + //Корректировка начислений
                  " real_insaldo " + DBManager.sDecimalType + "(14,2), " + //Корректировка входящего сальдо
                  " money_to " + DBManager.sDecimalType + "(14,2), " + //Оплата через расчетный счет ЕРЦ
                  " money_supp " + DBManager.sDecimalType + "(14,2), " + //Оплата напрямую поставщикам
                  " money_from " + DBManager.sDecimalType + "(14,2), " + //Оплата предыдущих биллинговых систем
                  " sum_outsaldo_c " + DBManager.sDecimalType + "(14,2), " + //Положительная часть исходящего сальдо
                  " sum_outsaldo_d " + DBManager.sDecimalType + "(14,2), " + //Отрицательная часть исходящего сальдо
                  " sum_outsaldo " + DBManager.sDecimalType + "(14,2), " + //Исходящее сальдо
                  " dolg " + DBManager.sDecimalType + "(14,2) DEFAULT 0, " + //Задолжность
                  " procent " + DBManager.sDecimalType + "(14,2) DEFAULT 0) " + DBManager.sUnlogTempTable; //Процент
            ExecSQL(sql);
            ExecSQL(" CREATE INDEX ix_t_all_71_5_3__1 on t_all_71_5_3(nzp_dom) ");

            sql = " CREATE TEMP TABLE t_kvdom_71_5_3(" +
                  " nzp_kvar INTEGER, " +
                  " num_ls INTEGER, " +
                  " nzp_dom INTEGER) " + DBManager.sUnlogTempTable;
            ExecSQL(sql);

            ExecSQL(" CREATE INDEX ix_t_kvdom_71_5_3__1 on t_kvdom_71_5_3(nzp_kvar) ");
            ExecSQL(" CREATE INDEX ix_t_kvdom_71_5_3__2 on t_kvdom_71_5_3(nzp_dom) ");

            if (ReportParams.CurrentReportKind == ReportKind.ListLC)
            {
                sql = " CREATE TEMP TABLE selected_kvars(" +
                      " nzp_kvar INTEGER," +
                      " nzp_dom INTEGER) " + DBManager.sUnlogTempTable;
                ExecSQL(sql);
            }
        }

        protected override void DropTempTable()
        {
            ExecSQL("DROP TABLE t_report_71_5_3");
            ExecSQL("DROP TABLE t_all_71_5_3");
            ExecSQL("DROP TABLE t_kvdom_71_5_3");
            if (ReportParams.CurrentReportKind == ReportKind.ListLC)
                ExecSQL(" drop table selected_kvars ", true);
        }
    }
}
