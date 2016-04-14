using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Bars.KP50.Report.Base;
using Bars.KP50.Utils;
using STCLINE.KP50.DataBase;
using Bars.KP50.Report.Tula.Properties;
using STCLINE.KP50.Global;
using Newtonsoft.Json;

namespace Bars.KP50.Report.Tula.Reports
{
	class Report71011015 : BaseSqlReport
	{
		public override string Name {
			get { return "71.11.15.Реестр оплаченных лицевых счетов"; }
		}

		public override string Description {
			get { return "11.15.Реестр оплаченных лицевых счетов"; }
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
			get { return Resources.Report_71_11_15; }
		}

		public override IList<ReportKind> ReportKinds {
			get { return new List<ReportKind> { ReportKind.Base }; }
		}

		/// <summary>Дата с</summary>
		protected DateTime DatS { get; set; }

		/// <summary>Дата по</summary>
		protected DateTime DatPo { get; set; }

		/// <summary>Поставщики</summary>
		protected List<long> Suppliers { get; set; }

		/// <summary>Заголовок отчета</summary>
		protected string SupplierHeader { get; set; }

		/// <summary>Заголовок территории</summary>
		protected string TerritoryHeader { get; set; }

		/// <summary>Поставщики, Агенты, Принципалы  </summary>
		protected BankSupplierParameterValue BankSupplier { get; set; }


		public override List<UserParam> GetUserParams() {
			var curCalcMonthYear = DBManager.GetCurMonthYear();
			DateTime datS = curCalcMonthYear != null
				? new DateTime(Convert.ToInt32(curCalcMonthYear.Rows[0]["yearr"]),
					Convert.ToInt32(curCalcMonthYear.Rows[0]["month_"]), 1)
				: DateTime.Now;
			DateTime datPo = curCalcMonthYear != null
				? datS.AddMonths(1).AddDays(-1)
				: DateTime.Now;
			return new List<UserParam>
			{
				new PeriodParameter(datS, datPo),
				 new BankSupplierParameter(),
			};
		}

		protected override void PrepareReport(FastReport.Report report) {

			report.SetParameterValue("dat", DateTime.Now.ToLongDateString());
			report.SetParameterValue("time", DateTime.Now.ToLongTimeString());
			report.SetParameterValue("dats", DatS.ToShortDateString());
			report.SetParameterValue("datpo", DatPo.ToShortDateString());

			string headerParam = !string.IsNullOrEmpty(TerritoryHeader) ? "Территория: " + TerritoryHeader + "\n" : string.Empty;
			headerParam += !string.IsNullOrEmpty(SupplierHeader) ? "Поставщики: " + SupplierHeader : string.Empty;
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
			BankSupplier = JsonConvert.DeserializeObject<BankSupplierParameterValue>(UserParamValues["BankSupplier"].Value.ToString());
		}

