using System;
using System.Collections.Generic;
using System.Data;
using System.Text.RegularExpressions;
using STCLINE.KP50.Global;
using System.Text;
using STCLINE.KP50.Interfaces;

namespace STCLINE.KP50.DataBase
{
    public class DataImportUtils : DbAdminClient
    {
        public static string plike
        {
            get
            {
#if PG
                return " like ";
#else
                return " matches ";
#endif
            }
        }

        /// <summary>
        /// Создание обычных индексов для таблицы
        /// </summary>
        /// <param name="conn_db"></param> соединение
        /// <param name="fullTableNameWithPref"></param> полное имя таблицы с названием БД (прим. ftul_data:file_kvar)
        /// <param name="indexDictionary"></param> Dictionary вида (название индекса типа string, названия полей для индекса типа List/<string/> )
        public void CreateOneIndex(IDbConnection conn_db, string fullTableNameWithPref, Dictionary<string, List<string>> indexDictionary)
        {
            string sql;
            string col = "";
            try
            {
                foreach (KeyValuePair<string, List<string>> kvp in indexDictionary)
                {
                    col = "";
                    foreach (string colunm in kvp.Value)
                    {
                        col += colunm + ",";
                    }
                    //убираем последнюю запятую
                    col = col.Substring(0, col.Length - 1);
                    try
                    {
                        sql = "create index " + kvp.Key + " on " + fullTableNameWithPref + " (" + col + ")";
                        ClassDBUtils.ExecSQL(sql, conn_db, ClassDBUtils.ExecMode.Log);
                    }
                    catch
                    {
                    }
                }
                sql = DBManager.sUpdStat + " " + fullTableNameWithPref;
                DBManager.ExecSQL(conn_db, null, sql, true);
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры CreateOneIndex : " + ex.Message + Environment.NewLine + ex.StackTrace, MonitorLog.typelog.Error, true);
            }
        }

        public void CreateUniqueIndex(IDbConnection conn_db, string fullTableNameWithPref, string indexName,
            List<string> columnsList)
        {
            string sql;
            string col = "";
            foreach (string colunm in columnsList)
            {
                col += colunm + ",";
            }
            //убираем последнюю запятую
            col = col.Substring(0, col.Length - 1);
            try
            {
                sql = "create unique index " + indexName + " on " + fullTableNameWithPref + " (" + col + ")";
                ClassDBUtils.ExecSQL(sql, conn_db, ClassDBUtils.ExecMode.Log);
            }
            catch
            {
            }
        }
        //Задание #83694: Функция, создающая sequence в случае отсутствия и restart-ующая с max значения по нижнему и всем верхним банкам для postgre и informix
        public void CreateSequence(IDbConnection conn_db, string TableName, string TableNzp)
        {
            //Returns re=ret;
            long maxseq = 0;
            List<string> pref = new List<string>();
            IDataReader reader = null;
            StringBuilder sql = new StringBuilder();
            conn_db.Open();
 
            sql.Append("select bd_kernel from  " + Points.Pref + DBManager.sKernelAliasRest + "s_point");
            if (DBManager.ExecRead(conn_db, out reader, sql.ToString(), true).result)
            {
                while (reader.Read())
                {
                    if (reader["bd_kernel"] != null)
                    {

                        pref.Add(reader["bd_kernel"].ToString().Trim());
                    }
                }

                for (int i = 0; i < pref.Count; i++)
                {
                    sql.Remove(0, sql.Length);
                    sql.Append("Select max(" + TableNzp + ") as nzp_kvar from " + pref[i].ToString() + DBManager.sDataAliasRest + TableName);
                    if (DBManager.ExecRead(conn_db, out reader, sql.ToString(), true).result)
                    {
                        reader.Read();
                        string s = reader["nzp_kvar"].ToString();
                        long aa = Convert.ToInt64(reader[TableNzp]);
                        if (maxseq < aa) maxseq = Convert.ToInt64(reader[TableNzp]);
                    }
                }
                sql.Remove(0, sql.Length);
                sql.Append("alter sequence " + Points.Pref + DBManager.sKernelAliasRest + TableName + TableNzp + "_seq restart " + (maxseq++).ToString());
                DBManager.ExecSQL(conn_db, sql.ToString(), true);
            }
            else return;
            if (conn_db != null)
            {
                conn_db.Close();
            }
        }
        
