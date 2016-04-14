using System;
using System.Collections.Generic;
using System.Data;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;
using System.Text;
using System.IO;
using System.Data.OleDb;
using System.Threading;

using SevenZip;
using System.Data.Odbc;
using System.Linq;
using STCLINE.KP50.Utility;
using Bars.KP50.Utils;


namespace STCLINE.KP50.DataBase
{
    public partial class DbAdmin : DbAdminClient
    {
        string Pvers;
        Int32 PmaxVisible = 200;

        protected string plike
        {
            get
            {
#if PG
                return " like ";
#else
                return " matches ";
#endif
            }
        }
        protected string pzvzd
        {
            get
            {
#if PG
                return "%";
#else
                return "*";
#endif
            }
        }

        private string tableUsersRecovery { get { return "users_recovery"; } }

        public List<FilesImported> GetFiles(FilesImported finder, out Returns ret)
        {
            IDataReader reader = null;
            IDbConnection conn_db = null;
            List<FilesImported> result = new List<FilesImported>();
            ret = Utils.InitReturns();
            try
            {
                string connectionString = Points.GetConnByPref(finder.pref);
                conn_db = GetConnection(connectionString);
                ret = OpenDb(conn_db, true);
                if (!ret.result)
                {
                    conn_db.Close();
                    return null;
                }

                if (!TempTableInWebCashe(conn_db, Points.Pref + "_data"+tableDelimiter+"files_imported"))
                {
                    ret.result = false;
                    ret.text = "Данные о загруженных файлах временно не доступны";
                    ret.tag = -1;
                    conn_db.Close();
                    return null;
                }

                string file_type = " and fi.file_type is null " + " and trim(upper(pref)) <> 'CHECKFILE' ";
                if (finder.file_type != 0)
                    file_type = " and fi.file_type = " + finder.file_type + " ";
#if PG
                string skip = finder.skip >= 0 && finder.rows >= 0 ? " offset " + finder.skip + " limit " + finder.rows : String.Empty;
#else
                string skip = finder.skip >= 0 && finder.rows >= 0 ? " skip " + finder.skip + " first " + finder.rows : String.Empty;
#endif
                
                string sql = "select "+
#if PG
#else
                    skip  +
#endif
                    " fi.nzp_file nzp_file, fi.nzp_exc,  fi.nzp_exc_log, fi.nzp_status ,fi.created_on date, fi.loaded_name, fi.saved_name,"+
                    " fs.status_name status, ff.format_name, fv.version_name, u.comment loaded_by, fi.percent, fi.diss_status as diss_status " +
                    " from " + Points.Pref + "_data" + tableDelimiter + "files_imported fi" +
                    " left join " + Points.Pref + "_kernel" + tableDelimiter + "file_versions fv on fi.nzp_version = fv.nzp_version" +
                    " left join " + Points.Pref + "_kernel" + tableDelimiter + "file_formats ff on ff.nzp_ff = fv.nzp_ff" +
                    " left join " + Points.Pref + "_kernel" + tableDelimiter + "file_statuses fs on fs.nzp_stat = fi.nzp_status" +
                    " left join " + Points.Pref + "_data" + tableDelimiter + "users u on u.nzp_user = fi.created_by" +
                    " where fi.nzp_status <>" + (int)STCLINE.KP50.Interfaces.FilesImported.Statuses.Deleted + " " + file_type +
                    " and fi.pref = '"+ finder.bank +"' "+ 
                    " order by date DESC"+
#if PG
                skip+
#else
                
#endif
                "";
                 
                ret = ExecRead(conn_db, out reader, sql, true);
                if (!ret.result)
                {
                    conn_db.Close();
                    return null;
                }

                int i = 0;
                while (reader.Read())
                {
                    i++;
                    FilesImported file = new FilesImported();
                    file.num = (i + finder.skip).ToString();
                    file.nzp_file = reader["nzp_file"] != DBNull.Value ? Convert.ToInt32(reader["nzp_file"]) : -1;
                    file.nzp_status = reader["nzp_status"] != DBNull.Value ? Convert.ToInt32(reader["nzp_status"]) : -1;
                    file.date = reader["date"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(reader["date"]) : null;
                    file.format_name = reader["format_name"] != DBNull.Value ? (reader["format_name"]).ToString().Trim() : null;
                    file.format_version = reader["version_name"] != DBNull.Value ? (reader["version_name"]).ToString().Trim() : null;
                    file.saved_name = reader["saved_name"] != DBNull.Value ? (reader["saved_name"]).ToString().Trim() : null;
                    file.loaded_name = reader["loaded_name"] != DBNull.Value ? (reader["loaded_name"]).ToString().Trim() : null;
                    file.status = reader["status"] != DBNull.Value ? (reader["status"]).ToString() : null;
                    file.loaded_string = reader["loaded_by"] != DBNull.Value ? reader["loaded_by"].ToString().Trim() : null;
                    file.nzp_exc = reader["nzp_exc"] != DBNull.Value ? Convert.ToInt32(reader["nzp_exc"]) : -1;
                    file.nzp_exc_log = reader["nzp_exc_log"] != DBNull.Value ? Convert.ToInt32(reader["nzp_exc_log"]) : -1;
                    file.percent = reader["percent"] != DBNull.Value ? (Convert.ToDecimal(reader["percent"])*100).ToString().Substring(0, (Convert.ToDecimal(reader["percent"])*100).ToString().Length -3)  + "%" : "";
                    file.diss_status = reader["diss_status"] != DBNull.Value ? (reader["diss_status"]).ToString().Trim() : null;
                    

                    sql =
                        "select * from " + Points.Pref + "_data" + tableDelimiter + "files_selected " +
                        "  where nzp_file = " + file.nzp_file + 
                        " and nzp_user = " + finder.nzp_user +
                        " and pref = '" + finder.bank.Trim() + "' ";

                    IntfResultTableType dt = ClassDBUtils.OpenSQL(sql, conn_db);
                    if (dt.resultData.Rows.Count == 0)
                        file.to_disassembly = false;
                    else
                        file.to_disassembly = true;

                    result.Add(file);
                }
                string where = " where file_type is null ";
                if (finder.file_type != 0)
                    where = " where file_type = " + finder.file_type + " ";
                sql = 
                    " select count(*) from " + Points.Pref + "_data" + tableDelimiter + "files_imported fi " +
                    where + " and nzp_status <> 7 " +
                    " and trim(upper(pref)) <> 'CHECKFILE' " +
                    "and fi.pref = '"+ finder.bank +"'";

                object count = ExecScalar(conn_db, sql, out ret, true);
                if (ret.result)
                {
                    ret.tag = Convert.ToInt32(count);
                }
                return result;
            }
            catch (Exception ex)
            {
                ret = new Returns(false, ex.Message);
                MonitorLog.WriteLog("Ошибка GetFiles\n" + ex.Message, MonitorLog.typelog.Error, 20, 201, true);
                return null;
            }
            finally
            {
                if (reader != null)
                {
                    reader.Close();
                    reader.Dispose();
                }
                if (conn_db != null)
                    conn_db.Close();
            }
        }

        List<Role> GetRoles(Role finder, out Returns ret, IDbConnection conn_web)
        {
            return GetRoles(finder, out ret, conn_web, false);
        }

        List<Role> GetRoles(Role finder, out Returns ret, IDbConnection conn_web, bool isLoadRolesVal)
        {


#if PG
            ret = ExecSQL(conn_web, " set search_path to 'public'", true);
#else
            ret = ExecSQL(conn_web, " set encryption password '" + BasePwd + "'", true);
#endif
            if (!ret.result) return null;

            List<Role> spis = new List<Role>();

            string where = " Where 1=1";
            string tableUserp = "";
            if (finder.nzp_role > 0) where += " and r.nzp_role = " + finder.nzp_role.ToString();
#if PG
            if (finder.role != "") where += " and upper(decrypt_char(r.role)) like '%" + finder.role.ToUpper().Replace("'", "\"").Replace("*", "%") + "%'";
#else
            if (finder.role != "") where += " and upper(decrypt_char(r.role)) like '%" + finder.role.ToUpper().Replace("'", "''").Replace("*", "%") + "%'";
#endif
            if (finder.nzpuser > 0)
            {
                where += " and u.nzp_role = r.nzp_role and u.nzp_user = " + finder.nzpuser.ToString();

                tableUserp = ", userp u";
            }
            else if (finder.nzpuser < 0)
            {
                where += " and r.nzp_role not in (select nzp_role from userp where nzp_user = " + (-finder.nzpuser).ToString() + ")";
                tableUserp = "";
            }

            int total_record_count = 0;
            object count = ExecScalar(conn_web, " Select count(*) From s_roles r" + tableUserp + where, out ret, true);
            if (ret.result)
            {
                try
                {
                    total_record_count = Convert.ToInt32(count);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = ex.Message;
                    return null;
                }
            }

            string skip = "";
#if PG
            if (finder.skip > 0) skip = " offset " + finder.skip.ToString();
#else
            if (finder.skip > 0) skip = " skip " + finder.skip.ToString();
#endif
#if PG
           
#else
             where += " and r.page_url = p.nzp_page";
#endif

#if PG
            string sql = " Select " +
                   " case when (select count(*) from roles rr where rr.cur_page = 0 and rr.nzp_role = r.nzp_role and decrypt_char(rr.sign) = rr.tip||CAST(rr.kod as text)||rr.cur_page||CAST(rr.nzp_role as text)||'-'||rr.nzp_rls||'roles') > 0 then 1 else case when r.nzp_role between 900 and 999 then 2 else 3 end end as role_type, r.nzp_role, decrypt_char(r.role) as role, r.page_url, decrypt_char(p.page_name) as page_name, r.sort " +
                 " From s_roles r left outer join pages p on   r.page_url = p.nzp_page " + tableUserp + where + " Order by 1, 3 "+ skip;
#else
            string sql = " Select " + skip +
                   " case when (select count(*) from roles rr where rr.cur_page = 0 and rr.nzp_role = r.nzp_role and decrypt_char(rr.sign) = rr.tip||rr.kod||rr.cur_page||rr.nzp_role||'-'||rr.nzp_rls||'roles') > 0 then 1 else case when r.nzp_role between 900 and 999 then 2 else 3 end end as role_type, r.nzp_role, decrypt_char(r.role) as role, r.page_url, decrypt_char(p.page_name) as page_name, r.sort " +
                 " From s_roles r, outer pages p " + tableUserp + where + " Order by 1, 3 ";
#endif


            IDataReader reader;

            ret = ExecRead(conn_web, out reader, sql, true);
            if (!ret.result) return null;

            try
            {
                int i = 0;
                while (reader.Read())
                {
                    Role zap = new Role();

                    zap.num = ++i + finder.skip;

                    if (reader["nzp_role"] != DBNull.Value) zap.nzp_role = Convert.ToInt32(reader["nzp_role"]);
                    if (reader["role"] != DBNull.Value) zap.role = Convert.ToString(reader["role"]).Trim();
                    if (finder.nzp_role <= 0 && reader["role_type"] != DBNull.Value && Convert.ToInt32(reader["role_type"]) == 1) zap.role += " (подсистема)";
                    if (reader["page_url"] != DBNull.Value) zap.page_url = Convert.ToInt32(reader["page_url"]);
                    if (reader["page_name"] != DBNull.Value) zap.page_name = Convert.ToString(reader["page_name"]).Trim();
                    if (reader["sort"] != DBNull.Value) zap.sort = Convert.ToInt32(reader["sort"]);

                    if (isLoadRolesVal)
                    {
                        zap.RolesVal = GetRolesKey(zap.nzp_role, conn_web, out ret);
                        if (!ret.result)
                        {
                            reader.Close();
                            return null;
                        }
                    }

                    spis.Add(zap);

                    if (i >= finder.rows) break;
                }

                reader.Close();
                ret.tag = total_record_count;
                return spis;
            }
            catch (Exception ex)
            {
                reader.Close();

                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;

                MonitorLog.WriteLog("Ошибка заполнения списка ролей " + err, MonitorLog.typelog.Error, 20, 201, true);

                return null;
            }
        }//GetUser

        List<_RolesVal> GetRolesKey(int nzp_role, IDbConnection conn_web, out Returns ret)
        {
#if PG
            string select = "Select distinct r.tip, r.kod";
            string from = " From roleskey r ";
            string where = " Where r.tip||CAST(r.kod as text)||r.nzp_role||'-'||r.nzp_rlsv||'roles' = decrypt_char(r.sign)" +
#else
            string select = "Select unique r.tip, r.kod";
            string from = " From roleskey r ";
            string where = " Where r.tip||r.kod||r.nzp_role||'-'||r.nzp_rlsv||'roles' = decrypt_char(r.sign)" +
#endif
            " and r.nzp_role = " + nzp_role;

            if (TableInWebCashe(conn_web, "s_area"))
            {
                select += ", a.area";
#if PG
                from += " left outer join s_area a on case when r.tip = 102 then r.kod else -1 end = a.nzp_area";
#else
                from += ", outer s_area a";
                where += " and case when r.tip = 102 then r.kod else -1 end = a.nzp_area";
#endif
            }
            else select += ", '' as area";

            if (TableInWebCashe(conn_web, "s_geu"))
            {
                select += ", g.geu";
#if PG
                from += " left outer join s_geu g on case when r.tip = 103 then r.kod else -1 end = g.nzp_geu ";
#else
                from += ", outer s_geu g";
                where += " and case when r.tip = 103 then r.kod else -1 end = g.nzp_geu";
#endif
            }
            else select += ", '' as geu";

            if (TableInWebCashe(conn_web, "services"))
            {
                select += ", s.service";
#if PG
                from += " left outer join services s on case when r.tip = 121 then r.kod else -1 end = s.nzp_serv ";
#else
                from += ", outer services s";
                where += " and case when r.tip = 121 then r.kod else -1 end = s.nzp_serv";
#endif
            }
            else select += ", '' as service";

            if (TableInWebCashe(conn_web, "supplier"))
            {
                select += ", sp.name_supp";
#if PG
                from += " left outer join supplier sp on case when r.tip = 120 then r.kod else -1 end = sp.nzp_supp ";
#else
                from += ", outer supplier sp";
                where += " and case when r.tip = 120 then r.kod else -1 end = sp.nzp_supp";
#endif
            }
            else select += ", '' as name_supp";

            if (TableInWebCashe(conn_web, "s_point"))
            {
                select += ", p.point";
#if PG
                from += " left outer join s_point p on case when r.tip = 101 then r.kod else -1 end = p.nzp_wp ";
#else
                from += ", outer s_point p";
                where += " and case when r.tip = 101 then r.kod else -1 end = p.nzp_wp";
#endif
            }
            else select += ", '' as point";

            if (TableInWebCashe(conn_web, "prm_name"))
            {
                select += ", prm.name_prm";
#if PG
                from += " left outer join prm_name prm on case when r.tip = " + Constants.role_sql_prm + " then r.kod else -1 end = prm.nzp_prm ";
#else
                from += ", outer prm_name prm";
                where += " and case when r.tip = " + Constants.role_sql_prm + " then r.kod else -1 end = prm.nzp_prm";
#endif
            }
            else select += ", '' as name_prm";

            if (TableInWebCashe(conn_web, "servers") && TableInWebCashe(conn_web, "s_rcentr"))
            {
                select += ", rc.rcentr";
#if PG
                from += "left outer join (servers srvr left outer join s_rcentr rc on and srvr.nzp_rc = rc.nzp_rc)" +
                    " on case when r.tip = " + Constants.role_sql_server + " then r.kod else -1 end = srvr.nzp_server";
#else
                from += ", outer (servers srvr, outer s_rcentr rc)";
                where += " and srvr.nzp_rc = rc.nzp_rc" +
                    " and case when r.tip = " + Constants.role_sql_server + " then r.kod else -1 end = srvr.nzp_server";
#endif
            }
            else select += ", '' as rcentr";

            string sql = select + from + where + " Order by tip, kod";

            IDataReader reader;
            ret = ExecRead(conn_web, out reader, sql, true);
            if (!ret.result) return null;

            List<_RolesVal> list = new List<_RolesVal>();

            try
            {
                while (reader.Read())
                {
                    _RolesVal roleVal = new _RolesVal();
                    if (reader["tip"] != DBNull.Value) roleVal.tip = Convert.ToInt32(reader["tip"]);
                    if (reader["kod"] != DBNull.Value) roleVal.kod = Convert.ToInt32(reader["kod"]);
                    switch (roleVal.tip)
                    {
                        case Constants.role_sql_wp:
                            if (reader["point"] != DBNull.Value) roleVal.val = Convert.ToString(reader["point"]).Trim();
                            break;
                        case Constants.role_sql_area:
                            if (reader["area"] != DBNull.Value) roleVal.val = Convert.ToString(reader["area"]).Trim();
                            break;
                        case Constants.role_sql_geu:
                            if (reader["geu"] != DBNull.Value) roleVal.val = Convert.ToString(reader["geu"]).Trim();
                            break;
                        case Constants.role_sql_supp:
                            if (reader["name_supp"] != DBNull.Value) roleVal.val = Convert.ToString(reader["name_supp"]).Trim();
                            break;
                        case Constants.role_sql_serv:
                            if (reader["service"] != DBNull.Value) roleVal.val = Convert.ToString(reader["service"]).Trim();
                            break;
                        case Constants.role_sql_prm:
                            if (reader["name_prm"] != DBNull.Value) roleVal.val = Convert.ToString(reader["name_prm"]).Trim();
                            break;
                        case Constants.role_sql_server:
                            if (reader["rcentr"] != DBNull.Value) roleVal.val = Convert.ToString(reader["rcentr"]).Trim();
                            break;
                    }
                    list.Add(roleVal);
                }
                reader.Close();
            }
            catch (Exception ex)
            {
                reader.Close();

                ret.result = false;
                ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка в функции GetRolesKey " + (Constants.Viewerror ? "\n" + ex.Message : ""), MonitorLog.typelog.Error, 20, 201, true);

                return null;
            }

            return list;
        }

        private string makeWhereForProcess(BackgroundProcess finder, string alias, ref Returns ret)
        {
            if (alias != "") alias = alias + ".";

            if (finder.nzp_user < 1)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Не определен пользователь";
                return "";
            }

            DateTime datIn = DateTime.MinValue;
            DateTime datInPo = DateTime.MaxValue;

            if (finder.dat_in != "" && !DateTime.TryParse(finder.dat_in, out datIn))
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка в параметрах запроса";
                return "";
            }
            if (finder.dat_in_po != "" && !DateTime.TryParse(finder.dat_in_po, out datInPo))
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка в параметрах запроса";
                return "";
            }

            string where = "";

            if (finder.nzp_key > 0) where += " and " + alias + "nzp_key = " + finder.nzp_key.ToString();

            if (finder.dat_in_po != "")
            {
                where += " and " + alias + "dat_in < '" + datInPo.AddDays(1).ToString("yyyy-MM-dd HH:mm") + "'";
                if (finder.dat_in != "") where += " and " + alias + "dat_in >= '" + datIn.ToString("yyyy-MM-dd HH:mm") + "'";
            }
            else if (finder.dat_in != "") where += " and " + alias + "dat_in >= '" + datIn.ToString("yyyy-MM-dd HH:mm") + "' and "
                                                           + alias + "dat_in < '" + datIn.AddDays(1).ToString("yyyy-MM-dd HH:mm") + "'";

            string prms = "";
            if (Utils.GetParams(finder.prms, Constants.act_process_in_queue.ToString())) prms += "," + BackgroundProcess.getKodInfo(Constants.act_process_in_queue);
            if (Utils.GetParams(finder.prms, Constants.act_process_active.ToString())) prms += "," + BackgroundProcess.getKodInfo(Constants.act_process_active);
            if (Utils.GetParams(finder.prms, Constants.act_process_finished.ToString())) prms += "," + BackgroundProcess.getKodInfo(Constants.act_process_finished);
            if (Utils.GetParams(finder.prms, Constants.act_process_with_errors.ToString())) prms += "," + BackgroundProcess.getKodInfo(Constants.act_process_with_errors);
            if (prms != "") where += " and " + alias + "kod_info in (" + prms.Substring(1, prms.Length - 1) + ")";

