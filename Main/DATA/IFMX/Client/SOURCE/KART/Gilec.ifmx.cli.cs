using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;
using System.Data;

namespace STCLINE.KP50.DataBase
{
    //----------------------------------------------------------------------
    public partial class DbGilecClient : DataBaseHead
    //----------------------------------------------------------------------
    {
        //----------------------------------------------------------------------
        public Returns ExecReadThr(IDbConnection connectionID, out IDataReader reader, string sql, bool inlog)
        //----------------------------------------------------------------------
        {
            Returns ret;
            ret = ExecRead(connectionID, out reader, sql, inlog); ;
            if (!ret.result)
            {
                connectionID.Close();
                throw new Exception();
            }
            return ret;
        }

        //----------------------------------------------------------------------
        public object ExecScalarThr(IDbConnection connectionID, string sql, out Returns ret, bool inlog)
        //----------------------------------------------------------------------
        {
            object count = ExecScalar(connectionID, sql, out ret, inlog);
            if (!ret.result)
            {
                connectionID.Close();
                throw new Exception();
            }
            return count;
        }

        //----------------------------------------------------------------------
        public void ExecSQLThr(IDbConnection connectionID, string sql, bool inlog) //
        //----------------------------------------------------------------------
        {
            if (!ExecSQL(connectionID, null, sql, inlog).result)
            {
                connectionID.Close();
                throw new Exception();
            }
        }

        //----------------------------------------------------------------------
        public string MakeWhereString(Gilec finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            return "";
        }

        //----------------------------------------------------------------------
        public string MakeWhereString(Kart finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();

            string whereString = "";
            WhereString(finder, "", ref whereString);

            if (whereString == "")
                return "";

            StringBuilder sql = new StringBuilder();

            string my_kart = "kart";
            if (finder.is_arx)
            {
                my_kart = "kart_arx";
            }
#if PG
            sql.Append(" Select count(*) From PREFX_data." + my_kart + " c");
            sql.Append(" where c.nzp_kvar = k.nzp_kvar ");
#else
            sql.Append(" Select count(*) From PREFX_data:" + my_kart + " c");
            sql.Append(" where c.nzp_kvar = k.nzp_kvar ");
#endif
            sql.Append(whereString);

            return sql.ToString();
        }

        //----------------------------------------------------------------------
        public string MakeWhereString(string prms)
        //----------------------------------------------------------------------
        {
            //            ret = Utils.InitReturns();


            StringBuilder sql = new StringBuilder();

            //учесть дополнительные шаблоны
            if (!Utils.GetParams(prms, Constants.act_ubil.ToString()))
            {
#if PG
                sql.AppendFormat(" AND (c.nzp_tkrt = 1 AND {0}(c.dat_oprp, {2}) >= {1} OR ", DBManager.sNvlWord, DBManager.sCurDate, DBManager.MDY(1, 1, 3000));
                sql.AppendFormat(" c.nzp_tkrt = 2 AND {0}(c.dat_ofor, {2}) >= {1})", DBManager.sNvlWord, DBManager.sCurDate, DBManager.MDY(1, 1, 3000));
                //БФТ
                //sql.Append(" and c.nzp_tkrt=1");
                //sql.Append(" and coalesce(c.dat_oprp, date('01.01.3000')) >= current_date");
#else
                sql.Append(" and c.nzp_tkrt=1");
                sql.Append(" and nvl(c.dat_oprp, date('01.01.3000')) >= today");
#endif
            }

            if (!Utils.GetParams(prms, Constants.act_actual.ToString()))
            {
                sql.Append(" and c.isactual='1'");
            }

            if (!Utils.GetParams(prms, Constants.act_neuch.ToString()))
            {
#if PG
                sql.Append(" and coalesce(c.neuch,'0')<>'1'");
#else
                sql.Append(" and nvl(c.neuch,'0')<>'1'");
#endif
            }

            if (Utils.GetParams(prms, Constants.act_arx.ToString()))
            {
                sql.Append(" AND 500=500"); // пометка для архива и и не архива
            }

            return sql.ToString();


        }
        //----------------------------------------------------------------------
        public string MakeGilPerWhereString(string prms)
        //----------------------------------------------------------------------
        {
            StringBuilder sql = new StringBuilder();

            //учесть дополнительные параметры
            if (!Utils.GetParams(prms, Constants.act_actual.ToString()))
            {
                sql.Append(" and c.is_actual<>100");
            }
            return sql.ToString();
        }

        //----------------------------------------------------------------------
        protected void WhereString(Kart finder, string tab_dosie, ref string whereString)
        //----------------------------------------------------------------------
        {
            StringBuilder swhere = new StringBuilder();
            if (finder.no_podtv_docs != -1)
                whereString += "  and  (select count(*) from PREFX_data" + tableDelimiter +
                           "gil_periods " + " t where t.nzp_kvar = c.nzp_kvar and t.nzp_gilec=c.nzp_gil and t.no_podtv_docs = " + finder.no_podtv_docs +
                           ") > 0";


            //====для Самары досье

            if ((finder.nzp_gil.Trim() != "") && (finder.nzp_kart.Trim() == ""))
            {
#if PG
                whereString += "  and 0= (select count(*) from " + tab_dosie + " t where t.nzp_kart=c.nzp_kart) " +
                                " and ( " +
                                "  0<( select count(*)" +
                                " from  " + tab_dosie + "  h" +
                                " where h.fam=c.fam" +
                                " and h.ima=c.ima    " +
                                " and h.otch=c.otch    " +
                                " and h.dat_rog=c.dat_rog)" +
                                " OR" +
                                "  0<( select count(*)" +
                                " from  " + tab_dosie + "  h" +
                                " where h.fam_c=c.fam" +
                                " and h.ima_c=c.ima    " +
                                " and h.otch_c=c.otch    " +
                                " and h.dat_rog_c=c.dat_rog)" +
                                " OR" +
                                "  0<( select count(*)" +
                                " from  " + tab_dosie + "  h" +
                                " where h.fam=c.fam_c" +
                                " and h.ima=c.ima_c    " +
                                " and h.otch=c.otch_c" +
                                " and h.dat_rog=c.dat_rog_c)" +
                                " OR" +
                                "  0<( select count(*)" +
                                " from  " + tab_dosie + "  h" +
                                " where h.fam_c=c.fam_c" +
                                " and h.ima_c=c.ima_c    " +
                                " and h.otch_c=c.otch_c    " +
                                " and h.dat_rog_c=c.dat_rog_c)" +
                                " OR" +
                                "  0<( select count(*)" +
                                " from  " + tab_dosie + "  h" +
                                " where h.nzp_gil=c.nzp_gil" +
                                " and h.pref='PREFX')" +
                                "      )";
#else
                whereString += "  and 0= (select count(*) from " + tab_dosie + " t where t.nzp_kart=c.nzp_kart) " +
                                " and ( " +
                                "  0<( select count(*)" +
                                " from  " + tab_dosie + "  h" +
                                " where h.fam=c.fam" +
                                " and h.ima=c.ima    " +
                                " and h.otch=c.otch    " +
                                " and h.dat_rog=c.dat_rog)" +
                                " OR" +
                                "  0<( select count(*)" +
                                " from  " + tab_dosie + "  h" +
                                " where h.fam_c=c.fam" +
                                " and h.ima_c=c.ima    " +
                                " and h.otch_c=c.otch    " +
                                " and h.dat_rog_c=c.dat_rog)" +
                                " OR" +
                                "  0<( select count(*)" +
                                " from  " + tab_dosie + "  h" +
                                " where h.fam=c.fam_c" +
                                " and h.ima=c.ima_c    " +
                                " and h.otch=c.otch_c" +
                                " and h.dat_rog=c.dat_rog_c)" +
                                " OR" +
                                "  0<( select count(*)" +
                                " from  " + tab_dosie + "  h" +
                                " where h.fam_c=c.fam_c" +
                                " and h.ima_c=c.ima_c    " +
                                " and h.otch_c=c.otch_c    " +
                                " and h.dat_rog_c=c.dat_rog_c)" +
                                " OR" +
                                "  0<( select count(*)" +
                                " from  " + tab_dosie + "  h" +
                                " where h.nzp_gil=c.nzp_gil" +
                                " and h.pref='PREFX')" +
                                "      )";
#endif

                return;
            }

            //====для Казани досье
            if ((finder.nzp_gil.Trim() != "") && (finder.nzp_kart.Trim() != ""))
            {
                whereString += " and c.nzp_gil = " + finder.nzp_gil;
                return;
            }
            //====для Казани



#if PG
            if (finder.fam != "")
                if (finder.fam.IndexOf("*") == -1) swhere.Append(" and upper(c.fam) = upper(initcap('" + finder.fam + "'))");
                else swhere.Append(" and upper(c.fam) like upper('" + finder.fam + "')");
            if (finder.ima != "")
                if (finder.ima.IndexOf("*") == -1) swhere.Append(" and upper(c.ima) = upper(initcap('" + finder.ima + "'))");
                else swhere.Append(" and upper(c.ima) like upper('" + finder.ima + "')");
            if (finder.otch != "")
                if (finder.otch.IndexOf("*") == -1) swhere.Append(" and upper(c.otch) = upper(initcap('" + finder.otch + "'))");
                else swhere.Append(" and upper(c.otch) like upper('" + finder.otch + "')");
            if (finder.fam_c != "")
                if (finder.fam_c.IndexOf("*") == -1) swhere.Append(" and upper(c.fam_c) = upper(initcap('" + finder.fam_c + "'))");
                else swhere.Append(" and upper(c.fam_c) like upper('" + finder.fam_c + "')");
            if (finder.ima_c != "")
                if (finder.ima_c.IndexOf("*") == -1) swhere.Append(" and upper(c.ima_c) = upper(initcap('" + finder.ima_c + "'))");
                else swhere.Append(" and upper(c.ima_c) like upper('" + finder.ima_c + "')");
            if (finder.otch_c != "")
                if (finder.otch_c.IndexOf("*") == -1) swhere.Append(" and upper(c.otch_c) = upper(initcap('" + finder.otch_c + "'))");
                else swhere.Append(" and upper(c.otch_c) like upper('" + finder.otch_c + "')");
            if (finder.dat_rog_po != "") swhere.Append(" and c.dat_rog <= '" + finder.dat_rog_po + "'");
            if (finder.dat_rog != "")
                if (finder.dat_rog_po != "") swhere.Append(" and c.dat_rog >= '" + finder.dat_rog + "'");
                else swhere.Append(" and c.dat_rog = '" + finder.dat_rog + "'");
            if (finder.dat_ofor_po != "") swhere.Append(" and c.dat_ofor <= '" + finder.dat_ofor_po + "'");
            if (finder.dat_ofor != "")
                if (finder.dat_ofor_po != "") swhere.Append(" and c.dat_ofor >= '" + finder.dat_ofor + "'");
                else swhere.Append(" and c.dat_ofor = '" + finder.dat_ofor + "'");
            if (finder.dat_oprp_po != "") swhere.Append(" and c.dat_oprp <= '" + finder.dat_oprp_po + "'");
            if (finder.dat_oprp != "")
                if (finder.dat_oprp_po != "") swhere.Append(" and c.dat_oprp >= '" + finder.dat_oprp + "'");
                else swhere.Append(" and c.dat_oprp = '" + finder.dat_oprp + "'");
            if (finder.vid_dat_po != "") swhere.Append(" and c.vid_dat <= '" + finder.vid_dat_po + "'");
            if (finder.vid_dat != "")
                if (finder.vid_dat_po != "") swhere.Append(" and c.vid_dat >= '" + finder.vid_dat + "'");
                else swhere.Append(" and c.vid_dat = '" + finder.vid_dat + "'");
#else
            if (finder.fam != "")
                if (finder.fam.IndexOf("*") == -1) swhere.Append(" and c.fam = initcap('" + finder.fam + "')");
                else swhere.Append(" and upper(c.fam) matches upper('" + finder.fam + "')");
            if (finder.ima != "")
                if (finder.ima.IndexOf("*") == -1) swhere.Append(" and c.ima = initcap('" + finder.ima + "')");
                else swhere.Append(" and upper(c.ima) matches upper('" + finder.ima + "')");
            if (finder.otch != "")
                if (finder.otch.IndexOf("*") == -1) swhere.Append(" and c.otch = initcap('" + finder.otch + "')");
                else swhere.Append(" and upper(c.otch) matches upper('" + finder.otch + "')");
            if (finder.fam_c != "")
                if (finder.fam_c.IndexOf("*") == -1) swhere.Append(" and c.fam_c = initcap('" + finder.fam_c + "')");
                else swhere.Append(" and upper(c.fam_c) matches upper('" + finder.fam_c + "')");
            if (finder.ima_c != "")
                if (finder.ima_c.IndexOf("*") == -1) swhere.Append(" and c.ima_c = initcap('" + finder.ima_c + "')");
                else swhere.Append(" and upper(c.ima_c) matches upper('" + finder.ima_c + "')");
            if (finder.otch_c != "")
                if (finder.otch_c.IndexOf("*") == -1) swhere.Append(" and c.otch_c = initcap('" + finder.otch_c + "')");
                else swhere.Append(" and upper(c.otch_c) matches upper('" + finder.otch_c + "')");
            if (finder.dat_rog_po != "") swhere.Append(" and c.dat_rog <= '" + finder.dat_rog_po + "'");
            if (finder.dat_rog != "")
                if (finder.dat_rog_po != "") swhere.Append(" and c.dat_rog >= '" + finder.dat_rog + "'");
                else swhere.Append(" and c.dat_rog = '" + finder.dat_rog + "'");
            if (finder.dat_ofor_po != "") swhere.Append(" and c.dat_ofor <= '" + finder.dat_ofor_po + "'");
            if (finder.dat_ofor != "")
                if (finder.dat_ofor_po != "") swhere.Append(" and c.dat_ofor >= '" + finder.dat_ofor + "'");
                else swhere.Append(" and c.dat_ofor = '" + finder.dat_ofor + "'");
            if (finder.dat_oprp_po != "") swhere.Append(" and c.dat_oprp <= '" + finder.dat_oprp_po + "'");
            if (finder.dat_oprp != "")
                if (finder.dat_oprp_po != "") swhere.Append(" and c.dat_oprp >= '" + finder.dat_oprp + "'");
                else swhere.Append(" and c.dat_oprp = '" + finder.dat_oprp + "'");
            if (finder.vid_dat_po != "") swhere.Append(" and c.vid_dat <= '" + finder.vid_dat_po + "'");
            if (finder.vid_dat != "")
                if (finder.vid_dat_po != "") swhere.Append(" and c.vid_dat >= '" + finder.vid_dat + "'");
                else swhere.Append(" and c.vid_dat = '" + finder.vid_dat + "'");
#endif
            //todo: PG проверить
            if (finder.tprp != "") swhere.Append(" and upper(c.tprp) = upper('" + finder.tprp + "') ");
            if (finder.dat_izm != "") swhere.Append(" and c.dat_izm >= '" + finder.dat_izm + "'");
            if (finder.dat_izm_po != "") swhere.Append(" and c.dat_izm <= '" + finder.dat_izm_po + "'");
            if (finder.who_pvu == "1") swhere.Append(" and ((c.dat_pvu is null) and (c.dat_svu is null)) ");

#if PG
            if (finder.nzp_kvar > 0)
            {
                swhere.Append(" and c.nzp_kvar = " + finder.nzp_kvar);
            }
            if (finder.nzp_tkrt != "") swhere.Append(" and c.nzp_tkrt = " + finder.nzp_tkrt);
            if (finder.gender != "") swhere.Append(" and c.gender = '" + finder.gender + "'");
            //if (finder.isactual != "") swhere.Append(" and c.isactual = '1'");
            //if (finder.neuch == "")     swhere.Append(" and coalesce(c.neuch,'0') <> '1'"); // наоборот
            if (finder.neuch == "1") swhere.Append(" and coalesce(c.neuch,'0') <> '1'");  //без удаленных
            if (finder.neuch == "2") swhere.Append(" and c.isactual = '1'");  //только актуальные
            if (finder.neuch == "3") swhere.Append(" and c.neuch='1'");  //только удаленные
            if (finder.no_get != "") swhere.Append(
                              " and c.isactual = '1'" +
                              " and c.nzp_tkrt =1 " +
                              " and coalesce(c.neuch,'0') <>'1' " +
                                    " and case when to_date( Extract(MONTH from current_date) ||'-' || Extract(DAY from current_date ) ,'MM-DD') >= " +
                                                  " to_date( Extract(MONTH from c.dat_rog) ||'-' || Extract(DAY from c.dat_rog ) ,'MM-DD') " +
                                             " then Extract(Year from current_date) - Extract(Year from c.dat_rog) " +
                                             " else Extract(Year from current_date) - Extract(Year from c.dat_rog)-1 end >=14" +

                                    " and case when to_date( Extract(MONTH from current_date) ||'-' || Extract(DAY from current_date ) ,'MM-DD') >= " +
                                                  " to_date( Extract(MONTH from c.dat_rog) ||'-' || Extract(DAY from c.dat_rog ) ,'MM-DD') " +
                                             " then Extract(Year from current_date) - Extract(Year from c.dat_rog) " +
                                             " else Extract(Year from current_date) - Extract(Year from c.dat_rog)-1 end  <20" +

                                    " and case when to_date( Extract(MONTH from c.vid_dat) ||'-' || Extract(DAY from c.vid_dat ) ,'MM-DD') >= " +
                                                  " to_date( Extract(MONTH from c.dat_rog) ||'-' || Extract(DAY from c.dat_rog ) ,'MM-DD') " +
                                             " then Extract(Year from c.vid_dat) - Extract(Year from c.dat_rog) " +
                                             " else Extract(Year from c.vid_dat) - Extract(Year from c.dat_rog)-1 end  <14"
                                                   );
            if (finder.no_change != "") swhere.Append(
                        " and (" +
                              "     c.isactual = '1'" +
                              " and c.nzp_tkrt =1 " +
                              " and coalesce(c.neuch,'0') <>'1' " +
                                    " and( (" +
                                    " case when to_date( Extract(MONTH from current_date) ||'-' || Extract(DAY from current_date ) ,'MM-DD') >= " +
                                              " to_date( Extract(MONTH from c.dat_rog) ||'-' || Extract(DAY from c.dat_rog ) ,'MM-DD') " +
                                         " then Extract(Year from current_date) - Extract(Year from c.dat_rog) " +
                                         " else Extract(Year from current_date) - Extract(Year from c.dat_rog)-1 end  >=20" +
                                    " and case when to_date( Extract(MONTH from current_date) ||'-' || Extract(DAY from current_date ) ,'MM-DD') >= " +
                                                  " to_date( Extract(MONTH from c.dat_rog) ||'-' || Extract(DAY from c.dat_rog ) ,'MM-DD') " +
                                             " then Extract(Year from current_date) - Extract(Year from c.dat_rog) " +
                                             " else Extract(Year from current_date) - Extract(Year from c.dat_rog)-1 end <45 " +
                                    " and case when to_date( Extract(MONTH from c.vid_dat) ||'-' || Extract(DAY from c.vid_dat ) ,'MM-DD') >= " +
                                                  " to_date( Extract(MONTH from c.dat_rog) ||'-' || Extract(DAY from c.dat_rog ) ,'MM-DD') " +
                                             " then Extract(Year from c.vid_dat) - Extract(Year from c.dat_rog)  " +
                                             " else Extract(Year from c.vid_dat) - Extract(Year from c.dat_rog)-1 end <20 " +
                                          ")" +
                                    " OR (" +
                                    "  case when to_date( Extract(MONTH from current_date) ||'-' || Extract(DAY from current_date) ,'MM-DD') >= " +
                                               " to_date( Extract(MONTH from c.dat_rog) ||'-' || Extract(DAY from c.dat_rog ) ,'MM-DD') " +
                                          " then Extract(Year from current_date) - Extract(Year from c.dat_rog) " +
                                          " else Extract(Year from current_date) - Extract(Year from c.dat_rog)-1 end >=45 " +
                                    " and case when to_date( Extract(MONTH from c.vid_dat) ||'-' || Extract(DAY from c.vid_dat) ,'MM-DD') >= " +
                                                  " to_date( Extract(MONTH from c.dat_rog) ||'-' || Extract(DAY from c.dat_rog ) ,'MM-DD') " +
                                             " then Extract(Year from c.vid_dat) - Extract(Year from c.dat_rog) " +
                                             " else Extract(Year from c.vid_dat) - Extract(Year from c.dat_rog)-1 end <45 " +
                                          "))" +
                    ")"
                                    );
            if (finder.serij != "") swhere.Append(" and c.nzp_dok >-99999   and c.serij = upper( '" + finder.serij + "')");  //для оптимизатора
            if (finder.nomer != "") swhere.Append(" and c.nzp_dok >-99999   and c.nomer = upper('" + finder.nomer + "')");
            //            if (finder.nzp_dok != "")    swhere.Append(" and c.nzp_dok = " + finder.nzp_dok); 
#else
            if (finder.nzp_kvar != Constants._ZERO_)
            {
                swhere.Append(" and c.nzp_kvar = " + finder.nzp_kvar);
            }
            if (finder.nzp_tkrt != "") swhere.Append(" and c.nzp_tkrt = " + finder.nzp_tkrt);
            if (finder.gender != "") swhere.Append(" and c.gender = '" + finder.gender + "'");
            //if (finder.isactual != "") swhere.Append(" and c.isactual = '1'");
            //if (finder.neuch == "")     swhere.Append(" and nvl(c.neuch,'0') <> '1'"); // наоборот
            if (finder.neuch == "1") swhere.Append(" and nvl(c.neuch,'0') <> '1'");  //без удаленных
            if (finder.neuch == "2") swhere.Append(" and c.isactual = '1'");  //только актуальные
            if (finder.neuch == "3") swhere.Append(" and c.neuch='1'");  //только удаленные
            if (finder.no_get != "") swhere.Append(
                              " and c.isactual = '1'" +
                              " and c.nzp_tkrt =1 " +
                              " and nvl(c.neuch,'0') <>'1' " +
                                    " and case when extend(today,month to day) >= extend(c.dat_rog,month to day) " +
                                    "             then year(today) - year(c.dat_rog) " +
                                    "             else year(today) - year(c.dat_rog)-1 " +
                                    "        end   >=14" +
                                    " and case when extend(today,month to day) >= extend(c.dat_rog,month to day) " +
                                    "             then year(today) - year(c.dat_rog) " +
                                    "             else year(today) - year(c.dat_rog)-1 " +
                                    "        end  <20" +
                                    " and case when extend(c.vid_dat,month to day) >= extend(c.dat_rog,month to day) " +
                                    "             then year(c.vid_dat) - year(c.dat_rog) " +
                                    "             else year(c.vid_dat) - year(c.dat_rog)-1 " +
                                    "        end  <14"
                                                   );
            if (finder.no_change != "") swhere.Append(
                        " and (" +
                              "     c.isactual = '1'" +
                              " and c.nzp_tkrt =1 " +
                              " and nvl(c.neuch,'0') <>'1' " +
                                    " and( (" +
                                    " case when extend(today,month to day) >= extend(c.dat_rog,month to day) " +
                                    "             then year(today) - year(c.dat_rog) " +
                                    "             else year(today) - year(c.dat_rog)-1 " +
                                    "        end  >=20" +
                                    " and case when extend(today,month to day) >= extend(c.dat_rog,month to day) " +
                                    "             then year(today) - year(c.dat_rog) " +
                                    "             else year(today) - year(c.dat_rog)-1 " +
                                    "        end <45 " +
                                    " and case when extend(c.vid_dat,month to day) >= extend(c.dat_rog,month to day) " +
                                    "             then year(c.vid_dat) - year(c.dat_rog) " +
                                    "             else year(c.vid_dat) - year(c.dat_rog)-1 " +
                                    "        end <20 " +
                                          ")" +
                                    " OR (" +
                                    "  case when extend(today,month to day) >= extend(c.dat_rog,month to day) " +
                                    "             then year(today) - year(c.dat_rog) " +
                                    "             else year(today) - year(c.dat_rog)-1 " +
                                    "        end >=45 " +
                                    " and case when extend(c.vid_dat,month to day) >= extend(c.dat_rog,month to day) " +
                                    "             then year(c.vid_dat) - year(c.dat_rog) " +
                                    "             else year(c.vid_dat) - year(c.dat_rog)-1 " +
                                    "        end <45 " +
                                          "))" +
                    ")"
                                    );
            if (finder.serij != "") swhere.Append(" and c.nzp_dok >-99999   and c.serij = upper( '" + finder.serij + "')");  //для оптимизатора
            if (finder.nomer != "") swhere.Append(" and c.nzp_dok >-99999   and c.nomer = upper('" + finder.nomer + "')");
            //            if (finder.nzp_dok != "")    swhere.Append(" and c.nzp_dok = " + finder.nzp_dok); 
#endif
            if (finder.dopFind != null)
                if (finder.dopFind.Count > 0) //учесть дополнительные шаблоны
                {
                    foreach (string s in finder.dopFind)
                    {
                        swhere.Append(s);
                    }
                }

            whereString = whereString + swhere.ToString();
        }
        //-------------------------------------------------------------------------
        //-------------------------------------------------------------------------
        public void SaveCheckedList(Gilec finder, List<string> checkedList, List<string> unCheckedList, out Returns ret)
        {
            // проверить пользователя
            ret = Utils.InitReturns();
            if (finder.nzp_user < 1)
            {
                ret.result = false;
                ret.text = "Не определен пользователь";
                ret.tag = -1;
                return;
            }


            string tXX_cnt = "";

            // таблица в кэше
            if (finder.prevPage == Constants.page_spisgil) tXX_cnt = "t" + Convert.ToString(finder.nzp_user).Trim() + "_gil";//список жильцов
            else if (finder.prevPage == Constants.page_kvargil) tXX_cnt = "t" + Convert.ToString(finder.nzp_user).Trim() + "_gilkvar";//поквартирная карточка
            else if (finder.prevPage == Constants.page_spisgilhistory) tXX_cnt = "t" + Convert.ToString(finder.nzp_user).Trim() + "_gilhist";//история жильца

            if (tXX_cnt == "")
            {
                ret.result = false;
                ret.text = "Не определена страница";
                return;
            }
            //string tXX_cnt = "t" + Convert.ToString(finder.nzp_user).Trim() + "_gil";

            // проверка подключения
            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result) return;

            if (!CasheExists(tXX_cnt))
            {
                ret.result = true;
                ret.text = "Данные не были выбраны! Выполните поиск.";
                ret.tag = -22;
                conn_web.Close();
                return;
            }

            // сформировать строку кодов отмеченных записей 
            //---------------------------------------------------
            string where_nzp_serial = "";
            string sqlString = "";

            for (int i = 0; i < checkedList.Count - 1; i++)
                where_nzp_serial += checkedList[i] + ",";
            if (checkedList.Count > 0) where_nzp_serial += checkedList[checkedList.Count - 1];

            if (where_nzp_serial.Length > 0)
            {
                // обновить список
                //---------------------------------------------------

                sqlString = "update " + tXX_cnt + " set is_checked = 1 where nzp_serial in (" + where_nzp_serial + ") and is_checked = 0 ";

                /*if (finder.is_arx) sqlString += " and is_arx = 1 ";
                else sqlString += " and is_arx = 0 ";*/

                ret = ExecSQL(conn_web, sqlString, true);
                if (!ret.result)
                {
                    conn_web.Close();
                    return;
                }
            }

            // сформировать строку кодов НЕотмеченных записей 
            //---------------------------------------------------
            where_nzp_serial = "";

            for (int i = 0; i < unCheckedList.Count - 1; i++)
                where_nzp_serial += unCheckedList[i] + ",";
            if (unCheckedList.Count > 0) where_nzp_serial += unCheckedList[unCheckedList.Count - 1];

            if (where_nzp_serial.Length > 0)
            {
                // обновить список
                //--------------------------------------------------
                sqlString = "update " + tXX_cnt + " set is_checked = 0 where nzp_serial in (" + where_nzp_serial + ") and is_checked = 1 ";
                /*if (finder.is_arx) sqlString += " and is_arx = 1 ";
                else sqlString += " and is_arx = 0 ";*/

                ret = ExecSQL(conn_web, sqlString, true);
                if (!ret.result)
                {
                    conn_web.Close();
                    return;
                }
            }
            conn_web.Close();
        }

