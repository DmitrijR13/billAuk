using System;
using System.Data;
using System.Linq;
using System.Net.Configuration;
using System.Xml.Linq;
using System.IO;
using System.Text;
using System.Collections.Generic;
using Bars.KP50.Utils;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;
using STCLINE.KP50.Utility;


namespace STCLINE.KP50.DataBase
{
    public class DbAdresHard : DataBaseHead
    {
        public List<Ls> LoadLs(IDbConnection conn_db, IDbTransaction transaction, Ls finder, out Returns ret) //найти и заполнить адрес для nzp_kvar
        {
            ret = new Returns(true);
            List<Ls> Listls = new List<Ls>();
            IDataReader reader, reader22;

           // string swhere = ""; //условия
            StringBuilder sql = new StringBuilder();
            DbTables tables = new DbTables(conn_db);

            // если mode = 0 - информация о л/с
            // если mode = 1 - информация о доме
            // если mode = 2 - информация об улице
            // если mode = 3 - информация о ЖЭУ
            // если mode = 4 - информация о Управляющая организация
            int mode = 0;
            
            if (finder.nzp_kvar > 0 || finder.num_ls > 0 || finder.pkod != "" || finder.pkod10 > 0
                || finder.fio != "")
            {
                StringBuilder where = new StringBuilder();
#if PG
                where.Append(" where 1=1");
#else
                where.Append(" and 1=1");
#endif
                if (finder.nzp_kvar > 0)
                {
                    where.Append(" and k.nzp_kvar = " + finder.nzp_kvar);
                }
                else if (finder.num_ls > 0)
                {
                    where.Append(" and k.num_ls = " + finder.num_ls);
                }
                else
                {

                    if (GlobalSettings.NewGeneratePkodMode)
                    {
                        where.Append(" and k.nzp_kvar in (select nzp_kvar from " + tables.kvar_pkodes + " where pkod = " + finder.pkod+ ")");
                    }
                    else
                    {
                        if (finder.pkod != "")
                        {
                            where.Append(" and k.pkod = " + finder.pkod);
                        }

                    }
                    if (finder.fio != "")
                    {
                        where.Append(" and k.fio like '%" + finder.fio + "%'");
                    }

                    if (finder.pkod10 > 0)
                    {
                        where.Append(" and k.pkod10 = " + finder.pkod10);
                    }
                }
#if PG
                sql.Append(" Select distinct d.nzp_wp, twn.nzp_town,"+(GlobalSettings.NewGeneratePkodMode?"":"k.pkod,")+" k.num_ls, k.pkod10, k.nzp_dom,k.fio, k.nzp_kvar, k.phone, k.porch, k.nkvar, k.nkvar_n, k.uch, k.typek, a.nzp_area, a.area, g.nzp_geu, g.geu, k.remark, d.nzp_ul,u.nzp_raj,  ");
                sql.Append("   trim(coalesce(ulicareg,'улица'))||' '||trim(coalesce(u.ulica,''))||' / '||trim(coalesce(r.rajon,''))|| ' / '||trim(" + sNvlWord +
                         "(twn.town,''))||'   дом '||" +
                           "   trim(coalesce(ndom,''))||'  корп. '|| trim(coalesce(nkor,''))||'  кв. '||trim(coalesce(nkvar,''))||'  ком. '||trim(coalesce(nkvar_n,'')) as adr");
                sql.Append(", trim(coalesce(ulicareg,'улица'))||' '||trim(coalesce(u.ulica,''))||' / '||trim(coalesce(r.rajon,'')) as ulica_rajon");
                sql.Append(", trim(coalesce(ulicareg,'улица'))||' '||trim(coalesce(u.ulica,'')) as ulica, r.rajon,s.stat");
                sql.Append(", rd.rajon_dom");
                sql.Append(", twn.town");
                sql.Append(", t.name_y stypek, ulica, ulicareg, ndom, nkor");
                if(!GlobalSettings.NewGeneratePkodMode) sql.Append(", round(k.pkod)||'' as spkod");
                sql.Append(", k.pref, k.is_open, t2.name_y state");
#else
                sql.Append(" Select first 1 unique d.nzp_wp, twn.nzp_town, "+(GlobalSettings.NewGeneratePkodMode?"":"k.pkod,")+"k.num_ls, k.pkod10, k.nzp_dom,k.fio, k.nzp_kvar, k.phone, k.porch, k.nkvar, k.nkvar_n, k.uch, k.typek, a.nzp_area, a.area, g.nzp_geu, g.geu, k.remark, d.nzp_ul,u.nzp_raj,  ");
                sql.Append("   trim(nvl(ulicareg,'улица'))||' '||trim(nvl(u.ulica,''))||' / '||trim(nvl(r.rajon,''))||'   дом '||" +
                           "   trim(nvl(ndom,''))||'  корп. '|| trim(nvl(nkor,''))||'  кв. '||trim(nvl(nkvar,''))||'  ком. '||trim(nvl(nkvar_n,'')) as adr");
                sql.Append(", trim(nvl(ulicareg,'улица'))||' '||trim(nvl(u.ulica,''))||' / '||trim(nvl(r.rajon,'')) as ulica_rajon");
                sql.Append(", trim(nvl(ulicareg,'улица'))||' '||trim(nvl(u.ulica,'')) as ulica, r.rajon");
                sql.Append(", rd.rajon_dom");
                sql.Append(", twn.town");
                sql.Append(", t.name_y stypek, ulica, ulicareg, ndom, nkor");
                if(!GlobalSettings.NewGeneratePkodMode) sql.Append(", round(k.pkod)||'' as spkod");
                sql.Append(", k.pref, k.is_open, t2.name_y state");
#endif


#if PG
                sql.Append(" From " + tables.kvar + " k");
                sql.Append(" left outer join " + tables.dom + " d on k.nzp_dom=d.nzp_dom");
                sql.Append(" left outer join " + tables.area + " a on a.nzp_area=k.nzp_area");
                sql.Append(" left outer join " + tables.ulica + " u on d.nzp_ul=u.nzp_ul");
                sql.Append(" left outer join " + tables.rajon + " r on u.nzp_raj=r.nzp_raj");
                sql.Append(" left outer join " + tables.geu + " g on g.nzp_geu=k.nzp_geu");
                sql.Append(" left outer join " + tables.res_y + " t on k.typek = t.nzp_y and t.nzp_res = 9999");
                sql.AppendFormat(" left outer join " + tables.res_y + " t2 on {0} = t2.nzp_y and t2.nzp_res = 18", "k.is_open".CastTo("INTEGER"));
                sql.Append(" left outer join  " + tables.town + " twn on twn.nzp_town=r.nzp_town");
                sql.Append(" left outer join " + tables.stat + " s on s.nzp_stat=twn.nzp_stat");
                sql.Append(" left outer join  " + tables.rajon_dom + " rd on d.nzp_raj = rd.nzp_raj_dom");
                sql.Append(where.ToString());
                sql.Append(" limit 1");
#else
                sql.Append(" From " + tables.kvar + " k" +
                    ", outer " + tables.res_y + " t" +
                    ", outer " + tables.res_y + " t2" +
                    ", " + tables.dom + " d" +
                    ", " + tables.ulica + " u" +
                    ", outer ( " + tables.rajon + " r, outer  " + tables.town + " twn)" +
                    ", outer  " + tables.rajon_dom + " rd" +
                    ", outer " + tables.area + " a" +
                    ", outer " + tables.geu + " g");
                sql.Append(" where k.nzp_dom=d.nzp_dom and twn.nzp_town=r.nzp_town and d.nzp_ul=u.nzp_ul and u.nzp_raj=r.nzp_raj and a.nzp_area=k.nzp_area and  g.nzp_geu=k.nzp_geu ");
                sql.Append(" and k.typek = t.nzp_y and t.nzp_res = 9999 ");
                sql.Append(" and k.is_open = t2.nzp_y and t2.nzp_res = 18 ");
                sql.Append(" and d.nzp_raj = rd.nzp_raj_dom");
                sql.Append(where.ToString());
#endif


                mode = 0;
            }
            else if (finder.nzp_dom > 0)
            {
#if PG
                sql.Append("Select d.nzp_wp, d.nzp_dom, twn.nzp_town, u.nzp_raj, u.nzp_ul, a.nzp_area, g.nzp_geu, g.geu, a.area, ");
                sql.Append("trim(coalesce(u.ulicareg,'улица'))||' '||trim(coalesce(u.ulica,''))||' / '||trim(coalesce(r.rajon,''))||'   дом '|| ");
                sql.Append("trim(coalesce(d.ndom,''))||'  корп. '|| trim(coalesce(d.nkor,'')) as adr, ");
                sql.Append("trim(coalesce(ulicareg,'улица'))||' '||trim(coalesce(u.ulica,''))||' / '||trim(coalesce(r.rajon,'')) as ulica_rajon, ndom, nkor ");
                sql.Append(", trim(coalesce(ulicareg,'улица'))||' '||trim(coalesce(u.ulica,'')) as ulica2, u.ulica, u.ulicareg, r.rajon,s.stat");
                sql.Append(", rd.rajon_dom");
                sql.Append(", twn.town");
                sql.Append(", d.pref");
                sql.Append(", (select re.remark from " + tables.s_remark + " re where d.nzp_dom=re.nzp_dom) as remark");
                sql.Append(" From " + tables.dom + " d");
                sql.Append(" left outer join " + tables.ulica + " u on d.nzp_ul=u.nzp_ul");
                sql.Append(" left outer join " + tables.rajon + " r on r.nzp_raj=u.nzp_raj");
                sql.Append(" left outer join  " + tables.town + " twn on twn.nzp_town=r.nzp_town");
                sql.Append(" left outer join " + tables.stat + " s on s.nzp_stat=twn.nzp_stat");
                sql.Append(" left outer join " + tables.area + " a on a.nzp_area=d.nzp_area");
                sql.Append(" left outer join " + tables.geu + " g on g.nzp_geu=d.nzp_geu");
                sql.Append(" left outer join  " + tables.rajon_dom + " rd on d.nzp_raj = rd.nzp_raj_dom");
                sql.Append(" where d.nzp_dom = " + finder.nzp_dom);
#else
                sql.Append("Select d.nzp_wp, twn.nzp_town, d.nzp_dom, u.nzp_raj, u.nzp_ul, a.nzp_area, g.nzp_geu, g.geu, a.area, ");
                sql.Append("trim(nvl(u.ulicareg,'улица'))||' '||trim(nvl(u.ulica,''))||' / '||trim(nvl(r.rajon,''))||'   дом '|| ");
                sql.Append("trim(nvl(d.ndom,''))||'  корп. '|| trim(nvl(d.nkor,'')) as adr, ");
                sql.Append("trim(nvl(ulicareg,'улица'))||' '||trim(nvl(u.ulica,''))||' / '||trim(nvl(r.rajon,'')) as ulica_rajon, ndom, nkor ");
                sql.Append(", trim(nvl(ulicareg,'улица'))||' '||trim(nvl(u.ulica,'')) as ulica2, u.ulica, u.ulicareg, r.rajon");
                sql.Append(", rd.rajon_dom");
                sql.Append(", twn.town");
                sql.Append(", d.pref");
                sql.Append(", (select re.remark from " + tables.s_remark + " re where d.nzp_dom=re.nzp_dom) as remark");
                sql.Append(" From " + tables.dom + " d" +
                    ", " + tables.ulica + " u" +
                    ", outer (" + tables.rajon + " r, outer  " + tables.town + " twn)" +
                    ", outer " + tables.area + " a" +
                    ", outer " + tables.geu + " g" +
                    ", outer  " + tables.rajon_dom + " rd");
                sql.Append(" Where d.nzp_ul=u.nzp_ul and twn.nzp_town=r.nzp_town and r.nzp_raj=u.nzp_raj and a.nzp_area=d.nzp_area and  g.nzp_geu=d.nzp_geu ");
                sql.Append(" and d.nzp_raj = rd.nzp_raj_dom");
                sql.Append(" and d.nzp_dom = " + finder.nzp_dom);
#endif

                mode = 1;
            }
            else if (finder.nzp_ul > 0)
            {
#if PG
                sql.Append("select trim(coalesce(u.ulicareg,'улица'))||' '||trim(coalesce(u.ulica,''))||' / '||trim(coalesce(r.rajon,'')) as adr ");
                sql.AppendFormat("from {0} u", tables.ulica);
                sql.AppendFormat(" left outer join {0} r on u.nzp_raj = r.nzp_raj", tables.rajon);
                sql.AppendFormat(" left outer join {0} r on r.nzp_town = twn.nzp_town", tables.town);
                sql.Append(" where u.nzp_ul = " + finder.nzp_ul);
#else
                sql.Append("select trim(nvl(u.ulicareg,'улица'))||' '||trim(nvl(u.ulica,''))||' / '||trim(nvl(r.rajon,'')) as adr ");
                sql.Append("from " + tables.ulica + " u, outer (" + tables.rajon + " r, outer  " + tables.town + " twn)");
                sql.Append(" where u.nzp_raj = r.nzp_raj and r.nzp_town = twn.nzp_town");
                sql.Append(" and u.nzp_ul = " + finder.nzp_ul);
#endif

                mode = 2;
            }
            else if (finder.nzp_geu > 0)
            {
                sql.Append("select geu from " + tables.geu + " where nzp_geu = " + finder.nzp_geu);
                mode = 3;
            }
            else if (finder.nzp_area > 0)
            {
                sql.Append("select area from " + tables.area + " where nzp_area = " + finder.nzp_area);
                mode = 4;
            }



            if (sql.Length <= 0) return Listls;

            if (!ExecRead(conn_db, transaction, out reader, sql.ToString(), true).result)
            {
                return Listls;
            }

            try
            {
                if (reader.Read())
                {
                    Ls ls = new Ls();
                    ls.nzp_kvar = finder.nzp_kvar;
                    ls.nzp_dom = finder.nzp_dom;
                    ls.nzp_ul = finder.nzp_ul;
                    if (mode <= 2)
                    {
                        if (reader["adr"] == DBNull.Value) ls.adr = "";
                        else ls.adr = Convert.ToString(reader["adr"]);

                        if (mode <= 1)
                        {
                            if (reader["pref"] != DBNull.Value) ls.pref = ((string)reader["pref"]).Trim();
                            if (reader["stat"] != DBNull.Value) ls.stat = Convert.ToString(reader["stat"]).Trim();
                            if (reader["town"] != DBNull.Value) ls.town = Convert.ToString(reader["town"]).Trim();
                            if (reader["nzp_raj"] != DBNull.Value) ls.nzp_raj = Convert.ToInt32(reader["nzp_raj"]);
                            if (reader["nzp_ul"] != DBNull.Value) ls.nzp_ul = Convert.ToInt32(reader["nzp_ul"]);
                            if (reader["nzp_dom"] != DBNull.Value) ls.nzp_dom = (int)reader["nzp_dom"];
                            if (reader["ndom"] != DBNull.Value) ls.ndom = Convert.ToString(reader["ndom"]);
                            if (reader["nkor"] != DBNull.Value) ls.nkor = Convert.ToString(reader["nkor"]);
                            if (reader["nzp_area"] != DBNull.Value) ls.nzp_area = (int)reader["nzp_area"];
                            if (reader["nzp_geu"] != DBNull.Value) ls.nzp_geu = (int)reader["nzp_geu"];
                            if (reader["remark"] != DBNull.Value) ls.remark = (string)reader["remark"];
                            if (reader["nzp_town"] != DBNull.Value) ls.nzp_town = Convert.ToInt32(reader["nzp_town"]);
                            if (reader["nzp_wp"] != DBNull.Value) ls.nzp_wp = Convert.ToInt32(reader["nzp_wp"]);

                            if (mode == 0)
                            {
                                if (reader["rajon"] != DBNull.Value) ls.rajon = Convert.ToString(reader["rajon"]).Trim();
                                if (ls.rajon == "" || ls.rajon == "-")
                                {
                                    if (reader["rajon_dom"] != DBNull.Value) ls.rajon = Convert.ToString(reader["rajon_dom"]).Trim();
                                    if (reader["ulica"] != DBNull.Value) ls.ulica = Convert.ToString(reader["ulica"]).Trim();
                                }
                                else
                                {
                                    if (reader["ulica_rajon"] != DBNull.Value) ls.ulica = Convert.ToString(reader["ulica_rajon"]);
                                }
                            }
                            else if (mode == 1)
                            {
                                if (reader["rajon"] != DBNull.Value) ls.rajon = Convert.ToString(reader["rajon"]).Trim();
                                if (ls.rajon == "" || ls.rajon == "-")
                                {
                                    if (reader["rajon_dom"] != DBNull.Value) ls.rajon = Convert.ToString(reader["rajon_dom"]).Trim();
                                }
                                if (reader["ulica"] != DBNull.Value) ls.ulica = Convert.ToString(reader["ulica"]);
                                if (reader["ulicareg"] != DBNull.Value) ls.ulicareg = Convert.ToString(reader["ulicareg"]);

                                ls.adr = ls.getAddress();
                            }

                            if (mode == 0)
                            {
                                if (GlobalSettings.WorkOnlyWithCentralBank == false)
                                    if (finder.prms == Constants.act_mode_edit.ToString()) //если данные необходимы для режима изменения
                                    {
                                        #region Определить пользователя
                                        finder.pref = ls.pref;
                                        int nzpUser = finder.nzp_user; //локальный пользователь   

                                        /*DbWorkUser db = new DbWorkUser();
                                        int nzpUser = db.GetLocalUser(conn_db, transaction, finder, out ret); //локальный пользователь      
                                        db.Close();
                                        if (!ret.result) return Listls;*/
                                        #endregion

                                        #region проверить не заблокирован ли лс, если нет то заблокировать
                                        IDataReader reader2;
#if PG
                                        string sqltxt = "select nzp_user, dat_when,  (now() - INTERVAL " + string.Format("'{0} minutes')", Constants.users_min) + " as cur_date from " + ls.pref +
                                              "_data.kvar_block where nzp_kvar = " + finder.nzp_kvar + " order by dat_when desc";
#else
                                        string sqltxt = "select nzp_user, dat_when,  (current year to second - " + Constants.users_min.ToString() + " units minute) as cur_date from " + ls.pref +
                                              "_data:kvar_block where nzp_kvar = " + finder.nzp_kvar + " order by dat_when desc";
#endif

                                        if (!ExecRead(conn_db, transaction, out reader2, sqltxt, true).result)
                                        {
                                            return null;
                                        }

                                        try
                                        {
                                            DateTime datwhen = DateTime.MinValue;
                                            DateTime curdate = DateTime.MinValue;
                                            int nzpuser = 0;

                                            if (reader2.Read())
                                            {
                                                if (reader2["dat_when"] != DBNull.Value) datwhen = Convert.ToDateTime(reader2["dat_when"]);
                                                if (reader2["cur_date"] != DBNull.Value) curdate = Convert.ToDateTime(reader2["cur_date"]);
                                                if (reader2["nzp_user"] != DBNull.Value) nzpuser = Convert.ToInt32(reader2["nzp_user"]);

                                                if (nzpuser > 0 && datwhen != DateTime.MinValue) //заблокирован лицевой счет
                                                {
                                                    if (nzpuser != nzpUser && curdate <= datwhen) //если заблокирована запись другим пользователем и 20 мин не прошло
                                                        ls.is_blocked = 1;
                                                }
                                            }
                                            reader2.Close();

                                            if (ls.is_blocked != 1) //если л/с не заблокирован или заблокирован тем же пользователем
                                            {
                                                #region Удалить все записи для finder.nzp_kvar
#if PG
                                                ret = ExecSQL(conn_db, transaction, "delete from " + ls.pref + "_data.kvar_block where nzp_kvar = " + finder.nzp_kvar, true);
#else
                                                ret = ExecSQL(conn_db, transaction, "delete from " + ls.pref + "_data:kvar_block where nzp_kvar = " + finder.nzp_kvar, true);
#endif

                                                if (!ret.result)
                                                {
                                                    ret.result = false;
                                                    ret.text = "Ошибка удаления из таблицы kvar_block";
                                                    return Listls;
                                                }
                                                #endregion

                                                #region Заблокировать л/с
#if PG
                                                ret = ExecSQL(conn_db, transaction, "insert into " + ls.pref + "_data.kvar_block (nzp_kvar,nzp_user, dat_when,kod) values(" +
                                                                                    finder.nzp_kvar + "," + nzpUser + ",now()," + Constants.ist + ")", true);
#else
                                                ret = ExecSQL(conn_db, transaction, "insert into " + ls.pref + "_data:kvar_block (nzp_kvar,nzp_user, dat_when,kod) values(" +
                                                                                    finder.nzp_kvar + "," + nzpUser + ",current year to second," + Constants.ist + ")", true);
#endif

                                                if (!ret.result)
                                                {
                                                    ret.result = false;
                                                    ret.text = "Ошибка добавления записи о блокировке в таблицу kvar_block";
                                                    return Listls;
                                                }
                                                #endregion
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            reader.Close();
                                            ret.result = false;
                                            ret.text = ex.Message;
                                            string err;
                                            if (Constants.Viewerror) err = " \n " + ex.Message;
                                            else err = "";
                                            MonitorLog.WriteLog("Ошибка получения информации о блокировки пользователя " + err, MonitorLog.typelog.Error, 20, 201, true);
                                            return Listls;
                                        }
                                        #endregion
                                    }

                                if (reader["num_ls"] == DBNull.Value) ls.num_ls = 0;
                                else ls.num_ls = (int)(reader["num_ls"]);

                                if (reader["pkod10"] == DBNull.Value) ls.pkod10 = 0;
                                else ls.pkod10 = (int)(reader["pkod10"]);

                                if (!GlobalSettings.NewGeneratePkodMode)
                                {
                                    if (reader["spkod"] == DBNull.Value) ls.pkod = "0";
                                    else ls.pkod = Convert.ToString(reader["spkod"]).Trim();
                                }
                                else
                                {
                                    sql.Remove(0, sql.Length);
                                    sql.AppendFormat("select pkod from {0}_data{1}kvar_pkodes where nzp_kvar = {2} and is_princip = 0 and is_default = 1", Points.Pref, tableDelimiter, finder.nzp_kvar);
                                    ret = ExecRead(conn_db, transaction, out reader22, sql.ToString(), true);
                                    if (!ret.result)
                                    {
                                        reader22.Close();
                                        return Listls;
                                    }
                                    ls.pkod = "";
                                    if (reader22.Read())
                                    {
                                        if (reader22["pkod"] != DBNull.Value)
                                            if (ls.pkod == "") ls.pkod += Convert.ToString(reader22["pkod"]);
                                            else ls.pkod += ", " + Convert.ToString(reader22["pkod"]);
                                    }
                                    reader22.Close();
                                }

                                if (Points.IsSmr)
                                {
                                    //int litera = GetLitera(finder, out ret, conn_db, transaction);
                                    //if (litera > 0) ls.num_ls_litera = ls.pkod10.ToString() + " " + litera.ToString();
                                    //else ls.num_ls_litera = ls.pkod10.ToString();

                                    if (ls.pkod == null || ls.pkod.Length != 13 || ls.pkod.Substring(10, 1) == "0")
                                        ls.num_ls_litera = ls.pkod10.ToString();
                                    else ls.num_ls_litera = ls.pkod10 + " " + ls.pkod.Substring(10, 1);
                                }

                                if (reader["fio"] == DBNull.Value) ls.fio = "";
                                else ls.fio = Convert.ToString(reader["fio"]);
                                /*ls.fio = "xxxxxxxx xxxxxx xxxxxx";*/

                                if (reader["typek"] != DBNull.Value) ls.typek = (int)reader["typek"];

                                if (reader["stypek"] == DBNull.Value) ls.stypek = "";
                                else ls.stypek = Convert.ToString(reader["stypek"]);

                                if (reader["is_open"] != DBNull.Value)
                                {
                                    int stateID;
                                    if (Int32.TryParse(((string)reader["is_open"]).Trim(), out stateID))
                                        ls.stateID = stateID;
                                    else
                                    {
                                        ls.stateID = 0;
                                    }
                                }
                                if (reader["state"] != DBNull.Value) ls.state = ((string)reader["state"]).Trim();

                                if (reader["nzp_kvar"] == DBNull.Value) ls.nzp_kvar = 0;
                                else ls.nzp_kvar = Convert.ToInt32(reader["nzp_kvar"]);

                                if (reader["nkvar"] != DBNull.Value) ls.nkvar = Convert.ToString(reader["nkvar"]);
                                if (reader["nkvar_n"] != DBNull.Value) ls.nkvar_n = Convert.ToString(reader["nkvar_n"]);
                                if (reader["phone"] != DBNull.Value) ls.phone = ((string)reader["phone"]).Trim();
                                if (reader["porch"] != DBNull.Value) ls.porch = Convert.ToString(reader["porch"]).Trim();
                                if (reader["uch"] != DBNull.Value) ls.uch = ((int)reader["uch"]).ToString();
                                if (reader["remark"] != DBNull.Value) ls.remark = ((string)reader["remark"]).Trim();
                            }
                        }
                    }

                    if (mode == 0 || mode == 1 || mode == 3)
                    {
                        if (reader["geu"] == DBNull.Value) ls.geu = "";
                        else ls.geu = Convert.ToString(reader["geu"]);
                    }

                    if (mode == 0 || mode == 1 || mode == 4)
                    {
                        if (reader["area"] == DBNull.Value) ls.area = "";
                        else ls.area = Convert.ToString(reader["area"]);
                    }

                    /*
                    IDataReader readerSD;
                    if (!ExecRead(conn_db, out readerSD, "select dat_saldo from " + pref + "_data:saldo_date where iscurrent = 0", true).result)
                    {
                            conn_db.Close();
                            return Listls;
                    }
                    string saldo_date = "";
                    while (readerSD.Read())
                        if (readerSD["dat_saldo"] != DBNull.Value)
                            saldo_date =String.Format("{0:dd.MM.yyyy}",readerSD["dat_saldo"]);

                    if (saldo_date != "")
                    {
                        IDataReader readerDO;
                        string s = "select dat_s from " + pref + "_data:prm_3 where nzp_prm=51 and val_prm=1 and is_actual<>100 and nzp=" +
                            Convert.ToString(ls.nzp_kvar) + " and \"" + saldo_date + "\" between  dat_s and  dat_po";
                        if (!ExecRead(conn_db, out readerDO,s , true).result)
                        {
                            conn_db.Close();
                            return Listls;
                        }

                        while (readerDO.Read())
                            if (readerDO["dat_s"] != DBNull.Value)
                                ls.dat_open = String.Format("{0:dd.MM.yyyy}", readerDO["dat_s"]);
                    }
                    */

                    if (mode == 0 && !GlobalSettings.WorkOnlyWithCentralBank)
                    {
                        ls.pref = ls.pref;
                        ls.stateValidOn = finder.stateValidOn;
                        using (var dbAdresClient = new DbAdres())
                        {
                            ret = dbAdresClient.LoadLsState(ls, conn_db, transaction);
                        }
                        if (!ret.result)
                        {
                            reader.Close();
                            return Listls;
                        }

                        DateTime d;
                        // дата последнего расчета лицевого счета
                        if (DateTime.TryParse(finder.dat_calc, out d))
                        {
                            string kvar_calc = ls.pref + "_charge_" + (d.Year % 100).ToString("00") + DBManager.tableDelimiter+"kvar_calc_" + d.Month.ToString("00");

                            if (TempTableInWebCashe(conn_db, transaction, kvar_calc))
                            {
                                string s = "select dat_calc from " + kvar_calc + " where nzp_kvar = " + finder.nzp_kvar;
                                IDataReader readerDO;
                                ret = ExecRead(conn_db, transaction, out readerDO, s, true);
                                if (!ret.result)
                                {
                                    reader.Close();
                                    return Listls;
                                }
                                if (readerDO.Read())
                                {
                                    if (readerDO["dat_calc"] != DBNull.Value) ls.dat_calc = Convert.ToDateTime(readerDO["dat_calc"]).ToShortDateString();
                                }
                            }
                        }
                    }

                    Listls.Add(ls);
                    ret.result = true;
                }

                reader.Close();
            }
            catch (Exception ex)
            {
                reader.Close();
                reader.Dispose();

                ret.result = false;
                ret.text = ex.Message;

                string err;
                if (Constants.Viewerror) err = " \n " + ex.Message;
                else err = "";

                MonitorLog.WriteLog("Ошибка заполнения Адреса " + err, MonitorLog.typelog.Error, 20, 201, true);
            }

            if (ret.result)
            {
                if (!GlobalSettings.WorkOnlyWithCentralBank)
                    if (Listls != null && Listls.Count > 0 /*&& (finder.is_pasportist > 0 || Points.Region == Regions.Region.Samarskaya_obl)*/)
                    {
                        Returns ret2 = Utils.InitReturns();
                        List<Prm> list = new List<Prm>();
                        Prm finderprm = new Prm();
                        finderprm.checkDataBlocking = 0; // не проверять блокировку
                        finderprm.pref = Listls[0].pref;
                        RecordMonth r_m = Points.GetCalcMonth(new CalcMonthParams(finderprm.pref));
                        finderprm.month_ = r_m.month_;
                        finderprm.year_ = r_m.year_;


                        if (finder.nzp_kvar > 0)
                        {
                            finderprm.prm_num = 1;
                            finderprm.nzp = finder.nzp_kvar;
                        }
                        else
                        {
                            finderprm.prm_num = 2;
                            finderprm.nzp = finder.nzp_dom;
                        }

                        if (finder.nzp_kvar > 0) finderprm.nzp_prm = 4;
                        else finderprm.nzp_prm = 40;

                        DbParameters dbparam = new DbParameters();
                        Prm prm = dbparam.FindPrmValue(conn_db, transaction, finderprm, out ret2);
                        if (prm == null) prm = new Prm();
                        prm.nzp_prm = finderprm.nzp_prm;
                        list.Add(prm);

                        if (finder.nzp_kvar > 0) finderprm.nzp_prm = 6;
                        else finderprm.nzp_prm = 36;
                        prm = dbparam.FindPrmValue(conn_db, transaction, finderprm, out ret2);
                        if (prm == null) prm = new Prm();
                        prm.nzp_prm = finderprm.nzp_prm;
                        list.Add(prm);

                        if (finder.nzp_kvar > 0)
                        {
                            finderprm.nzp_prm = (int) ParamIds.LsParams.OtopSqu;
                            prm = dbparam.FindPrmValue(conn_db, transaction, finderprm, out ret2);
                            if (prm == null) prm = new Prm();
                            prm.nzp_prm = finderprm.nzp_prm;
                            list.Add(prm);
                        }

                        DbAdres dbAdres = new DbAdres();
                        if (finder.num_page == Constants.page_carddom)//реквизиты дома
                        {
                            finderprm.nzp_prm = 2049;//МОП площадь
                            prm = dbparam.FindPrmValue(conn_db, transaction, finderprm, out ret2);
                            if (prm == null) prm = new Prm();
                            prm.nzp_prm = finderprm.nzp_prm;
                            list.Add(prm);

                            finderprm.nzp_prm = 4;
                            prm = dbAdres.FindPrmValueForOpenLs(conn_db, transaction, finderprm, out ret2);
                            if (prm == null) prm = new Prm();
                            prm.nzp_prm = finderprm.nzp_prm;
                            list.Add(prm);

                            finderprm.nzp_prm = 5;
                                prm = dbAdres.FindPrmValueForOpenLs(conn_db, transaction, finderprm, out ret2);
                            if (prm == null) prm = new Prm();
                            prm.nzp_prm = finderprm.nzp_prm;
                            list.Add(prm);

                            finderprm.nzp_prm = 1010270;
                            prm = dbAdres.FindPrmValueForOpenLs(conn_db, transaction, finderprm, out ret2);
                            if (prm == null) prm = new Prm();
                            prm.nzp_prm = finderprm.nzp_prm;
                            list.Add(prm);
                        }
                        else
                        {
                            finderprm.nzp_prm = 8; //приватизация
                            prm = dbparam.FindPrmValue(conn_db, transaction, finderprm, out ret2);
                            if (prm == null) prm = new Prm();
                            prm.nzp_prm = finderprm.nzp_prm;
                            list.Add(prm);

                            finderprm.nzp_prm = 572; //лоджии
                            prm = dbparam.FindPrmValue(conn_db, transaction, finderprm, out ret2);
                            if (prm == null) prm = new Prm();
                            prm.nzp_prm = finderprm.nzp_prm;
                            list.Add(prm);
                            finderprm.nzp_prm = 573; //лоджии
                            prm = dbparam.FindPrmValue(conn_db, transaction, finderprm, out ret2);
                            if (prm == null) prm = new Prm();
                            prm.nzp_prm = finderprm.nzp_prm;
                            list.Add(prm);
                            finderprm.nzp_prm = 574; //лоджии
                            prm = dbparam.FindPrmValue(conn_db, transaction, finderprm, out ret2);
                            if (prm == null) prm = new Prm();
                            prm.nzp_prm = finderprm.nzp_prm;
                            list.Add(prm);

                            finderprm.nzp_prm = 3;//комфортность
                            prm = dbparam.FindPrmValue(conn_db, transaction, finderprm, out ret2);
                            if (prm == null) prm = new Prm();
                            prm.nzp_prm = finderprm.nzp_prm;
                            list.Add(prm);

                            finderprm.nzp_prm = 107;//количество комнат в квартире
                            prm = dbparam.FindPrmValue(conn_db, transaction, finderprm, out ret2);
                            if (prm == null) prm = new Prm();
                            prm.nzp_prm = finderprm.nzp_prm;
                            list.Add(prm);

                            finderprm.nzp_prm = 2009;//статус жилья
                            prm = dbparam.FindPrmValue(conn_db, transaction, finderprm, out ret2);
                            if (prm == null) prm = new Prm();
                            prm.nzp_prm = finderprm.nzp_prm;
                            list.Add(prm);

                            finderprm.nzp_prm = 5;//количество проживающих
                            if (finder.dopUsl != null && finder.dopUsl.Count == 1 && finder.dopUsl.FirstOrDefault() == "AddressTemplate")
                                prm = GetGilNumberLiveFromGilXX(conn_db, transaction, finder);
                            else
                            prm = dbparam.FindPrmValue(conn_db, transaction, finderprm, out ret2);
                            if (prm == null) prm = new Prm();
                            prm.nzp_prm = finderprm.nzp_prm;
                            list.Add(prm);

                            finderprm.nzp_prm = 1010270;//колич прописанных
                            prm = dbparam.FindPrmValue(conn_db, transaction, finderprm, out ret2);
                            if (prm == null) prm = new Prm();
                            prm.nzp_prm = finderprm.nzp_prm;
                            list.Add(prm);

                            finderprm.nzp_prm = 17;//водопровод
                            finderprm.prm_num = 1;
                            finderprm.nzp = finder.nzp_kvar;
                            finderprm.type_prm = "bool";
                            prm = dbparam.FindPrmValue(conn_db, transaction, finderprm, out ret2);
                            if (prm == null) prm = new Prm();
                            prm.nzp_prm = finderprm.nzp_prm;
                            list.Add(prm);

                            finderprm.nzp_prm = 15;//канализация
                            finderprm.prm_num = 1;
                            finderprm.nzp = finder.nzp_kvar;
                            finderprm.type_prm = "bool";
                            prm = dbparam.FindPrmValue(conn_db, transaction, finderprm, out ret2);
                            if (prm == null) prm = new Prm();
                            prm.nzp_prm = finderprm.nzp_prm;
                            list.Add(prm);

                            finderprm.nzp_prm = 14;//лифт
                            finderprm.prm_num = 1;
                            finderprm.nzp = finder.nzp_kvar;
                            finderprm.type_prm = "bool";
                            prm = dbparam.FindPrmValue(conn_db, transaction, finderprm, out ret2);
                            if (prm == null) prm = new Prm();
                            prm.nzp_prm = finderprm.nzp_prm;
                            list.Add(prm);

                            finderprm.nzp_prm = 37;//этажность
                            finderprm.prm_num = 2;
                            finderprm.nzp = finder.nzp_dom;
                            finderprm.type_prm = "";
                            prm = dbparam.FindPrmValue(conn_db, transaction, finderprm, out ret2);
                            if (prm == null) prm = new Prm();
                            prm.nzp_prm = finderprm.nzp_prm;
                            list.Add(prm);

                            finderprm.nzp_prm = 150;//год постройки
                            finderprm.prm_num = 2;
                            finderprm.nzp = finder.nzp_dom;
                            finderprm.type_prm = "";
                            prm = dbparam.FindPrmValue(conn_db, transaction, finderprm, out ret2);
                            if (prm == null) prm = new Prm();
                            prm.nzp_prm = finderprm.nzp_prm;
                            list.Add(prm);

                            finderprm.nzp_prm = 1240;//тип отопления
                            finderprm.prm_num = 1;
                            finderprm.nzp = finder.nzp_kvar;
                            finderprm.type_prm = "sprav";
                            finderprm.nzp_res = 3018;
                            prm = dbparam.FindPrmValue(conn_db, transaction, finderprm, out ret2);
                            if (prm == null) prm = new Prm();
                            prm.nzp_prm = finderprm.nzp_prm;
                            list.Add(prm);

                            finderprm.nzp_prm = 2009;//статус жилья
                            finderprm.prm_num = 1;
                            finderprm.nzp = finder.nzp_kvar;
                            finderprm.type_prm = "sprav";
                            finderprm.nzp_res = 3001;
                            prm = dbparam.FindPrmValue(conn_db, transaction, finderprm, out ret2);
                            if (prm == null) prm = new Prm();
                            prm.nzp_prm = finderprm.nzp_prm;
                            list.Add(prm);

                            finderprm.nzp_prm = 1241;//Наличие электроосвещения
                            finderprm.prm_num = 1;
                            finderprm.nzp = finder.nzp_kvar;
                            finderprm.type_prm = "bool";
                            prm = dbparam.FindPrmValue(conn_db, transaction, finderprm, out ret2);
                            if (prm == null) prm = new Prm();
                            prm.nzp_prm = finderprm.nzp_prm;
                            list.Add(prm);

                            finderprm.nzp_prm = 1242;//Наличие ванны
                            finderprm.prm_num = 1;
                            finderprm.nzp = finder.nzp_kvar;
                            finderprm.type_prm = "bool";
                            prm = dbparam.FindPrmValue(conn_db, transaction, finderprm, out ret2);
                            if (prm == null) prm = new Prm();
                            prm.nzp_prm = finderprm.nzp_prm;
                            list.Add(prm);

                            finderprm.nzp_prm = 1243;//Наличие газа
                            finderprm.prm_num = 1;
                            finderprm.nzp = finder.nzp_kvar;
                            finderprm.type_prm = "bool";
                            prm = dbparam.FindPrmValue(conn_db, transaction, finderprm, out ret2);
                            if (prm == null) prm = new Prm();
                            prm.nzp_prm = finderprm.nzp_prm;
                            list.Add(prm);

                            finderprm.nzp_prm = 1244;//Тип ГВС
                            finderprm.prm_num = 1;
                            finderprm.nzp = finder.nzp_kvar;
                            finderprm.type_prm = "sprav";
                            finderprm.nzp_res = 3019;
                            prm = dbparam.FindPrmValue(conn_db, transaction, finderprm, out ret2);
                            if (prm == null) prm = new Prm();
                            prm.nzp_prm = finderprm.nzp_prm;
                            list.Add(prm);

                            finderprm.nzp_prm = 1373;//Тип собст
                            finderprm.prm_num = 1;
                            finderprm.nzp = finder.nzp_kvar;
                            finderprm.type_prm = "sprav";
                            finderprm.nzp_res = 3017;
                            prm = dbparam.FindPrmValue(conn_db, transaction, finderprm, out ret2);
                            if (prm == null) prm = new Prm();
                            prm.nzp_prm = finderprm.nzp_prm;
                            list.Add(prm);

                            finderprm.nzp_prm = 35;//Открытый водозабор горячей воды
                            finderprm.prm_num = 2;
                            finderprm.nzp = finder.nzp_dom;
                            finderprm.type_prm = "bool";
                            prm = dbparam.FindPrmValue(conn_db, transaction, finderprm, out ret2);
                            if (prm == null) prm = new Prm();
                            prm.nzp_prm = finderprm.nzp_prm;
                            list.Add(prm);
                        }
                        dbAdres.Close();
                        Listls[0].dopParams = list;
                        Listls[0].is_pasportist = finder.is_pasportist;
                    }
            }
            return Listls;
        }
        /// <summary>
        /// берем количество зарегистрированных жильцов из gil_xx #141528
        /// </summary>
        /// <param name="conn_db"></param>
        /// <param name="transaction"></param>
        /// <param name="finder"></param>
        /// <returns></returns>
        private Prm GetGilNumberLiveFromGilXX(IDbConnection conn_db, IDbTransaction transaction, Ls finder)
        {
            Prm prm = new Prm();
            Returns ret = Utils.InitReturns();
            CalcMonthParams cmParams = new CalcMonthParams(finder.pref);
            StringBuilder sql = new StringBuilder();
            RecordMonth rm = Points.GetCalcMonth(cmParams);
            string gil_xx = finder.pref + "_charge_" + (rm.year_ - 2000).ToString("00") + 
                DBManager.tableDelimiter + "gil_" + rm.month_.ToString("00");
            sql.AppendFormat("select cnt2 from {0} where stek = 3 and nzp_kvar = {1}", gil_xx, finder.nzp_kvar);
            var value = ExecScalar(conn_db, transaction, sql.ToString(), out ret, true);
            prm.val_prm = value != null ? value.ToString() : "";

            return prm;
        }


        //----------------------------------------------------------------------
        /// <summary>
        /// найти и заполнить адрес
        /// </summary>
        /// <param name="finder">если заполнено поле nzp_kvar - информация о л/с,
        /// nzp_dom - информация о доме, nzp_ul - об улице, nzp_area - о Управляющая организация,
        /// nzp_geu - о ЖЭУ
        /// </param>
        /// <param name="ret">результат</param>
        /// <returns>список объектов Ls</returns>
        public List<Ls> LoadLs(Ls finder, out Returns ret) //найти и заполнить адрес для nzp_kvar
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            if (finder.nzp_user < 1)
            {
                ret.result = false;
                ret.text = "Не определен пользователь";
                return null;
            }

            List<Ls> Listls = new List<Ls>();

            string pref = finder.pref == "" ? Points.Pref : finder.pref;

            #region соединение с БД
            string conn_kernel = Points.GetConnByPref(pref);
            IDbConnection conn_db = GetConnection(conn_kernel);
            //IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return Listls;
            #endregion

            Listls = LoadLs(conn_db, null, finder, out ret);

            conn_db.Close();
            return Listls;
        }

