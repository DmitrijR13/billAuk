using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Threading;
using FastReport;
using STCLINE.KP50.Global;
using System.IO;
using STCLINE.KP50.Interfaces;

namespace STCLINE.KP50.DataBase
{
    public partial class DbSmsMessage : DataBaseHead
    {
        public void FindSmsMessage(SmsMessage finder, out Returns ret)
        {
            ret = Utils.InitReturns();

            #region проверка значений
            //-----------------------------------------------------------------------
            // проверка наличия пользователя
            if (finder.nzp_user < 1)
            {
                ret = new Returns(false, "Не определен пользователь", -1);
                return;
            }
            //-----------------------------------------------------------------------
            #endregion

            #region создать кэш-таблицу
            //-----------------------------------------------------------------------           
            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result) return;
            
            string tXX_mess = "t" + Convert.ToString(finder.nzp_user) + "_mess";
            CreateTableWebMessage(conn_web, tXX_mess, true, out ret);
            if (!ret.result)
            {
                conn_web.Close();
                return;
            }
            //-----------------------------------------------------------------------
            #endregion

            //заполнить webdata
            string conn_kernel = Points.GetConnByPref(Points.Pref);
            IDbConnection conn_db = GetConnection(conn_kernel);

            ret = OpenDb(conn_db, true);
            if (!ret.result)
            {
                conn_web.Close();
                return;
            }

#if PG
            string tXX_mess_full = "public." + tXX_mess;
            string sql = "";
#else
            string tXX_mess_full = conn_web.Database + "@" + DBManager.getServer(conn_web) + ":" + tXX_mess;
            string sql = "";
#endif
            #region собрать условие
            //-----------------------------------------------------------------------
            // дата отправки
            string _where = this.constructSqlDate("s.sended_on", finder.sended_on, finder.sended_on_to);

            if (finder.is_sended == 1) _where += " and s.sended_on is not null ";

            // состояние сообщения
            if (finder.message_status_id > 0) _where += " and s.message_status_id = " + finder.message_status_id;

            // тип сообщения
            if (finder.supg_message_type_id > 0) _where += " and s.supg_message_type_id = " + finder.supg_message_type_id;
            //-----------------------------------------------------------------------
            #endregion

            int key = 0;

