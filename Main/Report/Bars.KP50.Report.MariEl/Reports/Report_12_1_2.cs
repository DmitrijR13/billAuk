using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Bars.KP50.Report.Base;
//using Bars.KP50.Report.MariEl.Properties;
using Bars.KP50.Report.MariEl.Properties;
using Bars.KP50.Utils;
using Newtonsoft.Json;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;

namespace Bars.KP50.Report.MariEl.Reports
{
	class Report120102 : BaseSqlReport
	{

		public override string Name {
            get { return "12.1.2 Отчет объемов по услугам и тарифам в разрезе домов"; }
		}

		public override string Description {
            get { return "1.2 	Отчет объемов по услугам и тарифам в разрезе домов"; }
		}

		public override IList<ReportGroup> ReportGroups {
            get
            {
                var result = new List<ReportGroup> { ReportGroup.Finans };
                return result;
            }
		}

		public override bool IsPreview {
			get { return false; }
		}

		public override IList<ReportKind> ReportKinds {
			get { return new List<ReportKind> { ReportKind.Base}; }
		}

		protected override byte[] Template {
			get { return Resources.Report_12_1_2; }
		}
		/// <summary>Заголовок отчета</summary>
		protected string SupplierHeader { get; set; }

		/// <summary>Заголовок территории</summary>
		protected string TerritoryHeader { get; set; }

		/// <summary>Поставщики, Агенты, Принципалы  </summary>
		protected BankSupplierParameterValue BankSupplier { get; set; }

        /// <summary>Список префиксов банков в БД</summary>  
        private List<string> PrefBanks { get; set; }
        /// <summary> с расчетного года </summary>

        /// <summary>Заголовок услуг</summary>
        protected string ServiceHeader { get; set; }
        /// <summary>Услуги</summary>
        protected List<int> Services { get; set; }

        private int YearS { get; set; }
        /// <summary> с расчетного месяца </summary>
        private int MonthS { get; set; }
        /// <summary> по расчетный год </summary>

		public override List<UserParam> GetUserParams() {
			var curCalcMonthYear = DBManager.GetCurMonthYear();
			return new List<UserParam>
			{
				new MonthParameter {Name = "Месяц с", Value = curCalcMonthYear != null ? curCalcMonthYear.Rows[0]["month_"] : DateTime.Today.Month },
                new YearParameter {Name = "Год с", Value = curCalcMonthYear != null ? curCalcMonthYear.Rows[0]["yearr"] : DateTime.Today.Year },
				new BankSupplierParameter(),
                new ServiceParameter()
			};
		}

