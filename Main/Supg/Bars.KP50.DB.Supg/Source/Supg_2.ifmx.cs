using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using STCLINE.KP50.Interfaces;
using STCLINE.KP50.Global;
using STCLINE.KP50.DataBase;
using System.Collections;
using System.Data;

namespace STCLINE.KP50.DataBase
{
    //----------------------------------------------------------------------
    public partial class Supg : DataBaseHead
    //----------------------------------------------------------------------
    {

        /// <summary>
        /// процедура генерации sql запроса для поиска СУПГ
        /// </summary>
        /// <param name="finder"></param>
        /// <returns></returns>
        public string MakeWhereStringGroup(SupgFinder finder, out Returns ret)
        {
            string sql = "";
            string sql2 = "";
            string sql3 = "";
            string sql4 = "";
            string where = "";
            string areas = "";
            string supp = "";
            string dop = "";
            ret = Utils.InitReturns();
            //соединение с БД
            //if (finder.RolesVal != null)
            //{
            //    if (finder.RolesVal.Count > 0)
            //    {
            //        foreach (_RolesVal role in finder.RolesVal)
            //        {
            //            if (role.tip == Constants.role_sql & role.kod == Constants.role_sql_supp)
            //                sql3 += " and nzp_status <> 1 and a.nzp_supp in (" + role.val + ") ";

            //            if (role.tip == Constants.role_sql & role.kod == Constants.role_sql_area)
            //                areas += " and k.nzp_area in (" + role.val + ") ";
            //        }
            //    }
            //}


            #region роли

            BaseUser.OrganizationTypes types = finder.organization;

            switch (types)
            {
                case BaseUser.OrganizationTypes.DispatchingOffice:
                    {
                        dop = " and z.nzp_payer = " + finder.nzp_disp;
                        break;
                    }
                case BaseUser.OrganizationTypes.UK:
                    {
                        areas = " and (z.nzp_payer = " + finder.nzp_payer + " or (z.nzp_payer = " + finder.nzp_disp + " and " + " k.nzp_area = " + finder.nzp_area + "))";
                        break;
                    }
                case BaseUser.OrganizationTypes.Supplier:
                    {
                        supp = " and ((zk.nzp_status <> 1 and " +
                              " a.nzp_supp=" + finder.nzp_supp + ") or " +
                              " z.nzp_payer=" + finder.nzp_payer + ")";
                        break;
                    }
            }

            #endregion

            if (Utils.GetParams(finder.prms, Constants.page_spisls))
            {
#if PG
                sql = "Select count(*) from " + Points.Pref + "_supg.zvk z";
                where = " Where z.nzp_kvar = k.nzp_kvar " + areas;
#else
                sql = "Select count(*) from " + Points.Pref + "_supg:zvk z";
                where = " Where z.nzp_kvar = k.nzp_kvar " + areas;
#endif
                if (finder.nzp_zvk > 0) where += " and z.nzp_zvk = " + finder.nzp_zvk;
                if (finder.zvk_date_from != "") where += " and z.zvk_date >= " + Utils.EStrNull(ConvertToDateSS(finder.zvk_date_from, 0)) + " ";
                if (finder.zvk_date_to != "") where += " and z.zvk_date < " + Utils.EStrNull(ConvertToDateSS(finder.zvk_date_to, 1)) + " ";
                if (finder.zvk_ztype > 0) where += " and z.nzp_ztype = " + finder.zvk_ztype + " ";
                if (finder.zvk_exec_date_from != "") where += " and z.exec_date >= " + Utils.EStrNull(finder.zvk_exec_date_from) + " ";
                if (finder.zvk_exec_date_to != "") where += " and z.exec_date <= " + Utils.EStrNull(finder.zvk_exec_date_to) + " ";
                if (finder.zvk_fact_date_from != "") where += " and z.fact_date >= " + Utils.EStrNull(finder.zvk_fact_date_from) + " ";
                if (finder.zvk_fact_date_to != "") where += " and z.fact_date <= " + Utils.EStrNull(finder.zvk_fact_date_to) + " ";
                if (finder.zvk_res > 0) where += " and z.nzp_res = " + finder.zvk_res;
#if PG
                if (finder.zvk_result_comment != "") where += " and z.result_comment like \'*" + finder.zvk_result_comment + "*\' ";
                if (finder.zvk_demand_name != "") where += " and z.demand_name like \'*" + finder.zvk_demand_name + "*\' ";
#else
                if (finder.zvk_result_comment != "") where += " and z.result_comment matches \'*" + finder.zvk_result_comment + "*\' ";
                if (finder.zvk_demand_name != "") where += " and z.demand_name matches \'*" + finder.zvk_demand_name + "*\' ";
#endif

                if (finder.phone != "") where += " and z.phone = \'" + finder.phone + "\' ";

                if (finder._date_from != "") sql2 += " and r._date >= " + Utils.EStrNull(ConvertToDateSS(finder._date_from, 0)) + " ";
                if (finder._date_to != "") sql2 += " and r._date < " + Utils.EStrNull(ConvertToDateSS(finder._date_to, 1)) + " ";
                if (finder.nzp_slug > 0) sql2 += " and r.nzp_slug = " + finder.nzp_slug + " ";

                if (finder.nzp_zk > 0) sql3 += " and a.nzp_zk = " + finder.nzp_zk + " ";
                if (finder.zk_order_date_from != "") sql3 += " and a.order_date >= " + Utils.EStrNull(ConvertToDateSS(finder.zk_order_date_from, 0)) + " ";
                if (finder.zk_order_date_to != "") sql3 += " and a.order_date < " + Utils.EStrNull(ConvertToDateSS(finder.zk_order_date_to, 1)) + " ";

                if (finder.plan_date_from != "") sql3 += " and a.plan_date >= " + Utils.EStrNull(ConvertToDateSS(finder.plan_date_from, 0)) + " ";//new
                if (finder.plan_date_to != "") sql3 += " and a.plan_date < " + Utils.EStrNull(ConvertToDateSS(finder.plan_date_to, 1)) + " ";//new

                if (finder.control_date_from != "") sql3 += " and a.control_date >= " + Utils.EStrNull(ConvertToDateSS(finder.control_date_from, 0)) + " ";//new
                if (finder.control_date_to != "") sql3 += " and a.control_date < " + Utils.EStrNull(ConvertToDateSS(finder.control_date_to, 1)) + " ";//new

                if (finder.zk_fact_date_from != "") sql3 += " and a.fact_date >= " + Utils.EStrNull(ConvertToDateHH(finder.zk_fact_date_from, 0)) + " ";
                if (finder.zk_fact_date_to != "") sql3 += " and a.fact_date < " + Utils.EStrNull(ConvertToDateHH(finder.zk_fact_date_to, 1)) + " ";

                //if (finder.zk_plan_date_from != "") sql3 += " and a.plan_date >= " + Utils.EStrNull(ConvertToDateSS(finder.zk_plan_date_from, 0)) + " ";
                //if (finder.zk_plan_date_to != "") sql3 += " and a.plan_date < " + Utils.EStrNull(ConvertToDateSS(finder.zk_plan_date_to, 1)) + " ";

                //if (finder.zk_mail_date_from != "") sql3 += " and a.mail_date >= " + Utils.EStrNull(ConvertToDateSS(finder.zk_mail_date_from, 0)) + " ";
                //if (finder.zk_mail_date_to != "") sql3 += " and a.mail_date < " + Utils.EStrNull(ConvertToDateSS(finder.zk_mail_date_to, 1)) + " ";

                //if (finder.zk_accept_date_from != "") sql3 += " and a.accept_date >= " + Utils.EStrNull(ConvertToDateSS(finder.zk_control_date_from, 0)) + " ";
                //if (finder.zk_accept_date_to != "") sql3 += " and a.accept_date < " + Utils.EStrNull(ConvertToDateSS(finder.zk_accept_date_to, 1)) + " ";
                
                //if (finder.zk_control_date_from != "") sql3 += " and a.control_date >= " + Utils.EStrNull(ConvertToDateSS(finder.zk_control_date_from, 0)) + " ";
                //if (finder.zk_control_date_to != "") sql3 += " and a.control_date < " + Utils.EStrNull(ConvertToDateSS(finder.zk_control_date_to, 1)) + " ";

                if (finder.nzp_status > 0) sql3 += " and a.nzp_status = " + finder.nzp_status + " ";

                if (finder.zk_nzp_supp > 0) sql3 += " and a.nzp_supp = " + finder.zk_nzp_supp + " ";

                if (finder.nzp_dest > 0) sql3 += " and a.nzp_dest = " + finder.nzp_dest + " ";
                if (finder.nzp_serv > 0 && finder.nzp_dest == 0) sql4 += " and s.nzp_serv = " + finder.nzp_serv + " ";

                if (finder.nzp_res > 0) sql3 += " and a.nzp_res = " + finder.nzp_res + " ";
                if (finder.nzp_atts > 0) sql3 += " and a.nzp_atts = " + finder.nzp_atts + " ";

                if (finder.repeated > -1) sql3 += " and a.repeated = " + finder.repeated + " ";
                if (finder.is_replicate > -1) sql3 += " and a.is_replicate = " + finder.is_replicate + " ";

#if PG
 if (sql2 != "")
                {
                    sql += ", " + Points.Pref + "_supg. readdress r ";
                    where += " and z.nzp_zvk = r.nzp_zvk " + sql2;
                }
                if (sql3 != "" || sql4 != "")
                {
                    sql += ", " + Points.Pref + "_supg. zakaz a ";
                    where += " and z.nzp_zvk = a.nzp_zvk  " + sql3;
                }
                if (sql4 != "")
                {
                    sql += ", " + Points.Pref + "_supg. s_dest s ";
                    where += " and a.nzp_dest = s.nzp_dest  " + sql4;
                }
                sql += where;
#else
                if (sql2 != "")
                {
                    sql += ", " + Points.Pref + "_supg: readdress r ";
                    where += " and z.nzp_zvk = r.nzp_zvk " + sql2;
                }
                if (sql3 != "" || sql4 != "")
                {
                    sql += ", " + Points.Pref + "_supg: zakaz a ";
                    where += " and z.nzp_zvk = a.nzp_zvk  " + sql3;
                }
                if (sql4 != "")
                {
                    sql += ", " + Points.Pref + "_supg: s_dest s ";
                    where += " and a.nzp_dest = s.nzp_dest  " + sql4;
                }
                sql += where;
#endif

            }
            return sql;
        }

        public static string ConvertToDateSS(string str, int check)
        {
            string retstr = "";
            DateTime time;
            DateTime.TryParse(str, out time);
            if (check != 0)
                time = time.AddDays(1);
            retstr = time.ToString("yyyy-MM-dd HH:mm:ss");
            return retstr;
        }

        public static string ConvertToDateHH(string str, int check)
        {
            string retstr = "";
            DateTime time;
            DateTime.TryParse(str, out time);
            if (check != 0)
                time = time.AddDays(1);
            retstr = time.ToString("yyyy-MM-dd HH");
            return retstr;
        }



        /// <summary>
        /// формирование результата поиска заявок
        /// </summary>
        /// <param name="nzp_user">пользователь</param>
        /// <param name="finder">контейнер</param>
        /// <param name="ret"></param>
        /// <returns>1 - успех</returns>
        public int FindZvk(SupgFinder finder, out Returns ret)
        {
            int flag = 0; //рисовать или нет наряды заказов
            string spls_from = ""; //для учитывания шаблона поиска лиц счетов
            string spls_where = ""; //для учитывания шаблона поиска лиц счетов
            string sql0 = "";
            IDbConnection conn_web = null;
            IDbConnection conn_db = null;
            ret = Utils.InitReturns();

            string tXX_supg = "t" + finder.nzp_user + "_supg"; //для поиска по шаблону поиска
            if (finder.flag_disp == 1)
            {
                tXX_supg = "t" + finder.nzp_user + "_disp"; //для необработанных заявок
            }

            if (finder.flag_disp == 2)
            {
                tXX_supg = "t" + finder.nzp_user + "_zakaz"; //для списка нарядов-заказов на выполнение
                if (finder.flag_survey == 1)
                {
                    tXX_supg = "t" + finder.nzp_user + "_zakazo"; //для списка нарядов-заказов для опроса
                }

            }

            //соединение с БД
            conn_web = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result)
            {
                MonitorLog.WriteLog("Ошибка при открытии соединения с БД conn_web в FindZvk", MonitorLog.typelog.Error, true);
                conn_web.Close();
                return -1;
            }

            //соединение с БД
            conn_db = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result)
            {
                MonitorLog.WriteLog("Ошибка при открытии соединения с БД conn_db в FindZvk", MonitorLog.typelog.Error, true);
                conn_db.Close();
                return -1;
            }


