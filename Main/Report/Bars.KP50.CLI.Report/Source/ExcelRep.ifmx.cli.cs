using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using STCLINE.KP50.Global;
//using FastReport;

using STCLINE.KP50.Interfaces;

namespace STCLINE.KP50.DataBase
{
    //Класс для получения данных из генератора отчетов
    //tXXX_prmall , tXXX_spls
    public class ExcelRepClient : DataBaseHead
    {
        public List<ExcelUtility> GetListReport(ExcelUtility finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            if (finder.nzp_user <= 0 && finder.webLogin == "")
            {
                ret.result = false;
                ret.text = "Не задан пользователь";
                ret.tag = -1;
                return null;
            }

            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result) return null;

            string sql = "";
            MyDataReader reader = null;
            try
            {
                if (!TempTableInWebCashe(conn_web, DBManager.sDefaultSchema+"excel_utility"))
                {
                    ret = new Returns(false, "Таблица не сформирована", -1);
                    return null;
                }
                //проверка на существование поля "общедоступный" - is_shared
                //if (!isTableHasColumn(conn_web, "excel_utility", "is_shared"))
                //{
                //    ret = AddFieldToTable(conn_web, "excel_utility", "is_shared", "integer default 0");
                //    if (!ret.result) return null;
                //}                
                ////проверка на существование поля file_name
                //if (!isTableHasColumn(conn_web, "excel_utility", "file_name"))
                //{
                //    ret = AddFieldToTable(conn_web, "excel_utility", "file_name", "char(100)");
                //    if (!ret.result) return null;
                //}

                if (finder.webLogin != "")
                {
                    sql = "select nzp_user from " + DBManager.sDefaultSchema + "users where login = " + Utils.EStrNull(finder.webLogin);

                    ret = ExecRead(conn_web, out reader, sql, true);
                    if (!ret.result)
                    {
                        return null;
                    }
                    if (!reader.Read())
                    {
                        ret.text = "Пользователь не найден";
                        ret.result = false;
                        return null;
                    }
                    finder.nzp_user = Convert.ToInt32(reader["nzp_user"]);
                    reader.Close();
                }

                string where = "";
                if (finder.nzp_exc > 0) where += " and nzp_exc = " + finder.nzp_exc;
                if (finder.date_begin == "*") where += " and dat_today = " + Utils.EStrNull(DateTime.Now.ToShortDateString());

                string usl = "";
                bool b = false;
                Role finderrole = new Role();
                finderrole.nzp_user = finder.nzp_user;
                finderrole.nzp_role = Constants.roleAdministrator;
                using (DbAdminClient db = new DbAdminClient())
                {
                    b = db.IsUserHasRole(finderrole, out ret);
                }
                if (b) finder.is_user_has_role_admin = 1;
                else finder.is_user_has_role_admin = 0;
                if (finder.is_user_has_role_admin == 0) usl += " where nzp_user = " + finder.nzp_user + where ;
                else usl += " where ( ( is_shared =1 " + where + " )  or (nzp_user = " + finder.nzp_user + where + ")) ";

                StringBuilder filter = new StringBuilder("");
                if (finder.dat_out != "") filter.Append(" and dat_out >= '" + finder.dat_out+"'");
                if (finder.dat_out_po != "") filter.Append(" and dat_out <= '" + finder.dat_out_po + "'");
                if (finder.rep_name != "") filter.Append(" and upper(trim(rep_name)) like '%"+finder.rep_name.Trim().ToUpper()+"%'");
                if (finder.stats != -1) filter.Append(" and stats = " + finder.stats);

                sql = "select count(*) from excel_utility " + usl + filter.ToString();
                object obj = ExecScalar(conn_web, sql, out ret, true);
                if (!ret.result) return null;
                int num = Convert.ToInt32(obj);
#if PG
                sql = "select "  +
                    " nzp_exc, nzp_user, prms, stats, dat_in, dat_start, dat_out, tip, rep_name, exc_path, exc_comment, progress, file_name " +
                    " from " + DBManager.sDefaultSchema + "excel_utility " +
                  usl + filter.ToString() +
                    " order by dat_in desc, nzp_exc desc" +(finder.skip > 0 ? " offset " + finder.skip : "") + (finder.rows > 0 ? " limit " + finder.rows : "");
#else
                sql = "select " + (finder.skip > 0 ? " skip " + finder.skip : "") + (finder.rows > 0 ? " first " + finder.rows : "") +
                    " nzp_exc, nzp_user, prms, stats, dat_in, dat_start, dat_out, tip, rep_name, exc_path, exc_comment, progress, file_name " +
                    " from excel_utility " +
                    " where ((is_shared=1 " + where + " )  or (nzp_user = " + finder.nzp_user + where + ")) " + filter.ToString() +
                    " order by dat_in desc, nzp_exc desc";
#endif
                ret = ExecRead(conn_web, out reader, sql, true);
                if (!ret.result) return null;

                List<ExcelUtility> list = new List<ExcelUtility>();
                while (reader.Read())
                {
                    ExcelUtility zap = new ExcelUtility();
                    if (reader["nzp_exc"] != DBNull.Value) zap.nzp_exc = Convert.ToInt32(reader["nzp_exc"]);
                    if (reader["stats"] != DBNull.Value) zap.stats = Convert.ToInt32(reader["stats"]);
                    if (reader["dat_out"] != DBNull.Value) zap.dat_out = Convert.ToString(reader["dat_out"]).Trim();
                    if (reader["rep_name"] != DBNull.Value) zap.rep_name = Convert.ToString(reader["rep_name"]).Trim();
                    if (reader["exc_path"] != DBNull.Value) zap.exc_path = Convert.ToString(reader["exc_path"]).Trim();
                    if (reader["exc_comment"] != DBNull.Value) zap.exec_comment = Convert.ToString(reader["exc_comment"]).Trim();
                    if (reader["progress"] != DBNull.Value) zap.progress = Convert.ToDecimal(reader["progress"]);
                    if (reader["file_name"] != DBNull.Value) zap.file_name = Convert.ToString(reader["file_name"]).Trim();
                    if (zap.stats == -1) zap.stats_name = "Выгрузка в файл прошла неудачно";
                    else if (zap.stats == -2)
                    {
                        zap.stats_name = "Выгрузка в файл прошла неудачно(функционал не поддерживается)";
                    }
                    else if (zap.stats == 0) zap.stats_name = "В очереди";
                    else if (zap.stats == 1) zap.stats_name = "Файл в процессе формирования";
                    else if (zap.stats == 2) zap.stats_name = "Файл успешно сформирован";
                    list.Add(zap);
                }
                reader.Close();
                ret.tag = num;
                return list;
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("ExcelReport : " + ex.Message, MonitorLog.typelog.Error, true);
                ret.result = false;
                ret.text = ex.Message;
                return null;
            }
            finally
            {
                if (reader != null) reader.Close();
                conn_web.Close();
            }
        }

