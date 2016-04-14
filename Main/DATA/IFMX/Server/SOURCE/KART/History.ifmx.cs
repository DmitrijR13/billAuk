using System;
using System.Collections.Generic;
using System.Data;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;
using System.Text;
using System.IO;
using System.Data.OleDb;
using System.Threading;
using SevenZip;
using System.Data.Odbc;
using System.Linq;
using Bars.KP50.Utils;

namespace STCLINE.KP50.DataBase
{
    public partial class DbAdmin : DbAdminClient
    {
        public List<SysEvents> GetSysEvents(SysEvents finder, out Returns ret)
        {
            IDbConnection conn_db = null;
            IDataReader reader;
            int recordsTotalCount = 0;
            var spis = new List<SysEvents>();

            try
            {
                #region соединение с БД
                conn_db = GetConnection(Constants.cons_Kernel);
                ret = OpenDb(conn_db, true);
                if (!ret.result)
                {
                    ret = new Returns(false, "Ошибка при открытии соединения с БД");
                    MonitorLog.WriteLog("Ошибка GetSysEvents Ошибка при открытии соединения с БД", MonitorLog.typelog.Error, 20, 201, true);
                    conn_db.Close();
                    return null;
                }
                #endregion

#if PG
                string data_type = " timestamp ";
                string user = " \"user\" ";
#else
                const string data_type = " datetime year to fraction(3) ";
                const string user = " user ";
#endif

                string where = ""; //дополнительные ограничения
                string nzp_where = ""; //ограничение по конкретному объекту
                string sql = "";

                #region формирование ограничений по входящему фильтру
                //выбрана дата
                if (finder.from_date != null)
                {
                    var date = Convert.ToDateTime(finder.from_date).ToString("u");
                    where += " and se.date_ >= '" + date.Substring(0, date.Length - 1) + ".0' ";
                }
                if (finder.to_date != null)
                {
                    var date = Convert.ToDateTime(finder.to_date).ToString("u");
                    where += " and se.date_ <= '" + date.Substring(0, date.Length - 1) + ".0' ";
                }

                //выбраны пользователи
                if (finder.users_list != null)
                {
                    where += " and trim((case when u.comment is not null then u.comment else '' end)) || '(' || trim(u.name) || ')' in (";
                    for (int i = 0; i < finder.users_list.Count; i++)
                    {
                        where += "'" + finder.users_list[i] + "',";
                    }
                    where = where.Substring(0, where.Length - 1) + ") ";
                }

                //выбраны события
                if (finder.events_list != null)
                {
                    where += " and se.nzp_dict_event in (";
                    for (int i = 0; i < finder.events_list.Count; i++)
                    {
                        where += finder.events_list[i] + ",";
                    }
                    where = where.Substring(0, where.Length - 1) + ") ";
                }

                //выбраны сущности
                if (finder.entity_list != null)
                {
                    where += " and ot.id in (";
                    for (int i = 0; i < finder.entity_list.Count; i++)
                    {
                        where += finder.entity_list[i] + ",";
                    }
                    where = where.Substring(0, where.Length - 1) + ") ";
                }

                //выбран конкретный объект
                if (finder.nzp != 0)
                {
                    nzp_where += " and se.nzp = " + finder.nzp;
                }

                #endregion формирование ограничений по входящему фильтру

                //скидываем темповую таблицу, если она существует
                ExecSQL(conn_db, "drop table sys_events_tmp;", false);
                //создание темповой таблицы
                ret = ExecSQL(conn_db, "create temp table sys_events_tmp (nzp_event integer, date_ " + data_type + ", nzp_user integer, nzp_dict_event integer, nzp integer, note character(200), " +
                    " event_name character(200), " + user + " char(200), entity_id character(20), entity character(25)) " + sUnlogTempTable);

                #region получение информации только по одному банку, если он был указан
                if (finder.bank != "")
                {
                    //получение основной инфы
                    sql = " insert into sys_events_tmp (nzp_event, date_, nzp_user, nzp_dict_event, nzp, note, event_name, " + user + ", entity_id, entity) select se.*, sdv.name as event_name, trim((case when u.comment is not null then u.comment else '' end)) || '(' || trim(u.name) || ')' as user, sdv.code as entity_id, ot.type_name as entity " +
                            " from " + finder.bank + tableDelimiter + "sys_events se " +
                               " join " + Points.Pref + "_data" + tableDelimiter + "sys_dictionary_values sdv " +
                                    " left join " + Points.Pref + "_data" + tableDelimiter + "object_types ot on cast(sdv.code as integer) = ot.id " +
                               " on se.nzp_dict_event = sdv.nzp_dict " +
                               " left join " + finder.bank + tableDelimiter + "users u on se.nzp_user = u.nzp_user  " +
                           " where 1=1 " + where + nzp_where;
                    ret = ExecSQL(conn_db, sql);

                    #region Если компонент работает как история событий ЛС + его ИПУ, то добавляем выборку по ИПУ
                    if (finder.mode == 1)
                    {
                        //собираем счетчики
                        sql = " insert into sys_events_tmp (nzp_event, date_, nzp_user, nzp_dict_event, nzp, note, event_name, " + user + ", entity_id, entity) select se.*, sdv.name as event_name, trim((case when u.comment is not null then u.comment else '' end)) || '(' || trim(u.name) || ')' as user, sdv.code as entity_id, ot.type_name as entity " +
                                " from " + finder.bank + tableDelimiter + "sys_events se " +
                                " join " + finder.bank + tableDelimiter + "counters_spis cs on se.nzp = cs.nzp_counter and cs.nzp_type = 3 " +
                                " join " + Points.Pref + "_data" + tableDelimiter + "sys_dictionary_values sdv " +
                                    " left join " + Points.Pref + "_data" + tableDelimiter + "object_types ot on cast(sdv.code as integer) = ot.id " +
                                " on se.nzp_dict_event = sdv.nzp_dict  " +
                                " left join " + finder.bank + tableDelimiter + "users u on se.nzp_user = u.nzp_user   " +
                            " where cs.nzp = " + finder.nzp + where;
                        ret = ExecSQL(conn_db, sql);

                        //собираем жильцов
                        sql = " insert into sys_events_tmp (nzp_event, date_, nzp_user, nzp_dict_event, nzp, note, event_name, " + user + ", entity_id, entity) select se.*, sdv.name as event_name, trim((case when u.comment is not null then u.comment else '' end)) || '(' || trim(u.name) || ')' as user, sdv.code as entity_id, ot.type_name as entity " +
                               " from " + finder.bank + tableDelimiter + "sys_events se " +
                               " join " + finder.bank + tableDelimiter + "kart k on se.nzp = k.nzp_gil and k.isactual = '1' " +
                               " join " + Points.Pref + "_data" + tableDelimiter + "sys_dictionary_values sdv " +
                                   " left join " + Points.Pref + "_data" + tableDelimiter + "object_types ot on cast(sdv.code as integer) = ot.id " +
                               " on se.nzp_dict_event = sdv.nzp_dict  " +
                               " left join " + finder.bank + tableDelimiter + "users u on se.nzp_user = u.nzp_user " +
                           " where k.nzp_kvar = " + finder.nzp + where;
                        ret = ExecSQL(conn_db, sql);

                        // собираем договоры
                        sql = " insert into sys_events_tmp (nzp_event, date_, nzp_user, nzp_dict_event, nzp, note, event_name, " + user + ", entity_id, entity) select se.*, sdv.name as event_name, trim((case when u.comment is not null then u.comment else '' end)) || '(' || trim(u.name) || ')' as user, sdv.code as entity_id, ot.type_name as entity " +
                              " from " + Points.Pref + "_data" + tableDelimiter + "sys_events se " +
                              " join " + Points.Pref + "_data" + tableDelimiter + "fn_lsdogovor dog on se.nzp = dog.nzp_con " +
                              " join " + Points.Pref + "_data" + tableDelimiter + "sys_dictionary_values sdv " +
                                  " left join " + Points.Pref + "_data" + tableDelimiter + "object_types ot on cast(sdv.code as integer) = ot.id " +
                              " on se.nzp_dict_event = sdv.nzp_dict  " +
                              " left join " + Points.Pref + "_data" + tableDelimiter + "users u on se.nzp_user = u.nzp_user " +
                          " where dog.nzp_kvar = " + finder.nzp + where;
                        ret = ExecSQL(conn_db, sql);
                    }
                    #endregion

                    sql = "select * from sys_events_tmp order by date_ desc";
                    ret = ExecRead(conn_db, out reader, sql, true);
                }
                #endregion получение информации только по одному банку, если он был указан

                #region получение информации по всем банкам, если конкретный банк указан не был
                else
                {
                    //Получаем список финансовых банков
                    List<string> fin_bank_list = new List<string>();
                    sql = "select dbname from " + Points.Pref + "_kernel" + tableDelimiter + "s_baselist where idtype = 4";
                    ret = ExecRead(conn_db, out reader, sql, true);
                    while (reader.Read())
                    {
                        fin_bank_list.Add(reader["dbname"].ToString());
                    }

                    //сохранение в темповую таблицу инфы по верхнему банку
                    sql = " insert into sys_events_tmp (nzp_event, date_, nzp_user, nzp_dict_event, nzp, note, event_name, " + user + ", entity_id, entity) select se.*, sdv.name as event_name, trim((case when u.comment is not null then u.comment else '' end)) || '(' || trim(u.name) || ')' as user, sdv.code as entity_id, ot.type_name as entity " +
                            " from " + Points.Pref + "_data" + tableDelimiter + "sys_events se " +
                                " join " + Points.Pref + "_data" + tableDelimiter + "sys_dictionary_values sdv " +
                                        " left join " + Points.Pref + "_data" + tableDelimiter + "object_types ot on cast(sdv.code as integer) = ot.id " +
                                " on se.nzp_dict_event = sdv.nzp_dict " +
                                " left join " + Points.Pref + "_data" + tableDelimiter + "users u on se.nzp_user = u.nzp_user " +
                            " where 1=1 " + where + nzp_where;
                    ret = ExecSQL(conn_db, sql);

                    //получение инфы с фин. банков
                    foreach (var bank in fin_bank_list)
                    {
                        #region проверка на наличие таблицы sys_events
                        try
                        {
#if PG
                            var tab = ClassDBUtils.OpenSQL(" select * from " + bank + tableDelimiter + "sys_events limit 1 ", conn_db);
#else
                            var tab = ClassDBUtils.OpenSQL(" select first 1 * from " + bank + tableDelimiter + "sys_events ", conn_db);
#endif
                            if (tab.resultData.Rows.Count == 0)
                                continue;
                        }
                        catch
                        {
                            continue;
                        }
                        #endregion

                        sql = " insert into sys_events_tmp (nzp_event, date_, nzp_user, nzp_dict_event, nzp, note, event_name, " + user + ", entity_id, entity) select se.*, sdv.name as event_name, trim((case when u.comment is not null then u.comment else '' end)) || '(' || trim(u.name) || ')' as user, sdv.code as entity_id, ot.type_name as entity " +
                                    " from " + bank + tableDelimiter + "sys_events se  " +
                                    " join " + Points.Pref + "_data" + tableDelimiter + "sys_dictionary_values sdv " +
                                        " left join " + Points.Pref + "_data" + tableDelimiter + "object_types ot on cast(sdv.code as integer) = ot.id " +
                                    " on se.nzp_dict_event = sdv.nzp_dict  " +
                                    " left join " + Points.Pref + "_data" + tableDelimiter + "users u on se.nzp_user = u.nzp_user   " +
                                " where 1=1 " + where + nzp_where;
                        ret = ExecSQL(conn_db, sql);
                    }

                    //получение инфы по нижним банкам
                    foreach (var bank in Points.PointList)
                    {
                        #region проверка на наличие таблицы sys_events
                        try
                        {
#if PG
                            var tab = ClassDBUtils.OpenSQL(" select * from " + bank.pref + "_data" + tableDelimiter + "sys_events limit 1 ", conn_db);
#else
                            var tab = ClassDBUtils.OpenSQL(" select first 1 * from " + bank.pref + "_data" + tableDelimiter + "sys_events ", conn_db);
#endif
                            if (tab.resultData.Rows.Count == 0)
                                continue;
                        }
                        catch
                        {
                            continue;
                        }
                        #endregion

                        sql = " insert into sys_events_tmp (nzp_event, date_, nzp_user, nzp_dict_event, nzp, note, event_name, " + user + ", entity_id, entity) select se.*, sdv.name as event_name, trim((case when u.comment is not null then u.comment else '' end)) || '(' || trim(u.name) || ')' as user, sdv.code as entity_id, ot.type_name as entity " +
                                    " from " + bank.pref + "_data" + tableDelimiter + "sys_events se  " +
                                    " join " + Points.Pref + "_data" + tableDelimiter + "sys_dictionary_values sdv " +
                                        " left join " + Points.Pref + "_data" + tableDelimiter + "object_types ot on cast(sdv.code as integer) = ot.id " +
                                    " on se.nzp_dict_event = sdv.nzp_dict  " +
                                    " left join " + bank.pref + "_data" + tableDelimiter + "users u on se.nzp_user = u.nzp_user   " +
                                " where 1=1 " + where + nzp_where;
                        ret = ExecSQL(conn_db, sql);
                        #region Если компонент работает как история событий ЛС + его ИПУ, то добавляем выборку по ИПУ
                        if (finder.mode == 1)
                        {
                            //собираем счетчики
                            sql = " insert into sys_events_tmp (nzp_event, date_, nzp_user, nzp_dict_event, nzp, note, event_name, " + user + ", entity_id, entity) select se.*, sdv.name as event_name, trim((case when u.comment is not null then u.comment else '' end)) || '(' || trim(u.name) || ')' as user, sdv.code as entity_id, ot.type_name as entity " +
                                    " from " + bank.pref + "_data" + tableDelimiter + "sys_events se " +
                                    " join " + bank.pref + "_data" + tableDelimiter + "counters_spis cs on se.nzp = cs.nzp_counter and cs.nzp_type = 3 " +
                                    " join " + Points.Pref + "_data" + tableDelimiter + "sys_dictionary_values sdv " +
                                        " left join " + Points.Pref + "_data" + tableDelimiter + "object_types ot on cast(sdv.code as integer) = ot.id " +
                                    " on se.nzp_dict_event = sdv.nzp_dict  " +
                                    " left join " + bank.pref + "_data" + tableDelimiter + "users u on se.nzp_user = u.nzp_user   " +
                                " where cs.nzp = " + finder.nzp + where;
                            ret = ExecSQL(conn_db, sql);

                            //собираем жильцов
                            sql = " insert into sys_events_tmp (nzp_event, date_, nzp_user, nzp_dict_event, nzp, note, event_name, " + user + ", entity_id, entity) select se.*, sdv.name as event_name, trim((case when u.comment is not null then u.comment else '' end)) || '(' || trim(u.name) || ')' as user, sdv.code as entity_id, ot.type_name as entity " +
                             " from " + bank.pref + tableDelimiter + "sys_events se " +
                             " join " + bank.pref + tableDelimiter + "kart k on se.nzp = k.nzp_gil and k.isactual = '1' " +
                             " join " + Points.Pref + "_data" + tableDelimiter + "sys_dictionary_values sdv " +
                                 " left join " + Points.Pref + "_data" + tableDelimiter + "object_types ot on cast(sdv.code as integer) = ot.id " +
                             " on se.nzp_dict_event = sdv.nzp_dict  " +
                             " left join " + bank.pref + tableDelimiter + "users u on se.nzp_user = u.nzp_user " +
                         " where k.nzp_kvar = " + finder.nzp + where;
                            ret = ExecSQL(conn_db, sql);

                            // собираем договоры
                            sql = " insert into sys_events_tmp (nzp_event, date_, nzp_user, nzp_dict_event, nzp, note, event_name, " + user + ", entity_id, entity) select se.*, sdv.name as event_name, trim((case when u.comment is not null then u.comment else '' end)) || '(' || trim(u.name) || ')' as user, sdv.code as entity_id, ot.type_name as entity " +
                             " from " + Points.Pref + "_data" + tableDelimiter + "sys_events se " +
                             " join " + Points.Pref + "_data" + tableDelimiter + "fn_lsdogovor dog on se.nzp = dog.nzp_con " +
                             " join " + Points.Pref + "_data" + tableDelimiter + "sys_dictionary_values sdv " +
                                 " left join " + Points.Pref + "_data" + tableDelimiter + "object_types ot on cast(sdv.code as integer) = ot.id " +
                             " on se.nzp_dict_event = sdv.nzp_dict  " +
                             " left join " + Points.Pref + "_data" + tableDelimiter + "users u on se.nzp_user = u.nzp_user " +
                         " where dog.nzp_kvar = " + finder.nzp + where;
                            ret = ExecSQL(conn_db, sql);
                        }
                        #endregion
                    }

                    sql = " select * from sys_events_tmp order by date_ desc ";
                    ret = ExecRead(conn_db, out reader, sql, true);
                }
                #endregion получение информации по всем банкам, если конкретный банк указан не был

                if (!ret.result)
                {
                    MonitorLog.WriteLog("Ошибка в функции GetSysEvents " + ret.text, MonitorLog.typelog.Error, 20, 201, true);
                    conn_db.Close();
                    return null;
                }

                //Определить общее количество записей
                recordsTotalCount = Convert.ToInt32(ExecScalar(conn_db, "select count(*) from sys_events_tmp", out ret, true));

                int k = 0;
                int count = 0;
                while (reader.Read())
                {
                    k++;
                    if (k < finder.skip) continue;

                    var zap = new SysEvents();
                    zap.num = k;
                    if (reader["nzp_event"] != DBNull.Value) zap.nzp_event = Convert.ToInt32(reader["nzp_event"]);
                    if (reader["date_"] != DBNull.Value) zap.date_ = Convert.ToDateTime(reader["date_"]);
                    if (reader["nzp_user"] != DBNull.Value) zap.nzp_user = Convert.ToInt32(reader["nzp_user"]);
                    if (reader["nzp_dict_event"] != DBNull.Value) zap.nzp_dict_event = Convert.ToInt32(reader["nzp_dict_event"]);
                    if (reader["nzp"] != DBNull.Value) zap.nzp = Convert.ToInt32(reader["nzp"]);
                    if (reader["note"] != DBNull.Value) zap.note = Convert.ToString(reader["note"]).Trim();
                    if (reader["user"] != DBNull.Value)
                        zap.user = Convert.ToString(reader["user"]).Trim();
                    else
                        zap.user = "Не определено";
                    zap.entity_name = reader["entity"] != DBNull.Value ? Convert.ToInt32(reader["nzp"]) != 0 ? Convert.ToString(reader["entity"]).Trim() + " " + Convert.ToString(reader["nzp"]).Trim() : Convert.ToString(reader["entity"]).Trim() : "";
                    zap.entity_id = reader["entity_id"] != DBNull.Value ? Convert.ToInt32(reader["entity_id"]) : 0;
                    if (reader["event_name"] != DBNull.Value) zap.event_name = Convert.ToString(reader["event_name"]).Trim();
                    spis.Add(zap);
                    count++;

                    if (count >= finder.rows) break;
                }

                ret.tag = recordsTotalCount;

                reader.Close();

                //скидываем темповую таблицу
                ClassDBUtils.ExecSQL("drop table sys_events_tmp;", conn_db);

                conn_db.Close();
                return spis;
            }
            catch (Exception ex)
            {
                conn_db.Close();
                ret = new Returns(false, ex.Message);
                MonitorLog.WriteLog("Ошибка выполнения GetSysEvents", MonitorLog.typelog.Error, 20, 201, true);
                return null;
            }
        }

