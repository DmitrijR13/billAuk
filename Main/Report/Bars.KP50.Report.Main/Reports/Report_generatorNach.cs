using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;
using Bars.KP50.Report.Base;
using Bars.KP50.Report.Main.Properties;
using Bars.KP50.Utils;
using Castle.Core.Internal;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;
using Constants = STCLINE.KP50.Global.Constants;

namespace Bars.KP50.Report.Main.Reports
{
	class GenNach : BaseSqlReport
	{
		public override string Name {
			get { return "Генератор по начислениям"; }
		}

		public override string Description {
			get { return "Генератор по начислениям"; }
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
			get { return Resources.Gen_nach; }
		}

		public override IList<ReportKind> ReportKinds {
			get { return new List<ReportKind> { ReportKind.Base }; }
		}

		//~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

		/// <summary>Месяц</summary>
		private int Month { get; set; }

		/// <summary>Год</summary>
		private int Year { get; set; }

		/// <summary>УК</summary>
		private List<long> Areas { get; set; }

		/// <summary>Поставщики</summary>
		private List<long> Suppliers { get; set; }

		/// <summary>Услуги</summary>
		private List<long> Services { get; set; }

		/// <summary>Заголовок территории</summary>
		protected string TerritoryHeader { get; set; }

		/// <summary>УК</summary>
		private string AreasHeader { get; set; }

		/// <summary>Поставщики</summary>
		private string SuppliersHeader { get; set; }

		/// <summary>Услуги</summary>
		private string ServicesHeader { get; set; }

		/// <summary>Параметры</summary>
		private List<long> Params { get; set; }

		/// <summary>Банки данных</summary>
		private List<int> Banks { get; set; }

		private bool RowCount { get; set; }

		/// <summary>Адрес</summary>
		protected AddressParameterValue Address { get; set; }

		/// <summary>Улица</summary>
		private string Raions { get; set; }

		/// <summary>Улица</summary>
		private string Streets { get; set; }

		/// <summary>Дом</summary>
		private string Houses { get; set; }

		/// <summary>Участок</summary>
		protected string Uchastok { get; set; }

		/// <summary>Территория</summary>
		protected List<int> Geus { get; set; }

		/// <summary>ЖЭУ</summary>
		protected string GeuHeader { get; set; }

		//~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

		public override List<UserParam> GetUserParams() {
			var curCalcMonthYear = DBManager.GetCurMonthYear();
			return new List<UserParam>
			{
				new MonthParameter {Value = curCalcMonthYear != null ? curCalcMonthYear.Rows[0]["month_"] : DateTime.Today.Month },
				new YearParameter {Value = curCalcMonthYear != null ? curCalcMonthYear.Rows[0]["yearr"] : DateTime.Today.Year },
				new SupplierAndBankParameter(),
				new AddressParameter(),
				new AreaParameter(),
				new GeuParameter(),
				new UchastokParameter(),
				new ServiceParameter(),
				new ComboBoxParameter(true) {
					Name = "Параметры отчета", 
					Code = "Params",
					Value = 1,
					Require = true,
					StoreData = new List<object> {
						new { Id = 1, Name = "Территория (УК)"},
						new { Id = 2, Name = "ЖЭУ"},
						new { Id = 3, Name = "Участок"},
						new { Id = 4, Name = "Улица"},
						new { Id = 5, Name = "Дом"},
						new { Id = 6, Name = "Квартира"},
						new { Id = 7, Name = "Лицевой счет"},
						new { Id = 8, Name = "Платежный код"},
						new { Id = 9, Name = "Поставщик"},
						new { Id = 10, Name = "Услуга"},
						new { Id = 11, Name = "Входящее сальдо"},
						new { Id = 12, Name = "Тариф"},
						new { Id = 13, Name = "Начислено"},
						new { Id = 14, Name = "Недопоставка"},
						new { Id = 15, Name = "Начислено с учетом недопоставки"},
						new { Id = 16, Name = "Перерасчет"},
						new { Id = 17, Name = "Перекидка"},
						new { Id = 18, Name = "К оплате"},
						new { Id = 19, Name = "Оплачено"},
						new { Id = 20, Name = "Исходящее сальдо"}
					}
				},
			};
		}

