using System.Diagnostics;
using Globals.SOURCE.Container;
using Newtonsoft.Json;

namespace Bars.KP50.DB.Exchange.Unload
{
    using System;
    using System.Linq;
    using System.Text;
    using System.Collections.Generic;
    using System.Data;
    using System.IO;
    using STCLINE.KP50.DataBase;
    using STCLINE.KP50.Interfaces;
    using STCLINE.KP50.Global;
    using STCLINE.KP50;


    public abstract class BaseUnload20 : IUnload20
    {
        /// <summary>Уникальный</summary>
        public virtual int Code { get; set; }

        /// <summary>Название</summary>
        public virtual string Name { get; set;  }

        /// <summary>Название</summary>
        public virtual string NameText { get; set; }

        /// <summary>Список комментариев</summary>
        private List<string> CommentList = new List<string>();

        /// <summary>
        /// Код в таблице загрузки
        /// </summary>
        protected int NzpUnload;

        /// <summary>
        /// Год
        /// </summary>
        public int Year;

        protected int GetYear()
        {
            return Year;
        }

        public List<int> ListNzpArea;


        public virtual List<FieldsUnload> Data { get; set; }
        /// <summary>
        /// Месяц
        /// </summary>
        public int Month;

        public int GetMonth()
        {
            return Month;
        }

        /// <summary>
        /// Префикс </summary>
        public string Pref;

        /// <summary>
        /// Код пользователя
        /// </summary>
        public int NzpUser;

        public int GetNzpUser()
        {
            return NzpUser;
        }

        /// <summary>
        /// Код в таблице Мои Файлы
        /// </summary>
        public int NzpExcelUtility;

        /// <summary>Имя файла загрузки</summary>
        public string FileName { get; set; }

        /// <summary>
        /// Тип параметров (домовой, квартирный)
        /// </summary>
        public enum STypeParam
        {
            Dom = 1,
            Kvar = 2
        }

        private struct Param
        {
            public string fileKodPrm;
            public int ourNzpPrm;
            public STypeParam type;
        }


        private static List<Param> PrmList;

        protected static List<string> GetPrmNum02(int nzp_prm)
        {
            FillPrmList();

            List<string> list = 
                PrmList.AsEnumerable()
                    .Where(x => x.ourNzpPrm == nzp_prm)
                    .Select(x => x.fileKodPrm)
                    .ToList();
            return list;
        }