        public List<SysEvents> GetSysEventsUsersList(SysEvents finder, out Returns ret)
        {
            IDbConnection conn_db = null;

            #region соединение с БД
            conn_db = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result)
            {
                ret = new Returns(false, "Ошибка при открытии соединения с БД");
                MonitorLog.WriteLog("Ошибка GetSysEventsUsersList Ошибка при открытии соединения с БД", MonitorLog.typelog.Error, 20, 201, true);
                conn_db.Close();
                return null;
            }
            #endregion

            string sql = "";

            List<SysEvents> spis = new List<SysEvents>();
            IDataReader reader;

            string nzp_where = "";
            //выбран конкретный объект
            if (finder.nzp != 0)
                nzp_where += " and se.nzp = " + finder.nzp;

            #region получение информации только по одному банку, если он был указан
            if (finder.bank != "")
            {
                if (finder.bank == "top")
                    finder.bank = Points.Pref + "_data";

                //скидываем темповую таблицу, если она существует
                ExecSQL(conn_db, "drop table sys_events_users_tmp;", false);
                //создание темповой таблицы
                ExecSQL(conn_db, "create temp table sys_events_users_tmp (uname varchar(200))" + sUnlogTempTable);

                sql = " insert into sys_events_users_tmp (uname) select distinct trim((case when u.comment is not null then u.comment else '' end)) || '(' || trim(u.name) || ')' as uname " +
                            " from " + finder.bank + tableDelimiter + "users u " +
                                  " join " + finder.bank + tableDelimiter + "sys_events se " +
                                        " join " + Points.Pref + "_data" + tableDelimiter + "sys_dictionary_values sdv on se.nzp_dict_event = sdv.nzp_dict " +
                                  " on u.nzp_user = se.nzp_user " + nzp_where;
                ret = ExecSQL(conn_db, sql);

                #region Если компонент работает как история событий ЛС + его ИПУ, то добавляем выборку по ИПУ
                if (finder.mode == 1)
                {
                    //собираем счетчики
                    sql = " insert into sys_events_users_tmp (uname)  select distinct trim((case when u.comment is not null then u.comment else '' end)) || '(' || trim(u.name) || ')' as uname " +
                        " from " + finder.bank + tableDelimiter + "users u " +
                        " join " + finder.bank + tableDelimiter + "sys_events se " +
                            " join " + Points.Pref + "_data" + tableDelimiter + "sys_dictionary_values sdv on se.nzp_dict_event = sdv.nzp_dict " +
                        " on u.nzp_user = se.nzp_user " +
                        " join " + finder.bank + tableDelimiter + "counters_spis cs on se.nzp = cs.nzp_counter and cs.nzp_type = 3 " +
                        " where cs.nzp = " + finder.nzp;
                    ret = ExecSQL(conn_db, sql);

                    //собираем жильцов
                    sql = " insert into sys_events_users_tmp (uname)  select distinct trim((case when u.comment is not null then u.comment else '' end)) || '(' || trim(u.name) || ')' as uname " +
                      " from " + finder.bank + tableDelimiter + "users u " +
                      " join " + finder.bank + tableDelimiter + "sys_events se " +
                            " join " + Points.Pref + "_data" + tableDelimiter + "sys_dictionary_values sdv on se.nzp_dict_event = sdv.nzp_dict " +
                      " on u.nzp_user = se.nzp_user " +
                      " join " + finder.bank + tableDelimiter + "kart k on se.nzp = k.nzp_gil and k.isactual = '1' " +
                      " where k.nzp_kvar = " + finder.nzp;
                    ret = ExecSQL(conn_db, sql);

                    // собираем договоры
                    sql = " insert into sys_events_users_tmp (uname)  select distinct trim((case when u.comment is not null then u.comment else '' end)) || '(' || trim(u.name) || ')' as uname " +
                      " from " + Points.Pref + "_data" + tableDelimiter + "users u " +
                      " join " + Points.Pref + "_data" + tableDelimiter + "sys_events se " +
                            " join " + Points.Pref + "_data" + tableDelimiter + "sys_dictionary_values sdv on se.nzp_dict_event = sdv.nzp_dict " +
                      " on u.nzp_user = se.nzp_user " +
                      " join " + Points.Pref + "_data" + tableDelimiter + "fn_lsdogovor dog on se.nzp = dog.nzp_con " +
                      " where dog.nzp_kvar = " + finder.nzp;
                    ret = ExecSQL(conn_db, sql);
                }
                #endregion
            }
            #endregion получение информации только по одному банку, если он был указан

