using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using Bars.KP50.Report.Base;
using Bars.KP50.Report.Base.Parameters;
using Bars.KP50.Utils;
using Newtonsoft.Json;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;
using Bars.KP50.Report.MariEl.Properties;
using STCLINE.KP50.Interfaces;

namespace Bars.KP50.Report.MariEl.Reports
{
  public  class Report120110 : BaseSqlReport
    {
        public override string Name
        {
            get { return "12.1.10 Информация о задолженности"; }
        }

        public override string Description
        {
            get { return "12.1.10 Информация о задолженности"; }
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
            get { return Resources.Report_12_1_10; }
        }

        public override IList<ReportKind> ReportKinds
        {
            get { return new List<ReportKind> { ReportKind.Base}; }
        }


        /// <summary> с расчетного дня </summary>
        protected DateTime DatS { get; set; }

        /// <summary> по расчетный день </summary>
        protected DateTime DatPo { get; set; }    

        /// <summary>Ук</summary>
        protected List<int> Areas { get; set; }

        /// <summary>Список УК в заголовке</summary>
        protected string AreaHeader { get; set; }

        /// <summary>Услуги</summary>
        protected List<int> Services { get; set; }

        /// <summary>Список услуг в заголовке</summary>
        protected string ServiceHeader { get; set; }

        /// <summary>Поставщики, Агенты, Принципалы  </summary>
        protected BankSupplierParameterValue BankSupplier { get; set; } 
      
      /// <summary>Список Поставщиков в заголовке</summary>
        protected string SupplierHeader { get; set; }
      
        /// <summary>Территории</summary>
        protected List<int> Banks { get; set; }
        /// <summary>Заголовок территории</summary>
        protected string TerritoryHeader { get; set; }

        /// <summary>Список префиксов банков в БД</summary>  
        private List<string> PrefBanks { get; set; }



        public override List<UserParam> GetUserParams()
        {   
            return new List<UserParam>
            {
                new PeriodParameter( DateTime.Now),
                new BankSupplierParameter(),
                new AreaParameter(),
                new ServiceParameter(),
            };
        }