        private static void FillPrmList()
        {
            if (PrmList == null)
            {
                PrmList = new List<Param>();

                PrmList.Add(new Param { fileKodPrm = "001", ourNzpPrm = 37, type = STypeParam.Dom });
                PrmList.Add(new Param { fileKodPrm = "002", ourNzpPrm = 37, type = STypeParam.Dom });
                PrmList.Add(new Param { fileKodPrm = "003", ourNzpPrm = 40, type = STypeParam.Dom });
                PrmList.Add(new Param { fileKodPrm = "004", ourNzpPrm = 2030, type = STypeParam.Dom });
                PrmList.Add(new Param { fileKodPrm = "005", ourNzpPrm = 631, type = STypeParam.Dom });
                //PrmList.Add(new Param { fileKodPrm = "006", ourNzpPrm = 23, type = STypeParam.Dom });
                PrmList.Add(new Param { fileKodPrm = "007", ourNzpPrm = 1176, type = STypeParam.Dom });
                //PrmList.Add(new Param { fileKodPrm = "008", ourNzpPrm = 23, type = STypeParam.Dom });
                PrmList.Add(new Param { fileKodPrm = "009", ourNzpPrm = 466, type = STypeParam.Dom });
                //PrmList.Add(new Param { fileKodPrm = "010", ourNzpPrm = 23, type = STypeParam.Dom });
                //PrmList.Add(new Param { fileKodPrm = "011", ourNzpPrm = 23, type = STypeParam.Dom });
                PrmList.Add(new Param { fileKodPrm = "012", ourNzpPrm = 2049, type = STypeParam.Dom });
                PrmList.Add(new Param { fileKodPrm = "013", ourNzpPrm = 377, type = STypeParam.Dom });
                PrmList.Add(new Param { fileKodPrm = "014", ourNzpPrm = 2001, type = STypeParam.Dom });
                //PrmList.Add(new Param { fileKodPrm = "015", ourNzpPrm = 23, type = STypeParam.Dom });
                PrmList.Add(new Param { fileKodPrm = "016", ourNzpPrm = 1180, type = STypeParam.Dom });

                
                PrmList.Add(new Param { fileKodPrm = "017", ourNzpPrm = 111, type = STypeParam.Kvar });
                //PrmList.Add(new Param { fileKodPrm = "018", ourNzpPrm = 5, type = STypeParam.Kvar });
                //PrmList.Add(new Param { fileKodPrm = "019", ourNzpPrm = 131, type = STypeParam.Kvar });
                //PrmList.Add(new Param { fileKodPrm = "020", ourNzpPrm = 10, type = STypeParam.Kvar });
                PrmList.Add(new Param { fileKodPrm = "021", ourNzpPrm = 107, type = STypeParam.Kvar });
                PrmList.Add(new Param { fileKodPrm = "022", ourNzpPrm = 4, type = STypeParam.Kvar });
                PrmList.Add(new Param { fileKodPrm = "023", ourNzpPrm = 3, type = STypeParam.Kvar });
                PrmList.Add(new Param { fileKodPrm = "024", ourNzpPrm = 35, type = STypeParam.Kvar });
                PrmList.Add(new Param { fileKodPrm = "025", ourNzpPrm = 327, type = STypeParam.Kvar });
                PrmList.Add(new Param { fileKodPrm = "026", ourNzpPrm = 59, type = STypeParam.Kvar });
                //PrmList.Add(new Param { fileKodPrm = "027", ourNzpPrm = , type = STypeParam.Kvar });
                PrmList.Add(new Param { fileKodPrm = "028", ourNzpPrm = 133, type = STypeParam.Kvar });
                //PrmList.Add(new Param { fileKodPrm = "029", ourNzpPrm = 5, type = STypeParam.Kvar });
                PrmList.Add(new Param { fileKodPrm = "030", ourNzpPrm = 2, type = STypeParam.Kvar });
            }
        }

        protected static string GetAllParamString(STypeParam typeParam)
        {
            FillPrmList();

            var result = string.Join(",",
                PrmList.AsEnumerable()
                  .Where(x => x.type == typeParam)
                  .Select(x => x.ourNzpPrm.ToString())
                  .ToArray()
                );
            return result;
        }

        /// <summary>Выполнить</summary>
        public abstract void Start();

        /// <summary>Выполнить</summary>
        /// <param name="pref">Префикс</param>
        public abstract void Start(string pref);

        /// <summary>Выполнить</summary>
        public abstract void StartSelect();

        public StreamWriter Writer { get; set; }

        protected virtual IDbConnection Connection { get; set; }

        /// <summary>Создать временные таблицы</summary>
        public abstract void CreateTempTable();

        /// <summary>Удалить временные таблицы</summary>
        public virtual void DropTempTable()
        {
            ExecSQL("DROP TABLE " + Name);
        }

        /// <summary>Записать комментарий в журнал выгрузки</summary>
        /// <param name="comment">Текст комментария</param>
        protected virtual void AddComment(string comment)
        {
            if(comment == String.Empty) return;
            CommentList.Add(comment);
        }

