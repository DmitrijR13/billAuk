using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Bars.KP50.Report.Base;
using Bars.KP50.Report.Base.Parameters;
using STCLINE.KP50.DataBase;
using Bars.KP50.Report.RT.Properties;
using STCLINE.KP50.Global;

namespace Bars.KP50.Report.RT.Reports
{
	class Report16612 : BaseSqlReport
	{
		public override string Name {
			get { return "16.6.12 Список домов с указанием этажей"; }
		}

		public override string Description {
			get { return "6.11 Список домов с указанием этажей"; }
		}

		public override IList<ReportGroup> ReportGroups {
			get {
				var result = new List<ReportGroup> { ReportGroup.Reports };
				return result;
			}
		}

		public override bool IsPreview {
			get { return false; }
		}

		protected override byte[] Template {
			get { return Resources.Report_16_6_12; }
		}

		public override IList<ReportKind> ReportKinds {
			get { return new List<ReportKind> { ReportKind.Base }; }
		}

		//~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

		/// <summary>Управляющие компании</summary>
		protected List<int> Area { get; set; }

		/// <summary>Территория</summary>
		protected List<int> Banks { get; set; }

		/// <summary>Дата с</summary>
		protected DateTime DatS { get; set; }

		/// <summary>Дата по</summary>
		protected DateTime DatPo { get; set; }

		/// <summary>Управляющие компании</summary>
		protected string AreaHeader { get; set; }

		/// <summary>Заголовок территории</summary>
		protected string TerritoryHeader { get; set; }

		//~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

		public override List<UserParam> GetUserParams() {
			return new List<UserParam>
			{
				new PeriodParameter(DateTime.Now,DateTime.Now),
				new BankParameter(),
				new AreaParameter(),
			};
		}

		protected override void PrepareReport(FastReport.Report report) {
			string period;

			if (DatS == DatPo)
			{
				period = "на" + DatS.ToShortDateString() + " г.";
			}
			else
			{
				period = "на период с " + DatS.ToShortDateString() + " г. по " + DatPo.ToShortDateString() + " г.";
			}
			report.SetParameterValue("period", period);
			report.SetParameterValue("printDate", DateTime.Now.ToLongDateString());
			report.SetParameterValue("printTime", DateTime.Now.ToLongTimeString());

			string headerParam = !string.IsNullOrEmpty(TerritoryHeader) ? "Территория: " + TerritoryHeader + "\n" : string.Empty;
			headerParam += !string.IsNullOrEmpty(AreaHeader) ? "Балансодержатель: " + AreaHeader + "\n" : string.Empty;
			headerParam = headerParam.TrimEnd('\n');
			report.SetParameterValue("headerParam", headerParam);
		}

		protected override void PrepareParams() {
			var period = UserParamValues["Period"].GetValue<string>();
			DateTime d1;
			DateTime d2;
			PeriodParameter.GetValues(period, out d1, out d2);
			DatS = d1;
			DatPo = d2;
			Area = UserParamValues["Areas"].GetValue<List<int>>();
			Banks = UserParamValues["Banks"].GetValue<List<int>>();
		}

