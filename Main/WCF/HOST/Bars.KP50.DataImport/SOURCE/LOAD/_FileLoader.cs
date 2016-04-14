using System;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.IO;
using Bars.KP50.Utils;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;
using STCLINE.KP50.Utility;


namespace STCLINE.KP50.DataBase
{
    public class DbValuesFromFile
    {
        public FilesImported finder { get; set; }
        public string rowNumber { get; set; }
        public int rowNumber1 { get; set; }
        public string Pvers { get; set; }
        public StringBuilder err { get; set; }
        public string[] vals { get; set; }
        public string sql { get; set; }
        public Returns ret { get; set; }
        public bool loaded13section { get; set; }
        public string fileName { get; set; }
        public int sectionNumber { get; set; }
        public string[] fileStrings { get; set; }

        public DbValuesFromFile()
        {
            finder = new FilesImported();
            rowNumber = "";
            rowNumber1 = 0;
            Pvers = "";
            err = new StringBuilder();
            sql = "";
            ret = Utils.InitReturns();
            loaded13section = false;
            fileName = "";
            sectionNumber = 0;
        }

    }

    public class DbFileLoader : DbAdminClient
    {
        private readonly IDbConnection _conDb;
        public FilesImported _finder { get; set; }
        public int _nzpVersion;
        public StringBuilder _errRelation { get; set; }
        public StringBuilder _errKvar { get; set; }
        public string _fullFileName { get; set; }
        public string _fDirectory { get; set; }
        public DbFileLoader(IDbConnection conDb)
        {
            _nzpVersion = -1;
            _conDb = conDb;
            _fullFileName = "";
            _fDirectory = "";
            _errKvar = new StringBuilder();
        }

        #region Поэтапная загрузка файла наследуемой информации
        /// <summary>
        /// Функция этапа Загрузки
        /// </summary>
        /// <param name="finder"></param>
        /// <param name="ret"></param>
        /// <returns></returns>
        public Returns LoadingStep(FilesImported finder, ref Returns ret)
        {
            _finder = finder;
            _errKvar = new StringBuilder();

            #region проверка на существование БД fXXX_upload

            string sql = "";
#if PG
            sql = " SET search_path TO  '" + Points.Pref + "_upload" + "'";
#else
            sql = "DATABASE " + Points.Pref + "_upload" + "; ";
#endif
            ret = ExecSQL(_conDb, sql, true);
            if (!ret.result)
            {
                ret.text = "Ошибка работы с БД. Возможно отсутствует схема(БД) для загрузки. Смотрите журнал ошибок.";
                ret.tag = -1;
                return ret;
            }
            #endregion

            int nzpExc = AddMyFile("Характеристики жилого фонда", _finder);
            int nzpExcLog = AddMyFile("Логи характеристик жилого фонда", _finder);

            try
            {

                sql =
                    "DELETE FROM " + Points.Pref + DBManager.sUploadAliasRest + "file_statuses " +
                    " WHERE nzp_stat IN (1,2,3,4);" +
                    "INSERT INTO " + Points.Pref + DBManager.sUploadAliasRest + "file_statuses (nzp_stat, status_name) " +
                    "VALUES " +
                    "(1,'Загружается'), " +
                    "(2,'Загружен')," +
                    "(3,'Загружен с ошибками')" +
                    "(4, 'Не загружен');";
                ExecSQL(sql, false);


                //директория файла
                _fDirectory = InputOutput.GetInputDir();

                _fullFileName = DecompressionFile(_finder.saved_name, InputOutput.GetInputDir(), ".txt", ref ret);

                _finder.nzp_file = InsertIntoFiles_imported(ref ret);

                SaveAndSetStat(nzpExc, ref ret);

                string[] fileStrings = ReadFile(_fullFileName);

                string versFull = "";
                _nzpVersion = CheckFileVersion(fileStrings, _fullFileName, ref versFull, ref ret, _finder);
                _finder.format_name = versFull;
                DbValuesFromFile valuesFromFile = new DbValuesFromFile
                {
                    finder = _finder,
                    loaded13section = false,
                    fileName = _fullFileName,
                    fileStrings = fileStrings
                };

                //Формуриуем запросы по секциям
                var addSections = new AddSections(valuesFromFile);
                addSections.Run(_conDb);
                _finder = valuesFromFile.finder;

                addSections.Close();

                if (valuesFromFile.err.Length != 0)
                {
                    CreateLogFile(_fullFileName, valuesFromFile.err, _errKvar, nzpExcLog,
                        (int)FilesImported.Statuses.LoadedWithErrors);
                    //есть ошибки -> заканчиваем работу
                    return ret;
                }

                //Запись в БД
                ExecFromFile_sql();

                if (valuesFromFile.err.Length != 0)
                {
                    CreateLogFile(_fullFileName, valuesFromFile.err, _errKvar, nzpExcLog,
                        (int)FilesImported.Statuses.LoadedWithErrors);
                    //есть ошибки -> заканчиваем работу
                }
                else
                {
                    CreateLogFile(_fullFileName, valuesFromFile.err, _errKvar, nzpExcLog,
                        (int)FilesImported.Statuses.Loaded);
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры LoadingStep." + Environment.NewLine + ex.Message + ex.StackTrace,
                    MonitorLog.typelog.Error, true);
                ret.result = false;
                ret.tag = -1;
                return ret;
            }
            finally
            {
                //доработать удаление по 20.000 записей
                sql =
                    " DELETE FROM   " + Points.Pref + DBManager.sUploadAliasRest + "file_sql " +
                    " WHERE nzp_file =" + _finder.nzp_file;
                DBManager.ExecSQL(_conDb, null, sql, true, 3600);
            }

            return ret;
        }
        /// <summary>
        /// Функция этапа Проверки целостности данных
        /// </summary>
        /// <param name="finder"></param>
        /// <param name="ret"></param>
        /// <returns></returns>
        public Returns CheckStep(FilesImported finder, ref Returns ret, IDbConnection conDb)
        {
            int nzpExcLog = AddMyFile("Логи характеристик жилого фонда", finder);
            string sql;
            bool loaded13section = false;
            try
            {
                _finder = finder;
                _errRelation = new StringBuilder();

                //Вытаскиваем наименование файла
                sql =
                    " SELECT TRIM(loaded_name) as col " +
                    " FROM " + Points.Pref + DBManager.sUploadAliasRest + " files_imported " +
                    " WHERE nzp_file = " + finder.nzp_file;
                DataTable dt1 = DBManager.ExecSQLToTable(conDb, sql);
                foreach (DataRow r in dt1.Rows)
                {
                    _fullFileName = r["col"].ToString();
                }

                sql =
                    " SELECT * " +
                    " FROM " + Points.Pref + DBManager.sUploadAliasRest + " file_services " +
                    " WHERE nzp_file = " + finder.nzp_file;
                DataTable dt = ClassDBUtils.OpenSQL(sql, conDb, ClassDBUtils.ExecMode.Exception).GetData();
                if (dt.Rows.Count != 0)
                {
                    loaded13section = true;
                }

                _errRelation = InsertIntoDb(loaded13section);

                if (_errRelation.Length == 0)
                {
                    CreateLogFile(_fullFileName, _errRelation, _errKvar, nzpExcLog,
                        (int)FilesImported.Statuses.Loaded);
                }
                else
                {
                    CreateLogFile(_fullFileName, _errRelation, _errKvar, nzpExcLog,
                        (int)FilesImported.Statuses.LoadedWithErrors);
                }

                ret.result = true;
                ret.text = "Целостность данных соблюдена";
                ret.tag = -1;
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры CheckStep." + Environment.NewLine + ex.Message + ex.StackTrace,
                    MonitorLog.typelog.Error, true);
                ret.result = false;
                ret.tag = -1;
                return ret;
            }
            
