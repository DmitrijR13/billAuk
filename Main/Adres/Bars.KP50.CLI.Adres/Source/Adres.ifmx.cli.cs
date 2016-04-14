using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;

namespace STCLINE.KP50.DataBase
{
    //----------------------------------------------------------------------
    public partial class DbAdresClient : DataBaseHead
    //----------------------------------------------------------------------
    {
        /// <summary>
        /// Проверка существования таблицы
        /// </summary>
        /// <param name="tab">Таблица</param>
        /// <returns>true/false</returns>
        public bool CasheExists(string tab)
        {
            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            if (!OpenDb(conn_web, true).result) return false;

            bool b = TableInWebCashe(conn_web, tab);
            conn_web.Close();

            return b;
        }


        /// <summary>
        /// Создание таблицы лицевого счета
        /// </summary>
        /// <param name="conn_web">Соедиение к базе</param>
        /// <param name="tXX_spls">Наименование таблицы</param>
        /// <param name="onCreate">Создание новой таблицы либо создание инфлексов на уже существующую</param>
        public Returns CreateTableWebLs(IDbConnection conn_web, string tXX_spls, bool onCreate)
        {
            var control = GetCacheTablesControl.GetInstance<CacheTablesControlLs>();
            return control.CreateTableWeb(conn_web, tXX_spls, onCreate);
        }

        private Returns SaveFinder(Ls finder, int nzp_page)
        {
            Returns ret = SaveFinder((Dom)finder, nzp_page);
            if (!ret.result) return ret;
            //соединение с БД
            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result) return ret;

            string tXX_spfinder = "t" + Convert.ToString(finder.nzp_user) + "_spfinder";

            if (ret.result)
            {
                string sql = "";
                //if (Points.IsSmr)
                //{
                //    if (finder.pkod10 > 0)
                //    {
                //        sql = "insert into " + tXX_spfinder + " values (0,\'Лицевой счет\',\'" + finder.pkod10.ToString() + "\'," + nzp_page.ToString() + ")";
                //        ret = ExecSQL(conn_web, sql, true);
                //        if (!ret.result)
                //        {
                //            conn_web.Close();
                //            return ret;
                //        }
                //    }
                //}
                //else if (finder.num_ls > 0)
                //{
                    sql = "insert into " + tXX_spfinder + " values (0,\'Лицевой счет\',\'" + finder.num_ls.ToString() + "\'," + nzp_page.ToString() + ")";
                    ret = ExecSQL(conn_web, sql, true);
                    if (!ret.result)
                    {
                        conn_web.Close();
                        return ret;
                    }
                //}

                if (finder.pkod.Trim() != "")
                {
                    sql = "insert into " + tXX_spfinder + " values (0,\'Платежный код\',\'" + finder.pkod.ToString() + "\'," + nzp_page.ToString() + ")";
                    ret = ExecSQL(conn_web, sql, true);
                    if (!ret.result)
                    {
                        conn_web.Close();
                        return ret;
                    }
                }

                if (finder.stateID > 0)
                {
                    sql = "insert into " + tXX_spfinder + " values (0,\'Состояние\',\'" + finder.state + "\'," + nzp_page + ")";
                    ret = ExecSQL(conn_web, sql, true);
                    if (!ret.result)
                    {
                        conn_web.Close();
                        return ret;
                    }
                }

                if (finder.typek > 0)
                {
                    sql = "insert into " + tXX_spfinder + " values (0,\'Тип счета\',\'" + finder.stypek.ToString() + "\'," + nzp_page.ToString() + ")";
                    ret = ExecSQL(conn_web, sql, true);
                    if (!ret.result)
                    {
                        conn_web.Close();
                        return ret;
                    }
                }

                if (finder.porch.Trim() != "")
                {
                    sql = "insert into " + tXX_spfinder + " values (0,\'Подъезд\',\'" + finder.porch.ToString() + "\'," + nzp_page.ToString() + ")";
                    ret = ExecSQL(conn_web, sql, true);
                    if (!ret.result)
                    {
                        conn_web.Close();
                        return ret;
                    }
                }

                if (finder.nkvar.Trim() != "")
                {
                    sql = "insert into " + tXX_spfinder + " values (0,\'Квартира с\',\'" + finder.nkvar.ToString() + "\'," + nzp_page.ToString() + ")";
                    ret = ExecSQL(conn_web, sql, true);
                    if (!ret.result)
                    {
                        conn_web.Close();
                        return ret;
                    }
                }

                if (finder.nkvar_po.Trim() != "")
                {
                    sql = "insert into " + tXX_spfinder + " values (0,\'Квартира по\',\'" + finder.nkvar_po.ToString() + "\'," + nzp_page.ToString() + ")";
                    ret = ExecSQL(conn_web, sql, true);
                    if (!ret.result)
                    {
                        conn_web.Close();
                        return ret;
                    }
                }

                if (finder.nkvar_n.Trim() != "")
                {
                    sql = "insert into " + tXX_spfinder + " values (0,\'Комната\',\'" + finder.nkvar_n.ToString() + "\'," + nzp_page.ToString() + ")";
                    ret = ExecSQL(conn_web, sql, true);
                    if (!ret.result)
                    {
                        conn_web.Close();
                        return ret;
                    }
                }

                if (finder.phone.Trim() != "")
                {
                    sql = "insert into " + tXX_spfinder + " values (0,\'Телефон квартиры\',\'" + finder.phone.ToString() + "\'," + nzp_page.ToString() + ")";
                    ret = ExecSQL(conn_web, sql, true);
                    if (!ret.result)
                    {
                        conn_web.Close();
                        return ret;
                    }
                }

                if (finder.fio.Trim() != "")
                {
                    sql = "insert into " + tXX_spfinder + " values (0,\'Квартиросъемщик\',\'" + finder.fio.ToString() + "\'," + nzp_page.ToString() + ")";
                    ret = ExecSQL(conn_web, sql, true);
                    if (!ret.result)
                    {
                        conn_web.Close();
                        return ret;
                    }
                }

                if (finder.uch.Trim() != "")
                {
                    sql = "insert into " + tXX_spfinder + " values (0,\'Участок\',\'" + finder.uch.ToString() + "\'," + nzp_page.ToString() + ")";
                    ret = ExecSQL(conn_web, sql, true);
                    if (!ret.result)
                    {
                        conn_web.Close();
                        return ret;
                    }
                }
            }
            conn_web.Close();
            return ret;
        }

        /// <summary>
        /// Сохранение параметров поиска дома
        /// </summary>
        /// <param name="finder">Объект поиска дома</param>
        /// <param name="nzp_page"></param>
        private Returns SaveFinder(Dom finder, int nzp_page)
        {
            Returns ret = Utils.InitReturns();
            if (finder.nzp_user <= 0)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Пользователь не определен";
                return ret;
            }

            //соединение с БД
            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result) return ret;

            string tXX_spfinder = "t" + Convert.ToString(finder.nzp_user) + "_spfinder";

            //проверка наличия таблицы в БД
            if (!TableInWebCashe(conn_web, tXX_spfinder))
            {
                //создать таблицу webdata
                ret = ExecSQL(conn_web,
                          " Create table " + tXX_spfinder +
                          " (nzp_finder serial, " +
                          "  name char(100), " +
                          "  value char(255), " +
                          "  nzp_page integer " +
                          " ) ", true);
            }
            if (!ret.result)
            {
                conn_web.Close();
                return ret;
            }

            string sql = "";
            sql = "delete from " + tXX_spfinder + " where nzp_page = " + nzp_page.ToString();
            ret = ExecSQL(conn_web, sql, true);
            if (!ret.result)
            {
                conn_web.Close();
                return ret;
            }

            sql = "";
            if (finder.nzp_wp > 0)
            {
                sql = "insert into " + tXX_spfinder + " values (0,\'Банк данных\',\'" +
                    (finder.point.Length > 250 ? finder.point.Substring(0, 255) : finder.point) + "\'," + nzp_page.ToString() + ")";
            }
            else if (finder.dopPointList != null && finder.dopPointList.Count > 0)
            {
                sql = "insert into " + tXX_spfinder + " values (0,\'Банк данных\',\'" +
                    (finder.point.Length > 250 ? finder.point.Substring(0, 255) : finder.point) + "\'," + nzp_page.ToString() + ")";
            }
            if (sql != "")
            {
                ret = ExecSQL(conn_web, sql, true);
                if (!ret.result)
                {
                    return ret;
                }
            }

            sql = "";
            if (finder.nzp_area > 0)
                sql = "insert into " + tXX_spfinder + " values (0,\'Управляющая организация\',\'" + finder.area.ToString() + "\'," + nzp_page.ToString() + ")";

            else if (finder.list_nzp_area != null && finder.list_nzp_area.Count > 0)
                sql = "insert into " + tXX_spfinder + " values (0,\'Управляющая организация\',\'" + finder.area.ToString() + "\'," + nzp_page.ToString() + ")";


            if (sql != "")
            {
                ret = ExecSQL(conn_web, sql, true);
                if (!ret.result)
                {
                    return ret;
                }
            }

            if (finder.nzp_geu > 0)
            {
                sql = "insert into " + tXX_spfinder + " values (0,\'Отделение\',\'" + finder.geu.ToString() + "\'," + nzp_page.ToString() + ")";
                ret = ExecSQL(conn_web, sql, true);
                if (!ret.result)
                {
                    conn_web.Close();
                    return ret;
                }
            }

            if (finder.nzp_ul > 0)
            {
                sql = "insert into " + tXX_spfinder + " values (0,\'Улица\',\'" + finder.ulica.ToString() + "\'," + nzp_page.ToString() + ")";
                ret = ExecSQL(conn_web, sql, true);
                if (!ret.result)
                {
                    conn_web.Close();
                    return ret;
                }
            }

            if (finder.ndom.Trim() != "")
            {
                sql = "insert into " + tXX_spfinder + " values (0,\'Номер дома с\',\'" + finder.ndom.ToString() + "\'," + nzp_page.ToString() + ")";
                ret = ExecSQL(conn_web, sql, true);
                if (!ret.result)
                {
                    conn_web.Close();
                    return ret;
                }
            }

            if (finder.ndom_po.Trim() != "")
            {
                sql = "insert into " + tXX_spfinder + " values (0,\'Номер дома по\',\'" + finder.ndom_po.ToString() + "\'," + nzp_page.ToString() + ")";
                ret = ExecSQL(conn_web, sql, true);
                if (!ret.result)
                {
                    conn_web.Close();
                    return ret;
                }
            }

            if (finder.nkor.Trim() != "")
            {
                sql = "insert into " + tXX_spfinder + " values (0,\'Корпус\',\'" + finder.nkor.ToString() + "\'," + nzp_page.ToString() + ")";
                ret = ExecSQL(conn_web, sql, true);
                if (!ret.result)
                {
                    conn_web.Close();
                    return ret;
                }
            }
            conn_web.Close();
            return ret;
        }

        //----------------------------------------------------------------------
        private void CreateTableWebDom(IDbConnection conn_web, string tXX_spdom, bool onCreate, out Returns ret) //
        //----------------------------------------------------------------------
        {
            if (onCreate)
            {
                if (TableInWebCashe(conn_web, tXX_spdom))
                {
                    ret = ExecSQL(conn_web, " Drop table " + tXX_spdom, false);
                    if (!ret.result)
                    {
                        ExecSQL(conn_web, " Delete from " + tXX_spdom, false);
                    }
                }

                //создать таблицу webdata:tXX_spDom
                ret = ExecSQL(conn_web,
                          " Create table " + tXX_spdom +
                          " ( nzp_dom    integer, " +
                          "   nzp_ul     integer, " +
                          "   nzp_area   integer, " +
                          "   nzp_geu    integer, " +
                          "   nzp_wp     integer, " +

                          "   area     char(60)," +
                          "   geu      char(60)," +

                          "   ulica    char(80)," +
                          "   ndom     char(20)," +
                          "   idom     integer, " +
                          "   pref     char(10)," +
                          "   point    char(60)," +
                          "   mark     integer," +
                          "   has_pu   integer" +
                          " ) ", true);
                if (!ret.result)
                {
                    return;
                }

            }
            else
            {
                ret = ExecSQL(conn_web, " Create index ix1_" + tXX_spdom + " on " + tXX_spdom + " (nzp_dom) ", true);
                ret = ExecSQL(conn_web, " Create index ix2_" + tXX_spdom + " on " + tXX_spdom + " (ulica,idom) ", true);
                ret = ExecSQL(conn_web, " Create index ix3_" + tXX_spdom + " on " + tXX_spdom + " (area,ulica,idom) ", true);

                if (!ret.result)
                {
#if PG
                    ret = ExecSQL(conn_web, " analyze  " + tXX_spdom, true);
#else
                    ret = ExecSQL(conn_web, " Update statistics for table  " + tXX_spdom, true);

#endif

                }
            }
        }



        /// <summary>
        /// Получение списка лицевых счетов
        /// </summary>
        /// <param name="finder">Объект поиска лицевых счетов</param>
        /// <param name="ret">[out] Объект результата</param>
        /// <returns>Список лицевых счетов</returns>
        public List<Ls> GetLs(Ls finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            if (finder.nzp_user < 1)
            {
                ret.result = false;
                ret.text = "Не определен пользователь";
                return null;
            }

            List<Ls> Spis = new List<Ls>();

            Spis.Clear();

            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result) return null;

            string tXX_spls = "t" + Convert.ToString(finder.nzp_user) + "_spls";
            string tXX_spls_full = DBManager.GetFullBaseName(conn_web) + DBManager.tableDelimiter +
                "t" + Convert.ToString(finder.nzp_user) + "_spls";
            if (Utils.GetParams(finder.prms, Constants.page_perechen_lsdom))
            {
                tXX_spls += "dom";
                tXX_spls_full += "dom";
            }
            if (!TempTableInWebCashe(conn_web, tXX_spls_full))
            {
                conn_web.Close();

                ret.tag = -1;
                ret.result = false;
                ret.text = "Данные не были выбраны";

                return null;
            }
            //else
            //{
            //    if (!isTableHasColumn(conn_web, tXX_spls, "mark"))
            //    {
            //        ret = ExecSQL(conn_web, "alter table " + tXX_spls_full + " add mark integer", true);
            //        if (!ret.result)
            //        {
            //            ret.text = "Не удалось добавить к таблице \"" + tXX_spls_full + "\" поле \"" + "mark" + " integer" + "\"";
            //            return null;
            //        }
            //    }
            //    if (!isTableHasColumn(conn_web, tXX_spls, "has_pu"))
            //    {
            //        ret = ExecSQL(conn_web, "alter table " + tXX_spls_full + " add has_pu integer", true);
            //        if (!ret.result)
            //        {
            //            ret.text = "Не удалось добавить к таблице \"" + tXX_spls_full + "\" поле \"" + "has_pu" + " integer" + "\"";
            //            return null;
            //        }
            //    }
            //}

            string wheremark = " where 1=1 ";
            if (finder.mark == 1) wheremark += " and mark = 1";
            else if (finder.mark == 2) wheremark += " and mark = 0";


            IDbCommand cmd = DBManager.newDbCommand(" Select count(*) From " + tXX_spls_full + wheremark, conn_web);
            try
            {
                string s = Convert.ToString(cmd.ExecuteScalar());
                ret.tag = Convert.ToInt32(s);
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

                MonitorLog.WriteLog("Ошибка заполнения списка лс " + err, MonitorLog.typelog.Error, 20, 201, true);
                return null;
            }

            string skip = "";
            if (finder.skip > 0) skip = " skip " + finder.skip;

            string orderby = " Order by adr";
            switch (finder.sortby)
            {
                case Constants.sortby_adr: orderby = " Order by ulica,idom,ndom,nkor,ikvar,nkvar,ikvar_n,nkvar_n "; break;
                case Constants.sortby_ls:
                    //if (Points.IsSmr) orderby = " Order by pkod10 ";
                    //else 
                        orderby = " Order by num_ls "; break;
            }


            /*
             * Получаем список лицевых счетов
             */
            IDataReader reader;