            return where;
        }

        private string makeWhereForProcess(ProcessWithYearMonth finder, string alias, ref Returns ret)
        {
            string where = makeWhereForProcess((BackgroundProcess)finder, alias, ref ret);

            if (alias != "") alias = alias + ".";

            if (finder.year_ > 0) where += " and " + alias + "year_ = " + finder.year_;
            if (finder.month_ > 0) where += " and " + alias + "month_ = " + finder.month_;

            return where;
        }

        private string makeWhereForProcess(ProcessSaldo finder, string alias, ref Returns ret)
        {
            string where = makeWhereForProcess(finder as ProcessWithYearMonth, alias, ref ret);

            if (alias != "") alias = alias + ".";

            if (finder.nzp_area > 0) where += " and " + alias + "nzp_area = " + finder.nzp_area;

            return where;
        }

        private string makeWhereForProcess(ProcessCalc finder, string alias, ref Returns ret)
        {
            string where = makeWhereForProcess(finder as ProcessWithYearMonth, alias, ref ret);

            if (alias != "") alias = alias + ".";

            if (finder.nzp >= 0) where += " and " + alias + "nzp = " + finder.nzp;
            if (finder.nzpt > 0) where += " and " + alias + "nzpt = " + finder.nzpt;
            if (finder.task >= 0) where += " and " + alias + "task = " + finder.task;
            if (finder.prior > 0) where += " and " + alias + "prior = " + finder.prior;

            return where;
        }

        private string makeWhereForProcess(ProcessBill finder, string alias, ref Returns ret)
        {
            string where = makeWhereForProcess(finder as ProcessWithYearMonth, alias, ref ret);

            if (alias != "") alias = alias + ".";

            if (finder.nzp_key > 0) where += " and " + alias + "nzp_key = " + finder.nzp_key;
            if (finder.nzp_area > 0) where += " and " + alias + "nzp_area = " + finder.nzp_area;
            if (finder.nzp_geu > 0) where += " and " + alias + "nzp_geu = " + finder.nzp_geu;
            if (finder.nzp_wp > 0) where += " and " + alias + "nzp_wp = " + finder.nzp_wp;

            return where;
        }

        public Returns UploadAreaInDb(FilesImported finder)
        {
            return UploadAreaInDb(finder, false);
        }



        public Returns UploadAreaInDb(FilesImported finder, bool add)
        {
            Returns ret = Utils.InitReturns();
            string connectionString = Points.GetConnByPref(Points.Pref);
            IDbConnection conn_db = GetConnection(connectionString);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return ret;
            ret = UploadAreaInDb(finder, false, conn_db);
            conn_db.Close();
            return ret;

        }

        public Returns UploadAreaInDb(FilesImported finder, bool add, IDbConnection conn_db)
        {
            Returns ret = Utils.InitReturns();


            string sql = "";
            IDataReader reader = null, reader2;

            int nzp_supp, nzp_area;
            int counter_update = 0, counter_insert = 0, counter = 0;
            DbTables tables = new DbTables(conn_db);

            string where = "";
            if (add)
            {
#if PG
                where += " and coalesce(nzp_area,0) = 0";
#else
                where += " and nvl(nzp_area,0) = 0";
#endif
            }

            try
            {
                sql = "select id, area, jur_address, " +
                      "fact_address, inn, kpp, rs, bank, bik, ks, nzp_area from " + tables.file_area +
                      " where nzp_file = " + finder.nzp_file + where;
                ret = ExecRead(conn_db, out reader, sql, true);
                if (!ret.result)
                {
                    // conn_db.Close();
                    return ret;
                }
                IDbTransaction transaction = null; // conn_db.BeginTransaction();
                while (reader.Read())
                {
                    counter++;
                    FileArea fileArea = new FileArea();
                    if (reader["id"] != DBNull.Value) fileArea.id = Convert.ToInt32(reader["id"]);
                    if (reader["area"] != DBNull.Value) fileArea.area = Convert.ToString(reader["area"]).Trim();
                    if (reader["inn"] != DBNull.Value) fileArea.inn = Convert.ToString(reader["inn"]).Trim();
                    if (reader["kpp"] != DBNull.Value) fileArea.kpp = Convert.ToString(reader["kpp"]).Trim();

                    if (add)
                    {
                        nzp_area = AddAreaInDb(conn_db, transaction, fileArea, finder, out ret);
                        if (!ret.result)
                        {
                            reader.Close();
                            reader.Dispose();
                            //   conn_db.Close();
                            return ret;
                        }
                        counter_insert++;
                    }
                    else
                    {
                        sql = "select nzp_supp from " + tables.payer + " where trim(inn) = '" + fileArea.inn + "'" +
                              " and trim(kpp) = '" + fileArea.kpp + "'";
                        ret = ExecRead(conn_db, transaction, out reader2, sql, true);
                        if (!ret.result)
                        {
                            //if (transaction != null) transaction.Rollback();
                            reader.Close();
                            reader.Dispose();
                            //conn_db.Close();
                            return ret;
                        }

                        nzp_supp = 0;
                        nzp_area = 0;
                        if (reader2.Read())
                            if (reader2["nzp_supp"] != DBNull.Value) nzp_supp = Convert.ToInt32(reader2["nzp_supp"]);
                        reader2.Close();
                        if (nzp_supp > 0)
                        {
                            sql = "select nzp_area from " + tables.area + " where nzp_supp = " + nzp_supp;
                            ret = ExecRead(conn_db, transaction, out reader2, sql, true);
                            if (!ret.result)
                            {
                                //     if (transaction != null) transaction.Rollback();
                                reader.Close();
                                reader.Dispose();
                                //conn_db.Close();
                                return ret;
                            }
                            if (reader2.Read())
                                if (reader2["nzp_area"] != DBNull.Value) nzp_area = Convert.ToInt32(reader2["nzp_area"]);
                            reader2.Close();
                            reader2.Dispose();
                        }
                    }

                    if (nzp_area > 0)
                    {
                        sql = "update " + tables.file_area + " set nzp_area = " + nzp_area + " where id = " + fileArea.id;
                        ret = ExecSQL(conn_db, transaction, sql, true);
                        if (!ret.result)
                        {
                            if (transaction != null) transaction.Rollback();

                            reader.Close();
                            reader.Dispose();
                            // conn_db.Close();
                            return ret;
                        }
                        counter_update++;
                    }

                }
                reader.Close();
                reader.Dispose();

                if (transaction != null) transaction.Commit();

                if (add) ret.text = "Добавлено " + counter_insert + " из " + counter;
                else ret.text = "Обновлено " + counter_update + " из " + counter;
            }
            catch (Exception ex)
            {
                CloseReader(ref reader);
                conn_db.Close();

                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;

                MonitorLog.WriteLog("Ошибка загрузки Управляющих организаций UploadAreaInDb " + err, MonitorLog.typelog.Error, 20, 201, true);
                return ret;
            }

            return ret;
        }

        private int AddAreaInDb(IDbConnection conn_db, IDbTransaction transaction, FileArea fileArea, FilesImported finder, out Returns ret)
        {
            DbSprav db = new DbSprav();
            Supplier supp = new Supplier();
            supp.name_supp = fileArea.area;
            supp.nzp_user = finder.nzp_user;
            ret = db.SaveSupplier(supp, transaction, conn_db);
            if (!ret.result) return 0;
            int nzp_supp = ret.tag;
            if (nzp_supp <= 0)
            {
                ret.result = false;
                return 0;
            }

            Payer payer = new Payer();
            payer.payer = payer.npayer = fileArea.area;
            payer.nzp_user = finder.nzp_user;
            payer.inn = fileArea.inn;
            payer.kpp = fileArea.kpp;
            payer.nzp_supp = nzp_supp;
            payer.nzp_type = Payer.ContragentTypes.UK.GetHashCode();
            payer.is_erc = 1;
            ret = db.SavePayer(payer, transaction, conn_db);
            if (!ret.result) return 0;
            int nzp_payer = ret.tag;
            if (nzp_payer <= 0)
            {
                ret.result = false;
                return 0;
            }

            DbAdres dba = new DbAdres();
            Area area = new Area();
            area.nzp_supp = nzp_supp;
            area.nzp_user = finder.nzp_user;
            area.area = fileArea.area;
            ret = dba.SaveArea(area, transaction, conn_db);
            if (!ret.result) return 0;

            return ret.tag;
        }

        public Returns UploadDomInDb(FilesImported finder)
        {
            return UploadDomInDb(finder, true);
        }

        public Returns UploadDomInDb(FilesImported finder, bool add)
        {
            Returns ret = Utils.InitReturns();

            string connectionString = Points.GetConnByPref(Points.Pref);
            IDbConnection conn_db = GetConnection(connectionString);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return ret;
            UploadDomInDb(finder, true, conn_db);
            return ret;
        }


        public Returns UploadDomInDb(FilesImported finder, bool add, IDbConnection conn_db)
        {
            Returns ret = Utils.InitReturns();

            // string connectionString = Points.GetConnByPref(Points.Pref);
            // IDbConnection conn_db = GetConnection(connectionString);
            // ret = OpenDb(conn_db, true);
            // if (!ret.result) return ret;

            DbTables tables = new DbTables(conn_db);
            IDataReader reader = null, reader2, reader3;

            if (Points.PointList.Count > 0)
            {
                finder.pref = Points.PointList[0].pref;
                finder.nzp_wp = Points.PointList[0].nzp_wp;
            }
            string where = "";
            //  if (add)
            //  {
            //      where += " and nvl(nzp_dom,0) = 0";
            //  }
#if PG
            where += " and coalesce(nzp_dom,0) = 0";
#else
            where += " and nvl(nzp_dom,0) = 0";
#endif

            string sql = "select id, ukds, town, rajon, ulica, ndom, nkor, area_id, " +
                         "cat_blago, etazh, build_year, total_square, mop_square, useful_square, mo_id, " +
                         "params, ls_row_number, odpu_row_number, nzp_ul, nzp_dom, nzp_raj, nzp_town " +
                         "from " + tables.file_dom + " where nzp_file = " + finder.nzp_file + where;
            ret = ExecRead(conn_db, out reader, sql, true);
            if (!ret.result)
            {
                //   conn_db.Close();
                return ret;
            }

            int nzp_area = 0;
            int counter_update = 0, counter_insert = 0, counter = 0;
            try
            {
                while (reader.Read())
                {
                    bool seachUlica = false;

                    counter++;
                    FileDom fileDom = new FileDom();
                    if (reader["id"] != DBNull.Value) fileDom.id = Convert.ToDecimal(reader["id"]);
                    if (reader["ukds"] != DBNull.Value) fileDom.ukds = Convert.ToInt32(reader["ukds"]);
                    if (reader["town"] != DBNull.Value) fileDom.town = Convert.ToString(reader["town"]).Trim();
                    if (reader["rajon"] != DBNull.Value) fileDom.rajon = Convert.ToString(reader["rajon"]).Trim();
                    if (reader["ulica"] != DBNull.Value) fileDom.ulica = Convert.ToString(reader["ulica"]).Trim();
                    if (reader["ndom"] != DBNull.Value) fileDom.ndom = Convert.ToString(reader["ndom"]).Trim();
                    if (reader["nkor"] != DBNull.Value) fileDom.nkor = Convert.ToString(reader["nkor"]).Trim();
                    if (reader["area_id"] != DBNull.Value) fileDom.area_id = Convert.ToDecimal(reader["area_id"]);
                    if (reader["cat_blago"] != DBNull.Value) fileDom.cat_blago = Convert.ToString(reader["cat_blago"]).Trim();
                    if (reader["etazh"] != DBNull.Value) fileDom.etazh = Convert.ToInt32(reader["etazh"]);
                    if (reader["build_year"] != DBNull.Value) fileDom.build_year = Convert.ToDateTime(reader["build_year"]).ToShortDateString();
                    if (reader["total_square"] != DBNull.Value) fileDom.total_square = Convert.ToDecimal(reader["total_square"]);
                    if (reader["mop_square"] != DBNull.Value) fileDom.mop_square = Convert.ToDecimal(reader["mop_square"]);
                    if (reader["useful_square"] != DBNull.Value) fileDom.useful_square = Convert.ToDecimal(reader["useful_square"]);
                    if (reader["mo_id"] != DBNull.Value) fileDom.mo_id = Convert.ToDecimal(reader["mo_id"]);
                    if (reader["params"] != DBNull.Value) fileDom.params_ = Convert.ToString(reader["params"]).Trim();
                    if (reader["ls_row_number"] != DBNull.Value) fileDom.ls_row_number = Convert.ToInt32(reader["ls_row_number"]);
                    if (reader["odpu_row_number"] != DBNull.Value) fileDom.odpu_row_number = Convert.ToInt32(reader["odpu_row_number"]);
                    if (reader["nzp_ul"] != DBNull.Value) fileDom.nzp_ul = Convert.ToInt32(reader["nzp_ul"]);
                    if (reader["nzp_dom"] != DBNull.Value) fileDom.nzp_dom = Convert.ToInt32(reader["nzp_dom"]);
                    if (reader["nzp_raj"] != DBNull.Value) fileDom.nzp_raj = Convert.ToInt32(reader["nzp_raj"]);
                    if (reader["nzp_town"] != DBNull.Value) fileDom.nzp_raj = Convert.ToInt32(reader["nzp_town"]);

                    if (fileDom.area_id <= 0)
                    {
                        UpdateCommentIntoFileDom(conn_db, fileDom.id, "Поле area_id должно быть заполнено");
                        continue;
                    }

                    #region определить nzp_area
                    sql = "select nzp_area from " + tables.file_area + " where id = " + fileDom.area_id;
                    ret = ExecRead(conn_db, out reader2, sql, true);
                    if (!ret.result)
                    {
                        UpdateCommentIntoFileDom(conn_db, fileDom.id, "Не удалось получить nzp_area из таблицы file_area");
                        continue;
                    }
                    nzp_area = 0;
                    if (reader2.Read()) if (reader2["nzp_area"] != DBNull.Value) nzp_area = Convert.ToInt32(reader2["nzp_area"]);
                    reader2.Close();
                    if (nzp_area <= 0)
                    {
                        UpdateCommentIntoFileDom(conn_db, fileDom.id, "Не удалось получить nzp_area из таблицы file_area");
                        continue;
                    }
                    #endregion

                    if (add)
                    {
                        AddDomInDb(conn_db, nzp_area, finder, fileDom, out ret);
                        counter_insert++;
                        continue;
                    }
                    else
                    {
                        if (fileDom.ukds > 0)
                        {
                            bool is_continue = false;
                            foreach (_Point point in Points.PointList)
                            {
                                #region найти nzp_dom (и nzp_area для него - loc_nzp_area) по ukds
#if PG
                                string pref = point.pref + "_data.";
#else
                                string pref = point.pref + "_data@" + DBManager.getServer(conn_db) + ":";
#endif
                                sql = "select nzp from " + pref + "prm_4 where nzp_prm = 866 and trim(val_prm) = " + fileDom.ukds + " " +
#if PG
                                     "and now() " +
#else
                                    "and current "+
#endif
                                      " between dat_s and dat_po and is_actual != 100";
                                ret = ExecRead(conn_db, out reader2, sql, true);
                                if (!ret.result)
                                {
                                    UpdateCommentIntoFileDom(conn_db, fileDom.id, "Ошибка поиска дома по ukds");
                                    is_continue = true;
                                    break;
                                }

                                int loc_nzp_area = 0;
                                while (reader2.Read())
                                {
                                    fileDom.nzp_dom = 0;
                                    if (reader2["nzp"] != DBNull.Value) fileDom.nzp_dom = Convert.ToInt32(reader2["nzp"]);

                                    #region определить nzp_area для дома loc_nzp_area
                                    sql = "select nzp_area from " + tables.dom + " where nzp_dom =" + fileDom.nzp_dom;
                                    ret = ExecRead(conn_db, out reader3, sql, true);
                                    if (!ret.result)
                                    {
                                        UpdateCommentIntoFileDom(conn_db, fileDom.id, "Ошибка при получении nzp_area для дома nzp_dom = " + fileDom.nzp_dom);
                                        is_continue = true;
                                        break;
                                    }
                                    loc_nzp_area = 0;
                                    int i = 0;
                                    while (reader3.Read())
                                    {
                                        if (i > 0)
                                        {
                                            is_continue = true;
                                            UpdateCommentIntoFileDom(conn_db, fileDom.id, "Для ukds = " + fileDom.ukds + " несколько домов");
                                            break;
                                        }
                                        if (reader3["nzp_area"] != DBNull.Value) loc_nzp_area = Convert.ToInt32(reader3["nzp_area"]);
                                        i++;
                                    }
                                    reader3.Close();
                                    #endregion

                                    if (is_continue || loc_nzp_area == nzp_area) break;
                                    else fileDom.nzp_dom = 0;
                                }
                                reader2.Close();
                                #endregion

                                if (fileDom.nzp_dom > 0 && loc_nzp_area == nzp_area)
                                {
                                    finder.pref = point.pref;
                                    break;
                                }
                                else seachUlica = true;
                            }
                            if (is_continue) continue;

                            fileDom.nzp_ul = 0;
                            if (fileDom.nzp_dom > 0)
                            {
                                #region найти улицу для nzp_dom и nzp_area
#if PG
                                string pref = finder.pref + "_data.";
#else
                                string pref = finder.pref + "_data@" + DBManager.getServer(conn_db) + ":";
#endif
                                sql = "select nzp_ul from " + pref + "dom where nzp_dom = " + fileDom.nzp_dom + " and nzp_area = " + nzp_area;
                                ret = ExecRead(conn_db, out reader3, sql, true);
                                if (!ret.result)
                                {
                                    UpdateCommentIntoFileDom(conn_db, fileDom.id, "Ошибка при проверке существования дома nzp_dom = " + fileDom.nzp_dom);
                                    continue;
                                }

                                if (reader3.Read()) if (reader3["nzp_ul"] != DBNull.Value) fileDom.nzp_ul = Convert.ToInt32(reader3["nzp_ul"]);
                                reader3.Close();
                                #endregion
                            }

                            if (fileDom.nzp_ul > 0)
                            {
                                counter_update++;
                                UpdateNzpIntoFileDom(conn_db, fileDom.id, fileDom.nzp_ul, fileDom.nzp_dom);
                                continue;
                            }
                            else seachUlica = true;
                        }
                        else seachUlica = true;//если ukds не заполнен                   

                        if (seachUlica)
                        {

                            #region найти улицу
                            fileDom.nzp_ul = FindUl(conn_db, ref fileDom, out ret);
                            if (!ret.result)
                            {
                                UpdateCommentIntoFileDom(conn_db, fileDom.id, "Ошибка в запросе при поиске улицы " + fileDom.ulica);
                                continue;
                            }
                            #endregion

                            if (fileDom.nzp_ul > 0)
                            {
                                #region найти дом
                                Dom d = FindDom(conn_db, fileDom, nzp_area, out ret, 1);
                                fileDom.nzp_dom = d.nzp_dom;
                                finder.pref = d.pref;
                                if (!ret.result)
                                {
                                    UpdateCommentIntoFileDom(conn_db, fileDom.id, "Ошибка в запросе при поиске дома №" + fileDom.ndom);
                                    continue;
                                }
                                #endregion

                                if (fileDom.nzp_dom > 0)
                                {
                                    counter_update++;
                                    UpdateNzpIntoFileDom(conn_db, fileDom.id, fileDom.nzp_ul, fileDom.nzp_dom);
                                    if (fileDom.ukds > 0) UpdatePrm4Ukds(conn_db, finder, fileDom);
                                    continue;
                                }
                            }
                            else
                            {
                                UpdateCommentIntoFileDom(conn_db, fileDom.id, "Не найдена улица " + fileDom.ulica);
                                continue;
                            }

                        }
                    }
                }
                reader.Close();
            }
            catch (Exception ex)
            {
                CloseReader(ref reader);
                //   conn_db.Close();

                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;

                MonitorLog.WriteLog("Ошибка загрузки домов UploadDomInDb " + err, MonitorLog.typelog.Error, 20, 201, true);
                return ret;
            }
            // conn_db.Close();
            if (add) ret.text = "Добавлено " + counter_insert + " из " + counter;
            else ret.text = "Обновлено " + counter_update + " из " + counter;
            return ret;
        }

        #region для UploadDomInDb
        private int AddDomInDb(IDbConnection conn_db, int nzp_area, FilesImported finder, FileDom fileDom, out Returns ret)
        {
            IDataReader reader2 = null;
            DbTables tables = new DbTables(conn_db);
            Dom dom = new Dom();
            dom.pref = Points.PointList[0].pref;
            dom.nzp_wp = Points.PointList[0].nzp_wp;

            #region определить nzp_stat
            string sql = "select nzp_stat from " + tables.town + " where nzp_town = " + fileDom.nzp_town;
            ret = ExecRead(conn_db, out reader2, sql, true);
            if (!ret.result)
            {
                UpdateCommentIntoFileDom(conn_db, fileDom.id, "Ошибка при добавлении дома при определении nzp_stat");
                return 0;
            }
            if (reader2.Read()) if (reader2["nzp_stat"] != DBNull.Value) dom.nzp_stat = Convert.ToInt32(reader2["nzp_stat"]);
            reader2.Close();
            #endregion

            #region определить nzp_land
            sql = "select nzp_land from " + tables.stat + " where nzp_stat = " + dom.nzp_stat;
            ret = ExecRead(conn_db, out reader2, sql, true);
            if (!ret.result)
            {
                UpdateCommentIntoFileDom(conn_db, fileDom.id, "Ошибка при добавлении дома при определении nzp_land");
                return 0;
            }
            if (reader2.Read()) if (reader2["nzp_land"] != DBNull.Value) dom.nzp_land = Convert.ToInt32(reader2["nzp_land"]);
            reader2.Close();
            #endregion


            dom.nkor = fileDom.nkor;
            if (fileDom.nkor.Trim() == "") dom.nkor = "-";
            dom.nzp_ul = fileDom.nzp_ul;
            dom.nzp_area = nzp_area;
            dom.ndom = fileDom.ndom;
            dom.nzp_user = finder.nzp_user;
            dom.nzp_raj = fileDom.nzp_raj;
            DbAdres db = new DbAdres();
            fileDom.nzp_dom = db.Update(conn_db, dom, out ret);
            if (!ret.result)
            {
                if (fileDom.nzp_dom > 0)
                {
                    // все нормально , такой дом существует нужно его использовать 
                }
                else
                {
                    UpdateCommentIntoFileDom(conn_db, fileDom.id, "Ошибка при добавлении дома");
                    return 0;
                }
            }
            UpdateNzpIntoFileDom(conn_db, fileDom.id, fileDom.nzp_ul, fileDom.nzp_dom);
            if (fileDom.ukds > 0) UpdatePrm4Ukds(conn_db, finder, fileDom);

            return fileDom.nzp_dom;
        }

        private void UpdatePrm4Ukds(IDbConnection conn_db, FilesImported file, FileDom dom)
        {
            DbParameters db = new DbParameters();
            Param finder = new Param();
            finder.nzp_user = file.nzp_user;
            finder.pref = file.pref;
            finder.webLogin = file.webLogin;
            finder.webUname = file.webUname;
            finder.dat_s = "1." + DateTime.Now.Month.ToString() + "." + DateTime.Now.Year.ToString();
            finder.nzp_prm = 866;
            finder.val_prm = dom.ukds.ToString();
            finder.prm_num = 4;
            finder.nzp = dom.nzp_dom;

            Returns ret = db.SavePrm(conn_db, null, finder);
            if (!ret.result)
            {
                UpdateCommentIntoFileDom(conn_db, dom.id, "Ошибка при добавлении параметра ukds");
            }
        }

        private void UpdateCommentIntoFileDom(IDbConnection conn_db, decimal id, string text)
        {
#if PG
            string pref_data = Points.Pref + "_data.";
#else
            string pref_data = Points.Pref + "_data@" + DBManager.getServer(conn_db) + ":";
#endif
            string sql = "update " + pref_data + "file_dom set comment = '" + text + "' where id = " + id;
            ExecSQL(conn_db, sql, true);
        }

        private void UpdateNzpIntoFileDom(IDbConnection conn_db, decimal id, int nzp_ul, int nzp_dom)
        {
#if PG
            string pref_data = Points.Pref + "_data.";
#else
            string pref_data = Points.Pref + "_data@" + DBManager.getServer(conn_db) + ":";
#endif
            string sql = "update " + pref_data + "file_dom set nzp_ul = " + nzp_ul + " , nzp_dom = " + nzp_dom + ", comment='' where id = " + id;
            ExecSQL(conn_db, sql, true);
        }

        private int FindUl(IDbConnection conn_db, ref FileDom finder, out Returns ret)
        {
#if PG
            string pref_data = Points.Pref + "_data.";
#else
            string pref_data = Points.Pref + "_data@" + DBManager.getServer(conn_db) + ":";
#endif
            string sql = "select nzp_town from " + pref_data + "s_town where upper(trim(town)) = " + Utils.EStrNull(finder.town.ToUpper().Trim().Replace(".", ""));

            IDataReader reader1;
            ret = ExecRead(conn_db, out reader1, sql, true);
            if (!ret.result)
            {
                // проверить на менее жесткие условия 
                reader1.Close();
                sql = "select nzp_town from " + pref_data + "s_town where upper(trim(replace(town,' Г','')) = " + Utils.EStrNull(finder.town.ToUpper().Trim().Replace(" Г", ""));
                ret = ExecRead(conn_db, out reader1, sql, true);
                return 0;
            }
            if (reader1.Read()) if (reader1["nzp_town"] != DBNull.Value) finder.nzp_town = Convert.ToInt32(reader1["nzp_town"]);
            reader1.Close();
            if (finder.nzp_town <= 0) return 0;

            string rr;
            if (Utils.EStrNull(finder.rajon.ToUpper().Trim()) == " NULL ") { rr = "'-'"; }
            else { rr = Utils.EStrNull(finder.rajon.ToUpper().Trim()); };
            sql = "select nzp_raj from " + pref_data + "s_rajon where nzp_town = " + finder.nzp_town + " and upper(trim(rajon)) = " + rr;
            ret = ExecRead(conn_db, out reader1, sql, true);
            if (!ret.result) return 0;
            if (reader1.Read()) if (reader1["nzp_raj"] != DBNull.Value) finder.nzp_raj = Convert.ToInt32(reader1["nzp_raj"]);
            reader1.Close();
            if (finder.nzp_raj <= 0) return 0;

            sql = "select nzp_ul from " + pref_data + "s_ulica where nzp_raj = " + finder.nzp_raj + " and upper(trim(ulica)) = " + Utils.EStrNull(finder.ulica.ToUpper().Trim());
            ret = ExecRead(conn_db, out reader1, sql, true);
            if (!ret.result) return 0;
            if (reader1.Read()) if (reader1["nzp_ul"] != DBNull.Value) finder.nzp_ul = Convert.ToInt32(reader1["nzp_ul"]);
            reader1.Close();

            if (finder.nzp_ul <= 0)
            {
                sql = "select nzp_ul from " + pref_data + "s_ulica where nzp_raj = " + finder.nzp_raj + " and replace(replace(upper(trim(ulica)),' ПР-КТ',' ПР'),' (П.ТРОИЦКИЙ)','') = " +
                   "replace(replace(" + Utils.EStrNull(finder.ulica.ToUpper().Trim()) + ",' (П.ТРОИЦКИЙ)',''),'ПАРКОВАЯ УЛ' ,'ПАРКОВАЯ УЛ') ";
                ret = ExecRead(conn_db, out reader1, sql, true);
                if (!ret.result) return 0;
                if (reader1.Read()) if (reader1["nzp_ul"] != DBNull.Value) finder.nzp_ul = Convert.ToInt32(reader1["nzp_ul"]);
                reader1.Close();
                if (finder.nzp_ul <= 0)
                {
                    sql = "select nzp_ul from " + pref_data + "s_ulica where nzp_raj = " + finder.nzp_raj +
                        " and replace(replace(upper(trim(ulica)),' УЛ','') ,' (П.ТРОИЦКИЙ)','')=replace(replace( "
                    + Utils.EStrNull(finder.ulica.ToUpper().Trim()) + ",' УЛ',''),' (П.ТРОИЦКИЙ)','')";
                    ret = ExecRead(conn_db, out reader1, sql, true);
                    if (!ret.result) return 0;
                    if (reader1.Read()) if (reader1["nzp_ul"] != DBNull.Value) finder.nzp_ul = Convert.ToInt32(reader1["nzp_ul"]);
                    reader1.Close();
                }
            }

            return finder.nzp_ul;
        }

        private Dom FindDom(IDbConnection conn_db, FileDom finder, int nzp_area, out Returns ret)
        {
            Dom d = new Dom();
#if PG
            string pref_data = Points.Pref + "_data.";
#else
            string pref_data = Points.Pref + "_data@" + DBManager.getServer(conn_db) + ":";
#endif
            string nkor = finder.nkor.Trim().ToUpper();
            if (finder.nkor.Trim() == "-") nkor = "";

            string sql = "select nzp_dom, pref from " + pref_data + "dom where nzp_ul = " + finder.nzp_ul +
                         " and nzp_area = " + nzp_area + " and trim(upper(ndom)) = '" + finder.ndom.Trim().ToUpper() + "'" +
#if PG
                            " and case when coalesce(nkor,'0') = '-' then '' else trim(upper(nkor)) end = '" + nkor + "'";
#else
                         " and case when nvl(nkor,'0') = '-' then '' else trim(upper(nkor)) end = '" + nkor + "'";
#endif
            IDataReader reader1;
            ret = ExecRead(conn_db, out reader1, sql, true);
            if (!ret.result) return d;

            if (reader1.Read())
            {
                if (reader1["nzp_dom"] != DBNull.Value) d.nzp_dom = Convert.ToInt32(reader1["nzp_dom"]);
                if (reader1["pref"] != DBNull.Value) d.pref = Convert.ToString(reader1["pref"]);
            }
            reader1.Close();
            return d;
        }

        private Dom FindDom(IDbConnection conn_db, FileDom finder, int nzp_area, out Returns ret, int pmode)
        {
            if (pmode == 0) { ret.result = true; };
            Dom d = new Dom();
#if PG
            string pref_data = Points.Pref + "_data.";
#else
            string pref_data = Points.Pref + "_data@" + DBManager.getServer(conn_db) + ":";
#endif
            string nkor = finder.nkor.Trim().ToUpper();
            //if (finder.nkor.Trim() == "-") nkor = "";
            if (nzp_area > 0)
            {
                string sql = "select nzp_dom, pref from " + pref_data + "dom where nzp_ul = " + finder.nzp_ul +
                 " and trim(upper(ndom)) = '" + finder.ndom.Trim().ToUpper() + "'" +
#if PG
                " and coalesce(trim(upper(nkor)),'-')=coalesce( '" + nkor + "','-') and nzp_area =" + nzp_area.ToString();
#else
 " and nvl(trim(upper(nkor)),'-')=nvl( '" + nkor + "','-') and nzp_area =" + nzp_area.ToString();
#endif
                IDataReader reader1;
                ret = ExecRead(conn_db, out reader1, sql, true);
                if (!ret.result) return d;

                if (reader1.Read())
                {
                    if (reader1["nzp_dom"] != DBNull.Value) d.nzp_dom = Convert.ToInt32(reader1["nzp_dom"]);
                    if (reader1["pref"] != DBNull.Value) d.pref = Convert.ToString(reader1["pref"]);
                }
                reader1.Close();
                if (d.nzp_dom > 0) { return d; };
            }
            if (nkor == "") { nkor = "-"; };
            string sql1 = "select nzp_dom, pref from " + pref_data + "dom where nzp_ul = " + finder.nzp_ul +
                         " and trim(upper(ndom)) = '" + finder.ndom.Trim().ToUpper() + "'" +
#if PG
                            " and coalesce(trim(upper(nkor)),'-')=coalesce( '" + nkor + "','-')";
#else
 " and nvl(trim(upper(nkor)),'-')=nvl( '" + nkor + "','-')";
#endif
            IDataReader reader11;
            ret = ExecRead(conn_db, out reader11, sql1, true);
            if (!ret.result) return d;

            if (reader11.Read())
            {
                if (reader11["nzp_dom"] != DBNull.Value) d.nzp_dom = Convert.ToInt32(reader11["nzp_dom"]);
                if (reader11["pref"] != DBNull.Value) d.pref = Convert.ToString(reader11["pref"]);
            }
            reader11.Close();
            return d;

        }
        #endregion

        public Returns UploadLsInDb(FilesImported finder)
        {
            return UploadLsInDb(finder, true);
        }

        public Returns UploadLsInDb(FilesImported finder, bool add)
        {
            string connectionString = Points.GetConnByPref(Points.Pref);
            IDbConnection conn_db = GetConnection(connectionString);
            Returns ret = OpenDb(conn_db, true);
            if (!ret.result) return ret;

            ret = UploadLsInDb(conn_db, finder, add);

            conn_db.Close();

            return ret;
        }
        public Returns UploadLsInDb(IDbConnection conn_db, FilesImported finder, bool add)
        {
            Returns ret = Utils.InitReturns();

            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result)
            {
                return ret;
            }
            ret = UploadLsInDb(conn_db, conn_web, finder, add);
            conn_web.Close();
            return ret;
        }

        public Returns UploadLsInDb(IDbConnection conn_db, IDbConnection conn_web, FilesImported finder, bool add)
        {
            Returns ret = Utils.InitReturns();
            /*
            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result)
            {
                return ret;
            }
            */
#if PG
            string pref_data = Points.Pref + "_data.";
#else
 string pref_data = Points.Pref + "_data@" + DBManager.getServer(conn_db) + ":";
#endif
            IDataReader reader = null, reader2;
            string where = "";
            if (add)
            {
#if PG
                where += " and coalesce(nzp_kvar,0) = 0";
#else
                where += " and nvl(nzp_kvar,0) = 0";
#endif
            }
            string sql = "select id, ukas, dom_id, nkvar, nkvar_n, ls_type, fam, ima, otch from " +
#if PG
                        pref_data + "file_kvar  where nzp_kvar is null and length(trim(coalesce(comment,' ')))=0 and nzp_file = " + finder.nzp_file + where;
#else
 pref_data + "file_kvar  where nzp_kvar is null and length(trim(nvl(comment,' ')))=0 and nzp_file = " + finder.nzp_file + where;
#endif
            ret = ExecRead(conn_db, out reader, sql, true);
            if (!ret.result)
            {
                return ret;
            }

            int counter_update = 0, counter_insert = 0, counter = 0;
            int nzp_dom = 0;
            bool searchLs;
            try
            {
                while (reader.Read())
                {
                    counter++;

                    FileKvar fileKvar = new FileKvar();
                    if (reader["id"] != DBNull.Value) fileKvar.id = Convert.ToDecimal(reader["id"]);
                    if (reader["ukas"] != DBNull.Value) fileKvar.ukas = Convert.ToInt32(reader["ukas"]);
                    if (reader["dom_id"] != DBNull.Value) fileKvar.dom_id = Convert.ToDecimal(reader["dom_id"]);
                    if (reader["nkvar"] != DBNull.Value) fileKvar.nkvar = Convert.ToString(reader["nkvar"]).Trim();
                    if (reader["nkvar_n"] != DBNull.Value) fileKvar.nkvar_n = Convert.ToString(reader["nkvar_n"]).Trim();
                    if (reader["ls_type"] != DBNull.Value) fileKvar.ls_type = Convert.ToInt32(reader["ls_type"]);
                    if (reader["fam"] != DBNull.Value) fileKvar.fam = Convert.ToString(reader["fam"]).Trim();
                    if (reader["ima"] != DBNull.Value) fileKvar.ima = Convert.ToString(reader["ima"]).Trim();
                    if (reader["otch"] != DBNull.Value) fileKvar.otch = Convert.ToString(reader["otch"]).Trim();

                    if (fileKvar.dom_id <= 0)
                    {
                        UpdateCommentIntoFileKvar(conn_db, fileKvar.id, "Поле dom_id должно быть заполнено");

                        continue;
                    }

                    #region определить nzp_dom
                    sql = "select nzp_dom from " + pref_data + "file_dom where id = " + fileKvar.dom_id + " and nzp_file = " + finder.nzp_file;
                    ret = ExecRead(conn_db, out reader2, sql, true);
                    if (!ret.result)
                    {
                        sql = "select nzp_dom from " + pref_data + "file_dom where id = " + fileKvar.dom_id + " and nzp_dom>0 ";
                        ret = ExecRead(conn_db, out reader2, sql, true);
                        if (!ret.result)
                        {
                            UpdateCommentIntoFileKvar(conn_db, fileKvar.id, "Не удалось получить nzp_dom из таблицы file_dom");
                            continue;
                        }
                    }
                    nzp_dom = 0;
                    if (reader2.Read()) if (reader2["nzp_dom"] != DBNull.Value) nzp_dom = Convert.ToInt32(reader2["nzp_dom"]);
                    reader2.Close();
                    reader2.Dispose();
                    if (nzp_dom <= 0)
                    {
                        UpdateCommentIntoFileKvar(conn_db, fileKvar.id, "Не удалось получить nzp_dom из таблицы file_dom");

                        continue;
                    }
                    #endregion

                    #region определить pref дома finder.pref
                    sql = "select pref from " + pref_data + "dom where nzp_dom = " + nzp_dom;
                    ret = ExecRead(conn_db, out reader2, sql, true);
                    if (!ret.result)
                    {
                        UpdateCommentIntoFileKvar(conn_db, fileKvar.id, "Не удалось определить pref для nzp_dom = " + nzp_dom);

                        continue;
                    }
                    finder.pref = "";
                    if (reader2.Read()) if (reader2["pref"] != DBNull.Value) finder.pref = Convert.ToString(reader2["pref"]).Trim();
                    reader2.Close();
                    reader2.Dispose();
                    if (finder.pref == "")
                    {
                        UpdateCommentIntoFileKvar(conn_db, fileKvar.id, "Не удалось определить pref для nzp_dom = " + nzp_dom);

                        continue;
                    }
                    #endregion

                    if (add)
                    {
                        AddLsInDb(conn_db, conn_web, nzp_dom, finder, fileKvar, out ret);
                        continue;
                    }
                    else
                    {
                        searchLs = false;

                        if (fileKvar.ukas > 0)
                        {
                            bool is_continue = false;
                            searchLs = true;
                            #region найти nzp_kvar по ukas
#if PG
                            string pref = finder.pref + "_data.";
#else
                            string pref = finder.pref + "_data@" + DBManager.getServer(conn_db) + ":";
#endif
                            sql = "select nzp from " + pref + "prm_15 where nzp_prm = 162 and trim(val_prm) = " + fileKvar.ukas + " " +
#if PG
                                     "and now() between dat_s and dat_po and is_actual != 100";
#else
 "and current between dat_s and dat_po and is_actual != 100";
#endif
                            ret = ExecRead(conn_db, out reader2, sql, true);
                            if (!ret.result)
                            {
                                UpdateCommentIntoFileKvar(conn_db, fileKvar.id, "Ошибка поиска л/с по ukas");
                                is_continue = true;
                            }
                            else
                            {
                                while (reader2.Read())
                                {
                                    fileKvar.nzp_kvar = 0;
                                    if (reader2["nzp"] != DBNull.Value) fileKvar.nzp_kvar = Convert.ToInt32(reader2["nzp"]);

                                    #region проверить, что nzp соостветствует дому
                                    sql = "select count(*) from " + pref_data + "kvar where nzp_dom = " + nzp_dom + " and nzp_kvar =" + fileKvar.nzp_kvar;
                                    object count = ExecScalar(conn_db, sql, out ret, true);
                                    int records;
                                    try { records = Convert.ToInt32(count); }
                                    catch (Exception e)
                                    {
                                        UpdateCommentIntoFileKvar(conn_db, fileKvar.id, "Ошибка при определении количества записей: " + (Constants.Debug ? e.Message : ""));
                                        is_continue = true;
                                        break;
                                    }
                                    if (records > 0)
                                    {
                                        counter_update++;
                                        UpdateNzpIntoFileKvar(conn_db, fileKvar.id, fileKvar.nzp_kvar, nzp_dom);
                                        is_continue = true;
                                        searchLs = false;
                                    }
                                    #endregion
                                }
                                reader2.Close();
                                reader2.Dispose();
                            }
                            #endregion

                            if (is_continue)
                            {

                                continue;
                            }
                        }
                        else searchLs = true;//если ukas не заполнен

                        if (searchLs)
                        {
                            #region найти л/с
                            Ls ls = FindKvar(conn_db, fileKvar, nzp_dom, pref_data, out ret);
                            fileKvar.nzp_kvar = ls.nzp_kvar;
                            if (!ret.result)
                            {
                                UpdateCommentIntoFileKvar(conn_db, fileKvar.id, "Ошибка в запросе при поиске л/с кв." + fileKvar.nkvar);

                                continue;
                            }
                            #endregion

                            if (fileKvar.nzp_kvar > 0)
                            {
                                counter_update++;
                                UpdateNzpIntoFileKvar(conn_db, fileKvar.id, fileKvar.nzp_kvar, nzp_dom);

                                if (fileKvar.ukas > 0) UpdatePrm4Ukas(conn_db, finder, fileKvar);

                                continue;
                            }
                        }
                    }

                }
            }
            catch (Exception ex)
            {

                CloseReader(ref reader);
                //conn_web.Close();
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;

                MonitorLog.WriteLog("Ошибка загрузки домов UploadLsInDb " + err, MonitorLog.typelog.Error, 20, 201, true);
                return ret;
            }

            //conn_web.Close();
            if (add) ret.text = "Добавлено " + counter_insert + " из " + counter;
            else ret.text = "Обновлено " + counter_update + " из " + counter;
            return ret;
        }

        private int AddLsInDb(IDbConnection conn_db, IDbConnection conn_web, int nzp_dom, FilesImported finder, FileKvar fileKvar, out Returns ret)
        {
            Ls kvar = new Ls();

            kvar.nzp_wp = Points.GetPoint(finder.pref).nzp_wp;
            kvar.pref = finder.pref;
            DbTables tables = new DbTables(conn_db);
            IDataReader reader2;
            #region определить nzp_area
            string sql = "select nzp_area, nzp_geu from " + tables.dom + " where nzp_dom = " + nzp_dom;
            ret = ExecRead(conn_db, out reader2, sql, true);
            if (!ret.result)
            {
                UpdateCommentIntoFileKvar(conn_db, fileKvar.id, "Ошибка при добавлении л/с при определении nzp_area");
                return 0;
            }
            if (reader2.Read())
            {
                if (reader2["nzp_area"] != DBNull.Value) kvar.nzp_area = Convert.ToInt32(reader2["nzp_area"]);
                if (reader2["nzp_geu"] != DBNull.Value && Convert.ToInt32(reader2["nzp_geu"]) > 0) kvar.nzp_geu = Convert.ToInt32(reader2["nzp_geu"]);
            }
            reader2.Close();
            reader2.Dispose();
            #endregion

            kvar.nkvar = fileKvar.nkvar;
            kvar.nkvar_n = fileKvar.nkvar_n;

            kvar.nzp_dom = nzp_dom;
            kvar.nzp_user = finder.nzp_user;
            kvar.fio = fileKvar.fam;
            if (fileKvar.ima != "") kvar.fio += " " + fileKvar.ima;
            if (fileKvar.otch != "") kvar.fio += " " + fileKvar.otch;
            kvar.chekexistls = 0;

            if (fileKvar.ls_type == 1) kvar.typek = 1;
            else if (fileKvar.ls_type == 2) kvar.typek = 3;
            else
            {
                UpdateCommentIntoFileKvar(conn_db, fileKvar.id, "Поле ls_type может быть 1 или 2");
                return 0;
            }
            // kvar.typek
            kvar.stateID = Ls.States.Open.GetHashCode();
            DbAdres db = new DbAdres();


            // IDbTransaction transaction =  conn_db.BeginTransaction();
            // transaction = conn_db.BeginTransaction();

            fileKvar.nzp_kvar = db.Update(conn_db, null, conn_web, kvar, out ret);
            if (!ret.result)
            {
                UpdateCommentIntoFileDom(conn_db, fileKvar.id, "Ошибка при добавлении л/с");
                return 0;
            }

            UpdateNzpIntoFileKvar(conn_db, fileKvar.id, fileKvar.nzp_kvar, nzp_dom);
            if (fileKvar.ukas > 0) UpdatePrm4Ukas(conn_db, finder, fileKvar);



            return fileKvar.nzp_kvar;
        }

        #region Функции для UploadLsInDb
        private void UpdateCommentIntoFileKvar(IDbConnection conn_db, decimal id, string text)
        {
#if PG
            string pref_data = Points.Pref + "_data.";
#else
            string pref_data = Points.Pref + "_data@" + DBManager.getServer(conn_db) + ":";
#endif
            string sql = "update " + pref_data + "file_kvar set comment = '" + text + "' where id = " + id;
            ExecSQL(conn_db, sql, true);
        }

        private void UpdateNzpIntoFileKvar(IDbConnection conn_db, decimal id, int nzp_kvar, int nzp_dom)
        {
#if PG
            string pref_data = Points.Pref + "_data.";
#else
            string pref_data = Points.Pref + "_data@" + DBManager.getServer(conn_db) + ":";
#endif
            string sql = "update " + pref_data + "file_kvar set nzp_kvar = " + nzp_kvar +
                ", nzp_dom = " + nzp_dom + ", comment='' where id = " + id;
            ExecSQL(conn_db, sql, true);
        }

        private Ls FindKvar(IDbConnection conn_db, FileKvar finder, int nzp_dom, string pref, out Returns ret)
        {
            Ls ls = new Ls();

            string nkvar_n = finder.nkvar_n.Trim().ToUpper();
            if (finder.nkvar_n.Trim() == "-") nkvar_n = "";

            string sql = "select nzp_kvar from " + pref + "kvar where nzp_dom = " + nzp_dom +
                         " and trim(upper(nkvar)) = '" + finder.nkvar.Trim().ToUpper() + "'" +
#if PG
                         " and case when coalesce(nkvar_n,'0') = '-' then '' else trim(upper(nkvar_n)) end = '" + nkvar_n + "'";
#else
 " and case when nvl(nkvar_n,'0') = '-' then '' else trim(upper(nkvar_n)) end = '" + nkvar_n + "'";
#endif
            IDataReader reader1;
            ret = ExecRead(conn_db, out reader1, sql, true);
            if (!ret.result) return ls;

            if (reader1.Read())
                if (reader1["nzp_kvar"] != DBNull.Value) ls.nzp_kvar = Convert.ToInt32(reader1["nzp_kvar"]);
            reader1.Close();
            reader1.Dispose();
            return ls;
        }

        private void UpdatePrm4Ukas(IDbConnection conn_db, FilesImported file, FileKvar kvar)
        {
            DbParameters db = new DbParameters();
            Param finder = new Param();
            finder.nzp_user = file.nzp_user;
            finder.pref = file.pref;
            finder.webLogin = file.webLogin;
            finder.webUname = file.webUname;
            finder.dat_s = "1." + DateTime.Now.Month.ToString() + "." + DateTime.Now.Year.ToString();
            finder.nzp_prm = 162;
            finder.val_prm = kvar.ukas.ToString();
            finder.prm_num = 15;
            finder.nzp = kvar.nzp_kvar;

            Returns ret = db.SavePrm(conn_db, null, finder);
            if (!ret.result)
            {
                UpdateCommentIntoFileKvar(conn_db, kvar.id, "Ошибка при добавлении параметра ukas");
            }
        }
        #endregion

        public Returns UploadSuppInDb(FilesImported finder)
        {
            return UploadSuppInDb(finder, false);
        }

        public Returns UploadSuppInDb(FilesImported finder, bool add)
        {
            Returns ret = Utils.InitReturns();

            string connectionString = Points.GetConnByPref(Points.Pref);
            IDbConnection conn_db = GetConnection(connectionString);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return ret;

            DbTables tables = new DbTables(conn_db);
            string where = "";
            if (add)
            {
#if PG
                where += " and coalesce(nzp_supp,0) = 0";
#else
                where += " and nvl(nzp_supp,0) = 0";
#endif
            }
            string sql = "select * from " + tables.file_supp + " where nzp_file = " + finder.nzp_file + where;
            IDataReader reader = null, reader2;
            int counter_update = 0, counter_insert = 0, counter = 0;
            try
            {
                ret = ExecRead(conn_db, out reader, sql, true);
                if (!ret.result)
                {
                    conn_db.Close();
                    return ret;
                }

                while (reader.Read())
                {
                    counter++;
                    FileSupp fileSupp = new FileSupp();
                    if (reader["id"] != DBNull.Value) fileSupp.id = Convert.ToInt32(reader["id"]);
                    if (reader["supp_name"] != DBNull.Value) fileSupp.supp_name = Convert.ToString(reader["supp_name"]).Trim();
                    if (reader["inn"] != DBNull.Value) fileSupp.inn = Convert.ToString(reader["inn"]).Trim();
                    if (reader["kpp"] != DBNull.Value) fileSupp.kpp = Convert.ToString(reader["kpp"]).Trim();

                    if (add)
                    {
                        fileSupp.nzp_supp = AddSuppInDb(conn_db, fileSupp, finder, out ret);
                        counter_insert++;
                    }
                    else
                    {
                        sql = "select nzp_supp from " + tables.payer + " where trim(inn) = " + fileSupp.inn + " and trim(kpp) = " + fileSupp.kpp;
                        ret = ExecRead(conn_db, out reader2, sql, true);
                        if (!ret.result)
                        {
                            UpdateCommentIntoFileSupp(conn_db, fileSupp.id, "Ошибка  при определении кода поставщика по инн и кпп");
                            continue;
                        }
                        if (reader2.Read()) if (reader2["nzp_supp"] != DBNull.Value) fileSupp.nzp_supp = Convert.ToInt32(reader2["nzp_supp"]);
                        reader2.Close();
                        reader2.Dispose();
                    }

                    if (fileSupp.nzp_supp > 0)
                    {
                        sql = "update " + tables.file_supp + " set nzp_supp = " + fileSupp.nzp_supp + " where id = " + fileSupp.id;
                        ret = ExecSQL(conn_db, sql, true);
                        if (!ret.result)
                        {
                            UpdateCommentIntoFileSupp(conn_db, fileSupp.id, "Ошибка  при обновлении поля nzp_supp = " + fileSupp.nzp_supp);
                            continue;
                        }
                        counter_update++;
                    }
                }
                reader.Close();
                reader.Dispose();
            }
            catch (Exception ex)
            {
                CloseReader(ref reader);
                conn_db.Close();
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;

                MonitorLog.WriteLog("Ошибка загрузки домов UploadSuppInDb " + err, MonitorLog.typelog.Error, 20, 201, true);
                return ret;
            }
            if (add) ret.text = "Добавлено " + counter_insert + " из " + counter;
            else ret.text = "Обновлено " + counter_update + " из " + counter;
            return ret;
        }

        private void UpdateCommentIntoFileSupp(IDbConnection conn_db, decimal id, string text)
        {
            DbTables tables = new DbTables(conn_db);
            string sql = "update " + tables.file_kvar + " set comment = '" + text + "' where id = " + id;
            ExecSQL(conn_db, sql, true);
        }

        private int AddSuppInDb(IDbConnection conn_db, FileSupp fileSupp, FilesImported finder, out Returns ret)
        {
            IDbTransaction transaction = conn_db.BeginTransaction();

            DbSprav db = new DbSprav();
            Supplier supp = new Supplier();
            supp.name_supp = fileSupp.supp_name;
            supp.nzp_user = finder.nzp_user;
            ret = db.SaveSupplier(supp, transaction, conn_db);
            if (!ret.result)
            {
                if (transaction != null) transaction.Rollback();
                UpdateCommentIntoFileSupp(conn_db, fileSupp.id, "Ошибка  при добавлении поставщика");
                return 0;
            }
            fileSupp.nzp_supp = ret.tag;

            if (fileSupp.nzp_supp <= 0)
            {
                if (transaction != null) transaction.Rollback();
                UpdateCommentIntoFileSupp(conn_db, fileSupp.id, "Ошибка  при добавлении поставщика");
                return 0;
            }

            Payer payer = new Payer();
            payer.payer = payer.npayer = fileSupp.supp_name;
            payer.nzp_user = finder.nzp_user;
            payer.inn = fileSupp.inn;
            payer.kpp = fileSupp.kpp;
            payer.nzp_supp = fileSupp.nzp_supp;
            payer.nzp_type = Payer.ContragentTypes.ServiceSupplier.GetHashCode();
            payer.is_erc = 0;
            ret = db.SavePayer(payer, transaction, conn_db);
            if (!ret.result)
            {
                if (transaction != null)
                    transaction.Rollback();
                UpdateCommentIntoFileSupp(conn_db, fileSupp.id, "Ошибка  при добавлении поставщика");
                return 0;
            }

            if (transaction != null)
            {
                transaction.Commit();
            }
            return fileSupp.nzp_supp;
        }

        public Returns UploadIPUInDb(FilesImported finder, bool add)
        {
            Returns ret = Utils.InitReturns();

            string connectionString = Points.GetConnByPref(Points.Pref);
            IDbConnection conn_db = GetConnection(connectionString);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return ret;

            DbTables tables = new DbTables(conn_db);
            string where = "";
            if (add)
            {
#if PG
                where += " and coalesce(nzp_counter,0) = 0";
#else
                where += " and nvl(nzp_counter,0) = 0";
#endif
            }
            string sql = "select * from " + tables.file_ipu + " where nzp_file = " + finder.nzp_file + where;
            IDataReader reader = null;
            int counter_update = 0, counter_insert = 0, counter = 0;

            try
            {
                ret = ExecRead(conn_db, out reader, sql, true);
                if (!ret.result)
                {
                    conn_db.Close();
                    return ret;
                }

                /*   while (reader.Read())
                   {
                       counter++;
                       FileIPU fileSupp = new FileIPU();
                       if (reader["id"] != DBNull.Value) fileSupp.id = Convert.ToInt32(reader["id"]);
                       if (reader["ls_id"] != DBNull.Value) fileSupp.ls_id = Convert.ToString(reader["ls_id"]).Trim();
                       if (reader["inn"] != DBNull.Value) fileSupp.inn = Convert.ToString(reader["inn"]).Trim();
                       if (reader["kpp"] != DBNull.Value) fileSupp.kpp = Convert.ToString(reader["kpp"]).Trim();

                       if (add)
                       {
                           fileSupp.nzp_supp = AddSuppInDb(conn_db, fileSupp, finder, out ret);
                           counter_insert++;
                       }
                       else
                       {
                           sql = "select nzp_supp from " + tables.payer + " where trim(inn) = " + fileSupp.inn + " and trim(kpp) = " + fileSupp.kpp;
                           ret = ExecRead(conn_db, out reader2, sql, true);
                           if (!ret.result)
                           {
                               UpdateCommentIntoFileSupp(conn_db, fileSupp.id, "Ошибка  при определении кода поставщика по инн и кпп");
                               continue;
                           }
                           if (reader2.Read()) if (reader2["nzp_supp"] != DBNull.Value) fileSupp.nzp_supp = Convert.ToInt32(reader2["nzp_supp"]);
                           reader2.Close();
                           reader2.Dispose();
                       }

                       if (fileSupp.nzp_supp > 0)
                       {
                           sql = "update " + tables.file_supp + " set nzp_supp = " + fileSupp.nzp_supp + " where id = " + fileSupp.id;
                           ret = ExecSQL(conn_db, sql, true);
                           if (!ret.result)
                           {
                               UpdateCommentIntoFileSupp(conn_db, fileSupp.id, "Ошибка  при обновлении поля nzp_supp = " + fileSupp.nzp_supp);
                               continue;
                           }
                           counter_update++;
                       }
                   }*/
                reader.Close();
                reader.Dispose();
            }
            catch (Exception ex)
            {
                CloseReader(ref reader);
                conn_db.Close();
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;

                MonitorLog.WriteLog("Ошибка загрузки домов UploadSuppInDb " + err, MonitorLog.typelog.Error, 20, 201, true);
                return ret;
            }
            if (add) ret.text = "Добавлено " + counter_insert + " из " + counter;
            else ret.text = "Обновлено " + counter_update + " из " + counter;
            return ret;
        }

        private int AddIPUInDb(IDbConnection conn_db)
        {
            return 0;
        }




        //Перезапись в нужную таблицу
        private Returns WriteToFin()
        {
            Returns ret = Utils.InitReturns();

            #region подключение к БД
            //Подключаемся к БД
            string connectionString2 = Points.GetConnByPref(Points.Pref);
            IDbConnection conn_db = GetConnection(connectionString2);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return ret;
            #endregion

            Utility.ClassLog.InitializeLog("c:\\", "chargeLoad.log");

            #region перезапись пачки реестров
            string dat_oper_day = "";
            string sql = "Select dat_oper From " + Points.Pref + "_data"+tableDelimiter+"fn_curoperday ";
            var dtr = ClassDBUtils.OpenSQL(sql, conn_db);
            foreach (DataRow rrR in dtr.resultData.Rows)
            {
                dat_oper_day = Convert.ToString(rrR["dat_oper"]).Substring(0, 10);
            }


            sql = "select id, datpaytorder, numpaytorder, sumofpayt, numofpayt, filename from " + Points.Pref + "_data"+tableDelimiter+"_reestrpack";
            var dt = ClassDBUtils.OpenSQL(sql, conn_db);

            foreach (DataRow rr1 in dt.resultData.Rows)
            {
                //уникальный код пачки
                string idPack1 = Convert.ToString(rr1["id"]);
                int idPack;
                //номер платежного поручения
                int numPaytOrder = Convert.ToInt32(rr1["numpaytorder"]);
                //количество платежей
                int numOfPayt = Convert.ToInt32(rr1["numofpayt"]);
                //общая сумма платежей
                string sumOfPayt = Convert.ToString(rr1["sumofpayt"]);
                //дата пачки
                string datPaytOrder1 = Convert.ToString(rr1["datpaytorder"]);
                string datPaytOrder = datPaytOrder1.Substring(0, 10);
                //месяц, за который осуществляется оплата
                string month = "01" + datPaytOrder.Substring(2);
                //имя файла
                string filePackName = Convert.ToString(rr1["filename"]);

                string sql1 = "insert into " + Points.Pref + "_fin_" + dat_oper_day.Substring(8, 2) + tableDelimiter+"pack (nzp_pack, par_pack, pack_type, nzp_bank, nzp_supp, nzp_oper, num_pack, dat_uchet, dat_pack, " +
                "num_charge, yearr, count_kv, sum_pack, geton_pack, real_sum, real_geton, real_count, flag, dat_vvod, islock, operday_payer, " +
                " peni_pack, sum_rasp, sum_nrasp, erc_code, dat_inp, time_inp, file_name)" +
#if PG
 "values ( default ," +
#else
                "values ( 0 ,"+
#endif
                " null, 10, 1999, null, null, '" + numPaytOrder + "', '" + dat_oper_day + "', '" + datPaytOrder + "', " +
                " null, null, " + numOfPayt + ", " + sumOfPayt + ", null, null, null, 0, 11, '" + datPaytOrder + "', null, null, " +
                "  '0', '0', null, null, '" + datPaytOrder + "', null, '" + filePackName + "');";
                ret = ExecSQL(conn_db, sql1, true);
                idPack = GetSerialValue(conn_db);
                if (!ret.result) return ret;



            #endregion

                #region перезапись отдельных реестров

                sql = "select id, datpaytorder, numpaytorder, datpayt, numfilial, kodoperator, numoperation, sumofpayment, persaccklient, filename from  "
                      + Points.Pref + "_data"+tableDelimiter+"_reestr where numpaytorder=" + numPaytOrder.ToString();
                dt = ClassDBUtils.OpenSQL(sql, conn_db);

                foreach (DataRow rr2 in dt.resultData.Rows)
                {
                    //уникальный код квитанции
                    int kodKvitan = Convert.ToInt32(rr2["numoperation"]);
                    //код пачки  
                    //int numPaytOrder = Convert.ToInt32(rr1["numpaytorder"]);
                    //string sql3 = "select nzp_pack from " + Points.Pref + "_fin_13:pack where num_pack =" + Convert.ToInt32(rr2["numpaytorder"]) + ";";
                    //var dt3 = ClassDBUtils.OpenSQL(sql3, conn_db);
                    //int count1 = dt3.resultData.Rows.Count;
                    //numPaytOrder = Convert.ToInt32(dt3.resultData.Rows[count1 - 1]["nzp_pack"]);
                    //номер лицевого счета
                    int persAccKlient = Convert.ToInt32(rr2["persaccklient"]);
                    //платежный номер
                    sql1 = "select pkod from " + Points.Pref + "_data"+tableDelimiter+"kvar where nzp_kvar =" + persAccKlient + ";";
                    var dt1 = ClassDBUtils.OpenSQL(sql1, conn_db);
                    string pkod;
                    if (dt1.resultData.Rows.Count == 0)
                    {
                        //ret.text = "Нет личного счета клиента в таблице квартир ";
                        //ret.result = false;
                        //return ret;
                        pkod = "";
                    }
                    else
                    {
                        pkod = Convert.ToString(dt1.resultData.Rows[0]["pkod"]);
                    }
                    if (pkod == "") //pkod = "null";
                        pkod = "31";
                    //префикс БД - первые три цифры платежного номера
                    string prefix;
                    if (pkod == "null") prefix = "null";
                    else if (pkod.Length < 3) prefix = pkod;
                    else prefix = pkod.Substring(0, 3);
                    //сумма оплаты
                    sumOfPayt = Convert.ToString(rr2["sumofpayment"]);
                    //начислено к оплате                
                    //дата оплаты квитанции
                    datPaytOrder1 = Convert.ToString(rr2["datpayt"]);
                    datPaytOrder = datPaytOrder1.Substring(0, 10);
                    //месяц, за который осуществляется оплата
                    month = "01" + datPaytOrder.Substring(2);
                    //номер квитанции


                    string sql2 = "insert into " + Points.Pref + "_fin_" + dat_oper_day.Substring(8, 2) +tableDelimiter + "pack_ls (nzp_pack, prefix_ls, pkod, num_ls, g_sum_ls," +
                    "sum_ls, geton_ls, sum_peni, dat_month, kod_sum, nzp_supp, paysource, id_bill, dat_vvod," +
                    "dat_uchet, info_num, anketa,inbasket, alg, unl, date_distr, date_rdistr, nzp_user, incase, nzp_rs, erc_code, distr_month) values" +
                    "(" + idPack + ", " + prefix + ", " + pkod + ", " + persAccKlient + ", " + sumOfPayt + ", '0', '0', '0', '" + month + "'," +
                    "33, 0, null, 0, '" + datPaytOrder + "', null, '" + kodKvitan + "', null, 0, 0, 0, null, null, 0, 0, 0, null, null);";

                    ret = ExecSQL(conn_db, sql2, true);
                    if (!ret.result) return ret;

                }
            }

                #endregion

            conn_db.Close();

            return ret;
        }

        // Убираем лидирующие нули
        private static string DeleteFirstZeros(string str)
        {
            int i = 0;

            if (str[i] == '0')
                while ((i < str.Length) && (str[i] == '0'))
                    i++;

            String strResult;
            if (str[i] == '.')
                strResult = str.Remove(0, i - 1);
            else
                strResult = str.Remove(0, i);

            return strResult;
        }

        //для функции UploadMURCPayment
        public struct structPack
        {
            public int nzp_pack;
            public DateTime date_pack;
        };

        /// <summary>
        /// Загрузка файла "Оплаты МУРЦ"
        /// </summary>
        /// <param name="finder">файл</param>
        /// <returns>результат</returns>
        public Returns UploadMURCPayment(FilesImported finder)
        {
            Returns ret = Utils.InitReturns();

            //директория файла
            string fDirectory = Constants.Directories.ImportDir.Replace("/", "\\");
            // fDirectory = "D:\\work.php\\KOMPLAT.50\\WEB\\WebKomplat5\\ExcelReport\\import\\";

            //имя файла
            string fileName = Path.Combine(fDirectory, finder.saved_name);

            if (InputOutput.useFtp) InputOutput.DownloadFile(finder.saved_name, fileName);

            //версия файла
            int nzp_version = -1;
            FileInfo[] files = new FileInfo[1];
            #region Разархивация файла
            using (SevenZipExtractor extractor = new SevenZipExtractor(fileName))
            {
                //создание папки с тем же именем
                DirectoryInfo exDirectorey = Directory.CreateDirectory(Path.Combine(fDirectory, finder.saved_name.Substring(0, finder.saved_name.LastIndexOf('.'))));
                extractor.ExtractArchive(exDirectorey.FullName);
                files = exDirectorey.GetFiles("*.mdb");
                if (files.Length == 0)
                {
                    ret.result = false;
                    ret.text = "Архив пустой";
                    ret.tag = -1;
                    return ret;
                }
            }

            #endregion

            #region Переменные
            List<string> sqlStr = new List<string>();
            StringBuilder err = new StringBuilder();
            string commStr = "";
            #endregion

            #region Вставка файла
            using (IDbConnection con_db = GetConnection(Constants.cons_Kernel))
            {
                try
                {
                    ret = OpenDb(con_db, true);

                    if (!ret.result)
                    {
                        MonitorLog.WriteLog("Ошибка открытия соединения", MonitorLog.typelog.Error, true);
                        ret.result = false;
                        ret.text = " Файл не загружен, отсутствует подключение к базе данных ";
                        ret.tag = -1;
                        return ret;
                    }

                    DbWorkUser db = new DbWorkUser();
                    int localUSer = db.GetLocalUser(con_db, finder, out ret);
                    if (!ret.result)
                    {
                        MonitorLog.WriteLog("Ошибка определения локальног пользователя", MonitorLog.typelog.Error, true);
                        ret.result = false;
                        ret.text = " Ошибка определения локального пользователя ";
                        ret.tag = -1;
                        return ret;
                    }
#if PG
                    string sql = " INSERT INTO " + Points.Pref + "_data"+tableDelimiter+"  files_imported (nzp_file, nzp_version, loaded_name, saved_name, nzp_status, created_by, created_on, file_type) ";
                    sql += " VALUES (default," + nzp_version + ",'" + finder.loaded_name + "',\'" + finder.saved_name + "\',2," + localUSer + ",now(), 3)  ";
#else
                    string sql = " INSERT INTO " + Points.Pref + "_data"+tableDelimiter+"  files_imported (nzp_file, nzp_version, loaded_name, saved_name, nzp_status, created_by, created_on, file_type) ";
                    sql += " VALUES (0," + nzp_version + ",'" + finder.loaded_name + "',\'" + finder.saved_name + "\',2," + localUSer + ",current, 3)  ";
#endif

                    ret = ExecSQL(con_db, sql, true);
                    if (!ret.result)
                    {
                        MonitorLog.WriteLog("Ошибка добавления файла в в таблицу файла " + fileName, MonitorLog.typelog.Error, true);
                        ret.result = false;
                        ret.text = " Ошибка добавления файла в базу данных. ";
                        ret.tag = -1;
                        return ret;
                    }

                    //получение nzp_file
                    finder.nzp_file = GetSerialValue(con_db);

                }
                catch (Exception ex)
                {
                    MonitorLog.WriteLog("Ошибка выполнения процедуры LoadHarGilFondGKU : " + ex.Message, MonitorLog.typelog.Error, true);
                    ret.result = false;
                    return ret;
                }
            }
            #endregion

            if (files[0].Name != "Оплата МУРЦ.mdb")
            {
                ret.result = false;
                ret.text = "Выбран неверный файл";
                ret.tag = -1;
                return ret;
            }

            var mdbBase = "Оплата МУРЦ";
            #region Считываем файл
            fileName = files[0].FullName;

            if (System.IO.File.Exists(fileName) == false)
            {
                ret.result = false;
                ret.text = "Файл отсутствует по указанному пути";
                ret.tag = -1;
                return ret;
            }

            DataTable tbl = new DataTable();
            try
            {
                OleDbConnection oDbCon = new OleDbConnection();
                var myConnectionString = @"Provider=Microsoft.ACE.OLEDB.12.0;" +
                           "Data Source=" + fileName + ";Jet OLEDB:Database Password=password;";
                oDbCon.ConnectionString = myConnectionString;
                oDbCon.Open();

                OleDbCommand cmd = new OleDbCommand();
                cmd.CommandText = "select * from [" + mdbBase + "]";
                cmd.Connection = oDbCon;

                // Адаптер данных
                OleDbDataAdapter da = new OleDbDataAdapter();
                da.SelectCommand = cmd;
                // Заполняем объект данными
                da.Fill(tbl);
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка открытия файла " + fileName + " " + ex.Message,
                    MonitorLog.typelog.Warn, true);
                ret.result = false;
                ret.text = "Файл недоступен по указанному пути";
                ret.tag = -1;
                return ret;
            }

            #endregion



            #region Собрать запросы
            using (IDbConnection con_db = GetConnection(Constants.cons_Kernel))
            {
                try
                {
                    ret = OpenDb(con_db, true);

                    if (!ret.result)
                    {
                        MonitorLog.WriteLog("Ошибка открытия соединения", MonitorLog.typelog.Error, true);
                        ret.result = false;
                        ret.text = " Файл не загружен, отсутствует подключение к базе данных ";
                        ret.tag = -1;
                        return ret;
                    }

                    var num_pack = Convert.ToInt32(tbl.Rows[0][1]);
                    var pack_base = Points.Pref + "_fin_" + finder.year.Substring(2, 2) + tableDelimiter + "pack";
                    var pack_ls_base = Points.Pref + "_fin_" + finder.year.Substring(2, 2) +tableDelimiter + "pack_ls";
                    string sql;

                    List<structPack> numPack = new List<structPack>();
                    numPack.Add(new structPack() { nzp_pack = Convert.ToInt32(tbl.Rows[0][1]), date_pack = Convert.ToDateTime(tbl.Rows[0][3]) });

                    //для записи пачек
                    decimal sumPack = 0; //сумма
                    int countPack = 0; //количество записей

                    // определить операционный день 
                    string dat_oper_day = "";
                    string sqlr = "Select dat_oper From " + Points.Pref + "_data"+tableDelimiter+"fn_curoperday ";
                    var dtr = ClassDBUtils.OpenSQL(sqlr, con_db);
                    foreach (DataRow rrR in dtr.resultData.Rows)
                    {
                        dat_oper_day = Convert.ToString(rrR["dat_oper"]).Substring(0, 10);
                    }



                    num_pack = Convert.ToInt32(tbl.Rows[0][1].ToString());
                    int nzp_pack = 0;
                    int counter = 0;
                    decimal nzp_pack1 = 0;
                    foreach (DataRow row in tbl.Rows)
                    {
                        counter++;

                        #region добавление пачки

                        if (Convert.ToInt32(row[1]) != num_pack || counter == 1)
                        {
                            num_pack = Convert.ToInt32(row[1]);

                            string file_name = "Оплата МУРЦ.mdb";

                            //если это не первая пачка, то заносит подсчитанную сумму и количество пачек
                            if (counter != 1)
                            {
                                sql = "update " + pack_base + " set count_kv =" + countPack + ", sum_pack = " + sumPack + " where nzp_pack =" + nzp_pack;
                                ret = ExecSQL(con_db, sql, true);
                            }
                            //заводим новую пачку
                            sql = "insert into " + pack_base + " ( count_kv, sum_pack, time_inp, dat_uchet, dat_vvod, dat_inp, pack_type, nzp_bank, flag, peni_pack, sum_rasp, real_count, file_name, " +
                                 "par_pack, nzp_supp, nzp_oper, num_charge, yearr, geton_pack, real_sum, real_geton, islock, operday_payer, sum_nrasp, erc_code , num_pack, dat_pack" +
                                 
#if PG
                                        ") values ( 0, 0 , " +" now(), '" +
#else
                                     ") values ( 0 , 0 , " + " current year to second, '" + 
#endif
 row[3].ToString().Substring(0, 10) + "', '" + row[18].ToString().Substring(0, 10) + "', '" + row[3].ToString().Substring(0, 10) +
                                 "', 10, 1999, 11, '0', '0', 0,'" + file_name + "', null, null, null, null, null, null, null, null, null, null, null, null," + row[1].ToString() + ", '" + dat_oper_day + "')";

                            ret = ExecSQL(con_db, sql, true);
                            IDbTransaction transaction = null;
                            nzp_pack1 = ClassDBUtils.GetSerialKey(con_db, transaction);
                            nzp_pack = Convert.ToInt32(nzp_pack1);



                            num_pack = Convert.ToInt32(row[1]);
                            sumPack = 0;
                            countPack = 0;
                        }

                        #endregion

                        #region добавление отдельного реестра
                        //перевести внешний код улицы во внутренний
                        var address = GetLsByExtAddress(new FullAddress() { ulica = row[6].ToString(), ndom = row[7].ToString(), nkvar = row[8].ToString() }, con_db);
                        var num_ls = address.num_ls;
                        var pref = address.pref;
                        commStr = "select * from " + pack_ls_base + " where num_ls = " + num_ls;

                        //получаем pkod
                        decimal pkod;
                        sql = "select pkod from " + Points.Pref + "_data"+tableDelimiter+"kvar where num_ls = " + num_ls;
                        var dt = ClassDBUtils.OpenSQL(sql, con_db);
                        if (dt.resultData.Rows.Count > 0 && dt.resultData.Rows[0]["pkod"] != DBNull.Value) pkod = Convert.ToDecimal(dt.resultData.Rows[0]["pkod"]);
                        else pkod = 0;

                        //получаем код квитанции
                        int kodKvitan = num_pack;
                        //sql = "select num_pack from " + pack_base + " where nzp_pack =" + nzp_pack;
                        //dt = ClassDBUtils.OpenSQL(sql, con_db);
                        //if (dt.resultData.Rows.Count > 0 && dt.resultData.Rows[0]["num_pack"] != DBNull.Value) kodKvitan = Convert.ToInt32(dt.resultData.Rows[0]["num_pack"]);
                        //else kodKvitan = 0;

                        //получаем сумму
                        string sumOfPayt = row[11].ToString();
                        if (sumOfPayt[sumOfPayt.Length - 3] != '.') sumOfPayt = sumOfPayt.Substring(0, sumOfPayt.Length - 2);

                        //для пачек
                        sumPack += Convert.ToDecimal(sumOfPayt);
                        countPack++;


                        sql = "insert into " + Points.Pref + "_fin_13"+tableDelimiter+"pack_ls (nzp_pack, prefix_ls, pkod, num_ls, g_sum_ls," +
                        "sum_ls, geton_ls, sum_peni, dat_month, kod_sum, nzp_supp, paysource, id_bill, dat_vvod," +
                        "dat_uchet, info_num, anketa,inbasket, alg, unl, date_distr, date_rdistr, nzp_user, incase, nzp_rs, erc_code, distr_month) values" +
                        "(" + nzp_pack + ", 31 , " + pkod + ", " + num_ls + ", " + sumOfPayt + ", '0', '0', '0', '" + row[0].ToString().Substring(0, 10) + "'," +
                        "33, 0, null, 0, '" + row[3].ToString().Substring(0, 10) + "', null, '" + kodKvitan + "', null, 0, 0, 0, null, null, 0, 0, 0, null, null);";

                        ret = ExecSQL(con_db, sql, true);
                        if (!ret.result) return ret;

                        #endregion
                    }
                    // обновляем последнюю пачку
                    sql = "update " + pack_base + " set count_kv =" + countPack + ", sum_pack = " + sumPack + " where nzp_pack =" + nzp_pack;
                    ret = ExecSQL(con_db, sql, true);

                }
                catch (Exception ex)
                {
                    MonitorLog.WriteLog("Ошибка выполнения процедуры UploadMURCPayment : " + ex.Message, MonitorLog.typelog.Error, true);
                    ret.result = false;
                    return ret;
                }
                finally
                {
                    con_db.Close();
                }
            }
            #endregion



            #region Лог ошибок
            if (err.Length != 0)
            {
                StreamWriter sw = File.CreateText(fileName + ".log");
                sw.Write(err.ToString());
                sw.Flush();
                sw.Close();

                #region Обновление статуса
                using (IDbConnection con_db = GetConnection(Constants.cons_Kernel))
                {
                    try
                    {
                        ret = OpenDb(con_db, true);

                        if (!ret.result)
                        {
                            MonitorLog.WriteLog("Ошибка открытия соединения", MonitorLog.typelog.Error, true);
                            ret.result = false;
                            ret.text = " Файл не загружен, отсутствует подключение к базе данных ";
                            ret.tag = -1;
                            return ret;
                        }

                        string sql = " UPDATE " + Points.Pref + "_data"+tableDelimiter+"files_imported set nzp_status =  " + (int)STCLINE.KP50.Interfaces.FilesImported.Statuses.LoadedWithErrors;
                        sql += " where nzp_file = " + finder.nzp_file;

                        ret = ExecSQL(con_db, sql, true);
                        if (!ret.result)
                        {
                            MonitorLog.WriteLog("Ошибка обновления статуса файла " + fileName, MonitorLog.typelog.Error, true);
                            ret.result = false;
                            ret.text = " Ошибка обновления статуса файла. ";
                            ret.tag = -1;
                            return ret;
                        }
                    }
                    catch (Exception ex)
                    {
                        MonitorLog.WriteLog("Ошибка выполнения процедуры LoadHarGilFondGKU : " + ex.Message, MonitorLog.typelog.Error, true);
                        ret.result = false;
                        return ret;
                    }
                }
                #endregion

                ret.tag = -1;
                ret.result = false;
                ret.text = "В загруженном файле обнаружились ошибки. Подробности в логе ошибок. ";

                return ret;
            }
            #endregion

            ret.result = true;
            ret.text = "Файл успешно загружен.";
            ret.tag = -1;

            return ret;
        }

        /// <summary>
        /// Получение num_ls по внешнему адресу
        /// </summary>
        /// <param name="obj">
        /// nzp_ul - внешний код улицы 
        /// ndom - номер дома
        /// nkvar - номер квартиры
        /// </param>
        /// <returns>
        ///nzp_user =  
        /// </returns>
        private static Ls GetLsByExtAddress(FullAddress obj, IDbConnection con_db)
        {
            decimal ls;
            var res = new Ls();
            bool done = false;
            try
            {
#if PG
                string sql = "select a.nzp_kvar as id, a.pref as pref from " + Points.Pref + "_data.kvar a, " + Points.Pref + "_data.s_ulica ul, " + Points.Pref + "_data.dom d, " + Points.Pref + "_data.file_ulica as ful where " +
                                    " a.nkvar = '" + obj.nkvar + "' and a.nzp_dom = d.nzp_dom and d.ndom ='" + obj.ndom + "' and ful.nzp_ul = ul.nzp_ul and ful.file_ulica_id = '" + obj.ulica + "' and ul.nzp_ul = d.nzp_ul";
#else
string sql = "select a.nzp_kvar as id, a.pref as pref from " + Points.Pref + "_data:kvar a, " + Points.Pref + "_data:s_ulica ul, " + Points.Pref + "_data:dom d, " + Points.Pref + "_data:file_ulica as ful where " +
                    " a.nkvar = '" + obj.nkvar + "' and a.nzp_dom = d.nzp_dom and d.ndom ='" + obj.ndom + "' and ful.nzp_ul = ul.nzp_ul and ful.file_ulica_id = " + obj.ulica + " and ul.nzp_ul = d.nzp_ul";
#endif
                //sql = "select a.id as id from " + Points.Pref + "_data"+tableDelimiter+"file_kvar a, " + Points.Pref + "_data"+tableDelimiter+"file_dom b  where a.nkvar = " + obj.nkvar + " and a.dom_id = b.id and b.local_id ='" + obj.ndom +
                //    "' and b.ulica ='" + ulica + "'";
                var dt = ClassDBUtils.OpenSQL(sql, con_db);
                if (dt.resultData.Rows.Count > 0)
                {
                    ls = Convert.ToDecimal(dt.resultData.Rows[0]["id"]);
                    res.num_ls = Convert.ToInt32(ls);
                    res.pref = ls.ToString();
                    done = true;
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка функции GetLsByExtAddress" + ex.Message, MonitorLog.typelog.Error, true);
            }

            if (!done)
            {
                res.num_ls = 0;
                res.pref = "";
            }

            return res;
        }

        /// <summary>
        /// Получение внешнего адреса по num_ls (вроде не проверена)
        /// </summary>
        /// <param name="obj">
        /// nzp_ul - внешний код улицы 
        /// ndom - номер дома
        /// nkvar - номер квартиры
        /// </param>
        /// <returns></returns>
        private static FullAddress GetExtAddressByLs(string num_ls, IDbConnection con_db)
        {
#if PG
            string sql = "select a.nkvar as nkvar, b.ndom as ndom, b.ulica as ulica from " + Points.Pref + "_data.file_kvar a, " + Points.Pref + "_data.file_dom b where" +
                            " a.id = " + num_ls + " and a.dom_id = b.id";
#else
string sql = "select a.nkvar as nkvar, b.ndom as ndom, b.ulica as ulica from " + Points.Pref + "_data:file_kvar a, " + Points.Pref + "_data:file_dom b where" +
                " a.id = " + num_ls + " and a.dom_id = b.id";
#endif
            var dt = ClassDBUtils.OpenSQL(sql, con_db);

            if (dt.resultData.Rows.Count > 0)
            {
                return new FullAddress()
                {
                    ulica = Convert.ToString(dt.resultData.Rows[0]["nkvar"]),
                    ndom = Convert.ToString(dt.resultData.Rows[0]["ndom"]),
                    nkvar = Convert.ToString(dt.resultData.Rows[0]["ulica"])
                };
            }
            else
            {
                return new FullAddress()
                {
                    ulica = "0",
                    ndom = "0",
                    nkvar = "0"
                };
            }
        }

        private Returns CheckUnique(IDbConnection conn_db, FilesImported finder, StringBuilder err)
        {
            Returns ret = Utils.InitReturns();
            try
            {

                #region 2 file_area
                ret = CheckOneUnique(conn_db, finder, err, "file_area", "id", " управляющие компании ");
                #endregion

                #region 3 file_dom
                ret = CheckOneUnique(conn_db, finder, err, "file_dom", "id", "дома");
                #endregion

                #region 4 file_kvar
                ret = CheckOneUnique(conn_db, finder, err, "file_kvar", "id", "квартиры");
                #endregion

                #region 5 file_supp
                ret = CheckOneUnique(conn_db, finder, err, "file_supp", "supp_id", "поставщики");
                #endregion

                #region 9 file_odpu
                ret = CheckOneUnique(conn_db, finder, err, "file_odpu", "local_id", "ОДПУ");
                #endregion

                #region 11 file_ipu
                ret = CheckOneUnique(conn_db, finder, err, "file_ipu", "local_id", "ИПУ");
                #endregion

                #region 14 file_mo
                ret = CheckOneUnique(conn_db, finder, err, "file_mo", "id_mo", "МО");
                #endregion

                #region 16 file_typeparams
                ret = CheckOneUnique(conn_db, finder, err, "file_typeparams", "id_prm", "выгруженные параметры");
                #endregion

                #region 17 file_gaz
                ret = CheckOneUnique(conn_db, finder, err, "file_gaz", "id_prm", "выгруженные типы домов по газоснабжению");
                #endregion

                #region 18 file_voda
                ret = CheckOneUnique(conn_db, finder, err, "file_voda", "id_prm", "выгруженные типы домов по водоснабжению");
                #endregion

                #region 19 file_blag
                ret = CheckOneUnique(conn_db, finder, err, "file_blag", "id_prm", "выгруженные категории благоустройства");
                #endregion

                #region 24 file_typenedopost
                ret = CheckOneUnique(conn_db, finder, err, "file_typenedopost", "type_ned", "типы недопоставок");
                #endregion

                #region 26 file_pack
                ret = CheckOneUnique(conn_db, finder, err, "file_pack", "id", "пачки реестров");
                #endregion

                #region 27 file_urlic
                ret = CheckOneUnique(conn_db, finder, err, "file_urlic", "supp_id", "юридические лица");
                #endregion



            }
            catch
            {
                err.Append("Ошибка при проверке уникальности строк в функцие CheckUnique " + Environment.NewLine);
            }
            return ret;
        }

        private Returns CheckOneUnique(IDbConnection conn_db, FilesImported finder, StringBuilder err, string table_name, string id_name, string errField)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                string sql;
#if PG
                sql = "set search_path to '" + Points.Pref + "_data'";
#else
                sql = "database " + Points.Pref + "_data";
#endif
                ret = ExecSQL(conn_db, sql, true);
                sql = "drop table " + Points.Pref + "_data"+tableDelimiter+"t_unique";
                ret = ExecSQL(conn_db, sql, false);
                sql = "select " + id_name + " as id_name, count(*) as kol " +
#if PG
                        " into unlogged t_unique "+
#else                    
#endif
                    "from " + Points.Pref + "_data" + tableDelimiter + "" + table_name +
                    " where nzp_file = " + finder.nzp_file +
                    " group by 1" +
                    " having count(*)>1 " +
#if PG
#else
                    " into temp t_unique "+
#endif
                        "";
                ret = ExecSQL(conn_db, sql, true);
                sql = "select * from " + Points.Pref + "_data"+tableDelimiter+"t_unique";
                var dt = ClassDBUtils.OpenSQL(sql, conn_db);
                if (Convert.ToInt32(dt.resultData.Rows.Count) > 0)
                {
                    err.Append("Обнаружена ошибка входных данных. Имеются " + errField + " с одинаковым уникальным номером в количестве " + Convert.ToInt32(dt.resultData.Rows.Count) + "." + Environment.NewLine);
                    err.Append(String.Format("{0,30}|{1,30}|{2}", "Уникальный код", "Количество строк", Environment.NewLine));

                    foreach (DataRow rr in dt.GetData().Rows)
                    {
                        string testMePls = String.Format("{0,30}|{1,30}|{2}", rr["id_name"].ToString().Trim(), rr["kol"].ToString().Trim(), Environment.NewLine);
                        err.Append(testMePls);
                    }

                    if (table_name.Trim() == "file_kvar")
                    {
                        sql = "update " + Points.Pref + "_data"+tableDelimiter+"file_kvar set nzp_status = 1 where id in (select id_name from " + Points.Pref + "_data"+tableDelimiter+"t_unique) and nzp_file =" + finder.nzp_file;
                        ret = ExecSQL(conn_db, sql, true);
                    }
                }
            }
            catch
            {
                err.Append("Ошибка при проверке уникальности строк в функцие CheckOneUnique " + Environment.NewLine);
            }
            return ret;
        }

        private Returns SectionsToDB(IDbConnection conn_db, FilesImported finder)
        {
            Returns ret = Utils.InitReturns();

            try
            {
                for (int i = 1; i < finder.sections.Length; i++)
                {
                    string sql = "insert into " + Points.Pref + "_data"+tableDelimiter+"file_section"+ 
                        " ( num_sec, sec_name, nzp_file, is_need_load)"+
                        " values( " + i + ", null, " + finder.nzp_file + ", " + Convert.ToInt32(finder.sections[i]) + " )";
                    ret = ExecSQL(conn_db, sql, true);
                }
            }
            catch
            {
                MonitorLog.WriteLog("Ошибка функции SectionsToDB", MonitorLog.typelog.Error, true);
            }
            return ret;
        }
        
        public List<AreaCodes> GetAreaCodes(AreaCodes finder, out Returns ret)
        {
            if (finder.nzp_user < 1)
            {
                ret = new Returns(false, "Не задан пользователь");
                return null;
            }

            string conn_kernel = Points.GetConnByPref(Points.Pref);
            IDbConnection conn_db = GetConnection(conn_kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return null;

            DbTables tables = new DbTables(conn_db);

            string where = "";
            if (finder.code > 0) where += " and ac.code = " + finder.code;
            if (finder.area != "") where += " and a.area like '%"+finder.area+"%'";
            if (finder.nzp_area > 0) where += " and ac.nzp_area = " + finder.nzp_area;

            //Определить общее количество записей
            string sql = "select count(*) from " + tables.area_codes + " ac, " +
                        tables.area + " a where ac.nzp_area = a.nzp_area " + where;
            object count = ExecScalar(conn_db, sql, out ret, true);
            int recordsTotalCount;
            try { recordsTotalCount = Convert.ToInt32(count); }
            catch (Exception e)
            {
                ret = new Returns(false, "Ошибка при определении количества записей: " + (Constants.Debug ? e.Message : ""));
                MonitorLog.WriteLog("Ошибка GetAreaCodes " + (Constants.Viewerror ? "\n" + e.Message : ""), MonitorLog.typelog.Error, 20, 201, true);
                conn_db.Close();
                return null;
            }

            List<AreaCodes> spis = new List<AreaCodes>();
            sql = " select ac.code, ac.nzp_area, a.area, ac.is_active from " + tables.area_codes + " ac, " +
                tables.area + " a where ac.nzp_area = a.nzp_area "+where+" order by code";
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
                    AreaCodes zap = new AreaCodes();
                    zap.num = i;
                    if (reader["code"] != DBNull.Value) zap.code = Convert.ToInt32(reader["code"]);
                    if (reader["nzp_area"] != DBNull.Value) zap.nzp_area = Convert.ToInt32(reader["nzp_area"]);
                    if (reader["area"] != DBNull.Value) zap.area = Convert.ToString(reader["area"]).Trim();
                    if (reader["is_active"] != DBNull.Value) zap.is_active = Convert.ToInt32(reader["is_active"]);
                    if (zap.is_active > 0) zap.active = "Да";
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
                MonitorLog.WriteLog("Ошибка заполнения списка уникальных кодов управляющих компаний " + (Constants.Viewerror ? " \n " + ex.Message : ""), MonitorLog.typelog.Error, 20, 201, true);
                return null;
            }
                    }

        public Returns GetMaxCodeFromAreaCodes()
        {
            string conn_kernel = Points.GetConnByPref(Points.Pref);
            IDbConnection conn_db = GetConnection(conn_kernel);
            Returns ret = OpenDb(conn_db, true);
            if (!ret.result) return ret;

            ret = GetMaxCodeFromAreaCodes(conn_db);

            conn_db.Close();
            return ret;
        }

        public Returns GetMaxCodeFromAreaCodes(IDbConnection conn_db)
        {
            Returns ret;
            DbTables tables = new DbTables(conn_db);
            string sql = "select max(code) as code from "+ tables.area_codes;
            object count = ExecScalar(conn_db, sql, out ret, true);
            int code;
            try { code = Convert.ToInt32(count)+1; }
            catch (Exception e)
            {
                ret = new Returns(false, "Ошибка при определении максимального кода: " + (Constants.Debug ? e.Message : ""));
                MonitorLog.WriteLog("Ошибка GetMaxCodeFromAreaCodes " + (Constants.Viewerror ? "\n" + e.Message : ""), MonitorLog.typelog.Error, 20, 201, true);              
                return ret;
            }
            ret.tag = code;
            return ret;
        }

        public Returns SaveAreaCodes(AreaCodes finder)
        {
            if (finder.nzp_user < 1) return new Returns(false, "Не задан пользователь");
           
            if (finder.code <= 0)
            {
                if (finder.nzp_area <= 0) return new Returns(false, "Не задана Управляющая организация", -1);
                if (finder.code_po > 0)
                {
                    if (finder.code_s <= 0) return new Returns(false, "Не верно задан диапазон",-1);
                }
            }
            
            #region подключение к базе
            string conn_kernel = Points.GetConnByPref(Points.Pref);
            IDbConnection conn_db = GetConnection(conn_kernel);
            Returns ret = OpenDb(conn_db, true);
            if (!ret.result) return ret;
            #endregion

            DbTables tables = new DbTables(conn_db);

            #region Определить пользователя
            DbWorkUser db = new DbWorkUser();
            int nzpUser = db.GetLocalUser(conn_db, null, finder, out ret); //локальный пользователь      
            db.Close();
            if (!ret.result) return ret;
            #endregion
       
            string sql;
            if (finder.code > 0)
            {
                sql = " select nzp_area from " + tables.area_codes + " where code = " + finder.code;
                object count = ExecScalar(conn_db, sql, out ret, true);
                int nzp_area;
                try { nzp_area = Convert.ToInt32(count); }
                catch (Exception e)
                {
                    ret = new Returns(false, "Ошибка при определении nzp_area: " + (Constants.Debug ? e.Message : ""));
                    MonitorLog.WriteLog("Ошибка SaveAreaCodes " + (Constants.Viewerror ? "\n" + e.Message : ""), MonitorLog.typelog.Error, 20, 201, true);
                    return ret;
                }
                sql = "update " + tables.area_codes + " set is_active = 0 where nzp_area = "+nzp_area;
                ret = ExecSQL(conn_db, sql, true);     

                sql = "update " + tables.area_codes + " set is_active = " + finder.is_active+", changed_by = "+nzpUser+
#if PG
                ", changed_on = now() " +
#else
                ", changed_on = current "+
#endif
                    " where code = "+finder.code;
                ret = ExecSQL(conn_db, sql, true);               
                conn_db.Close();
                return ret;
            }
            else
            {

                if (finder.code_s > 0 && finder.code_po <= 0) finder.code_po = finder.code_s;
                if (finder.code_s == 0 && finder.code_po == 0)
                {
                    Returns ret2 = GetMaxCodeFromAreaCodes(conn_db);
                    if (!ret2.result) 
                    {
                        conn_db.Close();
                        return ret;
                    }
                    finder.code_s = finder.code_po = ret2.tag;
                }

                if(finder.code_s > 0 && finder.code_po > 0)
                {
                    string errcodes = "";
                    string addedcodes = "";
                    for (int i = finder.code_s; i <=finder.code_po; i++)
                    {
                        sql = "select count(*) from " + tables.area_codes + " where code = " + i;
                        object count = ExecScalar(conn_db, sql, out ret, true);
                        int cnt;
                        try { cnt = Convert.ToInt32(count); }
                        catch (Exception e)
                        {
                            ret = new Returns(false, "Ошибка: " + (Constants.Debug ? e.Message : ""));
                            MonitorLog.WriteLog("Ошибка SaveAreaCodes " + (Constants.Viewerror ? "\n" + e.Message : ""), MonitorLog.typelog.Error, 20, 201, true);
                            return ret;
                        }
                        if (cnt == 0)
                        {
                            int isactive = 0;
                            if (finder.active_num > 0)
                            {
                                if (finder.active_num == i) isactive = 1;
                            }
                            sql = "insert into " + tables.area_codes + " (code, nzp_area, changed_by, changed_on, is_active) values (" + 
                                i + ", " + finder.nzp_area + ", "+nzpUser+
#if PG
                                ", now(), " +
#else
                                ", current, "+
#endif
                                isactive+")";
                            ret = ExecSQL(conn_db, sql, true);
                            if (ret.result)
                            {
                                if (addedcodes == "") addedcodes += i.ToString();
                                else addedcodes += ", " + i;
                                string seq = Points.Pref+"_data.kvar_pkod10_"+i+"_seq";
#if PG
                                
                                sql = "DROP SEQUENCE "+seq;
                                ExecSQL(conn_db, sql, false);
                                sql = "CREATE SEQUENCE "+seq +
                                      " INCREMENT 1 MINVALUE 1 MAXVALUE 9223372036854775807 "+
                                      " START 1 CACHE 1";
                                ret = ExecSQL(conn_db, sql, true);
                                if (!ret.result)
                                {
                                    conn_db.Close();
                                    return ret;
                                }
                                sql = " ALTER TABLE " + seq + " OWNER TO postgres";
                                ret = ExecSQL(conn_db, sql, true);
                                if (!ret.result)
                                {
                                    conn_db.Close();
                                    return ret;
                                }
#else
                                sql = "database " + Points.Pref + "_data";
                                ret = ExecSQL(conn_db, sql, false);
                                if (ret.result)
                                {
                                    sql = "CREATE SEQUENCE " + seq;
                                    ret = ExecSQL(conn_db, sql, true);
                                    if (!ret.result) return ret;
                                }
                                else return ret;
#endif

                            }
                        }
                        else                        
                            if (errcodes == "") errcodes += i.ToString();
                            else errcodes += ", " + i;                        
                    }

                    if (ret.result)
                    {
                        if (errcodes == "") ret.text = "Создание прошло успешно";
                        else 
                        {
                            if (addedcodes != "") ret.text = "Коды (" + addedcodes + ") были успешно добавлены.";
                            if (errcodes != "") ret.text = " Коды (" + errcodes + ") не были добавлены, так как уже существуют в БД";
                        }
                    }
                    else
                    {
                        if (addedcodes != "") ret.text = "Коды (" + addedcodes + ") были успешно добавлены.";
                        if (errcodes != "") ret.text = " Коды (" + errcodes + ") не были добавлены, так как уже существуют в БД";
                    }
                    ret.tag = -1;                    
                }
                conn_db.Close();
                return ret;
            }           
        }

        public Returns DeleteAreaCodes(AreaCodes finder)
        {
            if (finder.nzp_user < 1) return new Returns(false, "Не задан пользователь");
            if (finder.code <= 0) return new Returns(false, "Не выбрана запись", -1);

            #region подключение к базе
            string conn_kernel = Points.GetConnByPref(Points.Pref);
            IDbConnection conn_db = GetConnection(conn_kernel);
            Returns ret = OpenDb(conn_db, true);
            if (!ret.result) return ret;
            #endregion

            DbTables tables = new DbTables(conn_db);
                     
            string sql;
           
            sql = " select count(*) from " + tables.kvar + " where area_code = " + finder.code;
            object count = ExecScalar(conn_db, sql, out ret, true);
            int cnt;
            try { cnt = Convert.ToInt32(count); }
            catch (Exception e)
            {
                ret = new Returns(false, "Ошибка при определении л/с связанных с уникальным кодом управляющей компании: " + (Constants.Debug ? e.Message : ""));
                MonitorLog.WriteLog("Ошибка DeleteAreaCodes " + (Constants.Viewerror ? "\n" + e.Message : ""), MonitorLog.typelog.Error, 20, 201, true);
                return ret;
            }
            
            if (cnt > 0)
            {
                return new Returns(false, "Запись удалить нельзя, так как она связанна с л/с", -1);
            }
            sql = "delete from " + tables.area_codes + " where code = " + finder.code;
            ret = ExecSQL(conn_db, sql, true);

            string seq = Points.Pref + "_data" + tableDelimiter + "kvar_pkod10_" + finder.code + "_seq";
            sql = "DROP SEQUENCE " + seq;
            ExecSQL(conn_db, sql, false);
            conn_db.Close();
            return ret;
        }

        public Returns CreateSequence()
        {
            Returns ret = Utils.InitReturns();            

            #region подключение к базе
            string conn_kernel = Points.GetConnByPref(Points.Pref);
            IDbConnection conn_db = GetConnection(conn_kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return ret;
            #endregion

            string sql = "";
#if PG          
#else
            sql = "database " + Points.Pref + "_data";
            ret = ExecSQL(conn_db, sql, false);
#endif

            DbTables tables = new DbTables(conn_db);

            //num_ls
            ret = CreateNewSeq(conn_db, Points.Pref + "_data" + tableDelimiter + "kvar_num_ls_seq", "num_ls", tables.kvar);
            if (!ret.result)
            {
                conn_db.Close();
                return ret;
            }

            //nzp_counter            
            string seq = Points.Pref + "_data" + tableDelimiter + "counters_spis_nzp_counter_seq";           
#if PG
            sql = "SELECT nextval('" +seq+ "')";
#else        
            sql = "SELECT " + seq + ".nextval FROM DUAL";
#endif
            ret = ExecSQL(conn_db, sql, false);
            if (!ret.result)
            {
#if PG
                sql = "CREATE SEQUENCE "+seq +" INCREMENT 1 MINVALUE 1 MAXVALUE 9223372036854775807 START 1 CACHE 1";
                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result) return ret;

                sql = " ALTER TABLE "+seq+" OWNER TO postgres";
                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result) return ret;
#else
                sql = "CREATE SEQUENCE " + seq;
                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result) return ret;
