using System;
using System.Collections.Generic;
using System.Data;
using Bars.KP50.Report.Base;
using STCLINE.KP50.DataBase;
using Bars.KP50.Report.RT.Properties;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Linq;
using STCLINE.KP50.Global;
using Bars.KP50.Utils;

namespace Bars.KP50.Report.RT.Reports
{
	class Report16104 : BaseSqlReport
	{
		public override string Name {
			get { return "16.10.4Ф Состояние поступлений"; }
		}

		public override string Description {
			get { return "10.4Ф Состояние поступлений"; }
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

		protected override byte[] Template {
			get { return Resources._10_4_F_incom_status; }
		}

		public override IList<ReportKind> ReportKinds {
			get { return new List<ReportKind> { ReportKind.Base }; }
		}

		/// <summary> Расчетный год </summary>
		private int Year { get; set; }

		/// <summary> Расчетный месяц </summary>
		private int Month { get; set; }

		/// <summary> Период - с </summary>
		protected DateTime DatS { get; set; }

		/// <summary> Период -  по </summary>
		protected DateTime DatPo { get; set; }

		/// <summary>Услуги</summary>
		protected List<int> Services { get; set; }

		/// <summary>Поставщики</summary>
		protected List<long> Suppliers { get; set; }

		/// <summary>Услуги</summary>
		protected string ServiceHeader { get; set; }

		/// <summary>Заголовок поставщиков</summary>
		protected string SupplierHeader { get; set; }

		/// <summary>Заголовок территории</summary>
		protected string TerritoryHeader { get; set; }

		/// <summary>Банки данных</summary>
		protected List<int> Banks { get; set; }


		public override List<UserParam> GetUserParams() {
			return new List<UserParam>
			{
				new PeriodParameter(DateTime.Now, DateTime.Now),
				new YearParameter{ Name = "Расчетный год: ",Value = DateTime.Now.Year },
				new MonthParameter{ Name = "Расчетный месяц: ", Value = DateTime.Now.Month },
				new SupplierAndBankParameter(),
				new ServiceParameter()
			};
		}

		protected override void PrepareReport(FastReport.Report report) {
			var month = new[] { "", "Январь", "Февраль", "Март", "Апрель", "Май", "Июнь", "Июль", "Август", "Сентябрь", "Октябрь", "Ноябрь", "Декабрь" };

			report.SetParameterValue("year", Year);
			report.SetParameterValue("month", month[Month]);
			report.SetParameterValue("time", DateTime.Now.ToShortTimeString());
			report.SetParameterValue("date", DateTime.Now.ToShortDateString());
			report.SetParameterValue("dats", DatS.ToShortDateString());
			report.SetParameterValue("datpo", DatPo.ToShortDateString());
			string headerParam = !string.IsNullOrEmpty(TerritoryHeader) ? "Территория: " + TerritoryHeader + "\n" : string.Empty;
			headerParam += !string.IsNullOrEmpty(SupplierHeader) ? "Поставщики: " + SupplierHeader + "\n" : string.Empty;
			headerParam += !string.IsNullOrEmpty(ServiceHeader) ? "Услуги: " + ServiceHeader : string.Empty;
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

			Month = UserParamValues["Month"].GetValue<int>();
			Year = UserParamValues["Year"].GetValue<int>();

			Services = UserParamValues["Services"].GetValue<List<int>>();
			var values = JsonConvert.DeserializeObject<Dictionary<string, object>>(UserParamValues["SupplierAndBank"].GetValue<string>());
			Suppliers = values["Streets"] != null
				? values["Streets"].To<JArray>().Select(x => x.Value<long>()).ToList()
				: null;
			Banks = values["Raions"] != null
				? values["Raions"].To<JArray>().Select(x => x.Value<int>()).ToList()
				: null;
		}

		public override DataSet GetData() {
			string sql;
            IDataReader reader;
			string whereSupp = GetWhereSupp(),
					   whereServ = GetWhereServ(),
						  whereWp = GetWhereWp();

		    string prefData = ReportParams.Pref + DBManager.sDataAliasRest,
		            prefKernel = ReportParams.Pref + DBManager.sKernelAliasRest;

            if (TempTableInWebCashe("t_area_16_10_4")) ExecSQL(" DROP TABLE t_area_16_10_4 ");
            ExecSQL(" SELECT DISTINCT nzp_area FROM " + prefData + "kvar WHERE nzp_area NOT IN (350, 351, 5000) " + whereWp + " INTO TEMP t_area_16_10_4 ");

            if (TempTableInWebCashe("t_supp_16_10_4")) ExecSQL(" DROP TABLE t_supp_16_10_4 ");
            ExecSQL(" SELECT DISTINCT nzp_payer FROM " + prefKernel + "s_payer WHERE nzp_payer > 0 " + whereSupp + " INTO TEMP t_supp_16_10_4 ");

            string fnDistribYY = ReportParams.Pref + "_fin_" + (Year - 2000).ToString("00") +
                                    DBManager.tableDelimiter + "fn_distrib_" + Month.ToString("00");
		    if (TempTableInWebCashe(fnDistribYY))
		    {
		        sql = " INSERT INTO t_report_16_10_4(nzp_area, nzp_serv, sum_rasp, sum_ud) " +
		              " SELECT (CASE WHEN nzp_area > 1108 AND nzp_area < 1112 THEN 1102 ELSE nzp_area END) AS nzp_area, " +
		                     " (CASE WHEN nzp_serv = 14 THEN 25 ELSE nzp_serv END) AS nzp_serv, " +
		                     " SUM(sum_rasp), " +
		                     " SUM(sum_ud) " +
		              " FROM " + fnDistribYY +
		              " WHERE dat_oper >= DATE('1." + Month + "." + Year + "') " +
		                " AND dat_oper <= DATE('" + DateTime.DaysInMonth(Year,Month) + "." + Month + "." + Year + "') " +
                        " AND nzp_area IN (SELECT nzp_area FROM t_area_16_10_4) " +
                        " AND nzp_payer IN (SELECT nzp_payer FROM t_supp_16_10_4) " +
                        " AND nzp_serv > 1 " + whereServ +
		              " GROUP BY 1,2 ";
		        ExecSQL(sql);
		    }

            sql = " SELECT bd_kernel as pref " +
                  " FROM " + prefKernel + "s_point " +
                  " WHERE nzp_wp > 1 " + whereWp;

            ExecRead(out reader, sql);
		    while (reader.Read())
		    {
                string pref = reader["pref"].ToStr().Trim();

                for (int i = DatS.Year * 12 + DatS.Month; i < DatPo.Year * 12 + DatPo.Month + 1; i++)
		        {
                    int year = i / 12;
                    var month = i % 12;
                    if (month == 0) year--;
                    string chargeTable = pref + "_charge_" + (year - 2000).ToString("00") + 
                                            DBManager.tableDelimiter + "charge_" + month.ToString("00");
		            string localData = pref + DBManager.sDataAliasRest;
		            if (TempTableInWebCashe(chargeTable))
		            {
                        sql = " INSERT INTO t_report_16_10_4(nzp_area, nzp_serv, sum_charge) " +
                              " SELECT nzp_area, " +
                                     " nzp_serv, " +
                                     " SUM(sum_charge) " +
                              " FROM " + chargeTable + " c INNER JOIN " + localData + "kvar k ON k.nzp_kvar = c.nzp_kvar " +
                              " WHERE nzp_serv > 1 " +
                                " AND dat_charge IS NULL " + whereServ + whereSupp +
                              " GROUP BY 1,2 ";
                        ExecSQL(sql);
		            }
		        }
		    }
            reader.Close();

			sql = " SELECT TRIM(service) AS service, " +
			             " ordering, " +
						 " SUM(sum_charge) AS sum_charge, " +
                         " SUM(CASE WHEN nzp_area < 4000 THEN sum_charge ELSE 0 END) AS sum_charge_n, " +
                         " SUM(CASE WHEN nzp_area >= 4000 THEN sum_charge ELSE 0 END) AS sum_charge_a, " +
                         " SUM(sum_rasp) AS sum_rasp, " +
                         " SUM(CASE WHEN nzp_area < 4000 THEN sum_rasp ELSE 0 END) AS sum_rasp_n, " +
                         " SUM(CASE WHEN nzp_area >= 4000 THEN sum_rasp ELSE 0 END) AS sum_rasp_a, " +
						 " SUM(sum_ud) AS sum_ud, " +
                         " SUM(CASE WHEN nzp_area < 4000 THEN sum_ud ELSE 0 END) AS sum_ud_n, " +
                         " SUM(CASE WHEN nzp_area >= 4000 THEN sum_ud ELSE 0 END) AS sum_ud_a " +
				  " FROM t_report_16_10_4 t INNER JOIN " + prefKernel + "services s ON s.nzp_serv = t.nzp_serv " +
                  " WHERE ((ordering > 0 and ordering < 40) OR (t.nzp_serv = 98)) AND t.nzp_serv NOT IN (2,21,22) " +
				  " GROUP BY 1, 2 ORDER BY 2";

			DataTable dt = ExecSQLToTable(sql);
			dt.TableName = "Q_master";

			var ds = new DataSet();
			ds.Tables.Add(dt);
			return ds;
		}

		/// <summary> Ограничения по Банку Данных </summary>
		private string GetWhereWp() {
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
			whereWp = !String.IsNullOrEmpty(whereWp) ? " AND nzp_wp in (" + whereWp + ") " : String.Empty;
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

		/// <summary> Получате условия органичения по услугам </summary>
		private string GetWhereServ() {
			string whereServ = String.Empty;
			whereServ = Services != null
				? Services.Aggregate(whereServ, (current, nzpServ) => current + (nzpServ + ","))
				: ReportParams.GetRolesCondition(Constants.role_sql_serv);
			whereServ = whereServ.TrimEnd(',');
			whereServ = !String.IsNullOrEmpty(whereServ) ? " AND nzp_serv in (" + whereServ + ") " : String.Empty;

			if (!string.IsNullOrEmpty(whereServ) && string.IsNullOrEmpty(ServiceHeader))
			{
				ServiceHeader = string.Empty;
				string sql = " SELECT service FROM " + ReportParams.Pref + DBManager.sKernelAliasRest + "services WHERE nzp_serv > 0 " + whereServ;
				DataTable servTable = ExecSQLToTable(sql);
				foreach (DataRow row in servTable.Rows)
				{
					ServiceHeader += row["service"].ToString().Trim() + ", ";
				}
				ServiceHeader = ServiceHeader.TrimEnd(',', ' ');
			}
			return whereServ;
		}

		/// <summary> Получает условия ограничения по поставщику </summary>
		private string GetWhereSupp() {
			string result = String.Empty;
			if (Suppliers != null)
			{
				result = Suppliers.Aggregate(result, (current, nzpSupp) => current + (nzpSupp + ","));
			}
			else
			{
				result = ReportParams.GetRolesCondition(Constants.role_sql_supp);
			}
			result = result.TrimEnd(',');


			if (!String.IsNullOrEmpty(result))
			{
				result = " AND nzp_supp in (" + result + ") ";

				//Поставщики
				SupplierHeader = String.Empty;
				var sql = " SELECT name_supp from " +
						  ReportParams.Pref + DBManager.sKernelAliasRest + "supplier " +
						  " WHERE nzp_supp > 0 " + result;
				DataTable supp = ExecSQLToTable(sql);
				foreach (DataRow dr in supp.Rows)
				{
					SupplierHeader += dr["name_supp"].ToString().Trim() + ",";
				}
				SupplierHeader = SupplierHeader.TrimEnd(',');
			}
			return result;
		}

		protected override void CreateTempTable() {
			const string sql = " CREATE TEMP TABLE t_report_16_10_4(" +
							   " nzp_area INTEGER, " +
							   " nzp_serv INTEGER, " +
							   " sum_charge DECIMAL(14,2) DEFAULT 0, " +
							   " sum_charge_n DECIMAL(14,2) DEFAULT 0, " +
							   " sum_charge_a DECIMAL(14,2) DEFAULT 0, " +
							   " sum_rasp DECIMAL(14,2), " +
							   " sum_rasp_n DECIMAL(14,2), " +
							   " sum_rasp_a DECIMAL(14,2), " +
							   " sum_ud DECIMAL(14,2), " +
							   " sum_ud_n DECIMAL(14,2), " +
							   " sum_ud_a DECIMAL(14,2)) " + DBManager.sUnlogTempTable;
			ExecSQL(sql);
		}

		protected override void DropTempTable() {
			ExecSQL(" DROP TABLE t_report_16_10_4 ");
		}

	}
}
