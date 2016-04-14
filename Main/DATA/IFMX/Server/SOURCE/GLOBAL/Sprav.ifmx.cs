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
    public partial class DbSprav : DbSpravKernel
    //----------------------------------------------------------------------
    {
        public List<string> GetMainPageTitle()
        {
            Returns ret = new Returns();
            List<string> Spis = new List<string>();
            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return null;

            //проверка на существование таблицы
            if (!TempTableInWebCashe(conn_db, "s_point"))
            {
                return Spis;
            }

            //выбрать список
            string sql = String.Format(
                " Select s.point " +
#if PG
                " From  {0}_kernel.s_point s where s.nzp_wp = 1", Points.Pref);
#else
 " From  {0}_kernel:s_point s where s.nzp_wp = 1", Points.Pref);
#endif

            IDataReader reader;
            if (!ExecRead(conn_db, out reader, sql, true).result)
            {
                conn_db.Close();
                return null;
            }
            try
            {
                while (reader.Read())
                {
                    if (reader["point"] != DBNull.Value)
                        Spis.Add((string)reader["point"]);
                }
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

                MonitorLog.WriteLog("Ошибка заполнения  " + err, MonitorLog.typelog.Error, 20, 201, true);

                return null;
            }

            conn_db.Close();
            return Spis;
        }


        

        //----------------------------------------------------------------------
        public Returns WebService()
        //----------------------------------------------------------------------
        {
            return WebService(0, true);
        }
        //----------------------------------------------------------------------
        public Returns WebService(int nzp_server, bool is_insert)
        //----------------------------------------------------------------------
        {
            Returns ret = Utils.InitReturns();

            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result) return ret;

            string srv = "";
            if (nzp_server > 0)
                srv = "_" + nzp_server;

            string services = "services" + srv;
#if PG
            string services_full = "public." + services;
#else
            string services_full = conn_web.Database + "@" + DBManager.getServer(conn_web) + ":" + services;
#endif

            CreateWebService(conn_web, services, out ret);
            conn_web.Close();

            if (is_insert)
            {
                IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
                ret = OpenDb(conn_db, true);
                if (!ret.result)
                {
                    conn_web.Close();
                    return ret;
                }

                //выбрать список
                ret = ExecSQL(conn_db,
                    " Insert into " + services_full + " (nzp_serv, service, service_small, service_name, ed_izmer, type_lgot, nzp_frm, ordering) " +
                    " Select nzp_serv, service, service_small, service_name, ed_izmer, type_lgot, nzp_frm, ordering " +
#if PG
                    " From " + Points.Pref + "_kernel.services", true);
#else
 " From " + Points.Pref + "_kernel:services", true);
#endif

                conn_db.Close();
            }

            return ret;
        }

        //----------------------------------------------------------------------
        public List<_Service> ServiceLoad(Finder finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            return ServiceLoad(finder, "service", out ret);
        }
        public List<_Service> ServiceLoad(Service finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            return ServiceLoad(finder, "ordering", out ret);
        }
        //----------------------------------------------------------------------
        public List<_Service> ServiceLoad(Finder finder, string orderby, out Returns ret)
        {
            Service service = new Service();
            finder.CopyTo(service);
            return ServiceLoad(service, orderby, out ret);
        }

        public List<_Service> ServiceLoad(Service finder, string orderby, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();

            Services spis = new Services();
            spis.ServiceList.Clear();

            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return null;

            if (finder.pref == "") finder.pref = Points.Pref;

            //выбрать список
            string sql =
                " Select nzp_serv, service, service_small, service_name, ed_izmer,nzp_measure, type_lgot, nzp_frm, ordering " +
#if PG
                " From " + finder.pref + "_kernel.services " +
#else
 " From " + finder.pref + "_kernel:services " +
#endif
 " Where 1 = 1 ";

            string where;
            if (finder.nzp_serv > 0) where = " and nzp_serv = " + finder.nzp_serv;
            else where = " and nzp_serv > 1 ";

            if (finder.dopFind != null)
            {
                foreach (string str in finder.dopFind)
                {
                    if (str.Contains("FiltrOnDistrib"))
                    {
                        //фильтровать справочники по fn_distrib
                        where += FiltrOnDistrib("nzp_serv", finder.dopFind);
                    }
                    //фильтр услуг для калькуляции Содержания жилья
                    else if (str.Contains("FiltrCalculationServices"))
                    {
#if PG
                        where += " and nzp_serv in (select nzp_serv from " + Points.Pref + "_kernel.services_sg) ";
#else
                        where += " and nzp_serv in (select nzp_serv from " + Points.Pref + "_kernel:services_sg) ";
#endif

                    }
                    else
                    {
                        where += " and upper(service) like '%" + str.ToUpper().Replace("'", "''").Replace("*", "%") +
                                 "%' ";
                    }
                }
            }

            if (finder.RolesVal != null)
                foreach (_RolesVal role in finder.RolesVal)
                    if (role.tip == Constants.role_sql & role.kod == Constants.role_sql_serv) where += " and nzp_serv in (" + role.val + ")";

            if (finder.orderings != null && finder.orderings.Count > 0)
            {
                string property;
                bool isFirst = true;
                foreach (_OrderingField order in finder.orderings)
                {
                    try
                    {
                        Type type = finder.GetType();
                        System.Reflection.MemberInfo info = type.GetProperty(order.fieldName);
                        if (info != null)
                        {
                            if (info.Name == "ordering") property = "ordering";
                            else if (info.Name == "service") property = "service";
                            else if (info.Name == "service_small") property = "service_small";
                            else if (info.Name == "service_name") property = "service_name";
                            else if (info.Name == "ed_izmer") property = "ed_izmer";
                            else if (info.Name == "nzp_measure") property = "nzp_measure";
                            else property = "";

                            if (property != "")
                            {
                                if (isFirst)
                                {
                                    sql += " Order by";
                                    isFirst = false;
                                }
                                else sql += ", ";
                                sql += " " + property + " " + (order.orderingDirection == OrderingDirection.Ascending ? " asc" : "desc");
                            }
                        }
                    }
                    catch { }
                }
            }
            else
                sql += where + " Order by " + orderby;

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
                    _Service zap = new _Service();

                    if (reader["nzp_serv"] == DBNull.Value)
                        zap.nzp_serv = 0;
                    else
                        zap.nzp_serv = (int)reader["nzp_serv"];
                    if (reader["service"] == DBNull.Value)
                        zap.service = "";
                    else
                    {
                        zap.service = (string)reader["service"];
                        zap.service = zap.service.Trim();
                    }

                    //service_small, service_name, ed_izmer, type_lgot, nzp_frm, ordering "+
                    if (reader["service_small"] != DBNull.Value) zap.service_small = Convert.ToString(reader["service_small"]).Trim();
                    if (reader["service_name"] != DBNull.Value) zap.service_name = Convert.ToString(reader["service_name"]).Trim();
                    if (reader["ed_izmer"] != DBNull.Value) zap.ed_izmer = Convert.ToString(reader["ed_izmer"]).Trim();
                    if (reader["nzp_measure"] != DBNull.Value) zap.nzp_measure = Convert.ToInt32(reader["nzp_measure"]);
                    if (reader["type_lgot"] != DBNull.Value) zap.type_lgot = Convert.ToInt32(reader["type_lgot"]);
                    if (reader["nzp_frm"] != DBNull.Value) zap.nzp_frm = Convert.ToInt32(reader["nzp_frm"]);
                    if (reader["ordering"] != DBNull.Value) zap.ordering = Convert.ToInt32(reader["ordering"]);

                    spis.ServiceList.Add(zap);
                    if (finder.rows > 0 && i >= finder.skip + finder.rows) break;
                }

                reader.Close();

#if PG
                sql = " Select count(*) From " + finder.pref + "_kernel.services Where 1 = 1 " + where;
#else
                sql = " Select count(*) From " + finder.pref + "_kernel:services Where 1 = 1 " + where;
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
                        ret.result = false;
                        ret.text = ex.Message;
                        return null;
                    }
                }

                conn_db.Close();
                return spis.ServiceList;
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

                MonitorLog.WriteLog("Ошибка заполнения справочника услуг " + err, MonitorLog.typelog.Error, 20, 201, true);

                return null;
            }
        }

        //---------------------------------------------------------------------------------------------------------
        public List<_Service> CountsLoad(Finder finder, out Returns ret)
        //---------------------------------------------------------------------------------------------------------
        {
            return CountsLoad(finder, out ret, 0);
        }

        //--------------------------------------------------------------------------------------------------------------------
        public List<_Service> CountsLoad(Finder finder, out Returns ret, int nzp_kvar)
        //--------------------------------------------------------------------------------------------------------------------
        {
            ret = Utils.InitReturns();

            Services spis = new Services();
            spis.CountsList.Clear();

            string conn_kernel = Points.GetConnByPref(finder.pref);
            IDbConnection conn_db = GetConnection(conn_kernel);
            //IDbConnection conn_db = GetConnection(Constants.cons_Kernel);

            ret = OpenDb(conn_db, true);
            if (!ret.result) return null;

            //выбрать список
            IDataReader reader;
            string sql = "", where = "";

            if (finder.RolesVal != null)
                foreach (_RolesVal role in finder.RolesVal)
                    if (role.tip == Constants.role_sql & role.kod == Constants.role_sql_serv) where += " and c.nzp_serv in (" + role.val + ")";

            if (finder.pref.Trim() != "")
            {
                ret = ExecSQL(conn_db, "drop table serv_tarif", false);

#if PG
                sql = "CREATE TEMP TABLE serv_tarif AS SELECT DISTINCT nzp_serv FROM " + finder.pref.Trim() + "_data.tarif ";
                if (nzp_kvar > 0) sql += " WHERE nzp_kvar= " + nzp_kvar;
#else
                sql = "select unique nzp_serv from " + finder.pref.Trim() + "_data:tarif ";
                if (nzp_kvar > 0) sql += " where nzp_kvar= " + nzp_kvar;
                sql += " into temp serv_tarif ";
#endif

                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result)
                {
                    conn_db.Close();
                    return null;
                }
                sql = " Select distinct c.nzp_serv, c.name, c.nzp_cnt, m.measure " +
#if PG
                      " From " + finder.pref.Trim() + "_kernel.s_counts c, serv_tarif t, " + finder.pref.Trim() + "_kernel.s_measure m " +
#else
 " From " + finder.pref.Trim() + "_kernel:s_counts c, serv_tarif t, outer " + finder.pref.Trim() + "_kernel:s_measure m " +
#endif
 " Where t.nzp_serv = c.nzp_serv and c.nzp_measure = m.nzp_measure " + where +
                      " Union all " +
                      " Select distinct c.nzp_serv, c.name, c.nzp_cnt, m.measure " +
#if PG
                      " From " + finder.pref.Trim() + "_kernel.s_countsdop c, serv_tarif t, " + finder.pref.Trim() + "_kernel.s_measure m " +
#else
 " From " + finder.pref.Trim() + "_kernel:s_countsdop c, serv_tarif t, outer " + finder.pref.Trim() + "_kernel:s_measure m " +
#endif
 " Where t.nzp_serv = c.nzp_serv and c.nzp_measure = m.nzp_measure " + where +
                      " Order by 2";
            }
            else
                sql = " Select c.nzp_serv, c.name, c.nzp_cnt, m.measure " +
#if PG
                      " From " + Points.Pref + "_kernel.s_counts c, " + Points.Pref + "_kernel.s_measure m " +
#else
 " From " + Points.Pref + "_kernel:s_counts c, outer " + Points.Pref + "_kernel:s_measure m " +
