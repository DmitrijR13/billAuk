using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;

namespace STCLINE.KP50.DataBase
{
    //----------------------------------------------------------------------
    public class DbNedop : DbNedopClient
    //----------------------------------------------------------------------
    {
        /// <summary>
        /// Поиск недопоставок в основной БД и запись их в кэш
        /// </summary>
        /// <param name="finder">объект для поиска</param>
        /// <param name="ret">результат работы функции</param>
        public void FindNedop(Nedop finder, out Returns ret)
        {
            if (GlobalSettings.WorkOnlyWithCentralBank)
            {
                ret = new Returns(false, "Данные о недопоставках не доступны, т.к. установлен режим работы с центральным банком данных", -1);
                return;
            }

            #region Проверка входных параметров
            if (finder.nzp_user < 1)
            {
                ret = new Returns(false, "Не определен пользователь");
                return;
            }
            if (finder.pref == "")
            {
                ret = new Returns(false, "Не определен префикс БД");
                return;
            }
            if (Utils.GetParams(finder.prms, Constants.page_spisnddom))
            {
                if (finder.nzp_dom <= 0)
                {
                    ret = new Returns(false, "Не определен дом");
                    return;
                }
            }
            else if (finder.nzp_kvar <= 0)
            {
                ret = new Returns(false, "Не определен л/с");
                return;
            }
            #endregion

            ret = Utils.InitReturns();

            #region соединение с бд Kernel
            string connectionString = Points.GetConnByPref(finder.pref);
            IDbConnection conn_db = GetConnection(connectionString);
            ret = OpenDb(conn_db, true);
            if (!ret.result)
            {
                return;
            }
            #endregion

#if PG
if (!TempTableInWebCashe(conn_db, finder.pref + "_data.nedop_kvar"))
            {
                ret.result = false;
                ret.text = "Данные о недопоставках временно не доступны";
                ret.tag = -1;
                conn_db.Close();
                return;
            }
#else
            if (!TempTableInWebCashe(conn_db, finder.pref + "_data:nedop_kvar"))
            {
                ret.result = false;
                ret.text = "Данные о недопоставках временно не доступны";
                ret.tag = -1;
                conn_db.Close();
                return;
            }
#endif

            #region соединение с бд Webdata
            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result)
            {
                conn_db.Close();
                return;
            }
            #if PG
                            ExecSQL(conn_web, "set search_path to 'public'", false);
            #endif
            #endregion
#if PG
            string tXX_nedop = "t" + Convert.ToString(finder.nzp_user) + "_nedop";
            string tXX_nedop_full = "public."+ tXX_nedop;
#else
            string tXX_nedop = "t" + Convert.ToString(finder.nzp_user) + "_nedop";
            string tXX_nedop_full = conn_web.Database + "@" + DBManager.getServer(conn_web) + ":" + tXX_nedop;
#endif
            #region удалить существующую таблицу tXX_nedop
            if (TableInWebCashe(conn_web, tXX_nedop))
            {
                ret = ExecSQL(conn_web, " Drop table " + tXX_nedop, false);
            }
            #endregion
            bool key = false;
            if (ret.result)
            {
                #region создать таблицу webdata:tXX_nedop
#if PG
                            ret = ExecSQL(conn_web,
                          " Create table " + tXX_nedop +
                          " (  nzp_nedop INTEGER, "+
                          "     nzp_kvar INTEGER, "+
                          "     nzp_dom INTEGER, " +
                          "     nzp_serv INTEGER, "+
                          "     service CHAR(100), " +
                          "     nzp_supp INTEGER, "+
                          "     supplier CHAR(100), " +
                          "     dat_s timestamp, " +
                          "     dat_po timestamp , " +
                          "     tn CHAR(20), "+
                          "     comment CHAR(200), "+
                          "     is_actual SMALLINT, "+
                          "     remark CHAR(10), " +
                          "     nzp_user INTEGER, "+
                          "     user_name CHAR(113), " +
                          "     dat_when DATE, "+
                          "     nzp_kind INTEGER, "+
                          "     kind CHAR(90) " +
                          " ) ", true);
#else
                ret = ExecSQL(conn_web,
                                          " Create table " + tXX_nedop +
                                          " (  nzp_nedop INTEGER, " +
                                          "     nzp_kvar INTEGER, " +
                                          "     nzp_dom INTEGER, " +
                                          "     nzp_serv INTEGER, " +
                                          "     service CHAR(100), " +
                                          "     nzp_supp INTEGER, " +
                                          "     supplier CHAR(100), " +
                                          "     dat_s DATETIME YEAR to MINUTE, " +
                                          "     dat_po DATETIME YEAR to MINUTE, " +
                                          "     tn NCHAR(20), " +
                                          "     comment CHAR(200), " +
                                          "     is_actual SMALLINT, " +
                                          "     remark CHAR(10), " +
                                          "     nzp_user INTEGER, " +
                                          "     user_name CHAR(113), " +
                                          "     dat_when DATE, " +
                                          "     nzp_kind INTEGER, " +
                                          "     kind CHAR(90) " +
                                          " ) ", true);
#endif
                if (!ret.result)
                {
                    conn_db.Close();
                    conn_web.Close();
                    return;
                }
                key = true;
                #endregion
            }
            else ret = ExecSQL(conn_web, " delete from " + tXX_nedop, false);

            string filter = "";
            #region фильтр по услугам и поставщикам
            if (finder.RolesVal != null)
                foreach (_RolesVal role in finder.RolesVal)
                {
                    if (role.tip == Constants.role_sql)
                    {
                        if (role.kod == Constants.role_sql_serv) filter += " and n.nzp_serv in (" + role.val + ") ";
                        else if (role.kod == Constants.role_sql_supp) filter += " and n.nzp_supp in (" + role.val + ") ";
                    }
                }
            #endregion
            if (finder.nzp_kvar > 0) filter += " and n.nzp_kvar = " + finder.nzp_kvar;
            if (finder.nzp_dom > 0) filter += " and n.nzp_kvar = kv.nzp_kvar and kv.nzp_dom = " + finder.nzp_dom;

            StringBuilder sql = new StringBuilder();

            #region Создание временной таблицы t_actual
            ExecSQL(conn_db, "drop table t_actual", false);

            if (!ExecSQL(conn_db, "create temp table t_actual (is_actual integer, remark char(10))", false).result)
                ExecSQL(conn_db, "delete from table t_actual", false);

            ExecSQL(conn_db, "insert into t_actual (is_actual,remark) values(   1,'УК (РЖУ)')", false);
            ExecSQL(conn_db, "insert into t_actual (is_actual,remark) values(   2,'УПГ ЕРЦ')", false);
            ExecSQL(conn_db, "insert into t_actual (is_actual,remark) values(   3,'УПГ ЕРЦ')", false);
            ExecSQL(conn_db, "insert into t_actual (is_actual,remark) values(   4,'УПГ ЕРЦ')", false);
            ExecSQL(conn_db, "insert into t_actual (is_actual,remark) values(   5,'Портал')", false);
            ExecSQL(conn_db, "insert into t_actual (is_actual,remark) values(  11,'УПГ УК')", false);
            ExecSQL(conn_db, "insert into t_actual (is_actual,remark) values(  12,'УПГ УК')", false);
            ExecSQL(conn_db, "insert into t_actual (is_actual,remark) values(  13,'УПГ УК')", false);
            ExecSQL(conn_db, "insert into t_actual (is_actual,remark) values(  21,'Рассчитано')", false);
            ExecSQL(conn_db, "insert into t_actual (is_actual,remark) values( 100,'Удалено')", false);
            #endregion
#if PG

            string services = Points.Pref + "_kernel." + DBManager.getServer(conn_db) + "services s ";
            string supplier = Points.Pref + "_kernel." + DBManager.getServer(conn_db) + "supplier p ";
            
#else
            string services = Points.Pref + "_kernel@" + DBManager.getServer(conn_db) + ":services s";
            string supplier = Points.Pref + "_kernel@" + DBManager.getServer(conn_db) + ":supplier p";
#endif
            sql.Append(" Insert into " + tXX_nedop_full);
            sql.Append(" (nzp_nedop, nzp_kvar");
            if (finder.nzp_dom > 0) sql.Append(", nzp_dom");
            sql.Append(", nzp_serv, service, nzp_supp, supplier, dat_s, dat_po, tn, comment, is_actual, remark, nzp_user, user_name, dat_when, nzp_kind, kind) ");
            sql.Append(" Select n.nzp_nedop, n.nzp_kvar ");
            if (finder.nzp_dom > 0) sql.Append(", kv.nzp_dom");
            sql.Append(", n.nzp_serv, s.service, n.nzp_supp, p.name_supp, n.dat_s, n.dat_po, n.tn, n.comment, n.is_actual, a.remark, n.nzp_user, u.comment, n.dat_when, n.nzp_kind, k.name ");

#if PG
            sql.Append(" From " );
            if (finder.nzp_dom > 0) sql.Append(" " + finder.pref + "_data.kvar kv, ");
            sql.Append( services +", "+ finder.pref + "_data.nedop_kvar n "+
                        " left outer join " + supplier + " on n.nzp_supp = p.nzp_supp " + 
                        " left outer join " + finder.pref + "_data.upg_s_kind_nedop k on  n.nzp_kind = k.nzp_kind " +
                        " left outer join " + Points.Pref + "_data.users u on n.nzp_user = u.nzp_user " +
                        " left outer join t_actual a on n.is_actual = a.is_actual ");
            
            sql.Append(" Where n.nzp_serv = s.nzp_serv and k.kod_kind = 1 " + filter );
#else
            sql.Append(" From " + finder.pref + "_data:nedop_kvar n ");
            if (finder.nzp_dom > 0) sql.Append(", " + finder.pref + "_data:kvar kv");
            sql.Append(", " + services +
                        ", outer " + supplier +
                        ", outer " + finder.pref + "_data:upg_s_kind_nedop k " +
                        ", outer " + Points.Pref + "_data:users u " +
                        ", outer t_actual a ");

            sql.Append(" Where n.nzp_serv = s.nzp_serv and n.nzp_supp = p.nzp_supp and n.nzp_kind = k.nzp_kind and k.kod_kind = 1 ");
            sql.Append(" and n.nzp_user = u.nzp_user and n.is_actual = a.is_actual " + filter);
#endif
            ret = ExecSQL(conn_db, sql.ToString(), true);
            if (!ret.result)
            {
                conn_db.Close();
                conn_web.Close();
                return;
            }

            conn_db.Close(); //закрыть соединение с основной базой

            if (key)
            {
                //далее работаем с кешем
                //создаем индексы на tXX_nedop
                string ix = "ix" + Convert.ToString(finder.nzp_user) + "_nedop";

                ret = ExecSQL(conn_web, " Create index " + ix + "_1 on " + tXX_nedop + " (nzp_nedop, nzp_kvar, is_actual) ", true);

#if PG
                if (ret.result) ret = ExecSQL(conn_web, " analyze  " + tXX_nedop, true);
#else
                if (ret.result) ret = ExecSQL(conn_web, " Update statistics for table  " + tXX_nedop, true);
#endif
            }

            conn_web.Close();

            return;
        }

        /// <summary>
        /// Поиск недопоставок в кэше
        /// </summary>
        /// <param name="finder">объект для поиска</param>
        /// <param name="ret">результат работы функции</param>
        /// <returns>Список недопоставок</returns>
        public List<Nedop> GetNedop(Nedop finder, out Returns ret)
        {
            if (Utils.GetParams(finder.prms, Constants.page_spisnddom))
            {
                return GetNedopDom(finder, out ret);
            }

            if (GlobalSettings.WorkOnlyWithCentralBank)
            {
                ret = new Returns(false, "Данные о недопоставках не доступны, т.к. установлен режим работы с центральным банком данных", -1);
                return null;
            }

            #region Проверка входных параметров
            if (finder.nzp_user < 1)
            {
                ret = new Returns(false, "Не определен пользователь");
                return null;
            }
            if (finder.pref == "")
            {
                ret = new Returns(false, "Не определен префикс БД");
                return null;
            }
            if (finder.nzp_kvar <= 0)
            {
                ret = new Returns(false, "Не определен л/с");
                return null;
            }
            #endregion

            ret = Utils.InitReturns();

            #region соединение с бд Webdata
            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result) return null;
            #endregion

            #region соединение с БД
            string connectionString = Points.GetConnByPref(finder.pref);
            IDbConnection conn_db = GetConnection(connectionString);
            ret = OpenDb(conn_db, true);
            if (!ret.result)
            {
                conn_web.Close();
                return null;
            }
            #endregion

#if PG
            if (!TempTableInWebCashe(conn_db, Points.Pref + "_data.users"))
            {
                ret.result = false;
                ret.text = "Данные о недопоставках временно не доступны";
                ret.tag = -1;
                conn_db.Close();
                conn_web.Close();
                return null;
            }
#else
            if (!TempTableInWebCashe(conn_db, Points.Pref + "_data:users"))
            {
                ret.result = false;
                ret.text = "Данные о недопоставках временно не доступны";
                ret.tag = -1;
                conn_db.Close();
                conn_web.Close();
                return null;
            }
#endif

            #region определение локального пользователя
            int nzpUser = finder.nzp_user;
            
            /*DbWorkUser db = new DbWorkUser();
            int nzpUser = db.GetLocalUser(conn_db, finder, out ret);
            db.Close();
            if (!ret.result)
            {
                conn_web.Close();
                conn_db.Close();
                return null;
            }*/
            #endregion

            string tXX_nedop = DBManager.sDefaultSchema + "t" + Convert.ToString(finder.nzp_user) + "_nedop n";

            string skip = "";
#if PG
            if (finder.skip > 0) skip = " offset " + finder.skip;
#else
            if (finder.skip > 0) skip = " skip " + finder.skip;
#endif

            string sWhere = " Where 1=1";
            if (finder.nzp_kvar > 0) sWhere += " and n.nzp_kvar = " + finder.nzp_kvar;
            if (finder.nzp_serv > 0) sWhere += " and n.nzp_serv = " + finder.nzp_serv;
            if (finder.nzp_supp > 0) sWhere += " and n.nzp_supp = " + finder.nzp_supp;
            if (finder.nzp_kind > 0) sWhere += " and n.nzp_kind = " + finder.nzp_kind;

            if (finder.is_actual > 0) sWhere += " and n.is_actual = " + finder.is_actual;
            else if (finder.is_actual < 0) sWhere += " and n.is_actual <> " + (-1 * finder.is_actual);

            IDataReader reader = null, reader2 = null;
            List<Nedop> listNedop = new List<Nedop>();