        /// <summary>
        /// Генерирует лицевые счета (с приборами учета) или приборы учета
        /// </summary>
        /// <param name="kvar">Если задан, генерируются лицевые счета</param>
        /// <param name="CounterList">Если задан, генерируются приборы учета</param>
        /// <param name="ret"></param>
        /// <returns></returns>
        public Returns GenerateLsPu(Ls kvar, List<Counter> CounterList)
        {
            #region Проверка входных параметров
            string sPref = "";
            int nzpUser = 0;
            if (CounterList != null)
            {
                if (CounterList.Count == 0)
                {
                    return new ReturnsType() { result = false, tag = -1, text = "Список данных для генерации счетчиков пуст" }.GetReturns();
                }
                if (CounterList[0].nzp_user < 1)
                {
                    return new ReturnsType() { result = false, tag = -1, text = "Не определен пользователь" }.GetReturns();
                }
                else nzpUser = CounterList[0].nzp_user;

                if (CounterList[0].pref.Trim() == "")
                {
                    return new ReturnsType() { result = false, tag = -1, text = "Не определен префикс базы данных" }.GetReturns();
                }
                else sPref = CounterList[0].pref.Trim();
            }
            if (kvar != null)
            {
                if (kvar.nzp_user < 1)
                {
                    return new ReturnsType() { result = false, tag = -1, text = "Не определен пользователь" }.GetReturns();
                }
                else nzpUser = kvar.nzp_user;

                if (kvar.pref.Trim() == "")
                {
                    return new ReturnsType() { result = false, tag = -1, text = "Не определен префикс базы данных" }.GetReturns();
                }
                else sPref = kvar.pref.Trim();
            }
            if (nzpUser < 1)
            {
                return new ReturnsType() { result = false, tag = -1, text = "Не определен пользователь" }.GetReturns();
            }
            if (sPref.Trim() == "")
            {
                return new ReturnsType() { result = false, tag = -1, text = "Не определен префикс базы данных" }.GetReturns();
            }
            #endregion

            #region Инициализация переменых
            Returns ret = Utils.InitReturns();
            int inKvar = 0;
            int inKvar_po = 0;
            #endregion

            if (kvar != null)
            {
                #region Проверка входных параметров
                try { inKvar = Int32.Parse(kvar.nkvar); }
                catch
                {
                    return new ReturnsType() { result = false, tag = -1, text = "Некорректный интервал для генерации л/c" }.GetReturns();
                }
                try { inKvar_po = Int32.Parse(kvar.nkvar_po); }
                catch
                {
                    return new ReturnsType() { result = false, tag = -1, text = "Некорректный интервал для генерации л/c" }.GetReturns();
                }

                if ((kvar.gen_pu == 1) && (CounterList == null))
                {
                    return new ReturnsType() { result = false, tag = -1, text = "Не задан список услуг для генерации приборов учета" }.GetReturns();
                }
                #endregion

                #region Для копирования характеристик
                Ls from = new Ls();
                if (kvar.copy_ls == 1)
                {
                    kvar.CopyTo(from);
                    RecordMonth r_m = Points.GetCalcMonth(new CalcMonthParams(kvar.pref));
                    from.stateValidOn = new DateTime(r_m.year_, r_m.month_, 1).ToString("dd.MM.yyyy");
                    //from.num_ls = kvar.copy_ls_from;
                    from.nzp_kvar = kvar.copy_ls_from;
                    from.pref = "";

                    List<Ls> list = LoadLs(from, out ret);
                    if (!ret.result)
                    {
                        return ret;
                    }
                    if ((list == null) || (list.Count == 0))
                    {
                        return new ReturnsType() { result = false, tag = -1, text = "Не указан лицевой счет, характеристики которого необходимо копировать" }.GetReturns();
                    }
                    from.nzp_kvar = list[0].nzp_kvar;
                    from.pref = list[0].pref;
                }
                #endregion

                #region Подключение к БД
                string connectionString = Points.GetConnByPref(sPref);
                IDbConnection conn_db = GetConnection(connectionString);
                ret = OpenDb(conn_db, true);
                if (!ret.result)
                {
                    return ret;
                }
                IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
                ret = OpenDb(conn_web, true);
                if (!ret.result)
                {
                    conn_db.Close();
                    return ret;
                }
                #endregion

                string tXX_spls = "t" + Convert.ToString(kvar.nzp_user) + "_spls";
                bool isListLsInCacheExists = TableInWebCashe(conn_web, tXX_spls);

                IDbTransaction transaction = null;
                ///try { transaction = conn_db.BeginTransaction(); }
                ///catch { transaction = null; }
                try
                {
                    for (int iCount = inKvar; iCount <= inKvar_po; iCount++)
                    {
                        transaction = conn_db.BeginTransaction();

                        #region Создание Л/c
                        kvar.nzp_kvar = Constants._ZERO_;
                        kvar.num_ls = Constants._ZERO_;
                        kvar.chekexistls = 1;
                        kvar.nkvar = iCount.ToString();

                        kvar.nzp_kvar = Update(conn_db, transaction, conn_web, kvar, out ret);

                        if (!ret.result)
                            if (ret.tag == -11)
                            {
                                transaction.Rollback();
                                return new ReturnsType() { result = false, tag = -11, text = "Л/с по этому адресу уже существует!" }.GetReturns();
                            }
                        #endregion

                        #region сохранить состояние ЛС
                        /*Param prm = new Param();
                    prm.dat_s = new DateTime(Points.CalcMonth.year_, Points.CalcMonth.month_, 1).ToShortDateString();
                    prm.nzp_user = kvar.nzp_user;

                    prm.pref = kvar.pref;
                    prm.nzp = kvar.nzp_kvar;
                    prm.nzp_prm = 51;
                    prm.val_prm = kvar.stateID.ToString();
                    prm.prm_num = 3;

                    DbParameters dbprm = new DbParameters();
                    ret = dbprm.SavePrm(prm, conn_web);
                    if (!ret.result) return Constants._ZERO_;*/
                        #endregion

                        #region Копирование характеристик
                        if (kvar.copy_ls == 1)
                        {
                            DbParameters dbparam = new DbParameters();
                            ret = dbparam.CopyLsParams(conn_db, transaction, from, kvar);
                            if (!ret.result)
                            {
                                transaction.Rollback();
                                return ret;
                            }
                        }
                        #endregion

                        #region Создание ПУ
                        if (kvar.gen_pu == 1)
                        {
                            #region Цикл по счетчикам
                            for (int iCnt1 = 0; iCnt1 < CounterList.Count; ++iCnt1)
                            {
                                int iCount2 = CounterList[iCnt1].cnt_ls;
                                if (iCount2 == 0) iCount2 = 1;

                                Counter newCounter = new Counter();
                                CounterList[iCnt1].CopyTo(newCounter);
                                newCounter.nzp_type = 3;
                                newCounter.cnt_ls = 0;
                                newCounter.nzp = kvar.nzp_kvar;
                                newCounter.nzp_kvar = kvar.nzp_kvar;
                                newCounter.nzp_serv = CounterList[iCnt1].nzp_serv;
                                newCounter.nzp_cnt = CounterList[iCnt1].nzp_cnt;
                                newCounter.nzp_cnttype = CounterList[iCnt1].nzp_cnttype;
                                newCounter.nzp_user = CounterList[iCnt1].nzp_user;
                                newCounter.pref = sPref;

                                for (int iCnt2 = 1; iCnt2 <= iCount2; ++iCnt2)
                                {
                                    DbCounter dbcounter = new DbCounter();
                                    newCounter.nzp_counter = 0;

                                    if (newCounter.nzp_serv > 0)
                                        newCounter.num_cnt = "К-" + kvar.nzp_kvar.ToString() + "-" + newCounter.nzp_serv.ToString() + "-" + iCnt2.ToString();
                                    else
                                        newCounter.num_cnt = "К-" + kvar.nzp_kvar.ToString() + "-C" + newCounter.nzp_cnt.ToString() + "-" + iCnt2.ToString();
                                    RecordMonth r_m = Points.GetCalcMonth(new CalcMonthParams(sPref));
                                    ret = dbcounter.SaveCounter(conn_db, transaction, newCounter, new DateTime(r_m.year_, r_m.month_, 1).ToString("dd.MM.yyyy"));

                                    if (!ret.result)
                                    {
                                        return ret;
                                    }
                                }
                            }
                            #endregion
                        }
                        #endregion

                        //добавление ЛС в выбранный список
                        if (isListLsInCacheExists)
                        {
                            AddLsToCache(conn_db, transaction, conn_web, kvar);
                        }

                        transaction.Commit();
                        transaction = null;
                    }
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = ex.Message;
                    MonitorLog.WriteLog(ex.Message, MonitorLog.typelog.Error, true);
                    if (transaction != null) transaction.Rollback();
                }
                finally
                {
                    conn_db.Close();
                    conn_web.Close();
                }
            }
            else
            {
                //теперь здесь отдельная генерация пу не производится не делается
            }

            return ret;
        }

        /// <summary>
        /// Генерация ИПУ (используется в групповых операциях по ЛС)
        /// </summary>
        /// <param name="CounterList"></param>
        /// <returns></returns>
        public Returns GenerateGroupLsPu(Finder finder, CalcFonTask calcfon = null)
        {
            string sPref = "";
            Returns ret = Utils.InitReturns();

            #region Проверка входных параметров

            if (finder.nzp_user < 1)
            {
                MonitorLog.WriteLog("Не определен пользователь " +
                                    System.Reflection.MethodBase.GetCurrentMethod().Name, MonitorLog.typelog.Error, true);
                return new ReturnsType() {result = false, tag = -1, text = "Не определен пользователь"}.GetReturns();
            }

            if (finder.pref.Trim() == "")
            {
                MonitorLog.WriteLog("Не определен префикс базы данных " +
                                    System.Reflection.MethodBase.GetCurrentMethod().Name, MonitorLog.typelog.Error, true);
                return new ReturnsType() {result = false, tag = -1, text = "Не определен префикс базы данных"}.GetReturns();
            }
            sPref = finder.pref.Trim();

            #endregion

            #region Подключение к БД

            string connectionString = Points.GetConnByPref(sPref);
            IDbConnection conn_db = GetConnection(connectionString);
            ret = OpenDb(conn_db, true);
            if (!ret.result)
            {
                MonitorLog.WriteLog("Ошибка подключения к БД " +
                                    System.Reflection.MethodBase.GetCurrentMethod().Name, MonitorLog.typelog.Error, true);
                return ret;
            }

            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result)
            {
                MonitorLog.WriteLog("Ошибка подключения к web БД " +
                                    System.Reflection.MethodBase.GetCurrentMethod().Name, MonitorLog.typelog.Error, true);
                conn_db.Close();
                return ret;
            }

            #endregion

