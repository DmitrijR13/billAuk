using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.IO;
using System.Reflection.Emit;
using System.Text;
using SevenZip;
using STCLINE.KP50;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;

namespace Bars.KP50.Gubkin
{
    public class DbMURC : DataBaseHead
    {


        //для функции UploadMURCPayment
        public struct structPack
        {
            public int nzp_pack;
            public DateTime date_pack;
        };

       
        /// <summary>
        /// Загрузка файла "Оплаты МУРЦ"
        /// </summary>
        /// <param name="finder">файл</param>
        /// <returns>результат</returns>
        public Returns UploadMURCPayment(FilesImported finder)
        {
            Returns ret = Utils.InitReturns();

            //директория файла
            string fDirectory = Constants.Directories.ImportDir.Replace("/", "\\");
            // fDirectory = "D:\\work.php\\KOMPLAT.50\\WEB\\WebKomplat5\\ExcelReport\\import\\";

            //имя файла
            string fileName = Path.Combine(fDirectory, finder.saved_name);

            if (InputOutput.useFtp) InputOutput.DownloadFile(finder.saved_name, fileName);

            //версия файла
            int nzp_version = -1;
            FileInfo[] files = new FileInfo[1];

            #region Разархивация файла

            using (SevenZipExtractor extractor = new SevenZipExtractor(fileName))
            {
                //создание папки с тем же именем
                DirectoryInfo exDirectorey =
                    Directory.CreateDirectory(Path.Combine(fDirectory,
                        finder.saved_name.Substring(0, finder.saved_name.LastIndexOf('.'))));
                extractor.ExtractArchive(exDirectorey.FullName);
                files = exDirectorey.GetFiles("*.mdb");
                if (files.Length == 0)
                {
                    ret.result = false;
                    ret.text = "Архив пустой";
                    ret.tag = -1;
                    return ret;
                }
            }

            #endregion

            #region Переменные

            List<string> sqlStr = new List<string>();
            StringBuilder err = new StringBuilder();
            string commStr = "";

            #endregion

            #region Вставка файла

            using (IDbConnection con_db = GetConnection(Constants.cons_Kernel))
            {
                try
                {
                    ret = OpenDb(con_db, true);

                    if (!ret.result)
                    {
                        MonitorLog.WriteLog("Ошибка открытия соединения", MonitorLog.typelog.Error, true);
                        ret.result = false;
                        ret.text = " Файл не загружен, отсутствует подключение к базе данных ";
                        ret.tag = -1;
                        return ret;
                    }

                    int localUSer = finder.nzp_user;

                    /*DbWorkUser db = new DbWorkUser();
                    int localUSer = db.GetLocalUser(con_db, finder, out ret);
                    if (!ret.result)
                    {
                        MonitorLog.WriteLog("Ошибка определения локальног пользователя", MonitorLog.typelog.Error, true);
                        ret.result = false;
                        ret.text = " Ошибка определения локального пользователя ";
                        ret.tag = -1;
                        return ret;
                    }*/
#if PG
                    string sql = " INSERT INTO " + Points.Pref + "_data" + tableDelimiter +
                                 "  files_imported (nzp_file, nzp_version, loaded_name, saved_name, nzp_status, created_by, created_on, file_type) ";
                    sql += " VALUES (default," + nzp_version + ",'" + finder.loaded_name + "',\'" + finder.saved_name +
                           "\',2," + localUSer + ",now(), 3)  ";
#else
                    string sql = " INSERT INTO " + Points.Pref + "_data" + tableDelimiter + "  files_imported (nzp_file, nzp_version, loaded_name, saved_name, nzp_status, created_by, created_on, file_type) ";
                    sql += " VALUES (0," + nzp_version + ",'" + finder.loaded_name + "',\'" + finder.saved_name + "\',2," + localUSer + ",current, 3)  ";
#endif

                    ret = ExecSQL(con_db, sql, true);
                    if (!ret.result)
                    {
                        MonitorLog.WriteLog("Ошибка добавления файла в в таблицу файла " + fileName,
                            MonitorLog.typelog.Error, true);
                        ret.result = false;
                        ret.text = " Ошибка добавления файла в базу данных. ";
                        ret.tag = -1;
                        return ret;
                    }

                    //получение nzp_file
                    finder.nzp_file = GetSerialValue(con_db);

                }
                catch (Exception ex)
                {
                    MonitorLog.WriteLog("Ошибка выполнения процедуры LoadHarGilFondGKU : " + ex.Message,
                        MonitorLog.typelog.Error, true);
                    ret.result = false;
                    return ret;
                }
                //}

                #endregion

                if (files[0].Name != "Оплата МУРЦ.mdb")
                {
                    ret.result = false;
                    ret.text = "Выбран неверный файл";
                    ret.tag = -1;
                    return ret;
                }

                var mdbBase = "Оплата МУРЦ";

                #region Считываем файл

                fileName = files[0].FullName;

                if (System.IO.File.Exists(fileName) == false)
                {
                    ret.result = false;
                    ret.text = "Файл отсутствует по указанному пути";
                    ret.tag = -1;
                    return ret;
                }

                DataTable tbl = new DataTable();
                try
                {
                    OleDbConnection oDbCon = new OleDbConnection();
                    var myConnectionString = @"Provider=Microsoft.ACE.OLEDB.12.0;" +
                                             "Data Source=" + fileName + ";Jet OLEDB:Database Password=password;";
                    oDbCon.ConnectionString = myConnectionString;
                    oDbCon.Open();

                    OleDbCommand cmd = new OleDbCommand();
                    cmd.CommandText = "select * from [" + mdbBase + "]";
                    cmd.Connection = oDbCon;

                    // Адаптер данных
                    OleDbDataAdapter da = new OleDbDataAdapter();
                    da.SelectCommand = cmd;
                    // Заполняем объект данными
                    da.Fill(tbl);
                }
                catch (Exception ex)
                {
                    MonitorLog.WriteLog("Ошибка открытия файла " + fileName + " " + ex.Message,
                        MonitorLog.typelog.Warn, true);
                    ret.result = false;
                    ret.text = "Файл недоступен по указанному пути";
                    ret.tag = -1;
                    return ret;
                }

                #endregion



                #region Собрать запросы

                //using (IDbConnection con_db = GetConnection(Constants.cons_Kernel))
                //{
                try
                {
                    ret = OpenDb(con_db, true);

                    if (!ret.result)
                    {
                        MonitorLog.WriteLog("Ошибка открытия соединения", MonitorLog.typelog.Error, true);
                        ret.result = false;
                        ret.text = " Файл не загружен, отсутствует подключение к базе данных ";
                        ret.tag = -1;
                        return ret;
                    }

                    var num_pack = Convert.ToInt32(tbl.Rows[0][1]);
                    var pack_base = Points.Pref + "_fin_" + finder.year.Substring(2, 2) + tableDelimiter + "pack";
                    var pack_ls_base = Points.Pref + "_fin_" + finder.year.Substring(2, 2) + tableDelimiter + "pack_ls";
                    string sql;

                    List<structPack> numPack = new List<structPack>();
                    numPack.Add(new structPack()
                    {
                        nzp_pack = Convert.ToInt32(tbl.Rows[0][1]),
                        date_pack = Convert.ToDateTime(tbl.Rows[0][3])
                    });

                    //для записи пачек
                    decimal sumPack = 0; //сумма
                    int countPack = 0; //количество записей

                    // определить операционный день 
                    string dat_oper_day = "";
                    string sqlr = "Select dat_oper From " + Points.Pref + "_data" + tableDelimiter + "fn_curoperday ";
                    var dtr = ClassDBUtils.OpenSQL(sqlr, con_db);
                    foreach (DataRow rrR in dtr.resultData.Rows)
                    {
                        dat_oper_day = Convert.ToString(rrR["dat_oper"]).Substring(0, 10);
                    }



                    num_pack = Convert.ToInt32(tbl.Rows[0][1].ToString());
                    int nzp_pack = 0;
                    int counter = 0;
                    decimal nzp_pack1 = 0;
                    foreach (DataRow row in tbl.Rows)
                    {
                        counter++;

                        #region добавление пачки

                        if (Convert.ToInt32(row[1]) != num_pack || counter == 1)
                        {
                            num_pack = Convert.ToInt32(row[1]);

                            string file_name = "Оплата МУРЦ.mdb";

                            //если это не первая пачка, то заносит подсчитанную сумму и количество пачек
                            if (counter != 1)
                            {
                                sql = "update " + pack_base + " set count_kv =" + countPack + ", sum_pack = " + sumPack +
                                      " where nzp_pack =" + nzp_pack;
                                ret = ExecSQL(con_db, sql, true);
                            }
                            //заводим новую пачку
                            sql = "insert into " + pack_base +
                                  " ( count_kv, sum_pack, time_inp, dat_uchet, dat_vvod, dat_inp, pack_type, nzp_bank, flag, peni_pack, sum_rasp, real_count, file_name, " +
                                  "par_pack, nzp_supp, nzp_oper, num_charge, yearr, geton_pack, real_sum, real_geton, islock, operday_payer, sum_nrasp, erc_code , num_pack, dat_pack" +

#if PG
                                ") values ( 0, 0 , " + " now(), '" +
#else
 ") values ( 0 , 0 , " + " current year to second, '" +
#endif
                                row[3].ToString().Substring(0, 10) + "', '" + row[18].ToString().Substring(0, 10) +
                                  "', '" + row[3].ToString().Substring(0, 10) +
                                  "', 10, 1999, 11, '0', '0', 0,'" + file_name +
                                  "', null, null, null, null, null, null, null, null, null, null, null, null," +
                                  row[1].ToString() + ", '" + dat_oper_day + "')";

                            ret = ExecSQL(con_db, sql, true);
                            IDbTransaction transaction = null;
                            nzp_pack1 = ClassDBUtils.GetSerialKey(con_db, transaction);
                            nzp_pack = Convert.ToInt32(nzp_pack1);



                            num_pack = Convert.ToInt32(row[1]);
                            sumPack = 0;
                            countPack = 0;
                        }

                        #endregion

                        #region добавление отдельного реестра

                        //перевести внешний код улицы во внутренний
                        var address =
                            GetLsByExtAddress(
                                new FullAddress()
                                {
                                    ulica = row[6].ToString(),
                                    ndom = row[7].ToString(),
                                    nkvar = row[8].ToString()
                                }, con_db);
                        var num_ls = address.num_ls;
                        var pref = address.pref;
                        commStr = "select * from " + pack_ls_base + " where num_ls = " + num_ls;

                        //получаем pkod
                        decimal pkod;
                        sql = "select pkod from " + Points.Pref + "_data" + tableDelimiter + "kvar where num_ls = " +
                              num_ls;
                        var dt = ClassDBUtils.OpenSQL(sql, con_db);
                        if (dt.resultData.Rows.Count > 0 && dt.resultData.Rows[0]["pkod"] != DBNull.Value)
                            pkod = Convert.ToDecimal(dt.resultData.Rows[0]["pkod"]);
                        else pkod = 0;

                        //получаем код квитанции
                        int kodKvitan = num_pack;
                        //sql = "select num_pack from " + pack_base + " where nzp_pack =" + nzp_pack;
                        //dt = ClassDBUtils.OpenSQL(sql, con_db);
                        //if (dt.resultData.Rows.Count > 0 && dt.resultData.Rows[0]["num_pack"] != DBNull.Value) kodKvitan = Convert.ToInt32(dt.resultData.Rows[0]["num_pack"]);
                        //else kodKvitan = 0;

                        //получаем сумму
                        string sumOfPayt = row[11].ToString();
                        if (sumOfPayt[sumOfPayt.Length - 3] != '.')
                            sumOfPayt = sumOfPayt.Substring(0, sumOfPayt.Length - 2);

                        //для пачек
                        sumPack += Convert.ToDecimal(sumOfPayt);
                        countPack++;


                        sql = "insert into " + Points.Pref + "_fin_13" + tableDelimiter +
                              "pack_ls (nzp_pack, prefix_ls, pkod, num_ls, g_sum_ls," +
                              "sum_ls, geton_ls, sum_peni, dat_month, kod_sum, nzp_supp, paysource, id_bill, dat_vvod," +
                              "dat_uchet, info_num, anketa,inbasket, alg, unl, date_distr, date_rdistr, nzp_user, incase, nzp_rs, erc_code, distr_month) values" +
                              "(" + nzp_pack + ", 31 , " + pkod + ", " + num_ls + ", " + sumOfPayt +
                              ", '0', '0', '0', '" + row[0].ToString().Substring(0, 10) + "'," +
                              "33, 0, null, 0, '" + row[3].ToString().Substring(0, 10) + "', null, '" + kodKvitan +
                              "', null, 0, 0, 0, null, null, 0, 0, 0, null, null);";

                        ret = ExecSQL(con_db, sql, true);
                        if (!ret.result) return ret;

                        #endregion
                    }
                    // обновляем последнюю пачку
                    sql = "update " + pack_base + " set count_kv =" + countPack + ", sum_pack = " + sumPack +
                          " where nzp_pack =" + nzp_pack;
                    ret = ExecSQL(con_db, sql, true);

                }
                catch (Exception ex)
                {
                    MonitorLog.WriteLog("Ошибка выполнения процедуры UploadMURCPayment : " + ex.Message,
                        MonitorLog.typelog.Error, true);
                    ret.result = false;
                    return ret;
                }
                finally
                {
                    con_db.Close();
                }
            }

            #endregion



            #region Лог ошибок

            if (err.Length != 0)
            {
                StreamWriter sw = File.CreateText(fileName + ".log");
                sw.Write(err.ToString());
                sw.Flush();
                sw.Close();

                #region Обновление статуса

                using (IDbConnection con_db = GetConnection(Constants.cons_Kernel))
                {
                    try
                    {
                        ret = OpenDb(con_db, true);

                        if (!ret.result)
                        {
                            MonitorLog.WriteLog("Ошибка открытия соединения", MonitorLog.typelog.Error, true);
                            ret.result = false;
                            ret.text = " Файл не загружен, отсутствует подключение к базе данных ";
                            ret.tag = -1;
                            return ret;
                        }

                        string sql = " UPDATE " + Points.Pref + "_data" + tableDelimiter +
                                     "files_imported set nzp_status =  " +
                                     (int)FilesImported.Statuses.LoadedWithErrors;
                        sql += " where nzp_file = " + finder.nzp_file;

                        ret = ExecSQL(con_db, sql, true);
                        if (!ret.result)
                        {
                            MonitorLog.WriteLog("Ошибка обновления статуса файла " + fileName, MonitorLog.typelog.Error,
                                true);
                            ret.result = false;
                            ret.text = " Ошибка обновления статуса файла. ";
                            ret.tag = -1;
                            return ret;
                        }
                    }
                    catch (Exception ex)
                    {
                        MonitorLog.WriteLog("Ошибка выполнения процедуры LoadHarGilFondGKU : " + ex.Message,
                            MonitorLog.typelog.Error, true);
                        ret.result = false;
                        return ret;
                    }
                }

                #endregion

                ret.tag = -1;
                ret.result = false;
                ret.text = "В загруженном файле обнаружились ошибки. Подробности в логе ошибок. ";

                return ret;
            }

            #endregion

            ret.result = true;
            ret.text = "Файл успешно загружен.";
            ret.tag = -1;

            return ret;
        }

