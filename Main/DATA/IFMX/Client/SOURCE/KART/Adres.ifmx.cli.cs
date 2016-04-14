using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;

namespace STCLINE.KP50.DataBase
{
    //----------------------------------------------------------------------
    public partial class DbAdresClient : DataBaseHead
    //----------------------------------------------------------------------
    {
#if PG
        private readonly string pgDefaultSchema = "public";
#else
#endif

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
            if (Utils.GetParams(finder.prms, Constants.page_perechen_lsdom)) tXX_spls += "dom";

            if (!TableInWebCashe(conn_web, tXX_spls))
            {
                conn_web.Close();

                ret.tag = -1;
                ret.result = false;
                ret.text = "Данные не были выбраны";

                return null;
            }
            else
            {
                if (!isTableHasColumn(conn_web, tXX_spls, "mark"))
                {
                    ret = ExecSQL(conn_web, "alter table " + tXX_spls + " add mark integer", true);
                    if (!ret.result)
                    {
                        ret.text = "Не удалось добавить к таблице \"" + tXX_spls + "\" поле \"" + "mark" + " integer" + "\"";
                        return null;
                    }
                }
                if (!isTableHasColumn(conn_web, tXX_spls, "has_pu"))
                {
                    ret = ExecSQL(conn_web, "alter table " + tXX_spls + " add has_pu integer", true);
                    if (!ret.result)
                    {
                        ret.text = "Не удалось добавить к таблице \"" + tXX_spls + "\" поле \"" + "has_pu" + " integer" + "\"";
                        return null;
                    }
                }
            }

            string wheremark = " where 1=1 ";
            if (finder.mark == 1) wheremark += " and mark = 1";
            else if (finder.mark == 2) wheremark += " and mark = 0";


            IDbCommand cmd = DBManager.newDbCommand(" Select count(*) From " + tXX_spls + wheremark, conn_web);
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
                case Constants.sortby_adr: orderby = " Order by ulica,idom,ndom,ikvar "; break;
                case Constants.sortby_ls:
                    if (Points.IsSmr) orderby = " Order by pkod10 ";
                    else orderby = " Order by num_ls "; break;
            }


            /*
             * Получаем список лицевых счетов
             */
            IDataReader reader;

#if PG
            var sqlPgFormat = " Select a.*, round(pkod)||'' as spkod From {1} a {2} {3} offset {0}";
            var sql = string.Format(sqlPgFormat, finder.skip, tXX_spls, wheremark, orderby);
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

                MonitorLog.WriteLog("Ошибка заполнения списка лс " + err, MonitorLog.typelog.Error, 20, 201, true);

