using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Bars.KP50.Report.Base;
using Bars.KP50.Utils;
using Newtonsoft.Json;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;
using Bars.KP50.Report.Tula.Properties;

namespace Bars.KP50.Report.Tula.Reports
{
	public class Report715101 : BaseSqlReport
	{
		public override string Name
		{
			get { return "71.5.10.1 Сальдовая ведомость по услугам с расшифровкой"; }
		}

		public override string Description
		{
			get { return "Сальдовая ведомость"; }
		}

		public override IList<ReportGroup> ReportGroups
		{
			get
			{
				var result = new List<ReportGroup> {ReportGroup.Reports};
				return result;
			}
		}

		public override bool IsPreview
		{
			get { return false; }
		}

		protected override byte[] Template
		{
			get { return Resources.Report_71_5_10_1; }
		}

		public override IList<ReportKind> ReportKinds
		{
            get { return new List<ReportKind> { ReportKind.Base, ReportKind.ListLC }; }
		}


		/// <summary>Расчетный месяц</summary>
		protected int MonthS { get; set; }

		/// <summary>Расчетный год</summary>
		protected int YearS { get; set; }


		/// <summary>Расчетный месяц</summary>
		protected int MonthPo { get; set; }

		/// <summary>Расчетный год</summary>
		protected int YearPo { get; set; }

		/// <summary>Услуги</summary>
		protected List<int> Services { get; set; }


		/// <summary>Управляющие компании</summary>
		protected List<int> Areas { get; set; }

		/// <summary>Заголовок отчета</summary>
		protected string SupplierHeader { get; set; }

		/// <summary>Заголовок услуг</summary>
		protected string ServiceHeader { get; set; }

		/// <summary>Заголовок отчета</summary>
		protected string AreaHeader { get; set; }

		/// <summary>Заголовок территории</summary>
		protected string TerritoryHeader { get; set; }

		/// <summary>Поставщики, Агенты, Принципалы  </summary>
		protected BankSupplierParameterValue BankSupplier { get; set; }

		/// <summary>Тип группировки</summary>
		private int TypeGroup { get; set; }


		public override List<UserParam> GetUserParams()
		{
			var curCalcMonthYear = DBManager.GetCurMonthYear();
			return new List<UserParam>
			{
				new MonthParameter {Name = "Месяц с", Value = curCalcMonthYear != null ? curCalcMonthYear.Rows[0]["month_"] : DateTime.Today.Month },
				new YearParameter {Name = "Год с", Value = curCalcMonthYear != null ? curCalcMonthYear.Rows[0]["yearr"] : DateTime.Today.Year },
				new MonthParameter {Name = "Месяц по", Code = "Month1", Value = curCalcMonthYear != null ? curCalcMonthYear.Rows[0]["month_"] : DateTime.Today.Month },
				new YearParameter {Name = "Год по", Code = "Year1", Value = curCalcMonthYear != null ? curCalcMonthYear.Rows[0]["yearr"] : DateTime.Today.Year },
				new AreaParameter(),
				new BankSupplierParameter(),
				new ServiceParameter(),
				new ComboBoxParameter
				{
					Code = "TypeGroup",
					Name = "В разрезе",
					Value = 1,
					StoreData = new List<object>
					{
						new {Id = "1", Name = "территорий"},
						new {Id = "2", Name = "поставщиков"},
						new {Id = "3", Name = "принципалов"}
					}
				}
			};
		}

