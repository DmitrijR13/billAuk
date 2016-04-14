using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Bars.KP50.Report.Base;
//using Bars.KP50.Report.MariEl.Properties;
using Bars.KP50.Report.MariEl.Properties;
using Bars.KP50.Utils;
using Newtonsoft.Json;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;

namespace Bars.KP50.Report.MariEl.Reports
{
	class Report120308 : BaseSqlReportWithDates
	{

		public override string Name {
            get { return "12.3.8 Информация для отдела ЖКХ (Минстрой) по услугам"; }
		}

		public override string Description {
            get { return "3.8 Информация для отдела ЖКХ (Минстрой) по услугам"; }
		}

		public override IList<ReportGroup> ReportGroups {
            get
            {
                var result = new List<ReportGroup> { ReportGroup.Finans };
                return result;
            }
		}

		public override bool IsPreview {
			get { return false; }
		}

		public override IList<ReportKind> ReportKinds {
			get { return new List<ReportKind> { ReportKind.Base}; }
		}

		protected override byte[] Template {
			get { return Resources.Report_12_3_8; }
		}
        /// <summary>Список Поставщиков в заголовке</summary>
        protected string SupplierHeader { get; set; }

        /// <summary>Принципал</summary>
        protected string PrincipalHeader { get; set; }

        /// <summary>Агент</summary>
        protected string AgentHeader { get; set; }

		/// <summary>Заголовок территории</summary>
		protected string TerritoryHeader { get; set; }

		/// <summary>Поставщики, Агенты, Принципалы  </summary>
		protected BankSupplierParameterValue BankSupplier { get; set; }

        /// <summary>Список префиксов банков в БД</summary>  
        private List<string> PrefBanks { get; set; }
        /// <summary> с расчетного года </summary>

        /// <summary>Управляющие компании</summary>
        protected List<int> Areas { get; set; }

        /// <summary>Заголовок отчета</summary>
        protected string AreaHeader { get; set; }

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

        private int Year { get; set; }
        /// <summary> с расчетного месяца </summary>
        private int Month { get; set; }

        /// <summary>Районы</summary>
		public override List<UserParam> GetUserParams() {
			return new List<UserParam>
			{
				new MonthParameter { Value = Operday.Month },
                new YearParameter { Value = Operday.Year },
				new BankSupplierParameter(),
                new AddressParameter(),
                new AreaParameter()
			};
		}

		public override DataSet GetData() 
        {
			#region выборка в temp таблицы

            string whereSupp = GetWhereSupp("c.nzp_supp");
		    PreparePrefsAndTerritoryHeader();
		    GetWhereAdr();
			string sql ;

		    foreach (var pref in PrefBanks)
		    {

		        string charge = pref + "_charge_" + (Year - 2000).ToString("00") +
		                        DBManager.tableDelimiter + "charge_" + Month.ToString("00");
		        string supplier = Points.Pref + DBManager.sKernelAliasRest + "supplier";

		        sql =
		            " INSERT INTO t_svod" +
                    " (nzp_serv, nzp_supp_princip, sum_insaldo, sum_real, reval," +
		            " sum_money,  percent, total_saldo) " +
		            " SELECT c.nzp_serv, s.nzp_payer_supp, SUM(c.sum_insaldo), SUM(c.sum_real), SUM(c.reval)," +
                    " SUM(c.sum_money)," +
                    " (CASE WHEN (SUM(c.sum_real) + SUM(c.reval)) > 0 THEN" +
		            " ((SUM(c.sum_money)*100)/(SUM(c.sum_real) + SUM(c.reval))) ELSE 0.00 END)," +
                    " SUM(c.sum_insaldo) + SUM(c.sum_real) + SUM(c.reval) - SUM(c.sum_money) " +
                    " FROM " + charge + " c," +
		            " t_12_3_8_kvar k," +
		            " " + supplier + " s" +
		            " WHERE c.nzp_kvar = k.nzp_kvar AND c.nzp_supp = s.nzp_supp" +
		            " AND c.nzp_serv>1 AND c.dat_charge is null " + whereSupp + 
                    " GROUP BY 1,2 ";
		        ExecSQL(sql);
                
		        ExecSQL(DBManager.sUpdStat + " t_svod");

		    }

		    #endregion
            string centralKernel = ReportParams.Pref + DBManager.sKernelAliasRest;
            sql = 
                " SELECT t.nzp_serv, s.service as serv, t.nzp_supp_princip, p.payer as principal, t.sum_insaldo, t.sum_real, t.reval," +
                " t.sum_money, t.percent as percent_total, t.total_saldo as sum_totalsaldo " +
                " FROM t_svod t, " +
                centralKernel + "services s, " +
                centralKernel + "s_payer p" +
                " WHERE s.nzp_serv = t.nzp_serv AND p.nzp_payer = t.nzp_supp_princip" +
                " ORDER BY p.payer, s.service ";
            DataTable dt = ExecSQLToTable(sql);
            dt.TableName = "Q_master";
			var ds = new DataSet();
			ds.Tables.Add(dt);
			return ds;
		}

