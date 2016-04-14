using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Bars.KP50.Report.Base;
using Bars.KP50.Report.Tula.Properties;
using Bars.KP50.Utils;
using Newtonsoft.Json;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;

namespace Bars.KP50.Report.Tula.Reports
{
	class Report71119 : BaseSqlReport
	{
		/// <summary>Наименование отчета</summary>
		public override string Name {
			get { return "71.1.19 Сведение об образовавшейся задолжнности населения"; }
		}

		/// <summary>Описание отчета</summary>
		public override string Description {
			get { return "71.1.19 Сведение об образовавшейся задолжнности населения"; }
		}

		/// <summary>Группа отчета</summary>
		public override IList<ReportGroup> ReportGroups {
			get { return new List<ReportGroup> { 0 }; }
		}

		/// <summary>Предварительный просмотр</summary>
		public override bool IsPreview {
			get { return false; }
		}

		/// <summary>Файл-шаблон отчета</summary>
		protected override byte[] Template {
			get { return Resources.Report_71_1_19; }
		}

		/// <summary>Тип отчета</summary>
		public override IList<ReportKind> ReportKinds {
			get { return new List<ReportKind> { ReportKind.Base, ReportKind.ListLC }; }
		}

		#region~~~~~~~~~~~~~~~~~~~~~Параметры~~~~~~~~~~~~~~~~~~~~~~~~~~~~

		/// <summary>Расчетный месяц</summary>
		private int Month { get; set; }

		/// <summary>Расчетный год</summary>
		private int Year { get; set; }

		/// <summary>Поставщики, Агенты, Принципалы  </summary>
		private BankSupplierParameterValue BankSupplier { get; set; }

		/// <summary>Услуги</summary>
		private List<int> Services { get; set; }

		/// <summary>Заголовок территории</summary>
		protected string TerritoryHeader { get; set; }

		/// <summary>Заголовок - агент</summary>
		protected string AgentHeader { get; set; }

		/// <summary>Заголовок - принципал</summary>
		protected string PrincipalHeader { get; set; }

		/// <summary>Заголовок - поставщик</summary>
		protected string SupplierHeader { get; set; }

		/// <summary>Заголовок услуг</summary>
		private string ServiceHeader { get; set; }

		/// <summary>Задолженность свыше 3-х месяцев</summary>
		private Decimal Dolg3 { get; set; }

		/// <summary>Задолженность свыше 6-х месяцев</summary>
		private Decimal Dolg6 { get; set; }

		/// <summary>Задолженность свыше 9-х месяцев</summary>
		private Decimal Dolg9 { get; set; }

		/// <summary>Задолженность свыше 12-х месяцев</summary>
		private Decimal Dolg12 { get; set; }

		/// <summary>Задолженность свыше 24-х месяцев</summary>
		private Decimal Dolg24 { get; set; }

		#endregion~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

		/// <summary>Параметры для отображение на странице браузера</summary>
		/// <returns>Список параметров</returns>
		public override List<UserParam> GetUserParams() {
			var curCalcMonthYear = DBManager.GetCurMonthYear();
			return new List<UserParam>
			{
				new MonthParameter {Value = curCalcMonthYear != null ? curCalcMonthYear.Rows[0]["month_"] : DateTime.Today.Month },
				new YearParameter {Value = curCalcMonthYear != null ? curCalcMonthYear.Rows[0]["yearr"] : DateTime.Today.Year },
				new BankSupplierParameter(),
				new ServiceParameter()
			};
		}

		/// <summary>Заполнить параметры</summary>
		protected override void PrepareParams() {
			Month = UserParamValues["Month"].GetValue<int>();
			Year = UserParamValues["Year"].GetValue<int>();
			BankSupplier = JsonConvert.DeserializeObject<BankSupplierParameterValue>(UserParamValues["BankSupplier"].Value.ToString());
			Services = UserParamValues["Services"].GetValue<List<int>>();

			if (Month == 0)
				throw new ReportException("Не определено значение \"Расчетный месяц\"");
			if (Year == 0)
				throw new ReportException("Не определено значение \"Расчетный год\"");
		}

