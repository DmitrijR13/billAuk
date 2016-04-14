using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Bars.KP50.Report.Base;
using Bars.KP50.Utils;
using Newtonsoft.Json;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;
using Bars.KP50.Report.Tula.Properties;

namespace Bars.KP50.Report.Tula.Reports
{
  public  class Report7105020 : BaseSqlReport
    {
        public override string Name
        {
            get { return "71.5.20 - Сальдовая ведомость по лицевым счетам"; }
        }

        public override string Description
        {
            get { return "5.20 Сальдовая ведомость по лицевым счетам (с квартиросъемщиками)"; }
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
            get { return Resources.Report_71_5_20; }
        }

        public override IList<ReportKind> ReportKinds
        {
            get { return new List<ReportKind> { ReportKind.Base, ReportKind.ListLC }; }
        }


        /// <summary>Заголовок отчета</summary>
        protected int ReportTitle { get; set; }

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

        /// <summary>Список услуг в заголовке</summary>
        protected string ServiceHeader { get; set; }

        /// <summary>Список Поставщиков в заголовке</summary>
        protected string SupplierHeader { get; set; }

        /// <summary>Заголовок территории</summary>
        protected string TerritoryHeader { get; set; }

        /// <summary>Поставщики, Агенты, Принципалы  </summary>
        protected BankSupplierParameterValue BankSupplier { get; set; }