            #region получение информации по всем банкам, если конкретный банк указан не был
            else
            {
                #region Получаем список финансовых банков
                List<string> fin_bank_list = new List<string>();
                sql = "select dbname from " + Points.Pref + "_kernel" + tableDelimiter + "s_baselist where idtype = 4";
                ret = ExecRead(conn_db, out reader, sql, true);
                while (reader.Read())
                {
                    fin_bank_list.Add(reader["dbname"].ToString());
                }
                #endregion

                //скидываем темповую таблицу, если она существует
                ExecSQL(conn_db, "drop table sys_events_users_tmp;", false);
                //создание темповой таблицы
                ExecSQL(conn_db, "create temp table sys_events_users_tmp (uname varchar(200))" + sUnlogTempTable);

                //получаем список пользователей из верхнего банка
                sql = " insert into sys_events_users_tmp (uname)  select distinct trim((case when u.comment is not null then u.comment else '' end)) || '(' || trim(u.name) || ')' as uname " +
                      " from " + Points.Pref + "_data" + tableDelimiter + "users u " +
                      " join " + Points.Pref + "_data" + tableDelimiter + "sys_events se " +
                            " join " + Points.Pref + "_data" + tableDelimiter + "sys_dictionary_values sdv on se.nzp_dict_event = sdv.nzp_dict " +
                      " on u.nzp_user = se.nzp_user " + nzp_where;
                ret = ExecSQL(conn_db, sql);

                //получение инфы с фин. банков
                foreach (var bank in fin_bank_list)
                {
                    #region проверка на наличие таблицы sys_events
                    try
                    {
#if PG
                        var tab = ClassDBUtils.OpenSQL(" select * from " + bank + tableDelimiter + "sys_events limit 1 ", conn_db);
#else
                        var tab = ClassDBUtils.OpenSQL(" select first 1 * from " + bank + tableDelimiter + "sys_events ", conn_db);
#endif
                        if (tab.resultData.Rows.Count == 0)
                            continue;
                    }
                    catch (Exception ex)
                    {
                        continue;
                    }
                    #endregion

                    sql = " insert into sys_events_users_tmp (uname)  select distinct trim((case when u.comment is not null then u.comment else '' end)) || '(' || trim(u.name) || ')' as uname " +
                      " from " + Points.Pref + "_data" + tableDelimiter + "users u " +
                      " join " + bank + tableDelimiter + "sys_events se " +
                            " join " + Points.Pref + "_data" + tableDelimiter + "sys_dictionary_values sdv on se.nzp_dict_event = sdv.nzp_dict " +
                      " on u.nzp_user = se.nzp_user " + nzp_where;
                    ret = ExecSQL(conn_db, sql);
                }

                //получение пользователй из нижних банков
                foreach (var bank in Points.PointList)
                {
                    #region проверка на наличие таблицы sys_events
                    try
                    {
#if PG
                        var tab = ClassDBUtils.OpenSQL(" select * from " + bank.pref + "_data" + tableDelimiter + "sys_events limit 1 ", conn_db);
#else
                        var tab = ClassDBUtils.OpenSQL(" select first 1 * from " + bank.pref + "_data" + tableDelimiter + "sys_events ", conn_db);
#endif
                        if (tab.resultData.Rows.Count == 0)
                            continue;
                    }
                    catch (Exception ex)
                    {
                        continue;
                    }
                    #endregion

                    sql = " insert into sys_events_users_tmp (uname)  select distinct trim((case when u.comment is not null then u.comment else '' end)) || '(' || trim(u.name) || ')' as uname " +
                      " from " + bank.pref + "_data" + tableDelimiter + "users u " +
                      " join " + bank.pref + "_data" + tableDelimiter + "sys_events se " +
                            " join " + Points.Pref + "_data" + tableDelimiter + "sys_dictionary_values sdv on se.nzp_dict_event = sdv.nzp_dict " +
                      " on u.nzp_user = se.nzp_user " + nzp_where;
                    ret = ExecSQL(conn_db, sql);

                    #region Если компонент работает как история событий ЛС + его ИПУ, то добавляем выборку по ИПУ
                    if (finder.mode == 1)
                    {
                        //собираем счетчики
                        sql = " insert into sys_events_users_tmp (uname)  select distinct trim((case when u.comment is not null then u.comment else '' end)) || '(' || trim(u.name) || ')' as uname " +
                     " from " + bank.pref + "_data" + tableDelimiter + "users u " +
                     " join " + bank.pref + "_data" + tableDelimiter + "sys_events se " +
                            " join " + Points.Pref + "_data" + tableDelimiter + "sys_dictionary_values sdv on se.nzp_dict_event = sdv.nzp_dict " +
                     " on u.nzp_user = se.nzp_user " +
                     " join " + bank.pref + "_data" + tableDelimiter + "counters_spis cs on se.nzp = cs.nzp_counter and cs.nzp_type = 3 " +
                     " where cs.nzp = " + finder.nzp;
                        ret = ExecSQL(conn_db, sql);

                        //собираем жильцов
                        sql = " insert into sys_events_users_tmp (uname)  select distinct trim((case when u.comment is not null then u.comment else '' end)) || '(' || trim(u.name) || ')' as uname " +
                       " from " + bank.pref + "_data" + tableDelimiter + "users u " +
                       " join " + bank.pref + "_data" + tableDelimiter + "sys_events se " +
                            " join " + Points.Pref + "_data" + tableDelimiter + "sys_dictionary_values sdv on se.nzp_dict_event = sdv.nzp_dict " +
                       " on u.nzp_user = se.nzp_user " +
                       " join " + bank.pref + "_data" + tableDelimiter + "kart k on se.nzp = k.nzp_gil and k.isactual = '1' " +
                       " where k.nzp_kvar = " + finder.nzp;
                        ret = ExecSQL(conn_db, sql);

                        //собираем договоры
                        sql = " insert into sys_events_users_tmp (uname)  select distinct trim((case when u.comment is not null then u.comment else '' end)) || '(' || trim(u.name) || ')' as uname " +
                       " from " + Points.Pref + "_data" + tableDelimiter + "users u " +
                       " join " + Points.Pref + "_data" + tableDelimiter + "sys_events se " +
                            " join " + Points.Pref + "_data" + tableDelimiter + "sys_dictionary_values sdv on se.nzp_dict_event = sdv.nzp_dict " +
                       " on u.nzp_user = se.nzp_user " +
                      " join " + Points.Pref + "_data" + tableDelimiter + "fn_lsdogovor dog on se.nzp = dog.nzp_con " +
                       " where dog.nzp_kvar = " + finder.nzp;
                        ret = ExecSQL(conn_db, sql);
                    }
                    #endregion
                }
            }
            #endregion