#if PG
            var sqlPgFormat = " Select a.*, round(pkod)||'' as spkod From {1} a {2} {3} limit {4} offset {0}";
            var sql = string.Format(sqlPgFormat, finder.skip, DBManager.GetFullBaseName(conn_web, "public", tXX_spls), wheremark, orderby, finder.rows);
#else
            var sqlIfmxFormat = " Select skip {0} a.*, round(pkod)||'' as spkod From {1} a {2} {3}";
            var sql = string.Format(sqlIfmxFormat, finder.skip, tXX_spls, wheremark, orderby);
#endif

            if (!ExecRead(conn_web, out reader, sql, true).result)
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
                    Ls zap = new Ls();

                    zap.num = (i + finder.skip).ToString();

                    if (reader["num_ls"] == DBNull.Value)
                        zap.num_ls = 0;
                    else
                        zap.num_ls = (int)reader["num_ls"];

                    zap.pkod10 = reader["pkod10"] == DBNull.Value ? 0 : (int)reader["pkod10"];

                    /*
                    if (reader["pkod"] == DBNull.Value)
                        zap.pkod = "0";
                    else
                        zap.pkod = Convert.ToString((decimal)reader["pkod"]);
                    */
                    if (reader["spkod"] == DBNull.Value)
                        zap.pkod = "0";
                    else
                        zap.pkod = Convert.ToString(reader["spkod"]).Trim();

                    if (reader["stypek"] == DBNull.Value)
                        zap.stypek = "";
                    else
                        zap.stypek = (string)reader["stypek"];

                    if (reader["sostls"] == DBNull.Value)
                        zap.state = "";
                    else
                        zap.state = ((string)reader["sostls"]).Trim();

                    if (reader["fio"] == DBNull.Value)
                        zap.fio = "";
                    else
                        zap.fio = (string)reader["fio"];
                    //  zap.fio = Constants._UNDEF_;

                    if (reader["adr"] == DBNull.Value)
                        zap.adr = "";
                    else
                        zap.adr = (string)reader["adr"];
                    if (reader["nzp_kvar"] == DBNull.Value)
                        zap.nzp_kvar = 0;
                    else
                        zap.nzp_kvar = Convert.ToInt32((int)reader["nzp_kvar"]);
                    if (reader["nzp_dom"] == DBNull.Value)
                        zap.nzp_dom = 0;
                    else
                        zap.nzp_dom = Convert.ToInt32((int)reader["nzp_dom"]);
                    if (reader["pref"] == DBNull.Value)
                        zap.pref = "";
                    else
                        zap.pref = (string)reader["pref"];

                    if (reader["num_ls_litera"] == DBNull.Value)
                        zap.num_ls_litera = "";
                    else
                        zap.num_ls_litera = (string)reader["num_ls_litera"];

                    if (reader["mark"] == DBNull.Value)
                        zap.mark = 1;
                    else
                        zap.mark = (int)reader["mark"];

                    if (reader["remark"] == DBNull.Value)
                        zap.remark = "";
                    else
                    {
                        zap.remark = (string)reader["remark"];
                        if (zap.remark.Length > 20)
                        {
                            zap.remark = zap.remark.Remove(19) + "...";
                        }                        
                    }

                    if (reader["has_pu"] != DBNull.Value) if ((int)reader["has_pu"] == 1) zap.has_pu = "Да";


                    Spis.Add(zap);

                    if (i >= finder.rows) break;
                }

                reader.Close();
                conn_web.Close();
                return Spis;
            }
            catch (Exception ex)
            {
                reader.Close();
                //conn_web.Close();

                ret.result = false;
                ret.text = ex.Message;

                string err;
                if (Constants.Viewerror)
                    err = " \n " + ex.Message;
                else
                    err = "";

                MonitorLog.WriteLog("Ошибка заполнения списка лс " + err, MonitorLog.typelog.Error, 20, 201, true);

                return null;
            }
        }

        /// <summary>
        /// Получения списка домов
        /// </summary>
        /// <param name="finder">Объект поиска дома</param>
        /// <returns>Список домов</returns>
        public List<Dom> GetDom(Dom finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            if (finder.nzp_user < 1)
            {
                ret.result = false;
                ret.text = "Не определен пользователь";
                return null;
            }

            List<Dom> Spis = new List<Dom>();

            Spis.Clear();

            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result) return null;

            string tXX_spdom = "t" + Convert.ToString(finder.nzp_user) + "_spdom";

            if (!TableInWebCashe(conn_web, tXX_spdom))
            {
                conn_web.Close();
                ret.tag = -1;
                ret.result = false;
                ret.text = "Данные не были выбраны";

                return null;
            }
            else
            {
                if (!isTableHasColumn(conn_web, tXX_spdom, "mark"))
                {
                    ret = ExecSQL(conn_web, "alter table " + tXX_spdom + " add mark integer", true);
                    if (!ret.result)
                    {
                        ret.text = "Не удалось добавить к таблице \"" + tXX_spdom + "\" поле \"mark integer\"";
                        return null;
                    }
                }

                if (!isTableHasColumn(conn_web, tXX_spdom, "has_pu"))
                {
                    ret = ExecSQL(conn_web, "alter table " + tXX_spdom + " add has_pu integer", true);
                    if (!ret.result)
                    {
                        ret.text = "Не удалось добавить к таблице \"" + tXX_spdom + "\" поле \"has_pu integer\"";
                        return null;
                    }
                }
            }

            string wheremark = " where 1=1 ";
            if (finder.mark_dom == 1) wheremark += " and mark = 1";
            else if (finder.mark_dom == 2) wheremark += " and mark = 0";

            IDbCommand cmd = DBManager.newDbCommand(" Select count(*) From " + tXX_spdom + wheremark, conn_web);
            try
            {
                string s = Convert.ToString(cmd.ExecuteScalar());
                ret.tag = Convert.ToInt32(s);
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

                MonitorLog.WriteLog("Ошибка заполнения списка домов " + err, MonitorLog.typelog.Error, 20, 201, true);
                return null;
            }

            string skip = "";
            if (finder.skip > 0)
            {
#if PG
                skip = " offset " + finder.skip.ToString();
#else
                skip = " skip " + finder.skip.ToString();

#endif

            }

            string orderby = " Order by ulica, rajon, town, idom";
            switch (finder.sortby)
            {
                case Constants.sortby_adr: orderby = " Order by ulica, rajon, town, idom, ndom "; break;
                case Constants.sortby_ls: orderby = " Order by area, ulica, rajon, town, idom "; break;
                case Constants.sortby_ul: orderby = " Order by ulica, rajon, town, idom, ndom "; break;
                case Constants.sortby_uk: orderby = " Order by area, rajon, town, idom, ndom "; break;
            }

            //выбрать список
            IDataReader reader;
#if PG
            string sql = " Select t.* From " + tXX_spdom + " t " + wheremark + orderby + skip;
#else
            string sql = " Select " + skip + " t.* From " + tXX_spdom + " t " + wheremark + orderby;