        private int _typeAdres;

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
                new ServiceParameter(),
                new ComboBoxParameter
                {
                    Code = "ShowTown",
                    Name = "Состав адреса",
                    Value = "1",
                    StoreData = new List<object>
                    {
                        new {Id = "1", Name = "Район, населенный пункт, улица"},
                        new {Id = "2", Name = "Населенный пункт, улица"},
                        new {Id = "3", Name = "Улица"},
                    }
                },
            };
        }

        public override DataSet GetData()
        {
            #region Выборка по локальным банкам

            string centralData = ReportParams.Pref + DBManager.sDataAliasRest;
            string whereServ = GetWhereServ();
            bool listLc = GetSelectedKvars();

            MyDataReader reader;

            string sql = " SELECT bd_kernel AS pref " +
                         " FROM " + ReportParams.Pref + DBManager.sKernelAliasRest + "s_point " +
                         " WHERE nzp_wp > 1 " + GetWhereWp();

            ExecRead(out reader, sql);
            while (reader.Read())
            {
                string pref = reader["pref"].ToStr().Trim();
                //
                for (int i = YearS*12 + MonthS; i < YearPo*12 + MonthPo + 1; i++)
                {
                    int mo = i%12;
                    int ye = mo == 0 ? (i/12) - 1 : (i/12);
                    if (mo == 0) mo = 12;
                    string chargeYY = pref + "_charge_" + (ye - 2000).ToString("00") + DBManager.tableDelimiter +
                                      "charge_" + mo.ToString("00");
                    string perekidka = pref + "_charge_" + (ye - 2000).ToString("00") + DBManager.tableDelimiter +
                                       "perekidka";
                    string fromSupplier = pref + "_charge_" + (ye - 2000).ToString("00") + DBManager.tableDelimiter +
                                          "from_supplier ";
                    string sumInsaldo = ((mo == MonthS) & (ye == YearS)) ? "sum_insaldo" : "0";
                    string sumOutsaldo = ((mo == MonthPo) & (ye == YearPo)) ? "sum_outsaldo" : "0";

                    sql =
                        " INSERT INTO t_report_71_5_20 (nzp_kvar, num_ls, sum_insaldo_k, sum_insaldo_d, sum_insaldo, sum_real, reval, " +
                        " money_to, money_from, money_del, sum_outsaldo_k, sum_outsaldo_d, sum_outsaldo ) " +
                        " SELECT k.nzp_kvar, k.num_ls,  " +
                        " SUM(CASE WHEN c.sum_insaldo < 0 THEN " + sumInsaldo + " ELSE 0 END) AS sum_insaldo_k, " +
                        " SUM(CASE WHEN c.sum_insaldo > 0 THEN " + sumInsaldo + " ELSE 0 END) AS sum_insaldo_d, " +
                        " SUM(" + sumInsaldo + ") AS sum_insaldo, " +
                        " SUM(sum_real) AS sum_real, " +
                        " SUM(reval) AS reval, " +
                        " SUM(money_to) AS money_to, " +
                        " SUM(money_from) AS money_from, " +
                        " SUM(money_del) AS money_del, " +
                        " SUM(CASE WHEN c.sum_outsaldo < 0 THEN " + sumOutsaldo + " ELSE 0 END) AS sum_outsaldo_k, " +
                        " SUM(CASE WHEN c.sum_outsaldo > 0 THEN " + sumOutsaldo + " ELSE 0 END) AS sum_outsaldo_d, " +
                        " SUM(" + sumOutsaldo + ") AS sum_outsaldo " +
                        " FROM " + (listLc ? " selected_kvars_5_20 k, " : centralData + "kvar k, ") +
                        chargeYY + " c " +
                        " WHERE c.dat_charge IS NULL " +
                        " AND c.nzp_serv > 1 " +
                        " AND k.num_ls = c.num_ls " + whereServ + GetWhereSupp("c.nzp_supp") +
                        " GROUP BY 1,2 ";
                    ExecSQL(sql);

                    ExecSQL(DBManager.sUpdStat + " t_report_71_5_20");

                    sql = " UPDATE t_report_71_5_20 " +
                          " SET real_charge = ( SELECT SUM(sum_rcl)" +
                          " FROM " + perekidka + " p " +
                          " WHERE p.type_rcl not in (100,20) " +
                          " AND p.nzp_kvar = t_report_71_5_20.nzp_kvar " + whereServ + GetWhereSupp("nzp_supp") +
                          " AND p.month_ = " + mo + ") ";
                    ExecSQL(sql);

                    sql = " UPDATE t_report_71_5_20 " +
                          " SET real_insaldo = ( SELECT SUM(sum_rcl)" +
                          " FROM " + perekidka + " p " +
                          " WHERE p.type_rcl in (100,20) " +
                          " AND p.nzp_kvar = t_report_71_5_20.nzp_kvar " + whereServ + GetWhereSupp("nzp_supp") +
                          " AND p.month_ = " + mo + ") ";
                    ExecSQL(sql);

                    sql = " INSERT INTO t_report_71_5_20(num_ls, money_from, money_supp)" +
                          " SELECT num_ls , -SUM(sum_prih) AS money_from , SUM(sum_prih) AS money_supp " +
                          " FROM " + fromSupplier + " a " +
                          " WHERE a.kod_sum in (49, 50, 35) " + whereServ + GetWhereSupp("a.nzp_supp") +
                          " AND num_ls IN ( SELECT nzp_kvar FROM " +
                          (listLc ? " selected_kvars_5_20 k " : centralData + "kvar k ") + " ) " +
                          " AND dat_uchet >= '01." + mo + "." + ye + "' " +
                          " AND dat_uchet <= '" + DateTime.DaysInMonth(ye, mo) + "." + mo + "." + ye + "'" +
                          " GROUP BY 1";
                    ExecSQL(sql);

                    sql = " INSERT INTO t_all_71_5_20 " +
                          " SELECT * FROM t_report_71_5_20 ";
                    ExecSQL(sql);

                    ExecSQL(" DELETE FROM t_report_71_5_20 ");
                }
                //

            }

            reader.Close();

            #endregion

            #region Выборка на экран

            string adres;

            switch (_typeAdres)
            {
                case 1:
                    adres = "(CASE WHEN rajon='-' " +
                            " THEN town ELSE TRIM(town)||','||TRIM(rajon) END)||', '" +
                            "||TRIM(" + DBManager.sNvlWord + "(ulicareg,''))||' '||TRIM(ulica)";
                    break;
                case 2:
                    adres = "(CASE WHEN rajon='-' " +
                            " THEN TRIM(town) ELSE TRIM(rajon) END)||', '" +
                            "||TRIM(" + DBManager.sNvlWord + "(ulicareg,''))||' '||TRIM(ulica)";
                    break;
                case 3:
                    adres = "TRIM(" + DBManager.sNvlWord + "(ulicareg,''))||' '||TRIM(ulica)";
                    break;
                default: adres = "TRIM(" + DBManager.sNvlWord + "(ulicareg,''))||' '||TRIM(ulica)";
                    break;
            }
            

            sql =
                " SELECT a.num_ls, " + adres + " AS ulica, " +
                       " idom, ndom, (CASE WHEN nkor ='-' THEN '' ELSE nkor END) AS nkor, " +
                       " ikvar, (CASE WHEN (nkvar<>'0' and nkvar<>'-') THEN 'Кв.'||nkvar END) AS nkvar, fio, " +
                       " SUM(sum_insaldo_k) AS sum_insaldo_k, SUM(sum_insaldo_d) AS sum_insaldo_d, " +
                       "SUM(sum_insaldo) AS sum_insaldo, SUM(sum_real) AS sum_real, SUM(reval) AS reval, " +
                       " SUM(real_charge) AS real_charge, SUM(real_insaldo) AS real_insaldo, SUM(money_to) AS money_to," +
                       " SUM(money_supp) AS money_supp, SUM(money_from) AS money_from, SUM(money_del) AS money_del," +
                       " SUM(sum_outsaldo_k) AS sum_outsaldo_k, SUM(sum_outsaldo_d) AS sum_outsaldo_d, SUM(sum_outsaldo) AS sum_outsaldo " +
                " FROM t_all_71_5_20 a, " +
                        centralData + "kvar k, " +
                        centralData + "dom d, " +
                        centralData + "s_ulica s, " +
                        centralData + "s_rajon sr, " +
                        centralData + "s_town st " +
                " WHERE a.num_ls = k.num_ls " +
                  " AND k.nzp_dom = d.nzp_dom" +
                  " AND d.nzp_ul = s.nzp_ul" +
                  " AND s.nzp_raj = sr.nzp_raj" +
                  " AND sr.nzp_town = st.nzp_town" +
                " GROUP BY 1,2,3,4,5,6,7,8 "+
                " ORDER BY 2,3,4,5,6,7,1 ";
            DataTable dt = ExecSQLToTable(sql);
            dt.TableName = "Q_master";
            #endregion

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
                        string sql = " insert into selected_kvars_5_20 (num_ls, nzp_kvar) " +
                                     " select num_ls, nzp_kvar from " + tSpls;
                        ExecSQL(sql);
                        ExecSQL("create index ix_tmpsk_ls_01 on selected_kvars_5_20(nzp_kvar) ");
                        ExecSQL(DBManager.sUpdStat + " selected_kvars_5_20 ");
                        return true;
                    }
                }
            }
            return false;
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
                    string sql = " SELECT name_supp from " +
                                 ReportParams.Pref + DBManager.sKernelAliasRest + "supplier " +
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

        private string GetWhereWp()
        {
            string whereWp = String.Empty;
            if (BankSupplier !=null && BankSupplier.Banks != null)
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

        protected override void PrepareReport(FastReport.Report report)
        {
            var months = new[] {"","Январь","Февраль",
                 "Март","Апрель","Май","Июнь","Июль","Август","Сентябрь",
                 "Октябрь","Ноябрь","Декабрь"};
            report.SetParameterValue("month", months[MonthS]);
            report.SetParameterValue("year", YearS);
            if ((MonthS == MonthPo) & (YearS == YearPo))
            {
                report.SetParameterValue("period_month", months[MonthS] + " " + YearS);
            }
            else
            {
                report.SetParameterValue("period_month", "период с " + months[MonthS] + " " + YearS +
                                                         "г. по " + months[MonthPo] + " " + YearPo);

            }

            report.SetParameterValue("printDate", DateTime.Now.ToLongDateString());
            report.SetParameterValue("printTime", DateTime.Now.ToLongTimeString());

            string headerParam = !string.IsNullOrEmpty(TerritoryHeader) ? "Территория: " + TerritoryHeader + "\n" : string.Empty;
            headerParam += !string.IsNullOrEmpty(SupplierHeader) ? "Поставщики: " + SupplierHeader + "\n" : string.Empty;
            headerParam += !string.IsNullOrEmpty(ServiceHeader) ? "Услуги: " + ServiceHeader + "\n" : string.Empty;
            headerParam += ReportParams.CurrentReportKind == ReportKind.ListLC ? "По списку ЛС  " : string.Empty;
            headerParam = headerParam.TrimEnd('\n');
            report.SetParameterValue("headerParam", headerParam);
        }

        protected override void PrepareParams()
        {
            //using (var sw = new StreamWriter(@"D:\1.txt")) sw.WriteLine("Begin!");
            //System.Threading.Thread.Sleep(new TimeSpan(0, 15, 0));
            Services = UserParamValues["Services"].GetValue<List<int>>();

            MonthS = UserParamValues["Month"].GetValue<int>();
            YearS = UserParamValues["Year"].Value.To<int>();
            MonthPo = UserParamValues["Month1"].GetValue<int>();
            YearPo = UserParamValues["Year1"].Value.To<int>();
            _typeAdres = UserParamValues["ShowTown"].GetValue<int>();

            BankSupplier = JsonConvert.DeserializeObject<BankSupplierParameterValue>(UserParamValues["BankSupplier"].Value.ToString());
        }

        protected override void CreateTempTable()
        {
            string sql = " CREATE TEMP TABLE t_report_71_5_20 (  " +
                               " nzp_kvar INTEGER DEFAULT 0, " +
                               " num_ls INTEGER DEFAULT 0, " +
                               " sum_insaldo_k " + DBManager.sDecimalType + "(14,2), " +
                               " sum_insaldo_d " + DBManager.sDecimalType + "(14,2), " +
                               " sum_insaldo " + DBManager.sDecimalType + "(14,2), " +
                               " sum_real " + DBManager.sDecimalType + "(14,2), " +
                               " reval " + DBManager.sDecimalType + "(14,2), " +
                               " real_charge " + DBManager.sDecimalType + "(14,2), " +
                               " real_insaldo " + DBManager.sDecimalType + "(14,2), " +
                               " money_to " + DBManager.sDecimalType + "(14,2), " +
                               " money_supp " + DBManager.sDecimalType + "(14,2), " +
                               " money_from " + DBManager.sDecimalType + "(14,2), " +
                               " money_del " + DBManager.sDecimalType + "(14,2), " +
                               " sum_outsaldo_k " + DBManager.sDecimalType + "(14,2), " +
                               " sum_outsaldo_d " + DBManager.sDecimalType + "(14,2), " +
                               " sum_outsaldo " + DBManager.sDecimalType + "(14,2)) " + DBManager.sUnlogTempTable;
            ExecSQL(sql);

            ExecSQL(" CREATE INDEX ix_t_report_71_5_20 on t_report_71_5_20(nzp_kvar) ");

            sql = " CREATE TEMP TABLE t_all_71_5_20( " +
                          " nzp_kvar INTEGER DEFAULT 0, " +
                          " num_ls INTEGER DEFAULT 0, " +
                          " sum_insaldo_k " + DBManager.sDecimalType + "(14,2), " +
                          " sum_insaldo_d " + DBManager.sDecimalType + "(14,2), " +
                          " sum_insaldo " + DBManager.sDecimalType + "(14,2), " +
                          " sum_real " + DBManager.sDecimalType + "(14,2), " +
                          " reval " + DBManager.sDecimalType + "(14,2), " +
                          " real_charge " + DBManager.sDecimalType + "(14,2), " +       
                          " real_insaldo " + DBManager.sDecimalType + "(14,2), " +      
                          " money_to " + DBManager.sDecimalType + "(14,2), " +          
                          " money_supp " + DBManager.sDecimalType + "(14,2), " +        
                          " money_from " + DBManager.sDecimalType + "(14,2), " +
                          " money_del " + DBManager.sDecimalType + "(14,2), " +
                          " sum_outsaldo_k " + DBManager.sDecimalType + "(14,2), " +
                          " sum_outsaldo_d " + DBManager.sDecimalType + "(14,2), " +
                          " sum_outsaldo " + DBManager.sDecimalType + "(14,2)) " + DBManager.sUnlogTempTable; 
            ExecSQL(sql);

            if (ReportParams.CurrentReportKind == ReportKind.ListLC)
            {
                sql = " CREATE TEMP TABLE selected_kvars_5_20(" +
                      " nzp_kvar INTEGER, " +
                      " num_ls INTEGER ) " +
                      DBManager.sUnlogTempTable;
                ExecSQL(sql);
            }
        }

        protected override void DropTempTable()
        {
            ExecSQL(" DROP TABLE t_report_71_5_20 ", true);
            ExecSQL(" DROP TABLE t_all_71_5_20 ", true);
            if (ReportParams.CurrentReportKind == ReportKind.ListLC)
                ExecSQL(" DROP TABLE selected_kvars_5_20 ", true);
        }

    }
}
