using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using STCLINE.KP50.Global;
using FastReport;
using System.IO;
using System.Data.OleDb;
using SevenZip;
using STCLINE.KP50.Interfaces;
using Globals.SOURCE.Utility;
using System.Security.Permissions;
using STCLINE.KP50.Utility;
using Bars.KP50.Utils;

namespace STCLINE.KP50.DataBase
{
	//Класс для получения данных из генератора отчетов
	public partial class ExcelRep : ExcelRepClient
	{
		/// <summary>
		/// Выгрузка оплат МУРЦ
		/// </summary>
		/// <returns></returns>
		public Returns GenerateMURCVigr(out Returns ret, SupgFinder finder) {
			ret = Utils.InitReturns();

			var month = finder.adr;
			if (finder.adr.Length == 1)
				month = "0" + finder.adr;
			var year = finder.area;

			if (finder.nzp_user < 1)
			{
				ret.result = false;
				ret.text = "Не определен пользователь";
				ret.result = false;
				return ret;
			}



			string fn2 = "";
			//путь, по которому скачивается файл
			string path = "";
			//Имя файла отчета
			string fileNameIn = "MURC_vigr_" + DateTime.Now.Ticks;
			ExcelRep excelRepDb = new ExcelRep();
			StringBuilder sql = new StringBuilder();

			//запись в БД о постановки в поток(статус 0)
			ret = excelRepDb.AddMyFile(new ExcelUtility()
			{
				nzp_user = finder.nzp_user,
				status = ExcelUtility.Statuses.InProcess,
				rep_name = "Выгрузка оплат МУРЦ"
			});
			if (!ret.result) return ret;

			int nzpExc = ret.tag;

			IDbConnection conn_db = null;
			MyDataReader reader = null;
			decimal progress = 0;

			var dir = "FilesExchange\\files\\";
			if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
			var resDir = STCLINE.KP50.Global.Constants.ExcelDir.Replace("/", "\\");
			if (!Directory.Exists(resDir)) Directory.CreateDirectory(resDir);
			var fullPath = AppDomain.CurrentDomain.BaseDirectory;

			OleDbCommand Command = new OleDbCommand();
			OleDbConnection Connection = new OleDbConnection();

			try
			{
				#region подключение к БД
				IDbConnection conn_web = DBManager.newDbConnection(Constants.cons_Webdata);
				ret = OpenDb(conn_web, true);
				if (!ret.result)
				{
					return ret;
				}

				conn_db = DBManager.newDbConnection(Constants.cons_Kernel);
				ret = OpenDb(conn_db, true);
				if (!ret.result)
				{
					return ret;
				}
				#endregion

				#region создание и заполнение mdb
				if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
				//создание дубликата пустого mdb файла
				File.Copy("template/blank_table.mdb", dir + "Оплата Мурц.mdb");

				var myConnectionString = @"Provider=Microsoft.ACE.OLEDB.12.0;" +
						   "Data Source=" + dir + "Оплата МУРЦ.mdb;Jet OLEDB:Database Password=password;";
				Connection.ConnectionString = myConnectionString;
				Connection.Open();

				DataTable resTable = new DataTable();
				string strComm = "";

				//Заголовок
				strComm = "CREATE TABLE [Оплата МУРЦ] ([Дата расчета] DATETIME,[Ведомость] TEXT(20), [Номер оплаты] DOUBLE, [Дата оплаты] DATETIME, [Вид] INTEGER, [Участок] INTEGER, " +
						  " [Улица] INTEGER, [Дом] TEXT(50), [Квартира] INTEGER, [Номер] INTEGER, [Сумма1] DOUBLE, [Сумма2] DOUBLE, [Сумма3] DOUBLE, [Сумма4] DOUBLE, " +
						  " [Сумма5] DOUBLE, [Сумма6] DOUBLE, [Сумма7] DOUBLE, [Оператор] TEXT(100), [Дата ввода] DATETIME )";
				Command = new OleDbCommand(strComm, Connection);
				Command.ExecuteNonQuery();
				Command.Dispose();


				string sqlStr = " select pl.dat_month as dat_month, pl.nzp_pack as nzp_pack, pl.nzp_pack_ls as nzp_pack_ls, pl.dat_vvod as dat_vvod, pl.g_sum_ls as g_sum_ls, pl.num_ls as num_ls, p.num_pack as num_pack from "
					+ Points.Pref + "_fin_" + finder.year.Substring(2, 2) + tableDelimiter + "pack_ls pl, " + Points.Pref + "_fin_" + finder.year.Substring(2, 2) + tableDelimiter + "pack p where pl.nzp_pack = p.nzp_pack";
				resTable = ClassDBUtils.OpenSQL(sqlStr, conn_db).resultData;

				foreach (DataRow row in resTable.Rows)
				{
					#region получение значений из БД
					//Получаем номер квартиры

					string sql1 = "select nkvar from " + Points.Pref + "_data" + tableDelimiter + "file_kvar where id = " + row["num_ls"];
					var dt = ClassDBUtils.OpenSQL(sql1, conn_db);
					int nkvar;
					if (dt.resultData.Rows.Count > 0) nkvar = Convert.ToInt32(dt.resultData.Rows[0]["nkvar"].ToString().Trim());
					else nkvar = 0;
					//получаем сегодняшнюю дату и время
					DateTime thisDay = DateTime.Now;
					//получаем дом
					sql1 = "select a.ulica, a.ndom from " + Points.Pref + "_data" + tableDelimiter + "file_dom a, " + Points.Pref + "_data" + tableDelimiter + "file_kvar b " +
						"where a.id = b.dom_id and b.id =" + row["num_ls"];
					dt = ClassDBUtils.OpenSQL(sql1, conn_db);
					string dom;
					if (dt.resultData.Rows.Count > 0) dom = Convert.ToString(dt.resultData.Rows[0]["ndom"].ToString().Trim());
					else dom = "0";
					//получаем улицу
					string ulica_str;
					int ulica;
					if (dt.resultData.Rows.Count > 0)
					{
						ulica_str = Convert.ToString(dt.resultData.Rows[0]["ulica"].ToString().Trim());
						string sql2 = "select file_ulica_id from " + Points.Pref + "_data" + tableDelimiter + "file_ulica where File_ulica_street = '" + ulica_str + "'";
						dt = ClassDBUtils.OpenSQL(sql2, conn_db);
						if (dt.resultData.Rows.Count > 0) ulica = Convert.ToInt32(dt.resultData.Rows[0]["file_ulica_id"]);
						else ulica = 0;
					}
					else ulica = 0;
					//вид
					int vid = 3;
					//получаем номер участка
					sql1 = "select uch from " + Points.Pref + "_data" + tableDelimiter + "kvar k, " + Points.Pref + "_data" + tableDelimiter + "dom d where k.nkvar = '" + nkvar + "' and k.nzp_dom = d.nzp_dom and d.ndom = '"
						+ dom + "' and k.num_ls =" + row["num_ls"];
					dt = ClassDBUtils.OpenSQL(sql1, conn_db);
					int uch;
					if (dt.resultData.Rows.Count > 0 && dt.resultData.Rows[0]["uch"] != DBNull.Value) uch = Convert.ToInt32(dt.resultData.Rows[0]["uch"]);
					else uch = 0;
					//номер
					int num;
					num = 1;
					//получаем оператора
					string oper;
					sql1 = "select login from web" + Points.Pref.Substring(1, 3) + tableDelimiter + "users where nzp_user = " + finder.nzp_user;
					dt = ClassDBUtils.OpenSQL(sql1, conn_web);
					if (dt.resultData.Rows.Count > 0)
					{
						sql1 = "select comment from " + Points.Pref + "_data" + tableDelimiter + "users where name = '" + Convert.ToString(dt.resultData.Rows[0]["login"]) + "'";
						dt = ClassDBUtils.OpenSQL(sql1, conn_db);
						if (dt.resultData.Rows.Count > 0) oper = Convert.ToString(dt.resultData.Rows[0]["comment"]);
						else oper = "do not know";
					}
					else oper = "do not know";
					//DbWorkUser db = new DbWorkUser();
					//int localUSer = db.GetLocalUser(conn_web, finder, out ret);


					#endregion

					strComm = "insert into [Оплата МУРЦ] ([Дата расчета], [Ведомость],[Номер оплаты], [Дата оплаты],[Вид],[Участок],[Улица]," +
						" [Дом],[Квартира],[Номер],[Сумма1], [Сумма2],[Сумма3],[Сумма4],[Сумма5],[Сумма6],[Сумма7],[Оператор],[Дата ввода]) values " +
							  " ('" + row["dat_month"] + "','" + row["num_pack"] + "'," + row["nzp_pack_ls"] + ",'" + row["dat_vvod"] +
							  "'," + vid + "," + uch + "," + ulica + ",'" + dom + "'," + nkvar + "," + num + ", 0 ," + row["g_sum_ls"] + ", 0 , 0 , 0 , 0 , 0 ,'" + oper + "','" + thisDay + "') ";
					OleDbCommand cmd_insert = new OleDbCommand(strComm, Connection);
					cmd_insert.ExecuteNonQuery();
					cmd_insert.Dispose();
				}
				progress += 20 / Points.PointList.Count;
				excelRepDb.SetMyFileProgress(new ExcelUtility() { nzp_exc = nzpExc, progress = progress / 100 });

				#endregion

				conn_web.Close();

				#region архивация
				SevenZipCompressor file = new SevenZipCompressor();
				file.EncryptHeaders = true;
				file.CompressionMethod = SevenZip.CompressionMethod.BZip2;
				file.DefaultItemName = fileNameIn;
				file.CompressionLevel = SevenZip.CompressionLevel.Normal;

				file.CompressFiles(fullPath + dir + fileNameIn + ".7z", fullPath + dir + "Оплата МУРЦ.mdb");
				#endregion

				//перенос файла на клиент
				File.Copy(dir + fileNameIn + ".7z", resDir + fileNameIn + ".7z");
				if (InputOutput.useFtp) fn2 = InputOutput.SaveInputFile(fullPath + dir + fileNameIn + ".7z");
			}
			catch (Exception ex)
			{
				ret.result = false;
				ret.text = ex.Message;
				MonitorLog.WriteLog("Ошибка в функции GenerateMURCVigr:\n" + ex.Message, MonitorLog.typelog.Error, true);
			}
			finally
			{
				Connection.Close();
				Connection.Dispose();

				File.Delete(dir + "Оплата Мурц.mdb");
				File.Delete(dir + fileNameIn + ".7z");
				if (ret.result)
				{

					excelRepDb.SetMyFileProgress(new ExcelUtility() { nzp_exc = nzpExc, progress = 1 });
					excelRepDb.SetMyFileState(new ExcelUtility() { nzp_exc = nzpExc, status = ExcelUtility.Statuses.Success, exc_path = InputOutput.useFtp ? fn2 : path + fileNameIn + ".7z" });
				}
				else
				{
					excelRepDb.SetMyFileState(new ExcelUtility() { nzp_exc = nzpExc, status = ExcelUtility.Statuses.Failed });
				}
				excelRepDb.Close();

				if (reader != null) reader.Close();
				if (conn_db != null) conn_db.Close();
			}

			ret.text = "Файл успешно загружен";
			return ret;
		}


		//Смена УК для выбранного списка домов
		public Returns ChangeArea(FinderChangeArea finder) {
			Returns ret;
			ExcelRep excelRepDb = new ExcelRep();
			if (finder.nzp_user < 0)
			{
				ret = new Returns(false, "Не задан пользователь", -1);
				return ret;
			}

			IDbConnection conn_db = null;
			IDbConnection conn_web = null;
			IDataReader reader = null;
			StringBuilder sql = new StringBuilder();
			string temp_table = "spis_dom_port_" + finder.nzp_user;
			List<int> unports = new List<int>(); //список не перенесенных домов
			var list_houses = new List<int>(); //список домов
			var list_reasons = new Dictionary<int, string>();
			string result = "";
			string result_ls = "";
			var nzp_dom = 0;
			var sql2 = "";
			//запись в БД о постановки в поток(статус 0)
			ret = excelRepDb.AddMyFile(new ExcelUtility()
			{
				nzp_user = finder.nzp_user,
				status = ExcelUtility.Statuses.InProcess,
				rep_name = "Перенос лицевых счетов в новую УК:" + finder.new_area.area.Trim()
			});
			if (!ret.result) return ret;

			int nzpExc = ret.tag;

			string path = "";
			//Имя файла отчета
			string fileName = "";
			try
			{

				#region соединение с БД
				conn_web = GetConnection(Constants.cons_Webdata);
				ret = OpenDb(conn_web, true);
				if (!ret.result) return ret;


				conn_db = GetConnection(Constants.cons_Kernel);
				ret = OpenDb(conn_db, true);
				if (!ret.result) return ret;
				#endregion



#if PG
				string tXX_spdom = "t" + finder.nzp_user + "_spdom";
				string tXX_spdom_full = sDefaultSchema + tXX_spdom;
#else
				string tXX_spdom = "t" + finder.nzp_user + "_spdom";
				string tXX_spdom_full = conn_web.Database + "@" + DBManager.getServer(conn_db) + ":" + tXX_spdom;
#endif
				string dat_s = "'" + new DateTime(Points.CalcMonth.year_, Points.CalcMonth.month_, 1).ToShortDateString() + "'"; //начало  тек.расчетного месяца
				string dat_po = "'" + new DateTime(Points.CalcMonth.year_, Points.CalcMonth.month_, 1).AddDays(-1).ToShortDateString() + "'"; //конец пред.расчетного месяца
				int updating_dom = 0; //кол-во переносимых домов
				int updated_dom = 0; //кол-во успешно перенесенных домов


				ExecSQL(conn_db, "drop table  " + temp_table + ";", false);
				sql.Remove(0, sql.Length);
#if PG
				sql.Append(" select * into temp " + temp_table + sUnlogTempTable + " from " + tXX_spdom_full + " where mark=1 ");
#else
				sql.Append(" select * from " + tXX_spdom_full + " where mark=1 into temp " + temp_table + sUnlogTempTable);
#endif
				ret = ExecSQL(conn_db, sql.ToString(), true);
				if (!ret.result)
				{
					MonitorLog.WriteLog("ChangeArea: Ошибка переноса списка домов" + sql.ToString(), MonitorLog.typelog.Error, true);
					ret.result = false;
				}

				sql.Remove(0, sql.Length);
#if PG
				sql.Append(" analyze " + temp_table + "");
#else
				sql.Append(" update statistics for table " + temp_table + "");
#endif
				ret = ExecSQL(conn_db, sql.ToString(), true);
				if (!ret.result)
				{
					MonitorLog.WriteLog("ChangeArea: Ошибка переноса списка домов" + sql.ToString(), MonitorLog.typelog.Error, true);
					ret.result = false;
				}

				sql.Remove(0, sql.Length);
				sql.Append(" select  *  from " + temp_table + " ");
				var spis_dom = ClassDBUtils.OpenSQL(sql.ToString(), conn_db);
				if (spis_dom.resultCode == -1)
				{
					conn_db.Close();
					conn_web.Close();
					MonitorLog.WriteLog("ChangeArea: Ошибка получения списка домов для переноса в новую УК, sql: " + sql.ToString(), MonitorLog.typelog.Error, true);
					ret.result = false;
					ret.text = "Ошибка получения списка домов для переноса в новую УК";
					return ret;
				}
				updating_dom = spis_dom.resultData.Rows.Count;

				int j = 0;
				//цикл по домам
				for (int i = 0; i < spis_dom.resultData.Rows.Count; i++)
				{
					//начало транзакции
					IDbTransaction transaction = null;// conn_db.BeginTransaction();
					try
					{
						finder.nzp_dom = CastValue<int>(spis_dom.resultData.Rows[i]["nzp_dom"]);
						finder.pref = CastValue<string>(spis_dom.resultData.Rows[i]["pref"]);
						list_houses.Add(finder.nzp_dom);
						nzp_dom = finder.nzp_dom;
						sql2 = "select count(*) from " + finder.pref +
						sDataAliasRest + "s_area where nzp_area=" + finder.new_area.nzp_area + ";";
						var count = CastValue<int>(ExecScalar(conn_db, transaction, sql2, out ret, true));
						if (count < 1)
						{
							MonitorLog.WriteLog("ChangeArea: УК отсутствует в локальном банке: " + Points.PointList.Where(x => x.pref == finder.pref).FirstOrDefault().point
								, MonitorLog.typelog.Error, true);
							unports.Add(finder.nzp_dom);
							//transaction.Rollback();
							list_reasons.Add(finder.nzp_dom, " УК отсутствует в локальном банке: " + Points.PointList.Where(x => x.pref == finder.pref).FirstOrDefault().point);
							continue;
						}
						sql2 = "select count(*) from " + finder.pref +
								  sDataAliasRest + "s_geu where nzp_geu=" + finder.new_geu.nzp_geu + ";";
						count = CastValue<int>(ExecScalar(conn_db, transaction, sql2, out ret, true));
						if (count < 1)
						{
							MonitorLog.WriteLog("ChangeArea: ЖЭУ отсутствует в локальном банке: " + Points.PointList.Where(x => x.pref == finder.pref).FirstOrDefault().point
								, MonitorLog.typelog.Error, true);
							unports.Add(finder.nzp_dom);
							// transaction.Rollback();
							list_reasons.Add(finder.nzp_dom, " ЖЭУ отсутствует в локальном банке: " + Points.PointList.Where(x => x.pref == finder.pref).FirstOrDefault().point);
							continue;
						}

						ret = ChangeAreaForDom(finder, conn_db, conn_web, transaction);
						if (ret.result)
						{
							// transaction.Rollback(); - тестовый режим
							//transaction.Commit(); // - рабочий режим
							updated_dom++;
						}
						else
						{
							unports.Add(finder.nzp_dom);
							MonitorLog.WriteLog("ChangeArea: Дом c nzp_dom=" + finder.nzp_dom + " не был перенесен ", MonitorLog.typelog.Error, true);
							list_reasons.Add(finder.nzp_dom, ret.text);
							//transaction.Rollback();
						}
						j++;
						excelRepDb.SetMyFileProgress(new ExcelUtility() { nzp_exc = nzpExc, progress = ((decimal) j) / spis_dom.resultData.Rows.Count });
					}
					catch (Exception ex)
					{

						// transaction.Rollback();
						ret.result = false;
						ret.text = ex.Message;
						MonitorLog.WriteLog("Ошибка в функции ChangeArea:\n" + ex.Message, MonitorLog.typelog.Error, true);
						list_reasons.Add(nzp_dom, ret.text);
						break;
					}

				}
				var count_unported_ls = 0;
				var count_all_ls = 0;
				if (unports.Count > 0)
				{
					sql2 = "select count(*) from " + Points.Pref +
							 sDataAliasRest + "kvar where nzp_dom in (" + string.Join(",", unports) + ") and is_open='" + (int) Ls.States.Open + "';";
					count_unported_ls = CastValue<int>(ExecScalar(conn_db, sql2, out ret, true));

				}
				if (list_houses.Count > 0)
				{
					sql2 = "select count(*) from " + Points.Pref +
						   sDataAliasRest + "kvar where nzp_dom in (" + string.Join(",", list_houses) +
						   ") and is_open='" + (int) Ls.States.Open + "';";
					count_all_ls = CastValue<int>(ExecScalar(conn_db, sql2, out ret, true));
				}
				result_ls = "Перенесено " + (count_all_ls - count_unported_ls) + " из " + count_all_ls + " лицевых счетов";
				result = updated_dom + " из " + updating_dom + " домов были перенесены в УК: " + finder.new_area.area.Trim() + ", ЖЭУ: " + finder.new_geu.geu.Trim() + "";

			}
			catch (Exception ex)
			{
				ret.result = false;
				ret.text = ex.Message;
				MonitorLog.WriteLog("Ошибка в функции ChangeArea:\n" + ex.Message, MonitorLog.typelog.Error, true);
			}

			#region Заполнение параметров и списка домов
			DataTable DT = new DataTable();
			DT.TableName = "Ported";

			DataTable DT1 = new DataTable();
			DT1.TableName = "Unported";
			string listOldArea = "";
			string listOldGeu = "";

			try
			{

				string sp_dom = ""; //не перенесенные дома

				for (int i = 0; i < unports.Count; i++)
				{
					sp_dom += unports[i].ToString() + (i + 1 != unports.Count ? "," : "");
				}

				//список старых УК                 
				sql.Remove(0, sql.Length);
				sql.Append(" select distinct area from " + temp_table + " ");
				var spis_old_area = ClassDBUtils.OpenSQL(sql.ToString(), conn_db);
				if (spis_old_area.resultCode == -1)
				{
					MonitorLog.WriteLog("ChangeArea: Ошибка получения списка старых УК, sql: " + sql.ToString(), MonitorLog.typelog.Error, true);
					ret.result = false;
				}

				for (int i = 0; i < spis_old_area.resultData.Rows.Count; i++)
				{
					listOldArea += spis_old_area.resultData.Rows[i]["area"].ToString().Trim() + (i + 1 != spis_old_area.resultData.Rows.Count ? "," : "");
				}

				//список старых ЖЭУ                
				sql.Remove(0, sql.Length);
				sql.Append(" select distinct geu from " + temp_table + " ");
				var spis_old_geu = ClassDBUtils.OpenSQL(sql.ToString(), conn_db);
				if (spis_old_geu.resultCode == -1)
				{
					MonitorLog.WriteLog("ChangeArea: Ошибка получения списка старых УК, sql: " + sql.ToString(), MonitorLog.typelog.Error, true);
					ret.result = false;
				}

				for (int i = 0; i < spis_old_geu.resultData.Rows.Count; i++)
				{
					listOldGeu += spis_old_geu.resultData.Rows[i]["geu"].ToString().Trim() + (i + 1 != spis_old_geu.resultData.Rows.Count ? "," : "");
				}
				if (sp_dom != "")
				{
					sql.Remove(0, sql.Length);
					sql.Append("update " + temp_table + " set (mark)=(0) where nzp_dom in (" + sp_dom + ") "); //mark = 0 - неперенесены
					ret = ExecSQL(conn_db, sql.ToString(), true);
					if (!ret.result)
					{
						MonitorLog.WriteLog("ChangeArea: Ошибка формирования протокола о переносе ЛС" + sql.ToString(), MonitorLog.typelog.Error, true);
						ret.text = "Ошибка формирования протокола о переносе ЛС";
						ret.result = false;
					}
				}

				//обновление УК во временной табличке
				sql.Remove(0, sql.Length);
				sql.Append("update " + temp_table + " set (area,geu)=('" + finder.new_area.area.Trim() + "','" + finder.new_geu.geu.Trim() + "') where mark=1 ");
				ret = ExecSQL(conn_db, sql.ToString(), true);
				if (!ret.result)
				{
					MonitorLog.WriteLog("ChangeArea: Ошибка формирования протокола о переносе ЛС" + sql.ToString(), MonitorLog.typelog.Error, true);
					ret.text = "Ошибка формирования протокола о переносе ЛС";
					ret.result = false;
				}


				sql.Remove(0, sql.Length);
				sql.Append(" select (trim(ulica) ||' /'||trim(rajon)||' /'||trim(town)) as adres, trim(ndom) as ndom, ");
				sql.Append(" trim(area) as area, trim(geu) as geu from " + temp_table + " where mark=1"); //mark = 1 - перенесены
				ret = ExecRead(conn_db, out reader, sql.ToString(), true);
				if (!ret.result)
				{
					MonitorLog.WriteLog("ChangeArea: Ошибка формирования протокола о переносе ЛС" + sql.ToString(), MonitorLog.typelog.Error, true);
					ret.text = "Ошибка формирования протокола о переносе ЛС";
					ret.result = false;
				}
				if (reader != null)
				{
					//заполнение DataTable
					Utils.setCulture();
					DT.Load(reader, LoadOption.OverwriteChanges);
					reader.Close();
				}

				sql.Remove(0, sql.Length);
				sql.Append("select nzp_dom,(trim(ulica) ||' /'||trim(rajon)||' /'||trim(town)) as adres, trim(ndom) as ndom, ");
				sql.Append(" trim(area) as area, trim(geu) as geu from " + temp_table + " where mark=0"); //mark = 0 - неперенесены
				ret = ExecRead(conn_db, out reader, sql.ToString(), true);
				if (!ret.result)
				{
					MonitorLog.WriteLog("ChangeArea: Ошибка формирования протокола о переносе ЛС" + sql.ToString(), MonitorLog.typelog.Error, true);
					ret.text = "Ошибка формирования протокола о переносе ЛС";
					ret.result = false;
				}
				if (reader != null)
				{
					//заполнение DataTable
					Utils.setCulture();
					DT1.Load(reader, LoadOption.OverwriteChanges);
					DT1.Columns.Add(new DataColumn("reason", Type.GetType("System.String")));
					if (list_reasons.Count > 0)
					{
						for (var i = 0; i < DT1.Rows.Count; i++)
						{
							if (list_reasons.ContainsKey(CastValue<int>(DT1.Rows[i]["nzp_dom"])))
							{
								DT1.Rows[i]["reason"] = list_reasons[CastValue<int>(DT1.Rows[i]["nzp_dom"])];
							}
						}
					}
					reader.Close();
				}
			}
			catch (Exception ex)
			{
				ret.result = false;
				ret.text = "Протокол о переносе ЛС в новую УК не сформирован";
				MonitorLog.WriteLog("Ошибка формирования протокола о переносе ЛС в новую УК " +
					ex.Message, MonitorLog.typelog.Error, 20, 201, true);
			}


			#endregion

			#region Формирование протокола
			FastReport.Report rep = new Report();
			try
			{
				DataSet fDataSet = new DataSet();
				fDataSet.Tables.Add(DT);
				fDataSet.Tables.Add(DT1);

				string template = "protocol_change_area.frx";
				rep.Load(PathHelper.GetReportTemplatePath(template));

				rep.RegisterData(fDataSet);
				rep.GetDataSource("Ported").Enabled = true;
				rep.GetDataSource("Unported").Enabled = true;

				//установка параметров отчета
				rep.SetParameterValue("new_area", finder.new_area.area.Trim());
				rep.SetParameterValue("old_area", listOldArea);
				rep.SetParameterValue("date_port", DateTime.Now.ToString("dd.MM.yyyy hh:mm:ss"));
				rep.SetParameterValue("new_geu", finder.new_geu.geu.Trim());
				rep.SetParameterValue("old_geu", listOldGeu);
				rep.SetParameterValue("result", result);
				rep.SetParameterValue("result_ls", result_ls);


				rep.Prepare();
				ret.text = "Протокол о переносе ЛС в новую УК сформирован";
				ret.tag = rep.Report.PreparedPages.Count;

				fileName = "Протокол_о_переносе_ЛС_в_новую_УК" + DateTime.Now.ToShortDateString().Replace(".", "_") + "_" +
					DateTime.Now.ToShortTimeString().Replace(":", "_") + ".xls";

				FastReport.Export.OoXML.Excel2007Export export_xls = new FastReport.Export.OoXML.Excel2007Export();
				try
				{
					export_xls.ShowProgress = false;
					export_xls.Export(rep, Path.Combine(InputOutput.GetOutputDir(), fileName));
				}
				catch (Exception ex)
				{
					MonitorLog.WriteLog(ex.Message, MonitorLog.typelog.Error, true);
				}

				ret.result = true;

			}
			catch (Exception ex)
			{
				ret.result = false;
				ret.text = "Протокол о переносе ЛС в новую УК не сформирован";
				MonitorLog.WriteLog("Ошибка формирования протокола о переносе ЛС в новую УК " +
					ex.Message, MonitorLog.typelog.Error, 20, 201, true);
			}
			rep.Dispose();

			#endregion

			//перенос  на ftp сервер
			path = InputOutput.SaveOutputFile(Path.Combine(InputOutput.GetOutputDir(), fileName));

			if (ret.result)
			{
				excelRepDb.SetMyFileProgress(new ExcelUtility() { nzp_exc = nzpExc, progress = 1 });
				excelRepDb.SetMyFileState(new ExcelUtility() { nzp_exc = nzpExc, status = ExcelUtility.Statuses.Success, exc_path = path });
			}
			else
			{
				excelRepDb.SetMyFileState(new ExcelUtility() { nzp_exc = nzpExc, status = ExcelUtility.Statuses.Failed });
			}
			excelRepDb.Close();

			if (reader != null) reader.Close();
			if (conn_db != null) conn_db.Close();
			if (conn_web != null) conn_web.Close();

			return ret;



		}


