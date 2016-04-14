using System;
using System.Data;
using System.Linq;
using System.Xml.Linq;
using System.IO;
using System.Text;
using System.Collections.Generic;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;


namespace STCLINE.KP50.DataBase
{
    //----------------------------------------------------------------------
    public partial class DbAnalizClient : DataBaseHead
    //----------------------------------------------------------------------
    {
        //----------------------------------------------------------------------
        public List<Dom> GetAdres(int level, int dtip, Dom finder, out Returns ret, int year) //вытащить адреса для грида
        //----------------------------------------------------------------------
        {
            //level:
            //0 - nzp_area
            //1 - nzp_wp
            //2 - nzp_geu
            //3 - nzp_ul
            //4 - nzp_dom

            ret = Utils.InitReturns();
            if (finder.nzp_user < 1)
            {
                ret.result = false;
                ret.text = "Не определен пользователь";
                return null;
            }

            string srv = "";
            if (finder.nzp_server > 0)
                srv = "_" + finder.nzp_server;

            List<Dom> Spis = new List<Dom>();

            Spis.Clear();

            //выбрать общее кол-во
            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result) return null;

            string pXX_spdom = "anl" + year + "_dom" + srv;

            if (!TableInWebCashe(conn_web, pXX_spdom))
            {
                ret.text = "Данные не заполнены";
                conn_web.Close();
                return null;
            }


            StringBuilder sql = new StringBuilder();
#if PG
            sql.Append(" Select distinct ");
#else
            sql.Append(" Select unique ");
#endif
            string sw = "";

            switch (level)
            {
                case 0: 
                    {
                        switch (dtip)
                        {
                            case Constants.act_aa_showuk: //area
                                {
                                    sql.Append(" 0 nzp_wp, '' point, '' pref, a.nzp_area, area, 0 nzp_geu, '' geu, 0 nzp_ul, '' ulica, 0 nzp_dom, '' ndom, 0 idom ");
                                    break;
                                }
                            case Constants.act_aa_showbd: //point
                                {
                                    sql.Append(" t.nzp_wp, point, pref, 0 nzp_area, '' area, 0 nzp_geu, '' geu, 0 nzp_ul, '' ulica, 0 nzp_dom, '' ndom, 0 idom ");
                                    break;
                                }
                            case Constants.act_aa_showul: //ulica
                                {
                                    sql.Append(" 0 nzp_wp, '' point, '' pref,  0 nzp_area, '' area, 0 nzp_geu, '' geu, t.nzp_ul, ulica, 0 nzp_dom, '' ndom, 0 idom ");
                                    break;
                                }
                        }
                        break;
                    }
                case 1: 
                    {
                        switch (dtip)
                        {
                            case Constants.act_aa_showuk: //ulica
                                {
                                    sql.Append(" 0 nzp_wp, '' point, '' pref,  0 nzp_area, '' area, 0 nzp_geu, '' geu, t.nzp_ul, ulica, 0 nzp_dom, '' ndom, 0 idom ");
                                    if (finder.nzp_area > 0)
                                        sw += " and a.nzp_area = " + finder.nzp_area;
                                    break;
                                }
                            case Constants.act_aa_showbd: //geu
                                {
                                    sql.Append(" 0 nzp_wp, '' point, '' pref,  0 nzp_area, '' area, a.nzp_geu, geu, 0 nzp_ul, '' ulica, 0 nzp_dom, '' ndom, 0 idom ");
                                    if (finder.nzp_wp > 0)
                                        sw += " and t.nzp_wp = " + finder.nzp_wp;
                                    break;
                                }
                            case Constants.act_aa_showul: //dom
                                {
                                    sql.Append(" t.nzp_wp, point, pref, a.nzp_area, area, a.nzp_geu,  geu, t.nzp_ul, ulica, t.nzp_dom, ndom, idom ");
                                    if (finder.nzp_ul > 0)
                                        sw += " and nzp_ul = " + finder.nzp_ul;
                                    break;
                                }
                        }
                        break;
                    }
                case 2: //
                    {
                        switch (dtip)
                        {
                            case Constants.act_aa_showuk: //dom
                                {
                                    sql.Append(" t.nzp_wp, point, pref, a.nzp_area, area, a.nzp_geu,  geu, t.nzp_ul, ulica, t.nzp_dom, ndom, idom ");
                                    if (finder.nzp_area > 0)
                                        sw += " and a.nzp_area = " + finder.nzp_area;
                                    if (finder.nzp_ul > 0)
                                        sw += " and t.nzp_ul = " + finder.nzp_ul;
                                    break;
                                }
                            case Constants.act_aa_showbd: //ulica
                                {
                                    sql.Append(" 0 nzp_wp, '' point, '' pref,  0 nzp_area, '' area, 0 nzp_geu, '' geu, t.nzp_ul, ulica, 0 nzp_dom, '' ndom, 0 idom ");
                                    if (finder.nzp_wp > 0)
                                        sw += " and t.nzp_wp = " + finder.nzp_wp;
                                    if (finder.nzp_geu > 0)
                                        sw += " and a.nzp_geu = " + finder.nzp_geu;
                                    break;
                                }
                            case Constants.act_aa_showul: //
                                {
                                    break;
                                }
                        }
                        break;
                    }
                case 3: //ulica
                    {
                        switch (dtip)
                        {
                            case Constants.act_aa_showuk: 
                                {
                                    break;
                                }
                            case Constants.act_aa_showbd: //dom
                                {
                                    sql.Append(" t.nzp_wp, point, pref, a.nzp_area, area, a.nzp_geu,  geu, t.nzp_ul, ulica, t.nzp_dom, ndom, idom ");
                                    if (finder.nzp_wp > 0)
                                        sw += " and t.nzp_wp = " + finder.nzp_wp;
                                    if (finder.nzp_geu > 0)
                                        sw += " and a.nzp_geu = " + finder.nzp_geu;
                                    if (finder.nzp_ul > 0)
                                        sw += " and t.nzp_ul = " + finder.nzp_ul;
                                    break;
                                }
                            case Constants.act_aa_showul: 
                                {
                                    break;
                                }
                        }
                        break;
                    }
            }

            if (finder.RolesVal != null)
            {
                if (finder.RolesVal.Count > 0)
                {
                    foreach (_RolesVal role in finder.RolesVal)
                    {
                        if (role.tip == Constants.role_sql)
                        {
                            string s = "";
                            switch (role.kod)
                            {
                                case Constants.role_sql_area:
                                    {
                                        s = " and a.nzp_area in (";
                                        break;
                                    }
                                case Constants.role_sql_serv:
                                    {
                                        s = " and a.nzp_serv in (";
                                        break;
                                    }
                                case Constants.role_sql_supp:
                                    {
                                        s = " and a.nzp_supp in (";
                                        break;
                                    }
                                case Constants.role_sql_geu:
                                    s = " and a.nzp_geu in (";
                                    break;
                                case Constants.role_sql_wp:
                                    s = " and a.nzp_wp in (";
                                    break;
                            }

                            if (s != "") sw += s + role.val.Trim() + ")";

                        }
                    }
                }
            }

            string analiz1 = "anl" + year + srv; 
            
            sql.Append(" From " + pXX_spdom + " t, " + analiz1 + " a ");
            sql.Append(" Where t.nzp_dom = a.nzp_dom and t.nzp_area = a.nzp_area and t.nzp_geu = a.nzp_geu  and t.nzp_wp = a.nzp_wp  " + sw + " Order by area,point,geu,ulica,idom");

#if PG           
            ret = ExecSQL(conn_web, " analyze " + pXX_spdom, false);
            ret = ExecSQL(conn_web, " analyze " + analiz1, false);
#else               
            ret = ExecSQL(conn_web, " Update statistics for table " + pXX_spdom, false);
            ret = ExecSQL(conn_web, " Update statistics for table " + analiz1, false);
#endif

            //выбрать список
            IDataReader reader;
            if (!ExecRead(conn_web, out reader, sql.ToString(), true).result)
            {
                conn_web.Close();
                return null;
            }
            try
            {
                int i = 0;
                while (reader.Read())
                {
                    i = i + 1;
                    Dom zap = new Dom();
                    zap.num = (i + finder.skip).ToString();

                    if (reader["nzp_dom"] == DBNull.Value)
                        zap.nzp_dom = 0;
                    else
                        zap.nzp_dom = (int)reader["nzp_dom"];
                    if (reader["nzp_ul"] == DBNull.Value)
                        zap.nzp_ul = 0;
                    else
                        zap.nzp_ul = (int)reader["nzp_ul"];
                    if (reader["nzp_area"] == DBNull.Value)
                        zap.nzp_area = 0;
                    else
                        zap.nzp_area = (int)reader["nzp_area"];
                    if (reader["nzp_geu"] == DBNull.Value)
                        zap.nzp_geu = 0;
                    else
                        zap.nzp_geu = (int)reader["nzp_geu"];
                    if (reader["nzp_wp"] == DBNull.Value)
                        zap.nzp_wp = 0;
                    else
                        zap.nzp_wp = (int)reader["nzp_wp"];
                    if (reader["area"] == DBNull.Value)
                        zap.area = "";
                    else
                        zap.area = (string)reader["area"];
                    if (reader["geu"] == DBNull.Value)
                        zap.geu = "";
                    else
                        zap.geu = (string)reader["geu"];
                    if (reader["ulica"] == DBNull.Value)
                        zap.ulica = "";
                    else
                        zap.ulica = (string)reader["ulica"];
                    if (reader["ndom"] == DBNull.Value)
                        zap.ndom = "";
                    else
                        zap.ndom = (string)reader["ndom"];
                    if (reader["point"] == DBNull.Value)
                        zap.point = "";
                    else
                        zap.point = (string)reader["point"];
                    if (reader["pref"] == DBNull.Value)
                        zap.pref = "";
                    else
                        zap.pref = (string)reader["pref"];

                    Spis.Add(zap);
                }
                reader.Close();
                conn_web.Close();

                return Spis;
            }
            catch (Exception ex)
            {
                reader.Close();
                conn_web.Close();

                ret.result = false;
                ret.text = ex.Message;

                string err;
                if (Constants.Viewerror)
                    err = " \n " + ex.Message;
                else
                    err = "";

                MonitorLog.WriteLog("Ошибка заполнения списка адресов для анализа " + year + " " + err, MonitorLog.typelog.Error, 20, 201, true);

                return null;
            }

        }//GetAdres




        
        //----------------------------------------------------------------------
        public List<AnlSupp> GetSupp(int level, int dtip, AnlSupp finder, out Returns ret, int year) //вытащить адреса для грида
        //----------------------------------------------------------------------
        {
            //level:
            //0 - nzp_area
            //1 - nzp_wp
            //2 - nzp_geu
            //3 - nzp_ul
            //4 - nzp_dom

            ret = Utils.InitReturns();
            if (finder.nzp_user < 1)
            {
                ret.result = false;
                ret.text = "Не определен пользователь";
                return null;
            }

            List<AnlSupp> Spis = new List<AnlSupp>();

            Spis.Clear();

            //выбрать общее кол-во
            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result) return null;

