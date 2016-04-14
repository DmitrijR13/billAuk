using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Bars.KP50.Report.Base;
using Bars.KP50.Report.Base.Parameters;
using Bars.KP50.Report.RT.Properties;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;

namespace Bars.KP50.Report.RT.Reports
{
	class Report16651 : BaseSqlReport
	{
		public override string Name {
			get { return "16.6.5.1 Поквартирный список"; }
		}

		public override string Description {
			get { return "Поквартирный список"; }
		}

		public override IList<ReportGroup> ReportGroups {
			get { return new List<ReportGroup> { ReportGroup.Reports }; }
		}

		public override bool IsPreview {
			get { return false; }
		}

		protected override byte[] Template {
			get { return Resources.Report_16_6_5_1; }
		}

		public override IList<ReportKind> ReportKinds {
			get { return new List<ReportKind> { ReportKind.Base }; }
		}

		//~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

		/// <summary> Период - с </summary>
		protected DateTime DatS { get; set; }

		/// <summary> Период -  по </summary>
		protected DateTime DatPo { get; set; }

		/// <summary>Территория</summary>
		protected List<int> Banks { get; set; }

		/// <summary>УК</summary>
		protected List<int> Area { get; set; }

		/// <summary>Заголовок территории</summary>
		protected string TerritoryHeader { get; set; }

		/// <summary>УК</summary>
		protected string AreaHeader { get; set; }

		//~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

		public override List<UserParam> GetUserParams() {
			return new List<UserParam>
			{
				new PeriodParameter(DateTime.Now,DateTime.Now),
				new BankParameter(),
				new AreaParameter()
			};
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
			report.SetParameterValue("date", DateTime.Now.ToShortDateString());
			report.SetParameterValue("time", DateTime.Now.ToShortTimeString());

			string headerParam = !string.IsNullOrEmpty(TerritoryHeader) ? "Территория: " + TerritoryHeader + "\n" : string.Empty;
			headerParam += !string.IsNullOrEmpty(AreaHeader) ? "Балансодержатель: " + AreaHeader + "\n" : string.Empty;
			headerParam = headerParam.TrimEnd('\n');
			report.SetParameterValue("headerParam", headerParam);
		}

