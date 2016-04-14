using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using Bars.KP50.Report.Base;
using Bars.KP50.Report.Tula.Properties;
using Newtonsoft.Json;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.EPaspXsd;
using STCLINE.KP50.Global;
using Bars.KP50.Utils;

namespace Bars.KP50.Report.Tula.Reports
{
    class Report7111051 : BaseSqlReport
    {
        public override string Name
        {
            get { return "71.11.5.1 Выгрузка недопоставок"; }
        }

        public override string Description
        {
            get { return "11.5.1 Выгрузка недопоставок"; }
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
            get { return Resources.Report_71_11_5_1; }
        }

        public override IList<ReportKind> ReportKinds
        {
            get { return new List<ReportKind> { ReportKind.Base }; }
        }


        /// <summary>Расчетный месяц</summary>
        protected int Month { get; set; }

        /// <summary>Расчетный год</summary>
        protected int Year { get; set; }

        /// <summary>Заголовок отчета принципалы</summary>
        protected string PrincipalHeader { get; set; }

        /// <summary>Заголовок отчета</summary>
        protected string SupplierHeader { get; set; }

        /// <summary>Заголовок территории</summary>
        protected string TerritoryHeader { get; set; }

        /// <summary>Поставщики, Агенты, Принципалы  </summary>
        protected BankSupplierParameterValue BankSupplier { get; set; }
        /// <summary>Список префиксов банков в БД</summary>  
        private List<string> PrefBanks { get; set; }
        /// <summary>Список услуг </summary>  
        private List<string> NameServs { get; set; }

        public override List<UserParam> GetUserParams()
        {
            var curCalcMonthYear = DBManager.GetCurMonthYear();
            return new List<UserParam>
            {
                new MonthParameter {Value = curCalcMonthYear != null ? curCalcMonthYear.Rows[0]["month_"] : DateTime.Today.Month },
                new YearParameter {Value = curCalcMonthYear != null ? curCalcMonthYear.Rows[0]["yearr"] : DateTime.Today.Year },
                new BankSupplierParameter()
            };
        }

