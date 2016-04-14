using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using Bars.KP50.Report.Base;
using Bars.KP50.Report.Sakha.Properties;
using Bars.KP50.Utils;
using Newtonsoft.Json;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;

namespace Bars.KP50.Report.Sakha.Reports
{
    public class Report1431 : BaseSqlReport
    {
        public override string Name
        {
            get { return "14.3.1.Реестр поступлений по поставщикам"; }
        }

        public override string Description
        {
            get { return "Реестр поступлений по поставщикам"; }
        }

        public override IList<ReportGroup> ReportGroups
        {
            get
            {
                var result = new List<ReportGroup> { ReportGroup.Finans };
                return result;
            }
        }

        protected override byte[] Template
        {
            get { return Resources.Report_14_3_1; }
        }

        public override IList<ReportKind> ReportKinds
        {
            get { return new List<ReportKind> { ReportKind.Base }; }
        }

        /// <summary>Расчетный месяц</summary>
        protected int Month { get; set; }

        /// <summary>Расчетный год</summary>
        protected int Year { get; set; }

        /// <summary>Адрес</summary>
        private AddressParameterValue Address { get; set; }

        /// <summary>Районы</summary>
        private string AddressHeader { get; set; }

        /// <summary>Управляющие компании</summary>
        protected List<int> Areas { get; set; }

        /// <summary>Управляющая компания</summary>
        protected string AreaHeader { get; set; }

        /// <summary>Поставщики, Агенты, Принципалы  </summary>
        protected BankSupplierParameterValue BankSupplier { get; set; }

        /// <summary>Поставщик</summary>
        protected string SupplierHeader { get; set; }

        /// <summary>Агент</summary>
        protected string AgentHeader { get; set; }

        /// <summary>Заголовок территории</summary>
        protected string TerritoryHeader { get; set; }

        public override List<UserParam> GetUserParams()
        {
            return new List<UserParam>
            {
                new MonthParameter {Name = "Месяц", Value = DateTime.Today.Month},
                new YearParameter {Name = "Год", Value = DateTime.Today.Year},
                new AreaParameter(),
                new BankSupplierParameter(),
                new AddressParameter()
            };
        }

        protected override void PrepareParams()
        {

            Month = UserParamValues["Month"].GetValue<int>();
            Year = UserParamValues["Year"].Value.To<int>();

            Areas = UserParamValues["Areas"].Value.To<List<int>>();

            BankSupplier = JsonConvert.DeserializeObject<BankSupplierParameterValue>(UserParamValues["BankSupplier"].Value.ToString());

            Address = UserParamValues["Address"].GetValue<AddressParameterValue>();
        }