        //----------------------------------------------------------------------
        public List<Kart> GetKart(Kart finder, out Returns ret) //вытащить Карточки жильцов для грида
        //----------------------------------------------------------------------
        {

            //Новая культура для дат------------------------
            CultureInfo newCI = (CultureInfo)Thread.CurrentThread.CurrentCulture.Clone();
            newCI.NumberFormat.NumberDecimalSeparator = ".";
            newCI.DateTimeFormat.ShortDatePattern = "dd.MM.yyyy";
            newCI.DateTimeFormat.LongTimePattern = "";
            Thread.CurrentThread.CurrentCulture = newCI;
            //Конец Новая культура для дат------------------------

            ret = Utils.InitReturns();
            if (finder.nzp_user < 1)
            {
                ret.result = false;
                ret.text = "Не определен пользователь";
                ret.tag = -1;
                return null;
            }

            List<Kart> Spis = new List<Kart>();

            Spis.Clear();

            //выбрать общее кол-во
            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result) return null;

            string tab_gil;
            if (finder.nzp_gil.Trim() != "") tab_gil = "_gilhist";
            else if (finder.nzp_kvar == Constants._ZERO_) tab_gil = "_gil";
            else tab_gil = "_gilkvar";

            string tXX_cnt = "t" + Convert.ToString(finder.nzp_user).Trim() + tab_gil;
            string strWhere = " where 1=1";