		//Смена УК для списка ЛС из одного дома
		public Returns ChangeAreaForDom(FinderChangeArea finder, IDbConnection conn_db, IDbConnection conn_web, IDbTransaction transaction) {

			Returns ret = new Returns(true);
			List<_Point> prefixs = new List<_Point>();
			_Point point = new _Point();
			DbParameters DbPrm = new DbParameters();
			DbLsServices DbService = new DbLsServices();
			DbAdresHard DbAdres = new DbAdresHard();

			#region определение центрального пользователя
			int nzpUser = finder.nzp_user;
			/*DbWorkUser db = new DbWorkUser();
			Finder f_user = new Finder();
			f_user.nzp_user = finder.nzp_user;
			int nzpUser = db.GetLocalUser(conn_db, transaction, f_user, out ret);
			db.Close();
			if (!ret.result)
			{
				return ret;
			}*/
			#endregion

			if (finder.pref != "")
			{
				point.pref = finder.pref;
				prefixs.Add(point);
			}
			else
			{
				prefixs = Points.PointList;
			}
			MyDataReader reader;
			StringBuilder sql = new StringBuilder();

			//текущий расчетный месяц
			var calc_prm = new CalcMonthParams();
			calc_prm.pref = finder.pref;
			var rec = Points.GetCalcMonth(calc_prm);
			//текущий расчетный месяц этого банка
			var CalcDate = new DateTime(rec.year_, rec.month_, 1);

			string temp_table = "spis_dom_port_" + finder.nzp_user;
			string dat_s = "'" + CalcDate.ToShortDateString() + "'"; //начало  тек.расчетного месяца
			DateTime pred_month_calc = new DateTime(Points.CalcMonth.year_, Points.CalcMonth.month_, 1).AddMonths(-1); //предыдущий рассчетный месяц
			int _month = pred_month_calc.Month;
			int _year = pred_month_calc.Year - 2000;



			#region//--создаем таблицу для переноса kvar
			ExecSQL(conn_db, transaction, "drop table  port_ls;", false);

			sql.Remove(0, sql.Length);
			sql.Append(" CREATE TEMP TABLE  port_ls( ");
			sql.Append(" nzp_kvar integer, ");
			sql.Append(" new_nzp_kvar integer, ");
			sql.Append(" nzp_area INTEGER, ");
			sql.Append(" nzp_geu INTEGER, ");
			sql.Append(" nzp_dom INTEGER NOT NULL, ");
			sql.Append(" nkvar CHAR(10), ");
			sql.Append(" nkvar_n CHAR(10), ");
			sql.Append(" num_ls INTEGER, ");
			sql.Append(" porch SMALLINT, ");
			sql.Append(" phone NCHAR(10), ");
			sql.Append(" dat_notp_s DATE, ");
			sql.Append(" dat_notp_po DATE, ");
			sql.Append(" fio NCHAR(100), ");
			sql.Append(" ikvar INTEGER, ");
			sql.Append(" uch INTEGER, ");
			sql.Append(" gil_s FLOAT, ");
			sql.Append(" remark CHAR(100),  ");
			sql.Append(" pref CHAR(10), ");
			sql.Append(" is_open CHAR(1), ");
			sql.Append(" pkod DECIMAL(13) default 0.0000000000000000 NOT NULL, ");
			sql.Append(" typek INTEGER default 1 NOT NULL, ");
			sql.Append(" pkod10 INTEGER default 0 NOT NULL, ");
			sql.Append(" nzp_wp INTEGER) " + sUnlogTempTable + "; ");
			if (!ExecSQL(conn_db, transaction, sql.ToString(), true).result)
			{
				ret.text = "Ошибка создания таблицы";
				ret.result = false;
				MonitorLog.WriteLog("ChangeArea: Ошибка создания таблицы, sql: " + sql.ToString(), MonitorLog.typelog.Error, true);
				return ret;
			}
			sql.Remove(0, sql.Length);

			#endregion

			#region //--инсертим во врем.табл. ЛС для переноса //по локальным банкам
			foreach (var points in prefixs)
			{

				sql.Append(" insert into port_ls (nzp_kvar,nzp_area,nzp_geu,nzp_dom,nkvar,nkvar_n,num_ls,porch,phone, ");
				sql.Append(" dat_notp_s, dat_notp_po, fio,ikvar,uch,gil_s,remark,pref,is_open, pkod,typek, ");
				sql.Append(" pkod10, nzp_wp ) ");

				sql.Append(" select k.nzp_kvar,k.nzp_area,k.nzp_geu,k.nzp_dom,k.nkvar,k.nkvar_n,k.num_ls,k.porch,k.phone, ");
				sql.Append(" k.dat_notp_s, k.dat_notp_po, k.fio,k.ikvar,k.uch,k.gil_s,k.remark,k.pref,k.is_open, k.pkod,k.typek, ");
				sql.Append(" k.pkod10, k.nzp_wp  ");
				sql.Append(" from " + Points.Pref + "_data" + tableDelimiter + "kvar k, " + temp_table + " d, " + points.pref + "_data" + tableDelimiter + "prm_3 p3 ");
				sql.Append(" where k.nzp_dom=d.nzp_dom   and p3.nzp=k.nzp_kvar and d.nzp_dom=" + finder.nzp_dom + " ");
				sql.Append(" and (p3.nzp_prm=51 and p3.val_prm='1') and " + dat_s + " between p3.dat_s and p3.dat_po ");
				sql.Append(" and (k.nzp_area<>" + finder.new_area.nzp_area + " or k.nzp_geu<>" + finder.new_geu.nzp_geu + ") and p3.is_actual<>100; ");

				ret = ExecSQL(conn_db, transaction, sql.ToString(), true);
				if (!ret.result)
				{
					MonitorLog.WriteLog("ChangeArea: Ошибка  записи в таблицу, sql: " + sql.ToString(), MonitorLog.typelog.Error, true);
					ret.text = "Ошибка записи в таблицу";
					ret.result = false;
					return ret;
				}
				sql.Remove(0, sql.Length);
			}

			ret = ExecRead(conn_db, transaction, out reader, " select * from  port_ls;", true);
			if (!ret.result)
			{
				MonitorLog.WriteLog("ChangeArea: Ошибка извлечения данных, sql: " + sql.ToString(), MonitorLog.typelog.Error, true);
				ret.text = "Ошибка извлечения данных";
				ret.result = false;
				return ret;
			}
			#endregion

			int count_changed_ls = 0; //кол-во перенесенных ЛС в доме
			try
			{
				while (reader.Read())
				{
					//пришлось убрать транзакции с дома, транзакция на ЛС необходима из за функции GeneratePkodOneLS
					transaction = conn_db.BeginTransaction();

					count_changed_ls++;
					Console.WriteLine("Перенос " + count_changed_ls + "-го ЛС");
					Ls new_kvar = new Ls();
					new_kvar.moving = true;
					int old_nzp_kvar = (reader["nzp_kvar"] != DBNull.Value ? (int) reader["nzp_kvar"] : 0);
					new_kvar.nzp_area = finder.new_area.nzp_area;
					new_kvar.nzp_geu = finder.new_geu.nzp_geu;
					new_kvar.stateID = (int) Ls.States.Open;
					new_kvar.nzp_dom = (reader["nzp_dom"] != DBNull.Value ? (int) reader["nzp_dom"] : 0);
					new_kvar.nkvar = (reader["nkvar"] != DBNull.Value ? (string) reader["nkvar"] : "");
					new_kvar.nkvar_n = (reader["nkvar_n"] != DBNull.Value ? (string) reader["nkvar_n"] : "");
					new_kvar.num_ls = (reader["num_ls"] != DBNull.Value ? (int) reader["num_ls"] : 0);
					new_kvar.porch = (reader["porch"] != DBNull.Value ? ((Int16) reader["porch"]).ToString() : "");
					new_kvar.phone = (reader["phone"] != DBNull.Value ? (string) reader["phone"] : "");
					new_kvar.fio = (reader["fio"] != DBNull.Value ? (string) reader["fio"] : "");
					new_kvar.ikvar = (reader["ikvar"] != DBNull.Value ? (int) reader["ikvar"] : 0);
					new_kvar.uch = (reader["uch"] != DBNull.Value ? ((int) reader["uch"]).ToString() : "");
					new_kvar.remark = (reader["remark"] != DBNull.Value ? (string) reader["remark"] : "");
					new_kvar.pref = (reader["pref"] != DBNull.Value ? (string) reader["pref"] : "");
					new_kvar.pkod = (reader["pkod"] != DBNull.Value ? ((Decimal) reader["pkod"]).ToString("0") : "");
					new_kvar.typek = (reader["typek"] != DBNull.Value ? (int) reader["typek"] : 0);
					new_kvar.pkod10 = (reader["pkod10"] != DBNull.Value ? (int) reader["pkod10"] : 0);
					new_kvar.nzp_wp = (reader["nzp_wp"] != DBNull.Value ? (int) reader["nzp_wp"] : 0);
					new_kvar.chekexistls = 0;
					new_kvar.nzp_kvar = Constants._ZERO_;
					new_kvar.nzp_user = finder.nzp_user;
					new_kvar.webLogin = finder.webLogin;


					//создаем новые ЛС, добавляем в prm_3 параметр nzp_prm=51 
					int new_nzp_kvar = DbAdres.Update(conn_db, transaction, conn_web, new_kvar, out ret);
					if (!ret.result)
					{
						MonitorLog.WriteLog("ChangeArea: Ошибка создания новых ЛС Update: " + ret.text, MonitorLog.typelog.Error, true);
						ret.text = "Ошибка создания новых ЛС";
						ret.result = false;
						return ret;
					}

					sql.Remove(0, sql.Length);
					sql.Append("UPDATE " + Points.Pref + sDataAliasRest + "kvar SET is_open='1' where nzp_kvar =" +
							   new_nzp_kvar);
					if (!ExecSQL(conn_db, transaction, sql.ToString(), true).result)
					{
						ret.result = false;
						return ret;
					}

					//создаем связку new_nzp_kvar - old_nzp_kvar
					sql.Remove(0, sql.Length);
					sql.Append(" update port_ls set (new_nzp_kvar)=(" + new_nzp_kvar + ") where nzp_kvar=" + old_nzp_kvar + "; ");
					if (!ExecSQL(conn_db, transaction, sql.ToString(), true).result)
					{
						MonitorLog.WriteLog("ChangeArea: Ошибка обновления nzp_kvar во временной таблице, sql: " + sql.ToString(), MonitorLog.typelog.Error, true);
						ret.text = "Ошибка обновления номеров ЛС во временной таблице";
						ret.result = false;
						return ret;
					}

					#region определение локального пользователя
					int LocalnzpUser = finder.nzp_user;
					
					/*db = new DbWorkUser();
					f_user = new Finder();
					f_user.nzp_user = finder.nzp_user;
					f_user.pref = new_kvar.pref;
					int LocalnzpUser = db.GetLocalUser(conn_db, transaction, f_user, out ret);
					db.Close();
					if (!ret.result)
					{
						ret.result = false;
						return ret;
					}*/
					#endregion

					Ls finderFrom = new Ls();
					Ls finderTo = new Ls();
					finderFrom.pref = new_kvar.pref;
					finderTo.pref = new_kvar.pref;
					finderFrom.nzp_kvar = old_nzp_kvar;
					finderTo.nzp_kvar = new_nzp_kvar;
					finderFrom.moving = true; //!!!перенос в новую УК
					finderFrom.nzp_user = finder.nzp_user;
					finderFrom.stateValidOn = new DateTime(Points.CalcMonth.year_, Points.CalcMonth.month_, 1).ToShortDateString();

					//перенос параметров prm_1, prm_3(кроме nzp_prm=51), prm_18 
					ret = DbPrm.CopyLsParams(conn_db, transaction, finderFrom, finderTo);
					if (!ret.result)
					{
						MonitorLog.WriteLog("ChangeArea: Ошибка переноса параметров в CopyLsParams: " + ret.text, MonitorLog.typelog.Error, true);
						ret.text = "Ошибка переноса параметров";
						ret.result = false;
						return ret;
					}

					#region Запись истории перемещений ЛС
					sql.Remove(0, sql.Length);
					sql.Append("insert into " + Points.Pref + "_data" + tableDelimiter + "moving_operations (created_by,created_on,operation_type_id) ");
					sql.Append(" values (" + nzpUser + "," + sCurDateTime + ", 1) "); //operation_type_id==1 - Смена УК
					if (!ExecSQL(conn_db, transaction, sql.ToString(), true).result)
					{
						ret.text = "Ошибка записи в таблицу";
						ret.result = false;
						MonitorLog.WriteLog("ChangeArea: Ошибка записи в таблицу moving_operations, sql: " + sql.ToString(), MonitorLog.typelog.Error, true);
						return ret;
					}


					int operation_id = GetSerialValue(conn_db, transaction);
					sql.Remove(0, sql.Length);
					sql.Append("insert into " + new_kvar.pref + "_data" + tableDelimiter + "moving_objects (operation_id,old_id,new_id,object_type_id) ");
					sql.Append(" values (" + operation_id + "," + old_nzp_kvar + ", " + new_nzp_kvar + ", 1 ) "); //object_type_id==1 - ЛС
					if (!ExecSQL(conn_db, transaction, sql.ToString(), true).result)
					{
						ret.text = "Ошибка записи в таблицу";
						ret.result = false;
						MonitorLog.WriteLog("ChangeArea: Ошибка записи в таблицу moving_objects, sql: " + sql.ToString(), MonitorLog.typelog.Error, true);
						return ret;
					}
					#endregion

					#region Закрытие старых ЛС, Параметров

					Param prm = new Param();
					new_kvar.CopyTo(prm);
					prm.nzp = old_nzp_kvar;
					prm.prms = Constants.act_del_val.ToString();
					prm.dat_s = new DateTime(Points.CalcMonth.year_, Points.CalcMonth.month_, 1).ToShortDateString();
					#region закрытие параметров prm_1
					prm.prm_num = 1;
					sql.Remove(0, sql.Length);
					sql.Append(" select distinct  nzp_prm  from " + new_kvar.pref + "_data" + tableDelimiter + "prm_1 where nzp=" + old_nzp_kvar + "");
					var dt = ClassDBUtils.OpenSQL(sql.ToString(), conn_db, transaction);
					if (dt.resultCode == -1)
					{
						MonitorLog.WriteLog("ChangeArea: Ошибка выборки списка параметров prm_1, sql: " + sql.ToString(), MonitorLog.typelog.Error, true);
						ret.result = false;
						ret.text = "Ошибка выборки списка параметров";
						return ret;
					}

					for (int i = 0; i < dt.resultData.Rows.Count; i++)
					{
						int nzp_prm = ((int) (dt.resultData.Rows[i]["nzp_prm"]));
						prm.nzp_prm = nzp_prm;
						ret = DbPrm.SavePrm(conn_db, transaction, prm);
						if (!ret.result)
						{
							MonitorLog.WriteLog("ChangeArea: Ошибка закрытия параметров в prm_1:" + ret.text, MonitorLog.typelog.Error, true);
							ret.text = "Ошибка закрытия параметров";
							ret.result = false;
							return ret;
						}
					}

					#endregion

					#region закрытие параметров prm_3
					prm.prm_num = 3;
					sql.Remove(0, sql.Length);
					sql.Append(" select distinct  nzp_prm  from " + new_kvar.pref + "_data" + tableDelimiter + "prm_3 where nzp=" + old_nzp_kvar + "");
					dt = ClassDBUtils.OpenSQL(sql.ToString(), conn_db, transaction);
					if (dt.resultCode == -1)
					{
						MonitorLog.WriteLog("ChangeArea: Ошибка выборки списка параметров prm_3, sql: " + sql.ToString(), MonitorLog.typelog.Error, true);
						ret.result = false;
						ret.text = "Ошибка выборки списка параметров";
						return ret;
					}

					for (int i = 0; i < dt.resultData.Rows.Count; i++)
					{
						int nzp_prm = ((int) (dt.resultData.Rows[i]["nzp_prm"]));
						if (nzp_prm == 51)
						{
							prm.val_prm = "2";
							prm.prms = "";
						}
						else
						{
							prm.prms = Constants.act_del_val.ToString();
						}

						prm.nzp_prm = nzp_prm;
						ret = DbPrm.SavePrm(conn_db, transaction, prm);
						if (!ret.result)
						{
							MonitorLog.WriteLog("ChangeArea: Ошибка закрытия параметров в prm_3:" + ret.text, MonitorLog.typelog.Error, true);
							ret.text = "Ошибка закрытия параметров";
							ret.result = false;
							return ret;
						}
					}

					#endregion

					#region закрытие параметров prm_18
					string base_name = new_kvar.pref + "_data";
					if (TableInBase(conn_db, transaction, base_name, "prm_18"))//если существует
					{
						dt = null;
						prm.prm_num = 18;
						sql.Remove(0, sql.Length);
						sql.Append(" select distinct  nzp_prm  from " + new_kvar.pref + "_data" + tableDelimiter + "prm_18 where nzp=" + old_nzp_kvar + "");
						dt = ClassDBUtils.OpenSQL(sql.ToString(), conn_db, transaction);
						if (dt.resultCode == -1)
						{
							MonitorLog.WriteLog("ChangeArea: Ошибка выборки списка параметров prm_18, sql: " + sql.ToString(), MonitorLog.typelog.Error, true);
							ret.result = false;
							ret.text = "Ошибка выборки списка параметров";
							return ret;
						}

						for (int i = 0; i < dt.resultData.Rows.Count; i++)
						{
							int nzp_prm = ((int) (dt.resultData.Rows[i]["nzp_prm"]));
							prm.nzp_prm = nzp_prm;
							ret = DbPrm.SavePrm(conn_db, transaction, prm);
							if (!ret.result)
							{
								MonitorLog.WriteLog("ChangeArea: Ошибка закрытия параметров в prm_18:" + ret.text, MonitorLog.typelog.Error, true);
								ret.text = "Ошибка закрытия параметров";
								ret.result = false;
								return ret;
							}
						}
					}
					#endregion

					#region закрытие параметров tarif
					Service finder_srv = new Service();
					new_kvar.CopyTo(finder_srv);
					finder_srv.prms = "," + Constants.act_del_serv.ToString();
					finder_srv.nzp_kvar = old_nzp_kvar;
					finder_srv.dat_s = new DateTime(Points.CalcMonth.year_, Points.CalcMonth.month_, 1).ToShortDateString();
					finder_srv.dat_po = DateTime.MaxValue.ToString("dd.MM.yyyy");
					sql.Remove(0, sql.Length);

					sql.Append(" select distinct nzp_serv from " + new_kvar.pref + "_data" + tableDelimiter + "tarif where nzp_kvar=" + old_nzp_kvar + " and is_actual<>100 ");
					sql.Append(" and mdy(" + Points.CalcMonth.month_ + ",1," + Points.CalcMonth.year_ + ") between dat_s and dat_po");
					dt = ClassDBUtils.OpenSQL(sql.ToString(), conn_db, transaction);
					if (dt.resultCode == -1)
					{
						MonitorLog.WriteLog("ChangeArea: Ошибка выборки списка параметров tarif, sql: " + sql.ToString(), MonitorLog.typelog.Error, true);
						ret.result = false;
						ret.text = "Ошибка выборки списка параметров";
						return ret;
					}

					for (int i = 0; i < dt.resultData.Rows.Count; i++)
					{
						int nzp_serv = ((int) (dt.resultData.Rows[i]["nzp_serv"]));
						finder_srv.nzp_serv = nzp_serv;


						ret = DbService.SaveService(finder_srv, finder_srv, conn_db, transaction);
						if (!ret.result)
						{
							MonitorLog.WriteLog("ChangeArea: Ошибка закрытия параметров в tarif:" + ret.text, MonitorLog.typelog.Error, true);
							ret.text = "Ошибка закрытия параметров";
							ret.result = false;
							return ret;
						}
					}

					#endregion



					#region Перенос недопоставок

					var list_cols = string.Join(",",
						ListColumnsInTable(conn_db, transaction, new_kvar.pref + sDataAliasRest, "nedop_kvar", true));
					var list_vals = string.Join(",",
						ListColumnsInTable(conn_db, transaction, new_kvar.pref + sDataAliasRest, "nedop_kvar", true));
					list_vals = list_vals.Replace("nzp_kvar", new_nzp_kvar.ToString());
					list_vals = list_vals.Replace("dat_s", dat_s);
					list_vals = list_vals.Replace("nzp_user", LocalnzpUser.ToString());
					//insert'им недопоставки в новый ЛС
					sql.Remove(0, sql.Length);
					sql.Append(" insert into " + new_kvar.pref + "_data" + tableDelimiter + "nedop_kvar ");
					sql.Append("(" + list_cols + ")");
					sql.Append(" select " + list_vals + " from  " + new_kvar.pref + "_data" + tableDelimiter + "nedop_kvar ");
					sql.Append(" where nzp_kvar=" + old_nzp_kvar + " and is_actual<>100 and dat_po>=mdy(" + Points.CalcMonth.month_ + ",1," + Points.CalcMonth.year_ + ") ");
					if (!ExecSQL(conn_db, transaction, sql.ToString(), true).result)
					{
						MonitorLog.WriteLog("ChangeArea: Ошибка переноса недопоставок, sql: " + sql.ToString(), MonitorLog.typelog.Error, true);
						ret.text = "Ошибка переноса недопоставок";
						ret.result = false;
						return ret;
					}

					Nedop finder_nedop = new Nedop();
					new_kvar.CopyTo(finder_nedop);
					DbNedop DbNedop = new DbNedop();
					finder_nedop.dat_s = new DateTime(Points.CalcMonth.year_, Points.CalcMonth.month_, 1).ToString();
					finder_nedop.dat_po = "01.01.3000 0:00:00";
					finder_nedop.nzp_kvar = old_nzp_kvar;
					finder_nedop.prms = "," + Constants.act_del_nedop.ToString();
					finder_nedop.webLogin = finder.webLogin;


					//закрываем недопоставки по старым ЛС 
					sql.Remove(0, sql.Length);
					sql.Append(" select distinct  nzp_serv  from " + new_kvar.pref + "_data" + tableDelimiter + "nedop_kvar where nzp_kvar=" + old_nzp_kvar + "");
					dt = ClassDBUtils.OpenSQL(sql.ToString(), conn_db, transaction);
					if (dt.resultCode == -1)
					{
						MonitorLog.WriteLog("ChangeArea: Ошибка выборки списка параметров nedop_kvar, sql: " + sql.ToString(), MonitorLog.typelog.Error, true);
						ret.result = false;
						ret.text = "Ошибка выборки списка параметров";
						return ret;
					}

					for (int i = 0; i < dt.resultData.Rows.Count; i++)
					{
						int nzp_serv = ((int) (dt.resultData.Rows[i]["nzp_serv"]));
						finder_nedop.nzp_serv = nzp_serv;
						DbNedop.SaveNedop(finder_nedop, finder_nedop, conn_db, transaction, out ret);
						if (!ret.result)
						{
							MonitorLog.WriteLog("ChangeArea: Ошибка закрытия параметров в nedop_kvar:" + ret.text, MonitorLog.typelog.Error, true);
							ret.text = "Ошибка закрытия параметров";
							ret.result = false;
							return ret;
						}
					}

					#endregion

					#endregion

					#region Перенос ПУ

					sql.Remove(0, sql.Length);
					sql.Append(" select  nzp_counter from " + new_kvar.pref + "_data" + tableDelimiter + "counters_spis where nzp=" + old_nzp_kvar + " ");
					var spis_cnt = ClassDBUtils.OpenSQL(sql.ToString(), conn_db, transaction);
					if (spis_cnt.resultCode == -1)
					{
						MonitorLog.WriteLog("ChangeArea: Ошибка получения списка ИПУ для переноса в новую УК, sql: " + sql.ToString(), MonitorLog.typelog.Error, true);
						ret.result = false;
						ret.text = "Ошибка получения списка ИПУ для переноса в новую УК";
						return ret;
					}
					for (int i = 0; i < spis_cnt.resultData.Rows.Count; i++)
					{
						int old_nzp_counter = (int) spis_cnt.resultData.Rows[i]["nzp_counter"];
						//insert'им ИПУ в новый ЛС
						list_cols = string.Join(",", ListColumnsInTable(conn_db, transaction, new_kvar.pref + sDataAliasRest, "counters_spis", true));
						list_vals = list_cols + "";
						list_vals = list_vals.Replace(",nzp,", "," + new_nzp_kvar.ToString() + ",");

						sql.Remove(0, sql.Length);
						sql.Append(" insert into " + new_kvar.pref + "_data" + tableDelimiter + "counters_spis ");
						sql.Append("(" + list_cols + ")");
						sql.Append(" select " + list_vals + " from  " + new_kvar.pref + "_data" + tableDelimiter + "counters_spis");
						sql.Append(" where nzp_counter =" + old_nzp_counter + " ");
						if (!ExecSQL(conn_db, transaction, sql.ToString(), true).result)
						{
							MonitorLog.WriteLog("ChangeArea: Ошибка переноса приборов учета, sql: " + sql.ToString(), MonitorLog.typelog.Error, true);
							ret.text = "Ошибка переноса приборов учета";
							ret.result = false;
							return ret;
						}
						int new_nzp_counter = GetSerialValue(conn_db, transaction);

						list_cols = string.Join(",", ListColumnsInTable(conn_db, transaction, new_kvar.pref + sDataAliasRest, "prm_17", true));
						list_vals = list_cols + "";
						list_vals = list_vals.Replace("nzp,", new_nzp_counter.ToString() + ",");

						//перенос параметров из prm_17
						sql.Remove(0, sql.Length);
						sql.Append(" insert into " + new_kvar.pref + "_data" + tableDelimiter + "prm_17 ");
						sql.Append("(" + list_cols + ")");
						sql.Append(" select " + list_vals);
						sql.Append(" from " + new_kvar.pref + "_data" + tableDelimiter + "prm_17 where nzp = " + old_nzp_counter + " ");
						if (!ExecSQL(conn_db, transaction, sql.ToString(), true).result)
						{
							MonitorLog.WriteLog("ChangeArea: Ошибка переноса параметров prm_17, sql: " + sql.ToString(), MonitorLog.typelog.Error, true);
							ret.text = "Ошибка переноса приборов учета";
							ret.result = false;
							return ret;
						}

						list_cols = string.Join(",", ListColumnsInTable(conn_db, transaction, new_kvar.pref + sDataAliasRest, "counters", true));
						list_vals = list_cols + "";

						list_vals = list_vals.Replace("nzp_kvar", new_nzp_kvar.ToString());
						list_vals = list_vals.Replace("num_ls", new_nzp_kvar.ToString());
						list_vals = list_vals.Replace("nzp_counter", new_nzp_counter.ToString());

						//перенос показаний ПУ
						sql.Remove(0, sql.Length);
						sql.Append(" insert into  " + new_kvar.pref + "_data" + tableDelimiter + "counters  ");
						sql.Append("(" + list_cols + ")");
						sql.Append(" select  " + list_vals);
						sql.Append(" from  " + new_kvar.pref + "_data" + tableDelimiter + "counters where nzp_counter=" + old_nzp_counter + " ");
						if (!ExecSQL(conn_db, transaction, sql.ToString(), true).result)
						{
							MonitorLog.WriteLog("ChangeArea: Ошибка переноса показаний ПУ counters, sql: " + sql.ToString(), MonitorLog.typelog.Error, true);
							ret.text = "Ошибка переноса показаний приборов учета";
							ret.result = false;
							return ret;
						}

						#region Запись истории перемещений ПУ
						sql.Remove(0, sql.Length);
						sql.Append("insert into " + Points.Pref + "_data" + tableDelimiter + "moving_operations (created_by,created_on,operation_type_id) ");
						sql.Append(" values (" + nzpUser + "," + sCurDateTime + ", 1) "); //operation_type_id==1 - Смена УК 
						if (!ExecSQL(conn_db, transaction, sql.ToString(), true).result)
						{
							ret.text = "Ошибка записи в таблицу";
							ret.result = false;
							MonitorLog.WriteLog("ChangeArea: Ошибка записи в таблицу moving_operations, sql: " + sql.ToString(), MonitorLog.typelog.Error, true);
							return ret;
						}

						operation_id = GetSerialValue(conn_db, transaction);
						sql.Remove(0, sql.Length);
						sql.Append("insert into " + new_kvar.pref + "_data" + tableDelimiter + "moving_objects (operation_id,old_id,new_id,object_type_id) ");
						sql.Append(" values (" + operation_id + "," + old_nzp_counter + ", " + new_nzp_counter + ", 2 ) "); //object_type_id==2 - Жилец
						if (!ExecSQL(conn_db, transaction, sql.ToString(), true).result)
						{
							ret.text = "Ошибка записи в таблицу";
							ret.result = false;
							MonitorLog.WriteLog("ChangeArea: Ошибка записи в таблицу moving_objects, sql: " + sql.ToString(), MonitorLog.typelog.Error, true);
							return ret;
						}
						#endregion

						#region Закрытие ПУ
						sql.Remove(0, sql.Length);
						sql.Append(" update " + new_kvar.pref + "_data" + tableDelimiter + "counters_spis set (dat_close)=(mdy(" + DateTime.Now.Month + "," + DateTime.Now.Day + "," + DateTime.Now.Year + ")) ");
						sql.Append(" where nzp_counter=" + old_nzp_counter + " and dat_close is null and is_actual<>100; ");
						if (!ExecSQL(conn_db, transaction, sql.ToString(), true).result)
						{
							MonitorLog.WriteLog("ChangeArea: Ошибка закрытия ПУ в counters_spis, sql: " + sql.ToString(), MonitorLog.typelog.Error, true);
							ret.text = "Ошибка закрытия ПУ";
							ret.result = false;
							return ret;
						}
						#endregion
					}


					#endregion


					//todo ПЕРЕДЕЛАТЬ под новые условия
					#region Перенос сальдо
					if (finder.export_saldo)
					{

						//_year,_month - год, месяц предыдущей рассчетной даты
						//пишем в таблицу perekidka 
						sql.Remove(0, sql.Length);
						sql.Append(" insert into " + new_kvar.pref + "_charge_" + (Points.CalcMonth.year_ - 2000).ToString("00") + tableDelimiter + "perekidka ");
						sql.Append(" (nzp_kvar, num_ls, nzp_serv, nzp_supp, type_rcl, date_rcl, tarif, volum, sum_rcl, month_, comment, nzp_user, nzp_reestr)");
						sql.Append(" select c.nzp_kvar,c.num_ls ,c.nzp_serv,c.nzp_supp,1,mdy(" + DateTime.Now.Month + "," + DateTime.Now.Day + "," + DateTime.Now.Year + ") ");
						sql.Append(" ,0,0,-1*c.sum_outsaldo, " + _month + "");
						sql.Append(",'Перенос ЛС в новую УК с долгами'," + LocalnzpUser + ",0 ");
						sql.Append(" from " + new_kvar.pref + "_charge_" + _year.ToString("00") + tableDelimiter + "charge_" + _month.ToString("00") + " c where c.nzp_kvar=" + old_nzp_kvar + "");
						sql.Append(" and c.dat_charge is null and c.nzp_serv>1 ");
						sql.Append(" and (c.sum_insaldo<>0 and c.sum_insaldo is not null) ");

						if (!ExecSQL(conn_db, transaction, sql.ToString(), true).result)
						{
							MonitorLog.WriteLog("ChangeArea: Ошибка переноса сальдо в таблицу perekidka, sql: " + sql.ToString(), MonitorLog.typelog.Error, true);
							ret.text = "Ошибка переноса сальдо";
							ret.result = false;
							return ret;
						}


						//перенос сальдо в новые лицевые счета в прошлый рассчетный месяц
						sql.Remove(0, sql.Length);
						sql.Append(" insert into " + new_kvar.pref + "_charge_" + _year.ToString("00") + tableDelimiter + "charge_" + _month.ToString("00") + " ");
						sql.Append(" (nzp_kvar, num_ls, nzp_serv, nzp_supp, sum_outsaldo) ");
						sql.Append(" select " + new_nzp_kvar + "," + new_nzp_kvar + ",c.nzp_serv, c.nzp_supp,c.sum_outsaldo ");
						sql.Append(" from  " + new_kvar.pref + "_charge_" + _year.ToString("00") + tableDelimiter + "charge_" + _month.ToString("00") + " c");
						sql.Append(" where c.nzp_kvar=" + old_nzp_kvar + " and c.dat_charge is null and c.nzp_serv>1 and (c.sum_insaldo<>0 and c.sum_insaldo is not null) ");
						if (!ExecSQL(conn_db, transaction, sql.ToString(), true).result)
						{
							MonitorLog.WriteLog("ChangeArea: Ошибка переноса сальдо для новых ЛС, sql: " + sql.ToString(), MonitorLog.typelog.Error, true);
							ret.text = "Ошибка переноса сальдо";
							ret.result = false;
							return ret;
						}



					}
					#endregion

					#region Перенос жильца

					//достаем список nzp_gil для карточек 
					sql.Remove(0, sql.Length);
					sql.Append(" select distinct  nzp_gil from " + new_kvar.pref + "_data" + tableDelimiter + "kart where nzp_kvar=" + old_nzp_kvar + "");
					dt = ClassDBUtils.OpenSQL(sql.ToString(), conn_db, transaction);
					if (dt.resultCode == -1)
					{
						MonitorLog.WriteLog("ChangeArea: Ошибка выборки списка параметров kart, sql: " + sql.ToString(), MonitorLog.typelog.Error, true);
						ret.result = false;
						ret.text = "Ошибка выборки списка параметров";
						return ret;
					}
					for (int i = 0; i < dt.resultData.Rows.Count; i++)
					{
						int old_nzp_gil = (int) dt.resultData.Rows[i]["nzp_gil"];

						//пишем в gilec новую запись копирую параметры старой
						sql.Remove(0, sql.Length);
						sql.Append(" insert into " + new_kvar.pref + "_data" + tableDelimiter + "gilec ");
						sql.Append(" (sogl)");
						sql.Append(" select sogl from " + new_kvar.pref + "_data" + tableDelimiter + "gilec where nzp_gil=" + old_nzp_gil + "");
						if (!ExecSQL(conn_db, transaction, sql.ToString(), true).result)
						{
							MonitorLog.WriteLog("ChangeArea: Ошибка записи в таблицу gilec, sql: " + sql.ToString(), MonitorLog.typelog.Error, true);
							ret.text = "Ошибка переноса параметров жильцов";
							ret.result = false;
							return ret;
						}
						//получаем новый серийник
						int new_nzp_gil = GetSerialValue(conn_db, transaction);

						list_cols = string.Join(",", ListColumnsInTable(conn_db, transaction, new_kvar.pref + "_data", "kart", true));
						list_vals = list_cols + "";
						list_vals = list_vals.Replace("nzp_gil", new_nzp_gil.ToString());
						list_vals = list_vals.Replace("nzp_kvar", new_nzp_kvar.ToString());

						//пишем в kart записи у которых nzp_gil=old_nzp_gil. Пишем с новым серийником и new_nzp_gil, new_nzp_kvar
						sql.Remove(0, sql.Length);
						sql.Append(" insert into  " + new_kvar.pref + "_data" + tableDelimiter + "kart ");
						sql.Append("(" + list_cols + ")");
						sql.Append(" select " + list_vals + " from " + new_kvar.pref + "_data" + tableDelimiter + "kart where nzp_kvar=" + old_nzp_kvar + "  and  nzp_gil=" + old_nzp_gil + " ");
						if (!ExecSQL(conn_db, transaction, sql.ToString(), true).result)
						{
							MonitorLog.WriteLog("ChangeArea: Ошибка записи в таблицу kart, sql: " + sql.ToString(), MonitorLog.typelog.Error, true);
							ret.text = "Ошибка переноса параметров жильцов";
							ret.result = false;
							return ret;
						}

						list_cols = string.Join(",", ListColumnsInTable(conn_db, transaction, new_kvar.pref + "_data", "gil_periods", true));
						list_vals = list_cols + "";
						list_vals = list_vals.Replace("nzp_gil", new_nzp_gil.ToString());
						list_vals = list_vals.Replace("nzp_kvar", new_nzp_kvar.ToString());

						//пишем периоды убытия жильца
						sql.Remove(0, sql.Length);
						sql.Append(" insert into  " + new_kvar.pref + "_data" + tableDelimiter + "gil_periods ");
						sql.Append("(" + list_cols + ")");
						sql.Append(" select " + list_vals + " from " + new_kvar.pref + "_data" + tableDelimiter + "gil_periods" +
								   " where nzp_kvar=" + old_nzp_kvar + "  and  nzp_gilec=" + old_nzp_gil + " ");
						if (!ExecSQL(conn_db, transaction, sql.ToString(), true).result)
						{
							MonitorLog.WriteLog("ChangeArea: Ошибка записи в таблицу kart, sql: " + sql.ToString(), MonitorLog.typelog.Error, true);
							ret.text = "Ошибка переноса параметров жильцов";
							ret.result = false;
							return ret;
						}


						#region Запись истории перемещений Жильца
						sql.Remove(0, sql.Length);
						sql.Append("insert into " + Points.Pref + "_data" + tableDelimiter + "moving_operations (created_by,created_on,operation_type_id) ");
						sql.Append(" values (" + nzpUser + "," + sCurDateTime + ", 1) "); //operation_type_id==1 - Смена УК 
						if (!ExecSQL(conn_db, transaction, sql.ToString(), true).result)
						{
							ret.text = "Ошибка записи в таблицу";
							ret.result = false;
							MonitorLog.WriteLog("ChangeArea: Ошибка записи в таблицу moving_operations, sql: " + sql.ToString(), MonitorLog.typelog.Error, true);
							return ret;
						}

						operation_id = GetSerialValue(conn_db, transaction);
						sql.Remove(0, sql.Length);
						sql.Append("insert into " + new_kvar.pref + "_data" + tableDelimiter + "moving_objects (operation_id,old_id,new_id,object_type_id) ");
						sql.Append(" values (" + operation_id + "," + old_nzp_gil + ", " + new_nzp_gil + ", 3 ) "); //object_type_id==3 - Жилец
						if (!ExecSQL(conn_db, transaction, sql.ToString(), true).result)
						{
							ret.text = "Ошибка записи в таблицу";
							ret.result = false;
							MonitorLog.WriteLog("ChangeArea: Ошибка записи в таблицу moving_objects, sql: " + sql.ToString(), MonitorLog.typelog.Error, true);
							return ret;
						}
						#endregion

					}//конец цикла по nzp_gil

					#region + перенос жильцов для Самары  (_arx)
					if (Points.IsSmr)
					{
						//достаем список nzp_gil для карточек 
						sql.Remove(0, sql.Length);
						sql.Append(" select distinct  nzp_gil from " + new_kvar.pref + "_data" + tableDelimiter + "kart_arx where nzp_kvar=" + old_nzp_kvar + "");
						dt = ClassDBUtils.OpenSQL(sql.ToString(), conn_db, transaction);
						if (dt.resultCode == -1)
						{
							MonitorLog.WriteLog("ChangeArea: Ошибка выборки списка параметров kart_arx, sql: " + sql.ToString(), MonitorLog.typelog.Error, true);
							ret.result = false;
							ret.text = "Ошибка выборки списка параметров";
							return ret;
						}
						for (int i = 0; i < dt.resultData.Rows.Count; i++)
						{
							int old_nzp_gil = (int) dt.resultData.Rows[i]["nzp_gil"];

							//пишем в gilec новую запись копирую параметры старой
							sql.Remove(0, sql.Length);
							sql.Append(" insert into " + new_kvar.pref + "_data" + tableDelimiter + "gilec_arx ");
							sql.Append("(sogl)");
							sql.Append(" select sogl from " + new_kvar.pref + "_data" + tableDelimiter + "gilec_arx where nzp_gil=" + old_nzp_gil + "");
							if (!ExecSQL(conn_db, transaction, sql.ToString(), true).result)
							{
								MonitorLog.WriteLog("ChangeArea: Ошибка записи в таблицу gilec_arx, sql: " + sql.ToString(), MonitorLog.typelog.Error, true);
								ret.text = "Ошибка переноса параметров жильцов";
								ret.result = false;
								return ret;
							}
							//получаем новый серийник
							int new_nzp_gil = GetSerialValue(conn_db, transaction);
							list_cols = string.Join(",", ListColumnsInTable(conn_db, transaction, new_kvar.pref + "_data", "kart_arx", true));
							list_vals = list_cols + "";
							list_vals = list_vals.Replace("nzp_gil", new_nzp_gil.ToString());
							list_vals = list_vals.Replace("nzp_kvar", new_nzp_kvar.ToString());

							//пишем в kart записи у которых nzp_gil=old_nzp_gil. Пишем с новым серийником и new_nzp_gil, new_nzp_kvar
							sql.Remove(0, sql.Length);
							sql.Append(" insert into  " + new_kvar.pref + "_data" + tableDelimiter + "kart_arx ");
							sql.Append("(" + list_cols + ")");
							sql.Append(" select  " + list_vals + " from " + new_kvar.pref + "_data" + tableDelimiter + "kart_arx where nzp_kvar=" + old_nzp_kvar + "  and  nzp_gil=" + old_nzp_gil + " ");
							if (!ExecSQL(conn_db, transaction, sql.ToString(), true).result)
							{
								MonitorLog.WriteLog("ChangeArea: Ошибка записи в таблицу kart_arx, sql: " + sql.ToString(), MonitorLog.typelog.Error, true);
								ret.text = "Ошибка переноса параметров жильцов";
								ret.result = false;
								return ret;
							}

							#region Запись истории перемещений Жильца
							sql.Remove(0, sql.Length);
							sql.Append("insert into " + Points.Pref + "_data" + tableDelimiter + "moving_operations (created_by,created_on,operation_type_id) ");
							sql.Append(" values (" + nzpUser + "," + sCurDateTime + ", 1) "); //operation_type_id==1 - Смена УК 
							if (!ExecSQL(conn_db, transaction, sql.ToString(), true).result)
							{
								ret.text = "Ошибка записи в таблицу";
								ret.result = false;
								MonitorLog.WriteLog("ChangeArea: Ошибка записи в таблицу moving_operations, sql: " + sql.ToString(), MonitorLog.typelog.Error, true);
								return ret;
							}

							operation_id = GetSerialValue(conn_db, transaction);
							sql.Remove(0, sql.Length);
							sql.Append("insert into " + new_kvar.pref + "_data" + tableDelimiter + "moving_objects (operation_id,old_id,new_id,object_type_id) ");
							sql.Append(" values (" + operation_id + "," + old_nzp_gil + ", " + new_nzp_gil + ", 3 ) "); //object_type_id==3 - Жилец
							if (!ExecSQL(conn_db, transaction, sql.ToString(), true).result)
							{
								ret.text = "Ошибка записи в таблицу";
								ret.result = false;
								MonitorLog.WriteLog("ChangeArea: Ошибка записи в таблицу moving_objects, sql: " + sql.ToString(), MonitorLog.typelog.Error, true);
								return ret;
							}
							#endregion

						}//конец цикла по nzp_gil                        
					}
					#endregion

					list_cols = string.Join(",", ListColumnsInTable(conn_db, transaction, new_kvar.pref + "_data", "sobstw", true));
					list_vals = list_cols + "";
					list_vals = list_vals.Replace("nzp_kvar", new_nzp_kvar.ToString());

					//пишем записи в sobstw 
					sql.Remove(0, sql.Length);
					sql.Append(" insert into " + new_kvar.pref + "_data" + tableDelimiter + "sobstw ");
					sql.Append("(" + list_cols + ")");
					sql.Append(" select  " + list_vals + " ");
					sql.Append(" from  " + new_kvar.pref + "_data" + tableDelimiter + "sobstw where nzp_kvar=" + old_nzp_kvar + " ");
					if (!ExecSQL(conn_db, transaction, sql.ToString(), true).result)
					{
						MonitorLog.WriteLog("ChangeArea: Ошибка записи в таблицу sobstw, sql: " + sql.ToString(), MonitorLog.typelog.Error, true);
						ret.text = "Ошибка переноса параметров жильцов";
						ret.result = false;
						return ret;
					}

					int new_nzp_sobstw = GetSerialValue(conn_db, transaction);
					int old_nzp_sobstw = 0;

					sql.Remove(0, sql.Length);
					sql.Append("select nzp_sobstw from " + new_kvar.pref + "_data" + tableDelimiter + "sobstw where nzp_kvar=" + old_nzp_kvar + " ");
					object sobstw = ExecScalar(conn_db, transaction, sql.ToString(), out ret, true);
					if (!ret.result)
					{
						MonitorLog.WriteLog("ChangeArea: Ошибка получения серийника nzp_sobstw для старого ЛС, sql: " + sql.ToString(), MonitorLog.typelog.Error, true);
						ret.text = "Ошибка получения параметров";
						ret.result = false;
						return ret;
					}
					if (sobstw != null)
					{ old_nzp_sobstw = (int) sobstw; }
					if (old_nzp_kvar == 0)
					{
						MonitorLog.WriteLog("ChangeArea: Ошибка получения серийника nzp_sobstw для старого ЛС, sql: " + sql.ToString(), MonitorLog.typelog.Error, true);
						ret.text = "Ошибка получения параметров";
						ret.result = false;
						return ret;
					}

					#region Запись истории перемещений Собственника
					sql.Remove(0, sql.Length);
					sql.Append("insert into " + Points.Pref + "_data" + tableDelimiter + "moving_operations (created_by,created_on,operation_type_id) ");
					sql.Append(" values (" + nzpUser + "," + sCurDateTime + ", 1) "); //operation_type_id==1 - Смена УК 
					if (!ExecSQL(conn_db, transaction, sql.ToString(), true).result)
					{
						ret.text = "Ошибка записи в таблицу";
						ret.result = false;
						MonitorLog.WriteLog("ChangeArea: Ошибка записи в таблицу moving_operations, sql: " + sql.ToString(), MonitorLog.typelog.Error, true);
						return ret;
					}

					operation_id = GetSerialValue(conn_db, transaction);
					sql.Remove(0, sql.Length);
					sql.Append("insert into " + new_kvar.pref + "_data" + tableDelimiter + "moving_objects (operation_id,old_id,new_id,object_type_id) ");
					sql.Append(" values (" + operation_id + "," + old_nzp_sobstw + ", " + new_nzp_sobstw + ", 4) "); //object_type_id==4 - Собственник
					if (!ExecSQL(conn_db, transaction, sql.ToString(), true).result)
					{
						ret.text = "Ошибка записи в таблицу";
						ret.result = false;
						MonitorLog.WriteLog("ChangeArea: Ошибка записи в таблицу moving_objects, sql: " + sql.ToString(), MonitorLog.typelog.Error, true);
						return ret;
					}
					#endregion

					#endregion


					transaction.Commit();
				}//закрытие while по ЛС 
				reader.Close();
			}
			catch (Exception ex)
			{
				transaction.Rollback();
				MonitorLog.WriteLog("ChangeArea: Ошибка переноса лицевых счетов, ошибка: " + ex.Message, MonitorLog.typelog.Error, true);
				ret.text = "Ошибка переноса лицевых счетов";
				ret.result = false;
				return ret;
			}

			#region Обновление по домам
			//обновление данных домов по верхнему банку
			sql.Remove(0, sql.Length);
#if PG
			sql.Append("update  " + Points.Pref + "_data" + tableDelimiter + "dom set nzp_area=" + finder.new_area.nzp_area + ",nzp_geu=" + finder.new_geu.nzp_geu + " ");
#else
			sql.Append("update  " + Points.Pref + "_data" + tableDelimiter + "dom set (nzp_area,nzp_geu)=(" + finder.new_area.nzp_area + "," + finder.new_geu.nzp_geu + ") ");
#endif
			sql.Append(" where nzp_dom=" + finder.nzp_dom + " ");
			if (!ExecSQL(conn_db, transaction, sql.ToString(), true).result)
			{
				ret.text = "Ошибка обновления домовых данных";
				ret.result = false;
				MonitorLog.WriteLog("ChangeArea: Ошибка обновления таблицы dom в верхнем банке, sql: " + sql.ToString(), MonitorLog.typelog.Error, true);
				return ret;
			}

			//обновление данных домов по нижним банкам
			foreach (var points in prefixs)
			{
				sql.Remove(0, sql.Length);
#if PG
				sql.Append("update  " + points.pref + "_data" + tableDelimiter + "dom set nzp_area=" + finder.new_area.nzp_area + ",nzp_geu=" + finder.new_geu.nzp_geu + " ");
#else
			sql.Append("update  " + points.pref+ "_data" + tableDelimiter + "dom set (nzp_area,nzp_geu)=(" + finder.new_area.nzp_area + "," + finder.new_geu.nzp_geu + ") ");
#endif

				sql.Append(" where nzp_dom=" + finder.nzp_dom + " ");
				if (!ExecSQL(conn_db, transaction, sql.ToString(), true).result)
				{

					ret.text = "Ошибка обновления домовых данных";
					ret.result = false;
					MonitorLog.WriteLog("ChangeArea: Ошибка обновления таблицы dom в верхнем банке, sql: " + sql.ToString(), MonitorLog.typelog.Error, true);
					return ret;
				}
			}

			#endregion

			return ret;
		}

