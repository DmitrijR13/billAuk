using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using Bars.KP50.Report.Base;
using Bars.KP50.Report.Tula.Properties;
using Bars.KP50.Utils;
using Newtonsoft.Json;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;

namespace Bars.KP50.Report.Tula.Reports
{
	class Report71GenNach : BaseSqlReport
	{
		public override string Name {
			get { return "71. Генератор по начислениям"; }
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
			get { return Resources._71_Gen_nach; }
		}

		public override IList<ReportKind> ReportKinds {
			get { return new List<ReportKind> { ReportKind.Base, ReportKind.ListLC }; }
		}

		//~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

        /// <summary>Расчетный месяц</summary>
        protected int MonthS { get; set; }

        /// <summary>Расчетный год</summary>
        protected int YearS { get; set; }


        /// <summary>Расчетный месяц</summary>
        protected int MonthPo { get; set; }

        /// <summary>Расчетный год</summary>
        protected int YearPo { get; set; }

		/// <summary>Услуги</summary>
		private List<long> Services { get; set; }

		/// <summary>Поставщики</summary>
		private string SupplierHeader { get; set; }

		/// <summary>Услуги</summary>
		private string ServiceHeader { get; set; }

		/// <summary>Заголовок территории</summary>
		protected string TerritoryHeader { get; set; }

		/// <summary>Параметры</summary>
		private List<long> Params { get; set; }

		/// <summary>Статус ЛС</summary>
		private List<int> StatusLs { get; set; }

		private bool RowCount { get; set; }

		/// <summary>Поставщики, Агенты, Принципалы  </summary>
        protected BankSupplierParameterValue BankSupplier { get; set; }

        /// <summary>Районы </summary>
        private List<int> RaionsDoms { get; set; }

		/// <summary>Участок</summary>
		protected string Uchastok { get; set; }

		/// <summary>Управляющие компании</summary>
		protected List<int> Areas { get; set; }

		/// <summary>Территория</summary>
		protected List<int> Geus { get; set; }

		/// <summary>Управляющие компании</summary>
		protected string AreaHeader { get; set; }

		/// <summary>ЖЭУ</summary>
		protected string GeuHeader { get; set; }

		//~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

		public override List<UserParam> GetUserParams() {
			var curCalcMonthYear = DBManager.GetCurMonthYear();
			return new List<UserParam>
			{
                new MonthParameter {Name = "Месяц с", Value = curCalcMonthYear != null ? curCalcMonthYear.Rows[0]["month_"] : DateTime.Today.Month },
                new YearParameter {Name = "Год с", Value = curCalcMonthYear != null ? curCalcMonthYear.Rows[0]["yearr"] : DateTime.Today.Year },
                new MonthParameter {Name = "Месяц по", Code = "Month1", Value = curCalcMonthYear != null ? curCalcMonthYear.Rows[0]["month_"] : DateTime.Today.Month },
                new YearParameter {Name = "Год по", Code = "Year1", Value = curCalcMonthYear != null ? curCalcMonthYear.Rows[0]["yearr"] : DateTime.Today.Year },
  				new BankSupplierParameter(),
				new AreaParameter(),
				new GeuParameter(),
				new UchastokParameter(),
				new ServiceParameter(),
                new RaionsDomsParameter(),
				new ComboBoxParameter(true)
				{
					Name = "Статус ЛС",
					Code = "StatusLS",
					StoreData = new List<object>
					{
						new { Id = 1, Name = "Открытый" },
						new { Id = 2, Name = "Закрытый" },
						new { Id = 3, Name = "Неопределен" }
					}
				},
				new ComboBoxParameter(true) {
					Name = "Параметры отчета", 
					Code = "Params",
					Value = 1,
					Require = true,
					StoreData = new List<object> {
						new { Id = 21, Name = "Банк данных"},
						new { Id = 1, Name = "Территория (УК)"},
						new { Id = 2, Name = "ЖЭУ"},
						new { Id = 3, Name = "Участок"},
						new { Id = 22, Name = "Район"},
						new { Id = 4, Name = "Улица"},
						new { Id = 5, Name = "Дом"},
						new { Id = 6, Name = "Квартира"},
						new { Id = 7, Name = "Лицевой счет"},
						new { Id = 8, Name = "Платежный код"},
						new { Id = 9, Name = "Поставщик"},
						new { Id = 10, Name = "Услуга"},
						new { Id = 11, Name = "Входящее сальдо"},
						new { Id = 12, Name = "Тариф"},
						new { Id = 13, Name = "Недопоставка"},
						new { Id = 14, Name = "Начислено с учетом недопоставки"},
						new { Id = 15, Name = "К оплате"},
						new { Id = 16, Name = "Оплачено"},
						new { Id = 17, Name = "Корректировка"},
						new { Id = 18, Name = "Начислено"},
						new { Id = 19, Name = "Перерасчет"},
						new { Id = 20, Name = "Исходящее сальдо"}
					}
				},
			};
		}

