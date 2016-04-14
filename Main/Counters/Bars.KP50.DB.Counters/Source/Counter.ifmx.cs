using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Data;
using System.Text;
using System.Threading;
using Bars.KP50.Utils;
using FastReport;
using System.IO;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;
using System.Security.AccessControl;

namespace STCLINE.KP50.DataBase
{
    //----------------------------------------------------------------------
    public partial class DbCounter : DbCounterKernel
    //----------------------------------------------------------------------
    {
        public enum PrepareCounterValMode
        {
            SpisVal,
            CountersReadings
        }

        //!!!----------------------------------------------
        //----------------------------------------------------------------------
        public void FindPu(Counter finder, out Returns ret) //найти и заполнить список адресов
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();

            if (!Points.IsFabric || (finder.nzp_wp > 0))
            {
                //поиск в конкретном банке
                ret = FindPu00(finder, 0);
            }
            else
            {
                //параллельный поиск по серверам БД
                FindPuInThreads(finder, out ret);
            }
        }
        //----------------------------------------------------------------------
        private void FindPuInThreads(Counter finder, out Returns ret) //
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            if (finder.nzp_user < 1)
            {
                ret.result = false;
                ret.text = "Не определен пользователь";
                return;
            }

            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result) return;

            string tXX_meta = "t" + Convert.ToString(finder.nzp_user) + "_meta2";

            //создать таблицу контроля
            if (TableInWebCashe(conn_web, tXX_meta))
            {
                ExecSQL(conn_web, " Drop table " + tXX_meta, false);
            }

#if PG
            string date_time = DBManager.sDateTimeType;
#else
            string date_time = "datetime year to minute";
