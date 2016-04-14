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
    public partial class DbSprav : DbSpravKernel
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
#if PG
                string sql = "select dbname, dbserver from " + Points.Pref + "_kernel.s_baselist where idtype = 10";
#else
                string sql = "select dbname, dbserver from " + Points.Pref + "_kernel:s_baselist where idtype = 10";
#endif
                string connectionString = Points.GetConnByPref(Points.Pref);
                IDbConnection conn_db = GetConnection(connectionString);
                ret = OpenDb(conn_db, true);
                if (!ret.result) return ret;

                IDataReader reader = null;
                ret = ExecRead(conn_db, out reader, sql, true);
                if (!ret.result)
                {
                    conn_db.Close();
                    return ret;
                }
                string dbname = "", dbserver = "";
                try
                {
                    if (reader.Read())
                    {
                        if (reader["dbname"] != DBNull.Value) dbname = Convert.ToString(reader["dbname"]).Trim();
                        if (reader["dbserver"] != DBNull.Value) dbserver = Convert.ToString(reader["dbserver"]).Trim();
                    }
                }
                catch (Exception ex)
                {
                    MonitorLog.WriteLog("Ошибка UpdateSpravTables() " + (Constants.Viewerror ? "\n " + ex.Message : ""), MonitorLog.typelog.Error, 20, 201, true);
                    return ret;
                }

                IDbTransaction trans = conn_db.BeginTransaction();
#if PG
                sql = "delete from " + Points.Pref + "_data.s_area";
#else
                sql = "delete from " + Points.Pref + "_data:s_area";
#endif
                ret = ExecSQL(conn_db, trans, sql, true);
                if (!ret.result)
                {
                    trans.Rollback();
                    conn_db.Close();
                    return ret;
                }
#if PG
                sql = "insert into " + Points.Pref + "_data.s_area  select * from " + dbname + "_data@" + dbserver + ".s_area";
#else
                sql = "insert into " + Points.Pref + "_data:s_area  select * from " + dbname + "_data@" + dbserver + ":s_area";
#endif
                ret = ExecSQL(conn_db, trans, sql, true);
                if (!ret.result)
                {
                    trans.Rollback();
                    conn_db.Close();
                    return ret;
                }
#if PG
                sql = "delete from " + Points.Pref + "_kernel.s_payer";
#else
                sql = "delete from " + Points.Pref + "_kernel:s_payer";
#endif
                ret = ExecSQL(conn_db, trans, sql, true);
                if (!ret.result)
                {
                    conn_db.Close();
                    return ret;
                }
#if PG
                sql = "insert into " + Points.Pref + "_kernel.s_payer  select * from " + dbname + "_kernel@" + dbserver + ".s_payer";
#else
                sql = "insert into " + Points.Pref + "_kernel:s_payer  select * from " + dbname + "_kernel@" + dbserver + ":s_payer";
#endif
                ret = ExecSQL(conn_db, trans, sql, true);
                if (!ret.result)
                {
                    trans.Rollback();
                    conn_db.Close();
                    return ret;
                }
#if PG
                sql = "delete from " + Points.Pref + "_kernel.supplier";
#else
                sql = "delete from " + Points.Pref + "_kernel:supplier";
#endif
                ret = ExecSQL(conn_db, trans, sql, true);
                if (!ret.result)
                {
                    trans.Rollback();
                    conn_db.Close();
                    return ret;
                }
#if PG
                sql = "insert into " + Points.Pref + "_kernel.supplier  select * from " + dbname + "_kernel@" + dbserver + ".supplier";
#else
                sql = "insert into " + Points.Pref + "_kernel:supplier  select * from " + dbname + "_kernel@" + dbserver + ":supplier";
#endif
                ret = ExecSQL(conn_db, trans, sql, true);
                if (!ret.result)
                {
                    trans.Rollback();
                    conn_db.Close();
                    return ret;
                }

                trans.Commit();
            }

            return ret;
        }

        public List<Town> LoadTown(Town finder, out Returns ret)
        {
            ret = Utils.InitReturns();

            List<Town> spis = new List<Town>();

            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return null;

            string sql = "";
            DbTables tables = new DbTables(conn_db);
#if PG
            string from_tables = tables.town + " t, " + tables.rajon + " r, " + tables.dom + " d, " + tables.ulica + " u ";
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
            //выбрать список
            sql = "select distinct t.town, r.nzp_town from " + from_tables + " where 1=1 " + swhere + " Order by town ";
#else
            string from_tables = tables.town + " t, " + tables.rajon + " r, " + tables.dom + " d, " + tables.ulica + " u ";
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
            //выбрать список
            sql = "select unique t.town, r.nzp_town from " + from_tables + " where 1=1 " + swhere + " Order by town ";
#endif

            IDataReader reader;
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
                    i++;
                    if (i <= finder.skip) continue;
                    Town zap = new Town();
                    if (reader["nzp_town"] == DBNull.Value) zap.nzp_town = 0;
                    else zap.nzp_town = (int)reader["nzp_town"];
                    if (reader["town"] == DBNull.Value) zap.town = "";
                    else zap.town = (string)reader["town"];
                    spis.Add(zap);
                    if (finder.rows > 0 && i >= finder.skip + finder.rows) break;
                }

                reader.Close();

                //определить количество записей
