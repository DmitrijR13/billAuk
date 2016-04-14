using System.Runtime.Remoting.Contexts;

namespace Bars.KP50.Report.Handlers
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Web;

    using Bars.KP50.Queue;
    using Bars.KP50.Report.Base;
    using Bars.KP50.Utils;
    using Bars.QueueCore;

    using Globals.SOURCE.Config;
    using Globals.SOURCE.Container;
    using Globals.SOURCE.INTF.Report;

    using Newtonsoft.Json;

    using STCLINE.KP50.Client;
    using STCLINE.KP50.CliKart.SOURCE.REPORT;
    using STCLINE.KP50.DataBase;
    using STCLINE.KP50.Global;
    using STCLINE.KP50.Interfaces;

    public class ReportHandler : JsonRequestHandler
    {
        public override void ProcessRequest(HttpContext context)
        {
            try
            {
                var operation = context.Request.QueryString["operationId"];
                string jsonResult;
                if (string.IsNullOrEmpty(operation))
                {
                    SetFailure(context, "Не известная операция");
                    return;
                }

                if (operation.ToUpper() == "GET_REPORTS")
                {
                    // Получаем список отчетов
                    jsonResult = GetJson(GetReports(context));
                }
                else if (operation.ToUpper() == "GET_REPORT_PARAMS")
                {
                    // Получаем параметры отчета
                    jsonResult = GetJson(GetReportParams(context));
                }
                else if (operation.ToUpper() == "PRINT_REPORT")
                {
                    object result;
                    if (!PrintReport(context, out result))
                    {
                        SetFailure(context, result.ToStr());
                        return;
                    }

                    jsonResult = GetJson(new {success = true, data = result});
                }
                else if (operation.ToUpper() == "LIST_FILES")
                {
                    int totalCount;
                    var data = GetListFile(context, out totalCount);
                    jsonResult = GetJson(new {success = true, data, totalCount});
                }
                else
                {
                    SetFailure(context, "Не известная операция");
                    return;
                }

                SetResponse(context, jsonResult);
            }
            catch (Exception exc)
            {
                MonitorLog.WriteException("Ошибка процедуры ProcessRequest ", exc);
            }
        }

        /// <summary>Получить список файлов</summary>
        /// <param name="context">Контекст запроса</param>
        /// <param name="totalCount">Кол-во записей</param>
        /// <returns>Список файлов</returns>
        public object GetListFile(HttpContext context, out int totalCount)
        {
            totalCount = 0;
            var result = new List<object>();
            var finder = new ExcelUtility
            {
                nzp_user = GetNzpUser(context.User.Identity.Name),
                skip = context.Request["start"].ToInt(),
                rows = context.Request["limit"].ToInt()
            };

            Returns ret;
            var list = cli_ExcelRep.GetListReport(finder, out ret);
            if (ret.result && list != null)
            {
                totalCount = ret.tag;
                foreach (var record in list)
                {
                    result.Add(new
                    {
                        record.nzp_exc,
                        record.rep_name,
                        record.dat_out,
                        record.status_name,
                        record.exec_comment
                    });
                }
            }

            return result;
        }

        /// <summary>Получить список отчетов</summary>
        /// <param name="context">Контекст запроса</param>
        /// <returns>Список отчетов</returns>
        protected object GetReports(HttpContext context)
        {
            ReportKind reportKind = GetReportKind(context);
            //for (var i = 0; i < context.Request.Params.Count; i++)
            //{
            //    if (context.Request.Params.GetKey(i) == "k")
            //    {
            //        reportKind = Convert.ToInt32(context.Request.Params.Get(i));
            //    }
            //}



            var result = new List<object>();
            int nzp_user = GetNzpUser(context.User.Identity.Name);

            if (reportKind != ReportKind.LC && reportKind != ReportKind.Person)
                GetGroupReports("Начисления", ReportGroup.Reports, reportKind, result, nzp_user);
            if (reportKind != ReportKind.LC && reportKind != ReportKind.Person)
                GetGroupReports("Финансы", ReportGroup.Finans, reportKind, result, nzp_user);
            if (reportKind == ReportKind.LC) GetGroupReports("Справки", ReportGroup.Cards, reportKind, result, nzp_user);
            if (reportKind == ReportKind.Person) GetGroupReports("Справки", ReportGroup.Cards, reportKind, result, nzp_user);

            return result;
        }

        /// <summary>
        /// Получить тип запускаемого отчета
        /// определяется по номеру в адресной строке
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        private ReportKind GetReportKind(HttpContext context)
        {
            if (context.Request.UrlReferrer != null)
            {
                try
                {
                    string s =
                        context.Request.UrlReferrer.Query.Substring(
                            context.Request.UrlReferrer.Query.IndexOf("&k=", StringComparison.Ordinal) + 3, 2);

                    return (ReportKind) Convert.ToInt32(s);
                }
                catch (Exception)
                {

                }
            }
            return ReportKind.Base;
        }


        /// <summary>
        /// Отчёт
        /// </summary>
        private class TreeReport
        {
            /// <summary>Код</summary>
            public string code { get; set; }
            /// <summary>Наименование</summary>
            public string text { get; set; }
            /// <summary>Описание</summary>
            public string description { get; set; }
            /// <summary>Листик?</summary>
            public bool leaf { get { return true; } }
        }

        /// <summary>
        /// Группа отчётов
        /// </summary>
        private class TreeReportsGroup
        {
            /// <summary>Код</summary>
            public string code { get { return String.Empty; } }
            /// <summary>Наименование</summary>
            public string text { get; set; }
            /// <summary>Развернута?</summary>
            public bool expanded { get { return true; } }
            /// <summary>Листик?</summary>
            public bool leaf { get { return false; } }
            /// <summary>Список отчётов в группе</summary>
            public List<TreeReport> children { get; set; }
        }



        private static void GetGroupReports(string groupName, ReportGroup reportGroup, ReportKind reportKind,
            List<object> result, int nzp_user)
        {

            TreeReportsGroup group = new TreeReportsGroup() { text = groupName };

            List<TreeReport> reports = new List<TreeReport>();

            // Жесткие отчеты (из dll сборок)            
            IocContainer.Current.Resolve<IReportProvider>()
           .GetReports()
           .Where(x => (x.ReportKinds.Contains(reportKind)) & (x.ReportGroups.FirstOrDefault() == reportGroup) & (x.Code != "Bars.Report.Soft.Processor"))
           .GroupBy(x => 1)
           .FirstOrDefault()
           .ForEach
           (
                y => reports.Add
                (
                    new TreeReport()
                    {
                        code = y.Code,
                        text = y.Name,
                        description = y.Description,
                    }
                )
            );

            // Гибкие отчеты (из таблицы report.list)
            GetSoftReports(reports, groupName, nzp_user);

            // Заполнить список отчетов, отсортировав по алфавиту
            group.children = reports.OrderBy(z => ((TreeReport)z).text).ToList();


            result.Add(group);

            //result.Add(IocContainer.Current.Resolve<IReportProvider>()
            //    .GetReports()
            //    .Where(x => (x.ReportKinds.Contains(reportKind)) & (x.ReportGroups.FirstOrDefault() == reportGroup))
            //    .GroupBy(x => 1)
            //    .Select(x => new
            //    {
            //        code = string.Empty,
            //        text = groupName,
            //        expanded = true,
            //        leaf = false,
            //        children = x.Select(y => new
            //        {
            //            code = y.Code,
            //            text = y.Name,
            //            y.Description,
            //            leaf = true
            //        }).OrderBy(z => z.text)
            //            .ToArray()
            //    }).FirstOrDefault());

        }


        /// <summary>
        /// Получить список "гибких" отчетов
        /// </summary>
        /// <param name="result">Результат. Этот объект будет дополняться новыми отчётами</param>
        /// <param name="groupName">Наименование группы</param>
        /// <param name="nzp_user">Код пользователя</param>
        private static void GetSoftReports(List<TreeReport> result, string groupName, int nzp_user)
        {
            MyDataReader reader;

            IDbConnection connection = null;
            try
            {
                connection = DBManager.GetConnection(Constants.cons_Webdata);
                var ret = DBManager.OpenDb(connection, true);
                DBManager.ExecRead(connection, out reader, String.Format("select * from report.get_list('{0}', {1})", groupName, nzp_user), true);
                while (reader.Read())
                {
                    result.Add
                    (
                        new TreeReport()
                        {
                            code = "Bars.Report.Soft." + reader["id"].ToString(),
                            text = reader["title"].ToString(),
                            description = reader["description"].ToString()
                        }
                    );
                }
            }
            finally
            {
                connection.Close();
            }
        }



        /// <summary>Получить параметры отчета</summary>
        /// <param name="context">Контекст запроса</param>
        /// <returns>Список параметров отчета</returns>
        protected object GetReportParams(HttpContext context)
        {
            var reportId = context.Request["reportId"];

            if (reportId.StartsWith("Bars.Report.Soft."))
            {
                // Отчёт. 
                // Идентификатор.
                string report_id = reportId.Replace("Bars.Report.Soft.", ""); // Убрать префикс. Останется только то, что после "Bars.Report.Soft.". Это и будет идентификатором отчёта.
                // Наименование.
                string reportName = String.Empty;
                //  Описание.
                string reportDescription = String.Empty;

                // Пользователь. Идентификатор.
                int nzp_user = GetNzpUser(context.User.Identity.Name);

                // Код объекта (лицевого счёта / дома / ...)
                string nzp_object = "0";
                if (context.Request["id"] != null)
                {
                    nzp_object = context.Request["id"].ToString();
                }

                // Параметры 
                List<UserParam> rParameters = new List<UserParam>();

                IDbConnection connection = null;
                try
                {
                    connection = DBManager.GetConnection(Constants.cons_Webdata);
                    Returns ret = DBManager.OpenDb(connection, true);

                    // Наименование и описание отчета
                    MyDataReader readerInfo;
                    DBManager.ExecRead(connection, out readerInfo, String.Format("select * from report.get_info({0}, {1})", report_id, nzp_user), true);
                    readerInfo.Read();
                    reportName = readerInfo["title"].ToString();
                    reportDescription = readerInfo["description"].ToString();
                    readerInfo.Close();

                    // Параметры            
                    MyDataReader readerParameters;
                    DBManager.ExecRead(connection, out readerParameters, String.Format("select * from report.get_parameters({0}, {1}, {2})", report_id, nzp_user, nzp_object), true);
                    while (readerParameters.Read())
                    {
                        string kind = readerParameters["kind"].ToString(); // Тип параметра. Соответсвует классу из сборки Bars.KP50.Report
                        string json = readerParameters["json"].ToString(); // Свойства, в формате json
                        Type type = Type.GetType(String.Format("Bars.KP50.Report.{0}, Bars.KP50.Report", kind));
                        Bars.KP50.Report.UserParam parameter = (Bars.KP50.Report.UserParam)JsonConvert.DeserializeObject(json, type, new JsonParameterConverter());
                        rParameters.Add(parameter);
                    }
                    readerParameters.Close();
                }
                finally
                {
                    connection.Close();
                }

                return new
                {
                    Id = reportId,
                    Name = reportName,
                    Description = reportDescription,
                    Filters = new List<UserParam>(),
                    Params = rParameters
                };
            }
            else
            {
                var report = IocContainer.Current.Resolve<IReportProvider>().GetReport(reportId);
                return report == null
                    ? null
                    : new
                    {
                        Id = report.Code,
                        report.Name,
                        report.Description,
                        Filters = report.ListUserFilters,
                        Params = report.ListUserParams
                    };
            }

        }


        /// <summary>
        /// Специальный конвертер json параметров отчета (Bars.KP50.Report.UserParam).
        /// Используется только для десериализациии, сериализация производится "стандартным" способом.
        /// </summary>
        /// <remarks>
        /// Создан исключительно ради свойства StoreData объектов типа ComboBoxParameter, т.к. это свойство типа IEnumerable. А для IEnumerable не предусмотрен стандартный десериализацатор.
        /// </remarks>
        class JsonParameterConverter : Newtonsoft.Json.JsonConverter
        {
            /// <summary>
            /// Должна ли совершаться конвертация?
            /// </summary>            
            public override bool CanConvert(Type objectType)
            {
                // Специальный конвертер срабатывает только для свойств типа IEnumerable, остальные обрабатываются стандартным способом
                if (objectType == typeof(System.Collections.IEnumerable))
                {
                    // Применяется специальный конвертер
                    return true;
                }
                else
                {
                    // Стандартный способ
                    return false;
                }
            }

            /// <summary>
            /// Десериализировать
            /// </summary>                        
            public override object ReadJson(Newtonsoft.Json.JsonReader reader, Type objectType, object existingValue, Newtonsoft.Json.JsonSerializer serializer)
            {
                // Предполагается, что сюда приходят только объекты типа IEnumerable (т.е. objectType == IEnumerable).
                // Если же будут приходить объекты других типов, то потребуется switch(objectType).

                // Пришёл массив значений
                if (reader.TokenType == JsonToken.StartArray)
                {
                    List<object> elements = new List<object>();

                    // Читать до конца массива
                    while (reader.Read() && reader.TokenType != JsonToken.EndArray)
                    {
                        Newtonsoft.Json.Linq.JObject obj = serializer.Deserialize<Newtonsoft.Json.Linq.JToken>(reader) as Newtonsoft.Json.Linq.JObject;
                        elements.Add(obj);
                    }

                    return elements;
                }
                // Одно значение (хотя это не имеет практического смысла)
                else
                {
                    return reader.Value;
                }
            }

            /// <summary>
            /// Сериализировать
            /// </summary>     
            /// <remarks>
            /// Не реализовано, т.к. данный класс используется только для десериализации.
            /// Метод присутствует т.к. он абстрактрый и должен присутствовать.
            /// </remarks>            
            public override void WriteJson(Newtonsoft.Json.JsonWriter writer, object value, Newtonsoft.Json.JsonSerializer serializer)
            {
                throw new NotImplementedException();
            }
        }





        /// <summary>Получить параметры отчета</summary>
        /// <param name="context">Контекст запроса</param>
        /// <param name="result">Результат выполнение. Объект если печать успешна, текст если произошла ошика</param>
        /// <returns>Успешно или нет</returns>
        protected bool PrintReport(HttpContext context, out object result)
        {
            var reportId = context.Request["reportId"];
            var userFilters = context.Request["userFilters"];
            var userParams = context.Request["userParams"];
            var userName = context.User.Identity.Name;
            var nzpObject = context.Request["id"].ToInt();
            var curReportKind = GetReportKind(context);

            if (String.IsNullOrEmpty(userName))
            {
                result = "Формирование отчетов времено не доступно. Превыше лимит времени бездействия пользователя. Выполните вход в программу заново";
                return false;
            }
            
            
            ReturnsObjectType<ReportResult> reportResult;
            if (IocContainer.Current.Resolve<IConfigProvider>().GetConfig().RemoteExecuteReport)
            {
                reportResult = AddJob(reportId, userParams, curReportKind, userName, nzpObject);
            }
            else
            {
                var report = new cli_BaseReport(0);
                if (reportId.StartsWith("Bars.Report.Soft."))
                {
                    string id = reportId.Replace("Bars.Report.Soft.", "");
                    reportId = "Bars.Report.Soft.Processor";
                    nzpObject = Convert.ToInt32(id);
                }
                reportResult = report.PrintReport(reportId, userParams, userFilters, (int)curReportKind, userName, nzpObject);
            }

            try
            {
                result = reportResult.GetData();
            }
            catch (Exception exc)
            {
                MonitorLog.WriteException("Ошибка функции PrintReport ", exc);
                result = "Формирование отчетов времено не доступно. Нет связи с сервером. Попробуйте позднее.";
                return false;
            }

            return reportResult.result;
        }
        
        private ReturnsObjectType<ReportResult> AddJob(string reportId, string userParams,ReportKind curReportKind, string userName, int nzpObject)
        {
            var result = new ReturnsObjectType<ReportResult>() { result = true };
            var report = IocContainer.Current.Resolve<IBaseReport>(reportId);

            IDbConnection connection = null;
            IJobProvider jobProvider = null;
            try
            {
                if (report != null)
                {
                    var userParamValues = JsonConvert.DeserializeObject<IList<UserParamValue>>(userParams);

                    var exportFormat = userParamValues.Any(x => x.Code == "ExportFormat")
                        ? userParamValues.First(x => x.Code == "ExportFormat").GetValue<ExportFormat>()
                        : ExportFormat.Excel2007;

                    connection = DBManager.GetConnection(Constants.cons_Webdata);
                    var ret = DBManager.OpenDb(connection, true);
                    if (!ret.result)
                    {
                        // Пишем в лог
                        result.text = "Не удалось открыть соединение";
                        result.result = false;
                        return result;
                    }

                    // сохраняем информацию об печатоемом отчете + расширение отчета
                    var extension = GetExtentions(report, exportFormat);
                    var nzpUser = GetNzpUser(userName);

#warning Добавить сохранение текущего фильтра пользователя

                    var sql = new StringBuilder();
                    sql.Append(
                        "insert into " + DBManager.GetFullBaseName(connection) + DBManager.tableDelimiter
                        + "excel_utility (nzp_user, prms, dat_in, dat_start, rep_name, dat_today, file_extension) ");
                    sql.Append(
                        " values (" + nzpUser + ", " + Utils.EStrNull(userParams) + ", "
                        + Utils.EStrNull(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")) + ", "
                        + Utils.EStrNull(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")) + ", "
                        + Utils.EStrNull(report.Name) + ", " + Utils.EStrNull(DateTime.Now.ToShortDateString()) + ", "
                        + Utils.EStrNull(extension) + ")");

                    ret = DBManager.ExecSQL(connection, sql.ToString(), true);
                    if (!ret.result)
                    {
                        result.text = "Не удалось выполнить запрос";
                        result.result = false;
                        return result;
                    }

                    var nzpExcelUtility = DBManager.GetSerialValue(connection);

                    var fileName = string.Format("{0}.{1}", nzpExcelUtility, extension);
                    var savePath = Path.Combine(IocContainer.Current.Resolve<IConfigProvider>().GetConfig().Directories.ReportsAbsoluteDir, fileName);

                    var updateSql = "update " + DBManager.GetFullBaseName(connection) + DBManager.tableDelimiter
                                    + "excel_utility set exc_path ='" + fileName + "' " +
                                    " where nzp_exc = " + nzpExcelUtility;

                    ret = DBManager.ExecSQL(connection, updateSql, true);
                    if (!ret.result)
                    {
                        result.text = "Не удалось выполнить запрос";
                        result.result = false;
                        return result;
                    }

                    var queueName = report.IsPreview ? QueueNames.ReportHigh : QueueNames.ReportNormal;
                    jobProvider = IocContainer.Current.Resolve<IJobProvider>();
                    var jobArguments = new JobArguments { Code = report.Code, Name = report.Name };

                    jobArguments.Parameters.Add("UserParamValues", userParams);
                    jobArguments.Parameters.Add("SystemParams", GetSystemParams(nzpExcelUtility, curReportKind, nzpUser, userName, savePath, nzpObject));

                    jobProvider.AddJob(JobType.Report, jobArguments, queueName);

                    result.returnsData = new ReportResult { nzpExcelUtility = nzpExcelUtility };
                    updateSql = "update " + DBManager.GetFullBaseName(connection) + DBManager.tableDelimiter
                        + "excel_utility set stats = 1 where nzp_exc = " + nzpExcelUtility;

                    DBManager.ExecSQL(connection, updateSql, true);
                }
                else
                {
                    throw new ReportException("Не найден отчет");
                }
            }
            catch (Exception exc)
            {
                result.text = string.Format("Произошла ошибка при формировании отчета: \"{0}\"", exc.Message);
                result.result = false;
            }
            finally
            {
                if (jobProvider != null)
                {
                    IocContainer.Current.Release(jobProvider);
                }

                if (connection != null)
                {
                    connection.Close();
                }
            }

            return result;
        }

        private string GetExtentions(IBaseReport report, ExportFormat exportFormat)
        {
            if (report.IsPreview)
            {
                return "fpx";
            }

            string result = null;
            switch (exportFormat)
            {
                case ExportFormat.Excel2007:
                    result = "xlsx";
                    break;
                case ExportFormat.Gif:
                    result = "gif";
                    break;
                case ExportFormat.Html:
                    result = "html";
                    break;
                case ExportFormat.Jpg:
                    result = "jpg";
                    break;
                case ExportFormat.Pdf:
                    result = "pdf";
                    break;
                case ExportFormat.Png:
                    result = "png";
                    break;
                case ExportFormat.Txt:
                    result = "txt";
                    break;
                case ExportFormat.Csv:
                    result = "csv";
                    break;
                case ExportFormat.Dbf:
                    result = "dbf";
                    break;
            }

            return result;
        }

        private int GetNzpUser(string login)
        {
            var user = new User { login = login };
            var ret = new DbAdminClient().GetUserBy(ref user, DbAdminClient.UserBy.Login);
            return ret.result ? user.nzp_user : 0;
        }

        private string GetSystemParams(int nzpExcelUtility, ReportKind curReportKind, int nzpUser, string userName, string pathForSave, int nzpObject)
        {
            return JsonConvert.SerializeObject(new
            {
                NzpUser = nzpUser,
                NzpObject = nzpObject,
                NzpExcelUtility = nzpExcelUtility,
                UserLogin = userName,
                CalcMonth = new DateTime(Points.CalcMonth.year_, Points.CalcMonth.month_, 1),
                CurOperDay = Points.DateOper,
                PathForSave = pathForSave,
                CurReportKind = curReportKind
            });
        }
    }

    public class ReportNode
    {
        public string code { get; set; }

        public IList<ReportNode> children { get; set; }
    }
}