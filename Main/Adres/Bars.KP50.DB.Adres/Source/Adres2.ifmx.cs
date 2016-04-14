using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;

namespace STCLINE.KP50.DataBase
{
    public partial class DbAdres : DbAdresKernel
    {
        public Returns UpdateAddressPrefer(Ls finder)
        {
            if (finder.nzp_user < 0) return new Returns(false, "Не задан пользователь", -1);

            #region соединение с БД

            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
            Returns ret = OpenDb(conn_db, true);
            if (!ret.result) return ret;

            #endregion

            #region Определить пользователя
            int nzpUser = finder.nzp_user;   

            /*DbWorkUser db = new DbWorkUser();
            finder.pref = Points.Pref;
            int nzpUser = db.GetLocalUser(conn_db, finder, out ret); //локальный пользователь      
            db.Close();
            if (!ret.result) return ret;*/
            #endregion

            List<Prm> list = new List<Prm>();
            Prm prm = new Prm();
            prm.name_prm = "land_rg";
            prm.val_prm = finder.land;
            list.Add(prm);
            prm = new Prm();
            prm.name_prm = "nzp_land_rg";
            if (finder.nzp_land == 0) prm.val_prm = "";
            else prm.val_prm = finder.nzp_land.ToString();
            list.Add(prm);
            prm = new Prm();
            prm.name_prm = "stat_rg";
            prm.val_prm = finder.stat;
            list.Add(prm);
            prm = new Prm();
            prm.name_prm = "nzp_stat_rg";
            if (finder.nzp_stat == 0) prm.val_prm = "";
            else prm.val_prm = finder.nzp_stat.ToString();
            list.Add(prm);
            prm = new Prm();
            prm.name_prm = "town_rg";
            prm.val_prm = finder.town;
            list.Add(prm);
            prm = new Prm();
            prm.name_prm = "nzp_town_rg";
            if (finder.nzp_town == 0) prm.val_prm = "";
            else prm.val_prm = finder.nzp_town.ToString();
            list.Add(prm);
            prm = new Prm();
            prm.name_prm = "rajon_rg";
            prm.val_prm = finder.rajon;
            list.Add(prm);
            prm = new Prm();
            prm.name_prm = "nzp_raj_rg";
            if (finder.nzp_raj == 0) prm.val_prm = "";
            else prm.val_prm = finder.nzp_raj.ToString();
            list.Add(prm);

            foreach (Prm p in list)
            {
                ret = isParameterExist(p.name_prm, conn_db);
                if (!ret.result)
                {
                    conn_db.Close();
                    return ret;
                }
                if (ret.tag > 0)
                {
                    ret = UpdateParameter(p.name_prm, p.val_prm, nzpUser, conn_db);
                    if (!ret.result)
                    {
                        conn_db.Close();
                        return ret;
                    }
                }
                else
                {
                    ret = InsertParameter(p.name_prm, p.val_prm, nzpUser, conn_db);
                    if (!ret.result)
                    {
                        conn_db.Close();
                        return ret;
                    }
                }
            }
            conn_db.Close();
            return ret;
        }

        private Returns isParameterExist(string p_name, IDbConnection conn_db)
        {
            Returns ret = Utils.InitReturns();
            DbTables tables = new DbTables(conn_db);
#if PG
            string sql = "select count(*) from " + tables.prefer + " where p_name = '" + p_name + "'";
#else
			string sql = "select count(*) from " + tables.prefer + " where p_name = '" + p_name + "'";
#endif
            object cnt = ExecScalar(conn_db, sql, out ret, true);
            int recordsTotCount;
            try
            {
                recordsTotCount = Convert.ToInt32(cnt);
            }
            catch (Exception e)
            {
                ret = new Returns(false,
                    "Ошибка при определении количества записей: " + (Constants.Debug ? e.Message : ""));
                MonitorLog.WriteLog("Ошибка GetOverPayments " + (Constants.Viewerror ? "\n" + e.Message : ""),
                    MonitorLog.typelog.Error, 20, 201, true);
                return ret;
            }
            ret.tag = recordsTotCount;
            return ret;
        }

