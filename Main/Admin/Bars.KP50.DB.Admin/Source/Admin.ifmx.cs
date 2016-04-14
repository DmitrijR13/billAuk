using System;
using System.Collections.Generic;
using System.Data;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;
using System.Text;
using System.Linq;


namespace STCLINE.KP50.DataBase
{
    public partial class DbAdmin : DbAdminClient
    {
        Int32 PmaxVisible = 200;
        private string tableUsersRecovery { get { return "users_recovery"; } }

        private delegate void RemoveUserLockMakeQuery(IDbTransaction trans, string prefix, int UserId);
        public Returns RemoveUserLock(Finder WebUser)
        {
            Returns ret = Utils.InitReturns();
            IDbConnection conn_kernel = GetConnection(Constants.cons_Kernel);
            IDbTransaction transaction = null;
            //DbWorkUser db = new DbWorkUser();
            RemoveUserLockMakeQuery MakeQuery = delegate(IDbTransaction trans, string prefix, int UserId)
            {
                if (trans != null)
                {
                    if (prefix != Points.Pref) ExecSQL(conn_kernel, trans, string.Format("UPDATE {0}_data{2}counters_spis SET (dat_block, user_block) = (null, null) WHERE user_block = {1}", prefix, UserId, DBManager.tableDelimiter), true);
                    ExecSQL(conn_kernel, trans, string.Format("UPDATE {0}_data{2}prm_2 SET (dat_block, user_block) = (null, null) WHERE user_block = {1}", prefix, UserId, DBManager.tableDelimiter), true);
                    ExecSQL(conn_kernel, trans, string.Format("DELETE FROM {0}_data{2}kvar_block WHERE nzp_user = {1}", prefix, UserId, DBManager.tableDelimiter), true);
                    ExecSQL(conn_kernel, trans, string.Format("DELETE FROM {0}_data{2}pack_ls_block WHERE nzp_user = {1}", Points.Pref, UserId, DBManager.tableDelimiter), true);
                }
            };

            try
            {
                conn_kernel.Open();
                transaction = conn_kernel.BeginTransaction();
                //MakeQuery(transaction, (WebUser.pref = Points.Pref), db.GetLocalUser(conn_kernel, transaction, WebUser, out ret));
                MakeQuery(transaction, (WebUser.pref = Points.Pref), WebUser.nzp_user);
                //Points.PointList.ForEach(point => MakeQuery(transaction, (WebUser.pref = point.pref), db.GetLocalUser(conn_kernel, transaction, WebUser, out ret)));
                Points.PointList.ForEach(point => MakeQuery(transaction, (WebUser.pref = point.pref), WebUser.nzp_user));
                transaction.Commit();
            }
            catch (Exception ex)
            {
                if (transaction != null) transaction.Rollback();
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка в функции RemoveUserLock:\n" + ex.Message, MonitorLog.typelog.Error, true);
            }
            finally { if (conn_kernel.State != ConnectionState.Closed) conn_kernel.Close(); }

            return ret;
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
                 " From s_roles r left outer join pages p on   r.page_url = p.nzp_page " + tableUserp + where + " Order by 1, 3 " + skip;
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
            ExecSQL(conn_web,
                  " set search_path to 'public'"
                  , true);
#endif
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

        private string makeWhereForProcess(FonTask finder, string alias, ref Returns ret)
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
            if (Utils.GetParams(finder.prms, Constants.act_process_in_queue.ToString())) prms += "," + FonTask.getKodInfo(Constants.act_process_in_queue);
            if (Utils.GetParams(finder.prms, Constants.act_process_active.ToString())) prms += "," + FonTask.getKodInfo(Constants.act_process_active);
            if (Utils.GetParams(finder.prms, Constants.act_process_finished.ToString())) prms += "," + FonTask.getKodInfo(Constants.act_process_finished);
            if (Utils.GetParams(finder.prms, Constants.act_process_with_errors.ToString())) prms += "," + FonTask.getKodInfo(Constants.act_process_with_errors);
            if (prms != "") where += " and " + alias + "kod_info in (" + prms.Substring(1, prms.Length - 1) + ")";

            return where;
        }

        private string makeWhereForProcess(FonTaskWithYearMonth finder, string alias, ref Returns ret)
        {
            string where = makeWhereForProcess((FonTask)finder, alias, ref ret);

            if (alias != "") alias = alias + ".";

            if (finder.year_ > 0) where += " and " + alias + "year_ = " + finder.year_;
            if (finder.month_ > 0) where += " and " + alias + "month_ = " + finder.month_;

            return where;
        }

        private string makeWhereForProcess(SaldoFonTask finder, string alias, ref Returns ret)
        {
            string where = makeWhereForProcess(finder as FonTaskWithYearMonth, alias, ref ret);

            if (alias != "") alias = alias + ".";

            if (finder.nzp_area > 0) where += " and " + alias + "nzp_area = " + finder.nzp_area;

            return where;
        }

        private string makeWhereForProcess(CalcFonTask finder, string alias, ref Returns ret)
        {
            string where = makeWhereForProcess(finder as FonTaskWithYearMonth, alias, ref ret);

            if (alias != "") alias = alias + ".";

            if (finder.nzp >= 0) where += " and " + alias + "nzp = " + finder.nzp;
            if (finder.nzpt > 0) where += " and " + alias + "nzpt = " + finder.nzpt;
            if (finder.TaskType != CalcFonTask.Types.Unknown) where += " and " + alias + "task = " + (int)finder.TaskType;
            if (finder.prior > 0) where += " and " + alias + "prior = " + finder.prior;

            return where;
        }