            string path_to_public = "";
            string table_pref;
#if PG
            path_to_public = "SET search_path to public; ";
            table_pref = "public" + tableDelimiter;
#else
            table_pref = conn_web.Database + "@" + DBManager.getServer(conn_web) + tableDelimiter;
#endif

            #region Удаление существующей таблицы

            if (TableInWebCashe(conn_web, tXX_supg))
            {
                ExecSQL(conn_web, path_to_public + " Drop table " + tXX_supg, false);
            }
            #endregion

            #region Создание таблицы

            try
            {
                ret = ExecSQL(conn_web, 
                    path_to_public + 
                    " Create table   " + tXX_supg +
                    " ( nzp          serial not null, " +
                    "   pref         char(30), " +
                    "   nzp_kvar     integer, " +
                    "   nzp_dom      integer, " +
                    "   nzp_zvk      integer not null , " +
                    "   zvk_date     " + sDateTimeType + ", " +
                    "   comment      TEXT, " +
                    "   adr          char(160), " +
                    "   res_name     char(255), " +
                    "   nzp_zk       integer, " +
                    "   norm         integer, " +
                    "   order_date   " + sDateTimeType + "," +
                    "   service      char(100), " +
                    "   res_zakaz    char(30), " +
                    "   mark         integer default 1," +
                    "   nzp_supp     integer,          " +
                    "   ds_actual    integer,           " +
                    "   fact_date    " + sDateTimeType + ", " +
                    "   result       char(30) " +
                    " ) ", true);
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка при создании таблицы supg" + ex.Message, MonitorLog.typelog.Error, true);
                return -1;
            }

            if (ret.result)
            {
                try
                {
                    ret = ExecSQL(conn_web,
                        " Create index x_" + tXX_supg + "_zk on " + tXX_supg + " (nzp_zk) ", true);
                    ret = ExecSQL(conn_web,
                        " Create index x_" + tXX_supg + "_zvk on " + tXX_supg + " (nzp_zvk) ", true);
                }
                catch (Exception ex)
                {
                    MonitorLog.WriteLog(
                        "Ошибка при создании индекса index x_" + tXX_supg + "_pref для таблицы " + tXX_supg + " :" +
                        ex.Message, MonitorLog.typelog.Error, true);
                    return -1;
                }
            }

            #endregion


            if (finder.nzp_user < 1)
            {
                ret.result = false;
                ret.text = "Не определен пользователь";
                ret.tag = -1;
                return -2;
            }
            //Проверка организации
            if (finder.nzp_payer == 0)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Не определена организация пользователя!";
                return -2;
            }

            #region учитываем шаблон поиска лиц счетов

            if (Utils.GetParams(finder.prms, Constants.act_findls))
            {
                spls_from = ", " + Points.Pref + "_data"+tableDelimiter+"kvar k, " + Points.Pref + "_data" + tableDelimiter + "dom d, " + 
                            Points.Pref + sDataAliasRest + "s_ulica u , " + Points.Pref + sDataAliasRest + "s_rajon r ," + Points.Pref + sDataAliasRest + "s_town t ";
              
                spls_where = " and z.nzp_kvar=k.nzp_kvar  and k.nzp_dom = d.nzp_dom and k.nzp_wp is not null and u.nzp_ul = d.nzp_ul and r.nzp_raj = u.nzp_raj and r.nzp_town=t.nzp_town ";
            
                var listPoint = new List<int>();
                var wherePointList = ""; // условие для банков
                var listGeu = new List<int>();
                var whereGeuList = ""; // условие для ЖЭУ
                var listArea = new List<int>();
                var whereAreaList = "";// условие для УК
             
                decimal d = 0;
                Decimal.TryParse(finder.pkod.Trim(), out d);

                var whereString = " and d.nzp_dom = -1111 "; //чтобы ничего не выбиралось

               
                if (finder.num_ls > Constants._ZERO_) whereString = " and k.num_ls = " + finder.num_ls; // Указан ЛС
                else if (finder.pkod != "" && d != 0) // Указан платежный код
                {
                    if (!GlobalSettings.NewGeneratePkodMode) whereString = " and k.pkod = " + finder.pkod;
                }
                else if (finder.pkod10 > 0 && !Points.IsSmr) whereString = " and k.pkod10 = " + finder.pkod10;
                else
                {
                    var swhere = new StringBuilder();
                    int i;
                    // Для нулевого платежного кода  разрешается применять дополнительные фильтры
                    if (finder.pkod != "" && d == 0) swhere.Append(" and  " + sNvlWord + "(k.pkod,0) = " + finder.pkod);
                    if (finder.pkod10 > 0 && Points.IsSmr) swhere.Append(" and k.pkod10 = " + finder.pkod10);
                    if (finder.typek > 0) swhere.Append(" and k.typek = " + finder.typek);
                    if (finder.uch.Trim() != "") swhere.Append(" and k.uch = " + Convert.ToInt32(finder.uch));

                    // Формирование условий для УК
                    if (finder.list_nzp_area != null && finder.list_nzp_area.Count > 0)
                    {
                        listArea.AddRange(finder.list_nzp_area);
                        whereAreaList = " and k.nzp_area in (" + String.Join(",", finder.list_nzp_area) + ")";
                    }
                    else if (finder.nzp_area > 0)
                    {
                        whereAreaList = " and k.nzp_area = " + finder.nzp_area;
                        listArea.Add(finder.nzp_area);
                    }

                    // формирование уловий для ЖЭУ
                    if (finder.nzp_geu > 0)
                    {
                        whereGeuList = " and k.nzp_geu = " + finder.nzp_geu;
                        listGeu.Add(finder.nzp_geu);
                    }

                    if (finder.nzp_town > 0) swhere.Append(" and t.nzp_town = " + finder.nzp_town);
                    if (finder.nzp_raj > 0) swhere.Append(" and r.nzp_raj = " + finder.nzp_raj);
                    if (finder.nzp_ul > 0) swhere.Append(" and u.nzp_ul = " + finder.nzp_ul);
                   
                    //  Указан дом
                    if (finder.nzp_dom > 0) swhere.Append(" and k.nzp_dom = " + finder.nzp_dom);
                    else
                    {
                        if (finder.ndom_po != "")
                        {
                            i = Utils.GetInt(finder.ndom_po);
                            if (i > 0) swhere.Append(" and d.idom <= " + i);

                            i = Utils.GetInt(finder.ndom);
                            if (i > 0) swhere.Append(" and d.idom >= " + i);
                        }
                        else if (finder.ndom != "") swhere.Append(" and upper(d.ndom) = " + Utils.EStrNull(finder.ndom.ToUpper()));
                    }
                    
                    if (finder.nkor != "") swhere.Append(" and upper(d.nkor) = " + Utils.EStrNull(finder.nkor.ToUpper()));//корпус

                    // Указана квартира
                    if (finder.nzp_kvar > 0) swhere.Append(" and k.nzp_kvar = " + finder.nzp_kvar);
                    else
                    {
                        if (finder.stateID > 0) swhere.Append(" and cast(k.is_open as integer) = " + finder.stateID);
                        else if (finder.stateIDs != null && finder.stateIDs.Count > 0) swhere.Append(" and cast(k.is_open as integer) in (" + String.Join(",", finder.stateIDs) + ")");
                        else swhere.Append(" and k.is_open in ('" + Ls.States.Open.GetHashCode() + "','" + Ls.States.Closed.GetHashCode() + "')");

                        if (finder.nkvar_po != "")
                        {
                            i = Utils.GetInt(finder.nkvar_po);
                            if (i > 0) swhere.Append(" and k.ikvar <= " + i);

                            i = Utils.GetInt(finder.nkvar);
                            if (i > 0) swhere.Append(" and k.ikvar >= " + i);
                        }
                        else if (finder.nkvar != "") swhere.Append(" and k.nkvar = " + Utils.EStrNull(finder.nkvar));
                    }

                    if (finder.fio != "") swhere.Append(" and upper(k.fio) like '%" + finder.fio.ToUpper() + "%'");
                    if (finder.nkvar_n != "") swhere.Append(" and upper(k.nkvar_n)= '" + finder.nkvar_n.ToUpper().Trim() + "'");
                    if (finder.phone != "") swhere.Append(" and upper(k.phone) like '%" + finder.phone.ToUpper() + "%'");
                    if (finder.porch != "") swhere.Append(" and k.porch=" + finder.porch.Trim());

                    if (!String.IsNullOrWhiteSpace(finder.remark)) swhere.Append(" and lower(k.remark) like lower('%" + finder.remark.Trim() + "%')");

                    whereString = swhere.ToString();

                    // Формирование условий для банков
                    // Выбран  один банк
                    if (finder.nzp_wp > 0)
                    {
                        listPoint.Add(finder.nzp_wp);
                        wherePointList = " and k.nzp_wp=" + finder.nzp_wp;
                    }
                    // задано несколько банков
                    else if (finder.dopPointList != null && finder.dopPointList.Count > 0)
                    {
                        listPoint.AddRange(finder.dopPointList);
                        wherePointList = " and k.nzp_wp in (" + String.Join(",", finder.dopPointList) + ")";
                    }
                }

                // ограничения по ролям
                if (finder.RolesVal != null)
                {
                    foreach (_RolesVal role in finder.RolesVal.Where(role => role.tip == Constants.role_sql))
                    {
                        switch (role.kod)
                        {
                            case Constants.role_sql_area:
                                whereAreaList = getAvaliableRolesVal(role.val, listArea, "k.nzp_area"); 
                                break;
                            case Constants.role_sql_wp:
                                wherePointList = getAvaliableRolesVal(role.val, listPoint, "k.nzp_wp");
                                break;
                            case Constants.role_sql_geu:
                                whereGeuList = getAvaliableRolesVal(role.val, listGeu, "k.nzp_geu"); 
                                break;
                        }
                    }
                }

                // формирование базового условия основного запроса
                whereString += whereAreaList + whereGeuList + wherePointList;
                spls_where += whereString;
            }

            if (finder.nzp_kvar != 0)
            {
                spls_where = " and z.nzp_kvar = " + finder.nzp_kvar;
            }

            #endregion

            #region роли

            string supp = "";
            string where = "";
            string areas = "";
            string dop = "";

            BaseUser.OrganizationTypes types = finder.organization;

            bool filter_by_kvar = false;

            switch (types)
            {
                case BaseUser.OrganizationTypes.DispatchingOffice:
                {
                    dop = " and z.nzp_payer = " + finder.nzp_disp;
                    break;
                }
                case BaseUser.OrganizationTypes.UK:
                {
                    if (finder.flag_disp != 1)
                    {
                        filter_by_kvar = true;
                        areas = " and (z.nzp_payer = " + finder.nzp_payer + " or (z.nzp_payer = " + finder.nzp_disp +
                                " and " + " udk.nzp_area = " + finder.nzp_area + "))";
                    }
                    else
                    {
                        areas = " and z.nzp_payer = " + finder.nzp_payer;
                    }
                    break;
                }
                case BaseUser.OrganizationTypes.Supplier:
                {
                    if (finder.flag_disp != 1)
                    {
                        supp = " and ((zk.nzp_status <> 1 and " +
                               " zk.nzp_supp=" + finder.nzp_supp + ") or " +
                               " z.nzp_payer=" + finder.nzp_payer + ")";
                    }
                    else
                    {
                        where = " and z.nzp_payer=" + finder.nzp_payer;
                    }
                    break;
                }
            }

            #endregion


            #region для опроса

            string survey = " and zk.nzp_status in (2,3) and zk.nzp_res <> 5 ";

            #endregion


            #region sql_z
            string sql_z = "";
            if (finder.nzp_zvk > 0) sql_z += " and z.nzp_zvk = " + finder.nzp_zvk;
            if (finder.zvk_date_from != "") sql_z += " and z.zvk_date >= " + Utils.EStrNull(ConvertToDateSS(finder.zvk_date_from, 0)) + " ";
            if (finder.zvk_date_to != "") sql_z += " and z.zvk_date < " + Utils.EStrNull(ConvertToDateSS(finder.zvk_date_to, 1)) + " ";
            if (finder.zvk_ztype > 0) sql_z += " and z.nzp_ztype = " + finder.zvk_ztype + " ";
            if (finder.zvk_exec_date_from != "") sql_z += " and z.exec_date >= " + Utils.EStrNull(finder.zvk_exec_date_from) + " ";
            if (finder.zvk_exec_date_to != "") sql_z += " and z.exec_date <= " + Utils.EStrNull(finder.zvk_exec_date_to) + " ";
            if (finder.zvk_fact_date_from != "") sql_z += " and z.fact_date >= " + Utils.EStrNull(finder.zvk_fact_date_from) + " ";
            if (finder.zvk_fact_date_to != "") sql_z += " and z.fact_date <= " + Utils.EStrNull(finder.zvk_fact_date_to) + " ";
            if (finder.zvk_res > 0) sql_z += " and z.nzp_res = " + finder.zvk_res;
            if (finder.zvk_result_comment != "") sql_z += " and z.result_comment like \'%" + finder.zvk_result_comment + "%\' ";
            if (finder.zvk_demand_name != "") sql_z += " and z.demand_name like \'%" + finder.zvk_demand_name + "%\' ";
            if (finder.registration != null && finder.registration != "") sql_z += " and z.nzp_payer in (" + finder.registration + ") ";
            if (finder.phone != "") where += " and z.phone = \'" + finder.phone + "\' ";
            #endregion

            #region sql_r
            string sql_r = "";
            if (finder._date_from != "") sql_r += " and r._date >= " + Utils.EStrNull(ConvertToDateSS(finder._date_from, 0)) + " ";
            if (finder._date_to != "") sql_r += " and r._date < " + Utils.EStrNull(ConvertToDateSS(finder._date_to, 1)) + " ";
            if (finder.nzp_slug > 0) sql_r += " and r.nzp_slug = " + finder.nzp_slug + " ";
            #endregion

            if (sql_r != "")
            {
                sql_z += " and z.nzp_zvk in (select r.nzp_zvk from " + Points.Pref + sSupgAliasRest +
                         ".readdress r where 1=1 " + sql_r + " ) ";
            }

            #region sql_zk
            string sql_zk = supp;
            if (finder.nzp_zk > 0) sql_zk += " and zk.nzp_zk = " + finder.nzp_zk + " ";
            if (finder.zk_order_date_from != "") sql_zk += " and zk.order_date >= " + Utils.EStrNull(ConvertToDateSS(finder.zk_order_date_from, 0)) + " ";
            if (finder.zk_order_date_to != "") sql_zk += " and zk.order_date < " + Utils.EStrNull(ConvertToDateSS(finder.zk_order_date_to, 1)) + " ";
            if (finder.zk_fact_date_from != "") sql_zk += " and zk.fact_date >= " + Utils.EStrNull(ConvertToDateSS(finder.zk_fact_date_from, 0)) + " ";
            if (finder.zk_fact_date_to != "") sql_zk += " and zk.fact_date < " + Utils.EStrNull(ConvertToDateSS(finder.zk_fact_date_to, 1)) + " ";

            if (finder.plan_date_from != "") sql_zk += " and zk.plan_date >= " + Utils.EStrNull(ConvertToDateSS(finder.plan_date_from, 0)) + " ";//new
            if (finder.plan_date_to != "") sql_zk += " and zk.plan_date < " + Utils.EStrNull(ConvertToDateSS(finder.plan_date_to, 1)) + " "; //new

            if (finder.control_date_from != "") sql_zk += " and zk.control_date >= " + Utils.EStrNull(ConvertToDateSS(finder.control_date_from, 0)) + " "; //new
            if (finder.control_date_to != "")sql_zk += " and zk.control_date < " + Utils.EStrNull(ConvertToDateSS(finder.control_date_to, 1)) + " ";//new
            
            if (finder.nzp_status > 0 && finder.flag_survey != 1) sql_zk += " and zk.nzp_status = " + finder.nzp_status + " ";
            if (finder.nzp_status > 0 && finder.flag_disp == 2) sql_zk += " and zk.nzp_status in (2,3) ";

            if (finder.zk_control_date_from != "") sql_zk += " and zk.control_date >= " + Utils.EStrNull(ConvertToDateSS(finder.zk_control_date_from, 0)) +
                          " ";
            if (finder.zk_control_date_to != "")sql_zk += " and zk.control_date < " + Utils.EStrNull(ConvertToDateSS(finder.zk_control_date_to, 1)) +
                          " ";

            if (finder.zk_nzp_supp > 0) sql_zk += " and zk.nzp_supp = " + finder.zk_nzp_supp + " ";

            if (finder.nzp_dest > 0) sql_zk += " and zk.nzp_dest = " + finder.nzp_dest + " ";
            if (finder.nzp_serv > 0 && finder.nzp_dest == 0) sql_zk += " and ds.nzp_serv = " + finder.nzp_serv + " ";

            if (finder.nzp_res > 0 && finder.flag_survey != 1) sql_zk += " and zk.nzp_res = " + finder.nzp_res + " ";
            if (finder.nzp_atts > 0) sql_zk += " and zk.nzp_atts = " + finder.nzp_atts + " ";

            if (finder.repeated > -1) sql_zk += " and zk.repeated = " + finder.repeated + " ";
            if (finder.is_replicate > -1) sql_zk += " and zk.is_replicate = " + finder.is_replicate + " ";

            if (finder.flag_get_zakaz > 0)
            {
                sql_zk = " and 1 = 1 ";
            }

            if (finder.flag_survey == 1)
            {
                sql_zk += survey;
            }
            #endregion

