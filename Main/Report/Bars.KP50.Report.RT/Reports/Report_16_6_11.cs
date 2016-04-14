using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;
using Bars.KP50.Report.Base;
using STCLINE.KP50.DataBase;
using Bars.KP50.Report.RT.Properties;

namespace Bars.KP50.Report.RT.Reports
{
	class Report16611 : BaseSqlReport
	{
		public override string Name {
			get { return "16.6.11 Список домов с указанием площади и количества жильцов"; }
		}

		public override string Description {
			get { return "6.11 Список домов с указанием площади и количества жильцов"; }
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

		/// <summary>Управляющие компании</summary>
		protected List<int> Areas { get; set; }

		/// <summary>Управляющие компании</summary>
		protected List<int> Raions { get; set; }

		/// <summary>Дата с</summary>
		protected DateTime DatS { get; set; }

		/// <summary>Дата по</summary>
		protected DateTime DatPo { get; set; }

		/// <summary>Управляющие компании</summary>
		protected string AreaHeader { get; set; }

		/// <summary>Статус</summary>
		protected int Status { get; set; }


		public override List<UserParam> GetUserParams() {
			return new List<UserParam>
			{
				new RaionsParameter(),
				new AreaParameter(),
				new MonthParameter { Require = true },
				new YearParameter { Require = true },
				new ComboBoxParameter{
					Code = "Status",
					Name = "Статус",
					Value = "1",
					StoreData = new List<object>
					{
						new { Id = "1", Name = "Только открытые" },
						new { Id = "2", Name = "Только закрытые" },
						new { Id = "3", Name = "Все" }
					}
				}
			};
		}

		protected override void PrepareReport(FastReport.Report report) {
			//report.SetParameterValue("dat_s", DateTime.Today.AddDays(-DateTime.Today.Day + 1));
			//report.SetParameterValue("dat_po", DateTime.Today.AddMonths(1).AddDays(-DateTime.Today.Day));
			report.SetParameterValue("printDate", DateTime.Now.ToLongDateString());
			report.SetParameterValue("printTime", DateTime.Now.ToLongTimeString());
			report.SetParameterValue("area", AreaHeader);
		}

		protected override byte[] Template {
			get { return Resources.Spis_dom_gil; }
		}

		public override IList<ReportKind> ReportKinds {
			get { return new List<ReportKind> { ReportKind.Base }; }
		}

		protected override void PrepareParams() {
			Areas = UserParamValues["Areas"].GetValue<List<int>>();
			Raions = UserParamValues["Raions"].GetValue<List<int>>();
			Status = UserParamValues["Status"].GetValue<int>();
			DatS = new DateTime(UserParamValues["Year"].GetValue<int>(), UserParamValues["Month"].GetValue<int>(), 1);
			DatPo = DatS.AddMonths(1).AddDays(-1);

		}

		public override DataSet GetData() {
			var sql = new StringBuilder();
			MyDataReader reader;

			string whereRajon = "";
			string whereArea = "";


			if (Areas != null)
			{
				whereArea = Areas.Aggregate(whereArea, (current, nzpArea) => current + (nzpArea.ToString(CultureInfo.InvariantCulture) + ","));
			}

			whereArea = whereArea.TrimEnd(',');
			if (!String.IsNullOrEmpty(whereArea))
			{
				whereArea = " AND k.nzp_area in (" + whereArea + ")";

				sql.Remove(0, sql.Length);
				sql.Append(" SELECT area from ");
				sql.Append(ReportParams.Pref + DBManager.sDataAliasRest + "s_area k ");
				sql.Append(" WHERE nzp_area > 0 " + whereArea);
				DataTable area = ExecSQLToTable(sql.ToString());
				foreach (DataRow dr in area.Rows)
				{
					AreaHeader += dr["area"].ToString().Trim() + ",";
				}
				AreaHeader = AreaHeader.TrimEnd(',');
			}

			if (Raions != null)
			{
				whereRajon = Raions.Aggregate(whereRajon, (current, nzpRaj) => current + (nzpRaj + ","));
				whereRajon = whereRajon.TrimEnd(',');
			}

			whereRajon = !String.IsNullOrEmpty(whereRajon) ? " AND nzp_kvar in (SELECT k.nzp_kvar" +
																				" FROM " + ReportParams.Pref + DBManager.sDataAliasRest + "kvar k, " +
																						   ReportParams.Pref + DBManager.sDataAliasRest + "dom d, " +
																						   ReportParams.Pref + DBManager.sDataAliasRest + "s_ulica u " +
																				" WHERE k.nzp_dom=d.nzp_dom AND d.nzp_ul=u.nzp_ul " +
																				" AND u.nzp_raj in (" + whereRajon + ")) " : String.Empty;




			sql.Remove(0, sql.Length);
			if (Status == 1)
			{
				sql.Append(" INSERT INTO sel_kvar (nzp_kvar, ikvar, nzp_dom, idom, nkor, nzp_ul, ulica, nzp_raj, rajon, nzp_town, town, nzp_wp) " +
						   " SELECT " + DBManager.sUniqueWord + " k.nzp_kvar, k.ikvar, d.nzp_dom, d.idom, d.nkor, u.nzp_ul, u.ulica, r.nzp_raj, r.rajon, t.nzp_town, t.town, k.nzp_wp " +
						   " FROM " + ReportParams.Pref + DBManager.sDataAliasRest + "kvar k, " +
							 ReportParams.Pref + DBManager.sDataAliasRest + "dom d, " +
							 ReportParams.Pref + DBManager.sDataAliasRest + "s_ulica u, " +
							 ReportParams.Pref + DBManager.sDataAliasRest + "s_rajon r, " +
							 ReportParams.Pref + DBManager.sDataAliasRest + "s_town t, " +
							 ReportParams.Pref + DBManager.sDataAliasRest + "prm_3 p " +
						   " WHERE k.nzp_kvar=p.nzp AND nzp_prm=51 AND is_actual<>100 " +
						   " AND k.nzp_dom = d.nzp_dom AND d.nzp_ul = u.nzp_ul AND u.nzp_raj = r.nzp_raj AND r.nzp_town = t.nzp_town " +
						   " AND dat_s<='" + DatS.ToShortDateString() + "' AND dat_po>='" + DatPo.ToShortDateString() + "' " +
							 whereArea + whereRajon +
						   " AND val_prm='1' ");
			}
			else if (Status == 2)
			{
				sql.Append(" INSERT INTO sel_kvar (nzp_kvar, ikvar, nzp_dom, idom, nkor, nzp_ul, ulica, nzp_raj, rajon, nzp_town, town, nzp_wp) " +
						   " SELECT " + DBManager.sUniqueWord + " k.nzp_kvar, k.ikvar, d.nzp_dom, d.idom, d.nkor, u.nzp_ul, u.ulica, r.nzp_raj, r.rajon, t.nzp_town, t.town, k.nzp_wp " +
						   " FROM " + ReportParams.Pref + DBManager.sDataAliasRest + "kvar k, " +
							 ReportParams.Pref + DBManager.sDataAliasRest + "dom d, " +
							 ReportParams.Pref + DBManager.sDataAliasRest + "s_ulica u, " +
							 ReportParams.Pref + DBManager.sDataAliasRest + "s_rajon r, " +
							 ReportParams.Pref + DBManager.sDataAliasRest + "s_town t, " +
							 ReportParams.Pref + DBManager.sDataAliasRest + "prm_3 p " +
						   " WHERE k.nzp_kvar=p.nzp AND nzp_prm=51 AND is_actual<>100 " +
						   " AND k.nzp_dom = d.nzp_dom AND d.nzp_ul = u.nzp_ul AND u.nzp_raj = r.nzp_raj AND r.nzp_town = t.nzp_town " +
						   " AND dat_s<='" + DatS.ToShortDateString() + "' AND dat_po>='" + DatPo.ToShortDateString() + "' " +
							 whereArea + whereRajon +
						   " AND val_prm='2' ");
			}
			else if (Status == 3)
			{
				sql.Append(" INSERT INTO sel_kvar (nzp_kvar, ikvar, nzp_dom, idom, nkor, nzp_ul, ulica, nzp_raj, rajon, nzp_town, town, nzp_wp) " +
						   " SELECT " + DBManager.sUniqueWord + " k.nzp_kvar, k.ikvar, d.nzp_dom, d.idom, d.nkor, u.nzp_ul, u.ulica, r.nzp_raj, r.rajon, t.nzp_town, t.town, k.nzp_wp  " +
						   " FROM " + ReportParams.Pref + DBManager.sDataAliasRest + "kvar k, " +
							 ReportParams.Pref + DBManager.sDataAliasRest + "dom d, " +
							 ReportParams.Pref + DBManager.sDataAliasRest + "s_ulica u, " +
							 ReportParams.Pref + DBManager.sDataAliasRest + "s_rajon r, " +
							 ReportParams.Pref + DBManager.sDataAliasRest + "s_town t " +
						   " WHERE k.nzp_dom = d.nzp_dom AND d.nzp_ul = u.nzp_ul AND u.nzp_raj = r.nzp_raj AND r.nzp_town = t.nzp_town " +
							 whereArea + whereRajon);
			}
			ExecSQL(sql.ToString());

			sql.Remove(0, sql.Length);
			sql.Append(" SELECT * ");
			sql.Append(" FROM  " + ReportParams.Pref + DBManager.sKernelAliasRest + "s_point ");
			sql.Append(" WHERE nzp_wp>1 " +
					   " AND nzp_wp IN (SELECT " + DBManager.sUniqueWord + " nzp_wp FROM sel_kvar )");
			ExecRead(out reader, sql.ToString());
			while (reader.Read())
			{
				string pref = reader["bd_kernel"].ToString().ToLower().Trim();
				string calcGkuTable = pref + "_charge_" + (DatS.Year - 2000).ToString("00") + DBManager.tableDelimiter +
								 "calc_gku_" + DatS.Month.ToString("00");

				sql.Remove(0, sql.Length);
				sql.Append(" UPDATE sel_kvar SET pl_dom = (" +
						   " SELECT MAX(REPLACE(val_prm,',','.')" + DBManager.sConvToNum + ")" +
						   " FROM " + pref + DBManager.sDataAliasRest + "prm_1 p1 " +
						   " WHERE sel_kvar.nzp_kvar = p1.nzp " +
							 " AND p1.nzp_prm = 4 " +
							 " AND p1.is_actual <> 100 " +
							 " AND p1.dat_s <= " + DBManager.sCurDate +
							 " AND p1.dat_po >= " + DBManager.sCurDate + ") ");
				ExecSQL(sql.ToString());

				sql.Remove(0, sql.Length);
				sql.Append(" UPDATE sel_kvar SET kolgil = (" +
						   " SELECT MAX(ROUND(gil))" +
						   " FROM " + calcGkuTable + " cg " +
						   " WHERE sel_kvar.nzp_kvar = cg.nzp_kvar) ");
				ExecSQL(sql.ToString());
			}

			sql.Remove(0, sql.Length);
			sql.Append(" INSERT INTO t_gil(town, rajon, ulica, idom, nkor, pl_dom, kolls, kolkvar, kolgil) " +
				" SELECT town, rajon, ulica, idom, nkor, SUM(pl_dom) AS pl_dom, " +
					   " COUNT(sk.nzp_kvar) AS kolls, COUNT(DISTINCT ikvar) AS kolkvar, SUM(kolgil) AS kolgil " +
				" FROM sel_kvar sk " +
				" GROUP BY 1,2,3,4,5 ");
			ExecSQL(sql.ToString());

			sql.Remove(0, sql.Length);
			sql.Append(" select town, rajon, ulica, idom, (case when nkor<>'-' then nkor end) as nkor, sum(pl_dom) as pl_dom, " +
				" sum(kolls) as kolls, sum(kolkvar) as kolkvar, sum(kolgil) as kolgil " +
				" from t_gil " +
				" group by 1,2,3,4,5 " +
				" order by 1,2,3,4,5 ");
			DataTable dt = ExecSQLToTable(sql.ToString());
			dt.TableName = "Q_master";
			var ds = new DataSet();
			ds.Tables.Add(dt);

			return ds;
		}

		protected override void CreateTempTable() {
			var sql = new StringBuilder();
			sql.Append("create temp table sel_kvar (" +
				" nzp_kvar integer, " +
				" ikvar integer, " +
				" nzp_dom integer, " +
				" idom integer, " +
				" nkor char(3), " +
				" nzp_ul integer, " +
				" ulica char(40), " +
				" nzp_raj integer, " +
				" rajon char(30), " +
				" nzp_town integer, " +
				" town char(30)," +
				" pl_dom " + DBManager.sDecimalType + "(14,2)," +
				" kolgil INTEGER," +
				" nzp_wp INTEGER) "
				);
			ExecSQL(sql.ToString());
			sql.Remove(0, sql.Length);
			sql.Append("create temp table t_gil (" +
				 " town char(30), " +
				 " rajon char(30), " +
				 " ulica char(40), " +
				 " idom integer, " +
				 " nkor char(3), " +
				 " pl_dom " + DBManager.sDecimalType + "(14,2), " +
				 " kolls integer, " +
				 " kolkvar integer, " +
				 " kolgil integer) "
				 );
			ExecSQL(sql.ToString());
		}

		protected override void DropTempTable() {
			ExecSQL(" drop table sel_kvar; drop table t_gil; ");
		}

	}
}