            string srv = "";
            if (finder.nzp_server > 0)
                srv = "_" + finder.nzp_server;

            string pXX_spsupp = "anl" + year + "_supp" + srv;

            if (!TableInWebCashe(conn_web, pXX_spsupp))
            {
                ret.text = "Данные не заполнены";
                conn_web.Close();
                return null;
            }

            StringBuilder sql = new StringBuilder();
#if PG
            sql.Append(" Select distinct ");
#else
            sql.Append(" Select unique ");
#endif
            string sw = "";
            switch (level)
            {
            
                case 0:
                    {
                        switch (dtip)
                        {
                            case Constants.act_as_showsupp: //supp
                                {
                                    sql.Append(" 0 nzp_area, '' area, 0 nzp_geu, '' geu, nzp_supp,(case when nzp_supp = 0 then 'Неизвестный поставщик' else name_supp end), 0 nzp_serv, '' service, 0 nzp_frm, '' name_frm ");
                                    break;
                                }
                            case Constants.act_as_showserv: //serv
                                {
                                    sql.Append(" 0 nzp_area, '' area, 0 nzp_geu, '' geu, 0 nzp_supp, '' name_supp, nzp_serv, service, 0 nzp_frm, '' name_frm ");
                                    break;
                                }
                            case Constants.act_as_showuk: //area
                                {
                                    sql.Append(" nzp_area, area, 0 nzp_geu, '' geu, 0 nzp_supp, '' name_supp, 0 nzp_serv, '' service, 0 nzp_frm, '' name_frm ");
                                    break;
                                }
                        }
                        break;
                    }
                case 1:
                    {
                        switch (dtip)
                        {
                            case Constants.act_as_showsupp: //serv
                                {
                                    sql.Append(" 0 nzp_area, '' area, 0 nzp_geu, '' geu, 0 nzp_supp, '' name_supp, nzp_serv, service, 0 nzp_frm, '' name_frm ");
                                    if (finder.nzp_supp > 0)
                                        sw += " and nzp_supp = " + finder.nzp_supp;
                                    break;
                                }
                            case Constants.act_as_showserv: //supp
                                {
                                    sql.Append(" 0 nzp_area, '' area, 0 nzp_geu, '' geu, nzp_supp, (case when nzp_supp = 0 then 'Неизвестный поставщик' else name_supp end), 0 nzp_serv, '' service, 0 nzp_frm, '' name_frm ");
                                    if (finder.nzp_serv > 0)
                                        sw += " and nzp_serv = " + finder.nzp_serv;
                                    break;
                                }
                            case Constants.act_as_showuk: //supp
                                {
                                    sql.Append(" 0 nzp_area, '' area, 0 nzp_geu, '' geu, nzp_supp, (case when nzp_supp = 0 then 'Неизвестный поставщик' else name_supp end), 0 nzp_serv, '' service, 0 nzp_frm, '' name_frm ");
                                    if (finder.nzp_area > 0)
                                        sw += " and nzp_area = " + finder.nzp_area;
                                    break;
                                }
                        }
                        break;
                    }
                case 2: //
                    {
                        switch (dtip)
                        {
                            case Constants.act_as_showsupp: //form
                                {
                                    sql.Append(" 0 nzp_area, '' area, 0 nzp_geu, '' geu, 0 nzp_supp, '' name_supp, 0 nzp_serv, '' service, nzp_frm, name_frm ");
                                    if (finder.nzp_area > 0)
                                        sw += " and nzp_area = " + finder.nzp_area;
                                    if (finder.nzp_supp > 0)
                                        sw += " and nzp_supp = " + finder.nzp_supp;
                                    if (finder.nzp_serv > 0)
                                        sw += " and nzp_serv = " + finder.nzp_serv;
                                    break;
                                }
                            case Constants.act_as_showserv: //area
                                {
                                    sql.Append(" nzp_area, area, 0 nzp_geu, '' geu, 0 nzp_supp, '' name_supp, 0 nzp_serv, '' service, 0 nzp_frm, '' name_frm ");
                                    if (finder.nzp_area > 0)
                                        sw += " and nzp_area = " + finder.nzp_area;
                                    if (finder.nzp_supp > 0)
                                        sw += " and nzp_supp = " + finder.nzp_supp;
                                    if (finder.nzp_serv > 0)
                                        sw += " and nzp_serv = " + finder.nzp_serv;
                                    break;
                                }
                            case Constants.act_as_showuk: //serv
                                {
                                    sql.Append(" 0 nzp_area, '' area, 0 nzp_geu, '' geu, 0 nzp_supp, '' name_supp, nzp_serv, service, 0 nzp_frm, '' name_frm ");
                                    if (finder.nzp_area > 0)
                                        sw += " and nzp_area = " + finder.nzp_area;
                                    if (finder.nzp_supp > 0)
                                        sw += " and nzp_supp = " + finder.nzp_supp;
                                    break;
                                }
                        }
                        break;
                    }
                case 3: //
                    {
                        switch (dtip)
                        {
                            case Constants.act_as_showsupp:
                                {
                                    break;
                                }
                            case Constants.act_as_showserv: //geu
                                {
                                    sql.Append(" 0 nzp_area, '' area, nzp_geu, geu, 0 nzp_supp, '' name_supp, 0 nzp_serv, '' service, 0 nzp_frm, '' name_frm ");
                                    if (finder.nzp_serv > 0)
                                        sw += " and nzp_serv = " + finder.nzp_serv;
                                    if (finder.nzp_supp > 0)
                                        sw += " and nzp_supp = " + finder.nzp_supp;
                                    if (finder.nzp_area > 0)
                                        sw += " and nzp_area = " + finder.nzp_area;
                                    break;
                                }
                            case Constants.act_as_showuk: //form
                                {
                                    sql.Append(" 0 nzp_area, '' area, 0 nzp_geu, '' geu, 0 nzp_supp, '' name_supp, 0 nzp_serv, '' service, nzp_frm, name_frm ");
                                    if (finder.nzp_area > 0)
                                        sw += " and nzp_area = " + finder.nzp_area;
                                    if (finder.nzp_supp > 0)
                                        sw += " and nzp_supp = " + finder.nzp_supp;
                                    if (finder.nzp_serv > 0)
                                        sw += " and nzp_serv = " + finder.nzp_serv;
                                    break;
                                }
                        }
                        break;
                    }
            }