        #region WHERE - фильтры
        /// <summary>
		/// Получает условия ограничения по поставщику
		/// </summary>
		private string GetWhereSupp(string fieldPref) {
			string whereSupp = String.Empty;
			if (BankSupplier != null && BankSupplier.Suppliers != null)
			{
				string supp = string.Empty;
				supp = BankSupplier.Suppliers.Aggregate(supp, (current, nzpSupp) => current + (nzpSupp + ","));
                whereSupp += " and nzp_payer_supp in (" + supp.TrimEnd(',') + ")";
                if (string.IsNullOrEmpty(SupplierHeader)) SupplierHeader = GetWherePayer(supp.TrimEnd(','));
			}

			if (BankSupplier != null && BankSupplier.Principals != null)
			{
				string supp = string.Empty;
				supp = BankSupplier.Principals.Aggregate(supp, (current, nzpSupp) => current + (nzpSupp + ","));
                whereSupp += " and nzp_payer_princip in (" + supp.TrimEnd(',') + ")";
                if (string.IsNullOrEmpty(PrincipalHeader)) PrincipalHeader = GetWherePayer(supp.TrimEnd(','));
			}
			if (BankSupplier != null && BankSupplier.Agents != null)
			{
				string supp = string.Empty;
				supp = BankSupplier.Agents.Aggregate(supp, (current, nzpSupp) => current + (nzpSupp + ","));
                whereSupp += " and nzp_payer_agent in (" + supp.TrimEnd(',') + ")";
                if (string.IsNullOrEmpty(AgentHeader)) AgentHeader = GetWherePayer(supp.TrimEnd(','));
			}

			string oldsupp = ReportParams.GetRolesCondition(Constants.role_sql_supp);

			whereSupp = whereSupp.TrimEnd(',');

            if (!String.IsNullOrEmpty(oldsupp))
					whereSupp += " AND nzp_supp in (" + oldsupp + ")";
			
			return " and " + fieldPref + " in (select nzp_supp from " +
				   ReportParams.Pref + DBManager.sKernelAliasRest + "supplier " +
				   " where nzp_supp>0 " + whereSupp + ")";
		}
        /// <summary>Получает payer из таблички s_payer</summary>
        /// <param name="wherePayer">Фильтр</param>
        private string GetWherePayer(string wherePayer)
        {
            string prefKernel = ReportParams.Pref + DBManager.sKernelAliasRest;
            string namesPayer = string.Empty;
            if (wherePayer != string.Empty)
            {
                string sql = " SELECT trim(payer) as payer " +
                             " FROM " + prefKernel + "s_payer " +
                             " WHERE nzp_payer IN (" + wherePayer + ") GROUP BY 1 ORDER BY 1";
                DataTable dt = ExecSQLToTable(sql);
                namesPayer = dt.Rows.Cast<DataRow>()
                    .Where(row => row["payer"] != DBNull.Value)
                    .Aggregate(namesPayer, (current, row) => current + (row["payer"] + ", ")).TrimEnd(' ', ',');
            }

            return namesPayer;
        }


        private void PreparePrefsAndTerritoryHeader()
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
        }


        private void PrepareAreaHeader()
        {
            var result = String.Empty;
            if (Areas != null)
            {
                result = Areas.Aggregate(result, (current, nzpArea) => current + (nzpArea + ","));
            }
            else
            {
                result = ReportParams.GetRolesCondition(Constants.role_sql_area);
            }

            result = result.TrimEnd(',');
            if (!String.IsNullOrEmpty(result))
            {
                result = " AND nzp_area in (" + result + ")";

                AreaHeader = String.Empty;
                var sql = " SELECT area from " +
                      ReportParams.Pref + DBManager.sDataAliasRest + "s_area " + 
                      " WHERE nzp_area > 0 " + result;
                var area = ExecSQLToTable(sql);
                foreach (DataRow dr in area.Rows)
                {
                    AreaHeader += dr["area"].ToString().Trim() + ", ";
                }
                AreaHeader = AreaHeader.TrimEnd(',', ' ');
            }
        }


