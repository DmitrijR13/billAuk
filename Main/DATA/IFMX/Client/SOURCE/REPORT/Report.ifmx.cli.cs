using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using STCLINE.KP50.Global;
using STCLINE.KP50.Utility;
using System.Data;

namespace STCLINE.KP50.DataBase
{
    /// <summary>
    /// класс для работы с отчетами
    /// </summary>
    public class ReportUtils : DataBaseHead
    {
        /// <summary>
        /// процедура записи данных о выгружаемом отчете в БД
        /// </summary>
        /// <param name="report">объект отчета</param>
        /// <param name="ret"></param>
        public void RunReport(ReportParams report, out Returns ret)
        {
            ret = Utils.InitReturns();
            //проверка входных данных
            if (report.nzp_user != 0 && report.id != 0)
            {
                IDbConnection conn_db = GetConnection(Constants.cons_Webdata);
                ret = OpenDb(conn_db, true);
                if (!ret.result)
                {
                    ret.result = false;
                    ret.text = "Ошибка постановки отчета в очередь";
                    MonitorLog.WriteLog("ReportUtils : Ошибка при открытии соединения с БД ", MonitorLog.typelog.Error, true);
                    return;
                }

                #region Проверка на существование таблицы user_reports
                if (!TableInWebCashe(conn_db, "rep_user_reports"))
                {
                    //создаем таблицу
                    try
                    {
#if PG
  ret = ExecSQL(conn_db,
                                   "begin; CREATE TABLE rep_user_reports( " +
                                   "nzp_exc SERIAL NOT NULL, " +				/*уникальный идентификатор*/
                                   "nzp_rep INTEGER NOT NULL, " +              	/*внешний ключ*/
                                   "nzp_user INTEGER NOT NULL, " +				/*идентификатор пользователя*/
                                   "prms TEXT, " +								/*параметры*/
                                   "status INTEGER default 0, " +				/*статус*/
                                    "dat_in timestamp without time zone, " +       //время запуска
                                    "dat_start timestamp without time zone, " +    //время начала выполнения
                                    "dat_out timestamp without time zone	" +	    //время завершения выполнения
                                   ");"
                                  , true);
#else
                        ret = ExecSQL(conn_db,
                        "CREATE TABLE rep_user_reports( " +
                        "nzp_exc SERIAL NOT NULL, " +				    //уникальный идентификатор
                        "nzp_rep INTEGER NOT NULL, " +              	//внешний ключ
                        "nzp_user INTEGER NOT NULL, " +				//идентификатор пользователя
                        "status INTEGER default 0, " +				    //статус
                        "dat_in DATETIME YEAR to SECOND, " +			/*время запуска*/
                        "dat_start DATETIME YEAR to SECOND, " +		/*время начала выполнения*/
                        "dat_out DATETIME YEAR to SECOND	" +			/*время завершения выполнения*/
                        ");"
                    , true);
#endif
                    }
                    catch (Exception ex)
                    {
                        ret.result = false;
                        ret.text = "Ошибка постановки отчета в очередь";
                        MonitorLog.WriteLog("Ошибка при создании таблицы user_reports" + ex.Message, MonitorLog.typelog.Error, true);
                        conn_db.Close();
                        return;
                    }
                    //создание индексов
#if PG
if (ret.result) { ret = ExecSQL(conn_db, "lock table rep_user_reports in row share mode ; commit;", true); }
                    if (ret.result) { ret = ExecSQL(conn_db, "CREATE unique INDEX rur_rec_1 ON rep_user_reports(nzp_exc);", true); }
#else
                    if (ret.result) { ret = ExecSQL(conn_db, "alter table rep_user_reports lock mode (row); ", true); }
                    if (ret.result) { ret = ExecSQL(conn_db, "CREATE unique INDEX rur_rec_1 ON rep_user_reports(nzp_exc);", true); }
#endif
                    if (ret.result && TableInWebCashe(conn_db, "rep_fastrep"))
                    {
#if PG
                        ret = ExecSQL(conn_db, "ALTER TABLE rep_user_reports ADD CONSTRAINT nzp_rep FOREIGN KEY " +
                                                 "(nzp_rep) REFERENCES rep_fastrep (nzp_rep);", true);
#else
                        ret = ExecSQL(conn_db, @"ALTER TABLE rep_user_reports ADD CONSTRAINT (FOREIGN KEY " +
                                                "(nzp_rep) REFERENCES rep_fastrep CONSTRAINT nzp_rep);", true);
#endif
                    }
#if PG
 if (ret.result) { ret = ExecSQL(conn_db, "CREATE INDEX rur_rec_2 ON rep_user_reports(nzp_user);", true); }
                    if (ret.result) { ret = ExecSQL(conn_db, "analyze rep_user_reports", true); }
#else
                    if (ret.result) { ret = ExecSQL(conn_db, "CREATE INDEX rur_rec_2 ON rep_user_reports(nzp_user);", true); }
                    if (ret.result) { ret = ExecSQL(conn_db, "UPDATE STATISTICS FOR TABLE rep_user_reports", true); }
#endif
                    else
                    {
                        MonitorLog.WriteLog("Ошибка создания индексов в таблице rep_user_reports", MonitorLog.typelog.Error, true);
                        conn_db.Close();
                        return;
                    }
                }
                #endregion

                StringBuilder sql = new StringBuilder();
                
                string DateTimeString = IfmxFormatDatetimeToTime(report.date_in, out ret);
                sql.Append("INSERT INTO rep_user_reports (nzp_rep, nzp_user, dat_in) ");
                sql.Append("VALUES (" + report.id + ", " + report.nzp_user + ", \'" + Convert.ToDateTime(report.date_in).ToString("yyyy-MM-dd HH:mm:ss") + "\');");

                IDbCommand cmd = DBManager.newDbCommand(sql.ToString(), conn_db);
                try
                {
                    cmd.ExecuteNonQuery();

                    int unique = GetSerialValue(conn_db, null);

                    foreach (KeyValuePair<int, string> pair in report.selectedValues)
                    {
                        sql.Remove(0, sql.Length);
                        sql.Append("INSERT INTO rep_user_reports_params (nzp_rep, nzp_prm, prm_value) ");
                        sql.Append("VALUES (" + unique + ", " + pair.Key + ", " + pair.Value + ");");
                        cmd.CommandText = sql.ToString();
                        cmd.ExecuteNonQuery();
                    }

                    ret.text = "Отчет " + report.name + " успешно помещен в очередь на выполнение!";

                    #region запись в действующую таблицу excel
                    
                    sql.Remove(0, sql.Length);
                    sql.Append("insert into  excel_utility (nzp_user, prms, dat_in,  tip, rep_name, exc_comment, dat_today) ");
                    sql.Append(" values (" + report.nzp_user + ", \' \'," + "\'" + DateTimeString + "\',2, (SELECT name FROM report WHERE nzp_act = " + report.id + "), \'" + report.comment + "\', \'" + Convert.ToDateTime(report.date_in).ToShortDateString() + "\');");
                    cmd.CommandText = sql.ToString();
                    cmd.ExecuteNonQuery();

                    #endregion

                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = ex.Message;
                    MonitorLog.WriteLog("Ошибка записи в БД (RunReport): " + ex.Message, MonitorLog.typelog.Error, true);
                    return;
                }
                finally
                {
                    if (conn_db != null)
                    {
                        conn_db.Close();
                    }
                    sql.Remove(0, sql.Length);
                }
                conn_db.Close();
            }
            else
            {
                ret.result = false;
                ret.text = "Ошибка заполнения информации об отчете!";
                MonitorLog.WriteLog("Не заполнены все необходимые поля объекта ReportInfom!", MonitorLog.typelog.Error, true);
                return;
            }
        }

