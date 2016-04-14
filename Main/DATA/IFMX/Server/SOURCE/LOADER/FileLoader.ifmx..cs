using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.IO;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;
using SevenZip;
using JCS;
using STCLINE.KP50.Utility;
using Bars.KP50.Utils;
using STCLINE.KP50.DataBase;



namespace STCLINE.KP50.DataBase 
{
    public class DBValuesFromFile
    {
        public FilesImported finder{ get; set; }
        public string rowNumber { get; set; }
        public string Pvers { get; set; }
        public string versFull { get; set; }
        public StringBuilder err { get; set; }
        public string[] vals { get; set; }       
        public string sql { get; set; }
        public Returns ret { get; set; }
        public bool load13section { get; set; }
        public int sectionNumber { get; set; }  
    }

    public class DbFileLoader : DbAdminClient
    {    
    #region Загрузка файла наследуемой информации
    /// <summary>
    /// Загрузка файла "Характеристики жилого фонда и начисления ЖКУ"
    /// </summary>
    /// <param name="finder">файл</param>
    /// <returns>результат</returns>
    public Returns FileLoader(FilesImported finder)
    {

        Returns ret = Utils.InitReturns();

        try
        {
            //директория файла
            string fDirectory = Constants.Directories.ImportDir.Replace("/", "\\");

            //имя файла
            string fileName = Path.Combine(fDirectory, finder.saved_name);

            //версия файла
            int nzp_version = -1;

            //int nzpExc = AddMyFile("Характеристики жилого фонда", finder);
            //int nzpExcLog = AddMyFile("Логи характеристик жилого фонда", finder); 

            #region Разархивация файла

            using (SevenZipExtractor extractor = new SevenZipExtractor(fileName))
            {
                //создание папки с тем же именем
                DirectoryInfo exDirectorey = Directory.CreateDirectory(Path.Combine(fDirectory, finder.saved_name.Substring(0, finder.saved_name.LastIndexOf('.'))));
                extractor.ExtractArchive(exDirectorey.FullName);
                FileInfo[] files = exDirectorey.GetFiles("*.txt");
                if (files.Length > 0)
                {
                    FileInfo textFile = files[0];
                    textFile.MoveTo(Path.Combine(fDirectory, exDirectorey.Name + ".txt"));
                    //удаление распакованной директории
                    Directory.Delete(exDirectorey.FullName, true);

                    //обновляем ссылку на новый распакованный файл
                    fileName = textFile.FullName;
                }
            }

            #endregion

            #region Считываем файл
            byte[] buffer = new byte[0];

            if (System.IO.File.Exists(fileName) == false)
            {
                ret.result = false;
                ret.text = "Файл отсутствует по указанному пути";
                ret.tag = -1;
                return ret;
            }

            try
            {
                System.IO.FileStream fstream = new System.IO.FileStream(fileName, System.IO.FileMode.Open, System.IO.FileAccess.Read);

                buffer = new byte[fstream.Length];
                fstream.Position = 0;
                fstream.Read(buffer, 0, buffer.Length);
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

            string tehPlanFileString = System.Text.Encoding.GetEncoding(1251).GetString(buffer);
            string[] stSplit = { System.Environment.NewLine };
            string[] fileStrings = tehPlanFileString.Split(stSplit, StringSplitOptions.None);
            #endregion

            #region Проверка версии
            IDbConnection con_db = GetConnection(Constants.cons_Kernel);
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

                IDataReader reader = null;

                string firstRow = Array.Find(fileStrings, x => x != "");
                string version = firstRow.Split(new char[] { '|' })[1];
                string sql = 
                    " select nzp_version from " + Points.Pref + "_kernel" + tableDelimiter + "  file_versions "+
                    " where version_name = \'" + version + "\'";
                ret = ExecRead(con_db, out reader, sql, true);
                if (!ret.result)
                {
                    MonitorLog.WriteLog("Ошибка получения версии файла " + fileName, MonitorLog.typelog.Error, true);
                    ret.result = false;
                    ret.text = " Ошибка получения версии файла. ";
                    ret.tag = -1;
                    return ret;
                }
                while (reader.Read())
                {
                    if (reader["nzp_version"] != DBNull.Value) nzp_version = Convert.ToInt32(reader["nzp_version"]);
                }

                if (nzp_version == -1)
                {
                    MonitorLog.WriteLog("Ошибка версии файла " + fileName, MonitorLog.typelog.Error, true);
                    ret.result = false;
                    ret.text = " Ошибка версии файла. Загруженный файл не прошел контроль версии.";
                    ret.tag = -1;
                    return ret;
                }

            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры FileLoader : " + ex.Message + "\n" + ex.StackTrace, MonitorLog.typelog.Error, true);
                ret.result = false;
                return ret;
            }
            #endregion Проверка версии

            #region Добавление файла в таблицу файла files_imported
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
                    MonitorLog.WriteLog("Ошибка определения локального пользователя", MonitorLog.typelog.Error, true);
                    ret.result = false;
                    ret.text = " Ошибка определения локального пользователя ";
                    ret.tag = -1;
                    return ret;
                }

                string sql = 
                    " INSERT INTO " + Points.Pref + "_data" + tableDelimiter + "files_imported "+
                    " ( nzp_version, loaded_name, saved_name, nzp_status, "+
                    "    created_by, created_on, percent, pref) "+
                    " VALUES (" + nzp_version + ",'" + finder.loaded_name + "',\'" + finder.saved_name + "\',1," + 
                        localUSer +","+sCurDateTime+", 0, '" + finder.bank + "')  ";
                ret = ExecSQL(con_db, sql, true);
                if (!ret.result)
                {
                    MonitorLog.WriteLog("Ошибка добавления в таблицу файла " + fileName, MonitorLog.typelog.Error, true);
                    ret.result = false;
                    ret.text = " Ошибка добавления файла в базу данных. ";
                    ret.tag = -1;
                    return ret;
                }

                finder.nzp_file = GetSerialValue(con_db);
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры FileLoader : " + ex.Message + "\n" + ex.StackTrace, MonitorLog.typelog.Error, true);
                ret.result = false;
                return ret;
            }
            #endregion Добавление файла в таблицу files_imported

            #region Переменные
            List<string> sqlStr = new List<string>();
            StringBuilder err = new StringBuilder();
            StringBuilder errKvar = new StringBuilder();
            bool load13section = false;
            string versFull = ""; //полное наименование версии

            #endregion Переменные

            #region Запросы
            //using (IDbConnection con_db = GetConnection(Constants.cons_Kernel))
            //{
            //con_db = GetConnection(Constants.cons_Kernel);
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

                //создаем таблички, если их нет
                CreateTable(con_db);

                //очищаем табличку запросов file_sql

#if PG
                
                string sql = " SET search_path TO '" + Points.Pref + "_data'";
#else                    
                string sql = "database " + Points.Pref + "_data";
#endif

                ret = ExecSQL(con_db, sql, true);
                sql = 
                    " delete from " + Points.Pref + "_data" + tableDelimiter + "file_sql "+
                    " where nzp_file =" + finder.nzp_file.ToString();
                ret = ExecSQL(con_db, sql, true);

                sql = 
                    sUpdStat + " " + Points.Pref + "_data" + tableDelimiter + "file_sql";
                ret = ExecSQL(con_db, sql, true);

                //заполняем file_section - те секции, которые мы разрешаем грузить
                SectionsToDB(con_db, finder);
                
                string Pvers = ""; 

                #region Собрать запросы
                //цикл по строкам
                foreach (string str in fileStrings)
                {
                    //защита от пустых строк(пустые строки для сохранения нумерации)
                    if (str.Trim() == "")
                    {
                        continue;
                    }

                    ret = Utils.InitReturns();                    
                    sql = "";
                      

                    //массив значений строки
                    string[] vals = str.Split(new char[] { '|' }, StringSplitOptions.None);
                    Array.ForEach(vals, x => x = x.Trim());
                    //Array.ForEach(vals, x => x = x.Replace("'", "\""));
                    //Array.ForEach(vals, x => x = x.Replace("\"", "\"\""));

                    //пропуск пустой строчки
                    if (vals.Length == 0)
                    {
                        continue;
                    }

                    //номер строки в файле
                    string rowNumber = Environment.NewLine + " (строка " + (Array.IndexOf(fileStrings, str) + 1).ToString() + ") ";
                    ret.text += rowNumber;
                    Int32 rowNumber1 = Array.IndexOf(fileStrings, str) + 1;

                    if (fileStrings.Length / 25 != 0 && rowNumber1 % (fileStrings.Length / 25) == 0)
                    {
                        string sqlPercent =
                            "update " + Points.Pref + "_data" + tableDelimiter + "files_imported set percent = " + 
                            ((decimal)rowNumber1 / (decimal)fileStrings.Length) / 3 +
                            " where nzp_file = " + finder.nzp_file;
                        ret = ExecSQL(con_db, sqlPercent, true);
                    }

                    DBValuesFromFile ValuesFromFile = new DBValuesFromFile
                    {
                        finder = finder,
                        rowNumber = rowNumber,
                        Pvers = Pvers,
                        versFull = versFull,
                        err = err,
                        vals = vals,
                        sql = "",
                        load13section = load13section,
                        ret = ret
                    };

                    #region switch 28 секций
                    switch (vals[0])
                    {
                        #region 1 Заголовок
                        case "1":
                            {                  
                                AddFileHead_Section1(ValuesFromFile);
                                break;
                            }
                        #endregion

                        #region 2 УК
                        case "2":
                            {    
                                AddFileArea_Section2(ValuesFromFile);
                                break;
                            }
                        #endregion

                        #region 3 Дома
                        case "3":
                            {
                                AddFileDom_Section3(ValuesFromFile);                                    
                                break;
                            }
                        #endregion

                        #region 4 Информация о лицевых счетах
                        case "4":
                            {
                                AddFileKvar_Section4(ValuesFromFile);
                                break;
                            }
                        #endregion

                        #region 5 Поставщики услуг
                        case "5":
                            {
                                AddFileSupp_Section5(ValuesFromFile);
                                break;
                            }
                        #endregion

                        #region 6 Информация об оказываемых услугах
                        case "6":
                            {
                                AddFileServ_Section6(ValuesFromFile);
                                break;
                            }
                        #endregion

                        #region 7 Информация о параметрах лицевых счетов в месяце перерасчета
                        case "7":
                            {
                                AddFileKvarp_Section7(ValuesFromFile);                                
                                break;
                            }
                        #endregion

                        #region  8 Информация о перерасчетах начислений по услугам
                        case "8":
                            {
                                AddFileServp_Section8(ValuesFromFile);                               
                                break;
                            }
                        #endregion

                        #region 9 Информация об общедомовых приборах учета
                        case "9":
                            {
                                AddFileOdpu_Section9(ValuesFromFile); 
                                break;
                            }
                        #endregion

                        #region 10 Показания общедомовых приборов учета
                        case "10":
                            {
                                AddFileOdpu_p_Section10(ValuesFromFile);
                                break;
                            }
                        #endregion

                        #region 11 Счетчики ИПУ
                        case "11":
                            {
                                AddFileIpu_Section11(ValuesFromFile);
                                break;
                            }
                        #endregion

                        #region 12 Показания ИПУ
                        case "12":
                            {
                                AddFileIpu_p_Section12(ValuesFromFile);                                
                                break;
                            }
                        #endregion

                        #region 13 Перечень выгруженных услуг
                        case "13":
                            {
                                AddFileServices_Section13(ValuesFromFile);                                
                                break;
                            }
                        #endregion

                        #region 14 Выгруженные МО
                        case "14":
                            {
                                AddFileMo_Section14(ValuesFromFile);
                                break;
                            }
                        #endregion

                        #region 15 Жильцы
                        case "15":
                            {
                                AddFileGilec_Section15(ValuesFromFile);
                                break;
                            }
                        #endregion

                        #region 16 Выгруженные параметры
                        case "16":
                            {
                                AddFileTypeparams_Section16(ValuesFromFile);
                                break;
                            }
                        #endregion

                        #region 17 Выгруженные типы жилья по газоснабжению
                        case "17":
                            {
                                AddFileGaz_Section17(ValuesFromFile);
                                break;
                            }
                        #endregion

                        #region 18 Выгруженные типы жилья по водоснабжению
                        case "18":
                            {
                                AddFileVoda_Section18(ValuesFromFile);
                                break;
                            }
                        #endregion

                        #region 19 Выгруженные категории благоустройства дома
                        case "19":
                            {
                                AddFileBlag_Section19(ValuesFromFile);
                                break;
                            }
                        #endregion

                        #region 20 Доп. хакактеристики дома
                        case "20":
                            {
                                AddFileParamsdom_Section20(ValuesFromFile);
                                break;
                            }
                        #endregion

                        #region 21 Доп. характеристики лицевого счета
                        case "21":
                            {
                                AddFileParamsls_Section21(ValuesFromFile);
                                break;
                            }
                        #endregion

                        #region 22 Перечень оплат, проведеных по лицевому счету
                        case "22":
                            {
                                AddFileOplats_Section22(ValuesFromFile);
                                break;
                            }
                        #endregion

                        #region 23 Перечень недопоставок
                        case "23":
                            {
                                AddFileNedopost_Section23(ValuesFromFile);
                                break;
                            }
                        #endregion

                        #region 24 Перечень типов недопоставок
                        case "24":
                            {
                                AddFileTypenedopost_Section24(ValuesFromFile);
                                break;
                            }
                        #endregion

                        #region 25 Перечень услуг лицевого счета
                        case "25":
                            {
                                AddFileServls_Section25(ValuesFromFile);
                                break;
                            }
                        #endregion

                        #region 26 Пачки реестров
                        case "26":
                            {
                                AddFilePack_Section26(ValuesFromFile);
                                break;
                            }
                        #endregion

                        #region 27 Юридические лица
                        case "27":
                            {
                                AddFileUrlic_Section27(ValuesFromFile);
                                break;
                            }
                        #endregion

                        #region 28 Реестр временно убывших
                        case "28":
                            {
                                AddFileVrub_Section28(ValuesFromFile);
                                break;
                            }
                        #endregion

                    }
                    #endregion switch 28 секций

                    finder = ValuesFromFile.finder;
                    rowNumber = ValuesFromFile.rowNumber;
                    Pvers = ValuesFromFile.Pvers;
                    versFull = ValuesFromFile.versFull;
                    err = ValuesFromFile.err;
                    vals = ValuesFromFile.vals;
                    sql = ValuesFromFile.sql;
                    
                    if (sql.Trim() != "")
                    {
                        sql = sql.Replace("\"", " ");
                        sql = sql.Replace("\'", "$");
                        //sqlStr.Add(sql);
                        string sql_in_table =
                            " insert into " + Points.Pref + "_data" + tableDelimiter + "file_sql " +
                            " values(" + rowNumber1 + "," + finder.nzp_file.ToString() + ", '" + sql + "') ";
                        ret = ExecSQL(con_db, sql_in_table, true);
                        if (!ret.result)
                        {
                            MonitorLog.WriteLog("Ошибка записи в таблицу file_sql " + fileName /*добавить номер секции*/, MonitorLog.typelog.Error, true);
                            ret.result = false;
                            ret.text = " Ошибка загрузки файла в базу данных. ";
                            ret.tag = -1;
                            return ret;
                        }
                    }

                }
                #endregion

            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры FileLoader : " + ex.Message + "\n" + ex.StackTrace, MonitorLog.typelog.Error, true);
                ret.result = false;
                return ret;
            }
            //}
            #endregion

            int nzpExc = AddMyFile("Характеристики жилого фонда", finder);
            int nzpExcLog = AddMyFile("Логи характеристик жилого фонда", finder); 

            #region Лог ошибок
            if (err.Length != 0)
            {
                StreamWriter sw = File.CreateText(fileName + ".log");
                sw.Write(err.ToString());
                sw.Flush();
                sw.Close();

                string[] strFilesNames = new string[2];

                #region формирование отчета 3.1-3.3
                //using (IDbConnection con_db = GetConnection(Constants.cons_Kernel))
                //{
                //con_db = GetConnection(Constants.cons_Kernel);
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

                    //отчет по лицевым счетам 3.1-3.3
                    FileResultOfLoad(con_db, finder, errKvar);

                }
                catch (Exception ex)
                {
                    MonitorLog.WriteLog("Ошибка выполнения процедуры FileLoader : " + ex.Message + "\n" + ex.StackTrace, MonitorLog.typelog.Error, true);
                    ret.result = false;
                    return ret;
                }
                //}
                #endregion

                StreamWriter swKvar = File.CreateText(fileName + "Kvar.log");
                swKvar.Write(errKvar.ToString());
                swKvar.Flush();
                swKvar.Close();

                strFilesNames[0] = String.Format("{0}{1}.log", fDirectory, fileName.Replace(fDirectory, ""));
                strFilesNames[1] = String.Format("{0}{1}Kvar.log", fDirectory, fileName.Replace(fDirectory, ""));

                #region Архивация лога
                SevenZipCompressor szcComperssor = new SevenZipCompressor();
                szcComperssor.ArchiveFormat = OutArchiveFormat.Zip;
                szcComperssor.CompressFiles(String.Format("{0}log_{1}.zip", fDirectory, fileName.Replace(fDirectory, "")), strFilesNames);
                File.Delete(String.Format("{0}.log", fileName));

                string fn1 = "";
                if (InputOutput.useFtp) fn1 = InputOutput.SaveInputFile(String.Format("{0}log_{1}.zip", fDirectory, fileName.Replace(fDirectory, "")));

                fileName = String.Format("{0}log_{1}.zip", fDirectory, fileName.Replace(fDirectory, ""));

                #endregion

                #region Обновление статуса
                //using (IDbConnection con_db = GetConnection(Constants.cons_Kernel))
                //{
                //con_db = GetConnection(Constants.cons_Kernel);
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
                    string sql = 
                        " UPDATE " + Points.Pref + "_data" + tableDelimiter + "files_imported set (nzp_status, percent) =  "+
                        " (" + (int)STCLINE.KP50.Interfaces.FilesImported.Statuses.LoadedWithErrors + ", 1)" +
                        " where nzp_file = " + finder.nzp_file;

                    ret = ExecSQL(con_db, sql, true);
                    if (!ret.result)
                    {
                        MonitorLog.WriteLog("Ошибка обновления статуса файла " + fileName, MonitorLog.typelog.Error, true);
                        ret.result = false;
                        ret.text = " Ошибка обновления статуса файла. ";
                        ret.tag = -1;
                        return ret;
                    }

                    

                    string commStr = 
                        " update " + Points.Pref + "_data" + tableDelimiter + "files_imported " +
                        " set nzp_exc_log = " + nzpExcLog + " where nzp_file = " + finder.nzp_file;
                    ExecSQL(con_db, commStr, true);
                    SetMyFileState(new ExcelUtility()
                    {
                        nzp_exc = nzpExcLog,
                        status = ExcelUtility.Statuses.Success,
                        exc_path = InputOutput.useFtp ? fn1 : Path.Combine(fDirectory, fileName)
                    });
                }
                catch (Exception ex)
                {
                    MonitorLog.WriteLog("Ошибка выполнения процедуры FileLoader : " + ex.Message + "\n" + ex.StackTrace, MonitorLog.typelog.Error, true);
                    ret.result = false;
                    return ret;
                }
                //}
                #endregion

                ret.tag = -1;
                ret.result = false;
                ret.text = "В загруженном файле обнаружились ошибки. Подробности в логе ошибок. ";

                return ret;
            }
            #endregion

            //для лога ошибок связности БД
            StringBuilder errRelation = new StringBuilder();

            #region Запись в БД
            //using (IDbConnection con_db = GetConnection(Constants.cons_Kernel))
            //{
            //con_db = GetConnection(Constants.cons_Kernel);
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

// TODO надо закончить загрузку здесь


                #region Выполняем запросы из file_sql
                //for (int i = 0; i < dt.resultData.Rows.Count; i++)
                Int32 step1 = 0; Int32 step2 = 0;
                string sql1 = 
                    " select min(id) as step1,  max(id)  as step2 "+
                    " from " + Points.Pref + "_data" + tableDelimiter + "file_sql "+
                    " where nzp_file = " + finder.nzp_file.ToString();
                var dt2 = ClassDBUtils.OpenSQL(sql1, con_db);
                foreach (DataRow rr2 in dt2.resultData.Rows)
                {
                    step1 = Convert.ToInt32(rr2["step1"]);
                    step2 = Convert.ToInt32(rr2["step2"]);
                }

                for (int i = step1; i < step2; i = i + 2000)
                {
                    // Выполнить с шагом 2000 строк 
                    sql1 = 
                        "select id, sql_zapr from " + Points.Pref + "_data" + tableDelimiter + "file_sql "+
                        " where id>=" +Convert.ToString(i) + 
                        "  and id <" + Convert.ToString(i + 2000) + 
                        "  and nzp_file=" + finder.nzp_file.ToString() + 
                        " order by 1";
                    var dt = ClassDBUtils.OpenSQL(sql1, con_db);
                    foreach (DataRow rr in dt.resultData.Rows)
                    {
                        string sql2 = Convert.ToString(rr["sql_zapr"]);
                        sql2 = sql2.Replace("$", "\'");
                        ret = ExecSQL(con_db, sql2.Trim(), true);
                    }
                    dt = null;
                    sql1 = sUpdStat + " " + Points.Pref + "_data" + tableDelimiter + "file_sql";
                    ExecSQL(con_db, sql1, true);
                    sql1 = "";

                    decimal percentLoad = (decimal)(i - step1) / ((step2 - step1));
                    decimal dola = 0;
                    if (Math.Round(percentLoad, 1) >= dola)
                    {
                        string sqlPercent1 = 
                            " update " + Points.Pref + "_data" + tableDelimiter + "files_imported set percent = " + 
                            (Math.Round(percentLoad, 1) / 3 + (decimal)0.34) +
                            " where nzp_file = " + finder.nzp_file;
                        ret = ExecSQL(con_db, sqlPercent1, true);
                        dola = dola + (decimal)0.1;
                    }
                }

                sql1 = 
                    " delete from   " + Points.Pref + "_data" + tableDelimiter + "file_sql "+
                    " where nzp_file =" + finder.nzp_file;
                ret = ExecSQL(con_db, sql1, true);

                string sqlPercent = 
                    " update " + Points.Pref + "_data" + tableDelimiter + "files_imported set percent = 0.76" +
                    " where nzp_file = " + finder.nzp_file;
                ret = ExecSQL(con_db, sqlPercent, true);

                #endregion

                //заполняем в file_dom поля ndom, nkor, rajon, если они null 
                SetValueForZeroFields(con_db, errRelation);

                //сопоставление адресного пространства по коду КЛАДР, если версия файла 1.2.2
                if (versFull.Trim() == "'1.2.2'")
                {
                    SetLinksByKladr(con_db, finder, errRelation);
                }

                //если не загружали 13 секцию, то берем из таблицы kernel:services
                if (!load13section)
                {
                    LoadOur13Section(con_db, finder);
                }
                // Заполнить единицы измерения из стандартного приложения если нет раскладки из файла 
                FillMeasure(con_db, finder);

                sqlPercent = 
                    "update " + Points.Pref + "_data" + tableDelimiter + "files_imported set percent = 0.82" +
                    " where nzp_file = " + finder.nzp_file;
                ret = ExecSQL(con_db, sqlPercent, true);


                //проверка уникальности данных
                CheckUnique(con_db, finder, errRelation);


                sqlPercent = 
                    "update " + Points.Pref + "_data" + tableDelimiter + "files_imported set percent = 0.85" +
                    " where nzp_file = " + finder.nzp_file;
                ret = ExecSQL(con_db, sqlPercent, true);

                //проверка связности БД
                CheckRelation(con_db, finder, errRelation);

                sqlPercent = 
                    "update " + Points.Pref + "_data" + tableDelimiter + "files_imported set percent = 0.91" +
                    " where nzp_file = " + finder.nzp_file;
                ret = ExecSQL(con_db, sqlPercent, true);

                //качество данных из 6 секции
                Check6Section(con_db, finder, errRelation);

                sqlPercent = 
                    "update " + Points.Pref + "_data" + tableDelimiter + "files_imported set percent = 0.98" +
                    " where nzp_file = " + finder.nzp_file;
                ret = ExecSQL(con_db, sqlPercent, true);
                
                string commStr = 
                    "update " + Points.Pref + "_data" + tableDelimiter + "files_imported set nzp_exc = " + nzpExc + 
                    " where nzp_file = " + finder.nzp_file;
                ExecSQL(con_db, commStr, true);

                string fn4 = "";
                if (InputOutput.useFtp)
                { fn4 = InputOutput.SaveInputFile(String.Format("{0}{1}", fDirectory, finder.saved_name)); }

                SetMyFileState(new ExcelUtility() 
                { 
                    nzp_exc = nzpExc, 
                    status = ExcelUtility.Statuses.Success, 
                    exc_path = InputOutput.useFtp ? fn4 : Path.Combine(fDirectory, finder.saved_name) 
                });

                if (errRelation.Length == 0)
                {
                    sqlPercent = "update " + Points.Pref + "_data" + tableDelimiter + "files_imported set nzp_status = "
                        + (int)STCLINE.KP50.Interfaces.FilesImported.Statuses.Loaded +
                            " where nzp_file = " + finder.nzp_file;
                    ret = ExecSQL(con_db, sqlPercent, true);

                    //отчет по лицевым счетам 3.1-3.3
                    FileResultOfLoad(con_db, finder, errKvar);

                    StreamWriter swKvar = File.CreateText(fileName + "Kvar.log");
                    swKvar.Write(errKvar.ToString());
                    swKvar.Flush();
                    swKvar.Close();

                    #region Архивация лога
                    SevenZipCompressor szcComperssor = new SevenZipCompressor();
                    szcComperssor.ArchiveFormat = OutArchiveFormat.Zip;
                    szcComperssor.CompressFiles(String.Format("{0}log_{1}.zip", fDirectory, fileName.Replace(fDirectory, "")), String.Format("{0}{1}Kvar.log", fDirectory, fileName.Replace(fDirectory, "")));
                    File.Delete(String.Format("{0}.log", fileName));


                    string fn3 = "";
                    if (InputOutput.useFtp)
                        fn3 = InputOutput.SaveInputFile(String.Format("{0}log_{1}.zip", fDirectory, fileName.Replace(fDirectory, "")));
                    #endregion

                    fileName = String.Format("{0}log_{1}.zip", fDirectory, fileName.Replace(fDirectory, ""));
                    SetMyFileState(new ExcelUtility() { nzp_exc = nzpExcLog, status = ExcelUtility.Statuses.Success, exc_path = InputOutput.useFtp ? fn3 : Path.Combine(fDirectory, fileName) });
                    

                    sqlPercent = "update " + Points.Pref + "_data" + tableDelimiter + "files_imported set (percent,  nzp_exc_log) = (1, "
                        + nzpExcLog + ")" +
                            " where nzp_file = " + finder.nzp_file;
                    ret = ExecSQL(con_db, sqlPercent, true);
                }

            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры FileLoader : " + ex.Message + "\n" + ex.StackTrace, MonitorLog.typelog.Error, true);
                ret.result = false;
                return ret;
            }
            //finally
            //{
            //    con_db.Close();
            //}
            //}
            #endregion

            #region Лог ошибок связности БД
            if (errRelation.Length != 0)
            {
                string[] strFilesNames = new string[2];

                #region формирование отчета 3.1-3.3
                //using (IDbConnection con_db = GetConnection(Constants.cons_Kernel))
                //{
                //con_db = GetConnection(Constants.cons_Kernel);
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

                    //отчет по лицевым счетам 3.1-3.3
                    FileResultOfLoad(con_db, finder, errKvar);

                }
                catch (Exception ex)
                {
                    MonitorLog.WriteLog("Ошибка выполнения процедуры FileLoader : " + ex.Message, MonitorLog.typelog.Error, true);
                    ret.result = false;
                    return ret;
                }
                //}
                #endregion

                StreamWriter sw = File.CreateText(fileName + ".log");
                sw.Write(errRelation.ToString());
                sw.Flush();
                sw.Close();

                StreamWriter swKvar = File.CreateText(fileName + "Kvar.log");
                swKvar.Write(errKvar.ToString());
                swKvar.Flush();
                swKvar.Close();

                strFilesNames[0] = String.Format("{0}{1}.log", fDirectory, fileName.Replace(fDirectory, ""));
                strFilesNames[1] = String.Format("{0}{1}Kvar.log", fDirectory, fileName.Replace(fDirectory, ""));

                #region Архивация лога
                SevenZipCompressor szcComperssor = new SevenZipCompressor();
                szcComperssor.ArchiveFormat = OutArchiveFormat.Zip;
                szcComperssor.CompressFiles(String.Format("{0}log_{1}.zip", fDirectory, fileName.Replace(fDirectory, "")), strFilesNames);
                File.Delete(String.Format("{0}.log", fileName));
                //


                string fn2 = "";
                if (InputOutput.useFtp) fn2 = InputOutput.SaveInputFile(String.Format("{0}log_{1}.zip", fDirectory, fileName.Replace(fDirectory, "")));
                #endregion

                #region Обновление статуса
                //using (IDbConnection con_db = GetConnection(Constants.cons_Kernel))
                //{
                // con_db = GetConnection(Constants.cons_Kernel);
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
                    string commStr = "update " + Points.Pref + "_data" + tableDelimiter + "files_imported set (nzp_exc_log, percent) = (" + nzpExcLog + ",1)  where nzp_file = " + finder.nzp_file;
                    ExecSQL(con_db, commStr, true);
                    fileName = String.Format("{0}log_{1}.zip", fDirectory, fileName.Replace(fDirectory, ""));
                    SetMyFileState(new ExcelUtility() { nzp_exc = nzpExcLog, status = ExcelUtility.Statuses.Success, exc_path = InputOutput.useFtp ? fn2 : Path.Combine(fDirectory, fileName) });


                }
                catch (Exception ex)
                {
                    MonitorLog.WriteLog("Ошибка выполнения процедуры FileLoader : " + ex.Message, MonitorLog.typelog.Error, true);
                    ret.result = false;
                    return ret;
                }
                //}
                #endregion

                //ret.tag = -1;
                //ret.result = false;
                //ret.text = "В загруженном файле обнаружились ошибки. Подробности в логе ошибок. ";

                //return ret;
            }
            #endregion


            ret.result = true;
            ret.text = "Файл успешно загружен.";
            ret.tag = -1;

            con_db.Close();
        }
        catch (Exception ex)
        {
            MonitorLog.WriteLog("Ошибка FileLoader " + ex.Message + "\n" + ex.StackTrace, MonitorLog.typelog.Error, true);
            ret.result = false;
            ret.text = " Ошибка загрузки файла ";
            ret.tag = -1;
            return ret;
        }
        return ret;

    }
    #endregion Загрузка файла наследуемой информации


    #region вспомогательные функции

    /// <summary>
    /// Загрузка 1 секции "Заголовок файла"
    /// </summary>
    /// <param name="ValuesFromFile"></param>
    public void AddFileHead_Section1(DBValuesFromFile ValuesFromFile)
    {
        
        Returns ret = new Returns();

        if (ValuesFromFile.vals.Length < 13)
        {
            ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + " Неправильный формат файла загрузки: Заголовок файла, количество полей =" + ValuesFromFile.vals.Length + " вместо 13 ");
            return;
        }

        #region Загрузка 1 секции 
        
        ValuesFromFile.sql =
            " INSERT INTO " + Points.Pref + "_data" + tableDelimiter + "file_head" +
            " (nzp_file, org_name, branch_name, inn, kpp, file_no, file_date, sender_phone, sender_fio, calc_date, row_number) " +
            " VALUES (" + ValuesFromFile.finder.nzp_file + ", ";
        int i = 1;

        //2. Версия формата
        string vers = this.CheckText(ValuesFromFile.vals[i], true, 10, ref ret);
        if (!ret.result)
        {
            ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "Версия формата");
        }
        ValuesFromFile.Pvers = vers;
        ValuesFromFile.versFull = vers;
        ValuesFromFile.Pvers = "1.2";
        i++;

        //3. Тип файла
        string type = this.CheckText(ValuesFromFile.vals[i], true, 30, ref ret);
        if (!ret.result)
        {
            ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "Тип файла");
        }
        i++;

        //4. Наименование организации-отправителя 
        string org_name = this.CheckText(ValuesFromFile.vals[i], true, 40, ref ret);
        if (!ret.result)
        {
            ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "Наименование организации-отправителя ");
        }
        ValuesFromFile.sql += org_name + ", ";
        i++;

        //5. Подразделение организации-отправителя
        string branch_name = this.CheckText(ValuesFromFile.vals[i], false, 40, ref ret);
        if (!ret.result)
        {
            ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "Подразделение организации-отправителя ");
        }
        if (branch_name == "null") 
        {
            branch_name = " '-' "; 
        }

        ValuesFromFile.sql += branch_name + ", ";
        i++;

        //6. ИНН
        string inn = this.CheckText(ValuesFromFile.vals[i], false, 12, ref ret);
        if (!ret.result)
        {
            ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "ИНН");
        }
        if (inn == "null")
        {
            //TODO временное решение проблемы
            inn = " '0' ";
        }
        ValuesFromFile.sql += inn + ", ";
        i++;

        //7. КПП
        string kpp = this.CheckText(ValuesFromFile.vals[i], false, 9, ref ret);
        if (!ret.result)
        {
            ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "КПП");
        }
        if (kpp == "null")
        {
            //TODO временное решение проблемы
            kpp = " '0' ";
        }
        ValuesFromFile.sql += kpp + ", ";
        i++;

        //8. № файла
        string num = this.CheckInt(ValuesFromFile.vals[i], true, 1, null, ref ret);
        if (!ret.result)
        {
            ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "№ файла");
        }
        ValuesFromFile.sql += num + ", ";
        i++;

        //9. Дата файла
        string datf = this.CheckDateTime(ValuesFromFile.vals[i], true, ref ret);
        if (!ret.result)
        {
            ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "Дата файла");
        }
        ValuesFromFile.sql += datf + ", ";
        i++;

        //10. Телефон отправителя
        string tel = this.CheckText(ValuesFromFile.vals[i], true, 20, ref ret);
        if (!ret.result)
        {
            ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "Телефон отправителя");
        }
        ValuesFromFile.sql += tel + ", ";
        i++;

        //11. ФИО отправителя
        string fio = this.CheckText(ValuesFromFile.vals[i], true, 80, ref ret);
        if (!ret.result)
        {
            ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "ФИО отправителя");
        }
        ValuesFromFile.sql += fio + ", ";
        i++;

        //12. Месяц и год начислений
        string month = this.CheckDateTime(ValuesFromFile.vals[i], true, ref ret);
        if (!ret.result)
        {
            ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "Месяц и год начислений");
        }
        ValuesFromFile.sql += month + ", ";
        i++;

        //13. Количество записей в файле
        string rows = this.CheckInt(ValuesFromFile.vals[i], true, 0, null, ref ret);
        if (!ret.result)
        {
            ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "Количество записей в файле");
        }
        ValuesFromFile.sql += rows + "); ";
        i++;

        #endregion Загрузка секции
    }


    /// <summary>
    ///  Загрузка 2 секции "Управляющие компании"
    /// </summary>
    /// <param name="ValuesFromFile"></param>
    public void AddFileArea_Section2(DBValuesFromFile ValuesFromFile)
    {
        Returns ret = new Returns();
        if (ValuesFromFile.vals.Length < 11)
        {
            ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + " Неправильный формат файла загрузки: УК, количество полей =" + ValuesFromFile.vals.Length + " вместо 11 ");
            return;
        }

        #region Загрузка 2 секции
        ValuesFromFile.sql =
                    " INSERT INTO " + Points.Pref + "_data" + tableDelimiter + "file_area " +
                    "( id, nzp_file, area, jur_address, fact_address, inn, kpp, rs, bank, bik, ks, nzp_area) " +
                    " VALUES (";
            int i = 1;

            //2. Уникальный код УК
            string id = this.CheckInt(ValuesFromFile.vals[i], true, null, null, ref ret);
            if (!ret.result)
            {
                ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "Уникальный код УК");
            }
            ValuesFromFile.sql += id + ", ";
            i++;

            //nzp_file
            ValuesFromFile.sql += ValuesFromFile.finder.nzp_file + ", ";

            //3. Наименование УК
            string area = this.CheckText(ValuesFromFile.vals[i], true, 60, ref ret);
            if (!ret.result)
            {
                ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "Наименование УК");
            }
            ValuesFromFile.sql += area + ", ";
            i++;

            //4. Юридический адрес
            string jur_address = this.CheckText(ValuesFromFile.vals[i], false, 100, ref ret);
            if (!ret.result)
            {
                ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "Юридический адрес");
            }
            ValuesFromFile.sql += jur_address + ", ";
            i++;

            //5. Фактический адрес
            string fact_address = this.CheckText(ValuesFromFile.vals[i], false, 100, ref ret);
            if (!ret.result)
            {
                ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "Фактический адрес");
            }
            ValuesFromFile.sql += fact_address + ", ";
            i++;

            //6. ИНН
            string inn = this.CheckText(ValuesFromFile.vals[i], true, 20, ref ret);
            if (!ret.result)
            {
                ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "ИНН");
            }
            ValuesFromFile.sql += inn + ", ";
            i++;

            //7. КПП
            string kpp = this.CheckText(ValuesFromFile.vals[i], true, 20, ref ret);
            if (!ret.result)
            {
                ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "КПП");
            }
            ValuesFromFile.sql += kpp + ", ";
            i++;

            //8. Расчетный счет
            string rch = this.CheckText(ValuesFromFile.vals[i].Trim(), false, 20, ref ret);
            if (!ret.result)
            {
                ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + " Расчетный счет");
            }
            ValuesFromFile.sql += rch + ", ";
            i++;

            //9. Банк
            string bank = this.CheckText(ValuesFromFile.vals[i], false, 100, ref ret);
            if (!ret.result)
            {
                ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + " Банк");
            }
            ValuesFromFile.sql += bank + ", ";
            i++;

            //10. БИК банка
            string bik = this.CheckText(ValuesFromFile.vals[i], false, 20, ref ret);
            if (!ret.result)
            {
                ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + " БИК банка");
            }
            ValuesFromFile.sql += bik + ", ";
            i++;

            //11. Корреспондентский счет
            string kch = this.CheckText(ValuesFromFile.vals[i], false, 20, ref ret);
            if (!ret.result)
            {
                ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + " Корреспондентский счет");
            }
            ValuesFromFile.sql += kch + ", ";
            i++;

            //добавляем nzp_area
            ValuesFromFile.sql += "null);";

        #endregion Загрузка 2 секции
    }


    /// <summary>
    ///  Загрузка 3 секции "Информация о домах"
    /// </summary>
    /// <param name="ValuesFromFile"></param>
    public void AddFileDom_Section3(DBValuesFromFile ValuesFromFile)
    {
        Returns ret = new Returns();
        if (ValuesFromFile.vals.Length < 19)
        {
            ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + " Неправильный формат файла загрузки: Дома");
            return;
        }

        #region Загрузка 3 секции
            ValuesFromFile.sql =
                " INSERT INTO " + Points.Pref + "_data" + tableDelimiter + "file_dom " +
                "( id, local_id, nzp_file, ukds, town, rajon, ulica, ndom, nkor, area_id, cat_blago, etazh, build_year, total_square, mop_square, useful_square,  " +
                " mo_id, params, ls_row_number, odpu_row_number, nzp_ul, nzp_dom, comment ";
            if (ValuesFromFile.versFull.Trim() == "'1.2.2'")
                ValuesFromFile.sql += ", kod_kladr) ";
            else ValuesFromFile.sql += ")";
            ValuesFromFile.sql += " VALUES ( ";
            int i = 1;

            //2. УКДС
            string ukds = this.CheckInt(ValuesFromFile.vals[i], false, null, null, ref ret);
            if (!ret.result)
            {
                ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "УКДС");
            }
            i++;

            //3. Уникальный код дома в системе отправителя

            string local_id = this.CheckText(ValuesFromFile.vals[i], true, 20, ref ret);
            if (!ret.result)
            {
                ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "Уникальный код дома в системе отправителя");
            }
