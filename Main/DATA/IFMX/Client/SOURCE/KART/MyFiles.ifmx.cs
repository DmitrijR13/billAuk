using System;
using System.Data;
using STCLINE.KP50.Global;
using System.Text;

namespace STCLINE.KP50.DataBase
{
    /// <summary>
    /// Класс работы с таблицей ExcelUtility
    /// Осуществляет добавление, удаление заданий,
    /// прогресс заданий
    /// </summary>
    public class DBMyFiles : DataBaseHead
    {
        /// <summary>
        /// Подключение к Web базе
        /// </summary>
        private readonly IDbConnection _connWeb; 

        /// <summary>
        /// Добавляет запись в список моих файлов и возвращает код записи при успешном выполнении операции
        /// </summary>
        /// <param name="finder"></param>
        /// <returns></returns>
        public Returns AddFile(ExcelUtility finder)
        {

            Returns ret = Utils.InitReturns();
            if (_connWeb == null)
            {
                ret.result = false;
                return ret;
            }

            string sql = "insert into " + sDefaultSchema + "excel_utility (nzp_user, stats, prms, dat_in, rep_name," +
                         "  exc_comment, dat_today, exc_path, is_shared, file_name) " +
                         " values (" + finder.nzp_user +
                         ", " + (int) finder.status +
                         ", " + Utils.EStrNull(finder.prms, "empty") +
                         "," + Utils.EStrNull(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")) +
                         ", " + Utils.EStrNull(finder.rep_name) +
                         ", " + Utils.EStrNull(finder.exec_comment) +
                         ", " + Utils.EStrNull(DateTime.Now.ToShortDateString()) +
                         ", " + Utils.EStrNull(finder.exc_path) +
                         ", " + (int) finder.is_shared +
                         ", " + Utils.EStrNull(finder.file_name) + ")";

            ret = ExecSQL(_connWeb, sql, true);
            if (!ret.result) return ret;

            int id = GetSerialValue(_connWeb);

            if (finder.status == ExcelUtility.Statuses.InProcess)
            {
                ExecSQL(_connWeb, " update " + sDefaultSchema + "excel_utility set dat_start = dat_in where nzp_exc = " + id, true);
            }


            ret.tag = id;

            return ret;
        }


        /// <summary>
        /// Получить контекст задания из таблицы ExcelUtility
        /// </summary>
        /// <param name="id"></param>
        /// <param name="userID"></param>
        /// <returns></returns>
        public ExcelUtility GetFile(int id, int userID)
        {
            var excelUtility = new ExcelUtility();
            if (_connWeb == null)
            {

                return excelUtility;
            }

            MyDataReader reader;
            string sql = " select  nzp_exc, nzp_user, prms, stats, dat_in, dat_start, dat_out, tip, rep_name, " +
                         " exc_path, exc_comment, progress, file_name " +
                         " from excel_utility " +
                         " where ((is_shared=1)  or (nzp_user = " + userID + "))  " +
                         " and nzp_exc = " + id;

            Returns ret = ExecRead(_connWeb, out reader, sql, true);
            if (!ret.result) return excelUtility;
            
            if (reader.Read())
            {
                if (reader["nzp_exc"] != DBNull.Value) excelUtility.nzp_exc = Convert.ToInt32(reader["nzp_exc"]);
                if (reader["stats"] != DBNull.Value) excelUtility.stats = Convert.ToInt32(reader["stats"]);
                if (reader["dat_out"] != DBNull.Value)
                    excelUtility.dat_out = reader["dat_out"].ToString().Trim();
                if (reader["rep_name"] != DBNull.Value)
                    excelUtility.rep_name = reader["rep_name"].ToString().Trim();
                if (reader["exc_path"] != DBNull.Value)
                    excelUtility.exc_path = reader["exc_path"].ToString().Trim(); ;
                if (reader["exc_comment"] != DBNull.Value)
                    excelUtility.exec_comment = reader["exc_comment"].ToString().Trim();
                if (reader["progress"] != DBNull.Value) 
                    excelUtility.progress = Convert.ToDecimal(reader["progress"]);
                if (reader["file_name"] != DBNull.Value)
                    excelUtility.file_name = reader["file_name"].ToString().Trim();
            }
            reader.Close();
            return excelUtility;
        }



        /// <summary>
        /// Проверка на существование таблицы excel_utility, если нет, то создаем
        /// </summary>
        /// <returns></returns>
        private bool CheckExcelUtilityTable()
        {
            if (_connWeb == null) return false;
            if (!TempTableInWebCashe(_connWeb, sDefaultSchema+"excel_utility"))
            {
               Returns ret = ExecSQL(_connWeb,
                    " create table "+sDefaultSchema+"excel_utility " +
                    " ( nzp_exc      serial not null, " +
                    " nzp_user     integer not null, " +
                    " prms         char(200) not null, " +
                    " stats        integer default 0, " +
                    " dat_in       " + DBManager.sDateTimeType + ", " +
                    " dat_start    " + DBManager.sDateTimeType + ", " +
                    " dat_out      " + DBManager.sDateTimeType + ", " +
                    " tip          integer default 0 not null, " +
                    " rep_name     char(100),    " +
                    " exc_path     char(200), " +
                    " exc_comment  char(200), " +
                    " dat_today    date,   " +
                    " progress     integer default 0, " +
                    " is_shared INTEGER default 0, " +
                    " file_extension CHAR(10), " +
                    " file_name    char(200)" +
                    " ) ", true);
                if (!ret.result)
                {
                    return false;
                }


                ExecSQL(_connWeb, " create unique index ix_exc_1 on excel_utility (nzp_exc); ", true);
                ExecSQL(_connWeb, " create        index ix_exc_2 on excel_utility (nzp_user, dat_in); ", true);
                ExecSQL(_connWeb, DBManager.sUpdStat + "  excel_utility ", true);
            }
            else
            {
                Returns ret = AddFieldToTable(_connWeb, "excel_utility", "progress", "integer default 0");
                if (!ret.result)
                {
                    return false;
                }
            }
            return true;
        }


        /// <summary>
        /// Изменяет значение прогресса для файла в таблице ExcelUtility
        /// </summary>
        /// <param name="id">код файла(задания)</param>
        /// <param name="progress">Знчение прогресса</param>
        /// <returns></returns>
        public Returns SetFileProgress(int id, decimal progress)
        {
            
            if (_connWeb == null)
            {
                Returns ret = Utils.InitReturns();
                ret.result = false;
                return ret;
            }
            return ExecSQL(_connWeb, " update " + sDefaultSchema + "excel_utility " +
                                    " set progress = " + progress.ToString("0.00").Replace(",",".") + " " +
                                    " where nzp_exc = " + id, true);
        }


        /// <summary>
        /// Удаляет файл(задание) из таблицы ExcelUtility
        /// </summary>
        /// <param name="id">код файла(задания)</param>
        /// <returns></returns>
        public Returns DelFile(int id)
        {

            if (_connWeb == null)
            {
                Returns ret = Utils.InitReturns();
                ret.result = false;
                return ret;
            }
            return ExecSQL(_connWeb, " delete  from " + sDefaultSchema + "excel_utility " +
                                     " where nzp_exc = " + id, true);
        }


        /// <summary>
        /// Изменяет значение прогресса для файла в таблице ExcelUtility
        /// </summary>
        /// <param name="id">код файла(задания)</param>
        /// <param name="status"></param>
        /// <returns></returns>
        public Returns SetFileStatus(int id, ExcelUtility.Statuses status)
        {

            if (_connWeb == null)
            {
                Returns ret = Utils.InitReturns();
                ret.result = false;
                return ret;
            }
            return ExecSQL(_connWeb, " update " + sDefaultSchema + "excel_utility " +
                                     " set stats = " + (int)status +
                                     " where nzp_exc = " + id, true);
        }

        /// <summary>
        /// Установка статуса задания
        /// </summary>
        /// <param name="finder"></param>
        /// <returns></returns>
        public Returns SetFileState(ExcelUtility finder)
        {
            IDbConnection conn_db = GetConnection(Constants.cons_Webdata);
            Returns ret = OpenDb(conn_db, true);
            if (!ret.result) return ret;

            StringBuilder sql = new StringBuilder();
            sql.Append(" update " + DBManager.sDefaultSchema + "excel_utility set stats = " + (int)finder.status);
            if (finder.status == ExcelUtility.Statuses.InProcess)
            {
                sql.Append(", dat_start = " + Utils.EStrNull(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")));
            }
            else if (finder.status == ExcelUtility.Statuses.Success || finder.status == ExcelUtility.Statuses.Failed)
            {
                sql.Append(", dat_out = " + Utils.EStrNull(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")));
            }
            if (finder.exc_path != "") sql.Append(", exc_path = " + Utils.EStrNull(finder.exc_path));
            sql.Append(" where nzp_exc =" + finder.nzp_exc);

            ret = ExecSQL(conn_db, sql.ToString(), true);

            conn_db.Close();
            sql.Remove(0, sql.Length);

            return ret;
        }

        public Returns SetFilePath(ExcelUtility finder)
        {
            Returns ret = Utils.InitReturns();
            if (_connWeb == null)
            {

                ret.result = false;
                return ret;
            }
            
            string sql =
                " UPDATE " + DBManager.sDefaultSchema + "excel_utility " +
                " SET exc_path = " + Utils.EStrNull(finder.exc_path) +
                " WHERE nzp_exc =" + finder.nzp_exc;
            ret = ExecSQL(_connWeb, sql, true);

            return ret;
        }

        public DBMyFiles()
        {
            _connWeb = GetConnection(Constants.cons_Webdata);
            if (!OpenDb(_connWeb, true).result) _connWeb = null;
            
            if (!CheckExcelUtilityTable())
            {
                if (_connWeb != null)
                {
                    _connWeb.Close();
                }
                _connWeb = null;
            }
        }

        ~DBMyFiles()
        {
            if (_connWeb != null) _connWeb.Close();
        }

        
    }
}