            sql = " select distinct * from sys_events_users_tmp order by uname ";
            ret = ExecRead(conn_db, out reader, sql, true);

            if (!ret.result)
            {
                MonitorLog.WriteLog("Ошибка в функции GetSysEventsUsersList " + ret.text, MonitorLog.typelog.Error, 20, 201, true);
                conn_db.Close();
                return null;
            }
            try
            {
                while (reader.Read())
                {
                    var zap = new SysEvents();
                    if (reader["uname"] != DBNull.Value) zap.user = Convert.ToString(reader["uname"]).Trim();
                    spis.Add(zap);
                }

                reader.Close();

                ClassDBUtils.ExecSQL("drop table sys_events_users_tmp;", conn_db);

                conn_db.Close();
                return spis;
            }
            catch (Exception ex)
            {
                reader.Close();
                conn_db.Close();
                ret = new Returns(false, ex.Message);
                MonitorLog.WriteLog("Ошибка выполнения GetSysEventsUsersList", MonitorLog.typelog.Error, 20, 201, true);
                return null;
            }
        }

        //получить список названий событий, засветившихся в sys_events
        public List<SysEvents> GetSysEventsEventsList(SysEvents finder, out Returns ret)
        {
            IDbConnection conn_db = null;
            string sql = "";
            IDataReader reader;

            try
            {
                #region соединение с БД
                conn_db = GetConnection(Constants.cons_Kernel);
                ret = OpenDb(conn_db, true);
                if (!ret.result)
                {
                    ret = new Returns(false, "Ошибка при открытии соединения с БД");
                    MonitorLog.WriteLog("Ошибка GetSysEventsUsersList Ошибка при открытии соединения с БД", MonitorLog.typelog.Error, 20, 201, true);
                    conn_db.Close();
                    return null;
                }
                #endregion

                var spis = new List<SysEvents>();

                string where = "";
                string nzp_where = "";
                //выбраны сущности
                if (finder.entity_list != null)
                {
                    where += " and cast(sdv.code as integer) in (";
                    for (int i = 0; i < finder.entity_list.Count; i++)
                    {
                        where += finder.entity_list[i] + ",";
                    }
                    where = where.Substring(0, where.Length - 1) + ") ";
                }
                //выбраны пользователи
                if (finder.users_list != null)
                {
                    where += " and trim((case when u.comment is not null then u.comment else '' end)) || '(' || trim(u.name) || ')' in (";
                    for (int i = 0; i < finder.users_list.Count; i++)
                    {
                        where += "'" + finder.users_list[i] + "',";
                    }
                    where = where.Substring(0, where.Length - 1) + ") ";
                }
                //выбран конкретный объект
                if (finder.nzp != 0)
                {
                    nzp_where += " and se.nzp = " + finder.nzp;
                }

                //скидываем темповую таблицу, если она существует
                ExecSQL(conn_db, "drop table sys_events_events_tmp;", false);
                //создание темповой таблицы
                ExecSQL(conn_db, "create temp table sys_events_events_tmp (nzp_dict integer, name varchar(200), code varchar(200))" + sUnlogTempTable);

                #region получение информации только по одному банку, если он был указан
                if (finder.bank != "")
                {
                    if (finder.bank == "top")
                        finder.bank = Points.Pref + "_data";

                    sql = " insert into sys_events_events_tmp (nzp_dict, name, code)  select distinct sdv.nzp_dict, sdv.name, sdv.code " +
                            " from " + Points.Pref + "_data" + tableDelimiter + "sys_dictionary_values sdv " +
                                " join " + finder.bank + tableDelimiter + "sys_events se " +
                                        " join " + finder.bank + tableDelimiter + "users u on se.nzp_user = u.nzp_user " +
                                        " on sdv.nzp_dict = nzp_dict_event " + where + nzp_where;
                    ret = ExecSQL(conn_db, sql);

                    //Если компонент работает как история событий ЛС + его ИПУ, то добавляем выборку по ИПУ
                    if (finder.mode == 1)
                    {
                        //собираем счетчики
                        sql = " insert into sys_events_events_tmp (nzp_dict, name, code)  select distinct sdv.nzp_dict, sdv.name, sdv.code " +
                        " from " + Points.Pref + "_data" + tableDelimiter + "sys_dictionary_values sdv " +
                        " join " + finder.bank + tableDelimiter + "sys_events se " +
                            " join " + finder.bank + tableDelimiter + "users u on se.nzp_user = u.nzp_user " +
                        " on sdv.nzp_dict = se.nzp_dict_event " +
                        " join " + finder.bank + tableDelimiter + "counters_spis cs on se.nzp = cs.nzp_counter and cs.nzp_type = 3 " +
                        " where cs.nzp = " + finder.nzp + where;
                        ret = ExecSQL(conn_db, sql);

                        // собираем жильцов
                        sql = " insert into sys_events_events_tmp (nzp_dict, name, code)  select distinct sdv.nzp_dict, sdv.name, sdv.code " +
                      " from " + Points.Pref + "_data" + tableDelimiter + "sys_dictionary_values sdv " +
                      " join " + finder.bank + tableDelimiter + "sys_events se " +
                            " join " + finder.bank + tableDelimiter + "users u on se.nzp_user = u.nzp_user " +
                      " on sdv.nzp_dict = se.nzp_dict_event " +
                      " join " + finder.bank + tableDelimiter + "kart k on se.nzp = k.nzp_gil and k.isactual = '1' " +
                      " where k.nzp_kvar = " + finder.nzp + where;
                        ret = ExecSQL(conn_db, sql);

                        // собираем договоры
                        sql = " insert into sys_events_events_tmp (nzp_dict, name, code)  select distinct sdv.nzp_dict, sdv.name, sdv.code " +
                      " from " + Points.Pref + "_data" + tableDelimiter + "sys_dictionary_values sdv " +
                      " join " + Points.Pref + "_data" + tableDelimiter + "sys_events se " +
                            " join " + Points.Pref + "_data" + tableDelimiter + "users u on se.nzp_user = u.nzp_user " +
                      " on sdv.nzp_dict = se.nzp_dict_event " +
                      " join " + Points.Pref + "_data" + tableDelimiter + "fn_lsdogovor dog on se.nzp = dog.nzp_con " +
                      " where dog.nzp_kvar = " + finder.nzp + where;
                        ret = ExecSQL(conn_db, sql);
                    }
                }
                #endregion получение информации только по одному банку, если он был указан

                #region получение информации по всем банкам, если конкретный банк указан не был
                else
                {
                    #region Получаем список финансовых банков
                    List<string> fin_bank_list = new List<string>();
                    sql = "select dbname from " + Points.Pref + "_kernel" + tableDelimiter + "s_baselist where idtype = 4";
                    ret = ExecRead(conn_db, out reader, sql, true);
                    while (reader.Read())
                    {
                        fin_bank_list.Add(reader["dbname"].ToString());
                    }
                    #endregion

                    //получаем инфу из верхнего банка
                    sql = "  insert into sys_events_events_tmp (nzp_dict, name, code)  select distinct sdv.nzp_dict, sdv.name, sdv.code " +
                            " from " + Points.Pref + "_data" + tableDelimiter + "sys_dictionary_values sdv " +
                            " join " + Points.Pref + "_data" + tableDelimiter + "sys_events se " +
                                " join " + Points.Pref + "_data" + tableDelimiter + "users u on se.nzp_user = u.nzp_user " +
                            " on sdv.nzp_dict = se.nzp_dict_event " + where + nzp_where;
                    ret = ExecSQL(conn_db, sql);

                    //получение инфы с фин. банков
                    foreach (var bank in fin_bank_list)
                    {
                        #region проверка на наличие таблицы sys_events
                        try
                        {
#if PG
                            var tab = ClassDBUtils.OpenSQL(" select * from " + bank + tableDelimiter + "sys_events limit 1 ", conn_db);
#else
                            var tab = ClassDBUtils.OpenSQL(" select first 1 * from " + bank + tableDelimiter + "sys_events ", conn_db);
#endif
                            if (tab.resultData.Rows.Count == 0)
                                continue;
                        }
                        catch
                        {
                            continue;
                        }
                        #endregion

                        sql = " insert into sys_events_events_tmp (nzp_dict, name, code)  select distinct sdv.nzp_dict, sdv.name, sdv.code " +
                            " from " + Points.Pref + "_data" + tableDelimiter + "sys_dictionary_values sdv " +
                            " join " + bank + tableDelimiter + "sys_events se " +
                                " join " + Points.Pref + "_data" + tableDelimiter + "users u on se.nzp_user = u.nzp_user " +
                            " on sdv.nzp_dict = se.nzp_dict_event " + where + nzp_where;
                        ret = ExecSQL(conn_db, sql);
                    }

                    //получаем инфу из нижних банков
                    foreach (var bank in Points.PointList)
                    {
                        #region проверка на наличие таблицы sys_events
                        try
                        {
#if PG
                            var tab = ClassDBUtils.OpenSQL(" select * from " + bank.pref + "_data" + tableDelimiter + "sys_events limit 1 ", conn_db);
#else
                            var tab = ClassDBUtils.OpenSQL(" select first 1 * from " + bank.pref + "_data" + tableDelimiter + "sys_events ", conn_db);
#endif
                            if (tab.resultData.Rows.Count == 0)
                                continue;
                        }
                        catch
                        {
                            continue;
                        }
                        #endregion

                        sql = " insert into sys_events_events_tmp (nzp_dict, name, code)  select distinct sdv.nzp_dict, sdv.name, sdv.code " +
                            " from " + Points.Pref + "_data" + tableDelimiter + "sys_dictionary_values sdv " +
                            " join " + bank.pref + "_data" + tableDelimiter + "sys_events se " +
                                " join " + bank.pref + "_data" + tableDelimiter + "users u on se.nzp_user = u.nzp_user " +
                            " on sdv.nzp_dict = se.nzp_dict_event " + where + nzp_where;
                        ret = ExecSQL(conn_db, sql);

                        //Если компонент работает как история событий ЛС + его ИПУ, то добавляем выборку по ИПУ
                        if (finder.mode == 1)
                        {
                            //собираем счетчики
                            sql = " insert into sys_events_events_tmp (nzp_dict, name, code)  select distinct sdv.nzp_dict, sdv.name, sdv.code " +
                                " from " + Points.Pref + "_data" + tableDelimiter + "sys_dictionary_values sdv " +
                                " join " + bank.pref + "_data" + tableDelimiter + "sys_events se " +
                                    " join " + bank.pref + tableDelimiter + "users u on se.nzp_user = u.nzp_user " +
                                " on sdv.nzp_dict = se.nzp_dict_event " +
                                " join " + bank.pref + "_data" + tableDelimiter + "counters_spis cs on se.nzp = cs.nzp_counter and cs.nzp_type = 3 " +
                                " where cs.nzp = " + finder.nzp + where;
                            ret = ExecSQL(conn_db, sql);

                            //собираем жильцов
                            sql = " insert into sys_events_events_tmp (nzp_dict, name, code)  select distinct sdv.nzp_dict, sdv.name, sdv.code " +
                               " from " + Points.Pref + "_data" + tableDelimiter + "sys_dictionary_values sdv " +
                               " join " + bank.pref + tableDelimiter + "sys_events se " +
                                    " join " + bank.pref + tableDelimiter + "users u on se.nzp_user = u.nzp_user " +
                               " on sdv.nzp_dict = se.nzp_dict_event " +
                               " join " + bank.pref + tableDelimiter + "kart k on se.nzp = k.nzp_gil and k.isactual = '1' " +
                               " where k.nzp_kvar = " + finder.nzp + where;
                            ret = ExecSQL(conn_db, sql);

                            // собираем договоры
                            sql = " insert into sys_events_events_tmp (nzp_dict, name, code)  select distinct sdv.nzp_dict, sdv.name, sdv.code " +
                               " from " + Points.Pref + "_data" + tableDelimiter + "sys_dictionary_values sdv " +
                               " join " + Points.Pref + "_data" + tableDelimiter + "sys_events se " +
                                    " join " + Points.Pref + "_data" + tableDelimiter + "users u on se.nzp_user = u.nzp_user " +
                               " on sdv.nzp_dict = se.nzp_dict_event " +
                               " join " + Points.Pref + "_data" + tableDelimiter + "fn_lsdogovor dog on se.nzp = dog.nzp_con " +
                               " where dog.nzp_kvar = " + finder.nzp + where;
                            ret = ExecSQL(conn_db, sql);
                        }
                    }
                }
                #endregion

                sql = "select distinct * from sys_events_events_tmp order by code, nzp_dict";
                ret = ExecRead(conn_db, out reader, sql, true);

                if (!ret.result)
                {
                    MonitorLog.WriteLog("Ошибка в функции GetSysEventsEventsList " + ret.text, MonitorLog.typelog.Error, 20, 201, true);
                    conn_db.Close();
                    return null;
                }
                while (reader.Read())
                {
                    var zap = new SysEvents();
                    if (reader["nzp_dict"] != DBNull.Value) zap.nzp_dict_event = Convert.ToInt32(reader["nzp_dict"]);
                    if (reader["name"] != DBNull.Value) zap.event_name = Convert.ToString(reader["name"]).Trim();
                    spis.Add(zap);
                }

                reader.Close();

                ClassDBUtils.ExecSQL("drop table sys_events_events_tmp;", conn_db);

                conn_db.Close();
                return spis;
            }
            catch (Exception ex)
            {
                conn_db.Close();
                ret = new Returns(false, ex.Message);
                MonitorLog.WriteLog("Ошибка выполнения GetSysEventsEventsList", MonitorLog.typelog.Error, 20, 201, true);
                return null;
            }
        }