        /// <summary>
        /// Проверить колонку на наличие пустых записей
        /// при наличии пустых записей - записать в журнал выгрузки
        /// </summary>
        /// <param name="columnName">Название колонки, которая проверяется на наличие пустых записей</param>
        /// <param name="messageText">Текст сообщения который запишется в журнал загрузки</param>
        /// <param name="insertDefaultValue">Заполнить колонки значением по умолчанию - да/нет</param>
        /// <param name="columnDefaultValue">Значение по умолчанию которым надо заменить пустые записи</param>
        protected void CheckColumnOnEmptiness(string columnName, string messageText, bool insertDefaultValue = false, string columnDefaultValue = "")
        {
            string sql =
                " SELECT COUNT(*) as count " +
                " FROM " + Name +
                " WHERE " + columnName + " IS NULL ";
            int emptyRecordsCnt = Convert.ToInt32(ExecSQLToTable(sql).Rows[0]["count"]);
            if (emptyRecordsCnt > 0)
            {
                string message = String.Format("Имеются {0} в кол-ве: {1}", messageText, emptyRecordsCnt);
                AddComment(message);
                if (insertDefaultValue && columnDefaultValue != String.Empty)
                {
                    sql =
                        " UPDATE " + Name +
                        " SET  " + columnName + " = " + columnDefaultValue +
                        " WHERE  " + columnName + " IS NULL ";
                    ExecSQL(sql);
                }
            }
        }

        /// <summary>
        /// Записать в журнал выгрузки данные о записи, 
        /// которая не прошла по формату и была отброшена
        /// </summary>
        /// <param name="sql">Запрос, который вытаскивает отброшенные записи</param>
        /// <param name="fieldName">Название (тег) поля в секции</param>
        /// <param name="permittedLength">Разрешенная длина поля</param>
        protected void WriteAboutIncorrectNotes(string sql, string fieldName, int permittedLength)
        {
            try
            {
                string message = String.Empty;

                DataTable dt = ExecSQLToTable(sql);
                foreach (DataRow row in dt.Rows)
                {
                    message =
                        String.Format(
                            " Длина поля '{0}' в секции '{1}' превышает заданный формат! Разрешенная длина поля: {2}, ключ поля: '{3}'. Данная запись не войдет в выгрузку! ",
                            fieldName, NameText.Trim(), permittedLength, row[0].ToString().Trim());
                    AddComment(message);
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteException("Ошибка выполнения процедуры WriteAboutIncorrectNotes: " + ex.Message);
            }
        }


        /// <summary>Открыть соединение</summary>
        /// <returns>Открытое соединение</returns>
        protected virtual IDbConnection OpenConnection()
        {
            if (Connection == null)
            {
                Connection = DBManager.GetConnection(Constants.cons_Webdata);
                var result = DBManager.OpenDb(Connection, true);
                if (!result.result)
                {
                    throw new Exception(result.text);
                }
            }

            return Connection;
        }

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
                throw new Exception("Не удалось закрыть соединение", exc);
            }
        }

