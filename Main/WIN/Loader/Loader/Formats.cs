using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using Dapper;
using FormatLibrary;

namespace Loader
{
    [AssembleAttribute(FormatName = "ПГУ", Version = "1.0", RegistrationName = "PGU")]
    public class PGUFormat : Format
    {
        private string logName;
        public static void setCulture()
        //----------------------------------------------------------------------
        {
            var culture = new CultureInfo("ru-RU")
            {
                NumberFormat = { NumberDecimalSeparator = "." },
                DateTimeFormat = { ShortDatePattern = "dd.MM.yyyy", ShortTimePattern = "HH:mm:ss" }
            };
            Thread.CurrentThread.CurrentUICulture = culture;
            Thread.CurrentThread.CurrentCulture = culture;
        }
        protected override string Run()
        {
            AbsolutePath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "Loader");
            logName = System.IO.Path.Combine(AbsolutePath, "log" + nzp_load + ".txt");
            setCulture();
            SetProgress(0);
            Update(Connection, 0);
            AbsolutePath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "Loader");
            #region Получение путей + Создание необходимых папок + Распаковка файлов из архива
            var reportList = new List<string> { string.Format("Отчет о загрузке файла {0}", FileName) };
            var parentDir = string.IsNullOrEmpty(AbsolutePath) ? (new FileInfo(Assembly.GetEntryAssembly().Location)).Directory.FullName : AbsolutePath;
            var newPath = Directory.CreateDirectory(string.Format("{0}\\LoadFiles\\{1}\\{2}\\{3}\\{4}", parentDir, DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, FileName.Split('.').First()));
            Directory.CreateDirectory(string.Format("{0}\\iso_folder.exp\\pg", parentDir));
            if (File.Exists(newPath.FullName + "\\" + FileName))
                File.Delete(newPath.FullName + "\\" + FileName);
            File.Copy(System.IO.Path.Combine(Path, FileName), newPath.FullName + "\\" + FileName);
            Archive.GetInstance(newPath.FullName + "\\" + FileName).Decompress(newPath.FullName + "\\" + FileName, newPath.FullName);
            #endregion

            var header = new Header();
            try
            {
                #region Сохранение заголовка в БД
                var lines = File.ReadAllLines(newPath.FullName + "\\" + "InfoDescript.txt", Encoding.GetEncoding(1251));
                var fields = lines[0].Split('|');
                var i = 0;
                foreach (var field in header.GetType().GetProperties().Where(field => field.Name != "lineType"))
                {
                    field.SetValue(header, fields[i], null);
                    i++;
                }
                if (
                    Connection.ExecuteScalar<int>("select count(*) from public.erc where kod_erc = " +
                                                  header.raschSchet.Trim(), null, null, null, null) == 0)
                {
                    throw new Exception("Не найден соответствующий код ЕРЦ, проверьте правильность загружаемого файла");
                }
                Connection.Execute("Update public.erc set (dat_sys_start) =  ('" + header.datSysStart + "')  where kod_erc = " + header.raschSchet.Trim(), null, null, null, null);
                //InsertIntoERC(header);
                InsertIntoSaldoDate(header);
                reportList.Add(string.Format("Дата загрузки: {0}", DateTime.Now));
                reportList.Add(string.Format("Месяц начисления: {0}", header.chargeDate));
                reportList.Add(string.Format("Организация {0}; ИНН {1}, КПП {2}", header.nameOrgPasses, header.INN,
                    header.KPP));
                Connection.Execute(
                    "Update public.imports set (num_load,dat_month,org_name,org_podr_name,org_inn,org_kpp,org_file_num,sender_phone," +
                    "sender_fio,count_ls,sum_charge,date_load,file_name,file_size) =  ((select count(*)+1 from public.imports),now(),@nameOrgPasses," +
                    "@podrOrgPasses ,@INN ,@KPP,@fileNumber::integer,null,null,@lsCount::integer,null,now(),@passName,null)  where nzp_load = " +
                    nzp_load, header);
                if (string.IsNullOrEmpty(header.raschSchet))
                {
                    throw new Exception("Не определен Код расчетного центра");
                }
                #endregion

                #region Запуск процесса копирования данных
                var str =
                    string.Format(
                        "/C call \"" + AbsolutePath +
                        "\\part_payment.bat\" \"{0}\" \"{1}\" \"{2}\" \"{3}\" \"{4}\" \"{5}\" \"{6}\" \"{7}\" " +
                        "\"{8}\" \"{9}\" 2>\"" + logName + "\"", header.raschSchet, header.chargeDate, psqlPath, newPath.FullName, server, port, database, password, nzp_load, user);
                var info = new ProcessStartInfo("cmd.exe", str)
                {
                    StandardOutputEncoding = Encoding.GetEncoding(1251),
                    WorkingDirectory = Directory.GetCurrentDirectory(),
                    RedirectStandardOutput = true,
                    CreateNoWindow = true,
                    UseShellExecute = false
                };
                var proc = new Process
                {
                    EnableRaisingEvents = true,
                    StartInfo = info
                };
                proc.Start();
                proc.WaitForExit();
                proc.Close();
                proc.Dispose();
                reportList.Add(ReadLogsFromFile(parentDir));

                Update(Connection, decimal.Round(100m));
                SetProgress(decimal.Round(1m));
                #endregion
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
            finally
            {
                #region Удаление логов
                if (File.Exists(System.IO.Path.Combine(AbsolutePath ?? "", logName)))
                    File.Delete(System.IO.Path.Combine(AbsolutePath ?? "", logName));
                #endregion
            }
            #region Обновление данных в sys_imports
            var link = CreateProtocol(reportList);
            Connection.Execute("UPDATE sys_imports " +
                               " SET result='" + link.Replace("\'", "\'\'").Split('\\').Last() + "', status='" + "Завершено" +
                               "', statusid=" + (int)Statuses.Finished + ", progress=" + 100 + ", link='" +
                               link.Replace("\'", "\'\'") + "' " +
                               " WHERE nzp_load = " + nzp_load + "; ", null);
            #endregion

            return link;
        }