		/// <summary>Заполнить параметры в отчете</summary>
		/// <param name="report">Объект формы отчета</param>
		protected override void PrepareReport(FastReport.Report report) {
			var monthsR = new[]
			{
			 "", "Января" , "Февраля", "Марта",
				 "Апреля" , "Мая"    , "Июня", 
				 "Июля"   , "Августа" , "Сентября",
				 "Октября", "Ноября" , "Декабря"
			};
			var monthsI = new[]
			{
			 "", "Январь" , "Февраль", "Март",
				 "Апрель" , "Май"    , "Июнь", 
				 "Июль"   , "Август" , "Сентябрь",
				 "Октябрь", "Ноябрь" , "Декабрь"
			};
			var monthsO = new[]
			{
			 "", "январе" , "феврале", "марте",
				 "апреле" , "мае"    , "июне", 
				 "июле"   , "августе" , "сентябре",
				 "октябре", "ноябре" , "декабре"
			};
			int month = Month == 12 ? 1 : Month + 1,
				year = Month == 12 ? Year + 1 : Year;
			report.SetParameterValue("period", "на 1 " + monthsR[month] + " " + year + " г.");
			report.SetParameterValue("last_period", "за " + monthsI[Month] + " " + Year + " г.");
			report.SetParameterValue("month", monthsO[Month]);
			report.SetParameterValue("DATE", DateTime.Now.ToShortDateString());
			report.SetParameterValue("TIME", DateTime.Now.ToShortTimeString());

			string headerParam = !string.IsNullOrEmpty(TerritoryHeader) ? "Территория: " + TerritoryHeader + "\n" : string.Empty;
			headerParam += !string.IsNullOrEmpty(AgentHeader) ? "Агент: " + AgentHeader + "\n" : string.Empty;
			headerParam += !string.IsNullOrEmpty(PrincipalHeader) ? "Принципал: " + PrincipalHeader + "\n" : string.Empty;
			headerParam += !string.IsNullOrEmpty(SupplierHeader) ? "Поставщик: " + SupplierHeader + "\n" : string.Empty;
			headerParam += !string.IsNullOrEmpty(ServiceHeader) ? "Услуги: " + ServiceHeader : string.Empty;
			headerParam = headerParam.TrimEnd('\n');
			report.SetParameterValue("headerParam", headerParam);

			report.SetParameterValue("dolg3", Dolg3);
			report.SetParameterValue("dolg6", Dolg6);
			report.SetParameterValue("dolg9", Dolg9);
			report.SetParameterValue("dolg12", Dolg12);
			report.SetParameterValue("dolg24", Dolg24);
		}