#endif
 " Where c.nzp_measure = m.nzp_measure " + where +
                      " Order by c.name ";

            ret = ExecRead(conn_db, out reader, sql, true);
            if (!ret.result)
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
                    _Service zap = new _Service();

                    if (reader["nzp_serv"] == DBNull.Value)
                        zap.nzp_serv = 0;
                    else
                        zap.nzp_serv = (int)reader["nzp_serv"];

                    if (reader["nzp_cnt"] == DBNull.Value)
                        zap.nzp_cnt = 0;
                    else
                        zap.nzp_cnt = (int)reader["nzp_cnt"];

                    if (reader["name"] == DBNull.Value)
                        zap.service = "";
                    else
                        zap.service = ((string)reader["name"]).Trim();

                    if (reader["measure"] != DBNull.Value)
                        zap.service += " (" + ((string)reader["measure"]).Trim() + ")";

                    spis.CountsList.Add(zap);
                }

                ret.tag = i;

                reader.Close();

                if (finder.pref.Trim() != "")
                {
                    ExecSQL(conn_db, "drop table serv_tarif", false);
                }
                conn_db.Close();
                return spis.CountsList;
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

                MonitorLog.WriteLog("Ошибка заполнения справочника услуг счетчика " + err, MonitorLog.typelog.Error, 20, 201, true);

                return null;
            }
        }

        //----------------------------------------------------------------------
        public Returns WebSupplier()
        //----------------------------------------------------------------------
        {
            return WebSupplier(0, true);
        }
        //----------------------------------------------------------------------
        public Returns WebSupplier(int nzp_server, bool is_insert)
        //----------------------------------------------------------------------
        {
            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            Returns ret = OpenDb(conn_web, true);
            if (!ret.result) return ret;

            string srv = "";
            if (nzp_server > 0)
                srv = "_" + nzp_server;

            string supplier = "supplier" + srv;
#if PG
            string supplier_full = "public." + supplier;
#else
            string supplier_full = conn_web.Database + "@" + DBManager.getServer(conn_web) + ":" + supplier;
#endif

            CreateWebSupplier(conn_web, supplier, out ret);
            conn_web.Close();

            if (is_insert)
            {
                IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
                ret = OpenDb(conn_db, true);
                if (!ret.result)
                {
                    return ret;
                }

                //выбрать список
                ret = ExecSQL(conn_db,
                    " Insert into " + supplier_full + " ( nzp_supp, name_supp, adres_supp, phone_supp, geton_plat, have_proc, kod_supp ) " +
                    " Select nzp_supp, name_supp, adres_supp, phone_supp, geton_plat, have_proc, kod_supp " +
#if PG
                    " From " + Points.Pref + "_kernel.supplier", true);
#else
 " From " + Points.Pref + "_kernel:supplier", true);
#endif

                conn_db.Close();
            }

            return ret;
        }

        public List<_Supplier> SupplierLoad(Supplier finder, enTypeOfSupp type, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            if (finder.pref == "") finder.pref = Points.Pref;
            List<_Supplier> spis = new List<_Supplier>();

            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return null;

            DbTables tables = new DbTables(DBManager.getServer(conn_db));

            // Условия поиска
            string where = "";

            if (finder.nzp_supp != 0) where += " and s.nzp_supp = " + finder.nzp_supp;

            if (finder.dopFind != null && finder.dopFind.Count > 0)
                where += " and upper(s.name_supp) like '%" + finder.dopFind[0].ToUpper().Replace("'", "''").Replace("*", "%") + "%' ";

            if (finder.RolesVal != null)
                foreach (_RolesVal role in finder.RolesVal)
                    if (role.tip == Constants.role_sql && role.kod == Constants.role_sql_supp) where += " and s.nzp_supp in (" + role.val + ")";

            if (type == enTypeOfSupp.NotInListPayers)
            {
#if PG
                where += " and s.nzp_supp not in ( Select distinct p.nzp_supp From " + tables.payer + " p Where p.nzp_type = 2)";
#else
                where += " and s.nzp_supp not in ( Select unique p.nzp_supp From " + tables.payer + " p Where p.nzp_type = 2)";
#endif
            }

            //Определить общее количество записей
#if PG
            string sql = "Select count(*) From " + finder.pref + "_kernel.supplier s Where 1 = 1 " + where;
#else
            string sql = "Select count(*) From " + finder.pref + "_kernel:supplier s Where 1 = 1 " + where;
#endif
            object count = ExecScalar(conn_db, sql, out ret, true);
            int recordsTotalCount;
            try { recordsTotalCount = Convert.ToInt32(count); }
            catch (Exception e)
            {
                ret = new Returns(false, "Ошибка при определении количества записей: " + (Constants.Debug ? e.Message : ""));
                MonitorLog.WriteLog("Ошибка SupplierLoad " + (Constants.Viewerror ? "\n" + e.Message : ""), MonitorLog.typelog.Error, 20, 201, true);
                conn_db.Close();
                return null;
            }

            //выбрать список
            if (!Points.isFinances)
            {
#if PG
                sql = " select distinct s.nzp_supp, s.name_supp, 0 as nzp_payer, s.adres_supp, s.phone_supp, s.geton_plat, s.have_proc, s.kod_supp " +
                      " from " + finder.pref + "_kernel.supplier s " +
#else
                sql = " Select unique s.nzp_supp, s.name_supp, 0 as nzp_payer, s.adres_supp, s.phone_supp, s.geton_plat, s.have_proc, s.kod_supp " +
                      " From " + finder.pref + "_kernel:supplier s " +
#endif
 " Where 1 = 1 " + where +
                      " Order by name_supp";
            }
            else
            {
#if PG
                sql =
                    new StringBuilder(
                        " Select distinct s.nzp_supp, s.name_supp, p.nzp_payer, s.adres_supp, s.phone_supp, s.geton_plat, s.have_proc, s.kod_supp ")
                        .Append(" From " + finder.pref + "_kernel.supplier s left outer join " + Points.Pref + "_kernel.s_payer p ")
                        .Append(" on s.nzp_supp = p.nzp_supp ")
                        .Append(where.ReplaceFirstOccurence("and", "where"))
                        .Append(" Order by name_supp")
                        .ToString();
#else
                sql = " Select unique s.nzp_supp, s.name_supp, p.nzp_payer, s.adres_supp, s.phone_supp, s.geton_plat, s.have_proc, s.kod_supp " +
                      " From " + finder.pref + "_kernel:supplier s, outer " + Points.Pref + "_kernel:s_payer p " +
                      " Where s.nzp_supp = p.nzp_supp " + where +
                      " Order by name_supp";
#endif

            }

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
                    _Supplier zap = new _Supplier();
                    zap.num = i;

                    if (reader["nzp_supp"] != DBNull.Value) zap.nzp_supp = Convert.ToInt32(reader["nzp_supp"]);
                    if (reader["name_supp"] != DBNull.Value) zap.name_supp = Convert.ToString(reader["name_supp"]).Trim();
                    if (reader["nzp_payer"] != DBNull.Value) zap.nzp_payer = Convert.ToInt32(reader["nzp_payer"]);

                    if (reader["adres_supp"] != DBNull.Value) zap.adres_supp = Convert.ToString(reader["adres_supp"]).Trim();
                    if (reader["geton_plat"] != DBNull.Value) zap.geton_plat = Convert.ToString(reader["geton_plat"]).Trim();
                    if (reader["have_proc"] != DBNull.Value) zap.have_proc = Convert.ToInt32(reader["have_proc"]);
                    if (reader["kod_supp"] != DBNull.Value) zap.kod_supp = Convert.ToString(reader["kod_supp"]).Trim();

                    spis.Add(zap);
                    if (finder.rows > 0 && i >= finder.skip + finder.rows) break;
                }

                ret.tag = recordsTotalCount;

                reader.Close();
                conn_db.Close();
                return spis;
            }
            catch (Exception ex)
            {
                reader.Close();
                conn_db.Close();
                ret = new Returns(false, ex.Message);
                MonitorLog.WriteLog("Ошибка заполнения списка поставщиков " + (Constants.Viewerror ? " \n " + ex.Message : ""), MonitorLog.typelog.Error, 20, 201, true);
                return null;
            }
        }

        

        
        //----------------------------------------------------------------------
        public Returns SaveBank(Bank finder)
        //----------------------------------------------------------------------
        {
            #region проверка входных параметров
            if (finder.nzp_user < 1) return new Returns(false, "Не определен пользователь");
            if ((finder.bank ?? "").Trim() == "") return new Returns(false, "Не задано наименование банка", -1);
            #endregion

            #region подключение к базе
            string conn_kernel = Points.GetConnByPref(Points.Pref);
            IDbConnection conn_db = GetConnection(conn_kernel);
            Returns ret = OpenDb(conn_db, true);
            if (!ret.result) return ret;
            #endregion

            if (finder.pref == "") finder.pref = Points.Pref;
            finder.bank = finder.bank.Trim();
#if PG
            string table = finder.pref + "_kernel.s_bank";
#else
            string table = finder.pref + "_kernel:s_bank";
#endif
            string sql;

            string sql_bank;
            if ((finder.short_name == null) || (finder.short_name == ""))
            {
                sql_bank = "select COUNT(*) cnt from " + table + " where (bank = " + Utils.EStrNull(finder.bank ?? "").Trim() + ") AND nzp_bank <> " + finder.nzp_bank.ToString();
            }
            else
            {
                sql_bank = "select COUNT(*) cnt from " + table + " where ((bank = " + Utils.EStrNull(finder.bank ?? "").Trim() + ") OR (short_name = " + Utils.EStrNull(finder.short_name ?? "").Trim() + ")) AND nzp_bank <> " + finder.nzp_bank.ToString();
            }
            object count = ExecScalar(conn_db, sql_bank, out ret, true);
            if (!ret.result)
            {
                conn_db.Close();
                ret.tag = 0;
                return ret;
            }
            if (Convert.ToInt32(count) > 0)
            {
                ret.result = false;
                ret.text = "Обнаружено дублирование наименований банков";
                ret.tag = -1;
                conn_db.Close();
                return ret;
            }

            bool is_new = false;

            IDataReader reader;
            if (finder.nzp_bank != 0)
            {
                sql = "select nzp_bank from " + table + " where nzp_bank = " + finder.nzp_bank.ToString();
                ret = ExecRead(conn_db, out reader, sql, true);
                if (!ret.result)
                {
                    conn_db.Close();
                    ret.tag = 0;
                    return ret;
                }
                if (reader.Read())
                {
                    sql = " update " + table + " set " +
                          " bank = " + Utils.EStrNull(finder.bank) + ", " +
                          " nzp_payer = " + Utils.EStrNull(finder.nzp_payer.ToString()) + ", " +
                          " short_name = " + Utils.EStrNull(finder.short_name.ToString()) + ", " +
                          " adress = " + Utils.EStrNull(finder.adress.ToString()) + ", " +
                          " phone = " + Utils.EStrNull(finder.phone.ToString()) + " " +
                          " where nzp_bank = " + finder.nzp_bank.ToString();
                }
                else
                {
                    is_new = true;
                    sql = " insert into " + table + " (bank, nzp_payer, short_name, adress, phone) " +
                          " values (" + Utils.EStrNull(finder.bank.ToString()) + ", " +
                          Utils.EStrNull(finder.nzp_payer.ToString()) + ", " +
                          Utils.EStrNull(finder.short_name.ToString()) + ", " +
                          Utils.EStrNull(finder.adress.ToString()) + ", " +
                          Utils.EStrNull(finder.phone.ToString()) + ")";
                }
                CloseReader(ref reader);
            }
            else
            {
                is_new = true;
                sql = " insert into " + table + " (bank, nzp_payer, short_name, adress, phone) " +
                      " values (" + Utils.EStrNull(finder.bank.ToString()) + ", " +
                      Utils.EStrNull(finder.nzp_payer.ToString()) + ", " +
                      Utils.EStrNull(finder.short_name.ToString()) + ", " +
                      Utils.EStrNull(finder.adress.ToString()) + ", " +
                      Utils.EStrNull(finder.phone.ToString()) + ")";
            }

            if ((is_new) && (finder.nzp_bank > 0))
            {
                string sql_kod_bank = " select count(*) cnt from " + table + " where nzp_bank = " + finder.nzp_bank.ToString();
                object count_kod_bank = ExecScalar(conn_db, sql_kod_bank, out ret, true);
                if (!ret.result)
                {
                    conn_db.Close();
                    ret.tag = 0;
                    return ret;
                }
                if (Convert.ToInt32(count_kod_bank) > 0)
                {
                    ret.result = false;
                    ret.text = "Неверный код - такой код уже определен";
                    ret.tag = -1;
                    conn_db.Close();
                    return ret;
                }
            }

            ret = ExecSQL(conn_db, sql, true);
            if (ret.result && finder.nzp_bank == 0)
            {
                ret.tag = GetSerialValue(conn_db);
            }

            conn_db.Close();
            return ret;
        }

        //----------------------------------------------------------------------
        public Returns WebPayer()
        //----------------------------------------------------------------------
        {
            return WebPayer(0, true);
        }
        //----------------------------------------------------------------------
        public Returns WebPayer(int nzp_server, bool is_insert)
        //----------------------------------------------------------------------
        {
            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            Returns ret = OpenDb(conn_web, true);
            if (!ret.result) return ret;

            string srv = "";
            if (nzp_server > 0)
                srv = "_" + nzp_server;

            string payer = "s_payer" + srv;
#if PG
            string payer_full = "public." + payer;
#else
            string payer_full = conn_web.Database + "@" + DBManager.getServer(conn_web) + ":" + payer;
#endif

            CreateWebSupplier(conn_web, payer, out ret);
            conn_web.Close();

            if (is_insert)
            {
                IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
                ret = OpenDb(conn_db, true);
                if (!ret.result)
                {
                    return ret;
                }

                //выбрать список
                ret = ExecSQL(conn_db,
                    " Insert into " + payer_full + " ( nzp_payer, payer, npayer, nzp_supp, is_erc ) " +
                    " Select nzp_payer, payer, npayer, nzp_supp, is_erc " +
#if PG
                    " From " + Points.Pref + "_kernel.s_payer", true);
#else
 " From " + Points.Pref + "_kernel:s_payer", true);
#endif

                conn_db.Close();
            }

            return ret;
        }

        public List<Payer> LoadPayerTypes(Finder finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            if (finder.nzp_user <= 0)
            {
                ret.result = false;
                ret.text = "Не задан пользователь";
                return null;
            }

            string sql = "";

            string connectionString = Points.GetConnByPref(finder.pref);
            IDbConnection conn_db = GetConnection(connectionString);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return null;

            IDataReader reader = null;

            List<Payer> list = new List<Payer>();

            DbTables tables = new DbTables(DBManager.getServer(conn_db));
            try
            {
                sql = "select nzp_payer_type, type_name " +
                    " from " + tables.payer_types + " order by  type_name";
                ret = ExecRead(conn_db, out reader, sql, true);
                if (!ret.result)
                {
                    conn_db.Close();
                    return null;
                }

                while (reader.Read())
                {
                    Payer p = new Payer();
                    if (reader["nzp_payer_type"] != DBNull.Value) p.nzp_type = Convert.ToInt32(reader["nzp_payer_type"]);
                    if (reader["type_name"] != DBNull.Value) p.type_name = Convert.ToString(reader["type_name"]);
                    list.Add(p);
                }
                reader.Close();
                return list;
            }
            catch (Exception ex)
            {
                CloseReader(ref reader);
                conn_db.Close();

                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;

                MonitorLog.WriteLog("Ошибка получения LoadPayerTypes " + err, MonitorLog.typelog.Error, 20, 201, true);
                return null;
            }
        }
        //----------------------------------------------------------------------
        public List<Payer> PayerBankLoad(Payer finder, enSrvOper oper, out Returns ret)
        //----------------------------------------------------------------------
        {
            if (oper != enSrvOper.Bank &&
                oper != enSrvOper.Payer &&
                oper != enSrvOper.PayerReferencedFromBank)
            {
                ret = new Returns(false, "Неверные входные параметры");
                return null;
            }

            ret = Utils.InitReturns();

            List<Payer> spis = new List<Payer>();

            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return null;

            DbTables tables = new DbTables(DBManager.getServer(conn_db));

            string wherePayer = "", whereSupp = "";
            if (finder.RolesVal != null)
            {
                foreach (_RolesVal role in finder.RolesVal)
                {
                    if (role.tip == Constants.role_sql && role.kod == Constants.role_sql_payer)
                        wherePayer += " and p.nzp_payer in (" + role.val + ")";
                    else
                        if (role.tip == Constants.role_sql && role.kod == Constants.role_sql_supp)
                        {
                            if (!string.IsNullOrEmpty(whereSupp))
                                whereSupp += " and p.nzp_supp in (" + role.val + ")";
                            else
                                whereSupp += " and p.nzp_supp in (" + role.val + ")";
                        }
                }
            }

            //выбрать список
            //string where = " and exists ( Select * From " + Points.Pref + "_kernel:s_bank Where nzp_payer = p.nzp_payer ) ";
            if (finder.payer != "")
                wherePayer += " and upper(p.payer) like '%" + finder.payer.ToUpper().Replace("'", "''").Replace("*", "%") + "%'";
            if (finder.nzp_supp > 0)
                whereSupp += " and p.nzp_supp = " + finder.nzp_supp;
            if (finder.nzp_payer > 0)
                wherePayer += " and p.nzp_payer = " + finder.nzp_payer;
            if (finder.nzp_type > 0)
                wherePayer += " and p.nzp_type = " + finder.nzp_type;

            string sql = "";
            string orderBy = "";
            string filtrOnDistrib = "";
            string where = wherePayer + whereSupp;

            ExecSQL(conn_db, "drop table t_payers", false);

            switch (oper)
            {
                case enSrvOper.Bank:
                    //фильтровать справочники по fn_distrib
                    filtrOnDistrib = FiltrOnDistrib("p.nzp_payer", "nzp_bank", finder.dopFind);
                    wherePayer += filtrOnDistrib;

#if PG
                    sql = " Select distinct p.nzp_payer, p.payer, 0 as id_bc_type, p.npayer, p.nzp_supp, s.name_supp, p.is_erc, p.inn, p.kpp, p.nzp_type, pt.type_name " +
                            ", max( case when coalesce(b.nzp_payer,0) > 0 then 1 else 0 end ) as is_bank, b.nzp_bank, b.bank, b.short_name, b.adress, b.phone " +
                        " into temp t_payers" +
                        " From " + tables.bank + " b";
                    if (where == "")
                    {
                        sql += " left outer join " + tables.payer + " p on p.nzp_payer = b.nzp_payer " 
                            + "left outer join " + tables.supplier + " s on p.nzp_supp = s.nzp_supp " 
                            + "left outer join " + tables.payer_types + " pt on p.nzp_type = pt.nzp_payer_type ";
                    }
                    else if (wherePayer != "" && whereSupp != "")
                    {
                        sql += ", " + tables.payer + " p, " + tables.supplier + " s left outer join " + tables.payer_types + " pt on p.nzp_type = pt.nzp_payer_type" +
                            " Where p.nzp_payer = b.nzp_payer and p.nzp_supp = s.nzp_supp " + where;
                    }
                    else
                    {
                        sql += ", " + tables.payer + " p left outer join " + tables.supplier + " s on p.nzp_supp = s.nzp_supp left outer join " + tables.payer_types + " pt on p.nzp_type = pt.nzp_payer_type" +
                            " Where p.nzp_payer = b.nzp_payer " + where;
                    }
                    sql +=  " Group by 1,2,3,4,5,6,7,8,9,10,11,13,14,15,16,17 ";
#else
                    sql = " Select unique p.nzp_payer, p.payer, 0 as id_bc_type, p.npayer, p.nzp_supp, s.name_supp, p.is_erc, p.inn, p.kpp, p.nzp_type, pt.type_name " +
                        ", max( case when nvl(b.nzp_payer,0) > 0 then 1 else 0 end ) as is_bank, b.nzp_bank, b.bank, b.short_name, b.adress, b.phone " +
                        " From " + tables.bank + " b";
                    if (where == "")
                    {
                        sql += ", outer (" + tables.payer + " p, outer " + tables.supplier + " s, outer " + tables.payer_types + " pt) ";
                    }
                    else if (wherePayer != "" && whereSupp != "")
                    {
                        sql += ", " + tables.payer + " p, " + tables.supplier + " s, outer " + tables.payer_types + " pt";
                    }
                    else
                    {
                        sql += ", " + tables.payer + " p, outer " + tables.supplier + " s, outer " + tables.payer_types + " pt";
                    }

                    if (finder.bank != "") where += " and upper(b.bank) like upper('%" + finder.bank + "%') ";
                    sql += " Where p.nzp_payer = b.nzp_payer and p.nzp_supp = s.nzp_supp and p.nzp_type = pt.nzp_payer_type " + where +
                        " Group by 1,2,3,4,5,6,7,8,9,10,11,13,14,15,16,17 " +
                        " into temp t_payers";
#endif
                    orderBy = " Order by bank, payer";
                    break;

                case enSrvOper.Payer:
                    //фильтровать справочники по fn_distrib
                    filtrOnDistrib = FiltrOnDistrib("nzp_payer", finder.dopFind);
                    where += filtrOnDistrib;
                    //фильтровать по типу контрагентов
                    if (finder.exclude_types != null)
                    {
                        for (int i = 0; i < finder.exclude_types.Count; i++)
                        {
#if PG
                            where += " and coalesce(p.nzp_type,0)<>" + finder.exclude_types[i];
#else
                            where += " and nvl(p.nzp_type,0)<>" + finder.exclude_types[i];
#endif
                        }
                    }
                    if (finder.include_types != null)
                    {
                        string usl = "";
                        for (int i = 0; i < finder.include_types.Count; i++)
                        {
                            if (i == 0) usl = finder.include_types[i].ToString();
                            else usl += "," + finder.include_types[i];
                        }
                        if (usl != "")
                        {
#if PG
                            where += " and coalesce(p.nzp_type,0) in " + "("+usl +")";
#else
                            where += " and nvl(p.nzp_type,0) in (" + usl + ")";
#endif
                        }
                    }

#if PG
                    sql =
                        " Select p.nzp_payer, p.payer, p.npayer, p.nzp_supp, p.id_bc_type, s.name_supp, p.is_erc, p.inn, p.kpp, p.nzp_type, pt.type_name, " +
                        " 0 is_bank, 0 nzp_bank, ''::char bank, ''::char short_name, ''::char adress, ''::char phone " +
                        " into temp t_payers" +
                        " From " + tables.payer + " p left outer join " + tables.supplier + " s on p.nzp_supp = s.nzp_supp " +
                            "left outer join " + tables.payer_types + " pt on p.nzp_type = pt.nzp_payer_type" +
                            (string.IsNullOrEmpty(where) ? "" : (" Where 1=1 " + where));
#else
                    sql =
                        " Select p.nzp_payer, p.payer, p.npayer, p.nzp_supp, p.id_bc_type, s.name_supp, p.is_erc, p.inn, p.kpp, p.nzp_type, pt.type_name, " +
                        " 0 is_bank, 0 nzp_bank, '' bank, '' short_name, '' adress, '' phone " +
                        " From " + tables.payer + " p, outer " + tables.supplier + " s " +
                            ", outer " + tables.payer_types + " pt" +
                        " Where p.nzp_supp = s.nzp_supp and p.nzp_type = pt.nzp_payer_type " + where +
                        " into temp t_payers";
#endif
                    orderBy = " Order by payer";
                    break;

                case enSrvOper.PayerReferencedFromBank:
                    //фильтровать справочники по fn_distrib
                    filtrOnDistrib = FiltrOnDistrib("p.nzp_payer", "nzp_payer", finder.dopFind);
                    where += filtrOnDistrib;

                    sql =

#if PG
 " Select distinct p.nzp_payer, p.payer, p.npayer, p.nzp_supp, p.inn, 0 as id_bc_type, p.kpp, p.nzp_type, pt.type_name, s.name_supp, " +
                        " p.is_erc, 0 is_bank, b.nzp_bank, b.bank, b.short_name, b.adress, b.phone " +
                        " into temp t_payers "+
                        " From " +  tables.bank + " b, "+tables.payer + " p  "+
                        " left outer join " + tables.supplier + " s on p.nzp_supp = s.nzp_supp " +
                        " left outer join " + tables.payer_types + " pt on p.nzp_type = pt.nzp_payer_type " +   
                        " Where p.nzp_payer = b.nzp_payer  " + where;

#else
 " Select unique p.nzp_payer, p.payer, p.npayer, p.nzp_supp, p.inn, 0 as id_bc_type, p.kpp, p.nzp_type, pt.type_name, s.name_supp, " +
                        " p.is_erc, 0 is_bank, b.nzp_bank, b.bank, b.short_name, b.adress, b.phone " +
                        " From " + tables.payer + " p,  " + tables.bank + " b, outer " + tables.supplier + " s " +
                            ", outer " + tables.payer_types + " pt" +
                        " Where p.nzp_payer = b.nzp_payer and p.nzp_supp = s.nzp_supp and p.nzp_type = pt.nzp_payer_type " + where +
                        " into temp t_payers";
#endif
                    orderBy = " Order by payer";
                    break;
            }

            ret = ExecSQL(conn_db, sql, true);
            if (!ret.result)
            {
                conn_db.Close();
                return null;
            }

            sql = "select * from t_payers " + orderBy;

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
                    Payer zap = new Payer();

                    if (reader["nzp_payer"] != DBNull.Value) zap.nzp_payer = Convert.ToInt32(reader["nzp_payer"]);
                    if (reader["payer"] != DBNull.Value) zap.payer = Convert.ToString(reader["payer"]).Trim();
                    if (reader["npayer"] != DBNull.Value) zap.npayer = Convert.ToString(reader["npayer"]).Trim();
                    if (reader["nzp_supp"] != DBNull.Value) zap.nzp_supp = Convert.ToInt32(reader["nzp_supp"]);
                    if (reader["name_supp"] != DBNull.Value) zap.name_supp = Convert.ToString(reader["name_supp"]).Trim();
                    if (reader["is_erc"] != DBNull.Value) zap.is_erc = Convert.ToInt32(reader["is_erc"]);
                    if (reader["inn"] != DBNull.Value) zap.inn = Convert.ToString(reader["inn"]).Trim();
                    if (reader["kpp"] != DBNull.Value) zap.kpp = Convert.ToString(reader["kpp"]).Trim();
                    if (reader["nzp_type"] != DBNull.Value) zap.nzp_type = Convert.ToInt32(reader["nzp_type"]);
                    if (reader["type_name"] != DBNull.Value) zap.type_name = Convert.ToString(reader["type_name"]).Trim();
                    if (reader["is_bank"] != DBNull.Value) zap.is_bank = Convert.ToInt32(reader["is_bank"]);
                    if (reader["nzp_bank"] != DBNull.Value) zap.nzp_bank = Convert.ToInt32(reader["nzp_bank"]);
                    if (reader["bank"] != DBNull.Value) zap.bank = Convert.ToString(reader["bank"]).Trim();
                    if (reader["short_name"] != DBNull.Value) zap.short_name = Convert.ToString(reader["short_name"]).Trim();
                    if (reader["adress"] != DBNull.Value) zap.adress = Convert.ToString(reader["adress"]).Trim();
                    if (reader["phone"] != DBNull.Value) zap.phone = Convert.ToString(reader["phone"]).Trim();
                    if (reader["id_bc_type"] != DBNull.Value) zap.id_bc_type = Convert.ToInt32(reader["id_bc_type"]);

                    spis.Add(zap);
                    if (finder.rows > 0 && i >= finder.skip + finder.rows) break;
                }
                reader.Close();

                sql = "select count(*) from t_payers";
                object count = ExecScalar(conn_db, sql, out ret, true);
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

                ExecSQL(conn_db, "drop table t_payers", false);

                conn_db.Close();
                return spis;
            }
            catch (Exception ex)
            {
                reader.Close();
                conn_db.Close();

                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;

                MonitorLog.WriteLog("Ошибка заполнения списка подрядчиков " + err, MonitorLog.typelog.Error, 20, 201, true);

                return null;
            }
        }

        //----------------------------------------------------------------------
        public Returns WebPoint()
        //----------------------------------------------------------------------
        {
            return WebPoint(0, true);
        }
        //----------------------------------------------------------------------
        public Returns WebPoint(int nzp_server, bool is_insert)
        //----------------------------------------------------------------------
        {
            Returns ret = Utils.InitReturns();

            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result) return ret;

            string srv = "";
            if (nzp_server > 0)
                srv = "_" + nzp_server;

            string s_point = "s_point" + srv;