        /// <summary>Основная информация по СЗ</summary>
        private class ExSzInfo
        {
            public string DownUserName;
            public string UpUserName;
            public DateTime DownDate;
            public DateTime UpDate;
            public int DownCountLS;
            public int NosyncCountLS;
            public readonly List<string> Points;
            public bool Status;
            public string Comments;

            /// <summary>Наименование загруженного файла</summary>
            public string DownloadedFileName { get; set; }

            /// <summary>Наименование выгружаемого файла</summary>
            public string UploadFileName { get; set; }

            /// <summary>Идентификатор заруженного файла соц.защиты</summary>
            public int NzpExSZ { get; set; }

            /// <summary>Идентификатор пользователя</summary>
            public int IdUser { get; set; }

            /// <summary>Расчетный месяц</summary>
            public int Month { get; private set; }

            /// <summary>Расчетный год</summary>
            public int Year { get; private set; }

            /// <summary>Расчетная дата</summary>
            public string Date { get; private set; }

            /// <summary>Получить коррентную расчтеную дату</summary>
            /// <param name="month">Расчетный месяц</param>
            /// <param name="year">Расчетный год</param>
            /// <returns>Успешно ли выполнилась</returns>
            public bool GetCorrectBillingDate(string month, string year) {
                DateTime date;
                if (!DateTime.TryParse("1." + month.Trim() + "." + year.Trim(), out date)) return false;
                Month = date.Month;
                Year = date.Year;
                Date = date.ToShortDateString();
                return true;
            }