            string sql;
            IDbTransaction transaction = null;
            List<Counter> CounterList = new List<Counter>();
            try
            {
                #region Подгружаем данные по счетчикам из таблицы в public

                string tXX_gengrouplspu = DBManager.sDefaultSchema + "t" + finder.nzp_user + "_gengrouplspu" + finder.listNumber;

                if (TableInWebCashe(conn_web, "t" + finder.nzp_user + "_gengrouplspu" + finder.listNumber))
                {
                    sql = " SELECT * FROM " + tXX_gengrouplspu;
                    DataTable CountersDt = DBManager.ExecSQLToTable(conn_db, sql);
                    if (CountersDt.Rows.Count == 0)
                    {
                        MonitorLog.WriteLog("Список данных для генерации счетчиков пуст " +
                                            System.Reflection.MethodBase.GetCurrentMethod().Name,
                            MonitorLog.typelog.Error, true);
                        return new ReturnsType() {result = false,tag = -1,text = "Список данных для генерации счетчиков пуст"}.GetReturns();
                    }
                    foreach (DataRow row in CountersDt.Rows)
                    {
                        Counter counter = new Counter
                        {
                            cnt_ls = row["cnt_ls"].ToInt(),
                            nzp_cnt = row["nzp_cnt"].ToInt(),
                            nzp_cnttype = row["nzp_cnttype"].ToInt(),
                            nzp_serv = row["nzp_serv"].ToInt(),
                            prm = row["nzp_room"].ToString().Trim()
                        };
                        CounterList.Add(counter);
                    }
                }
                else
                {
                    MonitorLog.WriteLog("Не найдена таблица с данными, какие счетчики надо создавать " +
                                        System.Reflection.MethodBase.GetCurrentMethod().Name, MonitorLog.typelog.Error,true);
                    return new ReturnsType() {result = false, tag = -1, text = "Ошибка генерации ИПУ. Смотрите логи"}.GetReturns();
                }
                #endregion

                #region создаем счетчики

                string tXX_selectedls = "t" + Convert.ToString(finder.nzp_user) + "_selectedls" + finder.listNumber;

                if (TableInWebCashe(conn_web, tXX_selectedls))
                {
                    //для выставления процентов в фоновую задачу
                    int countKvar = 0;
                    sql = " SELECT DISTINCT t.nzp_kvar " +
                          " FROM " + tXX_selectedls + " t," +
                          sPref + sDataAliasRest + "prm_3 p " +
                          " WHERE pref = '" + sPref + "' AND p.nzp_prm = 51" +
                          " AND trim(p.val_prm) = '1' AND p.is_actual = 1" +
                          " AND '" + Points.GetCalcMonth(new CalcMonthParams(sPref)).RecordDateTime.ToShortDateString() + "'" +
                          " BETWEEN dat_s AND dat_po AND p.nzp = t.nzp_kvar";
                    DataTable dt = DBManager.ExecSQLToTable(conn_db, sql);
                    foreach (DataRow rowKvar in dt.Rows)
                    {
                        DbCharge dbcharge = new DbCharge();
                        if (calcfon != null)
                            dbcharge.SetTaskProgress(calcfon.nzp_key, Convert.ToDecimal(countKvar/dt.Rows.Count),
                                "calc_fon_" + calcfon.QueueNumber);

                        int nzp_kvarpu = 0;
                        if (rowKvar["nzp_kvar"] != DBNull.Value) nzp_kvarpu = (int) rowKvar["nzp_kvar"];
                        if (nzp_kvarpu > 0)
                        {
                            transaction = conn_db.BeginTransaction();

                            #region Цикл по счетчикам

                            for (int iCnt1 = 0; iCnt1 < CounterList.Count; ++iCnt1)
                            {
                                #region Заполняем поля для нового счетчика

                                int iCount2 = CounterList[iCnt1].cnt_ls;
                                if (iCount2 == 0) iCount2 = 1;

                                Counter newCounter = new Counter();
                                CounterList[iCnt1].CopyTo(newCounter);
                                newCounter.nzp_type = 3;
                                newCounter.cnt_ls = 0;
                                newCounter.nzp = nzp_kvarpu;
                                newCounter.nzp_kvar = nzp_kvarpu;
                                newCounter.nzp_cnt = CounterList[iCnt1].nzp_cnt;
                                newCounter.prm = CounterList[iCnt1].prm;
                                newCounter.nzp_serv = CounterList[iCnt1].nzp_serv;
                                newCounter.nzp_cnttype = CounterList[iCnt1].nzp_cnttype;
                                newCounter.nzp_user = finder.nzp_user;
                                newCounter.pref = sPref;

                                #region определяем услугу

                                if (!(newCounter.nzp_serv > 0))
                                {
                                    IDataReader reader = null;
                                    string s_counts = newCounter.pref + "_kernel" + tableDelimiter + "s_counts";
                                    string s_countsdop = newCounter.pref + "_kernel" + tableDelimiter + "s_countsdop";
                                    try
                                    {
                                        sql = "select nzp_serv from " + s_counts + " where nzp_cnt = " +
                                              newCounter.nzp_cnt;
                                        sql += " union all";
                                        sql += " select nzp_serv from " + s_countsdop + " where nzp_cnt = " +
                                               newCounter.nzp_cnt;
                                        ret = ExecRead(conn_db, transaction, out reader, sql, true);
                                        if (!ret.result)
                                        {
                                            return ret;
                                        }
                                        int nzpServ = 0, isGkal = 0;
                                        if (reader.Read())
                                        {
                                            if (reader["nzp_serv"] != DBNull.Value) nzpServ = (int) reader["nzp_serv"];
                                            if (nzpServ == 15 || nzpServ == 16 || nzpServ == 17) isGkal = 1;
                                        }
                                        reader.Close();
                                        if (nzpServ < 1)
                                        {
                                            MonitorLog.WriteLog("Не удалось определить код услуги " +
                                                                System.Reflection.MethodBase.GetCurrentMethod().Name,
                                                MonitorLog.typelog.Error, true);
                                            continue;
                                        }


                                        newCounter.nzp_serv = nzpServ;
                                        newCounter.is_gkal = isGkal;
                                    }
                                    finally
                                    {
                                        if (reader != null && reader.IsClosed) reader.Close();
                                    }

                                }


                                //проверяем, что данная услуга действует у данного ЛС
                                sql =
                                    " SELECT count(*)" +
                                    " FROM " + sPref + sDataAliasRest + "tarif" +
                                    " WHERE is_actual = 1 AND '" +
                                    Points.GetCalcMonth(new CalcMonthParams(sPref))
                                        .RecordDateTime.ToShortDateString() + "'" +
                                    " BETWEEN dat_s AND dat_po AND nzp_serv = " + newCounter.nzp_serv + " AND nzp_kvar = " +
                                    nzp_kvarpu;
                                var objServ = ExecScalar(conn_db, transaction, sql, out ret, true);
                                int intServ = 0;
                                if (objServ == null || !Int32.TryParse(objServ.ToString(), out intServ))
                                {
                                    MonitorLog.WriteLog(
                                        "Не удалось определить, действует ли выбранная для ИПУ услуга " +
                                        newCounter.nzp_serv + " по ЛС " + nzp_kvarpu +
                                        System.Reflection.MethodBase.GetCurrentMethod().Name,
                                        MonitorLog.typelog.Error, true);
                                    continue;
                                }
                                if (intServ < 1)
                                {
                                    MonitorLog.WriteLog(
                                        "Невозможно сгенерировать ИПУ по недействующей услуге " + newCounter.nzp_serv +
                                        " для ЛС с номером " + nzp_kvarpu + " " +
                                        System.Reflection.MethodBase.GetCurrentMethod().Name,
                                        MonitorLog.typelog.Error, true);
                                    continue;
                                }

                                #endregion

                                #region выбираем cписок ПУ этого ЛС, чтобы опрелелить далее новый num_cnt создаваемых ПУ

                                //чтобы при повторной генерации num_cnt не повторялся
                                List<string> numCntList = new List<string>();
                                string counters_spis = newCounter.pref + "_data" + tableDelimiter + "counters_spis";
                                sql = " SELECT num_cnt FROM " + counters_spis +
                                      " WHERE nzp = " + nzp_kvarpu;
                                DataTable numCntDT = DBManager.ExecSQLToTable(conn_db, sql);
                                foreach (DataRow row in numCntDT.Rows)
                                {
                                    numCntList.Add(row["num_cnt"].ToString());
                                }

                                #endregion

                                #endregion

                                for (int iCnt2 = 1; iCnt2 <= iCount2; iCnt2++)
                                {
                                    #region Создаем счетчики

                                    DbCounter dbcounter = new DbCounter();

                                    #region Выявляем неповторяющийся num_cnt создаваемого счетчика

                                    //начальное значение суффикса
                                    int numCntSuffix = numCntList.Count;
                                    string newNumCnt = "К-" + nzp_kvarpu + "-" + newCounter.nzp_serv + "-" +
                                                       numCntSuffix;
                                    //переменная, которая будет отвечать за то, что мы нашли num_cnt
                                    bool isFound = numCntList.Contains(newNumCnt);
                                    while (isFound)
                                    {
                                        numCntSuffix++;
                                        newNumCnt = "К-" + nzp_kvarpu + "-" + newCounter.nzp_serv + "-" + numCntSuffix;
                                        isFound = numCntList.Contains(newNumCnt);
                                    }
                                    numCntList.Add(newNumCnt);
                                    newCounter.num_cnt = newNumCnt;

                                    #endregion

                                    //чтобы создавался новый счетчик, не перетирая старого, обнуляем nzp_counter
                                    newCounter.nzp_counter = 0;
                                    newCounter.comment = "Создан групповой операцией";

                                    RecordMonth r_m = Points.GetCalcMonth(new CalcMonthParams(newCounter.pref));

                                    ret = dbcounter.SaveCounter(conn_db, transaction, newCounter,
                                        new DateTime(r_m.year_, r_m.month_, 1).ToString("dd.MM.yyyy"));

                                    if (!ret.result)
                                    {
                                        transaction.Rollback();
                                        return ret;
                                    }
                                    else
                                    {
                                        #region Сохраняем параметр ПУ - вид помещения

                                        if (!string.IsNullOrEmpty(newCounter.prm.Trim()))
                                        {
                                            Param prm = new Param();
                                            prm.dat_s = new DateTime(r_m.year_, r_m.month_, 1).ToShortDateString();
                                            prm.nzp_user = finder.nzp_user;
                                            prm.webLogin = finder.webLogin;
                                            prm.webUname = finder.webUname;
                                            prm.pref = finder.pref;
                                            prm.nzp = ret.tag;
                                            prm.nzp_prm = 974;
                                            prm.val_prm = newCounter.prm.Trim();
                                            prm.prm_num = 17;
                                            using (DbParameters dbparam = new DbParameters())
                                            {
                                                ret = dbparam.SavePrm(conn_db, transaction, prm);
                                            }
                                        }

                                        #endregion
                                    }

                                    #endregion
                                }
                            }
                            transaction.Commit();

                            countKvar ++;

                            #endregion
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;
                MonitorLog.WriteLog(ex.Message, MonitorLog.typelog.Error, true);
                if (transaction != null) transaction.Rollback();
            }
            finally
            {
                string sql1;
                Returns ret1;
                //удаляем таблицы из public
                if (TableInWebCashe(conn_web, "t" + finder.nzp_user + "_gengrouplspu" + finder.listNumber))
                {
                    sql1 = "DROP TABLE " + DBManager.sDefaultSchema + "t" + finder.nzp_user + "_gengrouplspu" +
                           finder.listNumber;
                    ret1 = DBManager.ExecSQL(conn_web, sql1, true);
                }
                if (TableInWebCashe(conn_web, "t" + Convert.ToString(finder.nzp_user) + "_selectedls" + finder.listNumber))
                {
                    sql1 = "DROP TABLE " + DBManager.sDefaultSchema + "t" + Convert.ToString(finder.nzp_user) + "_selectedls" + finder.listNumber;
                    ret1 = DBManager.ExecSQL(conn_web, sql1,true);
                }

                conn_db.Close();
                conn_web.Close();
            }

            #endregion

            return ret;
        }


        public Returns SaveGeu(Geu finder)
        {
            if (finder.nzp_user < 1) return new Returns(false, "Не задан пользователь");
            if (finder.geu.Trim() == "") return new Returns(false, "Не задано наименование отделения");
            finder.geu = finder.geu.Trim();

            #region подключение к базе
            string conn_kernel = Points.GetConnByPref(Points.Pref);
            IDbConnection conn_db = GetConnection(conn_kernel);
            Returns ret = OpenDb(conn_db, true);
            if (!ret.result) return ret;
            #endregion

            if (finder.pref == "") finder.pref = Points.Pref;
            string table = finder.pref + "_data" + DBManager.tableDelimiter + "s_geu";
            string sql;
            if (finder.nzp_geu > 0)
            {
                sql = "select nzp_geu from " + table + " where nzp_geu = " + finder.nzp_geu;
                IDataReader reader;
                ret = ExecRead(conn_db, out reader, sql, true);
                if (!ret.result)
                {
                    conn_db.Close();
                    return ret;
                }
                if (reader.Read())
                {
                    sql = "update " + table + " set geu = " + Utils.EStrNull(finder.geu) + " where nzp_geu = " + finder.nzp_geu;
                }
                else
                {
                    sql = "insert into " + table + " (nzp_geu, geu) values (" + finder.nzp_geu + ", " + Utils.EStrNull(finder.geu) + ")";
                }
                CloseReader(ref reader);
            }
            else
            {
                DbSprav db = new DbSprav();
                ret = db.GetNewId(conn_db, Series.Types.Geu);
                if (!ret.result)
                {
                    conn_db.Close();
                    return ret;
                }

                finder.nzp_geu = ret.tag;

                sql = "insert into " + table + " (nzp_geu, geu) values (" + finder.nzp_geu + ", " + Utils.EStrNull(finder.geu) + ")";
            }

            ret = ExecSQL(conn_db, sql, true);
            if (ret.result && finder.nzp_geu < 1)
            {
                ret.tag = GetSerialValue(conn_db);
            }
            conn_db.Close();

            return ret;
        }

        //----------------------------------------------------------------------
        public List<_Area> LoadArea(Finder finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            Areas spis = new Areas();
            spis.AreaList.Clear();
            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return null;

            string where = "";
            if (finder.RolesVal != null)
                foreach (_RolesVal role in finder.RolesVal)
                {
                    if (role.tip == Constants.role_sql & role.kod == Constants.role_sql_area)
                        where += " and nzp_area in (" + role.val + ")";
                }

            if (finder.dopFind != null)
                foreach (string str in finder.dopFind)
                {
                    if (str.Contains("FiltrOnDistrib"))
                    {
                        //фильтровать справочники по fn_distrib
                        where += DbSprav.FiltrOnDistrib("nzp_area", finder.dopFind);
                    }
                    else
                    {
                        where += " and upper(area) like '%" + str.ToUpper().Replace("'", "''").Replace("*", "%") + "%'";
                    }
                }
            if (finder.pref.Trim() == "") finder.pref = Points.Pref;
            string wherePoint = "";
            // string fromPoint = "";
            if (finder.dopPointList != null)
            {
                if (finder.dopPointList.Count > 0)
                {
                    string str = "";
                    for (int i = 0; i < finder.dopPointList.Count; i++)
                    {
                        if (i == 0) str += finder.dopPointList[i];
                        else str += ", " + finder.dopPointList[i];
                    }
                    if (str != "")
                    {
                        wherePoint += " and   a.nzp_area in (select distinct k.nzp_area from " + Points.Pref + "_data" + DBManager.tableDelimiter + "kvar k where k. nzp_wp  IN (" + str + ")) ";
                    }
                }
            }


            //выбрать список
            MyDataReader reader;

            string sqlarea = " Select a.nzp_area,a.area,a.nzp_supp, s.name_supp From " + finder.pref + sDataAliasRest +
                             "s_area a left outer join " + finder.pref + sKernelAliasRest + "supplier s " +
                             " on a.nzp_supp = s.nzp_supp " +
                             " where 1=1 " + where + wherePoint +
                             " Order by area ";

            ret = ExecRead(conn_db, out reader, sqlarea, true);
            if (!ret.result)
            {
                return null;
            }

            try
            {
                int i = 0;
                while (reader.Read())
                {
                    i++;
                    if (i <= finder.skip) continue;
                    _Area zap = new _Area();

                    if (reader["nzp_area"] == DBNull.Value)
                        zap.nzp_area = 0;
                    else
                        zap.nzp_area = (int) reader["nzp_area"];
                    if (reader["area"] == DBNull.Value)
                        zap.area = "";
                    else
                    {
                        zap.area = (string) reader["area"];
                        zap.area = zap.area.Trim();
                    }

                    if (reader["nzp_supp"] != DBNull.Value) zap.nzp_supp = Convert.ToInt32(reader["nzp_supp"]);
                    if (reader["name_supp"] != DBNull.Value) zap.name_supp = Convert.ToString(reader["name_supp"]);

                    spis.AreaList.Add(zap);
                    if (finder.rows > 0 && i >= finder.skip + finder.rows) break;
                }
                reader.Close();

                object count = ExecScalar(conn_db,
                    "select count(*) from " + Points.Pref + sDataAliasRest + "s_area where 1 = 1 " + where, out ret,
                    true);

                if (ret.result)
                {
                    try
                    {
                        ret.tag = Convert.ToInt32(count);
                    }
                    catch (Exception ex)
                    {
                        ret.result = false;
                        ret.text = ex.Message;
                        return null;
                    }
                }

                conn_db.Close();
                return spis.AreaList;
            }
            catch (Exception ex)
            {
                reader.Close();
                

                ret.result = false;
                ret.text = ex.Message;

                string err;
                if (Constants.Viewerror)
                    err = " \n " + ex.Message;
                else
                    err = "";

                MonitorLog.WriteLog("Ошибка заполнения справочника Управляющих организаций " + err,
                    MonitorLog.typelog.Error, 20, 201, true);

                return null;
            }
            finally
            {
                conn_db.Close();
            }
        }

        //----------------------------------------------------------------------
        public List<_Area> LoadAreaForKvar(Finder finder, out Returns ret)
            //----------------------------------------------------------------------
        {
            if (finder.nzp_user < 1)
            {
                ret = new Returns(false, "Не задан пользователь");
                return null;
            }
            if (finder.nzp_role < 1) //nzp_role => nzp_kvar
            {
                ret = new Returns(false, "Не задан лицевой счет");
                return null;
            }

            var spis = new Areas();
            spis.AreaList.Clear();
            IDbConnection connDB = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(connDB, true);
            if (!ret.result) return null;
            

            string where = string.Empty;
            if (finder.RolesVal != null)
                foreach (_RolesVal role in finder.RolesVal)
                {
                    if (role.tip == Constants.role_sql & role.kod == Constants.role_sql_area)
                        where += " AND nzp_area IN (" + role.val + ")";
                }

            if (finder.pref.Trim() == string.Empty) finder.pref = Points.Pref;

            //выбрать список
            MyDataReader reader;
            string centralData = Points.Pref + DBManager.sDataAliasRest,
                    prefData = finder.pref + sDataAliasRest,
                    prefKernel = finder.pref + sKernelAliasRest;

            string sqlarea = " SELECT a.nzp_area, a.area, a.nzp_supp, s.name_supp " +
                             " FROM " + prefData + "s_area a LEFT OUTER JOIN " + prefKernel + "supplier s ON a.nzp_supp = s.nzp_supp " +
                             " WHERE a.nzp_area IN ( SELECT DISTINCT k.nzp_area " +
                                                   " FROM " + centralData + "kvar k " +
                                                   " WHERE " + (finder.nzp_role > 0
                                                                  ? " nzp_kvar = " + finder.nzp_role
                                                                  : String.Empty) + " ) " + where +
                             " ORDER by area ";

            ret = ExecRead(connDB, out reader, sqlarea, true);
            if (!ret.result) return null;

            try
            {
                if (reader.Read())
                {
                    var area = new _Area();

                    if (reader["nzp_area"] == DBNull.Value) area.nzp_area = 0;
                    else area.nzp_area = (int) reader["nzp_area"];
                    if (reader["area"] == DBNull.Value) area.area = string.Empty;
                    else 
                    {
                        area.area = (string) reader["area"];
                        area.area = area.area.Trim();
                    }

                    if (reader["nzp_supp"] != DBNull.Value) area.nzp_supp = Convert.ToInt32(reader["nzp_supp"]);
                    if (reader["name_supp"] != DBNull.Value) area.name_supp = Convert.ToString(reader["name_supp"]);

                    spis.AreaList.Add(area);
                }
                reader.Close();
            }
            catch (Exception ex)
            {
                reader.Close();
                ret.result = false;
                ret.text = ex.Message;

                string err;
                if (Constants.Viewerror) err = " \n " + ex.Message;
                else err = string.Empty;

                MonitorLog.WriteLog("Ошибка в получение информации об управляющей компании. " + err,
                    MonitorLog.typelog.Error, 20, 201, true);

                return null;
            }
            finally
            {
                connDB.Close();
            }
            return spis.AreaList;
        }

        public List<_Area> LoadAreaPayer(Finder finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            Areas spis = new Areas();
            spis.AreaList.Clear();
            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return null;

            string where = "";
            if (finder.RolesVal != null)
                foreach (_RolesVal role in finder.RolesVal)
                {
                    if (role.tip == Constants.role_sql & role.kod == Constants.role_sql_area)
                        where += " and nzp_area in (" + role.val + ")";
                }

            
            if (finder.pref.Trim() == "") finder.pref = Points.Pref;
            
            //выбрать список
            MyDataReader reader;

            string sqlarea = " Select a.nzp_area,a.area,a.nzp_payer, s.payer From " + finder.pref + sDataAliasRest +
                             "s_area a left outer join " + Points.Pref + sKernelAliasRest + "s_payer s " +
                             " on a.nzp_payer = s.nzp_payer " +
                             " where 1=1 " + where +
                             " Order by area ";

            ret = ExecRead(conn_db, out reader, sqlarea, true);
            if (!ret.result)
            {
                return null;
            }

            try
            {
                int i = 0;
                while (reader.Read())
                {
                    i++;
                    if (i <= finder.skip) continue;
                    _Area zap = new _Area();

                    if (reader["nzp_area"] == DBNull.Value)
                        zap.nzp_area = 0;
                    else
                        zap.nzp_area = (int)reader["nzp_area"];
                    if (reader["area"] == DBNull.Value)
                        zap.area = "";
                    else
                    {
                        zap.area = (string)reader["area"];
                        zap.area = zap.area.Trim();
                    }

                    if (reader["nzp_payer"] != DBNull.Value) zap.nzp_payer = Convert.ToInt32(reader["nzp_payer"]);
                    if (reader["payer"] != DBNull.Value) zap.payer = Convert.ToString(reader["payer"]);

                    spis.AreaList.Add(zap);
                    if (finder.rows > 0 && i >= finder.skip + finder.rows) break;
                }
                reader.Close();

                object count = ExecScalar(conn_db,
                    "select count(*) from " + Points.Pref + sDataAliasRest + "s_area where 1 = 1 " + where, out ret,
                    true);

                if (ret.result)
                {
                    try
                    {
                        ret.tag = Convert.ToInt32(count);
                    }
                    catch (Exception ex)
                    {
                        ret.result = false;
                        ret.text = ex.Message;
                        return null;
                    }
                }

                conn_db.Close();
                return spis.AreaList;
            }
            catch (Exception ex)
            {
                reader.Close();


                ret.result = false;
                ret.text = ex.Message;

                string err;
                if (Constants.Viewerror)
                    err = " \n " + ex.Message;
                else
                    err = "";

                MonitorLog.WriteLog("Ошибка заполнения справочника Управляющих организаций " + err,
                    MonitorLog.typelog.Error, 20, 201, true);

                return null;
            }
            finally
            {
                conn_db.Close();
            }
        }

        //----------------------------------------------------------------------
        public string GetFakturaName(Ls finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();

            List<Ls> LsInfo = LoadLs(finder, out ret);
            string pkod = "";
            string std_fact = "~/App_Data/web_zel.frx";

            if (ret.result)
            {
                if (LsInfo.Count > 0) pkod = LsInfo[0].pkod;
            }

            if ((pkod != "") & (pkod.Length > 2))
            {
                if (pkod.Substring(0, 3) == "270") return "~/App_Data/web_avia.frx";
                if (pkod.Substring(0, 3) == "201") return "~/App_Data/web_zel.frx";
                // return std_fact; 
            }

            if (finder.nzp_user < 1)
            {
                ret.result = false;
                ret.text = "Не определен пользователь";
                return std_fact;
            }

            if (finder.pref == "")
            {
                ret.text = "Префикс базы данных не задан";
                return std_fact;
            }

            //заполнить webdata:tXX_spls
            string conn_kernel = Points.GetConnByPref(finder.pref);
            IDbConnection conn_db = GetConnection(conn_kernel);
            //IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result)
            {
                conn_db.Close();
                return std_fact;
            }



            string sql = "";

            IDataReader reader;

#if PG
            sql = " Select * from " + finder.pref + "_kernel.s_listfactura where townfilter=63  ";
#else
            sql = " Select * from " + finder.pref + "_kernel:s_listfactura where townfilter=63  ";
#endif

            if (!ExecRead(conn_db, out reader, sql, true).result)
            {
                MonitorLog.WriteLog("Ошибка выборки " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                reader.Close();
                conn_db.Close();
                return std_fact;
            }
            else
            {
                if (reader.Read())
                {
                    if (reader["file_name"] != DBNull.Value) std_fact = Convert.ToString(reader["file_name"]).Trim();
                }

            }
            reader.Close();
            conn_db.Close();
            return std_fact;

        }

        /// <summary>
        /// Генерация платежных кодов(постановка в фоновый процесс)
        /// </summary>
        /// <param name="finder">пользователь</param>
        /// <returns>Результат</returns>
        public Returns GeneratePkodFonAddTask(Finder finder)
        {
            CalcFonTask calcfon = new CalcFonTask(Points.GetCalcNum(0));
            calcfon.TaskType = CalcFonTask.Types.taskGeneratePkod;
            calcfon.Status = FonTask.Statuses.New; //на выполнение                
            calcfon.nzp_user = finder.nzp_user;
            calcfon.txt = "Процедура генерации платежных кодов по выбранному списку лицевых счетов";
            calcfon.nzp_user = finder.nzp_user;
            DbCalcQueueClient dbCalc = new DbCalcQueueClient();
            Returns ret = dbCalc.AddTask(calcfon);

            if (ret.result)
                if (ret.tag > 0)
                {
                    CreateSpLsForFon("t" +ret.text.Trim()+ ret.tag, finder.nzp_user);
                }
            if (ret.text == "" || ret.text=="0") ret.text = "Задача поставлена в очередь на выполнение";
            dbCalc.Close();
            return ret;
        }

        public Returns CreateSpLsForFon(string tablename, int nzp_user)
        {
            Returns ret = Utils.InitReturns();
                        
            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result)
            {
                conn_web.Close();
                return ret;
            }

#if PG
            ExecSQL(conn_web, "set search_path to 'public'", false);
#endif

            StringBuilder sql = new StringBuilder();
            sql.AppendFormat("create table {0} (nzp_kvar integer, num_ls integer, pref varchar(20))", tablename);
            ret = ExecSQL(conn_web, sql.ToString(), true);
            if (!ret.result)
            {
                conn_web.Close();
                return ret;
            }

            string tXX_spls = "t" + Convert.ToString(nzp_user) + "_spls";

            sql.Remove(0, sql.Length);
            sql.AppendFormat("insert into {0} (nzp_kvar,num_ls, pref) select nzp_kvar,num_ls, pref from {1} where mark = 1", tablename, tXX_spls);
            ret = ExecSQL(conn_web, sql.ToString(), true);
            if (!ret.result)
            {
                conn_web.Close();
                return ret;
            }

            conn_web.Close();
            return ret;
        }

        /// <summary>
        /// Получить список объектов на карте
        /// </summary>
        /// <param name="finder"></param>
        /// <param name="ret"></param>
        /// <returns>Список оъектов карты</returns>
        public List<MapObject> GetMapObjects(MapObject finder, out Returns ret)
        {
            ret = Utils.InitReturns();

            List<MapObject> mapObjects = new List<MapObject>();

            //соединение с БД
            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result) return mapObjects;

            IDataReader reader, readerPoint;

            string sql = " Select nzp_mo, tip, kod, nzp_wp, object_type, note from "+sDefaultSchema+"map_objects";
            sql += " Where tip = " + finder.tip.GetHashCode();
            sql += " and kod = " + finder.kod.ToString();
            if (finder.nzp_wp > 0) sql += " and nzp_wp = " + finder.nzp_wp.ToString();

            ret = ExecRead(conn_web, out reader, sql, true);
            if (!ret.result)
            {
                conn_web.Close();
                return mapObjects;
            }

            try
            {
                while (reader.Read())
                {
                    MapObject mapObject = new MapObject();
                    if (reader["nzp_mo"] != DBNull.Value) mapObject.nzp_mo = Convert.ToInt32(reader["nzp_mo"]);
                    if (reader["tip"] != DBNull.Value) mapObject.tip = MapObject.getTip(Convert.ToInt32(reader["tip"]));
                    if (reader["kod"] != DBNull.Value) mapObject.kod = Convert.ToInt32(reader["kod"]);
                    if (reader["nzp_wp"] != DBNull.Value) mapObject.nzp_wp = Convert.ToInt32(reader["nzp_wp"]);
                    if (reader["object_type"] != DBNull.Value) mapObject.object_type = Convert.ToInt32(reader["object_type"]);
                    //if (reader["note"] != DBNull.Value) mapObject.note = Convert.ToString(reader["note"]).Trim();

                    #region Заполнение комментария к объекту
                    Ls finderLs = new Ls();
                    finderLs.nzp_user = finder.nzp_user;
                    finderLs.nzp_wp = finder.nzp_wp;
                    for (int i = 0; i < Points.PointList.Count; i++)
                        if (Points.PointList[i].nzp_wp == finder.nzp_wp)
                        {
                            finderLs.pref = Points.PointList[i].pref;
                            break;
                        }
                    Returns ret2;
                    List<Ls> list;
                    switch (mapObject.tip)
                    {
                        case MapObject.Tip.dom:
                            finderLs.nzp_dom = mapObject.kod;
                            list = LoadLs(finderLs, out ret2);
                            if (ret2.result && list.Count > 0) mapObject.note = list[0].adr;
                            break;
                        case MapObject.Tip.ulica:
                            finderLs.nzp_ul = mapObject.kod;
                            list = LoadLs(finderLs, out ret2);
                            if (ret2.result && list.Count > 0) mapObject.note = list[0].adr;
                            break;
                        case MapObject.Tip.geu:
                            finderLs.nzp_geu = mapObject.kod;
                            list = LoadLs(finderLs, out ret2);
                            if (ret2.result && list.Count > 0) mapObject.note = list[0].geu;
                            break;
                        case MapObject.Tip.area:
                            finderLs.nzp_area = Convert.ToInt32(mapObject.kod);
                            list = LoadLs(finderLs, out ret2);
                            if (ret2.result && list.Count > 0) mapObject.note = list[0].area;
                            break;
                    }
                    #endregion

                    sql = "Select nzp_mp, nzp_mo, x, y, ordering From " + sDefaultSchema + "map_points Where nzp_mo = " + mapObject.nzp_mo.ToString() + " Order by ordering";
                    ret = ExecRead(conn_web, out readerPoint, sql, true);
                    if (!ret.result)
                    {
                        conn_web.Close();
                        return mapObjects;
                    }
                    while (readerPoint.Read())
                    {
                        _MapPoint mapPoint = new _MapPoint();
                        if (readerPoint["nzp_mp"] != DBNull.Value) mapPoint.nzp_mp = Convert.ToInt32(readerPoint["nzp_mp"]);
                        if (readerPoint["nzp_mo"] != DBNull.Value) mapPoint.nzp_mo = Convert.ToInt32(readerPoint["nzp_mo"]);
                        if (readerPoint["x"] != DBNull.Value) mapPoint.x = Convert.ToSingle(readerPoint["x"]);
                        if (readerPoint["y"] != DBNull.Value) mapPoint.y = Convert.ToSingle(readerPoint["y"]);
                        if (readerPoint["ordering"] != DBNull.Value) mapPoint.ordering = Convert.ToInt32(readerPoint["ordering"]);
                        mapObject.points.Add(mapPoint);
                    }
                    readerPoint.Close();
                    mapObjects.Add(mapObject);
                }
                reader.Close();
            }
            catch (Exception ex)
            {
                reader.Close();
                ret.result = false;
                ret.text = ex.Message;
                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;
                MonitorLog.WriteLog("Ошибка при определении списка объектов на карте: " + err, MonitorLog.typelog.Error, 20, 201, true);
            }

            conn_web.Close();
            return mapObjects;
        }

        public Returns UpdateLsInCache(Ls finder)
        {
            Returns ret = Utils.InitReturns();
            if (finder.nzp_user < 1)
            {
                ret.result = false;
                ret.text = "Не определен пользователь";
                return ret;
            }

            #region обновление данных в выбранном списке л/с
            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result)
            {
                return ret;
            }

            string tXX_spls = "t" + Convert.ToString(finder.nzp_user) + "_spls";

            if (TableInWebCashe(conn_web, tXX_spls))
            {
                List<Ls> list = LoadLs(finder, out ret);
                if (!ret.result)
                {
                    conn_web.Close();
                    return ret;
                }
                if (list.Count > 0)
                {
                    string sostls = list[0].stateID == Ls.States.Open.GetHashCode() ? "открыт" : "закрыт";

                    string sql = "update " + tXX_spls + " set nzp_area = " + list[0].nzp_area + ", nzp_geu = " + list[0].nzp_geu + ", fio = '" + list[0].fio +
                        "', nkvar = '" + list[0].nkvar + " " + list[0].nkvar_n + "', adr ='" + list[0].adr + "', sostls ='" + sostls + "' "
                        + " where nzp_kvar = " + finder.nzp_kvar + " and pref = '" + finder.pref + "'";
                    ret = ExecSQL(conn_web, sql, true);
                    if (!ret.result)
                    {
                        conn_web.Close();
                        return ret;
                    }
                }
            }
            return ret;
            #endregion
        }

