using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Win32;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;

namespace STCLINE.KP50.IFMX.Kernel.source.CreateNewBank
{
    public enum SchemaType
    {
        Kernel = 1
    }
    public class BankFinder
    {
        public string TemplateBaseName { get; set; }
        public int NzpBl { get; set; }
        public string NewBaseName { get; set; }
        public int NewBasesYear { get; set; }
        public string NewPref { get; set; }
        public string NewComment { get; set; }
        public SchemaType Type { get; set; }

        public BankFinder()
        {
            TemplateBaseName = NewBaseName = NewPref = "";
            NewComment = "";
            NewBasesYear = NzpBl = 0;
        }
    }

    public class BankCreator
    {
        
        public Returns Create(IDbConnection connection, BankFinder finder)
        {
            Returns ret = Utils.InitReturns();
            if (String.IsNullOrEmpty(finder.TemplateBaseName))
                return new Returns(false, "Не задано имя шаблонной базы, с которой будет производиться копирование");
            if (finder.NzpBl <= 0)
                return new Returns(false, "Не задан код шаблонной базы, с которой будет производиться копирование");
            if (String.IsNullOrEmpty(finder.NewBaseName))
                return new Returns(false, "Не задано имя новой базы");
            if (finder.NewBasesYear <= 0)
                return new Returns(false, "Не задан год новой базы");
            if (String.IsNullOrEmpty(finder.NewPref))
                return new Returns(false, "Не задан префикс новой базы");


            string sql =
                " SELECT schema_name FROM information_schema.schemata " +
                " WHERE  LOWER( TRIM(schema_name) ) = '" + finder.NewBaseName + "'";
            if (DBManager.ExecSQLToTable(connection, sql).Rows.Count == 1)
            {
                ret.text = "Схема " + finder.NewBaseName + " уже существует! Работа прекращена.";
                MonitorLog.WriteLog(ret.text, MonitorLog.typelog.Info, true);
                return ret;
            }

            // скопировать текущую схему
            ret = CopySchema(connection, finder);

            //проверяем - создалась ли схема
            sql =
                " SELECT schema_name FROM information_schema.schemata " +
                " WHERE  LOWER( TRIM(schema_name) ) = '" + finder.NewBaseName + "'";
            if (DBManager.ExecSQLToTable(connection, sql).Rows.Count < 1)
            {
                ret.text = "Схема " + finder.NewBaseName + " не создалась! Работа прекращена.";
                MonitorLog.WriteLog(ret.text, MonitorLog.typelog.Info, true);
                return ret;
            }

            if (ret.result)
            {
                // теперь можно добавить в список периодов
                sql =
                   String.Format(" INSERT INTO {0}s_baselist (dbname, idtype, yearr, attr, frec_id, comment) " +
                                 " SELECT '{1}', idtype, {2}, attr, frec_id, '{3}' " +
                                 " FROM {5}s_baselist where nzp_bl = {4}",
                       Points.Pref + DBManager.sKernelAliasRest,
                       finder.NewBaseName,
                       finder.NewBasesYear,
                       finder.NewComment,
                       finder.NzpBl,
                       Points.Pref + DBManager.sKernelAliasRest);
                ret = DBManager.ExecSQL(connection, sql, true);
                if (!ret.result)
                {
                    ret.text = "Ошибка при добавлении нового периода в верхний банк! Ошибки: " + Environment.NewLine + ret.text;
                MonitorLog.WriteLog(ret.text, MonitorLog.typelog.Info, true);
                    //выходим, т.к. без этой вставки не пройдут миграции
                    return ret;
                }

                //переписываем номера сделанных миграций
                sql =
                    " INSERT INTO " + Points.Pref + DBManager.sKernelAliasRest + "\"SchemaInfo\"" +
                    " (\"Version\",\"AssemblyKey\", \"Prefix\", \"UpdateDate\")" +
                    " SELECT \"Version\", \"AssemblyKey\", '" + finder.NewBaseName + "', \"UpdateDate\"" +
                    " FROM " + Points.Pref + DBManager.sKernelAliasRest + "\"SchemaInfo\" " +
                    " WHERE \"Prefix\" = '" + finder.TemplateBaseName + "'";
                ret = DBManager.ExecSQL(connection, sql, true);
                if(!ret.result)
                {
                    ret.text = "Ошибка при добавлении нового периода в верхний банк! Ошибки: " + Environment.NewLine + ret.text;
                MonitorLog.WriteLog(ret.text, MonitorLog.typelog.Info, true);
                }

                //если банк центральный, то не нужен данный insert
                if (Points.Pref != finder.NewPref)
                {
                    sql =
                        String.Format(
                            " INSERT INTO {0}s_baselist (dbname, idtype, yearr, attr, frec_id, comment) " +
                            " SELECT '{1}', idtype, {2}, attr, frec_id, '{3}' " +
                            " FROM {0}s_baselist where nzp_bl = {4}",
                            finder.NewPref + DBManager.sKernelAliasRest,
                            finder.NewBaseName,
                            finder.NewBasesYear,
                            finder.NewComment,
                            finder.NzpBl);
                    ret.result = DBManager.ExecSQL(connection, sql, true).result;
                }
                if (!ret.result)
                {
                    ret.text =
                        "Ошибка при добавлении нового периода в локальный банк! Ошибки: " + Environment.NewLine + ret.text;
                }
                else
                {
                    ret.text = "Новый период успешно создан! Теперь необходимо перезапустить 'Host' и 'Web'";
                   
                }
            }
            return ret;
        }