		protected override void PrepareReport(FastReport.Report report) {
			var months = new[] {"","Январь","Февраль",
				 "Март","Апрель","Май","Июнь","Июль","Август","Сентябрь",
				 "Октябрь","Ноябрь","Декабрь"};
			report.SetParameterValue("dat", DateTime.Now.ToLongDateString());
			report.SetParameterValue("time", DateTime.Now.ToLongTimeString());
			report.SetParameterValue("month", months[Month]);
			report.SetParameterValue("year", Year);

			Uchastok = Uchastok.Replace("null", "Не установлен");
			string headerParam = !string.IsNullOrEmpty(TerritoryHeader) ? "Территория: " + TerritoryHeader + "\n" : string.Empty;
			headerParam += !string.IsNullOrEmpty(Uchastok) ? "Участок: " + Uchastok + "\n" : string.Empty;
			headerParam += !string.IsNullOrEmpty(AreasHeader) ? "Балансодержатель: " + AreasHeader + "\n" : string.Empty;
			headerParam += !string.IsNullOrEmpty(GeuHeader) ? "ЖЭУ: " + GeuHeader + "\n" : string.Empty;
			headerParam += !string.IsNullOrEmpty(SuppliersHeader) ? "Поставщики: " + SuppliersHeader + "\n" : string.Empty;
			headerParam += !string.IsNullOrEmpty(ServicesHeader) ? "Услуги: " + ServicesHeader : string.Empty;
			headerParam = headerParam.TrimEnd('\n');
			report.SetParameterValue("headerParam", headerParam);

			string limit = ReportParams.ExportFormat == ExportFormat.Excel2007 ? "40000" : "100000";

			report.SetParameterValue("excel",
				RowCount
					? "Выборка записей ограничена первыми " + limit + " строками. Выберите другой формат экспортируемого файла, либо поставьте другие ограничения для отчета"
					: "");
		}

		protected override void PrepareParams() {
			Month = UserParamValues["Month"].GetValue<int>();
			Year = UserParamValues["Year"].GetValue<int>();
			Areas = UserParamValues["Areas"].GetValue<List<long>>();
			Services = UserParamValues["Services"].GetValue<List<long>>();
			Params = UserParamValues["Params"].GetValue<List<long>>();
			if (Params.Contains(6))
			{
				if (!Params.Contains(5)) { Params.Add(5); }
				if (!Params.Contains(4)) { Params.Add(4); }
			}
			if (Params.Contains(5))
			{
				if (!Params.Contains(4)) { Params.Add(4); }
			}

			var values = JsonConvert.DeserializeObject<Dictionary<string, object>>(UserParamValues["SupplierAndBank"].GetValue<string>());
			Suppliers = values["Streets"] != null
				? values["Streets"].To<JArray>().Select(x => x.Value<long>()).ToList()
				: null;
			Banks = values["Raions"] != null
				? values["Raions"].To<JArray>().Select(x => x.Value<int>()).ToList()
				: null;

			Address = UserParamValues["Address"].GetValue<AddressParameterValue>();
			if (Address.Raions != null)
			{
				Raions = String.Join(",", Address.Raions.Select(x => x.ToString(CultureInfo.InvariantCulture)).ToArray());
				Raions = "and u.nzp_raj in (" + Raions + ") ";
			}
			if (Address.Streets != null)
			{
				Streets = String.Join(",", Address.Streets.Select(x => x.ToString(CultureInfo.InvariantCulture)).ToArray());
				Streets = "and u.nzp_ul in (" + Streets + ") ";
			}
			if (Address.Houses != null)
			{
				List<string> goodHouses = Address.Houses.FindAll(x => x.Trim() != "" && x.Contains("'") == false);
				if (!goodHouses.IsNullOrEmpty())
					Houses = "and d.nzp_dom in (" + String.Join(",", goodHouses.ToArray()) + ") ";
			}

			var uch = UserParamValues["Uchastok"].GetValue<List<int>>() ?? new List<int>();
			Uchastok = uch.Any() ? uch.Aggregate(Uchastok, (current, nmbUch) => current + (nmbUch + ", ")).TrimEnd(' ', ',') : string.Empty;
			Uchastok = Uchastok.Replace("-999", "null"); //Участок со значением null
			Geus = UserParamValues["Geu"].GetValue<List<int>>();
		}