        #region Добавление записи в saldo_date
        protected void InsertIntoSaldoDate(Header header)
        {
            var date = DateTime.Parse(header.chargeDate);
            Connection.Execute(string.Format("Update public.saldo_date set active = 0 where kod_erc = {0} ",
                header.raschSchet.Trim()), null);
            Connection.Execute(string.Format("insert into public.saldo_date(kod_erc,saldo_month,saldo_year,saldo_date,active) values ({0},{1},{2},'{3}',{4})",
                header.raschSchet.Trim(), date.Month, date.Year, date.ToShortDateString(), 1), null);
        }
        #endregion

        #region Добавление записи в erc
        protected void InsertIntoERC(Header header)
        {
            if (Connection.ExecuteScalar<int>("select count(*) from public.erc where kod_erc = " + header.raschSchet.Trim(), null, null, null, null) == 0)
                Connection.Execute(string.Format("insert into public.erc(name_erc,kod_erc,dat_sys_start) values ('{0}',{1},'{2}')",
                   header.podrOrgPasses.Trim(), header.raschSchet.Trim(), header.datSysStart), null);
        }
        #endregion

        public string ReadLogsFromFile(string path)
        {
            var headerfileLines = File.ReadAllLines(logName, Encoding.UTF8);
            return string.Join("\r\n", headerfileLines);
        }

        public void Update(IDbConnection conn, decimal p)
        {
            conn.Execute("UPDATE sys_imports " +
                               " SET progress=" + p + ",result='Выполняется (" + p + "%)', status='Выполняется',link=null,statusid = 2 " +
                               " WHERE nzp_load = " + nzp_load + "; ", null);
        }

        #region Создание протокола
        public string CreateProtocol(List<string> list)
        {
            FileStream file = null;
            string fileName;
            var newFileName = FileName.Replace(".zip", "").Split('\\').Last();
            StreamWriter writer = null;
            try
            {
                newFileName += string.Format(" от {0}.txt", DateTime.Now.ToString("dd-MM-yyyy HH-mm-ss"));
                if (!File.Exists(System.IO.Path.Combine(Path, newFileName)))
                {
                    File.Copy(System.IO.Path.Combine(Path, FileName), GetPath(AbsolutePath) + "\\" + newFileName);
                }
                fileName = string.Format("{0}\\{1}", GetPath(AbsolutePath),
                    "Протокол сформированный при загрузке файла '" + newFileName.Replace(".txt", "") + "'.txt");
                if (File.Exists(fileName))
                    File.Delete(fileName);
                file = new FileStream(fileName, FileMode.CreateNew);
                writer = new StreamWriter(file);
                if (list != null)
                    foreach (var str in list)
                    {
                        writer.WriteLine(str);
                    }
            }
            catch (Exception ex)
            {
                return null;
            }
            finally
            {
                if (writer != null)
                {
                    writer.Flush();
                    writer.Close();
                }
                if (file != null)
                    file.Close();
            }
            SetProgress(1m);
            return fileName;
        }
        #endregion

