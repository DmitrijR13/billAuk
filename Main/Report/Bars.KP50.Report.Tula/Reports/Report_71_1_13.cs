using Bars.KP50.Report.Base;
using Bars.KP50.Report.Tula.Properties;
using Bars.KP50.Utils;
using Newtonsoft.Json;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace Bars.KP50.Report.Tula.Reports
{
	class Report71113 : BaseSqlReport
	{
		public override string Name {
			get { return "71.1.13 Отчет по расходам, начислениям и оплатам"; }
		}

		public override string Description {
			get { return "71.1.13 Отчет по расходам, начислениям и оплатам"; }
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
			get { return Resources.Report_71_1_13; }
		}

		public override IList<ReportKind> ReportKinds {
			get { return new List<ReportKind> { ReportKind.Base }; }
		}

		//~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

		/// <summary>Поставщики, Агенты, Принципалы  </summary>
		protected BankSupplierParameterValue BankSupplier { get; set; }

		/// <summary>Заголовок территории</summary>
		protected string TerritoryHeader { get; set; }

		/// <summary>Заголовок поставщиков</summary>
		protected string SupplierHeader { get; set; }

		/// <summary>Районы</summary>
		private string AddressHeader { get; set; }

		/// <summary>Адрес</summary>
		private AddressParameterValue Address { get; set; }

		/// <summary> Период: с </summary>
		private DateTime DatS { get; set; }

		/// <summary> Период: gj </summary>
		private DateTime DatPo { get; set; }

		private Boolean GroupServices { get; set; }

		public override List<UserParam> GetUserParams() {
			return new List<UserParam>
			{
				new PeriodParameter(DateTime.Now,DateTime.Now),
				new BankSupplierParameter(),
				new AddressParameter(),
				new ComboBoxParameter
				{
					Code = "GroupServices",
					Name = "Услуги",
					Value = "0",
					StoreData = new List<object>
					{
						new { Id = "0", Name = "ХВС и водоотведение" },
						new { Id = "1", Name = "ГВС и отопление" }
					}
				}
			};
		}

		protected override void PrepareParams() {
			DateTime begin;
			DateTime end;
			var period = UserParamValues["Period"].GetValue<string>();
			PeriodParameter.GetValues(period, out begin, out end);
			DatS = begin;
			DatPo = end;

			BankSupplier = JsonConvert.DeserializeObject<BankSupplierParameterValue>(UserParamValues["BankSupplier"].Value.ToString());
			Address = UserParamValues["Address"].GetValue<AddressParameterValue>();
			GroupServices = UserParamValues["GroupServices"].GetValue<byte>().ToBool();
		}

		protected override void PrepareReport(FastReport.Report report) {
			report.SetParameterValue("period", DatS == DatPo
											? "за " + DatS.ToShortDateString() + " г."
											: "за период с " + DatS.ToShortDateString() + " по " + DatPo.ToShortDateString());
			report.SetParameterValue("DATE", DateTime.Now.ToShortDateString());
			report.SetParameterValue("TIME", DateTime.Now.ToShortTimeString());
			report.SetParameterValue("GroupServices", GroupServices);

			string headerInfo = string.Empty;
			if (!string.IsNullOrEmpty(AddressHeader))
				headerInfo += string.IsNullOrEmpty(AddressHeader) ? string.Empty : "Адрес: " + AddressHeader + "\n";
			else headerInfo += string.IsNullOrEmpty(TerritoryHeader) ? string.Empty : "Территория: " + TerritoryHeader + "\n";
			headerInfo += string.IsNullOrEmpty(SupplierHeader) ? string.Empty : "Поставщики: " + SupplierHeader + "\n";
			report.SetParameterValue("headerInfo", headerInfo.TrimEnd('\n'));
		}

		public override DataSet GetData() {
			MyDataReader reader;

			string whereSupplier = GetWhereSupp("nzp_supp");

            //string tempTable = GroupServices ? "t_gvs_71_1_13" : "t_hvs_71_1_13";
            //string whereServ1 = GroupServices ? "9,513" : "7,511",
            //        whereServ2 = GroupServices ? "8,512" : "6,510",
            //         whereAllServ = whereServ1 + "," + whereServ2;

            #region Определяем список домов по фильтру



		    string sql = " INSERT INTO t_filtr_dom (nzp_dom) " +
		                 " SELECT " + DBManager.sUniqueWord + " d.nzp_dom " +
		                 " FROM " + ReportParams.Pref + DBManager.sDataAliasRest + "dom d, " +
		                 ReportParams.Pref + DBManager.sDataAliasRest + "s_ulica u, " +
		                 ReportParams.Pref + DBManager.sDataAliasRest + "s_rajon r " +
		                 " where d.nzp_ul=u.nzp_ul" +
		                 " and r.nzp_raj = u.nzp_raj " +
		                 " " + GetWhereAdr();
            ExecSQL(sql);

            #endregion 

             sql = " SELECT bd_kernel AS pref, point " +
						 " FROM  " + ReportParams.Pref + DBManager.sKernelAliasRest + "s_point " +
						 " WHERE nzp_wp>1 " + GetWhereWp();
			ExecRead(out reader, sql);

   		   while (reader.Read())
			{
				string pref = reader["pref"].ToString().Trim(),
					   prefData = pref + DBManager.sDataAliasRest;

                ExecSQL("drop table sel_dom");
                ExecSQL("create temp table sel_dom (nzp_dom integer, nzp_kvar integer)");
               
               sql = " INSERT INTO sel_dom (nzp_dom, nzp_kvar) " +
                      " SELECT  d.nzp_dom, k.nzp_kvar " +
			          " FROM t_filtr_dom d" +
                      " INNER JOIN " + prefData + "kvar k ON k.nzp_dom = d.nzp_dom " +
                      " group by 1,2";
				ExecSQL(sql);

                ExecSQL("create index ixmtmp_10 on sel_dom(nzp_dom, nzp_kvar)");
                ExecSQL(DBManager.sUpdStat+ " sel_dom");


				for (int i = DatS.Year * 12 + DatS.Month; i < DatPo.Year * 12 + DatPo.Month + 1; i++)
				{
					var year = i / 12;
					var month = i % 12;
					if (month == 0)
					{
						year--;
						month = 12;
					}

					string calcTable = pref + "_charge_" + (year - 2000).ToString("00") + DBManager.tableDelimiter +
										"calc_gku_" + month.ToString("00");
					string chargeTable = pref + "_charge_" + (year - 2000).ToString("00") + DBManager.tableDelimiter +
										 "charge_" + month.ToString("00");
					string countersTable = pref + "_charge_" + (year - 2000).ToString("00") + DBManager.tableDelimiter +
											"counters_" + month.ToString("00");

					if (TempTableInWebCashe(calcTable) && TempTableInWebCashe(chargeTable) && TempTableInWebCashe(countersTable))
					{
						if (GroupServices)
						{
							#region заполнение временной таблицы по улугам ГВС и отопление
                            sql = " INSERT INTO t_gvs_71_1_13(nzp_dom, odpu_gvs)" +
                                  " SELECT nzp_dom, SUM(val4) " +
                                  " FROM " + countersTable + " c " +
                                  " WHERE c.nzp_dom  IN (SELECT nzp_dom FROM sel_dom GROUP BY 1)" +
                                    " AND c.nzp_type = 1 " +
                                    " AND c.stek = 3 " +
                                    " AND c.nzp_dom IN (SELECT nzp_dom " +
                                                      " FROM " + countersTable + " " +
                                                      " WHERE nzp_counter > 0 " +
                                                        " AND stek IN (1,2) " +
                                                        " AND nzp_type = 1 " +
                                                        " AND nzp_serv = 9 " +
                                                      " GROUP BY 1) " +
                                    " AND nzp_dom IN (SELECT DISTINCT s.nzp_dom " +
                                                    " FROM " + chargeTable + " ch INNER JOIN sel_dom s ON s.nzp_kvar = ch.nzp_kvar " +
                                                    " WHERE ch.nzp_serv = c.nzp_serv " +
                                                      " AND dat_charge IS NULL " + whereSupplier + ") " +
                                    " AND nzp_serv = 9 " +
                                  " GROUP BY 1 ";
                            ExecSQL(sql);

                            sql = " INSERT INTO t_gvs_71_1_13(nzp_dom, odpu_otpl)" +
                                  " SELECT nzp_dom, SUM(val4) " +
                                  " FROM " + countersTable + " c " +
                                  " WHERE c.nzp_dom IN (SELECT nzp_dom FROM sel_dom GROUP BY 1)" +
                                    " AND c.nzp_type = 1 " +
                                    " AND c.stek = 3 " +
                                    " AND c.nzp_dom IN (SELECT nzp_dom " +
                                                      " FROM " + countersTable + " " +
                                                      " WHERE nzp_counter > 0 " +
                                                        " AND stek IN (1,2) " +
                                                        " AND nzp_type = 1 " +
                                                        " AND nzp_serv = 8 " +
                                                      " GROUP BY 1) " +
                                    " AND nzp_dom IN (SELECT DISTINCT s.nzp_dom " +
                                                    " FROM " + chargeTable + " ch INNER JOIN sel_dom s ON s.nzp_kvar = ch.nzp_kvar " +
                                                    " WHERE ch.nzp_serv = c.nzp_serv " +
                                                      " AND dat_charge IS NULL " + whereSupplier + ") " +
                                    " AND nzp_serv = 8 " +
                                  " GROUP BY 1 ";
                            ExecSQL(sql);



                            #region Батарея тарифов горячая вода

                            ExecSQL("drop table t_gvs");

                            sql = " SELECT k.nzp_dom, a.nzp_kvar, max(tarif) as tarif, " +
                                  " sum(rsum_tarif) as rsum_tarif," +
                                  " sum(reval+real_charge) as reval, " +
                                  " sum(real_charge) as real_charge, " +
                                  " sum(sum_nedop) as sum_nedop," +
                                  " sum(sum_money) as sum_money, " +
                                  " sum(c_calc) as c_calc" +
                                  " into temp t_gvs " +
                                  " FROM " + chargeTable + " a,   sel_dom k" +
                                  " WHERE a.nzp_kvar=k.nzp_kvar " + whereSupplier +
                                  " AND nzp_serv  in (9,513) and dat_charge is null  " +
                                  " GROUP BY 1,2";
                            ExecSQL(sql);

                            ExecSQL("drop table t_domgvs");

                            sql = " SELECT nzp_dom, max(tarif) as tarif " +
                                  " into temp t_domgvs " +
                                  " FROM t_gvs " +
                                  " GROUP BY 1";
                            ExecSQL(sql);

                            sql = " SELECT max(tarif) as tarif FROM t_domgvs ";
                            object obj = ExecScalar(sql);
                            decimal maxTarif = obj != DBNull.Value ? (decimal)obj : 0;

                            sql = " update t_gvs set tarif = (select tarif" +
                                  " from t_domgvs where t_gvs.nzp_dom=t_domgvs.nzp_dom)" +
                                  " where tarif = 0 ";
                            ExecSQL(sql);

                            sql = " update t_gvs set tarif = " + maxTarif +
                                  " where tarif is null ";
                            ExecSQL(sql);

                            ExecSQL("drop table t_domgvs");

                            #endregion

                            sql = " INSERT INTO t_gvs_71_1_13 (nzp_dom, sum_charge_gvs, reval_gvs, " +
                                  "     sum_nedop_gvs, sum_money, gvs )" +
                                  " SELECT nzp_dom,  " +
                                  " sum(rsum_tarif) as sum_charge," +
                                  " sum(reval) as reval, " +
                                  " sum(sum_nedop) as real_charge," +
                                  " sum(sum_money) as sum_money, " +
                                  " sum(case when tarif>0 then (rsum_tarif+reval-sum_nedop)/tarif else 0 end)  " +
                                  " as rashod" +
                                  " FROM t_gvs " +
                                  " GROUP BY 1";
                            ExecSQL(sql);
                            ExecSQL("drop table t_gvs");

                            #region Батарея тарифов отопление

                            ExecSQL("drop table t_otpl");

                            sql = " SELECT k.nzp_dom, a.nzp_kvar, max(tarif) as tarif, " +
                                  " sum(rsum_tarif) as rsum_tarif," +
                                  " sum(reval+real_charge) as reval, " +
                                  " sum(real_charge) as real_charge, " +
                                  " sum(sum_nedop) as sum_nedop," +
                                  " sum(sum_money) as sum_money, " +
                                  " sum(c_calc) as c_calc" +
                                  " into temp t_otpl " +
                                  " FROM " + chargeTable + " a,   sel_dom k" +
                                  " WHERE a.nzp_kvar=k.nzp_kvar " + whereSupplier +
                                  " AND nzp_serv in (8,512) and dat_charge is null  " +
                                  " GROUP BY 1,2";
                            ExecSQL(sql);

                            ExecSQL("drop table t_domotpl");

                            sql = " SELECT nzp_dom, max(tarif) as tarif " +
                                  " into temp t_domotpl " +
                                  " FROM t_otpl " +
                                  " GROUP BY 1";
                            ExecSQL(sql);

                            sql = " SELECT max(tarif) as tarif FROM t_domotpl ";
                            obj = ExecScalar(sql);
                            maxTarif = obj != DBNull.Value ? (decimal)obj : 0;

                            sql = " update t_otpl set tarif = (select tarif" +
                                  " from t_domotpl where t_otpl.nzp_dom=t_domotpl.nzp_dom)" +
                                  " where tarif = 0 ";
                            ExecSQL(sql);

                            sql = " update t_otpl set tarif = " + maxTarif +
                                  " where tarif is null ";
                            ExecSQL(sql);

                            ExecSQL("drop table t_domotpl");

                            #endregion

                            sql = " insert into t_gvs_71_1_13 (nzp_dom, sum_charge_otpl, reval_otpl, " +
                                  "     sum_nedop_otpl, sum_money, otoplenie )" +
                                  " SELECT nzp_dom, sum(rsum_tarif) as sum_charge," +
                                  " sum(reval) as reval," +
                                  " sum(sum_nedop) ," +
                                  " sum(sum_money) as sum_money, " +
                                  " sum(case when tarif>0 then (rsum_tarif+reval-sum_nedop)/tarif else 0 end)  " +
                                  "  as rashod" +
                                  " FROM t_otpl " +
                                  " GROUP BY 1";
                            ExecSQL(sql);

                            ExecSQL("drop table t_otpl");



                            sql = " INSERT INTO t_all_71_1_13(nzp_dom,   odpu_gvs, odpu_otpl," +
                              " sum_charge_gvs, reval_gvs, sum_nedop_gvs, gvs," +
                              " sum_charge_otpl, reval_otpl, sum_nedop_otpl, otoplenie," +
                              " sum_money) " +
                              " SELECT nzp_dom, sum(odpu_gvs), sum(odpu_otpl), " +
                              " sum(sum_charge_gvs+reval_gvs-sum_nedop_gvs), " +
                              " sum(reval_gvs), " +
                              " sum(sum_nedop_gvs), " +
                              " sum(gvs)," +
                              " sum(sum_charge_otpl+reval_otpl-sum_nedop_otpl), " +
                              " sum(reval_otpl), " +
                              " sum(sum_nedop_otpl), " +
                              " sum(otoplenie), " +
                              " sum(sum_money)" +
                              " FROM t_gvs_71_1_13" +
                              " GROUP BY 1 ";
                            ExecSQL(sql);

							#endregion
						}
						else
						{
							#region заполнение временной таблицы по улугам ХВС и водоотведение

                            sql = " INSERT INTO t_hvs_71_1_13(nzp_dom, odpu_hvs)" +
						          " SELECT nzp_dom, SUM(val4) " +
						          " FROM " + countersTable + " c " +
						          " WHERE c.nzp_dom IN (SELECT nzp_dom " +
                                                      " FROM sel_dom GROUP BY 1)" +
                                    " AND c.nzp_type = 1 " +
                                    " AND c.stek = 3 " +
                                    " AND c.nzp_dom IN (SELECT nzp_dom " +
                                                      " FROM " + countersTable + 
                                                      " WHERE nzp_counter > 0 " +
                                                        " AND stek IN (1,2) " +
                                                        " AND nzp_type = 1 " +
                                                        " AND nzp_serv = 6 " +
                                                      " GROUP BY 1) " +
                                    " AND nzp_dom IN (SELECT DISTINCT s.nzp_dom " +
                                                    " FROM " + chargeTable + " ch INNER JOIN sel_dom s ON s.nzp_kvar = ch.nzp_kvar " +
                                                    " WHERE ch.nzp_serv = c.nzp_serv " +
                                                      " AND dat_charge IS NULL " + whereSupplier + ") " +
						            " AND nzp_serv = 6 " +
                                  " GROUP BY 1 ";
							ExecSQL(sql);

                            #region Батарея тарифов водоотведение

                            ExecSQL("drop table t_kan");

                            sql = " SELECT k.nzp_dom, a.nzp_kvar, max(tarif) as tarif, " +
                                  " sum(rsum_tarif) as rsum_tarif," +
                                  " sum(reval+real_charge) as reval, " +
                                  " sum(real_charge) as real_charge, " +
                                  " sum(sum_nedop) as sum_nedop," +
						          " sum(sum_money) as sum_money, " +
                                  " sum(c_calc) as c_calc" +
                                  " into temp t_kan "+
						          " FROM " + chargeTable + " a,   sel_dom k" +
						          " WHERE a.nzp_kvar=k.nzp_kvar " +whereSupplier+
						          " AND nzp_serv = 7 and dat_charge is null " +
						          " GROUP BY 1,2";
                            ExecSQL(sql);

                            ExecSQL("drop table t_domkan");

                            sql = " SELECT nzp_dom, max(tarif) as tarif " +
                                  " into temp t_domkan " +
                                  " FROM t_kan " +
                                  " GROUP BY 1";
                            ExecSQL(sql);

						    sql = " SELECT max(tarif) as tarif FROM t_domkan ";
						    object obj = ExecScalar(sql);
						    decimal maxTarif = obj != DBNull.Value ? (decimal) obj : 0;

                            sql = " update t_kan set tarif = (select tarif" +
                                  " from t_domkan where t_kan.nzp_dom=t_domkan.nzp_dom)" +
                                  " where tarif = 0 ";
                            ExecSQL(sql);

                            sql = " update t_kan set tarif = "+maxTarif+
                                  " where tarif is null ";
                            ExecSQL(sql);

                            ExecSQL("drop table t_domkan");

                            #endregion

                            sql = " INSERT INTO t_hvs_71_1_13 (nzp_dom, sum_charge_kan, reval_kan, " +
                                  "     sum_nedop_kan, sum_money, kanalizacia )" +
						          " SELECT nzp_dom,  " +
                                  " sum(rsum_tarif) as sum_charge," +
                                  " sum(reval) as reval, " +
                                  " sum(sum_nedop) as sum_nedop ," +
						          " sum(sum_money) as sum_money, " +
                                  " sum(case when tarif>0 then (rsum_tarif+reval-sum_nedop)/tarif else 0 end)  " +
						          " as rashod" +
                                  " FROM t_kan " +
						          " GROUP BY 1";
                            ExecSQL(sql);
                            ExecSQL("drop table t_kan");

                            #region Батарея тарифов ХВС

                            ExecSQL("drop table t_hvs");

                            sql = " SELECT k.nzp_dom, a.nzp_kvar, max(tarif) as tarif, " +
                                  " sum(rsum_tarif) as rsum_tarif," +
                                  " sum(reval+real_charge) as reval, " +
                                  " sum(real_charge) as real_charge, " +
                                  " sum(sum_nedop) as sum_nedop," +
                                  " sum(sum_money) as sum_money, " +
                                  " sum(c_calc) as c_calc" +
                                  " into temp t_hvs " +
                                  " FROM " + chargeTable + " a,   sel_dom k" +
                                  " WHERE a.nzp_kvar=k.nzp_kvar " + whereSupplier +
                                  " AND nzp_serv in (6,14,510,514) and dat_charge is null  " +
                                  " GROUP BY 1,2";
                            ExecSQL(sql);

                            ExecSQL("drop table t_domhvs");

                            sql = " SELECT nzp_dom, max(tarif) as tarif " +
                                  " into temp t_domhvs " +
                                  " FROM t_hvs " +
                                  " GROUP BY 1";
                            ExecSQL(sql);

                            sql = " SELECT max(tarif) as tarif FROM t_domhvs ";
                            obj = ExecScalar(sql);
                            maxTarif = obj != DBNull.Value ? (decimal)obj : 0;

                            sql = " update t_hvs set tarif = (select tarif" +
                                  " from t_domhvs where t_hvs.nzp_dom=t_domhvs.nzp_dom)" +
                                  " where tarif = 0 ";
                            ExecSQL(sql);

                            sql = " update t_hvs set tarif = " + maxTarif +
                                  " where tarif is null ";
                            ExecSQL(sql);

                            ExecSQL("drop table t_domkan");

                            #endregion

                            sql = " insert into t_hvs_71_1_13 (nzp_dom, sum_charge_hvs, reval_hvs, " +
                                  "     sum_nedop_hvs, sum_money, hvs )" +
                                  " SELECT nzp_dom, sum(rsum_tarif) as sum_charge," +
                                  " sum(reval) as reval, " +
                                  " sum(sum_nedop) ," +
						          " sum(sum_money) as sum_money, " +
                                  " sum(case when tarif>0 then (rsum_tarif+reval-sum_nedop)/tarif else 0 end)  " +
						          "  as rashod" +
                                  " FROM t_hvs " +
						          " GROUP BY 1";
                            ExecSQL(sql);

						    ExecSQL("drop table t_hvs");

							sql = " INSERT INTO t_all_71_1_13(nzp_dom,   odpu_hvs," +
                                  " sum_charge_kan, reval_kan, sum_nedop_kan, kanalizacia," +
							      " sum_charge_hvs, reval_hvs, sum_nedop_hvs, hvs," +
							      " sum_money) " +
								  " SELECT nzp_dom, sum(odpu_hvs), " +
							      " sum(sum_charge_kan+reval_kan-sum_nedop_kan), " +
                                  " sum(reval_kan), " +
                                  " sum(sum_nedop_kan), " +
                                  " sum(kanalizacia),"+
							      " sum(sum_charge_hvs+reval_hvs-sum_nedop_hvs), " +
                                  " sum(reval_hvs), " +
                                  " sum(sum_nedop_hvs), " +
                                  " sum(hvs), " +							      
                                  " sum(sum_money)" +
                                  " FROM t_hvs_71_1_13" +
							      " GROUP BY 1 ";
							ExecSQL(sql);
							#endregion
						}


					}
				}
			}

			reader.Close();

         
		    
                sql = " SELECT d.nzp_dom, rajon, ulica, ulicareg, idom, ndom, nkor, " +
                     " (CASE WHEN ulicareg IS NOT NULL THEN ulicareg || '. ' ELSE '' END) || ulica ||" +
                     " (CASE WHEN ndom = '-' THEN ''  ELSE ', д.' || ndom END) || " +
                     " (CASE WHEN nkor = '-' THEN '' ELSE ', кор.' || nkor END) AS address, " +
                     " otoplenie, gvs, odpu_gvs, odpu_otpl as odpu_otop," +
                     " sum_charge_gvs, reval_gvs, sum_nedop_gvs, " +
                     " sum_charge_otpl, reval_otpl, sum_nedop_otpl, " +
                     " kanalizacia, hvs, odpu_hvs," +
                     " sum_charge_kan, reval_kan, sum_nedop_kan, " +
                     " sum_charge_hvs, reval_hvs, sum_nedop_hvs, " +
                     " sum_money " +
                     " FROM t_all_71_1_13 a, " +
                     ReportParams.Pref + DBManager.sDataAliasRest + "dom d,  " +
                     ReportParams.Pref + DBManager.sDataAliasRest + "s_ulica u,  " +
                     ReportParams.Pref + DBManager.sDataAliasRest + "s_rajon r" +
                     " WHERE a.nzp_dom=d.nzp_dom" +
                     " and d.nzp_ul=u.nzp_ul" +
                     " and u.nzp_raj=r.nzp_raj " +
                     " ORDER BY 2,3,4,5,6,7,8 ";
                var ds = new DataSet();
                DataTable dt1 = ExecSQLToTable(sql);
                DataTable dt2 = ExecSQLToTable(sql);
                dt1.TableName = "Q_master2";
		        dt2.TableName = "Q_master1";
		    
            ds.Tables.Add(dt1);
            ds.Tables.Add(dt2);	
		    



		    
			


			return ds;
		}

		/// <summary>Ограничение по банкам данных</summary>
		private string GetWhereWp() {
			string whereWp = String.Empty;
			whereWp = BankSupplier.Banks != null
				? BankSupplier.Banks.Aggregate(whereWp, (current, nzpWp) => current + (nzpWp + ","))
				: ReportParams.GetRolesCondition(Constants.role_sql_wp);
			whereWp = whereWp.TrimEnd(',');
			whereWp = !String.IsNullOrEmpty(whereWp) ? " AND nzp_wp in (" + whereWp + ")" : String.Empty;
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

		/// <summary>Ограничение по адресу</summary>
		private string GetWhereAdr() {
			string rajon = String.Empty,
				street = String.Empty,
				house = String.Empty;
			string prefData = ReportParams.Pref + DBManager.sDataAliasRest;

			string result = ReportParams.GetRolesCondition(Constants.role_sql_area);


			if (Address.Raions != null)
			{
				rajon = Address.Raions.Aggregate(rajon, (current, nzpRajon) => current + (nzpRajon + ","));
				rajon = rajon.TrimEnd(',');
			}
			if (Address.Streets != null)
			{
				street = Address.Streets.Aggregate(street, (current, nzpStreet) => current + (nzpStreet + ","));
				street = street.TrimEnd(',');
			}
			if (Address.Houses != null)
			{
				house = Address.Houses.Aggregate(house, (current, nzpHouse) => current + (nzpHouse + ","));
				house = house.TrimEnd(',');
			}

			result = result.TrimEnd(',');
			result = !String.IsNullOrEmpty(result) ? " AND d.nzp_area in (" + result + ")" : String.Empty;
			result += !String.IsNullOrEmpty(rajon) ? " AND u.nzp_raj IN ( " + rajon + ") " : string.Empty;
			result += !String.IsNullOrEmpty(street) ? " AND u.nzp_ul IN ( " + street + ") " : string.Empty;
			result += !String.IsNullOrEmpty(house) ? " AND d.nzp_dom IN ( " + house + ") " : string.Empty;
			if (!String.IsNullOrEmpty(house))
			{
				var sql = " SELECT TRIM(town) AS  town, TRIM(rajon) AS rajon, TRIM(ulica) AS ulica, TRIM(ndom) AS ndom, TRIM(nkor) AS nkor " +
						  " FROM " + prefData + "dom d INNER JOIN " + prefData + "s_ulica u ON u.nzp_ul = d.nzp_ul " +
													 " INNER JOIN " + prefData + "s_rajon r ON r.nzp_raj = u.nzp_raj " +
													 " INNER JOIN " + prefData + "s_town t ON t.nzp_town = r.nzp_town" +
						  " WHERE nzp_dom IN (" + house + ") ";
				DataTable addressTable = ExecSQLToTable(sql);
				foreach (DataRow row in addressTable.Rows)
				{
					AddressHeader = "," + AddressHeader;
					AddressHeader += !string.IsNullOrEmpty(row["town"].ToString().Trim())
						? "," + row["town"].ToString().Trim() + "/"
						: ",-/";
					AddressHeader += !string.IsNullOrEmpty(row["rajon"].ToString().Trim())
						? row["rajon"].ToString().Trim() + ","
						: "-,";
					AddressHeader += !string.IsNullOrEmpty(row["ulica"].ToString().Trim())
						? "ул. " + row["ulica"].ToString().Trim() + ","
						: string.Empty;
					AddressHeader += !string.IsNullOrEmpty(row["ndom"].ToString().Trim())
						? row["ndom"].ToString().Trim() != "-"
							? "д. " + row["ndom"].ToString().Trim() + ","
							: string.Empty
						: string.Empty;
					AddressHeader += !string.IsNullOrEmpty(row["nkor"].ToString().Trim())
						? row["nkor"].ToString().Trim() != "-"
							? "кор. " + row["nkor"].ToString().Trim() + ","
							: string.Empty
						: string.Empty;
					AddressHeader = AddressHeader.TrimEnd(',');

				}
				AddressHeader = AddressHeader.TrimEnd(',');
			}
			else if (!String.IsNullOrEmpty(street))
			{
				var sql = " SELECT TRIM(town) AS  town, TRIM(rajon) AS rajon, TRIM(ulica) AS ulica " +
						  " FROM " + prefData + "s_ulica u INNER JOIN " + prefData + "s_rajon r ON r.nzp_raj = u.nzp_raj " +
														 " INNER JOIN " + prefData + "s_town t ON t.nzp_town = r.nzp_town " +
						  " WHERE nzp_ul IN (" + street + ") ";
				DataTable addressTable = ExecSQLToTable(sql);
				foreach (DataRow row in addressTable.Rows)
				{
					AddressHeader += !string.IsNullOrEmpty(row["town"].ToString().Trim())
						? "," + row["town"].ToString().Trim() + "/"
						: ",-/";
					AddressHeader += !string.IsNullOrEmpty(row["rajon"].ToString().Trim())
						? row["rajon"].ToString().Trim() + ","
						: "-,";
					AddressHeader += !string.IsNullOrEmpty(row["ulica"].ToString().Trim())
						? "ул. " + row["ulica"].ToString().Trim() + ","
						: string.Empty;
					AddressHeader = AddressHeader.TrimEnd(',');
				}
				AddressHeader = AddressHeader.TrimEnd(',');
			}
			else if (!String.IsNullOrEmpty(rajon))
			{
				var sql = " SELECT TRIM(town) AS  town, TRIM(rajon) AS rajon " +
						  " FROM " + prefData + "s_rajon r INNER JOIN " + prefData + "s_town t ON t.nzp_town = r.nzp_town " +
						  " WHERE nzp_raj IN (" + rajon + ") ";
				DataTable addressTable = ExecSQLToTable(sql);
				foreach (DataRow row in addressTable.Rows)
				{
					AddressHeader += !string.IsNullOrEmpty(row["town"].ToString().Trim())
						? "," + row["town"].ToString().Trim() + "/"
						: ",-/";
					AddressHeader += !string.IsNullOrEmpty(row["rajon"].ToString().Trim())
						? row["rajon"].ToString().Trim() + ","
						: "-,";
					AddressHeader = AddressHeader.TrimEnd(',');
				}
				AddressHeader = AddressHeader.TrimEnd(',');
			}
			if (!string.IsNullOrEmpty(AddressHeader))
				AddressHeader = AddressHeader.TrimStart(',');


			return result;
		}

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
					SupplierHeader += dr["name_supp"].ToString().Trim() + ", ";
				}
				SupplierHeader = SupplierHeader.TrimEnd(',', ' ');
			}
			return " and " + fieldPref + " in (select nzp_supp from " +
				   ReportParams.Pref + DBManager.sKernelAliasRest + "supplier " +
				   " where nzp_supp>0 " + whereSupp + ")";
		}

		protected override void CreateTempTable() {
            ExecSQL("create temp table t_filtr_dom (nzp_dom integer)");
		    string sql = " CREATE TEMP TABLE t_hvs_71_1_13( " +
		                 " nzp_dom INTEGER, " +
		                 " address CHARACTER(100), " +
		                 " kanalizacia " + DBManager.sDecimalType + "(14,7), " +
		                 " hvs " + DBManager.sDecimalType + "(14,7), " +
		                 " odpu_hvs " + DBManager.sDecimalType + "(14,7), " +
		                 " sum_charge_kan " + DBManager.sDecimalType + "(14,2), " +
		                 " sum_charge_hvs " + DBManager.sDecimalType + "(14,2), " +
		                 " reval_kan " + DBManager.sDecimalType + "(14,2), " +
		                 " reval_hvs " + DBManager.sDecimalType + "(14,2), " +
		                 " sum_nedop_kan " + DBManager.sDecimalType + "(14,2), " +
		                 " sum_nedop_hvs " + DBManager.sDecimalType + "(14,2), " +
		                 " sum_money " + DBManager.sDecimalType + "(14,2)) " + DBManager.sUnlogTempTable;
			ExecSQL(sql);

			sql = " CREATE TEMP TABLE t_gvs_71_1_13( " +
                         " nzp_dom INTEGER, " +
                         " address CHARACTER(100), " +
                         " gvs " + DBManager.sDecimalType + "(14,7), " +
                         " otoplenie " + DBManager.sDecimalType + "(14,7), " +
                         " odpu_gvs " + DBManager.sDecimalType + "(14,7), " +
                         " odpu_otpl " + DBManager.sDecimalType + "(14,7), " +
                         " sum_charge_gvs " + DBManager.sDecimalType + "(14,2), " +
                         " sum_charge_otpl " + DBManager.sDecimalType + "(14,2), " +
                         " reval_gvs " + DBManager.sDecimalType + "(14,2), " +
                         " reval_otpl " + DBManager.sDecimalType + "(14,2), " +
                         " sum_nedop_gvs " + DBManager.sDecimalType + "(14,2), " +
                         " sum_nedop_otpl " + DBManager.sDecimalType + "(14,2), " +
                         " sum_money " + DBManager.sDecimalType + "(14,2)) " + DBManager.sUnlogTempTable;
			ExecSQL(sql);

			sql = " CREATE TEMP TABLE t_all_71_1_13( " +
					" nzp_dom INTEGER, " +
					" address CHARACTER(100), " +
					" kanalizacia " + DBManager.sDecimalType + "(14,7), " +
					" hvs " + DBManager.sDecimalType + "(14,7), " +
					" odpu_hvs " + DBManager.sDecimalType + "(14,7), " +
					" sum_charge_kan " + DBManager.sDecimalType + "(14,2), " +
					" sum_charge_hvs " + DBManager.sDecimalType + "(14,2), " +
                    " reval_kan " + DBManager.sDecimalType + "(14,2), " +
                    " reval_hvs " + DBManager.sDecimalType + "(14,2), " +
                    " sum_nedop_kan " + DBManager.sDecimalType + "(14,2), " +
                    " sum_nedop_hvs " + DBManager.sDecimalType + "(14,2), " +
					" gvs " + DBManager.sDecimalType + "(14,7), " +
					" otoplenie " + DBManager.sDecimalType + "(14,7), " +
					" odpu_gvs " + DBManager.sDecimalType + "(14,7), " +
					" odpu_otpl " + DBManager.sDecimalType + "(14,7), " +
                    " sum_charge_gvs " + DBManager.sDecimalType + "(14,2), " +
                    " sum_charge_otpl " + DBManager.sDecimalType + "(14,2), " +
                    " reval_gvs " + DBManager.sDecimalType + "(14,2), " +
                    " reval_otpl " + DBManager.sDecimalType + "(14,2), " +
                    " sum_nedop_gvs " + DBManager.sDecimalType + "(14,2), " +
                    " sum_nedop_otpl " + DBManager.sDecimalType + "(14,2), " +
					" sum_money " + DBManager.sDecimalType + "(14,2)) " + DBManager.sUnlogTempTable;
			ExecSQL(sql);
		}

		protected override void DropTempTable() {
            ExecSQL("DROP TABLE t_filtr_dom");
            ExecSQL("DROP TABLE t_hvs_71_1_13 ");
			ExecSQL("DROP TABLE t_gvs_71_1_13 ");
			ExecSQL("DROP TABLE t_all_71_1_13 ");
		}
	}
}