        /// <summary>
        /// получение объекта отчета
        /// </summary>
        /// <param name="id">идентификатор отчета</param>
        /// <param name="ret"></param>
        /// <returns></returns>
        public ReportParams GetReportById(int id, out Returns ret)
        {
            ret = Utils.InitReturns();
            ReportParams report = new ReportParams();
            //проверка входных данных
            if (id <= 0)
            {
                ret.result = false;
                ret.text = "Отчет не определен";
                ret.tag = -1;
                return null;
            }
            IDbConnection conn_db = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_db, true);
            if (!ret.result)
            {
                ret.result = false;
                ret.text = "Ошибка получения данных об отчете";
                MonitorLog.WriteLog("GetReportById : Ошибка при открытии соединения с БД ", MonitorLog.typelog.Error, true);
                return null;
            }
            StringBuilder sql = new StringBuilder();

            /*#region добавление параметра
            sql.Append("Insert into report_utils (nzp_act, prms, dll) ");
            report.prmsStr = @"1,2";
            sql.Append("values (304,?,?);");
            IDbCommand cmd = DBManager.newDbCommand(sql.ToString(), conn_db);
            DBManager.addDbCommandParameter(cmd, "@binaryValue", DbType.String, report.prmsStr);
            DBManager.addDbCommandParameter(cmd, "@binaryValue", DbType.String, "Report");
            cmd.ExecuteNonQuery();
            sql.Remove(0, sql.Length);
            #endregion*/

            sql.Append("SELECT nzp_cat FROM report_params WHERE nzp_act = " + id + ";");
            IDataReader reader;
            if (!ExecRead(conn_db, out reader, sql.ToString(), true).result)
            {
                MonitorLog.WriteLog("Ошибка выборки " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                conn_db.Close();
                ret.result = false;
                return null;
            }
            try
            {
                while (reader.Read())
                {
                    report.id = id;
                    if (reader["nzp_cat"] != DBNull.Value) report.prms.Add(Convert.ToInt32((reader["nzp_cat"]).ToString().Trim()));
                }
                reader.Close();
                conn_db.Close();
                return report;
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("GetReportById : " + ex.Message, MonitorLog.typelog.Error, true);
                if (reader != null) reader.Close();
                conn_db.Close();
                ret.result = false;
                ret.text = ex.Message;
                return null;
            }
        }

        public string IfmxFormatDatetimeToTime(string datahour, out Returns ret)
        {
            //привести "дд.мм.гггг чч:мм" к формату "гггг-мм-дд ч:м:с"
            ret = new Returns(false);
            string outs = "";

            if (String.IsNullOrEmpty(datahour))
            {
                return outs;
            }

            datahour = datahour.Trim();

            string[] mas1 = datahour.Split(new string[] { " " }, StringSplitOptions.None);

            string dt = "";
            string hm = "";
            try
            {
                dt = mas1[0].Trim();
                hm = mas1[1].Trim();

                if (String.IsNullOrEmpty(dt) || String.IsNullOrEmpty(hm))
                {
                    return outs;
                }

                string[] mas2 = dt.Split(new string[] { "." }, StringSplitOptions.None);
                //string[] mas3 = hm.Split(new string[] { ":" }, StringSplitOptions.None);

                outs = mas2[2].Trim() + "-" + mas2[1].Trim() + "-" + mas2[0].Trim() + " " + hm;
                ret.result = true;
            }
            catch
            {
                return outs;
            }

            return outs;
        }
    }
}