        public override DataSet GetData()
        {

            string whereSupp = GetWhereSupp("r.nzp_supp"), whereWp = GetwhereWp(), sql=string.Empty;

            foreach (var prefBank in PrefBanks)
            {
                
                string reval = prefBank + "_charge_" + (Year - 2000).ToString("00") + DBManager.tableDelimiter + "reval_" + Month.ToString("00"),
                       nedop = prefBank + DBManager.sDataAliasRest + "nedop_kvar ";

                ExecSQL(" DROP TABLE t_kv_nedop  ");
                sql =
                    " SELECT month_calc as month_,nzp_kvar,nzp_serv,nzp_supp, MAX(nzp_nedop) AS nzp_nedop " +
                    " INTO t_kv_nedop " +
                    " FROM " + nedop + " r" +
                    " WHERE is_actual<>100 and nzp_serv in (6,7,8,9,259,602) and month_calc='" + new DateTime(Year, Month, 1).ToShortDateString() + "'" + whereSupp +  //
                    " GROUP BY 1,2,3,4";
                ExecSQL(sql); 



                if (TempTableInWebCashe("t_kv_nedop") && TempTableInWebCashe(reval) && TempTableInWebCashe(nedop))
                {
                    sql =
                        " INSERT INTO t_11_5 (month_, nzp_kvar, nzp_serv, nzp_supp, nzp_kind, comment, reval, c_reval ) " +
                        " SELECT t.month_, t.nzp_kvar,  t.nzp_serv, t.nzp_supp, nzp_kind, comment, sum(reval), sum(case when reval>0 then reval/tarif else 0 end) " +
                        " FROM t_kv_nedop  t," + reval + " r, " +  nedop + " n "  +
                        " WHERE  r.nzp_kvar=t.nzp_kvar and r.nzp_supp=t.nzp_supp and r.nzp_serv=t.nzp_serv and t.nzp_nedop=n.nzp_nedop " +
                        " GROUP BY 1,2,3,4,5,6 ";
                    ExecSQL(sql);
                }
            }  
            ExecSQL(" CREATE INDEX ix_t_11_5 on t_11_5(nzp_kvar, nzp_serv, nzp_supp, nzp_kind) ");    
          

            sql = " INSERT INTO t_res ( month_, nzp_supp, nzp_kvar, subtype_nedop, comment, num_ls, " +
                  "                     nzp_area, nzp_dom, ikvar, nkvar, " +
                  "                     hvs_sum, gvs_sum, kan_sum, otopl_sum, hvs_rash, gvs_rash, kan_rash, sod_gil_sum, clean_MOP_sum) " +
                  " SELECT month_, nzp_supp, k.nzp_kvar, u.name, comment, num_ls," +
                  "        nzp_area, nzp_dom, ikvar, (CASE WHEN Trim(k.nkvar_n)='-'  THEN (k.nkvar) ELSE nkvar||'ком.'||TRIM(nkvar_n) END), " +
                  "        (CASE WHEN t.nzp_serv=6 then reval else 0 end), " +
                  "        (CASE WHEN t.nzp_serv=9 then reval else 0 end), " +
                  "        (CASE WHEN t.nzp_serv=7 then reval else 0 end), " +
                  "        (CASE WHEN t.nzp_serv=8 then reval else 0 end), " +
                  "        (CASE WHEN t.nzp_serv=6 then c_reval else 0 end), " +
                  "        (CASE WHEN t.nzp_serv=9 then c_reval else 0 end), " +
                  "        (CASE WHEN t.nzp_serv=7 then c_reval else 0 end), " +
                  "        (CASE WHEN t.nzp_serv=259 then reval else 0 end), " +
                  "        (CASE WHEN t.nzp_serv=602 then reval else 0 end) " +
                  " FROM  t_11_5 t, " + ReportParams.Pref + DBManager.sDataAliasRest + "kvar k," +
                  ReportParams.Pref + DBManager.sDataAliasRest + "upg_s_kind_nedop u " +
                  " WHERE u.kod_kind=t.nzp_kind AND u.nzp_kind=1 AND k.nzp_kvar=t.nzp_kvar";
            ExecSQL(sql);
            ExecSQL(" CREATE INDEX ix_t_res_11_5 on t_res( nzp_supp, nzp_area ) ");

            sql = " UPDATE t_res SET area=(SELECT area" +
                  "  FROM " + ReportParams.Pref + DBManager.sDataAliasRest + "s_area a" +
                  " WHERE t_res.nzp_area=a.nzp_area)," +
                  " supplier = (SELECT name_supp " +
                  "  FROM " + ReportParams.Pref + DBManager.sKernelAliasRest + "supplier s " +
                  " WHERE t_res.nzp_supp=s.nzp_supp)";
               ExecSQL(sql);
               ExecSQL(" CREATE INDEX ix_t_res_11_5_1 on t_res(nzp_dom ) ");
            DataTable tempTableRes =
                ExecSQLToTable(
                    " select month_, Trim(comment) as comment, subtype_nedop as reason, area, supplier, d.nzp_dom, num_ls, " +
                    " (CASE WHEN rajon='-'  THEN town ELSE TRIM(town)||','||TRIM(rajon) END)||', ' ||TRIM(" + DBManager.sNvlWord + "(ulicareg,''))||' '||TRIM(ulica) as ulica, " +
                    " ndom, (CASE WHEN nkor ='-' THEN '' ELSE 'кор.'||nkor END) AS nkor, nkvar, " +
                    " hvs_sum, gvs_sum, kan_sum, otopl_sum, hvs_rash, gvs_rash, kan_rash, sod_gil_sum, clean_MOP_sum  " +
                    " from t_res t, " + ReportParams.Pref + DBManager.sDataAliasRest + "dom d," +
                    ReportParams.Pref + DBManager.sDataAliasRest + "s_ulica u," +
                    ReportParams.Pref + DBManager.sDataAliasRest + "s_rajon r," +
                    ReportParams.Pref + DBManager.sDataAliasRest + "s_town twn" + 
                    " WHERE t.nzp_dom=d.nzp_dom and d.nzp_ul=u.nzp_ul AND u.nzp_raj=r.nzp_raj AND r.nzp_town=twn.nzp_town " +
                    " order by month_, comment, subtype_nedop, area, supplier,  town, rajon, ulica, idom, ndom, ikvar, nkvar   ");
            tempTableRes.TableName = "Q_master";

            var ds = new DataSet();
            ds.Tables.Add(tempTableRes);
            return ds;
        }

        /// <summary>
        /// Получить условия органичения по поставщикам
        /// </summary>
        /// <returns></returns>
        private string GetWhereSupp(string fieldPref)
        {
            string oldsupp = ReportParams.GetRolesCondition(Constants.role_sql_supp);
            string whereSupp = String.Empty;
            if (BankSupplier != null && BankSupplier.Suppliers != null)
            {  
                string supp = string.Empty;
                supp = BankSupplier.Suppliers.Aggregate(supp, (current, nzpSupp) => current + (nzpSupp + ","));
                //whereSupp = Suppliers.Aggregate(whereSupp, (current, nzpSupp) => current + (nzpSupp + ","));
                whereSupp += " and nzp_payer_supp in (" + supp.TrimEnd(',') + ")"; 
            }

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
            }
            
