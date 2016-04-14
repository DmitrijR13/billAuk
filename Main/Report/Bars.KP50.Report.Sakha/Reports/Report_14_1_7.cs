using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Newtonsoft.Json;
using Bars.KP50.Report.Base;
using Bars.KP50.Report.Sakha.Properties;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;

namespace Bars.KP50.Report.Sakha.Reports
{
	public class Report1417 : BaseSqlReport
	{
		public override string Name {
			get { return "14.1.7 Реестр снятий"; }
		}

		public override string Description {
			get { return "Реестр снятий"; }
		}

		public override IList<ReportGroup> ReportGroups {
			get { return new List<ReportGroup> { ReportGroup.Reports }; }
		}

		public override bool IsPreview {
			get { return false; }
		}

		protected override byte[] Template {
			get { return Resources.Report_14_1_7; }
		}
		public override IList<ReportKind> ReportKinds {
			get { return new List<ReportKind> { ReportKind.Base }; }
		}

		//~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

		/// <summary>Заголовок отчета</summary>
		private string SupplierHeader { get; set; }

		/// <summary>Заголовок территории</summary>
		private string TerritoryHeader { get; set; }

		/// <summary>Заголовок отчета</summary>
		private string AreaHeader { get; set; }

		/// <summary>Районы</summary>
		protected string AddressHeader { get; set; }

		private int Year { get; set; }

		/// <summary> Месяц </summary>
		private int Month { get; set; }

		/// <summary>Управляющие компании</summary>
		private List<int> Areas { get; set; }

		/// <summary>Поставщики, Агенты, Принципалы  </summary>
		private BankSupplierParameterValue BankSupplier { get; set; }

		/// <summary>Адрес</summary>
		protected AddressParameterValue Address { get; set; }

		//~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

		public override List<UserParam> GetUserParams() {
			return new List<UserParam>
			{
				new YearParameter {Value = DateTime.Now.Year},
				new MonthParameter {Value = DateTime.Now.Month},
				new BankSupplierParameter(),
				new AreaParameter(),
				new AddressParameter()
			};
		}

		protected override void PrepareParams() {
			Year = UserParamValues["Year"].GetValue<int>();
			Month = UserParamValues["Month"].GetValue<int>();
			Areas = UserParamValues["Areas"].GetValue<List<int>>();
			BankSupplier = JsonConvert.DeserializeObject<BankSupplierParameterValue>(UserParamValues["BankSupplier"].Value.ToString());
			Address = UserParamValues["Address"].GetValue<AddressParameterValue>();
		}

		protected override void PrepareReport(FastReport.Report report) {
			var month = new[] {"", "Январь", "Февраль", "Март", "Апрель", "Май", "Июнь", "Июль", 
									"Август", "Сентябрь", "Октябрь", "Ноябрь", "Декабрь"};
			report.SetParameterValue("period", "За " + month[Month] + " месяц " + Year);

			string headerParam = !string.IsNullOrEmpty(TerritoryHeader) ? "Территория: " + TerritoryHeader + "\n" : string.Empty;
			headerParam += !string.IsNullOrEmpty(SupplierHeader) ? "Поставщики: " + SupplierHeader + "\n" : string.Empty;
			headerParam += !string.IsNullOrEmpty(AreaHeader) ? "Балансодержатель: " + AreaHeader + "\n" : string.Empty;
			headerParam += string.IsNullOrEmpty(AddressHeader) ? string.Empty : "Адрес: " + AddressHeader + "\n";
			headerParam = headerParam.TrimEnd('\n');
			report.SetParameterValue("headerParam", headerParam);
		}

