using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using Castle.Core.Internal;
using FastReport;
using STCLINE.KP50;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;
using STCLINE.KP50.Utility;

namespace Bars.KP50.Load.Obninsk
{

    public class FileObnCounters
    {
        public int RowsCount { get; set; }
        public int NzpDownload { get; set; }
    }

    /// <summary>
    /// Класс протокола загрузки файла
    /// </summary>
    public class DbObnLoadProtokol : DataBaseHead
    {
        private readonly IDbConnection _connDB;

        /// <summary>
        /// Таблица счетчиков, которые не были сопоставлены в ходе разбора 
        /// </summary>
        public DataTable UnrecognizedCounters;
        /// <summary>
        /// Список некорректных строк
        /// </summary>
        public DataTable UncorrectRows;
        /// <summary>
        /// Комментарии в ходе загрузки
        /// </summary>
        private readonly DataTable _comment;
        /// <summary>
        /// Количество добавленных строк
        /// </summary>
        public int CountInsertedRows;
        /// <summary>
        /// Описатель файла
        /// </summary>
        private readonly FileObnCounters _fileArgs;

        public DbObnLoadProtokol(IDbConnection connDB, FileObnCounters fileArgs)
        {
            UnrecognizedCounters = new DataTable();
            UnrecognizedCounters.TableName = "cnt";
            UnrecognizedCounters.Columns.Add(new DataColumn("nzp_counter"));
            UnrecognizedCounters.Columns.Add(new DataColumn("value"));
            _connDB = connDB;
            _fileArgs = fileArgs;
            _comment = new DataTable();
            _comment.TableName = "kom";
            _comment.Columns.Add(new DataColumn("comment"));

            UncorrectRows = new DataTable();
            UncorrectRows.TableName = "uncor";
            UncorrectRows.Columns.Add(new DataColumn("sourceString"));

        }


