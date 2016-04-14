using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data;
using System.IO;
using Bars.KP50.Report;
using Bars.QueueCore;
using Castle.Windsor;
using IBM.Data.Informix;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;
using STCLINE.KP50;
using Newtonsoft.Json;
using Npgsql;

namespace Bars.KP50.Load.Obninsk
{
    /// <summary>Базовый отчет, ориентированный на получение данных через sql запросы</summary>
    public abstract class BaseSqlLoad : BaseLoad
    {
        protected JobArguments JobArguments { get; set; }

      
        protected virtual IDbConnection Connection { get; set; }

        /// <summary>Выполнить</summary>
        /// <param name="container">IoC контейнер</param>
        /// <param name="jobArguments">Параметры выполнения</param>
        public override void Run(IWindsorContainer container, JobArguments jobArguments)
        {
            Container = container;

            ReportParams = ReportParams ?? new Report.Base.ReportParams(Container);

            JobArguments = jobArguments;
            SetProcessState();
            bool success;
            string message;
            try
            {
                GenerateLoad(jobArguments.Parameters);
                success = true;
                message = ProtocolFileName;
            }
            catch (ReportException exc)
            {
                MonitorLog.WriteException("Ошибка при загрузке файла! ", exc);
                if (exc.InnerException != null)
                {
                    MonitorLog.WriteException("Произошла ошибка при загрузке файла ", exc);
                }
                success = false;
                message = exc.Message;
            }
            catch (Exception exc)
            {
                MonitorLog.WriteException("Ошибка при загрузке файла! ", exc);
                if (exc.InnerException != null)
                {
                    MonitorLog.WriteException("Произошла ошибка при загрузке файла ", exc);
                }
                success = false;
                message = exc.Message;
            }
            SetEndState(success, message);
        }

        /// <summary>Получить состояние отчета</summary>
        /// <returns>Состояние отчета</returns>
        public override JobState GetState()
        {
            return JobState;
        }

        protected virtual NameValueCollection GetNameValueCollection(FilesImported finder)
        {
            DateTime? date_load = null;

            if (finder.date == null)
            {
                CalcMonthParams prm = new CalcMonthParams(Points.GetPref(finder.nzp_wp));
                RecordMonth rm = Points.GetCalcMonth(prm);
                date_load = new DateTime(rm.year_, rm.month_, 1);
            }
            else
            {
                date_load = finder.date;
            }

            return new NameValueCollection
            {
                {
                    "SystemParams", JsonConvert.SerializeObject( new
                    {
                        NzpUser = finder.nzp_user,
                        NzpExcelUtility = 2,
                        UserLogin = finder.webLogin,
                        PathForSave = finder.ex_path,
                        UserFileName = finder.saved_name,
                        nzpSupp = finder.nzp_supp,
                        SimpLdTypeFile = finder.SimpLdFileType,
                        nzp_wp = finder.nzp_wp,
                        file_type = finder.file_type,
                        DateLoad = date_load.Value.ToString("dd.MM.yyyy")
                    })
                },
                {
                    "UserParamValues", JsonConvert.SerializeObject(new
                    {
                        Test = "test"
                    })
                }
            };
        }
        /// <summary>
        /// Код в таблице загрузки
        /// </summary>
        protected int NzpLoad;

        /// <summary>
        /// Загрузка счетчиков в простом формате с расходами
        /// </summary>
        /// <returns></returns>
        public Returns StartLoad(FilesImported finder)
        {
            try
            {
                //DateTime? date_load = null;

                //if (finder.date == null)
                //{
                //    CalcMonthParams prm  = new CalcMonthParams(Points.GetPref(finder.nzp_wp));
                //    RecordMonth rm = Points.GetCalcMonth(prm);
                //    date_load = new DateTime(rm.year_, rm.month_, 1);
                //}
                //else
                //{
                //    date_load = finder.date;  
                //}
                GenerateLoad(GetNameValueCollection(finder));
            }
            catch 
            {
                return new Returns(false, "При загрузке файла возникли ошибки, смотрите подробности в логах");
            }
            return new Returns(true);
        }

        public void StartWithObject(object container)
        {
            
            StartLoad((FilesImported)container);
            
        }
        

