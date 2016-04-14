using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using STCLINE.KP50.Global;
using STCLINE.KP50;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Interfaces;
using STCLINE.KP50.Utility;

namespace Bars.KP50.DataImport.CHECK.Load_Report
{
    /// <summary>Акт загрузки</summary>
    public class ActLoadOfReports
    {
        /// <summary>Список отчетов</summary>
        private List<ReportLoadTemplate> Reports { get; set; }
        /// <summary>Список путей к отчетам</summary>
        private List<String> FullPathOfTheFiles { get; set; }
        /// <summary>Подключатель к БД</summary>
        private IDbConnection ConnDb { get; set; }

        /// <summary>Идентификатор пользователя</summary>
        private readonly Int32 _nzpUser;
        /// <summary>Идентификатор файла</summary>
        private readonly Int32 _nzpFile;
        /// <summary>Месяц загрузки файла</summary>
        private readonly DateTime _calcData;
        /// <summary>Наименование загруженного файла</summary>
        private readonly string _loadFileName;
        /// <summary>Префикс банка, в который загрузили файл</summary>
        private readonly string _pref;


        /// <summary>Инициализация объекта Акта загрузки</summary>
        /// <param name="connection">Подключатель к БД</param>
        /// <param name="inputParams">Входные параметры</param>
        public ActLoadOfReports(IDbConnection connection, FilesImported inputParams) {
            Reports = new List<ReportLoadTemplate>();
            FullPathOfTheFiles = new List<string>();
            _nzpUser = inputParams.nzp_user;
            _nzpFile = inputParams.nzp_file;
            ConnDb = connection;

            string prefUpload = Points.Pref + DBManager.sUploadAliasRest,
                                prefKernel = Points.Pref + DBManager.sKernelAliasRest;

            string sql = " SELECT fh.calc_date AS calc_date, " +
                                " fi.pref AS pref, " +
                                " TRIM(fi.loaded_name) || '(' || fh.calc_date || ')' AS file_name " +
                         " FROM " + prefUpload + "files_imported fi INNER JOIN " + prefUpload + "file_head fh ON fh.nzp_file = fi.nzp_file " +
                                                                  " INNER JOIN " + prefKernel + "s_point p ON (TRIM(fi.pref) = bd_kernel " +
                                                                                                         " AND nzp_wp > 1) " +
                         " WHERE fi.nzp_file = " + _nzpFile;

            var dt = DBManager.ExecSQLToTable(ConnDb, sql);
            if (dt.Rows.Count < 1) throw new Exception("Файл " + _nzpFile + " не был загружен");

            _calcData = dt.Rows[0]["calc_date"] != DBNull.Value ? (DateTime) dt.Rows[0]["calc_date"] : new DateTime();
            _loadFileName = dt.Rows[0]["file_name"] != DBNull.Value ? dt.Rows[0]["file_name"].ToString() : string.Empty;
            _pref = dt.Rows[0]["pref"] != DBNull.Value ? dt.Rows[0]["pref"].ToString().Trim() : string.Empty;
        }

        public void AddReport(ReportLoadTemplate report) {
            if (report == null) return;
            report.InitParameter(ConnDb, _nzpFile, _nzpUser, _calcData, _loadFileName, _pref);
            Reports.Add(report);
        }