            return ret;
        }

        #endregion

        #region Загрузка файла наследуемой информации
        /// <summary>
        /// Загрузка файла "Характеристики жилого фонда и начисления ЖКУ"
        /// </summary>
        /// <param name="finder">файл</param>
        /// <returns>результат</returns>
        public Returns LoadFile(FilesImported finder, ref Returns ret)
        {
            _finder = finder;
            _errKvar = new StringBuilder();
            _errRelation = new StringBuilder();

            #region проверка на существование БД fXXX_upload

            string sql = "";
#if PG
                    sql = " SET search_path TO  '" + Points.Pref + "_upload" + "'";
#else
            sql = "DATABASE " + Points.Pref + "_upload" + "; ";
#endif
            ret = ExecSQL(_conDb, sql, true);
            if (!ret.result)
            {
                ret.text = "Ошибка работы с БД. Возможно отсутствует схема(БД) для загрузки. Смотрите журнал ошибок.";
                ret.tag = -1;
                return ret;
            }
            #endregion

            int nzpExc = AddMyFile("Характеристики жилого фонда", _finder);
            int nzpExcLog = AddMyFile("Логи характеристик жилого фонда", _finder);

            try
            {

                sql =
                    "DELETE FROM " + Points.Pref + DBManager.sUploadAliasRest + "file_statuses " +
                    " WHERE nzp_stat IN (1,2,3,4);" +
                    "INSERT INTO " + Points.Pref + DBManager.sUploadAliasRest + "file_statuses (nzp_stat, status_name) " +
                    "VALUES " +
                    "(1,'Загружается'), " +
                    "(2,'Загружен')," +
                    "(3,'Загружен с ошибками'), " +
                    "(4, 'Не загружен');";
                ExecSQL(sql, false);


                //директория файла
                _fDirectory = InputOutput.GetInputDir();

                _fullFileName = DecompressionFile(_finder.saved_name, InputOutput.GetInputDir(), ".txt", ref ret);

                _finder.nzp_file = InsertIntoFiles_imported(ref ret);

                SaveAndSetStat(nzpExc, ref ret);

                string[] fileStrings = ReadFile(_fullFileName);

                string versFull = "";
                _nzpVersion = CheckFileVersion(fileStrings, _fullFileName, ref versFull, ref ret, _finder);
                _finder.format_name = versFull;
                DbValuesFromFile valuesFromFile = new DbValuesFromFile
                {
                    finder = _finder,
                    loaded13section = false,
                    fileName = _fullFileName,
                    fileStrings = fileStrings
                };


                var addSections = new AddSections(valuesFromFile);
                addSections.Run(_conDb);
                _finder = valuesFromFile.finder;

                addSections.Close();

                if (valuesFromFile.err.Length != 0)
                {
                    CreateLogFile(_fullFileName, valuesFromFile.err, _errKvar, nzpExcLog,
                        (int) FilesImported.Statuses.LoadedWithErrors);
                    //есть ошибки -> заканчиваем работу
                    return ret;
                }

                //запись в БД 
                ExecFromFile_sql();
                //Проверка связности БД
                _errRelation = InsertIntoDb(valuesFromFile.loaded13section);

                if (_errRelation.Length == 0)
                {
                    CreateLogFile(_fullFileName, _errRelation, _errKvar, nzpExcLog,
                        (int) FilesImported.Statuses.Loaded);
                }
                else
                {
                    CreateLogFile(_fullFileName, _errRelation, _errKvar, nzpExcLog,
                        (int) FilesImported.Statuses.LoadedWithErrors);
                }

                ret.result = true;
                ret.text = "Файл успешно загружен.";
                ret.tag = -1;
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры LoadFile." + Environment.NewLine + ex.Message + ex.StackTrace,
                    MonitorLog.typelog.Error, true);
                ret.result = false;
                ret.tag = -1;
                return ret;
            }
            finally
            {
                //доработать удаление по 20.000 записей
                sql =
                    " TRUNCATE TABLE  " + Points.Pref + DBManager.sUploadAliasRest + "file_sql; " +
                    //" WHERE nzp_file =" + _finder.nzp_file;
                DBManager.ExecSQL(_conDb, null, sql, true, 3600);
            }
            return ret;
        }

        /// <summary>
        /// Выставление статуса успешной загрузки файла
        /// </summary>
        /// <param name="nzpExc"></param>
        /// <param name="ret"></param>
        public void SaveAndSetStat(int nzpExc, ref Returns ret)
        {
            try
            {
                string commStr =
                    " UPDATE " + Points.Pref + DBManager.sUploadAliasRest + "files_imported SET nzp_exc = " + nzpExc +
                    " WHERE nzp_file = " + _finder.nzp_file;
                ExecSQL(_conDb, commStr, true);

                string fn4 = "";
                if (InputOutput.useFtp)
                {
                    fn4 =
                        InputOutput.SaveInputFile(String.Format("{0}{1}", _fDirectory,
                            System.IO.Path.GetFileName(_finder.saved_name)));
                }

                string sql = " UPDATE " + sDefaultSchema + "excel_utility SET stats = " + (int)ExcelUtility.Statuses.Success +
                      ", dat_out = now(), exc_path = '" + (InputOutput.useFtp ? fn4 : Path.Combine(_fDirectory, System.IO.Path.GetFileName(_finder.saved_name))) +
                      "' WHERE nzp_exc = " + nzpExc;
                ExecSQL(_conDb, sql, true);

                //SetMyFileState(new ExcelUtility()
                //{
                //    nzp_exc = nzpExc,
                //    status = ExcelUtility.Statuses.Success,
                //    exc_path =
                //        InputOutput.useFtp
                //            ? fn4
                //            : Path.Combine(_fDirectory, System.IO.Path.GetFileName(_finder.saved_name))
                //});
            }
            catch (Exception ex)
            {
                ret.text = "Ошибка при выставлении статуса успешной загрузки файла.";
                ret.result = false;
                ret.tag = -1;
                throw new Exception("Ошибка выполнения процедуры SaveAndSetStat: " + ex.Message);
            }
        }

        #endregion Загрузка файла наследуемой информации


        #region вспомогательные функции

        /// <summary>
        /// Ф-ция формирования лог файла
        /// </summary>
        /// <param name="fullFileName"></param>
        /// <param name="err">Лог ошибок загрузки</param>
        /// <param name="errKvar">Лог ошибок загрузки ЛС</param>
        /// <param name="nzpExcLog">Уникальный номер лог-файла</param>
        /// <param name="loadstatus">Статаус загрузки</param>
        public void CreateLogFile(string fullFileName, StringBuilder err,  StringBuilder errKvar, int nzpExcLog, int loadstatus)
        {
            Returns ret;

            //отчет по лицевым счетам 3.1-3.3
            FileResultOfLoad(errKvar);

            string logFileFullName = fullFileName + ".log";
            string kvarLogFileFullName = fullFileName + "Kvar.log";

            StreamWriter sw = File.CreateText(logFileFullName);
            sw.Write(err.ToString());
            sw.Flush();
            sw.Close();

            StreamWriter swKvar = File.CreateText(kvarLogFileFullName);
            swKvar.Write(errKvar.ToString());
            swKvar.Flush();
            swKvar.Close();
            
            #region Архивация лога

            string logArchiveFullName = null;
            Archive.GetInstance()
                .Compress((logArchiveFullName = fullFileName + "_LOG.zip"),
                    new string[] {logFileFullName, kvarLogFileFullName}, true);
            /*
            string logArchiveFullName = fullFileName + "_LOG.zip";
            SevenZipCompressor szcComperssor = new SevenZipCompressor();
            szcComperssor.ArchiveFormat = OutArchiveFormat.Zip;
            szcComperssor.CompressFiles(logArchiveFullName, logFileFullName, kvarLogFileFullName);

            File.Delete(logFileFullName);
            File.Delete(kvarLogFileFullName);
            File.Delete(fullFileName);
            */

            string fn2 = "";
            if (InputOutput.useFtp)
                fn2 = InputOutput.SaveInputFile(logArchiveFullName);

            #endregion

            //Обновление статуса
            try
            {
                string sql =
                    " UPDATE " + Points.Pref + DBManager.sUploadAliasRest + "files_imported SET nzp_status =  " + loadstatus +
                    " WHERE nzp_file = " + _finder.nzp_file;
                ret = ExecSQL(_conDb, sql, true);
                if (!ret.result)
                {
                    MonitorLog.WriteLog("Ошибка обновления статуса файла " + fullFileName, MonitorLog.typelog.Error, true);
                    ret.result = false;
                    ret.text = " Ошибка обновления статуса файла. ";
                    ret.tag = -1;
                    return;
                }
                string commStr =
                    "UPDATE " + Points.Pref + DBManager.sUploadAliasRest +
                    "files_imported SET (nzp_exc_log, percent) = (" + nzpExcLog + ",1) " +
                    " WHERE nzp_file = " + _finder.nzp_file;
                ExecSQL(_conDb, commStr, true);

                fullFileName = logArchiveFullName;



                sql = " UPDATE " + sDefaultSchema + "excel_utility SET stats = " + (int)ExcelUtility.Statuses.Success +
                      ", dat_out = now(), exc_path = '" + (InputOutput.useFtp ? fn2 : fullFileName) +
                      "' WHERE nzp_exc = " + nzpExcLog;
                ExecSQL(_conDb, sql, true);


            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры LoadFile : " + ex.Message, MonitorLog.typelog.Error, true);
            }
        }

        /// <summary>
        /// Функция проставления уникальных кодов домов
        /// </summary>
        /// <param name="conn_db"></param>
        /// <param name="finder"></param>
        public Returns SetUniqDomNum(IDbConnection conn_db, FilesImported finder)
        {
            string sql;
            Returns ret = Utils.InitReturns();

            // Проверяем, есть ли local_id = null  в file_dom
            sql =
                " SELECT 1 " +
                " FROM " + Points.Pref + DBManager.sUploadAliasRest + "file_dom " +
                " WHERE nzp_file = " + finder.nzp_file +
                " AND local_id is  null";
            int cnt = ClassDBUtils.OpenSQL(sql, conn_db, ClassDBUtils.ExecMode.Exception).GetData().Rows.Count;
            if (cnt > 0)
            {
                MonitorLog.WriteLog("Имеются коды домов со значением null в таблице file_dom с номером файла: nzp_file = " + finder.nzp_file + " " + Environment.NewLine, MonitorLog.typelog.Error, true);
                return new Returns(false, "Имеются коды домов со значением null", -1);
            }
            
            sql = " DROP TABLE t_set_uniq_dom";
            DBManager.ExecSQL(conn_db, sql, true);
            MonitorLog.WriteLog("1", MonitorLog.typelog.Info, true);
            sql =
                " CREATE TEMP TABLE t_set_uniq_dom(" +
                " id serial, loc_id character(20))";
            DBManager.ExecSQL(conn_db, sql, true);
           // MonitorLog.WriteLog("2", MonitorLog.typelog.Info, true);
             sql =
                 " CREATE UNIQUE INDEX idx_for_t_set_uniq_dom ON t_set_uniq_dom (loc_id)";
            DBManager.ExecSQL(conn_db, sql, true);
           // MonitorLog.WriteLog("3", MonitorLog.typelog.Info, true);
            sql =
                " INSERT INTO t_set_uniq_dom(loc_id)" +
                " SELECT local_id " +
                " FROM " + Points.Pref + DBManager.sUploadAliasRest + "file_dom " +
                " WHERE nzp_file = " + finder.nzp_file;
            DBManager.ExecSQL(conn_db, sql, true);
           // MonitorLog.WriteLog("32", MonitorLog.typelog.Info, true);
            sql =
                " UPDATE " + Points.Pref + DBManager.sUploadAliasRest + "file_dom " +
                " SET id = " +
                " (SELECT id FROM t_set_uniq_dom " +
                " WHERE loc_id = " + Points.Pref + DBManager.sUploadAliasRest + "file_dom.local_id)" +
                " WHERE nzp_file = " + finder.nzp_file;
                DBManager.ExecSQL(conn_db, sql, true);
           // MonitorLog.WriteLog("4", MonitorLog.typelog.Info, true);
            sql =
                " UPDATE " + Points.Pref + DBManager.sUploadAliasRest + "file_kvar " +
                " SET dom_id = " +
                " (SELECT id FROM t_set_uniq_dom " +
                " WHERE loc_id = " + Points.Pref + DBManager.sUploadAliasRest + "file_kvar.dom_id_char)" +
                " WHERE nzp_file = " + finder.nzp_file +
                " AND EXISTS" +
                " (SELECT 1 FROM " + Points.Pref + DBManager.sUploadAliasRest + "file_dom d" +
                "  WHERE d.local_id = dom_id_char and d.nzp_file = " + finder.nzp_file +")";
            DBManager.ExecSQL(conn_db, sql, true);
            // MonitorLog.WriteLog("5", MonitorLog.typelog.Info, true);
            sql =
                " SELECT trim(id) as id FROM " + Points.Pref + DBManager.sUploadAliasRest + "file_kvar" +
                " WHERE dom_id = '-1' AND nzp_file = " + finder.nzp_file;
            DataTable kvar = ClassDBUtils.OpenSQL(sql, conn_db, ClassDBUtils.ExecMode.Exception).GetData();
           // MonitorLog.WriteLog("6", MonitorLog.typelog.Info, true);
            if (kvar.Rows.Count > 0)
            {
                string ls = "";
              //  MonitorLog.WriteLog("6_1", MonitorLog.typelog.Info, true);
                foreach (DataRow row in kvar.Rows)
                {
              //      MonitorLog.WriteLog("6_2", MonitorLog.typelog.Info, true);
                    ls += row["id"] + ", ";
                }
              //  MonitorLog.WriteLog("6_3", MonitorLog.typelog.Info, true);
                ls = ls.Length > 2 ? ls.Substring(0, ls.Length - 2) : ls;
                ret.result = false;
                ret.text += " Лицевым счетам " + ls +
                            " не прошла операция перенумерования домов, возможно имеется несвязность в файле, и для них отсутствуют дома";
            }

            //MonitorLog.WriteLog("9", MonitorLog.typelog.Info, true);
            sql =
                " UPDATE " + Points.Pref + DBManager.sUploadAliasRest + "file_odpu " +
                " SET dom_id = " +
                " (SELECT id FROM t_set_uniq_dom " +
                " WHERE loc_id = " + Points.Pref + DBManager.sUploadAliasRest + "file_odpu.dom_id_char)" +
                " WHERE nzp_file = " + finder.nzp_file +
                " AND EXISTS" +
                " (SELECT 1 FROM " + Points.Pref + DBManager.sUploadAliasRest + "file_dom d" +
                "  WHERE d.local_id = dom_id_char and d.nzp_file = " + finder.nzp_file + ")";
            DBManager.ExecSQL(conn_db, sql, true);
            //MonitorLog.WriteLog("10", MonitorLog.typelog.Info, true);
            sql =
                " SELECT trim(local_id) as local_id FROM " + Points.Pref + DBManager.sUploadAliasRest + "file_odpu" +
                " WHERE dom_id = '-1' AND nzp_file = " + finder.nzp_file + " AND type_pu <> 2";
            DataTable odpu = ClassDBUtils.OpenSQL(sql, conn_db, ClassDBUtils.ExecMode.Exception).GetData();
            //MonitorLog.WriteLog("11", MonitorLog.typelog.Info, true);
            if (odpu.Rows.Count > 0)
            {
                string od = "";
                foreach (DataRow row in odpu.Rows)
                {
                    od += row["local_id"] + ", ";
                }
                od = od.Length > 2 ? od.Substring(0, od.Length - 2) : od;
                ret.result = false;
                ret.text += " ОДПУ " + od + " не прошли операцию перенумерования домов, возможно имеется несвязность в файле, и для них отсутствуют дома";
            }

            //MonitorLog.WriteLog("12", MonitorLog.typelog.Info, true);
            sql =
                " UPDATE " + Points.Pref + DBManager.sUploadAliasRest + "file_agreement " +
                " SET id_dom = " +
                " (SELECT id FROM t_set_uniq_dom " +
                " WHERE loc_id = " + Points.Pref + DBManager.sUploadAliasRest + "file_agreement.id_dom)" +
                " WHERE nzp_file = " + finder.nzp_file +
                " AND EXISTS" +
                " (SELECT 1 FROM " + Points.Pref + DBManager.sUploadAliasRest + "file_dom d" +
                "  WHERE d.local_id = id_dom and d.nzp_file = " + finder.nzp_file + ")";
            DBManager.ExecSQL(conn_db, sql, true);

           // MonitorLog.WriteLog("13", MonitorLog.typelog.Info, true);
            sql =
                " SELECT id FROM " + Points.Pref + DBManager.sUploadAliasRest + "file_agreement" +
                " WHERE id_dom = '-1' AND nzp_file = " + finder.nzp_file;
            DataTable agr = ClassDBUtils.OpenSQL(sql, conn_db, ClassDBUtils.ExecMode.Exception).GetData();
            if (agr.Rows.Count > 0)
            {
                string a = "";
                foreach (DataRow row in agr.Rows)
                {
                    a += row["id"] + ", ";
                }
                a = a.Length > 2 ? a.Substring(0, a.Length - 2) : a;
                ret.result = false;
                ret.text += " Соглашения " + a + " не прошли операцию перенумерования домов, возможно имеется несвязность в файле, и для них отсутствуют дома"; 
            }
            MonitorLog.WriteLog("14", MonitorLog.typelog.Info, true);
            return ret;
        }

        /// <summary>
        /// Ф-ция записи загруженных данных в БД
        /// </summary>
        /// <param name="versFull"></param>
        /// <param name="loaded13Section"></param>
        /// <returns></returns>
        private StringBuilder InsertIntoDb( bool loaded13Section)
        {
            Returns ret;
            string format_name = "";

            //для лога ошибок связности БД
            StringBuilder errRelation = new StringBuilder();

            //проставляем уникальные коды домов 
            ret = SetUniqDomNum(_conDb, _finder);
            if (!ret.result)
            {
               errRelation.Append(" Ошибка выполнения процедуры SetUniqDomNum: " + ret.text + Environment.NewLine);
               return errRelation;
            }

            SetLoadPercent("0.76", _finder.nzp_file);

            //заполняем в file_dom поля ndom, nkor, rajon, если они null 
            SetValueForZeroFields(errRelation);

            //вытаскиваем имя формата файла
            string sql =
                " SELECT TRIM(fv.version_name) as name " +
                " FROM " + Points.Pref + DBManager.sUploadAliasRest + " files_imported fi, " +
                Points.Pref + DBManager.sUploadAliasRest + " file_versions fv " +
                " WHERE fi.nzp_file = " + _finder.nzp_file +
                " AND fi.nzp_version = fv.nzp_version";
            DataTable dt = DBManager.ExecSQLToTable(_conDb, sql);
            foreach (DataRow r in dt.Rows)
            {
                format_name = r["name"].ToString();
            }

            //сопоставление адресного пространства по коду КЛАДР, если версия файла 1.2.2
            if (format_name.Trim() == "'1.2.2'")
            {
                SetLinksByKladr(errRelation);
            }

            //если не загружали 13 секцию, то берем из таблицы kernel:services
            if (!loaded13Section)
            {
                LoadOur13Section(_conDb, _finder);
            }
            // Заполнить единицы измерения из стандартного приложения если нет раскладки из файла 
            FillMeasure(_conDb, _finder);

            FillTypeNedopos(_conDb, _finder);

            SetLoadPercent("0.80", _finder.nzp_file);

            using (var check = new Check())
            {
                //проверка уникальности юр.лиц
                check.CheckUniqUrlic(_conDb, _finder);
                SetLoadPercent("0.81", _finder.nzp_file);

                //проверки того, что ЮР лицо является управляющей компанией
                check.CheckIsUk(_conDb, _finder, errRelation);
                SetLoadPercent("0.82", _finder.nzp_file);


                //проверка оплат
                check.CheckOplats(_conDb, _finder);
                SetLoadPercent("0.84", _finder.nzp_file);

                //проверка существования кода перекидки в справочнике
                check.CheckKodPerekidki(_conDb, _finder);
                SetLoadPercent("0.85", _finder.nzp_file);

                //проверка уникальности инф-ии о ЛС, принадлежащих к ПУ
                check.CheckInfoPu(_conDb, _finder, errRelation);
                SetLoadPercent("0.86", _finder.nzp_file);

                //проверка уникальности данных
                check.CheckUnique(_conDb, _finder, errRelation);
                SetLoadPercent("0.87", _finder.nzp_file);

                //проверка связности БД
                check.CheckRelation(_conDb, _finder, errRelation);
                SetLoadPercent("0.91", _finder.nzp_file);

                //проверка показаний ИПУ перехода через ноль
                check.CheckIpuP(_conDb, _finder, errRelation);
                SetLoadPercent("0.96", _finder.nzp_file);

                //качество данных из 6 секции
                check.Check6Section(_conDb, _finder, errRelation);
                SetLoadPercent("0.98", _finder.nzp_file);
            }

            return errRelation;
        }


        /// <summary>
        /// Ф-ция добавления информации о файле в таблицу files_imported 
        /// </summary>
        /// <param name="ret"></param>
        /// <returns>Уникальный номер файла nzp_file</returns>
        public int InsertIntoFiles_imported( ref Returns ret)
        {
            /*DbWorkUser db = new DbWorkUser();
            int localUSer = db.GetLocalUser(_conDb, _finder, out ret);
            if (!ret.result)
            {
                ret.text = "Ошибка определения локального пользователя.";
                ret.result = false;
                ret.tag = -1;
                throw new Exception("Ошибка выполнения процедуры InsertIntoFiles_imported при определении локального пользователя.");
            }*/

            int localUSer = _finder.nzp_user;

            try
            {
                string sql =
                    " INSERT INTO " + Points.Pref + DBManager.sUploadAliasRest + "files_imported " +
                    " ( nzp_version, loaded_name, saved_name, nzp_status, " +
                    "    created_by, created_on, percent, pref, file_type) " +
                    " VALUES (" + _nzpVersion + ",'" + _finder.loaded_name + "',\'" + _finder.saved_name + "\',1," +
                    localUSer + "," + sCurDateTime + ", 0, '" + _finder.bank + "', " + _finder.file_type + ")  ";
                ClassDBUtils.ExecSQL(sql, _conDb, ClassDBUtils.ExecMode.Exception);
            }
            catch (Exception ex)
            {
                ret.text = "Ошибка добавления информации о файле.";
                ret.result = false;
                ret.tag = -1;
                throw new Exception("Ошибка выполнения процедуры InsertIntoFiles_imported " +
                                    "при добавлении информации о файле в таблицу " + Points.Pref + DBManager.sUploadAliasRest + "files_imported." + ex.Message);
            }
            return GetSerialValue(_conDb);
        }

        /// <summary>
        /// Ф-ция считывания загружаемого файла в массив строк
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns>Массив строк string[] </returns>
        public static string[] ReadFile(string fileName, int encoding = 1251)
        {
            Returns ret;

            byte[] buffer = new byte[0];

            if (System.IO.File.Exists(fileName) == false)
            {
                ret.result = false;
                ret.text = "Файл отсутствует по указанному пути";
                ret.tag = -1;
                throw new Exception("Файл отсутствует по указанному пути. Название файла: " + fileName);
            }


            System.IO.FileStream fstream = new System.IO.FileStream(fileName, System.IO.FileMode.Open,
                System.IO.FileAccess.Read);

            buffer = new byte[fstream.Length];
            fstream.Position = 0;
            fstream.Read(buffer, 0, buffer.Length);


            string tehPlanFileString = System.Text.Encoding.GetEncoding(encoding).GetString(buffer);
            string[] stSplit = { System.Environment.NewLine };
            return tehPlanFileString.Split(stSplit, StringSplitOptions.None);
        }

        /// <summary>
        /// Ф-ция выполнения sql-запросов из таблицы file_sql
        /// </summary>
        /// <param name="finder"></param>
        /// <param name="con_db"></param>
        private void ExecFromFile_sql()
        {
            Returns ret;

            Int32 step1 = 0;
            Int32 step2 = 0;
            string sql1 =
                " SELECT min(id) AS step1,  max(id)  AS step2 " +
                " FROM " + Points.Pref + DBManager.sUploadAliasRest+ "file_sql " +
                " WHERE nzp_file = " + _finder.nzp_file;
            var dt2 = ClassDBUtils.OpenSQL(sql1, _conDb);
            foreach (DataRow rr2 in dt2.resultData.Rows)
            {
                step1 = Convert.ToInt32(rr2["step1"]);
                step2 = Convert.ToInt32(rr2["step2"]);
            }

            for (int i = step1; i < step2; i = i + 5000)
            {
                // Выполнить с шагом 1000 строк 
                sql1 =
                    "SELECT id, sql_zapr FROM " + Points.Pref + DBManager.sUploadAliasRest + "file_sql " +
                    " WHERE id>=" + Convert.ToString(i) +
                    "  AND id <" + Convert.ToString(i + 5000) +
                    "  AND nzp_file=" + _finder.nzp_file.ToString() +
                    " ORDER BY 1";
                var dt = ClassDBUtils.OpenSQL(sql1, _conDb);
                //string sql3 = "";
                foreach (DataRow rr in dt.resultData.Rows)
                {
                    string sql2 = Convert.ToString(rr["sql_zapr"]);
                    sql2 = sql2.Replace("$", "\'");
                    //sql3 += sql2 + '\n';
                    ret = ExecSQL(_conDb, sql2.Trim(), true);
                }
                //ret = ExecSQL(_conDb, sql3.Trim(), true);
                dt = null;
                //sql1 = sUpdStat + " " + Points.Pref + DBManager.sUploadAliasRest+ "file_sql";
                //ExecSQL(_conDb, sql1, true);
                sql1 = "";

                decimal percentLoad = (decimal)(i - step1) / ((step2 - step1));
                decimal dola = 0;
                if (Math.Round(percentLoad, 1) >= dola)
                {
                    SetLoadPercent((Math.Round(percentLoad, 1) / 3 + (decimal)0.34).ToString(), _finder.nzp_file);
                    //string sqlPercent1 =
                    //    " update " + Points.Pref + DBManager.sUploadAliasRest + "files_imported set percent = " +
                    //    (Math.Round(percentLoad, 1)/3 + (decimal) 0.34) +
                    //    " where nzp_file = " + finder.nzp_file;
                    //ret = ExecSQL(con_db, sqlPercent1, true);
                    dola = dola + (decimal)0.1;
                }
            }

            sql1 =
                " DELETE FROM   " + Points.Pref + DBManager.sUploadAliasRest + "file_sql " +
                " WHERE nzp_file =" + _finder.nzp_file;
            ret = ExecSQL(_conDb, sql1, true);

        }

        /// <summary>
        /// Ф-ция проверки версии загружаемого файла
        /// </summary>
        /// <param name="fileStrings"></param>
        /// <param name="fileName"></param>
        /// <returns>Номер версии</returns>
        private int CheckFileVersion(string[] fileStrings, string fileName, ref string versFull, ref Returns ret, FilesImported finder)
        {
            _nzpVersion = -1;
            IDataReader reader = null;

            string firstRow = Array.Find(fileStrings, x => x != "");
            string version = firstRow.Split(new char[] { '|' })[1];
            versFull = version;
            string sql =
                " SELECT nzp_version FROM " + Points.Pref + DBManager.sUploadAliasRest + "  file_versions " +
                " WHERE version_name = \'" + version + "\'";
            ret = ExecRead(_conDb, out reader, sql, true);
            if (!ret.result)
            {
                ret.text = " Ошибка при получения версии файла. ";
                ret.tag = -1;
                throw new Exception(" Ошибка выполнения процедуры CheckFileVersion при получения версии файла " + fileName +
                    Environment.NewLine + " Проверьте таблицу " + Points.Pref + DBManager.sUploadAliasRest + "  file_versions ");
            }
            while (reader.Read())
            {
                if (reader["nzp_version"] != DBNull.Value) _nzpVersion = Convert.ToInt32(reader["nzp_version"]);
            }

            if (_nzpVersion == -1)
            {
                ret.result = false;
                ret.text = " Ошибка версии файла (значение из файла: '" + version + "'). Загруженный файл не прошел контроль версии.";
                ret.tag = -1;
                sql =
                    " UPDATE " + Points.Pref + DBManager.sUploadAliasRest + "files_imported " +
                    " SET nzp_status = 4 " +
                    " WHERE nzp_file = " + finder.nzp_file;
                ExecSQL(sql, false);
                throw new Exception(" Ошибка версии файла (значение из файла: "+ version + "). Загруженный файл не прошел контроль версии. Название: " + fileName);
            }
            sql =
                "UPDATE " + Points.Pref + DBManager.sUploadAliasRest + "files_imported SET " +
                " nzp_version = " + _nzpVersion + 
                " WHERE nzp_file = " +_finder.nzp_file;
            ClassDBUtils.ExecSQL(sql, _conDb, ClassDBUtils.ExecMode.Exception);
            
            return _nzpVersion;
        }

        /// <summary>
        /// Ф-ция разархивации загружаемого файла
        /// </summary>
        /// <param name="fileName">Имя сохраненного файла</param>
        /// <param name="fDirectory">Путь к файлу</param>
        /// <param name="extens">Расширение, которое необходимо извлечь из архива</param>
        /// <param name="ret"></param>
        /// <returns>Полный путь к разархивирвоанному файлу</returns>
        public static string DecompressionFile(string fileName, string fDirectory, string extens, ref Returns ret)
         {
            string fullFileName = Path.Combine(fDirectory, System.IO.Path.GetFileName(fileName));

            if (InputOutput.useFtp)
            {
                if (!InputOutput.DownloadFile(fileName, fullFileName))
                {
                    ret.text = "Не удалось скопировать с ftp сервера файл " + fileName + " в файл " + fullFileName;
                    ret.result = false;
                    ret.tag = -1;
                    throw new Exception("Ошибка выполнения процедуры DecompressionFile: " +
                                        "Не удалось скопировать с ftp сервера файл " + fileName + " в файл " +
                                        fullFileName);
                }

            }
            try {
                string[] files = Archive.GetInstance(fullFileName).Decompress(fullFileName, fDirectory);
                if (!files.IsEmpty())
                    fullFileName = Path.Combine(fDirectory, files.FirstOrDefault());
                else
                {
                    ret.text = " Ошибка при разархивации файла. ";
                    ret.result = false;
                    ret.tag = -1;
                    throw new Exception("Ошибка выполнения процедуры DecompressionFile. Название файла: " +
                                        fullFileName);
                }
            }
            catch (Exception ex)
            {
                ret.text = " Ошибка при разархивации файла. ";
                ret.result = false;
                ret.tag = -1;
                throw new Exception("Ошибка выполнения процедуры DecompressionFile. Название файла: " +
                                    fullFileName +
                                    Environment.NewLine + ex.Message);
            }
            /*
            try
            {
                using (SevenZipExtractor extractor = new SevenZipExtractor(fullFileName))
                {

                    //создание папки с тем же именем
                    DirectoryInfo exDirectorey =
                        Directory.CreateDirectory(Path.Combine(fDirectory,
                            System.IO.Path.GetFileNameWithoutExtension(fileName)));

                    extractor.ExtractArchive(exDirectorey.FullName);
                    FileInfo[] files = exDirectorey.GetFiles("*" + extens);

                    if (files.Length > 0)
                    {
                        FileInfo textFile = files[0];
                        textFile.MoveTo(Path.Combine(fDirectory, exDirectorey.Name + extens));
                        //удаление распакованной директории
                        Directory.Delete(exDirectorey.FullName, true);

                        //обновляем ссылку на новый распакованный файл
                        fullFileName = textFile.FullName;
                    }
                }
            }
            catch (Exception ex)
            {
                ret.text = " Ошибка при разархивации файла. ";
                ret.result = false;
                ret.tag = -1;
                throw new Exception("Ошибка выполнения процедуры DecompressionFile. Название файла: " +
                                    fullFileName +
                                    Environment.NewLine + ex.Message);
            }
            */
            return fullFileName;
        }

        /// <summary>
        /// Ф-ция выставления процента загрузки файла
        /// </summary>
        /// <param name="percent"></param>
        /// <param name="nzp_file"></param>
        /// <param name="con_db"></param>
        public void SetLoadPercent(string percent, int nzp_file)
        {
            try
            {
                string sqlPercent =
                    " UPDATE " + Points.Pref + DBManager.sUploadAliasRest + "files_imported SET percent = " + percent +
                    " WHERE nzp_file = " + nzp_file;
                ClassDBUtils.ExecSQL(sqlPercent, _conDb, ClassDBUtils.ExecMode.Exception);
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog(" Ошибка при выставлении процента загрузки (равного " + percent + ") в ф-ции SetLoadPercent: " + ex.Message + ex.StackTrace, MonitorLog.typelog.Error, true);
            }
        }

        /// <summary>
        /// Добавляет запись в список моих файлов и возвращает код записи при успешном выполнении операции
        /// </summary>
        /// <param name="repname">Выводимое название файла</param>
        /// <param name="finder"></param>
        /// <returns></returns>
        public static int AddMyFile(string repname, FilesImported finder)
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
                return -1;
            }

            return ret.tag;
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
            sql.Append(" UPDATE " + sDefaultSchema + "excel_utility SET stats = " + (int)finder.status);
            if (finder.status == ExcelUtility.Statuses.InProcess)
            {
                sql.Append(", dat_start = " + Utils.EStrNull(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")));
            }
            else if (finder.status == ExcelUtility.Statuses.Success || finder.status == ExcelUtility.Statuses.Failed)
            {
                sql.Append(", dat_out = " + Utils.EStrNull(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")));
            }

            
            if (finder.exc_path != "") sql.Append(", exc_path = " + Utils.EStrNull(finder.exc_path));
            sql.Append(" WHERE nzp_exc =" + finder.nzp_exc);

            ret = ExecSQL(conn_db, sql.ToString(), true);

            conn_db.Close();
            sql.Remove(0, sql.Length);

            return ret;
        }

        /// <summary>
        /// Отчет по разделу 3 из формата загрузок (<название_файла>.kvar.log)
        /// </summary>
        /// <param name="conn_db"></param>
        /// <param name="finder"></param>
        /// <param name="strResult"></param>
        /// <returns></returns>
        public Returns FileResultOfLoad(StringBuilder strResult)
        {
            Returns ret = new Returns();

            string sql = "";

            try
            {
                #region 3.1.	Заголовок файла
                sql =
                    "SELECT h.org_name as org_name, h.branch_name as branch_name, h.inn as inn, h.kpp as kpp, h.file_no as file_no, h.file_date as file_date," +
                    " h.sender_phone as sender_phone, h.sender_fio as sender_fio, h.row_number as row_number, v.version_name as version, i.nzp_status as nzp_status " +
                    " FROM " + Points.Pref + DBManager.sUploadAliasRest + "file_head h, " + Points.Pref + DBManager.sUploadAliasRest + "file_versions v, " + 
                    Points.Pref + DBManager.sUploadAliasRest + "files_imported i" +
                    " WHERE h.nzp_file =" + _finder.nzp_file + " AND v.nzp_version = i.nzp_version AND i.nzp_file = " + _finder.nzp_file;

                var dt = ClassDBUtils.OpenSQL(sql, _conDb).resultData;

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
                    return ret;
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
                    " SELECT ukas, id, open_date, close_date, fam, ima, otch, birth_date, dom_id, nkvar, nkvar_n, nzp_status" +
                    " FROM " + Points.Pref + DBManager.sUploadAliasRest + "file_kvar" +
                    " WHERE nzp_file =" + _finder.nzp_file;

                var dtk = ClassDBUtils.OpenSQL(sql, _conDb).resultData;

                foreach (DataRow r in dtk.Rows)
                {
                    int status;
                    if (r["nzp_status"] == DBNull.Value) status = 0;
                    else status = Convert.ToInt32(r["nzp_status"]);

                    string answer = "";
                    if (Convert.ToInt32(dt.Rows[0]["nzp_status"]) == 3)
                        answer = "Загружен с ошибками";
                    else if (Convert.ToInt32(dt.Rows[0]["nzp_status"]) == 1 || Convert.ToInt32(dt.Rows[0]["nzp_status"]) == 2)
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
        /// Ф-ция заполнения пустых полей таблиц file_*
        /// </summary>
        /// <param name="err"></param>
        private void SetValueForZeroFields(StringBuilder err)
        {
            #region file_dom
            //заполняем столбец ndom
            string sql = "update " + Points.Pref + DBManager.sUploadAliasRest + "file_dom set ndom = '-' where ndom is null";
            ClassDBUtils.ExecSQL(sql, _conDb);

            sql = "update " + Points.Pref + DBManager.sUploadAliasRest + "file_dom set nkor = '-' where nkor is null";
            ClassDBUtils.ExecSQL(sql, _conDb);

            //заполняем столбец rajon
            sql = "update " + Points.Pref + DBManager.sUploadAliasRest + "file_dom set rajon = '-' where rajon is null";
            ClassDBUtils.ExecSQL(sql, _conDb);

            //заполняем столбец town
            sql = "update " + Points.Pref + DBManager.sUploadAliasRest + "file_dom set town = '-' where town is null";
#if PG
            var res = ClassDBUtils.ExecSQL(sql, _conDb, true);
            if (res.resultCode != 0)
                throw new Exception(res.resultMessage);

            if (res.resultAffectedRows != 0)
#else
            ExecSQL(_conDb, sql, true);
            if (ClassDBUtils.GetAffectedRowsCount(_conDb) != 0)
#endif
            {
                err.Append("Имеются дома с незаполненным полем город/район в количестве " +
#if PG
 res.resultAffectedRows
#else
 ClassDBUtils.GetAffectedRowsCount(_conDb)
#endif
 + Environment.NewLine);
            }

            sql = " update " + Points.Pref + DBManager.sUploadAliasRest +
                  " file_dom set rajon = town where rajon='-' and nzp_file = " + _finder.nzp_file;
            ClassDBUtils.ExecSQL(sql, _conDb);


            //заполняем столбец ulica
            sql = "update " + Points.Pref + DBManager.sUploadAliasRest + "file_dom set ulica = '-' where ulica is null";
            ClassDBUtils.ExecSQL(sql, _conDb);

            #endregion

            //заполняем столбец vill в таблице file_mo
            sql = "UPDATE " + Points.Pref + DBManager.sUploadAliasRest + "file_mo SET vill = cast( mo_name as char (50)) " +
                  "WHERE vill is null";
            ClassDBUtils.ExecSQL(sql, _conDb);
        }



        /// <summary>
        /// Ф-ция сопоставления адресного пространства загруженных домов по коду КЛАДР
        /// </summary>
        /// <returns></returns>
        public void SetLinksByKladr(StringBuilder err)
        {

            try
            {
                string sql;
#if PG
                sql =
                    " UPDATE " + Points.Pref + DBManager.sUploadAliasRest + "file_dom d " +
                    " SET (nzp_ul, nzp_raj, nzp_town) = " +
                    " ( (" +
                    "      SELECT u.nzp_ul " +
                    "      FROM " + Points.Pref + DBManager.sDataAliasRest + "s_ulica u " +
                    "      WHERE u.soato = d.kod_kladr " +
                    "           AND u.soato <> '0' " +
                    "    ), " +
                    "    ( " +
                    "      SELECT u.nzp_raj " +
                    "      FROM " + Points.Pref + DBManager.sDataAliasRest + "s_ulica u " +
                    "      WHERE u.soato = d.kod_kladr " +
                    "           AND u.soato <> '0' " +
                    "    ), " +
                    "    ( " +
                    "      SELECT r.nzp_town " +
                    "      FROM " + Points.Pref + DBManager.sDataAliasRest + "s_ulica u, " +
                                    Points.Pref + DBManager.sDataAliasRest + "s_rajon r " +
                    "      WHERE u.nzp_raj = r.nzp_raj " +
                    "        AND u.soato = d.kod_kladr " +
                    "           AND u.soato <> '0' " +
                    "     ) " +
                    " ) " +
                    " WHERE Length(d.kod_kladr) > 0 " +
                    "       AND nzp_file = " + _finder.nzp_file;

#else
                sql =
                    " UPDATE " + Points.Pref + DBManager.sUploadAliasRest + "file_dom " +
                    " SET (nzp_ul, nzp_raj, nzp_town) = " +
                    " (( " +
                    "   SELECT u.nzp_ul, u.nzp_raj, r.nzp_town " +
                    "   FROM " + Points.Pref + "_data" + tableDelimiter + "s_ulica u,  " +
                    Points.Pref + "_data" + tableDelimiter + "s_rajon r " +
                    "   WHERE u.soato = " + Points.Pref + DBManager.sUploadAliasRest + "file_dom.kod_kladr " +
                    "          AND u.nzp_raj = r.nzp_raj " +
                    " )) " +
                    " WHERE Length(" + Points.Pref + DBManager.sUploadAliasRest + "file_dom.kod_kladr) > 0 " +
                    "       AND nzp_file = " + _finder.nzp_file;

#endif

                ClassDBUtils.ExecSQL(sql, _conDb, ClassDBUtils.ExecMode.Exception);
            }

            catch (Exception ex)
            {
                MonitorLog.WriteLog(
                    "Ошибка при сопоставлении улиц по коду КЛАДР в функции SetLinksByKladr : " + ex.Message +
                    ex.StackTrace, MonitorLog.typelog.Error, true);
                err.Append("Ошибка при сопоставлении улиц по коду КЛАДР." + Environment.NewLine);
            }
        }

        /// <summary>
        /// Заполняем file_services из своих таблиц, если в файле не было 13 секции
        /// </summary>
        public void LoadOur13Section(IDbConnection conn_db, FilesImported _finder)
        {
            string sql =
                "INSERT INTO " + Points.Pref + DBManager.sUploadAliasRest + "file_services (id_serv, service, service2, nzp_measure, ed_izmer, type_serv, nzp_file, nzp_serv)" +
                "SELECT  nzp_serv, service, service_name, nzp_measure, ed_izmer, 0, " + _finder.nzp_file + ", nzp_serv from " + Points.Pref + "_kernel" + tableDelimiter + "services " +
                "WHERE nzp_serv IN" +
                "(SELECT nzp_serv FROM " + Points.Pref + DBManager.sUploadAliasRest+ "file_serv WHERE nzp_file = " + _finder.nzp_file + ")";

            ClassDBUtils.ExecSQL(sql, conn_db, ClassDBUtils.ExecMode.Exception);

            sql = "insert into " + Points.Pref + DBManager.sUploadAliasRest+ "file_services ( id_serv, service, service2, nzp_measure, ed_izmer, type_serv, nzp_file, nzp_serv)" +
                                                     "select nzp_serv, service, service_name, nzp_measure, ed_izmer, 0, " + _finder.nzp_file + ", nzp_serv from " + Points.Pref + "_kernel" + tableDelimiter + "services " +
                                                     "where nzp_serv in" +
                                                     "(select nzp_serv from " + Points.Pref + DBManager.sUploadAliasRest+ "file_odpu where nzp_file = " + _finder.nzp_file + ") and" +
                                                     " nzp_serv not in (select nzp_serv from " + Points.Pref + DBManager.sUploadAliasRest+ "file_services where nzp_file = " + _finder.nzp_file + ")";

            ClassDBUtils.ExecSQL(sql,conn_db, ClassDBUtils.ExecMode.Exception);

            sql = "insert into " + Points.Pref + DBManager.sUploadAliasRest+ "file_services ( id_serv, service, service2, nzp_measure, ed_izmer, type_serv, nzp_file, nzp_serv)" +
                                                      "select  nzp_serv, service, service_name, nzp_measure, ed_izmer, 0, " + _finder.nzp_file + ", nzp_serv from " + Points.Pref + "_kernel" + tableDelimiter + "services " +
                                                      "where nzp_serv in" +
                                                      "(select nzp_serv from " + Points.Pref + DBManager.sUploadAliasRest+ "file_ipu where nzp_file = " + _finder.nzp_file + ") and" +
                                                      " nzp_serv not in (select nzp_serv from " + Points.Pref + DBManager.sUploadAliasRest+ "file_services where nzp_file = " + _finder.nzp_file + ")";

            ClassDBUtils.ExecSQL(sql, conn_db, ClassDBUtils.ExecMode.Exception);

            sql =
                "insert into " + Points.Pref + DBManager.sUploadAliasRest+ "file_services ( id_serv, service, service2, nzp_measure, ed_izmer, type_serv, nzp_file, nzp_serv)" +
                                     "select nzp_serv, service, service_name, nzp_measure, ed_izmer, 0, " + _finder.nzp_file + ", nzp_serv from " + Points.Pref + "_kernel" + tableDelimiter + "services " +
                                     "where nzp_serv in" +
                                     "(select nzp_serv from " + Points.Pref + DBManager.sUploadAliasRest+ "file_servp where nzp_file = " + _finder.nzp_file + ") and" +
                                     " nzp_serv not in (select nzp_serv from " + Points.Pref + DBManager.sUploadAliasRest+ "file_services where nzp_file = " + _finder.nzp_file + ")";

            ClassDBUtils.ExecSQL(sql, conn_db, ClassDBUtils.ExecMode.Exception);

        }

        /// <summary>
        /// Ф-ция  заполнения справочника единиц измерения услуг
        /// </summary>
        /// <param name="conn_db"></param>
        /// <param name="finder"></param>
        /// <returns></returns>
        public Returns FillMeasure(IDbConnection conn_db, FilesImported _finder)
        {
            Returns ret = Utils.InitReturns();
            string sql = "select count(*) as kol from " + Points.Pref + DBManager.sUploadAliasRest+ "file_measures where nzp_file =" + _finder.nzp_file;
            //var dt = ClassDBUtils.OpenSQL(sql, _conDb);
            //if (Convert.ToInt32(dt.resultData.Rows[0]["kol"]) == 0)
            object obj = DBManager.ExecScalar(_conDb, sql, out ret, true);
            if (!ret.result) throw new Exception("Ошибка в FillMeasure при определении количества строк в file_measures");
            int count = (obj != null ? Convert.ToInt32(obj) : 0);
            if(count == 0)
            {
                // считаем что люди заполнили справочник из стандартного приложения

                sql =
                    " insert into " + Points.Pref + DBManager.sUploadAliasRest+ "file_measures(id_measure,measure, nzp_file, nzp_measure) " +
                    " select idiotsky_kod, measure_long, " + _finder.nzp_file + ", nzp_measure " +
                    " from " + Points.Pref + "_kernel" + tableDelimiter + "s_measure " +
                    " where " + sNvlWord + "(idiotsky_kod, 0)>0 and idiotsky_kod is not null  ";

                ret = ExecSQL(conn_db, sql, true);
                ExecSQL(conn_db, DBManager.sUpdStat + "  " + Points.Pref + DBManager.sUploadAliasRest + "file_measures", true);
            }
            return ret;
        }


        private Returns FillTypeNedopos(IDbConnection conn_db, FilesImported _finder)
        {
            Returns ret = Utils.InitReturns();
            string sql = "SELECT count(*) as kol FROM " + Points.Pref + DBManager.sUploadAliasRest + "file_typenedopost WHERE nzp_file =" + _finder.nzp_file;
            var dt = ClassDBUtils.OpenSQL(sql, _conDb);
            if (Convert.ToInt32(dt.resultData.Rows[0]["kol"]) == 0)
            {
                // считаем что люди заполнили справочник из стандартного приложения
                sql =
                    " INSERT INTO " + Points.Pref + DBManager.sUploadAliasRest + "file_typenedopost (nzp_file, type_ned, ned_name, nzp_kind) " +
                    " SELECT " + _finder.nzp_file + ", nzp_kind, name, nzp_kind " +
                    " FROM " + Points.Pref + sDataAliasRest + "upg_s_kind_nedop " +
                    " where kod_kind = 1 and nzp_kind in" +
                    "   (SELECT DISTINCT type_ned FROM " + Points.Pref + sUploadAliasRest + "file_nedopost" +
                    "   WHERE nzp_file = " + _finder.nzp_file + ") ";

                ret = ExecSQL(conn_db, sql, true);
                ExecSQL(conn_db, DBManager.sUpdStat + "  " + Points.Pref + DBManager.sUploadAliasRest + "file_nedopost", true);
            }
            return ret;
        }

        #endregion
    }


}