#endif

            if (!ExecRead(conn_web, out reader, sql, true).result)
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

                    if (reader["area"] == DBNull.Value)
                        zap.area = "";
                    else
                        zap.area = (string)reader["area"];
                    if (reader["geu"] == DBNull.Value)
                        zap.geu = "";
                    else
                        zap.geu = (string)reader["geu"];

                    zap.ulica = reader["ulica"] != DBNull.Value ? ((string)reader["ulica"]).Trim() : "";
                    zap.ulicareg = reader["ulicareg"] != DBNull.Value ? ((string)reader["ulicareg"]).Trim() : "";
                    zap.rajon = reader["rajon"] != DBNull.Value ? ((string)reader["rajon"]).Trim() : "";
                    zap.town = reader["town"] != DBNull.Value ? ((string)reader["town"]).Trim() : "";

                    if (reader["ndom"] == DBNull.Value)
                        zap.ndom = "";
                    else
                        zap.ndom = (string)reader["ndom"];

                    if (reader["pref"] == DBNull.Value)
                        zap.pref = "";
                    else
                        zap.pref = (string)reader["pref"];

                    if (reader["point"] == DBNull.Value)
                        zap.point = "";
                    else
                        zap.point = (string)reader["point"];

                    if (reader["nzp_wp"] == DBNull.Value)
                        zap.nzp_wp = 0;
                    else
                        zap.nzp_wp = (int)reader["nzp_wp"];

                    if (reader["mark"] == DBNull.Value)
                        zap.mark_dom = 0;
                    else
                        zap.mark_dom = (int)reader["mark"];

                    // if (reader["has_pu"] != DBNull.Value) zap.has_pu = (int)reader["has_pu"];
                    if (reader["has_pu"] != DBNull.Value) if ((int)reader["has_pu"] == 1) zap.has_pu = "Да";

                    Spis.Add(zap);

                    if (i >= finder.rows) break;

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

                MonitorLog.WriteLog("Ошибка заполнения списка домов " + err, MonitorLog.typelog.Error, 20, 201, true);

                return null;
            }

        }

        /// <summary>
        /// Получение списка улиц
        /// </summary>
        /// <param name="finder">Объект поиска улиц</param>
        /// <returns>Список улиц</returns>
        public List<Ulica> GetUlica(Ulica finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            if (finder.nzp_user < 1)
            {
                ret.result = false;
                ret.text = "Не определен пользователь";
                return null;
            }

            List<Ulica> Spis = new List<Ulica>();

            Spis.Clear();

            //выбрать общее кол-во
            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result) return null;

            string tXX_spul = "t" + Convert.ToString(finder.nzp_user) + "_spul";

            if (!TableInWebCashe(conn_web, tXX_spul))
            {
                conn_web.Close();
                ret.tag = -1;
                ret.result = false;
                ret.text = "Данные не были выбраны";

                return null;
            }

            IDbCommand cmd = DBManager.newDbCommand(" Select count(*) From " + tXX_spul, conn_web);
            try
            {
                string s = Convert.ToString(cmd.ExecuteScalar());
                ret.tag = Convert.ToInt32(s);
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

                MonitorLog.WriteLog("Ошибка заполнения списка улиц " + err, MonitorLog.typelog.Error, 20, 201, true);
                return null;
            }

            string skip = "";
            if (finder.skip > 0)
            {
#if PG
                skip = " offset " + finder.skip.ToString();
#else
                skip = " skip " + finder.skip.ToString();

#endif

            }

            string orderby = " Order by ulica ";

            //выбрать список
            IDataReader reader;

#if PG
            var sql = " Select * From " + tXX_spul + orderby + skip;
#else
            var sql = " Select " + skip + " * From " + tXX_spul + orderby;