            public ExSzInfo() {
                NzpExSZ = 0;
                DownloadedFileName = "";
                UploadFileName = "";
                IdUser = 0;
                DownDate = DateTime.MinValue;
                DownCountLS = 0;
                NosyncCountLS = 0;
                Points = new List<string>();
            }
        }

        /// <summary>Выгрузить файл по СЗ</summary>
        /// <param name="finder">Объект с дополнительными параметрами</param>
        /// <param name="month">Расчетный месяц</param>
        /// <param name="year">Расчетный год</param>
        /// <param name="nzpExSZ">Идентификатор загруженного файла по СЗ</param>
        /// <param name="isPkodInLs">Указывать в качестве лицевого счета платежный код</param>
        /// <returns>Результат выполнение функции</returns>
        public Returns GetExchangeSZ(Finder finder, string month, string year, int nzpExSZ, bool isPkodInLs) {
            Returns ret = Utils.InitReturns();
            if (finder.nzp_user < 1)
            {
                ret.text = "Не определен пользователь";
                ret.result = false;
                return ret;
            }

            var info = new ExSzInfo { IdUser = finder.nzp_user, NzpExSZ = nzpExSZ };
            Utils.setCulture();
            if (!info.GetCorrectBillingDate(month, year))
            {
                ret.result = false;
                ret.text = "Неверные значения месяца или года для выгрузки";
                return ret;
            }

            #region запись в ExcelUtility

            var excelRepDb = new ExcelRep();
            ret = excelRepDb.AddMyFile(new ExcelUtility
            {
                nzp_user = info.IdUser,
                status = ExcelUtility.Statuses.InProcess,
                rep_name = "Выгрузка для обмена с соц.защитой за " + month + "." + year + "",
                is_shared = 1
            });
            if (!ret.result) return ret;
            int nzpExc = ret.tag; //идентификатор файла

            #endregion

            #region соединение с БД

            IDbConnection connDB = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(connDB, true);
            if (!ret.result) return ret;

            #endregion

            string fileName = string.Empty; //Имя файла отчета
            string dir = InputOutput.useFtp ? InputOutput.GetOutputDir() : Constants.ExcelDir;


            var prefixs = new List<_Point>();
            var perm = new SecurityPermission(SecurityPermissionFlag.UnmanagedCode);
            string prefData = Points.Pref + DBManager.sDataAliasRest,
                    prefKernel = Points.Pref + DBManager.sKernelAliasRest,
                     prefDept = Points.Pref + DBManager.sDebtAliasRest;

            try
            {
                if (Constants.Trace) ClassLog.WriteLog("СЗ.Начала формирование выгрузки.");

                //получить префикс
                string sql = " SELECT p.bd_kernel, p.point, p.nzp_wp " +
                             " FROM " + prefKernel + "s_point p, " +
                                        prefData + "tula_ex_sz_wp sz " +
                             " WHERE sz.nzp_ex_sz = " + nzpExSZ +
                             " AND sz.nzp_wp = p.nzp_wp ";
                DataTable pointTable = ClassDBUtils.OpenSQL(sql, connDB).resultData;
                if (pointTable.Rows.Count > 0)
                {
                    var point = new _Point();
                    foreach (DataRow row in pointTable.Rows)
                    {
                        point.pref = row["bd_kernel"].ToString().Trim();
                        point.nzp_wp = row["nzp_wp"].ToInt();
                        prefixs.Add(point);
                        info.Points.Add(row["point"].ToString().Trim());
                    }
                }

                //проставляем ссылку на БД
                sql = " UPDATE " + prefData + "tula_ex_sz_file t SET nzp_wp = k.nzp_wp" +
                      " FROM " + prefData + "kvar k " +
                      " WHERE k.nzp_kvar = t.nzp_kvar" +
                        " AND t.nzp_wp IS NULL  " +
                        " AND t.nzp_ex_sz = " + nzpExSZ;
                ret = ExecSQL(connDB, sql);
                if (!ret.result) throw new Exception("(#0)GetExchangeSZ: " + ret.text);

                info.DownloadedFileName = GetNameDownloadedFile(connDB, info.NzpExSZ, out ret);
                fileName = DeleteExtension(info.DownloadedFileName);
                info.UploadFileName = fileName + ".dbf";

                //(#132309) в случае, если в файле указана организация с кодом 191, то выводим только кап.ремонт
                bool isKapRemont = fileName.Length > 6 && fileName.Substring(3, 3) == "191";

                const byte countServ = 15; //количество льгот(услуг)
                string datePo = DateTime.DaysInMonth(info.Year, info.Month) + "." + info.Month + "." + (info.Year),
                        dateS = info.Date;
                foreach (var points in prefixs)
                {
                    string localPrefData = points.pref + DBManager.sDataAliasRest;
                    string prefCharge = points.pref + "_charge_" + (info.Year - 2000).ToString("00") + tableDelimiter + "charge_" + info.Month.ToString("00"),
                           prefCalcGku = points.pref + "_charge_" + (info.Year - 2000).ToString("00") + tableDelimiter + "calc_gku_" + info.Month.ToString("00"),
                             prefCounters = points.pref + "_charge_" + (info.Year - 2000).ToString("00") + tableDelimiter + "counters_" + info.Month.ToString("00"),
                                prefPerekidki = points.pref + "_charge_" + (info.Year - 2000).ToString("00") + tableDelimiter + "perekidka",
                                  prefReval = points.pref + "_charge_" + (info.Year - 2000).ToString("00") + tableDelimiter + "reval_" + info.Month.ToString("00"),
                                   gilYY = points.pref + "_charge_" + (info.Year - 2000).ToString("00") + tableDelimiter + "gil_" + info.Month.ToString("00");

                    CreateTempTable(connDB);
                    FillingServicesDirectory(connDB);

                    #region (#1) обновление информации о ЛС в таблице СЗ

                    #region (#1.1) заполнение временной таблицы

                    sql = " INSERT INTO t_sz_prm(id, sl, nzp_kvar, lchet, prk, prz) " +
                          " SELECT id, " +
                                 " sl, " +
                                 " nzp_kvar, " +
                                 " regexp_replace(lchet, '[^0-9]', '', 'g') " + DBManager.sConvToNum + ", " +
                                 " prk, " +
                                 " 1 AS prz " + //признак сопоставления в системе prz
                          " FROM " + prefData + "tula_ex_sz_file " +
                          " WHERE lchet IS NOT NULL " +
                            " AND nzp_kvar IS NOT NULL " +
                            " AND regexp_replace(lchet, '[^0-9]', '', 'g') <> '' " +
                            " AND nzp_wp = " + points.nzp_wp +
                            " AND nzp_ex_sz = " + nzpExSZ;
                    ret = ExecSQL(connDB, sql);
                    if (!ret.result) throw new Exception("(#1.1)GetExchangeSZ: " + ret.text);

                    ExecSQL(connDB, sUpdStat + " t_sz_prm", false);

                    if (isPkodInLs)
                    {
                        //признак изменения ЛС (pkod <> lchet)
                        sql = " UPDATE t_sz_prm SET prk = 1 " +
                              " FROM " + localPrefData + "kvar k " +
                              " WHERE k.nzp_kvar = t_sz_prm.nzp_kvar " +
                                " AND t_sz_prm.lchet <> k.pkod ";
                        ret = ExecSQL(connDB, sql);
                        if (!ret.result) throw new Exception("(#1.1)GetExchangeSZ: " + ret.text);
                    }

                    //кол-во проживающих
                    sql = " UPDATE t_sz_prm SET (kolpr) = (g.cnt1 + g.val3) " +
                          " FROM " + gilYY + " g " +
                          " WHERE g.stek = 3 " +
                            " AND g.nzp_kvar = t_sz_prm.nzp_kvar ";
                    ret = ExecSQL(connDB, sql);
                    if (!ret.result) throw new Exception("(#1.1)GetExchangeSZ: " + ret.text);

                    //количество зарегестрированных
                    sql = " UPDATE t_sz_prm SET (kolzrp) = (g.cnt2) " +
                          " FROM " + gilYY + " g " +
                          " WHERE g.stek = 3 " +
                            " AND g.nzp_kvar = t_sz_prm.nzp_kvar ";
                    ret = ExecSQL(connDB, sql);
                    if (!ret.result) throw new Exception("(#1.1)GetExchangeSZ: " + ret.text);

                    //наличие солашения
                    sql = " UPDATE t_sz_prm SET (is_agreement) = " +
                          " (( CASE WHEN nzp_kvar IN ( SELECT nzp_kvar " +
                                                     " FROM " + prefDept + "deal d INNER JOIN " +
                                                                prefDept + "agreement a ON (a.nzp_deal = d.nzp_deal " +
                                                                                      " AND agr_date <= DATE('" + datePo + "') " +
                                                                                      " AND (agr_date + (agr_month_count || ' month')::interval) " +
                                                                                          " >= DATE('" + dateS + "')) " +
                                                     " WHERE d.nzp_kvar = t_sz_prm.nzp_kvar ) THEN 1 ELSE 0 END ))";
                    ret = ExecSQL(connDB, sql);
                    if (!ret.result) throw new Exception("(#1.1)GetExchangeSZ: " + ret.text);

                    sql = " UPDATE t_sz_prm SET " +
                        //количество комнат
                          " kolk = " + sNvlWord + "(( SELECT MAX(regexp_replace(p1.val_prm, '[^0-9]', '', 'g')" + DBManager.sConvToInt + ") " +
                                                       " FROM " + localPrefData + "prm_1 p1 " +
                                                       " WHERE p1.nzp = t_sz_prm.nzp_kvar " +
                                                         " AND regexp_replace(p1.val_prm, '[^0-9]', '', 'g') <> '' " +
                                                         " AND p1.nzp_prm = 107 " +
                                                         " AND p1.is_actual <> 100 " +
                                                         " AND '" + info.Date + "' BETWEEN p1.dat_s AND p1.dat_po),0), " +
                        //вид жилого фонда
                          " vidgf = " + sNvlWord + "(( SELECT MAX(TRIM(name_y)) " +
                                                        " FROM " + localPrefData + "prm_1 p1 INNER JOIN " +
                                                                   prefKernel + "res_y r ON r.nzp_y = regexp_replace(p1.val_prm, '[^0-9]', '', 'g')" + DBManager.sConvToInt +
                                                        " WHERE p1.nzp = t_sz_prm.nzp_kvar " +
                                                          " AND regexp_replace(p1.val_prm, '[^0-9]', '', 'g') <> '' " +
                                                          " AND p1.nzp_prm = 110 " +
                                                          " AND r.nzp_res = 22 " +
                                                          " AND p1.is_actual <> 100 " +
                                                         " AND '" + info.Date + "' BETWEEN p1.dat_s AND p1.dat_po ),''), " +
                        //приватизация
                          " privat = " + sNvlWord + "(( SELECT MAX((CASE WHEN p1.val_prm = '1' THEN 'Д' ELSE 'Н' END)) " +
                                                        " FROM " + localPrefData + "prm_1 p1 " +
                                                        " WHERE p1.nzp = t_sz_prm.nzp_kvar " +
                                                          " AND p1.nzp_prm = 8 " +
                                                          " AND p1.is_actual <> 100 " +
                                                         " AND '" + info.Date + "' BETWEEN p1.dat_s AND p1.dat_po),''), " +
                        //общая площадь
                          " oplj = " + sNvlWord + "(( SELECT MAX(regexp_replace(p1.val_prm, '[^0-9.]', '', 'g') " + DBManager.sConvToNum + ") " +
                                                      " FROM " + localPrefData + "prm_1 p1 " +
                                                      " WHERE p1.nzp = t_sz_prm.nzp_kvar " +
                                                        " AND regexp_replace(p1.val_prm, '[^0-9]', '', 'g') <> '' " +
                                                        " AND p1.nzp_prm = 4 " +
                                                        " AND p1.is_actual <> 100 " +
                                                       " AND '" + info.Date + "' BETWEEN p1.dat_s AND p1.dat_po ),0), " +
                        //отапливаемая площадь
                        //в поля OPLJ и OTPLJ выгружать общую площадь!!! - требование заказчика GKHKPFIVE-10274
                          " otplj = " + sNvlWord + "(( SELECT MAX(regexp_replace(p1.val_prm, '[^0-9.]', '', 'g') " + DBManager.sConvToNum + ") " +
                                                       " FROM " + localPrefData + "prm_1 p1 " +
                                                       " WHERE p1.nzp = t_sz_prm.nzp_kvar " +
                                                         " AND regexp_replace(p1.val_prm, '[^0-9]', '', 'g') <> '' " +
                                                         " AND p1.nzp_prm = 4 " + // было 133
                                                         " AND p1.is_actual <> 100 " +
                                                        " AND '" + info.Date + "' BETWEEN p1.dat_s AND p1.dat_po ),0) ";
                    ret = ExecSQL(connDB, sql);
                    if (!ret.result) throw new Exception("(#1.1)GetExchangeSZ: " + ret.text);

                    #endregion

                    #region (#1.2) обновление основной таблицы

                    sql = " UPDATE " + prefData + "tula_ex_sz_file SET " +
                          " (prk) = (t.prk), " +
                          " (kolpr) = (t.kolpr), " +
                          " (kolk) = (CASE WHEN " + prefData + "tula_ex_sz_file.kolk IS NULL " +
                                         " THEN t.kolk ELSE " + prefData + "tula_ex_sz_file.kolk END), " +
                          " (vidgf) = (t.vidgf), " +
                          " (privat) = (t.privat), " +
                          " (kolzrp) = (t.kolzrp), " +
                          " (oplj) = (t.oplj), " +
                          " (otplj) = (t.otplj), " +
                          " (prz) = (t.prz) " +
                          " FROM t_sz_prm t " +
                          " WHERE t.id = " + prefData + "tula_ex_sz_file.id" +
                            " AND nzp_ex_sz = " + nzpExSZ;
                    ret = ExecSQL(connDB, sql);
                    if (!ret.result) throw new Exception("(#1.2)GetExchangeSZ: " + ret.text);

                    //обнуление prz(Признак корректировки записи)
                    sql = " UPDATE " + prefData + "tula_ex_sz_file SET prz = 0 " +
                          " WHERE prz IS NULL " +
                            " AND nzp_ex_sz = " + nzpExSZ;
                    ret = ExecSQL(connDB, sql);
                    if (!ret.result) throw new Exception("(#1.2)GetExchangeSZ: " + ret.text);

                    //указан ли флажек "Указывать в качестве лицевого счета платежный код"
                    if (isPkodInLs)
                    {
                        sql = " UPDATE " + prefData + "tula_ex_sz_file SET lchet = pkod " +
                              " FROM " + prefData + "kvar k " +
                              " WHERE k.nzp_kvar = " + prefData + "tula_ex_sz_file.nzp_kvar " +
                                " AND prk = 1 " +
                                " AND nzp_ex_sz = " + nzpExSZ;
                        ret = ExecSQL(connDB, sql);
                        if (!ret.result) throw new Exception("(#1.2)GetExchangeSZ: " + ret.text);
                    }

                    #region (#1.2.1)проверка лицевых счетов

                    //запись в протокол дублирующих лицевых счетов
                    sql = " SELECT lchet " +
                          " FROM t_sz_prm " +
                          " WHERE lchet IS NOT NULL AND sl = 1 " +
                          " GROUP BY 1 " +
                          " HAVING COUNT(lchet) > 1";
                    DataTable repeatLS = ClassDBUtils.OpenSQL(sql, connDB).resultData;
                    if (repeatLS.Rows.Count > 0)
                    {
                        for (int c = 0; c < repeatLS.Rows.Count; c++)
                        {
                            info.Comments += "Лицевой счет с уникальным полем LCHET=" +
                                                (repeatLS.Rows[c]["lchet"] != DBNull.Value ? Convert.ToString(repeatLS.Rows[c]["lchet"]).Trim() : string.Empty) +
                                                " встречается в файле больше одного раза; \r\n";
                        }
                    }

                    #endregion

                    #endregion

                    #region (#1.3) обновление параметра "Старый лицевой счет"
                    //закрываем старый параметр 2004 в prm_1
                    string datNow = "'" + new DateTime(Points.CalcMonth.year_, Points.CalcMonth.month_, 1).ToShortDateString() + "'"; //начало  тек.расчетного месяца
                    string datPrev = "'" + new DateTime(Points.CalcMonth.year_, Points.CalcMonth.month_, 1).AddDays(-1).ToShortDateString() + "'"; //конец пред.расчетного месяца

                    //закрываем параметры с dat_s <= конец пред.расчетного месяца
                    sql = " UPDATE " + localPrefData + "prm_1 set (dat_po) = (" + datPrev + ") " +
                          " WHERE nzp_prm = 2004 AND nzp IN (SELECT nzp_kvar " +
                                                           " FROM t_sz_prm " +
                                                           " WHERE prk = 1) " +
                            " AND is_actual <> 100 AND dat_s <= " + datPrev;
                    ret = ExecSQL(connDB, sql);
                    if (!ret.result) throw new Exception("(#1.3)GetExchangeSZ: " + ret.text);

                    //закрываем параметры с dat_s > конец пред.расчетного месяца
                    sql = " UPDATE " + localPrefData + "prm_1 SET (dat_s,dat_po) = (" + datPrev + "," + datPrev + ")" +
                          " WHERE nzp_prm = 2004 AND nzp IN (SELECT nzp_kvar " +
                                                           " FROM t_sz_prm " +
                                                           " WHERE prk = 1) " +
                            " AND is_actual <> 100 " +
                            " AND dat_s > " + datPrev;
                    ret = ExecSQL(connDB, sql);
                    if (!ret.result) throw new Exception("(#1.3)GetExchangeSZ: " + ret.text);

                    //открываем новые параметры 
                    sql = " INSERT INTO " + localPrefData + "prm_1 (nzp, nzp_prm, dat_s, dat_po, val_prm, is_actual,dat_when,nzp_user)" +
                          " SELECT sz.nzp_kvar, 2004, " + datNow + ",'01.01.3000', kv.pkod, 1, " + sCurDate + ", " + info.IdUser +
                          " FROM t_sz_prm sz, " + localPrefData + "kvar kv " +
                          " WHERE kv.nzp_kvar = sz.nzp_kvar " +
                            " AND sz.prk = 1";
                    ret = ExecSQL(connDB, sql);
                    if (!ret.result) throw new Exception("(#1.3)GetExchangeSZ: " + ret.text);
                    #endregion

                    #endregion

                    #region (#2) обновление информации об услугам(льготам) в таблице СЗ

                    // фильтрационная таблица
                    sql = " INSERT INTO t_sz_charge_filter(nzp_charge, nzp_kvar, nzp_serv, isdel) " +
                          " SELECT nzp_charge, " +
                                 " nzp_kvar, " +
                                 " nzp_serv, " +
                                 " isdel " +
                          " FROM " + prefCharge + " g " +
                          " WHERE isdel = 0 " + (isKapRemont ? " AND nzp_serv = 206 " : string.Empty) +
                            " AND dat_charge IS NULL " +
                            " AND nzp_serv IN (SELECT nzp_serv FROM t_sz_services) " +
                            " AND nzp_kvar IN (SELECT nzp_kvar FROM t_sz_prm) ";
                    ret = ExecSQL(connDB, sql);
                    if (!ret.result) throw new Exception("(#2)GetExchangeSZ: " + ret.text);

                    sql = " INSERT INTO t_sz_charge_filter(nzp_charge, nzp_kvar, nzp_serv, isdel) " +
                          " SELECT nzp_charge, " +
                                 " nzp_kvar, " +
                                 " nzp_serv, " +
                                 " isdel " +
                          " FROM " + prefCharge + " g " +
                          " WHERE isdel = 1 AND nzp_serv = 8 " +
                            " AND dat_charge IS NULL " +
                            " AND nzp_serv IN (SELECT nzp_serv FROM t_sz_services) " +
                            " AND nzp_kvar IN (SELECT nzp_kvar FROM t_sz_prm) ";
                    ret = ExecSQL(connDB, sql);
                    if (!ret.result) throw new Exception("(#2)GetExchangeSZ: " + ret.text);

                    ExecSQL(connDB, sUpdStat + " t_sz_charge_filter", false);

                    // основаная временная таблица
                    sql = " INSERT INTO t_sz_charge(nzp_kvar, nzp_serv, nzp_supp, nzp_frm, is_device, " +
                                                    " tarif_ch, sum_tarif, reval, rsum_tarif, sum_nedop, sum_money, sum_insaldo) " +
                              " SELECT nzp_kvar, " +
                                     " nzp_serv, " +
                                     " nzp_supp, " +
                                     " nzp_frm, " +
                                     " is_device, " +
                                     " tarif AS tarif_ch, " +
                                     " sum_tarif, " +
                                     " reval, " +
                                     " rsum_tarif, " +
                                     " sum_nedop, " +
                                     " sum_money, " +
                                     " sum_insaldo " +
                              " FROM " + prefCharge + " gc " +
                              " WHERE nzp_charge IN (SELECT " + DBManager.First1 + " nzp_charge " +
                                                   " FROM t_sz_charge_filter t " +
                                                   " WHERE t.nzp_kvar = gc.nzp_kvar " +
                                                     " AND t.nzp_serv = gc.nzp_serv " +
                                                     " AND t.isdel = 0 " +
                                                   " ORDER BY nzp_charge DESC " + DBManager.Limit1 + ") ";
                    ret = ExecSQL(connDB, sql);
                    if (!ret.result) throw new Exception("(#2)GetExchangeSZ: " + ret.text);

                    ExecSQL(connDB, sUpdStat + " t_sz_charge", false);

                    #region (#2.1) sum - сумма начислений

                    ExecSQL(connDB, "DROP TABLE t_sz_sum_charge");
                    sql = " SELECT c.nzp_kvar, c.nzp_serv, c.nzp_supp, " +
                                 " SUM(c.sum_tarif + " + sNvlWord + "(c.reval,0)) AS sum " +
                          " INTO TEMP t_sz_sum_charge " +
                          " FROM " + prefCharge + " c INNER JOIN t_sz_charge_filter t ON t.nzp_charge = c.nzp_charge " +
                          " GROUP BY 1,2,3 ";
                    ret = ExecSQL(connDB, sql);
                    if (!ret.result) throw new Exception("(#2.1)GetExchangeSZ: " + ret.text);

                    ExecSQL(connDB, "CREATE INDEX ix_t_sz_sum_charge_1 ON t_sz_sum_charge(nzp_kvar, nzp_serv, nzp_supp, sum)");

                    //плюс корректировка начислений и корректировка расхода к полю sum
                    sql = " UPDATE t_sz_sum_charge SET sum = sum + " + DBManager.sNvlWord +
                          " ((SELECT SUM(sum_rcl)  " +
                            " FROM " + prefPerekidki + " c " +
                            " WHERE c.nzp_serv = t_sz_sum_charge.nzp_serv " +
                              " AND c.nzp_kvar = t_sz_sum_charge.nzp_kvar " +
                              " AND c.nzp_supp = t_sz_sum_charge.nzp_supp " +
                              " AND c.type_rcl in (102, 163) " +
                              " AND month_ = " + info.Month + "),0)  ";
                    ret = ExecSQL(connDB, sql);
                    if (!ret.result) throw new Exception("(#2.1)GetExchangeSZ: " + ret.text);

                    sql = " UPDATE t_sz_charge t SET sum = " +
                          " (SELECT SUM(c.sum) " +
                           " FROM t_sz_sum_charge c " +
                           " WHERE c.nzp_kvar = t.nzp_kvar " +
                             " AND c.nzp_serv = t.nzp_serv) ";
                    ret = ExecSQL(connDB, sql);
                    if (!ret.result) throw new Exception("(#2.1)GetExchangeSZ: " + ret.text);

                    #endregion

                    #region (#2.2) koef - поправочный коэфициент

                    sql = " UPDATE t_sz_charge SET koef = (CASE WHEN rsum_tarif <> 0 THEN (1 - sum_nedop / rsum_tarif) ELSE 0 END) ";
                    ret = ExecSQL(connDB, sql);
                    if (!ret.result) throw new Exception("(#2.2)GetExchangeSZ: " + ret.text);

                    #endregion

                    #region (#2.3) ozs - наличие соглашение по оплате задолженности

                    sql = " UPDATE t_sz_charge SET ozs = (CASE WHEN nzp_kvar IN (SELECT nzp_kvar " +
                                                                               " FROM t_sz_prm " +
                                                                               " WHERE is_agreement = 1) " +
                                                             " THEN (CASE WHEN sum_money > 0 THEN 1 ELSE 0 END)  " +
                                                             " ELSE (CASE WHEN sum_money = 0 THEN 2 " +
                                                                        " WHEN sum_insaldo - sum_money <= 0 THEN 3 " +
                                                                        " WHEN rsum_tarif - sum_money > 0 THEN 4 END) END) ";
                    ret = ExecSQL(connDB, sql);
                    if (!ret.result) throw new Exception("(#2.3)GetExchangeSZ: " + ret.text);

                    #endregion

                    #region (#2.4) tarif - сумма тарифа

                    //признак изменения тарифа
                    sql = " UPDATE t_sz_charge t SET change_tarif = " +
                          " (CASE WHEN (SELECT COUNT(*)" +
                                      " FROM " + prefReval + " r" +
                                      " WHERE r.nzp_kvar = t.nzp_kvar " +
                                        " AND r.nzp_supp = t.nzp_supp " +
                                        " AND r.nzp_serv = t.nzp_serv " +
                                        " AND ABS(r.tarif_p - r.tarif) > 0.001 ) > 0 THEN 1 ELSE 0 END) ";
                    ret = ExecSQL(connDB, sql);
                    if (!ret.result) throw new Exception("(#2.4)GetExchangeSZ: " + ret.text);

                    sql = " INSERT INTO t_sz_tarif8(nzp_kvar) " +
                          " SELECT nzp_kvar FROM t_sz_charge GROUP BY 1 ORDER BY 1 ";
                    ret = ExecSQL(connDB, sql);
                    if (!ret.result) throw new Exception("(#2.4)GetExchangeSZ: " + ret.text);
                    ExecSQL(connDB, sUpdStat + " t_sz_tarif8", false);

                    //тариф по услуги отопление (Гкал)
                    sql = " UPDATE t_sz_tarif8 SET tarif = c.trf1 " +
                          " FROM " + prefCalcGku + " c " +
                          " WHERE c.nzp_serv = 8 " +
                            " AND c.nzp_kvar = t_sz_tarif8.nzp_kvar " +
                            " AND c.nzp_frm IN (SELECT nzp_frm " +
                                              " FROM " + prefKernel + "formuls " +
                                              " WHERE nzp_measure = 4) " + // nzp_measure = 4 (гКал)
                            " AND c.dat_charge IS NULL ";
                    ret = ExecSQL(connDB, sql);
                    if (!ret.result) throw new Exception("(#2.4)GetExchangeSZ: " + ret.text);

                    //тариф по услуги отопление по квартирному параметру
                    sql = " UPDATE t_sz_tarif8 SET tarif = val_prm " + DBManager.sConvToNum + "(15,5) " +
                          " FROM " + localPrefData + "prm_1 p " +
                          " WHERE p.is_actual <> 100 " +
                            " AND p.nzp_prm = 341 " + //ЭОТ-ЛС Ставка за нагрев отопления на 1 ГКал
                            " AND p.nzp = t_sz_tarif8.nzp_kvar " +
                            " AND p.dat_s <= DATE('" + datePo + "') " +
                            " AND p.dat_po >= DATE('" + dateS + "') " +
                            " AND (t_sz_tarif8.tarif = 0 OR t_sz_tarif8.tarif IS NULL) ";
                    ret = ExecSQL(connDB, sql);
                    if (!ret.result) throw new Exception("(#2.4)GetExchangeSZ: " + ret.text);

                    //тариф по услуги отопление по домовому параметру
                    sql = " UPDATE t_sz_tarif8 SET tarif = val_prm " + DBManager.sConvToNum + "(15,5) " +
                          " FROM " + localPrefData + "prm_2 p " +
                          " WHERE p.is_actual <> 100 " +
                            " AND p.nzp_prm = 1062 " + //ЭОТ-Дом Стоимость 1 ГКал
                            " AND p.nzp IN (SELECT nzp_dom FROM " + localPrefData + "kvar k WHERE k.nzp_kvar = t_sz_tarif8.nzp_kvar) " +
                            " AND p.dat_s <= DATE('" + datePo + "') " +
                            " AND p.dat_po >= DATE('" + dateS + "') " +
                            " AND (t_sz_tarif8.tarif = 0 OR t_sz_tarif8.tarif IS NULL) ";
                    ret = ExecSQL(connDB, sql);
                    if (!ret.result) throw new Exception("(#2.4)GetExchangeSZ: " + ret.text);

                    //тариф по услуги отопление (кв.м.)
                    sql = " UPDATE t_sz_tarif8 t SET tarif = tarif_ch " +
                          " FROM t_sz_charge c " +
                          " WHERE c.nzp_kvar = t.nzp_kvar " +
                            " AND c.nzp_serv = 8 " +
                            " AND (t.tarif = 0 OR t.tarif IS NULL) ";
                    ret = ExecSQL(connDB, sql);
                    if (!ret.result) throw new Exception("(#2.4)GetExchangeSZ: " + ret.text);

                    //тариф по услугам по отоплению
                    sql = " UPDATE t_sz_charge SET tarif = t.tarif " +
                          " FROM t_sz_tarif8 t " +
                          " WHERE t.nzp_kvar = t_sz_charge.nzp_kvar " +
                            " AND t_sz_charge.nzp_serv = 8 " +
                            " AND t_sz_charge.change_tarif = 0 "; // тариф не менялся
                    ret = ExecSQL(connDB, sql);
                    if (!ret.result) throw new Exception("(#2.4)GetExchangeSZ: " + ret.text);

                    //тариф по услугам, кроме отопления
                    sql = " UPDATE t_sz_charge t SET tarif = tarif_ch " +
                          " WHERE t.nzp_serv <> 8 " +
                            " AND t.change_tarif = 0 "; // тариф не менялся
                    ret = ExecSQL(connDB, sql);
                    if (!ret.result) throw new Exception("(#2.4)GetExchangeSZ: " + ret.text);

                    sql = " UPDATE t_sz_charge SET tarif = (CASE WHEN sum_tarif <> 0 THEN sum * tarif_ch / sum_tarif ELSE 0 END) " +
                          " WHERE t_sz_charge.change_tarif = 1 "; // тариф изменился
                    ret = ExecSQL(connDB, sql);
                    if (!ret.result) throw new Exception("(#2.4)GetExchangeSZ: " + ret.text);

                    // тариф по услуге "Уборка МОП" 
                    sql = " UPDATE t_sz_charge tc SET tarif = (CASE WHEN oplj > 0 THEN sum / oplj ELSE 0 END) " +
                          " FROM t_sz_prm tp " +
                          " WHERE tp.nzp_kvar = tc.nzp_kvar " +
                            " AND tc.nzp_serv = 17 ";
                    ret = ExecSQL(connDB, sql);
                    if (!ret.result) throw new Exception("(#2.4)GetExchangeSZ: " + ret.text);

                    #endregion

                    #region (#2.5) fakt - фактическое потребление ЖКУ

                    //fakt вычисляется в функции объединение полей UnionServices;

                    #endregion

                    #region (#2.6) norm - норматив потребления

                    sql = " UPDATE t_sz_charge SET norm = c.rash_norm_one " +
                          " FROM " + prefCounters + " c " +
                          " WHERE c.nzp_serv = t_sz_charge.nzp_serv " +
                            " AND c.nzp_kvar = t_sz_charge.nzp_kvar " +
                            " AND stek = 3 ";
                    ret = ExecSQL(connDB, sql);
                    if (!ret.result) throw new Exception("(#2.6)GetExchangeSZ: " + ret.text);

                    #endregion

                    // таблица с долгами
                    sql = " INSERT INTO t_sz_dolg (nzp_kvar, nzp_serv, sum_insaldo, sum_money, sum_z, sum_tarif) " +
                          " SELECT nzp_kvar, " +
                                 " nzp_serv, " +
                                 " SUM(sum_insaldo), " +
                                 " SUM(sum_money), " +
                                 " SUM(sum_insaldo) - SUM(sum_money), " +
                                 " SUM(sum_tarif)" +
                          " FROM " + prefCharge +
                          " WHERE nzp_charge IN (SELECT nzp_charge FROM t_sz_charge_filter) " +
                            " AND isdel = 0 " +
                          " GROUP BY nzp_kvar, nzp_serv" +
                          " HAVING SUM(sum_tarif) > 0 " +
                             " AND SUM(sum_money) < SUM(sum_insaldo) ";
                    ret = ExecSQL(connDB, sql);
                    if (!ret.result) throw new Exception("(#2)GetExchangeSZ: " + ret.text);

                    ExecSQL(connDB, sUpdStat + " t_sz_dolg", false);

                    #region (#2.7) sumz - сумма задолженности

                    sql = " UPDATE t_sz_charge SET sumz = (CASE WHEN t.sum_z < 0 THEN 0 ELSE t.sum_z END) " +
                          " FROM t_sz_dolg t " +
                          " WHERE t.nzp_kvar = t_sz_charge.nzp_kvar" +
                            " AND t.nzp_serv = t_sz_charge.nzp_serv ";
                    ret = ExecSQL(connDB, sql);
                    if (!ret.result) throw new Exception("(#2.7)GetExchangeSZ: " + ret.text);

                    #endregion

                    #region (#2.8) sumozs - сумма оплаты задолженности по соглашению

                    sql = " UPDATE t_sz_charge SET sumozs = t.sum_money " +
                          " FROM t_sz_dolg t " +
                          " WHERE t.nzp_kvar = t_sz_charge.nzp_kvar " +
                            " AND t.nzp_serv = t_sz_charge.nzp_serv " +
                            " AND t_sz_charge.nzp_kvar IN (SELECT p.nzp_kvar " +
                                                         " FROM t_sz_prm p" +
                                                         " WHERE p.is_agreement = 1) ";
                    ret = ExecSQL(connDB, sql);
                    if (!ret.result) throw new Exception("(#2.8)GetExchangeSZ: " + ret.text);

                    #endregion

                    #region (#2.9) klmz - количество месяцев задолженности

                    sql = " UPDATE t_sz_charge SET klmz = sumz / t.sum_tarif" +
                          " FROM  t_sz_dolg t " +
                          " WHERE t.nzp_kvar = t_sz_charge.nzp_kvar " +
                            " AND t.nzp_serv = t_sz_charge.nzp_serv " +
                            " AND sumz > 0 ";
                    ret = ExecSQL(connDB, sql);
                    if (!ret.result) throw new Exception("(#2.9)GetExchangeSZ: " + ret.text);

                    #endregion

                    #region (#2.10) org - поставщик

                    sql = " UPDATE t_sz_charge SET org = " + DBManager.SetSubString("name_supp", "0", "30") +
                          " FROM " + prefKernel + "supplier s " +
                          " WHERE s.nzp_supp = t_sz_charge.nzp_supp ";
                    ret = ExecSQL(connDB, sql);
                    if (!ret.result) throw new Exception("(#2.10)GetExchangeSZ: " + ret.text);

                    #endregion

                    #region (#2.11) vidtar - вид тарифа

                    sql = " UPDATE t_sz_charge SET vidtar = (CASE WHEN is_device > 0 THEN 1 ELSE 0 END) ";
                    ret = ExecSQL(connDB, sql);
                    if (!ret.result) throw new Exception("(#2.11)GetExchangeSZ: " + ret.text);

                    #endregion

                    #region (#2.12) lchet - Лицевой счет(платежный код)

                    sql = " UPDATE t_sz_charge SET lchet = k.pkod " +
                          " FROM " + localPrefData + "kvar k " +
                          " WHERE k.nzp_kvar = t_sz_charge.nzp_kvar ";
                    ret = ExecSQL(connDB, sql);
                    if (!ret.result) throw new Exception("(#2.12)GetExchangeSZ: " + ret.text);

                    #endregion

                    //TransferServiceHotWater(connDB);

                    #region (#2.13) gku - наименование ЖКУ

                    sql = " INSERT INTO t_sz_gku(nzp_kvar, nzp_serv, nzp_prm, nzp_res) " +
                          " SELECT c.nzp_kvar, c.nzp_serv, nzp_prm, nzp_res " +
                          " FROM t_sz_charge c INNER JOIN t_sz_services s ON s.nzp_serv = c.nzp_serv " +
                          " WHERE c.nzp_serv NOT IN (5, 15, 510, 515) ";
                    ret = ExecSQL(connDB, sql);
                    if (!ret.result) throw new Exception("(#2.13)GetExchangeSZ: " + ret.text);

                    ExecSQL(connDB, sUpdStat + " t_sz_gku", false);

                    sql = " UPDATE t_sz_gku t SET nzp_y = " +
                          " (SELECT " + DBManager.First1 + " p.val_prm " + DBManager.sConvToInt + " " +
                           " FROM " + localPrefData + "kvar kv INNER JOIN " + localPrefData + "prm_2 p ON p.nzp = kv.nzp_dom " +
                                                                                                    " AND p.nzp_prm = t.nzp_prm " +
                                                                                                    " AND p.is_actual <> 100 " +
                                                                                                    " AND p.dat_s <= DATE('" + datePo + "') " +
                                                                                                    " AND p.dat_po >= DATE('" + dateS + "') " +
                           " WHERE kv.nzp_kvar = t.nzp_kvar " +
                           " ORDER BY dat_s DESC, dat_po DESC " + DBManager.Limit1 + ") ";
                    ret = ExecSQL(connDB, sql);
                    if (!ret.result) throw new Exception("(#2.13)GetExchangeSZ: " + ret.text);

                    sql = " UPDATE t_sz_charge c SET gku = TRIM(r.name_y) " +
                          " FROM t_sz_gku t INNER JOIN " + prefKernel + " res_y r ON r.nzp_res = t.nzp_res " +
                                                                               " AND r.nzp_y = t.nzp_y " +
                          " WHERE t.nzp_kvar = c.nzp_kvar " +
                            " AND t.nzp_serv = c.nzp_serv ";
                    ret = ExecSQL(connDB, sql);
                    if (!ret.result) throw new Exception("(#2.13)GetExchangeSZ: " + ret.text);

                    sql = " UPDATE t_sz_charge SET gku = (CASE nzp_serv WHEN 5 THEN 'Содержание лифта' " +
                                                                      " WHEN 510 THEN '1.1. Холодная вода ОДН' " +
                                                                      " WHEN 515 THEN '1.1. Электроэнергия ОДН.' " +
                                                                      " WHEN 15 THEN 'Социальный найм' END) " +
                          " WHERE nzp_serv IN (5, 15, 510, 515) ";
                    ret = ExecSQL(connDB, sql);
                    if (!ret.result) throw new Exception("(#2.13)GetExchangeSZ: " + ret.text);

                    sql = " UPDATE t_sz_charge c SET gku = TRIM(service) " +
                          " FROM t_sz_services s " +
                          " WHERE c.nzp_serv = s.nzp_serv " +
                            " AND (c.gku IS NULL OR c.gku = '') ";
                    ret = ExecSQL(connDB, sql);
                    if (!ret.result) throw new Exception("(#2.13)GetExchangeSZ: " + ret.text);

                    #endregion

                    #region (#2.14) Объединение услуг

                    UnionServices(connDB);

                    #endregion

                    //выборка услуг с сортировкой
                    sql = " SELECT ch.nzp_serv, ts.ord, p.name_prm  " +
                          " FROM t_sz_charge ch INNER JOIN t_sz_services ts ON ts.nzp_serv = ch.nzp_serv " +
                                              " INNER JOIN t_sz_prm t ON t.nzp_kvar = ch.nzp_kvar " +
                                              " LEFT OUTER JOIN " + prefKernel + "prm_name p ON p.nzp_prm = ts.nzp_prm  " +
                          " GROUP BY ch.nzp_serv, ts.ord, p.name_prm " +
                          " ORDER BY ts.ord ";

                    DataTable resTable = ClassDBUtils.OpenSQL(sql, connDB).resultData;

                    #region (#2.15) обновляем данные по услугам

                    foreach (DataRow row in resTable.Rows)
                    {
                        var ord = row["ord"].To<int>();
                        var nzpServ = row["nzp_serv"].To<int>();
                        var namePrm = row["name_prm"].To<string>().Trim();

                        sql = " UPDATE " + prefData + "tula_ex_sz_file t SET " +
                              " (gku" + ord + ") = (TRIM(c.gku)), " +
                              " (tarif" + ord + ") = (c.tarif ), " +
                              " (sum" + ord + ") = (c.sum), " +
                              " (fakt" + ord + ") = (c.fakt), " +
                              " (norm" + ord + ") = (c.norm), " +
                              " (org" + ord + ") = (TRIM(c.org)), " +
                              " (lchet" + ord + ") = (TRIM(c.lchet)), " +
                              " (sumz" + ord + ") = (c.sumz), " +
                              " (klmz" + ord + ") = (c.klmz), " +
                              " (koef" + ord + ") = (c.koef), " +
                              " (ozs" + ord + ") = (c.ozs), " +
                              " (sumozs" + ord + ") = (c.sumozs), " +
                              " (vidtar" + ord + ") = (c.vidtar) " +
                              " FROM t_sz_charge c " +
                              " WHERE t.nzp_kvar = c.nzp_kvar " +
                                " AND t.nzp_ex_sz = " + nzpExSZ +
                                " AND c.nzp_serv = " + nzpServ;
                        ret = ExecSQL(connDB, sql);
                        if (!ret.result) throw new Exception("(#2.15)GetExchangeSZ: " + ret.text);

                        sql = " SELECT TRIM(nasp) ||', ' || TRIM(nylic) || ', д. ' || TRIM(ndom) AS dom  " +
                              " FROM " + prefData + "tula_ex_sz_file " +
                              " WHERE TRIM(gku" + ord + ") IN (SELECT TRIM(service) FROM t_sz_services) " +
                                " AND nzp_ex_sz = " + nzpExSZ +
                              " GROUP BY 1 ";
                        DataTable tt = ClassDBUtils.OpenSQL(sql, connDB).resultData;
                        if (tt.Rows.Count > 0)
                        {
                            for (int i = 0; i < tt.Rows.Count; i++)
                            {
                                info.Comments += "У дома по адресу: " +
                                                 (tt.Rows[i]["dom"] != DBNull.Value
                                                     ? Convert.ToString(tt.Rows[i]["dom"]).Trim()
                                                     : string.Empty) +
                                                 " не указан параметр \"" + namePrm + "\" \r\n";
                            }
                        }
                    }

                    #endregion

                    // prn если счетчик есть в counters_spis и начисления в counters =0  то prn=1 только для nzp_serv=6
                    sql = " UPDATE " + prefData + "tula_ex_sz_file SET (prn) = (( " +
                           " CASE WHEN fakt1 = 0 THEN 1 ELSE 0 END))  " +
                          " WHERE nzp_ex_sz = " + nzpExSZ +
                            " AND nzp_kvar IN (SELECT nzp_kvar FROM t_sz_prm) ";

                    ret = ExecSQL(connDB, sql);
                    if (!ret.result) throw new Exception("(#2)GetExchangeSZ: " + ret.text);

                    #endregion
                }

                #region (#3) формирование dbf-файла

                #region (#3.1) создание dbf-файла

                var eDBF = new exDBF(fileName);
                eDBF.AddColumn("SL", typeof(decimal), 1, 0);
                eDBF.AddColumn("FAMIL", typeof(string), 50, 0);
                eDBF.AddColumn("IMJA", typeof(string), 50, 0);
                eDBF.AddColumn("OTCH", typeof(string), 50, 0);
                eDBF.AddColumn("DROG", typeof(DateTime), 0, 0);
                eDBF.AddColumn("STRAHNM", typeof(string), 14, 0);
                eDBF.AddColumn("NASP", typeof(string), 50, 0);
                eDBF.AddColumn("NYLIC", typeof(string), 50, 5);
                eDBF.AddColumn("NDOM", typeof(string), 7, 0);
                eDBF.AddColumn("NKORP", typeof(string), 3, 0);
                eDBF.AddColumn("NKW", typeof(string), 15, 0);
                eDBF.AddColumn("NKOMN", typeof(string), 15, 0);
                eDBF.AddColumn("KOLK", typeof(decimal), 2, 0);
                eDBF.AddColumn("LCHET", typeof(string), 24, 0);
                eDBF.AddColumn("VIDGF", typeof(string), 25, 0);
                eDBF.AddColumn("PRIVAT", typeof(string), 1, 0);
                eDBF.AddColumn("OPL", typeof(decimal), 6, 2);
                eDBF.AddColumn("OTPL", typeof(decimal), 6, 2);
                eDBF.AddColumn("OPLJ", typeof(decimal), 6, 2);
                eDBF.AddColumn("OTPLJ", typeof(decimal), 6, 2);
                eDBF.AddColumn("KOLZR", typeof(decimal), 2, 0);
                eDBF.AddColumn("KOLZRP", typeof(decimal), 2, 0);
                eDBF.AddColumn("KOLPR", typeof(decimal), 2, 0);
                eDBF.AddColumn("PRZ", typeof(decimal), 1, 0);
                eDBF.AddColumn("PRN", typeof(decimal), 1, 0);
                eDBF.AddColumn("PRK", typeof(decimal), 1, 0);

                //данные по услугам
                for (int j = 1; j <= countServ; j++)
                {
                    eDBF.AddColumn("GKU" + j, typeof(string), 100, 0);
                    eDBF.AddColumn("TARIF" + j, typeof(decimal), 10, 5);
                    eDBF.AddColumn("SUM" + j, typeof(decimal), 10, 4);
                    eDBF.AddColumn("FAKT" + j, typeof(decimal), 10, 4);
                    eDBF.AddColumn("NORM" + j, typeof(decimal), 10, 4);
                    eDBF.AddColumn("SUMZ" + j, typeof(decimal), 10, 4);
                    eDBF.AddColumn("KLMZ" + j, typeof(decimal), 10, 0);
                    eDBF.AddColumn("OZS" + j, typeof(decimal), 10, 0);
                    eDBF.AddColumn("SUMOZS" + j, typeof(decimal), 10, 4);
                    eDBF.AddColumn("ORG" + j, typeof(string), 30, 0);
                    eDBF.AddColumn("VIDTAR" + j, typeof(decimal), 10, 0);
                    eDBF.AddColumn("KOEF" + j, typeof(decimal), 10, 2);
                    eDBF.AddColumn("LCHET" + j, typeof(string), 24, 0);
                }
                perm.Assert();
                eDBF.Save(dir, 866);

                if (Constants.Trace) ClassLog.WriteLog("Создание файла");

                string strFilePath = Path.GetFullPath(String.Format("{0}\\{1}.DBF", dir, fileName));

                #endregion

                #region (#3.2) запись в dbf-файл

                string selectServ = string.Empty;
                for (int i = 1; i <= countServ; i++)
                    selectServ += string.Format(" gku{0}, tarif{0}, sum{0}, fakt{0}, norm{0}, sumz{0}, klmz{0}, ozs{0}, sumozs{0}, org{0}, vidtar{0}, koef{0}, lchet{0},", i);

                //выбираем данные  для записи в файл
                sql = " SELECT  sl, famil, imja, otch, drog, strahnm, nasp, nylic, ndom, nkorp, nkw, nkomn, " +
                              " kolk, lchet, vidgf, privat, opl, otpl, oplj, otplj, kolzr, kolzrp, kolpr, prz, prn, prk, " + selectServ.TrimEnd(',') +
                      " FROM " + prefData + "tula_ex_sz_file WHERE nzp_ex_sz = " + nzpExSZ;

                DataTable dt = ClassDBUtils.OpenSQL(sql, connDB).resultData;

                if (Constants.Trace) ClassLog.WriteLog("основной селект");

                sql = "SELECT COUNT(*) AS num FROM " + prefData + "tula_ex_sz_file WHERE nzp_ex_sz = " + nzpExSZ;

                object obj = ExecScalar(connDB, sql, out ret, true);
                if (!ret.result) throw new Exception("(#3.2)GetExchangeSZ: " + ret.text);
                int num = Convert.ToInt32(obj);
                int lineRow = 0, lineServ = 0; // используется в логах

                try
                {
                    for (int i = 0; i < dt.Rows.Count; i++)
                    {
                        lineRow = i;
                        if (Constants.Trace) ClassLog.WriteLog("итерация цикла:" + i + "");
                        int sl = (dt.Rows[i]["sl"] != DBNull.Value) ? Convert.ToInt16(dt.Rows[i]["sl"]) : -1;
                        string famil = (dt.Rows[i]["famil"] != DBNull.Value ? Convert.ToString(dt.Rows[i]["famil"]).Trim() : null);
                        string imja = (dt.Rows[i]["imja"] != DBNull.Value ? Convert.ToString(dt.Rows[i]["imja"]).Trim() : null);
                        string otch = (dt.Rows[i]["otch"] != DBNull.Value ? Convert.ToString(dt.Rows[i]["otch"]).Trim() : null);
                        DateTime drog = (dt.Rows[i]["drog"] != DBNull.Value ? ((DateTime) dt.Rows[i]["drog"]) : new DateTime());
                        string strahnm = (dt.Rows[i]["strahnm"] != DBNull.Value ? Convert.ToString(dt.Rows[i]["strahnm"]).Trim() : null);
                        string nasp = (dt.Rows[i]["nasp"] != DBNull.Value ? Convert.ToString(dt.Rows[i]["nasp"]).Trim() : null);
                        string nylic = (dt.Rows[i]["nylic"] != DBNull.Value ? Convert.ToString(dt.Rows[i]["nylic"]).Trim() : null);
                        string ndom = (dt.Rows[i]["ndom"] != DBNull.Value ? Convert.ToString(dt.Rows[i]["ndom"]).Trim() : null);
                        string nkorp = (dt.Rows[i]["nkorp"] != DBNull.Value ? Convert.ToString(dt.Rows[i]["nkorp"]).Trim() : null);
                        string nkw = (dt.Rows[i]["nkw"] != DBNull.Value ? Convert.ToString(dt.Rows[i]["nkw"]).Trim() : null);
                        string nkomn = (dt.Rows[i]["nkomn"] != DBNull.Value ? Convert.ToString(dt.Rows[i]["nkomn"]).Trim() : null);
                        int kolk = (dt.Rows[i]["kolk"] != DBNull.Value ? Convert.ToInt32(dt.Rows[i]["kolk"]) : -1);//-1 - признак пустого поля
                        string lchet = (dt.Rows[i]["lchet"] != DBNull.Value ? Convert.ToString(dt.Rows[i]["lchet"]).Trim() : null);
                        string vidgf = (dt.Rows[i]["vidgf"] != DBNull.Value ? Convert.ToString(dt.Rows[i]["vidgf"]).Trim() : null);
                        string privat = (dt.Rows[i]["privat"] != DBNull.Value ? Convert.ToString(dt.Rows[i]["privat"]).Trim() : null);
                        decimal opl = (dt.Rows[i]["opl"] != DBNull.Value ? Convert.ToDecimal(dt.Rows[i]["opl"]) : -1);
                        decimal otpl = (dt.Rows[i]["otpl"] != DBNull.Value ? Convert.ToDecimal(dt.Rows[i]["otpl"]) : -1);
                        decimal oplj = (dt.Rows[i]["oplj"] != DBNull.Value ? Convert.ToDecimal(dt.Rows[i]["oplj"]) : -1);
                        decimal otplj = (dt.Rows[i]["otplj"] != DBNull.Value ? Convert.ToDecimal(dt.Rows[i]["otplj"]) : -1);
                        decimal kolzr = (dt.Rows[i]["kolzr"] != DBNull.Value ? Convert.ToInt32(dt.Rows[i]["kolzr"]) : -1);
                        decimal kolzrp = (dt.Rows[i]["kolzrp"] != DBNull.Value ? Convert.ToDecimal(dt.Rows[i]["kolzrp"]) : -1);
                        decimal kolpr = (dt.Rows[i]["kolpr"] != DBNull.Value ? Convert.ToInt32(dt.Rows[i]["kolpr"]) : -1);
                        decimal prz = (dt.Rows[i]["prz"] != DBNull.Value ? Convert.ToInt32(dt.Rows[i]["prz"]) : -1);
                        decimal prn = (dt.Rows[i]["prn"] != DBNull.Value ? Convert.ToInt32(dt.Rows[i]["prn"]) : -1);
                        decimal prk = (dt.Rows[i]["prk"] != DBNull.Value ? Convert.ToInt32(dt.Rows[i]["prk"]) : -1);

                        DataRow r = eDBF.DataTable.NewRow();
                        r["sl"] = sl;
                        r["FAMIL"] = famil;
                        r["IMJA"] = imja;
                        r["OTCH"] = otch;
                        r["DROG"] = drog;
                        r["STRAHNM"] = strahnm;
                        r["NASP"] = nasp;
                        r["NYLIC"] = nylic;
                        r["NDOM"] = ndom;
                        r["NKORP"] = nkorp;
                        r["NKW"] = nkw;
                        r["NKOMN"] = nkomn;
                        r["KOLK"] = kolk != -1 ? (object) kolk : DBNull.Value;
                        r["LCHET"] = lchet;
                        r["VIDGF"] = vidgf;
                        r["PRIVAT"] = privat;
                        r["OPL"] = opl != -1 ? (object) opl : DBNull.Value;
                        r["OTPL"] = otpl != -1 ? (object) otpl : DBNull.Value;
                        r["OPLJ"] = oplj != -1 ? (object) oplj : DBNull.Value;
                        r["OTPLJ"] = otplj != -1 ? (object) otplj : DBNull.Value;
                        r["KOLZR"] = kolzr != -1 ? (object) kolzr : DBNull.Value;
                        r["KOLZRP"] = kolzrp != -1 ? (object) kolzrp : DBNull.Value;
                        r["KOLPR"] = kolpr != -1 ? (object) kolpr : DBNull.Value;
                        r["PRZ"] = prz != -1 ? (object) prz : DBNull.Value;
                        r["PRN"] = prn != -1 ? (object) prn : DBNull.Value;
                        r["PRK"] = prk != -1 ? (object) prk : DBNull.Value;

                        for (int j = 1; j <= countServ; j++)
                        {
                            lineServ = j;
                            string gkuJ = (dt.Rows[i]["gku" + j] != DBNull.Value ? Convert.ToString(dt.Rows[i]["gku" + j]).Trim() : null);
                            decimal tarifJ = (dt.Rows[i]["tarif" + j] != DBNull.Value ? Convert.ToDecimal(dt.Rows[i]["tarif" + j]) : -1);
                            decimal sumJ = (dt.Rows[i]["sum" + j] != DBNull.Value ? Convert.ToDecimal(dt.Rows[i]["sum" + j]) : -1);
                            decimal faktJ = (dt.Rows[i]["fakt" + j] != DBNull.Value ? Convert.ToDecimal(dt.Rows[i]["fakt" + j]) : -1);
                            decimal normJ = (dt.Rows[i]["norm" + j] != DBNull.Value ? Convert.ToDecimal(dt.Rows[i]["norm" + j]) : -1);
                            string orgJ = (dt.Rows[i]["org" + j] != DBNull.Value ? Convert.ToString(dt.Rows[i]["org" + j]).Trim() : null);
                            int vidtarJ = (dt.Rows[i]["vidtar" + j] != DBNull.Value ? Convert.ToInt32(dt.Rows[i]["vidtar" + j]) : -1);
                            decimal koefJ = (dt.Rows[i]["koef" + j] != DBNull.Value ? Convert.ToDecimal(dt.Rows[i]["koef" + j]) : -1);
                            string lchetJ = (dt.Rows[i]["lchet" + j] != DBNull.Value ? Convert.ToString(dt.Rows[i]["lchet" + j]).Trim() : null);
                            decimal sumzJ = (dt.Rows[i]["sumz" + j] != DBNull.Value ? Convert.ToDecimal(dt.Rows[i]["sumz" + j]) : -1);
                            int klmzJ = (dt.Rows[i]["klmz" + j] != DBNull.Value ? Convert.ToInt32(dt.Rows[i]["klmz" + j]) : -1);
                            int ozsJ = (dt.Rows[i]["ozs" + j] != DBNull.Value ? Convert.ToInt32(dt.Rows[i]["ozs" + j]) : -1);
                            decimal sumozsJ = (dt.Rows[i]["sumozs" + j] != DBNull.Value ? Convert.ToDecimal(dt.Rows[i]["sumozs" + j]) : -1);

                            r["GKU" + j] = gkuJ;
                            r["TARIF" + j] = tarifJ != -1 ? (object) tarifJ : DBNull.Value;
                            r["SUM" + j] = sumJ != -1 ? (object) sumJ : DBNull.Value;
                            r["FAKT" + j] = faktJ != -1 ? (object) faktJ : DBNull.Value;
                            r["NORM" + j] = normJ != -1 ? (object) normJ : DBNull.Value;
                            r["SUMZ" + j] = sumzJ != -1 ? (object) sumzJ : DBNull.Value;
                            r["KLMZ" + j] = klmzJ != -1 ? (object) klmzJ : DBNull.Value;
                            r["OZS" + j] = ozsJ != -1 ? (object) ozsJ : DBNull.Value;
                            r["SUMOZS" + j] = sumozsJ != -1 ? (object) sumozsJ : DBNull.Value;
                            r["ORG" + j] = orgJ;
                            r["VIDTAR" + j] = vidtarJ != -1 ? (object) vidtarJ : DBNull.Value;
                            r["KOEF" + j] = koefJ != -1 ? (object) koefJ : DBNull.Value;
                            r["LCHET" + j] = lchetJ;
                        }

                        eDBF.DataTable.Rows.Add(r);
                        if (i % 100 == 0)
                        {
                            excelRepDb.SetMyFileProgress(new ExcelUtility { nzp_exc = nzpExc, progress = ((decimal) i) / num });
                            eDBF.Append(strFilePath);
                            eDBF.DataTable.Rows.Clear();
                            if (Constants.Trace) ClassLog.WriteLog("запись файла");
                        }
                    }
                }
                catch (Exception e)
                {
                    string log = "Ошибка в цикле записи dbf-файла(Строка - " + lineRow + ",№ услуги - " +
                                    lineServ + "):\n" + e.Message;
                    throw new Exception(log);
                }
                if (eDBF.DataTable.Rows.Count > 0)
                {
                    if (Constants.Trace) ClassLog.WriteLog("перед концом записи файла");
                    eDBF.Append(strFilePath);
                    if (Constants.Trace) ClassLog.WriteLog("конец записи файла");
                }
                if (InputOutput.useFtp) fileName = InputOutput.SaveOutputFile(Path.Combine(dir, fileName + ".dbf"));
                else fileName = Path.Combine(dir, fileName) + ".dbf";
                #endregion

                #endregion
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;
                MonitorLog.WriteLog("(Exception)Ошибка в функции GetExchangeSZ:\n" + ex.Message + "\n" + ex.StackTrace, MonitorLog.typelog.Error, true);
            }
            finally
            {
                info.DownDate = DateTime.Now;
                if (ret.result)
                {
                    excelRepDb.SetMyFileProgress(new ExcelUtility { nzp_exc = nzpExc, progress = 1 });
                    excelRepDb.SetMyFileState(new ExcelUtility { nzp_exc = nzpExc, status = ExcelUtility.Statuses.Success, exc_path = fileName });
                    info.Status = true;
                }
                else
                {
                    excelRepDb.SetMyFileState(new ExcelUtility { nzp_exc = nzpExc, status = ExcelUtility.Statuses.Failed });
                    info.Status = false;
                }

                GetProtocolSZ(connDB, info);

                excelRepDb.Close();
                perm.PermitOnly();
                if (connDB.State == ConnectionState.Open) connDB.Dispose();
            }
            if (Constants.Trace) ClassLog.WriteLog("СЗ.Конец формирование выгрузки.");
            return ret;
        }