#if PG
            string s_point_full = "public." + s_point;
#else
            string s_point_full = conn_web.Database + "@" + DBManager.getServer(conn_web) + ":" + s_point;
#endif

            CreateWebPoint(conn_web, s_point, out ret);
            conn_web.Close();

            if (is_insert)
            {
                IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
                ret = OpenDb(conn_db, true);
                if (!ret.result)
                {
                    conn_web.Close();
                    return ret;
                }

                //выбрать список
                ret = ExecSQL(conn_db,
                    " Insert into " + s_point_full + " (nzp_wp, nzp_graj, n, point, bd_kernel, bd_old, flag) " +
#if PG
                    " Select nzp_wp, nzp_graj, n, point, bd_kernel, bd_old, flag From " + Points.Pref + "_kernel.s_point", true);
#else
 " Select nzp_wp, nzp_graj, n, point, bd_kernel, bd_old, flag From " + Points.Pref + "_kernel:s_point", true);
#endif

                if (!ret.result)
                {
                    //значит это локальный банк
                    ret = ExecSQL(conn_db,
                    " Insert into " + s_point_full + " ( nzp_wp, nzp_graj, n, point, bd_kernel ) " +
                    " Values (" + Constants.DefaultZap + ",0,2, 'Локальный банк'," + Utils.EStrNull(Points.Pref) + ")", true);
                }

                ret = ExecSQL(conn_db, " Insert into " + s_point_full + " (nzp_wp, point) Values (-1, 'Полный доступ')", true);

                conn_db.Close();
            }

            return ret;
        }

        //----------------------------------------------------------------------
        public Returns WebPrm(/*Finder finder*/)
        //----------------------------------------------------------------------
        {
            Returns ret = Utils.InitReturns();
            /*if (finder.nzp_user < 1)
            {
                ret.result = false;
                ret.text = "Не определен пользователь";
                return ret;
            }*/

            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result) return ret;

            string prm_name = "prm_name";
#if PG
            string prm_name_full = "public." + prm_name;
#else
            string prm_name_full = conn_web.Database + "@" + DBManager.getServer(conn_web) + ":" + prm_name;
#endif

            if (TableInWebCashe(conn_web, prm_name))
            {
                ExecSQL(conn_web, " Drop table " + prm_name, false);
            }

            //создать таблицу webdata:s_area
#if PG
            ret = ExecSQL(conn_web,
                "CREATE TABLE " + prm_name + "(" +
                " nzp_prm SERIAL NOT NULL," +
                " name_prm CHAR(100)," +
                " numer INTEGER," +
                " old_field CHAR(20)," +
                " type_prm CHAR(10)," +
                " nzp_res INTEGER," +
                " prm_num INTEGER," +
                " low_ real," +
                " high_ real," +
                " digits_ INTEGER)", true);
#else
            ret = ExecSQL(conn_web,
                "CREATE TABLE " + prm_name + "(" +
                " nzp_prm SERIAL NOT NULL," +
                " name_prm CHAR(100)," +
                " numer INTEGER," +
                " old_field CHAR(20)," +
                " type_prm CHAR(10)," +
                " nzp_res INTEGER," +
                " prm_num INTEGER," +
                " low_ FLOAT," +
                " high_ FLOAT," +
                " digits_ INTEGER)", true);
#endif
            if (!ret.result)
            {
                conn_web.Close();
                return ret;
            }

            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result)
            {
                conn_web.Close();
                return ret;
            }

            //выбрать список
            ret = ExecSQL(conn_db,
                " Insert into " + prm_name_full + " (nzp_prm, name_prm, numer, old_field, type_prm, nzp_res, prm_num, low_, high_, digits_) " +
#if PG
                " Select nzp_prm, name_prm, numer, old_field, type_prm, nzp_res, prm_num, low_, high_, digits_ From " + Points.Pref + "_kernel.prm_name", true);
#else
 " Select nzp_prm, name_prm, numer, old_field, type_prm, nzp_res, prm_num, low_, high_, digits_ From " + Points.Pref + "_kernel:prm_name", true);