        private string makeWhereForProcess(BillFonTask finder, string alias, ref Returns ret)
        {
            string where = makeWhereForProcess(finder as FonTaskWithYearMonth, alias, ref ret);

            if (alias != "") alias = alias + ".";

            if (finder.nzp_key > 0) where += " and " + alias + "nzp_key = " + finder.nzp_key;
            if (finder.nzp_area > 0) where += " and " + alias + "nzp_area = " + finder.nzp_area;
            if (finder.nzp_geu > 0) where += " and " + alias + "nzp_geu = " + finder.nzp_geu;
            if (finder.nzp_wp > 0) where += " and " + alias + "nzp_wp = " + finder.nzp_wp;

            return where;
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
            string sql = "Select dat_oper From " + Points.Pref + "_data" + tableDelimiter + "fn_curoperday ";
            var dtr = ClassDBUtils.OpenSQL(sql, conn_db);
            foreach (DataRow rrR in dtr.resultData.Rows)
            {
                dat_oper_day = Convert.ToString(rrR["dat_oper"]).Substring(0, 10);
            }


            sql = "select id, datpaytorder, numpaytorder, sumofpayt, numofpayt, filename from " + Points.Pref + "_data" + tableDelimiter + "_reestrpack";
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

                string sql1 = "insert into " + Points.Pref + "_fin_" + dat_oper_day.Substring(8, 2) + tableDelimiter + "pack (nzp_pack, par_pack, pack_type, nzp_bank, nzp_supp, nzp_oper, num_pack, dat_uchet, dat_pack, " +
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
                      + Points.Pref + "_data" + tableDelimiter + "_reestr where numpaytorder=" + numPaytOrder.ToString();
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
                    sql1 = "select pkod from " + Points.Pref + "_data" + tableDelimiter + "kvar where nzp_kvar =" + persAccKlient + ";";
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


                    string sql2 = "insert into " + Points.Pref + "_fin_" + dat_oper_day.Substring(8, 2) + tableDelimiter + "pack_ls (nzp_pack, prefix_ls, pkod, num_ls, g_sum_ls," +
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


        public List<AreaCodes> GetAreaCodes(AreaCodes finder, out Returns ret)
        {
            bool pkMode = GlobalSettings.NewGeneratePkodMode;

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
            if (!pkMode)
            {
                if (finder.area != "") where += " and a.area like '%" + finder.area + "%'";
                if (finder.nzp_area > 0) where += " and ac.nzp_area = " + finder.nzp_area;
            }
            else
            {
                if (finder.payer != "") where += " and a.payer like '%" + finder.payer + "%'";
                if (finder.nzp_payer > 0) where += " and ac.nzp_payer = " + finder.nzp_payer;
            }


            string sql;
            //Определить общее количество записей
            if (!pkMode)
                sql = "select count(*) from " + tables.area_codes + " ac, " +
                           tables.area + " a where ac.nzp_area = a.nzp_area " + where;
            else
                sql = "select count(*) from " + tables.area_codes + " ac, " +
                    tables.payer + " a where ac.nzp_payer = a.nzp_payer " + where;


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
            if (!pkMode)
                sql = " select ac.code, ac.nzp_area, a.area, ac.is_active from " + tables.area_codes + " ac, " +
                    tables.area + " a where ac.nzp_area = a.nzp_area " + where + " order by code";
            else
                sql = " select ac.code, ac.nzp_payer, p.payer, ac.is_active from " + tables.area_codes + " ac, " +
         tables.payer + " p where ac.nzp_payer = p.nzp_payer " + where + " order by code";

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

                    if (!pkMode)
                    {
                        if (reader["area"] != DBNull.Value) zap.area = Convert.ToString(reader["area"]).Trim();
                        if (reader["nzp_area"] != DBNull.Value) zap.nzp_area = Convert.ToInt32(reader["nzp_area"]);
                    }
                    else
                    {
                        if (reader["payer"] != DBNull.Value) zap.payer = Convert.ToString(reader["payer"]).Trim();
                        if (reader["nzp_payer"] != DBNull.Value) zap.nzp_payer = Convert.ToInt32(reader["nzp_payer"]);
                    }
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
            string sql = "select max(code) as code from " + tables.area_codes;
            object count = ExecScalar(conn_db, sql, out ret, true);
            int code;
            try { code = Convert.ToInt32(count) + 1; }
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
            bool pkMode = GlobalSettings.NewGeneratePkodMode;
            if (finder.nzp_user < 1) return new Returns(false, "Не задан пользователь");

            if (finder.code <= 0)
            {
                if (!pkMode)
                {
                    if (finder.nzp_area <= 0) return new Returns(false, "Не задана Управляющая организация", -1);
                }
                else
                {
                    if (finder.nzp_payer <= 0) return new Returns(false, "Не задан контрагент", -1);
                }

                if (finder.code_po > 0)
                {
                    if (finder.code_s <= 0) return new Returns(false, "Не верно задан диапазон", -1);
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
            int nzpUser = finder.nzp_user;

            /*DbWorkUser db = new DbWorkUser();
            int nzpUser = db.GetLocalUser(conn_db, null, finder, out ret); //локальный пользователь      
            db.Close();
            if (!ret.result) return ret;*/

            #endregion

            string sql;
            if (!pkMode)
            {
                if (finder.code > 0)
                {

                    sql = " select nzp_area from " + tables.area_codes + " where code = " + finder.code;

                    object count = ExecScalar(conn_db, sql, out ret, true);
                    int nzp_area;
                    try
                    {
                        nzp_area = Convert.ToInt32(count);
                    }
                    catch (Exception e)
                    {
                        ret = new Returns(false,
                            "Ошибка при определении nzp_area: " + (Constants.Debug ? e.Message : ""));
                        MonitorLog.WriteLog("Ошибка SaveAreaCodes " + (Constants.Viewerror ? "\n" + e.Message : ""),
                            MonitorLog.typelog.Error, 20, 201, true);
                        return ret;
                    }
                    sql = "update " + tables.area_codes + " set is_active = 0 where nzp_area = " + nzp_area;
                    ret = ExecSQL(conn_db, sql, true);

                    sql = "update " + tables.area_codes + " set is_active = " + finder.is_active + ", changed_by = " +
                          nzpUser +
#if PG
 ", changed_on = now() " +
#else
                ", changed_on = current "+
#endif
 " where code = " + finder.code;
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

                    if (finder.code_s > 0 && finder.code_po > 0)
                    {
                        string errcodes = "";
                        string addedcodes = "";
                        for (int i = finder.code_s; i <= finder.code_po; i++)
                        {
                            sql = "select count(*) from " + tables.area_codes + " where code = " + i;
                            object count = ExecScalar(conn_db, sql, out ret, true);
                            int cnt;
                            try
                            {
                                cnt = Convert.ToInt32(count);
                            }
                            catch (Exception e)
                            {
                                ret = new Returns(false, "Ошибка: " + (Constants.Debug ? e.Message : ""));
                                MonitorLog.WriteLog(
                                    "Ошибка SaveAreaCodes " + (Constants.Viewerror ? "\n" + e.Message : ""),
                                    MonitorLog.typelog.Error, 20, 201, true);
                                return ret;
                            }
                            if (cnt == 0)
                            {
                                int isactive = 0;
                                if (finder.active_num > 0)
                                {
                                    if (finder.active_num == i)
                                    {
                                        isactive = 1;
                                        sql = "update " + tables.area_codes + " set is_active = 0 where nzp_area = " +
                                              finder.nzp_area;
                                        ret = ExecSQL(conn_db, sql, true);
                                    }
                                }
                                sql = "insert into " + tables.area_codes +
                                      " (code, nzp_area, changed_by, changed_on, is_active) values (" +
                                      i + ", " + finder.nzp_area + ", " + nzpUser +
#if PG
 ", now(), " +
#else
                                ", current, "+
#endif
 isactive + ")";
                                ret = ExecSQL(conn_db, sql, true);
                                if (ret.result)
                                {
                                    if (addedcodes == "") addedcodes += i.ToString();
                                    else addedcodes += ", " + i;
                                    string seq = Points.Pref + "_data.kvar_pkod10_" + i + "_seq";
#if PG

                                    sql = "DROP SEQUENCE " + seq;
                                    ExecSQL(conn_db, sql, false);
                                    sql = "CREATE SEQUENCE " + seq +
                                          " INCREMENT 1 MINVALUE 1 MAXVALUE 9223372036854775807 " +
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
                            else if (errcodes == "") errcodes += i.ToString();
                            else errcodes += ", " + i;
                        }

                        if (ret.result)
                        {
                            if (errcodes == "") ret.text = "Создание прошло успешно";
                            else
                            {
                                if (addedcodes != "") ret.text = "Коды (" + addedcodes + ") были успешно добавлены.";
                                if (errcodes != "")
                                    ret.text = " Коды (" + errcodes + ") не были добавлены, так как уже существуют в БД";
                            }
                        }
                        else
                        {
                            if (addedcodes != "") ret.text = "Коды (" + addedcodes + ") были успешно добавлены.";
                            if (errcodes != "")
                                ret.text = " Коды (" + errcodes + ") не были добавлены, так как уже существуют в БД";
                        }
                        ret.tag = -1;
                    }
                    conn_db.Close();
                    return ret;
                }
            }
            // Уникальные коды контрагентов
            else
            {
                if (finder.code > 0)
                {

                    sql = " select nzp_payer from " + tables.area_codes + " where code = " + finder.code;

                    object count = ExecScalar(conn_db, sql, out ret, true);
                    int nzp_payer;
                    try
                    {
                        nzp_payer = Convert.ToInt32(count);
                    }
                    catch (Exception e)
                    {
                        ret = new Returns(false,
                            "Ошибка при определении nzp_payer: " + (Constants.Debug ? e.Message : ""));
                        MonitorLog.WriteLog("Ошибка SaveAreaCodes " + (Constants.Viewerror ? "\n" + e.Message : ""),
                            MonitorLog.typelog.Error, 20, 201, true);
                        return ret;
                    }
                    sql = "update " + tables.area_codes + " set is_active = 0 where nzp_payer = " + nzp_payer;
                    ret = ExecSQL(conn_db, sql, true);

                    sql = "update " + tables.area_codes + " set is_active = " + finder.is_active + ", changed_by = " +
                          nzpUser +
#if PG
 ", changed_on = now() " +
#else
                ", changed_on = current "+
#endif
 " where code = " + finder.code;
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
                    bool activeNumInRangeUniqueCodes = false; // true- заданный действующий код в диапазоне заданных уникальных кодов, false- вне диапазона           
                    if (finder.code_s > 0 && finder.code_po > 0)
                    {
                        if (finder.active_num > 0)
                        {
                            if (finder.active_num >= finder.code_s && finder.active_num <= finder.code_po)
                                activeNumInRangeUniqueCodes = true;
                        }
                        string errcodes = "";
                        string addedcodes = "";
                        string dopMsg = "";
                        string changedActiveNumMsg = "";
                        for (int i = finder.code_s; i <= finder.code_po; i++)
                        {
                            sql = "select count(*) from " + tables.area_codes + " where code = " + i;
                            object count = ExecScalar(conn_db, sql, out ret, true);
                            int cnt;
                            try
                            {
                                cnt = Convert.ToInt32(count);
                            }
                            catch (Exception e)
                            {
                                ret = new Returns(false, "Ошибка: " + (Constants.Debug ? e.Message : ""));
                                MonitorLog.WriteLog(
                                    "Ошибка SaveAreaCodes " + (Constants.Viewerror ? "\n" + e.Message : ""),
                                    MonitorLog.typelog.Error, 20, 201, true);
                                return ret;
                            }
                            // Если добавляемый уникальный код не существует в БД
                            if (cnt == 0)
                            {
                                int isactive = 0;
                                if (activeNumInRangeUniqueCodes)
                                {
                                    if (finder.active_num == i)
                                    {
                                        isactive = 1;
                                        sql = "update " + tables.area_codes + " set is_active = 0 where nzp_payer = " +
                                              finder.nzp_payer;
                                        ret = ExecSQL(conn_db, sql, true);
                                        changedActiveNumMsg = "Действующий код изменен на " + finder.active_num;
                                    }

                                }
                                sql = "insert into " + tables.area_codes +
                                      " (code, nzp_payer, changed_by, changed_on, is_active, nzp_pkod_type) values (" +
                                      i + ", " + finder.nzp_payer + ", " + nzpUser +
#if PG
 ", now(), " +
#else
                                ", current, "+
#endif
 isactive + ", " + finder.nzp_pkod_type + ")";
                                ret = ExecSQL(conn_db, sql, true);
                                if (ret.result)
                                {
                                    if (addedcodes == "") addedcodes += i.ToString();
                                    else addedcodes += ", " + i;
                                    string seq = Points.Pref + "_data.kvar_pkod10_" + i + "_seq";
#if PG

                                    sql = "DROP SEQUENCE " + seq;
                                    ExecSQL(conn_db, sql, false);
                                    sql = "CREATE SEQUENCE " + seq +
                                          " INCREMENT 1 MINVALUE 1 MAXVALUE 9223372036854775807 " +
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
                            // Если добавляемый уникальный код НЕ существует в БД
                            else
                            {
                                if (errcodes == "") errcodes += i.ToString();// сформировать сообщение об ошибке
                                else errcodes += ", " + i;
                                // Если заданный действующий код в интервале заданных уникальных кодов
                                if (activeNumInRangeUniqueCodes)
                                {
                                    if (finder.active_num == i)
                                    {
                                        // Проверить: 1.существует ли он 2.принадлежит ли выбранному контрагенту 3.какому контрагенту принадлежит(при невыполнении п.2)
                                        checkExistsUniqueCode(finder, out dopMsg, i, conn_db, nzpUser, out changedActiveNumMsg);
                                    }
                                }
                            }
                        }
                        // Если  заданный дейтсвующий код вне интервала заданных уникальных кодов
                        if (!activeNumInRangeUniqueCodes)
                        {
                            // Проверить: 1.существует ли он 2.принадлежит ли выбранному контрагенту 3.какому контрагенту принадлежит(при невыполнении п.2)
                            if (finder.active_num > 0)
                            {
                                checkExistsUniqueCode(finder, out dopMsg, finder.active_num, conn_db, nzpUser, out changedActiveNumMsg);
                            }
                        }
                        // Вычисление ближайшего свободного уникального кода
                        Returns ret2 = GetMaxCodeFromAreaCodes(conn_db);
                        if (!ret2.result)
                        {
                            conn_db.Close();
                            return ret;
                        }

                        if (ret.result)
                        {
                            if (errcodes == "") ret.text = "Создание прошло успешно. " + changedActiveNumMsg;
                            else
                            {
                                if (addedcodes != "") ret.text = "Коды (" + addedcodes + ") были успешно добавлены." + changedActiveNumMsg;
                                if (errcodes != "")
                                    ret.text = " Коды (" + errcodes + ") не были добавлены, так как уже существуют в БД, ближайший свободный " + ret2.tag + ". " + changedActiveNumMsg + " " + dopMsg;
                            }
                        }
                        else
                        {
                            if (addedcodes != "") ret.text = "Коды (" + addedcodes + ") были успешно добавлены. " + changedActiveNumMsg;
                            if (errcodes != "")
                                ret.text = " Коды (" + errcodes + ") не были добавлены, так как уже существуют в БД, ближайший свободный" + ret2.tag + ". " + changedActiveNumMsg + " " + dopMsg;
                        }
                        ret.tag = -1;
                    }
                    conn_db.Close();
                    return ret;
                }
            }
        }

        private Returns checkExistsUniqueCode(AreaCodes finder, out string dopMsg, int uniqueCode, IDbConnection conn_db, int nzpUser, out string changedActiveNumMsg)
        {
            dopMsg = "";
            changedActiveNumMsg = "";
            #region подключение к базе
            Returns ret = new Returns();
            #endregion


            DbTables tables = new DbTables(conn_db);
            int nzp_payer_reader = 0;
            // Проверяем, принадлежит ли он заданному контрагенту (finder.nzp_payer)
            string sql = " select p.nzp_payer, p.payer from " + tables.area_codes + " ac left outer join " +
                         tables.payer + " p  on (ac.nzp_payer = p.nzp_payer)" +
                         " where code = " + uniqueCode + " and ac.nzp_payer=" + finder.nzp_payer + ";" +
                // выяснить, к какому контрагенту принадлежит
                         " select ac.code,p.nzp_payer, p.payer from " + tables.area_codes + "  ac  left outer join " +
                          tables.payer + " p on (ac.nzp_payer = p.nzp_payer) " +
                         "where ac.nzp_payer not in (" + finder.nzp_payer + ")  and code =" + uniqueCode;

            IDataReader reader;
            if (!ExecRead(conn_db, out reader, sql, true).result)
            {
                conn_db.Close();
                return ret;
            }
            bool goToNextRead = true; // true- перейти к следующему resultset
            try
            {
                while (reader.Read())
                {
                    if (reader["nzp_payer"] != DBNull.Value)
                    {
                        if (finder.nzp_payer == Convert.ToInt32(reader["nzp_payer"]))
                        {
                            nzp_payer_reader = finder.nzp_payer;
                            goToNextRead = false;
                            break;
                        }
                    }
                }
                if (goToNextRead)
                {
                    reader.NextResult();
                    while (reader.Read())
                    {
                        if (!String.IsNullOrEmpty(reader["payer"].ToString()))
                        {
                            if (dopMsg == "")
                            {
                                dopMsg = " (Заданный код " + uniqueCode +
                                         " не может быть действующим для указанного контрагента, так как принадлежит контрагенту " +
                                         reader["payer"].ToString() + ")";
                                break;
                            }
                        }
                    }
                    if (dopMsg == "")
                    {
                        dopMsg = " (Заданный код " + uniqueCode +
                                 " не может быть действующим для указанного контрагента, так как этот код не существует в БД)";
                    }
                }

                reader.Close();
                conn_db.Close();
            }
            catch (Exception ex)
            {
                reader.Close();
                conn_db.Close();
                ret = new Returns(false, ex.Message);
                MonitorLog.WriteLog("Ошибка поиска контрагентов по заданному уникальному коду " + (Constants.Viewerror ? " \n " + ex.Message : ""), MonitorLog.typelog.Error, 20, 201, true);
                return ret;
            }

            // Если принадлежит к заданному контрагенту, действующим будет заданный код
            if (nzp_payer_reader != 0)
            {
                int isactive = 1;
                sql = "update " + tables.area_codes +
                      " set is_active = 0 where nzp_payer = " + finder.nzp_payer;
                ret = ExecSQL(conn_db, sql, true);
                sql = "update " + tables.area_codes + " set is_active = " + isactive +
                      ", changed_by = " + nzpUser +
#if PG
 ", changed_on = now() " +
#else
                                 ", changed_on = current "+
#endif
 " where code = " + uniqueCode;

                ret = ExecSQL(conn_db, sql, true);
                conn_db.Close();
                changedActiveNumMsg = "Действующий код изменен на " + finder.active_num;
            }
            return ret;
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
            /*  ret = CreateNewSeq(conn_db, Points.Pref + "_data" + tableDelimiter + "kvar_num_ls_seq", "num_ls", tables.kvar);
              if (!ret.result)
              {
                  conn_db.Close();
                  return ret;
              }*/

            //nzp_counter            
            string seq = Points.Pref + "_data" + tableDelimiter + "counters_spis_nzp_counter_seq";
            ret = CreateNewSeq(conn_db, Series.Types.Counter.GetHashCode(), seq, "nzp_counter", "");
            if (!ret.result)
            {
                conn_db.Close();
                return ret;
            }

            //nzp_kvar          
            ret = CreateNewSeq(conn_db, Series.Types.Kvar.GetHashCode(), Points.Pref + "_data" + tableDelimiter + "kvar_nzp_kvar_seq", "nzp_kvar", tables.kvar);
            if (!ret.result)
            {
                conn_db.Close();
                return ret;
            }

            //nzp_dom            
            ret = CreateNewSeq(conn_db, Series.Types.Dom.GetHashCode(), Points.Pref + "_data" + tableDelimiter + "dom_nzp_dom_seq", "nzp_dom", tables.dom);
            if (!ret.result)
            {
                conn_db.Close();
                return ret;
            }

            //nzp_ul           
            ret = CreateNewSeq(conn_db, Series.Types.Ulica.GetHashCode(), Points.Pref + "_data" + tableDelimiter + "s_ulica_nzp_ul_seq", "nzp_ul", tables.ulica);
            if (!ret.result)
            {
                conn_db.Close();
                return ret;
            }

            //nzp_geu           
            ret = CreateNewSeq(conn_db, Series.Types.Geu.GetHashCode(), Points.Pref + "_data" + tableDelimiter + "s_geu_nzp_geu_seq", "nzp_geu", tables.geu);
            if (!ret.result)
            {
                conn_db.Close();
                return ret;
            }

            //nzp_area           
            ret = CreateNewSeq(conn_db, Series.Types.Area.GetHashCode(), Points.Pref + "_data" + tableDelimiter + "s_area_nzp_area_seq", "nzp_area", tables.area);
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
            ret = CreateNewSeq(conn_db, Series.Types.Payer.GetHashCode(), Points.Pref + "_kernel" + tableDelimiter + "s_payer_nzp_payer_seq", "nzp_payer", tables.payer);
            if (!ret.result)
            {
                conn_db.Close();
                return ret;
            }

            //nzp_supp        
            ret = CreateNewSeq(conn_db, Series.Types.Supplier.GetHashCode(), Points.Pref + "_kernel" + tableDelimiter + "supplier_nzp_supp_seq", "nzp_supp", tables.supplier);
            if (!ret.result)
            {
                conn_db.Close();
                return ret;
            }


            //pkod10
            #region Pkod10
            IDbTransaction transaction = null;
            try
            {
#if PG
#else
                sql = "database " + Points.Pref + "_data";
                ret = ExecSQL(conn_db, sql, false);
#endif

                //У Самары sequence создается и настраивается
                //по всевозможным комбинациям имен KodErc + KodGeu (пример: 46803. Где 468 - KodErc, а 03 - остаток от деления nzp_geu на 100)
                if (Points.IsSmr)
                {
                    //Глобальный Код ЕРС
                    var globalKodErc = DBManager.ExecScalar(conn_db,
                        " Select kod as kod_erc From " +
                        DBManager.GetFullBaseName(conn_db, Points.Pref + "_kernel", "s_erck"),
                        out ret, true) ?? 0;

                    //Код ЕРС  для всех УК                    
                    StringBuilder sqlBuilder = new StringBuilder();
                    string prm_7 = DBManager.GetFullBaseName(conn_db, Points.Pref + "_data", "prm_7");
                    sqlBuilder.AppendFormat(" Select distinct nzp, val_prm From {0}", prm_7);
                    sqlBuilder.Append(" Where nzp_prm = 995 ");
                    sqlBuilder.AppendFormat("   and dat_s  <= MDY({0},1,{1})", Points.CalcMonth.month_, Points.CalcMonth.year_);
                    sqlBuilder.AppendFormat("   and dat_po >= MDY({0},1,{1})", Points.CalcMonth.month_, Points.CalcMonth.year_);
                    sqlBuilder.Append("   and is_actual <> 100 ");
                    sqlBuilder.AppendFormat("   and nzp in (select nzp_area from {0}) ", DBManager.GetFullBaseName(conn_db, Points.Pref + "_data", "s_area"));
                    DataTable areaCodeRTable = DBManager.ExecSQLToTable(conn_db,
                        sqlBuilder.ToString());

                    ////словарь 
                    //Dictionary<int, int> areaKodErcDictionary =
                    //    areaCodeRTable.AsEnumerable().ToDictionary(r => r.Field<int>(0), r2 => Convert.ToInt32(r2.Field<string>(1)));

                    //словарь префикс Плат Кода - nzp_area
                    Dictionary<int, List<int>> areaKodErcDictionary =
                        areaCodeRTable.AsEnumerable()
                            .GroupBy(r => Convert.ToInt32(r.Field<string>("val_prm")))
                            .ToDictionary(k => k.Key, g => g.Select(r => r.Field<int>("nzp")).ToList());

                    //исключаем из списка глобальный префикс (случай назначения глобального перфикса на УК)
                    areaKodErcDictionary.Remove(Convert.ToInt32(globalKodErc));

                    //добавляем глобальный
                    areaKodErcDictionary.Add(Convert.ToInt32(globalKodErc), new List<int>() { 0 });



                    //Все ЖЭУ 
                    List<int> geuList =
                        DBManager.ExecSQLToTable(conn_db,
                            "Select nzp_geu from " +
                            DBManager.GetFullBaseName(conn_db, Points.Pref + "_data", "s_geu"))
                            .AsEnumerable()
                            .Select(r => r.Field<int>("nzp_geu"))
                            .Where(g => g != 0)
                            .ToList();

                    //префиксы ЖЭУ
                    Dictionary<string, List<int>> geuCodeDictionary = new Dictionary<string, List<int>>();
                    foreach (int g in geuList)
                    {
                        string gCode = (g % 100).ToString("00");
                        if (!geuCodeDictionary.ContainsKey(gCode))
                        {
                            geuCodeDictionary.Add(gCode, new List<int>() { g }); ;
                        }
                        else
                        {
                            geuCodeDictionary[gCode].Add(g);
                        }
                    }



                    //создать seq по всем комбинациям Код ЕРС + ЖЭУ
                    transaction = conn_db.BeginTransaction();
                    foreach (KeyValuePair<int, List<int>> areaErc in areaKodErcDictionary)
                    {
                        foreach (KeyValuePair<string, List<int>> gCodeGeu in geuCodeDictionary)
                        {
                            //todo убарать null значения из словаря
                            string area_code = areaErc.Key.ToString("000") + gCodeGeu.Key;
                            string seqName = Points.Pref + "_data" + tableDelimiter + "kvar_pkod10_" + area_code + "_seq";
                            string kvar = Points.Pref + DBManager.sDataAliasRest + "kvar";

                            string whereCond = areaErc.Value.FirstOrDefault() != 0
                                ? String.Format("where nzp_area in ({0}) and nzp_geu in ({1})",
                                    String.Join(",", areaErc.Value.Select(x => x.ToString()).ToArray()),
                                    String.Join(",", gCodeGeu.Value.Select(g => g.ToString()).ToArray()))
                                : String.Format("where nzp_area not in ({0}) and nzp_geu in ({1})",
                                    String.Join(",",
                                        areaKodErcDictionary.Values.SelectMany(k => k)
                                            .Select(k => k.ToString())
                                            .ToArray()),
                                    String.Join(",", gCodeGeu.Value.Select(g => g.ToString()).ToArray()));

                            ret = CreateNewSeq(conn_db, transaction, Series.Types.PKod10.GetHashCode(), seqName, "pkod10", kvar, whereCond);
                            if (!ret.result)
                            {
                                throw new Exception(ret.text);
                            }
                        }
                    }
                    transaction.Commit();
                }
                else
                {
                    bool pkMode = GlobalSettings.NewGeneratePkodMode;
                    MyDataReader reader;
                    sql = "select * from " + tables.area_codes + " where is_active = 1";
                    ret = ExecRead(conn_db, out reader, sql, true);
                    if (!ret.result)
                    {
                        conn_db.Close();
                        return ret;
                    }

                    while (reader.Read())
                    {
                        AreaCodes zap = new AreaCodes();
                        if (reader["code"] != DBNull.Value) zap.code = Convert.ToInt32(reader["code"]);
                        seq = Points.Pref + "_data" + tableDelimiter + "kvar_pkod10_" + zap.code + "_seq";
                        if (pkMode)
                            ret = CreateNewSeq(conn_db, Series.Types.PKod10.GetHashCode(), seq, "pkod10", tables.kvar_pkodes + " where area_code = " + zap.code);
                        else
                            ret = CreateNewSeq(conn_db, Series.Types.PKod10.GetHashCode(), seq, "pkod10", tables.kvar + " where area_code = " + zap.code);
                        if (!ret.result)
                        {
                            conn_db.Close();
                            return ret;
                        }
                    }

                    reader.Close();
                }
            }
            catch (Exception ex)
            {
                if (transaction != null) transaction.Rollback();

                conn_db.Close();
                ret = new Returns(false, ex.Message);
                MonitorLog.WriteLog(
                    "Ошибка установки кодов для pkod10 " + (Constants.Viewerror ? " \n " + ex.Message : ""),
                    MonitorLog.typelog.Error, 20, 201, true);
                return ret;
            }

            #endregion
            conn_db.Close();
            return ret;
        }

        public Returns CreateNewSeq(IDbConnection conn_db, IDbTransaction transaction, int kod, string seq, string field, string table, string whereCondition = "")
        {
            Returns ret;
            int cnt = 0;
            string sql = string.Empty;

            if (kod == Series.Types.Kvar.GetHashCode())
            {
                sql = String.Format("select max(nzp_kvar) mnzp_kvar, max(num_ls) mnum_ls from {0}_data{1}kvar", Points.Pref, tableDelimiter);
                MyDataReader reader = null;
                if (!ExecRead(conn_db, out reader, sql, true).result)
                {
                    ret = new Returns(false, "Ошибка при определении максимального кода между nzp_kvar и num_ls ");
                    MonitorLog.WriteLog("Ошибка CreateNewSeq ", MonitorLog.typelog.Error, 20, 201, true);
                    return ret;
                }
                int mnzp_kvar = 0, mnum_ls = 0;
                try
                {
                    if (reader.Read())
                    {
                        if (reader["mnzp_kvar"] != DBNull.Value) mnzp_kvar = Convert.ToInt32(reader["mnzp_kvar"]);
                        if (reader["mnum_ls"] != DBNull.Value) mnum_ls = Convert.ToInt32(reader["mnum_ls"]);
                    }
                }
                catch (Exception ex)
                {
                    reader.Close();
                    ret = new Returns(false, ex.Message);
                    MonitorLog.WriteLog("Ошибка получения максимальных кодов ЛС " + (Constants.Viewerror ? " \n " + ex.Message : ""), MonitorLog.typelog.Error, 20, 201, true);
                    return ret;
                }
                cnt = 0;
                if (mnum_ls >= mnzp_kvar) cnt = mnum_ls;
                else cnt = mnzp_kvar;
            }
            else if (kod == Series.Types.Counter.GetHashCode())
            {
                sql = "";
                foreach (_Point zap in Points.PointList)
                {
                    if (sql == "") sql = "select max(nzp_counter) as nzp from " + zap.pref + "_data" + tableDelimiter + "counters_spis ";
                    else sql += " union select max(nzp_counter) as nzp from " + zap.pref + "_data" + tableDelimiter + "counters_spis";
                }
                MyDataReader reader;
                if (!ExecRead(conn_db, out reader, sql, true).result)
                {
                    ret = new Returns(false, "Ошибка при определении максимального кода для счетчика ");
                    MonitorLog.WriteLog("Ошибка CreateNewSeq ", MonitorLog.typelog.Error, 20, 201, true);
                    return ret;
                }
                List<int> nzp = new List<int>();
                try
                {
                    while (reader.Read()) if (reader["nzp"] != DBNull.Value) nzp.Add(Convert.ToInt32(reader["nzp"]));
                }
                catch (Exception ex)
                {
                    reader.Close();
                    conn_db.Close();
                    ret = new Returns(false, ex.Message);
                    MonitorLog.WriteLog("Ошибка заполнения списка максимальных кодов счетчиков по всем БД " + (Constants.Viewerror ? " \n " + ex.Message : ""), MonitorLog.typelog.Error, 20, 201, true);
                    return ret;
                }
                cnt = 0;
                if (nzp.Count > 0) cnt = nzp.Max() + 1;
            }
            else
            {
                sql = String.Format("SELECT max({0}) FROM {1} {2} ", field, table, whereCondition);
                object count = ExecScalar(conn_db, transaction, sql, out ret, true);
                try { if (count != DBNull.Value) cnt = Convert.ToInt32(count); }
                catch (Exception e)
                {
                    ret = new Returns(false, "Ошибка при определении кода: " + table + (Constants.Debug ? e.Message : ""));
                    MonitorLog.WriteLog("Ошибка CreateNewSeq " + (Constants.Viewerror ? "\n" + e.Message : ""), MonitorLog.typelog.Error, 20, 201, true);
                    return ret;
                }

            }

#if PG
            if (cnt == 0) cnt++;
            sql = " SELECT setval('" + seq + "', " + cnt + ")";
#else                 
           
            /*if (cnt == 0)*/ cnt++;
            sql = " alter sequence " + seq + " RESTART "+cnt;
#endif
            ret = ExecSQL(conn_db, transaction, sql, false);
            if (!ret.result)
            {
#if PG
                sql = "CREATE SEQUENCE " + seq + " INCREMENT 1 MINVALUE 1 MAXVALUE 9223372036854775807 START 1 CACHE 1";
                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result) return ret;
                sql = " ALTER TABLE " + seq + " OWNER TO postgres";
                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result) return ret;
                sql = " SELECT setval('" + seq + "', " + cnt + ")";
                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result) return ret;
#else
                sql = "CREATE SEQUENCE " + seq;
                ret = ExecSQL(conn_db, transaction, sql, true);
                if (!ret.result) return ret;

                sql = " alter sequence " + seq + " RESTART " + cnt;
                ret = ExecSQL(conn_db, transaction, sql, true);
                if (!ret.result) return ret;
#endif
            }
            ret.tag = cnt;
            return ret;
        }

        public Returns CreateNewSeq(IDbConnection conn_db, int kod, string seq, string field, string table)
        {
            return CreateNewSeq(conn_db, null, kod, seq, field, table);
        }

        public Returns CreateNewSeq(string database, int kod, string seq, string field, string table)
        {
            #region подключение к базе
            string conn_kernel = Points.GetConnByPref(Points.Pref);
            IDbConnection conn_db = GetConnection(conn_kernel);
            Returns ret = OpenDb(conn_db, true);
            if (!ret.result) return ret;
            #endregion

#if PG
#else
            string sql = "database " + database;
            ret = ExecSQL(conn_db, sql, false);
#endif

            ret = CreateNewSeq(conn_db, null, kod, seq, field, table);
            conn_db.Close();
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
            if (!TableInBase(conn_db, null, pgDefaultSchema, "excel_utility"))
            {

                ret = ExecSQL(conn_db,
                                   " create table " + sDefaultSchema + "excel_utility " +
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

                if (!ret.result) return ret;



                ExecSQL(conn_db, " create unique index " + sDefaultSchema + "ix_exc_1 on " + sDefaultSchema + "excel_utility (nzp_exc); ", true);
                ExecSQL(conn_db, " create        index " + sDefaultSchema + "ix_exc_2 on " + sDefaultSchema + "excel_utility (nzp_user, dat_in); ", true);
                ExecSQL(conn_db, " analyze excel_utility ", true);

            }
            else
            {
                ret = AddFieldToTable(conn_db, "excel_utility", "progress", "integer default 0");
                if (!ret.result) return ret;
            }
            #endregion

            StringBuilder sql = new StringBuilder();
            sql.Append("insert into " + sDefaultSchema + "excel_utility (nzp_user, stats, prms, dat_in, rep_name, exc_comment, dat_today, exc_path, file_name) ");
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
                ExecSQL(conn_db, " update " + sDefaultSchema + "excel_utility set dat_start = dat_in where nzp_exc = " + id, true);
            }

            conn_db.Close();
            sql.Remove(0, sql.Length);

            ret.tag = id;

            return ret;
        }


        public List<KeyValue> LoadAreaAvailableForRole(Role finder, out Returns ret)
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

            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return null;

            DbTables tables = new DbTables(conn_db);

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

            string roleskey = "";
#if PG
            roleskey = "public.roleskey";

#else
            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result) return null;
            roleskey = conn_web.Database + "@" + DBManager.getServer(conn_web) + ":roleskey";
            conn_web.Close();
#endif

#if PG
            string where =
                " Where a.nzp_area not in ( Select kod From " + roleskey + " r Where r.nzp_role = " + finder.nzp_role +
                "   and r.tip = " + Constants.role_sql_area +
                "   and r.tip::character(90)||r.kod||r.nzp_role||'-'||r.nzp_rlsv||'roles' = r.sign) " + sw;
#else
            string where =
                " Where a.nzp_area not in ( Select kod From "+roleskey+" r Where r.nzp_role = " + finder.nzp_role +
                "   and r.tip = " + Constants.role_sql_area +
                "   and r.tip||r.kod||r.nzp_role||'-'||r.nzp_rlsv||'roles' = decrypt_char(r.sign)) " + sw;
#endif

            IDataReader reader;
#if PG

            ret = ExecRead(conn_db, out reader,
                          " Select  nzp_area, area " +
                          " From " + tables.area + " a " +
                          where +
                          " Order by area ", true);
#else
            ret = ExecRead(conn_db, out reader,
                " Select " + " nzp_area, area " +
                " From " + tables.area + " a " +
                where +
                " Order by area ", true);
#endif
            if (!ret.result)
            {
                conn_db.Close();
                return null;
            }

            List<KeyValue> spis = new List<KeyValue>();
            try
            {
                int i = 0;
                while (reader.Read())
                {
                    i++;

                    KeyValue zap = new KeyValue();

                    if (reader["nzp_area"] != DBNull.Value) zap.key = (int)reader["nzp_area"];
                    if (reader["area"] != DBNull.Value) zap.value = Convert.ToString(reader["area"]).Trim();

                    spis.Add(zap);
                }

                reader.Close();
                conn_db.Close();
                ret.tag = i;
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

                MonitorLog.WriteLog("Ошибка заполнения справочника управляющих организаций " + err, MonitorLog.typelog.Error, 20, 201, true);

                return null;
            }
        }

        public List<KeyValue> LoadGeuAvailableForRole(Role finder, out Returns ret)
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

            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return null;

            DbTables tables = new DbTables(conn_db);

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

            string roleskey = "";
#if PG
            roleskey = "public.roleskey";

#else
            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result) return null;
            roleskey = conn_web.Database + "@" + DBManager.getServer(conn_web) + ":roleskey";
            conn_web.Close();
#endif

#if PG
            string where =
                            " Where g.nzp_geu not in ( Select kod From " + roleskey + " r Where r.nzp_role = " + finder.nzp_role +
                            "   and r.tip = " + Constants.role_sql_geu +
                            "   and r.tip::character(90)||r.kod||r.nzp_role||'-'||r.nzp_rlsv||'roles' = r.sign) " + sw;
#else
            string where =
                            " Where g.nzp_geu not in ( Select kod From "+roleskey+" r Where r.nzp_role = " + finder.nzp_role +
                            "   and r.tip = " + Constants.role_sql_geu +
                            "   and r.tip||r.kod||r.nzp_role||'-'||r.nzp_rlsv||'roles' = decrypt_char(r.sign)) " + sw;
#endif

            IDataReader reader;
#if PG
            ret = ExecRead(conn_db, out reader,
                          " Select  nzp_geu, geu " +
                          " From " + tables.geu + " g " +
                          where +
                          " Order by geu ", true);
#else
            ret = ExecRead(conn_db, out reader,
                          " Select nzp_geu, geu " +
                          " From " + tables.geu + " g " +
                          where +
                          " Order by geu ", true);
#endif
            if (!ret.result)
            {
                conn_db.Close();
                return null;
            }

            List<KeyValue> spis = new List<KeyValue>();
            try
            {
                int i = 0;
                while (reader.Read())
                {
                    i++;

                    KeyValue zap = new KeyValue();

                    if (reader["nzp_geu"] != DBNull.Value) zap.key = (int)reader["nzp_geu"];
                    if (reader["geu"] != DBNull.Value) zap.value = Convert.ToString(reader["geu"]).Trim();

                    spis.Add(zap);
                }

                reader.Close();
                conn_db.Close();
                ret.tag = i;
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

                MonitorLog.WriteLog("Ошибка заполнения справочника ЖЭУ " + err, MonitorLog.typelog.Error, 20, 201, true);

                return null;
            }
        }