                return null;
            }
        }

        /// <summary>
        /// Создание таблицы лицевого счета
        /// </summary>
        /// <param name="conn_web">Соедиение к базе</param>
        /// <param name="tXX_spls">Наименование таблицы</param>
        /// <param name="onCreate">Создание новой таблицы либо создание инфлексов на уже существующую</param>
        public Returns CreateTableWebLs(IDbConnection conn_web, string tXX_spls, bool onCreate)
        {
            Returns ret;

            if (onCreate)
            {
                if (TableInWebCashe(conn_web, tXX_spls))
                {
                    ret = ExecSQL(conn_web, " Drop table " + tXX_spls, false);
                    if (!ret.result)
                    {
                        ret = ExecSQL(conn_web, " Delete from " + tXX_spls, false);
                        return ret;
                    }
                }

                //создать таблицу webdata:tXX_spls
                ret = ExecSQL(conn_web,
                          " Create table " + tXX_spls +
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

                          "   kod_ls   char(12)," +

                          "   ulica    char(80)," +
                          "   ndom     char(20)," +
                          "   idom     integer, " +
                          "   nkvar    char(20)," +
                          "   ikvar    integer, " +

                          "   stypek   char(20)," +
                          "   sostls   char(20)," +
                          "   pref     char(20)," +
                          "   mark     integer," +
                          "   has_pu   integer" +
                          " ) ", true);
                if (!ret.result)
                {
                    return ret;
                }
            }
            else
            {
                ret = ExecSQL(conn_web, " Create index ix1_" + tXX_spls + " on " + tXX_spls + " (nzp_kvar,nzp_dom,pref) ", true);
                if (ret.result) { ret = ExecSQL(conn_web, " Create index ix2_" + tXX_spls + " on " + tXX_spls + " (num_ls,pref) ", true); }
                if (ret.result) { ret = ExecSQL(conn_web, " Create index ix3_" + tXX_spls + " on " + tXX_spls + " (pkod10,pref) ", true); }
                if (ret.result) { ret = ExecSQL(conn_web, " Create index ix4_" + tXX_spls + " on " + tXX_spls + " (nzp_dom,pref) ", true); }

                if (ret.result) { ret = ExecSQL(conn_web, " Create index ix41_" + tXX_spls + " on " + tXX_spls + " (adr) ", true); }
                if (ret.result) { ret = ExecSQL(conn_web, " Create index ix42_" + tXX_spls + " on " + tXX_spls + " (ulica,idom) ", true); }

                if (ret.result)
                {
#if PG
                    ret = ExecSQL(conn_web, " analyze  " + tXX_spls, true);
#else
                    ret = ExecSQL(conn_web, " Update statistics for table  " + tXX_spls, true);

#endif

                }
            }

            return ret;
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
                if (Points.IsSmr)
                {
                    if (finder.pkod10 > 0)
                    {
                        sql = "insert into " + tXX_spfinder + " values (0,\'Лицевой счет\',\'" + finder.pkod10.ToString() + "\'," + nzp_page.ToString() + ")";
                        ret = ExecSQL(conn_web, sql, true);
                        if (!ret.result)
                        {
                            conn_web.Close();
                            return ret;
                        }
                    }
                }
                else if (finder.num_ls > 0)
                {
                    sql = "insert into " + tXX_spfinder + " values (0,\'Лицевой счет\',\'" + finder.num_ls.ToString() + "\'," + nzp_page.ToString() + ")";
                    ret = ExecSQL(conn_web, sql, true);
                    if (!ret.result)
                    {
                        conn_web.Close();
                        return ret;
                    }
                }

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
                sql = "insert into " + tXX_spfinder + " values (0,\'Банк данных\',\'" + finder.point.ToString() + "\'," + nzp_page.ToString() + ")";
            }
            else if (finder.dopPointList != null && finder.dopPointList.Count > 0)
            {
                sql = "insert into " + tXX_spfinder + " values (0,\'Банк данных\',\'" + finder.point.ToString() + "\'," + nzp_page.ToString() + ")";
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
                          " Order by area " + skip , true);
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
                          " Order by geu " + skip , true);
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

            if (!TableInWebCashe(conn_web, "map_objects"))
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
            string sql = "select kod, nzp_wp from map_objects a, map_points b where a.nzp_mo = b.nzp_mo and a.object_type = 1 and a.tip = " + MapObject.Tip.dom.GetHashCode().ToString() +
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

            string sql = " Select max(note) from map_objects where tip = -1";
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

            string sql = " Select b.x, b.y, a.note from map_objects a, map_points b where a.tip = -2 and a.nzp_mo = b.nzp_mo ";
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
                        sqlBuilder.AppendFormat(" values ({0}, {1})", mapObject.tip.GetHashCode(), mapObject.kod);
                        sqlBuilder.AppendFormat(",{0}", mapObject.nzp_wp > 0 ? mapObject.nzp_wp.ToString() : "null");
                        sqlBuilder.AppendFormat(",{0}", mapObject.object_type);
                        sqlBuilder.AppendFormat(",{0}", mapObject.note);
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
                        sqlBuilder.AppendFormat("Update map_objects set tip = {0}", mapObject.tip.GetHashCode());
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

                    sql = "delete from map_points where nzp_mo = " + mapObject.nzp_mo.ToString();
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
                if (!ExecSQL(conn_web, "Delete from map_points where nzp_mo in (select nzp_mo from map_objects " + where + " )", true).result)
                    throw new Exception("Не удалось удалить координаты объектов карты");
                if (!ExecSQL(conn_web, "Delete from map_objects " + where, true).result)
                    throw new Exception("Не удалось удалить объекты карты");
            }
            catch { }



            return ret.result;
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

            string tXX = "";
            if (finder.dopFind[0] == Constants.page_spisls.ToString())
            {
                tXX = "t" + Convert.ToString(finder.nzp_user) + "_selectedls" + finder.listNumber;
            }
            else if (finder.dopFind[0] == Constants.page_spisdom.ToString())
                tXX = "t" + Convert.ToString(finder.nzp_user) + "_spdom";

            if (!TableInWebCashe(conn_web, tXX))
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

        public List<_RajonDom> FindRajonDom(Finder finder, out Returns ret)
        {
            #region Проверка входных параметров
            if (finder.nzp_user < 1)
            {
                ret = new Returns(false, "Не определен пользователь");
                return null;
            }
            if (finder.pref == "")
            {
                ret = new Returns(false, "Не задан префикс базы данных");
                return null;
            }
            #endregion

            string connectionString = Points.GetConnByPref(finder.pref);
            IDbConnection conn_db = GetConnection(connectionString);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return null;

#if PG
            string sql = "Select nzp_raj_dom, rajon_dom, alt_rajon_dom from " + finder.pref + "_data.s_rajon_dom Order by rajon_dom";
#else
            string sql = "Select nzp_raj_dom, rajon_dom, alt_rajon_dom from " + finder.pref + "_data:s_rajon_dom Order by rajon_dom";

#endif

            IDataReader reader;
            ret = ExecRead(conn_db, out reader, sql, true);
            if (!ret.result)
            {
                conn_db.Close();
                return null;
            }

            List<_RajonDom> list = new List<_RajonDom>();
            try
            {
                while (reader.Read())
                {
                    _RajonDom raj = new _RajonDom();
                    raj.nzp_raj_dom = reader["nzp_raj_dom"] != DBNull.Value ? Convert.ToInt32(reader["nzp_raj_dom"]) : 0;
                    raj.rajon_dom = reader["rajon_dom"] != DBNull.Value ? Convert.ToString(reader["rajon_dom"]).Trim() : "";
                    raj.alt_rajon_dom = reader["alt_rajon_dom"] != DBNull.Value ? Convert.ToString(reader["alt_rajon_dom"]).Trim() : "";

                    list.Add(raj);
                }
            }
            catch (Exception ex)
            {
                ret = new Returns(false, ex.Message);
                MonitorLog.WriteLog("Ошибка заполнения списка районоа " + (Constants.Viewerror ? "\n" + ex.Message : ""), MonitorLog.typelog.Error, 20, 201, true);
                list.Clear();
            }
            if (reader != null) reader.Close();
            conn_db.Close();
            return list;
        }

        /// <summary>
        /// создать новую группу
        /// </summary>
        /// <param name="finder"></param>
        /// <returns></returns>
        public Returns CreateNewGroup(Group finder)
        {
            Returns ret = Utils.InitReturns();
            if (finder.nzp_user <= 0) return new Returns(false, "Не задан пользователь", -1);
            if (finder.ngroup == "") return new Returns(false, "Не задано наименование группы", -1);
            if (finder.pref.Trim() == "") return new Returns(false, "Не задан префикс", -1);

            string sql = "";
            IDataReader reader;

            #region Соединение с БД
            IDbConnection conn_db = GetConnection(Points.GetConnByPref(finder.pref));
            ret = OpenDb(conn_db, true);
            if (!ret.result) return ret;
            #endregion

            #region Проверить существование группы в бд
#if PG
            sql = "select count(*) as num from " + finder.pref + "_data.s_group where ngroup = " + Utils.EStrNull(finder.ngroup.Trim().ToUpper());
#else
            sql = "select count(*) as num from " + finder.pref + "_data:s_group where ngroup = " + Utils.EStrNull(finder.ngroup.Trim().ToUpper());

#endif

            ret = ExecRead(conn_db, out reader, sql, true);
            if (!ret.result)
            {
                conn_db.Close();
                return ret;
            }
            int num = 0;
            try
            {
                if (reader.Read())
                    if (reader["num"] != DBNull.Value) num = Convert.ToInt32(reader["num"]);
                reader.Close();
                if (num > 0)
                {
                    conn_db.Close();
                    return new Returns(false, "Группа с таким наименованием уже существует", -1);
                }
            }
            catch (Exception ex)
            {
                reader.Close();
                conn_db.Close();

                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;

                MonitorLog.WriteLog("Ошибка при создании новой группы " + err, MonitorLog.typelog.Error, 20, 201, true);

                return new Returns(false, ex.Message);
            }
            #endregion

            #region Добавить новую группу
#if PG
            sql = "insert into " + finder.pref + "_data.s_group (ngroup) values (" + Utils.EStrNull(finder.ngroup.Trim().ToUpper()) + ")";
#else
            sql = "insert into " + finder.pref + "_data:s_group (ngroup) values (" + Utils.EStrNull(finder.ngroup.Trim().ToUpper()) + ")";

#endif

            ret = ExecSQL(conn_db, sql, true);
            conn_db.Close();
            return ret;
            #endregion

        }

        /// <summary>
        /// Получить список групп, удовлетворяющих условиям поиска
        /// </summary>
        /// <param name="finder">Объект для поиска</param>
        /// <param name="ret">Объект с результатами поиска</param>
        /// <returns>Список групп</returns>
        public List<Group> GetListGroup(Group finder, out Returns ret)
        {
            ret = Utils.InitReturns(); //Инициализация результирующего объекта

            #region Проверка входных данных
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
            #endregion

            #region Условия поиска
            string where = "";
            if (finder.nzp_group > 0) where += " and nzp_group = " + finder.nzp_group;
            if (finder.dopFind != null && finder.dopFind.Count > 0) where += " and nzp_group in (" + finder.dopFind[0] + ")";

            string skip = "";
            if (finder.skip > 0)
            {
#if PG
                skip = " offset " + finder.skip;
#else
                skip = " skip " + finder.skip;
#endif
            }

            #endregion

            #region Наименование таблицы
#if PG
            string table = finder.pref + "_data.s_group";
#else
            string table = finder.pref + "_data:s_group";
#endif
            #endregion

            #region запрос
#if PG
            string sql = "select * from " + table + " where 1=1 " + where + " " + skip;
#else
            string sql = "select" + skip + " * from " + table + " where 1=1 " + where;
#endif
            #endregion

            #region Соединение с БД
            string conn_kernel = Points.GetConnByPref(finder.pref);
            IDbConnection conn_db = GetConnection(conn_kernel);
            //IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return null;
            #endregion

            #region определить число записей
            int total_record_count = 0;
            object count = ExecScalar(conn_db, " Select count(*) From " + table + " where 1=1 " + where, out ret, true);
            if (ret.result)
            {
                total_record_count = Convert.ToInt32(count);
            }
            #endregion

            #region Выполнить запрос
            IDataReader reader;
            ret = ExecRead(conn_db, out reader, sql, true);
            if (!ret.result)
            {
                conn_db.Close();
                return null;
            }
            #endregion

            #region Получить данные
            List<Group> list = new List<Group>();
            Group group;
            try
            {
                int row = 0;
                while (reader.Read())
                {
                    group = new Group();
                    row++;
                    group.num = row.ToString();
                    if (reader["nzp_group"] != DBNull.Value) group.nzp_group = (int)reader["nzp_group"];
                    if (reader["ngroup"] != DBNull.Value) group.ngroup = (string)reader["ngroup"];
                    list.Add(group);
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
            #endregion

            #region закрыть соединение
            ret.tag = total_record_count;
            conn_db.Close();
            #endregion

            return list;
        }

        //----------------------------------------------------------------------
        public void FindGroupLs(Group finder, out Returns ret) //найти и заполнить список ПУ
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
                return;
            }

            if (finder.pref == "")
            {
                ret.result = false;
                ret.tag = -2;
                ret.text = "Префикс не задан";
                return;
            }
            //-----------------------------------------------------------------------------------
            #endregion

            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result) return;

            string tXX_cnt = "t" + Convert.ToString(finder.nzp_user) + "_s_group";

            ExecSQL(conn_web, " Drop table " + tXX_cnt, false);

            //создать таблицу webdata:tXX_cnt
            ret = ExecSQL(conn_web,
                      " Create table " + tXX_cnt +
                      " ( nzp_group integer, " +
                      "   ngroup    char(80)," +
                      "   txt1      char(60)," +
                      "   txt2      char(60)," +
                      "   pref      char(20)  " +
                      " ) ", true);
            if (!ret.result)
            {
                conn_web.Close();
                return;
            }

            //заполнить webdata:tXX_cnt
            string conn_kernel = Points.GetConnByPref(finder.pref);
            IDbConnection conn_db = GetConnection(conn_kernel);
            //IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result)
            {
                conn_web.Close();
                return;
            }

