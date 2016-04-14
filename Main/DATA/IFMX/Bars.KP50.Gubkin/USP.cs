using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Security.Permissions;
using Globals.SOURCE.Utility;
using STCLINE.KP50;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;
using STCLINE.KP50.Utility;

namespace Bars.KP50.Gubkin
{
	public class USP : ExcelRepClient
	{
		/// <summary>Выгрузка файла обмена</summary>
		/// <returns>Результат работы функции</returns>
		public Returns GenerateExchange(out Returns ret, SupgFinder finder)
		{
			ret = Utils.InitReturns();
			if (finder.nzp_user < 1)
			{
				ret.result = false;
				ret.text = "Не определен пользователь";
				return ret;
			}

			var month = finder.adr.Length == 1 ? "0" + finder.adr : finder.adr;
			var year = finder.area;

			string fileName = "Exchange_" + DateTime.Now.Ticks + ".DBF"; //Наименование файла отчета
			string fileNameOutPut = string.Empty;//Наименование файла после сохранения

			#region запись в БД о постановки в поток

			var excelRepDb = new ExcelRep();
			ret = excelRepDb.AddMyFile(new ExcelUtility 
			{
				nzp_user = finder.nzp_user,
				status = ExcelUtility.Statuses.InProcess,
				rep_name = "Выгрузка файла обмена"
			});
			if (!ret.result) return ret;
			int nzpExc = ret.tag;

			#endregion

			IDbConnection connDB = null;
			MyDataReader reader = null;

			string dir = InputOutput.useFtp ? InputOutput.GetOutputDir() : Constants.Directories.ReportDir;
			var perm = new SecurityPermission(SecurityPermissionFlag.UnmanagedCode);

			try
			{
				if (Constants.Trace) ClassLog.WriteLog("старт try");

				connDB = DBManager.newDbConnection(Constants.cons_Kernel);
				ret = OpenDb(connDB, true);
				if (!ret.result) return ret;

				int servCount = 0;
				//определение максимального кол-ва услуг
				foreach (var bank in Points.PointList)
				{
					var charge = bank.pref + "_charge_" + year.Substring(2, 2) + tableDelimiter + "charge_" + month;
                    //для нормальной базы
                    string sqlStr = " SELECT COUNT(DISTINCT nzp_serv) AS cnt " +
                                    " FROM " + charge +
                                    " WHERE nzp_serv > 1 " +
                                      " AND dat_charge IS NULL ";

					int servMax = Convert.ToInt32(DBManager.ExecScalar(connDB,sqlStr,out ret, true));
					if (servMax > servCount) servCount = servMax;
				}

				#region Создание DBF файла

				var eDBF = new exDBF(fileName.TrimEnd('.','D','B','F'));
				eDBF.AddColumn("ID", typeof(decimal), 38, 0);
				eDBF.AddColumn("PKU", typeof(string), 11, 0);
				eDBF.AddColumn("FAMIL", typeof(string), 100, 0);
				eDBF.AddColumn("IMJA", typeof(string), 100, 0);
				eDBF.AddColumn("OTCH", typeof(string), 100, 0);
				eDBF.AddColumn("SNILS", typeof(string), 20, 0);
				eDBF.AddColumn("DROG", typeof(DateTime), 0, 0);
				eDBF.AddColumn("DATN", typeof(DateTime), 0, 0);
				eDBF.AddColumn("PRED", typeof(string), 100, 0);
				eDBF.AddColumn("FRA_REG_ID", typeof(double), 38, 5);
				eDBF.AddColumn("POSEL", typeof(string), 100, 0);
				eDBF.AddColumn("NASP", typeof(string), 100, 0);
				eDBF.AddColumn("YLIC", typeof(string), 100, 0);
				eDBF.AddColumn("NDOM", typeof(string), 100, 0);
				eDBF.AddColumn("NKORP", typeof(string), 100, 0);
				eDBF.AddColumn("NKW", typeof(string), 100, 0);
				eDBF.AddColumn("NKOMN", typeof(string), 50, 0);
				eDBF.AddColumn("ILCHET", typeof(string), 100, 0);
				eDBF.AddColumn("FAMIL_LCH", typeof(string), 100, 0);
				eDBF.AddColumn("IMJA_LCH", typeof(string), 100, 0);
				eDBF.AddColumn("OTCH_LCH", typeof(string), 100, 0);
				eDBF.AddColumn("SNILS_LCH", typeof(string), 100, 0);
				eDBF.AddColumn("DROG_LCH", typeof(DateTime), 0, 0);

				//Услуги
				for (int j = 1; j <= servCount; j++)
				{
					eDBF.AddColumn("KGKYSL_" + j, typeof(decimal), 38, 5);
					eDBF.AddColumn("GKYSL_" + j, typeof(string), 100, 0);
					eDBF.AddColumn("NGKYSL1_" + j, typeof(string), 100, 0);
					eDBF.AddColumn("NGKYSL2_" + j, typeof(string), 100, 0);
					eDBF.AddColumn("LCHET_" + j, typeof(string), 100, 0);
					eDBF.AddColumn("TARIF1_" + j, typeof(decimal), 38, 5);
					eDBF.AddColumn("TARIF2_" + j, typeof(decimal), 38, 5);
					eDBF.AddColumn("FAKT_" + j, typeof(decimal), 38, 5);
					eDBF.AddColumn("SUMTAR_" + j, typeof(decimal), 38, 5);
					eDBF.AddColumn("SUMOPL_" + j, typeof(decimal), 38, 5);
					eDBF.AddColumn("SUMLGT_" + j, typeof(decimal), 38, 5);
					eDBF.AddColumn("SUMDOLG_" + j, typeof(decimal), 38, 5);
					eDBF.AddColumn("OPLDOLG_" + j, typeof(decimal), 38, 5);
					eDBF.AddColumn("DATDOLG_" + j, typeof(DateTime), 8, 0);
					eDBF.AddColumn("KOLDOLG_" + j, typeof(decimal), 38, 5);
					eDBF.AddColumn("PRIZN_" + j, typeof(decimal), 38, 5);
					eDBF.AddColumn("KOLLGTP_" + j, typeof(decimal), 38, 5);
					eDBF.AddColumn("KOLLGT_" + j, typeof(decimal), 38, 5);
					eDBF.AddColumn("KOLZR_" + j, typeof(decimal), 38, 5);
				}

				perm.Assert();
				eDBF.Save(dir, 866);
				if (Constants.Trace) ClassLog.WriteLog("Создание файла");
				string strFilePath = Path.GetFullPath(String.Format("{0}\\{1}", dir, fileName));

				#endregion

				var servList = new List<int>();
				int count = 0;

				#region Выборка и запись данных

				foreach (var bank in Points.PointList)
				{
					string prefData = bank.pref + DBManager.sDataAliasRest,
							prefKernel = bank.pref + DBManager.sKernelAliasRest;
					string first =
#if !PG
						" first 1000 "
#else
						string.Empty
#endif
						,
						monthSQL =
#if PG
							" EXTRACT (MONTH FROM kt.dat_ofor) = " + month
#else
							" MONTH(kt.dat_ofor) = " + month 
#endif
						;
				   const string limit =
#if PG
							" LIMIT 1000 " 
#else
							string.Empty
#endif
						;

					#region выборка основной информации о лицевых счетах

				    ret = ExecSQL(connDB, " DROP TABLE t_usp_kart_" + finder.nzp_user);

                    string sqlStr = " SELECT kt.nzp_kvar, kt.nzp_gil, kt.fam, kt.ima, kt.otch, kt.dat_rog " +
                                        " INTO TEMP t_usp_kart_" + finder.nzp_user +
                                    " FROM " + prefData + "kart kt " +
                                    " WHERE kt.nzp_tkrt = 1 " +
                                      " AND kt.isactual = '1' " +
                                      " AND " + monthSQL;
                    ret = ExecSQL(connDB, sqlStr);
                    ret = ExecSQL(connDB, "CREATE INDEX ix_t_usp_kart_" + finder.nzp_user + "_1 ON t_usp_kart_" + finder.nzp_user + "(nzp_kvar)");

					sqlStr = " SELECT " + first +
									   " DISTINCT kt.nzp_gil AS ID, " +
									   " kt.fam AS FAMIL, " +
									   " kt.ima AS IMJA, " +
									   " kt.otch AS OTCH, " +
									   " kt.dat_rog AS DROG, " +
									   " town.town AS POSEl, " +
									   " ulica.ulica AS YLIC, " +
									   " dom.ndom AS NDOM, " +
									   " dom.nkor AS NKORP, " +
									   " kv.nkvar AS NKW, " +
									   " kt.dat_rog AS DROG_LCH, " +
									   " sup.name_supp AS PRED, " +
									   " kv.nzp_kvar AS FRA_reg_id, " +
									   " (CASE WHEN dom.nzp_raj <> -1 THEN raj.rajon ELSE '' END) AS NASP, " +
									   " kt.fam AS FAMIL_LCH, " +
									   " kt.ima AS IMJA_LCH, " +
									   " kt.otch AS OTCH_LCH " +
                                " FROM t_usp_kart_" + finder.nzp_user + " kt INNER JOIN " + prefData + "kvar kv ON kv.nzp_kvar = kt.nzp_kvar " +
                                                                           " INNER JOIN " + prefData + "dom dom ON dom.nzp_dom = kv.nzp_dom " +
                                                                           " INNER JOIN " + prefData + "s_ulica ulica ON ulica.nzp_ul = dom.nzp_ul " +
					                                                       " INNER JOIN " + prefData + "s_rajon raj ON raj.nzp_raj = ulica.nzp_raj " +
					                                                       " INNER JOIN " + prefData + "s_town town ON town.nzp_town = raj.nzp_town " +
					                                                       " INNER JOIN " + prefKernel + "supplier sup ON sup.nzp_supp = 1 " + limit;

					DataTable tmpHeaderTable = ClassDBUtils.OpenSQL(sqlStr, connDB).resultData;
					if (Constants.Trace) ClassLog.WriteLog("первый селектор: основыне данные лицевого счета");

					#endregion

					foreach (DataRow tmpHeader in tmpHeaderTable.Rows)
					{
						string centralData = Points.Pref + DBManager.sDataAliasRest,
								centralKernel = Points.Pref + DBManager.sKernelAliasRest;

						#region выборка информации об услугах

						sqlStr = " SELECT DISTINCT p.point AS GKYSL, " +
										" srv.service_name AS NGKYSL1, " +
										" service_small AS NGKYSL2, " +
										" ch.num_ls AS LCHET, " +
										" MAX(ch.tarif) AS TARIF1, " +
										" MAX(ch.tarif_f) AS TARIF2, " +
										" MAX(ch.sum_tarif) AS SUMTAR, " +
										" MAX(ch.c_calc) AS FAKT, " +
										" MAX(ch.sum_money) AS SUMOPL, " +
										" MAX(ch.rsum_lgota) AS SUMLGT, " +
										" ch.nzp_serv, " +
										" MAX((" + centralData + "get_kol_gil('01." + month + "." + year + "','" +
													DateTime.DaysInMonth(Convert.ToInt32(year), Convert.ToInt32(month)) +
												"." + month + "." + year + "', 15, " + tmpHeader["fra_reg_id"] + "))) as KOLZR, " +
										" MAX(ch.sum_insaldo) AS SUMDOLG, " +
										" MAX(ch.sum_money) AS OPLDOLG " +
								 " FROM " + bank.pref + "_charge_" + year.Substring(2, 2) + tableDelimiter + "charge_" + month + " ch, " +
											centralKernel + "s_point p, " + 
											centralKernel + "services srv " +
								 " WHERE nzp_kvar = " + tmpHeader["fra_reg_id"] + 
								   " AND p.bd_kernel = '" + bank.pref + "' " +
								   " AND srv.nzp_serv = ch.nzp_serv " +
								   " AND ch.nzp_serv <> 1 " +
								   " AND dat_charge IS NULL " +
								 " GROUP BY 1, 2, 3, 4, ch.nzp_serv ";
						DataTable tmpServTable = ClassDBUtils.OpenSQL(sqlStr, connDB).resultData;
						if (Constants.Trace) ClassLog.WriteLog("второй селектор: основыне данные начислений лицевого счета");

						//Заполнения списка с номерами услуг
						if (servList.Count == 0)
						{
							foreach (DataRow row in tmpServTable.Rows)
							{
								int nzpServ = row["nzp_serv"] != DBNull.Value ? Convert.ToInt32(row["nzp_serv"]) : 0;
								if (!servList.Contains(nzpServ))
								{
									servList.Add(nzpServ);
								}
							}
						}

						#endregion

						int yearInt = int.TryParse(year, out yearInt) ? yearInt : 0,
							monthInt = int.TryParse(finder.adr, out monthInt) ? monthInt : 0;
						DataRow rowDBF = eDBF.DataTable.NewRow();

						#region сохранение основных данных о лицевом счете в dbf-строку

						rowDBF["ID"] = tmpHeader["ID"] != DBNull.Value ? (Convert.ToDecimal(tmpHeader["ID"])) : 0;
						rowDBF["PKU"] = "0";
						rowDBF["FAMIL"] = tmpHeader["FAMIL"] != DBNull.Value ? tmpHeader["FAMIL"].ToString().Trim() : string.Empty;
						rowDBF["IMJA"] = tmpHeader["IMJA"] != DBNull.Value ? tmpHeader["IMJA"].ToString().Trim() : string.Empty;
						rowDBF["OTCH"] = tmpHeader["OTCH"] != DBNull.Value ? tmpHeader["OTCH"].ToString().Trim() : string.Empty;
						rowDBF["SNILS"] = "0";
						rowDBF["DROG"] = tmpHeader["DROG"] != DBNull.Value ? Convert.ToDateTime(tmpHeader["DROG"]) : new DateTime();
						rowDBF["DATN"] = yearInt == 0 || monthInt == 0 ? new DateTime() : new DateTime(yearInt, monthInt, 1);
						rowDBF["PRED"] = tmpHeader["PRED"] != DBNull.Value ? tmpHeader["PRED"].ToString().Trim() : string.Empty;
						rowDBF["FRA_REG_ID"] = tmpHeader["FRA_REG_ID"] != DBNull.Value ? Convert.ToDecimal(tmpHeader["FRA_REG_ID"]) : 0;
						rowDBF["POSEL"] = tmpHeader["POSEL"] != DBNull.Value ? tmpHeader["POSEL"].ToString().Trim() : string.Empty;
						rowDBF["NASP"] = tmpHeader["NASP"] != DBNull.Value ? tmpHeader["NASP"].ToString().Trim() : string.Empty;
						rowDBF["YLIC"] = tmpHeader["YLIC"] != DBNull.Value ? tmpHeader["YLIC"].ToString().Trim() : string.Empty;
						rowDBF["NDOM"] = tmpHeader["NDOM"] != DBNull.Value ? tmpHeader["NDOM"].ToString().Trim() : string.Empty;
						rowDBF["NKORP"] = tmpHeader["NKORP"] != DBNull.Value ? tmpHeader["NKORP"].ToString().Trim() : string.Empty;
						rowDBF["NKW"] = tmpHeader["NKW"] != DBNull.Value ? tmpHeader["NKW"].ToString().Trim() : string.Empty;
						rowDBF["NKOMN"] = "1";
						rowDBF["ILCHET"] = " ";
						rowDBF["FAMIL_LCH"] = tmpHeader["FAMIL_LCH"] != DBNull.Value ? tmpHeader["FAMIL_LCH"].ToString().Trim() : string.Empty;
						rowDBF["IMJA_LCH"] = tmpHeader["IMJA_LCH"] != DBNull.Value ? tmpHeader["IMJA_LCH"].ToString().Trim() : string.Empty;
						rowDBF["OTCH_LCH"] = tmpHeader["OTCH_LCH"] != DBNull.Value ? tmpHeader["OTCH_LCH"].ToString().Trim() : string.Empty;
						rowDBF["SNILS_LCH"] = " ";
						rowDBF["DROG_LCH"] = tmpHeader["DROG_LCH"] != DBNull.Value ? Convert.ToDateTime(tmpHeader["DROG_LCH"]) : new DateTime();

						#endregion

						#region сохранение информации о начисления лицевого счета в dbf-строку

						foreach (DataRow tmpServ in tmpServTable.Rows)
						{
							var index = servList.IndexOf(Convert.ToInt32(tmpServ["nzp_serv"]));
							if (index == -1) continue;
							index++;//прибавливаем единицу, так как списсок начинается с элемента 0
							decimal tarif1 = tmpServ["TARIF1"] != DBNull.Value ? (Convert.ToDecimal(tmpServ["TARIF1"])) : 0,
									sumtar = tmpServ["SUMTAR"] != DBNull.Value ? (Convert.ToDecimal(tmpServ["SUMTAR"])) : 0;

							rowDBF["KGKYSL_" + index] = 0;
							rowDBF["GKYSL_" + index] = tmpServ["GKYSL"] != DBNull.Value ? tmpServ["GKYSL"].ToString().Trim() : string.Empty;
							rowDBF["NGKYSL1_" + index] = tmpServ["NGKYSL1"] != DBNull.Value ? tmpServ["NGKYSL1"].ToString().Trim() : string.Empty;
							rowDBF["NGKYSL2_" + index] = tmpServ["NGKYSL2"] != DBNull.Value ? tmpServ["NGKYSL2"].ToString().Trim() : string.Empty;
							rowDBF["LCHET_" + index] = tmpServ["LCHET"] != DBNull.Value ? tmpServ["LCHET"].ToString().Trim() : string.Empty;
							rowDBF["TARIF1_" + index] = tarif1;
							rowDBF["TARIF2_" + index] = tmpServ["TARIF2"] != DBNull.Value ? (Convert.ToDecimal(tmpServ["TARIF2"])) : 0;
							rowDBF["FAKT_" + index] = tarif1 != 0 ? sumtar / tarif1 : 0;
							rowDBF["SUMTAR_" + index] = sumtar;
							rowDBF["SUMOPL_" + index] = tmpServ["SUMOPL"] != DBNull.Value ? (Convert.ToDecimal(tmpServ["SUMOPL"])) : 0;
							rowDBF["SUMLGT_" + index] = tmpServ["SUMLGT"] != DBNull.Value ? (Convert.ToDecimal(tmpServ["SUMLGT"])) : 0;
							rowDBF["SUMDOLG_" + index] = tmpServ["SUMDOLG"] != DBNull.Value ? (Convert.ToDecimal(tmpServ["SUMDOLG"])) : 0;
							rowDBF["OPLDOLG_" + index] = tmpServ["OPLDOLG"] != DBNull.Value ? (Convert.ToDecimal(tmpServ["OPLDOLG"])) : 0;
							rowDBF["DATDOLG_" + index] = new DateTime();
							rowDBF["KOLDOLG_" + index] = 0;
							rowDBF["PRIZN_" + index] = 0;
							rowDBF["KOLLGTP_" + index] = 0;
							rowDBF["KOLLGT_" + index] = 0;
							rowDBF["KOLZR_" + index] = tmpServ["KOLZR"] != DBNull.Value ? (Convert.ToDecimal(tmpServ["KOLZR"])) : 0;
						}

						#endregion

						count++;
						eDBF.DataTable.Rows.Add(rowDBF);//сохранение в dbf-таблицу
						if (count % 100 == 0)
						{
							eDBF.Append(strFilePath);//сохранение в dbf-файл
							eDBF.DataTable.Rows.Clear();
							if (Constants.Trace) ClassLog.WriteLog("запись файла");
						}
					}
				}
				if (eDBF.DataTable.Rows.Count > 0)
				{
					if (Constants.Trace) ClassLog.WriteLog("перед концом записи файла");
					eDBF.Append(strFilePath);
					if (Constants.Trace) ClassLog.WriteLog("конец записи файла");
				}
				fileNameOutPut = strFilePath.TrimEnd('.', 'D', 'B', 'F') + ".zip";
				if (!Archive.GetInstance().Compress(fileNameOutPut, new[] { strFilePath }, true))
					throw new Exception("Ошибка архивации файла");

				//сохранение на ftp
				if (InputOutput.useFtp) fileNameOutPut = InputOutput.SaveInputFile(fileNameOutPut);

				#endregion
			}
			catch (Exception ex)
			{
				ret.result = false;
				ret.text = ex.Message;
				MonitorLog.WriteLog("Ошибка в функции GenerateExchange:\n" + ex.Message, MonitorLog.typelog.Error, true);
			}
			finally
			{
				if (ret.result)
				{
					excelRepDb.SetMyFileProgress(new ExcelUtility { nzp_exc = nzpExc, progress = 1 });
					excelRepDb.SetMyFileState(new ExcelUtility
					{
						nzp_exc = nzpExc, 
						status = ExcelUtility.Statuses.Success,
						exc_path = fileNameOutPut
					});
				}
				else
				{
					excelRepDb.SetMyFileState(new ExcelUtility { nzp_exc = nzpExc, status = ExcelUtility.Statuses.Failed });
				}
				excelRepDb.Close();

				perm.PermitOnly();
				if (reader != null) reader.Close();
				if (connDB != null) connDB.Close();
			}
			if (Constants.Trace) ClassLog.WriteLog("функция завершилась");
			return ret;
		}
	}
}