#if PG
            sql0 = " Insert into " + tXX_supg +
                   " (pref, nzp_kvar, nzp_dom , nzp_zvk, zvk_date, comment, adr, res_name ";
#else
                sql0 = " Insert into " + conn_web.Database + "@" + DBManager.getServer(conn_web) + ":" + tXX_supg +
                       " (pref, nzp_kvar, nzp_dom , nzp_zvk, zvk_date, comment, adr, res_name ";
#endif
            if ((sql_z == "" && sql_zk == "") || sql_zk != "")
            {
                sql0 += ", nzp_zk, norm, order_date, service, res_zakaz, nzp_supp, ds_actual, fact_date, result ";
            }
            sql0 += ") ";

            sql0 += " Select distinct udk.pref as pref ," +
                    "" + sNvlWord + "(udk.nzp_kvar,0) nzp_kvar, " +
                    "" + sNvlWord + "(udk.nzp_dom,0) nzp_dom, " +
                    "z.nzp_zvk, " +
                    "z.zvk_date, " +
                    "z.comment, " +
                    "\'ул.\'||trim(udk.ulica)||" +
                    "(case when udk.ndom is null then \' \' else " +
                    "\' д.\'||trim(udk.ndom)|| " +
                    "(case when udk.nkor=\'-\' then \' \' else  " +
                    "\' корп.\'||trim(udk.nkor) end) end)|| " +
                    "(case when udk.nkvar is null then \' \' else " +
                    "\' кв.\'||trim(udk.nkvar)|| " +
                    "(case when trim(udk.nkvar_n)=\'-\' then \' \' else " +
                    "\' комн.\'||trim(udk.nkvar_n) end) " +
                    "end ) adr, trim(r1.res_name) || \' \' || (case when z.result_comment is null then \' \' else substr(z.result_comment,1,200) end) ";
            if (sql_zk != "" || (sql_z == "" && sql_zk == ""))
            {
                sql0 += " ,r2zkdssv.nzp_zk, " +
                        " r2zkdssv.norm, " +
                        " r2zkdssv.order_date, " +
                        " (case when r2zkdssv.service is null then r2zkdssv.dest_name else r2zkdssv.service end) as service, " +
                        " r2zkdssv.res_name as res_zakaz, " +
                        " r2zkdssv.nzp_supp, (case when r2zkdssv.nzp_res = 5 then r2zkdssv.ds_actual else 0 end) as ds_actual, r2zkdssv.fact_date, r2zkdssv.res_name ";
            }


            #region заполнение временной таблички udk

            string sql = path_to_public + " DROP TABLE udk";
            ret = ExecSQL(conn_web, sql, false);
            sql =
                "SELECT K .pref, K.nzp_area, K .nzp_kvar, K .nzp_dom, u.ulica, d.ndom, d.nkor, K .nkvar, K .nkvar_n" +
#if PG
 " INTO TEMP udk" +
#else
#endif
 " FROM " + Points.Pref + sDataAliasRest + " s_ulica u," +
                Points.Pref + sDataAliasRest + " dom d," +
                Points.Pref + sDataAliasRest + " kvar k " +
                "WHERE K .nzp_dom = d.nzp_dom AND d.nzp_ul = u.nzp_ul"
#if PG
#else
 + " INTO TEMP udk"
#endif
;
            ret = ExecSQL(conn_web, sql, true);
            #endregion

            #region заполнение временной таблички r2zkdssv

            sql = path_to_public + " DROP TABLE r2zkdssv";
            ret = ExecSQL(conn_web, sql, false);
            sql =
                "SELECT zk.nzp_zvk, zk.nzp_zk, zk.norm, zk.order_date, sv.service, ds.dest_name, r2.res_name, zk.nzp_supp, zk.nzp_res, zk.ds_actual, zk.fact_date" +
#if PG
 " INTO TEMP r2zkdssv" +
#else
#endif
 " FROM " + Points.Pref + sSupgAliasRest + " zakaz zk," +
                Points.Pref + sSupgAliasRest + " s_result r2," +
                //Points.Pref + sSupgAliasRest + "zvk z," +
                Points.Pref + sSupgAliasRest + " s_dest ds " +
                " LEFT OUTER JOIN " + Points.Pref + sKernelAliasRest + " services AS sv ON ds.nzp_serv = sv.nzp_serv " +
                " WHERE zk.nzp_res = r2.nzp_res and zk.nzp_dest = ds.nzp_dest " //+ sql_zk + " "
#if PG
#else
                    + " INTO TEMP r2zkdssv"