            //if (finder.nzp_kart != "")    strWhere=strWhere + " and nzp_kart = " + finder.nzp_kart ; 
            //if (finder.pref != "")        strWhere=strWhere + " and pref = '" + finder.pref +"'"; 


            if (!CasheExists(tXX_cnt))
            {
                ret.text = "Данные не были выбраны! Выполните поиск.";
                ret.tag = -22;
                conn_web.Close();
                return null;
            }
            ret.tag = DBManager.ExecScalar<Int32>(conn_web, " Select count(*) From " + tXX_cnt + strWhere);
            string limit;
            string offset = " OFFSET " + finder.skip;
            if (finder.rows != 0)
            {
                limit = " LIMIT " + finder.rows;
            }
            else
            {
                limit = " LIMIT 20 ";
            }


            string orderby = " ";

            switch (finder.sortby)
            {
                case Constants.sortby_fiodr: orderby = " order by fam, ima,otch, dat_rog, dat_ofor, dat_sost"; break;
                case Constants.sortby_adr: orderby = " order by rajon,ulica,idom,ndom,nkor,ikvar,nkvar,nkvar_n,num_ls,dat_rog,nzp_gil,dat_ofor,dat_sost "; break;
                case Constants.sortby_ls: orderby = " order by num_ls,dat_rog,nzp_gil,dat_ofor,dat_sost  "; break;
            }