		public override DataSet GetData() {
			var sql = new StringBuilder();
			MyDataReader reader;

			string whereSupp = GetWhereSupp();
			string whereArea = GetWhereArea();
			string whereServ = GetWhereServ();
			string whereGeu = GetWhereGeu("k.");
			string whereUchastok = !string.IsNullOrEmpty(Uchastok)
				? " AND (k.uch IN ( " + Uchastok + " ) " + (Uchastok.Contains("null") ? " OR k.uch IS NULL) " : ") ")
					: string.Empty;


			CreateTSvod();

			sql.Remove(0, sql.Length);
			sql.Append(" select bd_kernel as pref " +
				  " FROM " + ReportParams.Pref + DBManager.sKernelAliasRest + "s_point " +
				  " where nzp_wp>1 " + GetwhereWp());
			ExecRead(out reader, sql.ToString());
			while (reader.Read())
			{
				string pref = reader["pref"].ToStr().Trim();
				sql.Remove(0, sql.Length);
				sql.Append(" insert into t_svod( ");
				if (Params.Contains(1)) { sql.Append(" nzp_area, "); }
				if (Params.Contains(2)) { sql.Append(" nzp_geu, "); }
				if (Params.Contains(3)) { sql.Append(" uch, "); }
				if (Params.Contains(4)) { sql.Append(" nzp_raj, nzp_ul, "); }
				if (Params.Contains(5)) { sql.Append(" nzp_dom, "); }
				if (Params.Contains(6)) { sql.Append(" nzp_kvar, "); }
				if (Params.Contains(7)) { sql.Append(" num_ls, "); }
				if (Params.Contains(8)) { sql.Append(" pkod, "); }
				if (Params.Contains(9)) { sql.Append(" nzp_supp, "); }
				if (Params.Contains(10)) { sql.Append(" nzp_serv, "); }
				if (Params.Contains(11)) { sql.Append(" sum_insaldo, "); }
				if (Params.Contains(12)) { sql.Append(" tarif, "); }
				if (Params.Contains(13)) { sql.Append(" rsum_tarif, "); }
				if (Params.Contains(14)) { sql.Append(" sum_nedop, "); }
				if (Params.Contains(15)) { sql.Append(" sum_tarif, "); }
				if (Params.Contains(16)) { sql.Append(" reval, "); }
				if (Params.Contains(17)) { sql.Append(" real_charge, "); }
				if (Params.Contains(18)) { sql.Append(" sum_charge, "); }
				if (Params.Contains(19)) { sql.Append(" sum_money, "); }
				if (Params.Contains(20)) { sql.Append(" sum_outsaldo, "); }
				sql.Remove(sql.Length - 2, 2);
				sql.Append(") ");
				sql.Append(" select ");
				if (Params.Contains(1)) { sql.Append(" k.nzp_area, "); }
				if (Params.Contains(2)) { sql.Append(" k.nzp_geu, "); }
				if (Params.Contains(3)) { sql.Append(" uch, "); }
				if (Params.Contains(4)) { sql.Append(" u.nzp_raj, u.nzp_ul, "); }
				if (Params.Contains(5)) { sql.Append(" d.nzp_dom, "); }
				if (Params.Contains(6)) { sql.Append(" k.nzp_kvar, "); }
				if (Params.Contains(7)) { sql.Append(" k.num_ls, "); }
				if (Params.Contains(8)) { sql.Append(" pkod, "); }
				if (Params.Contains(9)) { sql.Append(" nzp_supp, "); }
				if (Params.Contains(10)) { sql.Append(" nzp_serv, "); }
				if (Params.Contains(11)) { sql.Append(" sum_insaldo, "); }
				if (Params.Contains(12)) { sql.Append(" tarif, "); }
				if (Params.Contains(13)) { sql.Append(" rsum_tarif, "); }
				if (Params.Contains(14)) { sql.Append(" sum_nedop, "); }
				if (Params.Contains(15)) { sql.Append(" sum_tarif, "); }
				if (Params.Contains(16)) { sql.Append(" reval, "); }
				if (Params.Contains(17)) { sql.Append(" real_charge, "); }
				if (Params.Contains(18)) { sql.Append(" sum_charge, "); }
				if (Params.Contains(19)) { sql.Append(" sum_money, "); }
				if (Params.Contains(20)) { sql.Append(" sum_outsaldo, "); }
				sql.Remove(sql.Length - 2, 2);
				sql.Append(" from " + pref + DBManager.sDataAliasRest + "kvar k, ");
				sql.Append(pref + "_charge_" + (Year - 2000).ToString("00") + DBManager.tableDelimiter + "charge_" + Month.ToString("00") + " c, ");
				if (Params.Contains(4)) { sql.Append(pref + DBManager.sDataAliasRest + "s_ulica u, " + pref + DBManager.sDataAliasRest + "dom d, "); }
				else if (Params.Contains(5)) { sql.Append(pref + DBManager.sDataAliasRest + "dom d, "); }
				sql.Remove(sql.Length - 2, 2);
				sql.Append(" where k.nzp_kvar = c.nzp_kvar and  c.dat_charge is null ");
				sql.Append(" and c.nzp_serv > 1 ");
				sql.Append(whereArea + whereSupp + whereServ + whereGeu + whereUchastok);
				if (Params.Contains(4)) { sql.Append(" and k.nzp_dom = d.nzp_dom and d.nzp_ul = u.nzp_ul " + Raions + Streets + Houses); }
				else if (Params.Contains(5)) { sql.Append(" and k.nzp_dom = d.nzp_dom " + Houses); }

				ExecSQL(sql.ToString());
			}
			reader.Close();

			string order = FillSvodTable();


			DataTable dt;
			try
			{
                dt = ExecSQLToTable(
                        " SELECT area, geu, uch, rajon, ulica, ndom, idom, nkor, nkvar, ikvar, num_ls, pkod, name_supp, service, " +
                        " SUM(sum_insaldo) as sum_insaldo, tarif, SUM(sum_nedop) as sum_nedop, SUM(sum_tarif) as sum_tarif, SUM(sum_charge) as sum_charge, " +
                        " SUM(sum_money) as sum_money, SUM(real_charge) as real_charge, SUM(rsum_tarif) as rsum_tarif, SUM(reval) as reval, SUM(sum_outsaldo) as sum_outsaldo " +
                        " FROM t_svod_all " +
                        " GROUP BY area, geu, uch, rajon, ulica, ndom,idom, nkor, nkvar, ikvar, num_ls, pkod, name_supp, service, tarif ", 1000);
                var dv = new DataView(dt) { Sort = order };
                dt = dv.ToTable();
                RowCount = false;
			}
			catch (Exception)
			{
                dt = ExecSQLToTable(DBManager.SetLimitOffset(
                            " SELECT area, geu, uch, rajon, ulica, ndom, idom, nkor, nkvar, ikvar, num_ls, pkod, name_supp, service, " +
                            " SUM(sum_insaldo) as sum_insaldo, tarif, SUM(sum_nedop) as sum_nedop, SUM(sum_tarif) as sum_tarif, SUM(sum_charge) as sum_charge, " +
                            " SUM(sum_money) as sum_money, SUM(real_charge) as real_charge, SUM(rsum_tarif) as rsum_tarif, SUM(reval) as reval, SUM(sum_outsaldo) as sum_outsaldo " +
                            " FROM t_svod_all " +
                            " GROUP BY area, geu, uch, rajon, ulica, ndom,idom, nkor, nkvar, ikvar, num_ls, pkod, name_supp, service, tarif ", 100000, 0), 1000);
                var dv = new DataView(dt) { Sort = order };
                dt = dv.ToTable();
                RowCount = true;
			}
			dt.TableName = "Q_master";

			if (dt.Rows.Count >= 100000)
			{
				if (ReportParams.ExportFormat == ExportFormat.Excel2007)
				{
					var dtr = dt.Rows.Cast<DataRow>().Skip(40000).ToArray();
					EnumerableExtension.ForEach(dtr, dt.Rows.Remove);
				}
				else
				{
					var dtr = dt.Rows.Cast<DataRow>().Skip(100000).ToArray();
					EnumerableExtension.ForEach(dtr, dt.Rows.Remove);
				}
				RowCount = true;
			}
			else
			{
				RowCount = false;
			}

			sql.Remove(0, sql.Length);
			sql.Append(" insert into t_title values( ");
			sql.Append(Params.Contains(1) ? " 'УК' , " : " '' , ");
			sql.Append(Params.Contains(2) ? " 'ЖЭУ' , " : " '' , ");
			sql.Append(Params.Contains(3) ? " 'Участок' , " : " '' , ");
			sql.Append(Params.Contains(4) ? " 'Улица' , " : " '' , ");
			sql.Append(Params.Contains(5) ? " 'Дом' , " : " '' , ");
			sql.Append(Params.Contains(6) ? " 'Квартира' , " : " '' , ");
			sql.Append(Params.Contains(7) ? " 'ЛС' , " : " '' , ");
			sql.Append(Params.Contains(8) ? " 'Пл. код' , " : " '' , ");
			sql.Append(Params.Contains(9) ? " 'Поставщик' , " : " '' , ");
			sql.Append(Params.Contains(10) ? " 'Услуга' , " : " '' , ");
			sql.Append(Params.Contains(11) ? " 'Вх. сальдо' , " : " '' , ");
			sql.Append(Params.Contains(12) ? " 'Тариф' , " : " '' , ");
			sql.Append(Params.Contains(13) ? " 'Начислено' , " : " '' , ");
			sql.Append(Params.Contains(14) ? " 'Недопоставка' , " : " '' , ");
			sql.Append(Params.Contains(15) ? " 'Начислено с учетом недоп-ки' , " : " '' , ");
			sql.Append(Params.Contains(16) ? " 'Перерасчет' , " : " '' , ");
			sql.Append(Params.Contains(17) ? " 'Перекидка' , " : " '' , ");
			sql.Append(Params.Contains(18) ? " 'К оплате' , " : " '' , ");
			sql.Append(Params.Contains(19) ? " 'Оплачено' , " : " '' , ");
			sql.Append(Params.Contains(20) ? " 'Исх. сальдо' , " : " '' , ");
			sql.Remove(sql.Length - 2, 2);
			sql.Append(") ");
			ExecSQL(sql.ToString());
			DataTable dt1 = ExecSQLToTable(" select * from t_title ");
			dt1.TableName = "Q_master1";


			var ds = new DataSet();
			ds.Tables.Add(dt);
			ds.Tables.Add(dt1);
			return ds;

		}