		/// <summary>Выборка данных</summary>
		/// <returns>Кеш данных</returns>
		public override DataSet GetData() {
			MyDataReader reader;
			string prefData = ReportParams.Pref + DBManager.sDataAliasRest,
					centralKernel = ReportParams.Pref + DBManager.sKernelAliasRest;

			string whereWp = GetWhereWp(),
				   whereSupp = GetWhereSupp(),
				   whereServ = GetWhereServ();
		    bool isKart = IsListLS();
		    whereWp += (isKart
		        ? " AND nzp_wp IN (SELECT DISTINCT nzp_wp " +
		                         " FROM " + prefData + "kvar " +
		                         " WHERE nzp_kvar IN (SELECT nzp_kvar FROM selected_kvars)) "
		        : string.Empty);

			string sql = " SELECT bd_kernel " +
						 " FROM " + centralKernel + "s_point " +
						 " WHERE nzp_wp > 1 " + whereWp;
			ExecRead(out reader, sql);

			while (reader.Read())
			{
				string pref = reader["bd_kernel"].ToStr().ToLower().Trim();
				string chargeYYcurrent = pref + "_charge_" + (Year - 2000).ToString("00") + DBManager.tableDelimiter +
										  "charge_" + Month.ToString("00");
                string tablePerekidka = pref + "_charge_" + (Year - 2000).ToString("00") + DBManager.tableDelimiter +
                                            "perekidka";
				for (int i = Year * 12 + Month - 24; i <= Year * 12 + Month; i++)
				{
					var year = i / 12;
					var month = i % 12;
					if (month == 0)
					{
						year--;
						month = 12;
					}

					string chargeYY = pref + "_charge_" + (year - 2000).ToString("00") + DBManager.tableDelimiter +
									  "charge_" + month.ToString("00");
                    string lPerekidka = pref + "_charge_" + (year - 2000).ToString("00") + DBManager.tableDelimiter +
                                              "perekidka ";
				    string caseSuppServ = whereSupp != string.Empty ? whereSupp.TrimStart(' ', 'A', 'N', 'D') : string.Empty;
				    caseSuppServ += caseSuppServ == string.Empty
				        ? whereServ.TrimStart(' ', 'A', 'N', 'D')
                        : whereServ;

					if (TempTableInWebCashe(chargeYY))
					{
					    string selectNachosP = caseSuppServ != string.Empty
					        ? " SUM(CASE WHEN " + caseSuppServ +
					          " THEN (sum_tarif + reval) " +
					          " ELSE 0 END) "
					        : " SUM(sum_tarif + reval) ",
					        selectMonthP = caseSuppServ != string.Empty
                                ? " MAX(CASE WHEN " + caseSuppServ + " THEN 1 ELSE 0 END) "
					            : " 1 ",
                            realChargeP = caseSuppServ != string.Empty
                                ? " SUM(CASE WHEN " + caseSuppServ + " THEN sum_rcl ELSE 0 END) "
                                : " SUM(sum_rcl) ";

                        sql = " INSERT INTO t_nachis_71_1_19(month_year, nzp_kvar, month_ls, month_p, nachis_ls, nachis_p) " +
                              " SELECT '" + month + year + "' AS month_year, " +
                                     " nzp_kvar, " +
                                     " 1 AS month_ls,  " + 
                                       selectMonthP + " AS month_p, " +
									 " SUM(sum_tarif + reval) AS nachis_ls, " +
									   selectNachosP + " AS nachis_p " +
							  " FROM " + chargeYY +
							  " WHERE dat_charge IS NULL " +
								" AND nzp_serv > 1 " +
                                " AND ABS(sum_tarif + reval) > 0.01 " +
                              " GROUP BY month_year, nzp_kvar ";
						ExecSQL(sql);
                        ExecSQL(DBManager.sUpdStat + " t_nachis_71_1_19");

                        sql = " UPDATE t_nachis_71_1_19 SET real_charge_ls = (SELECT SUM(sum_rcl)" +
                                                                            " FROM " + lPerekidka + " p " +
                                                                            " WHERE p.nzp_kvar = t_nachis_71_1_19.nzp_kvar " +
                                                                              " AND p.type_rcl = 102 " +
                                                                              " AND p.month_ = " + month + " ) " +
                              " WHERE TRIM(month_year) = '" + month + year + "' ";
                        ExecSQL(sql);

                        sql = " UPDATE t_nachis_71_1_19 SET real_charge_p = (SELECT " + realChargeP +
                                                                           " FROM " + lPerekidka + " p " +
                                                                           " WHERE p.nzp_kvar = t_nachis_71_1_19.nzp_kvar " +
                                                                             " AND p.type_rcl = 102 " +
                                                                             " AND p.month_ = " + month + " ) " +
                              " WHERE TRIM(month_year) = '" + month + year + "' ";
                        ExecSQL(sql);
					}
				}
				ExecSQL(DBManager.sUpdStat + " t_nachis_71_1_19");

                ExecSQL(" UPDATE t_nachis_71_1_19 SET real_charge_ls = 0 WHERE real_charge_ls IS NULL ");
                ExecSQL(" UPDATE t_nachis_71_1_19 SET real_charge_p = 0 WHERE real_charge_p IS NULL ");

				sql = " INSERT INTO t_average_71_1_19(nzp_kvar, nachis_av_ls, nachis_av_p) " +
					  " SELECT nzp_kvar, " +
                           " (CASE WHEN SUM(month_ls) > 0 THEN SUM(nachis_ls + real_charge_ls) / SUM(month_ls) ELSE 0 END) AS nachis_av_ls, " +
                           " (CASE WHEN SUM(month_p) > 0 THEN SUM(nachis_p + real_charge_p) / SUM(month_p) ELSE 0 END) AS nachis_av_p " +
					  " FROM t_nachis_71_1_19 " +
					  " GROUP BY nzp_kvar ";
				ExecSQL(sql);
				ExecSQL("DELETE FROM t_nachis_71_1_19");

                ExecSQL(DBManager.sUpdStat + " t_average_71_1_19");

			    string selectCaseSupp = whereSupp != string.Empty ? whereSupp.TrimStart(' ', 'A', 'N', 'D') : string.Empty;
			    selectCaseSupp += whereServ != string.Empty
			                            ? selectCaseSupp != string.Empty 
                                            ? whereServ 
                                            : whereServ.TrimStart(' ', 'A', 'N', 'D')
			                            : string.Empty;
                sql = " INSERT INTO t_report_71_1_19(nzp_kvar, tarif_reval, sum_money_p, sum_money_l, sum_outsaldo_p, sum_outsaldo_l) " +
					  " SELECT nzp_kvar, " +
                             " SUM(CASE WHEN " + selectCaseSupp + " THEN sum_tarif + reval ELSE 0 END) AS tarif_reval, " +
                             " SUM(CASE WHEN " + selectCaseSupp + " THEN sum_money ELSE 0 END) AS sum_money_p, " +
					         " SUM(sum_money) AS sum_money_l, " +
                             " SUM(CASE WHEN " + selectCaseSupp + " THEN sum_outsaldo ELSE 0 END) AS sum_outsaldo_p, " +
					         " SUM(sum_outsaldo) AS sum_outsaldo_l " +
					  " FROM " + chargeYYcurrent +
					  " WHERE nzp_serv > 1 " +
					    " AND dat_charge IS NULL " + 
					  " GROUP BY nzp_kvar ";
				ExecSQL(sql);

				ExecSQL(DBManager.sUpdStat + " t_report_71_1_19");

                sql = " UPDATE t_report_71_1_19 SET real_charge = (SELECT SUM(sum_rcl)" +
                                                                 " FROM " + tablePerekidka + " p " +
                                                                 " WHERE type_rcl = 102 " +
                                                                   " AND p.nzp_kvar = t_report_71_1_19.nzp_kvar " + 
                                                                     whereSupp + whereServ +
                                                                   " AND month_ = " + Month + ") ";
                ExecSQL(sql);

                ExecSQL(" UPDATE t_report_71_1_19 SET real_charge = 0 WHERE real_charge IS NULL ");

				sql = " UPDATE t_report_71_1_19 SET nachis_av_ls = " +
					  " (SELECT nachis_av_ls " +
					  " FROM t_average_71_1_19 t " +
					  " WHERE t.nzp_kvar = t_report_71_1_19.nzp_kvar) ";
				ExecSQL(sql);

				sql = " UPDATE t_report_71_1_19 SET nachis_av_p = " +
					  " (SELECT nachis_av_p " +
					  " FROM t_average_71_1_19 t " +
					  " WHERE t.nzp_kvar = t_report_71_1_19.nzp_kvar) ";
				ExecSQL(sql);
				ExecSQL("DELETE FROM t_average_71_1_19");
			}
			reader.Close();

			sql = " UPDATE t_report_71_1_19 " +
				  " SET month_dolg_ls = (CASE WHEN nachis_av_ls <> 0 THEN sum_outsaldo_l / nachis_av_ls ELSE 0 END) ";
			ExecSQL(sql);

			sql = " UPDATE t_report_71_1_19 " +
				  " SET month_dolg_p = (CASE WHEN nachis_av_p <> 0 THEN sum_outsaldo_p / nachis_av_p ELSE 0 END) ";
			ExecSQL(sql);

			sql = " UPDATE t_report_71_1_19 " +
				  " SET month_dolg = (CASE WHEN month_dolg_ls < month_dolg_p THEN month_dolg_ls ELSE month_dolg_p END) ";
			ExecSQL(sql);

		    sql = " UPDATE t_report_71_1_19 " +
		          " SET sum_money = sum_money_p "; //"(CASE WHEN month_dolg_ls < month_dolg_p THEN sum_money_l ELSE sum_money_p END) ";
			ExecSQL(sql);

		    sql = " UPDATE t_report_71_1_19 " +
		          " SET sum_outsaldo = sum_outsaldo_p "; //" (CASE WHEN month_dolg_ls < month_dolg_p THEN sum_outsaldo_l ELSE sum_outsaldo_p END) ";
			ExecSQL(sql);

			sql = " UPDATE t_report_71_1_19 " +
                  " SET nachis = nachis_av_p ";//(CASE WHEN month_dolg_ls < month_dolg_p THEN nachis_av_ls ELSE nachis_av_p END)
			ExecSQL(sql);

            string fromKvar = isKart ? "selected_kvars " : prefData + "kvar ";

			sql = " INSERT INTO t_kvar_71_1_19 " +
				  " SELECT nzp_kvar, num_ls, fio, nkvar, ikvar, " +
						 " ndom, idom, ulica, rajon, town " +
                  " FROM " + fromKvar + " k INNER JOIN " + prefData + "dom d ON d.nzp_dom = k.nzp_dom " +
											  " INNER JOIN " + prefData + "s_ulica u ON u.nzp_ul = d.nzp_ul " +
											  " INNER JOIN " + prefData + "s_rajon r ON r.nzp_raj = u.nzp_raj " +
											  " INNER JOIN " + prefData + "s_town t ON t.nzp_town = r.nzp_town ";
			ExecSQL(sql);

			ExecSQL(DBManager.sUpdStat + " t_kvar_71_1_19");

			DataTable dt = GetTableData(string.Empty, string.Empty);

            string where = " WHERE month_dolg > 3 AND month_dolg <= 6 AND nachis > 0 ";
			DataTable dt3 = GetTableData("3", where);
			foreach (DataRow dr in dt3.Rows) Dolg3 += dr["sum_outsaldo"] != DBNull.Value ? Convert.ToDecimal(dr["sum_outsaldo"]) : 0;

            where = " WHERE month_dolg > 6 AND month_dolg <= 9 AND nachis > 0 ";
			DataTable dt6 = GetTableData("6", where);
			foreach (DataRow dr in dt6.Rows) Dolg6 += dr["sum_outsaldo"] != DBNull.Value ? Convert.ToDecimal(dr["sum_outsaldo"]) : 0;

            where = " WHERE month_dolg > 9 AND month_dolg <= 12 AND nachis > 0 ";
			DataTable dt9 = GetTableData("9", where);
			foreach (DataRow dr in dt9.Rows) Dolg9 += dr["sum_outsaldo"] != DBNull.Value ? Convert.ToDecimal(dr["sum_outsaldo"]) : 0;

            where = " WHERE month_dolg > 12 AND month_dolg <= 24 AND nachis > 0 ";
			DataTable dt12 = GetTableData("12", where);
			foreach (DataRow dr in dt12.Rows) Dolg12 += dr["sum_outsaldo"] != DBNull.Value ? Convert.ToDecimal(dr["sum_outsaldo"]) : 0;

            where = " WHERE month_dolg > 24 AND nachis > 0 ";
			DataTable dt24 = GetTableData("24", where);
			foreach (DataRow dr in dt24.Rows) Dolg24 += dr["sum_outsaldo"] != DBNull.Value ? Convert.ToDecimal(dr["sum_outsaldo"]) : 0;

			where = " WHERE nachis = 0 AND sum_outsaldo > 0 ";
			DataTable dt0 = GetTableData("0", where);

			var ds = new DataSet();
			ds.Tables.Add(dt);
			ds.Tables.Add(dt3);
			ds.Tables.Add(dt6);
			ds.Tables.Add(dt9);
			ds.Tables.Add(dt12);
			ds.Tables.Add(dt24);
			ds.Tables.Add(dt0);

			return ds;
		}

