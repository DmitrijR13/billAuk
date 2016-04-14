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
    public partial class DbSpravClient : DataBaseHead
    {
        //----------------------------------------------------------------------
        public void CreateWebService(IDbConnection conn_web, string table, out Returns ret)
        //----------------------------------------------------------------------
        {
            if (TableInWebCashe(conn_web, table))
            {
                ExecSQL(conn_web, " Drop table " + table, false);
            }

            //создать таблицу webdata:services
#if PG
            ret = ExecSQL(conn_web,
                         " CREATE TABLE " + table +
                         " ( nzp_serv SERIAL NOT NULL, " +
                           " service CHAR(100), " +
                           " service_small CHAR(20), " +
                           " service_name CHAR(100), " +
                           " ed_izmer CHAR(30), " +
                           " type_lgot INTEGER, " +
                           " nzp_frm INTEGER, " +
                           " ordering INTEGER ) ", true);
#else
   ret = ExecSQL(conn_web,
                " CREATE TABLE " + table +
                " ( nzp_serv SERIAL NOT NULL, " +
                  " service CHAR(100), " +
                  " service_small CHAR(20), " +
                  " service_name CHAR(100), " +
                  " ed_izmer CHAR(30), " +
                  " type_lgot INTEGER, " +
                  " nzp_frm INTEGER, " +
                  " ordering INTEGER ) ", true);
#endif

            if (!ret.result)
            {
                return;
            }

            uint tabid = TableInWebCasheID(conn_web, table);
            string ix = "ix" + tabid + "_" + table;

#if PG
            ret = ExecSQL(conn_web, " CREATE UNIQUE INDEX " + ix + " ON " + table + " (nzp_serv)", true);
#else
ret = ExecSQL(conn_web, " CREATE UNIQUE INDEX " + ix + " ON " + table + " (nzp_serv)", true);
#endif
        }

        //----------------------------------------------------------------------
        public List<_Service> ServiceLoad_WebData(Finder finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();

            Services spis = new Services();
            spis.ServiceList.Clear();

            IDbConnection conn_db = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return null;

            string where = "";
            if (finder.RolesVal != null)
                foreach (_RolesVal role in finder.RolesVal)
                {
                    if (role.tip == Constants.role_sql & role.kod == Constants.role_sql_serv)
                        where += " and nzp_serv in (" + role.val + ")";
                }

            if (finder.dopFind != null && finder.dopFind.Count > 0)
            {
                string str = finder.dopFind[0];
                if (!str.Contains("FiltrOnDistrib"))
                {
                    where += " and upper(service) like '%" + str.ToUpper().Replace("'", "''").Replace("*", "%") + "%'";
                }
            }

            string tab_services = "services"; 
            if (finder.nzp_server > 0)
                tab_services += "_" + finder.nzp_server;

            //выбрать список
            IDataReader reader;
#if PG
            if (!ExecRead(conn_db, out reader,
                 " Select nzp_serv, service, service_small, service_name, ed_izmer, type_lgot, nzp_frm, ordering " +
                 " From " + tab_services +
                 " Where 1 = 1 " + where +
                 "   and nzp_serv > 1 " +
                 " Order by service ", true).result)
            {
                conn_db.Close();
                return null;
            }
#else
           if (!ExecRead(conn_db, out reader,
                " Select nzp_serv, service, service_small, service_name, ed_izmer, type_lgot, nzp_frm, ordering " +
                " From " + tab_services +
                " Where 1 = 1 " + where +
                "   and nzp_serv > 1 " +
                " Order by service ", true).result)
            {
                conn_db.Close();
                return null;
            }
#endif
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
                    if (reader["type_lgot"] != DBNull.Value) zap.type_lgot = Convert.ToInt32(reader["type_lgot"]);
                    if (reader["nzp_frm"] != DBNull.Value) zap.nzp_frm = Convert.ToInt32(reader["nzp_frm"]);
                    if (reader["ordering"] != DBNull.Value) zap.ordering = Convert.ToInt32(reader["ordering"]);

                    spis.ServiceList.Add(zap);
                    if (finder.rows > 0 && i >= finder.skip + finder.rows) break;
                }
                reader.Close();


#if PG
                object count = ExecScalar(conn_db,
                              " Select count(*) From " + tab_services +
                              " Where 1 = 1 " + where, out ret, true);
#else
      object count = ExecScalar(conn_db,
                    " Select count(*) From " + tab_services +
                    " Where 1 = 1 " + where, out ret, true);
#endif
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

        //----------------------------------------------------------------------
        public void ServiceLoadInWeb(List<_Service> ls, string table, out Returns ret)
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
#if PG
            IDbCommand cmd = DBManager.newDbCommand(
                       " Insert into __" + table +
                       "  ( nzp_serv, service, service_small, service_name, ed_izmer, type_lgot, nzp_frm, ordering ) " +
                       " Values (?,?,?,?,?,?,?,?) "
                       , conn_web);
#else
     IDbCommand cmd = DBManager.newDbCommand(
                " Insert into __" + table +
                "  ( nzp_serv, service, service_small, service_name, ed_izmer, type_lgot, nzp_frm, ordering ) " +
                " Values (?,?,?,?,?,?,?,?) "
                , conn_web);
#endif

            DBManager.addDbCommandParameter(cmd, "nzp_serv", DbType.Int32);
            DBManager.addDbCommandParameter(cmd, "service", DbType.String);
            DBManager.addDbCommandParameter(cmd, "service_small", DbType.String);
            DBManager.addDbCommandParameter(cmd, "service_name", DbType.String);
            DBManager.addDbCommandParameter(cmd, "ed_izmer", DbType.String);
            DBManager.addDbCommandParameter(cmd, "type_lgot", DbType.Int32);
            DBManager.addDbCommandParameter(cmd, "nzp_frm", DbType.Int32);
            DBManager.addDbCommandParameter(cmd, "ordering", DbType.Int32);

            try
            {
                foreach (_Service p in ls)
                {
                    (cmd.Parameters[0] as IDbDataParameter).Value = p.nzp_serv;
                    (cmd.Parameters[1] as IDbDataParameter).Value = p.service;
                    (cmd.Parameters[2] as IDbDataParameter).Value = p.service_small;
                    (cmd.Parameters[3] as IDbDataParameter).Value = p.service_name;
                    (cmd.Parameters[4] as IDbDataParameter).Value = p.ed_izmer;
                    (cmd.Parameters[5] as IDbDataParameter).Value = p.type_lgot;
                    (cmd.Parameters[6] as IDbDataParameter).Value = p.nzp_frm;
                    (cmd.Parameters[7] as IDbDataParameter).Value = p.ordering;

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
        public List<_Service> ServiceAvailableForRole(Role finder, out Returns ret)
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

            string table = "services";
            if (finder.nzp_server > 0)
                table += "_" + finder.nzp_server;

            if (!TableInWebCashe(conn_web, table))
            {
                ret = new Returns(false, "Справочник услуг не загружен", -1);
                return null;
            }

            string sw = "";
            if (finder.RolesVal != null)
                if (finder.RolesVal.Count > 0)
                {
                    foreach (_RolesVal role in finder.RolesVal)
                    {
                        if (role.tip == Constants.role_sql & role.kod == Constants.role_sql_serv)
                            sw += " and nzp_serv in (" + role.val + ")";
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

            string where = 
                " Where s.nzp_serv not in ( Select kod From roleskey r Where r.nzp_role = " + finder.nzp_role +
                "   and r.tip = " + Constants.role_sql_serv +
#if PG
                "   and r.tip||CAST(r.kod as text)||r.nzp_role||'-'||r.nzp_rlsv||'roles' = r.sign) " + sw;
#else
                "   and r.tip||r.kod||r.nzp_role||'-'||r.nzp_rlsv||'roles' = decrypt_char(r.sign)) " + sw;
#endif


            //выбрать список
            string skip = "";
            //if (finder.skip > 0) skip = " skip " + finder.skip.ToString();

            IDataReader reader;
#if PG
            ret = ExecRead(conn_web, out reader,
                " Select nzp_serv, service " +
                " From " + table + " s " +
                where +
                " Order by service " + skip, true);
#else
            ret = ExecRead(conn_web, out reader,
                " Select " + skip + " nzp_serv, service " +
                " From " + table + " s " +
                where +
                " Order by service ", true);
#endif
            if (!ret.result)
            {
                conn_web.Close();
                return null;
            }

            List<_Service> spis = new List<_Service>();
            try
            {
                int i = 0;
                while (reader.Read())
                {
                    i++;

                    _Service zap = new _Service();

                    if (reader["nzp_serv"] != DBNull.Value) zap.nzp_serv = Convert.ToInt32(reader["nzp_serv"]);
                    if (reader["service"] != DBNull.Value) zap.service = Convert.ToString(reader["service"]).Trim();

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

                MonitorLog.WriteLog("Ошибка заполнения справочника услуг " + err, MonitorLog.typelog.Error, 20, 201, true);

                return null;
            }
        }
        
        //----------------------------------------------------------------------
        public void CreateWebSupplier(IDbConnection conn_web, string table, out Returns ret)
        //----------------------------------------------------------------------
        {
            if (TableInWebCashe(conn_web, table))
            {
                ExecSQL(conn_web, " Drop table " + table, false);
            }

            //создать таблицу webdata:supplier
#if PG
            ret = ExecSQL(conn_web,
                " CREATE TABLE " + table +
                " ( nzp_supp SERIAL NOT NULL, " +
                  " name_supp CHAR(100), " +
                  " adres_supp CHAR(100), " +
                  " phone_supp CHAR(20), " +
                  " geton_plat CHAR(2), " +
                  " have_proc INTEGER, " +
                  " kod_supp CHAR(20) )", true);
#else
            ret = ExecSQL(conn_web,
                " CREATE TABLE " + table +
                " ( nzp_supp SERIAL NOT NULL, " +
                  " name_supp CHAR(100), " +
                  " adres_supp CHAR(100), " +
                  " phone_supp CHAR(20), " +
                  " geton_plat CHAR(2), " +
                  " have_proc INTEGER, " +
                  " kod_supp CHAR(20) )", true);
#endif

            if (!ret.result)
            {
                return;
            }

            uint tabid = TableInWebCasheID(conn_web, table);
            string ix = "ix" + tabid + "_" + table;

            ret = ExecSQL(conn_web, " CREATE UNIQUE INDEX " + ix + " ON " + table + " (nzp_supp)", true);
        }

        //----------------------------------------------------------------------
        public void SupplierLoadInWeb(List<_Supplier> ls, string table, out Returns ret)
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
#if PG
            IDbCommand cmd = DBManager.newDbCommand(
                         " Insert into __" + table +
                         "  ( nzp_supp, name_supp, adres_supp, phone_supp, geton_plat, have_proc, kod_supp ) " +
                         " Values (?,?,?,?,?,?,?) "
                         , conn_web);
#else
   IDbCommand cmd = DBManager.newDbCommand(
                " Insert into __" + table +
                "  ( nzp_supp, name_supp, adres_supp, phone_supp, geton_plat, have_proc, kod_supp ) " +
                " Values (?,?,?,?,?,?,?) "
                , conn_web);
#endif

            DBManager.addDbCommandParameter(cmd, "nzp_supp", DbType.Int32);
            DBManager.addDbCommandParameter(cmd, "name_supp", DbType.String);
            DBManager.addDbCommandParameter(cmd, "adres_supp", DbType.String);
            DBManager.addDbCommandParameter(cmd, "phone_supp", DbType.String);
            DBManager.addDbCommandParameter(cmd, "geton_plat", DbType.String);
            DBManager.addDbCommandParameter(cmd, "have_proc", DbType.Int32);
            DBManager.addDbCommandParameter(cmd, "kod_supp", DbType.String);

            try
            {
                foreach (_Supplier p in ls)
                {
                    (cmd.Parameters[0] as IDbDataParameter).Value = p.nzp_supp;
                    (cmd.Parameters[1] as IDbDataParameter).Value = p.name_supp;
                    (cmd.Parameters[2] as IDbDataParameter).Value = p.adres_supp;
                    (cmd.Parameters[3] as IDbDataParameter).Value = p.phone_supp;
                    (cmd.Parameters[4] as IDbDataParameter).Value = p.geton_plat;
                    (cmd.Parameters[5] as IDbDataParameter).Value = p.have_proc;
                    (cmd.Parameters[6] as IDbDataParameter).Value = p.kod_supp;

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
        public List<_Supplier> SupplierLoad_WebData(Finder finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();

            List<_Supplier> spis = new List<_Supplier>(); //

            IDbConnection conn_db = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return null;

            string where = "";
            if (finder.RolesVal != null)
                foreach (_RolesVal role in finder.RolesVal)
                {
                    if (role.tip == Constants.role_sql & role.kod == Constants.role_sql_supp)
                        where += " and nzp_supp in (" + role.val + ")";
                }

            if (finder.dopFind != null && finder.dopFind.Count > 0)
            {
                string str = finder.dopFind[0];
                if (!str.Contains("FiltrOnDistrib"))
                {
                    where += " and upper(name_supp) like '%" + str.ToUpper().Replace("'", "''").Replace("*", "%") + "%'";
                }
            }

            string tab_supplier = "supplier";
            if (finder.nzp_server > 0)
                tab_supplier += "_" + finder.nzp_server;

            //выбрать список
            IDataReader reader;
#if PG
            if (!ExecRead(conn_db, out reader,
                " Select nzp_supp, name_supp, nzp_payer, adres_supp, phone_supp, geton_plat, have_proc, kod_supp " +
                " From " + tab_supplier +
                " Where 1 = 1 " + where +
                " Order by name_supp ", true).result)
            {
                conn_db.Close();
                return null;
            }
#else
            if (!ExecRead(conn_db, out reader,
                " Select nzp_supp, name_supp, nzp_payer, adres_supp, phone_supp, geton_plat, have_proc, kod_supp " +
                " From " + tab_supplier +
                " Where 1 = 1 " + where +
                " Order by name_supp ", true).result)
            {
                conn_db.Close();
                return null;
            }
#endif
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
                reader.Close();

                ret.tag = i;

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
                if (Constants.Viewerror)
                    err = " \n " + ex.Message;
                else
                    err = "";

                MonitorLog.WriteLog("Ошибка заполнения справочника поставщиков " + err, MonitorLog.typelog.Error, 20, 201, true);

                return null;
            }
        }

        //----------------------------------------------------------------------
        public List<_Supplier> SupplierAvailableForRole(Role finder, out Returns ret)
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

            string table = "supplier";
            if (finder.nzp_server > 0)
                table += "_" + finder.nzp_server;

            if (!TableInWebCashe(conn_web, table))
            {
                ret = new Returns(false, "Справочник поставщиков не загружен", -1);
                return null;
            }

            string sw = "";
            if (finder.RolesVal != null)
                if (finder.RolesVal.Count > 0)
                {
                    foreach (_RolesVal role in finder.RolesVal)
                    {
                        if (role.tip == Constants.role_sql & role.kod == Constants.role_sql_supp)
                            sw += " and nzp_supp in (" + role.val + ")";
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

            string where =
                " Where s.nzp_supp not in ( Select kod From roleskey r Where r.nzp_role = " + finder.nzp_role +
                "   and r.tip = " + Constants.role_sql_supp +
#if PG
                "   and r.tip||CAST(r.kod as text)||r.nzp_role||'-'||r.nzp_rlsv||'roles' = r.sign) " + sw;
#else
                "   and r.tip||r.kod||r.nzp_role||'-'||r.nzp_rlsv||'roles' = decrypt_char(r.sign)) " + sw;
#endif


            //выбрать список
            string skip = "";
            //if (finder.skip > 0) skip = " skip " + finder.skip.ToString();

            IDataReader reader;
            ret = ExecRead(conn_web, out reader,
#if PG
                " Select nzp_supp, name_supp " +
                " From " + table + " s " + where +
                " Order by name_supp " + skip, true);
#else
                " Select " + skip + " nzp_supp, name_supp " +
                " From " + table + " s " + where +
                " Order by name_supp ", true);
#endif
            if (!ret.result)
            {
                conn_web.Close();
                return null;
            }

            List<_Supplier> spis = new List<_Supplier>();
            try
            {
                int i = 0;
                while (reader.Read())
                {
                    i++;

                    _Supplier zap = new _Supplier();

                    if (reader["nzp_supp"] != DBNull.Value) zap.nzp_supp = Convert.ToInt64(reader["nzp_supp"]);
                    if (reader["name_supp"] != DBNull.Value) zap.name_supp = Convert.ToString(reader["name_supp"]).Trim();

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

                MonitorLog.WriteLog("Ошибка заполнения справочника поставщиков " + err, MonitorLog.typelog.Error, 20, 201, true);

                return null;
            }
        }

        //----------------------------------------------------------------------
        public void CreateWebPayer(IDbConnection conn_web, string table, out Returns ret)
        //----------------------------------------------------------------------
        {
            if (TableInWebCashe(conn_web, table))
            {
                ExecSQL(conn_web, " Drop table " + table, false);
            }

            //создать таблицу webdata:s_payer
#if PG
            ret = ExecSQL(conn_web,
                 " CREATE TABLE " + table +
                 " ( nzp_payer SERIAL NOT NULL, " +
                   " payer CHAR(200), " +
                   " npayer CHAR(200), " +
                   " nzp_supp INTEGER, " +
                   " is_erc INTEGER, " +
                   " is_bank INTEGER " +
                   " )", true);
#else
           ret = ExecSQL(conn_web,
                " CREATE TABLE " + table +
                " ( nzp_payer SERIAL NOT NULL, " +
                  " payer CHAR(200), " +
                  " npayer CHAR(200), " +
                  " nzp_supp INTEGER, " +
                  " is_erc INTEGER, " +
                  " is_bank INTEGER " +
                  " )", true);
#endif

            if (!ret.result)
            {
                return;
            }

            uint tabid = TableInWebCasheID(conn_web, table);
            string ix = "ix" + tabid + "_" + table;
#if PG
            ret = ExecSQL(conn_web, " CREATE UNIQUE INDEX " + ix + " ON " + table + " (nzp_payer)", true);
            ret = ExecSQL(conn_web, " CREATE INDEX " + ix + "_2 ON " + table + " (is_bank)", true);
            ret = ExecSQL(conn_web, " CREATE INDEX " + ix + "_3 ON " + table + " (nzp_supp)", true);
#else
            ret = ExecSQL(conn_web, " CREATE UNIQUE INDEX " + ix + " ON " + table + " (nzp_payer)", true);
            ret = ExecSQL(conn_web, " CREATE INDEX " + ix + "_2 ON " + table + " (is_bank)", true);
            ret = ExecSQL(conn_web, " CREATE INDEX " + ix + "_3 ON " + table + " (nzp_supp)", true);
#endif
        }


        public void SetMainPageTitle(int i,Returns ret) {
            IDbConnection conn_db = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return;

        }

        //----------------------------------------------------------------------
        public void PayerLoadInWeb(List<Payer> ls, string table, out Returns ret)
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
#if PG
            IDbCommand cmd = DBManager.newDbCommand(
                       " Insert into __" + table +
                       "  ( nzp_payer, payer, npayer, nzp_supp, is_erc, is_bank ) " +
                       " Values (?,?,?,?,?,?) "
                       , conn_web);
#else
     IDbCommand cmd = DBManager.newDbCommand(
                " Insert into __" + table +
                "  ( nzp_payer, payer, npayer, nzp_supp, is_erc, is_bank ) " +
                " Values (?,?,?,?,?,?) "
                , conn_web);
#endif

            DBManager.addDbCommandParameter(cmd, "nzp_payer", DbType.Int32);
            DBManager.addDbCommandParameter(cmd, "payer", DbType.String);
            DBManager.addDbCommandParameter(cmd, "npayer", DbType.String);
            DBManager.addDbCommandParameter(cmd, "nzp_supp", DbType.Int32);
            DBManager.addDbCommandParameter(cmd, "is_erc", DbType.Int32);
            DBManager.addDbCommandParameter(cmd, "is_bank", DbType.Int32);

            try
            {
                foreach (Payer p in ls)
                {
                    (cmd.Parameters[0] as IDbDataParameter).Value = p.nzp_payer;
                    (cmd.Parameters[1] as IDbDataParameter).Value = p.payer;
                    (cmd.Parameters[2] as IDbDataParameter).Value = p.npayer;
                    (cmd.Parameters[3] as IDbDataParameter).Value = p.nzp_supp;
                    (cmd.Parameters[4] as IDbDataParameter).Value = p.is_erc;
                    (cmd.Parameters[5] as IDbDataParameter).Value = p.is_bank;

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

        /*
        //----------------------------------------------------------------------
        public List<Payer> LoadPayer(Payer finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            return LoadPayer(finder, false, out ret);
        }
        */

        

        //----------------------------------------------------------------------
        public static string FiltrOnDistrib(string pole, List<string> dopFind)
        //----------------------------------------------------------------------
        {
            return FiltrOnDistrib(pole, pole, dopFind);
        }
        //----------------------------------------------------------------------
        public static string FiltrOnDistrib(string pole, string pole2, List<string> dopFind)
        //----------------------------------------------------------------------
        {
            if (dopFind != null)
            {
                foreach (string str in dopFind)
                {
                    if (str.Contains("FiltrOnDistrib"))
                    {
                        string[] result = str.Split(new string[] { "|" }, StringSplitOptions.None);
                        DateTime d = new DateTime();
                        try
                        {
                            d = Convert.ToDateTime(result[1].Trim());
                        }
                        catch
                        {
                            d = Points.DateOper;
                        }

                        string ol_srv = "";
#if PG
                        string fn_distrib = Points.Pref + "_fin_" + (d.Year - 2000).ToString("00") + ol_srv + ".fn_distrib_dom_" + d.Month.ToString("00");
#else
                        string fn_distrib = Points.Pref + "_fin_" + (d.Year - 2000).ToString("00") + ol_srv + ":fn_distrib_dom_" + d.Month.ToString("00");
#endif
                        return " and " + pole + " in ( Select " + pole2 + " From " + fn_distrib + " )";
                    }
                }
            }

            return "";
        }

        //----------------------------------------------------------------------
        public List<Payer> PayerBankLoad_WebData(Payer finder, bool is_bank, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();

            List<Payer> spis = new List<Payer>(); //

            IDbConnection conn_db = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return null;

            string where = "";
            if (finder.RolesVal != null)
            {
                foreach (_RolesVal role in finder.RolesVal)
                {
                    if (role.tip == Constants.role_sql && role.kod == Constants.role_sql_payer)
                        where += " and p.nzp_payer in (" + role.val + ")";
                    else
                        if (role.tip == Constants.role_sql && role.kod == Constants.role_sql_supp)
                            where += " and p.nzp_supp in (" + role.val + ")";
                }
            }

            if (finder.payer != "")
                where += " and upper(p.payer) like '%" + finder.payer.ToUpper().Replace("'", "''").Replace("*", "%") + "%'";
            if (finder.nzp_supp > 0)
                where += " and p.nzp_supp = " + finder.nzp_supp;

            string tab_payer = "s_payer";
            if (finder.nzp_server > 0)
                tab_payer += "_" + finder.nzp_server;


            //фильтровать справочники по fn_distrib
            //string filtrOnDistrib = FiltrOnDistrib("nzp_payer", finder.dopFind);
            //where += filtrOnDistrib;

            //выбрать список
            IDataReader reader;
#if PG
            if (!ExecRead(conn_db, out reader,
                   " Select nzp_payer, payer, npayer, nzp_supp, is_erc, is_bank " +
                   " From " + tab_payer + " p " +
                   " Where 1 = 1 " + where +
                   " Order by payer ", true).result)
            {
                conn_db.Close();
                return null;
            }
#else
         if (!ExecRead(conn_db, out reader,
                " Select nzp_payer, payer, npayer, nzp_supp, is_erc, is_bank " +
                " From " + tab_payer + " p " +
                " Where 1 = 1 " + where +
                " Order by payer ", true).result)
            {
                conn_db.Close();
                return null;
            }
#endif
            try
            {
                int i = 0;
                while (reader.Read())
                {
                    i++;
                    if (i <= finder.skip) continue;
                    Payer zap = new Payer();
                    //zap.num = i;

                    if (reader["nzp_payer"] != DBNull.Value) zap.nzp_payer = Convert.ToInt32(reader["nzp_payer"]);
                    if (reader["payer"]     != DBNull.Value) zap.payer = Convert.ToString(reader["payer"]).Trim();
                    if (reader["npayer"]    != DBNull.Value) zap.npayer = Convert.ToString(reader["npayer"]).Trim();
                    if (reader["nzp_supp"]  != DBNull.Value) zap.nzp_supp = Convert.ToInt32(reader["nzp_supp"]);
                    if (reader["is_erc"]    != DBNull.Value) zap.is_erc = Convert.ToInt32(reader["is_erc"]);
                    if (reader["is_bank"]   != DBNull.Value) zap.is_bank = Convert.ToInt32(reader["is_bank"]);

                    spis.Add(zap);
                    if (finder.rows > 0 && i >= finder.skip + finder.rows) break;
                }
                reader.Close();

#if PG
                object count = ExecScalar(conn_db,
                    " Select count(*) From " + tab_payer +
                    " Where 1 = 1 " + where, out ret, true);
#else
                object count = ExecScalar(conn_db,
                    " Select count(*) From " + tab_payer +
                    " Where 1 = 1 " + where, out ret, true);
#endif
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
                return spis;
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

                MonitorLog.WriteLog("Ошибка заполнения справочника подрядчиков " + err, MonitorLog.typelog.Error, 20, 201, true);

                return null;
            }
        }

        /*
        //----------------------------------------------------------------------
        public List<Bank> LoadBank(Bank finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();

            List<Bank> spis = new List<Bank>();

            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return null;

            string sw = "";
            if (finder.RolesVal != null)
                foreach (_RolesVal role in finder.RolesVal)
                {
                    if (role.tip == Constants.role_sql & role.kod == Constants.role_sql_payer)
                        sw = " and p.nzp_payer in (" + role.val + ")";
                }

            //выбрать список
            string sql = "";
            string where = " and exists ( Select * From " + Points.Pref + "_kernel:s_bank Where nzp_payer = p.nzp_payer ) ";
            if (finder.bank != "")
                where += " and upper(payer) like '%" + finder.bank.ToUpper().Replace("'", "''").Replace("*", "%") + "%'";
            sql = " Select payer, nzp_payer " +
                  " From " + Points.Pref + "_kernel:s_payer p " +
                  " Where 1 = 1 " + sw + where +
                  " Order by payer ";

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
                    Bank zap = new Bank();

                    if (reader["nzp_payer"] != DBNull.Value) zap.nzp_bank = (int)reader["nzp_payer"];
                    if (reader["payer"] != DBNull.Value) zap.bank = Convert.ToString(reader["payer"]).Trim();

                    spis.Add(zap);
                    if (finder.rows > 0 && i >= finder.skip + finder.rows) break;
                }
                reader.Close();
                sql = " Select count(*) From " + Points.Pref + "_kernel:s_payer p Where 1 = 1 " + sw + where;
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

                MonitorLog.WriteLog("Ошибка заполнения списка банков(подрядчиков) " + err, MonitorLog.typelog.Error, 20, 201, true);

                return null;
            }
        }
        */

        //----------------------------------------------------------------------
        public void CreateWebPoint(IDbConnection conn_web, string table, out Returns ret)
        //----------------------------------------------------------------------
        {
            if (TableInWebCashe(conn_web, table))
            {
                ExecSQL(conn_web, " Drop table " + table, false);
            }

            //создать таблицу webdata:s_point
#if PG
            ret = ExecSQL(conn_web,
                " CREATE TABLE " + table +
                " ( nzp_wp SERIAL NOT NULL, " +
                  " nzp_graj INTEGER, " +
                  " n INTEGER, " +
                  " point CHAR(100), " +
                  " bd_kernel CHAR(100), " +
                  " bd_old CHAR(100), " +
                  " flag INTEGER )", true);
#else
            ret = ExecSQL(conn_web,
                " CREATE TABLE " + table +
                " ( nzp_wp SERIAL NOT NULL, " +
                  " nzp_graj INTEGER, " +
                  " n INTEGER, " +
                  " point CHAR(100), " +
                  " bd_kernel CHAR(100), " +
                  " bd_old CHAR(100), " +
                  " flag INTEGER )", true);
#endif

            if (!ret.result)
            {
                conn_web.Close();
                return;
            }

            uint tabid = TableInWebCasheID(conn_web, table);
            string ix = "ix" + tabid + "_" + table;

            ret = ExecSQL(conn_web, " CREATE UNIQUE INDEX " + ix + " ON " + table + " (nzp_wp)", true);
        }

        //----------------------------------------------------------------------
        public List<_Point> PointLoad_WebData(Finder finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();

            List<_Point> PointList = new List<_Point>();

            IDbConnection conn_db = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return null;

            //выбрать список
#if PG
            string sql =
                        " Select nzp_wp, nzp_graj, n, point, bd_kernel, bd_old, flag " +
                        " From s_point Where 1 = 1 " +
                        " Order by 1 ";
#else
    string sql =
                " Select nzp_wp, nzp_graj, n, point, bd_kernel, bd_old, flag " +
                " From s_point Where 1 = 1 "+
                " Order by 1 ";
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
                    _Point zap = new _Point();


                    //nzp_wp, nzp_graj, n, point, bd_kernel, bd_old, flag+
                    if (reader["nzp_wp"]    != DBNull.Value) zap.nzp_wp = Convert.ToInt32(reader["nzp_wp"]);
                    if (reader["nzp_graj"]  != DBNull.Value) zap.nzp_graj = Convert.ToInt32(reader["nzp_graj"]);
                    if (reader["n"]         != DBNull.Value) zap.n = Convert.ToInt32(reader["n"]);

                    if (reader["point"]     != DBNull.Value) zap.point = Convert.ToString(reader["point"]).Trim();
                    if (reader["bd_kernel"] != DBNull.Value) zap.pref = Convert.ToString(reader["bd_kernel"]).Trim();
                    if (reader["bd_old"]    != DBNull.Value) zap.ol_server = Convert.ToString(reader["bd_old"]).Trim();

                    if (reader["flag"]      != DBNull.Value) zap.flag = Convert.ToInt32(reader["flag"]);

                    PointList.Add(zap);
                    if (finder.rows > 0 && i >= finder.skip + finder.rows) break;
                }

                reader.Close();

#if PG
                sql = " Select count(*) From s_point Where 1 = 1 ";
#else
    sql = " Select count(*) From s_point Where 1 = 1 ";
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
                return PointList;
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

                MonitorLog.WriteLog("Ошибка заполнения справочника s_point " + err, MonitorLog.typelog.Error, 20, 201, true);

                return null;
            }
        }

        //----------------------------------------------------------------------
        public List<int> PointByAreaLoad(Dom finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();

            List<int> Spis = new List<int>();

            Spis.Clear();

            IDbConnection conn_db = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return null;

            //Взять информацию из аналитических таблиц
            string anl_dom = "anl" + Points.CalcMonth.year_ + "_dom";
            if (finder.nzp_server > 0)
                anl_dom += "_" + finder.nzp_server;

            string whereString = "";
            if (finder.area.Trim() != "")
#if PG
                whereString += " and upper(area) like upper('%" + finder.area + "%')";
#else
                whereString += " and upper(area) matches upper('*" + finder.area + "*')";
#endif
            if (finder.list_nzp_area != null && finder.list_nzp_area.Count > 0)
            {
                string str = "";
                for (int i = 0; i < finder.list_nzp_area.Count; i++)
                {
                    if (i == 0) str += finder.list_nzp_area[i];
                    else str += ", " + finder.list_nzp_area[i];
                }
                if (str != "") whereString += " and nzp_area in (" + str + ")";
            }
            else if (finder.nzp_area > 0)
                whereString += " and nzp_area =" + finder.nzp_area;
            if (finder.pref != "") 
                whereString += " and pref = " + Utils.EStrNull(finder.pref);

            string first = "";
#if PG
            //if (finder.rows > 0) first = " limit " + finder.rows.ToString();
            if (finder.rows > 0) first = " limit " + finder.rows.ToString();

#else
            if (finder.rows > 0) first = " first " + finder.rows.ToString();
#endif


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
                " Select distinct nzp_wp " +
                " From " + anl_dom +
                " Where 1 = 1 " + whereString +
                " Order by 1 " + first;
#else
            string sql =
                " Select " + first + " unique nzp_wp " +
                " From " + anl_dom +
                " Where 1 = 1 " + whereString +
                " Order by 1 ";
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

                    if (reader["nzp_wp"] != DBNull.Value)
                        Spis.Add((int)reader["nzp_wp"]);

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

                MonitorLog.WriteLog("Ошибка заполнения справочник банков данных " + err, MonitorLog.typelog.Error, 20, 201, true);

                return null;
            }

            conn_db.Close();
            return Spis;
        }

        //----------------------------------------------------------------------
        public void PointLoadInWeb(List<_Point> ls, string table, out Returns ret)
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
         #if PG
   IDbCommand cmd = DBManager.newDbCommand(
                " Insert into __" + table +
                "  ( nzp_wp, nzp_graj, n, point, bd_kernel, bd_old, flag ) " +
                " Values (?,?,?,?,?,?,?) "
                , conn_web);
#else
   IDbCommand cmd = DBManager.newDbCommand(
                " Insert into __" + table +
                "  ( nzp_wp, nzp_graj, n, point, bd_kernel, bd_old, flag ) " +
                " Values (?,?,?,?,?,?,?) "
                , conn_web);
#endif

            DBManager.addDbCommandParameter(cmd, "nzp_wp", DbType.Int32);
            DBManager.addDbCommandParameter(cmd, "nzp_graj", DbType.Int32);
            DBManager.addDbCommandParameter(cmd, "n", DbType.Int32);
            DBManager.addDbCommandParameter(cmd, "point", DbType.String);
            DBManager.addDbCommandParameter(cmd, "bd_kernel", DbType.String);
            DBManager.addDbCommandParameter(cmd, "bd_old", DbType.String);
            DBManager.addDbCommandParameter(cmd, "flag", DbType.Int32);

            try
            {
                foreach (_Point p in ls)
                {
                    (cmd.Parameters[0] as IDbDataParameter).Value = p.nzp_wp;
                    (cmd.Parameters[1] as IDbDataParameter).Value = p.nzp_graj;
                    (cmd.Parameters[2] as IDbDataParameter).Value = p.n;
                    (cmd.Parameters[3] as IDbDataParameter).Value = p.point;
                    (cmd.Parameters[4] as IDbDataParameter).Value = p.pref;
                    (cmd.Parameters[5] as IDbDataParameter).Value = p.ol_server;
                    (cmd.Parameters[6] as IDbDataParameter).Value = p.flag;

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
        public List<_Point> PointAvailableForRole(Role finder, out Returns ret)
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

            string table = "s_point";
            if (finder.nzp_server > 0)
                table += "_" + finder.nzp_server;

            if (!TableInWebCashe(conn_web, table))
            {
                ret = new Returns(false, "Справочник банков данных не загружен", -1);
                return null;
            }

            string sw = "";
            if (finder.RolesVal != null)
                if (finder.RolesVal.Count > 0)
                {
                    foreach (_RolesVal role in finder.RolesVal)
                    {
                        if (role.tip == Constants.role_sql & role.kod == Constants.role_sql_wp)
                            sw += " and nzp_wp in (" + role.val + ")";
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

            string where = 
                " Where p.nzp_wp not in ( Select kod From roleskey r Where r.nzp_role = " + finder.nzp_role +
                "   and r.tip = " + Constants.role_sql_wp +
#if PG
                "   and r.tip||CAST(r.kod as text)||r.nzp_role||'-'||r.nzp_rlsv||'roles' = r.sign) " + sw;
#else
                "   and r.tip||r.kod||r.nzp_role||'-'||r.nzp_rlsv||'roles' = decrypt_char(r.sign)) " + sw;
#endif

            //выбрать список
            string skip = "";
            //if (finder.skip > 0) skip = " skip " + finder.skip.ToString();

            IDataReader reader;
            ret = ExecRead(conn_web, out reader,
#if PG
                " Select nzp_wp, point " +
                " From " + table + " p " +
                where +
                " Order by point " + skip, true);
#else
                " Select " + skip + " nzp_wp, point " +
                " From " + table + " p " +
                where +
                " Order by point ", true);
#endif
            if (!ret.result)
            {
                conn_web.Close();
                return null;
            }

            List<_Point> spis = new List<_Point>();
            try
            {
                int i = 0;
                while (reader.Read())
                {
                    i++;

                    _Point zap = new _Point();

                    if (reader["nzp_wp"] != DBNull.Value) zap.nzp_wp = (int)reader["nzp_wp"];
                    if (reader["point"] != DBNull.Value) zap.point = Convert.ToString(reader["point"]).Trim();

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

                MonitorLog.WriteLog("Ошибка заполнения справочника банков данных " + err, MonitorLog.typelog.Error, 20, 201, true);

                return null;
            }
        }

        //----------------------------------------------------------------------
        public List<_Prm> PrmAvailableForRole(Role finder, out Returns ret)
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

#if PG
            if (!TempTableInWebCashe(conn_web, "public.prm_name"))
#else
            if (!TableInWebCashe(conn_web, "prm_name"))
#endif
            {
                ret = new Returns(false, "Справочник параметров не загружен", -1);
                return null;
            }

            string sw = "";
            if (finder.RolesVal != null)
                if (finder.RolesVal.Count > 0)
                {
                    foreach (_RolesVal role in finder.RolesVal)
                    {
                        if (role.tip == Constants.role_sql & role.kod == Constants.role_sql_prm)
                            sw += " and nzp_prm in (" + role.val + ")";
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

            string where = " Where p.nzp_prm not in (select kod from roleskey r where r.nzp_role = " + finder.nzp_role +
                " and r.tip = " + Constants.role_sql_prm +
#if PG
                " and r.tip||CAST(r.kod as text)||r.nzp_role||'-'||r.nzp_rlsv||'roles' = decrypt_char(r.sign)) " + sw;
#else
                " and r.tip||r.kod||r.nzp_role||'-'||r.nzp_rlsv||'roles' = decrypt_char(r.sign)) " + sw;
#endif

            //выбрать список
            string skip = "";
            //if (finder.skip > 0) skip = " skip " + finder.skip.ToString();

            IDataReader reader;
            ret = ExecRead(conn_web, out reader,
#if PG
                 " Select nzp_prm, name_prm From prm_name p " +
                where +
                " Order by name_prm " + skip, true);
#else
                " Select " + skip + " nzp_prm, name_prm From prm_name p " +
                where +
                " Order by name_prm ", true);
#endif
            if (!ret.result)
            {
                conn_web.Close();
                return null;
            }

            List<_Prm> spis = new List<_Prm>();
            try
            {
                int i = 0;
                while (reader.Read())
                {
                    i++;

                    _Prm zap = new _Prm();

                    if (reader["nzp_prm"] != DBNull.Value) zap.nzp_prm = Convert.ToInt32(reader["nzp_prm"]);
                    if (reader["name_prm"] != DBNull.Value) zap.name_prm = Convert.ToString(reader["name_prm"]).Trim();

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

                MonitorLog.WriteLog("Ошибка заполнения справочника параметров " + err, MonitorLog.typelog.Error, 20, 201, true);

                return null;
            }
        }

        //----------------------------------------------------------------------
        public void StartApp() //загрузить все ссылки меню из базы
        //----------------------------------------------------------------------
        {
            IDbConnection connectionID = GetConnection(Constants.cons_Webdata);
            if (!OpenDb(connectionID, true).result) return;
#if PG
#else
            if (!ExecSQL(connectionID,
                  " set encryption password '" + BasePwd + "'"
                  , true).result)
            {
                connectionID.Close();
                return;
            }
#endif

            //SysPrtData
            IDataReader reader; //, readerImg;
            
#if PG
            string sql = " Select num_prtd, val_prtd " +
                " From sysprtdata " +
                " Order by 1 ";
#else
            string sql = " Select num_prtd, decrypt_char(val_prtd) as val_prtd " +                
                " From sysprtdata " +
                " Order by 1 ";
#endif
            if (!ExecRead(connectionID, out reader, sql, true).result)
            {
                connectionID.Close();
                return;
            }
            try
            {
                while (reader.Read())
                {
                    _SysPort zap = new _SysPort();
                    zap.num_prtd = (reader["num_prtd"] != DBNull.Value) ? Convert.ToInt32(reader["num_prtd"]) : 0;
#if PG
                    zap.val_prtd = reader["val_prtd"] != DBNull.Value ? Convert.ToString(reader["val_prtd"]).Trim() : "-";
#else
                    zap.val_prtd = "-";
                    if (reader["val_prtd"] != DBNull.Value)
                    {
                        string s = Convert.ToString(reader["val_prtd"]);
                        s = s.Trim();
                        if (s.Length > 4)
                            zap.val_prtd = s.Substring(4, s.Length - 4);
                    }
#endif
                    Constants.SysPort.Add(zap);
                }
                reader.Close();
            }
            catch (Exception ex)
            {
                //System.Diagnostics.Trace.WriteLine("Exception: " + ex.Message);
                string err;
                if (Constants.Viewerror)
                    err = ex.Message;
                else
                    err = "";

                MonitorLog.WriteLog("Ошибка заполнения SysPort " + err, MonitorLog.typelog.Error, 20, 101, true);
                reader.Close();
                connectionID.Close();
                return;
            }

            //ARMS
            //reader, readerImg;
            string select = ", '' as url";
            string from = "";
            string where = "";
#if PG
            if (TempTableInWebCashe(connectionID, "foreign_systems"))
            {
                select = ", fs.url";
                from = "left outer join foreign_systems fs";
                where = " on fs.nzp_role = s.nzp_role";
            }
            if (!ExecRead(connectionID, out reader,
                " Select s.nzp_role, s.page_url, decrypt_char(s.role) as role, s.sort " + select +
                " From s_roles s " + from +
                where +
                " Order by s.sort", true).result)
            {
                connectionID.Close();
                return;
            }
#else
            if (TempTableInWebCashe(connectionID, "foreign_systems"))
            {
                select = ", fs.url";
                from = ", outer foreign_systems fs";
                where = " Where fs.nzp_role = s.nzp_role";
            }
            if (!ExecRead(connectionID, out reader,
                " Select s.nzp_role, s.page_url, decrypt_char(s.role) as role, s.sort " + select +
                " From s_roles s " + from +
                where +
                " Order by s.sort", true).result)
            {
                connectionID.Close();
                return;
            }
#endif
            try
            {
                while (reader.Read())
                {
                    _Arms zap = new _Arms();
                    zap.nzp_role = (int)reader["nzp_role"];

                    if (reader["page_url"] == DBNull.Value)
                        zap.page_url = 0;
                    else
                        zap.page_url = (int)reader["page_url"];

                    if (reader["role"] == DBNull.Value)
                        zap.role = "-";
                    else
                        zap.role = (string)reader["role"];

                    zap.url = reader["url"] != DBNull.Value ? Convert.ToString(reader["url"]).Trim() : "";

                    Constants.ArmList.Add(zap);
                }
                reader.Close();
            }
            catch (Exception ex)
            {
                //System.Diagnostics.Trace.WriteLine("Exception: " + ex.Message);
                string err;
                if (Constants.Viewerror)
                    err = ex.Message;
                else
                    err = "";

                MonitorLog.WriteLog("Ошибка заполнения ArmList " + err, MonitorLog.typelog.Error, 20, 101, true);
                reader.Close();
                connectionID.Close();
                return;
            }

            //PAGES
            if (!ExecRead(connectionID, out reader,
                " Select nzp_page, group_id, decrypt_char(page_url) as page_url, decrypt_char(page_menu) as page_menu, decrypt_char(page_name) as page_name, decrypt_char(hlp) as page_help " +
                " From pages " +
                " Order by nzp_page", true).result)
            {
                connectionID.Close();
                return;
            }
            try
            {
                while (reader.Read())
                {
                    _Pages zap = new _Pages();
                    zap.nzp_page = (int)reader["nzp_page"];

                    zap.group_id = reader["group_id"] != DBNull.Value ? (int?)reader["group_id"] : null;

                    if (reader["page_url"] == DBNull.Value)
                        zap.page_url = "-";
                    else
                        zap.page_url = (string)reader["page_url"];

                    if (reader["page_menu"] == DBNull.Value)
                        zap.page_menu = "-";
                    else
                        zap.page_menu = (string)reader["page_menu"];

                    if (reader["page_name"] == DBNull.Value)
                        zap.page_name = "-";
                    else
                        zap.page_name = (string)reader["page_name"];

                    if (reader["page_help"] == DBNull.Value)
                        zap.page_help = "-";
                    else
                        zap.page_help = (string)reader["page_help"];

                    Constants.Pages.Add(zap);
                    Constants.DictPages.Add(zap.nzp_page,zap);
                }
                reader.Close();
            }
            catch (Exception ex)
            {
                //System.Diagnostics.Trace.WriteLine("Exception: " + ex.Message);
                string err;
                if (Constants.Viewerror)
                    err = ex.Message;
                else
                    err = "";

                MonitorLog.WriteLog("Ошибка заполнения pages " + err, MonitorLog.typelog.Error, 20, 101, true);
                reader.Close();
                connectionID.Close();
                return;
            }
            if (!ExecRead(connectionID, out reader,
             " Select  up_kod, cur_page, page_url, sort_kod " +
             " From pages_show where up_kod=0 ", true).result)
            {
                connectionID.Close();
                return;
            }
            try
            {
                while (reader.Read())
                {
                    _PageShow zap = new _PageShow();
                    zap.cur_page = (int)reader["cur_page"];

                    if (reader["page_url"] == DBNull.Value)
                        zap.page_url = 0;
                    else
                        zap.page_url = (int)reader["page_url"];

                    int up_kod = 0;
                    if (reader["up_kod"] == DBNull.Value)
                        up_kod = 0;
                    else up_kod = (int)reader["up_kod"];

                    var sort_kod = 0;
                    if (reader["sort_kod"] == DBNull.Value)
                        sort_kod = 1;
                    else sort_kod = (int)reader["sort_kod"];

                    if (up_kod == 0)
                    {
                        _Menu menu = new _Menu();
                        menu.cur_page = zap.cur_page;
                        menu.page_url = zap.page_url;
                        menu.sort_kod = sort_kod;
                        menu.up_kod = 0;
                        Constants.Menu.Add(menu);
                    }
                }
                reader.Close();
            }
            catch (Exception ex)
            {
                //System.Diagnostics.Trace.WriteLine("Exception: " + ex.Message);
                string err;
                if (Constants.Viewerror)
                    err = ex.Message;
                else
                    err = "";

                MonitorLog.WriteLog("Ошибка заполнения pages " + err, MonitorLog.typelog.Error, 20, 101, true);
                reader.Close();
                connectionID.Close();
                return;
            }
            //PAGES_SHOW
            /*
            if (!ExecRead(connectionID, out reader,
                " Select nzp_psh, cur_page, page_url, up_kod, sort_kod" +
                " From pages_show " +
                " Where sort_kod||up_kod||page_url||cur_page||'-'||nzp_psh||'pages_show' = decrypt_char(sign) " +
                " Order by cur_page, up_kod, sort_kod", true).result)
            {
                connectionID.Close();
                return;
            }
            try
            {
                while (reader.Read())
                {
                    _PageShow zap = new _PageShow();
                    zap.cur_page = (int)reader["cur_page"];
                    zap.page_url = (int)reader["page_url"];
                    zap.up_kod = (int)reader["up_kod"];
                    zap.sort_kod = (int)reader["sort_kod"];
                    zap.img_url = " ";

                    string sql = " Select img_url From img_lnk " +
                                 " Where tip = 1 and kod = " + zap.page_url.ToString() +
                                   " and (cur_page = " + zap.cur_page.ToString() + " or cur_page = 0) " +
                                 " Order by kod desc ";
                    if (ExecRead(connectionID, out readerImg, sql, true).result)
                    {
                        if (readerImg.Read())
                        {
                            zap.img_url = Convert.ToString(readerImg["img_url"]);
                        }
                    }


                    Constants.PageShow.Add(zap);
                }
                reader.Close();
            }
            catch (Exception ex)
            {
                string err;
                if (Constants.Viewerror)
                    err = ex.Message;
                else
                    err = "";

                MonitorLog.WriteLog("Ошибка заполнения pages_show " + err, MonitorLog.typelog.Error, 20, 101, true);
                reader.Close();
                connectionID.Close();
                return;
            }
            */

            if (!ExecRead(connectionID, out reader,
#if PG
                " Select a.nzp_psh, a.cur_page, a.page_url, a.up_kod, a.sort_kod, max(b.img_url) as img_url " +
                " From pages_show a left outer join img_lnk b on b.kod = a.page_url " +
                "   and b.tip = 1 " +
                "   and (b.cur_page = a.cur_page or b.cur_page = 0) " +
                " Where CAST(a.sort_kod as text)||CAST(a.up_kod as text)||CAST(a.page_url as text)||CAST(a.cur_page as text)||'-'||CAST(a.nzp_psh as text)||'pages_show' = a.sign " +
                " Group by 1,2,3,4,5 " +
                " Order by a.cur_page, a.up_kod, a.sort_kod", true).result)
#else
                " Select a.nzp_psh, a.cur_page, a.page_url, a.up_kod, a.sort_kod, max(b.img_url) as img_url " +
                " From pages_show a, outer img_lnk b " +
                " Where a.sort_kod||a.up_kod||a.page_url||a.cur_page||'-'||a.nzp_psh||'pages_show' = decrypt_char(a.sign) " +
                "   and b.tip = 1 and b.kod = a.page_url " +
                "   and (b.cur_page = a.cur_page or b.cur_page = 0) " +
                " Group by 1,2,3,4,5 " +
                " Order by a.cur_page, a.up_kod, a.sort_kod", true).result)
#endif
            {
                connectionID.Close();
                return;
            }
            try
            {
                int prev_cur_page = -1;

                while (reader.Read())
                {
                    _PageShow zap = new _PageShow();
                    zap.cur_page = (int)reader["cur_page"];
                    zap.page_url = (int)reader["page_url"];
                    zap.up_kod = (int)reader["up_kod"];
                    zap.sort_kod = (int)reader["sort_kod"];
                    zap.img_url = " ";

                    if (reader["img_url"] != DBNull.Value)
                        zap.img_url = Convert.ToString(reader["img_url"]);

                    /*
                    string sql = " Select img_url From img_lnk " +
                                 " Where tip = 1 and kod = " + zap.page_url.ToString() +
                                   " and (cur_page = " + zap.cur_page.ToString() + " or cur_page = 0) " +
                                 " Order by kod desc ";
                    if (ExecRead(connectionID, out readerImg, sql, true).result)
                    {
                        if (readerImg.Read())
                        {
                            zap.img_url = Convert.ToString(readerImg["img_url"]);
                        }
                    }
                    */
                     
                    Constants.PageShow.Add(zap);

                    if (prev_cur_page != zap.cur_page)
                    {
                        prev_cur_page = zap.cur_page;
                        List<_PageShow> m = new List<_PageShow>();
                        m.Add(zap);
                        Constants.DictPagesShow.Add(zap.cur_page, m);
                    }
                    else
                    {
                        Constants.DictPagesShow[zap.cur_page].Add(zap);
                    }
                }
                reader.Close();
            }
            catch (Exception ex)
            {
                string err;
                if (Constants.Viewerror)
                    err = ex.Message;
                else
                    err = "";

                MonitorLog.WriteLog("Ошибка заполнения pages_show " + err, MonitorLog.typelog.Error, 20, 101, true);
                reader.Close();
                connectionID.Close();
                return;
            }


            //ACTIONS
            if (!ExecRead(connectionID, out reader,
                " Select nzp_act, decrypt_char(act_name) as act_name, decrypt_char(hlp) as act_help " +
                " From s_actions Order by nzp_act", true).result)
            {
                connectionID.Close();
                return;
            }
            try
            {
                while (reader.Read())
                {
                    _Actions zap = new _Actions();
                    zap.nzp_act = (int)reader["nzp_act"];
                    zap.act_name = (string)reader["act_name"];
                    if (reader["act_help"] == DBNull.Value)
                        zap.act_hlp = "-";
                    else zap.act_hlp = (string)reader["act_help"];
                    Constants.Actions.Add(zap);
                    Constants.DictActions.Add(zap.nzp_act,zap);
                }
                reader.Close();
            }
            catch (Exception ex)
            {
                string err;
                if (Constants.Viewerror)
                    err = ex.Message;
                else
                    err = "";

                MonitorLog.WriteLog("Ошибка заполнения s_actions " + err, MonitorLog.typelog.Error, 20, 101, true);
                reader.Close();
                connectionID.Close();
                return;
            }

            //ACTIONS_SHOW
            /*
            if (!ExecRead(connectionID, out reader,
                " Select nzp_ash, cur_page, nzp_act, act_tip, act_dd, sort_kod " +
                " From actions_show " +
                " Where sort_kod||act_dd||act_tip||nzp_act||cur_page||'-'||nzp_ash||'actions_show' = decrypt_char(sign) " +
                " Order by cur_page,sort_kod", true).result)
            {
                connectionID.Close();
                return;
            }
            try
            {
                while (reader.Read())
                {
                    _ActShow zap = new _ActShow();
                    zap.cur_page = (int)reader["cur_page"];
                    zap.nzp_act = (int)reader["nzp_act"];
                    zap.act_tip = (int)reader["act_tip"];
                    zap.act_dd = (int)reader["act_dd"];
                    zap.sort_kod = (int)reader["sort_kod"];
                    zap.img_url = " ";

                    string sql = " Select img_url From img_lnk " +
                                 " Where tip = 2 and kod = " + zap.nzp_act.ToString() +
                                   " and (cur_page = " + zap.cur_page.ToString() + " or cur_page = 0) " +
                                 " Order by kod desc";
                    if (ExecRead(connectionID, out readerImg, sql, true).result)
                    {
                        if (readerImg.Read())
                        {
                            zap.img_url = Convert.ToString(readerImg["img_url"]);
                        }
                    }


                    Constants.ActShow.Add(zap);
                }
                reader.Close();
            }
            catch (Exception ex)
            {
                string err;
                if (Constants.Viewerror)
                    err = ex.Message;
                else
                    err = "";

                MonitorLog.WriteLog("Ошибка заполнения actions_show " + err, MonitorLog.typelog.Error, 20, 101, true);
                reader.Close();
                connectionID.Close();
                return;
            }
            */

            if (!ExecRead(connectionID, out reader,
#if PG
                " Select a.nzp_ash, a.cur_page, a.nzp_act, a.act_tip, a.act_dd, a.sort_kod, max(b.img_url) as img_url " +
                " From actions_show a left outer join img_lnk b on b.kod = a.nzp_act " +
                " and CAST(a.sort_kod as text)||CAST(a.act_dd as text)||CAST(a.act_tip as text)||CAST(a.nzp_act as text)||CAST(a.cur_page as text)||'-'||CAST(a.nzp_ash as text)||'actions_show' = decrypt_char(a.sign) " +
                "   and b.tip = 2 " +
                "   and ( b.cur_page = a.cur_page or b.cur_page = 0 ) " +
                " Group by 1,2,3,4,5,6 " +
                " Order by a.cur_page,a.sort_kod", true).result)
#else
                " Select a.nzp_ash, a.cur_page, a.nzp_act, a.act_tip, a.act_dd, a.sort_kod, max(b.img_url) as img_url " +
                " From actions_show a, outer img_lnk b " +
                " Where a.sort_kod||a.act_dd||a.act_tip||a.nzp_act||a.cur_page||'-'||a.nzp_ash||'actions_show' = decrypt_char(a.sign) " +
                "   and b.tip = 2 and b.kod = a.nzp_act " +
                "   and ( b.cur_page = a.cur_page or b.cur_page = 0 ) " +
                " Group by 1,2,3,4,5,6 " +
                " Order by a.cur_page,a.sort_kod", true).result)
#endif
            {
                connectionID.Close();
                return;
            }
            try
            {
                while (reader.Read())
                {
                    _ActShow zap = new _ActShow();
                    zap.cur_page = (int)reader["cur_page"];
                    zap.nzp_act = (int)reader["nzp_act"];
                    zap.act_tip = (int)reader["act_tip"];
                    zap.act_dd = (int)reader["act_dd"];
                    zap.sort_kod = (int)reader["sort_kod"];
                    zap.img_url = " ";

                    if (reader["img_url"] != DBNull.Value)
                        zap.img_url = Convert.ToString(reader["img_url"]);

                    /*
                    string sql = " Select img_url From img_lnk " +
                                 " Where tip = 2 and kod = " + zap.nzp_act.ToString() +
                                   " and (cur_page = " + zap.cur_page.ToString() + " or cur_page = 0) " +
                                 " Order by kod desc";
                    if (ExecRead(connectionID, out readerImg, sql, true).result)
                    {
                        if (readerImg.Read())
                        {
                            zap.img_url = Convert.ToString(readerImg["img_url"]);
                        }
                    }
                    */
                    Constants.ActShow.Add(zap);
                    if (Constants.DictActionShow.ContainsKey(zap.cur_page))
                    {
                        Constants.DictActionShow[zap.cur_page].Add(zap);
                    }
                    else
                    {
                        List<_ActShow> m = new List<_ActShow>();
                        m.Add(zap);
                        Constants.DictActionShow.Add(zap.cur_page, m);
                    }
                }
                reader.Close();
            }
            catch (Exception ex)
            {
                string err;
                if (Constants.Viewerror)
                    err = ex.Message;
                else
                    err = "";

                MonitorLog.WriteLog("Ошибка заполнения actions_show " + err, MonitorLog.typelog.Error, 20, 101, true);
                reader.Close();
                connectionID.Close();
                return;
            }


            //ACTIONS_LNK
            if (!ExecRead(connectionID, out reader, " Select * From actions_lnk Order by cur_page", true).result)
            {
                connectionID.Close();
                return;
            }
            try
            {
                while (reader.Read())
                {
                    _ActLnk zap = new _ActLnk();
                    zap.cur_page = (int)reader["cur_page"];
                    zap.nzp_act = (int)reader["nzp_act"];
                    zap.page_url = (int)reader["page_url"];

                    Constants.ActLnk.Add(zap);
                }
                reader.Close();
            }
            catch (Exception ex)
            {
                string err;
                if (Constants.Viewerror)
                    err = ex.Message;
                else
                    err = "";

                MonitorLog.WriteLog("Ошибка заполнения actions_lnk " + err, MonitorLog.typelog.Error, 20, 101, true);
                reader.Close();
                connectionID.Close();
                return;
            }




            //ExtJS меню
            if (ExecRead(connectionID, out reader, " Select * From ext_mm Order by mm_sort", false).result)
            {
                try
                {
                    while (reader.Read())
                    {
                        _ExtMM zap = new _ExtMM();
                        zap.nzp_mm  = (int)reader["nzp_mm"];
                        zap.mm_text = (string)reader["mm_text"];
                        zap.mm_sort = (int)reader["mm_sort"];

                        zap.mm_text = zap.mm_text.Trim();

                        Constants.ExtMM.Add(zap);
                    }
                    reader.Close();
                }
                catch (Exception ex)
                {
                    string err;
                    if (Constants.Viewerror)
                        err = ex.Message;
                    else
                        err = "";

                    MonitorLog.WriteLog("Ошибка заполнения ext_mm " + err, MonitorLog.typelog.Error, 20, 101, true);
                    reader.Close();
                    return;
                }
            }


            //ExtJS подменю
            if (ExecRead(connectionID, out reader, " Select * From ext_pm Order by pm_sort", false).result)
            {
                try
                {
                    while (reader.Read())
                    {
                        _ExtPM zap = new _ExtPM();
                        zap.nzp_pm      = (int)reader["nzp_pm"];
                        zap.nzp_mm      = (int)reader["nzp_mm"];
                        zap.pm_text     = (string)reader["pm_text"];
                        zap.pm_action   = (string)reader["pm_action"];
                        zap.pm_control  = (string)reader["pm_control"];
                        zap.pm_sort     = (int)reader["pm_sort"];


                        zap.pm_text     = zap.pm_text.Trim();
                        zap.pm_action   = zap.pm_action.Trim();
                        zap.pm_control  = zap.pm_control.Trim();

                        Constants.ExtPM.Add(zap);
                    }
                    reader.Close();
                }
                catch (Exception ex)
                {
                    string err;
                    if (Constants.Viewerror)
                        err = ex.Message;
                    else
                        err = "";

                    MonitorLog.WriteLog("Ошибка заполнения ext_pm " + err, MonitorLog.typelog.Error, 20, 101, true);
                    reader.Close();
                    return;
                }
            }



            connectionID.Close();
        }//StartApp

        //----------------------------------------------------------------------
        public List<_Help> LoadHelp(int nzp_user, int cur_page, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            List<_Help> ListHelp = new List<_Help>();

            IDbConnection conn_db = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return null;

            string tablename = "s_help";
            if (!TableInWebCashe(conn_db, tablename))
            {
                conn_db.Close();

                ret.result = false;
                ret.text = "Данные не были выбраны";

                return null;
            }

            //выбрать список
            string sql;
            sql = "Select tip, kod, sort, hlp From " + tablename + " where cur_page = " + cur_page.ToString() + " or cur_page = 0 order by tip, sort";

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
                    _Help help = new _Help();

                    if (reader["tip"] == DBNull.Value) help.tip = 0;
                    else help.tip = Convert.ToInt32(reader["tip"]);

                    if (reader["kod"] == DBNull.Value) help.kod = 0;
                    else help.kod = Convert.ToInt32(reader["kod"]);

                    if (reader["sort"] == DBNull.Value) help.sort = 0;
                    else help.sort = Convert.ToInt32(reader["sort"]);

                    if (reader["hlp"] == DBNull.Value) help.hlp = "";
                    else help.hlp = Convert.ToString(reader["hlp"]);

                    ListHelp.Add(help);
                }                
                reader.Close();
                conn_db.Close();
                return ListHelp;
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

                MonitorLog.WriteLog("Ошибка заполнения Справки " + err, MonitorLog.typelog.Error, 20, 201, true);

                return null;
            }
        }



        //----------------------------------------------------------------------
        public string GetSpravFile(out Returns ret, Int64 nzp_act)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            if (nzp_act < 1)
            {
                ret.result = false;
                ret.text = "Не определен отчет";
                return null;
            }

            //заполнить webdata:tXX_cnt
            IDbConnection conn_db = GetConnection(Constants.cons_Webdata);

            IDataReader reader;

            ret = OpenDb(conn_db, true);
            if (!ret.result)
            {
                return null;
            }

            StringBuilder sql = new StringBuilder();

            sql.Append(" Select file_name ");
            sql.Append(" From report ");
            sql.Append(" Where nzp_act ="+nzp_act.ToString());
            
            if (!ExecRead(conn_db, out reader, sql.ToString(), true).result)
            {
                MonitorLog.WriteLog("Ошибка выборки " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                conn_db.Close();
                ret.result = false;
                return null;
            }
            string retStr = "";
            try
            {
                while (reader.Read())
                {
                    if (reader["file_name"] != DBNull.Value) retStr = Convert.ToString(reader["file_name"]).Trim();
                }
                reader.Close();
                conn_db.Close(); //закрыть соединение с основной базой
                
                return retStr;
            }
            finally
            {
                conn_db.Close(); //закрыть соединение с основной базой
            }
        }
    }
}