		public override DataSet GetData() {
			#region выборка в temp таблицы
                                                                                                
		    GetwhereWp();
            string whereSupp = GetWhereSupp("c.nzp_supp");
            string whereServ = GetWhereServ("c.");
			string sql ;

		    foreach (var pref in PrefBanks)
		    {

		        string charge = pref + "_charge_" + (YearS - 2000).ToString("00") +
		                        DBManager.tableDelimiter + "charge_" + MonthS.ToString("00");

		        sql =
		            " INSERT INTO t_svod_mini (nzp_kvar, nzp_serv, nzp_supp,  is_device, tarif, sum_tarif, rashod, reval, real_charge, sum_money, c_reval) " +
		            " SELECT nzp_kvar, nzp_serv, nzp_supp, is_device%2, tarif, rsum_tarif, c_calc, reval, real_charge, sum_money, c_reval  " +
		            " FROM " + charge + " c " +
		            " WHERE nzp_kvar>0 and nzp_serv>1 and dat_charge is null  " +
		            "  " + whereSupp + whereServ;
		        ExecSQL(sql);

                string calc = pref + "_charge_" + (YearS - 2000).ToString("00") +
		                      DBManager.tableDelimiter + "calc_gku_" + MonthS.ToString("00");

		        sql = " INSERT INTO t_svod_mini (nzp_kvar, nzp_serv, nzp_supp,  is_device, tarif, normativ) " +
		              " SELECT nzp_kvar, nzp_serv, nzp_supp, is_device%2, tarif, rash_norm_one  " +
		              " FROM " + calc + " c " +
		              " WHERE nzp_kvar>0 and nzp_serv>1 and dat_charge is null and stek=3 and rashod>0 " +
		              "  " + whereSupp + whereServ;
		        ExecSQL(sql);

		        ExecSQL(DBManager.sUpdStat + " t_svod_mini");

		        sql =
		            " INSERT INTO t_svod_kvar (nzp_kvar, nzp_serv, nzp_supp, tarif, normativ,is_device, sum_tarif, rashod, reval, real_charge, sum_money) " +
		            " SELECT nzp_kvar, nzp_serv, nzp_supp,  MAX(tarif), MAX(normativ), MAX(is_device), SUM(sum_tarif), SUM(rashod), SUM(reval), SUM(real_charge), SUM(sum_money)  " +
		            " FROM t_svod_mini " +
		            " GROUP BY  1,2,3 ";
		        ExecSQL(sql);

                sql = " UPDATE t_svod_kvar SET normativ = 1 " +
		              " WHERE is_device>0 ";
                ExecSQL(sql);

                string gilXX = pref + "_charge_" + (YearS - 2000).ToString("00") +
                      DBManager.tableDelimiter + "gil_" + MonthS.ToString("00");

		        sql = " UPDATE t_svod_kvar SET count_gil  =( SELECT cnt2 + ROUND(val3) + ROUND(val5)  " +
                      " FROM " + gilXX + " g " +
                      " WHERE g.nzp_kvar=t_svod_kvar.nzp_kvar and dat_charge is null and stek=3 LIMIT 1 ) ";
		        ExecSQL(sql);   

                string prm1 = pref + DBManager.sDataAliasRest + "prm_1" ;
		        string datS = "1." + MonthS + "." + YearS,
                    datPo = DateTime.DaysInMonth(YearS, MonthS) + "." + MonthS + "." + YearS;
		        sql = " UPDATE t_svod_kvar SET ob_s  =( SELECT max(val_prm)" + DBManager.sConvToNum + " " +
		              " FROM " + prm1 + " p " +
		              " WHERE p.nzp=t_svod_kvar.nzp_kvar and is_actual<>100 and nzp_prm=4 " +
		              " and p.dat_s <= '" + datPo + "' " +
		              " and p.dat_po >= '" + datS + "' " +
		              ") ";
		        ExecSQL(sql);

		        sql = " UPDATE t_svod_kvar SET nzp_serv = (CASE WHEN nzp_serv=210 THEN 516 " +
		              " WHEN nzp_serv=515 THEN 25 " +
		              " WHEN nzp_serv=510 THEN 6 " +
		              " WHEN nzp_serv=511 THEN 7 " +
		              " WHEN nzp_serv=512 THEN 8 " +
		              " WHEN nzp_serv=513 OR nzp_serv=1000513 THEN 9 " +
		              " WHEN nzp_serv=517 THEN 10 " +
		              " WHEN nzp_serv=514 OR nzp_serv=1000514 THEN 14 " +
		              " ELSE nzp_serv END)";
                ExecSQL(sql);

                sql = " INSERT INTO t_svod ( nzp_dom, nzp_serv, nzp_supp, tarif, normativ, count_ls, count_gil, ob_s, sum_nach, reval, real_charge, c_reval ,  " +
                      "  rashod, sum_money) " +
                      " SELECT  k.nzp_dom, nzp_serv, nzp_supp,  tarif,  normativ, count(distinct k.nzp_kvar), SUM(count_gil), SUM(ob_s), SUM(sum_tarif)," +
                      " SUM(reval), SUM(real_charge), SUM(case when tarif>0 then round(reval / tarif, 4) else 0 end), SUM(rashod),  SUM(sum_money)" +
                      " FROM t_svod_kvar t,  " + pref + DBManager.sDataAliasRest + " kvar k " +
                      " WHERE k.nzp_kvar=t.nzp_kvar" + 
                      " GROUP BY 1,2,3,4,5 ";
                ExecSQL(sql);
                                  
 
                ExecSQL(" TRUNCATE t_svod_mini ");
                ExecSQL(" TRUNCATE t_svod_kvar ");
		    }
			#endregion



            string centralData = ReportParams.Pref + DBManager.sDataAliasRest;
		    sql = " SELECT (CASE WHEN rajon='-' THEN town ELSE TRIM(town)||','||TRIM(rajon) END) as rajon," +
		          " ulica, ulicareg, ndom || (CASE WHEN nkor ='-' THEN '' ELSE nkor END) AS ndom," +
		          " name_supp, d.nzp_dom, serv.service as service1, tarif,  normativ, count_ls, ob_s as s_ob, count_gil, " +
		          " rashod as calc," +
		          " c_reval as calc_reval," +
                  " sum_nach, (reval+real_charge) as sum_reval, sum_money    " +
		          " FROM t_svod t," + ReportParams.Pref + DBManager.sKernelAliasRest + " services serv," +
                  ReportParams.Pref + DBManager.sKernelAliasRest + " supplier su," +
		          centralData + "dom d, " +
		          centralData + "s_ulica s, " +
		          centralData + "s_rajon sr, " +
		          centralData + "s_town st " +
		          " WHERE t.nzp_serv=serv.nzp_serv " +
                  " AND t.nzp_supp= su.nzp_supp" +
                  " AND t.nzp_dom = d.nzp_dom" +
                  " AND d.nzp_ul = s.nzp_ul" +
                  " AND s.nzp_raj = sr.nzp_raj" +
                  " AND sr.nzp_town = st.nzp_town " +
		          " ORDER BY name_supp, rajon, ulica, ulicareg, idom, ndom, service, tarif, normativ  ";
            DataTable dt = ExecSQLToTable(sql);
            dt.TableName = "Q_master";
			var ds = new DataSet();
			ds.Tables.Add(dt);
			return ds;
		}