#endif
;
            ret = ExecSQL(conn_web, sql, true);
            #endregion


            string sql_zk2 = "";
            sql0 += " FROM " + Points.Pref + sSupgAliasRest + "s_result r1 ";

            if (filter_by_kvar)
            {
                sql0 += " , udk ";
                sql_zk2 += " AND z.nzp_kvar = udk.nzp_kvar ";
                sql0 += ", " + Points.Pref + sSupgAliasRest + "zvk z ";
            }
            else
            {
                sql0 += ", " + Points.Pref + sSupgAliasRest + "zvk z ";
                sql0 += " LEFT OUTER JOIN udk ON z.nzp_kvar = udk.nzp_kvar ";
            }



            if (sql_zk != "")
            {
                sql0 += "," + Points.Pref + sSupgAliasRest + " zakaz zk" +
                    ",  r2zkdssv ";
                sql_zk2 += " and r2zkdssv.nzp_zvk = z.nzp_zvk ";
            }

            if (sql_zk == "" && sql_z == "")
            {
                sql0 += " LEFT OUTER JOIN r2zkdssv ON r2zkdssv.nzp_zvk = z.nzp_zvk ";
            }

            sql0 += spls_from;
            sql0 += " WHERE z.nzp_res = r1.nzp_res " + areas + " " + dop + " " + where + sql_zk2;

            sql0 += sql_z;
            sql0 += sql_zk + spls_where;

            if ((sql_z == "" && sql_zk == "") || sql_zk != "")
            {
                flag = 1;
            }

            ret = ExecSQL(conn_web, sql0, true);

            if (!ret.result)
            {
                MonitorLog.WriteLog("Ошибка при заполнении данными " + tXX_supg + " в FindZvk " + ret.text,
                    MonitorLog.typelog.Error, 20, 201, true);
                conn_web.Close();
                return -1;
            }

            if (ret.result)
            {
                try
                {
                    ret = ExecSQL(conn_web, sUpdStat + " " + tXX_supg, true);
                }
                catch (Exception ex)
                {
                    MonitorLog.WriteLog("Ошибка при обновлении статистики в таблице " + tXX_supg + " :" + ex.Message,
                        MonitorLog.typelog.Error, true);
                    return -1;
                }
            }
            ret = SaveFinder(finder, Constants.page_spis_order);

            return flag;
        }

        /// <summary>
        /// Проверяет соответствие заданных пользователем параметров (банков, жэу, УК) значениям RolesVal
        /// </summary>
        /// <param name="roleval"></param>
        /// <param name="listFromUser"></param>
        /// <param name="nameColumn"></param>
        /// <returns></returns>
        private string getAvaliableRolesVal(string roleval, List<int> listFromUser, string nameColumn)
        {
            // Если RolesVal пуст
            if (String.IsNullOrWhiteSpace(roleval))
            {
                return String.Empty;
            }
            // Если список параметров от пользователя пуст
            if (listFromUser == null || listFromUser.Count <= 0)
            {
                // значения формируются из RoleVal
                return " and " + nameColumn + " in (" + roleval + ")";
            }

            string[] arrRolesVal = roleval.Split(',');
            // получаем пересечение данных
            List<int> filteredList = new List<int>();
            foreach (int nzp in listFromUser)
            {
                foreach (var role in arrRolesVal)
                {
                    if (nzp.ToString() != role) continue;
                    filteredList.Add(nzp);
                }
            }
            //если ничего не отфильтровалось
            if (filteredList.Count <= 0)
            {
                return " and " + nameColumn + " in (" + roleval + ")";
            }
            return " and " + nameColumn + " in (" + String.Join(",", filteredList) + ")";
        }


        /// <summary>
        /// Быстрый поиск наряда - заказа
        /// </summary>
        /// <param name="finder">контейнер</param>
        /// <param name="ret">результат</param>
        /// <returns>контейнер с данными по н-з</returns>
        public ZvkFinder FastFindZk(SupgFinder finder, out Returns ret)
        {
            string sql = "";
            ZvkFinder res = null;
            IDataReader reader = null;
            IDbConnection conn_db = null;
            ret = Utils.InitReturns();

            if (finder.nzp_user < 1)
            {
                ret.result = false;
                ret.text = "Не определен пользователь";
                return null;
            }

            //List<_Point> prefixs = new List<_Point>();
            //_Point point = new _Point();
            //if (finder.pref != "")
            //{
            //    point.pref = finder.pref;
            //    prefixs.Add(point);
            //}
            //else
            //{
            //    prefixs = Points.PointList;
            //}

            string areas = "";

            //if (finder.RolesVal != null)
            //{
            //    if (finder.RolesVal.Count > 0)
            //    {
            //        foreach (_RolesVal role in finder.RolesVal)
            //        {
            //            if (role.tip == Constants.role_sql & role.kod == Constants.role_sql_area)
            //                    areas += " and k.nzp_area in (" + role.val + ")";
            //        }
            //    }
            //}

            #region роли

            if (finder.organization == BaseUser.OrganizationTypes.Supplier)
            {
                areas += " and zk.nzp_supp = " + finder.nzp_supp;
            }

            #endregion

            //соединение с БД
            conn_db = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result)
            {
                MonitorLog.WriteLog("Ошибка при открытии соединения с БД в FastFindZvk", MonitorLog.typelog.Error, true);
                conn_db.Close();
                ret.result = false;
                return null;
            }

            //foreach (_Point items in prefixs)
            //{
#if PG
sql = " Select k.pref, " +
                       "k.nzp_kvar, " +
                       "z.nzp_zvk, " +
                       "zk.nzp_zk " +
                       "From " + Points.Pref + "_supg. zakaz zk, " +
                       Points.Pref + "_data. kvar k, " +
                       Points.Pref + "_supg. zvk z " +
                       "Where zk.nzp_zk = " + finder.nzp_zk + " and zk.nzp_zvk = z.nzp_zvk and z.nzp_kvar = k.nzp_kvar " + areas;
#else
            sql = " Select k.pref, " +
                                   "k.nzp_kvar, " +
                                   "z.nzp_zvk, " +
                                   "zk.nzp_zk " +
                                   "From " + Points.Pref + "_supg: zakaz zk, " +
                                   Points.Pref + "_data: kvar k, " +
                                   Points.Pref + "_supg: zvk z " +
                                   "Where zk.nzp_zk = " + finder.nzp_zk + " and zk.nzp_zvk = z.nzp_zvk and z.nzp_kvar = k.nzp_kvar " + areas;