        //получить список названий сущностей, засветившихся в sys_events
        public List<SysEvents> GetSysEventsEntityList(SysEvents finder, out Returns ret)
        {
            IDbConnection conn_db = null;
            string sql = "";
            IDataReader reader;

            try
            {
                #region соединение с БД
                conn_db = GetConnection(Constants.cons_Kernel);
                ret = OpenDb(conn_db, true);
                if (!ret.result)
                {
                    ret = new Returns(false, "Ошибка при открытии соединения с БД");
                    MonitorLog.WriteLog("Ошибка GetSysEventsEntityList Ошибка при открытии соединения с БД", MonitorLog.typelog.Error, 20, 201, true);
                    conn_db.Close();
                    return null;
                }
                #endregion

                List<SysEvents> spis = new List<SysEvents>();

                string where = "";
                string nzp_where = "";
                //выбраны пользователи
                if (finder.users_list != null)
                {
                    where += " and trim((case when u.comment is not null then u.comment else '' end)) || '(' || trim(u.name) || ')' in (";
                    for (int i = 0; i < finder.users_list.Count; i++)
                    {
                        where += "'" + finder.users_list[i] + "',";
                    }
                    where = where.Substring(0, where.Length - 1) + ") ";
                }
                //выбран конкретный объект
                if (finder.nzp != 0)
                {
                    nzp_where += " and se.nzp = " + finder.nzp;
                }

                //скидываем темповую таблицу, если она существует
                ExecSQL(conn_db, "drop table sys_events_entity_tmp;", false);
                //создание темповой таблицы
                ExecSQL(conn_db, "create temp table sys_events_entity_tmp (id integer, type_name varchar(200))" + sUnlogTempTable);

                #region получение информации только по одному банку, если он был указан
                if (finder.bank != "")
                {
                    if (finder.bank == "top")
                        finder.bank = Points.Pref + "_data";

                    sql = " insert into sys_events_entity_tmp (id, type_name)  select distinct ot.* from " + Points.Pref + "_data" + tableDelimiter + "object_types ot " +
                            " join " + Points.Pref + "_data" + tableDelimiter + "sys_dictionary_values sdv " +
                                    " join " + finder.bank + tableDelimiter + "sys_events se " +
                                        " join " + finder.bank + tableDelimiter + "users u on se.nzp_user = u.nzp_user " +
                                    " on sdv.nzp_dict = se.nzp_dict_event " +
                            " on ot.id = cast(sdv.code as integer) " + where + nzp_where;
                    ret = ExecSQL(conn_db, sql);
                    #region Если компонент работает как история событий ЛС + его ИПУ, то добавляем выборку по ИПУ
                    if (finder.mode == 1)
                    {
                        //собираем счетчики
                        sql = " insert into sys_events_entity_tmp (id, type_name)  select distinct ot.* from " + Points.Pref + "_data" + tableDelimiter + "object_types ot " +
                            " join " + Points.Pref + "_data" + tableDelimiter + "sys_dictionary_values sdv " +
                                " join " + finder.bank + tableDelimiter + "sys_events se " +
                                     " join " + finder.bank + tableDelimiter + "counters_spis cs on se.nzp = cs.nzp_counter and cs.nzp_type = 3 and cs.nzp = " + finder.nzp +
                                     " join " + finder.bank + tableDelimiter + "users u on se.nzp_user = u.nzp_user " +
                                " on sdv.nzp_dict = se.nzp_dict_event " +
                            " on ot.id = cast(sdv.code as integer) " + where;
                        ret = ExecSQL(conn_db, sql);

                        //собираем жильцов
                        sql = " insert into sys_events_entity_tmp (id, type_name)  select distinct ot.* from " + Points.Pref + "_data" + tableDelimiter + "object_types ot " +
                        " join " + Points.Pref + "_data" + tableDelimiter + "sys_dictionary_values sdv " +
                            " join " + finder.bank + tableDelimiter + "sys_events se " +
                                 " join " + finder.bank + tableDelimiter + "kart k on se.nzp = k.nzp_gil and k.isactual = '1' and k.nzp_kvar = " + finder.nzp +
                                 " join " + finder.bank + tableDelimiter + "users u on se.nzp_user = u.nzp_user " +
                            " on sdv.nzp_dict = se.nzp_dict_event " +
                        " on ot.id = cast(sdv.code as integer) " + where;
                        ret = ExecSQL(conn_db, sql);

                        // собираем договоры
                        sql = " insert into sys_events_entity_tmp (id, type_name)  select distinct ot.* from " + Points.Pref + "_data" + tableDelimiter + "object_types ot " +
                        " join " + Points.Pref + "_data" + tableDelimiter + "sys_dictionary_values sdv " +
                            " join " + Points.Pref + "_data" + tableDelimiter + "sys_events se " +
                                 " join " + Points.Pref + "_data" + tableDelimiter + "fn_lsdogovor dog on se.nzp = dog.nzp_con and dog.nzp_kvar = " + finder.nzp +
                                 " join " + Points.Pref + "_data" + tableDelimiter + "users u on se.nzp_user = u.nzp_user " +
                            " on sdv.nzp_dict = se.nzp_dict_event " +
                        " on ot.id = cast(sdv.code as integer) " + where;
                        ret = ExecSQL(conn_db, sql);
                    }
                    #endregion
                }
                #endregion получение информации только по одному банку, если он был указан

                #region получение информации по всем банкам, если конкретный банк указан не был
                else
                {
                    #region Получаем список финансовых банков
                    List<string> fin_bank_list = new List<string>();
                    sql = "select dbname from " + Points.Pref + "_kernel" + tableDelimiter + "s_baselist where idtype = 4";
                    ret = ExecRead(conn_db, out reader, sql, true);
                    while (reader.Read())
                    {
                        fin_bank_list.Add(reader["dbname"].ToString());
                    }
                    #endregion

                    //получаем инфу из верхнего банка
                    sql = " insert into sys_events_entity_tmp (id, type_name)  select distinct ot.* from " + Points.Pref + "_data" + tableDelimiter + "object_types ot " +
                            " join " + Points.Pref + "_data" + tableDelimiter + "sys_dictionary_values sdv " +
                                " join " + Points.Pref + "_data" + tableDelimiter + "sys_events se " +
                                    " join " + Points.Pref + "_data" + tableDelimiter + "users u on se.nzp_user = u.nzp_user " +
                                " on sdv.nzp_dict = se.nzp_dict_event " +
                            " on ot.id = cast(sdv.code as integer) " + where + nzp_where;
                    ret = ExecSQL(conn_db, sql);

                    //получение инфы с фин. банков
                    foreach (var bank in fin_bank_list)
                    {
                        #region проверка на наличие таблицы sys_events
                        try
                        {
#if PG
                            var tab = ClassDBUtils.OpenSQL(" select * from " + bank + tableDelimiter + "sys_events limit 1 ", conn_db);
#else
                            var tab = ClassDBUtils.OpenSQL(" select first 1 * from " + bank + tableDelimiter + "sys_events ", conn_db);
#endif
                            if (tab.resultData.Rows.Count == 0)
                                continue;
                        }
                        catch
                        {
                            continue;
                        }
                        #endregion

                        sql = " insert into sys_events_entity_tmp (id, type_name)  select distinct ot.* from " + Points.Pref + "_data" + tableDelimiter + "object_types ot " +
                           " join " + Points.Pref + "_data" + tableDelimiter + "sys_dictionary_values sdv " +
                               " join " + bank + tableDelimiter + "sys_events se " +
                                    " join " + Points.Pref + "_data" + tableDelimiter + "users u on se.nzp_user = u.nzp_user " +
                               " on sdv.nzp_dict = se.nzp_dict_event " +
                           " on ot.id = cast(sdv.code as integer) " + where + nzp_where;
                        ret = ExecSQL(conn_db, sql);
                    }

                    //получаем инфу из нижних банков
                    foreach (var bank in Points.PointList)
                    {
                        sql = " insert into sys_events_entity_tmp (id, type_name)  select distinct ot.* from " + Points.Pref + "_data" + tableDelimiter + "object_types ot " +
                            " join " + Points.Pref + "_data" + tableDelimiter + "sys_dictionary_values sdv " +
                                " join " + bank.pref + "_data" + tableDelimiter + "sys_events se " +
                                    " join " + bank.pref + "_data" + tableDelimiter + "users u on se.nzp_user = u.nzp_user " +
                                " on sdv.nzp_dict = se.nzp_dict_event " +
                            " on ot.id = cast(sdv.code as integer) " + where + nzp_where;
                        ret = ExecSQL(conn_db, sql);

                        //Если компонент работает как история событий ЛС + его ИПУ, то добавляем выборку по ИПУ
                        if (finder.mode == 1)
                        {
                            sql = " insert into sys_events_entity_tmp (id, type_name)  select distinct ot.* from " + Points.Pref + "_data" + tableDelimiter + "object_types ot " +
                           " join " + Points.Pref + "_data" + tableDelimiter + "sys_dictionary_values sdv " +
                               " join " + bank.pref + "_data" + tableDelimiter + "sys_events se " +
                                    " join " + bank.pref + "_data" + tableDelimiter + "counters_spis cs on se.nzp = cs.nzp_counter and cs.nzp_type = 3 and cs.nzp = " + finder.nzp +
                                    " join " + bank.pref + "_data" + tableDelimiter + "users u on se.nzp_user = u.nzp_user " +
                               " on sdv.nzp_dict = se.nzp_dict_event " +
                           " on ot.id = cast(sdv.code as integer) " + where;
                            ret = ExecSQL(conn_db, sql);

                            //собираем жильцов
                            sql = " insert into sys_events_entity_tmp (id, type_name)  select distinct ot.* from " + Points.Pref + "_data" + tableDelimiter + "object_types ot " +
                            " join " + Points.Pref + "_data" + tableDelimiter + "sys_dictionary_values sdv " +
                                " join " + bank.pref + "_data" + tableDelimiter + "sys_events se " +
                                     " join " + bank.pref + "_data" + tableDelimiter + "kart k on se.nzp = k.nzp_gil and k.isactual = '1' and k.nzp_kvar = " + finder.nzp +
                                    " join " + bank.pref + "_data" + tableDelimiter + "users u on se.nzp_user = u.nzp_user " +
                                " on sdv.nzp_dict = se.nzp_dict_event " +
                            " on ot.id = cast(sdv.code as integer) " + where;
                            ret = ExecSQL(conn_db, sql);

                            // собираем договоры
                            sql = " insert into sys_events_entity_tmp (id, type_name)  select distinct ot.* from " + Points.Pref + "_data" + tableDelimiter + "object_types ot " +
                            " join " + Points.Pref + "_data" + tableDelimiter + "sys_dictionary_values sdv " +
                                " join " + Points.Pref + "_data" + tableDelimiter + "sys_events se " +
                                    " join " + Points.Pref + "_data" + tableDelimiter + "fn_lsdogovor dog on se.nzp = dog.nzp_con and dog.nzp_kvar = " + finder.nzp +
                                    " join " + Points.Pref + "_data" + tableDelimiter + "users u on se.nzp_user = u.nzp_user " +
                                " on sdv.nzp_dict = se.nzp_dict_event " +
                            " on ot.id = cast(sdv.code as integer) " + where;
                            ret = ExecSQL(conn_db, sql);
                        }
                    }
                }
                #endregion

                sql = "select distinct * from sys_events_entity_tmp order by id";
                ret = ExecRead(conn_db, out reader, sql, true);

                if (!ret.result)
                {
                    MonitorLog.WriteLog("Ошибка в функции GetSysEventsEntityList " + ret.text, MonitorLog.typelog.Error, 20, 201, true);
                    conn_db.Close();
                    return null;
                }
                while (reader.Read())
                {
                    SysEvents zap = new SysEvents();
                    if (reader["id"] != DBNull.Value) zap.entity_id = Convert.ToInt32(reader["id"]);
                    if (reader["type_name"] != DBNull.Value) zap.entity_name = Convert.ToString(reader["type_name"]).Trim();
                    spis.Add(zap);
                }

                reader.Close();

                ClassDBUtils.ExecSQL("drop table sys_events_entity_tmp;", conn_db);

                conn_db.Close();
                return spis;
            }
            catch (Exception ex)
            {
                conn_db.Close();
                ret = new Returns(false, ex.Message);
                MonitorLog.WriteLog("Ошибка выполнения GetSysEventsEntityList", MonitorLog.typelog.Error, 20, 201, true);
                return null;
            }
        }

