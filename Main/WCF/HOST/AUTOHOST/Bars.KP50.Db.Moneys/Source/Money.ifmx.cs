using System;
using System.Data;
using System.Globalization;
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
    public class DbMoneys : DataBaseHead
    //----------------------------------------------------------------------
    {
#if PG
        private readonly string pgDefaultDb = "public";
#else
#endif
        //----------------------------------------------------------------------
        public List<Money> GetMoney(Money finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            if (finder.nzp_user < 1)
            {
                ret = new Returns(false, "Не определен пользователь");
                return null;
            }

            if (finder.RolesVal != null)
                foreach (_RolesVal role in finder.RolesVal)
                    if (role.tip == Constants.role_sql && role.kod == Constants.role_sql_serv)
                    {
                        ret = new Returns(false, "Имеются ограничения пользователя по списку услуг. Режим просмотра оплат не доступен.", -1);
                        return null;
                    }

            ret = Utils.InitReturns();

            List<Money> spMoney = new List<Money>();

            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result) return null;

            string tXX_spls = "t" + Convert.ToString(finder.nzp_user) + "_spmoney";
#if PG
            string tXX_spls_full = pgDefaultDb + "." + tXX_spls;
#else
            string tXX_spls_full = conn_web.Database + "@" + DBManager.getServer(conn_web) + ":" + tXX_spls;
#endif
            string sWhere = " where nzp_kvar=" + finder.nzp_kvar.ToString();

            IDbCommand cmd = DBManager.newDbCommand(" Select count(*) From " + tXX_spls_full + sWhere, conn_web);
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

                MonitorLog.WriteLog("Ошибка заполнения списка оплат " + err, MonitorLog.typelog.Error, 20, 201, true);
                return null;
            }

#if PG
            string skip = "";
            if (finder.skip > 0) skip = " offset " + finder.skip.ToString();
            string orderby = " Order by num_ls,dat_months desc,dat_uchets desc ";
            StringBuilder sql = new StringBuilder();
            sql.Append(" Select" + skip);
            sql.Append(" nzp_kvar,num_ls,nzp_pack_ls,name_bank,sum_ls,dat_month,dat_uchet,dat_vvod,dat_months,dat_uchets, num_pack, dat_pack");
            sql.Append(" From " + tXX_spls_full + sWhere);
            sql.Append(orderby);
#else
            string skip = "";
            if (finder.skip > 0) skip = " skip " + finder.skip.ToString();
            string orderby = " Order by num_ls,dat_months desc,dat_uchets desc ";
            StringBuilder sql = new StringBuilder();
            sql.Append(" Select" + skip);
            sql.Append(" nzp_kvar,num_ls,nzp_pack_ls,name_bank,sum_ls,dat_month,dat_uchet,dat_vvod,dat_months,dat_uchets, num_pack, dat_pack");
            sql.Append(" From " + tXX_spls_full + sWhere);
            sql.Append(orderby);
#endif

            //выбрать список
            IDataReader reader;
            if (!ExecRead(conn_web, out reader, sql.ToString(), true).result)
            {
                conn_web.Close();
                return null;
            }
            try
            {
                int i = 0;
                while (reader.Read())
                {
                    Money zap = new Money();

                    i++; zap.num = (i + finder.skip).ToString();

                    if (reader["num_ls"] == DBNull.Value)
                        zap.num_ls = 0;
                    else
                        zap.num_ls = (int)(reader["num_ls"]);

                    if (reader["nzp_pack_ls"] == DBNull.Value) zap.nzp_pack_ls = 0;
                    else zap.nzp_pack_ls = Convert.ToInt32(reader["nzp_pack_ls"]);

                    if (reader["name_bank"] == DBNull.Value) zap.name_bank = "";
                    else zap.name_bank = Convert.ToString(reader["name_bank"]);

                    if (reader["sum_ls"] == DBNull.Value) zap.sum_ls = 0;
                    else zap.sum_ls = Convert.ToDecimal(reader["sum_ls"]);

                    if (reader["dat_month"] == DBNull.Value) zap.dat_month = "";
                    else zap.dat_month = Convert.ToDateTime(reader["dat_month"]).ToString("yyyy-MM");

                    if (reader["dat_uchet"] == DBNull.Value) zap.dat_uchet = "";
                    else zap.dat_uchet = Convert.ToString(reader["dat_uchet"]);

                    if (reader["dat_vvod"] == DBNull.Value) zap.dat_vvod = "";
                    else zap.dat_vvod = Convert.ToString(reader["dat_vvod"]);

                    if (reader["num_pack"] != DBNull.Value) zap.pack = "№ " + Convert.ToString(reader["num_pack"]).Trim();
                    if (reader["dat_pack"] != DBNull.Value) zap.pack += " от " + Convert.ToDateTime(reader["dat_pack"]).ToShortDateString();

                    spMoney.Add(zap);
                    if (i >= finder.rows) break;

                }

                reader.Close();
                conn_web.Close();
                return spMoney;
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

                MonitorLog.WriteLog("Ошибка поиска оплат GetMoney " + err, MonitorLog.typelog.Error, 20, 201, true);

                return null;
            }
            //return null;
        }
        //----------------------------------------------------------------------
        public void FindMoney(Money finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            if (finder.nzp_user < 1)
            {
                ret.result = false;
                ret.text = "Не определен пользователь";
                return;
            }

            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result) return;



