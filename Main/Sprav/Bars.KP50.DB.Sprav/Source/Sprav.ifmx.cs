using System;
using System.Data;
using System.Globalization;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Castle.MicroKernel.Util;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;
using System.Text;
using System.Net;
using System.IO;


namespace STCLINE.KP50.DataBase
{


    //----------------------------------------------------------------------
    public partial class DbSprav : DbSpravKernel
    //----------------------------------------------------------------------
    {
        public List<string> GetMainPageTitle()
        {
            var Spis = new List<string>();
            IDbConnection connDB = GetConnection(Constants.cons_Kernel);
            Returns ret = OpenDb(connDB, true);
            if (!ret.result) return null;
            try
            {

                //проверка на существование таблицы
                if (!TempTableInWebCashe(connDB, "s_point"))
                {
                    return Spis;
                }

                //выбрать список
                string sql = " SELECT s.point " +
                             " FROM  " + Points.Pref + DBManager.sKernelAliasRest + "s_point s " +
                             " WHERE s.nzp_wp = 1";
                MyDataReader reader;
                if (!ExecRead(connDB, out reader, sql, true).result)
                {
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

            }
            finally
            {
                connDB.Close();
            }
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
            IDbConnection connWeb = GetConnection(Constants.cons_Webdata);
            Returns ret = OpenDb(connWeb, true);
            if (!ret.result) return ret;

            string srv = "";
            if (nzp_server > 0)
                srv = "_" + nzp_server;

            string services = "services" + srv;
            string servicesFull = DBManager.GetFullBaseName(connWeb) +
                DBManager.tableDelimiter + services;

            CreateWebService(connWeb, services, out ret);
            connWeb.Close();

            if (is_insert)
            {
                IDbConnection connDB = GetConnection(Constants.cons_Kernel);
                ret = OpenDb(connDB, true);
                if (!ret.result)
                {
                    connWeb.Close();
                    return ret;
                }

                string sql = " Insert into " + servicesFull +
                             " (nzp_serv, service, service_small, service_name, ed_izmer, type_lgot, nzp_frm, ordering) " +
                    " Select nzp_serv, service, service_small, service_name, ed_izmer, type_lgot, nzp_frm, ordering " +
                             " From " + Points.Pref + DBManager.sKernelAliasRest + "services";
                //выбрать список
                ret = ExecSQL(connDB, sql, true);

                connDB.Close();
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
            var service = new Service();
            finder.CopyTo(service);
            return ServiceLoad(service, orderby, out ret);
        }

        public List<_Service> ServiceLoad(Service finder, string orderby, out Returns ret)
        //----------------------------------------------------------------------
        {


            var spis = new Services();


            IDbConnection connDB = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(connDB, true);
            if (!ret.result) return null;

            if (finder.pref == "") finder.pref = Points.Pref;

            //выбрать список
            string sql =
                " Select nzp_serv, service, service_small, service_name, ed_izmer, " +
                "       nzp_measure, type_lgot, nzp_frm, ordering " +
                " From " + finder.pref + DBManager.sKernelAliasRest + "services " +
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
                        where += " and nzp_serv in (select nzp_serv " +
                                 " from " + Points.Pref + DBManager.sKernelAliasRest + "services_sg) ";

                    }
                       //подтягиваем услуги для режима генерации ИПУ через
                    else if (str.Contains("GenLsPuSpisServ"))
                    {
                        where += FiltrGenPuLs(finder.dopFind, finder.pref);
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
                    if (role.tip == Constants.role_sql & role.kod == Constants.role_sql_serv)
                        where += " and nzp_serv in (" + role.val + ")";

            if (finder.orderings != null && finder.orderings.Count > 0)
            {
                bool isFirst = true;
                foreach (_OrderingField order in finder.orderings)
                {
                    try
                    {
                        Type type = finder.GetType();
                        System.Reflection.MemberInfo info = type.GetProperty(order.fieldName);
                        if (info != null)
                        {
                            string property;
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
                                sql += " " + property + " " +
                                       (order.orderingDirection == OrderingDirection.Ascending ? " asc" : "desc");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MonitorLog.WriteLog("Ошибка заполнения справочника услуг " + ex.Message,
                            MonitorLog.typelog.Error, 20, 201, true);
                    }
                }
            }
            else
                sql += where + " Order by " + orderby;

            MyDataReader reader;
            if (!ExecRead(connDB, out reader, sql, true).result)
            {
                connDB.Close();
                return null;
            }
            try
            {
                int i = 0;
                while (reader.Read())
                {
                    i++;
                    if (i <= finder.skip) continue;
                    var zap = new _Service();

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
                    if (reader["service_small"] != DBNull.Value)
                        zap.service_small = Convert.ToString(reader["service_small"]).Trim();
                    if (reader["service_name"] != DBNull.Value)
                        zap.service_name = Convert.ToString(reader["service_name"]).Trim();
                    if (reader["ed_izmer"] != DBNull.Value) zap.ed_izmer = Convert.ToString(reader["ed_izmer"]).Trim();
                    if (reader["nzp_measure"] != DBNull.Value) zap.nzp_measure = Convert.ToInt32(reader["nzp_measure"]);
                    if (reader["type_lgot"] != DBNull.Value) zap.type_lgot = Convert.ToInt32(reader["type_lgot"]);
                    if (reader["nzp_frm"] != DBNull.Value) zap.nzp_frm = Convert.ToInt32(reader["nzp_frm"]);
                    if (reader["ordering"] != DBNull.Value) zap.ordering = Convert.ToInt32(reader["ordering"]);

                    spis.ServiceList.Add(zap);
                    if (finder.rows > 0 && i >= finder.skip + finder.rows) break;
                }

                reader.Close();

                sql = " Select count(*) From " + finder.pref + DBManager.sKernelAliasRest + "services " +
                      " Where 1 = 1 " + where;
                object count = ExecScalar(connDB, sql, out ret, true);
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

                return spis.ServiceList;
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

                MonitorLog.WriteLog("Ошибка заполнения справочника услуг " + err, MonitorLog.typelog.Error, 20, 201,
                    true);

                return null;
            }
            finally
            {
                connDB.Close();
            }
        }

        private string FiltrGenPuLs(List<string> dopFind, string pref)
        {
            if (dopFind != null)
            {
                foreach (string str in dopFind)
                {
                    if (str.Contains("GenLsPuSpisServ"))
                    {
                        string[] result = str.Split(new string[] { "|" }, StringSplitOptions.None);
                        result = result.Where(x => x != "GenLsPuSpisServ").ToArray();
                        if (result.Count() < 1) return "";
                        string inStr = String.Join(",", result);
                        if (String.IsNullOrEmpty(inStr.Trim())) return "";


                        string s_counts = pref + "_kernel" + tableDelimiter + "s_counts";
                        string s_countsdop = pref + "_kernel" + tableDelimiter + "s_countsdop";
                        string dopWhere = " AND nzp_serv IN " +
                                          " (SELECT nzp_serv FROM " + s_counts + " WHERE nzp_cnt IN (" + inStr + ")" +
                                          " UNION " +
                                          " SELECT nzp_serv FROM " + s_countsdop + " WHERE nzp_cnt IN (" + inStr + "))";
                        return dopWhere;

                    }
                }
            }

            return "";
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


            var spis = new Services();
            spis.CountsList.Clear();

            string connKernel = Points.GetConnByPref(finder.pref);
            IDbConnection connDB = GetConnection(connKernel);
            //IDbConnection conn_db = GetConnection(Constants.cons_Kernel);

            ret = OpenDb(connDB, true);
            if (!ret.result) return null;

            //выбрать список
            MyDataReader reader;
            string sql = "", where = "";

            if (finder.RolesVal != null)
                foreach (_RolesVal role in finder.RolesVal)
                    if (role.tip == Constants.role_sql & role.kod == Constants.role_sql_serv) where += " and c.nzp_serv in (" + role.val + ")";

            if (finder.pref.Trim() != "")
            {
                ExecSQL(connDB, "drop table serv_tarif", false);


                sql = " create temp table serv_tarif( " +
                      " nzp_serv integer)" + DBManager.sUnlogTempTable;
                ExecSQL(connDB, sql, true);

                sql = " insert into serv_tarif (nzp_serv)" +
                      " select  nzp_serv " +
                      " from " + finder.pref.Trim() + DBManager.sDataAliasRest + "tarif " +
                      (nzp_kvar > 0 ? " where nzp_kvar= " + nzp_kvar : "") +
                      " group by 1";
                ret = ExecSQL(connDB, sql, true);
                if (!ret.result)
                {
                    connDB.Close();
                    return null;
                }
                sql = " Select distinct c.nzp_serv, c.name, c.nzp_cnt, m.measure " +
                      " From serv_tarif t, " + finder.pref.Trim() + DBManager.sKernelAliasRest + "s_counts c " +
                      "       left outer join " +
                      finder.pref.Trim() + DBManager.sKernelAliasRest + "s_measure m on c.nzp_measure = m.nzp_measure " +
                      " Where t.nzp_serv = c.nzp_serv  " + where +
                      " Union all " +
                      " Select distinct c.nzp_serv, c.name, c.nzp_cnt, m.measure " +
                      " From serv_tarif t, " +
                      finder.pref.Trim() + DBManager.sKernelAliasRest + "s_countsdop c " +
                      "  left outer join " +
                      finder.pref.Trim() + DBManager.sKernelAliasRest + "s_measure m on c.nzp_measure = m.nzp_measure" +
                      " Where t.nzp_serv = c.nzp_serv  " + where +
                      " Order by 2";
            }
            else
                sql = " Select c.nzp_serv, c.name, c.nzp_cnt, m.measure " +
                      " From " + Points.Pref + DBManager.sKernelAliasRest + "services s, " +
                      Points.Pref + DBManager.sKernelAliasRest + "s_counts c " +
                      " left outer join " + Points.Pref + DBManager.sKernelAliasRest + "s_measure m " +
                      "  on c.nzp_measure = m.nzp_measure" +
                      " Where s.nzp_serv = c.nzp_serv " + where +
                      " Order by c.name ";

            ret = ExecRead(connDB, out reader, sql, true);
            if (!ret.result)
            {
                connDB.Close();
                return null;
            }
            try
            {
                int i = 0;
                while (reader.Read())
                {
                    i++;
                    var zap = new _Service();

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
                        zap.service = reader["name"].ToString().Trim();

                    if (reader["measure"] != DBNull.Value)
                        zap.service += " (" + reader["measure"].ToString().Trim() + ")";

                    spis.CountsList.Add(zap);
                }

                ret.tag = i;

                reader.Close();

                if (finder.pref.Trim() != "")
                {
                    ExecSQL(connDB, "drop table serv_tarif", false);
                }
                connDB.Close();
                return spis.CountsList;
            }
            catch (Exception ex)
            {
                reader.Close();
                connDB.Close();

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
            IDbConnection connWeb = GetConnection(Constants.cons_Webdata);
            Returns ret = OpenDb(connWeb, true);
            if (!ret.result) return ret;

            string srv = "";
            if (nzp_server > 0)
                srv = "_" + nzp_server;

            string supplier = "supplier" + srv;
            string supplier_full = DBManager.GetFullBaseName(connWeb) +
                DBManager.tableDelimiter + supplier;

            CreateWebSupplier(connWeb, supplier, out ret);
            connWeb.Close();

            if (is_insert)
            {
                IDbConnection connDB = GetConnection(Constants.cons_Kernel);
                ret = OpenDb(connDB, true);
                if (!ret.result)
                {
                    return ret;
                }

                string sql = " Insert into " + supplier_full +
                             " ( nzp_supp, name_supp, adres_supp, phone_supp, geton_plat, have_proc, kod_supp ) " +
                    " Select nzp_supp, name_supp, adres_supp, phone_supp, geton_plat, have_proc, kod_supp " +
                             " From " + Points.Pref + DBManager.sKernelAliasRest + "supplier";
                //выбрать список
                ret = ExecSQL(connDB, sql, true);

                connDB.Close();
            }

            return ret;
        }

        public List<_Supplier> SupplierLoad(Supplier finder, enTypeOfSupp type, out Returns ret)
        //----------------------------------------------------------------------
        {

            if (finder.pref == "") finder.pref = Points.Pref;
            var spis = new List<_Supplier>();

            IDbConnection connDB = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(connDB, true);
            if (!ret.result) return null;

            var tables = new DbTables(DBManager.getServer(connDB));

            // Условия поиска
            string where = "";

            if (finder.nzp_supp != 0) where += " and s.nzp_supp = " + finder.nzp_supp;

            if (finder.dopFind != null && finder.dopFind.Count > 0)
                where += " and upper(s.name_supp) like '%" + finder.dopFind[0].ToUpper().Replace("'", "''").Replace("*", "%") + "%' ";

            if (finder.RolesVal != null)
                foreach (_RolesVal role in finder.RolesVal)
                    if (role.tip == Constants.role_sql && role.kod == Constants.role_sql_supp)
                        where += " and s.nzp_supp in (" + role.val + ")";

            if (type == enTypeOfSupp.NotInListPayers)
            {

                where += " and s.nzp_supp not in ( Select distinct p.nzp_supp From " + tables.payer + " p Where p.nzp_type = 2)";

            }

            //Определить общее количество записей
            string sql = " Select count(*) " +
                         " From " + finder.pref + DBManager.sKernelAliasRest + "supplier s " +
                         " Where 1 = 1 and nzp_supp > 0 " + where;
            object count = ExecScalar(connDB, sql, out ret, true);
            int recordsTotalCount;
            try { recordsTotalCount = Convert.ToInt32(count); }
            catch (Exception e)
            {
                ret = new Returns(false, "Ошибка при определении количества записей: " + (Constants.Debug ? e.Message : ""));
                MonitorLog.WriteLog("Ошибка SupplierLoad " + (Constants.Viewerror ? "\n" + e.Message : ""), MonitorLog.typelog.Error, 20, 201, true);
                connDB.Close();
                return null;
            }

            //выбрать список
            if (!Points.isFinances)
            {
                sql =
                    " Select distinct s.nzp_supp, s.name_supp, 0 as nzp_payer, s.adres_supp, s.phone_supp, s.geton_plat, s.have_proc, s.kod_supp " +
                    " From " + finder.pref + DBManager.sKernelAliasRest + "supplier s " +
 " Where 1 = 1 and s.nzp_supp > 0 " + where +
                      " Order by name_supp";
            }
            else
            {

                sql = " Select distinct s.nzp_supp, s.name_supp, p.nzp_payer, s.adres_supp, s.phone_supp, s.geton_plat, s.have_proc, s.kod_supp " +
                      " From " + finder.pref + DBManager.sKernelAliasRest + "supplier s" +
                      " left outer join " + Points.Pref + DBManager.sKernelAliasRest + "s_payer p " +
                      " on s.nzp_supp = p.nzp_supp " +
                      " where 1 = 1 and s.nzp_supp > 0 " + where +
                      " Order by name_supp";


            }

            MyDataReader reader;
            if (!ExecRead(connDB, out reader, sql, true).result)
            {
                connDB.Close();
                return null;
            }
            try
            {
                int i = 0;
                while (reader.Read())
                {
                    i++;
                    if (i <= finder.skip) continue;
                    var zap = new _Supplier { num = i };

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
                connDB.Close();
                return spis;
            }
            catch (Exception ex)
            {
                reader.Close();
                connDB.Close();
                ret = new Returns(false, ex.Message);
                MonitorLog.WriteLog("Ошибка заполнения списка поставщиков " + (Constants.Viewerror ? " \n " + ex.Message : ""), MonitorLog.typelog.Error, 20, 201, true);
                return null;
            }
        }


        public List<_Supplier> LoadSupplierByArea(Supplier finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            if (finder.pref == "") finder.pref = Points.Pref;
            var spis = new List<_Supplier>();

            IDbConnection connDB = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(connDB, true);
            if (!ret.result) return null;

            var tables = new DbTables(DBManager.getServer(connDB));

            // Условия поиска
            string where = "";
            if (finder.nzp_wp != 0) where += " and nzp_wp=" + finder.nzp_wp;

            //выбрать список

            string sql =
         "select distinct sp.nzp_supp,  s.name_supp from " + finder.pref + DBManager.sKernelAliasRest +
         "supplier_point sp, " + finder.pref + DBManager.sKernelAliasRest +
         "supplier s  where 1=1 and sp.nzp_supp=s.nzp_supp " + where;

            MyDataReader reader;
            if (!ExecRead(connDB, out reader, sql, true).result)
            {
                connDB.Close();
                return null;
            }
            try
            {

                while (reader.Read())
                {
                    var zap = new _Supplier();
                    if (reader["nzp_supp"] != DBNull.Value) zap.nzp_supp = Convert.ToInt32(reader["nzp_supp"]);
                    if (reader["name_supp"] != DBNull.Value) zap.name_supp = Convert.ToString(reader["name_supp"]).Trim();
                    spis.Add(zap);
                }

                reader.Close();
                connDB.Close();
                return spis;
            }
            catch (Exception ex)
            {
                reader.Close();
                connDB.Close();
                ret = new Returns(false, ex.Message);
                MonitorLog.WriteLog("Ошибка заполнения списка поставщиков " + (Constants.Viewerror ? " \n " + ex.Message : ""), MonitorLog.typelog.Error, 20, 201, true);
                return null;
            }
        }


        public List<ContractClass> ContractsLoad(ContractFinder finder, enTypeOfSupp type, out Returns ret)
        {
            if (finder.nzp_user <= 0)
            {
                ret = new Returns(false, "Пользователь не определен", -1);
                return null;
            }

            if (finder.pref == "") finder.pref = Points.Pref;
            var spis = new List<ContractClass>();

            IDbConnection connDB = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(connDB, true);
            if (!ret.result) return null;

            var tables = new DbTables(DBManager.getServer(connDB));

            // Условия поиска
            string where = "";

            if (finder.nzp_supp != 0) where += " and s.nzp_supp = " + finder.nzp_supp;

            if (finder.name_supp != "")
                where += " and upper(s.name_supp) like '%" + finder.name_supp.ToUpper().Replace("'", "''").Replace("*", "%") + "%' ";

            if (finder.RolesVal != null)
                foreach (_RolesVal role in finder.RolesVal)
                    if (role.tip == Constants.role_sql && role.kod == Constants.role_sql_supp)
                        where += " and s.nzp_supp in (" + role.val + ")";

            if (finder.nzp_serv > 0 && finder.pref != Points.Pref)
            {
                string tXX = sDefaultSchema + "t" + Convert.ToString(finder.nzp_user) + "_selectedls" + finder.listNumber;
                string where_num_ls = "";
                if (TempTableInWebCashe(connDB, tXX))
                {
                    where_num_ls = " and num_ls in (select num_ls from " + tXX + " where pref='" + finder.pref + "' and mark=1)";
                }

                where += " and nzp_supp in (select distinct nzp_supp from " + finder.pref + "_data" + tableDelimiter +
                    "tarif where nzp_serv = " + finder.nzp_serv + where_num_ls + ")";
            }

            var table = "";
            var usl = "";
            if (finder.nzp_wp != null && finder.nzp_wp.Count > 0)
            {
                table = ", " + Points.Pref + DBManager.sKernelAliasRest + "supplier_point swp  ";
                usl = " and s.nzp_supp = swp.nzp_supp and nzp_wp = " + finder.nzp_wp[0];
            }

            //Определить общее количество записей
            string sql = " Select count(distinct s.nzp_supp) " +
                         " From " + Points.Pref + DBManager.sKernelAliasRest + "supplier s  " +
                        table +
                         " Where 1 = 1 and s.nzp_supp>0  " + where + usl;
            object count = ExecScalar(connDB, sql, out ret, true);
            int recordsTotalCount;
            try { recordsTotalCount = Convert.ToInt32(count); }
            catch (Exception e)
            {
                ret = new Returns(false, "Ошибка при определении количества записей: " + (Constants.Debug ? e.Message : ""));
                MonitorLog.WriteLog("Ошибка ContractsLoad " + (Constants.Viewerror ? "\n" + e.Message : ""), MonitorLog.typelog.Error, 20, 201, true);
                connDB.Close();
                return null;
            }

            string is_dpd = " , dpd ";
            if (Points.Pref != finder.pref)
            {
                is_dpd = " , (select dpd from " + tables.supplier + " sp where sp.nzp_supp = s.nzp_supp ) dpd ";
            }

            //выбрать список   
            sql =
                " SELECT distinct s.nzp_supp, s.name_supp, s.nzp_payer_agent, s.nzp_payer_princip," +
                " s.nzp_payer_supp, s.adres_supp, s.phone_supp, s.geton_plat, s.have_proc, s.kod_supp " +
                is_dpd +
                " FROM " + finder.pref + DBManager.sKernelAliasRest + "supplier s " + table +
                /*  " left outer join " + Points.Pref + DBManager.sKernelAliasRest + "s_payer p " +
                 * " on s.nzp_supp = p.nzp_supp " +*/
                " WHERE 1 = 1 and s.nzp_supp>0 " + where + usl +
                " GROUP BY s.nzp_supp, s.name_supp, s.nzp_payer_agent, s.nzp_payer_princip, s.nzp_payer_supp," +
                " s.adres_supp, s.phone_supp, s.geton_plat, s.have_proc, s.kod_supp , dpd " +
                " ORDER BY name_supp";

            MyDataReader reader;
            ret = ExecRead(connDB, out reader, sql, true);
            if (!ret.result)
            {
                connDB.Close();
                return null;
            }
            try
            {
                int i = 0;
                while (reader.Read())
                {
                    i++;
                    if (i <= finder.skip) continue;
                    var zap = new ContractClass { num = i };

                    if (reader["nzp_supp"] != DBNull.Value) zap.nzp_supp = Convert.ToInt32(reader["nzp_supp"]);
                    if (reader["name_supp"] != DBNull.Value) zap.name_supp = Convert.ToString(reader["name_supp"]).Trim();
                    if (reader["nzp_payer_agent"] != DBNull.Value) zap.nzp_payer_agent = Convert.ToInt32(reader["nzp_payer_agent"]);
                    if (reader["nzp_payer_princip"] != DBNull.Value) zap.nzp_payer_princip = Convert.ToInt32(reader["nzp_payer_princip"]);
                    if (reader["nzp_payer_supp"] != DBNull.Value) zap.nzp_payer_supp = Convert.ToInt32(reader["nzp_payer_supp"]);
                    if (reader["dpd"] != DBNull.Value) zap.dpd = Convert.ToInt16(reader["dpd"]);

                    if (reader["adres_supp"] != DBNull.Value) zap.adres_supp = Convert.ToString(reader["adres_supp"]).Trim();
                    if (reader["geton_plat"] != DBNull.Value) zap.geton_plat = Convert.ToString(reader["geton_plat"]).Trim();
                    if (reader["have_proc"] != DBNull.Value) zap.have_proc = Convert.ToInt32(reader["have_proc"]);
                    if (reader["kod_supp"] != DBNull.Value) zap.kod_supp = Convert.ToString(reader["kod_supp"]).Trim();


                    spis.Add(zap);
                    if (finder.rows > 0 && i >= finder.skip + finder.rows) break;
                }

                ret.tag = recordsTotalCount;

                reader.Close();
                connDB.Close();
                return spis;
            }
            catch (Exception ex)
            {
                reader.Close();
                connDB.Close();
                ret = new Returns(false, ex.Message);
                MonitorLog.WriteLog("Ошибка заполнения списка договоров " + (Constants.Viewerror ? " \n " + ex.Message : ""), MonitorLog.typelog.Error, 20, 201, true);
                return null;
            }
        }

        public List<ContractClass> NewFdContractsLoad(ContractFinder finder, enTypeOfSupp type, out Returns ret)
        {
            if (finder.nzp_user <= 0)
            {
                ret = new Returns(false, "Пользователь не определен", -1);
                return null;
            }

            if (finder.pref == "") finder.pref = Points.Pref;
            var spis = new List<ContractClass>();

            IDbConnection connDB = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(connDB, true);
            if (!ret.result) return null;

            var tables = new DbTables(DBManager.getServer(connDB));

            // Условия поиска
            string where = "";

            if (finder.allow_overpayments > 0) where += " and s.allow_overpayments=1"; 

            if (finder.nzp_supp != 0) where += " and s.nzp_supp = " + finder.nzp_supp;

            if (finder.name_supp != "")
                where += " and upper(s.name_supp) like '%" + finder.name_supp.ToUpper().Replace("'", "''").Replace("*", "%") + "%' ";

            if (finder.RolesVal != null)
                foreach (_RolesVal role in finder.RolesVal)
                    if (role.tip == Constants.role_sql && role.kod == Constants.role_sql_supp)
                        where += " and s.nzp_supp in (" + role.val + ")";

            if (finder.nzp_serv > 0 && finder.pref != Points.Pref)
            {
                string tXX = sDefaultSchema + "t" + Convert.ToString(finder.nzp_user) + "_selectedls" + finder.listNumber;
                string where_num_ls = "";
                if (TempTableInWebCashe(connDB, tXX))
                {
                    where_num_ls = " and num_ls in (select num_ls from " + tXX + " where pref='" + finder.pref + "' and mark=1)";
                }

                where += " and nzp_supp in (select distinct nzp_supp from " + finder.pref + "_data" + tableDelimiter +
                    "tarif where nzp_serv = " + finder.nzp_serv + where_num_ls + ")";
            }

            var tsql = new StringBuilder();
            var table = new StringBuilder("");
            var usl = new StringBuilder("");
            if (finder.nzp_fd > 0)
            {
                ExecSQL(connDB, "drop table  tnzpfb", false);
                table.Append("tnzpfb");
                tsql.AppendFormat("select id  into temp {3} from {0}_data{1}fn_dogovor_bank_lnk where nzp_fd = {2}", Points.Pref,
                    tableDelimiter, finder.nzp_fd, table);
                ret = ExecSQL(connDB, tsql.ToString(), true);
                if (!ret.result)
                {
                    connDB.Close();
                    return null;
                }
                table.Insert(0, ", ");
                table.Append(" t ");
                usl.AppendFormat(" and t.id = s.fn_dogovor_bank_lnk_id ");
            }

            //Определить общее количество записей
            string sql = " Select count(distinct s.nzp_supp) " +
                         " From " + Points.Pref + DBManager.sKernelAliasRest + "supplier s  " +
                        table +
                         " Where 1 = 1 and s.nzp_supp > 0  " + where + usl;
            object count = ExecScalar(connDB, sql, out ret, true);
            int recordsTotalCount;
            try { recordsTotalCount = Convert.ToInt32(count); }
            catch (Exception e)
            {
                ret = new Returns(false, "Ошибка при определении количества записей: " + (Constants.Debug ? e.Message : ""));
                MonitorLog.WriteLog("Ошибка ContractsLoad " + (Constants.Viewerror ? "\n" + e.Message : ""), MonitorLog.typelog.Error, 20, 201, true);
                connDB.Close();
                return null;
            }

            string is_dpd = " , dpd ";
            if (Points.Pref != finder.pref)
            {
                is_dpd = " , (select dpd from " + tables.supplier + " sp where sp.nzp_supp = s.nzp_supp ) dpd ";
            }
            
            //выбрать список   
            sql =
                " SELECT distinct s.nzp_supp, s.name_supp, s.nzp_payer_agent, s.nzp_payer_princip," +
                " s.nzp_payer_supp, s.kod_supp, s.allow_overpayments " +
                is_dpd +
                " FROM " + finder.pref + DBManager.sKernelAliasRest + "supplier s " + table +
                " WHERE 1 = 1 and s.nzp_supp>0 " + where + usl +
                " GROUP BY s.nzp_supp, s.name_supp, s.nzp_payer_agent, s.nzp_payer_princip, s.nzp_payer_supp," +
                " s.kod_supp, s.allow_overpayments , dpd " +
                " ORDER BY name_supp";
            
            MyDataReader reader;
            if (!ExecRead(connDB, out reader, sql, true).result)
            {
                connDB.Close();
                return null;
            }
            try
            {
                int i = 0;
                while (reader.Read())
                {
                    i++;
                    if (i <= finder.skip) continue;
                    var zap = new ContractClass { num = i };

                    if (reader["nzp_supp"] != DBNull.Value) zap.nzp_supp = Convert.ToInt32(reader["nzp_supp"]);
                    if (reader["name_supp"] != DBNull.Value) zap.name_supp = Convert.ToString(reader["name_supp"]).Trim();
                    if (reader["nzp_payer_agent"] != DBNull.Value) zap.nzp_payer_agent = Convert.ToInt32(reader["nzp_payer_agent"]);
                    if (reader["nzp_payer_princip"] != DBNull.Value) zap.nzp_payer_princip = Convert.ToInt32(reader["nzp_payer_princip"]);
                    if (reader["nzp_payer_supp"] != DBNull.Value) zap.nzp_payer_supp = Convert.ToInt32(reader["nzp_payer_supp"]);
                    if (reader["dpd"] != DBNull.Value) zap.dpd = Convert.ToInt16(reader["dpd"]);

                    //if (reader["adres_supp"] != DBNull.Value) zap.adres_supp = Convert.ToString(reader["adres_supp"]).Trim();
                    //if (reader["geton_plat"] != DBNull.Value) zap.geton_plat = Convert.ToString(reader["geton_plat"]).Trim();
                    //if (reader["have_proc"] != DBNull.Value) zap.have_proc = Convert.ToInt32(reader["have_proc"]);
                    if (reader["kod_supp"] != DBNull.Value) zap.kod_supp = Convert.ToString(reader["kod_supp"]).Trim();
                    if (reader["allow_overpayments"] != DBNull.Value) zap.allow_overpayments = Convert.ToInt16(reader["allow_overpayments"]);


                    spis.Add(zap);
                    if (finder.rows > 0 && i >= finder.skip + finder.rows) break;
                }

                ret.tag = recordsTotalCount;

                reader.Close();

                ExecSQL(connDB, "drop table tnzpfb", false);

                connDB.Close();
                return spis;
            }
            catch (Exception ex)
            {
                reader.Close();
                connDB.Close();
                ret = new Returns(false, ex.Message);
                MonitorLog.WriteLog("Ошибка заполнения списка договоров ЖКУ" + (Constants.Viewerror ? " \n " + ex.Message : ""), MonitorLog.typelog.Error, 20, 201, true);
                return null;
            }
        }


        public List<Payer> PayerLoad(Payer finder, out Returns ret)
        {
            if (finder.pref == "") finder.pref = Points.Pref;

            if (finder.nzp_user <= 0)
            {
                ret = new Returns(false, "Пользователь не определен", -1);
                return null;
            }
            var spis = new List<Payer>();

            IDbConnection connDB = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(connDB, true);
            if (!ret.result) return null;

            var tables = new DbTables(DBManager.getServer(connDB));

            // Условия поиска
            string where = "";

            if (finder.nzp_payer != 0) where += " and p.nzp_payer = " + finder.nzp_payer;

            if (finder.nzp_payers != "") where += " and p.nzp_payer in (" + finder.nzp_payers + ")";

            if (finder.payer != "") where += " and lower(p.payer) like '%" + finder.payer.ToLower() + "%'";

            if (finder.nzp_type > 0) where += "  and p.nzp_type = " + finder.nzp_type;
            #region finder.dopFind 
            if (finder.dopFind != null)
            {
                foreach (string str in finder.dopFind)
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

                        string fn_distrib = Points.Pref + "_fin_" + (d.Year - 2000).ToString("00") + tableDelimiter + "fn_distrib_dom_" + d.Month.ToString("00");

                        string pole = "";
                        if (finder.type_name == Payer.ContragentTypes.Princip.GetHashCode().ToString()) pole = "nzp_payer_princip";
                        else if (finder.type_name == Payer.ContragentTypes.PayingAgent.GetHashCode().ToString()) pole = "nzp_payer_agent";
                        else if (finder.type_name == Payer.ContragentTypes.ServiceSupplier.GetHashCode().ToString()) pole = "nzp_payer_supp";
                        else if (finder.type_name == Payer.ContragentTypes.Podr.GetHashCode().ToString()) pole = "nzp_payer_podr";
                        string into_temp_pg = "", into_temp_ifmx = "";
#if PG
                        into_temp_pg = " into temp temptabledistribsupp ";
                        into_temp_ifmx = "";
#else
                        into_temp_ifmx = " into temp temptabledistribsupp with no log ";
                        into_temp_pg = "";
#endif
                        ExecSQL(connDB, "drop table temptabledistribsupp", false);
                        string tmpsql = "select " + sUniqueWord + " nzp_supp " + into_temp_pg + " from " +
                            fn_distrib + into_temp_ifmx;
                        ret = ExecSQL(connDB, tmpsql, true);
                        if (!ret.result)
                        {
                            connDB.Close();
                            return null;
                        }
#if PG
                        into_temp_pg = " into temp temptabledistribsupp2";
                        into_temp_ifmx = "";
#else
                        into_temp_ifmx = " into temp temptabledistribsupp2 with no log";
                        into_temp_pg = "";
#endif
                        ExecSQL(connDB, "drop table temptabledistribsupp2", false);
                        tmpsql = "select s.nzp_payer_princip, s.nzp_payer_agent, s.nzp_payer_supp " + into_temp_pg + " from " +
                            " temptabledistribsupp d, " + Points.Pref + "_kernel" + tableDelimiter + "supplier s " +
                            " where d.nzp_supp = s.nzp_supp " + into_temp_ifmx;
                        ret = ExecSQL(connDB, tmpsql, true);
                        if (!ret.result)
                        {
                            connDB.Close();
                            return null;
                        }
                        where += " and nzp_payer in ( Select " + pole + " From temptabledistribsupp2)";
                    }
                }
            }
            #endregion

            if (finder.RolesVal != null)
                foreach (_RolesVal role in finder.RolesVal)
                    if (role.tip == Constants.role_sql && role.kod == Constants.role_sql_supp)
                        where += " and p.nzp_supp in (" + role.val + ")";

            //Определить общее количество записей
            string sql = " Select count(*) " +
                         " From " + finder.pref + DBManager.sKernelAliasRest + "s_payer p " +
                         " Where 1 = 1 " + where;
            object count = ExecScalar(connDB, sql, out ret, true);
            int recordsTotalCount;
            try { recordsTotalCount = Convert.ToInt32(count); }
            catch (Exception e)
            {
                ret = new Returns(false, "Ошибка при определении количества записей: " + (Constants.Debug ? e.Message : ""));
                MonitorLog.WriteLog("Ошибка PayerLoad " + (Constants.Viewerror ? "\n" + e.Message : ""), MonitorLog.typelog.Error, 20, 201, true);
                connDB.Close();
                return null;
            }

            //выбрать список   
            sql = " Select p.nzp_payer, p.payer" +
                " From " + finder.pref + DBManager.sKernelAliasRest + "s_payer p" +
                    " where 1 = 1 " + where +
                    " Order by payer";

            MyDataReader reader;
            if (!ExecRead(connDB, out reader, sql, true).result)
            {
                connDB.Close();
                return null;
            }
            try
            {
                int i = 0;
                while (reader.Read())
                {
                    i++;
                    if (i <= finder.skip) continue;
                    var zap = new Payer();

                    if (reader["nzp_payer"] != DBNull.Value) zap.nzp_payer = Convert.ToInt32(reader["nzp_payer"]);
                    if (reader["payer"] != DBNull.Value) zap.payer = Convert.ToString(reader["payer"]).Trim();

                    spis.Add(zap);
                    if (finder.rows > 0 && i >= finder.skip + finder.rows) break;
                }

                ret.tag = recordsTotalCount;

                reader.Close();
                connDB.Close();
                return spis;
            }
            catch (Exception ex)
            {
                reader.Close();
                connDB.Close();
                ret = new Returns(false, ex.Message);
                MonitorLog.WriteLog("Ошибка заполнения списка payer " + (Constants.Viewerror ? " \n " + ex.Message : ""), MonitorLog.typelog.Error, 20, 201, true);
                return null;
            }
        }

        public List<FileName> FileNameLoad(FileName finder, out Returns ret)
        //----------------------------------------------------------------------
        {

            if (finder.pref == "") finder.pref = Points.Pref;
            var spis = new List<FileName>();

            IDbConnection connDB = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(connDB, true);
            if (!ret.result) return null;

            var tables = new DbTables(DBManager.getServer(connDB));

            //Определить общее количество записей
            string sql = " Select count(*) " +
                         " From " + finder.pref + DBManager.sUploadAliasRest + "files_imported " +
                         " Where nzp_status = 3 ";
            object count = ExecScalar(connDB, sql, out ret, true);
            int recordsTotalCount;
            try
            {
                recordsTotalCount = Convert.ToInt32(count);
            }
            catch (Exception e)
            {
                ret = new Returns(false,
                    "Ошибка при определении количества записей: " + (Constants.Debug ? e.Message : ""));
                MonitorLog.WriteLog("Ошибка FileNameLoad " + (Constants.Viewerror ? "\n" + e.Message : ""),
                    MonitorLog.typelog.Error, 20, 201, true);
                connDB.Close();
                return null;
            }

            string calc_date = "";

            var months = new[]
                    {
                        "", "Январь", "Февраль",
                        "Март", "Апрель", "Май", "Июнь", "Июль", "Август", "Сентябрь",
                        "Октябрь", "Ноябрь", "Декабрь"
                    };

            //выбрать список
            sql =
                " Select fi.nzp_file, fi.loaded_name as file_name, fh.calc_date " +
                " From " + finder.pref + DBManager.sUploadAliasRest + "files_imported fi, " +
                finder.pref + DBManager.sUploadAliasRest + "file_head fh " +
                " Where fi.nzp_file = fh.nzp_file and fi.nzp_status = 3 ";

            MyDataReader reader;
            if (!ExecRead(connDB, out reader, sql, true).result)
            {
                connDB.Close();
                return null;
            }
            try
            {
                int i = 0;
                while (reader.Read())
                {
                    i++;
                    if (i <= finder.skip) continue;
                    var zap = new FileName();

                    #region Месяц, за который грузим файл

                    if (reader["calc_date"] != DBNull.Value)
                    {
                        int n_month = Convert.ToInt32((reader["calc_date"]).ToString().Substring(3, 2));
                        calc_date = " (" + months[n_month] + " " + (reader["calc_date"]).ToString().Substring(6, 4) +
                                    ")";
                    }

                    #endregion

                    if (reader["nzp_file"] != DBNull.Value) zap.nzp_file = Convert.ToInt32(reader["nzp_file"]);
                    if (reader["file_name"] != DBNull.Value)
                        zap.file_name = Convert.ToString(reader["file_name"]).Trim() + calc_date;

                    spis.Add(zap);
                    if (finder.rows > 0 && i >= finder.skip + finder.rows) break;
                }

                ret.tag = recordsTotalCount;

                reader.Close();
                connDB.Close();
                return spis;
            }
            catch (Exception ex)
            {
                reader.Close();
                connDB.Close();
                ret = new Returns(false, ex.Message);
                MonitorLog.WriteLog(
                    "Ошибка заполнения списка имен файлов загрузки " + (Constants.Viewerror ? " \n " + ex.Message : ""),
                    MonitorLog.typelog.Error, 20, 201, true);
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

            if (finder.bank.Trim().Length > 40)
            {
                return new Returns(false, "Наименование должно содержать не более 40 символов", -1);
            }

            if (finder.short_name != null)
                if (finder.short_name.Trim().Length > 40)
                {
                    return new Returns(false, "Краткое наименование должно содержать не более 40 символов", -1);
                }

            if (finder.adress != null)
                if (finder.adress.Trim().Length > 100)
                {
                    return new Returns(false, "Адрес должен содержать не более 100 символов", -1);
                }

            if (finder.phone != null)
                if (finder.phone.Trim().Length > 20)
                {
                    return new Returns(false, "Телефон должен содержать не более 20 символов", -1);
                }
            #endregion

            #region подключение к базе
            string connKernel = Points.GetConnByPref(Points.Pref);
            IDbConnection connDB = GetConnection(connKernel);
            Returns ret = OpenDb(connDB, true);
            if (!ret.result) return ret;
            #endregion

            if (finder.pref == "") finder.pref = Points.Pref;
            string newBank = Utils.EStrNull(finder.bank == null ? "" : finder.bank.Trim());
            string table = finder.pref + DBManager.sKernelAliasRest + "s_bank";
            string sql;

            string sqlBank;
            if (String.IsNullOrEmpty(finder.short_name))
            {
                sqlBank = "select COUNT(*) cnt from " + table +
                          " where (bank = " + newBank + ") AND nzp_bank <> " + finder.nzp_bank;
            }
            else
            {
                sqlBank = "select COUNT(*) cnt from " + table +
                          " where ((bank = " + newBank + ") OR " +
                          "(short_name = " + Utils.EStrNull(finder.short_name).Trim() + ")) " +
                          "AND nzp_bank <> " + finder.nzp_bank;
            }
            object count = ExecScalar(connDB, sqlBank, out ret, true);
            if (!ret.result)
            {
                connDB.Close();
                ret.tag = 0;
                return ret;
            }
            if (Convert.ToInt32(count) > 0)
            {
                ret.result = false;
                ret.text = "Обнаружено дублирование наименований банков";
                ret.tag = -1;
                connDB.Close();
                return ret;
            }

            bool isNew = false;

            if (finder.nzp_bank != 0)
            {
                sql = "select nzp_bank from " + table + " where nzp_bank = " + finder.nzp_bank;
                MyDataReader reader;
                ret = ExecRead(connDB, out reader, sql, true);
                if (!ret.result)
                {
                    connDB.Close();
                    ret.tag = 0;
                    return ret;
                }
                if (reader.Read())
                {
                    sql = " update " + table + " set " +
                          " bank = " + newBank + ", " +
                          " nzp_payer = " + Utils.EStrNull(finder.nzp_payer.ToString(CultureInfo.InvariantCulture)) + ", " +
                          " short_name = " + Utils.EStrNull(finder.short_name, "") + ", " +
                          " adress = " + Utils.EStrNull(finder.adress, "") + ", " +
                          " phone = " + Utils.EStrNull(finder.phone, "") + " " +
                          " where nzp_bank = " + finder.nzp_bank;
                }
                else
                {
                    isNew = true;
                    sql = " insert into " + table + " (bank, nzp_payer, short_name, adress, phone) " +
                          " values (" + newBank + ", " +
                          Utils.EStrNull(finder.nzp_payer.ToString(CultureInfo.InvariantCulture)) + ", " +
                          Utils.EStrNull(finder.short_name, "") + ", " +
                          Utils.EStrNull(finder.adress, "") + ", " +
                          Utils.EStrNull(finder.phone, "") + ")";
                }
                reader.Close();
            }
            else
            {
                isNew = true;
                sql = " insert into " + table + " (bank, nzp_payer, short_name, adress, phone) " +
                      " values (" + newBank + ", " +
                      Utils.EStrNull(finder.nzp_payer.ToString(CultureInfo.InvariantCulture)) + ", " +
                      Utils.EStrNull(finder.short_name) + ", " +
                      Utils.EStrNull(finder.adress) + ", " +
                      Utils.EStrNull(finder.phone) + ")";
            }

            if ((isNew) && (finder.nzp_bank > 0))
            {
                string sqlKodBank = " select count(*) cnt from " + table +
                                      " where nzp_bank = " + finder.nzp_bank;
                object countKodBank = ExecScalar(connDB, sqlKodBank, out ret, true);
                if (!ret.result)
                {
                    connDB.Close();
                    ret.tag = 0;
                    return ret;
                }
                if (Convert.ToInt32(countKodBank) > 0)
                {
                    ret.result = false;
                    ret.text = "Неверный код - такой код уже определен";
                    ret.tag = -1;
                    connDB.Close();
                    return ret;
                }
            }

            ret = ExecSQL(connDB, sql, true);
            if (ret.result && finder.nzp_bank == 0)
            {
                ret.tag = GetSerialValue(connDB);
            }

            connDB.Close();
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
            IDbConnection connWeb = GetConnection(Constants.cons_Webdata);
            Returns ret = OpenDb(connWeb, true);
            if (!ret.result) return ret;

            string srv = "";
            if (nzp_server > 0)
                srv = "_" + nzp_server;

            string payer = "s_payer" + srv;
            string payerFull = DBManager.GetFullBaseName(connWeb) +
                DBManager.tableDelimiter + payer;

            CreateWebSupplier(connWeb, payer, out ret);
            connWeb.Close();

            if (is_insert)
            {
                IDbConnection connDB = GetConnection(Constants.cons_Kernel);
                ret = OpenDb(connDB, true);
                if (!ret.result)
                {
                    return ret;
                }

                string sql =
                    " Insert into " + payerFull + " ( nzp_payer, payer, npayer, nzp_supp, is_erc ) " +
                    " Select nzp_payer, payer, npayer, nzp_supp, is_erc " +
                    " From " + Points.Pref + DBManager.sKernelAliasRest + "s_payer";
                //выбрать список
                ret = ExecSQL(connDB, sql, true);

                connDB.Close();
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

            string connectionString = Points.GetConnByPref(finder.pref);
            IDbConnection connDB = GetConnection(connectionString);
            ret = OpenDb(connDB, true);
            if (!ret.result) return null;

            MyDataReader reader = null;

            var list = new List<Payer>();

            var tables = new DbTables(DBManager.getServer(connDB));
            try
            {
                string sql = "select nzp_payer_type, type_name " +
                             " from " + tables.payer_types + " order by  type_name";
                ret = ExecRead(connDB, out reader, sql, true);
                if (!ret.result)
                {
                    connDB.Close();
                    return null;
                }

                while (reader.Read())
                {
                    var p = new Payer();
                    if (reader["nzp_payer_type"] != DBNull.Value)
                        p.nzp_type = Convert.ToInt32(reader["nzp_payer_type"]);
                    if (reader["type_name"] != DBNull.Value) p.type_name = Convert.ToString(reader["type_name"]);
                    list.Add(p);
                }
                reader.Close();
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;

                MonitorLog.WriteLog("Ошибка получения LoadPayerTypes " + err, MonitorLog.typelog.Error, 20, 201, true);
                return null;
            }
            finally
            {
                if (reader != null) reader.Close();
                if (connDB.State == ConnectionState.Open) connDB.Close();
            }
            return list;
        }

        public Returns ContrRenameDog(Payer finder)
        {
            Returns ret = Utils.InitReturns();
            if (finder.nzp_user <= 0)
            {
                ret.result = false;
                ret.text = "Не задан пользователь";
                return ret;
            }

            string connectionString = Points.GetConnByPref(finder.pref);
            IDbConnection connDB = GetConnection(connectionString);
            ret = OpenDb(connDB, true);
            if (!ret.result) return ret;

            try
            {
                string sql = " UPDATE " + Points.Pref + sKernelAliasRest + "supplier" +
                             " SET (name_supp, changed_by) =" +
                             " ((SELECT (trim(substr(p1.payer,1,32))" +// принципал
                             "||'/'||trim(substr(p2.payer,1,32))" + // поставщик
                             "||case when p4.nzp_payer>0 then '/'||trim(substr(p4.payer,1,32)) else '' end" +// подрядчик
                             "||'/'||trim(substr(p3.payer,1,32)) )" +// агент
                             "  FROM  " + Points.Pref + sKernelAliasRest + "s_payer p1," +
                                Points.Pref + sKernelAliasRest + "s_payer p2," +
                                Points.Pref + sKernelAliasRest + "s_payer p3, " +
                                Points.Pref + sKernelAliasRest + "s_payer p4 " +
                             "  WHERE p1.nzp_payer = " + Points.Pref + sKernelAliasRest + "supplier.nzp_payer_princip" +
                             "  AND p2.nzp_payer = " + Points.Pref + sKernelAliasRest + "supplier.nzp_payer_supp" +
                             "  AND p3.nzp_payer = " + Points.Pref + sKernelAliasRest + "supplier.nzp_payer_agent " +
                             "  AND p4.nzp_payer = " + sNvlWord + "(" + Points.Pref + sKernelAliasRest + "supplier.nzp_payer_podr,0))," +
                             finder.nzp_user + ")" +
                             " WHERE (nzp_payer_agent = " + finder.nzp_payer + " OR " +
                             " nzp_payer_supp = " + finder.nzp_payer + " OR " +
                             " nzp_payer_princip = " + finder.nzp_payer + " OR " +
                             " nzp_payer_podr = " + finder.nzp_payer + ")";
                ret = ExecSQL(connDB, sql, true);
                if (!ret.result) return ret;

                foreach (var point in Points.PointList)
                {
                    sql = " UPDATE " + point.pref + sKernelAliasRest + "supplier" +
                          " SET (name_supp, changed_by) =" +
                          " ((SELECT (trim(substr(p1.payer,1,32))" +// принципал
                          "||'/'||trim(substr(p2.payer,1,32)) " +// поставщик
                          "||case when p4.nzp_payer>0 then '/'||trim(substr(p4.payer,1,32)) else '' end" +// подрядчик
                          "||'/'||trim(substr(p3.payer,1,32))) " +// агент
                          "  FROM  " + Points.Pref + sKernelAliasRest + "s_payer p1," +
                          Points.Pref + sKernelAliasRest + "s_payer p2," +
                          Points.Pref + sKernelAliasRest + "s_payer p3," +
                          Points.Pref + sKernelAliasRest + "s_payer p4 " +
                          "  WHERE p1.nzp_payer = " + point.pref + sKernelAliasRest + "supplier.nzp_payer_princip" +
                          "  AND p2.nzp_payer = " + point.pref + sKernelAliasRest + "supplier.nzp_payer_supp" +
                          "  AND p3.nzp_payer = " + point.pref + sKernelAliasRest + "supplier.nzp_payer_agent " +
                          "  AND p4.nzp_payer = " + sNvlWord + "(" + point.pref + sKernelAliasRest + "supplier.nzp_payer_podr,0))," +
                             finder.nzp_user + ")" +
                          " WHERE (nzp_payer_agent = " + finder.nzp_payer + " OR " +
                          " nzp_payer_supp = " + finder.nzp_payer + " OR " +
                          " nzp_payer_princip = " + finder.nzp_payer + " OR " +
                          " nzp_payer_podr = " + finder.nzp_payer + ")";
                    ret = ExecSQL(connDB, sql, true);
                    if (!ret.result) return ret;
                }
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;

                MonitorLog.WriteLog("Ошибка  ContrRenameDog " + err + " " + ex.StackTrace, MonitorLog.typelog.Error, 20, 201, true);
            }
            finally
            {
                if (connDB.State == ConnectionState.Open) connDB.Close();
            }
            return ret;
        }
        //----------------------------------------------------------------------
        public List<Payer> PayerBankLoad(Payer finder, enSrvOper oper, out Returns ret)
        //----------------------------------------------------------------------
        {
            if (oper != enSrvOper.Bank &&
                oper != enSrvOper.Payer &&
                oper != enSrvOper.Agent &&
                oper != enSrvOper.Supplier &&
                oper != enSrvOper.Principal &&
                oper != enSrvOper.BankForSaldoPoPerechisl &&
                oper != enSrvOper.PayerReferencedFromBank)
            {
                ret = new Returns(false, "Неверные входные параметры");
                return null;
            }



            var spis = new List<Payer>();

            IDbConnection connDB = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(connDB, true);
            if (!ret.result) return null;

            var tables = new DbTables(DBManager.getServer(connDB));

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

            string orderBy = "";
            string filtrOnDistrib;
            string where = wherePayer + whereSupp;

            ExecSQL(connDB, "drop table t_payers", false);

            string sql = "Create temp table t_payers(nzp_payer integer, " +
                         " payer char(200), " +
                         " npayer char(200), " +
                         " id_bc_type integer default 0, " +
                         " nzp_supp integer default 0," +
                         " name_supp char(100), " +
                         " is_erc integer, " +
                         " kpp char(9)," +
                         " inn char(12)," +
                         " nzp_type integer, " +
                         " type_name char(50), " +
                         " is_bank integer default 0, " +
                         " nzp_bank integer, " +
                         " bank char(40)," +
                         " short_name char(40), " +
                         " adress char(100)," +
                         " phone char(20))" + DBManager.sUnlogTempTable;
            ExecSQL(connDB, sql, true);

            switch (oper)
            {
                case enSrvOper.Bank:
                    {
                        //фильтровать справочники по fn_distrib
                        filtrOnDistrib = FiltrOnDistrib("p.nzp_payer", "nzp_bank", finder.dopFind);
                        wherePayer += filtrOnDistrib;




                        sql = " insert into  t_payers(nzp_payer, payer, id_bc_type, npayer, nzp_supp, name_supp," +
                              " is_erc, inn, kpp, nzp_type, type_name, is_bank, nzp_bank, bank, short_name, " +
                              " adress, phone) " +
                            " " +
                              " Select  p.nzp_payer, p.payer, 0 as id_bc_type, p.npayer, p.nzp_supp, s.name_supp, p.is_erc, p.inn, p.kpp, p.nzp_type, pt.type_name " +
                                ",  case when " + sNvlWord + "(b.nzp_payer,0) > 0 then 1 else 0 end  as is_bank, b.nzp_bank, b.bank, b.short_name, b.adress, b.phone " +
                            " From " + tables.bank + " b";
                        if (where == "")
                        {
                            sql += " left outer join " + tables.payer + " p on p.nzp_payer = b.nzp_payer "
                                + "left outer join " + tables.supplier + " s on p.nzp_supp = s.nzp_supp "
                                + "left outer join " + tables.payer_types + " pt on p.nzp_type = pt.nzp_payer_type ";
                        }
                        else if (wherePayer != "" && whereSupp != "")
                        {
                            sql += ", " + tables.supplier + " s, " +
                                   tables.payer + " p left outer join " + tables.payer_types +
                                   " pt on p.nzp_type = pt.nzp_payer_type" +
                                " Where p.nzp_payer = b.nzp_payer and p.nzp_supp = s.nzp_supp " + where;
                        }
                        else
                        {
                            sql += ", " + tables.payer + " p left outer join " + tables.supplier + " s on p.nzp_supp = s.nzp_supp left outer join " + tables.payer_types + " pt on p.nzp_type = pt.nzp_payer_type" +
                                " Where p.nzp_payer = b.nzp_payer " + where;
                        }


                        if (finder.bank != "") where += " and upper(b.bank) like upper('%" + finder.bank + "%') ";
                        //sql += " Where p.nzp_payer = b.nzp_payer " +
                        //       " and p.nzp_supp = s.nzp_supp " +
                        //       " and p.nzp_type = pt.nzp_payer_type " + where;

                        orderBy = " Order by bank, payer";
                    }
                    break;


                case enSrvOper.BankForSaldoPoPerechisl:
                    //фильтровать справочники по fn_distrib
                    /*     filtrOnDistrib = FiltrOnDistrib("p.nzp_payer", "nzp_bank", finder.dopFind);
                         where += filtrOnDistrib;*/

                    sql = "insert into t_payers(nzp_bank, bank, nzp_payer, payer, id_bc_type, " +
                          " npayer, nzp_supp, name_supp,  is_erc, inn, kpp, nzp_type, type_name, " +
                          " is_bank, short_name,  adress,  phone )" +
                          " select p.nzp_payer as nzp_bank, p.payer as bank, p.nzp_payer, p.payer, 0 as id_bc_type, " +
                        " p.npayer, p.nzp_supp, '' as name_supp,  p.is_erc, p.inn, p.kpp, p.nzp_type, '' as type_name, " +
                          " 0 as is_bank, '' as short_name,  '' as adress,  '' as phone  " +
                          " from " + tables.payer + " p " +
                          " where p.nzp_payer in " +
                        " (select b.nzp_payer from " + tables.bank + " b)";

                    if (finder.bank != "") sql += " and upper(p.payer) like upper('%" + finder.bank + "%') ";

                    orderBy = " Order by payer";
                    break;
                case enSrvOper.Agent:
                    sql = "insert into t_payers(nzp_bank, bank, nzp_payer, payer, id_bc_type, " +
                          " npayer, nzp_supp, name_supp,  is_erc, inn, kpp, nzp_type, type_name, " +
                          " is_bank, short_name,  adress,  phone )" +
                          " select p.nzp_payer as nzp_bank, p.payer as bank, p.nzp_payer, p.payer, 0 as id_bc_type, " +
                          " p.npayer, p.nzp_supp, '' as name_supp,  p.is_erc, p.inn, p.kpp, p.nzp_type, '' as type_name, " +
                          " 0 as is_bank, '' as short_name,  '' as adress,  '' as phone  " +
                          " from " + tables.payer + " p " +
                          " where p.nzp_payer in " +
                          " (select b.nzp_payer_agent from " + tables.supplier + " b ";
                    if (finder.nzp_wp > 0)
                    {
                        sql += ",  " + Points.Pref + sDataAliasRest + "fn_scope fs " +
                            ",  " + Points.Pref + sDataAliasRest + "fn_scope_adres fsa ";
                    }

                    sql += "  where nzp_payer_agent is not null " + whereSupp.Replace("p.", "b.");

                    if (finder.nzp_wp > 0)
                    {
                        sql += " and b.nzp_scope=fs.nzp_scope and fs.nzp_scope=fsa.nzp_scope and fsa.nzp_wp = " + finder.nzp_wp;
                    }
                    sql += ")";

                    if (finder.bank != "") sql += " and upper(p.payer) like upper('%" + finder.bank + "%') ";

                    orderBy = " Order by payer";
                    break;
                case enSrvOper.Supplier:
                    sql = "insert into t_payers(nzp_bank, bank, nzp_payer, payer, id_bc_type, " +
                          " npayer, nzp_supp, name_supp,  is_erc, inn, kpp, nzp_type, type_name, " +
                          " is_bank, short_name,  adress,  phone )" +
                          " select p.nzp_payer as nzp_bank, p.payer as bank, p.nzp_payer, p.payer, 0 as id_bc_type, " +
                          " p.npayer, p.nzp_supp, '' as name_supp,  p.is_erc, p.inn, p.kpp, p.nzp_type, '' as type_name, " +
                          " 0 as is_bank, '' as short_name,  '' as adress,  '' as phone  " +
                          " from " + tables.payer + " p " +
                          " where p.nzp_payer in " +
                          " (select b.nzp_payer_supp from " + tables.supplier + " b ";
                    if (finder.nzp_wp > 0)
                    {
                        sql += ",  " + Points.Pref + sDataAliasRest + "fn_scope fs " + 
                            ",  " + Points.Pref + sDataAliasRest + "fn_scope_adres fsa ";
                    }

                    sql += "  where nzp_payer_supp is not null " + whereSupp.Replace("p.", "b.");

                    if (finder.nzp_wp > 0)
                    {
                        sql += " and b.nzp_scope=fs.nzp_scope and fs.nzp_scope=fsa.nzp_scope and fsa.nzp_wp = " + finder.nzp_wp;
                    }
                    sql += ")";

                    if (finder.bank != "") sql += " and upper(p.payer) like upper('%" + finder.bank + "%') ";

                    orderBy = " Order by payer";
                    break;
                case enSrvOper.Principal:
                    sql = "insert into t_payers(nzp_bank, bank, nzp_payer, payer, id_bc_type, " +
                          " npayer, nzp_supp, name_supp,  is_erc, inn, kpp, nzp_type, type_name, " +
                          " is_bank, short_name,  adress,  phone )" +
                          " select p.nzp_payer as nzp_bank, p.payer as bank, p.nzp_payer, p.payer, 0 as id_bc_type, " +
                          " p.npayer, p.nzp_supp, '' as name_supp,  p.is_erc, p.inn, p.kpp, p.nzp_type, '' as type_name, " +
                          " 0 as is_bank, '' as short_name,  '' as adress,  '' as phone  " +
                          " from " + tables.payer + " p " +
                          " where p.nzp_payer in " +
                          " (select b.nzp_payer_princip from " + tables.supplier + " b";
                    if (finder.nzp_wp > 0)
                    {
                        sql += ",  " + Points.Pref + sDataAliasRest + "fn_scope fs " +
                            ",  " + Points.Pref + sDataAliasRest + "fn_scope_adres fsa ";
                    }

                    sql += "  where nzp_payer_princip is not null " + whereSupp.Replace("p.", "b.");
                    if (finder.nzp_wp > 0)
                    {
                        sql += " and b.nzp_scope=fs.nzp_scope and fs.nzp_scope=fsa.nzp_scope and fsa.nzp_wp = " + finder.nzp_wp;
                    }
                    sql += ")";

                    if (finder.bank != "") sql += " and upper(p.payer) like upper('%" + finder.bank + "%') ";

                    orderBy = " Order by payer";
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
                            where += " and " + DBManager.sNvlWord + "(p.nzp_type,0)<>" + finder.exclude_types[i];
                        }
                    }
                    if (finder.include_types != null)
                    {
                        string usl = "";
                        for (int i = 0; i < finder.include_types.Count; i++)
                        {
                            if (i == 0) usl = finder.include_types[i].ToString(CultureInfo.InvariantCulture);
                            else usl += "," + finder.include_types[i];
                        }
                        if (usl != "")
                        {

                            where += " and " + DBManager.sNvlWord + "(p.nzp_type,0) in (" + usl + ")";
                        }
                    }


                    sql = " insert into t_payers (nzp_payer, payer, npayer, nzp_supp, id_bc_type, name_supp," +
                          " is_erc, inn, kpp, nzp_type, type_name, is_bank, nzp_bank, bank, short_name, " +
                          " adress,  phone) " +
                          " Select p.nzp_payer, p.payer, p.npayer, p.nzp_supp, p.id_bc_type, s.name_supp, " +
                          "        p.is_erc, p.inn, p.kpp, p.nzp_type, pt.type_name, " +
                          "        0 is_bank, 0 nzp_bank, '' bank, '' short_name, " +
                          "        '' adress, '' phone " +
                          " From " + tables.payer + " p left outer join " + tables.supplier + " s on p.nzp_supp = s.nzp_supp " +
                          " left outer join " + tables.payer_types + " pt on p.nzp_type = pt.nzp_payer_type" +
                            (string.IsNullOrEmpty(where) ? "" : (" Where 1=1 " + where));
                    orderBy = " Order by payer";
                    break;

                case enSrvOper.PayerReferencedFromBank:
                    //фильтровать справочники по fn_distrib
                    filtrOnDistrib = FiltrOnDistrib("p.nzp_payer", "nzp_payer", finder.dopFind);
                    where += filtrOnDistrib;

                    sql =
                        "insert into t_payers(nzp_payer, payer,npayer,nzp_supp, inn,  id_bc_type, kpp, nzp_type, type_name, name_supp,is_erc, " +
                        "   is_bank, nzp_bank, bank, " +
                        "  short_name,  adress,  phone )" +
 " Select distinct p.nzp_payer, p.payer, p.npayer, p.nzp_supp, p.inn, 0 as id_bc_type, p.kpp, p.nzp_type, pt.type_name, s.name_supp, " +
                        " p.is_erc, 0 is_bank, b.nzp_bank, b.bank, b.short_name, b.adress, b.phone " +
                        " From " + tables.bank + " b, " + tables.payer + " p  " +
                        " left outer join " + tables.supplier + " s on p.nzp_supp = s.nzp_supp " +
                        " left outer join " + tables.payer_types + " pt on p.nzp_type = pt.nzp_payer_type " +
                        " Where p.nzp_payer = b.nzp_payer  " + where;


                    orderBy = " Order by nzp_payer,payer";
                    break;
            }

            ret = ExecSQL(connDB, sql, true);
            if (!ret.result)
            {
                connDB.Close();
                return null;
            }

            sql = "select * from t_payers "
                + orderBy;

            MyDataReader reader;
            ret = ExecRead(connDB, out reader, sql, true);
            if (!ret.result)
            {
                connDB.Close();
                return null;
            }
            try
            {
                int i = 0;
                while (reader.Read())
                {
                    i++;
                    if (i <= finder.skip) continue;
                    var zap = new Payer();

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
                object count = ExecScalar(connDB, sql, out ret, true);
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

                ExecSQL(connDB, "drop table t_payers", false);

                connDB.Close();
                return spis;
            }
            catch (Exception ex)
            {
                reader.Close();
                connDB.Close();

                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;

                MonitorLog.WriteLog("Ошибка заполнения списка подрядчиков " + err, MonitorLog.typelog.Error, 20, 201, true);

                return null;
            }
        }

        public List<Payer> BankPayerLoad(Payer finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            if (finder.nzp_user <= 0)
            {
                ret.result = false;
                ret.text = "Не задан пользователь";
                return null;
            }

            string connectionString = Points.GetConnByPref(finder.pref);
            IDbConnection connDB = GetConnection(connectionString);
            ret = OpenDb(connDB, true);
            if (!ret.result) return null;

            MyDataReader reader = null;

            var list = new List<Payer>();

            var tables = new DbTables(DBManager.getServer(connDB));
            try
            {
                string sql =
                    "select a.nzp_bank, a.bank || coalesce(' / '||p.payer,'') as bank from " + Points.Pref + "_kernel" + tableDelimiter + "s_bank a  left outer join " + Points.Pref + "_kernel" + tableDelimiter + "s_payer p on a.nzp_payer=p.nzp_payer";
                ret = ExecRead(connDB, out reader, sql, true);
                if (!ret.result)
                {
                    connDB.Close();
                    return null;
                }

                while (reader.Read())
                {
                    var p = new Payer();
                    if (reader["nzp_bank"] != DBNull.Value) p.nzp_bank = Convert.ToInt32(reader["nzp_bank"]);
                    if (reader["bank"] != DBNull.Value) p.bank = Convert.ToString(reader["bank"]);
                    list.Add(p);
                }
                reader.Close();
                return list;
            }
            catch (Exception ex)
            {
                if (reader != null) reader.Close();
                connDB.Close();

                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;

                MonitorLog.WriteLog("Ошибка получения банков " + err, MonitorLog.typelog.Error, 20, 201, true);
                return null;
            }
            //ret = new Returns();
            //return null;
        }


        public void PayerBankForIssrpF101(out Returns ret)
        {
            IDbConnection connDb = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(connDb, true);
            if (!ret.result) return;

            var sqlBuilder = new StringBuilder();

            int count = 0;
            foreach (var point in Points.PointList)
            {
                //временная заглушка только для одного района
                //todo убрать!
                if (point.pref != "baikal") continue;

                var period = new DateTime(point.CalcMonth.year_, point.CalcMonth.month_, 1).AddMonths(-1);
                if (count > 0)
                {
                    sqlBuilder.AppendLine("union all");
                }
                sqlBuilder.AppendFormat(@"
select
fb.nzp_fb,
ch.nzp_serv,
s.service_small,
'recipient:'||p.payer||'$inn:'||coalesce(p.inn,0::text)||'kpp:'||coalesce(p.kpp,0::text)||'$bankaccount:'||coalesce(fb.rcount,0::text)||'$bankname:'||coalesce(fb.bank_name,'')||'$bic:'||coalesce(fb.bik,0::text)||'$bankcorr:'||coalesce(fb.kcount,0::text) as banking

from {0}_charge_{1:00}.charge_{2:00} ch
inner join {0}_kernel.supplier sp on ch.nzp_supp = sp.nzp_supp
inner join {0}_kernel.services s on ch.nzp_serv = s.nzp_serv
inner join {3}{4}fn_bank fb on sp.nzp_payer_supp = fb.nzp_payer
inner join {3}{5}.s_payer p on sp.nzp_payer_supp = p.nzp_payer
where ch.dat_charge is null and ch.nzp_serv > 1

group by 1,2,3,4
                ", point.pref, period.Year % 100, period.Month, Points.Pref, sDataAliasRest, sKernelAliasRest);
                count++;

            }

            MyDataReader reader;
            ret = ExecRead(connDb, out reader, sqlBuilder.ToString(), true);
            if (!ret.result)
            {
                connDb.Close();
                return;
            }
            var date = DateTime.Now;
            string fileNameIn = string.Format("reestr-depts-{0}{1:00}{2:00}.csv", date.Year, date.Month, date.Day);
            string dir = Path.Combine(Constants.ExcelDir, fileNameIn);

            FileStream memstr = new FileStream(dir, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
            StreamWriter writer = new StreamWriter(memstr, Encoding.GetEncoding(1251));
            var strBuilder = new StringBuilder();
            try
            {

                string line = "";
                int i = 0;
                while (reader.Read())
                {
                    line = string.Format("{0};{1};{2};{3}",
                        reader["nzp_fb"] != DBNull.Value ? Convert.ToString(reader["nzp_fb"]).Trim() : "",
                        reader["nzp_serv"] != DBNull.Value ? Convert.ToString(reader["nzp_serv"]).Trim() : "",
                        reader["service_small"] != DBNull.Value ? Convert.ToString(reader["service_small"]).Trim() : "",
                        reader["banking"] != DBNull.Value ? Convert.ToString(reader["banking"]).Trim() : ""
                    );
                    writer.WriteLine(line);
                    strBuilder.AppendLine(line);
                }
                writer.Flush();
                writer.Close();
                memstr.Close();
                reader.Close();
                connDb.Close();

                if (InputOutput.useFtp)
                {
                    fileNameIn = InputOutput.SaveOutputFile(dir);
                }
                //todo вызываем метод вэб-сервиса
                var dcId = 111;
                var rDate = DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss");
                var token = Utils.CreateMD5StringHash(dcId + rDate + "testsecretword");

                using (WebClient client = new WebClient())
                {
                    System.Collections.Specialized.NameValueCollection reqparm = new System.Collections.Specialized.NameValueCollection();
                    reqparm.Add("DC_ID", dcId.ToString());
                    reqparm.Add("FILE_DATA", strBuilder.ToString());
                    reqparm.Add("R_DATE", rDate);
                    reqparm.Add("TOKEN", token);
                    byte[] responsebytes = client.UploadValues("http://10.2.10.96/webapi/UploadRekvisiti", "POST", reqparm);
                    string responsebody = Encoding.UTF8.GetString(responsebytes);
                }
            }
            catch (Exception ex)
            {
                reader.Close();
                connDb.Close();

                writer.Flush();
                writer.Close();
                memstr.Close();

                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;

                MonitorLog.WriteLog("Ошибка PayerBankForIssrpF101() " + err, MonitorLog.typelog.Error, 20, 201, true);

                return;
            }
        }


        public List<Payer> PayerBankLoadContract(Payer finder, enSrvOper oper, out Returns ret)
        //----------------------------------------------------------------------
        {
            if (oper != enSrvOper.Bank &&
                oper != enSrvOper.Payer &&
                oper != enSrvOper.Agent &&
                oper != enSrvOper.Supplier &&
                oper != enSrvOper.Principal &&
                oper != enSrvOper.BankForSaldoPoPerechisl &&
                oper != enSrvOper.PayerReferencedFromBank)
            {
                ret = new Returns(false, "Неверные входные параметры");
                return null;
            }



            var spis = new List<Payer>();

            IDbConnection connDB = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(connDB, true);
            if (!ret.result) return null;

            var tables = new DbTables(DBManager.getServer(connDB));

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

            string orderBy = "";
            string filtrOnDistrib;
            string where = wherePayer + whereSupp;

            ExecSQL(connDB, "drop table t_payers", false);

            string sql = "Create temp table t_payers(nzp_payer integer, " +
                         " payer char(200), " +
                         " npayer char(200), " +
                         " id_bc_type integer default 0, " +
                         " nzp_supp integer default 0," +
                         " name_supp char(100), " +
                         " is_erc integer, " +
                         " kpp char(9)," +
                         " inn char(12)," +
                         " nzp_type integer, " +
                         " type_name char(50), " +
                         " is_bank integer default 0, " +
                         " nzp_bank integer, " +
                         " bank char(40)," +
                         " short_name char(40), " +
                         " adress char(100)," +
                         " phone char(20))" + DBManager.sUnlogTempTable;
            ExecSQL(connDB, sql, true);

            switch (oper)
            {
                case enSrvOper.Bank:
                    {
                        //фильтровать справочники по fn_distrib
                        filtrOnDistrib = FiltrOnDistrib("p.nzp_payer", "nzp_bank", finder.dopFind);
                        wherePayer += filtrOnDistrib;




                        sql = " insert into  t_payers(nzp_payer, payer, id_bc_type, npayer, nzp_supp, name_supp," +
                              " is_erc, inn, kpp, nzp_type, type_name, is_bank, nzp_bank, bank, short_name, " +
                              " adress, phone) " +
                            " " +
                              " Select  p.nzp_payer, p.payer, 0 as id_bc_type, p.npayer, p.nzp_supp, s.name_supp, p.is_erc, p.inn, p.kpp, p.nzp_type, pt.type_name " +
                                ",  case when " + sNvlWord + "(b.nzp_payer,0) > 0 then 1 else 0 end  as is_bank, b.nzp_bank, b.bank, b.short_name, b.adress, b.phone " +
                            " From " + tables.bank + " b";
                        if (where == "")
                        {
                            sql += " left outer join " + tables.payer + " p on p.nzp_payer = b.nzp_payer "
                                + "left outer join " + tables.supplier + " s on p.nzp_supp = s.nzp_supp "
                                + "left outer join " + tables.payer_types + " pt on p.nzp_type = pt.nzp_payer_type ";
                        }
                        else if (wherePayer != "" && whereSupp != "")
                        {
                            sql += ", " + tables.supplier + " s, " +
                                   tables.payer + " p left outer join " + tables.payer_types +
                                   " pt on p.nzp_type = pt.nzp_payer_type" +
                                " Where p.nzp_payer = b.nzp_payer and p.nzp_supp = s.nzp_supp " + where;
                        }
                        else
                        {
                            sql += ", " + tables.payer + " p left outer join " + tables.supplier + " s on p.nzp_supp = s.nzp_supp left outer join " + tables.payer_types + " pt on p.nzp_type = pt.nzp_payer_type" +
                                " Where p.nzp_payer = b.nzp_payer " + where;
                        }


                        if (finder.bank != "") where += " and upper(b.bank) like upper('%" + finder.bank + "%') ";
                        //sql += " Where p.nzp_payer = b.nzp_payer " +
                        //       " and p.nzp_supp = s.nzp_supp " +
                        //       " and p.nzp_type = pt.nzp_payer_type " + where;

                        orderBy = " Order by bank, payer";
                    }
                    break;


                case enSrvOper.BankForSaldoPoPerechisl:
                    //фильтровать справочники по fn_distrib
                    /*     filtrOnDistrib = FiltrOnDistrib("p.nzp_payer", "nzp_bank", finder.dopFind);
                         where += filtrOnDistrib;*/

                    sql = "insert into t_payers(nzp_bank, bank, nzp_payer, payer, id_bc_type, " +
                          " npayer, nzp_supp, name_supp,  is_erc, inn, kpp, nzp_type, type_name, " +
                          " is_bank, short_name,  adress,  phone )" +
                          " select p.nzp_payer as nzp_bank, p.payer as bank, p.nzp_payer, p.payer, 0 as id_bc_type, " +
                        " p.npayer, p.nzp_supp, '' as name_supp,  p.is_erc, p.inn, p.kpp, p.nzp_type, '' as type_name, " +
                          " 0 as is_bank, '' as short_name,  '' as adress,  '' as phone  " +
                          " from " + tables.payer + " p " +
                          " where p.nzp_payer in " +
                        " (select b.nzp_payer from " + tables.bank + " b)";

                    if (finder.bank != "") sql += " and upper(p.payer) like upper('%" + finder.bank + "%') ";

                    orderBy = " Order by payer";
                    break;
                case enSrvOper.Agent:
                    sql = "insert into t_payers(nzp_bank, bank, nzp_payer, payer, id_bc_type, " +
                          " npayer, nzp_supp, name_supp,  is_erc, inn, kpp, nzp_type, type_name, " +
                          " is_bank, short_name,  adress,  phone )" +
                          " select p.nzp_payer as nzp_bank, p.payer as bank, p.nzp_payer, p.payer, 0 as id_bc_type, " +
                          " p.npayer, p.nzp_supp, '' as name_supp,  p.is_erc, p.inn, p.kpp, p.nzp_type, '' as type_name, " +
                          " 0 as is_bank, '' as short_name,  '' as adress,  '' as phone  " +
                          " from " + tables.payer + " p " +
                          " where p.nzp_payer in " +
                          " (select b.nzp_payer_agent from " + tables.supplier + " b ";
                    if (finder.nzp_wp > 0)
                        sql += ",  " + tables.supplier + "_point sp ";

                    sql += "  where nzp_payer_agent is not null";
                    if (finder.nzp_wp > 0) sql += " and b.nzp_supp=sp.nzp_supp and sp.nzp_wp = " + finder.nzp_wp;
                    sql += ")";

                    if (finder.bank != "") sql += " and upper(p.payer) like upper('%" + finder.bank + "%') ";

                    orderBy = " Order by payer";
                    break;
                case enSrvOper.Supplier:
                    sql = "insert into t_payers(nzp_bank, bank, nzp_payer, payer, id_bc_type, " +
                          " npayer, nzp_supp, name_supp,  is_erc, inn, kpp, nzp_type, type_name, " +
                          " is_bank, short_name,  adress,  phone )" +
                          " select p.nzp_payer as nzp_bank, p.payer as bank, p.nzp_payer, p.payer, 0 as id_bc_type, " +
                          " p.npayer, p.nzp_supp, '' as name_supp,  p.is_erc, p.inn, p.kpp, p.nzp_type, '' as type_name, " +
                          " 0 as is_bank, '' as short_name,  '' as adress,  '' as phone  " +
                          " from " + tables.payer + " p " +
                          " where p.nzp_payer in " +
                          " (select b.nzp_payer_supp from " + tables.supplier + " b ";
                    if (finder.nzp_wp > 0)
                        sql += ",  " + tables.supplier + "_point sp ";

                    sql += "  where nzp_payer_supp is not null";
                    if (finder.nzp_wp > 0) sql += " and b.nzp_supp=sp.nzp_supp and sp.nzp_wp = " + finder.nzp_wp;
                    sql += ")";

                    if (finder.bank != "") sql += " and upper(p.payer) like upper('%" + finder.bank + "%') ";

                    orderBy = " Order by payer";
                    break;
                case enSrvOper.Principal:
                    sql = "insert into t_payers(nzp_bank, bank, nzp_payer, payer, id_bc_type, " +
                          " npayer, nzp_supp, name_supp,  is_erc, inn, kpp, nzp_type, type_name, " +
                          " is_bank, short_name,  adress,  phone )" +
                          " select p.nzp_payer as nzp_bank, p.payer as bank, p.nzp_payer, p.payer, 0 as id_bc_type, " +
                          " p.npayer, p.nzp_supp, '' as name_supp,  p.is_erc, p.inn, p.kpp, p.nzp_type, '' as type_name, " +
                          " 0 as is_bank, '' as short_name,  '' as adress,  '' as phone  " +
                          " from " + tables.payer + " p " +
                          " where p.nzp_payer in " +
                          " (select b.nzp_payer_princip from " + tables.supplier + " b";
                    if (finder.nzp_wp > 0)
                        sql += ",  " + tables.supplier + "_point sp ";

                    sql += "  where nzp_payer_princip is not null";
                    if (finder.nzp_wp > 0) sql += " and b.nzp_supp=sp.nzp_supp and sp.nzp_wp = " + finder.nzp_wp;
                    sql += ")";



                    if (finder.bank != "") sql += " and upper(p.payer) like upper('%" + finder.bank + "%') ";

                    orderBy = " Order by payer";
                    break;
                case enSrvOper.Payer:
                    //фильтровать справочники по fn_distrib
                    filtrOnDistrib = FiltrOnDistrib("nzp_payer", finder.dopFind);
                    where += filtrOnDistrib;
                    string whereexcl = "", whereincl = "";
                    //фильтровать по типу контрагентов
                    if (finder.exclude_types != null)
                    {
                        for (int i = 0; i < finder.exclude_types.Count; i++)
                        {
                            whereexcl += " and " + DBManager.sNvlWord + "(pt.nzp_payer_type,0)<>" + finder.exclude_types[i];
                        }

                    }
                    if (finder.include_types != null)
                    {
                        string usl = "";
                        for (int i = 0; i < finder.include_types.Count; i++)
                        {
                            if (i == 0) usl = finder.include_types[i].ToString(CultureInfo.InvariantCulture);
                            else usl += "," + finder.include_types[i];
                        }
                        if (usl != "")
                        {

                            whereincl += " and " + DBManager.sNvlWord + "(pt.nzp_payer_type,0) in (" + usl + ")";
                        }
                    }
                    if (finder.exclude_types == null && finder.include_types == null)
                    {
                        sql = " insert into t_payers (nzp_payer, payer, npayer, nzp_supp, id_bc_type, name_supp," +
                              " is_erc, inn, kpp, nzp_type, type_name, is_bank, nzp_bank, bank, short_name, " +
                              " adress,  phone) " +
                              " Select p.nzp_payer, p.payer, p.npayer, p.nzp_supp, p.id_bc_type, s.name_supp, " +
                              "        p.is_erc, p.inn, p.kpp, p.nzp_type, pt.type_name, " +
                              "        0 is_bank, 0 nzp_bank, '' bank, '' short_name, " +
                              "        '' adress, '' phone " +
                              " From " + tables.payer + " p left outer join " + tables.supplier + " s on p.nzp_supp = s.nzp_supp " +
                              " left outer join " + tables.payer_types + " pt on p.nzp_type = pt.nzp_payer_type" +
                                (string.IsNullOrEmpty(where) ? "" : (" Where 1=1 " + where));
                        orderBy = " Order by payer";
                    }
                    else
                    {
                        if (finder.exclude_types != null)
                        {
                            where += " and p.nzp_payer not in (select pt.nzp_payer from " + tables.payertypes + " pt  where " +
                                 " p.nzp_payer = pt.nzp_payer " + whereexcl + ")";
                        }

                        if (finder.include_types != null)
                        {
                            where += " and p.nzp_payer in (select pt.nzp_payer from " + tables.payertypes + " pt  where " +
                                 " p.nzp_payer = pt.nzp_payer " + whereincl + ")";
                        }

                        sql = " insert into t_payers (nzp_payer, payer, npayer, nzp_supp, id_bc_type, name_supp," +
                              " is_erc, inn, kpp, nzp_type, type_name, is_bank, nzp_bank, bank, short_name, " +
                              " adress,  phone) " +
                              " Select p.nzp_payer, p.payer, p.npayer, p.nzp_supp, p.id_bc_type, '' name_supp, " +
                              "        p.is_erc, p.inn, p.kpp, p.nzp_type,'' type_name, " +
                              "        0 is_bank, 0 nzp_bank, '' bank, '' short_name, " +
                              "        '' adress, '' phone " +
                              " From " + tables.payer + " p " +

                                (string.IsNullOrEmpty(where) ? "" : (" Where 1=1 " + where));
                        orderBy = " Order by payer";
                    }
                    break;

                case enSrvOper.PayerReferencedFromBank:
                    //фильтровать справочники по fn_distrib
                    filtrOnDistrib = FiltrOnDistrib("p.nzp_payer", "nzp_payer", finder.dopFind);
                    where += filtrOnDistrib;

                    sql =
                        "insert into t_payers(nzp_payer, payer,npayer,nzp_supp, inn,  id_bc_type, kpp, nzp_type, type_name, name_supp,is_erc, " +
                        "   is_bank, nzp_bank, bank, " +
                        "  short_name,  adress,  phone )" +
 " Select distinct p.nzp_payer, p.payer, p.npayer, p.nzp_supp, p.inn, 0 as id_bc_type, p.kpp, p.nzp_type, pt.type_name, s.name_supp, " +
                        " p.is_erc, 0 is_bank, b.nzp_bank, b.bank, b.short_name, b.adress, b.phone " +
                        " From " + tables.bank + " b, " + tables.payer + " p  " +
                        " left outer join " + tables.supplier + " s on p.nzp_supp = s.nzp_supp " +
                        " left outer join " + tables.payer_types + " pt on p.nzp_type = pt.nzp_payer_type " +
                        " Where p.nzp_payer = b.nzp_payer  " + where;


                    orderBy = " Order by nzp_payer,payer";
                    break;
            }

            ret = ExecSQL(connDB, sql, true);
            if (!ret.result)
            {
                connDB.Close();
                return null;
            }

            sql = "select * from t_payers "
                + orderBy;

            MyDataReader reader;
            ret = ExecRead(connDB, out reader, sql, true);
            if (!ret.result)
            {
                connDB.Close();
                return null;
            }
            try
            {
                int i = 0;
                while (reader.Read())
                {
                    i++;
                    if (i <= finder.skip) continue;
                    var zap = new Payer();

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

                    if (zap.nzp_payer > 0 && oper == enSrvOper.Payer)
                    {
                        sql = "select nzp_payer_type from " + Points.Pref + "_kernel" + tableDelimiter + "payer_types " +
                            " where nzp_payer = " + zap.nzp_payer;
                        MyDataReader reader2;
                        if (!ExecRead(connDB, out reader2, sql, true).result)
                        {
                            connDB.Close();
                            return null;
                        }
                        zap.include_types = new List<int>();
                        while (reader2.Read())
                        {
                            if (reader2["nzp_payer_type"] != DBNull.Value) zap.include_types.Add(Convert.ToInt32(reader2["nzp_payer_type"]));
                        }
                        reader2.Close();
                    }

                    spis.Add(zap);
                    if (finder.rows > 0 && i >= finder.skip + finder.rows) break;
                }
                reader.Close();

                sql = "select count(*) from t_payers";
                object count = ExecScalar(connDB, sql, out ret, true);
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

                ExecSQL(connDB, "drop table t_payers", false);

                connDB.Close();
                return spis;
            }
            catch (Exception ex)
            {
                reader.Close();
                connDB.Close();

                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;

                MonitorLog.WriteLog("Ошибка заполнения списка подрядчиков " + err, MonitorLog.typelog.Error, 20, 201, true);

                return null;
            }
        }

        public List<Payer> LoadPayers(Payer finder, out Returns ret)
        {
            IDbConnection connDB = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(connDB, true);
            if (!ret.result) return null;

            string where = "", tbl = "";
            if (finder.nzp_type > 0 || (finder.include_types != null && finder.include_types.Count > 0))
            {
                tbl = ", " + Points.Pref + "_kernel" + tableDelimiter + "payer_types ptp ";
                where += " and p.nzp_payer = ptp.nzp_payer ";
                if (finder.include_types != null && finder.include_types.Count > 0)
                {
                    var pt = new StringBuilder();
                    foreach (var it in finder.include_types)
                        if (pt.Length == 0) pt.Append(it);
                        else pt.Append(", " + it);
                    where += " and ptp.nzp_payer_type in (" + pt + ")";
                }
                else where += " and ptp.nzp_payer_type = " + finder.nzp_type;
            }
            else
            {
                tbl = " left outer join " + Points.Pref + "_kernel" + tableDelimiter + "payer_types ptp on  p.nzp_payer = ptp.nzp_payer ";
            }

            string sql = "select distinct " +
                         " p.nzp_payer, p.payer, p.npayer, p.nzp_supp, p.id_bc_type, s.name_supp, " +
                         " p.is_erc, p.inn, p.kpp" +
                         (finder.nzp_type == -100 ? "" : ", ptp.nzp_payer_type, pt.type_name ");

            string sql2 =
                        " from " + Points.Pref + "_kernel" + tableDelimiter + "s_payer p " +
                        " left outer join " + Points.Pref + "_kernel" + tableDelimiter + "supplier s on p.nzp_supp = s.nzp_supp " +
                        tbl +
                        " left outer join " + Points.Pref + "_kernel" + tableDelimiter + "s_payer_types pt on  ptp.nzp_payer_type = pt.nzp_payer_type " +
                        " where 1=1 " + where;

            object count = ExecScalar(connDB, "select count(*) " + sql2, out ret, true);
            int recordsTotalCount;
            try { recordsTotalCount = Convert.ToInt32(count); }
            catch (Exception e)
            {
                ret = new Returns(false, "Ошибка при определении количества записей: " + (Constants.Debug ? e.Message : ""));
                MonitorLog.WriteLog("Ошибка LoadPayers " + (Constants.Viewerror ? "\n" + e.Message : ""), MonitorLog.typelog.Error, 20, 201, true);
                connDB.Close();
                return null;
            }

            MyDataReader reader;
            ret = ExecRead(connDB, out reader, sql + sql2 + " order by p.payer", true);
            if (!ret.result)
            {
                connDB.Close();
                return null;
            }

            List<Payer> spis = new List<Payer>();

            try
            {
                int i = 0;
                while (reader.Read())
                {
                    i++;
                    if (i <= finder.skip) continue;
                    var zap = new Payer();

                    if (reader["nzp_payer"] != DBNull.Value) zap.nzp_payer = Convert.ToInt32(reader["nzp_payer"]);
                    if (reader["payer"] != DBNull.Value) zap.payer = Convert.ToString(reader["payer"]).Trim();
                    if (reader["npayer"] != DBNull.Value) zap.npayer = Convert.ToString(reader["npayer"]).Trim();
                    if (reader["nzp_supp"] != DBNull.Value) zap.nzp_supp = Convert.ToInt32(reader["nzp_supp"]);
                    if (reader["name_supp"] != DBNull.Value) zap.name_supp = Convert.ToString(reader["name_supp"]).Trim();
                    if (reader["is_erc"] != DBNull.Value) zap.is_erc = Convert.ToInt32(reader["is_erc"]);
                    if (reader["inn"] != DBNull.Value) zap.inn = Convert.ToString(reader["inn"]).Trim();
                    if (reader["kpp"] != DBNull.Value) zap.kpp = Convert.ToString(reader["kpp"]).Trim();
                    if (reader["id_bc_type"] != DBNull.Value) zap.id_bc_type = Convert.ToInt32(reader["id_bc_type"]);
                    if (finder.nzp_type != -100)
                    {
                        if (reader["type_name"] != DBNull.Value) zap.type_name = Convert.ToString(reader["type_name"]).Trim();
                        if (reader["nzp_payer_type"] != DBNull.Value) zap.nzp_type = Convert.ToInt32(reader["nzp_payer_type"]);
                    }
                    spis.Add(zap);
                    if (finder.rows > 0 && i >= finder.skip + finder.rows) break;
                }

                reader.Close();
                connDB.Close();
                ret.tag = recordsTotalCount;
                return spis;
            }
            catch (Exception ex)
            {
                reader.Close();
                connDB.Close();

                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;

                MonitorLog.WriteLog("Ошибка заполнения списка подрядчиков " + err, MonitorLog.typelog.Error, 20, 201, true);

                return null;
            }
        }

        public List<Payer> LoadPayersNewFd(Payer finder, out Returns ret)
        {
            IDbConnection connDB = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(connDB, true);
            if (!ret.result) return null;

            string where = "", tbl = "";
            if (finder.nzp_type > 0 || (finder.include_types != null && finder.include_types.Count > 0))
            {
                tbl = ", " + Points.Pref + "_kernel" + tableDelimiter + "payer_types ptp ";
                where += " and p.nzp_payer = ptp.nzp_payer ";
                if (finder.include_types != null && finder.include_types.Count > 0)
                {
                    var pt = new StringBuilder();
                    foreach (var it in finder.include_types)
                        if (pt.Length == 0) pt.Append(it);
                        else pt.Append(", " + it);
                    where += " and ptp.nzp_payer_type in (" + pt + ")";
                }
                else where += " and ptp.nzp_payer_type = " + finder.nzp_type;
            }
            else
            {
                tbl = " left outer join " + Points.Pref + "_kernel" + tableDelimiter + "payer_types ptp on  p.nzp_payer = ptp.nzp_payer ";
            }

            if (finder.nzp_payer > 0) where += " and p.nzp_payer = " + finder.nzp_payer;
            if (!String.IsNullOrEmpty(finder.payer)) where += " and p.payer ilike '%" + finder.payer + "%'";

            string sql = "select distinct " +
                         " p.nzp_payer, p.payer, p.npayer, p.nzp_supp, p.id_bc_type, s.name_supp, " +
                         " p.is_erc, p.inn, p.kpp, p.ks, p.city, p.bik ";

            string sql2 =
                        " from " + Points.Pref + "_kernel" + tableDelimiter + "s_payer p " +
                        " left outer join " + Points.Pref + "_kernel" + tableDelimiter + "supplier s on p.nzp_supp = s.nzp_supp " +
                        tbl +
                        " left outer join " + Points.Pref + "_kernel" + tableDelimiter + "s_payer_types pt on  ptp.nzp_payer_type = pt.nzp_payer_type " +
                        " where 1=1 " + where;

            object count = ExecScalar(connDB, "select count(distinct p.nzp_payer) " + sql2, out ret, true);
            int recordsTotalCount;
            try { recordsTotalCount = Convert.ToInt32(count); }
            catch (Exception e)
            {
                ret = new Returns(false, "Ошибка при определении количества записей: " + (Constants.Debug ? e.Message : ""));
                MonitorLog.WriteLog("Ошибка LoadPayers " + (Constants.Viewerror ? "\n" + e.Message : ""), MonitorLog.typelog.Error, 20, 201, true);
                connDB.Close();
                return null;
            }

            MyDataReader reader;
            ret = ExecRead(connDB, out reader, sql + sql2 + " order by p.payer", true);
            if (!ret.result)
            {
                connDB.Close();
                return null;
            }

            List<Payer> spis = new List<Payer>();

            try
            {
                int i = 0;
                while (reader.Read())
                {
                    i++;
                    if (i <= finder.skip) continue;
                    var zap = new Payer();

                    if (reader["nzp_payer"] != DBNull.Value) zap.nzp_payer = Convert.ToInt32(reader["nzp_payer"]);
                    if (reader["payer"] != DBNull.Value) zap.payer = Convert.ToString(reader["payer"]).Trim();
                    if (reader["npayer"] != DBNull.Value) zap.npayer = Convert.ToString(reader["npayer"]).Trim();
                    if (reader["nzp_supp"] != DBNull.Value) zap.nzp_supp = Convert.ToInt32(reader["nzp_supp"]);
                    if (reader["name_supp"] != DBNull.Value) zap.name_supp = Convert.ToString(reader["name_supp"]).Trim();
                    if (reader["is_erc"] != DBNull.Value) zap.is_erc = Convert.ToInt32(reader["is_erc"]);
                    if (reader["inn"] != DBNull.Value) zap.inn = Convert.ToString(reader["inn"]).Trim();
                    if (reader["kpp"] != DBNull.Value) zap.kpp = Convert.ToString(reader["kpp"]).Trim();

                    if (reader["ks"] != DBNull.Value) zap.ks = Convert.ToString(reader["ks"]).Trim();
                    if (reader["bik"] != DBNull.Value) zap.bik = Convert.ToString(reader["bik"]).Trim();
                    if (reader["city"] != DBNull.Value) zap.city = Convert.ToString(reader["city"]).Trim();
                    if (reader["id_bc_type"] != DBNull.Value) zap.id_bc_type = Convert.ToInt32(reader["id_bc_type"]);
                    if (reader["nzp_payer"] != DBNull.Value)
                    {
                        zap.include_types = new List<int>();
                        sql = " SELECT nzp_payer_type FROM " + Points.Pref + "_kernel" + tableDelimiter + "payer_types" +
                              " WHERE nzp_payer = " + zap.nzp_payer;
                        DataTable dt = DBManager.ExecSQLToTable(connDB, sql);
                        foreach (DataRow row in dt.Rows)
                        {
                            zap.include_types.Add(Convert.ToInt32(row["nzp_payer_type"]));
                        }
                    }
                    spis.Add(zap);
                    if (finder.rows > 0 && i >= finder.skip + finder.rows) break;
                }

                reader.Close();
                connDB.Close();
                ret.tag = recordsTotalCount;
                return spis;
            }
            catch (Exception ex)
            {
                reader.Close();
                connDB.Close();

                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;

                MonitorLog.WriteLog("Ошибка заполнения списка подрядчиков " + err, MonitorLog.typelog.Error, 20, 201, true);

                return null;
            }
        }

        public List<Payer> LoadPayersContragents(Payer finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            if (finder.nzp_user <= 0)
            {
                ret.result = false;
                ret.text = "Не задан пользователь";
                return null;
            }

            string connectionString = Points.GetConnByPref(finder.pref);
            IDbConnection connDB = GetConnection(connectionString);
            ret = OpenDb(connDB, true);
            if (!ret.result) return null;

            MyDataReader reader = null;

            var list = new List<Payer>();

            var tables = new DbTables(DBManager.getServer(connDB));
            try
            {
                string sql =
                    " select p.payer || ' / принципал' as payer, p.nzp_payer from " + Points.Pref + "_kernel" + tableDelimiter + "supplier s, " + Points.Pref + "_kernel" + tableDelimiter + "s_payer p  where s.dpd=1 and s.nzp_payer_princip=p.nzp_payer " +
                    "UNION " +
                    " select distinct p.payer || ' / агент', p.nzp_payer from " + Points.Pref + "_kernel" + tableDelimiter + "supplier s, " + Points.Pref + "_kernel" + tableDelimiter + "s_payer p  where s.nzp_payer_agent=p.nzp_payer";
                ret = ExecRead(connDB, out reader, sql, true);
                if (!ret.result)
                {
                    connDB.Close();
                    return null;
                }

                while (reader.Read())
                {
                    var p = new Payer();
                    if (reader["nzp_payer"] != DBNull.Value) p.nzp_payer = Convert.ToInt32(reader["nzp_payer"]);
                    if (reader["payer"] != DBNull.Value) p.payer = Convert.ToString(reader["payer"]);
                    list.Add(p);
                }
                reader.Close();
                return list;
            }
            catch (Exception ex)
            {
                if (reader != null) reader.Close();
                connDB.Close();

                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;

                MonitorLog.WriteLog("Ошибка получения контрагентов " + err, MonitorLog.typelog.Error, 20, 201, true);
                return null;
            }
            //ret = new Returns();
            //return null;
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
            IDbConnection connWeb = GetConnection(Constants.cons_Webdata);
            Returns ret = OpenDb(connWeb, true);
            if (!ret.result) return ret;

            string srv = "";
            if (nzp_server > 0)
                srv = "_" + nzp_server;

            string sPoint = "s_point" + srv;

            string sPointFull = DBManager.GetFullBaseName(connWeb) +
                DBManager.tableDelimiter + sPoint;


            CreateWebPoint(connWeb, sPoint, out ret);
            connWeb.Close();

            if (is_insert)
            {
                IDbConnection connDB = GetConnection(Constants.cons_Kernel);
                ret = OpenDb(connDB, true);
                if (!ret.result)
                {
                    connWeb.Close();
                    return ret;
                }

                string sql = " Insert into " + sPointFull + " (nzp_wp, nzp_graj, n, point, bd_kernel, bd_old, flag) " +
                             " Select nzp_wp, nzp_graj, n, point, bd_kernel, bd_old, flag " +
                             " From " + Points.Pref + DBManager.sKernelAliasRest + "s_point";
                //выбрать список
                ret = ExecSQL(connDB, sql, true);

                if (!ret.result)
                {
                    //значит это локальный банк
                    sql =
                        " Insert into " + sPointFull + " ( nzp_wp, nzp_graj, n, point, bd_kernel ) " +
                        " Values (" + Constants.DefaultZap + ",0,2, 'Локальный банк'," + Utils.EStrNull(Points.Pref) +
                        ")";
                    ExecSQL(connDB, sql, true);
                }

                ret = ExecSQL(connDB, " Insert into " + sPointFull + " (nzp_wp, point) " +
                                       " Values (-1, 'Полный доступ')", true);

                connDB.Close();
            }

            return ret;
        }

        //----------------------------------------------------------------------
        public Returns WebPrm(/*Finder finder*/)
        //----------------------------------------------------------------------
        {
            /*if (finder.nzp_user < 1)
            {
                ret.result = false;
                ret.text = "Не определен пользователь";
                return ret;
            }*/

            IDbConnection connWeb = GetConnection(Constants.cons_Webdata);
            Returns ret = OpenDb(connWeb, true);
            if (!ret.result) return ret;

            const string prmName = "prm_name";

            string prmNameFull = DBManager.GetFullBaseName(connWeb) +
                DBManager.tableDelimiter + prmName;

#if PG
            ret = ExecSQL(connWeb, " set search_path to 'public'", true);
            if (!ret.result) return ret;
#endif

            if (TableInWebCashe(connWeb, prmName))
            {
                ExecSQL(connWeb, " Drop table " + prmName, false);
            }

            ret = ExecSQL(connWeb,
                "CREATE TABLE " + prmName + "(" +
                " nzp_prm SERIAL NOT NULL," +
                " name_prm CHAR(100)," +
                " numer INTEGER," +
                " old_field CHAR(20)," +
                " type_prm CHAR(10)," +
                " nzp_res INTEGER," +
                " prm_num INTEGER," +
                " low_ FLOAT," +
                " high_ FLOAT ," +
                " digits_ INTEGER)", true);
            if (!ret.result)
            {
                connWeb.Close();
                return ret;
            }

            IDbConnection connDB = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(connDB, true);
            if (!ret.result)
            {
                connWeb.Close();
                return ret;
            }

            //выбрать список
            string sql = " Insert into " + prmNameFull + " (nzp_prm, name_prm, numer, old_field," +
                         " type_prm, nzp_res, prm_num, low_, high_, digits_) " +
                         " Select nzp_prm, name_prm, numer, old_field, type_prm, nzp_res, prm_num, low_, high_, digits_ " +
                         " From " + Points.Pref + DBManager.sKernelAliasRest + "prm_name";
            ret = ExecSQL(connDB, sql, true);


            //ret = ExecSQL(connDB, " Insert into " + prmNameFull + " (nzp_prm, name_prm) " +
            //                      " Values (-1, 'Полный доступ')", true);

            if (ret.result) ret = ExecSQL(connWeb, "CREATE UNIQUE INDEX ix_web_prm_name_nzp_prm ON " + prmName + "(nzp_prm);", true);
            if (ret.result) ret = ExecSQL(connWeb, "CREATE INDEX ix_web_prm_name_name_prm ON " + prmName + "(name_prm);", true);

            connDB.Close();
            connWeb.Close();
            return ret;
        }

        //----------------------------------------------------------------------
        public string GetDbPortal(out Returns ret) //получить путь к портальной базеs
        //----------------------------------------------------------------------
        {


            IDbConnection connDB = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(connDB, true);
            if (!ret.result) return null;

            MyDataReader reader;
            if (!ExecRead(connDB, out reader,
                 " Select dbname, dbserver" +
                 " From " + Points.Pref + DBManager.sKernelAliasRest + "s_baselist " +
                 " Where nzp_bl = 1000 ", true).result)
            {
                connDB.Close();
                return null;
            }
            try
            {
                string s = "";
                if (reader.Read())
                {
                    if (reader["dbname"] != DBNull.Value)
                    {

                        s = reader["portal"].ToString().Trim();
                        if (reader["dbserver"] != DBNull.Value)
                            s += "@" + reader["dbserver"].ToString().Trim();
                        s += DBManager.tableDelimiter;
                    }
                }
                else
                    ret.result = false;


                return s;
            }
            catch (Exception ex)
            {
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
            finally
            {
                reader.Close();
                connDB.Close();
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
            MyDataReader reader;

            var rm = new RecordMonth { year_ = 0, month_ = 0 };

            ret = ExecRead(connection, out reader,
                " Select month_,yearr From " + pref + DBManager.sDataAliasRest + "saldo_date " +
                " Where iscurrent = 0 ", true);
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
            return rm;
        }

        /// <summary>
        /// Получить текущий расчетный месяц для управляющих организаций
        /// </summary>
        public Dictionary<int, RecordMonth> GetCalcMonthAreas(IDbConnection connection, IDbTransaction transaction, out Returns ret)
        {
            MyDataReader reader;

            var CalcMonthAreas = new Dictionary<int, RecordMonth>();
            var rm = new RecordMonth { month_ = 0, year_ = 0 };



            ret = ExecRead(connection, out reader,
                " Select nzp_area, month_,year_ From " + Points.Pref +
                DBManager.sDataAliasRest + "saldo_date_area " +
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
                    rm = new RecordMonth { month_ = (int)reader["month_"], year_ = (int)reader["year_"] };
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
            return CalcMonthAreas;
        }

        //        //----------------------------------------------------------------------
        //        public bool PointLoad(bool WorkOnlyWithCentralBank, out Returns ret) //проверить наличие s_point и заполнить список Points
        //        //----------------------------------------------------------------------
        //        {
        //            string z = "0";
        //            ret = Utils.InitReturns();
        //            IDbConnection connDB = null;
        //            MyDataReader reader = null;
        //            try
        //            {
        //                #region try
        //                Points.IsPoint = false;

        //                z = "1";

        //                if (Points.PointList == null) Points.PointList = new List<_Point>();
        //                else Points.PointList.Clear();

        //                if (Points.Servers == null) Points.Servers = new List<_Server>();
        //                else Points.Servers.Clear();

        //                z = 2.ToString();

        //                connDB = GetConnection(Constants.cons_Kernel);

        //                z = 3.ToString();

        //                ret = OpenDb(connDB, true);
        //                if (!ret.result) return false;

        //                //установить явно текущий банк
        //#if PG
        //                ret = ExecSQL(conn_db, "set search_path to '" + Points.Pref + "_kernel'", true);
        //#else
        //                ret = ExecSQL(connDB, " Database " + Points.Pref + "_kernel", true);
        //#endif
        //                if (!ret.result) return false;

        //                z = 4.ToString();

        //                var tables = new DbTables(DBManager.getServer(connDB));

        //                //+++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //                //скачаем текущий расчетный месяц
        //                //+++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //                Points.pointWebData.calcMonth = GetCalcMonth(connDB, null, out ret);
        //                if (!ret.result) return false;

        //                /*  Points.pointWebData.calcMonthAreas = GetCalcMonthAreas(conn_db, null, out ret);
        //                  if (!ret.result) return false;*/

        //                z = 5.ToString();

        //                if (Points.Pref == null) z = "6. Points.Pref is null ";
        //                else z = "6. Points.Pref = " + Points.Pref;

        //                //+++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //                //скачаем текущий операционный день
        //                //+++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //                if (ExecRead(connDB, out reader,
        // "Select dat_oper " +
        // "From " + Points.Pref + DBManager.sDataAliasRest+"fn_curoperday", true).result)
        //                {
        //                    z = "6.1";

        //                    Points.isFinances = true;

        //                    if (reader.Read() && reader["dat_oper"] != DBNull.Value)
        //                    {
        //                        z = "6.2";

        //                        Points.DateOper = Convert.ToDateTime(reader["dat_oper"]);
        //                        //        //определение версии "Финансы УК"
        //                        //        int yy = Points.DateOper.Year;
        //                        //        if (ExecRead(conn_db, out reader,
        //                        //            "Select * From " + Points.Pref + "_fin_" + (yy - 2000).ToString("00") + ":fn_distrib_01", false).result)
        //                        //        {
        //                        //            Points.financesType = FinancesTypes.Uk;
        //                        //        }
        //                    }
        //                    reader.Close();
        //                }

        //                z = "7";

        //                GetCalcDates(connDB, Points.Pref, out Points.pointWebData.beginWork, out Points.pointWebData.beginCalc, out Points.pointWebData.calcMonths, 0, out ret);
        //                if (!ret.result) return false;

        //                z = "8";

        //                z = "9";

        //                //+++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //                //считать s_point
        //                //+++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //                if (!TableInWebCashe(connDB, "s_point"))
        //                {
        //                    //по умолчанию Татарстан, т.к. только в нем есть банки без s_point
        //                    Points.Region = Regions.Region.Tatarstan;

        //                    //заполнить по-умолчанию point = pref (для одиночных баз)
        //                    _Point zap = new _Point();
        //                    zap.nzp_wp = Constants.DefaultZap;
        //                    zap.point = "Локальный банк";
        //                    zap.pref = Points.Pref; ;

        //                    zap.nzp_server = -1;

        //                    GetCalcDates(connDB, zap.pref, out zap.BeginWork, out zap.BeginCalc, out zap.CalcMonths, 0, out ret);
        //                    if (!ret.result) return false;

        //                    Points.isFinances = false; //финансы не подключены
        //                    Points.PointList.Add(zap);
        //                    Points.mainPageName = "Биллинговый центр";
        //                }
        //                else
        //                {
        //                    #region Определение региона
        //                    Points.Region = Regions.Region.Tatarstan;
        //                    Returns retRegion = ExecRead(connDB, out reader,
        //                        " select substr(bank_number" + DBManager.sConvToChar + ",1,2) region_code " +
        //                        "from " + Points.Pref + DBManager.sKernelAliasRest + "s_point where nzp_wp = 1 ", true);
        //                    if (retRegion.result)
        //                    {
        //                        try
        //                        {
        //                            if (reader.Read())
        //                            {
        //                                if (reader["region_code"] != DBNull.Value)
        //                                {
        //                                    Points.Region = Regions.GetById(Convert.ToInt32((string)reader["region_code"]));
        //                                }
        //                            }
        //                        }
        //                        catch (Exception ex)
        //                        {
        //                            throw new Exception("Ошибка при определении кода региона " + ex.Message);
        //                        }
        //                    }
        //                    reader.Close();
        //                    #endregion

        //                    List<string> title;
        //                    if ((title = this.GetMainPageTitle()) != null && title.Count != 0)
        //                    {
        //                        Points.mainPageName = title[0];
        //                    }
        //                    else
        //                    {
        //                        Points.mainPageName = "Биллинговый центр";
        //                    }

        //                    bool bYesServer = isTableHasColumn(connDB, "s_point", "nzp_server");

        //                    if (bYesServer && TableInWebCashe(connDB, "servers"))
        //                    {
        //                        //+++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //                        //подключен механизм фабрики серверов, считаем список серверов
        //                        //+++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //                        if (ExecRead(connDB, out reader,
        //                             " Select * From servers Order by nzp_server ", false).result)
        //                        {
        //                            try
        //                            {
        //                                while (reader.Read())
        //                                {
        //                                    var zap = new _Server();
        //                                    zap.is_valid = true;

        //                                    zap.conn = "";
        //                                    zap.ip_adr = "";
        //                                    zap.login = "";
        //                                    zap.pwd = "";
        //                                    zap.nserver = "";
        //                                    zap.ol_server = "";

        //                                    zap.nzp_server = (int)reader["nzp_server"];

        //                                    if (reader["conn"] != DBNull.Value)
        //                                        zap.conn = (string)reader["conn"];
        //                                    if (reader["ip_adr"] != DBNull.Value)
        //                                        zap.ip_adr = (string)reader["ip_adr"];
        //                                    if (reader["login"] != DBNull.Value)
        //                                        zap.login = (string)reader["login"];
        //                                    if (reader["pwd"] != DBNull.Value)
        //                                        zap.pwd = (string)reader["pwd"];
        //                                    if (reader["nserver"] != DBNull.Value)
        //                                        zap.nserver = (string)reader["nserver"];
        //                                    if (reader["ol_server"] != DBNull.Value)
        //                                        zap.ol_server = (string)reader["ol_server"];

        //                                    zap.conn = zap.conn.Trim();
        //                                    zap.ip_adr = zap.ip_adr.Trim();
        //                                    zap.login = zap.login.Trim();
        //                                    zap.pwd = zap.pwd.Trim();
        //                                    zap.nserver = zap.nserver.Trim();
        //                                    zap.ol_server = zap.ol_server.Trim();

        //                                    Points.Servers.Add(zap);
        //                                }
        //                                Points.IsFabric = true;
        //                            }
        //                            catch (Exception ex)
        //                            {
        //                                MonitorLog.WriteLog("Ошибка заполнения servers " + ex.Message, MonitorLog.typelog.Error, 30, 1, true);
        //                                Points.IsFabric = false;
        //                            }
        //                            reader.Close();
        //                        }
        //                    }

        //                    //заполнить список локальных банков данных
        //                    if (ExecRead(connDB, out reader,
        //                        //" Select * From s_point Where nzp_graj > 0 Order by n", false).result)
        //                       " Select * From s_point Order by nzp_wp", false).result)
        //                    {
        //                        try
        //                        {
        //                            while (reader.Read())
        //                            {
        //                                var zap = new _Point();
        //                                zap.nzp_wp = (int)reader["nzp_wp"];
        //                                zap.flag = (int)reader["flag"];
        //                                zap.point = (string)reader["point"];
        //                                zap.pref = (string)reader["bd_kernel"];
        //                                zap.CalcMonth = GetCalcMonth(connDB, null, zap.pref, out ret);
        //                                zap.point = zap.point.Trim();
        //                                zap.pref = zap.pref.Trim();
        //                                zap.ol_server = "";
        //                                zap.n = reader["n"] != DBNull.Value ? Convert.ToInt32(reader["n"]) : 0;
        //                                if (bYesServer)
        //                                {
        //                                    zap.nzp_server = (int)reader["nzp_server"];
        //                                    if (reader["bd_old"] != DBNull.Value)
        //                                        zap.ol_server = (string)reader["bd_old"];
        //                                }
        //                                else
        //                                    zap.nzp_server = -1;

        //                                if (zap.nzp_wp == 1)
        //                                {
        //                                    //фин.банк
        //                                    Points.Point = zap;
        //                                }
        //                                else
        //                                {
        //                                    //MonitorLog.WriteLog("4", MonitorLog.typelog.Info, true);
        //                                    //список расчетных дат
        //                                    Returns ret2;
        //                                    if (!WorkOnlyWithCentralBank)
        //                                    {
        //                                        //MonitorLog.WriteLog("5", MonitorLog.typelog.Info, true);
        //                                        GetCalcDates(connDB, zap.pref, zap.CalcMonth, out zap.BeginWork, out zap.BeginCalc, out zap.CalcMonths, zap.nzp_server, out ret2);
        //                                    }
        //                                    else
        //                                    {
        //                                        //MonitorLog.WriteLog("6", MonitorLog.typelog.Info, true);
        //                                        // при запрете работе с локальными банками используем параметры центрального банка
        //                                        zap.BeginWork = Points.BeginWork;
        //                                        zap.BeginCalc = Points.BeginCalc;
        //                                        zap.CalcMonths = Points.CalcMonths;
        //                                        ret2 = Utils.InitReturns();
        //                                    }

        //                                    if (!ret2.result && ret2.tag >= 0)
        //                                    {
        //                                        return false;
        //                                    }

        //                                    if (ret2.result)
        //                                    {
        //                                        Points.PointList.Add(zap);
        //                                    }
        //                                    else
        //                                    {
        //                                        ret.tag = -1;
        //                                        ret.text += "Банк данных \"" + zap.point + "\" не доступен\n";
        //                                    }
        //                                }

        //                            }

        //                            Points.IsPoint = true;
        //                        }
        //                        catch (Exception ex)
        //                        {
        //                            Points.IsPoint = false;
        //                            throw new Exception("Ошибка заполнения s_point " + ex.Message);
        //                        }
        //                        reader.Close();
        //                    }
        //                }

        //                CorrectPointsCalcMonths(ref Points.pointWebData.calcMonths, Points.PointList);

        //                z = "10";

        //                //+++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //                //признак Самары!!!
        //                //+++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //                Points.IsSmr = false;
        //                Returns ret3 = ExecRead(connDB, out reader,
        //                " Select val_prm From " + Points.Pref +DBManager.sDataAliasRest+ "prm_5 p " +
        //                " Where p.nzp_prm = 2000 " +
        //                "   and p.is_actual <> 100 " +
        //                "   and p.dat_s  <= " + DBManager.sCurDate + " and p.dat_po >= " +DBManager.sCurDate
        //                , true);
        //                if (ret3.result)
        //                {
        //                    if (reader.Read())
        //                    {
        //                        Points.IsSmr = (((string)reader["val_prm"]).Trim() == "1");
        //                    }
        //                }
        //                reader.Close();

        //                #region параметры распределения оплат
        //                Points.packDistributionParameters = new PackDistributionParameters();
        //                var dbparam = new DbParameters();
        //                var prm = new Prm()
        //                {
        //                    nzp_user = 1,
        //                    pref = Points.Pref,
        //                    prm_num = 10,
        //                    nzp_prm = 1131,
        //                    nzp = 0,
        //                    year_ = Points.CalcMonth.year_,
        //                    month_ = Points.CalcMonth.month_
        //                };
        //                Prm val;
        //                int id;

        //                //Определение стратегии распределения оплат
        //                if (Points.IsSmr) Points.packDistributionParameters.strategy = PackDistributionParameters.Strategies.Samara;
        //                else
        //                {
        //                    Points.packDistributionParameters.strategy = PackDistributionParameters.Strategies.InactiveServicesFirst;


        //                    val = dbparam.FindSimplePrmValue(connDB, prm, out ret3);
        //                    if (ret3.result)
        //                    {
        //                        if (Int32.TryParse(val.val_prm, out id))
        //                        {
        //                            switch (id)
        //                            {
        //                                case 1: Points.packDistributionParameters.strategy = PackDistributionParameters.Strategies.InactiveServicesFirst; break;
        //                                case 2: Points.packDistributionParameters.strategy = PackDistributionParameters.Strategies.ActiveServicesFirst; break;
        //                                case 3: Points.packDistributionParameters.strategy = PackDistributionParameters.Strategies.NoPriority; break;
        //                            }
        //                        }
        //                    }
        //                }

        //                //Распределять пачки сразу после загрузки
        //                prm.nzp_prm = 1132;
        //                val = dbparam.FindSimplePrmValue(connDB, prm, out ret3);
        //                if (ret3.result && val.val_prm == "1")
        //                    Points.packDistributionParameters.DistributePackImmediately = true;

        //                //Выполнять протоколирование процесса распределения оплат
        //                prm.nzp_prm = 1133;
        //                val = dbparam.FindSimplePrmValue(connDB, prm, out ret3);
        //                if (ret3.result && val.val_prm == "1")
        //                    Points.packDistributionParameters.EnableLog = true;

        //                //Первоначальное распределение по полю
        //                Points.packDistributionParameters.chargeMethod = PackDistributionParameters.ChargeMethods.PositiveOutsaldo;
        //                prm.nzp_prm = 1134;
        //                val = dbparam.FindSimplePrmValue(connDB, prm, out ret3);
        //                if (ret3.result)
        //                {
        //                    if (Int32.TryParse(val.val_prm, out id))
        //                    {
        //                        switch (id)
        //                        {
        //                            case 1: Points.packDistributionParameters.chargeMethod = PackDistributionParameters.ChargeMethods.Outsaldo; break;
        //                            case 2: Points.packDistributionParameters.chargeMethod = PackDistributionParameters.ChargeMethods.PositiveOutsaldo; break;
        //                            case 3: Points.packDistributionParameters.chargeMethod = PackDistributionParameters.ChargeMethods.MonthlyCalculationWithChangesAndOverpayment; break;
        //                            case 4: Points.packDistributionParameters.chargeMethod = PackDistributionParameters.ChargeMethods.PositiveMonthlyCalculationWithChangesAndOverpayment; break;
        //                            case 5: Points.packDistributionParameters.chargeMethod = PackDistributionParameters.ChargeMethods.MonthlyCalculationWithChanges; break;
        //                            case 6: Points.packDistributionParameters.chargeMethod = PackDistributionParameters.ChargeMethods.PositiveMonthlyCalculationWithChanges; break;
        //                        }
        //                    }
        //                }

        //                //Рассчитывать суммы к перечислению автоматически при распределении/откате оплат
        //                prm.nzp_prm = (int)ParamIds.SystemParams.CalcDistributionAutomatically;
        //                val = dbparam.FindSimplePrmValue(connDB, prm, out ret3);
        //                if (ret3.result && val.val_prm == "1")
        //                    Points.packDistributionParameters.CalcDistributionAutomatically = true;

        //                //Первоначальное распределение по полю
        //                Points.packDistributionParameters.distributionMethod = PackDistributionParameters.PaymentDistributionMethods.LastMonthPositiveSumCharge;
        //                prm.nzp_prm = (int)ParamIds.SystemParams.PaymentDistributionMethod;
        //                val = dbparam.FindSimplePrmValue(connDB, prm, out ret3);
        //                if (ret3.result)
        //                {
        //                    if (Int32.TryParse(val.val_prm, out id))
        //                    {
        //                        switch (id)
        //                        {
        //                            case 1: Points.packDistributionParameters.distributionMethod = PackDistributionParameters.PaymentDistributionMethods.LastMonthPositiveSumCharge; break;
        //                            case 2: Points.packDistributionParameters.distributionMethod = PackDistributionParameters.PaymentDistributionMethods.LastMonthSumCharge; break;
        //                            case 3: Points.packDistributionParameters.distributionMethod = PackDistributionParameters.PaymentDistributionMethods.CurrentMonthPositiveSumInsaldo; break;
        //                            case 4: Points.packDistributionParameters.distributionMethod = PackDistributionParameters.PaymentDistributionMethods.CurrentMonthSumInsaldo; break;
        //                        }
        //                    }
        //                }

        //                //Плательщик заполняет оплату по услугам
        //                prm.nzp_prm = 1135;
        //                val = dbparam.FindSimplePrmValue(connDB, prm, out ret3);
        //                if (ret3.result && val.val_prm == "1")
        //                    Points.packDistributionParameters.AllowSelectServicesWhilePaying = true;

        //                //Список приоритетных услуг
        //                Points.packDistributionParameters.PriorityServices = new List<Service>();

        //#if PG
        //                ret3 = ExecRead(conn_db, out reader, "select p.nzp_serv, s.service, s.ordering" +
        //                    " from " + Points.Pref + "_kernel" + DBManager.getServer(conn_db) + ".servpriority p, " + tables.services + " s" +
        //                    " where s.nzp_serv = p.nzp_serv and now() between p.dat_s and p.dat_po order by ordering", true);
        //#else
        //                ret3 = ExecRead(connDB, out reader, "select p.nzp_serv, s.service, s.ordering" +
        //                    " from " + Points.Pref + "_kernel@" + DBManager.getServer(connDB) + ":servpriority p, " + tables.services + " s" +
        //                    " where s.nzp_serv = p.nzp_serv and current between p.dat_s and p.dat_po order by ordering", true);

        //#endif
        //                if (ret3.result)
        //                {
        //                    while (reader.Read())
        //                    {
        //                        Service zap = new Service();
        //                        if (reader["nzp_serv"] != DBNull.Value) zap.nzp_serv = (int)reader["nzp_serv"];
        //                        if (reader["service"] != DBNull.Value) zap.service = ((string)reader["service"]).Trim();
        //                        if (reader["ordering"] != DBNull.Value) zap.ordering = (int)reader["ordering"];

        //                        Points.packDistributionParameters.PriorityServices.Add(zap);
        //                    }
        //                    reader.Close();
        //                }
        //                #endregion

        //                #region Признак, сохранять ли показания ПУ прямо в основной банк
        //                Prm prm2 = new Prm()
        //                {
        //                    nzp_user = 1,
        //                    pref = Points.Pref,
        //                    prm_num = 5,
        //                    nzp_prm = 1993,
        //                    nzp = 0,
        //                    year_ = Points.CalcMonth.year_,
        //                    month_ = Points.CalcMonth.month_
        //                };
        //                val = dbparam.FindSimplePrmValue(connDB, prm2, out ret3);
        //                if (ret3.result && val.val_prm == "1")
        //                    Points.SaveCounterReadingsToRealBank = true;
        //                else
        //                    Points.SaveCounterReadingsToRealBank = false;

        //                if (Constants.Trace) Utility.ClassLog.WriteLog("Сохранять показания приборов учета в основной банк данных: " + (Points.SaveCounterReadingsToRealBank ? "Да" : "Нет"));
        //                #endregion

        //                //+++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //                //признак Demo!!!
        //                //+++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //                Points.IsDemo = false;

        //#if PG
        //                ret3 = ExecRead(conn_db, out reader,
        //                " Select val_prm From " + Points.Pref + "_data.prm_5 p " +
        //                " Where p.nzp_prm = 1999 " +
        //                "   and p.is_actual <> 100 " +
        //                "   and p.dat_s  <= current_date and p.dat_po >= current_date "
        //                , true);
        //#else
        //                ret3 = ExecRead(connDB, out reader,
        //                " Select val_prm From " + Points.Pref + "_data:prm_5 p " +
        //                " Where p.nzp_prm = 1999 " +
        //                "   and p.is_actual <> 100 " +
        //                "   and p.dat_s  <= today and p.dat_po >= today "
        //                , true);

        //#endif
        //                if (ret3.result)
        //                {
        //                    if (reader.Read())
        //                    {
        //                        Points.IsDemo = (((string)reader["val_prm"]).Trim() == "1");
        //                    }
        //                    reader.Close();
        //                }
        //                z = 11.ToString();

        //                //+++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //                //признак Is50!!!
        //                //+++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //                Points.Is50 = false;

        //#if PG
        //                ret3 = ExecRead(conn_db, out reader,
        //                " Select val_prm From " + Points.Pref + "_data.prm_5 p " +
        //                " Where p.nzp_prm = 1997 " +
        //                "   and p.is_actual <> 100 " +
        //                "   and p.dat_s  <= current_date and p.dat_po >= current_date "
        //                , true);
        //#else
        //                ret3 = ExecRead(connDB, out reader,
        //                " Select val_prm From " + Points.Pref + "_data:prm_5 p " +
        //                " Where p.nzp_prm = 1997 " +
        //                "   and p.is_actual <> 100 " +
        //                "   and p.dat_s  <= today and p.dat_po >= today "
        //                , true);
        //#endif
        //                if (ret3.result)
        //                {
        //                    if (reader.Read())
        //                    {
        //                        Points.Is50 = (((string)reader["val_prm"]).Trim() == "1");
        //                    }

        //                    reader.Close();
        //                }

        //                z = 12.ToString();

        //                //+++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //                //снять зависшие фоновые задания при рестарте хоста
        //                //+++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //                IDbConnection conn_web = GetConnection(Constants.cons_Webdata);

        //                z = 13.ToString();

        //                //ret3 = OpenDb(conn_web, true);
        //                //if (ret3.result)
        //                //{

        //                //    ExecSQL(conn_web, " Update saldo_fon Set kod_info = 3, txt = 'повторно!' Where kod_info = 0 ", false);
        //                //}

        //                //удалить временные образы __anlXX
        //                DbAnalizClient dba = new DbAnalizClient();
        //                dba.Drop__AnlTables(conn_web);
        //                dba.Close();

        //                conn_web.Close();

        //                z = 14.ToString();

        //                //+++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //                //признак клона
        //                //+++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //                Points.isClone = false;
        //#if PG
        //                ret3 = ExecRead(conn_db, out reader,
        //                " Select dbname From " + Points.Pref + "_kernel.s_baselist " +
        //                " Where idtype = " + BaselistTypes.PrimaryBank.GetHashCode(), true);
        //#else
        //                ret3 = ExecRead(connDB, out reader,
        //                " Select dbname From " + Points.Pref + "_kernel:s_baselist " +
        //                " Where idtype = " + BaselistTypes.PrimaryBank.GetHashCode(), true);
        //#endif
        //                if (ret3.result)
        //                    if (reader.Read())
        //                    {
        //                        Points.isClone = (reader["dbname"] != DBNull.Value && ((string)reader["dbname"]).Trim() != "");
        //                    }
        //                reader.Close();
        //                z = 15.ToString();

        //                //+++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //                //признак IsCalcSubsidy !!!
        //                //+++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //                Points.IsCalcSubsidy = false;

        //#if PG
        //                ret3 = ExecRead(conn_db, out reader,
        //                " Select val_prm From " + Points.Pref + "_data.prm_5 p " +
        //                " Where p.nzp_prm = 1992 " +
        //                "   and p.is_actual <> 100 " +
        //                "   and p.dat_s  <= current_date and p.dat_po >= current_date "
        //                , true);
        //#else
        //                ret3 = ExecRead(connDB, out reader,
        //                " Select val_prm From " + Points.Pref + "_data:prm_5 p " +
        //                " Where p.nzp_prm = 1992 " +
        //                "   and p.is_actual <> 100 " +
        //                "   and p.dat_s  <= today and p.dat_po >= today "
        //                , true);
        //#endif
        //                if (ret3.result)
        //                    if (reader.Read())
        //                    {
        //                        Points.IsCalcSubsidy = (((string)reader["val_prm"]).Trim() == "1");
        //                    }
        //                z = 16.ToString();

        //                #region RecalcMode
        //                Points.RecalcMode = RecalcModes.Automatic;
        //#if PG
        //                ret3 = ExecRead(conn_db, out reader,
        //                    " Select val_prm From " + Points.Pref + "_data.prm_5 p " +
        //                    " Where p.nzp_prm = 1990 " +
        //                    "   and p.is_actual <> 100 " +
        //                    "   and p.dat_s  <= current_date and p.dat_po >= current_date "
        //                    , true);
        //#else
        //                ret3 = ExecRead(connDB, out reader,
        //                    " Select val_prm From " + Points.Pref + "_data:prm_5 p " +
        //                    " Where p.nzp_prm = 1990 " +
        //                    "   and p.is_actual <> 100 " +
        //                    "   and p.dat_s  <= today and p.dat_po >= today "
        //                    , true);
        //#endif
        //                if (ret3.result)
        //                {
        //                    if (reader.Read())
        //                    {
        //                        string val_prm = reader["val_prm"] != DBNull.Value ? ((string)reader["val_prm"]).Trim() : "";
        //                        if (val_prm == RecalcModes.Automatic.GetHashCode().ToString()) Points.RecalcMode = RecalcModes.Automatic;
        //                        else if (val_prm == RecalcModes.AutomaticWithCancelAbility.GetHashCode().ToString()) Points.RecalcMode = RecalcModes.AutomaticWithCancelAbility;
        //                        else if (val_prm == RecalcModes.Manual.GetHashCode().ToString()) Points.RecalcMode = RecalcModes.Manual;
        //                    }
        //                    reader.Close();
        //                    if (Constants.Trace) Utility.ClassLog.WriteLog("Задан режим перерасчета: " + Points.RecalcMode + "(код " + Points.RecalcMode.GetHashCode() + ")");
        //                }
        //                else
        //                {
        //                    if (Constants.Trace) Utility.ClassLog.WriteLog("Ошибка при определении режима перерасчета: " + ret3.text);
        //                }
        //                z = 17.ToString();
        //                #endregion

        //                #region проверить, что в таблице counters всех банков есть поле ngp_cnt
        //                if (WorkOnlyWithCentralBank)
        //                {
        //                    Points.IsIpuHasNgpCnt = false;
        //                }
        //                else
        //                {
        //                    Points.IsIpuHasNgpCnt = true;

        //                    foreach (_Point p in Points.PointList)
        //                    {
        //#if PG
        //                    ret3 = ExecSQL(conn_db, " Select c.ngp_cnt From " + p.pref + "_data.counters c Where c.nzp_cr = 0 ", false);
        //#else
        //                        ret3 = ExecSQL(connDB, " Select c.ngp_cnt From " + p.pref + "_data:counters c Where c.nzp_cr = 0 ", false);
        //#endif
        //                        if (!ret3.result)
        //                        {
        //                            Points.IsIpuHasNgpCnt = false;
        //                            break;
        //                        }
        //                    }
        //                }
        //                #endregion

        //                #region признак Работы с Series !!!
        //                Points.isUseSeries = true;

        //                Returns retus = ExecRead(connDB, out reader,
        //                " Select 1 From " + Points.Pref + "_data" + tableDelimiter + "prm_10 p " +
        //                " Where p.nzp_prm = 1266 " +
        //                "   and p.val_prm = '1'" +
        //                "   and p.is_actual <> 100 " +
        //                "   and " + sCurDate + " between p.dat_s and p.dat_po "
        //                , true);

        //                if (retus.result)
        //                {
        //                    Points.isUseSeries = reader.Read();
        //                    reader.Close();
        //                }

        //                connDB.Close();
        //                #endregion

        //                #region Тип функции генерации платежного кода
        //                //Определяем функцию для генерации платежного кода
        //                DbParameters dbParameters = new DbParameters();
        //                Prm prmGenPkod =
        //                    dbParameters.FindSimplePrmValue(connDB,
        //                        new Prm()
        //                        {
        //                            nzp_user = 1,
        //                            pref = Points.Pref,
        //                            prm_num = ParamNums.General10,
        //                            nzp_prm = ParamIds.SpravParams.TypePkod.GetHashCode(),
        //                            year_ = Points.CalcMonth.year_,
        //                            month_ = Points.CalcMonth.month_
        //                        }, out ret);
        //                dbParameters.Close();
        //                if (!ret.result) return false;

        //                Points.functionTypeGeneratePkod = FunctionsTypesGeneratePkod.standart;
        //                if (prmGenPkod.val_prm != "")
        //                {
        //                    Points.functionTypeGeneratePkod = (FunctionsTypesGeneratePkod)
        //                        Enum.Parse(typeof (FunctionsTypesGeneratePkod), prmGenPkod.val_prm);
        //                }                

        //                #endregion

        //                return true;
        //                #endregion
        //            }
        //            catch (Exception ex)
        //            {
        //                ret = new Returns(false, z + ": " + ex.Message);
        //                MonitorLog.WriteLog("Ошибка в функции DbSprav.PointLoad " + z + ": " + ex.Message, MonitorLog.typelog.Error, 30, 1, true);

        //                if (connDB != null) connDB.Close();
        //                return false;
        //            }
        //            finally
        //            {
        //                if (reader != null) reader.Close();
        //                if (connDB != null) connDB.Close();
        //            }
        //        }

        public bool PointLoad(bool WorkOnlyWithCentralBank, out Returns ret)
        {
            List<string> warnPoint = null;
            return PointLoad(WorkOnlyWithCentralBank, out ret, out warnPoint);
        }

        public bool PointLoad(bool WorkOnlyWithCentralBank, out Returns ret, out List<string> warnPoint)
        {
            var dbLoadPoints = new DbLoadPoints();
            dbLoadPoints.PointLoad(WorkOnlyWithCentralBank, out ret, out warnPoint);
            dbLoadPoints.Close();
            return ret.result;
        }

        public void CorrectPointsCalcMonths(ref List<RecordMonth> PointsCalcMonths, List<_Point> PointList)
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
        public void GetCalcDates(IDbConnection conn_db2, string pref, out RecordMonth bw, out RecordMonth bc, out List<RecordMonth> cm, int nzp_server, out Returns ret)
        //----------------------------------------------------------------------
        {
            RecordMonth calcMonth = Points.CalcMonth;
            GetCalcDates(conn_db2, pref, calcMonth, out bw, out bc, out cm, nzp_server, out ret);
        }

        public void GetCalcDates(IDbConnection conn_db2, string pref, RecordMonth calcMonth, out RecordMonth bw, out RecordMonth bc, out List<RecordMonth> cm, int nzp_server, out Returns ret)
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

            if (ResYs.ResYList != null) ResYs.ResYList.Clear();
            else ResYs.ResYList = new List<_ResY>();

            IDbConnection connDB = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(connDB, true);
            if (!ret.result) return null;

            var tables = new DbTables(DBManager.getServer(connDB));

            //найти префикс локальной базы, чтобы оттуда взять res_y
            //string pref = Points.Pref; //по-умолчанию
            //foreach (_Point zap in Points.PointList)
            //{
            //    if (zap.nzp_wp != Constants.DefaultZap)
            //    {
            //        pref = zap.pref;
            //        break;
            //    }
            //}

            MyDataReader reader;
            if (!ExecRead(connDB, out reader,
                " Select nzp_res,nzp_y,name_y " +
                " From " + tables.res_y +
                " Where nzp_res in (" + find_nzp_res + ")" +
                " Order by nzp_res,nzp_y ", true).result)
            {
                connDB.Close();
                return null;
            }
            try
            {
                int i = 0;
                while (reader.Read())
                {
                    i = i + 1;
                    var zap = new _ResY
                    {
                        nzp_res = reader["nzp_res"] == DBNull.Value ? 0 : Convert.ToInt32(reader["nzp_res"]),
                        nzp_y = reader["nzp_y"] == DBNull.Value ? 0 : Convert.ToInt32(reader["nzp_y"]),
                        name_y = reader["name_y"] == DBNull.Value ? "" : Convert.ToString(reader["name_y"]).Trim()
                    };

                    ResYs.ResYList.Add(zap);
                }

                ret.tag = i;
                reader.Close();

                return ResYs.ResYList;
            }
            catch (Exception ex)
            {
                if (reader != null) reader.Close();


                ret.result = false;
                ret.text = ex.Message;

                string err;
                if (Constants.Viewerror)
                    err = " \n " + ex.Message;
                else
                    err = "";

                MonitorLog.WriteLog("Ошибка заполнения справочника параметров " + err, MonitorLog.typelog.Error, 20, 201,
                    true);

                return null;
            }
            finally
            {
                connDB.Close();
            }
        }
        //----------------------------------------------------------------------
        public List<_TypeAlg> LoadTypeAlg(out Returns ret)
        //----------------------------------------------------------------------
        {


            TypeAlgs.AlgList.Clear();

            IDbConnection connDB = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(connDB, true);
            if (!ret.result) return null;

            //выбрать список


            string sql = " Select nzp_type_alg, name_type " +
                         " From " + Points.Pref + DBManager.sKernelAliasRest + "s_type_alg" +
                         " Order by name_type ";

            MyDataReader reader;
            if (!ExecRead(connDB, out reader, sql, true).result)
            {
                connDB.Close();
                return null;
            }
            try
            {
                int i = 0;
                while (reader.Read())
                {
                    i = i + 1;
                    var zap = new _TypeAlg();

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

                return TypeAlgs.AlgList;
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

                MonitorLog.WriteLog("Ошибка заполнения справочника типы алгоритма " + err, MonitorLog.typelog.Error, 20,
                    201, true);

                return null;
            }
            finally
            {
                connDB.Close();
            }
        }

        //----------------------------------------------------------------------
        public List<Namereg> LoadNamereg(Namereg finder, out Returns ret)
        //----------------------------------------------------------------------
        {

            var spis = new List<Namereg>();

            string connKernel = Points.GetConnByPref(finder.pref);
            IDbConnection connDB = GetConnection(connKernel);
            //IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(connDB, true);

            if (!ret.result) return null;
#if PG
            ret = ExecSQL(connDB, "set search_path to '" + finder.pref.Trim() + "_data'", true);
#else
            ret = ExecSQL(connDB, "database " + finder.pref.Trim() + "_data", true);
#endif
            if (!ret.result)
            {
                connDB.Close();
                return null;
            }

            //выбрать список
            string cols = "kod_namereg, namereg";
            string ogrn = "";
            if (isTableHasColumn(connDB, "s_namereg", "ogrn"))
            {
                ogrn = "ogrn";
                cols += "," + ogrn;
            }

            string inn = "";
            if (isTableHasColumn(connDB, "s_namereg", "inn"))
            {
                inn = "inn";
                cols += "," + inn;
            }

            string kpp = "";
            if (isTableHasColumn(connDB, "s_namereg", "kpp"))
            {
                kpp = "kpp";
                cols += "," + kpp;
            }

            string adrNamereg = "";
            if (isTableHasColumn(connDB, "s_namereg", "adr_namereg"))
            {
                adrNamereg = "adr_namereg";
                cols += "," + adrNamereg;
            }

            string telNamereg = "";
            if (isTableHasColumn(connDB, "s_namereg", "tel_namereg"))
            {
                telNamereg = "tel_namereg";
                cols += "," + telNamereg;
            }

            string dolgnost = "";
            if (isTableHasColumn(connDB, "s_namereg", "dolgnost"))
            {
                dolgnost = "dolgnost";
                cols += "," + dolgnost;
            }

            string fio_namereg = "";
            if (isTableHasColumn(connDB, "s_namereg", "fio_name_reg"))
            {
                fio_namereg = "fio_namereg";
                cols += "," + fio_namereg;
            }


            string sql = " Select " + cols +
                         " From " + finder.pref + DBManager.sDataAliasRest + "s_namereg " +
                         " order by namereg";


            MyDataReader reader;
            if (!ExecRead(connDB, out reader, sql, true).result)
            {
                connDB.Close();
                return null;
            }
            try
            {
                while (reader.Read())
                {
                    var zap = new Namereg();
                    if (reader["kod_namereg"] != DBNull.Value) zap.kod_namereg = (int)reader["kod_namereg"];
                    if (reader["namereg"] != DBNull.Value) zap.namereg = Convert.ToString(reader["namereg"]).Trim();
                    if (ogrn != "") if (reader["ogrn"] != DBNull.Value) zap.ogrn = Convert.ToString(reader["ogrn"]).Trim();
                    if (inn != "") if (reader["inn"] != DBNull.Value) zap.inn = Convert.ToString(reader["inn"]).Trim();
                    if (kpp != "") if (reader["kpp"] != DBNull.Value) zap.kpp = Convert.ToString(reader["kpp"]).Trim();
                    if (adrNamereg != "") if (reader["adr_namereg"] != DBNull.Value) zap.adr_namereg = Convert.ToString(reader["adr_namereg"]).Trim();
                    if (telNamereg != "") if (reader["tel_namereg"] != DBNull.Value) zap.tel_namereg = Convert.ToString(reader["tel_namereg"]).Trim();

                    spis.Add(zap);
                }
                reader.Close();
                return spis;
            }
            catch (Exception ex)
            {
                reader.Close();


                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;

                MonitorLog.WriteLog("Ошибка заполнения списка namereg " + err, MonitorLog.typelog.Error, 20, 201, true);

                return null;
            }
            finally
            {
                connDB.Close();
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


        public List<Measure> LoadMeasure(Measure finder, out Returns ret)
        {

            if (finder.nzp_user <= 0)
            {
                ret = new Returns(false, "Не задан пользователь");
                return null;
            }



            IDbConnection connDB = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(connDB, true);
            if (!ret.result) return null;

            var tables = new DbTables(DBManager.getServer(connDB));

            var list = new List<Measure>();

            //выбрать список
            string sql = " Select nzp_measure, measure " +
                         " From " + tables.measure +
                         " Order by measure ";

            MyDataReader reader;
            if (!ExecRead(connDB, out reader, sql, true).result)
            {
                connDB.Close();
                return null;
            }
            try
            {
                while (reader.Read())
                {
                    var zap = new Measure();

                    if (reader["nzp_measure"] != DBNull.Value) zap.nzp_measure = (int)reader["nzp_measure"];

                    if (reader["measure"] != DBNull.Value) zap.measure = ((string)reader["measure"]).Trim();

                    list.Add(zap);

                }
                reader.Close();

                return list;
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

                MonitorLog.WriteLog("Ошибка заполнения справочника единицы измерения " + err, MonitorLog.typelog.Error,
                    20, 201, true);

                return null;
            }
            finally
            {
                connDB.Close();
            }
        }

        public List<CalcMethod> LoadCalcMethod(CalcMethod finder, out Returns ret)
        {
            if (finder.nzp_user <= 0)
            {
                ret = new Returns(false, "Не задан пользователь");
                return null;
            }



            IDbConnection connDB = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(connDB, true);
            if (!ret.result) return null;

            var tables = new DbTables(DBManager.getServer(connDB));

            var list = new List<CalcMethod>();

            //выбрать список
            string sql = " Select nzp_calc_method, method_name" +
                         " From " + tables.calc_method +
                         " Order by method_name ";

            IDataReader reader;
            if (!ExecRead(connDB, out reader, sql, true).result)
            {
                connDB.Close();
                return null;
            }
            try
            {
                while (reader.Read())
                {
                    var zap = new CalcMethod();

                    if (reader["nzp_calc_method"] != DBNull.Value)
                        zap.nzp_calc_method = (int)reader["nzp_calc_method"];

                    if (reader["method_name"] != DBNull.Value)
                        zap.method_name = ((string)reader["method_name"]).Trim();

                    list.Add(zap);

                }
                reader.Close();

                return list;
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

                MonitorLog.WriteLog("Ошибка заполнения справочника единицы измерения " + err, MonitorLog.typelog.Error,
                    20, 201, true);

                return null;
            }
            finally
            {
                connDB.Close();
            }
        }

        public List<PackTypes> LoadPackTypes(PackTypes finder, out Returns ret)
        {
            if (finder.nzp_user <= 0)
            {
                ret = new Returns(false, "Не задан пользователь");
                return null;
            }



            IDbConnection connDB = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(connDB, true);
            if (!ret.result) return null;

            var tables = new DbTables(DBManager.getServer(connDB));

            var list = new List<PackTypes>();

            //выбрать список
            string sql = " Select id, type_name " +
                         " From " + tables.pack_types + " where id in (10,20) " +
                         " Order by type_name ";

            MyDataReader reader;
            if (!ExecRead(connDB, out reader, sql, true).result)
            {
                connDB.Close();
                return null;
            }
            try
            {
                while (reader.Read())
                {
                    var zap = new PackTypes();

                    if (reader["id"] != DBNull.Value) zap.id = (int)reader["id"];

                    if (reader["type_name"] != DBNull.Value) zap.type_name = ((string)reader["type_name"]).Trim();

                    list.Add(zap);

                }
                reader.Close();

                return list;
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

                MonitorLog.WriteLog("Ошибка заполнения справочника единицы измерения " + err, MonitorLog.typelog.Error,
                    20, 201, true);

                return null;
            }
            finally
            {
                connDB.Close();
            }
        }

        public List<BankPayers> BankPayersLoad(Supplier finder, enTypeOfSupp type, out Returns ret)
        //----------------------------------------------------------------------
        {

            if (finder.pref == "") finder.pref = Points.Pref;
            var spis = new List<BankPayers>();

            IDbConnection connDB = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(connDB, true);
            if (!ret.result) return null;

            var tables = new DbTables(DBManager.getServer(connDB));

            string[] tabnameFin = {Points.Pref + "_fin_" + (Points.DateOper.Year - 2000).ToString("00"),
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

                where += " and s.nzp_supp not in ( Select distinct p.nzp_supp From " + tables.payer + " p Where p.nzp_type = 2)";

            }

            //Определить общее количество записей

            string sql = "Select count(*) From " + finder.pref + DBManager.sKernelAliasRest + "supplier s Where 1 = 1 " + where;

            object count = ExecScalar(connDB, sql, out ret, true);
            int recordsTotalCount;
            try { recordsTotalCount = Convert.ToInt32(count); }
            catch (Exception e)
            {
                ret = new Returns(false, "Ошибка при определении количества записей: " + (Constants.Debug ? e.Message : ""));
                MonitorLog.WriteLog("Ошибка SupplierLoad " + (Constants.Viewerror ? "\n" + e.Message : ""), MonitorLog.typelog.Error, 20, 201, true);
                connDB.Close();
                return null;
            }

            //выбрать список
            if (!Points.isFinances)
            {

                sql = " Select distinct s.nzp_supp, s.name_supp, 0 as nzp_payer, s.adres_supp, s.phone_supp, s.geton_plat, s.have_proc, s.kod_supp " +
                      " From " + finder.pref + DBManager.sKernelAliasRest + "supplier s " +
 " Where 1 = 1 " + where +
                      " Order by name_supp";
            }
            else
            {
#warning Проверить
#if PG
                sql =
                    new System.Text.StringBuilder(
                        " Select distinct s.nzp_supp, s.name_supp, p.nzp_payer, s.adres_supp, s.phone_supp, s.geton_plat, s.have_proc, s.kod_supp ")
                        .Append(" From " + finder.pref + "_kernel.supplier s left outer join " + Points.Pref + "_kernel.s_payer p ")
                        .Append(" on s.nzp_supp = p.nzp_supp ")
                        .Append(where.ReplaceFirstOccurence("and", "where"))
                        .Append(" Order by name_supp")
                        .ToString();
#else
                sql = " Select distinct s.nzp_supp, s.name_supp, p.nzp_payer, s.adres_supp, s.phone_supp, " +
                      " s.geton_plat, s.have_proc, s.kod_supp, pb.payer as bank " +
                      " From " + finder.pref + DBManager.sKernelAliasRest+"supplier s, " 
                      + Points.Pref + DBManager.sKernelAliasRest+ "s_payer p, "
                      + Points.Pref + DBManager.sKernelAliasRest+ "s_payer pb " +
                      " Where p.id_bc_type is null and s.nzp_supp = p.nzp_supp " + where +
                      " and pb.nzp_payer in (select fn.nzp_payer_bank " +
                      "                      from " + Points.Pref + DBManager.sDataAliasRest+"fn_bank fn " +
                      "                      where fn.nzp_payer=p.nzp_payer) " +
                      " and pb.id_bc_type is not null  " +
                      " Order by s.name_supp";
#endif

            }

            MyDataReader reader;
            if (!ExecRead(connDB, out reader, sql, true).result)
            {
                connDB.Close();
                return null;
            }
            try
            {
                int i = 0, j = 0;
                while (reader.Read())
                {
                    i++;
                    if (i <= finder.skip) continue;
                    var zap = new BankPayers();



                    if (reader["nzp_supp"] != DBNull.Value) zap.nzp_supp = Convert.ToInt32(reader["nzp_supp"]);
                    if (reader["name_supp"] != DBNull.Value)
                        zap.name_supp = Convert.ToString(reader["name_supp"]).Trim();
                    if (reader["nzp_payer"] != DBNull.Value) zap.nzp_payer = Convert.ToInt32(reader["nzp_payer"]);
                    if (reader["bank"] != DBNull.Value) zap.bank = Convert.ToString(reader["bank"]).Trim();
                    zap.check = false;
                    if (reader["adres_supp"] != DBNull.Value)
                        zap.adres_supp = Convert.ToString(reader["adres_supp"]).Trim();
                    if (reader["geton_plat"] != DBNull.Value)
                        zap.geton_plat = Convert.ToString(reader["geton_plat"]).Trim();
                    if (reader["have_proc"] != DBNull.Value) zap.have_proc = Convert.ToInt32(reader["have_proc"]);
                    if (reader["kod_supp"] != DBNull.Value) zap.kod_supp = Convert.ToString(reader["kod_supp"]).Trim();

                    try
                    {
#warning Нет смысла в жизни
                        sql =
                            " Select sum(" + DBManager.sNvlWord + "(f1.sum_send,0))+" +
                            " sum(" + DBManager.sNvlWord + "(f2.sum_send,0))+" +
                            " sum(" + DBManager.sNvlWord + "(f3.sum_send,0))+" +
                            " sum(" + DBManager.sNvlWord + "(f4.sum_send,0)) as sum_send " +
                            " from " + tabnameFin[3] + DBManager.tableDelimiter + "fn_sended f1 " +
                            "left outer join " + tabnameFin[2] + DBManager.tableDelimiter + "fn_sended f2 " +
                            " on f1.nzp_snd=f2.nzp_snd " +
                            "left outer join " + tabnameFin[1] + DBManager.tableDelimiter + "fn_sended f3 " +
                            " on f2.nzp_snd=f3.nzp_snd " +
                            "left outer join " + tabnameFin[0] + DBManager.tableDelimiter + "fn_sended f4 " +
                            " on f3.nzp_snd=f4.nzp_snd " +
                            " where f1.nzp_payer=" + zap.nzp_payer + " and f1.id_bc_file is null ";

                        var dt = ClassDBUtils.OpenSQL(sql, connDB);
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

                return spis;
            }
            catch (Exception ex)
            {
                reader.Close();

                ret = new Returns(false, ex.Message);
                MonitorLog.WriteLog(
                    "Ошибка заполнения списка поставщиков " + (Constants.Viewerror ? " \n " + ex.Message : ""),
                    MonitorLog.typelog.Error, 20, 201, true);
                return null;
            }
            finally
            {
                connDB.Close();
            }
        }

        public List<Supplier> LoadSupplierSpis(Area_ls finder, out Returns ret)
        {

            var suppList = new List<Supplier>();
            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return null;

            string str = " select * from " + finder.pref + "_kernel" + tableDelimiter + "supplier " +
                         " where ( nzp_supp not in (select nzp_supp " +
                         "                          from " + finder.pref + "_data" + tableDelimiter + "alias_ls" +
                         "                          where nzp_kvar=" + finder.nzp_kvar + ") " +
                         "         or nzp_supp=" + finder.nzp_supp + ") " +
                         " and nzp_supp>0";
            MyDataReader reader;
            if (!ExecRead(conn_db, out reader, str, true).result)
            {
                conn_db.Close();
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка при формировании списка поставщиков";
                return null;
            }

            try
            {
                //int i = 0;
                while (reader.Read())
                {
                    var zap = new Supplier
                    {
                        name_supp = reader["name_supp"] == DBNull.Value ? "" : reader["name_supp"].ToString().Trim(),
                        nzp_supp = reader["nzp_supp"] == DBNull.Value ? 0 : Convert.ToInt32(reader["nzp_supp"])
                    };

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
        {
            if (finder.pref == "") finder.pref = Points.Pref;
            var spis = new List<BankPayers>();
            string prefKernel = Points.Pref + DBManager.sKernelAliasRest,
                prefData = Points.Pref + DBManager.sDataAliasRest;

            IDbConnection connDB = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(connDB, true);
            if (!ret.result) return null;


            var listDate = new List<byte>();
            byte beginYear = Convert.ToByte(Points.BeginWork.year_ - 2000),
                    endYear = Convert.ToByte(DateTime.Now.Year - 2000);
            for (var i = beginYear; i <= endYear; i++)
            {
                string finTable = Points.Pref + "_fin_" + i + DBManager.tableDelimiter + "fn_sended";
                if (TempTableInWebCashe(connDB, finTable))
                {
                    listDate.Add(i);
                }
            }

            string sql = " SELECT  p.nzp_payer, p.payer, pb.payer as bank " +
                         " FROM  " + prefKernel + "s_payer p, " + prefKernel + "s_payer pb, " + prefKernel + "payer_types pt " +
                         " WHERE pb.nzp_payer IN (SELECT fn.nzp_payer_bank " +
                                                " FROM " + prefData + "fn_bank fn " +
                                                " WHERE fn.nzp_payer = p.nzp_payer) " +
                           " AND pb.id_bc_type IS NOT NULL " +
                           " AND pt.nzp_payer = pb.nzp_payer " +
                           " AND pt.nzp_payer_type = 4 "; // 4 - организация осуществляющая платежи 

            IDataReader reader;
            if (!ExecRead(connDB, out reader, sql, true).result)
            {
                MonitorLog.WriteLog("BankPayersLoadBC: ошибка извлечение списка поставщиков\nsql - запрос : " + sql, MonitorLog.typelog.Error, true);
                ret.text = "Ошибка извлечения списка контрагентов";
                connDB.Close();
                return null;
            }
            try
            {
                int i = 0, j = 0;
                while (reader.Read())
                {
                    i++;
                    if (i <= finder.skip) continue;
                    var zap = new BankPayers();
                    if (reader["nzp_payer"] != DBNull.Value) zap.nzp_supp = Convert.ToInt32(reader["nzp_payer"]);
                    else zap.nzp_supp = -1;
                    if (reader["payer"] != DBNull.Value) zap.name_supp = Convert.ToString(reader["payer"]).Trim();
                    if (reader["bank"] != DBNull.Value) zap.bank = Convert.ToString(reader["bank"]).Trim();
                    zap.check = false;

                    try
                    {
                        decimal allSum = 0.0M;
                        foreach (var l in listDate)
                        {
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
                                         " GROUP BY nzp_payer)) a " +
                                  " WHERE nzp_payer=" + zap.nzp_supp;

                            var dt = ClassDBUtils.OpenSQL(sql, connDB);
                            decimal sumSup;
                            if (decimal.TryParse(dt.resultData.Rows[0][0].ToString(), out sumSup))
                            {
                                allSum += sumSup;
                            }

                        }
                        zap.summ_supp = allSum.ToString(CultureInfo.InvariantCulture);
                    }
                    catch (Exception ex)
                    {
                        MonitorLog.WriteLog("BankPayersLoadBC: ошибка извлечение списка поставщиков\n" + ex.Message, MonitorLog.typelog.Error, true);
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

                return spis;
            }
            catch (Exception ex)
            {
                reader.Close();

                ret = new Returns(false, ex.Message);
                MonitorLog.WriteLog(
                    "Ошибка заполнения списка поставщиков " + (Constants.Viewerror ? " \n " + ex.Message : ""),
                    MonitorLog.typelog.Error, 20, 201, true);
                return null;
            }
            finally
            {
                connDB.Close();

            }
        }

        /// <summary>Возвращает список контрагентов договора</summary>
        /// <param name="nzpUser">Идентификатор пользователя</param>
        /// <param name="typePayer">Тип контрагента</param>
        /// <param name="ret">Результат выполнения функции</param>
        public List<Payer> GetPayersDogovor(int nzpUser, Payer.ContragentTypes typePayer, out Returns ret) 
        {
            #region Проверка входных параметров

            if (nzpUser <= 0)
            {
                ret = new Returns(false, "Пользователь не определен");
                return new List<Payer>();
            }

            #endregion

            #region Открываем соединение

            IDbConnection connDb = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(connDb, true);
            if (!ret.result)
            {
                ret.text = "Ошибка при открытии соединения с БД";
                MonitorLog.WriteLog("GetPayersDogovor : " + ret.text, MonitorLog.typelog.Error, true);
                return new List<Payer>();
            }

            #endregion
            
            var payers = new List<Payer>();
            try
            {
                MyDataReader reader;

                string nzpPayer, errorText;
                string prefKernel = Points.Pref + DBManager.sKernelAliasRest;
                switch (typePayer)
                {
                    case Payer.ContragentTypes.Agent: nzpPayer = "nzp_payer_agent"; errorText = "агентов"; break;
                    case Payer.ContragentTypes.Princip: nzpPayer = "nzp_payer_princip"; errorText = "принципалов"; break;
                    case Payer.ContragentTypes.ServiceSupplier: nzpPayer = "nzp_payer_supp"; errorText = "поставщиков"; break;
                    default: throw new Exception("Указан не верный тип контрагента: " + typePayer);
                }

                string sql = " SELECT DISTINCT s." + nzpPayer + ", " +
                                    " TRIM(p.payer) AS payer " +
                             " FROM " + prefKernel + "supplier s INNER JOIN " + prefKernel + "s_payer p ON p.nzp_payer = s." + nzpPayer +
                             " WHERE s." + nzpPayer + " > 0 ORDER BY 2 ";
                ret = ExecRead(connDb, out reader, sql, true);
                if (!ret.result)
                {
                    string erroe = ret.text;
                    ret.text = "Ошибка при получении списка " + errorText;
                    throw new Exception(erroe);
                }

                while (reader.Read())
                {
                    payers.Add(new Payer
                    {
                        nzp_payer = reader[nzpPayer] != DBNull.Value ? Convert.ToInt32(reader[nzpPayer]) : 0,
                        payer = reader["payer"].ToString().Trim()
                    });
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры GetPayersDogovor : " + ex.Message, MonitorLog.typelog.Error, true);
                ret.text = string.IsNullOrEmpty(ret.text) ? "Ошибка при получении списка контрагентов" : ret.text;
                ret.result = false;
            }
            finally
            {
                if (connDb != null)
                    connDb.Close();
            }

            return payers;
        }
        //----------------------------------------------------------------------


        public List<Formuls> GetFormuls(FormulsFinder finder, out Returns ret)
        {
            if (finder.nzp_user < 0)
            {
                ret = new Returns(false, "Не задан пользователь", -1);
                return null;
            }

            IDbConnection connDB = GetConnection();
            ret = OpenDb(connDB, true);
            if (!ret.result) return null;

            var tables = new DbTables(connDB);
            List<Formuls> spis;

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
                var cnt = ExecScalar(connDB, sql, out ret, true);
                if (!ret.result) return null;

                sql = "select f.nzp_frm, f.name_frm, f.dat_s, f.dat_po, f.is_device, f.nzp_measure, m.measure, m.measure_long" +
                    " from " + tables.formuls + " f left outer join " + tables.measure + " m on f.nzp_measure = m.nzp_measure where 1=1 " + where +
                    " order by name_frm, dat_s desc";

                ret = ExecRead(connDB, out reader, sql, true);
                if (!ret.result) return null;

                spis = new List<Formuls>();
                int i = 0;
                while (reader.Read())
                {
                    i++;
                    if (i <= finder.skip) continue;

                    var zap = new Formuls { num = i };
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
                connDB.Close();
            }

            return spis;
        }

        public TDocumentBase GetDocumentBase(TDocumentBase finder, out Returns ret)
        {
            IDbConnection connDB = GetConnection();
            ret = OpenDb(connDB, true);
            if (!ret.result) return null;
            var tables = new DbTables(connDB);
            string where = "";
            if (finder.nzp_doc_base > 0) where += " and nzp_doc_base = " + finder.nzp_doc_base;
            string sql = "select * from " + tables.document_base + " where 1 = 1" + where;
            MyDataReader reader = null;
            TDocumentBase document_base = new TDocumentBase();
            try
            {
                ret = ExecRead(connDB, out reader, sql, true);
                if (!ret.result) return null;

                if (reader.Read())
                {
                    if (reader["nzp_doc_base"] != DBNull.Value) document_base.nzp_doc_base = (int)reader["nzp_doc_base"];
                    if (reader["num_doc"] != DBNull.Value) document_base.num_doc = Convert.ToString(reader["num_doc"]).Trim();
                    if (reader["dat_doc"] != DBNull.Value) document_base.dat_doc = Convert.ToDateTime(reader["dat_doc"]).ToShortDateString();
                    if (reader["nzp_type_doc"] != DBNull.Value) document_base.nzp_type_doc = Convert.ToInt32(reader["nzp_type_doc"]);
                    if (reader["comment"] != DBNull.Value) document_base.comment = Convert.ToString(reader["comment"]).Trim();
                }
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка получения документа-основания\n " + ex.Message, MonitorLog.typelog.Error, 20, 201, true);
                return null;
            }
            finally
            {
                if (reader != null) reader.Close();
                connDB.Close();
            }

            return document_base;
        }

        public List<KodSum> GeListKodSum(KodSum finder, out Returns ret)
        {
            var res = new List<KodSum>();
            ret = Utils.InitReturns();
            IDbConnection connDB = GetConnection();
            ret = OpenDb(connDB, true);
            if (!ret.result) return null;


            string where = "";
            if (finder.kod > 0) where += " and kod = " + finder.kod;
            if (finder.cnt_shkodes > -1) where += " and cnt_shkodes = " + finder.cnt_shkodes;
            if (finder.is_id_bill > -1) where += " and is_id_bill = " + finder.is_id_bill;

            string sql = "select * from " + Points.Pref + sKernelAliasRest + "kodsum where 1 = 1" + where + " order by kod";
            MyDataReader reader = null;
            try
            {
                ret = ExecRead(connDB, out reader, sql, true);
                if (!ret.result) return null;

                while (reader.Read())
                {
                    var item = new KodSum();
                    if (reader["kod"] != DBNull.Value) item.kod = Convert.ToInt32(reader["kod"]);
                    if (reader["comment"] != DBNull.Value) item.comment = Convert.ToString(reader["comment"]).Trim();
                    if (reader["cnt_shkodes"] != DBNull.Value) item.cnt_shkodes = Convert.ToInt32(reader["cnt_shkodes"]);
                    if (reader["is_id_bill"] != DBNull.Value) item.is_id_bill = Convert.ToInt32(reader["is_id_bill"]);
                    res.Add(item);
                }
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка получения кодов квитанций\n " + ex.Message, MonitorLog.typelog.Error, 20, 201, true);
                return null;
            }
            finally
            {
                if (reader != null) reader.Close();
                connDB.Close();
            }

            return res;
        }

        /// <summary> Загрузить список имеющихся участков </summary>
        public List<int> LoadListUchastok(Finder finder, out Returns ret)
        {
            var reader = new MyDataReader();
            var listUch = new List<int>();

            if (finder.nzp_user < 0)
            {
                ret = new Returns(false, "Не задан пользователь", -1);
                return null;
            }

            IDbConnection connDB = GetConnection();
            ret = OpenDb(connDB, true);

            if (!ret.result) return null;
            try
            {
                string sql = " SELECT uch " +
                             " FROM " + Points.Pref + DBManager.sDataAliasRest + "kvar " +
                             " WHERE uch IS NOT NULL GROUP BY 1 ORDER BY 1";
                ret = ExecRead(connDB, out reader, sql, true);
                if (!ret.result)
                {
                    connDB.Close();
                    reader.Close();
                    MonitorLog.WriteLog("Ошибка в функции LoadListUchastok", MonitorLog.typelog.Error, true);
                    return null;
                }

                listUch.Add(-999); // для значений со значением null
                while (reader.Read())
                {
                    int uchInt;
                    if (reader["uch"] != DBNull.Value && Int32.TryParse(reader["uch"].ToString(), out uchInt))
                        listUch.Add(uchInt);
                }
            }
            catch (Exception ex)
            {
                ret.result = false;
                MonitorLog.WriteLog("Ошибка в функции LoadListUchastok:\n " + ex.Message, MonitorLog.typelog.Error, true);
            }
            finally
            {
                connDB.Close();
                reader.Close();
            }
            return listUch;
        }
        /// <summary>
        ////Обновляет промежуточные справочные таблицы *.s_area, *.s_geu, *.services, *.supplier, *.s_point, *.prm_name, где * - схема public
        /// </summary>
        public Returns UpdateCashSpravTable(Finder finder)
        {
            Returns ret = Utils.InitReturns();
            IDbConnection connDB = GetConnection();
            ret = OpenDb(connDB, true);
            if (!ret.result) return ret;

            if (finder.nzp_user < 0)
            {
                return new Returns(false, "Не задан пользователь", -1);
            }
            List<string> sqlList = new List<string>
            {
                // Сначала удаление
                "Delete  from " + sDefaultSchema + "s_area where 1=1",
                "Delete  from " + sDefaultSchema + "s_geu where 1=1",
                "Delete  from " + sDefaultSchema + "services where 1=1",
                "Delete  from " + sDefaultSchema + "supplier where 1=1",
                "Delete  from " + sDefaultSchema + "s_point where 1=1",
                "Delete  from " + sDefaultSchema + "prm_name where 1=1",
                // потом вставка     
                "Insert into "+sDefaultSchema+"s_area(nzp_area, area, nzp_supp) select nzp_area, area, nzp_supp from "+Points.Pref+ sDataAliasRest+"s_area",
                "Insert into "+sDefaultSchema+"s_geu(nzp_geu, geu) select nzp_geu, geu from "+Points.Pref+ sDataAliasRest+"s_geu",
                "Insert into "+sDefaultSchema+"services(nzp_serv, service, service_small, service_name, ed_izmer, type_lgot, nzp_frm, ordering) select nzp_serv, service, service_small, service_name, ed_izmer, type_lgot, nzp_frm, ordering from "+Points.Pref+ sKernelAliasRest+"services",
                "Insert into "+sDefaultSchema+"supplier(nzp_supp, name_supp, adres_supp, phone_supp, geton_plat, have_proc, kod_supp) select nzp_supp, name_supp, adres_supp, phone_supp, geton_plat, have_proc, kod_supp from "+Points.Pref+ sKernelAliasRest+"supplier",
                "Insert into "+sDefaultSchema+"s_point(nzp_wp, nzp_graj, n, point, bd_kernel, bd_old, flag) select nzp_wp, nzp_graj, n, point, bd_kernel, bd_old, flag from "+Points.Pref+ sKernelAliasRest+"s_point",
                "Insert into "+sDefaultSchema+"prm_name(nzp_prm, name_prm, numer, old_field, type_prm, nzp_res, prm_num, low_, high_, digits_) select nzp_prm, name_prm, numer, old_field, type_prm, nzp_res, prm_num, low_, high_, digits_ from "+Points.Pref+ sKernelAliasRest+"prm_name",
            };

            try
            {
                foreach (string sql in sqlList)
                {
                    ret = ExecSQL(connDB, sql, true);
                    if (!ret.result)
                    {
                        return ret;
                    }
                }
            }
            catch (Exception ex)
            {
                ret.result = false;
                MonitorLog.WriteLog("Ошибка в функции UpdateCashSpravTable():\n " + ex.Message, MonitorLog.typelog.Error, true);
            }
            finally
            {
                connDB.Close();
            }
            return ret;
        }

        public List<OverpaymentForDistrib> GetOverpaymentForDistrib(OverpaymentForDistrib finder, out Returns ret)
        {
            IDbConnection connDB = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(connDB, true);
            if (!ret.result) return new List<OverpaymentForDistrib>();
            var spis = new List<OverpaymentForDistrib>();
            var reader = new MyDataReader();
            try
            {
                // для интерфейса делаем особую выборку
                if (finder.only_negative_for_intf) return GetOnlyNegativeForIntf(connDB, finder, out ret);

                string where = "";

                if (finder.id > 0) where += " AND o.id = " + finder.id + " ";
                if (finder.nzp_payer > 0) where += " AND o.nzp_payer = " + finder.nzp_payer + " ";
                if (finder.only_positive) where += " AND sum_outsaldo > 0 ";
                if (finder.only_negative) where += " AND sum_outsaldo < 0 ";

                string sql =
                    " SELECT DISTINCT o.id, o.nzp_payer, o.sum_payer, o.nzp_supp, o.nzp_serv, sp.payer," +
                    " su.name_supp as supp, se.service, o.sum_negative_outsaldo_payer, o.sum_outsaldo, o.ls_count, o.mark";

                string sql2 = 
                    " FROM " + Points.Pref + sDataAliasRest + "joined_overpayments o" +
                    " LEFT OUTER JOIN " + Points.Pref + sKernelAliasRest +
                    "s_payer sp ON sp.nzp_payer = o.nzp_payer" +
                    " LEFT OUTER JOIN " + Points.Pref + sKernelAliasRest +
                    "supplier su ON su.nzp_supp = o.nzp_supp" +
                    " LEFT OUTER JOIN " + Points.Pref + sKernelAliasRest +
                    "services se ON se.nzp_serv = o.nzp_serv" +
                    " WHERE 1=1 " + where;

                object count = ExecScalar(connDB, " SELECT count(o.id) " + sql2, out ret, true);
                int recordsTotalCount;
                try
                {
                    recordsTotalCount = Convert.ToInt32(count);
                }
                catch (Exception e)
                {
                    ret = new Returns(false,
                        "Ошибка при определении количества записей: " + (Constants.Debug ? e.Message : ""));
                    MonitorLog.WriteLog("Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name +
                                        (Constants.Viewerror ? "\n" + e.Message : ""), MonitorLog.typelog.Error, 20, 201,
                        true);
                    connDB.Close();
                    return null;
                }

                ret = ExecRead(connDB, out reader, sql + sql2 + " ORDER BY sp.payer, su.name_supp, se.service ", true);
                if (!ret.result)
                {
                    connDB.Close();
                    return null;
                }


                int i = 0;
                while (reader.Read())
                {
                    i++;
                    if (i <= finder.skip) continue;
                    var zap = new OverpaymentForDistrib();

                    if (reader["id"] != DBNull.Value) zap.id = Convert.ToInt32(reader["id"]);
                    if (reader["nzp_payer"] != DBNull.Value) zap.nzp_payer = Convert.ToInt32(reader["nzp_payer"]);
                    if (reader["payer"] != DBNull.Value) zap.payer = Convert.ToString(reader["payer"]);
                    if (reader["sum_negative_outsaldo_payer"] != DBNull.Value)
                        zap.sum_negative_outsaldo_payer = Convert.ToDecimal(reader["sum_negative_outsaldo_payer"]);
                    if (reader["sum_payer"] != DBNull.Value) zap.sum_payer = Convert.ToDecimal(reader["sum_payer"]).ToString("N2");
                    if (reader["nzp_supp"] != DBNull.Value) zap.nzp_supp = Convert.ToInt32(reader["nzp_supp"]);
                    if (reader["supp"] != DBNull.Value) zap.supp = Convert.ToString(reader["supp"]);
                    if (reader["nzp_serv"] != DBNull.Value) zap.nzp_serv = Convert.ToInt32(reader["nzp_serv"]);
                    if (reader["service"] != DBNull.Value) zap.service = Convert.ToString(reader["service"]);
                    if (reader["sum_outsaldo"] != DBNull.Value) zap.sum_outsaldo = Math.Abs(Convert.ToDecimal(reader["sum_outsaldo"]));
                    if (reader["ls_count"] != DBNull.Value) zap.ls_count = Convert.ToInt32(reader["ls_count"]);
                    if (reader["mark"] != DBNull.Value) zap.mark = Convert.ToBoolean(reader["mark"]);
                    zap.num = i;
                    
                    spis.Add(zap);
                    if (finder.rows > 0 && i >= finder.skip + finder.rows) break;
                }

                ret.tag = recordsTotalCount;
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = "Ошибка получения списка отобранных переплат для управления переплатами ";
                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;
                MonitorLog.WriteLog("Ошибка получения списка отобранных переплат для управления переплатами " +
                                    err, MonitorLog.typelog.Error, 20, 201, true);
                return new List<OverpaymentForDistrib>();
            }
            finally
            {
                if (connDB != null) connDB.Close();
                if (reader != null) reader.Close();
            }
            return spis;
        }

        /// <summary>
        /// Список получателей денежных средств
        /// </summary>
        /// <param name="connDB"></param>
        /// <param name="finder"></param>
        /// <param name="ret"></param>
        /// <returns></returns>
        private List<OverpaymentForDistrib> GetOnlyNegativeForIntf(IDbConnection connDB, OverpaymentForDistrib finder, out Returns ret)
        {
            ret = Utils.InitReturns();

            //определяем общее количество записей
            object count1 = ExecScalar(connDB,
                " SELECT count(distinct o.nzp_payer)" +
                " FROM " + Points.Pref + sDataAliasRest + "joined_overpayments o WHERE o.sum_outsaldo < 0",
                out ret, true);
            object count2 = ExecScalar(connDB,
                " SELECT count(o.id)" +
                " FROM " + Points.Pref + sDataAliasRest + "joined_overpayments o WHERE o.sum_outsaldo < 0",
                out ret, true);
            int recordsTotalCount = 0;
            try
            {
                recordsTotalCount = Convert.ToInt32(count1) + Convert.ToInt32(count2);
            }
            catch (Exception e)
            {
                ret = new Returns(false,
                    "Ошибка при определении количества записей: " + (Constants.Debug ? e.Message : ""));
                MonitorLog.WriteLog("Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name +
                                    (Constants.Viewerror ? "\n" + e.Message : ""), MonitorLog.typelog.Error, 20, 201, true);
            }

            var listres = new List<OverpaymentForDistrib>();
            var spisAll = new List<OverpaymentForDistrib>();
            string sql = " SELECT o.nzp_payer, o.sum_payer, sp.payer, o.sum_negative_outsaldo_payer, count(DISTINCT ovp.num_ls) as sum" +
                         " FROM " + Points.Pref + sDataAliasRest + "joined_overpayments o" +
                         " LEFT OUTER JOIN " + Points.Pref + sKernelAliasRest +
                         "s_payer sp ON sp.nzp_payer = o.nzp_payer," +
                         Points.Pref + sDataAliasRest + "overpayment ovp" +
                         " WHERE o.sum_outsaldo < 0 AND ovp.nzp_payer = o.nzp_payer" +
                         " group by 1,2,3,4 " +
                         " ORDER BY sp.payer";

            var reader = new MyDataReader();
            try
            {
                ret = ExecRead(connDB, out reader, sql, true);
                if (!ret.result)
                {
                    connDB.Close();
                    return null;
                }
                while (reader.Read())
                {
                    var zap = new OverpaymentForDistrib();

                    if (reader["nzp_payer"] != DBNull.Value) zap.nzp_payer = Convert.ToInt32(reader["nzp_payer"]);
                    if (reader["payer"] != DBNull.Value) zap.payer = Convert.ToString(reader["payer"]);
                    if (reader["sum_negative_outsaldo_payer"] != DBNull.Value)
                        zap.sum_outsaldo = -Convert.ToDecimal(reader["sum_negative_outsaldo_payer"]);
                    if (reader["sum_payer"] != DBNull.Value) zap.sum_payer = Convert.ToDecimal(reader["sum_payer"]).ToString("N2");
                    if (reader["sum"] != DBNull.Value) zap.ls_count = Convert.ToInt32(reader["sum"]);

                    spisAll.Add(zap);

                    sql =
                        " SELECT DISTINCT o.id, o.nzp_payer, o.sum_payer, o.nzp_supp, o.nzp_serv, " +
                        " su.name_supp as supp, se.service, o.sum_negative_outsaldo_payer, o.sum_outsaldo, o.ls_count, o.mark" +
                        " FROM " + Points.Pref + sDataAliasRest + "joined_overpayments o" +
                        " LEFT OUTER JOIN " + Points.Pref + sKernelAliasRest +
                        "supplier su ON su.nzp_supp = o.nzp_supp" +
                        " LEFT OUTER JOIN " + Points.Pref + sKernelAliasRest +
                        "services se ON se.nzp_serv = o.nzp_serv" +
                        " WHERE o.sum_outsaldo < 0 AND o.nzp_payer = " + zap.nzp_payer;
                    DataTable dt = DBManager.ExecSQLToTable(connDB, sql);
                    foreach (DataRow row in dt.Rows)
                    {
                        zap = new OverpaymentForDistrib();

                        if (row["id"] != DBNull.Value) zap.id = Convert.ToInt32(row["id"]);
                        if (row["nzp_payer"] != DBNull.Value) zap.nzp_payer = Convert.ToInt32(row["nzp_payer"]);
                        if (row["sum_negative_outsaldo_payer"] != DBNull.Value)
                            zap.sum_negative_outsaldo_payer = Convert.ToDecimal(row["sum_negative_outsaldo_payer"]);
                       // if (row["sum_payer"] != DBNull.Value) zap.sum_payer = Convert.ToDecimal(row["sum_payer"]).ToString("N2"); ;
                        if (row["nzp_supp"] != DBNull.Value) zap.nzp_supp = Convert.ToInt32(row["nzp_supp"]);
                        if (row["supp"] != DBNull.Value) zap.supp = Convert.ToString(row["supp"]);
                        if (row["nzp_serv"] != DBNull.Value) zap.nzp_serv = Convert.ToInt32(row["nzp_serv"]);
                        if (row["service"] != DBNull.Value) zap.service = Convert.ToString(row["service"]);
                        if (row["sum_outsaldo"] != DBNull.Value)
                            zap.sum_outsaldo = Math.Abs(Convert.ToDecimal(row["sum_outsaldo"]));
                        if (row["ls_count"] != DBNull.Value) zap.ls_count = Convert.ToInt32(row["ls_count"]);
                        if (row["mark"] != DBNull.Value) zap.mark = Convert.ToBoolean(row["mark"]);

                        spisAll.Add(zap);
                    }
                }
                if (ret.result)
                {
                    ret.tag = recordsTotalCount;
                }

                int i = 0;
                if (spisAll != null)
                {
                    foreach (OverpaymentForDistrib o in spisAll)
                    {
                        i++;
                        if (finder.skip > 0 && i <= finder.skip) continue;
                        listres.Add(o);
                        if (i >= finder.skip + finder.rows) break;
                    }
                }
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = "Ошибка получения получателей денежных средств ";
                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;
                MonitorLog.WriteLog("Ошибка GetOnlyNegativeForIntf " +
                                    err, MonitorLog.typelog.Error, 20, 201, true);
                return new List<OverpaymentForDistrib>();
            }
            finally
            {
                if (reader != null) reader.Close();
            }
            return listres;
        }

        /// <summary>
        /// Группировка переплат для отображения в режиме управления переплатами
        /// </summary>
        /// <param name="conn_db"></param>
        /// <returns></returns>
        public Returns PrepareOverpaymentDataForInterface(IDbConnection conn_db)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                string sql =
                    " truncate " + Points.Pref + sDataAliasRest + "joined_overpayments ";
                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result) return ret;

                //добавляем отрицательные оплаты
                sql =
                    " INSERT INTO " + Points.Pref + sDataAliasRest + "joined_overpayments" +
                    " (nzp_payer, sum_negative_outsaldo_payer, sum_payer, nzp_supp, nzp_serv, sum_outsaldo, ls_count) " +
                    " SELECT nzp_payer, sum_negative_outsaldo_payer, sum_payer, nzp_supp, nzp_serv, SUM(sum_outsaldo), COUNT(DISTINCT num_ls)" +
                    " FROM " + Points.Pref + sDataAliasRest + "overpayment " +
                    " WHERE sum_outsaldo < 0" +
                    " GROUP BY nzp_payer, sum_negative_outsaldo_payer, sum_payer, nzp_supp, nzp_serv";
                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result) return ret;

                //добавляем положительные оплаты
                sql =
                    " INSERT INTO " + Points.Pref + sDataAliasRest + "joined_overpayments" +
                    " (nzp_payer, sum_negative_outsaldo_payer, sum_payer, nzp_supp, nzp_serv, sum_outsaldo, ls_count) " +
                    " SELECT nzp_payer, sum_negative_outsaldo_payer, sum_payer, nzp_supp, nzp_serv, SUM(sum_outsaldo), COUNT(DISTINCT num_ls)" +
                    " FROM " + Points.Pref + sDataAliasRest + "overpayment " +
                    " WHERE sum_outsaldo > 0" +
                    " GROUP BY nzp_payer, sum_negative_outsaldo_payer, sum_payer, nzp_supp, nzp_serv";
                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result) return ret;
            }
            catch (Exception ex)
            {
                ret = new Returns(false);
                MonitorLog.WriteLog("Ошибка группировки переплат для отображения в режиме управления переплатами " +
                    ex.Message + " " + ex.StackTrace, MonitorLog.typelog.Error, true);
            }
            return ret;
        }

        public Returns PrepareOverpaymentDataForDistrib()
        {
            Returns ret = Utils.InitReturns();

            IDbConnection conn_db = GetConnection();
            ret = OpenDb(conn_db, true);
            if (!ret.result) return ret;
            try
            {
                //добавляем отрицательные оплаты
                string sql =
                    " UPDATE " + Points.Pref + sDataAliasRest + "overpayment" +
                    " SET mark = 1 WHERE EXISTS" +
                    " ( SELECT 1 FROM " + Points.Pref + sDataAliasRest + "joined_overpayments j" +
                    "   WHERE j.mark AND j.sum_outsaldo < 0" +
                    "   AND j.nzp_payer = " + Points.Pref + sDataAliasRest + "overpayment.nzp_payer" +
                    "   AND j.nzp_supp = " + Points.Pref + sDataAliasRest + "overpayment.nzp_supp" +
                    "   AND j.nzp_serv = " + Points.Pref + sDataAliasRest + "overpayment.nzp_serv ) " +
                    " AND sum_outsaldo < 0 ";
                ret = ExecSQL(conn_db, sql);

                //добавляем положительные оплаты
                sql =
                    " UPDATE " + Points.Pref + sDataAliasRest + "overpayment" +
                    " SET mark = 1 WHERE EXISTS" +
                    " ( SELECT 1 FROM " + Points.Pref + sDataAliasRest + "joined_overpayments j" +
                    "   WHERE j.mark AND j.sum_outsaldo > 0" +
                    "   AND j.nzp_payer = " + Points.Pref + sDataAliasRest + "overpayment.nzp_payer" +
                    "   AND j.nzp_supp = " + Points.Pref + sDataAliasRest + "overpayment.nzp_supp" +
                    "   AND j.nzp_serv = " + Points.Pref + sDataAliasRest + "overpayment.nzp_serv ) " +
                    " AND sum_outsaldo > 0 ";
                ret.result = ret.result && ExecSQL(conn_db, sql).result;
            }
            catch (Exception ex)
            {
                ret = new Returns(false);
                MonitorLog.WriteLog("Ошибка PrepareOverpaymentDataForDistrib " +
                    ex.Message + " " + ex.StackTrace, MonitorLog.typelog.Error, true);
            }
            finally
            {
                conn_db.Close();
            }
            return ret;
        }

        public Returns GetSelectedDogForDistribOv(OverpaymentForDistrib finder)
        {
            Returns ret = Utils.InitReturns();

            if (!finder.only_negative && !finder.only_positive)
                return new Returns(false, "Невозможно определить тип выделенных договоров", -1);

            IDbConnection connDB = GetConnection();
            ret = OpenDb(connDB, true);
            if (!ret.result) return ret;

            try
            {
                    string where = "";
                    if (finder.only_positive) where += " AND sum_outsaldo > 0 ";
                    if (finder.only_negative) where += " AND sum_outsaldo < 0 ";

                string sql =
                    " SELECT COUNT(id)" +
                    " FROM " + Points.Pref + sDataAliasRest + "joined_overpayments" +
                    " WHERE mark " + where;

                object obj = ExecScalar(connDB, sql, out ret, true);
                if (!ret.result) return ret;
                if(obj == null || !Int32.TryParse(obj.ToString(), out ret.tag))
                    return new Returns(false);
            }
            catch (Exception ex)
            {
                ret.result = false;
                MonitorLog.WriteLog("Ошибка в функции " + System.Reflection.MethodBase.GetCurrentMethod().Name +
                    ":\n " + ex.Message, MonitorLog.typelog.Error, true);
            }
            finally
            {
                connDB.Close();
            }
            return ret;
        }

        public Returns InterruptOverpaymentProcess(OverpaymentForDistrib finder)
        {
            Returns ret = Utils.InitReturns();

            if (finder.nzp_user < 1)
                return new Returns(false, "Не определен пользователь", -1);

            IDbConnection connDB = GetConnection();
            ret = OpenDb(connDB, true);
            if (!ret.result) return ret;
            
            MyDataReader reader = null;

            try
            {
                var sql = new StringBuilder("select nzp_status, nzp_fon_selection, nzp_fon_distrib from ");
                sql.AppendFormat(" {0}_data{1}overpaymentman_status where is_actual<>100", Points.Pref, tableDelimiter);
                ret = ExecRead(connDB, out reader, sql.ToString(), true);
                if (!ret.result)
                {
                    ret.text = "Ошибка при определении текущего статуса";
                    ret.tag = -1;
                    return ret;
                }

                if (reader.Read())
                {
                    var nzpStatus = reader["nzp_status"] != DBNull.Value ? Convert.ToInt32(reader["nzp_status"]) : 0;
                    var nzpFon = nzpStatus == 1
                        ? (reader["nzp_fon_selection"] != DBNull.Value ? Convert.ToInt32(reader["nzp_fon_selection"]): 0)
                        : (reader["nzp_fon_distrib"] != DBNull.Value ? Convert.ToInt32(reader["nzp_fon_distrib"]) : 0);

                    sql = new StringBuilder(" update ");
                    sql.AppendFormat(" {0}calc_fon_0 set kod_info = {1}", sDefaultSchema, FonTask.Statuses.WithErrors.GetHashCode());
                    sql.AppendFormat(" where nzp_key = {0}", nzpFon);
                    ret = ExecSQL(connDB, sql.ToString(), true);
                    if (!ret.result)
                    {
                        ret.text = "Ошибка при обновлении статуса процесса";
                        ret.tag = -1;
                        return ret;
                    }

                    sql = new StringBuilder(" UPDATE ");
                    sql.AppendFormat(" {0}{1}overpaymentman_status", Points.Pref, sDataAliasRest);
                    sql.Append(" SET is_actual = 100, is_interrupted = true WHERE is_actual <> 100 ");
                    ret = ExecSQL(connDB, sql.ToString(), true);
                    if (!ret.result)
                    {
                        ret.text = "Ошибка при обновлении статуса";
                        ret.tag = -1;
                        return ret;
                    }

                   
                } 
                sql = new StringBuilder(" truncate ");
                    sql.AppendFormat("{0}{1}overpayment ", Points.Pref, sDataAliasRest);
                    ExecSQL(connDB, sql.ToString(), true);

                    sql = new StringBuilder(" truncate ");
                    sql.AppendFormat("{0}{1}joined_overpayments ", Points.Pref, sDataAliasRest);
                    ret = ExecSQL(connDB, sql.ToString(), true);
                    if (!ret.result)
                    {
                        ret.text = "Ошибка при удалении таблицы с переплатами";
                        ret.tag = -1;
                        return ret;
                    }
            }
            catch (Exception ex)
            {
                ret.result = false;
                MonitorLog.WriteLog("Ошибка в функции " + System.Reflection.MethodBase.GetCurrentMethod().Name +
                    ":\n " + ex.Message, MonitorLog.typelog.Error, true);
            }
            finally
            {
                if (reader != null) reader.Close();
                connDB.Close();
            }
            return ret;
        }

        public Returns EditAliasDomList(House_kodes finder)
        {
            Returns ret = Utils.InitReturns();
            if (finder.nzp_user < 0)
            {
                return new Returns(false, "Не определен пользователь", -1);
            }
            if (finder.nzp_dom < 0)
            {
                return new Returns(false, "Не выбран дом", -1);
            }
            if (finder.pref == "")
            {
                return new Returns(false, "Не определен префикс БД", -1);
            }


            IDbConnection connDB = GetConnection();
            ret = OpenDb(connDB, true);
            if (!ret.result) return ret;

            try
            {
                string sql;
                string alias_dom = finder.pref + sDataAliasRest + "alias_dom";
                if (finder.id > 0)
                {
                    #region редактируем

                    sql =
                        " UPDATE " + alias_dom +
                        " SET kod_dom = '" + finder.kod_dom.Trim() + "', nzp_payer = " + finder.nzp_payer + "," +
                        " comment = '" + finder.comment.Trim() + "', nzp_user = " + finder.nzp_user + "," +
                        " dat_when = now()" +
                        " WHERE id = " + finder.id;
                    ret = ExecSQL(connDB, sql);

                    #endregion
                }
                else
                {
                    #region добавляем

                    sql =
                        " SELECT * FROM " + alias_dom +
                        " WHERE nzp_dom = " + finder.nzp_dom + 
                        " AND trim(upper(kod_dom)) = '" + finder.kod_dom.Trim().ToUpper() + "' " +
                        " AND nzp_payer = " + finder.nzp_payer + "";
                    DataTable dt = DBManager.ExecSQLToTable(connDB, sql);
                    if(dt.Rows.Count > 0)
                        return new Returns(false, "Данная тройка 'код дома в системе биллинга-контрагент-" +
                                                  "код дома в системе контрагента' уже имеется", -1);

                    sql =
                        " INSERT INTO " + alias_dom + 
                        " (nzp_dom, kod_dom, nzp_payer, comment, nzp_user, dat_when, is_actual)" +
                        " VALUES" +
                        " (" + finder.nzp_dom + ", '" + finder.kod_dom.Trim() + "', " + finder.nzp_payer + "," +
                        " '" + finder.comment + "', " + finder.nzp_user + ", now(), 1)";
                    ret = ExecSQL(connDB, sql);
                    if (ret.result) ret.tag = GetSerialValue(connDB);
                    #endregion
                }
            }
            catch (Exception ex)
            {
                ret.result = false;
                MonitorLog.WriteLog("Ошибка в функции " + System.Reflection.MethodBase.GetCurrentMethod().Name +
                    ":\n " + ex.Message, MonitorLog.typelog.Error, true);
            }
            finally
            {
                connDB.Close();
            }
            return ret;
        }
        public Returns DeleteAliasDomList(House_kodes finder)
        {
            Returns ret = Utils.InitReturns();
            if (finder.nzp_user < 0)
            {
                return new Returns(false, "Не определен пользователь", -1);
            }
            if (finder.nzp_dom < 0)
            {
                return new Returns(false, "Не выбран дом", -1);
            }
            if (finder.pref == "")
            {
                return new Returns(false, "Не определен префикс БД", -1);}


            IDbConnection connDB = GetConnection();
            ret = OpenDb(connDB, true);
            if (!ret.result) return ret;

            try
            {
                string sql;
                string alias_dom = finder.pref + sDataAliasRest + "alias_dom";
                if (finder.id > 0)
                {
                    #region редактируем

                    sql =
                        " UPDATE " + alias_dom +
                        " SET is_actual = 100, nzp_user = " + finder.nzp_user + "," +
                        " dat_when = now()" +
                        " WHERE id = " + finder.id;
                    ret = ExecSQL(connDB, sql);

                    #endregion
                }
                else
                {
                    #region добавляем
                    return new Returns(false, "Не определена запись для удаления", -1);
                    #endregion
                }
            }
            catch (Exception ex)
            {
                ret.result = false;
                MonitorLog.WriteLog("Ошибка в функции " + System.Reflection.MethodBase.GetCurrentMethod().Name +
                    ":\n " + ex.Message, MonitorLog.typelog.Error, true);
            }
            finally
            {
                connDB.Close();
            }
            return ret;
        }
    }
}