        public List<CountersArx> GetCountersChangeHistory(CountersArx finder, out Returns ret)
        {
            IDbConnection conn_db = null;

            try
            {
                #region соединение с БД
                conn_db = GetConnection(Constants.cons_Kernel);
                ret = OpenDb(conn_db, true);
                if (!ret.result)
                {
                    ret = new Returns(false, "Ошибка при открытии соединения с БД");
                    MonitorLog.WriteLog("Ошибка GetCountersChangeHistory Ошибка при открытии соединения с БД", MonitorLog.typelog.Error, 20, 201, true);
                    conn_db.Close();
                    return null;
                }
                #endregion

                string where = ""; //дополнительные ограничения

                int recordsTotalCount = 0;
                List<CountersArx> spis = new List<CountersArx>();
                IDataReader reader;

                #region формирование ограничений по входящему фильтру
                //выбрана дата
                if (finder.from_date != null)
                {
                    where += " and ca.dat_when >= '" + Convert.ToDateTime(finder.from_date).ToShortDateString() + "' ";
                }
                if (finder.to_date != null)
                {
                    where += " and ca.dat_when <= '" + Convert.ToDateTime(finder.to_date).ToShortDateString() + "' ";
                }

                //выбраны поля
                if (finder.fields_list != null)
                {
                    where += " and ca.pole in ('";
                    for (int i = 0; i < finder.fields_list.Count; i++)
                    {
                        where += finder.fields_list[i] + "','";
                    }
                    where = where.Substring(0, where.Length - 2) + ") ";
                }

                //выбраны пользователи
                if (finder.users_list != null)
                {
                    where += " and trim((case when u.comment is not null then u.comment else '' end)) || '(' || trim(u.name) || ')' in (";
                    for (int i = 0; i < finder.users_list.Count; i++)
                    {
                        where += "'" + finder.users_list[i] + "',";
                    }
                    where = where.Substring(0, where.Length - 1) + ") ";
                }
                #endregion формирование ограничений по входящему фильтру

                #region получение основной информации
                //Определить общее количество записей         
                var sql = " select count(*) from " + finder.counter_pref + "_data" + tableDelimiter + "counters_arx ca " +
                      " join " + finder.counter_pref + "_data" + tableDelimiter + "users u " +
                    " on ca.nzp_user = u.nzp_user " +
                    " where ca.nzp_counter = " + finder.nzp_counter + where; ;
                var t_count = ExecScalar(conn_db, sql, out ret, true);
                recordsTotalCount = Convert.ToInt32(t_count);

                //получить основной массив записей
                //получение инфы по нижним банкам 
                sql = " select ca.nzp_arx, ca.nzp_counter, ca.pole, ca.nzp_user, ca.dat_when, ca.dat_calc, trim(u.comment) as user, " +
                            " (case when ca.pole = 'nzp_cnttype' then (select sct.name_type from " + finder.counter_pref + "_kernel" + tableDelimiter + "s_counttypes sct where nzp_cnttype = cast(ca.val_old as integer)) else ca.val_old end) as val_old,  " +
                            " (case when ca.pole = 'nzp_cnttype' then (select sct.name_type from " + finder.counter_pref + "_kernel" + tableDelimiter + "s_counttypes sct where nzp_cnttype = cast(ca.val_new as integer)) else ca.val_new end) as val_new " +
                            " from " + finder.counter_pref + "_data" + tableDelimiter + "counters_arx ca " +
                            " join " + finder.counter_pref + "_data" + tableDelimiter + "users u " +
                            " on ca.nzp_user = u.nzp_user " +
                        " where ca.nzp_counter = " + finder.nzp_counter + where + " order by dat_when desc, nzp_arx desc ";

                ret = ExecRead(conn_db, out reader, sql, true);
                #endregion получение основной информации

                if (!ret.result)
                {
                    MonitorLog.WriteLog("Ошибка в функции GetCountersChangeHistory " + ret.text, MonitorLog.typelog.Error, 20, 201, true);
                    conn_db.Close();
                    return null;
                }

                int k = 0;
                int count = 0;
                while (reader.Read())
                {
                    k++;
                    if (k < finder.skip) continue;

                    CountersArx zap = new CountersArx();
                    zap.num = k;
                    if (reader["nzp_arx"] != DBNull.Value) zap.nzp_arx = Convert.ToInt32(reader["nzp_arx"]);
                    if (reader["nzp_counter"] != DBNull.Value) zap.nzp_counter = Convert.ToInt32(reader["nzp_counter"]);
                    if (reader["val_new"] != DBNull.Value) zap.val_new = Convert.ToString(reader["val_new"]).Trim();
                    if (reader["val_old"] != DBNull.Value) zap.val_old = Convert.ToString(reader["val_old"]).Trim();
                    if (reader["nzp_user"] != DBNull.Value) zap.nzp_user = Convert.ToInt32(reader["nzp_user"]);
                    if (reader["dat_when"] != DBNull.Value) zap.dat_when = Convert.ToDateTime(reader["dat_when"]);
                    if (reader["dat_when"] != DBNull.Value) zap.date_ = Convert.ToDateTime(reader["dat_when"]).ToShortDateString();
                    if (reader["dat_calc"] != DBNull.Value) zap.dat_calc = Convert.ToDateTime(reader["dat_calc"]);
                    if (reader["user"] != DBNull.Value) zap.user = Convert.ToString(reader["user"]).Trim();
                    if (reader["pole"] != DBNull.Value)
                    {
                        switch (reader["pole"].ToString().Trim())
                        {
                            case "comment":
                                zap.pole = "Комментарий";
                                break;
                            case "dat_close":
                                zap.pole = "Дата закрытия";
                                break;
                            case "dat_prov":
                                zap.pole = "Дата поверки";
                                break;
                            case "dat_provnext":
                                zap.pole = "Дата следующей поверки";
                                break;
                            case "num_cnt":
                                zap.pole = "Заводской номер прибора учета";
                                break;
                            case "nzp_cnttype":
                                zap.pole = "Тип прибора учета";
                                break;
                            case "dat_poch":
                                zap.pole = "Дата починки";
                                break;
                            case "dat_oblom":
                                zap.pole = "Дата поломки";
                                break;
                            default:
                                zap.pole = reader["pole"].ToString().Trim();
                                break;
                        }
                    }
                    spis.Add(zap);

                    count++;
                    if (count >= finder.rows) break;
                }

                ret.tag = recordsTotalCount;

                reader.Close();
                conn_db.Close();
                return spis;
            }
            catch (Exception ex)
            {
                conn_db.Close();
                ret = new Returns(false, ex.Message);
                MonitorLog.WriteLog("Ошибка выполнения GetCountersChangeHistory", MonitorLog.typelog.Error, 20, 201, true);
                return null;
            }
        }