       /// <summary>
       /// Загрузка файла 
       /// </summary>
       /// <param name="reportParameters"></param>
        public override void GenerateLoad(NameValueCollection reportParameters)
        {
            try
            {
                Protokol = new BaseLoadProtocol();
                SetLoadParameters(reportParameters);
                OpenConnection();
                InsertReestr();
                CreateTempTable();
                PrepareParams();
                LoadData();
                SetSourceOrg();
                ProtocolFileName = GetProtocolName();
                //if (InputOutput.useFtp)
                //{
                //    ReportParams.PathForSave = InputOutput.SaveOutputFile(ProtocolFileName);
                //}
            }
            catch (ReportException ex)
            {
                MonitorLog.WriteException("Произошла ошибка при загрузке файла", ex);
                if (ex.InnerException != null)
                {
                    MonitorLog.WriteException("Произошла ошибка при загрузке файла ", ex);
                }
                throw;
            }
            catch (Exception exc)
            {
                MonitorLog.WriteException("Произошла ошибка при загрузке файла ", exc);
                if (exc.InnerException != null)
                {
                    MonitorLog.WriteException("Произошла ошибка при загрузке файла ", exc);
                }
                throw new ReportException("Произошла ошибка при загрузке файла " + exc.Message, exc);
            }
            finally
            {
                DropTempTable();
                //удаляем промежуточный файл на хосте
                if (InputOutput.useFtp) File.Delete(TemporaryFileName);

                CloseConnection();
            }
        }

        /// <summary>
        /// Простановка организации и пользователя ответственного за выгрузку
        /// </summary>
        private void SetSourceOrg()
        {
            if (String.IsNullOrEmpty(SourceOrg) || String.IsNullOrEmpty(UserSourceOrg)) return;
            
            string sqlStr = " Update  " + Points.Pref + DBManager.sDataAliasRest + "simple_load " +
                            " set SourceOrg ='" + STCLINE.KP50.Global.Utils.EStrNull(SourceOrg) + "'," +
                            " UserSourceOrg ='" + STCLINE.KP50.Global.Utils.EStrNull(UserSourceOrg) + "'" +
                            " where nzp_load=" + NzpLoad;

            ExecSQL(sqlStr);
        }

        protected void SetProcessState()
        {
            try
            {
                JobState = JobState.Proccess;
                var connection = OpenConnection();
                var command = connection.CreateCommand();
#if PG
                command.CommandText =
                    "update public.jobs set job_state = :job_state, start_date = :start_date where id = :id";
                command.Parameters.Add(new NpgsqlParameter("job_state", (int) JobState));
                command.Parameters.Add(new NpgsqlParameter("start_date", DateTime.Now));
                command.Parameters.Add(new NpgsqlParameter("id", JobArguments.JobId));
#else
                command.CommandText = "update jobs set job_state = ?, start_date = ? where id = ?";
                command.Parameters.Add(new IfxParameter("job_state", (int)JobState));
                command.Parameters.Add(new IfxParameter("start_date", DateTime.Now));
                command.Parameters.Add(new IfxParameter("id", JobArguments.JobId));
#endif

                command.ExecuteNonQuery();
                CloseConnection();
            }
            catch (Exception ex)
            {
                MonitorLog.WriteException("Ошибка при записи информации об отчете в таблицу jobs! ", ex);
            }
        }

        protected void SetEndState(bool success, string message)
        {
            JobState = JobState.End;
            var connection = OpenConnection();
            var command = connection.CreateCommand();
#if PG
            command.CommandText = "update public.jobs set job_state = :job_state, end_date = :end_date, success = :success, message = :message where id = :id";
            command.Parameters.Add(new NpgsqlParameter("job_state", (int)JobState));
            command.Parameters.Add(new NpgsqlParameter("end_date", DateTime.Now));
            command.Parameters.Add(new NpgsqlParameter("success", success));
            command.Parameters.Add(new NpgsqlParameter("message", message));
            command.Parameters.Add(new NpgsqlParameter("id", JobArguments.JobId));
#else
            command.CommandText = "update jobs set job_state = ?, end_date = ?, success = ?, message = ? where id = ?";
            command.Parameters.Add(new IfxParameter("job_state", (int)JobState));
            command.Parameters.Add(new IfxParameter("end_date", DateTime.Now));
            command.Parameters.Add(new IfxParameter("success", success));
            command.Parameters.Add(new IfxParameter("message", message));
            command.Parameters.Add(new IfxParameter("id", JobArguments.JobId));
#endif

            command.ExecuteNonQuery();
            CloseConnection();
        }

        /// <summary>Создать временные таблицы</summary>
        protected virtual void CreateTempTable()
        {

        }