#if PG
            ExecSQL(conn_web, "set search_path to 'public'", false);
            string tXX_spls = "t" + Convert.ToString(finder.nzp_user) + "_spmoney";
            ExecSQL(conn_web, " Drop table " + tXX_spls, false);
            //создать таблицу webdata.tXX_spls
            ret = ExecSQL(conn_web,
                      " Create table " + tXX_spls +
                      " ( year_  integer," +
                      "   nzp_kvar integer," +
                      "   num_ls  integer," +
                      "   nzp_pack  integer," +
                      "   nzp_pack_ls  integer," +
                      "   nzp_bank  integer," +
                      "   name_bank  char(40)," +
                      "   sum_ls  decimal(10,2)," +
                      "   dat_month  char(10)," +
                      "   dat_uchet  char(10)," +
                      "   dat_months  date," +
                      "   dat_uchets  date," +
                      "   dat_vvod  char(10)," +
                      "   num_pack CHAR(10)," +
                      "   dat_pack DATE" +
                      " ) with oids", true);
#else
            string tXX_spls = "t" + Convert.ToString(finder.nzp_user) + "_spmoney";
            ExecSQL(conn_web, " Drop table " + tXX_spls, false);
            //создать таблицу webdata:tXX_spls
            ret = ExecSQL(conn_web,
                      " Create table " + tXX_spls +
                      " ( year_  integer," +
                      "   nzp_kvar integer," +
                      "   num_ls  integer," +
                      "   nzp_pack  integer," +
                      "   nzp_pack_ls  integer," +
                      "   nzp_bank  integer," +
                      "   name_bank  char(40)," +
                      "   sum_ls  decimal(10,2)," +
                      "   dat_month  char(10)," +
                      "   dat_uchet  char(10)," +
                      "   dat_months  date," +
                      "   dat_uchets  date," +
                      "   dat_vvod  char(10)," +
                      "   num_pack CHAR(10)," +
                      "   dat_pack DATE" +
                      " ) ", true);
#endif
            if (!ret.result) { conn_web.Close(); return; }

            if (finder.RolesVal != null)
                foreach (_RolesVal role in finder.RolesVal)
                    if (role.tip == Constants.role_sql && role.kod == Constants.role_sql_serv)
                    {
                        ret = new Returns(false, "Имеются ограничения пользователя по списку услуг. Режим просмотра оплат не доступен.", -1);
                        conn_web.Close();
                        return;
                    }

            // если не определен л/с, выход
            if (finder.nzp_kvar.ToString() == "") { conn_web.Close(); return; }
            if (finder.nzp_kvar <= 0) { conn_web.Close(); return; }
            if (finder.year_ <= 1900) { conn_web.Close(); return; }

            //заполнить webdata:tXX_spls
            string connectionString = Points.GetConnByPref(finder.pref);
            IDbConnection conn_db = GetConnection(connectionString);
            ret = OpenDb(conn_db, true);
            if (!ret.result) { conn_web.Close(); return; }

#if PG
            string tXX_spls_full = pgDefaultDb + "." + tXX_spls;
#else
            string tXX_spls_full = conn_web.Database + "@" + DBManager.getServer(conn_web) + ":" + tXX_spls;