		private string FillSvodTable() {
			var sql = new StringBuilder();
			var grouper = new StringBuilder();
			var order = new StringBuilder();

			sql.Append(" insert into t_svod_all( ");
			if (Params.Contains(1)) { sql.Append(" nzp_area, "); }
			if (Params.Contains(2)) { sql.Append(" nzp_geu, "); }
			if (Params.Contains(3)) { sql.Append(" uch, "); }
			if (Params.Contains(4)) { sql.Append(" nzp_raj, nzp_ul, "); }
			if (Params.Contains(5)) { sql.Append(" nzp_dom, "); }
			if (Params.Contains(6)) { sql.Append(" nzp_kvar, "); }
			if (Params.Contains(7)) { sql.Append(" num_ls, "); }
			if (Params.Contains(8)) { sql.Append(" pkod, "); }
			if (Params.Contains(9)) { sql.Append(" nzp_supp, "); }
			if (Params.Contains(10)) { sql.Append(" nzp_serv, "); }
			if (Params.Contains(11)) { sql.Append(" sum_insaldo, "); }
			if (Params.Contains(12)) { sql.Append(" tarif, "); }
			if (Params.Contains(13)) { sql.Append(" rsum_tarif, "); }
			if (Params.Contains(14)) { sql.Append(" sum_nedop, "); }
			if (Params.Contains(15)) { sql.Append(" sum_tarif, "); }
			if (Params.Contains(16)) { sql.Append(" reval, "); }
			if (Params.Contains(17)) { sql.Append(" real_charge, "); }
			if (Params.Contains(18)) { sql.Append(" sum_charge, "); }
			if (Params.Contains(19)) { sql.Append(" sum_money, "); }
			if (Params.Contains(20)) { sql.Append(" sum_outsaldo, "); }
			sql.Remove(sql.Length - 2, 2);
			sql.Append(") ");
			sql.Append(" select ");
			if (Params.Contains(1)) { sql.Append(" nzp_area, "); grouper.Append(" nzp_area, "); order.Append(" area, "); }
			if (Params.Contains(2)) { sql.Append(" nzp_geu, "); grouper.Append(" nzp_geu, "); order.Append(" geu, "); }
			if (Params.Contains(3)) { sql.Append(" uch, "); grouper.Append(" uch, "); order.Append(" uch, "); }
			if (Params.Contains(4)) { sql.Append(" nzp_raj, nzp_ul, "); grouper.Append(" nzp_raj, nzp_ul, "); order.Append(" rajon, ulica, "); }
			if (Params.Contains(5)) { sql.Append(" nzp_dom, "); grouper.Append(" nzp_dom, "); order.Append(" idom,ndom, nkor, "); }
			if (Params.Contains(6)) { sql.Append(" nzp_kvar, "); grouper.Append(" nzp_kvar, "); order.Append(" ikvar, "); }
			if (Params.Contains(7)) { sql.Append(" num_ls, "); grouper.Append(" num_ls, "); order.Append(" num_ls, "); }
			if (Params.Contains(8)) { sql.Append(" pkod, "); grouper.Append(" pkod, "); order.Append(" pkod, "); }
			if (Params.Contains(9)) { sql.Append(" nzp_supp, "); grouper.Append(" nzp_supp, "); order.Append(" name_supp, "); }
			if (Params.Contains(10)) { sql.Append(" nzp_serv, "); grouper.Append(" nzp_serv, "); order.Append(" service, "); }
			if (Params.Contains(11)) { sql.Append(" sum(sum_insaldo) as sum_insaldo, "); }
			if (Params.Contains(12)) { sql.Append(" sum(tarif) as tarif, "); }
			if (Params.Contains(13)) { sql.Append(" sum(rsum_tarif) as rsum_tarif, "); }
			if (Params.Contains(14)) { sql.Append(" sum(sum_nedop) as sum_nedop, "); }
			if (Params.Contains(15)) { sql.Append(" sum(sum_tarif) as sum_tarif, "); }
			if (Params.Contains(16)) { sql.Append(" sum(reval) as reval, "); }
			if (Params.Contains(17)) { sql.Append(" sum(real_charge) as real_charge, "); }
			if (Params.Contains(18)) { sql.Append(" sum(sum_charge) as sum_charge, "); }
			if (Params.Contains(19)) { sql.Append(" sum(sum_money) as sum_money, "); }
			if (Params.Contains(20)) { sql.Append(" sum(sum_outsaldo) as sum_outsaldo, "); }
			sql.Remove(sql.Length - 2, 2);
			sql.Append(" from t_svod ");
			grouper.Remove(grouper.Length - 2, 2);
			order.Remove(order.Length - 2, 2);
			sql.Append(" group by " + grouper);

			ExecSQL(" create index svod_index on t_svod(" + grouper + ") ");
			ExecSQL(DBManager.sUpdStat + " t_svod ");
			ExecSQL(sql.ToString());

			if (Params.Contains(1))
			{
				sql.Remove(0, sql.Length);
				sql.Append(" update t_svod_all set area = (" +
						   " select area from " + ReportParams.Pref + DBManager.sDataAliasRest + "s_area a " +
						   " where a.nzp_area = t_svod_all.nzp_area) ");
				ExecSQL(sql.ToString());
			}
			if (Params.Contains(2))
			{
				sql.Remove(0, sql.Length);
				sql.Append(" update t_svod_all set geu = (" +
						   " select geu from " + ReportParams.Pref + DBManager.sDataAliasRest + "s_geu a " +
						   " where a.nzp_geu = t_svod_all.nzp_geu) ");
				ExecSQL(sql.ToString());
			}
			if (Params.Contains(4))
			{
				sql.Remove(0, sql.Length);
				sql.Append(" update t_svod_all set " +
						   " rajon = (select rajon from " + ReportParams.Pref + DBManager.sDataAliasRest + "s_rajon a " +
						   " where a.nzp_raj = t_svod_all.nzp_raj), " +
						   " ulica = (select ulica from " + ReportParams.Pref + DBManager.sDataAliasRest + "s_ulica a " +
						   " where a.nzp_ul = t_svod_all.nzp_ul) ");
				ExecSQL(sql.ToString());
			}
			if (Params.Contains(5))
			{
				sql.Remove(0, sql.Length);
				sql.Append(" update t_svod_all set " +
						   " ndom = (select ndom from " + ReportParams.Pref + DBManager.sDataAliasRest + "dom a " +
						   " where a.nzp_dom = t_svod_all.nzp_dom), " +
						   " idom = (select idom from " + ReportParams.Pref + DBManager.sDataAliasRest + "dom a " +
                           " where a.nzp_dom = t_svod_all.nzp_dom), " +
                           " nkor = (select CASE WHEN trim(nkor) <> '' AND trim(nkor) <> '-' THEN ' к.'||nkor END from " + ReportParams.Pref + DBManager.sDataAliasRest + "dom a " +
						   " where a.nzp_dom = t_svod_all.nzp_dom and nkor<>'-') ");
				ExecSQL(sql.ToString());
			}
			if (Params.Contains(6))
			{
				sql.Remove(0, sql.Length);
				sql.Append(" update t_svod_all set nkvar = (" +
						   " select nkvar from " + ReportParams.Pref + DBManager.sDataAliasRest + "kvar a " +
						   " where a.nzp_kvar = t_svod_all.nzp_kvar), " +
						   " ikvar = ( select ikvar from " + ReportParams.Pref + DBManager.sDataAliasRest + "kvar a " +
						   " where a.nzp_kvar = t_svod_all.nzp_kvar) ");
				ExecSQL(sql.ToString());
			}
			if (Params.Contains(9))
			{
				sql.Remove(0, sql.Length);
				sql.Append(" update t_svod_all set name_supp = (" +
						   " select name_supp from " + ReportParams.Pref + DBManager.sKernelAliasRest + "supplier a " +
						   " where a.nzp_supp = t_svod_all.nzp_supp) ");
				ExecSQL(sql.ToString());
			}
			if (Params.Contains(10))
			{
				sql.Remove(0, sql.Length);
				sql.Append(" update t_svod_all set service = (" +
						   " select service from " + ReportParams.Pref + DBManager.sKernelAliasRest + "services a " +
						   " where a.nzp_serv = t_svod_all.nzp_serv) ");
				ExecSQL(sql.ToString());
			}

			return order.ToString();
		}

