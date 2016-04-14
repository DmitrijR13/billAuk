using System.Linq;
using FastReport;
using FastReport.Export.Dbf;

namespace Bars.KP50.Report.Base
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Data;
    using System.IO;

    using Utils;
    using QueueCore;

    using Castle.Windsor;

    using FastReport.Export;
    using FastReport.Export.Csv;
    using FastReport.Export.Html;
    using FastReport.Export.Image;
    using FastReport.Export.OoXML;
    using FastReport.Export.Pdf;
    using FastReport.Export.Text;

    using Newtonsoft.Json;

    using STCLINE.KP50;
    using Globals.SOURCE.Container;
    using STCLINE.KP50.Interfaces;
    using STCLINE.KP50.DataBase;
    using STCLINE.KP50.Global;
    using Globals.SOURCE.Utility;
    using STCLINE.KP50.Utility;

    /// <summary>
    /// Базовый отчета
    /// Реализация должна быть зарегестрирована в классе ReportsInstaller 
    /// </summary>
    public abstract class BaseFastReport : IBaseReport, IJob
    {
        public IWindsorContainer Container { get; set; }

        /// <summary>Уникальный идентификатор отчета</summary>
        public virtual string Code
        {
            get { return GetType().FullName; }
        }

        /// <summary>Название отчета</summary>
        public abstract string Name { get; }

        /// <summary>Описание отчета</summary>
        public abstract string Description { get; }

        /// <summary>Группы отчетов</summary>
        public abstract IList<ReportGroup> ReportGroups { get; }

        /// <summary>Вид отчета</summary>
        public abstract IList<ReportKind> ReportKinds
        {
            get; // { return ReportKind.Base; }
        }

        /// <summary>Выполняется предпросмотр</summary>
        public abstract bool IsPreview { get; }

        /// <summary>Парметры отчета</summary>
        public virtual ReportParams ReportParams { get; set; }

        /// <summary>Состояние работы</summary>
        protected JobState JobState { get; set; }
        
        /// <summary>Шаблон отчета</summary>
        protected abstract byte[] Template { get; }

        /// <summary>Пользовательские параметры</summary>
        protected Dictionary<string, UserParamValue> UserFilterValues { get; set; }

        /// <summary>Пользовательские параметры</summary>
        protected Dictionary<string, UserParamValue> UserParamValues { get; set; }

        /// <summary>Системные параметры</summary>
        protected Dictionary<string, string> SystemParams { get; set; }

        /// <summary>Получить состояние отчета</summary>
        /// <returns>Состояние отчета</returns>
        public abstract JobState GetState();

        /// <summary>Выполнить</summary>
        /// <param name="container">IoC контейнер</param>
        /// <param name="jobArguments">Параметры выполнения</param>
        public abstract void Run(IWindsorContainer container, JobArguments jobArguments);

        /// <summary>Получить пользовательские параметры отчета</summary>
        /// <returns>Параметры отчета</returns>
        public abstract List<UserParam> GetUserParams();

        /// <summary>Получить фильтры отчета</summary>
        /// <returns>Параметры отчета</returns>
        public virtual List<UserParam> GetUserFilters()
        {
            return null;
        }

        /// <summary>Задать параметры для отчета</summary>
        /// <param name="reportParameters">Параметры отчета</param>
        public virtual void SetReportParameters(NameValueCollection reportParameters)
        {
            Container = Container ?? IocContainer.Current;
            ReportParams = ReportParams ?? new ReportParams(Container);
            var usfilter = reportParameters["UserFilterValues"];
            if (usfilter != null)
            {
                var userFilters = JsonConvert.DeserializeObject<IList<UserParamValue>>(reportParameters["UserFilterValues"]);
                if (userFilters != null)
                {
                    UserFilterValues = new Dictionary<string, UserParamValue>(userFilters.Count);
                    foreach (var userFilter in userFilters)
                    {
                        UserFilterValues.Add(userFilter.Code, userFilter);
                    }
                }
            }

            if (reportParameters["UserParamValues"] == null)
            {
                MonitorLog.WriteLog("Пустые параметры пользователя UserParamValues для отчетов ", MonitorLog.typelog.Error, true);
            }

            var userParams = JsonConvert.DeserializeObject<IList<UserParamValue>>(reportParameters["UserParamValues"]);
            UserParamValues = new Dictionary<string, UserParamValue>(userParams.Count);
            foreach (var userParam in userParams)
            {
                UserParamValues.Add(userParam.Code, userParam);
            }


            if (reportParameters["SystemParams"] == null)
            {
                MonitorLog.WriteLog("Пустые параметры пользователя SystemParams для отчетов ", MonitorLog.typelog.Error, true);
            }
            SystemParams = JsonConvert.DeserializeObject<Dictionary<string, string>>(reportParameters["SystemParams"]);

            ReportParams.ExportFormat = UserParamValues.ContainsKey("ExportFormat") ? UserParamValues["ExportFormat"].GetValue<ExportFormat>() : ExportFormat.Excel2007;
            if (ReportKinds.Contains(ReportKind.House) ||
                ReportKinds.Contains(ReportKind.Person) ||
                ReportKinds.Contains(ReportKind.LC))
            {
                ReportParams.NzpObject = SystemParams.ContainsKey("NzpObject") ? SystemParams["NzpObject"].ToInt() : 0;
                if (ReportParams.NzpObject == 0)
                {
                    throw new ReportException("Не указан идентификатор записи");
                }
            }

            ReportParams.NzpExcelUtility = SystemParams.ContainsKey("NzpExcelUtility") ? SystemParams["NzpExcelUtility"].ToInt() : 0;
            if (ReportParams.NzpExcelUtility == 0)
            {
                throw new ReportException("Не указан идентификатор записи в таблице excel_utility");
            }

            ReportParams.PathForSave = SystemParams.ContainsKey("PathForSave") ? SystemParams["PathForSave"] : null;
            ReportParams.CalcMonth = SystemParams.ContainsKey("CalcMonth") ? DateTime.Parse(SystemParams["CalcMonth"]) : new DateTime();
            ReportParams.CurDateOper = SystemParams.ContainsKey("CurDateOper") ? DateTime.Parse(SystemParams["CurDateOper"]) : new DateTime();
            ReportParams.CurrentReportKind = SystemParams.ContainsKey("CurReportKind") ? (ReportKind)Int32.Parse(SystemParams["CurReportKind"]) : ReportKind.Base;


            int nzpUser = SystemParams.ContainsKey("NzpUser") ? Int32.Parse(SystemParams["NzpUser"]) : -1;

            var dbAdmin = new DbAdminClient();
            ReportParams.User = dbAdmin.GetUsers(new User {nzp_user = nzpUser, nzpuser = nzpUser}).GetData().FirstOrDefault() ??
                                new BaseUser();

            var db = new DbUserClient();
            ReportParams.User.RolesVal = new List<_RolesVal>();
            ReportParams.User.nzp_user = nzpUser;
            db.GetRolesVal(Constants.roleReport, nzpUser, ref ReportParams.User.RolesVal);
            db.Close();



            //var listRoles = new List<_RolesVal>();
            //DbAdminClient dbAdmin = new DbAdminClient();
            //dbAdmin.GetRolesKey(0, ReportParams.User.nzp_user, ref listRoles);

            //dbAdmin.GetUser()
            //ReportParams.User.RolesVal  = listRoles;
            
            
            

            if (string.IsNullOrEmpty(ReportParams.PathForSave))
            {
                throw new ReportException("Не указан путь сохранения отчета");
            }

            if (string.IsNullOrEmpty(ReportParams.ConnectionString))
            {
                throw new ReportException("Не указана строка соединения");
            }
        }

        /// <summary>Сформировать отчет</summary>
        /// <param name="reportParameters">Параметры отчета</param>
        public virtual void GenerateReport(NameValueCollection reportParameters)
        {
            var ds = GetData();
            Generate(ds);
        }

        
        /// <summary>Получить данные отчета</summary>
        /// <returns>Данные отчета в виде DataSet</returns>
        public abstract DataSet GetData();

        /// <summary>Сформировать отчет</summary>
        /// <param name="ds">Данные отчета в виде DataSet</param>
        public virtual void Generate(DataSet ds)
        {
            var report = new FastReport.Report();
            report.Load(GetTemplate());
            report.RegisterData(ds);
            PrepareReport(report);

            if (ReportParams.ExportFormat == ExportFormat.Dbf)
            {
                SaveReportDbf(ds);
            }
            else
            {
                SaveReport(report);    
            }
            
            SetProccessPercent(100M);
        }


        /// <summary>
        /// Сохранение отчета в DBF
        /// </summary>
        /// <param name="ds">Датасет передаваемый в отчет</param>
        protected void SaveReportDbf(DataSet ds)
        {
            var dbfExportReport = new DBFExportReport();
            var dbfList = dbfExportReport.SaveReportDbf(ds, ReportParams.PathForSave);

            //Архивация    
            ReportParams.PathForSave = Path.ChangeExtension(ReportParams.PathForSave, ".zip");
            Archive.GetInstance().Compress(ReportParams.PathForSave, dbfList, true);
            
            if (InputOutput.useFtp)
            {
                ReportParams.PathForSave = InputOutput.SaveOutputFile(ReportParams.PathForSave);
            }
        }

        /// <summary>Установить процент выполнения отчета</summary>
        /// <param name="percent">Процент выполнения</param>
        protected virtual void SetProccessPercent(decimal percent)
        {
        }

        /// <summary>Получить шаблон отчета</summary>
        /// <returns>MemoryStream</returns>
        protected virtual Stream GetTemplate()
        {
            // Если есть шаблон для подмены, берем его
            return new MemoryStream(Template);
        }

        /// <summary>Подготовить отчет, например, добавить параметры вызова отчета, произвести другие действия перед сохранением</summary>
        /// <param name="report">Отчет</param>
        protected virtual void PrepareReport(FastReport.Report report)
        {
        }

        /// <summary>Сохранить отчет</summary>
        /// <param name="report">Отчет</param>
        protected virtual void SaveReport(FastReport.Report report)
        {
            var exporter = GetExporter();
            exporter.ShowProgress = false;
            EnvironmentSettings env = new EnvironmentSettings();
            env.ReportSettings.ShowProgress = false;
            report.Prepare();
            if (IsPreview)
            {
                report.SavePrepared(ReportParams.PathForSave);
            }
            else
            {
                exporter.Export(report, ReportParams.PathForSave);
            }

            if (InputOutput.useFtp)
            {
                ReportParams.PathForSave = InputOutput.SaveOutputFile(ReportParams.PathForSave);
            }

           
        }

        /// <summary>Получить экпортер</summary>
        /// <returns>Экспортер</returns>
        protected virtual ExportBase GetExporter()
        {
            ExportBase exporter = null;
            
            switch (ReportParams.ExportFormat)
            {
                case ExportFormat.Excel2007:
                    exporter = new Excel2007Export();
                    break;
                case ExportFormat.Pdf:
                {
                    exporter = new PDFExport();
                    ((PDFExport) exporter).Compressed = false;
                }
                    break;
                case ExportFormat.Html:
                    exporter = new HTMLExport();
                    break;
                case ExportFormat.Gif:
                    exporter = new ImageExport { ImageFormat = ImageExportFormat.Gif };
                    break;
                case ExportFormat.Jpg:
                    exporter = new ImageExport { ImageFormat = ImageExportFormat.Jpeg };
                    break;
                case ExportFormat.Png:
                    exporter = new ImageExport { ImageFormat = ImageExportFormat.Png };
                    break;
                case ExportFormat.Txt:
                    exporter = new TextExport();
                    break;
                case ExportFormat.Csv:
                    exporter = new CSVExport();  
                    break;
                case ExportFormat.Dbf:
                    exporter = new DBFExport();
                    break; 
            }

            if (exporter == null)
            {
                throw new ReportException("Не известный формат отчета");
            }

            return exporter;
        }

        public string WebBase
        {
            get
            {
                int startIndex = Constants.cons_Webdata.IndexOf("Database=", StringComparison.Ordinal) + 9;
                int endIndex = Constants.cons_Webdata.Substring(startIndex, Constants.cons_Webdata.Length - startIndex).IndexOf(";", System.StringComparison.Ordinal);
                return Constants.cons_Webdata.Substring(startIndex, endIndex);
            }
        }
    }
}