#if PG
            string tXX_cnt_full = pgDefaultSchema + "." + tXX_cnt;
#else
            string tXX_cnt_full = conn_web.Database + "@" + DBManager.getServer(conn_web) + ":" + tXX_cnt;

#endif
            #region Условия поиска
            //-----------------------------------------------------------------------------------          
            string where = "";
            if (finder.nzp_group > 0) where += " and nzp_group = " + finder.nzp_group;
            if (finder.ngroup.Trim() != "") where += " and lower(ngroup) like '%" + finder.ngroup.Trim().ToLower() + "%'";
            /*  if (finder.nzp_kvar > 0)
              {
                  where += " and nzp_group not in (select l.nzp_group from "+finder.pref+"_data:link_group l where l.nzp = "+finder.nzp_kvar+")";
              }*/
            //-----------------------------------------------------------------------------------     
            #endregion

            StringBuilder sql = new StringBuilder();

            sql.Append(" Insert into " + tXX_cnt_full +
                           " (pref, nzp_group, ngroup, txt1, txt2) ");
#if PG
            sql.Append(" Select distinct '" + finder.pref + "', g.nzp_group, g.ngroup, g.txt1, g.txt2 ");
#else
            sql.Append(" Select unique '" + finder.pref + "', g.nzp_group, g.ngroup, g.txt1, g.txt2 ");