		public override DataSet GetData()
		{
			MyDataReader reader;
			string whereSupp = GetWhereSupp("a.nzp_supp");
			string whereServ = GetWhereServ("a.");
			string whereArea = GetWhereArea();
            if (ReportParams.CurrentReportKind == ReportKind.ListLC) GetSelectedKvars();

			string sql = " select point AS bank_name, bd_kernel as pref " +
						 " FROM " + ReportParams.Pref + DBManager.sKernelAliasRest + "s_point " +
						 " where nzp_wp>1 " + GetWhereWp();

			ExecRead(out reader, sql);

			while (reader.Read())
			{
				if (reader["pref"] == null) continue;
				for (int i = YearS*12 + MonthS; i < YearPo*12 + MonthPo+1; i++)
				{
					int mo = i%12;
					int ye = mo == 0 ? (i/12) - 1 : (i/12);
					if (mo == 0) mo = 12;
					string pref = reader["pref"].ToStr().Trim(),
							bankName = reader["bank_name"].ToStr().Trim();
					string baseData = pref + DBManager.sDataAliasRest;
					string sumInsaldo = ((mo == MonthS) & (ye == YearS)) ? "sum_insaldo" : "0";
					string sumOutsaldo = ((mo == MonthPo) & (ye == YearPo)) ? "sum_outsaldo" : "0";
					string tableCharge = pref + "_charge_" + (ye - 2000).ToString("00") +
										 DBManager.tableDelimiter + "charge_" +
										 mo.ToString("00");
					string tablePerekidka = pref + "_charge_" + (ye - 2000).ToString("00") + DBManager.tableDelimiter +
											"perekidka";
					string tableFromSupplier = pref + "_charge_" + (ye - 2000).ToString("00") +
										 DBManager.tableDelimiter + "from_supplier ";

                    string kvar = ReportParams.CurrentReportKind == ReportKind.ListLC ? " selected_kvars " : baseData + "kvar ";
                    string dom = baseData + "dom ";
                    string ulica = baseData + "s_ulica ";

					if (TempTableInWebCashe(tableCharge))
					{
						sql = " INSERT INTO t_svod(pref, prefym, nzp_kvar, nzp_serv, nzp_supp, sum_insaldo_k, sum_insaldo_d, " +
													" sum_insaldo, rsum_tarif, reval, money_to, money_from, money_del, " +
														" sum_outsaldo_k, sum_outsaldo_d, sum_outsaldo)" +
							  " SELECT '" + bankName + "' AS pref, '" + 
											pref + ye + mo + "' as prefym, " +
										" a.nzp_kvar, " +
										" a.nzp_serv, " +
										" a.nzp_supp, " +
										" SUM(CASE WHEN sum_insaldo < 0 THEN " + sumInsaldo + " ELSE 0 END) AS sum_insaldo_k," +
										" SUM(CASE WHEN sum_insaldo < 0 THEN 0 ELSE " + sumInsaldo + " END) AS sum_insaldo_d," +
										" SUM(" + sumInsaldo + ") AS sum_insaldo," +
										" SUM(sum_tarif) AS rsum_tarif," +
										" SUM(reval) AS reval," +
										" SUM(money_to) AS money_to," +
										" SUM(money_from) AS money_from, " +  
										" SUM(money_del) AS money_del, " +
										" SUM(CASE WHEN sum_outsaldo < 0 THEN " + sumOutsaldo + " ELSE 0 END) AS sum_outsaldo_k," +
										" SUM(CASE WHEN sum_outsaldo < 0 THEN 0 ELSE " + sumOutsaldo + " END) AS sum_outsaldo_d," +
										" SUM(" + sumOutsaldo + ") AS sum_outsaldo" +
							  " FROM " + tableCharge + " a, " +
                                         kvar + " k INNER JOIN " + dom + " d ON k.nzp_dom = d.nzp_dom INNER JOIN " + ulica + " u ON d.nzp_ul = u.nzp_ul " +
							  " WHERE a.nzp_kvar = k.nzp_kvar " +
							   " AND a.nzp_serv > 1 " +
							   " AND dat_charge IS NULL " +
							  whereArea + whereServ + whereSupp +
							  " GROUP BY 1,2,3,4,5 ";
						ExecSQL(sql);

						ExecSQL(DBManager.sUpdStat + " t_svod ");

						sql = " UPDATE t_svod " +
							  " SET real_charge = ( SELECT SUM(sum_rcl)" +
												  " FROM " + tablePerekidka + " p " + 
												  " WHERE p.type_rcl not in (100,20) " +
													" AND p.nzp_kvar > 0 " + 
													" AND p.nzp_kvar = t_svod.nzp_kvar" +
													" AND p.nzp_serv = t_svod.nzp_serv " +
                                                    " AND p.nzp_supp = t_svod.nzp_supp " +
						      "" + GetWhereSupp("p.nzp_supp") +
													" AND p.month_ = " + mo + ") " +
							  " WHERE prefym = '" + pref + ye + mo + "' " ;
						ExecSQL(sql);

						sql = " UPDATE t_svod " +
							  " SET real_insaldo = ( SELECT SUM(sum_rcl)" +
												  " FROM " + tablePerekidka + " p " +
												  " WHERE p.type_rcl in ( 100,20) " +  
													" AND p.nzp_kvar = t_svod.nzp_kvar" +
                                                    " AND p.nzp_supp = t_svod.nzp_supp " +
													" AND p.nzp_serv = t_svod.nzp_serv " + GetWhereSupp("p.nzp_supp") +
													" AND p.month_ = " + mo + ") " +
							  " WHERE prefym = '" + pref + ye + mo + "' ";
						ExecSQL(sql);

						sql = " INSERT INTO t_svod(pref, nzp_kvar, nzp_serv, nzp_supp, money_from, money_supp, sum_insaldo)" +
							  " SELECT '" + bankName + "' AS pref, nzp_kvar, nzp_serv , nzp_supp, -SUM(sum_prih), SUM(sum_prih), SUM(0) " +
                              " FROM " + tableFromSupplier + " a, " + kvar + " k " + 
							  " WHERE a.num_ls = k.num_ls  " +
						        " AND a.kod_sum in (49, 50, 35) " + whereArea + whereServ + whereSupp +//???
								" AND dat_uchet >= '01." + mo + "." + ye + "' " +
								" AND dat_uchet <= '" + DateTime.DaysInMonth(ye, mo) + "." + mo + "." + ye + "'" +
							  " GROUP BY 1,2,3,4 ";
						ExecSQL(sql);
					}
				}

			}

			reader.Close();

			string centralKernel = ReportParams.Pref + DBManager.sKernelAliasRest;

			sql = " SELECT service, " +
						 " SUM(sum_insaldo_k) AS sum_insaldo_k," +
						 " SUM(sum_insaldo_d) AS sum_insaldo_d," +
						 " SUM(sum_insaldo) AS sum_insaldo," +
						 " SUM(sum_insaldo - money_from) AS sum_insaldo_n," +
						 " SUM(rsum_tarif) AS rsum_tarif," +
						 " SUM(reval) AS reval," +
						 " SUM(real_charge) AS real_charge," +
						 " SUM(real_insaldo) AS real_insaldo," +
						 " SUM(sum_money) AS sum_money," +
						 " SUM(money_to) AS money_to," +
						 " SUM(money_supp) AS money_supp," +
						 " SUM(money_from) AS money_from," +
						 " SUM(money_del) AS money_del," +
						 " SUM(sum_outsaldo_k) AS sum_outsaldo_k," +
						 " SUM(sum_outsaldo_d) AS sum_outsaldo_d," +
						 " SUM(sum_outsaldo) AS sum_outsaldo" +
				  " FROM t_svod a, " + centralKernel + "services s " +
				  " WHERE a.nzp_serv = s.nzp_serv" +
				  " GROUP BY service " +
				  " ORDER BY service ";
			DataTable dt = ExecSQLToTable(sql);
			dt.TableName = "Q_master";
			if (dt.Rows.Count > 65000 && ReportParams.ExportFormat == ExportFormat.Excel2007)
			{
				var dtr = dt.Rows.Cast<DataRow>().Skip(65000).ToArray();
				dtr.ForEach(dt.Rows.Remove);
			}

			string selectTypeGroup = (TypeGroup == 1
										? " TRIM(pref) AS pref "
										: (TypeGroup == 2 || TypeGroup == 3
											? " TRIM(payer) AS payer " 
											: string.Empty)),
				   fromTypeGroup = (TypeGroup != 1 
										? ", " + centralKernel + "supplier su, " +
												 centralKernel + "s_payer p "
										: string.Empty),
				   whereTypeGroup = (TypeGroup == 1 
										? string.Empty 
										: (TypeGroup == 2 
											? " AND a.nzp_supp = su.nzp_supp " +
											  " AND su.nzp_payer_supp = p.nzp_payer "
											: (TypeGroup == 3 
												? " AND a.nzp_supp = su.nzp_supp " +
												  " AND su.nzp_payer_princip = p.nzp_payer " 
												: string.Empty))),
				   groupOrder = (TypeGroup == 1 
									? " pref " 
									: (TypeGroup == 2 || TypeGroup == 3 
										? "payer" 
										: string.Empty));

            if (TempTableInWebCashe("t_sv")) ExecSQL("drop table t_sv");
		    sql = " SELECT " + selectTypeGroup + ", " +
		          " a.nzp_serv, " +
		          " SUM(sum_insaldo_k) AS sum_insaldo_k, " +
		          " SUM(sum_insaldo_d) AS sum_insaldo_d, " +
		          " SUM(sum_insaldo) AS sum_insaldo, " +
		          " SUM(sum_insaldo - money_from) AS sum_insaldo_n, " +
		          " SUM(rsum_tarif) AS rsum_tarif, " +
		          " SUM(reval) AS reval, " +
		          " SUM(real_charge) AS real_charge, " +
		          " SUM(real_insaldo) AS real_insaldo, " +
		          " SUM(sum_money) AS sum_money, " +
		          " SUM(money_to) AS money_to, " +
		          " SUM(money_supp) AS money_supp, " +
		          " SUM(money_from) AS money_from, " +
		          " SUM(money_del) AS money_del, " +
		          " SUM(sum_outsaldo_k) AS sum_outsaldo_k, " +
		          " SUM(sum_outsaldo_d) AS sum_outsaldo_d, " +
		          " SUM(sum_outsaldo) AS sum_outsaldo " +
		          " into temp t_sv" +
		          " FROM t_svod a " + fromTypeGroup +
		          " WHERE 1=1  " + whereTypeGroup +
		          " GROUP BY " + groupOrder + ", nzp_serv ";
            ExecSQL(sql);

           

            sql = " SELECT " + groupOrder + ", " +
                         " se.service, " +
                         " SUM(sum_insaldo_k) AS sum_insaldo_k, " +
                         " SUM(sum_insaldo_d) AS sum_insaldo_d, " +
                         " SUM(sum_insaldo) AS sum_insaldo, " +
                         " SUM(sum_insaldo_n) AS sum_insaldo_n, " +
                         " SUM(rsum_tarif) AS rsum_tarif, " +
                         " SUM(reval) AS reval, " +
                         " SUM(real_charge) AS real_charge, " +
                         " SUM(real_insaldo) AS real_insaldo, " +
                         " SUM(sum_money) AS sum_money, " +
                         " SUM(money_to) AS money_to, " +
                         " SUM(money_supp) AS money_supp, " +
                         " SUM(money_from) AS money_from, " +
                         " SUM(money_del) AS money_del, " +
                         " SUM(sum_outsaldo_k) AS sum_outsaldo_k, " +
                         " SUM(sum_outsaldo_d) AS sum_outsaldo_d, " +
                         " SUM(sum_outsaldo) AS sum_outsaldo " +
                  " FROM t_sv a, " + centralKernel + "services se " + 
                  " WHERE a.nzp_serv = se.nzp_serv " + 
                  " GROUP BY " + groupOrder + ", service " +
                  " ORDER BY " + groupOrder + ", service ";
            
            DataTable dt1 = ExecSQLToTable(sql);
			dt1.TableName = "Q_master1";
		

			var ds = new DataSet();
			ds.Tables.Add(dt); 
			ds.Tables.Add(dt1);

			return ds;
		}

