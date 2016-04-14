using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Bars.KP50.Report.Base;
using Bars.KP50.Report.Base.Parameters;
using Bars.KP50.Report.Main.Properties;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;

namespace Bars.KP50.Report.Main.Reports
{
	class Report611 : BaseSqlReport
	{
		public override string Name {
			get { return "Базовый - 6.11 Список домов с указанием площади и количества жильцов"; }
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

		protected override byte[] Template {
			get { return Resources.Report_base6_11; }
		}

		public override IList<ReportKind> ReportKinds {
			get { return new List<ReportKind> { ReportKind.Base, ReportKind.ListLC }; }
		}

		public override bool IsPreview {
			get { return false; }
		}

		//~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
		/// <summary>Управляющие компании</summary>
		protected List<int> Areas { get; set; }

		/// <summary>Территория</summary>
		protected List<int> Banks { get; set; }

		/// <summary>Территория</summary>
		protected List<int> Geus { get; set; }

		/// <summary>Управляющие компании</summary>
		protected List<int> Raions { get; set; }

		/// <summary>Дата с</summary>
		protected DateTime DatS { get; set; }

		/// <summary>Дата по</summary>
		protected DateTime DatPo { get; set; }

		/// <summary>Управляющие компании</summary>
		protected string AreaHeader { get; set; }

		/// <summary>ЖЭУ</summary>
		protected string GeuHeader { get; set; }

		/// <summary>Список банков в БД</summary>  
		protected string TerritoryHeader { get; set; }

		/// <summary>Статус</summary>
		protected int Status { get; set; }

		/// <summary>Участок</summary>
		protected string Uchastok { get; set; }
		//~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

		public override List<UserParam> GetUserParams() {
			return new List<UserParam>
			{
				new MonthParameter {Value =  DateTime.Today.Month },
				new YearParameter {Value = DateTime.Today.Year },
				new BankParameter(),
				new AreaParameter(),
				new GeuParameter(),
				new UchastokParameter(),
				new RaionsParameter(),
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
			report.SetParameterValue("printDate", DateTime.Now.ToLongDateString());
			report.SetParameterValue("printTime", DateTime.Now.ToLongTimeString());

			Uchastok = Uchastok.Replace("null", "Не установлен");
			string headerParam = !string.IsNullOrEmpty(TerritoryHeader) ? "Территория: " + TerritoryHeader + "\n" : string.Empty;
			headerParam += !string.IsNullOrEmpty(Uchastok) ? "Участок: " + Uchastok + "\n" : string.Empty;
			headerParam += !string.IsNullOrEmpty(AreaHeader) ? "Балансодержатель: " + AreaHeader + "\n" : string.Empty;
			headerParam += !string.IsNullOrEmpty(GeuHeader) ? "ЖЭУ: " + GeuHeader + "\n" : string.Empty;
			headerParam = headerParam.TrimEnd('\n');
			report.SetParameterValue("headerParam", headerParam);
		}

		protected override void PrepareParams() {
			Banks = UserParamValues["Banks"].GetValue<List<int>>();
			Areas = UserParamValues["Areas"].GetValue<List<int>>();
			Geus = UserParamValues["Geu"].GetValue<List<int>>();
			Raions = UserParamValues["Raions"].GetValue<List<int>>();
			Status = UserParamValues["Status"].GetValue<int>();
			DatS = new DateTime(UserParamValues["Year"].GetValue<int>(), UserParamValues["Month"].GetValue<int>(), 1);
			DatPo = DatS.AddMonths(1).AddDays(-1);
			var uch = UserParamValues["Uchastok"].GetValue<List<int>>() ?? new List<int>();
			Uchastok = uch.Any() ? uch.Aggregate(Uchastok, (current, nmbUch) => current + (nmbUch + ", ")).TrimEnd(' ', ',') : string.Empty;
			Uchastok = Uchastok.Replace("-999", "null"); //Участок со значением null
		}

		public override DataSet GetData() {

			string whereUchastok = !string.IsNullOrEmpty(Uchastok)
					? " AND (k.uch IN ( " + Uchastok + " ) " + (Uchastok.Contains("null") ? " OR k.uch IS NULL) " : ") ")
					: string.Empty;

			GetSelectedKvars();

			MyDataReader reader;
			string sql = " SELECT * " +
						 " FROM  " + ReportParams.Pref + DBManager.sKernelAliasRest + "s_point " +
						 " WHERE nzp_wp > 1 " + GetWhereWp(string.Empty);
			ExecRead(out reader, sql);
			while (reader.Read())
			{
				string pref = reader["bd_kernel"].ToString().ToLower().Trim();

				sql = "create temp table sel_kvar (nzp_dom integer, nzp_kvar integer, ikvar char(40))" + DBManager.sUnlogTempTable;
				ExecSQL(sql);

				sql = " insert into sel_kvar(nzp_dom, nzp_kvar, ikvar) " +
					  " select k.nzp_dom, k.nzp_kvar, trim(k.nkvar)||'|'||k.nzp_dom " +
					  " from " + pref + DBManager.sDataAliasRest + "kvar k, selected_kvars kk" +
					  " where k.nzp_kvar = kk.nzp_kvar " + whereUchastok +
							GetWhereArea("k.") + GetWhereGeu("k.");
				if (Status != 3)
				{
					sql += " and 0<(select count(*) from " + pref + DBManager.sDataAliasRest + "prm_3 p " +
						   " where k.nzp_kvar=p.nzp and is_actual =1 and val_prm='" + Status + "'" +
						   " and dat_s<='" + DatPo.ToShortDateString() + "' " +
						   " and dat_po>='" + DatS.ToShortDateString() + "')";
				}
				ExecSQL(sql);

				sql = " insert into t_gil(nzp_dom, kolls, kolkvar) " +
					  " select nzp_dom,  " +
					  " count(sk.nzp_kvar) as kolls, count(distinct ikvar) as kolkvar " +
					  " from sel_kvar sk " +
					  " group by 1 ";
				ExecSQL(sql);


				sql = " insert into t_gil(nzp_dom, pl_dom ) " +
					  " select nzp_dom, sum(val_prm" + DBManager.sConvToNum + ") as pl_dom " +
					  " from sel_kvar sk, " + pref + DBManager.sDataAliasRest + "prm_1 p1  " +
					  " where sk.nzp_kvar = p1.nzp and p1.nzp_prm=4 and p1.is_actual<>100 " +
					  " and p1.dat_s <= " + DBManager.sCurDate +
					  " and p1.dat_po >= " + DBManager.sCurDate + " " +
					  " group by 1 ";
				ExecSQL(sql);



				sql = " insert into t_gil(nzp_dom, kolgil) " +
					  " select sk.nzp_dom, sum(val1 + val3 - val5) as kolgil " +
					  " from sel_kvar sk," + pref + "_charge_" + (DatS.Year - 2000).ToString("00") +
					  DBManager.tableDelimiter + "gil_" + DatS.Month.ToString("00") + " cg  " +
					  " where sk.nzp_kvar = cg.nzp_kvar and stek = 3 " +
					  " group by 1 ";
				ExecSQL(sql);
				ExecSQL("drop table sel_kvar");
			}


			sql = " select town, rajon, trim(" + DBManager.sNvlWord + "(ulicareg,'ул'))||'. '||trim(ulica) as ulica, idom," +
				  " (case when nkor<>'-' then nkor end) as nkor, sum(pl_dom) as pl_dom, " +
				" sum(kolls) as kolls, sum(kolkvar) as kolkvar, sum(kolgil) as kolgil " +
				" from t_gil a, " + ReportParams.Pref + DBManager.sDataAliasRest + "dom b, " +
				ReportParams.Pref + DBManager.sDataAliasRest + "s_ulica s, " +
				ReportParams.Pref + DBManager.sDataAliasRest + "s_rajon sr, " +
				ReportParams.Pref + DBManager.sDataAliasRest + "s_town st " +
				" where a.nzp_dom=b.nzp_dom " +
				" and b.nzp_ul=s.nzp_ul " +
				" and s.nzp_raj=sr.nzp_raj " +
				" and sr.nzp_town=st.nzp_town " +
				" group by 1,2,3,4,5 " +
				" order by 1,2,3,4,5 ";
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
					  " WHERE " + pref + "nzp_area > 0 " + result;
				var area = ExecSQLToTable(sql);
				foreach (DataRow dr in area.Rows)
				{
					AreaHeader += dr["area"].ToString().Trim() + ",";
				}
				AreaHeader = AreaHeader.TrimEnd(',');
			}
			return result;
		}

		/// <summary> Получить условия органичения по ЖЭУ </summary>
		private string GetWhereGeu(string pref) {
			var result = String.Empty;
			result = Geus != null
				? Geus.Aggregate(result, (current, nzpGeu) => current + (nzpGeu + ","))
				: ReportParams.GetRolesCondition(Constants.role_sql_geu);

			result = result.TrimEnd(',');
			if (!String.IsNullOrEmpty(result))
			{
				result = " AND " + pref + "nzp_geu in (" + result + ")";

				GeuHeader = String.Empty;
				var sql = " SELECT geu from " +
					  ReportParams.Pref + DBManager.sDataAliasRest + "s_geu " + pref.TrimEnd('.') +
					  " WHERE " + pref + "nzp_geu > 0 " + result;
				var geu = ExecSQLToTable(sql);
				foreach (DataRow dr in geu.Rows)
				{
					GeuHeader += dr["geu"].ToString().Trim() + ",";
				}
				GeuHeader = GeuHeader.TrimEnd(',');
			}
			return result;
		}

		/// <summary>
		/// Выборка списка квартир в картотеке
		///  </summary>
		/// <returns></returns>
		private void GetSelectedKvars() {
			string whereRajon = String.Empty;
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
			if (ReportParams.CurrentReportKind == ReportKind.ListLC)
			{
				using (IDbConnection connWeb = DBManager.GetConnection(Constants.cons_Webdata))
				{
					if (!DBManager.OpenDb(connWeb, true).result) return;

					string tSpls = DBManager.GetFullBaseName(connWeb) + DBManager.tableDelimiter +
								   "t" + ReportParams.User.nzp_user + "_spls";
					if (TempTableInWebCashe(tSpls))
					{
						string sql = " insert into selected_kvars (nzp_kvar) " +
									 " select nzp_kvar from " + tSpls +
									 " where num_ls > 0 " + whereRajon + GetWhereArea(string.Empty) + GetWhereGeu(string.Empty);
						ExecSQL(sql);
						ExecSQL("create index ix_tmpsk_ls_01 on selected_kvars(nzp_kvar) ");
						ExecSQL(DBManager.sUpdStat + " selected_kvars ");
					}
				}
			}
			else
			{
				string sql = " insert into selected_kvars (nzp_kvar) " +
							 " select nzp_kvar from " + ReportParams.Pref + DBManager.sDataAliasRest + "kvar k" +
							 " where num_ls>0 " + whereRajon;
				ExecSQL(sql);
				ExecSQL("create index ix_tmpsk_ls_01 on selected_kvars(nzp_kvar) ");
				ExecSQL(DBManager.sUpdStat + " selected_kvars ");


			}
		}

		protected override void CreateTempTable() {

			string sql = "create temp table selected_kvars ( nzp_kvar integer) " + DBManager.sUnlogTempTable;
			ExecSQL(sql);

			sql = "create temp table t_gil (" +
				  " nzp_dom integer, " +
				  " pl_dom " + DBManager.sDecimalType + "(14,2), " +
				  " kolls integer, " +
				  " kolkvar integer, " +
				  " kolgil integer) " + DBManager.sUnlogTempTable;
			ExecSQL(sql);
		}

		protected override void DropTempTable() {
			ExecSQL(" drop table selected_kvars; ");
			ExecSQL(" drop table t_gil; ");
		}

	}
}