#endif

            string sKodSum = "";
            //if (Points.Pref == "nch") { sKodSum = " and l.kod_sum=53"; }

            StringBuilder sql = new StringBuilder();

            string curYear2 = finder.year_.ToString().Substring(2, 2);

#if PG
            var schemaExists = this.SchemaExists(Points.Pref + "_fin_" + curYear2, conn_db);          
#else
            ret = ExecSQL(conn_db, "database " + Points.Pref + "_fin_" + curYear2, false);
#endif
#if PG
            if (schemaExists)
#else
            if (ret.result)
#endif
            {
#if PG
                sql.Append(
                    " Insert into " + tXX_spls_full +
                    " (year_,nzp_kvar,num_ls,nzp_pack,nzp_pack_ls,nzp_bank,name_bank,sum_ls,dat_month,dat_uchet,dat_vvod,dat_months,dat_uchets,num_pack,dat_pack)" +
                    " Select " +
                    finder.year_ + ",k.nzp_kvar,l.num_ls,l.nzp_pack,l.nzp_pack_ls,p.nzp_bank,b.bank name_bank,l.g_sum_ls,l.dat_month,l.dat_uchet,l.dat_vvod::DATE,l.dat_month,l.dat_uchet, p.num_pack, p.dat_pack" +
                    " From " +
                    finder.pref + "_data.kvar k," +
                    Points.Pref + "_fin_" + curYear2 + ".pack_ls l," +
                    Points.Pref + "_fin_" + curYear2 + ".pack p," +
                    Points.Pref + "_kernel.s_bank b" +
                    " where k.num_ls=l.num_ls and l.nzp_pack=p.nzp_pack and p.nzp_bank=b.nzp_bank" +
                    " and k.nzp_kvar=" + finder.nzp_kvar.ToString() + sKodSum +
                    " ");
#else
                sql.Append(
                    " Insert into " + tXX_spls_full +
                    " (year_,nzp_kvar,num_ls,nzp_pack,nzp_pack_ls,nzp_bank,name_bank,sum_ls,dat_month,dat_uchet,dat_vvod,dat_months,dat_uchets,num_pack,dat_pack)" +
                    " Select " +
                    finder.year_.ToString() + ",k.nzp_kvar,l.num_ls,l.nzp_pack,l.nzp_pack_ls,p.nzp_bank,b.bank name_bank,l.g_sum_ls,l.dat_month,l.dat_uchet,l.dat_vvod,l.dat_month,l.dat_uchet, p.num_pack, p.dat_pack" +
                    " From " +
                    finder.pref + "_data:kvar k," +
                    Points.Pref + "_fin_" + curYear2 + ":pack_ls l," +
                    Points.Pref + "_fin_" + curYear2 + ":pack p," +
                    Points.Pref + "_kernel:s_bank b" +
                    " where k.num_ls=l.num_ls and l.nzp_pack=p.nzp_pack and p.nzp_bank=b.nzp_bank" +
                    " and k.nzp_kvar=" + finder.nzp_kvar.ToString() + sKodSum +
                    " ");
#endif
                ret = ExecSQL(conn_db, sql.ToString(), true);
                if (!ret.result) { conn_db.Close(); conn_web.Close(); return; }

                conn_db.Close(); //закрыть соединение с основной базой

                //далее работаем с кешем
                //создаем индексы на tXX_spls
                string ix = "ix" + Convert.ToString(finder.nzp_user) + "_spmoney";

#if PG
                this.CreateIndexIfNotExists(conn_web, ix + "_1", "public."+ tXX_spls, "year_,num_ls,dat_uchet", true);
#else
                ret = ExecSQL(conn_web, " Create index " + ix + "_1 on " + tXX_spls + " (year_,num_ls,dat_uchet) ", true);
#endif
#if PG
                ret = ExecSQL(conn_web, " analyze  " + tXX_spls, true);
#else
                if (ret.result) { ret = ExecSQL(conn_web, " Update statistics for table  " + tXX_spls, true); }
#endif
                conn_web.Close();
            }
            else
            {
                conn_db.Close(); conn_web.Close();
                ret = new Returns(true);
            }
            return;
        }

        /// <summary>
        /// Загрузить список распределений оплаты по услугам
        /// </summary>
        /// <param name="finder"></param>
        /// <param name="ret"></param>
        /// <returns></returns>
        public List<Money> GetMoneyUchet(Money finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            return null;
        }

        /// <summary>
        /// Загрузить информацию о выбранном платеже
        /// </summary>
        /// <param name="finder"></param>
        /// <param name="ret"></param>
        /// <returns></returns>
        public Money LoadMoney(Money finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            return null;
        }
        // ...

        public List<AccountPayment> GetAccountPayment(AccountPayment finder, out Returns ret)
        {
            if (finder.nzp_user <= 0)
            {
                ret = new Returns(false, "Пользователь не определен");
                return null;
            }

#if PG
            string sql = "select dbname from " + Points.Pref + "_kernel.s_baselist where idtype = 8";
#else
            string sql = "select dbname from " + Points.Pref + "_kernel:s_baselist where idtype = 8";
#endif
            string connectionString = Points.GetConnByPref(finder.pref);
            IDbConnection conn_db = GetConnection(connectionString);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return null;

            IDataReader reader;
            ret = ExecRead(conn_db, out reader, sql, true);
            if (!ret.result)
            {
                conn_db.Close();
                return null;
            }

            List<AccountPayment> list = new List<AccountPayment>();
            int recordsTotalCount = 0;
            try
            {
                string dbname = "";
                if (reader.Read())
                {
                    if (reader["dbname"] != DBNull.Value) dbname = Convert.ToString(reader["dbname"]).Trim();
                }
                reader.Close();

#if PG
                string tablename = dbname + ".account_payment";
#else
                string tablename = dbname + "@" + DBManager.getServer(conn_db) + ":account_payment";
#endif
                if (!TempTableInWebCashe(conn_db, tablename))
                {
                    ret = new Returns(false, "Нет таблицы", -1);
                    return null;
                }

                //Определить общее количество записей
#if PG
                sql = "Select count(*) From " + tablename + " where pkod = (select pkod from " + finder.pref + "_data.kvar where nzp_kvar = " + finder.nzp_kvar + ") and " +
                      " is_del = 0";
#else
                sql = "Select count(*) From " + tablename + " where pkod = (select pkod from " + finder.pref + "_data:kvar where nzp_kvar = " + finder.nzp_kvar + ") and " +
                      " is_del = 0";
#endif
                object count = ExecScalar(conn_db, sql, out ret, true);

                try { recordsTotalCount = Convert.ToInt32(count); }
                catch (Exception e)
                {
                    ret = new Returns(false, "Ошибка при определении количества записей: " + (Constants.Debug ? e.Message : ""));
                    MonitorLog.WriteLog("Ошибка GetAccountPayment " + (Constants.Viewerror ? "\n" + e.Message : ""), MonitorLog.typelog.Error, 20, 201, true);
                    conn_db.Close();
                    return null;
                }

#if PG
                sql = "select nzp_payment,dat_month,payment_id,payment_date,sum_pay,web_client_name,dat_when " +
                                     " from " + tablename + " where pkod = (select pkod from " + finder.pref + "_data.kvar where nzp_kvar = " + finder.nzp_kvar + ") and " +
                                     " is_del = 0 order by payment_date";
#else
                sql = "select nzp_payment,dat_month,payment_id,payment_date,sum_pay,web_client_name,dat_when " +
                                     " from " + tablename + " where pkod = (select pkod from " + finder.pref + "_data:kvar where nzp_kvar = " + finder.nzp_kvar + ") and " +
                                     " is_del = 0 order by payment_date";
#endif
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
                    if (i <= finder.skip) continue;
                    AccountPayment ap = new AccountPayment();
                    ap.num = i.ToString();
                    if (reader["nzp_payment"] != DBNull.Value) ap.nzp_payment = Convert.ToInt32(reader["nzp_payment"]);
                    if (reader["dat_month"] != DBNull.Value) ap.dat_month = String.Format("{0:dd.MM.yyyy}", reader["dat_month"]);
                    if (reader["payment_id"] != DBNull.Value) ap.payment_id = Convert.ToDecimal(reader["payment_id"]);
                    if (reader["payment_date"] != DBNull.Value) ap.payment_date = String.Format("{0:dd.MM.yyyy}", reader["payment_date"]);
                    if (reader["sum_pay"] != DBNull.Value) ap.sum_pay = Convert.ToDecimal(reader["sum_pay"]);
                    if (reader["web_client_name"] != DBNull.Value) ap.web_client_name = Convert.ToString(reader["web_client_name"]);
                    if (reader["dat_when"] != DBNull.Value) ap.dat_when = String.Format("{0:dd.MM.yyyy}", reader["dat_when"]);
                    list.Add(ap);
                    if (finder.rows > 0 && i >= finder.skip + finder.rows) break;
                }
                reader.Close();
            }
            catch (Exception ex)
            {
                CloseReader(ref reader);
                conn_db.Close();

                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;

                MonitorLog.WriteLog("Ошибка получения GetAccountPayment " + err, MonitorLog.typelog.Error, 20, 201, true);
                return null;
            }
            ret.tag = recordsTotalCount;
            return list;
        }

        /// <summary>Удаление выгрузки</summary>
        /// <param name="nzpUser">Идентификатор пользователя</param>
        /// <param name="id">Идентификатор выгрузки</param>
        public Returns DeleteAllPayments(int nzpUser, int id)
        {
            Returns ret;

            #region Проверка входных параметров

            if (nzpUser <= 0)
            {
                ret = new Returns(false, "Не задан пользователь");
                return ret;
            }

            if (id <= 0)
            {
                ret = new Returns(false, "Не выбрана выгрузка");
                MonitorLog.WriteLog("DeleteAllPayments : Не выбрана выгрузка (id = " + id + ") ", 
                                        MonitorLog.typelog.Error, true);
                return ret;
            }

            #endregion

            #region Открываем соединение

            IDbConnection connDb = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(connDb, true);
            if (!ret.result)
            {
                ret.text = "Ошибка при открытии соединения с БД";
                MonitorLog.WriteLog("GetTransfersPayer : " + ret.text, MonitorLog.typelog.Error, true);
                return ret;
            }

            #endregion

            IDbTransaction transaction = connDb.BeginTransaction();
            try
            {

                #region  локальные переменные

                var listFiles = new List<FileInfo>(); //список наименований файла(полного пути)
                string prefData = Points.Pref + DBManager.sDataAliasRest;
                var listDate = new List<int>(); //список дат для которых имеются таблицы *central*_FIN_YY 

                byte beginYear = Convert.ToByte(Points.BeginWork.year_ - 2000),//начала работы банка данных
                        endYear = Convert.ToByte(DateTime.Now.Year - 2000);// текущая дата

                #endregion

                #region Получаем список файлов для удаления

                string sql = " SELECT TRIM(file_name) AS file_name " +
                             " FROM " + prefData + " bc_reestr_files " +
                             " WHERE id_bc_reestr = " + id;

                MyDataReader myread;
                ret = ExecRead(connDb, transaction, out myread, sql, true);
                if (!ret.result) throw new Exception(ret.text);

                while (myread.Read())
                {
                    string fileName = myread["file_name"] != DBNull.Value
                        ? Convert.ToString(myread["file_name"]).Trim()
                        : string.Empty;
                    var file = new FileInfo(Constants.Directories.FilesDir + fileName);
                    if (fileName != string.Empty && file.Exists) listFiles.Add(file);
                }
                myread.Close();

                #endregion

                #region Удаляем файл и все имеющиеся ссылки

                //список дат в которых имеются таблицы *central*_fin_YY
                for (var i = beginYear; i <= endYear; i++)
                {
                    string finTable = Points.Pref + "_fin_" + i + DBManager.tableDelimiter + "fn_sended";
                    if (TempTableInWebCashe(connDb, finTable)) listDate.Add(i);
                }

                foreach (var i in listDate)
                {
                    //удалить сылки в fn_sended
                    sql = " UPDATE " + Points.Pref + "_fin_" + i + DBManager.tableDelimiter + " fn_sended f SET id_bc_file = NULL " +
                          " FROM " + prefData + " bc_reestr_files r " +
                          " WHERE r.id = f.id_bc_file " +
                            " AND r.id_bc_reestr = " + id;
                    ret = ExecSQL(connDb, transaction, sql, true);
                    if (!ret.result) throw new Exception(ret.text);
                }

                sql = " DELETE FROM " + prefData + " bc_reestr_files WHERE id_bc_reestr = " + id;
                ret = ExecSQL(connDb, transaction, sql, true);
                if (!ret.result) throw new Exception(ret.text);

                sql = " DELETE FROM " + prefData + "bc_reestr WHERE id = " + id;
                ret = ExecSQL(connDb, transaction, sql, true);
                if (!ret.result) throw new Exception(ret.text);

                foreach (var file in listFiles) file.Delete();

                #endregion

                transaction.Commit();
            }
            catch (IOException ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры DeleteAllPayments : " + ex.Message, MonitorLog.typelog.Error, true);
                ret.text = "Ошибка при удалении выгрузки. Удаляемый файл используется.";
                ret.result = false;
                transaction.Rollback();
            }
            catch (System.Security.SecurityException ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры DeleteAllPayments : " + ex.Message, MonitorLog.typelog.Error, true);
                ret.text = "Ошибка при удалении выгрузки. Недостаточно прав для удаление файлов выгрузки.";
                ret.result = false;
                transaction.Rollback();
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры DeleteAllPayments : " + ex.Message, MonitorLog.typelog.Error, true);
                ret.text = "Ошибка при удалении выгрузки";
                ret.result = false;
                transaction.Rollback();
            }
            finally
            {
                connDb.Close();
                if (transaction != null) transaction.Dispose();
            }
            return ret;
        }

        /// <summary>Удаление файла выгрузки</summary>
        /// <param name="nzpUser">Идентификатор пользователя</param>
        /// <param name="files">Файлы</param>
        public Returns DeletePayments(int nzpUser, List<int> files) 
        {
            Returns ret;

            #region Проверка входных параметров

            if (nzpUser <= 0)
            {
                ret = new Returns(false, "Пользователь не определен");
                return ret;
            }

            if (files.Count == 0)
            {
                ret = new Returns(false, "Не выбран удаляемый файл");
                return ret;
            }

            #endregion

            #region Открываем соединение

            IDbConnection connDb = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(connDb, true);
            if (!ret.result)
            {
                ret.text = "Ошибка при открытии соединения с БД";
                MonitorLog.WriteLog("DeletePayments : " + ret.text, MonitorLog.typelog.Error, true);
                return ret;
            }

            #endregion

            IDbTransaction transaction = connDb.BeginTransaction();
            var fileNames = new List<FileInfo>();

            try
            {
                string prefData = Points.Pref + DBManager.sDataAliasRest;
                int beginYear = Points.BeginWork.year_ - 2000,
                    endYear = DateTime.Now.Year - 2000;

                string idFiles = string.Join(",", files).Trim(','); //todo протестировать

                var listDate = new List<int>();
                for (var i = beginYear; i <= endYear; i++)
                {
                    string finTable = Points.Pref + "_fin_" + i + DBManager.tableDelimiter + "fn_sended";
                    if (TempTableInWebCashe(connDb, transaction, finTable)) listDate.Add(i);
                }

                #region Получить список удаляемых файлов

                string sql = " SELECT file_name FROM " + prefData + " bc_reestr_files " +
                             " WHERE id IN (" + idFiles + ") ";
                
                MyDataReader myread;
                ret = ExecRead(connDb, transaction, out myread, sql, true);
                if (!ret.result) throw new Exception(ret.text);

                while (myread.Read())
                {
                    string fileName = myread["file_name"] != DBNull.Value
                        ? myread["file_name"].ToString().Trim()
                        : string.Empty;

                    var file = new FileInfo(Constants.Directories.ReportDir + fileName);
                    if (file.Exists)
                        fileNames.Add(file);
                }

                #endregion

                #region Удаление файла

                foreach (var i in listDate)
                {
                    //удалить сылки в fn_sended
                    sql = " UPDATE " + Points.Pref + "_fin_" + i + DBManager.tableDelimiter + "fn_sended " +
                          " SET id_bc_file = null WHERE id_bc_file IN (" + idFiles + ") ";
                    ret = ExecSQL(connDb, transaction, sql, true);
                    if (!ret.result) throw new Exception(ret.text);
                }

                ExecSQL(connDb, " DROP TABLE t_id_bc_reestr ");
                sql = " SELECT id_bc_reestr INTO TEMP t_id_bc_reestr " +
                      " FROM " + prefData + "bc_reestr_files " +
                      " WHERE id IN (" + idFiles + ") ";
                ret = ExecSQL(connDb, transaction, sql, true);
                if (!ret.result) throw new Exception(ret.text);

                sql = " DELETE FROM " + prefData + " bc_reestr_files " +
                      " WHERE id IN (" + idFiles + ")";
                ret = ExecSQL(connDb, transaction, sql, true);
                if (!ret.result) throw new Exception(ret.text);

                sql = " DELETE FROM " + prefData + " bc_reestr r " +
                      " WHERE id IN (SELECT id_bc_reestr FROM t_id_bc_reestr) " +
                        " AND 0 = (SELECT COUNT(*) " +
                                 " FROM " + prefData + " bc_reestr_files f " +
                                 " WHERE f.id_bc_reestr = r.id) ";
                ret = ExecSQL(connDb, transaction, sql, true);
                if (!ret.result) throw new Exception(ret.text);

                foreach (var fileN in fileNames)
                    fileN.Delete();

                #endregion

                transaction.Commit();
            }
            catch (IOException ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры DeletePayments : " + ex.Message, MonitorLog.typelog.Error, true);
                ret.text = "Ошибка при удалении файла выгрузки. Удаляемый файл выгрузки используется.";
                ret.result = false;
                transaction.Rollback();
            }
            catch (System.Security.SecurityException ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры DeletePayments : " + ex.Message, MonitorLog.typelog.Error, true);
                ret.text = "Ошибка при удалении файла выгрузки. Недостаточно прав для удаление файла выгрузки.";
                ret.result = false;
                transaction.Rollback();
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры DeletePayments : " + ex.Message, MonitorLog.typelog.Error, true);
                ret.text = "Ошибка при удалении файла выгрузки";
                ret.result = false;
                transaction.Rollback();
            }
            finally
            {
                connDb.Close();
                if (transaction != null) transaction.Dispose();
            }
            return ret;
        }

        /// <summary>Получить список банков осуществляющие платежи</summary>
        /// <param name="nzpUser">Идентификатор пользователя</param>
        /// <param name="ret">Результат выполнения функции</param>
        public List<Bank> GetBanksExecutingPayments(int nzpUser, out Returns ret)
        {
            #region Проверка входных параметров

            if (nzpUser <= 0)
            {
                ret = new Returns(false, "Пользователь не определен");
                return new List<Bank>();
            }

            #endregion

            #region Открываем соединение

            IDbConnection connDb = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(connDb, true);
            if (!ret.result)
            {
                ret.text = "Ошибка при открытии соединения с БД";
                MonitorLog.WriteLog("GetBanksExecutingPayments : " + ret.text, MonitorLog.typelog.Error, true);
                return new List<Bank>();
            }

            #endregion

            var banks = new List<Bank>();
            try
            {
                string prefKernel = Points.Pref + DBManager.sKernelAliasRest;
                
                MyDataReader myread;
                string sql = " SELECT DISTINCT spb.nzp_payer AS nzp_bank, " +
                                    " TRIM(spb.payer) AS bank " +
                             " FROM " + prefKernel + "s_payer spb INNER JOIN " + prefKernel + "payer_types pt ON spb.nzp_payer = pt.nzp_payer " +
                             " WHERE spb.id_bc_type IS NOT NULL " +
                               " AND (pt.nzp_payer_type = " + Convert.ToInt32(Payer.ContragentTypes.PayingAgent) +
                               " OR pt.nzp_payer_type = " + Convert.ToInt32(Payer.ContragentTypes.Bank) + ") " +
                             " ORDER BY 2 ";
                ret = ExecRead(connDb, out myread, sql, true);
                if (!ret.result)
                {
                    string erroe = ret.text;
                    ret.text = string.Empty;
                    throw new Exception(erroe);
                }

                while (myread.Read())
                {
                    int nzpBank = myread["nzp_bank"] != DBNull.Value ? Convert.ToInt32(myread["nzp_bank"]) : 0;
                    string bank = myread["bank"] != DBNull.Value ? myread["bank"].ToString().Trim() : string.Empty;
                    banks.Add(new Bank
                    {
                        nzp_bank = nzpBank,
                        bank = bank
                    });
                }
                myread.Close();
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры GetBanksExecutingPayments : " + ex.Message, MonitorLog.typelog.Error, true);
                ret.text = string.IsNullOrEmpty(ret.text) ? "Ошибка при получении списка банков" : ret.text;
                ret.result = false;
            }
            finally
            {
                if (connDb != null)
                    connDb.Close();
            }
            return banks;
        }
    }

}