        public override DataSet GetData()
        {
            #region Выборка по локальным банкам
            string whereServ = GetWhereServ(),
                whereArea = GetWhereArea(),
                whereSupp = GetWhereSupp("c.nzp_supp");
            
            GetwhereWp();
            string sql;



            foreach (var pref in PrefBanks)
            {    
                string chargeYY = pref + "_charge_" + (DatPo.Year - 2000).ToString("00") + DBManager.tableDelimiter +
                                  "charge_" + DatPo.Month.ToString("00"),
                    chargeXX = pref + "_charge_" + (DatPo.AddMonths(-1).Year - 2000).ToString("00") + DBManager.tableDelimiter +
                               "charge_" + DatPo.AddMonths(-1).Month.ToString("00");


                if (TempTableInWebCashe(chargeYY) && TempTableInWebCashe(chargeXX))
                    {
                        sql =
                            " INSERT INTO t_rep_1_10 (num_ls, sum_insaldo, reval, sum_outsaldo ) " +
                            " SELECT k.num_ls, SUM(sum_insaldo), SUM(reval+real_charge),  " +
                            " SUM(sum_insaldo+ reval+real_charge-sum_nedop) " +
                            " FROM " + chargeYY + " c, " + pref + DBManager.sDataAliasRest + "kvar k " +
                            " WHERE c.nzp_serv > 1  " +
                            " AND c.dat_charge IS NULL " +
                            " AND k.num_ls = c.num_ls " + whereServ + whereArea + whereSupp +
                            " GROUP BY 1 ";
                        ExecSQL(sql);


                        sql =
                            " INSERT INTO t_rep_1_10 (num_ls, sum_nach)" +
                            " SELECT k.num_ls, SUM(sum_tarif + real_charge + reval) " +
                            " FROM " + chargeXX + " c, " + pref + DBManager.sDataAliasRest + "kvar k " +
                            " WHERE c.nzp_serv > 1  " +
                            " AND c.dat_charge IS NULL " +
                            " AND k.num_ls = c.num_ls " + whereServ + whereArea + whereSupp +
                            " GROUP BY 1 ";
                        ExecSQL(sql);
                    } 
            

                DatS = new DateTime(DatPo.Year, DatPo.Month,1);


                string tableFnSupplier = pref + "_charge_" + (DatPo.Year - 2000).ToString("00") +
                                             DBManager.tableDelimiter + "fn_supplier" + DatPo.Month.ToString("00");  

                    if (TempTableInWebCashe(tableFnSupplier))
                    { 
                        sql =
                            " INSERT INTO t_rep_1_10 (num_ls, sum_money ) " +
                            " SELECT k.num_ls, SUM(sum_prih) " +
                            " FROM " +
                            tableFnSupplier + " c, " + pref + DBManager.sDataAliasRest + "kvar k " +
                            " WHERE c.nzp_serv > 1 " +
                            " AND c.dat_uchet >= '" + DatS.ToShortDateString() + "' " +
                            " AND c.dat_uchet <= '" + DatPo.ToShortDateString() + "'" +
                            " AND k.num_ls = c.num_ls " + whereServ + whereArea + whereSupp +
                            " GROUP BY 1 ";
                        ExecSQL(sql);
                    }    

                    string tableFromSupplier = pref + "_charge_" + (DatPo.Year - 2000).ToString("00") +
                                               DBManager.tableDelimiter + "from_supplier ";

                    if (TempTableInWebCashe(tableFromSupplier))
                    { 
                        sql =
                            " INSERT INTO t_rep_1_10 (num_ls , sum_money ) " +
                            " SELECT k.num_ls,  SUM(sum_prih) " +
                            " FROM " +
                            tableFromSupplier + " c, " + pref + DBManager.sDataAliasRest + "kvar k " +
                            " WHERE c.nzp_serv > 1 " +
                            " AND c.dat_uchet >= '" + DatS.ToShortDateString() + "' " +
                            " AND c.dat_uchet <= '" + DatPo.ToShortDateString() + "'" +
                            " AND c.kod_sum in (49, 50, 35) " +
                            " AND k.num_ls = c.num_ls " + whereServ + whereArea + whereSupp +
                            " GROUP BY 1 ";
                        ExecSQL(sql);
                    }


            }       
            #endregion

            #region Выборка на экран

            string centralData = ReportParams.Pref + DBManager.sDataAliasRest;
            sql =
                " SELECT k.num_ls, TRIM(fio) AS fio, " +
                " TRIM(town) ||  (CASE WHEN rajon IS NULL OR TRIM(rajon) = '' OR TRIM(rajon) = '-' THEN '' ELSE ', ' || TRIM(rajon) END) AS rajon, " +
                " TRIM(ulica) AS ulica, TRIM(ulicareg) AS ulicareg,  " +
                " TRIM(ndom) AS ndom,  (CASE WHEN nkor IS NULL  OR TRIM(nkor) = '' OR TRIM(nkor) = '-' THEN '' ELSE ', корп. ' || TRIM(nkor) END) AS nkor, " +
                " TRIM(nkvar) AS nkvar, (CASE WHEN nkvar_n IS NULL OR TRIM(nkvar_n) = '' OR TRIM(nkvar_n) = '-' THEN '' ELSE ', комн. ' || TRIM(nkvar_n) END) AS nkvar_n, " +
                " SUM(sum_insaldo) AS sum_insaldo, SUM(sum_nach) AS sum_nach, " +
                " SUM(reval) AS reval, SUM(sum_money) AS sum_money, SUM(sum_outsaldo) AS sum_outsaldo " +
                " FROM t_rep_1_10 t " +
                " JOIN " + centralData + "kvar k ON t.num_ls=k.num_ls " +
                " JOIN " + centralData + "dom d ON k.nzp_dom = d.nzp_dom " +
                " JOIN " + centralData + "s_ulica u ON d.nzp_ul = u.nzp_ul " +
                " JOIN " + centralData + "s_rajon r ON u.nzp_raj=r.nzp_raj " +
                " JOIN " + centralData + "s_town st ON r.nzp_town = st.nzp_town" +
                " GROUP BY 1,2,3,4,5,6,7,8,9 " +
                " ORDER BY 3,4,5,6,7,8,9 ";
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



      

        protected override void PrepareReport(FastReport.Report report)
        {
            string period = "Период с " + DatS.ToShortDateString() + " по " + DatPo.ToShortDateString(); 

            report.SetParameterValue("print_date", DateTime.Now.ToLongDateString());
            report.SetParameterValue("print_time", DateTime.Now.ToLongTimeString());

            string headerParam = period + "\n";
            headerParam += !string.IsNullOrEmpty(TerritoryHeader) ? "Территория: " + TerritoryHeader + "\n" : string.Empty;
            headerParam += !string.IsNullOrEmpty(AreaHeader) ? "УК: " + AreaHeader + "\n" : string.Empty;
            headerParam += !string.IsNullOrEmpty(SupplierHeader) ? "Поставщики: " + SupplierHeader + "\n" : string.Empty;
            headerParam += !string.IsNullOrEmpty(ServiceHeader) ? "Услуги: " + ServiceHeader + "\n" : string.Empty;
            report.SetParameterValue("headerParam", headerParam);
        }

        protected override void PrepareParams()
        {
            Areas = UserParamValues["Areas"].GetValue<List<int>>();
            Services = UserParamValues["Services"].GetValue<List<int>>();
            BankSupplier = JsonConvert.DeserializeObject<BankSupplierParameterValue>(UserParamValues["BankSupplier"].Value.ToString());

            var period = UserParamValues["Period"].GetValue<string>();
            DateTime d2;
            PeriodParameter.GetValues(period, out d2);
            DatPo = d2;
        }

        protected override void CreateTempTable()
        {
            string sql = " CREATE TEMP TABLE t_rep_1_10 (  " +  
                         " num_ls INTEGER DEFAULT 0, " +
                         " sum_insaldo " + DBManager.sDecimalType + "(14,2), " +
                         " sum_nach " + DBManager.sDecimalType + "(14,2), " +
                         " reval " + DBManager.sDecimalType + "(14,2), " +
                         " sum_money " + DBManager.sDecimalType + "(14,2), " +
                         " sum_outsaldo " + DBManager.sDecimalType + "(14,2)) " + DBManager.sUnlogTempTable;
            ExecSQL(sql);

            ExecSQL(" CREATE INDEX ix_t_rep_1_10_1 on t_rep_1_10(num_ls) ");
        }

        protected override void DropTempTable()
        {
            ExecSQL(" DROP TABLE t_rep_1_10 ", true);
        }

        #region  Фильтры

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
                AreaHeader = String.Empty;
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
        


        #endregion

    }
}