        /// <summary>Получить наименование загруженного файла от СЗ</summary>
        /// <param name="connDB">Подключение к БД</param>
        /// <param name="nzpExSZ">Идентификатор загруженного файла СЗ</param>
        /// <param name="ret">Индикатор ошибок</param>
        /// <returns></returns>
        private string GetNameDownloadedFile(IDbConnection connDB, int nzpExSZ, out Returns ret) {
            string prefData = Points.Pref + DBManager.sDataAliasRest;
            string sql = " SELECT file_name FROM  " + prefData + "tula_ex_sz WHERE nzp_ex_sz = " + nzpExSZ;
            string fileName = ExecScalar(connDB, sql, out ret, true).ToString().Trim();
            if (!ret.result) throw new Exception("GetNameLoadFile: " + ret.text);
            return fileName;
        }

        /// <summary>Удаление расширения</summary>
        /// <param name="fileName">Наименование файла</param>
        /// <returns>Наименование файла без расширения</returns>
        private string DeleteExtension(string fileName) {
            return fileName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase)
                ? Path.GetFileNameWithoutExtension(fileName)
                : fileName;
        }

        /// <summary>Создание временный таблиц</summary>
        /// <param name="connDB">Подключение к БД</param>
        private void CreateTempTable(IDbConnection connDB) {
            ExecSQL(connDB, "DROP TABLE t_sz_prm");
            string sql = " CREATE TEMP TABLE t_sz_prm( " +
                         " id INTEGER, " +
                         " nzp_kvar INTEGER, " +
                         " sl INTEGER, " +
                         " kolk INTEGER, " +
                         " lchet " + DBManager.sDecimalType + "(24,0), " +
                         " vidgf CHARACTER(25), " +
                         " privat CHARACTER(1), " +
                         " oplj " + DBManager.sDecimalType + "(14,2), " +
                         " otplj " + DBManager.sDecimalType + "(8,2), " +
                         " kolzrp SMALLINT, " +
                         " kolpr INTEGER, " +
                         " prz INTEGER, " +
                         " prk SMALLINT, " +
                         " is_agreement SMALLINT) " + DBManager.sUnlogTempTable;
            Returns ret = ExecSQL(connDB, sql);
            if (!ret.result) throw new Exception("(#СЗ)CreateTempTable: " + ret.text);

            ExecSQL(connDB, " CREATE INDEX ix_t_sz_prm_1 ON t_sz_prm(nzp_kvar)", false);


            if (TempTableInWebCashe(connDB, "t_sz_services")) ExecSQL(connDB, " DROP TABLE t_sz_services ");
            sql = " CREATE TEMP TABLE t_sz_services(" +
                    " nzp_serv INTEGER, " +
                    " nzp_serv_base INTEGER, " +
                    " ord INTEGER, " +
                    " nzp_prm INTEGER, " +
                    " nzp_res INTEGER, " +
                    " service CHARACTER(100)) " + DBManager.sUnlogTempTable;
            ret = ExecSQL(connDB, sql);
            if (!ret.result) throw new Exception("(#СЗ)CreateTempTable: " + ret.text);

            ExecSQL(connDB, "CREATE INDEX ix_t_sz_services_1 ON t_sz_services(nzp_serv)", false);

            ExecSQL(connDB, " DROP TABLE t_sz_charge ");
            sql = " CREATE TEMP TABLE t_sz_charge(" +
                    " nzp_kvar INTEGER, " +
                    " nzp_serv INTEGER, " +
                    " nzp_supp INTEGER, " +
                    " nzp_frm INTEGER, " +
                    " tarif_ch " + DBManager.sDecimalType + "(15,5), " +    //тариф из charge
                    " rsum_tarif " + DBManager.sDecimalType + "(14,2), " +  //начислено за месяц
                    " sum_tarif " + DBManager.sDecimalType + "(14,4), " +   //начислено за месяц с учетом не допоставок
                    " sum_nedop " + DBManager.sDecimalType + "(14,2), " +   //сумма недопоставок
                    " sum_money " + DBManager.sDecimalType + "(14,2), " +   //оплачено за месяц
                    " sum_insaldo " + DBManager.sDecimalType + "(14,2), " + //входящее сальдо
                    " change_tarif INTEGER, " +                             //изменялся лим тариф
                    " reval " + DBManager.sDecimalType + "(14,2), " +       //перерасчет
                    " gku CHARACTER(250), " +                               //Наименование ЖКУ
                    " tarif " + DBManager.sDecimalType + "(15,5), " +       //Сумма тарифа
                    " sum " + DBManager.sDecimalType + "(14,4), " +         //Сумма начислений
                    " fakt " + DBManager.sDecimalType + "(14,4), " +        //Фактическое потребление ЖКУ
                    " norm " + DBManager.sDecimalType + "(10,4), " +        //Норматив
                    " is_device INTEGER, " +                                //0 - норматив, 1 - ПУ
                    " org CHARACTER(30), " +                                //Поставщик
                    " vidtar INTEGER, " +                                   //Вид тарифа
                    " koef " + DBManager.sDecimalType + "(12,2)," +         //Поправочный коэфициент
                    " lchet CHARACTER(24), " +                              //Лицевой счет услуги
                    " sumz " + DBManager.sDecimalType + "(14,4), " +        //Сумма задолжнности
                    " klmz INTEGER, " +                                     //Количество месяцев задолжнности
                    " ozs INTEGER, " +                                      //Наличие соглашение по оплате
                    " sumozs " + DBManager.sDecimalType + "(14,4)) " +      //Сумма оплаты задолжнности
                  DBManager.sUnlogTempTable;
            ret = ExecSQL(connDB, sql);
            if (!ret.result) throw new Exception("(#СЗ)CreateTempTable: " + ret.text);

            ExecSQL(connDB, "CREATE INDEX ix_t_sz_charge_1 ON t_sz_charge(nzp_serv)", false);
            ExecSQL(connDB, "CREATE INDEX ix_t_sz_charge_2 ON t_sz_charge(nzp_kvar, nzp_serv)", false);

            ExecSQL(connDB, " DROP TABLE t_sz_serv ");
            sql = " CREATE TEMP TABLE t_sz_serv(" +
                    " nzp_kvar INTEGER, " +
                    " nzp_serv INTEGER, " +
                    " is_serv BOOLEAN, " +
                    " gku CHARACTER(250), " +                           //Наименование ЖКУ
                    " tarif " + DBManager.sDecimalType + "(15,5), " +   //Сумма тарифа
                    " sum " + DBManager.sDecimalType + "(14,4), " +     //Сумма начислений
                    " fakt " + DBManager.sDecimalType + "(14,4), " +    //Фактическое потребление ЖКУ
                    " norm " + DBManager.sDecimalType + "(10,4), " +    //Норматив
                    " org CHARACTER(30), " +                            //Поставщик
                    " vidtar INTEGER, " +                               //Вид тарифа
                    " koef " + DBManager.sDecimalType + "(12,2)," +     //Поправочный коэфициент
                    " lchet CHARACTER(24), " +                          //Лицевой счет услуги
                    " sumz " + DBManager.sDecimalType + "(14,4), " +    //Сумма задолжнности
                    " klmz INTEGER, " +                                 //Количество месяцев задолжнности
                    " ozs INTEGER, " +                                  //Наличие соглашение по оплате
                    " sumozs " + DBManager.sDecimalType + "(14,4)) " +  //Сумма оплаты задолжнности
                  DBManager.sUnlogTempTable;
            ret = ExecSQL(connDB, sql);
            if (!ret.result) throw new Exception("(#СЗ)CreateTempTable: " + ret.text);

            ExecSQL(connDB, " DROP TABLE t_sz_charge_filter ");
            sql = " CREATE TEMP TABLE t_sz_charge_filter( " +
                    " nzp_charge INTEGER, " +
                    " nzp_kvar INTEGER, " +
                    " nzp_serv INTEGER, " +
                    " isdel INTEGER) " + DBManager.sUnlogTempTable; // isdel - индикатор удаленной услуги в начислениях
            ret = ExecSQL(connDB, sql);
            if (!ret.result) throw new Exception("(#СЗ)CreateTempTable: " + ret.text);

            ExecSQL(connDB, " CREATE INDEX ix_t_sz_charge_filter_1 ON t_sz_charge_filter(nzp_charge, nzp_kvar, nzp_serv)");
            ExecSQL(connDB, " CREATE INDEX ix_t_sz_charge_filter_2 ON t_sz_charge_filter(nzp_kvar, nzp_serv, nzp_charge)");

            // вспомогательная таблица для подсчета тарифа по отоплению
            ExecSQL(connDB, " DROP TABLE t_sz_tarif8 ");
            sql = " CREATE TEMP TABLE t_sz_tarif8( " +
                    " nzp_kvar INTEGER, " +
                    " tarif " + DBManager.sDecimalType + "(15,5)) " + DBManager.sUnlogTempTable;
            ret = ExecSQL(connDB, sql);
            if (!ret.result) throw new Exception("(#СЗ)CreateTempTable: " + ret.text);

            ExecSQL(connDB, " CREATE INDEX ix_t_sz_tarif8_1 ON t_sz_tarif8(nzp_kvar)");

            // вспомогательная таблица для заполнения суммы задолженности и полей sumz, sumozs, klmz
            ExecSQL(connDB, " DROP TABLE t_sz_dolg ");
            sql = " CREATE TEMP TABLE t_sz_dolg( " +
                    " nzp_kvar INTEGER," +
                    " nzp_serv INTEGER," +
                    " sum_insaldo " + DBManager.sDecimalType + "(10,4), " +
                    " sum_money " + DBManager.sDecimalType + "(10,4), " +
                    " sum_z " + DBManager.sDecimalType + "(10,4), " +
                    " sum_tarif " + DBManager.sDecimalType + "(10,4)) " + DBManager.sUnlogTempTable;
            ret = ExecSQL(connDB, sql);
            if (!ret.result) throw new Exception("(#СЗ)CreateTempTable: " + ret.text);

            ExecSQL(connDB, " CREATE INDEX ix_t_sz_dolg_1 ON t_sz_dolg(nzp_kvar, nzp_serv)");

            //вспомогательная таблицы для заполнения наименование ЖКУ
            ExecSQL(connDB, " DROP TABLE t_sz_gku ");
            sql = " CREATE TEMP TABLE t_sz_gku(" +
                    " nzp_kvar INTEGER, " +
                    " nzp_serv INTEGER, " +
                    " nzp_prm INTEGER, " +
                    " nzp_res INTEGER, " +
                    " nzp_y INTEGER) " + DBManager.sUnlogTempTable;
            ret = ExecSQL(connDB, sql);
            if (!ret.result) throw new Exception("(#СЗ)CreateTempTable: " + ret.text);

            ExecSQL(connDB, " CREATE INDEX ix_t_sz_gku_1 ON t_sz_gku(nzp_kvar, nzp_prm)");

            //таблица для объединение услуг
            ExecSQL(connDB, " DROP TABLE t_sz_services_union ");
            sql = " CREATE TEMP TABLE t_sz_services_union(" +
                    " nzp_kvar INTEGER, " +
                    " nzp_serv_base INTEGER, " +
                    " nzp_serv_first INTEGER, " +
                    " sum_tarif " + DBManager.sDecimalType + "(14,4), " +   //начислено за месяц с учетом не допоставок
                    " tarif_ch " + DBManager.sDecimalType + "(15,5), " +    //тариф из charge
                    " change_tarif INTEGER, " +                             //изменялся лим тариф
                    " gku CHARACTER(250), " +                               //Наименование ЖКУ
                    " tarif " + DBManager.sDecimalType + "(15,5), " +       //Сумма тарифа
                    " sum " + DBManager.sDecimalType + "(14,4), " +         //Сумма начислений
                    " fakt " + DBManager.sDecimalType + "(14,4), " +        //Фактическое потребление ЖКУ
                    " norm " + DBManager.sDecimalType + "(10,4), " +        //Норматив
                    " org CHARACTER(30), " +                                //Поставщик
                    " vidtar INTEGER, " +                                   //Вид тарифа
                    " koef " + DBManager.sDecimalType + "(12,2)," +         //Поправочный коэфициент
                    " lchet CHARACTER(24), " +                              //Лицевой счет услуги
                    " sumz " + DBManager.sDecimalType + "(14,4), " +        //Сумма задолжнности
                    " klmz INTEGER, " +                                     //Количество месяцев задолжнности
                    " ozs INTEGER, " +                                      //Наличие соглашение по оплате
                    " sumozs " + DBManager.sDecimalType + "(14,4)) " +      //Сумма оплаты задолжнности
                      DBManager.sUnlogTempTable;
            ret = ExecSQL(connDB, sql);
            if (!ret.result) throw new Exception("(#СЗ)CreateTempTable: " + ret.text);

            ExecSQL(connDB, " CREATE INDEX ix_t_sz_services_union_1 ON t_sz_services_union(nzp_kvar)");
            ExecSQL(connDB, " CREATE INDEX ix_t_sz_services_union_2 ON t_sz_services_union(nzp_kvar, nzp_serv_base)");
            ExecSQL(connDB, " CREATE INDEX ix_t_sz_services_union_3 ON t_sz_services_union(nzp_kvar, nzp_serv_first)");
        }