        //----------------------------------------------------------------------
        public int Update(IDbConnection conn_db, IDbTransaction transaction, IDbConnection conn_web, Ls kvar, out Returns ret) //исправить данные квартиры
        //---------------------------------------------------------------------
        {
            ret = Utils.InitReturns();//инициализация результата
            string sql = "";
            IDataReader reader;

            #region Проверка существования л/с по определенному адресу при добавлении нового или редактировании л/с
            //если переменная kvar.chekexistls=1, значит функция вызывается в первый раз и надо проверить не
            //существует ли л/с по этому адресу (нужно для того, чтобы выдать предупреждения)
            if (kvar.chekexistls == 1)
            {
                bool b = false; //признак - продолжать проверку на существование л/с
                #region если редактирование, проверить не изменился ли адрес
                if (kvar.nzp_kvar != Constants._ZERO_)
                {
                    sql = "select ikvar, nkvar, nkvar_n, nzp_dom from " + kvar.pref + sDataAliasRest + "kvar where nzp_kvar = " + kvar.nzp_kvar;

                    if (!ExecRead(conn_db, transaction, out reader, sql, true).result)
                    {
                        return Constants._ZERO_;
                    }
                    try
                    {
                        int ikvar = 0;
                        int nzp_dom = 0;
                        string nkvar = "";
                        string nkvar_n = "";
                        if (reader.Read())
                        {
                            if (reader["ikvar"] != DBNull.Value) ikvar = Convert.ToInt32(reader["ikvar"]);
                            if (reader["nkvar"] != DBNull.Value) nkvar = Convert.ToString(reader["nkvar"]);
                            if (reader["nkvar_n"] != DBNull.Value) nkvar_n = Convert.ToString(reader["nkvar_n"]);
                            if (reader["nzp_dom"] != DBNull.Value) nzp_dom = Convert.ToInt32(reader["nzp_dom"]);
                        }
                        reader.Close();
                        if (ikvar == Utils.GetInt(kvar.nkvar) &&
                            nkvar.Trim() == kvar.nkvar &&
                            nkvar_n.Trim() == kvar.nkvar_n &&
                            nzp_dom == kvar.nzp_dom) b = false;
                        else b = true;
                    }
                    catch (Exception ex)
                    {
                        reader.Close();
                        ret.result = false;
                        ret.text = ex.Message;
                        string err;
                        if (Constants.Viewerror) err = " \n " + ex.Message;
                        else err = "";
                        MonitorLog.WriteLog("Ошибка получения информации о лс " + err, MonitorLog.typelog.Error, 20, 201, true);
                        return Constants._ZERO_;
                    }

                }
                #endregion

                #region если добавление или редактирование (с изменением адреса), то проверка на существования лс по адресу
                if (kvar.nzp_kvar == Constants._ZERO_ || b)
                {
                    sql = "select count(*) from " + kvar.pref + sDataAliasRest + "kvar " + " where nzp_dom  = " + kvar.nzp_dom + " and ikvar = " + Utils.GetInt(kvar.nkvar);

                    if (kvar.nkvar.Trim() == "") sql += " and (nkvar = '' or nkvar = '-' or nkvar is NULL)"; else sql += " and nkvar = " + Utils.EStrNull(kvar.nkvar.Trim());
                    if (kvar.nkvar_n.Trim() == "") sql += " and (nkvar_n = '' or nkvar_n = '-' or nkvar_n is NULL)"; else sql += " and nkvar_n = " + Utils.EStrNull(kvar.nkvar_n.Trim());

                    IDbCommand cmd = DBManager.newDbCommand(sql, conn_db, transaction);
                    try
                    {
                        string s = Convert.ToString(cmd.ExecuteScalar());
                        ret.tag = Convert.ToInt32(s);
                        if (ret.tag > 0)
                        {
                            ret.tag = -11;

                            ret.text = "Л/с по этому адресу уже существует. Продолжить сохранение л/с?";

                            ret.result = false;
                            return Constants._ZERO_;
                        }
                    }
                    catch (Exception ex)
                    {
                        ret.result = false;
                        ret.text = ex.Message;
                        string err;
                        if (Constants.Viewerror) err = " \n " + ex.Message; else err = "";
                        MonitorLog.WriteLog("Ошибка определения количества лс" + err, MonitorLog.typelog.Error, 20, 201, true);
                        return Constants._ZERO_;
                    }
                }
                #endregion
            }
            #endregion

            #region редактирование л/с
            if (kvar.nzp_kvar != Constants._ZERO_)
            {
                #region Определить пользователя
                int nzpUser = kvar.nzp_user; //локальный пользователь     
                
                /*DbWorkUser db = new DbWorkUser();
                int nzpUser = db.GetLocalUser(conn_db, transaction, kvar, out ret); //локальный пользователь      
                db.Close();
                if (!ret.result) return Constants._ZERO_;*/
                #endregion

                #region проверить не заблокирован ли лс, если нет то заблокировать
#if PG
                sql = "select nzp_user, dat_when,  (now() - INTERVAL " + string.Format("'{0} minutes')", Constants.users_min) + " as cur_date from " + kvar.pref +
                      "_data.kvar_block where nzp_kvar = " + kvar.nzp_kvar + " order by dat_when desc";
#else
                sql = "select nzp_user, dat_when,  (current year to second - " + Constants.users_min.ToString() + " units minute) as cur_date from " + kvar.pref +
                      "_data:kvar_block where nzp_kvar = " + kvar.nzp_kvar + " order by dat_when desc";
#endif
                if (!ExecRead(conn_db, transaction, out reader, sql, true).result)
                {
                    return Constants._ZERO_;
                }

                try
                {
                    DateTime datwhen = DateTime.MinValue;
                    DateTime curdate = DateTime.MinValue;
                    int nzpuser = 0;

                    if (reader.Read())
                    {
                        if (reader["dat_when"] != DBNull.Value) datwhen = Convert.ToDateTime(reader["dat_when"]);
                        if (reader["cur_date"] != DBNull.Value) curdate = Convert.ToDateTime(reader["cur_date"]);
                        if (reader["nzp_user"] != DBNull.Value) nzpuser = Convert.ToInt32(reader["nzp_user"]);

                        if (nzpuser > 0 && datwhen != DateTime.MinValue) //заблокирован лицевой счет
                        {
                            if (nzpuser != nzpUser && curdate <= datwhen) //если заблокирована запись другим пользователем и 20 мин не прошло
                            {
                                ret.result = false;
                                ret.text = "Редактировать данные запрещено, поскольку с ними работает другой пользователь";
                                ret.tag = -12;
                                reader.Close();
                                return Constants._ZERO_;
                            }
                        }


                    }
                    reader.Close();

                    #region Удалить все записи для kvar.nzp_kvar
#if PG
                    ret = ExecSQL(conn_db, transaction, "delete from " + kvar.pref + "_data.kvar_block where nzp_kvar = " + kvar.nzp_kvar, true);
#else
                    ret = ExecSQL(conn_db, transaction, "delete from " + kvar.pref + "_data:kvar_block where nzp_kvar = " + kvar.nzp_kvar, true);
#endif
                    if (!ret.result)
                    {
                        ret.result = false;
                        ret.text = "Ошибка удаления из таблицы kvar_block";
                        return Constants._ZERO_;
                    }
                    #endregion

                    #region Заблокировать л/с
#if PG
                    ret = ExecSQL(conn_db, transaction, "insert into " + kvar.pref + "_data.kvar_block (nzp_kvar,nzp_user, dat_when,kod) values(" +
                                                        kvar.nzp_kvar + "," + nzpUser + ",now()," + Constants.ist + ")", true);
#else
                    ret = ExecSQL(conn_db, transaction, "insert into " + kvar.pref + "_data:kvar_block (nzp_kvar,nzp_user, dat_when,kod) values(" +
                                                        kvar.nzp_kvar + "," + nzpUser + ",current year to second," + Constants.ist + ")", true);
#endif
                    if (!ret.result)
                    {
                        ret.result = false;
                        ret.text = "Ошибка добавления записи о блокировке в таблицу kvar_block";
                        return Constants._ZERO_;
                    }
                    #endregion
                }
                catch (Exception ex)
                {
                    reader.Close();
                    ret.result = false;
                    ret.text = ex.Message;
                    string err;
                    if (Constants.Viewerror) err = " \n " + ex.Message;
                    else err = "";
                    MonitorLog.WriteLog("Ошибка получения информации о блокировки пользователя " + err, MonitorLog.typelog.Error, 20, 201, true);
                    return 0;
                }
                #endregion

                #region Добавление в sys_events события 'Изменение адреса лицевого счёта'  и 'Изменение ФИО ответственного квартиросъемщика'
                var tsql = "select * from " + kvar.pref + "_data" + tableDelimiter + "kvar " +
                    " where nzp_kvar = " + kvar.nzp_kvar;

                ExecRead(conn_db, transaction, out reader, tsql, true);
                try
                {
                    string nkvar = "";
                    string nkvar_n = "";
                    string fio = "";
                    string phone = "";
                    string porch = "";
                    string uch = "";
                    string remark = "";
                    int typek = 0;
                    int nzp_area = 0;
                    int nzp_geu = 0;
                    if (reader.Read())
                    {
                        if (reader["nkvar"] != DBNull.Value) nkvar = Convert.ToString(reader["nkvar"]).Trim();
                        if (reader["nkvar_n"] != DBNull.Value) nkvar_n = Convert.ToString(reader["nkvar_n"]).Trim();
                        if (reader["fio"] != DBNull.Value) fio = Convert.ToString(reader["fio"]).Trim();
                        if (reader["phone"] != DBNull.Value) phone = Convert.ToString(reader["phone"]).Trim();
                        if (reader["remark"] != DBNull.Value) remark = Convert.ToString(reader["remark"]).Trim();
                        if (reader["uch"] != DBNull.Value) uch = Convert.ToString(reader["uch"]).Trim();
                        if (reader["porch"] != DBNull.Value) porch = Convert.ToString(reader["porch"]).Trim();
                        if (reader["typek"] != DBNull.Value) typek = Convert.ToInt32(reader["typek"]);
                        if (reader["nzp_area"] != DBNull.Value) nzp_area = Convert.ToInt32(reader["nzp_area"]);
                        if (reader["nzp_geu"] != DBNull.Value) nzp_geu = Convert.ToInt32(reader["nzp_geu"]);
                    }
                    reader.Close();

                    //если изменился адрес
                    if (nkvar.Trim() != kvar.nkvar ||
                        nkvar_n.Trim() != kvar.nkvar_n)
                    {
                        #region Добавление в sys_events события 'Изменение адреса лицевого счёта'
                        DbAdmin.InsertSysEvent(new SysEvents()
                        {
                            pref = kvar.pref,
                            nzp_user = kvar.nzp_user,
                            nzp_dict = 8214,
                            nzp_obj = kvar.nzp_kvar,
                            note = "Адрес был изменен c кв." + nkvar + ", комн." + nkvar_n + " на кв." + kvar.nkvar + ", комн." + kvar.nkvar_n
                        }, transaction, conn_db);
                        #endregion
                    }
                    else if (fio.Trim() != kvar.fio)                   //если изменилось фио                    
                    {
                        #region Добавление в sys_events события 'Изменение ФИО'
                        DbAdmin.InsertSysEvent(new SysEvents()
                        {
                            pref = kvar.pref,
                            nzp_user = kvar.nzp_user,
                            nzp_dict = 8216,
                            nzp_obj = kvar.nzp_kvar,
                            note = "ФИО были изменены с " + (fio != "" ? fio : "пусто") + " на " + kvar.fio
                        }, transaction, conn_db);
                        #endregion
                    }
                    else if (typek != kvar.typek)                   //если изменилось фио                    
                    {
                        #region Добавление в sys_events события 'Изменение ФИО'
                        DbAdmin.InsertSysEvent(new SysEvents()
                        {
                            pref = kvar.pref,
                            nzp_user = kvar.nzp_user,
                            nzp_dict = 8216,
                            nzp_obj = kvar.nzp_kvar,
                            note = " Тип ЛС был изменен с " + typek + " на " + kvar.typek
                        }, transaction, conn_db);
                        #endregion
                    }
                    else
                    {
                        #region Добавление в sys_events события 'Изменение реквизитов лицевого счёта'
                        string changed_fields = "";
                        if (phone.Trim() != kvar.phone)
                        {
                            changed_fields += "Телефон с " + (phone.Trim() != "" ? phone.Trim() : "пусто") + " на " + kvar.phone + ". ";
                        }
                        if (remark.Trim() != kvar.remark)
                        {
                            changed_fields += "Примечание с " + (remark.Trim() != "" ? remark.Trim() : "пусто") + " на " + kvar.remark + ". ";
                        }
                        if (uch.Trim() != kvar.uch)
                        {
                            changed_fields += "Участок с " + (uch.Trim() != "" ? uch.Trim() : "пусто") + " на " + kvar.uch + ". ";
                        }
                        if (porch.Trim() != kvar.porch)
                        {
                            changed_fields += "Подъезд с " + (porch.Trim() != "" ? porch.Trim() : "пусто") + " на " + kvar.porch + ". ";
                        }
                        if (nzp_area != kvar.nzp_area)
                        {
                            var area_old = "пусто";
                            var area_new = "пусто";
                            if (kvar.nzp_area != 0 && kvar.nzp_area != -1)
                            {
                                var r = ExecScalar(conn_db, transaction, "select area from " + kvar.pref + "_data" + tableDelimiter + "s_area where nzp_area = " + kvar.nzp_area, out ret, true);
                                if (r != null)
                                    area_old = r.ToString();
                            }
                            if (nzp_area != 0)
                            {
                                var r = ExecScalar(conn_db, transaction, "select area from " + kvar.pref + "_data" + tableDelimiter + "s_area where nzp_area = " + nzp_area, out ret, true);
                                if (r != null)
                                    area_old = r.ToString();
                            }
                            changed_fields += "Управляющая организация с " + area_old + " на " + area_new + ". ";
                        }
                        if (nzp_geu != kvar.nzp_geu)
                        {
                            var area_old = "пусто";
                            var area_new = "пусто";
                            if (kvar.nzp_geu != 0 && kvar.nzp_geu != -1)
                            {
                                var r = ExecScalar(conn_db, transaction, "select geu from " + kvar.pref + "_data" + tableDelimiter + "s_geu where nzp_geu = " + kvar.nzp_geu, out ret, true);
                                if (r != null)
                                    area_old = r.ToString();
                            }
                            if (nzp_geu != 0)
                            {
                                var r = ExecScalar(conn_db, transaction, "select geu from " + kvar.pref + "_data" + tableDelimiter + "s_geu where nzp_geu = " + nzp_geu, out ret, true);
                                if (r != null)
                                    area_old = r.ToString();
                            }
                            changed_fields += "ЖЭУ с " + area_old + " на " + area_new + ". ";
                        }

                        if (changed_fields != "")
                        {
                            DbAdmin.InsertSysEvent(new SysEvents()
                            {
                                pref = kvar.pref,
                                nzp_user = kvar.nzp_user,
                                nzp_dict = 6495,
                                nzp_obj = kvar.nzp_kvar,
                                note = "Были изменены: " + changed_fields
                            }, transaction, conn_db);
                        }
                        #endregion
                    }
                }
                catch (Exception ex)
                {
                    MonitorLog.WriteLog("Ошибка добавления события в sys_events" + ex.Message, MonitorLog.typelog.Error, 20, 201, true);
                }
                #endregion

#if PG
                sql =
                                    " Update " + kvar.pref + "_data.kvar " +
                                    " Set  phone = " + Utils.EStrNull(kvar.phone) +
                                        ", porch = " + Utils.EStrNull(kvar.porch) +
                                        ", fio   = " + Utils.EStrNull(kvar.fio) +
                                        ", nkvar = " + Utils.EStrNull(kvar.nkvar) +
                                        ", ikvar = " + Utils.GetInt(kvar.nkvar) +
                                        ", nkvar_n=" + Utils.EStrNull(kvar.nkvar_n) +
                                        ", uch    =" + Utils.EStrNull(kvar.uch) +
                                        ", remark =" + Utils.EStrNull(kvar.remark) +
                                        ", typek = " + kvar.typek +
                                        ", nzp_area = " + kvar.nzp_area +
                                        ", nzp_geu  = " + kvar.nzp_geu +
                                    " Where nzp_kvar= " + kvar.nzp_kvar;
#else
                sql =
                    " Update " + kvar.pref + "_data:kvar " +
                    " Set  phone = " + Utils.EStrNull(kvar.phone) +
                        ", porch = " + Utils.EStrNull(kvar.porch) +
                        ", fio   = " + Utils.EStrNull(kvar.fio) +
                        ", nkvar = " + Utils.EStrNull(kvar.nkvar) +
                        ", ikvar = " + Utils.GetInt(kvar.nkvar) +
                        ", nkvar_n=" + Utils.EStrNull(kvar.nkvar_n) +
                        ", uch    =" + Utils.EStrNull(kvar.uch) +
                        ", remark =" + Utils.EStrNull(kvar.remark) +
                        ", nzp_area = " + kvar.nzp_area +
                        ", nzp_geu  = " + kvar.nzp_geu +
                    " Where nzp_kvar= " + kvar.nzp_kvar;
#endif
                ret = ExecSQL(conn_db, transaction, sql, true);
                if (!ret.result)
                {
                    ret.text = "Ошибка записи данных лицевого счета";
                    return Constants._ZERO_;
                }

                ret = RefreshKvar(conn_db, transaction, kvar);
                if (!ret.result)
                {
                    ret.text = "Ошибка записи данных лицевого счета";
                    return Constants._ZERO_;
                }

                sql = "delete from " + kvar.pref + sDataAliasRest + "kvar_block where nzp_kvar = " + kvar.nzp_kvar;

                ret = ExecSQL(conn_db, transaction, sql, true);
                if (!ret.result)
                {
                    ret.text = "Ошибка записи данных лицевого счета";
                    return Constants._ZERO_;
                }

                #region обновление данных в выбранном списке л/с
                string tXX_spls = "t" + Convert.ToString(kvar.nzp_user) + "_spls";

                if (TableInWebCashe(conn_web, tXX_spls))
                {
                    List<Ls> list = LoadLs(conn_db, transaction, kvar, out ret);
                    if (!ret.result)
                    {
                        return Constants._ZERO_;
                    }

                    if (list != null && list.Count > 0)
                    {
                        sql = "update " + tXX_spls + " set nzp_area = " + list[0].nzp_area + ", nzp_geu = " + list[0].nzp_geu + ", fio = '" + list[0].fio +
                            "', nkvar = '" + list[0].nkvar + " " + list[0].nkvar_n + "', adr ='" + list[0].adr + "', sostls = " + Utils.EStrNull(list[0].state) +
                            ", typek = " + list[0].typek +
                            " where nzp_kvar = " + kvar.nzp_kvar + " and pref = '" + kvar.pref + "'";
                        ret = ExecSQL(conn_web, sql, true);
                        if (!ret.result)
                        {
                            return Constants._ZERO_;
                        }
                    }
                }
                #endregion

            }
            #endregion

            #region добавление л/с
            else
            {

                var series = new Series(new int[] { 1 });

                var db = new DbSpravKernel();
                db.GetSeries(conn_db, transaction, kvar.pref, series, out ret);
                db.Close();
                if (!ret.result)
                {
                    ret.text = "Ошибка получения ключей: " + ret.text;
                    return Constants._ZERO_;
                }

                _Series val = series.GetSeries(1);
                kvar.nzp_kvar = (val.cur_val != Constants._ZERO_) ? val.cur_val : 0;

                kvar.num_ls = kvar.nzp_kvar;

                //int pkod10 = 0;
                ////в Самаре для переноса ЛС в другую УК pkod10 из старого ЛС
                //if (Points.IsSmr && kvar.moving)
                //{
                //    pkod10 = kvar.pkod10;
                //}
                string ins_1 = "", ins_2 = "", ins_1_1 = "", ins_2_2 = ""; ;
                if (!GlobalSettings.NewGeneratePkodMode)
                {
                    DbAdres dbAdres = new DbAdres();
                    int areaCode, pkod10 = (Points.IsSmr && kvar.moving) ? kvar.pkod10 : 0;
                    kvar.pkod = dbAdres.GeneratePkodOneLS(
                        new Ls()
                        {
                            nzp_user = kvar.nzp_user,
                            nzp_area = kvar.nzp_area,
                            nzp_geu = kvar.nzp_geu,
                            nzp_kvar = kvar.nzp_kvar,
                            pref = kvar.pref
                        }, transaction, out areaCode, ref pkod10, out ret);
                    dbAdres.Close();
                    if (kvar.pkod == "" || !ret.result)
                    {
                        MonitorLog.WriteLog("Ошибка генерации платежного кода: " + ret.text, MonitorLog.typelog.Error, true);
                        return Constants._ZERO_;
                    }

                    kvar.pkod10 = pkod10;
                    kvar.area_code = areaCode;
                    ins_1 = ",area_code,pkod,pkod10";
                    ins_2 = "," + kvar.area_code + "," + kvar.pkod + "," + kvar.pkod10;
                    ins_1_1 = ",pkod,pkod10";
                    ins_2_2= "," + kvar.pkod + "," + kvar.pkod10;
                }
                else
                {
                    ins_1 = ins_1_1 = "";
                    ins_2 = ins_2_2 = "";
                }
                //GeneratePkod(conn_db, transaction, kvar, out ret);

                //if (!ret.result)
                //{
                //    MonitorLog.WriteLog("Ошибка генерации платежного кода: в вызываемой функции GeneratePkod произошла ошибка" + kvar.pkod + "; " + ret.text, MonitorLog.typelog.Error, true);
                //    return Constants._ZERO_;
                //}

                //if (Points.IsSmr && kvar.moving)
                //{
                //    kvar.pkod10 = pkod10;
                //}              

                int ikvar = 0;
                if (kvar.nkvar.Trim().Length > 0)
                {
                    if (!int.TryParse(kvar.nkvar.Trim(), out ikvar)) ikvar = 0;
                }
                string dop_field = "", dop_values = "";
                if (kvar.fio != "")
                {
                    dop_field += ",fio";
                    dop_values += ",'" + kvar.fio + "'";
                }
                if (kvar.remark != "")
                {
                    dop_field += ",remark";
                    dop_values += ",'" + kvar.remark + "'";
                }
                if (kvar.uch != "")
                {
                    dop_field += ",uch";
                    dop_values += ",'" + kvar.uch + "'";
                }
                if (kvar.porch != "")
                {
                    dop_field += ",porch";
                    dop_values += ",'" + kvar.porch + "'";
                }

                //int code = GetAreaCodes(conn_db, transaction, kvar, out ret);
                //if (!ret.result)
                //{
                //    MonitorLog.WriteLog("Ошибка получения текущего area_codes.code", MonitorLog.typelog.Error, true);
                //    return Constants._ZERO_;
                //}
                //string cd = "0";
                //if (code > 0) cd = code.ToString();

                DbTables tables = new DbTables(conn_db);
                sql = " Insert into " + tables.kvar +
                      " (nzp_kvar,num_ls"+ins_1+", nkvar,nkvar_n,ikvar,nzp_dom,nzp_area,nzp_geu,typek" +
                      dop_field + ") " +
                      " Values (" + kvar.nzp_kvar + "," + kvar.num_ls + ins_2 + "," +
                      (kvar.nkvar.Trim() == "" ? "'-'" : "'" + kvar.nkvar.Trim() + "'") + "," +         // номер квартиры
                      (kvar.nkvar_n.Trim() == "" ? "'-'" : "'" + kvar.nkvar_n.Trim() + "'") + "," + // номер комнаты 
                      ikvar + "," + // числовой номер квартиры
                      kvar.nzp_dom + "," + kvar.nzp_area + "," + kvar.nzp_geu + ',' + kvar.typek + dop_values + ")";
                ret = ExecSQL(conn_db, transaction, sql, true);
                if (!ret.result)
                {
                    ret.text = "Ошибка записи данных лицевого счета";
                    return Constants._ZERO_;
                }

                sql = " Insert into " + kvar.pref + "_data" + tableDelimiter + "kvar (nzp_kvar,num_ls" + ins_1_1 + ", nkvar,nkvar_n,ikvar,nzp_dom,nzp_area,nzp_geu,typek" + dop_field + ") " +
                     " Values (" + kvar.nzp_kvar + "," + kvar.num_ls +ins_2_2 + "," +
                                  (kvar.nkvar.Trim() == "" ? "'-'" : "'" + kvar.nkvar.Trim() + "'") + "," + // номер квартиры
                                  (kvar.nkvar_n.Trim() == "" ? "'-'" : "'" + kvar.nkvar_n.Trim() + "'") + "," + // номер комнаты 
                                  ikvar + "," +  // числовой номер квартиры
                                  kvar.nzp_dom + "," + kvar.nzp_area + "," + kvar.nzp_geu + ',' + kvar.typek + dop_values + ")";

                ret = ExecSQL(conn_db, transaction, sql, true);
                if (!ret.result)
                {
                    ret.text = "Ошибка записи данных лицевого счета";
                    return Constants._ZERO_;
                }

                if (kvar.stateID > 0)
                {
                    Param prm = new Param();
                    kvar.CopyTo(prm);
                    RecordMonth r_m = Points.GetCalcMonth(new CalcMonthParams(kvar.pref));
                    prm.dat_s = new DateTime(r_m.year_, r_m.month_, 1).ToShortDateString();
                    prm.nzp = kvar.nzp_kvar;
                    prm.nzp_prm = 51;
                    prm.val_prm = kvar.stateID.ToString();
                    prm.prm_num = 3;

                    DbParameters dbparam = new DbParameters();
                    ret = dbparam.SavePrm(conn_db, transaction, prm);
                    dbparam.Close();

                    if (!ret.result)
                    {
                        return Constants._ZERO_;
                    }
                }

                ret = RefreshKvar(conn_db, transaction, kvar);
                if (!ret.result)
                {
                    ret.text = "Ошибка записи данных лицевого счета";
                    return Constants._ZERO_;
                }

                #region Добавление в sys_events события 'Добавление лицевого счёта'
                try
                {
                    string area = "";
                    var tsql = "select sa.area from " + kvar.pref + "_data" + tableDelimiter + "kvar k  " +
                       " join " + kvar.pref + "_data" + tableDelimiter + "s_area sa on k.nzp_area = sa.nzp_area " +
                       " where nzp_kvar = " + kvar.nzp_kvar;

                    ExecRead(conn_db, transaction, out reader, tsql, true);

                    if (reader.Read())
                    {
                        if (reader["area"] != DBNull.Value) area = Convert.ToString(reader["area"]).Trim();
                    }
                    reader.Close();

                    DbAdmin.InsertSysEvent(new SysEvents()
                    {
                        pref = kvar.pref,
                        nzp_user = kvar.nzp_user,
                        nzp_dict = 6481,
                        nzp_obj = kvar.nzp_kvar,
                        note = "УК " + area
                    }, transaction, conn_db);
                }
                catch (Exception ex)
                {
                    MonitorLog.WriteLog("Ошибка добавления события в sys_events" + ex.Message, MonitorLog.typelog.Error, 20, 201, true);
                }
                #endregion

                #region обновление данных в выбранном списке л/с

                string tXX_spls = "t" + Convert.ToString(kvar.nzp_user) + "_spls";

                if (TableInWebCashe(conn_web, tXX_spls) && !kvar.moving) // при переносе ЛС не заходим
                {
                    List<Ls> list = LoadLs(conn_db, transaction, kvar, out ret);
                    if (!ret.result)
                    {
                        return Constants._ZERO_;
                    }

                    if (list != null && list.Count > 0)
                    {
                        sql = " Insert into " + tXX_spls +
                              " ( nzp_kvar, num_ls"+ins_1_1+", nzp_dom, nzp_area, nzp_geu, nzp_ul, typek, fio, adr, ulica, ndom," +
                              "   idom, nkvar, ikvar, stypek, sostls, pref ) " +
                              " Values(" + kvar.nzp_kvar + "," + kvar.num_ls + ins_2_2 + "," + kvar.nzp_dom + "," + kvar.nzp_area + "," + kvar.nzp_geu + "," +
                                       list[0].nzp_ul + "," + kvar.typek + ",'" + kvar.fio + "','" + list[0].adr + "','" +
                                       list[0].ulica + "','" + list[0].ndom + "'," + Utils.GetInt(list[0].ndom) + ",'" +
                                       list[0].nkvar + "'," + Utils.GetInt(list[0].nkvar) + ",'" + list[0].stypek + "'," + Utils.EStrNull(list[0].state) + ",'" + kvar.pref + "')";
                        ret = ExecSQL(conn_web, sql, true);
                        if (!ret.result)
                        {
                            return Constants._ZERO_;
                        }
                    }
                }
                #endregion
            }
            #endregion

            return kvar.nzp_kvar;
        }


