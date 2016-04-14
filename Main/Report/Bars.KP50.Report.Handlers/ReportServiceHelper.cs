namespace Bars.KP50.Report
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Data;
    using System.IO;
    using System.Linq;
    using System.Text;
    using Base;
    using Globals.SOURCE.Container;
    using Globals.SOURCE.INTF.Report;
    using Newtonsoft.Json;
    using STCLINE.KP50.DataBase;
    using STCLINE.KP50.Global;
    using STCLINE.KP50.Interfaces;
    
    public class ReportServiceHelper
    {
        public ReturnsObjectType<ReportResult> PrintReport(string reportId, string userParams, string userFilters, int curReportKind, string userName, int nzpObject)
        {
            var result = new ReturnsObjectType<ReportResult>() { result = true };
            var report = IocContainer.Current.Resolve<IBaseReport>(reportId);

            IDbConnection connection = null;
            try
            {
                if (report != null)
                {
                    var userParamValues = JsonConvert.DeserializeObject<IList<UserParamValue>>(userParams);

                    var exportFormat = userParamValues.Any(x => x.Code == "ExportFormat")
                                           ? userParamValues.First(x => x.Code == "ExportFormat")
                                                 .GetValue<ExportFormat>()
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
                    
                    var nzpUser = GetNzpUser(connection, userName);

                    // Инициализировать свойтсва "гибкого" отчёта
                    if (report is ISoftReport)
                    {
                        ((ISoftReport)report).InitSoftProperties(nzpObject, nzpUser, connection);
                    }

                    var extension = GetExtentions(report, exportFormat);





                    var sql = new StringBuilder();

                    string longUserParams;

                    if (userParams.Length > 200)
                        longUserParams = "'" + JsonConvert.SerializeObject(new List<UserParamValue>()) + "'";
                    else longUserParams = Utils.EStrNull(userParams);

                    sql.Append(
                        "insert into " + DBManager.GetFullBaseName(connection) + DBManager.tableDelimiter
                        + "excel_utility (nzp_user, prms, dat_in, dat_start, rep_name, dat_today, file_extension, file_name) ");
                    sql.Append(
                        " values (" + nzpUser + ", " + longUserParams + ", "
                        + Utils.EStrNull(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")) + ", "
                        + Utils.EStrNull(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")) + ", "
                        + Utils.EStrNull(report.Name) + ", " + Utils.EStrNull(DateTime.Now.ToShortDateString()) + ", "
                        + Utils.EStrNull(extension) + ", " + Utils.EStrNull((report.Name.Length >=95 ? report.Name.Substring(0,95) : report.Name) + "." + extension) + ")");
                    ret = DBManager.ExecSQL(connection, sql.ToString(), true);

                    //Если в excel_utility в одной из баз нет атрибута file_name
                    if (!ret.result)
                    {
                        sql.Remove(0, sql.Length);
                        sql.Append(
                            "insert into " + DBManager.GetFullBaseName(connection) + DBManager.tableDelimiter
                            + "excel_utility (nzp_user, prms, dat_in, dat_start, rep_name, dat_today, file_extension) ");
                        sql.Append(
                            " values (" + nzpUser + ", " + longUserParams + ", "
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
                    }


                    var nzpExcelUtility = DBManager.GetSerialValue(connection);

                    var fileName = string.Format("{0}.{1}", nzpExcelUtility, extension);
                    var savePath = Path.Combine(Constants.ExcelDir, fileName);

                    var updateSql = "update " + DBManager.GetFullBaseName(connection) + DBManager.tableDelimiter
                                    + "excel_utility set exc_path ='" + fileName + "' "+
                                    " where nzp_exc = "+ nzpExcelUtility;

                    ret = DBManager.ExecSQL(connection, updateSql, true);
                    if (!ret.result)
                    {
                        result.text = "Не удалось выполнить запрос";
                        result.result = false;
                        return result;
                    }

                    if (report.IsPreview)
                    {
                        /* Если просмотр отчета, то выполняем его сразу и ждем результат выполнения */

                        try
                        {
                            report.GenerateReport(
                            new NameValueCollection
                                {
                                    { "UserParamValues", userParams },
                                    { "UserFilterValues", userFilters },
                                    {
                                        "SystemParams",
                                        GetSystemParams(
                                            nzpExcelUtility,
                                            nzpUser,
                                            curReportKind,
                                            userName,
                                            savePath,
                                            nzpObject)
                                    }
                                });
                        }
                        catch
                        {                            
                            updateSql = "update " + DBManager.GetFullBaseName(connection) + DBManager.tableDelimiter
                                    + "excel_utility set stats = -1 where nzp_exc = " + nzpExcelUtility;

                            ret = DBManager.ExecSQL(connection, updateSql, true);
                            if (!ret.result)
                            {
                                result.text = "Не удалось выполнить запрос";
                                result.result = false;
                                return result;
                            }
                            
                            throw;
                        }

                        updateSql = "update " + DBManager.GetFullBaseName(connection) + DBManager.tableDelimiter
                                    + "excel_utility set stats = 2, dat_out = " +
                                    Utils.EStrNull(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")) + " where nzp_exc = " + nzpExcelUtility;

                        ret = DBManager.ExecSQL(connection, updateSql, true);
                        if (!ret.result)
                        {
                            result.text = "Не удалось выполнить запрос";
                            result.result = false;
                            return result;
                        }

                        result.returnsData = new ReportResult { nzpExcelUtility = nzpExcelUtility, isPreview = true };
                    }
                    else
                    {
                        FakeQueue.AddReport(
                            report,
                            new NameValueCollection
                            {
                                { "UserParamValues", userParams },
                                { "UserFilterValues", userFilters },
                                {
                                    "SystemParams",
                                    GetSystemParams(
                                        nzpExcelUtility,
                                        nzpUser,
                                        curReportKind,
                                        userName,
                                        savePath,
                                        nzpObject)
                                }
                            },
                            nzpExcelUtility);

                        result.returnsData = new ReportResult { nzpExcelUtility = nzpExcelUtility };
                        
                        updateSql = "update " + DBManager.GetFullBaseName(connection) + DBManager.tableDelimiter
                              + "excel_utility set stats = 1 where nzp_exc = " + nzpExcelUtility;

                        DBManager.ExecSQL(connection, updateSql, true);
                    }
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
                if (connection != null)
                {
                    connection.Close();
                }
            }

            return result;
        }

        protected string GetExtentions(IBaseReport report, ExportFormat exportFormat)
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
                    result = "zip";
                    break;
            }

            return result;
        }


        /// <summary>
        /// Получение кода web пользователя по его логину
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="userName"></param>
        /// <returns></returns>
        protected int GetNzpUser(IDbConnection connection, string userName)
        {

            string strSQLQuery = " Select max(nzp_user) as nzp_user " +
                                 " From " + DBManager.sDefaultSchema + "users u " +
                                 " where login=" + Utils.EStrNull(userName);
            Returns ret;
            object obj = DBManager.ExecScalar(connection, strSQLQuery, out ret, true);
            if (ret.result && obj != DBNull.Value)
            {
                return Convert.ToInt32(obj);
            }
            
            return -800;//Пользователь неопределен
        }

        protected List<_RolesVal> GetRoleUser(int nzpUser)
        {
            var listRoles = new List<_RolesVal>();
            DbAdminClient dbAdmin = new DbAdminClient();
            dbAdmin.GetRolesKey(0, nzpUser, ref listRoles);
            return listRoles;
        }

        protected string GetSystemParams(int nzpExcelUtility, int nzpUser, int curReportKind, string userName, string pathForSave, int nzpObject)
        {
            return JsonConvert.SerializeObject(new
            {
                NzpUser = nzpUser,
                NzpObject = nzpObject,
                NzpExcelUtility = nzpExcelUtility,
                UserLogin = userName,
                CalcMonth = new DateTime(Points.CalcMonth.year_, Points.CalcMonth.month_,1),
                CurOperDay = Points.DateOper,
                PathForSave = pathForSave,
                CurReportKind = curReportKind
            });
        }
    }
}