		#region~~~~~~~~~~~~~~~~~~~~~~Фильтр~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

		/// <summary>Ограничение по банку данных</summary>
		/// <returns>Условие выборки для sql-запроса</returns>
		private string GetWhereWp() {
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
				TerritoryHeader = string.Empty;
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

		/// <summary>Ограничения по поставщику</summary>
		/// <returns>Условие выборки для sql-запроса</returns>
		private string GetWhereSupp() {
			string prefKernel = ReportParams.Pref + DBManager.sKernelAliasRest;
			string sql;
			string whereSupp = string.Empty;
			string whereRoleSupp = string.Empty;
			DataTable payer;
			string oldsupp = ReportParams.GetRolesCondition(Constants.role_sql_supp);

			if (!String.IsNullOrEmpty(oldsupp))
				whereRoleSupp = " p INNER JOIN " + prefKernel + " supplier s ON (s.nzp_payer_supp = p.nzp_payer " +
																		   " AND s.nzp_supp IN (" + oldsupp + ")) ";

			if (BankSupplier != null && BankSupplier.Suppliers != null)
			{
				string supp = string.Empty;
				supp = BankSupplier.Suppliers.Aggregate(supp, (current, nzpSupp) => current + (nzpSupp + ","));

				if (string.IsNullOrEmpty(SupplierHeader))
				{
					SupplierHeader = string.Empty;
					sql = " SELECT payer " +
						  " FROM " + prefKernel + "s_payer " + whereRoleSupp +
						  " WHERE nzp_payer IN (" + supp.TrimEnd(',') + ") ";
					payer = ExecSQLToTable(sql);
					foreach (DataRow dr in payer.Rows)
					{
						SupplierHeader += dr["payer"].ToString().Trim() + ", ";
					}
					SupplierHeader = SupplierHeader.TrimEnd(',', ' ');
				}

				whereSupp += " and nzp_payer_supp in (" + supp.TrimEnd(',') + ")";
			}

			if (BankSupplier != null && BankSupplier.Principals != null)
			{
				string supp = string.Empty;
				supp = BankSupplier.Principals.Aggregate(supp, (current, nzpSupp) => current + (nzpSupp + ","));

				if (string.IsNullOrEmpty(PrincipalHeader))
				{
					PrincipalHeader = string.Empty;
					sql = " SELECT payer " +
						  " FROM " + prefKernel + "s_payer " + whereRoleSupp +
						  " WHERE nzp_payer IN (" + supp.TrimEnd(',') + ") ";
					payer = ExecSQLToTable(sql);
					foreach (DataRow dr in payer.Rows)
					{
						PrincipalHeader += dr["payer"].ToString().Trim() + ", ";
					}
					PrincipalHeader = PrincipalHeader.TrimEnd(',', ' ');
				}

				whereSupp += " and nzp_payer_princip in (" + supp.TrimEnd(',') + ")";
			}

			if (BankSupplier != null && BankSupplier.Agents != null)
			{
				string supp = string.Empty;
				supp = BankSupplier.Agents.Aggregate(supp, (current, nzpSupp) => current + (nzpSupp + ","));

				if (string.IsNullOrEmpty(AgentHeader))
				{
					AgentHeader = string.Empty;
					sql = " SELECT payer " +
						  " FROM " + prefKernel + "s_payer " + whereRoleSupp +
						  " WHERE nzp_payer IN (" + supp.TrimEnd(',') + ") ";
					payer = ExecSQLToTable(sql);
					foreach (DataRow dr in payer.Rows)
					{
						AgentHeader += dr["payer"].ToString().Trim() + ", ";
					}
					AgentHeader = AgentHeader.TrimEnd(',', ' ');
				}

				whereSupp += " and nzp_payer_agent in (" + supp.TrimEnd(',') + ")";
			}

			return " AND nzp_supp in (SELECT nzp_supp " +
											 " FROM " + prefKernel + "supplier " +
											 " WHERE nzp_supp > 0 " + whereSupp.TrimEnd(',') + ")";
		}

		/// <summary>Ограничения по услугам</summary>
		/// <returns>Условие выборки для sql-запроса</returns>
		private string GetWhereServ() {
			string whereServ = String.Empty;
			whereServ = Services != null
				? Services.Aggregate(whereServ, (current, nzpServ) => current + (nzpServ + ","))
				: ReportParams.GetRolesCondition(Constants.role_sql_serv);
			whereServ = whereServ.TrimEnd(',');
			whereServ = !String.IsNullOrEmpty(whereServ) ? " AND nzp_serv in (" + whereServ + ") " : String.Empty;

			if (!string.IsNullOrEmpty(whereServ) && string.IsNullOrEmpty(ServiceHeader))
			{
				ServiceHeader = String.Empty;
				string sql = " SELECT service " +
							 " FROM " + ReportParams.Pref + DBManager.sKernelAliasRest + "services " +
							 " WHERE nzp_serv > 0 " + whereServ;
				DataTable servTable = ExecSQLToTable(sql);
				foreach (DataRow row in servTable.Rows)
				{
					ServiceHeader += row["service"].ToString().Trim() + ", ";
				}
				ServiceHeader = ServiceHeader.TrimEnd(',', ' ');
			}
			return whereServ;
		}

        /// <summary>Ограничение лицевых счетов</summary>
        /// <returns>Индикатор использования фильтра</returns>
        private bool IsListLS() {
            if (ReportParams.CurrentReportKind == ReportKind.ListLC)
            {
                using (IDbConnection connWeb = DBManager.GetConnection(Constants.cons_Webdata))
                {
                    if (!DBManager.OpenDb(connWeb, true).result) return false;

                    string tSpls = DBManager.GetFullBaseName(connWeb) + DBManager.tableDelimiter +
                                   "t" + ReportParams.User.nzp_user + "_spls";
                    if (TempTableInWebCashe(tSpls))
                    {
                        if (TempTableInWebCashe("selected_kvars")) ExecSQL("DROP TABLE selected_kvars ");
                        ExecSQL(" SELECT nzp_kvar, nzp_dom, num_ls, fio, nkvar, ikvar INTO TEMP selected_kvars FROM " + tSpls);
                        return true;
                    }
                }
            }
            return false;
        }

		#endregion~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

		/// <summary>Выборка в таблицу</summary>
		/// <param name="numberTable">Номер Q_master</param>
		/// <param name="where">Условие выборки</param>
		/// <returns>Таблица с данными в памяти</returns>
		private DataTable GetTableData(string numberTable, string where) {
			string sql = " SELECT TRIM(town) AS town, " +
								" TRIM(rajon) AS rajon, " +
								" TRIM(ulica) AS ulica, " +
								" TRIM(ndom) AS ndom, idom, " +
								" TRIM(nkvar) AS nkvar, ikvar, " +
								" TRIM(fio) AS fio, " +
								" num_ls, " +
                                (numberTable == string.Empty && where == string.Empty ? " tarif_reval + real_charge " : " nachis ") + " AS nachis, " + 
								" sum_money, " +
								" sum_outsaldo " +
						 " FROM t_report_71_1_19 r INNER JOIN t_kvar_71_1_19 k ON k.nzp_kvar = r.nzp_kvar " + where +
						 " ORDER BY town, rajon, ulica, idom, ndom, ikvar, nkvar, fio ";
			DataTable dt = ExecSQLToTable(sql);
			dt.TableName = "Q_master" + numberTable;
			return dt;
		}

		/// <summary>Создание временных таблиц</summary>
		protected override void CreateTempTable() {
			string sql = " CREATE TEMP TABLE t_report_71_1_19(" +
						 " nzp_kvar INTEGER, " +
                         " tarif_reval " + DBManager.sDecimalType + "(14,2), " +
                         " real_charge " + DBManager.sDecimalType + "(14,2), " +
						 " month_dolg " + DBManager.sDecimalType + "(14,2), " +
						 " month_dolg_ls " + DBManager.sDecimalType + "(14,2), " +
						 " month_dolg_p " + DBManager.sDecimalType + "(14,2), " +
						 " nachis " + DBManager.sDecimalType + "(14,2), " +
						 " nachis_av_ls " + DBManager.sDecimalType + "(14,2), " +
						 " nachis_av_p " + DBManager.sDecimalType + "(14,2), " +
						 " sum_money " + DBManager.sDecimalType + "(14,2), " +
						 " sum_money_p " + DBManager.sDecimalType + "(14,2), " +
						 " sum_money_l " + DBManager.sDecimalType + "(14,2), " +
						 " sum_outsaldo " + DBManager.sDecimalType + "(14,2), " +
						 " sum_outsaldo_p " + DBManager.sDecimalType + "(14,2), " +
						 " sum_outsaldo_l " + DBManager.sDecimalType + "(14,2))" + DBManager.sUnlogTempTable;
			ExecSQL(sql);

			ExecSQL("CREATE INDEX ix_t_report_71_1_19 ON t_report_71_1_19(nzp_kvar) ");

			sql = " CREATE TEMP TABLE t_kvar_71_1_19( " +
				  " nzp_kvar INTEGER, " +
				  " num_ls INTEGER, " +
				  " fio CHARACTER(50), " +
				  " nkvar CHARACTER(10), " +
				  " ikvar INTEGER, " +
				  " ndom CHARACTER(10), " +
				  " idom INTEGER, " +
				  " ulica CHARACTER(40), " +
				  " rajon CHARACTER(30), " +
				  " town CHARACTER(30)) " + DBManager.sUnlogTempTable;
			ExecSQL(sql);

			ExecSQL("CREATE INDEX ix_t_kvar_71_1_19 ON t_kvar_71_1_19(nzp_kvar) ");

			sql = " CREATE TEMP TABLE t_nachis_71_1_19( " +
			        " month_year CHARACTER(10), " +
					" nzp_kvar INTEGER, " +
					" month_ls INTEGER, " +
                    " month_p INTEGER, " +
                    " real_charge_ls " + DBManager.sDecimalType + "(14,2), " +
                    " real_charge_p " + DBManager.sDecimalType + "(14,2), " +
					" nachis_ls " + DBManager.sDecimalType + "(14,2), " +
					" nachis_p " + DBManager.sDecimalType + "(14,2)) " + DBManager.sUnlogTempTable;
			ExecSQL(sql);

			ExecSQL("CREATE INDEX ix_t_nachis_71_1_19 ON t_nachis_71_1_19(nzp_kvar) ");

			sql = " CREATE TEMP TABLE t_average_71_1_19(" +
					" nzp_kvar INTEGER, " +
					" nachis_av_ls " + DBManager.sDecimalType + "(14,2), " +
					" nachis_av_p " + DBManager.sDecimalType + "(14,2)) " + DBManager.sUnlogTempTable;
			ExecSQL(sql);

			ExecSQL("CREATE INDEX ix_t_average_71_1_19 ON t_average_71_1_19(nzp_kvar) ");
		}

		/// <summary>Удаление временных таблиц</summary>
		protected override void DropTempTable() {
			ExecSQL("DROP TABLE t_report_71_1_19");
			ExecSQL("DROP TABLE t_kvar_71_1_19");
			ExecSQL("DROP TABLE t_nachis_71_1_19");
			ExecSQL("DROP TABLE t_average_71_1_19");
		}
	}
}