		protected override void PrepareReport(FastReport.Report report) {
			string statusLS = string.Empty;
			if (StatusLs != null)
			{
				statusLS = StatusLs.Contains(1) ? "Открытый, " : string.Empty;
				statusLS += StatusLs.Contains(2) ? "Закрытый, " : string.Empty;
				statusLS += StatusLs.Contains(3) ? "Неопределен, " : string.Empty;
				statusLS = statusLS.TrimEnd(',', ' ');
			}
			var months = new[] {"","Январь","Февраль",
				 "Март","Апрель","Май","Июнь","Июль","Август","Сентябрь",
				 "Октябрь","Ноябрь","Декабрь"};
			report.SetParameterValue("dat", DateTime.Now.ToLongDateString());
			report.SetParameterValue("time", DateTime.Now.ToLongTimeString());
		    if (MonthS == MonthPo && YearS == YearPo)
		    {
		        report.SetParameterValue("pPeriod", months[MonthS] + " " + YearS + "г.");
		    }
		    else
		    {
                report.SetParameterValue("pPeriod", "c "+ months[MonthS] + " " + YearS + "г. по "+ months[MonthPo] + " " + YearPo + "г.");    
		    }
            


			Uchastok = Uchastok.Replace("null", "Не установлен");
			string headerParam = !string.IsNullOrEmpty(TerritoryHeader) ? "Территория: " + TerritoryHeader + "\n" : string.Empty;
			headerParam += !string.IsNullOrEmpty(statusLS) ? "Статус ЛС: " + statusLS + "\n" : string.Empty;
			headerParam += !string.IsNullOrEmpty(Uchastok) ? "Участок: " + Uchastok + "\n" : string.Empty;
			headerParam += !string.IsNullOrEmpty(AreaHeader) ? "Балансодержатель: " + AreaHeader + "\n" : string.Empty;
			headerParam += !string.IsNullOrEmpty(GeuHeader) ? "ЖЭУ: " + GeuHeader + "\n" : string.Empty;
			headerParam += !string.IsNullOrEmpty(SupplierHeader) ? "Поставщики: " + SupplierHeader + "\n" : string.Empty;
			headerParam += !string.IsNullOrEmpty(ServiceHeader) ? "Услуги: " + ServiceHeader : string.Empty;
			headerParam = headerParam.TrimEnd('\n');
			report.SetParameterValue("headerParam", headerParam);

			string limit = ReportParams.ExportFormat == ExportFormat.Excel2007 ? "40000" : "100000";

			report.SetParameterValue("excel",
				RowCount
					? "Выборка записей ограничена первыми " + limit + " строками. Выберите другой формат экспортируемого файла, либо поставьте другие ограничения для отчета"
					: "");
		}

		protected override void PrepareParams() {
            MonthS = UserParamValues["Month"].GetValue<int>();
            YearS = UserParamValues["Year"].Value.To<int>();
            MonthPo = UserParamValues["Month1"].GetValue<int>();
            YearPo = UserParamValues["Year1"].Value.To<int>();

            Services = UserParamValues["Services"].GetValue<List<long>>();
            RaionsDoms = UserParamValues["RaionsDoms"].GetValue<List<int>>();
			Params = UserParamValues["Params"].GetValue<List<long>>();
			StatusLs = UserParamValues["StatusLS"].GetValue<List<int>>();
			if (Params.Contains(6))
			{
				if (!Params.Contains(5)) { Params.Add(5); }
				if (!Params.Contains(4)) { Params.Add(4); }
			}
			if (Params.Contains(5))
			{
				if (!Params.Contains(4)) { Params.Add(4); }
			}

			var uch = UserParamValues["Uchastok"].GetValue<List<int>>() ?? new List<int>();
			Uchastok = uch.Any() ? uch.Aggregate(Uchastok, (current, nmbUch) => current + (nmbUch + ", ")).TrimEnd(' ', ',') : string.Empty;
			Uchastok = Uchastok.Replace("-999", "null"); //Участок со значением null
			Areas = UserParamValues["Areas"].GetValue<List<int>>();
			Geus = UserParamValues["Geu"].GetValue<List<int>>();

			BankSupplier = JsonConvert.DeserializeObject<BankSupplierParameterValue>(UserParamValues["BankSupplier"].Value.ToString());
		}

