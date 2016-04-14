using System.Text;
using Bars.KP50.SzExchange.Interface;
using System;
using System.Collections.Generic;
using System.Data;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;

namespace Bars.KP50.SzExchange.UnloadForSZ
{

    public abstract class BaseUnloadClass: IUnlSz
    {
        /// <summary>
        /// Уникальный код
        /// </summary>
        public virtual int Code { get; set; }

        /// <summary>
        /// Наименование
        /// </summary>
        public virtual string Name { get; set; }

        /// <summary>
        /// Наименование 
        /// </summary>
        public virtual string NameText { get; set; }
        
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

        /// <summary>
        /// Выполнение запроса
        /// </summary>
        /// <param name="sql"></param>
        protected virtual void ExecSQL(string sql)
        {
            ExecSQL(sql, true, 6000);
        }

        /// <summary>Выполнение запроса</summary>
        /// <param name="sql">Sql запрос</param>
        /// <param name="inlog">Логировать да/нет</param>
        protected virtual void ExecSQL(string sql, bool inlog)
        {
            ExecSQL(sql, inlog, 6000);
        }

        /// <summary>Получение результата sql запроса в виде таблицы</summary>
        /// <param name="sql">Sql запрос</param>
        /// <returns>Таблица</returns>
        protected virtual DataTable ExecSQLToTable(string sql)
        {
            return DBManager.ExecSQLToTable(Connection, sql);
        }

        /// <summary>
        /// Создание временных таблиц
        /// </summary>
        public abstract void CreateTempTable();

        /// <summary>
        /// Удаление временных таблиц
        /// </summary>
        public virtual void DropTempTable()
        {
            ExecSQL("DROP TABLE " + Name);
        }

        /// <summary>
        /// Выполнить
        /// </summary>
        /// <param name="pref"></param>
        public abstract void Start();

        public abstract void Start(FilesImported finder);

        /// <summary>
        /// Соединение
        /// </summary>
        protected virtual IDbConnection Connection { get; set; }

        /// <summary>
        /// Открыть соединение
        /// </summary>
        /// <returns></returns>
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

        /// <summary>
        /// Закрыть соединение
        /// </summary>
        /// <returns></returns>
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
        
        /// <summary>
        /// Список комментариев
        /// </summary>
        private List<string> CommentList = new List<string>();

        /// <summary>
        /// Запись комментария в журнал выгрузки
        /// </summary>
        /// <param name="comment"></param>
        protected virtual void AddComment(string comment)
        {
            if (comment == String.Empty) return;
            CommentList.Add(comment);
        }

        public virtual string GetComment()
        {
            string ret = "";

            foreach (var element in CommentList)
            {
                ret += element + "\n";
            }

            return ret;
        }

        /// <summary>
        /// Проверка колонки на наличие пустых данных
        /// </summary>
        /// <param name="columnName"></param>
        /// <param name="param"></param> 
        /// <param name="messageText"></param>
        /// <param name="insertDefaultValue"></param>
        /// <param name="columnDefaultValue"></param>
        protected void CheckColumnOnEmptiness(string columnName, string param, string messageText, bool insertDefaultValue = false, string columnDefaultValue = "")
        {
            string parametr = "";

            if (param == "null")
                parametr = " IS NULL";
            if (param == "negative")
                parametr = " < 0";
            if (param == "zero")
                parametr = " = 0"; 

            string sql =
                " SELECT COUNT(*) as count " +
                " FROM " + Name +
                " WHERE " + columnName + parametr;
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
                        " WHERE  " + columnName + parametr;
                    ExecSQL(sql);
                }
            }
        }


        protected void СhangeProcessStatus(decimal progress, ExcelUtility.Statuses status, ExcelUtility excUtility)
        {
            OpenConnection();
            SetProcessProgress(progress, excUtility.nzp_exc);

            var myFile = new DBMyFiles();
            myFile.SetFileStatus(excUtility.nzp_exc, status);

            if (status == ExcelUtility.Statuses.Success || status == ExcelUtility.Statuses.Failed)
            {
                string sql = " UPDATE " + DBManager.sDefaultSchema + "excel_utility " +
                             " SET dat_out= " + Utils.EStrNull(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")) +
                             " WHERE nzp_exc = " + excUtility.nzp_exc;
                ExecSQL(sql);
                if (status == ExcelUtility.Statuses.Success)
                {
                    myFile.SetFilePath(new ExcelUtility()
                    {
                        nzp_exc = excUtility.nzp_exc,
                        exc_path = System.IO.Path.GetFileName(excUtility.exc_path)
                    });
                }
            }
            CloseConnection();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="progress"></param>
        protected void SetProcessProgress(decimal progress, int nzp_exc)
        {
            var myFile = new DBMyFiles();
            myFile.SetFileProgress(nzp_exc, progress);
        }

        protected bool GetIsKzn(FilesImported finder)
        {
            string sql;
            bool IsKzn = false;

            sql =
                " SELECT * " +
                " FROM INFORMATION_SCHEMA.COLUMNS" +
                " WHERE table_schema = '" + finder.bank + "_charge_" + finder.year.Substring(2, 2) + "' " +
                " AND table_name = 'charge_" + finder.month.PadLeft(2, '0') + "' " +
                " AND column_name = 'sum_tarif_sn_f'";
            if (ExecSQLToTable(sql).Rows.Count != 0)
                IsKzn = true;

            return IsKzn;
        }
      }

}