#warning Рената
            string id = this.CheckInt(ValuesFromFile.vals[i].ToUpper().Replace("/", "00").Replace("А", "01").Replace("Б", "02").Replace("В", "03").Replace("Г", "04").Replace("D", "001").Replace("-", "002").Replace("О", "16").Replace("К", "12"), true, null, null, ref ret);
            if (!ret.result)
            {
                ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + " локальный код дома в системе отправителя");
            }

            i++;

            ValuesFromFile.sql += id + ", " + local_id + ", " + ValuesFromFile.finder.nzp_file + ", " + ukds + ", ";


            //4. Город/район
            string town = this.CheckText(ValuesFromFile.vals[i], false, 100, ref ret);
            if (!ret.result)
            {
                ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "Город/район");
            }
            ValuesFromFile.sql += town + ", ";
            i++;


            //5. Село/деревня
            string rajon = this.CheckText(ValuesFromFile.vals[i], false, 50, ref ret);
            if (!ret.result)
            {
                ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "Село/деревня");
            }
            ValuesFromFile.sql += rajon + ", ";
            i++;


            //6. Наименование улицы
            string ul = this.CheckText(ValuesFromFile.vals[i], false, 40, ref ret);
            if (!ret.result)
            {
                ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "Наименование улицы");
            }
            ValuesFromFile.sql += ul + ", ";
            i++;


            //7. Дом
            string dom = this.CheckText(ValuesFromFile.vals[i], false, 10, ref ret);
            if (!ret.result)
            {
                ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "Дом");
            }
            ValuesFromFile.sql += dom + ", ";
            i++;


            //8. Корпус
            string kor = this.CheckText(ValuesFromFile.vals[i], false, 10, ref ret);
            if (!ret.result)
            {
                ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "Корпус");
            }
            ValuesFromFile.sql += kor + ", ";
            i++;


            //9. Код УК         
            string uk = this.CheckInt(ValuesFromFile.vals[i], true, 1, null, ref ret);
            if (!ret.result)
            {
                ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + " Код УК");
            }
            ValuesFromFile.sql += uk + ", ";
            i++;


            //10. Категория благоустроенности (значение из справочника)
            string cat = this.CheckText(ValuesFromFile.vals[i], false, 30, ref ret);
            if (!ret.result)
            {
                ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "Категория благоустроенности (значение из справочника)");
            }
            ValuesFromFile.sql += cat + ", ";
            i++;


            //11. Этажность
            string etag = this.CheckInt(ValuesFromFile.vals[i], true, 1, null, ref ret);
            if (!ret.result)
            {
                etag = "-1";
                ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + " Этажность");
            }
            ValuesFromFile.sql += etag + ", ";
            i++;


            //12. Год постройки
            //string y = this.CheckDateTime(vals[i], true, ref ret);
            string pval;
            if (ValuesFromFile.vals[i].Length == 4) { pval = "01.01." + ValuesFromFile.vals[i]; } else { pval = ValuesFromFile.vals[i]; }
            string y = this.CheckDateTime(pval, false, ref ret);
            if (!ret.result)
            {
                y = "'01.01.1900'";
                ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "Год постройки");
            }
            ValuesFromFile.sql += y + ", ";
            i++;


            //13. Общая площадь - необязат
            string tot_sq = this.CheckDecimal(ValuesFromFile.vals[i], false, false, 0, null, ref ret);
            if (ret.result)
            {
                if (tot_sq == "null") { tot_sq = "0"; }  // почему то не у всех есть общая площадь
                ValuesFromFile.sql += tot_sq + ", ";
            }
            else
            {

                ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "Общая площадь");
            }
            i++;

            //14. Площадь мест общего пользования
            string mop_sq = this.CheckDecimal(ValuesFromFile.vals[i], false, false, 0, null, ref ret);
            if (ret.result)
            {
                ValuesFromFile.sql += mop_sq + ", ";
            }
            else
            {
                ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "Площадь мест общего пользования");
            }
            i++;


            //15. Полезная (отапливаемая площадь)
            string use_sq = this.CheckDecimal(ValuesFromFile.vals[i], false, false, 0, null, ref ret);
            if (ret.result)
            {
                ValuesFromFile.sql += use_sq + ", ";
            }
            else
            {
                ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "Полезная (отапливаемая площадь)");
            }
            i++;

            //16. Код Муниципального образования (значение из справочника)
            string mo_id = this.CheckInt(ValuesFromFile.vals[i], false, 0, null, ref ret);
            if (ret.result)
            {
                ValuesFromFile.sql += mo_id + ", ";
            }
            else
            {
                ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "Код Муниципального образования (значение из справочника)");
            }
            i++;

            //17. Дополнительные характеристики дома (задается в соответствии с правилами заполнения значений параметров)
            string p = this.CheckText(ValuesFromFile.vals[i], false, 250, ref ret);
            if (ret.result)
            {
                ValuesFromFile.sql += p + ", ";
            }
            else
            {
                ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "Дополнительные характеристики дома (задается в соответствии с правилами заполнения значений параметров)");
            }
            i++;


            //18. Количество строк - лицевой счет
            string ls_row_number = this.CheckInt(ValuesFromFile.vals[i], true, 0, null, ref ret);
            if (!ret.result)
            {
                if (true)
                {
                    ls_row_number = "0";
                }
                //else
                //{
                //    err.Append(rowNumber + ret.text + "Количество строк - лицевой счет");
                //}
            }
            ValuesFromFile.sql += ls_row_number + ", ";
            i++;

            //19. Количество строк - общедомовой прибор учета
            string odpu_row_number = this.CheckInt(ValuesFromFile.vals[i], true, 0, null, ref ret);
            if (!ret.result)
            {
                if (true)
                {
                    odpu_row_number = "0";
                }
                //else
                //{
                //    err.Append(rowNumber + ret.text + "Количество строк - общедомовой прибор учета");
                //}
            }
            ValuesFromFile.sql += odpu_row_number + ", ";
            i++;

            // nzp_ul, nzp_dom, comment
            ValuesFromFile.sql += "null,null,null";

            if (ValuesFromFile.versFull.Trim() == "'1.2.2'")
            {
                #region 20. Код улицы КЛАДР
                string kod_kladr = this.CheckText(ValuesFromFile.vals[i], true, 30, ref ret);
                if (!ret.result)
                {
                    ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + " Код улицы КЛАДР");
                }
                ValuesFromFile.sql += ", " + kod_kladr;
                i++;
                #endregion 11. Код улицы КЛАДР
            }
            ValuesFromFile.sql += ")";

        #endregion Загрузка 3 секции
    }

    /// <summary>
    ///  Загрузка 4 секции "Информация о лицевых счетах"
    /// </summary>
    /// <param name="ValuesFromFile"></param>
    public void AddFileKvar_Section4(DBValuesFromFile ValuesFromFile)
    {
        Returns ret = new Returns();

        if (!ValuesFromFile.finder.sections[4])
        { return; }

        if (ValuesFromFile.vals.Length < 35)
        {
            ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + " Неправильный формат файла загрузки: Информация о лицевых счетах");
            return;
        }

        #region Загрузка 4секции

        ValuesFromFile.sql =
                " INSERT INTO  " + Points.Pref + "_data" + tableDelimiter + "file_kvar " +
                "( id, nzp_file, ukas, dom_id, ls_type, fam, ima, otch, birth_date, nkvar, nkvar_n,open_date, opening_osnov, close_date, closing_osnov, " +
                " kol_gil, kol_vrem_prib, kol_vrem_ub, room_number, total_square, living_square, otapl_square, naim_square, is_communal, is_el_plita, " +
                " is_gas_plita, is_gas_colonka, is_fire_plita, gas_type, water_type, hotwater_type,canalization_type, is_open_otopl, params, " +
                " service_row_number, reval_params_row_number, ipu_row_number, id_urlic, nzp_dom, nzp_kvar) " +
                " VALUES(";
            int i = 1;

            #region 2. УКАС
            string ukas = this.CheckInt(ValuesFromFile.vals[i], false, null, null, ref ret);
            if (!ret.result)
            {
                ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "УКАС");
            }
            i++;
            #endregion

            #region 3. Уникальный код дома для строки типа 2 – реквизит 3.
            string nzp_dom = this.CheckInt(ValuesFromFile.vals[i].ToUpper().Replace("/", "00").Replace("А", "01").Replace("Б", "02").Replace("В", "03").Replace("Г", "04").Replace("D", "001").Replace("-", "002").Replace("О", "16").Replace("К", "12"), true, null, null, ref ret);
            if (!ret.result)
            {
                ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + " Уникальный код дома для строки типа 2 – реквизит 3");
            }
            //string nzp_dom = this.CheckInt(vals[i], true, 1, null, ref ret);
            //if (!ret.result)
            //{
            //    err.Append(rowNumber + ret.text + "Уникальный код дома для строки типа 2 – реквизит 3");
            //}

            i++;
            #endregion

            #region 4. № ЛС в системе поставщика
            string id = this.CheckInt(ValuesFromFile.vals[i], true, null, null, ref ret);
            if (!ret.result)
            {
                id = this.CheckText(ValuesFromFile.vals[i], false, 40, ref ret);
                if (!ret.result)
                {
                    //id = ret.text;
                    ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "№ ЛС в системе поставщика");
                }
            }
            i++;

            #endregion

            ValuesFromFile.sql += id + ", " + ValuesFromFile.finder.nzp_file + ", " + ukas + ", " + nzp_dom + ", ";


            #region 5. Тип ЛС (1 – жилая квартира, 2 – субабонент / арендатор)
            string tp = this.CheckInt(ValuesFromFile.vals[i], true, null, null, ref ret);
            if (!ret.result)
            {
                ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "Тип ЛС (1 – жилая квартира, 2 – субабонент / арендатор)");
            }
            ValuesFromFile.sql += tp + ", ";
            i++;
            #endregion

            #region 6. Фамилия квартиросъемщика
            string fam = this.CheckText(ValuesFromFile.vals[i], false, 200, ref ret);
            if (!ret.result)
            {
                ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "Фамилия квартиросъемщика");
            }
            ValuesFromFile.sql += fam + ", ";
            i++;
            #endregion

            #region 7. Имя квартиросъемщика
            string name = this.CheckText(ValuesFromFile.vals[i], false, 40, ref ret);
            if (!ret.result)
            {
                ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "Имя квартиросъемщика");

            }
            ValuesFromFile.sql += name + ", ";
            i++;
            #endregion

            #region 8. Отчество квартиросъемщика
            string sur = this.CheckText(ValuesFromFile.vals[i], false, 40, ref ret);
            if (!ret.result)
            {
                ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "Отчество квартиросъемщика");

            }
            ValuesFromFile.sql += sur + ", ";
            i++;
            #endregion

            #region 9. Дата рождения квартиросъемщика
            string burthDate = this.CheckDateTime(ValuesFromFile.vals[i], false, ref ret);
            if (!ret.result)
            {
                ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "Дата рождения квартиросъемщика");

            }
            ValuesFromFile.sql += burthDate + ", ";
            i++;
            #endregion

            #region 10. Квартира
            string kvar = this.CheckText(ValuesFromFile.vals[i], false, 10, ref ret);
            if (!ret.result)
            {
                ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "Квартира");
            }
            if (kvar == "null") kvar = "'-'";
            ValuesFromFile.sql += kvar + ", ";
            i++;
            #endregion

            #region 11. Комната лицевого счета
            string nkvar_n = this.CheckText(ValuesFromFile.vals[i], false, 3, ref ret);
            if (!ret.result)
            {
                ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + " Комната лицевого счета");

            }
            if (nkvar_n == "null") nkvar_n = "'-'";
            ValuesFromFile.sql += nkvar_n + ", ";
            i++;
            #endregion

            #region 12. Дата открытия ЛС
            string open_date = this.CheckDateTime(ValuesFromFile.vals[i], false, ref ret);
            if (!ret.result)
            {
                ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "Дата открытия ЛС");

            }
            ValuesFromFile.sql += open_date + ", ";
            i++;
            #endregion

            #region 13. Основание открытия ЛС
            string osnov = this.CheckText(ValuesFromFile.vals[i], false, 100, ref ret);
            if (!ret.result)
            {
                ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "Основание открытия ЛС");

            }
            ValuesFromFile.sql += osnov + ", ";
            i++;
            #endregion

            #region 14. Дата закрытия ЛС
            string dat_close = this.CheckDateTime(ValuesFromFile.vals[i], false, ref ret);
            if (!ret.result)
            {
                ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "Дата закрытия ЛС");

            }
            ValuesFromFile.sql += dat_close + ", ";
            i++;
            #endregion

            #region 15. Основание закрытия ЛС
            string close_osnov = this.CheckText(ValuesFromFile.vals[i], false, 100, ref ret);
            if (!ret.result)
            {
                ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "Основание закрытия ЛС");

            }
            ValuesFromFile.sql += close_osnov + ", ";
            i++;
            #endregion

            #region 16. Количество проживающих
            string col_gil = "";
            if (ValuesFromFile.Pvers == "1.0" || ValuesFromFile.Pvers == "1.1")
            {
                col_gil = this.CheckInt(ValuesFromFile.vals[i], false, 0, null, ref ret);
            }
            else
            {
                col_gil = this.CheckDecimal(ValuesFromFile.vals[i], true, false, 0, null, ref ret);
            }
            if (!ret.result)
            {
                ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "Количество проживающих");

            }
            if (col_gil == "null") { col_gil = "0"; };
            ValuesFromFile.sql += col_gil + ", ";
            i++;
            #endregion

            #region 17. Количество врем. прибывших жильцов
            if (ValuesFromFile.vals[i].Length == 0) { ValuesFromFile.vals[i] = "0"; };
            string col_prib = "";
            if (ValuesFromFile.Pvers == "1.0" || ValuesFromFile.Pvers == "1.1")
            {
                col_prib = this.CheckInt(ValuesFromFile.vals[i], false, 0, null, ref ret);
            }
            else
            {
                col_prib = this.CheckDecimal(ValuesFromFile.vals[i], true, false, 0, null, ref ret);
            }
            if (!ret.result)
            {
                ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "Количество проживающих");
            }
            if (!ret.result)
            {
                ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "Количество врем. прибывших жильцов");

            }
            if (col_prib == "null") { col_prib = "0"; };
            ValuesFromFile.sql += col_prib + ", ";
            i++;
            #endregion

            #region 18. Количество  врем. убывших жильцов
            if (ValuesFromFile.vals[i].Length == 0) { ValuesFromFile.vals[i] = "0"; };
            string col_ub = "";
            if (ValuesFromFile.Pvers == "1.0" || ValuesFromFile.Pvers == "1.1")
            {
                col_ub = this.CheckInt(ValuesFromFile.vals[i], false, 0, null, ref ret);
            }
            else
            {
                col_ub = this.CheckDecimal(ValuesFromFile.vals[i], true, false, 0, null, ref ret);
            }
            if (!ret.result)
            {
                ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "Количество проживающих");

            }
            if (!ret.result)
            {
                ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "Количество  врем. убывших жильцов");
            }
            if (col_ub == "null") { col_ub = "0"; };
            ValuesFromFile.sql += col_ub + ", ";
            i++;
            #endregion

            #region 19. Количество комнат
            string room = this.CheckInt(ValuesFromFile.vals[i], false, 0, null, ref ret);
            if (!ret.result)
            {
                ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "Количество комнат");
            }
            if (room == "null") { room = "0"; };
            ValuesFromFile.sql += room + ", ";
            i++;
            #endregion

            #region 20. Общая площадь
            string tot_sq = this.CheckDecimal(ValuesFromFile.vals[i], false, false, 0, null, ref ret);
            if (!ret.result)
            {
                ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "Общая площадь");
            }
            ValuesFromFile.sql += tot_sq + ", ";
            i++;
            #endregion

            #region 21. Жилая площадь
            string gil_sq = this.CheckDecimal(ValuesFromFile.vals[i], false, false, 0, null, ref ret);
            if (!ret.result)
            {
                ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "Жилая площадь");
            }
            ValuesFromFile.sql += gil_sq + ", ";
            i++;
            #endregion

            #region 22. Отапливаемая площадь
            string otap_sq = this.CheckDecimal(ValuesFromFile.vals[i], false, false, 0, null, ref ret);
            if (!ret.result)
            {
                ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "Отапливаемая площадь");

            }
            ValuesFromFile.sql += otap_sq + ", ";
            i++;
            #endregion

            #region 23. Площадь для найма
            string naim_sq = this.CheckDecimal(ValuesFromFile.vals[i], false, false, 0, null, ref ret);
            if (!ret.result)
            {
                ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "Площадь для найма");
            }
            ValuesFromFile.sql += naim_sq + ", ";
            i++;
            #endregion

            #region 24. Признак коммунальной квартиры(1-да, 0 –нет)
            string is_komm = this.CheckInt(ValuesFromFile.vals[i], true, 0, 1, ref ret);
            if (!ret.result)
            {
                ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "Признак коммунальной квартиры(1-да, 0 –нет)");
            }
            ValuesFromFile.sql += is_komm + ", ";
            i++;
            #endregion

            #region 25. Наличие эл. плиты (1-да, 0 –нет)
            string el_pl = this.CheckInt(ValuesFromFile.vals[i], false, 0, 1, ref ret);
            if (!ret.result)
            {
                ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "Наличие эл. плиты (1-да, 0 –нет)");

            }
            ValuesFromFile.sql += el_pl + ", ";
            i++;
            #endregion

            #region 26. Наличие газовой плиты (1-да, 0 –нет)
            string gas_pl = this.CheckInt(ValuesFromFile.vals[i], false, 0, 1, ref ret);
            if (!ret.result)
            {
                ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "Наличие газовой плиты (1-да, 0 –нет)");

            }
            ValuesFromFile.sql += gas_pl + ", ";
            i++;
            #endregion

            #region 27. Наличие газовой колонки (1-да, 0 –нет)
            string gas_col = this.CheckInt(ValuesFromFile.vals[i], false, 0, 1, ref ret);
            if (!ret.result)
            {
                ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "Наличие газовой колонки (1-да, 0 –нет)");
            }
            ValuesFromFile.sql += gas_col + ", ";
            i++;
            #endregion

            #region 28. Наличие огневой плиты (1-да, 0 –нет)
            string ogn_pl = this.CheckInt(ValuesFromFile.vals[i], false, 0, 1, ref ret);
            if (!ret.result)
            {
                ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "Наличие огневой плиты (1-да, 0 –нет)");
            }
            ValuesFromFile.sql += ogn_pl + ", ";
            i++;
            #endregion

            #region 29. Код типа жилья по газоснабжению (из справочника)
            string ktg_gas = this.CheckInt(ValuesFromFile.vals[i], false, 0, null, ref ret);
            if (!ret.result)
            {
                ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "Код типа жилья по газоснабжению (из справочника)");
            }
            ValuesFromFile.sql += ktg_gas + ", ";
            i++;
            #endregion

            #region 30. Код типа жилья по водоснабжению (из справочника)
            string ktg_water = this.CheckInt(ValuesFromFile.vals[i], false, 0, null, ref ret);
            if (!ret.result)
            {
                ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + " Код типа жилья по водоснабжению (из справочника)");
            }
            ValuesFromFile.sql += ktg_water + ", ";
            i++;
            #endregion

            #region 31. Код типа жилья по горячей воде (из справочника)
            string ktg_water_hot = this.CheckInt(ValuesFromFile.vals[i], false, 0, null, ref ret);
            if (!ret.result)
            {
                ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + " Код типа жилья по горячей воде (из справочника)");
            }
            ValuesFromFile.sql += ktg_water_hot + ", ";
            i++;
            #endregion

            #region 32. Код типа жилья по канализации (из справочника)
            string ktg_canal = this.CheckInt(ValuesFromFile.vals[i], false, 0, null, ref ret);
            if (!ret.result)
            {
                ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + " Код типа жилья по канализации (из справочника)");
            }
            ValuesFromFile.sql += ktg_canal + ", ";
            i++;
            #endregion

            #region 33. Наличие забора из открытой системы отопления (1-да, 0 –нет)
            string z_otop = this.CheckInt(ValuesFromFile.vals[i], false, 0, 1, ref ret);
            if (!ret.result)
            {
                ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "Наличие забора из открытой системы отопления (1-да, 0 –нет)");
            }
            ValuesFromFile.sql += z_otop + ", ";
            i++;
            #endregion

            #region 34. Дополнительные характеристики ЛС (задается в соответствии с правилами заполнения значений параметров)
            string dop_har = this.CheckText(ValuesFromFile.vals[i], false, 250, ref ret);
            if (!ret.result)
            {
                ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + " Дополнительные характеристики ЛС (задается в соответствии с правилами заполнения значений параметров)");
            }
            ValuesFromFile.sql += dop_har + ", ";
            i++;
            #endregion

            #region 35. Количество строк - услуга
            string rows = this.CheckInt(ValuesFromFile.vals[i], true, 0, null, ref ret);
            if (!ret.result)
            {
                rows = "0"; 
            }
            ValuesFromFile.sql += rows + ", ";
            i++;
            #endregion

            #region 36. Количество строк  – параметры в месяце перерасчета лицевого счета
            try
            {
                string rows_params = this.CheckInt(ValuesFromFile.vals[i], true, 0, null, ref ret);
                if (!ret.result)
                {
                    rows_params = "0";
                    //err.Append(rowNumber + ret.text + " Количество строк  – параметры в месяце перерасчета лицевого счета");
                }
                ValuesFromFile.sql += rows_params + ", ";
                i++;
            }
            catch (Exception ex)
            {
                MonitorLog.WriteException("Ошибка загрузки файла Характеристики жилого фонда и начисления ЖКУ", ex);
                ValuesFromFile.err.Append("Отсутствует | Количество строк  – параметры в месяце перерасчета лицевого счета");
            }
            #endregion

            //   try
            //   {
            #region 37. Количество строк – индивидуальный прибор учета
            string rows_ipu = this.CheckInt(ValuesFromFile.vals[i], true, 0, null, ref ret);
            if (!ret.result)
            {
                //if (true)
                { rows_ipu = "0"; }   // стал необязательным 
                //else
                //{
                //    err.Append(rowNumber + ret.text + " Количество строк – индивидуальный прибор учета");
                //}
            }
            ValuesFromFile.sql += rows_ipu + ", ";
            i++;
            #endregion
            //  }
            //  catch (Exception ex) { err.Append(rowNumber + ret.text + "Отсутствует | Количество строк – индивидуальный прибор учета"); }

            #region 38. Уникальный код ЮЛ
            string id_urlic;
            if (ValuesFromFile.Pvers != "1.0" && ValuesFromFile.vals.Length > 37)
            {
                id_urlic = this.CheckText(ValuesFromFile.vals[i], false, 20, ref ret);
                if (!ret.result)
                {
                    ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + " Уникальный код ЮЛ)");
                }
            }
            else id_urlic = "null";
            ValuesFromFile.sql += id_urlic + ", ";
            i++;
            #endregion

            ValuesFromFile.sql += "null,null);";

        #endregion Загрузка 4 секции
    }

    /// <summary>
    ///  Загрузка 5 секции "Поставщики услуг"
    /// </summary>
    /// <param name="ValuesFromFile"></param>
    public void AddFileSupp_Section5(DBValuesFromFile ValuesFromFile)
    {
        Returns ret = new Returns();
        if (!ValuesFromFile.finder.sections[5])
        {return;}

        if (ValuesFromFile.vals.Length < 11)
        {
            ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + " Неправильный формат файла загрузки: Поставщики услуг");
            return;
        }

        #region Загрузка 5 секции
        ValuesFromFile.sql =
            " INSERT INTO  " + Points.Pref + "_data" + tableDelimiter + " file_supp " +
            "( id, nzp_file, supp_id, supp_name, jur_address, fact_address, inn, kpp, rs, bank, bik, ks, nzp_supp) " +
            " VALUES( ";
        int i = 1;

        #region 2. Уникальный код поставщика
        string id = this.CheckInt(ValuesFromFile.vals[i], true, null, null, ref ret);
        if (!ret.result)
        {
            ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "УКАС");

        }
        i++;
        #endregion

        //id serial
        ValuesFromFile.sql += "0, ";

        //nzp_file
        ValuesFromFile.sql += ValuesFromFile.finder.nzp_file + ", " + id + ", "; //продублировал

        #region 3. Наименование поставщика
        string name_supp = this.CheckText(ValuesFromFile.vals[i], true, 40, ref ret);
        if (!ret.result)
        {
            ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "Наименование поставщика");

        }
        ValuesFromFile.sql += name_supp + ", ";
        i++;
        #endregion

        #region 4. Юридический адрес
        string jur_adr = this.CheckText(ValuesFromFile.vals[i], false, 100, ref ret);
        if (!ret.result)
        {
            ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "Юридический адрес");

        }
        ValuesFromFile.sql += jur_adr + ", ";
        i++;
        #endregion

        #region 5. Фактический адрес
        string fact_adr = this.CheckText(ValuesFromFile.vals[i], false, 100, ref ret);
        if (!ret.result)
        {
            ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "Фактический адрес");

        }
        ValuesFromFile.sql += fact_adr + ", ";
        i++;
        #endregion

        #region 6. ИНН
        string inn = this.CheckText(ValuesFromFile.vals[i], true, 20, ref ret);
        if (!ret.result)
        {
            ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "ИНН");

        }
        ValuesFromFile.sql += inn + ", ";
        i++;
        #endregion

        #region 7. КПП
        string kpp = this.CheckText(ValuesFromFile.vals[i], true, 20, ref ret);
        if (!ret.result)
        {
            ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "КПП");

        }
        ValuesFromFile.sql += kpp + ", ";
        i++;
        #endregion

        #region 8. Расчетный счет
        string rchet = this.CheckText(ValuesFromFile.vals[i].Trim(), false, 20, ref ret);
        if (!ret.result)
        {
            ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "Расчетный счет");

        }
        ValuesFromFile.sql += rchet + ", ";
        i++;
        #endregion

        #region 9. Банк
        string bank = this.CheckText(ValuesFromFile.vals[i], false, 100, ref ret);
        if (!ret.result)
        {
            ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "Банк");

        }
        ValuesFromFile.sql += bank + ", ";
        i++;
        #endregion

        #region 10. БИК банка
        string bik = this.CheckText(ValuesFromFile.vals[i], false, 20, ref ret);
        if (!ret.result)
        {
            ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + " БИК банка");

        }
        ValuesFromFile.sql += bik + ", ";
        i++;
        #endregion

        #region 11. Корреспондентский счет
        string kschet = this.CheckText(ValuesFromFile.vals[i], false, 20, ref ret);
        if (!ret.result)
        {
            ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "Корреспондентский счет");

        }
        ValuesFromFile.sql += kschet + ", ";
        i++;
        #endregion

        //nzp_supp
        ValuesFromFile.sql += "null)";

        #endregion Загрузка 5 секции
    }

    /// <summary>
    ///  Загрузка 6 секции "Информация об оказываемых услугах"
    /// </summary>
    /// <param name="ValuesFromFile"></param>
    public void AddFileServ_Section6(DBValuesFromFile ValuesFromFile)
    {
        Returns ret = new Returns();

        if (!ValuesFromFile.finder.sections[6])
        {return;}

        if (ValuesFromFile.vals.Length < 24)
        {
            ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + " Неправильный формат файла загрузки: Информация об оказываемых услугах");
            return;
        }

        #region Загрузка 6 секции
            ValuesFromFile.sql =
                " INSERT INTO  " + Points.Pref + "_data" + tableDelimiter + "file_serv " +
                "(nzp_file, ls_id, supp_id, nzp_serv, sum_insaldo, eot, reg_tarif_percent, reg_tarif, nzp_measure, fact_rashod, norm_rashod, is_pu_calc, sum_nach, sum_reval, " +
                "sum_subsidy, sum_subsidyp, sum_lgota, sum_lgotap,sum_smo, sum_smop, sum_money, is_del, sum_outsaldo, servp_row_number, nzp_kvar, nzp_supp) " +
                " VALUES (";
            int i = 1;

            //nzp_file
            ValuesFromFile.sql += ValuesFromFile.finder.nzp_file + ", ";


            #region 2. № ЛС в системе поставщика
            string ls_id = this.CheckInt(ValuesFromFile.vals[i], true, 1, null, ref ret).ToString();
            if (!ret.result)
            {
                ls_id = this.CheckText(ValuesFromFile.vals[i], true, 20, ref ret);
                if (!ret.result)
                {
                    ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "№ ЛС в системе поставщика");

                }
            }
            ValuesFromFile.sql += ls_id + ", ";
            i++;
            #endregion

            #region 3. Код поставщика услуг
            string supp_id = this.CheckInt(ValuesFromFile.vals[i], true, 1, null, ref ret);
            if (!ret.result)
            {
                ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "Код поставщика услуг");

            }
            ValuesFromFile.sql += supp_id + ", ";
            i++;
            #endregion

            #region 4. Код услуги (из справочника)
            string nzp_serv = this.CheckInt(ValuesFromFile.vals[i], true, 1, null, ref ret);
            if (!ret.result)
            {
                ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "Код услуги (из справочника)");

            }
            ValuesFromFile.sql += nzp_serv + ", ";
            i++;
            #endregion

            #region 5. Входящее сальдо (Долг на начало месяца)
            //string saldo_in = this.CheckDecimal(vals[i], true, true, null, null, ref ret);
            string saldo_in = this.CheckDecimal(ValuesFromFile.vals[i], false, true, null, null, ref ret); // Ослабил для Губкина 
            if (!ret.result)
            {
                ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "Входящее сальдо (Долг на начало месяца)");

            }
            if (saldo_in == "null") { saldo_in = "0"; };
            ValuesFromFile.sql += saldo_in + ", ";
            i++;
            #endregion

            #region 6. Экономически обоснованный тариф
            string eot = this.CheckDecimal(ValuesFromFile.vals[i], true, false, null, null, ref ret);
            if (!ret.result)
            {
                ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "Экономически обоснованный тариф ");

            }
            ValuesFromFile.sql += eot + ", ";
            i++;
            #endregion

            #region 7. Процент регулируемого тарифа от экономически обоснованного
            string reg_tarif_percent = this.CheckDecimal(ValuesFromFile.vals[i], true, false, 0, 100, ref ret);
            if (!ret.result)
            {
                ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "Процент регулируемого тарифа от экономически обоснованного");

            }
            #endregion

            #region  8. Регулируемый тариф
            decimal? check = null;
            try
            {
                decimal checkt = Convert.ToDecimal(eot) * Convert.ToDecimal(reg_tarif_percent) / 100;
                checkt = Decimal.Round(checkt, 2);
                check = checkt;
                //проверка параметра 6
                //eot = this.CheckDecimal(vals[i], true, false, Convert.ToDecimal(check), null, ref ret);

                eot = this.CheckDecimal(ValuesFromFile.vals[i], true, false, Convert.ToDecimal(check), null, ref ret);
                if (!ret.result)
                {
                    // нужно вычислить правильный процент
                    // но мы вычислать не будем пока
                    // err.Append(rowNumber + ret.text + "Экономически обоснованный тариф ");
                    eot = this.CheckDecimal(ValuesFromFile.vals[i], true, false, null, null, ref ret);
                    if (!ret.result)
                    {
                        ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "Экономически обоснованный тариф ");
                    }
                }
            }
            catch (Exception) { }

            ValuesFromFile.sql += reg_tarif_percent + ", ";
            i++;

            //проверка параметра 8
            string reg_tarif = this.CheckDecimal(ValuesFromFile.vals[i], true, false, null, null, ref ret);
            if (!ret.result)
            {
                ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "Регулируемый тариф ");

            }
            ValuesFromFile.sql += reg_tarif + ", ";
            i++;
            #endregion

            #region 9. Код единицы измерения расхода (из справочника)
            string nzp_measure = this.CheckInt(ValuesFromFile.vals[i], true, 1, null, ref ret);
            if (!ret.result)
            {
                ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "Код единицы измерения расхода (из справочника) ");

            }
            ValuesFromFile.sql += nzp_measure + ", ";
            i++;
            #endregion

            #region 10. Расход фактический
            string ras_fact = this.CheckDecimal(ValuesFromFile.vals[i], true, false, 0, null, ref ret);
            if (!ret.result)
            {
                ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "Расход фактический");

            }
            ValuesFromFile.sql += ras_fact + ", ";
            i++;
            #endregion

            #region 11. Расход по нормативу
            string ras_norm = this.CheckDecimal(ValuesFromFile.vals[i], true, false, 0, null, ref ret);
            if (!ret.result)
            {
                ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "Расход по нормативу");

            }
            ValuesFromFile.sql += ras_norm + ", ";
            i++;
            #endregion

            #region 12. Вид расчета по прибору учета(1-по счетчику / 0-без счетчика)
            string is_pu_calc = this.CheckDecimal(ValuesFromFile.vals[i], true, false, 0, 1, ref ret);
            if (!ret.result)
            {
                ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "Расход по нормативу");

            }
            ValuesFromFile.sql += is_pu_calc + ", ";
            i++;
            #endregion

            #region 13. Сумма начисления
            //check = Convert.ToDecimal(eot) * Convert.ToDecimal(ras_fact);
            //check = Decimal.Round(check, 2,MidpointRounding.ToEven);
            //string sum_nach = this.CheckDecimal(vals[i], true, false, null, null, ref ret); 
            string sum_nach = this.CheckDecimal(ValuesFromFile.vals[i], false, false, null, null, ref ret); // ослабил для Губкина
            if (!ret.result)
            {
                ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "Сумма начисления");

            }
            if (sum_nach == "null") { sum_nach = "0"; };

            ValuesFromFile.sql += sum_nach + ", ";
            i++;
            #endregion

            #region 14. Сумма перерасчета начисления за предыдущий период (изменение сальдо)
            string sum_per = this.CheckDecimal(ValuesFromFile.vals[i], true, true, null, null, ref ret);
            if (!ret.result)
            {
                ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "Сумма перерасчета начисления за предыдущий период (изменение сальдо)");

            }
            ValuesFromFile.sql += sum_per + ", ";
            i++;
            #endregion

            #region 15. Сумма дотации
