using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Bars.KP50.Report.Base;
using Bars.KP50.Report.Base.Parameters;
using Bars.KP50.Report.Tula.Properties;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;

namespace Bars.KP50.Report.Tula.Reports
{
	class Report7136 : BaseSqlReport
	{
		public override string Name {
			get { return "71.3.6 Сводный отчет о распределении поступившей оплаты"; }
		}

		public override string Description {
			get { return "71.3.6 Сводный отчет о распределении поступившей оплаты"; }
		}

		public override IList<ReportGroup> ReportGroups {
			get {
				var result = new List<ReportGroup> { ReportGroup.Finans };
				return result;
			}
		}

		public override bool IsPreview {
			get { return false; }
		}

		protected override byte[] Template {
			get { return Resources.Report_71_3_6; }
		}

		public override IList<ReportKind> ReportKinds {
			get { return new List<ReportKind> { ReportKind.Base }; }
		}

		//~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
		/// <summary> Период: с </summary>
		private DateTime DatS { get; set; }

		/// <summary> Период: по </summary>
		private DateTime DatPo { get; set; }

		/// <summary>Территория</summary>
		protected List<int> Banks { get; set; }
		//~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

		public override List<UserParam> GetUserParams() {
			return new List<UserParam>
			{
				new PeriodParameter(DateTime.Now, DateTime.Now),
				new BankParameter()
			};
		}

		protected override void PrepareParams() {
			DateTime begin;
			DateTime end;
			var period = UserParamValues["Period"].GetValue<string>();
			PeriodParameter.GetValues(period, out begin, out end);
			DatS = begin;
			DatPo = end;
			Banks = UserParamValues["Banks"].GetValue<List<int>>();
		}

		protected override void PrepareReport(FastReport.Report report) {
		}

		public override DataSet GetData() {

			MyDataReader reader;

			string prefData = ReportParams.Pref + DBManager.sDataAliasRest,
					prefKernel = ReportParams.Pref + DBManager.sKernelAliasRest;

			string sql = " SELECT bd_kernel AS pref, point " +
			 " FROM  " + ReportParams.Pref + DBManager.sKernelAliasRest + "s_point " +
			 " WHERE nzp_wp > 1 " + GetWhereWp();
			ExecRead(out reader, sql);

			while (reader.Read())
			{
				string pref = reader["pref"].ToString().Trim();

				for (int i = DatS.Year * 12 + DatS.Month; i < DatPo.Year * 12 + DatPo.Month + 1; i++)
				{
					var year = i / 12;
					var month = i % 12;
					if (month == 0)
					{
						year--;
						month = 12;
					}

					string fnSupplierYY = pref + "_charge_" + (year - 2000).ToString("00") + DBManager.tableDelimiter + "fn_supplier" + month.ToString("00"),
							prefFinYY = ReportParams.Pref + "_fin_" + (year - 2000).ToString("00") + DBManager.tableDelimiter;

					if (TempTableInWebCashe(fnSupplierYY))
					{
						sql = " INSERT INTO t_report_71_3_6(dat_s, dat_po, area, nzp_area, geu, nzp_geu, supplier, nzp_supp, service, nzp_serv, sum_prih, nzp_bank) " +
							  " SELECT DATE('" + DatS.ToShortDateString() + "') AS dat_s, " +
									 " DATE('" + DatPo.ToShortDateString() + "') AS dat_po, " +
									 " area, " +
									 " k.nzp_area, " +
									 " geu, " +
									 " k.nzp_geu, " +
									 " name_supp AS supplier, " +
									 " fs.nzp_supp, " +
									 " service, " +
									 " fs.nzp_serv, " +
									 " sum(sum_prih) AS sum_prih, " +
									 " nzp_bank " +
							  " FROM " + fnSupplierYY + " fs INNER JOIN " + prefKernel + "services se ON se.nzp_serv = fs.nzp_serv " +
														   " INNER JOIN " + prefKernel + "supplier su ON su.nzp_supp = fs.nzp_supp " +
														   " INNER JOIN " + prefFinYY + "pack_ls pl ON pl.nzp_pack_ls = fs.nzp_pack_ls " +
														   " INNER JOIN " + prefFinYY + "pack p ON p.nzp_pack = pl.nzp_pack " +
														   " INNER JOIN " + prefData + "kvar k ON k.num_ls = pl.num_ls " +
														   " INNER JOIN " + prefData + "s_area sa ON sa.nzp_area = k.nzp_area " +
														   " INNER JOIN " + prefData + "s_geu sg ON sg.nzp_geu = k.nzp_geu " +
							  " WHERE fs.dat_uchet <= '" + DatPo.ToShortDateString() + "' " +
								" AND fs.dat_uchet >= '" + DatS.ToShortDateString() + "' " +
							  " GROUP BY 1,2,3,4,5,6,7,8,9,10,12 ";
						ExecSQL(sql);

						sql = " INSERT INTO t_all_71_3_6 " +
							  " SELECT * " +
							  " FROM t_report_71_3_6  ";
						ExecSQL(sql);
					}

				}
			}

			reader.Close();

			sql = " SELECT dat_s, " +
						 " dat_po, " +
						 " TRIM(area) AS area, " +
						 " nzp_area, " +
						 " TRIM(geu) AS geu, " +
						 " nzp_geu, " +
						 " TRIM(supplier) AS supplier, " +
						 " nzp_supp, " +
						 " TRIM(service) AS service, " +
						 " nzp_serv, " +
						 " SUM(sum_prih) AS sum_prih, " +
						 " nzp_bank " +
				  " FROM t_all_71_3_6 " +
				  " GROUP BY 1,2,3,4,5,6,7,8,9,10,12" +
				  " ORDER BY geu, area, supplier, service ";
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
			return whereWp;
		}

		protected override void CreateTempTable() {
			string sql = " CREATE TEMP TABLE t_report_71_3_6( " +
							" dat_s DATE, " +
							" dat_po DATE, " +
							" area CHARACTER(40), " +
							" nzp_area INTEGER, " +
							" geu CHARACTER(60), " +
							" nzp_geu INTEGER, " +
							" supplier CHARACTER(100), " +
							" nzp_supp INTEGER, " +
							" service CHARACTER(100), " +
							" nzp_serv INTEGER, " +
							" sum_prih NUMERIC(14,2), " +
							" nzp_bank INTEGER)" + DBManager.sUnlogTempTable;
			ExecSQL(sql);

			sql = " CREATE TEMP TABLE t_all_71_3_6( " +
							" dat_s DATE, " +
							" dat_po DATE, " +
							" area CHARACTER(40), " +
							" nzp_area INTEGER, " +
							" geu CHARACTER(60), " +
							" nzp_geu INTEGER, " +
							" supplier CHARACTER(100), " +
							" nzp_supp INTEGER, " +
							" service CHARACTER(100), " +
							" nzp_serv INTEGER, " +
							" sum_prih NUMERIC(14,2), " +
							" nzp_bank INTEGER)" + DBManager.sUnlogTempTable;
			ExecSQL(sql);
		}

		protected override void DropTempTable() {
			ExecSQL(" DROP TABLE t_report_71_3_6 ");
			ExecSQL(" DROP TABLE t_all_71_3_6 ");
		}
	}
}
