using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.IO;
using System.Text;
using SevenZip;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;

namespace STCLINE.KP50.DataBase
{
    public class DbUES : DbAdminClient
    {
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
                    string sql = " INSERT INTO " + Points.Pref + "_data.  files_imported (nzp_file, nzp_version, loaded_name, saved_name, nzp_status, created_by, created_on, file_type) ";
                    sql += " VALUES (default," + nzp_version + ",'" + finder.loaded_name + "',\'" + finder.saved_name + "\',2," + localUSer + ",now(), 2)  ";
#else
                string sql = " INSERT INTO " + Points.Pref + "_data" + tableDelimiter + "  files_imported (nzp_file, nzp_version, loaded_name, saved_name, nzp_status, created_by, created_on, file_type, pref) ";
                sql += " VALUES (0," + nzp_version + ",'" + finder.loaded_name + "',\'" + finder.saved_name + "\',2," + localUSer + ",current, 2, '')  ";
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

                                var sqlComm = "delete from " + bank.pref + "_charge_" + finder.year.Substring(2, 2) + tableDelimiter + "charge_" + finder.month + " where nzp_serv = " + nzp_serv; // + " and nzp_kvar = " + row[0];
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
                                //var sqlComm = "delete from " + bank.pref + "_charge_" + finder.year.Substring(2, 2) + tableDelimiter + " charge_" + finder.month + " where nzp_serv = " + nzp_serv + " and nzp_kvar = " + row[0];
                                //ExecSQL(con_db, sqlComm, true);
                                var sqlComm = "update " + bank.pref + "_data" + tableDelimiter + "tarif set is_actual = 100 where nzp_serv = " + nzp_serv + " and num_ls = " + row[0] + " and dat_s <= '01." + finder.month + "." + finder.year + "' and dat_po >= '01." + finder.month + "." + finder.year + "'";
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
                                //var sqlComm = "delete from " + bank.pref + "_charge_" + finder.year.Substring(2, 2) + tableDelimiter + "charge_" + finder.month + " where nzp_serv = " + nzp_serv + " and nzp_kvar = " + row[0];
                                //ExecSQL(con_db, sqlComm, true);
                                var sqlComm = "update " + bank.pref + "_data" + tableDelimiter + "tarif set is_actual = 100 where nzp_serv = " + nzp_serv + " and num_ls = " + row[0] + " and dat_s <= '01." + finder.month + "." + finder.year + "' and dat_po >= '01." + finder.month + "." + finder.year + "'";
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
                                                                                " cast( " +row[3] + "/" + val_prm + " as character(20)), 1, 1 )";
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
                                        " where nzp = " + row[0] + " and nzp_prm = " + nzp_prm + " and is_actual = 1  and dat_s <='28." + finder.month + "." + finder.year + "' and dat_po >'01." + finder.month + "." + finder.year + "'" +
                                              "),0)) as character(20)), '01." + finder.month + "." + finder.year + "', '"
                                                                                + DateTime.DaysInMonth(Convert.ToInt32(finder.year), Convert.ToInt32(finder.month)) + "."
                                                                                + finder.month + "." + finder.year + "') where nzp = " + row[0] + " and nzp_prm = " + nzp_prm + " and is_actual = 1";
#else
 " (" + row[3] + "/" + val_prm.Trim() + " + nvl((select val_prm from " + bank.pref + "_data" + tableDelimiter + "prm_1 " +
                                        " where nzp = " + row[0] + " and nzp_prm = " + nzp_prm + " and is_actual = 1 and dat_s <='28." + finder.month + "." + finder.year + "' and dat_po >'01." + finder.month + "." + finder.year + "'" +
                                              "),0), '01." + finder.month + "." + finder.year + "', '"
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
                                                                                " cast( " +row[3] + "/" + val_prm + " as character(20) + coalesce((select val_prm from " + bank.pref + "_data" + tableDelimiter + "prm_1 " +
                                        " where nzp = " + row[0] + " and nzp_prm = " + nzp_prm + " and is_actual = 1  and dat_s <='28." + finder.month + "." + finder.year + "' and dat_po >'01." + finder.month + "." + finder.year + "'" +
                                              "),0)), 1, 1 )";
#else
 row[3] + "/" + val_prm + " + nvl((select val_prm from " + bank.pref + "_data" + tableDelimiter + "prm_1 " +
                                                                                " where nzp = " + row[0] + " and nzp_prm = " + nzp_prm + " and is_actual = 1 and " +
                                                                                " dat_s <='28." + finder.month + "." + finder.year + "' and dat_po >'01." + finder.month + "." + finder.year + "'),0), 1, 1 )";
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

