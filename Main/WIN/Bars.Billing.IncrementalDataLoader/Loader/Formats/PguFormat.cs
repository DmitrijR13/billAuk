using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Bars.Billing.IncrementalDataLoader.Utils;
using Bars.Billing.IncrementalDataLoader.Utils.ArchiveUtils;
using Bars.Billing.IncrementalDataLoader.Utils.DbUtils;
using Dapper;
using Npgsql;

namespace Bars.Billing.IncrementalDataLoader.Formats
{
    [Assembly(FormatName = "ПГУ", Version = "1.0", RegistrationName = "PGU")]
    public class PguFormat : Format
    {
        private string logName;

        /// <summary>
        /// Установка региональных настроек
        /// </summary>
        private static void SetCulture()
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
            LogUtils.WriteLog(String.Format("Старт метода загрузки."), ELogType.Debug);

            AbsolutePath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "Loader");
            logName = System.IO.Path.Combine(AbsolutePath, "log" + nzp_load + ".txt");
            SetCulture();
            SetProgress(0);
            Update(Connection, 0);
            AbsolutePath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "Loader");
            #region Получение путей + Создание необходимых папок + Распаковка файлов из архива
            var reportList = new List<string> { string.Format("Отчет о загрузке файла {0}", FileName) };
            reportList.Add("Уникальный код загрузки: " + nzp_load);
            var parentDir = string.IsNullOrEmpty(AbsolutePath) ? (new FileInfo(Assembly.GetEntryAssembly().Location)).Directory.FullName : AbsolutePath;
            var newPath = Directory.CreateDirectory(string.Format("{0}\\LoadFiles\\{1}\\{2}\\{3}\\{4}", parentDir, DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, FileName.Split('.').First()));
            Directory.CreateDirectory(string.Format("{0}\\iso_folder.exp\\pg", parentDir));
            if (File.Exists(newPath.FullName + "\\" + FileName))
                File.Delete(newPath.FullName + "\\" + FileName);
            File.Copy(System.IO.Path.Combine(Path, FileName), newPath.FullName + "\\" + FileName);
            Archive.GetInstance(newPath.FullName + "\\" + FileName).Decompress(newPath.FullName + "\\" + FileName, newPath.FullName);
            #endregion

            int exitCode = -1;
            int loadedStatusId = (int)Statuses.Execute;
            var link1 = "";
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

                Connection.Execute(
                    "Update public.erc set (dat_sys_start) =  ('" + header.datSysStart + "')  where erc_code = " +
                    header.raschSchet.Trim());
                //InsertIntoERC(header);

                reportList.Add(string.Format("Дата загрузки: {0}", DateTime.Now));
                reportList.Add(string.Format("Месяц начисления: {0}", header.chargeDate));
                reportList.Add(string.Format("Организация {0}; ИНН {1}, КПП {2}", header.nameOrgPasses, header.INN,
                    header.KPP));
                Connection.Execute(
                    "UPDATE public.imports SET (num_load,dat_month,org_name,org_podr_name,org_inn,org_kpp,org_file_num,sender_phone," +
                    "sender_fio,count_ls,sum_charge,date_load,file_name,file_size) =  ((select count(*)+1 from public.imports),now(),@nameOrgPasses," +
                    "@podrOrgPasses ,@INN ,@KPP,@fileNumber::integer,null,null,@lsCount::integer,null,now(),@passName,null)  where nzp_load = " +
                    nzp_load, header);
                if (string.IsNullOrEmpty(header.raschSchet))
                {
                    throw new Exception("Не определен код расчетного центра");
                }
                if (
                    Connection.ExecuteScalar<int>("select count(*) from public.erc where erc_code = " +
                                                  header.raschSchet.Trim()) == 0)
                {
                    throw new Exception("Не найден соответствующий код ЕРЦ, проверьте правильность загружаемого файла");
                }

                if (!Regex.IsMatch(header.formatVersion.Trim(), "^1.1"))
                {

                    var errorText = String.Format(
                        "Неактуальная версия формата загрузки! Версия загруженного файла: '{1}'{0}" +
                        "Необходимо обновить программное обеспечение и повторно выгрузить файл.",
                        Environment.NewLine,
                        header.formatVersion.Trim());

                    throw new Exception(errorText);
                }

                #endregion

                #region Запуск процесса копирования данных
                LogUtils.WriteLog(String.Format("Старт процесса копирования данных."), ELogType.Debug);

                var str =
                    string.Format(
                        "/C call \"" + AbsolutePath +
                        "\\part_payment.bat\" \"{0}\" \"{1}\" \"{2}\" \"{3}\" \"{4}\" \"{5}\" \"{6}\" \"{7}\" " +
                        "\"{8}\" \"{9}\" >> \"" + logName + "\" 2>&1 ", header.raschSchet, header.chargeDate, PsqlPath,
                        newPath.FullName, Server, Port, Database, Password, nzp_load, User);

                LogUtils.WriteLog(
                    "В методе '" + System.Reflection.MethodBase.GetCurrentMethod() + "' вызывается команда: '" + str +
                    "'",
                    ELogType.Debug);
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
                exitCode = proc.ExitCode;
                proc.Close();
                proc.Dispose();

                LogUtils.WriteLog(String.Format("Процесса копирования данных завершился с кодом: {0} ", exitCode), ELogType.Debug);

                //Считывание логов, которые выдал psql
                reportList.Add(ReadLogsFromFile(parentDir));

                //отчет по некорректным данным
                //reportList.Add(String.Join("\r\n", GetUncorrectData(Connection, header)));

                #endregion

                /*
                var schemaName = "public";

                var dict = new Dictionary<string, string>
                {
                    {"ChargExpenseServ", "charge"},
                    {"Payment", "payments"},
                    {"PaymentDetails", "billinfo"},
                    {"Counters", "counters"},
                    {"InfoSocProtection", "sz"},
                    {"CharacterGilFond", "parameters"}
                };


                var executor = new SqlExecutor.SqlExecutor(ConnString);

                foreach (var file in Directory.EnumerateFiles(newPath.FullName, "*.txt"))
                {
                    //по названию файла выбираем соответствующую таблицу
                    var originalTableName = "";
                    dict.TryGetValue(System.IO.Path.GetFileNameWithoutExtension(file), out originalTableName);

                    //данные кладутся в партиционированные таблицы
                    var newTableName = String.Format("{0}.{1}_{2}_{3}",
                        schemaName,
                        originalTableName,
                        header.raschSchet,
                        Convert.ToDateTime(header.chargeDate).ToString("yyyyMM"));

                    try
                    {
                        var query = " DROP TABLE IF EXISTS " + newTableName + " CASCADE ";
                        executor.ExecuteSql(query);

                        //создаем партиционированную таблицу
                        query =
                            String.Format(
                                "CREATE TABLE IF NOT EXISTS " +
                                " {0} " +
                                " (LIKE {1} INCLUDING ALL) " +
                                " INHERITS({1}) WITH (OIDS=TRUE) ",
                                newTableName,
                                originalTableName
                                );
                        executor.ExecuteSql(query);


                        query = String.Format(
                            "COPY {0} FROM stdin WITH DELIMITER AS '|' NULL AS '' ENCODING 'WIN-1251' ",
                            newTableName
                            );

                        using (var stream = File.OpenRead(file))
                        {
                            //передача потока напрямую в БД
                            executor.CopyIn(query, stream);
                        }
                        reportList.Add(String.Format("Файл '{0}' успешно загружен. ",
                            System.IO.Path.GetFileNameWithoutExtension(file)));
                    }
                    catch (NpgsqlException ex)
                    {
                        //удаляем все таблицы
                        foreach (var oneTable in dict.Values)
                        {
                            var tableName = String.Format("{0}.{1}_{2}_{3}",
                                schemaName,
                                oneTable,
                                header.raschSchet,
                                Convert.ToDateTime(header.chargeDate).ToString("yyyyMM"));

                            var query = " DROP TABLE IF EXISTS " + tableName + " CASCADE ";
                            executor.ExecuteSql(query);
                        }

                        //пишем в протокол загрузки
                        var error = String.Format(
                            "Сообщение об ошибке:'{0}'.\n Местоположение некорректных данных:'{1}'",
                            ex.BaseMessage,
                            ex.Where);
                        reportList.Add(error);
                        throw new Exception(error, ex);
                    }
                }

                */
                if (exitCode < 0)
                {
                    loadedStatusId = (int)Statuses.Error;
                    throw new Exception("Ошибка при загрузке данных в БД! Смотрите лог-файл");
                }

                //вставляем в saldo_date только при успешной загрузке
                InsertIntoSaldoDate(header);
                loadedStatusId = (int)Statuses.Finished;
            }
            catch (Exception ex)
            {
                reportList.Add(ex.Message);
                loadedStatusId = (int)Statuses.Error;
                throw new Exception("Ошибка при загрузке! Смотрите лог-файл", ex);
            }
            finally
            {
                try
                {
                    //очищаем временную папку полностью
                    var directoryInfo = new DirectoryInfo(AbsolutePath);
                    foreach (var file in directoryInfo.GetFiles())
                    {
                        file.Delete();
                    }

                    foreach (var dir in directoryInfo.GetDirectories())
                    {
                        dir.Delete(true);
                    }
                }
                catch
                {
                }

                link1 = CreateProtocol(reportList);
                Connection.Execute(
                    " UPDATE sys_imports " +
                    " SET result='" + link1.Replace("\'", "\'\'").Split('\\').Last() + "', " +
                    " statusid=" + loadedStatusId + ", " +
                    " status ='Завершено', " +
                    " progress = " + 100 + ", " +
                    " link='" + link1.Replace("\'", "\'\'") + "' " +
                    " WHERE nzp_load = " + nzp_load + "; ");
            }
            return link1;
        }


        /// <summary>
        /// Добавление записи в saldo_date
        /// </summary>
        /// <param name="header"></param>
        private void InsertIntoSaldoDate(Header header)
        {
            var date = DateTime.Parse(header.chargeDate);
            if (
                //если загружается месяц, который раньше последнего загруженного
                Connection.ExecuteScalar<DateTime>(
                    "SELECT COALESCE(CAST(MAX(saldo_date) AS DATE), '1900-1-1') FROM public.saldo_date WHERE active = 1 AND erc_code = " +
                    header.raschSchet.Trim()) > date
                )
            {
                //вставляем запись, но делаем ее НЕ активной
                Connection.Execute(
                    string.Format(
                        "INSERT INTO public.saldo_date(erc_code,saldo_month,saldo_year,saldo_date,active) VALUES ({0},{1},{2},'{3}',{4})",
                        header.raschSchet.Trim(), date.Month, date.Year, date.ToShortDateString(), 0));
            }
            else
            {
                //делаем все записи НЕактивными, и вставляем новую запись активную
                Connection.Execute(string.Format("UPDATE public.saldo_date SET active = 0 WHERE erc_code = {0} ",
                    header.raschSchet.Trim()), null);
                Connection.Execute(
                    string.Format(
                        "INSERT INTO public.saldo_date(erc_code,saldo_month,saldo_year,saldo_date,active) VALUES ({0},{1},{2},'{3}',{4})",
                        header.raschSchet.Trim(), date.Month, date.Year, date.ToShortDateString(), 1));
            }
        }


        /// <summary>
        /// Считывание логов, которые выдал psql
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        private string ReadLogsFromFile(string path)
        {
            var headerfileLines = File.ReadAllLines(logName, Encoding.UTF8);
            return string.Join("\r\n", headerfileLines);
        }

        /// <summary>
        /// Обновление статуса загрузки
        /// </summary>
        /// <param name="conn"></param>
        /// <param name="p"></param>
        private void Update(IDbConnection conn, decimal p)
        {
            conn.Execute(
                "UPDATE sys_imports " +
                " SET progress=" + p + ",result='Выполняется (" + p +
                "%)', status='Выполняется',link=null,statusid =  " + Statuses.Execute.GetHashCode() +
                " WHERE nzp_load = " + nzp_load + "; ");
        }

        /// <summary>
        /// Отчет по загруженным данных
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="header"></param>
        /// <returns></returns>
        private IEnumerable<string> GetUncorrectData(IDbConnection connection, Header header)
        {
            var resultList = new List<string>();
            try
            {
                var tempTableName = String.Format("t_{0}_check_loaded_data_{1}", header.raschSchet.Trim(),
                    DateTime.Now.Ticks);


                var tablePostfix = String.Format("_{0}_{1}", header.raschSchet.Trim(),
                    DateTime.Parse(header.chargeDate).ToString("yyyyMM"));

                var sqlQuery = "";

                sqlQuery = String.Format(" DROP TABLE IF EXISTS {0};", tempTableName);
                connection.Execute(sqlQuery);

                sqlQuery = String.Format(
                    " CREATE TEMP TABLE {0} " +
                    " (error_type INTEGER,   " +
                    " erc_code BIGINT NOT NULL," +
                    " pkod BIGINT NOT NULL,  " +
                    " fio CHARACTER(100),    " +
                    " nkvar character(10),   " +
                    " regexp_result text[]); ",
                    tempTableName);
                connection.Execute(sqlQuery, commandTimeout: 3600);

                //поле "ФИО" не заполнено (ПГУ не даст авторизоваться)
                sqlQuery = String.Format(
                    " INSERT INTO  {0} (error_type, erc_code, pkod, fio, nkvar) " +
                    " SELECT 1, erc_code, pkod, fio, nkvar " +
                    " FROM parameters{1} WHERE fio IS NULL; ",
                    tempTableName,
                    tablePostfix);
                connection.Execute(sqlQuery, commandTimeout: 3600);


                //поле "ФИО" заполнено некорректными символами (ПГУ не даст авторизоваться)
                //(символ '-' ПГУ разрешает ввести)
                sqlQuery = String.Format(
                    " INSERT INTO  {0} (error_type, erc_code, pkod, fio, nkvar, regexp_result) " +
                    @" SELECT 2, erc_code, pkod, fio, nkvar, regexp_matches(fio, '^[_|=|.|,|;|!|?|@|#|№|/|\|*]+$') FROM parameters{1}; ",
                    tempTableName, tablePostfix);
                connection.Execute(sqlQuery, commandTimeout: 3600);

                //в поле "ФИО" имеются английские буквы (ПГУ не даст авторизоваться)
                sqlQuery = String.Format(
                    " INSERT INTO  {0} (error_type, erc_code, pkod, fio, nkvar, regexp_result) " +
                    @" SELECT 3, erc_code, pkod, fio, nkvar, regexp_matches(fio, '^[a-zA-Z]+$') FROM parameters{1}; ",
                    tempTableName, tablePostfix);
                connection.Execute(sqlQuery, commandTimeout: 3600);

                //поле "номер квартиры" не заполнено
                sqlQuery = String.Format(
                    " INSERT INTO  {0} (error_type, erc_code, pkod, fio, nkvar) " +
                    @" SELECT 4, erc_code, pkod, fio, nkvar  FROM parameters{1} WHERE nkvar IS NULL; ",
                    tempTableName, tablePostfix);
                connection.Execute(sqlQuery, commandTimeout: 3600);

                //поле "номер квартиры" заполнено некорректными символами (ПГУ не даст авторизоваться)
                //(символ '-' ПГУ разрешает ввести)
                sqlQuery = String.Format(
                    " INSERT INTO  {0} (error_type, erc_code, pkod, fio, nkvar, regexp_result) " +
                    @" SELECT 5, erc_code, pkod, fio, nkvar, regexp_matches(nkvar, '^[_|=|;|!|?|@|#|№|*]+$')  FROM parameters{1} WHERE nkvar IS NULL; ",
                    tempTableName, tablePostfix);
                connection.Execute(sqlQuery, commandTimeout: 3600);


                //в поле "номер квартиры" имеются английские буквы (ПГУ не даст авторизоваться)
                sqlQuery = String.Format(
                    " INSERT INTO  {0} (error_type, erc_code, pkod, fio, nkvar, regexp_result) " +
                    @" SELECT 6, erc_code, pkod, fio, nkvar, regexp_matches(nkvar, '^[a-zA-Z]+$')  FROM parameters{1} WHERE nkvar IS NULL; ",
                    tempTableName, tablePostfix);
                connection.Execute(sqlQuery, commandTimeout: 3600);

                //длина платкода не равно 10 знакам 
                sqlQuery = String.Format(
                    " INSERT INTO  {0} (error_type, erc_code, pkod, fio, nkvar) " +
                    @" SELECT 7, erc_code, pkod, fio, nkvar FROM parameters{1} WHERE LENGTH(pkod||'')!=10; ",
                    tempTableName, tablePostfix);
                connection.Execute(sqlQuery, commandTimeout: 3600);

                //нет начислений 
                sqlQuery = String.Format(
                    " INSERT INTO  {0} (error_type, erc_code, pkod, fio, nkvar) " +
                    @" SELECT 8, erc_code, pkod, fio, nkvar FROM parameters{1} p " +
                    " WHERE NOT EXISTS " +
                    " (SELECT 1 FROM charge{1} c  WHERE p.pkod = c.pkod AND p.dat_month = c.dat_month)" +
                    " AND p.account_state = 1;",
                    tempTableName, tablePostfix);
                connection.Execute(sqlQuery, commandTimeout: 3600);

                //нет показаний ИПУ (через ПГУ пользователь не сможет ввести показаний ИПУ)
                sqlQuery = String.Format(
                    " INSERT INTO  {0} (error_type, erc_code, pkod, fio, nkvar) " +
                    @" SELECT 9, erc_code, pkod, fio, nkvar FROM parameters{1} p " +
                    " WHERE NOT EXISTS " +
                    " (SELECT 1 FROM counters{1} c  WHERE p.pkod = c.pkod AND p.dat_month = c.dat_month) " +
                    " AND p.account_state = 1;",
                    tempTableName, tablePostfix);
                connection.Execute(sqlQuery, commandTimeout: 3600);

                //не проставлена "Группа услуг" у начисления (будет некорректная группировка при формировании ЕПД)
                sqlQuery = String.Format(
                    " INSERT INTO  {0} (error_type, erc_code, pkod) " +
                    @" SELECT 10, erc_code, pkod FROM charge{1} c " +
                    " WHERE nzp_serv_base IS NULL and serv_group IS NULL GROUP BY 2,3;",
                    tempTableName, tablePostfix);
                connection.Execute(sqlQuery, commandTimeout: 3600);

                sqlQuery =
                    String.Format(
                        " SELECT pkod AS AccountPaymentCode, " +
                        " TRIM(fio) AS Fio, " +
                        " TRIM(nkvar) AS FlatNumber," +
                        " CASE 	WHEN error_type = 1 THEN 'Поле ''ФИО'' не заполнено' " +
                        " 	WHEN error_type = 2 THEN 'Поле ''ФИО'' заполнено некорректными символами' " +
                        " 	WHEN error_type = 3 THEN 'В поле ''ФИО'' имеются английские буквы' " +
                        " 	WHEN error_type = 4 THEN 'Поле ''номер квартиры'' не заполнено' " +
                        " 	WHEN error_type = 5 THEN 'Поле ''номер квартиры'' заполнено некорректными символами' " +
                        " 	WHEN error_type = 6 THEN 'В поле ''номер квартиры'' имеются английские буквы' " +
                        " 	WHEN error_type = 7 THEN 'Длина платежного кода не равно 10 знакам' " +
                        " 	WHEN error_type = 8 THEN 'Нет начислений' " +
                        " 	WHEN error_type = 9 THEN 'Нет показаний ИПУ' " +
                        " 	WHEN error_type = 10 THEN 'Не проставлена ''Группа услуг'' в начиcлениях' " +
                        " END AS ErrorText, " +
                        " erc_code AS ErcCode " +
                        " FROM {0}" +
                        " ORDER BY pkod, fio, nkvar, error_type;",
                        tempTableName);
                var uncorrectData = connection.Query<LoadProtocolEntity>(sqlQuery, commandTimeout: 3600);

                resultList.Add("Информация по некорректным данным: ");

                foreach (var item in uncorrectData)
                {
                    resultList.Add("------------------------------------");
                    resultList.Add(String.Format("Платежный код: {0}", item.AccountPaymentCode));
                    resultList.Add(String.Format("ФИО Квартиросъемщика / Собственника / Нанимателя: {0}", item.Fio));
                    resultList.Add(String.Format("Номер квартиры: {0}", item.FlatNumber));
                    resultList.Add(String.Format("Текст ошибки: {0}", item.ErrorText));
                    resultList.Add(String.Format("Код ЕРЦ: {0}", item.ErcCode));
                    resultList.Add(String.Format("Расчетный месяц: {0}", header.chargeDate.Trim()));
                    resultList.Add("------------------------------------");
                }

            }
            catch (Exception ex)
            {
                throw;
            }

            return resultList;
        }

        /// <summary>
        /// Создание протокола
        /// </summary>
        /// <param name="list"></param>
        /// <returns></returns>

        private string CreateProtocol(List<string> list)
        {
            FileStream file = null;
            string fileName;
            var newFileName = FileName.Replace(".zip", "").Split('\\').Last();
            StreamWriter writer = null;
            try
            {
                newFileName += string.Format(" от {0}.txt", DateTime.Now.ToString("dd-MM-yyyy HH-mm-ss"));

                //old. непонятно зачем это надо было.
                if (!File.Exists(System.IO.Path.Combine(Path, newFileName)))
                {
                    File.Copy(System.IO.Path.Combine(Path, FileName), GetPath(AbsolutePath) + "\\" + newFileName);
                }

                fileName = string.Format("{0}\\{1}", GetPath(AbsolutePath),
                    "Протокол сформированный при загрузке файла '" + newFileName.Replace(".txt", "") + "'.txt");
                if (File.Exists(fileName))
                    File.Delete(fileName);
                file = new FileStream(fileName, FileMode.CreateNew);
                writer = new StreamWriter(file, Encoding.GetEncoding(1251));
                if (list != null)
                    foreach (var str in list)
                    {
                        writer.WriteLine(str);
                    }
            }
            catch
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

        /// <summary>
        /// Путь до директории с файлами
        /// </summary>
        /// <returns>Директория с файлами</returns>
        private string GetPath(string Dir = "")
        {
            var parentDir = Dir == "" ? (new FileInfo(Assembly.GetEntryAssembly().Location)).Directory.Name : Dir;
            var directory = Directory.CreateDirectory(string.Format("{0}\\Download\\{1}\\{2}\\{3}",
                parentDir, DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day));
            return directory.FullName;
        }
    }

}