            if (BankSupplier != null && BankSupplier.Principals != null)
            {  
                string supp = string.Empty;
                supp = BankSupplier.Principals.Aggregate(supp, (current, nzpSupp) => current + (nzpSupp + ","));
                //whereSupp = Suppliers.Aggregate(whereSupp, (current, nzpSupp) => current + (nzpSupp + ","));
                whereSupp += " and nzp_payer_princip in (" + supp.TrimEnd(',') + ")"; 
                           
                if (!String.IsNullOrEmpty(whereSupp) || !String.IsNullOrEmpty(oldsupp)) 
                {  
                    if (!String.IsNullOrEmpty(oldsupp))
                        whereSupp += " AND nzp_supp in (" + oldsupp + ")";
                
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
            }  


            if (BankSupplier != null && BankSupplier.Agents != null)
            {   
                string supp = string.Empty;
                supp = BankSupplier.Agents.Aggregate(supp, (current, nzpSupp) => current + (nzpSupp + ","));
                //whereSupp = Suppliers.Aggregate(whereSupp, (current, nzpSupp) => current + (nzpSupp + ","));
                whereSupp += " and nzp_payer_agent in (" + supp.TrimEnd(',') + ")";
            }
             
            whereSupp = whereSupp.TrimEnd(',');

            return " and " + fieldPref + " in (select nzp_supp from " +
                   ReportParams.Pref + DBManager.sKernelAliasRest + "supplier " +
                   " where nzp_supp>0 " + whereSupp + ")";
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
            report.SetParameterValue("date", months[Month]+" "+Year);
            report.SetParameterValue("pMonth", months[Month]);
            report.SetParameterValue("pYear", Year);


            string headerParam = !string.IsNullOrEmpty(TerritoryHeader) ? "Территория: " + TerritoryHeader + "\n" : string.Empty;
            headerParam += !string.IsNullOrEmpty(SupplierHeader) ? "Принципалы: " + PrincipalHeader + "\n" : string.Empty;
            headerParam += !string.IsNullOrEmpty(SupplierHeader) ? "Поставщики: " + SupplierHeader + "\n" : string.Empty;
            headerParam = headerParam.TrimEnd('\n');
            report.SetParameterValue("headerParam", headerParam);
        }

        protected override void PrepareParams()
        {
            Month = UserParamValues["Month"].GetValue<int>();
            Year = UserParamValues["Year"].GetValue<int>();
            BankSupplier = JsonConvert.DeserializeObject<BankSupplierParameterValue>(UserParamValues["BankSupplier"].Value.ToString());

            if (Month == 0)
            {
                throw new ReportException("Не определено значение \"Расчетный месяц\"");
            }

            if (Year == 0)
            {
                throw new ReportException("Не определено значение \"Расчетный год\"");
            }

        }

        protected override void CreateTempTable()
        {
            string sql = " CREATE TEMP TABLE t_11_5 ( " +
                         " month_ date, " +
                         " nzp_serv integer, " +
                         " nzp_kvar integer, " +
                         " nzp_supp integer, " +
                         " nzp_kind integer, " +
                         " comment char(100), " +
                         " c_reval " + DBManager.sDecimalType + "(14,2)," +
                         " reval " + DBManager.sDecimalType + "(14,2) )" +
                         DBManager.sUnlogTempTable;
            ExecSQL(sql);


            sql = " CREATE TEMP TABLE t_res ( " +
                  " month_ date, " +
                  " nzp_kvar integer, " +
                  " nzp_dom integer, " +
                  " nzp_area integer, " +
                  " nzp_supp integer, " +
                  " num_ls integer, " +
                  " ikvar integer, " +
                  " nkvar char(10), " +
                  " area char(100), " +
                  " supplier char(100), " +
                  " comment char(100), " +
                  " subtype_nedop char(90), " +
                  " hvs_sum " + DBManager.sDecimalType + "(14,4)," +
                  " gvs_sum " + DBManager.sDecimalType + "(14,4)," +
                  " kan_sum " + DBManager.sDecimalType + "(14,4)," +
                  " otopl_sum " + DBManager.sDecimalType + "(14,4)," +
                  " hvs_rash " + DBManager.sDecimalType + "(14,4)," +
                  " gvs_rash " + DBManager.sDecimalType + "(14,4)," +
                  " kan_rash " + DBManager.sDecimalType + "(14,4)," +
                  " sod_gil_sum " + DBManager.sDecimalType + "(14,4)," +
                  " clean_mop_sum " + DBManager.sDecimalType + "(14,4)" +
                  " )" +
                  DBManager.sUnlogTempTable;
            ExecSQL(sql);
        }

        protected override void DropTempTable()
        {
            ExecSQL(" DROP TABLE t_11_5 ");
            ExecSQL(" DROP TABLE t_res ");
            if (TempTableInWebCashe("t_kv_nedop")) ExecSQL(" DROP TABLE t_kv_nedop  ");
        }

    }
}