#endif
            }

            sql = "";
            foreach (_Point zap in Points.PointList)
            {
                if (sql == "") sql = "select max(nzp_counter) as nzp from " + zap.pref + "_data" + tableDelimiter + "counters_spis ";
                else sql += " union select max(nzp_counter) as nzp from " + zap.pref + "_data" + tableDelimiter + "counters_spis";
            }
            IDataReader reader;
            if (!ExecRead(conn_db, out reader, sql, true).result)
            {
                conn_db.Close();
                return ret;
            }
            List<int> nzp = new List<int>();
            try
            {              
                while (reader.Read())  if (reader["nzp"] != DBNull.Value) nzp.Add(Convert.ToInt32(reader["nzp"]));               
            }
            catch (Exception ex)
            {
                reader.Close();
                conn_db.Close();
                ret = new Returns(false, ex.Message);
                MonitorLog.WriteLog("Ошибка заполнения списка максимальных кодов счетчиков по всем БД " + (Constants.Viewerror ? " \n " + ex.Message : ""), MonitorLog.typelog.Error, 20, 201, true);
                return ret;
            }
            int nzp_counter = 0;
            if (nzp.Count > 0) nzp_counter = nzp.Max();
            if (nzp_counter == 0) nzp_counter++;
#if PG
            sql = "SELECT setval('" + seq + "', " + nzp_counter + ")";
