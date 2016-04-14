using System;
using System.Data;
using System.Linq;
using System.Xml.Linq;
using System.IO;
using System.Text;
using System.Globalization;
using System.Threading;
using System.Collections.Generic;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;
using SevenZip;
using System.Text.RegularExpressions;
using System.Collections;
using System.Diagnostics;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Security.Cryptography;

namespace STCLINE.KP50.DataBase
{
    using STCLINE.KP50.Utility;

    public class DbPatch : DataBaseHead
    {
        private string key1 = "l!xU[kDm]Htuvc=EG/aoBSDjCUmG6\\0(}HFsN+2mv,jEvOT]4VTjKT6s!)ib]aKl.%n3+~\"nW6x*x84_)?I[E3F\\jgh2vJKPf9=ze]U0:(_+c\\/yfb1,8)vt[6/ZKwf,";
        private string key2 = "G№%J37)\"/;>/]s>AOxb6v~4\"[=88LiMX~Y?81,\\A!\"xyJkwl1m,9?{;n]4i\\L+Bi\\\"jzzFYN3xMpt:Ocq2g!TT{f8L;Qs,=4v:5R1p\\cG\\9zq[*UN:P2YA\\zm\\c8THt5";

        //процедура выполнения SQL строки
        public Dictionary<string, object> GoPatch_DB(out Returns ret, ArrayList sqlArray, string dataBaseType, byte[] soup)
        {
            MonitorLog.WriteLog("Старт процедуры GoPatch_DB", MonitorLog.typelog.Info, true);
            ret = Utils.InitReturns();
            string SQL_String = "";
            foreach (string str in sqlArray)
            {
                //Расшифровка SQL запроса  
                SQL_String += Encryptor.Decrypt(str, soup) + " ";
            }
            SQL_String = SQL_String.Trim();
            //if (pref == "")
            //{
            //    ret.text = "Префикс базы данных не задан";
            //    return null;
            //}
            MonitorLog.WriteLog("Запрос=" + SQL_String, MonitorLog.typelog.Info, true);
            //Подключения к базам
            IDbConnection conn_db;
            if (dataBaseType == "cache")
            {
                conn_db = GetConnection(Constants.cons_Webdata);//new IDbConnection(Constants.cons_Webdata);
                //conn_db = new IDbConnection(" Client Locale=ru_ru.CP1251;Database=sav08_data;Database Locale=ru_ru.915;Server=ol_zenit;UID = informix;Pwd = info ");
            }
            else
            {
                conn_db = GetConnection(Constants.cons_Kernel);//new IDbConnection(Constants.cons_Kernel);
            }

            IDataReader reader = null;

            ret = OpenDb(conn_db, true);
            if (!ret.result)
            {
                return null;
            }

            //IDbTransaction trans = null;
            StringBuilder sql = new StringBuilder();
            //----------------------------Парсим входную строку SQL------------------------------------
            //List<string> l = SQL_String.Split(new char[] {';'}).ToList<string>;
            string[] sqlMas = SQL_String.Split(new char[] { ';' });
            List<string> list = new List<string>(sqlMas);

            list.ForEach(delegate(string s)
            {
                if (s == "")
                    list.Remove(s);
            });


            //сохранение в файл
            for (int f = 0; f < list.Count; f++)
            {
                list[f] = list[f] + ";";
            }

            string[] sqlMasString = list.ToArray();

            StreamWriter sw = new StreamWriter(@"C:\2.txt");
            foreach (string str in sqlMasString)
            {
                sw.WriteLine(str);
                sw.Flush();
            }
            sw.Close();
            ///////    
            //IDbTransaction trans ;
            //try
            //{
            //    trans = conn_db.BeginTransaction();
            //}
            //catch (Exception)
            //{
            //    MonitorLog.WriteLog("Транзакции не поддерживаются!", MonitorLog.typelog.Error, true);
            //    trans = null;
            //}
            //////
            for (int i = 0; i < sqlMasString.Length; i++)
            {
                if (sqlMasString[i].Trim() != "")
                {
                    reader = null;
                    sql.Append(SQL_String);

                    Returns retQ = ExecRead(conn_db, out reader, sqlMasString[i], true, 600);
                    if (!retQ.result)
                    {
                        MonitorLog.WriteLog("Ошибка выборки " + sqlMasString[i] + ' ' + retQ.text, MonitorLog.typelog.Error, 20, 201, true);
                        //если сломался один из запросов, то выполняется откат
                        //trans.Rollback();

                        conn_db.Close();
                        ret.result = false;

                        Dictionary<string, object> retDic = new Dictionary<string, object>();
                        retDic.Add("#error", retQ.sql_error);
                        return retDic;
                        //return null;
                    }
                }
            }

            //ридер содержит результат последнего запроса
            try
            {

                Dictionary<string, object> retDic = new Dictionary<string, object>();
                if (reader != null)
                {
                    int ind = 0;
                    while (reader.Read())
                    {
                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            if (reader[i] != DBNull.Value)
                            {
                                retDic.Add(reader.GetName(i) + "#" + ind, reader[i].ToString());
                            }
                        }
                        ind++;
                    }
                }

                //trans.Commit();
                if (reader != null)
                {
                    reader.Close();
                }
                sql.Remove(0, sql.Length);
                MonitorLog.WriteLog(retDic.Count.ToString(), MonitorLog.typelog.Info, true);
                return retDic;
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog(ex.Message, MonitorLog.typelog.Error, true);
                return null;
            }
            finally
            {
                conn_db.Close();
            }
        }

        //процедура выполнения незашифрованной SQL строки
        public void GoScript_DB(out Returns ret, ArrayList sqlArray, string ConnStr)
        {
            MonitorLog.WriteLog("Старт процедуры GoPatch_DB", MonitorLog.typelog.Info, true);
            ret = Utils.InitReturns();
            Returns ret2 = Utils.InitReturns();
            string SQL_String = "";
            foreach (string str in sqlArray)
            {
                int index = str.IndexOf("--");
                if ((index != 0) || (str.Trim().Length == 0))
                {
                    string str2 = str.Trim();
                    if (index > 0)
                    {
                        str2 = str.Substring(0, index).Trim();
                    }
                    SQL_String += str2 + " ";
                }
            }
            SQL_String = SQL_String.Trim();

            //Подключения к базам
            IDbConnection conn_db = GetConnection(ConnStr);

            ret = OpenDb(conn_db, true);
            if (!ret.result)
            {
                return;
            }

            StringBuilder sql = new StringBuilder();
            //----------------------------Парсим входную строку SQL------------------------------------
            string[] sqlMas = SQL_String.Split(new char[] { ';' });
            List<string> list = new List<string>(sqlMas);

            list.ForEach(delegate(string s)
            {
                if (s == "")
                    list.Remove(s);
            });

            //сохранение в файл
            for (int f = 0; f < list.Count; f++)
            {
                list[f] = list[f] + ";";
                if (list[f].IndexOf("CREATE PROCEDURE") != -1)
                {
                    int k = f + 1;
                    list[f] += " ";
                    do
                    {
                        list[f] += list[k] + "; ";
                        list.Remove(list[k]);
                    }
                    while (list[k].IndexOf("END PROCEDURE") == -1);
                    list[f] += list[k] + ";";
                    list.Remove(list[k]);
                }
            }

            string[] sqlMasString = list.ToArray();

            //StreamWriter sw = new StreamWriter(@"C:\2.txt");
            //foreach (string str in sqlMasString)
            //{
            //    sw.WriteLine(str);
            //    sw.Flush();
            //}
            //sw.Close();

            bool firstdrop = true;

            for (int i = 0; i < sqlMasString.Length; i++)
            {
                if (sqlMasString[i].Trim() != "")
                {
                    //sql.Append(SQL_String);
                    if (sqlMasString[i].IndexOf("CREATE PROCEDURE") != -1)
                    {
                        try
                        {
                            if (firstdrop)
                            {
                                string ProcName = "DROP PROCEDURE " + sqlMasString[i].Substring(18, sqlMasString[i].IndexOf('(') - 18) + ";";
                                Returns retQ = ExecSQL(conn_db, ProcName, true);
                                if (!retQ.result)
                                {
                                    MonitorLog.WriteLog("Ошибка удаления процедуры командой: " + ProcName + ", возможно процедуры не существует ", MonitorLog.typelog.Info, 20, 201, true);
                                }
                                firstdrop = !firstdrop;
                            }
                        }
                        finally
                        {
                            try
                            {
                                IDbCommand cmd = DBManager.newDbCommand(sqlMasString[i], conn_db);
                                cmd.ExecuteScalar();
                            }
                            catch (Exception ex)
                            {
                                MonitorLog.WriteLog("Ощибка создания процедуры командой: " + sqlMasString[i] + " " + ex.Message, MonitorLog.typelog.Error, 20, 201, true);
                                conn_db.Close();
                                ret.result = false;
                            }
                        }
                    }
                    else
                    {
                        Returns retQ = ExecSQL(conn_db, sqlMasString[i], true);
                        if (!retQ.result)
                        {
                            MonitorLog.WriteLog("Ошибка выборки " + sqlMasString[i] + ' ' + retQ.text, MonitorLog.typelog.Error, 20, 201, true);

                            conn_db.Close();
                            ret.result = false;
                            return;
                        }
                    }
                    //sql.Remove(0, sql.Length);
                }
            }
            ret.result = true;
            MonitorLog.WriteLog("webupdate_rt успешно выполнен", MonitorLog.typelog.Info, 20, 201, true);
            conn_db.Close();
        }