        /// <summary>Ограничение по адресу</summary>
        private void GetWhereAdr()
        {
            string
                area = string.Empty,
                rajon = String.Empty,
                street = String.Empty,
                house = String.Empty;
            string prefData = ReportParams.Pref + DBManager.sDataAliasRest;

            string result = ReportParams.GetRolesCondition(Constants.role_sql_area);


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

            if (Areas != null) area = Areas.Aggregate(area, (current, nzpArea) => current + (nzpArea + ",")).TrimEnd(',');
            if (Raions != null) rajon = Raions.Aggregate(rajon, (current, nzpRajon) => current + (nzpRajon + ",")).TrimEnd(',');
            if (Streets != null) street = Streets.Aggregate(street, (current, nzpStreet) => current + (nzpStreet + ",")).TrimEnd(',');
            if (Houses != null) house = Houses.Aggregate(house, (current, nzpHouse) => current + (nzpHouse + ",")).TrimEnd(',');
            result = result.TrimEnd(',');
            result = !String.IsNullOrEmpty(result) ? " AND k.nzp_area in (" + result + ")" : string.Empty;
            result += !String.IsNullOrEmpty(whereWp) ? " AND k.nzp_wp IN ( " + whereWp + ") " : string.Empty;
            result += !String.IsNullOrEmpty(rajon) ? " AND r.nzp_raj IN ( " + rajon + ") " : string.Empty;
            result += !String.IsNullOrEmpty(street) ? " AND u.nzp_ul IN ( " + street + ") " : string.Empty;
            result += !String.IsNullOrEmpty(house) ? " AND d.nzp_dom IN ( " + house + ") " : string.Empty;
            result += !String.IsNullOrEmpty(area) ? " AND k.nzp_area IN ( " + area + ") " : string.Empty;

            ExecSQL("DROP TABLE t_12_3_8_kvar");

            string sql = " SELECT k.nzp_kvar, k.nzp_area, r.nzp_raj INTO TEMP t_12_3_8_kvar " +
                         " FROM " + prefData + "kvar k INNER JOIN " + prefData + "dom d ON d.nzp_dom = k.nzp_dom " +
                                                     " INNER JOIN " + prefData + "s_ulica u ON u.nzp_ul = d.nzp_ul " +
                                                     " INNER JOIN " + prefData + "s_rajon r ON r.nzp_raj = u.nzp_raj " +
                         " WHERE nzp_kvar > 0 " + result;
            ExecSQL(sql);
            ExecSQL("create index ix_tmpkv_97 on t_12_3_8_kvar(nzp_kvar)");
            ExecSQL("create index ix_tmpkv_98 on t_12_3_8_kvar(nzp_area)");
            ExecSQL("analyze t_12_3_8_kvar");

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
        #endregion

		protected override void PrepareReport(FastReport.Report report) {
            var months = new[] {"","Январь","Февраль",
                 "Март","Апрель","Май","Июнь","Июль","Август","Сентябрь",
                 "Октябрь","Ноябрь","Декабрь"};
          
            report.SetParameterValue("period_month", months[Month] + " " + Year);

		    PrepareAreaHeader();
            string headerParam = !string.IsNullOrEmpty(TerritoryHeader) ? "Территория: " + TerritoryHeader + "\n" : string.Empty;
            headerParam += !string.IsNullOrEmpty(AgentHeader) ? "Агенты: " + AgentHeader + "\n" : string.Empty;
            headerParam += !string.IsNullOrEmpty(PrincipalHeader) ? "Принципалы: " + PrincipalHeader + "\n" : string.Empty;
            headerParam += !string.IsNullOrEmpty(SupplierHeader) ? "Поставщики: " + SupplierHeader + "\n" : string.Empty;
            headerParam += !string.IsNullOrEmpty(AddressHeader) ? "Адрес: " + AddressHeader + "\n" : string.Empty;
            headerParam += !string.IsNullOrEmpty(AreaHeader) ? "УО: " + AreaHeader + "\n" : string.Empty;
			headerParam = headerParam.TrimEnd('\n');
			report.SetParameterValue("headerParam", headerParam);

            
		}
        

		protected override void PrepareParams() {
            Month = UserParamValues["Month"].GetValue<int>();
            Year = UserParamValues["Year"].Value.To<int>();
			BankSupplier = JsonConvert.DeserializeObject<BankSupplierParameterValue>(UserParamValues["BankSupplier"].Value.ToString());

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

            Areas = UserParamValues["Areas"].GetValue<List<int>>();
		}

		
        protected override void CreateTempTable()
        {    
            string sql;

            sql = " CREATE TEMP TABLE t_svod ( " +
                  " nzp_serv integer, " +
                  " service CHAR(100)," +
                  " nzp_supp_princip integer," +
                  " principal CHAR(40)," +
                  " sum_insaldo " + DBManager.sDecimalType + "(14,2)," +
                  " sum_real " + DBManager.sDecimalType + "(14,2), " +
                  " reval " + DBManager.sDecimalType + "(14,2), " +
                  " sum_money " + DBManager.sDecimalType + "(14,2), " +
                  " percent " + DBManager.sDecimalType + "(14,2), " +
                  " total_saldo " + DBManager.sDecimalType + "(14,2) " +
                  " ) " + DBManager.sUnlogTempTable;
            ExecSQL(sql);

		}

		protected override void DropTempTable() {
            ExecSQL(" DROP TABLE t_svod ");
		}
	}
}
