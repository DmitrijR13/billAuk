using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.Globalization;
using System.Linq;
using System.Runtime.Remoting.Services;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Npgsql;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;
using STCLINE.KP50.Utility;
using System.IO;

namespace STCLINE.KP50.DataBase
{
    public partial class DbAdmin: DbAdminClient
    {
        protected IDbConnection Connection { get; set; }

        private List<BankDB> listBanks = new List<BankDB>();

        private List<string> _commentListErr;
        private List<string> _commentListSuc;

        private List<string> _commeListNotNullErr;
        private List<string> _commeListNotNullSuc;

        private string _localPref;
        
        /// <summary>
        /// Добавление первичных ключей
        /// </summary>
        /// <param name="nzpUser"> Код пользователя </param>
        public Returns AddPrimaryKey(int nzpUser)
        {
            OpenConnection();
            var ret = Utils.InitReturns();
            var commListErr = new List<string>();
            var commListSuc = new List<string>();
            IDataReader reader;
            try
            {
                //удаление временных таблиц
                string sql =
                    " SELECT table_schema, table_name FROM information_schema.tables WHERE table_name like 't%_%'";

                ExecRead(out reader, sql);

                while (reader.Read())
                {
                    Match isMatch = Regex.Match(reader["table_name"].ToString(), "t[0-9]+_*", RegexOptions.IgnoreCase);
                    //Match isMatch1 = Regex.Match(reader["table_name"].ToString(), "t[0-9][0-9]_*",
                    //    RegexOptions.IgnoreCase);
                    //Match isMatch2 = Regex.Match(reader["table_name"].ToString(), "t[0-9][0-9][0-9]_*",
                    //    RegexOptions.IgnoreCase);
                    //Match isMatch3 = Regex.Match(reader["table_name"].ToString(), "t[0-9][0-9][0-9][0-9]_*",
                    //    RegexOptions.IgnoreCase);
                    if (isMatch.Success)// || isMatch1.Success || isMatch2.Success || isMatch3.Success)
                    {
                        sql = " DROP TABLE " + reader["table_schema"] + DBManager.tableDelimiter + reader["table_name"] +
                              ";";
                        ExecSQL(sql);
                    }

                }

                //добавление первичных ключей у таблиц с serial
                sql =
                    " SELECT DISTINCT 'ALTER TABLE '||c.table_schema||'.'||trim(c.table_name)||' ADD PRIMARY KEY('||TRIM(c.column_name)||')' AS add_key," +
                    " c.table_schema as schema, c.table_name as table, c.column_name as column" +
                    " FROM information_schema.columns c " +
                    " WHERE 1=1 " +
                    " AND c.column_default LIKE 'next%' " +
                    " AND c.table_name NOT IN (SELECT table_name FROM information_schema.table_constraints WHERE constraint_type = 'PRIMARY KEY')";
                /*" AND c.table_schema = t.table_schema " +
                    " AND c.table_name = t.table_name " +
                    " AND t.constraint_type NOT LIKE 'PRIMARY%' " +
                    " ORDER BY 1 ";*/

                ExecRead(out reader, sql);

                while (reader.Read())
                {
                    var result = Utils.InitReturns();

                    ExecSQL(out result, reader["add_key"].ToString());
                    if (!result.result)
                    {
                        commListErr.Add("Ошибка добавления первичного ключа: " + result.sql_error);
                    }
                    else
                    {
                        commListSuc.Add("Добавлен первичный ключ в таблицу " + reader["schema"] + "." +
                            reader["table"] + " на колонку " + reader["column"] + ";");
                    }

                }
                string path = 
                    "protocol_add_pk_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".txt";
                var memstr =
                    new FileStream(Path.Combine(Constants.Directories.ReportDir, path),
                        FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
                var writer = new StreamWriter(memstr, Encoding.GetEncoding(1251));

                if (commListSuc.Count > 0)
                {
                    foreach (var comm in commListSuc)
                    {
                        writer.WriteLine(comm);
                    }
                }
                else
                {

                    if (commListErr.Count > 0)
                    {
                        writer.WriteLine("Нет добавленных первичных ключей.\r\n");
                    }
                    else
                    {
                        writer.WriteLine("В базе данных есть все необходимые первичные ключи.");
                    }
                }

                if (commListErr.Count > 0)
                {
                    writer.WriteLine("\r\nОшибки при добавлении первичных ключей");

                    foreach (var comment in commListErr)
                    {
                        writer.WriteLine(comment);
                    }
                }

                

                writer.Flush();
                writer.Close();
                memstr.Close();

                

                if (InputOutput.useFtp)
                {
                    path = InputOutput.SaveOutputFile(Path.Combine(Constants.Directories.ReportDir, path));
                }

                var myFile = new DBMyFiles();
                ret = myFile.AddFile(new ExcelUtility
                {
                    nzp_user = nzpUser,
                    status = ExcelUtility.Statuses.InProcess,
                    rep_name = " Протокол добавления первичных ключей ",
                    is_shared = 0,
                    exc_path = path
                });
                if (!ret.result) return ret;
                int nzpExc = ret.tag;
                ret = myFile.SetFileState(new ExcelUtility
                {
                    status = ExcelUtility.Statuses.Success,
                    nzp_exc = nzpExc
                });
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("AddPrimaryKey(): Ошибка выполнения операции в базе данных.\n: " + ex.Message,
                    MonitorLog.typelog.Error, true);
                ret = new Returns(false, "Ошибка при добавлении первичных ключей", -1);
            }
            finally
            {
                CloseConnection();
            }
            return ret;
        }

        /// <summary>
        /// Добавление индексов в таблицы
        /// </summary>
        /// <returns></returns>
        public Returns AddIndexes(int nzpUser, string parametrs)
        {
            OpenConnection();

            _commentListErr = new List<string>();
            _commentListSuc = new List<string>();

            var ret = Utils.InitReturns();
            try
            {
                string pathFile = Directory.GetCurrentDirectory() + @"\Template\indexes.txt";

                if (File.Exists(pathFile)){}
                else{return new Returns(false, "Нет файла с индексами", -1);}

                string[] bankArray;
 
                if (parametrs == null)
                {
                    bankArray = new[] { "central_data", "central_kernel", "local_data", "local_kernel",
                        "central_debt", "central_supg", "central_upload", "central_webfon", 
                        "central_fin", "local_charge", "public" };
                }
                else
                {
                    bankArray = JsonConvert.DeserializeObject<string[]>(parametrs);
                }

                var read = new StreamReader(pathFile);
                    //Path.Combine(Constants.Directories.ReportDir, "indexes.txt"));
                string s = Encryptor.Decrypt(read.ReadLine(), null);
                var banks = JsonConvert.DeserializeObject<List<BankDB>>(s);
                read.Close();

                foreach (var bank in banks)
                {

                    if (!bankArray.Contains(bank.name)) continue;
                    switch (bank.name)
                    {
                        case "central_data":
                            AddCentralBankIndexes(out ret, bank, "_data");
                            break;
                        case "central_kernel":
                            AddCentralBankIndexes(out ret, bank, "_kernel");
                            break;
                        case "central_fin":
                            AddFinBankIndexes(out ret, bank);
                            break;
                        case "central_debt":
                            AddCentralBankIndexes(out ret, bank, "_debt");
                            break;
                        case "central_supg":
                            AddCentralBankIndexes(out ret, bank, "_supg");
                            break;
                        case "central_upload":
                            AddCentralBankIndexes(out ret, bank, "_upload");
                            break;
                        case "central_webfon":
                            AddCentralBankIndexes(out ret, bank, "_webfon");
                            break;
                        case "local_data":
                            AddLocalBankIndexes(out ret, bank, "_data");
                            break;
                        case "local_kernel":
                            AddLocalBankIndexes(out ret, bank, "_kernel");
                            break;
                        case "local_charge":
                            AddChargeBankIndexes(out ret, bank);
                            break;
                        case "public":
                            AddPublicIndexes(out ret, bank);
                            break;
                    }
                }
                string path =
                    "protocol_add_indexes_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".txt";
                var memstr = new FileStream(Path.Combine(Constants.Directories.ReportDir, path),
                                FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
                var writer = new StreamWriter(memstr, Encoding.GetEncoding(1251));

                //запись в протокол информации о добавленных индексах
                if (_commentListSuc.Count > 0)
                {
                    foreach (var comment in _commentListSuc)
                    {
                        writer.WriteLine(comment);
                    }
                }
                else
                {
                    if(_commentListErr.Count == 0)
                    {
                        writer.WriteLine("Нет недостающих индексов.");
                    }
                }

               

                //запись в протокол информации об ошибках при добавлении индексов
                if (_commentListErr.Count > 0)
                {
                    writer.WriteLine("\r\nОшибки при добавлении индексов");
                    foreach (var comment in _commentListErr)
                    {
                        writer.WriteLine(comment);
                    }
                }
               
                writer.Flush();
                writer.Close();
                memstr.Close();
                if (InputOutput.useFtp)
                {
                    path = InputOutput.SaveOutputFile(Path.Combine(Constants.Directories.ReportDir, path));
                }

                var myFile = new DBMyFiles();
                ret = myFile.AddFile(new ExcelUtility
                {
                    nzp_user = nzpUser,
                    status = ExcelUtility.Statuses.InProcess,
                    rep_name = " Протокол добавления индексов ",
                    is_shared = 0,
                    exc_path = path
                });
                if (!ret.result) return ret;
                int nzpExc = ret.tag;
                ret = myFile.SetFileState(new ExcelUtility
                {
                    status = ExcelUtility.Statuses.Success,
                    nzp_exc = nzpExc
                });
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("AddIndexes(): Ошибка\n" + ex, MonitorLog.typelog.Error, true);
                ret = new Returns(false, "Ошибка добавления индексов", -1);
            }
            finally
            {
                CloseConnection();
            }
            return ret;
        }

        /// <summary>
        /// Установка ограничений NOT NULL таблицам БД
        /// </summary>
        /// <param name="nzpUser"></param>
        /// <param name="parametrs"></param>
        /// <returns></returns>
        public Returns AddNotNull(int nzpUser, string parametrs)
        {
            OpenConnection();
            
            _commeListNotNullErr = new List<string>();
            _commeListNotNullSuc = new List<string>();

            var ret = Utils.InitReturns();
            try
            {
                string pathFile = Directory.GetCurrentDirectory() + @"\Template\indexes.txt";

                if (File.Exists(pathFile)) { }
                else { return new Returns(false, "Нет файла с индексами", -1); }

                string[] bankArray;

                if (parametrs == null)
                {
                    bankArray = new[] { "central_data", "central_kernel", "local_data", "local_kernel",
                        "central_debt", "central_supg", "central_upload", "central_webfon", 
                        "central_fin", "local_charge", "public" };
                }
                else
                {
                    bankArray = JsonConvert.DeserializeObject<string[]>(parametrs);
                }

                var read = new StreamReader(pathFile);
                //Path.Combine(Constants.Directories.ReportDir, "indexes.txt"));
                string s = Encryptor.Decrypt(read.ReadLine(), null);
                var banks = JsonConvert.DeserializeObject<List<BankDB>>(s);
                read.Close();

                foreach (var bank in banks)
                {

                    if (!bankArray.Contains(bank.name)) continue;
                    switch (bank.name)
                    {
                        case "central_data":
                            AddSchemaNotNull(out ret, bank, "_data", STypeSchema.Central);
                            break;
                        case "central_kernel":
                            AddSchemaNotNull(out ret, bank, "_kernel", STypeSchema.Central);
                            break;
                        case "central_fin":
                            AddSchemaNotNull(out ret, bank, "_fin", STypeSchema.Central);
                            break;
                        case "central_debt":
                            AddSchemaNotNull(out ret, bank, "_debt", STypeSchema.Central);
                            break;
                        case "central_supg":
                            AddSchemaNotNull(out ret, bank, "_supg", STypeSchema.Central);
                            break;
                        case "central_upload":
                            AddSchemaNotNull(out ret, bank, "_upload", STypeSchema.Central);
                            break;
                        case "central_webfon":
                            AddSchemaNotNull(out ret, bank, "_webfon", STypeSchema.Central);
                            break;
                        case "local_data":
                            AddSchemaNotNull(out ret, bank, "_data", STypeSchema.Local);
                            break;
                        case "local_kernel":
                            AddSchemaNotNull(out ret, bank, "_kernel", STypeSchema.Local);
                            break;
                        case "local_charge":
                            AddSchemaNotNull(out ret, bank, "_charge", STypeSchema.Local);
                            break;
                        case "public":
                            AddSchemaNotNull(out ret, bank, "public", STypeSchema.Local);
                            break;
                    }
                }

                #region Формирование протокола
                string path =
                    "protocol_add_not_null_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".txt";
                var memstr = new FileStream(Path.Combine(Constants.Directories.ReportDir, path),
                                FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
                var writer = new StreamWriter(memstr, Encoding.GetEncoding(1251));

                //запись в протокол информации о добавленных индексах
                
                //запись в протокол информации о добавленных ограничениях NOT NULL
                if (_commeListNotNullSuc.Count > 0)
                {
                    foreach (var comment in _commeListNotNullSuc)
                    {
                        writer.WriteLine(comment);
                    }
                }
                else
                {
                    if (_commeListNotNullErr.Count == 0)
                    {
                        writer.WriteLine("Нет недостающих ограничений NOT NULL.");
                    }
                }

                
                //запись в протокол информации об ошибках при добавлении ограничений NOT NULL
                if (_commeListNotNullErr.Count > 0)
                {
                    writer.WriteLine("\r\nОшибки при добавлении ограничений NOT NULL");
                    foreach (var comment in _commeListNotNullErr)
                    {
                        writer.WriteLine(comment);
                    }
                }
                writer.Flush();
                writer.Close();
                memstr.Close();
                if (InputOutput.useFtp)
                {
                    path = InputOutput.SaveOutputFile(Path.Combine(Constants.Directories.ReportDir, path));
                }

                var myFile = new DBMyFiles();
                ret = myFile.AddFile(new ExcelUtility
                {
                    nzp_user = nzpUser,
                    status = ExcelUtility.Statuses.InProcess,
                    rep_name = " Протокол ограничений NOT NULL ",
                    is_shared = 0,
                    exc_path = path
                });
                if (!ret.result) return ret;
                int nzpExc = ret.tag;
                ret = myFile.SetFileState(new ExcelUtility
                {
                    status = ExcelUtility.Statuses.Success,
                    nzp_exc = nzpExc
                });
                #endregion
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("AddNotNull(): Ошибка\n" + ex, MonitorLog.typelog.Error, true);
                ret = new Returns(false, "Ошибка ограничений NOT NULL", -1);
            }
            finally
            {
                CloseConnection();
            }
            return ret;
        }

        /// <summary>
        /// Добавление внешних ключей
        /// </summary>
        /// <param name="nzpUser"> Код пользователя </param>
        /// <returns></returns>
        public Returns AddForeignKey(int nzpUser)
        {
            var ret = Utils.InitReturns();
            try
            {
                OpenConnection();

                string pathFile = Directory.GetCurrentDirectory() + @"\Template\foreign_key.txt";

                if (File.Exists(pathFile))
                {
                }
                else
                {
                    return new Returns(false, "Нет файла с внешними ключами", -1);
                }

                var read = new StreamReader(pathFile);

                string s = Encryptor.Decrypt(read.ReadLine(), null);
                var listFk = JsonConvert.DeserializeObject<List<ForeignKey>>(s);
                read.Close();

                string sql;
                IDataReader reader;
                string centrPref = Points.Pref;
                var listPref = Points.PointList;
                var commentListErr = new List<string>();
                var commentListSuc = new List<string>();
                foreach (var fKeyFile in listFk)
                {
                    var listFkey = new List<ForeignKey>();

                    if (fKeyFile.tableSchema.IndexOf("charge", StringComparison.Ordinal) > -1 ||
                        fKeyFile.tableSchema.IndexOf("fin", StringComparison.Ordinal) > -1)
                    {
                        #region _charge_XX или _fin_XX

                        string schema = String.Empty;
                        if (fKeyFile.tableSchema.IndexOf("fin", StringComparison.Ordinal) > -1)
                        {
                            schema = "%_fin_%";
                        }
                        else
                        {
                            schema = "%_charge_%";
                        }
                        sql =
                            " SELECT schema_name FROM information_schema.schemata WHERE schema_name like '" + schema + "' ";

                        ExecRead(out reader, sql);
                        while (reader.Read())
                        {
                            var key = new ForeignKey();
                            key.name = fKeyFile.name;
                            key.tableSchema = reader["schema_name"].ToString().Trim();
                            key.tableName = fKeyFile.tableName;
                            key.columnName = fKeyFile.columnName;
                            if (fKeyFile.referencesSchema.IndexOf("localPref", StringComparison.Ordinal) > -1)
                            {
                                int ind = key.tableSchema.IndexOf("_", StringComparison.Ordinal);
                                if (ind == -1) continue;
                                string pref = key.tableSchema.Substring(0, ind);
                                key.referencesSchema = fKeyFile.referencesSchema.Replace("localPref", pref);
                            } else
                            if (fKeyFile.referencesSchema.IndexOf("centrPref", StringComparison.Ordinal) > -1)
                            {
                                key.referencesSchema = fKeyFile.referencesSchema.Replace("centrPref", centrPref);
                            }
                            else
                            {
                                key.referencesSchema = fKeyFile.referencesSchema;
                            }
                            
                            if (fKeyFile.referencesSchema.IndexOf("charge", StringComparison.Ordinal) > -1)
                            {
                                key.referencesSchema = reader["schema_name"].ToString().Trim();
                            } 
                            if (fKeyFile.referencesSchema.IndexOf("fin", StringComparison.Ordinal) > -1)
                            {
                                string fin = reader["schema_name"].ToString().Trim();
                                key.referencesSchema = centrPref + "_fin_" +
                                                       fin.Substring(fin.Length - 2);
                            }
                            
                            key.referencesTable = fKeyFile.referencesTable;
                            key.referencesColumn = fKeyFile.referencesColumn;

                            listFkey.Add(key);
                        }
                        #endregion
                    }
                    else
                    {
                        #region остальные схемы
                        if (fKeyFile.tableSchema.IndexOf("centrPref", StringComparison.Ordinal) > -1)//центральные схемы кроме fin
                        {
                            var key = new ForeignKey();
                            key.name = fKeyFile.name;
                            key.tableSchema = fKeyFile.tableSchema.Replace("centrPref", centrPref);
                            key.tableName = fKeyFile.tableName;
                            key.columnName = fKeyFile.columnName;
                            key.referencesSchema = fKeyFile.referencesSchema.Replace("centrPref", centrPref);
                            key.referencesTable = fKeyFile.referencesTable;
                            key.referencesColumn = fKeyFile.referencesColumn;

                            listFkey.Add(key);
                        }
                        else //локальные data и kernel или public
                        {
                            if (fKeyFile.tableSchema.IndexOf("localPref", StringComparison.Ordinal) > -1)
                                //локальные data и kernel
                            {
                                foreach (var point in listPref)
                                {
                                    var key = new ForeignKey();
                                    key.name = fKeyFile.name;
                                    key.tableSchema = fKeyFile.tableSchema.Replace("localPref", point.pref);
                                    key.tableName = fKeyFile.tableName;
                                    key.columnName = fKeyFile.columnName;
                                    if (fKeyFile.referencesSchema.IndexOf("localPref", StringComparison.Ordinal) > -1)
                                    {
                                        key.referencesSchema = fKeyFile.referencesSchema.Replace("localPref", point.pref);
                                    }
                                    else
                                    {
                                        key.referencesSchema = fKeyFile.referencesSchema.Replace("centrPref", centrPref);
                                    }
                                    key.referencesTable = fKeyFile.referencesTable;
                                    key.referencesColumn = fKeyFile.referencesColumn;

                                    listFkey.Add(key);
                                }
                            }
                            else //public 
                            {
                                var key = new ForeignKey();
                                key.name = fKeyFile.name;
                                key.tableSchema = fKeyFile.tableSchema;
                                key.tableName = fKeyFile.tableName;
                                key.columnName = fKeyFile.columnName;
                                key.referencesSchema = fKeyFile.referencesSchema;
                                key.referencesTable = fKeyFile.referencesTable;
                                key.referencesColumn = fKeyFile.referencesColumn;

                                listFkey.Add(key);
                            }
                        }
                        #endregion
                    }
                    
                    //добавление индексов в БД
                    foreach (var Fkey in listFkey)
                    {
                        
                        sql = " SELECT * FROM information_schema.table_constraints WHERE table_schema = '" + Fkey.tableSchema + "' AND " +
                              " table_name = '" + Fkey.tableName + "' AND constraint_name = '" + Fkey.name + "'";
                        bool flag = false;
                        ExecRead(out reader, sql);
                        if (reader.Read())
                        {
                            flag = true;
                        }
                        if (!flag)
                        {
                            sql = " ALTER TABLE " + Fkey.tableSchema + DBManager.tableDelimiter + Fkey.tableName +
                                  " ADD CONSTRAINT " + Fkey.name + " FOREIGN KEY (" + Fkey.columnName + ") " +
                                  " REFERENCES " + Fkey.referencesSchema + DBManager.tableDelimiter +
                                  Fkey.referencesTable +
                                  " (" + Fkey.referencesColumn + ") " +
                                  " MATCH SIMPLE ON UPDATE NO ACTION ON DELETE NO ACTION ";
                            var result = Utils.InitReturns();
                            ExecSQL(out result, sql);
                            if (!result.result)
                            {
                                commentListErr.Add(result.sql_error);
                            }
                            else
                            {
                                commentListSuc.Add("Добавлен внешний ключ " + Fkey.name + " в таблицу " + 
                                    Fkey.tableSchema + "." + Fkey.tableName + " по полю " + Fkey.columnName + " с таблицей " +
                                    Fkey.referencesSchema + "." + Fkey.referencesTable + "(" + Fkey.referencesColumn + ").");
                            }
                        }
                    }
                }
                //сформировать протокол 
                string path = "protocol_add_fk_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".txt";
                var memstr =
                    new FileStream(Path.Combine(Constants.Directories.ReportDir, path),
                        FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
                var writer = new StreamWriter(memstr, Encoding.GetEncoding(1251));

                if (commentListSuc.Count > 0)
                {
                    foreach (var comm in commentListSuc)
                    {
                        writer.WriteLine(comm);
                    }
                }
                else
                {

                    if (commentListErr.Count > 0)
                    {
                        writer.WriteLine("Нет добавленных внешних ключей.\r\n");
                    }
                    else
                    {
                        writer.WriteLine("В базе данных есть все необходимые внешние ключи.");
                    }
                }

                if (commentListErr.Count > 0)
                {
                    writer.WriteLine("\r\nОшибки при добавлении внешних ключей");

                    foreach (var comment in commentListErr)
                    {
                        writer.WriteLine(comment);
                    }
                }



                writer.Flush();
                writer.Close();
                memstr.Close();

                if (InputOutput.useFtp)
                {
                    path = InputOutput.SaveOutputFile(Path.Combine(Constants.Directories.ReportDir, path));
                }

                //Добавление записи в excel_utility
                var myFile = new DBMyFiles();
                ret = myFile.AddFile(new ExcelUtility
                {
                    nzp_user = nzpUser,
                    status = ExcelUtility.Statuses.InProcess,
                    rep_name = " Протокол добавления внешних ключей ",
                    is_shared = 0,
                    exc_path = path
                });
                if (!ret.result) return ret;
                int nzpExc = ret.tag;
                ret = myFile.SetFileState(new ExcelUtility
                {
                    status = ExcelUtility.Statuses.Success,
                    nzp_exc = nzpExc
                });
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка AddForeignKey()" + ex, MonitorLog.typelog.Error, true);
                return new Returns(false, "Ошибка добавления внешних ключей", -1);
            }
            finally
            {
                CloseConnection();
            }
            return ret;
        }

        
        /// <summary>
        /// Добавление индексов в таблицы центрального банка
        /// </summary>
        /// <param name="ret"></param>
        /// <param name="bank"></param>
        /// <param name="point"></param>
        private void AddCentralBankIndexes(out Returns ret, BankDB bank, string point)
        {
            ret = Utils.InitReturns();
            IDataReader reader;
            string sql;

            foreach(var table in bank.listTables)
            {
                if (TempTableInWebCashe(Points.Pref + point +DBManager.tableDelimiter + table.name))
                {
                    foreach (var index in table.listIndex)
                    {
                        try
                        {
                            bool flag = false;
                            sql = " SELECT indexdef FROM pg_indexes WHERE schemaname = '" + Points.Pref + point +
                                  "' AND tablename = '"
                                  + table.name + "' AND indexname = '" + index.name + "';";
                            ExecRead(out reader, sql);

                            if (reader.Read())
                            {
                                flag = true;
                            }
                            if (!flag)
                            {
                                var result = Utils.InitReturns();
                                sql = index.text.Replace("CentrPref", Points.Pref);
                                sql = sql.Replace("_fin_XX", point);
                                ExecSQL(out result, sql);
                                if (!result.result)
                                {
                                    _commentListErr.Add("Добавить индекс - " + index.name + " не удалось. Ошибка: " +
                                                        result.sql_error);
                                }
                                else
                                {
                                    _commentListSuc.Add("Добавлен индекс: " + index.name + " в таблицу " + Points.Pref + point +
                                       "." + table.name + ";");
                                }
                            }
                        }
                        catch 
                        {
                        }
                    }
                    
                }
            }
        }

        /// <summary>
        /// Добавление индексов в таблицы схемы public
        /// </summary>
        /// <param name="ret"></param>
        /// <param name="bank"></param>
        private void AddPublicIndexes(out Returns ret, BankDB bank)
        {
            ret = Utils.InitReturns();
            IDataReader reader;
            string sql;
            foreach (var table in bank.listTables)
            {
                if (TempTableInWebCashe(DBManager.sDefaultSchema + DBManager.tableDelimiter + table.name))
                {
                    foreach (var index in table.listIndex)
                    {
                        try
                        {
                            bool flag = false;
                            sql = " SELECT indexname FROM pg_indexes WHERE schemaname = 'public'" +
                                  " AND tablename = '"
                                  + table.name + "' AND indexname = '" + index.name + "';";
                            ExecRead(out reader, sql);

                            if (reader.Read())
                            {
                                flag = true;
                            }
                            if (!flag)
                            {
                                var result = Utils.InitReturns();
                                sql = index.text;
                                ExecSQL(out result, sql);
                                if (!result.result)
                                {
                                    _commentListErr.Add("Добавить индекс - " + index.name + " не удалось. Ошибка: " + result.sql_error);
                                }
                                else
                                {
                                    _commentListSuc.Add("Добавлен индекс: " + index.name + " в таблицу public." + table.name + ";");
                                }
                            }
                        }
                        catch 
                        {
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Добавление индексов в таблицы схем _fin_XX
        /// </summary>
        /// <param name="ret"></param>
        /// <param name="bank"></param>
        private void AddFinBankIndexes(out Returns ret, BankDB bank)
        {
            ret = Utils.InitReturns();
            IDataReader reader;

            string sql = "SELECT schema_name FROM information_schema.schemata WHERE schema_name like '" + Points.Pref + "_fin_%'";
            ExecRead(out reader, sql);

            while (reader.Read())
            {
                string schema = reader["schema_name"].ToString().Replace(Points.Pref, "");
                AddCentralBankIndexes(out ret, bank, schema);
            }
        }

        /// <summary>
        /// Добавление индексов в таблицы схем _charge_XX
        /// </summary>
        /// <param name="ret"></param>
        /// <param name="bank"></param>
        private void AddChargeBankIndexes(out Returns ret, BankDB bank)
        {
            ret = Utils.InitReturns();

            {
                ret = Utils.InitReturns();
                string sql;
                IDataReader reader;
                try
                {
                    sql = "SELECT schema_name FROM information_schema.schemata WHERE schema_name like '" + "%_charge_%'";
                    ExecRead(out reader, sql);
                    while (reader.Read())
                    {
                        string pref = reader["schema_name"].ToString().Trim();
                        foreach (var table in bank.listTables)
                        {
                            if (TempTableInWebCashe(pref +  DBManager.tableDelimiter + table.name))
                            {
                                foreach (var index in table.listIndex)
                                {
                                    try
                                    {
                                        IDataReader readerInd;
                                        bool flag = false;
                                        sql = " SELECT indexdef FROM pg_indexes WHERE schemaname = '" + pref + 
                                              "' AND tablename = '"
                                              + table.name + "' AND indexname = '" + index.name + "';";
                                        ExecRead(out readerInd, sql);

                                        if (readerInd.Read())
                                        {
                                            flag = true;
                                        }
                                        if (!flag)
                                        {
                                            var result = Utils.InitReturns();
                                            sql = index.text.Replace("LocalPref_charge_XX", pref);
                                            ExecSQL(out result, sql);
                                            if (!result.result)
                                            {
                                                _commentListErr.Add("Добавить индекс - " + index.name + " не удалось. Ошибка: " + result.sql_error);
                                            }
                                            else
                                            {
                                                _commentListSuc.Add("Добавлен индекс: " + index.name + " в таблицу " + pref +
                                                   "." + table.name + ";");
                                            }
                                        }
                                    }
                                    catch 
                                    {
                                    }
                                }
                            }
                        }
                    }
                }
                catch  { }
            }
        }


        /// <summary>
        /// Добавление индексов в таблицы локальных банков
        /// </summary>
        /// <param name="ret"></param>
        /// <param name="bank"></param>
        /// <param name="point"></param>
        private void AddLocalBankIndexes(out Returns ret, BankDB bank, string point)
        {
            ret = Utils.InitReturns();
            string sql;
            IDataReader reader;
            try
            {
                sql = " SELECT bd_kernel FROM " + Points.Pref + DBManager.sKernelAliasRest + "s_point WHERE nzp_graj > 0";
                ExecRead(out reader, sql);
                while (reader.Read())
                {
                    string pref = reader["bd_kernel"].ToString().Trim();
                    foreach (var table in bank.listTables)
                    {
                        if (TempTableInWebCashe(pref + point + DBManager.tableDelimiter + table.name))
                        {
                            foreach (var index in table.listIndex)
                            {
                                try
                                {
                                    IDataReader readerInd;
                                    bool flag = false;
                                    sql = " SELECT indexdef FROM pg_indexes WHERE schemaname = '" + pref + point +
                                          "' AND tablename = '"
                                          + table.name + "' AND indexname = '" + index.name + "';";
                                    ExecRead(out readerInd, sql);

                                    if (readerInd.Read())
                                    {
                                        flag = true;
                                    }
                                    if (!flag)
                                    {
                                        var result = Utils.InitReturns();
                                        sql = index.text.Replace("LocalPref", pref);
                                        ExecSQL(out result, sql);
                                        if (!result.result)
                                        {
                                            _commentListErr.Add("Добавить индекс - " + index.name + " не удалось. Ошибка: " + result.sql_error);
                                        }
                                        else
                                        {
                                            _commentListSuc.Add("Добавлен индекс: " + index.name + " в таблицу " + pref + point +
                                               "." + table.name + ";");
                                        }

                                    }
                                }
                                catch 
                                {
                                }
                            }

                        }
                    }
                }
            }
            catch  { }
        }


        private void SetNotNull(out Returns ret, string schema, TableDb table)
        {
            ret = Utils.InitReturns();

            try
            {
                foreach (var column in table.ListColumnsNotNull)
                {
                    //проверка на существование колонки
                    string sql = " SELECT count(*) FROM information_schema.columns WHERE table_schema = '" + schema +
                                 "' AND table_name = '" + table.name + "' AND column_name = '" + column + "';";
                    object obj = ExecScalar(sql, out ret);
                    int count = 0;
                    if (obj != null && obj != DBNull.Value)
                    {
                        count = Convert.ToInt32(obj);
                    }
                    else
                    {
                        _commeListNotNullErr.Add("Ошибка при определении наличия поля " + column + " в таблице " +
                                                 schema + "." + table.name + ";");
                    }
                    if (count == 0) continue;

                    //проверка на существование ограничения NOT NULL
                    sql = " SELECT count(*) FROM information_schema.columns WHERE table_schema = '" + schema +
                          "' AND table_name = '"
                          + table.name + "' AND column_name = '" + column +
                          "' AND lower(replace(is_nullable, ' ', '')) = 'no';";
                    obj = ExecScalar(sql, out ret);
                    count = 1;
                    if (obj != null && obj != DBNull.Value)
                    {
                        count = Convert.ToInt32(obj);
                    }
                    else
                    {
                        _commeListNotNullErr.Add("Ошибка при определении наличия ограничения NOT NULL в таблице " +
                                                 schema + "." + table.name + " по полю " + column + ";");
                    }

                    //добавление ограничения
                    if (count == 0)
                    {
                        sql = " ALTER TABLE " + schema + DBManager.tableDelimiter + table.name +
                              " ALTER COLUMN " + column + " SET NOT NULL;";

                        var result = Utils.InitReturns();
                        ExecSQL(out result, sql);
                        if (!result.result)
                        {
                            _commeListNotNullErr.Add("Добавить ограничение NOT NULL для таблицы " + schema +
                                                     DBManager.tableDelimiter + table.name + " " +
                                                     "по полю " + column + " не удалось. Ошибка: " +
                                                     result.sql_error);
                        }
                        else
                        {
                            _commeListNotNullSuc.Add("Добавлено ограничение NOT NULL для таблицы " + schema +
                                                DBManager.tableDelimiter + table.name + " " +
                                                "по полю " + column + ";");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка в SetNotNull - " + ex, MonitorLog.typelog.Error, true);
                ret = new Returns(false, "Ошибка при добавлении ограничения NOT NULL", -1);
            }
        }

        private void AddSchemaNotNull(out Returns ret, BankDB bank, string point, STypeSchema typeSchema)
        {
            ret = Utils.InitReturns();
            IDataReader reader;

            string sql = "SELECT schema_name FROM information_schema.schemata WHERE schema_name " +
                (typeSchema == STypeSchema.Central ? " like " : " not like ") + "'" + Points.Pref + "%' AND schema_name like '%" + point + "%'";

            ExecRead(out reader, sql);

            while (reader.Read())
            {
                string schema = (reader["schema_name"] != DBNull.Value ? reader["schema_name"].ToString() : String.Empty);
                if (schema == String.Empty) continue;

                foreach (var table in bank.listTables)
                {
                    if (TempTableInWebCashe(schema + DBManager.tableDelimiter + table.name))
                    {
                        SetNotNull(out ret, schema, table);
                    }
                }
            }

        }

        /// <summary>
        /// Получить индексы таблиц
        /// </summary>
        /// <returns></returns>
        public Returns UnloadIndexes()
        {
            Returns ret = Utils.InitReturns();
            try
            {
                OpenConnection();

                var points = new[] {"_data", "_kernel", "_debt", "_supg", "_upload", "_webfon", "_fin", "_charge", "public"};
                IDataReader reader;
                string sql = "SELECT bd_kernel FROM " + Points.Pref + DBManager.sKernelAliasRest + "s_point WHERE nzp_graj > 0 LIMIT 1";
                ExecRead(out reader, sql);

                if (reader.Read())
                    _localPref = (reader["bd_kernel"] != DBNull.Value ? reader["bd_kernel"].ToString().Trim() : "");
                if (_localPref == String.Empty)
                {
                    ret = new Returns(false, "Не удалось получить префикс локального банка", -1);
                    return ret;
                }
                foreach (var point in points)
                {
                    switch (point)
                    {
                        case "_data":
                            GetBankIndexes(STypeSchema.Central, point);
                            GetBankIndexes(STypeSchema.Local, point);
                            break;
                        case "_kernel":
                            GetBankIndexes(STypeSchema.Central, point);
                            GetBankIndexes(STypeSchema.Local, point);
                            break;
                        case "_fin":
                            GetFinIndexes();
                            break;
                        case "_charge":
                            GetChargeIndexes();
                            break;
                        case "public":
                            GetPublicIndexes();
                            break;
                        default:
                            GetBankIndexes(STypeSchema.Central, point);
                            break;
                    }
                }

                var resultList = new List<BankDB>();
                foreach (var bnk in listBanks)
                {
                    var resultListTable = new BankDB();

                    resultListTable.name = bnk.name;
                    resultListTable.listTables = new List<TableDb>();
                    foreach (var tab in bnk.listTables)
                    {
                        Match isMatch = Regex.Match(tab.name, "t[0-9]+_*", RegexOptions.IgnoreCase);

                        if (isMatch.Success)
                        {
                            continue;
                        }
                        resultListTable.listTables.Add(tab);
                    }
                   resultList.Add(resultListTable);
                }
                var memstr = new FileStream(Path.Combine(Constants.Directories.ReportDir, "indexes_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".txt"),
                                FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
                var writer = new StreamWriter(memstr, Encoding.GetEncoding(1251));
                writer.WriteLine(Encryptor.Encrypt(JsonConvert.SerializeObject(resultList), null));
                writer.Flush();
                writer.Close();
                memstr.Close();
                
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("UnloadIndexes(): Ошибка\n" + ex, MonitorLog.typelog.Error, true);
                return new Returns(false, "Ошибка UnloadIndexes()", -1);
            }
            finally
            {
                CloseConnection();
            }

            return ret;
        }
        
        /// <summary>
        /// Получить внешние ключи таблицы
        /// </summary>
        /// <returns></returns>
        public Returns UnloadForeignKey()
        {
            var ret = Utils.InitReturns();
            var listFk = new List<ForeignKey>();
            IDataReader reader;
            try
            {
                OpenConnection();
                string centPref = Points.Pref;
                string localPref = "";
                string sql = " SELECT bd_kernel FROM " + centPref + DBManager.sKernelAliasRest + "s_point WHERE nzp_graj > 0 LIMIT 1";
                ExecRead(out reader, sql);
                if (reader.Read())
                {
                    localPref = (reader["bd_kernel"] != DBNull.Value ? reader["bd_kernel"].ToString().Trim() : "");
                }
                else
                {
                    return new Returns(false, "Ошибка добавления внешних ключей", -1);
                }
                /*
                sql =
                    " SELECT tc.table_schema, " +
                    " tc.table_name, " +
                    " kcu.column_name, " +
                    " tc.constraint_name, " +
                    " tc.constraint_type, " +
                    " ccu.table_schema AS references_schema, " +
                    " ccu.table_name AS references_table, " +
                    " ccu.column_name AS references_column " +
                    " FROM information_schema.table_constraints tc " +
                    " INNER JOIN information_schema.key_column_usage kcu " +
                    " ON tc.constraint_catalog = kcu.constraint_catalog " +
                    " AND tc.constraint_schema = kcu.constraint_schema " +
                    " AND tc.constraint_name = kcu.constraint_name " +
                    " INNER JOIN information_schema.referential_constraints rc " + 
                    " ON tc.constraint_catalog = rc.constraint_catalog " +
                    " AND tc.constraint_schema = rc.constraint_schema " +
                    " AND tc.constraint_name = rc.constraint_name " +
                    " INNER JOIN information_schema.constraint_column_usage ccu " +
                    " ON rc.unique_constraint_catalog = ccu.constraint_catalog " +
                    " AND rc.unique_constraint_schema = ccu.constraint_schema " +
                    " AND rc.unique_constraint_name = ccu.constraint_name " +
                    " WHERE tc.constraint_type LIKE 'FOREIGN%' " +
                    " AND tc.table_schema IN ((SELECT schema_name FROM information_schema.schemata WHERE schema_name LIKE '%_charge_%' LIMIT 1), " +
                    " (SELECT schema_name FROM information_schema.schemata WHERE schema_name LIKE '%_fin_%' LIMIT 1), 'public', " +
                    " '" + centPref + "_debt', '" + centPref + "_supg','" + centPref + "_upload', '" + centPref + "_webfon' " + 
                    " ) OR tc.table_schema IN " +
                    " (SELECT schema_name FROM information_schema.schemata WHERE schema_name LIKE '" + centPref + "_data' OR schema_name LIKE '" + localPref + "_data' " +
                    " OR schema_name LIKE '" + localPref + "_kernel' OR schema_name LIKE '" + centPref + "_kernel') " +
                    " GROUP BY 1,2,3,4,5,6,7,8 ";
                */

                sql = "SELECT " +
                      " x.cstrschema::information_schema.sql_identifier AS table_schema, " +
                      " x.reftable::information_schema.sql_identifier AS table_name, " +
                      " x.col::information_schema.sql_identifier AS column_name, " + 
                      " x.cstrname::information_schema.sql_identifier AS constraint_name, " + 
                      " contype AS constraint_type, " +
                      " x.tblschema::information_schema.sql_identifier AS references_schema, " +
                      " x.tblname::information_schema.sql_identifier AS references_table, " +
                      " x.colname::information_schema.sql_identifier AS references_column " +
                      " FROM ( " +
                      " SELECT nr.nspname, r.relname, r.relowner, a.attname, nc.nspname, c.conname,c.contype, " +         
                      " (SELECT rs.relname FROM pg_class rs WHERE rs.oid = c.conrelid) AS table, " +
                      " (SELECT attname FROM pg_attribute " + 
                      " WHERE attrelid = c.conrelid AND ARRAY[attnum] <@ c.conkey) AS col " +
                      " FROM pg_namespace nr, pg_class r, pg_attribute a, pg_namespace nc, pg_constraint c " +
                      " WHERE nr.oid = r.relnamespace " +
		              " AND r.oid = a.attrelid " +
		              " AND nc.oid = c.connamespace " +
		              " AND " +
                      " CASE " +
                      " WHEN c.contype = 'f'::\"char\" THEN r.oid = c.confrelid AND (a.attnum = ANY (c.confkey)) " +
                      " ELSE r.oid = c.conrelid AND (a.attnum = ANY (c.conkey)) " +
                      " END " +
		              " AND NOT a.attisdropped " +
		              " AND (c.contype = ANY (ARRAY['p'::\"char\", 'u'::\"char\", 'f'::\"char\"])		) " +
		              " AND r.relkind = 'r'::\"char\" " +
                      " ) " + 
                      " AS x(tblschema, tblname, tblowner, colname, cstrschema, cstrname, contype, reftable, col) " + 
                      " WHERE pg_has_role(x.tblowner, 'USAGE'::text) " +
                      " AND contype = 'f' " +
                      " AND " +
                      " (cstrschema IN ((SELECT schema_name FROM information_schema.schemata WHERE schema_name LIKE '%_charge_%' LIMIT 1), " +
                      " (SELECT schema_name FROM information_schema.schemata WHERE schema_name LIKE '%_fin_%' LIMIT 1), 'public', " +
                      " '" + centPref + "_debt', '" + centPref + "_supg','" + centPref + "_upload', '" + centPref + "_webfon' " +
                      " ) OR cstrschema IN " +
                      " (SELECT schema_name FROM information_schema.schemata WHERE schema_name LIKE '" + centPref + "_data' OR schema_name LIKE '" + localPref + "_data' " +
                      " OR schema_name LIKE '" + localPref + "_kernel' OR schema_name LIKE '" + centPref + "_kernel')) " +
                      " GROUP BY 1,2,3,4,5,6,7,8 " +
                      " ORDER BY 1,2,3,4,5,6,7,8 ";

                ExecRead(out reader, sql);
                while (reader.Read())
                {
                    var fKey = new ForeignKey();
                    fKey.name = reader["constraint_name"].ToString();
                    fKey.tableSchema = reader["table_schema"].ToString();
                    fKey.tableName = reader["table_name"].ToString();
                    fKey.columnName = reader["column_name"].ToString();
                    fKey.referencesSchema = reader["references_schema"].ToString();
                    fKey.referencesTable = reader["references_table"].ToString();
                    fKey.referencesColumn = reader["references_column"].ToString();

                    if(fKey.tableName.IndexOf("peni_", System.StringComparison.Ordinal) > -1 && 
                        fKey.tableSchema.IndexOf("charge", System.StringComparison.Ordinal) > -1)
                        continue;

                    if (fKey.tableSchema.IndexOf("charge", StringComparison.Ordinal) > -1 ||
                        fKey.tableSchema.IndexOf("fin", StringComparison.Ordinal) > -1)
                    {
                        if (fKey.tableSchema.IndexOf("charge", StringComparison.Ordinal) > -1)
                        {
                            fKey.tableSchema = "localPref_charge_XX";
                        }
                        else
                        {
                            fKey.tableSchema = "centrPref_fin_XX";
                        }
                    }
                    else
                    {
                        if (fKey.tableSchema.IndexOf(centPref, StringComparison.Ordinal) > -1)
                        {
                            fKey.tableSchema = fKey.tableSchema.Replace(centPref, "centrPref");
                        }
                        else
                        {
                             if (fKey.tableSchema.IndexOf(localPref, StringComparison.Ordinal) > -1)
                            fKey.tableSchema = fKey.tableSchema.Replace(localPref, "localPref");
                        }
                    }

                    if (fKey.referencesSchema.IndexOf("charge", StringComparison.Ordinal) > -1 ||
                        fKey.referencesSchema.IndexOf("fin", StringComparison.Ordinal) > -1)
                    {
                        if (fKey.referencesSchema.IndexOf("charge", StringComparison.Ordinal) > -1)
                        {
                            fKey.referencesSchema = "localPref_charge_XX";
                        }
                        else
                        {
                            fKey.referencesSchema = "centrPref_fin_XX";
                        }
                    }
                    else
                    {
                        if (fKey.referencesSchema.IndexOf(centPref, StringComparison.Ordinal) > -1)
                        {
                            fKey.referencesSchema = fKey.referencesSchema.Replace(centPref, "centrPref");
                        }
                        else
                        {
                            if (fKey.referencesSchema.IndexOf(localPref, StringComparison.Ordinal) > -1)
                                fKey.referencesSchema = fKey.referencesSchema.Replace(localPref, "localPref");
                        }
                    }

                    listFk.Add(fKey);
                }
               
                var memstr = new FileStream(Path.Combine(Constants.Directories.ReportDir, "foreign_key_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".txt"),
                                FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
                var writer = new StreamWriter(memstr, Encoding.GetEncoding(1251));
                writer.WriteLine(Encryptor.Encrypt(JsonConvert.SerializeObject(listFk), null));
                writer.Flush();
                writer.Close();
                memstr.Close();

            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка UnloadForeignKey():\n" + ex, MonitorLog.typelog.Error, true);
                return new Returns(false, "Ошибка выгрузки индексов", -1);
            }
            finally
            {
                CloseConnection();
            }
            return ret;
        }

        /// <summary>
        /// Получить индексы схемы
        /// </summary>
        /// <param name="type"></param>
        /// <param name="bank"></param>
        private void GetBankIndexes(STypeSchema type, string bank)
        {
            string schema;
            string nameSchema;
            if (bank == "_fin" || bank == "_charge")
            {
                switch (bank)
                {
                    case "_fin":
                        break;
                    case "_charge":
                        break;
                }

            }
            else
            {

                if (type == STypeSchema.Central)
                {
                    schema = Points.Pref + bank;
                    nameSchema = "CentrPref";
                    
                }

                else
                {
                    schema = _localPref + bank;
                    nameSchema = "LocalPref";
                }
                string sql = " SELECT * FROM information_schema.tables WHERE table_schema = '" + schema + "'";
                var tbles = new List<string>();

                IDataReader reader;

                ExecRead(out reader, sql);

                while (reader.Read())
                {
                    if (reader["table_name"] != DBNull.Value)
                        tbles.Add(reader["table_name"].ToString());
                }
                var tbls = new List<TableDb>();
                foreach (var table in tbles)
                {
                    var tbl = new TableDb { name = table, listIndex = new List<IndexDB>(), ListColumnsNotNull = new List<string>()};
                    
                    sql = "SELECT * FROM pg_indexes WHERE schemaname = '" + schema + "' AND tablename = '" +
                          table.Trim() + "'";
                    ExecRead(out reader, sql);
                    while (reader.Read())
                    {
                        var ind = new IndexDB();
                        ind.name = reader["indexname"].ToString();
                        if(ind.name.IndexOf("_pkey", StringComparison.CurrentCulture) > -1) continue;
                        ind.text = reader["indexdef"].ToString().Replace((type == STypeSchema.Central ? Points.Pref : _localPref), nameSchema);
                        tbl.listIndex.Add(ind);
                    }

                    //ограничения not null
                    sql = "SELECT column_name FROM information_schema.columns WHERE table_schema = '" + schema +
                          "' AND table_name = '" +
                          table.Trim() + "' AND lower(replace(is_nullable, ' ', '')) = 'no';";
                    ExecRead(out reader, sql);
                    while (reader.Read())
                    {
                        tbl.ListColumnsNotNull.Add(reader["column_name"].ToString());
                    }

                    tbls.Add(tbl);
                }
                var bnk = new BankDB { name = (type == STypeSchema.Central ? "central" : "local") + bank, listTables = tbls };
                listBanks.Add(bnk);
            }
        }

        /// <summary>
        /// Получить индексы таблиц схемы public
        /// </summary>
        private void GetPublicIndexes()
        {
            string sql = " SELECT * FROM information_schema.tables WHERE table_schema = 'public'";
            var tbles = new List<string>();

            IDataReader reader;

            ExecRead(out reader, sql);

            while (reader.Read())
            {
                if (reader["table_name"] != DBNull.Value)
                    tbles.Add(reader["table_name"].ToString());
            }
            var tbls = new List<TableDb>();
            foreach (var table in tbles)
            {
                var tbl = new TableDb { name = table, listIndex = new List<IndexDB>(), ListColumnsNotNull = new List<string>()};

                sql = "SELECT * FROM pg_indexes WHERE schemaname = 'public' AND tablename = '" +
                      table.Trim() + "'";
                ExecRead(out reader, sql);
                while (reader.Read())
                {
                    var ind = new IndexDB();
                    ind.name = reader["indexname"].ToString();
                    if (ind.name.IndexOf("_pkey", StringComparison.CurrentCulture) > -1) continue;
                    ind.text = reader["indexdef"].ToString();
                    tbl.listIndex.Add(ind);
                }

                //ограничения not null
                sql = "SELECT column_name FROM information_schema.columns WHERE table_schema = 'public' AND table_name = '" +
                      table.Trim() + "' AND lower(replace(is_nullable, ' ', '')) = 'no';";
                ExecRead(out reader, sql);
                while (reader.Read())
                {
                    tbl.ListColumnsNotNull.Add(reader["column_name"].ToString());
                }

                tbls.Add(tbl);
            }
            var bnk = new BankDB { name = "public", listTables = tbls };
            listBanks.Add(bnk);
        }

        /// <summary>
        /// Получить индексы схемы fin_XX
        /// </summary>
        private void GetFinIndexes()
        {
            string sql = "SELECT schema_name FROM information_schema.schemata WHERE schema_name like '" + Points.Pref + "_fin_%'";
            IDataReader reader;
            string schema;
            string bankName = "central_fin";
            ExecRead(out reader, sql);
            if (reader.Read())
            {
                schema = reader["schema_name"].ToString();
            }
            else
            {
                return;
            }

            string nameSchema = "CentrPref_fin_XX";

            sql = " SELECT * FROM information_schema.tables WHERE table_schema = '" + schema + "'";
            var tbles = new List<string>();


            ExecRead(out reader, sql);

            while (reader.Read())
            {
                if (reader["table_name"] != DBNull.Value)
                    tbles.Add(reader["table_name"].ToString());
            }
            var tbls = new List<TableDb>();
            foreach (var table in tbles)
            {
                var tbl = new TableDb { name = table, listIndex = new List<IndexDB>(), ListColumnsNotNull = new List<string>()};

                sql = "SELECT * FROM pg_indexes WHERE schemaname = '" + schema + "' AND tablename = '" +
                      table.Trim() + "'";
                ExecRead(out reader, sql);
                while (reader.Read())
                {
                    var ind = new IndexDB();
                    ind.name = reader["indexname"].ToString();
                    if (ind.name.IndexOf("_pkey", StringComparison.CurrentCulture) > -1) continue;
                    ind.text = reader["indexdef"].ToString().Replace(schema, nameSchema);
                    tbl.listIndex.Add(ind);
                }

                //ограничения not null
                sql = "SELECT column_name FROM information_schema.columns WHERE table_schema = '" + schema +
                      "' AND table_name = '" +
                      table.Trim() + "' AND lower(replace(is_nullable, ' ', '')) = 'no';";
                ExecRead(out reader, sql);
                while (reader.Read())
                {
                    tbl.ListColumnsNotNull.Add(reader["column_name"].ToString());
                }

                tbls.Add(tbl);
            }
            var bnk = new BankDB { name = bankName, listTables = tbls };
            listBanks.Add(bnk);
        }

        /// <summary>
        /// Получить индексы схемы charge_XX
        /// </summary>
        private void GetChargeIndexes()
        {

            {
                string sql = "SELECT schema_name FROM information_schema.schemata WHERE schema_name like '" + _localPref + "_charge_%'";
                IDataReader reader;
                string schema;
                string bankName = "local_charge";
                ExecRead(out reader, sql);
                if (reader.Read())
                {
                    schema = reader["schema_name"].ToString();
                }
                else
                {
                    return;
                }

                string nameSchema = "LocalPref_charge_XX";

                sql = " SELECT * FROM information_schema.tables WHERE table_schema = '" + schema + "'";
                var tbles = new List<string>();


                ExecRead(out reader, sql);

                while (reader.Read())
                {
                    if (reader["table_name"] != DBNull.Value)
                        tbles.Add(reader["table_name"].ToString());
                }
                
                var tbls = new List<TableDb>();
                foreach (var table in tbles)
                {
                    if(table.IndexOf("peni_", System.StringComparison.Ordinal) > -1) continue;

                    var tbl = new TableDb { name = table, listIndex = new List<IndexDB>(), ListColumnsNotNull = new List<string>()};

                    #region Индексы calc_gkuXXXX_XX
                    Match isMatch = Regex.Match(table, "calc_gku[0-9][0-9][0-9][0-9]_[0-9][0-9]", RegexOptions.IgnoreCase);
                    if (isMatch.Success)
                    {
                        //if(calc_gkuXXXX_XX)
                        //    continue;
                        //tbl.name = "calc_gkuXXXX_XX";
                        //sql = "SELECT * FROM pg_indexes WHERE schemaname = '" + schema + "' AND tablename = '" +
                        //  table.Trim() + "'";
                        //ExecRead(out reader, sql);
                        //while (reader.Read())
                        //{
                        //    var ind = new IndexDB();
                        //    ind.name = reader["indexname"].ToString().Replace(table, "calc_gkuXXXX_XX");
                        //    ind.text = reader["indexdef"].ToString().Replace(schema, nameSchema);
                        //    ind.text = ind.text.Replace(table, "calc_gkuXXXX_XX");
                        //    tbl.listIndex.Add(ind);
                        //}
                        
                        //tbls.Add(tbl);
                        //calc_gkuXXXX_XX = true;
                        continue;

                    }
                    #endregion

                    #region Индексы chargeXXXX_XX
                    isMatch = Regex.Match(table, "charge[0-9][0-9][0-9][0-9]_[0-9][0-9]", RegexOptions.IgnoreCase);
                    if (isMatch.Success)
                    {
                        //if (chargeXXXX_XX)
                        //    continue;
                        //tbl.name = "chargeXXXX_XX";
                        //sql = "SELECT * FROM pg_indexes WHERE schemaname = '" + schema + "' AND tablename = '" +
                        //  table.Trim() + "'";
                        //ExecRead(out reader, sql);
                        //while (reader.Read())
                        //{
                        //    var ind = new IndexDB();
                        //    ind.name = reader["indexname"].ToString().Replace(table.Substring(7, 6), "_XXXX_XX");
                        //    ind.text = reader["indexdef"].ToString().Replace(schema, nameSchema);
                        //    ind.text = ind.text.Replace(table, "chargeXXXX_XX");
                        //    ind.text = ind.text.Replace(table.Substring(7, 6), "_XXXX_XX");
                        //    tbl.listIndex.Add(ind);
                        //}

                        //tbls.Add(tbl);
                        //chargeXXXX_XX = true;
                        continue;

                    }
                    #endregion

                    #region Индексы countersXXXX_XX
                    isMatch = Regex.Match(table, "counters[0-9][0-9][0-9][0-9]_[0-9][0-9]", RegexOptions.IgnoreCase);
                    if (isMatch.Success)
                    {
                        //if (countersXXXX_XX)
                        //    continue;
                        //tbl.name = "countersXXXX_XX";
                        //sql = "SELECT * FROM pg_indexes WHERE schemaname = '" + schema + "' AND tablename = '" +
                        //  table.Trim() + "'";
                        //ExecRead(out reader, sql);
                        //while (reader.Read())
                        //{
                        //    var ind = new IndexDB();
                        //    ind.name = reader["indexname"].ToString().Replace(table, "countersXXXX_XX");
                        //    ind.text = reader["indexdef"].ToString().Replace(schema, nameSchema);
                        //    ind.text = ind.text.Replace(table, "countersXXXX_XX");
                        //    tbl.listIndex.Add(ind);
                        //}

                        //tbls.Add(tbl);
                        //countersXXXX_XX = true;
                        continue;

                    }
                    #endregion

                    #region Индексы countlnkXXXX_XX
                    isMatch = Regex.Match(table, "countlnk[0-9][0-9][0-9][0-9]_[0-9][0-9]", RegexOptions.IgnoreCase);
                    if (isMatch.Success)
                    {
                        //if (countlnkXXXX_XX)
                        //    continue;
                        //tbl.name = "countersXXXX_XX";
                        //sql = "SELECT * FROM pg_indexes WHERE schemaname = '" + schema + "' AND tablename = '" +
                        //  table.Trim() + "'";
                        //ExecRead(out reader, sql);
                        //while (reader.Read())
                        //{
                        //    var ind = new IndexDB();
                        //    ind.name = reader["indexname"].ToString().Replace(table, "countlnkXXXX_XX");
                        //    ind.text = reader["indexdef"].ToString().Replace(schema, nameSchema);
                        //    ind.text = ind.text.Replace(table, "countlnkXXXX_XX");
                        //    tbl.listIndex.Add(ind);
                        //}

                        //tbls.Add(tbl);
                        //countlnkXXXX_XX = true;
                        continue;

                    }
                    #endregion

                    #region Индексы gilXXXX_XX
                    isMatch = Regex.Match(table, "gil[0-9][0-9][0-9][0-9]_[0-9][0-9]", RegexOptions.IgnoreCase);
                    if (isMatch.Success)
                    {
                        //if (gilXXXX_XX)
                        //    continue;
                        //tbl.name = "gilXXXX_XX";
                        //sql = "SELECT * FROM pg_indexes WHERE schemaname = '" + schema + "' AND tablename = '" +
                        //  table.Trim() + "'";
                        //ExecRead(out reader, sql);
                        //while (reader.Read())
                        //{
                        //    var ind = new IndexDB();
                        //    ind.name = reader["indexname"].ToString().Replace(table, "gilXXXX_XX");
                        //    ind.text = reader["indexdef"].ToString().Replace(schema, nameSchema);
                        //    ind.text = ind.text.Replace(table, "gilXXXX_XX");
                        //    tbl.listIndex.Add(ind);
                        //}

                        //tbls.Add(tbl);
                        //gilXXXX_XX = true;
                        continue;

                    }
                    #endregion

                    #region Индексы nedoXXXX_XX
                    isMatch = Regex.Match(table, "nedo[0-9][0-9][0-9][0-9]_[0-9][0-9]", RegexOptions.IgnoreCase);
                    if (isMatch.Success)
                    {
                        //if (nedoXXXX_XX)
                        //    continue;
                        //tbl.name = "nedoXXXX_XX";
                        //sql = "SELECT * FROM pg_indexes WHERE schemaname = '" + schema + "' AND tablename = '" +
                        //  table.Trim() + "'";
                        //ExecRead(out reader, sql);
                        //while (reader.Read())
                        //{
                        //    var ind = new IndexDB();
                        //    ind.name = reader["indexname"].ToString().Replace(table, "nedoXXXX_XX");
                        //    ind.text = reader["indexdef"].ToString().Replace(schema, nameSchema);
                        //    ind.text = ind.text.Replace(table, "nedoXXXX_XX");
                        //    tbl.listIndex.Add(ind);
                        //}

                        //tbls.Add(tbl);
                        //nedoXXXX_XX = true;
                        continue;

                    }
                    #endregion

                    sql = "SELECT * FROM pg_indexes WHERE schemaname = '" + schema + "' AND tablename = '" +
                          table.Trim() + "'";
                    ExecRead(out reader, sql);
                    while (reader.Read())
                    {
                        var ind = new IndexDB();
                        ind.name = reader["indexname"].ToString();
                        if (ind.name.IndexOf("_pkey", StringComparison.CurrentCulture) > -1) continue;
                        ind.text = reader["indexdef"].ToString().Replace(schema, nameSchema);
                        tbl.listIndex.Add(ind);
                    }

                    //ограничения not null
                    sql = "SELECT column_name FROM information_schema.columns WHERE table_schema = '" + schema +
                          "' AND table_name = '" +
                          table.Trim() + "' AND lower(replace(is_nullable, ' ', '')) = 'no';";
                    ExecRead(out reader, sql);
                    while (reader.Read())
                    {
                        tbl.ListColumnsNotNull.Add(reader["column_name"].ToString());
                    }

                    tbls.Add(tbl);
                }
                var bnk = new BankDB { name = bankName, listTables = tbls };
                listBanks.Add(bnk);
            }
        }

        /// <summary>
        /// Тип банка(локальный, центральный)
        /// </summary>
        private enum  STypeSchema
         {
             Central = 1,
             Local = 2
         }


        private struct IndexDB
        {
            public string name;
            public string text;
        }

        struct TableDb
        {
            public string name;
            public List<IndexDB> listIndex;
            public List<string> ListColumnsNotNull;
        }

        private struct BankDB
        {
            public string name;
            public List<TableDb> listTables;
        }

        private struct ForeignKey
        {
            public string name;
            public string tableSchema;
            public string tableName;
            public string columnName;
            public string referencesSchema;
            public string referencesTable;
            public string referencesColumn;
        }

        /// <summary>Открыть соединение</summary>
        /// <returns>Открытое соединение</returns>
        protected virtual IDbConnection OpenConnection()
        {
            if (Connection == null)
            {
                Connection = DBManager.GetConnection(Constants.cons_Webdata);
                var result = DBManager.OpenDb(Connection, true);
                if (!result.result)
                {
                    throw new Exception(result.text);
                }
            }

            return Connection;
        }

        /// <summary>Закрыть соединение с БД</summary>
        protected virtual void CloseConnection()
        {
            try
            {
                if (Connection != null)
                {
                    Connection.Close();
                    Connection = null;
                }
            }
            catch (Exception exc)
            {
                throw new Exception("Не удалось закрыть соединение", exc);
            }
        }

        /// <summary>Выполнить запрос</summary>
        /// <param name="sql">Sql запрос</param>
        protected new virtual void ExecSQL(string sql)
        {
            ExecSQL(sql, true, 6000);
        }

        /// <summary>Выполнить запрос</summary>
        /// <param name="sql">Sql запрос</param>
        /// <param name="inlog">Логировать да/нет</param>
        protected new virtual void ExecSQL(string sql, bool inlog)
        {
            ExecSQL(sql, inlog, 6000);
        }

        /// <summary>Выполнить запрос</summary>
        /// <param name="sql">Sql запрос</param>
        /// <param name="inlog">Логировать да/нет</param>
        /// <param name="timeout">Таймаут</param>
        protected virtual void ExecSQL(string sql, bool inlog, int timeout)
        {
            var result = DBManager.ExecSQL(Connection, sql, inlog, timeout);
            if (!result.result)
            {
                throw new Exception(result.text);
            }
        }

        protected void ExecSQL(out Returns result, string sql)
        {
            result = Utils.InitReturns();
            try
            {
                result = ExecSql(Connection, null, sql, false, 6000); //DBManager.ExecSQL(Connection, sql, false, 6000);
            }
            catch 
            {
                throw new Exception(result.text);
            }
        }

        private static int _affectedRowsCount = -100;

        /// <summary>
        /// Выполнить запрос
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="transaction"></param>
        /// <param name="sql"></param>
        /// <param name="inlog"></param>
        /// <param name="time"></param>
        /// <returns></returns>
        private Returns ExecSql(IDbConnection connection, IDbTransaction transaction, string sql, bool inlog, int time)
        {
            System.Threading.Thread.CurrentThread.CurrentUICulture = new CultureInfo("en-US");
            
            var ret = Utils.InitReturns();

#if PG
            sql = sql.PgNormalize(connection);
#endif

            IDbCommand cmd = null;

            try
            {
                cmd = DBManager.newDbCommand(sql, connection, transaction);
                cmd.CommandTimeout = time;
                _affectedRowsCount = cmd.ExecuteNonQuery();
            }
            catch (NpgsqlException ex)//Exception ex)
            {
                ret.result = false;
                ret.text = "Ошибка выполнения операции в базе данных ";

                ret.sql_error = " БД " + connection.Database + " \n '" + sql + "' \n " + ex.Message +
                    " " + ex.Detail;

                if (ex.Message == "ОШИБКА: 21000: подзапрос в выражении вернул больше одной строки")
                    ret.sql_error += " \n Возможна ошибка в данных  \n";

                string err = Environment.NewLine +
                    ret.text + " \n " + ret.sql_error;

                if (inlog)
                {

                    StackTrace stackTrace = new StackTrace();           // get call stack
                    StackFrame[] stackFrames = stackTrace.GetFrames();  // get method calls (frames)

                    // write call stack method names
                    foreach (StackFrame stackFrame in stackFrames)
                    {
                        if (stackFrame.GetMethod().Name.Trim() == "Invoke") break;
                        err += stackFrame.GetMethod().Name + " \n"; // write method name
                    }


                    MonitorLog.WriteException(err);
                    //#if DEBUG
                    if (Points.FullLogging)
                    {
                        MonitorLog.WriteLog(" Выполнение запроса неудачно  " + sql + " " + ex, MonitorLog.typelog.Info,
                            1, 1, true);
                    }
                    //#endif
                }


                if (Constants.Viewerror)
                {
                    ret.text = err;
                }
            }
            finally
            {
                if (cmd != null)
                {
                    cmd.Dispose();
                }
            }

            return ret;
        }

        /// <summary>Проверка на существование таблицы</summary>
        /// <param name="sql">Sql запрос</param>
        /// <returns>Таблица</returns>
        protected virtual bool TempTableInWebCashe(string tableName)
        {
            return DBManager.TempTableInWebCashe(Connection, tableName);
        }

        /// <summary>Проверка на существование колонки в таблице</summary>
        /// <param name="tableName">Название таблицы</param>
        /// <param name="columnName">Название колонки</param>
        /// <returns>Колонка</returns>
        protected virtual bool TempColumnInWebCashe(string tableName, string columnName)
        {
            return DBManager.TempColumnInWebCashe(Connection, tableName, columnName);
        }


        /// <summary>Получить результат sql запроса в виде таблицы</summary>
        /// <param name="sql">Sql запрос</param>
        /// <returns>Таблица</returns>
        protected virtual DataTable ExecSQLToTable(string sql)
        {
            return DBManager.ExecSQLToTable(Connection, sql);
        }
        /// <summary>Получить результат sql запроса в виде IDataReader</summary>
        /// <param name="reader">IDataReader</param>
        /// <param name="sql">Sql запрос</param>
        protected virtual void ExecRead(out IDataReader reader, string sql)
        {
            ExecRead(out reader, sql, true, 300);
        }


        /// <summary>Получить результат sql запроса в виде значения</summary>
        /// <param name="sql">Sql запрос</param>
        protected virtual object ExecScalar(string sql)
        {
            Returns ret;
            return DBManager.ExecScalar(Connection, sql, out ret, true);
        }

        /// <summary>Получить результат sql запроса в виде IDataReader</summary>
        /// <param name="reader">IDataReader</param>
        /// <param name="sql">Sql запрос</param>
        /// <param name="inlog">Логировать да/нет</param>
        protected virtual void ExecRead(out IDataReader reader, string sql, bool inlog)
        {
            ExecRead(out reader, sql, inlog, 300);
        }

        /// <summary>Получить результат sql запроса в виде IDataReader</summary>
        /// <param name="reader">IDataReader</param>
        /// <param name="sql">Sql запрос</param>
        /// <param name="inlog">Логировать да/нет</param>
        /// <param name="timeout">Таймаут</param>
        protected virtual void ExecRead(out IDataReader reader, string sql, bool inlog, int timeout)
        {
            var result = DBManager.ExecRead(Connection, out reader, sql, inlog, timeout);
            if (!result.result)
            {
                throw new Exception(result.text);
            }
        }

        /// <summary>Получить результат sql запроса в виде IDataReader</summary>
        /// <param name="reader">IDataReader</param>
        /// <param name="sql">Sql запрос</param>
        /// <param name="inlog">Логировать да/нет (по умолчанию да)</param>
        protected virtual void ExecRead(out MyDataReader reader, string sql, bool inlog = true)
        {
            var result = DBManager.ExecRead(Connection, out reader, sql, inlog);
            if (!result.result)
            {
                throw new Exception(result.text);
            }
        }

    }
}