        /// <summary>
        /// Добавить ЛС в tXX_spls
        /// </summary>
        /// <param name="conn_db"></param>
        /// <param name="transaction"></param>
        /// <param name="conn_web"></param>
        /// <param name="kvar"></param>
        /// <param name="ret"></param>
        /// <returns></returns>
        private Returns AddLsToCache(IDbConnection conn_db, IDbTransaction transaction, IDbConnection conn_web, Ls kvar)
        {
            string tXX_spls = "t" + Convert.ToString(kvar.nzp_user) + "_spls";

            Returns ret;

            List<Ls> list = LoadLs(conn_db, transaction, kvar, out ret);
            if (!ret.result)
            {
                return ret;
            }
            if (list == null || list.Count == 0)
            {
                return new Returns(false, "Лицевой счет по адресу " + kvar.adr + " не найден");
            }

            string sql = "select nzp_kvar from " + tXX_spls + " where nzp_kvar = " + kvar.nzp_kvar;

            IDataReader reader;
            ret = ExecRead(conn_web, out reader, sql, true);
            if (!ret.result) return ret;

            if (reader.Read())
            {
                sql = "update " + tXX_spls + " set num_ls = " + list[0].num_ls +
                    ", pkod10 = " + list[0].pkod10 +
                    ", pkod = " + list[0].pkod +
                    ", nzp_area = " + list[0].nzp_area +
                    ", nzp_geu = " + list[0].nzp_geu +
                    ", typek = " + list[0].typek +
                    ", stypek ='" + list[0].stypek + "'" +
                    ", fio = '" + list[0].fio + "'" +
                    ", adr ='" + list[0].adr + "'" +
                    ", nkvar = '" + list[0].nkvar + " " + list[0].nkvar_n + "'" +
                    ", ikvar = " + Utils.GetInt(list[0].nkvar) +
                    ", sostls ='" + list[0].state + "'" +
                    " where nzp_kvar = " + kvar.nzp_kvar;
            }
            else
            {
                sql = " Insert into " + tXX_spls +
                      " ( nzp_kvar, num_ls, pkod, pkod10, nzp_dom, nzp_area, nzp_geu, nzp_ul, typek, fio, adr, ulica, ndom," +
                      "   idom, nkvar, ikvar, stypek, sostls, pref ) " +
                      " Values(" + kvar.nzp_kvar +
                      "," + list[0].num_ls +
                      "," + list[0].pkod +
                      "," + list[0].pkod10 +
                      "," + list[0].nzp_dom +
                      "," + list[0].nzp_area + "," + list[0].nzp_geu + "," +
                               list[0].nzp_ul + "," + list[0].typek + ",'" + list[0].fio + "','" + list[0].adr + "','" +
                               list[0].ulica + "','" + list[0].ndom + "'," + Utils.GetInt(list[0].ndom) + ",'" +
                               list[0].nkvar + "'," + Utils.GetInt(list[0].nkvar) + ",'" + list[0].stypek + "','','" + list[0].pref + "')";
            }

            ret = ExecSQL(conn_web, sql, true);
            return ret;
        }

        //----------------------------------------------------------------------
        public int Update(IDbConnection conn_db, IDbConnection conn_web, Ls kvar, out Returns ret) //исправить данные квартиры
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            int rez = Constants._ZERO_;

            IDbTransaction transaction;
            try { transaction = conn_db.BeginTransaction(); }
            catch { transaction = null; }

            rez = Update(conn_db, transaction, conn_web, kvar, out ret);

            if (!ret.result)
            {
                if (transaction != null) transaction.Rollback();
                return rez;
            }
            if (transaction != null) transaction.Commit();
            return rez;
        }


        public int Update(IDbConnection conn_db, Dom dom, out Returns ret)
        {
            ret = Utils.InitReturns();

            if (dom.nkor.Trim() == "") dom.nkor = "-";

            DbTables tables = new DbTables(conn_db);

            #region получить r.nzp_raj, t.nzp_town, s.nzp_stat, l.nzp_land

            IDataReader reader;
            string sql = " select r.nzp_raj, t.nzp_town, s.nzp_stat, l.nzp_land " +
                         " from " + tables.ulica + " u " +
                         " inner join " + tables.rajon + " r on u.nzp_raj = r.nzp_raj  " +
                         " inner join " + tables.town + "  t on t.nzp_town = r.nzp_town " +
                         " inner join " + tables.stat + "  s on s.nzp_stat = t.nzp_stat " +
                         " inner join " + tables.land + "  l on l.nzp_land = s.nzp_land " +
                         " where u.nzp_ul = " + dom.nzp_ul;
            if (!ExecRead(conn_db, out reader, sql, true).result)
            {
                ret.result = false;
                return 0;
            }
            try
            {
                if (reader.Read())
                {
                    //if (reader["nzp_raj"] != DBNull.Value) dom.nzp_raj = (int)reader["nzp_raj"];
                    if (reader["nzp_town"] != DBNull.Value) dom.nzp_town = (int) reader["nzp_town"];
                    if (reader["nzp_stat"] != DBNull.Value) dom.nzp_stat = (int) reader["nzp_stat"];
                    if (reader["nzp_land"] != DBNull.Value) dom.nzp_land = (int) reader["nzp_land"];
                }
                reader.Close();
            }
            catch (Exception ex)
            {
                reader.Close();

                ret.result = false;
                ret.text = ex.Message;

                string err;
                if (Constants.Viewerror)
                    err = " \n " + ex.Message;
                else
                    err = "";

                MonitorLog.WriteLog("Ошибка Update домов " + err, MonitorLog.typelog.Error, 20, 201, true);

                return 0;
            }

            #endregion

            #region проверка на существование дома

            sql =
                "select count(*) as num from " + dom.pref + DBManager.sDataAliasRest + "dom " +
                " where idom   =" + Utils.GetInt(dom.ndom) +
                " and ndom     =" + Utils.EStrNull(dom.ndom.Trim()) +
                " and nkor     =" + Utils.EStrNull(dom.nkor.Trim()) +
                " and nzp_ul   =" + dom.nzp_ul;
            if (dom.nzp_dom > Constants._ZERO_) sql += " and nzp_dom <> " + dom.nzp_dom;

            int num = 0;
            if (!ExecRead(conn_db, out reader, sql, true).result)
            {
                ret.result = false;
                return 0;
            }
            try
            {
                if (reader.Read())
                {
                    if (reader["num"] != DBNull.Value) num = Convert.ToInt32(reader["num"]);
                }
                reader.Close();
                if (num > 0)
                {
                    ret.result = false;
                    ret.tag = 1;
                    ret.text = "Дом с таким номером уже существует";
                    return 0;
                }
            }
            catch (Exception ex)
            {
                reader.Close();

                ret.result = false;
                ret.text = ex.Message;

                string err;
                if (Constants.Viewerror)
                    err = " \n " + ex.Message;
                else
                    err = "";

                MonitorLog.WriteLog("Ошибка Update домов " + err, MonitorLog.typelog.Error, 20, 201, true);

                return 0;
            }

            #endregion

            bool edit = false;

            if (dom.nzp_dom != Constants._ZERO_)
            {
                #region если редактирование
                edit = true;

                if (dom.clear_remark)
                {
                    dom.remark = "";
                }
                ;
                sql = "select nzp_dom from " + tables.s_remark + " where nzp_dom=" + dom.nzp_dom + "";
                if (!ExecRead(conn_db, out reader, sql, true).result)
                {
                    ret.result = false;
                    return 0;
                }
                try
                {
                    if (reader.Read())
                    {
                        sql = "update " + tables.s_remark + " " + "set (nzp_area, nzp_geu, nzp_dom, remark) = " +
                              " (0, 0, " + dom.nzp_dom + ", " + Utils.EStrNull(dom.remark) + ") " + 
                              " where nzp_dom = " + dom.nzp_dom + "";
                        ret = ExecSQL(conn_db, sql, true);
                        if (!ret.result)
                        {
                            ret.text = "Ошибка записи данных дома";
                            return 0;
                        }
                    }
                    else
                    {
                        sql = " insert into " + tables.s_remark + "" + " (nzp_area, nzp_geu, nzp_dom, remark)" +
                              " values (0, 0, " + dom.nzp_dom
                              + ", " + Utils.EStrNull(dom.remark) + ")";
                        ret = ExecSQL(conn_db, sql, true);
                        if (!ret.result)
                        {
                            ret.text = "Ошибка записи данных дома";
                            return 0;
                        }
                    }

                }
                catch (Exception ex)
                {
                    MonitorLog.WriteException("Ошибка функции Update ", ex);
                    reader.Close();
                    return 0;
                }
                reader.Close();

                if (num > 0)
                {
                    ret.result = false;
                    ret.tag = 1;
                    ret.text = "Дом с таким номером уже существует";
                    return 0;
                }

                #region Добавление в sys_events события 'Изменение адреса дома'

                try
                {
                    string ndom = "";
                    string nkor = "";
                    var nzp_ul = 0;
                    var nzp_raj = 0;
                    var nzp_town = 0;
                    var nzp_area = 0;
                    var nzp_geu = 0;

                    sql =
                        " SELECT * FROM " + dom.pref + DBManager.sDataAliasRest + "dom d " +
                        " WHERE d.nzp_dom = " + dom.nzp_dom;
                    ExecRead(conn_db, out reader, sql, true);

                    if (reader.Read())
                    {
                        if (reader["ndom"] != DBNull.Value) ndom = Convert.ToString(reader["ndom"]).Trim();
                        if (reader["nkor"] != DBNull.Value) nkor = Convert.ToString(reader["nkor"]).Trim();
                        if (reader["nzp_ul"] != DBNull.Value) nzp_ul = Convert.ToInt32(reader["nzp_ul"]);
                        if (reader["nzp_raj"] != DBNull.Value) nzp_raj = Convert.ToInt32(reader["nzp_raj"]);
                        if (reader["nzp_town"] != DBNull.Value) nzp_town = Convert.ToInt32(reader["nzp_town"]);
                        if (reader["nzp_area"] != DBNull.Value) nzp_area = Convert.ToInt32(reader["nzp_area"]);
                        if (reader["nzp_geu"] != DBNull.Value) nzp_geu = Convert.ToInt32(reader["nzp_geu"]);
                    }
                    reader.Close();

                    string note = "";
                    //если изменился адрес
                    if (ndom.Trim() != dom.ndom)
                    {
                        note += "Номер дома был изменен с " + ndom + " на " + dom.ndom + ". ";
                    }
                    if (nkor.Trim() != dom.nkor)
                    {
                        note += "Корпус был изменен с " + nkor + " на " + dom.nkor + ". ";
                    }
                    if (nzp_ul != dom.nzp_ul)
                    {
                        var var_old = "пусто";
                        var var_new = "пусто";
                        if (dom.nzp_ul != 0 && dom.nzp_ul != -1)
                        {
                            var r = ExecScalar(conn_db,
                                "select ulica from " + Points.Pref + DBManager.sDataAliasRest + "s_ulica " +
                                " where nzp_ul = " + dom.nzp_ul, out ret, true);
                            if (r != null)
                                var_old = r.ToString().Trim();
                        }
                        if (nzp_ul != 0)
                        {
                            var r = ExecScalar(conn_db,
                                "select ulica from " + Points.Pref + DBManager.sDataAliasRest + "s_ulica " +
                                " where nzp_ul = " + nzp_ul, out ret, true);
                            if (r != null)
                                var_new = r.ToString().Trim();
                        }
                        note += "Улица с " + var_old + " на " + var_new + ". ";
                    }
                    if (nzp_raj != dom.nzp_raj)
                    {
                        var var_old = "пусто";
                        var var_new = "пусто";
                        if (dom.nzp_raj != 0 && dom.nzp_raj != -1)
                        {
                            var r = ExecScalar(conn_db,
                                "select rajon from " + Points.Pref + DBManager.sDataAliasRest + "s_rajon " +
                                " where nzp_raj = " + dom.nzp_raj, out ret, true);
                            if (r != null)
                                var_old = r.ToString().Trim();
                        }
                        if (nzp_raj != 0)
                        {
                            var r = ExecScalar(conn_db,
                                "select rajon from " + Points.Pref + DBManager.sDataAliasRest + "s_rajon " +
                                " where nzp_raj = " + nzp_raj, out ret, true);
                            if (r != null)
                                var_new = r.ToString().Trim();
                        }
                        note += "Район с " + var_old + " на " + var_new + ". ";
                    }
                    if (nzp_town != dom.nzp_town)
                    {
                        var var_old = "пусто";
                        var var_new = "пусто";
                        if (dom.nzp_town != 0 && dom.nzp_town != -1)
                        {
                            var r = ExecScalar(conn_db,
                                "select town from " + Points.Pref + DBManager.sDataAliasRest + "s_town " +
                                " where nzp_town = " + dom.nzp_town, out ret, true);
                            if (r != null)
                                var_old = r.ToString().Trim();
                        }
                        if (nzp_town != 0)
                        {
                            var r = ExecScalar(conn_db,
                                " select town from " + Points.Pref + DBManager.sDataAliasRest + "s_town " +
                                " where nzp_town = " + nzp_town, out ret, true);
                            if (r != null)
                                var_new = r.ToString().Trim();
                        }
                        note += "Город с " + var_old + " на " + var_new + ". ";
                    }
                    if (nzp_area != dom.nzp_area)
                    {
                        var var_old = "пусто";
                        var var_new = "пусто";
                        if (dom.nzp_area != 0 && dom.nzp_area != -1)
                        {
                            var r = ExecScalar(conn_db,
                                " SELECT area FROM " + Points.Pref + DBManager.sDataAliasRest + " s_area " +
                                " WHERE nzp_area = " + dom.nzp_area, out ret, true);
                            if (r != null)
                                var_new = r.ToString().Trim();
                            InsAreaFromCenterToLocal(conn_db, dom);
                        }
                        if (nzp_area != 0)
                        {
                            var r = ExecScalar(conn_db,
                                " SELECT area FROM " + Points.Pref + DBManager.sDataAliasRest + " s_area " +
                                " WHERE nzp_area = " + nzp_area, out ret, true);
                            if (r != null)
                                var_old = r.ToString().Trim();
                        }
                        note += "Управляющая организация с " + var_old + " на " + var_new + ". ";
                    }
                    if (nzp_geu != dom.nzp_geu)
                    {
                        var var_old = "пусто";
                        var var_new = "пусто";
                        if (dom.nzp_geu != 0 && dom.nzp_geu != -1)
                        {
                            var r = ExecScalar(conn_db,
                                " SELECT geu FROM " + Points.Pref + DBManager.sDataAliasRest + "s_geu " +
                                " WHERE nzp_geu = " + dom.nzp_geu, out ret, true);
                            if (r != null)
                                var_new = r.ToString().Trim();
                            InsGeuFromCenterToLocal(conn_db, dom);
                        }
                        if (nzp_geu != 0)
                        {
                            var r = ExecScalar(conn_db,
                                " SELECT geu FROM " + Points.Pref + DBManager.sDataAliasRest + "s_geu " +
                                " WHERE nzp_geu = " + nzp_geu, out ret, true);
                            if (r != null)
                                var_old = r.ToString().Trim();
                            
                        }
                        note += "ЖЭУ с " + var_old + " на " + var_new + ". ";
                    }

                    if (note != "")
                    {
                        DbAdmin.InsertSysEvent(new SysEvents()
                        {
                            pref = dom.pref,
                            nzp_user = dom.nzp_user,
                            nzp_dict = 8215,
                            nzp_obj = dom.nzp_dom,
                            note = note
                        }, conn_db);
                    }
                }
                catch (Exception ex)
                {
                    MonitorLog.WriteLog("Ошибка добавления события в sys_events" + ex.Message, MonitorLog.typelog.Error,
                        20, 201, true);
                }

                #endregion





#if PG
                sql =
                    " Update " + dom.pref + "_data.dom " +
                    " Set idom     =" + Utils.GetInt(dom.ndom) +
                    ",ndom     =" + Utils.EStrNull(dom.ndom) +
                    ",nkor     =" + Utils.EStrNull(dom.nkor) +
                    ",nzp_area =" + dom.nzp_area +
                    ",nzp_geu  =" + dom.nzp_geu +
                    ",nzp_ul   =" + dom.nzp_ul +
                    ",nzp_land =" + dom.nzp_land +
                    ",nzp_stat =" + dom.nzp_stat +
                    ",nzp_town =" + dom.nzp_town +
                    ",nzp_raj  =" + dom.nzp_raj +
                    " Where nzp_dom= " + dom.nzp_dom;
#else
                sql =
                                    " Update " + dom.pref + "_data:dom " +
                                    " Set idom     =" + Utils.GetInt(dom.ndom) +
                                        ",ndom     =" + Utils.EStrNull(dom.ndom) +
                                        ",nkor     =" + Utils.EStrNull(dom.nkor) +
                                        ",nzp_area =" + dom.nzp_area +
                                        ",nzp_geu  =" + dom.nzp_geu +
                                        ",nzp_ul   =" + dom.nzp_ul +
                                        ",nzp_land =" + dom.nzp_land +
                                        ",nzp_stat =" + dom.nzp_stat +
                                        ",nzp_town =" + dom.nzp_town +
                                        ",nzp_raj  =" + dom.nzp_raj +
                                    " Where nzp_dom= " + dom.nzp_dom;
#endif

                ret = RefreshDom(conn_db, dom);
                if (!ret.result)
                {
                    ret.text = "Ошибка записи данных дома";
                    return 0;
                }

                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result)
                {
                    ret.text = "Ошибка записи данных дома";
                    return 0;
                }

                //обновление жэу и управляющих организаций всем лс дома
                sql = "update " + dom.pref + "_data" + tableDelimiter + "kvar "+
                    "set nzp_area = " + dom.nzp_area + 
                    ", nzp_geu = " + dom.nzp_geu +
                    " where nzp_dom = " + dom.nzp_dom;
                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result)
                {
                    ret.text = "Ошибка записи данных дома";
                    return 0;
                }

