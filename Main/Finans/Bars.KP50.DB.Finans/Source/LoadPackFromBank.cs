using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using FastReport;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;
using STCLINE.KP50.Utility;
using Constants = STCLINE.KP50.Global.Constants;

namespace STCLINE.KP50.DataBase
{
    public class StartLoadPackFromBank
    {
        public Returns UploadReestrInFon(FilesImported finder)
        {
            Returns ret;
            if (finder.upload_format == (int)FilesImported.UploadFormat.BaikalskVstkb || finder.upload_format == (int)FilesImported.UploadFormat.BaikalskSber)
            {
                using (DbPaymentsFromBankBaikalVstkb dbPaymentsFromBank = new DbPaymentsFromBankBaikalVstkb())
                {
                    ret = dbPaymentsFromBank.UploadReestrInFon(finder);
                }
            }
            else
                if (finder.upload_format == (int)FilesImported.UploadFormat.BaikalskSocProtectL)
                {
                    using (DbPaymentsFromBankSocProtectL dbPaymentsFromBank = new DbPaymentsFromBankSocProtectL())
                    {
                        ret = dbPaymentsFromBank.UploadReestrInFon(finder);
                    }
                }
                else
                    if (finder.upload_format == (int)FilesImported.UploadFormat.BaikalskSocProtectS)
                    {
                        using (DbPaymentsFromBankSocProtectS dbPaymentsFromBank = new DbPaymentsFromBankSocProtectS())
                        {
                            ret = dbPaymentsFromBank.UploadReestrInFon(finder);
                        }
                    }
                    else
                        if (finder.upload_format == (int)FilesImported.UploadFormat.TagilSber)
                        {
                            using (DbPaymentsFromBankTagilSber dbPaymentsFromBank = new DbPaymentsFromBankTagilSber())
                            {
                                ret = dbPaymentsFromBank.UploadReestrInFon(finder);
                            }
                        }
                        else

                            if (finder.upload_format == (int)FilesImported.UploadFormat.BaikalskVstkb || finder.upload_format == (int)FilesImported.UploadFormat.BaikalskSber)
                            {
                                using (DbPaymentsFromBankBaikalVstkb dbPaymentsFromBank = new DbPaymentsFromBankBaikalVstkb())
                                {
                                    ret = dbPaymentsFromBank.UploadReestrInFon(finder);
                                }
                            }
                            else
                                if (finder.upload_format == (int)FilesImported.UploadFormat.BaikalskSocProtectL)
                                {
                                    using (DbPaymentsFromBankSocProtectL dbPaymentsFromBank = new DbPaymentsFromBankSocProtectL())
                                    {
                                        ret = dbPaymentsFromBank.UploadReestrInFon(finder);
                                    }
                                }
                                else
                                    if (finder.upload_format == (int)FilesImported.UploadFormat.BaikalskSocProtectS)
                                    {
                                        using (DbPaymentsFromBankSocProtectS dbPaymentsFromBank = new DbPaymentsFromBankSocProtectS())
                                        {
                                            ret = dbPaymentsFromBank.UploadReestrInFon(finder);
                                        }
                                    }
                                    else
                                        if (finder.upload_format == (int)FilesImported.UploadFormat.TagilSber)
                                        {
                                            using (DbPaymentsFromBankTagilSber dbPaymentsFromBank = new DbPaymentsFromBankTagilSber())
                                            {
                                                ret = dbPaymentsFromBank.UploadReestrInFon(finder);
                                            }
                                        }
                                        else
                                            if (finder.upload_format == (int)FilesImported.UploadFormat.IssrpF112)
                                            {
                                                using (DbPaymentsFromBankIssrpF112 dbPaymentsFromBank = new DbPaymentsFromBankIssrpF112())
                                                {
                                                    ret = dbPaymentsFromBank.UploadReestrInFon(finder);
                                                }
                                            }
                                            else
                                                if (finder.upload_format == (int)FilesImported.UploadFormat.MariyEl)
                                                {
                                                    using (DbPaymentsFromBankMariyEl dbPaymentsFromBank = new DbPaymentsFromBankMariyEl())
                                                    {
                                                        ret = dbPaymentsFromBank.UploadReestrInFon(finder);
                                                    }
                                                }
                                                else
                                                {
                                                    using (DbFtrPaymentsFromBank dbPaymentsFromBank = new DbFtrPaymentsFromBank())
                                                    {
                                                        ret = dbPaymentsFromBank.UploadReestrInFon(finder);
                                                    }
                                                }
            return ret;
        }
    }

    public class FileNameStruct
    {

        public enum ReestrTypes
        {

            /// <summary> переодический реестр </summary>
            Period = 1,
            /// <summary> квитанция переодического реестра </summary>
            PeriodKvit = 2,
            /// <summary> итоговый реестр </summary>
            Svod = 3,
            /// <summary> квитанция итогового реестра</summary>
            SvodKvit = 4
        }

        public ReestrTypes FileType { get; set; }
        public string Number { get; set; }
        public int Year { get; set; }
        public int Month { get; set; }
        public int Day { get; set; }
        public string OtdNumber { get; set; }
        public int RowsCount { get; set; }
        public int NzpDownload { get; set; }
        public int KvitID { get; set; }
    }


    /// <summary>
    /// Класс протокола загрузки файла
    /// </summary>
    public class DbLoadProtokol : DataBaseHead
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
        /// Список невалидных показаний ПУ в реестре
        /// </summary>
        public DataTable UnValidValsForCounter;
        /// <summary>
        /// Комментарии в ходе загрузки
        /// </summary>
        private readonly DataTable _comment;
        /// <summary>
        /// Количество добавленных строк
        /// </summary>
        public int CountInsertedRows;
        /// <summary>
        /// Всего сумма оплат в пачке
        /// </summary>
        public decimal TotalSumPack;
        /// <summary>
        /// Сумма оплат
        /// </summary>
        public decimal SumCharge;
        /// <summary>
        /// Описатель файла
        /// </summary>
        private readonly FileNameStruct _fileArgs;
        /// <summary>
        /// Список сформированных пачек
        /// </summary>
        public List<int> NzpPack = new List<int>();

