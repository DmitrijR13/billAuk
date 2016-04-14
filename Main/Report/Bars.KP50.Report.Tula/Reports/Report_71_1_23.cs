using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Bars.KP50.Report.Base;
using Bars.KP50.Report.Tula.Properties;
using Newtonsoft.Json;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;
using Bars.KP50.Utils;

namespace Bars.KP50.Report.Tula.Reports
{
    class Report710123 : BaseSqlReport
    {
        public override string Name
        {
            get { return "71.1.23 Отчет по всем банкам данных"; }
        }

        public override string Description
        {
            get { return "Отчет по всем банкам данных"; }
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
            get { return Resources.Report_71_1_23; }
        }

        public override IList<ReportKind> ReportKinds
        {
            get { return new List<ReportKind> { ReportKind.Base }; }
        }

        /// <summary>Расчетный месяц</summary>
        protected int MonthS { get; set; }

        /// <summary>Расчетный год</summary>
        protected int YearS { get; set; }


        /// <summary>Расчетный месяц</summary>
        protected int MonthPo { get; set; }

        /// <summary>Расчетный год</summary>
        protected int YearPo { get; set; }

        /// <summary>Заголовок отчета принципалы</summary>
        protected string PrincipalHeader { get; set; }

        /// <summary>Заголовок отчета</summary>
        protected string SupplierHeader { get; set; }

        /// <summary>Заголовок территории</summary>
        protected string TerritoryHeader { get; set; }

        /// <summary>Поставщики, Агенты, Принципалы  </summary>
        protected BankSupplierParameterValue BankSupplier { get; set; }
        /// <summary>Услуги</summary>
        protected List<int> Services { get; set; }

        /// <summary>Список услуг в заголовке</summary>
        protected string ServiceHeader { get; set; }
        
        private List<string> PrefBanks { get; set; }

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