#endif

                if (!ExecRead(conn_db, out reader, sql, true).result)
                {
                    MonitorLog.WriteLog("Ошибка выборки FastFindZk" + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                    ret.result = false;
                    return null;
                }
                if (reader != null)
                {
                    while (reader.Read())
                    {
                        res = new ZvkFinder();
                        if (reader["pref"] != DBNull.Value) res.pref = Convert.ToString(reader["pref"]).Trim();
                        if (reader["nzp_kvar"] != DBNull.Value) res.nzp_kvar = Convert.ToInt32(reader["nzp_kvar"]);
                        if (reader["nzp_zvk"] != DBNull.Value) res.nzp_zvk = Convert.ToInt32(reader["nzp_zvk"]);
                        if (reader["nzp_zk"] != DBNull.Value) res.nzp_zk = Convert.ToString(reader["nzp_zk"]);
                    }
                }
            //    if (res != null)
            //        break;
            //}
            return res;
        }


        /// <summary>
        /// для получения предыдущего и следующего значения
        /// </summary>
        /// <param name="finder"></param>
        /// <param name="ret"></param>
        /// <returns>List<ZvkFinder></returns>
        public ZvkFinder GetCarousel(SupgFinder finder, out Returns ret)
        {
            ZvkFinder res = new ZvkFinder();
            List<string> sql = new List<string>();
            string next_pref = "";
            string prev_pref = "";
            int prev_zvk = 0;
            int next_zvk = 0;
            int next_zk = 0;
            int prev_zk = 0;
            int next_nzp_kvar = 0;
            int prev_nzp_kvar = 0;
            IDataReader reader = null;
            ret = Utils.InitReturns();

            if (finder.nzp_user < 1)
            {
                ret.result = false;
                ret.text = "Не определен пользователь";
                return null;
            }

            IDbConnection conn = null;            

            #region для заявки
            if (finder.nzp_zvk != 0 && !(finder.nzp_zvk !=0 && finder.nzp_zk !=0))
            {
                switch(finder.prms)
                {
                    case "спис.заяв.":
                        {
                            conn = GetConnection(Constants.cons_Webdata);
                            ret = OpenDb(conn, true);
                            if (!ret.result) return null;
#if PG
                            sql.Add("Select nzp_zvk, pref, nzp_kvar " +
                                       "From public.t" + finder.nzp_user + "_supg Where nzp_zvk < " + finder.nzp_zvk + " ORDER BY nzp_zvk DESC limit 1;");
                            sql.Add("Select pref, nzp_zvk, nzp_kvar " +
                                       "From public.t" + finder.nzp_user + "_supg Where nzp_zvk > " + finder.nzp_zvk + " ORDER BY nzp_zvk limit 1;");
#else
                            sql.Add("Select first 1 nzp_zvk, pref, nzp_kvar " +
                                       "From " + conn.Database + ":" + "t" + finder.nzp_user + "_supg Where nzp_zvk < " + finder.nzp_zvk + " ORDER BY nzp_zvk DESC;");
                            sql.Add("Select first 1 pref, nzp_zvk, nzp_kvar " +
                                       "From " + conn.Database + ":" + "t" + finder.nzp_user + "_supg Where nzp_zvk > " + finder.nzp_zvk + " ORDER BY nzp_zvk;");
#endif
                            
                            break;
                        }
                    case "спис.необр.заяв.":
                        {
                            conn = GetConnection(Constants.cons_Webdata);
                            ret = OpenDb(conn, true);
                            if (!ret.result) return null;
#if PG
                            sql.Add("Select nzp_zvk, pref, nzp_kvar " +
                                       "From public." + "t" + finder.nzp_user + "_disp Where nzp_zvk < " + finder.nzp_zvk + " ORDER BY nzp_zvk DESC limit 1;");
                            sql.Add("Select pref, nzp_zvk, nzp_kvar  " +
                                       "From public." + "t" + finder.nzp_user + "_disp Where nzp_zvk > " + finder.nzp_zvk + " ORDER BY nzp_zvk limit 1;");
#else
                            sql.Add("Select first 1 nzp_zvk, pref, nzp_kvar " +
                                       "From " + conn.Database + ":" + "t" + finder.nzp_user + "_disp Where nzp_zvk < " + finder.nzp_zvk + " ORDER BY nzp_zvk DESC;");
                            sql.Add("Select first 1 pref, nzp_zvk, nzp_kvar  " +
                                       "From " + conn.Database + ":" + "t" + finder.nzp_user + "_disp Where nzp_zvk > " + finder.nzp_zvk + " ORDER BY nzp_zvk;");
#endif

                            break;
                        }
                }
            }
            #endregion

            #region для наряда-заказа
            if (finder.nzp_zk != 0)
            {
                switch (finder.prms)
                {
                    case "спис.нз.выполн.":
                        {
                            conn = GetConnection(Constants.cons_Webdata);
                            ret = OpenDb(conn, true);
                            if (!ret.result) return null;

                            // Список выводится в порядке убывания номеров н-з

#if PG
                            sql.Add("Select nzp_zk, pref, nzp_zvk, nzp_kvar " +
                                        "From public." + "t" + finder.nzp_user + "_zakaz Where nzp_zk > " + finder.nzp_zk + " ORDER BY nzp_zk limit 1;");
                            sql.Add("Select nzp_zk, pref, nzp_zvk, nzp_kvar " +
                                        "From public." + "t" + finder.nzp_user + "_zakaz Where nzp_zk < " + finder.nzp_zk + " ORDER BY nzp_zk DESC limit 1;");
#else
                            sql.Add("Select first 1 nzp_zk, pref, nzp_zvk, nzp_kvar " +
                                                                    "From " + conn.Database + ":" + "t" + finder.nzp_user + "_zakaz Where nzp_zk > " + finder.nzp_zk + " ORDER BY nzp_zk;");
                            sql.Add("Select first 1 nzp_zk, pref, nzp_zvk, nzp_kvar " +
                                        "From " + conn.Database + ":" + "t" + finder.nzp_user + "_zakaz Where nzp_zk < " + finder.nzp_zk + " ORDER BY nzp_zk DESC;");
#endif
                            break;
                        }
                    case "спис.заяв.":
                        {
                            conn = GetConnection(Constants.cons_Webdata);
                            ret = OpenDb(conn, true);
                            if (!ret.result) return null;
#if PG
                            sql.Add("Select nzp_zk, pref, nzp_zvk, nzp_kvar " +
                                        "From public." + "t" + finder.nzp_user + "_supg Where nzp_zk < " + finder.nzp_zk + " ORDER BY nzp_zk DESC limit 1;");
                            sql.Add("Select nzp_zk, pref, nzp_zvk, nzp_kvar " +
                                        "From public." + "t" + finder.nzp_user + "_supg Where nzp_zk > " + finder.nzp_zk + " ORDER BY nzp_zk limit 1;");
#else
                            sql.Add("Select first 1 nzp_zk, pref, nzp_zvk, nzp_kvar " +
                                        "From " + conn.Database + ":" + "t" + finder.nzp_user + "_supg Where nzp_zk < " + finder.nzp_zk + " ORDER BY nzp_zk DESC;");
                            sql.Add("Select first 1 nzp_zk, pref, nzp_zvk, nzp_kvar " +
                                        "From " + conn.Database + ":" + "t" + finder.nzp_user + "_supg Where nzp_zk > " + finder.nzp_zk + " ORDER BY nzp_zk;");
#endif
                            
                            break;
                        }
                    case "спис.нз.заяв.":
                        {
                            conn = GetConnection(Constants.cons_Kernel);
                            ret = OpenDb(conn, true);
                            if (!ret.result) return null;
#if PG
                            sql.Add("Select zk.nzp_zk, '" + finder.pref + "' as pref, zk.nzp_zvk, k.nzp_kvar " +
                                        "From " + Points.Pref + "_supg. zakaz zk, " + Points.Pref + "_supg. zvk k " +
                                        "Where k.nzp_zvk = " + finder.nzp_zvk + " AND k.nzp_zvk = zk.nzp_zvk and zk.nzp_zk < " + finder.nzp_zk + " ORDER BY zk.nzp_zvk, zk.norm DESC, zk.nzp_zk DESC limit 1;");
                            sql.Add("Select zk.nzp_zk, '" + finder.pref + "' as pref, zk.nzp_zvk, k.nzp_kvar " +
                                        "From " + Points.Pref + "_supg. zakaz zk, " + Points.Pref + "_supg. zvk k " +
                                        "Where k.nzp_zvk = " + finder.nzp_zvk + " AND k.nzp_zvk = zk.nzp_zvk and zk.nzp_zk > " + finder.nzp_zk + " ORDER BY zk.nzp_zvk, zk.norm, zk.nzp_zk limit 1;");
#else
                            sql.Add("Select first 1 zk.nzp_zk, '" + finder.pref + "' as pref, zk.nzp_zvk, k.nzp_kvar " +
                                        "From " + Points.Pref + "_supg: zakaz zk, " + Points.Pref + "_supg: zvk k " +
                                        "Where k.nzp_zvk = " + finder.nzp_zvk + " AND k.nzp_zvk = zk.nzp_zvk and zk.nzp_zk < " + finder.nzp_zk + " ORDER BY zk.nzp_zvk, zk.norm DESC, zk.nzp_zk DESC;");
                            sql.Add("Select first 1 zk.nzp_zk, '" + finder.pref + "' as pref, zk.nzp_zvk, k.nzp_kvar " +
                                        "From " + Points.Pref + "_supg: zakaz zk, " + Points.Pref + "_supg: zvk k " +
                                        "Where k.nzp_zvk = " + finder.nzp_zvk + " AND k.nzp_zvk = zk.nzp_zvk and zk.nzp_zk > " + finder.nzp_zk + " ORDER BY zk.nzp_zvk, zk.norm, zk.nzp_zk;");
#endif
                            break;
                        }
                    case "спис.нз.опрос":
                        {
                            conn = GetConnection(Constants.cons_Webdata);
                            ret = OpenDb(conn, true);
                            if (!ret.result) return null;

                            // Список выводится в порядке убывания номеров н-з
#if PG
                            sql.Add("Select nzp_zk, pref, nzp_zvk, nzp_kvar " +
                                        "From public." + "t" + finder.nzp_user + "_zakazo Where nzp_zk > " + finder.nzp_zk + " ORDER BY nzp_zk limit 1;");
                            sql.Add("Select nzp_zk, pref, nzp_zvk, nzp_kvar " +
                                        "From public." + "t" + finder.nzp_user + "_zakazo Where nzp_zk < " + finder.nzp_zk + " ORDER BY nzp_zk DESC limit 1;");
#else
                            sql.Add("Select first 1 nzp_zk, pref, nzp_zvk, nzp_kvar " +
                                                                    "From " + conn.Database + ":" + "t" + finder.nzp_user + "_zakazo Where nzp_zk > " + finder.nzp_zk + " ORDER BY nzp_zk;");
                            sql.Add("Select first 1 nzp_zk, pref, nzp_zvk, nzp_kvar " +
                                        "From " + conn.Database + ":" + "t" + finder.nzp_user + "_zakazo Where nzp_zk < " + finder.nzp_zk + " ORDER BY nzp_zk DESC;");
#endif
                            break;
                        }
                    
                }
            }
            #endregion

            for (int i = 0; i < sql.Count; i++)
            {
                if (!ExecRead(conn, out reader, sql[i].ToString(), true).result)
                {
                    MonitorLog.WriteLog("Ошибка выборки GetCarousel" + sql[i].ToString(), MonitorLog.typelog.Error, 20, 201, true);
                    ret.result = false;
                    return null;
                }
                if (reader != null)
                {
                    while (reader.Read())
                    {
                        #region для заявки
                        if (i == 0)
                        {
                            if (reader["nzp_zvk"] != DBNull.Value) prev_zvk = Convert.ToInt32(reader["nzp_zvk"]);
                            if (reader["pref"] != DBNull.Value) prev_pref = Convert.ToString(reader["pref"]).Trim();
                            if (reader["nzp_kvar"] != DBNull.Value) prev_nzp_kvar = Convert.ToInt32(reader["nzp_kvar"]);
                        }
                        else
                        {
                            if (reader["nzp_zvk"] != DBNull.Value) next_zvk = Convert.ToInt32(reader["nzp_zvk"]);
                            if (reader["pref"] != DBNull.Value) next_pref = Convert.ToString(reader["pref"]).Trim();
                            if (reader["nzp_kvar"] != DBNull.Value) next_nzp_kvar = Convert.ToInt32(reader["nzp_kvar"]);
                        }
                        #endregion

                        #region для наряда-заказа
                        if (finder.nzp_zk != 0)
                        {

                            if (i == 0)
                            {
                                if (reader["nzp_zvk"] != DBNull.Value) prev_zvk = Convert.ToInt32(reader["nzp_zvk"]);
                                if (reader["nzp_zk"] != DBNull.Value) prev_zk = Convert.ToInt32(reader["nzp_zk"]);
                                if (reader["pref"] != DBNull.Value) prev_pref = Convert.ToString(reader["pref"]).Trim();
                                if (reader["nzp_kvar"] != DBNull.Value) prev_nzp_kvar = Convert.ToInt32(reader["nzp_kvar"]);
                            }
                            else
                            {
                                if (reader["nzp_zvk"] != DBNull.Value) next_zvk = Convert.ToInt32(reader["nzp_zvk"]);
                                if (reader["nzp_zk"] != DBNull.Value) next_zk = Convert.ToInt32(reader["nzp_zk"]);
                                if (reader["pref"] != DBNull.Value) next_pref = Convert.ToString(reader["pref"]).Trim();
                                if (reader["nzp_kvar"] != DBNull.Value) next_nzp_kvar = Convert.ToInt32(reader["nzp_kvar"]);
                            }
                        }
                        #endregion

                    }
                }
            }

            #region заполнение данных
            res.nzp_zvk_next = next_zvk;
            res.nzp_zvk_prev = prev_zvk;

            res.pref_next = next_pref;
            res.pref_prev = prev_pref;

            res.nzp_kvar_next = next_nzp_kvar;
            res.nzp_kvar_prev = prev_nzp_kvar;

            #region для наряда-заказа

            if (finder.nzp_zk != 0)
            {
                res.nzp_zk_next = next_zk;
                res.nzp_zk_prev = prev_zk;
            }

            #endregion
            #endregion

            return res;
        }

        /// <summary>
        /// процедура возвращает результат поиска заявок СУПГ
        /// </summary>
        /// <param name="nzp_user"></param>
        /// <param name="ret"></param>
        /// <returns></returns>
        public List<ZvkFinder> GetFindZvk(SupgFinder finder, int flag, out Returns ret)
        {
            List<ZvkFinder> resList = new List<ZvkFinder>();
            StringBuilder sql = new StringBuilder();
            IDataReader reader = null;
            ret = Utils.InitReturns();
            string skip = "";
            if (finder.nzp_user < 1)
            {
                ret.result = false;
                ret.text = "Не определен пользователь";
                return null;
            }

            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result) return null;

            string tXX_supg = "t" + finder.nzp_user + "_supg ";

            if (finder.flag_disp == 1)
            {
                tXX_supg = "t" + finder.nzp_user + "_disp ";
            }

            if (finder.flag_disp == 2)
            {
                tXX_supg = "t" + finder.nzp_user + "_zakaz ";
                if (finder.flag_survey == 1)
                {
                    tXX_supg = "t" + finder.nzp_user + "_zakazo ";
                }
            }

            //if (!TempTableInWebCashe(conn_web, tXX_supg))
            //{
            //    ret.text = "Список заявок не сформирован. Выполните поиск заявок.";
            //    ret.tag = -1;
            //    ret.result = false;

            //    conn_web.Close();
            //    return null;
            //}

            if (finder.skip > 0)
            {
#if PG
                skip = " offset " + finder.skip;
#else
                skip = " skip " + finder.skip;
#endif
            }
#if PG
            sql.Append("Select COUNT(*) From public." + tXX_supg + ";");
#else
            sql.Append("Select COUNT(*) From " + conn_web.Database + "@" + DBManager.getServer(conn_web) + ":" + tXX_supg + ";");