        private Returns UpdateParameter(string p_name, string p_value, int nzpUser, IDbConnection conn_db)
        {
            DbTables tables = new DbTables(conn_db);
            string sql = "update " + tables.prefer + " set p_value = '" + p_value + "', nzp_user = " + nzpUser +
                         " where p_name = '" + p_name + "' ";
            return ExecSQL(conn_db, sql, true);
        }

        private Returns InsertParameter(string p_name, string p_value, int nzpUser, IDbConnection conn_db)
        {
            DbTables tables = new DbTables(conn_db);
#if PG
            string sql = "insert into " + tables.prefer + " (nzp_user,p_mode,p_type,p_name,p_value) " +
                         " values (" + nzpUser + ",'kart','parameter','" + p_name + "','" + p_value + "') ";
#else
			string sql = "insert into " + tables.prefer + " (nzp_user,p_mode,p_type,p_name,p_value) " +
				" values (" + nzpUser + ",'kart','parameter','" + p_name + "','" + p_value + "') ";
#endif
            return ExecSQL(conn_db, sql, true);
        }

        public Ls LoadAddressPrefer(Finder finder, out Returns ret)
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

            Ls ls = new Ls();
            ret = GetParameter("nzp_land_rg", conn_db);
            if (!ret.result) return null;
            int nzp = 0;
            if (ret.text != "")
            {
                nzp = 0;
                Int32.TryParse(ret.text, out nzp);
                ls.nzp_land = nzp;
            }

            ret = GetParameter("nzp_stat_rg", conn_db);
            if (!ret.result) return null;
            nzp = 0;
            if (ret.text != "")
            {
                nzp = 0;
                Int32.TryParse(ret.text, out nzp);
                ls.nzp_stat = nzp;
            }

            ret = GetParameter("nzp_town_rg", conn_db);
            if (!ret.result) return null;
            nzp = 0;
            if (ret.text != "")
            {
                nzp = 0;
                Int32.TryParse(ret.text, out nzp);
                ls.nzp_town = nzp;
            }

            ret = GetParameter("nzp_raj_rg", conn_db);
            if (!ret.result) return null;
            nzp = 0;
            if (ret.text != "")
            {
                nzp = 0;
                Int32.TryParse(ret.text, out nzp);
                ls.nzp_raj = nzp;
            }

            conn_db.Close();
            return ls;
        }

        private Returns GetParameter(string p_name, IDbConnection conn_db)
        {
            DbTables tables = new DbTables(conn_db);
#if PG
            string sql = "select p_value from " + tables.prefer + " where p_name = '" + p_name +
                         "' and p_mode='kart' and p_type='parameter'";
#else
			string sql = "select p_value from " + tables.prefer + " where p_name = '" + p_name + "' and p_mode='kart' and p_type='parameter'";
#endif
            IDataReader reader;
            Returns ret = ExecRead(conn_db, out reader, sql, true);
            if (!ret.result) return ret;

            string p_value = "";
            if (reader.Read())
                if (reader["p_value"] != DBNull.Value)
                    p_value = ((string)reader["p_value"]).Trim();
            ret.text = p_value;
            reader.Close();
            return ret;
        }

        public GetSelectListDomInfo GetSelectListDomInfo(Finder finder, out Returns ret)
        {
            if (finder.nzp_user < 0)
            {
                ret = new Returns(false, "Не задан пользователь", -1);
                return null;
            }
            List<_Point> prefixs = new List<_Point>();
            prefixs = Points.PointList;

            string dat_s = "'" + new DateTime(Points.CalcMonth.year_, Points.CalcMonth.month_, 1).ToShortDateString() +
                           "'"; //начало тек.расчетного периода

            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result) return null;

            #region соединение с БД

            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return null;