        public List<KeyValue> LoadServiceAvailableForRole(Role finder, out Returns ret)
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

            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return null;

            DbTables tables = new DbTables(conn_db);

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

            string roleskey = "";
#if PG
            roleskey = "public.roleskey";

#else
            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result) return null;
            roleskey = conn_web.Database + "@" + DBManager.getServer(conn_web) + ":roleskey";
            conn_web.Close();
#endif

            string where =
                " Where s.nzp_serv not in ( Select kod From " + roleskey + " r Where r.nzp_role = " + finder.nzp_role +
                "   and r.tip = " + Constants.role_sql_serv +
#if PG
 "   and r.tip||CAST(r.kod as text)||r.nzp_role||'-'||r.nzp_rlsv||'roles' = r.sign) " + sw;
#else
                "   and r.tip||r.kod||r.nzp_role||'-'||r.nzp_rlsv||'roles' = decrypt_char(r.sign)) " + sw;
#endif

            IDataReader reader;
#if PG
            ret = ExecRead(conn_db, out reader,
                " Select nzp_serv, service " +
                " From " + tables.services + " s " +
                where +
                " Order by service ", true);
#else
            ret = ExecRead(conn_web, out reader,
                " Select " + " nzp_serv, service " +
                " From " + tables.services + " s " +
                where +
                " Order by service ", true);
#endif
            if (!ret.result)
            {
                conn_db.Close();
                return null;
            }

