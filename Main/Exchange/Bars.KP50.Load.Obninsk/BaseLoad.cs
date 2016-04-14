using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using Bars.KP50.Report;
using Bars.KP50.Report.Base;
using Bars.KP50.Utils;
using Bars.QueueCore;
using Castle.Windsor;
using FastReport;
using FastReport.Export;
using FastReport.Export.Csv;
using FastReport.Export.Html;
using FastReport.Export.Image;
using FastReport.Export.OoXML;
using FastReport.Export.Pdf;
using FastReport.Export.Text;
using Globals.SOURCE.Container;
using Newtonsoft.Json;
using STCLINE.KP50;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;
using ReportParams = Bars.KP50.Report.Base.ReportParams;

namespace Bars.KP50.Load.Obninsk
{
    /// <summary>
    /// Базовый отчета
    /// Реализация должна быть зарегестрирована в классе ReportsInstaller 
    /// </summary>
    public abstract class BaseLoad : IBaseLoad, IJob
    {
        public IWindsorContainer Container { get; set; }

        /// <summary>Уникальный идентификатор загрузки</summary>
        public virtual string Code
        {
            get { return GetType().FullName; }
        }

        /// <summary>Название загрузки</summary>
        public abstract string Name { get; }

        /// <summary>Описание загрузки</summary>
        public abstract string Description { get; }

        /// <summary>Имя файла загрузки</summary>
        public string FileName { get; set; }

        /// <summary>Временное имя файла загрузки в локальном хранилище</summary>
        public string TemporaryFileName { get; set; }

        /// <summary>Имя файла протокола загрузки</summary>
        public string ProtocolFileName { get; set; }

        /// <summary>Организация - источник файла</summary>
        public string SourceOrg { get; set; }

        /// <summary>Ответственный за выгрузку</summary>
        public string UserSourceOrg { get; set; }
        
        /// <summary>  Договор по которому может идти загрузка </summary>
        public int NzpSupp;
        /// <summary>  Первичный ключ вставленной строки </summary>
        public int Nzp;
        /// <summary> Схема БД в которую будут загружать данные </summary>
        public string Pref;

        /// <summary> код банка данных </summary>
        public int NzpWp { get; set; }
        /// <summary> Тип файла </summary>
        public int FileType { get; set; }

        /// <summary> Фомат загружаемоого файла </summary>
        public int UploadFormat { get; set; }

        /// <summary>Парметры загрузки</summary>
        public virtual ReportParams ReportParams { get; set; }

        /// <summary> Протокол загрузки </summary>
        public IBaseLoadProtokol Protokol { get; set;}

        /// <summary>Состояние работы</summary>
        protected JobState JobState { get; set; }
        
        /// <summary>Шаблон отчета протокола загрузки</summary>
        protected abstract byte[] Template { get; }

        /// <summary>
        /// Код в таблице Мои Файлы
        /// </summary>
        protected int NzpExcelUtility;
               
        /// <summary>Пользовательские параметры</summary>
        protected Dictionary<string, UserParamValue> UserParamValues { get; set; }

        /// <summary>Системные параметры</summary>
        protected Dictionary<string, string> SystemParams { get; set; }

        /// <summary>Получить состояние загрузки</summary>
        /// <returns>Состояние загрузки</returns>
        public abstract JobState GetState();


        /// <summary>Дата на которую загружаются данные</summary>
        public DateTime DateLoad;
        /// <summary>Пользователь</summary>
        public int Nzp_user { get; set; }

        /// <summary>Выполнить</summary>
        /// <param name="container">IoC контейнер</param>
        /// <param name="jobArguments">Параметры выполнения</param>
        public abstract void Run(IWindsorContainer container, JobArguments jobArguments);

        /// <summary>Получить пользовательские параметры загрузки</summary>
        /// <returns>Параметры загрузки</returns>
        public abstract List<UserParam> GetUserParams();

        public SimpleLoadTypeFile SimpLoadTypeFile { get; set; }

        public SimpleLoadPayOrIpuType LoadPayOrIpuType { get; set; }