        public override DataSet GetData()
        {        
            string whereServ = GetWhereServ(),
            whereSupp = GetWhereSupp("ch.nzp_supp"),
            whereWp = GetwhereWp();;
            DateTime DatS = new DateTime(YearS, MonthS, 1);
            DateTime DatPo = new DateTime(YearPo, MonthPo, DateTime.DaysInMonth(YearPo, MonthPo));
            string sql;
            foreach (var pref in PrefBanks)
            {
                string kvar = pref + DBManager.sDataAliasRest + "kvar k";
                string dom = pref + DBManager.sDataAliasRest + "dom d";
                string ulica = pref + DBManager.sDataAliasRest + "s_ulica u";  
                
                for (var i = YearS * 12 + MonthS; i < YearPo * 12 + MonthS+1; i++)
                {
                    var year = i / 12;
                    var month = i % 12;
                    if (month == 0)
                    {
                        year--;
                        month = 12;
                    }
                    DateTime datS = new DateTime(year, month, 1);
                    DateTime datPo = new DateTime(year, month, DateTime.DaysInMonth(year, month));
                    string charge = pref + "_charge_" + (year - 2000).ToString("00") + DBManager.tableDelimiter +
                                    "charge_" + month.ToString("00");
                    string perekidka = pref + "_charge_" + (year - 2000).ToString("00") + DBManager.tableDelimiter +
                                       "perekidka";
                    string pack = ReportParams.Pref + "_fin_" + (year - 2000).ToString("00") + DBManager.tableDelimiter +
                                  "pack";
                    string packls = ReportParams.Pref + "_fin_" + (year - 2000).ToString("00") +
                                    DBManager.tableDelimiter +
                                    "pack_ls ";
                    string fnsupp = pref + "_charge_" + (year - 2000).ToString("00") +
                                    DBManager.tableDelimiter + "fn_supplier" + month.ToString("00");
                    string fromsupp = pref + "_charge_" + (year - 2000).ToString("00") +
                                      DBManager.tableDelimiter + "from_supplier ";

                    if (TempTableInWebCashe(pack) && TempTableInWebCashe(packls)&&TempTableInWebCashe(pack) && TempTableInWebCashe(packls))
                    {
                        sql = " INSERT INTO t_svod (bd_kernel, nzp_raj, sum_nach)" +
                              " SELECT '"+pref+"', u.nzp_raj, sum(rsum_tarif)+sum(reval) " +
                              " FROM " + charge + " ch , " + kvar +  ", "+dom +", "+ulica+
                              " WHERE dat_charge IS NULL AND nzp_serv>1 AND ch.nzp_kvar>1 " +
                              " AND ch.num_ls=k.num_ls AND k.nzp_dom=d.nzp_dom AND d.nzp_ul=u.nzp_ul  " +
                              whereServ + whereSupp + 
                              " GROUP BY 1,2";
                        ExecSQL(sql);

                        sql = " INSERT INTO t_svod (bd_kernel, nzp_raj, sum_nach)" +
                              " SELECT '" + pref + "', u.nzp_raj, sum(sum_rcl) " +
                              " FROM " + perekidka + " ch , " + kvar + ", " + dom + ", " + ulica +
                              " WHERE ch.num_ls=k.num_ls AND month_=" + month +
                              " AND type_rcl not in (100,20) AND nzp_serv>1 AND ch.nzp_kvar>1 " +
                              " AND k.nzp_dom=d.nzp_dom AND d.nzp_ul=u.nzp_ul  " +
                              whereServ + whereSupp + 
                              " GROUP BY 1,2 ";
                        ExecSQL(sql);

                        sql = " INSERT INTO t_svod (bd_kernel, nzp_raj, nzp_bank, sum_money)" +
                              " SELECT '" + pref + "', u.nzp_raj, nzp_bank, sum(sum_prih) " +
                              " FROM " + pack + " pp," +
                              packls + " pl left outer join  " +
                              fnsupp + " ch  on pl.nzp_pack_ls = ch.nzp_pack_ls, " +
                              kvar + ", " + dom + ", " + ulica +
                              " WHERE   pp.nzp_pack = pl.nzp_pack AND pl.num_ls = k.num_ls AND ch.nzp_serv>1 " +
                              " AND ch.kod_sum in (33) " +
                              " AND k.nzp_dom=d.nzp_dom AND d.nzp_ul=u.nzp_ul  " +
                              //" AND pp.dat_uchet >= '" + datS.ToShortDateString() + "' " +
                              //" AND pp.dat_uchet <= '" + datPo.ToShortDateString() + "'" +
                              whereServ + whereWp + whereSupp +
                              " GROUP BY 1,2,3 ";  
                        ExecSQL(sql);

                        sql = " INSERT INTO t_svod (bd_kernel, nzp_raj, nzp_bank, sum_money)" +
                              " SELECT '" + pref + "', u.nzp_raj, nzp_bank, sum(sum_prih) " +
                              " FROM " + pack + " pp," +
                              packls + " pl left outer join  " +
                              fromsupp + " ch  on pl.nzp_pack_ls = ch.nzp_pack_ls, " +
                              kvar + ", " + dom + ", " + ulica +
                              " WHERE   pp.nzp_pack = pl.nzp_pack AND pl.num_ls = k.num_ls  AND ch.nzp_serv>1 " +
                              " AND ch.kod_sum in (49,50,35) " +
                              " AND k.nzp_dom=d.nzp_dom AND d.nzp_ul=u.nzp_ul  " +
                              " AND pp.dat_uchet >= '" + datS.ToShortDateString() + "' " +
                              " AND pp.dat_uchet <= '" + datPo.ToShortDateString() + "'" +
                              whereServ + whereWp + whereSupp +
                              " GROUP BY 1,2,3 ";
                        ExecSQL(sql);
                    }   
                }
                sql = " INSERT INTO t_count_ls (bd_kernel, nzp_raj, count_ls) " +
                      " SELECT '" + pref + "', u.nzp_raj, count(distinct num_ls) " +
                      " FROM " + kvar + " LEFT OUTER JOIN " +
                      pref + DBManager.sDataAliasRest + "prm_3 p3 ON p3.nzp=k.nzp_kvar " +
                      " AND p3.dat_s <= '" + DatPo + "' " +
                      " AND p3.dat_po >= '" + DatS + "' " +
                      " AND p3.is_actual = 1 " +
                      " AND p3.nzp_prm = 51, " +                    
                      dom + ", " + ulica +
                      " WHERE k.nzp_dom=d.nzp_dom AND d.nzp_ul=u.nzp_ul  " +
                      " AND (p3.val_prm = '1' OR p3.val_prm is null) " +
                      " GROUP BY 1,2 ";
                ExecSQL(sql);    
            }

            sql = " INSERT INTO t_res (bd_kernel, nzp_raj, sum_nach, sum_money)" +
                  " SELECT bd_kernel, nzp_raj, sum(sum_nach), sum(sum_money) " +
                  " FROM t_svod t "  + 
                  " GROUP BY 1,2 ";
            ExecSQL(sql);

            sql = " UPDATE t_res SET sum_money_sber = (SELECT SUM(sum_money) " +
                  " FROM t_svod t, " +
                  ReportParams.Pref + DBManager.sKernelAliasRest + "s_bank sb," +
                  ReportParams.Pref + DBManager.sKernelAliasRest + "s_payer sp" +
                  " WHERE sb.nzp_bank=t.nzp_bank  " +
                  " AND t.nzp_raj= t_res.nzp_raj " +
                  " AND t.bd_kernel= t_res.bd_kernel " +
                  " AND sp.nzp_payer=sb.nzp_payer AND UPPER(sp.payer) LIKE 'СБЕРБАНК%')";
            ExecSQL(sql);

            sql = " UPDATE t_res SET sum_money_post = (SELECT SUM(sum_money) " +
                  " FROM t_svod t,  " +
                  ReportParams.Pref + DBManager.sKernelAliasRest + "s_bank sb," +
                  ReportParams.Pref + DBManager.sKernelAliasRest + "s_payer sp" +
                  " WHERE sb.nzp_bank=t.nzp_bank  "+
                  " AND t.nzp_raj= t_res.nzp_raj "+
                  " AND t.bd_kernel= t_res.bd_kernel " +
                  " AND sp.nzp_payer=sb.nzp_payer  AND UPPER(sp.payer) LIKE 'ПОЧТА%')";
            ExecSQL(sql);

            DataTable d1t = ExecSQLToTable("select * from t_svod ");


            #region Выборка на экран 

            sql =
                " SELECT TRIM(point) as point, CASE WHEN rajon='-' THEN town ELSE TRIM(town)||', '||TRIM(rajon) END AS rajon," +
                " SUM(count_ls) AS count_ls, SUM(sum_nach) AS sum_nach, " +
                " SUM(sum_money) AS sum_money, SUM(sum_money_sber) AS sum_money_sber, SUM(sum_money_post) AS sum_money_post " +
                " FROM t_res t, t_count_ls tl, " +
                ReportParams.Pref + DBManager.sDataAliasRest + "s_rajon sr, " +
                ReportParams.Pref + DBManager.sDataAliasRest + "s_town st, " +
                ReportParams.Pref + DBManager.sKernelAliasRest + "s_point sp " +
                " WHERE   t.nzp_raj=sr.nzp_raj AND sr.nzp_town=st.nzp_town AND t.bd_kernel=sp.bd_kernel " +
                " AND t.nzp_raj=tl.nzp_raj AND t.bd_kernel=tl.bd_kernel " +
                " GROUP BY point, rajon, st.town " +
                " ORDER BY point, rajon ";
            DataTable dt = ExecSQLToTable(sql);

            dt.TableName = "Q_master";
            #endregion

            var ds = new DataSet();
            ds.Tables.Add(dt);

            return ds;
        }

        
        /// <summary>
        /// Получить условия органичения по банкам данных
        /// </summary>
        /// <returns></returns>
        private string GetwhereWp()
        {
            string whereWp = String.Empty;
            if (BankSupplier != null && BankSupplier.Banks != null)
            {
                whereWp = BankSupplier.Banks.Aggregate(whereWp, (current, nzpWp) => current + (nzpWp + ","));
            }
            else
            {
                whereWp = ReportParams.GetRolesCondition(Constants.role_sql_wp);
            }
            whereWp = whereWp.TrimEnd(',');
            string whereWpsql = !String.IsNullOrEmpty(whereWp) ? " AND nzp_wp in (" + whereWp + ")" : String.Empty;
            PrefBanks = new List<string>();
            if (!string.IsNullOrEmpty(whereWpsql))
            {
                TerritoryHeader = String.Empty;
                string sql = " SELECT point,bd_kernel FROM " + ReportParams.Pref + DBManager.sKernelAliasRest + "s_point WHERE nzp_wp > 0 " + whereWpsql;
                DataTable terrTable = ExecSQLToTable(sql);
                foreach (DataRow row in terrTable.Rows)
                {
                    TerritoryHeader += row["point"].ToString().Trim() + ", ";
                    PrefBanks.Add(row["bd_kernel"].ToString().Trim());

                }
                TerritoryHeader = TerritoryHeader.TrimEnd(',', ' ');
            }
            else
            {
                string sql = " SELECT bd_kernel FROM " + ReportParams.Pref + DBManager.sKernelAliasRest + "s_point WHERE nzp_wp > 0 and flag=2";
                DataTable terrTable = ExecSQLToTable(sql);
                foreach (DataRow row in terrTable.Rows)
                {
                    PrefBanks.Add(row["bd_kernel"].ToString().Trim());
                }
            }
            string whereWpRes = !String.IsNullOrEmpty(whereWp) ? "AND pl.num_ls in (SELECT num_ls FROM " + ReportParams.Pref + DBManager.sKernelAliasRest + "s_point s,"
                       + ReportParams.Pref + DBManager.sDataAliasRest + "kvar kv " +
                       "where kv.nzp_wp=s.nzp_wp AND s.nzp_wp in ( " + whereWp + ") )" : String.Empty;
            return whereWpRes;
        }