            List<KeyValue> spis = new List<KeyValue>();
            try
            {
                int i = 0;
                while (reader.Read())
                {
                    i++;

                    KeyValue zap = new KeyValue();

                    if (reader["nzp_serv"] != DBNull.Value) zap.key = Convert.ToInt32(reader["nzp_serv"]);
                    if (reader["service"] != DBNull.Value) zap.value = Convert.ToString(reader["service"]).Trim();

                    spis.Add(zap);
                }

                reader.Close();
                conn_db.Close();
                ret.tag = i;
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

                MonitorLog.WriteLog("Ошибка заполнения справочника услуг " + err, MonitorLog.typelog.Error, 20, 201, true);

                return null;
            }
        }

        public List<KeyValue> LoadSupplierAvailableForRole(Role finder, out Returns ret)
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

            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return null;

            DbTables tables = new DbTables(conn_db);

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

            string roleskey = "";
#if PG
            roleskey = "public.roleskey";

#else
            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result) return null;
            roleskey = conn_web.Database + "@" + DBManager.getServer(conn_web) + ":roleskey";
            conn_web.Close();
#endif

            string where =
                " Where s.nzp_supp > 0 and s.nzp_supp not in ( Select kod From " + roleskey + " r Where r.nzp_role = " + finder.nzp_role +
                "   and r.tip = " + Constants.role_sql_supp +