#else
            sql = "alter sequence " + seq + " RESTART " + nzp_counter;
#endif
            ret = ExecSQL(conn_db, sql, true);
            if (!ret.result) return ret;

            //nzp_kvar          
            ret = CreateNewSeq(conn_db, Points.Pref + "_data" + tableDelimiter + "kvar_nzp_kvar_seq", "nzp_kvar", tables.kvar);
            if (!ret.result)
            {
                conn_db.Close();
                return ret;
            }

            //nzp_dom            
            ret = CreateNewSeq(conn_db, Points.Pref + "_data" + tableDelimiter + "dom_nzp_dom_seq", "nzp_dom", tables.dom);
            if (!ret.result)
            {
                conn_db.Close();
                return ret;
            }
                           
            //nzp_ul           
            ret = CreateNewSeq(conn_db, Points.Pref + "_data" + tableDelimiter + "s_ulica_nzp_ul_seq", "nzp_ul", tables.ulica);
            if (!ret.result)
            {
                conn_db.Close();
                return ret;
            }

            //nzp_geu           
            ret = CreateNewSeq(conn_db, Points.Pref + "_data" + tableDelimiter + "s_geu_nzp_geu_seq", "nzp_geu", tables.geu);
            if (!ret.result)
            {
                conn_db.Close();
                return ret;
            }

            //nzp_area           
            ret = CreateNewSeq(conn_db, Points.Pref + "_data" + tableDelimiter + "s_area_nzp_area_seq", "nzp_area", tables.area);
            if (!ret.result)
            {
                conn_db.Close();
                return ret;
            }