        protected override void PrepareReport(FastReport.Report report)
        {
            var months = new[] {"","Январь","Февраль",
                 "Март","Апрель","Май","Июнь","Июль","Август","Сентябрь",
                 "Октябрь","Ноябрь","Декабрь"};
            if ((MonthS == MonthPo) & (YearS == YearPo))
            {
                report.SetParameterValue("pPeriod", months[MonthS] + " " + YearS+"г.");
            }
            else
            {
                report.SetParameterValue("pPeriod", "период с " + months[MonthS] + " " + YearS +
                                                         "г. по " + months[MonthPo] + " " + YearPo+"г.");

            }

            string headerParam = !string.IsNullOrEmpty(TerritoryHeader) ? "Территория: " + TerritoryHeader + "\n" : string.Empty;
            headerParam += !string.IsNullOrEmpty(PrincipalHeader) ? "Принципалы: " + PrincipalHeader + "\n" : string.Empty;
            headerParam += !string.IsNullOrEmpty(SupplierHeader) ? "Поставщики: " + SupplierHeader + "\n" : string.Empty;
            headerParam += !string.IsNullOrEmpty(ServiceHeader) ? "Услуги: " + ServiceHeader + "\n" : string.Empty;
            headerParam = headerParam.TrimEnd('\n');
            report.SetParameterValue("pHeader", headerParam);

        }