		/// <summary>
		/// Получить условия органичения по УК
		/// </summary>
		/// <returns></returns>
		private string GetWhereArea()
		{
			string whereArea = String.Empty;
			whereArea = Areas != null ? Areas.Aggregate(whereArea, (current, nzpArea) => current + (nzpArea + ",")) : ReportParams.GetRolesCondition(Constants.role_sql_area);
			whereArea = whereArea.TrimEnd(',');
			whereArea = !String.IsNullOrEmpty(whereArea) ? " AND k.nzp_area in (" + whereArea + ")" : String.Empty;
			if (!String.IsNullOrEmpty(whereArea))
			{
				if (string.IsNullOrEmpty(AreaHeader))
				{
					AreaHeader = string.Empty;
					string sql = " SELECT area from " + ReportParams.Pref + DBManager.sDataAliasRest +
								 "s_area k  WHERE k.nzp_area > 0 " + whereArea;
					DataTable area = ExecSQLToTable(sql);
					foreach (DataRow dr in area.Rows)
					{
						AreaHeader += dr["area"].ToString().Trim() + ", ";
					}
					AreaHeader = AreaHeader.TrimEnd(',', ' ');
				}
			}
			return whereArea;
		}

		/// <summary>
		/// Получает условия ограничения по поставщику
		/// </summary>
		/// <returns></returns>
		private string GetWhereSupp(string fieldPref)
		{
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

				//Поставщик
				if (string.IsNullOrEmpty(SupplierHeader))
				{
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
			}
			return " and " + fieldPref + " in (select nzp_supp from " +
				   ReportParams.Pref + DBManager.sKernelAliasRest + "supplier " +
				   " where nzp_supp>0 " + whereSupp + ")";
		}