		public override DataSet GetData() {
			var sql = new StringBuilder();
			MyDataReader reader;
			string datS = "1." + MonthS + "." + YearS,
					datPo = DateTime.DaysInMonth(YearPo, MonthPo) + "." + MonthPo + "." + YearPo;
			string whereSupp = GetWhereSupp("c.nzp_supp");
			string whereServ = GetWhereServ();
		    string whereRajDom = GetWhereRajDom("d.");
			string whereArea = GetWhereArea("k.");
			string whereGeu = GetWhereGeu("k.");
			string whereUchastok = !string.IsNullOrEmpty(Uchastok)
				? " AND (k.uch IN ( " + Uchastok + " ) " + (Uchastok.Contains("null") ? " OR k.uch IS NULL) " : ") ")
				: string.Empty;


			bool listLc = GetSelectedKvars();

			CreateTSvod();

			sql.Remove(0, sql.Length);
			sql.Append(" select bd_kernel as pref, point " +
				  " FROM " + ReportParams.Pref + DBManager.sKernelAliasRest + "s_point " +
				  " where nzp_wp>1 " + GetwhereWp());
			ExecRead(out reader, sql.ToString());
			while (reader.Read())
			{
                string pref = reader["pref"].ToStr().Trim();
                string point = reader["point"].ToStr().Trim();
			    for (int i = YearS*12 + MonthS; i < YearPo*12 + MonthPo + 1; i++)
			    {
			        int mo = i%12;
			        int ye = mo == 0 ? (i/12) - 1 : (i/12);
			        if (mo == 0) mo = 12;

                    string sumInsaldo = ((mo == MonthS) & (ye == YearS)) ? "sum_insaldo" : "0";
                    string sumOutsaldo = ((mo == MonthPo) & (ye == YearPo)) ? "sum_outsaldo" : "0";
			        ExecSQL(" DELETE FROM t_report_71_Nach ");

			        if (StatusLs != null)
			        {
			            string stat = " AND val_prm in ( ";
			            stat += StatusLs.Contains(1) ? "'1', " : string.Empty;
			            stat += StatusLs.Contains(2) ? "'2', " : string.Empty;
			            stat += StatusLs.Contains(3) ? "'3', " : string.Empty;
			            stat = stat.TrimEnd(',', ' ');
			            stat += ")";
			            {
			                sql.Remove(0, sql.Length);
			                sql.Append(" INSERT INTO t_report_71_Nach ( nzp_kvar ) " +
			                           " SELECT DISTINCT nzp" +
			                           " FROM " + pref + DBManager.sDataAliasRest + "prm_3 " +
			                           " WHERE dat_s <= '" + datPo + "' " +
			                           " AND dat_po >= '" + datS + "' " +
			                           " AND is_actual = 1 " +
			                           " AND nzp_prm = 51 " +
			                           stat);
			                ExecSQL(sql.ToString());
			            }
			        }
			        else
			        {
			            sql.Remove(0, sql.Length);
			            sql.Append(" INSERT INTO t_report_71_Nach ( nzp_kvar ) " +
			                       " SELECT DISTINCT nzp_kvar " +
			                       " FROM " + pref + DBManager.sDataAliasRest + "kvar k, " +
			                       pref + DBManager.sDataAliasRest + "dom d, " +
			                       pref + DBManager.sDataAliasRest + "s_ulica u " +
			                       " WHERE k.nzp_dom = d.nzp_dom " +
			                       " AND d.nzp_ul = u.nzp_ul ");
			            ExecSQL(sql.ToString());
			        }

			        sql.Remove(0, sql.Length);
                    sql.Append(" INSERT INTO t_svod( ");
                    if (Params.Contains(21)) sql.Append(" pref, ");
                    if (Params.Contains(1)) sql.Append(" nzp_area, ");
			        if (Params.Contains(2)) sql.Append(" nzp_geu, ");
                    if (Params.Contains(3)) sql.Append(" uch, ");
                    if (Params.Contains(22)) sql.Append(" nzp_raj_dom, ");
                    if (Params.Contains(4)) sql.Append(" nzp_raj, nzp_ul, ");
			        if (Params.Contains(5)) sql.Append(" nzp_dom, ");
			        if (Params.Contains(6)) sql.Append(" nzp_kvar, ");
			        if (Params.Contains(7)) sql.Append(" num_ls, ");
			        if (Params.Contains(8)) sql.Append(" pkod, ");
			        if (Params.Contains(9)) sql.Append(" nzp_supp, ");
			        if (Params.Contains(10)) sql.Append(" nzp_serv, ");
			        if (Params.Contains(11)) sql.Append(" sum_insaldo, ");
			        if (Params.Contains(12)) sql.Append(" tarif, ");
			        if (Params.Contains(13)) sql.Append(" sum_nedop, ");
			        if (Params.Contains(14)) sql.Append(" sum_tarif, ");
			        if (Params.Contains(15)) sql.Append(" sum_charge, ");
			        if (Params.Contains(16)) sql.Append(" sum_money, ");
			        if (Params.Contains(17)) sql.Append(" real_charge, ");
			        if (Params.Contains(18)) sql.Append(" rsum_tarif, ");
			        if (Params.Contains(19)) sql.Append(" reval, ");
			        if (Params.Contains(20)) sql.Append(" sum_outsaldo, ");
			        sql.Remove(sql.Length - 2, 2);
			        sql.Append(") ");
                    sql.Append(" SELECT ");
                    if (Params.Contains(21)) sql.Append(" '" + point + "', ");
			        if (Params.Contains(1)) sql.Append(" k.nzp_area, ");
			        if (Params.Contains(2)) sql.Append(" k.nzp_geu, ");
                    if (Params.Contains(3)) sql.Append(" uch, ");
                    if (Params.Contains(22)) sql.Append(" nzp_raj_dom, ");
			        if (Params.Contains(4)) sql.Append(" u.nzp_raj, u.nzp_ul, ");
			        if (Params.Contains(5)) sql.Append(" d.nzp_dom, ");
			        if (Params.Contains(6)) sql.Append(" k.nzp_kvar, ");
			        if (Params.Contains(7)) sql.Append(" k.num_ls, ");
			        if (Params.Contains(8)) sql.Append(" pkod, ");
			        if (Params.Contains(9)) sql.Append(" nzp_supp, ");
			        if (Params.Contains(10)) sql.Append(" nzp_serv, ");
                    if (Params.Contains(11)) sql.Append(" " + sumInsaldo + ", ");
			        if (Params.Contains(12)) sql.Append(" tarif, ");
			        if (Params.Contains(13)) sql.Append(" sum_nedop, ");
			        if (Params.Contains(14)) sql.Append(" sum_tarif, ");
			        if (Params.Contains(15)) sql.Append(" sum_charge, ");
			        if (Params.Contains(16)) sql.Append(" sum_money, ");
			        if (Params.Contains(17)) sql.Append(" real_charge, ");
			        if (Params.Contains(18)) sql.Append(" rsum_tarif, ");
			        if (Params.Contains(19)) sql.Append(" reval, ");
                    if (Params.Contains(20)) sql.Append(" " + sumOutsaldo + ", ");
			        sql.Remove(sql.Length - 2, 2);
			        sql.Append(" FROM " + (listLc ? " selected_kvars k " : pref + DBManager.sDataAliasRest + "kvar k "));
			        sql.Append(" INNER JOIN " + pref + "_charge_" + (ye - 2000).ToString("00") + DBManager.tableDelimiter + "charge_" +
                               mo.ToString("00") + " c ON k.nzp_kvar = c.nzp_kvar AND c.dat_charge is null AND c.nzp_serv > 1 ");
                    sql.Append(" INNER JOIN t_report_71_Nach t ON k.nzp_kvar = t.nzp_kvar ");
			        if (Params.Contains(4))
			        {
			            sql.Append(" INNER JOIN " + pref + DBManager.sDataAliasRest + "dom d ON k.nzp_dom = d.nzp_dom " + whereRajDom +
                            (Params.Contains(22) ? " INNER JOIN " + pref + DBManager.sDataAliasRest + "s_rajon_dom rd ON d.nzp_raj = rd.nzp_raj_dom " : string.Empty) +
                            " INNER JOIN " + pref + DBManager.sDataAliasRest + "s_ulica u ON d.nzp_ul = u.nzp_ul ");
			        }
                    else if (Params.Contains(22))
                    {
                        sql.Append(" INNER JOIN " + pref + DBManager.sDataAliasRest + "dom d ON k.nzp_dom = d.nzp_dom " + whereRajDom +
                                   " INNER JOIN " + pref + DBManager.sDataAliasRest + "s_rajon_dom rd ON d.nzp_raj = rd.nzp_raj_dom ");
                    }
			        sql.Append(" WHERE 1=1 " + whereArea + whereGeu + whereUchastok + whereSupp + whereServ);

			        ExecSQL(sql.ToString());
			    }
			}
			reader.Close();

            string order = FillSvodTable();


			DataTable dt;
			try
			{
			    dt = ExecSQLToTable(
                        " SELECT pref, area, geu, uch, rajon_dom, rajon, ulica, ndom, idom, nkor, nkvar, ikvar, num_ls, pkod, name_supp, service, " +
                        " SUM(sum_insaldo) as sum_insaldo, tarif, SUM(sum_nedop) as sum_nedop, SUM(sum_tarif) as sum_tarif, SUM(sum_charge) as sum_charge, " +
                        " SUM(sum_money) as sum_money, SUM(real_charge) as real_charge, SUM(rsum_tarif) as rsum_tarif, SUM(reval) as reval, SUM(sum_outsaldo) as sum_outsaldo " +
			            " FROM t_svod_all " +
                        " GROUP BY pref, area, geu, uch, rajon_dom, rajon, ulica, ndom,idom, nkor, nkvar, ikvar, num_ls, pkod, name_supp, service, tarif ", 2000);
				var dv = new DataView(dt) { Sort = order };
				dt = dv.ToTable();
				RowCount = false;
			}
			catch (Exception)
			{
			    dt = ExecSQLToTable(DBManager.SetLimitOffset(
                            " SELECT pref, area, geu, uch, rajon_dom, rajon, ulica, ndom, idom, nkor, nkvar, ikvar, num_ls, pkod, name_supp, service, " +
			                " SUM(sum_insaldo) as sum_insaldo, tarif, SUM(sum_nedop) as sum_nedop, SUM(sum_tarif) as sum_tarif, SUM(sum_charge) as sum_charge, " +
			                " SUM(sum_money) as sum_money, SUM(real_charge) as real_charge, SUM(rsum_tarif) as rsum_tarif, SUM(reval) as reval, SUM(sum_outsaldo) as sum_outsaldo " +
                            " FROM t_svod_all " +
                            " GROUP BY pref, area, geu, uch, rajon_dom, rajon, ulica, ndom,idom, nkor, nkvar, ikvar, num_ls, pkod, name_supp, service, tarif ", 100000, 0), 2000);
				var dv = new DataView(dt) { Sort = order };
				dt = dv.ToTable();
				RowCount = true;
			}
			dt.TableName = "Q_master";
            RowCount = false;

            if (ReportParams.ExportFormat == ExportFormat.Excel2007 && dt.Rows.Count >= 40000)
				{
					var dtr = dt.Rows.Cast<DataRow>().Skip(40000).ToArray();
					dtr.ForEach(dt.Rows.Remove);
                    RowCount = true;
				}


			sql.Remove(0, sql.Length);
            sql.Append(" insert into t_title values( ");
            sql.Append(Params.Contains(21) ? " 'Банк данных' , " : " '' , ");
            sql.Append(Params.Contains(1) ? " 'УК' , " : " '' , ");
			sql.Append(Params.Contains(2) ? " 'ЖЭУ' , " : " '' , ");
            sql.Append(Params.Contains(3) ? " 'Участок' , " : " '' , ");
            sql.Append(Params.Contains(22) ? " 'Район' , " : " '' , ");
            sql.Append(Params.Contains(4) ? " 'Улица' , " : " '' , ");
			sql.Append(Params.Contains(5) ? " 'Дом' , " : " '' , ");
			sql.Append(Params.Contains(6) ? " 'Квартира' , " : " '' , ");
			sql.Append(Params.Contains(7) ? " 'ЛС' , " : " '' , ");
			sql.Append(Params.Contains(8) ? " 'Пл. код' , " : " '' , ");
			sql.Append(Params.Contains(9) ? " 'Поставщик' , " : " '' , ");
			sql.Append(Params.Contains(10) ? " 'Услуга' , " : " '' , ");
			sql.Append(Params.Contains(11) ? " 'Вх. сальдо' , " : " '' , ");
			sql.Append(Params.Contains(12) ? " 'Тариф' , " : " '' , ");
			sql.Append(Params.Contains(13) ? " 'Недопоставка' , " : " '' , ");
			sql.Append(Params.Contains(14) ? " 'Начислено с учетом недоп-ки' , " : " '' , ");
			sql.Append(Params.Contains(15) ? " 'К оплате' , " : " '' , ");
			sql.Append(Params.Contains(16) ? " 'Оплачено' , " : " '' , ");
			sql.Append(Params.Contains(17) ? " 'Корректировка' , " : " '' , ");
			sql.Append(Params.Contains(18) ? " 'Начислено' , " : " '' , ");
			sql.Append(Params.Contains(19) ? " 'Перерасчет' , " : " '' , ");
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

        private string FillSvodTable()
        {
			var sql = new StringBuilder();
			var grouper = new StringBuilder();
			var order = new StringBuilder();

            sql.Append(" insert into t_svod_all( ");
            if (Params.Contains(21)) { sql.Append(" pref, "); }
            if (Params.Contains(1)) { sql.Append(" nzp_area, "); }
            if (Params.Contains(2)) { sql.Append(" nzp_geu, "); }
            if (Params.Contains(3)) { sql.Append(" uch, "); }
            if (Params.Contains(22)) { sql.Append(" nzp_raj_dom, "); }
			if (Params.Contains(4)) { sql.Append(" nzp_raj, nzp_ul, "); }
			if (Params.Contains(5)) { sql.Append(" nzp_dom, "); }
			if (Params.Contains(6)) { sql.Append(" nzp_kvar, "); }
			if (Params.Contains(7)) { sql.Append(" num_ls, "); }
			if (Params.Contains(8)) { sql.Append(" pkod, "); }
			if (Params.Contains(9)) { sql.Append(" nzp_supp, "); }
			if (Params.Contains(10)) { sql.Append(" nzp_serv, "); }
			if (Params.Contains(11)) sql.Append(" sum_insaldo, ");
			if (Params.Contains(12)) sql.Append(" tarif, ");
			if (Params.Contains(13)) sql.Append(" sum_nedop, ");
			if (Params.Contains(14)) sql.Append(" sum_tarif, ");
			if (Params.Contains(15)) sql.Append(" sum_charge, ");
			if (Params.Contains(16)) sql.Append(" sum_money, ");
			if (Params.Contains(17)) sql.Append(" real_charge, ");
			if (Params.Contains(18)) sql.Append(" rsum_tarif, ");
			if (Params.Contains(19)) sql.Append(" reval, ");
			if (Params.Contains(20)) sql.Append(" sum_outsaldo, ");
			sql.Remove(sql.Length - 2, 2);
			sql.Append(") ");
            sql.Append(" select ");
            if (Params.Contains(21)) { sql.Append(" pref, "); grouper.Append(" pref, "); order.Append(" pref, "); }
            if (Params.Contains(1)) { sql.Append(" nzp_area, "); grouper.Append(" nzp_area, "); order.Append(" area, "); }
            if (Params.Contains(2)) { sql.Append(" nzp_geu, "); grouper.Append(" nzp_geu, "); order.Append(" geu, "); }
            if (Params.Contains(3)) { sql.Append(" uch, "); grouper.Append(" uch, "); order.Append(" uch, "); }
            if (Params.Contains(22)) { sql.Append(" nzp_raj_dom, "); grouper.Append(" nzp_raj_dom, "); order.Append(" rajon_dom, "); }
			if (Params.Contains(4)) { sql.Append(" nzp_raj, nzp_ul, "); grouper.Append(" nzp_raj, nzp_ul, "); order.Append(" rajon, ulica, "); }
			if (Params.Contains(5)) { sql.Append(" nzp_dom, "); grouper.Append(" nzp_dom, "); order.Append(" idom, nkor, "); }
			if (Params.Contains(6)) { sql.Append(" nzp_kvar, "); grouper.Append(" nzp_kvar, "); order.Append(" ikvar, "); }
			if (Params.Contains(7)) { sql.Append(" num_ls, "); grouper.Append(" num_ls, "); order.Append(" num_ls, "); }
			if (Params.Contains(8)) { sql.Append(" pkod, "); grouper.Append(" pkod, "); order.Append(" pkod, "); }
			if (Params.Contains(9)) { sql.Append(" nzp_supp, "); grouper.Append(" nzp_supp, "); order.Append(" name_supp, "); }
			if (Params.Contains(10)) { sql.Append(" nzp_serv, "); grouper.Append(" nzp_serv, "); order.Append(" service, "); }
            if (Params.Contains(11)) { sql.Append(" sum(sum_insaldo) as sum_insaldo, "); }
			if (Params.Contains(12)) { sql.Append(" sum(tarif) as tarif, "); }
			if (Params.Contains(13)) { sql.Append(" sum(sum_nedop) as sum_nedop, "); }
			if (Params.Contains(14)) { sql.Append(" sum(sum_tarif) as sum_tarif, "); }
			if (Params.Contains(15)) { sql.Append(" sum(sum_charge) as sum_charge, "); }
			if (Params.Contains(16)) { sql.Append(" sum(sum_money) as sum_money, "); }
			if (Params.Contains(17)) { sql.Append(" sum(real_charge) as real_charge, "); }
			if (Params.Contains(18)) { sql.Append(" sum(rsum_tarif) as rsum_tarif, "); }
			if (Params.Contains(19)) { sql.Append(" sum(reval) as reval, "); }
            if (Params.Contains(20)) { sql.Append(" sum(sum_outsaldo) as sum_outsaldo, "); }
			sql.Remove(sql.Length - 2, 2);
			sql.Append(" from t_svod ");

			if (grouper.Length > 0)
			{
				grouper.Remove(grouper.Length - 2, 2);
				sql.Append(" group by " + grouper);
				ExecSQL(" create index svod_index on t_svod(" + grouper + ") ");
			}

			if (order.Length > 2) { order.Remove(order.Length - 2, 2); }
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
            if (Params.Contains(22))
            {
                sql.Remove(0, sql.Length);
                sql.Append(" UPDATE t_svod_all SET rajon_dom = (" +
                           " SELECT rajon_dom FROM " + ReportParams.Pref + DBManager.sDataAliasRest + "s_rajon_dom a " +
                           " WHERE a.nzp_raj_dom = t_svod_all.nzp_raj_dom) ");
                ExecSQL(sql.ToString());
            }
			if (Params.Contains(4))
			{
				sql.Remove(0, sql.Length);
				sql.Append(" update t_svod_all set " +
						   " rajon = (select rajon from " + ReportParams.Pref + DBManager.sDataAliasRest + "s_rajon a " +
						   " where a.nzp_raj = t_svod_all.nzp_raj), " +
						   " ulica = (select TRIM(" + DBManager.sNvlWord + "(ulicareg,''))||' '||TRIM(ulica) from " + ReportParams.Pref + DBManager.sDataAliasRest + "s_ulica a " +
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
						   " ikvar = (" +
						   " select ikvar from " + ReportParams.Pref + DBManager.sDataAliasRest + "kvar a " +
						   " where a.nzp_kvar = t_svod_all.nzp_kvar)");
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
		/// Получить условия органичения по поставщикам
		/// </summary>
		/// <returns></returns>
		private string GetWhereSupp(string fieldPref) {
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
			return " and " + fieldPref + " in (select nzp_supp from " +
				   ReportParams.Pref + DBManager.sKernelAliasRest + "supplier " +
				   " where nzp_supp>0 " + whereSupp + ")";
		}

        /// <summary>
        /// получает ограничение из таблицы s_rajon_dom
        /// </summary>
        /// <param name="tablePrefix"></param>
        /// <returns></returns>
        private string GetWhereRajDom(string tablePrefix)
        {
            string whereRajDom = String.Empty;
            if (RaionsDoms != null)
            {
                whereRajDom = RaionsDoms.Aggregate(whereRajDom, (current, nzpRajDom) => current + (nzpRajDom + ","));
                whereRajDom = whereRajDom.TrimEnd(',');
                whereRajDom = " AND d.nzp_raj IN (" + whereRajDom + ") ";
            }
            return whereRajDom;
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
				ServiceHeader = string.Empty;
				string sql = " SELECT service from " + ReportParams.Pref + DBManager.sKernelAliasRest + "services  WHERE nzp_serv > 0 " + whereServ;
				DataTable serv = ExecSQLToTable(sql);
				foreach (DataRow dr in serv.Rows)
				{
					ServiceHeader += dr["service"].ToString().Trim() + ", ";
				}
				ServiceHeader = ServiceHeader.TrimEnd(',', ' ');

			}
			return whereServ;
		}

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

		/// <summary> Получить условия органичения по УК </summary>
		private string GetWhereArea(string pref) {
			var result = String.Empty;
			result = Areas != null
				? Areas.Aggregate(result, (current, nzpArea) => current + (nzpArea + ","))
				: ReportParams.GetRolesCondition(Constants.role_sql_area);

			result = result.TrimEnd(',');
			if (!String.IsNullOrEmpty(result))
			{
				result = " AND " + pref + "nzp_area in (" + result + ") ";

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

		protected override void CreateTempTable() {
			if (ReportParams.CurrentReportKind == ReportKind.ListLC)
			{
				const string sqls = " create temp table selected_kvars(" +
									" nzp_kvar integer," +
									" num_ls integer," +
									" nzp_dom integer," +
									" nzp_geu integer," +
                                    " uch integer," +
									" nzp_area integer) " +
									DBManager.sUnlogTempTable;
				ExecSQL(sqls);
			}

			const string sql = " CREATE TEMP TABLE t_report_71_Nach( nzp_kvar INTEGER ) " + DBManager.sUnlogTempTable;
			ExecSQL(sql);
		}

		private void CreateTSvod() {
			var sql = new StringBuilder();
            sql.Append(" create temp table t_svod( ");
            if (Params.Contains(21)) { sql.Append(" pref char(100), "); }
            if (Params.Contains(1)) { sql.Append(" nzp_area integer, "); }
            if (Params.Contains(2)) { sql.Append(" nzp_geu integer, "); }
            if (Params.Contains(3)) { sql.Append(" uch integer, "); }
            if (Params.Contains(22)) { sql.Append(" nzp_raj_dom integer, "); }
			if (Params.Contains(4)) { sql.Append(" nzp_raj integer, nzp_ul integer, "); }
			if (Params.Contains(5)) { sql.Append(" nzp_dom integer, "); }
			if (Params.Contains(6)) { sql.Append(" nzp_kvar integer, "); }
			if (Params.Contains(7)) { sql.Append(" num_ls integer, "); }
			if (Params.Contains(8)) { sql.Append(" pkod " + DBManager.sDecimalType + "(13,0), "); }
			if (Params.Contains(9)) { sql.Append(" nzp_supp integer, "); }
			if (Params.Contains(10)) { sql.Append(" nzp_serv integer, "); }
			if (Params.Contains(11)) { sql.Append(" sum_insaldo " + DBManager.sDecimalType + "(14,2), "); }
			if (Params.Contains(12)) { sql.Append(" tarif " + DBManager.sDecimalType + "(14,2), "); }
			if (Params.Contains(13)) { sql.Append(" sum_nedop " + DBManager.sDecimalType + "(14,2), "); }
			if (Params.Contains(14)) { sql.Append(" sum_tarif " + DBManager.sDecimalType + "(14,2), "); }
			if (Params.Contains(15)) { sql.Append(" sum_charge " + DBManager.sDecimalType + "(14,2), "); }
			if (Params.Contains(16)) { sql.Append(" sum_money " + DBManager.sDecimalType + "(14,2), "); }
			if (Params.Contains(17)) { sql.Append(" real_charge " + DBManager.sDecimalType + "(14,2), "); }
			if (Params.Contains(18)) { sql.Append(" rsum_tarif " + DBManager.sDecimalType + "(14,2), "); }
			if (Params.Contains(19)) { sql.Append(" reval " + DBManager.sDecimalType + "(14,2), "); }
			if (Params.Contains(20)) { sql.Append(" sum_outsaldo " + DBManager.sDecimalType + "(14,2), "); }
			sql.Remove(sql.Length - 2, 2);
			sql.Append(") ");
			ExecSQL(sql.ToString());

			sql.Remove(0, sql.Length);
            sql.Append(" create temp table t_svod_all( " +
                        " pref char(100), " +
                        " nzp_area integer, " +
						" area char(100), " +
						" nzp_geu integer, " +
                        " geu char(100), " +
                        " uch integer, " +
                        " nzp_raj_dom integer, rajon_dom char(100), " +
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
						" sum_nedop " + DBManager.sDecimalType + "(14,2), " +
						" sum_tarif " + DBManager.sDecimalType + "(14,2), " +
						" sum_charge " + DBManager.sDecimalType + "(14,2), " +
						" sum_money " + DBManager.sDecimalType + "(14,2), " +
						" real_charge " + DBManager.sDecimalType + "(14,2), " +
						" rsum_tarif " + DBManager.sDecimalType + "(14,2), " +
						" reval " + DBManager.sDecimalType + "(14,2), " +
						" sum_outsaldo " + DBManager.sDecimalType + "(14,2)) ");
			ExecSQL(sql.ToString());

			sql.Remove(0, sql.Length);
            sql.Append(" create temp table t_title( " +
                        " pref char(50), " +
                        " area char(50), " +
						" geu char(50), " +
                        " uch char(50), " +
                        " rajon_dom char(50), " +
                        " rajon char(50), " +
						" ndom char(50), " +
						" nkvar char(50), " +
						" num_ls char(50), " +
						" pkod char(50), " +
						" name_supp char(100), " +
						" service char(50), " +
						" sum_insaldo char(50), " +
						" tarif char(50), " +
						" sum_nedop char(50), " +
						" sum_tarif char(50), " +
						" sum_charge char(50), " +
						" sum_money char(50), " +
						" real_charge char(50), " +
						" rsum_tarif char(50), " +
						" reval char(50), " +
						" sum_outsaldo char(50)) ");
			ExecSQL(sql.ToString());

		}

		protected override void DropTempTable() {
			try
			{
				ExecSQL(" DROP TABLE t_report_71_Nach ");
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
				if (ReportParams.CurrentReportKind == ReportKind.ListLC)
					ExecSQL(" drop table selected_kvars ", true);
			}
			catch (Exception e)
			{
				MonitorLog.WriteLog("Отчет 'Генератор по начислениям' " + e.Message, MonitorLog.typelog.Error, false);
			}
		}

		/// <summary>
		/// Выборка списка квартир в картотеке
		///  </summary>
		/// <returns></returns>
		private bool GetSelectedKvars() {
			if (ReportParams.CurrentReportKind == ReportKind.ListLC)
			{
				using (IDbConnection connWeb = DBManager.GetConnection(Constants.cons_Webdata))
				{
					if (!DBManager.OpenDb(connWeb, true).result) return false;

					string tSpls = DBManager.GetFullBaseName(connWeb) + DBManager.tableDelimiter +
								   "t" + ReportParams.User.nzp_user + "_spls";
					if (TempTableInWebCashe(tSpls))
					{
						string sql = " insert into selected_kvars (nzp_kvar, num_ls, nzp_dom, nzp_geu, nzp_area, uch) " +
									 " select a.nzp_kvar, a.num_ls, a.nzp_dom, a.nzp_geu, a.nzp_area, k.uch " +
						             " from " + tSpls+" a, "+
                                     ReportParams.Pref+"_data.kvar k" +
						             " where a.nzp_kvar=k.nzp_kvar ";
						ExecSQL(sql);
						ExecSQL("create index ix_sel_kvar_09 on selected_kvars(nzp_kvar)");
						ExecSQL(DBManager.sUpdStat + " selected_kvars");
						return true;
					}
				}
			}
			return false;
		}

	}
}
