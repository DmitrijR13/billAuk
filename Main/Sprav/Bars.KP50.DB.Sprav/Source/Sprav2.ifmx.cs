using System;
using System.Data;
using System.Collections.Generic;
using System.Linq;
using System.Security.Authentication;
using System.Text;
using Bars.KP50.DB.Sprav.Source;
using Bars.Security.Authentication;
using Bars.Security.Authentication.Session;
using Bars.Security.Authentication.Strategy;
using Bars.Security.Authorization;
using Bars.Security.Authorization.Access;
using Bars.Security.Authorization.Strategy;
using Bars.Security.Exceptions.Security;
using Castle.MicroKernel.Handlers;
using Castle.MicroKernel.ModelBuilder.Descriptors;
using STCLINE.KP50.Client;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;

namespace STCLINE.KP50.DataBase
{
    public partial class DbSprav
    {
        public Returns UpdateSpravTables(Finder finder)
        {
            if (finder.nzp_user <= 0)
            {
                return new Returns(false, "Не задан пользователь");
            }
            if (GlobalSettings.WorkOnlyWithCentralBank)
            {
                return new Returns(false, "Функция обновления справочников недоступна, т.к. установлен режим работы с центральным банком данных", -1);
            }

            Returns ret = Utils.InitReturns();

            if (Points.isClone)
            {
                string sql = " select dbname, dbserver " +
                             " from " + Points.Pref + DBManager.sKernelAliasRest + "s_baselist where idtype = 10";
                string connectionString = Points.GetConnByPref(Points.Pref);
                IDbConnection connDB = GetConnection(connectionString);
                ret = OpenDb(connDB, true);
                if (!ret.result) return ret;

                MyDataReader reader;
                ret = ExecRead(connDB, out reader, sql, true);
                if (!ret.result)
                {
                    connDB.Close();
                    return ret;
                }
                string dbname = "", dbserver = "";
                try
                {
                    if (reader.Read())
                    {
                        if (reader["dbname"] != DBNull.Value) dbname = Convert.ToString(reader["dbname"]).Trim();
                        if (reader["dbserver"] != DBNull.Value) dbserver = "@" + Convert.ToString(reader["dbserver"]).Trim();
                    }
                }
                catch (Exception ex)
                {
                    MonitorLog.WriteLog("Ошибка UpdateSpravTables() " + (Constants.Viewerror ? "\n " + ex.Message : ""), MonitorLog.typelog.Error, 20, 201, true);
                    return ret;
                }

                IDbTransaction trans = connDB.BeginTransaction();
                sql = "delete from " + Points.Pref + DBManager.sDataAliasRest + "s_area";
                ret = ExecSQL(connDB, trans, sql, true);
                if (!ret.result)
                {
                    trans.Rollback();
                    connDB.Close();
                    return ret;
                }

                sql = " insert into " + Points.Pref + DBManager.sDataAliasRest + "s_area " +
                      " select * from " + dbname + "_data" + dbserver + DBManager.tableDelimiter + "s_area";
                ret = ExecSQL(connDB, trans, sql, true);
                if (!ret.result)
                {
                    trans.Rollback();
                    connDB.Close();
                    return ret;
                }
                sql = "delete from " + Points.Pref + DBManager.sKernelAliasRest + "s_payer";
                ret = ExecSQL(connDB, trans, sql, true);
                if (!ret.result)
                {
                    connDB.Close();
                    return ret;
                }

                sql = " insert into " + Points.Pref + DBManager.sKernelAliasRest + "s_payer  " +
                      " select * from " + dbname + "_kernel" + dbserver + DBManager.sKernelAliasRest + "s_payer";

                ret = ExecSQL(connDB, trans, sql, true);
                if (!ret.result)
                {
                    trans.Rollback();
                    connDB.Close();
                    return ret;
                }
                sql = "delete from " + Points.Pref + DBManager.sKernelAliasRest + "supplier";
                ret = ExecSQL(connDB, trans, sql, true);
                if (!ret.result)
                {
                    trans.Rollback();
                    connDB.Close();
                    return ret;
                }

                sql = "insert into " + Points.Pref + DBManager.sKernelAliasRest + "supplier  " +
                      "select * from " + dbname + "_kernel" + dbserver + DBManager.sKernelAliasRest + "supplier";

                ret = ExecSQL(connDB, trans, sql, true);
                if (!ret.result)
                {
                    trans.Rollback();
                    connDB.Close();
                    return ret;
                }

                trans.Commit();
            }

            return ret;
        }