#endif
            if (!ExecRead(conn_web, out reader, sql, true).result)
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
                    Ulica zap = new Ulica();
                    zap.num = (i + finder.skip).ToString();

                    if (reader["nzp_ul"] == DBNull.Value)
                        zap.nzp_ul = 0;
                    else
                        zap.nzp_ul = (int)reader["nzp_ul"];
                    if (reader["ulica"] == DBNull.Value)
                        zap.ulica = "";
                    else
                        zap.ulica = (string)reader["ulica"];

                    Spis.Add(zap);

                    if (i >= finder.rows) break;


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

                MonitorLog.WriteLog("Ошибка заполнения списка улиц " + err, MonitorLog.typelog.Error, 20, 201, true);

                return null;
            }

        }

        /// <summary>
        /// Получение списка улиц из аналитических таблиц (ПОЧЕМУ FAIL?)
        /// </summary>
        /// <param name="finder">Объект поиска домов</param>
        /// <returns>Список улиц</returns>
        public List<Ulica> LoadUlicaFail2(Dom finder, out Returns ret)
        {
            ret = Utils.InitReturns();

            List<Ulica> Spis = new List<Ulica>();

            Spis.Clear();

            IDbConnection conn_db = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return null;

            //Взять информацию из аналитических таблиц
            string anl_dom = "anl" + Points.CalcMonth.year_ + "_dom";
            if (finder.nzp_server > 0)
                anl_dom += "_" + finder.nzp_server;

            string whereString = "";
            if (finder.ulica.Trim() != "")
#if PG
                whereString += " and upper(ulica) SIMILAR TO upper('*" + finder.ulica + "*')";
#else
                whereString += " and upper(ulica) matches upper('*" + finder.ulica + "*')";

#endif

            if (finder.nzp_ul > 0)
                whereString += " and nzp_ul =" + finder.nzp_ul;
            if (finder.nzp_area > 0)
                whereString += " and nzp_area =" + finder.nzp_area;
            if (finder.pref != "")
                whereString += " and pref = " + Utils.EStrNull(finder.pref);
            string first = "";
            if (finder.rows > 0)
#if PG
                //first = " limit " + finder.rows.ToString();
                first = " first " + finder.rows.ToString();
#else
                first = " first " + finder.rows.ToString();

#endif
            /*
             * Учет прав пользователя
             */
            if (finder.RolesVal != null)
            {
                if (finder.RolesVal.Count > 0)
                {
                    foreach (_RolesVal role in finder.RolesVal)
                    {
                        if (role.tip == Constants.role_sql & role.kod == Constants.role_sql_wp)
                            whereString += " and nzp_wp in (" + role.val + ")";

                        if (role.tip == Constants.role_sql & role.kod == Constants.role_sql_area)
                            whereString += " and nzp_area in (" + role.val + ")";
                    }
                }
            }

            //выбрать список
            IDataReader reader;
#if PG
            string sql =
                " Select distinct nzp_ul,trim(coalesce(ulica,'-')) as ulica " +
                " From " + anl_dom +
                " Where 1 = 1 " + whereString +
                " Order by 2 " + first;
#else
            string sql =
                " Select " + first + " unique nzp_ul,trim(nvl(ulica,'-')) as ulica " +
                " From " + anl_dom +
                " Where 1 = 1 " + whereString +
                " Order by 2 ";

#endif


            if (!ExecRead(conn_db, out reader, sql, true).result)
            {
                conn_db.Close();
                return null;
            }
            try
            {
                int i = 0;
                while (reader.Read())
                {
                    i = i + 1;
                    Ulica zap = new Ulica();

                    if (reader["nzp_ul"] == DBNull.Value)
                        zap.nzp_ul = 0;
                    else
                        zap.nzp_ul = (int)reader["nzp_ul"];
                    if (reader["ulica"] == DBNull.Value)
                        zap.ulica = "";
                    else
                    {
                        zap.ulica = (string)reader["ulica"];
                        zap.ulica = zap.ulica.Trim();

                        zap.ulica = zap.ulica.Replace("улица ", "");
                    }

                    Spis.Add(zap);

                    if (i >= finder.rows) break;
                }

                ret.tag = i;

                reader.Close();
            }
            catch (Exception ex)
            {
                reader.Close();
                conn_db.Close();

                ret.result = false;
                ret.text = ex.Message;

                string err;
                if (Constants.Viewerror)
                    err = " \n " + ex.Message;
                else
                    err = "";

                MonitorLog.WriteLog("Ошибка заполнения справочник улиц " + err, MonitorLog.typelog.Error, 20, 201, true);

                return null;
            }

            conn_db.Close();
            return Spis;
        }

        /// <summary>
        /// Конвертация строки адреса в объект Улицы
        /// </summary>
        /// <param name="dr">Строка адреса</param>
        /// <returns>Объект улицы</returns>
        public static Ulica ToUlicaValue(DataRow dr)
        {
            Ulica obj = new Ulica();

            obj.nzp_ul = STCLINE.KP50.Utility.DataConvert.FieldValue<Int32>(dr, "nzp_ul", 0);
            obj.ulica = STCLINE.KP50.Utility.DataConvert.FieldValue<string>(dr, "ulica", "");
            obj.ulicareg = STCLINE.KP50.Utility.DataConvert.FieldValue<string>(dr, "ulicareg", "");
            obj.rajon = STCLINE.KP50.Utility.DataConvert.FieldValue<string>(dr, "rajon", "");
            obj.town = STCLINE.KP50.Utility.DataConvert.FieldValue<string>(dr, "town", "");
            return obj;
        }

        /// <summary>
        /// Получить список ЖЭУ [ПОЧЕМУ АЛЬТЕРНАТИВНЫЙ?]
        /// </summary>
        /// <param name="finder">Базовый объект поиска</param>
        /// <returns>Список ЖЭУ</returns>
        public List<_Geu> LoadGeu2(Finder finder, out Returns ret)
        {
            ret = Utils.InitReturns();

            Geus spis = new Geus();
            spis.GeuList.Clear();

            IDbConnection conn_db = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return null;

            string where = "";

            if (finder.dopFind != null && finder.dopFind.Count > 0)
                where += " and upper(geu) like '%" + finder.dopFind[0].ToUpper().Replace("'", "''").Replace("*", "%") + "%' ";

            if (finder.RolesVal != null)
                foreach (_RolesVal role in finder.RolesVal)
                    if (role.tip == Constants.role_sql & role.kod == Constants.role_sql_geu) where += " and nzp_geu in (" + role.val + ")";


            string tab_geu = "s_geu"; //"anl" + Points.CalcMonth.year_ + "_dom";
            if (finder.nzp_server > 0)
                tab_geu += "_" + finder.nzp_server;


            //Определить общее количество записей
            string sql =
                " Select count(*) From " + tab_geu +
                " Where 1 = 1 " + where;
            object count = ExecScalar(conn_db, sql, out ret, true);
            int recordsTotalCount;
            try { recordsTotalCount = Convert.ToInt32(count); }
            catch (Exception e)
            {
                ret = new Returns(false, "Ошибка при определении количества записей: " + (Constants.Debug ? e.Message : ""));
                MonitorLog.WriteLog("Ошибка LoadGeu " + (Constants.Viewerror ? "\n" + e.Message : ""), MonitorLog.typelog.Error, 20, 201, true);
                conn_db.Close();
                return null;
            }

            //выбрать список
            IDataReader reader;
#if PG
            if (!ExecRead(conn_db, out reader,
                " Select distinct nzp_geu, geu " +
                " From " + tab_geu +
                " Where 1 = 1 " + where + " Order by geu", true).result)
#else
            if (!ExecRead(conn_db, out reader,
                " Select unique nzp_geu, geu " +
                " From " + tab_geu +
                " Where 1 = 1 " + where + " Order by geu", true).result)

#endif

            {
                conn_db.Close();
                return null;
            }
            try
            {
                int i = 0;
                while (reader.Read())
                {
                    i++;
                    if (i <= finder.skip) continue;
                    _Geu zap = new _Geu();

                    if (reader["nzp_geu"] != DBNull.Value) zap.nzp_geu = (int)reader["nzp_geu"];
                    if (reader["geu"] != DBNull.Value) zap.geu = Convert.ToString(reader["geu"]).Trim();

                    spis.GeuList.Add(zap);
                    if (finder.rows > 0 && i >= finder.skip + finder.rows) break;
                }

                ret.tag = i;

                reader.Close();
                conn_db.Close();
                return spis.GeuList;
            }
            catch (Exception ex)
            {
                reader.Close();
                conn_db.Close();
                ret = new Returns(false, ex.Message);
                MonitorLog.WriteLog("Ошибка заполнения справочника отделений " + (Constants.Viewerror ? "\n" + ex.Message : ""), MonitorLog.typelog.Error, 20, 201, true);
                return null;
            }
        }

        //альтернативный способ
        //----------------------------------------------------------------------
        public List<_Area> LoadArea2(Finder finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();

            Areas spis = new Areas();
            spis.AreaList.Clear();

            IDbConnection conn_db = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return null;

            string where = "";
            if (finder.RolesVal != null)
                foreach (_RolesVal role in finder.RolesVal)
                {
                    if (role.tip == Constants.role_sql & role.kod == Constants.role_sql_area)
                        where += " and nzp_area in (" + role.val + ")";
                }

            if (finder.dopFind != null && finder.dopFind.Count > 0)
            {
                string str = finder.dopFind[0];
                if (!str.Contains("FiltrOnDistrib"))
                {
                    where += " and upper(area) like '%" + str.ToUpper().Replace("'", "''").Replace("*", "%") + "%'";
                }
            }

            string tab_area = "s_area"; //"anl" + Points.CalcMonth.year_ + "_dom";
            if (finder.nzp_server > 0)
                tab_area += "_" + finder.nzp_server;

            //выбрать список
            IDataReader reader;
#if PG
            if (!ExecRead(conn_db, out reader,
                " Select distinct nzp_area,area From " + tab_area +
                " Where 1 = 1 " + where +
                " Order by area ", true).result)
#else
            if (!ExecRead(conn_db, out reader,
                " Select unique nzp_area,area From " + tab_area +
                " Where 1 = 1 " + where +
                " Order by area ", true).result)

#endif

            {
                conn_db.Close();
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

                    spis.AreaList.Add(zap);
                    if (finder.rows > 0 && i >= finder.skip + finder.rows) break;
                }
                reader.Close();

                object count = ExecScalar(conn_db,
                    " Select count(*) From " + tab_area +
                    " Where 1 = 1 " + where, out ret, true);
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
                conn_db.Close();

                ret.result = false;
                ret.text = ex.Message;

                string err;
                if (Constants.Viewerror)
                    err = " \n " + ex.Message;
                else
                    err = "";

                MonitorLog.WriteLog("Ошибка заполнения справочника управляющих организаций " + err, MonitorLog.typelog.Error, 20, 201, true);

                return null;
            }
        }

        //----------------------------------------------------------------------
        public void CreateWebArea(IDbConnection conn_web, string table, out Returns ret)
        //----------------------------------------------------------------------
        {
#if PG
            ret = ExecSQL(conn_web, " set search_path to 'public'", true);
            if (!ret.result) return;
#endif

            if (TableInWebCashe(conn_web, table))
            {
                ExecSQL(conn_web, " Drop table " + table, false);
            }
            //создать таблицу webdata:s_area
            ret = ExecSQL(conn_web,
                " CREATE TABLE " + table +
                "( nzp_area SERIAL NOT NULL, " +
                 " area CHAR(40), " +
                 " nzp_supp INTEGER ) ", true);

            if (!ret.result)
            {
                return;
            }

            uint tabid = TableInWebCasheID(conn_web, table);
            string ix = "ix" + tabid + "_" + table;

            ret = ExecSQL(conn_web, " CREATE UNIQUE INDEX " + ix + " ON " + table + " (nzp_area);", true);
        }
        //----------------------------------------------------------------------
        public void LoadAreaInWeb(List<_Area> ls, string table, out Returns ret)
        //----------------------------------------------------------------------
        {
            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);

            ret = OpenDb(conn_web, true);
            if (!ret.result)
            {
                ret.text = "Ошибка открытия БД";
                return;
            }


            //поготовка Insert'а
            IDbCommand cmd = DBManager.newDbCommand(
                " Insert into __" + table +
                "  ( nzp_area, area, nzp_supp ) " +
                " Values (?,?,?) "
                , conn_web);

            DBManager.addDbCommandParameter(cmd, "nzp_area", DbType.Int32);
            DBManager.addDbCommandParameter(cmd, "area", DbType.String);
            DBManager.addDbCommandParameter(cmd, "nzp_supp", DbType.Int32);

            try
            {
                foreach (_Area p in ls)
                {
                    (cmd.Parameters[0] as IDbDataParameter).Value = p.nzp_area;
                    (cmd.Parameters[1] as IDbDataParameter).Value = p.area;
                    (cmd.Parameters[2] as IDbDataParameter).Value = p.nzp_supp;

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
        public List<_Area> LoadAreaAvailableForRole(Role finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();

            if (finder.nzp_user < 1)
            {
                ret.result = false;
                ret.text = "Не определен пользователь";
                return null;
            }

            if (finder.nzp_role < 1)
            {
                ret.result = false;
                ret.text = "Не определена роль";
                return null;
            }

            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result) return null;

            string table = "s_area";
            if (finder.nzp_server > 0)
                table += "_" + finder.nzp_server;

            if (!TableInWebCashe(conn_web, table))
            {
                ret = new Returns(false, "Справочник управляющих организаций не загружен", -1);
                return null;
            }

            string sw = "";
            if (finder.RolesVal != null)
                if (finder.RolesVal.Count > 0)
                {
                    foreach (_RolesVal role in finder.RolesVal)
                    {
                        if (role.tip == Constants.role_sql & role.kod == Constants.role_sql_area)
                            sw += " and nzp_area in (" + role.val + ")";
                    }
                }

#if PG
            ret = ExecSQL(conn_web,
                  " set search_path to 'public'"
                  , true);

#else
            ret = ExecSQL(conn_web,
                  " set encryption password '" + BasePwd + "'"
                  , true);

            if (!ret.result)
            {
                conn_web.Close();
                return null;
            }
#endif

#if PG
            string where =
                            " Where a.nzp_area not in ( Select kod From roleskey r Where r.nzp_role = " + finder.nzp_role +
                            "   and r.tip = " + Constants.role_sql_area +
                            "   and r.tip::character(90)||r.kod||r.nzp_role||'-'||r.nzp_rlsv||'roles' = r.sign) " + sw;
#else
            string where =
                " Where a.nzp_area not in ( Select kod From roleskey r Where r.nzp_role = " + finder.nzp_role +
                "   and r.tip = " + Constants.role_sql_area +
                "   and r.tip||r.kod||r.nzp_role||'-'||r.nzp_rlsv||'roles' = decrypt_char(r.sign)) " + sw;
#endif



            //выбрать список
            string skip = "";
#if PG
            //if (finder.skip > 0) skip = " offset " + finder.skip.ToString();
#else
            //if (finder.skip > 0) skip = " skip " + finder.skip.ToString();
#endif
            IDataReader reader;
#if PG

            ret = ExecRead(conn_web, out reader,
                          " Select  nzp_area, area " +
                          " From " + table + " a " +
                          where +
                          " Order by area " + skip, true);
#else
            ret = ExecRead(conn_web, out reader,
                " Select " + skip + " nzp_area, area " +
                " From " + table + " a " +
                where +
                " Order by area ", true);
#endif
            if (!ret.result)
            {
                conn_web.Close();
                return null;
            }

            List<_Area> spis = new List<_Area>();
            try
            {
                int i = 0;
                while (reader.Read())
                {
                    i++;

                    _Area zap = new _Area();

                    if (reader["nzp_area"] != DBNull.Value) zap.nzp_area = (int)reader["nzp_area"];
                    if (reader["area"] != DBNull.Value) zap.area = Convert.ToString(reader["area"]).Trim();

                    spis.Add(zap);
                }

                reader.Close();
                conn_web.Close();
                ret.tag = i;
                return spis;
            }
            catch (Exception ex)
            {
                reader.Close();
                conn_web.Close();

                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;

                MonitorLog.WriteLog("Ошибка заполнения справочника управляющих организаций " + err, MonitorLog.typelog.Error, 20, 201, true);

                return null;
            }
        }

        //----------------------------------------------------------------------
        public void CreateWebGeu(IDbConnection conn_web, string table, out Returns ret)
        //----------------------------------------------------------------------
        {
#if PG
            ret = ExecSQL(conn_web, " set search_path to 'public'", true);
            if (!ret.result) return;
#endif

            if (TableInWebCashe(conn_web, table))
            {
                ExecSQL(conn_web, " Drop table " + table, false);
            }

            //создать таблицу webdata:s_area
            ret = ExecSQL(conn_web,
                "CREATE TABLE " + table +
                "(  nzp_geu SERIAL NOT NULL, " +
                  " geu CHAR(60) )", true);

            if (!ret.result)
            {
                return;
            }

            uint tabid = TableInWebCasheID(conn_web, table);
            string ix = "ix" + tabid + "_" + table;

            ret = ExecSQL(conn_web, "CREATE UNIQUE INDEX " + ix + " ON " + table + " (nzp_geu) ", true);
        }

        //----------------------------------------------------------------------
        public void LoadGeuInWeb(List<_Geu> ls, string table, out Returns ret)
        //----------------------------------------------------------------------
        {
            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);

            ret = OpenDb(conn_web, true);
            if (!ret.result)
            {
                ret.text = "Ошибка открытия БД";
                return;
            }


            //поготовка Insert'а
            IDbCommand cmd = DBManager.newDbCommand(
                " Insert into __" + table +
                "  ( nzp_geu, geu ) " +
                " Values (?,?) "
                , conn_web);

            DBManager.addDbCommandParameter(cmd, "nzp_geu", DbType.Int32);
            DBManager.addDbCommandParameter(cmd, "geu", DbType.String);

            try
            {
                foreach (_Geu p in ls)
                {
                    (cmd.Parameters[0] as IDbDataParameter).Value = p.nzp_geu;
                    (cmd.Parameters[1] as IDbDataParameter).Value = p.geu;

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
        public List<_Geu> LoadGeuAvailableForRole(Role finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();

            if (finder.nzp_user < 1)
            {
                ret.result = false;
                ret.text = "Не определен пользователь";
                return null;
            }

            if (finder.nzp_role < 1)
            {
                ret.result = false;
                ret.text = "Не определена роль";
                return null;
            }

            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result) return null;

            string table = "s_geu";
            if (finder.nzp_server > 0)
                table += "_" + finder.nzp_server;

            if (!TableInWebCashe(conn_web, table))
            {
                ret = new Returns(false, "Справочник участков не загружен", -1);
                return null;
            }

            string sw = "";
            if (finder.RolesVal != null)
                if (finder.RolesVal.Count > 0)
                {
                    foreach (_RolesVal role in finder.RolesVal)
                    {
                        if (role.tip == Constants.role_sql & role.kod == Constants.role_sql_geu)
                            sw += " and nzp_geu in (" + role.val + ")";
                    }
                }

#if PG
            ret = ExecSQL(conn_web,
                              " set search_path to 'public'"
                              , true);
#else
            ret = ExecSQL(conn_web,
                              " set encryption password '" + BasePwd + "'"
                              , true);
#endif

            if (!ret.result)
            {
                conn_web.Close();
                return null;
            }

#if PG
            string where =
                            " Where g.nzp_geu not in ( Select kod From roleskey r Where r.nzp_role = " + finder.nzp_role +
                            "   and r.tip = " + Constants.role_sql_geu +
                            "   and r.tip::character(90)||r.kod||r.nzp_role||'-'||r.nzp_rlsv||'roles' = r.sign) " + sw;
#else
            string where =
                            " Where g.nzp_geu not in ( Select kod From roleskey r Where r.nzp_role = " + finder.nzp_role +
                            "   and r.tip = " + Constants.role_sql_geu +
                            "   and r.tip||r.kod||r.nzp_role||'-'||r.nzp_rlsv||'roles' = decrypt_char(r.sign)) " + sw;
#endif


            //выбрать список
            string skip = "";
#if PG
            //if (finder.skip > 0) skip = " offset " + finder.skip.ToString();
#else
            //if (finder.skip > 0) skip = " skip " + finder.skip.ToString();
#endif
            IDataReader reader;
#if PG
            ret = ExecRead(conn_web, out reader,
                          " Select  nzp_geu, geu " +
                          " From " + table + " g " +
                          where +
                          " Order by geu " + skip, true);
#else
            ret = ExecRead(conn_web, out reader,
                          " Select " + skip + " nzp_geu, geu " +
                          " From " + table + " g " +
                          where +
                          " Order by geu ", true);
#endif
            if (!ret.result)
            {
                conn_web.Close();
                return null;
            }

            List<_Geu> spis = new List<_Geu>();
            try
            {
                int i = 0;
                while (reader.Read())
                {
                    i++;

                    _Geu zap = new _Geu();

                    if (reader["nzp_geu"] != DBNull.Value) zap.nzp_geu = (int)reader["nzp_geu"];
                    if (reader["geu"] != DBNull.Value) zap.geu = Convert.ToString(reader["geu"]).Trim();

                    spis.Add(zap);
                }

                reader.Close();
                conn_web.Close();
                ret.tag = i;
                return spis;
            }
            catch (Exception ex)
            {
                reader.Close();
                conn_web.Close();

                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;

                MonitorLog.WriteLog("Ошибка заполнения справочника ЖЭУ " + err, MonitorLog.typelog.Error, 20, 201, true);

                return null;
            }
        }

        //----------------------------------------------------------------------
        public Dom FindDomFromPm(_Placemark placemark, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            List<Dom> ListDom = new List<Dom>();

            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result) return null;

            if (!TempTableInWebCashe(conn_web, sDefaultSchema + "map_objects"))
            {
                conn_web.Close();
                ret.result = false;
                ret.tag = -1;
                ret.text = "Данные не были выбраны";
                return null;
            }

            IDataReader reader;
            //ищется ближайший дом в радиусе 100 метров от заданной точки
            double coord_x = 0.001566;
            double coord_y = 0.000898;
            string sql = "select kod, nzp_wp from " + sDefaultSchema + "map_objects a, " + sDefaultSchema
                + "map_points b where a.nzp_mo = b.nzp_mo and a.object_type = 1 and a.tip = " +
                MapObject.Tip.dom.GetHashCode().ToString() +
                " and (x between " + Convert.ToString(placemark.x) + "-" + Convert.ToString(coord_x) +
                    " and " + Convert.ToString(placemark.x) + "+" + Convert.ToString(coord_x) + ")" +
                " and (y between " + Convert.ToString(placemark.y) + "-" + Convert.ToString(coord_y) +
                    " and " + Convert.ToString(placemark.y) + "+" + Convert.ToString(coord_y) + ")" +
                " order by (x-" + Convert.ToString(placemark.x) + ")*(x-" + Convert.ToString(placemark.x) + ")+(y-" + Convert.ToString(placemark.y) + ")*(y-" + Convert.ToString(placemark.y) + ") ";
            if (!ExecRead(conn_web, out reader, sql, true).result)
            {
                conn_web.Close();
                return null;
            }
            try
            {
                if (reader.Read())
                {
                    Dom dom = new Dom();
                    if (Convert.ToInt32(reader["kod"]) > 0)
                    {
                        dom.nzp_dom = Convert.ToInt32(reader["kod"]);
                        dom.nzp_user = placemark.nzp_user;
                        if (Convert.ToInt32(reader["nzp_wp"]) > 0) dom.nzp_wp = Convert.ToInt32(reader["nzp_wp"]);
                        return dom;
                    }
                }
                reader.Close();
                return null;
            }
            catch (Exception ex)
            {
                reader.Close();
                conn_web.Close();
                ret.result = false;
                ret.text = ex.Message;
                string err;
                if (Constants.Viewerror) err = " \n " + ex.Message;
                else err = "";
                MonitorLog.WriteLog("Ошибка определения дома по Яндекс.Карте " + err, MonitorLog.typelog.Error, 20, 201, true);
                return null;
            }


        }

        /// <summary>
        /// Определить ключ для Яндекс.Карт
        /// </summary>
        /// <param name="ret"></param>
        /// <returns></returns>
        public string GetMapKey(out Returns ret)
        {
            ret = Utils.InitReturns();

            //соединение с БД
            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result) return null;

            string key = "";

            string sql = " Select max(note) from " + sDefaultSchema + "map_objects where tip = -1";
            IDbCommand cmd = DBManager.newDbCommand(sql, conn_web);
            try
            {
                key = Convert.ToString(cmd.ExecuteScalar()).Trim();
                if (key == "")
                {
                    ret.result = false;
                    ret.text = "Не задан ключ для карт";
                }
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;
                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;
                MonitorLog.WriteLog("Ошибка при определении ключа для карты: " + err, MonitorLog.typelog.Error, 20, 201, true);
                return null;
            }
            conn_web.Close();
            return key;
        }

        /// <summary>
        /// Определить точку на Яндекс.Карте по умолчанию
        /// </summary>
        /// <param name="ret"></param>
        /// <returns></returns>
        public _Placemark GetDefaultPlacemark(out Returns ret)
        {
            ret = Utils.InitReturns();

            _Placemark placemark = new _Placemark();

            //соединение с БД
            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result) return placemark;

            IDataReader reader;

            string sql = " Select b.x, b.y, a.note " +
                         " from " + sDefaultSchema + "map_objects a, " +
                         " " + sDefaultSchema + "map_points b where a.tip = -2 and a.nzp_mo = b.nzp_mo ";
            ret = ExecRead(conn_web, out reader, sql, true);
            if (!ret.result)
            {
                conn_web.Close();
                return placemark;
            }

            try
            {
                if (reader.Read())
                {
                    if (reader["x"] == DBNull.Value) placemark.x = 0; else placemark.x = Convert.ToSingle(reader["x"]);
                    if (reader["y"] == DBNull.Value) placemark.y = 0; else placemark.y = Convert.ToSingle(reader["y"]);
                    if (reader["note"] == DBNull.Value) placemark.note = ""; else placemark.note = Convert.ToString(reader["note"]);
                }
                else
                {
                    ret.result = false;
                    ret.text = "Координаты точки по умолчанию не заданы.";
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
                MonitorLog.WriteLog("Ошибка при определении точки на карте по умолчанию: " + err, MonitorLog.typelog.Error, 20, 201, true);
            }

            conn_web.Close();
            return placemark;
        }


        /// <summary>
        /// Сохранить список объектов на карте
        /// </summary>
        /// <param name="finder"></param>
        /// <param name="ret"></param>
        /// <returns>Результат операции</returns>
        public bool SaveMapObjects(List<MapObject> mapObjects, out Returns ret)
        {
            ret = Utils.InitReturns();

            //соединение с БД
            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result) return false;

            string sql;
            try
            {
                foreach (MapObject mapObject in mapObjects)
                {
                    int nzp_mo;
                    if (mapObject.nzp_mo <= 0)
                    {
#if PG
                        var sqlBuilder = new StringBuilder();
                        sqlBuilder.AppendFormat("Insert into {0}.map_objects (tip, kod, nzp_wp, object_type, note)", pgDefaultSchema);
                        sqlBuilder.AppendFormat(" values ({0}, {1}", mapObject.tip.GetHashCode(), mapObject.kod);
                        sqlBuilder.AppendFormat(",{0}", mapObject.nzp_wp > 0 ? mapObject.nzp_wp.ToString() : "null");
                        sqlBuilder.AppendFormat(",{0}", mapObject.object_type);
                        sqlBuilder.AppendFormat(",'{0}')", mapObject.note);
                        sqlBuilder.Append(" returning nzp_mo");

                        sql = sqlBuilder.ToString();
#else
                        sql = "Insert into map_objects (tip, kod, nzp_wp, object_type, note)";
                        sql += " Values (" + mapObject.tip.GetHashCode().ToString();
                        sql += ", " + mapObject.kod.ToString();
                        if (mapObject.nzp_wp > 0) sql += ", " + mapObject.nzp_wp.ToString();
                        else sql += ", null";
                        sql += ", " + mapObject.object_type.ToString();
                        sql += ", '" + mapObject.note.ToString() + "')";
#endif

#if PG
                        nzp_mo = (int)ExecScalar(conn_web, sql, out ret, true);
#else
                        if (!ExecSQL(conn_web, sql, true).result)
                            throw new Exception("Не удалось выполнить сохранение объекта карты");
                        IDbCommand cmd = DBManager.newDbCommand("select DBINFO('sqlca.sqlerrd1') from systables where tabid=1", conn_web);
                        nzp_mo = Convert.ToInt32(cmd.ExecuteScalar());
#endif
                    }
                    else
                    {
#if PG
                        var sqlBuilder = new StringBuilder();
                        sqlBuilder.AppendFormat("Update " + sDefaultSchema + "map_objects set tip = {0}", mapObject.tip.GetHashCode());
                        sqlBuilder.AppendFormat(", kod = {0}", mapObject.kod);
                        sqlBuilder.AppendFormat(", nzp_wp = {0}", mapObject.nzp_wp > 0 ? mapObject.nzp_wp.ToString() : "null");
                        sqlBuilder.AppendFormat(", object_type = {0}", mapObject.object_type);
                        sqlBuilder.AppendFormat(", note = {0}", mapObject.note);
                        sqlBuilder.AppendFormat("Where nzp_mo = {0}", mapObject.note);

                        sql = sqlBuilder.ToString();
                        ExecSQL(conn_web, sql, true);

                        nzp_mo = mapObject.nzp_mo;
#else
                        sql = "Update map_objects set tip = " + mapObject.tip.GetHashCode().ToString();
                        sql += ", kod = " + mapObject.kod.ToString();
                        if (mapObject.nzp_wp > 0) sql += ", nzp_wp = " + mapObject.nzp_wp.ToString();
                        else sql += ", nzp_wp = null";
                        sql += ", object_type = " + mapObject.object_type.ToString();
                        sql += ", note = '" + mapObject.note.ToString() + "'";
                        sql += " Where nzp_mo = " + mapObject.nzp_mo.ToString();

                        if (!ExecSQL(conn_web, sql, true).result)
                            throw new Exception("Не удалось выполнить сохранение объекта карты");
                        nzp_mo = mapObject.nzp_mo;
#endif

                    }

                    sql = "delete from " + sDefaultSchema + "map_points where nzp_mo = " + mapObject.nzp_mo.ToString();
                    if (!ExecSQL(conn_web, sql, true).result)
                        throw new Exception("Не удалось выполнить удаление объекта карты");

                    for (int i = 0; i < mapObject.points.Count; i++)
                    {
                        sql = "Insert into map_points (nzp_mo, x, y, ordering) Values (" +
                            nzp_mo.ToString() + ", " +
                            mapObject.points[i].x.ToString() + ", " +
                            mapObject.points[i].y.ToString() + ", " +
                            (i + 1).ToString() + ")";

                        if (!ExecSQL(conn_web, sql, true).result)
                            throw new Exception("Не удалось выполнить сохранение объекта карты");
                    }
                }
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;
                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;
                MonitorLog.WriteLog("Ошибка при сохранении списка объектов карты: " + err, MonitorLog.typelog.Error, 20, 201, true);
            }

            conn_web.Close();
            return ret.result;
        }

        /// <summary>
        /// Удалить список объектов на карте
        /// </summary>
        /// <param name="finder">Объект с параметрами поиска</param>
        /// <param name="ret"></param>
        /// <returns>Результат операции</returns>
        public bool DeleteMapObjects(MapObject finder, out Returns ret)
        {
            ret = Utils.InitReturns();

            //соединение с БД
            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result) return false;

            string where = "Where 1=1";
            if (finder.nzp_mo > 0) where += " and nzp_mo = " + finder.nzp_mo.ToString();
            if (finder.tip != MapObject.Tip.none) where += " and tip = " + finder.tip.GetHashCode().ToString();
            if (finder.kod > 0) where += " and kod = " + finder.kod.ToString();
            if (finder.object_type > 0) where += " and object_type = " + finder.object_type.ToString();
            if (finder.nzp_wp > 0) where += " and nzp_wp = " + finder.nzp_wp.ToString();
            for (int i = 0; i < finder.listNzpMo.Count; i++)
            {
                if (i == 0) where += " and nzp_mo in (" + finder.listNzpMo[i].ToString();
                else where += "," + finder.listNzpMo[i].ToString();
                if (i == finder.listNzpMo.Count - 1) where += ")";
            }

            try
            {
                if (!ExecSQL(conn_web, "Delete from " + sDefaultSchema + "map_points where nzp_mo in (select nzp_mo from " + sDefaultSchema + "map_objects " + where + " )", true).result)
                    throw new Exception("Не удалось удалить координаты объектов карты");
                if (!ExecSQL(conn_web, "Delete from " + sDefaultSchema + "map_objects " + where, true).result)
                    throw new Exception("Не удалось удалить объекты карты");
            }
            catch { }



            return ret.result;
        }

        /// <summary>
        /// Получить список банков для выбранных домов, которые имеют только открытые ЛС
        /// </summary>
        /// <param name="finder"></param>
        /// <param name="ret"></param>
        /// <returns></returns>
        public List<Finder> GetPointsDom(Finder finder, out Returns ret)
        {
            ret = Utils.InitReturns();

            if (finder.nzp_user < 0)
            {
                ret.result = false;
                ret.text = "Пользователь не определен";
                return null;
            }

            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result) return null;