                    string sql = " UPDATE " + Points.Pref + "_data" + tableDelimiter + "files_imported set nzp_status =  " + (int)FilesImported.Statuses.LoadedWithErrors;
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
        /// Выгрузка начислений УЭС
        /// </summary>
        /// <returns></returns>
        public Returns GenerateUESVigr(out Returns ret, SupgFinder finder)
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

            #region Проверка наличия таблиц
            using (IDbConnection con_db = GetConnection(Constants.cons_Kernel))
            {
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
                        string comStr = "select * from " + bank.pref + "_charge_" + finder.year.Substring(2, 2) + tableDelimiter + "charge_" + finder.month + " where nzp_kvar = -1";
                        var dt = ClassDBUtils.OpenSQL(comStr, con_db);
                    }
                }
                catch (Exception ex)
                {
                    MonitorLog.WriteException("Ошибка выгрузки начислений УЭС(GenerateUESVigr)", ex);
                    ret.result = false;
                    ret.text = "Для выбранной даты невозможна загрузка";
                    ret.tag = -1;
                    return ret;
                }
            }
            #endregion

            string fn2 = "";
            //путь, по которому скачивается файл
            string path = "";
            //Имя файла отчета
            string fileNameIn = "UES_vigr_" + DateTime.Now.Ticks;
            ExcelRep excelRepDb = new ExcelRep();
            StringBuilder sql = new StringBuilder();

            //запись в БД о постановки в поток(статус 0)
            
            ret = excelRepDb.AddMyFile(new ExcelUtility()
            {
                nzp_user = finder.nzp_user,
                status = ExcelUtility.Statuses.InProcess,
                rep_name = "Выгрузка начислений УЭС"
            });
            if (!ret.result) return ret;

            int nzpExc = ret.tag;

            IDbConnection conn_db = null;
            MyDataReader reader = null;
            decimal progress = 0;
            var dir = "FilesExchange\\files\\" + fileNameIn + "\\";
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            var resDir = STCLINE.KP50.Global.Constants.ExcelDir.Replace("/", "\\");
            if (!Directory.Exists(resDir)) Directory.CreateDirectory(resDir);

            var fullPath = AppDomain.CurrentDomain.BaseDirectory;
            OleDbCommand Command = new OleDbCommand();

            try
            {
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

                #region Создание mdb Счета Дотации УЭС
                //создание дубликата пустого mdb файла
                File.Copy("template/blank_table.mdb", dir + "Счета Дотации УЭС.mdb");
                File.Copy("template/blank_table.mdb", dir + "Счета Льготные площади.mdb");
                OleDbConnection Connection = new OleDbConnection();
                var myConnectionString = @"Provider=Microsoft.ACE.OLEDB.12.0;" +
                           "Data Source=" + dir + "Счета Дотации УЭС.mdb;Jet OLEDB:Database Password=password;";
                Connection.ConnectionString = myConnectionString;
                Connection.Open();

                DataTable resTable = new DataTable(), tmpHeaderTable = new DataTable(), tmpServTable = new DataTable();
                string strComm = "";

                //Заголовок
                strComm = "CREATE TABLE [Счета Дотации УЭС] ([Дата расчета] DATETIME,[Счет] DOUBLE, [Код Услуги] INTEGER, [Код льготы] INTEGER, [Вид регистрации] INTEGER, [Сумма] DOUBLE )";
                Command = new OleDbCommand(strComm, Connection);
                Command.ExecuteNonQuery();
                Command.Dispose();

                progress += 20;
                decimal pr = progress / 100;
                excelRepDb.SetMyFileProgress(new ExcelUtility() { nzp_exc = nzpExc, progress = progress / 100 });

                Connection.Close();
                //Connection.Dispose();

                #endregion

                #region Создание mdb Счета Льготные площади УЭС
                //создание дубликата пустого mdb файла
                //File.Copy("template/blank_table.mdb", dir + "Счета Льготные площади.mdb");
                Connection = new OleDbConnection();
                myConnectionString = @"Provider=Microsoft.ACE.OLEDB.12.0;" +
                           "Data Source=" + dir + "Счета Льготные площади.mdb;Jet OLEDB:Database Password=password;";
                Connection.ConnectionString = myConnectionString;
                Connection.Open();

                resTable = new DataTable();
                tmpHeaderTable = new DataTable();
                tmpServTable = new DataTable();
                strComm = "";

                //Заголовок
                strComm = "CREATE TABLE [Счета Льготные площади УЭС] ([Счет] DOUBLE, [Код Услуги] INTEGER, [Код льготы] INTEGER, [Вид регистрации] INTEGER, " +
                          " [Площадь] DOUBLE, [Дата расчета]  DATETIME,[Тариф] DOUBLE )";
                Command = new OleDbCommand(strComm, Connection);
                Command.ExecuteNonQuery();
                Command.Dispose();

                progress += 20;
                excelRepDb.SetMyFileProgress(new ExcelUtility() { nzp_exc = nzpExc, progress = progress / 100 });

                Connection.Close();
                Connection.Dispose();
                #endregion

                #region Создание mdb Счета Корректировки УЭС
                //создание дубликата пустого mdb файла
                File.Copy("template/blank_table.mdb", dir + "Счета Корректировки УЭС.mdb");
                Connection = new OleDbConnection();
                myConnectionString = @"Provider=Microsoft.ACE.OLEDB.12.0;" +
                           "Data Source=" + dir + "Счета Корректировки УЭС.mdb;Jet OLEDB:Database Password=password;";
                Connection.ConnectionString = myConnectionString;
                Connection.Open();

                resTable = new DataTable();
                tmpHeaderTable = new DataTable();
                tmpServTable = new DataTable();
                strComm = "";

                //запись в файл
                strComm = "CREATE TABLE [Счета Корректировки УЭС] ([Счет] DOUBLE, [Код Услуги] INTEGER, [Дата расчета] DATETIME, [Сумма] DOUBLE, [Вид] TEXT(5), [Код льготы] INTEGER, [Вид регистрации] INTEGER)";
                Command = new OleDbCommand(strComm, Connection);
                Command.ExecuteNonQuery();
                Command.Dispose();


                foreach (var bank in Points.PointList)
                {
                    //string sqlStr = " select nzp_serv, real_charge, nzp_kvar from " + bank.pref + "_charge_" + finder.year.Substring(2, 2) +  tableDelimiter + "charge_" + finder.month + " where nzp_serv = 8 or nzp_serv = 9 ";
                    //var result = ClassDBUtils.OpenSQL(sqlStr, conn_db);
                    //if (result.resultData != null)
                    //{
                    //    resTable = result.resultData;

                    //    foreach (DataRow row in resTable.Rows)
                    //    {
                    //        var nzp_serv = 2;
                    //        if (row["nzp_serv"].ToString().Trim() == "9")
                    //            nzp_serv = 3;
                    //        strComm = "insert into [Счета Корректировки УЭС] ([Счет], [Код услуги] , [Дата расчета], [Сумма], [Вид], [Код льготы], [Вид регистрации]) values " +
                    //                  " (" + row["nzp_kvar"] + ", " + nzp_serv + ", '01." + finder.month + "." + finder.year + "', " + row["real_charge"] + ", 'Н', 0, 0 ) ";
                    //        OleDbCommand cmd_insert = new OleDbCommand(strComm, Connection);
                    //        cmd_insert.ExecuteNonQuery();
                    //        cmd_insert.Dispose();
                    //    }
                    //}

                    // из xxx_charge_13:perekidki
                    string sqlStr1 = " select distinct nzp_serv, sum_rcl, nzp_kvar from " + bank.pref + "_charge_" + finder.year.Substring(2, 2) + tableDelimiter + "perekidka where nzp_serv in (8,9)" +
                        " and type_rcl = 1 and month_ = " + finder.month;
                    var result1 = ClassDBUtils.OpenSQL(sqlStr1, conn_db);
                    if (result1.resultData != null)
                    {
                        resTable = result1.resultData;

                        foreach (DataRow row in resTable.Rows)
                        {
                            var nzp_serv = 2;
                            if (row["nzp_serv"].ToString().Trim() == "9")
                                nzp_serv = 3;
                            strComm = "insert into [Счета Корректировки УЭС] ([Счет], [Код услуги] , [Дата расчета], [Сумма], [Вид], [Код льготы], [Вид регистрации]) values " +
                                      " (" + row["nzp_kvar"] + ", " + nzp_serv + ", '01." + finder.month + "." + finder.year + "', " + row["sum_rcl"] + ", 'Н', 0, 0 ) ";
                            OleDbCommand cmd_insert = new OleDbCommand(strComm, Connection);
                            cmd_insert.ExecuteNonQuery();
                            cmd_insert.Dispose();
                        }
                    }

                    progress += 20 / Points.PointList.Count;
                    excelRepDb.SetMyFileProgress(new ExcelUtility() { nzp_exc = nzpExc, progress = progress / 100 });
                }

                Connection.Close();
                Connection.Dispose();
                #endregion

                #region Создание mdb Счета Начисления УЭС
                //создание дубликата пустого mdb файла
                File.Copy("template/blank_table.mdb", dir + "Счета Начисления УЭС.mdb");
                Connection = new OleDbConnection();
                myConnectionString = @"Provider=Microsoft.ACE.OLEDB.12.0;" +
                           "Data Source=" + dir + "Счета Начисления УЭС.mdb;Jet OLEDB:Database Password=password;";
                Connection.ConnectionString = myConnectionString;
                Connection.Open();

                resTable = new DataTable();
                tmpHeaderTable = new DataTable();
                tmpServTable = new DataTable();
                strComm = "";

                //Заголовок
                strComm = "CREATE TABLE [Счета Начисления УЭС] ([Счет] DOUBLE, [Код Услуги] INTEGER, [Дата расчета] DATETIME, [Сумма] DOUBLE) ";
                Command = new OleDbCommand(strComm, Connection);
                Command.ExecuteNonQuery();
                Command.Dispose();

                foreach (var bank in Points.PointList)
                {
                    string sqlStr = " select nzp_serv, sum_tarif, nzp_kvar from " + bank.pref + "_charge_" + finder.year.Substring(2, 2) + tableDelimiter + "charge_" + finder.month + " where nzp_serv in(8, 9) and sum_tarif <> 0";
                    var result = ClassDBUtils.OpenSQL(sqlStr, conn_db);
                    if (result.resultData != null)
                    {
                        resTable = result.resultData;

                        foreach (DataRow row in resTable.Rows)
                        {
                            var nzp_serv = 2;
                            if (row["nzp_serv"].ToString().Trim() == "9")
                                nzp_serv = 3;
                            strComm = "insert into [Счета Начисления УЭС] ([Счет], [Код услуги] , [Дата расчета], [Сумма]) values " +
                                      " (" + row["nzp_kvar"] + ", " + nzp_serv + ", '01." + finder.month + "." + finder.year + "', " + row["sum_tarif"] + " ) ";
                            OleDbCommand cmd_insert = new OleDbCommand(strComm, Connection);
                            cmd_insert.ExecuteNonQuery();
                            cmd_insert.Dispose();
                        }
                    }

                    progress += 20 / Points.PointList.Count;
                    excelRepDb.SetMyFileProgress(new ExcelUtility() { nzp_exc = nzpExc, progress = progress / 100 });
                }

                Connection.Close();
                Connection.Dispose();
                #endregion

                #region Создание mdb Счета Превышения УЭС
                //создание дубликата пустого mdb файла
                File.Copy("template/blank_table.mdb", dir + "Счета Превышения УЭС.mdb");
                Connection = new OleDbConnection();
                myConnectionString = @"Provider=Microsoft.ACE.OLEDB.12.0;" +
                           "Data Source=" + dir + "Счета Превышения УЭС.mdb;Jet OLEDB:Database Password=password;";
                Connection.ConnectionString = myConnectionString;
                Connection.Open();

                resTable = new DataTable();
                tmpHeaderTable = new DataTable();
                tmpServTable = new DataTable();
                strComm = "";

                //Заголовок
                strComm = "CREATE TABLE [Счета Превышения УЭС] ([Счет] DOUBLE, [Код Услуги] INTEGER, [Дата расчета] DATETIME, [Сумма] DOUBLE) ";
                Command = new OleDbCommand(strComm, Connection);
                Command.ExecuteNonQuery();
                Command.Dispose();

                foreach (var bank in Points.PointList)
                {
                    //string sqlStr = " select nzp_serv, reval, nzp_kvar from " + bank.pref + "_charge_" + finder.year.Substring(2, 2) +  tableDelimiter + "charge_" + finder.month + " where nzp_serv = 8 or nzp_serv = 9 ";
                    //var result = ClassDBUtils.OpenSQL(sqlStr, conn_db);
                    //if (result.resultData != null)
                    //{
                    //    resTable = result.resultData;

                    //    foreach (DataRow row in resTable.Rows)
                    //    {
                    //        var nzp_serv = 2;
                    //        if (row["nzp_serv"].ToString().Trim() == "9")
                    //            nzp_serv = 3;
                    //        strComm = "insert into [Счета Превышения УЭС] ([Счет], [Код услуги] , [Дата расчета], [Сумма]) values " +
                    //                  " (" + row["nzp_kvar"] + ", " + nzp_serv + ", '01." + finder.month + "." + finder.year + "', " + row["reval"] + " ) ";
                    //        OleDbCommand cmd_insert = new OleDbCommand(strComm, Connection);
                    //        cmd_insert.ExecuteNonQuery();
                    //        cmd_insert.Dispose();
                    //    }
                    //}

                    string sqlStr1 = " select distinct nzp_serv, sum_rcl, nzp_kvar from " + bank.pref + "_charge_" + finder.year.Substring(2, 2) + tableDelimiter + "perekidka where nzp_serv in (8,9) " +
                        " and type_rcl = 2 and month_ = " + finder.month;
                    var result1 = ClassDBUtils.OpenSQL(sqlStr1, conn_db);
                    if (result1.resultData != null)
                    {
                        resTable = result1.resultData;

                        foreach (DataRow row in resTable.Rows)
                        {
                            var nzp_serv = 2;
                            if (row["nzp_serv"].ToString().Trim() == "9")
                                nzp_serv = 3;
                            strComm = "insert into [Счета Превышения УЭС] ([Счет], [Код услуги] , [Дата расчета], [Сумма]) values " +
                                      " (" + row["nzp_kvar"] + ", " + nzp_serv + ", '01." + finder.month + "." + finder.year + "', " + row["sum_rcl"] + " ) ";
                            OleDbCommand cmd_insert = new OleDbCommand(strComm, Connection);
                            cmd_insert.ExecuteNonQuery();
                            cmd_insert.Dispose();
                        }
                    }

                    progress += 20 / Points.PointList.Count;
                    excelRepDb.SetMyFileProgress(new ExcelUtility() { nzp_exc = nzpExc, progress = progress / 100 });
                }

                Connection.Close();
                Connection.Dispose();
                #endregion

                conn_web.Close();

                #region архивация
                SevenZipCompressor file = new SevenZipCompressor();
                file.EncryptHeaders = true;
                file.CompressionMethod = SevenZip.CompressionMethod.BZip2;
                file.DefaultItemName = fileNameIn;
                file.CompressionLevel = SevenZip.CompressionLevel.Normal;

                file.CompressDirectory(fullPath + dir, fullPath + dir.Substring(0, dir.Length - 1) + ".7z");
                #endregion

                //перенос файла на клиент
                File.Copy(dir.Substring(0, dir.Length - 1) + ".7z", resDir + fileNameIn + ".7z");
                if (InputOutput.useFtp) fn2 = InputOutput.SaveInputFile(fullPath + dir.Substring(0, dir.Length - 1) + ".7z");
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка в функции GenerateUESVigr:\n" + ex.Message, MonitorLog.typelog.Error, true);
            }
            finally
            {
                Directory.Delete(dir, true);

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
    }
}