        /// <summary>
        /// Выгрузка оплат МУРЦ
        /// </summary>
        /// <returns></returns>
        public Returns GenerateMURCVigr(out Returns ret, SupgFinder finder)
        {
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


            string date = "01." + finder.month + "." + finder.year;
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
            //decimal progress = 0;

            var dir = "FilesExchange\\files\\";

            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            var resDir = STCLINE.KP50.Global.Constants.ExcelDir.Replace("/", "\\");
            if (!Directory.Exists(resDir)) Directory.CreateDirectory(resDir);
            var fullPath = AppDomain.CurrentDomain.BaseDirectory;

            #region Удаление файлов
            File.Delete(dir + "Жители Льготники Мурц.mdb");
            File.Delete(dir + "Жители Мурц.mdb");
            File.Delete(dir + "Счета Заселение МУРЦ.mdb");
            File.Delete(dir + "Счета МУРЦ.mdb");
            File.Delete(dir + "Счета Параметры МУРЦ.mdb");
            File.Delete(dir + fileNameIn + ".7z");
            #endregion

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

                #region Переменные для запросов
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

                DataTable resTable = new DataTable();
                string strComm = "";
                string filePath;
                string myConnectionString;
                int countStr = 0;

                string oper;
                string sql1 = "select login from web" + Points.Pref.Substring(1, 3) + tableDelimiter +
                              "users where nzp_user = " + finder.nzp_user;
                var dt = ClassDBUtils.OpenSQL(sql1, conn_web);
                if (dt.resultData.Rows.Count > 0)
                {
                    sql1 = "select comment from " + Points.Pref + "_data" + tableDelimiter +
                           "users where name = '" +
                           Convert.ToString(dt.resultData.Rows[0]["login"]) + "'";
                    dt = ClassDBUtils.OpenSQL(sql1, conn_db);
                    if (dt.resultData.Rows.Count > 0) oper = Convert.ToString(dt.resultData.Rows[0]["comment"]).Trim();
                    else oper = "do not know";
                }
                else oper = "do not know";
                //DbWorkUser db = new DbWorkUser();
                //int localUSer = db.GetLocalUser(conn_web, finder, out ret);
                #endregion


                #region Файл "Жители Льготники МУРЦ"

                //создание дубликата пустого mdb файла
                File.Copy("template/blank_table.mdb", dir + "Жители Льготники Мурц.mdb");

                filePath = dir + "Жители Льготники Мурц.mdb";

                myConnectionString = @"Provider=Microsoft.ACE.OLEDB.12.0;" +
                                         "Data Source=" + filePath + ";Jet OLEDB:Database Password=password;";
                Connection.ConnectionString = myConnectionString;
                Connection.Open();

                //Заголовок
                strComm =
                    "CREATE TABLE [Жители Льготники МУРЦ] ([Код жителя] INTEGER, [Категория льготы] TEXT(50), [Дата регистрации] DATETIME, [Дата отмены] DATETIME," +
                    " [Номер документа] TEXT(20), [Дата выдачи] DATETIME, [Место выдачи] TEXT(200), [приоритет] TEXT(20), [Оператор] TEXT(200) )";
                Command = new OleDbCommand(strComm, Connection);
                Command.ExecuteNonQuery();
                Command.Dispose();

                Connection.Close();
                #endregion

                #region Файл "Жители МУРЦ"

                //создание дубликата пустого mdb файла
                File.Copy("template/blank_table.mdb", dir + "Жители Мурц.mdb");

                filePath = dir + "Жители Мурц.mdb";

                myConnectionString = @"Provider=Microsoft.ACE.OLEDB.12.0;" +
                                         "Data Source=" + filePath + ";Jet OLEDB:Database Password=password;";
                Connection.ConnectionString = myConnectionString;
                Connection.Open();

                //Заголовок
                strComm =
                    "CREATE TABLE [Житель МУРЦ] ([Код жителя] INTEGER, [Фамилия] TEXT(50), [Имя] TEXT(50), [Отчество] TEXT(50), " +
                    "[Дата рождения] DATETIME, [Оператор] TEXT(50) )";
                Command = new OleDbCommand(strComm, Connection);
                Command.ExecuteNonQuery();
                Command.Dispose();

                foreach (var pref in Points.PointList)
                {
                    string sqlStr = " select distinct nzp_gil, fam, ima, " + sNvlWord + "(otch, '') as otch, " + sNvlWord + "(dat_rog, '01.01.1990') dat_rog " +
                                    " from " + pref.pref + "_data" + tableDelimiter + "kart";
                    resTable = ClassDBUtils.OpenSQL(sqlStr, conn_db).resultData;

                    foreach (DataRow row in resTable.Rows)
                    {
                        try
                        {
                            strComm =
                                "insert into [Житель МУРЦ] ([Код жителя], [Фамилия], [Имя], [Отчество], [Дата рождения], [Оператор]) values " +
                                " (" + row["nzp_gil"] + ",'" + row["fam"] + "','" + row["ima"] + "','" + row["otch"] +
                                "','" + row["dat_rog"] +
                                "','" + oper + "') ";
                            OleDbCommand cmd_insert = new OleDbCommand(strComm, Connection);
                            cmd_insert.ExecuteNonQuery();
                            cmd_insert.Dispose();
                        }
                        catch(Exception ex)
                        {
                            MonitorLog.WriteLog("Ошибка добавления строки в таблицу жители МУРЦ" + ex.Message, MonitorLog.typelog.Error, true);
                        }
                    }
                }

                excelRepDb.SetMyFileProgress(new ExcelUtility() { nzp_exc = nzpExc, progress = 0.1M });

                Connection.Close();
                #endregion

                #region Файл "Счета Заселение МУРЦ"

                //создание дубликата пустого mdb файла
                File.Copy("template/blank_table.mdb", dir + "Счета Заселение МУРЦ.mdb");

                filePath = dir + "Счета Заселение МУРЦ.mdb";

                myConnectionString = @"Provider=Microsoft.ACE.OLEDB.12.0;" +
                                         "Data Source=" + filePath + ";Jet OLEDB:Database Password=password;";
                Connection.ConnectionString = myConnectionString;
                Connection.Open();

                //Заголовок
                strComm =
                    "CREATE TABLE [Счета Заселение МУРЦ] ([Счет]  TEXT(7), [Код жителя] INTEGER, [Дата регистрации] DATETIME, [Дата отмены] DATETIME," +
                    " [Вид регистрации] CHAR(10), [Семья] INTEGER, [Оператор] TEXT(100) )";
                Command = new OleDbCommand(strComm, Connection);
                Command.ExecuteNonQuery();
                Command.Dispose();

                foreach (var pref1 in Points.PointList)
                {
                    string sqlStr =
                        " select k.nzp_kvar, k.nzp_gil, k.tprp, max(k.dat_ofor) as dat_ofor,  g.dat_s as dat_s," +
                        " g.dat_po as dat_po , " + sNvlWord + "(k.dat_oprp, '01.01.3000') as dat_oprp, k.dat_prop as dat_prop, -1 as family " +
                        " from " + pref1.pref + "_data" + tableDelimiter + "kart k" +
                        " left outer join " + pref1.pref + "_data" + tableDelimiter +
                        "gil_periods g on  g.nzp_gilec = k.nzp_gil and g.is_actual <> 100 and '" + date +
                        "' between  g.dat_s and g.dat_po" +
                        " where k.nzp_tkrt = 1 and k.isactual = '1' and k.dat_ofor <= '" + date + "'  and " + sNvlWord + "(k.dat_oprp, '01.06.2050') > '" + date + "'" +
                        " group by 1,2,3, 5,6,7, 8" +
                        " order by nzp_gil";

                    resTable = ClassDBUtils.OpenSQL(sqlStr, conn_db).resultData;

                    countStr = 0;

                    foreach (DataRow row1 in resTable.Rows)
                    {
                        countStr ++;
                        try
                        {
                            int type_reg = 1;
                            if (row1["tprp"].ToString() == "В") type_reg = 2;
                            if (row1["dat_s"].ToString() != "") type_reg = 3;

                            strComm =
                                "insert into [Счета Заселение МУРЦ] ([Счет], [Код жителя], [Дата регистрации], [Дата отмены], [Вид регистрации], [Семья], [Оператор]) values " +
                                " (" + row1["nzp_kvar"].ToString().PadLeft(7, '0') + "," + row1["nzp_gil"] + ",'" + row1["dat_prop"] + "','" +
                                row1["dat_oprp"] + "','" + type_reg +
                                "'," + row1["family"] + ",'" + oper + "') ";
                            OleDbCommand cmd_insert = new OleDbCommand(strComm, Connection);
                            cmd_insert.ExecuteNonQuery();
                            cmd_insert.Dispose();
                        }
                        catch (Exception ex)
                        {
                            MonitorLog.WriteLog("Ошибка добавления строки в таблицу счета заселения МУРЦ" + ex.Message,
                                MonitorLog.typelog.Error, true);
                        }
                        if (countStr%100 == 0)
                        {
                            excelRepDb.SetMyFileProgress(new ExcelUtility()
                            {
                                nzp_exc = nzpExc,
                                progress = 0.3M*countStr/resTable.Rows.Count + 0.1M
                            });
                        }
                    }
                }

                excelRepDb.SetMyFileProgress(new ExcelUtility() { nzp_exc = nzpExc, progress = 0.43M });

                Connection.Close();
                #endregion

                #region Файл "Счета МУРЦ"

                //создание дубликата пустого mdb файла
                File.Copy("template/blank_table.mdb", dir + "Счета МУРЦ.mdb");

                filePath = dir + "Счета МУРЦ.mdb";

                myConnectionString = @"Provider=Microsoft.ACE.OLEDB.12.0;" +
                                         "Data Source=" + filePath + ";Jet OLEDB:Database Password=password;";
                Connection.ConnectionString = myConnectionString;
                Connection.Open();

                //Заголовок
                strComm =
                    "CREATE TABLE [Счета МУРЦ] ([Счет]  TEXT(7), [Участок] TEXT(200), [Улица] TEXT(20), [Дом] TEXT(20), [Квартира] TEXT(20)," +
                    " [Номер] TEXT(20),  [Фамилия] TEXT(100), [Имя] TEXT(100), [Отчество] TEXT(100), [Этаж] TEXT(20) )";
                Command = new OleDbCommand(strComm, Connection);
                Command.ExecuteNonQuery();
                Command.Dispose();

                foreach (var pref2 in Points.PointList)
                {
                    string sqlStr = " select kv.nzp_kvar as nzp_kvar, kv.uch as uch, d.nzp_ul as ulica, " +
                                    " d.ndom as ndom, kv.nkvar as nkvar, kv.nzp_kvar as nkvar_n," +
                                    sNvlWord + "( substr(trim(kv.fio), 1, " + Points.Pref + "_data" + tableDelimiter + "pos(replace(trim(kv.fio),' ', ';' ),';' )-1),'') as fam," +
                                    
                                    sNvlWord + "( substr(replace((substr((trim(kv.fio)||' '), " + Points.Pref + "_data" + tableDelimiter + "pos(replace(trim(kv.fio),' ', ';' ),';' )+1, " +
                                    " length(kv.fio) - " + Points.Pref + "_data" + tableDelimiter +"pos(replace(trim(kv.fio),' ', ';' ),';' ))||';'), ' ', ';'),1, " + 
                                    Points.Pref + "_data" + tableDelimiter +"pos(replace((substr((trim(kv.fio)||' '), " +
                                    " " + Points.Pref + "_data" + tableDelimiter +"pos(replace(trim(kv.fio),' ', ';' ),';' )+1, length(kv.fio) - " + 
                                    Points.Pref + "_data" + tableDelimiter +"pos(replace(trim(kv.fio),' ', ';' ),';' ))||';'), ' ', ';'), ';')-1),'') as ima, " +
                                    
                                    sNvlWord +
                                    "(substr(replace((substr((trim(kv.fio)||' '), " + Points.Pref + "_data" + tableDelimiter +"pos(replace(trim(kv.fio),' ', ';' ),';' )+1, " +
                                    "length(kv.fio) - " + Points.Pref + "_data" + tableDelimiter +"pos(replace(trim(kv.fio),' ', ';' ),';' ))||';'), ' ', ';')," + 
                                    Points.Pref + "_data" + tableDelimiter +"pos(replace((substr((trim(kv.fio)||' '), " +
                                    "" + Points.Pref + "_data" + tableDelimiter +"pos(replace(trim(kv.fio),' ', ';' ),';' )+1, length(kv.fio) - " + 
                                    Points.Pref + "_data" + tableDelimiter +"pos(replace(trim(kv.fio),' ', ';' ),';' ))||';'), ' ', ';'), ';')+1," +
                                    "length(replace((substr((trim(kv.fio)||' '), " + Points.Pref + "_data" + tableDelimiter +"pos(replace(trim(kv.fio),' ', ';' ),';' )+1," +
                                    " length(kv.fio) - " + Points.Pref + "_data" + tableDelimiter +"pos(replace(trim(kv.fio),' ', ';' ),';' ))||';'), ' ', ';')) -" +
                                    "" + Points.Pref + "_data" + tableDelimiter +"pos(replace((substr((trim(kv.fio)||' '), " + 
                                    Points.Pref + "_data" + tableDelimiter +"pos(replace(trim(kv.fio),' ', ';' ),';' )+1, length(kv.fio) - " + 
                                    Points.Pref + "_data" + tableDelimiter +"pos(replace(trim(kv.fio),' ', ';' ),';' ))||';'), ' ', ';'), ';')-1), '')  as otch," +
                                    
                                    "  0 as etazh" +
                                    " from " +
                                    pref2.pref + "_data" + tableDelimiter + "kvar kv, " +
                                    pref2.pref + "_data" + tableDelimiter + "dom d " +
                                    " where  kv.nzp_dom = d.nzp_dom ";
                    resTable = ClassDBUtils.OpenSQL(sqlStr, conn_db).resultData;

                    foreach (DataRow row2 in resTable.Rows)
                    {
                        try
                        {
                            strComm =
                                "insert into [Счета МУРЦ] ([Счет], [Участок], [Улица], [Дом], [Квартира], [Номер],  [Фамилия], [Имя], [Отчество], [Этаж])" +
                                " values " +
                                " (" + row2["nzp_kvar"].ToString().PadLeft(7, '0') + ",'" + row2["uch"] + "','" + row2["ulica"] + "','" +
                                row2["ndom"] + "','" + row2["nkvar"] +
                                "','" + row2["nkvar_n"] + "','" + row2["fam"] + "','" + row2["ima"] + "','" +
                                row2["otch"] +
                                "','" + row2["etazh"] + "') ";
                            OleDbCommand cmd_insert = new OleDbCommand(strComm, Connection);
                            cmd_insert.ExecuteNonQuery();
                            cmd_insert.Dispose();
                        }
                        catch (Exception ex)
                        {
                            MonitorLog.WriteLog("Ошибка добавления строки в таблицу счета МУРЦ" + ex.Message,
                                MonitorLog.typelog.Error, true);
                        }
                    }
                }

                excelRepDb.SetMyFileProgress(new ExcelUtility() { nzp_exc = nzpExc, progress = 0.65M });

                Connection.Close();
                #endregion

                #region Файл "Счета Параметры МУРЦ"

                //создание дубликата пустого mdb файла
                File.Copy("template/blank_table.mdb", dir + "Счета Параметры МУРЦ.mdb");

                filePath = dir + "Счета Параметры МУРЦ.mdb";

                myConnectionString = @"Provider=Microsoft.ACE.OLEDB.12.0;" +
                                         "Data Source=" + filePath + ";Jet OLEDB:Database Password=password;";
                Connection.ConnectionString = myConnectionString;
                Connection.Open();

                //Заголовок
                strComm =
                    "CREATE TABLE [Счета Параметры МУРЦ] ([Счет] TEXT(7), [Дата регистрации] DATETIME, [Дата отмены] DATETIME, [Комнат] INTEGER, [ПЛ Общая] DOUBLE," +
                    " [ПЛ Жилая] DOUBLE, [ПЛ Отапливаемая] DOUBLE, [ЭЛ норма] DOUBLE, [ЭЛ Мощность] DOUBLE, [Приватизация] TEXT(20)," +
                    " [Коммуналка] TEXT(20), [Метод превышения] INTEGER, [Категория жилья] INTEGER, [Оператор] TEXT(200) )";
                Command = new OleDbCommand(strComm, Connection);
                Command.ExecuteNonQuery();
                Command.Dispose();

                foreach (var pref2 in Points.PointList)
                {
                    string sqlStr = " select distinct k.nzp_kvar as nzp_kvar  " +
                                    " from " + pref2.pref + "_data" + tableDelimiter + "kvar k ";
                    
                    resTable = ClassDBUtils.OpenSQL(sqlStr, conn_db).resultData;

                    foreach (DataRow row2 in resTable.Rows)
                    {
                        countStr = 0;

                        #region общая площадь

                        sql1 =
                            " select nvl(val_prm, 0) as val from " + pref2.pref + "_data" + tableDelimiter + "prm_1 " +
                            " where nzp = " + row2["nzp_kvar"] + " and nzp_prm = 4 " +
                            " and dat_s <= " + "'" + date + "' " +
                            " and dat_po > " + "'" + date + "' " +
                            " and is_actual = 1 ";
                        dt = ClassDBUtils.OpenSQL(sql1, conn_db);
                        string pl_ob;
                        if (dt.resultData.Rows.Count > 0 && dt.resultData.Rows[0]["val"] != DBNull.Value)
                            pl_ob = dt.resultData.Rows[0]["val"].ToString();
                        else pl_ob = "0";

                        #endregion

                        #region жил площадь

                        sql1 =
                            " select nvl(val_prm, 0) as val from " + pref2.pref + "_data" + tableDelimiter + "prm_1 " +
                            " where nzp = " + row2["nzp_kvar"] + " and nzp_prm = 6 " +
                            " and dat_s <= " + "'" + date + "' " +
                            " and dat_po > " + "'" + date + "' " +
                            " and is_actual = 1 ";
                        dt = ClassDBUtils.OpenSQL(sql1, conn_db);
                        string pl_z;
                        if (dt.resultData.Rows.Count > 0 && dt.resultData.Rows[0]["val"] != DBNull.Value)
                            pl_z = dt.resultData.Rows[0]["val"].ToString();
                        else pl_z = "0";

                        #endregion

                        #region отапл площадь

                        sql1 =
                            " select nvl(val_prm, 0) as val from " + pref2.pref + "_data" + tableDelimiter + "prm_1 " +
                            " where nzp = " + row2["nzp_kvar"] + " and nzp_prm = 133 " +
                            " and dat_s <= " + "'" + date + "' " +
                            " and dat_po > " + "'" + date + "' " +
                            " and is_actual = 1 ";
                        dt = ClassDBUtils.OpenSQL(sql1, conn_db);
                        string pl_ot;
                        if (dt.resultData.Rows.Count > 0 && dt.resultData.Rows[0]["val"] != DBNull.Value)
                            pl_ot = dt.resultData.Rows[0]["val"].ToString();
                        else pl_ot = "0";

                        #endregion

                        #region приватизировано

                        sql1 =
                            " select nvl(val_prm, 0) as val from " + pref2.pref + "_data" + tableDelimiter + "prm_1 " +
                            " where nzp = " + row2["nzp_kvar"] + " and nzp_prm = 8 " +
                            " and dat_s <= " + "'" + date + "' " +
                            " and dat_po > " + "'" + date + "' " +
                            " and is_actual = 1 ";
                        dt = ClassDBUtils.OpenSQL(sql1, conn_db);
                        string pr;
                        if (dt.resultData.Rows.Count > 0 && dt.resultData.Rows[0]["val"] != DBNull.Value)
                            pr = dt.resultData.Rows[0]["val"].ToString();
                        else pr = "0";
                        if (pr.Trim() == "нет") pr = "0";
                        if (pr.Trim() == "да") pr = "1";
                        if (pr.Trim() == "1") pr = "-1";

                        #endregion

                        #region количество комнат

                        sql1 =
                            " select nvl(val_prm, 0) as val from " + pref2.pref + "_data" + tableDelimiter + "prm_1 " +
                            " where nzp = " + row2["nzp_kvar"] + " and nzp_prm = 107 " +
                            " and dat_s <= " + "'" + date + "' " +
                            " and dat_po > " + "'" + date + "' " +
                            " and is_actual = 1 ";
                        dt = ClassDBUtils.OpenSQL(sql1, conn_db);
                        string kol;
                        if (dt.resultData.Rows.Count > 0 && dt.resultData.Rows[0]["val"] != DBNull.Value)
                            kol = dt.resultData.Rows[0]["val"].ToString();
                        else kol = "0";

                        #endregion

                        #region коммуналка

                        sql1 =
                            " select nvl(val_prm, 0) as val from " + pref2.pref + "_data" + tableDelimiter + "prm_1 " +
                            " where nzp = " + row2["nzp_kvar"] + " and nzp_prm = 3 " +
                            " and dat_s <= " + "'" + date + "' " +
                            " and dat_po > " + "'" + date + "' " +
                            " and is_actual = 1 ";
                        dt = ClassDBUtils.OpenSQL(sql1, conn_db);
                        string kom;
                        if (dt.resultData.Rows.Count > 0 && dt.resultData.Rows[0]["val"] != DBNull.Value)
                        {
                            if (dt.resultData.Rows[0]["val"].ToString().Trim() == "2") kom = "1";
                            else kom = "0";
                        }
                        else kom = "0";

                        #endregion

                        #region оператор

                        string oper_sp = "";
                        //if (row2["user_del"].ToString().Length != 0)
                        //{
                        //    sql1 = "select trim(comment) from " + Points.Pref + "_data" + tableDelimiter +
                        //           " users where nzp_user = " + row2["user_del"].ToString();
                        //    dt = ClassDBUtils.OpenSQL(sql1, conn_db);
                        //    if (dt.GetData().Rows.Count == 1) oper_sp = dt.GetData().Rows[0]["comment"].ToString();
                        //}
                        //else if (row2["nzp_user"].ToString().Length != 0)
                        //{
                        //    sql1 = "select trim(comment) from " + Points.Pref + "_data" + tableDelimiter +
                        //           " users where nzp_user = " + row2["nzp_user"].ToString();
                        //    dt = ClassDBUtils.OpenSQL(sql1, conn_db);
                        //    if (dt.GetData().Rows.Count == 1) oper_sp = dt.GetData().Rows[0]["comment"].ToString();
                        //}

                        #endregion

                        #region Дата регистрации


                        string dat_reg; 
                        string dat_otm;
                        sql1 =
                            " select dat_s as val, dat_po as val2 from " + pref2.pref + "_data" + tableDelimiter + "prm_3 " +
                            " where nzp = " + row2["nzp_kvar"] + " and nzp_prm = 51 " +
                            " and dat_s <= " + "'" + date + "' " +
                            " and dat_po > " + "'" + date + "' " +
                            " and is_actual = 1 and val_prm = 1 ";
                        dt = ClassDBUtils.OpenSQL(sql1, conn_db);
                        if (dt.resultData.Rows.Count > 0 && dt.resultData.Rows[0]["val"] != DBNull.Value)
                        {
                            dat_reg = dt.resultData.Rows[0]["val"].ToString().Substring(0, 10);
                            dat_otm = dt.resultData.Rows[0]["val2"].ToString().Substring(0, 10);
                        }
                        else
                        {
                            dat_reg = "01.01.1900";
                            dat_otm = "01.01.3000";
                        }

                        #endregion

                        #region Дата отмены
                        sql1 =
                            " select dat_s as val from " + pref2.pref + "_data" + tableDelimiter + "prm_3 " +
                            " where nzp = " + row2["nzp_kvar"] + " and nzp_prm = 51 " +
                            " and dat_s <= " + "'" + date + "' " +
                            " and dat_po > " + "'" + date + "' " +
                            " and is_actual = 1 and val_prm = 3 ";
                        dt = ClassDBUtils.OpenSQL(sql1, conn_db);
                        if (dt.resultData.Rows.Count > 0 && dt.resultData.Rows[0]["val"] != DBNull.Value)
                            dat_otm = dt.resultData.Rows[0]["val"].ToString().Substring(0, 10);

                        #endregion


                        try
                        {
                            strComm =
                                "insert into [Счета Параметры МУРЦ] ([Счет], [Дата регистрации], [Дата отмены], [Комнат], [ПЛ Общая]," +
                                " [ПЛ Жилая], [ПЛ Отапливаемая], [ЭЛ норма], [ЭЛ Мощность], [Приватизация]," +
                                " [Коммуналка], [Метод превышения], [Категория жилья], [Оператор])" +
                                " values " +
                                " (" + row2["nzp_kvar"].ToString().PadLeft(7, '0') + ",'" + dat_reg + "','" + dat_otm + "'," +
                                kol + "," + pl_ob +
                                "," + pl_z + "," + pl_ot + ", 0, 0,'" + pr + "','" + kom + "', 0, 0, '" + oper_sp +
                                "' ) ";
                            OleDbCommand cmd_insert = new OleDbCommand(strComm, Connection);
                            cmd_insert.ExecuteNonQuery();
                            cmd_insert.Dispose();
                        }
                        catch (Exception ex)
                        {
                            MonitorLog.WriteLog("Ошибка добавления строки в таблицу счета параметры МУРЦ" + ex.Message,
                                MonitorLog.typelog.Error, true);
                        }
                        if (countStr%100 == 0)
                        {
                            excelRepDb.SetMyFileProgress(new ExcelUtility()
                            {
                                nzp_exc = nzpExc,
                                progress = 0.2M*countStr/resTable.Rows.Count + 0.65M
                            });
                        }
                    }
                }

                excelRepDb.SetMyFileProgress(new ExcelUtility() { nzp_exc = nzpExc, progress = 0.85M });

                Connection.Close();
                #endregion


                #region создание и заполнение mdb

                //if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                ////создание дубликата пустого mdb файла
                //File.Copy("template/blank_table.mdb", dir + "Оплата Мурц.mdb");

                //var myConnectionString = @"Provider=Microsoft.ACE.OLEDB.12.0;" +
                //                         "Data Source=" + dir + "Оплата МУРЦ.mdb;Jet OLEDB:Database Password=password;";
                //Connection.ConnectionString = myConnectionString;
                //Connection.Open();

                //DataTable resTable = new DataTable();
                //string strComm = "";

                ////Заголовок
                //strComm =
                //    "CREATE TABLE [Оплата МУРЦ] ([Дата расчета] DATETIME,[Ведомость] TEXT(20), [Номер оплаты] DOUBLE, [Дата оплаты] DATETIME, [Вид] INTEGER, [Участок] INTEGER, " +
                //    " [Улица] INTEGER, [Дом] TEXT(50), [Квартира] INTEGER, [Номер] INTEGER, [Сумма1] DOUBLE, [Сумма2] DOUBLE, [Сумма3] DOUBLE, [Сумма4] DOUBLE, " +
                //    " [Сумма5] DOUBLE, [Сумма6] DOUBLE, [Сумма7] DOUBLE, [Оператор] TEXT(100), [Дата ввода] DATETIME )";
                //Command = new OleDbCommand(strComm, Connection);
                //Command.ExecuteNonQuery();
                //Command.Dispose();


                //string sqlStr = " select pl.dat_month as dat_month, pl.nzp_pack as nzp_pack, pl.nzp_pack_ls as nzp_pack_ls, pl.dat_vvod as dat_vvod, pl.g_sum_ls as g_sum_ls, pl.num_ls as num_ls, p.num_pack as num_pack from "
                //                + Points.Pref + "_fin_" + finder.year.Substring(2, 2) + tableDelimiter + "pack_ls pl, " +
                //                Points.Pref + "_fin_" + finder.year.Substring(2, 2) + tableDelimiter +
                //                "pack p where pl.nzp_pack = p.nzp_pack";
                //resTable = ClassDBUtils.OpenSQL(sqlStr, conn_db).resultData;

                //foreach (DataRow row in resTable.Rows)
                //{
                //    #region получение значений из БД

                //    //Получаем номер квартиры

                //    string sql1 = "select nkvar from " + Points.Pref + "_data" + tableDelimiter +
                //                  "file_kvar where id = " + row["num_ls"];
                //    var dt = ClassDBUtils.OpenSQL(sql1, conn_db);
                //    int nkvar;
                //    if (dt.resultData.Rows.Count > 0)
                //        nkvar = Convert.ToInt32(dt.resultData.Rows[0]["nkvar"].ToString().Trim());
                //    else nkvar = 0;
                //    //получаем сегодняшнюю дату и время
                //    DateTime thisDay = DateTime.Now;
                //    //получаем дом
                //    sql1 = "select a.ulica, a.ndom from " + Points.Pref + "_data" + tableDelimiter + "file_dom a, " +
                //           Points.Pref + "_data" + tableDelimiter + "file_kvar b " +
                //           "where a.id = b.dom_id and b.id =" + row["num_ls"];
                //    dt = ClassDBUtils.OpenSQL(sql1, conn_db);
                //    string dom;
                //    if (dt.resultData.Rows.Count > 0)
                //        dom = Convert.ToString(dt.resultData.Rows[0]["ndom"].ToString().Trim());
                //    else dom = "0";
                //    //получаем улицу
                //    string ulica_str;
                //    int ulica;
                //    if (dt.resultData.Rows.Count > 0)
                //    {
                //        ulica_str = Convert.ToString(dt.resultData.Rows[0]["ulica"].ToString().Trim());
                //        string sql2 = "select file_ulica_id from " + Points.Pref + "_data" + tableDelimiter +
                //                      "file_ulica where File_ulica_street = '" + ulica_str + "'";
                //        dt = ClassDBUtils.OpenSQL(sql2, conn_db);
                //        if (dt.resultData.Rows.Count > 0)
                //            ulica = Convert.ToInt32(dt.resultData.Rows[0]["file_ulica_id"]);
                //        else ulica = 0;
                //    }
                //    else ulica = 0;
                //    //вид
                //    int vid = 3;
                //    //получаем номер участка
                //    sql1 = "select uch from " + Points.Pref + "_data" + tableDelimiter + "kvar k, " + Points.Pref +
                //           "_data" + tableDelimiter + "dom d where k.nkvar = '" + nkvar +
                //           "' and k.nzp_dom = d.nzp_dom and d.ndom = '"
                //           + dom + "' and k.num_ls =" + row["num_ls"];
                //    dt = ClassDBUtils.OpenSQL(sql1, conn_db);
                //    int uch;
                //    if (dt.resultData.Rows.Count > 0 && dt.resultData.Rows[0]["uch"] != DBNull.Value)
                //        uch = Convert.ToInt32(dt.resultData.Rows[0]["uch"]);
                //    else uch = 0;
                //    //номер
                //    int num;
                //    num = 1;
                //    //получаем оператора
                //    string oper;
                //    sql1 = "select login from web" + Points.Pref.Substring(1, 3) + tableDelimiter +
                //           "users where nzp_user = " + finder.nzp_user;
                //    dt = ClassDBUtils.OpenSQL(sql1, conn_web);
                //    if (dt.resultData.Rows.Count > 0)
                //    {
                //        sql1 = "select comment from " + Points.Pref + "_data" + tableDelimiter + "users where name = '" +
                //               Convert.ToString(dt.resultData.Rows[0]["login"]) + "'";
                //        dt = ClassDBUtils.OpenSQL(sql1, conn_db);
                //        if (dt.resultData.Rows.Count > 0) oper = Convert.ToString(dt.resultData.Rows[0]["comment"]);
                //        else oper = "do not know";
                //    }
                //    else oper = "do not know";
                //    //DbWorkUser db = new DbWorkUser();
                //    //int localUSer = db.GetLocalUser(conn_web, finder, out ret);


                //    #endregion

                //    strComm =
                //        "insert into [Оплата МУРЦ] ([Дата расчета], [Ведомость],[Номер оплаты], [Дата оплаты],[Вид],[Участок],[Улица]," +
                //        " [Дом],[Квартира],[Номер],[Сумма1], [Сумма2],[Сумма3],[Сумма4],[Сумма5],[Сумма6],[Сумма7],[Оператор],[Дата ввода]) values " +
                //        " ('" + row["dat_month"] + "','" + row["num_pack"] + "'," + row["nzp_pack_ls"] + ",'" +
                //        row["dat_vvod"] +
                //        "'," + vid + "," + uch + "," + ulica + ",'" + dom + "'," + nkvar + "," + num + ", 0 ," +
                //        row["g_sum_ls"] + ", 0 , 0 , 0 , 0 , 0 ,'" + oper + "','" + thisDay + "') ";
                //    OleDbCommand cmd_insert = new OleDbCommand(strComm, Connection);
                //    cmd_insert.ExecuteNonQuery();
                //    cmd_insert.Dispose();
                //}
                //progress += 20 / Points.PointList.Count;
                //excelRepDb.SetMyFileProgress(new ExcelUtility() { nzp_exc = nzpExc, progress = progress / 100 });

                #endregion

                conn_web.Close();

                #region архивация

                SevenZipCompressor file = new SevenZipCompressor();
                file.EncryptHeaders = true;
                file.CompressionMethod = SevenZip.CompressionMethod.BZip2;
                file.DefaultItemName = fileNameIn;
                file.CompressionLevel = SevenZip.CompressionLevel.Normal;

                file.CompressFiles(fullPath + dir + fileNameIn + ".7z", fullPath + dir + "Жители Льготники Мурц.mdb", fullPath + dir + "Жители Мурц.mdb",
                    fullPath + dir + "Счета Заселение МУРЦ.mdb", fullPath + dir + "Счета МУРЦ.mdb", fullPath + dir + "Счета Параметры МУРЦ.mdb");
                //file.CompressFiles(fullPath + dir + fileNameIn + ".7z", fullPath + dir + "Жители Льготники Мурц.mdb");

                #endregion

                //перенос файла на клиент
                File.Copy(dir + fileNameIn + ".7z", resDir + fileNameIn + ".7z");
                if (InputOutput.useFtp) fn2 = InputOutput.SaveInputFile(fullPath + dir + fileNameIn + ".7z");


                excelRepDb.SetMyFileProgress(new ExcelUtility() { nzp_exc = nzpExc, progress = 0.94M });
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

                #region Удаление файла
                //File.Delete(dir + "Оплата Мурц.mdb");
                //File.Delete(dir + fileNameIn + ".7z");
                File.Delete(dir + "Счета Параметры МУРЦ.mdb");
                File.Delete(dir + "Счета МУРЦ.mdb");
                File.Delete(dir + "Счета Заселение МУРЦ.mdb");
                File.Delete(dir + "Жители Льготники Мурц.mdb");
                File.Delete(dir + "Жители Мурц.mdb");
                File.Delete(dir + fileNameIn + ".7z");
                #endregion

                if (ret.result)
                {

                    excelRepDb.SetMyFileProgress(new ExcelUtility() {nzp_exc = nzpExc, progress = 1});
                    excelRepDb.SetMyFileState(new ExcelUtility()
                    {
                        nzp_exc = nzpExc,
                        status = ExcelUtility.Statuses.Success,
                        exc_path = InputOutput.useFtp ? fn2 : path + fileNameIn + ".7z"
                    });
                }
                else
                {
                    excelRepDb.SetMyFileState(new ExcelUtility()
                    {
                        nzp_exc = nzpExc,
                        status = ExcelUtility.Statuses.Failed
                    });
                }
                excelRepDb.Close();

                if (reader != null) reader.Close();
                if (conn_db != null) conn_db.Close();
            }