        /// <summary>Сформировать акт</summary>
        /// <returns>Результат работы функции</returns>
        public Returns ToGenerateAct() {
            Returns ret;
            int errorStatus = 0;
            try
            {
                string fullFileName = Constants.Directories.FilesDir + "act_" + DateTime.Now.Ticks + ".zip";
                while (File.Exists(fullFileName))
                    fullFileName = Constants.Directories.FilesDir + "act_" + DateTime.Now.Ticks + ".zip";

                #region Занес информацию в таблицу файлов

                var excelRep = new ExcelRepClient();
                ret = excelRep.AddMyFile(new ExcelUtility
                {
                    nzp_user = _nzpUser,
                    status = ExcelUtility.Statuses.InProcess,
                    rep_name = "Приложение к акту приема передачи данных",
                    is_shared = 1
                });
                if (!ret.result) return new Returns(false, "Ошибка постановки акта", -1);
                int nzpExc = ret.tag;

                #endregion

                foreach (var report in Reports)
                {
                    string fullPath = report.ToGenerateReport(out ret);
                    if (!ret.result) errorStatus = -10;
                    if (fullPath != string.Empty) FullPathOfTheFiles.Add(fullPath);
                }
                if (FullPathOfTheFiles.Count > 0)
                {
                    if (!Archive.GetInstance().Compress(fullFileName, FullPathOfTheFiles.ToArray(), true)) throw new Exception("Произошла ошибка при архивации");

                    if (InputOutput.useFtp) fullFileName = InputOutput.SaveOutputFile(fullFileName);

                    var myFile = new DBMyFiles();
                    myFile.SetFileState(new ExcelUtility
                    {
                        nzp_exc = nzpExc,
                        status = ExcelUtility.Statuses.Success,
                        exc_path = fullFileName
                    });

                    SetNzpExcReport(ConnDb, nzpExc);
                }
                else if (Reports.Count > 0) throw new Exception("Произошла ошибка при формирование отчетов");

            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                throw new Exception(ex.Message);
            }
            finally
            {
                if (errorStatus == -10)
                {
                    ret.tag = errorStatus;
                    ret.text = "Имеются несформированные отчеты";
                }
            }

            return ret;
        }

        /// <summary>Создать группу ЛС</summary>
        /// <returns>Результат работы функции</returns>
        public Returns InsertNzpKvarInGroup() {
            Returns ret = STCLINE.KP50.Global.Utils.InitReturns();
            if (ConnDb != null && _nzpFile != 0 && !string.IsNullOrEmpty(_pref))
            {
                string prefData = _pref + DBManager.sDataAliasRest,
                        prefUpload = Points.Pref + DBManager.sUploadAliasRest;
                string insertValue = "Акт загрузки с номером файла " + _nzpFile;

                string sql = " SELECT COUNT(nzp_group) FROM " + prefData + "s_group WHERE TRIM(ngroup) = '" + insertValue + "' ";
                DataTable dt = DBManager.ExecSQLToTable(ConnDb, sql);
                bool isGroup = ((dt != null && dt.Rows.Count > 0 && dt.Rows[0][0] != DBNull.Value)
                    ? Convert.ToInt32(dt.Rows[0][0])
                    : 0) > 0;

                if (isGroup)
                {
                    ret.text = " Для файла с номером " + _nzpFile + " уже существует группа ";
                    return ret;
                }

                sql = " INSERT INTO " + prefData + "s_group(ngroup) SELECT ngroup FROM (SELECT '" + insertValue + "' AS ngroup) t ";
                ret = DBManager.ExecSQL(ConnDb, sql, true);
                if (!ret.result) throw new Exception("InsertNzpKvarInGroup: " + ret.text);

                sql = " INSERT INTO " + prefData + "link_group(nzp_group, nzp) " +
                      " SELECT (SELECT MAX(nzp_group) FROM " + prefData + "s_group WHERE TRIM(ngroup) = '" + insertValue + "'), " +
                             " nzp_kvar " +
                      " FROM " + prefUpload + "file_kvar " +
                      " WHERE nzp_file = " + _nzpFile +
                      " AND nzp_kvar IS NOT NULL ";
                ret = DBManager.ExecSQL(ConnDb, sql, true);
                if (!ret.result) throw new Exception("InsertNzpKvarInGroup: " + ret.text);
            }
            else
            {
                ret.text = ConnDb == null ? " Нет соединения с БД " : string.Empty;
                ret.text = _nzpFile == 0 ? " Необределён идентификатор файла " : string.Empty;
                ret.text = string.IsNullOrEmpty(_pref) ? " Необределён банк данных " : string.Empty;
            }

            return ret;
        }

        /// <summary>Сохранение сылки на таблицу файлов</summary>
        /// <param name="conDB">Объект подключения</param>
        /// <param name="nzpExc">Идентификатор таблицы файлов</param>
        private void SetNzpExcReport(IDbConnection conDB, int nzpExc) {
            string sql = " UPDATE " + Points.Pref + DBManager.sUploadAliasRest + "files_imported " +
                         " SET nzp_exc_rep_load = " + nzpExc +
                         " WHERE  nzp_file = " + _nzpFile;
            DBManager.ExecSQL(conDB, sql, true);
        }
    }
}