#endif
#if PG
            sql.Append(" From " + finder.pref + "_data.s_group g ");
#else
            sql.Append(" From " + finder.pref + "_data:s_group g ");

#endif
            sql.Append(" where 1=1");
            sql.Append(where);

            ret = ExecSQL(conn_db, sql.ToString(), true);
            if (!ret.result)
            {
                conn_db.Close();
                conn_web.Close();
                return;
            }

            conn_db.Close(); //закрыть соединение с основной базой

            //далее работаем с кешем
            //создаем индексы на tXX_cnt
            string ix = "ix" + Convert.ToString(finder.nzp_user) + "_s_group";

            ret = ExecSQL(conn_web, " Create index " + ix + "_1 on " + tXX_cnt + " (nzp_group,pref) ", true);
            if (ret.result)
            {
#if PG
                ret = ExecSQL(conn_web, " analyze  " + tXX_cnt, true);
#else
                ret = ExecSQL(conn_web, " Update statistics for table  " + tXX_cnt, true);

#endif
            }

            conn_web.Close();

            return;
        }//FindGroupLs

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
#if PG
            if (finder.skip > 0) skip = " offset " + finder.skip.ToString();
#else
            if (finder.skip > 0) skip = " skip " + finder.skip.ToString();
#endif
            string orderby = " order by ngroup";

            //выбрать список

            IDataReader reader;
#if PG
            if (!ExecRead(conn_web, out reader,
                            " Select nzp_group, ngroup, pref " +
                            " From " + tXX_cnt + strWhere + orderby  + skip, true).result)
            {
                conn_web.Close();
                return null;
            }