            ret.text = "Файл успешно загружен";
            return ret;
        }


        /// <summary>
        /// Получение num_ls по внешнему адресу
        /// </summary>
        /// <param name="obj">
        /// nzp_ul - внешний код улицы 
        /// ndom - номер дома
        /// nkvar - номер квартиры
        /// </param>
        /// <returns>
        ///nzp_user =  
        /// </returns>
        private static Ls GetLsByExtAddress(FullAddress obj, IDbConnection con_db)
        {
            decimal ls;
            var res = new Ls();
            bool done = false;
            try
            {
#if PG
                string sql = "select a.nzp_kvar as id, a.pref as pref from " + Points.Pref + "_data.kvar a, " +
                             Points.Pref + "_data.s_ulica ul, " + Points.Pref + "_data.dom d, " + Points.Pref +
                             "_data.file_ulica as ful where " +
                             " a.nkvar = '" + obj.nkvar + "' and a.nzp_dom = d.nzp_dom and d.ndom ='" + obj.ndom +
                             "' and ful.nzp_ul = ul.nzp_ul and ful.file_ulica_id = '" + obj.ulica +
                             "' and ul.nzp_ul = d.nzp_ul";
#else
                string sql = "select a.nzp_kvar as id, a.pref as pref from " + Points.Pref + "_data:kvar a, " + Points.Pref + "_data:s_ulica ul, " + Points.Pref + "_data:dom d, " + Points.Pref + "_data:file_ulica as ful where " +
                                    " a.nkvar = '" + obj.nkvar + "' and a.nzp_dom = d.nzp_dom and d.ndom ='" + obj.ndom + "' and ful.nzp_ul = ul.nzp_ul and ful.file_ulica_id = " + obj.ulica + " and ul.nzp_ul = d.nzp_ul";
#endif
                //sql = "select a.id as id from " + Points.Pref + "_data"+tableDelimiter+"file_kvar a, " + Points.Pref + "_data"+tableDelimiter+"file_dom b  where a.nkvar = " + obj.nkvar + " and a.dom_id = b.id and b.local_id ='" + obj.ndom +
                //    "' and b.ulica ='" + ulica + "'";
                var dt = ClassDBUtils.OpenSQL(sql, con_db);
                if (dt.resultData.Rows.Count > 0)
                {
                    ls = Convert.ToDecimal(dt.resultData.Rows[0]["id"]);
                    res.num_ls = Convert.ToInt32(ls);
                    res.pref = ls.ToString();
                    done = true;
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка функции GetLsByExtAddress" + ex.Message, MonitorLog.typelog.Error, true);
            }

            if (!done)
            {
                res.num_ls = 0;
                res.pref = "";
            }

            return res;
        }

    }
}