            try
            {
                #region сформировать sql для записи в кэш-таблицу
                //----------------------------------------------------------------------------
#if PG
  sql = " Insert into " + tXX_mess_full + " (" +
                        " message_id," +
                        " receiver, mobile_phone," +
                        " supg_message_type_id, sms_text," +
                        " message_status_id, message_status," +
                        " created_on, creator," +
                        " sended_on, sender, status_message, sms_id) " +
                      " Select s.message_id," +
                        " coalesce(r.receiver, '') receiver, s.outbound_address," +
                        " s.supg_message_type_id, s.message_text," +
                        // Статус Отправлено отображать в зависимости от даты отправки
                        // Отправлено для date(scheduled_on) <= today
                        // Отправлено (через N дней) для date(scheduled_on) > today, и N = date(scheduled_on) - today
                        // У кого sended_on is null и date(scheduled_on) <= today выводить статус "Отправлено (ожидание)"
                        " s.message_status_id, " +
                        " (case when s.message_status_id = 210 then " + 
                        "  (case " +
                        "  when s.sended_on is not null and date(s.scheduled_on) <= current_date then 'Отправлено' " +
                        "  when s.sended_on is null and date(s.scheduled_on) > current_date then 'Отправлено через ' || date(s.scheduled_on) - current_date || ' дней' " +
                        "  when s.sended_on is null and date(s.scheduled_on) <= current_date then 'Отправлено (ожидание)' " + 
                        "  else 'Отправлено' end) " + 
                        " else m.message_status end) message_status, " +
                        " s.created_on, (select u.comment From " + Points.Pref + "_supg.users u where u.nzp_user = s.created_by) creator," +
                        " s.sended_on, (select u.comment From " + Points.Pref + "_supg.users u where u.nzp_user = s.sended_by) sender," + 
                        " s.status_message, s.outbound_id " +
                      " From " + Points.Pref + "_msg.message_outbound s, " + Points.Pref + "_msg.s_receiver r, " + Points.Pref + "_msg.s_message_status m " +
                      " Where s.receiver_id = r.receiver_id " + 
                        " and s.message_status_id = m.message_status_id " + _where;
#else
                sql = " Insert into " + tXX_mess_full + " (" +
                                      " message_id," +
                                      " receiver, mobile_phone," +
                                      " supg_message_type_id, sms_text," +
                                      " message_status_id, message_status," +
                                      " created_on, creator," +
                                      " sended_on, sender, status_message, sms_id) " +
                                    " Select s.message_id," +
                                      " nvl(r.receiver, '') receiver, s.outbound_address," +
                                      " s.supg_message_type_id, s.message_text," +
                    // Статус Отправлено отображать в зависимости от даты отправки
                    // Отправлено для date(scheduled_on) <= today
                    // Отправлено (через N дней) для date(scheduled_on) > today, и N = date(scheduled_on) - today
                    // У кого sended_on is null и date(scheduled_on) <= today выводить статус "Отправлено (ожидание)"
                                      " s.message_status_id, " +
                                      " (case when s.message_status_id = 210 then " +
                                      "  (case " +
                                      "  when s.sended_on is not null and date(s.scheduled_on) <= today then 'Отправлено' " +
                                      "  when s.sended_on is null and date(s.scheduled_on) > today then 'Отправлено через ' || date(s.scheduled_on) - today || ' дней' " +
                                      "  when s.sended_on is null and date(s.scheduled_on) <= today then 'Отправлено (ожидание)' " +
                                      "  else 'Отправлено' end) " +
                                      " else m.message_status end) message_status, " +
                                      " s.created_on, (select u.comment From " + Points.Pref + "_supg:users u where u.nzp_user = s.created_by) creator," +
                                      " s.sended_on, (select u.comment From " + Points.Pref + "_supg:users u where u.nzp_user = s.sended_by) sender," +
                                      " s.status_message, s.outbound_id " +
                                    " From " + Points.Pref + "_msg:message_outbound s, " + Points.Pref + "_msg:s_receiver r, " + Points.Pref + "_msg:s_message_status m " +
                                    " Where s.receiver_id = r.receiver_id " +
                                      " and s.message_status_id = m.message_status_id " + _where;
#endif
                //----------------------------------------------------------------------------
                #endregion

                //записать текст sql в лог-журнал
                key = LogSQL(conn_db, finder.nzp_user, sql);
                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result) throw new Exception(ret.text);

                //создать индексы на tXX_cv
                CreateTableWebMessage(conn_web, tXX_mess, false, out ret);
                if (!ret.result) throw new Exception(ret.text); 
            }
            catch (Exception ex)
            {
                if (key > 0) LogSQL_Error(conn_web, key, ex.Message);
                conn_db.Close();
                conn_web.Close();
                return;
            }

