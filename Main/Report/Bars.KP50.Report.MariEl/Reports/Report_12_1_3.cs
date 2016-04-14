using System;
using System.CodeDom;
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
    class Report120103 : BaseSqlReportCounterFlow
	{

		public override string Name {
            get { return "12.1.3 Отчет ИПУ по внесению показаний"; }
		}

		public override string Description {
            get { return "1.3.	Отчет ИПУ по внесению показаний"; }
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
			get { return Resources.Report_12_1_3; }
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
        protected int aService { get; set; }

        private int Year { get; set; }
        /// <summary> с расчетного месяца </summary>
        private int Month { get; set; }
        /// <summary> по расчетный год </summary>

		public override List<UserParam> GetUserParams() {
			var curCalcMonthYear = DBManager.GetCurMonthYear();
			return new List<UserParam>
			{
				new MonthParameter {Name = "Месяц", Value = curCalcMonthYear != null ? curCalcMonthYear.Rows[0]["month_"] : DateTime.Today.Month },
                new YearParameter {Name = "Год", Value = curCalcMonthYear != null ? curCalcMonthYear.Rows[0]["yearr"] : DateTime.Today.Year },
				new BankSupplierParameter(),
                new ServiceParameter(false,false){ Require =true}
			};
		}

		public override DataSet GetData() {
			#region выборка в temp таблицы

		    GetNameServ();
            GetwhereWp();
            string whereSupp = GetWhereSupp("c.nzp_supp");
			string sql ;

		    foreach (var pref in PrefBanks)
		    {
                string prefData = pref + DBManager.sDataAliasRest;
                string prefKernel= pref + DBManager.sKernelAliasRest;


		        string charge = pref + "_charge_" + (Year - 2000).ToString("00") +
		                        DBManager.tableDelimiter + "charge_" + Month.ToString("00");
                #region квартира
                sql = " INSERT INTO t_svod_kvar (nzp_kvar, nzp_supp, sum_nach) " +
                      " SELECT nzp_kvar,  nzp_supp, rsum_tarif  " +
                      " FROM " + charge + " c " +
                      " WHERE dat_charge is null AND nzp_kvar>0 AND is_device>0 " +
                      " AND nzp_serv=" + aService +
                      whereSupp;
                ExecSQL(sql);
                                 
                string gilXX = pref + "_charge_" + (Year - 2000).ToString("00") +
                      DBManager.tableDelimiter + "gil_" + Month.ToString("00");

                sql = " UPDATE t_svod_kvar SET count_gil  =( SELECT cnt2 + ROUND(val3) + ROUND(val5)  " +
                      " FROM " + gilXX + " g " +
                      " WHERE g.nzp_kvar=t_svod_kvar.nzp_kvar and dat_charge is null and stek=3 LIMIT 1 ) ";
                ExecSQL(sql);
                ExecSQL(DBManager.sUpdStat + " t_svod_kvar ");

		        string calcXX = pref + "_charge_" + (Year - 2000).ToString("00") +
		                       DBManager.tableDelimiter + "calc_gku_" + Month.ToString("00");
		        sql = " UPDATE t_svod_kvar SET is_device  =( SELECT is_device  " +
		              " FROM " + calcXX + " c " +
		              " WHERE c.nzp_kvar=t_svod_kvar.nzp_kvar and dat_charge is null and stek=3 LIMIT 1 ) ";
		        ExecSQL(sql);
                #endregion
                
                sql = " CREATE TEMP TABLE t_svod_mini ( " +
		              " nzp_serv integer, " +
		              " nzp_kvar integer, " +
		              " nzp_counter integer, " +
                      " num_cnt char(40), " +
		              " pr integer, " +
		              " mmnog " + DBManager.sDecimalType + "(8,4)," +
		              " val_cnt " + DBManager.sDecimalType + "(20,7), " +
		              " val_cnt_pred " + DBManager.sDecimalType + "(20,7), " +
                      " rashod " + DBManager.sDecimalType + "(20,7), " +
		              " ngp_cnt  " + DBManager.sDecimalType + "(14,4) default 0, " +
		              " cnt_stage integer," +
		              " ngp_lift " + DBManager.sDecimalType + "(14,4), " +
		              " dat_uchet DATE, " +
		              " dat_uchet_pred DATE " +
		              " ) " + DBManager.sUnlogTempTable;
                ExecSQL(sql);
                ExecSQL(" CREATE INDEX ix_t_svod_mini_1 on t_svod_mini (nzp_kvar) ");
                ExecSQL(" CREATE INDEX ix_t_svod_mini_3 on t_svod_mini (nzp_counter) ");

		        sql =
                    " INSERT INTO t_svod_mini (nzp_kvar, nzp_serv, nzp_counter,  cnt_stage, mmnog, num_cnt ) " +
                    " SELECT nzp, cs.nzp_serv, nzp_counter, t.cnt_stage, t.mmnog, num_cnt  " +
		            " FROM " +
		            prefData + "counters_spis cs, " +
		            prefKernel + "s_counts cc, " +
		            prefKernel + "s_counttypes t, " +
		            prefKernel + "s_measure m " +
		            " WHERE nzp_type=3 AND is_actual<>100 " +
		            " AND cs.nzp_serv = cc.nzp_serv " +
		            " AND cs.nzp_cnttype = t.nzp_cnttype " +
		            " AND cc.nzp_measure = m.nzp_measure " +
                    " AND (dat_close is null OR dat_close >= '" + new DateTime(Year, Month, DateTime.DaysInMonth(Year, Month)).ToShortDateString() + "')"+
		            " AND cs.nzp_serv=" + aService;
		        ExecSQL(sql);
                ExecSQL(DBManager.sUpdStat + " t_svod_mini ");

                int YearPred = Year;
                int MonthPred = Month - 1;
                if (MonthPred == 0)
                {
                    MonthPred = 12;
                    YearPred = YearPred - 1;
                }

                var d0 = new DateTime(YearPred, MonthPred, 2) ;
                var d1 = new DateTime(Year, Month, 1) ;

                GetCounterFlow("t_svod_mini", 3, pref, d0, d1 );

		        sql = " INSERT INTO t_svod (nzp_kvar, nzp_supp, sum_nach, count_gil, is_device,  rashod ) " +
                      " SELECT tm.nzp_kvar, nzp_supp, sum_nach, count_gil, is_device, sum(tm.rashod) " +
                      " FROM  t_svod_kvar tk, t_svod_mini tm " +
		              " WHERE tm.nzp_kvar=tk.nzp_kvar " +
		              " GROUP BY 1,2,3,4,5" ; 
		        ExecSQL(sql);  

                //sql = " UPDATE t_svod SET pr = 1" +
                //      " WHERE t_svod.nzp_kvar in (SELECT tm.nzp_kvar " +
                //      " FROM t_svod_mini tm " +
                //      " WHERE dat_uchet BETWEEN '" + d0.ToShortDateString() + "' AND '" + d1.ToShortDateString() + " ')" +
                //      " AND pr is null";
                //ExecSQL(sql);     

                sql = " UPDATE t_svod SET pr = 0 " +
                      " WHERE is_device=1";
                ExecSQL(sql);

                sql = " UPDATE t_svod SET pr = 1" +
                      " WHERE pr is null";
                ExecSQL(sql);

		        sql = " SELECT nzp_kvar, nzp_counter " +
		              " FROM t_svod_mini ";

               
                MyDataReader reader;
                ExecRead(out reader, sql);
                while (reader.Read())
		        {
                   var nzp_counter= reader["nzp_counter"].ToString();
                   var nzp_kvar = reader["nzp_kvar"].ToString();

		            sql =
		                " UPDATE t_svod SET comment = comment || " +
		                "                   (SELECT  num_cnt||' '||rtrim(to_char(val_cnt, 'FM999999999990D999999'), ',')||'/'|| rtrim(to_char(val_cnt_pred, 'FM999999999990D999999'), ',') " +
                        "                    || '('||dat_uchet||')' || ';'" +
		                " FROM t_svod_mini tm " +
		                " WHERE tm.nzp_kvar=" + nzp_kvar + " AND tm.nzp_counter=" + nzp_counter + " )" +
                        " WHERE t_svod.pr=0 AND nzp_kvar=" + nzp_kvar;
		            ExecSQL(sql);

		            sql =
                        " UPDATE t_svod SET comment = comment || (SELECT  num_cnt||' '||rtrim(to_char(val_cnt_pred, 'FM999999999990D999999'), ',')||'('||dat_uchet_pred||');'" +
		                " FROM t_svod_mini tm " +
		                " WHERE tm.nzp_kvar=" + nzp_kvar + " AND tm.nzp_counter=" + nzp_counter + " )" +
                        " WHERE t_svod.pr=1 AND nzp_kvar=" + nzp_kvar;
		            ExecSQL(sql);		            
		        }     

                ExecSQL(" DROP TABLE t_svod_mini ");
                ExecSQL(" TRUNCATE t_svod_kvar ");
		    }

			#endregion
            ExecSQL(DBManager.sUpdStat + " t_svod ");
            string centralData = ReportParams.Pref + DBManager.sDataAliasRest;
		    sql = " SELECT (CASE WHEN rajon='-' THEN town ELSE TRIM(town)||','||TRIM(rajon) END) as rajon," +
                  " ulica, ulicareg, ndom || (CASE WHEN nkor ='-' THEN '' ELSE nkor END) AS ndom, " +
                  " (CASE WHEN (nkvar_n<>'0' and nkvar_n<>'-') THEN 'кв.'||nkvar||' ком.'||nkvar_n ELSE 'кв.'||nkvar END) AS nkvar, ikvar, " +
		          " name_supp, pr as predost,  count_gil, sum_nach, rashod, is_device, comment" +
		          " FROM t_svod t," +
                  ReportParams.Pref + DBManager.sKernelAliasRest + " supplier su," +
                  centralData + "kvar k, " +
                  centralData + "dom d, " +
		          centralData + "s_ulica s, " +
		          centralData + "s_rajon sr, " +
		          centralData + "s_town st " +
		          " WHERE " +
                  " t.nzp_supp= su.nzp_supp" +
                  " AND t.nzp_kvar = k.nzp_kvar" +
                  " AND k.nzp_dom = d.nzp_dom" +
                  " AND d.nzp_ul = s.nzp_ul" +
                  " AND s.nzp_raj = sr.nzp_raj" +
                  " AND sr.nzp_town = st.nzp_town " +
		          " ORDER BY name_supp,predost, rajon, ulica, ulicareg, idom, ndom, ikvar, nkvar ";
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

            report.SetParameterValue("pmonth", months[Month] + " " + Year); 

			report.SetParameterValue("DATE", DateTime.Now.ToShortDateString());
			report.SetParameterValue("TIME", DateTime.Now.ToLongTimeString());

			string headerParam = !string.IsNullOrEmpty(TerritoryHeader) ? "Территория: " + TerritoryHeader + "\n" : string.Empty;
			headerParam += !string.IsNullOrEmpty(SupplierHeader) ? "Поставщики: " + SupplierHeader : string.Empty;
            headerParam += !string.IsNullOrEmpty(ServiceHeader) ? "Услуги: " + ServiceHeader : string.Empty;
			headerParam = headerParam.TrimEnd('\n');
			report.SetParameterValue("headerParam", headerParam);
		}


        /// <summary>
        /// Получить название услуги
        /// </summary>
        /// <returns></returns>
        private void GetNameServ()
        {

                    ServiceHeader = string.Empty;
                    string sql = " SELECT service from " + ReportParams.Pref + DBManager.sKernelAliasRest +
                                 "services  WHERE nzp_serv = " + aService;
                    DataTable serv = ExecSQLToTable(sql);
                    foreach (DataRow dr in serv.Rows)
                    {
                        ServiceHeader += dr["service"].ToString().Trim();
                    }

        }


		protected override void PrepareParams() {
            Month = UserParamValues["Month"].GetValue<int>();
            Year = UserParamValues["Year"].Value.To<int>();
			BankSupplier = JsonConvert.DeserializeObject<BankSupplierParameterValue>(UserParamValues["BankSupplier"].Value.ToString());
           	aService = UserParamValues["Services"].GetValue<int>();  
		}

		
        protected override void CreateTempTable()
        {    
            string sql;


            sql = " CREATE TEMP TABLE t_svod_kvar ( " +
                  " nzp_kvar integer, " +
                  " nzp_supp integer, " +
                  " count_gil integer, " +
                  " is_device integer, " +
                  " rashod " + DBManager.sDecimalType + "(20,7), " +
                  " sum_nach " + DBManager.sDecimalType + "(14,2) " +
                  " ) " + DBManager.sUnlogTempTable;
            ExecSQL(sql);
            ExecSQL(" CREATE INDEX ix_t_svod_kvar_1 on t_svod_kvar (nzp_kvar) ");
            ExecSQL(" CREATE INDEX ix_t_svod_kvar_2 on t_svod_kvar (nzp_supp) ");



            sql = " CREATE TEMP TABLE t_svod ( " +
                  " nzp_kvar integer, " +
                  " nzp_supp integer, " +
                  " count_gil integer, " +
                  " comment char(500) default '', " +
                  " pr integer, " +
                  " is_device integer, " +
                  " rashod " + DBManager.sDecimalType + "(20,7), " +
                  //" val_cnt " + DBManager.sDecimalType + "(20,7), " +
                  //" val_cnt_pred " + DBManager.sDecimalType + "(20,7), " +
                  //" dat_uchet DATE, " +
                  " sum_nach " + DBManager.sDecimalType + "(14,2) " +
                  " ) " + DBManager.sUnlogTempTable;
            ExecSQL(sql);
            ExecSQL(" CREATE INDEX ix_t_svod_1 on t_svod (nzp_kvar) ");
            ExecSQL(" CREATE INDEX ix_t_svod_2 on t_svod (nzp_supp) ");
		}

		protected override void DropTempTable() {
            ExecSQL(" DROP TABLE t_svod ");
            ExecSQL(" DROP TABLE t_svod_kvar ");
            ExecSQL(" DROP TABLE t_svod_mini ");
		}
	}
}