#if PG
 "   and r.tip||CAST(r.kod as text)||r.nzp_role||'-'||r.nzp_rlsv||'roles' = r.sign) " + sw;
#else
                "   and r.tip||r.kod||r.nzp_role||'-'||r.nzp_rlsv||'roles' = decrypt_char(r.sign)) " + sw;
#endif

            IDataReader reader;
            ret = ExecRead(conn_db, out reader,
#if PG
 " Select nzp_supp, name_supp " +
                " From " + tables.supplier + " s " + where +
                " Order by name_supp ", true);
#else
                " Select nzp_supp, name_supp " +
                " From " +  tables.supplier + " s " + where +
                " Order by name_supp ", true);
#endif
            if (!ret.result)
            {
                conn_db.Close();
                return null;
            }

            List<KeyValue> spis = new List<KeyValue>();
            try
            {
                int i = 0;
                while (reader.Read())
                {
                    i++;

                    KeyValue zap = new KeyValue();

                    if (reader["nzp_supp"] != DBNull.Value) zap.key = Convert.ToInt64(reader["nzp_supp"]);
                    if (reader["name_supp"] != DBNull.Value) zap.value = Convert.ToString(reader["name_supp"]).Trim();

                    spis.Add(zap);
                }

                reader.Close();
                conn_db.Close();
                ret.tag = i;
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

                MonitorLog.WriteLog("Ошибка заполнения справочника поставщиков " + err, MonitorLog.typelog.Error, 20, 201, true);

                return null;
            }
        }

        public List<KeyValue> LoadPrmAvailableForRole(Role finder, out Returns ret)
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

            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return null;

            DbTables tables = new DbTables(conn_db);

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
            string roleskey = "";