            try
            {
                #region определить число записей
                int total_record_count = 0;
                object count = ExecScalar(conn_web, " Select count(*) From " + tXX_nedop + sWhere, out ret, true);
                if (ret.result)
                {
                    total_record_count = Convert.ToInt32(count);
                }
                #endregion

                #region получить данные
                StringBuilder sql = new StringBuilder();
#if PG
                sql.Append(" Select " );
                sql.Append(" nzp_nedop, nzp_kvar, nzp_serv, service, nzp_supp, supplier, dat_s, dat_po, tn, comment, is_actual, remark, nzp_user, user_name, dat_when, nzp_kind, kind ");
                sql.Append(" From " + tXX_nedop + sWhere);

                if (finder.sortby == Constants.sortby_serv) sql.Append(" Order by service, dat_s desc ");
                else sql.Append(" Order by dat_s desc, service ");
                sql.Append( skip );
#else
                sql.Append(" Select " + skip);
                sql.Append(" nzp_nedop, nzp_kvar, nzp_serv, service, nzp_supp, supplier, dat_s, dat_po, tn, comment, is_actual, remark, nzp_user, user_name, dat_when, nzp_kind, kind ");
                sql.Append(" From " + tXX_nedop + sWhere);

                if (finder.sortby == Constants.sortby_serv) sql.Append(" Order by service, dat_s desc ");
                else sql.Append(" Order by dat_s desc, service ");
#endif
                if (!ExecRead(conn_web, out reader, sql.ToString(), true).result)
                {
                    conn_web.Close();
                    return null;
                }

                int i = 0;
                while (reader.Read())
                {
                    i++;

                    Nedop nedop = new Nedop();
                    nedop.num = (i + finder.skip).ToString();

                    #region Проверка блокировки записей
                    string bl = "";
                    if (i == 1)
                    {
                        string nedop_kvar = "nedop_kvar";
#if PG
                        string nedop_kvar_full = finder.pref + "_data." + nedop_kvar;
#else
                        string nedop_kvar_full = finder.pref + "_data:" + nedop_kvar;
#endif
                        if (TempTableInWebCashe(conn_db, nedop_kvar_full))
                        {
                            sql = new StringBuilder();
#if PG
                            sql.Append("Select n.dat_block, n.user_block, u.comment as user_name_block, ");
                            sql.Append("(now() - INTERVAL ' " + Constants.users_min.ToString() + " minutes') as cur_dat ");
                            sql.Append(" From " + nedop_kvar_full + " n ");
                            sql.Append(" left outer join " + Points.Pref + "_data.users u on n.user_block = u.nzp_user");                            
                            sql.Append(" Where n.nzp_kvar = " + finder.nzp_kvar + " ");
#else
                            sql.Append("Select n.dat_block, n.user_block, u.comment as user_name_block, (current year to second - " + Constants.users_min.ToString() + " units minute) as cur_dat");
                            sql.Append(" From " + nedop_kvar_full + " n");
                            sql.Append(", outer " + Points.Pref + "_data:users u ");
                            sql.Append(" Where n.nzp_kvar = " + finder.nzp_kvar);
                            sql.Append(" and n.user_block = u.nzp_user");
#endif
                            ret = ExecRead(conn_db, out reader2, sql.ToString(), true);
                            if (!ret.result) throw new Exception(ret.text);

                            if (reader2.Read())
                            {
                                DateTime dt_block = DateTime.MinValue;
                                DateTime dt_cur = DateTime.MinValue;
                                int user_block = 0;
                                string userNameBlock = "";

                                if (reader2["user_block"] != DBNull.Value) user_block = (int)reader2["user_block"]; //пользователь, который заблокировал
                                if (reader2["user_name_block"] != DBNull.Value) userNameBlock = ((string)reader2["user_name_block"]).Trim(); //имя пользователь, который заблокировал
                                if (reader2["dat_block"] != DBNull.Value) dt_block = Convert.ToDateTime(reader2["dat_block"]);//дата блокировки
                                if (reader2["cur_dat"] != DBNull.Value) dt_cur = Convert.ToDateTime(reader2["cur_dat"]);//текущее время/дата - 20 мин

                                if (user_block > 0 && dt_block != DateTime.MinValue) //заблокирован прибор учета
                                    if (user_block != nzpUser && dt_cur < dt_block) //если заблокирована запись другим пользователем и 20 мин не прошло
                                        bl = "Недопоставки заблокированы пользователем " + userNameBlock + " " + dt_block.ToString("dd.MM.yyyy в HH:mm") + ". Редактировать данные запрещено.";

                                if (bl == "") // действующей блокировки нет, или она сделана самим пользователем
                                {
                                    if (Utils.GetParams(finder.prms, Constants.act_mode_edit.ToString())) //если берут данные на изменение
                                    {
#if PG
                                        ret = ExecSQL(conn_db, "update " + nedop_kvar_full + " set dat_block = now() , user_block = " + nzpUser + " where nzp_kvar = " + finder.nzp_kvar, true);
#else
                                        ret = ExecSQL(conn_db, "update " + nedop_kvar_full + " set dat_block = current year to minute, user_block = " + nzpUser + " where nzp_kvar = " + finder.nzp_kvar, true);
#endif
                                    }
                                    else //если  на просмотр
                                    {
                                        ret = ExecSQL(conn_db, "update " + nedop_kvar_full + " set dat_block = null, user_block = null where nzp_kvar = " + finder.nzp_kvar, true);
                                    }
                                    if (!ret.result) throw new Exception("Ошибка обновления записи таблицы " + nedop_kvar);
                                }
                            }
                            if (reader2 != null) reader2.Close();
                        }
                        else
                        {
                            bl = "Банк данных с недопоставками не доступен";
                        }
                    }
                    nedop.block = bl;
                    #endregion

                    nedop.nzp_nedop = reader["nzp_nedop"] != DBNull.Value ? Convert.ToInt32(reader["nzp_nedop"]) : 0;

                    if (reader["nzp_kvar"] == DBNull.Value) nedop.nzp_kvar = 0;
                    else nedop.nzp_kvar = Convert.ToInt32(reader["nzp_kvar"]);

                    if (reader["nzp_serv"] == DBNull.Value) nedop.nzp_serv = 0;
                    else nedop.nzp_serv = Convert.ToInt32(reader["nzp_serv"]);

                    if (reader["service"] == DBNull.Value) nedop.service = "";
                    else nedop.service = Convert.ToString(reader["service"]);

                    if (reader["nzp_supp"] == DBNull.Value) nedop.nzp_supp = 0;
                    else nedop.nzp_supp = Convert.ToInt32(reader["nzp_supp"]);

                    if (reader["supplier"] == DBNull.Value) nedop.supplier = "";
                    else nedop.supplier = Convert.ToString(reader["supplier"]);

                    DateTime dat_s = DateTime.MinValue;
                    DateTime dat_po = DateTime.MaxValue;

                    if (reader["dat_s"] == DBNull.Value) nedop.dat_s = "";
                    else
                    {
                        nedop.dat_s = String.Format("{0:dd.MM.yyyy HH:mm}", reader["dat_s"]);
                        dat_s = Convert.ToDateTime(reader["dat_s"]);
                    }

                    if (reader["dat_po"] == DBNull.Value) nedop.dat_po = "";
                    else
                    {
                        nedop.dat_po = String.Format("{0:dd.MM.yyyy HH:mm}", reader["dat_po"]);
                        dat_po = Convert.ToDateTime(reader["dat_po"]);
                    }
                    if (nedop.dat_po == "01.01.3000")
                    {
                        nedop.dat_po = "";
                        dat_po = DateTime.MaxValue;
                    }

                    if (dat_s != DateTime.MinValue && dat_po != DateTime.MaxValue)
                    {
                        TimeSpan duration = dat_po - dat_s;
                        if (duration.Days > 0) nedop.duration += duration.Days + " д.";
                        if (duration.Hours > 0) nedop.duration += " " + duration.Hours + " ч.";
                        if (duration.Minutes > 0) nedop.duration += " " + duration.Minutes + " мин.";
                    }

                    if (reader["tn"] == DBNull.Value) nedop.tn = "";
                    else nedop.tn = Convert.ToString(reader["tn"]);

                    if (reader["comment"] == DBNull.Value) nedop.comment = "";
                    else nedop.comment = Convert.ToString(reader["comment"]);

                    if (reader["is_actual"] == DBNull.Value) nedop.is_actual = 0;
                    else nedop.is_actual = Convert.ToInt32(reader["is_actual"]);

                    if (reader["remark"] == DBNull.Value) nedop.remark = "";
                    else nedop.remark = Convert.ToString(reader["remark"]);

                    if (reader["nzp_user"] == DBNull.Value) nedop.nzp_user_when = 0;
                    else nedop.nzp_user_when = Convert.ToInt32(reader["nzp_user"]);

                    if (reader["dat_when"] == DBNull.Value) nedop.dat_when = "";
                    else
                    {
                        nedop.dat_when = String.Format("{0:dd.MM.yyyy}", reader["dat_when"]);
                        if (reader["user_name"] != DBNull.Value) nedop.dat_when += " (" + Convert.ToString(reader["user_name"]).Trim() + ")";
                    }

                    if (reader["nzp_kind"] == DBNull.Value) nedop.nzp_kind = 0;
                    else nedop.nzp_kind = Convert.ToInt32(reader["nzp_kind"]);

                    if (reader["kind"] == DBNull.Value) nedop.kind = "";
                    else nedop.kind = Convert.ToString(reader["kind"]);

                    listNedop.Add(nedop);

                    if (i >= finder.rows) break;
                }
                #endregion
                reader.Close();
                conn_web.Close();
                ret.tag = total_record_count;
                return listNedop;
            }
            catch (Exception ex)
            {
                if (reader != null) reader.Close();
                if (reader2 != null) reader2.Close();
                conn_web.Close();
                conn_db.Close();
                ret = new Returns(false, ex.Message);
                MonitorLog.WriteLog("Ошибка поиска недопоставок GetNedop " + (Constants.Viewerror ? "\n " + ex.Message : ""), MonitorLog.typelog.Error, 20, 201, true);
                return null;
            }
        }

        private List<Nedop> GetNedopDom(Nedop finder, out Returns ret)
        {
            #region Проверка входных параметров
            if (finder.nzp_user < 1)
            {
                ret = new Returns(false, "Не определен пользователь");
                return null;
            }
            if (finder.pref == "")
            {
                ret = new Returns(false, "Не определен префикс БД");
                return null;
            }
            if (finder.nzp_dom <= 0)
            {
                ret = new Returns(false, "Не определен дом");
                return null;
            }
            DateTime ds = DateTime.MinValue;
            if (!DateTime.TryParse(finder.dat_s, out ds))
            {
                ret = new Returns(false, "Неверно задана дата начала недопоставки");
                return null;
            }
            DateTime dpo = DateTime.MaxValue;
            if (!DateTime.TryParse(finder.dat_po, out dpo))
            {
                ret = new Returns(false, "Неверно задана дата окончания недопоставки");
                return null;
            }
            #endregion

            ret = Utils.InitReturns();

            #region соединение с бд Webdata
            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result) return null;
            #endregion

            string tXX_nedop = DBManager.sDefaultSchema + "t" + Convert.ToString(finder.nzp_user) + "_nedop n";

            string sWhere = " Where 1=1";
            if (finder.nzp_dom > 0) sWhere += " and n.nzp_dom = " + finder.nzp_dom;
            if (finder.nzp_serv > 0) sWhere += " and n.nzp_serv = " + finder.nzp_serv;
            if (finder.nzp_supp > 0) sWhere += " and n.nzp_supp = " + finder.nzp_supp;
            if (finder.nzp_kind > 0) sWhere += " and n.nzp_kind = " + finder.nzp_kind;
            sWhere += " and n.dat_s <= " + Utils.EStrNull(dpo.ToString("yyyy-MM-dd HH:00")) + " and n.dat_po >= " + Utils.EStrNull(ds.ToString("yyyy-MM-dd HH:00"));

            if (finder.is_actual > 0) sWhere += " and n.is_actual = " + finder.is_actual;
            else if (finder.is_actual < 0) sWhere += " and n.is_actual <> " + (-1 * finder.is_actual);

            string groupBy = "";
            if (finder.nzp_dom > 0)
                groupBy += " Group by nzp_serv, service, nzp_supp, supplier, dat_s, dat_po, tn, nzp_kind, kind, comment";

            IDataReader reader = null;
            List<Nedop> listNedop = new List<Nedop>();

            try
            {
                #region определить число записей
                int total_record_count = 0;
                ret = ExecRead(conn_web, out reader, "Select nzp_serv, service, nzp_supp, supplier, dat_s, dat_po, tn, nzp_kind, kind, comment From " + tXX_nedop + sWhere + groupBy, true);
                if (!ret.result)
                {
                    conn_web.Close();
                    return null;
                }
                while (reader.Read()) total_record_count++;
                reader.Close();
                #endregion

                #region получить данные
                StringBuilder sql = new StringBuilder();

                sql.Append("Select nzp_serv, service, nzp_supp, supplier, dat_s, dat_po, tn, nzp_kind, kind, comment, count(*) as cnt_ls");
                sql.Append(" From " + tXX_nedop + sWhere + groupBy);

                if (finder.sortby == Constants.sortby_serv) sql.Append(" Order by service, dat_s desc");
                else sql.Append(" Order by dat_s desc, service");

                if (!ExecRead(conn_web, out reader, sql.ToString(), true).result)
                {
                    conn_web.Close();
                    return null;
                }

                int i = 0;
                while (reader.Read())
                {
                    i++;

                    if (finder.nzp_dom > 0 && finder.skip > 0 && i <= finder.skip) continue;

                    Nedop nedop = new Nedop();
                    nedop.num = i.ToString();
                    nedop.nzp_serv = reader["nzp_serv"] != DBNull.Value ? Convert.ToInt32(reader["nzp_serv"]) : 0;
                    nedop.service = reader["service"] != DBNull.Value ? Convert.ToString(reader["service"]) : "";
                    nedop.comment = reader["comment"] != DBNull.Value ? Convert.ToString(reader["comment"]) : "";
                    nedop.nzp_supp = reader["nzp_supp"] != DBNull.Value ? Convert.ToInt32(reader["nzp_supp"]) : 0;
                    nedop.supplier = reader["supplier"] != DBNull.Value ? Convert.ToString(reader["supplier"]) : "";

                    DateTime dat_s = DateTime.MinValue;
                    DateTime dat_po = DateTime.MaxValue;

                    if (reader["dat_s"] == DBNull.Value) nedop.dat_s = "";
                    else
                    {
                        nedop.dat_s = String.Format("{0:dd.MM.yyyy HH:mm}", reader["dat_s"]);
                        dat_s = Convert.ToDateTime(reader["dat_s"]);
                    }

                    if (reader["dat_po"] == DBNull.Value) nedop.dat_po = "";
                    else
                    {
                        nedop.dat_po = String.Format("{0:dd.MM.yyyy HH:mm}", reader["dat_po"]);
                        dat_po = Convert.ToDateTime(reader["dat_po"]);
                    }
                    if (nedop.dat_po == "01.01.3000")
                    {
                        nedop.dat_po = "";
                        dat_po = DateTime.MaxValue;
                    }

                    if (dat_s != DateTime.MinValue && dat_po != DateTime.MaxValue)
                    {
                        TimeSpan duration = dat_po - dat_s;
                        if (duration.Days > 0) nedop.duration += duration.Days + " д.";
                        if (duration.Hours > 0) nedop.duration += " " + duration.Hours + " ч.";
                        if (duration.Minutes > 0) nedop.duration += " " + duration.Minutes + " мин.";
                    }

                    nedop.tn = reader["tn"] != DBNull.Value ? Convert.ToString(reader["tn"]) : "";
                    nedop.nzp_kind = reader["nzp_kind"] != DBNull.Value ? Convert.ToInt32(reader["nzp_kind"]) : 0;
                    nedop.kind = reader["kind"] != DBNull.Value ? Convert.ToString(reader["kind"]) : "";
                    nedop.cnt_ls = reader["cnt_ls"] != DBNull.Value ? Convert.ToInt32(reader["cnt_ls"]) : 0;

                    listNedop.Add(nedop);

                    if (finder.rows > 0 && i >= finder.rows + finder.skip) break;
                }
                #endregion
                reader.Close();
                conn_web.Close();
                ret.tag = total_record_count;
                return listNedop;
            }
            catch (Exception ex)
            {
                if (reader != null) reader.Close();
                conn_web.Close();
                ret = new Returns(false, ex.Message);
                MonitorLog.WriteLog("Ошибка поиска недопоставок GetNedop " + (Constants.Viewerror ? "\n " + ex.Message : ""), MonitorLog.typelog.Error, 20, 201, true);
                return null;
            }
        }

        public List<_Service> GetServicesForNedop(Nedop finder, out Returns ret)
        {
            ret = Utils.InitReturns();

            if (finder.nzp_user < 1)
            {
                ret.result = false;
                ret.text = "Не определен пользователь";
                ret.tag = -1;
                return null;
            }

            /*if (finder.pref == "")
            {
                ret.result = false;
                ret.text = "Не определен префикс";
                ret.tag = -2;
                return null;
            }*/

            string filter = "";

            #region фильтр по услугам
            if (finder.RolesVal != null)
                foreach (_RolesVal role in finder.RolesVal)
                    if (role.tip == Constants.role_sql)
                        if (role.kod == Constants.role_sql_serv) filter += " and s.nzp_serv in (" + role.val + ") ";
            #endregion

            string connectionString = Points.GetConnByPref(finder.pref == "" ? Points.Pref : finder.pref);
            IDbConnection conn_db = GetConnection(connectionString);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return null;

            string dop = "";
            string sql = "";
            if (finder.pref != "")
            {
                if (finder.nzp_kvar > 0) dop += " and nzp_kvar = " + finder.nzp_kvar.ToString();

                ExecSQL(conn_db, "drop table tarif_services", false);
                string sqls = "create temp table tarif_services( nzp_serv integer)" + DBManager.sUnlogTempTable;
                ret = ExecSQL(conn_db, sqls, true);
                if (!ret.result)
                {
                    conn_db.Close();
                    return null;
                }

                ret = ExecSQL(conn_db, " insert into tarif_services" +
                                       " select distinct nzp_serv " +
                                       " from " + finder.pref + DBManager.sDataAliasRest+ "tarif " +
                                       " where is_actual <> 100 " + dop + " ; ", true);
                if (!ret.result)
                {
                    conn_db.Close();
                    return null;
                }
#if PG
                sql = "select distinct nt.nzp_parent, s.service from " + finder.pref + "_data.upg_s_kind_nedop nt, " + finder.pref + "_kernel.services s, tarif_services t" +
                                    " where s.nzp_serv = t.nzp_serv and nt.kod_kind=1 and nt.nzp_parent = s.nzp_serv " + filter + " order by s.service";
#else
                sql = "select distinct nt.nzp_parent, s.service from " + finder.pref + "_data:upg_s_kind_nedop nt, " + finder.pref + "_kernel:services s, tarif_services t" +
                                    " where s.nzp_serv = t.nzp_serv and nt.kod_kind=1 and nt.nzp_parent = s.nzp_serv " + filter + " order by s.service";
#endif
            }
            else
            {
#if PG
                sql = "select distinct nt.nzp_parent, s.service from " + Points.Pref + "_data.upg_s_kind_nedop nt, " + Points.Pref + "_kernel.services s" +
                                   " where nt.kod_kind=1 and nt.nzp_parent = s.nzp_serv " + filter + " order by s.service";
#else
                sql = "select distinct nt.nzp_parent, s.service from " + Points.Pref + "_data:upg_s_kind_nedop nt, " + Points.Pref + "_kernel:services s" +
                                   " where nt.kod_kind=1 and nt.nzp_parent = s.nzp_serv " + filter + " order by s.service";
#endif
            }

            IDataReader reader;
            ret = ExecRead(conn_db, out reader, sql, true);
            if (!ret.result)
            {
                if (finder.pref != "") ExecSQL(conn_db, "drop table tarif_services", true);
                conn_db.Close();
                return null;
            }

            List<_Service> list = new List<_Service>();
            try
            {
                _Service service;
                while (reader.Read())
                {
                    service = new _Service();

                    if (reader["nzp_parent"] != DBNull.Value) service.nzp_serv = (int)reader["nzp_parent"];
                    if (reader["service"] != DBNull.Value) service.service = (string)reader["service"];

                    list.Add(service);
                }
                reader.Close();
            }
            catch (Exception ex)
            {
                if (finder.pref != "") ExecSQL(conn_db, "drop table tarif_services", true);
                conn_db.Close();
                ret = new Returns(false, ex.Message);
                MonitorLog.WriteLog("Ошибка поиска услуг GetServicesForNedop " + (Constants.Viewerror ? "\n " + ex.Message : ""), MonitorLog.typelog.Error, 20, 201, true);
                return null;
            }

            if (finder.pref != "") ExecSQL(conn_db, "drop table tarif_services", true);

            conn_db.Close();
            return list;
        }

        public List<NedopType> GetNedopTypeForNedop(Nedop finder, out Returns ret)
        {
            ret = Utils.InitReturns();

            if (finder.nzp_user < 1)
            {
                ret.result = false;
                ret.text = "Не определен пользователь";
                ret.tag = -1;
                return null;
            }

            if (finder.pref == "") finder.pref = Points.Pref;
            /*if (finder.pref == "")
            {
                ret.result = false;
                ret.text = "Не определен префикс";
                ret.tag = -2;
                return null;
            }*/

            if (finder.nzp_serv <= 0)
            {
                ret.result = false;
                ret.text = "Не определена услуга";
                ret.tag = -3;
                return null;
            }

            string sql = "select nt.name, nt.nzp_kind, nt.value_ as val, nt.is_param from " +
                finder.pref + "_data"+tableDelimiter+"upg_s_kind_nedop nt" +
                " where nt.kod_kind=1 and nt.nzp_parent = " + finder.nzp_serv + " order by nt.name";

            string conn_kernel = Points.GetConnByPref(finder.pref);
            IDbConnection conn_db = GetConnection(conn_kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return null;

            IDataReader reader;

            if (!ExecRead(conn_db, out reader, sql, true).result)
            {
                conn_db.Close();
                return null;
            }

            List<NedopType> list = new List<NedopType>();
            try
            {
                NedopType nedoptype;
                while (reader.Read())
                {
                    nedoptype = new NedopType();

                    if (reader["nzp_kind"] != DBNull.Value) nedoptype.nzp_kind = (int)reader["nzp_kind"];
                    if (reader["val"] != DBNull.Value) nedoptype.value_ = Convert.ToDecimal(reader["val"]);
                    if (reader["name"] != DBNull.Value) nedoptype.nedop_name = (string)reader["name"];
                    if (reader["is_param"] != DBNull.Value) nedoptype.is_param = (int)reader["is_param"];
                    nedoptype.key = nedoptype.nzp_kind + "_" + nedoptype.is_param+"_" + nedoptype.value_;
                    list.Add(nedoptype);
                }
                reader.Close();
            }
            catch (Exception ex)
            {
                reader.Close();
                conn_db.Close();

                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;

                MonitorLog.WriteLog("Ошибка поиска услуг GetNedopTypeForNedop " + err, MonitorLog.typelog.Error, 20, 201, true);

                return null;
            }

            conn_db.Close();
            return list;
        }

        /// <summary>
        /// возвращает список причин недопоставки
        /// </summary>
        /// <param name="finder"></param>
        /// <param name="ret"></param>
        /// <returns></returns>
        public List<NedopType> GetNedopWorkType(Nedop finder, out Returns ret)
        {
            ret = Utils.InitReturns();

            if (finder.nzp_user < 1)
            {
                ret.result = false;
                ret.text = "Не определен пользователь";
                ret.tag = -1;
                return null;
            }

            if (finder.pref == "") finder.pref = Points.Pref;
#if PG
            string sql = "select nzp_work_type, name_work_type from " + finder.pref + "_supg.s_work_type order by nzp_work_type";
#else
            string sql = "select nzp_work_type, name_work_type from " + finder.pref + "_supg:s_work_type order by nzp_work_type";
#endif
            string conn_kernel = Points.GetConnByPref(finder.pref);
            IDbConnection conn_db = GetConnection(conn_kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return null;

            IDataReader reader;

            if (!ExecRead(conn_db, out reader, sql, true).result)
            {
                conn_db.Close();
                return null;
            }

            List<NedopType> list = new List<NedopType>();
            try
            {
                NedopType nedoptype;
                while (reader.Read())
                {
                    nedoptype = new NedopType();

                    if (reader["nzp_work_type"] != DBNull.Value) nedoptype.nzp_work_type = (int)reader["nzp_work_type"];
                    if (reader["name_work_type"] != DBNull.Value) nedoptype.name_work_type = (string)reader["name_work_type"];
                    list.Add(nedoptype);
                }
                reader.Close();
            }
            catch (Exception ex)
            {
                reader.Close();
                conn_db.Close();

                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;
                MonitorLog.WriteLog("Ошибка поиска услуг GetNedopWorkType " + err, MonitorLog.typelog.Error, 20, 201, true);
                return null;
            }
            conn_db.Close();
            return list;
        }

        public List<Nedop> GetNedopDependencies(Nedop finder, IDbConnection conn_db, IDbTransaction transaction)
        {
            Returns ret = Utils.InitReturns();
            List<Nedop> lstNedop = new List<Nedop>();
            lstNedop.Add(finder);

            IDataReader reader = null;

#warning ошибка в select
#if PG
            string strSqlQuiery = String.Format("SELECT nzp_serv_slave FROM {0}_data.dep_servs WHERE nzp_serv = {1} AND is_actual = 1 AND (DATE {2}, DATE {3}) OVERLAPS (dat_s, dat_po);", Points.Pref, finder.nzp_serv, Utils.EDateNull(finder.dat_s), Utils.EDateNull(finder.dat_po));
#else
            string strSqlQuiery = String.Format("SELECT nzp_serv_slave FROM {0}_data:dep_servs WHERE nzp_serv = {1} AND is_actual = 1 AND {2} >= dat_s AND {3} < dat_po;", 
                Points.Pref, 
                finder.nzp_serv, 
                String.IsNullOrEmpty(finder.dat_s.Trim()) ? "'01.01.1990'" : "'" + Convert.ToDateTime(finder.dat_s).ToString("dd.MM.yyyy") + "'",
                String.IsNullOrEmpty(finder.dat_po.Trim()) ? "'01.01.3000'" : "'" + Convert.ToDateTime(finder.dat_po).ToString("dd.MM.yyyy") + "'"
                //Utils.EDateNull(finder.dat_po)
                );
#endif
            ret = ExecRead(conn_db, transaction, out reader, strSqlQuiery, true);
            if (ret.result)
            {
                while (reader.Read())
                {
                    int nzp_serv_slave = 0;
                    if (reader["nzp_serv_slave"] != DBNull.Value) if (Int32.TryParse(reader["nzp_serv_slave"].ToString(), out nzp_serv_slave))
                        {
                            Nedop item = new Nedop();
                            finder.CopyTo(item);
                            item.nzp_kvar = finder.nzp_kvar;
                            item.prms = finder.prms;
                            item.nzp_serv = nzp_serv_slave;
                            lstNedop.Add(item);
                        }
                }
                if (reader != null) reader.Close();
            }

            return lstNedop;
        }

        public List<Nedop> GetNedopDependencies(Nedop finder)
        {
            Returns ret = Utils.InitReturns();
            List<Nedop> lstNedop = new List<Nedop>();

            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);

            conn_db.Open();
            IDbTransaction transaction = null;
            try
            {
                lstNedop = GetNedopDependencies(finder, conn_db, transaction);
            }
            catch { }
            finally { conn_db.Close(); }
            return lstNedop;
        }

        public Returns SaveNedop(Nedop finder, Nedop additionalFinder)
        {
            //IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
            //Returns ret = OpenDb(conn_db, true);
            //if (!ret.result) return ret;

            //IDbTransaction transaction;
            //try { transaction = conn_db.BeginTransaction(); }
            //catch { transaction = null; }

            //List<Nedop> lstNedop = GetNedopDependencies(finder, conn_db, transaction);
            //foreach (Nedop item in lstNedop) SaveNedop(item, additionalFinder, conn_db, transaction, out ret);
            //// SaveNedop(finder, additionalFinder, conn_db, transaction, out ret);

            //if (transaction != null)
            //{
            //    if (ret.result) transaction.Commit();
            //    else transaction.Rollback();
            //}

            //conn_db.Close();
            //return ret;
            Returns ret;
            using (var dbNedopSave = new DbNedopSave())
            {
                ret = dbNedopSave.SaveNedop(finder, additionalFinder);
            }
            return ret;
        }

        /// <summary>
        /// групповая операция сохранение недопоставок
        /// </summary>
        /// <param name="finder"></param>
        /// <param name="ret"></param>
        public void SaveNedop(Nedop finder, Nedop additionalFinder, IDbConnection conn_db, IDbTransaction transaction, out Returns ret)
        {
            #region проверка входных параметров
            if (finder.nzp_user < 1)
            {
                ret = new Returns(false, "Не определен пользователь");
                return;
            }
            if (finder.pref.Trim() == "")
            {
                ret = new Returns(false, "Не задан префикс базы данных");
                return;
            }
            if (finder.nzp_serv < 1)
            {
                ret = new Returns(false, "Не задана услуга");
                return;
            }
            if (!Utils.GetParams(finder.prms, Constants.act_add_nedop) && !Utils.GetParams(finder.prms, Constants.act_del_nedop))
            {
                ret = new Returns(false, "Не задана операция");
                return;
            }
            if (Utils.GetParams(finder.prms, Constants.act_add_nedop) && finder.nzp_kind <= 0)
            {
                ret = new Returns(false, "Не задан тип недопоставки");
                return;
            }
            if (!Utils.GetParams(finder.prms, Constants.page_groupnedop) &&
                !Utils.GetParams(finder.prms, Constants.page_group_nedop_dom) &&
                !Utils.GetParams(finder.prms, Constants.page_spisnddom) &&
                finder.nzp_kvar < 1)
            {
                ret = new Returns(false, "Не задан режим работы");
                return;
            }
            DateTime ds = DateTime.MinValue;
            DateTime dpo = DateTime.MaxValue;
            if (Utils.GetParams(finder.prms, Constants.page_spisnddom))
            {
                if (additionalFinder == null)
                {
                    ret = new Returns(false, "Неверные параметры поиска");
                    return;
                }
                if (additionalFinder.nzp_dom < 1 ||
                    additionalFinder.nzp_serv < 1 ||
                    //additionalFinder.nzp_supp < 1 ||
                    additionalFinder.dat_s == "" ||
                    additionalFinder.dat_po == "" ||
                    additionalFinder.nzp_kind < 1)
                {
                    ret = new Returns(false, "Не заданы условия поиска");
                    return;
                }
                if (!DateTime.TryParse(additionalFinder.dat_s, out ds))
                {
                    ret = new Returns(false, "Неверная дата начала периода");
                    return;
                }
                if (!DateTime.TryParse(additionalFinder.dat_po, out dpo))
                {
                    ret = new Returns(false, "Неверная дата окончания периода");
                    return;
                }
            }
            #endregion

            ret = Utils.InitReturns();

            string pref = finder.pref.Trim();

            string tXX_sp = "";
            if (Utils.GetParams(finder.prms, Constants.page_groupnedop)) tXX_sp = "t" + Convert.ToString(finder.nzp_user) + "_spls";
            else if (Utils.GetParams(finder.prms, Constants.page_group_nedop_dom) && finder.nzp_dom < 1) tXX_sp = "t" + Convert.ToString(finder.nzp_user) + "_spdom";

            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result) return;

            if (tXX_sp != "")
                if (!TableInWebCashe(conn_web, tXX_sp))
                {
                    conn_web.Close();
                    ret.result = false;
                    ret.text = "Данные не выбраны";
                    return;
                }

            /* IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
             ret = OpenDb(conn_db, true);
             if (!ret.result)
             {
                 conn_web.Close();
                 return;
             }*/
#if PG
            string tXX_sp_full = conn_web.Database + ".public." + tXX_sp;
            string kvar = pref + "_data.kvar";
#else
            string tXX_sp_full = conn_web.Database + "@" + DBManager.getServer(conn_web) + ":" + tXX_sp;
            string kvar = pref + "_data:kvar";
#endif
            string kvar_filter = " 1=0 ";
            string dom_filter = " 1=0 ";
            if (finder.nzp_kvar > 0)
            {
                kvar_filter = "nzp_kvar = " + finder.nzp_kvar;
            }
            else if (Utils.GetParams(finder.prms, Constants.page_groupnedop))
            {
                kvar_filter = "nzp_kvar in (select nzp_kvar from " + tXX_sp_full + " where pref = " + Utils.EStrNull(pref) + " and  mark = 1) ";
            }
            else if (Utils.GetParams(finder.prms, Constants.page_group_nedop_dom))
            {
                if (finder.nzp_dom > 0)
                {
                    kvar_filter = "nzp_kvar in (select b.nzp_kvar from " + kvar + " b where b.nzp_dom = " + finder.nzp_dom + ") ";
                    dom_filter = "nzp_dom = " + finder.nzp_dom;
                }
                else
                {
                    kvar_filter = "nzp_kvar in (select b.nzp_kvar from " + tXX_sp_full + " a, " + kvar + " b where a.pref = " + Utils.EStrNull(pref) + " and a.mark = 1 and a.nzp_dom = b.nzp_dom) ";
                    dom_filter = "nzp_dom in (select nzp_dom from " + tXX_sp_full + " where pref = " + Utils.EStrNull(pref) + " and mark = 1) ";
                }
            }
            else if (Utils.GetParams(finder.prms, Constants.page_spisnddom))
            {
#if PG
                kvar_filter = "nzp_kvar in (select nk.nzp_kvar from " + pref + "_data.nedop_kvar nk, " + pref + "_data.kvar kv where kv.nzp_kvar = nk.nzp_kvar" +
                                    " and kv.nzp_dom = " + additionalFinder.nzp_dom +
                                    " and nk.nzp_serv = " + additionalFinder.nzp_serv;
#else
                kvar_filter = "nzp_kvar in (select nk.nzp_kvar from " + pref + "_data:nedop_kvar nk, " + pref + "_data:kvar kv where kv.nzp_kvar = nk.nzp_kvar" +
                   " and kv.nzp_dom = " + additionalFinder.nzp_dom +
                   " and nk.nzp_serv = " + additionalFinder.nzp_serv;
#endif

                if (additionalFinder.nzp_supp > 0) kvar_filter += " and nk.nzp_supp = " + additionalFinder.nzp_supp;
                else kvar_filter += " and (nk.nzp_supp is null or nk.nzp_supp = 0)";
                kvar_filter += " and nk.nzp_kind = " + additionalFinder.nzp_kind +
                    " and nk.dat_s = " + Utils.EStrNull(ds.ToString("yyyy-MM-dd HH:00")) +
                    " and nk.dat_po = " + Utils.EStrNull(dpo.ToString("yyyy-MM-dd HH:00"));
                if (additionalFinder.tn.Trim() != "") kvar_filter += " and nk.tn = " + Utils.EStrNull(additionalFinder.tn.Trim());
                else kvar_filter += " and (nk.tn is null or nk.tn = '')";
                kvar_filter += ") ";
            }

            string t_kvar = "t_nk_selected";
            ExecSQL(conn_db, transaction, "drop table " + t_kvar, false);

#if PG
            string sql = "select nzp_kvar into temp " + t_kvar + " from  " + finder.pref + "_data.nedop_kvar where " + kvar_filter + " ";
#else
            string sql = "select nzp_kvar from " + finder.pref + "_data:nedop_kvar where " + kvar_filter +
                " into temp " + t_kvar + " with no log";
#endif


            ret = ExecSQL(conn_db, transaction, sql, true);
            if (!ret.result)
            {
                conn_web.Close();
                return;
            }
            ret = ExecSQL(conn_db, transaction, "create index ix_" + t_kvar + "_1 on " + t_kvar + " (nzp_kvar) ", false);
#if PG
            if (ret.result) ret = ExecSQL(conn_web, transaction, " analyze " + t_kvar, false);
#else
            if (ret.result) ret = ExecSQL(conn_web, transaction, " Update statistics for table " + t_kvar, false);
#endif
            EditInterData editData = new EditInterData();

            //указываем таблицу для редактирования
            editData.pref = pref;
            editData.nzp_wp = finder.nzp_wp;
            editData.nzp_user = finder.nzp_user;
            editData.webLogin = finder.webLogin;
            editData.webUname = finder.webUname;
            editData.primary = "nzp_nedop";
            editData.table = "nedop_kvar";
            editData.todelete = Utils.GetParams(finder.prms, Constants.act_del_nedop);

            //указываем вставляемый период
            editData.dat_s = finder.dat_s;
            editData.dat_po = finder.dat_po;
            editData.intvType = enIntvType.intv_Hour;

            if (finder.nzp_kvar > 0)
            {
                editData.dopFind = new List<string>();
                sql = " and nzp_kvar = " + finder.nzp_kvar + " and nzp_serv = " + finder.nzp_serv;
                if (editData.todelete && finder.nzp_kind > 0)
                    sql += " and nzp_kind = " + finder.nzp_kind;
                editData.dopFind.Add(sql);

                //перечисляем ключевые поля и значения (со знаком сравнения!)
                Dictionary<string, string> keys = new Dictionary<string, string>();
                keys.Add("nzp_kvar", "1|nzp_kvar|kvar|nzp_kvar=" + finder.nzp_kvar); //ссылка на ключевую таблицу
                keys.Add("nzp_serv", "2|" + finder.nzp_serv);
                if (editData.todelete && finder.nzp_kind > 0)
                    keys.Add("nzp_kind", "2|" + finder.nzp_kind);

                editData.keys = keys;

                //перечисляем поля и значения этих полей, которые вставляются
                Dictionary<string, string> vals = new Dictionary<string, string>();
                vals.Add("tn", finder.tn);
                vals.Add("comment", finder.comment);
                if (!editData.todelete || finder.nzp_kind <= 0)
                    vals.Add("nzp_kind", finder.nzp_kind.ToString());
                vals.Add("nzp_supp", finder.nzp_supp.ToString());
                editData.vals = vals;
            }
            else if (Utils.GetParams(finder.prms, Constants.page_spisnddom))
            {
                //условие выборки данных из целевой таблицы           
                sql = " and nzp_serv = " + additionalFinder.nzp_serv;
                if (additionalFinder.nzp_supp > 0) sql += " and nzp_supp = " + additionalFinder.nzp_supp;
                else sql += " and (nzp_supp is null or nzp_supp = 0)";
                sql += " and nzp_kind = " + additionalFinder.nzp_kind +
                    " and dat_s = " + Utils.EStrNull(ds.ToString("yyyy-MM-dd HH:00")) +
                    " and dat_po = " + Utils.EStrNull(dpo.ToString("yyyy-MM-dd HH:00"));
                if (additionalFinder.tn.Trim() != "") sql += " and tn = " + Utils.EStrNull(additionalFinder.tn.Trim());
                else sql += " and (tn is null or tn = '')";
#if PG
                sql += " and nzp_kvar in (select nzp_kvar from " + pref + "_data.kvar where nzp_dom = " + additionalFinder.nzp_dom + ") ";
#else
                sql += " and nzp_kvar in (select nzp_kvar from " + pref + "_data:kvar where nzp_dom = " + additionalFinder.nzp_dom + ") ";
#endif
                editData.dopFind = new List<string>();
                editData.dopFind.Add(sql);

                //перечисляем ключевые поля и значения (со знаком сравнения!)
                editData.keys = new Dictionary<string, string>();
                editData.keys.Add("nzp_kvar", "1|nzp_kvar|kvar|" + kvar_filter); //ссылка на ключевую таблицу

                //перечисляем поля и значения этих полей, которые вставляются
                editData.vals = new Dictionary<string, string>();
                editData.vals.Add("nzp_serv", finder.nzp_serv.ToString());
                if (finder.nzp_supp > 0) editData.vals.Add("nzp_supp", finder.nzp_supp.ToString());
                editData.vals.Add("nzp_kind", finder.nzp_kind.ToString());
                if (finder.tn.Trim() != "") editData.vals.Add("tn", finder.tn.Trim());
            }
            else
            {
                //условие выборки данных из целевой таблицы           
                sql = " and nzp_serv = " + finder.nzp_serv;
                if (editData.todelete && finder.nzp_kind > 0) sql = " and nzp_kind = " + finder.nzp_kind;
                sql += " and " + kvar_filter;
                editData.dopFind = new List<string>();
                editData.dopFind.Add(sql);

                //перечисляем ключевые поля и значения (со знаком сравнения!)
                editData.keys = new Dictionary<string, string>();

                if (Utils.GetParams(finder.prms, Constants.page_groupnedop)) editData.keys.Add("nzp_kvar", "1|nzp_kvar|kvar|" + kvar_filter); //ссылка на ключевую таблицу
                else if (Utils.GetParams(finder.prms, Constants.page_group_nedop_dom)) editData.keys.Add("nzp_kvar", "1|nzp_kvar|kvar|" + dom_filter); //ссылка на ключевую таблицу

                editData.keys.Add("nzp_serv", "2|" + finder.nzp_serv);
                if (Utils.GetParams(finder.prms, Constants.act_del_nedop) && finder.nzp_kind > 0) editData.keys.Add("nzp_kind", "2|" + finder.nzp_kind);

                //перечисляем поля и значения этих полей, которые вставляются
                editData.vals = new Dictionary<string, string>();
                editData.vals.Add("tn", finder.tn);
                if (finder.comment.Trim() != "") editData.vals.Add("comment", finder.comment);
                if (Utils.GetParams(finder.prms, Constants.act_add_nedop) || finder.nzp_kind <= 0) editData.vals.Add("nzp_kind", finder.nzp_kind.ToString());
                editData.vals.Add("nzp_supp", finder.nzp_supp.ToString());
            }

            //вызов сервиса
            DbEditInterData db = new DbEditInterData();
            db.Saver(conn_db, transaction, editData, out ret);

            if (editData.todelete)
            {
                #region Добавление в sys_events события 'Удаление недопоставки'
                DbAdmin.InsertSysEvent(new SysEvents()
                {
                    pref = finder.pref,
                    nzp_user = finder.nzp_user,
                    nzp_dict = 6603,
                    nzp_obj = finder.nzp_kvar,
                    note = "Период с " + finder.dat_s + " по " + finder.dat_po + ". " + (finder.tn != "" ? "Процент снятия: " + finder.tn + ". " : "")
                }, transaction, conn_db);
                #endregion
            }
            else
            {
                #region Добавление в sys_events события 'Добавление недопоставки'
                DbAdmin.InsertSysEvent(new SysEvents()
                {
                    pref = finder.pref,
                    nzp_user = finder.nzp_user,
                    nzp_dict = 6602,
                    nzp_obj = finder.nzp_kvar,
                    note = "Период с " + finder.dat_s + " по " + finder.dat_po + ". " + (finder.tn != "" ? "Процент снятия: " + finder.tn + ". " : "")
                }, transaction, conn_db);
                #endregion
            }

            if (ret.result)
            {
                #region Добавление признаков перерасчетов
                if (Points.RecalcMode == RecalcModes.AutomaticWithCancelAbility)
                {
                    EditInterDataMustCalc eid = new EditInterDataMustCalc();

                    eid.mcalcType = enMustCalcType.Nedop;
                    eid.nzp_wp = editData.nzp_wp;
                    eid.pref = editData.pref;
                    eid.nzp_user = editData.nzp_user;
                    eid.webLogin = editData.webLogin;
                    eid.webUname = editData.webUname;

                    RecordMonth rm = Points.GetCalcMonth(new CalcMonthParams(editData.pref));
                    eid.dat_s = "'" + new DateTime(rm.year_, rm.month_, 1).ToShortDateString() + "'";
                    eid.dat_po = "'" + new DateTime(rm.year_, rm.month_, 1).AddMonths(1).AddDays(-1).ToShortDateString() + "'";
                    eid.intvType = editData.intvType;
                    eid.table = editData.table;
                    eid.primary = editData.primary;
                    eid.kod2 = 0;

                    eid.keys = new Dictionary<string, string>();
                    eid.vals = new Dictionary<string, string>();

                    eid.dopFind = new List<string>();
                    eid.dopFind.Add(" and p.nzp_kvar in (select nzp_kvar from " + t_kvar + ") ");
                    eid.comment_action = finder.comment_action;

                    db.MustCalc(conn_db, transaction, eid, out ret);
                }
                #endregion
            }

            db.Close();

            ExecSQL(conn_db, transaction, "drop table " + t_kvar, false);
            conn_web.Close();
        }

        /// <summary> Разблокировать недопоставки
        /// </summary>
        ///
        public Returns UnlockNedop(Nedop finder)
        {
            #region Проверка входных параметров
            if (finder.nzp_user < 1) return new Returns(false, "Не определен пользователь");
            if (finder.nzp_kvar <= 0) return new Returns(false, "Не определен л/с");
            if (finder.pref == "") return new Returns(false, "Не определен префикс БД");
            #endregion

            #region соединение с бд
            string connectionString = Points.GetConnByPref(finder.pref);
            IDbConnection conn_db = GetConnection(connectionString);
            Returns ret = OpenDb(conn_db, true);
            if (!ret.result) return ret;
            #endregion

            #region определение локального пользователя
            int nzpUser = finder.nzp_user;
            
            /*DbWorkUser db = new DbWorkUser();
            int nzpUser = db.GetLocalUser(conn_db, finder, out ret);
            db.Close();
            if (!ret.result)
            {
                conn_db.Close();
                return ret;
            }*/
            #endregion

            StringBuilder sql = new StringBuilder();
#if PG
            sql.Append(" Select n.dat_block, n.user_block, (now() - INTERVAL ' " + Constants.users_min.ToString() + "  minutes') as cur_dat ");
            sql.Append(" From " + finder.pref + "_data.nedop_kvar n");
            sql.Append(" Where n.nzp_kvar = " + finder.nzp_kvar.ToString());
#else
            sql.Append(" Select n.dat_block, n.user_block, (current year to second - " + Constants.users_min.ToString() + " units minute) as cur_dat ");
            sql.Append(" From " + finder.pref + "_data:nedop_kvar n");
            sql.Append(" Where n.nzp_kvar = " + finder.nzp_kvar.ToString());
#endif
            IDataReader reader;
            ret = ExecRead(conn_db, out reader, sql.ToString(), true);
            if (!ret.result)
            {
                conn_db.Close();
                return ret;
            }

            try
            {
                if (reader.Read())
                {
                    DateTime dt_block = DateTime.MinValue;
                    DateTime dt_cur = DateTime.MinValue;
                    int user_block = 0;

                    if (reader["user_block"] != DBNull.Value) user_block = (int)reader["user_block"]; //пользователь, который заблокировал
                    if (reader["dat_block"] != DBNull.Value) dt_block = Convert.ToDateTime(reader["dat_block"]);//дата блокировки
                    if (reader["cur_dat"] != DBNull.Value) dt_cur = Convert.ToDateTime(reader["cur_dat"]);//текущее время/дата - 20 мин

                    string bl = "";
                    if (user_block > 0 && dt_block != DateTime.MinValue) //есть блокировка
                        if (user_block != nzpUser && dt_cur < dt_block) //если заблокирована запись другим пользователем и 20 мин не прошло
                            bl = "Недопоставки заблокированы";

                    if (bl == "") // действующей блокировки нет, или она сделана самим пользователем
                    {
#if PG
                        ret = ExecSQL(conn_db, "update " + finder.pref + "_data.nedop_kvar set dat_block = null, user_block = null where nzp_kvar = " + finder.nzp_kvar, true);
#else
                        ret = ExecSQL(conn_db, "update " + finder.pref + "_data:nedop_kvar set dat_block = null, user_block = null where nzp_kvar = " + finder.nzp_kvar, true);
#endif
                    }
                }
            }
            catch (Exception ex)
            {
                ret = new Returns(false, ex.Message);
                MonitorLog.WriteLog("Ошибка разблокировки недопоставок " + (Constants.Viewerror ? "\n " + ex.Message : ""), MonitorLog.typelog.Error, 20, 201, true);
            }

            if (reader != null) reader.Close();
            conn_db.Close();
            return ret;
        }

        public Returns FindLSDomFromDomNedop(Nedop finder)
        {
            #region проверка входных параметров
            if (finder.nzp_user < 1) return new Returns(false, "Не определен пользователь");
            if (finder.pref.Trim() == "") return new Returns(false, "Не задан префикс базы данных");
            if (finder.nzp_dom < 1 ||
                finder.nzp_serv < 1 ||
                finder.dat_s == "" ||
                finder.dat_po == "" ||
                finder.nzp_kind < 1)
            {
                return new Returns(false, "Неверные входные параметры поиска");
            }
            DateTime ds = DateTime.MinValue;
            DateTime dpo = DateTime.MaxValue;
            if (!DateTime.TryParse(finder.dat_s, out ds)) return new Returns(false, "Неверная дата начала периода");
            if (!DateTime.TryParse(finder.dat_po, out dpo)) return new Returns(false, "Неверная дата окончания периода");
            #endregion

            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            Returns ret = OpenDb(conn_web, true);
            if (!ret.result) return ret;

            string tXX_spls = "t" + Convert.ToString(finder.nzp_user) + "_splsdom";

            //создать кэш-таблицу
            using (var db = new DbAdresClient())
            {
                ret = db.CreateTableWebLs(conn_web, tXX_spls, true);
            }
            if (!ret.result)
            {
                conn_web.Close();
                return ret;
            }

            string conn_kernel = Points.GetConnByPref(finder.pref);
            IDbConnection conn_db = GetConnection(conn_kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result)
            {
                conn_web.Close();
                return ret;
            }
            string field_num_ls_litera = "", value_num_ls_litera = "";
            if (Points.IsSmr)
            {
#if PG
                string tprm1 = finder.pref + "_data" + DBManager.getServer(conn_db) + ".prm_1";
#else
                string tprm1 = finder.pref + "_data@" + DBManager.getServer(conn_db) + ":prm_1";
#endif
                field_num_ls_litera = " , num_ls_litera ";
                value_num_ls_litera = " , (select case when EXTRACT( year FROM  dat_s) -2000 = 0 then k.pkod10 || '' else k.pkod10 || ' ' || EXTRACT( year FROM  dat_s) -2000 end from " +
                    tprm1 + " where nzp_prm=2004 and nzp = k.nzp_kvar) ";
            }

            //заполнить webdata:tXX_spls
#if PG
            string tXX_spls_full = conn_web.Database + ".public." + tXX_spls;
#else
            string tXX_spls_full = conn_web.Database + "@" + DBManager.getServer(conn_web) + ":" + tXX_spls;
#endif
            StringBuilder sql = new StringBuilder();

#if PG
            sql.Append(" Insert into " + tXX_spls_full + " (nzp_kvar,num_ls" + field_num_ls_litera + ",pkod10,pkod,typek,pref,nzp_dom,nzp_area,nzp_geu,fio, ikvar,idom, ndom,nkvar,nzp_ul,ulica, adr,sostls,stypek, mark) ");
            sql.Append(" Select distinct k.nzp_kvar,k.num_ls" + value_num_ls_litera + ",k.pkod10,k.pkod,k.typek,'" + finder.pref + "',k.nzp_dom,k.nzp_area,k.nzp_geu,k.fio, ikvar,idom, ");
            sql.Append("   trim(coalesce(d.ndom,''))||' '||trim(coalesce(d.nkor,'')) as ndom, " +
                       "   trim(coalesce(k.nkvar,''))||' '||trim(coalesce(k.nkvar_n,'')) as nkvar, " +
                       "   d.nzp_ul, trim(u.ulica)||' / '||trim(coalesce(r.rajon,'')) as ulica, ");
            sql.Append("   trim(coalesce(ulicareg,'улица'))||' '||trim(coalesce(u.ulica,''))||' / '||trim(coalesce(r.rajon,''))||'   дом '||" +
                       "   trim(coalesce(ndom,''))||'  корп. '|| trim(coalesce(nkor,''))||'  кв. '||trim(coalesce(nkvar,''))||'  ком. '||trim(coalesce(nkvar_n,'')) as adr, ");
            sql.Append("   ry.name_y as sostls, t.name_y stypek, 1 ");
            sql.Append(" From " + finder.pref + "_data.nedop_kvar n ");
            sql.Append(" LEFT OUTER JOIN	 " + Points.Pref + "_data.kvar k on k.nzp_kvar = n.nzp_kvar ");
            sql.Append(" LEFT OUTER JOIN	 " + finder.pref + "_kernel.res_y t on K.typek = t.nzp_y ");
            sql.Append(" LEFT OUTER JOIN	 " + Points.Pref + "_data.dom d on K.nzp_dom = d.nzp_dom");
            sql.Append(" LEFT OUTER JOIN	 " + Points.Pref + "_data.s_ulica u on d.nzp_ul = u.nzp_ul ");
            sql.Append(" LEFT OUTER JOIN	  " + finder.pref + "_data.s_rajon r on r.nzp_raj = u.nzp_raj ");

            if (finder.stateID > 0)
               sql.Append(", " + finder.pref + "_data.prm_3 p, " + finder.pref + "_kernel.res_y ry ");
            else
            {
                sql.Append(" LEFT OUTER JOIN " + finder.pref + "_data.prm_3 p on K.nzp_kvar = P.nzp ");
                sql.Append(" LEFT OUTER JOIN " + finder.pref + "_kernel.res_y ry on cast(trim(p.val_prm) as integer)= ry.nzp_y ");
            }

            sql.Append(" Where n.nzp_kvar = k.nzp_kvar ");
            if (finder.stateID > 0)
            {
                 sql.Append(" and cast(trim(p.val_prm) as integer)= ry.nzp_y ");
            }
            sql.Append(" and k.nzp_dom = " + finder.nzp_dom +
                            " and k.nzp_dom = d.nzp_dom and d.nzp_ul = u.nzp_ul and u.nzp_raj = r.nzp_raj  ");
            sql.Append(" and k.nzp_kvar = p.nzp and p.nzp_prm = 51 " +
                       " and ry.nzp_res = 18 and p.dat_s <= now() and p.dat_po >= now() and p.is_actual <> 100 ");
            sql.Append(" and k.typek = t.nzp_y and t.nzp_res = 9999 ");
            sql.Append(" and n.nzp_serv = " + finder.nzp_serv);
            if (finder.nzp_supp > 0) sql.Append(" and n.nzp_supp = " + finder.nzp_supp);
            else sql.Append(" and (n.nzp_supp is null or n.nzp_supp = 0) ");
            sql.Append(" and n.nzp_kind = " + finder.nzp_kind +
                " and n.dat_s = " + Utils.EStrNull(ds.ToString("dd.MM.yyyy HH:00")) +
                " and n.dat_po = " + Utils.EStrNull(dpo.ToString("dd.MM.yyyy HH:00")));
            if (finder.tn.Trim() != "") sql.Append(" and n.tn = " + Utils.EStrNull(finder.tn.Trim()));
            else sql.Append(" and (n.tn is null or n.tn = '') ");

#else
            sql.Append(" Insert into " + tXX_spls_full + " (nzp_kvar,num_ls" + field_num_ls_litera + ",pkod10,pkod,typek,pref,nzp_dom,nzp_area,nzp_geu,fio, ikvar,idom, ndom,nkvar,nzp_ul,ulica, adr,sostls,stypek, mark) ");
            sql.Append(" Select unique k.nzp_kvar,k.num_ls" + value_num_ls_litera + ",k.pkod10,k.pkod,k.typek,'" + finder.pref + "',k.nzp_dom,k.nzp_area,k.nzp_geu,k.fio, ikvar,idom, ");
            sql.Append("   trim(nvl(d.ndom,''))||' '||trim(nvl(d.nkor,'')) as ndom, " +
                       "   trim(nvl(k.nkvar,''))||' '||trim(nvl(k.nkvar_n,'')) as nkvar, " +
                       "   d.nzp_ul, trim(u.ulica)||' / '||trim(nvl(r.rajon,'')) as ulica, ");
            sql.Append("   trim(nvl(ulicareg,'улица'))||' '||trim(nvl(u.ulica,''))||' / '||trim(nvl(r.rajon,''))||'   дом '||" +
                       "   trim(nvl(ndom,''))||'  корп. '|| trim(nvl(nkor,''))||'  кв. '||trim(nvl(nkvar,''))||'  ком. '||trim(nvl(nkvar_n,'')) as adr, ");
            sql.Append("   ry.name_y as sostls, t.name_y stypek, 1 ");
            sql.Append(" From " + finder.pref + "_data:nedop_kvar n");
            sql.Append(", " + Points.Pref + "_data:kvar k");
            sql.Append(", " + finder.pref + "_kernel:res_y t");
            sql.Append(", " + Points.Pref + "_data:dom d");
            sql.Append(", " + Points.Pref + "_data:s_ulica u");
            sql.Append(", outer  " + finder.pref + "_data:s_rajon r");
            if (finder.stateID > 0)
                sql.Append(", " + finder.pref + "_data:prm_3 p, " + finder.pref + "_kernel:res_y ry ");
            else
                sql.Append(", outer (" + finder.pref + "_data:prm_3 p, " + finder.pref + "_kernel:res_y ry) ");
            sql.Append(" Where n.nzp_kvar = k.nzp_kvar " +
                            " and k.nzp_dom = " + finder.nzp_dom +
                            " and k.nzp_dom = d.nzp_dom and d.nzp_ul = u.nzp_ul and u.nzp_raj = r.nzp_raj  ");
            sql.Append(" and k.nzp_kvar = p.nzp and p.nzp_prm = 51 and trim(p.val_prm) = ry.nzp_y " +
                       " and ry.nzp_res = 18 and p.dat_s <= today and p.dat_po >= today and p.is_actual <> 100");
            sql.Append(" and k.typek = t.nzp_y and t.nzp_res = 9999 ");
            sql.Append(" and n.nzp_serv = " + finder.nzp_serv);
            if (finder.nzp_supp > 0) sql.Append(" and n.nzp_supp = " + finder.nzp_supp);
            else sql.Append(" and (n.nzp_supp is null or n.nzp_supp = 0)");
            sql.Append(" and n.nzp_kind = " + finder.nzp_kind +
                " and n.dat_s = " + Utils.EStrNull(ds.ToString("yyyy-MM-dd HH:00")) +
                " and n.dat_po = " + Utils.EStrNull(dpo.ToString("yyyy-MM-dd HH:00")));
            if (finder.tn.Trim() != "") sql.Append(" and n.tn = " + Utils.EStrNull(finder.tn.Trim()));
            else sql.Append(" and (n.tn is null or n.tn = '')");
#endif

            ret = ExecSQL(conn_db, sql.ToString(), true);
            if (!ret.result)
            {
                conn_db.Close();
                conn_web.Close();
                return ret;
            }

            sql = new StringBuilder();

#if PG
            sql.Append("Update " + tXX_spls_full +
                            " Set has_pu = 1" +
                            " Where pref = '" + finder.pref + "'" +
                                " and nzp_kvar in (select nzp from " + finder.pref + "_data.counters_spis" +
                                                 " where nzp_type = " + (int)CounterKinds.Kvar + " and is_actual <> 100) ");
#else
            sql.Append("Update " + tXX_spls_full +
                 " Set has_pu = 1" +
                 " Where pref = '" + finder.pref + "'" +
                     " and nzp_kvar in (select nzp from " + finder.pref + "_data:counters_spis" +
                                      " where nzp_type = " + (int)CounterKinds.Kvar + " and is_actual <> 100)");
#endif
            ret = ExecSQL(conn_db, sql.ToString(), true);
            if (!ret.result)
            {
                conn_db.Close();
                conn_web.Close();
                return ret;
            }

            sql = new StringBuilder();
#if PG
            sql.Append(" Update " + tXX_spls_full +
                " Set has_pu = 1" +
                " Where pref = '" + finder.pref + "'" +
                    " and exists (select 1 from " + finder.pref + "_data.counters_spis cs, " + finder.pref + "_data.counters_link cl" +
                                     " where cs.nzp_type in (" + (int)CounterKinds.Group + "," + (int)CounterKinds.Communal + ")" +
                                         " and cs.nzp_counter = cl.nzp_counter" +
                                         " and cs.is_actual <> 100" +
                                         " and " + tXX_spls_full + ".nzp_kvar = cl.nzp_kvar)");
#else
            sql.Append("Update " + tXX_spls_full +
                " Set has_pu = 1" +
                " Where pref = '" + finder.pref + "'" +
                    " and exists (select 1 from " + finder.pref + "_data:counters_spis cs, " + finder.pref + "_data:counters_link cl" +
                                     " where cs.nzp_type in (" + (int)CounterKinds.Group + "," + (int)CounterKinds.Communal + ")" +
                                         " and cs.nzp_counter = cl.nzp_counter" +
                                         " and cs.is_actual <> 100" +
                                         " and " + tXX_spls_full + ".nzp_kvar = cl.nzp_kvar)");
#endif
            ret = ExecSQL(conn_db, sql.ToString(), true);
            if (!ret.result)
            {
                conn_db.Close();
                conn_web.Close();
                return ret;
            }

            conn_db.Close(); //закрыть соединение с основной базой

            //создаем индексы на tXX_spls
            using (var db = new DbAdresClient())
            {
                ret = db.CreateTableWebLs(conn_web, tXX_spls, false);
            }

            conn_web.Close();
            return ret;
        }

        /// <summary>
        /// возвращает данные для отчета по списку плановых работ
        /// </summary>
        /// <param name="nzp_user"></param>
        /// <param name="en"></param>
        /// <param name="ret"></param>
        /// <returns></returns>
        public List<NedopInfo> PlannedWorksList(int nzp_user, enSrvOper en, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<NedopInfo> res = null;
            IDbConnection con_web = null;
            IDbConnection con_db = null;
            IDataReader reader = null;
            IDataReader reader1 = null;


            string sql = "";

            if (nzp_user < 1)
            {
                ret.result = false;
                ret.text = "Не определен пользователь";
                return null;
            }
            try
            {
                con_web = GetConnection(Constants.cons_Webdata);
                ret = OpenDb(con_web, true);
                if (!ret.result)
                {
                    MonitorLog.WriteLog("Ошибка при открытии соединения с БД в PlannedWorksList", MonitorLog.typelog.Error, true);
                    ret.result = false;
                    return null;
                }

                con_db = GetConnection(Constants.cons_Kernel);
                ret = OpenDb(con_db, true);
                if (!ret.result)
                {
                    MonitorLog.WriteLog("Ошибка при открытии соединения с БД в PlannedWorksList", MonitorLog.typelog.Error, true);
                    ret.result = false;
                    return null;
                }
#if PG
                string tXX_supg_acts = "t" + Convert.ToString(nzp_user) + "_supg_acts";
                string tXX_supg_acts_full = con_web.Database + ".public." + tXX_supg_acts;
#else
                string tXX_supg_acts = "t" + Convert.ToString(nzp_user) + "_supg_acts";
                string tXX_supg_acts_full = con_web.Database + "@" + DBManager.getServer(con_web) + ":" + tXX_supg_acts;
#endif
#if PG
                if (en == enSrvOper.GetPlannedWorksSupp)
                {
                    sql = "select a.plan_number number, " +
                            "a.comment, a.dat_s, a.dat_po," +
                            "s.name_supp, a.nzp_act " +
                            "from " + Points.Pref + "_supg.act a, " + con_web.Database + ".public" + ".supplier s , " + tXX_supg_acts_full + " t " +
                            "where " +
                            "t.nzp_act=a.nzp_act " +
                            "and a.nzp_supp=s.nzp_supp " +
                            "order by 5,1";
                }
                if (en == enSrvOper.GetPlannedWorksNone)
                {
                    sql = "select distinct year(a._date) ydate,"+pgDefaultSchema+".sortnum(a.plan_number) a1, a.number, a.plan_date, a.nzp_act, " +
                            "a.comment, a.dat_s, a.dat_po, " +
                            "s.name_supp " +
                            "from " + Points.Pref + "_supg.act a, " + con_web.Database + ".public" + ".supplier s , " + tXX_supg_acts_full + " t " +
                            "where " +
                            "t.nzp_act=a.nzp_act " +
                            "and a.nzp_supp=s.nzp_supp " +
                            "order by 1,2";
                }
                if (en == enSrvOper.GetPlannedWorksActs)
                {
                    sql = "select a.number, a._date, a.tn, a.plan_number, a.plan_date, " +
                            "a.dat_s, a.dat_po, a.nzp_act, " +
                            "trim(s.service)||case when n.nzp_kind is null then ' ' else ' / '||trim(n.name) end name_nedop " +
                            "from " + Points.Pref + "_supg.act a, " +
                            "outer(" + Points.Pref + "_data.upg_s_kind_nedop n), " + Points.Pref + "_kernel.services s ," +
                            tXX_supg_acts_full + " t " +
                            "where " +
                            "t.nzp_act=a.nzp_act " +
                            "and a.nzp_kind=n.nzp_kind " +
                            "and n.kod_kind=1 " +
                            "and a.nzp_serv=s.nzp_serv and a.is_actual = 2 " +
                            "order by 1,2";
                }
#else
                if (en == enSrvOper.GetPlannedWorksSupp)
                {
                    sql = "select a.plan_number number, " +
                            "a.comment, a.dat_s, a.dat_po," +
                            "s.name_supp, a.nzp_act " +
                            "from " + Points.Pref + "_supg:act a, " + con_web.Database + "@" + DBManager.getServer(con_web) + ":supplier s , " + tXX_supg_acts_full + " t " +
                            "where " +
                            "t.nzp_act=a.nzp_act " +
                            "and a.nzp_supp=s.nzp_supp " +
                            "order by 5,1";
                }
                if (en == enSrvOper.GetPlannedWorksNone)
                {
                    sql = "select unique year(a._date) ydate," + Points.Pref + "_data: sortnum(a.plan_number) a1, a.number, a.plan_date, a.nzp_act, " +
                            "a.comment, a.dat_s, a.dat_po, " +
                            "s.name_supp " +
                            "from " + Points.Pref + "_supg:act a, " + con_web.Database + "@" + DBManager.getServer(con_web) + ":supplier s , " + tXX_supg_acts_full + " t " +
                            "where " +
                            "t.nzp_act=a.nzp_act " +
                            "and a.nzp_supp=s.nzp_supp " +
                            "order by 1,2";
                }
                if (en == enSrvOper.GetPlannedWorksActs)
                {
                    sql = "select a.number, a._date, a.tn, a.plan_number, a.plan_date, " +
                            "a.dat_s, a.dat_po, a.nzp_act, " +
                            "trim(s.service)||case when n.nzp_kind is null then ' ' else ' / '||trim(n.name) end name_nedop " +
                            "from " + Points.Pref + "_supg:act a, " +
                            "outer(" + Points.Pref + "_data:upg_s_kind_nedop n), " + Points.Pref + "_kernel:services s ," +
                            tXX_supg_acts_full + " t " +
                            "where " +
                            "t.nzp_act=a.nzp_act " +
                            "and a.nzp_kind=n.nzp_kind " +
                            "and n.kod_kind=1 " +
                            "and a.nzp_serv=s.nzp_serv and a.is_actual = 2 " +
                            "order by 1,2";
                }
#endif

                if (!ExecRead(con_db, out reader, sql.ToString(), true).result)
                {
                    MonitorLog.WriteLog("Ошибка выборки PlannedWorksList " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                    ret.result = false;
                    return null;
                }

                #region Запись выгрузки

                try
                {
                    if (reader != null)
                    {
                        res = new List<NedopInfo>();

                        #region #1,2
                        if (en == enSrvOper.GetPlannedWorksSupp || en == enSrvOper.GetPlannedWorksNone)
                        {
                            while (reader.Read())
                            {
                                NedopInfo nedop_info = new NedopInfo();
                                if (reader["number"] != DBNull.Value) nedop_info.plan_number = Convert.ToString(reader["number"]).Trim();

                                if (en == enSrvOper.GetPlannedWorksNone)
                                {
                                    if (reader["plan_date"] != DBNull.Value)
                                    {
                                        if (!String.IsNullOrEmpty(reader["plan_date"].ToString()))
                                            nedop_info.plan_date = Convert.ToString(reader["plan_date"]).Trim();
                                    }
                                }

                                if (reader["nzp_act"] != DBNull.Value) nedop_info.nzp_act = Convert.ToInt32(reader["nzp_act"]);
                                if (reader["comment"] != DBNull.Value)
                                {
                                    if (!String.IsNullOrEmpty(reader["comment"].ToString()))
                                    {
                                        nedop_info.comment = Convert.ToString(reader["comment"]).ToString().Trim();
                                    }
                                }
                                if (reader["dat_s"] != DBNull.Value) nedop_info.disconnect_date = Convert.ToString(reader["dat_s"]);
                                if (reader["dat_po"] != DBNull.Value) nedop_info.connect_date = Convert.ToString(reader["dat_po"]);
                                if (reader["name_supp"] != DBNull.Value) nedop_info.name_supp = Convert.ToString(reader["name_supp"]);
                                nedop_info.officer = "";
                                nedop_info.ctp = "";
                                res.Add(nedop_info);
                            }
                        }
                        #endregion

                        #region #3
                        if (en == enSrvOper.GetPlannedWorksActs)
                        {
                            while (reader.Read())
                            {
                                NedopInfo nedop_info = new NedopInfo();

                                if (reader["number"] != DBNull.Value) nedop_info.number = Convert.ToString(reader["number"]).Trim();
                                if (reader["plan_number"] != DBNull.Value) nedop_info.plan_number = Convert.ToString(reader["plan_number"]).Trim();
                                if (reader["_date"] != DBNull.Value)
                                {
                                    if (!String.IsNullOrEmpty(reader["_date"].ToString()))
                                        nedop_info.act_date = Convert.ToString(reader["_date"]).Trim();
                                }
                                if (reader["plan_date"] != DBNull.Value)
                                {
                                    if (!String.IsNullOrEmpty(reader["plan_date"].ToString()))
                                        nedop_info.plan_date = Convert.ToString(reader["plan_date"]).Trim();
                                }
                                if (reader["tn"] != DBNull.Value) nedop_info.tn = Convert.ToString(reader["tn"]).Trim();

                                if (reader["nzp_act"] != DBNull.Value) nedop_info.nzp_act = Convert.ToInt32(reader["nzp_act"]);
                                if (reader["dat_s"] != DBNull.Value) nedop_info.disconnect_date = Convert.ToString(reader["dat_s"]);
                                if (reader["dat_po"] != DBNull.Value) nedop_info.connect_date = Convert.ToString(reader["dat_po"]);
                                if (reader["name_nedop"] != DBNull.Value) nedop_info.name_nedop = Convert.ToString(reader["name_nedop"]);
                                res.Add(nedop_info);
                            }
                        }
                        #endregion
                    }
                    reader.Close();

                    foreach (NedopInfo item in res)
                    {
                        #region добавление данных по номеру акта

#if PG
                        sql = "select  distinct g.geu, " +
                                                        "'ул.'||trim(u.ulica)|| (case when trim(r.rajon)='-' then ' ' else ' / '||trim(r.rajon) end)|| " +
                                                        "(case when d.ndom is null then '' else ' д.'||trim(d.ndom)|| " +
                                                        "(case when d.nkor='-' then '' else ' корп.'||trim(d.nkor) end) end)|| " +
                                                        "(case when k.nkvar is null then '' else ' кв.'||trim(k.nkvar)|| " +
                                                        "(case when trim(k.nkvar_n)='-' then '' else ' комн.'||trim(k.nkvar_n) end) end ) address " +
                                                        "from " +
                                                             Points.Pref + "_supg.act_obj ao, " +
                                                             Points.Pref + "_data.s_ulica u, " +
                                                             Points.Pref + "_data.s_rajon r, " +
                                                             "outer(" + Points.Pref + "_data.dom d, " + Points.Pref + "_data.s_geu gleft outer join(" + Points.Pref + "_data.kvar k)) " +
                                                        "where ao.nzp_act= " + item.nzp_act + " " +
                                                          "and ao.nzp_dom=d.nzp_dom and d.nzp_geu=g.nzp_geu " +
                                                          "and u.nzp_ul=ao.nzp_ul and u.nzp_ul=d.nzp_ul and d.nzp_dom=k.nzp_dom " +
                                                          "and u.nzp_raj=r.nzp_raj and ao.nzp_kvar=k.nzp_kvar ";
#else
                        sql = "select  unique g.geu, " +
                                "'ул.'||trim(u.ulica)|| (case when trim(r.rajon)='-' then ' ' else ' / '||trim(r.rajon) end)|| " +
                                "(case when d.ndom is null then '' else ' д.'||trim(d.ndom)|| " +
                                "(case when d.nkor='-' then '' else ' корп.'||trim(d.nkor) end) end)|| " +
                                "(case when k.nkvar is null then '' else ' кв.'||trim(k.nkvar)|| " +
                                "(case when trim(k.nkvar_n)='-' then '' else ' комн.'||trim(k.nkvar_n) end) end ) address " +
                                "from " +
                                     Points.Pref + "_supg:act_obj ao, " +
                                     Points.Pref + "_data:s_ulica u, " +
                                     Points.Pref + "_data:s_rajon r, " +
                                     "outer(" + Points.Pref + "_data:dom d, " + Points.Pref + "_data:s_geu g, outer(" + Points.Pref + "_data:kvar k)) " +
                                "where ao.nzp_act= " + item.nzp_act + " " +
                                  "and ao.nzp_dom=d.nzp_dom and d.nzp_geu=g.nzp_geu " +
                                  "and u.nzp_ul=ao.nzp_ul and u.nzp_ul=d.nzp_ul and d.nzp_dom=k.nzp_dom " +
                                  "and u.nzp_raj=r.nzp_raj and ao.nzp_kvar=k.nzp_kvar ";
#endif

                        if (en == enSrvOper.GetPlannedWorksSupp)
                        {
                            sql += "order by 1,2";
                        }
                        if (en == enSrvOper.GetPlannedWorksNone)
                        {
                            sql += "order by 1";
                        }
                        if (en == enSrvOper.GetPlannedWorksActs)
                        {
                            sql += "order by 1";
                        }

                        if (!ExecRead(con_db, out reader1, sql.ToString(), true).result)
                        {
                            MonitorLog.WriteLog("Ошибка выборки PlannedWorksList " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                            ret.result = false;
                            return null;
                        }
                        if (reader1 != null)
                        {
                            item.geu_list = new List<string>();
                            item.address_list = new List<string>();
                            while (reader1.Read())
                            {
                                string sgeu = "";
                                if (reader1["geu"] != DBNull.Value) sgeu = Convert.ToString(reader1["geu"]).Trim();
                                item.geu_list.Add(sgeu);
                                if (reader1["address"] != DBNull.Value) item.address_list.Add(Convert.ToString(reader1["address"]).Trim());
                            }
                        }
                        reader1.Close();
                        #endregion
                    }
                }
                catch (Exception ex)
                {
                    MonitorLog.WriteLog("Ошибка в процедуре PlannedWorksList " + ex.Message, MonitorLog.typelog.Error, true);
                    con_db.Close();
                    reader.Close();
                    return null;
                }

                #endregion

                return res;
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры  GetOrderList : " + ex.Message, MonitorLog.typelog.Error, true);
                return null;
            }
            finally
            {
                #region Закрытие соединений

                if (con_db != null)
                {
                    con_db.Close();
                }

                if (con_web != null)
                {
                    con_web.Close();
                }

                if (reader != null)
                {
                    reader.Close();
                }

                #endregion
            }
        }

        /// <summary>
        /// возвращает данные для отчета по списку недопоставок
        /// </summary>
        /// <param name="nzp_user"></param>
        /// <param name="en"></param>
        /// <param name="ret"></param>
        /// <returns></returns>
        public List<NedopInfo> GetRepNedopList(int nzp_user, int nzp_jrn, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<NedopInfo> res = null;
            IDbConnection con_web = null;
            IDbConnection con_db = null;
            IDataReader reader = null;
            IDataReader reader1 = null;


            string sql = "";
            string dbsupg = "";

            if (nzp_user < 1)
            {
                ret.result = false;
                ret.text = "Не определен пользователь";
                return null;
            }
            try
            {
                con_web = GetConnection(Constants.cons_Webdata);
                ret = OpenDb(con_web, true);
                if (!ret.result)
                {
                    MonitorLog.WriteLog("Ошибка при открытии соединения с БД в GetRepNedopList", MonitorLog.typelog.Error, true);
                    ret.result = false;
                    return null;
                }

                con_db = GetConnection(Constants.cons_Kernel);
                ret = OpenDb(con_db, true);
                if (!ret.result)
                {
                    MonitorLog.WriteLog("Ошибка при открытии соединения с БД в GetRepNedopList", MonitorLog.typelog.Error, true);
                    ret.result = false;
                    return null;
                }

                #region Определить БД СУПГ (распределение недопоставок будет выполняться в Комплат)
#if PG
                sql = "select * from " + Points.Pref + "_kernel .s_baselist where idtype=6";
#else
                sql = "select * from " + Points.Pref + "_kernel :s_baselist where idtype=6";
#endif
                if (!ExecRead(con_db, out reader, sql.ToString(), true).result)
                {
                    string err_text = "Ошибка при определении ссылки на БД СУПГ ";
                    MonitorLog.WriteLog(err_text + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                    ret.result = false;
                    return null;
                }

                if (reader != null)
                {
                    while (reader.Read())
                    {
                        if (reader["dbname"] != DBNull.Value)
                        {
                            dbsupg = reader["dbname"].ToString();
                        }
                    }
                }

                if (dbsupg == "")
                {
                    string err_text = "В системе не определена ссылка на БД СУПГ!";
                    MonitorLog.WriteLog(err_text, MonitorLog.typelog.Error, 20, 201, true);
                    ret.result = false;
                    return null;
                }

                // Проверить доступ на БД СУПГ
#if PG
                if (!TempTableInWebCashe(con_db, dbsupg + ".act"))
                {
                    string err_text = "Банк данных " + dbsupg + " недоступен";
                    MonitorLog.WriteLog(err_text, MonitorLog.typelog.Warn, true);
                    ret.result = false;
                    return null;
                }
#else
                if (!TempTableInWebCashe(con_db, dbsupg + ":act"))
                {
                    string err_text = "Банк данных " + dbsupg + " недоступен";
                    MonitorLog.WriteLog(err_text, MonitorLog.typelog.Warn, true);
                    ret.result = false;
                    return null;
                }
#endif

                #endregion

#if PG
                sql = "select distinct " +
                                      "2 knd, " +
                                      "s.service name_serv, n.name name_nedop, " +
                                      "a.number, a._date, " +
                                      "(case when a.dat_s > coalesce(g.d_begin,public.mdy(1,1,1900)) then a.dat_s else g.d_begin end) dat_s, " +
                                      "(case when a.dat_po < g.d_end then a.dat_po else g.d_end end) dat_po, " +
                                      "a.nzp_act " +
                                      "from " + dbsupg + ".act a, " +
                                      Points.Pref + "_data.upg_s_kind_nedop n, " + Points.Pref + "_kernel.services s ," +
                                      dbsupg + ".jrn_upg_nedop g,  " +
                                      dbsupg + ".nedop_kvar nk " +
                                      "where " +
                                      "g.no=" + nzp_jrn.ToString() + " and g.is_actual in (0,2) " +
                                      "and g.no=nk.nzp_jrn and nk.is_actual=14 " +
                                      "and a.nzp_act=nk.act_no " +
                                      "and a.nzp_kind=n.nzp_kind " +
                                      "and n.kod_kind=1 " +
                                      "and a.nzp_serv=s.nzp_serv " +
                                      "union select " +
                                      "1, " +
                                      "s.service, n.name, " +
                                      "zk.nzp_zk||\'\', date(zk.order_date), " +
                                      "nk.dat_s, " +
                                      "nk.dat_po, " +
                                      "nk.nzp_kvar " +
                                      "from " + dbsupg + ".zakaz zk, " + dbsupg + ".s_dest d, " +
                                      Points.Pref + "_data.upg_s_kind_nedop n, " + Points.Pref + "_kernel.services s ," +
                                      dbsupg + ".jrn_upg_nedop g,  " +
                                      dbsupg + ".nedop_kvar nk " +
                                      "where " +
                                      "g.no=" + nzp_jrn.ToString() + " and g.is_actual in (0,1) " +
                                      "and g.no=nk.nzp_jrn and nk.is_actual=12 " +
                                      "and zk.nzp_zk=nk.act_no " +
                                      "and zk.nzp_dest=d.nzp_dest and d.nzp_serv=s.nzp_serv " +
                                      "and d.num_nedop=n.nzp_kind " +
                                      "and n.kod_kind=1 " +
                                      "order by 1,2,3,4";
#else
                sql = "select unique " +
                                      "2 knd, " +
                                      "s.service name_serv, n.name name_nedop, " +
                                      "a.number, a._date, " +
                                      "(case when a.dat_s > nvl(g.d_begin,mdy(1,1,1900)) then a.dat_s else EXTEND(g.d_begin, year to hour) end) dat_s, " +
                                      "(case when a.dat_po < g.d_end then a.dat_po else EXTEND(g.d_end, year to hour) end) dat_po, " +
                                      "a.nzp_act " +
                                      "from " + dbsupg + ":act a, " +
                                      Points.Pref + "_data:upg_s_kind_nedop n, " + Points.Pref + "_kernel:services s ," +
                                      dbsupg + ":jrn_upg_nedop g,  " +
                                      dbsupg + ":nedop_kvar nk " +
                                      "where " +
                                      "g.no=" + nzp_jrn.ToString() + " and g.is_actual in (0,2) " +
                                      "and g.no=nk.nzp_jrn and nk.is_actual=14 " +
                                      "and a.nzp_act=nk.act_no " +
                                      "and a.nzp_kind=n.nzp_kind " +
                                      "and n.kod_kind=1 " +
                                      "and a.nzp_serv=s.nzp_serv " +
                                      "union select " +
                                      "1, " +
                                      "s.service, n.name, " +
                                      "zk.nzp_zk||\'\', date(zk.order_date), " +
                                      "EXTEND(nk.dat_s, year to hour), " +
                                      "EXTEND(nk.dat_po, year to hour), " +
                                      "nk.nzp_kvar " +
                                      "from " + dbsupg + ":zakaz zk, " + dbsupg + ":s_dest d, " +
                                      Points.Pref + "_data:upg_s_kind_nedop n, " + Points.Pref + "_kernel:services s ," +
                                      dbsupg + ":jrn_upg_nedop g,  " +
                                      dbsupg + ":nedop_kvar nk " +
                                      "where " +
                                      "g.no=" + nzp_jrn.ToString() + " and g.is_actual in (0,1) " +
                                      "and g.no=nk.nzp_jrn and nk.is_actual=12 " +
                                      "and zk.nzp_zk=nk.act_no " +
                                      "and zk.nzp_dest=d.nzp_dest and d.nzp_serv=s.nzp_serv " +
                                      "and d.num_nedop=n.nzp_kind " +
                                      "and n.kod_kind=1 " +
                                      "order by 1,2,3,4";
#endif

                if (!ExecRead(con_db, out reader, sql.ToString(), true).result)
                {
                    MonitorLog.WriteLog("Ошибка выборки GetRepNedopList " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                    ret.result = false;
                    return null;
                }

                #region Запись выгрузки

                try
                {
                    if (reader != null)
                    {
                        res = new List<NedopInfo>();

                        #region #3
                        while (reader.Read())
                        {
                            NedopInfo nedop_info = new NedopInfo();

                            if (reader["knd"] != DBNull.Value) nedop_info.ctp = Convert.ToString(reader["knd"]).Trim();
                            if (reader["name_serv"] != DBNull.Value) nedop_info.name_serv = Convert.ToString(reader["name_serv"]).Trim();
                            if (reader["number"] != DBNull.Value) nedop_info.number = Convert.ToString(reader["number"]).Trim();
                            if (reader["_date"] != DBNull.Value)
                            {
                                if (!String.IsNullOrEmpty(reader["_date"].ToString()))
                                    //nedop_info.act_date = Convert.ToString(reader["_date"]).Trim();
                                    nedop_info.act_date = System.Convert.ToDateTime(reader["_date"]).ToString("dd.MM.yyyy");
                            }

                            //if (reader["tn"] != DBNull.Value) nedop_info.tn = Convert.ToString(reader["tn"]).Trim();

                            if (reader["nzp_act"] != DBNull.Value) nedop_info.nzp_act = Convert.ToInt32(reader["nzp_act"]);
                            //if (reader["dat_s"] != DBNull.Value) nedop_info.disconnect_date = Convert.ToString(reader["dat_s"]);
                            //if (reader["dat_po"] != DBNull.Value) nedop_info.connect_date = Convert.ToString(reader["dat_po"]);
                            if (reader["dat_s"] != DBNull.Value) nedop_info.disconnect_date = System.Convert.ToDateTime(reader["dat_s"]).ToString("dd.MM.yyyy HH:mm");
                            if (reader["dat_po"] != DBNull.Value) nedop_info.connect_date = System.Convert.ToDateTime(reader["dat_po"]).ToString("dd.MM.yyyy HH:mm");
                            if (reader["name_nedop"] != DBNull.Value) nedop_info.name_nedop = Convert.ToString(reader["name_nedop"]);
                            res.Add(nedop_info);
                        }

                        #endregion
                    }
                    reader.Close();

                    foreach (NedopInfo item in res)
                    {
                        #region добавление данных по номеру акта

#if PG
                        string sql_fields = "select  distinct g.geu, " +
                                                        "'ул.'||trim(u.ulica)|| (case when trim(r.rajon)='-' then ' ' else ' / '||trim(r.rajon) end)|| " +
                                                        "(case when d.ndom is null then '' else ' д.'||trim(d.ndom)|| " +
                                                        "(case when d.nkor='-' then '' else ' корп.'||trim(d.nkor) end) end)|| " +
                                                        "(case when k.nkvar is null then '' else ' кв.'||trim(k.nkvar)|| " +
                                                        "(case when trim(k.nkvar_n)='-' then '' else ' комн.'||trim(k.nkvar_n) end) end ) address ";
#else
                        string sql_fields = "select  unique g.geu, " +
                                "'ул.'||trim(u.ulica)|| (case when trim(r.rajon)='-' then ' ' else ' / '||trim(r.rajon) end)|| " +
                                "(case when d.ndom is null then '' else ' д.'||trim(d.ndom)|| " +
                                "(case when d.nkor='-' then '' else ' корп.'||trim(d.nkor) end) end)|| " +
                                "(case when k.nkvar is null then '' else ' кв.'||trim(k.nkvar)|| " +
                                "(case when trim(k.nkvar_n)='-' then '' else ' комн.'||trim(k.nkvar_n) end) end ) address ";
#endif
#if PG
                        if (item.ctp == "1")
                        {
                            sql = sql_fields +
                                   "from " + Points.Pref + "_data.s_ulica u, " +
                                            Points.Pref + "_data.s_rajon r, " +
                                            Points.Pref + "_data.dom d left outer join " + Points.Pref + "_data.s_geu g on d.nzp_geu=g.nzp_geu, " + Points.Pref + "_data.kvar k " +
                                   "where k.nzp_kvar= " + item.nzp_act + " " +
                                       "and k.nzp_dom=d.nzp_dom " +
                                       "and u.nzp_ul=d.nzp_ul " +
                                       "and u.nzp_raj=r.nzp_raj ";
                        }
                        else
                        {
                            sql = String.Format(@"{0} FROM
                               	{1}_supg.act_obj ao,
                                {1}_data.s_rajon r,
	                            {1}_data.s_ulica u                               	
                                LEFT JOIN {1}_data.dom d ON u.nzp_ul = d.nzp_ul and d.nzp_dom in (select nzp_dom from {1}_supg.act_obj where nzp_act = {2} )
                                LEFT JOIN {1}_data.s_geu G ON d.nzp_geu=g.nzp_geu
                                LEFT JOIN {1}_data.kvar K ON d.nzp_dom = K .nzp_dom and k.nzp_kvar in (select nzp_kvar from {1}_supg.act_obj where nzp_act = {2} )                               
                               WHERE
                               	ao.nzp_act = {2}
                               AND u.nzp_ul = ao.nzp_ul                               
                               AND u.nzp_raj = r.nzp_raj", sql_fields, Points.Pref, item.nzp_act);
                        }
#else
                        if (item.ctp == "1")
                        {
                            sql = sql_fields +
                                    "from " + Points.Pref + "_data:s_ulica u, " +
                                             Points.Pref + "_data:s_rajon r, " +
                                             Points.Pref + "_data:dom d, outer(" + Points.Pref + "_data:s_geu g), " + Points.Pref + "_data:kvar k " +
                                    "where k.nzp_kvar= " + item.nzp_act + " " +
                                        "and k.nzp_dom=d.nzp_dom and d.nzp_geu=g.nzp_geu " +
                                        "and u.nzp_ul=d.nzp_ul " +
                                        "and u.nzp_raj=r.nzp_raj ";
                        }
                        else
                        {
                            sql = sql_fields +
                                    "from " +
                                         dbsupg + ":act_obj ao, " +
                                         Points.Pref + "_data:s_ulica u, " +
                                         Points.Pref + "_data:s_rajon r, " +
                                         "outer(" + Points.Pref + "_data:dom d, " + Points.Pref + "_data:s_geu g, outer(" + Points.Pref + "_data:kvar k)) " +
                                    "where ao.nzp_act= " + item.nzp_act + " " +
                                      "and ao.nzp_dom=d.nzp_dom and d.nzp_geu=g.nzp_geu " +
                                      "and u.nzp_ul=ao.nzp_ul and u.nzp_ul=d.nzp_ul and d.nzp_dom=k.nzp_dom " +
                                      "and u.nzp_raj=r.nzp_raj and ao.nzp_kvar=k.nzp_kvar ";
                        }
#endif
                        sql += " order by 1,2 ";

                        if (!ExecRead(con_db, out reader1, sql.ToString(), true).result)
                        {
                            MonitorLog.WriteLog("Ошибка выборки GetRepNedopList " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                            ret.result = false;
                            return null;
                        }
                        if (reader1 != null)
                        {
                            item.geu_list = new List<string>();
                            item.address_list = new List<string>();
                            while (reader1.Read())
                            {
                                string sgeu = "";
                                if (reader1["geu"] != DBNull.Value) sgeu = Convert.ToString(reader1["geu"]).Trim();
                                item.geu_list.Add(sgeu);
                                if (reader1["address"] != DBNull.Value) item.address_list.Add(Convert.ToString(reader1["address"]).Trim());
                            }
                        }
                        reader1.Close();
                        #endregion
                    }
                }
                catch (Exception ex)
                {
                    MonitorLog.WriteLog("Ошибка в процедуре GetRepNedopList " + ex.Message, MonitorLog.typelog.Error, true);
                    con_db.Close();
                    reader.Close();
                    return null;
                }

                #endregion

                return res;
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры  GetRepNedopList : " + ex.Message, MonitorLog.typelog.Error, true);
                return null;
            }
            finally
            {
                #region Закрытие соединений

                if (con_db != null)
                {
                    con_db.Close();
                }

                if (con_web != null)
                {
                    con_web.Close();
                }

                if (reader != null)
                {
                    reader.Close();
                }

                #endregion
            }
        }

        /// <summary>
        /// процедура записи параметров поиска в базу
        /// </summary>
        /// <param name="finder"></param>
        /// <param name="nzp_page"></param>
        /// <returns></returns>
        public Returns SaveFinder(SupgActFinder finder, int nzp_page)
        {
            Returns ret = Utils.InitReturns();
            StringBuilder sql = new StringBuilder();
            IDbConnection conn_web = null;
            IDbConnection conn_db = null;
            IDataReader reader = null;

            try
            {
                if (finder.nzp_user <= 0)
                {
                    ret.result = false;
                    ret.tag = -1;
                    ret.text = "Пользователь не определен";
                    return ret;
                }

                //соединение с БД
                conn_web = GetConnection(Constants.cons_Webdata);
                ret = OpenDb(conn_web, true);

                if (!ret.result)
                {
                    MonitorLog.WriteLog("Ошибка при открытии соединения с БД в SaveFinder", MonitorLog.typelog.Error, true);
                    ret.result = false;
                    return ret;
                }

                string tXX_spfinder = "t" + Convert.ToString(finder.nzp_user) + "_spfinder";

                //проверка наличия таблицы в БД
                if (!TableInWebCashe(conn_web, tXX_spfinder))
                {
                    //создать таблицу webdata
#if PG
                    ret = ExecSQL(conn_web,
                                                 " Create table " + tXX_spfinder +
                                                 " (nzp_finder serial, " +
                                                 "  name char(100), " +
                                                 "  value char(255), " +
                                                 "  nzp_page integer " +
                                                 " ) ", true);
#else
                    ret = ExecSQL(conn_web,
                              " Create table " + tXX_spfinder +
                              " (nzp_finder serial, " +
                              "  name char(100), " +
                              "  value char(255), " +
                              "  nzp_page integer " +
                              " ) ", true);
#endif
                }
                if (!ret.result)
                {
                    conn_web.Close();
                    return ret;
                }

                sql.Append("delete from " + tXX_spfinder + " where nzp_page = " + nzp_page.ToString());
                ret = ExecSQL(conn_web, sql.ToString(), true);
                if (!ret.result)
                {
                    conn_web.Close();
                    return ret;
                }

                #region Парметры поиска

                #region Входящий документ

                //номер документа
                if (finder.plan_number != "")
                {
                    sql.Remove(0, sql.Length);
                    sql.Append(" Insert into " + tXX_spfinder);
                    sql.Append(" values (0,\'Номер документа\',\'" + finder.plan_number.Trim() + "\'," + nzp_page.ToString() + ")");
                    ret = ExecSQL(conn_web, sql.ToString(), true);
                    if (!ret.result)
                    {
                        conn_web.Close();
                        return ret;
                    }
                }

                //дата документа c
                if (finder.plan_date.Trim() != "")
                {
                    sql.Remove(0, sql.Length);
                    sql.Append(" Insert into " + tXX_spfinder);
                    sql.Append(" Values (0,\'Дата документа с\',\'" + finder.plan_date.Trim() + "\'," + nzp_page.ToString() + ")");
                    ret = ExecSQL(conn_web, sql.ToString(), true);
                    if (!ret.result)
                    {
                        conn_web.Close();
                        return ret;
                    }
                }

                //дата документа по
                if (finder.plan_date_to.Trim() != "")
                {
                    sql.Remove(0, sql.Length);
                    sql.Append(" Insert into " + tXX_spfinder);
                    sql.Append(" Values (0,\'Дата документа по\',\'" + finder.plan_date_to.Trim() + "\'," + nzp_page.ToString() + ")");
                    ret = ExecSQL(conn_web, sql.ToString(), true);
                    if (!ret.result)
                    {
                        conn_web.Close();
                        return ret;
                    }
                }

                //начало документа с
                if (finder.dat_s.Trim() != "")
                {
                    sql.Remove(0, sql.Length);
                    sql.Append(" Insert into " + tXX_spfinder);
                    sql.Append(" Values (0,\'Начало периода с\',\'" + finder.dat_s.Trim() + "\'," + nzp_page.ToString() + ")");
                    ret = ExecSQL(conn_web, sql.ToString(), true);
                    if (!ret.result)
                    {
                        conn_web.Close();
                        return ret;
                    }
                }

                //начало документа по
                if (finder.dat_s_to.Trim() != "")
                {
                    sql.Remove(0, sql.Length);
                    sql.Append(" Insert into " + tXX_spfinder);
                    sql.Append(" Values (0,\'Начало периода по\',\'" + finder.dat_s_to.Trim() + "\'," + nzp_page.ToString() + ")");
                    ret = ExecSQL(conn_web, sql.ToString(), true);
                    if (!ret.result)
                    {
                        conn_web.Close();
                        return ret;
                    }
                }

                //конец периода с
                if (finder.dat_po.Trim() != "")
                {
                    sql.Remove(0, sql.Length);
                    sql.Append(" Insert into " + tXX_spfinder);
                    sql.Append(" Values (0,\'Конец периода с\',\'" + finder.dat_po.Trim() + "\'," + nzp_page.ToString() + ")");
                    ret = ExecSQL(conn_web, sql.ToString(), true);
                    if (!ret.result)
                    {
                        conn_web.Close();
                        return ret;
                    }
                }

                //конец периода по
                if (finder.dat_po_to.Trim() != "")
                {
                    sql.Remove(0, sql.Length);
                    sql.Append(" Insert into " + tXX_spfinder);
                    sql.Append(" Values (0,\'Конец периода по\',\'" + finder.dat_po_to.Trim() + "\'," + nzp_page.ToString() + ")");
                    ret = ExecSQL(conn_web, sql.ToString(), true);
                    if (!ret.result)
                    {
                        conn_web.Close();
                        return ret;
                    }
                }

                //Организация
                if (finder.nzp_supp_plant > 0)
                {
                    #region название организации

                    conn_db = GetConnection(Constants.cons_Kernel);
                    ret = OpenDb(conn_db, true);

                    sql.Remove(0, sql.Length);
#if PG
                    sql.Append(" Select name_supp from " + Points.Pref + "_kernel. supplier");
                    sql.Append(" Where nzp_supp = " + finder.nzp_supp_plant);
#else
                    sql.Append(" Select name_supp from " + Points.Pref + "_kernel: supplier");
                    sql.Append(" Where nzp_supp = " + finder.nzp_supp_plant);
#endif
                    if (!ExecRead(conn_db, out reader, sql.ToString(), true).result)
                    {
                        MonitorLog.WriteLog("Ошибка выборки " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                        ret.result = false;
                        return ret;
                    }
                    string name_supp = "";
                    if (reader != null)
                    {
                        while (reader.Read())
                        {
                            if (reader["name_supp"] != DBNull.Value) name_supp = Convert.ToString(reader["name_supp"]).Trim();
                        }
                    }
                    #endregion

                    sql.Remove(0, sql.Length);
                    sql.Append(" Insert into " + tXX_spfinder);
                    sql.Append(" Values (0,\'Организация\',\'" + name_supp.Trim() + "\'," + nzp_page.ToString() + ")");
                    ret = ExecSQL(conn_web, sql.ToString(), true);
                    if (!ret.result)
                    {
                        conn_web.Close();
                        return ret;
                    }
                }

                //причина недопоставки
                if (finder.nzp_work_type > 0)
                {
                    #region название причины

                    conn_db = GetConnection(Constants.cons_Kernel);
                    ret = OpenDb(conn_db, true);
                    sql.Remove(0, sql.Length);
#if PG
                    sql.Append(" Select name_work_type from " + Points.Pref + "_supg. s_work_type");
                    sql.Append(" Where nzp_work_type = " + finder.nzp_work_type);
#else
                    sql.Append(" Select name_work_type from " + Points.Pref + "_supg: s_work_type");
                    sql.Append(" Where nzp_work_type = " + finder.nzp_work_type);
#endif
                    if (!ExecRead(conn_db, out reader, sql.ToString(), true).result)
                    {
                        MonitorLog.WriteLog("Ошибка выборки " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                        ret.result = false;
                        return ret;
                    }
                    string name_work_type = "";
                    if (reader != null)
                    {
                        while (reader.Read())
                        {
                            if (reader["name_work_type"] != DBNull.Value) name_work_type = Convert.ToString(reader["name_work_type"]).Trim();
                        }
                    }
                    #endregion

                    sql.Remove(0, sql.Length);
                    sql.Append(" Insert into " + tXX_spfinder);
                    sql.Append(" Values (0,\'Причина недопоставки\',\'" + name_work_type.Trim() + "\'," + nzp_page.ToString() + ")");
                    ret = ExecSQL(conn_web, sql.ToString(), true);
                    if (!ret.result)
                    {
                        conn_web.Close();
                        return ret;
                    }
                }

                //услуга
                if (finder.nzp_serv > 0)
                {
                    #region название услуги

                    conn_db = GetConnection(Constants.cons_Kernel);
                    ret = OpenDb(conn_db, true);
                    sql.Remove(0, sql.Length);
#if PG
                    sql.Append("Select service from " + Points.Pref + "_kernel. services where nzp_serv = " + finder.nzp_serv);
#else
                    sql.Append("Select service from " + Points.Pref + "_kernel: services where nzp_serv = " + finder.nzp_serv);
#endif
                    if (!ExecRead(conn_db, out reader, sql.ToString(), true).result)
                    {
                        MonitorLog.WriteLog("Ошибка выборки " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                        ret.result = false;
                        return ret;
                    }
                    string service = "";
                    if (reader != null)
                    {
                        while (reader.Read())
                        {
                            if (reader["service"] != DBNull.Value)
                            { service = Convert.ToString(reader["service"]).Trim(); }
                            else
                            { service = "-"; }
                        }
                    }
                    #endregion

                    sql.Remove(0, sql.Length);
                    sql.Append(" Insert into " + tXX_spfinder);
                    sql.Append(" Values (0,\'Услуга\',\'" + service + "\'," + nzp_page.ToString() + ")");
                    ret = ExecSQL(conn_web, sql.ToString(), true);
                    if (!ret.result)
                    {
                        conn_web.Close();
                        return ret;
                    }
                }

                //тип недопоставки
                if (finder.nzp_kind > 0)
                {
                    #region название типа недопоставки

                    conn_db = GetConnection(Constants.cons_Kernel);
                    ret = OpenDb(conn_db, true);
                    sql.Remove(0, sql.Length);
#if PG
                    sql.Append("Select name from " + Points.Pref + "_data. upg_s_kind_nedop where kod_kind = 1 and nzp_kind = " + finder.nzp_kind);
#else
                    sql.Append("Select name from " + Points.Pref + "_data: upg_s_kind_nedop where kod_kind = 1 and nzp_kind = " + finder.nzp_kind);
#endif
                    if (!ExecRead(conn_db, out reader, sql.ToString(), true).result)
                    {
                        MonitorLog.WriteLog("Ошибка выборки " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                        ret.result = false;
                        return ret;
                    }
                    string kind_name = "";
                    if (reader != null)
                    {
                        while (reader.Read())
                        {
                            if (reader["name"] != DBNull.Value)
                                kind_name = Convert.ToString(reader["name"]).Trim();
                        }
                    }
                    #endregion

                    sql.Remove(0, sql.Length);
                    sql.Append(" Insert into " + tXX_spfinder);
                    sql.Append(" Values (0,\'Тип недопоставки\',\'" + kind_name + "\'," + nzp_page.ToString() + ")");
                    ret = ExecSQL(conn_web, sql.ToString(), true);
                    if (!ret.result)
                    {
                        conn_web.Close();
                        return ret;
                    }
                }

                //температура с
                if (finder.tn > 0)
                {
                    sql.Remove(0, sql.Length);
                    sql.Append(" Insert into " + tXX_spfinder);
                    sql.Append(" Values (0,\'Температура с\',\'" + finder.tn + "\'," + nzp_page.ToString() + ")");
                    ret = ExecSQL(conn_web, sql.ToString(), true);
                    if (!ret.result)
                    {
                        conn_web.Close();
                        return ret;
                    }
                }

                //температура по
                if (finder.tn_to > Constants._ZERO_)
                {
                    sql.Remove(0, sql.Length);
                    sql.Append(" Insert into " + tXX_spfinder);
                    sql.Append(" Values (0,\'Температура по\',\'" + finder.tn_to + "\'," + nzp_page.ToString() + ")");
                    ret = ExecSQL(conn_web, sql.ToString(), true);
                    if (!ret.result)
                    {
                        conn_web.Close();
                        return ret;
                    }
                }

                #endregion

                #region Акт о недопоставке

                //акт действителен
                if (finder.is_actual > 0)
                {
                    string stat = "";
                    if (finder.is_actual == 0)
                        stat = "Не важно";
                    if (finder.is_actual == 1)
                        stat = "Действителен";
                    if (finder.is_actual == 100)
                        stat = "Не действителен";
                    sql.Remove(0, sql.Length);
                    sql.Append(" Insert into " + tXX_spfinder);
                    sql.Append(" Values (0,\'Акт действителен\',\'" + stat + "\'," + nzp_page.ToString() + ")");
                    ret = ExecSQL(conn_web, sql.ToString(), true);
                    if (!ret.result)
                    {
                        conn_web.Close();
                        return ret;
                    }
                }

                //номер акта
                if (finder.number != "")
                {
                    sql.Remove(0, sql.Length);
                    sql.Append(" Insert into " + tXX_spfinder);
                    sql.Append(" Values (0,\'Номер акта\',\'" + finder.number.Trim() + "\'," + nzp_page.ToString() + ")");
                    ret = ExecSQL(conn_web, sql.ToString(), true);
                    if (!ret.result)
                    {
                        conn_web.Close();
                        return ret;
                    }
                }

                //дата акта с
                if (finder._date.Trim() != "")
                {
                    sql.Remove(0, sql.Length);
                    sql.Append(" Insert into " + tXX_spfinder);
                    sql.Append(" Values (0,\'Дата акта с\',\'" + finder._date.Trim() + "\'," + nzp_page.ToString() + ")");
                    ret = ExecSQL(conn_web, sql.ToString(), true);
                    if (!ret.result)
                    {
                        conn_web.Close();
                        return ret;
                    }
                }

                //дата акта по
                if (finder._date_to.Trim() != "")
                {
                    sql.Remove(0, sql.Length);
                    sql.Append(" Insert into " + tXX_spfinder);
                    sql.Append(" Values (0,\'Дата акта по\',\'" + finder._date_to.Trim() + "\'," + nzp_page.ToString() + ")");
                    ret = ExecSQL(conn_web, sql.ToString(), true);
                    if (!ret.result)
                    {
                        conn_web.Close();
                        return ret;
                    }
                }

                //дата регистрации с
                if (finder.reply_date.Trim() != "")
                {
                    sql.Remove(0, sql.Length);
                    sql.Append(" Insert into " + tXX_spfinder);
                    sql.Append(" Values (0,\'Дата регистрации с\',\'" + finder.reply_date.Trim() + "\'," + nzp_page.ToString() + ")");
                    ret = ExecSQL(conn_web, sql.ToString(), true);
                    if (!ret.result)
                    {
                        conn_web.Close();
                        return ret;
                    }
                }

                //дата регистрации по
                if (finder.reply_date_to.Trim() != "")
                {
                    sql.Remove(0, sql.Length);
                    sql.Append(" Insert into " + tXX_spfinder);
                    sql.Append(" Values (0,\'Дата регистрации по\',\'" + finder.reply_date_to.Trim() + "\'," + nzp_page.ToString() + ")");
                    ret = ExecSQL(conn_web, sql.ToString(), true);
                    if (!ret.result)
                    {
                        conn_web.Close();
                        return ret;
                    }
                }

                //поставщик услуг
                if (finder.nzp_supp > 0)
                {
                    #region название поставщика услуг

                    conn_db = GetConnection(Constants.cons_Kernel);
                    ret = OpenDb(conn_db, true);

                    sql.Remove(0, sql.Length);
#if PG
                    sql.Append(" Select name_supp from " + Points.Pref + "_kernel. supplier");
#else
                    sql.Append(" Select name_supp from " + Points.Pref + "_kernel: supplier");
#endif
                    sql.Append(" Where nzp_supp = " + finder.nzp_supp);

                    if (!ExecRead(conn_db, out reader, sql.ToString(), true).result)
                    {
                        MonitorLog.WriteLog("Ошибка выборки " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                        ret.result = false;
                        return ret;
                    }
                    string name_supp = "";
                    if (reader != null)
                    {
                        while (reader.Read())
                        {
                            if (reader["name_supp"] != DBNull.Value) name_supp = Convert.ToString(reader["name_supp"]).Trim();
                        }
                    }
                    #endregion

                    sql.Remove(0, sql.Length);
                    sql.Append(" Insert into " + tXX_spfinder);
                    sql.Append(" Values (0,\'Поставщик услуг\',\'" + name_supp.Trim() + "\'," + nzp_page.ToString() + ")");
                    ret = ExecSQL(conn_web, sql.ToString(), true);
                    if (!ret.result)
                    {
                        conn_web.Close();
                        return ret;
                    }
                }

                #endregion

                #endregion

                return ret;
            }


            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры  SaveFinder : " + ex.Message, MonitorLog.typelog.Error, true);
                ret.result = false;
                return ret;
            }
            finally
            {
                #region Закрытие соединений

                if (conn_db != null)
                {
                    conn_db.Close();
                }

                if (conn_web != null)
                {
                    conn_web.Close();
                }

                if (reader != null)
                {
                    reader.Close();
                }

                #endregion
            }
        }

        /// <summary>
        /// получить информацию о выполненном последнем поиске
        /// </summary>
        /// <param name="nzp_user"></param>
        /// <param name="ret"></param>
        /// <returns></returns>
        public DataTable GetReportInfo(int nzp_user, out Returns ret)
        {
            ret = Utils.InitReturns();
            DataTable res_table = new DataTable();
            IDbConnection con_web = null;
            IDbConnection con_db = null;
            IDataReader reader = null;
            string sql = "";

            if (nzp_user < 1)
            {
                ret.result = false;
                ret.text = "Не определен пользователь";
                return null;
            }
            try
            {
                con_web = GetConnection(Constants.cons_Webdata);
                ret = OpenDb(con_web, true);
                if (!ret.result) return null;

                string table_name = "t" + Convert.ToString(nzp_user) + "_spfinder";
#if PG
                sql = "Select name, value From " + con_web.Database + "." + table_name + " Where nzp_page = " + Constants.page_planned_works;
#else
                sql = "Select name, value From " + con_web.Database + ":" + table_name + " Where nzp_page = " + Constants.page_planned_works;
#endif
                if (!ExecRead(con_web, out reader, sql.ToString(), true).result)
                {
                    MonitorLog.WriteLog("Ошибка выборки GetReportInfo" + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                    ret.result = false;
                    return null;
                }

                try
                {
                    if (reader != null)
                    {
                        res_table.Load(reader, LoadOption.OverwriteChanges);
                    }
                }
                catch (Exception ex)
                {
                    MonitorLog.WriteLog("Ошибка при записи данных в DataTable в GetReportInfo" + ex.Message, MonitorLog.typelog.Error, true);
                    con_db.Close();
                    reader.Close();
                    return null;
                }
                return res_table;
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры  GetReportInfo : " + ex.Message, MonitorLog.typelog.Error, true);
                return null;
            }
            finally
            {
                if (con_web != null)
                {
                    con_web.Close();
                }

                if (reader != null)
                {
                    reader.Close();
                }
            }
        }

    }
}