#warning выставить ноль, если пусто. слделать и для  след.полей
            string sum_dot = this.CheckDecimal(ValuesFromFile.vals[i], false, true, 0, null, ref ret);
            if (!ret.result)
            {
                ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "Сумма дотации");

            }
            ValuesFromFile.sql += sum_dot + ", ";
            i++;
            #endregion

            #region 16. Сумма перерасчета дотации за предыдущий период (за все месяца)
#warning поставил как обязательное для заполнения. иначе - нарушение ограничения not null
            string sum_dot_per = this.CheckDecimal(ValuesFromFile.vals[i], true, true, null, null, ref ret);
            if (!ret.result)
            {
                ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "Сумма перерасчета дотации за предыдущий период (за все месяца)");

            }
            ValuesFromFile.sql += sum_dot_per + ", ";
            i++;
            #endregion

            #region 17. Сумма льготы (Общая сумма льготы по всем категориям льгот для данной услуги)
#warning поставил как обязательное для заполнения. иначе - нарушение ограничения not null
            string sum_lgota = this.CheckDecimal(ValuesFromFile.vals[i], true, true, 0, null, ref ret);
            if (!ret.result)
            {
                ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "Сумма льготы (Общая сумма льготы по всем категориям льгот для данной услуги)");

            }
            ValuesFromFile.sql += sum_lgota + ", ";
            i++;
            #endregion

            #region 18. Сумма перерасчета льготы за предыдущий период (Общая по всем категориям льгот для данной услуги) (за все месяца)
#warning поставил как обязательное для заполнения. иначе - нарушение ограничения not null
            string sum_lgota_per = this.CheckDecimal(ValuesFromFile.vals[i], true, true, null, null, ref ret);
            if (!ret.result)
            {
                ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "Сумма перерасчета льготы за предыдущий период (Общая по всем категориям льгот для данной услуги) (за все месяца)");

            }
            ValuesFromFile.sql += sum_lgota_per + ", ";
            i++;
            #endregion

            #region 19. Сумма СМО
#warning поставил как обязательное для заполнения. иначе - нарушение ограничения not null
            string smo = this.CheckDecimal(ValuesFromFile.vals[i], true, true, 0, null, ref ret);
            if (!ret.result)
            {
                ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "Сумма СМО");

            }
            ValuesFromFile.sql += smo + ", ";
            i++;
            #endregion

            #region 20. Сумма перерасчета  СМО за предыдущий период (за все месяца)