#if PG
                sql = "select count(distinct t.nzp_town) from " + from_tables + " where 1=1 " + swhere;
#else
                sql = "select count(unique t.nzp_town) from " + from_tables + " where 1=1 " + swhere;
#endif
                object count = ExecScalar(conn_db, sql, out ret, true);
                if (ret.result)
                {
                    try
                    {
                        ret.tag = Convert.ToInt32(count);
                    }
                    catch (Exception ex)
                    {
                        conn_db.Close();
                        ret.result = false;
                        ret.text = ex.Message;
                        return null;
                    }
                }

                conn_db.Close();
                return spis;
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
                MonitorLog.WriteLog("Ошибка заполнения справочника районов " + err, MonitorLog.typelog.Error, 20, 201, true);
                return null;
            }
        }

        public List<_reestr_unloads> LoadUploadedReestrList(Finder finder, out Returns ret)
        {
            ret = Utils.InitReturns();

            List<_reestr_unloads> spis = new List<_reestr_unloads>();
            IDataReader reader;
            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return null;

            string sql = "";
            DbTables tables = new DbTables(conn_db);
            string skip = "";
            string rows = "";


#if PG
            if (finder.skip != 0)
            {
                skip = " offset " + finder.skip;
            }
            if (finder.rows != 0)
            {
                rows = " limit " + finder.rows;
            }
#else
            if (finder.skip != 0)
            {
                skip = " skip " + finder.skip;
            }
            if (finder.rows != 0)
            {
                rows = " first " + finder.rows;
            }
#endif

            //выбрать список
#if PG
            sql = "select r.*,u.name from " + Points.Pref + "_data.tula_reestr_unloads r left outer join " + Points.Pref + "_data.users u on r.user_unloaded=u.nzp_user where r.is_actual<>100  order by nzp_reestr desc  " + skip + " " + rows + " ";
#else
            sql = "select " + skip + " " + rows + " r.*,u.name from " + Points.Pref + "_data:tula_reestr_unloads r left outer join " + Points.Pref + "_data:users u on r.user_unloaded=u.nzp_user where r.is_actual<>100 order by nzp_reestr desc ";
#endif
            if (!ExecRead(conn_db, out reader, sql, true).result)
            {
                conn_db.Close();
                MonitorLog.WriteLog("Ошибка получения списка выгрузок реестра", MonitorLog.typelog.Error, 20, 201, true);
                return null;
            }
            try
            {
                //int i = 0;
                while (reader.Read())
                {
                    _reestr_unloads zap = new _reestr_unloads();
                    if (reader["nzp_reestr"] == DBNull.Value) zap.nzp_reestr = 0;
                    else zap.nzp_reestr = (int)reader["nzp_reestr"];

                    if (reader["name_file"] == DBNull.Value) zap.name_file = "";
                    else zap.name_file = (string)reader["name_file"].ToString().Trim();

                    if (reader["date_unload"] == DBNull.Value) zap.date_unload = new DateTime().ToShortDateString();
                    else
                    {
                        DateTime dat_unload = (DateTime)reader["date_unload"];
                        zap.date_unload = dat_unload.ToShortDateString();
                    }

                    if (reader["unloading_date"] == DBNull.Value) zap.unloading_date = "";
                    else
                    {
                        zap.unloading_date = reader["unloading_date"].ToString();
                    }

                    if (reader["user_unloaded"] == DBNull.Value) zap.user_unloaded = 0;
                    else zap.user_unloaded = (int)reader["user_unloaded"];

                    if (reader["name"] == DBNull.Value) zap.name_user_unloaded = "";
                    else zap.name_user_unloaded = (string)reader["name"].ToString().Trim();

                    if (reader["nzp_exc"] == DBNull.Value) zap.nzp_exc = 0;
                    else zap.nzp_exc = (int)reader["nzp_exc"];

                    spis.Add(zap);

                }

                //определить количество записей
#if PG
                sql = "select count(*) from " + Points.Pref + "_data.tula_reestr_unloads where is_actual<>100; ";
#else
                sql = "select count(*) from " + Points.Pref + "_data:tula_reestr_unloads where is_actual<>100; ";
#endif
                object count = ExecScalar(conn_db, sql, out ret, true);
                if (ret.result)
                {
                    try
                    {
                        ret.tag = Convert.ToInt32(count);
                    }
                    catch (Exception ex)
                    {
                        conn_db.Close();
                        ret.result = false;
                        ret.text = ex.Message;
                        return null;
                    }
                }

                reader.Close();
                conn_db.Close();
                return spis;
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
                MonitorLog.WriteLog("Ошибка получения списка выгрузок реестра" + err, MonitorLog.typelog.Error, 20, 201, true);
                return null;
            }
        }

        public List<_reestr_downloads> LoadDownloadedReestrList(Finder finder, out Returns ret)
        {
            ret = Utils.InitReturns();

            List<_reestr_downloads> spis = new List<_reestr_downloads>();
            IDataReader reader;
            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return null;

            string sql = "";

            string skip = "";
            string rows = "";
#if PG
            if (finder.skip != 0)
            {
                skip = " offset " + finder.skip;
            }
            if (finder.rows != 0)
            {
                rows = " limit " + finder.rows;
            }
#else
            if (finder.skip != 0)
            {
                skip = " skip " + finder.skip;
            }
            if (finder.rows != 0)
            {
                rows = " first " + finder.rows;
            }
#endif
            //выбрать список

#if PG
            sql = "select  r.*,s.name_type,u.name, b.branch_name from " + Points.Pref + "_data.tula_reestr_downloads r left outer join " + Points.Pref + "_data.users u on r.user_downloaded=u.nzp_user " +
            "left outer join " + Points.Pref + "_data.tula_reestr_sprav s on r.nzp_type=s.nzp_type left outer join " + Points.Pref + "_data.tula_s_bank b on b.branch_id = r.branch_id  " + skip + " " + rows + "";
#else
            sql = "select " + skip + " " + rows + " r.*,s.name_type,u.name, b.branch_name from " + Points.Pref + "_data:tula_reestr_downloads r left outer join " + Points.Pref + "_data:users u on r.user_downloaded=u.nzp_user " +
            "left outer join " + Points.Pref + "_data:tula_reestr_sprav s on r.nzp_type=s.nzp_type left outer join " + Points.Pref + "_data:tula_s_bank b on b.branch_id = r.branch_id  order by nzp_download desc";
#endif
            if (!ExecRead(conn_db, out reader, sql, true).result)
            {
                conn_db.Close();
                MonitorLog.WriteLog("Ошибка получения списка загрузок реестра", MonitorLog.typelog.Error, 20, 201, true);
                return null;
            }
            try
            {

                while (reader.Read())
                {
                    _reestr_downloads zap = new _reestr_downloads();
                    if (reader["nzp_download"] == DBNull.Value) zap.nzp_download = 0;
                    else zap.nzp_download = (int)reader["nzp_download"];

                    if (reader["file_name"] == DBNull.Value) zap.file_name = "";
                    else zap.file_name = (string)reader["file_name"].ToString().Trim();

                    if (reader["name_type"] == DBNull.Value) zap.name_type = "";
                    else zap.name_type = (string)reader["name_type"].ToString().Trim();


                    if (reader["date_download"] == DBNull.Value) zap.date_download = "";
                    else
                    {
                        DateTime date_download = (DateTime)reader["date_download"];
                        zap.date_download = date_download.ToString();
                    }

                    if (reader["day"] == DBNull.Value || reader["month"] == DBNull.Value) zap.day_month = "";
                    else zap.day_month = ((int)reader["day"]).ToString() + "/" + ((int)reader["month"]).ToString();


                    if (reader["branch_name"] == DBNull.Value) zap.branch_name = "";
                    else zap.branch_name = (string)reader["branch_name"].ToString().Trim();


                    if (reader["name"] == DBNull.Value) zap.name_user_downloaded = "";
                    else zap.name_user_downloaded = (string)reader["name"].ToString().Trim();
                    spis.Add(zap);

                }

                //определить количество записей
#if PG
                sql = "select count(*) from " + Points.Pref + "_data.tula_reestr_downloads; ";
#else
                sql = "select count(*) from " + Points.Pref + "_data:tula_reestr_downloads; ";
#endif
                object count = ExecScalar(conn_db, sql, out ret, true);
                if (ret.result)
                {
                    try
                    {
                        ret.tag = Convert.ToInt32(count);
                    }
                    catch (Exception ex)
                    {
                        conn_db.Close();
                        ret.result = false;
                        ret.text = ex.Message;
                        return null;
                    }
                }

                reader.Close();
                conn_db.Close();
                return spis;
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
                MonitorLog.WriteLog("Ошибка получения списка выгрузок реестра" + err, MonitorLog.typelog.Error, 20, 201, true);
                return null;
            }
        }

        public List<Land> LoadLand(Land finder, out Returns ret)
        {
            if (finder.nzp_user < 0)
            {
                ret = new Returns(false, "Не задан пользователь", -1);
                return null;
            }

            #region соединение с БД
            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return null;
            #endregion

            DbTables tables = new DbTables(conn_db);
#if PG
            string sql = "select * from " + tables.land + " order by land";
#else
            string sql = "select * from " + tables.land + " order by land";
#endif
            IDataReader reader;
            ret = ExecRead(conn_db, out reader, sql, true);
            if (!ret.result) return null;

            List<Land> list = new List<Land>();
            try
            {
                while (reader.Read())
                {
                    Land land = new Land();
                    if (reader["land"] != DBNull.Value) land.land = ((string)reader["land"]).Trim();
                    if (reader["nzp_land"] != DBNull.Value) land.nzp_land = Convert.ToInt32(reader["nzp_land"]);
                    list.Add(land);
                }
            }
            catch (Exception ex)
            {
                CloseReader(ref reader);
                conn_db.Close();
                ret = new Returns(false, ex.Message);
                MonitorLog.WriteLog("Ошибка получения информации", MonitorLog.typelog.Error, 20, 201, true);
                return null;
            }

            return list;
        }

        public List<Stat> LoadStat(Stat finder, out Returns ret)
        {
            if (finder.nzp_user < 0)
            {
                ret = new Returns(false, "Не задан пользователь", -1);
                return null;
            }

            #region соединение с БД
            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return null;
            #endregion

            string where = "";
            if (finder.nzp_land > 0) where = " and nzp_land = " + finder.nzp_land;

            DbTables tables = new DbTables(conn_db);
#if PG
            string sql = "select * from " + tables.stat + " where 1=1 " + where + " order by stat";
#else
            string sql = "select * from " + tables.stat + " where 1=1 " + where + " order by stat";
#endif
            IDataReader reader;
            ret = ExecRead(conn_db, out reader, sql, true);
            if (!ret.result) return null;

            List<Stat> list = new List<Stat>();
            try
            {
                while (reader.Read())
                {
                    Stat stat = new Stat();
                    if (reader["stat"] != DBNull.Value) stat.stat = ((string)reader["stat"]).Trim();
                    if (reader["nzp_stat"] != DBNull.Value) stat.nzp_stat = Convert.ToInt32(reader["nzp_stat"]);
                    list.Add(stat);
                }
            }
            catch (Exception ex)
            {
                CloseReader(ref reader);
                conn_db.Close();
                ret = new Returns(false, ex.Message);
                MonitorLog.WriteLog("Ошибка получения информации", MonitorLog.typelog.Error, 20, 201, true);
                return null;
            }

            return list;
        }

        public List<Town> LoadTown2(Town finder, out Returns ret)
        {
            if (finder.nzp_user < 0)
            {
                ret = new Returns(false, "Не задан пользователь", -1);
                return null;
            }

            #region соединение с БД
            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return null;
            #endregion

            string where = "";
            if (finder.nzp_stat > 0) where = " and nzp_stat = " + finder.nzp_stat;

            DbTables tables = new DbTables(conn_db);
#if PG
            string sql = "select * from " + tables.town + " where 1=1 " + where + " order by town";
#else
            string sql = "select * from " + tables.town + " where 1=1 " + where + " order by town";
#endif
            IDataReader reader;
            ret = ExecRead(conn_db, out reader, sql, true);
            if (!ret.result) return null;

            List<Town> list = new List<Town>();
            try
            {
                while (reader.Read())
                {
                    Town town = new Town();
                    if (reader["town"] != DBNull.Value) town.town = ((string)reader["town"]).Trim();
                    if (reader["nzp_town"] != DBNull.Value) town.nzp_town = Convert.ToInt32(reader["nzp_town"]);
                    list.Add(town);
                }
            }
            catch (Exception ex)
            {
                CloseReader(ref reader);
                conn_db.Close();
                ret = new Returns(false, ex.Message);
                MonitorLog.WriteLog("Ошибка получения информации", MonitorLog.typelog.Error, 20, 201, true);
                return null;
            }

            return list;
        }

        public List<Rajon> LoadRajon(Rajon finder, out Returns ret)
        {
            if (finder.nzp_user < 0)
            {
                ret = new Returns(false, "Не задан пользователь", -1);
                return null;
            }

            #region соединение с БД
            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return null;
            #endregion

            string where = "";
            if (finder.nzp_town > 0) where = " and nzp_town = " + finder.nzp_town;

            DbTables tables = new DbTables(conn_db);
#if PG
            string sql = "select * from " + tables.rajon + " where 1=1 " + where + " order by rajon";
#else
            string sql = "select * from " + tables.rajon + " where 1=1 " + where + " order by rajon";
#endif
            IDataReader reader;
            ret = ExecRead(conn_db, out reader, sql, true);
            if (!ret.result) return null;

            List<Rajon> list = new List<Rajon>();
            try
            {
                while (reader.Read())
                {
                    Rajon rajon = new Rajon();
                    if (reader["rajon"] != DBNull.Value) rajon.rajon = ((string)reader["rajon"]).Trim();
                    if (reader["nzp_raj"] != DBNull.Value) rajon.nzp_raj = Convert.ToInt32(reader["nzp_raj"]);
                    list.Add(rajon);
                }
            }
            catch (Exception ex)
            {
                CloseReader(ref reader);
                conn_db.Close();
                ret = new Returns(false, ex.Message);
                MonitorLog.WriteLog("Ошибка получения информации", MonitorLog.typelog.Error, 20, 201, true);
                return null;
            }

            if (list != null)
            {
                ret.tag = list.Count;
                if (finder.skip > 0 && list.Count > finder.skip) list.RemoveRange(0, finder.skip);
                if (finder.rows > 0 && list.Count > finder.rows) list.RemoveRange(finder.rows, list.Count - finder.rows);

            }


            return list;
        }

        public Returns DeleteReestrTula(_reestr_unloads finder)
        {

            Returns ret;
            ret = Utils.InitReturns();
            if (finder.nzp_user < 0)
            {
                ret = new Returns(false, "Не задан пользователь", -1);
                ret.result = false;
                return ret;
            }
            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return ret;

#if PG
            if (!ExecSQL(conn_db, " update " + Points.Pref + "_data.tula_reestr_unloads set (is_actual)=(100) where nzp_reestr=" + finder.nzp_reestr, true).result)
            {
                ret.result = false;
                ret.text = "Ошибка обновления реестра для Тулы";
            }
#else
            if (!ExecSQL(conn_db, " update " + Points.Pref + "_data:tula_reestr_unloads set (is_actual)=(100) where nzp_reestr=" + finder.nzp_reestr, true).result)
            {
                ret.result = false;
                ret.text = "Ошибка обновления реестра для Тулы";
            }
#endif
            return ret;
        }


        public Returns DeleteDownloadReestrTula(Finder finder, int nzp_download)
        {
            Returns ret = Utils.InitReturns();
            if (finder.nzp_user < 0)
            {
                ret = new Returns(false, "Не задан пользователь", -1);
                ret.result = false;
                return ret;
            }
            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return ret;

            StringBuilder sql = new StringBuilder();
            sql.Remove(0, sql.Length);

#if PG
            sql.Append("select nzp_type from " + Points.Pref + "_data.tula_reestr_downloads where nzp_download=" + nzp_download + " ");
#else
            sql.Append("select nzp_type from " + Points.Pref + "_data:tula_reestr_downloads where nzp_download=" + nzp_download + " ");
#endif
            object obj = ExecScalar(conn_db, sql.ToString(), out ret, true);
            if (!ret.result)
            {
                return ret;
            }

            if (obj == null)
            {
                ret.result = false;
                ret.text = "Ошибка удаления данных";
                return ret;
            }

            int nzp_type = Convert.ToInt32(obj);
            //Удаляем квитанцию, она удаляется только вместе с файлом реестра
            if (nzp_type == 2 || nzp_type == 4)
            {
                sql.Remove(0, sql.Length);
#if PG
                sql.Append(" select file_name from " + Points.Pref + "_data.tula_reestr_downloads where nzp_download=" + nzp_download + " ");
#else
                sql.Append(" select file_name from " + Points.Pref + "_data:tula_reestr_downloads where nzp_download=" + nzp_download + " ");
#endif

                object name = ExecScalar(conn_db, sql.ToString(), out ret, true);
                if (!ret.result)
                {
                    return ret;
                }
                if (name == null)
                {
                    ret.result = false;
                    MonitorLog.WriteLog("Ошибка получения данных о удаляемом файле", MonitorLog.typelog.Error, true);
                    return ret;
                }
                string Type = "";
                var NameFile = Convert.ToString(name).Trim().Split('.');
                if (nzp_type == 2)
                {
                    Type = ".0" + NameFile[1].Substring(1, 2);
                }
                else
                {
                    Type = ".00" + NameFile[1].Substring(2);
                }
                string Name = NameFile[0] + Type;


                #region//Удаляем записи в реестре загрузок

                sql.Remove(0, sql.Length);
#if PG
                sql.Append("  delete from " + Points.Pref + "_data.tula_reestr_downloads where  nzp_download = " + nzp_download + " ");
#else
                sql.Append("  delete from " + Points.Pref + "_data:tula_reestr_downloads where  nzp_download = " + nzp_download + " ");

#endif
                if (!ExecSQL(conn_db, sql.ToString(), true).result)
                {
                    ret.result = false;
                    ret.text = "Ошибка удаления данных загрузки";
                    return ret;
                }


                //Удаляем запись реестра
                sql.Remove(0, sql.Length);
#if PG
                sql.Append("  delete from " + Points.Pref + "_data.tula_reestr_downloads where   file_name = '" + Name + "' ");
#else
                sql.Append("  delete from " + Points.Pref + "_data:tula_reestr_downloads where  file_name = '" + Name + "' ");

#endif
                if (!ExecSQL(conn_db, sql.ToString(), true).result)
                {
                    ret.result = false;
                    ret.text = "Ошибка удаления данных загрузки";
                    return ret;
                }
                #endregion

                sql.Remove(0, sql.Length);
#if PG
                sql.Append(" select nzp_kvit_reestr from  " + Points.Pref + "_data.tula_kvit_reestr where file_name='" + Name + "' ");
#else
                sql.Append(" select nzp_kvit_reestr from  " + Points.Pref + "_data:tula_kvit_reestr where file_name='" + Name + "' ");
#endif
                object nzp_kvit = ExecScalar(conn_db, sql.ToString(), out ret, true);
                if (!ret.result)
                {
                    return ret;
                }
                if (nzp_kvit == null)
                {
                    return ret;
                }
                int nzp_kvit_reestr = Convert.ToInt32(nzp_kvit);

                sql.Remove(0, sql.Length);
#if PG
                sql.Append(" delete from " + Points.Pref + "_data.tula_file_reestr where nzp_kvit_reestr=" + nzp_kvit_reestr + "");
#else
                sql.Append(" delete from " + Points.Pref + "_data:tula_file_reestr where nzp_kvit_reestr=" + nzp_kvit_reestr + "");
#endif
                if (!ExecSQL(conn_db, sql.ToString(), true).result)
                {
                    ret.result = false;
                    ret.text = "Ошибка удаления данных загрузки";
                    return ret;
                }


                sql.Remove(0, sql.Length);
#if PG
                sql.Append(" delete from " + Points.Pref + "_data.tula_kvit_reestr where nzp_kvit_reestr=" + nzp_kvit_reestr + "");
#else
                sql.Append(" delete from " + Points.Pref + "_data:tula_kvit_reestr where nzp_kvit_reestr=" + nzp_kvit_reestr + "");
#endif
                if (!ExecSQL(conn_db, sql.ToString(), true).result)
                {
                    ret.result = false;
                    ret.text = "Ошибка удаления данных загрузки";
                    return ret;
                }


            }
            else
            {
                sql.Remove(0, sql.Length);
                //Удаляем данные реестра
#if PG

                sql.Append(" delete from  " + Points.Pref + "_data.tula_file_reestr where nzp_kvit_reestr = ");
                sql.Append(" (select nzp_kvit_reestr from  " + Points.Pref + "_data.tula_kvit_reestr kv,  " + Points.Pref + "_data.tula_reestr_downloads d ");
                sql.Append(" where trim(d.file_name)=trim(kv.file_name) and d.nzp_download=" + nzp_download + ") ");
#else
                sql.Append(" delete from  " + Points.Pref + "_data:tula_file_reestr where nzp_kvit_reestr = ");
                sql.Append(" (select nzp_kvit_reestr from  " + Points.Pref + "_data:tula_kvit_reestr kv,  " + Points.Pref + "_data:tula_reestr_downloads d ");
                sql.Append(" where trim(d.file_name)=trim(kv.file_name) and d.nzp_download=" + nzp_download + ") ");
#endif
                if (!ExecSQL(conn_db, sql.ToString(), true).result)
                {
                    ret.result = false;
                    ret.text = "Ошибка удаления данных реестра";
                    return ret;
                }


                //Удаляем запись реестра
                sql.Remove(0, sql.Length);
#if PG
                sql.Append("  delete from " + Points.Pref + "_data.tula_reestr_downloads where  nzp_download = " + nzp_download + " ");
#else
                sql.Append("  delete from " + Points.Pref + "_data:tula_reestr_downloads where  nzp_download = " + nzp_download + " ");

#endif
                if (!ExecSQL(conn_db, sql.ToString(), true).result)
                {
                    ret.result = false;
                    ret.text = "Ошибка удаления данных загрузки";
                    return ret;
                }

            }

            return ret;
        }



        public List<unload_exchange_sz> LoadListExchangeSZ(Finder finder, out Returns ret)
        {
            ret = Utils.InitReturns();

            if (finder.nzp_user < 0)
            {
                ret = new Returns(false, "Не задан пользователь", -1);
                return null;
            }

            List<unload_exchange_sz> spis = new List<unload_exchange_sz>();
            IDataReader reader;
            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return null;

            string sql = "";
            DbTables tables = new DbTables(conn_db);
            string skip = "";
            string rows = "";

#if PG
            if (finder.skip != 0)
            {
                skip = " offset " + finder.skip;
            }
            if (finder.rows != 0)
            {
                rows = " limit " + finder.rows;
            }
#else
            if (finder.skip != 0)
            {
                skip = " skip " + finder.skip;
            }
            if (finder.rows != 0)
            {
                rows = " first " + finder.rows;
            }
#endif

            //выбрать список
#if PG
            sql = "select  r.*,u.name from " + Points.Pref + "_data.tula_ex_sz r left outer join " + Points.Pref + "_data.users u on r.nzp_user=u.nzp_user  order by nzp_ex_sz " + skip + " " + rows + " ";
#else
            sql = "select " + skip + " " + rows + " r.*,u.name from " + Points.Pref + "_data:tula_ex_sz r left outer join " + Points.Pref + "_data:users u on r.nzp_user=u.nzp_user order by nzp_ex_sz ";
#endif
            if (!ExecRead(conn_db, out reader, sql, true).result)
            {
                conn_db.Close();
                MonitorLog.WriteLog("Ошибка получения списка выгрузок реестра", MonitorLog.typelog.Error, 20, 201, true);
                return null;
            }
            try
            {
                //int i = 0;
                while (reader.Read())
                {
                    unload_exchange_sz zap = new unload_exchange_sz();
                    if (reader["nzp_ex_sz"] == DBNull.Value) zap.nzp_ex_sz = 0;
                    else zap.nzp_ex_sz = (int)reader["nzp_ex_sz"];

                    if (reader["file_name"] == DBNull.Value) zap.file_name = "";
                    else zap.file_name = (string)reader["file_name"].ToString().Trim();

                    if (reader["dat_upload"] == DBNull.Value) zap.dat_upload = new DateTime().ToShortDateString();
                    else
                    {
                        DateTime dat_upload = (DateTime)reader["dat_upload"];
                        zap.dat_upload = dat_upload.ToString("yyyy.MM.dd HH:mm:ss");
                    }
                    if (reader["name"] == DBNull.Value) zap.name_user_unloaded = "";
                    else zap.name_user_unloaded = (string)reader["name"].ToString().Trim();

                    if (reader["proc"] == DBNull.Value) zap.proc = 0d;
                    else zap.proc = Convert.ToDouble(reader["proc"]) * 100d;

                    spis.Add(zap);

                }

                //определить количество записей
#if PG
                sql = "select count(*) from " + Points.Pref + "_data.tula_ex_sz; ";
#else
                sql = "select count(*) from " + Points.Pref + "_data:tula_ex_sz; ";
#endif
                object count = ExecScalar(conn_db, sql, out ret, true);
                if (ret.result)
                {
                    try
                    {
                        ret.tag = Convert.ToInt32(count);
                    }
                    catch (Exception ex)
                    {
                        conn_db.Close();
                        ret.result = false;
                        ret.text = ex.Message;
                        return null;
                    }
                }

                reader.Close();
                conn_db.Close();
                return spis;
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
                MonitorLog.WriteLog("Ошибка получения списка файлов обмена" + err, MonitorLog.typelog.Error, 20, 201, true);
                return null;
            }
        }

        public List<BCTypes> LoadBCTypes(BCTypes finder, out Returns ret)
        {
            if (finder.nzp_user < 0)
            {
                ret = new Returns(false, "Не задан пользователь", -1);
                return null;
            }

            IDataReader reader;
            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return null;

            DbTables tables = new DbTables(conn_db);

            if (!TempTableInWebCashe(conn_db, tables.bc_types))
            {
                ret = new Returns(false, "Нет таблицы в БД", -1);
                MonitorLog.WriteLog("Ошибка получения списка: Формат Банк-клиент " + ret.text, MonitorLog.typelog.Error, 20, 201, true);
                return null;
            }

            string where = "";
            if (finder.id > 0) where += " and id = " + finder.id;
            if (finder.is_active > 0) where += " and is_active = " + finder.is_active;

            string sql = "select * from " + Points.Pref + "_kernel" + tableDelimiter + "bc_types where 1=1 " + where;

            if (!ExecRead(conn_db, out reader, sql, true).result)
            {
                conn_db.Close();
                MonitorLog.WriteLog("Ошибка получения списка: Формат Банк-клиент ", MonitorLog.typelog.Error, 20, 201, true);
                return null;
            }
            List<BCTypes> spis = new List<BCTypes>();
            try
            {
                while (reader.Read())
                {
                    BCTypes zap = new BCTypes();
                    if (reader["id"] != DBNull.Value) zap.id = (int)reader["id"];
                    if (reader["name_"] != DBNull.Value) zap.name_ = (string)reader["name_"].ToString().Trim();
                    if (reader["is_active"] != DBNull.Value) zap.is_active = Convert.ToInt32(reader["is_active"]);
                    spis.Add(zap);
                }

                reader.Close();
                conn_db.Close();
                return spis;
            }
            catch (Exception ex)
            {
                if (reader != null) reader.Close();
                conn_db.Close();

                ret.result = false;
                ret.text = ex.Message;

                string err;
                if (Constants.Viewerror) err = " \n " + ex.Message;
                else err = "";
                MonitorLog.WriteLog("Ошибка получения списка: Формат Банк-клиент" + err, MonitorLog.typelog.Error, 20, 201, true);
                return null;
            }
        }

        public List<Formuls> GetFormuls(FormulsFinder finder, out Returns ret)
        {
            if (finder.nzp_user < 0)
            {
                ret = new Returns(false, "Не задан пользователь", -1);
                return null;
            }

            IDbConnection conn_db = GetConnection();
            ret = OpenDb(conn_db, true);
            if (!ret.result) return null;

            DbTables tables = new DbTables(conn_db);
            List<Formuls> spis = null;

            MyDataReader reader = null;

            try
            {
                string where = "";
                if (finder.nzp_frm > 0) where += " and f.nzp_frm = " + finder.nzp_frm;
                if (finder.is_device >= 0) where += " and f.is_device = " + finder.is_device;
                if (finder.name_frm != "")
                {
                    string s = Utils.EStrNull(finder.name_frm, "").ToLower();
                    where += " and lower(f.name_frm) like '%" + s.Substring(1, s.Length - 2) + "%'";
                }
                if (finder.nzp_measure > 0) where += " and f.nzp_measure = " + finder.nzp_measure;

                string sql = "select count(*) as num from " + tables.formuls + " f where 1=1 " + where;
                var cnt = ExecScalar(conn_db, sql, out ret, true);
                if (!ret.result) return null;

                sql = "select f.nzp_frm, f.name_frm, f.dat_s, f.dat_po, f.is_device, f.nzp_measure, m.measure, m.measure_long" +
                    " from " + tables.formuls + " f left outer join " + tables.measure + " m on f.nzp_measure = m.nzp_measure where 1=1 " + where +
                    " order by name_frm, dat_s desc";

                ret = ExecRead(conn_db, out reader, sql, true);
                if (!ret.result) return null;

                spis = new List<Formuls>();
                int i = 0;
                while (reader.Read())
                {
                    i++;
                    if (i <= finder.skip) continue;

                    Formuls zap = new Formuls();
                    zap.num = i;
                    if (reader["nzp_frm"] != DBNull.Value) zap.nzp_frm = (int)reader["nzp_frm"];
                    if (reader["name_frm"] != DBNull.Value) zap.name_frm = Convert.ToString(reader["name_frm"]).Trim();
                    if (reader["dat_s"] != DBNull.Value) zap.dat_s = Convert.ToDateTime(reader["dat_s"]).ToShortDateString();
                    if (reader["dat_po"] != DBNull.Value) zap.dat_po = Convert.ToDateTime(reader["dat_po"]).ToShortDateString();
                    if (reader["is_device"] != DBNull.Value) zap.is_device = Convert.ToInt32(reader["is_device"]);

                    zap.measure = new SMeasure();
                    if (reader["nzp_measure"] != DBNull.Value) zap.measure.nzp_measure = Convert.ToInt32(reader["nzp_measure"]);
                    if (reader["measure"] != DBNull.Value) zap.measure.measure = Convert.ToString(reader["measure"]).Trim();
                    if (reader["measure_long"] != DBNull.Value) zap.measure.measure_long = Convert.ToString(reader["measure_long"]).Trim();

                    spis.Add(zap);

                    if (finder.rows > 0 && i >= finder.skip + finder.rows) break;
                }

                ret.tag = Convert.ToInt32(cnt);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка получения списка формул расчета\n " + ex.Message, MonitorLog.typelog.Error, 20, 201, true);
                return null;
            }
            finally
            {
                if (reader != null) reader.Close();
                conn_db.Close();
                tables = null;
            }

            return spis;
        }
    }
}