#if PG
            ExecSQL(conn_web, "set search_path to 'public'", false);
#endif

            string tXX = "t" + Convert.ToString(finder.nzp_user) + "_spdom";
            if (!TempTableInWebCashe(conn_web, tXX))
            {
                conn_web.Close();
                ret.tag = -1;
                ret.result = false;
                ret.text = "Данные не были выбраны";
                return null;
            }
            if (!isTableHasColumn(conn_web, tXX, "mark"))
            {
                ret = ExecSQL(conn_web, "alter table " + tXX + " add mark integer", true);
                if (!ret.result)
                {
                    ret.text = "Не удалось добавить к таблице \"" + tXX + "\" поле \"" + "mark" + " integer" + "\"";
                    return null;
                }
            }

            string sql = "select d.pref, count(d.nzp_dom) as num from " + sDefaultSchema + tXX + " d " +
                         "where d.mark=1 and exists " +
                         "(select 1 from " + Points.Pref + sDataAliasRest + "kvar k " +
                         "where k.nzp_wp=d.nzp_wp and k.nzp_dom=d.nzp_dom and k.is_open='1')  group by 1 order by pref";
            IDataReader reader;

            if (!ExecRead(conn_web, out reader, sql, true).result)
            {
                conn_web.Close();
                return null;
            }

            List<Finder> listPoint = new List<Finder>();
            try
            {
                ret.tag = 0;
                while (reader.Read())
                {
                    Finder pnt = new Finder();
                    if (reader["pref"] != DBNull.Value) pnt.pref = Convert.ToString(reader["pref"]);
                    if (reader["num"] != DBNull.Value) pnt.rows = Convert.ToInt32(reader["num"]);

                    ret.tag += pnt.rows;

                    foreach (_Point zap in Points.PointList)
                    {
                        if (zap.pref != pnt.pref) continue;

                        pnt.nzp_wp = zap.nzp_wp;
                        pnt.point = zap.point;
                        listPoint.Add(pnt);
                        break;
                    }
                }
                reader.Close();
                conn_web.Close();

                return listPoint;
            }
            catch (Exception ex)
            {
                reader.Close();
                conn_web.Close();

                ret.result = false;
                ret.text = ex.Message;

                if (Constants.Viewerror) ret.text = " \n " + ex.Message;
                else ret.text = "";

                MonitorLog.WriteLog("Ошибка заполнения списка домов " + ex.Message, MonitorLog.typelog.Error, 20, 201, true);

                return null;
            }
        }

        /// <summary> получить список префиксов из выбранного списка л/с
        /// </summary>
        public List<Finder> GetPointsLs(Finder finder, out Returns ret)
        {
            ret = Utils.InitReturns();

            if (finder.nzp_user < 0)
            {
                ret.result = false;
                ret.text = "Пользователь не определен";
                return null;
            }

            if (finder.dopFind == null || finder.dopFind.Count == 0 ||
                (finder.dopFind[0] != Constants.page_spisls.ToString() && finder.dopFind[0] != Constants.page_spisdom.ToString()))
            {
                ret.result = false;
                ret.text = "Неверные входные параметры";
                return null;
            }

            if (finder.dopFind[0] == Constants.page_spisls.ToString() && finder.listNumber < 0)
            {
                ret.result = false;
                ret.text = "Список лицевых счетов не сформирован";
                ret.tag = -1;
                return null;
            }

            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result) return null;