        /// <summary>Закрыть IDataReader</summary>
        /// <param name="reader">Экземпляр IDataReader</param>
        protected virtual void CloseReader(ref IDataReader reader)
        {
            if (reader != null)
            {
                if (!reader.IsClosed)
                {
                    reader.Close();
                }

                reader.Dispose();
                reader = null;
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
                throw new Exception(result.text);
            }
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
                throw new Exception(result.text);
            }
        }

        /// <summary>Проверка на существование таблицы</summary>
        /// <param name="sql">Sql запрос</param>
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
                throw new Exception(result.text);
            }
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
                throw new Exception(result.text);
            }
        }

        /// <summary>
        /// Сохранение записи о реестре в Базу данных
        /// </summary>
        private Returns InsertReestr()
        {
            var ret = Utils.InitReturns();
            try
            {
                
                var myFile = new DBMyFiles();
                ret = myFile.AddFile(new ExcelUtility
                {
                    nzp_user = NzpUser,
                    status = ExcelUtility.Statuses.InProcess,
                    rep_name = " Выгрузка в ЦХД ",
                    is_shared = 1
                });
                if (!ret.result) return ret;

                NzpExcelUtility = ret.tag;

                //FileName = Constants.Directories.ReportDir + Year + "_" + Month + "_" + NzpExcelUtility + ".txt";

                InsertInUnloadDw();

                //myFile.SetFilePath(new ExcelUtility()
                //{
                //    nzp_exc = NzpExcelUtility,
                //    exc_path = System.IO.Path.GetFileName(FileName)
                //});
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка InsertReestr(): " + ex, MonitorLog.typelog.Error, true);
                return new Returns(false, "Ошибка записи в реестр", -1);
            }
            return ret;
        }

        private void InsertInUnloadDw()
        {
            string sql =
                " INSERT INTO " + DBManager.sDefaultSchema +
                "unloaddw ( " +
                //" 	nzp_exc, " +
                " 	month_, " +
                " 	year_, " +
                " 	status, " +
                " 	pref, " +
                //" 	file_name, " +
                "   progress, " +
                " 	date_start, " +
                " 	created_on, " +
                " 	created_by " +
                " ) " +
                " VALUES " +
                " ( " +
                //" " + NzpExcelUtility + " , " +
                Month + " , " +
                Year + " , " +
                ExcelUtility.Statuses.InProcess.GetHashCode() + " , " +
                " '" + Pref + "' , " +
                //" '" + System.IO.Path.GetFileName(FileName) + "'," +
                "0, " +
                Utils.EStrNull(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")) + "," +
                Utils.EStrNull(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")) + "," +
                NzpUser + " )";
            ExecSQL(sql);

            NzpUnload = GetSerialValue();

            sql =
                " UPDATE " + DBManager.sDefaultSchema + "unloaddw SET nzp_wp = " +
                "(" +
                "  SELECT nzp_wp " +
                "  FROM " + Points.Pref + DBManager.sKernelAliasRest + "s_point " +
                "  WHERE TRIM(LOWER(bd_kernel))=" + DBManager.sDefaultSchema + "unloaddw.pref" +
                ")" +
                " WHERE nzp_unload = " + NzpUnload;
            ExecSQL(sql);
        }

        public Returns GetUnload(int nzpUser, string year, string month, string pref, List<int> listNzpArea)
        {
            OpenConnection();

            var ret = Utils.InitReturns();

            ret = StartUnload(nzpUser, year, month, pref, listNzpArea);
            if (!ret.result)
            {
                AddComment(ret.text);
            }

            //формирование протокола выгрузки
            GetProtokol();

            CloseConnection();

            return ret;
        }

        public Returns StartUnload(int nzpUser, string year, string month, string pref, List<int> listNzpArea)
        {
            var ret = Utils.InitReturns();
            
            try
            {
                if (!CheckInputParams(nzpUser, year, month, pref, listNzpArea))
                {
                    СhangeProcessStatus(0, ExcelUtility.Statuses.Failed);
                    return new Returns(false, "Не корректные входные параметры", -1);
                }

                ret = InsertReestr();
                if (!ret.result)
                {
                    СhangeProcessStatus(0, ExcelUtility.Statuses.Failed);
                    return ret;
                }

                FileName = Constants.Directories.ReportDir + year + "_" + month + "_" + NzpExcelUtility + ".txt";

                if (FileName == null)
                {
                    СhangeProcessStatus(0, ExcelUtility.Statuses.Failed);
                    CloseConnection();
                    MonitorLog.WriteLog("StartUnload int nzpUser, string year, string month, string pref, List<int> listNzpArea): " +
                                        "Ошибка.\n: Не определено имя файла\n", MonitorLog.typelog.Error, true);
                    return new Returns(false, "Ошибка при определении имени файла");
                }

                ret = UnloadSections();
                if (!ret.result)
                {
                    СhangeProcessStatus(0, ExcelUtility.Statuses.Failed);
                    return ret;
                }

                //#region Архивация
                //try
                //{
                //    var fileNames = new List<string>();
                //    fileNames.Add(FileName);
                //    string fileName = FileName.Replace("txt", "zip");

                //    if (File.Exists(FileName))
                //        Archive.GetInstance(ArchiveFormat.Zip).Compress(fileName, fileNames.ToArray(), true);

                //    string ext_path = Year + "_" + Month + "_" + NzpExcelUtility + ".zip";

                //    string sql = " UPDATE " + DBManager.sDefaultSchema + "excel_utility " +
                //      " SET exc_path = '" + ext_path +
                //      "' WHERE nzp_exc = " + NzpExcelUtility;

                //    ExecSQL(sql);

                //    sql = " UPDATE " + DBManager.sDefaultSchema + "unloaddw " +
                //      " SET file_name = '" + ext_path +
                //      "' WHERE nzp_exc = " + NzpExcelUtility + " and nzp_unload = " + NzpUnload;

                //    ExecSQL(sql);

                //    FileName = fileName;
                //}
                //catch (Exception ex)
                //{
                //    MonitorLog.WriteLog("Ошибка архивирования файла " + ex.Message, MonitorLog.typelog.Error, 20, 201, true);
                //}
                //#endregion

                if (InputOutput.useFtp)
                {
                    FileName = InputOutput.SaveOutputFile(FileName);
                }

                СhangeProcessStatus(1, ExcelUtility.Statuses.Success);
                
            }
            catch (Exception ex)
            {
                СhangeProcessStatus(0, ExcelUtility.Statuses.Failed);

                
                MonitorLog.WriteLog("StartUnload int nzpUser, string year, string month, string pref, List<int> listNzpArea): Ошибка.\n: (" + 
                nzpUser + ", " + year + ", "
                    + month + ", " + pref + ", " + listNzpArea + ")\n" + ex, MonitorLog.typelog.Error, true);

                return new Returns(false, "При выгрузке файла возникли ошибки, смотрите подробности в логах");
            }

            return new Returns(true);
        }

        /// <summary>Получить ключ вставленной записи</summary>
        protected virtual int GetSerialValue()
        {
            return DBManager.GetSerialValue(Connection);
        }


        private void СhangeProcessStatus(decimal progress, ExcelUtility.Statuses status)
        {
            try
            {
                SetProcessProgress(progress);

                var myFile = new DBMyFiles();
                myFile.SetFileStatus(NzpExcelUtility, status);

                string sql =
                    " UPDATE " + DBManager.sDefaultSchema + "unloaddw " +
                    " SET status = " + status.GetHashCode();
                if (status == ExcelUtility.Statuses.Success || status == ExcelUtility.Statuses.Failed)
                {
                    sql += ", date_finish = " + Utils.EStrNull(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                }
                sql += " WHERE nzp_unload = " + NzpUnload;
                ExecSQL(sql);

                if (status == ExcelUtility.Statuses.Success || status == ExcelUtility.Statuses.Failed)
                {
                    sql = " UPDATE " + DBManager.sDefaultSchema + "excel_utility " +
                          " SET dat_out= " + Utils.EStrNull(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")) +
                          " WHERE nzp_exc = " + NzpExcelUtility;
                    ExecSQL(sql);
                    if (status == ExcelUtility.Statuses.Success)
                    {
                        myFile.SetFilePath(new ExcelUtility()
                        {
                            nzp_exc = NzpExcelUtility,
                            exc_path = System.IO.Path.GetFileName(FileName)
                        });
                        sql =
                            " UPDATE " + DBManager.sDefaultSchema + "unloaddw " +
                            " SET file_name = '" + System.IO.Path.GetFileName(FileName) + "', nzp_exc = " +
                            NzpExcelUtility + " where nzp_unload = " + NzpUnload;
                        ExecSQL(sql);
                    }
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка СhangeProcessStatus(decimal progress, ExcelUtility.Statuses status)\n" + ex, MonitorLog.typelog.Error, true);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="progress"></param>
        public void SetProcessProgress(decimal progress)
        {
            var myFile = new DBMyFiles();
            myFile.SetFileProgress(NzpExcelUtility, progress);

            string sql =
                " UPDATE " + DBManager.sDefaultSchema + "unloaddw " +
                " SET progress = " + progress +
                " WHERE nzp_unload = " + NzpUnload;
            ExecSQL(sql);

        }

        /// <summary>Конвертация кода единицы измерения</summary>
        public int ConvertCode(int code)
        {
            int conv_code;
            switch (code)
            {
                case 1:
                    conv_code = 2;
                    break;
                case 2:
                    conv_code = 11;
                    break;
                case 3:
                    conv_code = 3;
                    break;
                case 4:
                    conv_code = 4;
                    break;
                case 5:
                    conv_code = 1;
                    break;
                case 6:
                    conv_code = 6;
                    break;
                case 7:
                    conv_code = 6;
                    break;
                case 8:
                    conv_code = 7;
                    break;
                case 9:
                    conv_code = 3;
                    break;
                case 10:
                    conv_code = 8;
                    break;
                case 11:
                    conv_code = 9;
                    break;
                default :
                    conv_code = code;
                    break;
            }
            return conv_code;
        }


        public string GetNzpArea(List<int> listNzpArea)
        {
            string NzpArea = "";
            if (listNzpArea == null || listNzpArea.Count == 0)
                NzpArea = String.Empty;
            else
            {
                for (int i = 0; i < listNzpArea.Count; i++)
                {
                    if (i == 0) NzpArea += listNzpArea[i];
                    else NzpArea += ", " + listNzpArea[i];
                }
            }

            return NzpArea;
        }

        /// <summary>
        /// Проверка входных параметров
        /// </summary>
        /// <param name="nzpUser"> Код пользователя </param>
        /// <param name="year"> Год </param>
        /// <param name="month"> Месяц </param>
        /// <param name="pref"> Префикс локального банка данных</param>
        /// <param name="listNzpArea"> Список УК </param>
        /// <returns></returns>
        private bool CheckInputParams(int nzpUser, string year, string month, string pref, List<int> listNzpArea)
        {
            try
            {
                if (nzpUser < 1)
                {
                    AddComment("Не определен пользователь.");
                    return false;
                }
                NzpUser = nzpUser;
                if (!Int32.TryParse(year, out Year))
                {
                    AddComment("Не определен год выгрузки.");
                    return false;
                }
                if (!Int32.TryParse(month, out Month))
                {
                    AddComment("Не определен месяц выгрузки.");
                    return false;
                }
                if (String.IsNullOrEmpty(pref))
                {
                    AddComment("Не определен банк данных для выгрузки.");
                    return false;
                }
                else
                {
                    Pref = pref;
                }
                ListNzpArea = listNzpArea;
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка:" + ex, MonitorLog.typelog.Error, true);
                return false;
            }
           
            return true;
        }

        private Returns UnloadSections()
        {
            var ret = Utils.InitReturns();
            try
            {
                Writer = new StreamWriter(FileName, true, Encoding.GetEncoding(1251));

                var creator = new CreatorSections();

                var sectionsList = creator.FactoryMethod();

                var sections = new SectionsUnload();

                foreach (var section in sectionsList)
                {
                    section.Year = Year;
                    section.Month = Month;
                    section.ListNzpArea = ListNzpArea;
                    section.NzpUser = NzpUser;
                    section.Start(Pref);

                    CommentList.AddRange(section.CommentList);

                    if (section.Data == null)
                    {
                        AddComment("Данные секции " + section.NameText + " не выгружены");
                        continue;
                    }

                    var sectionUnload = new SectionUnload();
                    sectionUnload.TS = section.Code;
                    sectionUnload.N = section.Name;
                    sectionUnload.NT = section.NameText;
                    sectionUnload.D = section.Data;
                    
                    sections.Sections.Add(sectionUnload);
                    
                }

                Writer.WriteLine(sections.ToString());
                Writer.Flush();
                Writer.Close();
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выгрузки:" + ex, MonitorLog.typelog.Error, true);
                return new Returns(false, "Ошибка выгрузки данных", -1);
            }
            return ret;
        }

        private void GetProtokol()
        {
            var ret = Utils.InitReturns();
            try
            {
                string protokolName = "protokol_unload_" + DateTime.Now.ToString("yyyyMMdd_hhmmss") + ".txt";
                if (String.IsNullOrEmpty(protokolName))
                {
                    throw new Exception("Ошибка при формированиии имени файла протокола");
                }
                Writer = new StreamWriter(Path.Combine(Constants.Directories.ReportDir, protokolName));
                var myFile = new DBMyFiles();
                ret = myFile.AddFile(new ExcelUtility
                {
                    nzp_user = NzpUser,
                    status = ExcelUtility.Statuses.InProcess,
                    rep_name = "Протокол выгрузки в ЦХД",
                    is_shared = 1
                });
                if (!ret.result) return;

                NzpExcelUtility = ret.tag;

                var Months = new[] {"","Январь","Февраль",
                 "Март","Апрель","Май","Июнь","Июль","Август","Сентябрь",
                 "Октябрь","Ноябрь","Декабрь"};

                Writer.WriteLine(" Протокол выгрузки данных в ЦХД за " + Months[Month] + " месяц " + Year + " года");
                string point = GetBankName();
                Writer.WriteLine(" Банк данных - " + (point == String.Empty ? "Не определен" : point));
                Writer.WriteLine(" Имя файла - " + (String.IsNullOrEmpty(FileName) ? "Не сформирован" : Path.GetFileName(FileName)));
                Writer.WriteLine(" Статус - " + (CommentList.Count > 0 ? "Выгружено с ошибками" : "Успешно"));
                Writer.WriteLine(" Дата выгрузки - " + DateTime.Now);
                Writer.WriteLine();
                if (CommentList.Count > 0)
                {
                    Writer.WriteLine("Сообщения:");
                    foreach (var comment in CommentList)
                    {
                        Writer.WriteLine(comment);
                    }
                }

                Writer.Flush();
                Writer.Close();
                myFile = new DBMyFiles();
                myFile.SetFileProgress(NzpExcelUtility, 1);
                myFile.SetFilePath(new ExcelUtility
                {
                    nzp_exc = NzpExcelUtility,
                    exc_path = protokolName
                });
                myFile.SetFileState(new ExcelUtility
                {
                    nzp_exc = NzpExcelUtility, 
                    status = ExcelUtility.Statuses.Success
                });

                if (InputOutput.useFtp)
                {
                    protokolName =
                        InputOutput.SaveOutputFile(Path.Combine(Constants.Directories.ReportDir, protokolName));
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка формирования протокола выгрузки: " + ex, MonitorLog.typelog.Error, true);
            }
        }

        /// <summary>
        /// Получить название банка данных
        /// </summary>
        /// <returns></returns>
        private string GetBankName()
        {
            string result = String.Empty;
            try
            {
                string sql = " SELECT point FROM " + Points.Pref + DBManager.sKernelAliasRest + "s_point " +
                             " WHERE trim(bd_kernel) = '" + Pref + "'";
                IDataReader reader;
                ExecRead(out reader, sql);
                while (reader.Read())
                {
                    result = (reader["point"] == DBNull.Value ? String.Empty : reader["point"].ToString());
                }
                return result;
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка при определении банка данных" + ex, MonitorLog.typelog.Error, true);
                return String.Empty;
            }
        }
    }



    internal class CreatorSections
    {
        public List<BaseUnload20> FactoryMethod()
        {
            return new List<BaseUnload20>
            { 
                new RegisterSupplData(), 
                new RegisterLegalFace(), 
                new HouseRegister(), 
                new PlaceRegister(),
                new IndividualRegister(),
                new AccountRegister(),
                new ContractRegister(),
                new AccountInformRegister(),
                new AccountReCalcInformRegister(),
                new AccountPaymentReCalcRegister(),
                new IndicatorRegister(),
                new EvidenceIndicatorRegister(),
                new ResidentRegister(),
                new AccountPaymentRegister(),
                new PaymentDistributionRegister(),
                new BackorderRegister(),
                new CheckAccountRegister(),
                new TenantTimeRegister(),
                new AgreementContractRegister(),
                new AccountUnitRegister(),
                new HouseInformUnitRegister(),
                new ChangeBalanceRegister(),
                new GroupRegister()
            };
        }
    }
}