        private Returns CopySchema(IDbConnection connection, BankFinder finder)
        {
            Returns ret = new Returns() { result = true };
            MonitorLog.WriteLog("Старт создания схемы: '" + finder.NewBaseName + "'", MonitorLog.typelog.Info, true);
            try
            {
                MonitorLog.WriteLog("Старт считывания параметров подключения БД из Host.config", MonitorLog.typelog.Info,
                    true);
                System.Data.Common.DbConnectionStringBuilder builder =
                    DBManager.getDbStringBuilder(connection.ConnectionString);
                string tempFileFullName = Path.GetTempFileName();
                string host = builder["Server"].ToString();
                string port = builder["Port"].ToString();
                string user = builder["User Id"].ToString();
                string password = builder["Password"].ToString();
                string database = builder["Database"].ToString();
                MonitorLog.WriteLog("Успешно выполнено считывание параметров подключения БД из Host.config ",
                    MonitorLog.typelog.Info, true);

                // снимем бэкап схемы
                string psqlCommand = "";
                if (finder.Type == SchemaType.Kernel)
                {
                    //если kernel, то бэкапим с данными
                    psqlCommand =
                        String.Format(
                            "pg_dump.exe --host {0} --port {1} --username {2} -F p --file \"{3}\" --serializable-deferrable --schema {4} {5} ",
                            host, port, user, tempFileFullName, finder.TemplateBaseName, database);
                    MonitorLog.WriteLog("Выбран бэкап '" + finder.NewBaseName + "' типа " + SchemaType.Kernel, MonitorLog.typelog.Info,
                        true);
                }
                else
                {
                    psqlCommand =
                        String.Format(
                            "pg_dump.exe --host {0} --port {1} --username {2} -F p --file \"{3}\" --schema-only --schema {4} {5} ",
                            host, port, user, tempFileFullName, finder.TemplateBaseName, database);
                }
                MonitorLog.WriteLog(
                    "Старт снятия бэкапа схем, которую копируем. Выполняемая команда: '" + psqlCommand + "'",
                    MonitorLog.typelog.Info, true);

                if (!ExecuteCommand(psqlCommand, password))
                {
                    ret.text = "Ошибка выполнения psql-команды! См. error.log ";
                    MonitorLog.WriteLog(ret.text, MonitorLog.typelog.Info, true);
                    ret.result = false;
                    return ret;
                }

                // теперь заменим в файле старую схему на новую
                MonitorLog.WriteLog(
                    "Старт редактирования бэкапа схемы (изменение префикса схемы с '" + finder.TemplateBaseName + "' на '" +
                    finder.NewBaseName + "' )" + " в файле " + tempFileFullName,
                    MonitorLog.typelog.Info, true);
                string text = File.ReadAllText(tempFileFullName);

                text = text.Replace(String.Format("CREATE SCHEMA {0};", finder.TemplateBaseName),
                    String.Format("CREATE SCHEMA {0};", finder.NewBaseName));
                text = text.Replace(String.Format("ALTER SCHEMA {0} OWNER", finder.TemplateBaseName),
                    String.Format("ALTER SCHEMA {0} OWNER", finder.NewBaseName));
                text = text.Replace(String.Format("SET search_path = {0}", finder.TemplateBaseName),
                    String.Format("SET search_path = {0}", finder.NewBaseName));
                text = text.Replace(String.Format("ALTER FUNCTION {0}.", finder.TemplateBaseName),
                    String.Format("ALTER FUNCTION {0}.", finder.NewBaseName));
                text = text.Replace(String.Format("ALTER TABLE {0}.", finder.TemplateBaseName),
                    String.Format("ALTER TABLE {0}.", finder.NewBaseName));


                File.WriteAllText(tempFileFullName, text);
                MonitorLog.WriteLog("Успешно выполнено редактирование бэкапа схемы ",
                    MonitorLog.typelog.Info, true);


                psqlCommand =
                    String.Format(
                        "psql --host {0} --port {1} --username {2} --dbname {3} --file {4} ",
                        host, port, user, database, tempFileFullName);

                MonitorLog.WriteLog(
                    "Старт восстановления бэкапа схемы. Выполняемая команда: '" + psqlCommand + "' из файла " +
                    tempFileFullName, MonitorLog.typelog.Info, true);
                if (!ExecuteCommand(psqlCommand, password))
                {
                    ret.text = "Ошибка выполнения psql-команды! См. error.log ";
                    MonitorLog.WriteLog(ret.text, MonitorLog.typelog.Info, true);
                    ret.result = false;
                }
                //проверяем - создалась ли схема
                string sql =
                    " SELECT schema_name FROM information_schema.schemata " +
                    " WHERE  LOWER( TRIM(schema_name) ) = '" + finder.NewBaseName + "'";
                if (DBManager.ExecSQLToTable(connection, sql).Rows.Count < 1)
                {
                    ret.text = "Схема " + finder.NewBaseName + " не создалась! Работа прекращена.";
                    MonitorLog.WriteLog(ret.text, MonitorLog.typelog.Info, true);
                    ret.result = false;
                    return ret;
                }
            }
            catch (Exception ex)
            {
                ret.text = "Ошибка фукции CopySchema! См. error.log";
                MonitorLog.WriteLog(ex.Message + ex.StackTrace, MonitorLog.typelog.Info, true);
                ret.result = false;
            }
            return ret;
        }