            conn_db.Close(); //закрыть соединение с основной базой
            conn_web.Close();
        }

        /// <summary>
        /// Создать кэш-таблицу для сообщений
        /// </summary>
        private void CreateTableWebMessage(IDbConnection conn_web, string tXX_mess, bool onCreate, out Returns ret) //
        {
            if (onCreate)
            {
                if (TableInWebCashe(conn_web, tXX_mess))
                {
                    ExecSQL(conn_web, " Drop table " + tXX_mess, false);
                }

#if PG
                ret = ExecSQL(conn_web,
                      " Create table " + tXX_mess + "(" +
                      " message_id        integer," +
                      " receiver          char(120)," +
                      " mobile_phone      char(20)," +
                      " supg_message_type_id   smallint," +
                      " sms_text          char(255)," +
                      " message_status_id integer," +
                      " message_status    char(60)," +
                      " created_on        timestamp," +
                      " creator           char(120)," +
                      " sended_on         timestamp," +
                      " sender            char(100)," +
                      " status_message    char(255)," +
                      " sms_id            integer)", true);
#else
                ret = ExecSQL(conn_web,
                      " Create table " + tXX_mess + "(" +
                      " message_id        integer," +
                      " receiver          char(120)," +
                      " mobile_phone      char(20)," +
                      " supg_message_type_id   smallint," +
                      " sms_text          char(255)," +
                      " message_status_id integer," +
                      " message_status    char(60)," +
                      " created_on        DATETIME YEAR to SECOND," +
                      " creator           char(120)," +
                      " sended_on         DATETIME YEAR to SECOND," +
                      " sender            char(100)," +
                      " status_message    char(255)," +
                      " sms_id            integer)", true);
#endif
                if (!ret.result)
                {
                    conn_web.Close();
                    return;
                }
            }
            else
            {
                ret = ExecSQL(conn_web, " Create index ix1_" + tXX_mess + " on " + tXX_mess + " (message_id) ", true);
            }
        }

        /// <summary>
        /// Условие для дат
        /// </summary>
        private string constructSqlDate(string field, string dateFrom, string dateTo)
        {
            string sqlCondition = "";

            if (dateFrom.Length > 0 || dateTo.Length > 0)
            {
#if PG
                string _field = " and public.mdy(" + Convert.ToDateTime(field).Month + ", " + 
                                                     Convert.ToDateTime(field).Day + ", "+
                                                     Convert.ToDateTime(field).Year+") ";
#else
                string _field = " and mdy(month(" + field + "), day(" + field + "), year(" + field + ")) ";
#endif


                if (dateFrom == dateTo) sqlCondition = _field + " = '" + dateFrom + "'";
                else
                {
                    if (dateFrom != null) sqlCondition = _field + " >= '" + dateFrom + "'";
                    if (dateTo != null) sqlCondition = _field + " <= '" + dateTo + "'";
                }
            }

            return sqlCondition;
        }

        /// <summary>
        /// Получить список показаний ПУ из кэша
        /// </summary>
        public List<SmsMessage> GetSmsMessage(SmsMessage finder, out Returns ret)
        {
            #region проверка значений
            //------------------------------------------------------------------
            ret = Utils.InitReturns();
            if (finder.nzp_user < 1)
            {
                ret = new Returns(false, "Не определен пользователь", -1);
                return null;
            }
            //------------------------------------------------------------------
            #endregion

            #region подключение к web + проверка, что данные были выбраны
            //------------------------------------------------------------------
            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result) return null;

            string tXX_mess = "t" + Convert.ToString(finder.nzp_user) + "_mess";

            if (!TableInWebCashe(conn_web, tXX_mess))
            {
                conn_web.Close();
                ret = new Returns(false, "Данные не были выбраны", -22);
                return null;
            }
            //------------------------------------------------------------------
            #endregion

            #region подсчитать общее количество записей
            //------------------------------------------------------------------
            int total = 0;
            object count;

            try
            {
#if PG
count = ExecScalar(conn_web, " Select coalesce(count(*), 0) From " + tXX_mess, out ret, true);
#else
count = ExecScalar(conn_web, " Select nvl(count(*), 0) From " + tXX_mess, out ret, true);
#endif
                if (!ret.result) throw new Exception(ret.text);
                total = Convert.ToInt32(count);
            }
            catch (Exception ex)
            {
                conn_web.Close();
                ret = new Returns(false, ex.Message);
                return null;
            }
            //-------------------------------------------------
            #endregion

            #region подключиться к базе в кэше
            //--------------------------------------------------------------------            
#if PG
            string tXX_mess_full = "public." + tXX_mess;
#else
            string tXX_mess_full = conn_web.Database + "@" + DBManager.getServer(conn_web) + ":" + tXX_mess;