		public override DataSet GetData() {
			MyDataReader reader;

			string whereWp = GetWhereWp("k."),
					whereArea = GetWhereArea("k.");

			string prefCentralData = ReportParams.Pref + DBManager.sDataAliasRest;

			#region запонение временной таблицы
			string sql = " INSERT INTO t_dom_16_6_12(nzp_dom, ndom, idom, nkor, ulica, rajon, town, kol_kv, kol_ls) " +
						 " SELECT d.nzp_dom, ndom, idom, nkor, ulica, rajon, town, COUNT(DISTINCT nkvar) AS kol_kv, COUNT(num_ls) AS kol_ls " +
						 " FROM " + prefCentralData + "kvar k INNER JOIN " +
									prefCentralData + "dom d ON k.nzp_dom = d.nzp_dom INNER JOIN " +
									prefCentralData + "s_ulica u ON u.nzp_ul = d.nzp_ul INNER JOIN " +
									prefCentralData + "s_rajon r ON r.nzp_raj = u.nzp_raj INNER JOIN " +
									prefCentralData + "s_town t ON t.nzp_town = r.nzp_town " +
						 " WHERE k.nzp_kvar > 0 " + whereWp + whereArea +
						 " GROUP BY 1,2,3,4,5,6,7 ";
			ExecSQL(sql);
			ExecSQL(DBManager.sUpdStat + " t_dom_16_6_12");

			sql = " SELECT * FROM s_point k " +
				  " WHERE nzp_wp > 0 " + whereWp;
			ExecRead(out reader, sql);

			while (reader.Read())
			{
				string pref = reader["bd_kernel"].ToString().ToLower().Trim(),
						prefData = pref + DBManager.sDataAliasRest;
				string calcGkuTable = pref + "_charge_" + (DatS.Year - 2000).ToString("00") + DBManager.tableDelimiter +
								 "calc_gku_" + DatS.Month.ToString("00");

				if (TempTableInWebCashe(calcGkuTable))
				{
					//кол-во жильцов
					sql = " UPDATE t_dom_16_6_12 SET kolgil = (" +
						  " SELECT MAX(ROUND(gil))" +
						  " FROM " + calcGkuTable + " cg " +
						  " WHERE t_dom_16_6_12.nzp_dom = cg.nzp_dom) WHERE kolgil IS NULL ";
					ExecSQL(sql);
				}

				//площадь жилого дома
				sql = " UPDATE t_dom_16_6_12 SET pl_dom = " +
					  " (SELECT MAX(REPLACE(val_prm,',','.') " + DBManager.sConvToNum + ") " +
					   " FROM " + prefData + "prm_2 p " +
					   " WHERE p.nzp = t_dom_16_6_12.nzp_dom" +
						 " AND nzp_prm = 40 " +
						 " AND is_actual <> 100 " +
						 " AND dat_s <= DATE('" + DatPo.ToShortDateString() + "')" +
						 " AND dat_po >= DATE('" + DatS.ToShortDateString() + "')) WHERE pl_dom IS NULL ";
				ExecSQL(sql);

				//кол-во этажей
				sql = " UPDATE t_dom_16_6_12 SET kol_etazh = " +
					  " (SELECT MAX(REPLACE(val_prm,',','.') " + DBManager.sConvToNum + ") " +
					   " FROM " + prefData + "prm_2 p " +
					   " WHERE p.nzp = t_dom_16_6_12.nzp_dom" +
						 " AND nzp_prm = 37 " +
						 " AND is_actual <> 100 " +
						 " AND dat_s <= DATE('" + DatPo.ToShortDateString() + "')" +
						 " AND dat_po >= DATE('" + DatS.ToShortDateString() + "')) WHERE kol_etazh IS NULL ";
				ExecSQL(sql);

				//кол-во подъездов
				sql = " UPDATE t_dom_16_6_12 SET kol_podezd = " +
					  " (SELECT MAX(REPLACE(val_prm,',','.') " + DBManager.sConvToNum + ") " +
					   " FROM " + prefData + "prm_2 p " +
					   " WHERE p.nzp = t_dom_16_6_12.nzp_dom" +
						 " AND nzp_prm = 41 " +
						 " AND is_actual <> 100 " +
						 " AND dat_s <= DATE('" + DatPo.ToShortDateString() + "')" +
						 " AND dat_po >= DATE('" + DatS.ToShortDateString() + "')) WHERE kol_podezd IS NULL ";
				ExecSQL(sql);
			}
			reader.Close();
			#endregion

			sql = " SELECT TRIM(town) AS town, " +
						 " TRIM(rajon) AS rajon, " +
						 " TRIM(ulica) AS ulica, " +
						 " idom, " +
						 " TRIM(ndom) AS ndom, " +
						 " TRIM(nkor) AS nkor, " +
						 " pl_dom, kol_etazh , kol_podezd, kol_kv, kol_ls, kolgil " +
				  " FROM t_dom_16_6_12 " +
				  " ORDER BY 1,2,3,4,5,6 ";
			DataTable dt = ExecSQLToTable(sql);
			dt.TableName = "Q_master";

			var ds = new DataSet();
			ds.Tables.Add(dt);

			return ds;
		}