        public List<FilesImported> GetFiles(FilesImported finder, out Returns ret)
        {
            IDataReader reader = null;
            IDbConnection conn_db = null;
            List<FilesImported> result = new List<FilesImported>();
            ret = STCLINE.KP50.Global.Utils.InitReturns();
            try
            {
                string connectionString = Points.GetConnByPref(finder.pref);
                conn_db = GetConnection(connectionString);
                ret = OpenDb(conn_db, true);
                if (!ret.result)
                {
                    conn_db.Close();
                    return null;
                }

                if (!TempTableInWebCashe(conn_db, Points.Pref + sUploadAliasRest + "files_imported"))
                {
                    ret.result = false;
                    ret.text = "Данные о загруженных файлах временно не доступны";
                    ret.tag = -1;
                    conn_db.Close();
                    return null;
                }

                //99 - это разовая загрузка
                // 1 - выгрузка Характеристик ЖКУ
                // 2 - загрузка из СЗ
                string file_type = " and " + sNvlWord + "(fi.file_type,99) = 99 " + " and trim(upper(pref)) <> 'CHECKFILE' ";
                if (finder.file_type != 0)
                    file_type = " and fi.file_type = " + finder.file_type + " ";
#if PG
                string skip = finder.skip >= 0 && finder.rows >= 0 ? " offset " + finder.skip + " limit " + finder.rows : String.Empty;
#else
                string skip = finder.skip >= 0 && finder.rows >= 0 ? " skip " + finder.skip + " first " + finder.rows : String.Empty;
#endif

                string sql = "select " +
#if PG
#else
 skip +
#endif
 " fi.nzp_file nzp_file, fi.nzp_exc,  fi.nzp_exc_log, fi.nzp_status ,fi.created_on date, fi.loaded_name, fi.saved_name," +
                    " fs.status_name status, ff.format_name, fv.version_name, u.comment loaded_by, fi.percent," +
                    " fi.diss_status as diss_status, fh.calc_date, fi.nzp_exc_rep_load " +
                    " from " + Points.Pref + DBManager.sUploadAliasRest + "files_imported fi" +
                    " left join " + Points.Pref + DBManager.sUploadAliasRest + "file_head fh on fh.nzp_file = fi.nzp_file" +
                    " left join " + Points.Pref + DBManager.sUploadAliasRest + "file_versions fv on fi.nzp_version = fv.nzp_version" +
                    " left join " + Points.Pref + DBManager.sUploadAliasRest + "file_formats ff on ff.nzp_ff = fv.nzp_ff" +
                    " left join " + Points.Pref + DBManager.sUploadAliasRest + "file_statuses fs on fs.nzp_stat = fi.nzp_status" +
                    " left join " + Points.Pref + "_data" + tableDelimiter + "users u on u.nzp_user = fi.created_by" +
                    " where fi.nzp_status <>" + (int)FilesImported.Statuses.Deleted + " " + file_type +
                    " and fi.pref = '" + finder.bank + "' " +
                    " order by date DESC" +
#if PG
                skip+
#else

#endif
 "";
                ret = ExecRead(conn_db, out reader, sql, true);
                if (!ret.result)
                {
                    conn_db.Close();
                    return null;
                }

                int i = 0;
                while (reader.Read())
                {
                    #region Месяц, за который грузим месяц

                    string calc_date = "";

                    var months = new[]
                    {
                        "", "Январь", "Февраль",
                        "Март", "Апрель", "Май", "Июнь", "Июль", "Август", "Сентябрь",
                        "Октябрь", "Ноябрь", "Декабрь"
                    };
                    if (reader["calc_date"] != DBNull.Value)
                    {
                        int n_month = Convert.ToInt32((reader["calc_date"]).ToString().Substring(3, 2));
                        calc_date = " (" + months[n_month] + " " + (reader["calc_date"]).ToString().Substring(6, 4) + ")";
                    }

                    #endregion

                    i++;
                    FilesImported file = new FilesImported();
                    file.num = (i + finder.skip).ToString();
                    file.nzp_file = reader["nzp_file"] != DBNull.Value ? Convert.ToInt32(reader["nzp_file"]) : -1;
                    file.nzp_status = reader["nzp_status"] != DBNull.Value ? Convert.ToInt32(reader["nzp_status"]) : -1;
                    file.date = reader["date"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(reader["date"]) : null;
                    file.format_name = reader["format_name"] != DBNull.Value ? (reader["format_name"]).ToString().Trim() : null;
                    file.format_version = reader["version_name"] != DBNull.Value ? (reader["version_name"]).ToString().Trim() : null;
                    file.saved_name = reader["saved_name"] != DBNull.Value ? (reader["saved_name"]).ToString().Trim() : null;
                    file.loaded_name = reader["loaded_name"] != DBNull.Value ? (reader["loaded_name"]).ToString().Trim() + calc_date : null;
                    file.status = reader["status"] != DBNull.Value ? (reader["status"]).ToString() : null;
                    file.loaded_string = reader["loaded_by"] != DBNull.Value ? reader["loaded_by"].ToString().Trim() : null;
                    file.nzp_exc = reader["nzp_exc"] != DBNull.Value ? Convert.ToInt32(reader["nzp_exc"]) : -1;
                    file.nzp_exc_log = reader["nzp_exc_log"] != DBNull.Value ? Convert.ToInt32(reader["nzp_exc_log"]) : -1;
                    file.percent = reader["percent"] != DBNull.Value ? (Convert.ToDecimal(reader["percent"]) * 100).ToString().Substring(0, (Convert.ToDecimal(reader["percent"]) * 100).ToString().Length - 3) + "%" : "";
                    file.diss_status = reader["diss_status"] != DBNull.Value ? (reader["diss_status"]).ToString().Trim() : null;
                    file.nzp_exc_rep_load = reader["nzp_exc_rep_load"] != DBNull.Value ? Convert.ToInt32(reader["nzp_exc_rep_load"]) : -1;
                    file.rep_load_name = reader["nzp_exc_rep_load"] != DBNull.Value ? "Акт загрузки данных" : "";

                    sql =
                        "select * from " + Points.Pref + DBManager.sUploadAliasRest + "files_selected " +
                        "  where nzp_file = " + file.nzp_file +
                        " and nzp_user = " + finder.nzp_user +
                        " and pref = '" + finder.bank.Trim() + "' ";

                    IntfResultTableType dt = ClassDBUtils.OpenSQL(sql, conn_db);
                    if (dt.resultData.Rows.Count == 0)
                        file.to_disassembly = false;
                    else
                        file.to_disassembly = true;

                    result.Add(file);
                }
                string where = " where file_type is null ";
                if (finder.file_type != 0)
                    where = " where file_type = " + finder.file_type + " ";
                sql =
                    " select count(*) from " + Points.Pref + DBManager.sUploadAliasRest + "files_imported fi " +
                    where + " and nzp_status <> 7 " +
                    " and trim(upper(pref)) <> 'CHECKFILE' " +
                    " and fi.pref = '" + finder.bank + "'";

                object count = DBManager.ExecScalar(conn_db, sql, out ret, true);
                if (ret.result)
                {
                    ret.tag = Convert.ToInt32(count);
                }
                return result;
            }
            catch (Exception ex)
            {
                ret = new Returns(false, ex.Message);
                MonitorLog.WriteLog("Ошибка GetFiles\n" + ex.Message, MonitorLog.typelog.Error, 20, 201, true);
                return null;
            }
            finally
            {
                if (reader != null)
                {
                    reader.Close();
                    reader.Dispose();
                }
                if (conn_db != null)
                    conn_db.Close();
            }
        }

        public static string GetIDom(string ndom)
        {
            string pattern = "[1-9][0-9]*";
            string idom = Regex.Match(ndom, pattern).Value;
            idom = string.IsNullOrEmpty(idom) ? "0" : idom;
            return idom;
        }
    }
}