#endif
            ret = ExecSQL(conn_web,
                         " Create table " + tXX_meta +
                         " ( nzp_server integer," +
                         "   dat_in     " + date_time + ", " +
                         "   dat_out    " + date_time + ", " +
                         "   kod        integer default 0 " +
                         " ) ");
            if (!ret.result)
            {
                conn_web.Close();
                return;
            }
            ret = ExecSQL(conn_web, " Alter table " + tXX_meta + "  lock mode (row) ", true);

            //открываем цикл по серверам БД
            foreach (_Server server in Points.Servers)
            {
                int nzp_server = server.nzp_server;
                System.Threading.Thread thServer =
                                new System.Threading.Thread(delegate() { FindPu01(finder, nzp_server); });
                thServer.Start();
                //MyThread1.Join();
            }

            //а пока создадим кэш-таблицы лицевых счетов и домов, чтобы время не терять пока потоки делают свою работу
            string tXX_cnt = "t" + Convert.ToString(finder.nzp_user) + "_pu";

            CreateTableWebCnt(conn_web, tXX_cnt, true, out ret);
            if (!ret.result)
            {
                conn_web.Close();
                return;
            }

            //дождаться и соединить результаты
            IDataReader reader;

            string sql = " Select distinct kod From " + tXX_meta;

            while (true)
            {
                ret = ExecRead(conn_web, out reader, sql, true);
                if (!ret.result)
                {
                    conn_web.Close();
                    return;
                }

                bool b = true;
                try
                {
                    while (reader.Read())
                    {
                        int kod = (int)reader["kod"];

                        if (kod == -1) //ошибка выполнения!!
                        {
                            conn_web.Close();
                            ret.result = false;
                            return;
                        }
                        if (kod == 0) //еще есть невыполнениые задания
                        {
                            b = false;
                            break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    conn_web.Close();

                    ret.result = false;
                    ret.text = ex.Message;

                    MonitorLog.WriteLog("Ошибка контроля выполнения " + ex.Message, MonitorLog.typelog.Error, 20, 201, true);
                    return;
                }

                if (b)
                {
                    //все потоки выполнились, выходим 
                    break;
                }

                Thread.Sleep(1000); //продолжаем ждать
            }

            //далее соединяем результаты
            //открываем цикл по серверам БД
            foreach (_Server server in Points.Servers)
            {
                string tXX_cnt_local = "t" + Convert.ToString(finder.nzp_user) + "_pu" + server.nzp_server.ToString();
                ret = ExecSQL(conn_web, " Insert into " + tXX_cnt + " Select * From " + tXX_cnt_local, true);
                if (!ret.result)
                {
                    conn_web.Close();
                    return;
                }
            }

            //построим индексы
            CreateTableWebCnt(conn_web, tXX_cnt, false, out ret);
            if (!ret.result)
            {
                conn_web.Close();
                return;
            }

            conn_web.Close();
        }
        //----------------------------------------------------------------------
        private void FindPu01(Counter finder, int nzp_server) //вызов из потока
        //----------------------------------------------------------------------
        {
            Returns ret = FindPu00(finder, nzp_server);

            if (!ret.result)
            {
                //вылетел по ошибке - надо собщить контролю
                IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
                ret = OpenDb(conn_web, true);
                if (!ret.result) return;

                string tXX_meta = "t" + Convert.ToString(finder.nzp_user) + "_meta2";

                //создать таблицу контроля
                ExecSQL(conn_web,
                                " Update " + tXX_meta +
                                " Set kod = -1, dat_out = " + DBManager.sCurDateTime + "  Where nzp_server = " + nzp_server, true);
                conn_web.Close();
            }
        }
        //----------------------------------------------------------------------
        private void CreateTableWebCnt(IDbConnection conn_web, string tXX_cnt, bool onCreate, out Returns ret) //
        //----------------------------------------------------------------------
        {
            if (onCreate)
            {
                if (TableInWebCashe(conn_web, tXX_cnt))
                {
                    ExecSQL(conn_web, " Drop table " + tXX_cnt, false);
                }

                //создать таблицу webdata:tXX_cnt
                ret = ExecSQL(conn_web,
                    " Create table " + tXX_cnt +
                    " ( nzp_kvar    integer, " +
                    "   num_ls      integer, " +
                    "   nzp_dom     integer, " +
                    "   nzp_type    integer, " +
                    "   nzp_serv    integer, " +
                    "   name_uchet  char(30), " +
                    "   is_pl       integer, " +
                    "   nzp_counter integer, " +
                    "   service     char(100)," +
                    "   num_cnt     char(40)," +
                    "   name_type   char(70)," +
                    "   nzp_cnttype integer," +
                    "   cnt_stage   integer, " +
                    "   mmnog       " + DBManager.sDecimalType + "(14,7), " +
                    "   dat_close   char(10)," +
                    "   dat_prov    char(10)," +
                    "   dat_provnext char(10)," +
                    "   dat_oblom    char(10)," +
                    "   dat_poch     char(10)," +
                    "   adr          char(80), " +
                    "   ikvar        integer,  " +
                    "   idom         integer,  " +
                    "   pref         char(20),  " +
                    "   comments     char(2000)," +
                    "   nzp_counter_old integer " + //№ родительского ПУ при замене ПУ
                    " ) ", true);
                if (!ret.result)
                {
                    conn_web.Close();
                    return;
                }
            }
            else
            {
                ret = ExecSQL(conn_web, " Create index ix1_" + tXX_cnt + " on " + tXX_cnt + " (nzp_kvar,nzp_dom,pref) ", true);
                if (ret.result) { ret = ExecSQL(conn_web, " Create index ix_2" + tXX_cnt + " on " + tXX_cnt + " (nzp_counter,pref) ", true); }
                if (ret.result) { ret = ExecSQL(conn_web, " Create index ix_3" + tXX_cnt + " on " + tXX_cnt + " (nzp_dom,pref) ", true); }
                if (ret.result) { ret = ExecSQL(conn_web, DBManager.sUpdStat + " " + tXX_cnt, true); }
            }
        }
        //!!!----------------------------------------------

        /// <summary> Загрузить список ПУ в кэш
        /// </summary>
        private Returns FindPu00(Counter finder, int nzp_server) //найти и заполнить список ПУ
        {
            string numt = "";

            Returns ret = Utils.InitReturns();
            if (finder.nzp_user < 1)
            {
                ret.result = false;
                ret.text = "Не определен пользователь";
                return ret;
            }

            if (finder.prevPage == Constants.page_findls)
            {
                //вызов следует из шаблона адресов, поэтому
                //надо прежде заполнить список адресов
                var db = new DbAdres();
                try
                {
                    db.FindLs(finder, out ret);
                }
                catch (Exception ex)
                {
                    MonitorLog.WriteLog(ex.Message, MonitorLog.typelog.Error, true);
                    ret.result = false;
                    ret.text = ex.Message;
                    return ret;
                }
                finally
                {
                    db.Close();
                }

                if (!ret.result) return ret;
            }




            var connWeb = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(connWeb, true);
            if (!ret.result) return ret;
#if PG
            ExecSQL(connWeb, "set search_path to 'public'", false);
#endif

            //заполнить webdata:tXX_cnt
            var connKernel = Points.GetConnKernel(finder.nzp_wp, nzp_server);
            if (connKernel == "")
            {
                connWeb.Close();
                ret.result = false;
                ret.text = "Не определен connect к БД";
                return ret;
            }

            var connDb = GetConnection(connKernel);
            ret = OpenDb(connDb, true);
            if (!ret.result)
            {
                return ret;
            }

            try
            {
                var tXX_cnt = "t" + finder.nzp_user + "_pu";
                var tXX_meta = "t" + finder.nzp_user + "_meta2";

                if (nzp_server > 0)
                {
                    numt = nzp_server.ToString();

                    //сообщит контролю, что процесс стартовал
                    ExecSQL(connWeb,
                        " Insert into " + tXX_meta + "(nzp_server, dat_in) " +
                        " Values (" + nzp_server + ", " + sCurDateTime + " )", true);
                }
                else if (finder.nzp_kvar > 0 || finder.nzp_dom > 0)
                {
                    numt = "2";
                    tXX_cnt += numt;
                }

                //создать кэш-таблицу
                CreateTableWebCnt(connWeb, tXX_cnt, true, out ret);
                if (!ret.result)
                {
                    return ret;
                }

                string cur_pref;
                string ws_ls = WhereString(finder);
                string ws_dom = ws_ls;
                bool load_cnt_dom; //выборка домовых и групповых ПУ

                var tXX_cnt_full = DBManager.GetFullBaseName(connWeb) + tableDelimiter + tXX_cnt;

                if (finder.prevPage == Constants.page_findls) //вызов из поиска адресов
                {
                    load_cnt_dom = true;
                    if (finder.nkvar != "" || finder.nzp_kvar > 0) load_cnt_dom = false;

                    var tXX_spls = DBManager.GetFullBaseName(connWeb) + tableDelimiter + "t" + finder.nzp_user + "_spls";

                    ws_ls += " and exists ( Select 1 From " + tXX_spls + " spls where spls.nzp_kvar=k.nzp_kvar )  "; //выборка из кеша
                    ws_dom += " and exists ( Select 1  From " + tXX_spls + " spls where spls.nzp_dom=d.nzp_dom )  ";
                }
                else if (finder.nzp_type == (int)CounterKinds.Dom || finder.nzp_type == (int)CounterKinds.Group ||
                         finder.nzp_type == (int)CounterKinds.Communal)
                    load_cnt_dom = true;
                else
                    load_cnt_dom = false;

                //цикл по s_point
                foreach (_Point zap in Points.PointList)
                {
                    if (nzp_server > 0)
                    {
                        if (zap.nzp_server != nzp_server) continue;
                    }
                    if (finder.nzp_wp > 0)
                    {
                        if (zap.nzp_wp != finder.nzp_wp) continue;
                    }
                    else if (finder.pref != "")
                    {
                        if (zap.pref != finder.pref) continue;
                    }

                    if (!Utils.IsInRoleVal(finder.RolesVal, zap.nzp_wp.ToString(), Constants.role_sql,
                        Constants.role_sql_wp)) continue;

                    cur_pref = zap.pref;

                    ws_ls = ws_ls.Replace("PREFX", cur_pref);
                    ws_dom = ws_dom.Replace("PREFX", cur_pref);

                    var sql = new StringBuilder();
                    sql.Append(" Insert into " + tXX_cnt_full +
                               " ( pref,nzp_kvar,num_ls,ikvar,nzp_dom,idom,adr, nzp_type,nzp_serv,service,num_cnt," +
                               "   nzp_counter, name_type,cnt_stage,mmnog,dat_close,dat_prov,dat_provnext, dat_oblom, dat_poch, nzp_cnttype," +
                               " comments, nzp_counter_old ) ");
                    sql.Append(" SELECT distinct '" + cur_pref + "' ");

                    sql.Append(", k.nzp_kvar ");
                    sql.Append(", k.num_ls ");
                    sql.Append(", ikvar ");
                    sql.Append(", d.nzp_dom, idom ");

                    sql.Append(", trim(" + DBManager.sNvlWord + "(u.ulica, '')) || ' / ' || TRIM(" + DBManager.sNvlWord + "(r.rajon, '')) || " + 
                        "'   дом ' || TRIM(" + DBManager.sNvlWord + "(ndom, '')) || '  корп. ' || TRIM (" + DBManager.sNvlWord + "(nkor, '')) || " + 
                        " '  кв. ' || TRIM(" + DBManager.sNvlWord + "(nkvar, '')) || '  ком. ' || TRIM (" + DBManager.sNvlWord + "(nkvar_n, '')) AS adr ");
                    sql.Append(
                        ",c.nzp_type, c.nzp_serv, " +
                        "(case when c.nzp_cnt in (15, 16) then cntdop.name || ' (' || mdop.measure || ')' else cnt.name || ' (' || m.measure || ')' end) service, "+
                        "c.num_cnt, c.nzp_counter, name_type, cnt_stage, mmnog, dat_close, dat_prov, dat_provnext, dat_oblom, dat_poch," +
                        " n.nzp_cnttype, c.comment, MAX(crpld.nzp_counter_old) ");
                    sql.Append(" FROM ");
                    sql.Append(cur_pref + "_data" + tableDelimiter + "counters_spis c ");
                    sql.Append(
                    "   left outer join " + cur_pref + DBManager.sDataAliasRest + "counters_replaced crpld" +
                    "   on crpld.nzp_counter_new = c.nzp_counter and crpld.is_actual " +
                    "   left outer join " + cur_pref + "_kernel" + DBManager.tableDelimiter + "s_counts cnt " +
                    "       left outer join " + cur_pref + "_kernel" + DBManager.tableDelimiter + "s_measure   m  on cnt.nzp_measure = m.nzp_measure " +
                    "   on c.nzp_cnt = cnt.nzp_cnt " +
                    "   left outer join " + cur_pref + "_kernel" + DBManager.tableDelimiter + "s_countsdop cntdop " +
                    "       left outer join " + cur_pref + "_kernel.s_measure   mdop  on cntdop.nzp_measure = mdop.nzp_measure  " +
                    "   on c.nzp_cnt = cntdop.nzp_cnt, ");
                    //sql.Append(cur_pref + "_kernel" + tableDelimiter + "services s, ");
                    sql.Append(cur_pref + "_kernel" + tableDelimiter + "s_counttypes n, ");
                    sql.Append(cur_pref + "_data" + tableDelimiter + "kvar k, ");
                    sql.Append(cur_pref + "_data" + tableDelimiter + "dom d, ");
                    sql.Append(Points.Pref + "_data" + tableDelimiter + "s_ulica u ");
                    sql.Append(" left outer join " + Points.Pref + "_data" + tableDelimiter + "s_rajon r on u.nzp_raj=r.nzp_raj ");

                    sql.Append(" Where d.nzp_ul=u.nzp_ul ");

                    sql.Append("   and c.nzp = k.nzp_kvar and c.nzp_type = 3  and k.nzp_dom = d.nzp_dom  ");
                    //if (finder.nzp_kvar > 0) //вызов списка ПУ из списка лс (не для шаблона поиска)
                    //{
                    //    sql.Append("  and k.nzp_dom = d.nzp_dom and c.nzp = k.nzp_kvar ");
                    //}
                    //else
                    //{
                    //    if (finder.nzp_type == (int) CounterKinds.Group || finder.nzp_type == (int) CounterKinds.Dom ||
                    //        finder.nzp_type == (int) CounterKinds.Communal)
                    //        sql.Append(" and c.nzp = d.nzp_dom");
                    //}

                    sql.Append(" and c.nzp_cnttype = n.nzp_cnttype and c.is_actual <> 100  ");

                    //if (finder.get_koss) sql.Append("    and c.nzp_serv in (25,210,11,242) ");

                    string s = ws_ls + WhereVal(finder, cur_pref);

                    if (s.Trim() != "")
                        sql.Append(s);
                    else
                        sql.Append(" and d.nzp_dom = -111 "); //если условия не указаны, то ничего не выберем

                    sql.Append(" GROUP BY 1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19,20,21,22");
                    //записать текст sql в лог-журнал
                    int key = LogSQL(connWeb, finder.nzp_user, sql.ToString());

                    ret = ExecSQL(connDb, sql.ToString(), true);
                    if (!ret.result)
                    {
                        if (key > 0)
                        {
                            LogSQL_Error(connWeb, key, ret.text);
                        }

                        return ret;
                    }

                    if (load_cnt_dom)
                    {
                        sql.Remove(0, sql.Length);
                        sql.Append(" Insert into " + tXX_cnt_full +
                                   " ( pref,nzp_kvar,num_ls,nzp_dom,ikvar,idom,adr, nzp_type,nzp_serv,service,num_cnt," +
                                   "   nzp_counter, name_type,cnt_stage,mmnog,dat_close,dat_prov,dat_provnext, dat_oblom, dat_poch," +
                                   "   nzp_cnttype, name_uchet, is_pl, nzp_counter_old  ) ");
                        sql.Append(" Select distinct '" + cur_pref + "',0,0, d.nzp_dom, 0,idom, ");

                        sql.Append("   trim(" + DBManager.sNvlWord + "(u.ulica,''))||' / '||trim(" + DBManager.sNvlWord + "(r.rajon,''))||'   дом '||" +
                                   "   trim(" + DBManager.sNvlWord + "(ndom,''))||'  корп. '|| trim(" + DBManager.sNvlWord + "(nkor,'')) as adr, ");
                        sql.Append(
                            "   c.nzp_type,c.nzp_serv,"+
                             "(case when c.nzp_cnt in (15, 16) then cntdop.name || ' (' || mdop.measure || ')' else cnt.name || ' (' || m.measure || ')' end) service"+
                            ",c.num_cnt, c.nzp_counter, name_type,cnt_stage,mmnog,dat_close,dat_prov,dat_provnext, dat_oblom, dat_poch," +
                            " n.nzp_cnttype, tu.name_uchet, tu.is_pl,  MAX(crpld.nzp_counter_old) ");
                        sql.Append(" FROM ");
                        sql.Append(cur_pref + "_data" + tableDelimiter + "counters_spis c");
                        sql.Append(
                            "   left outer join " + cur_pref + DBManager.sDataAliasRest + "counters_replaced crpld" +
                            "   on crpld.nzp_counter_new = c.nzp_counter and crpld.is_actual ");
                        sql.Append(" left outer join " + cur_pref + "_kernel" + tableDelimiter + "s_typeuchet tu on tu.is_pl = c.is_pl");
                        sql.Append("   left outer join " + cur_pref + "_kernel" + DBManager.tableDelimiter + "s_counts cnt " +
                                    "       left outer join " + cur_pref + "_kernel" + DBManager.tableDelimiter + "s_measure   m  on cnt.nzp_measure = m.nzp_measure " +
                                    "   on  c.nzp_cnt = cnt.nzp_cnt " +
                                    "   left outer join " + cur_pref + "_kernel" + DBManager.tableDelimiter + "s_countsdop cntdop " +
                                    "       left outer join " + cur_pref + "_kernel.s_measure   mdop  on cntdop.nzp_measure = mdop.nzp_measure  " +
                                    "   on  c.nzp_cnt = cntdop.nzp_cnt, ");
                        //   sql.Append(cur_pref + "_kernel" + tableDelimiter + "services s,");
                        sql.Append(cur_pref + "_kernel" + tableDelimiter + "s_counttypes n,");

                        sql.Append(Points.Pref + "_data" + tableDelimiter + "dom d, ");
                        sql.Append(Points.Pref + "_data" + tableDelimiter + "s_ulica u");
                        sql.Append(" left outer join " + Points.Pref + "_data" + tableDelimiter + "s_rajon r on u.nzp_raj=r.nzp_raj ");

                        sql.Append(" Where d.nzp_ul=u.nzp_ul " +
                                   "   and c.nzp = d.nzp_dom and c.nzp_type in (1,2,4) " +
                                   "   and c.nzp_cnttype = n.nzp_cnttype ");

                        //if (finder.get_koss) sql.Append("    and c.nzp_serv in (25,210,11,242) ");

                        s = ws_dom + WhereVal(finder, cur_pref);

                        sql.Append(s.Trim() != "" ? s : " and d.nzp_dom = -111 "); //если условия не указаны, то ничего не выберем

                        sql.Append(" GROUP BY 1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19,20,21,22,23");
                        //записать текст sql в лог-журнал
                        //key = LogSQL(conn_web, finder.nzp_user, sql.ToString());

                        ret = ExecSQL(connDb, sql.ToString(), true);
                        if (!ret.result)
                        {
                            //if (key > 0) LogSQL_Error(conn_web, key, ret.text);
                            return ret;
                        }
                    }
                }

                //далее работаем с кешем
                //создаем индексы на tXX_cnt
                CreateTableWebCnt(connWeb, tXX_cnt, false, out ret);
                if (!ret.result)
                {
                    return ret;
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog(ex.Message, MonitorLog.typelog.Error, true);
                ret.result = false;
                ret.text = ex.Message;
            }
            finally
            {
                connWeb.Close();
                connDb.Close();
            }

            return ret;
        } //FindPu

        private string GetRashodKOplate(IDbConnection conn_db, Counter finder)
        {
            if (finder.nzp_serv <= 0) return "";

            string table = finder.pref + "_charge_" + (finder.year_ % 100).ToString("00") + DBManager.tableDelimiter + "calc_gku_" + finder.month_.ToString("00");
            if (TempTableInWebCashe(conn_db, table))
            {
                string sql = "select max(rashod) as val1 from " + table + " where nzp_serv =" + finder.nzp_serv +
                    " and nzp_kvar = " + finder.nzp_kvar;
                IDataReader reader2;
                Returns ret = ExecRead(conn_db, out reader2, sql, true);
                if (!ret.result) return "";
                string val = "";
                if (reader2.Read())
                {
                    if (reader2["val1"] != DBNull.Value) val = Convert.ToDecimal(reader2["val1"]).ToString("0.0000");
                }
                if (reader2 != null) reader2.Close();
                return val;
            }
            else return "";
        }

        /// <summary> Определить максимальную дату учета
        /// </summary>
        /// <returns>Заполняется поле dat_uchet</returns>
        public Counter FindMaxDatUchet(Counter finder, out Returns ret) //найти и заполнить список показаний ПУ
        {
            ret = Utils.InitReturns();
            if (finder.nzp_user < 1)
            {
                ret.result = false;
                ret.text = "Не определен пользователь";
                return null;
            }

            if (finder.pref.Trim() == "")
            {
                ret.result = false;
                ret.text = "Не задан префикс базы данных";
                return null;
            }

            if (!((finder.nzp_type == (int)CounterKinds.Dom && finder.nzp_dom > 0) ||
                (finder.nzp_type == (int)CounterKinds.Kvar && finder.nzp_kvar > 0) ||
                (finder.nzp_type == (int)CounterKinds.Group && (finder.nzp_kvar > 0 || finder.nzp_dom > 0)) ||
                (finder.nzp_type == (int)CounterKinds.Communal && finder.nzp_dom > 0)
                ))
            {
                ret.result = false;
                ret.text = "Неверные входные параметры";
                return null;
            }

            string cur_pref = finder.pref.Trim();

            #region Соединение с БД
            string connectionString = Points.GetConnByPref(finder.pref);
            IDbConnection conn_db = GetConnection(connectionString);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return null;
            #endregion

            StringBuilder sql = new StringBuilder();

            sql.Append(" Select max(dat_uchet) as dat_uchet ");
            sql.Append(" From ");
            sql.Append(cur_pref + "_data" + DBManager.tableDelimiter + "counters_spis b, ");
            switch (finder.nzp_type)
            {
                case (int)CounterKinds.Kvar:     // показания квартирных ПУ
                    sql.Append(cur_pref + "_data" + DBManager.tableDelimiter + "counters a ");
                    sql.Append(" Where a.nzp_kvar = " + finder.nzp_kvar);
                    break;
                case (int)CounterKinds.Dom:      // показания домовых ПУ
                    sql.Append(cur_pref + "_data" + DBManager.tableDelimiter + "counters_dom a ");
                    sql.Append(" Where a.nzp_dom = " + finder.nzp_dom);
                    break;
                case (int)CounterKinds.Group:     // показания групповых ПУ
                case (int)CounterKinds.Communal: // показания коммунальных ПУ
                    sql.Append(cur_pref + "_data" + DBManager.tableDelimiter + "counters_group a ");
                    if (finder.nzp_kvar > 0)
                    {
                        sql.Append(" Where exists (select nzp_kvar from " + cur_pref + "_data" + DBManager.tableDelimiter + "counters_link cl where cl.nzp_counter = a.nzp_counter and cl.nzp_kvar = " + finder.nzp_kvar + ")");
                    }
                    else if (finder.nzp_dom > 0)
                    {
                        sql.Append(" Where b.nzp = " + finder.nzp_dom);
                    }
                    break;
            }
            sql.Append(" and a.nzp_counter = b.nzp_counter ");
            sql.Append(" and a.is_actual <> 100 ");
            if (finder.nzp_counter > 0) sql.Append(" and b.nzp_counter = " + finder.nzp_counter.ToString());
            if (finder.RolesVal != null)
                foreach (_RolesVal role in finder.RolesVal)
                    if (role.tip == Constants.role_sql && role.kod == Constants.role_sql_serv) sql.Append(" and b.nzp_serv in (" + role.val + ") ");

            IDataReader reader;
            ret = ExecRead(conn_db, out reader, sql.ToString(), true);
            if (!ret.result)
            {
                conn_db.Close();
                return null;
            }

            try
            {
                Counter counter = new Counter();
                if (reader.Read())
                {
                    if (reader["dat_uchet"] != DBNull.Value) counter.dat_uchet = String.Format("{0:dd.MM.yyyy}", reader["dat_uchet"]);
                }

                reader.Close();
                conn_db.Close();
                return counter;
            }
            catch (Exception ex)
            {
                reader.Close();
                conn_db.Close();

                ret.result = false;
                ret.text = ex.Message;

                string err;
                if (Constants.Viewerror) err = " \n " + ex.Message;
                else err = "";

                MonitorLog.WriteLog("Ошибка заполнения списка показаний ПУ " + err, MonitorLog.typelog.Error, 20, 201, true);

                return null;
            }
        }

        public List<CounterCnttypeLight> LoadCntType(Counter finder, out Returns ret)
        {
            if (finder.nzp_user < 1)
            {
                ret = new Returns(false, "Не определен пользователь");
                return null;
            }

            if (finder.nzp_cnttype > 0 && finder.pref == "")
            {
                ret = new Returns(false, "Не задан префикс");
                return null;
            }

            string connectionString = Points.GetConnByPref(finder.pref);
            IDbConnection conn_db = GetConnection(connectionString);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return null;

            List<CounterCnttypeLight> list = LoadCntType(finder, conn_db, out ret);

            conn_db.Close();

            return list;
        }

        private List<CounterCnttypeLight> LoadCntType(Counter finder, IDbConnection conn_db, out Returns ret)
        {
            ret = Utils.InitReturns();

            StringBuilder sql;
            IDataReader reader;
            List<CounterCnttypeLight> listCntType = new List<CounterCnttypeLight>();
            CounterCnttypeLight ctype;

            foreach (_Point zap in Points.PointList)
            {
                if (!Utils.IsInRoleVal(finder.RolesVal, zap.nzp_wp.ToString(), Constants.role_sql, Constants.role_sql_wp)) continue;

                if (finder.pref != "" && finder.pref != zap.pref) continue;

                string cur_pref = zap.pref;

                sql = new StringBuilder();
                
                sql.Append("Select nzp_cnttype, name_type, cnt_stage, mmnog from " + cur_pref + "_kernel" + DBManager.tableDelimiter + "s_counttypes n ");
                sql.Append(" Where 1=1 ");
                if (finder.cnt_type.Trim() != "") sql.Append(" and upper(name_type) like '%" + finder.cnt_type.Trim().ToUpper() + "%'");
                if (finder.nzp_cnttype > 0) sql.Append(" and nzp_cnttype = " + finder.nzp_cnttype);
                sql.Append(" Order by name_type");

                ret = ExecRead(conn_db, out reader, sql.ToString(), true);
                if (!ret.result)
                {
                    return null;
                }
                while (reader.Read())
                {
                    ctype = new CounterCnttypeLight();
                    
                    if (reader["nzp_cnttype"] != DBNull.Value) ctype.nzp_cnttype = (int)reader["nzp_cnttype"];
                    if (reader["name_type"] != DBNull.Value) ctype.name_type = ((string)reader["name_type"]).Trim();
                    
                    if (finder.pref != "")
                    {
                        if (reader["cnt_stage"] != DBNull.Value) ctype.cnt_stage = (int)reader["cnt_stage"];
                        ctype.name = ctype.name_type + " (разр: " + ctype.cnt_stage + ")";
                        if (reader["mmnog"] != DBNull.Value) ctype.mmnog = Convert.ToDecimal(reader["mmnog"]);
                        listCntType.Add(ctype);
                    }
                    else
                    {
                        ctype.name = ctype.name_type;
                        if (listCntType.IndexOf(ctype) < 0) listCntType.Add(ctype);
                    }
                }
                reader.Close();
            }
            return listCntType;
        }

        public Returns SaveCntType(CounterCnttype finder)
        {
            Returns ret = Utils.InitReturns();

            if (finder.nzp_user < 1) return new Returns(false, "Не определен пользователь");
            if (finder.pref == "") return new Returns(false, "Не определен префикс");
            if (finder.cnt_stage <= 0) return new Returns(false, "Не задана разрядность прибора учета", -1);
            if (finder.mmnog <= 0) return new Returns(false, "Не задан масштабный множитель", -1);
            if (finder.name_type.Trim() == "") return new Returns(false, "Не задано наименование типа прибора учета", -1);

            string connectionString = Points.GetConnByPref(finder.pref);
            IDbConnection conn_db = GetConnection(connectionString);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return ret;

            string sql = "select * from " + finder.pref + "_kernel" + DBManager.tableDelimiter + "s_counttypes where upper(name_type) = " + Utils.EStrNull(finder.name_type.Trim().ToUpper()) + " and nzp_cnttype <> " + finder.nzp_cnttype;

            IDataReader reader;
            ret = ExecRead(conn_db, out reader, sql, true);
            if (!ret.result)
            {
                conn_db.Close();
                return ret;
            }

            if (reader.Read())
            {
                reader.Close();
                conn_db.Close();
                return new Returns(false, "Тип прибора учета с таким наименованием уже существует", -1);
            }

            if (finder.nzp_cnttype > 0) // изменение существующего типа ПУ
            {
                sql = "update " + finder.pref + "_kernel" + DBManager.tableDelimiter + "s_counttypes set name_type = " + Utils.EStrNull(finder.name_type.Trim()) +
                    ", cnt_stage = " + finder.cnt_stage +
                    ", mmnog = " + finder.mmnog +
                    " Where nzp_cnttype = " + finder.nzp_cnttype;
                ret = ExecSQL(conn_db, sql, true);
            }
            else // добавление нового типа ПУ
            {
                sql = "Insert into " + finder.pref + "_kernel" + DBManager.tableDelimiter + "s_counttypes (cnt_stage, name_type, mmnog) " +
                    " Values (" + finder.cnt_stage + "," + Utils.EStrNull(finder.name_type.Trim()) + "," + finder.mmnog + ")";
                ret = ExecSQL(conn_db, sql, true);
            }

            conn_db.Close();

            return ret;
        }

        /// <summary>
        /// Получение списка закрытых ПУ, которые могут быть заменены текущим ПУ
        /// </summary>
        /// <param name="finder"></param>
        /// <param name="ret"></param>
        /// <returns></returns>
        public List<ReplacedCounter> LoadPuForReplacing(Counter finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            var list = new List<ReplacedCounter>();

            if (finder.nzp_user < 1)
            {
                ret = new Returns(false, "Не задан пользователь");
                return list;
            }
            if (finder.pref == "")
            {
                ret = new Returns(false, "Не определен префикс");
                return list;
            }
            if (finder.nzp_counter < 1 && (finder.nzp_serv < 1 || finder.nzp < 1))
            {
                ret = new Returns(false, "Не хватает данных: либо кода ПУ, либо услуги и ЛС/дома");
                return list;
            } 

            string connectionString = Points.GetConnByPref(finder.pref);
            IDbConnection conn_db = GetConnection(connectionString);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return list;

            string temp_table = "ttable_pu_for_repl" + DateTime.Now.Ticks;

            try
            {
                string sql;
                DataTable dt;

                #region определяем nzp_serv и nzp по ПУ, для которого осуществляется поиск (если nzp_counter > 0); либо nzp_serv по nzp_cnt

                if (finder.nzp_counter > 0)
                {
                    sql =
                        " SELECT cs.nzp_serv, cs.nzp, cs.dat_close" +
                        " FROM " + finder.pref + DBManager.sDataAliasRest + "counters_spis cs" +
                        " WHERE cs.nzp_counter = " + finder.nzp_counter + "";
                    dt = DBManager.ExecSQLToTable(conn_db, sql);
                    if (dt.Rows.Count < 1)
                    {
                        ret = new Returns(false, "ПУ, для которого осуществляется поиск, не найден в системе");
                        return list;
                    }

                    if (dt.Rows[0]["nzp_serv"] == DBNull.Value)
                    {
                        ret = new Returns(false, "Невозможно определить услугу ПУ, для которого осуществляется поиск");
                        return list;
                    }
                    finder.nzp_serv = (int) dt.Rows[0]["nzp_serv"];

                    if (dt.Rows[0]["nzp"] == DBNull.Value)
                    {
                        ret = new Returns(false,
                            "Невозможно определить код ЛС/дома ПУ, для которого осуществляется поиск");
                        return list;
                    }
                    finder.nzp = (int) dt.Rows[0]["nzp"];

                    if (dt.Rows[0]["dat_close"] != DBNull.Value)
                        finder.dat_close = dt.Rows[0]["dat_close"].ToString();
                }
                else
                {
                    sql =
                        " SELECT c.nzp_serv" +
                        " FROM " + finder.pref + DBManager.sKernelAliasRest + "s_counts c" +
                        " WHERE c.nzp_cnt = " + finder.nzp_serv + "";
                    dt = DBManager.ExecSQLToTable(conn_db, sql);
                    if (dt.Rows.Count < 1)
                    {
                        ret = new Returns(false, "Невозможно определить услугу ПУ");
                        return list;
                    }

                    if (dt.Rows[0]["nzp_serv"] == DBNull.Value)
                    {
                        ret = new Returns(false, "Невозможно определить услугу ПУ, для которого осуществляется поиск");
                        return list;
                    }
                    finder.nzp_serv = (int)dt.Rows[0]["nzp_serv"];
                    
                }

                #endregion

                sql = "DROP TABLE " + temp_table;
                DBManager.ExecSQL(conn_db, sql, false);

                sql = 
                    " SELECT cs.nzp_counter, cs.num_cnt, cs.dat_close, COALESCE(cr.nzp_counter_old, cs.nzp_counter) as nzp_counter_old" +
                    " INTO " + temp_table + 
                    " FROM " + finder.pref + DBManager.sDataAliasRest + "counters_spis cs " +
                    " LEFT OUTER JOIN " + finder.pref + DBManager.sDataAliasRest + "counters_replaced cr" +
                    " ON cr.nzp_counter_new = cs.nzp_counter AND cr.is_actual " +
                    " WHERE cs.dat_close is not null AND cs.nzp = " + finder.nzp +
                    (!string.IsNullOrEmpty(finder.dat_close) ? 
                    " AND cs.dat_close < '" + finder.dat_close.ToDateTime().ToShortDateString() + "'" : "") +
                    " AND cs.nzp_serv =" + finder.nzp_serv + 
                    " ORDER BY nzp_counter_old";
                ret = DBManager.ExecSQL(conn_db, sql, true);
                if (!ret.result) return list;

                sql =
                    " SELECT t.nzp_counter, t.num_cnt, t.dat_close, t.nzp_counter_old" +
                    " FROM " + temp_table + " t" +
                    " WHERE t.dat_close =" +
                    " (SELECT MAX(tt.dat_close) FROM " + temp_table + " tt" +
                    "  WHERE tt.nzp_counter_old = t.nzp_counter_old)" +
                    " ORDER BY 2";
                dt = DBManager.ExecSQLToTable(conn_db, sql);
                foreach (DataRow r in dt.Rows)
                {
                    var cr = new ReplacedCounter();
                    if (r["nzp_counter"] != DBNull.Value)
                        cr.nzp_counter_replaced = (int)r["nzp_counter"];
                    if (r["nzp_counter_old"] != DBNull.Value)
                        cr.nzp_counter_old = (int)r["nzp_counter_old"];
                    if (r["num_cnt"] != DBNull.Value)
                        cr.pu_replaced_info += "ПУ № " + r["num_cnt"];
                    if (r["dat_close"] != DBNull.Value)
                        cr.pu_replaced_info += " закрыт " + r["dat_close"].ToDateTime().ToShortDateString();
                    list.Add(cr);
                }


                sql = "DROP TABLE " + temp_table;
                DBManager.ExecSQL(conn_db, sql, true);

            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = "";
                ret.tag = -1;

                MonitorLog.WriteLog("Ошибка получения списка закрытых ПУ, которые могут быть заменены текущим ПУ " +
                    System.Reflection.MethodBase.GetCurrentMethod().Name + "\n  " +
                    ex.Message + " " + ex.StackTrace, MonitorLog.typelog.Error, 20, 201, true);
            }
            finally
            {
                conn_db.Close();
            }
            return list;
        }

        /// <summary> Загрузка таблицы counters_ord из основного банка в БД портала
        /// </summary>
        public Returns FindCountersOrd(CounterOrd finder)
        {
            Returns ret = Utils.InitReturns();

            #region Проверка входных параметров
            if (finder.nzp_user < 1)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Не определен пользователь";
                return ret;
            }

            if (finder.pref == "")
            {
                ret.result = false;
                ret.tag = -2;
                ret.text = "Не задан префикс базы данных";
                return ret;
            }

            if (finder.pkod == "")
            {
                ret.result = false;
                ret.tag = -3;
                ret.text = "Не задан платежный код";
                return ret;
            }

            if (finder.year_ < 1)
            {
                ret.result = false;
                ret.tag = -4;
                ret.text = "Не задан расчетный год";
                return ret;
            }

            if (finder.month_ < 1)
            {
                ret.result = false;
                ret.tag = -5;
                ret.text = "Не задан расчетный месяц";
                return ret;
            }
            #endregion
            DbSprav db = new DbSprav();
            string dbPortal = db.GetDbPortal(out ret);
            db.Close();
            if (!ret.result) return ret;

            string connectionString = Points.GetConnByPref(finder.pref);
            IDbConnection conn_db = GetConnection(connectionString);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return ret;

            string counters_ord_portal = dbPortal + "counters_ord";
            string counters_ord_master = finder.pref + "_charge_" + (finder.year_ % 100).ToString("00") + DBManager.tableDelimiter + "counters_ord";
            string kvar = finder.pref + "_data" + DBManager.tableDelimiter + "kvar";
            string services = finder.pref + "_kernel" + DBManager.tableDelimiter + "services";
            string dat_month = "'" + new DateTime(finder.year_, finder.month_, 1).ToShortDateString() + "'";

            StringBuilder sql = new StringBuilder();
            sql.Append("Insert into " + counters_ord_portal + " (num_ls, pkod, dat_month, num_cnt, nzp_serv, service, cnt_stage, nzp_cnttype, formula, prev_dat, prev_val, cur_dat, order_num, is_prov, dat_prov, dat_provnext)");
            sql.Append(" Select distinct c.num_ls, c.pkod, c.dat_month, c.num_cnt, c.nzp_serv, s.service, c.cnt_stage, c.nzp_cnttype, c.formula, c.dat_uchet, c.val_cnt, c.dat_uchet + 1 units month, c.order_num, c.is_prov, c.dat_prov, c.dat_provnext");
            sql.Append(" From " + counters_ord_master + " c left outer join " + services + " s");
            sql.Append(" Where c.dat_month = " + dat_month);
            sql.Append(" and c.pkod = " + finder.pkod);
            sql.Append(" and c.nzp_serv = s.nzp_serv");
            sql.Append(" and (select count(*) from " + counters_ord_portal + " cp where c.num_ls = cp.num_ls and c.dat_month = cp.dat_month and c.nzp_serv = cp.nzp_serv and c.num_cnt = cp.num_cnt) = 0 ");
            ret = ExecSQL(conn_db, sql.ToString(), true);

            conn_db.Close();

            return ret;
        }
        
        /// <summary> Получить сведения о показаниях ПУ из портальной БД
        /// </summary>
        public List<CounterOrd> GetCountersOrd(CounterOrd finder, out Returns ret)
        {
            ret = Utils.InitReturns();

            #region Проверка входных параметров
            if (finder.nzp_user < 1)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Не определен пользователь";
                return null;
            }

            if (finder.pref == "")
            {
                ret.result = false;
                ret.tag = -2;
                ret.text = "Не задан префикс базы данных";
                return null;
            }

            if (finder.pkod == "")
            {
                ret.result = false;
                ret.tag = -3;
                ret.text = "Не задан платежный код";
                return null;
            }

            if (finder.year_ < 1)
            {
                ret.result = false;
                ret.tag = -4;
                ret.text = "Не задан расчетный год";
                return null;
            }

            if (finder.month_ < 1)
            {
                ret.result = false;
                ret.tag = -5;
                ret.text = "Не задан расчетный месяц";
                return null;
            }
            #endregion
            DbSprav db = new DbSprav();
            string dbPortal = db.GetDbPortal(out ret);
            db.Close();
            if (!ret.result) return null;

            string connectionString = Points.GetConnByPref(finder.pref);
            IDbConnection conn_db = GetConnection(connectionString);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return null;

            string counters_ord = dbPortal + "counters_ord c";

            string dat_month = "'" + new DateTime(finder.year_, finder.month_, 1).ToShortDateString() + "'";
            string where = " Where c.pkod = " + finder.pkod;
            where += " and c.dat_month = " + dat_month;

            int total_record_count = 0;
            object count = ExecScalar(conn_db, " Select count(*) From " + counters_ord + where, out ret, true);
            if (ret.result)
            {
                try
                {
                    total_record_count = Convert.ToInt32(count);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = ex.Message;
                    conn_db.Close();
                    return null;
                }
            }

            IDataReader reader;

            string sql = "Select nzp_ck, num_ls, pkod, dat_month, num_cnt, nzp_serv, service, cnt_stage, nzp_cnttype, formula, prev_dat, prev_val, cur_dat, cur_val, order_num, " +
                "is_prov, dat_prov, dat_provnext, dat_vvod, nzp_pack_ls, dat_load, dat_when, nzp_account, pref, ist, ist_name From " + counters_ord + where +
                " Order by c.service, c.order_num";

            ret = ExecRead(conn_db, out reader, sql, true);
            if (!ret.result)
            {
                conn_db.Close();
                return null;
            }

            List<CounterOrd> Spis = new List<CounterOrd>();

            try
            {
                while (reader.Read())
                {
                    CounterOrd zap = new CounterOrd();

                    if (reader["nzp_ck"] != DBNull.Value) zap.nzp_ck = Convert.ToInt32(reader["nzp_ck"]);
                    if (reader["num_ls"] != DBNull.Value) zap.num_ls = Convert.ToInt32(reader["num_ls"]);
                    if (reader["pkod"] != DBNull.Value) zap.pkod = Convert.ToString(reader["pkod"]);
                    if (reader["dat_month"] != DBNull.Value) zap.dat_month = String.Format("{0:dd.MM.yyyy}", reader["dat_month"]);
                    if (reader["num_cnt"] != DBNull.Value) zap.num_cnt = Convert.ToString(reader["num_cnt"]);
                    if (reader["nzp_serv"] != DBNull.Value) zap.nzp_serv = Convert.ToInt32(reader["nzp_serv"]);
                    if (reader["service"] != DBNull.Value) zap.service = Convert.ToString(reader["service"]);
                    if (reader["cnt_stage"] != DBNull.Value) zap.cnt_stage = Convert.ToInt32(reader["cnt_stage"]);
                    if (reader["nzp_cnttype"] != DBNull.Value) zap.nzp_cnttype = Convert.ToInt32(reader["nzp_cnttype"]);
                    if (reader["formula"] != DBNull.Value) zap.formula = Convert.ToString(reader["formula"]);
                    if (reader["prev_dat"] != DBNull.Value) zap.dat_uchet_pred = String.Format("{0:dd.MM.yyyy}", reader["prev_dat"]);
                    if (reader["prev_val"] != DBNull.Value) zap.val_cnt_pred = Convert.ToDecimal(reader["prev_val"]);
                    if (reader["cur_dat"] != DBNull.Value) zap.dat_uchet = String.Format("{0:dd.MM.yyyy}", reader["cur_dat"]);
                    if (reader["cur_val"] != DBNull.Value) zap.val_cnt = Convert.ToDecimal(reader["cur_val"]);
                    if (reader["order_num"] != DBNull.Value) zap.order_num = Convert.ToInt32(reader["order_num"]);
                    if (reader["is_prov"] != DBNull.Value) zap.is_prov = Convert.ToInt32(reader["is_prov"]);
                    if (reader["dat_prov"] != DBNull.Value) zap.dat_prov = String.Format("{0:dd.MM.yyyy}", reader["dat_prov"]);
                    if (reader["dat_provnext"] != DBNull.Value) zap.dat_provnext = String.Format("{0:dd.MM.yyyy}", reader["dat_provnext"]);
                    if (reader["dat_vvod"] != DBNull.Value) zap.dat_vvod = String.Format("{0:dd.MM.yyyy}", reader["dat_vvod"]);
                    if (reader["nzp_pack_ls"] != DBNull.Value) zap.nzp_pack_ls = Convert.ToInt32(reader["nzp_pack_ls"]);
                    if (reader["dat_load"] != DBNull.Value) zap.dat_load = String.Format("{0:dd.MM.yyyy}", reader["dat_load"]);
                    if (reader["dat_when"] != DBNull.Value) zap.dat_when = String.Format("{0:dd.MM.yyyy}", reader["dat_when"]);
                    if (reader["nzp_account"] != DBNull.Value) zap.nzp_account = Convert.ToInt32(reader["nzp_account"]);
                    if (reader["pref"] != DBNull.Value) zap.pref = Convert.ToString(reader["pref"]);
                    if (reader["ist"] != DBNull.Value) zap.ist = Convert.ToInt32(reader["ist"]);
                    if (reader["ist_name"] != DBNull.Value) zap.ist_name = Convert.ToString(reader["ist_name"]);

                    Spis.Add(zap);
                }

                reader.Close();
                conn_db.Close();
                ret.tag = total_record_count;
                return Spis;
            }
            catch (Exception ex)
            {
                reader.Close();
                conn_db.Close();

                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;

                MonitorLog.WriteLog("Ошибка заполнения списка показаний " + err, MonitorLog.typelog.Error, 20, 201, true);

                return null;
            }
        }

        /// <summary> Сохранить в портальной БД введенные показания ПУ
        /// </summary>
        public Returns SaveCountersOrd(List<CounterOrd> list)
        {
            Returns ret = Utils.InitReturns();

            if (list.Count == 0)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Список показаний пуст";
                return ret;
            }

            if (list[0].nzp_user < 1)
            {
                ret.result = false;
                ret.tag = -2;
                ret.text = "Не задан пользователь";
                return ret;
            }

            int nzp_user = list[0].nzp_user;
            DbSprav db = new DbSprav();
            string dbPortal = db.GetDbPortal(out ret);
            db.Close();
            if (!ret.result) return ret;

            string connectionString = Points.GetConnByPref(list[0].pref);
            IDbConnection conn_db = GetConnection(connectionString);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return ret;

            string counters_ord = dbPortal + "counters_ord";

            IDbTransaction transaction;
            try { transaction = conn_db.BeginTransaction(); }
            catch { transaction = null; }

            foreach (CounterOrd co in list)
            {
                if (co.val_cnt != Constants._ZERO_)
                {
                    if (co.nzp_ck < 1)
                    {
                        ret.result = false;
                        ret.tag = -3;
                        ret.text = "Ошибка в параметрах";
                        if (transaction != null) transaction.Rollback();
                        conn_db.Close();
                        return ret;
                    }

                    string sql = "Update " + counters_ord + " Set cur_val = " + co.val_cnt.ToString("F6") + ", dat_vvod = " + DBManager.sCurDateTime + ", dat_when = " + DBManager.sCurDateTime + ", nzp_account = " + nzp_user;
                    sql += " Where nzp_ck = " + co.nzp_ck;

                    ret = ExecSQL(conn_db, transaction, sql, true);
                    if (!ret.result)
                    {
                        if (transaction != null) transaction.Rollback();
                        conn_db.Close();
                        return ret;
                    }
                }
            }

            if (transaction != null) transaction.Commit();
            conn_db.Close();
            return ret;
        }

        /// <summary> Добавить запись в counters_arx
        /// </summary>
        /// <param name="counters_arx"></param>
        /// <param name="nzp_counter"></param>
        /// <param name="pole"></param>
        /// <param name="val_old"></param>
        /// <param name="val_new"></param>
        /// <param name="nzp_user"></param>
        /// <param name="dat_calc"></param>
        /// <param name="conn_db"></param>
        /// <param name="transaction"></param>
        /// <returns></returns>
        public Returns InsertIntoCountersArx(CountersArx counterArx, IDbConnection conn_db, IDbTransaction transaction)
        {
            Returns ret = Utils.InitReturns();

            string str = "insert into " + counterArx.pref + "_data" + DBManager.tableDelimiter + "counters_arx (nzp_counter, pole, val_old, val_new, nzp_user, dat_calc, nzp_prm, dat_when) values " +
                "(" + counterArx.nzp_counter + ", " +
                Utils.EStrNull(counterArx.pole) + ", " +
                Utils.EStrNull(counterArx.val_old) + ", " +
                Utils.EStrNull(counterArx.val_new) + ", " +
                counterArx.nzp_user + ", " +
                Utils.EStrNull(counterArx.dat_calc) + ", " +
                counterArx.nzp_prm + ", " +
                DBManager.sCurDateTime + ")";
                
            ret = ExecSQL(conn_db, transaction, str, true);
            return ret;
        }

        /// <summary> Получить информацию об одном ПУ
        /// </summary>
        public Counter LoadCounter(Counter finder, out Returns ret)
        {
            Counter counter = new Counter(); //инициализация результирующего объекта
            ret = Utils.InitReturns(); //результат

            #region проверка входных данных
            if (finder.nzp_user < 1)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Не определен пользователь";
                return null;
            }

            if (finder.pref == "")
            {
                ret.result = false;
                ret.tag = -2;
                ret.text = "Не определен префикс";
                return null;
            }

            if (finder.nzp_counter <= 0)
            {
                ret.result = false;
                ret.tag = -3;
                ret.text = "Ошибка выбора данных";
                return null;
            }
            #endregion

            #region Соединение с БД
            string connectionString = Points.GetConnByPref(finder.pref);
            IDbConnection conn_db = GetConnection(connectionString);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return null;
            #endregion

            #region определение локального пользователя
            int nzpUser = finder.nzp_user;
                       
            /*DbWorkUser db = new DbWorkUser();
            int nzpUser = db.GetLocalUser(conn_db, finder, out ret);
            db.Close();
            if (!ret.result)
            {
                conn_db.Close();
                return null;
            }*/
            #endregion

            string counters_spis = finder.pref + "_data" + DBManager.tableDelimiter + "counters_spis";