#if PG
            ExecSQL(conn_web, "set search_path to 'public'", false);
#endif

            string tXX = "";
            if (finder.dopFind[0] == Constants.page_spisls.ToString())
            {
                tXX = "t" + Convert.ToString(finder.nzp_user) + "_selectedls" + finder.listNumber;
            }
            else if (finder.dopFind[0] == Constants.page_spisdom.ToString())
                tXX = "t" + Convert.ToString(finder.nzp_user) + "_spdom";

            if (!TempTableInWebCashe(conn_web, tXX))
            {
                conn_web.Close();
                ret.tag = -1;
                ret.result = false;
                ret.text = "Данные не были выбраны";

                return null;
            }
            else
            {
                if (!isTableHasColumn(conn_web, tXX, "mark"))
                {
                    ret = ExecSQL(conn_web, "alter table " + tXX + " add mark integer", true);
                    if (!ret.result)
                    {
                        ret.text = "Не удалось добавить к таблице \"" + tXX + "\" поле \"" + "mark" + " integer" + "\"";
                        return null;
                    }
                }
            }

            string sql = "select pref, count(*) as num from " + tXX + " where mark = 1 Group by pref";
            IDataReader reader;

            if (!ExecRead(conn_web, out reader, sql, true).result)
            {
                conn_web.Close();
                return null;
            }

            List<Finder> listPoint = new List<Finder>();
            try
            {
                ret.tag = 0;
                while (reader.Read())
                {
                    Finder pnt = new Finder();
                    if (reader["pref"] != DBNull.Value) pnt.pref = Convert.ToString(reader["pref"]);
                    if (reader["num"] != DBNull.Value) pnt.rows = Convert.ToInt32(reader["num"]);

                    ret.tag += pnt.rows;

                    foreach (_Point zap in Points.PointList)
                    {
                        if (zap.pref != pnt.pref) continue;

                        pnt.nzp_wp = zap.nzp_wp;
                        pnt.point = zap.point;
                        listPoint.Add(pnt);
                        break;
                    }
                }
                reader.Close();
                conn_web.Close();

                return listPoint;
            }
            catch (Exception ex)
            {
                reader.Close();
                conn_web.Close();

                ret.result = false;
                ret.text = ex.Message;

                if (Constants.Viewerror) ret.text = " \n " + ex.Message;
                else ret.text = "";

                MonitorLog.WriteLog("Ошибка заполнения списка домов " + ex.Message, MonitorLog.typelog.Error, 20, 201, true);

                return null;
            }
        }

        /// <summary>
        /// получить список параметров, которые были выбраны при поиске
        /// </summary>
        /// <param name="finder"></param>
        /// <param name="ret"></param>
        /// <returns></returns>
        public List<Search_Info> GetSearchInfo(Ls finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<Search_Info> res = null;

            if (finder.nzp_user < 1)
            {
                ret.result = false;
                ret.text = "Не определен пользователь";
                return null;
            }

            #region Подключение к БД

            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            IDataReader reader = null;
            StringBuilder sql = new StringBuilder();

            ret = OpenDb(conn_web, true);
            if (!ret.result)
            {
                MonitorLog.WriteLog("Ошибка при открытии соединения с БД ", MonitorLog.typelog.Error, true);
                return null;
            }

            #endregion

            try
            {
                #region Выборка

                sql.Remove(0, sql.Length);
                sql.Append(" select name, value ");
                sql.Append(" from  t" + finder.nzp_user + "_spfinder where nzp_page = " + finder.prms + " order by nzp_finder");

                if (!ExecRead(conn_web, out reader, sql.ToString(), true).result)
                {
                    MonitorLog.WriteLog("Ошибка выборки в GetSearchInfo " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                    sql.Remove(0, sql.Length);
                    ret.result = false;
                    return null;
                }
                #endregion

                #region Запись выгрузки

                if (reader != null)
                {
                    res = new List<Search_Info>();

                    while (reader.Read())
                    {
                        Search_Info item = new Search_Info();
                        if (reader["name"] != DBNull.Value) item.name = Convert.ToString(reader["name"]).Trim();
                        if (reader["value"] != DBNull.Value) item.value = Convert.ToString(reader["value"]).Trim();

                        res.Add(item);
                    }
                }
                else
                {
                    ret.text = "reader пуст";
                    ret.result = false;
                    return null;
                }

                #endregion

            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры GetSearchInfo " + ex.Message, MonitorLog.typelog.Error, true);
                return null;
            }
            finally
            {
                if (reader != null) reader.Close();
                sql.Remove(0, sql.Length);
                conn_web.Close();
            }

            return res;
        }

        /// <summary>
        /// Изменение marks для выбранных списков лс
        /// </summary>
        /// <param name="list0">список не выбранных лс</param>
        /// <param name="list1">список выбранных лс</param>
        /// <param name="finder">nzp_user необходим</param>
        /// <returns></returns>
        public Returns ChangeMarksSpisLs(Finder finder, List<Ls> list0, List<Ls> list1)
        {
            Returns ret = Utils.InitReturns();
            if (finder.nzp_user < 1)
            {
                ret.result = false;
                ret.text = "Не определен пользователь";
                return ret;
            }

            string true_ = "";
            for (int i = 0; i < list1.Count; i++)
            {
                if (i == 0) true_ += list1[i].nzp_kvar;
                else true_ += "," + list1[i].nzp_kvar;
            }
            if (true_ != "") true_ = "(" + true_ + ")";

            string false_ = "";
            for (int i = 0; i < list0.Count; i++)
            {
                if (i == 0) false_ += list0[i].nzp_kvar;
                else false_ += "," + list0[i].nzp_kvar;
            }
            if (false_ != "") false_ = "(" + false_ + ")";

            string tXX_sp = "";
            string keyfield = "";
            if (finder.dopFind.Count > 0)
            {
                if (finder.dopFind[0] == Constants.page_spisls.ToString())
                {
                    tXX_sp = "t" + Convert.ToString(finder.nzp_user) + "_spls";
                    keyfield = "nzp_kvar";
                }
                else if (finder.dopFind[0] == Constants.page_spisdom.ToString())
                {
                    tXX_sp = "t" + Convert.ToString(finder.nzp_user) + "_spdom";
                    keyfield = "nzp_dom";
                }
            }
            if (tXX_sp != "" && keyfield != "")
            {
                //выбрать общее кол-во
                IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
                ret = OpenDb(conn_web, true);
                if (!ret.result) return ret;

                if (!TableInWebCashe(conn_web, tXX_sp))
                {
                    conn_web.Close();
                    ret.result = false;
                    ret.text = "Данные не были выбраны";
                    return ret;
                }

                string sql = "";
                if (true_ != "")
                {
                    sql = "update " + tXX_sp + " set mark = 1 where " + keyfield + " in " + true_;
                    ret = ExecSQL(conn_web, sql, true);
                    if (!ret.result)
                    {
                        conn_web.Close();
                        return ret;
                    }
                }
                if (false_ != "")
                {
                    sql = "update " + tXX_sp + " set mark = 0 where " + keyfield + " in " + false_;
                    ret = ExecSQL(conn_web, sql, true);
                    if (!ret.result)
                    {
                        conn_web.Close();
                        return ret;
                    }
                }
                conn_web.Close();
            }
            return ret;
        }

        public List<Group> GetGroupLs(Group finder, out Returns ret) //вытащить группы лицевых счетов
        //----------------------------------------------------------------------
        {

            ret = Utils.InitReturns();

            #region Проверка входных данных
            //-----------------------------------------------------------------------------------
            if (!(finder.nzp_user > 0))
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Пользователь не известен";
                return null;
            }

            if (finder.pref == "")
            {
                ret.result = false;
                ret.tag = -2;
                ret.text = "Префикс не задан";
                return null;
            }
            //-----------------------------------------------------------------------------------
            #endregion

            List<Group> Spis = new List<Group>();

            Spis.Clear();

            //выбрать общее кол-во
            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result) return null;

            string tXX_cnt = "t" + Convert.ToString(finder.nzp_user) + "_s_group";
            string strWhere = " where 1=1";

            if (finder.nzp_group > 0) strWhere = strWhere + " and nzp_group = " + finder.nzp_group.ToString();
            if (finder.pref != "") strWhere = strWhere + " and pref = '" + finder.pref + "'";
            if (finder.ngroup2 != "") strWhere = strWhere + " and nzp_group not in (" + finder.ngroup2 + ")";
            if (!CasheExists(tXX_cnt))
            {
                ret.result = true;
                ret.text = "Данные не были выбраны! Выполните поиск.";
                ret.tag = -22;
                return null;
            }


            IDbCommand cmd = DBManager.newDbCommand(" Select count(*) From " + tXX_cnt + strWhere, conn_web);
            try
            {
                string s = Convert.ToString(cmd.ExecuteScalar());
                ret.tag = Convert.ToInt32(s);
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

                MonitorLog.WriteLog("Ошибка выборки справочника групп лицевых счетов " + err, MonitorLog.typelog.Error, 20, 201, true);
                return null;
            }

            string skip = "";
            string first = "";
