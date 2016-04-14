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
  public  class Report120307 : BaseSqlReportWithDates
    {
        public override string Name
        {
            get { return "12.3.7 Информация о собираемости платежей с населения"; }
        }

        public override string Description
        {
            get { return "12.3.7. Информация о собираемости платежей с населения"; }
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
            get { return Resources.Report_12_3_7; }
        }

        public override IList<ReportKind> ReportKinds
        {
            get { return new List<ReportKind> { ReportKind.Base}; }
        }


        private int Year { get; set; }
        /// <summary> с расчетного месяца </summary>
        private int Month { get; set; }  

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

        /// <summary>Адрес</summary>
        protected AddressParameterValue Address { get; set; }

        /// <summary>Улица</summary>
        private List<int> Raions { get; set; }

        /// <summary>Улица</summary>
        private List<int> Streets { get; set; }

        /// <summary>Дом</summary>
        private List<string> Houses { get; set; }

        /// <summary>Районы</summary>
        private string AddressHeader { get; set; }
      
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
                new MonthParameter{Value=Operday.Month},
                new YearParameter{Value=Operday.Year},
                new BankSupplierParameter(),
                new AddressParameter(),
                new AreaParameter(),
            };
        }

        public override DataSet GetData()
        {
            #region Выборка по локальным банкам
            string whereSupp = GetWhereSupp("c.nzp_supp");
            
            GetwhereWp();
            GetWhereAdr();
            string sql;



            foreach (var pref in PrefBanks)
            {
                int YearP;
                int MonthP = Month - 1;
                if (MonthP == 0){
                    MonthP = 12;
                    YearP = Year - 1;
                }
                else
                {
                    YearP = Year;
                }

                string chargeYY = pref + "_charge_" + (Year - 2000).ToString("00") + DBManager.tableDelimiter +
                                  "charge_" + Month.ToString("00"),
                    chargeXX = pref + "_charge_" + (YearP - 2000).ToString("00") + DBManager.tableDelimiter +
                               "charge_" + MonthP.ToString("00");

                if (TempTableInWebCashe(chargeYY) && TempTableInWebCashe(chargeXX))
                    {
                        sql =
                            " INSERT INTO t_rep_3_7 (nzp_dom, sum_nach ) " +
                            " SELECT k.nzp_dom, SUM(sum_tarif)" +
                            " FROM " + chargeXX + " c JOIN t_12_3_7_kvar k ON k.nzp_kvar = c.nzp_kvar " +
                            " WHERE c.nzp_serv > 1  " +
                            " AND c.dat_charge IS NULL " + whereSupp +
                            " GROUP BY 1 ";
                        ExecSQL(sql);

                        sql =
                            " INSERT INTO t_rep_3_7 (nzp_dom, sum_insaldo, reval, sum_money, sum_outsaldo ) " +
                            " SELECT k.nzp_dom, SUM(sum_insaldo),  SUM(reval), SUM(sum_money), SUM(sum_outsaldo) " +
                            " FROM " + chargeYY + " c JOIN t_12_3_7_kvar k ON k.nzp_kvar = c.nzp_kvar " +
                            " WHERE c.nzp_serv > 1  " +
                            " AND c.dat_charge IS NULL " + whereSupp +
                            " GROUP BY 1 ";
                        ExecSQL(sql);
                    }         

            }       
            #endregion

            #region Выборка на экран

            string centralData = ReportParams.Pref + DBManager.sDataAliasRest;
            sql =
                " SELECT " +
                " TRIM(town) ||  (CASE WHEN rajon IS NULL OR TRIM(rajon) = '' OR TRIM(rajon) = '-' THEN '' ELSE ', ' || TRIM(rajon) END) AS rajon, " +
                " TRIM(area) AS area, TRIM(geu) AS geu, TRIM(ulica) ||' '|| TRIM(ulicareg) AS ulica,  " +
                " SUM(sum_insaldo) AS sum_insaldo, SUM(sum_nach) AS sum_nach, " +
                " SUM(reval) AS sum_reval, SUM(sum_money) AS sum_money, SUM(sum_outsaldo) AS sum_outsaldo," +
                " SUM( CASE WHEN (reval+sum_nach)<>0 THEN (sum_money*100)/(reval+sum_nach) ELSE 0 END) AS percent " +
                " FROM t_rep_3_7 t " +
                " JOIN " + centralData + "dom d ON t.nzp_dom = d.nzp_dom " +
                " JOIN " + centralData + "s_area a ON d.nzp_area = a.nzp_area " +
                " JOIN " + centralData + "s_geu g ON d.nzp_geu = g.nzp_geu " +
                " JOIN " + centralData + "s_ulica u ON d.nzp_ul = u.nzp_ul " +
                " JOIN " + centralData + "s_rajon r ON u.nzp_raj=r.nzp_raj " +
                " JOIN " + centralData + "s_town st ON r.nzp_town = st.nzp_town" +
                " GROUP BY 1,2,3,4 " +
                " ORDER BY 1,2,3,4 ";
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
           
            var months = new[] {"","Январь","Февраль",
                 "Март","Апрель","Май","Июнь","Июль","Август","Сентябрь",
                 "Октябрь","Ноябрь","Декабрь"};
            report.SetParameterValue("print_date", DateTime.Now.ToLongDateString());
            report.SetParameterValue("print_time", DateTime.Now.ToLongTimeString());
            report.SetParameterValue("MonthYear", months[Month] + " " + Year);
            string headerParam = string.Empty;
            headerParam += !string.IsNullOrEmpty(TerritoryHeader) ? "Территория: " + TerritoryHeader + "\n" : string.Empty;
            headerParam += !string.IsNullOrEmpty(AreaHeader) ? "УК: " + AreaHeader + "\n" : string.Empty;
            headerParam += !string.IsNullOrEmpty(SupplierHeader) ? "Поставщики: " + SupplierHeader + "\n" : string.Empty;
            headerParam += !string.IsNullOrEmpty(AddressHeader) ? "Адрес: " + AddressHeader + "\n" : string.Empty;
            report.SetParameterValue("headerParam", headerParam);
        }

        protected override void PrepareParams()
        {
            Areas = UserParamValues["Areas"].GetValue<List<int>>();
            Address = UserParamValues["Address"].GetValue<AddressParameterValue>();
            if (Address.Raions != null)
            {
                Raions = Address.Raions;
            }
            if (Address.Streets != null)
            {
                Streets = Address.Streets;
            }
            if (Address.Houses != null)
            {
                Houses = Address.Houses;
            }
            BankSupplier = JsonConvert.DeserializeObject<BankSupplierParameterValue>(UserParamValues["BankSupplier"].Value.ToString());

            Month = UserParamValues["Month"].GetValue<int>();
            Year = UserParamValues["Year"].Value.To<int>();
        }

        protected override void CreateTempTable()
        {
            string sql = " CREATE TEMP TABLE t_rep_3_7 (  " +  
                         " nzp_dom INTEGER DEFAULT 0, " +
                         " sum_insaldo " + DBManager.sDecimalType + "(14,2), " +
                         " sum_nach " + DBManager.sDecimalType + "(14,2), " +
                         " reval " + DBManager.sDecimalType + "(14,2), " +
                         " sum_money " + DBManager.sDecimalType + "(14,2), " +
                         " sum_outsaldo " + DBManager.sDecimalType + "(14,2)) " + DBManager.sUnlogTempTable;
            ExecSQL(sql);

            ExecSQL(" CREATE INDEX ix_t_rep_3_7_1 on t_rep_3_7(nzp_dom) ");
        }

        protected override void DropTempTable()
        {
            ExecSQL(" DROP TABLE t_rep_3_7 ", true);
        }

        #region  Фильтры

        private void GetwhereWp()
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


        /// <summary>Ограничение по адресу</summary>
        private void GetWhereAdr()
        {
            string
                rajon = string.Empty,
                street = string.Empty,
                house = string.Empty;
            string prefData = ReportParams.Pref + DBManager.sDataAliasRest;

            string result = GetWhereArea();


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

            if (Raions != null) rajon = Raions.Aggregate(rajon, (current, nzpRajon) => current + (nzpRajon + ",")).TrimEnd(',');
            if (Streets != null) street = Streets.Aggregate(street, (current, nzpStreet) => current + (nzpStreet + ",")).TrimEnd(',');
            if (Houses != null) house = Houses.Aggregate(house, (current, nzpHouse) => current + (nzpHouse + ",")).TrimEnd(',');
            result = result.TrimEnd(',');
            result += !string.IsNullOrEmpty(whereWp) ? " AND k.nzp_wp IN ( " + whereWp + ") " : string.Empty;
            result += !string.IsNullOrEmpty(rajon) ? " AND r.nzp_raj IN ( " + rajon + ") " : string.Empty;
            result += !string.IsNullOrEmpty(street) ? " AND u.nzp_ul IN ( " + street + ") " : string.Empty;
            result += !string.IsNullOrEmpty(house) ? " AND d.nzp_dom IN ( " + house + ") " : string.Empty;

            ExecSQL("DROP TABLE t_12_3_7_kvar");

            string sql = " SELECT k.nzp_kvar, k.nzp_dom INTO TEMP t_12_3_7_kvar " +
                         " FROM " + prefData + "kvar k INNER JOIN " + prefData + "dom d ON d.nzp_dom = k.nzp_dom " +
                                                     " INNER JOIN " + prefData + "s_ulica u ON u.nzp_ul = d.nzp_ul " +
                                                     " INNER JOIN " + prefData + "s_rajon r ON r.nzp_raj = u.nzp_raj " +
                         " WHERE nzp_kvar > 0 " + result;
            ExecSQL(sql);
            ExecSQL("create index ix_tmpkv_97 on t_12_3_7_kvar(nzp_kvar)");
            ExecSQL("analyze t_12_3_7_kvar");

            if (!String.IsNullOrEmpty(house))
            {
                sql = " SELECT TRIM(town) AS  town, TRIM(rajon) AS rajon, TRIM(ulica) AS ulica, TRIM(ndom) AS ndom, TRIM(nkor) AS nkor " +
                          " FROM " + prefData + "dom d INNER JOIN " + prefData + "s_ulica u ON u.nzp_ul = d.nzp_ul " +
                                                     " INNER JOIN " + prefData + "s_rajon r ON r.nzp_raj = u.nzp_raj " +
                                                     " INNER JOIN " + prefData + "s_town t ON t.nzp_town = r.nzp_town" +
                          " WHERE nzp_dom IN (" + house + ") ";
                DataTable addressTable = ExecSQLToTable(sql);
                foreach (DataRow row in addressTable.Rows)
                {
                    AddressHeader = "," + AddressHeader;
                    AddressHeader += !string.IsNullOrEmpty(row["town"].ToString().Trim())
                        ? "," + row["town"].ToString().Trim() + "/"
                        : ",-/";
                    AddressHeader += !string.IsNullOrEmpty(row["rajon"].ToString().Trim())
                        ? row["rajon"].ToString().Trim() + ","
                        : "-,";
                    AddressHeader += !string.IsNullOrEmpty(row["ulica"].ToString().Trim())
                        ? "ул. " + row["ulica"].ToString().Trim() + ","
                        : string.Empty;
                    AddressHeader += !string.IsNullOrEmpty(row["ndom"].ToString().Trim())
                        ? row["ndom"].ToString().Trim() != "-"
                            ? "д. " + row["ndom"].ToString().Trim() + ","
                            : string.Empty
                        : string.Empty;
                    AddressHeader += !string.IsNullOrEmpty(row["nkor"].ToString().Trim())
                        ? row["nkor"].ToString().Trim() != "-"
                            ? "кор. " + row["nkor"].ToString().Trim() + ","
                            : string.Empty
                        : string.Empty;
                    AddressHeader = AddressHeader.TrimEnd(',');

                }
                AddressHeader = AddressHeader.TrimEnd(',');
            }
            else if (!String.IsNullOrEmpty(street))
            {
                sql = " SELECT TRIM(town) AS  town, TRIM(rajon) AS rajon, TRIM(ulica) AS ulica " +
                          " FROM " + prefData + "s_ulica u INNER JOIN " + prefData + "s_rajon r ON r.nzp_raj = u.nzp_raj " +
                                                         " INNER JOIN " + prefData + "s_town t ON t.nzp_town = r.nzp_town " +
                          " WHERE nzp_ul IN (" + street + ") ";
                DataTable addressTable = ExecSQLToTable(sql);
                foreach (DataRow row in addressTable.Rows)
                {
                    AddressHeader += !string.IsNullOrEmpty(row["town"].ToString().Trim())
                        ? "," + row["town"].ToString().Trim() + "/"
                        : ",-/";
                    AddressHeader += !string.IsNullOrEmpty(row["rajon"].ToString().Trim())
                        ? row["rajon"].ToString().Trim() + ","
                        : "-,";
                    AddressHeader += !string.IsNullOrEmpty(row["ulica"].ToString().Trim())
                        ? "ул. " + row["ulica"].ToString().Trim() + ","
                        : string.Empty;
                    AddressHeader = AddressHeader.TrimEnd(',');
                }
                AddressHeader = AddressHeader.TrimEnd(',');
            }
            else if (!String.IsNullOrEmpty(rajon))
            {
                sql = " SELECT TRIM(town) AS  town, TRIM(rajon) AS rajon " +
                          " FROM " + prefData + "s_rajon r INNER JOIN " + prefData + "s_town t ON t.nzp_town = r.nzp_town " +
                          " WHERE nzp_raj IN (" + rajon + ") ";
                DataTable addressTable = ExecSQLToTable(sql);
                foreach (DataRow row in addressTable.Rows)
                {
                    AddressHeader += !string.IsNullOrEmpty(row["town"].ToString().Trim())
                        ? "," + row["town"].ToString().Trim() + "/"
                        : ",-/";
                    AddressHeader += !string.IsNullOrEmpty(row["rajon"].ToString().Trim())
                        ? row["rajon"].ToString().Trim() + ","
                        : "-,";
                    AddressHeader = AddressHeader.TrimEnd(',');
                }
                AddressHeader = AddressHeader.TrimEnd(',');
            }
            if (!string.IsNullOrEmpty(AddressHeader))
                AddressHeader = AddressHeader.TrimStart(',');
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