#if PG
#else
            sql = "database " + Points.Pref + "_kernel";
            ret = ExecSQL(conn_db, sql, false);
#endif

            //nzp_payer_         
            ret = CreateNewSeq(conn_db, Points.Pref + "_kernel" + tableDelimiter + "s_payer_nzp_payer_seq", "nzp_payer", tables.payer);
            if (!ret.result)
            {
                conn_db.Close();
                return ret;
            }

            //nzp_supp        
            ret = CreateNewSeq(conn_db, Points.Pref + "_kernel" + tableDelimiter + "supplier_nzp_supp_seq", "nzp_supp", tables.supplier);
            if (!ret.result)
            {
                conn_db.Close();
                return ret;
            }


            //pkod10
#if PG
#else
            sql = "database " + Points.Pref + "_data";
            ret = ExecSQL(conn_db, sql, false);
#endif

            sql = "select * from " + tables.area_codes + " where is_active = 1";          
            ret = ExecRead(conn_db, out reader, sql, true);
            if (!ret.result)
            {
                conn_db.Close();
                return ret;
            }
            try
            {
                while (reader.Read())
                {                    
                    AreaCodes zap = new AreaCodes();                   
                    if (reader["code"] != DBNull.Value) zap.code = Convert.ToInt32(reader["code"]);
                    seq = Points.Pref + "_data" + tableDelimiter + "kvar_pkod10_" + zap.code + "_seq";
                    ret = CreateNewSeq(conn_db, seq, "pkod10", tables.kvar + " where area_code = " + zap.code);
                    if (!ret.result)
                    {
                        conn_db.Close();
                        return ret;
                    }
                }             

                reader.Close();
              
            }
            catch (Exception ex)
            {
                reader.Close();
                conn_db.Close();
                ret = new Returns(false, ex.Message);
                MonitorLog.WriteLog("Ошибка установки кодов для pkod10 " + (Constants.Viewerror ? " \n " + ex.Message : ""), MonitorLog.typelog.Error, 20, 201, true);
                return ret;
            }
            conn_db.Close();
            return ret;
        }

        private Returns CreateNewSeq(IDbConnection conn_db, string seq, string field, string table)
        {
            Returns ret;
            string sql = string.Empty;
#if PG
            sql = " SELECT setval('"+seq +"', max("+field+")) FROM " + table;
#else      
            int cnt = 0;
            sql = "SELECT max(" + field + ") FROM " + table;           
            object count = ExecScalar(conn_db, sql, out ret, true);           
            try { if (count != DBNull.Value) cnt = Convert.ToInt32(count); }
            catch (Exception e)
            {
                ret = new Returns(false, "Ошибка при определении кода: "+table + (Constants.Debug ? e.Message : ""));
                MonitorLog.WriteLog("Ошибка CreateNewSeq " + (Constants.Viewerror ? "\n" + e.Message : ""), MonitorLog.typelog.Error, 20, 201, true);
                return ret;
            }
            if (cnt == 0) cnt++;
            sql = " alter sequence " + seq + " RESTART "+cnt;
#endif
            ret = ExecSQL(conn_db, sql, false);
            if (!ret.result)
            {
#if PG
                sql = "CREATE SEQUENCE " + seq + " INCREMENT 1 MINVALUE 1 MAXVALUE 9223372036854775807 START 1 CACHE 1";
                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result) return ret;
                sql = " ALTER TABLE " + seq + " OWNER TO postgres";
                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result) return ret;
                sql = " SELECT setval('" + seq + "', max(" + field + ")) FROM " + table;
                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result) return ret;