#if PG
            if (finder.skip > 0) skip = " offset " + finder.skip.ToString();
            if (finder.rows > 0) first = " limit " + finder.rows.ToString();
#else
            if (finder.skip > 0) skip = " skip " + finder.skip.ToString();
            if (finder.rows > 0) first = " first " + finder.rows;
#endif
            string orderby = " order by ngroup";

            //выбрать список

            IDataReader reader;
#if PG
            if (!ExecRead(conn_web, out reader,
                            " Select nzp_group, ngroup, pref " +
                            " From " + tXX_cnt + strWhere + orderby + first + skip, true).result)
            {
                conn_web.Close();
                return null;
            }
#else
            if (!ExecRead(conn_web, out reader,
                            " Select " + skip +first +
                              " nzp_group, ngroup, pref " +
                            " From " + tXX_cnt + strWhere + orderby, true).result)
            {
                conn_web.Close();
                return null;
            }
#endif

            try
            {
                int i = 0;
                while (reader.Read())
                {
                    i = i + 1;
                    Group zap = new Group();

                    zap.num = (i + finder.skip).ToString();

                    // код группы
                    if (reader["nzp_group"] == DBNull.Value)
                        zap.nzp_group = 0;
                    else
                        zap.nzp_group = (int)reader["nzp_group"];
                    // название группы
                    if (reader["ngroup"] == DBNull.Value)
                        zap.ngroup = "";
                    else
                        zap.ngroup = reader["ngroup"].ToString().Trim();

                    zap.pref = (string)reader["pref"];

                    Spis.Add(zap);

                    if (i >= finder.rows) break;
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

                MonitorLog.WriteLog("Ошибка заполнения списка групп лицевых счетов " + err, MonitorLog.typelog.Error, 20, 201, true);

                return null;
            }

        }//GetGroupLs



        public static string MakeWhereStringGroup(List<Group> finder)
        {
            var sqlList = "";
            var kodstr = "";
            if (finder != null && finder.Count > 0) kodstr = finder[0].prms;
            else return sqlList;

            foreach (var fgroup in finder)
            {
                var sql = "";
                if (Utils.GetParams(kodstr, Constants.page_spisls) ||
                    Utils.GetParams(kodstr, Constants.page_spisgil))
                {
                    if (fgroup.ngroup != "" && fgroup.ngroup2 == "")
                    {
                        sql = String.Format(" exists (select 1 from {0}_data{1}link_group lg where " +
                                            " k.pref = '{0}' and lg.nzp = k.nzp_kvar and lg.nzp_group in ({2}))", 
                            Points.GetPref(fgroup.nzp_wp), tableDelimiter, fgroup.ngroup);
                    }
                    else if (fgroup.ngroup != "" && fgroup.ngroup2 != "")
                    {
                        sql = String.Format(" exists (select 1 from {0}_data{1}link_group lg where " +
                                            " k.pref = '{0}' and  lg.nzp = k.nzp_kvar and lg.nzp_group in ({2}) " +
                                            " and not exists (select 1 from {0}_data{1}link_group lg where  " +
                                            " k.pref = '{0}' and  lg.nzp = k.nzp_kvar and lg.nzp_group in ({3})))",
                            Points.GetPref(fgroup.nzp_wp), tableDelimiter, fgroup.ngroup, fgroup.ngroup2);
                    }
                    else if (fgroup.ngroup == "" && fgroup.ngroup2 != "")
                    {
                        sql = String.Format(" exists (select 1 from {0}_data{1}link_group lg where " +
                                            " k.pref = '{0}' and lg.nzp = k.nzp_kvar and lg.nzp_group not in ({2}))", 
                                            Points.GetPref(fgroup.nzp_wp), tableDelimiter, fgroup.ngroup2); }
                }
                else if (Utils.GetParams(kodstr, Constants.page_spisdom))
                {
                    if (fgroup.ngroup != "" && fgroup.ngroup2 == "")
                    {
                        sql = String.Format(" exists (select 1 from {0}_data{1}kvar k1, " +
                                            " {0}_data{1}link_group lg where k1.nzp_dom = d.nzp_dom " +
                                            " and lg.nzp = k1.nzp_kvar and lg.nzp_group in ({2}))",
                                            Points.GetPref(fgroup.nzp_wp), tableDelimiter, fgroup.ngroup);
                    }
                    else if (fgroup.ngroup != "" && fgroup.ngroup2 != "")
                    {
                        sql = String.Format("exists (select 1 from {0}_data{1}kvar k1, " +
                                            " {0}_data{1}link_group lg where " +
                                            " k1.nzp_dom = d.nzp_dom and lg.nzp = k1.nzp_kvar " +
                                            " and lg.nzp_group in ({2}) " +
                                            " and not exists ( select 1 from {0}_data{1}kvar k1, " +
                                            " {0}_data{1}link_group lg where " +
                                            " k1.nzp_dom = d.nzp_dom and lg.nzp = k1.nzp_kvar " +
                                            " and lg.nzp_group in ({3})))",
                                             Points.GetPref(fgroup.nzp_wp), tableDelimiter, fgroup.ngroup, fgroup.ngroup2);
                    }
                    else if (fgroup.ngroup == "" && fgroup.ngroup2 != "")
                    {
                        sql = String.Format(" not exists (select 1 from {0}_data{1}kvar k1, " +
                                            " {0}_data{1}link_group lg where k1.nzp_dom = d.nzp_dom " +
                                            " and lg.nzp = k1.nzp_kvar and lg.nzp_group in ({2}))",
                                             Points.GetPref(fgroup.nzp_wp), tableDelimiter, fgroup.ngroup2);
                    }
                }
                if (sqlList == "" && sql != "")
                {
                    sqlList = "(";
                    sqlList += sql;
                }
                else if (sql != "") sqlList += " or " + sql;
            }
            if (sqlList != "") sqlList += ")";
            return sqlList;
        }

        //обновлние АП
        //----------------------------------------------------------------------
        bool DropRefresh(IDbConnection conn_db)
        //----------------------------------------------------------------------
        {
            ExecSQL(conn_db, "Drop table ttt_all", false);
            ExecSQL(conn_db, "Drop table ttt_kvar", false);
            ExecSQL(conn_db, "Drop table ttt_all_dom", false);
            ExecSQL(conn_db, "Drop table ttt_dom", false);
            ExecSQL(conn_db, "Drop table ttt_area", false);
            ExecSQL(conn_db, "Drop table ttt_geu", false);
            ExecSQL(conn_db, "Drop table ttt_ulica", false);
            return false;
        }

        /// <summary>
        /// Создать кэш-таблицу для последних показаний приборов учета
        /// </summary>
        private void CreateTableWebUniquePointAreaGet(IDbConnection conn_web, string tXX_cv, bool onCreate, out Returns ret) //
        {
            if (onCreate)
            {
                if (TableInWebCashe(conn_web, tXX_cv))
                {
                    ExecSQL(conn_web, " Drop table " + tXX_cv, false);
                }

                //создать таблицу webdata:tXX_cv
#if PG
                ret = ExecSQL(conn_web,
                                      " Create table " + tXX_cv + "(" +
                                      " nzp_wp   integer," +
                                      " nzp_area integer," +
                                      " nzp_geu  integer," +
                                      " point    CHARACTER(100)," +
                                      " area     CHARACTER(40)," +
                                      " geu      CHARACTER(60) " +
                                      ")", true);
#else
                ret = ExecSQL(conn_web,
                                      " Create table " + tXX_cv + "(" +
                                      " nzp_wp   integer," +
                                      " nzp_area integer," +
                                      " nzp_geu  integer," +
                                      " point    nchar(100)," +
                                      " area     nchar(40)," +
                                      " geu      nchar(60) " +
                                      ")", true);
#endif
                if (!ret.result)
                {
                    conn_web.Close();
                    return;
                }
            }
            else
            {
                ret = ExecSQL(conn_web, " Create index ix1_" + tXX_cv + " on " + tXX_cv + " (nzp_wp) ", true);
                if (ret.result) { ret = ExecSQL(conn_web, " Create index ix_2" + tXX_cv + " on " + tXX_cv + " (nzp_area) ", true); }
                if (ret.result) { ret = ExecSQL(conn_web, " Create index ix_3" + tXX_cv + " on " + tXX_cv + " (nzp_geu) ", true); }
            }
        }



        public Returns PrepareSelectedListDom(Ls finder)
        {
            var control = GetCacheTablesControl.GetInstance<CacheTablesControlHouse>();
            return control.PrepareSelectedList(finder);
        }
    }



    public static class GetCacheTablesControl
    {
        /// <summary>
        /// Фабричный метод для получения класса управления кэш-таблицами
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static CacheTablesControl GetInstance<T>() where T : CacheTablesControl, new()
        {
            return new T();
        }
    }

    /// <summary>
    /// Класс управления кэш-таблицами типа tXX_selectedYY
    /// </summary>
    public abstract class CacheTablesControl
    {

        /// Подготавливает список лицевых счетов для выполнения групповой операции
        /// </summary>
        /// <param name="finder"></param>
        /// <returns></returns>
        public virtual Returns PrepareSelectedList(Ls finder)
        {
            if (finder.nzp_user < 1) return new Returns(false, "Пользователь не определен", -1);
            var ret = Utils.InitReturns();

            using (var conn_web = DBManager.GetConnection(Constants.cons_Webdata))
            {
                ret = DBManager.OpenDb(conn_web, true);
                if (!ret.result) return ret;

                #region наименование таблиц tXX_spls/tXX_spls_full/tXX_meta
                string tXX_spls = GetTableName(finder);
                if (Utils.GetParams(finder.prms, Constants.page_perechen_lsdom)) tXX_spls += "dom";
                if (!DBManager.TempTableInWebCashe(conn_web, tXX_spls)) return ret;
                #endregion

                int num;
                string tXX_selectedls;
                ret = CreateCacheTable(finder, conn_web, out tXX_selectedls, out num);
                if (!ret.result) return ret;

                //заполняем кэш-таблицу
                string sql = "insert into " + tXX_selectedls + " select * from " + tXX_spls + " where mark = 1";
                ret = DBManager.ExecSQL(conn_web, sql, true);
                if (!ret.result) return ret;

                //обновление статистики
                ret = DBManager.ExecSQL(conn_web, DBManager.sUpdStat + " " + DBManager.sDefaultSchema + tXX_selectedls, true);
                if (!ret.result) return ret;

                //запомним номер таблицы
                ret.tag = num;
            }


            return ret;
        }

        /// <summary>
        /// Создание кэш-таблицы
        /// </summary>
        /// <param name="finder"></param>
        /// <param name="conn_web"></param>
        /// <param name="tXX_selected"></param>
        /// <param name="num"></param>
        /// <returns></returns>
        protected abstract Returns CreateCacheTable(Ls finder, IDbConnection conn_web,
            out string tXX_selected, out int num);


        /// <summary>
        /// Удаляем кэш-таблицы старше 2х дней
        /// </summary>
        /// <param name="conn_web"></param>
        /// <param name="cache_tables"></param>
        /// <returns></returns>
        protected Returns DeleteOldCacheTables(IDbConnection conn_web, string cache_tables)
        {
            var ret = Utils.InitReturns();
            try
            {
                //считаем что 2х дней достаточно, после таблица подлежит удалению
                var sql = " SELECT id, table_name FROM " + cache_tables +
                          " WHERE (now()-created_on)> INTERVAL '2 DAY' " +
                          " LIMIT 100 ";
                var DB = ClassDBUtils.OpenSQL(sql, conn_web).resultData;
                var tablesForDelete = DB.Rows.Cast<DataRow>().ToDictionary(
                    row => DBManager.CastValue<long>(row["id"]),
                    row => DBManager.CastValue<string>(row["table_name"]).Trim());
                if (DB.Rows.Count > 0)
                {
                    //удаляем таблицы 
                    sql = string.Join(";", tablesForDelete.Select(x => "DROP TABLE " + x.Value));
                    ret = DBManager.ExecSQL(conn_web, sql, true);
                    if (!ret.result) return ret;

                    //удаляем записи о таблицах
                    sql = " DELETE FROM " + cache_tables +
                          " WHERE id IN (" + string.Join(",", tablesForDelete.Keys) + ")";
                    ret = DBManager.ExecSQL(conn_web, sql, true);
                    if (!ret.result) return ret;
                }
            }
            catch (Exception)
            {
                ret.result = false;
                ret.text = "Ошибка удаления старых кэш-таблиц";
                return ret;
            }
            return ret;
        }


        /// <summary>
        /// Создание таблицы лицевого счета
        /// </summary>
        /// <param name="conn_web">Соедиение к базе</param>
        /// <param name="tXX_spYY">Наименование таблицы</param>
        /// <param name="onCreate">Создание новой таблицы либо создание инфлексов на уже существующую</param>
        public virtual Returns CreateTableWeb(IDbConnection conn_web, string tXX_spYY, bool onCreate)
        {
            Returns ret = Utils.InitReturns();

            if (onCreate)
            {
#if PG
                ret = DBManager.ExecSQL(conn_web, " set search_path to 'public'", true);
                if (!ret.result) return ret;
#endif
                if (DBManager.TempTableInWebCashe(conn_web, tXX_spYY))
                {
                    ret = DBManager.ExecSQL(conn_web, " truncate table " + tXX_spYY, false);
                    if (!ret.result)
                    {
                        return ret;
                    }
                }
                else
                {
                    //создать таблицу webdata:tXX_spls
                    ret = DBManager.ExecSQL(conn_web,
                        " Create table " + tXX_spYY +
                        " ( nzp_kvar integer, " +
                        "   num_ls   integer, " +
                        "   num_ls_litera   char(250), " +
                        "   pkod10   integer, " +
                        "   pkod     decimal(13)," +
                        "   nzp_dom  integer, " +
                        "   nzp_area integer, " +
                        "   nzp_geu  integer, " +
                        "   nzp_ul   integer, " +
                        "   typek    integer, " +
                        "   fio      char(60)," +
                        "   adr      char(160)," +
                        "   nzp_raj   integer, " +
                        "   nzp_town  integer, " +
                        "   rajon    char(30)," +
                        "   town     char(30)," +
                        "   ulica    char(80)," +
                        "   ndom     char(20)," +
                        "   nkor     char(5)," +
                        "   idom     integer, " +
                        "   nkvar    char(20)," +
                        "   ikvar    integer, " +
                        "   nkvar_n  char(10)," +
                        "   ikvar_n    integer, " +
                        "   stypek   char(20)," +
                        "   sostls   char(20)," +
                        "   pref     char(20)," +
                        "   remark   char(100)," +
                        "   mark     integer default 1," +
                        "   has_pu   integer default 0" +
                        " ) ", true);

                    if (!ret.result)
                    {
                        return ret;
                    }

                    ret = CreateIndexesForTable(conn_web, tXX_spYY);
                }
            }
            else
            {
                ret = DBManager.ExecSQL(conn_web, DBManager.sUpdStat + " " + DBManager.sDefaultSchema + tXX_spYY, true);
            }
            return ret;
        }

        /// <summary>
        /// Создаем индексы для таблицы tXX_spls
        /// </summary>
        /// <param name="conn_web"></param>
        /// <param name="tXX_spYY"></param>
        /// <param name="ret"></param>
        /// <returns></returns>
        protected virtual Returns CreateIndexesForTable(IDbConnection conn_web, string tXX_spYY)
        {
            var ret = Utils.InitReturns();

            ret = DBManager.ExecSQL(conn_web,
                " Create index ix1_" + tXX_spYY + " on " + DBManager.sDefaultSchema + tXX_spYY + " (nzp_kvar,nzp_dom,pref) ",
                true);
            if (!ret.result) return ret;

            ret = DBManager.ExecSQL(conn_web,
                " Create index ix2_" + tXX_spYY + " on " + DBManager.sDefaultSchema + tXX_spYY + " (num_ls,pref) ",true);
            if (!ret.result) return ret;

            ret = DBManager.ExecSQL(conn_web,
                " Create index ix3_" + tXX_spYY + " on " + DBManager.sDefaultSchema + tXX_spYY + " (pkod10,pref) ",true);
            if (!ret.result) return ret;

            ret = DBManager.ExecSQL(conn_web,
                " Create index ix4_" + tXX_spYY + " on " + DBManager.sDefaultSchema + tXX_spYY + " (nzp_dom,pref) ",true);
            if (!ret.result) return ret;

            ret = DBManager.ExecSQL(conn_web,
                " Create index ix41_" + tXX_spYY + " on " + DBManager.sDefaultSchema + tXX_spYY + " (adr) ", true);
            if (!ret.result) return ret;

            ret = DBManager.ExecSQL(conn_web,
                " Create index ix42_" + tXX_spYY + " on " + DBManager.sDefaultSchema + tXX_spYY + " (ulica,idom) ",true);
            if (!ret.result) return ret;

            return ret;
        }
        /// <summary>
        /// Вернуть таблицу для получения данных
        /// </summary>
        /// <returns></returns>
        protected abstract string GetTableName(Ls finder);

    }


    /// <summary>
    /// Класс управления кэш-таблицами для tXX_selectedls
    /// </summary>
    public class CacheTablesControlLs : CacheTablesControl
    {

        protected override Returns CreateCacheTable(Ls finder, IDbConnection conn_web, out string tXX_selected, out int num)
        {
            Returns ret;
            #region создать кэш-таблицу

            const string cache_tables = DBManager.sDefaultSchema + "cache_tables";
            num = DBManager.ExecScalar<int>(conn_web,
                " SELECT MAX(number) FROM " + cache_tables +
                " WHERE nzp_user=" + finder.nzp_user + " AND type=" + (int)TypesCacheTables.Ls);
            num++;
            tXX_selected = "t" + finder.nzp_user + "_selectedls" + num;

            //перестраховка
            while (DBManager.TempTableInWebCashe(conn_web, tXX_selected))
            {
                num++;
                tXX_selected = "t" + finder.nzp_user + "_selectedls" + num;
            }

            //создаем кэш-таблицу
            ret = CreateTableWeb(conn_web, tXX_selected, true);
            if (!ret.result) return ret;

            //делаем запись в cache_tables - реестре кэш-таблиц
            var sql = string.Format(" INSERT INTO {0} (nzp_user,number,table_name, type) VALUES ({1},{2},'{3}',{4})",
                cache_tables, finder.nzp_user, num, tXX_selected, (int)TypesCacheTables.Ls);
            ret = DBManager.ExecSQL(conn_web, sql, true);
            if (!ret.result) return ret;

            //удаляем старые таблицы
            ret = DeleteOldCacheTables(conn_web, cache_tables);
            if (!ret.result) return ret;

            #endregion
            return ret;
        }

        protected override string GetTableName(Ls finder)
        {
            return "t" + finder.nzp_user + "_spls";
        }
    }

    public class CacheTablesControlHouse : CacheTablesControl
    {
        protected override Returns CreateCacheTable(Ls finder, IDbConnection conn_web, out string tXX_selected, out int num)
        {
            Returns ret;
            #region создать кэш-таблицу

            const string cache_tables = DBManager.sDefaultSchema + "cache_tables";
            num = DBManager.ExecScalar<int>(conn_web,
                " SELECT MAX(number) FROM " + cache_tables +
                " WHERE nzp_user=" + finder.nzp_user + " AND type=" + (int)TypesCacheTables.House);
            num++;
            tXX_selected = "t" + finder.nzp_user + "_selecteddom" + num;

            //перестраховка
            while (DBManager.TempTableInWebCashe(conn_web, tXX_selected))
            {
                num++;
                tXX_selected = "t" + finder.nzp_user + "_selecteddom" + num;
            }

            //создаем кэш-таблицу
            ret = CreateTableWeb(conn_web, tXX_selected, true);
            if (!ret.result) return ret;

            //делаем запись в cache_tables - реестре кэш-таблиц
            var sql = string.Format(" INSERT INTO {0} (nzp_user,number,table_name, type) VALUES ({1},{2},'{3}',{4})",
                cache_tables, finder.nzp_user, num, tXX_selected, (int)TypesCacheTables.House);
            ret = DBManager.ExecSQL(conn_web, sql, true);
            if (!ret.result) return ret;

            //удаляем старые таблицы
            ret = DeleteOldCacheTables(conn_web, cache_tables);
            if (!ret.result) return ret;

            #endregion
            return ret;
        }

        protected override string GetTableName(Ls finder)
        {
            return "t" + finder.nzp_user + "_spdom";
        }
    }

}