            if (finder.RolesVal != null)
            {
                if (finder.RolesVal.Count > 0)
                {
                    foreach (_RolesVal role in finder.RolesVal)
                    {
                        if (role.tip == Constants.role_sql)
                        {
                            string s = "";
                            switch (role.kod)
                            {
                                case Constants.role_sql_area:
                                    {
                                        s = " and nzp_area in (";
                                        break;
                                    }
                                case Constants.role_sql_serv:
                                    {
                                        s = " and nzp_serv in (";
                                        break;
                                    }
                                case Constants.role_sql_supp:
                                    {
                                        s = " and nzp_supp in (";
                                        break;
                                    }
                            }

                            if (s != "") sw += s + role.val.Trim() + ")";

                        }
                    }
                }
            }

            sql.Append(" From " + pXX_spsupp + " t ");
            sql.Append(" Where 1=1 and trim(name_supp) <> '' " + sw + " Order by name_supp,service,area,geu");

            //выбрать список
            IDataReader reader;
            if (!ExecRead(conn_web, out reader, sql.ToString(), true).result)
            {
                conn_web.Close();
                return null;
            }
            try
            {
                while (reader.Read())
                {
                    AnlSupp zap = new AnlSupp();

                    if (reader["nzp_supp"] == DBNull.Value)
                        zap.nzp_supp = 0;
                    else
                        zap.nzp_supp = (int)reader["nzp_supp"];
                    if (reader["nzp_serv"] == DBNull.Value)
                        zap.nzp_serv = 0;
                    else
                        zap.nzp_serv = (int)reader["nzp_serv"];
                    if (reader["nzp_area"] == DBNull.Value)
                        zap.nzp_area = 0;
                    else
                        zap.nzp_area = (int)reader["nzp_area"];
                    if (reader["nzp_geu"] == DBNull.Value)
                        zap.nzp_geu = 0;
                    else
                        zap.nzp_geu = (int)reader["nzp_geu"];

                    if (reader["area"] == DBNull.Value)
                        zap.area = "";
                    else
                        zap.area = (string)reader["area"];
                    if (reader["geu"] == DBNull.Value)
                        zap.geu = "";
                    else
                        zap.geu = (string)reader["geu"];
                    if (reader["name_supp"] == DBNull.Value)
                        zap.name_supp = "";
                    else
                        zap.name_supp = (string)reader["name_supp"];
                    if (reader["service"] == DBNull.Value)
                        zap.service = "";
                    else
                        zap.service = (string)reader["service"];
                    if (reader["name_frm"] == DBNull.Value)
                        zap.name_frm = "";
                    else
                        zap.name_frm = (string)reader["name_frm"];

                    Spis.Add(zap);
                }
                reader.Close();
                conn_web.Close();

                return Spis;
            }
            catch (Exception ex)
            {
                reader.Close();
                conn_web.Close();

                ret.result = false;
                ret.text = ex.Message;

                string err;
                if (Constants.Viewerror)
                    err = " \n " + ex.Message;
                else
                    err = "";

                MonitorLog.WriteLog("Ошибка заполнения списка поставщиков для анализа " + err, MonitorLog.typelog.Error, 20, 201, true);

                return null;
            }

        }//GetSupp


        //----------------------------------------------------------------------
        void GoPointSupp(IDbConnection conn_web, int nzp_server, string webdata, string pXX_spsupp_full, string analiz1, out Returns ret)
        //----------------------------------------------------------------------
        {
            string conn_kernel = Constants.cons_Kernel;
            if (nzp_server > 0)
                conn_kernel = Points.GetConnByServer(nzp_server);

            IDbConnection conn_db = GetConnection(conn_kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result)
            {
                conn_web.Close();
                return;
            }

            //цикл по s_point
            foreach (_Point zap in Points.PointList)
            {
                if (nzp_server > 0)
                {
                    if (zap.nzp_server != nzp_server) continue;
                }

                //проверка на доступность банка данных
#if PG
                if (!TempTableInWebCashe(conn_db, zap.pref + "_data.s_area"))
                {
                    MonitorLog.WriteLog("Первый запуск приложения.Загрузка поставщиков.Банк данных . " + zap.pref + " не доступен", MonitorLog.typelog.Warn, true);
                    continue;
                }
#else
                if (!TempTableInWebCashe(conn_db, zap.pref + "_data:s_area"))
                {
                    MonitorLog.WriteLog("Первый запуск приложения.Загрузка поставщиков.Банк данных : " + zap.pref + " не доступен", MonitorLog.typelog.Warn, true);
                    continue;
                }
#endif
                ExecSQL(conn_db, " Drop table ttt_supp ", false);

                StringBuilder sql = new StringBuilder();

#if PG
  sql.Append(" Select distinct z.nzp_area,z.nzp_geu,z.nzp_supp,z.nzp_serv,z.nzp_frm, ");
                sql.Append(" area,geu, name_supp,service, trim(coalesce(f.name_frm,''))||' ('||trim(coalesce(m.measure,''))||')' as name_frm ");
                sql.Append(" From " + analiz1 + " z, ");
                sql.Append(" outer " + zap.pref + "_data.s_area a,");
                sql.Append(" outer " + zap.pref + "_data.s_geu g, ");
                sql.Append(" outer " + zap.pref + "_kernel.services s, ");
                sql.Append(" outer " + zap.pref + "_kernel.supplier p, ");
                sql.Append(" outer (" + zap.pref + "_kernel.formuls f, " + zap.pref + "_kernel.s_measure m )");
                sql.Append(" Where z.nzp_wp = " + zap.nzp_wp + " and z.nzp_area = a.nzp_area and z.nzp_geu = g.nzp_geu and z.nzp_serv = s.nzp_serv and z.nzp_supp = p.nzp_supp ");
                sql.Append("   and z.nzp_frm = f.nzp_frm and f.nzp_measure = m.nzp_measure ");
                sql.Append("   and 1 > ( Select count(*) From " + pXX_spsupp_full + " pp ");
                sql.Append("  Where z.nzp_area = pp.nzp_area and z.nzp_geu = pp.nzp_geu and z.nzp_supp = pp.nzp_supp ");
                sql.Append("    and z.nzp_serv = pp.nzp_serv and z.nzp_frm = pp.nzp_frm ) ");
                sql.Append(" Into temp ttt_supp With no log ");
                //int key = LogSQL(conn_web, finder.nzp_user, pXX_spsupp_full + "." + whereString);
#else
                sql.Append(" Select unique z.nzp_area,z.nzp_geu,z.nzp_supp,z.nzp_serv,z.nzp_frm, ");
                sql.Append(" area,geu, name_supp,service, trim(nvl(f.name_frm,''))||' ('||trim(nvl(m.measure,''))||')' as name_frm ");
                sql.Append(" From " + analiz1 + " z, ");
                sql.Append(" outer " + zap.pref + "_data:s_area a,");
                sql.Append(" outer " + zap.pref + "_data:s_geu g, ");
                sql.Append(" outer " + zap.pref + "_kernel:services s, ");
                sql.Append(" outer " + zap.pref + "_kernel:supplier p, ");
                sql.Append(" outer (" + zap.pref + "_kernel:formuls f, " + zap.pref + "_kernel:s_measure m )");
                sql.Append(" Where z.nzp_wp = " + zap.nzp_wp + " and z.nzp_area = a.nzp_area and z.nzp_geu = g.nzp_geu and z.nzp_serv = s.nzp_serv and z.nzp_supp = p.nzp_supp ");
                sql.Append("   and z.nzp_frm = f.nzp_frm and f.nzp_measure = m.nzp_measure ");
                sql.Append("   and 1 > ( Select count(*) From " + pXX_spsupp_full + " pp ");
                sql.Append("  Where z.nzp_area = pp.nzp_area and z.nzp_geu = pp.nzp_geu and z.nzp_supp = pp.nzp_supp ");
                sql.Append("    and z.nzp_serv = pp.nzp_serv and z.nzp_frm = pp.nzp_frm ) ");
                sql.Append(" Into temp ttt_supp With no log ");
                //int key = LogSQL(conn_web, finder.nzp_user, pXX_spsupp_full + ":" + whereString);
#endif
                ret = ExecSQL(conn_db, sql.ToString(), true, 800);
                if (!ret.result)
                {
                    //if (key > 0) LogSQL_Error(conn_web, key, ret.text);

                    conn_db.Close();
                    conn_web.Close();
                    return;
                }

#if PG
  ret = ExecSQL(conn_db,
                           " Insert into " + pXX_spsupp_full + " (nzp_area,nzp_geu,nzp_supp,nzp_serv,nzp_frm, area,geu, name_supp,service, name_frm)" +
                           " Select nzp_area,nzp_geu,nzp_supp,nzp_serv,nzp_frm, area,geu, name_supp,service, name_frm " +
                           " From ttt_supp ", true);
#else
                ret = ExecSQL(conn_db,
                                         " Insert into " + pXX_spsupp_full + " (nzp_area,nzp_geu,nzp_supp,nzp_serv,nzp_frm, area,geu, name_supp,service, name_frm)" +
                                         " Select nzp_area,nzp_geu,nzp_supp,nzp_serv,nzp_frm, area,geu, name_supp,service, name_frm " +
                                         " From ttt_supp ", true);
#endif
                if (!ret.result)
                {
                    conn_db.Close();
                    conn_web.Close();
                    return;
                }

                ExecSQL(conn_db, " Drop table ttt_supp ", false);
                 
            }

            conn_db.Close(); //закрыть соединение с основной базой
        }

        //----------------------------------------------------------------------
        void GoPointAnl(IDbConnection conn_web, int nzp_server, string __analiz1_full, string analiz1_full, string tmpt, bool b_create, int year, string analiz1, out Returns ret)
        //----------------------------------------------------------------------
        {
            string conn_kernel = Constants.cons_Kernel;
            if (nzp_server > 0)
                conn_kernel = Points.GetConnByServer(nzp_server);

            IDbConnection conn_db = GetConnection(conn_kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result)
            {
                conn_web.Close();
                return;
            }

            //цикл по s_point
            foreach (_Point zap in Points.PointList)
            {
                if (nzp_server > 0)
                {
                    if (zap.nzp_server != nzp_server) continue;
                }
                StringBuilder sql = new StringBuilder();

                //вытащить открытые лс
#if PG
ExecSQL(conn_db, " Drop table topen_ls1 ", false);
                ExecSQL(conn_db, " Drop table " + tmpt, false);
                sql.Append(" Select distinct k.nzp_kvar, k.nzp_dom, k.nzp_area, k.nzp_geu ");
                sql.Append(" Into unlogged table topen_ls1 ");
                sql.Append(" From " + zap.pref + "_data.kvar k, " + zap.pref + "_data.prm_3 p ");
                sql.Append(" Where k.nzp_kvar = p.nzp and k.num_ls > 0 and p.nzp_prm = 51 and p.val_prm in ('1','2') and p.is_actual <> 100 ");
                sql.Append(" and p.dat_s <= public.mdy(12,31," + year + ") and p.dat_po >= public.mdy(1,1," + year + ") ");

#else
                ExecSQL(conn_db, " Drop table topen_ls1 ", false);
                ExecSQL(conn_db, " Drop table " + tmpt, false);
                sql.Append(" Select unique k.nzp_kvar, k.nzp_dom, k.nzp_area, k.nzp_geu ");
                sql.Append(" From " + zap.pref + "_data:kvar k, " + zap.pref + "_data:prm_3 p ");
                sql.Append(" Where k.nzp_kvar = p.nzp and k.num_ls > 0 and p.nzp_prm = 51 and p.val_prm in ('1','2') and p.is_actual <> 100 ");
                sql.Append(" and p.dat_s <= mdy(12,31," + year + ") and p.dat_po >= mdy(1,1," + year + ") ");
                sql.Append(" Into temp topen_ls1 With no log ");
#endif
                ret = ExecSQL(conn_db, sql.ToString(), true, 600);
                if (!ret.result)
                {
                    conn_db.Close();
                    conn_web.Close();
                    return;
                }

#if PG
 ret = ExecSQL(conn_db, " Create unique index ix_topen_ls1 on topen_ls1 (nzp_kvar) ", true);
                if (!ret.result)
                {
                    conn_db.Close();
                    conn_web.Close();
                    return;
                }
                ret = ExecSQL(conn_db, " analyze topen_ls1 ", true);
                if (!ret.result)
                {
                    conn_db.Close();
                    conn_web.Close();
                    return;
                }
#else
                ret = ExecSQL(conn_db, " Create unique index ix_topen_ls1 on topen_ls1 (nzp_kvar) ", true);
                if (!ret.result)
                {
                    conn_db.Close();
                    conn_web.Close();
                    return;
                }
                ret = ExecSQL(conn_db, " Update statistics for table topen_ls1 ", true);
                if (!ret.result)
                {
                    conn_db.Close();
                    conn_web.Close();
                    return;
                }
#endif

                sql.Remove(0, sql.Length);

#if PG
                sql.Append(" Select distinct 0 as kod, k.nzp_dom," + zap.nzp_wp.ToString() + " as nzp_wp, k.nzp_area, k.nzp_geu, t.nzp_serv, t.nzp_supp, t.nzp_frm ");
                sql.Append(" From topen_ls1 k, " + zap.pref + "_data.tarif t ");
                sql.Append(" Where k.nzp_kvar = t.nzp_kvar and t.dat_s <= mdy(12,31," + year.ToString() + ") and t.dat_po >= mdy(1,1," + year.ToString() + ") ");
                sql.Append(" and t.is_actual <> 100 ");
                sql.Append(" Into temp " + tmpt + " With no log ");
#else
                sql.Append(" Select unique 0 as kod, k.nzp_dom," + zap.nzp_wp.ToString() + " as nzp_wp, k.nzp_area, k.nzp_geu, t.nzp_serv, t.nzp_supp, t.nzp_frm ");
                sql.Append(" From topen_ls1 k, " + zap.pref + "_data:tarif t ");
                sql.Append(" Where k.nzp_kvar = t.nzp_kvar and t.dat_s <= mdy(12,31," + year.ToString() + ") and t.dat_po >= mdy(1,1," + year.ToString() + ") ");
                sql.Append(" and t.is_actual <> 100 ");
                sql.Append(" Into temp " + tmpt + " With no log ");
#endif
                ret = ExecSQL(conn_db, sql.ToString(), true, 600);

                ExecSQL(conn_db, " Drop table topen_ls1 ", false);

                if (!ret.result)
                {
                    conn_db.Close();
                    conn_web.Close();
                    return;
                }

                ret = ExecSQL(conn_db,
                    " Create index ix_" + tmpt.Trim() + " on " + tmpt + " (nzp_dom) "
                    , true);
                if (!ret.result)
                {
                    conn_db.Close();
                    conn_web.Close();
                    return;
                }

#if PG
ret = ExecSQL(conn_db,
                    " Update " + tmpt +
                    " Set nzp_area = ( " +
                         " Select nzp_area From " + zap.pref + "_data.dom d " +
                         " Where " + tmpt + ".nzp_dom = d.nzp_dom  ) " +
                    " Where nzp_area < 1 " +
                    "   and exists ( Select nzp_dom From " + zap.pref + "_data.dom d Where " + tmpt + ".nzp_dom = d.nzp_dom ) "
                    , true);
#else
                ret = ExecSQL(conn_db,
                                    " Update " + tmpt +
                                    " Set nzp_area = ( " +
                                         " Select nzp_area From " + zap.pref + "_data:dom d " +
                                         " Where " + tmpt + ".nzp_dom = d.nzp_dom  ) " +
                                    " Where nzp_area < 1 " +
                                    "   and exists ( Select nzp_dom From " + zap.pref + "_data:dom d Where " + tmpt + ".nzp_dom = d.nzp_dom ) "
                                    , true);
#endif
                if (!ret.result)
                {
                    conn_db.Close();
                    conn_web.Close();
                    return;
                }
#if PG
                    ret = ExecSQL(conn_db,
                    " Update " + tmpt +
                    " Set nzp_geu = ( " +
                         " Select nzp_geu From " + zap.pref + "_data.dom d " +
                         " Where " + tmpt + ".nzp_dom = d.nzp_dom  ) " +
                    " Where nzp_geu < 1 " +
                    "   and exists ( Select nzp_dom From " + zap.pref + "_data.dom d Where " + tmpt + ".nzp_dom = d.nzp_dom ) "
                    , true);
#else
                ret = ExecSQL(conn_db,
                                    " Update " + tmpt +
                                    " Set nzp_geu = ( " +
                                         " Select nzp_geu From " + zap.pref + "_data:dom d " +
                                         " Where " + tmpt + ".nzp_dom = d.nzp_dom  ) " +
                                    " Where nzp_geu < 1 " +
                                    "   and exists ( Select nzp_dom From " + zap.pref + "_data:dom d Where " + tmpt + ".nzp_dom = d.nzp_dom ) "
                                    , true);
#endif
                if (!ret.result)
                {
                    conn_db.Close();
                    conn_web.Close();
                    return;
                }

                if (b_create)
                {
#if PG
//полная вставка
                    /*
                    ret = ExecSQL(conn_db, 
                        " Insert into " + __analiz1_full + " (nzp_dom,nzp_wp,nzp_area,nzp_geu,nzp_serv,nzp_supp,nzp_frm) " +
                        " Select distinct nzp_dom,nzp_wp,nzp_area,nzp_geu,nzp_serv,nzp_supp,nzp_frm "+
                        " From " + tmpt + " Where nzp_area > 0 and nzp_geu > 0 ", true, 1000);
                    */
                    ExecByStep(conn_db, tmpt, "nzp_dom",
                        " Insert into " + __analiz1_full + " (nzp_dom,nzp_wp,nzp_area,nzp_geu,nzp_serv,nzp_supp,nzp_frm) " +
                        " Select distinct nzp_dom,nzp_wp,nzp_area,nzp_geu,nzp_serv,nzp_supp,nzp_frm " +
                        " From " + tmpt + " Where nzp_area > 0 and nzp_geu > 0 "
                      , 1000, "", out ret);
#else
                    //полная вставка
                    /*
                    ret = ExecSQL(conn_db, 
                        " Insert into " + __analiz1_full + " (nzp_dom,nzp_wp,nzp_area,nzp_geu,nzp_serv,nzp_supp,nzp_frm) " +
                        " Select unique nzp_dom,nzp_wp,nzp_area,nzp_geu,nzp_serv,nzp_supp,nzp_frm "+
                        " From " + tmpt + " Where nzp_area > 0 and nzp_geu > 0 ", true, 1000);
                    */
                    ExecByStep(conn_db, tmpt, "nzp_dom",
                        " Insert into " + __analiz1_full + " (nzp_dom,nzp_wp,nzp_area,nzp_geu,nzp_serv,nzp_supp,nzp_frm) " +
                        " Select unique nzp_dom,nzp_wp,nzp_area,nzp_geu,nzp_serv,nzp_supp,nzp_frm " +
                        " From " + tmpt + " Where nzp_area > 0 and nzp_geu > 0 "
                      , 1000, "", out ret);
#endif
                    if (!ret.result)
                    {
                        conn_db.Close();
                        conn_web.Close();
                        return;
                    }
                }
                else
                {
                    //иначе, вставим только отсутствующие связки
                    ret = ExecSQL(conn_db, " Create unique index " + tmpt + "_1 on " + tmpt + " (nzp_dom, nzp_area,nzp_geu,nzp_supp, nzp_serv, nzp_wp, nzp_frm) ", true);

                    if (ret.result)
                        ret = ExecSQL(conn_db, " Create index " + tmpt + "_2 on " + tmpt + " (nzp_area,nzp_geu, nzp_serv,nzp_supp, nzp_frm) ", true);
#if PG
                    if (ret.result)
                        ret = ExecSQL(conn_db, " analyze " + tmpt, true);
#else
                    if (ret.result)
                        ret = ExecSQL(conn_db, " Update statistics for table " + tmpt, true);
#endif
                    if (ret.result)
                        ret = ExecSQL(conn_db, " Update " + tmpt + " Set kod = 1 " +
                                               " Where 1 > ( Select count(*) " +
                                               " From " + analiz1_full + ".nzp_dom  = " + tmpt + ".nzp_dom " +
                                               "  and " + analiz1_full + ".nzp_area = " + tmpt + ".nzp_area " +
                                               "  and " + analiz1_full + ".nzp_geu  = " + tmpt + ".nzp_geu " +
                                               "  and " + analiz1_full + ".nzp_supp = " + tmpt + ".nzp_supp " +
                                               "  and " + analiz1_full + ".nzp_serv = " + tmpt + ".nzp_serv " +
                                               "  and " + analiz1_full + ".nzp_wp   = " + tmpt + ".nzp_wp " +
                                               "  and " + analiz1_full + ".nzp_frm  = " + tmpt + ".nzp_frm"
                                               , true, 1000);
                    if (ret.result)
                        ret = ExecSQL(conn_db, " Insert into " + analiz1_full + " (nzp_dom,nzp_wp,nzp_area,nzp_geu,nzp_serv,nzp_supp,nzp_frm) " +
                                               " Select nzp_dom,nzp_wp,nzp_area,nzp_geu,nzp_serv,nzp_supp,nzp_frm From " + tmpt + " Where kod = 1 ", true);

                    if (!ret.result)
                    {
                        conn_db.Close();
                        conn_web.Close();
                        return;
                    }
                }

            }

            conn_db.Close(); //закрыть соединение с основной базой
        }

        public delegate void CreateTable (IDbConnection conn_web, string table, out Returns ret);

        //----------------------------------------------------------------------
        public void CreateOrRenameAnlTable(string table, bool create, CreateTable createTable, out Returns ret)
        //----------------------------------------------------------------------
        {
            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);

            ret = OpenDb(conn_web, true);
            if (!ret.result)
            {
                ret.text = "Ошибка открытия БД";
                return;
            }

            CreateOrRenameAnlTable(conn_web, table, create, createTable, out ret);
        }
        //----------------------------------------------------------------------
        public void CreateOrRenameAnlTable(IDbConnection conn_web, string table, bool create, CreateTable createTable, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            if (create)
            {
                //наличие прообраза таблицы
                if (TableInWebCashe(conn_web, "__" + table))
                {
                    ret.result = false;
                    ret.tag = Constants.workinfon;
                    ret.text = "Выполняется подсчет данных для аналитики!";
                    return;
                }

                createTable(conn_web, "__" + table, out ret);
                if (!ret.result)
                {
                    return;
                }
            }
            else 
            {
                ExecSQL(conn_web, " Drop table " + table, false);
                ret = ExecSQL(conn_web, " Rename table  __" + table + " to " + table, true);
#if PG
                ExecSQL(conn_web, " analyze  " + table, false);
#else
                ExecSQL(conn_web, " Update statistics for table  " + table, false);
#endif
            }
        }

        //----------------------------------------------------------------------
        public void LoadAnlXX(List<AnlXX> ls, Finder finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);

            ret = OpenDb(conn_web, true);
            if (!ret.result)
            {
                ret.text = "Ошибка открытия БД";
                return;
            }
            //finder.
            string table = finder.database; //"anl" + finder.dopTag + "_" + finder.nzp_server;


            //поготовка Insert'а
            IDbCommand cmd = DBManager.newDbCommand(
                " Insert into __" + table +
                "  (nzp_anl, nzp_dom, nzp_wp, nzp_area, nzp_geu, nzp_serv, nzp_supp, nzp_frm) " +
                " Values (?,?,?,?,?,?,?,?) "
                , conn_web);

            DBManager.addDbCommandParameter(cmd, "nzp_anl", DbType.Int32);
            DBManager.addDbCommandParameter(cmd, "nzp_dom", DbType.Int32);
            DBManager.addDbCommandParameter(cmd, "nzp_wp",  DbType.Int32);
            DBManager.addDbCommandParameter(cmd, "nzp_area",DbType.Int32);
            DBManager.addDbCommandParameter(cmd, "nzp_geu", DbType.Int32);
            DBManager.addDbCommandParameter(cmd, "nzp_serv",DbType.Int32);
            DBManager.addDbCommandParameter(cmd, "nzp_supp",DbType.Int32);
            DBManager.addDbCommandParameter(cmd, "nzp_frm", DbType.Int32);

            try
            {
                foreach (AnlXX p in ls)
                {
                    (cmd.Parameters[0] as IDbDataParameter).Value = p.serial_key;
                    (cmd.Parameters[1] as IDbDataParameter).Value = p.nzp_dom;
                    (cmd.Parameters[2] as IDbDataParameter).Value = p.nzp_wp;
                    (cmd.Parameters[3] as IDbDataParameter).Value = p.nzp_area;
                    (cmd.Parameters[4] as IDbDataParameter).Value = p.nzp_geu;
                    (cmd.Parameters[5] as IDbDataParameter).Value = p.nzp_serv;
                    (cmd.Parameters[6] as IDbDataParameter).Value = p.nzp_supp;
                    (cmd.Parameters[7] as IDbDataParameter).Value = p.nzp_frm;

                    cmd.ExecuteNonQuery();
                    if (!ret.result)
                    {
                        conn_web.Close();
                        return;
                    }
                }
            }
            catch (Exception e)
            {
                conn_web.Close();

                ret.result = false;
                ret.text = e.Message;
                return;
            }

            conn_web.Close();
        }

        //----------------------------------------------------------------------
        public void LoadAnlDom(List<AnlDom> ls, Finder finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);

            ret = OpenDb(conn_web, true);
            if (!ret.result)
            {
                ret.text = "Ошибка открытия БД";
                return;
            }
            string table = finder.database; // "anl" + finder.dopTag + "_dom_" + finder.nzp_server;


            //поготовка Insert'а
            IDbCommand cmd = DBManager.newDbCommand(
                " Insert into __" + table +
                "  (nzp_sp, nzp_dom, nzp_ul, nzp_area, nzp_geu, nzp_wp, area, geu, ulica, ndom, idom, pref, point) " +
                " Values (?,?,?,?,?,?,?,?,?,?,?,?,?) "
                , conn_web);

            DBManager.addDbCommandParameter(cmd, "nzp_sp",  DbType.Int32);
            DBManager.addDbCommandParameter(cmd, "nzp_dom", DbType.Int32);
            DBManager.addDbCommandParameter(cmd, "nzp_ul",  DbType.Int32);
            DBManager.addDbCommandParameter(cmd, "nzp_area",DbType.Int32);
            DBManager.addDbCommandParameter(cmd, "nzp_geu", DbType.Int32);
            DBManager.addDbCommandParameter(cmd, "nzp_wp",  DbType.Int32);
            DBManager.addDbCommandParameter(cmd, "area",    DbType.String);
            DBManager.addDbCommandParameter(cmd, "geu",     DbType.String);
            DBManager.addDbCommandParameter(cmd, "ulica",   DbType.String);

            DBManager.addDbCommandParameter(cmd, "ndom",    DbType.String);
            DBManager.addDbCommandParameter(cmd, "idom",    DbType.Int32);
            DBManager.addDbCommandParameter(cmd, "pref",    DbType.String);
            DBManager.addDbCommandParameter(cmd, "point",   DbType.String);

            try
            {
                foreach (AnlDom p in ls)
                {
                    (cmd.Parameters[0] as IDbDataParameter).Value = p.serial_key;
                    (cmd.Parameters[1] as IDbDataParameter).Value = p.nzp_dom;
                    (cmd.Parameters[2] as IDbDataParameter).Value = p.nzp_ul;
                    (cmd.Parameters[3] as IDbDataParameter).Value = p.nzp_area;
                    (cmd.Parameters[4] as IDbDataParameter).Value = p.nzp_geu;
                    (cmd.Parameters[5] as IDbDataParameter).Value = p.nzp_wp;
                    (cmd.Parameters[6] as IDbDataParameter).Value = p.area;
                    (cmd.Parameters[7] as IDbDataParameter).Value = p.geu;
                    (cmd.Parameters[8] as IDbDataParameter).Value = p.ulica;
                    (cmd.Parameters[9] as IDbDataParameter).Value = p.ndom;
                    (cmd.Parameters[10] as IDbDataParameter).Value= p.idom;
                    (cmd.Parameters[11] as IDbDataParameter).Value= p.pref;
                    (cmd.Parameters[12] as IDbDataParameter).Value= p.point;

                    cmd.ExecuteNonQuery();
                    if (!ret.result)
                    {
                        conn_web.Close();
                        return;
                    }
                }
            }
            catch (Exception e)
            {
                conn_web.Close();

                ret.result = false;
                ret.text = e.Message;
                return;
            }

            conn_web.Close();
        }

        //----------------------------------------------------------------------
        public void LoadAnlSupp(List<AnlSupp> ls, Finder finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);

            ret = OpenDb(conn_web, true);
            if (!ret.result)
            {
                ret.text = "Ошибка открытия БД";
                return;
            }
            string table = finder.database; // "anl" + finder.dopTag + "_supp_" + finder.nzp_server;


            //поготовка Insert'а
            IDbCommand cmd = DBManager.newDbCommand(
                " Insert into __" + table +
                "  (nzp_sp, nzp_area, nzp_geu, nzp_supp, nzp_serv, nzp_frm, area, geu, name_supp, service, name_frm) " +
                " Values (?,?,?,?,?,?,?,?,?,?,?) "
                , conn_web);

            DBManager.addDbCommandParameter(cmd, "nzp_sp",   DbType.Int32);
            DBManager.addDbCommandParameter(cmd, "nzp_area", DbType.Int32);
            DBManager.addDbCommandParameter(cmd, "nzp_geu",  DbType.Int32);
            DBManager.addDbCommandParameter(cmd, "nzp_supp", DbType.Int32);
            DBManager.addDbCommandParameter(cmd, "nzp_serv", DbType.Int32);
            DBManager.addDbCommandParameter(cmd, "nzp_frm",  DbType.Int32);
            DBManager.addDbCommandParameter(cmd, "area",     DbType.String);
            DBManager.addDbCommandParameter(cmd, "geu",      DbType.String);
            DBManager.addDbCommandParameter(cmd, "name_supp",DbType.String);
            DBManager.addDbCommandParameter(cmd, "service",  DbType.String);
            DBManager.addDbCommandParameter(cmd, "name_frm", DbType.String);

            try
            {
                foreach (AnlSupp p in ls)
                {
                    (cmd.Parameters[0] as IDbDataParameter).Value = p.serial_key;
                    (cmd.Parameters[1] as IDbDataParameter).Value = p.nzp_area;
                    (cmd.Parameters[2] as IDbDataParameter).Value = p.nzp_geu;
                    (cmd.Parameters[3] as IDbDataParameter).Value = p.nzp_supp;
                    (cmd.Parameters[4] as IDbDataParameter).Value = p.nzp_serv;
                    (cmd.Parameters[5] as IDbDataParameter).Value = p.nzp_frm;
                    (cmd.Parameters[6] as IDbDataParameter).Value = p.area;
                    (cmd.Parameters[7] as IDbDataParameter).Value = p.geu;
                    (cmd.Parameters[8] as IDbDataParameter).Value = p.name_supp;
                    (cmd.Parameters[9] as IDbDataParameter).Value = p.service;
                    (cmd.Parameters[10] as IDbDataParameter).Value= p.name_frm;

                    cmd.ExecuteNonQuery();
                    if (!ret.result)
                    {
                        conn_web.Close();
                        return;
                    }
                }
            }
            catch (Exception e)
            {
                conn_web.Close();

                ret.result = false;
                ret.text = e.Message;
                return;
            }

            conn_web.Close();
        }


        //----------------------------------------------------------------------
        public void CreateAnlXX(IDbConnection conn_web, string analiz1, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();

            //сначала создадим прообраз таблицы, чтобы застраховаться от одновременного обращения
            ret = ExecSQL(conn_web,
                      " Create table " + analiz1 +
                      " ( nzp_anl  serial not null, " +
                      "   nzp_dom  integer, " +
                      "   nzp_wp   integer, " +
                      "   nzp_area integer, " +
                      "   nzp_geu  integer, " +
                      "   nzp_serv integer, " +
                      "   nzp_supp integer, " +
                      "   nzp_frm  integer  " +
                      " ) " + DBManager.sUnlogTempTable, true);
            if (!ret.result)
            {
                conn_web.Close();
                return;
            }

            uint tabid = TableInWebCasheID(conn_web, analiz1);
            string ix = "ix" + tabid + "_" + analiz1;

            ret = ExecSQL(conn_web, " Create index " + ix + "_1 on " + analiz1 + " (nzp_anl) ", true);
            if (ret.result) { ret = ExecSQL(conn_web, " Create index " + ix + "_2 on " + analiz1 + " (nzp_dom, nzp_area, nzp_geu, nzp_wp, nzp_serv, nzp_supp, nzp_frm) ", true); }
            if (ret.result) { ret = ExecSQL(conn_web, " Create index " + ix + "_3 on " + analiz1 + " (nzp_area) ", true); }
            if (ret.result) { ret = ExecSQL(conn_web, " Create index " + ix + "_4 on " + analiz1 + " (nzp_geu) ", true); }
            if (ret.result) { ret = ExecSQL(conn_web, " Create index " + ix + "_5 on " + analiz1 + " (nzp_supp) ", true); }
            if (ret.result) { ret = ExecSQL(conn_web, " Create index " + ix + "_6 on " + analiz1 + " (nzp_serv) ", true); }
            if (ret.result) { ret = ExecSQL(conn_web, " Create index " + ix + "_7 on " + analiz1 + " (nzp_wp,nzp_area,nzp_geu,nzp_serv,nzp_supp) ", true); }
        }

        //----------------------------------------------------------------------
        public void CreateAnlDom(IDbConnection conn_web, string pXX_spdom, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();

            //создать таблицу webdata:pXX_spdom
            ret = ExecSQL(conn_web,
                      " Create table " + pXX_spdom +
                      " ( nzp_sp     serial not null, " +
                      "   nzp_dom    integer, " +
                      "   nzp_ul     integer, " +
                      "   nzp_area   integer, " +
                      "   nzp_geu    integer, " +
                      "   nzp_wp     integer, " +

                      "   area     char(60)," +
                      "   geu      char(60)," +

                      "   ulica    char(80)," +
                      "   ndom     char(1000)," +
                      "   idom     integer, " +
                      "   pref     char(10)," +
                      "   point    char(100) " +
                      " ) ", true);
            if (!ret.result)
            {
                return;
            }

            uint tabid = TableInWebCasheID(conn_web, pXX_spdom);
            string ix = "ix" + tabid + "_" + pXX_spdom;


            ret = ExecSQL(conn_web, " Create unique index " + ix + "_0 on " + pXX_spdom + " (nzp_sp) ", true);
            if (!ret.result)
                ret = ExecSQL(conn_web, " Create index " + ix + "_1 on " + pXX_spdom + " (nzp_dom,nzp_ul, nzp_area,nzp_geu) ", true);
            if (!ret.result)
                ret = ExecSQL(conn_web, " Create index " + ix + "_2 on " + pXX_spdom + " (nzp_area) ", true);
            if (!ret.result)
                ret = ExecSQL(conn_web, " Create index " + ix + "_3 on " + pXX_spdom + " (nzp_geu) ", true);
            if (!ret.result)
                ret = ExecSQL(conn_web, " Create index " + ix + "_4 on " + pXX_spdom + " (nzp_ul) ", true);
            if (!ret.result)
                ret = ExecSQL(conn_web, " Create index " + ix + "_5 on " + pXX_spdom + " (nzp_wp) ", true);

            if (!ret.result) ret = ExecSQL(conn_web, DBManager.sUpdStat + " " + pXX_spdom, true);
        }

        //----------------------------------------------------------------------
        public void CreateAnlSupp(IDbConnection conn_web, string pXX_spsupp, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();

            //создать таблицу webdata:pXX_spsupp
            ret = ExecSQL(conn_web,
                      " Create table " + pXX_spsupp +
                      " ( nzp_sp     serial not null, " +
                     "    nzp_area   integer, " +
                      "   nzp_geu    integer, " +
                      "   nzp_supp   integer, " +
                      "   nzp_serv   integer, " +
                      "   nzp_frm    integer, " +

                      "   area       char(60)," +
                      "   geu        char(60)," +
                      "   name_supp  char(100)," +
                      "   service    char(100)," +
                      "   name_frm   char(90) " +
                      " ) ", true);
            if (!ret.result)
            {
                return;
            }

            //создаем индексы на pXX_spsupp
            uint tabid = TableInWebCasheID(conn_web, pXX_spsupp);
            string ix = "ix" + tabid + "_" + pXX_spsupp;

            ret = ExecSQL(conn_web, " Create unique index " + ix + "_0 on " + pXX_spsupp + " (nzp_sp) ", true);
            if (!ret.result)
                ret = ExecSQL(conn_web, " Create index " + ix + "_1 on " + pXX_spsupp + " (nzp_supp) ", true);
            if (!ret.result)
                ret = ExecSQL(conn_web, " Create index " + ix + "_2 on " + pXX_spsupp + " (nzp_area,nzp_geu,nzp_supp,nzp_serv,nzp_frm) ", true);
            if (!ret.result)
                ret = ExecSQL(conn_web, " Create index " + ix + "_3 on " + pXX_spsupp + " (nzp_geu) ", true);
            if (!ret.result)
                ret = ExecSQL(conn_web, " Create index " + ix + "_4 on " + pXX_spsupp + " (nzp_serv) ", true);
#if PG
            if (!ret.result)
                ret = ExecSQL(conn_web, " analyze  " + pXX_spsupp, true);
#else
            if (!ret.result)
                ret = ExecSQL(conn_web, " Update statistics for table  " + pXX_spsupp, true);
#endif
        }

        //----------------------------------------------------------------------
        public List<AnlXX> GetAnlXX(Finder finder, out Returns ret) //
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            List<AnlXX> Spis = new List<AnlXX>();

            string table = finder.database; //"anl" + finder.dopTag;

            Spis.Clear();

            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result) return null;

            //кол-во записей
            int cnt = 0;

            IDbCommand cmd = DBManager.newDbCommand(" Select count(*) From " + table, conn_web);

            try
            {
                string s = Convert.ToString(cmd.ExecuteScalar());
                cnt = Convert.ToInt32(s);
            }
            catch (Exception ex)
            {
                conn_web.Close();

                ret.result = false;
                ret.text = ex.Message;

                string err;
                if (Constants.Viewerror)
                    err = " \n " + ex.Message;
                else
                    err = "";

                MonitorLog.WriteLog("Ошибка GetAnlXX " + err, MonitorLog.typelog.Error, 20, 201, true);
                return null;
            }

            string skip = "";