        //----------------------------------Перенос из BaseHead---------------------------------------
        //Проверка на доступные обновления
        public System.Collections.ArrayList CheckForUpdates(out Returns ret, string connectionStr, string typeUpdate)
        {
            System.Collections.ArrayList PathList = new System.Collections.ArrayList();

            //Returns ret = Utils.InitReturns();     
            ret = Utils.InitReturns();
            //заполнить webdata:tXX_cnt
            IDbConnection conn_db = GetConnection(connectionStr);//new IDbConnection(connectionStr);
            IDataReader reader;


            ret = OpenDb(conn_db, true);
            if (!ret.result)
            {
                return null;
            }

            //string cur_pref = US.pref;
            StringBuilder sql = new StringBuilder();

#if PG
            sql.Append(" Select * from updater where  status = 0 and typeup =\'" + typeUpdate + "\' ORDER BY version Asc ");    //nzp_up, path, version,  status ,key, soup  
#else
 sql.Append(" Select * from updater where  status = 0 and typeup =\'" + typeUpdate + "\' ORDER BY version Asc ");    //nzp_up, path, version,  status ,key, soup  
#endif

            if (!ExecRead(conn_db, out reader, sql.ToString(), true).result)
            {
                MonitorLog.WriteLog("Ошибка выборки " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                //reader.Close();
                sql.Remove(0, sql.Length);
                conn_db.Close();
                ret.result = false;
                return null;
            }
            try
            {
                while (reader.Read())
                {
                    UpData updata = new UpData();

                    if (reader["path"] != DBNull.Value) updata.Path = Convert.ToString(reader["path"]).Trim().Replace("\\", "\\\\");
                    if (reader["version"] != DBNull.Value) updata.Version = (double)(reader["version"]);
                    if (reader["status"] != DBNull.Value) updata.status = Convert.ToString(reader["status"]).Trim();
                    if (reader["key"] != DBNull.Value) updata.key = Convert.ToString(reader["key"]).Trim();
                    if (reader["soup"] != DBNull.Value) updata.soup = Convert.ToString(reader["soup"]).Trim();
                    if (reader["nzp_up"] != DBNull.Value) updata.nzp = Convert.ToString(reader["nzp_up"]).Trim();
                    if (reader["web_path"] != DBNull.Value) updata.web_path = Convert.ToString(reader["web_path"]).Trim();
                    if (reader["typeup"] != DBNull.Value) updata.typeUp = Convert.ToString(reader["typeup"]).Trim();


                    PathList.Add(updata);
                }

                sql.Remove(0, sql.Length);
                if (reader != null)
                {
                    reader.Close();
                }

                return PathList;
            }
            finally
            {
                sql.Remove(0, sql.Length);
                if (reader != null)
                {
                    reader.Close();
                }
                conn_db.Close();
            }
        }

        //Получение успешных и неуспешных обновлений хоста
        public System.Collections.Generic.List<UpData> GetUdatesDB(ref Returns ret, string connectionStr, System.Collections.ArrayList UpdList, string typeUp, bool AllUpdates)
        {
            System.Collections.Generic.List<UpData> UpdLis = new System.Collections.Generic.List<UpData>();

            //Returns ret = Utils.InitReturns();     
            ret = Utils.InitReturns();
            //заполнить webdata:tXX_cnt
            IDbConnection conn_db = GetConnection(connectionStr);//new IDbConnection(connectionStr);
            IDataReader reader;
            //IDataReader reader_2;


            ret = OpenDb(conn_db, true);
            if (!ret.result)
            {
                return null;
            }

            //StringBuilder sql = new StringBuilder();
            StringBuilder sql_2 = new StringBuilder();

            string Select = "SELECT ";
            if (AllUpdates)
            {
                Select += "* FROM updater;";
                sql_2.Append(Select);
            }
            else
            {
                Select += "FIRST 1 * FROM updater WHERE typeup = ";

                switch (typeUp)
                {
                    case "0":
                        {
                            sql_2.Append(Select + "'host' ORDER BY dateup DESC ;");
                            break;
                        }
                    case "1":
                        {
                            sql_2.Append(Select + "'web' ORDER BY dateup DESC ;");
                            break;
                        }
                    case "2":
                        {
                            sql_2.Append(Select + "'broker' ORDER BY dateup DESC ;");
                            break;
                        }
                    case "3":
                        {
                            sql_2.Append(Select + "'script' ORDER BY dateup DESC ;");
                            break;
                        }
                    case "4":
                        {
                            sql_2.Append(Select + "'updhost' ORDER BY dateup DESC ;");
                            break;
                        }
                    case "5":
                        {
                            sql_2.Append(Select + "'updbroker' ORDER BY dateup DESC ;");
                            break;
                        }
                    default:
                        {
                            return null;
                        }
                }
            }

            if (!ExecRead(conn_db, out reader, sql_2.ToString(), true).result)
            {
                MonitorLog.WriteLog("Ошибка выборки " + sql_2.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                conn_db.Close();
                sql_2.Remove(0, sql_2.Length);
                ret.result = false;
                return null;
            }
            try
            {
                while (reader.Read())
                {
                    UpData updata = new UpData();

                    if (reader["status"] != DBNull.Value) updata.status = Convert.ToString(reader["status"]).Trim();
                    //if (updata.status == "0")
                    //{
                    //    return null;
                    //}
                    if (reader["path"] != DBNull.Value) updata.Path = Convert.ToString(reader["path"]).Trim().Replace("\\", "\\\\");
                    if (reader["version"] != DBNull.Value) updata.Version = (double)(reader["version"]);
                    if (reader["key"] != DBNull.Value) updata.key = Convert.ToString(reader["key"]).Trim();
                    if (reader["soup"] != DBNull.Value) updata.soup = Convert.ToString(reader["soup"]).Trim();
                    if (reader["nzp_up"] != DBNull.Value) updata.nzp = Convert.ToString(reader["nzp_up"]).Trim();
                    if (reader["report"] != DBNull.Value) updata.report = Convert.ToString(reader["report"]).Trim();
                    if (reader["typeup"] != DBNull.Value) updata.typeUp = Convert.ToString(reader["typeup"]).Trim();
                    if (reader["dateup"] != DBNull.Value) updata.date = Convert.ToString(reader["dateup"]).Trim();


                    UpdLis.Add(updata);
                }
                sql_2.Remove(0, sql_2.Length);
                if (reader != null)
                {
                    reader.Close();
                }
                return UpdLis;
            }
            finally
            {
                if (reader != null)
                {
                    reader.Close();
                }
                sql_2.Remove(0, sql_2.Length);
                conn_db.Close();
            }
        }
        //--------------------------------------------------------------------------------------------

        // возвращает события в monitorlog за опред. отрезок времени
        public List<EventLogEntry> GetMonitorLog_Host(DateTime BeginDate, DateTime EndDate)
        {
            List<EventLogEntry> Result = new List<EventLogEntry>();
            System.Diagnostics.EventLog name = new System.Diagnostics.EventLog();
            name.Source = "Komplat50Log";
            foreach (System.Diagnostics.EventLogEntry entry in name.Entries)
            {
                if ((DateTime.Compare(BeginDate, entry.TimeWritten) <= 0) && (DateTime.Compare(EndDate, entry.TimeWritten) >= 0))
                {
                    Result.Add(entry);
                }
            }
            return Result;
        }

        // выполняет набор sql команд
        public void ExecSQLList(string[] List)
        {
            int status = 1;
            string error = "";
            ArrayList SqlList = new ArrayList();
            foreach (string SqlLine in List)
            {
                if (SqlLine.Trim() != "")
                    SqlList.Add(SqlLine.Trim());
            }
            ArrayList SqlResult = new ArrayList();

            Format Formatted = new Format();

            try
            {
                SqlResult = Formatted.ConvertText(SqlList, ref error);
            }
            catch (Exception ex)
            {
                status = 7;
                error += "\r\n\r\nОшибка конвертирования текста " + ex.Message;
            }

            if (error == "")
            {
                try
                {
                    Returns ret = new Returns();
                    SqlResult = new ParserSelect().ExecList(out ret, Constants.cons_Kernel, SqlResult, true, ref error);
                }
                catch (Exception ex)
                {
                    status = 7;
                    error += "\r\n\r\nОшибка полученного запроса выполнения запроса" + ex.Message;
                }
            }

            if (error != "")
            {
                SqlResult = new ArrayList();
                SqlResult.Add(error);
            }
            string FilePath = Path.GetTempPath() + "select";
            StreamWriter sw = new StreamWriter(FilePath + ".txt");
            foreach (string str in SqlResult)
            {
                sw.WriteLine(Crypt.Encrypt(str, key1));
            }
            sw.Close();

            string[] param = new string[1];
            param[0] = FilePath + ".txt";
            try
            {
                SevenZipCompressor compressor = new SevenZipCompressor();
                compressor.CompressFilesEncrypted(FilePath + ".7z", key2, param);
                File.Delete(FilePath + ".txt");
            }
            catch (Exception ex)
            {
                status = 7;
                MonitorLog.WriteLog("Ошибка архивации. Возможно неообходимо заменить файл 7z.dll в UPDATER_EXE на соответствующий битности системы. " + ex.Message, MonitorLog.typelog.Error, true);
            }
            IDbConnection dbconn = GetConnection(Constants.cons_Webdata);
            Returns ret2 = OpenDb(dbconn, true);
            if (!ret2.result)
            {
                MonitorLog.WriteLog("Невозможно подключится к БД", MonitorLog.typelog.Error, true);
            }

            try
            {
                ExecSQL(dbconn, "INSERT INTO updater (dateup, typeup, status, path) VALUES ('" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "', 'script', " + status + ", '" + FilePath + ".7z');", true);
            }
            catch
            {
                MonitorLog.WriteLog("Ошибка добавления данных", MonitorLog.typelog.Error, true);
            }
            finally
            {
                dbconn.Close();
            }

            return;
        }

        public byte[] GetSelect()
        {
            DbPatch db = new DbPatch();
            UpData2 ud2 = new UpData2(db.GetHistoryLast(3));

            byte[] Result = File.ReadAllBytes(ud2.Path);
            File.Delete(ud2.Path);

            return Result;
        }

        public void FullUpdate(byte[] UpdateByte, string UpdateMD5, int UpdateIndex, string WebPath, string pass, string passMD5)
        {
            int PID = Process.GetCurrentProcess().Id; // id процесса и папка с обновлением во временной папке

            #region Подготовка директорий для обновления
            if (!Directory.Exists(Path.GetTempPath() + @"Debug"))
            {
                Directory.CreateDirectory(Path.GetTempPath() + @"Debug");
            }
            if (!Directory.Exists(Path.GetTempPath() + @"Debug\" + PID))
            {
                Directory.CreateDirectory(Path.GetTempPath() + @"Debug\" + PID);
            }
            FileStream Writer = File.Open(Path.GetTempPath() + @"Debug\" + PID + @"\update.7z", FileMode.Append, FileAccess.Write, FileShare.None);
            string UpdatePath = Path.GetTempPath() + @"Debug\" + PID + @"\Update\";
            string CurrentPath;
            if (UpdateIndex == 1)
            {
                CurrentPath = WebPath;
            }
            else
            {
                CurrentPath = Directory.GetCurrentDirectory();
            }
            Writer.Write(UpdateByte, 0, UpdateByte.Length);
            Writer.Close();
            Writer.Dispose();
            if (Directory.Exists(UpdatePath))
            {
                Directory.Delete(UpdatePath, true);
            }
            Directory.CreateDirectory(UpdatePath);

            if (!File.Exists(Path.GetTempPath() + @"7z.dll"))
            {
                File.Copy(Directory.GetCurrentDirectory() + @"\UPDATER_EXE\7z.dll", Path.GetTempPath() + @"7z.dll");
            }
            #endregion

            #region Распаковка обновления
            try
            {
                SevenZipExtractor.SetLibraryPath(Path.GetTempPath() + @"7z.dll");
                SevenZipExtractor extractor;
                if (pass != "")
                {
                    extractor = new SevenZipExtractor(Path.GetTempPath() + @"Debug\" + PID + @"\update.7z", pass);
                }
                else
                {
                    extractor = new SevenZipExtractor(Path.GetTempPath() + @"Debug\" + PID + @"\update.7z");
                }
                extractor.ExtractArchive(UpdatePath);
                extractor.Dispose();
                File.Delete(Path.GetTempPath() + @"Debug\" + PID + @"\update.7z");
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка распаковки обновления: " + ex.Message, MonitorLog.typelog.Error, true);
                return;
            }
            #endregion

            // запуск обновления
            // здесь надо запустить updater.exe с параметрами "Строка подключения, id процесса, логин, пароль, путь к папке с распакованным обновлением, путь к папке для обновления, индекс обновления (0-хост, 1-веб, 2-брокер)"
            string start = Constants.cons_Webdata.Replace(' ', '€') + '♂' + PID + '♂' + Constants.Login + '♂' + Constants.Password + '♂' + UpdatePath.Replace(' ', '€') + '♂' + WebPath.Replace(' ', '€') + '♂' + UpdateIndex.ToString() + "♂0";
            Process.Start(Directory.GetCurrentDirectory() + @"\UPDATER_EXE\KP50.Updater.exe", start);
        }

        public void UpdateUpdater(byte[] UpdaterFile, int UpdateIndex)
        {
            int status = 0;
            String logstr = "";
            string UpdatePath = Path.Combine(Path.GetTempPath(), @"TempFolder");
            string CurrentPath = Path.Combine(Directory.GetCurrentDirectory(), @"UPDATER_EXE");

            bool OK = true;

            try
            {
                FileStream Writer = File.Open(Path.Combine(Path.GetTempPath(), @"updater.7z"), FileMode.Append, FileAccess.Write, FileShare.None);
                Writer.Write(UpdaterFile, 0, UpdaterFile.Length);
                Writer.Close();
                Writer.Dispose();
                logstr += "Обновление записано на диск в файл '" + Path.Combine(Path.GetTempPath(), @"updater.7z'.");
            }
            catch (Exception ex)
            {
                logstr += "Ошибка записи обновления на диск в файл '" + Path.Combine(Path.GetTempPath(), @"updater.7z'. ") + ex.Message;
                OK = false;
            }

            if (OK) try
                {
                    if (Directory.Exists(UpdatePath))
                    {
                        Directory.Delete(UpdatePath, true);
                    }
                    Directory.CreateDirectory(UpdatePath);
                    logstr += "\r\n\r\nСоздана директория '" + UpdatePath + "'.";
                    if (File.Exists(Path.Combine(Path.GetTempPath(), @"7z.dll")))
                    {
                        File.Delete(Path.Combine(Path.GetTempPath(), @"7z.dll"));
                    }
                    File.Copy(Path.Combine(Directory.GetCurrentDirectory(), @"UPDATER_EXE\7z.dll"), Path.Combine(Path.GetTempPath(), @"7z.dll"));
                    SevenZipExtractor.SetLibraryPath(Path.Combine(Path.GetTempPath(), @"7z.dll"));
                    SevenZipExtractor extractor = new SevenZipExtractor(Path.Combine(Path.GetTempPath(), @"updater.7z"));
                    extractor.ExtractArchive(UpdatePath);
                    File.Delete(Path.Combine(Path.GetTempPath(), @"updater.7z"));
                    logstr += "\r\n\r\nФайл распакован успешно\r\n Начало удаление старой резевной копии (если она осталась)...";
                }
                catch (Exception ex)
                {
                    logstr += "\r\n\r\nОшибка распаковки файла в директорию '" + UpdatePath + "'(Текущая директория " + Directory.GetCurrentDirectory() + "). " + ex.Message;
                    OK = false;
                    status = 8;
                }

            if (OK) try
                {
                    FileInfo[] files = (new DirectoryInfo(CurrentPath)).GetFiles();
                    foreach (FileInfo file in files)
                    {
                        if (file.Extension == ".bak")
                        {
                            File.SetAttributes(file.FullName, FileAttributes.Normal);
                            file.Delete();
                            logstr += "Удален файл '" + file.FullName + "'\r\n";
                        }
                    }
                    logstr += "\r\n\r\nРезервная копия успешно удалена.";
                }
                catch
                {
                    status = 6;
                    logstr += "\r\n\r\nОшибка удаления резерной копии.";
                    OK = false;
                }

            DirectoryInfo DINew = new DirectoryInfo(UpdatePath);
            if (OK) try
                {
                    logstr += "Начало процедуры обновления\r\n";
                    FileInfo[] FilesNew = DINew.GetFiles();
                    foreach (FileInfo aFile in FilesNew)
                    {
                        if (File.Exists(Path.Combine(CurrentPath, aFile.Name)))
                        {
                            File.SetAttributes(aFile.FullName, FileAttributes.Normal);
                            File.Move(Path.Combine(CurrentPath, aFile.Name), Path.Combine(CurrentPath, aFile.Name) + ".bak");
                            logstr += "\r\nСоздана резервная копия файла '" + Path.Combine(CurrentPath, aFile.Name) + "'.";
                        }
                        File.Move(aFile.FullName, Path.Combine(CurrentPath, aFile.Name));
                        logstr += "\r\nОбновлен файл '" + Path.Combine(CurrentPath, aFile.Name) + "'.";

                        #region Изменение владельца и безопасности

                        string pathIntern = Path.Combine(CurrentPath, aFile.Name);
                        try
                        {
                            DirectoryInfo diIntern = new DirectoryInfo(pathIntern);
                            DirectorySecurity dsecIntern = diIntern.GetAccessControl();
                            IdentityReference newUser = new NTAccount(Environment.UserDomainName + "\\" + Environment.UserName);
                            dsecIntern.SetOwner(newUser);
                            FileSystemAccessRule permissions = new FileSystemAccessRule(newUser, FileSystemRights.FullControl, AccessControlType.Allow);
                            dsecIntern.AddAccessRule(permissions);
                            diIntern.SetAccessControl(dsecIntern);
                        }
                        catch (Exception ex)
                        {
                            logstr += "Ошибка изменения владельца файла" + Environment.NewLine + pathIntern + Environment.NewLine + ex.Message;
                        }

                        try
                        {
                            FileInfo fInfo = new FileInfo(pathIntern);
                            if (fInfo.Exists)
                            {
                                FileSecurity fSec = fInfo.GetAccessControl();
                                fSec.SetAccessRuleProtection(false, false);
                                fInfo.SetAccessControl(fSec);
                            }
                        }
                        catch (Exception ex)
                        {
                            logstr += "Ошибка изменения безопасности файла" + Environment.NewLine + pathIntern + Environment.NewLine + ex.Message;
                        }

                        #endregion
                    }
                    logstr += "\r\nОбновление завершено.";
                    Directory.Delete(UpdatePath);
                    logstr += "\r\nУдалена папка с обновлением.";
                }
                catch (Exception ex)
                {
                    FileInfo[] FilesNew = new DirectoryInfo(CurrentPath).GetFiles();
                    OK = false;
                    logstr += "\r\nОшибка обновления! " + ex.Message + "\r\nОткат...";
                    try
                    {
                        foreach (FileInfo afile in FilesNew)
                        {
                            if (afile.Extension == ".bak")
                            {
                                string NewBadFile = afile.FullName.Substring(0, afile.FullName.Length - 4);
                                if (File.Exists(NewBadFile))
                                {
                                    File.SetAttributes(NewBadFile, FileAttributes.Normal);
                                    File.Delete(NewBadFile);
                                    logstr += "\r\nУдален файл '" + NewBadFile + "'.";
                                }
                                File.Move(afile.FullName, NewBadFile);
                                logstr += "\r\nВосстановлен файл '" + NewBadFile + "'.";
                            }
                        }
                        status = 2;
                    }
                    catch (Exception ex2)
                    {
                        logstr += "\r\n\r\nОшибка восстановления из резервной копии! " + ex2.Message + "\r\n\r\nТребуется ручное вмешательство!";
                        status = 3;
                    }
                }

            if (OK) try
                {
                    logstr += "\r\n\r\nНачало удаление резевной копии...\r\n";
                    DINew = new DirectoryInfo(CurrentPath);
                    FileInfo[] FilesNew2 = DINew.GetFiles();

                    foreach (FileInfo aFile in FilesNew2)
                    {
                        if (aFile.Extension == ".bak")
                        {
                            File.SetAttributes(aFile.FullName, FileAttributes.Normal);
                            File.Delete(aFile.FullName);
                            logstr += "\r\nУдален файл '" + aFile.FullName + "'.";
                        }
                    }
                    logstr += "\r\nРезервная копия успешно удалена.";
                    status = 1;
                }
                catch (Exception ex)
                {
                    logstr += "\r\nОшибка удаления созданной резервной копии. " + ex.Message;
                    status = 6;
                    OK = false;
                }

            // запись отчета в базу
            logstr += "\r\nЗапись отчета в базу";
            Returns ret = Utils.InitReturns();
            IDbConnection conn = GetConnection(Constants.cons_Webdata);

            ret = OpenDb(conn, true);

            if (!ret.result)
            {
                return;
            }

            StringBuilder sql = new StringBuilder();
            string typeup = "host";
            if (UpdateIndex == 5)
            {
                typeup = "broker";
            }
            sql.Append("INSERT INTO updater (dateup, typeup, status) VALUES ('" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "', 'upd" + typeup + "'," + status.ToString() + ");");

            try
            {
                ret = ExecSQL(conn, sql.ToString(), true);
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения добавления данных в базу " + ex.Message, MonitorLog.typelog.Error, true);
                conn.Close();
                return;
            }

            DbPatch db = new DbPatch();
            UpData up = db.GetHistoryLast(UpdateIndex);

            try
            {
                ret = WriteBlob(up, null, ref logstr);
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка добавления отчета в базу " + ex.Message, MonitorLog.typelog.Error, true);
                return;
            }
            finally
            {
                conn.Close();
            }
            return;
        }

        public void RemoveBackupFiles(DirectoryInfo dir)
        {
            DirectoryInfo[] dirs = dir.GetDirectories();
            foreach (DirectoryInfo subdir in dirs)
            {
                RemoveBackupFiles(subdir);
            }

            FileInfo[] files = dir.GetFiles();
            foreach (FileInfo file in files)
            {
                if ((file.Extension == ".bak") || (file.Extension == ".arl"))
                {
                    File.SetAttributes(file.FullName, FileAttributes.Normal);
                    file.Delete();
                }
            }
        }

        public void RestoreFromBackup(string WebPath, int UpdateIndex)
        {
            int PID = Process.GetCurrentProcess().Id;
            if (Directory.Exists(WebPath))
            {
                Process.Start(Directory.GetCurrentDirectory() + @"\UPDATER_EXE\KP50.Updater.exe", @"NODATA♂NODATA♂NODATA♂NODATA♂NODATA♂" + WebPath.Replace(' ', '€') + @"♂1♂1");
            }
            Process.Start(Directory.GetCurrentDirectory() + @"\UPDATER_EXE\KP50.Updater.exe", @"NODATA♂" + PID + @"♂NODATA♂NODATA♂NODATA♂NODATA♂" + UpdateIndex.ToString() + @"♂1");
        }

        public Stream GetHistoryFull()
        {
            Returns ret = Utils.InitReturns();
            List<STCLINE.KP50.Global.UpData> host_up_list = new List<STCLINE.KP50.Global.UpData>();
            DbPatch db = new DbPatch();
            host_up_list = db.GetUdatesDB(ref ret, Constants.cons_Webdata, null, "0", true);

            List<STCLINE.KP50.Global.UpData> web_up_list = new List<STCLINE.KP50.Global.UpData>();
            web_up_list = db.GetUdatesDB(ref ret, Constants.cons_Webdata, null, "1", true);
            db.Close();

            UpData2[] ud = new UpData2[host_up_list.Count + web_up_list.Count];

            List<UpData> l1 = host_up_list;
            List<UpData> l2 = web_up_list;

            for (int i = 0; i < host_up_list.Count; i++)
            {
                ud[i] = new UpData2(host_up_list[i]);
            }
            for (int i = 0; i < web_up_list.Count; i++)
            {
                ud[host_up_list.Count + i] = new UpData2(web_up_list[i]);
            }

            MemoryStream Result = new MemoryStream();
            BinaryFormatter formatter = new BinaryFormatter();
            try
            {
                formatter.Serialize(Result, ud.ToArray());
            }
            catch (Exception ex)
            {
                string str = ex.Message;
            }
            Result.Seek(0, SeekOrigin.Begin);
            return Result;
        }

        public UpData GetHistoryLast(int count)
        {
            Returns ret = Utils.InitReturns();
            DbPatch db = new DbPatch();
            List<UpData> history = db.GetUdatesDB(ref ret, Constants.cons_Webdata, null, count.ToString(), false);
            return history[0];
        }

        //Процедура добавления отчета в базу
        public Returns WriteBlob(UpData upd, string PathFile, ref string LOGSTR)
        {
            Returns ret = Utils.InitReturns();
            IDbConnection i_connect;
            string LogString = "";


            #region Подключение к БД
            try
            {
                LogString += " \r\n\r\nПопытка подключени к БД...";
                i_connect = GetConnection(Constants.cons_Webdata);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;
                LogString += "\r\n\r\nОшибка подключения к БД.";
                return ret;
            }

            ret = OpenDb(i_connect, true);
            if (!ret.result)
            {
                LogString += "\r\n\r\nОшибка подключения к БД.";
                return ret;
            }
            #endregion
            LogString += "OK";
            StringBuilder sql = new StringBuilder();

            //Загрузка файла отчета в строку байтов
            #region Загрузка файла отчета в  байты
            //string data2 = File.ReadAllText(PathFile,Encoding.GetEncoding("windows-1251"));       
            #endregion

            sql.Append(" UPDATE  updater SET report = ?  where nzp_up = " + "\'" + upd.nzp + "\';");


            #region Выполнение запроса на обновление
            IDbTransaction trans;
            try
            {
                trans = i_connect.BeginTransaction();
            }
            catch (Exception)
            {
                trans = null;
            }
            //Выполнение запроса            
            ret = Utils.InitReturns();
            try
            {
                LogString += "\r\n\r\nПопытка выполнить запрос...";
                IDbCommand cmd = null;
                if (trans != null)
                {
                    cmd = DBManager.newDbCommand(sql.ToString(), i_connect, trans);
                }
                else
                {
                    cmd = DBManager.newDbCommand(sql.ToString(), i_connect);
                }
                //Параметры для запроса
                DBManager.addDbCommandParameter(cmd, "@binaryValue", DbType.String, LOGSTR);//data2;

                int res = cmd.ExecuteNonQuery();
                sql.Remove(0, sql.Length);
                if (trans != null)
                {
                    trans.Commit();
                }
                i_connect.Close();
                ret.text = "Обновлено строк: " + res;
                LogString += "OK";
                return ret;
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = "\r\n\r\nОшибка зписи в базу данных ";
                ret.sql_error = " БД " + i_connect.Database + " \n '" + sql.ToString() + "' \n " + ex.Message;
                string err = ret.text + " \n " + ret.sql_error;
                LogString += err;
                MonitorLog.WriteLog("17" + err, MonitorLog.typelog.Error, 1, 3, true);

                if (Constants.Viewerror)
                {
                    ret.text = err;
                }

                sql.Remove(0, sql.Length);
                if (trans != null)
                {
                    trans.Rollback();
                }
                i_connect.Close();

                return ret;
            }
            #endregion
        }

        public void AddUpdateInfo(string Date, string typeup, string status)
        {
            Returns ret = Utils.InitReturns();
            IDbConnection conn = GetConnection(Constants.cons_Webdata);

            ret = OpenDb(conn, true);

            if (!ret.result)
            {
                return;
            }

            StringBuilder sql = new StringBuilder();
            sql.Append("INSERT INTO updater (dateup, typeup, status) VALUES ('" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "', '" + typeup + "', " + status + " );");
            try
            {
                ret = ExecSQL(conn, sql.ToString(), true);
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка обновления. Ошибка выполнения добавления данных в базу " + ex.Message, MonitorLog.typelog.Error, true);
                return;
            }
        }

        public List<PatchInfo> CreateTableForPatcher(bool RunFromHostMan)
        {
            Returns ret = Utils.InitReturns();
            IDbConnection conn = GetConnection(Constants.cons_Kernel);

            List<PatchInfo> retList = new List<PatchInfo>();

            ret = OpenDb(conn, true);
            if (!ret.result)
            {
                return null;
            }

            string sql_select = "SELECT nzp_wp, bd_kernel " + (RunFromHostMan ? ", bank_number" : "") + " FROM s_point;";
#if PG
            ExecSQL(conn, "set search_path to '" + Points.Pref + "_kernel'", false);
#endif

            if (RunFromHostMan)
            {
                ExecSQL(conn, "alter table s_point ADD bank_number VARCHAR(8);", false);
            }

            if (!TableInWebCashe(conn, "s_point"))
            {
                if (!TableInWebCashe(conn, "s_points"))
                {
                    MonitorLog.WriteLog("Ошибка обновления. Нет таблицы s_point или s_points", MonitorLog.typelog.Error, true);
                    return null;
                }
                else
                {
                    sql_select = "SELECT nzp_wp, bd_kernel FROM s_points;";
                }
            }

            IDataReader reader = null;

            try
            {
                ret = ExecRead(conn, out reader, sql_select, true);
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка обновления. Ошибка выборки из таблицы s_point.  " + ex.Message, MonitorLog.typelog.Error, true);
                conn.Close();
                return null;
            }

            #region Считывание префиксов
            if (reader != null)
            {
                while (reader.Read())
                {
                    PatchInfo pi = new PatchInfo();
                    if (reader["nzp_wp"] != DBNull.Value) pi.wp = Convert.ToInt32(reader["nzp_wp"]);
                    if (reader["bd_kernel"] != DBNull.Value) pi.pref = Convert.ToString(reader["bd_kernel"]).Trim();
                    if (RunFromHostMan)
                    {
                        if (reader["bank_number"] != DBNull.Value) pi.bank_number = Convert.ToString(reader["bank_number"]);
                    }
                    retList.Add(pi);
                }
                reader.Close();
            }
            else
            {
                return null;
            }
            #endregion

#if PG
            sql_select = "SELECT * FROM patcher_history ORDER BY id DESC LIMIT 1;";
#else
            sql_select = "SELECT FIRST 1 * FROM patcher_history ORDER BY patch_date DESC, version DESC;";
#endif

            foreach (var pi in retList)
            {
                #region Считывание результата последнего обновления

                string dbName = pi.pref + "_kernel";
#if PG
                string connToDB = "set search_path to '" + dbName + "'";
                string sql_insert = "INSERT INTO '" + dbName + "'.patcher_history VALUES (default,0,0,'',now());";
#else
                string connToDB = "database " + dbName;
                string sql_insert = "INSERT INTO " + dbName + ":patcher_history VALUES (0,0,0,'',current);";
#endif
#if PG
                string sql_create = "CREATE TABLE patcher_history ( "
#else
                    string sql_create = "CREATE TABLE " + dbName + ":patcher_history ( "
#endif
                + " id SERIAL NOT NULL, "
                + " is_locked INT, "
                + " version INT, "
                + " locked_by VARCHAR(50), "
#if PG
                + " patch_date TIMESTAMP "
#else
                + " patch_date DATETIME YEAR TO SECOND "
#endif
                + " );";

                ExecScalar(conn, connToDB, out ret, false);

                if (!ret.result)
                {
#if PG
                    ExecScalar(conn, "set search_path to '" + Regex.Match(Constants.cons_Kernel, "(?i)(?<=database=)\\w+").Value + "'", out ret, false);
#else
                    ExecScalar(conn, "database " + Regex.Match(Constants.cons_Kernel, "(?i)(?<=database=)\\w+").Value , out ret, false);
#endif
                    continue;
                }

#if PG
                if (!TempTableInWebCashe(conn, dbName + ".patcher_history"))
#else
                if (!TempTableInWebCashe(conn, dbName + ":patcher_history"))
#endif
                {
                    try
                    {
                        ExecScalar(conn, sql_create, out ret, true);
                        ExecScalar(conn, sql_insert, out ret, true);
                    }
                    catch (Exception ex)
                    {
                        MonitorLog.WriteLog("Ошибка обновления. Ошибка создания таблиц для патчера: " + ex.Message, MonitorLog.typelog.Error, true);
                        conn.Close();
                        return null;
                    }
                }

                try
                {
                    ret = ExecRead(conn, out reader, sql_select, true);
                }
                catch (Exception ex)
                {
                    MonitorLog.WriteLog("Ошибка обновления. Ошибка выборки из таблицы patcher_history. " + ex.Message, MonitorLog.typelog.Error, true);
                    conn.Close();
                    return null;
                }

                if (reader != null)
                {
                    while (reader.Read())
                    {
                        if (reader["id"] != DBNull.Value) pi.id = Convert.ToInt32(reader["id"]);
                        if (reader["is_locked"] != DBNull.Value) pi.locked = Convert.ToInt32(reader["is_locked"]);
                        if (reader["version"] != DBNull.Value) pi.version = Convert.ToInt32(reader["version"]);
                        if (reader["locked_by"] != DBNull.Value) pi.locked_by = Convert.ToString(reader["locked_by"]).Trim();
                    }
                    reader.Close();
                }
                #endregion

                if ((pi.version != Constants.VersionDB) || (pi.locked > 0))
                {
                    #region Добавление строки для нового обновления
#if PG
                    sql_insert = "INSERT INTO patcher_history VALUES (default," + pi.locked + "," + pi.version + ",'" + pi.locked_by + "',now());";
#else
                    sql_insert = "INSERT INTO patcher_history VALUES (0," + pi.locked + "," + pi.version + ",'" + pi.locked_by + "',current);";
#endif

                    try
                    {
                        ExecScalar(conn, sql_insert, out ret, true);
                    }
                    catch (Exception ex)
                    {
                        MonitorLog.WriteLog("Ошибка обновления. Ошибка ошибка добавления новой строки для обновления: " + ex.Message, MonitorLog.typelog.Error, true);
                        conn.Close();
                        return null;
                    }

                    try
                    {
                        ret = ExecRead(conn, out reader, sql_select, true);
                    }
                    catch (Exception ex)
                    {
                        MonitorLog.WriteLog("Ошибка обновления. Ошибка выборки из таблицы patcher_history. " + ex.Message, MonitorLog.typelog.Error, true);
                        conn.Close();
                        return null;
                    }

                    if (reader != null)
                    {
                        while (reader.Read())
                        {
                            if (reader["id"] != DBNull.Value) pi.id = Convert.ToInt32(reader["id"]);
                        }
                        reader.Close();
                    }
                    #endregion
                }
            }

            conn.Close();
            return retList;
        }

        public void SetUpdateStatus(PatchInfo pi, int value)
        {
            if (pi.id <= 0) return;

            Returns ret = Utils.InitReturns();
            IDbConnection conn = GetConnection(Constants.cons_Kernel);

            ret = OpenDb(conn, true);
            if (!ret.result)
            {
                return;
            }
#if PG
            string sql_selectDB = "set search_path to '" + pi.pref + "_kernel'";
            string sql_update = "update patcher_history SET is_locked = " + pi.locked.ToString() + ", version = " + value.ToString() + ", locked_by = '" + pi.locked_by + "', patch_date = now() WHERE id = " + pi.id;
#else
            string sql_selectDB = "database " + pi.pref + "_kernel";
            string sql_update = "update patcher_history SET is_locked = " + pi.locked.ToString() + ", version = " + value.ToString() + ", locked_by = '" + pi.locked_by + "', patch_date = current WHERE id = " + pi.id;
#endif

            try
            {
                ExecSQL(conn, sql_selectDB, true);
                ExecSQL(conn, sql_update, true);
            }
            finally
            {
                conn.Close();
            }
        }
    }

    public class PatchInfo
    {
        public int id;
        public int wp;
        public string pref;
        public int locked;
        public int version;
        public string locked_by;
        public string bank_number;
    }

    // класс для преобразования команд псевдоязыка в набор последовательных команд
    class Format
    {
        private ArrayList ConstList;

        //private const string ConstRE = "(?i)^\\s*const\\s+(?<ConstName>\\w+)\\s*=\\s*(?((['\"])?(?<ConstValue>[\\w\\s*.,<>='\"]*)?(\\1))|(?<ConstValue>\\b\\w+\\b[\\w\\s.,{}()]*);)"; // шаблон для констант
        private const string ConstRE = "(?i)(?<=\\s*const\\s+(?<ConstName>\\w+)\\s*=\\s*(['\"]?))(?<ConstValue>[\\w\\s\\d()!@$%^&*()_\\-+='\"<>?/{}.,:;]+?)(?=\\1?;)";
        //private const string ForeachRE = "(?i)^\\s*foreach\\s+(?<fchperem>\\w+)\\s+in\\s+(?((['\"])?(?<fchparam>[\\w\\s*#.,<>='\"]*)?(\\1))|(?<fchparam>#?\\b\\w+\\b))"; // шаблон для foreach
        private const string ForeachRE = "(?i)(?<=^\\s*foreach\\s*)(?<fchperem>[\\w#]+)(\\s*in\\s*)(['\"]?\\s*)(?<fchparam>[\\s\\w!@$%^&*()_\\-+='\"<>?/{}.,:;]+?)(?=\\s*\\3?\\s*{)";
        private const string Foreach2RE = "(?i)^\\s*foreach\\s+(?<fchperem>\\w+)\\s+in\\s+\\((\\s*[\\w])";
        // конструктор, инициализация массивов
        public Format()
        {
            ConstList = new ArrayList();
        }

        // первый проход.
        public ArrayList ConvertText(ArrayList SqlList, ref string error)
        {
            ArrayList Result = new ArrayList();
            ArrayList Pass = new ArrayList();
            string ConnStr = Constants.cons_Webdata;
            string tempstr = "";
            try
            {
                foreach (string str in SqlList)
                {
                    if (tempstr != "")
                    {
                        tempstr += ' ' + str.Trim();
                    }
                    else
                    {
                        tempstr = str.Trim();
                    }
                    if ((str.Trim()[str.Trim().Length - 1] == ';') || (str.Trim()[str.Trim().Length - 1] == '{') || (str.Trim()[str.Trim().Length - 1] == '}')) // т.е. если последний символ строки ;,{ или }, то строка команды закончена
                    {
                        // проверка на const
                        Regex _Regex = new Regex(ConstRE);
                        Match _Match = _Regex.Match(tempstr);
                        if (_Match.Success)
                        {
                            string[] AddConst = new string[2];
                            AddConst[0] = _Match.Groups["ConstName"].ToString().Trim();
                            AddConst[1] = _Match.Groups["ConstValue"].ToString().Trim();
                            ConstList.Add(AddConst);
                        }
                        else
                        {
                            Pass.Add(tempstr);
                        }
                        tempstr = "";
                    }
                    else if ((str.Trim() == "try") || (str.Trim() == "except"))
                    {
                        Pass.Add(tempstr);
                        tempstr = "";
                    }
                }
            }
            catch (Exception ex)
            {
                error += "\r\nОшибка выполнения первого прохода. " + ex.Message;
            }

            ArrayList Pass2 = new ArrayList();

            if (error == "")
            {
                try
                {
                    foreach (string str in Pass)
                    {
                        // проверка на const в foreach
                        string str2 = str;
                        foreach (var itemArr in ConstList)
                        {
                            Regex _Regex = new Regex("#" + ((string[])itemArr)[0] + "[\\s!@$%^&*()_\\-+='\"<>?/{}.,:;]+"); //исправить 2 RegEx'a на один с (?=[...]+)
                            Match _Match = _Regex.Match(str2);
                            if (_Match.Success)
                            {
                                Regex _Regex2 = new Regex("#" + ((string[])itemArr)[0]);
                                Match _Match2 = _Regex2.Match(str2);
                                str2 = _Regex2.Replace(str2, ((string[])itemArr)[1]);
                                //_Match = _Match.NextMatch();
                            }
                        }
                        Pass2.Add(str2);
                    }
                }
                catch (Exception ex)
                {
                    error += "\r\n\r\nОшибка выполнения второго прохода. " + ex.Message;
                }
            }

            if (error == "")
            {
                try
                {
                    int i = 0;
                    while (i < Pass2.Count)
                    {
                        if (Pass2[i].ToString().Trim().ToLower().Contains("connect to webdata;"))
                        {
                            ConnStr = Constants.cons_Webdata;
                        }
                        if (Pass2[i].ToString().Trim().ToLower().Contains("connect to kernel;"))
                        {
                            ConnStr = Constants.cons_Kernel;
                        }
                        if (Pass2[i].ToString().Trim().ToLower().Contains("foreach"))
                        {
                            string perem = "", param = "";
                            Regex _Regex = new Regex(ForeachRE);
                            Match _Match = _Regex.Match(Pass2[i].ToString().Trim());
                            if (_Match.Success)
                            {
                                perem = _Match.Groups["fchperem"].ToString();
                                param = _Match.Groups["fchparam"].ToString().Replace('\'', ' ');
                                int level = 1;
                                ArrayList ForeachList = new ArrayList();
                                while (level > 0)
                                {
                                    i++;
                                    if (Pass2[i].ToString().Trim()[Pass2[i].ToString().Trim().Length - 1] == '}')
                                    {
                                        level--;
                                        if (level > 0)
                                        {
                                            ForeachList.Add(Pass2[i].ToString().Trim());
                                        }
                                    }
                                    else
                                    {
                                        ForeachList.Add(Pass2[i].ToString().Trim());
                                        if (Pass2[i].ToString().Trim()[Pass2[i].ToString().Trim().Length - 1] == '{')
                                        {
                                            level++;
                                        }
                                    }
                                }
                                ParserSelect ps = new ParserSelect();
                                ArrayList ForeachZnach = new ArrayList();
                                Returns ret = Utils.InitReturns();
                                ArrayList paramarray = new ArrayList();
                                paramarray.Add(param);
                                ForeachZnach = ps.ExecList(out ret, ConnStr, paramarray, false, ref error);
                                if (ForeachZnach != null)
                                {
                                    ForeachZnach = FormatForeach(perem, ForeachZnach, ForeachList, ref error);
                                    foreach (string str in ForeachZnach)
                                    {
                                        Result.Add(str);
                                    }
                                }
                            }
                        }
                        else
                        {
                            Result.Add(Pass2[i].ToString());
                            i++;
                        }
                    }
                }
                catch (Exception ex)
                {
                    error += "\r\nОшибка обработки foreach " + ex.Message;
                }
            }

            return Result;
        }

        public ArrayList FormatForeach(string perem, ArrayList znach, ArrayList strings1, ref string error)
        {
            ArrayList Result = new ArrayList();
            foreach (string zhachperem in znach)
            {
                int i = 0;
                ArrayList strings = new ArrayList();
                while (i < strings1.Count)
                {
                    foreach (string str in strings1)
                    {
                        Regex _Regex = new Regex("#" + perem + "[\\s<>.,'\"+={}();_]+");
                        Match _Match = _Regex.Match(str);
                        if (_Match.Success)
                        {
                            string tempstr = str;
                            while (_Match.Success)
                            {
                                _Regex = new Regex("#" + perem);
                                tempstr = _Regex.Replace(tempstr, zhachperem);
                                _Match = _Match.NextMatch();
                            }
                            strings.Add(tempstr);
                        }
                        else
                        {
                            strings.Add(str);
                        }
                        i++;
                    }
                }
                i = 0;
                while (i < strings.Count)
                {
                    if (strings[i].ToString().Trim().ToLower().Contains("foreach"))
                    {
                        string perem2 = "", param2 = "";
                        Regex _Regex = new Regex(ForeachRE);
                        Match _Match = _Regex.Match(strings[i].ToString().Trim());
                        if (_Match.Success)
                        {
                            perem2 = _Match.Groups["fchperem"].ToString();
                            param2 = _Match.Groups["fchparam"].ToString();
                            int level = 1;
                            ArrayList ForeachList = new ArrayList();
                            while (level > 0)
                            {
                                i++;
                                if (strings[i].ToString().Trim()[strings[i].ToString().Trim().Length - 1] == '}')
                                {
                                    level--;
                                    if (level > 0)
                                    {
                                        ForeachList.Add(strings[i].ToString().Trim());
                                    }
                                }
                                else
                                {
                                    ForeachList.Add(strings[i].ToString().Trim());
                                    if (strings[i].ToString().Trim()[strings[i].ToString().Trim().Length - 1] == '{')
                                    {
                                        level++;
                                    }
                                }
                            }
                            ParserSelect ps = new ParserSelect();
                            ArrayList ForeachZnach = new ArrayList();
                            Returns ret = Utils.InitReturns();
                            ArrayList paramarray = new ArrayList();
                            paramarray.Add(param2);
                            ForeachZnach = ps.ExecList(out ret, Constants.cons_Kernel, paramarray, false, ref error);
                            if (ForeachZnach != null)
                            {
                                ForeachZnach = FormatForeach(perem, ForeachZnach, ForeachList, ref error);
                                foreach (string str in ForeachZnach)
                                {
                                    Result.Add(str);
                                }
                            }
                        }
                    }
                    else
                    {
                        if (strings[i].ToString().Trim()[strings[i].ToString().Trim().Length - 1] != '}')
                        {
                            Result.Add(strings[i]);
                        }
                        i++;
                    }
                }
            }
            return Result;
        }
    }

    // выполняет посл. команд (включая блоки try-except)
    class ParserSelect : DataBaseHead
    {
        private const string DataBase = @"(?i)(?<=database\s*=\s*)[\w\d]*?(?=[\s]*;)";
        private string BeginDB = "";

        public ParserSelect()
        {
        }

        private string GetDbFromConnStr(string Connstr)
        {
            string webXXX = "";
            Regex regex = new Regex(DataBase);
            Match match = regex.Match(Connstr);
            if (match.Success)
            {
                webXXX = match.Value;
            }
            return webXXX;
        }

        public ArrayList ExecList(out Returns ret, string ConnStr, ArrayList SqlList, bool NotForeach, ref string error)
        {
            ArrayList Result = new ArrayList();
            IDbConnection db_conn = GetConnection(ConnStr);
            BeginDB = GetDbFromConnStr(ConnStr);
            IDataReader reader;
            ret = Utils.InitReturns();

            ret = OpenDb(db_conn, true);
            if (!ret.result)
            {
                error += "\r\nОшибка подключения к БД";
                return null;
            }

            int i = 0;
            while (i < SqlList.Count)
            {
                if (SqlList[i].ToString().ToLower().Contains("connect to webdata;"))
                {
                    ExecSQL(db_conn, "database " + BeginDB + ";", true);
                    db_conn.Close();
                    ConnStr = Constants.cons_Webdata;
                    db_conn = GetConnection(ConnStr);
                    BeginDB = GetDbFromConnStr(ConnStr);
                    ret = OpenDb(db_conn, true);
                    if (!ret.result)
                    {
                        error += "\r\nОшибка подключения к webdata";
                        return null;
                    }
                    i++;
                    continue;
                }

                if (SqlList[i].ToString().ToLower().Contains("connect to kernel;"))
                {
                    ExecSQL(db_conn, "database " + BeginDB + ";", true);
                    db_conn.Close();
                    ConnStr = Constants.cons_Kernel;
                    db_conn = GetConnection(ConnStr);
                    BeginDB = GetDbFromConnStr(ConnStr);
                    ret = OpenDb(db_conn, true);
                    if (!ret.result)
                    {
                        error += "\r\nОшибка подключения к kernel";
                        return null;
                    }
                    i++;
                    continue;
                }

                if (SqlList[i].ToString() == "try")
                {
                    i++;
                    while (SqlList[i].ToString() != "except")
                    {
                        try
                        {
                            ExecSQL(db_conn, SqlList[i].ToString(), true);
                        }
                        finally
                        {
                            i++;
                        }
                    }
                    i++;
                }
                else
                {
                    if (SqlList[i].ToString().Length > 6)
                    {
                        if (SqlList[i].ToString().Substring(0, 6).ToLower().Contains("select"))
                        {
                            if (!ExecRead(db_conn, out reader, SqlList[i].ToString(), true).result)
                            {
                                error += "Ошибка выборки " + SqlList[i].ToString() + ' ' + ret.text;
                                MonitorLog.WriteLog("Ошибка выборки " + SqlList[i].ToString(), MonitorLog.typelog.Error, 20, 201, true);
                                db_conn.Close();
                                ret.result = false;
                                return null;
                            }
                            if (reader != null)
                            {
                                bool empty = true;
                                while (reader.Read())
                                {
                                    if (empty && NotForeach)
                                    {
                                        bool first = true;
                                        string Header = "";
                                        DataTable schemaTable = reader.GetSchemaTable();
                                        foreach (DataRow myField in schemaTable.Rows)
                                        {
                                            if (first)
                                            {
                                                Header = myField.ItemArray[0].ToString();
                                                first = false;
                                            }
                                            else
                                            {
                                                Header += '|' + myField.ItemArray[0].ToString();
                                            }
                                        }
                                        Result.Add(Header);
                                    }
                                    empty = false;
                                    string OneRow = reader[0].ToString().Trim();
                                    for (int j = 1; j < reader.FieldCount; j++)
                                    {
                                        OneRow += "|" + reader[j].ToString().Trim();
                                    }
                                    Result.Add(OneRow);
                                }
                                reader.Close();
                            }
                        }
                        else
                        {
                            try
                            {
                                ExecSQL(db_conn, SqlList[i].ToString(), true);
                            }
                            catch
                            {
                                error += "Ошибка выполнения команды " + SqlList[i].ToString() + ' ' + ret.text;
                                MonitorLog.WriteLog("Ошибка выполнения команды " + SqlList[i].ToString(), MonitorLog.typelog.Error, 20, 201, true);
                                db_conn.Close();
                                ret.result = false;
                                return null;
                            }
                        }
                    }
                    i++;
                }
            }

            ExecSQL(db_conn, "database " + BeginDB + ";", true);
            db_conn.Close();
            return Result;
        }

        public void AddSelectCount(out Returns ret, string ConnStr, int Count, string date)
        {
            IDbConnection db_conn = GetConnection(ConnStr);
            ret = Utils.InitReturns();

            ret = OpenDb(db_conn, true);
            if (!ret.result)
            {
                return;
            }

            string sql = "UPDATE updater SET status = " + Count.ToString() + " WHERE dateup = '" + DateTime.Parse(date).ToString("yyyy-MM-dd HH:mm:ss") + "' AND typeup = 'script'";

            try
            {
                ret = ExecSQL(db_conn, sql, true);
                return;
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка добавление в статус выборки " + ex.Message, MonitorLog.typelog.Error, true);
                return;
            }
        }
    }
}
