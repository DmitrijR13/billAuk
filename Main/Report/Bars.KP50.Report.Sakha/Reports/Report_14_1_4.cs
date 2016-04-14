using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Bars.KP50.Report.Base;
using Bars.KP50.Report.Base.Parameters;
using Bars.KP50.Report.Sakha.Properties;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;

namespace Bars.KP50.Report.Sakha.Reports
{
	public class Report1414 : BaseSqlReport
	{
		public override string Name {
			get { return "14.1.4 Список задолженников"; }
		}

		public override string Description {
			get { return "Список задолженников"; }
		}

		public override IList<ReportGroup> ReportGroups {
			get { return new List<ReportGroup> { ReportGroup.Reports }; }
		}

		public override bool IsPreview {
			get { return false; }
		}

		protected override byte[] Template {
			get { return Resources.Report_14_1_4; }
		}
		public override IList<ReportKind> ReportKinds {
			get { return new List<ReportKind> { ReportKind.Base }; }
		}

		//~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

		/// <summary>Услуги</summary>
		private List<int> Services { get; set; }

		/// <summary> Задолженность с </summary>
		private decimal DolgS { get; set; }

		/// <summary> Задолженность по </summary>
		private decimal DolgPo { get; set; }

		/// <summary>Территория</summary>
		protected List<int> Banks { get; set; }

		/// <summary>Заголовок территории</summary>
		protected string TerritoryHeader { get; set; }

		//~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

		public override List<UserParam> GetUserParams() {
			return new List<UserParam>
			{
				new BankParameter(),
				new ServiceParameter(),
				new StringParameter
				{
					Name = "Задолженность с",
					Code = "ZadolS",
					DefaultValue = 0m,
					Require = false,
					TypeValue = typeof(decimal),
					Value = 0m
				},
				new StringParameter
				{
					Name = "Задолженность по",
					Code = "ZadolPo",
					DefaultValue = 0m,
					Require = false,
					TypeValue = typeof(decimal),
					Value = 0m
				}
			};
		}

		protected override void PrepareParams() {
			Services = UserParamValues["Services"].GetValue<List<int>>();
			DolgS = UserParamValues["ZadolS"].GetValue<decimal>();
			DolgPo = UserParamValues["ZadolPo"].GetValue<decimal>();
			Banks = UserParamValues["Banks"].GetValue<List<int>>();
		}

		protected override void PrepareReport(FastReport.Report report) {
			report.SetParameterValue("now_date", "1." + DateTime.Now.Month.ToString("00") + "." + DateTime.Now.Year.ToString("00"));
			report.SetParameterValue("params", TerritoryHeader != string.Empty ? "Территория: " + TerritoryHeader : string.Empty);
		}



		public override DataSet GetData() {
			MyDataReader reader;
			DateTime nowDate = DateTime.Now;
			string whereServices = GetWhereServ();

			var sql = " SELECT * FROM  " + ReportParams.Pref + DBManager.sKernelAliasRest + "s_point " +
					  " WHERE nzp_wp > 1 " + GetWhereWp();

			ExecRead(out reader, sql);

			while (reader.Read())
			{
				var pref = reader["bd_kernel"].ToString().ToLower().Trim();
				var prefData = pref + DBManager.sDataAliasRest;
				var chargeYY = pref + "_charge_" + (nowDate.Year - 2000).ToString("00") + DBManager.tableDelimiter +
							   "charge_" + nowDate.Month.ToString("00");
				if (TempTableInWebCashe(chargeYY))
				{
					sql = " INSERT INTO t_report_14_1_4(nzp_kvar, ulica, nzp_dom, idom, ndom, nkor, ikvar, nkvar, sum_charge, sum_pere) " +
                          " SELECT c.nzp_kvar, ulica, k.nzp_dom, idom, ndom, nkor, ikvar, nkvar, SUM(sum_charge), SUM(sum_insaldo - sum_money) " +
						  " FROM " + chargeYY + " c INNER JOIN " + prefData + "kvar k ON k.nzp_kvar = c.nzp_kvar " +
												  " INNER JOIN " + prefData + "dom d ON d.nzp_dom = k.nzp_dom " +
												  " INNER JOIN " + prefData + "s_ulica u ON u.nzp_ul = d.nzp_ul " +
						  " WHERE c.dat_charge IS NULL AND tarif > 0 AND (sum_insaldo - sum_money / tarif) > 3 " + whereServices +
						  " GROUP BY 1,2,3,4,5,6,7,8 ";
					ExecSQL(sql);
				}
			}
			reader.Close();

			sql = " SELECT nzp_kvar, TRIM(ulica) AS ulica, nzp_dom, idom, TRIM(ndom) AS ndom, " +
						 " TRIM(nkor) AS nkor, ikvar, TRIM(nkvar) AS nkvar, SUM(sum_charge) AS sum_charge, SUM(sum_pere) AS sum_pere " +
				  " FROM t_report_14_1_4 " +
				  " GROUP BY 1,2,3,4,5,6,7, 8 " +
				  " HAVING SUM(sum_pere) >= " + DolgS + (DolgPo != 0m ? " AND SUM(sum_pere) <= " + DolgPo : string.Empty) +
				  " ORDER BY 2, 4, 6, 7 ";

			DataTable dt = ExecSQLToTable(sql);
			dt.TableName = "Q_master";

			var ds = new DataSet();
			ds.Tables.Add(dt);

			return ds;
		}

		/// <summary>Ограничение по банкам данных</summary>
		private string GetWhereWp() {
			string whereWp = String.Empty;
			whereWp = Banks != null
				? Banks.Aggregate(whereWp, (current, nzpWp) => current + (nzpWp + ","))
				: ReportParams.GetRolesCondition(Constants.role_sql_wp);
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

		/// <summary> Получить список услуг </summary>
		private string GetWhereServ() {
			var result = String.Empty;
			result = Services != null
				? Services.Aggregate(result, (current, nzpServ) => current + (nzpServ + ","))
				: ReportParams.GetRolesCondition(Constants.role_sql_serv);
			result = result.TrimEnd(',');
			result = !String.IsNullOrEmpty(result)
				? " AND nzp_serv in (" + result + ")"
				: String.Empty;
			return result;
		}

		protected override void CreateTempTable() {
			string sql = " CREATE TEMP TABLE t_report_14_1_4 (" +
						 " nzp_kvar INTEGER, " +
						 " ulica CHARACTER(40), " +
						 " nzp_dom INTEGER, " +
						 " idom INTEGER, " +
						 " ndom CHARACTER(10), " +
						 " nkor CHARACTER(3), " +
						 " ikvar INTEGER, " +
						 " nkvar CHARACTER(10), " +
						 " sum_charge " + DBManager.sDecimalType + "(14,2)," +
						 " sum_pere " + DBManager.sDecimalType + "(14,2)) " + DBManager.sUnlogTempTable;
			ExecSQL(sql);
		}

		protected override void DropTempTable() {
			ExecSQL("DROP TABLE t_report_14_1_4");
		}
	}
}