#if PG
            if (finder.skip > 0) skip = " offset " + finder.skip;
#else
                  if (finder.skip > 0) skip = " skip " + finder.skip;
#endif

            //выбрать список
            IDataReader reader;
#if PG
            ret = ExecRead(conn_web, out reader,
                           " Select nzp_anl, nzp_dom, nzp_wp, nzp_area, nzp_geu, nzp_serv, nzp_supp, nzp_frm " +
                           " From " + table + skip, true);
#else
            ret = ExecRead(conn_web, out reader,
                " Select " + skip + " nzp_anl, nzp_dom, nzp_wp, nzp_area, nzp_geu, nzp_serv, nzp_supp, nzp_frm "+
                " From " + table, true);
#endif

            if (!ret.result)
            {
                conn_web.Close();
                return null;
            }
            try
            {
                int i = 0;
                while (reader.Read())
                {
                    i += 1;
                    AnlXX zap = new AnlXX();

                    if (reader["nzp_anl"] != DBNull.Value) zap.serial_key = (int)reader["nzp_anl"];
                    if (reader["nzp_dom"] != DBNull.Value) zap.nzp_dom = (int)reader["nzp_dom"];
                    if (reader["nzp_wp"]  != DBNull.Value) zap.nzp_wp = (int)reader["nzp_wp"];
                    if (reader["nzp_area"]!= DBNull.Value) zap.nzp_area = (int)reader["nzp_area"];
                    if (reader["nzp_geu"] != DBNull.Value) zap.nzp_geu = (int)reader["nzp_geu"];
                    if (reader["nzp_serv"]!= DBNull.Value) zap.nzp_serv = (int)reader["nzp_serv"];
                    if (reader["nzp_supp"]!= DBNull.Value) zap.nzp_supp = (int)reader["nzp_supp"];
                    if (reader["nzp_frm"] != DBNull.Value) zap.nzp_frm = (int)reader["nzp_frm"];

                    Spis.Add(zap);
                    if (i >= finder.rows) break;
                }

                reader.Close();
                conn_web.Close();

                ret.tag = cnt;
                return Spis;
            }
            catch (Exception ex)
            {
                reader.Close();
                conn_web.Close();

                ret.result = false;
                ret.text = ex.Message;

                string err;
                if (Constants.Viewerror)
                    err = " \n " + ex.Message;
                else
                    err = "";

                MonitorLog.WriteLog("Ошибка заполнения GetAnlXX " + err, MonitorLog.typelog.Error, 20, 201, true);

                return null;
            }

        }//GetAnlXX

        //----------------------------------------------------------------------
        public List<AnlDom> GetAnlDom(Finder finder, out Returns ret) //
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            List<AnlDom> Spis = new List<AnlDom>();

            string table = finder.database; // "anl" + finder.dopTag + "_dom";

            Spis.Clear();

            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result) return null;

            //кол-во записей
            int cnt = 0;
            IDbCommand cmd = DBManager.newDbCommand(" Select count(*) From " + table, conn_web);
            try
            {
                string s = Convert.ToString(cmd.ExecuteScalar());
                cnt = Convert.ToInt32(s);
            }
            catch (Exception ex)
            {
                conn_web.Close();

                ret.result = false;
                ret.text = ex.Message;

                string err;
                if (Constants.Viewerror)
                    err = " \n " + ex.Message;
                else
                    err = "";

                MonitorLog.WriteLog("Ошибка GetAnlDom " + err, MonitorLog.typelog.Error, 20, 201, true);
                return null;
            }
            string skip = "";