        /// <summary>Удалить временные таблицы</summary>
        protected virtual void DropTempTable()
        {
            
        }

        /// <summary>Открыть соединение</summary>
        /// <returns>Открытое соединение</returns>
        protected virtual IDbConnection OpenConnection()
        {
            if (Connection == null)
            {
                //Connection = DBManager.GetConnection(ReportParams.ConnectionString);
                Connection = DBManager.GetConnection(Constants.cons_Kernel);
                var result = DBManager.OpenDb(Connection, true);
                if (!result.result)
                {
                    throw new ReportException(result.text);
                }
            }

            return Connection;
        }

        /// <summary>Подготовить параметры отчета</summary>
        protected abstract void PrepareParams();

        /// <summary>Закрыть соединение с БД</summary>
        protected virtual void CloseConnection()
        {
            try
            {
                if (Connection != null)
                {
                    Connection.Close();
                    Connection = null;
                }
            }
            catch (Exception exc)
            {
                throw new ReportException("Не удалось закрыть соединение", exc);
            }
        }

        /// <summary>Получить пользователя</summary>
        /// <returns>Пользователь</returns>
        protected virtual User GetUser()
        {
            return null;
        }

        /// <summary>Получить список ролей пользователя</summary>
        /// <returns>Список ролей</returns>
        protected virtual List<_RolesVal> GetUserRoles()
        {
            return null;
        }

        /// <summary>Открыть соединение с БД</summary>
        /// <param name="inlog">Логировать да/нет</param>
        protected virtual void OpenDb(bool inlog = true)
        {
            var result = DBManager.OpenDb(Connection, inlog);
            if (!result.result)
            {
                throw new ReportException(result.text);
            }
        }

        /// <summary>Палучить ключ вставленной записи</summary>
        protected virtual int GetSerialValue()
        {
            return DBManager.GetSerialValue(Connection);
        }

        /// <summary>Выполнить запрос</summary>
        /// <param name="sql">Sql запрос</param>
        protected virtual void ExecSQL(string sql)
        {
            ExecSQL(sql, true, 6000);
        }

        /// <summary>Выполнить запрос</summary>
        /// <param name="sql">Sql запрос</param>
        /// <param name="inlog">Логировать да/нет</param>
        protected virtual void ExecSQL(string sql, bool inlog)
        {
            ExecSQL(sql, inlog, 6000);
        }

        /// <summary>Выполнить запрос</summary>
        /// <param name="sql">Sql запрос</param>
        /// <param name="inlog">Логировать да/нет</param>
        /// <param name="timeout">Таймаут</param>
        protected virtual void ExecSQL(string sql, bool inlog, int timeout)
        {
            var result = DBManager.ExecSQL(Connection, sql, inlog, timeout);
            if (!result.result)
            {
                throw new ReportException(result.text);
            }
        }

        /// <summary>Проверка на существование таблицы</summary>
        /// <param name="tableName">Имя таблицы</param>
        /// <returns>Таблица</returns>
        protected virtual bool TempTableInWebCashe(string tableName)
        {
            return DBManager.TempTableInWebCashe(Connection, tableName);
        }

        /// <summary>Проверка на существование колонки в таблице</summary>
        /// <param name="tableName">Название таблицы</param>
        /// <param name="columnName">Название колонки</param>
        /// <returns>Колонка</returns>
        protected virtual bool TempColumnInWebCashe(string tableName, string columnName)
        {
            return DBManager.TempColumnInWebCashe(Connection, tableName, columnName);
        }


        /// <summary>Получить результат sql запроса в виде таблицы</summary>
        /// <param name="sql">Sql запрос</param>
        /// <returns>Таблица</returns>
        protected virtual DataTable ExecSQLToTable(string sql)
        {
            return DBManager.ExecSQLToTable(Connection, sql);
        }
        /// <summary>Получить результат sql запроса в виде IDataReader</summary>
        /// <param name="reader">IDataReader</param>
        /// <param name="sql">Sql запрос</param>
        protected virtual void ExecRead(out IDataReader reader, string sql)
        {
            ExecRead(out reader, sql, true, 300);
        }


        /// <summary>Получить результат sql запроса в виде значения</summary>
        /// <param name="sql">Sql запрос</param>
        protected virtual object ExecScalar(string sql)
        {
            Returns ret;
            return DBManager.ExecScalar(Connection, sql, out ret, true);
        }

