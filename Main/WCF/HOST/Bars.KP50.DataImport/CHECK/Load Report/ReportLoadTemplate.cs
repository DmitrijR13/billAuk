using System;
using System.Data;
using System.IO;
using FastReport;
using FastReport.Export.OoXML;
using STCLINE.KP50.Global;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Interfaces;

namespace Bars.KP50.DataImport.CHECK.Load_Report
{
    public abstract class ReportLoadTemplate : ReportTemplate
    {
        protected int NzpFile;

        /// <summary>Префикс центрального банка Upload</summary>
        protected string PrefUpload { get { return Points.Pref + DBManager.sUploadAliasRest; } }
        /// <summary>Префикс центрального банка Data</summary>
        protected string PrefData { get { return Points.Pref + DBManager.sDataAliasRest; } }
        /// <summary>Префикс центрального банка Kernel</summary>
        protected string PrefKernel { get { return Points.Pref + DBManager.sKernelAliasRest; } }
        /// <summary>Префикс банка загрузки + Data</summary>
        protected string LoadPrefData { get { return Bank.pref + DBManager.sDataAliasRest; } }
        /// <summary>Префикс банка загрузки + Kernel</summary>
        protected string LoadPrefKernel { get { return Bank.pref + DBManager.sKernelAliasRest; } }

        /// <summary>Месяц загрузки данных</summary>
        protected DateTime CalcMonth { get; set; }

        /// <summary>Наименование загруженного файла</summary>
        protected string FileName { get; set; }

        public void InitParameter(IDbConnection connection, int nzpFile, int nzpUser, DateTime calcDate, string loadFile, string pref)
        {
            conn_db = connection;
            NzpFile = nzpFile;
            Bank = new _Point{ pref = pref };
            CalcMonth = calcDate;
            Month = CalcMonth.Month;
            Year = CalcMonth.Year;
            FileName = loadFile;
            User = new User { nzp_user = nzpUser };
        }

        public string ToGenerateReport(out Returns ret) {
            ret = STCLINE.KP50.Global.Utils.InitReturns();
            try
            {
                DropTempTable();
                CreateTempTable();
                FillTempTable();

                DataSet dsReport = AddDataSource();

                var rep = new FastReport.Report();
                rep.Load(Directory.GetCurrentDirectory() + @"\Template\" + reportFrxSource);

                SetParamValues(rep);

                rep.RegisterData(dsReport);

// ReSharper disable once UnusedVariable
                var env = new EnvironmentSettings {ReportSettings = {ShowProgress = false}}; //?
                rep.Prepare();

                fullFileName = Constants.Directories.ReportDir + fileName + ".xlsx";
                if (!Directory.Exists(Constants.Directories.ReportDir)) Directory.CreateDirectory(Constants.Directories.ReportDir);
                int n = 0;
                while (File.Exists(fullFileName)) fullFileName = Path.GetFileNameWithoutExtension(fullFileName) + (++n) + ".xlsx";
                (new Excel2007Export()).Export(rep, fullFileName);
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка формирования отчета \"" + fullFileName + "\" " + ex.Message, MonitorLog.typelog.Error, 20, 201, true);
                ret.result = false;
                ret.text = "Ошибка формирования отчета";
                ret.tag = -1;
                fullFileName = string.Empty;
            }
            finally
            {
                DropTempTable();
            }

            return fullFileName;
        }

// ReSharper disable once InconsistentNaming
        /// <summary>Выставление ссылки на отчет</summary>
        /// <param name="nzp_exc"></param>
        protected override void SetNzpExc(int nzp_exc) { SetNzpExcReport(nzp_exc); }

        /// <summary>Сохранение сылки на таблицу файлов</summary>
        /// <param name="nzpExc">Идентификатор таблицы файлов</param>
        private void SetNzpExcReport(int nzpExc)
        {
            string sql = " UPDATE " + PrefUpload + "files_imported " +
                         " SET nzp_exc_rep_load = " + nzpExc +
                         " WHERE  nzp_file = " + NzpFile;
            ExecSQL(sql);
        }

        /// <summary>Выполнит SQL-запрос</summary>
        /// <param name="sql">SQL-запрос</param>
        /// <param name="inlog">Логировать ли</param>
        protected void ExecSQL(string sql, bool inlog = true)
        {
            Returns ret = DBManager.ExecSQL(conn_db, sql, inlog);
            if(!ret.result) throw new Exception(ret.sql_error);
        }

        /// <summary>Возвращает содержимо запроса в таблицу</summary>
        /// <param name="sql">SQL-запрос</param>
        /// <returns>Объект таблица</returns>
        protected DataTable ExecSQLToTable(string sql)
        {
            return DBManager.ExecSQLToTable(conn_db, sql);
        }

        /// <summary>Проверка наличия доступа к таблице </summary>
        /// <param name="table">Наименование таблицы</param>
        /// <returns>Существуется таблица ли</returns>
        protected Boolean TempTableInWebCashe(string table)
        {
            return DBManager.TempTableInWebCashe(conn_db, table);
        }
    }
}