        /// <summary>Заполнение справочника услуг</summary>
        /// <param name="connDB">Подключение к БД</param>
        private void FillingServicesDirectory(IDbConnection connDB) {
            /*
            *| Порядковый номер | Идентификатор услуги | Услуга в биллинге                                 | Услуга в соц.защите                         |
            *|        1         |          2           | Содерж. и ремонт жилого помещения, управление МКД | Содержание жилья                            |
            *|        1         |          18          | Уборка при домовой территории                     | Содержание жилья                            |
            *|        1         |          209         | Текущий ремонт ВДГО                               | Содержание жилья                            |
            *|        1         |          259         | Содержание и ремонт жилья                         | Содержание жилья                            |
            *|        1         |          525         | Содержание мусоропровода                          | Содержание жилья                            |
            *|        2         |          206         | Взнос на капремонт                                | Капитальный ремонт                          |
            *|        3         |          15          | Социальный найм                                   | Социальный найм  (Электроотопление)         |
            *|        3         |          524         | НАЕМ                                              | Социальный найм  (Электроотопление)         |
            *|        3         |          1010056     | Коммерческий найм                                 | Социальный найм  (Электроотопление)         |
              *Прим.: Электроотопление пока не изв.
            *|        4         |          6           | Холодная вода                                     | Холодная вода                               |
            *|        4         |          1010047     | п/к Холодная вода                                 | Холодная вода                               |
            *|        4         |          374         | Транспортировка воды                              | Холодная вода                               |
            *|        5         |          9           | Горячая вода                                      | Горячая вода                                |
            *|        5         |          513         | ОДН-Горячая вода                                  | Горячая вода                                |
            *|        5         |          1010040     | п/к Горячая вода                                  | Горячая вода                                |
            *|        5         |          1010052     | п/к ОДН-Горячая вода                              | Горячая вода                                |
            *|        5         |          14          | Холодная вода для нужд ГВС                        | Горячая вода                                |
            *|        5         |          514         | ОДН-Холодная вода для нужд ГВС                    | Горячая вода                                |
            *|        6         |          7           | Канализация                                       | Канализация                                 |
            *|        6         |          324         | Очистка стоков                                    | Канализация                                 |
            *|        6         |          353         | Транспортирвка стоков                             | Канализация                                 |
            *|        6         |          1010041     | п/к Канализация                                   | Канализация                                 |
            *|        7         |          16          | Вывоз ТБО                                         | Вывоз мусора                                |
            *|        7         |          24          | Вывоз ЖБО                                         | Вывоз мусора                                |
            *|        7         |          285         | ВЫВОЗ КГМ                                         | Вывоз мусора                                |
            *|        7         |          266         | Захоронение ТБО                                   | Вывоз мусора                                |
            *|        8         |          10          | Газоснабжение                                     | Газ                                         |
            *|        9         |          25          | Электроснабжение                                  | Электроэнергия                              |
            *|        9         |          1010049     | п/к Электроэнергия                                | Электроэнергия                              |
            *|        10        |          8           | Отопление                                         | Отопление                                   |
            *|        10        |          512         | ОДН-Отопление                                     | Отопление                                   |
            *|        10        |          1010043     | п/к Отопление                                     | Отопление                                   |
            *|        11        |          17          | уборка лестничных клеток                          | Уборка МОП                                  |
            *|        11        |          602         | УБОРКА МОП                                        | Уборка МОП                                  |
            *|        12        |          5           | Содерж. лифт. хоз-ва                              | Содержание лифта                            |
            *|        13        |          510         | ОДН-Холодная вода                                 | Холодная вода ОДН                           |
            *|        13        |          1010050     | п/к ОДН-Холодная вода                             | Холодная вода ОДН                           |
            *|        14        |          0           |                                                   | Тепловая энергия на подогрев холодной воды  |
              *Прим.: Сюда переносится Горячая вода, если есть услуга 14
            *|        15        |          515         | ОДН-Электроснабжение                              | Электроэнергия ОДН                          |
            *|        15        |          1010042     | п/к ОДН-Электроэнергия                            | Электроэнергия ОДН                          |
            */
            InsertServicesDirectory(connDB, 2,       2,   1,  1401, 3101, "Содержание жилья");
            InsertServicesDirectory(connDB, 18,      2,   1,  1401, 3101, "Содержание жилья");
            InsertServicesDirectory(connDB, 209,     2,   1,  1401, 3101, "Содержание жилья");
            InsertServicesDirectory(connDB, 259,     2,   1,  1401, 3101, "Содержание жилья");
            InsertServicesDirectory(connDB, 525,     2,   1,  1401, 3101, "Содержание жилья");
            InsertServicesDirectory(connDB, 206,     206, 2,  1402, 3102, "Капитальный ремонт");
            //Записывается услуга сюда в п/н № 3 (по задаче GKHKPFIVE-9631)
            InsertServicesDirectory(connDB, 15,      15,  3,  0,    0,    "Социальный найм");
            InsertServicesDirectory(connDB, 524,     15,  3,  0,    0,    "Социальный найм");
            InsertServicesDirectory(connDB, 1010056, 15,  3,  0,    0,    "Социальный найм");
            InsertServicesDirectory(connDB, 6,       6,   4,  1407, 3107, "Холодная вода");
            InsertServicesDirectory(connDB, 1010047, 6,   4,  1407, 3107, "Холодная вода");
            InsertServicesDirectory(connDB, 374,     6,   4,  1407, 3107, "Холодная вода");
            InsertServicesDirectory(connDB, 9,       9,   5,  1408, 3108, "Горячая вода");
            InsertServicesDirectory(connDB, 513,     9,   5,  1408, 3108, "Горячая вода");
            InsertServicesDirectory(connDB, 1010040, 9,   5,  1408, 3108, "Горячая вода");
            InsertServicesDirectory(connDB, 1010052, 9,   5,  1408, 3108, "Горячая вода");
            InsertServicesDirectory(connDB, 14,      9,   5,  1408, 3108, "Горячая вода");
            InsertServicesDirectory(connDB, 514,     9,   5,  1408, 3108, "Горячая вода");
            InsertServicesDirectory(connDB, 7,       7,   6,  1409, 3109, "Канализация");
            InsertServicesDirectory(connDB, 324,     7,   6,  1409, 3109, "Канализация");
            InsertServicesDirectory(connDB, 353,     7,   6,  1409, 3109, "Канализация");
            InsertServicesDirectory(connDB, 1010041, 7,   6,  1409, 3109, "Канализация");
            InsertServicesDirectory(connDB, 16,      16,  7,  1411, 3111, "Вывоз мусора");
            InsertServicesDirectory(connDB, 24,      16,  7,  1411, 3111, "Вывоз мусора");
            InsertServicesDirectory(connDB, 285,     16,  7,  1411, 3111, "Вывоз мусора");
            InsertServicesDirectory(connDB, 266,     16,  7,  1411, 3111, "Вывоз мусора");
            InsertServicesDirectory(connDB, 10,      10,  8,  1405, 3105, "Газ");
            InsertServicesDirectory(connDB, 25,      25,  9,  1404, 3104, "Электроэнергия");
            InsertServicesDirectory(connDB, 1010049, 25,  9,  1404, 3104, "Электроэнергия");
            InsertServicesDirectory(connDB, 8,       8,   10, 1410, 3110, "Отопление");
            InsertServicesDirectory(connDB, 512,     8,   10, 1410, 3110, "Отопление");
            InsertServicesDirectory(connDB, 1010043, 8,   10, 1410, 3110, "Отопление");
            InsertServicesDirectory(connDB, 17,      17,  11, 1412, 3112, "Уборка МОП");
            InsertServicesDirectory(connDB, 602,     17,  11, 1412, 3112, "Уборка МОП");
            InsertServicesDirectory(connDB, 5,       12,  12, 0,    0,    "Содержание лифта");
            InsertServicesDirectory(connDB, 510,     510, 13, 1403, 3103, "Холодная вода ОДН");
            InsertServicesDirectory(connDB, 1010050, 510, 13, 1403, 3103, "Холодная вода ОДН");
            //InsertServicesDirectory(connDB, -9,  -9,  14, 1406, 3106, "Тепловая энергия на подогрев холодной воды");
            InsertServicesDirectory(connDB, 515,     515, 15, 0,    0,    "Электроэнергия ОДН");
            InsertServicesDirectory(connDB, 1010042, 515, 15, 0,    0,    "Электроэнергия ОДН");
        }