#endif

            ret = ExecSQL(conn_db, " Insert into " + prm_name_full + " (nzp_prm, name_prm) Values (-1, 'Полный доступ')", true);

            if (ret.result) ret = ExecSQL(conn_web, "CREATE UNIQUE INDEX ixPrm_1 ON " + prm_name + "(nzp_prm);", true);
            if (ret.result) ret = ExecSQL(conn_web, "CREATE INDEX ixPrm_2 ON " + prm_name + "(name_prm);", true);

            conn_db.Close();
            conn_web.Close();
            return ret;
        }

        //----------------------------------------------------------------------
        public string GetDbPortal(out Returns ret) //получить путь к портальной базеs
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();

            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
            ret = this.OpenDb(conn_db, true);
            if (!ret.result) return null;

            IDataReader reader;
            if (!ExecRead(conn_db, out reader,
#if PG
                " Select trim(dbname)||'.' as portal" +
#else
 " Select trim(dbname)||'@'||trim(nvl(dbserver,''))||':' as portal" +
#endif
 " From s_baselist Where nzp_bl = 1000 ", true).result)
            {
                conn_db.Close();
                return null;
            }
            try
            {
                string s = "";
                if (reader.Read())
                {
                    if (reader["portal"] != DBNull.Value) s = (string)reader["portal"];
                }
                else
                    ret.result = false;

                reader.Close();
                conn_db.Close();
                return s;
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

                MonitorLog.WriteLog("Ошибка выборки данных GetDbPortal " + err, MonitorLog.typelog.Error, 20, 201, true);

                return "";
            }
        }

        /// <summary>
        /// Получить текущий расчетный месяц
        /// </summary>
        public RecordMonth GetCalcMonth(IDbConnection connection, IDbTransaction transaction, out Returns ret)
        {
            string pref = Points.Pref;
            return GetCalcMonth(connection, transaction, pref, out ret);
        }

        /// <summary>
        /// Получить текущий расчетный месяц
        /// </summary>
        public RecordMonth GetCalcMonth(IDbConnection connection, IDbTransaction transaction, string pref, out Returns ret)
        {
            IDataReader reader;

            RecordMonth rm = new RecordMonth();
            rm.year_ = 0;
            rm.month_ = 0;
#if PG
            ret = ExecRead(connection, out reader,
                " Select month_,yearr From " + pref + "_data.saldo_date " +
                " Where iscurrent = 0 ", true);
#else
            ret = ExecRead(connection, out reader,
                " Select month_,yearr From " + pref + "_data:saldo_date " +
                " Where iscurrent = 0 ", true);
#endif
            if (!ret.result)
            {
                return rm;
            }

            if (!reader.Read())
            {
                ret.text = "Не определен текущий расчетный месяц";
                ret.result = false;
                ret.tag = -1;
                return rm;
            }

            try
            {
                rm.month_ = (int)reader["month_"];
                rm.year_ = (int)reader["yearr"];
            }
            catch
            {
                rm.month_ = 0;
                rm.year_ = 0;

                ret.text = "Ошибка определения текущего расчетного месяца";
                ret.result = false;
            }

            reader.Close();
            reader.Dispose();
            return rm;
        }

        /// <summary>
        /// Получить текущий расчетный месяц для управляющих организаций
        /// </summary>
        public Dictionary<int, RecordMonth> GetCalcMonthAreas(IDbConnection connection, IDbTransaction transaction, out Returns ret)
        {
            IDataReader reader;

            Dictionary<int, RecordMonth> CalcMonthAreas = new Dictionary<int, RecordMonth>();
            RecordMonth rm = new RecordMonth();
            rm.month_ = 0;
            rm.year_ = 0;

            string delmtr = ":";
#if PG
            delmtr = ".";
#endif

            ret = ExecRead(connection, out reader,
                " Select nzp_area, month_,year_ From " + Points.Pref + "_data" + delmtr + "saldo_date_area " +
                " Where is_current = 1 ", true);

            if (!ret.result)
            {
                CalcMonthAreas.Add(0, rm);
                return CalcMonthAreas;
            }

            try
            {
                while (reader.Read())
                {
                    rm = new RecordMonth();
                    rm.month_ = (int)reader["month_"];
                    rm.year_ = (int)reader["year_"];
                    CalcMonthAreas.Add((int)reader["nzp_area"], rm);
                }
            }
            catch
            {
                rm.month_ = 0;
                rm.year_ = 0;
                CalcMonthAreas.Add(0, rm);
                ret.text = "Ошибка определения текущего расчетного месяца для управляющих организаций";
                ret.result = false;
            }

            reader.Close();
            reader.Dispose();
            return CalcMonthAreas;
        }

        //----------------------------------------------------------------------
        public bool PointLoad(bool WorkOnlyWithCentralBank, out Returns ret) //проверить наличие s_point и заполнить список Points
        //----------------------------------------------------------------------
        {
            string z = 0.ToString();
            ret = Utils.InitReturns();
            IDbConnection conn_db = null;
            MyDataReader reader = null;
            try
            {
                #region try
                Points.IsPoint = false;

                z = 1.ToString();

                if (Points.PointList == null) Points.PointList = new List<_Point>();
                else Points.PointList.Clear();

                if (Points.Servers == null) Points.Servers = new List<_Server>();
                else Points.Servers.Clear();

                z = 2.ToString();

                conn_db = GetConnection(Constants.cons_Kernel);

                z = 3.ToString();

                ret = OpenDb(conn_db, true);
                if (!ret.result) return false;

                //установить явно текущий банк
#if PG
                ret = ExecSQL(conn_db, "set search_path to '" + Points.Pref + "_kernel'", true);
#else
                ret = ExecSQL(conn_db, " Database " + Points.Pref + "_kernel", true);
#endif
                if (!ret.result) return false;

                z = 4.ToString();

                DbTables tables = new DbTables(DBManager.getServer(conn_db));

                //+++++++++++++++++++++++++++++++++++++++++++++++++++++++
                //скачаем текущий расчетный месяц
                //+++++++++++++++++++++++++++++++++++++++++++++++++++++++
                Points.pointWebData.calcMonth = GetCalcMonth(conn_db, null, out ret);
                if (!ret.result) return false;

                /*  Points.pointWebData.calcMonthAreas = GetCalcMonthAreas(conn_db, null, out ret);
                  if (!ret.result) return false;*/

                z = 5.ToString();

                if (Points.Pref == null) z = "6. Points.Pref is null ";
                else z = "6. Points.Pref = " + Points.Pref;

                //+++++++++++++++++++++++++++++++++++++++++++++++++++++++
                //скачаем текущий операционный день
                //+++++++++++++++++++++++++++++++++++++++++++++++++++++++
                if (ExecRead(conn_db, out reader,
#if PG
                    "Select dat_oper From " + Points.Pref + "_data.fn_curoperday", true).result)
#else
 "Select dat_oper From " + Points.Pref + "_data:fn_curoperday", true).result)