            #endregion

            MyDataReader reader;
            GetSelectListDomInfo info = new GetSelectListDomInfo();
#if PG
            string tXX_spdom = "t" + finder.nzp_user + "_spdom";
            string tXX_spdom_full = sDefaultSchema + tXX_spdom;
#else
			string tXX_spdom = "t" + finder.nzp_user + "_spdom";
			string tXX_spdom_full = conn_web.Database + "@" + DBManager.getServer(conn_db) + ":" + tXX_spdom;
#endif
            conn_web.Close();

            StringBuilder sql = new StringBuilder();
            //список УК
            info.list_area = new List<string>();

            sql.Remove(0, sql.Length);

            sql.Append("select distinct a.area from " + tXX_spdom_full + " d, " + Points.Pref + sDataAliasRest + "kvar k, " +
                        Points.Pref + sDataAliasRest + "s_area a ");
            sql.Append("where d.nzp_dom = k.nzp_dom and k.nzp_area=a.nzp_area and d.mark=1 AND k.is_open='" + (int)Ls.States.Open + "'");

            if (!ExecRead(conn_db, out reader, sql.ToString(), false).result)
            {
                ret.result = false;
                conn_db.Close();
                return null;
            }

            while (reader.Read())
            {
                info.list_area.Add(reader["area"].ToString().Trim());
            }


            //кол-во активных ЛС

            sql.Remove(0, sql.Length);

            sql.Append(" select count(*) from " + tXX_spdom_full + " d, " + Points.Pref + sDataAliasRest + "kvar k");
            sql.Append(" where  d.nzp_dom = k.nzp_dom  and d.mark=1  ");
            sql.Append(" and k.is_open='" + (int)Ls.States.Open + "'");

            object obj = ExecScalar(conn_db, sql.ToString(), out ret, true);
            if (obj != null)
            {
                info.count_ls += Convert.ToInt32(obj);
            }


            sql.Remove(0, sql.Length);
            //кол-во домов
            sql.Append("select count(*) from " + tXX_spdom_full + " d where d.mark=1 ");

            object obj_d = ExecScalar(conn_db, sql.ToString(), out ret, true);
            if (obj_d != null)
            {
                info.count_dom = Convert.ToInt32(obj_d);
            }

