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
	class Report710124 : BaseSqlReport
	{

		public override string Name {
            get { return "71.1.24 Отчет комитета по тарифам Тульской области"; }
		}

		public override string Description {
            get { return "71.1.24 Отчет комитета по тарифам Тульской области"; }
		}

		public override IList<ReportGroup> ReportGroups {
			get { return new List<ReportGroup>(0); }
		}

		public override bool IsPreview {
			get { return false; }
		}

		public override IList<ReportKind> ReportKinds {
			get { return new List<ReportKind> { ReportKind.Base, ReportKind.ListLC }; }
		}

		protected override byte[] Template {
			get { return Resources.Report_71_1_24; }
		}

		/// <summary> с расчетного дня </summary>
		protected DateTime DatS { get; set; }

		/// <summary> по расчетный год </summary>
		protected DateTime DatPo { get; set; }

		/// <summary>Заголовок отчета</summary>
		protected string SupplierHeader { get; set; }

		/// <summary>Заголовок территории</summary>
		protected string TerritoryHeader { get; set; }

		/// <summary>Поставщики, Агенты, Принципалы  </summary>
		protected BankSupplierParameterValue BankSupplier { get; set; }

        /// <summary>Список префиксов банков в БД</summary>  
        private List<string> PrefBanks { get; set; } 

        private int YearS { get; set; }
        private int MonthS { get; set; }

        private int YearPo { get; set; }
        private int MonthPo { get; set; }
        /// <summary>Районы</summary>
        protected List<int> Rajons { get; set; }

		public override List<UserParam> GetUserParams() {
			var curCalcMonthYear = DBManager.GetCurMonthYear();
			return new List<UserParam>
			{
				new MonthParameter {Name = "Месяц с", Value = curCalcMonthYear != null ? curCalcMonthYear.Rows[0]["month_"] : DateTime.Today.Month },
                new YearParameter {Name = "Год с", Value = curCalcMonthYear != null ? curCalcMonthYear.Rows[0]["yearr"] : DateTime.Today.Year },
                new MonthParameter {Name = "Месяц по", Code = "Month1", Value = curCalcMonthYear != null ? curCalcMonthYear.Rows[0]["month_"] : DateTime.Today.Month },
                new YearParameter {Name = "Год по", Code = "Year1", Value = curCalcMonthYear != null ? curCalcMonthYear.Rows[0]["yearr"] : DateTime.Today.Year },
				new BankSupplierParameter(),
                new RaionsParameter()
			};
		}

		public override DataSet GetData() {
			#region выборка в temp таблицы

		    bool isLc= GetSelectedKvars();
		    GetwhereWp();
            string whereSupp = GetWhereSupp("c.nzp_supp");
		    string whereRaj = GetRajon("k.");
			string sql ;

		    foreach (var pref in PrefBanks)
		    {
		        string prefData = pref + DBManager.sDataAliasRest;
                                    string iflc = isLc
                        ? " and nzp_kvar in (select nzp_kvar from selected_kvars) "
                        : "";
		        string countersSpis = pref + DBManager.sDataAliasRest + "counters_spis";
		        //string kvarTable = isLc ? "selected_kvars" : pref + DBManager.sDataAliasRest + "kvar k ";

                for (int i = DatS.Year * 12 + DatS.Month; i < DatPo.Year * 12 + DatPo.Month + 1; i++)
                {
                    var year = i/12;
                    var month = i%12;
				    if (month == 0)
				    {
				        year--;
				        month = 12;
				    }

                    string calcGkuTable = pref + "_charge_" + (year - 2000).ToString("00") +
                                          DBManager.tableDelimiter + "calc_gku_" + month.ToString("00");
                    string countersTable = pref + "_charge_" + (year - 2000).ToString("00") +
                                           DBManager.tableDelimiter + "counters_" + month.ToString("00"); 
	
                    if (TempTableInWebCashe(countersTable))
                    {
                        sql =
                            " insert into t_mini( nzp_dom, nzp_kvar, nzp_serv, count_gil, has_ipu, ob_s, rashod, val_norm )" +
                            " select nzp_dom, nzp_kvar, c.nzp_serv, round(c.gil), is_device, c.squ,rashod, rash_norm_one " +
                            " from " + calcGkuTable + " c " +
                            " where c.stek=3  and nzp_kvar>0 and dat_charge is null  " +
                            "       and nzp_serv in(6,7,9,8,10,14,25,324,353)" + whereSupp + whereRaj + iflc;
                        ExecSQL(sql);

                        #region Добавляем количество счетчиков

                        sql = " UPDATE t_mini set count_ipu= (" +
                              " SELECT count(distinct num_cnt) " +
                              " FROM " + countersSpis + " a " +
                              " WHERE nzp_type=3 " +
                              "  AND  a.nzp=t_mini.nzp_kvar " +
                              "  AND  a.nzp_serv=t_mini.nzp_serv" +
                              "  AND  is_actual<>100 " +
                              " AND (dat_close > '" + DatPo.ToShortDateString() + "' or dat_close is null) " +
                              "  )" ;
                        ExecSQL(sql);
                        #endregion

                        #region Добавляем наименование норматива

                        sql = " UPDATE t_mini set cnt2= (" +
                              " SELECT MAX(cnt2) " +
                              " FROM " + countersTable + " a " +
                              " WHERE stek=3 AND nzp_type=3 " +
                              "  AND  a.nzp_kvar=t_mini.nzp_kvar " +
                              "  AND  a.nzp_serv=t_mini.nzp_serv " +
                              "  )," +
                              " cnt3= (" +
                              " SELECT MAX(cnt3) " +
                              " FROM " + countersTable + " a " +
                              " WHERE stek=3 AND nzp_type=3 " +
                              "  AND  a.nzp_kvar=t_mini.nzp_kvar " +
                              "  AND  a.nzp_serv=t_mini.nzp_serv " +
                              "  ) ";
                        ExecSQL(sql);

                        sql = " UPDATE t_mini set cnt2 = (" +
                              " SELECT MAX(val_prm" + DBManager.sConvToNum + ") " +
                              " FROM " + pref + DBManager.sDataAliasRest + "prm_1 p " +
                              " WHERE p.nzp = t_mini.nzp_kvar " +
                              " AND p.nzp_prm = 7 " +
                              " AND p.is_actual <> 100 " +
                              " AND dat_s <= DATE('" + DatPo.ToShortDateString() + "') " +
                              " AND dat_po >= DATE('" + DatS.ToShortDateString() + "')) " +
                              " WHERE cnt2 is null ";
                        ExecSQL(sql);

                        sql = " UPDATE t_mini set cnt2 = 0 where cnt2 is null ";
                        ExecSQL(sql);
                        #endregion
                    }
                }//месяц 

		        sql = "  insert into t_dom (nzp_dom, nzp_kvar, nzpdomnkvar) " +
		              "  select k.nzp_dom, k.nzp_kvar, k.nzp_dom||'.'||k.nkvar" + DBManager.sConvToChar+"(4)" +
		              "  from " + prefData + " kvar k " +
		              " where nzp_kvar>0 "+iflc;
                        ExecSQL(sql);

		        sql = " update t_dom set is_mkd = 1 " +
		              " where nzp_dom in (select nzp " +
		              " from " + prefData + "prm_2 " +
		              " where nzp_prm=2030 and is_actual=1 and val_prm='1')";
                        ExecSQL(sql);

		        sql = " update t_dom set god = (select max(val_prm) " +
		              " from " + prefData + "prm_2 " +
		              " where nzp_prm=150 and is_actual=1 and nzp=t_dom.nzp_dom)";
                        ExecSQL(sql);
            

                #region Вода

		        sql = " INSERT INTO t_voda_res (" +
		              " nzp_serv, is_mkd, nzp_y, nzp_res,  val_norm, rashod,  count_gil, count_ls, count_kvar ) " +
		              " SELECT s.nzp_serv, d.is_mkd, cnt2, cnt3, val_norm, " +
                      " sum(rashod), sum(count_gil), count(distinct d.nzp_kvar), count(distinct d.nzpdomnkvar)" +
                      " FROM t_mini s, t_dom d " +
		              " WHERE s.nzp_kvar=d.nzp_kvar and nzp_serv in(6,7,9,324,353) and has_ipu=0 " +
		              " GROUP BY 1,2,3,4,5";
		        ExecSQL(sql);

		        sql = " INSERT INTO t_voda_res (" +
		              " nzp_serv, is_mkd, nzp_y, nzp_res,  val_norm, " +
                      " rashod_ipu, count_gil_ipu, count_ls_ipu, count_kvar_ipu,  count_ipu) " +
		              " SELECT s.nzp_serv, d.is_mkd, cnt2, cnt3, val_norm, " +
                      " sum(rashod), sum(count_gil), count(distinct d.nzp_kvar), count(distinct nzpdomnkvar), sum(count_ipu)" +
		              " FROM t_mini s, t_dom d " +
		              " WHERE s.nzp_kvar=d.nzp_kvar and nzp_serv in(6,7,9,324,353) and has_ipu>0" +
		              " GROUP BY 1,2,3,4,5";
                ExecSQL(sql); 

		        sql = " UPDATE t_voda_res SET name_norm = (SELECT name_y " +
		              " FROM " + pref + DBManager.sKernelAliasRest + "res_y y " +
		              " WHERE t_voda_res.nzp_y = y.nzp_y " +
		              " AND t_voda_res.nzp_res = y.nzp_res  )";
                ExecSQL(sql);



            #endregion                                          

            
                #region Отопление

            sql = " INSERT INTO t_otopl_res (" +
                  " is_mkd,nzp_dom, god, rashod,  count_ls, count_kvar, ob_s )" +
                  " SELECT is_mkd, s.nzp_dom, god, sum(rashod), count(distinct s.nzp_kvar), count(distinct d.nzpdomnkvar), sum(ob_s) " +
                  " FROM  t_mini s, t_dom d " +
                  " WHERE s.nzp_kvar=d.nzp_kvar and nzp_serv=8 " +
                  " GROUP BY 1,2,3";
            ExecSQL(sql);

		        sql = " update t_otopl_res set has_odpu = (select max(val_prm) " +
		              " from " + prefData + "prm_2 " +
                      " where nzp_prm=361 and is_actual=1 and nzp=t_otopl_res.nzp_dom)" +
		              " where has_odpu is null";
            ExecSQL(sql);

            #endregion      
            

                #region Газ

		        sql = " INSERT INTO t_gas_res (" +
		              " is_mkd,  val_norm, rashod,  count_gil, count_ls, count_kvar) " +
		              " SELECT  is_mkd,  val_norm, " +
		              " sum(rashod), sum(count_gil), count(d.nzp_kvar), count(distinct d.nzpdomnkvar)" +
		              " FROM t_mini s, t_dom d" +
                      " WHERE s.nzp_kvar=d.nzp_kvar and nzp_serv =10 and has_ipu=0" +
		              " GROUP BY 1,2 ";
		        ExecSQL(sql);

		        sql = " INSERT INTO t_gas_res (" +
		              " is_mkd,  val_norm, rashod_ipu, count_gil_ipu, count_ls_ipu, count_kvar_ipu, count_ipu) " +
		              " SELECT  is_mkd,  val_norm, " +
		              " sum(rashod), sum(count_gil), count(d.nzp_kvar), count(distinct nzpdomnkvar), sum(has_ipu)" +
		              " FROM t_mini s, t_dom d" +
                      " WHERE s.nzp_kvar=d.nzp_kvar and nzp_serv =10 and has_ipu>0" +
		              " GROUP BY 1,2 ";
		        ExecSQL(sql);

            #endregion

            
                #region Электричество

		        sql = " INSERT INTO t_el_res (" +
		              " is_mkd, nzp_dom, god, val_norm, ob_s, rashod,  count_gil, count_ls, count_kvar) " +
		              " SELECT  is_mkd, d.nzp_dom, god, val_norm, sum(ob_s), " +
                      " sum(rashod), sum(count_gil), count(distinct d.nzp_kvar), count(distinct nzpdomnkvar)" + 
		              " FROM t_mini s, t_dom d " +
		              " WHERE s.nzp_kvar=d.nzp_kvar  and nzp_serv =25 and has_ipu=0 " +
		              " GROUP BY 1,2,3,4 ";
		        ExecSQL(sql);

		        sql = " INSERT INTO t_el_res (" +
		              " is_mkd, nzp_dom, god, val_norm, ob_s,  " +
		              " rashod_ipu, count_gil_ipu, count_ls_ipu, count_kvar_ipu, count_ipu) " +
		              " SELECT  is_mkd, d.nzp_dom, god, val_norm, sum(ob_s), " +
                      " sum(rashod), sum(count_gil), count(distinct d.nzp_kvar), count(distinct nzpdomnkvar), sum(has_ipu) " +
		              " FROM t_mini s, t_dom d " +
                      " WHERE s.nzp_kvar=d.nzp_kvar  and nzp_serv =25 and has_ipu>0 " +
		              " GROUP BY 1,2,3,4 ";
                ExecSQL(sql);
		       
                sql = " update t_el_res set floor = (select max(val_prm) " +
		              " from " + prefData + "prm_2 " +
		              " where nzp_prm=37 and is_actual=1 and nzp=t_el_res.nzp_dom)" +
                      " where floor is null ";
		        ExecSQL(sql);

		        sql = " update t_el_res set lift = (select max(val_prm) " +
		              " from " + prefData + "prm_2 " +
		              " where nzp_prm=1081 and is_actual=1 and nzp=t_el_res.nzp_dom)" +
		              " where lift is null";
		        ExecSQL(sql);


            #endregion   

           
                ExecSQL(" truncate t_mini ");  
                ExecSQL(" truncate t_dom ");
}





			#endregion

			#region Лист1 Вода

            sql = " select trim(service) as service, (case when is_mkd=0 then 'ЧС' else 'МКД' end ) as isMkd, " +
                  " name_norm, val_norm, sum(rashod)as rashod,  sum(count_gil)as count_gil, " +
		          " sum(count_ls)as count_ls, sum(count_kvar)as count_kvar, " +
                  " sum(rashod_ipu) as rashod_ipu , sum(count_gil_ipu) as count_gil_ipu, " +
                  " sum(count_ls_ipu) as count_ls_ipu, sum(count_kvar_ipu) as count_kvar_ipu, sum(count_ipu) as count_ipu " +
		          " FROM t_voda_res t, " + ReportParams.Pref + DBManager.sKernelAliasRest + "services s " +
                  " where t.nzp_serv=s.nzp_serv " +
                  " GROUP BY 1,2,3,4 " +
                  " ORDER BY 1,2,3,4";

			DataTable dt1 = ExecSQLToTable(sql);
			dt1.TableName = "Q_master";
			#endregion

            #region Лист2 Отопление

            sql = " select (case when is_mkd=0 then 'ЧС' else 'МКД' end ) as isMkd, point, " +
                  " CASE WHEN rajon='-' THEN town ELSE TRIM(town)||', '||TRIM(rajon) END AS rajon, " +
                  " ulica, ndom||nkor as ndom, god,has_odpu,ob_s, " +
                  " rashod, count_ls, count_kvar, " +
                  " count_has_ipu,  count_ind " +
                  " FROM t_otopl_res t, "+
                    ReportParams.Pref + DBManager.sDataAliasRest + "s_rajon sr, " +  
                    ReportParams.Pref + DBManager.sDataAliasRest + "s_town st, " +
                    ReportParams.Pref + DBManager.sDataAliasRest + "dom d, " +
                    ReportParams.Pref + DBManager.sDataAliasRest + "s_ulica ul," +
                    ReportParams.Pref + DBManager.sKernelAliasRest + "s_point p" +
                  " where t.nzp_dom=d.nzp_dom and  " +
                  " d.nzp_raj=sr.nzp_raj and d.nzp_town=st.nzp_town and d.nzp_ul=ul.nzp_ul and p.nzp_wp=d.nzp_wp " +
                  " ORDER BY 1,2,3,4,5";

            DataTable dt2 = ExecSQLToTable(sql);
            dt2.TableName = "Q_master1";
            #endregion

            #region Лист3 Газ

            sql = " select (case when is_mkd=0 then 'ЧС' else 'МКД' end ) as isMkd, " +
                  " val_norm, sum(rashod)as rashod, sum(count_gil)as count_gil, " +
                  " sum(count_ls)as count_ls, sum(count_kvar)as count_kvar, " +
                  " sum(rashod_ipu) as rashod_ipu , sum(count_gil_ipu) as count_gil_ipu," +
                  " sum(count_ls_ipu)as count_ls_ipu, " +
                  " sum(count_kvar_ipu)as count_kvar_ipu, sum(count_ipu) as count_ipu " +
                  " FROM t_gas_res t " +
                  " GROUP BY 1,2 " +
                  " ORDER BY 1,2 ";

            DataTable dt3 = ExecSQLToTable(sql);
            dt3.TableName = "Q_master2";
            #endregion

            #region Лист4 Электричество

		    sql = " select (case when is_mkd=0 then 'ЧС' else 'МКД' end ) as isMkd, point, " +
		          " CASE WHEN rajon='-' THEN town ELSE TRIM(town)||', '||TRIM(rajon) END AS rajon, " +
                  " ulica, ndom||(case when nkor<>'-' and nkor<>'' then 'корп.'||nkor else '' end) as ndom, god, ob_s, floor, lift, " +
                  " val_norm, sum(rashod)as rashod,  sum(count_gil) as count_gil, sum(count_ls) as count_ls, sum(count_kvar) as count_kvar, " +
                  " sum(rashod_ipu) as rashod_ipu, sum(count_gil_ipu) as count_gil_ipu , sum(count_ls_ipu)as count_ls_ipu , sum(count_kvar_ipu) as count_kvar_ipu, sum(count_ipu) as count_ipu" +
		          " FROM t_el_res t, " +
		          ReportParams.Pref + DBManager.sDataAliasRest + "s_rajon sr, " +
		          ReportParams.Pref + DBManager.sDataAliasRest + "s_town st, " +
		          ReportParams.Pref + DBManager.sDataAliasRest + "dom d, " +
		          ReportParams.Pref + DBManager.sDataAliasRest + "s_ulica ul," +
                  ReportParams.Pref + DBManager.sKernelAliasRest + "s_point p " +
		          " where t.nzp_dom=d.nzp_dom  and  d.nzp_raj=sr.nzp_raj and " +
		          " d.nzp_town=st.nzp_town and d.nzp_ul=ul.nzp_ul and p.nzp_wp=d.nzp_wp "+
                  " GROUP BY 1,2,3,4,5,6,7,8,9,10 "+
                  " ORDER BY 1,2,3,4,5,6,7,8 ";

            DataTable dt4 = ExecSQLToTable(sql);
            dt4.TableName = "Q_master3";
            #endregion

			var ds = new DataSet();
			ds.Tables.Add(dt1);
			ds.Tables.Add(dt2);
			ds.Tables.Add(dt3);
            ds.Tables.Add(dt4);
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
            if ((MonthS == MonthPo) & (YearS == YearPo))
            {
                report.SetParameterValue("period_month", months[MonthS] + " " + YearS);
            }
            else
            {
                report.SetParameterValue("period_month", "период с " + months[MonthS] + " " + YearS +
                                                         "г. по " + months[MonthPo] + " " + YearPo);

            }
			report.SetParameterValue("DATE", DateTime.Now.ToShortDateString());
			report.SetParameterValue("TIME", DateTime.Now.ToLongTimeString());

			string headerParam = !string.IsNullOrEmpty(TerritoryHeader) ? "Территория: " + TerritoryHeader + "\n" : string.Empty;
			headerParam += !string.IsNullOrEmpty(SupplierHeader) ? "Поставщики: " + SupplierHeader : string.Empty;
			headerParam = headerParam.TrimEnd('\n');
			report.SetParameterValue("headerParam", headerParam);
		}


        /// <summary>
        /// Ограничение по районам
        /// </summary>
        /// <returns></returns>
        public string GetRajon(string filedPref)
        {
            string whereRajon = String.Empty;
            if (Rajons != null)
            {
                whereRajon = Rajons.Aggregate(whereRajon, (current, nzpArea) => current + (nzpArea + ","));
            }
            whereRajon = whereRajon.TrimEnd(',');
            whereRajon = !String.IsNullOrEmpty(whereRajon)
                ? " AND " + filedPref + "nzp_dom in ( select nzp_dom " +
                  " from " + ReportParams.Pref + DBManager.sDataAliasRest + "dom d," +
                  ReportParams.Pref + DBManager.sDataAliasRest + "s_ulica su " +
                  " where d.nzp_ul=su.nzp_ul and su.nzp_raj in (" + whereRajon + "))"
                  : String.Empty;

            return whereRajon;
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
                        string sql = " insert into selected_kvars (nzp_kvar) " +
                                     " select nzp_kvar,nzp_dom from " + tSpls;
                        ExecSQL(sql);
                        ExecSQL("create index ix_sel_kvar_01 on selected_kvars(nzp_kvar)");
                        ExecSQL(DBManager.sUpdStat + " selected_kvars");
                        return true;
                    }
                }
            }
            return false;
        }


		protected override void PrepareParams() {
            MonthS = UserParamValues["Month"].GetValue<int>();
            YearS = UserParamValues["Year"].Value.To<int>();
            MonthPo = UserParamValues["Month1"].GetValue<int>();
            YearPo = UserParamValues["Year1"].Value.To<int>();
			BankSupplier = JsonConvert.DeserializeObject<BankSupplierParameterValue>(UserParamValues["BankSupplier"].Value.ToString());

            DatS = new DateTime(YearS, MonthS, 1);
            DatPo = new DateTime(YearPo, MonthPo, DateTime.DaysInMonth(YearPo, MonthPo));  
		}

		
        protected override void CreateTempTable()
        {    
            string sql;
            sql = " CREATE TEMP TABLE t_mini ( " +
                  " has_ipu integer, " +
                  " nzp_serv integer, " +
                  " nzp_kvar integer, " +
                  " nzp_dom integer, " +
                  " count_gil integer, " +
                  " count_ipu integer, " +
                  " cnt2 integer, " +
                  " cnt3 integer, " +
                  " ob_s " + DBManager.sDecimalType + "(20,2), " +
                  " rashod " + DBManager.sDecimalType + "(20,7), " +
                  " val_norm " + DBManager.sDecimalType + "(20,4) " +
                  " ) " + DBManager.sUnlogTempTable;
            ExecSQL(sql);

            sql = " CREATE TEMP TABLE t_dom ( " +
                  " is_mkd integer default 0, " +
                  " nzp_kvar integer, " +
                  " nzp_dom integer, " +
                  " nzpdomnkvar char(30), " +
                  " god char (20) " +
                  " ) " + DBManager.sUnlogTempTable;
            ExecSQL(sql); 

            sql = " CREATE TEMP TABLE t_voda_res ( " +
                  " nzp_serv INTEGER, " +
                  " is_mkd INTEGER, " +
                  " count_ls_ipu INTEGER, " +
                  " count_kvar_ipu INTEGER, " +
                  " count_gil_ipu INTEGER, " +
                  " count_ipu INTEGER, " +
                  " count_ls INTEGER, " +
                  " count_kvar INTEGER, " +
                  " count_gil INTEGER, " +
                  " nzp_y  INTEGER, " +
                  " nzp_res  INTEGER, " +
                  " name_norm  char(150), " +
                  " rashod " + DBManager.sDecimalType + "(20,7), " +
                  " rashod_ipu " + DBManager.sDecimalType + "(20,7), " +
                  " val_norm " + DBManager.sDecimalType + "(20,4)) " + DBManager.sUnlogTempTable;
            ExecSQL(sql);


            sql = " CREATE TEMP TABLE t_gas_res ( " +
                  " is_mkd INTEGER, " +
                  " count_ls_ipu INTEGER, " +
                  " count_kvar_ipu INTEGER, " +
                  " count_gil_ipu INTEGER, " +
                  " count_ipu INTEGER, " +
                  " count_ls INTEGER, " +
                  " count_kvar INTEGER, " +
                  " count_gil INTEGER, " +
                  " name_norm CHARACTER(150), " +
                  " rashod " + DBManager.sDecimalType + "(20,4), " +
                  " rashod_ipu " + DBManager.sDecimalType + "(20,4), " +
                  " val_norm " + DBManager.sDecimalType + "(20,4)) " + DBManager.sUnlogTempTable;
            ExecSQL(sql);

            sql = " CREATE TEMP TABLE t_el_res ( " +
                  " is_mkd INTEGER, " +
                  " nzp_dom INTEGER, " +
                  " count_ls_ipu INTEGER, " +
                  " count_kvar_ipu INTEGER, " +
                  " count_gil_ipu INTEGER, " +
                  " count_ipu INTEGER, " +
                  " count_ls INTEGER, " +
                  " count_kvar INTEGER, " +
                  " count_gil INTEGER, " +
                  " floor char(20), " +
                  " lift char(20), " +
                  " name_norm CHARACTER(150), " +
                  " rashod " + DBManager.sDecimalType + "(20,4), " +
                  " rashod_ipu " + DBManager.sDecimalType + "(20,4), " +
                  " god char(10), " +
                  " ob_s " + DBManager.sDecimalType + "(20,2), " +
                  " val_norm " + DBManager.sDecimalType + "(20,2)) " + DBManager.sUnlogTempTable;
            ExecSQL(sql);

            sql = " CREATE TEMP TABLE t_otopl_res ( " +
                  " is_mkd INTEGER, " +
                  " nzp_dom INTEGER, " +
                  " has_odpu char(20), " +
                  " count_ls INTEGER, " +
                  " count_kvar INTEGER, " +
                  " count_has_ipu INTEGER, " +
                  " count_ind INTEGER, " +
                  " god char(10), " +
                  " ob_s " + DBManager.sDecimalType + "(20,2), " +
                  " rashod " + DBManager.sDecimalType + "(20,4)) " + DBManager.sUnlogTempTable;
            ExecSQL(sql);

            ExecSQL(" create index svod_index_1 on t_mini (nzp_kvar)");
            ExecSQL(" create index el_index_1 on t_el_res (nzp_dom)");
            ExecSQL(" create index otopl_index_1 on t_otopl_res (nzp_dom)");
            ExecSQL(" create index dom_index_1 on t_dom (nzp_dom)");

            if (ReportParams.CurrentReportKind == ReportKind.ListLC)
            {
                ExecSQL(" create temp table selected_kvars(nzp_kvar integer, nzp_dom integer) " + DBManager.sUnlogTempTable);
            }         

		}

		protected override void DropTempTable() {
            if (ReportParams.CurrentReportKind == ReportKind.ListLC)
            {
                ExecSQL(" drop table selected_kvars " + DBManager.sUnlogTempTable);
            }    
            ExecSQL(" DROP TABLE t_dom ");
            ExecSQL(" DROP TABLE t_mini ");
            ExecSQL(" DROP TABLE t_el_res ");
            ExecSQL(" DROP TABLE t_otopl_res  ");
            ExecSQL(" drop table t_voda_res ", true);
            ExecSQL(" drop table t_gas_res ", true);

		}
	}
}
