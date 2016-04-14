using System;
using System.Collections.Generic;
using System.Data;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;
using System.Text;
using System.IO;
using SevenZip;
using System.Data.Odbc;
using System.Data.OleDb;
using JCS;
using STCLINE.KP50.Utility;
using Bars.KP50.Utils;

namespace STCLINE.KP50.DataBase
{
    public partial class DbAdmin : DbAdminClient
    {

#if PG
        private readonly string pgDefaultDb = "public";
#else
#endif
    

      
        /// <summary>
        /// Загрузка файла "Счета начисления УЭС"
        /// </summary>
        /// <param name="finder">файл</param>
        /// <returns>результат</returns>
        public Returns UploadUESCharge(FilesImported finder)
        {
            Returns ret = Utils.InitReturns();

            #region Проверка наличия таблиц
            IDbConnection con_db = GetConnection(Constants.cons_Kernel);

            try
            {
                var t = OpenDb(con_db, true);

                if (!t.result)
                {
                    MonitorLog.WriteLog("Ошибка открытия соединения", MonitorLog.typelog.Error, true);
                    ret.result = false;
                    ret.text = " Файл не загружен, отсутствует подключение к базе данных ";
                    ret.tag = -1;
                    return ret;
                }

                foreach (var bank in Points.PointList)
                {
                    string sql = "select * from " + bank.pref + "_charge_" + finder.year.Substring(2, 2) + tableDelimiter + "charge_" + finder.month + " where nzp_kvar = -1";
                    var dt = ClassDBUtils.OpenSQL(sql, con_db);
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteException("Ошибка функции UploadUESCharge ", ex);
                ret.result = false;
                ret.text = "Для выбранной даты невозможна загрузка";
                ret.tag = -1;
                return ret;
            }
            finally
            {
                con_db.Close();
            }
            #endregion

            //директория файла
            string fDirectory = Constants.Directories.ImportDir.Replace("/", "\\");
            // fDirectory = "D:\\work.php\\KOMPLAT.50\\WEB\\WebKomplat5\\ExcelReport\\import\\";

            //имя файла
            string fileName = Path.Combine(fDirectory, finder.saved_name);

            //fileName = "D:\\TestFiles\\13.txt";
            if (InputOutput.useFtp) InputOutput.DownloadFile(finder.saved_name, fileName);

            List<string> sqlStr = new List<string>();
            StringBuilder err = new StringBuilder();

            //версия файла
            int nzp_version = -1;
            FileInfo[] files = new FileInfo[1];
            #region Разархивация файла

            using (SevenZipExtractor extractor = new SevenZipExtractor(fileName))
            {
                //создание папки с тем же именем
                DirectoryInfo exDirectorey = Directory.CreateDirectory(Path.Combine(fDirectory, finder.saved_name.Substring(0, finder.saved_name.LastIndexOf('.'))));
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


            #region Вставка файла
            con_db = GetConnection(Constants.cons_Kernel);

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

                DbWorkUser db = new DbWorkUser();
                int localUSer = db.GetLocalUser(con_db, finder, out ret);
                if (!ret.result)
                {
                    MonitorLog.WriteLog("Ошибка определения локальног пользователя", MonitorLog.typelog.Error, true);
                    ret.result = false;
                    ret.text = " Ошибка определения локального пользователя ";
                    ret.tag = -1;
                    return ret;
                }

#if PG
                string sql = " INSERT INTO " + Points.Pref + "_data.  files_imported (nzp_file, nzp_version, loaded_name, saved_name, nzp_status, created_by, created_on, file_type) ";
                sql += " VALUES (default," + nzp_version + ",'" + finder.loaded_name + "',\'" + finder.saved_name + "\',2," + localUSer + ",now(), 2)  ";
#else
                string sql = " INSERT INTO " + Points.Pref + "_data" + tableDelimiter + "  files_imported (nzp_file, nzp_version, loaded_name, saved_name, nzp_status, created_by, created_on, file_type) ";
                sql += " VALUES (0," + nzp_version + ",'" + finder.loaded_name + "',\'" + finder.saved_name + "\',2," + localUSer + ",current, 2)  ";
#endif

                ret = ExecSQL(con_db, sql, true);
                if (!ret.result)
                {
                    MonitorLog.WriteLog("Ошибка добавления файла в в таблицу файла " + fileName, MonitorLog.typelog.Error, true);
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
                MonitorLog.WriteLog("Ошибка выполнения процедуры LoadHarGilFondGKU : " + ex.Message, MonitorLog.typelog.Error, true);
                ret.result = false;
                return ret;
            }
            finally
            {
                con_db.Close();
            }
            #endregion

            #region чистка услуг, найденных в файле
            foreach (var mdbFile in files)
            {
                #region Счета Корректировки УЭС
                if (mdbFile.Name == "Счета Корректировки УЭС.mdb")
                {
                    var mdbBase = mdbFile.Name.Split('.')[0];
                    #region Считываем файл
                    fileName = mdbFile.FullName;

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
                    con_db = GetConnection(Constants.cons_Kernel);

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

                        foreach (var bank in Points.PointList)
                        {
                            //очищаем XXX_charge_mm
                            string sqlComm1 = "delete from " + bank.pref + "_charge_" + finder.year.Substring(2, 2) + tableDelimiter + "perekidka where nzp_serv in (8,9) and month_ = " + finder.month +
                                " and nzp_supp = 6 ";
                            ExecSQL(con_db, sqlComm1, true);

                            ////Удаляем исходящее сальдо предыдущего месяца                            
                            //DateTime prevMonth;
                            //if (finder.month != "01")
                            //{
                            //    prevMonth = new DateTime(Convert.ToInt32(finder.year), Convert.ToInt32(finder.month) - 1, 1);
                            //}
                            //else
                            //{
                            //    prevMonth = new DateTime(Convert.ToInt32(finder.year) - 1 , 12, 1);
                            //}
                            //sqlComm1 = "update " + bank.pref + "_charge_" + (prevMonth.Year - 2000) + tableDelimiter + "charge_" + prevMonth.Month.ToString().PadLeft(2, '0') +
                            //    " set sum_outsaldo = 0 where nzp_serv in (8,9)";
                            //ExecSQL(con_db, sqlComm1, true);

                        }

                        foreach (DataRow row in tbl.Rows)
                        {
                            var nzp_serv = 0;
                            //var nzp_frm = 0;
                            var nzp_prm = 0;
                            switch (row[1].ToString())
                            {
                                case "2":
                                    {
                                        nzp_serv = 8;
                                        //nzp_frm = -2;
                                        nzp_prm = 100010;
                                    }
                                    break;
                                case "3":
                                    {
                                        nzp_serv = 9;
                                        //nzp_frm = -3;
                                        nzp_prm = 100020;
                                    }
                                    break;
                            }
                            foreach (var bank in Points.PointList)
                            {

                                var sqlComm = "delete from " + bank.pref + "_charge_" + finder.year.Substring(2, 2) + tableDelimiter + "charge_" + finder.month + " where nzp_serv = " + nzp_serv + " and nzp_kvar = " + row[0];
                                ExecSQL(con_db, sqlComm, true);
                                sqlComm = "update " + bank.pref + "_data" + tableDelimiter + "tarif set is_actual = 100 where nzp_serv = " + nzp_serv + " and num_ls = " + row[0] + " and dat_s <= '01." + finder.month + "." + finder.year + "' and dat_po >= '01." + finder.month + "." + finder.year + "'";
                                ExecSQL(con_db, sqlComm, true);
#if PG
                                sqlComm = "update " + bank.pref + "_data" + tableDelimiter + "prm_1 set dat_when =current_date , dat_po = cast('01." + finder.month + "." + finder.year + "' as date )-Interval '1 day' where nzp_prm = " + nzp_prm + " and nzp = " + row[0] + " and dat_s < '01." + finder.month + "." + finder.year + "' and dat_po >= '01." + finder.month + "." + finder.year + "'";
                                ExecSQL(con_db, sqlComm, true);
                                sqlComm = "update " + bank.pref + "_data" + tableDelimiter + "prm_1 set is_actual=100  , dat_when =current_date where nzp_prm = " + nzp_prm + " and nzp = " + row[0] + " and dat_s = '01." + finder.month + "." + finder.year + "' and dat_po >= '01." + finder.month + "." + finder.year + "'";
                                ExecSQL(con_db, sqlComm, true);
                                sqlComm = "update " + bank.pref + "_data" + tableDelimiter + "prm_1 set is_actual=100  , dat_when =current_date where nzp_prm = " + nzp_prm.ToString().Substring(0, nzp_prm.ToString().Length - 1) + " and nzp = " + row[0] + " and dat_s = '01." + finder.month + "." + finder.year + "' and dat_po >= '01." + finder.month + "." + finder.year + "' and val_prm = '0'";
#else
                                sqlComm = "update " + bank.pref + "_data" + tableDelimiter + "prm_1 set dat_when =today , dat_po = cast('01." + finder.month + "." + finder.year + "' as date )-1 units day where nzp_prm = " + nzp_prm + " and nzp = " + row[0] + " and dat_s < '01." + finder.month + "." + finder.year + "' and dat_po >= '01." + finder.month + "." + finder.year + "'";
                                ExecSQL(con_db, sqlComm, true);
                                sqlComm = "update " + bank.pref + "_data" + tableDelimiter + "prm_1 set is_actual=100  , dat_when =today where nzp_prm = " + nzp_prm + " and nzp = " + row[0] + " and dat_s = '01." + finder.month + "." + finder.year + "' and dat_po >= '01." + finder.month + "." + finder.year + "'";
                                ExecSQL(con_db, sqlComm, true);
                                sqlComm = "update " + bank.pref + "_data" + tableDelimiter + "prm_1 set is_actual=100  , dat_when =today where nzp_prm = " + nzp_prm.ToString().Substring(0, nzp_prm.ToString().Length - 1) + " and nzp = " + row[0] + " and dat_s = '01." + finder.month + "." + finder.year + "' and dat_po >= '01." + finder.month + "." + finder.year + "' and val_prm = 0";
#endif
                                ExecSQL(con_db, sqlComm, true);



                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MonitorLog.WriteLog("Ошибка выполнения процедуры UploadUESCharge : " + ex.Message, MonitorLog.typelog.Error, true);
                        ret.result = false;
                        return ret;
                    }
                    finally
                    {
                        con_db.Close();
                    }
                    #endregion
                }
                #endregion

                #region Счета Начисления УЭС
                if (mdbFile.Name == "Счета Начисления УЭС.mdb")
                {
                    var mdbBase = mdbFile.Name.Split('.')[0];
                    #region Считываем файл
                    fileName = mdbFile.FullName;

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
                    con_db = GetConnection(Constants.cons_Kernel);
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
                        foreach (DataRow row in tbl.Rows)
                        {
                            var nzp_serv = 0;
                            //var nzp_frm = 0;
                            var nzp_prm = 0;
                            switch (row[1].ToString())
                            {
                                case "2":
                                    {
                                        nzp_serv = 8;
                                        //nzp_frm = -2;
                                        nzp_prm = 100010;
                                    }
                                    break;
                                case "3":
                                    {
                                        nzp_serv = 9;
                                        //nzp_frm = -3;
                                        nzp_prm = 100020;
                                    }
                                    break;
                            }
                            foreach (var bank in Points.PointList)
                            {

#if PG
                                var sqlComm = "delete from " + bank.pref + "_charge_" + finder.year.Substring(2, 2) + tableDelimiter + " charge_" + finder.month + " where nzp_serv = " + nzp_serv + " and nzp_kvar = " + row[0];
                                ExecSQL(con_db, sqlComm, true);
                                sqlComm = "update " + bank.pref + "_data" + tableDelimiter + "tarif set is_actual = 100 where nzp_serv = " + nzp_serv + " and num_ls = " + row[0] + " and dat_s <= '01." + finder.month + "." + finder.year + "' and dat_po >= '01." + finder.month + "." + finder.year + "'";
                                ExecSQL(con_db, sqlComm, true);
                                sqlComm = "update " + bank.pref + "_data" + tableDelimiter + "prm_1 set dat_when =current_date , dat_po = cast('01." + finder.month + "." + finder.year + "' as date )- Interval '1 day' where nzp_prm = " + nzp_prm + " and nzp = " + row[0] + " and dat_s < '01." + finder.month + "." + finder.year + "' and dat_po >= '01." + finder.month + "." + finder.year + "'";
                                ExecSQL(con_db, sqlComm, true);
                                sqlComm = "update " + bank.pref + "_data" + tableDelimiter + "prm_1 set is_actual=100  , dat_when =current_date where nzp_prm = " + nzp_prm + " and nzp = " + row[0] + " and dat_s = '01." + finder.month + "." + finder.year + "' and dat_po >= '01." + finder.month + "." + finder.year + "'";
                                ExecSQL(con_db, sqlComm, true);
                                sqlComm = "update " + bank.pref + "_data" + tableDelimiter + "prm_1 set is_actual=100  , dat_when =current_date where nzp_prm = " + nzp_prm.ToString().Substring(0, nzp_prm.ToString().Length - 1) + " and nzp = " + row[0] + " and dat_s = '01." + finder.month + "." + finder.year + "' and dat_po >= '01." + finder.month + "." + finder.year + "' and val_prm = '0'";
                                ExecSQL(con_db, sqlComm, true);
#else
                                var sqlComm = "delete from " + bank.pref + "_charge_" + finder.year.Substring(2, 2) + tableDelimiter + " charge_" + finder.month + " where nzp_serv = " + nzp_serv + " and nzp_kvar = " + row[0];
                                ExecSQL(con_db, sqlComm, true);
                                sqlComm = "update " + bank.pref + "_data" + tableDelimiter + "tarif set is_actual = 100 where nzp_serv = " + nzp_serv + " and num_ls = " + row[0] + " and dat_s <= '01." + finder.month + "." + finder.year + "' and dat_po >= '01." + finder.month + "." + finder.year + "'";
                                ExecSQL(con_db, sqlComm, true);
                                sqlComm = "update " + bank.pref + "_data" + tableDelimiter + "prm_1 set dat_when =today , dat_po = cast('01." + finder.month + "." + finder.year + "' as date )-1 units day where nzp_prm = " + nzp_prm + " and nzp = " + row[0] + " and dat_s < '01." + finder.month + "." + finder.year + "' and dat_po >= '01." + finder.month + "." + finder.year + "'";
                                ExecSQL(con_db, sqlComm, true);
                                sqlComm = "update " + bank.pref + "_data" + tableDelimiter + "prm_1 set is_actual=100  , dat_when =today where nzp_prm = " + nzp_prm + " and nzp = " + row[0] + " and dat_s = '01." + finder.month + "." + finder.year + "' and dat_po >= '01." + finder.month + "." + finder.year + "'";
                                ExecSQL(con_db, sqlComm, true);
                                sqlComm = "update " + bank.pref + "_data" + tableDelimiter + "prm_1 set is_actual=100  , dat_when =today where nzp_prm = " + nzp_prm.ToString().Substring(0, nzp_prm.ToString().Length - 1) + " and nzp = " + row[0] + " and dat_s = '01." + finder.month + "." + finder.year + "' and dat_po >= '01." + finder.month + "." + finder.year + "' and val_prm = 0";
                                ExecSQL(con_db, sqlComm, true);
#endif

                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MonitorLog.WriteLog("Ошибка выполнения процедуры UploadUESCharge : " + ex.Message, MonitorLog.typelog.Error, true);
                        ret.result = false;
                        return ret;
                    }
                    finally
                    {
                        con_db.Close();
                    }
                    #endregion
                }
                #endregion

                #region Счета Превышения УЭС
                if (mdbFile.Name == "Счета Превышения УЭС.mdb")
                {
                    var mdbBase = mdbFile.Name.Split('.')[0];
                    #region Считываем файл
                    fileName = mdbFile.FullName;

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
                    con_db = GetConnection(Constants.cons_Kernel);
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



                        foreach (DataRow row in tbl.Rows)
                        {
                            var nzp_serv = 0;
                            //var nzp_frm = 0;
                            var nzp_prm = 0;
                            switch (row[1].ToString())
                            {
                                case "2":
                                    {
                                        nzp_serv = 8;
                                        //nzp_frm = -2;
                                        nzp_prm = 100010;
                                    }
                                    break;
                                case "3":
                                    {
                                        nzp_serv = 9;
                                        //nzp_frm = -3;
                                        nzp_prm = 100020;
                                    }
                                    break;
                            }
                            foreach (var bank in Points.PointList)
                            {

#if PG
                                var sqlComm = "delete from " + bank.pref + "_charge_" + finder.year.Substring(2, 2) + tableDelimiter + "charge_" + finder.month + " where nzp_serv = " + nzp_serv + " and nzp_kvar = " + row[0];
                                ExecSQL(con_db, sqlComm, true);
                                sqlComm = "update " + bank.pref + "_data" + tableDelimiter + "tarif set is_actual = 100 where nzp_serv = " + nzp_serv + " and num_ls = " + row[0] + " and dat_s <= '01." + finder.month + "." + finder.year + "' and dat_po >= '01." + finder.month + "." + finder.year + "'";
                                ExecSQL(con_db, sqlComm, true);
                                sqlComm = "update " + bank.pref + "_data" + tableDelimiter + "prm_1 set dat_when =current_date , dat_po = cast('01." + finder.month + "." + finder.year + "' as date )-Interval '1 day' where nzp_prm = " + nzp_prm + " and nzp = " + row[0] + " and dat_s < '01." + finder.month + "." + finder.year + "' and dat_po >= '01." + finder.month + "." + finder.year + "'";
                                ExecSQL(con_db, sqlComm, true);
                                sqlComm = "update " + bank.pref + "_data" + tableDelimiter + "prm_1 set is_actual=100  , dat_when =current_date where nzp_prm = " + nzp_prm + " and nzp = " + row[0] + " and dat_s = '01." + finder.month + "." + finder.year + "' and dat_po >= '01." + finder.month + "." + finder.year + "'";
                                ExecSQL(con_db, sqlComm, true);
                                sqlComm = "update " + bank.pref + "_data" + tableDelimiter + "prm_1 set is_actual=100  , dat_when =current_date where nzp_prm = " + nzp_prm.ToString().Substring(0, nzp_prm.ToString().Length - 1) + " and nzp = " + row[0] + " and dat_s = '01." + finder.month + "." + finder.year + "' and dat_po >= '01." + finder.month + "." + finder.year + "' and val_prm = '0'";
                                ExecSQL(con_db, sqlComm, true);
#else
                                var sqlComm = "delete from " + bank.pref + "_charge_" + finder.year.Substring(2, 2) + tableDelimiter + "charge_" + finder.month + " where nzp_serv = " + nzp_serv + " and nzp_kvar = " + row[0];
                                ExecSQL(con_db, sqlComm, true);
                                sqlComm = "update " + bank.pref + "_data" + tableDelimiter + "tarif set is_actual = 100 where nzp_serv = " + nzp_serv + " and num_ls = " + row[0] + " and dat_s <= '01." + finder.month + "." + finder.year + "' and dat_po >= '01." + finder.month + "." + finder.year + "'";
                                ExecSQL(con_db, sqlComm, true);
                                sqlComm = "update " + bank.pref + "_data" + tableDelimiter + "prm_1 set dat_when =today , dat_po = cast('01." + finder.month + "." + finder.year + "' as date )-1 units day where nzp_prm = " + nzp_prm + " and nzp = " + row[0] + " and dat_s < '01." + finder.month + "." + finder.year + "' and dat_po >= '01." + finder.month + "." + finder.year + "'";
                                ExecSQL(con_db, sqlComm, true);
                                sqlComm = "update " + bank.pref + "_data" + tableDelimiter + "prm_1 set is_actual=100  , dat_when =today where nzp_prm = " + nzp_prm + " and nzp = " + row[0] + " and dat_s = '01." + finder.month + "." + finder.year + "' and dat_po >= '01." + finder.month + "." + finder.year + "'";
                                ExecSQL(con_db, sqlComm, true);
                                sqlComm = "update " + bank.pref + "_data" + tableDelimiter + "prm_1 set is_actual=100  , dat_when =today where nzp_prm = " + nzp_prm.ToString().Substring(0, nzp_prm.ToString().Length - 1) + " and nzp = " + row[0] + " and dat_s = '01." + finder.month + "." + finder.year + "' and dat_po >= '01." + finder.month + "." + finder.year + "' and val_prm = 0";
                                ExecSQL(con_db, sqlComm, true);
#endif


                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MonitorLog.WriteLog("Ошибка выполнения процедуры UploadUESCharge : " + ex.Message, MonitorLog.typelog.Error, true);
                        ret.result = false;
                        return ret;
                    }
                    finally
                    {
                        con_db.Close();
                    }
                    #endregion
                }
                #endregion
            }
            #endregion

            #region заполнение базы
            foreach (var mdbFile in files)
            {

                #region Счета Начисления УЭС
                if (mdbFile.Name == "Счета Начисления УЭС.mdb")
                {
                    var mdbBase = mdbFile.Name.Split('.')[0];
                    #region Считываем файл
                    fileName = mdbFile.FullName;

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
                    con_db = GetConnection(Constants.cons_Kernel);
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
                        foreach (DataRow row in tbl.Rows)
                        {
                            var nzp_serv = 0;
                            var nzp_frm = 0;
                            var nzp_prm = 0;
                            switch (row[1].ToString())
                            {
                                case "2":
                                    {
                                        nzp_serv = 8;
                                        nzp_frm = -2;
                                        nzp_prm = 100010;
                                    }
                                    break;
                                case "3":
                                    {
                                        nzp_serv = 9;
                                        nzp_frm = -3;
                                        nzp_prm = 100020;
                                    }
                                    break;
                            }
                            foreach (var bank in Points.PointList)
                            {
                                //todo Postgres
                                var sqlComm = "update " + bank.pref + "_charge_" + finder.year.Substring(2, 2) + tableDelimiter + "charge_" + finder.month +
                                            " set (sum_tarif, sum_charge, sum_outsaldo, sum_money, rsum_tarif) = " +
                                            " ( (" + row[3] + " + sum_tarif), (" + row[3] + " + sum_charge), (" + row[3] + " + sum_outsaldo), ("
                                            + row[3] + " + sum_money), (" + row[3] + " + rsum_tarif) ) where nzp_kvar = " + row[0] + " and nzp_serv = " + nzp_serv + ";";

#if PG
                                var res = ClassDBUtils.ExecSQL(sqlComm, con_db, true);
                                if (res.resultCode != 0)
                                    throw new Exception(res.resultMessage);

                                if (res.resultAffectedRows == 0)
#else
                                ExecSQL(con_db, sqlComm, true);
                                if (ClassDBUtils.GetAffectedRowsCount(con_db) == 0)
#endif

                                {
                                    sqlComm = "insert into " + bank.pref + "_charge_" + finder.year.Substring(2, 2) + tableDelimiter + "charge_" + finder.month +
                                        " (nzp_kvar, num_ls, nzp_serv, nzp_supp, nzp_frm, real_charge, sum_charge, sum_tarif, reval, sum_pere, sum_outsaldo, " +
                                        " c_calc, tarif, tarif_p, sum_money, rsum_lgota, sum_insaldo, sum_dlt_tarif, sum_dlt_tarif_p, sum_tarif_p, sum_lgota," +
                                        " sum_dlt_lgota, sum_dlt_lgota_p, sum_lgota_p, sum_nedop, sum_real, real_pere, money_to, money_from, money_del, sum_fakt," +
                                        " fakt_to, fakt_from, fakt_del, izm_saldo, isblocked, c_okaz, c_nedop, isdel, c_reval, tarif_f_p, rsum_tarif, sum_nedop_p, tarif_f ) values " +
                                         " ( " + row[0] + ", " + row[0] + ", " + nzp_serv + ", 6, " + nzp_frm + ", 0, " + row[3] + ", " + row[3] + ", 0, 0, " + row[3] + ", 1, 0, 0, "
                                         + row[3] + ", 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, " + row[3] + ", 0, 0 )";
                                    ret = ExecSQL(con_db, sqlComm, true);
                                    if (!ret.result)
                                    {
                                        MonitorLog.WriteLog("Ошибка записи файла " + fileName, MonitorLog.typelog.Error, true);
                                        ret.result = false;
                                        ret.text = " Ошибка загрузки файла в базу данных. ";
                                        ret.tag = -1;
                                        return ret;
                                    }
                                }
                                sqlComm = "update " + bank.pref + "_data" + tableDelimiter + "tarif set (tarif, dat_po) = " +
                                       " (" + row[3] + " + tarif, '" + DateTime.DaysInMonth(Convert.ToInt32(finder.year), Convert.ToInt32(finder.month)) + "." + finder.month + "." + finder.year + "') where nzp_kvar = " + row[0] + " and nzp_serv = " + nzp_serv + " and is_actual = 1";

#if PG

                                res = ClassDBUtils.ExecSQL(sqlComm, con_db, true);
                                if (res.resultCode != 0)
                                    ret.result = false;
#else
                                ret = ExecSQL(con_db, sqlComm, true);
#endif

                                if (!ret.result)
                                {
                                    MonitorLog.WriteLog("Ошибка записи файла " + fileName, MonitorLog.typelog.Error, true);
                                    ret.result = false;
                                    ret.text = " Ошибка загрузки файла в базу данных. ";
                                    ret.tag = -1;
                                    return ret;
                                }


#if PG

                                if (res.resultAffectedRows == 0)
#else
                                if (ClassDBUtils.GetAffectedRowsCount(con_db) == 0)
#endif

                                {
                                    sqlComm = "insert into " + bank.pref + "_data" + tableDelimiter + "tarif (nzp_kvar, num_ls, nzp_serv, nzp_supp, nzp_frm, tarif, dat_s, dat_po, is_actual, nzp_user, dat_when, is_unl, cur_unl, nzp_wp, month_calc) values " +
                                         " ( " + row[0] + ", " + row[0] + ", " + nzp_serv + ", 6, " + nzp_frm + ", " + row[3] + ", '01." + finder.month + "." + finder.year + "', '" + DateTime.DaysInMonth(Convert.ToInt32(finder.year), Convert.ToInt32(finder.month)) + "." + finder.month + "." + finder.year + "', 1, " + finder.nzp_user + ", '" + DateTime.Now.ToShortDateString() + "', 5, 0, 1, '01." + finder.month + "." + finder.year + "' )";
                                    ExecSQL(con_db, sqlComm, true);
                                }


                                sqlComm = "select max(val_prm) from " + bank.pref + "_data" + tableDelimiter + "prm_1 where nzp_prm=" + nzp_prm.ToString().Substring(0, nzp_prm.ToString().Length - 1) + " and nzp=  " + row[0] +
                                    " and dat_s<='01." + finder.month + "." + finder.year + "' and dat_po>='01." + finder.month + "." + finder.year + "'  and is_actual = 1";

                                string val_prm = ExecScalar(con_db, sqlComm, out  ret, true).ToString().Trim();
                                if (val_prm.Trim() == "" || val_prm.Trim() == "0")
                                {
                                    sqlComm = "insert into " + bank.pref + "_data" + tableDelimiter + "prm_1 (nzp, nzp_prm, dat_s, dat_po, val_prm, is_actual, cur_unl) values " +
                                                                            " ( " + row[0] + ", " + nzp_prm.ToString().Substring(0, nzp_prm.ToString().Length - 1) + ", '01." + finder.month + "." + finder.year + "', '"
                                                                            + DateTime.DaysInMonth(Convert.ToInt32(finder.year), Convert.ToInt32(finder.month)) + "."
                                                                            + finder.month + "." + finder.year + "', " + row[3] + ", 1, 1 )";
                                    ret = ExecSQL(con_db, sqlComm, true);

                                    sqlComm = "insert into " + bank.pref + "_data" + tableDelimiter + "prm_1 (nzp, nzp_prm, dat_s, dat_po, val_prm, is_actual, cur_unl) values " +
                                                                            " ( " + row[0] + ", " + nzp_prm + ", '01." + finder.month + "." + finder.year + "', '"
                                                                            + DateTime.DaysInMonth(Convert.ToInt32(finder.year), Convert.ToInt32(finder.month)) + "."
                                                                            + finder.month + "." + finder.year + "', 1, 1, 1 )";
                                    ret = ExecSQL(con_db, sqlComm, true);
                                }
                                else
                                {
                                    sqlComm = "update " + bank.pref + "_data" + tableDelimiter + "prm_1 set (val_prm, dat_s, dat_po) = " +
#if PG
 "(cast( (" + row[3] + "/" + val_prm.Trim() + ") as character(20)),  '01." + finder.month + "." + finder.year + "','"
                                                                                + DateTime.DaysInMonth(Convert.ToInt32(finder.year), Convert.ToInt32(finder.month)) + "."
                                                                                + finder.month + "." + finder.year + "') " +
                                        " where nzp = " + row[0] + " and nzp_prm = " + nzp_prm + " and is_actual = 1";
#else
 " (" + row[3] + "/" + val_prm.Trim() + " + val_prm,  '01." + finder.month + "." + finder.year + "', '"
                                        + DateTime.DaysInMonth(Convert.ToInt32(finder.year), Convert.ToInt32(finder.month)) + "."
                                        + finder.month + "." + finder.year + "') " +
                                        " where nzp = " + row[0] + " and nzp_prm = " + nzp_prm + " and is_actual = 1";
#endif

#if PG

                                    res = ClassDBUtils.ExecSQL(sqlComm, con_db, true);
                                    if (res.resultCode != 0)
                                        ret.result = false;
#else
                                    ret = ExecSQL(con_db, sqlComm, true);
#endif
                                    if (!ret.result)
                                    {
                                        MonitorLog.WriteLog("Ошибка записи файла " + fileName, MonitorLog.typelog.Error, true);
                                        ret.result = false;
                                        ret.text = " Ошибка загрузки файла в базу данных. ";
                                        ret.tag = -1;
                                        return ret;
                                    }
#if PG

                                    if (res.resultAffectedRows == 0)
#else
                                    if (ClassDBUtils.GetAffectedRowsCount(con_db) == 0)
#endif
                                    {

                                        sqlComm = "insert into " + bank.pref + "_data" + tableDelimiter + "prm_1 (nzp, nzp_prm, dat_s, dat_po, val_prm, is_actual, cur_unl) values " +
                                                                                " ( " + row[0] + ", " + nzp_prm + ", '01." + finder.month + "." + finder.year + "', '"
                                                                                + DateTime.DaysInMonth(Convert.ToInt32(finder.year), Convert.ToInt32(finder.month)) + "."
                                                                                + finder.month + "." + finder.year + "', " +
#if PG
 " cast( " + row[3] + "/" + val_prm + " as character(20)), 1, 1 )";
#else
 row[3] + "/" + val_prm + ", 1, 1 )";
#endif
                                        ret = ExecSQL(con_db, sqlComm, true);

                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MonitorLog.WriteLog("Ошибка выполнения процедуры UploadUESCharge : " + ex.Message, MonitorLog.typelog.Error, true);
                        ret.result = false;
                        return ret;
                    }
                    finally
                    {
                        con_db.Close();
                    }
                    #endregion
                }
                #endregion

                #region Счета Корректировки УЭС
                if (mdbFile.Name == "Счета Корректировки УЭС.mdb")
                {
                    var mdbBase = mdbFile.Name.Split('.')[0];
                    #region Считываем файл
                    fileName = mdbFile.FullName;

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
                    con_db = GetConnection(Constants.cons_Kernel);
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
                        foreach (DataRow row in tbl.Rows)
                        {
                            var nzp_serv = 0;
                            var nzp_frm = 0;
                            //var nzp_prm = 0;
                            switch (row[1].ToString())
                            {
                                case "2":
                                    {
                                        nzp_serv = 8;
                                        nzp_frm = -2;
                                        //nzp_prm = 100010;
                                    }
                                    break;
                                case "3":
                                    {
                                        nzp_serv = 9;
                                        nzp_frm = -3;
                                        //nzp_prm = 100020;
                                    }
                                    break;
                            }
                            foreach (var bank in Points.PointList)
                            {

                                var sqlComm = "update " + bank.pref + "_charge_" + finder.year.Substring(2, 2) + tableDelimiter + "charge_" + finder.month + " set (sum_tarif, sum_charge, sum_outsaldo, sum_money, rsum_tarif) = " +
                                                                     " ( (" + row[3] + " + sum_tarif), (" + row[3] + " + sum_charge), (" + row[3] + " + sum_outsaldo), (" + row[3] + " + sum_money), (" + row[3] + " + rsum_tarif) ) where nzp_kvar = " + row[0] + " and nzp_serv = " + nzp_serv + ";";
#if PG
                                var res = ClassDBUtils.ExecSQL(sqlComm, con_db, true);
                                if (res.resultCode != 0)
                                    throw new Exception(res.resultMessage);

                                if (res.resultAffectedRows == 0)
#else
                                ExecSQL(con_db, sqlComm, true);
                                if (ClassDBUtils.GetAffectedRowsCount(con_db) == 0)
#endif
                                {
                                    sqlComm = "insert into " + bank.pref + "_charge_" + finder.year.Substring(2, 2) + tableDelimiter + "charge_" + finder.month + " (nzp_kvar, num_ls, nzp_serv, nzp_supp, nzp_frm, real_charge, sum_charge, sum_tarif, reval, sum_pere, sum_outsaldo, c_calc, tarif, tarif_p, sum_money, rsum_lgota, sum_insaldo, sum_dlt_tarif, sum_dlt_tarif_p, sum_tarif_p, sum_lgota, sum_dlt_lgota, sum_dlt_lgota_p, sum_lgota_p, sum_nedop, sum_real, real_pere, money_to, money_from, money_del, sum_fakt, fakt_to, fakt_from, fakt_del, izm_saldo, isblocked, c_okaz, c_nedop, isdel, c_reval, tarif_f_p, rsum_tarif, sum_nedop_p, tarif_f ) values " +
                                         " ( " + row[0] + ", " + row[0] + ", " + nzp_serv + ", 6, " + nzp_frm + ", 0, " + row[3] + ", " + row[3] + ", 0, 0, " + row[3] + ", 1, 0, 0, " + row[3] + ", 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, " + row[3] + ", 0, 0 )";
                                    ret = ExecSQL(con_db, sqlComm, true);
                                    if (!ret.result)
                                    {
                                        MonitorLog.WriteLog("Ошибка записи файла " + fileName, MonitorLog.typelog.Error, true);
                                        ret.result = false;
                                        ret.text = " Ошибка загрузки файла в базу данных. ";
                                        ret.tag = -1;
                                        return ret;
                                    }
                                }

                                //#if PG
                                //                                sqlComm = "insert into " + bank.pref + "_data.tarif (nzp_kvar, num_ls, nzp_serv, nzp_supp, nzp_frm, tarif, dat_s, dat_po, is_actual, nzp_user, dat_when, is_unl, cur_unl, nzp_wp, month_calc) values " +
                                //                                                                   " ( " + row[0] + ", " + row[0] + ", " + nzp_serv + ", 6, " + nzp_frm + ", " + row[3] + ", '01." + finder.month + "." + finder.year + "', '" + DateTime.DaysInMonth(Convert.ToInt32(finder.year), Convert.ToInt32(finder.month)) + "." + finder.month + "." + finder.year + "', 1, " + finder.nzp_user + ", '" + DateTime.Now.ToShortDateString() + "', 5, 0, 1, '01." + finder.month + "." + finder.year + "' )";
                                //#else
                                //                                sqlComm = "insert into " + bank.pref + "_data"+tableDelimiter+"tarif (nzp_kvar, num_ls, nzp_serv, nzp_supp, nzp_frm, tarif, dat_s, dat_po, is_actual, nzp_user, dat_when, is_unl, cur_unl, nzp_wp, month_calc) values " +
                                //                                                                   " ( " + row[0] + ", " + row[0] + ", " + nzp_serv + ", 6, " + nzp_frm + ", " + row[3] + ", '01." + finder.month + "." + finder.year + "', '" + DateTime.DaysInMonth(Convert.ToInt32(finder.year), Convert.ToInt32(finder.month)) + "." + finder.month + "." + finder.year + "', 1, " + finder.nzp_user + ", '" + DateTime.Now.ToShortDateString() + "', 5, 0, 1, '01." + finder.month + "." + finder.year + "' )";
                                //#endif

                                //                                ret = ExecSQL(con_db, sqlComm, true);
                                //                                if (!ret.result)
                                //                                {
                                //                                    MonitorLog.WriteLog("Ошибка записи файла " + fileName, MonitorLog.typelog.Error, true);
                                //                                    ret.result = false;
                                //                                    ret.text = " Ошибка загрузки файла в базу данных. ";
                                //                                    ret.tag = -1;
                                //                                    return ret;
                                //                                }

                                //#if PG
                                //                                sqlComm = "insert into " + bank.pref + "_data.prm_1 (nzp, nzp_prm, dat_s, dat_po, val_prm, is_actual, cur_unl) values " +
                                //                                                                  " ( " + row[0] + ", " + nzp_prm + ", '01." + finder.month + "." + finder.year + "', '" + DateTime.DaysInMonth(Convert.ToInt32(finder.year), Convert.ToInt32(finder.month)) + "." + finder.month + "." + finder.year + "', " + row[3] + ", 1, 1 )";
                                //#else
                                //                                sqlComm = "insert into " + bank.pref + "_data"+tableDelimiter+"prm_1 (nzp, nzp_prm, dat_s, dat_po, val_prm, is_actual, cur_unl) values " +
                                //                                                                  " ( " + row[0] + ", " + nzp_prm + ", '01." + finder.month + "." + finder.year + "', '" + DateTime.DaysInMonth(Convert.ToInt32(finder.year), Convert.ToInt32(finder.month)) + "." + finder.month + "." + finder.year + "', " + row[3] + ", 1, 1 )";
                                //#endif


                                //                                ret = ExecSQL(con_db, sqlComm, true);
                                //                                if (!ret.result)
                                //                                {
                                //                                    MonitorLog.WriteLog("Ошибка записи файла " + fileName, MonitorLog.typelog.Error, true);
                                //                                    ret.result = false;
                                //                                    ret.text = " Ошибка загрузки файла в базу данных. ";
                                //                                    ret.tag = -1;
                                //                                    return ret;
                                //                                }


                                string sqlComm1 = "";

                                //sqlComm1 = "update " + bank.pref + "_charge_" + finder.year.Substring(2, 2) + tableDelimiter + "perekidka set" +
                                //               " ( num_ls, date_rcl, tarif, volum, sum_rcl, month_, comment, nzp_user, nzp_reestr)=" +
                                //               " ( " + row[0] + ", '01." + finder.month + "." + finder.year + "', 0 , 0 ," + row[3] + "," + finder.month +
                                //               ", 'поправка к счету', " + finder.nzp_user + "," + finder.nzp_file + ")" +
                                //               " where nzp_kvar = " + row[0] + " and nzp_serv =" + nzp_serv + " and nzp_supp = 6 and type_rcl = 1";

                                //ret = ExecSQL(con_db, sqlComm1, true);
                                //if (ClassDBUtils.GetAffectedRowsCount(con_db) == 0)
                                //{
                                sqlComm1 = "insert into " + bank.pref + "_charge_" + finder.year.Substring(2, 2) + tableDelimiter + "perekidka " +
                                           " ( nzp_kvar, num_ls, nzp_serv, nzp_supp, type_rcl, date_rcl, tarif, volum, sum_rcl, month_, comment, nzp_user, nzp_reestr)" +
                                           " values( " + row[0] + ", " + row[0] + "," + nzp_serv + ",6, 1, '01." + finder.month + "." + finder.year + "', 0 , 0 ," + row[3] + "," + finder.month +
                                           ", 'поправка к счету', " + finder.nzp_user + "," + finder.nzp_file + ")";

                                ret = ExecSQL(con_db, sqlComm1, true);
                                //}

                                if (!ret.result)
                                {
                                    MonitorLog.WriteLog("Ошибка записи файла " + fileName, MonitorLog.typelog.Error, true);
                                    ret.result = false;
                                    ret.text = " Ошибка загрузки файла в базу данных. ";
                                    ret.tag = -1;
                                    return ret;
                                }

                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MonitorLog.WriteLog("Ошибка выполнения процедуры UploadUESCharge : " + ex.Message, MonitorLog.typelog.Error, true);
                        ret.result = false;
                        return ret;
                    }
                    finally
                    {
                        con_db.Close();
                    }
                    #endregion
                }
                #endregion

                #region Счета Превышения УЭС
                if (mdbFile.Name == "Счета Превышения УЭС.mdb")
                {
                    var mdbBase = mdbFile.Name.Split('.')[0];
                    #region Считываем файл
                    fileName = mdbFile.FullName;

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
                    con_db = GetConnection(Constants.cons_Kernel);
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
                        foreach (DataRow row in tbl.Rows)
                        {
                            var nzp_serv = 0;
                            var nzp_frm = 0;
                            var nzp_prm = 0;
                            switch (row[1].ToString())
                            {
                                case "2":
                                    {
                                        nzp_serv = 8;
                                        nzp_frm = -2;
                                        nzp_prm = 100010;
                                    }
                                    break;
                                case "3":
                                    {
                                        nzp_serv = 9;
                                        nzp_frm = -3;
                                        nzp_prm = 100020;
                                    }
                                    break;
                            }
                            foreach (var bank in Points.PointList)
                            {

                                var sqlComm = "update " + bank.pref + "_charge_" + finder.year.Substring(2, 2) + tableDelimiter + "charge_" + finder.month + " set (sum_charge, sum_outsaldo, real_charge, sum_money, rsum_tarif) = " +
                                                                        " ( (" + row[3] + " + sum_charge), (" + row[3] + " + sum_outsaldo),(" + row[3] + " + real_charge), ( sum_money), ( rsum_tarif) ) where nzp_kvar = " + row[0] + " and nzp_serv = " + nzp_serv + ";";

#if PG
                                var res = ClassDBUtils.ExecSQL(sqlComm, con_db, true);
                                if (res.resultCode != 0)
                                    throw new Exception(res.resultMessage);

                                if (res.resultAffectedRows == 0)
#else
                                ExecSQL(con_db, sqlComm, true);
                                if (ClassDBUtils.GetAffectedRowsCount(con_db) == 0)
#endif

                                {
#if PG
                                    sqlComm = "insert into " + bank.pref + "_charge_" + finder.year.Substring(2, 2) + ".charge_" + finder.month + " (nzp_kvar, num_ls, nzp_serv, nzp_supp, nzp_frm, real_charge, sum_charge, sum_tarif, reval, sum_pere, sum_outsaldo, c_calc, tarif, tarif_p, sum_money, rsum_lgota, sum_insaldo, sum_dlt_tarif, sum_dlt_tarif_p, sum_tarif_p, sum_lgota, sum_dlt_lgota, sum_dlt_lgota_p, sum_lgota_p, sum_nedop, sum_real, real_pere, money_to, money_from, money_del, sum_fakt, fakt_to, fakt_from, fakt_del, izm_saldo, isblocked, c_okaz, c_nedop, isdel, c_reval, tarif_f_p, rsum_tarif, sum_nedop_p, tarif_f ) values " +
                                                                               " ( " + row[0] + ", " + row[0] + ", " + nzp_serv + ", 6, " + nzp_frm + ", 0, " + row[3] + ", " + row[3] + ", 0, 0, " + row[3] + ", 1, 0, 0, " + row[3] + ", 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, " + row[3] + ", 0, 0 )";
#else
                                    sqlComm = "insert into " + bank.pref + "_charge_" + finder.year.Substring(2, 2) + tableDelimiter + "charge_" + finder.month + " (nzp_kvar, num_ls, nzp_serv, nzp_supp, nzp_frm, real_charge, sum_charge, sum_tarif, reval, sum_pere, sum_outsaldo, c_calc, tarif, tarif_p, sum_money, rsum_lgota, sum_insaldo, sum_dlt_tarif, sum_dlt_tarif_p, sum_tarif_p, sum_lgota, sum_dlt_lgota, sum_dlt_lgota_p, sum_lgota_p, sum_nedop, sum_real, real_pere, money_to, money_from, money_del, sum_fakt, fakt_to, fakt_from, fakt_del, izm_saldo, isblocked, c_okaz, c_nedop, isdel, c_reval, tarif_f_p, rsum_tarif, sum_nedop_p, tarif_f ) values " +
                                                                               " ( " + row[0] + ", " + row[0] + ", " + nzp_serv + ", 6, " + nzp_frm + ", 0, " + row[3] + ", " + row[3] + ", 0, 0, " + row[3] + ", 1, 0, 0, " + row[3] + ", 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, " + row[3] + ", 0, 0 )";
#endif
                                    ret = ExecSQL(con_db, sqlComm, true);
                                    if (!ret.result)
                                    {
                                        MonitorLog.WriteLog("Ошибка записи файла " + fileName, MonitorLog.typelog.Error, true);
                                        ret.result = false;
                                        ret.text = " Ошибка загрузки файла в базу данных. ";
                                        ret.tag = -1;
                                        return ret;
                                    }
                                }

                                sqlComm = "update " + bank.pref + "_data" + tableDelimiter + "tarif set (tarif, dat_po) = " +
                                      " (" + row[3] + " + tarif, '" + DateTime.DaysInMonth(Convert.ToInt32(finder.year), Convert.ToInt32(finder.month)) + "." + finder.month + "." + finder.year + "') where nzp_kvar = " + row[0] + " and nzp_serv = " + nzp_serv + " and is_actual = 1";

#if PG

                                res = ClassDBUtils.ExecSQL(sqlComm, con_db, true);
                                if (res.resultCode != 0)
                                    ret.result = false;
#else
                                ret = ExecSQL(con_db, sqlComm, true);
#endif

                                if (!ret.result)
                                {
                                    MonitorLog.WriteLog("Ошибка записи файла " + fileName, MonitorLog.typelog.Error, true);
                                    ret.result = false;
                                    ret.text = " Ошибка загрузки файла в базу данных. ";
                                    ret.tag = -1;
                                    return ret;
                                }


#if PG

                                if (res.resultAffectedRows == 0)
#else
                                if (ClassDBUtils.GetAffectedRowsCount(con_db) == 0)
#endif

                                {
                                    sqlComm = "insert into " + bank.pref + "_data" + tableDelimiter + "tarif (nzp_kvar, num_ls, nzp_serv, nzp_supp, nzp_frm, tarif, dat_s, dat_po, is_actual, nzp_user, dat_when, is_unl, cur_unl, nzp_wp, month_calc) values " +
                                         " ( " + row[0] + ", " + row[0] + ", " + nzp_serv + ", 6, " + nzp_frm + ", " + row[3] + ", '01." + finder.month + "." + finder.year + "', '" + DateTime.DaysInMonth(Convert.ToInt32(finder.year), Convert.ToInt32(finder.month)) + "." + finder.month + "." + finder.year + "', 1, " + finder.nzp_user + ", '" + DateTime.Now.ToShortDateString() + "', 5, 0, 1, '01." + finder.month + "." + finder.year + "' )";
                                    ExecSQL(con_db, sqlComm, true);
                                }

                                sqlComm = "select max(val_prm) from " + bank.pref + "_data" + tableDelimiter + "prm_1 where nzp_prm=" +
                                          nzp_prm.ToString().Substring(0, nzp_prm.ToString().Length - 1) + " and nzp=  " + row[0] +
                                          " and dat_s<='01." + finder.month + "." + finder.year + "' and dat_po>='01." + finder.month + "." + finder.year + "'  and is_actual = 1";

                                string val_prm = ExecScalar(con_db, sqlComm, out  ret, true).ToString().Trim();
                                if (val_prm.Trim() == "" || val_prm.Trim() == "0")
                                {
                                    sqlComm = "insert into " + bank.pref + "_data" + tableDelimiter + "prm_1 (nzp, nzp_prm, dat_s, dat_po, val_prm, is_actual, cur_unl) values " +
                                                                            " ( " + row[0] + ", " + nzp_prm.ToString().Substring(0, nzp_prm.ToString().Length - 1) + ", '01." + finder.month + "." + finder.year + "', '"
                                                                            + DateTime.DaysInMonth(Convert.ToInt32(finder.year), Convert.ToInt32(finder.month)) + "."
                                                                            + finder.month + "." + finder.year + "', " + row[3] + ", 1, 1 )";
                                    ret = ExecSQL(con_db, sqlComm, true);

                                    sqlComm = "insert into " + bank.pref + "_data" + tableDelimiter + "prm_1 (nzp, nzp_prm, dat_s, dat_po, val_prm, is_actual, cur_unl) values " +
                                                                            " ( " + row[0] + ", " + nzp_prm + ", '01." + finder.month + "." + finder.year + "', '"
                                                                            + DateTime.DaysInMonth(Convert.ToInt32(finder.year), Convert.ToInt32(finder.month)) + "."
                                                                            + finder.month + "." + finder.year + "', 1, 1, 1 )";
                                    ret = ExecSQL(con_db, sqlComm, true);
                                }
                                else
                                {
                                    sqlComm = "update " + bank.pref + "_data" + tableDelimiter + "prm_1 set (val_prm,dat_s, dat_po) = " +
#if PG
 "(cast( (" + row[3] + "/" + val_prm.Trim() + " + coalesce((select val_prm from " + bank.pref + "_data" + tableDelimiter + "prm_1 " +
                                        " where nzp = " + row[0] + " and nzp_prm = " + nzp_prm + " and is_actual = 1),0)) as character(20)), '01." + finder.month + "." + finder.year + "', '"
                                                                                + DateTime.DaysInMonth(Convert.ToInt32(finder.year), Convert.ToInt32(finder.month)) + "."
                                                                                + finder.month + "." + finder.year + "') where nzp = " + row[0] + " and nzp_prm = " + nzp_prm + " and is_actual = 1";
#else
 " (" + row[3] + "/" + val_prm.Trim() + " + nvl((select val_prm from " + bank.pref + "_data" + tableDelimiter + "prm_1 " +
                                        " where nzp = " + row[0] + " and nzp_prm = " + nzp_prm + " and is_actual = 1),0), '01." + finder.month + "." + finder.year + "', '"
                                                                                + DateTime.DaysInMonth(Convert.ToInt32(finder.year), Convert.ToInt32(finder.month)) + "."
                                                                                + finder.month + "." + finder.year + "') " +
                                        "where nzp = " + row[0] + " and nzp_prm = " + nzp_prm + " and is_actual = 1";
#endif

#if PG

                                    res = ClassDBUtils.ExecSQL(sqlComm, con_db, true);
                                    if (res.resultCode != 0)
                                        ret.result = false;
#else
                                    ret = ExecSQL(con_db, sqlComm, true);
#endif
                                    if (!ret.result)
                                    {
                                        MonitorLog.WriteLog("Ошибка записи файла " + fileName, MonitorLog.typelog.Error, true);
                                        ret.result = false;
                                        ret.text = " Ошибка загрузки файла в базу данных. ";
                                        ret.tag = -1;
                                        return ret;
                                    }
#if PG

                                    if (res.resultAffectedRows == 0)
#else
                                    if (ClassDBUtils.GetAffectedRowsCount(con_db) == 0)
#endif
                                    {

                                        sqlComm = "insert into " + bank.pref + "_data" + tableDelimiter + "prm_1 (nzp, nzp_prm, dat_s, dat_po, val_prm, is_actual, cur_unl) values " +
                                                                                " ( " + row[0] + ", " + nzp_prm + ", '01." + finder.month + "." + finder.year + "', '"
                                                                                + DateTime.DaysInMonth(Convert.ToInt32(finder.year), Convert.ToInt32(finder.month)) + "."
                                                                                + finder.month + "." + finder.year + "', " +
#if PG
 " cast( " + row[3] + "/" + val_prm + " as character(20) + coalesce((select val_prm from " + bank.pref + "_data" + tableDelimiter + "prm_1 " +
                                        " where nzp = " + row[0] + " and nzp_prm = " + nzp_prm + " and is_actual = 1),0)), 1, 1 )";
#else
 row[3] + "/" + val_prm + " + nvl((select val_prm from " + bank.pref + "_data" + tableDelimiter + "prm_1 " +
                                                                                " where nzp = " + row[0] + " and nzp_prm = " + nzp_prm + " and is_actual = 1),0), 1, 1 )";
#endif
                                        ret = ExecSQL(con_db, sqlComm, true);

                                    }
                                }

                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MonitorLog.WriteLog("Ошибка выполнения процедуры UploadUESCharge : " + ex.Message, MonitorLog.typelog.Error, true);
                        ret.result = false;
                        ret.text = " Ошибка загрузки файла УЭС";
                        ret.tag = -1;
                        return ret;
                    }
                    finally
                    {
                        con_db.Close();
                    }
                    #endregion
                }
                #endregion
            }
            #endregion

            #region Заполняем sum_money из таблички charge значением sum_prih из таблички fn_supplier
            con_db = GetConnection(Constants.cons_Kernel);
            try
            {
                string sqlComm;
                foreach (var bank in Points.PointList)
                {
                    sqlComm =
                        "update " + bank.pref + "_charge_" + finder.year.Substring(2, 2) + tableDelimiter + "charge_" + finder.month +
                        " set sum_money = " +
                        " (select sum(sum_prih) from " + bank.pref + "_charge_" + finder.year.Substring(2, 2) + tableDelimiter + "fn_supplier" + finder.month + " fn" +
                        " where fn.nzp_supp =  " + bank.pref + "_charge_" + finder.year.Substring(2, 2) + tableDelimiter + "charge_" + finder.month + ".nzp_supp and" +
                        " fn.nzp_serv =  " + bank.pref + "_charge_" + finder.year.Substring(2, 2) + tableDelimiter + "charge_" + finder.month + ".nzp_serv and" +
                        " fn.num_ls =  " + bank.pref + "_charge_" + finder.year.Substring(2, 2) + tableDelimiter + "charge_" + finder.month + ".num_ls " +
                        " group by num_ls)";

                    ret = ExecSQL(con_db, sqlComm, true);
                    if (!ret.result)
                    {
                        MonitorLog.WriteLog("Ошибка записи файла " + fileName, MonitorLog.typelog.Error, true);
                        ret.result = false;
                        ret.text = " Ошибка загрузки файла в базу данных. ";
                        ret.tag = -1;
                        return ret;
                    }

                    sqlComm =
                        "update " + bank.pref + "_charge_" + finder.year.Substring(2, 2) + tableDelimiter + "charge_" + finder.month +
                        " set real_charge = " +
                        " (select sum(sum_rcl) from " + bank.pref + "_charge_" + finder.year.Substring(2, 2) + tableDelimiter + "perekidka p" +
                        " where p.nzp_supp =  " + bank.pref + "_charge_" + finder.year.Substring(2, 2) + tableDelimiter + "charge_" + finder.month + ".nzp_supp and" +
                        " p.nzp_serv =  " + bank.pref + "_charge_" + finder.year.Substring(2, 2) + tableDelimiter + "charge_" + finder.month + ".nzp_serv and" +
                        " p.nzp_kvar =  " + bank.pref + "_charge_" + finder.year.Substring(2, 2) + tableDelimiter + "charge_" + finder.month + ".nzp_kvar and " +
                        " month_ = " + finder.month +
                        " group by num_ls)";

                    ret = ExecSQL(con_db, sqlComm, true);
                    if (!ret.result)
                    {
                        MonitorLog.WriteLog("Ошибка записи файла " + fileName, MonitorLog.typelog.Error, true);
                        ret.result = false;
                        ret.text = " Ошибка загрузки файла в базу данных. ";
                        ret.tag = -1;
                        return ret;
                    }
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры UploadUESCharge : " + ex.Message, MonitorLog.typelog.Error, true);
                ret.result = false;
                return ret;
            }
            finally
            {
                con_db.Close();
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
                con_db = GetConnection(Constants.cons_Kernel);
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

                    string sql = " UPDATE " + Points.Pref + "_data" + tableDelimiter + "files_imported set nzp_status =  " + (int)STCLINE.KP50.Interfaces.FilesImported.Statuses.LoadedWithErrors;
                    sql += " where nzp_file = " + finder.nzp_file;
                    ret = ExecSQL(con_db, sql, true);
                    if (!ret.result)
                    {
                        MonitorLog.WriteLog("Ошибка обновления статуса файла " + fileName, MonitorLog.typelog.Error, true);
                        ret.result = false;
                        ret.text = " Ошибка обновления статуса файла. ";
                        ret.tag = -1;
                        return ret;
                    }
                }
                catch (Exception ex)
                {
                    MonitorLog.WriteLog("Ошибка выполнения процедуры UploadUESCharge : " + ex.Message, MonitorLog.typelog.Error, true);
                    ret.result = false;
                    return ret;
                }
                finally
                {
                    con_db.Close();
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
        /// получение кол-ва жильцов
        /// </summary>
        /// <param name="finder">файл</param>
        /// <returns>результат</returns>
        public ReturnsObjectType<DownloadedData> GetFileGilec(Finder finder)
        {
            ReturnsObjectType<DownloadedData> ret = new ReturnsObjectType<DownloadedData>();

            #region Запись в БД
            IDbConnection con_db = GetConnection(Constants.cons_Kernel);
            try
            {
                var t = OpenDb(con_db, true);

                if (!t.result)
                {
                    MonitorLog.WriteLog("Ошибка открытия соединения", MonitorLog.typelog.Error, true);
                    ret.result = false;
                    ret.text = " Файл не загружен, отсутствует подключение к базе данных ";
                    ret.tag = -1;
                    return ret;
                }

                //todo Postgres
#if PG
                string sql = "select p.*, u.* from (select count (*) as prib_kol from " + Points.Pref + "_data.file_gilec where nzp_file in " +
                                             " (select nzp_file from " + Points.Pref + "_data.rust_load) and nzp_tkrt = 1) p, " +
                                             " (select count (*) as ub_kol from " + Points.Pref + "_data.file_gilec where nzp_file in (select nzp_file from " + Points.Pref + "_data.rust_load) and nzp_tkrt = 2) u";
#else
                string sql = "select p.*, u.* from (select count (*) as prib_kol from " + Points.Pref + "_data" + tableDelimiter + "file_gilec where nzp_file in " +
                             " (select nzp_file from " + Points.Pref + "_data" + tableDelimiter + "rust_load) and nzp_tkrt = 1) p, " +
                             " (select count (*) as ub_kol from " + Points.Pref + "_data" + tableDelimiter + "file_gilec " +
                                " where nzp_file in (select nzp_file from " + Points.Pref + "_data" + tableDelimiter + "rust_load) and nzp_tkrt = 2) u";
#endif
                var dt = ClassDBUtils.OpenSQL(sql, con_db);
                ret.returnsData = new DownloadedData()
                {
                    kol_prib = Convert.ToInt32(dt.resultData.Rows[0]["prib_kol"]),
                    kol_ub = Convert.ToInt32(dt.resultData.Rows[0]["ub_kol"])
                };
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры GetFileGilec : " + ex.Message, MonitorLog.typelog.Error, true);
                ret.result = false;
                return ret;
            }
            finally
            {
                con_db.Close();
            }
            #endregion

            ret.result = true;
            ret.text = "Выполнено.";
            ret.tag = -1;

            return ret;
        }

        /// <summary>
        /// получение кол-ва ипу
        /// </summary>
        /// <param name="finder">файл</param>
        /// <returns>результат</returns>
        public ReturnsObjectType<DownloadedData> GetFileIpu(Finder finder)
        {
            ReturnsObjectType<DownloadedData> ret = new ReturnsObjectType<DownloadedData>();

            #region Запись в БД
            IDbConnection con_db = GetConnection(Constants.cons_Kernel);

            try
            {
                var t = OpenDb(con_db, true);

                if (!t.result)
                {
                    MonitorLog.WriteLog("Ошибка открытия соединения", MonitorLog.typelog.Error, true);
                    ret.result = false;
                    ret.text = " Файл не загружен, отсутствует подключение к базе данных ";
                    ret.tag = -1;
                    return ret;
                }

                //todo Postgres
#if PG
                string sql = "select count(*) as kol, sum(val_cnt) as sum from " + Points.Pref + "_data.file_ipu where nzp_file in (select nzp_file from " + Points.Pref + "_data.rust_load)";
#else
                string sql = "select count(*) as kol, sum(val_cnt) as sum from " + Points.Pref + "_data" + tableDelimiter + "file_ipu where nzp_file in (select nzp_file from " + Points.Pref + "_data" + tableDelimiter + "rust_load)";
#endif
                var dt = ClassDBUtils.OpenSQL(sql, con_db);
                decimal sum = 0;
                if (dt.resultData.Rows[0]["sum"] != DBNull.Value)
                    sum = Convert.ToInt32(dt.resultData.Rows[0]["sum"]);
                ret.returnsData = new DownloadedData()
                {
                    kol = Convert.ToInt32(dt.resultData.Rows[0]["kol"]),
                    sum = sum
                };
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры GetFileIpu : " + ex.Message, MonitorLog.typelog.Error, true);
                ret.result = false;
                return ret;
            }
            finally
            {
                con_db.Close();
            }
            #endregion

            ret.result = true;
            ret.text = "Выполнено.";
            ret.tag = -1;

            return ret;
        }


        /// <summary>
        /// получение кол-ва ипу
        /// </summary>
        /// <param name="finder">файл</param>
        /// <returns>результат</returns>
        public ReturnsObjectType<DownloadedData> GetFileIpuP(Finder finder)
        {
            ReturnsObjectType<DownloadedData> ret = new ReturnsObjectType<DownloadedData>();

            #region Запись в БД
            IDbConnection con_db = GetConnection(Constants.cons_Kernel);

            try
            {
                var t = OpenDb(con_db, true);

                if (!t.result)
                {
                    MonitorLog.WriteLog("Ошибка открытия соединения", MonitorLog.typelog.Error, true);
                    ret.result = false;
                    ret.text = " Файл не загружен, отсутствует подключение к базе данных ";
                    ret.tag = -1;
                    return ret;
                }


                string sql = "select count(*) as kol, sum(val_cnt) as sum from "
                    + Points.Pref + "_data" + tableDelimiter + "file_ipu_p where nzp_file in (select nzp_file from " + Points.Pref + "_data" + tableDelimiter + "rust_load)";
                var dt = ClassDBUtils.OpenSQL(sql, con_db);
                decimal sum = 0;
                if (dt.resultData.Rows[0]["sum"] != DBNull.Value)
                    sum = Convert.ToInt32(dt.resultData.Rows[0]["sum"]);
                ret.returnsData = new DownloadedData()
                {
                    kol = Convert.ToInt32(dt.resultData.Rows[0]["kol"]),
                    sum = sum
                };
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры GetFileIpuP : " + ex.Message, MonitorLog.typelog.Error, true);
                ret.result = false;
                return ret;
            }
            finally
            {
                con_db.Close();
            }
            #endregion

            ret.result = true;
            ret.text = "Выполнено.";
            ret.tag = -1;

            return ret;
        }

        /// <summary>
        /// получение кол-ва одпу
        /// </summary>
        /// <param name="finder">файл</param>
        /// <returns>результат</returns>
        public ReturnsObjectType<DownloadedData> GetFileOdpu(Finder finder)
        {
            ReturnsObjectType<DownloadedData> ret = new ReturnsObjectType<DownloadedData>();

            #region Запись в БД
            IDbConnection con_db = GetConnection(Constants.cons_Kernel);

            try
            {
                var t = OpenDb(con_db, true);

                if (!t.result)
                {
                    MonitorLog.WriteLog("Ошибка открытия соединения", MonitorLog.typelog.Error, true);
                    ret.result = false;
                    ret.text = " Файл не загружен, отсутствует подключение к базе данных ";
                    ret.tag = -1;
                    return ret;
                }

                string sql = "select count(*) as kol, sum(val_cnt) as sum from " + Points.Pref + "_data" + tableDelimiter + "file_odpu where nzp_file in (select nzp_file from " + Points.Pref + "_data" + tableDelimiter + "rust_load)";
                var dt = ClassDBUtils.OpenSQL(sql, con_db);
                decimal sum = 0;
                if (dt.resultData.Rows[0]["sum"] != DBNull.Value)
                    sum = Convert.ToInt32(dt.resultData.Rows[0]["sum"]);
                ret.returnsData = new DownloadedData()
                {
                    kol = Convert.ToInt32(dt.resultData.Rows[0]["kol"]),
                    sum = sum
                };
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры GetFileOdpu : " + ex.Message, MonitorLog.typelog.Error, true);
                ret.result = false;
                return ret;
            }
            finally
            {
                con_db.Close();
            }
            #endregion

            ret.result = true;
            ret.text = "Выполнено.";
            ret.tag = -1;

            return ret;
        }

        /// <summary>
        /// получение кол-ва одпу
        /// </summary>
        /// <param name="finder">файл</param>
        /// <returns>результат</returns>
        public ReturnsObjectType<DownloadedData> GetFileOdpuP(Finder finder)
        {
            ReturnsObjectType<DownloadedData> ret = new ReturnsObjectType<DownloadedData>();

            #region Запись в БД
            IDbConnection con_db = GetConnection(Constants.cons_Kernel);

            try
            {
                var t = OpenDb(con_db, true);

                if (!t.result)
                {
                    MonitorLog.WriteLog("Ошибка открытия соединения", MonitorLog.typelog.Error, true);
                    ret.result = false;
                    ret.text = " Файл не загружен, отсутствует подключение к базе данных ";
                    ret.tag = -1;
                    return ret;
                }

                string sql = "select count(*) as kol, sum(val_cnt) as sum from " + Points.Pref + "_data" + tableDelimiter + "file_odpu_p where nzp_file in (select nzp_file from " + Points.Pref + "_data" + tableDelimiter + "rust_load)";
                var dt = ClassDBUtils.OpenSQL(sql, con_db);
                decimal sum = 0;
                if (dt.resultData.Rows[0]["sum"] != DBNull.Value)
                    sum = Convert.ToInt32(dt.resultData.Rows[0]["sum"]);
                ret.returnsData = new DownloadedData()
                {
                    kol = Convert.ToInt32(dt.resultData.Rows[0]["kol"]),
                    sum = sum
                };
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры GetFileOdpuP : " + ex.Message, MonitorLog.typelog.Error, true);
                ret.result = false;
                return ret;
            }
            finally
            {
                con_db.Close();
            }
            #endregion

            ret.result = true;
            ret.text = "Выполнено.";
            ret.tag = -1;

            return ret;
        }

        /// <summary>
        /// получение кол-ва недопоставок
        /// </summary>
        /// <param name="finder">файл</param>
        /// <returns>результат</returns>
        public ReturnsObjectType<DownloadedData> GetFileNedopost(Finder finder)
        {
            ReturnsObjectType<DownloadedData> ret = new ReturnsObjectType<DownloadedData>();

            #region Запись в БД
            IDbConnection con_db = GetConnection(Constants.cons_Kernel);

            try
            {
                var t = OpenDb(con_db, true);

                if (!t.result)
                {
                    MonitorLog.WriteLog("Ошибка открытия соединения", MonitorLog.typelog.Error, true);
                    ret.result = false;
                    ret.text = " Файл не загружен, отсутствует подключение к базе данных ";
                    ret.tag = -1;
                    return ret;
                }

                //todo Postgres
#if PG
                string sql = "select count(*) as kol, sum(sum_ned) as sum from " + Points.Pref + "_data.file_nedopost where nzp_file in (select nzp_file from " + Points.Pref + "_data.rust_load)";
#else
                string sql = "select count(*) as kol, sum(sum_ned) as sum from " + Points.Pref + "_data" + tableDelimiter + "file_nedopost where nzp_file in (select nzp_file from " + Points.Pref + "_data" + tableDelimiter + "rust_load)";
#endif
                var dt = ClassDBUtils.OpenSQL(sql, con_db);
                decimal sum = 0;
                if (dt.resultData.Rows[0]["sum"] != DBNull.Value)
                    sum = Convert.ToInt32(dt.resultData.Rows[0]["sum"]);
                ret.returnsData = new DownloadedData()
                {
                    kol = Convert.ToInt32(dt.resultData.Rows[0]["kol"]),
                    sum = sum
                };
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры GetFileNedopost : " + ex.Message, MonitorLog.typelog.Error, true);
                ret.result = false;
                return ret;
            }
            finally
            {
                con_db.Close();
            }
            #endregion

            ret.result = true;
            ret.text = "Выполнено.";
            ret.tag = -1;

            return ret;
        }

        /// <summary>
        /// получение кол-ва одпу
        /// </summary>
        /// <param name="finder">файл</param>
        /// <returns>результат</returns>
        public ReturnsObjectType<DownloadedData> GetFileOplats(Finder finder)
        {
            ReturnsObjectType<DownloadedData> ret = new ReturnsObjectType<DownloadedData>();

            #region Запись в БД
            IDbConnection con_db = GetConnection(Constants.cons_Kernel);

            try
            {
                var t = OpenDb(con_db, true);

                if (!t.result)
                {
                    MonitorLog.WriteLog("Ошибка открытия соединения", MonitorLog.typelog.Error, true);
                    ret.result = false;
                    ret.text = " Файл не загружен, отсутствует подключение к базе данных ";
                    ret.tag = -1;
                    return ret;
                }

                //todo Postgres
#if PG
                string sql = "select count(*) as kol, sum(sum_oplat) as sum from " + Points.Pref + "_data.file_oplats where nzp_file in (select nzp_file from " + Points.Pref + "_data.rust_load)";
#else
                string sql = "select count(*) as kol, sum(sum_oplat) as sum from " + Points.Pref + "_data" + tableDelimiter + "file_oplats where nzp_file in (select nzp_file from " + Points.Pref + "_data" + tableDelimiter + "rust_load)";
#endif
                var dt = ClassDBUtils.OpenSQL(sql, con_db);
                decimal sum = 0;
                if (dt.resultData.Rows[0]["sum"] != DBNull.Value)
                    sum = Convert.ToInt32(dt.resultData.Rows[0]["sum"]);
                ret.returnsData = new DownloadedData()
                {
                    kol = Convert.ToInt32(dt.resultData.Rows[0]["kol"]),
                    sum = sum
                };
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры GetFileOplats : " + ex.Message, MonitorLog.typelog.Error, true);
                ret.result = false;
                return ret;
            }
            finally
            {
                con_db.Close();
            }
            #endregion

            ret.result = true;
            ret.text = "Выполнено.";
            ret.tag = -1;

            return ret;
        }

        /// <summary>
        /// получение кол-ва одпу
        /// </summary>
        /// <param name="finder">файл</param>
        /// <returns>результат</returns>
        public ReturnsObjectType<DownloadedData> GetFileParamDom(Finder finder)
        {
            ReturnsObjectType<DownloadedData> ret = new ReturnsObjectType<DownloadedData>();

            #region Запись в БД
            IDbConnection con_db = GetConnection(Constants.cons_Kernel);

            try
            {
                var t = OpenDb(con_db, true);

                if (!t.result)
                {
                    MonitorLog.WriteLog("Ошибка открытия соединения", MonitorLog.typelog.Error, true);
                    ret.result = false;
                    ret.text = " Файл не загружен, отсутствует подключение к базе данных ";
                    ret.tag = -1;
                    return ret;
                }

                //todo Postgres
#if PG
                string sql = "select count(*) as kol from " + Points.Pref + "_data.file_paramsdom where nzp_file in (select nzp_file from " + Points.Pref + "_data.rust_load)";
#else
                string sql = "select count(*) as kol from " + Points.Pref + "_data" + tableDelimiter + "file_paramsdom where nzp_file in (select nzp_file from " + Points.Pref + "_data" + tableDelimiter + "rust_load)";
#endif
                var dt = ClassDBUtils.OpenSQL(sql, con_db);
                ret.returnsData = new DownloadedData()
                {
                    kol = Convert.ToInt32(dt.resultData.Rows[0]["kol"]),
                };
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры GetFileParamDom : " + ex.Message, MonitorLog.typelog.Error, true);
                ret.result = false;
                return ret;
            }
            finally
            {
                con_db.Close();
            }
            #endregion

            ret.result = true;
            ret.text = "Выполнено.";
            ret.tag = -1;

            return ret;
        }

        /// <summary>
        /// получение кол-ва одпу
        /// </summary>
        /// <param name="finder">файл</param>
        /// <returns>результат</returns>
        public ReturnsObjectType<DownloadedData> GetFileParamLs(Finder finder)
        {
            ReturnsObjectType<DownloadedData> ret = new ReturnsObjectType<DownloadedData>();

            #region Запись в БД
            IDbConnection con_db = GetConnection(Constants.cons_Kernel);

            try
            {
                var t = OpenDb(con_db, true);

                if (!t.result)
                {
                    MonitorLog.WriteLog("Ошибка открытия соединения", MonitorLog.typelog.Error, true);
                    ret.result = false;
                    ret.text = " Файл не загружен, отсутствует подключение к базе данных ";
                    ret.tag = -1;
                    return ret;
                }

                //todo Postgres
#if PG
                string sql = "select count(*) as kol from " + Points.Pref + "_data.file_paramsls where nzp_file in (select nzp_file from " + Points.Pref + "_data.rust_load)";
#else
                string sql = "select count(*) as kol from " + Points.Pref + "_data" + tableDelimiter + "file_paramsls where nzp_file in (select nzp_file from " + Points.Pref + "_data" + tableDelimiter + "rust_load)";
#endif
                var dt = ClassDBUtils.OpenSQL(sql, con_db);
                ret.returnsData = new DownloadedData()
                {
                    kol = Convert.ToInt32(dt.resultData.Rows[0]["kol"]),
                };
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры GetFileParamLs : " + ex.Message, MonitorLog.typelog.Error, true);
                ret.result = false;
                return ret;
            }
            finally
            {
                con_db.Close();
            }
            #endregion

            ret.result = true;
            ret.text = "Выполнено.";
            ret.tag = -1;

            return ret;
        }

        /// <summary>
        /// получение кол-ва одпу
        /// </summary>
        /// <param name="finder">файл</param>
        /// <returns>результат</returns>
        public ReturnsObjectType<DownloadedData> GetFileTypeNedop(Finder finder)
        {
            ReturnsObjectType<DownloadedData> ret = new ReturnsObjectType<DownloadedData>();

            #region Запись в БД
            IDbConnection con_db = GetConnection(Constants.cons_Kernel);

            try
            {
                var t = OpenDb(con_db, true);

                if (!t.result)
                {
                    MonitorLog.WriteLog("Ошибка открытия соединения", MonitorLog.typelog.Error, true);
                    ret.result = false;
                    ret.text = " Файл не загружен, отсутствует подключение к базе данных ";
                    ret.tag = -1;
                    return ret;
                }

                //todo Postgres
#if PG
                string sql = "select count(*) as kol from " + Points.Pref + "_data.file_typenedopost where nzp_file in (select nzp_file from " + Points.Pref + "_data.rust_load)";
#else
                string sql = "select count(*) as kol from " + Points.Pref + "_data" + tableDelimiter + "file_typenedopost where nzp_file in (select nzp_file from " + Points.Pref + "_data" + tableDelimiter + "rust_load)";
#endif
                var dt = ClassDBUtils.OpenSQL(sql, con_db);
                ret.returnsData = new DownloadedData()
                {
                    kol = Convert.ToInt32(dt.resultData.Rows[0]["kol"]),
                };
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры GetFileTypeNedop : " + ex.Message, MonitorLog.typelog.Error, true);
                ret.result = false;
                return ret;
            }
            finally
            {
                con_db.Close();
            }
            #endregion

            ret.result = true;
            ret.text = "Выполнено.";
            ret.tag = -1;

            return ret;
        }

        /// <summary>
        /// получение кол-ва одпу
        /// </summary>
        /// <param name="finder">файл</param>
        /// <returns>результат</returns>
        public ReturnsObjectType<DownloadedData> GetFileTypeParams(Finder finder)
        {
            ReturnsObjectType<DownloadedData> ret = new ReturnsObjectType<DownloadedData>();

            #region Запись в БД
            IDbConnection con_db = GetConnection(Constants.cons_Kernel);

            try
            {
                var t = OpenDb(con_db, true);

                if (!t.result)
                {
                    MonitorLog.WriteLog("Ошибка открытия соединения", MonitorLog.typelog.Error, true);
                    ret.result = false;
                    ret.text = " Файл не загружен, отсутствует подключение к базе данных ";
                    ret.tag = -1;
                    return ret;
                }

                //todo Postgres
#if PG
                string sql = "select count(*) as kol from " + Points.Pref + "_data.file_typeparams where nzp_file in (select nzp_file from " + Points.Pref + "_data.rust_load)";
#else
                string sql = "select count(*) as kol from " + Points.Pref + "_data" + tableDelimiter + "file_typeparams where nzp_file in (select nzp_file from " + Points.Pref + "_data" + tableDelimiter + "rust_load)";
#endif
                var dt = ClassDBUtils.OpenSQL(sql, con_db);
                ret.returnsData = new DownloadedData()
                {
                    kol = Convert.ToInt32(dt.resultData.Rows[0]["kol"]),
                };
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры GetFileTypeParams : " + ex.Message, MonitorLog.typelog.Error, true);
                ret.result = false;
                return ret;
            }
            finally
            {
                con_db.Close();
            }
            #endregion

            ret.result = true;
            ret.text = "Выполнено.";
            ret.tag = -1;

            return ret;
        }

        /// <summary>
        /// получение nzp_kvar
        /// </summary>

        public Returns GetNZP_Kvar(int intStreetID, string strHomeNo, int intFlatNo)
        {
            Returns retData = Utils.InitReturns();
            #region Create and execute SQL query.
            IDbConnection con_db = GetConnection(Constants.cons_Kernel);

            try
            {
                var t = OpenDb(con_db, true);

                if (!t.result)
                {
                    MonitorLog.WriteLog("Ошибка открытия соединения", MonitorLog.typelog.Error, true);
                    retData.result = false;
                    retData.text = " Файл не загружен, отсутствует подключение к базе данных ";
                    retData.tag = -1;
                    return retData;
                }

#if PG
                string strQuery = String.Format("SELECT MIN({0}_data.kvar.nzp_kvar) AS _NZP_KVAR FROM {0}_data.kvar, {0}_data.dom WHERE {0}_data.dom.nzp_dom = {0}_data.kvar.nzp_dom AND {0}_data.dom.nzp_ul = {1} AND TRIM(UPPER({0}_data.dom.ndom)) = '{2}' AND {0}_data.kvar.nkvar = {3};", Points.Pref, intStreetID, strHomeNo, intFlatNo);
#else
                string strQuery = String.Format("SELECT MIN({0}_data" + tableDelimiter + "kvar.nzp_kvar) AS _NZP_KVAR FROM {0}_data" + tableDelimiter + "kvar, {0}_data" + tableDelimiter + "dom WHERE {0}_data" + tableDelimiter + "dom.nzp_dom = {0}_data" + tableDelimiter + "kvar.nzp_dom AND {0}_data" + tableDelimiter + "dom.nzp_ul = {1} AND TRIM(UPPER({0}_data" + tableDelimiter + "dom.ndom)) = '{2}' AND {0}_data" + tableDelimiter + "kvar.nkvar = {3};", Points.Pref, intStreetID, strHomeNo, intFlatNo);
#endif
                DataTable dtResult = ClassDBUtils.OpenSQL(strQuery, con_db, ClassDBUtils.ExecMode.Exception).GetData();
                retData.result = true;
                retData.text = Convert.ToString(dtResult.Rows[0]["_NZP_KVAR"]);
                retData.tag = 0;
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("GetNZP_Kvar\n" + ex.Message, MonitorLog.typelog.Error, true);
                retData.result = false;
                retData.text = ex.Message;
            }
            finally
            {
                con_db.Close();
            }
            #endregion
            return retData;
        }

       
        /// <summary>
        /// получение несопоставленных участков
        /// </summary>
        /// <param name="finder">файл</param>
        /// <returns>результат</returns>
        public ReturnsObjectType<List<ServFormulFinder>> GetServFormul(Finder finder)
        {

            ReturnsObjectType<List<ServFormulFinder>> ret = new ReturnsObjectType<List<ServFormulFinder>>();

            #region Запись в БД
            IDbConnection con_db = GetConnection(Constants.cons_Kernel);

            try
            {
                var t = OpenDb(con_db, true);
                string sql = "";

                if (!t.result)
                {
                    MonitorLog.WriteLog("Ошибка открытия соединения", MonitorLog.typelog.Error, true);
                    ret.result = false;
                    ret.text = " Файл не загружен, отсутствует подключение к базе данных ";
                    ret.tag = -1;
                    return ret;
                }

                try
                {
                    sql = "drop table tmp_serv; ";
                    ClassDBUtils.ExecSQL(sql, con_db);
                }
                catch (Exception)
                {
                }

                //создаем временную таблицу
                sql = " create temp table tmp_serv ( nzp_serv integer, service char(100), ed_izmer char(100), nzp_ops integer, nzp_frm integer, nzp_measure integer, nzp_prm_tarif_ls integer, " +
                        " nzp_prm_tarif_lsp integer, nzp_prm_tarif_dm integer, " +
                        " nzp_prm_tarif_su integer, nzp_prm_tarif_bd integer, nzp_supp integer, name_supp char(100), name_frm char(100), toChange integer ) ";
                ClassDBUtils.ExecSQL(sql, con_db);

                //определяем префиксы
                sql = " select distinct pref from " + Points.Pref + "_data" + tableDelimiter + "rust_load where pref is not null and pref <> '' ";
                var tdt = ClassDBUtils.OpenSQL(sql, con_db);
                //заполняем временную таблицу
                foreach (DataRow tr in tdt.resultData.Rows)
                {
                    sql = "insert into tmp_serv " +
                         " select distinct b.nzp_serv, service, ed_izmer, nzp_ops, aa.nzp_frm, b.nzp_measure, nzp_prm_tarif_ls, nzp_prm_tarif_lsp, " +
                        " nzp_prm_tarif_dm, nzp_prm_tarif_su, nzp_prm_tarif_bd, t.nzp_supp, supp.name_supp, a.name_frm,  " +
                        " (case when (select count(*) from " + Points.Pref + "_data" + tableDelimiter + "file_serv_tuning where nzp_serv = b.nzp_serv and nzp_supp = supp.nzp_supp and " +
                        " nzp_measure = b.nzp_measure and nzp_frm = a.nzp_frm) <> 0 then 1 else 0 end) as toChange  " +
                    " from  " + tr["pref"] + "_kernel" + tableDelimiter + "formuls_opis aa, " + tr["pref"] + "_kernel" + tableDelimiter + "formuls a, " +
                    Points.Pref + "_kernel" + tableDelimiter + "services b, " + tr["pref"] + "_kernel" + tableDelimiter + "l_foss t, " +
                        Points.Pref + "_data" + tableDelimiter + "file_serv fs, " + Points.Pref + "_kernel" + tableDelimiter + "supplier supp   " +
                        " where a.nzp_measure=b.nzp_measure  and t.nzp_serv=b.nzp_serv and t.nzp_frm =a.nzp_frm and a.nzp_frm<>1  and a.nzp_frm=aa.nzp_frm  and fs.nzp_serv = b.nzp_serv " +
                        " and supp.nzp_supp = t.nzp_supp and  fs.nzp_file in (select nzp_file from " + Points.Pref + "_data" + tableDelimiter + "rust_load where pref is not null and pref = '" + tr["pref"] + "') ";
                    ClassDBUtils.ExecSQL(sql, con_db);
                }
#if PG
                string skip = finder.skip >= 0 && finder.rows >= 0 ? " offset " + finder.skip + " limit " + finder.rows : String.Empty;
#else
                string skip = finder.skip >= 0 && finder.rows >= 0 ? " skip " + finder.skip + " first " + finder.rows : String.Empty;
#endif
                sql = " select * from tmp_serv; ";

                var dt = ClassDBUtils.OpenSQL(sql, con_db);
                List<ServFormulFinder> list = new List<ServFormulFinder>();
                for (int i = 0; i < dt.resultData.Rows.Count; i++)
                {
                    list.Add(new ServFormulFinder()
                    {
                        serv_name = dt.resultData.Rows[i]["service"].ToString().Trim(),
                        supplier_name = dt.resultData.Rows[i]["name_supp"].ToString().Trim(),
                        measure_name = dt.resultData.Rows[i]["ed_izmer"].ToString().Trim(),
                        formul_name = dt.resultData.Rows[i]["name_frm"].ToString().Trim(),
                        toChange = dt.resultData.Rows[i]["toChange"].ToString().Trim() == "1" ? true : false,
                        nzp_serv = Convert.ToInt32(dt.resultData.Rows[i]["nzp_serv"]),
                        nzp_measure = Convert.ToInt32(dt.resultData.Rows[i]["nzp_measure"]),
                        nzp_supp = Convert.ToInt32(dt.resultData.Rows[i]["nzp_supp"]),
                        nzp_frm = Convert.ToInt32(dt.resultData.Rows[i]["nzp_frm"])
                    });
                    if (PmaxVisible < i) { break; }
                }
                ret.returnsData = list;

                Returns retVar = new Returns();
#if PG
                sql = " select count(*) from (select distinct upper(area) as area from " + Points.Pref + "_data.file_area  where nzp_area is null and " +
                                                " nzp_file in (select nzp_file from " + Points.Pref + "_data.rust_load))";
#else
                sql = " select count(*) from tmp_serv ";
#endif
                object count = ExecScalar(con_db, sql, out retVar, true);
                if (retVar.result)
                {
                    ret.tag = Convert.ToInt32(count);
                }

                sql = "drop table tmp_serv; ";
                ClassDBUtils.ExecSQL(sql, con_db);
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры GetServFormul : " + ex.Message, MonitorLog.typelog.Error, true);
                ret.result = false;
                return ret;
            }
            finally
            {
                con_db.Close();
            }
            #endregion


            ret.result = true;
            ret.text = "Выполнено.";

            return ret;
        }

        /// <summary>
        /// сохранение в таблицу file_serv_tuning
        /// </summary>
        /// <param name="finder">файл</param>
        /// <returns>результат</returns>
        public Returns SetToChange(ServFormulFinder finder)
        {

            Returns ret = new Returns();

            #region Запись в БД
            IDbConnection con_db = GetConnection(Constants.cons_Kernel);

            try
            {
                var t = OpenDb(con_db, true);
                string sql = "";

                if (!t.result)
                {
                    MonitorLog.WriteLog("Ошибка открытия соединения", MonitorLog.typelog.Error, true);
                    ret.result = false;
                    ret.text = " Файл не загружен, отсутствует подключение к базе данных ";
                    ret.tag = -1;
                    return ret;
                }

                sql = " select count(*) from " + Points.Pref + "_data" + tableDelimiter + "file_serv_tuning where nzp_serv = " + finder.nzp_serv + " and nzp_measure = " + finder.nzp_measure + " and nzp_supp = " +
                        finder.nzp_supp + " and nzp_frm = " + finder.nzp_frm;

                var count = Convert.ToInt32(ExecScalar(con_db, sql, out ret, true));
                if (count == 0)
                {
                    sql = "insert into " + Points.Pref + "_data" + tableDelimiter + "file_serv_tuning (nzp_serv, nzp_measure, nzp_supp, nzp_frm) values " +
                        " ( " + finder.nzp_serv + ", " + finder.nzp_measure + ", " + finder.nzp_supp + ", " + finder.nzp_frm + " ) ";
                    ClassDBUtils.ExecSQL(sql, con_db);
                }
                else
                {
                    sql = "delete from " + Points.Pref + "_data" + tableDelimiter + "file_serv_tuning where nzp_serv = " + finder.nzp_serv + " and nzp_measure = " + finder.nzp_measure + " and nzp_supp = " +
                        finder.nzp_supp + " and nzp_frm = " + finder.nzp_frm;
                    ClassDBUtils.ExecSQL(sql, con_db);
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры SetToChange : " + ex.Message, MonitorLog.typelog.Error, true);
                ret.result = false;
                return ret;
            }
            finally
            {
                con_db.Close();
            }
            #endregion


            ret.result = true;
            ret.text = "Выполнено.";

            return ret;
        }

      
        #region Удаление несвязанных данных из выбранного файла
        //удаление несвязной информации
        public Returns DeleteUnrelatedInfo()
        {

            Returns ret = new Returns();

            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);

            try
            {
                ret = OpenDb(conn_db, true);

                if (!ret.result)
                {
                    MonitorLog.WriteLog("Ошибка открытия соединения", MonitorLog.typelog.Error, true);
                    ret.result = false;
                    ret.text = " Файл не загружен, отсутствует подключение к базе данных ";
                    ret.tag = -1;
                    return ret;
                }

                ret = DeleteUnrelatedInfo(conn_db);
                if (!ret.result)
                {
                    ret.result = false;
                    ret.text = "Удаление несвязной информации произошло с ошибками";
                }

            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры DeleteUnrelatedInfo : " + ex.Message, MonitorLog.typelog.Error, true);
                ret.result = false;
                ret.text = "Ошибка выполнения процедуры удаления несвязанной информации";
            }
            finally
            {
                conn_db.Close();
            }

            return ret;
        }

        private Returns DeleteUnrelatedInfo(IDbConnection conn_db)
        {
            Returns ret = new Returns();

            try
            {
                string sql = "";
                StringBuilder err = new StringBuilder();

                sql = "insert into " + Points.Pref + "_data" + tableDelimiter + "file_del_unrel_info (id, nzp_file, is_success) select nzp_file, nzp_file, 1 from " + Points.Pref + "_data" + tableDelimiter + "rust_load";
                ret = ExecSQL(conn_db, sql, true);

                if (ret.result)
                {
                    #region 22. Выбираем дома без УК

                    DeleteOneUnrelated(conn_db, err, "file_dom", "id", "file_area", "id", "area_id", "id", "id", "Уникальный код дома", "Уникальный код УК", Points.Pref.ToString(), "дома без УК");
                    #endregion Выбираем дома без УК

                    #region 8. Выбираем дома без МО

                    DeleteOneUnrelated(conn_db, err, "file_dom", "id", "file_mo", "id_mo, id", "mo_id", "id_mo", "id", "Уникальный код дома", "Уникальный код МО", Points.Pref.ToString(), "дома без МО");
                    #endregion Выбираем дома без МО

                    #region 1. Выбираем квартиры без домов

                    DeleteOneUnrelated(conn_db, err, "file_kvar", "id", "file_dom", "id", "dom_id", "id", "id", "Номер ЛС", "Уникальный номер дома", Points.Pref.ToString(), "квартиры без домов");
                    #endregion Выбираем квартиры без домов

                    #region 27. Выбираем квартиры без кода ЮЛ

                    DeleteOneUnrelated(conn_db, err, "file_kvar", "id", "file_urlic", "supp_id, id", "id_urlic", "supp_id", "id", "Номер ЛС", "Код ЮЛ", Points.Pref.ToString(), "квартиры без кода ЮЛ");
                    #endregion Выбираем квартиры без кода ЮЛ

                    #region 26. Выбираем квартиры без кода типа по горячему водоснабжению

                    DeleteOneUnrelated(conn_db, err, "file_kvar", "id", "file_voda", "id_prm, id", "hotwater_type", "id_prm", "id", "Номер ЛС", "Код параметра", Points.Pref.ToString(), "квартиры без кода типа по горячему водоснабжению");
                    #endregion Выбираем квартиры без кода типа по горячему водоснабжению

                    #region 25. Выбираем квартиры без кода типа по водоснабжению

                    DeleteOneUnrelated(conn_db, err, "file_kvar", "id", "file_voda", "id_prm, id", "water_type", "id_prm", "id", "Номер ЛС", "Код параметра", Points.Pref.ToString(), "квартиры без кода типа по водоснабжению");
                    #endregion Выбираем квартиры без кода типа по водоснабжению

                    #region 24. Выбираем квартиры без кода типа по газоснабжению

                    DeleteOneUnrelated(conn_db, err, "file_kvar", "id", "file_gaz", "id_prm, id", "gas_type", "id_prm", "id", "Номер ЛС", "Код параметра", Points.Pref.ToString(), "квартиры без кода типа по газоснабжению");
                    #endregion Выбираем квартиры без кода типа по газоснабжению

                    #region 12. Выбираем параметры дома без дома

                    DeleteOneUnrelated(conn_db, err, "file_paramsdom", "id_dom, id", "file_dom", "id", "id_dom", "id", "id_prm", "Код параметра", "Уникальный код дома", Points.Pref.ToString(), "параметры дома без дома");
                    #endregion Выбираем параметры дома без дома

                    #region 9. Выбираем общедомовые приборы учета без дома

                    DeleteOneUnrelated(conn_db, err, "file_odpu", "local_id, id", "file_dom", "id", "dom_id", "id", "num_cnt", "Заводской номер ПУ", "Уникальный код дома", Points.Pref.ToString(), "ОДПУ без домов");
                    #endregion Выбираем общедомовые приборы учета без дома

                    #region 10. Выбираем ОДПУ без единиц измерения

                    DeleteOneUnrelated(conn_db, err, "file_odpu", "local_id, id", "file_measures", "id_measure, id", "nzp_measure", "id_measure", "num_cnt", "Заводской номер ПУ", "Код единицы измерения", Points.Pref.ToString(), "ОДПУ без единиц измерения");
                    #endregion Выбираем ОДПУ без единиц измерения

                    #region 23. Выбираем ОДПУ без кода услуги

                    DeleteOneUnrelated(conn_db, err, "file_odpu", "local_id, id", "file_services", "id_serv,  id", "nzp_serv", "id_serv", "num_cnt", "Заводской номер ПУ", "Код услуги", Points.Pref.ToString(), "ОДПУ без кода услуги");
                    #endregion Выбираем общедомовые приборы учета без кода услуги

                    #region 21. Выбираем показания ОДПУ без ОДПУ

                    DeleteOneUnrelated(conn_db, err, "file_odpu_p", "id_odpu, id", "file_odpu", "local_id, id", "id_odpu", "local_id", "dat_uchet", "Дата учета", "Код ОДПУ", Points.Pref.ToString(), "показания ОДПУ без ОДПУ");
                    #endregion Выбираем показания ОДПУ без ОДПУ

                    #region 29. Выбираем оплаты без номера пачки

                    DeleteOneUnrelated(conn_db, err, "file_oplats", "ls_id, id", "file_pack", "num_plat,id", "nzp_pack", "num_plat", "numplat", "Номер платежного документа", "Номер пачки", Points.Pref.ToString(), "оплаты без номера пачки");
                    #endregion Выбираем оплаты без номера пачки

                    #region 11. Выбираем оплаты без квартир

                    DeleteOneUnrelated(conn_db, err, "file_oplats", "ls_id, id", "file_kvar", "id", "ls_id", "id", "numplat", "Номер платежного документа", "Номер ЛС", Points.Pref.ToString(), "оплаты без квартир");
                    #endregion Выбираем оплаты без квартир

                    #region 7. Выбираем услуги без единицы измерения

                    DeleteOneUnrelated(conn_db, err, "file_serv", "ls_id, nzp_serv, nzp_measure, id", "file_measures", "id_measure, id", "nzp_measure", "id_measure", "nzp_serv", "Код услуги", "Единица измерения", Points.Pref.ToString(), "услуги без единицы измерения");
                    #endregion Выбираем услуги без единицы измерения

                    #region 24. Выбираем услуги, не входящие в перечень выгруженных услуг

                    DeleteOneUnrelated(conn_db, err, "file_serv", "ls_id, nzp_serv, nzp_measure, id", "file_services", "id_serv,  id", "nzp_serv", "id_serv",
                        "nzp_serv", "Код услуги", "Код услуги", Points.Pref.ToString(), "услуги, не входящие в перечень выгруженных услуг, ");
                    #endregion Выбираем услуги без поставщиков

                    #region 20. Выбираем услуги без поставщиков

                    DeleteOneUnrelated(conn_db, err, "file_serv", "ls_id, nzp_serv, nzp_measure, id", "file_supp", "supp_id, id", "supp_id", "supp_id", "nzp_serv", "Код услуги", "Код поставщика", Points.Pref.ToString(), "услуги без поставщиков");
                    #endregion Выбираем услуги без поставщиков

                    #region 2. Выбираем услуги без квартир

                    DeleteOneUnrelated(conn_db, err, "file_serv", "ls_id, nzp_serv, nzp_measure, id", "file_kvar", "id", "ls_id", "id", "nzp_serv", "Код услуги", "Номер ЛС", Points.Pref.ToString(), "услуги без квартир");
                    #endregion Выбираем услуги без квартир

                    #region 28. Выбираем ИПУ без единиц измерения

                    DeleteOneUnrelated(conn_db, err, "file_ipu", "local_id, id", "file_measures", "id_measure, id", "nzp_measure", "id_measure", "num_cnt", "Заводской номер ПУ", "Код единицы измерения", Points.Pref.ToString(), "ИПУ без единиц измерения");
                    #endregion Выбираем ИПУ без единиц измерения

                    #region 23. Выбираем ИПУ без кода услуги

                    DeleteOneUnrelated(conn_db, err, "file_ipu", "local_id, id", "file_services", "id_serv,  id", "nzp_serv", "id_serv", "num_cnt", "Заводской номер ПУ", "Код услуги", Points.Pref.ToString(), "ИПУ без кода услуги");
                    #endregion Выбираем ИПУ без кода услуги

                    #region 4. Выбираем ИПУ без квартир

                    DeleteOneUnrelated(conn_db, err, "file_ipu", "local_id, id", "file_kvar", "id", "ls_id", "id", "num_cnt", "Заводской номер ПУ", "Номер ЛС", Points.Pref.ToString(), "ИПУ без квартир");
                    #endregion Выбираем ИПУ без квартир

                    #region 5. Выбираем показания ИПУ без ИПУ

                    DeleteOneUnrelated(conn_db, err, "file_ipu_p", "id_ipu, id", "file_ipu", "local_id, id", "id_ipu", "local_id", "dat_uchet", "Дата показания", "уникальный код ПУ", Points.Pref.ToString(), "показания ИПУ без ИПУ");
                    #endregion Выбираем пересчет ИПУ без ИПУ

                    #region 15. Выбираем параметры услуг без поставщиков

                    DeleteOneUnrelated(conn_db, err, "file_servp", "ls_id, id", "file_supp", "supp_id, id", "supp_id", "supp_id", "reval_month", "Дата перерасчета", "Уникальный код поставщика", Points.Pref.ToString(), "параметры услуг без поставщиков");
                    #endregion Выбираем параметры услуг без поставщиков

                    #region 14. Выбираем параметры услуг без квартир

                    DeleteOneUnrelated(conn_db, err, "file_servp", "ls_id, id", "file_kvar", "id", "ls_id", "id", "reval_month", "Дата перерасчета", "Номер ЛС", Points.Pref.ToString(), "параметры услуг без квартир");
                    #endregion Выбираем параметры услуг без квартир

                    #region 3. Выбираем жильцов без квартир

                    DeleteOneUnrelated(conn_db, err, "file_gilec", "num_ls, id", "file_kvar", "id", "num_ls", "id", "nzp_gil", "Уникальный номер гражданина", "Номер ЛС", Points.Pref.ToString(), "жильцы без квартир");
                    #endregion Выбираем жильцов без квартир

                    #region 13. Выбираем параметры ЛС без квартиры

                    DeleteOneUnrelated(conn_db, err, "file_paramsls", "ls_id, id", "file_kvar", "id", "ls_id", "id", "id_prm", "Код параметра", "Номер ЛС", Points.Pref.ToString(), "параметры ЛС без квартир");
                    #endregion Выбираем параметры ЛС без квартиры

                    #region 6. Выбираем параметры квартиры без квартиры

                    DeleteOneUnrelated(conn_db, err, "file_kvarp", "id", "file_kvar", "id", "nzp_kvar", "id", "reval_month", "Дата перерасчета", "Номер ЛС", Points.Pref.ToString(), "пересчеты квартиры без квартир");
                    #endregion Выбираем параметры квартиры без квартиры

                    #region 19. Выбираем временно убывших без квартир

                    DeleteOneUnrelated(conn_db, err, "file_vrub", "ls_id, id", "file_kvar", "id", "ls_id", "id", "gil_id", "Уникальный код гражданина", "Номер ЛС", Points.Pref.ToString(), "временно убывшие без квартир");
                    #endregion Выбираем временно убывших без квартир

                    #region 33. Выбираем перерасчеты начислений по услугам без единиц измерения

                    DeleteOneUnrelated(conn_db, err, "file_servp", "ls_id, id", "file_measures", "id_measure, id", "nzp_measure", "id_measure", "reval_month", "Дата перерасчета", "Уникальный код услуги", Points.Pref.ToString(), "перерасчеты начислений по услугам без единиц измерения");
                    #endregion Выбираем перерасчеты начислений по услугам без единиц измерения

                    #region 32. Выбираем перерасчеты начислений по услугам без услуг

                    DeleteOneUnrelated(conn_db, err, "file_servp", "ls_id, id", "file_services", "id_serv,  id", "nzp_serv", "id_serv", "reval_month", "Дата перерасчета", "Уникальный код услуги", Points.Pref.ToString(), "перерасчеты начислений по услугам без услуг");
                    #endregion Выбираем перерасчеты начислений по услугам без услуг

                    #region 18. Выбираем недопоставки без типа недопоставки

                    DeleteOneUnrelated(conn_db, err, "file_nedopost", "type_ned, id", "file_typenedopost", "type_ned, id", "type_ned", "type_ned", "dat_nedstart",
                                                            "Дата начала недопоставки", "Тип недопоставки", Points.Pref.ToString(), "недопоставки без типа недопоставки");
                    #endregion Выбираем недопоставки без типа недопоставки

                    #region 17. Выбираем недопоставки без услуги

                    DeleteOneUnrelated(conn_db, err, "file_nedopost", "type_ned, id", "file_serv", "ls_id, nzp_serv, nzp_measure, id", "id_serv", "nzp_serv", "type_ned", "Тип недопоставки", "Код услуги", Points.Pref.ToString(), "недопоставки без услуг");
                    #endregion Выбираем недопоставки без услуги

                    #region 16. Выбираем недопоставки без квартир

                    DeleteOneUnrelated(conn_db, err, "file_nedopost", "type_ned, id", "file_kvar", "id", "ls_id", "id", "type_ned", "Тип недопоставки", "Номер ЛС", Points.Pref.ToString(), "недопоставки без квартир");
                    #endregion Выбираем недопоставки без квартир

                    #region 31. Выбираем услуги ЛС без поставщика

                    DeleteOneUnrelated(conn_db, err, "file_servls", "ls_id, id", "file_supp", "supp_id, id", "supp_id", "supp_id", "id_serv", "Код услуги", "Код поставщика", Points.Pref.ToString(), "услуги ЛС без поставщика");
                    #endregion Выбираем услуги ЛС без поставщика

                    #region 30. Выбираем услуги ЛС без ЛС

                    DeleteOneUnrelated(conn_db, err, "file_servls", "ls_id, id", "file_kvar", "id", "ls_id", "id", "id_serv", "Код услуги", "Номер ЛС", Points.Pref.ToString(), "услуги ЛС без ЛС");
                    #endregion Выбираем услуги ЛС без ЛС

                    if (err.Length != 0)
                    {
                        MonitorLog.WriteLog("Ошибка выполнения процедуры DeleteUnrelatedInfo : имеются несвязанные данные, которые не удалились в выделенных файлах"
                            + Environment.NewLine + err.ToString(), MonitorLog.typelog.Error, true);
                        ret.result = false;
                        ret.text = "Ошибка выполнения процедуры DeleteUnrelatedInfo : имеются несвязанные данные, которые не удалились в выделенных файлах.";
                    }
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры DeleteUnrelatedInfo : " + ex.Message, MonitorLog.typelog.Error, true);
                ret.result = false;
                ret.text = "Ошибка выполнения процедуры DeleteUnrelatedInfo : " + ex.Message;
            }

            return ret;
        }

        //удаление несвязной информации из одной таблицы 
        private Returns DeleteOneUnrelated(IDbConnection conn_db, StringBuilder err, string doch_tbl, string doch_field_for_index, string rodit_tbl, string rodit_field_for_index,
                                        string doch_field_relation, string rodit_field_relation, string doch_field_log, string field1_name, string feild2_name, string pref, string errMessage)
        {
            Returns ret = new Returns();

            try
            {
                string sql = "";

                sql = "select rl.nzp_file as nzp_file, fi.loaded_name as loaded_name from " + Points.Pref + "_data" + tableDelimiter + "rust_load rl, " + Points.Pref + "_data" + tableDelimiter + "files_imported fi" +
                    " where rl.nzp_file = fi.nzp_file";
                var dtFiles = ClassDBUtils.OpenSQL(sql, conn_db);
                foreach (DataRow r in dtFiles.resultData.Rows)
                {
                    #region удаление данных
                    //todo Postgres

                    //                    if (rodit_tbl.Trim() == "file_ipu" || doch_tbl.Trim() == "file_ipu")
                    //                    {
                    //                        sql = "delete from " + pref.Trim() + "_data" + tableDelimiter + "" + doch_tbl.Trim() +
                    //                                                        " where nzp_file =" + r["nzp_file"]
                    //                                                        + " and " + doch_field_relation.Trim() + " is not null and " +
                    //                                                        " not exists (select b." + rodit_field_relation.Trim() + " from " + pref.Trim() + "_data" + tableDelimiter + "" + rodit_tbl.Trim() + " b" +
                    //#if PG
                    //                                                        " where cast (b." + rodit_field_relation.Trim() + " as numeric(14,0)) = cast( " + doch_field_relation.Trim() + " as numeric(14,0)) " +
                    //#else
                    // " where cast (b." + rodit_field_relation.Trim() + " as decimal(14,0)) = cast( " + doch_field_relation.Trim() + " as decimal(14,0)) " +
                    //#endif
                    // " and b.nzp_file = " + r["nzp_file"] + ")";
                    //                    }
                    //                    else
                    //                    {
                    sql = "delete from " + pref.Trim() + "_data" + tableDelimiter + "" + doch_tbl.Trim() +
                                                     " where nzp_file =" + r["nzp_file"] +
                                                     " and " + doch_field_relation.Trim() + " is not null and " +
                                                     " not exists (select b." + rodit_field_relation.Trim() + " from " + pref.Trim() + "_data" + tableDelimiter + "" + rodit_tbl.Trim() + " b" +
                                                     " where b." + rodit_field_relation.Trim() + " = " + doch_field_relation.Trim() +
                                                     " and b.nzp_file = " + r["nzp_file"] + ")";
                    //}
                    ret = ExecSQL(conn_db, sql, true);
                    #endregion

                    #region новая проверка
                    if (rodit_tbl.Trim() == "file_ipu" || doch_tbl.Trim() == "file_ipu")
                    {
                        sql = "select a." + doch_field_log.Trim() + " as field1, a." + doch_field_relation.Trim() + " as field2 from " + pref.Trim() + "_data" + tableDelimiter + "" + doch_tbl.Trim() + " a " +
                                                        " where a.nzp_file =" + r["nzp_file"] +
                                                        " and a." + doch_field_relation.Trim() + " is not null and " +
                                                        " not exists (select b." + rodit_field_relation.Trim() + " from " + pref.Trim() + "_data" + tableDelimiter + "" + rodit_tbl.Trim() + " b" +
                                                        " where cast (b." + rodit_field_relation.Trim() + " as decimal(14,0)) = cast( a." + doch_field_relation.Trim() + " as decimal(14,0))" +
                                                         " and b.nzp_file = " + r["nzp_file"] + ")";
                    }
                    else
                    {
                        sql = "select a." + doch_field_log.Trim() + " as field1, a." + doch_field_relation.Trim() + " as field2 from " + pref.Trim() + "_data" + tableDelimiter + "" + doch_tbl.Trim() + " a " +
                                                         "where a.nzp_file =" + r["nzp_file"] +
                                                         " and a." + doch_field_relation.Trim() + " is not null and " +
                                                         " not exists (select b." + rodit_field_relation.Trim() + " from " + pref.Trim() + "_data" + tableDelimiter + "" + rodit_tbl.Trim() + " b" +
                                                         " where b." + rodit_field_relation.Trim() + " = a." + doch_field_relation.Trim() +
                                                         " and b.nzp_file = " + r["nzp_file"] + ")";
                    }


                    var dt = ClassDBUtils.OpenSQL(sql, conn_db);
                    if (dt.resultData.Rows.Count > 0)
                    {
                        sql = "update " + Points.Pref + "_data" + tableDelimiter + "file_del_unrel_info set is_success = 0 where nzp_file = " + r["nzp_file"] +
                            " and id = (select max(id) from " + Points.Pref + "_data" + tableDelimiter + "file_del_unrel_info where nzp_file = " + r["nzp_file"] + ")";

                        err.Append("Обнаружена несвязность данных. Имеются " + errMessage + " в количестве " + dt.resultData.Rows.Count + " в файле номер " + r["loaded_name"] + "." + Environment.NewLine);
                        err.Append(String.Format("{0,30}|{1,30}|{2}", field1_name, feild2_name, Environment.NewLine));

                        foreach (DataRow rr in dt.GetData().Rows)
                        {
                            string testMePls = String.Format("{0,30}|{1,30}|{2}", rr["field1"].ToString().Trim(), rr["field2"].ToString().Trim(), Environment.NewLine);
                            err.Append(testMePls);
                        }
                    }
                    #endregion
                }

            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры DeleteOneUnrelated : " + ex.Message, MonitorLog.typelog.Error, true);
            }

            ret.result = true;
            ret.text = "Выполнено.";

            return ret;
        }
        #endregion
        
        public Returns GetUploadExchangeSZ(Finder finder, string file_name)
        {
            Returns ret = Utils.InitReturns();

            ExcelRepClient excelRepDb = new ExcelRepClient();
            if (finder.nzp_user < 0)
            {
                ret = new Returns(false, "Не задан пользователь", -1);
                return ret;
            }

            #region объявление переменных
            IDbConnection conn_db = null;
            IDbConnection conn_web = null;
            StringBuilder sql = new StringBuilder();
            string name_table = "";


            #endregion




            #region  получение префиксов
            List<_Point> prefixs = new List<_Point>();
            _Point point = new _Point();
            if (finder.pref != "")
            {
                point.pref = finder.pref;
                prefixs.Add(point);
            }
            else
            {
                prefixs = Points.PointList;
            }
            #endregion

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

                #region определение центрального пользователя
                DbWorkUser db = new DbWorkUser();
                Finder f_user = new Finder();
                f_user.nzp_user = finder.nzp_user;
                int nzpUser = db.GetLocalUser(conn_db, f_user, out ret);
                db.Close();
                if (!ret.result)
                {
                    return ret;
                }
                #endregion

#if PG
                name_table = pgDefaultDb + ".t" + finder.nzp_user + "_exchange_sz"; //получаем имя таблички на web 
#else
                name_table = conn_web.Database + "@" + DBManager.getServer(conn_web) + ":t" + finder.nzp_user + "_exchange_sz"; //получаем имя таблички на web 
#endif
                sql.Remove(0, sql.Length);
#if PG
                sql.Append("insert into " + Points.Pref + "_data.tula_ex_sz (file_name,dat_upload, nzp_user) values ('" + file_name + "', now()," + nzpUser + ")");
#else
                sql.Append("insert into " + Points.Pref + "_data@" + DBManager.getServer(conn_db) + ":tula_ex_sz (file_name,dat_upload, nzp_user) values ('" + file_name + "', '" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "'," + nzpUser + ")");
#endif
                if (!ExecSQL(conn_db, sql.ToString(), true).result)
                {
                    MonitorLog.WriteLog("GetUploadExchangeSZ: Ошибка записи в таблицу tula_ex_sz, sql: " + sql.ToString(), MonitorLog.typelog.Error, true);
                    ret.text = "Ошибка записи в реестр загрузок";
                    ret.result = false;
                    return ret;
                }
                int nzp_ex_sz = GetSerialValue(conn_db); //получили номер реестра для загрузки

                
            
                //переносим даннные из таблички с web
                sql.Remove(0, sql.Length);
#if PG
                ExecSQL(conn_db, "set search_path to 'public'", true);
                sql.Append("  insert into  " + Points.Pref + "_data.tula_ex_sz_file ");
#else
                sql.Append("  insert into  " + Points.Pref + "_data@" + DBManager.getServer(conn_db) + ":tula_ex_sz_file ");
#endif
                sql.Append("(famil, imja, otch, drog, strahnm, nasp, nylic, ndom, nkorp, nkw, nkomn, kolk, lchet, ");
                sql.Append("  vidgf, privat, opl, otpl, otplj, kolzr, kolpr, prz, prn, prk, gku1, tarif1, sum1, fakt1, org1, vidtar1, koef1, ");
                sql.Append("  lchet1, sumz1, klmz1, ozs1, sumozs1, gku2, tarif2, sum2, fakt2, org2, vidtar2, koef2, lchet2, sumz2, klmz2, ozs2, ");
                sql.Append("  sumozs2, gku3, tarif3, sum3, fakt3, org3, vidtar3, koef3, lchet3, sumz3, klmz3, ozs3, sumozs3, gku4, tarif4, ");
                sql.Append("  sum4, fakt4, org4, vidtar4, koef4, lchet4, sumz4, klmz4, ozs4, sumozs4, gku5, tarif5, sum5, fakt5, org5,  ");
                sql.Append("  vidtar5, koef5, lchet5, sumz5, klmz5, ozs5, sumozs5, gku6, tarif6, sum6, fakt6, org6, vidtar6, koef6, lchet6, ");
                sql.Append("  sumz6, klmz6, ozs6, sumozs6, gku7, tarif7, sum7, fakt7, org7, vidtar7, koef7, lchet7, sumz7, klmz7, ozs7, ");
                sql.Append("  sumozs7, gku8, tarif8, sum8, fakt8, org8, vidtar8, koef8, lchet8, sumz8, klmz8, ozs8, sumozs8, gku9, tarif9,  ");
                sql.Append("  sum9, fakt9, org9, vidtar9, koef9, lchet9, sumz9, klmz9, ozs9, sumozs9, gku10, tarif10, sum10, fakt10, org10, ");
                sql.Append("  vidtar10, koef10, lchet10, sumz10, klmz10, ozs10, sumozs10, gku11, tarif11, sum11, fakt11, org11, vidtar11, ");
                sql.Append("  koef11, lchet11, sumz11, klmz11, ozs11, sumozs11, gku12, tarif12, sum12, fakt12, org12, vidtar12, koef12, ");
                sql.Append("  lchet12, sumz12, klmz12, ozs12, sumozs12, gku13, tarif13, sum13, fakt13, org13, vidtar13, koef13, lchet13, ");
                sql.Append("  sumz13, klmz13, ozs13, sumozs13, gku14, tarif14, sum14, fakt14, org14, vidtar14, koef14, lchet14, sumz14,");
                sql.Append("  klmz14, ozs14, sumozs14, gku15, tarif15, sum15, fakt15, org15, vidtar15, koef15, lchet15, sumz15, klmz15, ");
                sql.Append("  ozs15, sumozs15, nzp_ex_sz) ");
                sql.Append("  select  famil, imja, otch, drog, strahnm, nasp, nylic, ndom, nkorp, nkw, nkomn, kolk, lchet, vidgf, privat, opl, ");
                sql.Append("  otpl, otplj, kolzr, kolpr, prz, prn, prk, gku1, tarif1, sum1, fakt1, org1, vidtar1, koef1, lchet1, sumz1, klmz1,  ");
                sql.Append("  ozs1, sumozs1, gku2, tarif2, sum2, fakt2, org2, vidtar2, koef2, lchet2, sumz2, klmz2, ozs2, sumozs2, gku3, tarif3, ");
                sql.Append("  sum3, fakt3, org3, vidtar3, koef3, lchet3, sumz3, klmz3, ozs3, sumozs3, gku4, tarif4, sum4, fakt4, org4, vidtar4, ");
                sql.Append("  koef4, lchet4, sumz4, klmz4, ozs4, sumozs4, gku5, tarif5, sum5, fakt5, org5, vidtar5, koef5, lchet5, sumz5, klmz5,");
                sql.Append("  ozs5, sumozs5, gku6, tarif6, sum6, fakt6, org6, vidtar6, koef6, lchet6, sumz6, klmz6, ozs6, sumozs6, gku7, tarif7, ");
                sql.Append("  sum7, fakt7, org7, vidtar7, koef7, lchet7, sumz7, klmz7, ozs7, sumozs7, gku8, tarif8, sum8, fakt8, org8, vidtar8, ");
                sql.Append("  koef8, lchet8, sumz8, klmz8, ozs8, sumozs8, gku9, tarif9, sum9, fakt9, org9, vidtar9, koef9, lchet9, sumz9, klmz9, ");
                sql.Append("  ozs9, sumozs9, gku10, tarif10, sum10, fakt10, org10, vidtar10, koef10, lchet10, sumz10, klmz10, ozs10, sumozs10, ");
                sql.Append("  gku11, tarif11, sum11, fakt11, org11, vidtar11, koef11, lchet11, sumz11, klmz11, ozs11, sumozs11, gku12, tarif12, ");
                sql.Append("  sum12, fakt12, org12, vidtar12, koef12, lchet12, sumz12, klmz12, ozs12, sumozs12, gku13, tarif13, sum13, fakt13,  ");
                sql.Append("  org13, vidtar13, koef13, lchet13, sumz13, klmz13, ozs13, sumozs13, gku14, tarif14, sum14, fakt14, org14, vidtar14, ");
                sql.Append("  koef14, lchet14, sumz14, klmz14, ozs14, sumozs14, gku15, tarif15, sum15, fakt15, org15, vidtar15, koef15, lchet15, ");
                sql.Append("  sumz15, klmz15, ozs15, sumozs15, " + nzp_ex_sz + " from " + name_table + " ");

                if (!ExecSQL(conn_db, sql.ToString(), true).result)
                {
                    MonitorLog.WriteLog("GetUploadExchangeSZ: Ошибка записи в таблицу tula_ex_sz_file, sql: " + sql.ToString(), MonitorLog.typelog.Error, true);
                    ret.text = "Ошибка при записи данных из файла в систему";
                    ret.result = false;
                    return ret;
                }

#if PG
                ExecSQL(conn_db, " create index ix_ex_1 on " + Points.Pref + "_data.tula_ex_sz_file(lchet);", false);
                ExecSQL(conn_db, " analyze  " + Points.Pref + "_data.tula_ex_sz_file; ", false);
#else
                ExecSQL(conn_db, " create index ix_ex_1 on " + Points.Pref + "_data@" + DBManager.getServer(conn_db) + ":tula_ex_sz_file(lchet);", false);
                ExecSQL(conn_db, " update statistics for table  " + Points.Pref + "_data@" + DBManager.getServer(conn_db) + ":tula_ex_sz_file; ", false);
#endif

                const string tPrm2004 = "t_prm_2004";
                ExecSQL(conn_db, "drop table " + tPrm2004, false);
                ret = ExecSQL(conn_db,
                    "create temp table " + tPrm2004 + " (stack integer, nzp_kvar integer, val_prm numeric(20,0)) " + sUnlogTempTable,
                    true);
                if (!ret.result) return ret;
                ExecSQL(conn_db, " create index ix_" + tPrm2004 + "_2 on " + tPrm2004 + "(stack, nzp_kvar, val_prm)", true);

                int iterator=0;
                double proc=0;
                //сопоставляем с номером лс в системе 
                foreach (var points in prefixs)
                {                   
                    ret = ExecSQL(conn_db, "delete from " + tPrm2004, true);
                    if (!ret.result) return ret;

                    sql = new StringBuilder();
                    sql.Append("insert into " + tPrm2004 + " (stack, nzp_kvar, val_prm) ");
                    sql.Append(" select distinct 1, p1.nzp, cast(replace(p1.val_prm,' ','') as " + sDecimalType + ") from " +
                               points.pref + "_data" + tableDelimiter + "prm_1 p1 ");
                    sql.Append(" where p1.nzp_prm = 2004 and p1.is_actual <> 100 and " +
                               MDY(Points.GetCalcMonth(new CalcMonthParams(points.pref)).month_, 1,
                                   Points.GetCalcMonth(new CalcMonthParams(points.pref)).year_) +
                               " between dat_s and dat_po ");
                    ret = ExecSQL(conn_db, sql.ToString(), true);
                    if (!ret.result) return ret;
                    ExecSQL(conn_db, sUpdStat + " " + tPrm2004, false);

                    sql = new StringBuilder();
                    sql.Append(" delete from " + tPrm2004 + " where val_prm in (select val_prm from " + tPrm2004 + " group by val_prm having count(nzp_kvar)>1 ) ");
                    ret = ExecSQL(conn_db, sql.ToString(), true);
                    if (!ret.result) return ret;

                    ExecSQL(conn_db, sUpdStat + " " + tPrm2004, false);


                    sql = new StringBuilder();
                    sql.Append(" update  " + Points.Pref + "_data" + tableDelimiter + "tula_ex_sz_file set nzp_kvar = (");
                    sql.Append(" select nzp_kvar from " + tPrm2004 + " where val_prm = cast(replace(" + Points.Pref +
                               "_data" + tableDelimiter + "tula_ex_sz_file.lchet,' ','') as " + sDecimalType + "))");
                    sql.Append(" where lchet not like'%-%' and lchet is not null and nzp_kvar is null and nzp_ex_sz = " + nzp_ex_sz);

                    ret = ExecSQL(conn_db, sql.ToString(), true);
                    if (!ret.result)
                    {
                        MonitorLog.WriteLog(
                            "GetUploadExchangeSZ: Ошибка обновления таблицы tula_ex_sz_file, sql: " + sql.ToString(),
                            MonitorLog.typelog.Error, true);
                        ret.text = "Ошибка при соспоставлении номеров лицевых счетов\n" + ret.text;
                        return ret;
                    }

                    iterator++;
                    if((Convert.ToDouble(iterator)/Convert.ToDouble(prefixs.Count))>1d/prefixs.Count)
                    {
                        iterator = 0;
                        proc += 1d / prefixs.Count;
                        ExecSQL(conn_db, "update " + Points.Pref + "_data@" + DBManager.getServer(conn_db) + ":tula_ex_sz set proc="+proc+" where nzp_ex_sz=" + nzp_ex_sz, false);
                    }

                }
                ExecSQL(conn_db, "update " + Points.Pref + "_data@" + DBManager.getServer(conn_db) + ":tula_ex_sz set proc=1 where nzp_ex_sz=" + nzp_ex_sz, false);
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("GetUploadExchangeSZ: Ошибка записи данных в систему: " + ex.Message, MonitorLog.typelog.Error, true);
                ret.text = "Ошибка записи данных в систему";
                ret.result = false;
                return ret;
            }
        
            return ret;
        }

        #region удаление файлов обмена
        public Returns DeleteFromExchangeSZ(Finder finder, int nzp_ex_sz)
        {
            Returns ret = Utils.InitReturns();
            if (finder.nzp_user < 0)
            {
                ret = new Returns(false, "Не задан пользователь", -1);
                ret.result = false;
                return ret;
            }

            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return ret;

            StringBuilder sql = new StringBuilder();

            sql.Remove(0, sql.Length);
            sql.Append(" delete from " + Points.Pref + "_data" + tableDelimiter + "tula_ex_sz_file where nzp_ex_sz=" + nzp_ex_sz + "");
            ret = ExecSQL(conn_db, sql.ToString(), true);
            if (!ret.result)
            {
                return ret;
            }

            sql.Remove(0, sql.Length);
            sql.Append(" delete from " + Points.Pref + "_data" + tableDelimiter + "tula_ex_sz where nzp_ex_sz=" + nzp_ex_sz + "");
            ret = ExecSQL(conn_db, sql.ToString(), true);
            if (!ret.result)
            {
                return ret;
            }


            return ret;
        }
        #endregion

    }
}