#warning поставил как обязательное для заполнения. иначе - нарушение ограничения not null
            string smo_per = this.CheckDecimal(ValuesFromFile.vals[i], true, true, null, null, ref ret);
            if (!ret.result)
            {
                ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "Сумма перерасчета  СМО за предыдущий период (за все месяца)");

            }
            ValuesFromFile.sql += smo_per + ", ";
            i++;
            #endregion

            #region 21. Сумма оплаты, поступившие за месяц начислений
            string sum_opl = this.CheckDecimal(ValuesFromFile.vals[i], true, true, null, null, ref ret);
            if (!ret.result)
            {
                ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "Сумма оплаты, поступившие за месяц начислений");

            }
            ValuesFromFile.sql += sum_opl + ", ";
            i++;
            #endregion

            #region 22. Признак удаленности услуги
            string is_del = this.CheckInt(ValuesFromFile.vals[i], true, 0, 1, ref ret);
            if (!ret.result)
            {
                if (ValuesFromFile.vals[i] == "0.00") { ret.result = true; }
                else
                    ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "Признак удаленности услуги");

            }
            ValuesFromFile.sql += is_del + ", ";
            i++;
            #endregion

            #region 23. Исходящее сальдо (Долг на окончание месяца)
            //string outsaldo = this.CheckDecimal(vals[i], true, true, null, null, ref ret);
            string outsaldo = this.CheckDecimal(ValuesFromFile.vals[i], true, false, null, null, ref ret, true); // Убрал условие из_моней для Губкина, добавил условие из_губкин
            if (!ret.result)
            {
                ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "Исходящее сальдо (Долг на окончание месяца)");

            }
            if (outsaldo == "null") { outsaldo = "0"; };
            // округление до 2 знаков                            
            ValuesFromFile.sql += outsaldo + ", ";
            i++;
            #endregion

            #region 24. Количество строк – перерасчетов начисления по услуге

            string rows = this.CheckInt(ValuesFromFile.vals[i], true, 0, null, ref ret);
            if (!ret.result)
            {
                rows = "0";
                //  err.Append(rowNumber + ret.text + "Количество строк – перерасчетов начисления по услуге");
            }
            ValuesFromFile.sql += rows + ", ";
            i++;
            #endregion

            //nzp_kvar, nzp_supp
            ValuesFromFile.sql += "null,null);";
            #endregion        
    }
        

    /// <summary>
    ///  Загрузка 7 секции "Информация о параметрах лицевых счетов в месяце перерасчета"
    /// </summary>
    /// <param name="ValuesFromFile"></param>
    public void AddFileKvarp_Section7(DBValuesFromFile ValuesFromFile)
    {
        Returns ret = new Returns();
        if (!ValuesFromFile.finder.sections[7])
        { return;}

        if (ValuesFromFile.vals.Length < 32)
        {
            ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + " Неправильный формат файла загрузки: Информация о параметрах лицевых счетов в месяце перерасчета");
            return;
        }
        
        #region загрузка 7 секции
            ValuesFromFile.sql = 
                " INSERT INTO  " + Points.Pref + "_data" + tableDelimiter + "file_kvarp( id, reval_month, nzp_file, fam, ima, otch, birth_date, nkvar, nkvar_n, open_date, opening_osnov, close_date,  "+
                " closing_osnov, kol_gil, kol_vrem_prib, kol_vrem_ub, room_number, total_square, living_square, otapl_square, naim_square, is_communal, "+
                " is_el_plita, is_gas_plita, is_gas_colonka, is_fire_plita, gas_type, water_type, hotwater_type, canalization_type, is_open_otopl, params, "+
                " nzp_dom, nzp_kvar, comment, nzp_status)   "+
                " VALUES( ";
            int i = 1;


            //2. 2. Месяц и год перерасчета
            string my_per = this.CheckDateTime(ValuesFromFile.vals[i], true, ref ret);
            if (!ret.result)
            {
                ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "Месяц и год перерасчета");

            }
            //sql += my_per + ", ";
            i++;


            //nzp_file
            //sql += finder.nzp_file + ", ";


            //3. № ЛС в системе поставщика
            string ls = this.CheckText(ValuesFromFile.vals[i], true, 20, ref ret);
            if (!ret.result)
            {
                ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "№ ЛС в системе поставщика");

            }
            //sql += ls + ", ";
            i++;

            ValuesFromFile.sql += ls + ", " + my_per + ", " + ValuesFromFile.finder.nzp_file + ", ";

            //4. Фамилия квартиросъемщика
            string fam = this.CheckText(ValuesFromFile.vals[i], false, 40, ref ret);
            if (!ret.result)
            {
                ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "Фамилия квартиросъемщика");

            }
            ValuesFromFile.sql += fam + ", ";
            i++;


            //5. Имя квартиросъемщика
            string name = this.CheckText(ValuesFromFile.vals[i], false, 40, ref ret);
            if (!ret.result)
            {
                ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "Имя квартиросъемщика");

            }
            ValuesFromFile.sql += name + ", ";
            i++;

            //6. Отчество квартиросъемщика
            string patronymic = this.CheckText(ValuesFromFile.vals[i], false, 40, ref ret);
            if (!ret.result)
            {
                ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "Отчество квартиросъемщика");

            }
            ValuesFromFile.sql += patronymic + ", ";
            i++;


            //7. Дата рождения квартиросъемщика
            string patronym = this.CheckDateTime(ValuesFromFile.vals[i], false, ref ret);
            if (!ret.result)
            {
                ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "Дата рождения квартиросъемщика");

            }
            ValuesFromFile.sql += patronym + ", ";
            i++;


            //8. Квартира
            string kvar = this.CheckText(ValuesFromFile.vals[i], false, 10, ref ret);
            if (!ret.result)
            {
                ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "Квартира");

            }
            ValuesFromFile.sql += kvar + ", ";
            i++;

            //9. Комната
            string kom = this.CheckText(ValuesFromFile.vals[i], false, 3, ref ret);
            if (!ret.result)
            {
                ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "Комната");

            }
            ValuesFromFile.sql += kom + ", ";
            i++;

            //10. Дата открытия ЛС
            string dat_openLs = this.CheckDateTime(ValuesFromFile.vals[i], false, ref ret);
            if (!ret.result)
            {
                ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "Дата открытия ЛС");

            }
            ValuesFromFile.sql += dat_openLs + ", ";
            i++;

            //11. Основание открытия ЛС
            string osnov_ls = this.CheckText(ValuesFromFile.vals[i], false, 100, ref ret);
            if (!ret.result)
            {
                ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "Основание открытия ЛС");

            }
            ValuesFromFile.sql += osnov_ls + ", ";
            i++;

            //12. Дата закрытия ЛС
            string datCloseLs = this.CheckDateTime(ValuesFromFile.vals[i], false, ref ret);
            if (!ret.result)
            {
                ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "Дата закрытия ЛС");

            }
            ValuesFromFile.sql += datCloseLs + ", ";
            i++;

            //13. Основание закрытия ЛС
            string osnovClose = this.CheckText(ValuesFromFile.vals[i], false, 100, ref ret);
            if (!ret.result)
            {
                ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "Основание закрытия ЛС");

            }
            ValuesFromFile.sql += osnovClose + ", ";
            i++;

            //14. Количество проживающих
            string countLiv = this.CheckInt(ValuesFromFile.vals[i], false, 0, null, ref ret);
            if (!ret.result)
            {
                ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "Количество проживающих");

            }
            if (countLiv == "null") { countLiv = "0"; };
            ValuesFromFile.sql += countLiv + ", ";
            i++;

            //15. Количество врем. Прибывших жильцов
            string countTempLiv = this.CheckInt(ValuesFromFile.vals[i], false, 0, null, ref ret);
            if (!ret.result)
            {
                ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "Количество врем. Прибывших жильцов");

            }
            if (countTempLiv == "null") { countTempLiv = "0"; };

            ValuesFromFile.sql += countTempLiv + ", ";
            i++;

            //16. Количество  врем. Убывших жильцов
            string countTempOut = this.CheckInt(ValuesFromFile.vals[i], false, 0, null, ref ret);
            if (!ret.result)
            {
                ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "Количество  врем. Убывших жильцов");

            }
            if (countTempOut == "null") { countTempOut = "0"; };
            ValuesFromFile.sql += countTempOut + ", ";
            i++;

            //17. Количество комнат
            string countRoom = this.CheckInt(ValuesFromFile.vals[i], false, 0, null, ref ret);
            if (!ret.result)
            {
                ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "Количество комнат");

            }
            if (countRoom == "null") { countRoom = "0"; };
            ValuesFromFile.sql += countRoom + ", ";
            i++;


            //18. Общая площадь
            string totSq = this.CheckDecimal(ValuesFromFile.vals[i], false, false, 0, null, ref ret);
            if (!ret.result)
            {
                ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "Общая площадь");

            }
            ValuesFromFile.sql += totSq + ", ";
            i++;

            //19. Жилая площадь
            string livSq = this.CheckDecimal(ValuesFromFile.vals[i], false, false, 0, null, ref ret);
            if (!ret.result)
            {
                ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "Жилая площадь");

            }
            ValuesFromFile.sql += livSq + ", ";
            i++;


            //20. Отапливаемая площадь
            string warmSq = this.CheckDecimal(ValuesFromFile.vals[i], false, false, 0, null, ref ret);
            if (!ret.result)
            {
                ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "Отапливаемая площадь");

            }
            ValuesFromFile.sql += warmSq + ", ";
            i++;

            //21. Площадь для найма
            string rentSq = this.CheckDecimal(ValuesFromFile.vals[i], false, false, 0, null, ref ret);
            if (!ret.result)
            {
                ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "Площадь для найма");

            }
            ValuesFromFile.sql += rentSq + ", ";
            i++;

            //22. Признак коммунальной квартиры(1-да, 0 –нет)
            string comKv = this.CheckInt(ValuesFromFile.vals[i], false, 0, 1, ref ret);
            if (!ret.result)
            {
                ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "Признак коммунальной квартиры(1-да, 0 –нет)");

            }
            ValuesFromFile.sql += comKv + ", ";
            i++;


            //23. Наличие эл. Плиты (1-да, 0 –нет)
            string is_plate = this.CheckInt(ValuesFromFile.vals[i], false, 0, 1, ref ret);
            if (!ret.result)
            {
                ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "Наличие эл. Плиты (1-да, 0 –нет)");

            }
            ValuesFromFile.sql += is_plate + ", ";
            i++;

            //24. Наличие газовой плиты (1-да, 0 –нет)
            string is_plateGas = this.CheckInt(ValuesFromFile.vals[i], false, 0, 1, ref ret);
            if (!ret.result)
            {
                ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "Наличие газовой плиты (1-да, 0 –нет)");

            }
            ValuesFromFile.sql += is_plateGas + ", ";
            i++;


            //25. Наличие газовой колонки (1-да, 0 –нет
            string is_geyser = this.CheckInt(ValuesFromFile.vals[i], false, 0, 1, ref ret);
            if (!ret.result)
            {
                ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "Наличие газовой колонки (1-да, 0 –нет");

            }
            ValuesFromFile.sql += is_geyser + ", ";
            i++;

            //26. Наличие огневой плиты (1-да, 0 –нет)
            string is_plateFire = this.CheckInt(ValuesFromFile.vals[i], false, 0, 1, ref ret);
            if (!ret.result)
            {
                ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "Наличие огневой плиты (1-да, 0 –нет)");

            }
            ValuesFromFile.sql += is_plateFire + ", ";
            i++;

            //27. Код типа жилья по газоснабжению (из справочника)
            string codeGas = this.CheckInt(ValuesFromFile.vals[i], false, 1, null, ref ret);
            if (!ret.result)
            {
                ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "Код типа жилья по газоснабжению (из справочника)");

            }
            ValuesFromFile.sql += codeGas + ", ";
            i++;


            //28. Код типа жилья по водоснабжению (из справочника)
            string codWater = this.CheckInt(ValuesFromFile.vals[i], false, null, null, ref ret);
            if (!ret.result)
            {
                ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + " Код типа жилья по водоснабжению (из справочника)");

            }
            ValuesFromFile.sql += codWater + ", ";
            i++;

            //29. Код типа жилья по горячей воде (из справочника)
            string codWaterHot = this.CheckInt(ValuesFromFile.vals[i], false, null, null, ref ret);
            if (!ret.result)
            {
                ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + " Код типа жилья по горячей воде (из справочника)");

            }
            ValuesFromFile.sql += codWaterHot + ", ";
            i++;

            //30. Код типа жилья по канализации (из справочника)
            string codCanal = this.CheckInt(ValuesFromFile.vals[i], false, null, null, ref ret);
            if (!ret.result)
            {
                ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + " Код типа жилья по канализации (из справочника)");

            }
            ValuesFromFile.sql += codCanal + ", ";
            i++;

            //31. Наличие забора из открытой системы отопления (1-да, 0 –нет)
            string codOpen = this.CheckInt(ValuesFromFile.vals[i], false, 0, 1, ref ret);
            if (!ret.result)
            {
                ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "Наличие забора из открытой системы отопления (1-да, 0 –нет)");

            }
            ValuesFromFile.sql += codOpen + ", ";
            i++;

            //32. Дополнительные характеристики ЛС (заполняется в соответствии с форматом задания значений параметров)
            string additionalHar = this.CheckText(ValuesFromFile.vals[i], false, 250, ref ret);
            if (!ret.result)
            {
                ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "Основание закрытия ЛС");

            }
            ValuesFromFile.sql += additionalHar + ", ";
            i++;


            //nzp_dom, nzp_kvar, comment, nzp_status
            ValuesFromFile.sql += " null,null,null,null); ";

            #endregion                
    }


    /// <summary>
    ///  Загрузка 8 секции "Информация о перерасчетах начислений по услугам"
    /// </summary>
    /// <param name="ValuesFromFile"></param>
    public void AddFileServp_Section8(DBValuesFromFile ValuesFromFile)
    {
        Returns ret = new Returns();
        if (!ValuesFromFile.finder.sections[8])
        {return;}

        if (ValuesFromFile.vals.Length < 16)
        {
            ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + " Неправильный формат файла загрузки: Информация о перерасчетах начислений по услугам");
            return;
        }

        #region Загрузка 8 секции
        ValuesFromFile.sql = " INSERT INTO  " + Points.Pref + "_data" + tableDelimiter + "file_servp( nzp_file, reval_month, ls_id, supp_id, nzp_serv, eot, reg_tarif_percent, reg_tarif, nzp_measure,  " +
            " fact_rashod, norm_rashod, is_pu_calc, sum_reval, sum_subsidyp, sum_lgotap, sum_smop, nzp_kvar, nzp_supp) "+
            "VALUES (";
        int i = 1;

        //nzp_file
        ValuesFromFile.sql += ValuesFromFile.finder.nzp_file + ", ";


        //2. 2. Месяц и год перерасчета
        string my_per = this.CheckDateTime(ValuesFromFile.vals[i], false, ref ret);
        if (!ret.result)
        {
            ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "Месяц и год перерасчета");
        }
        ValuesFromFile.sql += my_per + ", ";
        i++;

        //3. № ЛС в системе поставщика
        string ls = this.CheckText(ValuesFromFile.vals[i], true, 20, ref ret);
        if (!ret.result)
        {
            ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "№ ЛС в системе поставщика");
        }
        ValuesFromFile.sql += ls + ", ";
        i++;

        //4. Код поставщика.
        string supp_id = this.CheckInt(ValuesFromFile.vals[i], false, 1, null, ref ret);
        if (!ret.result)
        {
            ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "Код поставщика");
        }
        ValuesFromFile.sql += supp_id + ", ";
        i++;

        //5. Код услуги (из справочника)
        string nzp_serv = this.CheckInt(ValuesFromFile.vals[i], true, 1, null, ref ret);
        if (!ret.result)
        {
            ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "Код услуги (из справочника)");
        }
        ValuesFromFile.sql += nzp_serv + ", ";
        i++;

        //6. Экономически обоснованный тариф 
        string eot = this.CheckDecimal(ValuesFromFile.vals[i], false, false, null, null, ref ret);
        if (!ret.result)
        {
            ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "Экономически обоснованный тариф ");
        }
        ValuesFromFile.sql += eot + ", ";
        i++;


        //7. Процент регулируемого тарифа от экономически обоснованного
        string reg_tarif_percent = this.CheckDecimal(ValuesFromFile.vals[i], true, false, 0, 100, ref ret);
        if (!ret.result)
        {
            ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "Процент регулируемого тарифа от экономически обоснованного");
        }
        ValuesFromFile.sql += reg_tarif_percent + ", ";
        i++;

        //8. Регулируемый тариф                        
        decimal? check = null;
        try
        {
            decimal checkt = Convert.ToDecimal(eot) * Convert.ToDecimal(reg_tarif_percent) / 100;
            checkt = Decimal.Round(checkt, 2);
            check = checkt;
            //проверка параметра 6
            eot = this.CheckDecimal(ValuesFromFile.vals[i], true, false, Convert.ToDecimal(check), null, ref ret);
            if (!ret.result)
            {
                ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "Экономически обоснованный тариф ");
            }
        }
        catch (Exception) { }

        //проверка параметра 8
        string reg_tarif = this.CheckDecimal(ValuesFromFile.vals[i], true, false, check, check, ref ret);
        if (!ret.result)
        {
            ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "Регулируемый тариф ");
        }
        ValuesFromFile.sql += reg_tarif + ", ";
        i++;

        //9. Код единицы измерения расхода (из справочника)                            
        string nzp_measure = this.CheckInt(ValuesFromFile.vals[i], true, 1, null, ref ret);
        if (!ret.result)
        {
            ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "Код единицы измерения расхода (из справочника) ");
        }
        ValuesFromFile.sql += nzp_measure + ", ";
        i++;

        //10. Расход фактический
        string ras_fact = this.CheckDecimal(ValuesFromFile.vals[i], true, false, null, null, ref ret);
        if (!ret.result)
        {
            ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "Расход фактический");
        }
        ValuesFromFile.sql += ras_fact + ", ";
        i++;

        //11. Расход по нормативу
        string ras_norm = this.CheckDecimal(ValuesFromFile.vals[i], true, false, 0, null, ref ret);
        if (!ret.result)
        {
            ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "Расход по нормативу");
        }
        ValuesFromFile.sql += ras_norm + ", ";
        i++;

        //12. Вид расчета по прибору учета(1-по счетчику / 0-без счетчика)
        string is_pu_calc = this.CheckDecimal(ValuesFromFile.vals[i], true, false, 0, 1, ref ret);
        if (!ret.result)
        {
            ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "Расход по нормативу");
        }
        ValuesFromFile.sql += is_pu_calc + ", ";
        i++;

        //13. Сумма перерасчета начисления за месяц перерасчета
        string sumPerMonthPer = this.CheckDecimal(ValuesFromFile.vals[i], true, true, null, null, ref ret);
        if (!ret.result)
        {
            ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "Сумма перерасчета начисления за месяц перерасчета");
        }
        ValuesFromFile.sql += sumPerMonthPer + ", ";
        i++;

        //14. Сумма перерасчета дотации за месяц перерасчета
        string sumPerSubsMothPer = this.CheckDecimal(ValuesFromFile.vals[i], true, true, null, null, ref ret);
        if (!ret.result)
        {
            ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "Сумма перерасчета дотации за месяц перерасчета");
        }
        ValuesFromFile.sql += sumPerSubsMothPer + ", ";
        i++;

        //15. Сумма перерасчета льготы за месяц перерасчета (Общая по всем категориям льгот для данной услуги)
        string sumPerLgot = this.CheckDecimal(ValuesFromFile.vals[i], true, true, null, null, ref ret);
        if (!ret.result)
        {
            ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "Сумма перерасчета льготы за месяц перерасчета (Общая по всем категориям льгот для данной услуги)");
        }
        ValuesFromFile.sql += sumPerLgot + ", ";
        i++;

        //16. Сумма перерасчета СМО за месяц перерасчета
        string sumPerSmo = this.CheckDecimal(ValuesFromFile.vals[i], true, true, null, null, ref ret);
        if (!ret.result)
        {
            ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "Сумма перерасчета СМО за месяц перерасчета");
        }
        ValuesFromFile.sql += sumPerSmo + ", ";
        i++;

        //nzp_kvar, nzp_supp
        ValuesFromFile.sql += "null,null);";
        #endregion        
    }


    /// <summary>
    ///  Загрузка 9 секции "Информация об общедомовых приборах учета"
    /// </summary>
    /// <param name="ValuesFromFile"></param>
    public void AddFileOdpu_Section9(DBValuesFromFile ValuesFromFile)
    {
        Returns ret = new Returns();

        if (!ValuesFromFile.finder.sections[9])
        { return; }

        if (ValuesFromFile.Pvers == "1.0")
        {
            #region Версия 1.0 (Здесь ОДПУ с показаниями )

                                        #region Заготовка Insert
            ValuesFromFile.sql = 
                " INSERT INTO  " + Points.Pref + "_data" + tableDelimiter + "file_odpu( nzp_file, dom_id, nzp_serv, rashod_type, serv_type, counter_type, cnt_stage,  "+
                " mmnog, num_cnt, dat_uchet, val_cnt, nzp_measure, dat_prov, dat_provnext, nzp_dom, nzp_counter) "+
                " VALUES(";
                                        #endregion Заготовка Insert

                                        #region Номер файла
                                        int i = 1;

                                        //nzp_file
                                        ValuesFromFile.sql += ValuesFromFile.finder.nzp_file + ", ";
                                        #endregion Номер файла

                                        #region 2. Уникальный код дома для строки типа 2 – реквизит 3.
                                        //string codeDom = this.CheckInt(vals[i], true, 1, null, ref ret);
                                        string codeDom = this.CheckInt(ValuesFromFile.vals[i].ToUpper().Replace("/", "00").Replace("А", "01").Replace("Б", "02").Replace("В", "03").Replace("Г", "04").Replace("D", "001").Replace("-", "002"), true, null, null, ref ret);
                                        if (!ret.result)
                                        {
                                            ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + " Уникальный код дома для строки типа 2 – реквизит 3.");
                                        }
                                        ValuesFromFile.sql += codeDom + ", ";
                                        i++;
                                        #endregion 2. Уникальный код дома для строки типа 2 – реквизит 3.

                                        #region 3. Код услуги (из справочника)
                                        string codeServ = this.CheckInt(ValuesFromFile.vals[i], true, 1, null, ref ret);
                                        if (!ret.result)
                                        {
                                            ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "  Код услуги (из справочника)");

                                        }
                                        ValuesFromFile.sql += codeServ + ", ";
                                        i++;
                                        #endregion 3. Код услуги (из справочника)

                                        #region 4. Тип расхода (1 – при выгрузке показаний, 2 – при выгрузке месячного расхода)
                                        string typeExpenditure = this.CheckInt(ValuesFromFile.vals[i], true, null, null, ref ret);
                                        if (!ret.result)
                                        {
                                            ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "Тип расхода (1 – при выгрузке показаний, 2 – при выгрузке месячного расхода)");

                                        }
                                        ValuesFromFile.sql += typeExpenditure + ", ";
                                        i++;
                                        #endregion 4. Тип расхода (1 – при выгрузке показаний, 2 – при выгрузке месячного расхода)

                                        #region 5. Тип услуги (тарифная зона: 1 –дневное потребление, 2 –ночное потребление)
                                        //string typeService = this.CheckInt(vals[i], true , null, null, ref ret);
                                        string typeService = this.CheckInt(ValuesFromFile.vals[i], false, null, null, ref ret);
                                        if (!ret.result)
                                        {

                                            ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + " Тип услуги (тарифная зона: 1 –дневное потребление, 2 –ночное потребление)");
                                        }
                                        ValuesFromFile.sql += typeService + ", ";
                                        i++;
                                        #endregion 5. Тип услуги (тарифная зона: 1 –дневное потребление, 2 –ночное потребление)

                                        #region 6. Тип счетчика
                                        string typeCounter = this.CheckText(ValuesFromFile.vals[i], true, 25, ref ret);
                                        if (!ret.result)
                                        {
                                            ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "Тип счетчика ");

                                        }
                                        ValuesFromFile.sql += typeCounter + ", ";
                                        i++;
                                        #endregion 6. Тип счетчика

                                        #region 7. Разрядность прибора
                                        string capCounter = this.CheckInt(ValuesFromFile.vals[i], true, null, null, ref ret);
                                        if (!ret.result)
                                        {
                                            ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "Разрядность прибора ");

                                        }
                                        ValuesFromFile.sql += capCounter + ", ";
                                        i++;
                                        #endregion 7. Разрядность прибора

                                        #region 8. Повышающий коэффициент (коэффициент трансформации тока)
                                        string upIndex = this.CheckInt(ValuesFromFile.vals[i], true, null, null, ref ret);
                                        if (!ret.result)
                                        {
                                            ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "Повышающий коэффициент (коэффициент трансформации тока) ");

                                        }
                                        ValuesFromFile.sql += upIndex + ", ";
                                        i++;
                                        #endregion 8. Повышающий коэффициент (коэффициент трансформации тока)

                                        #region 9. Заводской номер прибора учета
                                        string numCounter = this.CheckText(ValuesFromFile.vals[i], true, 20, ref ret);
                                        if (!ret.result)
                                        {
                                            ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "Заводской номер прибора учета");

                                        }
                                        ValuesFromFile.sql += numCounter + ", ";
                                        i++;
                                        #endregion 9. Заводской номер прибора учета

                                        // Расходы заносятся прямо здесь 

                                        #region 10. Дата показания прибора учета / Месяц показания
                                        string dateCounter = this.CheckDateTime(ValuesFromFile.vals[i], true, ref ret);
                                        if (!ret.result)
                                        {
                                            ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + ". Дата показания прибора учета / Месяц показания");

                                        }
                                        ValuesFromFile.sql += dateCounter + ", ";
                                        i++;
                                        #endregion 10. Дата показания прибора учета / Месяц показания

                                        #region 11. Показание прибора учета / Месячный расход
                                        string dateCounerMonthExp = this.CheckDecimal(ValuesFromFile.vals[i], true, false, null, null, ref ret);
                                        if (!ret.result)
                                        {
                                            ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "Показание прибора учета / Месячный расход");

                                        }
                                        ValuesFromFile.sql += dateCounerMonthExp + ", ";
                                        i++;
                                        #endregion 11. Показание прибора учета / Месячный расход

                                        #region 12. Код единицы измерения расхода (из справочника)
                                        string upIndexExp = this.CheckInt(ValuesFromFile.vals[i], true, 1, null, ref ret);
                                        if (!ret.result)
                                        {
                                            ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "Код единицы измерения расхода (из справочника) ");

                                        }
                                        ValuesFromFile.sql += upIndexExp + ", ";
                                        i++;
                                        #endregion 12. Код единицы измерения расхода (из справочника)

                                        #region 13. Дата поверки
                                        string dateCheck = this.CheckDateTime(ValuesFromFile.vals[i], false, ref ret);
                                        if (!ret.result)
                                        {
                                            ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "Дата поверки");

                                        }
                                        ValuesFromFile.sql += dateCheck + ", ";
                                        i++;
                                        #endregion 13. Дата поверки

                                        #region 14. Дата следующей поверки
                                        string dateCheckNext = this.CheckDateTime(ValuesFromFile.vals[i], false, ref ret);
                                        if (!ret.result)
                                        {
                                            ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "Дата следующей поверки");

                                        }
                                        ValuesFromFile.sql += dateCheckNext + ", ";
                                        i++;
                                        #endregion 14. Дата следующей поверки

                                        #region Завершить формирование insert
                                        //nzp_dom, nzp_counter
                                        ValuesFromFile.sql += "null,null);";
                                        #endregion Завершить формирование insert

                                        #endregion Версия 1.0
        }
        else
        {
            #region Версия 1.2 (Здесь только счетчики ОДПУ без показаний )

            if (ValuesFromFile.vals.Length < 13)
            {
                ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + " Неправильный формат файла загрузки: Информация об общедомовых приборах учета");
                return ;
            }
            #region Заготовка инсерта
                                            ValuesFromFile.sql = 
                                                " INSERT INTO  " + Points.Pref + "_data" + tableDelimiter + "file_odpu( nzp_file, dom_id,local_id, nzp_serv, serv_type,  counter_type, cnt_stage,  "+
                                                "mmnog, num_cnt, nzp_measure, dat_prov, dat_provnext, doppar) "+
                                                " VALUES(";
                                            #endregion Заготовка инсерта

                                            #region 1. Тип строки пропускаем
                                            int i = 1;
                                            //nzp_file
                                            ValuesFromFile.sql += ValuesFromFile.finder.nzp_file + ", ";
                                            #endregion 1. Тип строки пропускаем

                                            #region  2. Уникальный код дома для строки типа 2 – реквизит 3.
                                            //2. Уникальный код дома для строки типа 2 – реквизит 3.
                                            //string codeDom = this.CheckInt(vals[i], true, 1, null, ref ret);
                                            string codeDom = this.CheckInt(ValuesFromFile.vals[i].ToUpper().Replace("/", "00").Replace("А", "01").Replace("Б", "02").Replace("В", "03").Replace("Г", "04").Replace("D", "001").Replace("-", "002"), true, null, null, ref ret);
                                            if (!ret.result)
                                            {
                                                ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + " Уникальный код дома для строки типа 2 – реквизит 3.");

                                            }
                                            ValuesFromFile.sql += codeDom + ", ";
                                            i++;
                                            #endregion  Уникальный код дома для строки типа 2 – реквизит 3.

                                            #region 3. Уникальный код прибора в системе поставщика ОДПУ.
                                            //3. Уникальный код прибора ОДПУ.
                                            string codeOdpu = this.CheckText(ValuesFromFile.vals[i], true, 20, ref ret);
                                            if (!ret.result)
                                            {
                                                ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + " Уникальный код прибора учета в системе поставщика ");
                                            }
                                            ValuesFromFile.sql += codeOdpu + ", ";
                                            i++;
                                            #endregion 3. Уникальный код прибора ОДПУ.

                                            #region 4. Код услуги (из справочника)
                                            //4. Код услуги (из справочника)
                                            string codeServ = this.CheckInt(ValuesFromFile.vals[i], true, 1, null, ref ret);
                                            if (!ret.result)
                                            {
                                                ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "  Код услуги (из справочника)");

                                            }
                                            ValuesFromFile.sql += codeServ + ", ";
                                            i++;
                                            #endregion 4. Код услуги (из справочника)

                                            #region 5. Тип услуги (тарифная зона: 1 –дневное потребление, 2 –ночное потребление)
                                            //5. Тип услуги (тарифная зона: 1 –дневное потребление, 2 –ночное потребление)
                                            string typeService = this.CheckInt(ValuesFromFile.vals[i], true, null, null, ref ret);
                                            if (!ret.result)
                                            {
                                                ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + " Тип услуги (тарифная зона: 1 –дневное потребление, 2 –ночное потребление)");

                                            }
                                            ValuesFromFile.sql += typeService + ", ";
                                            i++;
                                            #endregion 5. Тип услуги (тарифная зона: 1 –дневное потребление, 2 –ночное потребление)

                                            #region 6. Тип счетчика
                                            //6. Тип счетчика 
                                            string typeCounter = this.CheckText(ValuesFromFile.vals[i], true, 25, ref ret);
                                            if (!ret.result)
                                            {
                                                ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "Тип счетчика ");

                                            }
                                            ValuesFromFile.sql += typeCounter + ", ";
                                            i++;
                                            #endregion 6. Тип счетчика

                                            #region 7. Разрядность прибора
                                            //7. Разрядность прибора 
                                            string capCounter = this.CheckInt(ValuesFromFile.vals[i], true, null, null, ref ret);
                                            if (!ret.result)
                                            {
                                                ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "Разрядность прибора ");
                                            }
                                            ValuesFromFile.sql += capCounter + ", ";
                                            i++;
                                            #endregion 7. Разрядность прибора

                                            #region 8. Повышающий коэффициент (коэффициент трансформации тока)
                                            //8. Повышающий коэффициент (коэффициент трансформации тока)
                                            string upIndex = this.CheckInt(ValuesFromFile.vals[i], true, null, null, ref ret);
                                            if (!ret.result)
                                            {
                                                ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "Повышающий коэффициент (коэффициент трансформации тока) ");

                                            }
                                            ValuesFromFile.sql += upIndex + ", ";
                                            i++;
                                            #endregion 8. Повышающий коэффициент (коэффициент трансформации тока)

                                            #region 9. Заводской номер прибора учета
                                            //9. Заводской номер прибора учета
                                            string numCounter = this.CheckText(ValuesFromFile.vals[i], true, 20, ref ret);
                                            if (!ret.result)
                                            {
                                                ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "Заводской номер прибора учета");

                                            }
                                            ValuesFromFile.sql += numCounter + ", ";
                                            i++;
                                            #endregion 9. Заводской номер прибора учета

                                            #region 10. Код единицы измерения расхода (из справочника)
                                            //12. Код единицы измерения расхода (из справочника)
                                            string upIndexExp = this.CheckInt(ValuesFromFile.vals[i], true, 1, null, ref ret);
                                            if (!ret.result)
                                            {
                                                ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "Код единицы измерения расхода (из справочника) ");

                                            }
                                            ValuesFromFile.sql += upIndexExp + ", ";
                                            i++;

                                            #endregion 10. Код единицы измерения расхода (из справочника)

                                            #region 11. Дата поверки
                                            //13. Дата поверки
                                            string dateCheck = this.CheckDateTime(ValuesFromFile.vals[i], false, ref ret);
                                            if (!ret.result)
                                            {
                                                ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "Дата поверки");

                                            }
                                            ValuesFromFile.sql += dateCheck + ", ";
                                            i++;
                                            #endregion 11. Дата поверки

                                            #region 12. Дата следующей поверки
                                            //14. Дата следующей поверки
                                            string dateCheckNext = this.CheckDateTime(ValuesFromFile.vals[i], false, ref ret);
                                            if (!ret.result)
                                            {
                                                ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "Дата следующей поверки");

                                            }
                                            ValuesFromFile.sql += dateCheckNext + ", ";
                                            i++;
                                            #endregion 12. Дата следующей поверки

                                            #region 13. Дополнительные характеристики (временно в загрузке не участвуют , только в разборе строки )
                                            //13. Дополнительные характеристики
                                            string DopCharact = this.CheckText(ValuesFromFile.vals[i], false, 250, ref ret);
                                            if (!ret.result)
                                            {
                                                ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "Дополнительные характеристики ОДПУ");

                                            }
                                            #region временно не загружаем
                                            ValuesFromFile.sql += DopCharact + " ";
                                            i++;
                                            #endregion временно не загружаем
                                            #endregion 13. Дополнительные характеристики

                                            #region Завершающие действия для инсерта в спец таблицу
                                            //nzp_dom, nzp_counter
                                            ValuesFromFile.sql += ");";
                                            #endregion Завершающие действия для инсерта в спец таблицу                                        

                                        #endregion Версия 1.2
        }
    }


    /// <summary>
    ///  Загрузка 10 секции "Показания общедомовых приборов учета"
    /// </summary>
    /// <param name="ValuesFromFile"></param>
    public void AddFileOdpu_p_Section10(DBValuesFromFile ValuesFromFile)
    {
        Returns ret = new Returns();

        if (!ValuesFromFile.finder.sections[10])
        { return; }

        if (ValuesFromFile.Pvers == "1.0")
        {
            #region Версия 1.0 (Здесь ИПУ с показаниями )

            #region Заготовка инсерта
            ValuesFromFile.sql =
                " INSERT INTO  " + Points.Pref + "_data" + tableDelimiter + "file_ipu( nzp_file, ls_id, nzp_serv, rashod_type, serv_type, counter_type, cnt_stage, mmnog, num_cnt, dat_uchet,  " +
                " val_cnt, nzp_measure, dat_prov, dat_provnext, nzp_kvar, nzp_counter) " +
                " VALUES(";

            #endregion Заготовка инсерта

            #region 1. Тип строки пропускаем
            int i = 1;

            //nzp_file
            ValuesFromFile.sql += ValuesFromFile.finder.nzp_file + ", ";

            #endregion 1. Тип строки пропускаем

            #region 2. № ЛС в системе поставщика
            string ls = this.CheckText(ValuesFromFile.vals[i], true, 20, ref ret);
            if (!ret.result)
            {
                ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + " № ЛС в системе поставщика");

            }
            ValuesFromFile.sql += ls + ", ";
            i++;
            #endregion 2. № ЛС в системе поставщика

            #region 3. Код услуги (из справочника)
            string codeServ = this.CheckInt(ValuesFromFile.vals[i], true, 1, null, ref ret);
            if (!ret.result)
            {
                ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "  Код услуги (из справочника)");

            }
            ValuesFromFile.sql += codeServ + ", ";
            i++;
            #endregion 3. Код услуги (из справочника)

            #region 4. Тип расхода (1 – при выгрузке показаний, 2 – при выгрузке месячного расхода)
            string typeExpenditure = this.CheckInt(ValuesFromFile.vals[i], true, null, null, ref ret);
            if (!ret.result)
            {
                ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "Тип расхода (1 – при выгрузке показаний, 2 – при выгрузке месячного расхода)");

            }
            ValuesFromFile.sql += typeExpenditure + ", ";
            i++;
            #endregion 4. Тип расхода (1 – при выгрузке показаний, 2 – при выгрузке месячного расхода)

            #region 5. Тип услуги (тарифная зона: 1 –дневное потребление, 2 –ночное потребление)
            string typeService = this.CheckInt(ValuesFromFile.vals[i], true, null, null, ref ret);
            if (!ret.result)
            {
                ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + " Тип услуги (тарифная зона: 1 –дневное потребление, 2 –ночное потребление)");

            }
            ValuesFromFile.sql += typeService + ", ";
            i++;
            #endregion 5. Тип услуги (тарифная зона: 1 –дневное потребление, 2 –ночное потребление)

            #region 6. Тип счетчика
            string typeCounter = this.CheckText(ValuesFromFile.vals[i], true, 25, ref ret);
            if (!ret.result)
            {
                ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "Тип счетчика ");

            }
            ValuesFromFile.sql += typeCounter + ", ";
            i++;
            #endregion 6. Тип счетчика

            #region 7. Разрядность прибора
            string capCounter = this.CheckInt(ValuesFromFile.vals[i], true, null, null, ref ret);
            if (!ret.result)
            {
                ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "Разрядность прибора ");

            }
            ValuesFromFile.sql += capCounter + ", ";
            i++;
            #endregion 7. Разрядность прибора

            #region 8. Повышающий коэффициент (коэффициент трансформации тока)
            string upIndex = this.CheckInt(ValuesFromFile.vals[i], true, null, null, ref ret);
            if (!ret.result)
            {
                ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "Повышающий коэффициент (коэффициент трансформации тока) ");

            }
            ValuesFromFile.sql += upIndex + ", ";
            i++;
            #endregion 8. Повышающий коэффициент (коэффициент трансформации тока)

            #region 9. Заводской номер прибора учета
            string numCounter = this.CheckText(ValuesFromFile.vals[i], true, 20, ref ret);
            if (!ret.result)
            {
                ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "Заводской номер прибора учета");

            }
            ValuesFromFile.sql += numCounter + ", ";
            i++;
            #endregion 9. Заводской номер прибора учета

            #region 10. Дата показания прибора учета / Месяц показания
            string dateCounter = this.CheckDateTime(ValuesFromFile.vals[i], true, ref ret);
            if (!ret.result)
            {
                ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + ". Дата показания прибора учета / Месяц показания");

            }
            ValuesFromFile.sql += dateCounter + ", ";
            i++;
            #endregion 10. Дата показания прибора учета / Месяц показания

            #region 11. Показание прибора учета / Месячный расход
            string dateCounerMonthExp = this.CheckDecimal(ValuesFromFile.vals[i], true, false, null, null, ref ret);
            if (!ret.result)
            {
                ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "Показание прибора учета / Месячный расход");

            }
            ValuesFromFile.sql += dateCounerMonthExp + ", ";
            i++;
            #endregion 11. Показание прибора учета / Месячный расход

            #region 12. Код единицы измерения расхода (из справочника)
            string upIndexExp = this.CheckInt(ValuesFromFile.vals[i], true, 1, null, ref ret);
            if (!ret.result)
            {
                ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "Код единицы измерения расхода (из справочника) ");

            }
            ValuesFromFile.sql += upIndexExp + ", ";
            i++;
            #endregion 12. Код единицы измерения расхода (из справочника)

            #region 13. Дата поверки
            string dateCheck = this.CheckDateTime(ValuesFromFile.vals[i], false, ref ret);
            if (!ret.result)
            {
                ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "Дата поверки");

            }
            ValuesFromFile.sql += dateCheck + ", ";
            i++;
            #endregion 13. Дата поверки

            #region 14. Дата следующей поверки
            string dateCheckNext = this.CheckDateTime(ValuesFromFile.vals[i], false, ref ret);
            if (!ret.result)
            {
                ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "Дата следующей поверки");

            }
            ValuesFromFile.sql += dateCheckNext + ", ";
            i++;
            #endregion 14. Дата следующей поверки

            #region завершение инсерта
            //nzp_kvar, nzp_counter
            ValuesFromFile.sql += "null,null);";
            #endregion завершение инсерта

            #endregion Версия 1.0
        }
        else
        {
            #region Версия 1.2 (здесь только  показания ОДПУ)
            if (ValuesFromFile.vals.Length < 5)
            {
                ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "Неправильный формат файла загрузки: Информация об индивидуальных приборах учета");
                return;
            }

            #region Заготовка инсерта
            ValuesFromFile.sql =
                " INSERT INTO  " + Points.Pref + "_data" + tableDelimiter + "file_odpu_p(nzp_file , id_odpu  , rashod_type ,  dat_uchet ,   val_cnt  ) " +
                " VALUES(";
            #endregion Заготовка инсерта

            #region 1. Тип строки пропускаем
            int i = 1;

            //nzp_file
            ValuesFromFile.sql += ValuesFromFile.finder.nzp_file + ", ";

            #endregion 1. Тип строки пропускаем

            #region 2. Уникальный код счетчика одпу
            string local_id = this.CheckText(ValuesFromFile.vals[i], true, 20, ref ret);
            if (!ret.result)
            {
                ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "Уникальный код счетчика");
            }

            ValuesFromFile.sql += local_id + ", ";
            i++;
            #endregion 2. Уникальный код счетчика одпу

            #region 3. Тип расхода (1 – при выгрузке показаний, 2 – при выгрузке месячного расхода)
            string typeExpenditure = this.CheckInt(ValuesFromFile.vals[i], true, null, null, ref ret);
            if (!ret.result)
            {
                ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "Тип расхода (1 – при выгрузке показаний, 2 – при выгрузке месячного расхода)");

            }
            ValuesFromFile.sql += typeExpenditure + ", ";
            i++;
            #endregion 3. Тип расхода (1 – при выгрузке показаний, 2 – при выгрузке месячного расхода)

            #region 4. Дата показания прибора учета / Месяц показания
            string dateCounter = this.CheckDateTime(ValuesFromFile.vals[i], true, ref ret);
            if (!ret.result)
            {
                ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + ". Дата показания прибора учета / Месяц показания");

            }
            ValuesFromFile.sql += dateCounter + ", ";
            i++;
            #endregion 4. Дата показания прибора учета / Месяц показания

            #region 5. Показание прибора учета / Месячный расход
            string dateCounerMonthExp = this.CheckDecimal(ValuesFromFile.vals[i], true, false, null, null, ref ret);
            if (!ret.result)
            {
                ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "Показание прибора учета / Месячный расход");

            }
            ValuesFromFile.sql += dateCounerMonthExp + " ";
            i++;
            #endregion 5. Показание прибора учета / Месячный расход

            //nzp_dom, nzp_counter
            ValuesFromFile.sql += ");";
            #endregion Версия 1.2 (здесь показания ОДПУ)
        }
    }


    /// <summary>
    ///  Загрузка 11 секции "Информация об индивидуальных приборах учета"
    /// </summary>
    /// <param name="ValuesFromFile"></param>
    public void AddFileIpu_Section11(DBValuesFromFile ValuesFromFile)
    {
        Returns ret = new Returns();

        if (!ValuesFromFile.finder.sections[11])
        { return; }

        if (ValuesFromFile.Pvers != "1.0")
        {
            #region Версия 1.2 (Здесь счетчики ИПУ без показаний , показания в 12 секции)

            if (ValuesFromFile.vals.Length < 13)
            {
                ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + " Неправильный формат файла загрузки: показания ИПУ");
                return;
            }
            #region Заготовка инсерта
            ValuesFromFile.sql =
                " INSERT INTO  " + Points.Pref + "_data" + tableDelimiter + "file_ipu(nzp_file, ls_id, local_id,kod_serv, serv_type, counter_type, cnt_stage, mmnog, num_cnt,   " +
                " nzp_measure, dat_prov, dat_provnext,doppar) " +
                " VALUES(";
            #endregion Заготовка инсерта

            #region 1. Тип строки пропускаем
            int i = 1;

            //nzp_file
            ValuesFromFile.sql += ValuesFromFile.finder.nzp_file + ", ";

            #endregion 1. Тип строки пропускаем

            #region 2. № ЛС в системе поставщика
            string ls = this.CheckText(DeleteFirstZeros(ValuesFromFile.vals[i]), true, 20, ref ret);
            if (!ret.result)
            {
                ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + " № ЛС в системе поставщика");

            }
            ValuesFromFile.sql += ls + ", ";
            i++;
            #endregion 2. № ЛС в системе поставщика

            #region 3.Код прибора учета в системе поставщика
            string local_id = this.CheckInt(ValuesFromFile.vals[i], true, 1, null, ref ret);

            if (!ret.result)
            {
                local_id = this.CheckText(ValuesFromFile.vals[i], true, 20, ref ret);
                if (!ret.result)
                {
                    ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + " № Индивидуального прибора учета в системе поставщика");
                }
            }
            ValuesFromFile.sql += local_id + ", ";
            i++;

            #endregion 3.Код прибора учета в системе поставщика

            #region 3. Код услуги (из справочника)
            string codeServ = this.CheckInt(ValuesFromFile.vals[i], true, 1, null, ref ret);
            if (!ret.result)
            {
                ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "  Код услуги (из справочника)");

            }
            ValuesFromFile.sql += codeServ + ", ";
            i++;
            #endregion 3. Код услуги (из справочника)

            #region 4. Тип услуги (тарифная зона: 1 –дневное потребление, 2 –ночное потребление)
            //string typeService = this.CheckInt(vals[i], true, null, null, ref ret);
            string typeService = this.CheckInt(ValuesFromFile.vals[i], false, null, null, ref ret);
            if (!ret.result)
            {
                ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + " Тип услуги (тарифная зона: 1 –дневное потребление, 2 –ночное потребление)");

            }
            ValuesFromFile.sql += typeService + ", ";
            i++;
            #endregion 4. Тип услуги (тарифная зона: 1 –дневное потребление, 2 –ночное потребление)

            #region 6. Тип счетчика
            string typeCounter = this.CheckText(ValuesFromFile.vals[i], true, 25, ref ret);
            if (!ret.result)
            {
                ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "Тип счетчика ");

            }
            ValuesFromFile.sql += typeCounter + ", ";
            i++;
            #endregion 6. Тип счетчика

            #region 7. Разрядность прибора
            string capCounter = this.CheckInt(ValuesFromFile.vals[i], true, null, null, ref ret);
            if (!ret.result)
            {
                ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "Разрядность прибора ");

            }
            ValuesFromFile.sql += capCounter + ", ";
            i++;
            #endregion 7. Разрядность прибора

            #region 8. Повышающий коэффициент (коэффициент трансформации тока)
            string upIndex = this.CheckInt(ValuesFromFile.vals[i], true, null, null, ref ret);
            if (!ret.result)
            {
                ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "Повышающий коэффициент (коэффициент трансформации тока) ");

            }
            ValuesFromFile.sql += upIndex + ", ";
            i++;
            #endregion 8. Повышающий коэффициент (коэффициент трансформации тока)

            #region 9. Заводской номер прибора учета
            string numCounter = this.CheckText(ValuesFromFile.vals[i], true, 20, ref ret);
            if (!ret.result)
            {
                ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "Заводской номер прибора учета");

            }
            ValuesFromFile.sql += numCounter + ", ";
            i++;
            #endregion 9. Заводской номер прибора учета

            #region 10. Код единицы измерения расхода (из справочника)
            string upIndexExp = this.CheckInt(ValuesFromFile.vals[i], true, 1, null, ref ret);
            if (!ret.result)
            {
                ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "Код единицы измерения расхода (из справочника) ");

            }
            ValuesFromFile.sql += upIndexExp + ", ";
            i++;
            #endregion 10. Код единицы измерения расхода (из справочника)

            #region 11. Дата поверки
            string dateCheck = this.CheckDateTime(ValuesFromFile.vals[i], false, ref ret);
            if (!ret.result)
            {
                ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "Дата поверки");

            }
            ValuesFromFile.sql += dateCheck + ", ";
            i++;
            #endregion 11. Дата поверки

            #region 12. Дата следующей поверки
            string dateCheckNext = this.CheckDateTime(ValuesFromFile.vals[i], false, ref ret);
            if (!ret.result)
            {
                ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "Дата следующей поверки");

            }
            ValuesFromFile.sql += dateCheckNext + ", ";
            i++;
            #endregion 12. Дата следующей поверки

            #region 13. Доп параметры ИПУ
            string DopParIpu = this.CheckText(ValuesFromFile.vals[i], false, 250, ref ret);
            if (!ret.result)
            {
                ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "13. Доп параметры ИПУ");

            }
            #region Пока не используем но проверяем наличие
            if (DopParIpu.Length == 0) { DopParIpu = "null"; };
            ValuesFromFile.sql += DopParIpu + " ";
            i++;
            #endregion Пока не используем но проверяем наличие
            #endregion 13. Доп параметры ИПУ

            #region завершение инсерта
            //nzp_kvar, nzp_counter
            ValuesFromFile.sql += ");";
            #endregion завершение инсерта


            #endregion Версия 1.2
        }
    }


    /// <summary>
    ///  Загрузка 12 секции "Показания индивидуальных приборов учета"
    /// </summary>
    /// <param name="ValuesFromFile"></param>
    public void AddFileIpu_p_Section12(DBValuesFromFile ValuesFromFile)
    {
        Returns ret = new Returns();

        if (!ValuesFromFile.finder.sections[12])
        { return; }

        if (ValuesFromFile.Pvers != "1.0")
        {
            #region Версия 1.2 (Здесь Показания ИПУ )

            if (ValuesFromFile.vals.Length < 6)
            {
                ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + " Неправильный формат файла загрузки: показания ИПУ");
                return;
            }
                #region Заготовка инсерта
                ValuesFromFile.sql = " INSERT INTO  " + Points.Pref + "_data" + tableDelimiter + "file_ipu_p( nzp_file , id_ipu,  rashod_type ,  dat_uchet ,   val_cnt, kod_serv) " +
                    " VALUES(";
                #endregion Заготовка инсерта

                #region 1. Тип строки пропускаем
                int i = 1;

                //nzp_file
                ValuesFromFile.sql += ValuesFromFile.finder.nzp_file + ", ";

                #endregion 1. Тип строки пропускаем

                #region 2. Уникальный код счетчика ипу
                string local_id = this.CheckText(ValuesFromFile.vals[i], true, 20, ref ret);
                if (!ret.result)
                {
                    ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "Уникальный код счетчика ИПУ");
                }
                ValuesFromFile.sql += local_id + ", ";
                i++;

                #endregion 2. Уникальный код счетчика одпу

                #region 3. Тип расхода (1 – при выгрузке показаний, 2 – при выгрузке месячного расхода)
                string typeExpenditure = this.CheckInt(ValuesFromFile.vals[i], true, null, null, ref ret);
                if (!ret.result)
                {
                    ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "Тип расхода (1 – при выгрузке показаний, 2 – при выгрузке месячного расхода)");

                }
                ValuesFromFile.sql += typeExpenditure + ", ";
                i++;
                #endregion 3. Тип расхода (1 – при выгрузке показаний, 2 – при выгрузке месячного расхода)

                #region 4. Дата показания прибора учета / Месяц показания
                string dateCounter = this.CheckDateTime(ValuesFromFile.vals[i], true, ref ret);
                if (!ret.result)
                {
                    ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + ". Дата показания прибора учета / Месяц показания");

                }
                ValuesFromFile.sql += dateCounter + ", ";
                i++;
                #endregion 4. Дата показания прибора учета / Месяц показания

                #region 5. Показание прибора учета / Месячный расход
                string dateCounerMonthExp = this.CheckDecimal(ValuesFromFile.vals[i], true, false, null, null, ref ret);
                if (!ret.result)
                {
                    ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "Показание прибора учета / Месячный расход");
                }
                ValuesFromFile.sql += dateCounerMonthExp + ", ";
                i++;
                #endregion 5. Показание прибора учета / Месячный расход

                #region 6. Код услуги
                string kod_serv = this.CheckDecimal(ValuesFromFile.vals[i], true, false, null, null, ref ret);
                if (!ret.result)
                {
                    ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "Код услуги");

                }
                ValuesFromFile.sql += kod_serv + " ";
                i++;
                #endregion 6. Код услуги

                #region завершение инсерта
                ValuesFromFile.sql += ");";
                #endregion завершение инсерта

            #endregion Версия 1.2
        }
    }


    /// <summary>
    ///  Загрузка 13 секции "Перечень выгруженных услуг"
    /// </summary>
    /// <param name="ValuesFromFile"></param>
    public void AddFileServices_Section13(DBValuesFromFile ValuesFromFile)
    {
        Returns ret = new Returns();
        if (!ValuesFromFile.finder.sections[13])
        {return;}
        //загружаем ли 13 секцию? если нет, то берем из собственных таблиц
        ValuesFromFile.load13section = true;

        if (ValuesFromFile.Pvers != "1.0")
        {
            #region Версия 1.2 (Перечень выгруженных услуг )

                if (ValuesFromFile.vals.Length < 5)
                {
                    ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + " Неправильный формат файла загрузки: Перечень выгруженных услуг");
                    return ;
                }

                    #region Заготовка инсерта
                    ValuesFromFile.sql = " INSERT INTO  " + Points.Pref + "_data" + tableDelimiter + "file_services( nzp_file, id_serv, service, service2, type_serv)" +
                        " VALUES(";

                    #endregion Заготовка инсерта

                    #region 1. Тип строки пропускаем
                    int i = 1;

                    //nzp_file
                    ValuesFromFile.sql += ValuesFromFile.finder.nzp_file + ", ";

                    #endregion 1. Тип строки пропускаем

                    #region 2. Уникальный код услуги в системе поставщика информации
                    string local_id = this.CheckInt(ValuesFromFile.vals[i], true, null, null, ref ret);
                    if (!ret.result)
                    {
                        ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "Уникальный код услуги в системе поставщика информации");
                    }
                    ValuesFromFile.sql += local_id + ", ";
                    i++;
                    #endregion 2. Уникальный код услуги в системе поставщика информации

                    #region 3. Наименование услуги
                    string serv_name = this.CheckText(ValuesFromFile.vals[i], true, 60, ref ret);
                    if (!ret.result)
                    {
                        ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + " Наименование услуги ");

                    }
                    ValuesFromFile.sql += serv_name + ", ";
                    i++;
                    #endregion 3.  Наименование услуги

                    #region 4. Краткое наименование услуги
                    string serv_name_short = this.CheckText(ValuesFromFile.vals[i], false, 60, ref ret);
                    if (!ret.result)
                    {
                        ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + " Краткое наименование услуги ");

                    }
                    ValuesFromFile.sql += serv_name_short + ", ";
                    i++;
                    #endregion 4.  Краткое наименование услуги

                    #region 5. Тип услуги (1 - коммунальная, 2 - жилищная, 0 - не определено)
                    string type_serv = this.CheckInt(ValuesFromFile.vals[i], false, null, null, ref ret);
                    if (!ret.result)
                    {
                        ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "Тип услуги (1 - коммунальная, 2 - жилищная, 0 - не определено)");

                    }
                    ValuesFromFile.sql += type_serv + ");";
                    #endregion 5. Тип услуги (1 - коммунальная, 2 - жилищная, 0 - не определено)               

                #endregion Версия 1.2
        }
    }



    /// <summary>
    ///  Загрузка 14 секции "Перечень выгруженных муниципальных образований "
    /// </summary>
    /// <param name="ValuesFromFile"></param>
    public void AddFileMo_Section14(DBValuesFromFile ValuesFromFile)
    {
        Returns ret = new Returns();

        if (!ValuesFromFile.finder.sections[14])
        { return; }
        
        if (ValuesFromFile.Pvers != "1.0")
        {
            #region Версия 1.2 (Перечень выгруженных муниципальных образований )

                if (ValuesFromFile.vals.Length < 5)
                {
                    ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + " Неправильный формат файла загрузки: перечень выгруженных муниципальных образований");
                    return;
                }
                
                    #region Заготовка инсерта
                    ValuesFromFile.sql =
                        " INSERT INTO  " + Points.Pref + "_data" + tableDelimiter + "file_mo(nzp_file, id_mo, mo_name, raj, nzp_raj)" +
                        "VALUES(";
                    #endregion Заготовка инсерта

                    #region 1. Тип строки пропускаем
                    int i = 1;

                    //nzp_file
                    ValuesFromFile.sql += ValuesFromFile.finder.nzp_file + ", ";

                    #endregion 1. Тип строки пропускаем

                    #region 2. Уникальный код муниципального образования (МО) в системе поставщика информации
                    string id_mo = this.CheckInt(ValuesFromFile.vals[i], true, null, null, ref ret);
                    if (!ret.result)
                    {
                        ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "Уникальный код муниципального образования (МО) в системе поставщика информации");
                    }
                    ValuesFromFile.sql += id_mo + ", ";
                    i++;
                    #endregion 2. Уникальный код муниципального образования (МО) в системе поставщика информации

                    #region 3. Наименование МО
                    string mo_name = this.CheckText(ValuesFromFile.vals[i], true, 60, ref ret);
                    if (!ret.result)
                    {
                        ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + " Наименование МО ");

                    }
                    ValuesFromFile.sql += mo_name + ", ";
                    i++;
                    #endregion 3.  Наименование МО

                    #region 4. Район
                    string raj = this.CheckText(ValuesFromFile.vals[i], true, 60, ref ret);
                    if (!ret.result)
                    {
                        ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + " Район ");

                    }
                    ValuesFromFile.sql += raj + ", ";
                    i++;
                    #endregion 4.  Район

                    #region 5. Уникальный код района
                    string nzp_raj = this.CheckInt(ValuesFromFile.vals[i], false, null, null, ref ret);
                    if (!ret.result)
                    {
                        ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "Уникальный код района");
                    }
                    ValuesFromFile.sql += nzp_raj + "); ";
                    #endregion 5. Уникальный код района
                

                #endregion Версия 1.2
        }
    }


    /// <summary>
    ///  Загрузка 15 секции "Информация по проживающим "
    /// </summary>
    /// <param name="ValuesFromFile"></param>
    public void AddFileGilec_Section15(DBValuesFromFile ValuesFromFile)
    {
        Returns ret = new Returns();

        if (!ValuesFromFile.finder.sections[15])
        {return;}

        if (ValuesFromFile.Pvers != "1.0")
        {
            #region Версия 1.2 (Информация о проживающем)

                if (ValuesFromFile.vals.Length < 51)
                {
                    ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + " Неправильный формат файла загрузки: информация о проживающем");
                    return;
                }
                    #region Заготовка инсерта
                ValuesFromFile.sql = 
                    " INSERT INTO  " + Points.Pref + "_data" + tableDelimiter + "file_gilec (nzp_file, num_ls, nzp_gil, nzp_kart, nzp_tkrt, fam,"+
                    " ima, otch, dat_rog, fam_c, ima_c, otch_c, dat_rog_c, gender, nzp_dok, serij, nomer, vid_dat, vid_mes, kod_podrazd," +
                    " strana_mr, region_mr, okrug_mr, gorod_mr, npunkt_mr, strana_op, region_op, okrug_op, gorod_op, npunkt_op," +
                    " rem_op, strana_ku, region_ku, okrug_ku, gorod_ku, npunkt_ku, rem_ku, rem_p, tprp, dat_prop, dat_oprp, dat_pvu, " +
                    "who_pvu, dat_svu, namereg, kod_namereg, rod, nzp_celp, nzp_celu, dat_sost, dat_ofor)"+
                    " VALUES(";
                    #endregion Заготовка инсерта

                    #region 1. Тип строки пропускаем
                    int i = 1;

                    //nzp_file
                    ValuesFromFile.sql += ValuesFromFile.finder.nzp_file + ", ";

                    #endregion 1. Тип строки пропускаем

                    #region 2. Уникальный номер лицевого счета
                    string num_ls = this.CheckInt(ValuesFromFile.vals[i], true, 1, null, ref ret).ToString();
                    if (!ret.result)
                    {
                        num_ls = this.CheckText(ValuesFromFile.vals[i], true, 20, ref ret);
                        if (!ret.result)
                        {
                            ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + " Уникальный номер лицевого счета ");
                        }
                    }
                    ValuesFromFile.sql += num_ls + ", ";
                    i++;
                    #endregion 2.  Уникальный номер лицевого счета

                    #region 3. Уникальный номер гражданина
                    string nzp_gil = this.CheckInt(ValuesFromFile.vals[i], true, null, null, ref ret);
                    if (!ret.result)
                    {
                        ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "Уникальный номер гражданина");
                    }
                    ValuesFromFile.sql += nzp_gil + ", ";
                    i++;
                    #endregion 3. Уникальный номер гражданина

                    #region 4. Уникальный номер адресного листка прибытия/убытия гражданина
                    string nzp_kart = this.CheckInt(ValuesFromFile.vals[i], true, null, null, ref ret);
                    if (!ret.result)
                    {
                        ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "Уникальный номер адресного листка прибытия/убытия гражданина");
                    }
                    ValuesFromFile.sql += nzp_kart + ", ";
                    i++;
                    #endregion 4. Уникальный номер адресного листка прибытия/убытия гражданина

                    #region 5. Тип адресного листка (1 - прибытие, 2 - убытие)
                    string nzp_tkrt = this.CheckInt(ValuesFromFile.vals[i], true, 1, 2, ref ret);
                    if (!ret.result)
                    {
                        ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "Тип адресного листка");
                    }
                    ValuesFromFile.sql += nzp_tkrt + ", ";
                    i++;
                    #endregion 5. Тип адресного листка

                    #region 6. Фамилия
                    string fam = this.CheckText(ValuesFromFile.vals[i], true, 40, ref ret);
                    if (!ret.result)
                    {
                        ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + " Фамилия  ");
                    }
                    ValuesFromFile.sql += fam + ", ";
                    i++;
                    #endregion 6. Фамилия

                    #region 7. Имя
                    string ima = this.CheckText(ValuesFromFile.vals[i], true, 40, ref ret);
                    if (!ret.result)
                    {
                        ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + " Имя ");
                    }
                    ValuesFromFile.sql += ima + ", ";
                    i++;
                    #endregion 7. Имя

                    #region 8. Отчество
                    string otch = this.CheckText(ValuesFromFile.vals[i], false, 40, ref ret);
                    if (!ret.result)
                    {
                        ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + " Отчество ");
                    }
                    ValuesFromFile.sql += otch + ", ";
                    i++;
                    #endregion 8. Отчество

                    #region 9. Дата рождения
                    string dat_rog = this.CheckDateTime(ValuesFromFile.vals[i], true, ref ret);
                    if (!ret.result)
                    {
                        ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "Дата рождения");

                    }
                    ValuesFromFile.sql += dat_rog + ", ";
                    i++;
                    #endregion 9. Дата рождения

                    #region 10. Измененная фамилия
                    string fam_c = this.CheckText(ValuesFromFile.vals[i], false, 40, ref ret);
                    if (!ret.result)
                    {
                        ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + " Измененная фамилия ");
                    }
                    ValuesFromFile.sql += fam_c + ", ";
                    i++;
                    #endregion 10. Измененная фамилия

                    #region 11. Измененное имя
                    string ima_c = this.CheckText(ValuesFromFile.vals[i], false, 40, ref ret);
                    if (!ret.result)
                    {
                        ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + " Измененное имя ");
                    }
                    ValuesFromFile.sql += ima_c + ", ";
                    i++;
                    #endregion 11. Измененное имя

                    #region 12. Измененное отчество
                    string otch_c = this.CheckText(ValuesFromFile.vals[i], false, 40, ref ret);
                    if (!ret.result)
                    {
                        ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + " Измененное отчество ");
                    }
                    ValuesFromFile.sql += otch_c + ", ";
                    i++;
                    #endregion 12. Измененное отчество

                    #region 13. Измененная дата рождения
                    string dat_rog_c = this.CheckDateTime(ValuesFromFile.vals[i], false, ref ret);
                    if (!ret.result)
                    {
                        ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "Измененная дата рождения");

                    }
                    ValuesFromFile.sql += dat_rog_c + ", ";
                    i++;
                    #endregion 13. Измененная дата рождения

                    #region 14. Пол (М - мужской, Ж - женский)
                    string gender = this.CheckText(ValuesFromFile.vals[i], true, 1, ref ret);
                    if (!ret.result)
                    {
                        ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + " Пол (М - мужской, Ж - женский) ");
                    }
                    ValuesFromFile.sql += gender + ", ";
                    i++;
                    #endregion 14. Пол

                    #region 15. Тип удостоверения личности (1-паспорт, 2-св-во, 3-справка, 4-воен.билет, 5-удостоверение)
                    string nzp_dok = this.CheckInt(ValuesFromFile.vals[i], true, 1, 5, ref ret);
                    if (!ret.result)
                    {
                        ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "Тип удостоверения личности");
                    }
                    ValuesFromFile.sql += nzp_dok + ", ";
                    i++;
                    #endregion 15. Тип удостоверения личности

                    #region 16. Серия удостоверения личности
                    string serij = this.CheckText(ValuesFromFile.vals[i], true, 10, ref ret);
                    if (!ret.result)
                    {
                        ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + " Серия удостоверения личности ");
                    }
                    ValuesFromFile.sql += serij + ", ";
                    i++;
                    #endregion 16. Серия удостоверения личности

                    #region 17. Номер удостоверения личности
                    string nomer = this.CheckText(ValuesFromFile.vals[i], true, 7, ref ret);
                    if (!ret.result)
                    {
                        ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + " Номер удостоверения личности ");
                    }
                    ValuesFromFile.sql += nomer + ", ";
                    i++;
                    #endregion 17. Номер удостоверения личности

                    #region 18. Дата выдачи удостоверения личности
                    string vid_dat = this.CheckDateTime(ValuesFromFile.vals[i], false, ref ret);
                    if (!ret.result)
                    {
                        ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "Дата выдачи удостоверения личности");
                    }
                    ValuesFromFile.sql += vid_dat + ", ";
                    i++;
                    #endregion 18. Дата выдачи удостоверения личности

                    #region 19. Место выдачи удостоверения личности
                    string vid_mes = this.CheckText(ValuesFromFile.vals[i], true, 70, ref ret);
                    if (!ret.result)
                    {
                        ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + " Место выдачи удостоверения личности ");
                    }
                    ValuesFromFile.sql += vid_mes + ", ";
                    i++;
                    #endregion 19. Место выдачи удостоверения личности

                    #region 20. Код органа выдачи удостоверения личности
                    string kod_podrazd = this.CheckText(ValuesFromFile.vals[i], false, 7, ref ret);
                    if (!ret.result)
                    {
                        ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + " Код органа выдачи удостоверения личности ");
                    }
                    ValuesFromFile.sql += kod_podrazd + ", ";
                    i++;
                    #endregion 20. Код органа выдачи удостоверения личности

                    #region 21. Страна рождения
                    string strana_mr = this.CheckText(ValuesFromFile.vals[i], false, 40, ref ret);
                    if (!ret.result)
                    {
                        ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + " Страна рождения ");
                    }
                    ValuesFromFile.sql += strana_mr + ", ";
                    i++;
                    #endregion 21. Страна рождения

                    #region 22. Регион рождения
                    string region_mr = this.CheckText(ValuesFromFile.vals[i], false, 40, ref ret);
                    if (!ret.result)
                    {
                        ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + " Регион рождения ");
                    }
                    ValuesFromFile.sql += region_mr + ", ";
                    i++;
                    #endregion 22. Регион рождения

                    #region 23. Округ рождения
                    string okrug_mr = this.CheckText(ValuesFromFile.vals[i], false, 40, ref ret);
                    if (!ret.result)
                    {
                        ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + " Округ рождения ");
                    }
                    ValuesFromFile.sql += okrug_mr + ", ";
                    i++;
                    #endregion 23. Округ рождения

                    #region 24. Город рождения
                    string gorod_mr = this.CheckText(ValuesFromFile.vals[i], false, 40, ref ret);
                    if (!ret.result)
                    {
                        ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + " Город рождения ");
                    }
                    ValuesFromFile.sql += gorod_mr + ", ";
                    i++;
                    #endregion 24. Город рождения

                    #region 25. Нас. пункт рождения
                    string npunkt_mr = this.CheckText(ValuesFromFile.vals[i], false, 40, ref ret);
                    if (!ret.result)
                    {
                        ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + " Нас. пункт рождения ");
                    }
                    ValuesFromFile.sql += npunkt_mr + ", ";
                    i++;
                    #endregion 25. Нас. пункт рождения

                    #region 26. Страна откуда прибыл
                    string strana_op;
                    //if (nzp_tkrt == "1") strana_op = this.CheckText(vals[i], true, 40, ref ret);
                    //else 
                    strana_op = this.CheckText(ValuesFromFile.vals[i], false, 40, ref ret);
                    if (!ret.result)
                    {
                        ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "  Страна откуда прибыл ");
                    }
                    ValuesFromFile.sql += strana_op + ", ";
                    i++;
                    #endregion 26.  Страна откуда прибыл

                    #region 27. Регион откуда прибыл
                    string region_op;
                    //if (nzp_tkrt == "1") region_op = this.CheckText(vals[i], true, 40, ref ret);
                    //else 
                    region_op = this.CheckText(ValuesFromFile.vals[i], false, 40, ref ret);
                    if (!ret.result)
                    {
                        ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "  Регион откуда прибыл ");
                    }
                    ValuesFromFile.sql += region_op + ", ";
                    i++;
                    #endregion 27.  Регион откуда прибыл

                    #region 28. Район откуда прибыл
                    string okrug_op;
                    //if (nzp_tkrt == "1") okrug_op = this.CheckText(vals[i], true, 40, ref ret);
                    //else 
                    okrug_op = this.CheckText(ValuesFromFile.vals[i], false, 40, ref ret);
                    if (!ret.result)
                    {
                        ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "  Район откуда прибыл ");
                    }
                    ValuesFromFile.sql += okrug_op + ", ";
                    i++;
                    #endregion 28.  Район откуда прибыл

                    #region 29. Город откуда прибыл
                    string gorod_op;
                    //if (nzp_tkrt == "1") gorod_op = this.CheckText(vals[i], true, 40, ref ret);
                    //else 
                    gorod_op = this.CheckText(ValuesFromFile.vals[i], false, 40, ref ret);
                    if (!ret.result)
                    {
                        ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + " Город откуда прибыл ");
                    }
                    ValuesFromFile.sql += gorod_op + ", ";
                    i++;
                    #endregion 29.  Город откуда прибыл

                    #region 30. Нас. пункт откуда прибыл
                    string npunkt_op;
                    //if (nzp_tkrt == "1") npunkt_op = this.CheckText(vals[i], true, 40, ref ret);
                    //else 
                    npunkt_op = this.CheckText(ValuesFromFile.vals[i], false, 40, ref ret);
                    if (!ret.result)
                    {
                        ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + " Нас. пункт откуда прибыл ");
                    }
                    ValuesFromFile.sql += npunkt_op + ", ";
                    i++;
                    #endregion 30.  Нас. пункт откуда прибыл

                    #region 31. Улица, дом, корпус, квартира откуда прибыл
                    string rem_op;
                    //if (nzp_tkrt == "1") rem_op = this.CheckText(vals[i], true, 40, ref ret);
                    //else 
                    rem_op = this.CheckText(ValuesFromFile.vals[i], false, 40, ref ret);
                    if (!ret.result)
                    {
                        ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + " Улица, дом, корпус, квартира откуда прибыл ");
                    }
                    ValuesFromFile.sql += rem_op + ", ";
                    i++;
                    #endregion 31.  Улица, дом, корпус, квартира откуда прибыл

                    #region 32. Страна куда убыл
                    string strana_ku;
                    if (nzp_tkrt == "2") strana_ku = this.CheckText(ValuesFromFile.vals[i], true, 40, ref ret);
                    else
                        strana_ku = this.CheckText(ValuesFromFile.vals[i], false, 40, ref ret);
                    if (!ret.result)
                    {
                        ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + " Страна куда убыл ");
                    }
                    ValuesFromFile.sql += strana_ku + ", ";
                    i++;
                    #endregion 32.  Страна куда убыл

                    #region 33. Регион куда убыл
                    string region_ku;
                    if (nzp_tkrt == "2") region_ku = this.CheckText(ValuesFromFile.vals[i], true, 40, ref ret);
                    else
                        region_ku = this.CheckText(ValuesFromFile.vals[i], false, 40, ref ret);
                    if (!ret.result)
                    {
                        ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + " Регион куда убыл ");
                    }
                    ValuesFromFile.sql += region_ku + ", ";
                    i++;
                    #endregion 33. Регион куда убыл

                    #region 34. Район куда убыл
                    string okrug_ku;
                    if (nzp_tkrt == "2") okrug_ku = this.CheckText(ValuesFromFile.vals[i], true, 40, ref ret);
                    else
                        okrug_ku = this.CheckText(ValuesFromFile.vals[i], false, 40, ref ret);
                    if (!ret.result)
                    {
                        ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + " Район куда убыл ");
                    }
                    ValuesFromFile.sql += okrug_ku + ", ";
                    i++;
                    #endregion 34. Район куда убыл

                    #region 35. Город куда убыл
                    string gorod_ku;
                    if (nzp_tkrt == "2") gorod_ku = this.CheckText(ValuesFromFile.vals[i], true, 40, ref ret);
                    else
                        gorod_ku = this.CheckText(ValuesFromFile.vals[i], false, 40, ref ret);
                    if (!ret.result)
                    {
                        ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + " Город куда убыл ");
                    }
                    ValuesFromFile.sql += gorod_ku + ", ";
                    i++;
                    #endregion 35. Город куда убыл

                    #region 36. Нас.пункт куда убыл
                    string npunkt_ku;
                    if (nzp_tkrt == "2") npunkt_ku = this.CheckText(ValuesFromFile.vals[i], true, 40, ref ret);
                    else
                        npunkt_ku = this.CheckText(ValuesFromFile.vals[i], false, 40, ref ret);
                    if (!ret.result)
                    {
                        ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + " Нас.пункт куда убыл ");
                    }
                    ValuesFromFile.sql += npunkt_ku + ", ";
                    i++;
                    #endregion 36. Нас.пункт куда убыл

                    #region 37. Улица, дом, корпус, квартира куда убыл
                    string rem_ku;
                    if (nzp_tkrt == "2") rem_ku = this.CheckText(ValuesFromFile.vals[i], true, 40, ref ret);
                    else
                        rem_ku = this.CheckText(ValuesFromFile.vals[i], false, 40, ref ret);
                    if (!ret.result)
                    {
                        ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + " Улица, дом, корпус, квартира куда убыл ");
                    }
                    ValuesFromFile.sql += rem_ku + ", ";
                    i++;
                    #endregion 37. Улица, дом, корпус, квартира куда убыл

                    #region 38. Улица, дом, корпус, квартира для поля "переезд в том же нас. пункте"
                    string rem_p;
                    if (nzp_tkrt == "2") rem_p = this.CheckText(ValuesFromFile.vals[i], true, 40, ref ret);
                    else
                        rem_p = this.CheckText(ValuesFromFile.vals[i], false, 40, ref ret);
                    if (!ret.result)
                    {
                        ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + " Улица, дом, корпус, квартира для поля \"переезд в том же нас. пункте\" ");
                    }
                    ValuesFromFile.sql += rem_p + ", ";
                    i++;
                    #endregion 38. Улица, дом, корпус, квартира для поля "переезд в том же нас. пункте"

                    #region 39. Тип регистрации (П - по месту жмительства, В - по месту пребывания)
                    string tprp = this.CheckText(ValuesFromFile.vals[i], true, 1, ref ret);
                    if (!ret.result)
                    {
                        ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + " Тип регистрации ");
                    }
                    ValuesFromFile.sql += tprp + ", ";
                    i++;
                    #endregion 39. Тип регистрации

                    #region 40. Дата первой регистрации по адресу
                    string dat_prop = this.CheckDateTime(ValuesFromFile.vals[i], false, ref ret);
                    if (!ret.result)
                    {
                        ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + " Дата первой регистрации по адресу ");
                    }
                    ValuesFromFile.sql += dat_prop + ", ";
                    i++;
                    #endregion 40. Дата первой регистрации по адресу

                    #region 41. Дата окончания регистрации по месту пребывания
                    string dat_oprp;
                    if (nzp_tkrt == "1") dat_oprp = this.CheckDateTime(ValuesFromFile.vals[i], true, ref ret);
                    else
                        dat_oprp = this.CheckDateTime(ValuesFromFile.vals[i], false, ref ret);
                    if (!ret.result)
                    {
                        ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + " Дата окончания регистрации по месту пребывания ");
                    }
                    ValuesFromFile.sql += dat_oprp + ", ";
                    i++;
                    #endregion 41. Дата окончания регистрации по месту пребывания

                    #region 42. Дата постановки на воинский учет
                    string dat_pvu = this.CheckDateTime(ValuesFromFile.vals[i], false, ref ret);
                    if (!ret.result)
                    {
                        ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + " Дата постановки на воинский учет ");
                    }
                    ValuesFromFile.sql += dat_pvu + ", ";
                    i++;
                    #endregion 42. Дата постановки на воинский учет

                    #region 43. Орган регистрации воинского учета
                    string who_pvu = this.CheckText(ValuesFromFile.vals[i], false, 100, ref ret);
                    if (!ret.result)
                    {
                        ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + " Орган регистрации воинского учета ");
                    }
                    ValuesFromFile.sql += who_pvu + ", ";
                    i++;
                    #endregion 43. Орган регистрации воинского учета

                    #region 44. Дата снятия с воинского учета
                    string dat_svu = this.CheckDateTime(ValuesFromFile.vals[i], false, ref ret);
                    if (!ret.result)
                    {
                        ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + " Дата снятия с воинского учета ");
                    }
                    ValuesFromFile.sql += dat_svu + ", ";
                    i++;
                    #endregion 44. Дата снятия с воинского учета

                    #region 45. Орган регистрационного учета
                    string namereg = this.CheckText(ValuesFromFile.vals[i], false, 100, ref ret);
                    if (!ret.result)
                    {
                        ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + " Орган регистрационного учета ");
                    }
                    ValuesFromFile.sql += namereg + ", ";
                    i++;
                    #endregion 45. Орган регистрационного учета

                    #region 46. Код органа регистрации учета
                    string kod_namereg = this.CheckText(ValuesFromFile.vals[i], false, 7, ref ret);
                    if (!ret.result)
                    {
                        ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + " Код органа регистрации учета ");
                    }
                    ValuesFromFile.sql += kod_namereg + ", ";
                    i++;
                    #endregion 46. Код органа регистрации учета

                    #region 47. Родственные отношения
                    string rod = this.CheckText(ValuesFromFile.vals[i], false, 30, ref ret);
                    if (!ret.result)
                    {
                        ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + " Родственные отношения ");
                    }
                    ValuesFromFile.sql += rod + ", ";
                    i++;
                    #endregion 47. Родстенные отношения

                    #region 48. Код цели прибытия
                    string nzp_celp;
                    if (nzp_tkrt == "1") nzp_celp = this.CheckInt(ValuesFromFile.vals[i], true, null, null, ref ret);
                    else
                        nzp_celp = this.CheckInt(ValuesFromFile.vals[i], false, null, null, ref ret);
                    if (!ret.result)
                    {
                        ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "Код цели прибытия");
                    }
                    ValuesFromFile.sql += nzp_celp + ", ";
                    i++;
                    #endregion 48. Код цели прибытия

                    #region 49. Код цели убытия
                    string nzp_celu;
                    if (nzp_tkrt == "2") nzp_celu = this.CheckInt(ValuesFromFile.vals[i], true, null, null, ref ret);
                    else
                        nzp_celu = this.CheckInt(ValuesFromFile.vals[i], false, null, null, ref ret);
                    if (!ret.result)
                    {
                        ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "Код цели убытия");
                    }
                    ValuesFromFile.sql += nzp_celu + ", ";
                    i++;
                    #endregion 49. Код цели убытия

                    #region 50. Дата составления адресного листка
                    string dat_sost = this.CheckDateTime(ValuesFromFile.vals[i], true, ref ret);
                    if (!ret.result)
                    {
                        ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + " Дата составления адресного листка");
                    }
                    ValuesFromFile.sql += dat_sost + ", ";
                    i++;
                    #endregion 50. Дата составления адресного листка

                    #region 51. Дата оформления регистрации
                    string dat_ofor = this.CheckDateTime(ValuesFromFile.vals[i], true, ref ret);
                    if (!ret.result)
                    {
                        ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + " Дата оформления регистрации");
                    }
                    ValuesFromFile.sql += dat_ofor + ") ";
                    #endregion 51. Дата оформления регистрации

                #endregion Версия 1.2
        }       
    }


    /// <summary>
    ///  Загрузка 16 секции "Перечень выгруженных параметров "
    /// </summary>
    /// <param name="ValuesFromFile"></param>
    public void AddFileTypeparams_Section16(DBValuesFromFile ValuesFromFile)
    {
        Returns ret = new Returns();
        if (!ValuesFromFile.finder.sections[16])
        { return; }

        if (ValuesFromFile.Pvers != "1.0")
        {
            #region Версия 1.2 (Перечень выгруженных параметров )

            if (ValuesFromFile.vals.Length < 5)
            {
                ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + " Неправильный формат файла загрузки: перечень выгруженных параметров");
                return;
            }

            #region Заготовка инсерта
            ValuesFromFile.sql = 
                " INSERT INTO  " + Points.Pref + "_data" + tableDelimiter + "file_typeparams(nzp_file , id_prm, prm_name, level_, type_prm ) "+
                " VALUES(";
            #endregion Заготовка инсерта

            #region 1. Тип строки пропускаем
            int i = 1;

            //nzp_file
            ValuesFromFile.sql += ValuesFromFile.finder.nzp_file + ", ";

            #endregion 1. Тип строки пропускаем

            #region 2. Уникальный код параметра в системе поставщика информации
            string id_prm = this.CheckInt(ValuesFromFile.vals[i], true, null, null, ref ret);
            if (!ret.result)
            {
                ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + " Уникальный код параметра в системе поставщика информации ");
            }
            ValuesFromFile.sql += id_prm + ", ";
            i++;
            #endregion 2. Уникальный код параметра в системе поставщика информации

            #region 3. Наименование параметра
            string prm_name = this.CheckText(ValuesFromFile.vals[i], true, 60, ref ret);
            if (!ret.result)
            {
                ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "Наименование параметра");
            }
            ValuesFromFile.sql += prm_name + ", ";
            i++;

            #endregion 3. Наименование параметра

            #region 4. Принадлежность к уровню (1-база, 2-дом, 3-лицевой счет, 4-домовой ПУ)
            string level = this.CheckInt(ValuesFromFile.vals[i], true, null, null, ref ret);
            if (!ret.result)
            {
                ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + " Принадлежность к уровню ");
            }
            ValuesFromFile.sql += level + ", ";
            i++;
            #endregion 4. Принадлежность к уровню (1-база, 2-дом, 3-лицевой счет, 4-домовой ПУ)

            #region 5. Тип параметра (1-текст, 2-число, 3-дата)
            string type_prm = this.CheckInt(ValuesFromFile.vals[i], true, null, null, ref ret);
            if (!ret.result)
            {
                ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + " Тип параметра ");
            }
            ValuesFromFile.sql += type_prm + ") ";
            #endregion 5. Тип параметра
            #endregion Версия 1.2
        }
    }


    /// <summary>
    ///  Загрузка 17 секции "Перечень выгруженных типов жилья по газоснабжению "
    /// </summary>
    /// <param name="ValuesFromFile"></param>
    public void AddFileGaz_Section17(DBValuesFromFile ValuesFromFile)
    {
        Returns ret = new Returns();

        if (!ValuesFromFile.finder.sections[17])
        {return;}

        if (ValuesFromFile.Pvers != "1.0")
        {
            #region Версия 1.2 (Перечень выгруженных типов жилья по газоснабжению)

            if (ValuesFromFile.vals.Length < 3)
            {
                ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + " Неправильный формат файла загрузки: перечень выгруженных типов жилья по газоснабжению");
                return;
            }
                #region Заготовка инсерта
            ValuesFromFile.sql =
                " INSERT INTO  " + Points.Pref + "_data" + tableDelimiter + "file_gaz( nzp_file , id_prm, name) VALUES(";
                #endregion Заготовка инсерта

                #region 1. Тип строки пропускаем
                int i = 1;

                //nzp_file
                ValuesFromFile.sql += ValuesFromFile.finder.nzp_file + ", ";

                #endregion 1. Тип строки пропускаем

                #region 2. Уникальный код типа жилья по газоснабжению в системе поставщика информации
                string id_prm = this.CheckInt(ValuesFromFile.vals[i], true, null, null, ref ret);
                if (!ret.result)
                {
                    ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + " Уникальный код типа жилья по газоснабжению в системе поставщика информации");
                }
                ValuesFromFile.sql += id_prm + ", ";
                i++;
                #endregion 2. Уникальный код типа жилья по газоснабжению в системе поставщика информации

                #region 3. Наименование типа
                string name = this.CheckText(ValuesFromFile.vals[i], true, 60, ref ret);
                if (!ret.result)
                {
                    ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "Наименование типа");
                }
                ValuesFromFile.sql += name + ") ";
                #endregion 3. Наименование типа            

            #endregion Версия 1.2
        }
    }


    /// <summary>
    ///  Загрузка 18 секции "Перечень выгруженных типов жилья по водоснабжению"
    /// </summary>
    /// <param name="ValuesFromFile"></param>
    public void AddFileVoda_Section18(DBValuesFromFile ValuesFromFile)
    {
        Returns ret = new Returns();

        if (!ValuesFromFile.finder.sections[18])
        {return;}
        if (ValuesFromFile.Pvers != "1.0")
        {
            #region Версия 1.2 (Перечень выгруженных типов жилья по водоснабжению)
            if (ValuesFromFile.vals.Length < 3)
            {
                ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + " Неправильный формат файла загрузки: перечень выгруженных типов жилья по водоснабжению");
                return;
            }
            #region Заготовка инсерта
            ValuesFromFile.sql = " INSERT INTO  " + Points.Pref + "_data" + tableDelimiter + "file_voda( nzp_file , id_prm, name) VALUES(";
            #endregion Заготовка инсерта

            #region 1. Тип строки пропускаем
            int i = 1;

            //nzp_file
            ValuesFromFile.sql += ValuesFromFile.finder.nzp_file + ", ";

            #endregion 1. Тип строки пропускаем

            #region 2. Уникальный код типа жилья по водоснабжению в системе поставщика информации
            string id_prm = this.CheckInt(ValuesFromFile.vals[i], true, null, null, ref ret);
            if (!ret.result)
            {
                ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + " Уникальный код типа жилья по водоснабжению в системе поставщика информации");
            }
            ValuesFromFile.sql += id_prm + ", ";
            i++;
            #endregion 2. Уникальный код типа жилья по водоснабжению в системе поставщика информации

            #region 3. Наименование типа
            string name = this.CheckText(ValuesFromFile.vals[i], true, 60, ref ret);
            if (!ret.result)
            {
                ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "Наименование типа");
            }
            ValuesFromFile.sql += name + ") ";
            #endregion 3. Наименование типа

            #endregion Версия 1.2
        }
    }


    /// <summary>
    ///  Загрузка 19 секции "Перечень выгруженных категорий благоустройства дома "
    /// </summary>
    /// <param name="ValuesFromFile"></param>
    public void AddFileBlag_Section19(DBValuesFromFile ValuesFromFile)
    {
        Returns ret = new Returns();

        if (!ValuesFromFile.finder.sections[19])
        {return;}
        if (ValuesFromFile.Pvers != "1.0")
        {
            #region Версия 1.2 (Перечень выгруженных категорий благоустройства дома)

            if (ValuesFromFile.vals.Length < 3)
            {
                ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + " Неправильный формат файла загрузки: перечень выгруженных категорий благоустройства дома");
                return;
            }

                #region Заготовка инсерта
                ValuesFromFile.sql = 
                    " INSERT INTO  " + Points.Pref + "_data" + tableDelimiter + "file_blag(nzp_file , id_prm, name) VALUES(";
                #endregion Заготовка инсерта

                #region 1. Тип строки пропускаем
                int i = 1;

                //nzp_file
                ValuesFromFile.sql += ValuesFromFile.finder.nzp_file + ", ";

                #endregion 1. Тип строки пропускаем

                #region 2. Уникальный код категории в системе поставщика информации
                string id_prm = this.CheckInt(ValuesFromFile.vals[i], true, null, null, ref ret);
                if (!ret.result)
                {
                    ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + " Уникальный код категории в системе поставщика информации");
                }
                ValuesFromFile.sql += id_prm + ", ";
                i++;
                #endregion 2. Уникальный код категории в системе поставщика информации

                #region 3. Наименование категории благоустройства
                string name = this.CheckText(ValuesFromFile.vals[i], true, 60, ref ret);
                if (!ret.result)
                {
                    ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "Наименование категории благоустройства");
                }
                ValuesFromFile.sql += name + ") ";
                #endregion 3. Наименование категории благоустройства
            #endregion Версия 1.2
        }
    }

   
    /// <summary>
    ///  Загрузка 20 секции "Перечень дополнительных характеристик дома "
    /// </summary>
    /// <param name="ValuesFromFile"></param>
    public void AddFileParamsdom_Section20(DBValuesFromFile ValuesFromFile)
    {
        Returns ret = new Returns();

        if (!ValuesFromFile.finder.sections[20])
        {return;}

        if (ValuesFromFile.Pvers != "1.0")
        {
            #region Версия 1.2 (Перечень дополнительных характеристик дома)
            if (ValuesFromFile.vals.Length < 4)
            {
                ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + " Неправильный формат файла загрузки: перечень дополнительных характеристик дома");
                return ;
            }
                    #region Заготовка инсерта
                    ValuesFromFile.sql = " INSERT INTO  " + Points.Pref + "_data" + tableDelimiter + "file_paramsdom(nzp_file , id_dom, id_prm, val_prm) "+
                        " VALUES(";
                    #endregion Заготовка инсерта

                    #region 1. Тип строки пропускаем
                    int i = 1;

                    //nzp_file
                    ValuesFromFile.sql += ValuesFromFile.finder.nzp_file + ", ";

                    #endregion 1. Тип строки пропускаем

                    #region 2. Уникальный код дома в системе поставщика информации
                    string id_dom = this.CheckText(ValuesFromFile.vals[i], true, 20, ref ret);
                    if (!ret.result)
                    {
                        ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "Уникальный код дома в системе поставщика информации");
                    }
                    ValuesFromFile.sql += id_dom + ", ";
                    i++;
                    #endregion 2. Уникальный код дома в системе поставщика информации

                    #region 3. Код параметра дома
                    string id_prm = this.CheckInt(ValuesFromFile.vals[i], true, null, null, ref ret);
                    if (!ret.result)
                    {
                        ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + " Код параметра дома");
                    }
                    ValuesFromFile.sql += id_prm + ", ";
                    i++;
                    #endregion 3. Код параметра дома

                    #region 4. Значение параметра дома
                    string val_prm = this.CheckText(ValuesFromFile.vals[i], true, 80, ref ret);
                    if (!ret.result)
                    {
                        ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "Значение параметра дома");
                    }
                    ValuesFromFile.sql += val_prm + ") ";
                    #endregion 4. Значение параметра дома
                
                #endregion Версия 1.2
        }
    }

    /// <summary>
    ///  Загрузка 21 секции "Перечень дополнительных характеристик лицевого счета "
    /// </summary>
    /// <param name="ValuesFromFile"></param>
    public void AddFileParamsls_Section21(DBValuesFromFile ValuesFromFile)
    {
        Returns ret = new Returns();

        if (!ValuesFromFile.finder.sections[21])
        {return;}

        if (ValuesFromFile.Pvers != "1.0")
        {
            #region Версия 1.2 (Перечень дополнительных характеристик лицевого счета)
            if (ValuesFromFile.vals.Length < 4)
            {
                ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + " Неправильный формат файла загрузки: перечень дополнительных характеристик лицевого счета");
                return ;
            }
                    #region Заготовка инсерта
            ValuesFromFile.sql = 
                " INSERT INTO  " + Points.Pref + "_data" + tableDelimiter + "file_paramsls(nzp_file , ls_id, id_prm, val_prm) VALUES(";
                    #endregion Заготовка инсерта

                    #region 1. Тип строки пропускаем
                    int i = 1;

                    //nzp_file
                    ValuesFromFile.sql += ValuesFromFile.finder.nzp_file + ", ";

                    #endregion 1. Тип строки пропускаем

                    #region 2. Уникальный код лицевого счета в системе поставщика информации
                    string ls_id = this.CheckInt(ValuesFromFile.vals[i], true, null, null, ref ret);
                    if (!ret.result)
                    {
                        ls_id = this.CheckText(ValuesFromFile.vals[i], true, 20, ref ret);
                        if (!ret.result)
                        {
                            ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "Уникальный код лицевого счета в системе поставщика информации");
                        }
                    }
                    ValuesFromFile.sql += ls_id + ", ";
                    i++;
                    #endregion 2. Уникальный код лицевого счета в системе поставщика информации

                    #region 3. Код параметра лицевого счета
                    string id_prm = this.CheckInt(ValuesFromFile.vals[i], true, null, null, ref ret);
                    if (!ret.result)
                    {
                        ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + " Код параметра лицевого счета");
                    }
                    ValuesFromFile.sql += id_prm + ", ";
                    i++;
                    #endregion 3. Код параметра лицевого счета

                    #region 4. Значение параметра лицевого счета
                    string val_prm = this.CheckText(ValuesFromFile.vals[i], true, 80, ref ret);
                    if (!ret.result)
                    {
                        ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "Значение параметра лицевого счета");
                    }
                    ValuesFromFile.sql += val_prm + ") ";
                    #endregion 4. Значение параметра лицевого счета

                #endregion Версия 1.2
        }
    }

    /// <summary>
    ///  Загрузка 22 секции "Перечень оплат проведенных по лицевому счету "
    /// </summary>
    /// <param name="ValuesFromFile"></param>
    public void AddFileOplats_Section22(DBValuesFromFile ValuesFromFile)
    {
        Returns ret = new Returns();

        if (!ValuesFromFile.finder.sections[22])
        {return;}

        if (ValuesFromFile.Pvers != "1.0")
        {
            #region Версия 1.2 (Перечень оплат проведенных по лицевому счету)
            if (ValuesFromFile.vals.Length < 10)
            {
                ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + " Неправильный формат файла загрузки: перечень оплат проведенных по лицевому счету");
                return ;
            }
            
                #region Заготовка инсерта
            ValuesFromFile.sql = 
                " INSERT INTO  " + Points.Pref + "_data" + tableDelimiter + "file_oplats(nzp_file , ls_id, type_oper, numplat, dat_opl, "+
                "dat_uchet, dat_izm, sum_oplat, ist_opl, mes_oplat ";
            if (ValuesFromFile.versFull.Trim() == "'1.2.2'") {ValuesFromFile.sql += ", nzp_pack, id_serv) ";}
            else { ValuesFromFile.sql += ")"; }
            
           ValuesFromFile.sql+= " VALUES(";
                #endregion Заготовка инсерта

                #region 1. Тип строки пропускаем
                int i = 1;

                //nzp_file
                ValuesFromFile.sql += ValuesFromFile.finder.nzp_file + ", ";

                #endregion 1. Тип строки пропускаем

                #region 2. Уникальный код лицевого счета в системе поставщика информации
                string ls_id = this.CheckInt(ValuesFromFile.vals[i], true, null, null, ref ret);
                if (!ret.result)
                {
                    ls_id = this.CheckText(ValuesFromFile.vals[i], true, 20, ref ret);
                    if (!ret.result)
                    {
                        ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "Уникальный код лицевого счета в системе поставщика информации");
                    }
                }
                ValuesFromFile.sql += ls_id + ", ";
                i++;
                #endregion 2. Уникальный код лицевого счета в системе поставщика информации

                #region 3. Тип операции(1-оплата, 2-сторнирование оплаты)
                string type_oper = this.CheckInt(ValuesFromFile.vals[i], true, null, null, ref ret);
                if (!ret.result)
                {
                    ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + " Тип операции(1-оплата, 2-сторнирование оплаты) ");
                }
                ValuesFromFile.sql += type_oper + ", ";
                i++;
                #endregion 3. Тип операции(1-оплата, 2-сторнирование оплаты)

                #region 4. Номер платежного документа
                string numplat = this.CheckText(ValuesFromFile.vals[i], true, 80, ref ret);
                if (!ret.result)
                {
                    ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "Номер платежного документа");
                }
                ValuesFromFile.sql += numplat + ", ";
                i++;
                #endregion 4. Номер платежного документа

                #region 5. Дата оплаты
                string dat_opl = this.CheckDateTime(ValuesFromFile.vals[i], true, ref ret);
                if (!ret.result)
                {
                    ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "Дата оплаты");
                }
                ValuesFromFile.sql += dat_opl + ", ";
                i++;
                #endregion 5. Дата оплаты

                #region 6. Дата учета
                string dat_uchet = this.CheckDateTime(ValuesFromFile.vals[i], false, ref ret);
                if (!ret.result)
                {
                    ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "Дата учета");
                }
                ValuesFromFile.sql += dat_uchet + ", ";
                i++;
                #endregion 6. Дата учета

                #region 7. Дата корректировки
                string dat_izm = this.CheckDateTime(ValuesFromFile.vals[i], false, ref ret);
                if (!ret.result)
                {
                    ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "Дата корректировки");
                }
                ValuesFromFile.sql += dat_izm + ", ";
                i++;
                #endregion 7. Дата корректировки

                #region 8. Сумма оплаты
                string sum_oplat = this.CheckDecimal(ValuesFromFile.vals[i], true, true, null, null, ref ret);
                if (!ret.result)
                {
                    ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + " Сумма оплаты ");

                }
                ValuesFromFile.sql += sum_oplat + ", ";
                i++;
                #endregion 8. Сумма оплаты

                #region 9. Источник оплаты
                string ist_opl = this.CheckText(ValuesFromFile.vals[i], false, 60, ref ret);
                if (!ret.result)
                {
                    ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "Источник оплаты");
                }
                ValuesFromFile.sql += ist_opl + ", ";
                i++;
                #endregion 9. Источник оплаты

                #region 10. Месяц, за который произведена оплата
                string mes_oplat = this.CheckDateTime(ValuesFromFile.vals[i], false, ref ret);
                if (!ret.result)
                {
                    ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "Месяц, за который произведена оплата ");
                }
                ValuesFromFile.sql += mes_oplat;
                i++;
                #endregion 10. Месяц, за который произведена оплата

                if (ValuesFromFile.versFull.Trim() == "'1.2.2'")
                {
                    #region 11. Уникальный номер пачки
                    string nzp_pack = this.CheckInt(ValuesFromFile.vals[i], false, null, null, ref ret);
                    if (!ret.result)
                    {
                        ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + " Уникальный номер пачки ");
                    }
                    ValuesFromFile.sql += ", " + nzp_pack + ", ";
                    i++;
                    #endregion 11. Уникальный номер пачки

                    #region 12. Код услуги
                    string id_serv = this.CheckInt(ValuesFromFile.vals[i], false, null, null, ref ret);
                    if (!ret.result)
                    {
                        ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + " Код услуги ");
                    }
                    ValuesFromFile.sql += id_serv;
                    i++;
                    #endregion 12. Код услуги
                }
                ValuesFromFile.sql += ")";

                #endregion Версия 1.2
        }
    }

    /// <summary>
    ///  Загрузка 23 секции "Перечень недопоставок "
    /// </summary>
    /// <param name="ValuesFromFile"></param>
    public void AddFileNedopost_Section23(DBValuesFromFile ValuesFromFile)
    {
        Returns ret = new Returns();

        if (!ValuesFromFile.finder.sections[23])
        {return;}

        if (ValuesFromFile.Pvers != "1.0")
        {
            #region Версия 1.2 (Перечень недоспоставок)
            if (ValuesFromFile.vals.Length < 8)
            {
                ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + " Неправильный формат файла загрузки: перечень недоспоставок");
                return;
            }
            #region Заготовка инсерта
            ValuesFromFile.sql =
                " INSERT INTO  " + Points.Pref + "_data" + tableDelimiter + "file_nedopost (nzp_file, ls_id, id_serv, type_ned, temper, dat_nedstart, dat_nedstop, sum_ned) " +
                " VALUES(";
            #endregion Заготовка инсерта

            #region 1. Тип строки пропускаем
            int i = 1;
            //nzp_file
            ValuesFromFile.sql += ValuesFromFile.finder.nzp_file + ", ";
            #endregion 1. Тип строки пропускаем

            #region 2. Уникальный код лицевого счета в системе поставщика информации
            string ls_id = this.CheckInt(ValuesFromFile.vals[i], true, null, null, ref ret);
            if (!ret.result)
            {
                ls_id = this.CheckText(ValuesFromFile.vals[i], true, 20, ref ret);
                if (!ret.result)
                {
                    ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "Уникальный код лицевого счета в системе поставщика информации");
                }
            }
            ValuesFromFile.sql += ls_id + ", ";
            i++;
            #endregion 2. Уникальный код лицевого счета в системе поставщика информации

            #region 3. Код услуги в системе постащика
            string id_serv = this.CheckText(ValuesFromFile.vals[i], true, 20, ref ret);
            if (!ret.result)
            {
                ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "Код услуги в системе поставщика");
            }
            ValuesFromFile.sql += id_serv + ", ";
            i++;
            #endregion 3. Код услуги в системе поставщика

            #region 4. Тип недопоставки
            string type_ned = this.CheckInt(ValuesFromFile.vals[i], true, null, null, ref ret);
            if (!ret.result)
            {
                ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + " Тип недопоставки ");
            }
            ValuesFromFile.sql += type_ned + ", ";
            i++;
            #endregion 4. Тип недопоставки

            #region 5. Температура
            string temper = this.CheckInt(ValuesFromFile.vals[i], false, null, null, ref ret);
            if (!ret.result)
            {
                ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + " Температура ");
            }
            ValuesFromFile.sql += temper + ", ";
            i++;
            #endregion 5. Температура

            #region 6. Дата начала недопоставки
            string dat_nedstart = this.CheckDateTime(ValuesFromFile.vals[i], true, ref ret);
            if (!ret.result)
            {
                ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "Дата начала недопоставки");
            }
            ValuesFromFile.sql += dat_nedstart + ", ";
            i++;
            #endregion 6. Дата начала недопоставки

            #region 7. Дата окончания недопоставки
            string dat_nedstop = this.CheckDateTime(ValuesFromFile.vals[i], true, ref ret);
            if (!ret.result)
            {
                ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "Дата окончания недопоставки");
            }
            ValuesFromFile.sql += dat_nedstop + ", ";
            i++;
            #endregion 7. Дата окончания недопоставки

            #region 8. Сумма недопоставки
            string sum_ned = this.CheckDecimal(ValuesFromFile.vals[i], false, false, null, null, ref ret);
            
            if (!ret.result)
            {
                ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + " Сумма недопоставки ");
            }
            if (sum_ned == null)
            { sum_ned = "0"; }
            ValuesFromFile.sql += sum_ned + ") ";
            i++;
            #endregion 8. Сумма недопоставки
            #endregion Версия 1.2
        }
    }

    /// <summary>
    /// Загрузка 24 секции "Перечень типов недопоставки" 
    /// </summary>
    /// <param name="ValuesFromFile"></param>
    public void AddFileTypenedopost_Section24(DBValuesFromFile ValuesFromFile)
    {
        Returns ret = new Returns();

        if (!ValuesFromFile.finder.sections[24])
        {return;}

        if (ValuesFromFile.Pvers != "1.0")
        {
            #region Версия 1.2 (Перечень типов недопоставок)

            if (ValuesFromFile.vals.Length < 3)
            {
                ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + " Неправильный формат файла загрузки: перечень типов недопоставок");
                return;
            }
                #region Заготовка инсерта
            ValuesFromFile.sql = 
                " INSERT INTO  " + Points.Pref + "_data" + tableDelimiter + "file_typenedopost (nzp_file, type_ned, ned_name) VALUES(";
                #endregion Заготовка инсерта

                #region 1. Тип строки пропускаем
                int i = 1;
                //nzp_file
                ValuesFromFile.sql += ValuesFromFile.finder.nzp_file + ", ";
                #endregion 1. Тип строки пропускаем

                #region 2. Тип недопоставки
                string type_ned = this.CheckInt(ValuesFromFile.vals[i], true, null, null, ref ret);
                if (!ret.result)
                {
                    ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + " Тип недопоставки ");
                }
                ValuesFromFile.sql += type_ned + ", ";
                i++;
                #endregion 2. Тип недопоставки

                #region 3. Наименование недопоставки
                string ned_name = this.CheckText(ValuesFromFile.vals[i], true, 100, ref ret);
                if (!ret.result)
                {
                    ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "Наименование недопоставки");
                }
                ValuesFromFile.sql += ned_name + "); ";
                i++;
                #endregion 3. Наименование недопоставки

                #endregion Версия 1.2
        }
    }

    /// <summary>
    ///  Загрузка 25 секции "Перечень услуг лицевого счета"
    /// </summary>
    /// <param name="ValuesFromFile"></param>
    public void AddFileServls_Section25(DBValuesFromFile ValuesFromFile)
    {
        Returns ret = new Returns();

        if (!ValuesFromFile.finder.sections[25])
        {return;}
        if (ValuesFromFile.Pvers != "1.0")
        {
            #region Версия 1.2 (Перечень услуг лицевого счета)

            if (ValuesFromFile.vals.Length < 5)
            {
                ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + " Неправильный формат файла загрузки: перечень услуг лицевого счета");
                return;
            }
                    #region Заготовка инсерта
            ValuesFromFile.sql = 
                " INSERT INTO  " + Points.Pref + "_data" + tableDelimiter + "file_servls (nzp_file, ls_id, id_serv, dat_start, dat_stop, supp_id) VALUES(";
                    #endregion Заготовка инсерта

                    #region 1. Тип строки пропускаем
                    int i = 1;
                    //nzp_file
                    ValuesFromFile.sql += ValuesFromFile.finder.nzp_file + ", ";
                    #endregion 1. Тип строки пропускаем

                    #region 2. Уникальный код лицевого в системе поставщика услуг

                    string ls_id = this.CheckInt(ValuesFromFile.vals[i], true, null, null, ref ret);
                    if (!ret.result)
                    {
                        ls_id = this.CheckInt(ValuesFromFile.vals[i], true, null, null, ref ret);
                        if (!ret.result)
                        {
                            ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + " Уникальный код лицевого в системе поставщика услуг ");
                        }
                    }
                    ValuesFromFile.sql += ls_id + ", ";
                    i++;
                    #endregion 2. Уникальный код лицевого в системе поставщика услуг

                    #region 3. Код услуги в системе поставщика информации
                    string id_serv = this.CheckText(ValuesFromFile.vals[i], true, 100, ref ret);
                    if (!ret.result)
                    {
                        ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "Код услуги в системе поставщика информации");
                    }
                    ValuesFromFile.sql += id_serv + ", ";
                    i++;
                    #endregion 3. Код услуги в системе поставщика информации

                    #region 4. Дата начала действия услуг
                    string dat_start = this.CheckDateTime(ValuesFromFile.vals[i], true, ref ret);
                    if (!ret.result)
                    {
                        ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "Дата начала действия услуг");
                    }
                    ValuesFromFile.sql += dat_start + ", ";
                    i++;
                    #endregion 4. Дата начала действия услуг

                    #region 5. Дата окончиная действия услуг
                    string dat_stop = this.CheckDateTime(ValuesFromFile.vals[i], true, ref ret);
                    if (!ret.result)
                    {
                        ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "Дата окончиная действия услуг");
                    }
                    ValuesFromFile.sql += dat_stop + ", ";
                    i++;
                    #endregion 5. Дата окончиная действия услуг

                    //if (versFull.Trim() == "'1.2.2'")
                    //{
                    #region 6. Уникальный код поставщика
                    string supp_id = this.CheckDecimal(ValuesFromFile.vals[i], true, false, 0, null, ref ret);
                    if (!ret.result)
                    {
                        ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + " Уникальный код поставщика");
                    }
                    ValuesFromFile.sql += supp_id;
                    i++;
                    #endregion 11. Уникальный код поставщика
                    //}
                    //else
                    //    sql += "null";

                    ValuesFromFile.sql += "); ";

                #endregion Версия 1.2
        }
    }

    /// <summary>
    ///  Загрузка 26 секции "Пачки реестров"
    /// </summary>
    /// <param name="ValuesFromFile"></param>
    public void AddFilePack_Section26(DBValuesFromFile ValuesFromFile)
    {
        Returns ret = new Returns();

        if (!ValuesFromFile.finder.sections[26])
        {return;}
        if (ValuesFromFile.Pvers != "1.0")
        {
            #region Версия 1.2.2
            if (ValuesFromFile.vals.Length < 6)
            {
                ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + " Неправильный формат файла загрузки: пачки реестров");
                return;
            }
            #region Заготовка инсерта
            ValuesFromFile.sql =
                " INSERT INTO  " + Points.Pref + "_data" + tableDelimiter + "file_pack (id, nzp_file, dat_plat, num_plat, sum_plat, kol_plat) VALUES( ";
            #endregion Заготовка инсерта

            #region 1. Уникальный номер пачки (тип сроки пропускаем)
            int i = 1;
            string id = this.CheckInt(ValuesFromFile.vals[i], true, null, null, ref ret);
            if (!ret.result)
            {
                ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + " Уникальный номер пачки ");
            }
            ValuesFromFile.sql += id + ", ";
            i++;
            #endregion 1.

            #region 2. nzp_file
            //nzp_file
            ValuesFromFile.sql += ValuesFromFile.finder.nzp_file + ", ";
            #endregion

            #region 3. Дата платежного поручения
            string dat_plat = this.CheckDateTime(ValuesFromFile.vals[i], true, ref ret);
            if (!ret.result)
            {
                ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + "Дата платежного поручения");
            }
            ValuesFromFile.sql += dat_plat + ", ";
            i++;
            #endregion

            #region 4. Номер платежного поручения
            string num_plat = this.CheckInt(ValuesFromFile.vals[i], true, null, null, ref ret);
            if (!ret.result)
            {
                ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + " Номер платежного поручения ");
            }
            ValuesFromFile.sql += num_plat + ", ";
            i++;
            #endregion

            #region 5. Сумма платежа
            string sum_plat = this.CheckDecimal(ValuesFromFile.vals[i], true, true, null, null, ref ret);
            if (!ret.result)
            {
                ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + " Сумма платежа ");

            }
            ValuesFromFile.sql += sum_plat + ", ";
            i++;
            #endregion

            #region 6. Количество платежей, вошедших в платежное поручение
            string kol_plat = this.CheckInt(ValuesFromFile.vals[i], true, null, null, ref ret);
            if (!ret.result)
            {
                ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + " Количество платежей, вошедших в платежное поручение ");
            }
            ValuesFromFile.sql += kol_plat + "); ";
            i++;
            #endregion
            #endregion
        }
    }

    /// <summary>
    ///  Загрузка 27 секции "Юридические лица (арендаторы и собственники)"
    /// </summary>
    /// <param name="ValuesFromFile"></param>
    public void AddFileUrlic_Section27(DBValuesFromFile ValuesFromFile)
    {
        Returns ret = new Returns();

        if (!ValuesFromFile.finder.sections[27])
        { return; }
        if (ValuesFromFile.Pvers != "1.0")
        {
            #region Версия 1.2.2
            if (ValuesFromFile.vals.Length < 25)
            {
                ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + " Неправильный формат файла загрузки: юридические лица (арендаторы и собственники)");
                return;
            }
                #region Заготовка инсерта
            ValuesFromFile.sql = 
                " INSERT INTO  " + Points.Pref + "_data" + tableDelimiter + "file_urlic (nzp_file, supp_id, supp_name, "+
                " jur_address, fact_address, inn, kpp, rs, bank, bik_bank, ks, tel_chief, tel_b, chief_name,chief_post, "+
                " b_name, okonh1, okonh2, okpo, bank_pr, bank_adr, bik, rs_pr, ks_pr, post_and_name, nzp_supp) "+
                " VALUES(";
                #endregion Заготовка инсерта

                #region 1. Тип строки пропускаем, nzp_file
                int i = 1;
                //nzp_file
                ValuesFromFile.sql += ValuesFromFile.finder.nzp_file + ", ";
                #endregion 1. Тип строки пропускаем

                #region 2. Уникальный код ЮЛ
                string supp_id = this.CheckInt(ValuesFromFile.vals[i], true, null, null, ref ret);
                if (!ret.result)
                {
                    ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + " Уникальный код ЮЛ ");
                }
                ValuesFromFile.sql += supp_id + ", ";
                i++;
                #endregion 2. Уникальный код ЮЛ

                #region 3. Наименование ЮЛ
                string supp_name = this.CheckText(ValuesFromFile.vals[i], true, 100, ref ret);
                if (!ret.result)
                {
                    ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + " Наименование ЮЛ");
                }
                ValuesFromFile.sql += supp_name + ", ";
                i++;
                #endregion 3. Наименование ЮЛ

                #region 4. Юридический адрес
                string jur_address = this.CheckText(ValuesFromFile.vals[i], false, 100, ref ret);
                if (!ret.result)
                {
                    ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + " Юридический адрес");
                }
                ValuesFromFile.sql += jur_address + ", ";
                i++;
                #endregion 4. Юридический адрес

                #region 5. Фактический адрес
                string fact_address = this.CheckText(ValuesFromFile.vals[i], false, 100, ref ret);
                if (!ret.result)
                {
                    ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + " Фактический адрес");
                }
                ValuesFromFile.sql += fact_address + ", ";
                i++;
                #endregion 5. Фактический адрес

                #region 6. ИНН
                string inn = this.CheckText(ValuesFromFile.vals[i], true, 12, ref ret);
                if (!ret.result)
                {
                    ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + " ИНН");
                }
                ValuesFromFile.sql += inn + ", ";
                i++;
                #endregion 6. ИНН

                #region 7. КПП
                string kpp = this.CheckText(ValuesFromFile.vals[i], true, 9, ref ret);
                if (!ret.result)
                {
                    ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + " КПП");
                }
                ValuesFromFile.sql += kpp + ", ";
                i++;
                #endregion 7. КПП

                #region 8. Расчетный счет
                string rs = this.CheckText(ValuesFromFile.vals[i].Trim(), false, 20, ref ret);
                if (!ret.result)
                {
                    ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + " Расчетный счет");
                }
                ValuesFromFile.sql += rs + ", ";
                i++;
                #endregion 8. Расчетный счет

                #region 9. Банк
                string bank = this.CheckText(ValuesFromFile.vals[i], false, 100, ref ret);
                if (!ret.result)
                {
                    ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + " Банк");
                }
                ValuesFromFile.sql += bank + ", ";
                i++;
                #endregion 9. Банк

                #region 10. БИК банка
                string bik_bank = this.CheckText(ValuesFromFile.vals[i], false, 20, ref ret);
                if (!ret.result)
                {
                    ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + " БИК банка");
                }
                ValuesFromFile.sql += bik_bank + ", ";
                i++;
                #endregion 10. БИК банка

                #region 11. Корреспондентский счет
                string ks = this.CheckText(ValuesFromFile.vals[i], false, 20, ref ret);
                if (!ret.result)
                {
                    ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + " Корреспондентский счет");
                }
                ValuesFromFile.sql += ks + ", ";
                i++;
                #endregion 11. Корреспондентский счет

                #region 12. Телефон руководителя
                string tel_chief = this.CheckText(ValuesFromFile.vals[i], false, 20, ref ret);
                if (!ret.result)
                {
                    ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + " Телефон руководителя");
                }
                ValuesFromFile.sql += tel_chief + ", ";
                i++;
                #endregion 12.  Телефон руководителя

                #region 13. Телефон бухгалтерии
                string tel_b = this.CheckText(ValuesFromFile.vals[i], false, 20, ref ret);
                if (!ret.result)
                {
                    ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + " Телефон бухгалтерии");
                }
                ValuesFromFile.sql += tel_b + ", ";
                i++;
                #endregion 13.  Телефон бухгалтерии

                #region 14. ФИО руководителя
                string chief_name = this.CheckText(ValuesFromFile.vals[i], false, 100, ref ret);
                if (!ret.result)
                {
                    ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + " ФИО руководителя");
                }
                ValuesFromFile.sql += chief_name + ", ";
                i++;
                #endregion 14.  ФИО руководителя

                #region 15. Должность руководителя
                string chief_post = this.CheckText(ValuesFromFile.vals[i], false, 40, ref ret);
                if (!ret.result)
                {
                    ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + " Должность руководителя");
                }
                ValuesFromFile.sql += chief_post + ", ";
                i++;
                #endregion 15.  Должность руководителя

                #region 16. ФИО бухгалтера
                string b_name = this.CheckText(ValuesFromFile.vals[i], false, 100, ref ret);
                if (!ret.result)
                {
                    ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + " ФИО бухгалтера");
                }
                ValuesFromFile.sql += b_name + ", ";
                i++;
                #endregion 16.  ФИО бухгалтера

                #region 17. ОКОНХ1
                string okonh1 = this.CheckText(ValuesFromFile.vals[i], false, 20, ref ret);
                if (!ret.result)
                {
                    ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + " ОКОНХ1");
                }
                ValuesFromFile.sql += okonh1 + ", ";
                i++;
                #endregion 17. ОКОНХ1

                #region 18. ОКОНХ2
                string okonh2 = this.CheckText(ValuesFromFile.vals[i], false, 20, ref ret);
                if (!ret.result)
                {
                    ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + " ОКОНХ2");
                }
                ValuesFromFile.sql += okonh2 + ", ";
                i++;
                #endregion 18. ОКОНХ2

                #region 19. ОКПО
                string okpo = this.CheckText(ValuesFromFile.vals[i], false, 20, ref ret);
                if (!ret.result)
                {
                    ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + " ОКПО");
                }
                ValuesFromFile.sql += okpo + ", ";
                i++;
                #endregion 19. ОКПО

                #region 20. Банк предприятия
                string bank_pr = this.CheckText(ValuesFromFile.vals[i], false, 100, ref ret);
                if (!ret.result)
                {
                    ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + " Банк предприятия");
                }
                ValuesFromFile.sql += bank_pr + ", ";
                i++;
                #endregion 20. Банк предприятия

                #region 21. Адрес банка
                string bank_adr = this.CheckText(ValuesFromFile.vals[i], false, 100, ref ret);
                if (!ret.result)
                {
                    ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + " Адрес банка");
                }
                ValuesFromFile.sql += bank_adr + ", ";
                i++;
                #endregion 21. Адрес банка

                #region 22. БИК
                string bik = this.CheckText(ValuesFromFile.vals[i], false, 20, ref ret);
                if (!ret.result)
                {
                    ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + " БИК");
                }
                ValuesFromFile.sql += bik + ", ";
                i++;
                #endregion 22. БИК

                #region 23. Р/счет предприятия
                string rs_pr = this.CheckText(ValuesFromFile.vals[i], false, 20, ref ret);
                if (!ret.result)
                {
                    ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + " Р/счет предприятия");
                }
                ValuesFromFile.sql += rs_pr + ", ";
                i++;
                #endregion 23. Р/счет предприятия

                #region 24. К/счет предприятия
                string ks_pr = this.CheckText(ValuesFromFile.vals[i], false, 20, ref ret);
                if (!ret.result)
                {
                    ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + " К/счет предприятия");
                }
                ValuesFromFile.sql += ks_pr + ", ";
                i++;
                #endregion 24. К/счет предприятия

                #region 25. Должность + ФИО в Р.п.
                string post_and_name = this.CheckText(ValuesFromFile.vals[i], false, 200, ref ret);
                if (!ret.result)
                {
                    ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + " Должность + ФИО в Р.п.");
                }
                ValuesFromFile.sql += post_and_name + ", ";
                i++;
                #endregion 25. Должность + ФИО в Р.п.

                #region Конец запроса
                ValuesFromFile.sql += " null);";
                i++;
                #endregion            
            #endregion
        }
    }

    /// <summary>
    ///  Загрузка 28 секции "Реестр временно-убывших"
    /// </summary>
    /// <param name="ValuesFromFile"></param>
    public void AddFileVrub_Section28(DBValuesFromFile ValuesFromFile)
    {
        Returns ret = new Returns();

        if (!ValuesFromFile.finder.sections[28])
        {return;}

        if (ValuesFromFile.Pvers != "1.0")
        {
            #region Версия 1.2.2
            if (ValuesFromFile.vals.Length < 5)
            {
                ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + " Неправильный формат файла загрузки: ресстр временно убывших");
                return;
            }       
            #region Заготовка инсерта
            ValuesFromFile.sql = 
                " INSERT INTO  " + Points.Pref + "_data" + tableDelimiter + "file_vrub (nzp_file, ls_id, gil_id, dat_vrvib, dat_end)  VALUES(";
                    #endregion Заготовка инсерта

                    #region 1. Тип строки пропускаем, nzp_file
                    int i = 1;
                    //nzp_file
                    ValuesFromFile.sql += ValuesFromFile.finder.nzp_file + ", ";
                    #endregion 1. Тип строки пропускаем

                    #region 2. Уникальный номер лицевого счета

                    string ls_id = this.CheckInt(ValuesFromFile.vals[i], true, 1, null, ref ret).ToString();
                    if (!ret.result)
                    {
                        ls_id = this.CheckText(ValuesFromFile.vals[i], true, 20, ref ret);
                        if (!ret.result)
                        {
                            ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + " Уникальный номер лицевого счета");
                        }
                    }
                    ValuesFromFile.sql += ls_id + ", ";
                    i++;
                    #endregion 2. Уникальный номер лицевого счета

                    #region 3. Уникальный номер гражданина
                    string gil_id = this.CheckInt(ValuesFromFile.vals[i], true, null, null, ref ret);
                    if (!ret.result)
                    {
                        ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + " Уникальный номер гражданина ");
                    }
                    ValuesFromFile.sql += gil_id + ", ";
                    i++;
                    #endregion 3. Уникальный номер гражданина

                    #region 4. Дата начала
                    string dat_vrvib = this.CheckDateTime(ValuesFromFile.vals[i], true, ref ret);
                    if (!ret.result)
                    {
                        ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + " Дата начала");
                    }
                    ValuesFromFile.sql += dat_vrvib + ", ";
                    i++;
                    #endregion 4. Дата начала

                    #region 4. Дата окончания
                    string dat_end = this.CheckDateTime(ValuesFromFile.vals[i], true, ref ret);
                    if (!ret.result)
                    {
                        ValuesFromFile.err.Append(ValuesFromFile.rowNumber + ret.text + " Дата окончания");
                    }
                    ValuesFromFile.sql += dat_end + "); ";
                    i++;
                    #endregion 4. Дата окончания
                
                #endregion
        }
    }


    /// <summary>
    /// Добавляет запись в список моих файлов и возвращает код записи при успешном выполнении операции
    /// </summary>
    /// <param name="finder"></param>
    /// <returns></returns>
    public int AddMyFile(string repname, FilesImported finder)
    {
        ExcelRepClient excelRep = new ExcelRepClient();
        Returns ret = excelRep.AddMyFile(new ExcelUtility()
        {
            nzp_user = finder.nzp_user,
            status = ExcelUtility.Statuses.InProcess,
            rep_name = repname
        });
        if (!ret.result)
        {
            //MonitorLog.WriteLog("Ошибка при записи в список моих файлов. Название файла '" +repname+"'", MonitorLog.typelog.Error, true);
            return -1;
        }

        return ret.tag;       
    }

  
        //todo засунуть внутрь класса создание таблиц
        
    /// <summary>
    /// Создает отсутствующие таблицы в БД
    /// </summary>
    /// <param name="conn_db"></param>
    /// <returns></returns>
    public Returns CreateTable(IDbConnection conn_db)
    {
        Returns ret = Utils.InitReturns();

        try
        {
            #region 1 Проверка таблицы file_pack
#if PG
            string fields = "(id INTEGER, nzp_file INTEGER, dat_plat DATE, num_plat character(20), sum_plat NUMERIC(14,2), kol_plat INTEGER)";
#else
                string fields = "(id INTEGER, nzp_file INTEGER, dat_plat DATE, num_plat CHAR(20), sum_plat DECIMAL(14,2), kol_plat INTEGER)";
#endif
            ret = CreateOneTable(conn_db, "file_pack", "_data", fields);
            #endregion Проверка таблицы file_pack

            #region 2 Проверка таблицы file_area
#if PG
            fields = "(id SERIAL NOT NULL, nzp_file INTEGER NOT NULL, area CHARACTER(40), jur_address CHARACTER(100), fact_address CHARACTER(100)," +
                                    "inn CHARACTER(12), kpp CHARACTER(9), rs CHARACTER(20), bank CHARACTER(100), bik CHARACTER(20), ks CHARACTER(20), nzp_area INTEGER)";
#else
                fields = "(id SERIAL NOT NULL, nzp_file INTEGER NOT NULL, area CHAR(40), jur_address CHAR(100), fact_address CHAR(100)," +
                                        "inn CHAR(12), kpp CHAR(9), rs CHAR(20), bank CHAR(100), bik CHAR(20), ks CHAR(20), nzp_area INTEGER)";
#endif
            ret = CreateOneTable(conn_db, "file_area", "_data", fields);
            #endregion Проверка таблицы file_area

            #region 3 Проверка таблицы file_dom
#if PG
            fields = "(id NUMERIC(18,0), nzp_file INTEGER NOT NULL, ukds INTEGER, town VARCHAR(100), rajon VARCHAR(100), ulica VARCHAR(100), ndom CHAR(10), nkor CHARACTER(3)," +
                                  "area_id NUMERIC(18,0) NOT NULL, cat_blago CHARACTER(30), etazh INTEGER NOT NULL, build_year DATE, total_square NUMERIC(14,2) NOT NULL," +
                                  "mop_square NUMERIC(14,2), useful_square NUMERIC(14,2), mo_id NUMERIC(13,0), params CHARACTER(250), ls_row_number INTEGER NOT NULL," +
                                  "odpu_row_number INTEGER NOT NULL, nzp_ul INTEGER, nzp_dom INTEGER, comment VARCHAR(250), local_id CHAR(20), nzp_raj INTEGER, nzp_town INTEGER, " +
                                  " nzp_geu INTEGER, uch INTEGER, kod_kladr CHARACTER(30))";
#else
                fields = "(id DECIMAL(18,0), nzp_file INTEGER NOT NULL, ukds INTEGER, town CHAR(100), rajon CHAR(30), ulica CHAR(40), ndom CHAR(10), nkor CHAR(3)," +
                                      "area_id DECIMAL(18,0) NOT NULL, cat_blago CHAR(30), etazh INTEGER NOT NULL, build_year DATE, total_square DECIMAL(14,2) NOT NULL," +
                                      "mop_square DECIMAL(14,2), useful_square DECIMAL(14,2), mo_id DECIMAL(13,0), params CHAR(250), ls_row_number INTEGER NOT NULL," +
                                      "odpu_row_number INTEGER NOT NULL, nzp_ul INTEGER, nzp_dom INTEGER, comment CHAR(250), local_id CHAR(20), nzp_raj INTEGER, nzp_town INTEGER, " + 
                                      " nzp_geu INTEGER, uch INTEGER, kod_kladr CHAR(30))";
#endif
            ret = CreateOneTable(conn_db, "file_dom", "_data", fields);
            #endregion Проверка таблицы file_dom

            #region 4 Проверка таблицы file_gaz
            fields = "(id SERIAL NOT NULL, id_prm INTEGER, name CHAR(100), nzp_file INTEGER, nzp_prm INTEGER)";
            ret = CreateOneTable(conn_db, "file_gaz", "_data", fields);
            #endregion Проверка таблицы file_gaz

            #region 5 Проверка таблицы file_gilec
            fields = "(nzp_file INTEGER, num_ls INTEGER, nzp_gil INTEGER, nzp_kart INTEGER, nzp_tkrt INTEGER, fam CHAR(60), ima CHAR(60), otch CHAR(60), dat_rog DATE," +
                                    "fam_c CHAR(60), ima_c CHAR(60), otch_c CHAR(60), dat_rog_c DATE, gender NCHAR(1), nzp_dok INTEGER, serij NCHAR(10), nomer NCHAR(7), vid_dat DATE, vid_mes CHAR(70)," +
                                    "kod_podrazd CHAR(7), strana_mr CHAR(60), region_mr CHAR(30), okrug_mr CHAR(30), gorod_mr CHAR(30), npunkt_mr CHAR(30), rem_mr CHAR(180), strana_op CHAR(60), region_op CHAR(60)," +
                                    "okrug_op CHAR(60), gorod_op CHAR(60), npunkt_op CHAR(30), rem_op CHAR(180), strana_ku CHAR(30), region_ku CHAR(30), okrug_ku CHAR(30), gorod_ku CHAR(60), npunkt_ku CHAR(60)," +
                                    "rem_ku CHAR(180), rem_p CHAR(40), tprp NCHAR(1), dat_prop DATE, dat_oprp DATE, dat_pvu DATE, who_pvu CHAR(40), dat_svu DATE, namereg CHAR(80), kod_namereg CHAR(7)," +
                                    "rod CHAR(60), nzp_celp INTEGER, nzp_celu INTEGER, dat_sost DATE, dat_ofor DATE, comment CHAR(40), id SERIAL NOT NULL)";
            ret = CreateOneTable(conn_db, "file_gilec", "_data", fields);
            #endregion Проверка таблицы file_gilec

            #region 6 Проверка таблицы file_head
            fields = "(id SERIAL NOT NULL, nzp_file INTEGER NOT NULL, org_name CHAR(40) NOT NULL, branch_name CHAR(40) NOT NULL, inn CHAR(12) NOT NULL, kpp CHAR(9) NOT NULL," +
                                    "file_no INTEGER NOT NULL, file_date DATE NOT NULL, sender_phone CHAR(20) NOT NULL, sender_fio CHAR(80) NOT NULL, calc_date DATE NOT NULL, row_number INTEGER NOT NULL)";
            ret = CreateOneTable(conn_db, "file_head", "_data", fields);
            #endregion Проверка таблицы file_head

            #region 7 Проверка таблицы file_ipu
            fields = "(id SERIAL NOT NULL, nzp_file INTEGER NOT NULL, ls_id CHAR(20), nzp_serv INTEGER, rashod_type INTEGER, serv_type INTEGER, counter_type CHAR(25), cnt_stage INTEGER," +
                                    "mmnog INTEGER, num_cnt CHAR(20), dat_uchet DATE, val_cnt FLOAT, nzp_measure INTEGER, dat_prov DATE, dat_provnext DATE, nzp_kvar INTEGER, nzp_counter INTEGER, local_id CHAR(20)," +
                                    "kod_serv CHAR(20), doppar CHAR(25))";
            ret = CreateOneTable(conn_db, "file_ipu", "_data", fields);
            #endregion Проверка таблицы file_ipu

            #region 8 Проверка таблицы file_ipu_p
            fields = "(id SERIAL NOT NULL, nzp_file INTEGER, id_ipu CHAR(20), rashod_type INTEGER, dat_uchet DATE, val_cnt FLOAT, kod_serv INTEGER)";
            ret = CreateOneTable(conn_db, "file_ipu_p", "_data", fields);
            #endregion Проверка таблицы file_ipu_p

            #region 9 Проверка таблицы file_kvar
#if PG
            fields = "(id CHAR(20) NOT NULL, nzp_file INTEGER NOT NULL, ukas INTEGER," + " dom_id NUMERIC(18,0) NOT NULL, " +
            "ls_type INTEGER NOT NULL, fam VARCHAR(60), ima VARCHAR(60), otch VARCHAR(60), birth_date DATE," +
            "nkvar CHAR(10), nkvar_n CHAR(3), open_date DATE, opening_osnov CHAR(100), close_date DATE, closing_osnov CHAR(100), kol_gil INTEGER NOT NULL, kol_vrem_prib INTEGER NOT NULL," +
            "kol_vrem_ub INTEGER NOT NULL, room_number INTEGER NOT NULL, total_square NUMERIC(14,2) NOT NULL, living_square NUMERIC(14,2), otapl_square NUMERIC(14,2), naim_square NUMERIC(14,2)," +
            "is_communal INTEGER NOT NULL, is_el_plita INTEGER, is_gas_plita INTEGER, is_gas_colonka INTEGER, is_fire_plita INTEGER, gas_type INTEGER, water_type INTEGER, hotwater_type INTEGER," +
            "canalization_type INTEGER, is_open_otopl INTEGER, params CHAR(250), service_row_number INTEGER NOT NULL, reval_params_row_number INTEGER NOT NULL, ipu_row_number INTEGER NOT NULL," +
            "nzp_dom INTEGER, nzp_kvar INTEGER, comment CHAR(250), nzp_status INTEGER, id_urlic CHAR(20) )";
#else
                fields = "(id CHAR(20) NOT NULL, nzp_file INTEGER NOT NULL, ukas INTEGER," + " dom_id DECIMAL(18,0) NOT NULL, " +
 "ls_type INTEGER NOT NULL, fam CHAR(40), ima CHAR(40), otch CHAR(40), birth_date DATE," +
                                        "nkvar CHAR(10), nkvar_n CHAR(3), open_date DATE, opening_osnov CHAR(100), close_date DATE, closing_osnov CHAR(100), kol_gil INTEGER NOT NULL, kol_vrem_prib INTEGER NOT NULL," +
                                        "kol_vrem_ub INTEGER NOT NULL, room_number INTEGER NOT NULL, total_square DECIMAL(14,2) NOT NULL, living_square DECIMAL(14,2), otapl_square DECIMAL(14,2), naim_square DECIMAL(14,2)," +
                                        "is_communal INTEGER NOT NULL, is_el_plita INTEGER, is_gas_plita INTEGER, is_gas_colonka INTEGER, is_fire_plita INTEGER, gas_type INTEGER, water_type INTEGER, hotwater_type INTEGER," +
                                        "canalization_type INTEGER, is_open_otopl INTEGER, params CHAR(250), service_row_number INTEGER NOT NULL, reval_params_row_number INTEGER NOT NULL, ipu_row_number INTEGER NOT NULL," +
                                        "nzp_dom INTEGER, nzp_kvar INTEGER, comment CHAR(250), nzp_status INTEGER, id_urlic CHAR(20) )";
#endif
            ret = CreateOneTable(conn_db, "file_kvar", "_data", fields);
            #endregion Проверка таблицы file_kvar

            #region 10 Проверка таблицы file_kvarp
#if PG
            fields = "(id CHAR(20) NOT NULL, reval_month DATE, nzp_file INTEGER NOT NULL, fam VARCHAR(60), ima VARCHAR(60), otch VARCHAR(60), birth_date DATE, nkvar CHAR(10), nkvar_n CHAR(3), open_date DATE," +
                    "opening_osnov CHAR(100), close_date DATE, closing_osnov CHAR(100), kol_gil INTEGER NOT NULL, kol_vrem_prib INTEGER NOT NULL, kol_vrem_ub INTEGER NOT NULL, room_number INTEGER NOT NULL," +
                    "total_square NUMERIC(14,2) NOT NULL, living_square NUMERIC(14,2), otapl_square NUMERIC(14,2), naim_square NUMERIC(14,2), is_communal INTEGER NOT NULL, is_el_plita INTEGER, is_gas_plita INTEGER," +
                    "is_gas_colonka INTEGER, is_fire_plita INTEGER, gas_type INTEGER, water_type INTEGER, hotwater_type INTEGER, canalization_type INTEGER, is_open_otopl INTEGER, params CHAR(250), nzp_dom INTEGER," +
                    "nzp_kvar INTEGER, comment CHAR(250), nzp_status INTEGER, local_id CHAR(20))";
#else
                fields = "(id CHAR(20) NOT NULL, reval_month DATE, nzp_file INTEGER NOT NULL, fam CHAR(40), ima CHAR(40), otch CHAR(40), birth_date DATE, nkvar CHAR(10), nkvar_n CHAR(3), open_date DATE," +
                                        "opening_osnov CHAR(100), close_date DATE, closing_osnov CHAR(100), kol_gil INTEGER NOT NULL, kol_vrem_prib INTEGER NOT NULL, kol_vrem_ub INTEGER NOT NULL, room_number INTEGER NOT NULL," +
                                        "total_square DECIMAL(14,2) NOT NULL, living_square DECIMAL(14,2), otapl_square DECIMAL(14,2), naim_square DECIMAL(14,2), is_communal INTEGER NOT NULL, is_el_plita INTEGER, is_gas_plita INTEGER," +
                                        "is_gas_colonka INTEGER, is_fire_plita INTEGER, gas_type INTEGER, water_type INTEGER, hotwater_type INTEGER, canalization_type INTEGER, is_open_otopl INTEGER, params CHAR(250), nzp_dom INTEGER," +
                                        "nzp_kvar INTEGER, comment CHAR(250), nzp_status INTEGER, local_id CHAR(20))";
#endif
            ret = CreateOneTable(conn_db, "file_kvarp", "_data", fields);
            #endregion Проверка таблицы file_kvarp

            #region 11 Проверка таблицы file_measures
            fields = "(id SERIAL NOT NULL, id_measure INTEGER, measure CHAR(100), nzp_file INTEGER, nzp_measure INTEGER)";
            ret = CreateOneTable(conn_db, "file_measures", "_data", fields);
            #endregion Проверка таблицы file_measures

            #region 12 Проверка таблицы file_mo
#if PG
            fields = "(id SERIAL NOT NULL, id_mo INTEGER, vill CHARACTER(50), nzp_vill NUMERIC(13,0), nzp_raj INTEGER, nzp_file INTEGER, raj CHARACTER(60), mo_name CHARACTER(60))";
#else
                fields = "(id SERIAL NOT NULL, id_mo INTEGER, vill CHAR(50), nzp_vill DECIMAL(13,0), nzp_raj INTEGER, nzp_file INTEGER, raj CHAR(60), mo_name CHAR(60))";
#endif
            ret = CreateOneTable(conn_db, "file_mo", "_data", fields);
            #endregion Проверка таблицы file_mo

            #region 13 Проверка таблицы file_nedopost
#if PG
            fields = "(id SERIAL NOT NULL, nzp_file INTEGER, ls_id CHARACTER(20), id_serv CHAR(20), type_ned NUMERIC(10,0), temper INTEGER, dat_nedstart DATE, dat_nedstop DATE, sum_ned NUMERIC(10,2))";
#else
                fields = "(id SERIAL NOT NULL, nzp_file INTEGER, ls_id CHAR(20), id_serv CHAR(20), type_ned DECIMAL(10,0), temper INTEGER, dat_nedstart DATE, dat_nedstop DATE, sum_ned DECIMAL(10,2))";
#endif
            ret = CreateOneTable(conn_db, "file_nedopost", "_data", fields);
            #endregion Проверка таблицы file_nedopost

            #region 14 Проверка таблицы file_odpu
#if PG
            fields = "(id SERIAL NOT NULL, nzp_file INTEGER NOT NULL, dom_id NUMERIC(18,0), nzp_serv INTEGER, rashod_type INTEGER, serv_type INTEGER, counter_type CHAR(25), cnt_stage INTEGER," +
                    "mmnog INTEGER, num_cnt CHAR(20), dat_uchet DATE, val_cnt FLOAT, nzp_measure INTEGER, dat_prov DATE, dat_provnext DATE, nzp_dom INTEGER, nzp_counter INTEGER, local_id CHAR(20), doppar CHAR(25))";
#else
                fields = "(id SERIAL NOT NULL, nzp_file INTEGER NOT NULL, dom_id DECIMAL(18,0), nzp_serv INTEGER, rashod_type INTEGER, serv_type INTEGER, counter_type CHAR(25), cnt_stage INTEGER," +
                                        "mmnog INTEGER, num_cnt CHAR(20), dat_uchet DATE, val_cnt FLOAT, nzp_measure INTEGER, dat_prov DATE, dat_provnext DATE, nzp_dom INTEGER, nzp_counter INTEGER, local_id CHAR(20), doppar CHAR(25))";
#endif
            ret = CreateOneTable(conn_db, "file_odpu", "_data", fields);
            #endregion Проверка таблицы file_odpu

            #region 15 Проверка таблицы file_odpu_p
#if PG
            fields = "(id SERIAL NOT NULL, nzp_file INTEGER, id_odpu CHAR(20), rashod_type INTEGER, dat_uchet DATE, val_cnt FLOAT, id_ipu INTEGER, kod_serv NUMERIC(10,0))";
#else
                fields = "(id SERIAL NOT NULL, nzp_file INTEGER, id_odpu CHAR(20), rashod_type INTEGER, dat_uchet DATE, val_cnt FLOAT, id_ipu INTEGER, kod_serv DECIMAL(10,0))";
#endif
            ret = CreateOneTable(conn_db, "file_odpu_p", "_data", fields);
            #endregion Проверка таблицы file_odpu_p

            #region 16 Проверка таблицы file_oplats
#if PG
            fields = "(id SERIAL NOT NULL, ls_id CHAR(20), type_oper INTEGER, numplat CHAR(80), dat_opl DATE, dat_uchet DATE, dat_izm DATE, " +
                "sum_oplat NUMERIC(14,2), ist_opl CHAR(80), mes_oplat DATE, nzp_file INTEGER, nzp_pack INTEGER, id_serv INTEGER)";
#else
                fields = "(id SERIAL NOT NULL, ls_id CHAR(20), type_oper INTEGER, numplat CHAR(80), dat_opl DATE, dat_uchet DATE, dat_izm DATE, " +
                                    "sum_oplat DECIMAL(14,2), ist_opl CHAR(80), mes_oplat DATE, nzp_file INTEGER, nzp_pack INTEGER, id_serv INTEGER)";
#endif
            ret = CreateOneTable(conn_db, "file_oplats", "_data", fields);
            #endregion Проверка таблицы file_oplats

            #region 17 Проверка таблицы file_paramsdom
#if PG
            fields = "(id SERIAL NOT NULL, id_dom CHAR(20), id_prm INTEGER, val_prm CHAR(100), nzp_dom INTEGER, nzp_file INTEGER)";
#else
                fields = "(id SERIAL NOT NULL, id_dom CHAR(20), id_prm INTEGER, val_prm CHAR(100), nzp_dom INTEGER, nzp_file INTEGER)";
#endif
            ret = CreateOneTable(conn_db, "file_paramsdom", "_data", fields);
            #endregion Проверка таблицы file_paramsdom

            #region 18 Проверка таблицы file_paramsls
#if PG
            fields = "(id SERIAL NOT NULL, ls_id CHAR(20), id_prm INTEGER, val_prm CHAR(100), num_ls INTEGER, nzp_file INTEGER)";
#else
                fields = "(id SERIAL NOT NULL, ls_id CHAR(20), id_prm INTEGER, val_prm CHAR(100), num_ls INTEGER, nzp_file INTEGER)";
#endif
            ret = CreateOneTable(conn_db, "file_paramsls", "_data", fields);
            #endregion Проверка таблицы file_paramsls

            #region 19 Проверка таблицы file_pasp
#if PG
            fields = "(nzp_gil SERIAL NOT NULL, fam CHARACTER(20) NOT NULL, ima CHARACTER(20) NOT NULL, otch CHARACTER(20), dat_rog DATE NOT NULL, gender CHARACTER(1), prop CHAR(30), pr_lista CHAR(2)," +
                    "country CHARACTER(5), region CHARACTER(5), selo CHARACTER(30), ulica CHARACTER(40), ndom CHARACTER(10), nkor CHAR(3), nkvar CHARACTER(10), nkvar_n CHARACTER(3), doctype CHARACTER(30), serij CHARACTER(10), nomer CHARACTER(7)," +
                    "vid_dat DATE, rog_country CHARACTER(5), rog_region CHARACTER(30), rog_selo CHARACTER(30), dat_sost DATE, dat_ofor DATE)";
#else
                fields = "(nzp_gil SERIAL NOT NULL, fam NCHAR(20) NOT NULL, ima NCHAR(20) NOT NULL, otch NCHAR(20), dat_rog DATE NOT NULL, gender NCHAR(1), prop CHAR(30), pr_lista CHAR(2)," +
                                        "country CHAR(5), region CHAR(5), selo CHAR(30), ulica CHAR(40), ndom CHAR(10), nkor CHAR(3), nkvar CHAR(10), nkvar_n CHAR(3), doctype CHAR(30), serij CHAR(10), nomer CHAR(7)," +
                                        "vid_dat DATE, rog_country CHAR(5), rog_region CHAR(30), rog_selo CHAR(30), dat_sost DATE, dat_ofor DATE)";
#endif
            ret = CreateOneTable(conn_db, "file_pasp", "_data", fields);
            #endregion Проверка таблицы file_pasp

            #region 20 Проверка таблицы file_serv
#if PG
            fields = "(id SERIAL NOT NULL, nzp_file INTEGER NOT NULL, ls_id CHAR(20) NOT NULL, supp_id NUMERIC(18,0) NOT NULL, nzp_serv INTEGER NOT NULL, sum_insaldo NUMERIC(14,2) NOT NULL," +
                    "eot NUMERIC(14,3) NOT NULL, reg_tarif_percent NUMERIC(14,3) NOT NULL, reg_tarif NUMERIC(14,3) NOT NULL, nzp_measure INTEGER NOT NULL, fact_rashod NUMERIC(18,7) NOT NULL," +
                    "norm_rashod NUMERIC(18,7) NOT NULL, is_pu_calc INTEGER NOT NULL, sum_nach NUMERIC(14,2) NOT NULL, sum_reval NUMERIC(14,2) NOT NULL, sum_subsidy NUMERIC(14,2) NOT NULL," +
                    "sum_subsidyp NUMERIC(14,2) NOT NULL, sum_lgota NUMERIC(14,2) NOT NULL, sum_lgotap NUMERIC(14,2) NOT NULL, sum_smo NUMERIC(14,2) NOT NULL, sum_smop NUMERIC(14,2) NOT NULL," +
                    "sum_money NUMERIC(14,2) NOT NULL, is_del INTEGER NOT NULL, sum_outsaldo NUMERIC(14,2) NOT NULL, servp_row_number INTEGER NOT NULL, nzp_kvar INTEGER, nzp_supp INTEGER)";
#else
                fields = "(id SERIAL NOT NULL, nzp_file INTEGER NOT NULL, ls_id CHAR(20) NOT NULL, supp_id DECIMAL(18,0) NOT NULL, nzp_serv INTEGER NOT NULL, sum_insaldo DECIMAL(14,2) NOT NULL," +
                                        "eot DECIMAL(14,3) NOT NULL, reg_tarif_percent DECIMAL(14,3) NOT NULL, reg_tarif DECIMAL(14,3) NOT NULL, nzp_measure INTEGER NOT NULL, fact_rashod DECIMAL(18,7) NOT NULL," +
                                        "norm_rashod DECIMAL(18,7) NOT NULL, is_pu_calc INTEGER NOT NULL, sum_nach DECIMAL(14,2) NOT NULL, sum_reval DECIMAL(14,2) NOT NULL, sum_subsidy DECIMAL(14,2) NOT NULL," +
                                        "sum_subsidyp DECIMAL(14,2) NOT NULL, sum_lgota DECIMAL(14,2) NOT NULL, sum_lgotap DECIMAL(14,2) NOT NULL, sum_smo DECIMAL(14,2) NOT NULL, sum_smop DECIMAL(14,2) NOT NULL," +
                                        "sum_money DECIMAL(14,2) NOT NULL, is_del INTEGER NOT NULL, sum_outsaldo DECIMAL(14,2) NOT NULL, servp_row_number INTEGER NOT NULL, nzp_kvar INTEGER, nzp_supp INTEGER)";
#endif
            ret = CreateOneTable(conn_db, "file_serv", "_data", fields);
            #endregion Проверка таблицы file_serv

            #region 21 Проверка таблицы file_services
#if PG
            fields = "(id SERIAL NOT NULL, id_serv INTEGER, service CHAR(100), service2 CHAR(100), nzp_file INTEGER, nzp_measure INTEGER, ed_izmer CHAR(30), type_serv INTEGER, nzp_serv INTEGER)";
#else
                fields = "(id SERIAL NOT NULL, id_serv INTEGER, service CHAR(100), service2 CHAR(100), nzp_file INTEGER, nzp_measure INTEGER, ed_izmer CHAR(30), type_serv INTEGER, nzp_serv INTEGER)";
#endif
            ret = CreateOneTable(conn_db, "file_services", "_data", fields);
            #endregion Проверка таблицы file_services

            #region 22 Проверка таблицы file_servls
#if PG
            fields = "(id SERIAL NOT NULL, nzp_file INTEGER, ls_id NUMERIC(14,0), id_serv CHAR(100), dat_start DATE, dat_stop DATE, supp_id NUMERIC(14,0))";
#else
                fields = "(id SERIAL NOT NULL, nzp_file INTEGER, ls_id DECIMAL(14,0), id_serv CHAR(100), dat_start DATE, dat_stop DATE, supp_id DECIMAL(14,0))";
#endif
            ret = CreateOneTable(conn_db, "file_servls", "_data", fields);
            #endregion Проверка таблицы file_servls

            #region 23 Проверка таблицы file_servp
#if PG
            fields = "(id SERIAL NOT NULL, nzp_file INTEGER NOT NULL, reval_month DATE, ls_id CHAR(20) NOT NULL, supp_id NUMERIC(18,0) NOT NULL, nzp_serv INTEGER NOT NULL, eot NUMERIC(14,3) NOT NULL," +
                    "reg_tarif_percent NUMERIC(14,3) NOT NULL, reg_tarif NUMERIC(14,3) NOT NULL, nzp_measure INTEGER NOT NULL, fact_rashod NUMERIC(18,7) NOT NULL, norm_rashod NUMERIC(18,7) NOT NULL," +
                    "is_pu_calc INTEGER NOT NULL, sum_reval NUMERIC(14,2) NOT NULL, sum_subsidyp NUMERIC(14,2) NOT NULL, sum_lgotap NUMERIC(14,2) NOT NULL, sum_smop NUMERIC(14,2) NOT NULL," +
                    "nzp_kvar INTEGER, nzp_supp INTEGER)";
#else
                fields = "(id SERIAL NOT NULL, nzp_file INTEGER NOT NULL, reval_month DATE, ls_id CHAR(20) NOT NULL, supp_id DECIMAL(18,0) NOT NULL, nzp_serv INTEGER NOT NULL, eot DECIMAL(14,3) NOT NULL," +
                                        "reg_tarif_percent DECIMAL(14,3) NOT NULL, reg_tarif DECIMAL(14,3) NOT NULL, nzp_measure INTEGER NOT NULL, fact_rashod DECIMAL(18,7) NOT NULL, norm_rashod DECIMAL(18,7) NOT NULL," +
                                        "is_pu_calc INTEGER NOT NULL, sum_reval DECIMAL(14,2) NOT NULL, sum_subsidyp DECIMAL(14,2) NOT NULL, sum_lgotap DECIMAL(14,2) NOT NULL, sum_smop DECIMAL(14,2) NOT NULL," +
                                        "nzp_kvar INTEGER, nzp_supp INTEGER)";
#endif
            ret = CreateOneTable(conn_db, "file_servp", "_data", fields);
            #endregion Проверка таблицы file_servp

            #region 24 Проверка таблицы file_supp
#if PG
            fields = "(id SERIAL NOT NULL, nzp_file INTEGER NOT NULL, supp_id NUMERIC(18,0) NOT NULL, supp_name CHAR(25) NOT NULL, jur_address CHAR(100), fact_address CHAR(100), inn CHAR(12), kpp CHAR(9)," +
                    "rs CHAR(20), bank CHAR(100), bik CHAR(20), ks CHAR(20), nzp_supp INTEGER)";
#else
                fields = "(id SERIAL NOT NULL, nzp_file INTEGER NOT NULL, supp_id DECIMAL(18,0) NOT NULL, supp_name CHAR(25) NOT NULL, jur_address CHAR(100), fact_address CHAR(100), inn CHAR(12), kpp CHAR(9)," +
                                        "rs CHAR(20), bank CHAR(100), bik CHAR(20), ks CHAR(20), nzp_supp INTEGER)";
#endif
            ret = CreateOneTable(conn_db, "file_supp", "_data", fields);
            #endregion Проверка таблицы  file_supp

            #region 25 Проверка таблицы file_typenedopost
#if PG
            fields = "(id SERIAL NOT NULL, nzp_file INTEGER, type_ned NUMERIC(10,0), ned_name CHAR(100))";
#else
                fields = "(id SERIAL NOT NULL, nzp_file INTEGER, type_ned DECIMAL(10,0), ned_name CHAR(100))";
#endif
            ret = CreateOneTable(conn_db, "file_typenedopost", "_data", fields);
            #endregion Проверка таблицы file_typenedopost

            #region 26 Проверка таблицы file_typeparams
#if PG
            fields = "(id SERIAL NOT NULL, id_prm INTEGER, prm_name CHAR(100), level_ INTEGER, type_prm INTEGER, nzp_file INTEGER, nzp_prm INTEGER)";
#else
                fields = "(id SERIAL NOT NULL, id_prm INTEGER, prm_name CHAR(100), level_ INTEGER, type_prm INTEGER, nzp_file INTEGER, nzp_prm INTEGER)";
#endif
            ret = CreateOneTable(conn_db, "file_typeparams", "_data", fields);
            #endregion Проверка таблицы  file_typeparams

            #region 27 Проверка таблицы file_urlic
#if PG
            fields = "(id SERIAL NOT NULL, nzp_file INTEGER NOT NULL, supp_id NUMERIC(18,0) NOT NULL, supp_name CHAR(100) NOT NULL, jur_address CHAR(100), fact_address CHAR(100)," +
                 " inn CHAR(12), kpp CHAR(9), rs CHAR(20), bank CHAR(100), bik_bank CHAR(20), ks CHAR(20), tel_chief CHAR(20), tel_b CHAR(20), chief_name CHAR(100)," +
                 " chief_post CHAR(40), b_name CHAR(100), okonh1 CHAR(20), okonh2 CHAR(20), okpo CHAR(20), bank_pr CHAR(100), bank_adr CHAR(100), bik CHAR(20)," +
                 " rs_pr CHAR(20), ks_pr CHAR(20), post_and_name CHAR(200), nzp_supp INTEGER)";
#else
                fields = "(id SERIAL NOT NULL, nzp_file INTEGER NOT NULL, supp_id DECIMAL(18,0) NOT NULL, supp_name CHAR(100) NOT NULL, jur_address CHAR(100), fact_address CHAR(100)," +
                                    " inn CHAR(12), kpp CHAR(9), rs CHAR(20), bank CHAR(100), bik_bank CHAR(20), ks CHAR(20), tel_chief CHAR(20), tel_b CHAR(20), chief_name CHAR(100)," +
                                    " chief_post CHAR(40), b_name CHAR(100), okonh1 CHAR(20), okonh2 CHAR(20), okpo CHAR(20), bank_pr CHAR(100), bank_adr CHAR(100), bik CHAR(20)," +
                                    " rs_pr CHAR(20), ks_pr CHAR(20), post_and_name CHAR(200), nzp_supp INTEGER)";
#endif
            ret = CreateOneTable(conn_db, "file_urlic", "_data", fields);
            #endregion Проверка таблицы  file_urlic

            #region 28 Проверка таблицы file_voda
#if PG
            fields = "(id SERIAL NOT NULL, id_prm INTEGER, name CHAR(100), nzp_file INTEGER, nzp_prm INTEGER)";
#else
                fields = "(id SERIAL NOT NULL, id_prm INTEGER, name CHAR(100), nzp_file INTEGER, nzp_prm INTEGER)";
#endif
            ret = CreateOneTable(conn_db, "file_voda", "_data", fields);
            #endregion Проверка таблицы  file_voda

            #region 29 Проверка таблицы file_vrub
#if PG
            fields = "(id SERIAL NOT NULL, nzp_file INTEGER, ls_id CHAR(20),  gil_id INTEGER , dat_vrvib DATE, dat_end DATE)";
#else
                fields = "(id SERIAL NOT NULL, nzp_file INTEGER, ls_id CHAR(20),  gil_id INTEGER , dat_vrvib DATE, dat_end DATE)";
#endif
            ret = CreateOneTable(conn_db, "file_vrub", "_data", fields);
            #endregion Проверка таблицы file_vrub

            #region 30 Проверка таблицы files_imported
#if PG
            fields = "(nzp_file SERIAL NOT NULL, nzp_version INTEGER NOT NULL, loaded_name CHAR(90), saved_name CHAR(90), nzp_status INTEGER NOT NULL, created_by INTEGER NOT NULL," +
                    "created_on timestamp without time zone NOT NULL NOT NULL, file_type INTEGER, nzp_exc INTEGER, nzp_exc_log INTEGER)";
#else
                fields = "(nzp_file SERIAL NOT NULL, nzp_version INTEGER NOT NULL, loaded_name CHAR(90), saved_name CHAR(90), nzp_status INTEGER NOT NULL, created_by INTEGER NOT NULL," +
                                        "created_on DATETIME YEAR to SECOND NOT NULL, file_type INTEGER, nzp_exc INTEGER, nzp_exc_log INTEGER)";
#endif
            ret = CreateOneTable(conn_db, "files_imported", "_data", fields);
            #endregion Проверка таблицы  files_imported

            #region 31 Проверка таблицы file_blag
#if PG
            fields = "(id SERIAL NOT NULL, id_prm INTEGER, name CHAR(100), nzp_file INTEGER, nzp_prm INTEGER)";
#else
                fields = "(id SERIAL NOT NULL, id_prm INTEGER, name CHAR(100), nzp_file INTEGER, nzp_prm INTEGER)";
#endif
            ret = CreateOneTable(conn_db, "file_blag", "_data", fields);
            #endregion Проверка таблицы  file_blag

            #region 32 Проверка таблицы file_uchs
            fields = "( uch INTEGER, geu CHAR(50), iddom CHAR(15), nzp_dom INTEGER, nzp_geu INTEGER)";
            ret = CreateOneTable(conn_db, "file_uchs", "_data", fields);
            #endregion Проверка таблицы  file_uchs

            #region 33 Проверка таблицы file_ulica
#if PG
            fields = "(file_ulica_id VARCHAR(100), file_ulica_street VARCHAR(100), nzp_ul INTEGER, f_ulica VARCHAR(100))";
#else
                fields = "(file_ulica_id VARCHAR(30), file_ulica_street VARCHAR(30), nzp_ul INTEGER, f_ulica CHAR(60))";
#endif
            ret = CreateOneTable(conn_db, "file_ulica", "_data", fields);
            #endregion Проверка таблицы  file_ulica

            #region 34 Проверка таблицы file_section
#if PG
            fields = "(id SERIAL NOT NULL, num_sec INTEGER, sec_name CHAR(100), nzp_file INTEGER, is_need_load INTEGER default 1)";
#else
                fields = "(id SERIAL NOT NULL, num_sec INTEGER, sec_name CHAR(100), nzp_file INTEGER, is_need_load INTEGER default 1)";
#endif
            ret = CreateOneTable(conn_db, "file_section ", "_data", fields);
            #endregion Проверка таблицы  file_section

            #region 35 Проверка таблицы file_serv_tuning
#if PG
            fields = " (   id SERIAL NOT NULL,   nzp_serv INTEGER,   nzp_supp INTEGER,   nzp_measure INTEGER,   nzp_frm INTEGER) ";
#else
                fields = " (   id SERIAL NOT NULL,   nzp_serv INTEGER,   nzp_supp INTEGER,   nzp_measure INTEGER,   nzp_frm INTEGER) ";
#endif
            ret = CreateOneTable(conn_db, "file_serv_tuning ", "_data", fields);
            #endregion 35 Проверка таблицы file_serv_tuning

            #region 36 Проверка таблицы file_sql
#if PG
            fields = " (id integer NOT NULL, nzp_file integer, sql_zapr CHAR(2000)) ";
#else
                fields = " (id integer NOT NULL, nzp_file integer, sql_zapr CHAR(2000)) ";
#endif
            ret = CreateOneTable(conn_db, "file_sql ", "_data", fields);
            #endregion 36 Проверка таблицы file_sql

            #region 36 Проверка таблицы upload_progress
#if PG
            fields = " (id SERIAL NOT NULL, date_upload timestamp without time zone, progress NUMERIC(14, 2), upload_type INTEGER) ";
#else
                fields = " (id SERIAL NOT NULL, date_upload DATETIME YEAR to SECOND, progress DECIMAL(14,2), upload_type INTEGER) ";
#endif
            ret = CreateOneTable(conn_db, "upload_progress", "_data", fields);
            #endregion 36 Проверка таблицы upload_progress

            #region 37 Проверка таблицы file_del_unrel_info
#if PG
            fields = " (id SERIAL NOT NULL, nzp_file INTEGER NOT NULL, is_success INTEGER NOT NULL) ";
#else
                fields = " (id SERIAL NOT NULL, nzp_file INTEGER NOT NULL, is_success INTEGER NOT NULL) ";
#endif
            ret = CreateOneTable(conn_db, "file_del_unrel_info", "_data", fields);
            #endregion 37 Проверка таблицы file_del_unrel_info

        }
        catch
        {
            ret.result = false;
        }
        return ret;
    }

    /// <summary>
    /// ПРоверка наличия и создание одной таблички
    /// </summary>
    /// <param name="conn_db">подключение к БД</param>
    /// <param name="table_name">название таблицы</param>
    /// <param name="dbname_withou_pref">название БД без префикса</param>
    /// <param name="fields">поля создаваемой таблички в круглых скобках как в SQL-запросе</param>
    /// <returns></returns>
    private Returns CreateOneTable(IDbConnection conn_db, string tablename, string dbname_withoupref, string fields)
    {
        Returns ret = Utils.InitReturns();
        string table_name = tablename.Trim();
        string dbname = dbname_withoupref.Trim();

        try
        {
            #region Проверка таблицы
#if PG
            string sql = 
                "select table_name as tabname "+
                " from information_schema.tables "+
                " where table_schema ='" + Points.Pref + dbname + "' and table_name ='" + table_name + "'";
#else
            string sql = 
            " select * from " + Points.Pref + dbname + tableDelimiter + "systables a "+
            " where a.tabname = '" + table_name + "' and a.tabid > 99";
#endif
            var dt = ClassDBUtils.OpenSQL(sql, conn_db);
            if (dt.resultData.Rows.Count == 0)
            {
#if PG
                sql = " SET search_path TO  '" + Points.Pref + dbname + "'";
#else
                    sql = "DATABASE " + Points.Pref + dbname + "; ";
#endif
                ret = ExecSQL(conn_db, sql, true);
                if (ret.result)
                {
                    sql = "CREATE TABLE " + table_name + fields;
                    ret = ExecSQL(conn_db, sql, true);
                }
            }
            #endregion Проверка таблицы
        }
        catch (Exception ex)
        {
            MonitorLog.WriteLog("Ошибка создания таблицы " + table_name + " в функцие CreateOneTable : " + ex.Message + ex.StackTrace, MonitorLog.typelog.Error, true);
            ret.result = false;
            return ret;
        }

        return ret;
    }

        //todo засунуть внутрь класса, conn и выбранные секции
    private Returns SectionsToDB(IDbConnection conn_db, FilesImported finder)
    {
        Returns ret = Utils.InitReturns();

        try
        {
            for (int i = 1; i < finder.sections.Length; i++)
            {
                string sql =
                    " insert into " + Points.Pref + "_data" + tableDelimiter + "file_section " +
                    " ( num_sec, sec_name, nzp_file, is_need_load)" +
                    " values( " + i + ", null, " + finder.nzp_file + ", " + Convert.ToInt32(finder.sections[i]) + " )";
                ret = ExecSQL(conn_db, sql, true);
            }
        }
        catch (Exception ex)
        {
            MonitorLog.WriteLog("Ошибка функции SectionsToDB"+ex.Message+"\n"+ex.StackTrace, MonitorLog.typelog.Error, true);
        }
        return ret;
    }


    /// <summary>
    /// Смена статуса задания
    /// </summary>
    /// <param name="finder"></param>
    /// <returns></returns>
    public Returns SetMyFileState(ExcelUtility finder)
    {
        IDbConnection conn_db = GetConnection(Constants.cons_Webdata);
        Returns ret = OpenDb(conn_db, true);
        if (!ret.result) return ret;

        StringBuilder sql = new StringBuilder();
        sql.Append(" update " + sPublicForMDY + "excel_utility set stats = " + (int)finder.status);
        if (finder.status == ExcelUtility.Statuses.InProcess)
        {
            sql.Append(", dat_start = " + Utils.EStrNull(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")));
        }
        else if (finder.status == ExcelUtility.Statuses.Success || finder.status == ExcelUtility.Statuses.Failed)
        {
            sql.Append(", dat_out = " + Utils.EStrNull(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")));
        }
        if (finder.exc_path != "") sql.Append(", exc_path = " + Utils.EStrNull(finder.exc_path));
        sql.Append(" where nzp_exc =" + finder.nzp_exc);

        ret = ExecSQL(conn_db, sql.ToString(), true);

        conn_db.Close();
        sql.Remove(0, sql.Length);

        return ret;
    }

    
    //проверки на число, дату, текст
    private string CheckInt(string s_int, bool not_null, Int64? low, Int64? high, ref Returns ret)
    {
        string temp = ret.text;
        ret = Utils.InitReturns();
        ret.text = temp;

        Int64 i_int = 0;

        if (s_int == "")
        {
            if (not_null)
            {
                ret.result = false;
                ret.text = "Не заполнено обязательное числовое поле: ";
                return "";
            }
            else
            {
                return "null";
            }
        }


        if (!Int64.TryParse(s_int, out i_int))
        {
            ret.result = false;
            ret.text = "Поле имеет неверный формат. Значение = " + s_int + ", имя поля: ";
            return "";
        }

        if (low.HasValue)
        {
            if (i_int < low.Value)
            {
                ret.result = false;
                ret.text = "Поле имеет неверное значение(меньше " + low.Value + "). Значение = " + i_int + ", имя поля: ";
                return "";
            }
        }

        if (high.HasValue)
        {
            if (i_int > high.Value)
            {
                ret.result = false;
                ret.text = "Поле имеет неверное значение(превышает " + high.Value + "). Значение = " + i_int + ", имя поле: ";
                return "";
            }
        }

        return i_int.ToString();
    }


    /// <summary>
    /// Проверка на Число
    /// </summary>
    /// <param name="s_decimal">число в строковом представлении</param>
    /// <param name="not_null">Обязательность поля</param>
    /// <param name="ret">ошибки</param>
    /// <returns>Число</returns>
    private string CheckDecimal(string s_decimal, bool not_null, bool is_money, decimal? low, decimal? high, ref Returns ret, bool is_gubkin = false)
    {
        string temp = ret.text;
        ret = Utils.InitReturns();

        ret.text = temp;

        s_decimal = s_decimal.Replace(",", ".");

        decimal d_decimal = 0;

        if (s_decimal == "")
        {
            if (not_null)
            {
                ret.result = false;
                ret.text = "Не заполнено обязательное числовое поле: ";
                return "";
            }
            else
            {
                return "null";
            }
        }


        if (!Decimal.TryParse(s_decimal, out d_decimal))
        {
            if (is_gubkin)
            {
                if (Decimal.TryParse(s_decimal.Replace("E-01", ""), out d_decimal))
                {
                    d_decimal = d_decimal / 10;
                }
                else
                {
                    if (Decimal.TryParse(s_decimal.Replace("E-02", ""), out d_decimal))
                    {
                        d_decimal = d_decimal / 100;
                    }
                    else
                    {
                        if (Decimal.TryParse(s_decimal.Replace("E-03", ""), out d_decimal))
                        {
                            d_decimal = d_decimal / 1000;
                        }
                        else
                        {
                            if (s_decimal.Contains("E-") || s_decimal.Contains("e-"))
                            {
                                d_decimal = 0;
                                return d_decimal.ToString();
                            }
                            else
                            {
                                ret.result = false;
                                ret.text = "Поле имеет неверный формат. Значение = " + s_decimal + ", имя поля: ";
                                return "";

                            }
                        }
                    }
                }
            }
            else
            {
                if (s_decimal.Contains("E-") || s_decimal.Contains("e-"))
                {
                    d_decimal = 0;
                }
                else
                {
                    ret.result = false;
                    ret.text = "Поле имеет неверный формат. Значение = " + s_decimal + ", имя поля: ";
                    return "";
                }
            }
        }


        if (is_money && Math.Abs(d_decimal - decimal.Truncate(d_decimal)).ToString().Length > 4)
        {
            ret.result = false;
            ret.text = "Поле имеет неверный формат(дробная часть превышает 2 знака). Значение = " + s_decimal + ", имя поля: ";
            return "";
        }

        if (!is_money && Math.Abs(d_decimal - decimal.Truncate(d_decimal)).ToString().Length > 20)
        {
            ret.result = false;
            ret.text = "Поле имеет неверный формат(дробная часть превышает 20 знаков). Значение = " + s_decimal + " имя поля: ";
            return "";
        }

        if (low.HasValue)
        {
            if (d_decimal < low.Value)
            {
                ret.result = false;
                ret.text = "Поле имеет неверное значение(меньше " + low.Value + "). Значение = " + d_decimal + ", имя поля: ";
                return "";
            }
        }

        if (high.HasValue)
        {
            if (d_decimal > high.Value)
            {
                ret.result = false;
                ret.text = "Поле имеет неверное значение(превышает " + high.Value + "). Значение = " + d_decimal + ", имя поля: ";
                return "";
            }
        }

        return d_decimal.ToString();
    }


    /// <summary>
    /// Проверка не дату 
    /// </summary>
    /// <param name="s_date">Строковое представление даты</param>
    /// <param name="not_null">Обязательное</param>
    /// <param name="ret">Ошибки</param>
    /// <returns>Дата в строковом представшлении</returns>
    private string CheckDateTime(string s_date, bool not_null, ref Returns ret)
    {
        string temp = ret.text;
        ret = Utils.InitReturns();
        ret.text = temp;
        DateTime d_date = new DateTime();

        if (s_date == "")
        {
            if (not_null)
            {
                ret.result = false;
                ret.text = "Не заполнено обязательное поле даты: ";
                return "";
            }
            else
            {
                return "null";
            }
        }


        if (!DateTime.TryParse(s_date, out d_date))
        {
            ret.result = false;
            ret.text = "Поле имеет неверный формат. Значение = " + s_date + ", имя поля: ";
            return "";
        }


        return "\'" + d_date.ToShortDateString() + "\'";
    }

    /// <summary>
    /// Проверка текста
    /// </summary>
    /// <param name="s_text"></param>
    /// <param name="not_null"></param>
    /// <param name="ln"></param>
    /// <param name="ret"></param>
    /// <returns></returns>
    private string CheckText(string s_text, bool not_null, int ln, ref Returns ret)
    {
        string temp = ret.text;
        ret = Utils.InitReturns();
        ret.text = temp;

        s_text = s_text.Replace(",", ".");
        s_text = s_text.Replace("«", "\"");
        s_text = s_text.Replace("»", "\"");
        s_text = s_text.Replace("'", "\"");
        s_text = s_text.Trim();

        if (s_text == "")
        {
            if (not_null)
            {
                ret.result = false;
                ret.text = "Не заполнено обязательное числовое поле: ";
                return "";
            }
            else
            {
                return "null";
            }
        }


        if (s_text.Length > ln)
        {
            ret.result = false;
            ret.text = "Длина текста превышает заданный формат (" + ln + ") поля: ";
            return "";
        }

        return "\'" + s_text.Trim() + "\'";

    }


    //проверка уникальности данных
    private Returns CheckUnique(IDbConnection conn_db, FilesImported finder, StringBuilder err)
    {
        Returns ret = Utils.InitReturns();
        try
        {

            #region 2 file_area
            ret = CheckOneUnique(conn_db, finder, err, "file_area", "id", " управляющие компании ");
            #endregion

            #region 3 file_dom
            ret = CheckOneUnique(conn_db, finder, err, "file_dom", "id", "дома");
            #endregion

            #region 4 file_kvar
            ret = CheckOneUnique(conn_db, finder, err, "file_kvar", "id", "квартиры");
            #endregion

            #region 5 file_supp
            ret = CheckOneUnique(conn_db, finder, err, "file_supp", "supp_id", "поставщики");
            #endregion

            #region 9 file_odpu
            ret = CheckOneUnique(conn_db, finder, err, "file_odpu", "local_id", "ОДПУ");
            #endregion

            #region 11 file_ipu
            ret = CheckOneUnique(conn_db, finder, err, "file_ipu", "local_id", "ИПУ");
            #endregion

            #region 14 file_mo
            ret = CheckOneUnique(conn_db, finder, err, "file_mo", "id_mo", "МО");
            #endregion

            #region 16 file_typeparams
            ret = CheckOneUnique(conn_db, finder, err, "file_typeparams", "id_prm", "выгруженные параметры");
            #endregion

            #region 17 file_gaz
            ret = CheckOneUnique(conn_db, finder, err, "file_gaz", "id_prm", "выгруженные типы домов по газоснабжению");
            #endregion

            #region 18 file_voda
            ret = CheckOneUnique(conn_db, finder, err, "file_voda", "id_prm", "выгруженные типы домов по водоснабжению");
            #endregion

            #region 19 file_blag
            ret = CheckOneUnique(conn_db, finder, err, "file_blag", "id_prm", "выгруженные категории благоустройства");
            #endregion

            #region 24 file_typenedopost
            ret = CheckOneUnique(conn_db, finder, err, "file_typenedopost", "type_ned", "типы недопоставок");
            #endregion

            #region 26 file_pack
            ret = CheckOneUnique(conn_db, finder, err, "file_pack", "id", "пачки реестров");
            #endregion

            #region 27 file_urlic
            ret = CheckOneUnique(conn_db, finder, err, "file_urlic", "supp_id", "юридические лица");
            #endregion



        }
        catch
        {
            err.Append("Ошибка при проверке уникальности строк в функцие CheckUnique " + Environment.NewLine);
        }
        return ret;
    }

    private Returns CheckOneUnique(IDbConnection conn_db, FilesImported finder, StringBuilder err, string table_name, string id_name, string errField)
    {
        Returns ret = Utils.InitReturns();
        try
        {
            string sql;
#if PG
            sql = "set search_path to '" + Points.Pref + "_data'";
#else
                sql = "database " + Points.Pref + "_data";
#endif
            ret = ExecSQL(conn_db, sql, true);
            sql = "drop table " + Points.Pref + "_data" + tableDelimiter + "t_unique";
            ret = ExecSQL(conn_db, sql, false);
            sql = "select " + id_name + " as id_name, count(*) as kol " +
#if PG
 " into unlogged t_unique " +
#else
#endif
 "from " + Points.Pref + "_data" + tableDelimiter + "" + table_name +
                " where nzp_file = " + finder.nzp_file +
                " group by 1" +
                " having count(*)>1 " +
#if PG
#else
                    " into temp t_unique "+
#endif
 "";
            ret = ExecSQL(conn_db, sql, true);
            sql = "select * from " + Points.Pref + "_data" + tableDelimiter + "t_unique";
            var dt = ClassDBUtils.OpenSQL(sql, conn_db);
            if (Convert.ToInt32(dt.resultData.Rows.Count) > 0)
            {
                err.Append("Обнаружена ошибка входных данных. Имеются " + errField + " с одинаковым уникальным номером в количестве " + Convert.ToInt32(dt.resultData.Rows.Count) + "." + Environment.NewLine);
                err.Append(String.Format("{0,30}|{1,30}|{2}", "Уникальный код", "Количество строк", Environment.NewLine));

                foreach (DataRow rr in dt.GetData().Rows)
                {
                    string testMePls = String.Format("{0,30}|{1,30}|{2}", rr["id_name"].ToString().Trim(), rr["kol"].ToString().Trim(), Environment.NewLine);
                    err.Append(testMePls);
                }

                if (table_name.Trim() == "file_kvar")
                {
                    sql = "update " + Points.Pref + "_data" + tableDelimiter + "file_kvar set nzp_status = 1 where id in (select id_name from " + Points.Pref + "_data" + tableDelimiter + "t_unique) and nzp_file =" + finder.nzp_file;
                    ret = ExecSQL(conn_db, sql, true);
                }
            }
        }
        catch
        {
            err.Append("Ошибка при проверке уникальности строк в функцие CheckOneUnique " + Environment.NewLine);
        }
        return ret;
    }


    #region Функция проверки связности загружаемых таблиц
    /// <summary>
    /// Проверка связности БД
    /// </summary>
    /// <returns></returns>
    public Returns CheckRelation(IDbConnection conn_db, FilesImported finder, StringBuilder err)
    {
        Returns ret = Utils.InitReturns();
        try
        {
            #region 1. Выбираем квартиры без домов

            CheckOneRelation(conn_db, finder, err, "file_kvar", "id", "file_dom", "id", "dom_id", "id", "id", "Номер ЛС", "Уникальный номер дома", Points.Pref.ToString(), "квартиры без домов");
            #endregion Выбираем квартиры без домов

            #region 2. Выбираем услуги без квартир

            CheckOneRelation(conn_db, finder, err, "file_serv", "ls_id, nzp_serv, nzp_measure, id", "file_kvar", "id", "ls_id", "id", "nzp_serv", "Код услуги", "Номер ЛС", Points.Pref.ToString(), "услуги без квартир");
            #endregion Выбираем услуги без квартир

            #region 3. Выбираем жильцов без квартир

            CheckOneRelation(conn_db, finder, err, "file_gilec", "num_ls, id", "file_kvar", "id", "num_ls", "id", "nzp_gil", "Уникальный номер гражданина", "Номер ЛС", Points.Pref.ToString(), "жильцы без квартир");
            #endregion Выбираем жильцов без квартир

            #region 4. Выбираем ИПУ без квартир

            CheckOneRelation(conn_db, finder, err, "file_ipu", "local_id, id", "file_kvar", "id", "ls_id", "id", "num_cnt", "Заводской номер ПУ", "Номер ЛС", Points.Pref.ToString(), "ИПУ без квартир");
            #endregion Выбираем ИПУ без квартир

            #region 5. Выбираем показания ИПУ без ИПУ

            CheckOneRelation(conn_db, finder, err, "file_ipu_p", "id_ipu, id", "file_ipu", "local_id, id", "id_ipu", "local_id", "dat_uchet", "Дата показания", "уникальный код ПУ", Points.Pref.ToString(), "показания ИПУ без ИПУ");
            #endregion Выбираем пересчет ИПУ без ИПУ

            #region 6. Выбираем параметры квартиры без квартиры

            CheckOneRelation(conn_db, finder, err, "file_kvarp", "id", "file_kvar", "id", "nzp_kvar", "id", "reval_month", "Дата перерасчета", "Номер ЛС", Points.Pref.ToString(), "перерасчеты квартиры без квартир");
            #endregion Выбираем параметры квартиры без квартиры

            #region 7. Выбираем услуги без единицы измерения

            CheckOneRelation(conn_db, finder, err, "file_serv", "ls_id, nzp_serv, nzp_measure, id", "file_measures", "id_measure, id", "nzp_measure", "id_measure", "nzp_serv", "Код услуги", "Единица измерения", Points.Pref.ToString(), "услуги без единицы измерения");
            #endregion Выбираем услуги без единицы измерения

            #region 8. Выбираем дома без МО

            CheckOneRelation(conn_db, finder, err, "file_dom", "id", "file_mo", "id_mo, id", "mo_id", "id_mo", "id", "Уникальный код дома", "Уникальный код МО", Points.Pref.ToString(), "дома без МО");
            #endregion Выбираем дома без МО

            #region 9. Выбираем ОДПУ без дома

            CheckOneRelation(conn_db, finder, err, "file_odpu", "local_id, id", "file_dom", "id", "dom_id", "id", "num_cnt", "Заводской номер ПУ", "Уникальный код дома", Points.Pref.ToString(), "ОДПУ без домов");
            #endregion Выбираем общедомовые приборы учета без дома

            #region 10. Выбираем ОДПУ без единиц измерения

            CheckOneRelation(conn_db, finder, err, "file_odpu", "local_id, id", "file_measures", "id_measure, id", "nzp_measure", "id_measure", "num_cnt", "Заводской номер ПУ", "Код единицы измерения", Points.Pref.ToString(), "ОДПУ без единиц измерения");
            #endregion Выбираем ОДПУ без единиц измерения

            #region 11. Выбираем оплаты без квартир

            CheckOneRelation(conn_db, finder, err, "file_oplats", "ls_id, id", "file_kvar", "id", "ls_id", "id", "numplat", "Номер платежного документа", "Номер ЛС", Points.Pref.ToString(), "оплаты без квартир");
            #endregion Выбираем оплаты без квартир

            #region 12. Выбираем параметры дома без дома

            CheckOneRelation(conn_db, finder, err, "file_paramsdom", "id_dom, id", "file_dom", "id", "id_dom", "id", "id_prm", "Код параметра", "Уникальный код дома", Points.Pref.ToString(), "параметры дома без дома");
            #endregion Выбираем параметры дома без дома

            #region 13. Выбираем параметры ЛС без квартиры

            CheckOneRelation(conn_db, finder, err, "file_paramsls", "ls_id, id", "file_kvar", "id", "ls_id", "id", "id_prm", "Код параметра", "Номер ЛС", Points.Pref.ToString(), "параметры ЛС без квартир");
            #endregion Выбираем параметры ЛС без квартиры

            #region 14. Выбираем параметры услуг без квартир

            CheckOneRelation(conn_db, finder, err, "file_servp", "ls_id, id", "file_kvar", "id", "ls_id", "id", "reval_month", "Дата перерасчета", "Номер ЛС", Points.Pref.ToString(), "параметры услуг без квартир");
            #endregion Выбираем параметры услуг без квартир

            #region 15. Выбираем параметры услуг без поставщиков

            CheckOneRelation(conn_db, finder, err, "file_servp", "ls_id, id", "file_supp", "supp_id, id", "supp_id", "supp_id", "reval_month", "Дата перерасчета", "Уникальный код поставщика", Points.Pref.ToString(), "параметры услуг без поставщиков");
            #endregion Выбираем параметры услуг без поставщиков

            #region 16. Выбираем недопоставки без квартир

            CheckOneRelation(conn_db, finder, err, "file_nedopost", "type_ned, id", "file_kvar", "id", "ls_id", "id", "type_ned", "Тип недопоставки", "Номер ЛС", Points.Pref.ToString(), "недопоставки без квартир");
            #endregion Выбираем недопоставки без квартир

            #region 17. Выбираем недопоставки без услуги

            CheckOneRelation(conn_db, finder, err, "file_nedopost", "type_ned, id", "file_services", "id_serv,id", "id_serv", "id_serv", "type_ned", "Тип недопоставки", "Код услуги", Points.Pref.ToString(), "недопоставки без услуг");
            #endregion Выбираем недопоставки без услуги

            #region 18. Выбираем недопоставки без типа недопоставки

            CheckOneRelation(conn_db, finder, err, "file_nedopost", "type_ned, id", "file_typenedopost", "type_ned, id", "type_ned", "type_ned", "dat_nedstart",
                                                    "Дата начала недопоставки", "Тип недопоставки", Points.Pref.ToString(), "недопоставки без типа недопоставки");
            #endregion Выбираем недопоставки без типа недопоставки

            #region 19. Выбираем временно убывших без квартир

            CheckOneRelation(conn_db, finder, err, "file_vrub", "ls_id, id", "file_kvar", "id", "ls_id", "id", "gil_id", "Уникальный код гражданина", "Номер ЛС", Points.Pref.ToString(), "временно убывшие без квартир");
            #endregion Выбираем временно убывших без квартир

            #region 20. Выбираем услуги без поставщиков

            CheckOneRelation(conn_db, finder, err, "file_serv", "ls_id, nzp_serv, nzp_measure, id", "file_supp", "supp_id, id", "supp_id", "supp_id", "nzp_serv", "Код услуги", "Код поставщика", Points.Pref.ToString(), "услуги без поставщиков");
            #endregion Выбираем услуги без поставщиков

            #region 21. Выбираем показания ОДПУ без ОДПУ

            CheckOneRelation(conn_db, finder, err, "file_odpu_p", "id_odpu, id", "file_odpu", "local_id, id", "id_odpu", "local_id", "dat_uchet", "Дата учета", "Код ОДПУ", Points.Pref.ToString(), "показания ОДПУ без ОДПУ");
            #endregion Выбираем показания ОДПУ без ОДПУ

            #region 22. Выбираем дома без УК

            CheckOneRelation(conn_db, finder, err, "file_dom", "id", "file_area", "id", "area_id", "id", "id", "Уникальный код дома", "Уникальный код УК", Points.Pref.ToString(), "дома без УК");
            #endregion Выбираем дома без УК

            #region 23. Выбираем ОДПУ без кода услуги

            CheckOneRelation(conn_db, finder, err, "file_odpu", "local_id, id", "file_services", "id_serv,  id", "nzp_serv", "id_serv", "num_cnt", "Заводской номер ПУ", "Код услуги", Points.Pref.ToString(), "ОДПУ без кода услуги");
            #endregion Выбираем общедомовые приборы учета без кода услуги

            #region 24. Выбираем услуги, не входящие в перечень выгруженных услуг

            CheckOneRelation(conn_db, finder, err, "file_serv", "ls_id, nzp_serv, nzp_measure, id", "file_services", "id_serv,  id", "nzp_serv", "id_serv",
                "nzp_serv", "Код услуги", "Код услуги", Points.Pref.ToString(), "услуги, не входящие в перечень выгруженных услуг, ");
            #endregion Выбираем услуги без поставщиков

            #region 23. Выбираем ИПУ без кода услуги

            CheckOneRelation(conn_db, finder, err, "file_ipu", "local_id, id", "file_services", "id_serv,  id", "nzp_serv", "id_serv", "num_cnt", "Заводской номер ПУ", "Код услуги", Points.Pref.ToString(), "ИПУ без кода услуги");
            #endregion Выбираем ИПУ без кода услуги

            #region 24. Выбираем квартиры без кода типа по газоснабжению

            CheckOneRelation(conn_db, finder, err, "file_kvar", "id", "file_gaz", "id_prm, id", "gas_type", "id_prm", "id", "Номер ЛС", "Код параметра", Points.Pref.ToString(), "квартиры без кода типа по газоснабжению");
            #endregion Выбираем квартиры без кода типа по газоснабжению

            #region 25. Выбираем квартиры без кода типа по водоснабжению

            CheckOneRelation(conn_db, finder, err, "file_kvar", "id", "file_voda", "id_prm, id", "water_type", "id_prm", "id", "Номер ЛС", "Код параметра", Points.Pref.ToString(), "квартиры без кода типа по водоснабжению");
            #endregion Выбираем квартиры без кода типа по водоснабжению

            #region 26. Выбираем квартиры без кода типа по горячему водоснабжению

            CheckOneRelation(conn_db, finder, err, "file_kvar", "id", "file_voda", "id_prm, id", "hotwater_type", "id_prm", "id", "Номер ЛС", "Код параметра", Points.Pref.ToString(), "квартиры без кода типа по горячему водоснабжению");
            #endregion Выбираем квартиры без кода типа по горячему водоснабжению

            #region 27. Выбираем квартиры без кода типа по канализации

            CheckOneRelation(conn_db, finder, err, "file_kvar", "id", "file_voda", "id_prm, id", "canalization_type", "id_prm", "id", "Номер ЛС", "Код параметра", Points.Pref.ToString(), "квартиры без кода типа по канализации");
            #endregion Выбираем квартиры без кода типа по канализации

            #region 27. Выбираем квартиры без кода ЮЛ

            CheckOneRelation(conn_db, finder, err, "file_kvar", "id", "file_urlic", "supp_id, id", "id_urlic", "supp_id", "id", "Номер ЛС", "Код ЮЛ", Points.Pref.ToString(), "квартиры без кода ЮЛ");
            #endregion Выбираем квартиры без кода ЮЛ

            #region 28. Выбираем ИПУ без единиц измерения

            CheckOneRelation(conn_db, finder, err, "file_ipu", "local_id, id", "file_measures", "id_measure, id", "nzp_measure", "id_measure", "num_cnt", "Заводской номер ПУ", "Код единицы измерения", Points.Pref.ToString(), "ИПУ без единиц измерения");
            #endregion Выбираем ИПУ без единиц измерения

            #region 29. Выбираем оплаты без номера пачки

            CheckOneRelation(conn_db, finder, err, "file_oplats", "ls_id, id", "file_pack", "num_plat,id", "nzp_pack", "num_plat", "numplat", "Номер платежного документа", "Номер пачки", Points.Pref.ToString(), "оплаты без номера пачки");
            #endregion Выбираем оплаты без номера пачки

            #region 30. Выбираем услуги ЛС без ЛС

            CheckOneRelation(conn_db, finder, err, "file_servls", "ls_id, id", "file_kvar", "id", "ls_id", "id", "id_serv", "Код услуги", "Номер ЛС", Points.Pref.ToString(), "услуги ЛС без ЛС");
            #endregion Выбираем услуги ЛС без ЛС

            #region 31. Выбираем услуги ЛС без поставщика

            CheckOneRelation(conn_db, finder, err, "file_servls", "ls_id, id", "file_supp", "supp_id, id", "supp_id", "supp_id", "id_serv", "Код услуги", "Код поставщика", Points.Pref.ToString(), "услуги ЛС без поставщика");
            #endregion Выбираем услуги ЛС без поставщика

            #region 32. Выбираем перерасчеты начислений по услугам без услуг

            CheckOneRelation(conn_db, finder, err, "file_servp", "ls_id, id", "file_services", "id_serv,  id", "nzp_serv", "id_serv", "reval_month", "Дата перерасчета", "Уникальный код услуги", Points.Pref.ToString(), "перерасчеты начислений по услугам без услуг");
            #endregion Выбираем перерасчеты начислений по услугам без услуг

            #region 33. Выбираем перерасчеты начислений по услугам без единиц измерения

            CheckOneRelation(conn_db, finder, err, "file_servp", "ls_id, id", "file_measures", "id_measure, id", "nzp_measure", "id_measure", "reval_month", "Дата перерасчета", "Уникальный код услуги", Points.Pref.ToString(), "перерасчеты начислений по услугам без единиц измерения");
            #endregion Выбираем перерасчеты начислений по услугам без единиц измерения
        }
        catch
        {
            err.Append("Ошибка при проверке несвязности таблиц " + Environment.NewLine);
        }
        return ret;
    }
    #endregion Функция проверки связности загружаемых таблиц

    #region Функция проверки связности конкретной пары таблиц
    /// <summary>
    /// создание индексов и проверка связности для двух таблиц
    /// </summary>
    /// <param name="conn_db"></param>
    /// <param name="err"></param>
    /// <param name="doch_tbl">название дочерней таблицы</param>
    /// <param name="doch_field_for_index">поле, по которому будет создаваться индекс дочерней таблицы</param>
    /// <param name="rodit_tbl">название родительской таблицы</param>
    /// <param name="rodit_field_for_index">поле, по которому будет создаваться индекс родительской таблицы</param>
    /// <param name="doch_field_relation"> поле дочерней таблицы для связи с родительской</param>
    /// <param name="rodit_field_relation">поле родительской таблицы для связи с дочерней</param>
    /// <returns></returns>
    private Returns CheckOneRelation(IDbConnection conn_db, FilesImported finder, StringBuilder err, string doch_tbl, string doch_field_for_index, string rodit_tbl, string rodit_field_for_index,
                                    string doch_field_relation, string rodit_field_relation, string doch_field_log, string field1_name, string feild2_name, string pref, string errMessage)
    {
        Returns ret = Utils.InitReturns();
        try
        {
            string sql;
#if PG
            ret = ExecSQL(conn_db, " SET search_path TO '" + pref.Trim() + "_data'", true);
#else
                ret = ExecSQL(conn_db, " DATABASE " + pref.Trim() + "_data", true);
#endif

            #region создаем индексы
#if PG
            ret = ExecSQL(conn_db, " Create index ix1_" + doch_tbl.Trim() + " on " + Points.Pref + "_data." + doch_tbl.Trim() + " (" + doch_field_for_index.Trim() + ")", false);
            if (ret.result)
            {
                ExecSQL(conn_db, " analyze " + pref.Trim() + "_data." + doch_tbl.Trim(), false);
            }
            ret = ExecSQL(conn_db, " Create index ix1_" + rodit_tbl.Trim() + " on " + pref.Trim() + "_data." + rodit_tbl.Trim() + " (" + rodit_field_for_index.Trim() + ")", false);
            if (ret.result)
            {
                ExecSQL(conn_db, " analyze " + Points.Pref + "_data." + rodit_tbl.Trim(), false);
            }
#else
                ret = ExecSQL(conn_db, " Create index ix1_" + doch_tbl.Trim() + " on " + pref.Trim() + "_data" + tableDelimiter + "" + doch_tbl.Trim() + " (" + doch_field_for_index.Trim() + ")", false);
                if (ret.result)
                {
                    ExecSQL(conn_db, " Update statistics for table " + pref.Trim() + "_data" + tableDelimiter + "" + doch_tbl.Trim(), false);
                }
                ret = ExecSQL(conn_db, " Create index ix1_" + rodit_tbl.Trim() + " on " + pref.Trim() + "_data" + tableDelimiter + "" + rodit_tbl.Trim() + " (" + rodit_field_for_index.Trim() + ")", false);
                if (ret.result)
                {
                    ExecSQL(conn_db, " Update statistics for table " + pref.Trim() + "_data" + tableDelimiter + "" + rodit_tbl.Trim(), false);
                }
#endif
            #endregion

            #region Сама проверка связности
            //                sql = "select count(" + doch_field_for_index.Trim() + ") as kol from " + Points.Pref + "_data"+tableDelimiter + "" + doch_tbl.Trim() + " a " +
            //                                 "where a.nzp_file =" + finder.nzp_file + " and " +
            //                                 " not exists (select b." + rodit_field_relation.Trim() + " from " + Points.Pref + "_data"+tableDelimiter + "" + rodit_tbl.Trim() + " b" +
            //                                 " where b." + rodit_field_relation.Trim() + " = a." + doch_field_relation.Trim() + ")";

            //                var dt = ClassDBUtils.OpenSQL(sql, conn_db);
            //                if (Convert.ToInt32(dt.resultData.Rows[0]["kol"]) > 0)
            //                {
            //                    err.Append("Обнаружена несвязность данных. Имеются " + errMessage + " в количестве " + Convert.ToInt32(dt.resultData.Rows[0]["kol"]) + "." + Environment.NewLine);
            //                    //ret.text = "Обнаружена несвязность таблиц. Имеются квартиры без домов.";
            //                    //ret.result = false;
            //                    //return ret;
            //                }
            #endregion

            #region Сама проверка связности
            //todo Postgres

            //if (rodit_tbl.Trim() == "file_ipu" || doch_tbl.Trim() == "file_ipu")
            //{
            //    sql = "select a." + doch_field_log.Trim() + " as field1, a." + doch_field_relation.Trim() + " as field2 from " + pref.Trim() + "_data"+tableDelimiter + "" + doch_tbl.Trim() + " a " +
            //                                    " where a.nzp_file =" + finder.nzp_file + " and a." + doch_field_relation.Trim() + " is not null and " +
            //                                    " not exists (select b." + rodit_field_relation.Trim() + " from " + pref.Trim() + "_data"+tableDelimiter + "" + rodit_tbl.Trim() + " b" +
            //                                    " where cast (b." + rodit_field_relation.Trim() + " as decimal(14,0)) = cast( a." + doch_field_relation.Trim() + " as decimal(14,0)) " +
            //                                    " and b.nzp_file = " + finder.nzp_file + ")";
            //}
            //else
            //{
            sql = "select a." + doch_field_log.Trim() + " as field1, a." + doch_field_relation.Trim() + " as field2 from " + pref.Trim() + "_data" + tableDelimiter + "" + doch_tbl.Trim() + " a " +
                "where a.nzp_file =" + finder.nzp_file + " and a." + doch_field_relation.Trim() + " is not null and " +
                " not exists (select b." + rodit_field_relation.Trim() + " from " + pref.Trim() + "_data" + tableDelimiter + "" + rodit_tbl.Trim() + " b" +
                " where cast( b." + rodit_field_relation.Trim() + " as varchar(100))= cast (a." + doch_field_relation.Trim() + " as varchar(100)) " +
                " and b.nzp_file =" + finder.nzp_file + ")";
            //}


            var dt = ClassDBUtils.OpenSQL(sql, conn_db);
            if (dt.resultData.Rows.Count > 0)
            {
                err.Append("Обнаружена несвязность данных. Имеются " + errMessage + " в количестве " + dt.resultData.Rows.Count + "." + Environment.NewLine);
                err.Append(String.Format("{0,30}|{1,30}|{2}", field1_name, feild2_name, Environment.NewLine));

                foreach (DataRow rr in dt.GetData().Rows)
                {
                    string testMePls = String.Format("{0,30}|{1,30}|{2}", rr["field1"].ToString().Trim(), rr["field2"].ToString().Trim(), Environment.NewLine);
                    err.Append(testMePls);
                }

                //для лицевых счетов при несвязности меняем nzp_status
                if (doch_tbl.Trim() == "file_kvar")
                {
                    sql = "update " + pref.Trim() + "_data" + tableDelimiter + "" + doch_tbl.Trim() +
                          " set nzp_status = 2 " +
                          " where nzp_file =" + finder.nzp_file +
                          " and not exists (select b." + rodit_field_relation.Trim() + " from " + pref.Trim() + "_data" + tableDelimiter + "" + rodit_tbl.Trim() + " b" +
                          " where b." + rodit_field_relation.Trim() + " = " + doch_field_relation.Trim() +
                          " and b.nzp_file =" + finder.nzp_file + ") and nzp_status is null";
                    ret = ExecSQL(conn_db, sql, true);
                }
            }
            #endregion

        }
        catch (Exception ex)
        {
            MonitorLog.WriteLog("Ошибка при проверке несвязности таблиц в функции CheckRelation при проверке имеются ли " + errMessage + ex.Message + ex.TargetSite, MonitorLog.typelog.Error, true);
            err.Append("Ошибка при проверке при проверке имеются ли " + errMessage + Environment.NewLine);
        }
        return ret;
    }
    #endregion Функция проверки связности конкретной пары таблиц


    #region Функция проверки качества данных 6 секции
    public Returns Check6Section(IDbConnection conn_db, FilesImported finder, StringBuilder err)
    {
        Returns ret = Utils.InitReturns();
        string sql = "";
        try
        {
            sql = "select ls_id, supp_id, nzp_serv, eot, reg_tarif, fact_rashod, norm_rashod from " + Points.Pref + "_data" + tableDelimiter + "file_serv " +
                " where nzp_file = " + finder.nzp_file +
                " and (eot = 0 or reg_tarif = 0 or fact_rashod = 0 or fact_rashod = 0)";

            var dt = ClassDBUtils.OpenSQL(sql, conn_db);
            if (dt.resultData.Rows.Count > 0)
            {
                err.Append("Результат проверки качества данных 6 секции." + Environment.NewLine);

                string str = String.Format(@"{0,20}|{1,20}|{2,20}|{3,20}|{4,20}|{5,20}|{6,20}|", "Лицевые счета", "Код поставщика", "Код услуги", "ЭО тариф",
                "Рег. тариф", "Расход факт.", "Расход по норм.");
                str += Environment.NewLine;
                err.Append(str);

                foreach (DataRow rr in dt.GetData().Rows)
                {
                    str = String.Format(@"{0,20}|{1,20}|{2,20}|{3,20}|{4,20}|{5,20}|{6,20}|", rr["ls_id"].ToString().Trim(),
                        rr["supp_id"].ToString().Trim(), rr["nzp_serv"].ToString().Trim(), rr["eot"].ToString().Trim(), rr["reg_tarif"].ToString().Trim(),
                        rr["fact_rashod"].ToString().Trim(), rr["norm_rashod"].ToString().Trim());
                    str += Environment.NewLine;
                    err.Append(str);
                }
            }
        }
        catch (Exception ex)
        {
            MonitorLog.WriteLog("Ошибка при проверке качества данных 6 секции в функции Check6Section " + ex.Message + ex.TargetSite, MonitorLog.typelog.Error, true);
        }
        return ret;
    }
    #endregion

    // Отчет по разделу 3 из формата загрузок (<название_файла>.kvar.log)
    public Returns FileResultOfLoad(IDbConnection conn_db, FilesImported finder, StringBuilder strResult)
    {
        Returns ret = new Returns();

        string sql = "";

        try
        {
            #region 3.1.	Заголовок файла
            sql = 
                "select h.org_name as org_name, h.branch_name as branch_name, h.inn as inn, h.kpp as kpp, h.file_no as file_no, h.file_date as file_date," +
                " h.sender_phone as sender_phone, h.sender_fio as sender_fio, h.row_number as row_number, v.version_name as version, i.nzp_status as nzp_status " +
                " from " + Points.Pref + "_data" + tableDelimiter + "file_head h, " + Points.Pref + "_kernel" + tableDelimiter + "file_versions v, " + Points.Pref + "_data" + tableDelimiter + "files_imported i" +
                " where h.nzp_file =" + finder.nzp_file + " and v.nzp_version = i.nzp_version and i.nzp_file = " + finder.nzp_file;

            var dt = ClassDBUtils.OpenSQL(sql, conn_db).resultData;

            if (dt.Rows.Count > 1)
            {
                strResult.Append("1|" + dt.Rows[0]["version"].ToString().Trim() + 
                    "|Результат загрузки|" + dt.Rows[0]["org_name"].ToString().Trim() + "|" +
                    dt.Rows[0]["branch_name"].ToString().Trim() + "|" + dt.Rows[0]["inn"].ToString().Trim()
                    + "|" + dt.Rows[0]["kpp"].ToString().Trim() + "|" + dt.Rows[0]["file_no"].ToString().Trim() +
                    "|" + dt.Rows[0]["file_date"].ToString().Trim() + "|" + dt.Rows[0]["sender_phone"].ToString().Trim()
                    + "|" + dt.Rows[0]["sender_fio"].ToString().Trim() + "|" +
                    dt.Rows[0]["row_number"].ToString().Trim() + "|1|Количество заголовков:" + dt.Rows.Count + "|" + Environment.NewLine);
            }
            else if (dt.Rows.Count == 0)
            {

                strResult.Append("1||Результат загрузки||||||||||2|Файл не был загружен из-за ошибок|" + Environment.NewLine);
            }
            else
            {
                strResult.Append("1|" + dt.Rows[0]["version"].ToString().Trim() + "|Результат загрузки|" + dt.Rows[0]["org_name"].ToString().Trim() + "|" +
                    dt.Rows[0]["branch_name"].ToString().Trim() + "|" + dt.Rows[0]["inn"].ToString().Trim() + "|"
                    + dt.Rows[0]["kpp"].ToString().Trim() + "|" + dt.Rows[0]["file_no"].ToString().Trim() +
                    "|" + dt.Rows[0]["file_date"].ToString().Trim() + "|" + dt.Rows[0]["sender_phone"].ToString().Trim() + "|" + dt.Rows[0]["sender_fio"].ToString().Trim() + "|" +
                    dt.Rows[0]["row_number"].ToString().Trim() + "|" + "|" + "|" + Environment.NewLine);

            }
            #endregion

            #region 3.2.	Коды результатов загрузки

            strResult.Append("2|0|Загружено без ошибок|" + Environment.NewLine);

            strResult.Append("2|1|Синтаксическая ошибка|" + Environment.NewLine);

            strResult.Append("2|2|Семантическая ошибка|" + Environment.NewLine);

            #endregion

            sql = 
                " select ukas, id, open_date, close_date, fam, ima, otch, birth_date, dom_id, nkvar, nkvar_n, nzp_status" +
                " from " + Points.Pref + "_data" + tableDelimiter + "file_kvar" +
                " where nzp_file =" + finder.nzp_file;

            var dtk = ClassDBUtils.OpenSQL(sql, conn_db).resultData;

            foreach (DataRow r in dtk.Rows)
            {
                int status;
                if (r["nzp_status"] == DBNull.Value) status = 0;
                else status = Convert.ToInt32(r["nzp_status"]);

#warning Рената. обращение должно быть к dtK.Rows[0], а не dt.Rows[0]? так как берет другой nzp_status
                string answer = "";
                if (Convert.ToInt32(dt.Rows[0]["nzp_status"]) == 1 || Convert.ToInt32(dt.Rows[0]["nzp_status"]) == 3)
                    answer = "Загружен с ошибками";
                else if (Convert.ToInt32(dt.Rows[0]["nzp_status"]) == 2)
                    answer = "Загружен";

                strResult.Append("3|" + r["ukas"].ToString().Trim() + "|" + r["id"].ToString().Trim() + "|" + r["open_date"].ToString().Trim()
                    + "|" + r["close_date"].ToString().Trim() + "|" + r["fam"].ToString().Trim() + "|"
                    + r["ima"].ToString().Trim() + "|" + r["otch"].ToString().Trim() + "|" + r["birth_date"].ToString().Trim() + "||||"
                    + r["dom_id"].ToString().Trim() + "||" + r["nkvar"].ToString().Trim() + "|"
                    + r["nkvar_n"].ToString().Trim() + "|" + status + "|" + answer + "|"
                    + Environment.NewLine);
            }

        }
        catch (Exception ex)
        {
            MonitorLog.WriteLog("Ошибка выполнения процедуры FileResultOfLoad : " + ex.Message, MonitorLog.typelog.Error, true);
            ret.result = false;
            return ret;
        }

        ret.result = true;
        ret.text = "Выполнено.";

        return ret;
    }


    /// <summary>
    /// заполняет пустые поля таблиц _data"+tableDelimiter + "file_
    /// </summary>
    /// <param name="conn_db"></param>
    /// <returns></returns>
    private Returns SetValueForZeroFields(IDbConnection conn_db, StringBuilder err)
    {
        Returns ret = new Returns();

        #region Update tables
        try
        {
            #region file_dom
            //заполняем столбец ndom
            string sql = "update " + Points.Pref + "_data" + tableDelimiter + "file_dom set ndom = '-' where ndom is null";
            var dt = ClassDBUtils.ExecSQL(sql, conn_db);

            sql = "update " + Points.Pref + "_data" + tableDelimiter + "file_dom set nkor = '-' where nkor is null";
            dt = ClassDBUtils.ExecSQL(sql, conn_db);

            //заполняем столбец rajon
            sql = "update " + Points.Pref + "_data" + tableDelimiter + "file_dom set rajon = '-' where rajon is null";
            dt = ClassDBUtils.ExecSQL(sql, conn_db);

            //заполняем столбец town
            sql = "update " + Points.Pref + "_data" + tableDelimiter + "file_dom set town = '-' where town is null";
#if PG
            var res = ClassDBUtils.ExecSQL(sql, conn_db, true);
            if (res.resultCode != 0)
                throw new Exception(res.resultMessage);

            if (res.resultAffectedRows != 0)
#else
                ExecSQL(conn_db, sql, true);
                if (ClassDBUtils.GetAffectedRowsCount(conn_db) != 0)
#endif
            {
                err.Append("Имеются дома с незаполненным полем город/район в количестве " +
#if PG
 res.resultAffectedRows
#else
 ClassDBUtils.GetAffectedRowsCount(conn_db)
#endif
 + Environment.NewLine);
            }

            //заполняем столбец ulica
            sql = "update " + Points.Pref + "_data" + tableDelimiter + "file_dom set ulica = '-' where ulica is null";
            dt = ClassDBUtils.ExecSQL(sql, conn_db);

            #endregion

            #region file_mo
            //заполняем столбец vill
            sql = "update " + Points.Pref + "_data" + tableDelimiter + "file_mo set vill = cast( mo_name as char (50)) where vill is null";
            dt = ClassDBUtils.ExecSQL(sql, conn_db);
            #endregion

        }
        catch (Exception ex)
        {
            MonitorLog.WriteLog("Ошибка выполнения процедуры SetValueForZeroFields : " + ex.Message, MonitorLog.typelog.Error, true);
            ret.result = false;
            return ret;
        }
        #endregion

        ret.result = true;
        ret.text = "Выполнено.";

        return ret;
    }


    #region Функция сопоставления адресного пространства загруженных домов по коду КЛАДР
    /// <summary>
    /// Сопоставление адресного пространства загруженных домов по коду КЛАДР
    /// </summary>
    /// <returns></returns>
    public Returns SetLinksByKladr(IDbConnection conn_db, FilesImported finder, StringBuilder err)
    {
        Returns ret = Utils.InitReturns();
        try
        {
            string sql;
#if PG
            sql =
                " UPDATE " + Points.Pref + "_data" + tableDelimiter + "file_dom d " +
                " SET (nzp_ul, nzp_raj, nzp_town) = " +
                " ( (" +
                "      SELECT u.nzp_ul " +
                "      FROM " + Points.Pref + "_data" + tableDelimiter + "s_ulica u " +
                "      WHERE u.soato = d.kod_kladr " +
                "    ), " +
                "    ( " +
                "      SELECT u.nzp_raj " +
                "      FROM " + Points.Pref + "_data" + tableDelimiter + "s_ulica u " +
                "      WHERE u.soato = d.kod_kladr " +
                "    ), " +
                "    ( " +
                "      SELECT r.nzp_town " +
                "      FROM " + Points.Pref + "_data" + tableDelimiter + "s_ulica u, " +
                                Points.Pref + "_data" + tableDelimiter + "s_rajon r " +
                "      WHERE u.nzp_raj = r.nzp_raj " +
                "        AND u.soato = d.kod_kladr " +
                "     ) " +
                " ) " +
                " WHERE Length(d.kod_kladr) > 0 " +
                "       AND nzp_file = " + finder.nzp_file;

#else
                sql =
                    " UPDATE " + Points.Pref + "_data"+tableDelimiter+"file_dom " +
                    " SET (nzp_ul, nzp_raj, nzp_town) = " +
                    " (( " +
                    "   SELECT u.nzp_ul, u.nzp_raj, r.nzp_town " +
                    "   FROM " + Points.Pref + "_data"+tableDelimiter+"s_ulica u,  " + 
                                 Points.Pref + "_data"+tableDelimiter+"s_rajon r " +
                    "   WHERE u.soato = " + Points.Pref + "_data" + tableDelimiter + "file_dom.kod_kladr " +
                    "          AND u.nzp_raj = r.nzp_raj " +
                    " )) " +
                    " WHERE Length(" + Points.Pref + "_data" + tableDelimiter + "file_dom.kod_kladr) > 0 " +
                    "       AND nzp_file = " + finder.nzp_file;
        
#endif

            ClassDBUtils.ExecSQL(sql, conn_db, ClassDBUtils.ExecMode.Exception);
        }

        catch (Exception ex)
        {
            MonitorLog.WriteLog("Ошибка при сопоставлении улиц по коду КЛАДР в функции SetLinksByKladr : " + ex.Message, MonitorLog.typelog.Error, true);
            err.Append("Ошибка при сопоставлении улиц по коду КЛАДР в функции SetLinksByKladr: " + Environment.NewLine);
        }
        return ret;
    }
    #endregion Функция сопоставления адресного пространства загруженных домов по коду КЛАДР


    /// <summary>
    /// Заполняем file_servisec из своих таблиц, если в файле не было этой секции
    /// </summary>
    /// <param name="conn_db"></param>
    /// <returns></returns>
    private Returns LoadOur13Section(IDbConnection conn_db, FilesImported finder)
    {
        Returns ret = new Returns();

        #region Заполняем 13 секцию
        try
        {

            string sql = "insert into " + Points.Pref + "_data" + tableDelimiter + "file_services (id_serv, service, service2, nzp_measure, ed_izmer, type_serv, nzp_file, nzp_serv)" +
                                      "select  nzp_serv, service, service_name, nzp_measure, ed_izmer, 0, " + finder.nzp_file + ", nzp_serv from " + Points.Pref + "_kernel" + tableDelimiter + "services " +
                                      "where nzp_serv in" +
                                      "(select nzp_serv from " + Points.Pref + "_data" + tableDelimiter + "file_serv where nzp_file = " + finder.nzp_file + ")";

            ret = ExecSQL(conn_db, sql, true);
            if (!ret.result)
            {
                MonitorLog.WriteLog("Ошибка выполнения запроса в процедуре LoadOur13Section ", MonitorLog.typelog.Error, true);
                ret.result = false;
                ret.text = " Ошибка загрузки справочника услуг ";
                return ret;
            }

            sql = "insert into " + Points.Pref + "_data" + tableDelimiter + "file_services ( id_serv, service, service2, nzp_measure, ed_izmer, type_serv, nzp_file, nzp_serv)" +
                                                     "select nzp_serv, service, service_name, nzp_measure, ed_izmer, 0, " + finder.nzp_file + ", nzp_serv from " + Points.Pref + "_kernel" + tableDelimiter + "services " +
                                                     "where nzp_serv in" +
                                                     "(select nzp_serv from " + Points.Pref + "_data" + tableDelimiter + "file_odpu where nzp_file = " + finder.nzp_file + ") and" +
                                                     " nzp_serv not in (select nzp_serv from " + Points.Pref + "_data" + tableDelimiter + "file_services where nzp_file = " + finder.nzp_file + ")";

            ret = ExecSQL(conn_db, sql, true);
            if (!ret.result)
            {
                MonitorLog.WriteLog("Ошибка выполнения запроса в процедуре LoadOur13Section ", MonitorLog.typelog.Error, true);
                ret.result = false;
                ret.text = " Ошибка загрузки справочника услуг ";
                return ret;
            }

            sql = "insert into " + Points.Pref + "_data" + tableDelimiter + "file_services ( id_serv, service, service2, nzp_measure, ed_izmer, type_serv, nzp_file, nzp_serv)" +
                                                      "select  nzp_serv, service, service_name, nzp_measure, ed_izmer, 0, " + finder.nzp_file + ", nzp_serv from " + Points.Pref + "_kernel" + tableDelimiter + "services " +
                                                      "where nzp_serv in" +
                                                      "(select nzp_serv from " + Points.Pref + "_data" + tableDelimiter + "file_ipu where nzp_file = " + finder.nzp_file + ") and" +
                                                      " nzp_serv not in (select nzp_serv from " + Points.Pref + "_data" + tableDelimiter + "file_services where nzp_file = " + finder.nzp_file + ")";

            ret = ExecSQL(conn_db, sql, true);
            if (!ret.result)
            {
                MonitorLog.WriteLog("Ошибка выполнения запроса в процедуре LoadOur13Section ", MonitorLog.typelog.Error, true);
                ret.result = false;
                ret.text = " Ошибка загрузки справочника услуг ";
                return ret;
            }

            sql = "insert into " + Points.Pref + "_data" + tableDelimiter + "file_services ( id_serv, service, service2, nzp_measure, ed_izmer, type_serv, nzp_file, nzp_serv)" +
                                     "select nzp_serv, service, service_name, nzp_measure, ed_izmer, 0, " + finder.nzp_file + ", nzp_serv from " + Points.Pref + "_kernel" + tableDelimiter + "services " +
                                     "where nzp_serv in" +
                                     "(select nzp_serv from " + Points.Pref + "_data" + tableDelimiter + "file_servp where nzp_file = " + finder.nzp_file + ") and" +
                                     " nzp_serv not in (select nzp_serv from " + Points.Pref + "_data" + tableDelimiter + "file_services where nzp_file = " + finder.nzp_file + ")";

            ret = ExecSQL(conn_db, sql, true);
            if (!ret.result)
            {
                MonitorLog.WriteLog("Ошибка выполнения запроса в процедуре LoadOur13Section ", MonitorLog.typelog.Error, true);
                ret.result = false;
                ret.text = " Ошибка загрузки справочника услуг ";
                return ret;
            }

        }
        catch (Exception ex)
        {
            MonitorLog.WriteLog("Ошибка выполнения процедуры LoadOur13Section : " + ex.Message, MonitorLog.typelog.Error, true);
            ret.result = false;
            return ret;
        }
        #endregion

        ret.result = true;
        ret.text = "Выполнено.";

        return ret;
    }

    #region Функция  заполнения справочника единиц измерения услуг
    public Returns FillMeasure(IDbConnection conn_db, FilesImported finder)
    {
        Returns ret = Utils.InitReturns();
        string sql = "select count(*) as kol from " + Points.Pref + "_data" + tableDelimiter + "file_measures where nzp_file =" + finder.nzp_file;
        var dt = ClassDBUtils.OpenSQL(sql, conn_db);
        if (Convert.ToInt32(dt.resultData.Rows[0]["kol"]) == 0)
        {
            // считаем что люди заполнили справочник из стандартного приложения

            sql = " insert into " + Points.Pref + "_data" + tableDelimiter + "file_measures(id_measure,measure, nzp_file, nzp_measure) " +
                 " select idiotsky_kod, measure_long, " + finder.nzp_file.ToString() + ", nzp_measure " +
                 " from " + Points.Pref + "_kernel" + tableDelimiter + "s_measure where " + sNvlWord + "(idiotsky_kod, 0)>0 and idiotsky_kod is not null  ";

            ret = ExecSQL(conn_db, sql, true);

#if PG
            ExecSQL(conn_db, " analyze " + Points.Pref + "_data" + tableDelimiter + "file_measures", true);
#else
                ExecSQL(conn_db, " Update statistics for table " + Points.Pref + "_data" + tableDelimiter + "file_measures", true);
#endif
        }
        return ret;
    }
    #endregion Функция  заполнения справочника единиц измерения услуг


    // Убираем лидирующие нули
    private static string DeleteFirstZeros(string str)
    {
        int i = 0;

        if (str[i] == '0')
            while ((i < str.Length) && (str[i] == '0'))
                i++;

        String strResult;
        if (str[i] == '.')
            strResult = str.Remove(0, i - 1);
        else
            strResult = str.Remove(0, i);

        return strResult;
    }
#endregion вспомогающие функции
    }


}