        /// <summary>
        /// Добавляет запись в список моих файлов и возвращает код записи при успешном выполнении операции
        /// </summary>
        /// <param name="finder"></param>
        /// <returns></returns>
        public Returns AddMyFile(ExcelUtility finder)
        {
            IDbConnection conn_db = GetConnection(Constants.cons_Webdata);
            Returns ret = OpenDb(conn_db, true);
            if (!ret.result) return ret;
#if PG
            ExecSQL(conn_db, " set search_path to 'public'", false);

#endif
            try
            {
//                #region Проверка на существование таблицы excel_utility, если нет, то создаем
//                if (!TableInWebCashe(conn_db, "excel_utility"))     
//                {
//#if PG
//                    ret = ExecSQL(conn_db,
//                                        " create table public.excel_utility " +
//                                        " ( nzp_exc      serial not null, " +
//                                        " nzp_user     integer not null, " +
//                                        " prms         character(200) not null, " +
//                                        " stats        integer default 0, " +
//                                        " dat_in       timestamp without time zone, " +
//                                        " dat_start    timestamp without time zone, " +
//                                        " dat_out      timestamp without time zone, " +
//                                        " tip          integer not null default 0, " +
//                                        " rep_name     character(100),    " +
//                                        " exc_path     character(200), " +
//                                        " exc_comment  character(200), " +
//                                        " dat_today    date,   " +
//                                        " progress    integer default 0, " +
//                                        " is_shared   integer default 0,  " +
//                                        " file_name    character(100) " +
//                                        " ) ", true);

//#else
//                    ret = ExecSQL(conn_db,
//                    " create table webdb.excel_utility " +
//                    " ( nzp_exc      serial not null, " +
//                    " nzp_user     integer not null, " +
//                    " prms         char(200) not null, " +
//                    " stats        integer default 0, " +
//                    " dat_in       datetime year to second, " +
//                    " dat_start    datetime year to second, " +
//                    " dat_out      datetime year to second, " +
//                    " tip          integer default 0 not null, " +
//                    " rep_name     char(100),    " +
//                    " exc_path     char(200), " +
//                    " exc_comment  char(200), " +
//                    " dat_today    date,   " +
//                    " progress    integer default 0, " +
//                    " is_shared   integer default 0,  " +
//                    " file_name    character(100) " +
//                    " ) ", true);
//#endif
//                    if (!ret.result) return ret;

//                    //создаем индексы
//#if PG
//                    ExecSQL(conn_db, " create unique index public.ix_exc_1 on public.excel_utility (nzp_exc); ", true);
//                    ExecSQL(conn_db, " create        index public.ix_exc_2 on public.excel_utility (nzp_user, dat_in); ", true);
//                    ExecSQL(conn_db, " analyze excel_utility ", true);
//#else
//                ExecSQL(conn_db, " create unique index webdb.ix_exc_1 on webdb.excel_utility (nzp_exc); ", true);
//                ExecSQL(conn_db, " create        index webdb.ix_exc_2 on webdb.excel_utility (nzp_user, dat_in); ", true);
//                ExecSQL(conn_db, " Update statistics for table excel_utility ", true);
//#endif
//                }
//                else
//                {
//                    ret = AddFieldToTable(conn_db, "excel_utility", "progress", "integer default 0");
//                    if (!ret.result) return ret;
//                }
//                //проверка на существование поля "общедоступный" - is_shared
//                if (!isTableHasColumn(conn_db, "excel_utility", "is_shared"))
//                {
//                    ret = AddFieldToTable(conn_db, "excel_utility", "is_shared", "integer default 0");
//                    if (!ret.result) return ret;
//                }

//                #endregion

                StringBuilder sql = new StringBuilder();
                sql.Append("insert into " + DBManager.sDefaultSchema + "excel_utility (nzp_user, stats, prms, dat_in, rep_name, exc_comment, dat_today, exc_path, is_shared, file_name) ");
                sql.Append(" values (" + finder.nzp_user +
                    ", " + (int)finder.status +
                    ", " + Utils.EStrNull(finder.prms, "empty") +
                    "," + Utils.EStrNull(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")) +
                    ", " + Utils.EStrNull(finder.rep_name) +
                    ", " + Utils.EStrNull(finder.exec_comment) +
                    ", " + Utils.EStrNull(DateTime.Now.ToShortDateString()) +
                    ", " + Utils.EStrNull(finder.exc_path) +
                    ", " + finder.is_shared +
                    ", " + Utils.EStrNull(finder.file_name) +
                    ")");

                ret = ExecSQL(conn_db, sql.ToString(), true);
                if (!ret.result) return ret;

                int id = GetSerialValue(conn_db);

                if (finder.status == ExcelUtility.Statuses.InProcess)
                {
                    ExecSQL(conn_db, "update " + DBManager.sDefaultSchema + "excel_utility set dat_start = dat_in where nzp_exc = " + id, true);
                }
                else if (finder.status == ExcelUtility.Statuses.Failed)
                {
                    ExecSQL(conn_db, "update " + DBManager.sDefaultSchema + "excel_utility set dat_start = dat_in, dat_out = dat_in where nzp_exc = " + id, true);
                }

                sql.Remove(0, sql.Length);

                ret.tag = id;
            }
            catch (Exception ex)
            {
                ret.text = ex.Message;
                ret.result = false;
                MonitorLog.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().Name + "\n" + ex.Message, MonitorLog.typelog.Error, true);
            }
            finally
            {
                conn_db.Close();
            }

            return ret;
        }

        /// <summary>
        /// Обновляет процент выполнения задания
        /// </summary>
        /// <param name="finder"></param>
        /// <returns></returns>
        public Returns SetMyFileProgress(ExcelUtility finder)
        {
            IDbConnection conn_db = GetConnection(Constants.cons_Webdata);
            Returns ret = OpenDb(conn_db, true);
            if (!ret.result) return ret;

            string sql = "update " + DBManager.sDefaultSchema + "excel_utility set progress = " + finder.progress.ToString("N4").Replace(',', '.') + " where nzp_exc = " + finder.nzp_exc;
            ret = ExecSQL(conn_db, sql, true);

            conn_db.Close();
            return ret;
        }

        /// <summary>
        /// Установка статуса задания
        /// </summary>
        /// <param name="finder"></param>
        /// <returns></returns>
        public Returns SetMyFileState(ExcelUtility finder)
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
    }
}