            conn_db.Close();
            return info;
        }

        public List<Dom> LoadDom(Dom finder, out Returns ret)
        {
            string connectionString = Points.GetConnByPref(finder.pref);
            IDbConnection conn_db = GetConnection(connectionString);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return null;

            DbTables tables = new DbTables(conn_db);

            string where = "", table_rajon = "";
            table_rajon = ", " + tables.rajon + " r";
            if (finder.ndom != "") where = " and d.ndom = " + Utils.EStrNull(finder.ndom);
            if (finder.nzp_ul > 0) where += " and d.nzp_ul = " + finder.nzp_ul;
            if (finder.nzp_raj > 0) where += " and u.nzp_raj = " + finder.nzp_raj;
            if (finder.nzp_town > 0)
            {
                where += " and r.nzp_town = " + finder.nzp_town;

            }
            if (finder.dopFind != null && finder.dopFind.Count > 0)
            {
                where += " and d.nzp_dom in (" + finder.dopFind[0] + ")";
            }

            if (finder.nzp_wp > 0)
            {
                where += " and d.nzp_wp=" + finder.nzp_wp;
            }

            //Определить общее количество записей
            string sql = "select count(*) from " + tables.dom + " d, " + tables.ulica + " u " + table_rajon
                            + " , " + tables.town + " t " +
                         " where d.nzp_ul = u.nzp_ul  and u.nzp_raj = r.nzp_raj  and r.nzp_town = t.nzp_town " + where;
            object count = ExecScalar(conn_db, sql, out ret, true);
            int recordsTotalCount;
            try
            {
                recordsTotalCount = Convert.ToInt32(count);
            }
            catch (Exception e)
            {
                ret = new Returns(false,
                    "Ошибка при определении количества записей: " + (Constants.Debug ? e.Message : ""));
                MonitorLog.WriteLog("Ошибка LoadDom " + (Constants.Viewerror ? "\n" + e.Message : ""),
                    MonitorLog.typelog.Error, 20, 201, true);
                conn_db.Close();
                return null;
            }

            sql = " select d.nzp_dom, d.ndom, d.nzp_ul, d.nkor, u.ulica, r.rajon, t.town from " + tables.dom + " d, " + tables.ulica +
                  " u " + table_rajon + " , " + tables.town + " t " +
                  " where d.nzp_ul = u.nzp_ul  and u.nzp_raj = r.nzp_raj and r.nzp_town = t.nzp_town " + where;
            IDataReader reader;
            if (!ExecRead(conn_db, out reader, sql, true).result)
            {
                conn_db.Close();
                return null;
            }
            List<Dom> list = new List<Dom>();
            try
            {
                int i = 0;
                while (reader.Read())
                {
                    i++;
                    if (i <= finder.skip) continue;
                    Dom zap = new Dom();
                    if (reader["nzp_dom"] != DBNull.Value) zap.nzp_dom = Convert.ToInt32(reader["nzp_dom"]);
                    if (reader["ndom"] != DBNull.Value) zap.ndom = Convert.ToString(reader["ndom"]).Trim();
                    if (reader["nkor"] != DBNull.Value) zap.nkor = Convert.ToString(reader["nkor"]).Trim();
                    if (reader["ulica"] != DBNull.Value) zap.ulica = Convert.ToString(reader["ulica"]).Trim();
                    if (reader["town"] != DBNull.Value) zap.town = Convert.ToString(reader["town"]).Trim();
                    if (reader["rajon"] != DBNull.Value) zap.rajon = Convert.ToString(reader["rajon"]).Trim();
                    zap.adr = zap.town + "/" + zap.rajon + "/ул. " + zap.ulica + ", д. " + zap.ndom;
                    if (zap.nkor.Trim() != "") zap.adr += ", корп. " + zap.nkor;
                    list.Add(zap);
                    if (finder.rows > 0 && i >= finder.skip + finder.rows) break;
                }

                ret.tag = recordsTotalCount;

                reader.Close();
                conn_db.Close();
                return list;
            }
            catch (Exception ex)
            {
                reader.Close();
                conn_db.Close();
                ret = new Returns(false, ex.Message);
                MonitorLog.WriteLog(
                    "Ошибка заполнения списка уникальных кодов управляющих компаний " +
                    (Constants.Viewerror ? " \n " + ex.Message : ""), MonitorLog.typelog.Error, 20, 201, true);
                return null;
            }
        }

        private bool IsPg()
        {
#if PG
            return true;
#else
			return false;
#endif
        }


        public List<Town> GetTownList(Town finder, out Returns ret)
        {
            var result = new List<Town>();
            string connectionString = Points.GetConnByPref(finder.pref);
            using (IDbConnection connDb = GetConnection(connectionString))
            {
                ret = OpenDb(connDb, true);
                if (!ret.result)
                {
                    return null;
                }

                //var point = Points.PointList.FirstOrDefault(x => x.nzp_wp == finder.nzp_wp);
                //var pref = string.IsNullOrEmpty(point.pref) ? string.Empty : point.pref;

                var command = connDb.CreateCommand();
                command.CommandText = string.Format(@"select 
						t.nzp_town,
						t.town
					from {0}_data{1}s_town t
					where t.nzp_stat = {2}", Points.Pref, tableDelimiter, IsPg() ? ":nzp_stat" : "?");

                var param = command.CreateParameter();
                param.ParameterName = "nzp_stat";
                param.DbType = DbType.Int32;
                param.Value = finder.nzp_stat;

                command.Parameters.Add(param);

                var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    result.Add(new Town
                    {
                        nzp_town = reader.GetInt32(0),
                        town = reader.GetString(1)
                    });
                }
            }

            return result;
        }

        public List<Rajon> GetRajonList(Rajon finder, out Returns ret)
        {
            var result = new List<Rajon>();
            string connectionString = Points.GetConnByPref(finder.pref);
            using (IDbConnection connDb = GetConnection(connectionString))
            {
                ret = OpenDb(connDb, true);
                if (!ret.result)
                {
                    return null;
                }

                //var point = Points.PointList.FirstOrDefault(x => x.nzp_wp == finder.nzp_wp);
                //var pref = string.IsNullOrEmpty(point.pref) ? string.Empty : point.pref;

                var command = connDb.CreateCommand();
                command.CommandText = string.Format(@"select
					t.nzp_raj,
					t.rajon
				from {0}_data{1}s_rajon t
					where t.nzp_town ={2}", Points.Pref, tableDelimiter, IsPg() ? ":nzp_town" : "?");

                var param = command.CreateParameter();
                param.ParameterName = "nzp_town";
                param.DbType = DbType.Int32;
                param.Value = finder.nzp_town;

                command.Parameters.Add(param);

                var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    result.Add(new Rajon()
                    {
                        nzp_raj = reader.GetInt32(0),
                        rajon = reader.GetString(1)
                    });
                }
            }

            return result;
        }

        public Returns GetCountLSBySelectedDom(Group finder)
        {
            Returns ret = Utils.InitReturns();
            if (finder.pref == "")
            {
                ret = new Returns(false, "Префикс не задан", -1);
                return ret;
            }
            if (finder.nzp_user <= 0)
            {
                ret = new Returns(false, "Не задан пользователь", -1);
                return ret;
            }
            IDbConnection conn_db = null;
            try
            {
                string connectionString = Points.GetConnByPref(finder.pref);
                conn_db = GetConnection(connectionString);
                ret = OpenDb(conn_db, true);
                if (!ret.result) return ret;
                int nzp_wp = Points.GetPoint(finder.pref).nzp_wp;
                if (nzp_wp <= 0)
                {
                    return new Returns(false, "Не верные входные параметры. Не определился код банка данных", -1);
                }
                string tabledom = sDefaultSchema + "t" + finder.nzp_user + "_spdom";
                string sql = "select count(*) from " + tabledom + " d, " + Points.Pref + sDataAliasRest + "kvar k " +
                             "where d.nzp_dom=k.nzp_dom and k.is_open='1' and k.nzp_wp=d.nzp_wp and d.mark=1 and d.nzp_wp=" + nzp_wp;
                object count = ExecScalar(conn_db, sql, out ret, true);
                if (!ret.result)
                {
                    return ret;
                }
                if (count == DBNull.Value)
                {
                    ret = new Returns(false, "Для выбранных(ого) домов(а) лицевые счета отсутствуют", -1);
                    return ret;
                }
                int parsed_count;
                if (!Int32.TryParse(count.ToString(), out parsed_count))
                {
                    ret = new Returns(false, "Ошибка получения количества ЛС для выбранных(ого) домов(а)", -1);
                    MonitorLog.WriteLog("GetCountLSBySelectedDom(). Ошибка преобразования количества ЛС по выбранному списку домов ", MonitorLog.typelog.Error, 20, 201, true);
                    return ret;
                }
                ret.tag = parsed_count;
            }
            catch (Exception ex)
            {
                ret = new Returns(false, ex.Message);
            }
            finally
            {
                if (!ret.result)
                {
                    MonitorLog.WriteLog(
                   "GetCountLSBySelectedDom(). Ошибка получения количества ЛС по выбранным домам " +
                   (Constants.Viewerror ? " \n " + ret.text : ""), MonitorLog.typelog.Error, 20, 201, true);
                }
                if (conn_db != null) conn_db.Close();
            }
            return ret;
        }
    }
}