        /// <summary>Добавление новой услуги в справочник услуг</summary>
        /// <param name="connDB">Подключение к БД</param>
        /// <param name="nzpServ">Идентификатор услуги</param>
        /// <param name="nzpServBase">Идентификатор базовой услуги</param>
        /// <param name="order">Порядковый номер</param>
        /// <param name="nzpPrm">Идентификатор параметра</param>
        /// <param name="nzpRes">Идентификатор двумерной таблицы для расчетов</param>
        /// <param name="serviceBase">Наименование услуги(льготы)</param>
        private void InsertServicesDirectory(IDbConnection connDB, int nzpServ, int nzpServBase, int order, int nzpPrm, int nzpRes, string serviceBase) {
            string sql =
                string.Format(" INSERT INTO t_sz_services (nzp_serv, nzp_serv_base, ord, nzp_prm, nzp_res, service) " +
                              " VALUES ({0},{1},{2},{3},{4},'{5}')",
                                nzpServ, nzpServBase, order, nzpPrm, nzpRes, serviceBase);

            Returns ret = ExecSQL(connDB, sql);
            if (!ret.result) throw new Exception("InsertServicesDirectory: " + ret.text);

            ExecSQL(connDB, sUpdStat + " t_sz_services", false);
        }

        ///// <summary>Перенос услуги "Горячая вода"</summary>
        ///// <remarks>
        ///// Переносим значения по горячей воде в 14 льготу, 
        ///// если имеется услуга "Холодная вода для нужд ГВС".(GKHKPFIVE-9227)
        ///// </remarks>
        ///// <param name="connDB">Подключение к БД</param>
        //private void TransferServiceHotWater(IDbConnection connDB) {
        //    string sql = " UPDATE t_sz_charge SET nzp_serv = -9 " +
        //                 " WHERE nzp_serv = 9 " +
        //                   " AND 0 < ( SELECT COUNT(*) " +
        //                             " FROM t_sz_charge t " +
        //                             " WHERE nzp_serv = 14 " +
        //                               " AND t.nzp_kvar = t_sz_charge.nzp_kvar ) ";
        //    Returns ret = ExecSQL(connDB, sql);
        //    if (!ret.result) throw new Exception("TransferServiceHotWater: " + ret.text);