#if PG
            if (finder.skip > 0) skip = " offset " + finder.skip;
#else
                  if (finder.skip > 0) skip = " skip " + finder.skip;
#endif

            //выбрать список
            IDataReader reader;
#if PG
            ret = ExecRead(conn_web, out reader,
                            " Select nzp_sp, nzp_dom, nzp_ul, nzp_area, nzp_geu, nzp_wp, area, geu, ulica, ndom, idom, pref, point " +
                            " From " + table + skip, true);
#else
ret = ExecRead(conn_web, out reader,
                " Select  " + skip + "  nzp_sp, nzp_dom, nzp_ul, nzp_area, nzp_geu, nzp_wp, area, geu, ulica, ndom, idom, pref, point "+
                " From " + table, true);
#endif
            if (!ret.result)
            {
                conn_web.Close();
                return null;
            }
            try
            {
                int i= 0;
                while (reader.Read())
                {
                    i += 1;
                    AnlDom zap = new AnlDom();

                    if (reader["nzp_sp"]  != DBNull.Value) zap.serial_key = (int)reader["nzp_sp"];
                    if (reader["nzp_dom"] != DBNull.Value) zap.nzp_dom = (int)reader["nzp_dom"];
                    if (reader["nzp_ul"]  != DBNull.Value) zap.nzp_ul = (int)reader["nzp_ul"];
                    if (reader["nzp_area"]!= DBNull.Value) zap.nzp_area = (int)reader["nzp_area"];
                    if (reader["nzp_geu"] != DBNull.Value) zap.nzp_geu = (int)reader["nzp_geu"];
                    if (reader["nzp_wp"]  != DBNull.Value) zap.nzp_wp = (int)reader["nzp_wp"];

                    if (reader["area"]    != DBNull.Value) zap.area = (string)reader["area"];
                    if (reader["geu"]     != DBNull.Value) zap.geu = (string)reader["geu"];
                    if (reader["ulica"]   != DBNull.Value) zap.ulica = (string)reader["ulica"];
                    if (reader["ndom"]    != DBNull.Value) zap.ndom = (string)reader["ndom"];
                    if (reader["idom"]    != DBNull.Value) zap.idom = (int)reader["idom"];

                    if (reader["pref"]    != DBNull.Value) zap.pref = (string)reader["pref"];
                    if (reader["point"]   != DBNull.Value) zap.point = (string)reader["point"];

                    Spis.Add(zap);
                    if (i >= finder.rows) break;
                }

                reader.Close();
                conn_web.Close();

                ret.tag = cnt;
                return Spis;
            }
            catch (Exception ex)
            {
                reader.Close();
                conn_web.Close();

                ret.result = false;
                ret.text = ex.Message;

                string err;
                if (Constants.Viewerror)
                    err = " \n " + ex.Message;
                else
                    err = "";

                MonitorLog.WriteLog("Ошибка заполнения GetAnlDom " + err, MonitorLog.typelog.Error, 20, 201, true);

                return null;
            }

        }//GetAnlDom

        //----------------------------------------------------------------------
        public List<AnlSupp> GetAnlSupp(Finder finder, out Returns ret) //
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            List<AnlSupp> Spis = new List<AnlSupp>();

            string table = finder.database; // "anl" + finder.dopTag + "_supp";

            Spis.Clear();

            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result) return null;

            //кол-во записей
            int cnt = 0;