        /// <summary>
        /// Добавление описателя задания в таблицу Мои Файлы
        /// </summary>
        /// <param name="finder"></param>
        /// <returns></returns>
        public Returns AddMyFile(ExcelUtility finder)
        {

            string sql = "insert into " + sDefaultSchema +
                         "excel_utility (nzp_user, stats, prms, dat_in, rep_name, exc_comment, dat_today, exc_path,is_shared) " +
                         " values (" + finder.nzp_user +
                         ", " + (int)finder.status +
                         ", " + Utils.EStrNull(finder.prms, "empty") +
                         "," + Utils.EStrNull(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")) +
                         ", " + Utils.EStrNull(finder.rep_name) +
                         ", " + Utils.EStrNull(finder.exec_comment) +
                         ", " + Utils.EStrNull(DateTime.Now.ToShortDateString()) +
                         ", " + Utils.EStrNull(finder.exc_path) +
                         ", " + finder.is_shared + ")";

            Returns ret = ExecSQL(_connDB, sql, true);
            if (!ret.result) return ret;

            int id = GetSerialValue(_connDB);

            if (finder.status == ExcelUtility.Statuses.InProcess)
            {
                ExecSQL(_connDB, "update excel_utility set dat_start = dat_in where nzp_exc = " + id, true);
            }

            ret.tag = id;
            return ret;
        }

        /// <summary>
        /// Смена статуса задания
        /// </summary>
        /// <param name="finder"></param>
        /// <returns></returns>
        public Returns SetMyFileState(ExcelUtility finder)
        {

            string sql = " update " + sDefaultSchema + "excel_utility set stats = " + (int)finder.status;
            if (finder.status == ExcelUtility.Statuses.InProcess)
            {
                sql += ", dat_start = " + Utils.EStrNull(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            }
            else if (finder.status == ExcelUtility.Statuses.Success || finder.status == ExcelUtility.Statuses.Failed)
            {
                sql += ", dat_out = " + Utils.EStrNull(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            }
            if (finder.exc_path != "") sql += ", exc_path = " + Utils.EStrNull(finder.exc_path);
            sql += " where nzp_exc =" + finder.nzp_exc;

            Returns ret = ExecSQL(_connDB, sql, true);


            return ret;
        }


        public void AddComment(string comment)
        {
            DataRow dr = _comment.NewRow();
            dr["comment"] = comment;
            _comment.Rows.Add(dr);
        }

        public void AddUncorrectedRows(string sourceSring)
        {
            DataRow dr = UncorrectRows.NewRow();
            dr["sourceString"] = sourceSring;
            UncorrectRows.Rows.Add(dr);
        }

        /// <summary>
        /// Добавить в список нераспознанных счетчиков
        /// </summary>
        /// <param name="nzpCounter"></param>
        /// <param name="valCnt"></param>
        public void AddBadCounter(int nzpCounter, decimal valCnt)
        {
            DataRow dr = UnrecognizedCounters.NewRow();
            dr["nzp_counter"] = nzpCounter;
            dr["value"] = valCnt;
            UnrecognizedCounters.Rows.Add(dr);
        }


        /// <summary>
        /// Получение протокола к загрузке
        /// </summary>
        /// <returns></returns>
        public Returns GetProtocolWwb(int nzpUser, int nzpReestr)
        {
            Returns ret = Utils.InitReturns();
            if (!ret.result)
            {
                return ret;
            }

            #region Формирование протокола

            //  запись в БД о постановки в поток(статус 0)
            ret = AddMyFile(new ExcelUtility
            {
                nzp_user = nzpUser,
                status = ExcelUtility.Statuses.InProcess,
                rep_name = "Загрузка счетчиков в биллинговую систему ",
                is_shared = 1
            });
            if (!ret.result) return ret;


            int nzpExc = ret.tag;
            //Имя файла отчета
            int tag = 0;
            string statusName = "Успешно";

            var rep = new Report();

            
            if (UnrecognizedCounters.Rows.Count > 0 || _comment.Rows.Count > 0)
            {
                SetProcent(100, (int)ExcelUtility.Statuses.Failed);
                tag = 2;
                statusName = "Загружено с ошибками";
            }
            if (UncorrectRows.Rows.Count > 0)
            {
                tag = 2;
                statusName = "Загружено с ошибками";
            }
           
            var env = new EnvironmentSettings();
            env.ReportSettings.ShowProgress = false;

            DataSet fDataSet = new DataSet();
            fDataSet.Tables.Add(UnrecognizedCounters);
            fDataSet.Tables.Add(_comment);
            fDataSet.Tables.Add(UncorrectRows);

            string template = "protocol_workwithbank.frx";
            rep.Load(PathHelper.GetReportTemplatePath(template));

            rep.RegisterData(fDataSet);
            rep.GetDataSource("ls").Enabled = true;
            rep.GetDataSource("cnt").Enabled = true;
            rep.GetDataSource("kom").Enabled = true;
            rep.GetDataSource("uncor").Enabled = true;
            rep.SetParameterValue("status", statusName);
            rep.SetParameterValue("count_rows", _fileArgs.RowsCount);
            rep.SetParameterValue("count_inserted_rows", CountInsertedRows);
            
            rep.Prepare();

            var exportXls = new FastReport.Export.OoXML.Excel2007Export();
            string fileName = "protocol_WWB" + DateTime.Now.ToShortDateString().Replace(".", "_") + "_" +
                              DateTime.Now.ToShortTimeString().Replace(":", "_") + ".xlsx";
            exportXls.ShowProgress = false;
            MonitorLog.WriteLog(fileName, MonitorLog.typelog.Info, 20, 201, true);
            try
            {
                exportXls.Export(rep, Path.Combine(STCLINE.KP50.Global.Constants.ExcelDir, fileName));
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog(ex.Message, MonitorLog.typelog.Error, true);
            }

            rep.Dispose();


            //перенос  на ftp сервер
            if (InputOutput.useFtp)
            {
                fileName = InputOutput.SaveOutputFile(STCLINE.KP50.Global.Constants.ExcelDir + fileName);
            }




            ret = ExecSQL(_connDB, "UPDATE " + Points.Pref + sDataAliasRest + "tula_reestr_downloads " +
                                   " SET nzp_exc=" + nzpExc +
                                   " WHERE nzp_download=" + _fileArgs.NzpDownload, true);
            ret.tag = tag;
            SetMyFileState(new ExcelUtility
            {
                nzp_exc = nzpExc,
                status = ExcelUtility.Statuses.Success,
                exc_path = fileName
            });

            #endregion


            return ret;
        }

        /// <summary>
        /// Установка процента прогресса
        /// </summary>
        /// <param name="proc"></param>
        /// <param name="status"></param>
        /// <returns></returns>
        public bool SetProcent(double proc, int status)
        {
            bool result = true;
            string sql;
            if (proc > 0)
            {
                sql = " UPDATE " + Points.Pref + sDataAliasRest + "tula_reestr_downloads " +
                      " SET proc=(" + proc + ") " +
                      " WHERE nzp_download=" + _fileArgs.NzpDownload;
                Returns ret = ExecSQL(_connDB, sql, true);
                if (!ret.result)
                {
                    MonitorLog.WriteLog("Ошибка обновления статуса загрузки реестра", MonitorLog.typelog.Info, 20, 201, true);
                    result = false;
                }
            }
            if (status != -999)
            {
                sql = " UPDATE " + Points.Pref + sDataAliasRest + "tula_reestr_downloads " +
                      " SET status=(" + status + ") " +
                      " WHERE nzp_download=" + _fileArgs.NzpDownload;
                if (!ExecSQL(_connDB, sql, true).result)
                {
                    MonitorLog.WriteLog("Ошибка обновления статуса загрузки реестра", MonitorLog.typelog.Info, 20, 201, true);
                    result = false;
                }
            }

            return result;
        }
    }

    /// <summary>Сводный отчет по начислениям для Тулы</summary>
    public class DbLoadObnCounters
    {
        private IDbConnection _connDB;
        /// <summary>
        /// Протокол загрузки файла
        /// </summary>
        private DbObnLoadProtokol _loadProtokol;

        /// <summary>
        /// Массив максимально-допустимых разниц показаний счетчиков
        /// </summary>
        private Dictionary<string, Dictionary<int, decimal>> _maxDiffBetweenValues;

        
        /// <summary>
        /// Описатель файла
        /// </summary>
        private FileObnCounters _fileArgs;

        /// <summary>
        /// Загрузка реестра оплат
        /// </summary>
        /// <param name="fileName">Имя файла загружаемого пользователем</param>
        /// <param name="fileLocal">Имя внутреннего временного файла</param>
        /// <param name="nzpUser">Код пользователя</param>
        /// <returns></returns>
        public Returns UploadFile(string fileName, string fileLocal, int nzpUser)
        {
            _connDB = DBManager.GetConnection(STCLINE.KP50.Global.Constants.cons_Kernel);
            Returns ret = DBManager.OpenDb(_connDB, true);
            if (!ret.result)
            {
                return ret;
            }
            _fileArgs  = new FileObnCounters();

            Utils.setCulture(); // установка региональных настроек
            _loadProtokol = new DbObnLoadProtokol(_connDB, _fileArgs);
            try
            {

                //запись в реестр загрузок
                InsertIntoObnCountersFiles(_connDB, fileName, nzpUser);

                _loadProtokol.SetProcent(0, (int) ExcelUtility.Statuses.InProcess);


                #region Загрузка данных из файла

                var fs = new FileStream(fileLocal, FileMode.Open, FileAccess.Read);
                var tableCounters = Utils.ConvertDBFtoDataTable(fs, out ret);
                fs.Close();

                if (!ret.result)
                {

                    MonitorLog.WriteLog("Ошибка разбора файла со счетчиками" + fileName,
                        MonitorLog.typelog.Error, 20, 201, true);
                    _loadProtokol.AddComment("Ошибка разбора файла со счетчиками" + fileName);
                    return ret;
                }

                //удаляем промежуточный файл на хосте
                if (InputOutput.useFtp) File.Delete(fileLocal);



                #endregion

              

                //Разбор реестра
                ret = ParseTableCounters(tableCounters);

                if (!ret.result) throw  new UserException(ret.text);

                //записываем данные в систему: pack,pack_ls,pu_vals 
                ret = SaveCountersInBase();
                if (!ret.result)
                {
                    MonitorLog.WriteLog("Ошибка записи счетчиков в систему",
                        MonitorLog.typelog.Error, 20, 201, true);

                }
                else
                {
                    _loadProtokol.SetProcent(100, (int) ExcelUtility.Statuses.Success);
                }

            }
            catch (UserException ex)
            {
                ret.result = false;
                MonitorLog.WriteLog("Ошибка " + ex.Message, MonitorLog.typelog.Error, 20, 201, true);
                _loadProtokol.AddComment(ex.Message);
            }
            catch (Exception ex)
            {
                ret.result = false;
                MonitorLog.WriteLog("Ошибка " + ex.Message, MonitorLog.typelog.Error, 20, 201, true);
                RollbackReestr();
            }
            finally
            {

                _loadProtokol.GetProtocolWwb(nzpUser, _fileArgs.NzpDownload);
                _connDB.Close();

            }

            return ret;
        }


        /// <summary>
        /// Сохранение записи о реестре в Базу данных
        /// </summary>
        /// <param name="connDB">Подключение</param>
        /// <param name="fileName">Имя Файла</param>
        /// <param name="nzpUser">Код пользователя</param>
        private void InsertIntoObnCountersFiles(IDbConnection connDB, string fileName, int nzpUser)
        {

            var dateD = DateTime.Now.ToString("yyyy.MM.dd HH:mm:ss").Replace('.', '-');

            string sqlStr = "INSERT INTO " + Points.Pref + DBManager.sDataAliasRest + "simple_rashod_files " +
                            "(file_name, created_on, created_by) " +
                            "VALUES " +
                            " ( '" + fileName + "'," + nzpUser + ",'" + dateD + "' )";

            ClassDBUtils.ExecSQL(sqlStr, connDB);
            _fileArgs.NzpDownload = DBManager.GetSerialValue(connDB);

        }


        /// <summary>
        /// Разбор Таблицы со счетчиками и сохранение в базе данных 
        /// </summary>
        /// <param name="tableCounters">Таблица, считанная из файла</param>
        /// <returns></returns>
        private Returns ParseTableCounters(DataTable tableCounters)
        {
            if (tableCounters == null)
            {
                return new Returns(false, "В таблице счетчиков отсуствует записи");
            }

            _fileArgs.RowsCount = tableCounters.Rows.Count;
            Returns ret = Utils.InitReturns();

          
            foreach (DataRow dr in tableCounters.Rows)
            {
                string uk = dr["company"].ToString().Trim();
                string datePay = dr["date_pay"].ToString().Trim();
                string pkod = dr["paccount"].ToString().Trim();

                foreach (DataColumn dc in tableCounters.Columns)
                {
                    string colName = dc.ColumnName.ToLower();
                    int nzpServ = -1;
                    if (colName == "company" ||
                        colName == "date_pay" ||
                        colName == "paccount") continue;

                    if (colName.IndexOf("cold", StringComparison.Ordinal) > -1)
                    {
                        nzpServ = 6;
                    }
                    else if (colName.IndexOf("hot", StringComparison.Ordinal) > -1)
                    {
                        nzpServ = 9;
                    }
                    else if (colName.IndexOf("elec_d", StringComparison.Ordinal) > -1)
                    {
                        nzpServ = 25;
                    }
                    else if (colName.IndexOf("elec_n", StringComparison.Ordinal) > -1)
                    {
                        nzpServ = 210;
                    }
                    string num = colName.Substring(colName.Length - 1);

                    string sql = "insert into " + Points.Pref +
                                 "simple_counters(nzp_reestr, uk, date_pay, paccount, nzp_serv, num, val_cnt)" +
                                 "values(" + _fileArgs.NzpDownload + ",'" + uk + "','" + datePay + "'," +
                                 pkod + "," + nzpServ + "," + num + "," + dr[dc.ColumnName] + ")";
                    ret = DBManager.ExecSQL(_connDB, sql, true);
                }
            }
            return ret;
        }

        /// <summary>
        /// Сохранение счетчиков в базе данных
        /// </summary>
        /// <returns></returns>
        private Returns SaveCountersInBase()
        {
            Returns ret = Utils.InitReturns();
            _maxDiffBetweenValues = GetMaxDiffBetweenValuesDict();

            #region Собираем данные во временную таблицу
            string sql = "Create temp table t_counts (nzp_kvar integer, " +
                         " nzp_serv integer," +
                         " nzp_wp integer," +
                         " bad_cnt integer default 0," +
                         " paccount " + DBManager.sDecimalType + "(13,0)," +
                         " val_cnt " + DBManager.sDecimalType + "(14,4)," +
                         " last_val " + DBManager.sDecimalType + "(14,4)," +
                         " rashod " + DBManager.sDecimalType + "(14,4)," +
                         " nzp_counter integer)" + DBManager.sUnlogTempTable;
            DBManager.ExecSQL(_connDB, sql, true);

            sql = " insert into t_counts(nzp_kvar, nzp_wp, paccount, nzp_serv, val_cnt) " +
                  " select k.nzp_kvar, k.nzp_wp, paccount, nzp_serv, sum(val_cnt) as val_cnt " +
                  " from "+Points.Pref + DBManager.sDataAliasRest+"simple_reestr a left outer join " +
                  "      "+Points.Pref+DBManager.sDecimalType+"kvar k "+
                  " on a.paccount=k.pkod "+
                  " where nzp_reestr = " + _fileArgs.NzpDownload;
            DBManager.ExecSQL(_connDB, sql, true);

            foreach (_Point p in Points.PointList)
            {

                sql = " update t_counts set nzp_counter = (select max(nzp_counter) " +
                      " from " + p.pref + DBManager.sDataAliasRest + "counters_spis s" +
                      " where t_counts.nzp_kvar=s.nzp_kvar " +
                      " and t_counts.nzp_serv=s.nzp_serv)" +
                      " where nzp_wp=" + p.nzp_wp;
                DBManager.ExecSQL(_connDB, sql, true);
                
                sql = " update t_counts set last_val = (select max(val_cnt) " +
                      " from " + p.pref + DBManager.sDataAliasRest + "counters s" +
                      " where t_counts.nzp_counter=s.nzp_counter)" +
                      " where nzp_counter is not null and nzp_wp=" + p.nzp_wp;
                DBManager.ExecSQL(_connDB, sql, true);

                sql = " update t_counts set rashod = val-last_val where last_val is not null ";
                DBManager.ExecSQL(_connDB, sql, true);

                #region Проверки
                sql = " update t_counts set bad_cnt = 1 where rashod<0 and nzp_wp=" + p.nzp_wp; 
                DBManager.ExecSQL(_connDB, sql, true);

                sql = " update t_counts set bad_cnt = 1 " +
                      " where rashod>" + GetDiffByServ(p.pref,9) + " and nzp_serv=9 and nzp_wp=" + p.nzp_wp; 
                DBManager.ExecSQL(_connDB, sql, true);

                sql = " update t_counts set bad_cnt = 1 " +
                      " where rashod>" + GetDiffByServ(p.pref, 6) + " and nzp_serv=6 and nzp_wp=" + p.nzp_wp; 
                DBManager.ExecSQL(_connDB, sql, true);

                sql = " update t_counts set bad_cnt = 1 " +
                      " where rashod>" + GetDiffByServ(p.pref, 25) + " and nzp_serv=25 and nzp_wp=" + p.nzp_wp; 
                DBManager.ExecSQL(_connDB, sql, true);

                sql = " update t_counts set bad_cnt = 1 " +
                      " where rashod>" + GetDiffByServ(p.pref, 210) + " and nzp_serv=210 and nzp_wp=" + p.nzp_wp; 
                DBManager.ExecSQL(_connDB, sql, true);

                #endregion
            }
            sql = " update t_counts set bad_cnt = 1 where nzp_counters is null";
            DBManager.ExecSQL(_connDB, sql, true);
            #endregion

            #region Выбираем проблеммные счетчики

            sql = " select * from t_counts  " +
                  " where bad_cnt =1 ) ";
            var badCounterTable = DBManager.ExecSQLToTable(_connDB, sql);
            foreach (DataRow dr in badCounterTable.Rows)
            {
                if (dr["nzp_counter"] == DBNull.Value)
                    _loadProtokol.AddComment("Счетчик не зарегистрирован в системе платежный код  " + dr["pkod"]);
                else if (dr["rashod"] != DBNull.Value)
                    _loadProtokol.AddComment("Слишком большое показание " + dr["rashod"] +
                                             " для счетчика по услуге " + dr["nzp_serv"] +
                                             ", данные не загружены." +
                                             " платежный код  " + dr["paccount"]);

            }

            #endregion

            #region Добавляем хорошие счетчики

            sql = " select a.nzp_wp, p.pref " +
                  " from t_counts a, " +Points.Pref + DBManager.sKernelAliasRest + "s_point p" +
                  " where a.nzp_wp=p.nzp_wp " +
                  " group by 1";
            var preftable = DBManager.ExecSQLToTable(_connDB, sql);
            foreach (DataRow dr in preftable.Rows)
            {
                string localData = dr["pref"].ToString().Trim() + DBManager.sDataAliasRest;

                sql = " insert into " + localData + "counters " +
                      " (nzp_counter, num_ls, nzp_kvar, nzp_cnttype, nzp_serv, num_cnt, val_cnt, dat_uchet, dat_when, ist)" +
                      " select a.nzp_counter, k.num_ls, a.nzp_kvar, s.nzp_cnttype, a.nzp_serv, " +
                      " s.num_cnt, a.val_cnt, a.dat_uchet, "+DBManager.sCurDate+", 6 " +
                      " from t_counts a, " + localData + "kvar k, " + localData + "counters_spis s " +
                      " where a.nzp_kvar=k.nzp_kvar and a.nzp_counter=s.nzp_counter " +
                      " and nzp_wp = "+dr["nzp_wp"];
                DBManager.ExecSQL(_connDB, sql, true);
            }
            #endregion


            return ret;
        }

        /// <summary>
        /// Откат 
        /// </summary>
        private void RollbackReestr()
        {

        }

        /// <summary>
        /// заполняем макимальную разницу показаний для отдельного банка
        /// </summary>
        /// <param name="pref">префикс банка</param>
        /// <returns></returns>
        private Dictionary<int, decimal> GetMaxDiffBetweenValuesDictForOneBank(string pref)
        {
            var resDictionary = new Dictionary<int, decimal>
            {
                {25, GetMaxDiffBetweenValuesOneServ(pref, 2081)},
                {9, GetMaxDiffBetweenValuesOneServ(pref, 2082)},
                {6, GetMaxDiffBetweenValuesOneServ(pref, 2083)},
                {10, GetMaxDiffBetweenValuesOneServ(pref, 2084)}
            };
            return resDictionary;
        }

        /// <summary>
        ///  заполняем макимальную разницу показаний по одной услуге для отдельного банка
        /// </summary>
        /// <param name="pref">префикс банка</param>
        /// <param name="param">код параметра</param>
        /// <returns></returns>
        private decimal GetMaxDiffBetweenValuesOneServ(string pref, int param)
        {
            Returns ret;
            string sql =
                " SELECT " + DBManager.sNvlWord + "(max(p.val_prm " + DBManager.sConvToNum + "), 10000) " +
                " FROM " + pref + DBManager.sKernelAliasRest + "prm_name pn " +
                " LEFT JOIN " + pref + DBManager.sDataAliasRest + "prm_10 p ON pn.nzp_prm = p.nzp_prm " +
                " WHERE pn.nzp_prm = " + param;
            decimal result = CollectionExtensions.IsNullOrEmpty(DBManager.ExecScalar(_connDB, null, sql, out ret, true).ToString()) ?
                10000m : Convert.ToDecimal(DBManager.ExecScalar(_connDB, null, sql, out ret, true));
            return result;
        }



        /// <summary>
        /// заполняем максимальную разницу показаний по верхнему и всем локальным банкам
        /// </summary>
        /// <returns></returns>
        private Dictionary<string, Dictionary<int, decimal>> GetMaxDiffBetweenValuesDict()
        {
            var resDictionary = new Dictionary<string, Dictionary<int, decimal>>();
            string sql =
                " SELECT distinct trim(bd_kernel) as bd_kernel " +
                " FROM " + Points.Pref + DBManager.sKernelAliasRest + "s_point";
            var pref = ClassDBUtils.OpenSQL(sql, _connDB, null).resultData;
            foreach (DataRow r in pref.Rows)
                resDictionary.Add(r["bd_kernel"].ToString(),
                    GetMaxDiffBetweenValuesDictForOneBank(r["bd_kernel"].ToString()));
            return resDictionary;
        }

        /// <summary>
        /// Получить максимально допустимую разницу между показаниями ПУ
        /// </summary>
        /// <param name="pref"></param>
        /// <param name="nzpServ"></param>
        /// <returns></returns>
        public decimal GetDiffByServ(string pref, int nzpServ)
        {
            if (_maxDiffBetweenValues.ContainsKey(pref))
            {
                if (_maxDiffBetweenValues[pref].ContainsKey(nzpServ))
                {
                    return _maxDiffBetweenValues[pref][nzpServ];
                }


            }
            else if (_maxDiffBetweenValues.ContainsKey(Points.Pref))
            {
                if (_maxDiffBetweenValues[Points.Pref].ContainsKey(nzpServ))
                {
                    return _maxDiffBetweenValues[Points.Pref][nzpServ];
                }
            }
            return 1000000;

        }



    }
}