        //    sql = " UPDATE t_sz_charge SET nzp_serv = 9 WHERE nzp_serv = 14 ";
        //    ret = ExecSQL(connDB, sql);
        //    if (!ret.result) throw new Exception("TransferServiceHotWater: " + ret.text);

        //    ExecSQL(connDB, sUpdStat + " t_sz_charge", false);
        //}

        /// <summary>Объединение услуг</summary>
        /// <param name="connDB">Подключение к БД</param>
        private void UnionServices(IDbConnection connDB)
        {
            string sql = " INSERT INTO t_sz_services_union(nzp_kvar, nzp_serv_base, lchet, gku, nzp_serv_first, sum, norm, " +
                                                            " sumz, klmz, vidtar, koef, ozs, sumozs) " +
                         " SELECT nzp_kvar, nzp_serv_base, lchet, TRIM(gku) AS gku, " +
                                " MIN(c.nzp_serv) AS nzp_serv_first, SUM(sum) AS sum, SUM(norm) AS norm, SUM(sumz) AS sumz, " +
                                " SUM(klmz) AS klmz, MAX(vidtar) AS vidtar, MAX(koef) AS koef, " +
                                " MAX(ozs) AS ozs, SUM(sumozs) AS sumozs " +
                         " FROM t_sz_charge c INNER JOIN t_sz_services s ON s.nzp_serv = c.nzp_serv " +
                         " GROUP BY 1, 2, 3, 4 ";
            Returns ret = ExecSQL(connDB, sql);
            if (!ret.result) throw new Exception("UnionServices: " + ret.text);

            ExecSQL(connDB, DBManager.sUpdStat + " t_sz_services_union");

            sql = " UPDATE t_sz_services_union t SET nzp_serv_first = c.nzp_serv " +
                  " FROM t_sz_charge c " +
                  " WHERE c.nzp_kvar = t.nzp_kvar " +
                    " AND c.nzp_serv = t.nzp_serv_base  ";
            ret = ExecSQL(connDB, sql);
            if (!ret.result) throw new Exception("UnionServices: " + ret.text);

            sql = " UPDATE t_sz_services_union t SET tarif = c.tarif, " +
                                                   " org = TRIM(c.org), " +
                                                   " change_tarif = c.change_tarif, " +
                                                   " tarif_ch = c.tarif_ch, " +
                                                   " sum_tarif = c.sum_tarif " +
                  " FROM t_sz_charge c " +
                  " WHERE c.nzp_kvar = t.nzp_kvar " +
                    " AND c.nzp_serv = t.nzp_serv_first ";
            ret = ExecSQL(connDB, sql);
            if (!ret.result) throw new Exception("UnionServices: " + ret.text);

            #region fakt - фактическое потребление ЖКУ

            //FAKTX = SUMX / TARIFX, если TARIFX > 0. Если TARIFX = 0, то TARIFX = SUMX, FAKTX = 1   GKHKPFIVE-9182
            sql = " UPDATE t_sz_services_union SET fakt = sum/tarif " +
                  " WHERE tarif > 0" +
                  " AND (t_sz_services_union.fakt IS NULL OR t_sz_services_union.fakt = 0) " +
                  " AND t_sz_services_union.change_tarif = 0 "; // тариф не менялся
            ret = ExecSQL(connDB, sql);
            if (!ret.result) throw new Exception("(#2.5)GetExchangeSZ: " + ret.text);

            sql = " UPDATE t_sz_services_union SET (fakt, tarif) = (1, sum) " +
                  " WHERE tarif = 0 " +
                  " AND (t_sz_services_union.fakt IS NULL OR t_sz_services_union.fakt = 0) " +
                  " AND t_sz_services_union.change_tarif = 0 "; // тариф не менялся
            ret = ExecSQL(connDB, sql);
            if (!ret.result) throw new Exception("(#2.5)GetExchangeSZ: " + ret.text);

            sql = " UPDATE t_sz_services_union SET fakt = (CASE WHEN tarif_ch > 0 THEN sum_tarif / tarif_ch ELSE 0 END) " +
                  " WHERE t_sz_services_union.change_tarif = 1 "; // тариф изменился
            ret = ExecSQL(connDB, sql);
            if (!ret.result) throw new Exception("(#2.5)GetExchangeSZ: " + ret.text);

            // расход по услуге "Уборка МОП" 
            sql = " UPDATE t_sz_services_union tc SET fakt = oplj " +
                  " FROM t_sz_prm tp " +
                  " WHERE tp.nzp_kvar = tc.nzp_kvar " +
                    " AND tc.nzp_serv_first = 17 ";
            ret = ExecSQL(connDB, sql);
            if (!ret.result) throw new Exception("(#2.5)GetExchangeSZ: " + ret.text);

            #endregion

            ret = ExecSQL(connDB, "DELETE FROM t_sz_charge ");
            if (!ret.result) throw new Exception("UnionServices: " + ret.text);

            sql = " INSERT INTO t_sz_charge(nzp_kvar, nzp_serv, gku, tarif, sum, fakt, norm, org, " +
                                            " vidtar, koef, lchet, sumz, klmz, ozs, sumozs) " +
                  " SELECT nzp_kvar, t.nzp_serv_base, " +
                         " (CASE WHEN gku IS NULL OR TRIM(gku) = '' THEN TRIM(s.service) ELSE gku END) AS gku, " +
                         " tarif, sum, fakt, norm, org, " +
                         " vidtar, koef, lchet, sumz, klmz, ozs, sumozs " +
                  " FROM t_sz_services_union t INNER JOIN t_sz_services s ON s.nzp_serv = t.nzp_serv_base ";
            ret = ExecSQL(connDB, sql);
            if (!ret.result) throw new Exception("UnionServices: " + ret.text);

            ExecSQL(connDB, DBManager.sUpdStat + " t_sz_charge");
        }

        /// <summary>Сформировать протокол</summary>
        /// <param name="connDB">Подключение к БД</param>
        /// <param name="info">Информация о работе занесения в файл от соц.защиты</param>
        private void GetProtocolSZ(IDbConnection connDB, ExSzInfo info) {
            string points = info.Points.Aggregate(string.Empty, (current, value) => current + (value + ","));

            #region запись в ExcelUtility

            var excelRepDb = new ExcelRep();
            Returns ret = excelRepDb.AddMyFile(new ExcelUtility
            {
                nzp_user = info.IdUser,
                status = ExcelUtility.Statuses.InProcess,
                rep_name = "Протокол выгрузки для обмена с соц.защитой за " + info.Month + "." + info.Year + " " + points
            });
            if (!ret.result) return;
            int nzpExc = ret.tag;

            #endregion

            string fileName = string.Empty;
            string prefData = Points.Pref + DBManager.sDataAliasRest;
            try
            {
                //число загруженных лс
                string sql = "SELECT count(*) FROM " + prefData + "tula_ex_sz_file WHERE nzp_ex_sz=" + info.NzpExSZ;
                var downCountLS = ExecScalar(connDB, sql, out ret, true);
                if (downCountLS != null && downCountLS != DBNull.Value)
                {
                    info.DownCountLS = Convert.ToInt32(downCountLS);
                }

                //несопоставленные лс
                sql = " SELECT (TRIM(" + sNvlWord + "(famil,''))||' '||" +
                               "TRIM(" + sNvlWord + "(imja,''))||' '||" +
                               "TRIM(" + sNvlWord + "(otch,''))) AS fio, drog, lchet, " +
                              "(TRIM(" + sNvlWord + "(nasp,''))||', '||" +
                               "TRIM(" + sNvlWord + "(nylic,''))||', '||" +
                               "TRIM(" + sNvlWord + "(ndom,''))||', '||" +
                               "TRIM(" + sNvlWord + "(nkw,''))) AS adr " +
                      " FROM " + prefData + "tula_ex_sz_file WHERE nzp_ex_sz = " + info.NzpExSZ + " AND nzp_kvar IS NULL ORDER BY adr, fio";
                DataTable dt = ClassDBUtils.OpenSQL(sql, connDB).resultData;
                if (dt != null && dt.Rows.Count > 0)
                {
                    info.NosyncCountLS = dt.Rows.Count;
                }

                //сопоставленные лс
                sql = " SELECT (TRIM(" + sNvlWord + "(famil,''))||' '||" +
                               "TRIM(" + sNvlWord + "(imja,''))||' '||" +
                               "TRIM(" + sNvlWord + "(otch,''))) AS fio, drog, lchet, " +
                              "(TRIM(" + sNvlWord + "(nasp,''))||', '||" +
                               "TRIM(" + sNvlWord + "(nylic,''))||', '||" +
                               "TRIM(" + sNvlWord + "(ndom,''))||', '||" +
                               "TRIM(" + sNvlWord + "(nkw,''))) AS adr " +
                      " FROM " + prefData + "tula_ex_sz_file WHERE nzp_ex_sz = " + info.NzpExSZ + " AND nzp_kvar IS NOT NULL ORDER BY adr,fio";
                DataTable dt1 = ClassDBUtils.OpenSQL(sql, connDB).resultData;

                //загрузил файл
                sql = " SELECT u.name FROM " + prefData + "users u,  " +
                                               prefData + "tula_ex_sz s " +
                      " WHERE s.nzp_ex_sz = " + info.NzpExSZ + " and s.nzp_user = u.nzp_user ";
                var upName = ExecScalar(connDB, sql, out ret, true);
                if (upName != null && upName != DBNull.Value)
                {
                    info.UpUserName = Convert.ToString(upName).Trim();
                }

                //выгрузил файл
                sql = "SELECT u.name FROM " + prefData + "users u WHERE u.nzp_user = " + info.IdUser;
                var downName = ExecScalar(connDB, sql, out ret, true);
                if (downName != null && downName != DBNull.Value)
                {
                    info.DownUserName = Convert.ToString(downName).Trim();
                }

                //Дата загрузки файла
                sql = "SELECT dat_upload FROM " + prefData + "tula_ex_sz WHERE nzp_ex_sz=" + info.NzpExSZ;
                var upDate = ExecScalar(connDB, sql, out ret, true);
                if (upDate != null && upDate != DBNull.Value)
                {
                    info.UpDate = Convert.ToDateTime(upDate);
                }

                var rep = new Report();

                if (dt == null) dt = new DataTable();
                dt.TableName = "ls";
                dt1.TableName = "ls1";
                var fDataSet = new DataSet();
                fDataSet.Tables.Add(dt);
                fDataSet.Tables.Add(dt1);

                const string template = "protocol_SZ.frx";
                rep.Load(PathHelper.GetReportTemplatePath(template));

                rep.RegisterData(fDataSet);
                rep.GetDataSource("ls").Enabled = true;
                rep.GetDataSource("ls1").Enabled = true;

                //установка параметров отчета
                rep.SetParameterValue("up_file_name", info.UploadFileName);
                rep.SetParameterValue("down_file_name", info.DownloadedFileName);
                rep.SetParameterValue("up_user_name", info.UpUserName);
                rep.SetParameterValue("down_user_name", info.DownUserName);
                rep.SetParameterValue("status", (info.Status ? "Успешно" : "Произошла ошибка"));
                rep.SetParameterValue("up_date", info.UpDate.ToString("dd.MM.yyyy HH:mm:ss"));
                rep.SetParameterValue("down_date", info.DownDate.ToString("dd.MM.yyyy HH:mm:ss"));
                rep.SetParameterValue("down_count_ls", info.DownCountLS);
                rep.SetParameterValue("nosync_count_ls", info.NosyncCountLS);
                rep.SetParameterValue("sync_count_ls", info.DownCountLS - info.NosyncCountLS);
                rep.SetParameterValue("on_date", info.Month + "." + info.Year);
                rep.SetParameterValue("point", points);
                rep.SetParameterValue("comments", info.Comments);

                rep.Prepare();
                ret.tag = rep.Report.PreparedPages.Count;

                string dir = InputOutput.useFtp ? InputOutput.GetOutputDir() : Constants.ExcelDir;

                var exportXls = new FastReport.Export.OoXML.Excel2007Export();
                fileName = "protocol_SZ" + DateTime.Now.ToShortDateString().Replace(".", "_") + "_" +
                    DateTime.Now.ToShortTimeString().Replace(":", "_") + ".xlsx";
                exportXls.ShowProgress = false;
                exportXls.Export(rep, Path.Combine(dir, fileName));
                rep.Dispose();

                if (InputOutput.useFtp)
                    fileName = InputOutput.SaveOutputFile(Path.Combine(dir, fileName));

            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка в функции GetProtocolSZ:\n" + ex.Message, MonitorLog.typelog.Error, true);
            }
            finally
            {
                if (ret.result)
                {
                    excelRepDb.SetMyFileProgress(new ExcelUtility { nzp_exc = nzpExc, progress = 1 });
                    excelRepDb.SetMyFileState(new ExcelUtility { nzp_exc = nzpExc, status = ExcelUtility.Statuses.Success, exc_path = fileName });
                }
                else
                {
                    excelRepDb.SetMyFileState(new ExcelUtility { nzp_exc = nzpExc, status = ExcelUtility.Statuses.Failed });
                }
                excelRepDb.Close();
            }
        }

		/// <summary>
		/// Возвращает список колонок в таблице - только для PG
		/// </summary>
        /// <param name="connDB">соединение</param>
		/// <param name="trans"></param>
		/// <param name="schema">схема</param>
		/// <param name="table">таблица</param>
        /// <param name="excludeSerial">исключить серийник из списка</param>
		/// <returns></returns>
        public List<string> ListColumnsInTable(IDbConnection connDB, IDbTransaction trans, string schema, string table,
            bool excludeSerial = false) {
			var res = new List<string>();

			var filter = "";
			if (excludeSerial)
			{
				filter = " AND " + sNvlWord + "(column_default,'') not like '%nextval%' ";
			}
			var sql = " SELECT column_name FROM information_schema.columns " +
					  " WHERE table_schema = " + Utils.EStrNull(schema.Replace(".", "")) + " AND table_name   = " + Utils.EStrNull(table) + " " + filter;
			var dt = ClassDBUtils.OpenSQL(sql, connDB, trans).resultData;
			for (int i = 0; i < dt.Rows.Count; i++)
			{
				res.Add(CastValue<string>(dt.Rows[i]["column_name"]).Trim());
			}

			return res;
		}
	}


}