#if PG
            roleskey = "public.roleskey";

#else
            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result) return null;
            roleskey = conn_web.Database + "@" + DBManager.getServer(conn_web) + ":roleskey";
            conn_web.Close();
#endif

            string where = " Where p.nzp_prm not in (select kod from " + roleskey + " r where r.nzp_role = " + finder.nzp_role +
                " and r.tip = " + Constants.role_sql_prm +
#if PG
 " and r.tip||CAST(r.kod as text)||r.nzp_role||'-'||r.nzp_rlsv||'roles' = decrypt_char(r.sign)) " + sw;
#else
                " and r.tip||r.kod||r.nzp_role||'-'||r.nzp_rlsv||'roles' = decrypt_char(r.sign)) " + sw;
#endif

            IDataReader reader;
            ret = ExecRead(conn_db, out reader,
#if PG
 " Select nzp_prm, name_prm From " + tables.prm_name + " p " +
                where +
                " Order by name_prm ", true);
#else
                " Select nzp_prm, name_prm From "+tables.prm_name+" p " +
                where +
                " Order by name_prm ", true);
#endif
            if (!ret.result)
            {
                conn_db.Close();
                return null;
            }

            List<KeyValue> spis = new List<KeyValue>();
            try
            {
                int i = 0;
                while (reader.Read())
                {
                    i++;

                    KeyValue zap = new KeyValue();

                    if (reader["nzp_prm"] != DBNull.Value) zap.key = Convert.ToInt32(reader["nzp_prm"]);
                    if (reader["name_prm"] != DBNull.Value) zap.value = Convert.ToString(reader["name_prm"]).Trim();

                    spis.Add(zap);
                }

                reader.Close();
                conn_db.Close();
                ret.tag = i;
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

                MonitorLog.WriteLog("Ошибка заполнения справочника параметров " + err, MonitorLog.typelog.Error, 20, 201, true);

                return null;
            }
        }

        public List<KeyValue> LoadPointAvailableForRole(Role finder, out Returns ret)
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

            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return null;

            DbTables tables = new DbTables(conn_db);

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

            string roleskey = "";