        public List<CountersArx> GetCountersFields(CountersArx finder, out Returns ret)
        {
            IDbConnection conn_db = null;
            IDataReader reader;

            try
            {
                #region соединение с БД
                conn_db = GetConnection(Constants.cons_Kernel);
                ret = OpenDb(conn_db, true);
                if (!ret.result)
                {
                    ret = new Returns(false, "Ошибка при открытии соединения с БД");
                    MonitorLog.WriteLog("Ошибка GetSysEvents Ошибка при открытии соединения с БД", MonitorLog.typelog.Error, 20, 201, true);
                    conn_db.Close();
                    return null;
                }
                #endregion

                DbTables tables = new DbTables(conn_db);

                string sql = "";
                int recordsTotalCount = 0;
                List<CountersArx> spis = new List<CountersArx>();

                #region получение основной информации
                //получение инфы по нижним банкам
                sql = " select distinct pole from " + finder.counter_pref + "_data" + tableDelimiter + "counters_arx where nzp_counter = " + finder.nzp_counter + " order by pole ";

                ret = ExecRead(conn_db, out reader, sql, true);
                #endregion получение основной информации

                if (!ret.result)
                {
                    MonitorLog.WriteLog("Ошибка в функции GetCountersFields " + ret.text, MonitorLog.typelog.Error, 20, 201, true);
                    conn_db.Close();
                    return null;
                }
                while (reader.Read())
                {
                    CountersArx zap = new CountersArx();
                    if (reader["pole"] != DBNull.Value)
                    {
                        zap.field = reader["pole"].ToString().Trim();
                        switch (reader["pole"].ToString().Trim())
                        {
                            case "comment":
                                zap.pole = "Комментарий";
                                break;
                            case "dat_close":
                                zap.pole = "Дата закрытия";
                                break;
                            case "dat_prov":
                                zap.pole = "Дата поверки";
                                break;
                            case "dat_provnext":
                                zap.pole = "Дата следующей поверки";
                                break;
                            case "num_cnt":
                                zap.pole = "Заводской номер прибора учета";
                                break;
                            case "nzp_cnttype":
                                zap.pole = "Тип прибора учета";
                                break;
                            case "dat_poch":
                                zap.pole = "Дата починки";
                                break;
                            case "dat_oblom":
                                zap.pole = "Дата поломки";
                                break;
                            default:
                                zap.pole = reader["pole"].ToString().Trim();
                                break;
                        }
                    }
                    spis.Add(zap);
                }

                ret.tag = recordsTotalCount;

                reader.Close();
                conn_db.Close();
                return spis;
            }
            catch (Exception ex)
            {
                conn_db.Close();
                ret = new Returns(false, ex.Message);
                MonitorLog.WriteLog("Ошибка выполнения GetCountersFields", MonitorLog.typelog.Error, 20, 201, true);
                return null;
            }
        }