		public override DataSet GetData() {
			MyDataReader reader;

			string prefCentralData = ReportParams.Pref + DBManager.sDataAliasRest;

			#region запонение временной таблицы
			string sql = " INSERT INTO t_kvar_16_6_5_1(nzp_kvar, nkvar, ndom, nkor, ulica, rajon, town, fio)" +
						 " SELECT nzp_kvar, nkvar, ndom, nkor, ulica, rajon, town, fio " +
						 " FROM " + prefCentralData + "kvar k INNER JOIN " +
									prefCentralData + "dom d ON k.nzp_dom = d.nzp_dom INNER JOIN " +
									prefCentralData + "s_ulica u ON u.nzp_ul = d.nzp_ul INNER JOIN " +
									prefCentralData + "s_rajon r ON r.nzp_raj = u.nzp_raj INNER JOIN " +
									prefCentralData + "s_town t ON t.nzp_town = r.nzp_town " +
						 " WHERE k.nzp_kvar > 0 " + GetWhereWp("k.") + GetWhereArea("k.");
			ExecSQL(sql);

			sql = " SELECT * FROM s_point " +
				  " WHERE nzp_wp > 0 " + GetWhereWp(string.Empty);
			ExecRead(out reader, sql);

			while (reader.Read())
			{
				string pref = reader["bd_kernel"].ToString().ToLower().Trim(),
						prefData = pref + DBManager.sDataAliasRest;

				//общая площадь
				sql = " UPDATE t_kvar_16_6_5_1 SET ob_pl = " +
					  " (SELECT MAX(REPLACE(val_prm,',','.') " + DBManager.sConvToNum + ") " +
					   " FROM " + prefData + "prm_1 p " +
					   " WHERE p.nzp = t_kvar_16_6_5_1.nzp_kvar" +
						 " AND nzp_prm = 4 " +
						 " AND is_actual <> 100 " +
						 " AND dat_s <= DATE('" + DatPo.ToShortDateString() + "')" +
						 " AND dat_po >= DATE('" + DatS.ToShortDateString() + "')) WHERE ob_pl IS NULL  ";
				ExecSQL(sql);

				//привитизированная площадь
				sql = " UPDATE t_kvar_16_6_5_1 SET privat = 1 " +
					  " WHERE 1 = (SELECT MAX(REPLACE(val_prm,',','.') " + DBManager.sConvToNum + ") " +
					   " FROM " + prefData + "prm_1 p " +
					   " WHERE p.nzp = t_kvar_16_6_5_1.nzp_kvar" +
						 " AND nzp_prm = 8 " +
						 " AND is_actual <> 100 " +
						 " AND dat_s <= DATE('" + DatPo.ToShortDateString() + "')" +
						 " AND dat_po >= DATE('" + DatS.ToShortDateString() + "')) ";
				ExecSQL(sql);

#if PG
				for (int i = DatS.Year * 12 + DatS.Month; i < DatPo.Year * 12 + DatPo.Month + 1; i++)
				{
					var year = i / 12;
					var month = i % 12;
					if (month == 0)
					{
						year--;
						month = 12;
					}
					string gilXX = pref + "_charge_" + (year - 2000).ToString("00") + DBManager.tableDelimiter + "gil_" +
								   month.ToString("00");

					if (TempTableInWebCashe(gilXX))
					{
						//прописано жильцов
						sql = " UPDATE t_kvar_16_6_5_1 SET gil = " +
							  " (SELECT MAX(val1 + val3 - val5) " +
							  " FROM " + gilXX + " g " +
							  " WHERE g.nzp_kvar = t_kvar_16_6_5_1.nzp_kvar" +
							  " AND stek = 3 " +
							  " AND dat_charge IS NULL " +
							  " AND dat_s <= DATE('" + DatPo.ToShortDateString() + "')" +
							  " AND dat_po >= DATE('" + DatS.ToShortDateString() + "')) WHERE gil IS NULL ";
						ExecSQL(sql);
					}
				}
#else
				sql = " UPDATE t_kvar_16_6_5_1 " +
					  " SET gil = " + prefData + "get_kol_gil(DATE('" + DatS.ToShortDateString() + "'), "+
															" DATE('" + DatPo.ToShortDateString() + "'), 15, t_kvar_16_6_5_1.nzp_kvar) WHERE gil IS NULL ";
				ExecSQL(sql);
#endif
            }
			reader.Close();
			#endregion

			sql = " SELECT TRIM(town) AS town, " +
						 " TRIM(rajon) AS rajon, " +
						 " TRIM(ulica) AS ulica, " +
						 " TRIM(ndom) AS ndom, " +
						 " TRIM(nkor) AS nkor, " +
						 " TRIM(nkvar) AS nkvar, " +
						 " TRIM(fio) AS fio, " +
						 " ob_pl, privat, gil " +
				  " FROM t_kvar_16_6_5_1 " +
				  " ORDER BY 1,2,3,4 ";
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
			const string sql = " CREATE TEMP TABLE t_kvar_16_6_5_1(" +
				   " nzp_kvar INTEGER, " +
				   " town CHARACTER(30), " +
				   " rajon CHARACTER(30), " +
				   " ulica CHARACTER(40), " +
				   " ndom CHARACTER(10), " +
				   " nkor CHARACTER(3), " +
				   " nkvar CHARACTER(10), " +
				   " fio CHARACTER(50), " +
				   " ob_pl " + DBManager.sDecimalType + "(14,2), " +
				   " privat INTEGER DEFAULT 0 , " +
				   " gil INTEGER) " + DBManager.sUnlogTempTable;
			ExecSQL(sql);
		}

		protected override void DropTempTable() {
			ExecSQL(" DROP TABLE t_kvar_16_6_5_1 ");
		}
	}
}
