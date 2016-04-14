using System;
using System.Data;
using System.Collections.Generic;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;

namespace STCLINE.KP50.DataBase
{

    public class DbSpravToAdres :DataBaseHead
    {

        public List<Town> LoadTown(Town finder, out Returns ret)
        {

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
            int countElems = 0;
            string sql = "select count(distinct t.nzp_town) from " + fromTables + " where 1=1 " + swhere;
            var count = ExecScalar(connDB, sql, out ret, true);
            if (ret.result)
            {
                try
                {
                    countElems = (count != DBNull.Value && count != null) ? Convert.ToInt32(count) : 0;
                }
                catch (Exception ex)
                {
                    MonitorLog.WriteException("Ошибка заполнения справочника районов: подсчет количества записей ", ex);
                    //MonitorLog.WriteLog("1)LoadTown: connDB.State : " + connDB.State, MonitorLog.typelog.Warn, true);
                    ret.result = false;
                    ret.text = ex.Message;
                    return null;
                }
            }


            //выбрать список
            sql = "select distinct t.town, r.nzp_town from " + fromTables + " where 1=1 " + swhere + wherePoint + " Order by town ";


            MyDataReader reader;
            ret = ExecRead(connDB, out reader, sql, true);
            if (!ret.result)
            {
                //MonitorLog.WriteLog("2)LoadTown: connDB.State : " + connDB.State + "reader" + reader, MonitorLog.typelog.Warn, true);
                return null;
            }

            try
            {
                int i = 0;
                while (reader.Read())
                {
                    i++;
                    if (i <= finder.skip) continue;
                    var zap = new Town();
                    if (reader["nzp_town"] == DBNull.Value) zap.nzp_town = 0;
                    else zap.nzp_town = (int)reader["nzp_town"];
                    if (reader["town"] == DBNull.Value) zap.town = "";
                    else zap.town = (string)reader["town"];
                    spis.Add(zap);
                    if (finder.rows > 0 && i >= finder.skip + finder.rows) break;
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
                MonitorLog.WriteLog("Ошибка заполнения справочника районов " + err, MonitorLog.typelog.Error, 20, 201,
                    true);
                return null;
            }
            finally
            {
                if (reader != null) reader.Close();
                connDB.Close();
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
            IDbConnection connDB = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(connDB, true);
            if (!ret.result) return null;
            #endregion

            var tables = new DbTables(connDB);

            string sql = "select * from " + tables.land + " order by land";
            MyDataReader reader;
            ret = ExecRead(connDB, out reader, sql, true);
            if (!ret.result) return null;

            var list = new List<Land>();
            try
            {
                while (reader.Read())
                {
                    var land = new Land();
                    if (reader["land"] != DBNull.Value) land.land = ((string)reader["land"]).Trim();
                    if (reader["nzp_land"] != DBNull.Value) land.nzp_land = Convert.ToInt32(reader["nzp_land"]);
                    list.Add(land);
                }
                reader.Close();
            }
            catch (Exception ex)
            {
                reader.Close();

                ret = new Returns(false, ex.Message);
                MonitorLog.WriteLog("Ошибка получения информации", MonitorLog.typelog.Error, 20, 201, true);
                return null;
            }
            finally
            {
                connDB.Close();

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
            IDbConnection connDB = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(connDB, true);
            if (!ret.result) return null;
            #endregion

            string where = "";
            if (finder.nzp_land > 0) where = " and nzp_land = " + finder.nzp_land;

            var tables = new DbTables(connDB);
            string sql = "select * from " + tables.stat + " where 1=1 " + where + " order by stat";
            MyDataReader reader;
            ret = ExecRead(connDB, out reader, sql, true);
            if (!ret.result) return null;

            var list = new List<Stat>();
            try
            {
                while (reader.Read())
                {
                    var stat = new Stat();
                    if (reader["stat"] != DBNull.Value) stat.stat = ((string)reader["stat"]).Trim();
                    if (reader["nzp_stat"] != DBNull.Value) stat.nzp_stat = Convert.ToInt32(reader["nzp_stat"]);
                    list.Add(stat);
                }
                reader.Close();
            }
            catch (Exception ex)
            {
                reader.Close();

                ret = new Returns(false, ex.Message);
                MonitorLog.WriteLog("Ошибка получения информации", MonitorLog.typelog.Error, 20, 201, true);
                return null;
            }
            finally
            {
                connDB.Close();
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
            IDbConnection connDB = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(connDB, true);
            if (!ret.result) return null;
            #endregion

            string where = "";
            if (finder.nzp_stat > 0) where = " and nzp_stat = " + finder.nzp_stat;

            var tables = new DbTables(connDB);
            string sql = "select * from " + tables.town + " where 1=1 " + where + " order by town";
            MyDataReader reader;
            ret = ExecRead(connDB, out reader, sql, true);
            if (!ret.result) return null;

            var list = new List<Town>();
            try
            {
                while (reader.Read())
                {
                    var town = new Town();
                    if (reader["town"] != DBNull.Value) town.town = ((string)reader["town"]).Trim();
                    if (reader["nzp_town"] != DBNull.Value) town.nzp_town = Convert.ToInt32(reader["nzp_town"]);
                    list.Add(town);
                }
                reader.Close();
            }
            catch (Exception ex)
            {
                reader.Close();

                ret = new Returns(false, ex.Message);
                MonitorLog.WriteLog("Ошибка получения информации", MonitorLog.typelog.Error, 20, 201, true);
                return null;
            }
            finally
            {
                connDB.Close();
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
            IDbConnection connDB = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(connDB, true);
            if (!ret.result) return null;
            #endregion

            string where = "";
            if (finder.nzp_town > 0) where = " and nzp_town = " + finder.nzp_town;

            var tables = new DbTables(connDB);
            string sql = "select * from " + tables.rajon + " where 1=1 " + where + " order by rajon";
            MyDataReader reader;
            ret = ExecRead(connDB, out reader, sql, true);
            if (!ret.result) return null;

            var list = new List<Rajon>();
            try
            {
                while (reader.Read())
                {
                    var rajon = new Rajon();
                    if (reader["rajon"] != DBNull.Value) rajon.rajon = ((string)reader["rajon"]).Trim();
                    if (reader["nzp_raj"] != DBNull.Value) rajon.nzp_raj = Convert.ToInt32(reader["nzp_raj"]);
                    list.Add(rajon);
                }
                reader.Close();
            }
            catch (Exception ex)
            {
                reader.Close();

                ret = new Returns(false, ex.Message);
                MonitorLog.WriteLog("Ошибка получения информации", MonitorLog.typelog.Error, 20, 201, true);
                return null;
            }
            finally
            {
                connDB.Close();
            }

            ret.tag = list.Count;
            if (finder.skip > 0 && list.Count > finder.skip) list.RemoveRange(0, finder.skip);
            if (finder.rows > 0 && list.Count > finder.rows) list.RemoveRange(finder.rows, list.Count - finder.rows);


            return list;
        }
    }

}