        public override DataSet GetData()
        {
            string sql = "";
            string whereSupp = GetWhereSupp("a.nzp_supp"),
                whereArea = GetWhereArea(),
                  whereWP = GetwhereWp(),
                    whereAdr = GetWhereAdr(),
                     wherePayer = GetWherePayer("a.nzp_payer");

            var distribXx = ReportParams.Pref + "_fin_" + (Year - 2000).ToString("00") +
                                DBManager.tableDelimiter + "fn_distrib_dom_" + Month.ToString("00");

            sql = " INSERT INTO t_distrib (nzp_dom, nzp_serv, " +
                      "         nzp_supp, sum_rasp, sum_ud, sum_charge )" +
                      " SELECT a.nzp_dom, a.nzp_serv, a.nzp_supp, " +
                      "         sum(a.sum_rasp), sum(a.sum_ud), sum(a.sum_charge)  " +
                      " FROM " + distribXx + " a,   " +
                      ReportParams.Pref + DBManager.sKernelAliasRest + "supplier s, " +
                      ReportParams.Pref + DBManager.sDataAliasRest + "s_ulica u " +
                      "  WHERE   " +
                      "         a.nzp_supp=s.nzp_supp " +
                      "         and a.nzp_payer=s.nzp_payer_princip " +
                      whereSupp + wherePayer + whereAdr + whereArea +
                      " GROUP BY  1,2,3 ";
            if (TempTableInWebCashe(distribXx))
                ExecSQL(sql);
            //#region Выборка на экран

            sql = " SELECT  sp.point as pref, pa.payer as agent, pp.payer as principal,  s.service," +
                  "        p.payer as name_supp, " +
                  "        sum(t.sum_charge) as sum_charge, sum(sum_rasp) as sum_rasp, " +
                  "        sum(t.sum_ud) as sum_ud  " +
                  " FROM t_distrib t, " +
                  ReportParams.Pref + DBManager.sKernelAliasRest + "services s, " +
                  ReportParams.Pref + DBManager.sKernelAliasRest + "supplier su, " +
                  ReportParams.Pref + DBManager.sKernelAliasRest + "s_payer p, " +
                  ReportParams.Pref + DBManager.sKernelAliasRest + "s_payer pa, " +
                  ReportParams.Pref + DBManager.sKernelAliasRest + "s_payer pp, " +
                  ReportParams.Pref + DBManager.sDataAliasRest + "dom d, " +
                  ReportParams.Pref + DBManager.sDataAliasRest + "s_ulica sl, " +
                  ReportParams.Pref + DBManager.sKernelAliasRest + "s_point sp " +
                  " WHERE t.nzp_supp = su.nzp_supp " +
                  "        AND su.nzp_payer_supp = p.nzp_payer " +
                  "        AND su.nzp_payer_agent = pa.nzp_payer " +
                  "        AND su.nzp_payer_princip = pp.nzp_payer " +
                  "        AND t.nzp_serv = s.nzp_serv " +
                  "        and t.nzp_dom=d.nzp_dom " +
                  "        and d.nzp_ul=sl.nzp_ul " +
                  "        AND sp.bd_kernel = d.pref " + GetwhereWp() +
                  "        GROUP BY 1,2,3,4,5 ORDER BY 1,2,3,4,5  ";
            
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


        public override bool IsPreview
        {
            get { return false; }
        }

        protected override void PrepareReport(FastReport.Report report)
        {
            var months = new[] {"","Январь","Февраль",
                 "Март","Апрель","Май","Июнь","Июль","Август","Сентябрь",
                 "Октябрь","Ноябрь","Декабрь"};
            string date = months[Month] + " месяц " + Year;
            report.SetParameterValue("Month", date);

            string headerParam = !string.IsNullOrEmpty(AreaHeader) ? "Управляющая компания: " + AreaHeader + "\n" : "Управляющая компания: Все управляющие компании \n";
            headerParam += !string.IsNullOrEmpty(SupplierHeader) ? "Поставщики: " + SupplierHeader + "\n" : "Поставщики: Все поставщики \n";
            headerParam += !string.IsNullOrEmpty(TerritoryHeader) ? "Округ: " + TerritoryHeader + "\n" : "Округ: Все округа \n";
            headerParam += !string.IsNullOrEmpty(AddressHeader) ? "Адрес: " + AddressHeader + "\n" : "Адрес: Все адреса \n";
            headerParam += !string.IsNullOrEmpty(AgentHeader) ? "Агент: " + AgentHeader + "\n" : "Агент: Все агенты \n";
            headerParam = headerParam.TrimEnd('\n');
            report.SetParameterValue("headerParam", headerParam);
        }

        

        protected override void CreateTempTable()
        {
            const string sql = " create temp table t_distrib (     " +
                               " nzp_dom integer default 0," +
                               " nzp_area integer default 0," +
                               " nzp_serv integer default 0," +
                               " nzp_supp integer default 0," +
                               " sum_rasp " + DBManager.sDecimalType + "(14,2) default 0.00, " + //Рапределено
                               " sum_ud " + DBManager.sDecimalType + "(14,2) default 0.00, " + //Удержано
                               " sum_charge " + DBManager.sDecimalType + "(14,2) default 0.00 " + //К перечислению
                               " ) " + DBManager.sUnlogTempTable;
            ExecSQL(sql);
        }

        /// <summary>Ограничение по адресу</summary>
        private string GetWhereAdr()
        {
            string rajon = String.Empty,
                street = String.Empty,
                house = String.Empty;
            string prefData = ReportParams.Pref + DBManager.sDataAliasRest;

            string result = ReportParams.GetRolesCondition(Constants.role_sql_area);


            if (Address.Raions != null)
            {
                rajon = Address.Raions.Aggregate(rajon, (current, nzpRajon) => current + (nzpRajon + ","));
                rajon = rajon.TrimEnd(',');
            }
            if (Address.Streets != null)
            {
                street = Address.Streets.Aggregate(street, (current, nzpStreet) => current + (nzpStreet + ","));
                street = street.TrimEnd(',');
            }
            if (Address.Houses != null)
            {
                house = Address.Houses.Aggregate(house, (current, nzpHouse) => current + (nzpHouse + ","));
                house = house.TrimEnd(',');
            }

            result = result.TrimEnd(',');
            result = !String.IsNullOrEmpty(result) ? " AND k.nzp_area in (" + result + ")" : String.Empty;
            result += !String.IsNullOrEmpty(rajon) ? " AND u.nzp_raj IN ( " + rajon + ") " : string.Empty;
            result += !String.IsNullOrEmpty(street) ? " AND u.nzp_ul IN ( " + street + ") " : string.Empty;
            result += !String.IsNullOrEmpty(house) ? " AND d.nzp_dom IN ( " + house + ") " : string.Empty;
            if (!String.IsNullOrEmpty(house))
            {
                var sql = " SELECT TRIM(town) AS  town, TRIM(rajon) AS rajon, TRIM(ulica) AS ulica, TRIM(ndom) AS ndom, TRIM(nkor) AS nkor " +
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
                var sql = " SELECT TRIM(town) AS  town, TRIM(rajon) AS rajon, TRIM(ulica) AS ulica " +
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
                var sql = " SELECT TRIM(town) AS  town, TRIM(rajon) AS rajon " +
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


            return result;
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
            whereArea = !String.IsNullOrEmpty(whereArea) ? " AND nzp_area in (" + whereArea + ")" : String.Empty;
            if (!String.IsNullOrEmpty(whereArea))
            {
                AreaHeader = String.Empty;
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
        /// Получает условия ограничения по поставщику
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

               
            }
            return " and " + fieldPref + " in (select nzp_supp from " +
                   ReportParams.Pref + DBManager.sKernelAliasRest + "supplier " +
                   " where nzp_supp>0 " + whereSupp + ")";
        }

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
            whereWp = !String.IsNullOrEmpty(whereWp) ? " AND sp.nzp_wp in (" + whereWp + ")" : String.Empty;
            if (!string.IsNullOrEmpty(whereWp))
            {
                TerritoryHeader = String.Empty;
                string sql = " SELECT point FROM " + ReportParams.Pref + DBManager.sKernelAliasRest + "s_point sp WHERE nzp_wp > 0 " + whereWp;
                DataTable terrTable = ExecSQLToTable(sql);
                foreach (DataRow row in terrTable.Rows)
                {
                    TerritoryHeader += row["point"].ToString().Trim() + ", ";
                }
                TerritoryHeader = TerritoryHeader.TrimEnd(',', ' ');
            }
            return whereWp;
        }

        /// <summary>
        /// Получает условия ограничения по контрагенту, который получает деньги
        /// </summary>
        /// <returns></returns>
        private string GetWherePayer(string fieldPref)
        {
            string wherePayer = String.Empty;
            if (BankSupplier != null && BankSupplier.Principals != null)
            {

                wherePayer += BankSupplier.Principals.Aggregate(wherePayer, (current, nzpSupp) => current + (nzpSupp + ","));

            }

            wherePayer = wherePayer.TrimEnd(',');
            string rolePayer = ReportParams.GetRolesCondition(Constants.role_sql_payer);

            if (!String.IsNullOrEmpty(wherePayer) || !String.IsNullOrEmpty(rolePayer))
            {
                wherePayer = " WHERE nzp_payer in  (" + wherePayer + ")";
                if (!String.IsNullOrEmpty(rolePayer))
                    wherePayer += " and nzp_payer in (" + rolePayer + ")";

                //Агенты
                AgentHeader = String.Empty;
                string _sql = " SELECT payer from " +
                             ReportParams.Pref + DBManager.sKernelAliasRest + "s_payer " +
                             wherePayer;
                DataTable payer = ExecSQLToTable(_sql);
                foreach (DataRow dr in payer.Rows)
                {
                    AgentHeader += "(" + dr["payer"].ToString().Trim() + "), ";
                }
                AgentHeader = AgentHeader.TrimEnd(',', ' ');


                string sql = " and " + fieldPref + " in ( SELECT nzp_payer " +
                             " from " + ReportParams.Pref + DBManager.sKernelAliasRest + "s_payer " +
                             wherePayer + ")";
                return sql;
            }
            return String.Empty;
        }

        protected override void DropTempTable()
        {
            ExecSQL(" drop table t_distrib ", false);
        }
    }
}