#if PG
            string interval = "now() - INTERVAL '" + Constants.users_min + " minutes'";
#else
            string interval = "current year to second - " + Constants.users_min + " units minute";
#endif

            string sql = 
                " select cs.nzp_counter, cs.nzp_serv, cs.nzp_cnt, tu.is_pl, tu.name_uchet, cs.nzp_type, cs.nzp, cs.nzp_cnttype, "+
                "(case when cs.nzp_cnt in (15, 16) then cntdop.name || ' (' || mdop.measure || ')' else cnt.name || " +
                "' (' || m.measure || ')' end) service, " +
                "   ct.name_type, ct.cnt_stage, cs.num_cnt, cs.comment, cs.dat_prov,  cs.dat_provnext," +
                "   cs.dat_oblom, cs.dat_close, cs.dat_poch, cs.dat_block, cs.user_block," +
                " u.comment as user_name_block, (" + interval + ") as cur_dat, crpld.nzp_counter_old" +
                " FROM " + finder.pref + DBManager.sKernelAliasRest + "services s, " +
                finder.pref + DBManager.sKernelAliasRest + "s_counttypes ct" + ", " +
                counters_spis + " cs " +
                " left outer join " + finder.pref + DBManager.sDataAliasRest + "counters_replaced crpld on crpld.nzp_counter_new = cs.nzp_counter and crpld.is_actual AND crpld.nzp_counter_new <> crpld.nzp_counter_old" +
                " left outer join " + finder.pref + "_kernel" + DBManager.tableDelimiter + "s_counts cnt" +
                " left outer join " + finder.pref + "_kernel" + DBManager.tableDelimiter + "s_measure   m  on cnt.nzp_measure = m.nzp_measure " +
                        " on cs.nzp_cnt = cnt.nzp_cnt " +
                " left outer join " + finder.pref + "_kernel" + DBManager.tableDelimiter + "s_countsdop cntdop " +
                " left outer join " + finder.pref + "_kernel.s_measure   mdop  on cntdop.nzp_measure = mdop.nzp_measure  " +
                        "   on cs.nzp_cnt = cntdop.nzp_cnt " +
                " left outer join " + finder.pref + "_kernel" + DBManager.tableDelimiter + "s_typeuchet tu on tu.is_pl = cs.is_pl " +
                " left outer join " + Points.Pref + "_data" + DBManager.tableDelimiter + "users u on cs.user_block = u.nzp_user " +
                " Where s.nzp_serv = cs.nzp_serv and ct.nzp_cnttype = cs.nzp_cnttype and nzp_counter = " + Convert.ToString(finder.nzp_counter);

            if (finder.RolesVal != null)
                foreach (_RolesVal role in finder.RolesVal)
                    if (role.tip == Constants.role_sql && role.kod == Constants.role_sql_serv) sql += " and cs.nzp_serv in (" + role.val + ") ";

            IDataReader reader;
            ret = ExecRead(conn_db, out reader, sql, true);
            if (!ret.result)
            {
                conn_db.Close();
                return null;
            }

            if (reader.Read())
            {
                try
                {
                    DateTime dt_block = DateTime.MinValue;
                    DateTime dt_cur = DateTime.MinValue;
                    int nzp_user = 0;
                    string userNameBlock = "";

                    if (reader["user_block"] != DBNull.Value) nzp_user = (int)reader["user_block"];
                    if (reader["user_name_block"] != DBNull.Value) userNameBlock = ((string)reader["user_name_block"]).Trim();
                    if (reader["dat_block"] != DBNull.Value) dt_block = Convert.ToDateTime(reader["dat_block"]);
                    if (reader["cur_dat"] != DBNull.Value) dt_cur = Convert.ToDateTime(reader["cur_dat"]);

                    if (nzp_user > 0 && dt_block != DateTime.MinValue) //заблокирован прибор учета
                    {
                        if (nzp_user != nzpUser && dt_cur < dt_block) //если заблокирована запись другим пользователем и 20 мин не прошло
                        {
                            counter.block = "Прибор учета заблокирован пользователем " + userNameBlock + " " + dt_block.ToString("dd.MM.yyyy в HH:mm") + ". Редактировать данные запрещено.";
                            counter.is_blocked = 1;
                        }
                    }

                    if (finder.prm == Constants.act_mode_edit.ToString()) //если берут данные на изменение
                    {

                        if (counter.block == "")
                        {
#if PG
                            ret = ExecSQL(conn_db, "update " + counters_spis + " set dat_block = now(), user_block = " + nzpUser + " where nzp_counter = " + finder.nzp_counter.ToString(), true);
#else
							ret = ExecSQL(conn_db, "update " + counters_spis + " set dat_block = current year to second, user_block = " + nzpUser + " where nzp_counter = " + finder.nzp_counter.ToString(), true);
#endif
                            if (!ret.result)
                            {
                                reader.Close();
                                ret.result = false;
                                ret.text = "Ошибка обновления таблицы counters_spis";
                                conn_db.Close();
                                return null;
                            }
                        }
                    }
                    else //если  на просмотр
                    {
                        if (nzp_user == nzpUser)
                        {
                            ret = ExecSQL(conn_db, "update " + counters_spis + " set dat_block = null, user_block = null where nzp_counter = " + finder.nzp_counter.ToString(), true);
                            if (!ret.result)
                            {
                                reader.Close();
                                ret.result = false;
                                ret.text = "Ошибка обновления таблицы counters_spis";
                                conn_db.Close();
                                return null;
                            }
                        }
                    }

                    if (reader["nzp_counter"] != DBNull.Value) counter.nzp_counter = (int)reader["nzp_counter"];
                    if (reader["nzp_serv"] != DBNull.Value) counter.nzp_serv = (int)reader["nzp_serv"];
                    if (reader["nzp_cnt"] != DBNull.Value) counter.nzp_cnt = (int)reader["nzp_cnt"];
                    if (reader["is_pl"] != DBNull.Value) counter.is_pl = (int)reader["is_pl"];
                    if (reader["nzp"] != DBNull.Value) counter.nzp = Convert.ToInt32(reader["nzp"]);
                    if (reader["service"] != DBNull.Value) counter.service = (string)reader["service"];
                    if (reader["name_uchet"] != DBNull.Value) counter.name_uchet = (string)reader["name_uchet"];

                    if (reader["name_type"] != DBNull.Value) counter.name_type = (string)reader["name_type"] + "(разр: " + Convert.ToString(reader["cnt_stage"]) + ")";
                    if (reader["nzp_type"] != DBNull.Value) counter.nzp_type = (int)reader["nzp_type"];
                    if (reader["nzp_cnttype"] != DBNull.Value) counter.nzp_cnttype = (int)reader["nzp_cnttype"];
                    if (reader["num_cnt"] != DBNull.Value) counter.num_cnt = (string)reader["num_cnt"];
                    if (reader["comment"] != DBNull.Value) counter.comment = (string)reader["comment"];
                    counter.cnt_type = CounterKind.GetKindNameById(counter.nzp_type);
                    if (reader["dat_prov"] != DBNull.Value) counter.dat_prov = Convert.ToDateTime(reader["dat_prov"]).ToShortDateString();
                    if (reader["dat_provnext"] != DBNull.Value) counter.dat_provnext = Convert.ToDateTime(reader["dat_provnext"]).ToShortDateString();
                    if (reader["dat_oblom"] != DBNull.Value) counter.dat_oblom = Convert.ToDateTime(reader["dat_oblom"]).ToShortDateString();
                    if (reader["dat_poch"] != DBNull.Value) counter.dat_poch = Convert.ToDateTime(reader["dat_poch"]).ToShortDateString();
                    if (reader["dat_close"] != DBNull.Value) counter.dat_close = Convert.ToDateTime(reader["dat_close"]).ToShortDateString();

                    if (reader["nzp_counter_old"] != DBNull.Value) counter.nzp_counter_old = (int)reader["nzp_counter_old"];

                    if (counter.nzp_counter_old > 0)
                    {
                        sql =
                            " SELECT cs.nzp_counter, cs.num_cnt, cs.dat_close" +
                            " FROM " + finder.pref + DBManager.sDataAliasRest + "counters_spis cs, " +
                            finder.pref + DBManager.sDataAliasRest + "counters_replaced cr" +
                            " where cr.nzp_counter_new = cs.nzp_counter AND cr.nzp_counter_old = " +
                            counter.nzp_counter_old +
                            " AND cr.is_actual AND cs.dat_close < '" +
                            (String.IsNullOrEmpty(counter.dat_close) ? "01.01.3000" : counter.dat_close) + "' " +
                            " ORDER BY dat_close desc, nzp_counter desc limit 1";
                        DataTable dt = DBManager.ExecSQLToTable(conn_db, sql);
                        if (dt.Rows.Count > 0)
                        {
                            if (dt.Rows[0]["nzp_counter"] != DBNull.Value)
                                counter.nzp_counter_replaced = (int) dt.Rows[0]["nzp_counter"];
                            if (dt.Rows[0]["num_cnt"] != DBNull.Value)
                                counter.counter_replaced_info += "ПУ № " + dt.Rows[0]["num_cnt"];
                            if (dt.Rows[0]["dat_close"] != DBNull.Value)
                                counter.counter_replaced_info += " закрыт " + dt.Rows[0]["dat_close"].ToDateTime().ToShortDateString();
                        }
                    }

                    /* if (counter.nzp_type == (int)CounterKinds.Kvar)
                     {
                         sql = " select trim(nvl(u.ulica,''))||' / '||trim(nvl(r.rajon,''))||'   дом '||" +
                               " trim(nvl(ndom,''))||'  корп. '|| trim(nvl(nkor,''))||'  кв. '||trim(nvl(nkvar,''))||'  ком. '||trim(nvl(nkvar_n,'')) as adr from " +
                                      counters_spis + " cs, " +
                                      finder.pref + "_data:kvar k, " +
                                      finder.pref + "_data:dom d, " +
                                      finder.pref + "_data:s_ulica u, outer  " +
                                      finder.pref + "_data:s_rajon r " +
                               " where k.nzp_dom=d.nzp_dom and d.nzp_ul=u.nzp_ul and u.nzp_raj=r.nzp_raj and cs.nzp = k.nzp_kvar and cs.nzp_type = " + (int)CounterKinds.Kvar.ToString() +
                               " and nzp_counter = " + finder.nzp_counter.ToString();
                     }
                     else
                     {
                         sql = " select trim(nvl(u.ulica,''))||' / '||trim(nvl(r.rajon,''))||'   дом '||" + " trim(nvl(ndom,''))||'  корп. '|| trim(nvl(nkor,'')) as adr from " +
                                     counters_spis + " cs, " +                                    
                                     finder.pref + "_data:dom d, " +
                                     finder.pref + "_data:s_ulica u, outer  " +
                                     finder.pref + "_data:s_rajon r " +
                              " where k.nzp_dom=d.nzp_dom and d.nzp_ul=u.nzp_ul and u.nzp_raj=r.nzp_raj and cs.nzp = d.nzp_dom and cs.nzp_type in( " + (int)CounterKinds.Dom.ToString() +
                              "," + (int)CounterKinds.Group.ToString()+") "+
                              " and nzp_counter = " + finder.nzp_counter.ToString();
                     }

                     ret = ExecRead(conn_db, out reader2,sql, true);
                     if (!ret.result)
                     {
                         conn_db.Close();
                         return null;
                     }
                     if (reader2.Read())
                     {
                         try
                         {
                             if (reader2["adr"] != DBNull.Value) counter.adr = (string)reader2["adr"];
                         }
                         catch (Exception ex)
                         {
                             ret.result = false;
                             ret.text = ex.Message;
                             conn_db.Close();
                             return null;
                         }
                     }*/

                    reader.Close();
                }
                catch (Exception ex)
                {
                    reader.Close();
                    ret.result = false;
                    ret.text = ex.Message;
                    conn_db.Close();
                    return null;
                }
            }

            conn_db.Close();
            return counter;
        }

        /// <summary> Проверка возможности сохранения ПУ
        /// </summary>
        private Returns CanSaveCounter(IDbConnection conn_db, IDbTransaction transaction, Counter finder)
        {
            IDataReader reader;
            string sql = "select nzp_counter from " + finder.pref + "_data" + DBManager.tableDelimiter + "counters_spis" +
                    " where nzp_type = " + finder.nzp_type +
                    " and nzp = " + finder.nzp +
                    " and nzp_serv = " + finder.nzp_serv +
                    " and nzp_cnttype = " + finder.nzp_cnttype +
                    " and num_cnt = " + Utils.EStrNull(finder.num_cnt, "") +
                    " and is_gkal = " + finder.is_gkal +
                    " and nzp_counter <> " + finder.nzp_counter;

            Returns ret = ExecRead(conn_db, transaction, out reader, sql, true);

            if (!ret.result) return ret;
            if (reader.Read()) ret = new Returns(false, "Прибор учета с таким видом, услугой, типом, номером уже зарегистрирован по этому адресу", -1);
            reader.Close();
            return ret;
        }

        /// <summary> Сохранить ПУ
        /// </summary>
        /// <param name="newCounter"></param>
        /// <param name="dat_calc">Расчетный месяц</param>
        /// <returns></returns>
        public Returns SaveCounter(Counter newCounter, string dat_calc)
        {
            Returns ret = Utils.InitReturns();
            #region Проверка входных параметров
            if (newCounter.nzp_user < 1)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Не определен пользователь";
                return ret;
            }

            if (newCounter.pref == "")
            {
                ret.result = false;
                ret.tag = -2;
                ret.text = "Не определен префикс";
                return ret;
            }
            #endregion

            #region Подключение к БД
            string connectionString = Points.GetConnByPref(newCounter.pref);
            IDbConnection conn_db = GetConnection(connectionString);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return ret;
            #endregion

            IDbTransaction transaction;
            try { transaction = conn_db.BeginTransaction(); }
            catch { transaction = null; }

            ret = SaveCounter(conn_db, transaction, newCounter, dat_calc);

            if (!ret.result)
            {
                if (transaction != null) transaction.Rollback();
            }
            else
            {
                if (transaction != null) transaction.Commit();
            }
            conn_db.Close();
            return ret;
        }

        /// <summary> Сохранить ПУ
        /// </summary>
        /// <param name="newCounter"></param>
        /// <param name="dat_calc">Расчетный месяц</param>
        /// <returns></returns>
        public Returns SaveCounter(IDbConnection conn_db, IDbTransaction transaction, Counter newCounter, string dat_calc)
        {
            Returns ret = Utils.InitReturns();

            int nzpUser = newCounter.nzp_user;
            
            /*#region определение локального пользователя
            DbWorkUser db = new DbWorkUser();
            int nzpUser = db.GetLocalUser(conn_db, transaction, newCounter, out ret);
            db.Close();
            if (!ret.result)
            {
                return ret;
            }
            #endregion*/

            string counters_spis = newCounter.pref + "_data" + tableDelimiter + "counters_spis";
            string counters = newCounter.pref + "_data" + tableDelimiter + "counters";
            string counters_dom = newCounter.pref + "_data" + tableDelimiter + "counters_dom";
            string counters_domspis = newCounter.pref + "_data" + tableDelimiter + "counters_domspis";
            string counters_group = newCounter.pref + "_data" + tableDelimiter + "counters_group";
            string counters_arx = newCounter.pref + "_data" + tableDelimiter + "counters_arx";
            string s_counts = newCounter.pref + "_kernel" + tableDelimiter + "s_counts";
            string s_countsdop = newCounter.pref + "_kernel" + tableDelimiter + "s_countsdop";
            string counters_replaced = newCounter.pref + "_data" + tableDelimiter + "counters_replaced";
            string sql;

            // тип учета для домовых и групповых ПУ
            string is_pl;
            if (newCounter.is_pl < 0) is_pl = "null";
            else is_pl = newCounter.is_pl.ToString();

            CountersArx counterArx = new CountersArx();

            counterArx.pref = newCounter.pref;
            counterArx.nzp_counter = newCounter.nzp_counter;
            counterArx.nzp_user = newCounter.nzp_counter;
            counterArx.nzp_user = nzpUser;
            counterArx.dat_calc = dat_calc;
            
            #region определить код услуги
            sql = "select nzp_serv, nzp_measure from " + s_counts + " where nzp_cnt = " + newCounter.nzp_cnt;
            sql += " union all";
            sql += " select nzp_serv, nzp_measure from " + s_countsdop + " where nzp_cnt = " + newCounter.nzp_cnt;
            IDataReader reader;
            ret = ExecRead(conn_db, transaction, out reader, sql, true);
            if (!ret.result) { return ret; }
            int nzpServ = 0, isGkal = 0;
            int nzpMeasure = 0;
            if (reader.Read())
            {
                if (reader["nzp_serv"] != DBNull.Value) nzpServ = (int)reader["nzp_serv"];
                if (nzpServ == 15 || nzpServ == 16 || nzpServ == 17) isGkal = 1;
                if (reader["nzp_measure"] != DBNull.Value) nzpMeasure = (int)reader["nzp_measure"];
                //if ((int)reader["nzp_measure"] == 4) // Гигакалории
                //isGkal = 1;*/
            }
            reader.Close();
            if (nzpServ < 1)
            {
                return new Returns(false, "Не удалось определить код услуги");
            }
            #endregion

            newCounter.nzp_serv = nzpServ;
            newCounter.is_gkal = isGkal;

            if (newCounter.nzp_kvar > 0) newCounter.nzp = newCounter.nzp_kvar;
            else if (newCounter.nzp_dom > 0) newCounter.nzp = newCounter.nzp_dom;
            else newCounter.nzp = 0;

            if (newCounter.nzp_counter > 0) //изменение счетчика
            {
                Counter oldCounter = GetCounter(conn_db, transaction, newCounter, out ret, nzpUser);
                if (!ret.result)
                {
                    return ret;
                }
                if (!(oldCounter.nzp_cnt != newCounter.nzp_cnt ||
                    oldCounter.nzp_cnttype != newCounter.nzp_cnttype ||
                    oldCounter.num_cnt != newCounter.num_cnt ||
                    oldCounter.comment != newCounter.comment ||
                    oldCounter.dat_prov != newCounter.dat_prov ||
                    oldCounter.dat_provnext != newCounter.dat_provnext ||
                    oldCounter.dat_oblom != newCounter.dat_oblom ||
                    oldCounter.dat_poch != newCounter.dat_poch ||
                    oldCounter.dat_close != newCounter.dat_close ||
                    oldCounter.is_pl != newCounter.is_pl))
                {
                    ret.result = true;
                    ret.tag = -1;
                    ret.text = "Данные прибора учета не были изменены";
                    return ret;
                }

                newCounter.nzp_type = oldCounter.nzp_type;
                newCounter.nzp = oldCounter.nzp;

                ret = CanSaveCounter(conn_db, transaction, newCounter);
                if (!ret.result) { return ret; }

                if (newCounter.nzp_counter_old > 0 && newCounter.dat_close != oldCounter.dat_close
                    && !string.IsNullOrEmpty(newCounter.dat_close))
                {
                    sql =
                        " SELECT cs.nzp_counter, cs.num_cnt, cs.dat_close" +
                        " FROM " + newCounter.pref + DBManager.sDataAliasRest + "counters_spis cs, " +
                        newCounter.pref + DBManager.sDataAliasRest + "counters_replaced cr" +
                        " where cr.nzp_counter_new = cs.nzp_counter" +
                        " AND cr.nzp_counter_old = " + newCounter.nzp_counter_old +
                        " AND cr.is_actual AND cs.dat_close < '" + newCounter.dat_close + "' " +
                        " ORDER BY dat_close desc, nzp_counter desc limit 1";
                    DataTable dt = DBManager.ExecSQLToTable(conn_db, sql);
                    if (dt.Rows.Count == 0) 
                    { return new Returns(false, "Дата закрытия не согласуется с датой закрытия замещенного ПУ. Изменения не сохранены.", -1);}
                }

                #region Если изменилась услуга
                if (oldCounter.nzp_cnt != newCounter.nzp_cnt)
                {
                    counterArx.pole = "nzp_cnt";
                    counterArx.val_new = newCounter.nzp_cnt.ToString();
                    counterArx.val_old = oldCounter.nzp_cnt.ToString();

                    ret = InsertIntoCountersArx(counterArx, conn_db, transaction);
                    if (!ret.result)
                    {
                        ret.result = false;
                        ret.text = "Ошибка сохранения информации об 'изменении услуги' в таблицу counters_arx";
                        return ret;
                    }

                    counterArx.pole = "nzp_serv";
                    counterArx.val_new = newCounter.nzp_serv.ToString();
                    counterArx.val_old = oldCounter.nzp_serv.ToString();
                    
                    ret = InsertIntoCountersArx(counterArx, conn_db, transaction);
                    if (!ret.result)
                    {
                        ret.result = false;
                        ret.text = "Ошибка сохранения информации об 'изменении услуги' в таблицу counters_arx";
                        return ret;
                    }

                    #region Если изменился признак ГКал
                    counterArx.pole = "is_gkal";
                    counterArx.val_new = newCounter.is_gkal.ToString();
                    counterArx.val_old = oldCounter.is_gkal.ToString();
                    
                    if (oldCounter.is_gkal != isGkal)
                    {
                        ret = InsertIntoCountersArx(counterArx, conn_db, transaction);
                        if (!ret.result)
                        {
                            ret.result = false;
                            ret.text = "Ошибка сохранения информации об 'изменении признака ГКал' в таблицу counters_arx";
                            return ret;
                        }
                    }
                    #endregion
                }
                #endregion

                #region Если изменился тип учета
                if (oldCounter.is_pl != newCounter.is_pl && newCounter.is_pl > -1)
                {
                    counterArx.pole = "is_pl";
                    counterArx.val_new = newCounter.is_pl.ToString();
                    counterArx.val_old = oldCounter.is_pl.ToString();

                    ret = InsertIntoCountersArx(counterArx, conn_db, transaction);
                    if (!ret.result)
                    {
                        ret.result = false;
                        ret.text = "Ошибка сохранения информации об 'изменении типа учета' в таблицу counters_arx";
                        return ret;
                    }

                    switch (newCounter.nzp_type)
                    {
                        case (int)CounterKinds.Dom:
                            sql = "update " + counters_dom + " set is_pl = " + newCounter.is_pl.ToString() + " where nzp_counter = " + newCounter.nzp_counter;
                            break;
                        case (int)CounterKinds.Group:
                        case (int)CounterKinds.Communal:
                            sql = "update " + counters_group + " set is_pl = " + newCounter.is_pl.ToString() + " where nzp_counter = " + newCounter.nzp_counter;
                            break;
                        default: sql = "";
                            break;
                    }

                    if (sql != "")
                    {
                        ret = ExecSQL(conn_db, transaction, sql, true);
                        if (!ret.result)
                        {
                            return ret;
                        }
                    }

                }
                #endregion

                #region Если изменился тип ПУ
                if (oldCounter.nzp_cnttype != newCounter.nzp_cnttype)
                {
                    counterArx.pole = "nzp_cnttype";
                    counterArx.val_new = newCounter.nzp_cnttype.ToString();
                    counterArx.val_old = oldCounter.nzp_cnttype.ToString();

                    ret = InsertIntoCountersArx(counterArx, conn_db, transaction);
                    if (!ret.result)
                    {
                        ret.result = false;
                        ret.text = "Ошибка сохранения информации об 'изменении типа счетчика' в таблицу counters_arx";
                        return ret;
                    }

                    switch (newCounter.nzp_type)
                    {
                        case (int)CounterKinds.Kvar:
                            sql = "update " + counters + " set nzp_cnttype = " + newCounter.nzp_cnttype + " where nzp_counter = " + newCounter.nzp_counter;
                            break;
                        case (int)CounterKinds.Dom:
                            sql = "update " + counters_dom + " set nzp_cnttype = " + newCounter.nzp_cnttype + " where nzp_counter = " + newCounter.nzp_counter;
                            break;
                        case (int)CounterKinds.Group:
                            sql = "update " + counters_domspis + " set nzp_cnttype = " + newCounter.nzp_cnttype + " where nzp_counter = " + newCounter.nzp_counter;
                            break;
                        default:
                            sql = "";
                            break;
                    }

                    if (sql != "")
                    {
                        ret = ExecSQL(conn_db, transaction, sql, true);
                        if (!ret.result)
                        {
                            return ret;
                        }
                    }
                }
                #endregion

                #region Если изменился номер ПУ
                if (oldCounter.num_cnt != newCounter.num_cnt)
                {
                    counterArx.pole = "num_cnt";
                    counterArx.val_new = newCounter.num_cnt;
                    counterArx.val_old = oldCounter.num_cnt;

                    ret = InsertIntoCountersArx(counterArx, conn_db, transaction);
                    if (!ret.result)
                    {
                        ret.result = false;
                        ret.text = "Ошибка сохранения информации об 'изменении номера счетчика' в таблицу counters_arx";
                        return ret;
                    }

                    List<string> sqls = new List<string>();
                    switch (newCounter.nzp_type)
                    {
                        case (int)CounterKinds.Kvar:
                            sqls.Add("update " + counters + " set num_cnt = " + Utils.EStrNull(newCounter.num_cnt) + " where nzp_counter = " + newCounter.nzp_counter);
                            break;
                        case (int)CounterKinds.Dom:
                            sqls.Add("update " + counters_dom + " set num_cnt = " + Utils.EStrNull(newCounter.num_cnt) + " where nzp_counter = " + newCounter.nzp_counter);
                            break;
                        case (int)CounterKinds.Group:
                            //sqls.Add("update " + counters_group + " set num_cnt = " + Utils.EStrNull(newCounter.num_cnt) + " where nzp_counter = " + newCounter.nzp_counter);
                            sqls.Add("update " + counters_domspis + " set num_cnt = " + Utils.EStrNull(newCounter.num_cnt) + " where nzp_counter = " + newCounter.nzp_counter);
                            break;
                    }

                    foreach (string s in sqls)
                    {
                        ret = ExecSQL(conn_db, transaction, s, true);
                        if (!ret.result)
                        {
                            return ret;
                        }
                    }
                }
                #endregion

                #region Если изменился комментарий
                if (oldCounter.comment != newCounter.comment)
                {
                    counterArx.pole = "comment";
                    counterArx.val_new = newCounter.comment;
                    counterArx.val_old = oldCounter.comment;

                    ret = InsertIntoCountersArx(counterArx, conn_db, transaction); 
                    if (!ret.result)
                    {
                        ret.result = false;
                        ret.text = "Ошибка сохранения информации об 'изменении комментария' в таблицу counters_arx";
                        return ret;
                    }
                }
                #endregion

                #region Если изменилась дата поверки
                if (oldCounter.dat_prov != newCounter.dat_prov)
                {
                    counterArx.pole = "dat_prov";
                    counterArx.val_new = newCounter.dat_prov;
                    counterArx.val_old = oldCounter.dat_prov;

                    ret = InsertIntoCountersArx(counterArx, conn_db, transaction); 
                    if (!ret.result)
                    {
                        ret.result = false;
                        ret.text = "Ошибка сохранения информации об 'изменении даты поверки счетчика' в таблицу counters_arx";
                        return ret;
                    }

                    switch (newCounter.nzp_type)
                    {
                        case (int)CounterKinds.Kvar:
                            ret = UpdateDateInCounters(conn_db, transaction, newCounter.nzp_counter, counters, "nzp_cr", "dat_prov", newCounter.dat_prov);
                            break;
                        case (int)CounterKinds.Dom:
                            ret = UpdateDateInCounters(conn_db, transaction, newCounter.nzp_counter, counters_dom, "nzp_crd", "dat_prov", newCounter.dat_prov);
                            break;
                        case (int)CounterKinds.Group:
                            sql = "Update " + counters_domspis + " set dat_prov = " + Utils.EStrNull(newCounter.dat_prov) + " where nzp_counter = " + newCounter.nzp_counter;
                            ret = ExecSQL(conn_db, transaction, sql, true);
                            break;
                    }
                    if (!ret.result)
                    {
                        return ret;
                    }
                }
                #endregion

                #region Если изменилась дата следующей поверки
                if (oldCounter.dat_provnext != newCounter.dat_provnext)
                {
                    counterArx.pole = "dat_provnext";
                    counterArx.val_new = newCounter.dat_provnext;
                    counterArx.val_old = oldCounter.dat_provnext;

                    ret = InsertIntoCountersArx(counterArx, conn_db, transaction);
                    
                    if (!ret.result)
                    {
                        ret.result = false;
                        ret.text = "Ошибка сохранения информации об 'изменении даты следующей поверки счетчика' в таблицу counters_arx";
                        return ret;
                    }

                    switch (newCounter.nzp_type)
                    {
                        case (int)CounterKinds.Kvar:
                            ret = UpdateDateInCounters(conn_db, transaction, newCounter.nzp_counter, counters, "nzp_cr", "dat_provnext", newCounter.dat_provnext);
                            break;
                        case (int)CounterKinds.Dom:
                            ret = UpdateDateInCounters(conn_db, transaction, newCounter.nzp_counter, counters_dom, "nzp_crd", "dat_provnext", newCounter.dat_provnext);
                            break;
                        case (int)CounterKinds.Group:
                            sql = "Update " + counters_domspis + " set dat_provnext = " + Utils.EStrNull(newCounter.dat_provnext) + " where nzp_counter = " + newCounter.nzp_counter;
                            ret = ExecSQL(conn_db, transaction, sql, true);
                            break;
                    }
                    if (!ret.result)
                    {
                        return ret;
                    }
                }
                #endregion

                #region Если изменилась дата поломки
                if (oldCounter.dat_oblom != newCounter.dat_oblom)
                {
                    counterArx.pole = "dat_oblom";
                    counterArx.val_new = newCounter.dat_oblom;
                    counterArx.val_old = oldCounter.dat_oblom;

                    ret = InsertIntoCountersArx(counterArx, conn_db, transaction);
                    if (!ret.result)
                    {
                        ret.result = false;
                        ret.text = "Ошибка сохранения информации об 'изменении даты поломки счетчика' в таблицу counters_arx";
                        return ret;
                    }

                    switch (newCounter.nzp_type)
                    {
                        case (int)CounterKinds.Kvar:
                            ret = UpdateDateInCounters(conn_db, transaction, newCounter.nzp_counter, counters, "nzp_cr", "dat_oblom", newCounter.dat_oblom);
                            break;
                        case (int)CounterKinds.Dom:
                            ret = UpdateDateInCounters(conn_db, transaction, newCounter.nzp_counter, counters_dom, "nzp_crd", "dat_oblom", newCounter.dat_oblom);
                            break;
                        case (int)CounterKinds.Group:
                            sql = "Update " + counters_domspis + " set dat_oblom = " + Utils.EStrNull(newCounter.dat_oblom) + " where nzp_counter = " + newCounter.nzp_counter;
                            ret = ExecSQL(conn_db, transaction, sql, true);
                            break;
                    }
                    if (!ret.result)
                    {
                        return ret;
                    }
                }
                #endregion

                #region Если изменилась дата починки
                if (oldCounter.dat_poch != newCounter.dat_poch)
                {
                    counterArx.pole = "dat_poch";
                    counterArx.val_new = newCounter.dat_poch;
                    counterArx.val_old = oldCounter.dat_poch;

                    ret = InsertIntoCountersArx(counterArx, conn_db, transaction);
                    if (!ret.result)
                    {
                        ret.result = false;
                        ret.text = "Ошибка сохранения информации об 'изменении даты починки счетчика' в таблицу counters_arx";
                        return ret;
                    }

                    switch (newCounter.nzp_type)
                    {
                        case (int)CounterKinds.Kvar:
                            ret = UpdateDateInCounters(conn_db, transaction, newCounter.nzp_counter, counters, "nzp_cr", "dat_poch", newCounter.dat_poch);
                            break;
                        case (int)CounterKinds.Dom:
                            ret = UpdateDateInCounters(conn_db, transaction, newCounter.nzp_counter, counters_dom, "nzp_crd", "dat_poch", newCounter.dat_poch);
                            break;
                        case (int)CounterKinds.Group:
                            sql = "Update " + counters_domspis + " set dat_poch = " + Utils.EStrNull(newCounter.dat_poch) + " where nzp_counter = " + newCounter.nzp_counter;
                            ret = ExecSQL(conn_db, transaction, sql, true);
                            break;
                    }
                    if (!ret.result)
                    {
                        return ret;
                    }
                }
                #endregion

                #region Если изменилась дата закрытия
                if (oldCounter.dat_close != newCounter.dat_close)
                {
                    counterArx.pole = "dat_close";
                    counterArx.val_new = newCounter.dat_close;
                    counterArx.val_old = oldCounter.dat_close;

                    ret = InsertIntoCountersArx(counterArx, conn_db, transaction);
                    if (!ret.result)
                    {
                        ret.result = false;
                        ret.text = "Ошибка сохранения информации об 'изменении даты закрытия счетчика' в таблицу counters_arx";
                        return ret;
                    }

                    switch (newCounter.nzp_type)
                    {
                        case (int)CounterKinds.Kvar:
                            ret = UpdateDateInCounters(conn_db, transaction, newCounter.nzp_counter, counters, "nzp_cr", "dat_close", newCounter.dat_close);
                            break;
                        case (int)CounterKinds.Dom:
                            ret = UpdateDateInCounters(conn_db, transaction, newCounter.nzp_counter, counters_dom, "nzp_crd", "dat_close", newCounter.dat_close);
                            break;
                        case (int)CounterKinds.Group:
                            sql = "Update " + counters_domspis + " set dat_close = " + Utils.EStrNull(newCounter.dat_close) + " where nzp_counter = " + newCounter.nzp_counter;
                            ret = ExecSQL(conn_db, transaction, sql, true);
                            break;
                    }
                    if (!ret.result)
                    {
                        return ret;
                    }
                }
                #endregion

                #region Если изменился заменный ПУ

                if (oldCounter.nzp_counter_old != newCounter.nzp_counter_old)
                {
                    if (newCounter.nzp_counter_old == -1)
                    {
                        sql = 
                            " UPDATE " + counters_replaced +
                            " SET is_actual  = false, changed_on = now(), changed_by = " + newCounter.nzp_user +
                            " WHERE nzp_counter_new = " + newCounter.nzp_counter;
                        ret = ExecSQL(conn_db, transaction, sql, true);
                        if (!ret.result)
                        {
                            ret.text = "Изменение в таблице counters_replaced прошло с ошибкой";
                            return ret;
                        }
                        //если осталась только одна запись с таким корневым ПУ и эта запись является корнем, то удаляем
                        sql =
                          " SELECT * FROM " + counters_replaced +
                          " WHERE nzp_counter_old = " + oldCounter.nzp_counter_old + " AND is_actual" +     
                          " LIMIT 1";
                        DataTable dt = DBManager.ExecSQLToTable(conn_db, sql);
                        if (dt.Rows.Count == 1)
                        {
                            sql =
                             " SELECT * FROM " + counters_replaced +
                             " WHERE nzp_counter_old = nzp_counter_new AND nzp_counter_old = " + oldCounter.nzp_counter_old +
                             " AND is_actual" +     
                             " LIMIT 1";
                            DataTable dt1 = DBManager.ExecSQLToTable(conn_db, sql);
                            if (dt1.Rows.Count == 1)
                            {
                                sql =
                                    " UPDATE " + counters_replaced +
                                    " SET is_actual  = false, changed_on = now(), changed_by = " + newCounter.nzp_user +
                                    " WHERE nzp_counter_new = " + oldCounter.nzp_counter_old;
                                ret = ExecSQL(conn_db, transaction, sql, true);
                                if (!ret.result)
                                {
                                    ret.text = "Изменение в таблице counters_replaced прошло с ошибкой";
                                    return ret;
                                }
                            }
                        }
                    }
                    else if (newCounter.nzp_counter_old > 0)
                    {
                        //проверяем, есть ли корень связки
                        sql = 
                            " SELECT * FROM " + counters_replaced +
                            " WHERE nzp_counter_new = " + newCounter.nzp_counter_old + " AND is_actual" +
                            " LIMIT 1";
                        DataTable dt = DBManager.ExecSQLToTable(conn_db, sql);
                        if (dt.Rows.Count == 0)
                        {
                            sql =
                                " INSERT INTO " + counters_replaced +
                                " (nzp_counter_new, nzp_counter_old, is_actual, created_on, created_by)" +
                                " VALUES (" + newCounter.nzp_counter_old + "," + newCounter.nzp_counter_old + "," +
                                " true, now(), " + newCounter.nzp_user + ")";
                            ret = ExecSQL(conn_db, transaction, sql, true);
                            if (!ret.result)
                            {
                                ret.text = "Добавление корня в counters_replaced прошло с ошибкой";
                                return ret;
                            }
                        }

                        //если текущий ПУ является корневым, то делаем корневым тот, которого он закрывает
                        sql =
                            " SELECT * FROM " + counters_replaced +
                            " WHERE nzp_counter_old = " + newCounter.nzp_counter + " AND is_actual" +
                            " LIMIT 1";
                       dt = DBManager.ExecSQLToTable(conn_db, sql);
                        if (dt.Rows.Count > 0)
                        {
                            sql =
                                " UPDATE  " + counters_replaced +
                                " SET is_actual  = false, changed_on = now(), changed_by = " + newCounter.nzp_user +
                                " WHERE nzp_counter_new = nzp_counter_old AND nzp_counter_old = " + newCounter.nzp_counter;
                            ret = ExecSQL(conn_db, transaction, sql, true);
                            if (!ret.result)
                            {
                                ret.text = "Удаление старого корня в counters_replaced прошло с ошибкой";
                                return ret;
                            }

                            sql =
                                " UPDATE " + counters_replaced +
                                " SET nzp_counter_old = " + newCounter.nzp_counter_old + "," +
                                " changed_on = now(), changed_by = " + newCounter.nzp_user +
                                " WHERE nzp_counter_old = " + newCounter.nzp_counter;
                            ret = ExecSQL(conn_db, transaction, sql, true);
                            if (!ret.result)
                            {
                                ret.text = "Изменение старого корня в counters_replaced прошло с ошибкой";
                                return ret;
                            }
                        }

                        sql =
                            " INSERT INTO " + counters_replaced +
                            " (nzp_counter_new, nzp_counter_old, is_actual, created_on, created_by)" +
                            " VALUES (" + newCounter.nzp_counter + "," + newCounter.nzp_counter_old + "," +
                            " true, now(), " + newCounter.nzp_user + ")";
                        ret = ExecSQL(conn_db, transaction, sql, true);
                        if (!ret.result)
                        {
                            ret.text = "Добавление ПУ с nzp_counter = " + newCounter.nzp_counter + " в counters_replaced прошло с ошибкой";
                            return ret;
                        }
                    }
                }
                #endregion

                sql = "update " + counters_spis + " set nzp_cnt = " + newCounter.nzp_cnt + ", is_gkal = " + isGkal + ", nzp_serv = " + nzpServ + ", " +
                    " is_pl = " + is_pl + ", nzp_cnttype = " + newCounter.nzp_cnttype + ", " +
                    " num_cnt = " +  Utils.EStrNull(newCounter.num_cnt.Trim()) + ", comment = " + Utils.EStrNull(newCounter.comment.Trim()) + ", " +
                    " dat_prov = " + Utils.EStrNull(newCounter.dat_prov) + ", dat_provnext = " + Utils.EStrNull(newCounter.dat_provnext) + ", " +
                    " dat_oblom = " + Utils.EStrNull(newCounter.dat_oblom) + ", dat_poch = " + Utils.EStrNull(newCounter.dat_poch) + ", " +
                    " dat_close=" + Utils.EStrNull(newCounter.dat_close) + ", dat_block = null, user_block = null where nzp_counter = " + newCounter.nzp_counter;

                ret = ExecSQL(conn_db, transaction, sql, true);
                if (!ret.result)
                {
                    ret.result = false;
                    ret.text = "Ошибка сохранения в таблицу counters_spis";
                    return ret;
                }

                #region Добавление в sys_events события 'Изменение адреса дома'
                try
                {
                    DbAdmin.InsertSysEvent(new SysEvents()
                    {
                        pref = newCounter.pref,
                        nzp_user = newCounter.nzp_user,
                        nzp_dict = 6496,
                        nzp_obj = newCounter.nzp_counter,
                        note = "ПУ был изменен. Подробнее в истории изменений."
                    }, transaction, conn_db);
                }
                catch (Exception ex)
                {
                    MonitorLog.WriteLog(ex.Message, MonitorLog.typelog.Error, true);
                }
                #endregion
            }
            else //добавление нового ПУ
            {
                ret = CanSaveCounter(conn_db, transaction, newCounter);
                if (!ret.result)
                {
                    return ret;
                }

                if (newCounter.nzp == 0 || newCounter.nzp_type <= 0)
                {
                    ret.result = false;
                    ret.text = "Не задан адрес или тип прибора учета";
                    ret.tag = -1;
                    return ret;
                }

                DbSprav dbSprav = new DbSprav();
                ret = dbSprav.GetNewId(conn_db, transaction, Series.Types.Counter, newCounter.pref);
                if (!ret.result)
                {
                    return ret;
                }

                newCounter.nzp_counter = ret.tag;

                sql = "insert into " + counters_spis + "(nzp_counter, nzp, nzp_type, nzp_serv, nzp_cnt, is_gkal, is_pl, nzp_cnttype, " +
                    " num_cnt, comment, dat_prov, dat_provnext, " +
                    " dat_oblom, dat_poch, dat_close, nzp_user, dat_when) values " +
                    "(" + newCounter.nzp_counter + "," + newCounter.nzp + "," + newCounter.nzp_type + "," + nzpServ + "," + newCounter.nzp_cnt + "," + isGkal + "," + is_pl + ", " + newCounter.nzp_cnttype + ", " +
                    Utils.EStrNull(newCounter.num_cnt.Trim()) + ", " + Utils.EStrNull(newCounter.comment.Trim()) + ", " + Utils.EStrNull(newCounter.dat_prov) + ", " + Utils.EStrNull(newCounter.dat_provnext) + ", " +
                    Utils.EStrNull(newCounter.dat_oblom) + ", " + Utils.EStrNull(newCounter.dat_poch) + ", " + Utils.EStrNull(newCounter.dat_close) + ", " + nzpUser + ", " + DBManager.sCurDateTime + ")"; 

                ret = ExecSQL(conn_db, transaction, sql, true);
                if (!ret.result)
                {
                    ret.result = false;
                    ret.text = "Ошибка сохранения в таблицу counters_spis";
                    return ret;
                }

                if (newCounter.nzp_type == (int)CounterKinds.Group)
                {
                    sql = "insert into " + counters_domspis +
                        "(nzp_counter,nzp_dom,num_cnt, comment,nzp_cnttype,nzp_serv,dat_prov,dat_provnext,is_actual,nzp_user,dat_when,dat_oblom,dat_poch,dat_close) values " +
                        "(" + newCounter.nzp_counter + ", " +
                            newCounter.nzp + ", " +
                            Utils.EStrNull(newCounter.num_cnt) + ", " +
                            Utils.EStrNull(newCounter.comment) + ", " +
                            newCounter.nzp_cnttype + ", " +
                            nzpServ + ", " +
                            Utils.EStrNull(newCounter.dat_prov) + ", " +
                            Utils.EStrNull(newCounter.dat_provnext) + ", " +
                            (int)CounterVal.Ist.Operator + ", " +
                            nzpUser + ", " +
                            DBManager.sCurDateTime + ", " +
                            Utils.EStrNull(newCounter.dat_oblom) + ", " +
                            Utils.EStrNull(newCounter.dat_poch) + ", " +
                            Utils.EStrNull(newCounter.dat_close) + ")";

                    ret = ExecSQL(conn_db, transaction, sql, true);
                    if (!ret.result)
                    {
                        ret.result = false;
                        ret.text = "Ошибка сохранения в таблицу counters_domspis";
                        return ret;
                    }
                }

                ret.tag = Convert.ToInt32(newCounter.nzp_counter);

                #region Добавление в sys_events события 'Добавление ПУ'
                try
                {
                    DbAdmin.InsertSysEvent(new SysEvents()
                    {
                        pref = newCounter.pref,
                        nzp_user = newCounter.nzp_user,
                        nzp_dict = 6497,
                        nzp_obj = newCounter.nzp_counter,
                        note = "Заводской номер " + newCounter.num_cnt
                    }, transaction, conn_db);
                }
                catch (Exception ex)
                {
                    MonitorLog.WriteLog(ex.Message, MonitorLog.typelog.Error, true);
                }
                #endregion
            }

            #region выставить признак перерасчета при закрытии ПУ
            //-------------------------------------------------------------------------
            if (newCounter.nzp_type == (int)CounterKinds.Kvar)
            {
                // получить расчетный месяц банка
                RecordMonth rm = Points.GetCalcMonth(new CalcMonthParams(newCounter.pref));
                DateTime calcMonth = new DateTime(rm.year_, rm.month_, 1);
                DateTime dat_close = new DateTime(rm.year_, rm.month_, 1);

                try
                { dat_close = Convert.ToDateTime(newCounter.dat_close); }
                catch
                { }

                if (dat_close < calcMonth)
                {
                    try
                    {
                        DateTime dat_s = new DateTime(dat_close.Year, dat_close.Month, 1);
                        DateTime dat_po = calcMonth.AddDays(-1);

                        MustCalcTable must = new MustCalcTable();
                        must.NzpKvar = Convert.ToInt32(newCounter.nzp);
                        must.NzpServ = newCounter.nzp_serv;
                        must.NzpSupp = 0;
                        must.Month = calcMonth.Month;
                        must.Year = calcMonth.Year;
                        must.DatS = dat_s;
                        must.DatPo = dat_po;
                        must.Reason = MustCalcReasons.Counter;
                        must.Kod2 = 0;
                        must.NzpUser = nzpUser;
                        must.Comment = "Закрытие прибора учета";

                        DbMustCalcNew dbMustCalc = new DbMustCalcNew(conn_db, transaction);
                        ret = dbMustCalc.InsertReason(newCounter.pref + "_data", must);
                        if (!ret.result) throw new Exception(ret.text);

                        #region вставка признака перерасчета по связанным услугам
                        //-------------------------------------------------------------------------------------------------------------------------------
                        // определение связанных услуг
                        sql = String.Format("SELECT nzp_serv_slave FROM {0}_data{4}dep_servs " +
                            "WHERE nzp_serv = {1} AND is_actual = 1 " +
                            "AND '{2}' >= dat_s AND '{3}' < dat_po and nzp_dep = 1", Points.Pref, newCounter.nzp_serv, dat_s.ToShortDateString(), dat_po.ToShortDateString(), DBManager.tableDelimiter);

                        IntfResultTableType rt = ClassDBUtils.OpenSQL(sql, conn_db, transaction);
                        if (rt.resultCode < 0) throw new Exception(rt.resultMessage);

                        // вставка признка перерасчета по связанным услугам
                        for (int i = 0; i < rt.resultData.Rows.Count; i++)
                        {
                            must.NzpServ = Convert.ToInt32(rt.resultData.Rows[i]["nzp_serv_slave"]);
                            ret = dbMustCalc.InsertReason(newCounter.pref + "_data", must);
                            if (!ret.result) throw new Exception(ret.text);
                        }
                        //-------------------------------------------------------------------------------------------------------------------------------
                        #endregion
                    }
                    catch (Exception ex)
                    {
                        MonitorLog.WriteLog(ex.Message, MonitorLog.typelog.Error, true);
                    }
                }
            }
            //-------------------------------------------------------------------------
            #endregion

            return ret;
        }

        /// <summary> Обновляет поля типа дата в таблицах с показаниями ПУ
        /// </summary>
        /// <param name="conn_db"></param>
        /// <param name="transaction"></param>
        /// <param name="nzp_counter">уникальный код ПУ</param>
        /// <param name="table">наименование таблицы, в которой находится обновляемое поле</param>
        /// <param name="key">ключевое поле таблицы</param>
        /// <param name="field">наименование поля, которое необходимо обновить</param>
        /// <param name="value">значение поля</param>
        /// <returns>результат операции</returns>
        private Returns UpdateDateInCounters(IDbConnection conn_db, IDbTransaction transaction, long nzp_counter, string table, string key, string field, string value)
        {
            string sql;
            if (value == "")
            {
                sql = "update " + table + " set " + field + " = null where is_actual <> 100 and nzp_counter = " + nzp_counter;
                return ExecSQL(conn_db, transaction, sql, true);
            }
            else
            {

                sql = "select " + key + " from " + table + " where nzp_counter = " + nzp_counter + " and is_actual <> 100 order by dat_uchet desc";
                IDataReader reader;

                Returns ret = ExecRead(conn_db, transaction, out reader, sql, true);
                if (!ret.result) return ret;
                if (reader.Read())
                {
                    int keyId;
                    if (reader[key] != DBNull.Value)
                    {
                        keyId = (int)reader[key];

                        sql = "update " + table + " set " + field + " = " + Utils.EStrNull(value) + " where is_actual <> 100 and " + key + " = " + keyId;
                        return ExecSQL(conn_db, transaction, sql, true);
                    }
                }
                return new Returns(true);
            }
        }

        /// <summary> Получить список л/с для группового счетчика
        /// </summary>
        public List<Ls> GetLsGroupCounter(Counter finder, out Returns ret)
        {
            ret = Utils.InitReturns();

            if (finder.nzp_user < 1)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Не определен пользователь";
                return null;
            }

            if (finder.nzp_counter < 0)
            {
                ret.result = false;
                ret.tag = -2;
                ret.text = "Не определен прибор";
                return null;
            }

            if (finder.pref == "")
            {
                ret.result = false;
                ret.tag = -3;
                ret.text = "Не определен префикс";
                return null;
            }
            List<Ls> list = new List<Ls>();
            Ls ls;

            string connectionString = Points.GetConnByPref(finder.pref);
            IDbConnection conn_db = GetConnection(connectionString);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return null;

            //Определить общее количество записей
            string sql = "select count(*) " +
                " from " + finder.pref + "_data" + DBManager.tableDelimiter + "counters_link cl, " +
                Points.Pref + "_data" + DBManager.tableDelimiter + "dom d, " +
                Points.Pref + "_data" + DBManager.tableDelimiter + "kvar k, " +
                Points.Pref + "_data" + DBManager.tableDelimiter + "s_ulica u " +
                " left outer join  " + Points.Pref + "_data" + DBManager.tableDelimiter + "s_rajon r  on  u.nzp_raj=r.nzp_raj " +
                " where  k.nzp_dom = d.nzp_dom and d.nzp_ul = u.nzp_ul " +
                "   and cl.nzp_kvar = k.nzp_kvar and nzp_counter = " + finder.nzp_counter.ToString();

            object count = ExecScalar(conn_db, sql, out ret, true);
            int recordsTotalCount;
            try { recordsTotalCount = Convert.ToInt32(count); }
            catch (Exception e)
            {
                ret = new Returns(false, "Ошибка при определении количества записей: " + (Constants.Debug ? e.Message : ""));
                MonitorLog.WriteLog("Ошибка GetLsGroupCounter " + (Constants.Viewerror ? "\n" + e.Message : ""), MonitorLog.typelog.Error, 20, 201, true);
                conn_db.Close();
                return null;
            }

            sql = " select trim(" + DBManager.sNvlWord + "(u.ulica,''))||' / '||trim(" + DBManager.sNvlWord + "(r.rajon,''))||'   дом '|| " +
                "   trim(" + DBManager.sNvlWord + "(ndom,''))||'  корп. '|| trim(" + DBManager.sNvlWord + "(nkor,''))||'  кв. '||trim(" + DBManager.sNvlWord + "(nkvar,''))||'  ком. '||trim(" + DBManager.sNvlWord + "(nkvar_n,'')) as adr, cl.nzp_kvar " +
                " from " + finder.pref + "_data" + DBManager.tableDelimiter + "counters_link cl, " +
                    Points.Pref + "_data" + DBManager.tableDelimiter + "kvar k, " +
                    Points.Pref + "_data" + DBManager.tableDelimiter + "dom d, " +
                    Points.Pref + "_data" + DBManager.tableDelimiter + "s_ulica u " +
                    " LEFT OUTER JOIN  " + Points.Pref + "_data" + DBManager.tableDelimiter + "s_rajon r ON u.nzp_raj = r.nzp_raj " +
                " where  k.nzp_dom = d.nzp_dom and d.nzp_ul = u.nzp_ul " +
                    " and cl.nzp_kvar = k.nzp_kvar and nzp_counter = " + finder.nzp_counter.ToString() +
                " order by u.ulica,r.rajon,d.ndom,d.nkor,k.ikvar,k.nkvar_n";

            IDataReader reader;
            ret = ExecRead(conn_db, out reader, sql, true);
            if (!ret.result)
            {
                conn_db.Close();
                return null;
            }
            int i = 0;
            while (reader.Read())
            {
                i++;
                if (i <= finder.skip) continue;
                try
                {
                    ls = new Ls();
                    if (reader["adr"] != DBNull.Value) ls.adr = (string)reader["adr"];
                    if (reader["nzp_kvar"] != DBNull.Value) ls.nzp_kvar = (int)reader["nzp_kvar"];
                    list.Add(ls);
                }
                catch (Exception ex)
                {
                    reader.Close();
                    ret.result = false;
                    ret.text = ex.Message;
                    conn_db.Close();
                    return null;
                }
                if (finder.rows > 0 && i >= finder.skip + finder.rows) break;
            }
            ret.tag = recordsTotalCount;
            reader.Close();
            conn_db.Close();
            return list;
        }

        /// <summary> Получить список л/с, доступных для добавления к групповому Пу, за исключением уже имеющихся в ГПУ
        /// </summary>
        public List<Ls> GetLsDomNotGroupCnt(Counter finder, out Returns ret)
        {
            ret = Utils.InitReturns();

            if (finder.nzp_user < 1)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Не определен пользователь";
                return null;
            }

            if (finder.nzp_counter < 0)
            {
                ret.result = false;
                ret.tag = -2;
                ret.text = "Не определен прибор";
                return null;
            }

            if (finder.pref == "")
            {
                ret.result = false;
                ret.tag = -3;
                ret.text = "Не определен префикс";
                return null;
            }

            if (finder.nzp_dom < 0)
            {
                ret.result = false;
                ret.tag = -4;
                ret.text = "Не определен дом";
                return null;
            }

            List<Ls> list = new List<Ls>();
            Ls ls;

            string connectionString = Points.GetConnByPref(finder.pref);
            IDbConnection conn_db = GetConnection(connectionString);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return null;

            //Определить общее количество записей
            string sql = "select count(*) " +
                " from " + Points.Pref + "_data" + DBManager.tableDelimiter + "kvar k, " + Points.Pref + "_data" + DBManager.tableDelimiter + "dom d, " +
                           Points.Pref + "_data" + DBManager.tableDelimiter + "s_ulica u " +
                           " left outer join " + Points.Pref + "_data" + DBManager.tableDelimiter + "s_rajon r ON u.nzp_raj = r.nzp_raj " +
                " where k.nzp_dom=d.nzp_dom and d.nzp_ul=u.nzp_ul and " + 
                    " (select count(*) from " + finder.pref + "_data" + DBManager.tableDelimiter + "counters_link cl where nzp_counter = " + finder.nzp_counter + " and k.nzp_kvar = cl.nzp_kvar) = 0 " +
                    "and k.nzp_dom = " + finder.nzp_dom;

            object count = ExecScalar(conn_db, sql, out ret, true);
            int recordsTotalCount;
            try { recordsTotalCount = Convert.ToInt32(count); }
            catch (Exception e)
            {
                ret = new Returns(false, "Ошибка при определении количества записей: " + (Constants.Debug ? e.Message : ""));
                MonitorLog.WriteLog("Ошибка GetLsDomNotGroupCnt " + (Constants.Viewerror ? "\n" + e.Message : ""), MonitorLog.typelog.Error, 20, 201, true);
                conn_db.Close();
                return null;
            }

            sql = "select k.nzp_kvar, trim(" + DBManager.sNvlWord + "(u.ulica,''))||' / '||trim(" + DBManager.sNvlWord + "(r.rajon,''))||'   дом '|| " +
                "   trim(" + DBManager.sNvlWord + "(ndom,''))||'  корп. '|| trim(" + DBManager.sNvlWord + "(nkor,''))||'  кв. '||trim(" + DBManager.sNvlWord + "(nkvar,''))||'  ком. '||trim(" + DBManager.sNvlWord + "(nkvar_n,'')) as adr " +
                " from " + Points.Pref + "_data" + DBManager.tableDelimiter + "kvar k, " + 
                            Points.Pref + "_data" + DBManager.tableDelimiter + "dom d, " +
                            Points.Pref + "_data" + DBManager.tableDelimiter + "s_ulica u " +
                "   left outer join " + Points.Pref + "_data" + DBManager.tableDelimiter + "s_rajon r ON u.nzp_raj = r.nzp_raj " +
                " where k.nzp_dom = d.nzp_dom and d.nzp_ul = u.nzp_ul " +
                "   and (select count(*) from " + finder.pref + "_data" + DBManager.tableDelimiter + "counters_link cl where cl.nzp_kvar = k.nzp_kvar and nzp_counter = " + finder.nzp_counter + ") = 0 " +
                "   and k.nzp_dom = " + finder.nzp_dom +
                " order by k.ikvar,k.nkvar_n";

            IDataReader reader;
            ret = ExecRead(conn_db, out reader, sql, true);
            if (!ret.result)
            {
                conn_db.Close();
                return null;
            }
            int i = 0;
            while (reader.Read())
            {
                i++;
                if (i <= finder.skip) continue;
                try
                {

                    ls = new Ls();
                    if (reader["adr"] != DBNull.Value) ls.adr = (string)reader["adr"];
                    if (reader["nzp_kvar"] != DBNull.Value) ls.nzp_kvar = (int)reader["nzp_kvar"];
                    list.Add(ls);
                }
                catch (Exception ex)
                {
                    reader.Close();
                    ret.result = false;
                    ret.text = ex.Message;
                    conn_db.Close();
                    return null;
                }

                if (finder.rows > 0 && i >= finder.skip + finder.rows) break;
            }
            ret.tag = recordsTotalCount;
            reader.Close();
            conn_db.Close();
            return list;
        }

        /// <summary> Добавить л/с к групповому ПУ
        /// </summary>
        public Returns AddLsForGroupCnt(Counter finder, List<int> list_nzp_kvar, string dat_calc)
        {
            Returns ret = Utils.InitReturns();

            if (finder.nzp_user < 1)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Не определен пользователь";
                return ret;
            }

            if (finder.nzp_counter < 0)
            {
                ret.result = false;
                ret.tag = -2;
                ret.text = "Не определен прибор";
                return ret;
            }

            if (finder.pref == "")
            {
                ret.result = false;
                ret.tag = -3;
                ret.text = "Не определен префикс";
                return ret;
            }

            if (list_nzp_kvar.Count == 0)
            {
                ret.result = false;
                ret.tag = -4;
                ret.text = "Не известны л/с для добавления групповому ПУ";
                return ret;
            }

            string connectionString = Points.GetConnByPref(finder.pref);
            IDbConnection conn_db = GetConnection(connectionString);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return ret;

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

            IDbTransaction transaction;
            try { transaction = conn_db.BeginTransaction(); }
            catch { transaction = null; }
            string sql;

            if (list_nzp_kvar.Count > 0 && finder.nzp_counter > 0)
            {
                Counter oldCounter = GetCounter(conn_db, transaction, finder, out ret, nzpUser);
                if (!ret.result)
                {
                    if (transaction != null) transaction.Rollback();
                    conn_db.Close();
                    return ret;
                }

                if (oldCounter.cnt_ls != list_nzp_kvar.Count)
                {
                    CountersArx counterArx = new CountersArx();

                    counterArx.pref = finder.pref;
                    counterArx.nzp_counter = finder.nzp_counter;
                    counterArx.pole = "cnt_ls";
                    counterArx.val_new = list_nzp_kvar.Count.ToString();
                    counterArx.val_old = oldCounter.cnt_ls.ToString();
                    counterArx.dat_calc = dat_calc;
                    counterArx.nzp_user = nzpUser;

                    ret = InsertIntoCountersArx(counterArx, conn_db, transaction);
                    
                    if (!ret.result)
                    {
                        ret.result = false;
                        ret.text = "Ошибка сохранения информации об 'изменении количества л/с' в таблицу counters_arx";
                        if (transaction != null) transaction.Rollback();
                        conn_db.Close();
                        return ret;
                    }
                    sql = "update " + finder.pref + "_data" + DBManager.tableDelimiter + "counters_spis set cnt_ls=" + list_nzp_kvar.Count.ToString() + " where nzp_counter = " + finder.nzp_counter.ToString();

                    ret = ExecSQL(conn_db, transaction, sql, true);
                    if (!ret.result)
                    {
                        ret.result = false;
                        ret.text = "Ошибка изменения количества л/с группового ПУ";
                        ret.tag = -6;
                        if (transaction != null) transaction.Rollback();
                        conn_db.Close();
                        return ret;
                    }
                }
            }

            for (int i = 0; i < list_nzp_kvar.Count; i++)
            {
                sql = "insert into  " + finder.pref + "_data" + DBManager.tableDelimiter + "counters_link (nzp_counter, nzp_kvar) values (" + finder.nzp_counter.ToString() + "," + list_nzp_kvar[i].ToString() + ")";

                ret = ExecSQL(conn_db, transaction, sql, true);
                if (!ret.result)
                {
                    ret.result = false;
                    ret.text = "Ошибка добавления л/с групповому ПУ";
                    ret.tag = -5;
                    if (transaction != null) transaction.Rollback();
                    conn_db.Close();
                    return ret;
                }
            }

            if (transaction != null) transaction.Commit();
            conn_db.Close();
            return ret;
        }

        /// <summary> Удалить л/с из группового ПУ
        /// </summary>
        /// <param name="finder">объект Counter</param>
        /// <param name="list_nzp_kvar">список nzp_kvar</param>
        /// <returns>результат</returns>
        public Returns DelLsFromGroupCnt(Counter finder, List<int> list_nzp_kvar, string dat_calc)
        {
            Returns ret = Utils.InitReturns();

            if (finder.nzp_user < 1)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Не определен пользователь";
                return ret;
            }

            if (finder.nzp_counter < 0)
            {
                ret.result = false;
                ret.tag = -2;
                ret.text = "Не определен прибор";
                return ret;
            }

            if (finder.pref == "")
            {
                ret.result = false;
                ret.tag = -3;
                ret.text = "Не определен префикс";
                return ret;
            }

            if (list_nzp_kvar.Count == 0)
            {
                ret.result = false;
                ret.tag = -4;
                ret.text = "Не известны л/с для добавления групповому ПУ";
                return ret;
            }

            string connectionString = Points.GetConnByPref(finder.pref);
            IDbConnection conn_db = GetConnection(connectionString);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return ret;

            #region определение локального пользователя
            int nzpUser = finder.nzp_user;
            
            /*
            DbWorkUser db = new DbWorkUser();
            int nzpUser = db.GetLocalUser(conn_db, finder, out ret);
            db.Close();
            if (!ret.result)
            {
                conn_db.Close();
                return ret;
            }*/
            #endregion

            IDbTransaction transaction;
            try { transaction = conn_db.BeginTransaction(); }
            catch { transaction = null; }
            string sql;

            if (list_nzp_kvar.Count > 0 && finder.nzp_counter > 0)
            {
                Counter oldCounter = GetCounter(conn_db, transaction, finder, out ret, nzpUser);
                if (!ret.result)
                {
                    if (transaction != null) transaction.Rollback();
                    conn_db.Close();
                    return ret;
                }

                if (oldCounter.cnt_ls != list_nzp_kvar.Count)
                {
                    CountersArx counterArx = new CountersArx();
                    
                    counterArx.pref = finder.pref;
                    counterArx.nzp_counter = finder.nzp_counter;
                    counterArx.pole = "cnt_ls";
                    counterArx.val_new = list_nzp_kvar.Count.ToString();
                    counterArx.val_old = oldCounter.cnt_ls.ToString();
                    counterArx.dat_calc = dat_calc;
                    counterArx.nzp_user = nzpUser;

                    ret = InsertIntoCountersArx(counterArx, conn_db, transaction);
                    if (!ret.result)
                    {
                        ret.result = false;
                        ret.text = "Ошибка сохранения информации об 'изменении количества л/с' в таблицу counters_arx";
                        if (transaction != null) transaction.Rollback();
                        conn_db.Close();
                        return ret;
                    }
                }
            }
            string nzp_kvars = "";

            for (int i = 0; i < list_nzp_kvar.Count; i++)
            {
                if (i == 0) nzp_kvars += list_nzp_kvar[i].ToString();
                else nzp_kvars += "," + list_nzp_kvar[i].ToString();
            }

            sql = "delete from  " + finder.pref + "_data" + DBManager.tableDelimiter + "counters_link where nzp_counter = " + finder.nzp_counter.ToString() + " and  nzp_kvar in (" + nzp_kvars + ")";

            ret = ExecSQL(conn_db, transaction, sql, true);
            if (!ret.result)
            {
                ret.result = false;
                ret.text = "Ошибка удаления л/с группового ПУ";
                ret.tag = -5;
                conn_db.Close();
                return ret;
            }

            if (transaction != null) transaction.Commit();
            conn_db.Close();
            return ret;
        }

        /// <summary> Получить список типов учета показаний домовых, групповых ПУ
        /// </summary>
        public List<Counter> LoadCntTypeUchet(Counter finder, out Returns ret)
        {
            ret = Utils.InitReturns();

            if (finder.nzp_user < 1)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Не определен пользователь";
                return null;
            }

            if (finder.pref == "")
            {
                ret.result = false;
                ret.tag = -3;
                ret.text = "Не определен префикс";
                return null;
            }

            string connectionString = Points.GetConnByPref(finder.pref);
            IDbConnection conn_db = GetConnection(connectionString);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return null;

            string sql = "select is_pl, name_uchet from " + finder.pref + "_kernel" + DBManager.tableDelimiter + "s_typeuchet order by name_uchet";

            IDataReader reader;
            List<Counter> listCntTypeUchet = new List<Counter>();
            Counter typeuchet;

            ret = ExecRead(conn_db, out reader, sql, true);
            if (!ret.result)
            {
                conn_db.Close();
                return null;
            }

            while (reader.Read())
            {
                typeuchet = new Counter();
                if (reader["name_uchet"] != DBNull.Value) typeuchet.name_uchet = ((string)reader["name_uchet"]).Trim();
                if (reader["is_pl"] != DBNull.Value) typeuchet.is_pl = Convert.ToInt32(reader["is_pl"]);
                listCntTypeUchet.Add(typeuchet);
            }
            reader.Close();
            conn_db.Close();
            return listCntTypeUchet;
        }

        /// <summary> Разблокировать ПУ
        /// </summary>
        /// <param name="finder"></param>
        /// <returns></returns>
        public Returns UnlockCounter(Counter finder)
        {
            #region проверка входных данных
            if (finder.nzp_user < 1) return new Returns(false, "Не определен пользователь", -1);
            if (finder.pref == "") return new Returns(false, "Не определен префикс", -2);
            if (!(finder.nzp_counter > 0 ||
                (finder.nzp_type == (int)CounterKinds.Dom && finder.nzp_dom > 0) ||
                (finder.nzp_type == (int)CounterKinds.Kvar && finder.nzp_kvar > 0) ||
                (finder.nzp_type == (int)CounterKinds.Group && (finder.nzp_kvar > 0 || finder.nzp_dom > 0)) ||
                (finder.nzp_type == (int)CounterKinds.Communal && finder.nzp_dom > 0)
                ))
            {
                return new Returns(false, "Неверные входные параметры", -3);
            }
            #endregion

            #region Соединение с БД
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

            string sql = "";

            string counters_spis = finder.pref + "_data" + DBManager.tableDelimiter + "counters_spis";
            sql = " Update " + counters_spis + " set dat_block = null, user_block = null  Where " + DBManager.sNvlWord + "(user_block, 0) = " + nzpUser;

            if (finder.nzp_counter > 0)
                sql += " and nzp_counter = " + finder.nzp_counter.ToString();
            else
            {
                sql += " and nzp_type = " + finder.nzp_type;
                
                switch (finder.nzp_type)
                {
                    case (int)CounterKinds.Kvar: // показания квартирных ПУ
                        sql += " and nzp = " + finder.nzp_kvar;
                        break;
                    case (int)CounterKinds.Dom: // показания домовых ПУ
                        sql += " and nzp = " + finder.nzp_dom;
                        break;
                    case (int)CounterKinds.Group: // показания групповых ПУ
                    case (int)CounterKinds.Communal: // показания коммунальных ПУ
                        if (finder.nzp_kvar > 0)
                        {
                            sql += " and (select count(*) from " + finder.pref + "_data" + DBManager.tableDelimiter + "counters_link cl where cl.nzp_counter = nzp_counter and cl.nzp_kvar = " + finder.nzp_kvar + ") > 0 ";  
                        }
                        else if (finder.nzp_dom > 0)
                        {
                            sql += " and nzp = " + finder.nzp_dom;
                        }
                        else sql += " 1=0";
                        break;
                    default:
                        sql += " 1=0";
                        break;
                }
            }

            ret = ExecSQL(conn_db, sql);
            if (!ret.result)
            {
                conn_db.Close();
                return ret;
            }

            conn_db.Close();
            return ret;
        }

        /// <summary> Удалить ПУ
        /// </summary>
        public Returns DeleteCounter(Counter finder)
        {
            #region проверка входных данных
            if (finder == null) return new Returns(false, "Неверные входные параметры", -1);
            if (finder.nzp_user < 1) return new Returns(false, "Не определен пользователь", -1);
            if (finder.pref == "") return new Returns(false, "Не определен префикс", -1);
            if (finder.nzp_counter < 1) return new Returns(false, "Неверные входные параметры", -1);
            if (finder.dat_uchet == "") return new Returns(false, "Не задан расчетный месяц", -1);
            DateTime dat = DateTime.MinValue;
            if (!DateTime.TryParse(finder.dat_uchet, out dat)) return new Returns(false, "Неверные входные параметры", -1);
            #endregion

            #region Соединение с БД
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

#if PG
            string counters_spis = finder.pref + "_data.counters_spis";
            string interval = "now() - INTERVAL '" + Constants.users_min + " minutes'";
#else
            string counters_spis = finder.pref + "_data:counters_spis";
            string interval = "current year to second - " + Constants.users_min + " units minute";
#endif

            // проверка блокировки ПУ другим пользователем
            string sql = " SELECT cs.nzp_type, cs.dat_block, cs.user_block, u.comment as user_name_block, (" + interval + ") as cur_dat " +
                " FROM " + finder.pref + "_data" + DBManager.tableDelimiter + "counters_spis cs " +
                "   LEFT OUTER JOIN " + Points.Pref + "_data" + DBManager.tableDelimiter + "users u ON cs.user_block = u.nzp_user " +
                " WHERE cs.nzp_counter = " + finder.nzp_counter;

            IDataReader reader;
            ret = ExecRead(conn_db, out reader, sql, true);
            if (!ret.result)
            {
                conn_db.Close();
                return ret;
            }
            if (reader.Read())
            {
                string bl = "";
                DateTime dt_block = DateTime.MinValue;
                DateTime dt_cur = DateTime.MinValue;
                int user_block = 0;
                string userNameBlock = "";

                if (reader["user_block"] != DBNull.Value) user_block = (int)reader["user_block"]; //пользователь, который заблокирован
                if (reader["user_name_block"] != DBNull.Value) userNameBlock = ((string)reader["user_name_block"]).Trim(); //пользователь, который заблокирован
                if (reader["dat_block"] != DBNull.Value) dt_block = Convert.ToDateTime(reader["dat_block"]);//дата блокировки
                if (reader["cur_dat"] != DBNull.Value) dt_cur = Convert.ToDateTime(reader["cur_dat"]);//текущее время/дата - 20 мин
                if (reader["nzp_type"] != DBNull.Value) finder.nzp_type = Convert.ToInt32(reader["nzp_type"]);//текущее время/дата - 20 мин

                if (user_block > 0 && dt_block != DateTime.MinValue) //заблокирован прибор учета
                    if (user_block != nzpUser && dt_cur < dt_block) //если заблокирована запись другим пользователем и 20 мин не прошло
                        bl = "Прибор учета заблокирован пользователем " + userNameBlock + " " + dt_block.ToString("dd.MM.yyyy в HH:mm") + ". Удалять прибор учета нельзя.";

                if (bl == "") // ПУ не заблокирован, можно удалять
                {
                    reader.Close();

                    #region Проверка наличия утвержденных показаний ПУ
                    sql = "select * from " + finder.pref + "_data" + DBManager.tableDelimiter;

                    switch (finder.nzp_type)
                    {
                        case (int)CounterKinds.Kvar: sql += "counters"; break;
                        case (int)CounterKinds.Dom: sql += "counters_dom"; break;
                        case (int)CounterKinds.Group:
                        case (int)CounterKinds.Communal:
                            sql += "counters_group"; break;
                        default:
                            reader.Close();
                            conn_db.Close();
                            return new Returns(false, "Неизвестный тип прибора учета", -1);
                    }
                    sql += " Where nzp_counter = " + finder.nzp_counter;
                    ret = ExecRead(conn_db, out reader, sql, true);
                    if (!ret.result)
                    {
                        conn_db.Close();
                        return ret;
                    }
                    #endregion
                    if (reader.Read())
                    {
                        ret = new Returns(false, "Прибор учета имеет утвержденные показания", -1);
                    }
                    else
                    {
                        reader.Close();
                        #region Проверка наличия введенных показаний ПУ
#if PG
                        sql = "set search_path to '" + finder.pref + "_charge_" + (dat.Year % 100).ToString("00") + "' ";
                        //пробел после кавычки нужен?
#else
						sql = "database " + finder.pref + "_charge_" + (dat.Year % 100).ToString("00");
#endif
                        ret = ExecSQL(conn_db, sql, true);
                        if (ret.result)
                        {
                            sql = "Select * from " + finder.pref + "_charge_" + (dat.Year % 100).ToString("00") + DBManager.tableDelimiter + "counters_vals where nzp_counter = " + finder.nzp_counter +
                                " and is_new = 1 and is_new is not null";
                            ret = ExecRead(conn_db, out reader, sql, true);
                            if (!ret.result)
                            {
                                conn_db.Close();
                                return ret;
                            }
                            if (reader.Read())
                            {
                                reader.Close();
                                conn_db.Close();
                                return new Returns(false, "Прибор учета имеет показания", -1);
                            }
                            // удаление введенных показаний
                            sql = "delete from " + finder.pref + "_charge_" + (dat.Year % 100).ToString("00") + DBManager.tableDelimiter + "counters_vals where nzp_counter = " + finder.nzp_counter;
                            ret = ExecSQL(conn_db, sql, true);
                            if (!ret.result)
                            {
                                reader.Close();
                                conn_db.Close();
                                return ret;
                            }
                        }
                        #endregion
                        //удаление ПУ
                        ret = ExecSQL(conn_db, sql, false);
                        if (ret.result)
                        {

                            sql = "Delete from " + finder.pref + "_data" + DBManager.tableDelimiter + "counters_spis where nzp_counter = " + finder.nzp_counter;
                            ret = ExecSQL(conn_db, sql, true);
                        }
                    }
                }
                else
                {
                    ret.result = false;
                    ret.text = bl;
                    ret.tag = -1;
                }
            }
            reader.Close();
            conn_db.Close();
            return ret;
        }
                
        
        /// <summary>
        /// Получить список групп по всем локальным банкам
        /// </summary>
        public List<Group> GetAllLocalLsGroup(Group finder, out Returns ret)
        {
            ret = new Returns();
            return null;
        }//GetGroupLs

        public Returns PrepareReportPuData(CounterVal finder, List<Dom> houseList)
        {
            if (Points.IsSmr)
                return PrepareReportPuDataSamara(finder, houseList);
            else
                return PrepareReportPuDataRt(finder, houseList);
        }

        public Returns PrepareReportPuDataSamara(CounterVal finder, List<Dom> houseList)
        {
            #region проверка значений
            //-----------------------------------------------------------------------
            // проверка наличия пользователя
            if (finder.nzp_user < 1) return new Returns(false, "Не определен пользователь", -1); ;

            // проверка даты учета
            if (finder.dat_uchet.Length <= 0) return new Returns(false, "Не задана дата начала учета", -3); ;

            if (finder.dat_uchet_po.Length <= 0) return new Returns(false, "Не задана дата окончания учета", -4); ;

            DateTime dat_uchet;
            DateTime dat_uchet_po;

            try
            {
                dat_uchet = Convert.ToDateTime(finder.dat_uchet);
            }
            catch
            {
                return new Returns(false, "Неверный формат даты начала учета", -5);
            }

            try
            {
                dat_uchet_po = Convert.ToDateTime(finder.dat_uchet_po);
            }
            catch
            {
                return new Returns(false, "Неверный формат даты окончания учета", -6);
            }

            // определить список префиксов и кодов домов по улице и дому
            Returns ret = new Returns();
            //-----------------------------------------------------------------------
            #endregion

            string where_serv = GetWhereServ(finder, houseList);
            string where_kvar_ = GetWhereKvar(finder, houseList);
            
            string sql = "";

            DateTime dt_pred = dat_uchet;
            DateTime dt_pred_pred = dat_uchet;

            string conn_kernel = Points.GetConnByPref(Points.Pref);
            IDbConnection conn_db = GetConnection(conn_kernel);

            ret = OpenDb(conn_db, true);
            if (!ret.result)
            {
                return new Returns(false, "Не удалось установить подключение", -8);
            }

            finder.nzp_dom = -1;
            List<string> prefList = GetPrefList(finder, houseList, conn_db);
            if (!ret.result) return ret;

            if (prefList == null || prefList.Count == 0) return new Returns(false, "Не удалось определить банки данных", -7);
            

            DataTable table = new DataTable();
            table.TableName = "Q_master";

            table.Columns.Add("nzp_ul", typeof(Int64));
            table.Columns.Add("nzp_dom", typeof(Int64));
            table.Columns.Add("geu", typeof(string));
            table.Columns.Add("ulica", typeof(string));
            table.Columns.Add("ndom", typeof(string));
            table.Columns.Add("nkor", typeof(string));
            table.Columns.Add("num_ls", typeof(string));
            table.Columns.Add("nkvar", typeof(string));
            table.Columns.Add("nkvar_n", typeof(string));
            table.Columns.Add("num_cnt", typeof(string));
            table.Columns.Add("name_type", typeof(string));
            table.Columns.Add("cnt_stage", typeof(string));
            table.Columns.Add("mmnog", typeof(string));
            table.Columns.Add("service", typeof(string));
            table.Columns.Add("measure", typeof(string));

            table.Columns.Add("dat_uchet", typeof(string));
            table.Columns.Add("val_cnt", typeof(string));

            table.Columns.Add("dat_uchet_pred", typeof(string));
            table.Columns.Add("val_cnt_pred", typeof(string));

            table.Columns.Add("rashod", typeof(decimal));

            table.Columns.Add("sred_rashod", typeof(decimal));
            table.Columns.Add("normativ", typeof(decimal));

            table.Columns.Add("fio", typeof(string));
            table.Columns.Add("ngp_cnt", typeof(decimal));


            IDataReader reader = null;

            int year_begin = dat_uchet.Year;
            int year_end = dat_uchet_po.Year;
            int month_begin = dat_uchet.Month;
            int month_end = dat_uchet_po.Month;

            CounterVal cv = new CounterVal();

            ExecSQL(conn_db, "Drop table tmp_counters", false);

            try
            {
                ret = ExecSQL(conn_db, " Create temp table tmp_counters (" +
                   " pref        char(10), " +
                   " geu         char(60), " +
                   " ulica       char(40), " +
                   " idom        integer, " +
                   " ndom        char(15), " +
                   " nkor        char(15), " +
                   " ikvar       integer, " +
                   " nkvar       char(10), " +
                   " nkvar_n     char(10), " +
                   " nzp_ul      integer, " +
                   " nzp_dom     integer, " +
                   " nzp_counter integer, " +
                   " num_ls      char(20), " +
                   " fio         char(40), " +
                   " num_cnt     char(20), " +
                   " name_type   char(40), " +
                   " cnt_stage   integer, " +
                   " mmnog       numeric(14,7), " +
                   " measure     char(20), " +
                   " service     char(100)) " + DBManager.sUnlogTempTable, true);

                if (!ret.result) throw new Exception("Не удалось выполнить запрос");

                foreach (string cur_pref in prefList)
                {
                    // группа
                    #region сформировать sql
                    //----------------------------------------------------------------------------
                    sql = " Insert into tmp_counters (pref, geu, ulica, idom, ndom, nkor, ikvar, nkvar, nkvar_n, nzp_ul, nzp_dom, nzp_counter, " +
                          " num_ls, fio, num_cnt, name_type, cnt_stage, mmnog, measure, service) " +
                        " Select distinct " + Utils.EStrNull(cur_pref) + ", g.geu, u.ulica, d.idom, d.ndom, d.nkor, k.ikvar, k.nkvar, k.nkvar_n, u.nzp_ul, d.nzp_dom, cs.nzp_counter, " +
                            "substr(pkod, 6, 5) || ' ' || case when substr(pkod, 11, 1) = '0' then '' else substr(pkod, 11, 1) end as num_ls" +
                            ", k.fio, cs.num_cnt, t.name_type, t.cnt_stage, t.mmnog, m.measure, s.service " +
                            (Points.IsIpuHasNgpCnt ? ", a.ngp_cnt" : "") +
                        " From " +
                            cur_pref + "_data" + DBManager.tableDelimiter + "counters_spis cs, " +
                            cur_pref + "_data" + DBManager.tableDelimiter + "kvar k, " +
                            cur_pref + "_data" + DBManager.tableDelimiter + "dom d, " +
                            cur_pref + "_data" + DBManager.tableDelimiter + "s_ulica u, " +
                            cur_pref + "_kernel" + DBManager.tableDelimiter + "s_counts cc, " +
                            cur_pref + "_kernel" + DBManager.tableDelimiter + "s_counttypes t, " +
                            cur_pref + "_kernel" + DBManager.tableDelimiter + "s_measure m, " +
                            cur_pref + "_kernel" + DBManager.tableDelimiter + "services s, " +
                            cur_pref + "_data" + DBManager.tableDelimiter + "s_geu g " +
                        " Where cs.nzp = k.nzp_kvar " +
                            " and k.nzp_dom = d.nzp_dom " +
                            " and d.nzp_ul = u.nzp_ul " +
                            " and cs.nzp_cnttype = t.nzp_cnttype " +
                            " and cs.nzp_serv = s.nzp_serv " +
                            " and cs.nzp_serv = cc.nzp_serv " +
                            " and cc.nzp_measure = m.nzp_measure " +
                            " and k.nzp_geu = g.nzp_geu " +
                            " and cs.nzp_type = 3 " + // квартирные ПУ
                        // только открытые лицевые счета
                            " and (select count(*) from " + cur_pref + "_data" + DBManager.tableDelimiter + "prm_3 p3 where p3.nzp_prm = 51 " +
                            " and p3.val_prm in ('1', '2') and current between p3.dat_s and p3.dat_po and p3.nzp = k.nzp_kvar and p3.is_actual <> 100) > 0 " +
                            " and " + where_kvar_ + where_serv;

                    // только новые ПУ
                    if (finder.is_new == 1)
                    {
                        sql += " and (select min(cd.dat_uchet) from " + cur_pref + "_data" + DBManager.tableDelimiter + "counters cd where cd.nzp_counter = cs.nzp_counter " +
                            " and cd.is_actual <> 100) between " + Utils.EStrNull(dat_uchet.ToShortDateString()) + " and " + Utils.EStrNull(dat_uchet.AddMonths(1).ToShortDateString());
                    }
                    //----------------------------------------------------------------------------
                    #endregion

                    //записать текст sql в лог-журнал
                    ret = ExecSQL(conn_db, sql, true);
                    if (!ret.result) throw new Exception("Не удалось выполнить запрос");
                }


                ret = ExecSQL(conn_db, "create index ix_1 on tmp_counters(nzp_counter)", true);
                if (!ret.result) throw new Exception("Не удалось выполнить запрос");

                ret = ExecSQL(conn_db, DBManager.sUpdStat + " tmp_counters", true);
                if (!ret.result) throw new Exception("Не удалось выполнить запрос");

                #region
                //--------------------------------------------------------------------------
                int month_count = month_end + (year_end - year_begin) * 12 - month_begin + 1;

                ExecSQL(conn_db, "drop table tmp_vals", false);
                ret = ExecSQL(conn_db, " Create temp table tmp_vals (" +
                    " month_      date, " +
                    " pref        char(10), " +
                    " nzp_counter integer, " +
                    " val_s       " + DBManager.sDecimalType + "(14,7), " +
                    " val_po      " + DBManager.sDecimalType + "(14,7), " +
                    " dat_s       date, " +
                    " dat_po      date, " +
                    " val         " + DBManager.sDecimalType + "(14,7), " +
                    " stek        integer) " + DBManager.sUnlogTempTable, true);
                if (!ret.result) throw new Exception("Не удалось выполнить запрос");

                for (int j = 0; j < month_count; j++)
                {
                    foreach (string cur_pref in prefList)
                    {
                        sql = " insert into tmp_vals (month_, pref, nzp_counter, val_s, val_po, dat_s, dat_po, val, stek) " +
                            " Select " + Utils.EStrNull(new DateTime(year_begin, month_begin, 1).ToShortDateString()) + ", " + Utils.EStrNull(cur_pref) + ", " +
                            " nzp_counter, val_s, val_po, dat_s, dat_po, val1, stek " +
                            " From " + cur_pref + "_charge_" + (year_begin % 100).ToString("00") + DBManager.tableDelimiter + "counters_" + month_begin.ToString("00") +
                            " Where nzp_counter in (Select nzp_counter from tmp_counters where pref = " + Utils.EStrNull(cur_pref) + ")";
                        ret = ExecSQL(conn_db, sql, true);
                        if (!ret.result) throw new Exception("Не удалось выполнить запрос");

                        // получить ПУ, у которых есть расход 
                        ExecSQL(conn_db, "Drop table tmp_01", false);
                        
                        sql = " create temp table tmp_01 (nzp_counter integer) " + DBManager.sUnlogTempTable;
                        ret = ExecSQL(conn_db, sql, true);
                        if (!ret.result) throw new Exception("Не удалось выполнить запрос");
                        
                        sql = " Insert into tmp_01 " + 
                            " Select nzp_counter from tmp_vals where stek = 1 and pref = " + Utils.EStrNull(cur_pref) +
                            " and month_ = " + Utils.EStrNull(new DateTime(year_begin, month_begin, 1).ToShortDateString());
                        ret = ExecSQL(conn_db, sql, true);
                        if (!ret.result) throw new Exception("Не удалось выполнить запрос");
                        
                        // удалить из временной таблицы показания со средним расходом у тех ПУ, у которых уже есть расход 
                        sql = " Delete from tmp_vals where stek = 2 " + 
                            "   and nzp_counter in (select nzp_counter from tmp_01) and pref = " + Utils.EStrNull(cur_pref) +
                            " and month_ = " + Utils.EStrNull(new DateTime(year_begin, month_begin, 1).ToShortDateString());
                        ret = ExecSQL(conn_db, sql, true);
                        if (!ret.result) throw new Exception("Не удалось выполнить запрос");
                        
                        // получить ПУ, у которых есть cредний расход 
                        ExecSQL(conn_db, "Drop table tmp_01", false);

                        sql = " create temp table tmp_01 (nzp_counter integer) " + DBManager.sUnlogTempTable;
                        ret = ExecSQL(conn_db, sql, true);
                        if (!ret.result) throw new Exception("Не удалось выполнить запрос");

                        sql = " Insert into tmp_01 " +
                            " Select nzp_counter from tmp_vals where stek = 2 and pref = " + Utils.EStrNull(cur_pref) +
                            " and month_ = " + Utils.EStrNull(new DateTime(year_begin, month_begin, 1).ToShortDateString());
                        ret = ExecSQL(conn_db, sql, true);
                        if (!ret.result) throw new Exception("Не удалось выполнить запрос");
                        
                        // удалить из временной таблицы показания с нормативом у тех ПУ, у которых есть средний расход 
                        sql = " Delete from tmp_vals where stek = 3 and nzp_counter in (select nzp_counter from tmp_01) and pref = " + Utils.EStrNull(cur_pref) +
                            " and month_ = " + Utils.EStrNull(new DateTime(year_begin, month_begin, 1).ToShortDateString());
                        ret = ExecSQL(conn_db, sql, true);
                        if (!ret.result) throw new Exception("Не удалось выполнить запрос");
                    }
                    month_begin++;
                    if (month_begin > 12)
                    {
                        month_begin = 1;
                        year_begin++;
                    }
                }
                //--------------------------------------------------------------------------
                #endregion

                ret = ExecSQL(conn_db, "create index ix_02 on tmp_vals(nzp_counter)", true);
                if (!ret.result) throw new Exception("Не удалось выполнить запрос");

                ret = ExecSQL(conn_db, DBManager.sUpdStat + " tmp_vals", true);
                if (!ret.result) throw new Exception("Не удалось выполнить запрос");

                sql = "select * from tmp_vals v, tmp_counters c where v.nzp_counter = c.nzp_counter " +
                    " and v.pref = c.pref " +
                    "Order by c.ulica, c.idom, c.ndom, c.nkor, c.ikvar, c.nkvar, c.nkvar_n, c.nzp_counter, v.dat_s, v.stek ";

                //записать текст sql в лог-журнал
                ret = ExecRead(conn_db, out reader, sql, true);
                if (!ret.result)
                {
                    conn_db.Close();
                    prefList.Clear();
                    reader.Close();
                    return new Returns(false, "Не удалось выполнить запрос");
                }

                string nzp_counter;
                int k;
                int stek;

                decimal rashod;
                decimal sred_rashod;
                decimal normativ;
                string val_cnt = "";
                string val_cnt_pred = "";


                while (reader.Read())
                {
                    if (reader["nzp_counter"] != DBNull.Value) nzp_counter = Convert.ToString(reader["nzp_counter"]).Trim();

                    if (reader["nzp_ul"] != DBNull.Value) cv.nzp_ul = Convert.ToInt32(reader["nzp_ul"]);
                    if (reader["nzp_dom"] != DBNull.Value) cv.nzp_dom = Convert.ToInt32(reader["nzp_dom"]);
                    if (reader["geu"] != DBNull.Value) cv.geu = Convert.ToString(reader["geu"]).Trim();
                    if (reader["ulica"] != DBNull.Value) cv.ulica = Convert.ToString(reader["ulica"]).Trim();
                    if (reader["ndom"] != DBNull.Value) cv.ndom = Convert.ToString(reader["ndom"]).Trim();
                    if (reader["nkor"] != DBNull.Value) cv.nkor = Convert.ToString(reader["nkor"]).Trim();
                    if (reader["num_ls"] != DBNull.Value) cv.pkod = Convert.ToString(reader["num_ls"]).Trim();
                    if (reader["nkvar"] != DBNull.Value) cv.nkvar = Convert.ToString(reader["nkvar"]).Trim();
                    if (reader["nkvar_n"] != DBNull.Value) cv.nkvar_n = Convert.ToString(reader["nkvar_n"]).Trim();
                    if (reader["num_cnt"] != DBNull.Value) cv.num_cnt = Convert.ToString(reader["num_cnt"]).Trim();
                    if (reader["name_type"] != DBNull.Value) cv.name_type = Convert.ToString(reader["name_type"]).Trim();
                    if (reader["cnt_stage"] != DBNull.Value) cv.cnt_stage = Convert.ToInt32(reader["cnt_stage"]);
                    if (reader["mmnog"] != DBNull.Value) cv.mmnog = Convert.ToDecimal(reader["mmnog"]);
                    if (reader["service"] != DBNull.Value) cv.service = Convert.ToString(reader["service"]).Trim();
                    if (reader["measure"] != DBNull.Value) cv.measure = Convert.ToString(reader["measure"]).Trim();
                    if (reader["fio"] != DBNull.Value) cv.fio = Convert.ToString(reader["fio"]).Trim();

                    cv.fio = cv.fio.ToLower();
                    k = cv.fio.IndexOf(" ");
                    if (k > 0) cv.fio = cv.fio.Substring(0, k) + " " + cv.fio.Substring(k + 1, 1).ToUpper() + cv.fio.Substring(k + 2);
                    k = cv.fio.LastIndexOf(" ");
                    if (k > 0) cv.fio = cv.fio.Substring(0, k) + " " + cv.fio.Substring(k + 1, 1).ToUpper() + cv.fio.Substring(k + 2);
                    if (cv.fio.Length > 0) cv.fio = cv.fio.Substring(0, 1).ToUpper() + cv.fio.Substring(1);

                    // cпециально для Лениногорска, в котором нет разделителей между инициалами
                    k = cv.fio.LastIndexOf(".");
                    if (k > 0) cv.fio = cv.fio.Substring(0, k - 1) + cv.fio.Substring(k - 1, 1).ToUpper() + cv.fio.Substring(k);

                    if (reader["dat_s"] != DBNull.Value) cv.dat_uchet = Convert.ToDateTime(reader["dat_s"]).ToShortDateString();
                    if (reader["dat_po"] != DBNull.Value) cv.dat_uchet_pred = Convert.ToDateTime(reader["dat_po"]).ToShortDateString();

                    if (reader["val_s"] != DBNull.Value) cv.val_cnt_pred = Convert.ToDecimal(reader["val_s"]);
                    if (reader["val_po"] != DBNull.Value) cv.val_cnt = Convert.ToDecimal(reader["val_po"]);

                    val_cnt_pred = cv.val_cnt_pred.ToString();
                    val_cnt = cv.val_cnt.ToString();

                    cv.rashod = "";
                    cv.sred_rashod = "";
                    cv.normativ = "";

                    stek = 0;
                    if (reader["stek"] != DBNull.Value) stek = Convert.ToInt32(reader["stek"]);

                    rashod = 0;
                    sred_rashod = 0;
                    normativ = 0;

                    switch (stek)
                    {
                        case 1:
                            cv.rashod = Convert.ToString(reader["val"]);
                            rashod = Convert.ToDecimal(cv.rashod);
                            break;
                        case 2:
                            cv.sred_rashod = Convert.ToString(reader["val"]);
                            sred_rashod = Convert.ToDecimal(cv.sred_rashod);

                            val_cnt_pred = "";
                            val_cnt = "";

                            break;
                        case 3:
                            cv.normativ = Convert.ToString(reader["val"]);
                            normativ = Convert.ToDecimal(cv.normativ);

                            val_cnt_pred = "";
                            val_cnt = "";

                            break;
                    }

                    //if (Points.IsIpuHasNgpCnt)
                    //{
                    //    if (reader["ngp_cnt"] != DBNull.Value) cv.ngp_cnt = Convert.ToDecimal(reader["ngp_cnt"]);
                    //}

                    table.Rows.Add(
                        cv.nzp_ul,
                        cv.nzp_dom,
                        cv.geu,
                        cv.ulica,
                        cv.ndom,
                        cv.nkor,
                        cv.pkod,
                        cv.nkvar,
                        cv.nkvar_n,
                        cv.num_cnt,
                        cv.name_type,
                        cv.cnt_stage,
                        cv.mmnog.ToString().Replace(".0000000", ""),
                        cv.service,
                        cv.measure,
                        cv.dat_uchet,
                        val_cnt,
                        cv.dat_uchet_pred,
                        val_cnt_pred,
                        rashod,
                        sred_rashod,
                        normativ,
                        cv.fio,
                        cv.ngp_cnt
                    );
                }
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                conn_db.Close();
                prefList.Clear();

                MonitorLog.WriteLog("Ошибка выполнения запроса " + ex.Message, MonitorLog.typelog.Error, 20, 201, true);
                return ret;
            }

            reader.Close();
            conn_db.Close();
            prefList.Clear();

            DataSet FDataSet = new DataSet();
            FDataSet.Tables.Add(table);

            Report report = new Report();
            report.Load(PathHelper.GetReportTemplatePath("pu_data_samara.frx"));
            //report.Load(@"template/pu_data.frx");

            // дом
            string pdom = "<Все>";
            if (finder.ndom.Length > 0 || finder.ndom_po.Length > 0)
            {
                if (finder.ndom == finder.ndom_po || finder.ndom.Length > 0 && finder.ndom_po.Length == 0) pdom = finder.ndom;
                else
                {
                    if (finder.ndom.Length > 0) pdom = "c " + finder.ndom;
                    if (finder.ndom_po.Length > 0) pdom += " по " + finder.ndom_po;
                }
            }

            // месяц
            string pmonth = "";

            if (finder.dat_uchet == finder.dat_uchet_po) pmonth = dat_uchet.ToString("MMMM yyyy").ToLower() + " г.";
            else pmonth = dat_uchet.ToString("MMMM yyyy").ToLower() + " г." + " - " + dat_uchet_po.ToString("MMMM yyyy").ToLower() + " г.";

            report.SetParameterValue("pulica", finder.ulica);
            report.SetParameterValue("pservice", finder.service);
            report.SetParameterValue("pdom", pdom);
            report.SetParameterValue("pmonth", pmonth);
            report.SetParameterValue("isSmr", (Points.IsSmr ? "1" : "0"));
            report.SetParameterValue("parea", finder.area);

            if (finder.is_new == 1) report.SetParameterValue("pcounter", "Новые");
            else report.SetParameterValue("pcounter", "Все");

            report.RegisterData(FDataSet);
            report.GetDataSource("Q_master").Enabled = true;
            bool a = report.Prepare();

            string destinationFilename = "";

            try
            {
                string path = Constants.ExcelDir;
                if (Path.DirectorySeparatorChar.ToString() == "\\") path = path.Replace("/", "\\");
                else if (Path.DirectorySeparatorChar.ToString() == "/") path = path.Replace("\\", "/");

                if (!Directory.Exists(path)) throw new Exception("Не найдена папка " + path);

                destinationFilename = DateTime.Now.Ticks + "_" + finder.nzp_user + "_pu_data_samara.fpx";
                // директория
                ret.text = destinationFilename;
                report.SavePrepared(Path.Combine(path, destinationFilename));
            }
            catch (Exception ex)
            {
                ret.text = "";
                ret.result = false;
                MonitorLog.WriteLog("Ошибка формирования отчета \"Данные приборов учета по жилым домам\" " + ex.Message, MonitorLog.typelog.Error, 20, 401, true);
            }
            return ret;
        }
    }
}