            string sql = " Select " +
                         "   nzp_serial, is_checked,nzp_kvar, num_ls, pkod10, nzp_dom, nzp_ul,nzp_raj, nkvar,nkvar_n,ndom,nkor,ulica, rajon, nzp_geu, geu, adr, nzp_gil,nzp_kart, rod, " +
                         "   fam, ima, otch, dat_rog, gender, grgd, rem_mr, nzp_tkrt, isactual, neuch, sud, tprp, dat_ofor, dat_sost, " +
                         "   ikvar, idom, pref, dat_oprp, dok, serij, nomer, vid_mes, vid_dat, is_arx, " +
                         "   fio_kvs, obsh_plosh, gil_plosh, projiv, propis, tip_sobstv, komfortnost " +
                         " From " + tXX_cnt + strWhere + orderby + limit + offset ;
            //выбрать список
            IDataReader reader;
            if (!ExecRead(conn_web, out reader,
                sql, true).result)
            {
                conn_web.Close();
                ret.result = false;
                return null;
            }
            try
            {
                int i = 0;
                while (reader.Read())
                {
                    i = i + 1;
                    Kart zap = new Kart();

                    zap.num = (i + finder.skip).ToString();
                    if ((reader["is_checked"] == DBNull.Value)
                        || ((int)reader["is_checked"] == 0))
                        zap.is_checked = false;
                    else
                        zap.is_checked = true;


                    if (reader["num_ls"] == DBNull.Value)
                        zap.num_ls = 0;
                    else
                        zap.num_ls = (int)reader["num_ls"];

                    zap.pkod10 = reader["pkod10"] == DBNull.Value ? 0 : (int)reader["pkod10"];

                    if (reader["nzp_kvar"] == DBNull.Value)
                        zap.nzp_kvar = 0;
                    else
                        zap.nzp_kvar = (int)reader["nzp_kvar"];

                    if (reader["nzp_dom"] == DBNull.Value)
                        zap.nzp_dom = 0;
                    else
                        zap.nzp_dom = (int)reader["nzp_dom"];

                    if (reader["nzp_geu"] != DBNull.Value) zap.nzp_geu = (int)reader["nzp_geu"];
                    if (reader["geu"] != DBNull.Value) zap.geu = Convert.ToString(reader["geu"]).Trim();

                    if (reader["adr"] == DBNull.Value)
                        zap.adr = "";
                    else
                        zap.adr = (string)reader["adr"];

                    zap.nzp_gil = Convert.ToString(reader["nzp_gil"]).Trim();
                    zap.nzp_kart = Convert.ToString(reader["nzp_kart"]).Trim();
                    zap.rod = Convert.ToString(reader["rod"]).Trim();
                    zap.fam = Convert.ToString(reader["fam"]).Trim();
                    zap.ima = Convert.ToString(reader["ima"]).Trim();

                    zap.otch = Convert.ToString(reader["otch"]).Trim();
                    //                    zap.dat_rog = ((DateTime)reader["dat_rog"]).ToString("dd.MM.yyyy");
                    zap.dat_rog = Convert.ToString(reader["dat_rog"]).Trim();
                    zap.gender = Convert.ToString(reader["gender"]).Trim();
                    zap.rem_mr = Convert.ToString(reader["rem_mr"]).Trim();
                    zap.grgd = Convert.ToString(reader["grgd"]).Trim();
                    zap.nzp_tkrt = Convert.ToString(reader["nzp_tkrt"]).Trim();

                    zap.isactual = Convert.ToString(reader["isactual"]).Trim();
                    zap.neuch = Convert.ToString(reader["neuch"]).Trim();
                    zap.sud = Convert.ToString(reader["sud"]).Trim();
                    zap.tprp = Convert.ToString(reader["tprp"]).Trim();

                    zap.dat_ofor = Convert.ToString(reader["dat_ofor"]).Trim();
                    if (reader["dat_oprp"] != DBNull.Value) zap.dat_oprp = Convert.ToString(reader["dat_oprp"]).Trim();
                    zap.dat_sost = Convert.ToString(reader["dat_sost"]).Trim();

                    zap.pref = Convert.ToString(reader["pref"]).Trim();

                    zap.nzp_serial = Convert.ToString(reader["nzp_serial"]).Trim();

                    // документ, удостоверяющий личность
                    if (reader["dok"] != DBNull.Value) zap.dok = Convert.ToString(reader["dok"]).Trim();
                    if (reader["serij"] != DBNull.Value) zap.serij = Convert.ToString(reader["serij"]).Trim();
                    if (reader["nomer"] != DBNull.Value) zap.nomer = Convert.ToString(reader["nomer"]).Trim();
                    if (reader["vid_mes"] != DBNull.Value) zap.vid_mes = Convert.ToString(reader["vid_mes"]).Trim();
                    if (reader["vid_dat"] != DBNull.Value) zap.vid_dat = Convert.ToString(reader["vid_dat"]).Trim();

                    zap.is_arx = (Convert.ToString(reader["is_arx"]).Trim() == "1");

                    int projiv = 0;
                    int propis = 0;

                    if (reader["fio_kvs"] != DBNull.Value) zap.fio_kvs = reader["fio_kvs"].ToString();
                    if (reader["obsh_plosh"] != DBNull.Value) zap.obsh_plosh = reader["obsh_plosh"].ToString();
                    if (reader["gil_plosh"] != DBNull.Value) zap.gil_plosh = reader["gil_plosh"].ToString();
                    if (reader["projiv"] != DBNull.Value) if (Int32.TryParse(reader["projiv"].ToString(), out projiv)) zap.projiv = projiv;
                    if (reader["propis"] != DBNull.Value) if (Int32.TryParse(reader["propis"].ToString(), out propis)) zap.propis = propis;
                    if (reader["tip_sobstv"] != DBNull.Value) zap.tip_sobstv = reader["tip_sobstv"].ToString();
                    if (reader["komfortnost"] != DBNull.Value) zap.komfortnost = reader["komfortnost"].ToString();

                    //#region гражданства 
                    //string tXX_grgd = "t" + Convert.ToString(finder.nzp_user) + "_grgd";
                    //IDataReader reader_g;
                    //if (!ExecRead(conn_web, out reader_g,
                    //     " Select  nzp_kart, nzp_grgd, grgd,  pref " +
                    //     " From " + tXX_grgd + " where nzp_kart ="+ zap.nzp_kart+ 
                    //     " and pref='"+zap.pref+"'"  , true).result)
                    //{
                    //    conn_web.Close();
                    //    return null;
                    //}

                    //while (reader_g.Read())
                    //{
                    //    KartGrgd grgd = new KartGrgd();
                    //    grgd.nzp_kart = Convert.ToString(reader_g["nzp_kart"]);
                    //    grgd.nzp_grgd = Convert.ToString(reader_g["nzp_grgd"]);
                    //    grgd.grgd = Convert.ToString(reader_g["grgd"]);
                    //    grgd.pref = Convert.ToString(reader_g["pref"]);
                    //    zap.listKartGrgd.Add(grgd);
                    //}

                    //#endregion  


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

                MonitorLog.WriteLog("Ошибка заполнения списка карточек жильцов " + err, MonitorLog.typelog.Error, 20, 201, true);

                return null;
            }

        }//GetKart

        //----------------------------------------------------------------------
        public bool CasheExists(string tab) //проверить есть ли в кеше
        //----------------------------------------------------------------------
        {
            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            if (!OpenDb(conn_web, true).result) return false;

            bool b = TableInWebCashe(conn_web, tab);
            conn_web.Close();

            return b;

        }

        //----------------------------------------------------------------------
        public string MakeSobstwWhereString(string prms)
        //----------------------------------------------------------------------
        {
            StringBuilder sql = new StringBuilder();

            //учесть дополнительные параметры
            if (!Utils.GetParams(prms, Constants.act_actual.ToString()))
            {
                sql.Append(" and c.is_actual=1");
            }
            return sql.ToString();
        }
    }

}