#endif

            string connectionString = Points.GetConnByPref(finder.pref);
            IDbConnection conn_db = GetConnection(connectionString);
            ret = OpenDb(conn_db, true);

            if (!ret.result)
            {
                conn_web.Close();
                return null;
            }
            //--------------------------------------------------------------------            
            #endregion

            #region получить список сообщений
            //------------------------------------------------------------------
            IDataReader reader = null;
            List<SmsMessage> Spis = new List<SmsMessage>();

            string sql = "Select ";
#if PG
#else
            if (finder.skip > 0) sql += " skip " + finder.skip;
            if (finder.rows > 0) sql += " first " + finder.rows;
#endif

#if PG
            sql += " * From " + tXX_mess + " s Order by s.created_on desc, s.sended_on desc, s.receiver, s.mobile_phone ";
#else
            sql += " * From " + tXX_mess + " s Order by s.created_on desc, s.sended_on desc, s.receiver, s.mobile_phone ";
#endif

#if PG
            if (finder.skip > 0) sql += " offset " + finder.skip;
            if (finder.rows > 0) sql += " limit " + finder.rows;            
#endif
            ret = ExecRead(conn_web, out reader, sql, true);
            if (!ret.result)
            {
                reader.Close();
                conn_web.Close();
                conn_db.Close();
                return null;
            }

            int i = 0;
            while (reader.Read())
            {
                i = i + 1;
                SmsMessage zap = new SmsMessage();
                zap.num = (i + finder.skip).ToString();

                if (reader["message_id"] != DBNull.Value) zap.message_id = Convert.ToInt32(reader["message_id"]);
                if (reader["receiver"] != DBNull.Value) zap.receiver = ((string)reader["receiver"]).Trim();
                if (reader["mobile_phone"] != DBNull.Value) zap.mobile_phone = ((string)reader["mobile_phone"]).Trim();
                if (reader["supg_message_type_id"] != DBNull.Value) zap.supg_message_type_id = Convert.ToInt16(reader["supg_message_type_id"]);
                if (reader["sms_text"] != DBNull.Value) zap.sms_text = ((string)reader["sms_text"]).Trim();
                if (reader["message_status"] != DBNull.Value) zap.message_status = ((string)reader["message_status"]).Trim();
                if (reader["message_status_id"] != DBNull.Value) zap.message_status_id = Convert.ToInt32(reader["message_status_id"]);
                if (reader["sended_on"] != DBNull.Value) zap.sended_on = ((DateTime)reader["sended_on"]).ToString();
                if (reader["sender"] != DBNull.Value) zap.sender = ((string)reader["sender"]).Trim();
                if (reader["created_on"] != DBNull.Value) zap.created_on = ((DateTime)reader["created_on"]).ToString();
                if (reader["creator"] != DBNull.Value) zap.creator = ((string)reader["creator"]).Trim();

                zap.creator = zap.created_on + ", " + zap.creator;
                
                if (reader["sms_id"] != DBNull.Value) zap.sms_id = Convert.ToInt32(reader["sms_id"]);
                if (reader["status_message"] != DBNull.Value) zap.status_message = ((string)reader["status_message"]).Trim();
                
                zap.mobile_phone_show = "+7" + zap.mobile_phone;
                
                if (zap.supg_message_type_id == 1) zap.message_type = "Авария";
                else zap.message_type = "Устранение аварии";
                
                if (zap.sms_id > 0) zap.message_status += " (№ " + zap.sms_id + ")";

                Spis.Add(zap);

                if (i >= finder.rows) break;
            }
            //------------------------------------------------------------------
            #endregion

            reader.Close();
            conn_web.Close();
            conn_db.Close();
            ret.tag = total;
            return Spis;
        }

        /// <summary>
        /// Получить состояния сообщений
        /// </summary>
        public List<SmsMessageStatus> GetSmsMessageStatus(out Returns ret)
        {
            ret = Utils.InitReturns();

            string conn_kernel = Points.GetConnByPref(Points.Pref);
            IDbConnection conn_db = GetConnection(conn_kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return null;

#if PG
string sql = " select * from " + Points.Pref + "_msg.s_message_status ";
#else
            string sql = " select * from " + Points.Pref + "_msg:s_message_status ";
#endif
            IDataReader reader;
            List<SmsMessageStatus> Spis = new List<SmsMessageStatus>();

            ret = ExecRead(conn_db, out reader, sql, true);
            if (!ret.result)
            {
                reader.Close();
                conn_db.Close();
                return null;
            }

            while (reader.Read())
            {
                SmsMessageStatus zap = new SmsMessageStatus();

                if (reader["message_status_id"] != DBNull.Value) zap.message_status_id = Convert.ToInt32(reader["message_status_id"]);
                if (reader["message_status"] != DBNull.Value) zap.message_status = ((string)reader["message_status"]).Trim();
                Spis.Add(zap);
            }

            conn_db.Close(); //закрыть соединение с основной базой
            reader.Close();
            return Spis;
        }

        /// <summary>
        /// Получить получателей
        /// </summary>
        public List<SmsReceiver> GetSmsReceiver(out Returns ret)
        {
            ret = Utils.InitReturns();

            string conn_kernel = Points.GetConnByPref(Points.Pref);
            IDbConnection conn_db = GetConnection(conn_kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return null;
#if PG
            string sql = " select * from " + Points.Pref + "_supg._s_receiver r order by r.receiver, r.mobile_phone ";
#else
            string sql = " select * from " + Points.Pref + "_msg:s_receiver r order by r.receiver, r.mobile_phone ";
#endif
            IDataReader reader;
            List<SmsReceiver> Spis = new List<SmsReceiver>();

            ret = ExecRead(conn_db, out reader, sql, true);
            if (!ret.result)
            {
                reader.Close();
                conn_db.Close();
                return null;
            }

            int i = 0;

            while (reader.Read())
            {
                i++;
                SmsReceiver zap = new SmsReceiver();
                zap.num = i.ToString();
                if (reader["receiver_id"] != DBNull.Value) zap.receiver_id = Convert.ToInt32(reader["receiver_id"]);
                if (reader["receiver"] != DBNull.Value) zap.receiver = ((string)reader["receiver"]).Trim();
                if (reader["mobile_phone"] != DBNull.Value) zap.mobile_phone = ((string)reader["mobile_phone"]).Trim();
                if (reader["post"] != DBNull.Value) zap.post = ((string)reader["post"]).Trim();
                zap.mobile_phone_show = "+7" + zap.mobile_phone;
                Spis.Add(zap);
            }

            conn_db.Close(); //закрыть соединение с основной базой
            reader.Close();
            return Spis;
        }

        /// <summary>
        /// Отправить сообщение
        /// </summary>
        public Returns SendSmsMessage(SmsMessage finder, List<SmsReceiver> list)
        {
            #region проверка значений
            //-----------------------------------------------------------------------
            // проверка наличия пользователя
            if (finder.nzp_user < 1) return new Returns(false, "Не определен пользователь", -1);
            // проверка списка получателей
            if (list == null || (list != null && list.Count == 0)) return new Returns(false, "Не выбраны получатели сообщения");
            //-----------------------------------------------------------------------
            #endregion

            #region установить соединение
            //-----------------------------------------------------------------------
            string conn_kernel = Points.GetConnByPref(Points.Pref);
            IDbConnection conn_db = GetConnection(conn_kernel);

            Returns ret = OpenDb(conn_db, true);
            if (!ret.result) return ret;
            //-----------------------------------------------------------------------
            #endregion

            #region получить код пользователя
            //----------------------------------------------------------------------------
            DbWorkUser dbU = new DbWorkUser();
            int local_user = dbU.GetSupgUser(conn_db, null, new Finder() { nzp_user = finder.nzp_user }, out ret);
            if (!ret.result)
            {
                MonitorLog.WriteLog("Ошибка определения локального пользователя :" + ret.text, MonitorLog.typelog.Error, true);
                return ret;
            }
            dbU.Close();
            //----------------------------------------------------------------------------
            #endregion

            IDbTransaction transaction = null;
                
            try
            {
                // начать транзакцию
                try { transaction = conn_db.BeginTransaction(); }
                catch { transaction = null; }

                string sql = "";

                finder.sms_text = finder.sms_text.Replace("\n", "");
                finder.sms_text = finder.sms_text.Replace("\r", "");

                for (int i = 0; i < list.Count; i++)
                {
#if PG
 sql = " Insert into " + Points.Pref + "_msg.message_outbound " +
                        " (receiver_id, message_type_id, supg_message_type_id, outbound_address, message_text, created_on, created_by, message_status_id, sended_by, scheduled_on, valid_thru) " +
                      " Select" +
                        " r.receiver_id, 1, " + finder.supg_message_type_id + ", r.mobile_phone, " + Utils.EStrNull(finder.sms_text) + "," +
                        " now(), " + local_user + ", 210, " + local_user + ", now(), now() " +
                      " From " + Points.Pref + "_msg.s_receiver r " +
                      " Where r.receiver_id = " + list[i].receiver_id;
#else
                    sql = " Insert into " + Points.Pref + "_msg:message_outbound " +
                                           " (receiver_id, message_type_id, supg_message_type_id, outbound_address, message_text, created_on, created_by, message_status_id, sended_by, scheduled_on, valid_thru) " +
                                         " Select" +
                                           " r.receiver_id, 1, " + finder.supg_message_type_id + ", r.mobile_phone, " + Utils.EStrNull(finder.sms_text) + "," +
                                           " current, " + local_user + ", 210, " + local_user + ", current, current " +
                                         " From " + Points.Pref + "_msg:s_receiver r " +
                                         " Where r.receiver_id = " + list[i].receiver_id;
#endif
                    ret = ExecSQL(conn_db, transaction, sql, true);
                    if (!ret.result) throw new Exception(ret.text);
                }
            }
            catch (Exception ex)
            {
                if (transaction != null) transaction.Rollback();
                conn_db.Close();
                return new Returns(false, ex.Message);
            }

            // завершить транзакцию
            if (transaction != null) transaction.Commit();
            conn_db.Close();

            return ret;
        }

        /// <summary>
        /// Сохранить получателя
        /// </summary>
        public Returns SaveSmsReceiver(SmsReceiver finder)
        {
            #region установить соединение
            //-----------------------------------------------------------------------
            string conn_kernel = Points.GetConnByPref(Points.Pref);
            IDbConnection conn_db = GetConnection(conn_kernel);

            Returns ret = OpenDb(conn_db, true);
            if (!ret.result) return ret;
            //-----------------------------------------------------------------------
            #endregion

            string sql = "";

            if (finder.receiver_id > 0)
            {
#if PG
 sql = "Update " + Points.Pref + "_supg._s_receiver Set " +
                        " receiver = " + Utils.EStrNull(finder.receiver) + ", " +
                        " mobile_phone = " + Utils.EStrNull(finder.mobile_phone) + ", " +
                        " post = " + Utils.EStrNull(finder.post) +
                    " Where receiver_id = " + finder.receiver_id; 
#else
                sql = "Update " + Points.Pref + "_msg:s_receiver Set " +
                                       " receiver = " + Utils.EStrNull(finder.receiver) + ", " +
                                       " mobile_phone = " + Utils.EStrNull(finder.mobile_phone) + ", " +
                                       " post = " + Utils.EStrNull(finder.post) +
                                   " Where receiver_id = " + finder.receiver_id;
#endif
            }
            else 
            {
#if PG
   sql = "Insert into " + Points.Pref + "_supg._s_receiver (receiver, mobile_phone, post) values(" +
                    Utils.EStrNull(finder.receiver) + ", " +
                    Utils.EStrNull(finder.mobile_phone) + ", " +
                    Utils.EStrNull(finder.post) + ")";
#else
                sql = "Insert into " + Points.Pref + "_msg:s_receiver (receiver, mobile_phone, post) values(" +
                                 Utils.EStrNull(finder.receiver) + ", " +
                                 Utils.EStrNull(finder.mobile_phone) + ", " +
                                 Utils.EStrNull(finder.post) + ")";
#endif
            }

            IDbTransaction transaction;
            try { transaction = conn_db.BeginTransaction(); }
            catch { transaction = null; }

            ret = ExecSQL(conn_db, transaction, sql, true);
            if (!ret.result)
            {
                if (transaction != null) transaction.Rollback();
                conn_db.Close();
                return ret;
            }

            if (finder.receiver_id == 0) finder.receiver_id = GetSerialValue(conn_db, transaction);

            if (transaction != null) transaction.Commit();
            conn_db.Close();
            
            // передать код отправителя
            ret.tag = finder.receiver_id;
            
            return ret;
        }

        /// <summary>
        /// Удалить получателя
        /// </summary>
        public Returns DeleteSmsReceiver(SmsReceiver finder)
        {
            #region установить соединение
            //-----------------------------------------------------------------------
            string conn_kernel = Points.GetConnByPref(Points.Pref);
            IDbConnection conn_db = GetConnection(conn_kernel);

            Returns ret = OpenDb(conn_db, true);
            if (!ret.result) return ret;
            //-----------------------------------------------------------------------
            #endregion

            #region подсчитать количество сообщений для данного получателя
            //-----------------------------------------------------------------------
#if PG
string sql = "Select count(*) From " + Points.Pref + "_supg._message_outbound s" + 
                " where s.receiver_id = " + finder.receiver_id;
#else
            string sql = "Select count(*) From " + Points.Pref + "_msg:message_outbound s" +
                            " where s.receiver_id = " + finder.receiver_id;
#endif
            Object obj = ExecScalar(conn_db, sql, out ret, true);
            if (!ret.result)
            {
                conn_db.Close();
                return ret;
            }

            int cnt = 0;
            try
            { cnt = Convert.ToInt32(obj); }
            catch
            { cnt = 0; }

            if (cnt > 0)
            {
                conn_db.Close();
                return new Returns(false, "Выбранный получатель не может быть удален, т.к. найдены сообщения, отправленные данному получателю", -1);
            }
            //-----------------------------------------------------------------------
            #endregion

            #region удалить получателя
            //-----------------------------------------------------------------------
#if PG
 sql = "Delete From " + Points.Pref + "_supg._s_receiver " + " Where receiver_id = " + finder.receiver_id;
#else
            sql = "Delete From " + Points.Pref + "_msg:s_receiver " + " Where receiver_id = " + finder.receiver_id;
#endif
            IDbTransaction transaction;
            try { transaction = conn_db.BeginTransaction(); }
            catch { transaction = null; }

            ret = ExecSQL(conn_db, transaction, sql, true);
            if (!ret.result)
            {
                if (transaction != null) transaction.Rollback();
                conn_db.Close();
                return ret;
            }

            if (transaction != null) transaction.Commit();
            conn_db.Close();

            // передать код отправителя
            ret.tag = finder.receiver_id;

            return ret;
            //-----------------------------------------------------------------------
            #endregion
        }

        /// <summary>
        /// Повторно отправить сообщения
        /// </summary>
        public Returns ResendSmsMessage(SmsMessage finder, List<SmsMessage> list)
        {
            #region проверка значений
            //-----------------------------------------------------------------------
            // проверка наличия пользователя
            if (finder.nzp_user < 1) return new Returns(false, "Не определен пользователь", -1);
            // проверка списка сообщений
            if (list == null || (list != null && list.Count == 0)) return new Returns(false, "Нет сообщений для отправки");
            //-----------------------------------------------------------------------
            #endregion

            #region установить соединение
            //-----------------------------------------------------------------------
            string conn_kernel = Points.GetConnByPref(Points.Pref);
            IDbConnection conn_db = GetConnection(conn_kernel);

            Returns ret = OpenDb(conn_db, true);
            if (!ret.result) return ret;
            //-----------------------------------------------------------------------
            #endregion

            #region получить код пользователя
            //----------------------------------------------------------------------------
            DbWorkUser dbU = new DbWorkUser();
            int local_user = dbU.GetSupgUser(conn_db, null, new Finder() { nzp_user = finder.nzp_user }, out ret);
            if (!ret.result)
            {
                MonitorLog.WriteLog("Ошибка определения локального пользователя :" + ret.text, MonitorLog.typelog.Error, true);
                return ret;
            }
            dbU.Close();
            //----------------------------------------------------------------------------
            #endregion

            #region сохранить данные
            //----------------------------------------------------------------------------
            IDbTransaction transaction = null;
            string sql = ""; 
                
            try
            {
                // начать транзакцию
                try { transaction = conn_db.BeginTransaction(); }
                catch { transaction = null; }

                for (int i = 0; i < list.Count; i++)
                {
#if PG
sql = "Update " + Points.Pref + "_msg.message_outbound Set " + 
                            "message_status_id = 210, " +
                            "sended_on = null, " +
                            "sended_by = " + local_user + ", " + 
                            "scheduled_on = now(), " +
                            "valid_thru = now() " +
                        "Where message_id = " + list[i].message_id + 
                            " and message_status_id <> 210";
#else
                    sql = "Update " + Points.Pref + "_msg:message_outbound Set " +
                                                "message_status_id = 210, " +
                                                "sended_on = null, " +
                                                "sended_by = " + local_user + ", " +
                                                "scheduled_on = current, " +
                                                "valid_thru = current " +
                                            "Where message_id = " + list[i].message_id +
                                                " and message_status_id <> 210";
#endif

                    ret = ExecSQL(conn_db, transaction, sql, true);
                    if (!ret.result) throw new Exception(ret.text);
                }
            }
            catch (Exception ex)
            {
                if (transaction != null) transaction.Rollback();
                conn_db.Close();
                return new Returns(false, ex.Message);
            }

            // завершить транзакцию
            if (transaction != null) transaction.Commit();
            conn_db.Close();

            return ret;
            //----------------------------------------------------------------------------
            #endregion
        }

        /// <summary>
        /// Удалить сообщения
        /// </summary>
        public Returns DeleteSmsMessage(List<SmsMessage> list)
        {
            #region проверка значений
            //-----------------------------------------------------------------------
            // проверка списка сообщений
            if (list == null || (list != null && list.Count == 0)) return new Returns(false, "Нет сообщений для удаления");
            //-----------------------------------------------------------------------
            #endregion

            #region установить соединение
            //-----------------------------------------------------------------------
            string conn_kernel = Points.GetConnByPref(Points.Pref);
            IDbConnection conn_db = GetConnection(conn_kernel);

            Returns ret = OpenDb(conn_db, true);
            if (!ret.result) return ret;
            //-----------------------------------------------------------------------
            #endregion

            #region удалить сообщения
            //----------------------------------------------------------------------------
            IDbTransaction transaction = null;
            string sql = "";

            try
            {
                // начать транзакцию
                try { transaction = conn_db.BeginTransaction(); }
                catch { transaction = null; }

                for (int i = 0; i < list.Count; i++)
                {
#if PG
sql = "Delete From " + Points.Pref + "_msg.message_outbound " +
                          "Where message_id = " + list[i].message_id +
                            " and (message_status_id <> 210 or sended_on is not null)";
#else
                    sql = "Delete From " + Points.Pref + "_msg:message_outbound " +
                                              "Where message_id = " + list[i].message_id +
                                                " and (message_status_id <> 210 or sended_on is not null)";
#endif

                    ret = ExecSQL(conn_db, transaction, sql, true);
                    if (!ret.result) throw new Exception(ret.text);
                }
            }
            catch (Exception ex)
            {
                if (transaction != null) transaction.Rollback();
                conn_db.Close();
                return new Returns(false, ex.Message);
            }

            // завершить транзакцию
            if (transaction != null) transaction.Commit();
            conn_db.Close();

            return ret;
            //----------------------------------------------------------------------------
            #endregion
        }
    }
}