        //todo Переместить в Adres
        public List<Town> LoadTown(Town finder, out Returns ret)
        {
            //var dbSpravToAdres = new DbSpravToAdres();
            //var listTown = dbSpravToAdres.LoadTown(finder, out ret);
            //return listTown;



            var spis = new List<Town>();

            IDbConnection connDB = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(connDB, true);
            if (!ret.result) return null;

            var tables = new DbTables(connDB);
            string wherePoint = "";
            if (finder.pref.Trim() == "") finder.pref = Points.Pref;
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

                        wherePoint += " AND t.nzp_town  IN (	SELECT DISTINCT	T .nzp_town	FROM "
                                      + Points.Pref + "_data" + DBManager.tableDelimiter + "kvar k, "
                                      + Points.Pref + "_data" + DBManager.tableDelimiter + "dom d, "
                                      + Points.Pref + "_data" + DBManager.tableDelimiter + "s_ulica u, "
                                      + Points.Pref + "_data" + DBManager.tableDelimiter + "s_rajon r, "
                                      + Points.Pref + "_data" + DBManager.tableDelimiter + "s_town T "
                                      + " where k. nzp_wp  IN (" + str + ") AND K .nzp_dom = d.nzp_dom "
                                      + " AND d.nzp_ul = u.nzp_ul	AND u.nzp_raj = r.nzp_raj "
                                      + " AND r.nzp_town = T .nzp_town) ";
                    }
                }
            }

            string fromTables = tables.town + " t, " + tables.rajon + " r, " + tables.dom + " d, " + tables.ulica + " u ";
            string swhere = " and t.nzp_town = r.nzp_town and r.nzp_raj = u.nzp_raj and d.nzp_ul = u.nzp_ul ";
            if (finder.nzp_town > 0) swhere += " and nzp_town = " + finder.nzp_town;
            if (finder.town.Trim() != "") swhere += " and t.town like %'" + finder.town + "'%";
            if (finder.RolesVal != null)
            {
                foreach (_RolesVal role in finder.RolesVal)
                {
                    if (role.tip == Constants.role_sql && role.kod == Constants.role_sql_town)
                        swhere += " and t.nzp_town in (" + role.val + ")";
                }

            }


            //определить количество записей



            //выбрать список
            string sql = "select distinct t.town, r.nzp_town from " + fromTables + " where 1=1 " + swhere + wherePoint + " Order by town ";



            DataTable dt = DBManager.ExecSQLToTable(connDB, sql);

            int countElems = 0;
            try
            {

                int i = 0;
                if (dt != null)
                {
                    foreach (DataRow r in dt.Rows)
                    {
                        i++;
                        if (finder.skip > 0 && finder.skip >= i) continue;
                        var zap = new Town();
                        if (r["nzp_town"] == DBNull.Value) zap.nzp_town = 0;
                        else zap.nzp_town = (int) r["nzp_town"];
                        if (r["town"] == DBNull.Value) zap.town = "";
                        else zap.town = (string) r["town"];
                        spis.Add(zap);
                        if (finder.rows > 0 && i >= finder.skip + finder.rows) break;
                    }
                    countElems = dt.Rows.Count;
                }
                //количество записей
                ret.tag = countElems;
                return spis;
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err;
                if (Constants.Viewerror) err = " \n " + ex.Message;
                else err = "";
                MonitorLog.WriteLog("Ошибка заполнения справочника районов " + err +
                                    Environment.NewLine + " skip = " +
                                    finder.skip + " rows = " +
                                    finder.rows + " " +
                                    " 4)LoadTown: connDB.State : " + connDB.State, MonitorLog.typelog.Error, 20, 201,
                    true);
                return null;
            }
            finally
            {

                connDB.Close();
            }


            //var spis = new List<Town>();

            //IDbConnection connDB = GetConnection(Constants.cons_Kernel);
            //ret = OpenDb(connDB, true);
            //if (!ret.result) return null;

            //var tables = new DbTables(connDB);
            //string wherePoint = "";
            //if (finder.pref.Trim() == "") finder.pref = Points.Pref;
            //if (finder.dopPointList != null)
            //{
            //    if (finder.dopPointList.Count > 0)
            //    {
            //        string str = "";
            //        for (int i = 0; i < finder.dopPointList.Count; i++)
            //        {
            //            if (i == 0) str += finder.dopPointList[i];
            //            else str += ", " + finder.dopPointList[i];
            //        }
            //        if (str != "")
            //        {

            //            wherePoint += " AND t.nzp_town  IN (	SELECT DISTINCT	T .nzp_town	FROM "
            //                + Points.Pref + "_data" + DBManager.tableDelimiter + "kvar k, "
            //                + Points.Pref + "_data" + DBManager.tableDelimiter + "dom d, "
            //                + Points.Pref + "_data" + DBManager.tableDelimiter + "s_ulica u, "
            //                + Points.Pref + "_data" + DBManager.tableDelimiter + "s_rajon r, "
            //                + Points.Pref + "_data" + DBManager.tableDelimiter + "s_town T "
            //                + " where k. nzp_wp  IN (" + str + ") AND K .nzp_dom = d.nzp_dom "
            //                + " AND d.nzp_ul = u.nzp_ul	AND u.nzp_raj = r.nzp_raj "
            //                + " AND r.nzp_town = T .nzp_town) ";
            //        }
            //    }
            //}

            //string fromTables = tables.town + " t, " + tables.rajon + " r, " + tables.dom + " d, " + tables.ulica + " u ";
            //string swhere = " and t.nzp_town = r.nzp_town and r.nzp_raj = u.nzp_raj and d.nzp_ul = u.nzp_ul ";
            //if (finder.nzp_town > 0) swhere += " and nzp_town = " + finder.nzp_town;
            //if (finder.town.Trim() != "") swhere += " and t.town like %'" + finder.town + "'%";
            //if (finder.RolesVal != null)
            //{
            //    foreach (_RolesVal role in finder.RolesVal)
            //    {
            //        if (role.tip == Constants.role_sql && role.kod == Constants.role_sql_town)
            //            swhere += " and t.nzp_town in (" + role.val + ")";
            //    }
            //}


            ////выбрать список
            //string sql = "select distinct t.town, r.nzp_town from " + fromTables + " where 1=1 " + swhere + wherePoint + " Order by town ";


            //MyDataReader reader;
            //if (!ExecRead(connDB, out reader, sql, true).result)
            //{
            //    connDB.Close();
            //    return null;
            //}
            //try
            //{
            //    int i = 0;
            //    while (reader.Read())
            //    {
            //        i++;
            //        if (i <= finder.skip) continue;
            //        var zap = new Town();
            //        if (reader["nzp_town"] == DBNull.Value) zap.nzp_town = 0;
            //        else zap.nzp_town = (int) reader["nzp_town"];
            //        if (reader["town"] == DBNull.Value) zap.town = "";
            //        else zap.town = (string) reader["town"];
            //        spis.Add(zap);
            //        if (finder.rows > 0 && i >= finder.skip + finder.rows) break;
            //    }

            //    reader.Close();

            //    //определить количество записей
            //    sql = "select count(distinct t.nzp_town) from " + fromTables + " where 1=1 " + swhere;
            //    object count = ExecScalar(connDB, sql, out ret, true);
            //    if (ret.result)
            //    {
            //        try
            //        {
            //            ret.tag = Convert.ToInt32(count);
            //        }
            //        catch (Exception ex)
            //        {
            //            connDB.Close();
            //            ret.result = false;
            //            ret.text = ex.Message;
            //            return null;
            //        }
            //    }


            //    return spis;
            //}
            //catch (Exception ex)
            //{
            //    reader.Close();


            //    ret.result = false;
            //    ret.text = ex.Message;

            //    string err;
            //    if (Constants.Viewerror) err = " \n " + ex.Message;
            //    else err = "";
            //    MonitorLog.WriteLog("Ошибка заполнения справочника районов " + err, MonitorLog.typelog.Error, 20, 201,
            //        true);
            //    return null;
            //}
            //finally
            //{
            //    connDB.Close(); 
            //}
        }

        //todo Переместить в Exchange
        public List<_reestr_unloads> LoadUploadedReestrList(Finder finder, out Returns ret)
        {
            var dbReestrTula = new DbReestrTula();
            var spis = dbReestrTula.LoadUploadedReestrList(finder, out ret);
            dbReestrTula.Close();
            return spis;

//            var spis = new List<_reestr_unloads>();
//            MyDataReader reader;
//            IDbConnection connDB = GetConnection(Constants.cons_Kernel);
//            ret = OpenDb(connDB, true);
//            if (!ret.result) return null;

//            string skip = "";
//            string rows = "";


//#if PG
//            if (finder.skip != 0)
//            {
//                skip = " offset " + finder.skip;
//            }
//            if (finder.rows != 0)
//            {
//                rows = " limit " + finder.rows;
//            }
//#else
//            if (finder.skip != 0)
//            {
//                skip = " skip " + finder.skip;
//            }
//            if (finder.rows != 0)
//            {
//                rows = " first " + finder.rows;
//            }
//#endif
//            string sql = "";
//            //выбрать список
//#if PG
//            sql = "select r.*,u.name from " + Points.Pref + "_data.tula_reestr_unloads r left outer join " + Points.Pref +
//            "_data.users u on r.user_unloaded=u.nzp_user where r.is_actual<>100  order by nzp_reestr desc  " + skip + " " + rows + " ";
//#else
//            sql = "select " + skip + " " + rows + " r.*,u.name " +
//                         "from " + Points.Pref + DBManager.sDataAliasRest + "tula_reestr_unloads r left outer join " +
//                         Points.Pref + DBManager.sDataAliasRest + "users u on r.user_unloaded=u.nzp_user " +
//                         " where r.is_actual<>100 order by nzp_reestr desc ";
//#endif
//            if (!ExecRead(connDB, out reader, sql, true).result)
//            {
//                connDB.Close();
//                MonitorLog.WriteLog("Ошибка получения списка выгрузок реестра", MonitorLog.typelog.Error, 20, 201, true);
//                return null;
//            }
//            try
//            {
//                //int i = 0;
//                while (reader.Read())
//                {
//                    var zap = new _reestr_unloads();
//                    if (reader["nzp_reestr"] == DBNull.Value) zap.nzp_reestr = 0;
//                    else zap.nzp_reestr = (int) reader["nzp_reestr"];

//                    zap.name_file = reader["name_file"] == DBNull.Value ? "" : reader["name_file"].ToString().Trim();

//                    if (reader["date_unload"] == DBNull.Value) zap.date_unload = new DateTime().ToShortDateString();
//                    else
//                    {
//                        var datUnload = (DateTime) reader["date_unload"];
//                        zap.date_unload = datUnload.ToShortDateString();
//                    }

//                    zap.unloading_date = reader["unloading_date"] == DBNull.Value
//                        ? ""
//                        : reader["unloading_date"].ToString();

//                    if (reader["user_unloaded"] == DBNull.Value) zap.user_unloaded = 0;
//                    else zap.user_unloaded = (int) reader["user_unloaded"];

//                    zap.name_user_unloaded = reader["name"] == DBNull.Value ? "" : reader["name"].ToString().Trim();

//                    if (reader["nzp_exc"] == DBNull.Value) zap.nzp_exc = 0;
//                    else zap.nzp_exc = (int) reader["nzp_exc"];

//                    spis.Add(zap);

//                }

//                //определить количество записей
//                sql = "select count(*) from " + Points.Pref + DBManager.sDataAliasRest +
//                      "tula_reestr_unloads where is_actual<>100; ";
//                object count = ExecScalar(connDB, sql, out ret, true);
//                if (ret.result)
//                {
//                    try
//                    {
//                        ret.tag = Convert.ToInt32(count);
//                    }
//                    catch (Exception ex)
//                    {
//                        connDB.Close();
//                        ret.result = false;
//                        ret.text = ex.Message;
//                        return null;
//                    }
//                }

//                reader.Close();

//                return spis;
//            }
//            catch (Exception ex)
//            {
//                reader.Close();


//                ret.result = false;
//                ret.text = ex.Message;

//                string err;
//                if (Constants.Viewerror) err = " \n " + ex.Message;
//                else err = "";
//                MonitorLog.WriteLog("Ошибка получения списка выгрузок реестра" + err, MonitorLog.typelog.Error, 20, 201,
//                    true);
//                return null;
//            }
//            finally
//            {
//                connDB.Close();
//            }
        }

        //todo Переместить в Exchange
        public List<_reestr_downloads> LoadDownloadedReestrList(Finder finder, out Returns ret)
        {
            var dbReestrTula = new DbReestrTula();
            var spis = dbReestrTula.LoadDownloadedReestrList(finder, out ret);
            dbReestrTula.Close();
            return spis;

//            var spis = new List<_reestr_downloads>();
//            MyDataReader reader;
//            IDbConnection connDB = GetConnection(Constants.cons_Kernel);
//            ret = OpenDb(connDB, true);
//            if (!ret.result) return null;

//            string skip = "";
//            string rows = "";
//#if PG
//            if (finder.skip != 0)
//            {
//                skip = " offset " + finder.skip;
//            }
//            if (finder.rows != 0)
//            {
//                rows = " limit " + finder.rows;
//            }
//#else
//            if (finder.skip != 0)
//            {
//                skip = " skip " + finder.skip;
//            }
//            if (finder.rows != 0)
//            {
//                rows = " first " + finder.rows;
//            }
//#endif
//            //выбрать список
//            string sql = "";

//#if PG
//            sql = "select  r.*,s.name_type,u.name, b.branch_name from " + Points.Pref + "_data.tula_reestr_downloads r left outer join " + Points.Pref + "_data.users u on r.user_downloaded=u.nzp_user " +
//            "left outer join " + Points.Pref + "_data.tula_reestr_sprav s on r.nzp_type=s.nzp_type left outer join " + Points.Pref + "_data.tula_s_bank b on b.branch_id = r.branch_id  " + skip + " " + rows + "";
//#else
//            sql = "select " + skip + " " + rows + " r.*,s.name_type,u.name, b.branch_name from " +
//                Points.Pref + "_data:tula_reestr_downloads r left outer join " + Points.Pref + "_data:users u on r.user_downloaded=u.nzp_user " +
//                         "left outer join " + Points.Pref + "_data:tula_reestr_sprav s on r.nzp_type=s.nzp_type left outer join " +
//                         Points.Pref + "_data:tula_s_bank b on b.branch_id = r.branch_id  order by nzp_download desc";
//#endif
//            if (!ExecRead(connDB, out reader, sql, true).result)
//            {
//                connDB.Close();
//                MonitorLog.WriteLog("Ошибка получения списка загрузок реестра", MonitorLog.typelog.Error, 20, 201, true);
//                return null;
//            }
//            try
//            {

//                while (reader.Read())
//                {
//                    var zap = new _reestr_downloads();
//                    if (reader["nzp_download"] == DBNull.Value) zap.nzp_download = 0;
//                    else zap.nzp_download = (int) reader["nzp_download"];

//                    zap.file_name = reader["file_name"] == DBNull.Value ? "" : reader["file_name"].ToString().Trim();

//                    zap.name_type = reader["name_type"] == DBNull.Value ? "" : reader["name_type"].ToString().Trim();


//                    if (reader["date_download"] == DBNull.Value) zap.date_download = "";
//                    else
//                    {
//                        var dateDownload = (DateTime) reader["date_download"];
//                        zap.date_download = dateDownload.ToString(CultureInfo.InvariantCulture);
//                    }

//                    if (reader["day"] == DBNull.Value || reader["month"] == DBNull.Value) zap.day_month = "";
//                    else zap.day_month = ((int) reader["day"]) + "/" + ((int) reader["month"]);


//                    zap.branch_name = reader["branch_name"] == DBNull.Value
//                        ? ""
//                        : reader["branch_name"].ToString().Trim();


//                    zap.name_user_downloaded = reader["name"] == DBNull.Value ? "" : reader["name"].ToString().Trim();
//                    spis.Add(zap);

//                }

//                //определить количество записей
//                sql = "select count(*) from " + Points.Pref + DBManager.sDataAliasRest + "tula_reestr_downloads; ";
//                object count = ExecScalar(connDB, sql, out ret, true);
//                if (ret.result)
//                {
//                    try
//                    {
//                        ret.tag = Convert.ToInt32(count);
//                    }
//                    catch (Exception ex)
//                    {
//                        connDB.Close();
//                        ret.result = false;
//                        ret.text = ex.Message;
//                        return null;
//                    }
//                }

//                reader.Close();

//                return spis;
//            }
//            catch (Exception ex)
//            {
//                reader.Close();


//                ret.result = false;
//                ret.text = ex.Message;

//                string err;
//                if (Constants.Viewerror) err = " \n " + ex.Message;
//                else err = "";
//                MonitorLog.WriteLog("Ошибка получения списка выгрузок реестра" + err, MonitorLog.typelog.Error, 20, 201,
//                    true);
//                return null;
//            }
//            finally
//            {
//                connDB.Close();
//            }
        }

        //todo Переместить в Adres
        public List<Land> LoadLand(Land finder, out Returns ret)
        {
            var dbSpravToAdres = new DbSpravToAdres();
            var listLand = dbSpravToAdres.LoadLand(finder, out ret);
            dbSpravToAdres.Close();
            return listLand;

            //if (finder.nzp_user < 0)
            //{
            //    ret = new Returns(false, "Не задан пользователь", -1);
            //    return null;
            //}

            //#region соединение с БД
            //IDbConnection connDB = GetConnection(Constants.cons_Kernel);
            //ret = OpenDb(connDB, true);
            //if (!ret.result) return null;
            //#endregion

            //var tables = new DbTables(connDB);

            //string sql = "select * from " + tables.land + " order by land";
            //MyDataReader reader;
            //ret = ExecRead(connDB, out reader, sql, true);
            //if (!ret.result) return null;

            //var list = new List<Land>();
            //try
            //{
            //    while (reader.Read())
            //    {
            //        var land = new Land();
            //        if (reader["land"] != DBNull.Value) land.land = ((string) reader["land"]).Trim();
            //        if (reader["nzp_land"] != DBNull.Value) land.nzp_land = Convert.ToInt32(reader["nzp_land"]);
            //        list.Add(land);
            //    }
            //    reader.Close();
            //}
            //catch (Exception ex)
            //{
            //    reader.Close();

            //    ret = new Returns(false, ex.Message);
            //    MonitorLog.WriteLog("Ошибка получения информации", MonitorLog.typelog.Error, 20, 201, true);
            //    return null;
            //}
            //finally
            //{
            //    connDB.Close();

            //}

            //return list;
        }

        //todo Переместить в Adres
        public List<Stat> LoadStat(Stat finder, out Returns ret)
        {
            var dbSpravToAdres = new DbSpravToAdres();
            var listStat = dbSpravToAdres.LoadStat(finder, out ret);
            dbSpravToAdres.Close();
            return listStat;

            //if (finder.nzp_user < 0)
            //{
            //    ret = new Returns(false, "Не задан пользователь", -1);
            //    return null;
            //}

            //#region соединение с БД
            //IDbConnection connDB = GetConnection(Constants.cons_Kernel);
            //ret = OpenDb(connDB, true);
            //if (!ret.result) return null;
            //#endregion

            //string where = "";
            //if (finder.nzp_land > 0) where = " and nzp_land = " + finder.nzp_land;

            //var tables = new DbTables(connDB);
            //string sql = "select * from " + tables.stat + " where 1=1 " + where + " order by stat";
            //MyDataReader reader;
            //ret = ExecRead(connDB, out reader, sql, true);
            //if (!ret.result) return null;

            //var list = new List<Stat>();
            //try
            //{
            //    while (reader.Read())
            //    {
            //        var stat = new Stat();
            //        if (reader["stat"] != DBNull.Value) stat.stat = ((string) reader["stat"]).Trim();
            //        if (reader["nzp_stat"] != DBNull.Value) stat.nzp_stat = Convert.ToInt32(reader["nzp_stat"]);
            //        list.Add(stat);
            //    }
            //    reader.Close();
            //}
            //catch (Exception ex)
            //{
            //    reader.Close();

            //    ret = new Returns(false, ex.Message);
            //    MonitorLog.WriteLog("Ошибка получения информации", MonitorLog.typelog.Error, 20, 201, true);
            //    return null;
            //}
            //finally
            //{
            //    connDB.Close();
            //}

            //return list;
        }

        //todo Переместить в Adres
        public List<Town> LoadTown2(Town finder, out Returns ret)
        {
            var dbSpravToAdres = new DbSpravToAdres();
            var listTown = dbSpravToAdres.LoadTown2(finder, out ret);
            dbSpravToAdres.Close();
            return listTown;

            //if (finder.nzp_user < 0)
            //{
            //    ret = new Returns(false, "Не задан пользователь", -1);
            //    return null;
            //}

            //#region соединение с БД
            //IDbConnection connDB = GetConnection(Constants.cons_Kernel);
            //ret = OpenDb(connDB, true);
            //if (!ret.result) return null;
            //#endregion

            //string where = "";
            //if (finder.nzp_stat > 0) where = " and nzp_stat = " + finder.nzp_stat;

            //var tables = new DbTables(connDB);
            //string sql = "select * from " + tables.town + " where 1=1 " + where + " order by town";
            //MyDataReader reader;
            //ret = ExecRead(connDB, out reader, sql, true);
            //if (!ret.result) return null;

            //var list = new List<Town>();
            //try
            //{
            //    while (reader.Read())
            //    {
            //        var town = new Town();
            //        if (reader["town"] != DBNull.Value) town.town = ((string) reader["town"]).Trim();
            //        if (reader["nzp_town"] != DBNull.Value) town.nzp_town = Convert.ToInt32(reader["nzp_town"]);
            //        list.Add(town);
            //    }
            //    reader.Close();
            //}
            //catch (Exception ex)
            //{
            //    reader.Close();

            //    ret = new Returns(false, ex.Message);
            //    MonitorLog.WriteLog("Ошибка получения информации", MonitorLog.typelog.Error, 20, 201, true);
            //    return null;
            //}
            //finally
            //{
            //    connDB.Close();
            //}

            //return list;
        }

        //todo Переместить в Adres
        public List<Rajon> LoadRajon(Rajon finder, out Returns ret)
        {
            var dbSpravToAdres = new DbSpravToAdres();
            var listRajon = dbSpravToAdres.LoadRajon(finder, out ret);
            dbSpravToAdres.Close();
            return listRajon;
            //if (finder.nzp_user < 0)
            //{
            //    ret = new Returns(false, "Не задан пользователь", -1);
            //    return null;
            //}

            //#region соединение с БД
            //IDbConnection connDB = GetConnection(Constants.cons_Kernel);
            //ret = OpenDb(connDB, true);
            //if (!ret.result) return null;
            //#endregion

            //string where = "";
            //if (finder.nzp_town > 0) where = " and nzp_town = " + finder.nzp_town;

            //var tables = new DbTables(connDB);
            //string sql = "select * from " + tables.rajon + " where 1=1 " + where + " order by rajon";
            //MyDataReader reader;
            //ret = ExecRead(connDB, out reader, sql, true);
            //if (!ret.result) return null;

            //var list = new List<Rajon>();
            //try
            //{
            //    while (reader.Read())
            //    {
            //        var rajon = new Rajon();
            //        if (reader["rajon"] != DBNull.Value) rajon.rajon = ((string) reader["rajon"]).Trim();
            //        if (reader["nzp_raj"] != DBNull.Value) rajon.nzp_raj = Convert.ToInt32(reader["nzp_raj"]);
            //        list.Add(rajon);
            //    }
            //    reader.Close();
            //}
            //catch (Exception ex)
            //{
            //    reader.Close();

            //    ret = new Returns(false, ex.Message);
            //    MonitorLog.WriteLog("Ошибка получения информации", MonitorLog.typelog.Error, 20, 201, true);
            //    return null;
            //}
            //finally
            //{
            //    connDB.Close();
            //}

            //ret.tag = list.Count;
            //if (finder.skip > 0 && list.Count > finder.skip) list.RemoveRange(0, finder.skip);
            //if (finder.rows > 0 && list.Count > finder.rows) list.RemoveRange(finder.rows, list.Count - finder.rows);


            //return list;
        }

        //todo переместить в Exchange
        public Returns DeleteReestrTula(_reestr_unloads finder)
        {

            var dbReestrTula = new DbReestrTula();
            var ret = dbReestrTula.DeleteReestrTula(finder);
            dbReestrTula.Close();
            return ret;
            //Returns ret;
            //if (finder.nzp_user < 0)
            //{
            //    ret = new Returns(false, "Не задан пользователь", -1) { result = false };
            //    return ret;
            //}
            //IDbConnection connDB = GetConnection(Constants.cons_Kernel);
            //ret = OpenDb(connDB, true);
            //if (!ret.result) return ret;

            //if (!ExecSQL(connDB, " update " + Points.Pref + DBManager.sDataAliasRest + "tula_reestr_unloads set is_actual=100 " +
            //                      " where nzp_reestr=" + finder.nzp_reestr, true).result)
            //{
            //    ret.result = false;
            //    ret.text = "Ошибка обновления реестра для Тулы";
            //}
            //return ret;
        }

        //todo переместить в Exchange
        public Returns DeleteDownloadReestrTula(Finder finder, int nzpDownload)
        {
            var dbReestrTula = new DbReestrTula();
            var ret = dbReestrTula.DeleteDownloadReestrTula(finder, nzpDownload);
            dbReestrTula.Close();
            return ret;

        }

        //todo переместить в Exchange
        public Returns DeleteDownloadReestrMariyEl(Finder finder, int nzpDownload)
        {
            var dbReestrTula = new DbReestrTula();
            var ret = dbReestrTula.DeleteDownloadReestrMariyEl(finder, nzpDownload);
            dbReestrTula.Close();
            return ret;

        }

        //todo переместить в Exchange
        public List<unload_exchange_sz> LoadListExchangeSZ(Finder finder, out Returns ret)
        {
            if (finder.nzp_user < 0)
            {
                ret = new Returns(false, "Не задан пользователь", -1);
                return null;
            }

            var spis = new List<unload_exchange_sz>();
            MyDataReader reader;
            IDbConnection connDB = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(connDB, true);
            if (!ret.result) return null;

            string skip = string.Empty;
            string rows = string.Empty;
            string skipRowsBegin, skipRowsEnd;
            string prefData = Points.Pref + DBManager.sDataAliasRest,
                prefKernel = Points.Pref + DBManager.sKernelAliasRest;

#if PG
            if (finder.skip != 0)
            {
                skip = " offset " + finder.skip;
            }
            if (finder.rows != 0)
            {
                rows = " limit " + finder.rows;
            }
            skipRowsBegin = string.Empty;
            skipRowsEnd = skip + " " + rows;
#else
			if (finder.skip != 0)
			{
				skip = " skip " + finder.skip;
			}
			if (finder.rows != 0)
			{
				rows = " first " + finder.rows;
			}
			skipRowsBegin = skip + " " + rows;
			skipRowsEnd = string.Empty;
#endif
            //выбрать список
            string sql = " SELECT " + skipRowsBegin + " r.*," +
                         " u.name " +
                         " FROM " + prefData + "tula_ex_sz r LEFT OUTER JOIN " + prefData + "users u ON r.nzp_user = u.nzp_user " +
                         " LEFT OUTER JOIN " + prefKernel + "s_point p ON r.nzp_wp = p.nzp_wp " +
                         " ORDER BY nzp_ex_sz desc " + skipRowsEnd;

            ret = ExecRead(connDB, out reader, sql, true);
            if (!ret.result)
            {
                connDB.Close();
                MonitorLog.WriteLog("Ошибка получения списка выгрузок реестра", MonitorLog.typelog.Error, 20, 201, true);
                return null;
            }
            try
            {
                while (reader.Read())
                {
                    var zap = new unload_exchange_sz
                    {
                        nzp_ex_sz = reader["nzp_ex_sz"] != DBNull.Value ? Convert.ToInt32(reader["nzp_ex_sz"]) : 0,
                        file_name = reader["file_name"] != DBNull.Value ? reader["file_name"].ToString().Trim() : string.Empty,
                        dat_upload = reader["dat_upload"] != DBNull.Value
                            ? Convert.ToDateTime(reader["dat_upload"]).ToString("yyyy.MM.dd HH:mm:ss")
                            : new DateTime().ToShortDateString(),
                        name_user_unloaded = reader["name"] != DBNull.Value ? reader["name"].ToString().Trim() : string.Empty,
                        proc = reader["proc"] != DBNull.Value ? Convert.ToDouble(reader["proc"])*100d : 0d
                    };

                    sql = " SELECT TRIM(point) AS point " +
                          " FROM " + prefData + "tula_ex_sz_wp t INNER JOIN " + prefKernel + "s_point p ON p.nzp_wp = t.nzp_wp " +
                          " WHERE t.nzp_ex_sz = " + zap.nzp_ex_sz;
                    DataTable tableSzWp = DBManager.ExecSQLToTable(connDB, sql);
                    foreach (DataRow row in tableSzWp.Rows)
                        zap.point += row["point"] != DBNull.Value ? row["point"] + ", " : string.Empty;
                    zap.point = zap.point != null ? zap.point.TrimEnd(' ', ',') : string.Empty;

                    spis.Add(zap);
                }
                reader.Close();

                //определить количество записей
                sql = "SELECT count(*) FROM " + prefData + "tula_ex_sz ";
                object count = ExecScalar(connDB, sql, out ret, true);
                ret.tag = Convert.ToInt32(count);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = "Ошибка загрузки списка файлов";
                MonitorLog.WriteLog("Ошибка получения списка файлов обмена:\n" + ex.Message, MonitorLog.typelog.Error, 20, 201,
                    true);
                spis = null;
            }
            finally
            {
                if (connDB != null) connDB.Close();
                if (reader != null) reader.Close();
            }
            return spis;
        }

        //todo переместить в Exchange
        public List<BCTypes> LoadBCTypes(BCTypes finder, out Returns ret)
        {
            if (finder.nzp_user < 0)
            {
                ret = new Returns(false, "Не задан пользователь", -1);
                return null;
            }

            MyDataReader reader;
            IDbConnection connDB = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(connDB, true);
            if (!ret.result) return null;

            var tables = new DbTables(connDB);

            if (!TempTableInWebCashe(connDB, tables.bc_types))
            {
                ret = new Returns(false, "Нет таблицы в БД", -1);
                MonitorLog.WriteLog("Ошибка получения списка: Формат Банк-клиент " + ret.text, MonitorLog.typelog.Error, 20, 201, true);
                return null;
            }

            string where = "";
            if (finder.id > 0) where += " and id = " + finder.id;
            if (finder.is_active > 0) where += " and is_active = " + finder.is_active;

            string sql = "select * from " + Points.Pref + "_kernel" + tableDelimiter + "bc_types where 1=1 " + where;

            if (!ExecRead(connDB, out reader, sql, true).result)
            {
                connDB.Close();
                MonitorLog.WriteLog("Ошибка получения списка: Формат Банк-клиент ", MonitorLog.typelog.Error, 20, 201, true);
                return null;
            }
            var spis = new List<BCTypes>();
            try
            {
                while (reader.Read())
                {
                    var zap = new BCTypes();
                    if (reader["id"] != DBNull.Value) zap.id = (int) reader["id"];
                    if (reader["name_"] != DBNull.Value) zap.name_ = reader["name_"].ToString().Trim();
                    if (reader["is_active"] != DBNull.Value) zap.is_active = Convert.ToInt32(reader["is_active"]);
                    spis.Add(zap);
                }

                reader.Close();

                return spis;
            }
            catch (Exception ex)
            {
                if (reader != null) reader.Close();


                ret.result = false;
                ret.text = ex.Message;

                string err;
                if (Constants.Viewerror) err = " \n " + ex.Message;
                else err = "";
                MonitorLog.WriteLog("Ошибка получения списка: Формат Банк-клиент" + err, MonitorLog.typelog.Error, 20,
                    201, true);
                return null;
            }
            finally
            {
                connDB.Close();
            }
        }

        public Returns MergeContr(Payer finder, List<int> list)
        {
            Returns ret = Utils.InitReturns();

            if (finder.nzp_user < 0)
            {
                ret = new Returns(false, "Не задан пользователь", -1);
                return ret;
            }

            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return ret;

            try
            {

//                string copies = String.Join(",", list.AsEnumerable().Select(x => x.ToString()));

//                string sql; 
//#if PG
//                sql =
//                    " SELECT table_schema as schema, table_name as tab, column_name as col" +
//                    " FROM information_schema" + tableDelimiter + "columns" +
//                    " WHERE column_name like 'nzp_payer%'";
//#else
//                sql = " SELECT dbname" +
//                      " FROM " + Points.Pref + sKernelAliasRest + "s_baselist";
//                DataTable dbname = ClassDBUtils.OpenSQL(sql, conn_db).resultData;
//                List<string> unoin_str = new List<string>();
//                foreach (DataRow r in dbname.Rows)
//                {
//                    unoin_str.Add(
//                        " SELECT " + r["dbname"] + "as schema, t.tabname as tab, c.colname as col" +
//                        " FROM " + r["dbname"] + tableDelimiter + "systables t, " +
//                        r["dbname"] + tableDelimiter + "syscolumns c" +
//                        " WHERE t.tabid = c.tabid" +
//                        " AND c.colname matches 'nzp_payer*'");
//                }
//                sql = String.Join(" unoin ", unoin_str);
//#endif
//                DataTable dt = ClassDBUtils.OpenSQL(sql, conn_db).resultData;
//                foreach (DataRow r in dt.Rows)
//                {
//                    sql =
//                        " UPDATE " + r["schema"] + tableDelimiter + r["tab"] + 
//                        " SET " + r["col"] + " = " + finder.nzp_payer +
//                        " WHERE " + r["col"] + " in " + copies;
//                    ret = ExecSQL(conn_db, sql, true);
//                    if (!ret.result)
//                    {
//                        ret.text = "Ошибка объединения контрагентов, смотрите лог ошибок";
//                        ret.tag = -1;
//                    }
//                }

//#if PG
//                sql =
//                    " SELECT distinct table_schema as schema" +
//                    " FROM information_schema" + tableDelimiter + "tables" +
//                    " WHERE table_name = 's_payer'";
//#else
//                sql = " SELECT dbname" +
//                      " FROM " + Points.Pref + sKernelAliasRest + "s_baselist";
//                dbname = ClassDBUtils.OpenSQL(sql, conn_db).resultData;
//                unoin_str = new List<string>();
//                foreach (DataRow r in dbname.Rows)
//                {
//                    unoin_str.Add(
//                        " SELECT " + r["dbname"] + "as schema" +
//                        " FROM " + r["dbname"] + tableDelimiter + "systables t " +
//                        " WHERE t.tabname matches 's_payer'");
//                }
//                sql = String.Join(" unoin ", unoin_str);
//#endif
//                dt = ClassDBUtils.OpenSQL(sql, conn_db).resultData;
//                foreach (DataRow r in dt.Rows)
//                {
//                    sql =
//                        " DELETE FROM " + r["schema"] + tableDelimiter + "s_payer" +
//                        " WHERE nzp_payer IN " + copies;
//                    ret = ExecSQL(conn_db, sql, true);
//                    if (!ret.result)
//                    {
//                        ret.text = "Ошибка удаления копий конрагентов";
//                        ret.tag = -1;
//                    }
//                }
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = "Ошибка объединения контрагентов";
                ret.tag = -1;

                MonitorLog.WriteLog("Ошибка объединения контрагентов\n " + ex.Message + " " + ex.StackTrace, MonitorLog.typelog.Error, 20,
                    201, true);
            }

            return ret;
        }

        public List<Supplier> LoadDogovorByPoints(Finder finder, out Returns ret)
        {
            List<Supplier> list = new List<Supplier>();
            if (finder.nzp_user < 0)
            {
                ret = new Returns(false, "Не задан пользователь", -1);
                return list;
            }
            MyDataReader reader;
            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return list;
            string whereNzpWpList;
            if (finder.dopFind != null && finder.dopFind.Count > 0)
            {
                whereNzpWpList = "and nzp_wp in(";
                for (int i = 0; i < finder.dopFind.Count; i++)
                {
                    whereNzpWpList += (i == (finder.dopFind.Count - 1)) ? finder.dopFind[i] + ")" : finder.dopFind[i] + " ,";
                }
            }
            else
            {
                whereNzpWpList = "";
            }
            string sql = "SELECT nzp_fd, nzp_payer_agent||'/'||nzp_payer_princip as name_supp from " + Points.Pref + sDataAliasRest + "fn_dogovor fd where nzp_fd in " +
                         "(select nzp_dog from " + Points.Pref + sKernelAliasRest + "dogovor_point where 1=1 " + whereNzpWpList + ")";
            ret = ExecRead(conn_db, out reader, sql, true);
            if (!ret.result)
            {
                conn_db.Close();
                MonitorLog.WriteLog("Ошибка в методе LoadDogovorByPoints() ", MonitorLog.typelog.Error, 20, 201, true);
                return list;
            }
            try
            {
                while (reader.Read())
                {
                    var supp = new Supplier();
                    if (reader["nzp_fd"] != DBNull.Value) supp.nzp_supp = (int) reader["nzp_fd"];
                    if (reader["name_supp"] != DBNull.Value) supp.name_supp = reader["name_supp"].ToString().Trim();
                    list.Add(supp);
                }
                reader.Close();
                conn_db.Close();
                return list;
            }
            catch (Exception ex)
            {
                if (reader != null) reader.Close();
                ret.text = ex.Message;
                ret.result = false;
                string err;
                if (Constants.Viewerror) err = " \n " + ex.Message;
                else err = "";
                MonitorLog.WriteLog("Ошибка получения списка договоров в методе LoadDogovorByPoints() " + err, MonitorLog.typelog.Error, 20,
                    201, true);
                return list;
            }
            finally
            {
                conn_db.Close();
            }

        }
        public List<Finder> LoadPointsByScopeDogovor(ScopeAdress finder, out Returns ret)
        {
            List<Finder> list = new List<Finder>();
            ret = Utils.InitReturns();
            if (finder.nzp_user < 0)
            {
                ret.result = false;
                ret.text = "Не задан пользователь";
                ret.tag = -1;
                return list;
            }
            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return list;

            MyDataReader reader;
            string sql = "select distinct fs.nzp_wp, sp.point from " + Points.Pref + sDataAliasRest + "fn_scope_adres fs, " + Points.Pref + sKernelAliasRest + "s_point sp " +
                         " where nzp_scope=" + finder.parent_nzp_scope + " and fs.nzp_wp=sp.nzp_wp";
            ret = ExecRead(conn_db, out reader, sql, true);
            if (!ret.result)
            {
                conn_db.Close();
                MonitorLog.WriteLog("Ошибка в методе LoadAdressForSelectedDogovor() ", MonitorLog.typelog.Error, 20, 201, true);
                return list;
            }
            try
            {
                while (reader.Read())
                {
                    var fnd = new Finder();
                    if (reader["nzp_wp"] != DBNull.Value) fnd.nzp_wp = (int)reader["nzp_wp"];
                    if (reader["point"] != DBNull.Value) fnd.point = reader["point"].ToString();
                    list.Add(fnd);
                }
                reader.Close();
                return list;
            }
            catch (Exception ex)
            {
                if (reader != null) reader.Close();
                ret.text = ex.Message;
                ret.result = false;
                return list;
            }
            finally
            {
                conn_db.Close();
            }
        }

        public RecordMonth GetCalcMonth()
        {
            return Points.CalcMonth;
        }

        public List<PrmTypes> LoadKodSum(Finder finder, out Returns ret)
        {
            List<PrmTypes> list = new List<PrmTypes>();
            ret = Utils.InitReturns();
             
            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return list;

            try
            {
                string sql = 
                    " SELECT kod, comment " +
                    " FROM " + Points.Pref + sKernelAliasRest + "kodsum" +
                    " WHERE kod in (" + String.Join(", ", finder.dopFind) + ")";
                DataTable dt = DBManager.ExecSQLToTable(conn_db, sql);
                if (dt.Rows.Count != finder.dopFind.Count)
                {
                    ret.text = "Количество найденных видов оплат " + dt.Rows.Count +
                        " не совпадает с количеством, необходимым для отображения " + finder.dopFind.Count;
                    ret.result = false;
                }
                foreach (DataRow row in dt.Rows)
                {
                    list.Add(new PrmTypes(){id = Convert.ToInt32(row["kod"]), type_name = row["comment"].ToString().Trim()});
                }
            }
            catch(Exception ex)
            {
                MonitorLog.WriteLog("Ошибка  " + System.Reflection.MethodBase.GetCurrentMethod().Name +
                    "\n " + ex.Message + ex.StackTrace, MonitorLog.typelog.Error, true);
                ret.text = ex.Message;
                ret.result = false;
                return list;
            }
            finally
            {
                conn_db.Close();
            }

            return list;
        }

        public List<PrmTypes> GetListNzpCntServ(Finder finder, out Returns ret)
        {
            List<PrmTypes> list = new List<PrmTypes>();

            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return list;

            try
            {
                string s_counts = finder.pref + "_kernel" + tableDelimiter + "s_counts";
                string s_countsdop = finder.pref + "_kernel" + tableDelimiter + "s_countsdop";
                string sql =
                    " SELECT nzp_cnt, nzp_serv FROM " + s_counts + " " + 
                    " union all" +
                    " select nzp_cnt, nzp_serv FROM " + s_countsdop + " ";
                DataTable dt = DBManager.ExecSQLToTable(conn_db, sql);
                if (dt.Rows.Count == 0)
                {
                    return list;
                }
                foreach (DataRow row in dt.Rows)
                {
                    list.Add(new PrmTypes { id = Convert.ToInt32(row["nzp_cnt"]), type_name = row["nzp_serv"].ToString() });
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка  " + System.Reflection.MethodBase.GetCurrentMethod().Name +
                    "\n " + ex.Message + ex.StackTrace, MonitorLog.typelog.Error, true);
                ret.text = ex.Message;
                ret.result = false;
                return list;
            }
            finally
            {
                conn_db.Close();
            }

            return list;
        }

        /// <summary>
        /// сохранение договоров ЖКУ, которые будут участвовать в управлении переплатами
        /// </summary>
        /// <param name="finder"></param>
        /// <param name="oper"></param>
        /// <param name="list"></param>
        /// <returns></returns>
        public Returns SaveContractAllowOv(Finder finder, enSrvOper oper, List<ContractClass> list)
        {
            Returns ret = Utils.InitReturns();

            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return ret;

            try
            {
                string sql;
                foreach (var c in list)
                {
                    sql = 
                        " UPDATE " + Points.Pref + DBManager.sKernelAliasRest + "supplier" +
                        " SET allow_overpayments = " + c.allow_overpayments + ", changed_on = " + DBManager.sCurDateTime + 
                        " WHERE nzp_supp = " + c.nzp_supp;
                    ret.result = ret.result && ExecSQL(conn_db, sql).result;
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка  " + System.Reflection.MethodBase.GetCurrentMethod().Name +
                    "\n " + ex.Message + ex.StackTrace, MonitorLog.typelog.Error, true);
                ret.text = ex.Message;
                ret.result = false;
            }
            finally
            {
                conn_db.Close();
            }

            return ret;
        }

        public Returns SaveSelectedOverpaymentForDistrib(OverpaymentForDistrib finder, List<OverpaymentForDistrib> list)
        {
            Returns ret = Utils.InitReturns();
            if(!finder.only_negative && !finder.only_positive) return new Returns(false, "Невозможно определить тип выделенных договоров", -1);

            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return ret;

            try
            {
                string sql;

                string where = "";
                if (finder.only_positive) where += " AND sum_outsaldo > 0 ";
                if (finder.only_negative) where += " AND sum_outsaldo < 0 ";

                if (finder.all_selected>0)
                {
                    sql =
                        " UPDATE " + Points.Pref + sDataAliasRest + "joined_overpayments" +
                        " SET mark = " + (finder.all_selected==1?"true":"false") +
                        " WHERE 1=1 " + where;
                    ret = ExecSQL(conn_db, sql);
                }
                else
                {
                    foreach (var c in list)
                    {

                        sql =
                            " UPDATE " + Points.Pref + sDataAliasRest + "joined_overpayments" +
                            " SET mark = " + c.mark +
                            " WHERE id = " + c.id + where;
                        ret.result = ret.result && ExecSQL(conn_db, sql).result;
                    }
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка  " + System.Reflection.MethodBase.GetCurrentMethod().Name +
                    "\n " + ex.Message + ex.StackTrace, MonitorLog.typelog.Error, true);
                ret.text = ex.Message;
                ret.result = false;
            }
            finally
            {
                conn_db.Close();
            }

            return ret;
        }

        public List<House_kodes> GetAliasDomList(House_kodes finder, out Returns ret)
        {
            if (finder.nzp_dom < 0)
            {
                ret = new Returns(false, "Не выбран дом", -1);
                return new List<House_kodes>();
            }
            if (finder.pref == "")
            {
                ret = new Returns(false, "Не определен префикс БД", -1);
                return new List<House_kodes>();
            }
            var list = new List<House_kodes>();

            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return list;

            try
            {
                string where = "";
                if (finder.nzp_payer > 0) where += " AND a.nzp_payer = " + finder.nzp_payer + " ";
                if (finder.id > 0) where += " AND a.id = " + finder.id + " ";

                string alias_dom = finder.pref + sDataAliasRest + "alias_dom";
                
                string sql = " SELECT a.id, a.nzp_dom, a.kod_dom, a.nzp_payer, a.comment," +
                    " a.nzp_user, a.dat_when, p.payer, u.comment as user ";
                string sql2 = " FROM " + alias_dom + " a " +
                    " LEFT OUTER JOIN " + Points.Pref + sKernelAliasRest + "s_payer p ON p.nzp_payer = a.nzp_payer" +
                    " LEFT OUTER JOIN " + Points.Pref + sDataAliasRest + "users u ON u.nzp_user = a.nzp_user " +
                    " WHERE is_actual <> 100 AND a.nzp_dom = " + finder.nzp_dom + " " + where;

                object count = ExecScalar(conn_db, " SELECT count(distinct a.id) " + sql2, out ret, true);
                int recordsTotalCount;
                try { recordsTotalCount = Convert.ToInt32(count); }
                catch (Exception e)
                {
                    ret = new Returns(false, "Ошибка при определении количества записей: " + (Constants.Debug ? e.Message : ""));
                    MonitorLog.WriteLog("Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name +
                         (Constants.Viewerror ? "\n" + e.Message : ""), MonitorLog.typelog.Error, 20, 201, true);
                    conn_db.Close();
                    return null;
                }


                DataTable dt = DBManager.ExecSQLToTable(conn_db, sql + sql2 + " ORDER BY p.payer");
                if (dt.Rows.Count == 0)
                {
                    return list;
                }
                int i = 0;
                foreach (DataRow row in dt.Rows)
                {
                    i++;
                    if (i <= finder.skip) continue;
                    var h = new House_kodes();
                    if (row["id"] != null) h.id = Convert.ToInt32(row["id"]);
                    if (row["nzp_dom"] != null) h.nzp_dom = Convert.ToInt32(row["nzp_dom"]);
                    if (row["kod_dom"] != null) h.kod_dom = Convert.ToString(row["kod_dom"]).Trim();
                    if (row["nzp_payer"] != null) h.nzp_payer = Convert.ToInt32(row["nzp_payer"]);
                    if (row["comment"] != null) h.comment = Convert.ToString(row["comment"]).Trim();
                    if (row["nzp_user"] != null) h.nzp_user = Convert.ToInt32(row["nzp_user"]);
                    if (row["dat_when"] != null) h.dat_when = Convert.ToDateTime(row["dat_when"]).ToShortDateString();
                    if (row["payer"] != null) h.payer = Convert.ToString(row["payer"]).Trim();
                    if (row["user"] != null) h.user = Convert.ToString(row["user"]).Trim();
                    h.num = i;
                    list.Add(h); 
                    if (finder.rows > 0 && i >= finder.skip + finder.rows) break;
                }

                if (ret.result) ret.tag = recordsTotalCount;
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка  " + System.Reflection.MethodBase.GetCurrentMethod().Name +
                    "\n " + ex.Message + ex.StackTrace, MonitorLog.typelog.Error, true);
                ret.text = ex.Message;
                ret.result = false;
                return list;
            }
            finally
            {
                conn_db.Close();
            }

            return list;
        }

        public Returns CheckToOpenServOnLSByAdress(List<ScopeAdress> scopeAdressList)
        {
            Returns ret = Utils.InitReturns();
            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
            List<int> orderNumsRows= new List<int>();
            try
            {
                ret = OpenDb(conn_db, true);
                if (!ret.result)
                {
                    throw new InvalidOperationException("Ошибка подключения к базе данных");
                }

                // извлечь адреса, которые будут удаляться
                DbScopeAddress db = new DbScopeAddress();
               
                //для каждого удаляемого адреса проверим наличие ЛС с открытыми услугами
                foreach (ScopeAdress adress in scopeAdressList)
                {
                    // т.к. с интерфейса приходит список первичных ключей таблицы, из которой будут удаляться адреса, то сначала нужно
                    //извлечь адреса, которые будут удаляться
                    string sql = "select * from " + Points.Pref + sDataAliasRest + "fn_scope_adres " +
                            "where nzp_scope_adres ="+ adress.nzp_scope_adres;
                    List<ScopeAdress> listAddresseToDelete = db.getScopeAdresses(conn_db, out ret, sql);
                    if (!ret.result)
                    {
                        throw new InvalidOperationException("Ошибка получения списка адресов для удаления");
                    }
                    if (listAddresseToDelete.Count == 0)
                    {
                        continue;
                    }
                    var deletedAdress = listAddresseToDelete.First();
                    string pref = Points.GetPref(deletedAdress.nzp_wp);
                    string localTarifTable = pref + sDataAliasRest + "tarif";
                    RecordMonth rm = Points.GetCalcMonth(new CalcMonthParams(pref));
                    // т.к. услуга может быть открыта как в первой половине месяца, так и во второй
                    // то нужно проверять по двум датам из начала и конца месяца
                    string firstDayMonth = rm.RecordDateTime.ToShortDateString();
                    string lastDayMonth = new DateTime(rm.RecordDateTime.Year, rm.RecordDateTime.Month, 
                        DateTime.DaysInMonth(rm.RecordDateTime.Year, rm.RecordDateTime.Month)).ToShortDateString();
                    // основное тело запроса
                    string mainSql = "select 1 from " + localTarifTable + " t, " + Points.Pref + sDataAliasRest + "kvar k," +
                                     Points.Pref + sDataAliasRest + "dom d, " + Points.Pref + sDataAliasRest + "s_ulica u, " +
                                     Points.Pref + sDataAliasRest + "s_rajon r, " + Points.Pref + sDataAliasRest + " s_town tw" +
                                     " where t.is_actual<>100 and t.nzp_kvar=k.nzp_kvar " +
                                     "and (('" + firstDayMonth + "' between t.dat_s and t.dat_po) or ('" + lastDayMonth + "' between t.dat_s and t.dat_po)) " +
                                     "and t.nzp_supp="+adress.nzp_supp+" and k.nzp_dom=d.nzp_dom and d.nzp_ul=u.nzp_ul and u.nzp_raj=r.nzp_raj and r.nzp_town= tw.nzp_town ";
                    // список колонок и значений для условия where (условие для банка будет всегда)
                    Dictionary<string, int> whereDict = new Dictionary<string, int> { { "and k.nzp_wp", deletedAdress.nzp_wp } };
                    // в зависимости от глубины адреса, сформировать условие where
                    switch (deletedAdress.ScopeLevel)
                    {
                        case ScopeAdress.ScopeUntil.Town:
                            whereDict.Add("tw.nzp_town", deletedAdress.nzp_town);
                            break;
                        case ScopeAdress.ScopeUntil.Rajon:
                            whereDict.Add("tw.nzp_town", deletedAdress.nzp_town);
                            whereDict.Add("r.nzp_raj", deletedAdress.nzp_raj);
                            break;
                        case ScopeAdress.ScopeUntil.Ulica:
                            whereDict.Add("tw.nzp_town", deletedAdress.nzp_town);
                            whereDict.Add("r.nzp_raj", deletedAdress.nzp_raj);
                            whereDict.Add("u.nzp_ul", deletedAdress.nzp_ul);
                            break;
                        case ScopeAdress.ScopeUntil.Dom:
                            whereDict.Add("d.nzp_dom", deletedAdress.nzp_dom);
                            break;
                    }
                    string where = String.Join(" and ", whereDict.Select(d => d.Key + "=" + d.Value));
                    // основной запрос
                    mainSql = "select exists (" + mainSql + where + ")";
                    bool result =(bool) ExecScalar(conn_db, mainSql, out ret, true);
                    if (!ret.result)
                    {
                        throw new InvalidOperationException("Ошибка проверки наличия открытых услуг по ЛС для указанных адресов");
                    }
                    if (result)
                    {
                        // добавление порядкового номера строки адреса для отображения в сообщении пользователю
                        orderNumsRows.Add(adress.order_num);
                    }
                }
                if (orderNumsRows.Count > 0)
                {
                    ret.tag = -1;
                    ret.text = "Удаление невозможно , т.к. адрес(а) в строке(ах) с порядковым(и) номером(ами) " + String.Join(", ", orderNumsRows) +
                               " имеет(ют) лицевые счета с действующими услугами";
                }
            }
            catch (InvalidOperationException ex)
            {
                MonitorLog.WriteLog("Ошибка  " + System.Reflection.MethodBase.GetCurrentMethod().Name +
                                    "\n " + ex.Message + ex.StackTrace, MonitorLog.typelog.Error, true);
                ret.tag = -1;
                ret.text = ex.Message;
                ret.result = false;
                return ret;
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка  " + System.Reflection.MethodBase.GetCurrentMethod().Name +
                                    "\n " + ex.Message + ex.StackTrace, MonitorLog.typelog.Error, true);
                ret.text = ex.Message;
                ret.result = false;
                return ret;
            }
            finally
            {
                if (conn_db != null)
                {
                    conn_db.Close();
                }
            }
            return ret;
        }

    }
}