        protected override void PrepareParams()
        {
            MonthS = UserParamValues["Month"].GetValue<int>();
            YearS = UserParamValues["Year"].Value.To<int>();
            MonthPo = UserParamValues["Month1"].GetValue<int>();
            YearPo = UserParamValues["Year1"].Value.To<int>();
            BankSupplier = JsonConvert.DeserializeObject<BankSupplierParameterValue>(UserParamValues["BankSupplier"].Value.ToString());
            Services = UserParamValues["Services"].GetValue<List<int>>();
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
                //whereSupp = Suppliers.Aggregate(whereSupp, (current, nzpSupp) => current + (nzpSupp + ","));
                whereSupp += " and nzp_payer_supp in (" + supp.TrimEnd(',') + ")";
            }

            if (BankSupplier != null && BankSupplier.Principals != null)
            {

                string supp = string.Empty;
                supp = BankSupplier.Principals.Aggregate(supp, (current, nzpSupp) => current + (nzpSupp + ","));
                //whereSupp = Suppliers.Aggregate(whereSupp, (current, nzpSupp) => current + (nzpSupp + ","));
                whereSupp += " and nzp_payer_princip in (" + supp.TrimEnd(',') + ")";
            }
            if (BankSupplier != null && BankSupplier.Agents != null)
            {

                string supp = string.Empty;
                supp = BankSupplier.Agents.Aggregate(supp, (current, nzpSupp) => current + (nzpSupp + ","));
                //whereSupp = Suppliers.Aggregate(whereSupp, (current, nzpSupp) => current + (nzpSupp + ","));
                whereSupp += " and nzp_payer_agent in (" + supp.TrimEnd(',') + ")";
            }