#else
                sql = "CREATE SEQUENCE " + seq;
                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result) return ret;

                sql = " alter sequence " + seq + " RESTART " + cnt;
                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result) return ret;
#endif
            }
            return ret;
        }

       
        /// <summary>
        /// Функция подготовки данных для печати ЛС
        /// </summary>
        /// <param name="finder"></param>
        /// <returns></returns>
        public Returns PreparePrintInvoices(List<PointForPrepare> finder)
        {
            Returns ret = Utils.InitReturns();

            

            foreach (PointForPrepare pointForPrepare in finder)
            {
                //поставить фоновую задачу для каждого лок банка                
                CalcFon calcfon = new CalcFon(Points.GetCalcNum(0));
                calcfon.task = FonTaskTypeIds.taskPreparePrintInvoices;
                calcfon.status = FonTaskStatusId.New; //на выполнение    
                calcfon.nzp = pointForPrepare.mark ? 1 : 0;
                calcfon.nzpt = pointForPrepare.nzp_wp;
                calcfon.yy = pointForPrepare.PrepareDate.Year;
                calcfon.mm = pointForPrepare.PrepareDate.Month;
                calcfon.txt = "Операция закрытия месяца : " + Utils.GetMonthName(pointForPrepare.PrepareDate.Month) + pointForPrepare.PrepareDate.Year + "г.";
                calcfon.nzp_user = pointForPrepare.nzp_user;
                DbCalc dbCalc = new DbCalc();
                ret = dbCalc.AddTask(calcfon);
                if (!ret.result)
                {
                    return ret;
                }
            }

            return ret;
        }

        public Returns UpdatePwds()
        {
            Returns ret = Utils.InitReturns();
            IDbConnection conn_web = null;
            conn_web = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result)
            {
                MonitorLog.WriteLog("Ошибка при открытии соединения с БД в UpdatePwds", MonitorLog.typelog.Error, true);
                conn_web.Close();
                return ret;
            }
            ExecSQL(conn_web, "SET search_path to public;", false);

            if (!DBManager.isTableHasColumn(conn_web, "users", "is_new_pwd"))
            {
                ret = ExecSQL(conn_web, "alter table users add is_new_pwd integer default 0", true);
                if (!ret.result)
                {
                    ret.text = "Ошибка изменения структуры таблицы users при добавлении поля is_new_pwd ";
                    return ret;
                }
            }
            string sql = " select nzp_user, pwd FROM users WHERE is_new_pwd = 0; ";
            IDataReader reader;
            if (!ExecRead(conn_web, out reader, sql, true).result)
            {
                conn_web.Close();
                return ret;
            }
            try
            {
                List<User> users = new List<User>();
                while (reader.Read())
                {
                    User user = new User();
                    if (reader["nzp_user"] != DBNull.Value) user.nzp_user = Convert.ToInt32(reader["nzp_user"]);
                    if (reader["pwd"] != DBNull.Value) user.pwd = Convert.ToString(reader["pwd"]).Trim();
                    user.pwd = user.pwd.Substring(user.pwd.IndexOf('-') + 1, user.pwd.Length - user.pwd.IndexOf('-') - 1);
                    users.Add(user);
                }
                reader.Close();

                //обновление паролей
                foreach (var user in users)
                {
                    string newPwd = Utils.CreateMD5StringHash(user.pwd + user.nzp_user + BasePwd);
                    ret = ExecSQL(conn_web, "update users set pwd = " + Utils.EStrNull(newPwd) + ", is_new_pwd = 1 where nzp_user = " + user.nzp_user, true);
                    if (!ret.result)
                    {
                        conn_web.Close();
                        return ret;
                    }
                }
                conn_web.Close();
                return ret;
            }
            catch (Exception ex)
            {
                reader.Close();
                conn_web.Close();
                ret = new Returns(false, ex.Message);
                MonitorLog.WriteLog("Ошибка обновления паролей БД " + (Constants.Viewerror ? " \n " + ex.Message : ""), MonitorLog.typelog.Error, 20, 201, true);
                return ret;
            }
        }


        /// <summary>
        /// Добавляет запись в список моих файлов и возвращает код записи при успешном выполнении операции
        /// </summary>
        /// <param name="finder"></param>
        /// <returns></returns>
        public Returns AddMyFile(ExcelUtility finder)
        {
            IDbConnection conn_db = GetConnection(Constants.cons_Webdata);
            Returns ret = OpenDb(conn_db, true);
            if (!ret.result) return ret;

            #region Проверка на существование таблицы excel_utility, если нет, то создаем
            if (!TableInWebCashe(conn_db, "excel_utility"))
            {
#if PG
                ret = ExecSQL(conn_db,
                                   " create table public.excel_utility " +
                                   " ( nzp_exc      serial not null, " +
                                   " nzp_user     integer not null, " +
                                   " prms         character(200) not null, " +
                                   " stats        integer default 0, " +
                                   " dat_in       timestamp without time zone, " +
                                   " dat_start    timestamp without time zone, " +
                                   " dat_out      timestamp without time zone, " +
                                   " tip          integer default 0 not null, " +
                                   " rep_name     character(100),    " +
                                   " exc_path     character(200), " +
                                   " exc_comment  character(200), " +
                                   " dat_today    date,   " +
                                   " progress     integer default 0, " +
                                   " file_name    character(100) " +
                                   " ) ", true);
#else
                ret = ExecSQL(conn_db,
                                   " create table webdb.excel_utility " +
                                   " ( nzp_exc      serial not null, " +
                                   " nzp_user     integer not null, " +
                                   " prms         char(200) not null, " +
                                   " stats        integer default 0, " +
                                   " dat_in       datetime year to second, " +
                                   " dat_start    datetime year to second, " +
                                   " dat_out      datetime year to second, " +
                                   " tip          integer default 0 not null, " +
                                   " rep_name     char(100),    " +
                                   " exc_path     char(200), " +
                                   " exc_comment  char(200), " +
                                   " dat_today    date,   " +
                                   " progress     integer default 0, " +
                                   " file_name    char(200)" +
                                   " ) ", true);
#endif
                if (!ret.result) return ret;


#if PG
                ExecSQL(conn_db, " create unique index public.ix_exc_1 on public.excel_utility (nzp_exc); ", true);
                ExecSQL(conn_db, " create        index public.ix_exc_2 on public.excel_utility (nzp_user, dat_in); ", true);
                ExecSQL(conn_db, " analyze excel_utility ", true);
#else
                ExecSQL(conn_db, " create unique index webdb.ix_exc_1 on webdb.excel_utility (nzp_exc); ", true);
                ExecSQL(conn_db, " create        index webdb.ix_exc_2 on webdb.excel_utility (nzp_user, dat_in); ", true);
                ExecSQL(conn_db, " Update statistics for table excel_utility ", true);
#endif
            }
            else
            {
                ret = AddFieldToTable(conn_db, "excel_utility", "progress", "integer default 0");
                if (!ret.result) return ret;
            }
            #endregion

            StringBuilder sql = new StringBuilder();
            sql.Append("insert into " + sPublicForMDY + "excel_utility (nzp_user, stats, prms, dat_in, rep_name, exc_comment, dat_today, exc_path, file_name) ");
            sql.Append(" values (" + finder.nzp_user +
                ", " + (int)finder.status +
                ", " + Utils.EStrNull(finder.prms, "empty") +
                "," + Utils.EStrNull(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")) +
                ", " + Utils.EStrNull(finder.rep_name) +
                ", " + Utils.EStrNull(finder.exec_comment) +
                ", " + Utils.EStrNull(DateTime.Now.ToShortDateString()) +
                ", " + Utils.EStrNull(finder.exc_path) +
                ", " + Utils.EStrNull(finder.file_name) +
                ")");

            ret = ExecSQL(conn_db, sql.ToString(), true);
            if (!ret.result) return ret;

            int id = GetSerialValue(conn_db);

            if (finder.status == ExcelUtility.Statuses.InProcess)
            {
                ExecSQL(conn_db, " update " + sPublicForMDY + "excel_utility set dat_start = dat_in where nzp_exc = " + id, true);
            }

            conn_db.Close();
            sql.Remove(0, sql.Length);

            ret.tag = id;

            return ret;
        }






        #region Функция загрузки паспортистки
        public Returns UploadGilec(List<int> lst)
        {
            string conn_kernel = Points.GetConnByPref(Points.Pref);
            IDbConnection conn_db = GetConnection(conn_kernel);
            Returns ret = OpenDb(conn_db, true);
            if (!ret.result) return ret;

            #region ограничить выборку
            string where_nzp_file = "";
            if (lst.Count != 0)
            {
                where_nzp_file = " and nzp_file in (";
                int k = 0;
                foreach (int i in lst)
                {
                    where_nzp_file += i.ToString();
                    if (k < lst.Count - 1) where_nzp_file += ",";
                    k++;
                }
                where_nzp_file += ")";
            }
            #endregion


            IDataReader reader;
            IDataReader reader1;
            string sql = "select * from " + Points.Pref + "_data" + tableDelimiter + "file_gilec where ((comment is null) or (trim(comment) = '')) " + where_nzp_file;
            ret = ExecRead(conn_db, out reader, sql, true);
            if (!ret.result) return ret;

            while (reader.Read())
            {
                if (reader["nzp_file"] != DBNull.Value)
                {
                    #region формирование параметров основного запроса
                    //Получить nzp_kvar
                    string nzp_kvar = "null";
                    if (reader["num_ls"].ToString().Trim() != "")
                    {
                        sql = "select nzp_kvar from " + Points.Pref + "_data" + tableDelimiter + "file_kvar where id = " + reader["num_ls"].ToString().Trim();
                        ret = ExecRead(conn_db, out reader1, sql, true);
                        if ((ret.result) && (reader1.Read()) && (!reader1.IsDBNull(0)) && (reader1["nzp_kvar"].ToString().Trim() != "")) nzp_kvar = reader1["nzp_kvar"].ToString().Trim();
                        reader1.Close();
                    }
                    //Получить pref
                    string pref = "";
                    if (nzp_kvar != "null")
                    {
                        sql = "select pref from " + Points.Pref + "_data" + tableDelimiter + "kvar where nzp_kvar = " + nzp_kvar;
                        ret = ExecRead(conn_db, out reader1, sql, true);
                        if ((ret.result) && (reader1.Read()) && (!reader1.IsDBNull(0)) && (reader1["pref"].ToString().Trim() != "")) pref = reader1["pref"].ToString().Trim();
                        reader1.Close();
                    }
                    // nzp_rod
                    string nzp_rod = "null";
                    if (reader["rod"].ToString().Trim() != "")
                    {
                        sql = "select nzp_rod from " + Points.Pref + "_data" + tableDelimiter + "s_rod " +
                              " where upper(rod) " + plike + "  upper ('" + reader["rod"].ToString().Trim() + pzvzd + "')";
                        ret = ExecRead(conn_db, out reader1, sql, true);
                        if ((ret.result) && (reader1.Read()) && (!reader1.IsDBNull(0)) && (reader1["nzp_rod"].ToString().Trim() != "")) nzp_rod = reader1["nzp_rod"].ToString().Trim();
                        reader1.Close();
                    }
                    #region Устарело
                    //// nzp_lnmr
                    //string nzp_land_mr = "null";
                    //if (reader["lnmr"].ToString().Trim() != "")
                    //{
                    //    sql = "select nzp_land from " + Points.Pref + "_data"+tableDelimiter + "s_land " +
                    //          "where upper(land) like  upper ('" + reader["lnmr"].ToString().Trim() + "*')";
                    //    ret = ExecRead(conn_db, out reader1, sql, true);
                    //    if ((ret.result) && (reader1.Read()) && (!reader1.IsDBNull(0)) && (reader1["nzp_land"].ToString().Trim() != "")) nzp_land_mr = reader1["nzp_land"].ToString().Trim();
                    //}
                    //// nzp_stmr
                    //string nzp_stat_mr = "null";
                    //if ((reader["stmr"].ToString().Trim() != "") && (nzp_land_mr != "null"))
                    //{
                    //    sql = "select nzp_stat from " + Points.Pref + "_data"+tableDelimiter + "s_stat " +
                    //          "where upper(stat) like  upper ('" + reader["stmr"].ToString().Trim() + "*')" +
                    //          " and nzp_land =" + nzp_land_mr;
                    //    ret = ExecRead(conn_db, out reader1, sql, true);
                    //    if ((ret.result) && (reader1.Read()) && (!reader1.IsDBNull(0)) && (reader1["nzp_stat"].ToString().Trim() != "")) nzp_stat_mr = reader1["nzp_stat"].ToString().Trim();
                    //}
                    //// nzp_tnmr
                    //string nzp_town_mr = "null";
                    //if ((reader["tnmr"].ToString().Trim() != "") && (nzp_stat_mr != "null"))
                    //{
                    //    sql = "select nzp_town from " + Points.Pref + "_data"+tableDelimiter + "s_town " +
                    //          "where upper(town) like  upper ('" + reader["tnmr"].ToString().Trim() + "*')" +
                    //          " and nzp_stat =" + nzp_stat_mr;
                    //    ret = ExecRead(conn_db, out reader1, sql, true);
                    //    if ((ret.result) && (reader1.Read()) && (!reader1.IsDBNull(0)) && (reader1["nzp_town"].ToString().Trim() != "")) nzp_town_mr = reader1["nzp_town"].ToString().Trim();
                    //}
                    //// nzp_rnmr
                    //string nzp_raj_mr = "null";
                    //if ((reader["rnmr"].ToString().Trim() != "") && (nzp_town_mr != "null"))
                    //{
                    //    sql = "select nzp_raj from " + Points.Pref + "_data"+tableDelimiter + "s_rajon " +
                    //          "where upper(rajon) like  upper ('" + reader["rnmr"].ToString().Trim() + "*')" +
                    //          " and nzp_town =" + nzp_town_mr;
                    //    ret = ExecRead(conn_db, out reader1, sql, true);
                    //    if ((ret.result) && (reader1.Read()) && (!reader1.IsDBNull(0)) && (reader1["nzp_raj"].ToString().Trim() != "")) nzp_raj_mr = reader1["nzp_raj"].ToString().Trim();
                    //}
                    //// nzp_lnop
                    //string nzp_land_op = "null";
                    //if (reader["lnop"].ToString().Trim() != "")
                    //{
                    //    sql = "select nzp_land from " + Points.Pref + "_data"+tableDelimiter + "s_land " +
                    //          "where upper(land) like  upper ('" + reader["lnop"].ToString().Trim() + "*')";
                    //    ret = ExecRead(conn_db, out reader1, sql, true);
                    //    if ((ret.result) && (reader1.Read()) && (!reader1.IsDBNull(0)) && (reader1["nzp_land"].ToString().Trim() != "")) nzp_land_op = reader1["nzp_land"].ToString().Trim();
                    //}
                    //// nzp_stmr
                    //string nzp_stat_op = "null";
                    //if ((reader["stop_"].ToString().Trim() != "") && (nzp_land_op != "null"))
                    //{
                    //    sql = "select nzp_stat from " + Points.Pref + "_data"+tableDelimiter + "s_stat " +
                    //          "where upper(stat) like  upper ('" + reader["stop_"].ToString().Trim() + "*')" +
                    //          " and nzp_land =" + nzp_land_op;
                    //    ret = ExecRead(conn_db, out reader1, sql, true);
                    //    if ((ret.result) && (reader1.Read()) && (!reader1.IsDBNull(0)) && (reader1["nzp_stat"].ToString().Trim() != "")) nzp_stat_op = reader1["nzp_stat"].ToString().Trim();
                    //}
                    //// nzp_tnop
                    //string nzp_town_op = "null";
                    //if ((reader["tnop"].ToString().Trim() != "") && (nzp_stat_op != "null"))
                    //{
                    //    sql = "select nzp_town from " + Points.Pref + "_data"+tableDelimiter + "s_town " +
                    //          "where upper(town) like  upper ('" + reader["tnop"].ToString().Trim() + "*')" +
                    //          " and nzp_stat =" + nzp_stat_op;
                    //    ret = ExecRead(conn_db, out reader1, sql, true);
                    //    if ((ret.result) && (reader1.Read()) && (!reader1.IsDBNull(0)) && (reader1["nzp_town"].ToString().Trim() != "")) nzp_town_op = reader1["nzp_town"].ToString().Trim();
                    //}
                    //// nzp_rnop
                    //string nzp_raj_op = "null";
                    //if ((reader["rnop"].ToString().Trim() != "") && (nzp_town_op != "null"))
                    //{
                    //    sql = "select nzp_raj from " + Points.Pref + "_data"+tableDelimiter + "s_rajon " +
                    //          "where upper(rajon) like  upper ('" + reader["rnop"].ToString().Trim() + "*')" +
                    //          " and nzp_town =" + nzp_town_op;
                    //    ret = ExecRead(conn_db, out reader1, sql, true);
                    //    if ((ret.result) && (reader1.Read()) && (!reader1.IsDBNull(0)) && (reader1["nzp_raj"].ToString().Trim() != "")) nzp_raj_op = reader1["nzp_raj"].ToString().Trim();
                    //}
                    //// nzp_lnku
                    //string nzp_land_ku = "null";
                    //if (reader["lnku"].ToString().Trim() != "")
                    //{
                    //    sql = "select nzp_land from " + Points.Pref + "_data"+tableDelimiter + "s_land " +
                    //          "where upper(land) like  upper ('" + reader["lnku"].ToString().Trim() + "*')";
                    //    ret = ExecRead(conn_db, out reader1, sql, true);
                    //    if ((ret.result) && (reader1.Read()) && (!reader1.IsDBNull(0)) && (reader1["nzp_land"].ToString().Trim() != "")) nzp_land_ku = reader1["nzp_land"].ToString().Trim();
                    //}
                    //// nzp_stmr
                    //string nzp_stat_ku = "null";
                    //if ((reader["stku"].ToString().Trim() != "") && (nzp_land_ku != "null"))
                    //{
                    //    sql = "select nzp_stat from " + Points.Pref + "_data"+tableDelimiter + "s_stat " +
                    //          "where upper(stat) like  upper ('" + reader["stku"].ToString().Trim() + "*')" +
                    //          " and nzp_land =" + nzp_land_ku;
                    //    ret = ExecRead(conn_db, out reader1, sql, true);
                    //    if ((ret.result) && (reader1.Read()) && (!reader1.IsDBNull(0)) && (reader1["nzp_stat"].ToString().Trim() != "")) nzp_stat_ku = reader1["nzp_stat"].ToString().Trim();
                    //}
                    //// nzp_tnku
                    //string nzp_town_ku = "null";
                    //if ((reader["tnku"].ToString().Trim() != "") && (nzp_stat_ku != "null"))
                    //{
                    //    sql = "select nzp_town from " + Points.Pref + "_data"+tableDelimiter + "s_town " +
                    //          "where upper(town) like  upper ('" + reader["tnku"].ToString().Trim() + "*')" +
                    //          " and nzp_stat =" + nzp_stat_ku;
                    //    ret = ExecRead(conn_db, out reader1, sql, true);
                    //    if ((ret.result) && (reader1.Read()) && (!reader1.IsDBNull(0)) && (reader1["nzp_town"].ToString().Trim() != "")) nzp_town_ku = reader1["nzp_town"].ToString().Trim();
                    //}
                    //// nzp_rnku
                    //string nzp_raj_ku = "null";
                    //if ((reader["rnku"].ToString().Trim() != "") && (nzp_town_ku != "null"))
                    //{
                    //    sql = "select nzp_raj from " + Points.Pref + "_data"+tableDelimiter + "s_rajon " +
                    //          "where upper(rajon) like  upper ('" + reader["rnku"].ToString().Trim() + "*')" +
                    //          " and nzp_town =" + nzp_town_ku;
                    //    ret = ExecRead(conn_db, out reader1, sql, true);
                    //    if ((ret.result) && (reader1.Read()) && (!reader1.IsDBNull(0)) && (reader1["nzp_raj"].ToString().Trim() != "")) nzp_raj_ku = reader1["nzp_raj"].ToString().Trim();
                    //}
                    #endregion
                    #endregion

                    if ((nzp_kvar != "null") && (pref != "null"))
                    {
                        #region основной запрос
                        sql = " insert into " + pref + "_data" + tableDelimiter + "kart " +
                                                      " (nzp_gil, isactual, fam, ima, otch, dat_rog, dat_smert, fam_c, ima_c, otch_c ,dat_rog_c ,gender ,tprp ,dat_prop ,dat_oprp ,dat_pvu ,who_pvu ,dat_svu ," +
                                                      "  nzp_user , nzp_tkrt ,nzp_kvar ,rem_p ,namereg ,kod_namereg_prn ,nzp_rod ,rodstvo,nzp_celp ,nzp_celu ,nzp_nkrt ,nzp_dok ,serij ,nomer ,vid_mes ,vid_dat , " +
                                                      "  strana_mr, region_mr, okrug_mr, gorod_mr, npunkt_mr, rem_mr ,strana_op, region_op, okrug_op, gorod_op, npunkt_op, rem_op, strana_ku, region_ku, okrug_ku, " +
                                                      "  gorod_ku, npunkt_ku, rem_ku ,dat_prib ,dat_ubit ,dat_sost ,dat_ofor ,kod_podrazd) " +
                                                        " values ( " +
                                                        reader["nzp_gil"].ToString().Trim() + ", " +
                                                        " 1, " +
                                                        "'" + reader["fam"].ToString().Trim() + "', " +
                                                        "'" + reader["ima"].ToString().Trim() + "', " +
                                                        "'" + reader["otch"].ToString().Trim() + "', ";
                        if (reader["dat_rog"] != DBNull.Value) sql += "'" + Convert.ToDateTime(reader["dat_rog"]).ToShortDateString().ToString().Trim() + "', ";
                        else sql += " null , ";
                        sql += " null, ";
                        if ((reader["fam_c"] != DBNull.Value) && (reader["fam_c"].ToString().Trim() != "")) sql += "'" + reader["fam_c"].ToString().Trim() + "', ";
                        else sql += " null , ";
                        if ((reader["ima_c"] != DBNull.Value) && (reader["ima_c"].ToString().Trim() != "")) sql += "'" + reader["ima_c"].ToString().Trim() + "', ";
                        else sql += " null , ";
                        if ((reader["otch_c"] != DBNull.Value) && (reader["otch_c"].ToString().Trim() != "")) sql += "'" + reader["otch_c"].ToString().Trim() + "', ";
                        else sql += " null , ";
                        if (reader["dat_rog_c"] != DBNull.Value) sql += "'" + Convert.ToDateTime(reader["dat_rog_c"]).ToShortDateString().ToString().Trim() + "', ";
                        else sql += " null , ";
                        sql += "'" + reader["gender"].ToString().Trim() + "', " +
                                "'" + reader["tprp"].ToString().Trim() + "', ";
                        if (reader["dat_prop"] != DBNull.Value) sql += "'" + Convert.ToDateTime(reader["dat_prop"]).ToShortDateString().ToString().Trim() + "', ";//dat_prop
                        else sql += " null , ";
                        if (reader["dat_oprp"] != DBNull.Value) sql += "'" + Convert.ToDateTime(reader["dat_oprp"]).ToShortDateString().ToString().Trim() + "', ";//dat_oprp
                        else sql += " null , ";
                        if (reader["dat_pvu"] != DBNull.Value) sql += "'" + Convert.ToDateTime(reader["dat_pvu"]).ToShortDateString().ToString().Trim() + "', ";
                        else sql += " null , ";
                        if ((reader["who_pvu"] != DBNull.Value) && (reader["who_pvu"].ToString().Trim() != "")) sql += "'" + reader["who_pvu"].ToString().Trim() + "', ";
                        else sql += " null , ";
                        if (reader["dat_svu"] != DBNull.Value) sql += "'" + Convert.ToDateTime(reader["dat_svu"]).ToShortDateString().ToString().Trim() + "', ";
                        else sql += " null , ";
                        sql += " -1, " +//nzp_user
                                reader["nzp_tkrt"].ToString().Trim() + ", " +
                                nzp_kvar + ", ";
                        if ((reader["rem_p"] != DBNull.Value) && (reader["rem_p"].ToString().Trim() != "")) sql += "'" + reader["rem_p"].ToString().Trim() + "', ";
                        else sql += " null , ";
                        if ((reader["namereg"] != DBNull.Value) && (reader["namereg"].ToString().Trim() != "")) sql += "'" + reader["namereg"].ToString().Trim() + "', ";
                        else sql += " null , ";
                        if ((reader["kod_namereg"] != DBNull.Value) && (reader["kod_namereg"].ToString().Trim() != "")) sql += "'" + reader["kod_namereg"].ToString().Trim() + "', ";
                        else sql += " null , ";
                        sql += nzp_rod + ", ";
                        if ((reader["rod"] != DBNull.Value) && (reader["rod"].ToString().Trim() != "")) sql += "'" + reader["rod"].ToString().Trim() + "', ";
                        else sql += " null , ";
                        if ((reader["nzp_celp"] != DBNull.Value) && (reader["nzp_celp"].ToString().Trim() != "")) sql += "'" + reader["nzp_celp"].ToString().Trim() + "', ";
                        else sql += " null , ";
                        if ((reader["nzp_celu"] != DBNull.Value) && (reader["nzp_celu"].ToString().Trim() != "")) sql += "'" + reader["nzp_celu"].ToString().Trim() + "', ";
                        else sql += " null , ";
                        sql += " null , " +
                                reader["nzp_dok"].ToString().Trim() + ", ";
                        if ((reader["serij"] != DBNull.Value) && (reader["serij"].ToString().Trim() != "")) sql += "'" + reader["serij"].ToString().Trim() + "', ";
                        else sql += " null , ";
                        if ((reader["nomer"] != DBNull.Value) && (reader["nomer"].ToString().Trim() != "")) sql += "'" + reader["nomer"].ToString().Trim() + "', ";
                        else sql += " null , ";
                        if ((reader["vid_mes"] != DBNull.Value) && (reader["vid_mes"].ToString().Trim() != "")) sql += "'" + reader["vid_mes"].ToString().Trim() + "', ";
                        else sql += " null , ";
                        if (reader["vid_dat"] != DBNull.Value) sql += "'" + Convert.ToDateTime(reader["vid_dat"]).ToShortDateString().ToString().Trim() + "', ";
                        else sql += " null , ";
                        if ((reader["strana_mr"] != DBNull.Value) && (reader["strana_mr"].ToString().Trim() != "")) sql += "'" + reader["strana_mr"].ToString().Trim() + "', ";
                        else sql += " null , ";
                        if ((reader["region_mr"] != DBNull.Value) && (reader["region_mr"].ToString().Trim() != "")) sql += "'" + reader["region_mr"].ToString().Trim() + "', ";
                        else sql += " null , ";
                        if ((reader["okrug_mr"] != DBNull.Value) && (reader["okrug_mr"].ToString().Trim() != "")) sql += "'" + reader["okrug_mr"].ToString().Trim() + "', ";
                        else sql += " null , ";
                        if ((reader["gorod_mr"] != DBNull.Value) && (reader["gorod_mr"].ToString().Trim() != "")) sql += "'" + reader["gorod_mr"].ToString().Trim() + "', ";
                        else sql += " null , ";
                        if ((reader["npunkt_mr"] != DBNull.Value) && (reader["npunkt_mr"].ToString().Trim() != "")) sql += "'" + reader["npunkt_mr"].ToString().Trim() + "', ";
                        else sql += " null , ";
                        if ((reader["rem_mr"] != DBNull.Value) && (reader["rem_mr"].ToString().Trim() != "")) sql += "'" + reader["rem_mr"].ToString().Trim() + "', ";
                        else sql += " null , ";
                        if ((reader["strana_op"] != DBNull.Value) && (reader["strana_op"].ToString().Trim() != "")) sql += "'" + reader["strana_op"].ToString().Trim() + "', ";
                        else sql += " null , ";
                        if ((reader["region_op"] != DBNull.Value) && (reader["region_op"].ToString().Trim() != "")) sql += "'" + reader["region_op"].ToString().Trim() + "', ";
                        else sql += " null , ";
                        if ((reader["okrug_op"] != DBNull.Value) && (reader["okrug_op"].ToString().Trim() != "")) sql += "'" + reader["okrug_op"].ToString().Trim() + "', ";
                        else sql += " null , ";
                        if ((reader["gorod_op"] != DBNull.Value) && (reader["gorod_op"].ToString().Trim() != "")) sql += "'" + reader["gorod_op"].ToString().Trim() + "', ";
                        else sql += " null , ";
                        if ((reader["npunkt_op"] != DBNull.Value) && (reader["npunkt_op"].ToString().Trim() != "")) sql += "'" + reader["npunkt_op"].ToString().Trim() + "', ";
                        else sql += " null , ";
                        if ((reader["rem_op"] != DBNull.Value) && (reader["rem_op"].ToString().Trim() != "")) sql += "'" + reader["rem_op"].ToString().Trim() + "', ";
                        else sql += " null , ";
                        if ((reader["strana_ku"] != DBNull.Value) && (reader["strana_ku"].ToString().Trim() != "")) sql += "'" + reader["strana_ku"].ToString().Trim() + "', ";
                        else sql += " null , ";
                        if ((reader["region_ku"] != DBNull.Value) && (reader["region_ku"].ToString().Trim() != "")) sql += "'" + reader["region_ku"].ToString().Trim() + "', ";
                        else sql += " null , ";
                        if ((reader["okrug_ku"] != DBNull.Value) && (reader["okrug_ku"].ToString().Trim() != "")) sql += "'" + reader["okrug_ku"].ToString().Trim() + "', ";
                        else sql += " null , ";
                        if ((reader["gorod_ku"] != DBNull.Value) && (reader["gorod_ku"].ToString().Trim() != "")) sql += "'" + reader["gorod_ku"].ToString().Trim() + "', ";
                        else sql += " null , ";
                        if ((reader["npunkt_ku"] != DBNull.Value) && (reader["npunkt_ku"].ToString().Trim() != "")) sql += "'" + reader["npunkt_ku"].ToString().Trim() + "', ";
                        else sql += " null , ";
                        if ((reader["rem_ku"] != DBNull.Value) && (reader["rem_ku"].ToString().Trim() != "")) sql += "'" + reader["rem_ku"].ToString().Trim() + "', ";
                        else sql += " null , ";
                        if (reader["dat_prop"] != DBNull.Value) sql += "'" + Convert.ToDateTime(reader["dat_prop"]).ToShortDateString().ToString().Trim() + "', ";//dat_prib 
                        else sql += " null , ";
                        if (reader["dat_oprp"] != DBNull.Value) sql += "'" + Convert.ToDateTime(reader["dat_oprp"]).ToShortDateString().ToString().Trim() + "', ";//dat_ubit
                        else sql += " null , ";
                        if (reader["dat_sost"] != DBNull.Value) sql += "'" + Convert.ToDateTime(reader["dat_sost"]).ToShortDateString().ToString().Trim() + "', ";
                        else sql += " null , ";
                        if (reader["dat_ofor"] != DBNull.Value) sql += "'" + Convert.ToDateTime(reader["dat_ofor"]).ToShortDateString().ToString().Trim() + "', ";
                        if ((reader["kod_podrazd"] != DBNull.Value) && (reader["kod_podrazd"].ToString().Trim() != "")) sql += "'" + reader["kod_podrazd"].ToString().Trim() + "', ";
                        else sql += " null ";
                        sql += " )";
                        ret = ExecSQL(conn_db, sql, true);
                        if (ret.result)
                        {
                            try
                            {
                                sql = "update " + Points.Pref + "_data" + tableDelimiter + "file_gilec set comment = 'загружен' where nzp_kart = " + reader["nzp_kart"].ToString().Trim();
                                ret = ExecSQL(conn_db, sql, true);
                            }
                            catch (Exception ex)
                            {
                                ret.result = false;
                                ret.text = ex.Message;
                                MonitorLog.WriteLog("Ошибка в функции UploadGilec:\n" + (ex.Message != "" ? ex.Message : ret.text), MonitorLog.typelog.Error, true);
                            }
                            finally
                            {
                            }
                        }
                        #endregion
                    }
                }
            }
            reader.Close();
            conn_db.Close();
            return ret;
        }
        #endregion Функция загрузки паспортистки
    }
}