        public List<CountersArx> GetCountersArxUsersList(CountersArx finder, out Returns ret)
        {
            IDbConnection conn_db = null;
            IDataReader reader;

            try
            {
                #region соединение с БД
                conn_db = GetConnection(Constants.cons_Kernel);
                ret = OpenDb(conn_db, true);
                if (!ret.result)
                {
                    ret = new Returns(false, "Ошибка при открытии соединения с БД");
                    MonitorLog.WriteLog("Ошибка GetSysEventsUsersList Ошибка при открытии соединения с БД", MonitorLog.typelog.Error, 20, 201, true);
                    conn_db.Close();
                    return null;
                }
                #endregion

                DbTables tables = new DbTables(conn_db);

                List<CountersArx> spis = new List<CountersArx>();

                var sql = " select distinct loc_u.nzp_user, loc_u.name, trim(" + sNvlWord + "(loc_u.comment, '')) as uname " +
                        " from " + finder.counter_pref + "_data" + tableDelimiter + "users loc_u " +
                        " join " + finder.counter_pref + "_data" + tableDelimiter + "counters_arx ca on loc_u.nzp_user = ca.nzp_user " +
                        " where ca.nzp_counter = " + finder.nzp_counter + " order by uname";

                ret = ExecRead(conn_db, out reader, sql, true);

                if (!ret.result)
                {
                    MonitorLog.WriteLog("Ошибка в функции GetCountersArxUsersList " + ret.text, MonitorLog.typelog.Error, 20, 201, true);
                    conn_db.Close();
                    return null;
                }
                while (reader.Read())
                {
                    CountersArx zap = new CountersArx();
                    zap.nzp_user = 0;
                    zap.name = "";
                    zap.user = "";

                    if (reader["nzp_user"] != DBNull.Value) zap.nzp_user = Convert.ToInt32(reader["nzp_user"]);
                    if (reader["name"] != DBNull.Value) zap.name = Convert.ToString(reader["name"]);
                    if (reader["uname"] != DBNull.Value) zap.user = Convert.ToString(reader["uname"]).Trim();
                    spis.Add(zap);
                }

                reader.Close();
                conn_db.Close();
                return spis;
            }
            catch (Exception ex)
            {
                conn_db.Close();
                ret = new Returns(false, ex.Message);
                MonitorLog.WriteLog("Ошибка выполнения GetCountersArxUsersList", MonitorLog.typelog.Error, 20, 201, true);
                return null;
            }
        }

        public bool InsertSysEvent(SysEvents finder)
        {
            try
            {
                IDbConnection conn_db = null;

                conn_db = GetConnection(Constants.cons_Kernel);
                OpenDb(conn_db, true);

                if (finder.pref == null || finder.pref == "")
                    finder.pref = Points.Pref;

                string sSQL_Text = " insert into " + finder.pref + "_data" + tableDelimiter + "sys_events(DATE_,NZP_USER,NZP_DICT_EVENT,NZP,NOTE) " +
                    " values( '" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "', " + (finder.nzp_user != 0 ? "(select nzp_user from " + finder.pref + "_data" + tableDelimiter + "users where web_user = " + finder.nzp_user.ToString("") + ") " : " 0 ") +
                    ", " + finder.nzp_dict.ToString("") + "," + finder.nzp_obj.ToString("") + ",'" + finder.note + "' ) ";

                return DBManager.ExecSQL(conn_db, sSQL_Text, true).result;                    
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка добавления события в sys_events" + ex.Message, MonitorLog.typelog.Error, 20, 201, true);
                return false;
            }
        }

        public static bool InsertSysEvent(SysEvents finder, IDbConnection conn)
        {
            try
            {
                if (finder.pref == null || finder.pref == "")
                    finder.pref = Points.Pref;
                var bank = finder.bank;
                if (bank == null)
                    bank = finder.pref + "_data";

                string sSQL_Text = " insert into " + bank + tableDelimiter + "sys_events(DATE_,NZP_USER,NZP_DICT_EVENT,NZP,NOTE) " +
                    " values( '" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "', " + (finder.nzp_user != 0 ? "(select nzp_user from " + finder.pref + "_data" + tableDelimiter + "users where web_user = " + finder.nzp_user.ToString("") + ") " : " 0 ") +
                    ", " + finder.nzp_dict.ToString("") + "," + finder.nzp_obj.ToString("") + ",'" + finder.note + "' ) ";

                return DBManager.ExecSQL(conn, sSQL_Text, true).result;                    
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка добавления события в sys_events" + ex.Message, MonitorLog.typelog.Error, 20, 201, true);
                return false;
            }
        }

        public static bool InsertSysEvent(SysEvents finder, IDbTransaction transaction, IDbConnection conn)
        {
            try
            {
                if (string.IsNullOrEmpty(finder.pref))
                    finder.pref = Points.Pref;
                var bank = finder.bank ?? finder.pref + "_data";

                string sSqlText = " insert into " + bank + tableDelimiter + "sys_events(DATE_,NZP_USER,NZP_DICT_EVENT,NZP,NOTE) " +
                    " values( '" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "', " + (finder.nzp_user != 0 ? "(select nzp_user from " + finder.pref + "_data" + tableDelimiter + "users where web_user = " + finder.nzp_user.ToString("") + ") " : " 0 ") + ", " +
                    finder.nzp_dict.ToString("") + "," + finder.nzp_obj.ToString("") + ",'" + finder.note + "' ) ";
                
               return  DBManager.ExecSQL(conn, transaction, sSqlText, true).result;                
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка добавления события в sys_events" + ex.Message, MonitorLog.typelog.Error, 20, 201, true);
                return false;
            }
        }

        public LogsTree GetHostLogsList(LogsTree finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            try
            {
                var res = finder;

                #region Читаем список логов хоста
                if (Directory.Exists(Directory.GetCurrentDirectory() + @"\Logs"))
                {
                    var dirs = Directory.GetDirectories(Directory.GetCurrentDirectory() + @"\Logs");
                    foreach (var dir in dirs)
                    {
                        DateTime curDate;
                        var parseRes = DateTime.TryParse(dir.Split('\\').Last(), out curDate);
                        if (parseRes == false)
                            continue;

                        var year = curDate.Year;
                        var month = curDate.Month;
                        var day = curDate.Day;

                        //Если в словаре еще нет указанного года, добавляем
                        if (!res.childs.Keys.Contains(year))
                            res.childs.Add(year, new LogsTree() { log_name = year.ToString() });

                        //Если в словаре еще нет указанного месяца, добавляем
                        if (!res.childs[year].childs.Keys.Contains(month))
                            res.childs[year].childs.Add(month, new LogsTree() { log_name = curDate.ToString("MMMM") });

                        //Добавляем день в словарь
                        if (!res.childs[year].childs[month].childs.Keys.Contains(day))
                            res.childs[year].childs[month].childs.Add(day, new LogsTree() { log_name = day + " (Host)" });
                        else
                            res.childs[year].childs[month].childs[day].log_name = day + " (Web, Host)";
                    }
                }
                #endregion Читаем список логов хоста

                return res;
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка функции GetHostLogsList" + ex.Message, MonitorLog.typelog.Error, 20, 201, true);
                ret = new Returns(false, ex.Message);
                return new LogsTree();
            }
        }

        /// <summary>
        /// Получение файла логов c хоста
        /// </summary>
        /// <returns></returns>
        public LogsTree GetHostLogsFile(LogsTree finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            var arch = new Utility.Archive();
            int nzpExc = 0;
            var hostLogName = "";
            bool has_host_logs = false;
            var logsDir = Directory.GetCurrentDirectory() + @"\Logs\";

            try
            {
                //скачиваем логи с веба, если таковые есть
                if (finder.has_web_logs)
                {
                    if (InputOutput.useFtp)
                        InputOutput.DownloadFile(finder.web_log_name, logsDir + finder.web_log_name);
                    else
                        File.Move(Path.Combine(Constants.Directories.ImportDir.Replace("/", "\\"), finder.web_log_name), logsDir + finder.web_log_name);
                }

                //формируем архив с логами хоста
                if (Directory.Exists(logsDir))
                {
                    //получаем список папок с логами
                    var dirs = Directory.GetDirectories(logsDir);

                    foreach (var dir in dirs)
                    {
                        DateTime curDate;
                        var parseRes = DateTime.TryParse(dir.Split('\\').Last(), out curDate);
                        //если не является логом
                        if (parseRes == false)
                            continue;

                        var year = curDate.Year;
                        var month = curDate.Month;
                        var day = curDate.Day;

                        //если дата совпадает с той, что пришла в файндере
                        if (year == finder.year && month == finder.month && day == finder.day)
                        {
                            // архивируем логи хоста
                            hostLogName = "host_" + DateTime.Now.Ticks + ".7z";
                            var result = arch.CompressDirectory(dir, logsDir + hostLogName, false);

                            has_host_logs = true;

                            break;
                        }
                    }
                }

                if (finder.has_web_logs || has_host_logs)
                {
                    var selectedDate =
                         finder.year +
                         (finder.month != 0 ? "_" + finder.month.ToString("00") : "") +
                         (finder.day != 0 ? "_" + finder.day.ToString("00") : "");
                    //закидываем логи веба и хоста в один архив
                    var logName = "logs_" + selectedDate + DateTime.Now.Ticks + ".7z";

                    if (finder.has_web_logs)
                        arch.AddFile(logsDir + finder.web_log_name);
                    else
                        arch.AddFile(logsDir + hostLogName);
                    if (finder.has_web_logs)
                        arch.AddFile(logsDir + hostLogName);

                    var result = arch.CompressFiles(logsDir + "\\" + logName, true);

                    var path = "";
                    if (InputOutput.useFtp)
                    {
                        //переносим на фтп или веб
                        path = InputOutput.SaveOutputFile(logsDir + "\\" + logName);
                    }
                    else
                    {
                        path = Path.Combine(Constants.Directories.ImportDir, logName);
                        File.Move(logsDir + "\\" + logName, path);
                    }

                    //сохраняес в excelutility
                    ret = AddMyFile(new ExcelUtility()
                    {
                        nzp_user = finder.nzp_user,
                        status = ExcelUtility.Statuses.Success,
                        rep_name = "Логи" + selectedDate,
                        file_name = "logs_" + selectedDate + ".7z",
                        exc_path = path
                    });

                    nzpExc = ret.tag;
                }
                else
                {
                    ret = new Returns(false, "Логи за указанный период не найдены.", -2);
                }

                return new LogsTree() { nzp_exc = nzpExc };
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка функции GetHostLogsFile" + ex.Message, MonitorLog.typelog.Error, 20, 201, true);
                ret = new Returns(false, ex.Message);
                return new LogsTree();
            }
        }
    }
}