#else
            if (!ExecRead(conn_web, out reader,
                            " Select " + skip +
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
                        zap.ngroup = (string)reader["ngroup"];

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

        public void SaveSupplierLs(Area_ls finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            #region Проверка входных данных
            //-----------------------------------------------------------------------------------
            if (finder.pref == "")
            {
                ret.result = false;
                ret.tag = -2;
                ret.text = "Префикс не задан";
                return;
            }

            if (!(finder.nzp_kvar > 0))
            {
                ret.result = false;
                ret.tag = -3;
                ret.text = "Лицевой счет не задан";
                return;
            }

            //-----------------------------------------------------------------------------------
            #endregion
            string conn_kernel = Points.GetConnByPref(finder.pref);
            IDbConnection conn_db = GetConnection(conn_kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return;
            StringBuilder sql = new StringBuilder();
            string note = String.Empty;
            if (finder.mode == 1)
            {
                sql.Append("Insert into " + finder.pref + "_data" + tableDelimiter + "alias_ls (nzp_kvar,numls,nzp_supp,comment) values ("
                    + finder.nzp_kvar + ",'" + finder.numls + "'," + finder.nzp_supp + ",'" + finder.comment + "');");
                note = "Добавление нового кода ЛС поставщика ("+finder.nzp_supp+")";
            }
            else
            {
                sql.Append("Update " + finder.pref + "_data" + tableDelimiter + "alias_ls set(numls,nzp_supp,comment)=");
                sql.Append("('" + finder.numls + "'," + finder.nzp_supp + ",'" + finder.comment.Trim() + "') where nzp_supp=" + finder.supplier + " and nzp_kvar=" + finder.nzp_kvar + ";");
                note = "Изменение кода ЛС поставщика (" + finder.nzp_supp + ")";
            }
            sql.Append(" insert into " + Points.Pref + "_data" + tableDelimiter + "sys_events (date_,nzp_user,nzp_dict_event,nzp,note) values");
            sql.Append(" ('" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.000") + "',(select nzp_user from " + finder.pref + "_data" + tableDelimiter + "users where web_user = " + finder.nzp_user + "),6495," + finder.nzp_kvar + ",'" + note + "');");
            if (!ExecSQL(conn_db, sql.ToString(), true).result)
            {
                conn_db.Close();
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка при сохранении текущего лицевого счета";
                return;
            }
            conn_db.Close();
            return;
        }

        public Area_ls LoadCurrentAliasLs(Area_ls finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            #region Проверка входных данных
            //-----------------------------------------------------------------------------------
            if (finder.pref == "")
            {
                ret.result = false;
                ret.tag = -2;
                ret.text = "Префикс не задан";
                return null;
            }

            if (!(finder.nzp_kvar > 0))
            {
                ret.result = false;
                ret.tag = -3;
                ret.text = "Лицевой счет не задан";
                return null;
            }

            //-----------------------------------------------------------------------------------
            #endregion
            string conn_kernel = Points.GetConnByPref(finder.pref);
            IDbConnection conn_db = GetConnection(conn_kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return null;
            StringBuilder str = new StringBuilder();
            str.Append("select a.nzp_kvar,a.numls,a.nzp_supp,a.comment,max(s.date_) as date_,s.nzp_user,u.comment as name,supp.name_supp from " + finder.pref + "_data" + tableDelimiter + "alias_ls a left outer join " + finder.pref + "_data" + tableDelimiter
                + "sys_events s on a.nzp_kvar=s.nzp and s.nzp_dict_event=6495 and s.note like '%('||a.nzp_supp||')%' left outer join " + finder.pref + "_data" + tableDelimiter +
                "users u on u.web_user=s.nzp_user," + finder.pref + "_kernel" + tableDelimiter + "supplier supp where supp.nzp_supp=a.nzp_supp and a.nzp_kvar=" + finder.nzp_kvar + "and a.nzp_supp=" + finder.nzp_supp + " group by 1,2,3,4,6,7,8");
            IDataReader reader;
            if (!ExecRead(conn_db, out reader, str.ToString(), true).result)
            {
                conn_db.Close();
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка при загрузке текущего лицевого счета";
                return null;
            }

            try
            {
                Area_ls zap = new Area_ls();
                while (reader.Read())
                {
                    zap.nzp_kvar = reader["nzp_kvar"] == DBNull.Value ? 0 : Convert.ToInt32(reader["nzp_kvar"]);
                    zap.numls = reader["numls"] == DBNull.Value ? "" : reader["nzp_kvar"].ToString();
                    zap.nzp_supp = reader["nzp_supp"] == DBNull.Value ? 0 : Convert.ToInt32(reader["nzp_supp"]);
                    zap.supplier = reader["name_supp"] == DBNull.Value ? "" : reader["name_supp"].ToString().Trim();
                    zap.comment = reader["comment"] == DBNull.Value ? "" : reader["comment"].ToString();
                    zap.changes = (reader["name"] == DBNull.Value ? "" : reader["name"].ToString().Trim()) + (reader["date_"] == DBNull.Value ? "" : " (" + reader["date_"].ToString() + ")");
                }

                reader.Close();
                conn_db.Close();
                return zap;
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

                MonitorLog.WriteLog("Ошибка выборки кодов поставщиков текущего лицевого счета " + err, MonitorLog.typelog.Error, 20, 201, true);

                return null;
            }

        }

        public void DeleteSupplierLs(Area_ls finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            #region Проверка входных данных
            //-----------------------------------------------------------------------------------
            if (finder.pref == "")
            {
                ret.result = false;
                ret.tag = -2;
                ret.text = "Префикс не задан";
                return;
            }

            if (!(finder.nzp_kvar > 0))
            {
                ret.result = false;
                ret.tag = -3;
                ret.text = "Лицевой счет не задан";
                return;
            }
            #endregion
            string conn_kernel = Points.GetConnByPref(finder.pref);
            IDbConnection conn_db = GetConnection(conn_kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return;
            string str = "delete from " + finder.pref + "_data" + tableDelimiter + "alias_ls where nzp_kvar=" + finder.nzp_kvar + " and nzp_supp=" + finder.nzp_supp;
            if (!ExecSQL(conn_db, str.ToString(), true).result)
            {
                conn_db.Close();
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка при удалении кода лицевого счета";
                return ;
            }
            conn_db.Close();
            return;
        }

        public List<Area_ls> LoadCurrentLsSupplier(Area_ls finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            #region Проверка входных данных
            //-----------------------------------------------------------------------------------
            if (finder.pref == "")
            {
                ret.result = false;
                ret.tag = -2;
                ret.text = "Префикс не задан";
                return null;
            }

            if (!(finder.nzp_kvar > 0))
            {
                ret.result = false;
                ret.tag = -3;
                ret.text = "Лицевой счет не задан";
                return null;
            }

            //-----------------------------------------------------------------------------------
            #endregion
            List<Area_ls> ls_list = new List<Area_ls>();
            string conn_kernel = Points.GetConnByPref(finder.pref);
            IDbConnection conn_db = GetConnection(conn_kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return null;
            List<Area_ls> list = new List<Area_ls>();
            StringBuilder str = new StringBuilder();
            str.Append("select a.nzp_kvar,a.numls,a.nzp_supp,a.comment,max(s.date_) as date_,s.nzp_user,u.comment as name,supp.name_supp from " + finder.pref + "_data" + tableDelimiter + "alias_ls a left outer join " + Points.Pref+ "_data" + tableDelimiter
                + "sys_events s on a.nzp_kvar=s.nzp and s.nzp_dict_event=6495 and s.note like '%('||a.nzp_supp||')%' left outer join " + finder.pref + "_data" + tableDelimiter +
                "users u on u.web_user=s.nzp_user," + finder.pref + "_kernel" + tableDelimiter + "supplier supp where supp.nzp_supp=a.nzp_supp and a.nzp_kvar=" + finder.nzp_kvar + " group by 1,2,3,4,6,7,8");
            IDataReader reader;
            if (!ExecRead(conn_db, out reader, str.ToString(), true).result)
            {
                conn_db.Close();
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка при загрузке текущего лицевого счета";
                return null;
            }

            try
            {
                int i = 0;
                while (reader.Read())
                {
                    i = i + 1;
                    Area_ls zap = new Area_ls();

                    zap.num = i.ToString();
                    zap.nzp_kvar = reader["nzp_kvar"] == DBNull.Value ? 0 : Convert.ToInt32(reader["nzp_kvar"]);
                    zap.numls = reader["numls"] == DBNull.Value ? "" : reader["numls"].ToString().Trim();
                    zap.nzp_supp = reader["nzp_supp"] == DBNull.Value ? 0 : Convert.ToInt32(reader["nzp_supp"]);
                    zap.supplier = reader["name_supp"] == DBNull.Value ? "" : reader["name_supp"].ToString().Trim();
                    zap.comment = reader["comment"] == DBNull.Value ? "" : reader["comment"].ToString();
                    zap.changes = (reader["name"] == DBNull.Value ? "" : reader["name"].ToString().Trim()) + (reader["date_"] == DBNull.Value ? "" : " (" + reader["date_"].ToString() + ")");
                    list.Add(zap);
                }

                reader.Close();
                conn_db.Close();
                return list;
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

                MonitorLog.WriteLog("Ошибка выборки кодов поставщиков текущего лицевого счета " + err, MonitorLog.typelog.Error, 20, 201, true);

                return null;
            }
        }

        public List<Group> LoadCurrentLsGroup(Group finder, out Returns ret) //загрузить группы текущего лицевого счета
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();

            #region Проверка входных данных
            //-----------------------------------------------------------------------------------
            if (finder.pref == "")
            {
                ret.result = false;
                ret.tag = -2;
                ret.text = "Префикс не задан";
                return null;
            }

            if (!(finder.nzp_kvar > 0))
            {
                ret.result = false;
                ret.tag = -3;
                ret.text = "Лицевой счет не задан";
                return null;
            }

            //-----------------------------------------------------------------------------------
            #endregion

            List<Group> Spis = new List<Group>();

            Spis.Clear();

            //выбрать общее кол-во
            string conn_kernel = Points.GetConnByPref(finder.pref);
            IDbConnection conn_db = GetConnection(conn_kernel);
            //IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return null;
#if PG
            string pref = finder.pref + "_data.";
            //выбрать группы текущего лицевого счета
            string sqlStr =
                 " Select '" + finder.pref + "' as pref , " +
                 "   g.nzp_group, g.ngroup " +
                 " From " + pref + "link_group l, " + pref + "s_group g " +
                 " Where l.nzp_group = g.nzp_group " +
                 "   and l.nzp = " + finder.nzp_kvar.ToString() +
                 " order by g.ngroup ";
#else
            string pref = finder.pref + "_data:";
            //выбрать группы текущего лицевого счета
            string sqlStr =
                 " Select '" + finder.pref + "' as pref , " +
                 "   g.nzp_group, g.ngroup " +
                 " From " + pref + "link_group l, " + pref + "s_group g " +
                 " Where l.nzp_group = g.nzp_group " +
                 "   and l.nzp = " + finder.nzp_kvar.ToString() +
                 " order by g.ngroup ";
#endif

            IDataReader reader;
            if (!ExecRead(conn_db, out reader, sqlStr, true).result)
            {
                conn_db.Close();
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка при загрузке групп текущего лицевого счета";
                return null;
            }

            try
            {
                int i = 0;
                while (reader.Read())
                {
                    i = i + 1;
                    Group zap = new Group();

                    zap.num = i.ToString();

                    // код группы
                    if (reader["nzp_group"] == DBNull.Value)
                        zap.nzp_group = 0;
                    else
                        zap.nzp_group = (int)reader["nzp_group"];
                    // название группы
                    if (reader["ngroup"] == DBNull.Value)
                        zap.ngroup = "";
                    else
                        zap.ngroup = (string)reader["ngroup"];

                    zap.pref = (string)reader["pref"];

                    Spis.Add(zap);
                }

                reader.Close();
                conn_db.Close();
                return Spis;
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

                MonitorLog.WriteLog("Ошибка выборки групп текущего лицевого счета " + err, MonitorLog.typelog.Error, 20, 201, true);

                return null;
            }

        }//LoadCurrentLsGroup

        public bool SaveLsGroup(Group finder, List<string> groupList, out Returns ret) //загрузить группы текущего лицевого счета
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();

            #region Проверка входных данных
            //-----------------------------------------------------------------------------------
            if (finder.pref == "")
            {
                ret.result = false;
                ret.tag = -2;
                ret.text = "Префикс не задан";
                return false;
            }

            if (!(finder.nzp_kvar > 0))
            {
                ret.result = false;
                ret.tag = -3;
                ret.text = "Лицевой счет не задан";
                return false;
            }

            //-----------------------------------------------------------------------------------
            #endregion

            #region подключение к базе
            //-----------------------------------------------------------------------------------
            string conn_kernel = Points.GetConnByPref(finder.pref);
            IDbConnection conn_db = GetConnection(conn_kernel);
            //IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return false;
            //-----------------------------------------------------------------------------------
            #endregion

            string sqlString = "";
#if PG
            string pref = finder.pref + "_data.";
#else
            string pref = finder.pref + "_data:";

#endif
            IDbTransaction transaction;
            try { transaction = conn_db.BeginTransaction(); }
            catch { transaction = null; }

            #region сохранение
            //-----------------------------------------------------------------------------------
            sqlString = "delete from " + pref + "link_group where nzp = " + finder.nzp_kvar.ToString();

            ret = ExecSQL(conn_db, transaction, sqlString, true);
            if (ret.result)
            {
                for (int i = 0; i < groupList.Count; i++)
                {
                    sqlString = "insert into " + pref + "link_group (nzp_group, nzp) values (" + groupList[i] + "," + finder.nzp_kvar.ToString() + ")";
                    ret = ExecSQL(conn_db, transaction, sqlString, true);

                    if (!ret.result) break;
                }

                if (ret.result)
                {
                    if (transaction != null) transaction.Commit();
                }
                else
                {
                    ret.text = "Ошибка сохранения групп лицевого счета";
                    if (transaction != null) transaction.Rollback();
                }

            }
            else
            {
                ret.text = "Ошибка сохранения групп лицевого счета";
                if (transaction != null)
                    transaction.Rollback();
            }
            //-----------------------------------------------------------------------------------
            #endregion

            return ret.result;

        }//SaveLsGroup

        public static string MakeWhereStringGroup(Group finder)
        {
            string sql = "";
            if (Utils.GetParams(finder.prms, Constants.page_spisls))
            {
                if (finder.ngroup != "" && finder.ngroup2 == "")
                {
#if PG
                    sql = "select count(*) from PREFX_data.link_group lg where lg.nzp = k.nzp_kvar and lg.nzp_group in (" + finder.ngroup + ")";
#else
                    sql = "select count(*) from PREFX_data:link_group lg where lg.nzp = k.nzp_kvar and lg.nzp_group in (" + finder.ngroup + ")";

#endif
                }
                else if (finder.ngroup != "" && finder.ngroup2 != "")
                {
#if PG
                    sql = "select count(*) from PREFX_data.link_group lg where lg.nzp = k.nzp_kvar and lg.nzp_group in (" + finder.ngroup
                          + ") and lg.nzp_group not in (" + finder.ngroup2 + ")";
#else
                    sql = "select count(*) from PREFX_data:link_group lg where lg.nzp = k.nzp_kvar and lg.nzp_group in (" + finder.ngroup
                          + ") and lg.nzp_group not in (" + finder.ngroup2 + ")";

#endif
                }
                else if (finder.ngroup == "" && finder.ngroup2 != "")
                {
#if PG
                    sql =
                        "select count(*) from PREFX_data.kvar k1 where k1.nzp_kvar = k.nzp_kvar and k1.nzp_kvar not in (select lg.nzp from PREFX_data.link_group lg where lg.nzp_group in ("
                        + finder.ngroup2 + "))";
#else
                    sql =
                        "select count(*) from PREFX_data:kvar k1 where k1.nzp_kvar = k.nzp_kvar and k1.nzp_kvar not in (select lg.nzp from PREFX_data:link_group lg where lg.nzp_group in ("
                        + finder.ngroup2 + "))";

#endif
                }
            }
            else if (Utils.GetParams(finder.prms, Constants.page_spisdom))
            {
                if (finder.ngroup != "" && finder.ngroup2 == "")
                {
#if PG
                    sql =
                        "select count(*) from PREFX_data.kvar k1, PREFX_data.link_group lg where k1.nzp_dom = d.nzp_dom and lg.nzp = k1.nzp_kvar and lg.nzp_group in ("
                        + finder.ngroup + ")";
#else
                    sql =
                        "select count(*) from PREFX_data:kvar k1, PREFX_data:link_group lg where k1.nzp_dom = d.nzp_dom and lg.nzp = k1.nzp_kvar and lg.nzp_group in ("
                        + finder.ngroup + ")";

#endif
                }
                else if (finder.ngroup != "" && finder.ngroup2 != "")
                {
#if PG
                    sql =
                        "select count(*) from PREFX_data.kvar k1, PREFX_data.link_group lg where k1.nzp_dom = d.nzp_dom and lg.nzp = k1.nzp_kvar and lg.nzp_group in ("
                        + finder.ngroup + ") and lg.nzp_group not in (" + finder.ngroup2 + ")";
#else
                    sql =
                        "select count(*) from PREFX_data:kvar k1, PREFX_data:link_group lg where k1.nzp_dom = d.nzp_dom and lg.nzp = k1.nzp_kvar and lg.nzp_group in ("
                        + finder.ngroup + ") and lg.nzp_group not in (" + finder.ngroup2 + ")";

#endif
                }
                else if (finder.ngroup == "" && finder.ngroup2 != "")
                {
#if PG
                    sql =
                        "select count(*) from PREFX_data.dom d1 where d1.nzp_dom=d.nzp_dom and d1.nzp_dom not in (select k1.nzp_dom from PREFX_data.kvar k1, PREFX_data.link_group lg where k1.nzp_dom = d.nzp_dom and lg.nzp = k1.nzp_kvar and lg.nzp_group in ("
                        + finder.ngroup2 + "))";
#else
                    sql =
                        "select count(*) from PREFX_data:dom d1 where d1.nzp_dom=d.nzp_dom and d1.nzp_dom not in (select k1.nzp_dom from PREFX_data:kvar k1, PREFX_data:link_group lg where k1.nzp_dom = d.nzp_dom and lg.nzp = k1.nzp_kvar and lg.nzp_group in ("
                        + finder.ngroup2 + "))";

#endif
                }
            }
            else if (Utils.GetParams(finder.prms, Constants.page_spisgil))
            {
                if (finder.ngroup != "" && finder.ngroup2 == "")
                {
#if PG
                    sql = "select count(*) from PREFX_data.link_group lg where lg.nzp = k.nzp_kvar and lg.nzp_group in (" + finder.ngroup + ")";
#else
                    sql = "select count(*) from PREFX_data:link_group lg where lg.nzp = k.nzp_kvar and lg.nzp_group in (" + finder.ngroup + ")";

#endif
                }
                else if (finder.ngroup != "" && finder.ngroup2 != "")
                {
#if PG
                    sql = "select count(*) from PREFX_data.link_group lg where lg.nzp = k.nzp_kvar and lg.nzp_group in (" + finder.ngroup
                          + ") and lg.nzp_group not in (" + finder.ngroup2 + ")";
#else
                    sql = "select count(*) from PREFX_data:link_group lg where lg.nzp = k.nzp_kvar and lg.nzp_group in (" + finder.ngroup
                          + ") and lg.nzp_group not in (" + finder.ngroup2 + ")";

#endif
                }
                else if (finder.ngroup == "" && finder.ngroup2 != "")
                {
#if PG
                    sql =
                        "select count(*) from PREFX_data.kvar k1 where k1.nzp_kvar = k.nzp_kvar and k1.nzp_kvar not in (select lg.nzp from PREFX_data.link_group lg where lg.nzp_group in ("
                        + finder.ngroup2 + "))";
#else
                    sql =
                        "select count(*) from PREFX_data:kvar k1 where k1.nzp_kvar = k.nzp_kvar and k1.nzp_kvar not in (select lg.nzp from PREFX_data:link_group lg where lg.nzp_group in ("
                        + finder.ngroup2 + "))";

#endif
                }
            }
            return sql;
        }

        public bool SaveListGroup(Group finder, out Returns ret) //включить/исключить выбранный список в/из групп
        {
            ret = Utils.InitReturns();

            #region Проверка входных данных
            if (finder.pref == "")
            {
                ret = new Returns(false, "Префикс не задан", -1);
                return ret.result;
            }
            if (finder.nzp_user <= 0)
            {
                ret = new Returns(false, "Не задан пользователь", -1);
                return ret.result;
            }
            if (finder.ngroup == "")
            {
                ret = new Returns(false, "Нет выбранных групп", -1);
                return ret.result;
            }
            #endregion

            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result) return false;
            //проверить
            string tXX = "t" + finder.nzp_user.ToString() + "_spls";
#if PG
         //  string tXX_full = conn_web.Database + "." + tXX;
            string tXX_full = "public." + tXX;

#else
            string tXX_full = conn_web.Database + "@" + DBManager.getServer(conn_web) + ":" + tXX;

#endif
            if (!TableInWebCashe(conn_web, tXX))
            {
                ret = new Returns(false, "Нет выбранных лицевых счетов", -1);
                return ret.result;
            }

            #region подключение к базе
            string conn_kernel = Points.GetConnByPref(finder.pref);
            IDbConnection conn_db = GetConnection(conn_kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return false;
            #endregion

            string sqlString = "";

            IDbTransaction transaction;
            try { transaction = conn_db.BeginTransaction(); }
            catch { transaction = null; }

            if (Utils.GetParams(finder.prms, Constants.act_add_ingroup))//добавление
            {
                string[] array = finder.ngroup.Split(',');
                foreach (string nzp in array)
                {
#if PG
                    sqlString = "delete from " + finder.pref + "_data.link_group where nzp_group = " + nzp +
                                " and nzp in (select nzp_kvar from " + tXX_full + " where pref = '" + finder.pref + "' and mark=1)";
#else
                    sqlString = "delete from " + finder.pref + "_data:link_group where nzp_group = " + nzp +
                                " and nzp in (select nzp_kvar from " + tXX_full + " where pref = '" + finder.pref + "' and mark=1)";

#endif
                    ret = ExecSQL(conn_db, transaction, sqlString, true);
                    if (ret.result)
                    {
#if PG
                        sqlString = "insert into " + finder.pref + "_data.link_group (nzp_group, nzp) " +
                                    " select " + nzp + ", nzp_kvar from " + tXX_full + " where pref = '" + finder.pref + "' and mark=1";
#else
                        sqlString = "insert into " + finder.pref + "_data:link_group (nzp_group, nzp) " +
                                    " select " + nzp + ", nzp_kvar from " + tXX_full + " where pref = '" + finder.pref + "' and mark=1";

#endif
                        ret = ExecSQL(conn_db, transaction, sqlString, true);
                        if (!ret.result) break;
                    }
                    else break;
                }
                if (ret.result)
                {
                    if (transaction != null) transaction.Commit();
                }
                else
                {
                    if (transaction != null) transaction.Rollback();
                }
            }
            else
            {
                if (Utils.GetParams(finder.prms, Constants.act_del_outgroup))//удаление
                {
                    string[] array = finder.ngroup.Split(',');
                    foreach (string nzp in array)
                    {
#if PG
                        sqlString = "delete from " + finder.pref + "_data.link_group where nzp_group = " + nzp +
                                    " and nzp in (select nzp_kvar from " + tXX_full + " where pref = '" + finder.pref + "' and mark=1)";
#else
                        sqlString = "delete from " + finder.pref + "_data:link_group where nzp_group = " + nzp +
                                    " and nzp in (select nzp_kvar from " + tXX_full + " where pref = '" + finder.pref + "' and mark=1)";

#endif
                        ret = ExecSQL(conn_db, transaction, sqlString, true);
                        if (!ret.result) break;
                    }
                    if (ret.result)
                    {
                        if (transaction != null) transaction.Commit();
                    }
                    else
                    {
                        if (transaction != null) transaction.Rollback();
                    }
                }
            }

            conn_web.Close();
            conn_db.Close();
            return ret.result;

        }//SaveListGroup

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

        /// <summary>
        /// Подготавливает список лицевых счетов для выполнения групповой операции
        /// </summary>
        /// <param name="finder"></param>
        /// <returns></returns>
        public Returns PrepareSelectedListLs(Ls finder)
        {
            if (finder.nzp_user < 1) return new Returns(false, "Пользователь не определен", -1);

            #region соединение conn_web
            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            Returns ret = OpenDb(conn_web, true);
            if (!ret.result) return ret;
            #endregion

            #region наименование таблиц tXX_spls/tXX_spls_full/tXX_meta
            string tXX_spls = "t" + finder.nzp_user + "_spls";
            if (Utils.GetParams(finder.prms, Constants.page_perechen_lsdom)) tXX_spls += "dom";
            #endregion

            try
            {
                if (!TempTableInWebCashe(conn_web, tXX_spls)) return ret;

                #region создать кэш-таблицу
                int num = 0;
                string tXX_selectedls = "t" + finder.nzp_user + "_selectedls" + num;
                while (TempTableInWebCashe(conn_web, tXX_selectedls))
                {
                    num++;
                    tXX_selectedls = "t" + finder.nzp_user + "_selectedls" + num;
                }

                ret = CreateTableWebLs(conn_web, tXX_selectedls, true);
                if (!ret.result) return ret;
                #endregion

                string sql = "insert into " + tXX_selectedls + " select * from " + tXX_spls + " where mark = 1";
                ret = ExecSQL(conn_web, sql, true);

                ret = CreateTableWebLs(conn_web, tXX_selectedls, false);

                //запомним номер таблицы
                if (ret.result) ret.tag = num;
            }
            finally
            {
                conn_web.Close();
            }

            return ret;
        }


    }
}