		public override DataSet GetData() {
			MyDataReader reader;

			string whereSupp = GetWhereSupp("nzp_supp");

			string sql = " select bd_kernel as pref " +
						 " FROM " + ReportParams.Pref + DBManager.sKernelAliasRest + "s_point " +
						 " where nzp_wp>1 " + GetwhereWp();

			ExecRead(out reader, sql);
			while (reader.Read())
			{
				string pref = reader["pref"].ToStr().Trim();
				for (var i = DatS.Year * 12 + DatS.Month;
						 i < DatPo.Year * 12 + DatPo.Month + 1;
						 i++)
				{
					var year = i / 12;
					var month = i % 12;
					if (month == 0)
					{
						year--;
						month = 12;
					}
					sql = " insert into t_svod (num_ls, dat_vvod, kind_opl, sum_prih) " +
						  " select k.num_ls, f.dat_prih as dat_vvod, 'РЦ'," +
						  " sum(f.sum_prih) as sum_prih " +
						  " from " + pref + DBManager.sDataAliasRest + "kvar k, " +
						  pref + "_charge_" + (year - 2000) + DBManager.tableDelimiter + "fn_supplier" +
						  month.ToString("00") + " f " +
						  " where dat_uchet>='" + DatS.ToShortDateString() + "' " +
						  " and dat_uchet<='" + DatPo.ToShortDateString() + "' " +
						  " and f.num_ls = k.num_ls  " + whereSupp +
						  " group by 1,2 ";
					ExecSQL(sql);
				}

				for (var i = DatS.Year; i < DatPo.Year + 1; i++)
				{
					sql = " insert into t_svod (num_ls, dat_vvod, kind_opl, sum_prih) " +
						  " select k.num_ls, dat_prih as dat_vvod, 'П'," +
						  " sum(f.sum_prih) as sum_prih " +
						  " from " + pref + DBManager.sDataAliasRest + "kvar k, " +
						  pref + "_charge_" + (i - 2000) + DBManager.tableDelimiter + "from_supplier f " +
						  " where dat_uchet>='" + DatS.ToShortDateString() + "' " +
						  " and dat_uchet<='" + DatPo.ToShortDateString() + "' " +
						  " and f.num_ls = k.num_ls " + whereSupp +
						  " group by 1,2,3 ";
					ExecSQL(sql);
				}

				for (var i = DatS.Year; i < DatPo.Year + 1; i++)
				{
					sql = " insert into t_svod (num_ls, dat_vvod, kind_opl, sum_prih) " +
						  " select k.num_ls, dat_prih as dat_vvod, 'ПСО'," +
						  " sum(f.sum_prih) as sum_prih " +
						  " from " + pref + DBManager.sDataAliasRest + "kvar k, " +
						  pref + "_charge_" + (i - 2000) + DBManager.tableDelimiter + "del_supplier f " +
						  " where dat_uchet>='" + DatS.ToShortDateString() + "' " +
						  " and dat_uchet<='" + DatPo.ToShortDateString() + "' " +
						  " and f.num_ls = k.num_ls " + whereSupp +
						  " group by 1,2,3 ";
					ExecSQL(sql);
				}

			}
			reader.Close();
			#region Выборка на экран
			sql = " select (case when rajon='-' " +
							" then town else trim(town)||','||trim(rajon) end) as rajon," +
				  " ulica, idom, ndom, " +
				  " case when nkor<>'-' then nkor else '' end as nkor, ikvar, " +
				  " case when (nkvar<>'-') and (nkvar<>'0') then nkvar end as nkvar, " +
				  " case when nkvar_n<>'-' then nkvar_n else '' end as nkvar_n, " +
				  " t.num_ls,  fio, dat_vvod,kind_opl, sum(sum_prih) as sum_prih " +
				  " from t_svod t, " +
					ReportParams.Pref + DBManager.sDataAliasRest + "kvar k, " +
					ReportParams.Pref + DBManager.sDataAliasRest + "dom d, " +
					ReportParams.Pref + DBManager.sDataAliasRest + "s_ulica u, " +
					ReportParams.Pref + DBManager.sDataAliasRest + "s_rajon sr, " +
					ReportParams.Pref + DBManager.sDataAliasRest + "s_town st " +
				  " where t.num_ls = k.num_ls " +
				  "         and k.nzp_dom = d.nzp_dom " +
				  "         and d.nzp_ul = u.nzp_ul " +
				  "         and u.nzp_raj = sr.nzp_raj " +
				  "         and sr.nzp_town = st.nzp_town " +
				  " group by 1,2,3,4,5,6,7,8,9,10,11,12 " +
				  " order by 1,2,3,4,5,6,7,8 ";
			#endregion
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

		/// <summary>
		/// Получить условия органичения по банкам данных
		/// </summary>
		/// <returns></returns>
		private string GetwhereWp() {
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
				TerritoryHeader = String.Empty;
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

		/// <summary>
		/// Получить условия органичения по поставщикам
		/// </summary>
		/// <returns></returns>
		private string GetWhereSupp(string fieldPref) {
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

		protected override void CreateTempTable() {
			const string sql = " create temp table t_svod( " +
							   " num_ls integer, " +
							   " dat_vvod Date, " +
							   " kind_opl char(4), " +
							   " sum_prih " + DBManager.sDecimalType + "(14,2))";
			ExecSQL(sql);
		}

		protected override void DropTempTable() {
			ExecSQL(" drop table t_svod; ");
		}

	}
}
