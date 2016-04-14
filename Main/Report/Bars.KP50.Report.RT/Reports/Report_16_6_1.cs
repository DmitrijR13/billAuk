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
	class Report1661 : BaseSqlReport
	{
		public override string Name {
			get { return "16.6.1 Характеристики жилого фонда"; }
		}

		public override string Description {
			get { return "Характеристики жилого фонда"; }
		}

		public override IList<ReportGroup> ReportGroups {
			get { return new List<ReportGroup> { ReportGroup.Reports }; }
		}

		public override bool IsPreview {
			get { return false; }
		}

		protected override byte[] Template {
			get { return Resources.Report_16_6_1; }
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
				new PeriodParameter(DateTime.Now, DateTime.Now),
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

			string whereWp = GetWhereWp(),
					whereArea = GetWhereArea();

			#region запонение временной таблицы
			string sql = " INSERT INTO t_kvar_16_6_1(nzp_dom, idom, nzp_area, ndom, ulica)" +
						 " SELECT nzp_dom, " +
								" idom, " +
								" nzp_area, " +
								" ndom, " +
								" ulica " +
						 " FROM " + ReportParams.Pref + DBManager.sDataAliasRest + "dom d INNER JOIN " +
									ReportParams.Pref + DBManager.sDataAliasRest + "s_ulica u ON d.nzp_ul = u.nzp_ul" +
						 " WHERE d.nzp_dom > 0 " + whereWp + whereArea;
			ExecSQL(sql);

			sql = " UPDATE t_kvar_16_6_1 SET area = " +
				  " (SELECT area FROM " + ReportParams.Pref + DBManager.sDataAliasRest + "s_area a " +
				  " WHERE a.nzp_area = t_kvar_16_6_1.nzp_area) ";
			ExecSQL(sql);

			sql = " SELECT * FROM s_point " +
				  " WHERE nzp_wp > 0 " + whereWp;
			ExecRead(out reader, sql);

			while (reader.Read())
			{
				string pref = reader["bd_kernel"].ToString().ToLower().Trim(),
						prefData = pref + DBManager.sDataAliasRest;

				//котельная
				sql = " UPDATE t_kvar_16_6_1 SET katel = " +
					  " (SELECT val_prm " +
					   " FROM " + prefData + "prm_2 p " +
					   " WHERE p.nzp = t_kvar_16_6_1.nzp_dom" +
						 " AND nzp_prm = 1179 " +
						 " AND is_actual <> 100 " +
						 " AND dat_s <= DATE('" + DatPo.ToShortDateString() + "')" +
						 " AND dat_po >= DATE('" + DatS.ToShortDateString() + "')) WHERE katel IS NULL ";
				ExecSQL(sql);

				//объем жилого дома
				sql = " UPDATE t_kvar_16_6_1 SET ob_dom = " +
					  " (SELECT MAX(REPLACE(val_prm,',','.') " + DBManager.sConvToNum + ") " +
					   " FROM " + prefData + "prm_2 p " +
					   " WHERE p.nzp = t_kvar_16_6_1.nzp_dom" +
						 " AND nzp_prm = 32 " +
						 " AND is_actual <> 100 " +
						 " AND dat_s <= DATE('" + DatPo.ToShortDateString() + "')" +
						 " AND dat_po >= DATE('" + DatS.ToShortDateString() + "')) WHERE ob_dom IS NULL ";
				ExecSQL(sql);

				//площадь жилого дома
				sql = " UPDATE t_kvar_16_6_1 SET pl_dom = " +
					  " (SELECT MAX(REPLACE(val_prm,',','.') " + DBManager.sConvToNum + ") " +
					   " FROM " + prefData + "prm_2 p " +
					   " WHERE p.nzp = t_kvar_16_6_1.nzp_dom" +
						 " AND nzp_prm = 40 " +
						 " AND is_actual <> 100 " +
						 " AND dat_s <= DATE('" + DatPo.ToShortDateString() + "')" +
						 " AND dat_po >= DATE('" + DatS.ToShortDateString() + "')) WHERE pl_dom IS NULL ";
				ExecSQL(sql);

				//норматив потребления
				sql = " UPDATE t_kvar_16_6_1 SET norm = " +
					  " (SELECT MAX(REPLACE(val_prm,',','.') " + DBManager.sConvToNum + ") " +
					   " FROM " + prefData + "prm_2 p " +
					   " WHERE p.nzp = t_kvar_16_6_1.nzp_dom" +
						 " AND nzp_prm = 723 " +
						 " AND is_actual <> 100 " +
						 " AND dat_s <= DATE('" + DatPo.ToShortDateString() + "') " +
						 " AND dat_po >= DATE('" + DatS.ToShortDateString() + "')) WHERE norm IS NULL ";
				ExecSQL(sql);
			}
			reader.Close();
			#endregion

			ExecSQL(" UPDATE t_kvar_16_6_1 SET katel = 'Котельная не определена' WHERE katel IS NULL ");

			sql = " SELECT TRIM(katel) AS katel, " +
						 " TRIM(area) AS area, " +
						 " TRIM(ulica) AS ulica, " +
						 " idom, " +
						 " TRIM(ndom) AS ndom, " +
						 " ob_dom, pl_dom, norm " +
				  " FROM t_kvar_16_6_1 " +
				  " ORDER BY 1,2,3,4,5 ";
			DataTable dt = ExecSQLToTable(sql);
			dt.TableName = "Q_master";

			var ds = new DataSet();
			ds.Tables.Add(dt);

			return ds;
		}

		/// <summary> Получить условия органичения по банкам данных </summary>
		private string GetWhereWp() {
			string whereWp = String.Empty;
			whereWp = Banks != null
				? Banks.Aggregate(whereWp, (current, nzpWp) => current + (nzpWp + ","))
				: ReportParams.GetRolesCondition(Constants.role_sql_wp);
			whereWp = whereWp.TrimEnd(',');
			whereWp = !String.IsNullOrEmpty(whereWp) ? " AND nzp_wp in (" + whereWp + ")" : String.Empty;
			if (string.IsNullOrEmpty(TerritoryHeader) && !string.IsNullOrEmpty(whereWp))
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

		/// <summary> Получить условия органичения по УК </summary>
		private string GetWhereArea() {
			string whereArea = String.Empty;
			whereArea = Area != null ? Area.Aggregate(whereArea, (current, nzpArea) => current + (nzpArea + ",")) : ReportParams.GetRolesCondition(Constants.role_sql_area);
			whereArea = whereArea.TrimEnd(',');
			whereArea = !String.IsNullOrEmpty(whereArea) ? " AND nzp_area in (" + whereArea + ")" : String.Empty;
			if (String.IsNullOrEmpty(AreaHeader) && !string.IsNullOrEmpty(whereArea))
			{
				string sql = " SELECT area from " + ReportParams.Pref + DBManager.sDataAliasRest + "s_area  WHERE nzp_area > 0 " + whereArea;
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
			const string sql = " CREATE TEMP TABLE t_kvar_16_6_1(" +
							   " nzp_dom INTEGER, " +
							   " idom INTEGER, " +
							   " nzp_area INTEGER, " +
							   " katel CHARACTER(40), " +
							   " area CHARACTER(40), " +
							   " ulica CHARACTER(40), " +
							   " ndom CHARACTER(10), " +
							   " ob_dom " + DBManager.sDecimalType + "(14,2), " +
							   " pl_dom " + DBManager.sDecimalType + "(14,2), " +
							   " norm " + DBManager.sDecimalType + "(14,2)) " + DBManager.sUnlogTempTable;
			ExecSQL(sql);
		}

		protected override void DropTempTable() {
			ExecSQL(" DROP TABLE t_kvar_16_6_1 ");
		}
	}
}