#endif
                {
                    z = "6.1";

                    Points.isFinances = true;

                    if (reader.Read() && reader["dat_oper"] != DBNull.Value)
                    {
                        z = "6.2";

                        Points.DateOper = Convert.ToDateTime(reader["dat_oper"]);
                        //        //определение версии "Финансы УК"
                        //        int yy = Points.DateOper.Year;
                        //        if (ExecRead(conn_db, out reader,
                        //            "Select * From " + Points.Pref + "_fin_" + (yy - 2000).ToString("00") + ":fn_distrib_01", false).result)
                        //        {
                        //            Points.financesType = FinancesTypes.Uk;
                        //        }
                    }
                    reader.Close();
                }

                z = 7.ToString();

                GetCalcDates(conn_db, Points.Pref, out Points.pointWebData.beginWork, out Points.pointWebData.beginCalc, out Points.pointWebData.calcMonths, 0, out ret);
                if (!ret.result) return false;

                z = 8.ToString();

                z = 9.ToString();

                //+++++++++++++++++++++++++++++++++++++++++++++++++++++++
                //считать s_point
                //+++++++++++++++++++++++++++++++++++++++++++++++++++++++
                if (!TableInWebCashe(conn_db, "s_point"))
                {
                    //по умолчанию Татарстан, т.к. только в нем есть банки без s_point
                    Points.Region = Regions.Region.Tatarstan;

                    //заполнить по-умолчанию point = pref (для одиночных баз)
                    _Point zap = new _Point();
                    zap.nzp_wp = Constants.DefaultZap;
                    zap.point = "Локальный банк";
                    zap.pref = Points.Pref; ;

                    zap.nzp_server = -1;

                    GetCalcDates(conn_db, zap.pref, out zap.BeginWork, out zap.BeginCalc, out zap.CalcMonths, 0, out ret);
                    if (!ret.result) return false;

                    Points.isFinances = false; //финансы не подключены
                    Points.PointList.Add(zap);
                    Points.mainPageName = "Биллинговый центр";
                }
                else
                {
                    #region Определение региона
                    Points.Region = Regions.Region.Tatarstan;
#if PG
                    Returns retRegion = ExecRead(conn_db, out reader, " select substr(bank_number::char,1,2) region_code from " + Points.Pref + "_kernel.s_point where nzp_wp = 1 ", true);
#else
                    Returns retRegion = ExecRead(conn_db, out reader, " select substr(bank_number,1,2) region_code from " + Points.Pref + "_kernel:s_point where nzp_wp = 1 ", true);
#endif
                    if (retRegion.result)
                    {
                        try
                        {
                            if (reader.Read())
                            {
                                if (reader["region_code"] != DBNull.Value)
                                {
                                    Points.Region = Regions.GetById(Convert.ToInt32((string)reader["region_code"]));
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            throw new Exception("Ошибка при определении кода региона " + ex.Message);
                        }
                    }
                    reader.Close();
                    #endregion

                    List<string> title;
                    if ((title = this.GetMainPageTitle()) != null && title.Count != 0)
                    {
                        Points.mainPageName = title[0];
                    }
                    else
                    {
                        Points.mainPageName = "Биллинговый центр";
                    }

                    bool b_yes_server = isTableHasColumn(conn_db, "s_point", "nzp_server");

                    if (b_yes_server && TableInWebCashe(conn_db, "servers"))
                    {
                        //+++++++++++++++++++++++++++++++++++++++++++++++++++++++
                        //подключен механизм фабрики серверов, считаем список серверов
                        //+++++++++++++++++++++++++++++++++++++++++++++++++++++++
                        if (ExecRead(conn_db, out reader,
                             " Select * From servers Order by nzp_server ", false).result)
                        {
                            try
                            {
                                while (reader.Read())
                                {
                                    _Server zap = new _Server();
                                    zap.is_valid = true;

                                    zap.conn = "";
                                    zap.ip_adr = "";
                                    zap.login = "";
                                    zap.pwd = "";
                                    zap.nserver = "";
                                    zap.ol_server = "";

                                    zap.nzp_server = (int)reader["nzp_server"];

                                    if (reader["conn"] != DBNull.Value)
                                        zap.conn = (string)reader["conn"];
                                    if (reader["ip_adr"] != DBNull.Value)
                                        zap.ip_adr = (string)reader["ip_adr"];
                                    if (reader["login"] != DBNull.Value)
                                        zap.login = (string)reader["login"];
                                    if (reader["pwd"] != DBNull.Value)
                                        zap.pwd = (string)reader["pwd"];
                                    if (reader["nserver"] != DBNull.Value)
                                        zap.nserver = (string)reader["nserver"];
                                    if (reader["ol_server"] != DBNull.Value)
                                        zap.ol_server = (string)reader["ol_server"];

                                    zap.conn = zap.conn.Trim();
                                    zap.ip_adr = zap.ip_adr.Trim();
                                    zap.login = zap.login.Trim();
                                    zap.pwd = zap.pwd.Trim();
                                    zap.nserver = zap.nserver.Trim();
                                    zap.ol_server = zap.ol_server.Trim();

                                    Points.Servers.Add(zap);
                                }
                                Points.IsFabric = true;
                            }
                            catch (Exception ex)
                            {
                                MonitorLog.WriteLog("Ошибка заполнения servers " + ex.Message, MonitorLog.typelog.Error, 30, 1, true);
                                Points.IsFabric = false;
                            }
                            reader.Close();
                        }
                    }

                    //заполнить список локальных банков данных
                    if (ExecRead(conn_db, out reader,
                        //" Select * From s_point Where nzp_graj > 0 Order by n", false).result)
                       " Select * From s_point Order by nzp_wp", false).result)
                    {
                        try
                        {
                            Returns ret2;
                            while (reader.Read())
                            {
                                _Point zap = new _Point();
                                zap.nzp_wp = (int)reader["nzp_wp"];
                                zap.flag = (int)reader["flag"];
                                zap.point = (string)reader["point"];
                                zap.pref = (string)reader["bd_kernel"];
                                zap.CalcMonth = GetCalcMonth(conn_db, null, zap.pref, out ret);
                                zap.point = zap.point.Trim();
                                zap.pref = zap.pref.Trim();
                                zap.ol_server = "";
                                zap.n = reader["n"] != DBNull.Value ? Convert.ToInt32(reader["n"]) : 0;
                                if (b_yes_server)
                                {
                                    zap.nzp_server = (int)reader["nzp_server"];
                                    if (reader["bd_old"] != DBNull.Value)
                                        zap.ol_server = (string)reader["bd_old"];
                                }
                                else
                                    zap.nzp_server = -1;

                                if (zap.nzp_wp == 1)
                                {
                                    //фин.банк
                                    Points.Point = zap;
                                }
                                else
                                {
                                    //MonitorLog.WriteLog("4", MonitorLog.typelog.Info, true);
                                    //список расчетных дат
                                    if (!WorkOnlyWithCentralBank)
                                    {
                                        //MonitorLog.WriteLog("5", MonitorLog.typelog.Info, true);
                                        GetCalcDates(conn_db, zap.pref, zap.CalcMonth, out zap.BeginWork, out zap.BeginCalc, out zap.CalcMonths, zap.nzp_server, out ret2);
                                    }
                                    else
                                    {
                                        //MonitorLog.WriteLog("6", MonitorLog.typelog.Info, true);
                                        // при запрете работе с локальными банками используем параметры центрального банка
                                        zap.BeginWork = Points.BeginWork;
                                        zap.BeginCalc = Points.BeginCalc;
                                        zap.CalcMonths = Points.CalcMonths;
                                        ret2 = Utils.InitReturns();
                                    }

                                    if (!ret2.result && ret2.tag >= 0)
                                    {
                                        return false;
                                    }

                                    if (ret2.result)
                                    {
                                        Points.PointList.Add(zap);
                                    }
                                    else
                                    {
                                        ret.tag = -1;
                                        ret.text += "Банк данных \"" + zap.point + "\" не доступен\n";
                                    }
                                }

                            }

                            Points.IsPoint = true;
                        }
                        catch (Exception ex)
                        {
                            Points.IsPoint = false;
                            throw new Exception("Ошибка заполнения s_point " + ex.Message);
                        }
                        reader.Close();
                    }
                }

                CorrectPointsCalcMonths(ref Points.pointWebData.calcMonths, Points.PointList);

                z = 10.ToString();

                //+++++++++++++++++++++++++++++++++++++++++++++++++++++++
                //признак Самары!!!
                //+++++++++++++++++++++++++++++++++++++++++++++++++++++++
                Points.IsSmr = false;
#if PG
                Returns ret3 = ExecRead(conn_db, out reader,
                " Select val_prm From " + Points.Pref + "_data.prm_5 p " +
                " Where p.nzp_prm = 2000 " +
                "   and p.is_actual <> 100 " +
                "   and p.dat_s  <= current_date and p.dat_po >= current_date "
                , true);
#else
                Returns ret3 = ExecRead(conn_db, out reader,
                " Select val_prm From " + Points.Pref + "_data:prm_5 p " +
                " Where p.nzp_prm = 2000 " +
                "   and p.is_actual <> 100 " +
                "   and p.dat_s  <= today and p.dat_po >= today "
                , true);
#endif
                if (ret3.result)
                {
                    if (reader.Read())
                    {
                        Points.IsSmr = (((string)reader["val_prm"]).Trim() == "1");
                    }
                }
                reader.Close();

                #region параметры распределения оплат
                Points.packDistributionParameters = new PackDistributionParameters();
                DbParameters dbparam = new DbParameters();
                Prm prm = new Prm()
                {
                    nzp_user = 1,
                    pref = Points.Pref,
                    prm_num = 10,
                    nzp_prm = 1131,
                    nzp = 0,
                    year_ = Points.CalcMonth.year_,
                    month_ = Points.CalcMonth.month_
                };
                Prm val;
                int id;

                //Определение стратегии распределения оплат
                if (Points.IsSmr) Points.packDistributionParameters.strategy = PackDistributionParameters.Strategies.Samara;
                else
                {
                    Points.packDistributionParameters.strategy = PackDistributionParameters.Strategies.InactiveServicesFirst;


                    val = dbparam.FindSimplePrmValue(conn_db, prm, out ret3);
                    if (ret3.result)
                    {
                        if (Int32.TryParse(val.val_prm, out id))
                        {
                            switch (id)
                            {
                                case 1: Points.packDistributionParameters.strategy = PackDistributionParameters.Strategies.InactiveServicesFirst; break;
                                case 2: Points.packDistributionParameters.strategy = PackDistributionParameters.Strategies.ActiveServicesFirst; break;
                                case 3: Points.packDistributionParameters.strategy = PackDistributionParameters.Strategies.NoPriority; break;
                            }
                        }
                    }
                }

                //Распределять пачки сразу после загрузки
                prm.nzp_prm = 1132;
                val = dbparam.FindSimplePrmValue(conn_db, prm, out ret3);
                if (ret3.result && val.val_prm == "1")
                    Points.packDistributionParameters.DistributePackImmediately = true;

                //Выполнять протоколирование процесса распределения оплат
                prm.nzp_prm = 1133;
                val = dbparam.FindSimplePrmValue(conn_db, prm, out ret3);
                if (ret3.result && val.val_prm == "1")
                    Points.packDistributionParameters.EnableLog = true;

                //Первоначальное распределение по полю
                Points.packDistributionParameters.chargeMethod = PackDistributionParameters.ChargeMethods.PositiveOutsaldo;
                prm.nzp_prm = 1134;
                val = dbparam.FindSimplePrmValue(conn_db, prm, out ret3);
                if (ret3.result)
                {
                    if (Int32.TryParse(val.val_prm, out id))
                    {
                        switch (id)
                        {
                            case 1: Points.packDistributionParameters.chargeMethod = PackDistributionParameters.ChargeMethods.Outsaldo; break;
                            case 2: Points.packDistributionParameters.chargeMethod = PackDistributionParameters.ChargeMethods.PositiveOutsaldo; break;
                            case 3: Points.packDistributionParameters.chargeMethod = PackDistributionParameters.ChargeMethods.MonthlyCalculationWithChangesAndOverpayment; break;
                            case 4: Points.packDistributionParameters.chargeMethod = PackDistributionParameters.ChargeMethods.PositiveMonthlyCalculationWithChangesAndOverpayment; break;
                            case 5: Points.packDistributionParameters.chargeMethod = PackDistributionParameters.ChargeMethods.MonthlyCalculationWithChanges; break;
                            case 6: Points.packDistributionParameters.chargeMethod = PackDistributionParameters.ChargeMethods.PositiveMonthlyCalculationWithChanges; break;
                        }
                    }
                }

                //Рассчитывать суммы к перечислению автоматически при распределении/откате оплат
                prm.nzp_prm = (int)ParamIds.SystemParams.CalcDistributionAutomatically;
                val = dbparam.FindSimplePrmValue(conn_db, prm, out ret3);
                if (ret3.result && val.val_prm == "1")
                    Points.packDistributionParameters.CalcDistributionAutomatically = true;

                //Первоначальное распределение по полю
                Points.packDistributionParameters.distributionMethod = PackDistributionParameters.PaymentDistributionMethods.LastMonthPositiveSumCharge;
                prm.nzp_prm = (int)ParamIds.SystemParams.PaymentDistributionMethod;
                val = dbparam.FindSimplePrmValue(conn_db, prm, out ret3);
                if (ret3.result)
                {
                    if (Int32.TryParse(val.val_prm, out id))
                    {
                        switch (id)
                        {
                            case 1: Points.packDistributionParameters.distributionMethod = PackDistributionParameters.PaymentDistributionMethods.LastMonthPositiveSumCharge; break;
                            case 2: Points.packDistributionParameters.distributionMethod = PackDistributionParameters.PaymentDistributionMethods.LastMonthSumCharge; break;
                            case 3: Points.packDistributionParameters.distributionMethod = PackDistributionParameters.PaymentDistributionMethods.CurrentMonthPositiveSumInsaldo; break;
                            case 4: Points.packDistributionParameters.distributionMethod = PackDistributionParameters.PaymentDistributionMethods.CurrentMonthSumInsaldo; break;
                        }
                    }
                }

                //Плательщик заполняет оплату по услугам
                prm.nzp_prm = 1135;
                val = dbparam.FindSimplePrmValue(conn_db, prm, out ret3);
                if (ret3.result && val.val_prm == "1")
                    Points.packDistributionParameters.AllowSelectServicesWhilePaying = true;

                //Список приоритетных услуг
                Points.packDistributionParameters.PriorityServices = new List<Service>();

#if PG
                ret3 = ExecRead(conn_db, out reader, "select p.nzp_serv, s.service, s.ordering" +
                    " from " + Points.Pref + "_kernel" + DBManager.getServer(conn_db) + ".servpriority p, " + tables.services + " s" +
                    " where s.nzp_serv = p.nzp_serv and now() between p.dat_s and p.dat_po order by ordering", true);
#else
                ret3 = ExecRead(conn_db, out reader, "select p.nzp_serv, s.service, s.ordering" +
                    " from " + Points.Pref + "_kernel@" + DBManager.getServer(conn_db) + ":servpriority p, " + tables.services + " s" +
                    " where s.nzp_serv = p.nzp_serv and current between p.dat_s and p.dat_po order by ordering", true);

#endif
                if (ret3.result)
                {
                    while (reader.Read())
                    {
                        Service zap = new Service();
                        if (reader["nzp_serv"] != DBNull.Value) zap.nzp_serv = (int)reader["nzp_serv"];
                        if (reader["service"] != DBNull.Value) zap.service = ((string)reader["service"]).Trim();
                        if (reader["ordering"] != DBNull.Value) zap.ordering = (int)reader["ordering"];

                        Points.packDistributionParameters.PriorityServices.Add(zap);
                    }
                    reader.Close();
                }
                #endregion

                #region Признак, сохранять ли показания ПУ прямо в основной банк
                Prm prm2 = new Prm()
                {
                    nzp_user = 1,
                    pref = Points.Pref,
                    prm_num = 5,
                    nzp_prm = 1993,
                    nzp = 0,
                    year_ = Points.CalcMonth.year_,
                    month_ = Points.CalcMonth.month_
                };
                val = dbparam.FindSimplePrmValue(conn_db, prm2, out ret3);
                if (ret3.result && val.val_prm == "1")
                    Points.SaveCounterReadingsToRealBank = true;
                else
                    Points.SaveCounterReadingsToRealBank = false;

                if (Constants.Trace) Utility.ClassLog.WriteLog("Сохранять показания приборов учета в основной банк данных: " + (Points.SaveCounterReadingsToRealBank ? "Да" : "Нет"));
                #endregion

                //+++++++++++++++++++++++++++++++++++++++++++++++++++++++
                //признак Demo!!!
                //+++++++++++++++++++++++++++++++++++++++++++++++++++++++
                Points.IsDemo = false;

#if PG
                ret3 = ExecRead(conn_db, out reader,
                " Select val_prm From " + Points.Pref + "_data.prm_5 p " +
                " Where p.nzp_prm = 1999 " +
                "   and p.is_actual <> 100 " +
                "   and p.dat_s  <= current_date and p.dat_po >= current_date "
                , true);
#else
                ret3 = ExecRead(conn_db, out reader,
                " Select val_prm From " + Points.Pref + "_data:prm_5 p " +
                " Where p.nzp_prm = 1999 " +
                "   and p.is_actual <> 100 " +
                "   and p.dat_s  <= today and p.dat_po >= today "
                , true);

#endif
                if (ret3.result)
                {
                    if (reader.Read())
                    {
                        Points.IsDemo = (((string)reader["val_prm"]).Trim() == "1");
                    }
                    reader.Close();
                }
                z = 11.ToString();

                //+++++++++++++++++++++++++++++++++++++++++++++++++++++++
                //признак Is50!!!
                //+++++++++++++++++++++++++++++++++++++++++++++++++++++++
                Points.Is50 = false;

#if PG
                ret3 = ExecRead(conn_db, out reader,
                " Select val_prm From " + Points.Pref + "_data.prm_5 p " +
                " Where p.nzp_prm = 1997 " +
                "   and p.is_actual <> 100 " +
                "   and p.dat_s  <= current_date and p.dat_po >= current_date "
                , true);
#else
                ret3 = ExecRead(conn_db, out reader,
                " Select val_prm From " + Points.Pref + "_data:prm_5 p " +
                " Where p.nzp_prm = 1997 " +
                "   and p.is_actual <> 100 " +
                "   and p.dat_s  <= today and p.dat_po >= today "
                , true);
#endif
                if (ret3.result)
                {
                    if (reader.Read())
                    {
                        Points.Is50 = (((string)reader["val_prm"]).Trim() == "1");
                    }

                    reader.Close();
                }

                z = 12.ToString();

                //+++++++++++++++++++++++++++++++++++++++++++++++++++++++
                //снять зависшие фоновые задания при рестарте хоста
                //+++++++++++++++++++++++++++++++++++++++++++++++++++++++
                IDbConnection conn_web = GetConnection(Constants.cons_Webdata);

                z = 13.ToString();

                ret3 = OpenDb(conn_web, true);
                if (ret3.result)
                {

                    ExecSQL(conn_web, " Update saldo_fon Set kod_info = 3, txt = 'повторно!' Where kod_info = 0 ", false);
                }

                //удалить временные образы __anlXX
                DbAnalizClient dba = new DbAnalizClient();
                dba.Drop__AnlTables(conn_web);
                dba.Close();

                conn_web.Close();

                z = 14.ToString();

                //+++++++++++++++++++++++++++++++++++++++++++++++++++++++
                //признак клона
                //+++++++++++++++++++++++++++++++++++++++++++++++++++++++
                Points.isClone = false;
#if PG
                ret3 = ExecRead(conn_db, out reader,
                " Select dbname From " + Points.Pref + "_kernel.s_baselist " +
                " Where idtype = " + BaselistTypes.PrimaryBank.GetHashCode(), true);
#else
                ret3 = ExecRead(conn_db, out reader,
                " Select dbname From " + Points.Pref + "_kernel:s_baselist " +
                " Where idtype = " + BaselistTypes.PrimaryBank.GetHashCode(), true);
#endif
                if (ret3.result)
                    if (reader.Read())
                    {
                        Points.isClone = (reader["dbname"] != DBNull.Value && ((string)reader["dbname"]).Trim() != "");
                    }
                reader.Close();
                z = 15.ToString();

                //+++++++++++++++++++++++++++++++++++++++++++++++++++++++
                //признак IsCalcSubsidy !!!
                //+++++++++++++++++++++++++++++++++++++++++++++++++++++++
                Points.IsCalcSubsidy = false;

#if PG
                ret3 = ExecRead(conn_db, out reader,
                " Select val_prm From " + Points.Pref + "_data.prm_5 p " +
                " Where p.nzp_prm = 1992 " +
                "   and p.is_actual <> 100 " +
                "   and p.dat_s  <= current_date and p.dat_po >= current_date "
                , true);
#else
                ret3 = ExecRead(conn_db, out reader,
                " Select val_prm From " + Points.Pref + "_data:prm_5 p " +
                " Where p.nzp_prm = 1992 " +
                "   and p.is_actual <> 100 " +
                "   and p.dat_s  <= today and p.dat_po >= today "
                , true);
#endif
                if (ret3.result)
                    if (reader.Read())
                    {
                        Points.IsCalcSubsidy = (((string)reader["val_prm"]).Trim() == "1");
                    }
                z = 16.ToString();

                #region RecalcMode
                Points.RecalcMode = RecalcModes.Automatic;
#if PG
                ret3 = ExecRead(conn_db, out reader,
                    " Select val_prm From " + Points.Pref + "_data.prm_5 p " +
                    " Where p.nzp_prm = 1990 " +
                    "   and p.is_actual <> 100 " +
                    "   and p.dat_s  <= current_date and p.dat_po >= current_date "
                    , true);
#else
                ret3 = ExecRead(conn_db, out reader,
                    " Select val_prm From " + Points.Pref + "_data:prm_5 p " +
                    " Where p.nzp_prm = 1990 " +
                    "   and p.is_actual <> 100 " +
                    "   and p.dat_s  <= today and p.dat_po >= today "
                    , true);
#endif
                if (ret3.result)
                {
                    if (reader.Read())
                    {
                        string val_prm = reader["val_prm"] != DBNull.Value ? ((string)reader["val_prm"]).Trim() : "";
                        if (val_prm == RecalcModes.Automatic.GetHashCode().ToString()) Points.RecalcMode = RecalcModes.Automatic;
                        else if (val_prm == RecalcModes.AutomaticWithCancelAbility.GetHashCode().ToString()) Points.RecalcMode = RecalcModes.AutomaticWithCancelAbility;
                        else if (val_prm == RecalcModes.Manual.GetHashCode().ToString()) Points.RecalcMode = RecalcModes.Manual;
                    }
                    reader.Close();
                    if (Constants.Trace) Utility.ClassLog.WriteLog("Задан режим перерасчета: " + Points.RecalcMode + "(код " + Points.RecalcMode.GetHashCode() + ")");
                }
                else
                {
                    if (Constants.Trace) Utility.ClassLog.WriteLog("Ошибка при определении режима перерасчета: " + ret3.text);
                }
                z = 17.ToString();
                #endregion

                #region проверить, что в таблице counters всех банков есть поле ngp_cnt
                if (WorkOnlyWithCentralBank)
                {
                    Points.IsIpuHasNgpCnt = false;
                }
                else
                {
                    Points.IsIpuHasNgpCnt = true;

                    foreach (_Point p in Points.PointList)
                    {
#if PG
                    ret3 = ExecSQL(conn_db, " Select c.ngp_cnt From " + p.pref + "_data.counters c Where c.nzp_cr = 0 ", false);
#else
                        ret3 = ExecSQL(conn_db, " Select c.ngp_cnt From " + p.pref + "_data:counters c Where c.nzp_cr = 0 ", false);
#endif
                        if (!ret3.result)
                        {
                            Points.IsIpuHasNgpCnt = false;
                            break;
                        }
                    }
                }
                #endregion

                #region признак Работы с Series !!!
                Points.isUseSeries = true;

                Returns retus = ExecRead(conn_db, out reader,
                " Select 1 From " + Points.Pref + "_data" + tableDelimiter + "prm_10 p " +
                " Where p.nzp_prm = 1266 " +
                "   and p.val_prm = '1'" +
                "   and p.is_actual <> 100 " +
                "   and " + sCurDate + " between p.dat_s and p.dat_po "
                , true);

                if (retus.result)
                {
                    Points.isUseSeries = reader.Read();
                    reader.Close();
                }

                conn_db.Close();
                #endregion

                #region Тип функции генерации платежного кода
                //Определяем функцию для генерации платежного кода
                DbParameters dbParameters = new DbParameters();
                Prm prmGenPkod =
                    dbParameters.FindSimplePrmValue(conn_db,
                        new Prm()
                        {
                            nzp_user = 1,
                            pref = Points.Pref,
                            prm_num = ParamNums.General10,
                            nzp_prm = ParamIds.SpravParams.TypePkod.GetHashCode(),
                            year_ = Points.CalcMonth.year_,
                            month_ = Points.CalcMonth.month_
                        }, out ret);
                dbParameters.Close();
                if (!ret.result) return false;

                Points.functionTypeGeneratePkod = FunctionsTypesGeneratePkod.standart;
                if (prmGenPkod.val_prm != "")
                {
                    Points.functionTypeGeneratePkod = (FunctionsTypesGeneratePkod)
                        Enum.Parse(typeof (FunctionsTypesGeneratePkod), prmGenPkod.val_prm);
                }                

                #endregion

                return true;
                #endregion
            }
            catch (Exception ex)
            {
                ret = new Returns(false, z + ": " + ex.Message);
                MonitorLog.WriteLog("Ошибка в функции DbSprav.PointLoad " + z + ": " + ex.Message, MonitorLog.typelog.Error, 30, 1, true);

                if (conn_db != null) conn_db.Close();
                return false;
            }
            finally
            {
                if (reader != null) reader.Close();
                if (conn_db != null) conn_db.Close();
            }
        }

        private void CorrectPointsCalcMonths(ref List<RecordMonth> PointsCalcMonths, List<_Point> PointList)
        {
            if (PointList == null && PointList.Count == 0) return;
            if (PointsCalcMonths == null && PointsCalcMonths.Count == 0) return;

            RecordMonth r_m = Points.GetCalcMonth(new CalcMonthParams(PointList[0].pref));
            DateTime dt = new DateTime(r_m.year_, r_m.month_, 1);
            for (int i = 1; i < PointList.Count; i++)
            {
                RecordMonth r_m2 = Points.GetCalcMonth(new CalcMonthParams(PointList[i].pref));
                DateTime dt2 = new DateTime(r_m2.year_, r_m2.month_, 1);

                if (dt2 > dt)
                {
                    dt = dt2;
                    r_m = r_m2;
                }
            }
            RecordMonth r = new RecordMonth();
            for (int y = r_m.year_; y > PointsCalcMonths[0].year_; y--)
            {
                int mm = 12;
                if (y == r_m.year_) mm = r_m.month_;

                for (int m = mm; m >= 1; m--)
                {
                    r.year_ = y;
                    r.month_ = m;

                    PointsCalcMonths.Insert(0, r);
                }

                r.year_ = y;
                r.month_ = 0;
                PointsCalcMonths.Insert(0, r);
            }
        }

        //----------------------------------------------------------------------
        void GetCalcDates(IDbConnection conn_db2, string pref, out RecordMonth bw, out RecordMonth bc, out List<RecordMonth> cm, int nzp_server, out Returns ret)
        //----------------------------------------------------------------------
        {
            RecordMonth calcMonth = Points.CalcMonth;
            GetCalcDates(conn_db2, pref, calcMonth, out bw, out bc, out cm, nzp_server, out ret);
        }

        void GetCalcDates(IDbConnection conn_db2, string pref, RecordMonth calcMonth, out RecordMonth bw, out RecordMonth bc, out List<RecordMonth> cm, int nzp_server, out Returns ret)
        //----------------------------------------------------------------------
        {
            //проверим, есть ли данная база на сервере
            ret = Utils.InitReturns();
            bw.month_ = 0;
            bw.year_ = 0;
            bc.month_ = 0;
            bc.year_ = 0;
            cm = new List<RecordMonth>();

            //bool b = DatabaseOnServer(conn_db2, pref + "_data", out ret);

            //if (b || ret.result)
            //{
            //да, есть, значит считываем данные
            GetCalcDates0(conn_db2, pref, calcMonth, out bw, out bc, out cm, out ret);
            //}
            //else
            //{
            //    if (nzp_server < 1)
            //    {
            //        ret.result = false;
            //        ret.text = "БД " + pref + " не определена";
            //        return;
            //    }

            //    //надо связаться с сервером
            //    string conn_kernel = Points.GetConnByServer(nzp_server);
            //    IDbConnection conn_db = GetConnection(conn_kernel);
            //    ret = this.OpenDb(conn_db, true);
            //    if (!ret.result) return;

            //    if (DatabaseOnServer(conn_db, pref + "_data"))
            //    {
            //        GetCalcDates0(conn_db, pref, out bw, out bc, out cm, out ret);
            //    }
            //    else
            //    {
            //        ret.result = false;
            //        ret.text = "БД " + pref + " не доступна";
            //    }

            //    conn_db.Close();
            //}
        }

        void GetCalcDates0(IDbConnection conn_db, string pref, RecordMonth calcMonth, out RecordMonth bw, out RecordMonth bc, out List<RecordMonth> cm, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            bw.month_ = 0;
            bw.year_ = 0;
            bc.month_ = 0;
            bc.year_ = 0;
            cm = new List<RecordMonth>();

            DateTime defaultStartDate = new DateTime(2005, 1, 1); //дата начала работы по умолчанию

#if PG
            if (!TempTableInWebCashe(conn_db, pref + "_data.prm_10"))
#else
            if (!TempTableInWebCashe(conn_db, pref + "_data:prm_10"))
#endif
            {
                MonitorLog.WriteLog("При загрузке банка данных таблица " + pref + "_data:prm_10 оказалась не доступна. Дата начала работы системы и дата начала перерасчетов установлены в значения по умолчанию " + defaultStartDate.ToString("dd.MM.yyyy"), MonitorLog.typelog.Warn, 30, 1, true);
            }
            else
            {
                IDataReader reader;
                //даты начала работы системы и перерасчетов
                if (!ExecRead(conn_db, out reader,
#if PG
                    " Select nzp_prm,val_prm From " + pref + "_data.prm_10 " +
                    " Where nzp_prm in (82,771) and is_actual <> 100 " +
                    "   and COALESCE(dat_s, public.MDY(1,1,2001)) <= public.MDY(" + calcMonth.month_.ToString() + ",1," + calcMonth.year_.ToString() + ") " +
                    "   and COALESCE(dat_po, public.MDY(1,1,3000)) >= public.MDY(" + calcMonth.month_.ToString() + ",1," + calcMonth.year_.ToString() + ") " +
                    " Order by nzp_prm "
#else
 " Select nzp_prm,val_prm From " + pref + "_data:prm_10 " +
                    " Where nzp_prm in (82,771) and is_actual <> 100 " +
                    "   and nvl(dat_s, MDY(1,1,2001)) <= MDY(" + calcMonth.month_.ToString() + ",1," + calcMonth.year_.ToString() + ") " +
                    "   and nvl(dat_po,MDY(1,1,3000)) >= MDY(" + calcMonth.month_.ToString() + ",1," + calcMonth.year_.ToString() + ") " +
                    " Order by nzp_prm "
#endif
, true).result
                   )
                {
                    ret.text = "Ошибка чтения (" + pref + ")prm_10";
                    ret.result = false;
                    return;
                }
                try
                {
                    string val_prm;

                    while (reader.Read())
                    {
                        val_prm = (string)reader["val_prm"];
                        DateTime d = Convert.ToDateTime(val_prm.Trim());

                        if ((int)reader["nzp_prm"] == 82)
                        {
                            bw.month_ = d.Month;
                            bw.year_ = d.Year;
                        }
                        else
                        {
                            bc.month_ = d.Month;
                            bc.year_ = d.Year;

                            break;
                        }
                    }
                    reader.Close();
                }
                catch (Exception ex)
                {
                    reader.Close();
                    ret.text = "Ошибка чтения (" + pref + ")prm_10";
                    ret.result = false;
                    MonitorLog.WriteLog(ex.Message, MonitorLog.typelog.Error, true);
                    return;
                }
            }

            if (bw.year_ < 1998)
            {
                //не указана дата начала работы системы
                bw.year_ = defaultStartDate.Year;
                bw.month_ = defaultStartDate.Month;
            }
            if (bc.year_ < 1998)
            {
                //не указана дата пересчетов
                bc.year_ = bw.year_;
                bc.month_ = bw.month_;
            }

            RecordMonth r;


            for (int y = calcMonth.year_; y >= bw.year_; y--)
            {
                r.year_ = y;
                r.month_ = 0;
                cm.Add(r);

                int mm = 12;
                if (y == calcMonth.year_) mm = calcMonth.month_;

                for (int m = mm; m >= 1; m--)
                {
                    r.year_ = y;
                    r.month_ = m;

                    cm.Add(r);
                }
            }
            if (cm.Count == 0)
            {
                MonitorLog.WriteLog("Список расчетных месяцев пуст для банка " + pref, MonitorLog.typelog.Warn, true);
            }
            if (Constants.Trace) Utility.ClassLog.WriteLog("Список расчетных месяцев для банка " + pref + " - " + cm.Count);
        }
        //----------------------------------------------------------------------
        public List<_ResY> LoadResY(string find_nzp_res, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            if (ResYs.ResYList != null) ResYs.ResYList.Clear();
            else ResYs.ResYList = new List<_ResY>();

            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return null;

            DbTables tables = new DbTables(DBManager.getServer(conn_db));

            //найти префикс локальной базы, чтобы оттуда взять res_y
            string pref = Points.Pref; //по-умолчанию
            foreach (_Point zap in Points.PointList)
            {
                if (zap.nzp_wp != Constants.DefaultZap)
                {
                    pref = zap.pref;
                    break;
                }
            }

            IDataReader reader;
            if (!ExecRead(conn_db, out reader,
                " Select nzp_res,nzp_y,name_y From " + tables.res_y +
                " Where nzp_res in (" + find_nzp_res + ")" +
                " Order by nzp_res,nzp_y ", true).result)
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
                    _ResY zap = new _ResY();

                    if (reader["nzp_res"] == DBNull.Value) zap.nzp_res = 0;
                    else zap.nzp_res = Convert.ToInt32(reader["nzp_res"]);
                    if (reader["nzp_y"] == DBNull.Value) zap.nzp_y = 0;
                    else zap.nzp_y = Convert.ToInt32(reader["nzp_y"]);
                    if (reader["name_y"] == DBNull.Value) zap.name_y = "";
                    else
                    {
                        zap.name_y = Convert.ToString(reader["name_y"]);
                        zap.name_y = zap.name_y.Trim();
                    }

                    ResYs.ResYList.Add(zap);
                }

                ret.tag = i;
                reader.Close();
                conn_db.Close();
                return ResYs.ResYList;
            }
            catch (Exception ex)
            {
                if (reader != null) reader.Close();
                conn_db.Close();

                ret.result = false;
                ret.text = ex.Message;

                string err;
                if (Constants.Viewerror)
                    err = " \n " + ex.Message;
                else
                    err = "";

                MonitorLog.WriteLog("Ошибка заполнения справочника параметров " + err, MonitorLog.typelog.Error, 20, 201, true);

                return null;
            }
        }
        //----------------------------------------------------------------------
        public List<_TypeAlg> LoadTypeAlg(out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();

            TypeAlgs.AlgList.Clear();

            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return null;

            //выбрать список
            StringBuilder sql = new StringBuilder();
#if PG
            sql.Append(" Select nzp_type_alg, name_type From " + Points.Pref + "_kernel.s_type_alg");
#else
            sql.Append(" Select nzp_type_alg, name_type From " + Points.Pref + "_kernel:s_type_alg");
#endif
            sql.Append(" Order by name_type ");

            IDataReader reader;
            if (!ExecRead(conn_db, out reader, sql.ToString(), true).result)
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
                    _TypeAlg zap = new _TypeAlg();

                    if (reader["nzp_type_alg"] == DBNull.Value) zap.nzp_type_alg = 0;
                    else zap.nzp_type_alg = (int)reader["nzp_type_alg"];

                    if (reader["name_type"] == DBNull.Value) zap.name_type = "";
                    else
                    {
                        zap.name_type = (string)reader["name_type"];
                        zap.name_type = zap.name_type.Trim();
                    }

                    TypeAlgs.AlgList.Add(zap);

                }
                ret.tag = i;
                reader.Close();
                conn_db.Close();
                return TypeAlgs.AlgList;
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

                MonitorLog.WriteLog("Ошибка заполнения справочника типы алгоритма " + err, MonitorLog.typelog.Error, 20, 201, true);

                return null;
            }
        }

        //----------------------------------------------------------------------
        public List<Namereg> LoadNamereg(Namereg finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();

            List<Namereg> spis = new List<Namereg>();

            string conn_kernel = Points.GetConnByPref(finder.pref);
            IDbConnection conn_db = GetConnection(conn_kernel);
            //IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(conn_db, true);

            if (!ret.result) return null;
            ret = ExecSQL(conn_db, "database " + finder.pref.Trim() + "_data", true);
            if (!ret.result)
            {
                conn_db.Close();
                return null;
            }

            //выбрать список
            string cols = "kod_namereg, namereg";
            string ogrn = "";
            if (isTableHasColumn(conn_db, "s_namereg", "ogrn"))
            {
                ogrn = "ogrn";
                cols += "," + ogrn;
            }

            string inn = "";
            if (isTableHasColumn(conn_db, "s_namereg", "inn"))
            {
                inn = "inn";
                cols += "," + inn;
            }

            string kpp = "";
            if (isTableHasColumn(conn_db, "s_namereg", "kpp"))
            {
                kpp = "kpp";
                cols += "," + kpp;
            }

            string adr_namereg = "";
            if (isTableHasColumn(conn_db, "s_namereg", "adr_namereg"))
            {
                adr_namereg = "adr_namereg";
                cols += "," + adr_namereg;
            }

            string tel_namereg = "";
            if (isTableHasColumn(conn_db, "s_namereg", "tel_namereg"))
            {
                tel_namereg = "tel_namereg";
                cols += "," + tel_namereg;
            }

            string dolgnost = "";
            if (isTableHasColumn(conn_db, "s_namereg", "dolgnost"))
            {
                dolgnost = "dolgnost";
                cols += "," + dolgnost;
            }

            string fio_namereg = "";
            if (isTableHasColumn(conn_db, "s_namereg", "fio_name_reg"))
            {
                fio_namereg = "fio_namereg";
                cols += "," + fio_namereg;
            }

#if PG
            string sql = " Select " + cols + " From " + finder.pref + "_data.s_namereg order by namereg";
#else
            string sql = " Select " + cols + " From " + finder.pref + "_data:s_namereg order by namereg";
#endif

            IDataReader reader;
            if (!ExecRead(conn_db, out reader, sql, true).result)
            {
                conn_db.Close();
                return null;
            }
            try
            {
                while (reader.Read())
                {
                    Namereg zap = new Namereg();
                    if (reader["kod_namereg"] != DBNull.Value) zap.kod_namereg = (int)reader["kod_namereg"];
                    if (reader["namereg"] != DBNull.Value) zap.namereg = Convert.ToString(reader["namereg"]).Trim();
                    if (ogrn != "") if (reader["ogrn"] != DBNull.Value) zap.ogrn = Convert.ToString(reader["ogrn"]).Trim();
                    if (inn != "") if (reader["inn"] != DBNull.Value) zap.inn = Convert.ToString(reader["inn"]).Trim();
                    if (kpp != "") if (reader["kpp"] != DBNull.Value) zap.kpp = Convert.ToString(reader["kpp"]).Trim();
                    if (adr_namereg != "") if (reader["adr_namereg"] != DBNull.Value) zap.adr_namereg = Convert.ToString(reader["adr_namereg"]).Trim();
                    if (tel_namereg != "") if (reader["tel_namereg"] != DBNull.Value) zap.tel_namereg = Convert.ToString(reader["tel_namereg"]).Trim();

                    spis.Add(zap);
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

                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;

                MonitorLog.WriteLog("Ошибка заполнения списка namereg " + err, MonitorLog.typelog.Error, 20, 201, true);

                return null;
            }
        }

        //пример обращения для получения кодов 1,2,10 -nzp_kvar,num_ls,pkod10
        // Series series = new Series( new int[]{1,2,10} );
        // DbEditInterData.GetSeries('vas', series, out ret);
        // if (ret.result) 
        // {
        //      val  = series.GetSeries(1)
        //      if (val.cur_val != Zero) nzp_kvar = val.cur_val
        //      kod2  = series.GetSeries(2);
        //      kod10 = series.GetSeries(10);
        // }
        //----------------------------------------------------------------------
        public void GetSeries(string pref, Series series, out Returns ret)
        {
            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result)
            {
                return;
            }

            GetSeries(conn_db, pref, series, out ret);

            conn_db.Close();
        }

        public void GetSeries(IDbConnection connection, string pref, Series series, out Returns ret)
        {
            ret = Utils.InitReturns();

            IDbTransaction trans = connection.BeginTransaction();
            GetSeries(connection, trans, pref, series, out ret);
            if (ret.result) trans.Commit();
            else trans.Rollback();
        }

        public void GetSeries(IDbConnection connection, IDbTransaction trans, string pref, Series series, out Returns ret)
        {
            if (series == null)
            {
                ret = new Returns(false, "Входной параметр series не задан");
                return;
            }


            if (Points.isUseSeries) // работа с таблицей series
            {
                GetSeriesUseSeries(connection, trans, pref, series, out ret);
            }
            else
            {
                GetSeriesWithoutSeries(connection, trans, pref, series, out ret);
            }

        }

        public void GetSeriesUseSeries(IDbConnection connection, IDbTransaction trans, string pref, Series series, out Returns ret)
        {

            /*
                        // Rust 
                        // Получить значения ключей 
                        IDataReader reader;
                        string sql = "select " + pref + "_data:get_seq_sel(kod,1) as cur_val, kod, v_min,v_max from " + pref + "_data:series where kod in ( " + series.GetStringKod() + ")";            
                        ret = ExecRead(connection, trans, out reader, sql, true);
                        if (!ret.result)
                        {
                            ret.text = "Ошибка блокирования series";
                           // ExecSQL(connection, trans, " SET ISOLATION TO DIRTY READ ", true);
                            return;
                        }
                        // все остальное как обычно
                        while (reader.Read())
                        {
                            _Series val = series.EmptyVal();
                            val = series.GetSeries((int)reader["kod"]);
                            if (val.kod == Constants._ZERO_)
                            {
                                ret.text = "Внутренняя ошибка series";
                                ret.result = false;
                                reader.Close();
                                reader.Dispose();

                                return;
                            }

                            val.v_min = (int)reader["v_min"];
                            val.v_max = (int)reader["v_max"];
                            val.cur_val = (int)reader["cur_val"];
                            series.PutVal(val);

                            if (val.getAndInc)
                            {
                                if (val.cur_val < 1 || val.cur_val < val.v_min)
                                {
                                    reader.Close();
                                    reader.Dispose();
                                    ret.text = "Значение series с кодом " + val.kod + " в банке данных " + pref + " выходит за границы допустимого диапазона";
                                    ret.result = false;
                                    if (Constants.Debug) MonitorLog.WriteLog("Функция GetSeries()\n" + ret.text, MonitorLog.typelog.Error, 2, 100, true);
                                    return;
                                }

                                if (val.cur_val > val.v_max)
                                {
                                    reader.Close();
                                    reader.Dispose();
                                    ret.text = "Лимит series с кодом " + val.kod + " в банке данных " + pref + " исчерпан";
                                    ret.result = false;
                                    if (Constants.Debug) MonitorLog.WriteLog("Функция GetSeries()\n" + ret.text, MonitorLog.typelog.Error, 2, 100, true);
                                    return;
                                }

                          //      ret = ExecSQL(connection, trans,
                          //          " Update " + pref + "_data:series " +
                          //          " Set cur_val = cur_val + 1 " +
                          //          " Where kod = " + val.kod
                          //          , true);
                                if (!ret.result)
                                {
                                    reader.Close();
                                    reader.Dispose();
                                    ret.text = "Ошибка выделения series";
                                    ret.result = false;
                                    return;
                                }
                            }
                        }
                        reader.Close();
                        reader.Dispose();


              }
                    */
            // Rust


#if PG
            ret = ExecSQL(connection, trans, "begin; SET TRANSACTION ISOLATION LEVEL READ COMMITTED; ", true);
#else
            ret = ExecSQL(connection, trans, " SET ISOLATION TO COMMITTED READ ", true);
#endif
            if (!ret.result)
            {
                ret.text = "Не удалось установить уровень изоляции на COMMITTED READ: " + ret.text;
                return;
            }

            //попытка заблокировать series
#if PG
            ret = ExecSQL(connection, trans, " Lock table " + pref + "_data.series in exclusive mode ", true);
#else
            ret = ExecSQL(connection, trans, " Lock table " + pref + "_data:series in exclusive mode ", true);
#endif
            if (!ret.result)
            {
                ret.text = "Ошибка блокирования series";
#if PG
                ExecSQL(connection, trans, "SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED ", true);
#else
                ExecSQL(connection, trans, " SET ISOLATION TO DIRTY READ ", true);
#endif
                return;
            }

            MyDataReader reader = null;

            try
            {
                //вытащим заказанные коды
#if PG
                string sql = " Select * From " + pref + "_data.series " +
#else
                string sql = " Select * From " + pref + "_data:series " +
#endif
 " Where kod in (" + series.GetStringKod() + ") Order by kod ";
                ret = ExecRead(connection, trans, out reader, sql, true);
                if (!ret.result) return;

                while (reader.Read())
                {
                    _Series val = series.EmptyVal();
                    val = series.GetSeries((int)reader["kod"]);

                    if (val.kod == Constants._ZERO_)
                    {
                        ret.text = "Внутренняя ошибка series";
                        ret.result = false;
                        return;
                    }

                    val.v_min = (int)reader["v_min"];
                    val.v_max = (int)reader["v_max"];
                    val.cur_val = (int)reader["cur_val"];

                    series.PutVal(val);

                    if (val.getAndInc)
                    {
                        if (val.cur_val < 1 || val.cur_val < val.v_min)
                        {
                            ret.text = "Значение series с кодом " + val.kod + " в банке данных " + pref + " выходит за границы допустимого диапазона";
                            ret.result = false;
                            if (Constants.Debug) MonitorLog.WriteLog("Функция GetSeries()\n" + ret.text, MonitorLog.typelog.Error, 2, 100, true);
                            return;
                        }

                        if (val.cur_val > val.v_max)
                        {
                            ret.text = "Лимит series с кодом " + val.kod + " в банке данных " + pref + " исчерпан";
                            ret.result = false;
                            if (Constants.Debug) MonitorLog.WriteLog("Функция GetSeries()\n" + ret.text, MonitorLog.typelog.Error, 2, 100, true);
                            return;
                        }

                        ret = ExecSQL(connection, trans,
#if PG
                        " Update " + pref + "_data.series " +
#else
 " Update " + pref + "_data:series " +
#endif
 " Set cur_val = cur_val + 1 " +
                            " Where kod = " + val.kod
                            , true);
                        if (!ret.result)
                        {
                            ret.text = "Ошибка выделения series";
                            ret.result = false;
                            return;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка GetSeries()\n" + ret.text, MonitorLog.typelog.Error, 2, 100, true);
            }
            finally
            {
                if (reader != null) reader.Close();
#if PG
                ExecSQL(connection, trans, " commit; SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED; ", true);
#else
                ExecSQL(connection, trans, " SET ISOLATION TO DIRTY READ ", true);
#endif
            }
        }

        public void GetSeriesWithoutSeries(IDbConnection connection, IDbTransaction trans, string pref, Series series, out Returns ret)
        {
            ret = Utils.InitReturns();
            MyDataReader reader = null;

            try
            {
                List<int> list = series.GetListKod();
                if (list == null || list.Count <= 0)
                {
                    ret.text = "Внутренняя ошибка series";
                    ret.result = false;
                    return;
                }
                string sql = "";

                //вытащим заказанные коды
#if PG
                sql = " SELECT nextval('"+Points.Pref+"_data.kvar_nzp_kvar_seq') as nzp_kvar, "+
                             " nextval('" + Points.Pref + "_data.kvar_num_ls_seq') as num_ls, " +
                             " nextval('" + Points.Pref + "_data.dom_nzp_dom_seq') as nzp_dom, " +
                             " nextval('" + Points.Pref + "_data.s_ulica_nzp_ul_seq') as nzp_ul, " +
                             " nextval('" + Points.Pref + "_data.s_geu_nzp_geu_seq') as nzp_geu, " +
                             " nextval('" + Points.Pref + "_data.s_area_nzp_area_seq') as nzp_area, " +
                             " nextval('" + Points.Pref + "_kernel.s_payer_nzp_payer_seq') as nzp_payer, " +
                             " nextval('" + Points.Pref + "_kernel.supplier_nzp_supp_seq') as nzp_supp, " +
                             " nextval('" + Points.Pref + "_data.counters_spis_nzp_counter_seq') as nzp_counter";
#else
                sql = " SELECT " + Points.Pref + "_data:kvar_nzp_kvar_seq.nextval as nzp_kvar, " +
                           Points.Pref + "_data:kvar_num_ls_seq.nextval as num_ls, " +
                           Points.Pref + "_data:dom_nzp_dom_seq.nextval as nzp_dom, " +
                           Points.Pref + "_data:s_ulica_nzp_ul_seq.nextval as nzp_ul, " +
                           Points.Pref + "_data:s_geu_nzp_geu_seq.nextval as nzp_geu, " +
                           Points.Pref + "_data:s_area_nzp_area_seq.nextval as nzp_area, " +
                           Points.Pref + "_kernel:s_payer_nzp_payer_seq.nextval as nzp_payer, " +
                           Points.Pref + "_kernel:supplier_nzp_supp_seq.nextval as nzp_supp, " +
                           Points.Pref + "_data:counters_spis_nzp_counter_seq.nextval as nzp_counter from " + Points.Pref + "_data:dual";
#endif
                ret = ExecRead(connection, trans, out reader, sql, true);
                if (!ret.result) return;

                if (reader.Read())
                {
                    foreach (int kod in list)
                    {
                        _Series val = series.EmptyVal();
                        val = series.GetSeries(kod);

                        if (val.kod == Constants._ZERO_)
                        {
                            ret.text = "Внутренняя ошибка series";
                            ret.result = false;
                            return;
                        }

                        val.v_min = 1;
                        val.v_max = 2147483647;

                        if (kod == Series.Types.Area.GetHashCode()) val.cur_val = Convert.ToInt32(reader["nzp_area"]);
                        else if (kod == Series.Types.Counter.GetHashCode()) val.cur_val = Convert.ToInt32(reader["nzp_counter"]);
                        else if (kod == Series.Types.Dom.GetHashCode()) val.cur_val = Convert.ToInt32(reader["nzp_dom"]);
                        else if (kod == Series.Types.Geu.GetHashCode()) val.cur_val = Convert.ToInt32(reader["nzp_geu"]);
                        else if (kod == Series.Types.Kvar.GetHashCode()) val.cur_val = Convert.ToInt32(reader["nzp_kvar"]);
                        else if (kod == Series.Types.NumLs.GetHashCode()) val.cur_val = Convert.ToInt32(reader["num_ls"]);
                        else if (kod == Series.Types.Payer.GetHashCode()) val.cur_val = Convert.ToInt32(reader["nzp_payer"]);
                        else if (kod == Series.Types.Supplier.GetHashCode()) val.cur_val = Convert.ToInt32(reader["nzp_supp"]);
                        else if (kod == Series.Types.Ulica.GetHashCode()) val.cur_val = Convert.ToInt32(reader["nzp_ul"]);
                        series.PutVal(val);

                        if (val.getAndInc)
                        {
                            /* if (kod == Series.Types.NumLs.GetHashCode())
                              {
                                  ret = ExecSQL(connection, trans, "SELECT setval('"+Points.Pref+"_data.kvar_num_ls_seq', max(num_ls)) FROM fsmr_data.kvar", true);
                                  if (!ret.result)
                                  {
                                      ret.text = "Ошибка выделения series";
                                      ret.result = false;
                                      return;
                                  }
                              }*/
                        }
                    }

                }
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка GetSeries()\n" + ret.text, MonitorLog.typelog.Error, 2, 100, true);
            }
        }


        public Returns GetNewId(IDbConnection connection, IDbTransaction transaction, Series.Types number, string pref)
        {
            if (pref.Trim() == "" &&
                (number == Series.Types.Counter ||
                number == Series.Types.Dom ||
                number == Series.Types.Kvar ||
                number == Series.Types.NumLs ||
                number == Series.Types.PKod10))
            {
                return new Returns(false, "Не задан префикс базы данных");
            }

            string prefix = pref == null || pref.Trim() == "" ? Points.Pref : pref.Trim();

            if (number == Series.Types.Area ||
                number == Series.Types.Geu ||
                number == Series.Types.Payer ||
                number == Series.Types.Supplier ||
                number == Series.Types.Ulica)
            {
                prefix = Points.Pref;
            }

            Returns ret;

            Series series = new Series(new int[] { (int)number });
            GetSeries(connection, transaction, prefix, series, out ret);

            if (!ret.result) return ret;

            _Series val = series.GetSeries((int)number);
            if (val.cur_val < 1)
            {
                ret.text = "Не определен series с кодом " + (int)number;
                ret.result = false;
                MonitorLog.WriteLog(ret.text, MonitorLog.typelog.Error, true);
                return ret;
            }

            ret.tag = val.cur_val;
            return ret;
        }

        public Returns GetNewId(IDbConnection connection, IDbTransaction transaction, Series.Types number)
        {
            return GetNewId(connection, transaction, number, Points.Pref);
        }

        public Returns GetNewId(IDbConnection connection, Series.Types number, string pref)
        {
            Returns ret = Utils.InitReturns();

            IDbTransaction trans = connection.BeginTransaction();
            ret = GetNewId(connection, trans, number, pref);
            if (ret.result) trans.Commit();
            else trans.Rollback();

            return ret;
        }

        public Returns GetNewId(IDbConnection connection, Series.Types number)
        {
            return GetNewId(connection, number, Points.Pref);
        }

        public List<Measure> LoadMeasure(Measure finder, out Returns ret)
        {
            if (finder.nzp_user <= 0)
            {
                ret = new Returns(false, "Не задан пользователь");
                return null;
            }

            ret = Utils.InitReturns();

            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return null;

            DbTables tables = new DbTables(DBManager.getServer(conn_db));

            List<Measure> list = new List<Measure>();

            //выбрать список
            string sql = " Select nzp_measure, measure From " + tables.measure +
                         " Order by measure ";

            IDataReader reader;
            if (!ExecRead(conn_db, out reader, sql.ToString(), true).result)
            {
                conn_db.Close();
                return null;
            }
            try
            {
                while (reader.Read())
                {
                    Measure zap = new Measure();

                    if (reader["nzp_measure"] != DBNull.Value) zap.nzp_measure = (int)reader["nzp_measure"];

                    if (reader["measure"] != DBNull.Value) zap.measure = ((string)reader["measure"]).Trim();

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

                MonitorLog.WriteLog("Ошибка заполнения справочника единицы измерения " + err, MonitorLog.typelog.Error, 20, 201, true);

                return null;
            }
        }

        public List<CalcMethod> LoadCalcMethod(CalcMethod finder, out Returns ret)
        {
            if (finder.nzp_user <= 0)
            {
                ret = new Returns(false, "Не задан пользователь");
                return null;
            }

            ret = Utils.InitReturns();

            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return null;

            DbTables tables = new DbTables(DBManager.getServer(conn_db));

            List<CalcMethod> list = new List<CalcMethod>();

            //выбрать список
            string sql = " Select nzp_calc_method, method_name From " + tables.calc_method +
                         " Order by method_name ";

            IDataReader reader;
            if (!ExecRead(conn_db, out reader, sql.ToString(), true).result)
            {
                conn_db.Close();
                return null;
            }
            try
            {
                while (reader.Read())
                {
                    CalcMethod zap = new CalcMethod();

                    if (reader["nzp_calc_method"] != DBNull.Value) zap.nzp_calc_method = (int)reader["nzp_calc_method"];

                    if (reader["method_name"] != DBNull.Value) zap.method_name = ((string)reader["method_name"]).Trim();

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

                MonitorLog.WriteLog("Ошибка заполнения справочника единицы измерения " + err, MonitorLog.typelog.Error, 20, 201, true);

                return null;
            }
        }

        public List<PackTypes> LoadPackTypes(PackTypes finder, out Returns ret)
        {
            if (finder.nzp_user <= 0)
            {
                ret = new Returns(false, "Не задан пользователь");
                return null;
            }

            ret = Utils.InitReturns();

            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return null;

            DbTables tables = new DbTables(DBManager.getServer(conn_db));

            List<PackTypes> list = new List<PackTypes>();

            //выбрать список
            string sql = " Select id, type_name From " + tables.pack_types +
                         " Order by type_name ";

            MyDataReader reader;
            if (!ExecRead(conn_db, out reader, sql.ToString(), true).result)
            {
                conn_db.Close();
                return null;
            }
            try
            {
                while (reader.Read())
                {
                    PackTypes zap = new PackTypes();

                    if (reader["id"] != DBNull.Value) zap.id = (int)reader["id"];

                    if (reader["type_name"] != DBNull.Value) zap.type_name = ((string)reader["type_name"]).Trim();

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

                MonitorLog.WriteLog("Ошибка заполнения справочника единицы измерения " + err, MonitorLog.typelog.Error, 20, 201, true);

                return null;
            }
        }
        //todo postgree
        public List<BankPayers> BankPayersLoad(Supplier finder, enTypeOfSupp type, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            if (finder.pref == "") finder.pref = Points.Pref;
            List<BankPayers> spis = new List<BankPayers>();

            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return null;

            DbTables tables = new DbTables(DBManager.getServer(conn_db));

            string[] tabname_fin = {Points.Pref + "_fin_" + (Points.DateOper.Year - 2000).ToString("00"),
                Points.Pref + "_fin_" + (Points.DateOper.Year - 2001).ToString("00"),
                Points.Pref + "_fin_" + (Points.DateOper.Year - 2002).ToString("00"),
                Points.Pref + "_fin_" + (Points.DateOper.Year - 2003).ToString("00")
            };
            // Условия поиска
            string where = "";

            if (finder.nzp_supp != 0) where += " and s.nzp_supp = " + finder.nzp_supp;

            if (finder.dopFind != null && finder.dopFind.Count > 0)
                where += " and upper(s.name_supp) like '%" + finder.dopFind[0].ToUpper().Replace("'", "''").Replace("*", "%") + "%' ";

            if (finder.RolesVal != null)
                foreach (_RolesVal role in finder.RolesVal)
                    if (role.tip == Constants.role_sql && role.kod == Constants.role_sql_supp) where += " and s.nzp_supp in (" + role.val + ")";

            if (type == enTypeOfSupp.NotInListPayers)
            {
#if PG
                where += " and s.nzp_supp not in ( Select distinct p.nzp_supp From " + tables.payer + " p Where p.nzp_type = 2)";
#else
                where += " and s.nzp_supp not in ( Select unique p.nzp_supp From " + tables.payer + " p Where p.nzp_type = 2)";
#endif
            }

            //Определить общее количество записей
#if PG
            string sql = "Select count(*) From " + finder.pref + "_kernel.supplier s Where 1 = 1 " + where;
#else
            string sql = "Select count(*) From " + finder.pref + "_kernel:supplier s Where 1 = 1 " + where;
#endif
            object count = ExecScalar(conn_db, sql, out ret, true);
            int recordsTotalCount;
            try { recordsTotalCount = Convert.ToInt32(count); }
            catch (Exception e)
            {
                ret = new Returns(false, "Ошибка при определении количества записей: " + (Constants.Debug ? e.Message : ""));
                MonitorLog.WriteLog("Ошибка SupplierLoad " + (Constants.Viewerror ? "\n" + e.Message : ""), MonitorLog.typelog.Error, 20, 201, true);
                conn_db.Close();
                return null;
            }

            //выбрать список
            if (!Points.isFinances)
            {
#if PG
                sql = " select distinct s.nzp_supp, s.name_supp, 0 as nzp_payer, s.adres_supp, s.phone_supp, s.geton_plat, s.have_proc, s.kod_supp " +
                      " from " + finder.pref + "_kernel.supplier s " +
#else
                sql = " Select unique s.nzp_supp, s.name_supp, 0 as nzp_payer, s.adres_supp, s.phone_supp, s.geton_plat, s.have_proc, s.kod_supp " +
                      " From " + finder.pref + "_kernel:supplier s " +
#endif
 " Where 1 = 1 " + where +
                      " Order by name_supp";
            }
            else
            {
#if PG
                sql =
                    new StringBuilder(
                        " Select distinct s.nzp_supp, s.name_supp, p.nzp_payer, s.adres_supp, s.phone_supp, s.geton_plat, s.have_proc, s.kod_supp ")
                        .Append(" From " + finder.pref + "_kernel.supplier s left outer join " + Points.Pref + "_kernel.s_payer p ")
                        .Append(" on s.nzp_supp = p.nzp_supp ")
                        .Append(where.ReplaceFirstOccurence("and", "where"))
                        .Append(" Order by name_supp")
                        .ToString();
#else
                sql = " Select unique s.nzp_supp, s.name_supp, p.nzp_payer, s.adres_supp, s.phone_supp, s.geton_plat, s.have_proc, s.kod_supp, pb.payer as bank " +
                      " From " + finder.pref + "_kernel:supplier s, " + Points.Pref + "_kernel:s_payer p, " + Points.Pref + "_kernel:s_payer pb " +
                      " Where p.id_bc_type is null and s.nzp_supp = p.nzp_supp " + where +
                      " and pb.nzp_payer in (select fn.nzp_payer_bank from " + Points.Pref + "_data:fn_bank fn where fn.nzp_payer=p.nzp_payer) " +
                      " and pb.id_bc_type is not null  " +
                      " Order by s.name_supp";
#endif

            }

            IDataReader reader;
            if (!ExecRead(conn_db, out reader, sql, true).result)
            {
                conn_db.Close();
                return null;
            }
            try
            {
                int i = 0, j = 0;
                while (reader.Read())
                {
                    i++;
                    if (i <= finder.skip) continue;
                    BankPayers zap = new BankPayers();



                    if (reader["nzp_supp"] != DBNull.Value) zap.nzp_supp = Convert.ToInt32(reader["nzp_supp"]);
                    if (reader["name_supp"] != DBNull.Value) zap.name_supp = Convert.ToString(reader["name_supp"]).Trim();
                    if (reader["nzp_payer"] != DBNull.Value) zap.nzp_payer = Convert.ToInt32(reader["nzp_payer"]);
                    if (reader["bank"] != DBNull.Value) zap.bank = Convert.ToString(reader["bank"]).Trim();
                    zap.check = false;
                    if (reader["adres_supp"] != DBNull.Value) zap.adres_supp = Convert.ToString(reader["adres_supp"]).Trim();
                    if (reader["geton_plat"] != DBNull.Value) zap.geton_plat = Convert.ToString(reader["geton_plat"]).Trim();
                    if (reader["have_proc"] != DBNull.Value) zap.have_proc = Convert.ToInt32(reader["have_proc"]);
                    if (reader["kod_supp"] != DBNull.Value) zap.kod_supp = Convert.ToString(reader["kod_supp"]).Trim();

                    try
                    {
                        sql = " Select nvl(sum(nvl(f1.sum_send,0))+sum(nvl(f2.sum_send,0))+sum(nvl(f3.sum_send,0))+sum(nvl(f4.sum_send,0)),0) as sum_send " +
                            " from " + tabname_fin[3] + ":fn_sended f1 left outer join " + tabname_fin[2] + ":fn_sended f2 " +
                            " on f1.nzp_snd=f2.nzp_snd left outer join " + tabname_fin[1] + ":fn_sended f3 " +
                            " on f2.nzp_snd=f3.nzp_snd left outer join " + tabname_fin[0] + ":fn_sended f4 " +
                            " on f3.nzp_snd=f4.nzp_snd where f1.nzp_payer=" + zap.nzp_payer + " and f1.id_bc_file is null ";

                        var dt = ClassDBUtils.OpenSQL(sql, conn_db);
                        zap.summ_supp = dt.resultData.Rows[0][0].ToString();
                    }
                    catch
                    {
                        zap.summ_supp = "0";
                    }

                    if (zap.summ_supp != "0.0")
                    {
                        j++;
                        zap.num = j;
                        spis.Add(zap);
                    }
                    if (finder.rows > 0 && i >= finder.skip + finder.rows) break;
                }

                ret.tag = recordsTotalCount;

                reader.Close();
                conn_db.Close();
                return spis;
            }
            catch (Exception ex)
            {
                reader.Close();
                conn_db.Close();
                ret = new Returns(false, ex.Message);
                MonitorLog.WriteLog("Ошибка заполнения списка поставщиков " + (Constants.Viewerror ? " \n " + ex.Message : ""), MonitorLog.typelog.Error, 20, 201, true);
                return null;
            }
        }

        public List<Supplier> LoadSupplierSpis(Area_ls finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<Supplier> suppList = new List<Supplier>();
            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return null;
            StringBuilder str = new StringBuilder();
            str.Append("select * from " + finder.pref + "_kernel" + tableDelimiter + "supplier where ( nzp_supp not in (select nzp_supp from " + finder.pref + "_data" + tableDelimiter
                + "alias_ls where nzp_kvar=" + finder.nzp_kvar + ") or nzp_supp=" + finder.nzp_supp + ") and nzp_supp>0");
            IDataReader reader;
            if (!ExecRead(conn_db, out reader, str.ToString(), true).result)
            {
                conn_db.Close();
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка при формировании списка поставщиков";
                return null;
            }

            try
            {
                int i = 0;
                while (reader.Read())
                {
                    Supplier zap = new Supplier();

                    zap.name_supp = reader["name_supp"] == DBNull.Value ? "" : reader["name_supp"].ToString().Trim();
                    zap.nzp_supp = reader["nzp_supp"] == DBNull.Value ? 0 : Convert.ToInt32(reader["nzp_supp"]);
                    suppList.Add(zap);
                }

                reader.Close();
                conn_db.Close();
                return suppList;
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

                MonitorLog.WriteLog("Ошибка при формировании списка поставщиков " + err, MonitorLog.typelog.Error, 20, 201, true);

                return null;
            }
        }
        public List<BankPayers> BankPayersLoadBC(Supplier finder, enTypeOfSupp type, out Returns ret)
        //----------------------------------------------------------------------
        {
            if (finder.pref == "") finder.pref = Points.Pref;
            List<BankPayers> spis = new List<BankPayers>();

            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return null;

            DbTables tables = new DbTables(DBManager.getServer(conn_db));

            StringBuilder zapros = new StringBuilder();
            var listDate = new List<byte>();
            byte beginYear = Convert.ToByte(Points.BeginWork.year_ - 2000),
                    endYear = Convert.ToByte(DateTime.Now.Year - 2000);
            for (var i = beginYear; i <= endYear; i++)
            {
                string finTable = Points.Pref + "_fin_" + i + DBManager.tableDelimiter + "fn_sended";
                if (TempTableInWebCashe(conn_db,finTable))
                {
                    listDate.Add(i);
                }
            }
            ret.result = true;
            string sql;
            sql = "Select  p.nzp_payer, p.payer, pb.payer as bank " +
                " From  " + Points.Pref + "_kernel" + DBManager.tableDelimiter + "s_payer p, " + Points.Pref + "_kernel" + DBManager.tableDelimiter + "s_payer pb " +
                " Where pb.nzp_payer in (select fn.nzp_payer_bank from " + Points.Pref + "_data" + DBManager.tableDelimiter + "fn_bank fn where fn.nzp_payer=p.nzp_payer) " +
                " AND pb.id_bc_type is not null ";

            IDataReader reader;
            if (!ExecRead(conn_db, out reader, sql, true).result)
            {
                conn_db.Close();
                return null;
            }
            try
            {
                int i = 0, j = 0;
                while (reader.Read())
                {
                    i++;
                    if (i <= finder.skip) continue;
                    BankPayers zap = new BankPayers();
                    if (reader["nzp_payer"] != DBNull.Value) zap.nzp_supp = Convert.ToInt32(reader["nzp_payer"]); else zap.nzp_supp = -1;
                    if (reader["payer"] != DBNull.Value) zap.name_supp = Convert.ToString(reader["payer"]).Trim();
                    if (reader["bank"] != DBNull.Value) zap.bank = Convert.ToString(reader["bank"]).Trim();
                    zap.check = false;

                    try
                    {
                        //sql = " Select nvl(sum(nvl(f1.sum_send,0))+sum(nvl(f2.sum_send,0))+sum(nvl(f3.sum_send,0))+sum(nvl(f4.sum_send,0)),0) as sum_send " +
                        //    " from " + tabname_fin[3] + ":fn_sended f1 left outer join " + tabname_fin[2] + ":fn_sended f2 " +
                        //    " on f1.nzp_snd=f2.nzp_snd left outer join " + tabname_fin[1] + ":fn_sended f3 " +
                        //    " on f2.nzp_snd=f3.nzp_snd left outer join " + tabname_fin[0] + ":fn_sended f4 " +
                        //    " on f3.nzp_snd=f4.nzp_snd where f1.nzp_payer=" + zap.nzp_payer + " and f1.id_bc_file is null ";
                        decimal sumSup = 0.0M, allSum = 0.0M;
                        foreach (var l in listDate)
                        {
                            sumSup = 0.0M;
                            /*sql = "SELECT sum(fn.sum_send)" +
                                " FROM " + Points.Pref + "_fin_" + l + DBManager.tableDelimiter + "fn_sended fn, " + Points.Pref + "_data" + DBManager.tableDelimiter + " bc_reestr_files bc " +
                                " WHERE fn.nzp_payer=" + zap.nzp_supp + " AND  (fn.id_bc_file IS NULL OR (fn.id_bc_file IS NOT NULL AND fn.id_bc_file = bc.id AND bc.is_treaster=1)) ";*/

                            sql = " SELECT SUM(sum_send) " +
                                  " FROM ((SELECT DISTINCT nzp_payer, SUM(sum_send) AS sum_send " +
                                         " FROM " + Points.Pref + "_fin_" + l + DBManager.tableDelimiter + "fn_sended " +
                                         " WHERE id_bc_file IS NULL " +
                                         " GROUP BY nzp_payer) " +
                                         " UNION " +
                                         " (SELECT DISTINCT fn.nzp_payer,SUM(fn.sum_send) AS sum_send " +
                                         " FROM " + Points.Pref + "_fin_" + l + DBManager.tableDelimiter + "fn_sended fn, " +
                                              Points.Pref + DBManager.sDataAliasRest + "bc_reestr_files bc " +
                                         " WHERE fn.id_bc_file IS NOT NULL AND fn.id_bc_file = bc.id AND bc.is_treaster=1 " +
                                         " GROUP BY nzp_payer)) " +
                                  " WHERE nzp_payer=" + zap.nzp_supp;

                            var dt = ClassDBUtils.OpenSQL(sql, conn_db);
                            if (decimal.TryParse(dt.resultData.Rows[0][0].ToString(), out sumSup)) { allSum += sumSup; }

                        }
                        zap.summ_supp = allSum.ToString();
                    }
                    catch
                    {
                        zap.summ_supp = "0.0";
                    }

                    if (zap.summ_supp != "0.0")
                    {
                        j++;
                        zap.num = j;
                        spis.Add(zap);
                    }
                    if (finder.rows > 0 && i >= finder.skip + finder.rows) break;
                }

                reader.Close();
                conn_db.Close();
                return spis;
            }
            catch (Exception ex)
            {
                reader.Close();
                conn_db.Close();
                ret = new Returns(false, ex.Message);
                MonitorLog.WriteLog("Ошибка заполнения списка поставщиков " + (Constants.Viewerror ? " \n " + ex.Message : ""), MonitorLog.typelog.Error, 20, 201, true);
                return null;
            }
        }

    }



    

    /// <summary>
    /// Типы баз данных
    /// </summary>
    public enum BaselistTypes
    {
        /// <summary>
        /// Начисления
        /// </summary>
        Charge = 1,

        /// <summary>
        /// Характеристики жилья и т.п.
        /// </summary>
        Data = 2,

        /// <summary>
        /// Системный
        /// </summary>
        Kernel = 3,

        /// <summary>
        /// Финансовый
        /// </summary>
        Fin = 4,

        Tbo = 5,
        Cds = 6,
        Mail = 7,
        WebFon = 8,
        WebCds = 9,

        /// <summary>
        /// Основной банк данных (используется в банке-клоне)
        /// </summary>
        PrimaryBank = 10
    }
}