		/// <summary>
		/// Получает условия ограничения по поставщику
		/// </summary>
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


        private string GetwhereWp()
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
            string whereWpsql = !String.IsNullOrEmpty(whereWp) ? " AND nzp_wp in (" + whereWp + ")" : String.Empty;
            PrefBanks = new List<string>();
            if (!string.IsNullOrEmpty(whereWpsql))
            {
                TerritoryHeader = String.Empty;
                string sql = " SELECT point,bd_kernel FROM " + ReportParams.Pref + DBManager.sKernelAliasRest + "s_point WHERE nzp_wp > 0 " + whereWpsql;
                DataTable terrTable = ExecSQLToTable(sql);
                foreach (DataRow row in terrTable.Rows)
                {
                    TerritoryHeader += row["point"].ToString().Trim() + ", ";
                    PrefBanks.Add(row["bd_kernel"].ToString().Trim());

                }
                TerritoryHeader = TerritoryHeader.TrimEnd(',', ' ');
            }
            else
            {
                string sql = " SELECT bd_kernel FROM " + ReportParams.Pref + DBManager.sKernelAliasRest + "s_point WHERE nzp_wp > 0 and flag=2";
                DataTable terrTable = ExecSQLToTable(sql);
                foreach (DataRow row in terrTable.Rows)
                {
                    PrefBanks.Add(row["bd_kernel"].ToString().Trim());
                }
            }
            string whereWpRes = !String.IsNullOrEmpty(whereWp) ? "AND pl.num_ls in (SELECT num_ls FROM " + ReportParams.Pref + DBManager.sKernelAliasRest + "s_point s,"
                       + ReportParams.Pref + DBManager.sDataAliasRest + "kvar kv " +
                       "where kv.nzp_wp=s.nzp_wp AND s.nzp_wp in ( " + whereWp + ") )" : String.Empty;
            return whereWpRes;
        }