            string oldsupp = ReportParams.GetRolesCondition(Constants.role_sql_supp);

            whereSupp = whereSupp.TrimEnd(',');


            if (!String.IsNullOrEmpty(whereSupp) || !String.IsNullOrEmpty(oldsupp))
            {
                if (!String.IsNullOrEmpty(oldsupp))
                    whereSupp += " AND nzp_supp in (" + oldsupp + ")";

                //Поставщики
                SupplierHeader = String.Empty;
                string sql = " SELECT name_supp from " +
                             ReportParams.Pref + DBManager.sKernelAliasRest + "supplier " +
                             " WHERE nzp_supp > 0 " + whereSupp;
                DataTable supp = ExecSQLToTable(sql);
                foreach (DataRow dr in supp.Rows)
                {
                    SupplierHeader += "(" + dr["name_supp"].ToString().Trim() + "), ";
                }
                SupplierHeader = SupplierHeader.TrimEnd(',', ' ');

                //Принципалы
                PrincipalHeader = String.Empty;
                string sqlp = " SELECT payer from " +
                             ReportParams.Pref + DBManager.sKernelAliasRest + "supplier s, " +
                             ReportParams.Pref + DBManager.sKernelAliasRest + "s_payer p " +
                             " WHERE nzp_payer > 0  and nzp_payer=nzp_payer_princip " + whereSupp;
                DataTable princip = ExecSQLToTable(sqlp);
                foreach (DataRow dr in princip.Rows)
                {
                    PrincipalHeader += dr["payer"].ToString().Trim() + ", ";
                }
                PrincipalHeader = PrincipalHeader.TrimEnd(',', ' ');

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

        protected override void CreateTempTable()
        {
            string sql = " create temp table t_svod (  " +
                         " bd_kernel CHAR(100), " +
                         " nzp_raj integer, " +
                         " nzp_bank integer, " +
                         " sum_nach " + DBManager.sDecimalType + "(14,2) default 0, " +
                         " sum_money " + DBManager.sDecimalType + "(14,2) default 0 " +
                         " ) " + DBManager.sUnlogTempTable;
            ExecSQL(sql);

            sql = " create temp table t_count_ls (  " +
                  " bd_kernel CHAR(100), " +
                  " nzp_raj integer, " +
                  " count_ls integer " +
                  " ) " + DBManager.sUnlogTempTable;
            ExecSQL(sql);

            ExecSQL(" create index t_svod_index on t_svod(nzp_raj)");
            ExecSQL(" create index t_count_ls_index on t_count_ls(nzp_raj)");

            ExecSQL(" create index t_svod_index1 on t_svod(bd_kernel)");
            ExecSQL(" create index t_count_ls_index1 on t_count_ls(bd_kernel)");

            const string sqlfin = " create temp table t_res (  " +
                                  " bd_kernel CHAR(100), " +
                                  " nzp_raj integer, " +
                                  " sum_nach " + DBManager.sDecimalType + "(14,2) default 0, " +
                                  " sum_money " + DBManager.sDecimalType + "(14,2) default 0, " +
                                  " sum_money_sber " + DBManager.sDecimalType + "(14,2) default 0, " +
                                  " sum_money_post " + DBManager.sDecimalType + "(14,2) default 0 " +
                                  " ) " + DBManager.sUnlogTempTable;
            ExecSQL(sqlfin);
        }

        protected override void DropTempTable()
        {
            ExecSQL(" drop table t_svod ", true);
            ExecSQL(" drop table t_res ", true);
            ExecSQL(" drop table t_count_ls ", true);
        }

    }
}