                #endregion если редактирование
            }
            else
            {
                #region добавление
                Series series = new Series(new int[] {3});
                DbSprav db = new DbSprav();
                db.GetSeries(dom.pref, series, out ret);
                db.Close();
                if (!ret.result)
                {
                    ret.text = "Ошибка получения ключей: " + ret.text;
                    return 0;
                }

                bool b = false;
                _Series val = series.GetSeries(3);
                if (val.cur_val != Constants._ZERO_) dom.nzp_dom = val.cur_val;
                else b = true;

                if (b)
                {
                    ret.text = "Внутренняя ошибка получения ключей: " + ret.text;
                    ret.result = false;
                    return 0;
                }

                #region Добавление в sys_events события 'Добавление дома'

                try
                {
                    string ndom = "";
                    string nkor = "";
                    string ulica = "";
                    string rajon = "";
                    string town = "";

                    sql = "  select '" + dom.ndom + "' as ndom, '" + dom.nkor + "' as nkor, * " +
                          " from " + dom.pref + DBManager.sDataAliasRest + "s_ulica ul " +
                          " left join " + dom.pref + DBManager.sDataAliasRest + "s_town t on t.nzp_town = " +
                          dom.nzp_town +
                          " left join " + dom.pref + DBManager.sDataAliasRest + "s_rajon r on r.nzp_raj = " +
                          dom.nzp_raj +
                          " where ul.nzp_ul = " + dom.nzp_ul;
                    ExecRead(conn_db, out reader, sql, true);

                    if (reader.Read())
                    {
                        ndom = reader["ndom"] != DBNull.Value ? Convert.ToString(reader["ndom"]).Trim() + " " : "";
                        nkor = reader["nkor"] != DBNull.Value ? Convert.ToString(reader["nkor"]).Trim() + " " : "";
                        ulica = reader["ulica"] != DBNull.Value ? Convert.ToString(reader["ulica"]).Trim() + ", " : "";
                        rajon = reader["rajon"] != DBNull.Value ? Convert.ToString(reader["rajon"]).Trim() + ", " : "";
                        town = reader["town"] != DBNull.Value ? Convert.ToString(reader["town"]).Trim() + ", " : "";
                    }
                    reader.Close();

                    DbAdmin.InsertSysEvent(new SysEvents()
                    {
                        pref = dom.pref,
                        nzp_user = dom.nzp_user,
                        nzp_dict = 6484,
                        nzp_obj = dom.nzp_dom,
                        note = "Был добавлен дом по адресу " + town + rajon + ulica + ndom + nkor
                    }, conn_db);
                }
                catch (Exception ex)
                {
                    MonitorLog.WriteLog("Ошибка добавления события в sys_events" + ex.Message, MonitorLog.typelog.Error,
                        20, 201, true);
                }

                #endregion

                sql =
                    " Insert into " + tables.dom +
                    " (nzp_dom,nzp_area,nzp_geu,nzp_ul,idom,ndom,nkor,nzp_land,nzp_stat,nzp_town,nzp_raj) " +
                    " Values (" + dom.nzp_dom + "," + dom.nzp_area + "," + dom.nzp_geu + "," + dom.nzp_ul + "," +
                    Utils.GetInt(dom.ndom) + "," + Utils.EStrNull(dom.ndom) + "," + Utils.EStrNull(dom.nkor) + "," +
                    dom.nzp_land + "," + dom.nzp_stat + "," + dom.nzp_town + "," + dom.nzp_raj + ")";
                
                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result)
                {
                    ret.text = "Ошибка записи данных дома в центральный банк";
                    return 0;
                }


                ret = InsAdrFromCenterToLocal(conn_db, dom);
                if (!ret.result)
                {
                    return 0;
                }
                ret = InsAreaFromCenterToLocal(conn_db, dom);
                if (!ret.result)
                {
                    ret.text = "Ошибка записи данных Управляющей организации в локальный БД";
                    return 0;
                }
                ret = InsGeuFromCenterToLocal(conn_db, dom);
                if (!ret.result)
                {
                    ret.text = "Ошибка записи данных ЖЭУ в локальный БД";
                    return 0;
                }

                sql =
                    " INSERT INTO " + dom.pref + DBManager.sDataAliasRest + "dom " +
                    " (nzp_dom, nzp_area, nzp_geu, nzp_ul, " +
                    " idom, ndom, nkor," +
                    " nzp_land, nzp_stat, nzp_town, nzp_raj) "+ 
                    " VALUES (" + 
                    dom.nzp_dom + "," + dom.nzp_area + "," + dom.nzp_geu + "," + dom.nzp_ul + "," +
                    Utils.GetInt(dom.ndom) + "," + Utils.EStrNull(dom.ndom) + "," + Utils.EStrNull(dom.nkor) + "," +
                    dom.nzp_land + "," + dom.nzp_stat + "," + dom.nzp_town + "," + dom.nzp_raj + ")";

                #endregion
                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result)
                {
                    ret.text = "Ошибка записи данных дома";
                    return 0;
                }
            }
            
         

            if (dom.clear_remark)
            {
                dom.remark = "";
            };

            sql = "select nzp_dom from " + tables.s_remark + " where nzp_dom=" + dom.nzp_dom + "";
            if (!ExecRead(conn_db, out reader, sql, true).result)
            {
                ret.result = false;
                return 0;
            }
            try
            {
                if (reader.Read())
                {
                    sql = "update " + tables.s_remark + " " +
                          "set (nzp_area, nzp_geu, nzp_dom, remark) = (0, 0, " + dom.nzp_dom + ", " +
                          Utils.EStrNull(dom.remark) + ") " +
                          "where nzp_dom = " + dom.nzp_dom + "";
                    ret = ExecSQL(conn_db, sql, true);
                    if (!ret.result)
                    {
                        ret.text = "Ошибка записи данных дома";
                        return 0;
                    }
                }
                else
                {
                    sql = " insert into " + tables.s_remark + "" +
                          " (nzp_area, nzp_geu, nzp_dom, remark)" +
                          " values (0, 0, " + dom.nzp_dom + ", " + Utils.EStrNull(dom.remark) + ")";
                    ret = ExecSQL(conn_db, sql, true);
                    if (!ret.result)
                    {
                        ret.text = "Ошибка записи данных дома";
                        return 0;
                    }
                }

            }
            catch (Exception ex)
            {
                MonitorLog.WriteException("Ошибка функции Update ", ex);
                reader.Close();
                return 0;
            }
            reader.Close();


            ret = RefreshDom(conn_db, dom);
            if (!ret.result)
            {
                ret.text = "Ошибка записи данных дома";
                return 0;
            }

            if (edit)
            {
                #region обновление данных в выбранном списке домов

                IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
                ret = OpenDb(conn_web, true);
                if (!ret.result)
                {
                    return Constants._ZERO_;
                }

                string tXX_spdom = sDefaultSchema + "t" + Convert.ToString(dom.nzp_user) + "_spdom";

                if (TempTableInWebCashe(conn_web, tXX_spdom))
                {
                    Ls ls = new Ls();
                    ls.nzp_dom = dom.nzp_dom;
                    ls.pref = dom.pref;
                    ls.nzp_user = dom.nzp_user;


                    List<Ls> list = LoadLs(ls, out ret);
                    if (!ret.result)
                    {
                        conn_web.Close();
                        return Constants._ZERO_;
                    }

                    sql = "update " + tXX_spdom + " set idom     =" + Utils.GetInt(list[0].ndom) +
                          ",ndom     =  'дом " + list[0].ndom + " корп. " + list[0].nkor + "'" +
                          ",nzp_area =" + list[0].nzp_area +
                          ",nzp_geu  =" + list[0].nzp_geu +
                          ",area = '" + dom.area + "'" +
                          ",geu = '" + dom.geu + "'" +
                          ",nzp_ul   =" + list[0].nzp_ul +
                          ",ulica ='" + list[0].ulica.Trim() + "'" +
                          ",ulicareg = '" + list[0].ulicareg.Trim() + "'" +
                          ",rajon = '" + list[0].rajon.Trim() + "'" +
                          " where nzp_dom = " + ls.nzp_dom + " and pref = '" + ls.pref + "'";
                    ret = ExecSQL(conn_web, sql, true);
                    if (!ret.result)
                    {
                        conn_web.Close();
                        return Constants._ZERO_;
                    }

                    #region обновление данных в выбранном списке л/с

                    string tXX_spls = sDefaultSchema + "t" + Convert.ToString(ls.nzp_user) + "_spls";

                    if (TempTableInWebCashe(conn_web, tXX_spls))
                    {
                        sql = "select nzp_kvar, pref from " + tXX_spls + " where nzp_dom = " + ls.nzp_dom +
                              " and pref = '" + ls.pref + "'";
                        if (!ExecRead(conn_web, out reader, sql, true).result)
                        {
                            ret.result = false;
                            conn_web.Close();
                            return Constants._ZERO_;
                        }
                        try
                        {
                            List<Ls> list2;
                            Ls ls2;
                            while (reader.Read())
                            {
                                ls2 = new Ls();
                                if (reader["nzp_kvar"] != DBNull.Value) ls2.nzp_kvar = (int) reader["nzp_kvar"];
                                if (reader["pref"] != DBNull.Value) ls2.pref = (string) reader["pref"];
                                ls2.nzp_user = dom.nzp_user;
                                list2 = LoadLs(ls2, out ret);
                                if (!ret.result)
                                {
                                    reader.Close();
                                    conn_web.Close();
                                    return Constants._ZERO_;
                                }

                                sql = "update " + tXX_spls + " set nzp_area = " + list2[0].nzp_area + ", nzp_geu = " +
                                      list2[0].nzp_geu + ", fio = '" + list2[0].fio +
                                      "', nkvar = '" + list2[0].nkvar + " " + list2[0].nkvar_n + "', adr ='" +
                                      list2[0].adr + "'"
                                      + " where nzp_kvar = " + ls2.nzp_kvar + " and pref = '" + ls2.pref + "'";
                                ret = ExecSQL(conn_web, sql, true);
                                if (!ret.result)
                                {
                                    reader.Close();
                                    conn_web.Close();
                                    return Constants._ZERO_;
                                }
                            }
                            reader.Close();
                        }
                        catch (Exception ex)
                        {
                            reader.Close();

                            ret.result = false;
                            ret.text = ex.Message;

                            string err;
                            if (Constants.Viewerror)
                                err = " \n " + ex.Message;
                            else
                                err = "";

                            MonitorLog.WriteLog("Ошибка Update домов " + err, MonitorLog.typelog.Error, 20, 201, true);

                            return 0;
                        }

                    }

                    #endregion

                }

                #endregion
            }
            else
            {
                #region обновление данных в выбранном списке домов

                IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
                ret = OpenDb(conn_web, true);
                if (!ret.result)
                {
                    return Constants._ZERO_;
                }

                string tXX_spdom = sDefaultSchema + "t" + Convert.ToString(dom.nzp_user) + "_spdom";

                if (TempTableInWebCashe(conn_web, tXX_spdom))
                {
                    Ls ls = new Ls();
                    ls.nzp_dom = dom.nzp_dom;
                    ls.pref = dom.pref;
                    ls.nzp_user = dom.nzp_user;


                    List<Ls> list = LoadLs(ls, out ret);
                    if (!ret.result)
                    {
                        conn_web.Close();
                        return Constants._ZERO_;
                    }

                    int nzp_wp = 0;
                    string point = "";
                    foreach (_Point zap in Points.PointList)
                    {
                        if (zap.pref != dom.pref) continue;

                        nzp_wp = zap.nzp_wp;
                        point = zap.point;

                        break;
                    }
                    if (list != null && list.Count > 0)
                    {
                        sql = "insert into " + tXX_spdom +
                              " (nzp_dom, nzp_ul, nzp_area, nzp_geu, nzp_wp, area, geu, ulica, ndom, idom, pref, point, ulicareg, rajon, town) " +
                              " values (" + dom.nzp_dom + "," + dom.nzp_ul + "," + dom.nzp_area + "," + dom.nzp_geu +
                              "," + nzp_wp + ",'" + list[0].area + "','" +
                              list[0].geu + "','" + list[0].ulica + "','дом " + dom.ndom + " корп. " + dom.nkor.Trim() +
                              "'," + Utils.GetInt(dom.ndom) + ",'" + dom.pref + "','" + point + "'" +
                              ",'" + list[0].ulicareg + "', " + "'" + list[0].rajon + "','" + list[0].town + "'" + ")";
                        ret = ExecSQL(conn_web, sql, true);
                        if (!ret.result)
                        {
                            conn_web.Close();
                            return Constants._ZERO_;
                        }
                    }
                }

                #endregion
            }

            return dom.nzp_dom;
        }

        public Returns InsAdrFromCenterToLocal(IDbConnection conn_db, Dom dom)
        {
            Returns ret = Utils.InitReturns();
            string sql = 
                " INSERT INTO " + dom.pref + sDataAliasRest + "s_ulica" +
                " (nzp_ul, ulica, ulicareg, nzp_raj, soato) " +
                " SELECT nzp_ul, ulica, ulicareg, nzp_raj, soato " +
                " FROM " + Points.Pref + sDataAliasRest + "s_ulica " +
                " WHERE nzp_ul = " + dom.nzp_ul + 
                " AND NOT EXISTS" +
                " (SELECT nzp_ul" +
                "  FROM " + dom.pref + sDataAliasRest + "s_ulica " +
                "  WHERE nzp_ul = " + dom.nzp_ul + ")";
            ret = ExecSQL(conn_db, sql, true);
            if (!ret.result)
            {
                ret.text = "Ошибка записи улицы в локальную БД";
                return ret;
            }

            sql = 
                " SELECT nzp_raj " +
                " FROM " + Points.Pref + sDataAliasRest + "s_ulica " +
                " WHERE nzp_ul = " + dom.nzp_ul;
            var nzp_raj = ExecScalar(conn_db, sql, out ret, true).ToInt();

            sql =
                " INSERT INTO " + dom.pref + sDataAliasRest + "s_rajon" +
                " (nzp_raj, nzp_town, rajon, rajon_t, soato) " +
                " SELECT nzp_raj, nzp_town, rajon, rajon_t, soato " +
                " FROM " + Points.Pref + sDataAliasRest + "s_rajon " +
                " WHERE nzp_raj =" + nzp_raj +
                " AND NOT EXISTS" +
                " (SELECT nzp_raj" +
                "  FROM " + dom.pref + sDataAliasRest + "s_rajon " +
                "  WHERE nzp_raj = " + nzp_raj + ")";
            ret = ExecSQL(conn_db, sql, true);
            if (!ret.result)
            {
                ret.text = "Ошибка записи населенного пункта в локальную БД";
                return ret;
            }

            sql =
                " SELECT nzp_town " +
                " FROM " + Points.Pref + sDataAliasRest + "s_rajon " +
                " WHERE nzp_raj = " + nzp_raj;
            var nzp_town = ExecScalar(conn_db, sql, out ret, true).ToInt();

            sql =
                " INSERT INTO " + dom.pref + sDataAliasRest + "s_town" +
                " (nzp_town, nzp_stat, town, town_t, soato) " +
                " SELECT nzp_town, nzp_stat, town, town_t, soato " +
                " FROM " + Points.Pref + sDataAliasRest + "s_town " +
                " WHERE nzp_town = " + nzp_town + 
                " AND NOT EXISTS" +
                " (SELECT nzp_town" +
                "  FROM " + dom.pref + sDataAliasRest + "s_town " +
                "  WHERE nzp_town = " + nzp_town + ")";
            ret = ExecSQL(conn_db, sql, true);
            if (!ret.result)
            {
                ret.text = "Ошибка записи города/района в локальную БД";
                return ret;
            }

            sql =
                " SELECT nzp_stat " +
                " FROM " + Points.Pref + sDataAliasRest + "s_town " +
                " WHERE nzp_town = " + nzp_town;
            var nzp_stat = ExecScalar(conn_db, sql, out ret, true).ToInt();

            sql =
                " INSERT INTO " + dom.pref + sDataAliasRest + "s_stat" +
                " (nzp_stat, nzp_land, stat, stat_t, soato) " +
                " SELECT nzp_stat, nzp_land, stat, stat_t, soato " +
                " FROM " + Points.Pref + sDataAliasRest + "s_stat " +
                " WHERE nzp_stat =" + nzp_stat + 
                " AND NOT EXISTS" +
                " (SELECT nzp_stat" +
                "  FROM " + dom.pref + sDataAliasRest + "s_stat " +
                "  WHERE nzp_stat = " + nzp_stat + ")";
            ret = ExecSQL(conn_db, sql, true);
            if (!ret.result)
            {
                ret.text = "Ошибка записи региона в локальную БД";
                return ret;
            }

            sql =
                " SELECT nzp_land " +
                " FROM " + Points.Pref + sDataAliasRest + "s_stat " +
                " WHERE nzp_stat = " + nzp_stat;
            var nzp_land = ExecScalar(conn_db, sql, out ret, true).ToInt();

            sql =
                " INSERT INTO " + dom.pref + sDataAliasRest + "s_land" +
                " (nzp_land, land, land_t, soato) " +
                " SELECT nzp_land, land, land_t, soato " +
                " FROM " + Points.Pref + sDataAliasRest + "s_land " +
                " WHERE nzp_land = " + nzp_land + 
                " AND NOT EXISTS" +
                " (SELECT nzp_land" +
                "  FROM " + dom.pref + sDataAliasRest + "s_land " +
                "  WHERE nzp_land = " + nzp_land + ")";
            ret = ExecSQL(conn_db, sql, true);
            if (!ret.result)
            {
                ret.text = "Ошибка записи страны в локальную БД";
                return ret;
            }

            return ret;
        }

        public Returns InsAreaFromCenterToLocal(IDbConnection conn_db, Dom dom)
        {
            Returns ret = Utils.InitReturns();
            string sql = "select count(*) from " + dom.pref + "_data" + tableDelimiter + "s_area " +
                " where nzp_area = " + dom.nzp_area;
            object obj = ExecScalar(conn_db, sql, out ret, true);
            if (!ret.result) throw new Exception(ret.text);
            int totalRecordCount = 0;
            try { totalRecordCount = Convert.ToInt32(obj); }
            catch { throw new Exception("Ошибка определения числа записей"); }
            if (totalRecordCount == 0)
            {
                 sql =  " insert into " + dom.pref + "_data" + tableDelimiter + "s_area "+
                        " (nzp_area, area, nzp_supp, nzp_payer)" +
                        " select nzp_area, area, nzp_supp, nzp_payer from " +
                        Points.Pref + "_data" + tableDelimiter + "s_area "+
                        " where nzp_area = " + dom.nzp_area;
                 ret = ExecSQL(conn_db, sql, true);
                 if (!ret.result)
                 {
                     ret.text = "Ошибка записи данных управляющей организации в локальный БД";
                     return ret;
                 }
            }
            return ret;
        }

        public Returns InsGeuFromCenterToLocal(IDbConnection conn_db, Dom dom)
        {
            Returns ret = Utils.InitReturns();
            string sql = "select count(*) from " + dom.pref + "_data" + tableDelimiter + "s_geu " +
                " where nzp_geu = " + dom.nzp_geu;
            object obj = ExecScalar(conn_db, sql, out ret, true);
            if (!ret.result) throw new Exception(ret.text);
            int totalRecordCount = 0;
            try { totalRecordCount = Convert.ToInt32(obj); }
            catch { throw new Exception("Ошибка определения числа записей"); }
            if (totalRecordCount == 0)
            {
                sql = " insert into " + dom.pref + "_data" + tableDelimiter + "s_geu " +
                       " (nzp_geu, geu)" +
                       " select nzp_geu, geu from " +
                       Points.Pref + "_data" + tableDelimiter + "s_geu " +
                       " where nzp_geu = " + dom.nzp_geu;
                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result)
                {
                    ret.text = "Ошибка записи данных ЖЭУ в локальный БД";
                    return ret;
                }
            }
            return ret;
        }

        //----------------------------------------------------------------------
        public int Update(Ls kvar, out Returns ret) //исправить данные квартиры
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            int rez = Constants._ZERO_;

            #region Проверка входных параметров
            if (kvar.nzp_user < 1)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Не определен пользователь";
                return rez;
            }
            if (kvar.pref == "")
            {
                ret.result = false;
                ret.tag = -2;
                ret.text = "Не определен префикс";
                return rez;
            }
            #endregion

            #region Подключение к БД
            string connectionString = Points.GetConnByPref(kvar.pref);
            IDbConnection conn_db = GetConnection(connectionString);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return Constants._ZERO_;

            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result)
            {
                conn_db.Close();
                return Constants._ZERO_;
            }
            #endregion

            rez = Update(conn_db, conn_web, kvar, out ret);

            if (!ret.result)
            {
                conn_web.Close();
                conn_db.Close();
                return rez;
            }
            conn_web.Close();
            conn_db.Close();
            return rez;
        }

        public Returns SaveUlica(Ulica finder)
        {IDataReader reader;
            if (finder.pref == "") finder.pref = Points.Pref;

            if (finder.nzp_user < 1) return new Returns(false, "Не задан пользователь");
            if (finder.nzp_raj < 1) return new Returns(false, "Не задан район");
            if (finder.ulica.Trim() == "") return new Returns(false, "Не задано наименование улицы");
         //   if ((finder.nzp_ul < 1) && (finder.pref != Points.Pref)) return new Returns(false, "Не задан код улицы для локального банка");

            if (finder.ulica.IndexOf("'") != -1) return new Returns(false, "Кавычки в названии улицы недопустимы");
            if (finder.ulica.IndexOf("\"") != -1) return new Returns(false, "Кавычки в названии улицы недопустимы");

            finder.ulica = finder.ulica.Trim().ToUpper();
            finder.ulicareg = finder.ulicareg.Trim().ToUpper();

            #region подключение к базе
            string conn_kernel = Points.GetConnByPref(Points.Pref);
            IDbConnection conn_db = GetConnection(conn_kernel);
            Returns ret = OpenDb(conn_db, true);
            if (!ret.result) return ret;
            #endregion

#if PG
            string table = finder.pref + "_data.s_ulica";
#else
            string table = finder.pref + "_data:s_ulica";
#endif
            string sql;

            #region проверки
            if (finder.pref == Points.Pref)
            {
                sql = "select nzp_ul from " + table + " where Upper(ulica) = '" + finder.ulica + "' and nzp_raj = " + finder.nzp_raj;
                if (finder.nzp_ul > 0) sql += "and nzp_ul <> " + finder.nzp_ul;

                ret = ExecRead(conn_db, out reader, sql, true);
                if (!ret.result)
                {
                    conn_db.Close();
                    return ret;
                }
                if (reader.Read())
                {
                    CloseReader(ref reader);
                    conn_db.Close();
                    return new Returns(false, "Уже есть улица с таким именем.");
                }
            }
            #endregion
            
            

            //bool isNew;
            if (finder.nzp_ul > 0)
            {
              //  isNew = false;
                sql = "select nzp_ul from " + table + " where nzp_ul = " + finder.nzp_ul;               
                ret = ExecRead(conn_db, out reader, sql, true);
                if (!ret.result)
                {
                    conn_db.Close();
                    return ret;
                }
                if (reader.Read())
                {
                    sql = "Update " + table +
                        " Set ulica = " + Utils.EStrNull(finder.ulica) + "," +
                        " ulicareg = " + Utils.EStrNull(finder.ulicareg) +
                        " Where nzp_ul = " + finder.nzp_ul;
                }
                else
                {
                    sql = "insert into " + table + " (nzp_ul, nzp_raj, ulica, ulicareg ) values (" + finder.nzp_ul + "," + finder.nzp_raj + ", '" + finder.ulica + "', '" + finder.ulicareg + "')";
                }
                CloseReader(ref reader);
            }
            else
            {
               // isNew = true;
                DbSprav db = new DbSprav();
                ret = db.GetNewId(conn_db, Series.Types.Ulica);
                if (!ret.result)
                {
                    conn_db.Close();
                    return ret;
                }

                finder.nzp_ul = ret.tag;

                sql = "insert into " + table + " (nzp_ul, nzp_raj, ulica, ulicareg) values (" + finder.nzp_ul + "," + finder.nzp_raj + ",'" + finder.ulica + "', '" + finder.ulicareg + "')";
            }

            ret = ExecSQL(conn_db, sql, true);

            if (ret.result)
            {
                ret.tag = finder.nzp_ul;
            }

            if ((finder.pref == Points.Pref) /*&& !isNew*/)
            {
                foreach (_Point zap in Points.PointList)
                {
#if PG
                    sql = "Update " + zap.pref + "_data.s_ulica" +
                                            " Set ulica = " + Utils.EStrNull(finder.ulica) + "," +
                                            " ulicareg = " + Utils.EStrNull(finder.ulicareg) +
                                            " Where nzp_ul = " + finder.nzp_ul;
#else
                    sql = "Update " + zap.pref + "_data:s_ulica" +
                                            " Set ulica = " + Utils.EStrNull(finder.ulica) + "," +
                                            " ulicareg = " + Utils.EStrNull(finder.ulicareg) +
                                            " Where nzp_ul = " + finder.nzp_ul;
#endif
                    ExecSQL(conn_db, sql, false);
                }

                if (finder.dopFind != null && finder.dopFind.Count > 0)
                {
                    if (finder.pref.Trim() != finder.dopFind[0].Trim())
                    {
                        sql = "select * from " + finder.dopFind[0] + "_data" + tableDelimiter + "s_ulica where nzp_ul = " + finder.nzp_ul;
                        ret = ExecRead(conn_db, out reader, sql, true);
                        if (!reader.Read())
                        {
                            sql = "insert into " + finder.dopFind[0] + "_data" + tableDelimiter + "s_ulica " + 
                                " (nzp_ul, nzp_raj, ulica, ulicareg) values (" + finder.nzp_ul + "," + finder.nzp_raj + ",'" + finder.ulica + "', '" + finder.ulicareg + "')";
                            ret = ExecSQL(conn_db, sql, true);
                        }
                        reader.Close();
                    }
                }
            }

            conn_db.Close();

            return ret;
        }

        //----------------------------------------------------------------------
        public int Update(Dom dom, out Returns ret) //исправить данные дома
        //----------------------------------------------------------------------
        {
            string conn_kernel = Points.GetConnByPref(dom.pref);
            IDbConnection conn_db = GetConnection(conn_kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result)
            {
                return 0;
            }
            int nzp_dom = Update(conn_db, dom, out ret);
            conn_db.Close();
            return nzp_dom;
        }




        public Returns RefreshKvar(IDbConnection conn_db, IDbTransaction transaction, Ls finder)
        {
            if (finder.pref == "") return new Returns(false, "Не задан префикс БД");

            DbTables tables = new DbTables(conn_db);
#if PG
            string local_kvar = finder.pref + "_data.kvar";
            string s_point = Points.Pref + "_kernel.s_point";
            string prm_3 = finder.pref + "_data.prm_3";
#else
            string local_kvar = finder.pref + "_data@" + DBManager.getServer(conn_db) + ":kvar";
            string s_point = Points.Pref + "_kernel@" + DBManager.getServer(conn_db) + ":s_point";
            string prm_3 = finder.pref + "_data@" + DBManager.getServer(conn_db) + ":prm_3";
#endif


            string sql = "select nzp_kvar from " + tables.kvar + " where nzp_kvar = " + finder.nzp_kvar;
            IDataReader reader;
            Returns ret = ExecRead(conn_db, transaction, out reader, sql, true);
            if (!ret.result) return ret;

            if (!reader.Read())
            {
                DbAdres dbAdres = new DbAdres();
                int code = dbAdres.GetAreaCodes(conn_db, transaction, finder, out ret);
                dbAdres.Close();
                if (!ret.result)
                {
                    MonitorLog.WriteLog("Ошибка получения текущего area_codes.code", MonitorLog.typelog.Error, true);
                }
                string cd = "0";
                if (code > 0) cd = code.ToString();

                sql =
                 " Insert into " + tables.kvar +
                 " (nzp_kvar,nzp_area,nzp_geu,nzp_dom,nkvar,nkvar_n,num_ls,fio,ikvar,area_code) " +
                 " Select nzp_kvar,nzp_area,nzp_geu,nzp_dom,nkvar,nkvar_n,num_ls,fio,ikvar, " + cd +
                 " From " + local_kvar + " where nzp_kvar = " + finder.nzp_kvar;

                ret = ExecSQL(conn_db, transaction, sql, true);
                if (!ret.result) return ret;
            }
            reader.Close();
            reader.Dispose();

            sql = " Update " + tables.kvar +
             " Set pref = '" + finder.pref + "'" +
                ", nzp_wp = (select max(nzp_wp) from " + s_point + " p where p.bd_kernel = '" + finder.pref + "')" +
             " Where nzp_kvar =" + finder.nzp_kvar;

            ret = ExecSQL(conn_db, transaction, sql, true);
            if (!ret.result) return ret;

#if PG
            sql = " Update " + tables.kvar +
             " Set is_open = coalesce(" + ("( Select max(val_prm) From " + prm_3 +
                             " Where " + tables.kvar + ".nzp_kvar = nzp and nzp_prm = 51 and current_date between dat_s and dat_po and is_actual = 1)").CastTo("INTEGER") + ", " + Ls.States.Undefined.GetHashCode() + ") " +
             " Where nzp_kvar =" + finder.nzp_kvar;
#else
            sql = " Update " + tables.kvar +
             " Set is_open = nvl(( Select max(val_prm) From " + prm_3 +
                             " Where " + tables.kvar + ".nzp_kvar = nzp and nzp_prm = 51 and today between dat_s and dat_po and is_actual = 1), " + Ls.States.Undefined.GetHashCode() + ") " +
             " Where nzp_kvar =" + finder.nzp_kvar;
#endif
            ret = ExecSQL(conn_db, transaction, sql, true);
            if (!ret.result) return ret;

#if PG
            sql = " Update " + tables.kvar;
            sql = sql.UpdateSet(
                                "nzp_area, nzp_geu, nzp_dom, num_ls, nkvar, nkvar_n, porch, phone, uch, ikvar, fio, pkod, pkod10, typek, remark",
                "nzp_area, nzp_geu, nzp_dom, num_ls, nkvar, nkvar_n, porch, phone, uch, ikvar, fio, pkod, pkod10, typek, remark", "sub");
            sql += " from ( select nzp_area, nzp_geu, nzp_dom, num_ls, nkvar, nkvar_n, porch, phone, uch, ikvar, fio, pkod, pkod10, typek, remark From " + local_kvar + " k Where k.nzp_kvar = " + finder.nzp_kvar + ") as sub" + " Where nzp_kvar = " + finder.nzp_kvar;
#else
            sql = " Update " + tables.kvar +
             " Set (nzp_area, nzp_geu, nzp_dom, num_ls, nkvar, nkvar_n, porch, phone, uch, ikvar, fio, pkod, pkod10, typek, remark) =" +
             " ((Select nzp_area, nzp_geu, nzp_dom, num_ls, nkvar, nkvar_n, porch, phone, uch, ikvar, fio, pkod, pkod10, typek, remark From " + local_kvar + " k Where " + tables.kvar + ".nzp_kvar = k.nzp_kvar ))" +
             " Where nzp_kvar = " + finder.nzp_kvar;
#endif
            ret = ExecSQL(conn_db, transaction, sql, true);
            return ret;
        }

        public Returns RefreshDom(IDbConnection conn_db, Dom finder)
        {
            if (finder.pref == "") return new Returns(false, "Не задан префикс БД");

            DbTables tables = new DbTables(conn_db);
#if PG
            string local_dom = finder.pref + "_data.dom";
            string s_point = Points.Pref + "_kernel.s_point";
#else
            string local_dom = finder.pref + "_data@" + DBManager.getServer(conn_db) + ":dom";
            string s_point = Points.Pref + "_kernel@" + DBManager.getServer(conn_db) + ":s_point";
#endif


            string sql = "select nzp_dom from " + tables.dom + " where nzp_dom = " + finder.nzp_dom;
            IDataReader reader;
            Returns ret = ExecRead(conn_db, out reader, sql, true);
            if (!ret.result) return ret;

            if (!reader.Read())
            {
                sql =
                 " Insert into " + tables.dom +
                 " (nzp_dom,nzp_area,nzp_geu,nzp_ul,idom,ndom,nkor,nzp_land,nzp_stat,nzp_town,nzp_raj) " +
                 " Select nzp_dom,nzp_area,nzp_geu,nzp_ul,idom,ndom,nkor,nzp_land,nzp_stat,nzp_town,nzp_raj " +
                 " From " + local_dom + " where nzp_dom = " + finder.nzp_dom;

                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result) return ret;
            }
            reader.Close();
            reader.Dispose();

            sql = " Update " + tables.dom +
             " Set pref = '" + finder.pref + "'" +
                ", nzp_wp = (select max(nzp_wp) from " + s_point + " p where p.bd_kernel = '" + finder.pref + "')" +
             " Where nzp_dom =" + finder.nzp_dom;

            ret = ExecSQL(conn_db, sql, true);
            if (!ret.result) return ret;

            sql = "update " + tables.kvar+
                    " set nzp_area = " + finder.nzp_area +
                    ", nzp_geu = " + finder.nzp_geu +
                    " where nzp_dom = " + finder.nzp_dom;
            ret = ExecSQL(conn_db, sql, true);
            if (!ret.result) return ret;