        private bool ExecuteCommand(string command, string password)
        {
            try
            {
                string args = String.Format("/c {0}", command);
                // найдем место где есть утилиты postgresql
                string pgDirectory = ReadRegKey(Registry.CurrentUser, "SOFTWARE\\pgAdmin III", "PostgreSQLPath");
                if (String.IsNullOrEmpty(pgDirectory))
                {
                    pgDirectory = ReadRegKey(Registry.LocalMachine,
                        "SOFTWARE\\PostgreSQL Global Development Group\\PostgreSQL", "Location");
                    if (String.IsNullOrEmpty(pgDirectory))
                    {
                        // путь не найден, пытаемся найти dll рядом с hostman.exe в папке pg_library
                        pgDirectory = Path.Combine(Environment.CurrentDirectory, "pg_library\\");
                    }
                    else
                    {
                        pgDirectory += "\\bin";
                    }
                }

                MonitorLog.WriteLog(" Выбран путь для выполнения psql-команды: " + pgDirectory, MonitorLog.typelog.Info,
                    true);

                Environment.SetEnvironmentVariable("PGPASSWORD", password);
                System.Diagnostics.ProcessStartInfo info = new System.Diagnostics.ProcessStartInfo("cmd.exe", args);
                info.WorkingDirectory = pgDirectory;
                //info.RedirectStandardError = true;
                //info.RedirectStandardOutput = true;
                //info.CreateNoWindow = true;
                //info.UseShellExecute = false;
                info.RedirectStandardError = false;
                info.RedirectStandardOutput = false;
                info.CreateNoWindow = false;
                info.UseShellExecute = false;
                System.Diagnostics.Process proc = new System.Diagnostics.Process();
                proc.EnableRaisingEvents = true;
                proc.StartInfo = info;
                proc.Start();
                proc.WaitForExit();
                proc.Close();
                MonitorLog.WriteLog("Успешно выполнена psql-команда: " + command, MonitorLog.typelog.Info, true);
                return true;
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog(
                    " Ошибка процедуры создании нового локального банка при выполнении функции ExecuteCommand. " +
                    Environment.NewLine +
                    " Выполняемая команда: " + command + Environment.NewLine +
                    " Текст ошибки: " + ex.Message + Environment.NewLine + ex.StackTrace, MonitorLog.typelog.Error, true);
                return false;
            }
        }
        public string ReadRegKey(RegistryKey baseKey, string subKey, string keyName)
        {
            // Opening the registry key
            RegistryKey rk = baseKey;
            // Open a subKey as read-only
            RegistryKey sk1 = rk.OpenSubKey(subKey);
            // If the RegistrySubKey doesn't exist -> (null)
            if (sk1 == null)
            {
                return null;
            }
            else
            {
                try
                {
                    // If the RegistryKey exists I get its value
                    // or null is returned.
                    return (string)sk1.GetValue(keyName.ToUpper());
                }
                catch (Exception ex)
                {
                    //ShowErrorMessage(e, "Reading registry " + KeyName.ToUpper());
                    MonitorLog.WriteException("Ошибка функции ReadRegKey", ex);
                    return null;
                }
            }
        }
    }
}