        /// <summary>Задать параметры для отчета</summary>
        /// <param name="reportParameters">Параметры загрузки</param>
        public virtual void SetLoadParameters(NameValueCollection reportParameters)
        {
            Container = Container ?? IocContainer.Current;
            ReportParams = ReportParams ?? new ReportParams(Container);
            SystemParams = JsonConvert.DeserializeObject<Dictionary<string, string>>(reportParameters["SystemParams"]);
            ReportParams.PathForSave = SystemParams.ContainsKey("PathForSave") ? SystemParams["PathForSave"] : null;


            TemporaryFileName = Constants.Directories.ImportAbsoluteDir + Path.GetFileName(ReportParams.PathForSave);
            if (InputOutput.useFtp)
                InputOutput.DownloadFile(ReportParams.PathForSave,TemporaryFileName, true);


            DateLoad = SystemParams.ContainsKey("DateLoad") 
                ? Convert.ToDateTime(SystemParams["DateLoad"]) 
                : new DateTime(Points.CalcMonth.year_, Points.CalcMonth.month_,1);

            Nzp_user = SystemParams.ContainsKey("NzpUser")
                ? Convert.ToInt32(SystemParams["NzpUser"] ?? "-1")
                : -1;
            

            NzpSupp = SystemParams.ContainsKey("nzpSupp")
                ? Convert.ToInt32(SystemParams["nzpSupp"] ?? "-1")
                : -1;

            NzpWp = SystemParams.ContainsKey("nzp_wp")
                ? Convert.ToInt32(SystemParams["nzp_wp"] ?? "-1")
                : -1;
            FileType = SystemParams.ContainsKey("file_type")
                ? Convert.ToInt32(SystemParams["file_type"] ?? "-1")
                : -1;
            SimpLoadTypeFile = SystemParams.ContainsKey("SimpLdTypeFile")
             ? (SimpleLoadTypeFile)(Convert.ToInt32(SystemParams["SimpLdTypeFile"] ?? "0"))
             : SimpleLoadTypeFile.None;
            LoadPayOrIpuType = SystemParams.ContainsKey("LoadPayOrIpuType")
                ? (SimpleLoadPayOrIpuType) (Convert.ToInt32(SystemParams["LoadPayOrIpuType"] ?? "1"))
                : SimpleLoadPayOrIpuType.Ipu;

            FileName = SystemParams.ContainsKey("UserFileName") ? SystemParams["UserFileName"] : String.Empty;

            UploadFormat = SystemParams.ContainsKey("uploadFormat")
                ? Convert.ToInt32(SystemParams["uploadFormat"] ?? "-1")
                : -1;
            int nzpUser = SystemParams.ContainsKey("NzpUser") ? Int32.Parse(SystemParams["NzpUser"]) : -1;

            var dbAdmin = new DbAdminClient();
            ReportParams.User = dbAdmin.GetUsers(new User {nzp_user = nzpUser, nzpuser = nzpUser}).GetData().FirstOrDefault() ??
                                new BaseUser();

            var db = new DbUserClient();
            ReportParams.User.RolesVal = new List<_RolesVal>();
            ReportParams.User.nzp_user = nzpUser;
            db.GetRolesVal(Constants.roleReport, nzpUser, ref ReportParams.User.RolesVal);
            db.Close();


            if (string.IsNullOrEmpty(ReportParams.PathForSave))
            {
                throw new ReportException("Не указан путь к файлу загрузки");
            }

            //if (string.IsNullOrEmpty(ReportParams.ConnectionString))
            if (string.IsNullOrEmpty(Constants.cons_Kernel))
            {
                throw new ReportException("Не указана строка соединения");
            }
        }

        /// <summary>Запустить загрузку</summary>
        /// <param name="reportParameters">Параметры загрузки</param>
        public virtual void GenerateLoad(NameValueCollection reportParameters)
        {
            Protokol = new BaseLoadProtocol();
            TemporaryFileName = LoadFileIntoTemp();
            LoadData();
            ProtocolFileName = GetProtocolName();
            if (InputOutput.useFtp)
            {
                ReportParams.PathForSave = InputOutput.SaveOutputFile(ProtocolFileName);
            }
        }

        /// <summary>
        /// Загрузка файла во временное хранилище
        /// </summary>
        /// <returns></returns>
        public virtual string LoadFileIntoTemp()
        {
            return "";
        }


        /// <summary>Получить данные загрузки</summary>
        public abstract void LoadData();

        /// <summary>Получить протокол загрузки</summary>
        /// <returns>Данные отчета в виде DataSet</returns>
        public abstract string GetProtocolName();

        /// <summary>Получить шаблон загрузки</summary>
        /// <returns>MemoryStream</returns>
        protected virtual Stream GetTemplate()
        {
            // Если есть шаблон для подмены, берем его
            return new MemoryStream(Template);
        }

        public abstract void SetProcessPercent(double percent, ExcelUtility.Statuses status);
    }
}