#if PG
            roleskey = "public.roleskey";

#else
            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result) return null;
            roleskey = conn_web.Database + "@" + DBManager.getServer(conn_web) + ":roleskey";
            conn_web.Close();
#endif

            string where =
                " Where p.nzp_wp <> 1 and p.nzp_wp not in ( Select kod From " + roleskey + " r Where r.nzp_role = " + finder.nzp_role +
                "   and r.tip = " + Constants.role_sql_wp +
#if PG
 "   and r.tip||CAST(r.kod as text)||r.nzp_role||'-'||r.nzp_rlsv||'roles' = r.sign) " + sw;
#else
                "   and r.tip||r.kod||r.nzp_role||'-'||r.nzp_rlsv||'roles' = decrypt_char(r.sign)) " + sw;
#endif

            IDataReader reader;
            ret = ExecRead(conn_db, out reader,
#if PG
 " Select nzp_wp, point " +
                " From " + tables.point + " p " +
                where +
                " Order by point ", true);
#else
                " Select nzp_wp, point " +
                " From " + tables.point + " p " +
                where +
                " Order by point ", true);
#endif
            if (!ret.result)
            {
                conn_db.Close();
                return null;
            }

            List<KeyValue> spis = new List<KeyValue>();
            try
            {
                int i = 0;
                while (reader.Read())
                {
                    i++;

                    KeyValue zap = new KeyValue();

                    if (reader["nzp_wp"] != DBNull.Value) zap.key = (int)reader["nzp_wp"];
                    if (reader["point"] != DBNull.Value) zap.value = Convert.ToString(reader["point"]).Trim();

                    spis.Add(zap);
                }

                reader.Close();
                conn_db.Close();
                ret.tag = i;
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

                MonitorLog.WriteLog("Ошибка заполнения справочника банков данных " + err, MonitorLog.typelog.Error, 20, 201, true);

                return null;
            }
        }

    }
}