		public override DataSet GetData() {
			MyDataReader reader;
			DateTime nowDate = DateTime.Now;
			string whereAddress = GetWhereAdr(),
					whereArea = GetWhereArea("k."),
					  whereSupplier = GetWhereSupp();

			var sql = " SELECT * FROM  " + ReportParams.Pref + DBManager.sKernelAliasRest + "s_point " +
					  " WHERE nzp_wp > 1 " + GetWhereWp();
			ExecRead(out reader, sql);

			while (reader.Read())
			{
				var pref = reader["bd_kernel"].ToString().ToLower().Trim();
				string prefData = pref + DBManager.sDataAliasRest,
						prefKernel = pref + DBManager.sKernelAliasRest;
				var chargeYY = pref + "_charge_" + (nowDate.Year - 2000).ToString("00") + DBManager.tableDelimiter +
							   "charge_" + nowDate.Month.ToString("00");
				if (TempTableInWebCashe(chargeYY))
				{
					sql = " INSERT INTO t_report_14_1_7(nzp_kvar, service, ulica, nzp_dom, idom, ndom, nkor, ikvar, nkvar, sum_reestr) " +
						  " SELECT c.nzp_kvar, service, ulica, k.nzp_dom, idom, ndom, nkor, ikvar, nkvar, SUM(real_charge - sum_tarif) AS sum_reestr " +
						  " FROM " + chargeYY + " c INNER JOIN " + prefData + "kvar k ON k.nzp_kvar = c.nzp_kvar " +
												  " INNER JOIN " + prefData + "dom d ON d.nzp_dom = k.nzp_dom " +
												  " INNER JOIN " + prefData + "s_ulica u ON u.nzp_ul = d.nzp_ul " +
												  " INNER JOIN " + prefKernel + "services s ON s.nzp_serv = c.nzp_serv " +
						  " WHERE c.dat_charge IS NULL AND c.nzp_serv > 0 " + whereAddress + whereArea + whereSupplier +
						  " GROUP BY 1,2,3,4,5,6,7,8,9 ";
					ExecSQL(sql);
				}
			}

			reader.Close();

			sql = " SELECT nzp_kvar, TRIM(service) AS service, TRIM(ulica) AS ulica, nzp_dom, idom, TRIM(ndom) AS ndom, " +
						 " TRIM(nkor) AS nkor, ikvar, TRIM(nkvar) AS nkvar, SUM(sum_reestr) AS sum_reestr " +
				  " FROM t_report_14_1_7 t " +
				  " GROUP BY 1,2,3,4,5,6,7,8,9 " +
				  " ORDER BY 3, 5, 7, 8 ";

			DataTable dt = ExecSQLToTable(sql);
			dt.TableName = "Q_master";
			var ds = new DataSet();
			ds.Tables.Add(dt);
			return ds;
		}

		/// <summary> Ограничение по территории </summary>
		private string GetWhereWp() {
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
			whereWp = !String.IsNullOrEmpty(whereWp) ? " AND nzp_wp in (" + whereWp + ")" : String.Empty;
			if (!string.IsNullOrEmpty(whereWp))
			{
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

		/// <summary> Ограгичение по адресу(район, улица, дом) </summary>
		private string GetWhereAdr() {
			var result = String.Empty;
			string rajon = String.Empty,
				street = String.Empty,
				house = String.Empty;
			string prefData = ReportParams.Pref + DBManager.sDataAliasRest;
			if (Areas != null)
			{
				result = Areas.Aggregate(result, (current, nzpArea) => current + (nzpArea + ","));
			}
			else
			{
				result = ReportParams.GetRolesCondition(Constants.role_sql_area);
			}

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

		/// <summary> Ограничение по УК </summary>
		private string GetWhereArea(string pref) {
			var result = String.Empty;
			result = Areas != null
				? Areas.Aggregate(result, (current, nzpArea) => current + (nzpArea + ","))
				: ReportParams.GetRolesCondition(Constants.role_sql_area);

			result = result.TrimEnd(',');
			if (!String.IsNullOrEmpty(result))
			{
				result = " AND " + pref + "nzp_area in (" + result + ")";

				AreaHeader = String.Empty;
				var sql = " SELECT area from " +
					  ReportParams.Pref + DBManager.sDataAliasRest + "s_area " + pref.TrimEnd('.') +
					  " WHERE nzp_area > 0 " + result;
				var area = ExecSQLToTable(sql);
				foreach (DataRow dr in area.Rows)
				{
					AreaHeader += dr["area"].ToString().Trim() + ", ";
				}
				AreaHeader = AreaHeader.TrimEnd(',', ' ');
			}
			return result;
		}

		/// <summary> Ограничение по поставщикам </summary>
		/// <returns></returns>
		private string GetWhereSupp() {
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
			return " and nzp_supp in (select nzp_supp from " +
				   ReportParams.Pref + DBManager.sKernelAliasRest + "supplier " +
				   " where nzp_supp>0 " + whereSupp + ")";
		}

		protected override void CreateTempTable() {
			string sql = " CREATE TEMP TABLE t_report_14_1_7(" +
						 " nzp_kvar INTEGER, " +
						 " service CHARACTER(100), " +
						 " ulica CHARACTER(40), " +
						 " nzp_dom INTEGER, " +
						 " idom INTEGER, " +
						 " ndom CHARACTER(10), " +
						 " nkor CHARACTER(3), " +
						 " ikvar INTEGER, " +
						 " nkvar CHARACTER(10), " +
						 " sum_reestr " + DBManager.sDecimalType + "(14,2)) " + DBManager.sUnlogTempTable;
			ExecSQL(sql);
		}

		protected override void DropTempTable() {
			ExecSQL("DROP TABLE t_report_14_1_7 ");
		}
	}
}