#endif

            ret = ExecRead(conn_web, out reader, sql.ToString(), true);
            if (!ret.result)
            {
                MonitorLog.WriteLog("Ошибка выборки GetFindZvk" + ret.text, MonitorLog.typelog.Error, 20, 201, true);
                ret.result = false;
                return null;
            }
            if (reader != null)
            {
                while (reader.Read())
                {
#if PG
                    if (reader.GetValue(0) != DBNull.Value) ret.tag = Convert.ToInt32(reader[0]);
#else
                    if (reader.GetValue(0) != DBNull.Value) ret.tag = Convert.ToInt32(reader.GetString(0));
#endif
                }
            }

            reader = null;


            if (flag == 0)
            {
                sql.Remove(0, sql.Length);
#if PG
                sql.Append(" Select nzp, pref, nzp_kvar, nzp_dom, nzp_zvk, zvk_date, comment, adr, res_name"); 
                sql.Append(" From public." + tXX_supg + " ORDER BY nzp_zvk, norm, nzp_zk " + skip + " ;");
#else
                sql.Append(" Select " + skip + " nzp, pref, nzp_kvar, nzp_dom, nzp_zvk, zvk_date, comment, adr, res_name");
                sql.Append(" From " + conn_web.Database + ":" + tXX_supg + " ORDER BY nzp_zvk, norm, nzp_zk ;");
#endif
                if (!ExecRead(conn_web, out reader, sql.ToString(), true).result)
                {
                    MonitorLog.WriteLog("Ошибка выборки GetFindZvk" + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                    ret.result = false;
                    return null;
                }
                if (reader != null)
                {
                    int i = 0;
                    while (reader.Read())
                    {
                        i++;

                        ZvkFinder item = new ZvkFinder();
                        if (reader["nzp_kvar"] != DBNull.Value) item.nzp = Convert.ToInt32(reader["nzp_kvar"]);
                        if (reader["pref"] != DBNull.Value) item.pref = Convert.ToString(reader["pref"]).Trim();
                        if (reader["nzp_kvar"] != DBNull.Value) item.nzp_kvar = Convert.ToInt32(reader["nzp_kvar"]);
                        if (reader["nzp_dom"] != DBNull.Value) item.nzp_dom = Convert.ToInt32(reader["nzp_dom"]);
                        if (reader["nzp_zvk"] != DBNull.Value) item.nzp_zvk = Convert.ToInt32(reader["nzp_zvk"]);
                        if (reader["zvk_date"] != DBNull.Value) item.zvk_date = Convert.ToString(reader["zvk_date"]).Trim();
                        if (reader["comment"] != DBNull.Value) item.comment = Convert.ToString(reader["comment"]).Trim();
                        if (reader["adr"] != DBNull.Value) item.adr = Convert.ToString(reader["adr"]).Trim();
                        if (reader["res_name"] != DBNull.Value) item.res_name = Convert.ToString(reader["res_name"]).Trim();
                        resList.Add(item);

                        if (finder.rows > 0 && i >= finder.rows) break;
                    }
                }
            }
            else
            {
#if PG
 if (flag == 2)
                {
                    sql.Remove(0, sql.Length);
                    sql.Append(" Select  nzp, pref, nzp_kvar, nzp_dom, nzp_zvk, zvk_date, comment, adr,");
                    sql.Append(" res_name, norm, nzp_zk, order_date, service, res_zakaz, ds_actual, fact_date, result");
                    sql.Append(" From public." + tXX_supg + " ORDER BY nzp_zk desc" + skip + ";");
                }
                else
                {
                    sql.Remove(0, sql.Length);
                    sql.Append(" Select nzp, pref, nzp_kvar, nzp_dom, nzp_zvk, zvk_date, comment, adr,");
                    sql.Append(" res_name, norm, nzp_zk, order_date, service, res_zakaz, ds_actual");
                    sql.Append(" From public." + tXX_supg + " ORDER BY nzp_zvk DESC, norm, nzp_zk " + skip + " ;");
                }
#else
 if (flag == 2)
                {
                    sql.Remove(0, sql.Length);
                    sql.Append(" Select " + skip + " nzp, pref, nzp_kvar, nzp_dom, nzp_zvk, zvk_date, comment, adr,");
                    sql.Append(" res_name, norm, nzp_zk, order_date, service, res_zakaz, ds_actual, fact_date, result");
                    sql.Append(" From " + conn_web.Database + ":" + tXX_supg + " ORDER BY nzp_zk desc;");
                }
                else
                {
                    sql.Remove(0, sql.Length);
                    sql.Append(" Select " + skip + " nzp, pref, nzp_kvar, nzp_dom, nzp_zvk, zvk_date, comment, adr,");
                    sql.Append(" res_name, norm, nzp_zk, order_date, service, res_zakaz, ds_actual");
                    sql.Append(" From " + conn_web.Database + ":" + tXX_supg + " ORDER BY nzp_zvk DESC, norm, nzp_zk ;");
                }
#endif

                if (!ExecRead(conn_web, out reader, sql.ToString(), true).result)
                {
                    MonitorLog.WriteLog("Ошибка выборки GetFindZvk" + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                    ret.result = false;
                    return null;
                }
                if (reader != null)
                {
                    int i = 0;
                    while (reader.Read())
                    {
                        i++;

                        ZvkFinder item = new ZvkFinder();
                        int temp_ds_actual = 0;
                        if (reader["nzp"] != DBNull.Value) item.nzp = Convert.ToInt32(reader["nzp_kvar"]);
                        if (reader["pref"] != DBNull.Value) item.pref = Convert.ToString(reader["pref"]).Trim();
                        if (reader["nzp_kvar"] != DBNull.Value) item.nzp_kvar = Convert.ToInt32(reader["nzp_kvar"]);
                        if (reader["nzp_dom"] != DBNull.Value) item.nzp_dom = Convert.ToInt32(reader["nzp_dom"]);
                        if (reader["nzp_zvk"] != DBNull.Value) item.nzp_zvk = Convert.ToInt32(reader["nzp_zvk"]);
                        if (reader["zvk_date"] != DBNull.Value) item.zvk_date = Convert.ToString(reader["zvk_date"]).Trim();
                        if (reader["comment"] != DBNull.Value) item.comment = Convert.ToString(reader["comment"]).Trim();
                        if (reader["adr"] != DBNull.Value) item.adr = Convert.ToString(reader["adr"]).Trim();
                        if (reader["res_name"] != DBNull.Value) item.res_name = Convert.ToString(reader["res_name"]).Trim();
                        if (reader["nzp_zk"] != DBNull.Value) item.nzp_zk = Convert.ToString(reader["nzp_zk"]).Trim();
                        if (reader["order_date"] != DBNull.Value) item.order_date = Convert.ToString(reader["order_date"]).Trim();
                        if (reader["service"] != DBNull.Value) item.service = Convert.ToString(reader["service"]).Trim();
                        if (reader["res_zakaz"] != DBNull.Value) item.res_zakaz = Convert.ToString(reader["res_zakaz"]).Trim();
                        
                        if (reader["ds_actual"] != DBNull.Value) temp_ds_actual = Convert.ToInt32(reader["ds_actual"]);
                        if (temp_ds_actual == 0)
                        {
                            item.ds_actual = "";
                        }
                        else
                        {
                            item.ds_actual = "Сформировать недопоставку";
                        }

                        if (flag == 2)
                        {
                            if (reader["fact_date"] != DBNull.Value) item.fact_date = Convert.ToString(reader["fact_date"]).Trim();
                            if (reader["result"] != DBNull.Value) item.result = Convert.ToString(reader["result"]).Trim();
                        }

                        resList.Add(item);

                        if (finder.rows > 0 && i >= finder.rows) break;
                    }
                }
            }
            return resList;
        }


        private Returns SaveFinder(SupgFinder finder, int nzp_page)
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
                if (ret.result)
                {
                    conn_db = GetConnection(Constants.cons_Kernel);
                    ret = OpenDb(conn_db, true);
                }
                else
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

#if PG
                sql.Append("delete from " + tXX_spfinder + " where nzp_page = " + nzp_page.ToString());
#else
                sql.Append("delete from " + tXX_spfinder + " where nzp_page = " + nzp_page.ToString());
#endif

                ret = ExecSQL(conn_web, sql.ToString(), true);
                if (!ret.result)
                {
                    conn_web.Close();
                    return ret;
                }

                #region Первая вкладка шаблона поиска - Заявка

                if (finder.nzp_zvk > 0)
                {
                    sql.Remove(0, sql.Length);
#if PG
                    sql.Append("insert into " + tXX_spfinder + " values (0,\'Номер заявки\',\'" + finder.nzp_zvk.ToString() + "\'," + nzp_page.ToString() + ")");
#else
                    sql.Append("insert into " + tXX_spfinder + " values (0,\'Номер заявки\',\'" + finder.nzp_zvk.ToString() + "\'," + nzp_page.ToString() + ")");
#endif
                    ret = ExecSQL(conn_web, sql.ToString(), true);
                    if (!ret.result)
                    {
                        conn_web.Close();
                        return ret;
                    }
                }

                if (finder.zvk_date_from.Trim() != "")
                {
                    sql.Remove(0, sql.Length);
#if PG
                    sql.Append("insert into " + tXX_spfinder + " values (0,\'Дата регистрации с\',\'" + finder.zvk_date_from.ToString() + "\'," + nzp_page.ToString() + ")");
#else
                    sql.Append("insert into " + tXX_spfinder + " values (0,\'Дата регистрации с\',\'" + finder.zvk_date_from.ToString() + "\'," + nzp_page.ToString() + ")");
#endif
                    ret = ExecSQL(conn_web, sql.ToString(), true);
                    if (!ret.result)
                    {
                        conn_web.Close();
                        return ret;
                    }
                }

                if (finder.zvk_date_to.Trim() != "")
                {
                    sql.Remove(0, sql.Length);
#if PG
                    sql.Append("insert into " + tXX_spfinder + " values (0,\'Дата регистрации по\',\'" + finder.zvk_date_to.ToString() + "\'," + nzp_page.ToString() + ")");
#else
                    sql.Append("insert into " + tXX_spfinder + " values (0,\'Дата регистрации по\',\'" + finder.zvk_date_to.ToString() + "\'," + nzp_page.ToString() + ")");
#endif
                    ret = ExecSQL(conn_web, sql.ToString(), true);
                    if (!ret.result)
                    {
                        conn_web.Close();
                        return ret;
                    }
                }


                if (finder.zvk_ztype > 0)
                {
                    sql.Remove(0, sql.Length);
#if PG
                    sql.Append("Select zvk_type from " + Points.Pref + "_supg. s_zvktype where nzp_ztype = " + finder.zvk_ztype);
#else
                    sql.Append("Select zvk_type from " + Points.Pref + "_supg: s_zvktype where nzp_ztype = " + finder.zvk_ztype);
#endif
                    if (!ExecRead(conn_db, out reader, sql.ToString(), true).result)
                    {
                        MonitorLog.WriteLog("Ошибка выборки " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                        ret.result = false;
                        return ret;
                    }
                    string zvk_type = "";
                    if (reader != null)
                    {
                        while (reader.Read())
                        {
                            if (reader["zvk_type"] != DBNull.Value) zvk_type = Convert.ToString(reader["zvk_type"]).Trim();
                        }
                    }

                    sql.Remove(0, sql.Length);
#if PG
                    sql.Append("insert into " + tXX_spfinder + " values (0,\'Классификация сообщения\',\'" + zvk_type + "\'," + nzp_page.ToString() + ")");
#else
                    sql.Append("insert into " + tXX_spfinder + " values (0,\'Классификация сообщения\',\'" + zvk_type + "\'," + nzp_page.ToString() + ")");
#endif
                    ret = ExecSQL(conn_web, sql.ToString(), true);
                    if (!ret.result)
                    {
                        conn_web.Close();
                        return ret;
                    }
                }

                if (finder.zvk_exec_date_from.Trim() != "")
                {
                    sql.Remove(0, sql.Length);
#if PG
                    sql.Append("insert into " + tXX_spfinder + " values (0,\'Cрок выполнения с\',\'" + finder.zvk_exec_date_from.ToString() + "\'," + nzp_page.ToString() + ")");
#else
                    sql.Append("insert into " + tXX_spfinder + " values (0,\'Cрок выполнения с\',\'" + finder.zvk_exec_date_from.ToString() + "\'," + nzp_page.ToString() + ")");
#endif
                    ret = ExecSQL(conn_web, sql.ToString(), true);
                    if (!ret.result)
                    {
                        conn_web.Close();
                        return ret;
                    }
                }

                if (finder.zvk_exec_date_to.Trim() != "")
                {
                    sql.Remove(0, sql.Length);
#if PG
                    sql.Append("insert into " + tXX_spfinder + " values (0,\'Cрок выполнения по\',\'" + finder.zvk_exec_date_to.ToString() + "\'," + nzp_page.ToString() + ")");
#else
                    sql.Append("insert into " + tXX_spfinder + " values (0,\'Cрок выполнения по\',\'" + finder.zvk_exec_date_to.ToString() + "\'," + nzp_page.ToString() + ")");
#endif
                    ret = ExecSQL(conn_web, sql.ToString(), true);
                    if (!ret.result)
                    {
                        conn_web.Close();
                        return ret;
                    }
                }

                if (finder.zvk_fact_date_from.Trim() != "")
                {
                    sql.Remove(0, sql.Length);
                    #if PG
                    sql.Append("insert into " + tXX_spfinder + " values (0,\'Факт выполнения с\',\'" + finder.zvk_fact_date_from.ToString() + "\'," + nzp_page.ToString() + ")");
#else
                    sql.Append("insert into " + tXX_spfinder + " values (0,\'Факт выполнения с\',\'" + finder.zvk_fact_date_from.ToString() + "\'," + nzp_page.ToString() + ")");
#endif
                    ret = ExecSQL(conn_web, sql.ToString(), true);
                    if (!ret.result)
                    {
                        conn_web.Close();
                        return ret;
                    }
                }

                if (finder.zvk_fact_date_to.Trim() != "")
                {
                    sql.Remove(0, sql.Length);
#if PG
                    sql.Append("insert into " + tXX_spfinder + " values (0,\'Факт выполнения по\',\'" + finder.zvk_fact_date_to.ToString() + "\'," + nzp_page.ToString() + ")");
#else
                    sql.Append("insert into " + tXX_spfinder + " values (0,\'Факт выполнения по\',\'" + finder.zvk_fact_date_to.ToString() + "\'," + nzp_page.ToString() + ")");
#endif
                    ret = ExecSQL(conn_web, sql.ToString(), true);
                    if (!ret.result)
                    {
                        conn_web.Close();
                        return ret;
                    }
                }

                if (finder.zvk_res > 0)
                {
                    sql.Remove(0, sql.Length);
#if PG
                    sql.Append("Select res_name from " + Points.Pref + "_supg. s_result where nzp_res = " + finder.zvk_res);
#else
                    sql.Append("Select res_name from " + Points.Pref + "_supg: s_result where nzp_res = " + finder.zvk_res);
#endif
                    if (!ExecRead(conn_db, out reader, sql.ToString(), true).result)
                    {
                        MonitorLog.WriteLog("Ошибка выборки " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                        ret.result = false;
                        return ret;
                    }
                    string res_name = "";
                    if (reader != null)
                    {
                        while (reader.Read())
                        {
                            if (reader["res_name"] != DBNull.Value) res_name = Convert.ToString(reader["res_name"]).Trim();
                        }
                    }

                    sql.Remove(0, sql.Length);
#if PG
                    sql.Append("insert into " + tXX_spfinder + " values (0,\'Результат выполнения\',\'" + res_name + "\'," + nzp_page.ToString() + ")");
#else
                    sql.Append("insert into " + tXX_spfinder + " values (0,\'Результат выполнения\',\'" + res_name + "\'," + nzp_page.ToString() + ")");
#endif
                    ret = ExecSQL(conn_web, sql.ToString(), true);
                    if (!ret.result)
                    {
                        conn_web.Close();
                        return ret;
                    }
                }

                if (finder.zvk_result_comment.Trim() != "")
                {

                    sql.Remove(0, sql.Length);
#if PG
                    sql.Append("insert into " + tXX_spfinder + " values (0,\'Комментарий результата\',\'" + finder.zvk_result_comment.ToString() + "\'," + nzp_page.ToString() + ")");
#else
                    sql.Append("insert into " + tXX_spfinder + " values (0,\'Комментарий результата\',\'" + finder.zvk_result_comment.ToString() + "\'," + nzp_page.ToString() + ")");
#endif
                    ret = ExecSQL(conn_web, sql.ToString(), true);
                    if (!ret.result)
                    {
                        conn_web.Close();
                        return ret;
                    }
                }

                if (finder.zvk_demand_name.Trim() != "")
                {
                    sql.Remove(0, sql.Length);
#if PG
                    sql.Append("insert into " + tXX_spfinder + " values (0,\'Ф.И.О. заявителя\',\'" + finder.zvk_demand_name.ToString() + "\'," + nzp_page.ToString() + ")");
#else
                    sql.Append("insert into " + tXX_spfinder + " values (0,\'Ф.И.О. заявителя\',\'" + finder.zvk_demand_name.ToString() + "\'," + nzp_page.ToString() + ")");
#endif
                    ret = ExecSQL(conn_web, sql.ToString(), true);
                    if (!ret.result)
                    {
                        conn_web.Close();
                        return ret;
                    }
                }

                #endregion

                #region Вторая вкладка шаблона поиска - Переадресация

                if (finder._date_from.ToString().Trim() != "")
                {
                    sql.Remove(0, sql.Length);
#if PG
                    sql.Append(" Insert into " + tXX_spfinder);
                    sql.Append(" values (0,\'Дата с\',\'" + finder._date_from.ToString() + "\'," + nzp_page.ToString() + ")");
#else
                    sql.Append(" Insert into " + tXX_spfinder);
                    sql.Append(" values (0,\'Дата с\',\'" + finder._date_from.ToString() + "\'," + nzp_page.ToString() + ")");
#endif
                    ret = ExecSQL(conn_web, sql.ToString(), true);
                    if (!ret.result)
                    {
                        conn_web.Close();
                        return ret;
                    }
                }

                if (finder._date_to.Trim() != "")
                {
                    sql.Remove(0, sql.Length);
#if PG
                    sql.Append(" Insert into " + tXX_spfinder);
                    sql.Append(" values (0,\'Дата по\',\'" + finder._date_to.ToString() + "\'," + nzp_page.ToString() + ")");
#else
                    sql.Append(" Insert into " + tXX_spfinder);
                    sql.Append(" values (0,\'Дата по\',\'" + finder._date_to.ToString() + "\'," + nzp_page.ToString() + ")");
#endif
                    ret = ExecSQL(conn_web, sql.ToString(), true);
                    if (!ret.result)
                    {
                        conn_web.Close();
                        return ret;
                    }
                }

                if (finder.nzp_slug > 0)
                {
                    sql.Remove(0, sql.Length);
                    #if PG
                    sql.Append(" Select slug_name");
                    sql.Append(" From " + Points.Pref + "_supg. s_slug where nzp_slug = " + finder.nzp_slug);
#else
                    sql.Append(" Select slug_name");
                    sql.Append(" From " + Points.Pref + "_supg: s_slug where nzp_slug = " + finder.nzp_slug);
#endif
                    if (!ExecRead(conn_db, out reader, sql.ToString(), true).result)
                    {
                        MonitorLog.WriteLog("Ошибка выборки " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                        ret.result = false;
                        return ret;
                    }
                    string slug_name = "";
                    if (reader != null)
                    {
                        while (reader.Read())
                        {
                            if (reader["slug_name"] != DBNull.Value) slug_name = Convert.ToString(reader["slug_name"]).Trim();
                        }
                    }

                    sql.Remove(0, sql.Length);
#if PG
                    sql.Append("Insert into " + tXX_spfinder);
                    sql.Append(" values (0,\'Cлужба\',\'" + slug_name + "\'," + nzp_page.ToString() + ")");
#else
                    sql.Append("Insert into " + tXX_spfinder);
                    sql.Append(" values (0,\'Cлужба\',\'" + slug_name + "\'," + nzp_page.ToString() + ")");
#endif
                    ret = ExecSQL(conn_web, sql.ToString(), true);
                    if (!ret.result)
                    {
                        conn_web.Close();
                        return ret;
                    }
                }

                #endregion

                #region Третья вкладка - Наряд - заказ

                if (finder.nzp_zk > 0)
                {
                    sql.Remove(0, sql.Length);
#if PG
                    sql.Append(" Insert into " + tXX_spfinder);
                    sql.Append(" values (0,\'Номер заявления\',\'" + finder.nzp_zk.ToString() + "\'," + nzp_page.ToString() + ")");
#else
                    sql.Append(" Insert into " + tXX_spfinder);
                    sql.Append(" values (0,\'Номер заявления\',\'" + finder.nzp_zk.ToString() + "\'," + nzp_page.ToString() + ")");
#endif
                    ret = ExecSQL(conn_web, sql.ToString(), true);
                    if (!ret.result)
                    {
                        conn_web.Close();
                        return ret;
                    }
                }

                if (finder.zk_order_date_from.Trim() != "")
                {
                    sql.Remove(0 , sql.Length);
#if PG
                    sql.Append(" Insert into " + tXX_spfinder);
                    sql.Append(" Values (0,\'Дата наряда заказа с\',\'" + finder.zk_order_date_from.ToString() + "\'," + nzp_page.ToString() + ")");
#else
                    sql.Append(" Insert into " + tXX_spfinder);
                    sql.Append(" Values (0,\'Дата наряда заказа с\',\'" + finder.zk_order_date_from.ToString() + "\'," + nzp_page.ToString() + ")");
#endif
                    ret = ExecSQL(conn_web, sql.ToString(), true);
                    if (!ret.result)
                    {
                        conn_web.Close();
                        return ret;
                    }
                }

                if (finder.zk_order_date_to.Trim() != "")
                {
                    sql.Remove(0, sql.Length);
#if PG
                    sql.Append(" Insert into " + tXX_spfinder);
                    sql.Append(" Values (0,\'Дата наряда заказа по\',\'" + finder.zk_order_date_to.ToString() + "\'," + nzp_page.ToString() + ")");
#else
                    sql.Append(" Insert into " + tXX_spfinder);
                    sql.Append(" Values (0,\'Дата наряда заказа по\',\'" + finder.zk_order_date_to.ToString() + "\'," + nzp_page.ToString() + ")");
#endif
                    ret = ExecSQL(conn_web, sql.ToString(), true);
                    if (!ret.result)
                    {
                        conn_web.Close();
                        return ret;
                    }
                }

                if (finder.zk_fact_date_from.Trim() != "")
                {
                    sql.Remove(0, sql.Length);
#if PG
                    sql.Append(" Insert into " + tXX_spfinder);
                    sql.Append(" Values (0,\'Факт выполнения заказа с\',\'" + finder.zk_fact_date_from.ToString() + "\'," + nzp_page.ToString() + ")");
#else
                    sql.Append(" Insert into " + tXX_spfinder);
                    sql.Append(" Values (0,\'Факт выполнения заказа с\',\'" + finder.zk_fact_date_from.ToString() + "\'," + nzp_page.ToString() + ")");
#endif
                    ret = ExecSQL(conn_web, sql.ToString(), true);
                    if (!ret.result)
                    {
                        conn_web.Close();
                        return ret;
                    }
                }

                if (finder.zk_fact_date_to.Trim() != "")
                {
                    sql.Remove(0, sql.Length);
                    #if PG
                    sql.Append(" Insert into " + tXX_spfinder);
                    sql.Append(" Values (0,\'Факт выполнения заказа по\',\'" + finder.zk_fact_date_to.ToString() + "\'," + nzp_page.ToString() + ")");
#else
                    sql.Append(" Insert into " + tXX_spfinder);
                    sql.Append(" Values (0,\'Факт выполнения заказа по\',\'" + finder.zk_fact_date_to.ToString() + "\'," + nzp_page.ToString() + ")");
#endif
                    ret = ExecSQL(conn_web, sql.ToString(), true);
                    if (!ret.result)
                    {
                        conn_web.Close();
                        return ret;
                    }
                }

                if (finder.zk_plan_date_from.Trim() != "")
                {
                    sql.Remove(0, sql.Length);
                    #if PG
                    sql.Append(" Insert into " + tXX_spfinder);
                    sql.Append(" Values (0,\'Дата. план ремонт с\',\'" + finder.zk_plan_date_from.ToString() + "\'," + nzp_page.ToString() + ")");
#else
                    sql.Append(" Insert into " + tXX_spfinder);
                    sql.Append(" Values (0,\'Дата. план ремонт с\',\'" + finder.zk_plan_date_from.ToString() + "\'," + nzp_page.ToString() + ")");
#endif
                    ret = ExecSQL(conn_web, sql.ToString(), true);
                    if (!ret.result)
                    {
                        conn_web.Close();
                        return ret;
                    }
                }


                if (finder.zk_plan_date_to.Trim() != "")
                {
                    sql.Remove(0, sql.Length);
                    #if PG
                    sql.Append(" Insert into " + tXX_spfinder);
                    sql.Append(" Values (0,\'Дата. план ремонт по\',\'" + finder.zk_plan_date_to.ToString() + "\'," + nzp_page.ToString() + ")");
#else
                    sql.Append(" Insert into " + tXX_spfinder);
                    sql.Append(" Values (0,\'Дата. план ремонт по\',\'" + finder.zk_plan_date_to.ToString() + "\'," + nzp_page.ToString() + ")");
#endif
                    ret = ExecSQL(conn_web, sql.ToString(), true);
                    if (!ret.result)
                    {
                        conn_web.Close();
                        return ret;
                    }
                }

                if (finder.zk_control_date_from.Trim() != "")
                {
                    sql.Remove(0, sql.Length);
                    #if PG
                    sql.Append(" Insert into " + tXX_spfinder);
                    sql.Append(" Values (0,\'Контр. срок выполнения с\',\'" + finder.zk_control_date_from.ToString() + "\'," + nzp_page.ToString() + ")");
#else
                    sql.Append(" Insert into " + tXX_spfinder);
                    sql.Append(" Values (0,\'Контр. срок выполнения с\',\'" + finder.zk_control_date_from.ToString() + "\'," + nzp_page.ToString() + ")");
#endif
                    ret = ExecSQL(conn_web, sql.ToString(), true);
                    if (!ret.result)
                    {
                        conn_web.Close();
                        return ret;
                    }
                }


                if (finder.zk_control_date_to.Trim() != "")
                {
                    sql.Remove(0, sql.Length);
                    #if PG
                    sql.Append(" Insert into " + tXX_spfinder);
                    sql.Append(" Values (0,\'Контр. срок выполнения по\',\'" + finder.zk_control_date_to.ToString() + "\'," + nzp_page.ToString() + ")");
#else
                    sql.Append(" Insert into " + tXX_spfinder);
                    sql.Append(" Values (0,\'Контр. срок выполнения по\',\'" + finder.zk_control_date_to.ToString() + "\'," + nzp_page.ToString() + ")");
#endif
                    ret = ExecSQL(conn_web, sql.ToString(), true);
                    if (!ret.result)
                    {
                        conn_web.Close();
                        return ret;
                    }
                }


                if (finder.nzp_status > 0)
                {
                    string stat = "";
                    if (finder.nzp_status == 1)
                        stat = "В разработке для отправки исполнителю";   
                    if (finder.nzp_status == 2)
                        stat = "Отправлен исполнителю";
                    if (finder.nzp_status == 3)
                        stat = "Получен исполнителем";
                    sql.Remove(0, sql.Length);
#if PG
                    sql.Append(" Insert into " + tXX_spfinder);
                    sql.Append(" Values (0,\'Статус наряда-заказа\',\'" + stat + "\'," + nzp_page.ToString() + ")");
#else
                    sql.Append(" Insert into " + tXX_spfinder);
                    sql.Append(" Values (0,\'Статус наряда-заказа\',\'" + stat + "\'," + nzp_page.ToString() + ")");
#endif
                    ret = ExecSQL(conn_web, sql.ToString(), true);
                    if (!ret.result)
                    {
                        conn_web.Close();
                        return ret;
                    }
                }

                if (finder.zk_accept_date_from.Trim() != "")
                {
                    sql.Remove(0, sql.Length);
                    #if PG
                    sql.Append(" Insert into " + tXX_spfinder);
                    sql.Append(" Values (0,\'Дата получения исполнителем с\',\'" + finder.zk_accept_date_from.ToString() + "\'," + nzp_page.ToString() + ")");
#else
                    sql.Append(" Insert into " + tXX_spfinder);
                    sql.Append(" Values (0,\'Дата получения исполнителем с\',\'" + finder.zk_accept_date_from.ToString() + "\'," + nzp_page.ToString() + ")");
#endif
                    ret = ExecSQL(conn_web, sql.ToString(), true);
                    if (!ret.result)
                    {
                        conn_web.Close();
                        return ret;
                    }
                }

                if (finder.zk_accept_date_to.Trim() != "")
                {
                    sql.Remove(0, sql.Length);
#if PG
                    sql.Append(" Insert into " + tXX_spfinder);
                    sql.Append(" Values (0,\'Дата получения исполнителем по\',\'" + finder.zk_accept_date_to.ToString() + "\'," + nzp_page.ToString() + ")");
#else
                    sql.Append(" Insert into " + tXX_spfinder);
                    sql.Append(" Values (0,\'Дата получения исполнителем по\',\'" + finder.zk_accept_date_to.ToString() + "\'," + nzp_page.ToString() + ")");
#endif
                    ret = ExecSQL(conn_web, sql.ToString(), true);
                    if (!ret.result)
                    {
                        conn_web.Close();
                        return ret;
                    }
                }

                if (finder.zk_mail_date_from.Trim() != "")
                {
                    sql.Remove(0, sql.Length);
                    #if PG
                    sql.Append(" Insert into " + tXX_spfinder);
                    sql.Append(" Values (0,\'Дата отправки исполнителю с\',\'" + finder.zk_mail_date_from.ToString() + "\'," + nzp_page.ToString() + ")");
#else
                    sql.Append(" Insert into " + tXX_spfinder);
                    sql.Append(" Values (0,\'Дата отправки исполнителю с\',\'" + finder.zk_mail_date_from.ToString() + "\'," + nzp_page.ToString() + ")");
#endif
                    ret = ExecSQL(conn_web, sql.ToString(), true);
                    if (!ret.result)
                    {
                        conn_web.Close();
                        return ret;
                    }
                }

                if (finder.zk_mail_date_to.Trim() != "")
                {
                    sql.Remove(0, sql.Length);
                    #if PG
                    sql.Append(" Insert into " + tXX_spfinder);
                    sql.Append(" Values (0,\'Дата отправки исполнителю по\',\'" + finder.zk_mail_date_to.ToString() + "\'," + nzp_page.ToString() + ")");
#else
                    sql.Append(" Insert into " + tXX_spfinder);
                    sql.Append(" Values (0,\'Дата отправки исполнителю по\',\'" + finder.zk_mail_date_to.ToString() + "\'," + nzp_page.ToString() + ")");
#endif
                    ret = ExecSQL(conn_web, sql.ToString(), true);
                    if (!ret.result)
                    {
                        conn_web.Close();
                        return ret;
                    }
                }

                if (finder.zk_nzp_supp > 0)
                {
                    sql.Remove(0, sql.Length);
                    #if PG
                    sql.Append(" Select name_supp from " + Points.Pref + "_kernel.supplier");
                    sql.Append(" Where nzp_supp = " + finder.zk_nzp_supp);
#else
                    sql.Append(" Select name_supp from " + Points.Pref + "_kernel: supplier");
                    sql.Append(" Where nzp_supp = " + finder.zk_nzp_supp);
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

                    sql.Remove(0, sql.Length);
                    #if PG
                    sql.Append("insert into " + tXX_spfinder + " values (0,\'Исполнитель\',\'" + name_supp + "\'," + nzp_page.ToString() + ")");
#else
                    sql.Append("insert into " + tXX_spfinder + " values (0,\'Исполнитель\',\'" + name_supp + "\'," + nzp_page.ToString() + ")");
#endif
                    ret = ExecSQL(conn_web, sql.ToString(), true);
                    if (!ret.result)
                    {
                        conn_web.Close();
                        return ret;
                    }
                }

                if (finder.nzp_serv > 0)
                {
                    sql.Remove(0, sql.Length);
                   #if PG
 sql.Append("Select service from " + Points.Pref + "_kernel.services where nzp_serv = " + finder.nzp_serv);
#else
 sql.Append("Select service from " + Points.Pref + "_kernel: services where nzp_serv = " + finder.nzp_serv);
#endif
                    if (!ExecRead(conn_db, out reader, sql.ToString(), true).result)
                    {
                        MonitorLog.WriteLog("Ошибка выборки " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                        ret.result = false;
                        return ret;
                    }
                    string nzp_serv = "";
                    if (reader != null)
                    {
                        while (reader.Read())
                        {
                            if (reader["service"] != DBNull.Value)
                            { nzp_serv = Convert.ToString(reader["service"]).Trim(); }
                            else
                            { nzp_serv = "-"; }
                        }
                    }

                    sql.Remove(0, sql.Length);
                    #if PG
                    sql.Append("insert into " + tXX_spfinder + " values (0,\'Услуга\',\'" + nzp_serv + "\'," + nzp_page.ToString() + ")");
#else
                    sql.Append("insert into " + tXX_spfinder + " values (0,\'Услуга\',\'" + nzp_serv + "\'," + nzp_page.ToString() + ")");
#endif
                    ret = ExecSQL(conn_web, sql.ToString(), true);
                    if (!ret.result)
                    {
                        conn_web.Close();
                        return ret;
                    }
                }

                if (finder.nzp_res > 0)
                {
                    sql.Remove(0, sql.Length);
                    #if PG
                    sql.Append("Select res_name from " + Points.Pref + "_supg. s_result where nzp_res = " + finder.nzp_res);
#else
                    sql.Append("Select res_name from " + Points.Pref + "_supg: s_result where nzp_res = " + finder.nzp_res);
#endif
                    if (!ExecRead(conn_db, out reader, sql.ToString(), true).result)
                    {
                        MonitorLog.WriteLog("Ошибка выборки " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                        ret.result = false;
                        return ret;
                    }
                    string res_name = "";
                    if (reader != null)
                    {
                        while (reader.Read())
                        {
                            if (reader["res_name"] != DBNull.Value) res_name = Convert.ToString(reader["res_name"]).Trim();
                        }
                    }

                    sql.Remove(0, sql.Length);
                    #if PG
                    sql.Append("insert into " + tXX_spfinder + " values (0,\'Результат\',\'" + res_name + "\'," + nzp_page.ToString() + ")");
#else
                    sql.Append("insert into " + tXX_spfinder + " values (0,\'Результат\',\'" + res_name + "\'," + nzp_page.ToString() + ")");
#endif
                    ret = ExecSQL(conn_web, sql.ToString(), true);
                    if (!ret.result)
                    {
                        conn_web.Close();
                        return ret;
                    }
                }

                if (finder.nzp_atts > 0)
                {
                    sql.Remove(0, sql.Length);
                    #if PG
                    sql.Append("Select atts_name from " + Points.Pref + "_supg. s_attestation where nzp_atts = " + finder.nzp_atts);
#else
                    sql.Append("Select atts_name from " + Points.Pref + "_supg: s_attestation where nzp_atts = " + finder.nzp_atts);
#endif
                    if (!ExecRead(conn_db, out reader, sql.ToString(), true).result)
                    {
                        MonitorLog.WriteLog("Ошибка выборки " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                        ret.result = false;
                        return ret;
                    }
                    string atts_name = "";
                    if (reader != null)
                    {
                        while (reader.Read())
                        {
                            if (reader["atts_name"] != DBNull.Value) atts_name = Convert.ToString(reader["atts_name"]).Trim();
                        }
                    }

                    sql.Remove(0, sql.Length);
                    #if PG
                    sql.Append("insert into " + tXX_spfinder + " values (0,\'Подтверждение выполнения\',\'" + atts_name + "\'," + nzp_page.ToString() + ")");
#else
                    sql.Append("insert into " + tXX_spfinder + " values (0,\'Подтверждение выполнения\',\'" + atts_name + "\'," + nzp_page.ToString() + ")");
#endif
                    ret = ExecSQL(conn_web, sql.ToString(), true);
                    if (!ret.result)
                    {
                        conn_web.Close();
                        return ret;
                    }
                }

                if (finder.repeated > -1)
                {
                    sql.Remove(0, sql.Length);
                    string repeated = "";
                    if (finder.repeated == 0)
                    {
                        repeated = "Нет";
                    }
                    else
                    {
                        if (finder.repeated == 1)
                        {
                            repeated = "Да";
                        }
                    }
                    #if PG
                    sql.Append("insert into " + tXX_spfinder + " values (0,\'Выдано повторное\',\'" + repeated + "\'," + nzp_page.ToString() + ")");
#else
                    sql.Append("insert into " + tXX_spfinder + " values (0,\'Выдано повторное\',\'" + repeated + "\'," + nzp_page.ToString() + ")");
#endif
                    ret = ExecSQL(conn_web, sql.ToString(), true);
                    if (!ret.result)
                    {
                        conn_web.Close();
                        return ret;
                    }
                }

                if (finder.is_replicate > -1)
                {
                    sql.Remove(0, sql.Length);
                    string is_replicate = "";
                    if (finder.is_replicate == 0)
                    {
                        is_replicate = "Нет";
                    }
                    else
                    {
                        if (finder.is_replicate == 1)
                        {
                            is_replicate = "Да";
                        }
                    }
                    #if PG
                    sql.Append("insert into " + tXX_spfinder + " values (0,\'Повторное\',\'" + is_replicate + "\'," + nzp_page.ToString() + ")");
#else
                    sql.Append("insert into " + tXX_spfinder + " values (0,\'Повторное\',\'" + is_replicate + "\'," + nzp_page.ToString() + ")");
#endif
                    ret = ExecSQL(conn_web, sql.ToString(), true);
                    if (!ret.result)
                    {
                        conn_web.Close();
                        return ret;
                    }
                }

                return ret;
            }
            #endregion

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

        


        //public Dictionary<int,string> GetLsGroups(Ls finder, out Returns ret)
        //{

        //}

    }
}