        public DbLoadProtokol(IDbConnection connDB, FileNameStruct fileArgs)
        {
            UnrecognizedCounters = new DataTable();
            UnrecognizedCounters.TableName = "cnt";
            UnrecognizedCounters.Columns.Add(new DataColumn("nzp_counter"));
            UnrecognizedCounters.Columns.Add(new DataColumn("pkod"));
            UnrecognizedCounters.Columns.Add(new DataColumn("pref"));
            UnrecognizedCounters.Columns.Add(new DataColumn("point"));
            UnrecognizedCounters.Columns.Add(new DataColumn("value"));

            _connDB = connDB;
            _fileArgs = fileArgs;
            _comment = new DataTable();
            _comment.TableName = "kom";
            _comment.Columns.Add(new DataColumn("comment"));

            UncorrectRows = new DataTable();
            UncorrectRows.TableName = "uncor";
            UncorrectRows.Columns.Add(new DataColumn("sourceString"));
            UncorrectRows.Columns.Add(new DataColumn("pref"));
            UncorrectRows.Columns.Add(new DataColumn("point"));

            UnValidValsForCounter = new DataTable();
            UnValidValsForCounter.TableName = "unvalid";
            UnValidValsForCounter.Columns.Add(new DataColumn("pkod"));
            UnValidValsForCounter.Columns.Add(new DataColumn("nzp_counter"));
            UnValidValsForCounter.Columns.Add(new DataColumn("num_cnt"));
            UnValidValsForCounter.Columns.Add(new DataColumn("val_cnt"));
            UnValidValsForCounter.Columns.Add(new DataColumn("reason"));
            UnValidValsForCounter.Columns.Add(new DataColumn("pref"));
            UnValidValsForCounter.Columns.Add(new DataColumn("point"));

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
                ExecSQL(_connDB, "update " + sDefaultSchema +
                         "excel_utility set dat_start = dat_in where nzp_exc = " + id, true);
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
        /// Добавить в список не валидныз показаний ПУ
        /// </summary>
        /// <param name="nzpCounter"></param>
        /// <param name="valCnt"></param>
        public void AddUnValidValsForCounter(string nzp_counter, string num_cnt, string val_cnt, string pkod, string pref, string point, string reason)
        {
            DataRow dr = UnValidValsForCounter.NewRow();
            dr["nzp_counter"] = nzp_counter;
            dr["num_cnt"] = num_cnt;
            dr["val_cnt"] = val_cnt;
            dr["pkod"] = pkod;
            dr["pref"] = pref;
            dr["point"] = point;
            dr["reason"] = reason;
            UnValidValsForCounter.Rows.Add(dr);
        }


        /// <summary>
        /// Добавить в список нераспознанных счетчиков
        /// </summary>
        /// <param name="nzpCounter"></param>
        /// <param name="valCnt"></param>
        public void AddBadCounter(string nzpCounter, string pkod, string pref, string point, string valCnt)
        {
            DataRow dr = UnrecognizedCounters.NewRow();
            dr["nzp_counter"] = nzpCounter;
            dr["pkod"] = pkod;
            dr["pref"] = pref;
            dr["point"] = point;
            dr["value"] = valCnt;
            UnrecognizedCounters.Rows.Add(dr);
        }


        /// <summary>
        /// Получение протокола к загрузке
        /// </summary>
        /// <param name="finder"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        public Returns GetProtocolWwb(FilesImported finder, bool result)
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
                nzp_user = finder.nzp_user,
                status = ExcelUtility.Statuses.InProcess,
                rep_name = "Загрузка реестра в биллинговую систему за " + _fileArgs.Day.ToString("00") + "." +
                           _fileArgs.Month.ToString("00"),
                is_shared = 1
            });
            if (!ret.result) return ret;


            int nzpExc = ret.tag;
            //Имя файла отчета
            int tag = 0;
            string statusName = "Успешно";

            var sql = "";

            sql = "SELECT max(nzp_kvit_reestr) FROM " + Points.Pref + sDataAliasRest + "tula_kvit_reestr " +
                       " WHERE file_name='" + finder.saved_name + "'  ";
            //                       + (finder.upload_format != (int)FilesImported.UploadFormat.MariyEl ? " AND sum_plat=" + TotalSumPack : "");

            object obj = ExecScalar(_connDB, sql, out ret, true);
            if (obj != null && obj != DBNull.Value)
            {
                _fileArgs.KvitID = Convert.ToInt32(obj);
            }
            else
            {
                ret.result = false;
                ret.text = "\nОшибка определения кода квитанции";
            }

            sql = " select pkod, sum_charge" +
                " from " + Points.Pref + sDataAliasRest + "tula_file_reestr " +
                       "where nzp_kvit_reestr=" + _fileArgs.KvitID + " and nzp_kvar is null";

            DataTable dt = ClassDBUtils.OpenSQL(sql, _connDB).resultData;


            sql = " SELECT d.*,b.bank FROM " + Points.Pref + sDataAliasRest + "tula_reestr_downloads d " +
                  " LEFT OUTER JOIN " + Points.Pref + sKernelAliasRest + "s_bank b  on b.nzp_bank=d.nzp_bank" +
                  " WHERE nzp_download=" + _fileArgs.NzpDownload + "";
            DataTable info = ClassDBUtils.OpenSQL(sql, _connDB).resultData;


            sql = " SELECT * FROM " + Points.Pref + sDataAliasRest + "tula_kvit_reestr " +
                  " WHERE nzp_kvit_reestr=" + _fileArgs.KvitID;
            DataTable kvit = ClassDBUtils.OpenSQL(sql, _connDB).resultData;


            sql = " select sum(sum_charge) as inserted_sum from " + Points.Pref + sDataAliasRest + "tula_file_reestr " +
                  "where nzp_kvit_reestr=" + _fileArgs.KvitID + " and nzp_kvar is not null";
            //загруженные оплаты       
            decimal InsertedSumCharge = CastValue<decimal>(ExecScalar(_connDB, sql.ToString(), out ret, true));


            var rep = new Report();

            dt.TableName = "ls";
            if (dt.Rows.Count > 0 || UnrecognizedCounters.Rows.Count > 0 || _comment.Rows.Count > 0
                || UnValidValsForCounter.Rows.Count > 0)
            {
                SetProcent(100, (int)StatusWWB.WithErrors);
                tag = 2;
                statusName = "Загружено с ошибками";
            }
            if (result && UncorrectRows.Rows.Count > 0)
            {
                tag = 2;
                statusName = "Загружено с ошибками";
            }


            #region Если все успешно загружено, то формируем должников с делами, которые оплатили

            var debt = FormDTDebtLsForReport(result);

            #endregion

            var env = new EnvironmentSettings();
            env.ReportSettings.ShowProgress = false;

            DataSet fDataSet = new DataSet();
            fDataSet.Tables.Add(dt);
            fDataSet.Tables.Add(UnrecognizedCounters);
            fDataSet.Tables.Add(_comment);
            fDataSet.Tables.Add(UncorrectRows);
            fDataSet.Tables.Add(UnValidValsForCounter);
            fDataSet.Tables.Add(debt);

            string template = "protocol_workwithbank.frx";
            rep.Load(PathHelper.GetReportTemplatePath(template));

            rep.RegisterData(fDataSet);
            rep.GetDataSource("ls").Enabled = true;
            rep.GetDataSource("cnt").Enabled = true;
            rep.GetDataSource("kom").Enabled = true;
            rep.GetDataSource("uncor").Enabled = true;
            rep.GetDataSource("unvalid").Enabled = true;
            rep.GetDataSource("debt").Enabled = true;

            //установка параметров отчета
            if (info.Rows.Count > 0)
            {
                rep.SetParameterValue("file_name",
                    (info.Rows[0]["file_name"] != DBNull.Value ? info.Rows[0]["file_name"].ToString() : ""));

                if (!result) statusName = "Ошибка";
            }
            else rep.SetParameterValue("file_name", "");

            rep.SetParameterValue("status", statusName);

            //скрываем не нужные страницы
            rep.SetParameterValue("pg1", true);
            rep.SetParameterValue("pg2", _comment.Rows.Count > 0);
            rep.SetParameterValue("pg3", UncorrectRows.Rows.Count > 0);
            rep.SetParameterValue("pg4", dt.Rows.Count > 0);
            rep.SetParameterValue("pg5", UnrecognizedCounters.Rows.Count > 0);
            rep.SetParameterValue("pg6", UnValidValsForCounter.Rows.Count > 0);
            rep.SetParameterValue("pg7", debt.Rows.Count > 0);

            rep.SetParameterValue("count_rows", _fileArgs.RowsCount);
            rep.SetParameterValue("count_inserted_rows", CountInsertedRows);
            rep.SetParameterValue("sum_plat", TotalSumPack);

            if (kvit.Rows.Count > 0)
            {
                rep.SetParameterValue("count_rows_kvit",
                    (kvit.Rows[0]["count_rows"] != DBNull.Value ? kvit.Rows[0]["count_rows"].ToString() : ""));
                rep.SetParameterValue("sum_plat_kvit",
                    (kvit.Rows[0]["sum_plat"] != DBNull.Value ? kvit.Rows[0]["sum_plat"].ToString() : ""));
            }


            rep.SetParameterValue("sum_in_plat", InsertedSumCharge);
            if (info.Rows.Count > 0)
            {
                rep.SetParameterValue("branch_name",
                    (info.Rows[0]["bank"] != DBNull.Value ? info.Rows[0]["bank"].ToString() : ""));
                if (result)
                    rep.SetParameterValue("text",
                        (_fileArgs.RowsCount != CountInsertedRows
                            ? "Загружено " + CountInsertedRows + " записей из " + _fileArgs.RowsCount
                            : ""));

            }
            else rep.SetParameterValue("branch_name", "");



            rep.Prepare();

            var exportXls = new FastReport.Export.OoXML.Excel2007Export();
            string fileName = "protocol_WWB" + DateTime.Now.ToShortDateString().Replace(".", "_") + "_" +
                              DateTime.Now.ToShortTimeString().Replace(":", "_") + ".xlsx";
            exportXls.ShowProgress = false;
            MonitorLog.WriteLog(fileName, MonitorLog.typelog.Info, 20, 201, true);
            try
            {
                exportXls.Export(rep, Path.Combine(Constants.ExcelDir, fileName));
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog(ex.Message, MonitorLog.typelog.Error, true);
            }

            ret.result = true;
            rep.Dispose();


            //перенос  на ftp сервер
            if (InputOutput.useFtp)
            {
                fileName = InputOutput.SaveOutputFile(Constants.ExcelDir + fileName);
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
        /// формирование списка заплативших лс с делами в пс должники
        /// </summary>
        /// <param name="result"></param>
        /// <returns></returns>
        private DataTable FormDTDebtLsForReport(bool result)
        {
            DataTable debt = new DataTable { TableName = "debt" };
            debt.Columns.Add("bank", typeof(string));
            debt.Columns.Add("fio", typeof(string));
            debt.Columns.Add("adres", typeof(string));
            debt.Columns.Add("sum_money", typeof(decimal));
            debt.Columns.Add("sum_debt", typeof(decimal));
            if (result)
            {
                string sqlStr =
                    " SELECT  sp.point, d.debt_money, sum(sum_charge) as inserted_sum , k.fio, " +
                    " trim(r.rajon)||' ул.'||trim(u.ulica)||' д.'||trim(dom.ndom)||'/'||trim(dom.nkor)||' кв.'||trim(k.nkvar) as adres " +
                    " FROM " + Points.Pref + DBManager.sDataAliasRest + "dom dom," +
                    Points.Pref + DBManager.sDataAliasRest + "s_ulica u, " +
                    Points.Pref + DBManager.sDataAliasRest + "s_rajon r, " +
                    Points.Pref + DBManager.sDataAliasRest + "kvar k, " +
                    Points.Pref + "_debt" + DBManager.tableDelimiter + "deal d, " +
                    Points.Pref + DBManager.sKernelAliasRest + "s_point sp, " +
                    Points.Pref + sDataAliasRest + "tula_file_reestr t " +
                    " WHERE t.nzp_kvit_reestr=" + _fileArgs.KvitID + " AND t.nzp_kvar is not null" +
                    " AND k.nzp_kvar = t.nzp_kvar AND k.nzp_kvar = d.nzp_kvar" +
                    " AND sp.bd_kernel = d.pref" +
                    " AND dom.nzp_dom = k.nzp_dom " +
                    " AND dom.nzp_ul=u.nzp_ul " +
                    " AND u.nzp_raj=r.nzp_raj " +
                    " GROUP BY point, debt_money, k.fio, adres ";
                DataTable dtPack = DBManager.ExecSQLToTable(_connDB, sqlStr);

                foreach (DataRow row in dtPack.Rows)
                {
                    debt.Rows.Add(row["point"], row["fio"], row["adres"], row["inserted_sum"], row["debt_money"]);
                }
            }
            return debt;
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


    /// <summary>
    /// Загрузка реестра из банка
    /// </summary>
    abstract public class DbBasePaymentsFromBank : DataBaseHead
    {

        /// <summary>
        /// Протокол загрузки файла
        /// </summary>
        protected DbLoadProtokol _loadProtokol;


        public string NotExistBranch = "";

        protected FileNameStruct _fileArgs;


        /// <summary>
        /// Фоновая загрузка реестра оплат
        /// </summary>
        /// <param name="finder">Описатель файла</param>
        /// <returns></returns>
        public Returns UploadReestrInFon(FilesImported finder)
        {


            Returns ret = Utils.InitReturns();
            try
            {
                //загрузка в фоне
                ret = UploadReestr(finder);
                if (!ret.result)
                {
                    if (_fileArgs.FileType == FileNameStruct.ReestrTypes.SvodKvit)
                    {
                        return ret;
                    }
                    _loadProtokol.AddComment(ret.text.Contains("\n") ? ret.text : "");
                }


                if (_fileArgs.FileType == FileNameStruct.ReestrTypes.Svod)
                    ret.tag = _loadProtokol.GetProtocolWwb(finder, ret.result).tag;
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка добавления итогового реестра " + ex + " " +
                                    finder, MonitorLog.typelog.Error, 20, 201, true);
            }
            if (ret.result)
            {
                if (ret.tag == 2)
                {
                    _loadProtokol.SetProcent(100, (int)StatusWWB.WithErrors);
                }
                else
                {
                    _loadProtokol.SetProcent(100, (int)StatusWWB.Success);
                }
            }
            else
            {
                //если файл не прошел проверку на формат
                if (_loadProtokol.UncorrectRows.Rows.Count > 0)
                {
                    _loadProtokol.SetProcent(-1, (int)StatusWWB.FormatErr);
                }
                else
                {
                    _loadProtokol.SetProcent(-1, (int)StatusWWB.Fail);
                }
            }

            return ret;
        }


        /// <summary>
        /// Проверка файла перед загрузкой
        /// </summary>
        /// <param name="finder"></param>
        /// <param name="ret"></param>
        /// <returns></returns>
        abstract public FilesImported FastCheck(FilesImported finder, out Returns ret);


        /// <summary>
        /// Получение уникального кода квитанции
        /// </summary>
        /// <param name="connDB"></param>
        /// <param name="fileName">Имя файла</param>

        /// <returns></returns>
        protected bool GetKvitId(IDbConnection connDB, FilesImported file)
        {
            bool result = true;

            Returns ret;
            string sqlStr = " SELECT max(nzp_kvit_reestr) FROM " + Points.Pref + sDataAliasRest + "tula_kvit_reestr " +
                            " WHERE file_name='" + file.saved_name + "'";
            if (file.upload_format == (int)FilesImported.UploadFormat.Tula)
            {
                sqlStr += " AND sum_plat=" + _loadProtokol.TotalSumPack;//контрольная сумма оплат 
            }
            object obj = ExecScalar(connDB, sqlStr, out ret, true);
            if (obj != null && obj != DBNull.Value)
            {
                _fileArgs.KvitID = Convert.ToInt32(obj);
                if (_fileArgs.KvitID == 0)
                {
                    MonitorLog.WriteLog("obj=null", MonitorLog.typelog.Info, 20, 201, true);
                    result = false;
                    _loadProtokol.AddComment("\nНе найдена соответствующая квитанция");
                }
            }
            else
            {
                MonitorLog.WriteLog("Не найдена соответствующая квитанция", MonitorLog.typelog.Error, 20, 201, true);
            }
            return result;
        }

        /// <summary>
        /// Сохранить файл
        /// </summary>
        /// <param name="finder"></param>
        /// <returns>Returns</returns>
        abstract protected Returns SaveKvitReestr(IDbConnection _connDB, FilesImported finder);

        /// <summary>
        /// Загрузка реестра оплат
        /// </summary>
        /// <param name="finder">Описатель реестра</param>
        public virtual Returns UploadReestr(FilesImported finder)
        {

            IDbConnection connDB = GetConnection(Constants.cons_Kernel);
            Returns ret = OpenDb(connDB, true);
            if (!ret.result)
            {
                return ret;
            }

            Utils.setCulture(); // установка региональных настроек
            _fileArgs = getFileName(finder.saved_name);

            _loadProtokol = new DbLoadProtokol(connDB, _fileArgs);

            ret = SaveKvitReestr(connDB, finder);
            if (!ret.result)
            {
                ret.text = "\nОшибка при сохранении файла";
                return ret;
            }

            //получаем массив строк из файла
            string[] reestrStrings = ReadReestrFile(finder.ex_path);
            
            //удаляем промежуточный файл на хосте
            if (InputOutput.useFtp) File.Delete(finder.ex_path);

            _fileArgs.RowsCount = finder.count_rows = reestrStrings.Length;
            DbBaseReestrFromBank reestr = null;
            string backTransaction = string.Empty;

            //запись в реестр загрузок
            InsertIntoTulaDownloaded(connDB, finder.saved_name, _fileArgs, finder);
            _loadProtokol.SetProcent(0, (int)StatusWWB.InProcess);

            if (Regex.Replace(reestrStrings[0], "[0-9А-яЁё|.#_№@*,a-zA-Z;\n\r-!:-]", "").
                 Replace("/", "").
                 Replace(@"\", "").
                 Trim().Length != 0)
            {
                _loadProtokol.AddComment(" Кодировка файла не соответствует WIN-1251");
                ret = new Returns(false, " Кодировка файла не соответствует WIN-1251");
                connDB.Close();
                return ret;
            }

            try
            {
                if (_fileArgs.FileType == FileNameStruct.ReestrTypes.Svod)
                {
                    //Разбор реестра для Тулы
                    if (finder.upload_format == (int)FilesImported.UploadFormat.Tula)
                    {
                        reestr = new DbReestrFromBank(connDB, _fileArgs, _loadProtokol);
                        //выполняем проверки и получаем контрольную сумму оплат
                        ret = reestr.FindErrors(reestrStrings);
                        if (!ret.result)
                        {
                            return ret;
                        }

                        //выполняем поиск соответствующей квитанции для реестра
                        if (_fileArgs.FileType != FileNameStruct.ReestrTypes.SvodKvit)
                            if (!GetKvitId(connDB, finder))
                            {
                                ret.result = false;
                                ret.text = "\nНе найдена соответствующая квитанция (квитанция с названием:\'" + finder.saved_name + "\' и " +
                                           " контрольной суммой: " + _loadProtokol.TotalSumPack + " )";
                                return ret;
                            }

                        //разбираем файл 
                        ret = reestr.ParseReestr(finder, reestrStrings);

                    }

                }

                if (_fileArgs.FileType == FileNameStruct.ReestrTypes.SvodKvit)
                {
                    var reestKvit = new DbReestKvit(connDB);
                    ret = reestKvit.Parse(reestrStrings, _fileArgs);
                }
                else if (_fileArgs.FileType == FileNameStruct.ReestrTypes.Svod)
                {
                    //Разбор реестра для Марий Эл
                    if (finder.upload_format == (int)FilesImported.UploadFormat.MariyEl)
                    {
                        //выполняем поиск соответствующей квитанции для реестра
                        if (_fileArgs.FileType != FileNameStruct.ReestrTypes.SvodKvit)
                            if (!GetKvitId(connDB, finder))
                            {
                                ret.result = false;
                                ret.text = "\nНе найдена соответствующая квитанция (квитанция с названием:\'" + finder.saved_name + "\' и " +
                                           " контрольной суммой: " + _loadProtokol.TotalSumPack + " )";
                                return ret;
                            }
                        reestr = new DbReestrFromBankMariyEl(connDB, _fileArgs, _loadProtokol);
                        ret = reestr.ParseReestr(finder, reestrStrings);
                    }

                    if (reestr != null) backTransaction = reestr.BackTransaction;
                }
                else
                {
                    ret.text = "\nНеверный формат файла. Количество полей в файле не совпадает с форматом.";
                    ret.result = false;
                    MonitorLog.WriteLog("Неверное количество полей в файле ", MonitorLog.typelog.Error, 20, 201, true);
                }



                if (!ret.result) return ret;

                if (_fileArgs.FileType == FileNameStruct.ReestrTypes.Period || _fileArgs.FileType == FileNameStruct.ReestrTypes.Svod)
                {
                    DbSaveReestr saveReestr = null;

                    if (finder.upload_format == (int)FilesImported.UploadFormat.MariyEl)
                    {
                        saveReestr = new DbMariiElSaveReestr(connDB, _fileArgs, _loadProtokol, finder.nzp_bank);
                    }
                    else
                    {
                        saveReestr = new DbTulaSaveReestr(connDB, _fileArgs, _loadProtokol, 0);
                    }

                    //записываем данные в систему: pack,pack_ls,pu_vals 
                    if (!saveReestr.SyncLsAndInsertPack(finder).result)
                    {
                        ret.result = false;
                        MonitorLog.WriteLog("Ошибка записи пачки оплат в систему",
                            MonitorLog.typelog.Error, 20, 201, true);
                        ret.text = "\nОшибка при записи данных в систему";
                    }

                }
                if (ret.result)
                {
                    _loadProtokol.SetProcent(100, (int)StatusWWB.Success);

                    if (_fileArgs.FileType == FileNameStruct.ReestrTypes.Period || _fileArgs.FileType == FileNameStruct.ReestrTypes.Svod)
                        PackDist(_loadProtokol.NzpPack, finder); //распределение пачек                
                }

            }
            catch (UserException ex)
            {
                ret.result = false;
                MonitorLog.WriteException("Ошибка загрузки реестра", ex);
                _loadProtokol.AddComment(ex.Message);
            }
            catch (Exception ex)
            {
                ret.result = false;
                MonitorLog.WriteException("Ошибка загрузки реестра", ex);
                RollbackReestrTula(connDB, finder, _fileArgs.NzpDownload);
            }

            connDB.Close();

            //предупреждения
            if (backTransaction != "")
            {
                ret.text += " " + backTransaction;
            }
            if (NotExistBranch != "")
            {
                ret.text += " " + NotExistBranch;
            }

            return ret;
        }


        /// <summary>
        /// Прочитать строки файла оплат
        /// </summary>
        /// <param name="path">Путь к файлу</param>
        /// <returns></returns>
        public string[] ReadReestrFile(string path)
        {
            var fstream = new FileStream(path, FileMode.Open, FileAccess.Read);
            byte[] buffer = new byte[fstream.Length];
            fstream.Position = 0;
            fstream.Read(buffer, 0, buffer.Length);
            fstream.Close();

            string reestrFileString = Encoding.GetEncoding(1251).GetString(buffer);
            string[] stSplit = { Environment.NewLine };
            string[] reestrStrings = reestrFileString.Split(stSplit, StringSplitOptions.RemoveEmptyEntries);
            return reestrStrings;
        }

        /// <summary>
        /// Проверка реестра на существование
        /// </summary>
        /// <param name="connDB"></param>
        /// <param name="fileName"></param>
        /// <param name="file"></param>
        /// <returns></returns>
        protected bool CheckReestrName(IDbConnection connDB, string fileName, FileNameStruct file)
        {
            var sqlStr = " SELECT count(k.*) " +
                         " FROM " + Points.Pref + sDataAliasRest + "tula_kvit_reestr k, " +
                           Points.Pref + sDataAliasRest + "tula_reestr_downloads d" +
                         " WHERE k.nzp_download=d.nzp_download AND  k.file_name='" + fileName + "'";

            //костыль на быструю проверку наличия квитанции для реестра
            sqlStr += " AND abs(k.date_plat-" + Utils.EStrNull(Points.DateOper.ToShortDateString()) + ")<300";
            if (file.FileType == FileNameStruct.ReestrTypes.Svod)
            {
                sqlStr += " AND is_itog=1";
            }
#if PG
            sqlStr += "AND extract(year from date_download)=extract(year from now())";
#else
            sqlStr += "AND  YEAR(date_download)= YEAR(TODAY)";
#endif

            Returns ret;
            var count = CastValue<int>(ExecScalar(connDB, sqlStr, out ret, true));
            return count > 0;
        }

        /// <summary>
        /// Определение банка откуда пришли оплаты
        /// </summary>
        /// <param name="connDB"></param>
        /// <param name="branchID"></param>
        /// <returns></returns>
        private int GetNzpBankByBranchId(IDbConnection connDB, FileNameStruct fileArgs)
        {
            Returns ret;
            int nzpBank = 0;
            string branch_name = fileArgs.FileType == FileNameStruct.ReestrTypes.SvodKvit ? "branch_id" : "branch_id_reestr";
            string sqlString = " SELECT nzp_bank FROM " + Points.Pref + DBManager.sDataAliasRest +
                               "tula_s_bank where is_actual<>100 " +
                               " and " + branch_name + "=" + Utils.EStrNull(fileArgs.OtdNumber.ToUpper());
            nzpBank = CastValue<int>(ExecScalar(connDB, sqlString, out ret, true));
            return nzpBank;
        }


        /// <summary>
        /// Сохранение записи о реестре в Базу данных
        /// </summary>
        /// <param name="connDB">Подключение</param>
        /// <param name="fileName">Имя Файла</param>
        /// <param name="file">Описатель</param>
        /// <param name="finder"></param>
        protected void InsertIntoTulaDownloaded(IDbConnection connDB, string fileName, FileNameStruct file, FilesImported finder)
        {

            //var db = new DbWorkUser();
            finder.pref = Points.Pref;
            Returns ret;
            /*int nzpUser = db.GetLocalUser(connDB, finder, out ret);
            db.Close();*/

            int nzpUser = finder.nzp_user;
            finder.nzp_user_main = nzpUser;

            int nzpBank = 0;

            if (finder.nzp_bank > 0)
            {
                nzpBank = finder.nzp_bank;
            }
            else
            {
                nzpBank = GetNzpBankByBranchId(connDB, file);
            }

            if (nzpBank == 0)
            {
                ret.result = false;
                ret.text = "\r\n Не определен банк";
                return;
            }

            var dateD = DateTime.Now.ToString("yyyy.MM.dd HH:mm:ss").Replace('.', '-');

            string sqlStr = "INSERT INTO " + Points.Pref + sDataAliasRest + "tula_reestr_downloads " +
                            "(file_name,nzp_type,date_download,user_downloaded,day,month, branch_id,nzp_bank) " +
                            "VALUES " +
                            " ( '" + fileName + "'," + (int)file.FileType + ",'" + dateD + "'," + nzpUser + "," +
                            file.Day + "," + file.Month + ",'" + file.OtdNumber + "'," + nzpBank + " )";

            ClassDBUtils.ExecSQL(sqlStr, connDB);
            file.NzpDownload = GetSerialValue(connDB);

        }

        /// <summary>
        /// Проверка на повторную загрузку файла
        /// </summary>
        /// <param name="connDB"></param>
        /// <param name="fileName"></param>
        /// <returns></returns>
        protected bool ExistFile(IDbConnection connDB, string fileName)
        {
            bool ret = true;
            int num;
            Returns retur;

            string sqlString = " SELECT count(*) FROM " + Points.Pref + sDataAliasRest + "tula_reestr_downloads " +
                               " where file_name='" + fileName + "'";
#if PG
            sqlString += " AND extract(year from date_download)=extract(year from now())";
#else
            sqlString += " AND  YEAR(date_download)= YEAR(TODAY)";
#endif

            object obj = ExecScalar(connDB, sqlString, out retur, true);
            num = Convert.ToInt32(obj);
            if (num > 0)
            {
                ret = false;
            }
            return ret;
        }

        /// <summary>
        /// Заполнение файловой структуры
        /// </summary>
        /// <param name="fileName">Имя файла</param>
        /// <param name="branch_id">Код отделения для Марий Эл</param>
        /// <returns></returns>
        abstract public FileNameStruct getFileName(string fileName);


        /// <summary>
        /// Определение платежного агента
        /// </summary>
        /// <param name="connDB"></param>
        /// <param name="fileArgs"></param>
        /// <returns></returns>
        protected Returns CheckBank(IDbConnection connDB, FileNameStruct fileArgs)
        {
            Returns ret;
            var branch_name = "";
            branch_name = fileArgs.FileType == FileNameStruct.ReestrTypes.SvodKvit ? "branch_id" : "branch_id_reestr";

            string sqlString = "select nzp_bank from " + Points.Pref + DBManager.sDataAliasRest + "tula_s_bank " +
                               "where is_actual<>100 and  " + branch_name + "=" + Utils.EStrNull(fileArgs.OtdNumber.ToUpper());
            string otd = CastValue<string>(ExecScalar(connDB, sqlString, out ret, true));
            if (!ret.result) return ret;
            if (otd.Length == 0)
            {
                ret.tag = 999;
                ret.result = false;
                NotExistBranch = "\r\nПлатежный агент не определен. Добавьте платежного агента в справочнике платежных агентов";
            }
            return ret;
        }


        /// <summary>
        /// Распределение пачек
        /// </summary>
        /// <param name="nzpPack"></param>
        /// <param name="finder"></param>
        /// <returns></returns>
        public Returns PackDist(List<int> nzpPack, FilesImported finder)
        {
            if (finder == null) throw new ArgumentNullException("finder");
            Returns ret = Utils.InitReturns();
            ret.result = true;
            #region распределение пачек
            //в зависимости от настроек распределяем пачки сразу 
            if (Points.packDistributionParameters.DistributePackImmediately)
            {

                DbCalcPack db1 = new DbCalcPack();
                DbPack pack = new DbPack();
                for (int i = 0; i < nzpPack.Count; i++)
                {
                    Pack finderp = new Pack();
                    db1.PackFonTasks(nzpPack[i], Points.DateOper.Year, finder.nzp_user, CalcFonTask.Types.DistributePack, out ret);  // Отдаем пачку на распределение                 
                    finderp.flag = Pack.Statuses.WaitingForDistribution.GetHashCode();
                    finderp.nzp_user = finder.nzp_user;
                    finderp.nzp_pack = nzpPack[i];
                    finderp.year_ = Points.DateOper.Year;//Points.CalcMonth.year_;
                    if (ret.result)
                    {
                        db1.UpdatePackStatus(finderp);
                    }
                }
                db1.Close();
                pack.Close();
            }

            #endregion
            return ret;
        }



        /// <summary>
        /// Откат загрузки тульского реестра
        /// </summary>
        /// <param name="connDB"></param>
        /// <param name="finder"></param>
        /// <param name="nzpDownload"></param>
        /// <returns></returns>
        public Returns RollbackReestrTula(IDbConnection connDB, Finder finder, int nzpDownload)
        {
            Returns ret;

            string sql = " select nzp_type from " + Points.Pref + DBManager.sDataAliasRest + "tula_reestr_downloads " +
                         " where nzp_download=" + nzpDownload + " ";
            object obj = ExecScalar(connDB, sql, out ret, true);
            if (!ret.result)
            {
                return ret;
            }

            if (obj == null)
            {
                ret.result = false;
                ret.text = "Ошибка удаления данных";
                return ret;
            }

            int nzpType = Convert.ToInt32(obj);
            //Удаляем квитанцию, она удаляется только вместе с файлом реестра

            string finAlias = Points.Pref + "_fin_" + (Points.DateOper.Year - 2000).ToString("00") +
                              tableDelimiter;
            string nameNewFile = "";
            var datePlat = "";
            var datesPlat = new List<DateTime>();
            if (nzpType == 2 || nzpType == 4)//квитанция
            {

                #region Получение имени файла по загрузке
                sql = " select nzp_kvit_reestr from " + Points.Pref + DBManager.sDataAliasRest +
                  "tula_kvit_reestr where nzp_download=" + nzpDownload + " ";
                var nzp_kvit = CastValue<int>(ExecScalar(connDB, sql, out ret, true));

                if (nzp_kvit > 0)
                {
                    sql = "select trim(file_name) from " + Points.Pref + DBManager.sDataAliasRest +
                          "tula_kvit_reestr where nzp_kvit_reestr=" + nzp_kvit;
                    nameNewFile = CastValue<string>(ExecScalar(connDB, sql, out ret, true));

                    //дата оплаты из файла квитанции - для старых записей
                    sql = "select date_plat from " + Points.Pref + DBManager.sDataAliasRest +
                      "tula_kvit_reestr where nzp_kvit_reestr=" + nzp_kvit;
                    datesPlat.Add(CastValue<DateTime>(ExecScalar(connDB, sql, out ret, true)));

                    //получаем список дат оплат (из transaction_id)
                    sql = "select distinct payment_datetime from " + Points.Pref + DBManager.sDataAliasRest +
                          "tula_file_reestr where nzp_kvit_reestr=" + nzp_kvit;
                    var dates = ClassDBUtils.OpenSQL(sql, connDB).resultData;
                    if (dates != null)
                    {
                        for (int i = 0; i < dates.Rows.Count; i++)
                        {
                            datesPlat.Add(CastValue<DateTime>(dates.Rows[i]["payment_datetime"]));
                        }
                    }
                    //dat_vvod in (...)
                    datePlat = string.Join(",", datesPlat.Select(x => Utils.EStrNull(x.ToShortDateString())));
                }
                else
                {
                    sql = " select file_name from " + Points.Pref + DBManager.sDataAliasRest +
                   "tula_reestr_downloads where nzp_download=" + nzpDownload + " ";

                    object name = ExecScalar(connDB, sql, out ret, true);
                    if (!ret.result)
                    {
                        return ret;
                    }
                    if (name == null)
                    {
                        ret.result = false;
                        MonitorLog.WriteLog("Ошибка получения данных о удаляемом файле", MonitorLog.typelog.Error, true);
                        return ret;
                    }
                    string type;
                    var nameFile = Convert.ToString(name).Trim().Split('.');
                    if (nzpType == 2)
                    {
                        type = ".0" + nameFile[1].Substring(1, 2);
                    }
                    else
                    {
                        type = ".00" + nameFile[1].Substring(2);
                    }
                    nameNewFile = nameFile[0] + type;
                }
                #endregion

                #region Проверка распределенных сумм

                sql = " select count(*) as count_rasp " +
                      " from " + finAlias + "pack p," +
                      "      " + finAlias + "pack_ls pl " +
                      " where file_name=" + Utils.EStrNull(nameNewFile) +
                      " and p.nzp_pack=pl.nzp_pack " +
                      " and pl.dat_uchet is not null" +
                      (datesPlat.Count > 0 ? " and pl.dat_vvod in (" + datePlat + ")" : "");
                object countRasp = ExecScalar(connDB, sql, out ret, true);
                if ((countRasp != DBNull.Value) && (Int32.Parse(countRasp.ToString()) > 0))
                {
                    ret = new Returns(false, "Есть распределенные оплаты по реестру, " + Environment.NewLine +
                                             "сначала отмените распределение по данному реестру", -1) { result = false };
                    return ret;
                }

                #region Удаляет показания счетчиков
                sql = " delete from " + finAlias + "pu_vals " +
                      " where nzp_pack_ls in (select nzp_pack_ls " +
                      " from " + finAlias + "pack_ls pl, " +
                      " " + finAlias + "pack p" +
                      " where pl.nzp_pack=p.nzp_pack " +
                      " and file_name=" + Utils.EStrNull(nameNewFile) + "" +
                         (datesPlat.Count > 0 ? " and pl.dat_vvod in (" + datePlat + ")" : "") +
                      ")";
                if (!ExecSQL(connDB, sql, true).result)
                {
                    ret.result = false;
                    ret.text = "Ошибка удаления данных загрузки";
                    return ret;
                }

                #endregion


                sql = "select nzp_pack from " + finAlias + "pack_ls where nzp_pack in (select nzp_pack" +
                                  " from " + finAlias + "pack " +
                                  " where file_name=" + Utils.EStrNull(nameNewFile) + " )  " +
                                   (datesPlat.Count > 0 ? " and dat_vvod in (" + datePlat + ")" : "") +
                                  " group by 1";
                var DT = ClassDBUtils.OpenSQL(sql, connDB).resultData;
                List<string> l_nzp_pack = new List<string>();
                foreach (DataRow row in DT.Rows)
                {
                    l_nzp_pack.Add(CastValue<string>(row["nzp_pack"]));
                }
                var s_nzp_pack = string.Join(",", l_nzp_pack);


                //Удаление оплат
                sql = " delete  " +
                      " from " + finAlias + "pack_ls " +
                      " where nzp_pack in (select nzp_pack" +
                      " from " + finAlias + "pack " +
                      " where file_name=" + Utils.EStrNull(nameNewFile) + ")" +
                       (datesPlat.Count > 0 ? " and dat_vvod in (" + datePlat + ")" : "");
                ExecSQL(connDB, sql, true);


                if (l_nzp_pack.Count > 0)
                {
                    //Удаление пачек
                    sql = " delete  " +
                          " from " + finAlias + "pack " +
                          " where nzp_pack in (" + s_nzp_pack + ")";
                    ExecSQL(connDB, sql, true);
                }


                #endregion


                #region Удаляем записи в реестре загрузок


                sql = " delete from " + Points.Pref + DBManager.sDataAliasRest + "tula_reestr_downloads " +
                      " where  nzp_download = " + nzpDownload + " ";
                if (!ExecSQL(connDB, sql, true).result)
                {
                    ret.result = false;
                    ret.text = "Ошибка удаления данных загрузки";
                    return ret;
                }


                //Удаляем запись реестра

                sql = "delete from " + Points.Pref + DBManager.sDataAliasRest +
                      "tula_reestr_downloads " +
                      " where  file_name = '" + nameNewFile + "'  and date_download>=" + Utils.EStrNull(Points.DateOper.ToShortDateString());
                if (!ExecSQL(connDB, sql, true).result)
                {
                    ret.result = false;
                    ret.text = "Ошибка удаления данных загрузки";
                    return ret;
                }
                #endregion

                #region Получаем код реестра квитанций
                if (nzp_kvit <= 0)
                {
                    sql = " SELECT max(nzp_kvit_reestr)" +
                          " FROM  " + Points.Pref + DBManager.sDataAliasRest + "tula_kvit_reestr " +
                          " WHERE file_name='" + nameNewFile + "' " +
                          " AND sum_plat=" + _loadProtokol.TotalSumPack;//контрольная сумма оплат 

                    nzp_kvit = DBManager.ExecScalar<int>(connDB, sql);
                    if (!ret.result)
                    {
                        return ret;
                    }

                }
                #endregion

                #region Удаляем счетчики

                sql = " delete from " + Points.Pref + DBManager.sDataAliasRest + "tula_counters_reestr " +
                      " where nzp_reestr_d in (select nzp_reestr_d " +
                      "from " + Points.Pref + DBManager.sDataAliasRest + "tula_file_reestr " +
                      " where nzp_kvit_reestr=" + nzp_kvit + ")";
                if (!ExecSQL(connDB, sql, true).result)
                {
                    ret.result = false;
                    ret.text = "Ошибка удаления данных загрузки";
                    return ret;
                }


                #endregion

                #region Удаляем платежи
                sql = " delete from " + Points.Pref + DBManager.sDataAliasRest + "tula_file_reestr " +
                     " where nzp_kvit_reestr=" + nzp_kvit + "";
                if (!ExecSQL(connDB, sql, true).result)
                {
                    ret.result = false;
                    ret.text = "Ошибка удаления данных загрузки";
                    return ret;
                }
                #endregion

                #region Удаляем квитанцию
                sql = " delete from " + Points.Pref + DBManager.sDataAliasRest + "tula_kvit_reestr " +
                      " where nzp_kvit_reestr=" + nzp_kvit + "";
                if (!ExecSQL(connDB, sql, true).result)
                {
                    ret.result = false;
                    ret.text = "Ошибка удаления данных загрузки";
                    return ret;
                }
                #endregion

            }
            else //реестр
            {

                #region Проверка распределенных сумм

                sql = " select count(*) as count_rasp " +
                      " from " + finAlias + "pack p," +
                      "      " + finAlias + "pack_ls pl, " +
                      "      " + Points.Pref + DBManager.sDataAliasRest + "tula_reestr_downloads d " +
                      " where  d.nzp_download=" + nzpDownload +
                      "     and p.file_name=d.file_name " +
                      "     and p.nzp_pack=pl.nzp_pack " +
                      "     and pl.dat_uchet is not null";
                object countRasp = ExecScalar(connDB, sql, out ret, true);
                if ((countRasp != DBNull.Value) && (Int32.Parse(countRasp.ToString()) > 0))
                {
                    ret = new Returns(false, "Есть распределенные оплаты по реестру, " + Environment.NewLine +
                                             "сначала отмените распределение по данному реестру", -1) { result = false };
                    return ret;
                }

                sql = " delete from " + finAlias + "pu_vals " +
                      " where nzp_pack_ls in (select nzp_pack_ls " +
                      " from " + finAlias + "pack_ls pl, " +
                      " " + finAlias + "pack p, " +
                      "      " + Points.Pref + DBManager.sDataAliasRest + "tula_reestr_downloads d " +
                      " where pl.nzp_pack=p.nzp_pack " +
                      " and p.file_name=d.file_name)";
                if (!ExecSQL(connDB, sql, true).result)
                {
                    ret.result = false;
                    ret.text = "Ошибка удаления данных загрузки";
                    return ret;
                }

                #endregion

                //Удаление оплат
                sql = " delete  " +
                      " from " + finAlias + "pack_ls " +
                      " where nzp_pack in (select nzp_pack" +
                      " from " + finAlias + "pack p, " +
                      "      " + Points.Pref + DBManager.sDataAliasRest + "tula_reestr_downloads d " +
                      "  where d.nzp_download=" + nzpDownload +
                      "     and p.file_name=d.file_name) and dat_uchet is null";
                ExecSQL(connDB, sql, true);

                //Удаление пачек
                sql = " delete  " +
                      " from " + finAlias + "pack p " +
                      " where file_name in (select file_name" +
                      " from     " + Points.Pref + DBManager.sDataAliasRest + "tula_reestr_downloads d " +
                      "  where d.nzp_download=" + nzpDownload + ") and p.sum_rasp is null";
                ExecSQL(connDB, sql, true);





                #region Удаляем счетчики

                sql = " delete " +
                      " from " + Points.Pref + DBManager.sDataAliasRest + "tula_counters_reestr " +
                      " where nzp_reestr_d in (select nzp_reestr_d " +
                      " from  " + Points.Pref + DBManager.sDataAliasRest + "tula_kvit_reestr kv,  " +
                      Points.Pref + DBManager.sDataAliasRest + "tula_file_reestr t, " +
                      Points.Pref + DBManager.sDataAliasRest + "tula_reestr_downloads d " +
                      " where trim(d.file_name)=trim(kv.file_name) " +
                      " and t.nzp_kvit_reestr =kv.nzp_kvit_reestr " +
                      " and d.nzp_download=" + nzpDownload + ") ";
                if (!ExecSQL(connDB, sql, true).result)
                {
                    ret.result = false;
                    ret.text = "Ошибка удаления данных загрузки";
                    return ret;
                }


                #endregion


                //Удаляем данные реестра
                sql = " delete from  " + Points.Pref + DBManager.sDataAliasRest +
                      "tula_file_reestr where nzp_kvit_reestr = " +
                      " (select nzp_kvit_reestr from  " + Points.Pref + DBManager.sDataAliasRest +
                      "tula_kvit_reestr kv,  " +
                      Points.Pref + DBManager.sDataAliasRest + "tula_reestr_downloads d " +
                      " where trim(d.file_name)=trim(kv.file_name) " +
                      " and d.nzp_download=" + nzpDownload + ") ";
                if (!ExecSQL(connDB, sql, true).result)
                {
                    ret.result = false;
                    ret.text = "Ошибка удаления данных реестра";
                    return ret;
                }


                //Удаляем запись реестра

                //sql = " delete from " + Points.Pref + DBManager.sDataAliasRest + "tula_reestr_downloads " +
                //      " where  nzp_download = " + nzpDownload + " ";
                //if (!ExecSQL(connDB, sql, true).result)
                //{
                //    ret.result = false;
                //    ret.text = "Ошибка удаления данных загрузки";
                //    return ret;
                //}

            }

            return ret;
        }

    }

    /// <summary>
    /// Загрузка реестра из банка для Тулы
    /// </summary>
    public class DbPaymentsFromBank : DbBasePaymentsFromBank
    {
        /// <summary>
        /// Заполнение файловой структуры
        /// </summary>
        /// <param name="fileName">Имя файла</param>
        /// <returns></returns>
        override public FileNameStruct getFileName(string fileName)
        {
            var name = new FileNameStruct();
            string[] fileN = fileName.Split('.');
            try
            {
                name.Number = fileN[0].Substring(0, 5);
                name.Month = int.Parse(fileN[0].Substring(5, 1), NumberStyles.AllowHexSpecifier);
                name.Day = Convert.ToInt32(fileN[0].Substring(6, 2));

                switch (fileN[1].Substring(0, 1))
                {
                    case "K":
                        {
                            name.FileType = FileNameStruct.ReestrTypes.SvodKvit;
                            name.OtdNumber = fileN[1].Substring(1);
                        } break;

                    case "0":
                        {
                            name.FileType = FileNameStruct.ReestrTypes.Svod;
                            name.OtdNumber = fileN[1].Substring(1);
                        } break;
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка разбора имени файла: " + ex.Message, MonitorLog.typelog.Error, true);
            }
            return name;
        }

        /// <summary>
        /// Сохранить файл
        /// </summary>
        /// <param name="finder"></param>
        /// <returns>Returns</returns>
        override protected Returns SaveKvitReestr(IDbConnection _connDB, FilesImported finder)
        {
            return new Returns(true, "");
        }

        /// <summary>
        /// Проверка файла перед загрузкой
        /// </summary>
        /// <param name="finder"></param>
        /// <param name="ret"></param>
        /// <returns></returns>
        override public FilesImported FastCheck(FilesImported finder, out Returns ret)
        {
            //директория файла   
            string fDirectory;
            Utils.setCulture(); // установка региональных настроек
            if (InputOutput.useFtp)
            {
                fDirectory = InputOutput.GetInputDir();
                InputOutput.DownloadFile(finder.loaded_name, fDirectory + finder.saved_name, true);
            }
            else
            {
                fDirectory = Constants.Directories.ImportAbsoluteDir;
            }
            finder.ex_path = Path.Combine(fDirectory, finder.saved_name);
            #region проверка входных параметров
            if (finder.nzp_user < 1)
            {
                ret = new Returns(false, "Не определен пользователь");
                return finder;
            }

            #endregion

            IDbConnection connDB = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(connDB, true);
            if (!ret.result)
            {
                return finder;
            }
            try
            {
                //проверка на существование такого же загруженного файла
                if (!ExistFile(connDB, finder.saved_name))
                {
                    ret.text = "\nФайл с таким именем в этом году уже был загружен";
                    ret.result = false;
                    ret.tag = 996;
                    if (InputOutput.useFtp) File.Delete(finder.ex_path);
                    return finder;
                }


                FileNameStruct fileArgs = getFileName(finder.saved_name);
                if (fileArgs.FileType == 0)
                {
                    ret.text = "Расширение файла недопустимо для этого банка.";
                    ret.result = false;
                    if (InputOutput.useFtp) File.Delete(finder.ex_path);
                    return finder;
                }

                //проверка на существование отделения банка
                ret = CheckBank(connDB, fileArgs);
                if (!ret.result)
                {
                    if (ret.tag == 999) // предложить создать нового платежного агента
                    {
                        ret.text = NotExistBranch;
                        ret.sql_error = fileArgs.OtdNumber.ToUpper();
                    }
                    return finder;
                }


                if (fileArgs.FileType == FileNameStruct.ReestrTypes.Period || fileArgs.FileType == FileNameStruct.ReestrTypes.Svod)
                    //проверка на существование квитанции для реестра
                    if (!CheckReestrName(connDB, finder.saved_name, fileArgs))
                    {
                        ret.result = false;
                        ret.tag = 995;
                        ret.text = "\nДля файла итогового реестра не найдена квитанция";
                        return finder;
                    }
            }
            finally
            {
                connDB.Close();
            }

            return finder;
        }
    }

    /// <summary>
    /// Загрузка реестра из банка для Марий Эл
    /// </summary>
    public class DbPaymentsFromBankMariyEl : DbBasePaymentsFromBank
    {
        /// <summary>
        /// Сохранить файл
        /// </summary>
        /// <param name="finder"></param>
        /// <returns>Returns</returns>
        override protected Returns SaveKvitReestr(IDbConnection _connDB, FilesImported finder)
        {
            string sqlStr = "INSERT INTO " + Points.Pref + sDataAliasRest + "tula_kvit_reestr (date_plat,file_name,is_itog) " +
                            " VALUES " +
                            " ( " + DBManager.sCurDate + ", " + Utils.EStrNull(finder.saved_name) + ", 1)";

            Returns ret = ExecSQL(_connDB, sqlStr, true);

            if (!ret.result) throw new UserException("Ошибка при записи данных квитанции итогового реестра");

            return ret;
        }

        /// <summary>
        /// Заполнение файловой структуры
        /// </summary>
        /// <param name="fileName">Имя файла</param>
        /// <returns></returns>
        override public FileNameStruct getFileName(string fileName)
        {
            var name = new FileNameStruct();
            string[] fileN = fileName.Split('.');
            try
            {
                name.Number = fileN[0];
                name.FileType = FileNameStruct.ReestrTypes.Svod;
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка разбора имени файла: " + ex.Message, MonitorLog.typelog.Error, true);
            }
            return name;
        }

        /// <summary>
        /// Проверка файла перед загрузкой
        /// </summary>
        /// <param name="finder"></param>
        /// <param name="ret"></param>
        /// <returns></returns>
        override public FilesImported FastCheck(FilesImported finder, out Returns ret)
        {
            //директория файла   
            string fDirectory;
            Utils.setCulture(); // установка региональных настроек
            if (InputOutput.useFtp)
            {
                fDirectory = InputOutput.GetInputDir();
                InputOutput.DownloadFile(finder.loaded_name, fDirectory + finder.saved_name, true);
            }
            else
            {
                fDirectory = Constants.Directories.ImportAbsoluteDir;
            }
            finder.ex_path = Path.Combine(fDirectory, finder.saved_name);

            #region проверка входных параметров
            if (finder.nzp_user < 1)
            {
                ret = new Returns(false, "Не определен пользователь");
                return finder;
            }

            if (finder.nzp_bank < 1)
            {
                ret = new Returns(false, "Не определен банк");
                return finder;
            }
            #endregion

            IDbConnection connDB = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(connDB, true);
            if (!ret.result)
            {
                return finder;
            }
            try
            {
                //проверка на существование такого же загруженного файла
                if (!ExistFile(connDB, finder.saved_name))
                {
                    ret.text = "\nФайл с таким именем уже был загружен";
                    ret.result = false;
                    ret.tag = 996;
                    if (InputOutput.useFtp) File.Delete(finder.ex_path);
                    return finder;
                }
            }
            finally
            {
                connDB.Close();
            }

            return finder;
        }
    }

    /// <summary>
    /// Абстрактный класс для разбора строки платежа по реестру
    /// </summary>
    abstract public class DbBaseReestrFromBank : DataBaseHead
    {
        protected class ReestrCounter
        {
            public string Cnt { get; set; }
            public string ValCnt { get; set; }

            public ReestrCounter()
            {
                Cnt = String.Empty;
                ValCnt = String.Empty;
            }
        }

        protected class ReestrBody
        {
            public string LSKod { get; set; }
            public decimal SumPlat { get; set; }
            public string TransactionID { get; set; }
            public string NomerPlatPoruch { get; set; }
            public string DatePlatPoruch { get; set; }
            public string ServiceField { get; set; }
            public string PaymentDate { get; set; }
            public string PaymentTime { get; set; }
            public string Address { get; set; }
            public DateTime PaymentDateFromTransactionID { get; set; }

            public List<ReestrCounter> counterList = new List<ReestrCounter>();
        }


        protected readonly DbLoadProtokol _loadProtokol;
        protected readonly FileNameStruct _fileArgs;
        public string BackTransaction = "";

        protected int _numRow;

        protected readonly IDbConnection _connDB;

        public DbBaseReestrFromBank(IDbConnection connDB, FileNameStruct fileArgs, DbLoadProtokol loadProtokol)
        {
            _loadProtokol = loadProtokol;
            _connDB = connDB;
            _fileArgs = fileArgs;
        }





        /// <summary>
        /// Проверка счетчиков на допустимое значение
        /// </summary>
        /// <param name="numCnt"></param>
        /// <param name="valCnt"></param>
        /// <returns></returns>
        protected static string FillNumCounter(string numCnt, string valCnt)
        {
            var numResult = String.IsNullOrEmpty(numCnt)
                ? "null"
                : "'" + numCnt + "'";
            var valResult = (valCnt == "НЕТ" || valCnt == "" || valCnt == " " || valCnt == null)
                ? "null"
                : valCnt;

            return numResult + ',' + valResult;
        }



        /// <summary>
        /// Проверка на существование платежа
        /// </summary>
        /// <param name="pkod"></param>
        /// <param name="transactionID"></param>
        /// <param name="sumCharge">Сумма оплаты</param>
        /// <returns></returns>
        protected abstract bool IsPaymentExists(string pkod, string transactionID, decimal sumCharge);



        /// <summary>
        /// Заполнение структуры платежа из строки
        /// </summary>
        /// <param name="fields">набор полей строки</param>
        /// <returns></returns>
        abstract protected ReestrBody FillBody(string[] fields);


        /// <summary>
        /// Построчная разборка реестра квитанций
        /// </summary>
        /// <param name="finder"></param>
        /// <param name="reestrStrings"></param>
        /// <returns></returns>
        public virtual Returns ParseReestr(FilesImported finder, string[] reestrStrings)
        {

            //сопоставление указанного в квитанции для реестра кол-ва строк и суммы оплат с данными в файле реестра 
            var ret = CheckReestrAtr(reestrStrings.Length, finder.saved_name);
            if (!ret.result)
                return ret;

            try
            {

                double costOneRow = Math.Round(30d / Math.Max(finder.count_rows, 1), 4);
                int counter = 0;
                double currentProgres = 0;
                //заголовочная структура


                foreach (var str in reestrStrings)
                {
                    _numRow++;
                    counter++;

                    ret = ParseOneString(str);
                    if (!ret.result) return ret;

                    if (counter % 10 == 0)
                    {
                        currentProgres += costOneRow * 10;
                        _loadProtokol.SetProcent(currentProgres, (int)StatusWWB.InProcess);
                    }
                }
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = "\nОшибка в строке " + _numRow + " при разборе файла оплат от банка ";
                MonitorLog.WriteLog(ret.text + ". " + ex.Message, MonitorLog.typelog.Error, 20, 201, true);
            }

            return ret;
        }

        /// <summary>
        /// Проверка строк по списку возможных ошибок в заполнении файла
        /// </summary>
        /// <param name="rows"></param>
        /// <returns></returns>
        abstract public Returns FindErrors(string[] rows);


        /// <summary>
        /// сопоставление указанного в квитанции для реестра кол-ва строк и суммы оплат с данными в файле реестра 
        /// </summary>
        /// <param name="reestrRowsCount"></param>
        /// <param name="fileName"></param>
        /// <returns></returns>
        abstract public Returns CheckReestrAtr(int reestrRowsCount, string fileName);


        /// <summary>
        /// Добавление платежа в БД
        /// </summary>
        /// <param name="bodyReestr"></param>
        /// <returns></returns>
        abstract protected bool InsertPayment(ReestrBody bodyReestr);

        /// <summary>
        /// Определить код счетчика по номеру
        /// </summary>
        /// <param name="bodyReestr"></param>
        /// <returns></returns>
        abstract protected string GetNzpCounter(string numCnt, string pkod, string valCnt);

        /// <summary>
        /// Добавление счетчиков
        /// </summary>
        /// <param name="bodyReestr">Строка реестра оплат</param>
        /// <param name="nzpReestrD">Код реестра</param>
        protected void InsertCounters(ReestrBody bodyReestr, int nzpReestrD)
        {
            string sqlStr;

            for (int i = 0; i < bodyReestr.counterList.Count; i++)
            {
                if (!String.IsNullOrEmpty(bodyReestr.counterList[i].Cnt) && !String.IsNullOrEmpty(bodyReestr.counterList[i].ValCnt))
                {
                    string nzp_counter = GetNzpCounter(bodyReestr.counterList[i].Cnt, bodyReestr.LSKod, bodyReestr.counterList[i].ValCnt);

                    if (nzp_counter != "")
                    {
                        sqlStr = "INSERT INTO " + Points.Pref + "_data" + tableDelimiter + "tula_counters_reestr " +
                             " (nzp_reestr_d, nzp_counter, cnt, val_cnt) " +
                             " VALUES ( " + nzpReestrD + ", " + nzp_counter + "," +
                             FillNumCounter(bodyReestr.counterList[i].Cnt, bodyReestr.counterList[i].ValCnt) + ")";
                        ExecSQL(_connDB, sqlStr, true);
                    }
                }
            }
        }

        /// <summary>
        /// Разбор строки платежа
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        protected virtual Returns ParseOneString(string str)
        {

            string[] fields = str.Split(';');
            Returns ret = Utils.InitReturns();
            var bodyReestr = FillBody(fields);
            _loadProtokol.SumCharge += bodyReestr.SumPlat;
            //проверка на существование оплаты(если уже есть, то не пишем)
            if (IsPaymentExists(bodyReestr.LSKod, bodyReestr.TransactionID, bodyReestr.SumPlat))
            {
                if (!InsertPayment(bodyReestr))
                {
                    ret.result = false;
                    string numStr = "\n Номер строки с ошибкой: " + _numRow;
                    ret.text = "\nОшибка при записи данных итогового реестра" + numStr;
                    return ret;

                }
                _loadProtokol.CountInsertedRows++;
            }
            return ret;
        }
    }

    //******************************************************************************************************************************************
    /// <summary>
    /// Класс для разбора строки платежа по реестру для Тулы
    /// </summary>
    public class DbReestrFromBank : DbBaseReestrFromBank
    {
        public DbReestrFromBank(IDbConnection connDB, FileNameStruct fileArgs, DbLoadProtokol loadProtokol)
            : base(connDB, fileArgs, loadProtokol)
        {

        }


        /// <summary>
        /// Проверка строк по списку возможных ошибок в заполнении файла
        /// </summary>
        /// <param name="rows"></param>
        /// <returns></returns>
        public override Returns FindErrors(string[] rows)
        {
            Returns ret = Utils.InitReturns();
            Utils.setCulture(); // установка региональных настроек
            var numRows = new Dictionary<int, string>();
            _loadProtokol.TotalSumPack = 0;
            for (int i = 0; i < rows.Length; i++)
            {
                bool add = false;
                string err = "";

                char[] splitSymbols = { ';', '|' };
                //проверяем структуру строки на целостность
                string[] rowEl = rows[i].Split(splitSymbols);
                if (rowEl.Length != 17)
                {
                    add = true;
                    err += (err != ""
                        ? ", Нарушен формат реестра: Неверное число полей в строке"
                        : " Нарушен формат реестра: Неверное число полей в строке");
                }

                //число полей по формату
                if (rowEl.Length == 17)
                {
                    //Код лицевого счета
                    if (!Regex.IsMatch(rowEl[0], @"^\d+$") || rowEl[0].Trim().Length > 20)
                    {
                        add = true;
                        err += (err != ""
                            ? ", Нарушен формат поля \"Код лицевого счета\""
                            : "Нарушен формат поля \"Код лицевого счета\"");
                    }

                    //Сумма платежа           
                    if (!Regex.IsMatch(rowEl[1], @"^[0-9]+(\.[0-9]+)?$"))
                    {
                        add = true;
                        err += (err != ""
                            ? ", Нарушен формат поля \"Сумма платежа\""
                            : "Нарушен формат поля \"Сумма платежа\"");
                    }
                    else
                    {
                        decimal sumOpl;
                        Decimal.TryParse(rowEl[1], out sumOpl);
                        _loadProtokol.TotalSumPack += sumOpl;
                    }

                    //Номер транзакции 
                    if (!Regex.IsMatch(rowEl[2], @"\d{26}"))
                    {
                        add = true;
                        err += (err != ""
                            ? ", Нарушен формат поля \"Номер транзакции\""
                            : "Нарушен формат поля \"Номер транзакции\"");
                    }
                    else
                    {
                        if (rowEl[2].Length >= 6)
                        {
                            //выковыриваем дату платежа из кода транзакции
                            DateTime paymentDay;
                            if (!DateTime.TryParseExact(rowEl[2].Substring(0, 6), "ddMMyy", CultureInfo.InvariantCulture,
                                    DateTimeStyles.None, out paymentDay))
                            {
                                add = true;
                                err += (err != ""
                                    ? ", Дата платежа в поле \"Номер транзакции\" не определена"
                                    : "Дата платежа в поле \"Номер транзакции\" не определена");
                            }
                            else
                                if (paymentDay > Points.DateOper.AddYears(1))
                                {
                                    add = true;
                                    err += (err != ""
                                        ? ", Дата платежа в поле \"Номер транзакции\" выходит за границы допустимого значения"
                                        : "Дата платежа в поле \"Номер транзакции\" выходит за границы допустимого значения");
                                }
                        }
                        else
                        {
                            add = true;
                            err += (err != ""
                                ? ", Дата платежа в поле \"Номер транзакции\" не определена"
                                : "Дата платежа в поле \"Номер транзакции\" не определена");
                        }
                    }

                    //Номер платежного поручения 
                    if (!Regex.IsMatch(rowEl[3], @"^[0-9]\d*$"))
                    {
                        add = true;
                        err += (err != ""
                            ? ", Нарушен формат поля \"Номер платежного поручения\""
                            : "Нарушен формат поля \"Номер платежного поручения\"");
                    }

                    //Дата платёжного поручения
                    if (!Regex.IsMatch(rowEl[4], @"^(\d{2}).\d{2}.(\d{4})$"))
                    {
                        add = true;
                        err += (err != ""
                            ? ", Нарушен формат поля \"Дата платёжного поручения\""
                            : "Нарушен формат поля \"Дата платёжного поручения\"");
                    }

                    int start_pos = 4;
                    //пробегаем по номерам ПУ и их показаниям
                    for (int j = 1; j <= 12; j += 2)
                    {
                        //Код счетчика 
                        if (!Regex.IsMatch(rowEl[start_pos + j], @"^[0-9]+(\.[0-9]+)?$|^НЕТ$"))
                        {
                            add = true;
                            err += (err != ""
                                ? ", Нарушен формат поля \"Код счетчика №" + (j / 2 + 1) + "\""
                                : "Нарушен формат поля \"Код счетчика №" + (j / 2 + 1) + "\"");
                        }

                        //Показание счетчика текущее
                        if (!Regex.IsMatch(rowEl[start_pos + j + 1], @"^[0-9]+(\.[0-9]+)?$|^НЕТ$"))
                        {
                            add = true;
                            err += (err != ""
                                ? ", Нарушен формат поля \"Показание счетчика текущее №" + (j / 2 + 1) + "\""
                                : "Нарушен формат поля \"Показание счетчика текущее №" + (j / 2 + 1) + "\"");
                        }
                    }

                }

                //добавляем номер строки в список строк с ошибками
                if (add) numRows.Add(i + 1, err);
            }
            if (numRows.Count > 0)
            {
                ret.result = false;
                _loadProtokol.AddUncorrectedRows("Номера строк с ошибками:\r\n");
                foreach (var num in numRows)
                {
                    _loadProtokol.AddUncorrectedRows("Строка: " + num.Key + ", Ошибки: " + num.Value);
                   // ret.text += "Строка: " + num.Key + ", Ошибки: " + num.Value + ";\r\n";
                }
               // ret.text = ret.text.Remove(ret.text.Length - 1, 1);
            }

            return ret;
        }

        protected override bool IsPaymentExists(string pkod, string transactionID, decimal sumCharge)
        {
            bool result = true;

            #region Чистим оплаты с нулевым кодом квитанции
            #region Удаляем счетчики

            var sql = " DELETE FROM " + Points.Pref + DBManager.sDataAliasRest + "tula_counters_reestr " +
                   " WHERE nzp_reestr_d in (select nzp_reestr_d " +
                   " FROM " + Points.Pref + DBManager.sDataAliasRest + "tula_file_reestr " +
                   " WHERE nzp_kvit_reestr=0 AND pkod=" + pkod + " " +
                           " AND transaction_id='" + transactionID + "')";
            ExecSQL(_connDB, sql, true);


            #endregion

            #region Удаляем платежи
            sql = " DELETE FROM " + Points.Pref + DBManager.sDataAliasRest + "tula_file_reestr " +
                    " WHERE nzp_kvit_reestr=0 AND pkod=" + pkod + " " +
                           " AND transaction_id='" + transactionID + "'";
            ExecSQL(_connDB, sql, true);

            #endregion
            #endregion Чистим оплаты с нулевым кодом квитанции

            sql = " SELECT sum(sum_charge) as sum_charge" +
                           " FROM " + Points.Pref + "_data" + tableDelimiter + "tula_file_reestr " +
                           " WHERE nzp_kvit_reestr>0 AND pkod=" + pkod + " " +
                           " AND transaction_id='" + transactionID + "'";

            Returns ret;
            object obj = ExecScalar(_connDB, sql, out ret, true);
            if (obj != null && obj != DBNull.Value)
            {
                result = false;

                if (Convert.ToDecimal(obj) > 0)
                {
                    if (sumCharge != Convert.ToDecimal(obj))
                    {
                        _loadProtokol.AddComment("Дублированный номер банковской транзакции для оплат с разными суммами " +
                                                 " платежный код " + pkod + " номер транзации " + transactionID);
                    }
                    else
                    {
                        _loadProtokol.AddComment("Дублированный номер банковской транзакции " +
                                                " платежный код " + pkod + " номер транзации " + transactionID);
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Проверка строк по списку возможных ошибок в заполнении файла
        /// </summary>
        /// <param name="rows"></param>
        /// <returns></returns>
        protected override ReestrBody FillBody(string[] fields)
        {

            var body = new ReestrBody();
            int index = 0;
            bool add = false;
            try
            {
                if (fields[index].Trim() != "")
                {
                    body.LSKod = fields[index].Trim();
                    if (body.LSKod.Length != 13)
                    {
                        MonitorLog.WriteLog("Ошибка, неверный лицевой счет ", MonitorLog.typelog.Error, 20, 201, true);
                    }
                }
                index++;

                if (fields[index].Trim() != "")
                {
                    body.SumPlat = Convert.ToDecimal(fields[index].Trim());
                }
                index++;

                if (fields[index].Trim() != "")
                {
                    body.TransactionID = fields[index].Trim();
                    if (body.TransactionID.Length > 30) body.TransactionID = body.TransactionID.Substring(0, 30);
                    if (body.TransactionID.Length >= 6)
                    {
                        //выковыриваем дату платежа из кода транзакции
                        body.PaymentDateFromTransactionID = DateTime.ParseExact(body.TransactionID.Substring(0, 6),
                            "ddMMyy",
                            CultureInfo.InvariantCulture);
                    }
                }
                index++;

                if (fields[index].Trim() != "")
                {
                    body.NomerPlatPoruch = fields[index].Trim();
                }
                index++;

                if (fields[index].Trim() != "")
                {
                    body.DatePlatPoruch = Convert.ToDateTime(fields[index].Trim()).ToString("dd.MM.yyyy");
                }
                index++;

                ReestrCounter counter = null;

                while (index < fields.Length)
                {
                    counter = new ReestrCounter();

                    if (fields[index].Trim() != "")
                    {
                        counter.Cnt = fields[index].Trim();
                        if (counter.Cnt == "НЕТ") counter.Cnt = String.Empty;
                    }
                    index++;

                    if (fields[index].Trim() != "")
                    {
                        counter.ValCnt = fields[index].Trim();
                        if (String.IsNullOrEmpty(counter.Cnt) && counter.ValCnt != "НЕТ") add = true;
                    }
                    index++;

                    body.counterList.Add(counter);
                }

                for (int i = 0; i < body.counterList.Count; i++)
                {
                    if (body.counterList[i].ValCnt == "НЕТ") body.counterList[i].ValCnt = String.Empty;
                }

                if (add)
                {
                    _loadProtokol.AddUncorrectedRows("Строка " + _numRow +
                                                     ": Нераспределенные показания приборов учета (нет номеров ПУ);");
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteException("Ошибка парсинга данных реестра", ex);
                throw;
            }

            return body;
        }

        /// <summary>
        /// сопоставление указанного в квитанции для реестра кол-ва строк и суммы оплат с данными в файле реестра 
        /// </summary>
        /// <param name="reestrRowsCount"></param>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public override Returns CheckReestrAtr(int reestrRowsCount, string fileName)
        {
            Returns ret;

            string sqlString = "SELECT count(*) FROM " + Points.Pref + sDataAliasRest + "tula_kvit_reestr " +
                        " where file_name='" + fileName + "' " +
                        " and sum_plat=" + _loadProtokol.TotalSumPack +
                        " and count_rows=" + reestrRowsCount;
            object obj = ExecScalar(_connDB, sqlString, out ret, true);
            int num = obj != DBNull.Value ? Convert.ToInt32(obj) : 0;
            if (num == 0)
            {
                ret.result = false;
                _loadProtokol.AddComment("\nВ файле реестра сумма платежей или количество строк не " +
                           " совпадает с данными в квитанции ");
            }
            else
            {
                ret.result = true;
            }
            return ret;
        }

        /// <summary>
        /// Добавление платежа в БД
        /// </summary>
        /// <param name="bodyReestr"></param>
        /// <returns></returns>
        protected override bool InsertPayment(ReestrBody bodyReestr)
        {
            if (bodyReestr.NomerPlatPoruch == null || bodyReestr.DatePlatPoruch == null)
            {
                _loadProtokol.AddComment(
                    "\nВ файле реестра не заполнены поля: \"Номер платежного поручения\" или \"Дата платежного поручения\"");
                return false;
            }

            int cnt = Math.Min(6, bodyReestr.counterList.Count);

            string sqlStr = "INSERT INTO " + Points.Pref + "_data" + tableDelimiter + "tula_file_reestr " +
                            " (pkod,nzp_kvit_reestr,sum_charge,transaction_id,nomer_plat_poruch," +
                            " date_plat_poruch, payment_datetime";

            for (int i = 1; i < Math.Min(cnt + 1, 7); i++)
            {
                sqlStr += ", cnt" + i + ", val_cnt" + i;
            }

            sqlStr += string.Format(") VALUES " + " ( {0}, {1}, {2}, {3}, {4}, {5},{6}",
                bodyReestr.LSKod, _fileArgs.KvitID, bodyReestr.SumPlat,
                Utils.EStrNull(bodyReestr.TransactionID),
                ((bodyReestr.NomerPlatPoruch == "НЕТ") ? "null" : Utils.EStrNull(bodyReestr.NomerPlatPoruch)),
                ((bodyReestr.DatePlatPoruch == "НЕТ") ? "null" : Utils.EStrNull(bodyReestr.DatePlatPoruch)),
                (bodyReestr.PaymentDateFromTransactionID == DateTime.MinValue
                //дата платежа определяется из кода транзакции!
                    ? "null"
                    : Utils.EStrNull(bodyReestr.PaymentDateFromTransactionID.ToShortDateString())));


            for (int i = 0; i < Math.Min(cnt, 6); i++)
            {
                sqlStr += ", " + FillNumCounter(bodyReestr.counterList[i].Cnt, bodyReestr.counterList[i].ValCnt);
            }

            sqlStr += ")";

            if (!ExecSQL(_connDB, sqlStr, true).result)
            {

                MonitorLog.WriteLog("Ошибка сохранения  " + (Constants.Viewerror ? "\n" + sqlStr : ""),
                    MonitorLog.typelog.Error, 20, 201, true);
                _loadProtokol.AddComment("\nОшибка сохранения данных реестра");
                if (bodyReestr.TransactionID.Length > 26)
                    _loadProtokol.AddComment(
                        ". Превышена длина поля \"Номер транзакции\", по формату " +
                        "длина поля составляет 26 символов");
                return false;
            }

            int nzpReestrD = GetSerialValue(_connDB);

            //Если есть счетчики добавляем
            InsertCounters(bodyReestr, nzpReestrD);

            return true;
        }

        /// <summary>
        /// Получить уникальный код счетчика 
        /// </summary>
        /// <param name="numCnt"></param>
        /// <returns></returns>
        override protected string GetNzpCounter(string nzp_counter, string pkod, string valCnt)
        {
            IDataReader reader = null;
            string nzp_counter_res = "0";

            try
            {
                string sql = "select k.pref, k.nzp_kvar, s.point from " +
                              Points.Pref + "_data" + DBManager.tableDelimiter + "kvar k, " + Points.Pref + sKernelAliasRest + "s_point s" +
                             " where k.nzp_wp=s.nzp_wp and k.pkod = " + pkod;
                Returns ret = new Returns(true);

                ret = ExecRead(_connDB, out reader, sql, true);
                if (!ret.result) throw new Exception(ret.text);

                if (reader.Read())
                {
                    string nzp_kvar = reader["nzp_kvar"].ToString();
                    string pref = reader["pref"].ToString().Trim();
                    string point = reader["point"].ToString().Trim();

                    sql = "select nzp_counter from " + pref + "_data" + DBManager.tableDelimiter + "counters_spis " +
                        " where nzp = " + nzp_kvar +
                        " and nzp_type = 3 " +
                        " and nzp_counter=" + nzp_counter;

                    DataTable resTable = ClassDBUtils.OpenSQL(sql, _connDB).GetData();
                    if (resTable.Rows.Count != 1)
                    {
                        _loadProtokol.AddBadCounter(nzp_counter, pkod, pref, point, valCnt.ToString());
                    }
                    else
                    {
                        nzp_counter_res = resTable.Rows[0][0].ToString();
                    }

                    resTable.Dispose();
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка определения кода ПУ  " + (Constants.Viewerror ? "\n" + ex.Message : ""), MonitorLog.typelog.Error, 20, 201, true);

                if (reader != null)
                {
                    reader.Close();
                    reader.Dispose();
                    reader = null;
                }

                nzp_counter_res = "0";
            }
            finally
            {
                reader.Close();
                reader.Dispose();
                reader = null;
            }

            return nzp_counter_res;
        }
    }

    //******************************************************************************************************************************************


    //******************************************************************************************************************************************
    /// <summary>
    /// Класс для разбора строки платежа по реестру для Марий Эл
    /// </summary>
    public class DbReestrFromBankMariyEl : DbBaseReestrFromBank
    {
        public DbReestrFromBankMariyEl(IDbConnection connDB, FileNameStruct fileArgs, DbLoadProtokol loadProtokol)
            : base(connDB, fileArgs, loadProtokol)
        {

        }

        /// <summary>
        /// Проверка строк по списку возможных ошибок в заполнении файла
        /// </summary>
        /// <param name="rows"></param>
        /// <returns></returns>
        override public Returns FindErrors(string[] rows)
        {
            Returns ret = Utils.InitReturns();
            Utils.setCulture(); // установка региональных настроек
            var numRows = new Dictionary<int, string>();
            for (int i = 0; i < rows.Length; i++)
            {
                string err = "";

                //проверяем структуру строки на целостность
                string[] rowEl = rows[i].Split(';');
                if (rowEl.Length != 27)
                {
                    err += (err != "" ? ", " : "") + "Нарушен формат реестра: Неверное число полей в строке";
                }
                else
                {
                    //число полей по формату

                    // Уникальный номер операции в системе платежного агента
                    if (rowEl[0].Trim().Length > 30)
                    {
                        err += (err != "" ? "," : "") + "Нарушен формат поля \"Уникальный номер операции в системе платежного агента\"";
                    }

                    // Служебное поле  платежного агента
                    if (rowEl[1].Trim().Length > 30)
                    {
                        err += (err != "" ? "," : "") + "Нарушен формат поля \"Служебное поле  платежного агента\"";
                    }

                    //Дата платёжного поручения
                    if (!Regex.IsMatch(rowEl[2], @"^(\d{2}).\d{2}.(\d{4})$"))
                    {
                        err += (err != "" ? "," : "") + "Нарушен формат поля \"Дата совершения платежа\"";
                    }

                    //Время совершения платежа
                    if (!Regex.IsMatch(rowEl[3], @"^(\d{2}):\d{2}:(\d{2})$"))
                    {
                        err += (err != "" ? "," : "") + "Нарушен формат поля \"Время совершения платежа\"";
                    }

                    //Код лицевого счета
                    if (!Regex.IsMatch(rowEl[4], @"^\d+$") || rowEl[4].Trim().Length != 13)
                    {
                        err += (err != "" ? "," : "") + "Нарушен формат поля \"Код лицевого счета\"";
                    }

                    //Адрес плательщика (неполный)  Адрес в формате: <№ дома с корпусом> - <№ квартиры></комната если есть>
                    if (rowEl[5].Trim().Length > 100)
                    {
                        err += (err != "" ? "," : "") + "Нарушен формат поля \"Адрес плательщика\"";
                    }

                    // Сумма платежа
                    if (!Regex.IsMatch(rowEl[6], @"^[0-9]+(\.[0-9]+)?$"))
                    {
                        err += (err != "" ? "," : "") + "Нарушен формат поля \"Сумма платежа\"";
                    }
                    else
                    {
                        decimal sumOpl;
                        Decimal.TryParse(rowEl[1], out sumOpl);
                        _loadProtokol.TotalSumPack += sumOpl;
                    }

                    int start_pos = 6;
                    //пробегаем по номерам ПУ и их показаниям
                    for (int j = 1; j <= 20; j += 2)
                    {
                        //Код счетчика 
                        if (rowEl[start_pos + j].Trim().Length > 30)
                        {
                            err += (err != "" ? "," : "") + "Нарушен формат поля \"Наименование счетчика №" + (j / 2 + 1) + "\"";
                        }

                        //Показание счетчика текущее
                        if (rowEl[start_pos + j + 1].Trim().Length > 0 && !Regex.IsMatch(rowEl[start_pos + j + 1], @"^[0-9]+(\.[0-9]+)?$"))
                        {
                            err += (err != "" ? "," : "") + "Нарушен формат поля \"Переданное показание счетчика №" + (j / 2 + 1);
                        }
                    }
                }

                //добавляем номер строки в список строк с ошибками
                if (err != "") numRows.Add(i + 1, err);
            }
            if (numRows.Count > 0)
            {
                ret.result = false;
                _loadProtokol.AddUncorrectedRows("Номера строк с ошибками:\r\n");
                foreach (var num in numRows)
                {
                    _loadProtokol.AddUncorrectedRows("Строка: " + num.Key + ", Ошибки: " + num.Value);
                    //ret.text += "Строка: " + num.Key + ", Ошибки: " + num.Value + ";\r\n";
                }
               // ret.text = ret.text.Remove(ret.text.Length - 1, 1);
            }

            return ret;
        }

        /// <summary>
        /// Проверка строк по списку возможных ошибок в заполнении файла
        /// </summary>
        /// <param name="rows"></param>
        /// <returns></returns>
        override protected ReestrBody FillBody(string[] fields)
        {
            var body = new ReestrBody();
            int index = 0;
            bool add = false;

            // Уникальный номер операции в системе платежного агента
            if (fields[index].Trim() != "")
            {
                body.TransactionID = fields[index].Trim();
            }
            index++;

            // Служебное поле  платежного агента
            if (fields[index].Trim() != "")
            {
                body.ServiceField = fields[index].Trim();
            }
            index++;

            // Дата совершения платежа
            if (fields[index].Trim() != "")
            {
                body.PaymentDate = fields[index].Trim();
            }
            index++;

            // Время совершения платежа
            if (fields[index].Trim() != "")
            {
                body.PaymentTime = fields[index].Trim();
            }
            index++;

            // Код ЛС
            if (fields[index].Trim() != "")
            {
                body.LSKod = fields[index].Trim();
            }
            index++;

            // Адрес плательщика 
            if (fields[index].Trim() != "")
            {
                body.Address = fields[index].Trim();
            }
            index++;

            // Сумма платежа
            if (fields[index].Trim() != "")
            {
                body.SumPlat = Convert.ToDecimal(fields[index].Trim());
            }
            index++;

            ReestrCounter counter = null;

            for (int i = 1; i <= 10; i++)
            {
                counter = new ReestrCounter();

                if (fields[index].Trim() != "")
                {
                    counter.Cnt = fields[index].Trim();
                }
                index++;

                if (fields[index].Trim() != "")
                {
                    counter.ValCnt = fields[index].Trim();
                    if (counter.Cnt == "" && counter.ValCnt != "НЕТ") add = true;
                }
                index++;

                body.counterList.Add(counter);
            }

            for (int i = 0; i < body.counterList.Count; i++)
            {
                if (body.counterList[i].ValCnt == "НЕТ") body.counterList[i].ValCnt = String.Empty;
            }

            if (add)
            {
                _loadProtokol.AddUncorrectedRows("Строка " + _numRow + ": Нераспределенные показания приборов учета (нет номеров ПУ);");
            }

            return body;
        }

        /// <summary>
        /// сопоставление указанного в квитанции для реестра кол-ва строк и суммы оплат с данными в файле реестра 
        /// </summary>
        /// <param name="reestrRowsCount"></param>
        /// <param name="fileName"></param>
        /// <returns></returns>
        override public Returns CheckReestrAtr(int reestrRowsCount, string fileName)
        {
            return new Returns(true, "", 0);
        }

        /// <summary>
        /// Добавление платежа в БД
        /// </summary>
        /// <param name="bodyReestr"></param>
        /// <returns></returns>
        override protected bool InsertPayment(ReestrBody bodyReestr)
        {
            string sqlStr = "INSERT INTO " + Points.Pref + "_data" + tableDelimiter + "tula_file_reestr " +
                            " (pkod, nzp_kvit_reestr, sum_charge, transaction_id, service_field, payment_datetime, address, date_plat_poruch, nomer_plat_poruch) VALUES (" +
                            bodyReestr.LSKod + ", " + _fileArgs.KvitID + ", " +
                            bodyReestr.SumPlat + ", " +
                            Utils.EStrNull(bodyReestr.TransactionID) + ", " +
                            Utils.EStrNull(bodyReestr.ServiceField) + ", " +
                            Utils.EStrNull(bodyReestr.PaymentDate + " " + bodyReestr.PaymentTime) + ", " +
                            Utils.EStrNull(bodyReestr.Address) + "," +
                            Utils.EStrNull(bodyReestr.PaymentDate) + ", " +
                            _fileArgs.KvitID + ")";
            if (!ExecSQL(_connDB, sqlStr, true).result)
            {

                MonitorLog.WriteLog("Ошибка сохранения  " + (Constants.Viewerror ? "\n" + sqlStr : ""),
                    MonitorLog.typelog.Error, 20, 201, true);
                _loadProtokol.AddComment("\nОшибка сохранения данных реестра");
                if (bodyReestr.TransactionID.Length > 30)
                    _loadProtokol.AddComment(
                        ". Превышена длина поля \"Номер транзакции\", по формату " +
                        "длина поля составляет 30 символов");
                return false;
            }

            int nzpReestrD = GetSerialValue(_connDB);
            //Если есть счетчики добавляем
            InsertCounters(bodyReestr, nzpReestrD);

            return true;
        }

        /// <summary>
        /// Получить уникальный код счетчика 
        /// </summary>
        /// <param name="numCnt"></param>
        /// <returns></returns>
        override protected string GetNzpCounter(string numCnt, string pkod, string valCnt)
        {
            IDataReader reader = null;
            string nzp_coutner = "";

            try
            {
                string sql = "select pref, nzp_kvar from " + Points.Pref + "_data" + DBManager.tableDelimiter + "kvar where pkod = " + pkod;
                Returns ret = new Returns(true);

                ret = ExecRead(_connDB, out reader, sql, true);
                if (!ret.result)
                {
                    _loadProtokol.AddUncorrectedRows("Не удалось определить код ЛС и префикс. Платежный код ЛС: " + pkod);
                    return nzp_coutner;
                }

                if (reader.Read())
                {
                    string nzp_kvar = reader["nzp_kvar"].ToString();
                    string pref = reader["pref"].ToString().Trim();
                    string num_cnt = numCnt.Replace(" ", "_");

                    sql = "select nzp_counter from " + pref + "_data" + DBManager.tableDelimiter + "counters_spis " +
                        " where nzp = " + nzp_kvar +
                        " and nzp_type = 3 " +
                        " and num_cnt ilike '%" + num_cnt + "'";

                    DataTable resTable = ClassDBUtils.OpenSQL(sql, _connDB).GetData();
                    if (resTable.Rows.Count != 1)
                    {
                        _loadProtokol.AddUncorrectedRows("Не удалось однозначно определить код ПУ. Платежный код ЛС: " + pkod + ", № ПУ:" + num_cnt);
                        nzp_coutner = "";
                    }
                    else
                    {
                        nzp_coutner = resTable.Rows[0][0].ToString();
                    }

                    resTable.Dispose();
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка определения кода ПУ  " + (Constants.Viewerror ? "\n" + ex.Message : ""), MonitorLog.typelog.Error, 20, 201, true);
                nzp_coutner = "";
            }
            finally
            {
                if (reader != null)
                {
                    reader.Close();
                    reader.Dispose();
                    reader = null;
                }
            }

            return nzp_coutner;
        }

        /// <summary>
        /// Проверка на существование платежа
        /// </summary>
        /// <param name="pkod"></param>
        /// <param name="transactionID"></param>
        /// <param name="sumCharge">Сумма оплаты</param>
        /// <returns></returns>
        override protected bool IsPaymentExists(string pkod, string transactionID, decimal sumCharge)
        {
            bool result = true;

            var sql = " SELECT sum(sum_charge) as sum_charge" +
                            " FROM " + Points.Pref + "_data" + tableDelimiter + "tula_file_reestr " +
                            " WHERE  pkod=" + pkod + " " +
                            " AND transaction_id='" + transactionID + "'";

            Returns ret;
            object obj = ExecScalar(_connDB, sql, out ret, true);
            if (obj != null && obj != DBNull.Value)
            {
                result = false;

                if (Convert.ToDecimal(obj) > 0)
                {
                    if (sumCharge != Convert.ToDecimal(obj))
                    {
                        _loadProtokol.AddComment("Дублированный номер банковской транзакции для оплат с разными суммами " +
                                                 " платежный код " + pkod + " номер транзации " + transactionID);
                    }
                    else
                    {
                        _loadProtokol.AddComment("Дублированный номер банковской транзакции " +
                                                " платежный код " + pkod + " номер транзации " + transactionID);
                    }
                }
            }

            return result;
        }
    }
    //******************************************************************************************************************************************


    /// <summary>
    /// Класс разбора итоговой квитанции по реестру
    /// </summary>
    public class DbReestKvit : DataBaseHead
    {

        private readonly IDbConnection _connDB;

        /// <summary>
        /// Заголовок
        /// </summary>
        private struct ReestrHead
        {
            public string Date { get; set; }
            public string FileName { get; set; }
            public int KodDopol { get; set; }
            public int FileLineCount { get; set; }
            public decimal SumPlat { get; set; }
            public string IsKvit { get; set; }
            public string BranchID { get; set; }
        }



        public DbReestKvit(IDbConnection connDB)
        {
            _connDB = connDB;
        }


        /// <summary>
        /// Заполнение структуры заголовка строки
        /// </summary>
        /// <param name="fields"></param>
        /// <returns></returns>
        private ReestrHead FillHead(string[] fields)
        {
            ReestrHead headReestr = new ReestrHead();
            int index = 0;

            if (fields[index].Trim() != "")
            {
                headReestr.Date = fields[index].Trim();
            }
            index++;

            if (fields[index].Trim() != "")
            {
                headReestr.FileName = fields[index].Trim();
            }
            index++;

            if (fields[index].Trim() != "")
            {
                headReestr.KodDopol = Convert.ToInt32(fields[index].Trim());
            }
            index++;

            if (fields[index].Trim() != "")
            {
                headReestr.FileLineCount = Convert.ToInt32(fields[index].Trim());
            }
            index++;

            if (fields[index].Trim() != "")
            {
                headReestr.SumPlat = Convert.ToDecimal(fields[index].Replace(',', '.').Trim());
            }

            return headReestr;
        }


        /// <summary>
        /// Добавлении квитанции по реестру
        /// </summary>
        /// <param name="headReestr"></param>
        /// <param name="nzp_download"></param>
        private void InsertKvit(ReestrHead headReestr, int nzp_download)
        {
            headReestr.IsKvit = "1";
            string sqlStr = "INSERT INTO " + Points.Pref + sDataAliasRest + "tula_kvit_reestr (date_plat,file_name," +
                            " kod_dop,count_rows,sum_plat,is_itog, branch_id,nzp_download ) " +
                            " VALUES " +
                            " ( '" + headReestr.Date + "', '" + headReestr.FileName + "', " +
                            headReestr.KodDopol + ", " +
                            headReestr.FileLineCount + ", " + headReestr.SumPlat + ", " +
                            headReestr.IsKvit + ", '" + headReestr.BranchID + "'," + nzp_download + ")";

            if (!ExecSQL(_connDB, sqlStr, true).result)
                throw new UserException("Ошибка при записи данных квитанции итогового реестра");

        }

        /// <summary>
        /// Проверка квитанций на загруженность в БД
        /// </summary>
        /// <param name="typeKvit"></param>
        /// <param name="fileName"></param>
        /// <returns></returns>
        private Returns KvitVerify(FileNameStruct.ReestrTypes typeKvit, ReestrHead head)
        {
            Returns ret;
            string sqlString = " SELECT count(*) FROM " + Points.Pref + sDataAliasRest + "tula_kvit_reestr " +
                               " where file_name='" + head.FileName + "' and date_plat=" + Utils.EStrNull(head.Date);

            object obj = ExecScalar(_connDB, sqlString, out ret, true);
            if (!ret.result || obj == null || obj == DBNull.Value)
                throw new UserException("Ошибка выборки квитанции " + head.FileName);
            int num = Convert.ToInt32(obj);
            ret.text = "";
            if (num > 0)
            {
                ret.text = "\nВ файле квитанции указано имя реестра, который уже загружен ранее.";
                ret.result = false;
                return ret;
            }
            return ret;
        }

        /// <summary>
        /// Разбор квитанции по реестру
        /// </summary>
        /// <param name="str">Строка для разбора</param>
        /// <returns></returns>
        private Returns ParseOneString(FileNameStruct fileArgs, string str)
        {
            string[] fields = str.Split('|');
            ReestrHead headReestr = FillHead(fields);
            headReestr.BranchID = fileArgs.OtdNumber;
            Returns ret;
            if ((ret = KvitVerify(fileArgs.FileType, headReestr)).result)
            {
                InsertKvit(headReestr, fileArgs.NzpDownload);
            }
            else
            {
                ExecSQL(_connDB,
                    "DELETE FROM " + Points.Pref + sDataAliasRest + "tula_reestr_downloads WHERE nzp_download=" +
                    fileArgs.NzpDownload, true);
            }
            return ret;
        }


        /// <summary>
        /// Построчная разборка реестра квитанций
        /// </summary>
        /// <param name="reestrStrings"></param>
        /// <param name="branchID">Код подразделения</param>
        /// <param name="typeKvit">Тип файла</param>
        /// <returns></returns>
        public Returns Parse(string[] reestrStrings, FileNameStruct fileArgs)
        {
            Returns ret = Utils.InitReturns();
            foreach (var str in reestrStrings)
            {
                ret = ParseOneString(fileArgs, str);
                if (!ret.result) return ret;
            }
            return ret;
        }

    }


    /// <summary>
    /// Класс для сохранения реестра оплат
    /// </summary>
    abstract public class DbSaveReestr : DataBaseHead
    {
        protected readonly FileNameStruct _fileArgs;
        protected readonly DbLoadProtokol _loadProtokol;
        protected IDbConnection _connDb;
        protected int _nzpBank;

        public DbSaveReestr(IDbConnection connDb, FileNameStruct fileArgs, DbLoadProtokol loadProtokol, int nzpBank)
        {
            _fileArgs = fileArgs;
            _loadProtokol = loadProtokol;
            _connDb = connDb;
            _nzpBank = nzpBank;
        }

        /// <summary>
        /// Управляющая функция
        /// </summary>

        /// <param name="finder"></param>
        /// <returns></returns>
        public Returns SyncLsAndInsertPack(FilesImported finder)
        {
            Returns ret = Utils.InitReturns();

            #region связываем с ЛС из системы

            string sql = " update  " + Points.Pref + sDataAliasRest + "tula_file_reestr " +
                         " set nzp_kvar= " +
                         " (select k.nzp_kvar from " + Points.Pref + sDataAliasRest + "kvar k " +
                         " where k.pkod=" + Points.Pref + sDataAliasRest + "tula_file_reestr.pkod) " +
                         " where nzp_kvit_reestr=" + _fileArgs.KvitID + " ";
            if (!ExecSQL(_connDb, sql, true).result)
            {
                ret.text = "Ошибка сопоставления платежных кодов";
                ret.result = false;
                MonitorLog.WriteLog("Ошибка сопоставления платежных кодов: " + sql, MonitorLog.typelog.Error, true);
                return ret;
            }

            #endregion

            _loadProtokol.SetProcent(40, -999);
            //запись в систему показаний ПУ и оплат 
            ret = InsertPack(finder);
            if (!ret.result)
            {
                MonitorLog.WriteLog("Ошибка записи реестра " + _fileArgs.Number + _fileArgs.Month +
                    _fileArgs.Day + _fileArgs.OtdNumber + " в систему: " + ret.text, MonitorLog.typelog.Error, true);
            }

            return ret;
        }


        //запись в систему: пачки, оплаты, показания ПУ
        private Returns InsertPack(FilesImported finder)
        {
            Returns ret;

            string datPrev = "'" +
                             new DateTime(Points.CalcMonth.year_, Points.CalcMonth.month_, 1).AddMonths(-1)
                                 .ToShortDateString() + "'"; //предыдущий рассчетный месяц
            string year = (Points.DateOper.Year - 2000).ToString("00");

            string operDay = (new DateTime(Points.DateOper.Year, Points.DateOper.Month, Points.DateOper.Day)).ToString(
                "dd.MM.yyyy");

            //получаем локального пользователя
            /*var db = new DbWorkUser();
            int nzpUser = db.GetLocalUser(_connDb, finder, out ret);
            db.Close();*/

            int nzpUser = finder.nzp_user;

            #region Пачки оплат

            try
            {
                //проверяем кол-во пачек. если >1 создаем суперпачку и связываем с ней остальные пачки
                string sql = " SELECT nomer_plat_poruch, date_plat_poruch " +
                             " FROM " + Points.Pref + sDataAliasRest + "tula_file_reestr " +
                             " WHERE nzp_kvit_reestr=" + _fileArgs.KvitID + "" +
                             " GROUP BY nomer_plat_poruch, date_plat_poruch ";
                var packs = ClassDBUtils.OpenSQL(sql, _connDb).resultData;

                if (packs.Rows.Count > 1)
                {
                    int progress = 30 / packs.Rows.Count;
                    int superPack = SaveSuperPack(year, operDay);

                    for (int i = 0; i < packs.Rows.Count; i++)
                    {
                        ret = SaveOnePack(packs.Rows[i], year, superPack, operDay, nzpUser, datPrev);
                        if (!ret.result) return ret;
                        _loadProtokol.SetProcent(40 + progress * i, (int)StatusWWB.InProcess);
                    }

                }
                else if (packs.Rows.Count == 1)
                {
                    ret = SaveOnePack(packs.Rows[0], year, 0, operDay, nzpUser, datPrev);
                    if (!ret.result) return ret;
                }




                _loadProtokol.SetProcent(70, (int)StatusWWB.InProcess);

                #region Сохранение счетчиков

                var dbSaveReestrCounters = new DbSaveReestrCounters(_connDb, _fileArgs, _loadProtokol);
                ret = dbSaveReestrCounters.SavePackCounters(packs, year);

                #endregion
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка в функции InsertPack " + ex,
                    MonitorLog.typelog.Error, true);
                return new Returns(false, "Ошибка при загрузке пачек оплат в режиме взаимодействие с Банком");
            }
            #endregion
            return ret;
        }

        /// <summary>
        /// Сохранение пачки в pack
        /// </summary>
        /// <param name="pack"></param>
        /// <param name="year"></param>
        /// <param name="insertedRowsForPack"></param>
        /// <param name="sumPack"></param>
        /// <param name="operDay"></param>
        /// <returns></returns>
        abstract protected Returns InsertPack(DataRow pack, string year, int insertedRowsForPack, decimal sumPack, string parPack, string operDay);

        /// <summary>
        /// Сохранение одной пачки с платежами
        /// </summary>
        /// <param name="pack"></param>
        /// <param name="year"></param>
        /// <param name="idSuperPack"></param>
        /// <param name="operDay"></param>
        /// <param name="nzpUser"></param>
        /// <param name="datPrev"></param>
        /// <returns></returns>
        private Returns SaveOnePack(DataRow pack, string year, int idSuperPack,
            string operDay, int nzpUser, string datPrev)
        {
            Returns ret;

            string finAlias = Points.Pref + "_fin_" + year + tableDelimiter;
            decimal sumPack = 0;
            //достаем реальные значения оплаты для пачки, т.к. некоторые записи уже могли быть загружены ранее в период.реестре
            string sql = " SELECT sum(sum_charge) FROM " + Points.Pref + sDataAliasRest + "tula_file_reestr" +
                         " WHERE nzp_kvit_reestr=" + _fileArgs.KvitID +
                         " AND nomer_plat_poruch='" + pack["nomer_plat_poruch"] + "'" +
                         " AND date_plat_poruch='" + pack["date_plat_poruch"] + "'";
            object obj = ExecScalar(_connDb, sql, out ret, true);

            if (obj != null && obj != DBNull.Value)
            {
                sumPack = Convert.ToDecimal(obj);
            }
            else
            {
                MonitorLog.WriteLog("Ошибка получения суммы оплат для пачки: " + sql,
                    MonitorLog.typelog.Error, true);
            }


            //кол-во строк в пачке
            var insertedRowsForPack = 0;
            sql = " SELECT count(*) FROM " + Points.Pref + "_data" + tableDelimiter + "tula_file_reestr" +
                  " WHERE nzp_kvit_reestr=" + _fileArgs.KvitID +
                  " AND nomer_plat_poruch='" + pack["nomer_plat_poruch"] + "'" +
                  " AND date_plat_poruch='" + pack["date_plat_poruch"] + "' ";
            obj = ExecScalar(_connDb, sql, out ret, true);

            if (obj != null && obj != DBNull.Value)
            {
                insertedRowsForPack = Convert.ToInt32(obj);
            }
            else
            {
                MonitorLog.WriteLog("Ошибка получения кол-ва строк для пачки: " + sql,
                    MonitorLog.typelog.Error, true);
            }

            string parPack;
            if (idSuperPack == 0) parPack = "NULL";
            else parPack = idSuperPack.ToString(CultureInfo.InvariantCulture);

            //записываем в pack
            ret = InsertPack(pack, year, insertedRowsForPack, sumPack, parPack, operDay);
            if (!ret.result)
            {
                return new Returns(false, "Ошибка записи пачек оплаты");
            }

            var thisPack = GetSerialValue(_connDb);
            _loadProtokol.NzpPack.Add(thisPack);

            //Добавляем оплаты по ЛС
            sql = " insert into " + finAlias + "pack_ls " +
                  " (nzp_pack, num_ls, g_sum_ls, sum_ls, kod_sum,  paysource, id_bill, dat_vvod, info_num, " +
                  " inbasket, alg, unl, incase, pkod, nzp_user,dat_month, transaction_id) " +
                  " select " + thisPack + ", k1.num_ls, f.sum_charge, 0 as sum_ls, 33, 1 as paysource," +
                  "  0 as id_bill" +
                  ", payment_datetime " + //дата оплаты = дата из кода транзакции
                // костыль
                  " ,(case when length(f.transaction_id) >= 10 then substr(f.transaction_id,10,6) else f.transaction_id end)" + sConvToInt + " as num_oper, " +
                  " (case when f.nzp_kvar is not null then 0 else 1 end) as inbasket, " +
                  " 0 as alg, 0 as unl, 0 as incase, " +
                  " (case when f.pkod is null or f.pkod>9999999999999 then 0 else f.pkod end), " +
                  nzpUser + " ," + datPrev + ", f.transaction_id " +
                  " from " +
                  Points.Pref + "_data" + tableDelimiter + "tula_kvit_reestr k, " +
                  Points.Pref + "_data" + tableDelimiter + "tula_file_reestr f " +
                  " LEFT OUTER JOIN " + Points.Pref + "_data" + tableDelimiter +
                  "kvar k1  on f.nzp_kvar=k1.nzp_kvar  " +
                  " where k.nzp_kvit_reestr=f.nzp_kvit_reestr " +
                  "       and k.nzp_kvit_reestr=" + _fileArgs.KvitID + " " +
                  "       and f.nomer_plat_poruch='" + pack["nomer_plat_poruch"] + "'" +
                  "       and f.date_plat_poruch='" + pack["date_plat_poruch"].ToString().Trim() + "'";
            ret = ExecSQL(_connDb, sql, true);
            if (!ret.result)
            {
                ret.text = "Ошибка записи оплат по лицевым счетам";
                return ret;
            }

            //записываем не соотнесенные с системой ЛС в pack_ls_err  
            sql = " insert into " + finAlias + "pack_ls_err (nzp_pack_ls, nzp_err, note) " +
                  " select nzp_pack_ls, 666,pkod " +
                  " from  " + finAlias + "pack_ls " +
                  " where nzp_pack= " + thisPack + " and inbasket=1";
            if (!ExecSQL(_connDb, sql, true).result)
            {
                ret.text = "Ошибка записи не сопоставленных лицевых счетов";
                ret.result = false;
                return ret;
            }
            return ret;
        }

        /// <summary>
        /// Сохранение cуперпачки в pack
        /// </summary>
        /// <param name="pack"></param>
        /// <param name="year"></param>
        /// <param name="insertedRowsForPack"></param>
        /// <param name="sumPack"></param>
        /// <param name="operDay"></param>
        /// <returns></returns>
        protected Returns InsertSuperPack(string year, decimal sumPack, string operDay)
        {
            string sql = " insert into " + Points.Pref + "_fin_" + year + tableDelimiter + "pack  " +
                  "(pack_type, nzp_bank,num_pack, dat_pack, count_kv, sum_pack, real_count, flag, dat_vvod,  file_name,dat_uchet) " +
                  " select 10, 1000, substr(k.file_name,0,8), k.date_plat, " + _loadProtokol.CountInsertedRows + ", " +
                  sumPack + "," + _loadProtokol.CountInsertedRows + ", 11, " + sCurDateTime + ",k.file_name " +
                  ", '" + operDay + "' " +
                  " from " + Points.Pref + "_data" + tableDelimiter + "tula_kvit_reestr k " +
                  " where k.nzp_kvit_reestr=" + _fileArgs.KvitID;
            return ExecSQL(_connDb, sql, true);
        }

        /// <summary>
        /// Сохранение суперпачки
        /// </summary>
        /// <param name="year"></param>
        /// <param name="operDay"></param>
        /// <returns></returns>
        private int SaveSuperPack(string year, string operDay)
        {
            Returns ret;
            int result = 0;
            decimal sumPack;
            //достаем реальные значения оплаты для пачки, т.к. некоторые записи уже могли быть 
            //загружены ранее в период.реестре
            string sql = " select sum(sum_charge) " +
                         " from " + Points.Pref + sDataAliasRest + "tula_file_reestr " +
                         " where nzp_kvit_reestr=" + _fileArgs.KvitID + "";

            object obj = ExecScalar(_connDb, sql, out ret, true);

            if (obj != null && obj != DBNull.Value)
            {
                sumPack = Convert.ToDecimal(obj);
            }
            else
            {
                MonitorLog.WriteLog("Ошибка получения суммы оплат для суперпачки: " + sql,
                    MonitorLog.typelog.Error, true);
                return result;

            }

            ret = InsertSuperPack(year, sumPack, operDay);
            if (!ret.result)
                throw new UserException("Ошибка записи пачек оплаты");

            result = GetSerialValue(_connDb);


            sql = " update " + Points.Pref + "_fin_" + year + tableDelimiter + "pack " +
                  " set par_pack=" + result + " " +
                  " where nzp_pack=" + result;
            if (!ExecSQL(_connDb, sql, true).result)
                throw new UserException("Ошибка записи пачек оплаты");

            return result;


        }
    }

    public class DbTulaSaveReestr : DbSaveReestr
    {
        public DbTulaSaveReestr(IDbConnection connDb, FileNameStruct fileArgs, DbLoadProtokol loadProtokol, int nzpBank)
            : base(connDb, fileArgs, loadProtokol, nzpBank)
        {

        }

        override protected Returns InsertPack(DataRow pack, string year, int insertedRowsForPack, decimal sumPack, string parPack, string operDay)
        {
            //записываем в pack
            string sql = " INSERT INTO " + Points.Pref + "_fin_" + year + tableDelimiter + "pack  " +
                  "(pack_type, nzp_bank,num_pack, par_pack , dat_pack, count_kv, " +
                  " sum_pack, real_count, flag, dat_vvod,  file_name,dat_uchet) " +
                  " select 10, b.nzp_bank, '" + pack["nomer_plat_poruch"] + "'," +
                  parPack + ",'" + pack["date_plat_poruch"] + "', " + insertedRowsForPack + ", " +
                  +sumPack + "," + insertedRowsForPack + ", 11, " + sCurDateTime + ",k.file_name " +
                  ", '" + operDay + "' " +
                  " from " + Points.Pref + sDataAliasRest + "tula_s_bank b, " +
                  "      " + Points.Pref + sDataAliasRest + "tula_kvit_reestr k " +
                  " where k.nzp_kvit_reestr=" + _fileArgs.KvitID +
                  " and b.branch_id = k.branch_id " +
                  " and b.is_actual<>100";
            return ExecSQL(_connDb, sql, true);
        }
    }

    public class DbMariiElSaveReestr : DbSaveReestr
    {
        public DbMariiElSaveReestr(IDbConnection connDb, FileNameStruct fileArgs, DbLoadProtokol loadProtokol, int nzpBank)
            : base(connDb, fileArgs, loadProtokol, nzpBank)
        {

        }

        override protected Returns InsertPack(DataRow pack, string year, int insertedRowsForPack, decimal sumPack, string parPack, string operDay)
        {
            //записываем в pack
            string sql = " INSERT INTO " + Points.Pref + "_fin_" + year + tableDelimiter + "pack  " +
                  "(pack_type, nzp_bank,num_pack, par_pack , dat_pack, count_kv, " +
                  " sum_pack, real_count, flag, dat_vvod,  file_name,dat_uchet) " +
                  " select 10, " + _nzpBank + ", '" + pack["nomer_plat_poruch"] + "'," +
                  parPack + ",'" + pack["date_plat_poruch"] + "', " + insertedRowsForPack + ", " +
                  +sumPack + "," + insertedRowsForPack + ", 11, " + sCurDateTime + ",k.file_name " +
                  ", '" + operDay + "' " +
                  " from " + Points.Pref + sDataAliasRest + "tula_kvit_reestr k " +
                  " where k.nzp_kvit_reestr=" + _fileArgs.KvitID;
            return ExecSQL(_connDb, sql, true);
        }
    }

    /// <summary>
    /// Класс сохранения счетчиков из оплат от банка
    /// </summary>
    public class DbSaveReestrCounters : DataBaseHead
    {
        private readonly FileNameStruct _fileArgs;
        private readonly DbLoadProtokol _loadProtokol;
        private IDbConnection _connDb;
        private Dictionary<string, Dictionary<int, decimal>> maxDiffBetweenValues;

        public DbSaveReestrCounters(IDbConnection connDb, FileNameStruct fileArgs, DbLoadProtokol loadProtokol)
        {
            _fileArgs = fileArgs;
            _loadProtokol = loadProtokol;
            _connDb = connDb;
            maxDiffBetweenValues = GetMaxDiffBetweenValuesDict();
        }

        /// <summary>
        /// По всем пачкам платежей сохраняет счетчики
        /// </summary>
        /// <param name="packs">Таблица пачек</param>
        /// <param name="year">Финансовый год</param>
        /// <returns></returns>
        public Returns SavePackCounters(DataTable packs, string year)
        {
            Returns ret = Utils.InitReturns();
            try
            {

                //цикл по пачкам
                if (packs != null && packs.Rows.Count > 0)
                {
                    double progress = 20d / packs.Rows.Count;
                    int i = 0;
                    foreach (DataRow pack in packs.Rows)
                    {
                        i++;
                        double curProgress = 70 + progress * i;
                        ret = SaveOnePackCounters(pack, year, 70 + progress * i, progress);
                        if (!ret.result) return ret;
                        _loadProtokol.SetProcent(curProgress, (int)StatusWWB.InProcess);
                    }
                }
            }
            catch (Exception ex)
            {

                ret.result = false;
                ret.text = "Ошибка сохранения приборов учета";
                MonitorLog.WriteLog(ret.text + " " + ex, MonitorLog.typelog.Error, true);
            }

            return ret;
        }


        /// <summary>
        /// Сохранение платежей одной пачки
        /// </summary>
        /// <param name="pack">Заголовок пачки</param>
        /// <param name="year">Год в финансах</param>
        /// <param name="curProgress">Текущая позиция прогресса</param>
        /// <param name="step">Шаг</param>
        /// <returns></returns>
        public Returns SaveOnePackCounters(DataRow pack, string year, double curProgress, double step)
        {
            Returns ret = Utils.InitReturns();

            try
            {

                #region Заполняем временную таблицу со счетчиками

                string sql = " Create temp table tmp_counters (" +
                             " nzp_kvar integer, " +
                             " nzp_counter integer," +
                             " nzp_serv integer default 0," +
                             " val_file " + DBManager.sDecimalType + "(14,4)," +
                             " val_cnt " + DBManager.sDecimalType + "(14,4)," +
                             " nzp_wp integer," +
                             " pref char(10)," +
                             " point varchar(100)," +
                             " transaction_id varchar," +
                             " numCnt char(100)," +
                             " old_dat_uchet Date," +
                             " rashod  " + DBManager.sDecimalType + "(14,4) default 0," +
                             " max_diff " + DBManager.sDecimalType + "(14,4)," +
                             " pkod " + DBManager.sDecimalType + "(13,0) default 0," +
                             " cnt_stage integer)" + DBManager.sUnlogTempTable;
                ExecSQL(_connDb, sql, true);

                sql = " insert into tmp_counters(nzp_kvar, nzp_counter,  val_file, pkod, " +
                      "nzp_wp, pref, point, numCnt, transaction_id)" +
                      " select  kv.nzp_kvar, k.nzp_counter, k.val_cnt, a.pkod, kv.nzp_wp, " +
                      " kv.pref,s.point, cnt, a.transaction_id " +
                      " from " +
                      "      " + Points.Pref + sDataAliasRest + "tula_counters_reestr k, " +
                      "      " + Points.Pref + sDataAliasRest + "tula_file_reestr a, " +
                      "      " + Points.Pref + sDataAliasRest + "kvar kv, " +
                      "      " + Points.Pref + sKernelAliasRest + "s_point s " +
                      " where s.nzp_wp=kv.nzp_wp " +
                      " and a.nzp_reestr_d=k.nzp_reestr_d and kv.nzp_kvar = a.nzp_kvar " +
                      " and nzp_kvit_reestr=" + _fileArgs.KvitID +
                      " and nomer_plat_poruch='" + pack["nomer_plat_poruch"] + "'" +
                      " and date_plat_poruch='" + pack["date_plat_poruch"].ToString().Trim() + "'";
                ExecSQL(_connDb, sql, true);


                ExecSQL(_connDb, "create index ixtmp_tc_01 on tmp_counters(nzp_counter)", true);
                ExecSQL(_connDb, "create index ixtmp_tc_02 on tmp_counters(nzp_kvar)", true);
                ExecSQL(_connDb, "create index ixtmp_tc_03 on tmp_counters(pkod)", true);
                ExecSQL(_connDb, "create index ixtmp_tc_04 on tmp_counters(transaction_id)", true);
                ExecSQL(_connDb, DBManager.sUpdStat + " tmp_counters", true);
                _loadProtokol.SetProcent(curProgress + step / 4, (int)StatusWWB.InProcess);
                #endregion

                #region Заполнение дополнительных полей

                sql = "select pref, nzp_wp from tmp_counters group by 1,2";
                var prefTable = DBManager.ExecSQLToTable(_connDb, sql);
                foreach (DataRow pref in prefTable.Rows)
                {
                    sql = " update tmp_counters set nzp_serv=(select max(nzp_serv) " +
                          " from " + pref["pref"].ToString().Trim() + sDataAliasRest + "counters_spis a" +
                          " where tmp_counters.nzp_counter = a.nzp_counter and nzp_type=3) " +
                          " where  nzp_wp = " + pref["nzp_wp"];
                    ExecSQL(_connDb, sql, true);

                    sql = " update tmp_counters set old_dat_uchet=(select max(dat_uchet)  " +
                         "          from " + pref["pref"].ToString().Trim() + sDataAliasRest + "counters a" +
                         "          where tmp_counters.nzp_counter = a.nzp_counter and a.is_actual=1) " +
                         " where  nzp_wp = " + pref["nzp_wp"] + " and nzp_serv is not null ";
                    ExecSQL(_connDb, sql, true);

                    sql = " update tmp_counters set val_cnt=(select max(val_cnt)  " +
                          "          from " + pref["pref"].ToString().Trim() + sDataAliasRest + "counters a" +
                          "          where tmp_counters.nzp_counter = a.nzp_counter and a.is_actual=1 " +
                          "          and a.dat_uchet=tmp_counters.old_dat_uchet ) " +
                          " where  nzp_wp = " + pref["nzp_wp"] + " and nzp_serv is not null " +
                          " and old_dat_uchet  is not null";
                    ExecSQL(_connDb, sql, true);

                    sql = " update tmp_counters set numCnt=(select num_cnt  " +
                         "          from " + pref["pref"].ToString().Trim() + sDataAliasRest + "counters_spis a" +
                         "          where tmp_counters.nzp_counter = a.nzp_counter ) " +
                         " where  nzp_wp = " + pref["nzp_wp"] + " and nzp_counter is not null ";
                    ExecSQL(_connDb, sql, true);

                    #region проставляем максимальную разницу по услугам

                    sql = " update tmp_counters set max_diff= " + GetDiffByServ(pref["pref"].ToString().Trim(), 25) +
                          " where  nzp_wp = " + pref["nzp_wp"] + " and nzp_serv =25 ";
                    ExecSQL(_connDb, sql, true);

                    sql = " update tmp_counters set max_diff= " + GetDiffByServ(pref["pref"].ToString().Trim(), 8) +
                          " where  nzp_wp = " + pref["nzp_wp"] + " and nzp_serv =8 ";
                    ExecSQL(_connDb, sql, true);

                    sql = " update tmp_counters set max_diff= " + GetDiffByServ(pref["pref"].ToString().Trim(), 9) +
                          " where  nzp_wp = " + pref["nzp_wp"] + " and nzp_serv =9 ";
                    ExecSQL(_connDb, sql, true);


                    sql = " update tmp_counters set max_diff= " + GetDiffByServ(pref["pref"].ToString().Trim(), 10) +
                          " where  nzp_wp = " + pref["nzp_wp"] + " and nzp_serv =10 ";
                    ExecSQL(_connDb, sql, true);

                    sql = " update tmp_counters set max_diff= 10000" +
                          " where  nzp_wp = " + pref["nzp_wp"] + " and nzp_serv is not null and max_diff is null ";
                    ExecSQL(_connDb, sql, true);

                    #endregion
                }
                //Проставляем расход
                sql = " update tmp_counters set rashod = val_file - val_cnt " +
                      " where val_cnt is not null";
                ExecSQL(_connDb, sql, true);

                #endregion
                _loadProtokol.SetProcent(curProgress + step * 2 / 4, (int)StatusWWB.InProcess);

                #region Выбираем проблемные счетчики

                string commentIsPUValLoaded = " загружено предыдущее показание";//: " данные не загружены.");

                sql = " select * from tmp_counters  " +
                      " where (rashod>max_diff) or (" + sNvlWord + "(nzp_counter,0) = 0) " +
                      " or rashod<0";
                var badCounterTable = DBManager.ExecSQLToTable(_connDb, sql);
                foreach (DataRow dr in badCounterTable.Rows)
                {
                    if (dr["nzp_counter"].ToString() == "0")
                        _loadProtokol.AddUnValidValsForCounter(
                            "",
                            "",
                            dr["val_file"].ToString().Trim(),
                            dr["pkod"].ToString().Trim(),
                            dr["pref"].ToString().Trim(),
                            dr["point"].ToString().Trim(),
                            "Счетчик не зарегистрирован в системе");
                    else if (dr["rashod"] != DBNull.Value)
                    {
                        if (Convert.ToDecimal(dr["rashod"]) < 0)
                            _loadProtokol.AddUnValidValsForCounter(
                          dr["nzp_counter"].ToString().Trim(),
                          dr["numCnt"].ToString().Trim(),
                          dr["val_file"].ToString().Trim(),
                          dr["pkod"].ToString().Trim(),
                          dr["pref"].ToString().Trim(),
                          dr["point"].ToString().Trim(),
                          "Переход через 0," + commentIsPUValLoaded);
                        else
                            _loadProtokol.AddUnValidValsForCounter(
                         dr["nzp_counter"].ToString().Trim(),
                         dr["numCnt"].ToString().Trim(),
                         dr["val_file"].ToString().Trim(),
                         dr["pkod"].ToString().Trim(),
                         dr["pref"].ToString().Trim(),
                         dr["point"].ToString().Trim(),
                         "Слишком большое показание для счетчика," + commentIsPUValLoaded);
                    }
                }

                #endregion
                _loadProtokol.SetProcent(curProgress + step * 3 / 4, (int)StatusWWB.InProcess);

                #region Сохраняем хорошие счетчики

                foreach (DataRow pref in prefTable.Rows)
                {
                    //получаем рассчетный месяц локального банка
                    var localDate = Points.GetCalcMonth(new CalcMonthParams(pref["pref"].ToString().Trim()));
                    if (localDate.month_ == 0 || localDate.year_ == 0) localDate = Points.CalcMonth;

                    string datNextMonth = "'" + new DateTime(localDate.year_, localDate.month_, 1).AddMonths(1)
                        .ToShortDateString() + "'";

                    sql = " insert into  " + Points.Pref + "_fin_" + year + tableDelimiter + "pu_vals  " +
                          " (nzp_pack_ls, num_ls, nzp_counter, val_cnt, dat_month, cur_unl) " +
                          " select pl.nzp_pack_ls, f.nzp_kvar, nzp_counter, val_file, " + datNextMonth + ",  " +
                          _fileArgs.KvitID +
                          " from " + Points.Pref + "_fin_" + year + tableDelimiter + "pack_ls pl," +
                          " tmp_counters f " +
                          " where f.pkod = pl.pkod and pl.transaction_id= f.transaction_id " +
                          " and nzp_wp = " + pref["nzp_wp"] +
                          " and rashod < max_diff and nzp_counter is not null and rashod >= 0";
                    ret = ExecSQL(_connDb, sql, true);
                    if (!ret.result)
                    {
                        return ret;
                    }

                    //Для показаний вызывающих переход через ноль пишем предыдущие - теперь для всех регионов 
                    sql = " insert into  " + Points.Pref + "_fin_" + year + tableDelimiter + "pu_vals  " +
                          " (nzp_pack_ls, num_ls, nzp_counter, val_cnt, dat_month, cur_unl) " +
                          " select pl.nzp_pack_ls, f.nzp_kvar, nzp_counter, val_cnt, " + datNextMonth + ",  " +
                          _fileArgs.KvitID +
                          " from " + Points.Pref + "_fin_" + year + tableDelimiter + "pack_ls pl," +
                          " tmp_counters f " +
                          " where f.pkod = pl.pkod and pl.transaction_id= f.transaction_id " +
                          " and nzp_wp = " + pref["nzp_wp"] +
                          " and (rashod >= max_diff or rashod < 0) and nzp_counter is not null";
                    ret = ExecSQL(_connDb, sql, true);
                    if (!ret.result)
                    {
                        return ret;
                    }
                }

                #endregion

                _loadProtokol.SetProcent(curProgress + step * 4 / 4, (int)StatusWWB.InProcess);

            }
            catch (Exception ex)
            {
                _loadProtokol.AddComment("\n Ошибка при добавлении показания ПУ для ЛС ");
                MonitorLog.WriteLog("Ошибка при добавлении показания ПУ для  ЛС  " + ex,
                    MonitorLog.typelog.Error, true);
            }
            finally
            {
                ExecSQL(_connDb, "drop table tmp_counters", false);
            }

            return ret;
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
                " SELECT " + sNvlWord + "(max(p.val_prm " + sConvToNum + "), 10000) " +
                " FROM " + pref + sKernelAliasRest + "prm_name pn " +
                " LEFT JOIN " + pref + sDataAliasRest + "prm_10 p ON pn.nzp_prm = p.nzp_prm and p.is_actual = 1" +
                " WHERE pn.nzp_prm = " + param;
            object obj = ExecScalar(_connDb, null, sql, out ret, true);

            decimal result = obj == null || obj == DBNull.Value ? 10000m : Convert.ToDecimal(obj);
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
                " FROM " + Points.Pref + sKernelAliasRest + "s_point";
            var pref = ClassDBUtils.OpenSQL(sql, _connDb, null).resultData;
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
            if (maxDiffBetweenValues.ContainsKey(pref))
            {
                if (maxDiffBetweenValues[pref].ContainsKey(nzpServ))
                {
                    return maxDiffBetweenValues[pref][nzpServ];
                }


            }
            else if (maxDiffBetweenValues.ContainsKey(Points.Pref))
            {
                if (maxDiffBetweenValues[Points.Pref].ContainsKey(nzpServ))
                {
                    return maxDiffBetweenValues[Points.Pref][nzpServ];
                }
            }
            return 1000000;

        }



    }

}