#if PG
            IDbCommand cmd = DBManager.newDbCommand(" Select count(*) From " + table, conn_web);
#else
            IDbCommand cmd = DBManager.newDbCommand(" Select count(*) From " + table, conn_web);
#endif
            try
            {
                string s = Convert.ToString(cmd.ExecuteScalar());
                cnt = Convert.ToInt32(s);
            }
            catch (Exception ex)
            {
                conn_web.Close();

                ret.result = false;
                ret.text = ex.Message;

                string err;
                if (Constants.Viewerror)
                    err = " \n " + ex.Message;
                else
                    err = "";

                MonitorLog.WriteLog("Ошибка GetAnlSupp " + err, MonitorLog.typelog.Error, 20, 201, true);
                return null;
            }
            string skip = "";

#if PG
            if (finder.skip > 0) skip = " offset " + finder.skip;
#else
                  if (finder.skip > 0) skip = " skip " + finder.skip;
#endif

            //выбрать список
            IDataReader reader;
#if PG
            ret = ExecRead(conn_web, out reader,
                           " Select nzp_sp, nzp_area, nzp_geu, nzp_supp, nzp_serv, nzp_frm, area, geu, name_supp, service, name_frm " +
                           " From " + table + skip, true);
#else
 ret = ExecRead(conn_web, out reader,
                " Select " + skip + " nzp_sp, nzp_area, nzp_geu, nzp_supp, nzp_serv, nzp_frm, area, geu, name_supp, service, name_frm "+
                " From " + table, true);