		protected override void PrepareReport(FastReport.Report report) {
            var months = new[] {"","Январь","Февраль",
                 "Март","Апрель","Май","Июнь","Июль","Август","Сентябрь",
                 "Октябрь","Ноябрь","Декабрь"};

            report.SetParameterValue("period_month", months[MonthS] + " " + YearS); 

			report.SetParameterValue("DATE", DateTime.Now.ToShortDateString());
			report.SetParameterValue("TIME", DateTime.Now.ToLongTimeString());

			string headerParam = !string.IsNullOrEmpty(TerritoryHeader) ? "Территория: " + TerritoryHeader + "\n" : string.Empty;
			headerParam += !string.IsNullOrEmpty(SupplierHeader) ? "Поставщики: " + SupplierHeader : string.Empty;
            headerParam += !string.IsNullOrEmpty(ServiceHeader) ? "Услуги: " + ServiceHeader : string.Empty;
			headerParam = headerParam.TrimEnd('\n');
			report.SetParameterValue("headerParam", headerParam);
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



		protected override void PrepareParams() {
            MonthS = UserParamValues["Month"].GetValue<int>();
            YearS = UserParamValues["Year"].Value.To<int>();
			BankSupplier = JsonConvert.DeserializeObject<BankSupplierParameterValue>(UserParamValues["BankSupplier"].Value.ToString());
           	Services = UserParamValues["Services"].Value.To<List<int>>();  
		}

		
        protected override void CreateTempTable()
        {    
            string sql;
            sql = " CREATE TEMP TABLE t_svod_mini ( " +
                  " nzp_serv integer, " +
                  " nzp_kvar integer, " +
                  " nzp_supp integer, " +
                  " is_device integer default 0, " +
                  " count_gil integer, " +
                  " tarif " + DBManager.sDecimalType + "(14,4), " +
                  " normativ " + DBManager.sDecimalType + "(14,4), " +
                  " ob_s " + DBManager.sDecimalType + "(14,2), " +
                  " rashod " + DBManager.sDecimalType + "(20,7), " +
                  " c_reval " + DBManager.sDecimalType + "(20,7), " +
                  " sum_tarif " + DBManager.sDecimalType + "(14,2), " +
                  " sum_nach " + DBManager.sDecimalType + "(14,2), " +
                  " reval " + DBManager.sDecimalType + "(14,2), " +
                  " real_charge " + DBManager.sDecimalType + "(14,2), " +
                  " sum_money " + DBManager.sDecimalType + "(14,2) " +
                  " ) " + DBManager.sUnlogTempTable;
            ExecSQL(sql);
            ExecSQL(" CREATE INDEX ix_t_svod_mini_1 on t_svod_mini (nzp_kvar) ");
            ExecSQL(" CREATE INDEX ix_t_svod_mini_2 on t_svod_mini (nzp_serv, nzp_supp) ");

            sql = " CREATE TEMP TABLE t_svod_kvar ( " +
                  " nzp_serv integer, " +
                  " nzp_kvar integer, " +
                  " nzp_supp integer, " +
                  " is_device integer default 0, " +
                  " count_gil integer, " +
                  " tarif " + DBManager.sDecimalType + "(14,4), " +
                  " normativ " + DBManager.sDecimalType + "(14,4), " +
                  " ob_s " + DBManager.sDecimalType + "(14,2), " +
                  " rashod " + DBManager.sDecimalType + "(20,7), " +
                  " c_reval " + DBManager.sDecimalType + "(20,7), " +
                  " sum_tarif " + DBManager.sDecimalType + "(14,2), " +
                  " sum_nach " + DBManager.sDecimalType + "(14,2), " +
                  " reval " + DBManager.sDecimalType + "(14,2), " +
                  " real_charge " + DBManager.sDecimalType + "(14,2), " +
                  " sum_money " + DBManager.sDecimalType + "(14,2) " +
                  " ) " + DBManager.sUnlogTempTable;
            ExecSQL(sql);
            ExecSQL(" CREATE INDEX ix_t_svod_kvar_1 on t_svod_kvar (nzp_kvar) ");
            ExecSQL(" CREATE INDEX ix_t_svod_kvar_2 on t_svod_kvar (nzp_serv, nzp_supp) ");


            sql = " CREATE TEMP TABLE t_svod ( " +
                  " nzp_dom integer, " +
                  " nzp_serv integer, " +
                  " nzp_supp integer, " +
                  " tarif " + DBManager.sDecimalType + "(14,4), " +
                  " normativ " + DBManager.sDecimalType + "(14,4), " +
                  " count_gil integer, " +
                  " count_gil_ipu integer, " +
                  " count_gil_norm integer, " +
                  " count_ls integer, " +
                  " is_device integer default 0, " +
                  " ob_s " + DBManager.sDecimalType + "(14,2), " +
                  " rashod " + DBManager.sDecimalType + "(20,7), " +
                  " calc_norm " + DBManager.sDecimalType + "(20,7), " +
                  " calc_ipu " + DBManager.sDecimalType + "(20,7), " +
                  " c_reval " + DBManager.sDecimalType + "(20,7), " +
                  " sum_nach " + DBManager.sDecimalType + "(14,2), " +
                  " sum_odn " + DBManager.sDecimalType + "(14,2), " +
                  " reval " + DBManager.sDecimalType + "(14,2), " +
                  " real_charge " + DBManager.sDecimalType + "(14,2), " +
                  " sum_money " + DBManager.sDecimalType + "(14,2) " +
                  " ) " + DBManager.sUnlogTempTable;
            ExecSQL(sql);
            ExecSQL(" CREATE INDEX ix_t_svod_1 on t_svod (nzp_dom) ");
            ExecSQL(" CREATE INDEX ix_t_svod_2 on t_svod (nzp_serv, nzp_supp) ");

		}

		protected override void DropTempTable() {
            ExecSQL(" DROP TABLE t_svod ");
            ExecSQL(" DROP TABLE t_svod_kvar ");
            ExecSQL(" DROP TABLE t_svod_mini ");
		}
	}
}