#if PG                                      
            sql = "WITH tt AS " +
                  "(SELECT d.nzp_area, " +
                          "d.nzp_geu, " +
                          "d.nzp_ul, " +
                          "d.idom, " +
                          "d.ndom, " +
                          "d.nkor, " +
                          "d.nzp_land, " +
                          "d.nzp_stat, " +
                          "d.nzp_town, " +
                          "d.nzp_raj " +
                   "FROM " + local_dom + " d WHERE d.nzp_dom = " + finder.nzp_dom + ") " +
                   "UPDATE " + tables.dom + " " +
                   "SET nzp_area = tt.nzp_area, " +
                   "nzp_geu = tt.nzp_geu, " +
                   "nzp_ul = tt.nzp_ul, " +
                   "idom = tt.idom, " +
                   "ndom = tt.ndom, " +
                   "nkor = tt.nkor, " +
                   "nzp_land = tt.nzp_land, " +
                   "nzp_stat = tt.nzp_stat, " +
                   "nzp_town = tt.nzp_town, " +
                   "nzp_raj = tt.nzp_raj " +
                   "FROM tt " +
                   "WHERE nzp_dom = " + finder.nzp_dom;
#else
            sql = " Update " + tables.dom +
                " Set (nzp_area,nzp_geu,nzp_ul,idom,ndom,nkor,nzp_land,nzp_stat,nzp_town,nzp_raj) =" +
                " ((Select nzp_area,nzp_geu,nzp_ul,idom,ndom,nkor,nzp_land,nzp_stat,nzp_town,nzp_raj From " + local_dom + " d Where " + tables.dom + ".nzp_dom = d.nzp_dom ))" +
                " Where nzp_dom = " + finder.nzp_dom;