		/// <summary>
		/// Получить условия органичения по УК
		/// </summary>
		/// <returns></returns>
		private string GetWhereArea() {
			string whereArea = String.Empty;
			whereArea = Areas != null ? Areas.Aggregate(whereArea, (current, nzpArea) => current + (nzpArea + ",")) : ReportParams.GetRolesCondition(Constants.role_sql_area);
			whereArea = whereArea.TrimEnd(',');
			whereArea = !String.IsNullOrEmpty(whereArea) ? " AND k.nzp_area in (" + whereArea + ")" : String.Empty;
			if (!String.IsNullOrEmpty(whereArea))
			{
				string sql = " SELECT area from " + ReportParams.Pref + DBManager.sDataAliasRest + "s_area k  WHERE k.nzp_area > 0 " + whereArea;
				DataTable area = ExecSQLToTable(sql);
				foreach (DataRow dr in area.Rows)
				{
					AreasHeader += dr["area"].ToString().Trim() + ", ";
				}
				AreasHeader = AreasHeader.TrimEnd(',', ' ');
			}
			return whereArea;
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
				result = " AND " + pref + "nzp_geu in (" + result + ") ";

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
		/// Получить условия органичения по поставщикам
		/// </summary>
		/// <returns></returns>
		private string GetWhereSupp() {
			string whereSupp = String.Empty;
			whereSupp = Suppliers != null ? Suppliers.Aggregate(whereSupp, (current, nzpSupp) => current + (nzpSupp + ",")) : ReportParams.GetRolesCondition(Constants.role_sql_supp);
			whereSupp = whereSupp.TrimEnd(',');
			whereSupp = !String.IsNullOrEmpty(whereSupp) ? " AND nzp_supp in (" + whereSupp + ")" : String.Empty;
			if (!String.IsNullOrEmpty(whereSupp))
			{
				string sql = " SELECT name_supp from " + ReportParams.Pref + DBManager.sKernelAliasRest + "supplier  WHERE nzp_supp > 0 " + whereSupp;
				DataTable supp = ExecSQLToTable(sql);
				foreach (DataRow dr in supp.Rows)
				{
					SuppliersHeader += dr["name_supp"].ToString().Trim() + ", ";
				}
				SuppliersHeader = SuppliersHeader.TrimEnd(',', ' ');
			}
			return whereSupp;
		}

		/// <summary>
		/// Получить условия органичения по услугам
		/// </summary>
		/// <returns></returns>
		private string GetWhereServ() {
			string whereServ = String.Empty;
			whereServ = Services != null ? Services.Aggregate(whereServ, (current, nzpServ) => current + (nzpServ + ",")) : ReportParams.GetRolesCondition(Constants.role_sql_serv);
			whereServ = whereServ.TrimEnd(',');
			whereServ = !String.IsNullOrEmpty(whereServ) ? " AND nzp_serv in (" + whereServ + ")" : String.Empty;
			if (!String.IsNullOrEmpty(whereServ))
			{
				string sql = " SELECT service from " + ReportParams.Pref + DBManager.sKernelAliasRest + "services  WHERE nzp_serv > 0 " + whereServ;
				DataTable serv = ExecSQLToTable(sql);
				foreach (DataRow dr in serv.Rows)
				{
					ServicesHeader += dr["service"].ToString().Trim() + ", ";
				}
				ServicesHeader = ServicesHeader.TrimEnd(',', ' ');

			}
			return whereServ;
		}

		private string GetwhereWp() {
			string whereWp = String.Empty;
			if (Banks != null)
			{
				whereWp = Banks.Aggregate(whereWp, (current, nzpWp) => current + (nzpWp + ","));
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

		protected override void CreateTempTable() {

		}

		private void CreateTSvod() {
			var sql = new StringBuilder();
			sql.Append(" create temp table t_svod( ");
			if (Params.Contains(1)) { sql.Append(" nzp_area integer, "); }
			if (Params.Contains(2)) { sql.Append(" nzp_geu integer, "); }
			if (Params.Contains(3)) { sql.Append(" uch integer, "); }
			if (Params.Contains(4)) { sql.Append(" nzp_raj integer, nzp_ul integer, "); }
			if (Params.Contains(5)) { sql.Append(" nzp_dom integer, "); }
			if (Params.Contains(6)) { sql.Append(" nzp_kvar integer, "); }
			if (Params.Contains(7)) { sql.Append(" num_ls integer, "); }
			if (Params.Contains(8)) { sql.Append(" pkod " + DBManager.sDecimalType + "(13,0), "); }
			if (Params.Contains(9)) { sql.Append(" nzp_supp integer, "); }
			if (Params.Contains(10)) { sql.Append(" nzp_serv integer, "); }
			if (Params.Contains(11)) { sql.Append(" sum_insaldo " + DBManager.sDecimalType + "(14,2), "); }
			if (Params.Contains(12)) { sql.Append(" tarif " + DBManager.sDecimalType + "(14,2), "); }
			if (Params.Contains(13)) { sql.Append(" rsum_tarif " + DBManager.sDecimalType + "(14,2), "); }
			if (Params.Contains(14)) { sql.Append(" sum_nedop " + DBManager.sDecimalType + "(14,2), "); }
			if (Params.Contains(15)) { sql.Append(" sum_tarif " + DBManager.sDecimalType + "(14,2), "); }
			if (Params.Contains(16)) { sql.Append(" reval " + DBManager.sDecimalType + "(14,2), "); }
			if (Params.Contains(17)) { sql.Append(" real_charge " + DBManager.sDecimalType + "(14,2), "); }
			if (Params.Contains(18)) { sql.Append(" sum_charge " + DBManager.sDecimalType + "(14,2), "); }
			if (Params.Contains(19)) { sql.Append(" sum_money " + DBManager.sDecimalType + "(14,2), "); }
			if (Params.Contains(20)) { sql.Append(" sum_outsaldo " + DBManager.sDecimalType + "(14,2), "); }
			sql.Remove(sql.Length - 2, 2);
			sql.Append(") ");
			ExecSQL(sql.ToString());

			sql.Remove(0, sql.Length);
			sql.Append(" create temp table t_svod_all( " +
						" nzp_area integer, " +
						" area char(100), " +
						" nzp_geu integer, " +
						" geu char(100), " +
						" uch integer, " +
						" nzp_raj integer, nzp_ul integer, " +
						" rajon char(100), ulica char(100), " +
						" nzp_dom integer, " +
						" idom integer, " +
						" ikvar integer, " +
						" ndom char(15), " +
						" nkor char(15), " +
						" nzp_kvar integer, " +
						" nkvar char(40), " +
						" num_ls integer, " +
						" pkod " + DBManager.sDecimalType + "(13,0), " +
						" nzp_supp integer, " +
						" name_supp char(100), " +
						" nzp_serv integer, " +
						" service char(100), " +
						" sum_insaldo " + DBManager.sDecimalType + "(14,2), " +
						" tarif " + DBManager.sDecimalType + "(14,2), " +
						" rsum_tarif " + DBManager.sDecimalType + "(14,2), " +
						" sum_nedop " + DBManager.sDecimalType + "(14,2), " +
						" sum_tarif " + DBManager.sDecimalType + "(14,2), " +
						" reval " + DBManager.sDecimalType + "(14,2), " +
						" real_charge " + DBManager.sDecimalType + "(14,2), " +
						" sum_charge " + DBManager.sDecimalType + "(14,2), " +
						" sum_money " + DBManager.sDecimalType + "(14,2), " +
						" sum_outsaldo " + DBManager.sDecimalType + "(14,2)) ");
			ExecSQL(sql.ToString());

			sql.Remove(0, sql.Length);
			sql.Append(" create temp table t_title( " +
						" area char(50), " +
						" geu char(50), " +
						" uch char(50), " +
						" rajon char(50), " +
						" ndom char(50), " +
						" nkvar char(50), " +
						" num_ls char(50), " +
						" pkod char(50), " +
						" name_supp char(100), " +
						" service char(50), " +
						" sum_insaldo char(50), " +
						" tarif char(50), " +
						" rsum_tarif char(50), " +
						" sum_nedop char(50), " +
						" sum_tarif char(50), " +
						" reval char(50), " +
						" real_charge char(50), " +
						" sum_charge char(50), " +
						" sum_money char(50), " +
						" sum_outsaldo char(50)) ");
			ExecSQL(sql.ToString());
		}

		protected override void DropTempTable() {
			try
			{
				if (TempTableInWebCashe("t_svod"))
				{
					ExecSQL(" drop table t_svod ");
				}
				if (TempTableInWebCashe("t_svod_all"))
				{
					ExecSQL(" drop table t_svod_all ");
				}
				if (TempTableInWebCashe("t_title"))
				{
					ExecSQL(" drop table t_title ");
				}
			}
			catch (Exception e)
			{
				MonitorLog.WriteLog("Отчет 'Генератор по начислениям' " + e.Message, MonitorLog.typelog.Error, false);
			}
		}

	}
}