#endif
            if (!ret.result)
            {
                conn_web.Close();
                return null;
            }
            try
            {
                int i = 0;
                while (reader.Read())
                {
                    i += 1;
                    AnlSupp zap = new AnlSupp();

                    if (reader["nzp_sp"]   != DBNull.Value) zap.serial_key = (int)reader["nzp_sp"];
                    if (reader["nzp_area"] != DBNull.Value) zap.nzp_area = (int)reader["nzp_area"];
                    if (reader["nzp_geu"]  != DBNull.Value) zap.nzp_geu = (int)reader["nzp_geu"];

                    if (reader["nzp_supp"] != DBNull.Value) zap.nzp_supp = (int)reader["nzp_supp"];
                    if (reader["nzp_serv"] != DBNull.Value) zap.nzp_serv = (int)reader["nzp_serv"];
                    if (reader["nzp_frm"]  != DBNull.Value) zap.nzp_frm = (int)reader["nzp_frm"];

                    if (reader["area"]     != DBNull.Value) zap.area = (string)reader["area"];
                    if (reader["geu"]      != DBNull.Value) zap.geu = (string)reader["geu"];
                    if (reader["name_supp"]!= DBNull.Value) zap.name_supp = (string)reader["name_supp"];
                    if (reader["service"]  != DBNull.Value) zap.service = (string)reader["service"];
                    if (reader["name_frm"] != DBNull.Value) zap.name_frm = (string)reader["name_frm"];

                    Spis.Add(zap);
                    if (i >= finder.rows) break;
                }

                reader.Close();
                conn_web.Close();

                ret.tag = cnt;
                return Spis;
            }
            catch (Exception ex)
            {
                reader.Close();
                conn_web.Close();

                ret.result = false;
                ret.text = ex.Message;

                string err;
                if (Constants.Viewerror)
                    err = " \n " + ex.Message;
                else
                    err = "";

                MonitorLog.WriteLog("Ошибка заполнения GetAnlSupp " + err, MonitorLog.typelog.Error, 20, 201, true);

                return null;
            }

        }//GetAnlDom

        //----------------------------------------------------------------------
        public bool Drop__AnlTables(IDbConnection conn_web) //
        //----------------------------------------------------------------------
        {
            IDataReader reader;
            if (!ExecRead(conn_web, out reader,
#if PG
                " Select table_name as tabname From information_schema.tables " +
                " Where lower(table_name) like '\\_\\_%'", true).result)
#else
                " Select tabname From systables " +
                " Where tabname matches '__*' and tabid > 100", true).result)
#endif
            {
                return false;
            }
            try
            {
                while (reader.Read())
                {
                    string sql = "";
                    if (reader["tabname"] != DBNull.Value)
                    {
                        sql = " Drop table " + (string)reader["tabname"];

                        ExecSQL(conn_web, sql.Trim(), false);
                    }
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка удаления временных таблиц \n " + ex.Message, MonitorLog.typelog.Error, 20, 201, true);
                return false;
            }
            finally
            {
                reader.Close();
                reader.Dispose();
            }
            return true;
        }
    }

}