        /// <summary>
        /// Путь до директории с файлами
        /// </summary>
        /// <returns>Директория с файлами</returns>
        public string GetPath(string Dir = "")
        {
            var parentDir = Dir == "" ? (new FileInfo(Assembly.GetEntryAssembly().Location)).Directory.Name : Dir;
            var directory = Directory.CreateDirectory(string.Format("{0}\\Download\\{1}\\{2}\\{3}",
                parentDir, DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day));
            return directory.FullName;
        }
    }


    [AssembleAttribute(FormatName = "ГИС", Version = "1.0", RegistrationName = "GIS")]
    public class GISFormat : Format
    {
        object obj = new object();
        protected class Str
        {
            public string batName { get; set; }
            public Dictionary<string, string> fields { get; set; }
            public Str()
            {
                fields = new Dictionary<string, string>();
            }
        }

        public Dictionary<string, string> Types = new Dictionary<string, string>
        {
            { "SERIAL", "integer" },
            { "INTEGER", "integer" },
            { "DECIMAL", "numeric" },
            { "DATE", "DATE" },
            { "SMALLINT", "SMALLINT" },
            { "DATETIME", "timestamp without time zone" },
            { "CHAR", "CHAR(255)" },
            { "FLOAT", "numeric" },
            { "NCHAR","CHAR(255)" },
            { "VARCHAR","CHAR(255)" },
            { "BYTE","BYTEA" }
        };

        public class Constraint
        {
            public string primary_table { get; set; }
            public string foreign_table { get; set; }
        }

        private string logName;
        public static void setCulture()
        //----------------------------------------------------------------------
        {
            var culture = new CultureInfo("ru-RU")
            {
                NumberFormat = { NumberDecimalSeparator = "." },
                DateTimeFormat = { ShortDatePattern = "dd.MM.yyyy", ShortTimePattern = "HH:mm:ss" }
            };
            Thread.CurrentThread.CurrentUICulture = culture;
            Thread.CurrentThread.CurrentCulture = culture;
        }

        protected override string Run()
        {
            AbsolutePath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "Loader");
            logName = System.IO.Path.Combine(AbsolutePath, "log" + nzp_load + ".txt");
            setCulture();
            var reportList = new List<string> { string.Format("Отчет о загрузке файла {0}", FileName) };
            SetProgress(0);
            Update(Connection, 0);
            string parentDir = null;
            DirectoryInfo newPath = null;
            DirectoryInfo scriptsPath = null;
            var errList = new List<string>();
            var updDict = new Dictionary<string, Str>();
            var delDict = new Dictionary<string, Str>();
            var mustCalcDict = new Dictionary<string, Str>();
            var packLsDict = new Dictionary<string, Str>();
            Header header;
            var schema_code = "";
            var schema_pref = "";
            string fileConstraint = null;
            try
            {
                #region Получение путей + Создание необходимых папок + Распаковка файлов из архива
                parentDir = string.IsNullOrEmpty(AbsolutePath) ? (new FileInfo(Assembly.GetEntryAssembly().Location)).Directory.FullName : AbsolutePath;
                newPath = Directory.CreateDirectory(string.Format("{0}\\LoadFiles\\{1}\\{2}\\{3}\\{4}\\" + nzp_load, parentDir, DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, FileName.Split('.').First()));
                scriptsPath = Directory.CreateDirectory(string.Format("{0}\\iso_folder.exp\\pg_" + nzp_load + "\\convert_" + nzp_load, parentDir));
                if (File.Exists(newPath.FullName + "\\" + FileName))
                    File.Delete(newPath.FullName + "\\" + FileName);
                File.Copy(System.IO.Path.Combine(Path, FileName), newPath.FullName + "\\" + FileName);
                Archive.GetInstance(newPath.FullName + "\\" + FileName).Decompress(newPath.FullName + "\\" + FileName, newPath.FullName);
                fileConstraint = System.IO.Path.Combine(scriptsPath.FullName, "constraints.txt");
                #endregion

                #region Обработка файла _main.txt

                string[] headerfileLines;
                try
                {
                    if (!File.Exists(newPath.FullName + "\\_main.txt"))
                    {
                        newPath = new DirectoryInfo(newPath.FullName + "\\" + FileName.Split('.').First());
                    }


                    #region Сохранение заголовка в БД

                    headerfileLines = File.ReadAllLines(newPath.FullName + "\\_main.txt", Encoding.GetEncoding(1251));
                    {
                        string[] firstLine;
                        try
                        {
                            firstLine = headerfileLines[0].Split('|');
                            schema_code = firstLine[6];
                            schema_pref = firstLine[7];
                            header = new Header
                            {
                                fileDate = firstLine[0],
                                nameOrgPasses = firstLine[3],
                                passNumber = firstLine[2],
                                passName = firstLine[1],
                                formatVersion = firstLine[4],
                                chargeDate = firstLine[5]
                            };
                        }
                        catch (Exception ex)
                        {
                            throw new Exception("Неверные параметры файла _main.txt");
                        }
                        reportList.Add(string.Format("Дата загрузки: {0}", DateTime.Now));
                        reportList.Add(string.Format("Месяц начисления: {0}", header.chargeDate));
                        reportList.Add(string.Format("Организация {0}; ИНН {1}, КПП {2}", header.nameOrgPasses,
                            header.INN, header.KPP));
                        Connection.Execute(
                            "Update public.imports set (num_load,dat_month,org_name,org_podr_name,org_inn,org_kpp,org_file_num,sender_phone," +
                            "sender_fio,count_ls,sum_charge,date_load,file_name,file_size) =  ((select count(*)+1 from public.imports),@chargeDate::date,@nameOrgPasses," +
                            "@podrOrgPasses ,@INN ,@KPP,@fileNumber::integer,null,null,@lsCount::integer,null,now(),@passName,null)  where nzp_load = " +
                            nzp_load, header);
                        date_charge = Convert.ToDateTime(header.chargeDate);
                        schema =
                            Connection.ExecuteScalar<string>("select pref from gkh_pref where upper(id_case) = upper('" + schema_code +
                                                             "')", null, null, null, null).Trim() + "_" + schema_pref;
                        if (string.IsNullOrEmpty(schema))
                            throw new Exception("Неопределена схема");
                        if (!new Db(connString).CheckGkh(schema_code, schema_pref,
                            Convert.ToDateTime(header.chargeDate)))
                        {
                            throw new Exception(
                                "Не возможно загрузить данные, т.к. в базе данных есть тне все связи. Порядок загрукм следующий: kernel,data,fin или charge");
                        }
                        InsertIntoGkhLoad(header.chargeDate);
                    }
                    #endregion
                }
                catch
                {
                    throw new Exception("Не верный формат архива");
                }

                #region Заполнение словарей с бат-файлами
                for (var i = 1; i < headerfileLines.Count(); i++)
                {
                    var split = headerfileLines[i].Split('|');

                    if (split[1].Trim() == "must_calc")
                    {
                        mustCalcDict.Add("must_calc", new Str { batName = "gkh_must_calc.bat" });
                        continue;
                    }
                    if (split[1].Trim() == "pack_ls")
                    {
                        packLsDict.Add("pack_ls", new Str { batName = "gkh_pack_ls.bat" });
                        continue;
                    }
                    if (split[0].Trim() == "del" || split[0].Trim() == "del_ins")
                    {
                        delDict.Add(split[1].Trim(), new Str { batName = "gkh_del.bat" });
                    }
                    else if (!split[1].Trim().Contains("recalc") || split[1].Trim().Contains("recalc") && split[1].Trim().Contains(date_charge.Year.ToString()))
                    {
                        updDict.Add(split[1].Trim(), new Str { batName = "gkh_upd.bat" });
                    }
                }
                #endregion



                #endregion

                #region Отключение триггеров для схемы data
                if (schema.Contains("data"))
                {

                    var info1 = new ProcessStartInfo("cmd.exe", string.Format("/C call \"" + AbsolutePath + "\\gkh_triggers_data_off.bat\" \"{0}\" \"{1}\" \"{2}\" \"{3}\" \"{4}\" \"{5}\" \"{6}\"",
                        schema, psqlPath, server, port, database, user, password))
                    {
                        StandardOutputEncoding = Encoding.UTF8,
                        WorkingDirectory = Directory.GetCurrentDirectory(),
                        RedirectStandardOutput = true,
                        CreateNoWindow = true,
                        UseShellExecute = false
                    };
                    var proc1 = new Process
                    {
                        EnableRaisingEvents = true,
                        StartInfo = info1
                    };
                    proc1.Start();
                    proc1.WaitForExit();
                    proc1.Close();
                }
                #endregion

                #region Отправка на выполнение скриптовых файлов
                #region Инициализация коллекции словарей + параметров командной строки
                var MainDict = new Dictionary<Dictionary<string, Str>, string>()
                {
                     {delDict,string.Format( "/C call \"" + AbsolutePath + "\\gkh_del.bat\" \"{0}\" \"{1}\" \"{2}\" \"{3}\" \"{4}\" \"{5}\" \"{8}\" \"{6}\" \"{7}\" 2>\"" + logName+"\"",schema, psqlPath, scriptsPath.FullName, server, port, database, password,nzp_load,user)},
                     {updDict,string.Format("/C call \"" + AbsolutePath + "\\gkh_upd.bat\" \"{0}\" \"{1}\" \"{2}\" \"{3}\" \"{4}\" \"{5}\" \"{9}\" \"{6}\" \"{7}\" \"{8}\" 2>\"" + logName+"\"", schema,psqlPath, scriptsPath.FullName, server, port, database, password,nzp_load,date_charge.ToShortDateString(),user)},
                     {packLsDict,string.Format("/C call \"" + AbsolutePath + "\\gkh_pack_ls.bat\" \"{0}\" \"{1}\" \"{2}\" \"{3}\" \"{4}\" \"{5}\" \"{8}\" \"{6}\" \"{7}\" 2>\"" + logName+"\"", schema,psqlPath, scriptsPath.FullName, server, port, database, password,nzp_load,user)},
                     {mustCalcDict,string.Format("/C call \"" + AbsolutePath + "\\gkh_must_calc.bat\" \"{0}\"  \"{1}\" \"{2}\" \"{3}\" \"{4}\" \"{5}\" \"{9}\" \"{6}\" \"{7}\" \"{8}\" 2>\"" + logName+"\"", schema,psqlPath,scriptsPath.FullName, server, port, database, password,nzp_load,date_charge.ToShortDateString(),user)}
                };

                var fileFields = File.ReadAllLines(newPath.FullName + "\\opis.csv", Encoding.GetEncoding(1251));
                for (var j = 0; j < MainDict.Count; j++)
                    for (var i = 0; i < fileFields.Count(); i++)
                    {
                        var split = fileFields[i].Split('|');
                        if (MainDict.ElementAt(j).Key.ContainsKey(split[0]))
                        {
                            try
                            {
                                if (split[2].Contains("order"))
                                    MainDict.ElementAt(j).Key[split[0]].fields.Add("\"" + split[2] + "\"", Types[split[3]]);
                                else if (split[2].Contains("pkod"))
                                    MainDict.ElementAt(j).Key[split[0]].fields.Add(split[2], "numeric(10,0)");
                                else if (split[3].Contains("DECIMAL") || split[3].Contains("FLOAT"))
                                    MainDict.ElementAt(j).Key[split[0]].fields.Add(split[2], split[3] + "(" + split[4] + ")");
                                else
                                    MainDict.ElementAt(j).Key[split[0]].fields.Add(split[2], Types[split[3]]);
                            }
                            catch (Exception ex)
                            {
                                throw new Exception(ex.Message);
                            }
                        }
                    }

                #endregion

                Update(Connection, 10m);
                SetProgress(0.1m);
                var inc = 1;
                foreach (var dict in MainDict)
                {
                    #region Перемещение файлов соответсвующего словаря
                    foreach (var file in dict.Key)
                    {
                        try
                        {
                            if (File.Exists(scriptsPath.FullName + "\\t_" + nzp_load + "_" + file.Key + ".unl"))
                                File.Delete(scriptsPath.FullName + "\\t_" + nzp_load + "_" + file.Key + ".unl");
                            File.Move(newPath.FullName + "\\" + file.Key + ".unl",
                                scriptsPath.FullName + "\\t_" + nzp_load + "_" + file.Key + ".unl");
                        }
                        catch (Exception ex)
                        {
                            errList.Add(string.Format("Ошибка при загрузке файлов:{0}", ex.Message.Split('\\').Last()));
                        }
                    }
                    #endregion

                    if (dict.Key.Count > 0)
                    {
                        var ConstrListReq = new List<string>();
                        ConstrListReq.AddRange(dict.Key.Select(x => x.Key).ToList());
                        var ConstrListQuery = Connection.Query<Constraint>(string.Format(@"    SELECT
	                    c1.relname AS primary_table,
	                    c2.relname AS foreign_table
                        FROM pg_catalog.pg_constraint c
                        JOIN ONLY pg_catalog.pg_class c1     ON c1.oid = c.confrelid
                        JOIN ONLY pg_catalog.pg_class c2     ON c2.oid = c.conrelid
                        JOIN ONLY pg_catalog.pg_namespace n1 ON n1.oid = c1.relnamespace
                        JOIN ONLY pg_catalog.pg_namespace n2 ON n2.oid = c2.relnamespace
                        WHERE c1.relkind = 'r' AND c.contype = 'f' and n1.nspname =  '{0}' 
                        ORDER BY 1,2;", schema), null);
                        //удаление лишних записей
                        var ConstrListQuery1 = new List<Constraint>();
                        ConstrListQuery1.AddRange(ConstrListQuery.ToList());
                        var primaryList = ConstrListQuery1.Where(y => ConstrListReq.Contains(y.primary_table)).ToList();
                        ConstrListQuery = ConstrListQuery.Where(x => ConstrListReq.Contains(x.foreign_table)).Distinct().ToList();
                        var ResultList = new List<string>();
                        ResultList.AddRange(ConstrListReq.Where(x => !ConstrListQuery.Select(y => y.foreign_table).Contains(x) && !(primaryList.Select(z => z.primary_table)).Contains(x)).ToList());
                        var TempList = ConstrListQuery.Select(x => x.foreign_table).Distinct().ToList();
                        while (TempList.Any())
                        {
                            ResultList.AddRange(TempList);
                            TempList = TempList.Where(x => ConstrListQuery.Select(y => y.primary_table).Contains(x)).Distinct().ToList();
                            TempList.ForEach(x => ResultList.Remove(x));
                        }

                        ResultList.AddRange(primaryList.Select(x => x.primary_table).Distinct());
                        ResultList.Reverse();
                        if (File.Exists(fileConstraint))
                            File.Delete(fileConstraint);
                        using (var f = File.Create(fileConstraint))
                        {
                            using (var sw = new StreamWriter(f))
                            {
                                ResultList.ForEach(sw.WriteLine);
                                sw.Flush();
                            }
                            f.Close();
                        }

                        #region Создание темповых таблиц
                        var sql1 = dict.Key.Aggregate("",
                               (current, d) =>
                                   current + string.Format("drop table if exists public.t_{0}_{1};", d.Key, nzp_load));
                        Connection.Execute(sql1, null);
                        foreach (var d in dict.Key)
                        {
                            var sql = string.Format("Create table public.t_{0}_{1}(", d.Key, nzp_load);
                            var new_f = d.Value.fields.Select(x => x.Key + " " + x.Value).ToArray();
                            sql += string.Join(", ", new_f);
                            sql += ");";
                            Connection.Execute(sql, null);
                        }
                        #endregion

                        #region Перекодировка файлов

                        var s = "/C call \"" + AbsolutePath + "\\ISOto1251.exe\" \"" + nzp_load + "\"";
                        var info1 = new ProcessStartInfo("cmd.exe", s)
                        {
                            WorkingDirectory = Directory.GetCurrentDirectory(),
                            CreateNoWindow = true,
                            UseShellExecute = false
                        };
                        var proc1 = new Process
                        {
                            EnableRaisingEvents = true,
                            StartInfo = info1
                        };
                        proc1.Start();
                        proc1.WaitForExit();
                        proc1.Close();
                        #endregion

                        #region Запуск процесса копирования данных
                        var str = dict.Value;
                        var info = new ProcessStartInfo("cmd.exe", str);
                        info = new ProcessStartInfo("cmd.exe", str)
                        {
                            StandardOutputEncoding = Encoding.UTF8,
                            WorkingDirectory = Directory.GetCurrentDirectory(),
                            RedirectStandardOutput = true,
                            CreateNoWindow = true,
                            UseShellExecute = false
                        };
                        var proc = new Process
                        {
                            EnableRaisingEvents = true,
                            StartInfo = info
                        };
                        proc.Start();
                        var succ = proc.StandardOutput.ReadToEnd();
                        reportList.Add(succ);
                        proc.WaitForExit();
                        proc.Close();
                        errList.Add(ReadLogsFromFile(parentDir));
                        Update(Connection, (decimal)inc / MainDict.Count * 100);
                        SetProgress((decimal)inc / MainDict.Count);
                        #endregion

                        #region Insert from temp table

                        var error = false;
                        try
                        {
                            var suc = "--------------------------------------------------------------------------------\r\n";

                            foreach (var d in ResultList)
                            {
                                try
                                {
                                    var sql = string.Format("Insert into {1}.{0} (", d.Replace("recalc_" + date_charge.Year + "_", ""), schema);
                                    var new_f = dict.Key[d].fields.Select(x => x.Key).ToArray();
                                    sql += string.Join(", ", new_f);
                                    sql += string.Format(") select * from public.t_{0}_{1}", d, nzp_load);
                                    Connection.Execute(sql, null, null, 1000000, null);
                                    suc += string.Format("Данные для таблицы {0} загружены\r\n", d);
                                }
                                catch (Exception ex)
                                {
                                    error = true;
                                    suc += string.Format("Данные для таблицы {0} не загружены\r\n", d);
                                    errList.Add("Таблица " + d + " :" + ex.Message);
                                }
                            }
                            suc += "--------------------------------------------------------------------------------\r\n";
                            reportList.Add(suc);
                        }
                        finally
                        {
                            var sql = dict.Key.Aggregate("",
                                (current, d) =>
                                    current + string.Format("drop table if exists public.t_{0}_{1};", d.Key, nzp_load));
                            Connection.Execute(sql, null);
                            if (!error) InsertIntoGkhCheck(Convert.ToDateTime(header.chargeDate), schema_code, schema_pref);
                        }

                        #endregion
                    }

                    #region Удаление файлов из директории
                    try
                    {
                        foreach (var file in dict.Key)
                        {
                            try
                            {
                                if (File.Exists(scriptsPath.FullName + "\\t_" + nzp_load + "_" + file.Key + ".unl"))
                                    File.Delete(scriptsPath.FullName + "\\t_" + nzp_load + "_" + file.Key + ".unl");
                            }
                            catch (Exception ex)
                            {
                                errList.Add(string.Format("Ошибка при загрузке файлов:{0}", ex.Message.Split('\\').Last()));
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        errList.Add("Ошибка:" + ex.Message);
                    }
                    inc++;
                    #endregion
                }
                #endregion

                #region Включение триггеров для схемы data
                if (schema.Contains("data"))
                {

                    var info1 = new ProcessStartInfo("cmd.exe", string.Format("/C call \"" + AbsolutePath + "\\gkh_triggers_data_on.bat\" \"{0}\" \"{1}\" \"{2}\" \"{3}\" \"{4}\" \"{5}\" \"{6}\"",
                        schema, psqlPath, server, port, database, user, password))
                    {
                        WorkingDirectory = Directory.GetCurrentDirectory(),
                        CreateNoWindow = true,
                        UseShellExecute = false
                    };
                    var proc1 = new Process
                    {
                        EnableRaisingEvents = true,
                        StartInfo = info1
                    };
                    proc1.Start();
                    proc1.WaitForExit();
                    proc1.Close();
                }
                reportList.AddRange(errList);
                #endregion
                //-------------------------------------------------
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                #region Удаление логов
                if (File.Exists(fileConstraint))
                    File.Delete(fileConstraint);
                if (File.Exists(System.IO.Path.Combine(AbsolutePath ?? "", logName)))
                    File.Delete(System.IO.Path.Combine(AbsolutePath ?? "", logName));
                DeleteFileFromDirectory(scriptsPath.FullName);
                DeleteFileFromDirectory(newPath.FullName);
                #endregion
            }
            #region Обновление данных в sys_imports
            Update(Connection, 95m);
            var link = "";
            SetProgress(0.95m);
            lock (obj)
            {
                link = CreateProtocol(reportList);
                Connection.Execute("UPDATE sys_imports " +
                                   " SET result='" + link.Replace("\'", "\'\'").Split('\\').Last() + "', status='" +
                                   "Завершено" +
                                   "', statusid=" + (int)Statuses.Finished + ", progress=" + 100 + ", link='" +
                                   link.Replace("\'", "\'\'") + "' " +
                                   " WHERE nzp_load = " + nzp_load + "; ", null);
            }
            #endregion
            return link;
        }

        public void Update(IDbConnection conn, decimal p)
        {
            lock (obj)
            {
                conn.Execute("UPDATE sys_imports " +
                             " SET progress=" + p + ",result='Выполняется (" + p + "%)', status='Выполняется',link=null,statusid = 2" +
                             " WHERE nzp_load = " + nzp_load + "; ", null);
            }
        }

        #region Создание протокола
        public string CreateProtocol(List<string> list)
        {
            FileStream file = null;
            string fileName;
            var newFileName = FileName.Replace(".zip", "").Split('\\').Last();
            StreamWriter writer = null;
            try
            {
                newFileName += string.Format(" от {0}.txt", DateTime.Now.ToString("dd-MM-yyyy HH-mm-ss"));
                //if (!File.Exists(Path + newFileName))
                //{
                //    File.Copy(Path + FileName, GetPath(AbsolutePath) + "\\" + newFileName);
                //}
                fileName = string.Format("{0}\\{1}", GetPath(AbsolutePath),
                    "Протокол сформированный при загрузке файла '" + newFileName.Replace(".txt", "") + "'.txt");
                if (File.Exists(fileName))
                    File.Delete(fileName);
                file = new FileStream(fileName, FileMode.CreateNew);
                writer = new StreamWriter(file);
                if (list != null)
                    foreach (var str in list)
                    {
                        writer.WriteLine(str);
                    }
            }
            catch (Exception ex)
            {
                return null;
            }
            finally
            {
                if (writer != null)
                {
                    writer.Flush();
                    writer.Close();
                }
                if (file != null)
                    file.Close();
            }
            Update(Connection, 1m);
            SetProgress(1m);
            return fileName;
        }
        #endregion

        /// <summary>
        /// Путь до директории с файлами
        /// </summary>
        /// <returns>Директория с файлами</returns>
        public string GetPath(string Dir = "")
        {
            var parentDir = Dir == "" ? (new FileInfo(Assembly.GetEntryAssembly().Location)).Directory.Name : Dir;
            var directory = Directory.CreateDirectory(string.Format("{0}\\Download\\{1}\\{2}\\{3}",
                parentDir, DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day));
            return directory.FullName;
        }

        public string ReadLogsFromFile(string path)
        {
            var headerfileLines = File.ReadAllLines(logName, Encoding.UTF8);
            return string.Join("\r\n", headerfileLines);
        }

        public void InsertIntoGkhLoad(string date, string schema_code = "null")
        {
            Connection.Execute("delete from public.gkh_load where nzp_load = " + nzp_load, null);
            if (Connection.ExecuteScalar<int>(
                    string.Format("Select count(*) from public.gkh_load where date_charge > '{0}' and schema_code = {1}", date, schema_code), null, null, null, null) != 0)
            {
                throw new Exception("Загружен архив более поздней версии");
            }
            if (Connection.ExecuteScalar<int>(
                    string.Format("Select count(*) from public.gkh_load where date_charge = '{0}' and schema_code = {1}", date, schema_code), null, null, null, null) != 0)
            {
                Connection.Execute(
                    string.Format(
                        "Update public.gkh_load set is_actual = 100 where date_charge = '{0}' and schema_code = {1}", date, schema_code), null);
            }
            Connection.Execute(
                    string.Format(
                        "Insert into public.gkh_load(date_charge,schema_code,nzp_load,is_actual) values ('{0}',{1},{2},1)", date, schema_code, nzp_load), null);
        }

        public void InsertIntoGkhCheck(DateTime charge_date, string case_id, string bank)
        {
            Connection.Execute("delete from gkh_check where charge_date = '" + charge_date + "' and case_id ='" + case_id + "' and bank = '" + bank + "'", null);
            Connection.Execute("insert into gkh_check(charge_date,case_id,bank) values ('" + charge_date + "','" + case_id + "','" + bank + "')", null);
        }

        public void DeleteFileFromDirectory(string path)
        {
            if (Directory.Exists(path))
            {
                var downloadedMessageInfo = new DirectoryInfo(path);

                foreach (var file in downloadedMessageInfo.GetFiles())
                {
                    file.Delete();
                }
                foreach (var dir in downloadedMessageInfo.GetDirectories())
                {
                    dir.Delete(true);
                }
                Directory.Delete(path);
            }
        }
    }
}
