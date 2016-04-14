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
	class Report7116 : BaseSqlReport
	{

		public override string Name {
			get { return "71.1.6 Отчет по начислениям и оплатам по ИПУ и нормативам"; }
		}

		public override string Description {
			get { return "71.1.6 Отчет по начислениям и оплатам для поставщиков по ИПУ и нормативам"; }
		}

		public override IList<ReportGroup> ReportGroups {
			get { return new List<ReportGroup>(0); }
		}

		public override bool IsPreview {
			get { return false; }
		}

		public override IList<ReportKind> ReportKinds {
			get { return new List<ReportKind> { ReportKind.Base }; }
		}

		protected override byte[] Template {
			get { return Resources.Report_71_1_6; }
		}

		/// <summary> с расчетного дня </summary>
		protected DateTime DatS { get; set; }

		/// <summary> по расчетный год </summary>
		protected DateTime DatPo { get; set; }

		/// <summary>Заголовок - поставщик</summary>
		protected string SupplierHeader { get; set; }

        /// <summary>Заголовок - принципал</summary>
        protected string PrincipalHeader { get; set; }

        /// <summary>Заголовок - агент</summary>
        protected string AgentHeader { get; set; }

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
				new BankSupplierParameter()
			};
		}

		public override DataSet GetData() {
			MyDataReader reader;

			#region выборка в temp таблицу

		    string whereSupp = GetWhereSupp("a.nzp_supp");

			string sql = " SELECT bd_kernel as pref " +
						 " FROM " + DBManager.GetFullBaseName(Connection) + DBManager.tableDelimiter + "s_point " +
						 " WHERE nzp_wp>1 " + GetwhereWp();

			ExecRead(out reader, sql);

			while (reader.Read())
			{
				if (reader["pref"] != null)
				{
					string pref = reader["pref"].ToStr().Trim();

					for (int i = DatS.Year * 12 + DatS.Month; i < DatPo.Year * 12 + DatPo.Month + 1; i++)
					{
						var year = i / 12;
						var month = i % 12;
						if (month == 0)
						{
							year--;
							month = 12;
						}

						string calcGkuTable = pref + "_charge_" + (year - 2000).ToString("00") +
											  DBManager.tableDelimiter + "calc_gku_" + month.ToString("00");
						string chargeTable = pref + "_charge_" + (year - 2000).ToString("00") + DBManager.tableDelimiter +
											 "charge_" + month.ToString("00");
						string perekidkaTable = pref + "_charge_" + (year - 2000).ToString("00") + DBManager.tableDelimiter +
												"perekidka ";
						string countersTable = pref + "_charge_" + (year - 2000).ToString("00") +
								  DBManager.tableDelimiter + "counters_" + month.ToString("00");
					    string kvar = pref + DBManager.sDataAliasRest + "kvar ";


						if (TempTableInWebCashe(calcGkuTable) && TempTableInWebCashe(chargeTable) && TempTableInWebCashe(countersTable))
						{
                            ExecSQL("drop table t_nach_tmp");

                            sql = " Create temp table t_nach_tmp" +
                                  " (pref CHARACTER(15), " +
                                  " heater CHARACTER(20), " +//Наименование котельной 1010141
                                  " waterin CHARACTER(20), " +//Наименование Водозабора 1010137
                                  " nzp_y integer," +//код наименования норматива
                                  " nzp_dom integer," +
                                  " nzp_kvar integer," +
                                  " is_device integer," +
                                  " nzp_serv integer, " +
                                  " nzp_supp integer, " +
                                  " count_gil integer," +
                                  " normativ " + DBManager.sDecimalType + "(14,4)," +
                                  " tarif " + DBManager.sDecimalType + "(14,4)," +
                                  " rashod " + DBManager.sDecimalType + "(14,7)," +
                                  " sum_nach " + DBManager.sDecimalType + "(14,2)," +
                                  " real_charge " + DBManager.sDecimalType + "(14,2)," +
                                  " sum_money " + DBManager.sDecimalType + "(14,2)," +
                                  " c_calc " + DBManager.sDecimalType + "(14,4))";
                            ExecSQL(sql);

						    sql =
						        " INSERT INTO t_nach_tmp(nzp_dom, nzp_kvar, is_device, nzp_serv, nzp_supp, " +
						        " tarif, normativ, count_gil ) " +
						        " SELECT " +
						        " a.nzp_dom, " +
						        " a.nzp_kvar, " +
						        " (case when a.is_device = 0 then 0 " +
						        " when a.is_device = 9 then 9 else 1 end), " +
						        " a.nzp_serv, " +
						        " a.nzp_supp, " +
						        " max(a.tarif) , " +
						        " max(a.rash_norm_one)," +
						        " sum(gil) " +
						        " FROM " + calcGkuTable + " a " +
						        " WHERE a.nzp_serv IN (6,7,9,14,324,353,200,201,202)  " +
						        " AND a.dat_charge is null " + whereSupp +
						        " AND a.nzp_kvar>1 AND stek=3 " +
						        " GROUP BY 1,2,3,4,5 ";
                            ExecSQL(sql);

						    sql =
						        " INSERT INTO t_nach_tmp(nzp_dom, nzp_kvar, is_device, nzp_serv, nzp_supp, " +
						        " tarif, sum_nach, real_charge,sum_money ) " +
						        " SELECT "+
						        " k.nzp_dom, " +
						        " k.nzp_kvar, " +
						        " (case when a.is_device = 0 then 0 " +
						        " when a.is_device = 9 then 9 else 1 end), " +
						        " a.nzp_serv, " +
						        " a.nzp_supp, " +
						        " max(tarif) , " +
						        " SUM(sum_tarif + reval), SUM(real_charge), SUM(sum_money) " +
						        " FROM " +chargeTable + " a, "  + kvar + "k" +
						        " WHERE nzp_serv IN (6,7,9,14,324,353,200,201,202) " +
						        " AND dat_charge is null " + whereSupp +
						        " AND a.nzp_kvar=k.nzp_kvar " +
						        " GROUP BY 1,2,3,4,5 ";
                            ExecSQL(sql);

                            ExecSQL("Create index ixt_t_nacht_01 on t_nach_tmp(nzp_kvar, nzp_serv, nzp_supp)");
                            ExecSQL("Create index ixt_t_nacht_02 on t_nach_tmp(nzp_dom, nzp_serv)");
                            ExecSQL(DBManager.sUpdStat + " t_nach_tmp");
                            
                            ExecSQL("drop table t_nach");

							sql = " Create temp table t_nach" +
								  " (pref CHARACTER(15), " +
								  " heater CHARACTER(20), " +//Наименование котельной 1010141
								  " waterin CHARACTER(20), " +//Наименование Водозабора 1010137
								  " nzp_y integer," +//код наименования норматива
								  " nzp_dom integer," +
								  " nzp_kvar integer," +
								  " is_device integer," +
								  " nzp_serv integer, " +
                                  " nzp_supp integer, " +
								  " count_gil integer," +
                                  " normativ " + DBManager.sDecimalType + "(14,4)," +
								  " tarif " + DBManager.sDecimalType + "(14,4)," +
								  " rashod " + DBManager.sDecimalType + "(14,7)," +
								  " sum_nach " + DBManager.sDecimalType + "(14,2)," +
								  " real_charge " + DBManager.sDecimalType + "(14,2)," +
								  " sum_money " + DBManager.sDecimalType + "(14,2)," +
								  " c_calc " + DBManager.sDecimalType + "(14,4))";
							ExecSQL(sql);

							#region Запонение расхода, норматива и пр.

						    sql =
						        " INSERT INTO t_nach(pref, nzp_dom, nzp_kvar, nzp_serv, nzp_supp, is_device,  " +
						        " tarif, normativ, count_gil, sum_nach, real_charge,sum_money   ) " +
						        " SELECT '" + pref + month + year + "' AS pref, " +
						        " nzp_dom, " +
						        " nzp_kvar, " +
						        " nzp_serv, " +
						        " nzp_supp, " +						        
                                " max(is_device), " +
						        " max(tarif) , " +
						        " max(normativ), " +
                                " SUM(count_gil), SUM(sum_nach), SUM(real_charge), SUM(sum_money) " +
						        " FROM t_nach_tmp " +
						        " GROUP BY 1,2,3,4,5 ";
                            ExecSQL(sql);

							ExecSQL("Create index ixt_t_nach_01 on t_nach(nzp_kvar, nzp_serv)");
							ExecSQL("Create index ixt_t_nach_02 on t_nach(nzp_dom, nzp_serv)");
							ExecSQL(DBManager.sUpdStat + " t_nach");
							#endregion

                            //#region Добавляем начисления и оплаты

                            //sql = " UPDATE t_nach SET sum_nach = " +
                            //      " (SELECT SUM(sum_tarif + reval) " +
                            //      " FROM " + chargeTable + " a " +
                            //      " WHERE a.nzp_kvar=t_nach.nzp_kvar " +
                            //      "  AND  a.nzp_serv=t_nach.nzp_serv " +
                            //      "  AND  a.nzp_supp=t_nach.nzp_supp " + 
                            //      "  AND  dat_charge is null), " +
                            //      " real_charge = " +
                            //      " (SELECT SUM(real_charge) " +
                            //      " FROM " + chargeTable + " a " +
                            //      " WHERE a.nzp_kvar=t_nach.nzp_kvar " +
                            //      "  AND  a.nzp_serv=t_nach.nzp_serv " +
                            //      "  AND  a.nzp_supp=t_nach.nzp_supp " +
                            //      "  AND  dat_charge is null)," +
                            //      "  sum_money = " +
                            //      " (SELECT SUM(sum_money) " +
                            //      " FROM " + chargeTable + " a " +
                            //      " WHERE nzp_serv IN (6,7,9,14,324,353,200,201,202) " +
                            //      "  AND  a.nzp_kvar=t_nach.nzp_kvar " +
                            //      "  AND  a.nzp_serv=t_nach.nzp_serv " +
                            //      "  AND  a.nzp_supp=t_nach.nzp_supp " +
                            //      "  AND  dat_charge is null)  " +
                            //      " WHERE pref= '" + pref + month + year + "'";
                            //ExecSQL(sql);


                            //#endregion         

							#region Добавляем корректировки начислений

							ExecSQL("drop table t_perekidka");

							sql = " select nzp_kvar, nzp_serv, sum(sum_rcl) as sum_rcl " +
								  " into temp t_perekidka " +
								  " from " + perekidkaTable +   " a "  +
								  " where type_rcl not in (100,20) and nzp_serv in (6,7,9,14,324,353,200,201,202)" +
								  " and month_=" + month + whereSupp +
								  " group by 1,2 ";
							ExecSQL(sql);

							ExecSQL("Create index ixt_t_perekidka_01 on t_perekidka(nzp_kvar, nzp_serv)");
							ExecSQL(DBManager.sUpdStat + " t_perekidka");

							sql = " update t_nach set sum_nach = sum_nach + (select sum(sum_rcl)" +
								  " from t_perekidka a" +
								  " where  t_nach.nzp_kvar=a.nzp_kvar and t_nach.nzp_serv=a.nzp_serv)" +
								  " where 0<(select count(*) " +
								  " from t_perekidka a" +
								  " where  t_nach.nzp_kvar=a.nzp_kvar AND t_nach.real_charge <> 0 " +
								  " and t_nach.nzp_serv=a.nzp_serv )";
							ExecSQL(sql);

							ExecSQL("drop table t_perekidka");
							#endregion

							#region Добавляем наименование норматива

						    sql = " UPDATE t_nach set nzp_y = (" +
						          " SELECT MAX(cnt2) " +
                                  " FROM " + countersTable +  " a " +
						          " WHERE stek=3 AND nzp_type=3 " +
						          "  AND  a.nzp_kvar=t_nach.nzp_kvar " +
                                  "  AND  a.nzp_serv=t_nach.nzp_serv " +
                                  "  )  ";
                            ExecSQL(sql);   
                            
                            sql = " UPDATE t_nach set nzp_y = (" +
								  " SELECT MAX(val_prm" + DBManager.sConvToNum + ") " +
								  " FROM " + pref + DBManager.sDataAliasRest + "prm_1 p " +
								  " WHERE p.nzp = t_nach.nzp_kvar " +
								  " AND p.nzp_prm = 7 " +
								  " AND p.is_actual <> 100 " +
								  " AND dat_s <= DATE('" + DatPo.ToShortDateString() + "') " +
								  " AND dat_po >= DATE('" + DatS.ToShortDateString() + "')) " +
                                  " WHERE nzp_y is null ";
							ExecSQL(sql);

							sql = " UPDATE t_nach set nzp_y = 0 where nzp_y is null ";
							ExecSQL(sql);
							#endregion                                                             

							#region Батарея тарифов
							ExecSQL("drop table t_tarif");

							sql = " select nzp_dom, nzp_serv,  max(tarif) as tarif " +
								  " into temp t_tarif " +
								  " FROM t_nach " +
								  " group by 1,2";
							ExecSQL(sql);
							ExecSQL("Create index ixt_t_tarif_01 on t_tarif(nzp_dom, nzp_serv)");
							ExecSQL(DBManager.sUpdStat + " t_tarif");

							ExecSQL("drop table t_servtarif");
							sql = " SELECT nzp_serv, max(tarif) as tarif " +
								  " into temp t_servtarif " +
								  " FROM t_tarif" +
								  " group by 1 ";
							ExecSQL(sql);


							sql = " update t_nach set tarif = (select tarif" +
								  " from t_tarif " +
								  " where t_nach.nzp_dom=t_tarif.nzp_dom " +
								  " and t_nach.nzp_serv=t_tarif.nzp_serv) " +
								  " where tarif = 0 ";
							ExecSQL(sql);


							sql = " update t_nach set tarif = (select tarif" +
								  " from t_servtarif " +
								  " where t_nach.nzp_serv=t_servtarif.nzp_serv) " +
								  " where tarif is null or tarif = 0";
							ExecSQL(sql);


							ExecSQL("drop table t_servtarif");
							ExecSQL("drop table t_tarif");
							#endregion
   
							#region Добавляем счетчик по канализации
							sql = "Create temp table t_device (nzp_kvar integer, is_device integer)" + DBManager.sUnlogTempTable;
							ExecSQL(sql);

							sql = " insert into t_device " +
								  " select nzp_kvar, max(is_device) as is_device " +
								  " from t_nach " +
								  " where is_device >0  " +
								  " and nzp_serv in (6,9)" +
								  " group by 1";
							ExecSQL(sql);

                            // устанавливаем счетчик за канализацию, там где есть водопроводный счетчик
                            sql = " UPDATE t_nach " +
                                  " set is_device = 1 " +
                                  " where nzp_kvar in (select nzp_kvar from t_device where is_device = 1 )   " +
                                  " and nzp_serv IN (7,324,353) and is_device = 0  ";
                            ExecSQL(sql);

							sql = " UPDATE t_nach " +
								  " set is_device = 9 " +
								  " where nzp_kvar in (select nzp_kvar from t_device where is_device = 9 )   " +
								  " and nzp_serv IN (7,324,353) and is_device = 0  ";
							ExecSQL(sql);


							ExecSQL(" drop table t_device");

							#endregion

							#region группируем по домам

							ExecSQL(" drop table t_domnach", false);

							sql = " select pref,  heater , " + //Наименование котельной 1010141
								  " waterin , " + //Наименование Водозабора 1010137
								  " nzp_y," + //код наименования норматива
								  " nzp_dom ," +
								  " is_device," +
								  " nzp_serv," +
                                  " tarif," +
                                  " max(normativ) as normativ, " +
								  " sum(count_gil) as count_gil," + 
								  " sum(case when tarif>0 then sum_nach/tarif else 0 end) as rashod, " +
								  " sum(sum_nach) as sum_nach," +
								  " sum(sum_money) as sum_money " +
								  " into temp t_domnach" +
								  " from t_nach" +
								  " group by 1,2,3,4,5,6,7,8";
							ExecSQL(sql);

							#endregion

							ExecSQL("drop table t_nach");

							#region Проставляем котельную и водозабор

							sql = " UPDATE t_domnach set heater = (" +
							   " SELECT max(val_prm) " +
							   " FROM " + pref + DBManager.sDataAliasRest + "prm_2 p " +
							   " WHERE p.nzp = t_domnach.nzp_dom " +
							   " AND p.nzp_prm = 1010141 " +
							   " AND p.is_actual <> 100 " +
							   " AND dat_s <= DATE('" + DatPo.ToShortDateString() + "') " +
							   " AND dat_po >= DATE('" + DatS.ToShortDateString() + "')) ";
							ExecSQL(sql);

							sql = " UPDATE t_domnach set waterin = (" +
								  " SELECT max(val_prm) " +
								  " FROM " + pref + DBManager.sDataAliasRest + "prm_2 p " +
								  " WHERE p.nzp = t_domnach.nzp_dom " +
								  " AND p.nzp_prm = 1010137 " +
								  " AND p.is_actual <> 100 " +
								  " AND dat_s <= DATE('" + DatPo.ToShortDateString() + "') " +
								  " AND dat_po >= DATE('" + DatS.ToShortDateString() + "')) ";
							ExecSQL(sql);

							sql = " UPDATE t_domnach set heater = 'не проставлена'" +
								  " Where heater is null";
							ExecSQL(sql);

							sql = " UPDATE t_domnach set waterin = 'не проставлен'" +
								  " Where waterin is null";
							ExecSQL(sql);
							#endregion

							string datS = "1." + month + "." + year,
									datPo = DateTime.DaysInMonth(year, month) + "." + month + "." + year;
							string prefData = pref + DBManager.sDataAliasRest,
									prefKernel = pref + DBManager.sKernelAliasRest;

							sql = " INSERT INTO t_res_y_71_1_6(nzp_y, name_y) " +
								  " SELECT nzp_y, " +
										 " name_y " +
								  " FROM " + prefKernel + "res_y " +
								  " WHERE nzp_res = (SELECT val_prm " + DBManager.sConvToInt +
												   " FROM " + prefData + "prm_13 " +
												   " WHERE nzp_prm = 172 " +
													 " AND is_actual <> 100 " +
													 " AND dat_s = (SELECT MAX(dat_s) " +
																  " FROM " + prefData + "prm_13 " +
																  " WHERE is_actual <> 100 " +
																	" AND nzp_prm = 172 " +
																	" AND dat_s <= DATE('" + datPo + "') " +
																	" AND dat_po >= DATE('" + datS + "')) " +
													 " AND dat_s <= DATE('" + datPo + "') " +
													 " AND dat_po >= DATE('" + datS + "')) ";
							ExecSQL(sql);

							sql = " INSERT INTO t_norm_imd(pref, heater, waterin, is_device, nzp_serv, " +
								  " name_y, tarif, normativ, rashod, sum_nach, sum_money,count_gil ) " +
								  " SELECT pref, heater, waterin,  is_device, nzp_serv, " +
								  " trim(" + DBManager.sNvlWord + "(name_y,'Норматив не проставлен')), " +
								  " tarif, " +
								  " (CASE WHEN is_device = 0 THEN normativ ELSE 0 END) AS normativ, " +
								  " SUM(rashod) AS rashod, " +
								  " SUM(sum_nach), " +
								  " SUM(sum_money),  " +
								  " SUM(count_gil)  " +
								  " FROM t_domnach a left outer join t_res_y_71_1_6 r ON a.nzp_y = r.nzp_y " +
								  " GROUP BY 1,2,3,4,5,6,7,8 ";
							ExecSQL(sql);

							ExecSQL("drop table t_nachdom");
							ExecSQL("DELETE FROM t_res_y_71_1_6 ");
						}
					}
				}
			}

			reader.Close();
			#endregion

            #region Норматив

            sql = " INSERT INTO t_HVGV_71_1_6(heater, name_y, order_, service_1, tarif_1, normativ_1, rashod_1, sum_nach_1, sum_money_1, count_gil_1) " +
                  " SELECT 'Водозабор: '||trim(waterin)||' Котельная '||trim(heater) as heater, name_y, 1 as order_, " +
                         " 'холодное водоснабжение' AS service, " +
                         " tarif, max(normativ) as normativ, sum(rashod), sum(sum_nach), sum(sum_money), sum(count_gil) " +
                  " FROM t_norm_imd " +
                  " WHERE nzp_serv = 6 and is_device=0 " +
                  " GROUP BY 1,2,3,4,5 ";
            ExecSQL(sql);

            sql = " INSERT INTO t_HVGV_71_1_6(heater, name_y, order_, service_2, tarif_2, normativ_2, rashod_2, sum_nach_2, sum_money_2, count_gil_2) " +
                  " SELECT 'Водозабор: '||trim(waterin)||' Котельная '||trim(heater) as heater, name_y, 1 as order_, " +
                          " 'горячее водоснабжение' AS service, " +
                           " tarif,  max(normativ) as normativ, sum(rashod), sum(sum_nach), sum(sum_money), sum(count_gil) " +
                  " FROM t_norm_imd " +
                  " WHERE nzp_serv = 9 and is_device=0 " +
                  " GROUP BY 1,2,3,4,5 ";
            ExecSQL(sql);

            #endregion

            #region Норматив - Итог

            sql = " INSERT INTO t_HVGV_71_1_6(heater, name_y, order_, service_1,  tarif_1, normativ_1, rashod_1, sum_nach_1, sum_money_1, count_gil_1) " +
                  " SELECT 'Водозабор: '||trim(waterin)||' Котельная '||trim(heater) as heater, 'Итог норматив ' AS name_y, 2 as order_, " +
                         " 'холодное водоснабжение' AS service, " +
                         "  tarif, 0, sum(rashod), sum(sum_nach), sum(sum_money), sum(count_gil) " +
                  " FROM t_norm_imd " +
                  " WHERE nzp_serv = 6 and is_device=0 " +
                  " GROUP BY 1,2,3,4,5,6 ";
            ExecSQL(sql);

            sql = " INSERT INTO t_HVGV_71_1_6(heater, name_y, order_, service_2,  tarif_2, normativ_2, rashod_2, sum_nach_2, sum_money_2, count_gil_2) " +
                  " SELECT 'Водозабор: '||trim(waterin)||' Котельная '||trim(heater) as heater, 'Итог норматив ' AS name_y, 2 as order_, " +
                          " 'горячее водоснабжение' AS service, " +
                           " tarif, 0, sum(rashod), sum(sum_nach), sum(sum_money), sum(count_gil) " +
                  " FROM t_norm_imd " +
                  " WHERE nzp_serv = 9 and is_device=0 " +
                  " GROUP BY 1,2,3,4,5,6 ";
            ExecSQL(sql);

            #endregion

            #region Среднее значение

            sql = " INSERT INTO t_HVGV_71_1_6(heater, name_y, order_, service_1, tarif_1, normativ_1, rashod_1, sum_nach_1, sum_money_1, count_gil_1) " +
                  " SELECT 'Водозабор: '||trim(waterin)||' Котельная '||trim(heater) as heater, 'По среднему значению ' AS name_y, 3 as order_, " +
                         " 'холодное водоснабжение' AS service, " +
                           " tarif, 0, sum(rashod), sum(sum_nach), sum(sum_money), sum(count_gil) " +
                  " FROM t_norm_imd " +
                  " WHERE nzp_serv = 6 and is_device=9 " +
                  " GROUP BY 1,2,3,4,5,6 ";
            ExecSQL(sql);

            sql = " INSERT INTO t_HVGV_71_1_6(heater, name_y, order_, service_2, tarif_2, normativ_2, rashod_2, sum_nach_2, sum_money_2, count_gil_2) " +
                  " SELECT 'Водозабор: '||trim(waterin)||' Котельная '||trim(heater) as heater, 'По среднему значению ' AS name_y, 3 as order_, " +
                          " 'горячее водоснабжение' AS service, " +
                           " tarif, 0, sum(rashod), sum(sum_nach), sum(sum_money), sum(count_gil) " +
                  " FROM t_norm_imd " +
                  " WHERE nzp_serv = 9 and is_device=9" +
                  " GROUP BY 1,2,3,4,5,6 ";
            ExecSQL(sql);

            #endregion

            #region ПУ

            sql = " INSERT INTO t_HVGV_71_1_6(heater, name_y, order_, service_1, tarif_1, normativ_1, rashod_1, sum_nach_1, sum_money_1, count_gil_1) " +
                  " SELECT 'Водозабор: '||trim(waterin)||' Котельная '||trim(heater) as heater, 'По приборам учета ' AS name_y, 4 as order_, " +
                         " 'холодное водоснабжение' AS service, " +
                          " tarif, 0, sum(rashod), sum(sum_nach), sum(sum_money), sum(count_gil) " +
                  " FROM t_norm_imd " +
                  " WHERE nzp_serv = 6 and is_device=1" +
                  " GROUP BY 1,2,3,4,5,6 ";
            ExecSQL(sql);

            sql = " INSERT INTO t_HVGV_71_1_6(heater, name_y, order_, service_2, tarif_2, normativ_2, rashod_2, sum_nach_2, sum_money_2, count_gil_2) " +
                  " SELECT 'Водозабор: '||trim(waterin)||' Котельная '||trim(heater) as heater, 'По приборам учета ' AS name_y, 4 as order_, " +
                          " 'горячее водоснабжение' AS service, " +
                          " tarif, 0, sum(rashod), sum(sum_nach), sum(sum_money), sum(count_gil) " +
                  " FROM t_norm_imd " +
                  " WHERE nzp_serv = 9 and is_device=1 " +
                  " GROUP BY 1,2,3,4,5,6 ";
            ExecSQL(sql);

            #endregion

            sql = " SELECT TRIM(heater) AS heater, " +
                         " TRIM(name_y) AS name_y, " +
                         " order_, " +
                         " TRIM(service_1) AS service_1, " +
                         " tarif_1 AS tarif_1, " +
                         " normativ_1 AS normativ_1, " +
                         " SUM(rashod_1) AS rashod_1, " +
                         " SUM(sum_nach_1) AS sum_nach_1, " +
                         " SUM(sum_money_1) AS sum_money_1, " +
                         " SUM(count_gil_1) AS count_gil_1, " +
                         " TRIM(service_2) AS service_2, " +
                         " tarif_2 AS tarif_2, " +
                         " normativ_2 AS normativ_2, " +
                         " SUM(rashod_2) AS rashod_2, " +
                         " SUM(sum_nach_2) AS sum_nach_2, " +
                         " SUM(sum_money_2) AS sum_money_2, " +
                         " SUM(count_gil_2) AS count_gil_2 " +
                  " FROM t_HVGV_71_1_6 " +
                  " GROUP BY 1,2,3, t_HVGV_71_1_6.service_1, tarif_1,normativ_1, t_HVGV_71_1_6.service_2, tarif_2,normativ_2 " +
                  " ORDER BY heater, order_, name_y   ";
            DataTable dt1 = ExecSQLToTable(sql);
            dt1.TableName = "Q_master3";

            ExecSQL("DELETE FROM t_all_71_1_6 ");

            #region Норматив

            sql = " INSERT INTO t_all_71_1_6(heater, name_y, order_, order_serv, service,  tarif, normativ, rashod, sum_nach, sum_money, count_gil) " +
                  " SELECT 'Водозабор: '||trim(waterin)||' Котельная '||trim(heater) as heater, name_y, 1 as order_, 3 AS order_serv, " +
                         " 'холодную воду для нужд ГВС' AS service,  tarif,  max(normativ) as  normativ," +
                            GetServString(14, 0) +
                  " FROM t_norm_imd " +
                  " WHERE nzp_serv = 14  and is_device=0 " +
                  " GROUP BY 1,2,3,4,5,6 ";
            ExecSQL(sql);

            sql = " INSERT INTO t_all_71_1_6(heater, name_y, order_, order_serv, service,  tarif, normativ, rashod, sum_nach, sum_money, count_gil) " +
                  " SELECT 'Водозабор: '||trim(waterin)||' Котельная '||trim(heater) as heater, name_y, 1 as order_, 4 AS order_serv, " +
                         " 'водоотведение' AS service,  tarif,  max(normativ) as  normativ, " +
                            GetServString(7, 0) +
                  " FROM t_norm_imd " +
                  " WHERE nzp_serv = 7  and is_device=0 " +
                  " GROUP BY 1,2,3,4,5,6";
            ExecSQL(sql);

            sql = " INSERT INTO t_all_71_1_6(heater, name_y, order_, order_serv, service,  tarif, normativ, rashod, sum_nach, sum_money, count_gil) " +
                  " SELECT 'Водозабор: '||trim(waterin)||' Котельная '||trim(heater) as heater, name_y, 1 as order_, 5 AS order_serv, " +
                          " 'стоки' AS service,  tarif,  max(normativ) as  normativ, " +
                           GetServString(324, 0) +
                  " FROM t_norm_imd " +
                  " WHERE nzp_serv = 324  and is_device=0" +
                  " GROUP BY 1,2,3,4,5,6 ";
            ExecSQL(sql);

            sql = " INSERT INTO t_all_71_1_6(heater, name_y, order_, order_serv, service,  tarif, normativ, rashod, sum_nach, sum_money, count_gil) " +
                  " SELECT 'Водозабор: '||trim(waterin)||' Котельная '||trim(heater) as heater, name_y, 1 as order_, 6 AS order_serv, " +
                         " 'транспортировку стоков' AS service,  tarif,  max(normativ) as  normativ," +
                            GetServString(353, 0) +
                  " FROM t_norm_imd " +
                  " WHERE nzp_serv = 353  and is_device=0 " +
                  " GROUP BY 1,2,3,4,5,6";
            ExecSQL(sql);

            #endregion

            #region Норматив - Итог

            sql = " INSERT INTO t_all_71_1_6(heater, name_y, order_, order_serv, service,  tarif, normativ, rashod, sum_nach, sum_money, count_gil) " +
                  " SELECT 'Водозабор: '||trim(waterin)||' Котельная '||trim(heater) as heater, 'Итог норматив ' AS name_y, 2 as order_, 3 AS order_serv, " +
                         " 'холодную воду для нужд ГВС' AS service, " +
                           " tarif, 0," + GetServString(14, 0) +
                  " FROM t_norm_imd " +
                  " WHERE nzp_serv = 14  and is_device=0" +
                  " GROUP BY 1,2,3,4,5,6,7 ";
            ExecSQL(sql);

            sql = " INSERT INTO t_all_71_1_6(heater, name_y, order_, order_serv, service,  tarif, normativ, rashod, sum_nach, sum_money, count_gil) " +
                  " SELECT 'Водозабор: '||trim(waterin)||' Котельная '||trim(heater) as heater, 'Итог норматив ' AS name_y, 2 as order_, 4 AS order_serv, " +
                         " 'водоотведение' AS service, " +
                           " tarif, 0," + GetServString(7, 0) +
                  " FROM t_norm_imd " +
                  " WHERE nzp_serv = 7  and is_device=0" +
                  " GROUP BY 1,2,3,4,5,6,7 ";
            ExecSQL(sql);

            sql = " INSERT INTO t_all_71_1_6(heater, name_y, order_, order_serv, service,  tarif, normativ, rashod, sum_nach, sum_money, count_gil) " +
                  " SELECT 'Водозабор: '||trim(waterin)||' Котельная '||trim(heater) as heater, 'Итог норматив ' AS name_y, 2 as order_, 5 AS order_serv, " +
                          " 'стоки' AS service, " +
                           " 0, 0," + GetServString(324, 0) +
                  " FROM t_norm_imd " +
                  " WHERE nzp_serv = 324  and is_device=0" +
                  " GROUP BY 1,2,3,4,5,6,7 ";
            ExecSQL(sql);

            sql = " INSERT INTO t_all_71_1_6(heater, name_y, order_, order_serv, service,  tarif, normativ, rashod, sum_nach, sum_money, count_gil) " +
                  " SELECT 'Водозабор: '||trim(waterin)||' Котельная '||trim(heater) as heater, 'Итог норматив ' AS name_y, 2 as order_, 6 AS order_serv, " +
                         " 'транспортировку стоков' AS service, " +
                           " 0, 0," + GetServString(353, 0) +
                  " FROM t_norm_imd " +
                  " WHERE nzp_serv = 353  and is_device=0 " +
                  " GROUP BY 1,2,3,4,5,6,7 ";
            ExecSQL(sql);

            #endregion

            #region Среднее значение

            sql = " INSERT INTO t_all_71_1_6(heater, name_y, order_, order_serv, service,  tarif, normativ, rashod, sum_nach, sum_money, count_gil) " +
                  " SELECT 'Водозабор: '||trim(waterin)||' Котельная '||trim(heater) as heater, 'По среднему значению ' AS name_y, 3 as order_, 3 AS order_serv, " +
                         " 'холодную воду для нужд ГВС' AS service, " +
                           " tarif, 0," + GetServString(14, 9) +
                  " FROM t_norm_imd " +
                  " WHERE nzp_serv = 14 and is_device=9  " +
                  " GROUP BY 1,2,3,4,5,6,7 ";
            ExecSQL(sql);

            sql = " INSERT INTO t_all_71_1_6(heater, name_y, order_, order_serv, service,  tarif, normativ, rashod, sum_nach, sum_money, count_gil) " +
                  " SELECT 'Водозабор: '||trim(waterin)||' Котельная '||trim(heater) as heater, 'По среднему значению ' AS name_y, 3 as order_, 4 AS order_serv, " +
                         " 'водоотведение' AS service, " +
                           " tarif, 0," + GetServString(7, 9) +
                  " FROM t_norm_imd " +
                  " WHERE nzp_serv = 7 and is_device=9  " +
                  " GROUP BY 1,2,3,4,5,6,7 ";
            ExecSQL(sql);

            sql = " INSERT INTO t_all_71_1_6(heater, name_y, order_, order_serv, service,  tarif, normativ, rashod, sum_nach, sum_money, count_gil) " +
                  " SELECT 'Водозабор: '||trim(waterin)||' Котельная '||trim(heater) as heater, 'По среднему значению ' AS name_y, 3 as order_, 5 AS order_serv, " +
                          " 'стоки' AS service, " +
                           " 0, 0," + GetServString(324, 9) +
                  " FROM t_norm_imd " +
                  " WHERE nzp_serv = 324 and is_device=9  " +
                  " GROUP BY 1,2,3,4,5,6,7 ";
            ExecSQL(sql);

            sql = " INSERT INTO t_all_71_1_6(heater, name_y, order_, order_serv, service,  tarif, normativ, rashod, sum_nach, sum_money, count_gil) " +
                  " SELECT 'Водозабор: '||trim(waterin)||' Котельная '||trim(heater) as heater, 'По среднему значению ' AS name_y, 3 as order_, 6 AS order_serv, " +
                         " 'транспортировку стоков' AS service, " +
                           " 0, 0," + GetServString(353, 9) +
                  " FROM t_norm_imd " +
                  " WHERE nzp_serv = 353 and is_device=9  " +
                  " GROUP BY 1,2,3,4,5,6,7 ";
            ExecSQL(sql);

            #endregion

            #region ПУ

            sql = " INSERT INTO t_all_71_1_6(heater, name_y, order_, order_serv, service,  tarif, normativ, rashod, sum_nach, sum_money, count_gil) " +
                  " SELECT 'Водозабор: '||trim(waterin)||' Котельная '||trim(heater) as heater, 'По приборам учета ' AS name_y, 4 as order_, 3 AS order_serv, " +
                         " 'холодную воду для нужд ГВС' AS service, " +
                           " tarif, 0," + GetServString(14, 1) +
                  " FROM t_norm_imd " +
                  " WHERE nzp_serv = 14 and is_device=1 " +
                  " GROUP BY 1,2,3,4,5,6,7 ";
            ExecSQL(sql);

            sql = " INSERT INTO t_all_71_1_6(heater, name_y, order_, order_serv, service,  tarif, normativ, rashod, sum_nach, sum_money, count_gil) " +
                  " SELECT 'Водозабор: '||trim(waterin)||' Котельная '||trim(heater) as heater, 'По приборам учета ' AS name_y, 4 as order_, 4 AS order_serv, " +
                         " 'водоотведение' AS service, " +
                           " tarif, 0," + GetServString(7, 1) +
                  " FROM t_norm_imd " +
                  " WHERE nzp_serv = 7 and is_device=1 " +
                  " GROUP BY 1,2,3,4,5,6,7 ";
            ExecSQL(sql);

            sql = " INSERT INTO t_all_71_1_6(heater, name_y, order_, order_serv, service,  tarif, normativ, rashod, sum_nach, sum_money, count_gil) " +
                  " SELECT 'Водозабор: '||trim(waterin)||' Котельная '||trim(heater) as heater, 'По приборам учета ' AS name_y, 4 as order_, 5 AS order_serv, " +
                          " 'стоки' AS service, " +
                           " 0, 0," + GetServString(324, 1) +
                  " FROM t_norm_imd " +
                  " WHERE nzp_serv = 324 and is_device=1 " +
                  " GROUP BY 1,2,3,4,5,6,7 ";
            ExecSQL(sql);

            sql = " INSERT INTO t_all_71_1_6(heater, name_y, order_, order_serv, service,  tarif, normativ, rashod, sum_nach, sum_money, count_gil) " +
                  " SELECT 'Водозабор: '||trim(waterin)||' Котельная '||trim(heater) as heater, 'По приборам учета ' AS name_y, 4 as order_, 6 AS order_serv, " +
                         " 'транспортировку стоков' AS service, " +
                           " 0, 0," + GetServString(353, 1) +
                  " FROM t_norm_imd " +
                  " WHERE nzp_serv = 353 and is_device=1 " +
                  " GROUP BY 1,2,3,4,5,6,7 ";
            ExecSQL(sql);

            #endregion

            sql = " SELECT TRIM(heater) AS heater, " +
                         " TRIM(name_y) AS name_y, " +
                         " order_, " +
                         " order_serv, " +
                         " TRIM(service) AS service, " +
                         " tarif, " +
                         " normativ, " +
                         " rashod, " +
                         " sum_nach, " +
                         " sum_money, " +
                         " count_gil " +
                  " FROM t_all_71_1_6 " +
                  " ORDER BY order_serv, heater, order_, name_y, tarif, normativ ";

            DataTable dt3 = ExecSQLToTable(sql);
            dt3.TableName = "Q_master1";

            ExecSQL("DELETE FROM t_all_71_1_6 ");

            #region Норматив

            sql = " INSERT INTO t_all_71_1_6(heater, name_y, order_, order_serv, service, rashod, sum_nach, sum_money, count_gil) " +
                  " SELECT 'Водозабор: '||trim(waterin)||' Котельная '||trim(heater) as heater, 'Итог норматив ' AS name_y, 2 as order_, 1 AS order_serv, " +
                         " 'полив' AS service, " +
                           GetServString(200, 0) +
                  " FROM t_norm_imd " +
                  " WHERE nzp_serv = 200  and is_device=0 " +
                  " GROUP BY 1,2,3,4,5 ";
            ExecSQL(sql);

            sql = " INSERT INTO t_all_71_1_6(heater, name_y, order_, order_serv, service, rashod, sum_nach, sum_money, count_gil) " +
                  " SELECT 'Водозабор: '||trim(waterin)||' Котельная '||trim(heater) as heater, 'Итог норматив ' AS name_y, 2 as order_, 2 AS order_serv, " +
                          " 'воду для домашних животных' AS service, " +
                           GetServString(201, 0) +
                  " FROM t_norm_imd " +
                  " WHERE nzp_serv = 201  and is_device=0 " +
                  " GROUP BY 1,2,3,4,5 ";
            ExecSQL(sql);

            sql = " INSERT INTO t_all_71_1_6(heater, name_y, order_, order_serv, service, rashod, sum_nach, sum_money, count_gil) " +
                  " SELECT 'Водозабор: '||trim(waterin)||' Котельная '||trim(heater) as heater, 'Итог норматив ' AS name_y, 2 as order_, 3 AS order_serv, " +
                         " 'воду для транспорта' AS service, " +
                           GetServString(202, 0) +
                  " FROM t_norm_imd " +
                  " WHERE nzp_serv = 202  and is_device=0 " +
                  " GROUP BY 1,2,3,4,5 ";
            ExecSQL(sql);

            #endregion

            #region Среднее значение

            sql = " INSERT INTO t_all_71_1_6(heater, name_y, order_, order_serv, service, rashod, sum_nach, sum_money, count_gil) " +
                  " SELECT 'Водозабор: '||trim(waterin)||' Котельная '||trim(heater) as heater, 'По среднему значению ' AS name_y, 3 as order_, 1 AS order_serv, " +
                         " 'полив' AS service, " +
                           GetServString(200, 9) +
                  " FROM t_norm_imd " +
                  " WHERE nzp_serv = 200  and is_device=9 " +
                  " GROUP BY 1,2,3,4,5 ";
            ExecSQL(sql);

            sql = " INSERT INTO t_all_71_1_6(heater, name_y, order_, order_serv, service, rashod, sum_nach, sum_money, count_gil) " +
                  " SELECT 'Водозабор: '||trim(waterin)||' Котельная '||trim(heater) as heater, 'По среднему значению ' AS name_y, 3 as order_, 2 AS order_serv, " +
                          " 'воду для домашних животных' AS service, " +
                           GetServString(201, 9) +
                  " FROM t_norm_imd " +
                  " WHERE nzp_serv = 201 and is_device=9 " +
                  " GROUP BY 1,2,3,4,5 ";
            ExecSQL(sql);

            sql = " INSERT INTO t_all_71_1_6(heater, name_y, order_, order_serv, service, rashod, sum_nach, sum_money, count_gil) " +
                  " SELECT 'Водозабор: '||trim(waterin)||' Котельная '||trim(heater) as heater, 'По среднему значению ' AS name_y, 3 as order_, 3 AS order_serv, " +
                         " 'воду для транспорта' AS service, " +
                           GetServString(202, 9) +
                  " FROM t_norm_imd " +
                  " WHERE nzp_serv = 202 and is_device=9 " +
                  " GROUP BY 1,2,3,4,5 ";
            ExecSQL(sql);

            #endregion

            #region ПУ

            sql = " INSERT INTO t_all_71_1_6(heater, name_y, order_, order_serv, service, rashod, sum_nach, sum_money, count_gil) " +
                  " SELECT 'Водозабор: '||trim(waterin)||' Котельная '||trim(heater) as heater, 'По приборам учета ' AS name_y, 4 as order_, 1 AS order_serv, " +
                         " 'полив' AS service, " +
                           GetServString(200, 1) +
                  " FROM t_norm_imd " +
                  " WHERE nzp_serv = 200 and is_device=1 " +
                  " GROUP BY 1,2,3,4,5 ";
            ExecSQL(sql);

            sql = " INSERT INTO t_all_71_1_6(heater, name_y, order_, order_serv, service, rashod, sum_nach, sum_money, count_gil) " +
                  " SELECT 'Водозабор: '||trim(waterin)||' Котельная '||trim(heater) as heater, 'По приборам учета ' AS name_y, 4 as order_, 2 AS order_serv, " +
                          " 'воду для домашних животных' AS service, " +
                            GetServString(201, 1) +
                  " FROM t_norm_imd " +
                  " WHERE nzp_serv = 201 and is_device=1 " +
                  " GROUP BY 1,2,3,4,5 ";
            ExecSQL(sql);

            sql = " INSERT INTO t_all_71_1_6(heater, name_y, order_, order_serv, service, rashod, sum_nach, sum_money, count_gil) " +
                  " SELECT 'Водозабор: '||trim(waterin)||' Котельная '||trim(heater) as heater, 'По приборам учета ' AS name_y, 4 as order_, 3 AS order_serv, " +
                         " 'воду для транспорта' AS service, " +
                            GetServString(202, 1) +
                  " FROM t_norm_imd " +
                  " WHERE nzp_serv = 202 and is_device=1 " +
                  " GROUP BY 1,2,3,4,5 ";
            ExecSQL(sql);

            #endregion

            sql = " SELECT TRIM(heater) AS heater, " +
                         " TRIM(name_y) AS name_y, " +
                         " order_, " +
                         " order_serv, " +
                         " TRIM(service) AS service, " +
                         " rashod, " +
                         " sum_nach, " +
                         " sum_money, " +
                         " count_gil " +
                  " FROM t_all_71_1_6 " +
                  " ORDER BY order_serv, heater, order_, name_y ";

            DataTable dt2 = ExecSQLToTable(sql);
            dt2.TableName = "Q_master2";

			var ds = new DataSet();
			ds.Tables.Add(dt1);
			ds.Tables.Add(dt2);
            ds.Tables.Add(dt3);
			return ds;
		}

		/// <summary>
		/// Форматирует выходную строку для сборки запроса
		/// </summary>
		/// <param name="nzpServ">Код услуги</param>
		/// <param name="isDevice">Тип расчета 0- норматив 1- ПУ, 9 - среднее</param>
		/// <returns></returns>
		private string GetServString(int nzpServ, int isDevice) {
			return String.Format(" sum(case when nzp_serv={0} and is_device ={1} then rashod else 0 end ) as rashod{0}, " +
								 " sum(case when nzp_serv={0} and is_device ={1} then sum_nach else 0 end ) as sum_nach{0}, " +
								 " sum(case when nzp_serv={0} and is_device ={1} then sum_money else 0 end ) as sum_money{0}, " +
								 " sum(case when nzp_serv={0} and is_device ={1} then count_gil else 0 end ) as count_gil{0} ",
								 nzpServ, isDevice);

		}


		/// <summary>
		/// Получает условия ограничения по поставщику
		/// </summary>
		private string GetWhereSupp(string fieldPref)
		{
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
                          " WHERE nzp_payer IN (" + supp.TrimEnd(',')+ ") ";
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

			return " AND " + fieldPref + " in (SELECT nzp_supp " +
			                                 " FROM " + prefKernel + "supplier " +
                                             " WHERE nzp_supp > 0 " + whereSupp.TrimEnd(',') + ")";
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

		protected override void PrepareReport(FastReport.Report report) {
			string period;

			if (DatS == DatPo)
			{
				period = DatS.ToShortDateString() + " г.";
			}
			else
			{
				period = "период с " + DatS.ToShortDateString() + " г. по " + DatPo.ToShortDateString() + " г.";
			}
			report.SetParameterValue("period", period);
			report.SetParameterValue("DATE", DateTime.Now.ToShortDateString());
			report.SetParameterValue("TIME", DateTime.Now.ToLongTimeString());

			string headerParam = !string.IsNullOrEmpty(TerritoryHeader) ? "Территория: " + TerritoryHeader + "\n" : string.Empty;
            headerParam += !string.IsNullOrEmpty(AgentHeader) ? "Агент: " + AgentHeader + "\n" : string.Empty;
            headerParam += !string.IsNullOrEmpty(PrincipalHeader) ? "Принципал: " + PrincipalHeader + "\n" : string.Empty;
            headerParam += !string.IsNullOrEmpty(SupplierHeader) ? "Поставщик: " + SupplierHeader : string.Empty;
			headerParam = headerParam.TrimEnd('\n');
			report.SetParameterValue("headerParam", headerParam);
		}

		protected override void PrepareParams() {
			DateTime begin;
			DateTime end;
			var period = UserParamValues["Period"].GetValue<string>();
			PeriodParameter.GetValues(period, out begin, out end);
			DatS = begin;
			DatPo = end;
			BankSupplier = JsonConvert.DeserializeObject<BankSupplierParameterValue>(UserParamValues["BankSupplier"].Value.ToString());
		}

		protected override void CreateTempTable() {
			string sql = " CREATE TEMP TABLE t_norm_imd ( " +
						 " pref CHARACTER(15), " +
						 " nzp_serv INTEGER, " +
						 " is_device INTEGER, " +
						 " count_gil INTEGER, " +
						 " name_y CHARACTER(150), " +
						 " heater CHARACTER(20), " +
						 " waterin CHARACTER(20), " +
						 " rashod " + DBManager.sDecimalType + "(14,7), " +
						 " sum_nach " + DBManager.sDecimalType + "(14,2), " +
						 " sum_money " + DBManager.sDecimalType + "(14,2), " +
						 " tarif " + DBManager.sDecimalType + "(14,4), " +
						 " normativ " + DBManager.sDecimalType + "(14,4)) " + DBManager.sUnlogTempTable;
			ExecSQL(sql);

            sql = " CREATE TEMP TABLE t_all_71_1_6 ( " +
                    " heater CHARACTER(100), " +
                    " name_y CHARACTER(100), " +
                    " order_ INTEGER, " +
                    " order_serv INTEGER, " +
                    " service CHARACTER(30), " +
                    " tarif " + DBManager.sDecimalType + "(14,4), " +
                    " normativ " + DBManager.sDecimalType + "(14,4)," +
                    " rashod " + DBManager.sDecimalType + "(14,7), " +
                    " sum_nach " + DBManager.sDecimalType + "(14,2), " +
                    " sum_money " + DBManager.sDecimalType + "(14,2), " +
                    " count_gil INTEGER) " + DBManager.sUnlogTempTable;
            ExecSQL(sql);

            sql = " CREATE TEMP TABLE t_HVGV_71_1_6 ( " +
                    " heater CHARACTER(100), " +
                    " name_y CHARACTER(100), " +
                    " order_ INTEGER, " +
                    " service_1 CHARACTER(30), " +
                    " tarif_1 " + DBManager.sDecimalType + "(14,4), " +
                    " normativ_1 " + DBManager.sDecimalType + "(14,4)," +
                    " rashod_1 " + DBManager.sDecimalType + "(14,7), " +
                    " sum_nach_1 " + DBManager.sDecimalType + "(14,2), " +
                    " sum_money_1 " + DBManager.sDecimalType + "(14,2), " +
                    " count_gil_1 INTEGER, " +
                    " service_2 CHARACTER(30), " +
                    " tarif_2 " + DBManager.sDecimalType + "(14,4), " +
                    " normativ_2 " + DBManager.sDecimalType + "(14,4)," +
                    " rashod_2 " + DBManager.sDecimalType + "(14,7), " +
                    " sum_nach_2 " + DBManager.sDecimalType + "(14,2), " +
                    " sum_money_2 " + DBManager.sDecimalType + "(14,2), " +
                    " count_gil_2 INTEGER) " + DBManager.sUnlogTempTable;
            ExecSQL(sql);

			sql = " CREATE TEMP TABLE t_res_y_71_1_6(" +
				  " nzp_y INTEGER, " +
				  " name_y CHARACTER(250)) " + DBManager.sUnlogTempTable;
			ExecSQL(sql);

		}

		protected override void DropTempTable() {
			if (TempTableInWebCashe("t_norm_imd"))
			{
				ExecSQL(" drop table t_norm_imd ", true);
			}
            ExecSQL(" drop table t_all_71_1_6 ", true);
            ExecSQL(" drop table t_res_y_71_1_6 ", true);
            ExecSQL(" drop table t_HVGV_71_1_6 ", true);

		}
	}
}