		/// <summary>
		/// Получить условия органичения по услугам
		/// </summary>
		/// <returns></returns>
		private string GetWhereServ(string prefTable)
		{
			string whereServ = String.Empty;
			whereServ = Services != null
				? Services.Aggregate(whereServ, (current, nzpServ) => current + (nzpServ + ","))
				: ReportParams.GetRolesCondition(Constants.role_sql_serv);
			whereServ = whereServ.TrimEnd(',');
			whereServ = !String.IsNullOrEmpty(whereServ) ? " AND " + prefTable + " nzp_serv in (" + whereServ + ")" : String.Empty;
			if (!string.IsNullOrEmpty(whereServ) && string.IsNullOrEmpty(ServiceHeader))
			{
				if (string.IsNullOrEmpty(ServiceHeader))
				{
					ServiceHeader = string.Empty;
					string sql = " SELECT service FROM " + ReportParams.Pref + DBManager.sKernelAliasRest + "services " +
								 prefTable.TrimEnd('.') + " WHERE nzp_serv > 0 " + whereServ;
					DataTable servTable = ExecSQLToTable(sql);
					foreach (DataRow row in servTable.Rows)
					{
						ServiceHeader += row["service"].ToString().Trim() + ", ";
					}
					ServiceHeader = ServiceHeader.TrimEnd(',', ' ');
				}
			}
			return whereServ;
		}