        /// <summary>Получить результат sql запроса в виде IDataReader</summary>
        /// <param name="reader">IDataReader</param>
        /// <param name="sql">Sql запрос</param>
        /// <param name="inlog">Логировать да/нет</param>
        protected virtual void ExecRead(out IDataReader reader, string sql, bool inlog)
        {
            ExecRead(out reader, sql, inlog, 300);
        }

        /// <summary>Получить результат sql запроса в виде IDataReader</summary>
        /// <param name="reader">IDataReader</param>
        /// <param name="sql">Sql запрос</param>
        /// <param name="inlog">Логировать да/нет</param>
        /// <param name="timeout">Таймаут</param>
        protected virtual void ExecRead(out IDataReader reader, string sql, bool inlog, int timeout) 
        {
            var result = DBManager.ExecRead(Connection, out reader, sql, inlog, timeout);
            if (!result.result)
            {
                throw new ReportException(result.text);
            }
        }

        /// <summary>Получить результат sql запроса в виде IDataReader</summary>
        /// <param name="sql">Sql запрос</param>
        /// <param name="ret">Результат разпроса (выполнен ли)</param>
        /// <param name="inlog">Логировать да/нет</param>
        protected virtual object ExecScalar( string sql, out Returns ret, bool inlog)
        {
            
            object obj = DBManager.ExecScalar(Connection,  sql, out ret, inlog);
            if (!ret.result)
            {
                throw new ReportException(ret.text);
            }
            return obj;
        }

        /// <summary>Получить результат sql запроса в виде IDataReader</summary>
        /// <param name="reader">IDataReader</param>
        /// <param name="sql">Sql запрос</param>
        /// <param name="inlog">Логировать да/нет (по умолчанию да)</param>
        protected virtual void ExecRead(out MyDataReader reader, string sql, bool inlog = true)
        {
            var result = DBManager.ExecRead(Connection, out reader, sql, inlog);
            if (!result.result)
            {
                throw new ReportException(result.text);
            }
        }

        public override void SetProcessPercent(double percent, ExcelUtility.Statuses status)
        {
            string sqlStr =" Update  " + Points.Pref + DBManager.sDataAliasRest + "simple_load " +
                           " set percent =" + percent +
                           " where nzp_load=" + NzpLoad;

            ExecSQL(sqlStr);

            sqlStr = " Update  " + DBManager.sDefaultSchema + "excel_utility " +
                     " set progress =" + percent + "/100," +
                     " stats = " + (int) status +
                     " where nzp_exc=" + NzpExcelUtility;
            ExecSQL(sqlStr);

        }
        public  void SetProcessPercentEvArgs(object sender, ProgressEventArgs e)
        {
            string sqlStr = " Update  " + Points.Pref + DBManager.sDataAliasRest + "simple_load " +
                           " set percent =" + e.Progress +
                           " where nzp_load=" + NzpLoad;

            ExecSQL(sqlStr);

            sqlStr = " Update  " + DBManager.sDefaultSchema + "excel_utility " +
                     " set progress =" + e.Progress + "," +
                     " stats = " + (int)ExcelUtility.Statuses.InProcess +
                     " where nzp_exc=" + NzpExcelUtility;
            ExecSQL(sqlStr);

        }

        /// <summary>
        /// Сохранение записи о реестре в Базу данных
        /// </summary>
        protected virtual void InsertReestr()
        {
            var myFile = new DBMyFiles();
            var ret = myFile.AddFile(new ExcelUtility
            {
                nzp_user = ReportParams.User.nzp_user,
                status = ExcelUtility.Statuses.InProcess,
                rep_name = Name,
                is_shared = 1
            });
            if (!ret.result) return;
            NzpExcelUtility = ret.tag;
            
            string sqlStr = "INSERT INTO " + Points.Pref + DBManager.sDataAliasRest + "simple_load " +
                            "(file_name, temp_file, nzp, nzp_wp, month_, year_, " + 
                            " created_by, created_on, tip, download_status ) " +
                            "VALUES " +
                            " ( '" +  FileName + "','" + TemporaryFileName + "'," +
                            NzpSupp + "," + NzpWp + "," + DateLoad.Month + "," + DateLoad.Year + "," +
                            ReportParams.User.nzp_user + ", " + DBManager.sCurDateTime + ", " + (int)SimpLoadTypeFile +", "+2+")";
            ExecSQL(sqlStr);
            NzpLoad = GetSerialValue();
        }
    }
}