		/// <summary> Получить условия органичения по банкам данных </summary>
		private string GetWhereWp(string pref) {
			string whereWp = String.Empty;
			whereWp = Banks != null
				? Banks.Aggregate(whereWp, (current, nzpWp) => current + (nzpWp + ","))
				: ReportParams.GetRolesCondition(Constants.role_sql_wp);
			whereWp = whereWp.TrimEnd(',');
			whereWp = !String.IsNullOrEmpty(whereWp) ? " AND " + pref + "nzp_wp in (" + whereWp + ")" : String.Empty;
			if (string.IsNullOrEmpty(TerritoryHeader) && !string.IsNullOrEmpty(whereWp))
			{
				string sql = " SELECT point FROM " + ReportParams.Pref + DBManager.sKernelAliasRest + "s_point " + pref.TrimEnd('.') + " WHERE nzp_wp > 0 " + whereWp;
				DataTable terrTable = ExecSQLToTable(sql);
				foreach (DataRow row in terrTable.Rows)
				{
					TerritoryHeader += row["point"].ToString().Trim() + ", ";
				}
				TerritoryHeader = TerritoryHeader.TrimEnd(',', ' ');
			}
			return whereWp;
		}

		/// <summary> Получить условия органичения по УК </summary>
		private string GetWhereArea(string pref) {
			string whereArea = String.Empty;
			whereArea = Area != null ? Area.Aggregate(whereArea, (current, nzpArea) => current + (nzpArea + ",")) : ReportParams.GetRolesCondition(Constants.role_sql_area);
			whereArea = whereArea.TrimEnd(',');
			whereArea = !String.IsNullOrEmpty(whereArea) ? " AND " + pref + "nzp_area in (" + whereArea + ")" : String.Empty;
			if (String.IsNullOrEmpty(AreaHeader) && !string.IsNullOrEmpty(whereArea))
			{
				string sql = " SELECT area from " + ReportParams.Pref + DBManager.sDataAliasRest + "s_area " + pref.TrimEnd('.') + "  WHERE nzp_area > 0 " + whereArea;
				DataTable area = ExecSQLToTable(sql);
				foreach (DataRow dr in area.Rows)
				{
					AreaHeader += dr["area"].ToString().Trim() + ", ";
				}
				AreaHeader = AreaHeader.TrimEnd(',', ' ');
			}
			return whereArea;
		}

		protected override void CreateTempTable() {
			const string sql = "CREATE TEMP TABLE t_dom_16_6_12 (" +
							   " nzp_dom INTEGER, " +
							   " idom INTEGER, " +
							   " ndom CHAR(10), " +
							   " nkor CHAR(3), " +
							   " nzp_ul INTEGER, " +
							   " ulica CHAR(40), " +
							   " nzp_raj INTEGER, " +
							   " rajon CHAR(30), " +
							   " nzp_town INTEGER, " +
							   " town CHAR(30), " +
							   " pl_dom " + DBManager.sDecimalType + "(14,2), " +
							   " kol_kv INTEGER, " +
							   " kol_ls INTEGER, " +
							   " kol_etazh INTEGER, " +
							   " kol_podezd INTEGER, " +
							   " kolgil INTEGER) " + DBManager.sUnlogTempTable;
			ExecSQL(sql);

			ExecSQL("CREATE INDEX ix_t_dom_16_6_12 ON t_dom_16_6_12 (nzp_dom)");
		}

		protected override void DropTempTable() {
			ExecSQL(" DROP TABLE t_dom_16_6_12 ");
		}
	}
}