		private string GetWhereWp()
		{
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

		protected override void CreateTempTable()
		{
			const string sql = " create temp table t_svod( " +
							   " pref character(100), " +
							   " prefym character(20), " +
							   " nzp_kvar integer, " +
							   " nzp_serv integer, " +
							   " nzp_supp INTEGER, " +
							   " sum_insaldo " + DBManager.sDecimalType + "(14,2)," +
							   " sum_insaldo_k " + DBManager.sDecimalType + "(14,2)," +
							   " sum_insaldo_d " + DBManager.sDecimalType + "(14,2)," +
							   " rsum_tarif " + DBManager.sDecimalType + "(14,2)," +
							   " reval " + DBManager.sDecimalType + "(14,2)," +
							   " reval_charge " + DBManager.sDecimalType + "(14,2)," +
							   " real_charge " + DBManager.sDecimalType + "(14,2)," +
							   " real_insaldo " + DBManager.sDecimalType + "(14,2)," +
							   " sum_outsaldo " + DBManager.sDecimalType + "(14,2)," +
							   " sum_outsaldo_k " + DBManager.sDecimalType + "(14,2)," +
							   " sum_outsaldo_d " + DBManager.sDecimalType + "(14,2)," +
							   " sum_money " + DBManager.sDecimalType + "(14,2)," +
							   " money_del " + DBManager.sDecimalType + "(14,2)," +
							   " money_to " + DBManager.sDecimalType + "(14,2)," +
							   " money_supp " + DBManager.sDecimalType + "(14,2)," +
							   " money_from " + DBManager.sDecimalType + "(14,2))"+DBManager.sUnlogTempTable;
			ExecSQL(sql);

			ExecSQL(" CREATE INDEX ix_t_svod ON t_svod(nzp_kvar,nzp_serv) ");

            if (ReportParams.CurrentReportKind == ReportKind.ListLC)
            {
                const string sqls = " create temp table selected_kvars(" +
                                    " nzp_kvar integer," +
                                    " num_ls integer," +
                                    " nzp_dom integer," +
                                    " nzp_geu integer," +
                                    " nzp_area integer) " +
                                    DBManager.sUnlogTempTable;
                ExecSQL(sqls);
            }

		}

		protected override void DropTempTable()
		{
			ExecSQL("drop table t_svod");
            ExecSQL("drop table t_sv");
            if (ReportParams.CurrentReportKind == ReportKind.ListLC)
                ExecSQL(" drop table selected_kvars ", true);
		}

		protected override void PrepareReport(FastReport.Report report)
		{
			var months = new[] {"","Январь","Февраль",
				 "Март","Апрель","Май","Июнь","Июль","Август","Сентябрь",
				 "Октябрь","Ноябрь","Декабрь"};
			report.SetParameterValue("month", months[MonthS]);
			report.SetParameterValue("year", YearS);
			if ((MonthS == MonthPo) & (YearS == YearPo))
			{
				report.SetParameterValue("period_month", months[MonthS] + " " + YearS);
			}
			else
			{
				report.SetParameterValue("period_month", "период с " + months[MonthS] + " " + YearS +
														 "г. по " + months[MonthPo] + " " + YearPo);
			}

			string headerParam = !string.IsNullOrEmpty(TerritoryHeader) ? "Территория: " + TerritoryHeader + "\n" : string.Empty;
            headerParam += !string.IsNullOrEmpty(SupplierHeader) ? "Поставщики: " + (SupplierHeader.Length > 600 ? SupplierHeader.Substring(0, 600) + "..." : SupplierHeader) + "\n" : string.Empty;
			headerParam += !string.IsNullOrEmpty(AreaHeader) ? "Балансодержатель: " + AreaHeader + "\n" : string.Empty;
			headerParam += !string.IsNullOrEmpty(ServiceHeader) ? "Услуги: " + ServiceHeader : string.Empty;
			headerParam = headerParam.TrimEnd('\n');
			report.SetParameterValue("headerParam", headerParam);

			report.SetParameterValue("TypeGroup", TypeGroup);
		}

		protected override void PrepareParams()
		{
			MonthS = UserParamValues["Month"].GetValue<int>();
			YearS = UserParamValues["Year"].Value.To<int>();
			MonthPo = UserParamValues["Month1"].GetValue<int>();
			YearPo = UserParamValues["Year1"].Value.To<int>();
			
			Services = UserParamValues["Services"].Value.To<List<int>>();
			Areas = UserParamValues["Areas"].Value.To<List<int>>();

			BankSupplier = JsonConvert.DeserializeObject<BankSupplierParameterValue>(UserParamValues["BankSupplier"].Value.ToString());
			TypeGroup = UserParamValues["TypeGroup"].GetValue<int>();
		}

        /// <summary>
        /// Выборка списка квартир в картотеке
        ///  </summary>
        /// <returns></returns>
        private bool GetSelectedKvars()
        {
            if (ReportParams.CurrentReportKind == ReportKind.ListLC)
            {
                using (IDbConnection connWeb = DBManager.GetConnection(Constants.cons_Webdata))
                {
                    if (!DBManager.OpenDb(connWeb, true).result) return false;

                    string tSpls = DBManager.GetFullBaseName(connWeb) + DBManager.tableDelimiter +
                                   "t" + ReportParams.User.nzp_user + "_spls";
                    if (TempTableInWebCashe(tSpls))
                    {
                        string sql = " insert into selected_kvars (nzp_kvar, num_ls, nzp_dom, nzp_geu, nzp_area) " +
                                     " select nzp_kvar, num_ls, nzp_dom, nzp_geu, nzp_area from " + tSpls;
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