#endif

            ret = ExecSQL(conn_db, sql, true);
            return ret;
        }


        //        /// <summary>
        //        /// Получение списка лицевых счетов
        //        /// </summary>
        //        /// <param name="finder">Объект поиска лицевых счетов</param>
        //        /// <param name="ret">[out] Объект результата</param>
        //        /// <returns>Список лицевых счетов</returns>
        //        public List<Ls> GetLs(Ls finder, out Returns ret)
        //        {
        //            ret = Utils.InitReturns();
        //            if (finder.nzp_user < 1)
        //            {
        //                ret.result = false;
        //                ret.text = "Не определен пользователь";
        //                return null;
        //            }

        //            List<Ls> Spis = new List<Ls>();

        //            Spis.Clear();

        //            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
        //            ret = OpenDb(conn_web, true);
        //            if (!ret.result) return null;

        //            string tXX_spls = "t" + Convert.ToString(finder.nzp_user) + "_spls";
        //            if (Utils.GetParams(finder.prms, Constants.page_perechen_lsdom)) tXX_spls += "dom";

        //            if (!TableInWebCashe(conn_web, tXX_spls))
        //            {
        //                conn_web.Close();

        //                ret.tag = -1;
        //                ret.result = false;
        //                ret.text = "Данные не были выбраны";

        //                return null;
        //            }
        //            else
        //            {
        //                if (!isTableHasColumn(conn_web, tXX_spls, "mark"))
        //                {
        //                    ret = ExecSQL(conn_web, "alter table " + tXX_spls + " add mark integer", true);
        //                    if (!ret.result)
        //                    {
        //                        ret.text = "Не удалось добавить к таблице \"" + tXX_spls + "\" поле \"" + "mark" + " integer" + "\"";
        //                        return null;
        //                    }
        //                }
        //                if (!isTableHasColumn(conn_web, tXX_spls, "has_pu"))
        //                {
        //                    ret = ExecSQL(conn_web, "alter table " + tXX_spls + " add has_pu integer", true);
        //                    if (!ret.result)
        //                    {
        //                        ret.text = "Не удалось добавить к таблице \"" + tXX_spls + "\" поле \"" + "has_pu" + " integer" + "\"";
        //                        return null;
        //                    }
        //                }
        //            }

        //            string wheremark = " where 1=1 ";
        //            if (finder.mark == 1) wheremark += " and mark = 1";
        //            else if (finder.mark == 2) wheremark += " and mark = 0";


        //            IDbCommand cmd = DBManager.newDbCommand(" Select count(*) From " + tXX_spls + wheremark, conn_web);
        //            try
        //            {
        //                string s = Convert.ToString(cmd.ExecuteScalar());
        //                ret.tag = Convert.ToInt32(s);
        //            }
        //            catch (Exception ex)
        //            {
        //                conn_web.Close();

        //                ret.result = false;
        //                ret.text = ex.Message;

        //                string err;
        //                if (Constants.Viewerror)
        //                    err = " \n " + ex.Message;
        //                else
        //                    err = "";

        //                MonitorLog.WriteLog("Ошибка заполнения списка лс " + err, MonitorLog.typelog.Error, 20, 201, true);
        //                return null;
        //            }

        //            string skip = "";
        //            if (finder.skip > 0) skip = " skip " + finder.skip;

        //            string orderby = " Order by adr";
        //            switch (finder.sortby)
        //            {
        //                case Constants.sortby_adr: orderby = " Order by ulica,idom,ndom,ikvar "; break;
        //                case Constants.sortby_ls:
        //                    if (Points.IsSmr) orderby = " Order by pkod10 ";
        //                    else orderby = " Order by num_ls "; break;
        //            }


        //            /*
        //             * Получаем список лицевых счетов
        //             */
        //            IDataReader reader;

        //#if PG
        //            var sqlPgFormat = " Select a.*, round(pkod)||'' as spkod From {1} a {2} {3} offset {0}";
        //            var sql = string.Format(sqlPgFormat, finder.skip, tXX_spls, wheremark, orderby);
        //#else
        //            var sqlIfmxFormat = " Select skip {0} a.*, round(pkod)||'' as spkod From {1} a {2} {3}";
        //            var sql = string.Format(sqlIfmxFormat, finder.skip, tXX_spls, wheremark, orderby);
        //#endif

        //            if (!ExecRead(conn_web, out reader, sql, true).result)
        //            {
        //                conn_web.Close();
        //                return null;
        //            }
        //            try
        //            {
        //                int i = 0;
        //                while (reader.Read())
        //                {
        //                    i = i + 1;
        //                    Ls zap = new Ls();

        //                    zap.num = (i + finder.skip).ToString();

        //                    if (reader["num_ls"] == DBNull.Value)
        //                        zap.num_ls = 0;
        //                    else
        //                        zap.num_ls = (int)reader["num_ls"];

        //                    zap.pkod10 = reader["pkod10"] == DBNull.Value ? 0 : (int)reader["pkod10"];

        //                    /*
        //                    if (reader["pkod"] == DBNull.Value)
        //                        zap.pkod = "0";
        //                    else
        //                        zap.pkod = Convert.ToString((decimal)reader["pkod"]);
        //                    */
        //                    if (reader["spkod"] == DBNull.Value)
        //                        zap.pkod = "0";
        //                    else
        //                        zap.pkod = Convert.ToString(reader["spkod"]).Trim();

        //                    if (reader["stypek"] == DBNull.Value)
        //                        zap.stypek = "";
        //                    else
        //                        zap.stypek = (string)reader["stypek"];

        //                    if (reader["sostls"] == DBNull.Value)
        //                        zap.state = "";
        //                    else
        //                        zap.state = ((string)reader["sostls"]).Trim();

        //                    if (reader["fio"] == DBNull.Value)
        //                        zap.fio = "";
        //                    else
        //                        zap.fio = (string)reader["fio"];
        //                    //  zap.fio = Constants._UNDEF_;

        //                    if (reader["adr"] == DBNull.Value)
        //                        zap.adr = "";
        //                    else
        //                        zap.adr = (string)reader["adr"];
        //                    if (reader["nzp_kvar"] == DBNull.Value)
        //                        zap.nzp_kvar = 0;
        //                    else
        //                        zap.nzp_kvar = Convert.ToInt32((int)reader["nzp_kvar"]);
        //                    if (reader["nzp_dom"] == DBNull.Value)
        //                        zap.nzp_dom = 0;
        //                    else
        //                        zap.nzp_dom = Convert.ToInt32((int)reader["nzp_dom"]);
        //                    if (reader["pref"] == DBNull.Value)
        //                        zap.pref = "";
        //                    else
        //                        zap.pref = (string)reader["pref"];

        //                    if (reader["num_ls_litera"] == DBNull.Value)
        //                        zap.num_ls_litera = "";
        //                    else
        //                        zap.num_ls_litera = (string)reader["num_ls_litera"];

        //                    if (reader["mark"] == DBNull.Value)
        //                        zap.mark = 1;
        //                    else
        //                        zap.mark = (int)reader["mark"];

        //                    if (reader["has_pu"] != DBNull.Value) if ((int)reader["has_pu"] == 1) zap.has_pu = "Да";


        //                    Spis.Add(zap);

        //                    if (i >= finder.rows) break;
        //                }

        //                reader.Close();
        //                conn_web.Close();
        //                return Spis;
        //            }
        //            catch (Exception ex)
        //            {
        //                reader.Close();
        //                conn_web.Close();

        //                ret.result = false;
        //                ret.text = ex.Message;

        //                string err;
        //                if (Constants.Viewerror)
        //                    err = " \n " + ex.Message;
        //                else
        //                    err = "";

        //                MonitorLog.WriteLog("Ошибка заполнения списка лс " + err, MonitorLog.typelog.Error, 20, 201, true);

        //                return null;
        //            }
        //        }

        public List<SplitLsParams> ExecuteSplitLS(List<SplitLsParams> listPrm, List<Perekidka> listPerekidka, List<Kart> listGilec, out Returns ret)
        {
            int indEtalon = 0;
            SplitLsParams etalonLs = GetEtalonLs(listPrm, out indEtalon);
            if (etalonLs == null)
            {
                ret = new Returns(false, "Не выбран лицевой счет для разделения", -1);
                return null;
            }

            IDbTransaction transaction = null;
            IDbConnection conn_db = GetConnection(Points.GetConnByPref(Points.Pref));
            ret = OpenDb(conn_db, true);
            if (!ret.result) return null;

            int count_changed_ls = 0;

            Ls ls = GetLsData(conn_db, null, etalonLs, out ret);
            if (!ret.result)
            {
                conn_db.Close();
                return null;
            }

            //текущий расчетный месяц
            var rec = Points.GetCalcMonth(new CalcMonthParams { pref = etalonLs.pref });
            var CalcDate = new DateTime(rec.year_, rec.month_, 1);

            //месяц открытия
            DateTime dt = DateTime.MinValue;
            if (listPrm[0].dat_open != "") DateTime.TryParse(listPrm[0].dat_open, out dt);
            dt = new DateTime(dt.Year, dt.Month, 1);

            #region определение локального пользователя
            int LocalnzpUser = 0;
            LocalnzpUser = etalonLs.nzp_user;
            
            /*using (var db = new DbWorkUser())
            {
                LocalnzpUser = db.GetLocalUser(conn_db, etalonLs, out ret);
            }
            if (!ret.result)
            {
                conn_db.Close();
                return null;
            }*/
            #endregion

            var new_kvars = new List<Ls>();
            var prm = new Param();
            
            foreach (var splitLsParams in listPrm)//цикл по всем лс 
            {  
                if (splitLsParams.nzp_kvar > 0) continue; //исходный лс

                transaction = conn_db.BeginTransaction();

                Ls new_kvar = CopyLsData(ls, splitLsParams, etalonLs);
                if (dt == CalcDate) new_kvar.stateID = (int) Ls.States.Open;

                //создаем новые ЛС, добавляем в prm_3 параметр nzp_prm=51 
                new_kvar.nzp_kvar = Update(conn_db, transaction, conn_db, new_kvar, out ret);
                if (!ret.result)
                {
                    transaction.Rollback();
                    conn_db.Close();
                    return null;
                }

                splitLsParams.nzp_kvar = new_kvar.nzp_kvar;

                if (new_kvar.stateID == (int) Ls.States.Open)
                {
                    SetSostLS(conn_db, transaction, splitLsParams.nzp_kvar, out ret);
                    if (!ret.result)
                    {
                        transaction.Rollback();
                        conn_db.Close();
                        return null;
                    }
                }
                else //лс в будещем
                {
                    prm = new Param();
                    ls.CopyTo(prm);
                    prm.dat_s = new DateTime(dt.Year, dt.Month, 1).ToShortDateString();
                    prm.nzp = splitLsParams.nzp_kvar;
                    prm.nzp_prm = 51;
                    prm.val_prm = ((int) Ls.States.Open).ToString();
                    prm.prm_num = 3;

                    using (var dbPrm = new DbSavePrm(conn_db))
                    {
                        ret = dbPrm.Save(prm);
                    }
                    if (!ret.result)
                    {
                        transaction.Rollback();
                        conn_db.Close();
                        return null;
                    }
                }

                var finderFrom = new Ls();
                var finderTo = new Ls();
                finderFrom.pref = new_kvar.pref;
                finderTo.pref = new_kvar.pref;
                finderFrom.nzp_kvar = etalonLs.nzp_kvar;
                finderTo.nzp_kvar = splitLsParams.nzp_kvar;
                finderFrom.nzp_user = splitLsParams.nzp_user;
                finderFrom.stateValidOn = dt.ToShortDateString();

                //перенос параметров prm_1, prm_3(кроме nzp_prm=51), prm_18 
                using (var dbPrm = new DbParameters())
                {
                    ret = dbPrm.CopyLsParams(conn_db, transaction, finderFrom, finderTo);
                }
                if (!ret.result)
                {
                    transaction.Rollback();
                    conn_db.Close();
                    return null;
                }

                #region смена значений параметров
                prm = new Param();
                ls.CopyTo(prm);
                prm.dat_s = new DateTime(dt.Year, dt.Month, 1).ToShortDateString();
                prm.nzp = splitLsParams.nzp_kvar;
                prm.nzp_prm = (int)ParamIds.LsParams.SquareTotal;
                prm.val_prm = splitLsParams.tot_squ.ToString();
                prm.prm_num = 1;
                using (var dbPrm = new DbSavePrm(conn_db))
                {
                    ret = dbPrm.Save(prm);
                }
                if (!ret.result)
                {
                    conn_db.Close();
                    return null;
                }

                ls.CopyTo(prm);
                prm.dat_s = new DateTime(dt.Year, dt.Month, 1).ToShortDateString();
                prm.nzp = splitLsParams.nzp_kvar;
                prm.nzp_prm = (int)ParamIds.LsParams.SquareLiving;
                prm.val_prm = splitLsParams.liv_squ.ToString();
                prm.prm_num = 1;
                using (var dbPrm = new DbSavePrm(conn_db))
                {
                    ret = dbPrm.Save(prm);
                }
                if (!ret.result)
                {
                    conn_db.Close();
                    return null;
                }

                prm = new Param();
                ls.CopyTo(prm);
                prm.dat_s = new DateTime(dt.Year, dt.Month, 1).ToShortDateString();
                prm.nzp = splitLsParams.nzp_kvar;
                prm.nzp_prm = (int)ParamIds.LsParams.OtopSqu;
                prm.val_prm = splitLsParams.otopl_squ.ToString();
                prm.prm_num = 1;
                using (var dbPrm = new DbSavePrm(conn_db))
                {
                    ret = dbPrm.Save(prm);
                }
                if (!ret.result)
                {
                    conn_db.Close();
                    return null;
                }

                prm = new Param();
                ls.CopyTo(prm);
                prm.dat_s = new DateTime(dt.Year, dt.Month, 1).ToShortDateString();
                prm.nzp = splitLsParams.nzp_kvar;
                prm.nzp_prm = (int)ParamIds.LsParams.GilNumberLive;
                prm.val_prm = splitLsParams.kol_gil.ToString();
                prm.prm_num = 1;
                using (var dbPrm = new DbSavePrm(conn_db))
                {
                    ret = dbPrm.Save(prm);
                }
                if (!ret.result)
                {
                    conn_db.Close();
                    return null;
                }
                #endregion

                new_kvars.Add(new_kvar);
                transaction.Commit();
               
           
                count_changed_ls++;
            }
            ls.nzp_kvar = etalonLs.nzp_kvar;
            ls.num_ls = etalonLs.nzp_kvar;

            ret = CreateGroupCounterForSplitLs(conn_db, transaction, dt, LocalnzpUser, ls, listPrm);
            if (!ret.result)
            {
                conn_db.Close();
                return null;
            }

            #region смена значений параметров
            prm = new Param();
            ls.CopyTo(prm);
            prm.dat_s = new DateTime(dt.Year, dt.Month, 1).ToShortDateString();
            prm.nzp = etalonLs.nzp_kvar;
            prm.nzp_prm = (int)ParamIds.LsParams.SquareTotal;
            prm.val_prm = etalonLs.tot_squ.ToString();
            prm.prm_num = 1;
            using (var dbPrm = new DbSavePrm(conn_db))
            {
                ret = dbPrm.Save(prm);
            }
            if (!ret.result)
            {
                conn_db.Close();
                return null;
            }

            prm = new Param();
            ls.CopyTo(prm);
            prm.dat_s = new DateTime(dt.Year, dt.Month, 1).ToShortDateString();
            prm.nzp = etalonLs.nzp_kvar;
            prm.nzp_prm = (int)ParamIds.LsParams.SquareLiving;
            prm.val_prm = etalonLs.liv_squ.ToString();
            prm.prm_num = 1;
            using (var dbPrm = new DbSavePrm(conn_db))
            {
                ret = dbPrm.Save(prm);
            }
            if (!ret.result)
            {
                conn_db.Close();
                return null;
            }

            prm = new Param();
            ls.CopyTo(prm);
            prm.dat_s = new DateTime(dt.Year, dt.Month, 1).ToShortDateString();
            prm.nzp = etalonLs.nzp_kvar;
            prm.nzp_prm = (int)ParamIds.LsParams.OtopSqu;
            prm.val_prm = etalonLs.otopl_squ.ToString();
            prm.prm_num = 1;
            using (var dbPrm = new DbSavePrm(conn_db))
            {
                ret = dbPrm.Save(prm);
            }
            if (!ret.result)
            {
                conn_db.Close();
                return null;
            }

            prm = new Param();
            ls.CopyTo(prm);
            prm.dat_s = new DateTime(dt.Year, dt.Month, 1).ToShortDateString();
            prm.nzp = etalonLs.nzp_kvar;
            prm.nzp_prm = (int)ParamIds.LsParams.GilNumberLive;
            prm.val_prm = etalonLs.kol_gil.ToString();
            prm.prm_num = 1;
            using (var dbPrm = new DbSavePrm(conn_db))
            {
                ret = dbPrm.Save(prm);
            }
            if (!ret.result)
            {
                conn_db.Close();
                return null;
            }
            #endregion

            #region смена фамилии у эталонного лс

            UpdateFioLS(conn_db, null, etalonLs.nzp_kvar, Points.Pref, etalonLs.fio, out ret);
            if (!ret.result)
            {
                conn_db.Close();
                return null;
            }

            UpdateFioLS(conn_db, null, etalonLs.nzp_kvar, etalonLs.pref, etalonLs.fio, out ret);
            if (!ret.result)
            {
                conn_db.Close();
                return null;
            }

            #endregion

            #region Разделение сальдо
            Returns retSplSaldo = Utils.InitReturns();
            if (listPrm[indEtalon].is_split_saldo)
            {

                if (listPerekidka == null || listPerekidka.Count == 0)
                {
                    retSplSaldo = SplitSaldo(new_kvars, conn_db, ls, listPrm, indEtalon);
                }
                else
                {
                    retSplSaldo = SplitSaldoManual(conn_db, listPerekidka, new_kvars, indEtalon, ls, listPrm);
                }
            }
            #endregion

            #region Разделение жильцов
            Returns retSplGil = SplitGils(conn_db, listGilec, new_kvars, ls);
            #endregion

            if (ret.result)
            {
                if (!retSplSaldo.result)
                {
                    ret.text += " Ошибка разделения сальдо";
                }

                if (!retSplGil.result)
                {
                    ret.text += " Ошибка разделения жильцов";
                }
            }

            //добавить период перерасчета, если открытие не текущий месяц
            conn_db.Close();
            return listPrm;
        }

        public List<SplitLsParams> ExecuteSplitLS(List<SplitLsParams> listPrm,  out Returns ret)
        {
            return ExecuteSplitLS(listPrm, null, null, out ret);
        }
        
        private void SetSostLS(IDbConnection conn_db, IDbTransaction transaction, int nzp_kvar, out Returns ret)
        {
            var sql = new StringBuilder();
            sql.AppendFormat("UPDATE {0}{1}kvar SET is_open='1' where nzp_kvar ={2}", Points.Pref, sDataAliasRest, nzp_kvar);
            ret = ExecSQL(conn_db, transaction, sql.ToString(), true);
        }

        private void UpdateFioLS(IDbConnection conn_db, IDbTransaction transaction, int nzp_kvar, string pref, string fio, out Returns ret)
        {
            var sql = new StringBuilder();
            sql.AppendFormat("UPDATE {0}{1}kvar SET fio='{3}' where nzp_kvar ={2}", pref, sDataAliasRest, nzp_kvar, fio);
            ret = ExecSQL(conn_db, transaction, sql.ToString(), true);
        }

        private Ls GetLsData(IDbConnection conn_db, IDbTransaction transaction, SplitLsParams etalonLs, out Returns ret)
        {
            MyDataReader reader;
            var sql = new StringBuilder();
            var tables = new DbTables(conn_db);

            sql.AppendFormat("select * from {0} where nzp_kvar = {1}", tables.kvar, etalonLs.nzp_kvar);
            ret = ExecRead(conn_db, out reader, sql.ToString(), true);
            if (!ret.result) return null;

            var ls = new Ls();
            if (reader.Read())
            {
                if (reader["nzp_area"] != DBNull.Value) ls.nzp_area = Convert.ToInt32(reader["nzp_area"]);
                if (reader["nzp_geu"] != DBNull.Value) ls.nzp_geu = Convert.ToInt32(reader["nzp_geu"]);
                if (reader["nzp_dom"] != DBNull.Value) ls.nzp_dom = Convert.ToInt32(reader["nzp_dom"]);
                if (reader["nkvar"] != DBNull.Value) ls.nkvar = Convert.ToString(reader["nkvar"]).Trim();
                if (reader["porch"] != DBNull.Value) ls.porch = Convert.ToString(reader["porch"]).Trim();
                if (reader["phone"] != DBNull.Value) ls.phone = Convert.ToString(reader["phone"]).Trim();
                if (reader["ikvar"] != DBNull.Value) ls.ikvar = Convert.ToInt32(reader["ikvar"]);
                if (reader["uch"] != DBNull.Value) ls.uch = Convert.ToString(reader["uch"]).Trim();
                if (reader["remark"] != DBNull.Value) ls.remark = Convert.ToString(reader["remark"]).Trim();
                if (reader["pref"] != DBNull.Value) ls.pref = Convert.ToString(reader["pref"]).Trim();
                if (reader["typek"] != DBNull.Value) ls.typek = Convert.ToInt32(reader["typek"]);
                if (reader["nzp_wp"] != DBNull.Value) ls.nzp_wp = Convert.ToInt32(reader["nzp_wp"]);
                ls.nzp_user = etalonLs.nzp_user;
            }
            reader.Close();

            return ls;
        }

        private Ls CopyLsData(Ls ls, SplitLsParams splitLsParams, SplitLsParams etalonLs)
        {
            return new Ls
            {
                moving = true,
                num = splitLsParams.num.ToString(),
                nzp_area = ls.nzp_area,
                nzp_geu = ls.nzp_geu,
                nzp_dom = ls.nzp_dom,
                nkvar = ls.nkvar,
                nkvar_n = splitLsParams.nkvar_n.ToString(),
                porch = ls.porch,
                phone = ls.phone,
                fio = splitLsParams.fio,
                ikvar = ls.ikvar,
                uch = ls.uch,
                remark = ls.remark,
                pref = etalonLs.pref,
                typek = ls.typek,
                nzp_wp = ls.nzp_wp,
                chekexistls = 0,
                nzp_kvar = Constants._ZERO_,
                nzp_user = splitLsParams.nzp_user,
                webLogin = splitLsParams.webLogin,
                webUname = splitLsParams.webUname
            };
        }

        /// <summary>
        /// получить эталонный лицевой счет
        /// </summary>
        /// <returns></returns>
        private SplitLsParams GetEtalonLs(List<SplitLsParams> listPrm, out int ind)
        {
            SplitLsParams etalonLs = null;
            ind = 0;
            foreach (SplitLsParams splitLsParams in listPrm)
            {
                if (splitLsParams.nzp_kvar > 0)
                {
                    etalonLs = new SplitLsParams();
                    etalonLs.nzp_kvar = splitLsParams.nzp_kvar;
                    etalonLs.num_ls = splitLsParams.num_ls;
                    etalonLs.nzp_user = splitLsParams.nzp_user;
                    etalonLs.pref = splitLsParams.pref;
                    etalonLs.fio = splitLsParams.fio;
                    etalonLs.tot_squ = splitLsParams.tot_squ;
                    etalonLs.liv_squ = splitLsParams.liv_squ;
                    etalonLs.otopl_squ = splitLsParams.otopl_squ;
                    etalonLs.kol_gil = splitLsParams.kol_gil;
                    etalonLs.is_split_saldo = splitLsParams.is_split_saldo;
                    break;
                }
                ind++;
            }

            return etalonLs;
        }

        private List<Decimal> GetEtalonForDistribSaldo(List<SplitLsParams> listPrm)
        {
            List<Decimal> etalonSums = new List<decimal>();

            foreach (SplitLsParams splitLsParams in listPrm)
                etalonSums.Add(splitLsParams.tot_squ);

            return etalonSums;
        }

        private Returns CreateGroupCounterForSplitLs(IDbConnection conn_db, IDbTransaction transaction, DateTime openMonth, int LocalnzpUser, Ls old_kvar, List<SplitLsParams> listPrm)
        {
            Returns ret = Utils.InitReturns();
            var rec = Points.GetCalcMonth(new CalcMonthParams { pref = old_kvar.pref });
            //текущий расчетный месяц этого банка
            var CalcDate = new DateTime(rec.year_, rec.month_, 1);

            // Создание группового ПУ
            StringBuilder sql = new StringBuilder();
            sql.AppendFormat("select * from {0}_data{1}counters_spis where nzp={2} and nzp_type = 3 and is_actual = 1 and (dat_close is null or dat_close > '{3}')", old_kvar.pref,
                tableDelimiter, old_kvar.nzp_kvar, openMonth.ToShortDateString());
            MyDataReader reader;

            ret = ExecRead(conn_db, transaction, out reader, sql.ToString(), false);
            if (!ret.result) return ret;
            int oldNzpCounter = 0;
            while (reader.Read())
            {
                Counter newCounter = new Counter();
                newCounter.nzp_user = old_kvar.nzp_user;
                newCounter.webLogin = old_kvar.webLogin;
                newCounter.webUname = old_kvar.webUname;
                newCounter.pref = old_kvar.pref;
                newCounter.nzp_dom = old_kvar.nzp_dom;
                newCounter.nzp = old_kvar.nzp_dom;
                newCounter.nzp_type = (int)CounterKinds.Communal;
                if (reader["nzp_cnttype"] != DBNull.Value) newCounter.nzp_cnttype = Convert.ToInt32(reader["nzp_cnttype"]);
                if (reader["nzp_serv"] != DBNull.Value) newCounter.nzp_serv = Convert.ToInt32(reader["nzp_serv"]);
                if (reader["num_cnt"] != DBNull.Value) newCounter.num_cnt = Convert.ToString(reader["num_cnt"]);
                if (reader["is_gkal"] != DBNull.Value) newCounter.is_gkal = Convert.ToInt32(reader["is_gkal"]);
                if (reader["dat_prov"] != DBNull.Value) newCounter.dat_prov = Convert.ToString(reader["dat_prov"]);
                if (reader["dat_provnext"] != DBNull.Value) newCounter.dat_provnext = Convert.ToString(reader["dat_provnext"]);
                if (reader["dat_oblom"] != DBNull.Value) newCounter.dat_oblom = Convert.ToString(reader["dat_oblom"]);
                if (reader["dat_poch"] != DBNull.Value) newCounter.dat_poch = Convert.ToString(reader["dat_poch"]);
                if (reader["comment"] != DBNull.Value) newCounter.comment = Convert.ToString(reader["comment"]);
                if (reader["is_actual"] != DBNull.Value) newCounter.is_actual = Convert.ToInt32(reader["is_actual"]);
                if (reader["nzp_cnt"] != DBNull.Value) newCounter.nzp_cnt = Convert.ToInt32(reader["nzp_cnt"]);
                if (reader["is_pl"] != DBNull.Value) newCounter.is_pl = Convert.ToInt32(reader["is_pl"]);
                if (reader["cnt_ls"] != DBNull.Value) newCounter.cnt_ls = Convert.ToInt32(reader["cnt_ls"]);
                if (reader["nzp_counter"] != DBNull.Value) oldNzpCounter = Convert.ToInt32(reader["nzp_counter"]);
                using (var dbCounter = new DbCounter())
                {
                    ret = dbCounter.SaveCounter(conn_db, transaction, newCounter, CalcDate.ToShortDateString());
                }
                if (!ret.result) return ret;
                newCounter.nzp_counter = ret.tag;

                //перенос параметров из prm_17
                sql.Remove(0, sql.Length);
                sql.AppendFormat("Insert into {0}_data.prm_17 (nzp,nzp_prm,dat_s,dat_po,val_prm,is_actual,cur_unl,nzp_wp,nzp_user, " +
                           " dat_when,dat_del,user_del,dat_block,user_block,month_calc)", old_kvar.pref.Trim());
                sql.AppendFormat(" Select {0},nzp_prm, '{1}',dat_po,val_prm,is_actual,cur_unl,nzp_wp,{2},now(),dat_del,user_del,dat_block,user_block,month_calc",
                    oldNzpCounter, openMonth.ToShortDateString(), LocalnzpUser);
                sql.AppendFormat(" From {0}_data.prm_17", old_kvar.pref);
                sql.AppendFormat(" Where is_actual <> 100 and '{0}' between dat_s and dat_po and nzp = {1}", openMonth.ToShortDateString(), newCounter.nzp_counter);
                ret = ExecSQL(conn_db, transaction, sql.ToString(), true);
                if (!ret.result) return ret;

                //перенос показаний ПУ
                sql.Remove(0, sql.Length);
                sql.Append(" insert into  " + old_kvar.pref + "_data" + tableDelimiter + "counters_group  ");
                sql.Append("( dat_uchet, val_cnt, is_actual, nzp_user, dat_when, cur_unl,  " +
                           " nzp_wp, dat_del, user_del, nzp_counter)");
                sql.Append(" select  dat_uchet, val_cnt, is_actual, ");
                sql.AppendFormat(" {0}, now(), cur_unl, nzp_wp,   dat_del, user_del, {1}", LocalnzpUser, newCounter.nzp_counter);
                sql.AppendFormat(" from {0}_data{1}counters where nzp_counter={2}", old_kvar.pref, tableDelimiter, oldNzpCounter);
                sql.AppendFormat(" and dat_uchet >= '{0}'", openMonth.ToShortDateString());
                ret = ExecSQL(conn_db, transaction, sql.ToString(), true);
                if (!ret.result) return ret;

                var list_nzp_kvar = new List<int>();
                foreach (var splitLsParams in listPrm) list_nzp_kvar.Add(splitLsParams.nzp_kvar);
                using (var dbCounter = new DbCounter())
                {
                    ret = dbCounter.AddLsForGroupCnt(newCounter, list_nzp_kvar, CalcDate.ToShortDateString());
                }
                if (!ret.result) return ret;

                // Закрытие ПУ
                sql.Remove(0, sql.Length);
                sql.AppendFormat(" update {0}_data{1}counters_spis set (dat_close)=(mdy({2},{3},{4})) ", old_kvar.pref, tableDelimiter, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.Year);
                sql.AppendFormat(" where nzp_counter={0} and dat_close is null and is_actual<>100; ", oldNzpCounter);
                ret = ExecSQL(conn_db, transaction, sql.ToString(), true);
                if (!ret.result) return ret;
            }

            return ret;
        }

        private Returns SplitSaldo(List<Ls> new_kvars, IDbConnection conn_db, Ls old_kvar, List<SplitLsParams> listPrm, int indEtalon)
        {
            Returns ret = Utils.InitReturns();

            List<Decimal> listSums = GetEtalonForDistribSaldo(listPrm);
            if (new_kvars == null || new_kvars.Count <= 0) return new Returns(false, "Нет новых ЛС", -1);

            var rec = Points.GetCalcMonth(new CalcMonthParams { pref = new_kvars[0].pref });
            //текущий расчетный месяц этого банка
            var calcDate = new DateTime(rec.year_, rec.month_, 1);
            MyDataReader reader = null;
            var sql = new StringBuilder();

            // Разделение сальдо
            sql.Remove(0, sql.Length);
            sql.AppendFormat("select * from {0}_charge_{1}{2}charge_{3} ", old_kvar.pref, (calcDate.Year%100).ToString("00"), tableDelimiter, calcDate.Month.ToString("00"));
            sql.AppendFormat(" where nzp_kvar = {0} and dat_charge is null and nzp_serv > 1", listPrm[indEtalon].nzp_kvar);
            ret = ExecRead(conn_db, out reader,sql.ToString(), true);
            if (!ret.result) return ret;

            while (reader.Read())
            {
                Charge charge = new Charge();
                if (reader["sum_insaldo"] != DBNull.Value) charge.sum_outsaldo = Convert.ToDecimal(reader["sum_insaldo"]);
                if (reader["nzp_serv"] != DBNull.Value) charge.nzp_serv = Convert.ToInt32(reader["nzp_serv"]);
                if (reader["nzp_supp"] != DBNull.Value) charge.nzp_supp = Convert.ToInt32(reader["nzp_supp"]);
                List<decimal> list = MathUtility.DistributeSum(charge.sum_outsaldo, listSums);
                int i = 0;
                foreach (var new_kvar in new_kvars)
                {
                    decimal sum_rcl = 0;
                    if (i == indEtalon) sum_rcl = list[i] - charge.sum_outsaldo;
                    else sum_rcl = list[i];
                    var doc = new TDocumentBase()
                    {
                        nzp_type_doc = 6,
                        dat_doc = DateTime.Now.ToShortDateString(),
                        comment = "Разделение сальдо при разделении ЛС номер " + new_kvars[indEtalon]
                    };

                    var typercl = new TypeRcl()
                    {
                        type_rcl = 20
                    };

                    Perekidka p = new Perekidka()
                    {
                        nzp_user = new_kvar.nzp_user,
                        webLogin = new_kvar.webLogin,
                        webUname = new_kvar.webUname,
                        nzp_kvar = new_kvar.nzp_kvar,
                        pref = new_kvar.pref,
                        nzp_serv = charge.nzp_serv,
                        nzp_supp = (int)charge.nzp_supp,
                        sum_rcl = sum_rcl,
                        month_ = calcDate.Month,
                        year_ = calcDate.Year,
                        doc_base = doc,
                        typercl = typercl
                    };
                    i++;

                    using (var db = new DbCharge())
                    {
                        ret = db.SavePerekidka(p);
                    }
                    if (!ret.result) return ret;
                }
            }
            return ret;
        }

        private Returns SplitSaldoManual(IDbConnection conn_db, List<Perekidka> listPerekidka, List<Ls> new_kvars, int indEtalon, Ls old_kvar, List<SplitLsParams> listPrm)
        {
            Returns ret = Utils.InitReturns();
             if (new_kvars == null || new_kvars.Count <= 0) return new Returns(false, "Нет новых ЛС", -1);
             if (listPerekidka == null || listPerekidka.Count == 0) return ret;
            var rec = Points.GetCalcMonth(new CalcMonthParams { pref = listPerekidka[0].pref });
            //текущий расчетный месяц этого банка
            var calcDate = new DateTime(rec.year_, rec.month_, 1);
            MyDataReader reader = null;
            var sql = new StringBuilder();

              sql.Remove(0, sql.Length);
            sql.AppendFormat("select * from {0}_charge_{1}{2}charge_{3} ", old_kvar.pref, (calcDate.Year%100).ToString("00"), tableDelimiter, calcDate.Month.ToString("00"));
            sql.AppendFormat(" where nzp_kvar = {0} and dat_charge is null and nzp_serv > 1", listPrm[indEtalon].nzp_kvar);
            ret = ExecRead(conn_db, out reader,sql.ToString(), true);
            if (!ret.result) return ret;
            List<Charge> listCh = new List<Charge>();
            while (reader.Read())
            {
                Charge charge = new Charge();
                if (reader["sum_insaldo"] != DBNull.Value)
                    charge.sum_outsaldo = Convert.ToDecimal(reader["sum_insaldo"]);
                if (reader["nzp_serv"] != DBNull.Value) charge.nzp_serv = Convert.ToInt32(reader["nzp_serv"]);
                if (reader["nzp_supp"] != DBNull.Value) charge.nzp_supp = Convert.ToInt32(reader["nzp_supp"]);
                listCh.Add(charge);
            }

            // Разделение сальдо
            foreach (var perekidka in listPerekidka)
            {
                decimal sum_rcl = 0;

                foreach (var ch in listCh)
                {
                    if (ch.nzp_serv == perekidka.nzp_serv && ch.nzp_supp == perekidka.nzp_supp)
                    {
                        sum_rcl = perekidka.sums[0] - ch.sum_outsaldo;
                        break;
                    }
                }

                var doc = new TDocumentBase()
                {
                    nzp_type_doc = 6,
                    dat_doc = DateTime.Now.ToShortDateString(),
                    comment = "Разделение сальдо при разделении ЛС номер " + old_kvar.num_ls
                };

                var typercl = new TypeRcl()
                {
                    type_rcl = 20
                };

                Perekidka p = new Perekidka()
                {
                    nzp_user = old_kvar.nzp_user,
                    webLogin = old_kvar.webLogin,
                    webUname = old_kvar.webUname,
                    nzp_kvar = old_kvar.nzp_kvar,
                    pref = old_kvar.pref,
                    nzp_serv = perekidka.nzp_serv,
                    nzp_supp = perekidka.nzp_supp,
                    sum_rcl = sum_rcl,
                    month_ = calcDate.Month,
                    year_ = calcDate.Year,
                    doc_base = doc,
                    typercl = typercl
                };

                using (var db = new DbCharge())
                {
                    ret = db.SavePerekidka(p);
                }
                if (!ret.result) return ret;

                int i = 0;
                foreach (var new_kvar in new_kvars)
                {
                    sum_rcl = perekidka.sums[Convert.ToInt32(new_kvar.num)];

                    doc = new TDocumentBase()
                    {
                        nzp_type_doc = 6,
                        dat_doc = DateTime.Now.ToShortDateString(),
                        comment = "Разделение сальдо при разделении ЛС номер "  +old_kvar.num_ls
                    };

                    typercl = new TypeRcl()
                    {
                        type_rcl = 20
                    };

                    p = new Perekidka()
                    {
                        nzp_user = new_kvar.nzp_user,
                        webLogin = new_kvar.webLogin,
                        webUname = new_kvar.webUname,
                        nzp_kvar = new_kvar.nzp_kvar,
                        pref = new_kvar.pref,
                        nzp_serv = perekidka.nzp_serv,
                        nzp_supp = perekidka.nzp_supp,
                        sum_rcl = sum_rcl,
                        month_ = calcDate.Month,
                        year_ = calcDate.Year,
                        doc_base = doc,
                        typercl = typercl
                    };
                    i++;
                    if (p.sum_rcl != 0)
                    {
                        using (var db = new DbCharge())
                        {
                            ret = db.SavePerekidka(p);
                        }
                        if (!ret.result) return ret;
                    }
                }
            }
            return ret;
        }

        private Returns SplitGils(IDbConnection conn_db, List<Kart> listKart, List<Ls> new_kvars, Ls old_kvar)
        {
            Returns ret = Utils.InitReturns();
            if (new_kvars == null || new_kvars.Count <= 0) return new Returns(false, "Нет новых ЛС", -1);
            if (listKart == null) return ret;
            if (listKart.Count == 0) return ret;
            string pref = listKart[0].pref;
            var rec = Points.GetCalcMonth(new CalcMonthParams { pref = pref });
            //текущий расчетный месяц этого банка
            var calcDate = new DateTime(rec.year_, rec.month_, 1);
            MyDataReader reader = null;
            var sql = new StringBuilder();

            foreach (Kart k in listKart)
            {
                if (k.nzp_kvar == old_kvar.nzp_kvar) continue;
                foreach (Ls new_kvar in new_kvars)
                {
                    if (k.nzp_kvar == Convert.ToInt32(new_kvar.num))
                    {
                        IDbTransaction trans = conn_db.BeginTransaction();

                        sql = new StringBuilder("insert into " );
                        sql.AppendFormat("{0}_data{1}gilec (nzp_gil) values(default) returning nzp_gil", pref, tableDelimiter);
                        var value = ExecScalar(conn_db, trans, sql.ToString(), out ret, true);
                        if (!ret.result)
                        {
                            trans.Rollback();
                            return ret;
                        }
                        int nzp_gil = value != null ? Convert.ToInt32(value) : 0;
                        if (nzp_gil == 0) return new Returns(false, "Ошибка добавления жильца", -1);
                        
                        sql = new StringBuilder("update ");
                        sql.AppendFormat("{0}_data{1}kart set isactual='' where nzp_kart = {2}", pref, tableDelimiter,
                            k.nzp_kart);
                        ret = ExecSQL(conn_db, trans, sql.ToString(), true);
                        if (!ret.result)
                        {
                            trans.Rollback();
                            return ret;
                        }

                        //убытие
                        sql = new StringBuilder("insert into ");
                        sql.AppendFormat("{0}_data{1}kart (", pref, tableDelimiter);
                        sql.Append("nzp_gil, fam, ima, otch, dat_rog, nzp_tkrt, dat_sost, gender, nzp_rod, nzp_sud, neuch, ndees, ");
                        sql.Append("nzp_dok, serij, nomer, vid_dat, vid_mes, kod_podrazd, " );
                        sql.Append("nzp_kvar, tprp, namereg, kod_namereg_prn, dat_ofor, dat_oprp, dat_prop, ");
                        sql.Append("nzp_celp, nzp_lnop, nzp_stop, nzp_tnop, nzp_rnop, rem_op, " );
                        sql.Append("nzp_celu, nzp_lnku, nzp_stku, nzp_tnku, nzp_rnku, rem_ku, ");
                        sql.Append("nzp_lnmr, nzp_stmr, nzp_tnmr, nzp_rnmr, rem_mr, ");
                        sql.Append("jobname, jobpost,");
                        sql.Append("who_pvu, dat_pvu, dat_svu," );
                        if (Points.Region != Regions.Region.Tatarstan)
                        {
                            sql.Append(" strana_mr, region_mr, okrug_mr, gorod_mr, npunkt_mr,");
                            sql.Append(" strana_op, region_op, okrug_op, gorod_op, npunkt_op," );
                            sql.Append(" strana_ku, region_ku, okrug_ku, gorod_ku, npunkt_ku,");
                            sql.Append(" dat_smert, dat_fio_c, rodstvo,");
                        }
                        sql.Append("dat_izm, isactual, nzp_user) ");
                        sql.AppendFormat("select nzp_gil, fam, ima, otch, dat_rog, {0}, now(), gender, nzp_rod, nzp_sud, neuch, ndees,", Constants.typKrtUbit);
                        sql.Append("nzp_dok, serij, nomer, vid_dat, vid_mes, kod_podrazd, ");
                        sql.AppendFormat("{0}, tprp, namereg, kod_namereg_prn, '{1}', dat_oprp, dat_prop, ", old_kvar.nzp_kvar, calcDate.AddDays(-1).ToShortDateString());
                        sql.Append("nzp_celp, nzp_lnop, nzp_stop, nzp_tnop, nzp_rnop, rem_op, ");
                        sql.Append("nzp_celu, nzp_lnku, nzp_stku, nzp_tnku, nzp_rnku, rem_ku, ");
                        sql.Append("nzp_lnmr, nzp_stmr, nzp_tnmr, nzp_rnmr, rem_mr, ");
                        sql.Append("jobname, jobpost,");
                        sql.Append("who_pvu, dat_pvu, dat_svu,");
                        if (Points.Region != Regions.Region.Tatarstan)
                        {
                            sql.Append(" strana_mr, region_mr, okrug_mr, gorod_mr, npunkt_mr,");
                            sql.Append(" strana_op, region_op, okrug_op, gorod_op, npunkt_op,");
                            sql.Append(" strana_ku, region_ku, okrug_ku, gorod_ku, npunkt_ku,");
                            sql.Append(" dat_smert, dat_fio_c, rodstvo,");
                        }
                        sql.AppendFormat("dat_izm, 1, {0} ", k.nzp_user);
                        sql.AppendFormat(" from {0}_data{1}kart ", pref, tableDelimiter);
                        sql.AppendFormat("where nzp_kart = {0} ", k.nzp_kart);
                        ret = ExecSQL(conn_db, trans, sql.ToString(), true);
                        if (!ret.result)
                        {
                            trans.Rollback();
                            return ret;
                        }

                        //прибытие
                        sql = new StringBuilder("insert into ");
                        sql.AppendFormat("{0}_data{1}kart (", pref, tableDelimiter);
                        sql.Append("nzp_gil, fam, ima, otch, dat_rog, nzp_tkrt, dat_sost, gender, nzp_rod, nzp_sud, neuch, ndees, ");
                        sql.Append("nzp_dok, serij, nomer, vid_dat, vid_mes, kod_podrazd, ");
                        sql.Append("nzp_kvar, tprp, namereg, kod_namereg_prn, dat_ofor, dat_oprp, dat_prop, ");
                        sql.Append("nzp_celp, nzp_lnop, nzp_stop, nzp_tnop, nzp_rnop, rem_op, ");
                        sql.Append("nzp_celu, nzp_lnku, nzp_stku, nzp_tnku, nzp_rnku, rem_ku, ");
                        sql.Append("nzp_lnmr, nzp_stmr, nzp_tnmr, nzp_rnmr, rem_mr, ");
                        sql.Append("jobname, jobpost,");
                        sql.Append("fam_c, ima_c, otch_c, dat_rog_c, ");
                        sql.Append("who_pvu, dat_pvu, dat_svu,");
                        if (Points.Region != Regions.Region.Tatarstan)
                        {
                            sql.Append(" strana_mr, region_mr, okrug_mr, gorod_mr, npunkt_mr,");
                            sql.Append(" strana_op, region_op, okrug_op, gorod_op, npunkt_op,");
                            sql.Append(" strana_ku, region_ku, okrug_ku, gorod_ku, npunkt_ku,");
                            sql.Append(" dat_smert, dat_fio_c, rodstvo,");
                        }
                        sql.Append("dat_izm, isactual, nzp_user) ");
                        sql.AppendFormat("select {0}, fam, ima, otch, dat_rog, {1}, now(), gender, nzp_rod, nzp_sud, neuch, ndees,", nzp_gil, Constants.typKrtPrib);
                        sql.Append("nzp_dok, serij, nomer, vid_dat, vid_mes, kod_podrazd, ");
                        sql.AppendFormat("{0}, tprp, namereg, kod_namereg_prn, '{1}', dat_oprp, dat_prop, ", new_kvar.nzp_kvar, calcDate.ToShortDateString());
                        sql.Append("nzp_celp, nzp_lnop, nzp_stop, nzp_tnop, nzp_rnop, rem_op, ");
                        sql.Append("nzp_celu, nzp_lnku, nzp_stku, nzp_tnku, nzp_rnku, rem_ku, ");
                        sql.Append("nzp_lnmr, nzp_stmr, nzp_tnmr, nzp_rnmr, rem_mr, ");
                        sql.Append("jobname, jobpost,");
                        sql.Append("fam_c, ima_c, otch_c, dat_rog_c, ");
                        sql.Append("who_pvu, dat_pvu, dat_svu,");
                        if (Points.Region != Regions.Region.Tatarstan)
                        {
                            sql.Append(" strana_mr, region_mr, okrug_mr, gorod_mr, npunkt_mr,");
                            sql.Append(" strana_op, region_op, okrug_op, gorod_op, npunkt_op,");
                            sql.Append(" strana_ku, region_ku, okrug_ku, gorod_ku, npunkt_ku,");
                            sql.Append(" dat_smert, dat_fio_c, rodstvo,");
                        }
                        sql.AppendFormat("dat_izm, 1, {0} ", k.nzp_user);
                        sql.AppendFormat(" from {0}_data{1}kart ", pref, tableDelimiter);
                        sql.AppendFormat("where nzp_kart = {0} ", k.nzp_kart);
                        ret = ExecSQL(conn_db, trans, sql.ToString(), true);
                        if (!ret.result)
                        {
                            trans.Rollback();
                            return ret;
                        }

                        trans.Commit();

                        break;
                    }
                }
            }

            return ret;
        }
    }
}