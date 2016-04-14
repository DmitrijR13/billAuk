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
using Bars.KP50.Utils;

namespace STCLINE.KP50.DataBase
{
    public partial class DbAdmin : DbAdminClient
    {
        public Returns UploadEFS(FilesImported finder)
        {
            Returns ret = Utils.InitReturns();

            if (finder.nzp_user < 1) return new Returns(false, "Не задан пользователь");

            #region подключение к базе
            string conn_kernel = Points.GetConnByPref(Points.Pref);
            IDbConnection conn_db = GetConnection(conn_kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return ret;
            #endregion

            #region определение центрального пользователя
            DbWorkUser db = new DbWorkUser();
            Finder f_user = new Finder();
            f_user.nzp_user = finder.nzp_user;
            int nzpUser = db.GetLocalUser(conn_db, f_user, out ret);
            db.Close();
            if (!ret.result)
            {
                conn_db.Close();
                return ret;
            }
            #endregion
                        
            DateTime dt = Points.DateOper;
            EFSReestr efsreestr = new EFSReestr();
            efsreestr.nzp_user = finder.nzp_user;
            efsreestr.webLogin = finder.webLogin;
            efsreestr.webUname = finder.webUname;
            string efs_reestr = Points.Pref + "_data" + tableDelimiter + "efs_reestr";
            string sqlString = "insert into " + efs_reestr + " (file_name,date_uchet,changed_on,changed_by,packstatus, status)" +
                " values (" + Utils.EStrNull(finder.loaded_name) + "," + Utils.EStrNull(dt.ToShortDateString()) +
#if PG
                ", now(), "
#else
 ", current, "
#endif
 + nzpUser + "," + EFSReestr.ReestrStatuses.PackNotForm.GetHashCode() + "," + ExcelUtility.Statuses.InProcess.GetHashCode() + ")";
            ret = ExecSQL(conn_db, sqlString, true);
            if (!ret.result)
            {
                conn_db.Close();
                return ret;
            }
            int nzp_reestr = GetSerialValue(conn_db);
            efsreestr.nzp_efs_reestr = nzp_reestr;

            //директория файла
            string fDirectory = Constants.Directories.ImportDir.Replace("/", "\\");
            //имя файла
            string fileName = Path.Combine(fDirectory, finder.saved_name);

            #region Разархивация файла
            Utility.Archive arch = new Utility.Archive();
            var dir = "Source\\SBPAY";
            ret = arch.Decompress(fileName, dir, false);

            if (!ret.result)
            {
                sqlString = "update " + efs_reestr + " set status= " + ExcelUtility.Statuses.Failed.GetHashCode() +
                      ", comment = " + Utils.EStrNull("Не удалось извлечь файлы") +
                      " where nzp_efs_reestr = " + nzp_reestr;
                ret = ExecSQL(conn_db, sqlString, true);
                conn_db.Close();
                return new Returns(false, "Не удалось извлечь файлы", -1);
            }
            #endregion

            ret = UploadFilesPayCnt(finder, efsreestr, conn_db);
            conn_db.Close();
            return ret;
        }

        private List<EFSPay> GetListPayFromDBF(OleDbConnection oDbCon, out Returns ret)
        {
            #region проверка на наличие файлов
            if (!File.Exists("Source\\SBPAY\\PAY.DBF"))
            {
                ret = new Returns(false, "Не найден PAY.DBF", -1);
                return null;
            }
            #endregion

            #region Считывание dbf
            try
            {
                OleDbCommand cmd = new OleDbCommand();
                cmd.CommandText = "select * from pay";
                cmd.Connection = oDbCon;

                // Адаптер данных
                OleDbDataAdapter da = new OleDbDataAdapter();
                da.SelectCommand = cmd;
                // Заполняем объект данными
                DataTable tbl = new DataTable();
                da.Fill(tbl);


                List<EFSPay> list = new List<EFSPay>();
                foreach (DataRow dr in tbl.Rows)
                {
                    EFSPay sbp = new EFSPay();
                    if (dr["ID_PAY"] != DBNull.Value) sbp.id_pay = Convert.ToDecimal(dr["ID_PAY"]);
                    if (dr["ID_SERV"] != DBNull.Value) sbp.id_serv = Convert.ToString(dr["ID_SERV"]);
                    if (dr["LS_NUM"] != DBNull.Value) sbp.ls_num = Convert.ToDecimal(dr["LS_NUM"]);
                    if (dr["SUMMA"] != DBNull.Value) sbp.summa = Convert.ToDecimal(dr["SUMMA"]);
                    if (dr["PAY_DATE"] != DBNull.Value) sbp.pay_date = Convert.ToDateTime(dr["PAY_DATE"]).ToShortDateString();
                    if (dr["BARCODE"] != DBNull.Value) sbp.barcode = Convert.ToString(dr["BARCODE"]);
                    if (dr["ADDRESS"] != DBNull.Value) sbp.address = Convert.ToString(dr["ADDRESS"]);
                    if (dr["PLPOR_NUM"] != DBNull.Value) sbp.plpor_num = Convert.ToInt32(dr["PLPOR_NUM"]);
                    if (dr["PLPOR_DATE"] != DBNull.Value) sbp.plpor_date = Convert.ToDateTime(dr["PLPOR_DATE"]).ToShortDateString();

                    if (sbp.id_pay == 0 && sbp.id_serv == "" && sbp.ls_num == 0 && sbp.summa == 0 &&
                        sbp.pay_date == "" && sbp.barcode == "" && sbp.plpor_num == 0 && sbp.plpor_date == "") continue;
                    list.Add(sbp);
                }
                ret = Utils.InitReturns();
                return list;
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения GetListPayFromDBF : " + ex.Message, MonitorLog.typelog.Error, true);
                ret = new Returns(false);
                return null;
            }
            #endregion
        }

        private List<EFSCnt> GetListCntFromDBF(OleDbConnection oDbCon, out Returns ret)
        {
            #region проверка на наличие файлов
            if (!File.Exists("Source\\SBPAY\\CNT.DBF"))
            {
                ret = new Returns(false, "Не найден CNT.DBF", -1);
                return null;
            }
            #endregion

            #region Считывание dbf
            try
            {
                OleDbCommand cmd = new OleDbCommand();
                cmd.CommandText = "select * from cnt";
                cmd.Connection = oDbCon;

                // Адаптер данных
                OleDbDataAdapter da = new OleDbDataAdapter();
                da.SelectCommand = cmd;
                // Заполняем объект данными
                DataTable tbl = new DataTable();
                da.Fill(tbl);

                List<EFSCnt> list = new List<EFSCnt>();
                foreach (DataRow dr in tbl.Rows)
                {
                    EFSCnt sbc = new EFSCnt();
                    if (dr["ID_PAY"] != DBNull.Value) sbc.id_pay = Convert.ToDecimal(dr["ID_PAY"]);
                    if (dr["CNT_NUM"] != DBNull.Value) sbc.cnt_num = Convert.ToInt32(dr["CNT_NUM"]);
                    if (dr["CNT_VAL"] != DBNull.Value) sbc.cnt_val = Convert.ToDecimal(dr["CNT_VAL"]);
                    if (dr["CNT_VAL_BE"] != DBNull.Value) sbc.cnt_val_be = Convert.ToDecimal(dr["CNT_VAL_BE"]);
                    if (sbc.id_pay == 0 && sbc.cnt_num == 0 && sbc.cnt_val == 0 && sbc.cnt_val_be == 0) continue;
                    list.Add(sbc);
                }

                ret = Utils.InitReturns();
                return list;
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения GetListCntFromDBF : " + ex.Message, MonitorLog.typelog.Error, true);
                ret = new Returns(false);
                return null;
            }
            #endregion
        }



        private Returns UploadFilesPayCnt(FilesImported finder, EFSReestr efsreestr, IDbConnection conn_db)
        {
            Returns ret = Utils.InitReturns();

            // Создать объект подключения
            OleDbConnection oDbCon = new OleDbConnection();
            var myConnectionString = @"Provider=Microsoft.ACE.OLEDB.12.0;" +
                                     "Data Source=Source\\SBPAY;Extended Properties=dBASE III;";
            oDbCon.ConnectionString = myConnectionString;
            oDbCon.Open();
            string efs_reestr = Points.Pref + "_data" + tableDelimiter + "efs_reestr";
            List<EFSPay> listpay = GetListPayFromDBF(oDbCon, out ret);
            string sqlString = "";
            if (!ret.result)
            {
                oDbCon.Close();
                sqlString = "update " + efs_reestr + " set status= " + ExcelUtility.Statuses.Failed.GetHashCode() +
                    ", comment = " + Utils.EStrNull("Не удалось прочитать файл PAY.DBF") +
                    " where nzp_efs_reestr = " + efsreestr.nzp_efs_reestr;
                ret = ExecSQL(conn_db, sqlString, true);
                return ret;
            }
            List<EFSCnt> listcnt = GetListCntFromDBF(oDbCon, out ret);
            oDbCon.Close();
            if (!ret.result)
            {
                sqlString = "update " + efs_reestr + " set status= " + ExcelUtility.Statuses.Failed.GetHashCode() +
                    ", comment = " + Utils.EStrNull("Не удалось прочитать файл CNT.DBF") +
                    " where nzp_efs_reestr = " + efsreestr.nzp_efs_reestr;
                ret = ExecSQL(conn_db, sqlString, true);
                return ret;
            }

            if (listcnt == null && listpay == null)
            {
                sqlString = "update " + efs_reestr + " set status= " + ExcelUtility.Statuses.Failed.GetHashCode() +
                    ", comment = " + Utils.EStrNull("Нет данных для загрузки") +
                    " where nzp_efs_reestr = " + efsreestr.nzp_efs_reestr;
                ret = ExecSQL(conn_db, sqlString, true);
                return new Returns(true, "Нет данных для загрузки", -1);
            }
            if (listcnt.Count == 0 && listpay.Count == 0)
            {
                sqlString = "update " + efs_reestr + " set status= " + ExcelUtility.Statuses.Failed.GetHashCode() +
                       ", comment = " + Utils.EStrNull("Нет данных для загрузки") +
                       " where nzp_efs_reestr = " + efsreestr.nzp_efs_reestr;
                ret = ExecSQL(conn_db, sqlString, true);
                return new Returns(true, "Нет данных для загрузки", -1);
            }


            DateTime dt = Points.DateOper;

            IDbTransaction transaction = conn_db.BeginTransaction();

            string mes = "";

            try
            {
                sqlString = "INSERT INTO " + Points.Pref + "_fin_" + (dt.Year - 2000).ToString("00") + tableDelimiter + "efs_pay ( nzp_efs_reestr,id_pay,id_serv,ls_num,summa, " +
                            "pay_date,barcode,address,plpor_num,plpor_date ) VALUES" +
#if PG
 " ( :nzp_efs_reestr, :id_pay, :id_serv, :ls_num, :summa, :pay_date, :barcode, :address, :plpor_num, :plpor_date ) ";
#else
 " ( ?, ?, ?, ?, ?, ?, ?, ?, ?, ? ) ";
#endif
                IDbCommand ifxCommand = DBManager.newDbCommand(sqlString, conn_db, transaction);
#if PG
                DBManager.addDbCommandParameter(ifxCommand, "nzp_efs_reestr", DbType.Int32);
                DBManager.addDbCommandParameter(ifxCommand, "id_pay", DbType.Int32);
                DBManager.addDbCommandParameter(ifxCommand, "id_serv", DbType.String);
                DBManager.addDbCommandParameter(ifxCommand, "ls_num", DbType.Decimal);
                DBManager.addDbCommandParameter(ifxCommand, "summa", DbType.Decimal);
                DBManager.addDbCommandParameter(ifxCommand, "pay_date", DbType.Date);
                DBManager.addDbCommandParameter(ifxCommand, "barcode", DbType.String);
                DBManager.addDbCommandParameter(ifxCommand, "address", DbType.String);
                DBManager.addDbCommandParameter(ifxCommand, "plpor_num", DbType.Int32);
                DBManager.addDbCommandParameter(ifxCommand, "plpor_date", DbType.Date);


                foreach (EFSPay sb in listpay)
                {
                    (ifxCommand.Parameters[0] as IDbDataParameter).Value = efsreestr.nzp_efs_reestr;
                    (ifxCommand.Parameters[1] as IDbDataParameter).Value = sb.id_pay;
                    (ifxCommand.Parameters[2] as IDbDataParameter).Value = sb.id_serv;
                    (ifxCommand.Parameters[3] as IDbDataParameter).Value = sb.ls_num;
                    (ifxCommand.Parameters[4] as IDbDataParameter).Value = sb.summa;
                    if (sb.pay_date != "") (ifxCommand.Parameters[5] as IDbDataParameter).Value = Convert.ToDateTime(sb.pay_date);
                    else (ifxCommand.Parameters[5] as IDbDataParameter).Value = null;
                    (ifxCommand.Parameters[6] as IDbDataParameter).Value = sb.barcode;
                    (ifxCommand.Parameters[7] as IDbDataParameter).Value = sb.address;
                    (ifxCommand.Parameters[8] as IDbDataParameter).Value = sb.plpor_num;
                    if (sb.plpor_date != "") (ifxCommand.Parameters[9] as IDbDataParameter).Value = Convert.ToDateTime(sb.plpor_date);
                    else (ifxCommand.Parameters[9] as IDbDataParameter).Value = null;

                    ifxCommand.ExecuteNonQuery();
                }
#else
                ifxCommand.Prepare();

                foreach (EFSPay sb in listpay)
                {
                    ifxCommand.Parameters.Clear();
                    IfxCommandQueryParam ifxPrm = new IfxCommandQueryParam(ifxCommand);
                    ifxPrm.AddParam("nzp_efs_reestr", efsreestr.nzp_efs_reestr);
                    ifxPrm.AddParam("id_pay", sb.id_pay);
                    ifxPrm.AddParam("id_serv", sb.id_serv);
                    ifxPrm.AddParam("ls_num", sb.ls_num);
                    ifxPrm.AddParam("summa", sb.summa);
                    ifxPrm.AddParam("pay_date", sb.pay_date);
                    ifxPrm.AddParam("barcode", sb.barcode);
                    ifxPrm.AddParam("address", sb.address);
                    ifxPrm.AddParam("plpor_num", sb.plpor_num);
                    ifxPrm.AddParam("plpor_date", sb.plpor_date);

                    ifxCommand.ExecuteNonQuery();
                }
#endif
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                sqlString = "update " + efs_reestr + " set status= " + ExcelUtility.Statuses.Failed.GetHashCode() +
                      ", comment = " + Utils.EStrNull("Ошибка выполнения операции") +
                      " where nzp_efs_reestr = " + efsreestr.nzp_efs_reestr;
                ret = ExecSQL(conn_db, sqlString, true);

                MonitorLog.WriteLog("Ошибка выполнения UploadFilesPayCnt : " + ex.Message, MonitorLog.typelog.Error, true);
                return new Returns(false);
            }

            mes = "Файл PAY.DBF успешно загружен.";


            try
            {
                sqlString = "INSERT INTO " + Points.Pref + "_fin_" + (dt.Year - 2000).ToString("00") + tableDelimiter + "efs_cnt ( nzp_efs_reestr,id_pay,cnt_num,cnt_val,cnt_val_be )" +
#if PG
 "VALUES ( :nzp_efs_reestr, :id_pay, :cnt_num, :cnt_val, :cnt_val_be) ";
#else
 "VALUES ( ?, ?, ?, ?, ? ) ";
#endif
                IDbCommand ifxCommand = DBManager.newDbCommand(sqlString, conn_db, transaction);
#if PG
              
#else
                ifxCommand.Prepare();
#endif
                foreach (EFSCnt sb in listcnt)
                {
                    ifxCommand.Parameters.Clear();
                    IfxCommandQueryParam ifxPrm = new IfxCommandQueryParam(ifxCommand);
                    ifxPrm.AddParam("nzp_efs_reestr", efsreestr.nzp_efs_reestr);
                    ifxPrm.AddParam("id_pay", sb.id_pay);
                    ifxPrm.AddParam("cnt_num", sb.cnt_num);
                    ifxPrm.AddParam("cnt_val", sb.cnt_val);
                    ifxPrm.AddParam("cnt_val_be", sb.cnt_val_be);

                    ifxCommand.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                sqlString = "update " + efs_reestr + " set status= " + ExcelUtility.Statuses.Failed.GetHashCode() +
                   ", comment = " + Utils.EStrNull("Ошибка выполнения операции") +
                   " where nzp_efs_reestr = " + efsreestr.nzp_efs_reestr;
                ret = ExecSQL(conn_db, sqlString, true);
                MonitorLog.WriteLog("Ошибка выполнения UploadFilesPayCnt : " + ex.Message, MonitorLog.typelog.Error, true);
                return new Returns(false);
            }
            transaction.Commit();
            mes += " Файл CNT.DBF успешно загружен.";

            ret = CompareSbPayments(efsreestr, conn_db);

            conn_db.Close();
            return ret;
        }

        public List<EFSReestr> GetEFSReestr(EFSReestr finder, out Returns ret)
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

            string where = "";
            if (finder.nzp_efs_reestr > 0) where += " and nzp_efs_reestr = " + finder.nzp_efs_reestr;

            DbTables tables = new DbTables(conn_db);

            //Определить общее количество записей
            string sql = "select count(*) from " + Points.Pref + "_data" + tableDelimiter + "efs_reestr r ," + tables.user + " u " +
                " where r.changed_by=u.nzp_user " + where;
            object count = ExecScalar(conn_db, sql, out ret, true);
            int recordsTotalCount;
            try { recordsTotalCount = Convert.ToInt32(count); }
            catch (Exception e)
            {
                ret = new Returns(false, "Ошибка при определении количества записей: " + (Constants.Debug ? e.Message : ""));
                MonitorLog.WriteLog("Ошибка GetEFSReestr " + (Constants.Viewerror ? "\n" + e.Message : ""), MonitorLog.typelog.Error, 20, 201, true);
                conn_db.Close();
                return null;
            }

            List<EFSReestr> spis = new List<EFSReestr>();
            sql = " select r.nzp_efs_reestr, r.file_name, r.date_uchet, r.packstatus, r.changed_on, " +
                " r.status, r.nzp_exc,r.comment, " +
                "u.comment as username from " + Points.Pref + "_data" + tableDelimiter + "efs_reestr r ," + tables.user + " u " +
                " where r.changed_by=u.nzp_user " + where + " order by nzp_efs_reestr desc";
            IDataReader reader;
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
                    if (i <= finder.skip) continue;
                    EFSReestr zap = new EFSReestr();
                    if (reader["nzp_efs_reestr"] != DBNull.Value) zap.nzp_efs_reestr = Convert.ToInt32(reader["nzp_efs_reestr"]);
                    if (reader["file_name"] != DBNull.Value) zap.file_name = Convert.ToString(reader["file_name"]);
                    if (reader["username"] != DBNull.Value) zap.changed = Convert.ToString(reader["username"]).Trim();
                    if (reader["changed_on"] != DBNull.Value) zap.changed += " (" + Convert.ToDateTime(reader["changed_on"]).ToShortDateString() + ")";
                    if (reader["date_uchet"] != DBNull.Value) zap.date_uchet = Convert.ToDateTime(reader["date_uchet"]).ToShortDateString();
                    if (reader["packstatus"] != DBNull.Value) zap.packstatus = Convert.ToInt32(reader["packstatus"]);
                    if (reader["status"] != DBNull.Value) zap.status = Convert.ToInt32(reader["status"]);

                    if (reader["nzp_exc"] != DBNull.Value) zap.nzp_exc = Convert.ToInt32(reader["nzp_exc"]);

                    if (reader["comment"] != DBNull.Value) zap.comment = Convert.ToString(reader["comment"]).Trim();

                    if (zap.packstatus == EFSReestr.ReestrStatuses.PackNotForm.GetHashCode()) zap.strpackstatus = "Пачки не сформированы";
                    else if (zap.packstatus == EFSReestr.ReestrStatuses.PackForming.GetHashCode()) zap.strpackstatus = "Пачки в процессе формирования";
                    else zap.strpackstatus = "Пачки сформированы";

                    if (zap.status == -1) zap.strstatus = "Выгрузка в файл прошла неудачно";
                    else if (zap.status == -2)
                    {
                        zap.strstatus = "Выгрузка в файл прошла неудачно(функционал не поддерживается)";
                    }
                    else if (zap.status == 0) zap.strstatus = "В очереди";
                    else if (zap.status == 1) zap.strstatus = "Файл в процессе формирования";
                    else if (zap.status == 2) zap.strstatus = "Файл успешно сформирован";

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
                MonitorLog.WriteLog("Ошибка заполнения списка " + (Constants.Viewerror ? " \n " + ex.Message : ""), MonitorLog.typelog.Error, 20, 201, true);
                return null;
            }
        }

        public List<EFSPay> GetEFSPay(EFSPay finder, out Returns ret)
        {
            if (finder.nzp_user < 1)
            {
                ret = new Returns(false, "Не задан пользователь");
                return null;
            }

            if (finder.nzp_efs_reestr <= 0)
            {
                ret = new Returns(false, "Не задан код реестра");
                return null;
            }

            string conn_kernel = Points.GetConnByPref(Points.Pref);
            IDbConnection conn_db = GetConnection(conn_kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return null;

            string where = "";
            if (finder.nzp_pay > 0) where += " and nzp_pay = " + finder.nzp_pay;
            where += " and nzp_efs_reestr = " + finder.nzp_efs_reestr;

            DbTables tables = new DbTables(conn_db);

            //Определить общее количество записей
            string sql = "select count(*) from " + Points.Pref + "_fin_" + (Points.DateOper.Year - 2000).ToString("00") + tableDelimiter + "efs_pay " +
                " where 1=1 " + where;
            object count = ExecScalar(conn_db, sql, out ret, true);
            int recordsTotalCount;
            try { recordsTotalCount = Convert.ToInt32(count); }
            catch (Exception e)
            {
                ret = new Returns(false, "Ошибка при определении количества записей: " + (Constants.Debug ? e.Message : ""));
                MonitorLog.WriteLog("Ошибка GetEFSPay " + (Constants.Viewerror ? "\n" + e.Message : ""), MonitorLog.typelog.Error, 20, 201, true);
                conn_db.Close();
                return null;
            }

            List<EFSPay> spis = new List<EFSPay>();
            sql = " select * from " + Points.Pref + "_fin_" + (Points.DateOper.Year - 2000).ToString("00") + tableDelimiter + "efs_pay " +
                " where 1=1 " + where;
            IDataReader reader;
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
                    if (i <= finder.skip) continue;
                    EFSPay zap = new EFSPay();
                    if (reader["nzp_pay"] != DBNull.Value) zap.nzp_pay = Convert.ToInt32(reader["nzp_pay"]);
                    if (reader["nzp_efs_reestr"] != DBNull.Value) zap.nzp_efs_reestr = Convert.ToInt32(reader["nzp_efs_reestr"]);
                    if (reader["id_pay"] != DBNull.Value) zap.id_pay = Convert.ToDecimal(reader["id_pay"]);
                    if (reader["id_serv"] != DBNull.Value) zap.id_serv = Convert.ToString(reader["id_serv"]).Trim();
                    if (reader["ls_num"] != DBNull.Value) zap.ls_num = Convert.ToDecimal(reader["ls_num"]);
                    if (reader["summa"] != DBNull.Value) zap.summa = Convert.ToDecimal(reader["summa"]);
                    if (reader["pay_date"] != DBNull.Value) zap.pay_date = Convert.ToDateTime(reader["pay_date"]).ToShortDateString();
                    if (reader["barcode"] != DBNull.Value) zap.barcode = Convert.ToString(reader["barcode"]).Trim();
                    if (reader["address"] != DBNull.Value) zap.address = Convert.ToString(reader["address"]).Trim();
                    if (reader["plpor_num"] != DBNull.Value) zap.plpor_num = Convert.ToInt32(reader["plpor_num"]);
                    if (reader["plpor_date"] != DBNull.Value) zap.plpor_date = Convert.ToDateTime(reader["plpor_date"]).ToShortDateString();
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
                MonitorLog.WriteLog("Ошибка заполнения списка " + (Constants.Viewerror ? " \n " + ex.Message : ""), MonitorLog.typelog.Error, 20, 201, true);
                return null;
            }
        }

        public List<EFSCnt> GetEFSCnt(EFSCnt finder, out Returns ret)
        {
            if (finder.nzp_user < 1)
            {
                ret = new Returns(false, "Не задан пользователь");
                return null;
            }

            if (finder.nzp_efs_reestr <= 0)
            {
                ret = new Returns(false, "Не задан код реестра");
                return null;
            }

            string conn_kernel = Points.GetConnByPref(Points.Pref);
            IDbConnection conn_db = GetConnection(conn_kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return null;

            string where = "";
            if (finder.nzp_cnt > 0) where += " and nzp_cnt = " + finder.nzp_cnt;
            where += " and nzp_efs_reestr = " + finder.nzp_efs_reestr;
            if (finder.id_pay > 0) where += " and id_pay = " + finder.id_pay;

            DbTables tables = new DbTables(conn_db);

            //Определить общее количество записей
            string sql = "select count(*) from " + Points.Pref + "_fin_" + (Points.DateOper.Year - 2000).ToString("00") + tableDelimiter + "efs_cnt " +
                " where 1=1 " + where;
            object count = ExecScalar(conn_db, sql, out ret, true);
            int recordsTotalCount;
            try { recordsTotalCount = Convert.ToInt32(count); }
            catch (Exception e)
            {
                ret = new Returns(false, "Ошибка при определении количества записей: " + (Constants.Debug ? e.Message : ""));
                MonitorLog.WriteLog("Ошибка GetEFSCnt " + (Constants.Viewerror ? "\n" + e.Message : ""), MonitorLog.typelog.Error, 20, 201, true);
                conn_db.Close();
                return null;
            }

            List<EFSCnt> spis = new List<EFSCnt>();
            sql = " select * from " + Points.Pref + "_fin_" + (Points.DateOper.Year - 2000).ToString("00") + tableDelimiter + "efs_cnt " +
                " where 1=1 " + where;
            IDataReader reader;
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
                    if (i <= finder.skip) continue;
                    EFSCnt zap = new EFSCnt();
                    if (reader["nzp_cnt"] != DBNull.Value) zap.nzp_cnt = Convert.ToInt32(reader["nzp_cnt"]);
                    if (reader["nzp_efs_reestr"] != DBNull.Value) zap.nzp_efs_reestr = Convert.ToInt32(reader["nzp_efs_reestr"]);
                    if (reader["id_pay"] != DBNull.Value) zap.id_pay = Convert.ToDecimal(reader["id_pay"]);
                    if (reader["cnt_num"] != DBNull.Value) zap.cnt_num = Convert.ToInt32(reader["cnt_num"]);
                    if (reader["cnt_val"] != DBNull.Value) zap.cnt_val = Convert.ToDecimal(reader["cnt_val"]);
                    if (reader["cnt_val_be"] != DBNull.Value) zap.cnt_val_be = Convert.ToDecimal(reader["cnt_val_be"]);
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
                MonitorLog.WriteLog("Ошибка заполнения списка " + (Constants.Viewerror ? " \n " + ex.Message : ""), MonitorLog.typelog.Error, 20, 201, true);
                return null;
            }
        }

        public Returns DeleteFromEFSReestr(EFSReestr finder)
        {
            if (finder.nzp_user < 1) return new Returns(false, "Не задан пользователь");
            if (finder.nzp_efs_reestr <= 0) return new Returns(false, "Не выбрана запись", -1);

            #region подключение к базе
            string conn_kernel = Points.GetConnByPref(Points.Pref);
            IDbConnection conn_db = GetConnection(conn_kernel);
            Returns ret = OpenDb(conn_db, true);
            if (!ret.result) return ret;
            #endregion

            string sql;

            IDbTransaction transaction = conn_db.BeginTransaction();

            sql = "delete from " + Points.Pref + "_fin_" + (Points.DateOper.Year - 2000).ToString("00") + tableDelimiter + "efs_cnt " +
                " where nzp_efs_reestr = " + finder.nzp_efs_reestr;
            ret = ExecSQL(conn_db, transaction, sql, true);
            if (!ret.result)
            {
                transaction.Rollback();
                conn_db.Close();
                return ret;
            }

            sql = "delete from " + Points.Pref + "_fin_" + (Points.DateOper.Year - 2000).ToString("00") + tableDelimiter + "efs_pay " +
                " where nzp_efs_reestr = " + finder.nzp_efs_reestr;
            ret = ExecSQL(conn_db, transaction, sql, true);
            if (!ret.result)
            {
                transaction.Rollback();
                conn_db.Close();
                return ret;
            }

            sql = "delete from " + Points.Pref + "_data" + tableDelimiter + "efs_reestr where nzp_efs_reestr = " + finder.nzp_efs_reestr;
            ret = ExecSQL(conn_db, transaction, sql, true);
            if (!ret.result)
            {
                transaction.Rollback();
                conn_db.Close();
                return ret;
            }

            transaction.Commit();
            conn_db.Close();
            return ret;
        }

        public Returns CompareSbPayments(EFSReestr finder, IDbConnection conn_db)
        {
            if (finder.nzp_user <= 0) return new Returns(false, "Пользователь не задан");
            if (finder.nzp_efs_reestr <= 0) return new Returns(false, "Код реестра не задан");

            Returns ret = Utils.InitReturns();
            MyDataReader reader, reader2, reader3;
            string efs_reestr = Points.Pref + "_data" + tableDelimiter + "efs_reestr";
            #region определить БД webfon
            string sql = "select dbname,dbserver from " + Points.Pref + "_kernel" + tableDelimiter + "s_baselist where idtype = 8";
            ret = ExecRead(conn_db, out reader3, sql, true);
            if (!ret.result)
            {
                sql = "update " + efs_reestr + " set status= " + ExcelUtility.Statuses.Failed.GetHashCode() +
                       ", comment = " + Utils.EStrNull("Ошибка выполнения операции") +
                       " where nzp_efs_reestr = " + finder.nzp_efs_reestr;
                ret = ExecSQL(conn_db, sql, true);
                return ret;
            }
            string dbname = "", dbserver = "", namewebfon = "";
            if (reader3.Read())
            {
                if (reader3["dbname"] != DBNull.Value) dbname = Convert.ToString(reader3["dbname"]).Trim();
                if (reader3["dbserver"] != DBNull.Value) dbserver = Convert.ToString(reader3["dbserver"]).Trim();
            }
            reader3.Close();
            if (dbname != "") namewebfon = dbname;
#if PG
           // if (dbserver != "") namewebfon += "@" + dbserver;
#else
            if (dbserver != "") namewebfon += "@" + dbserver;
#endif
            #endregion

            string table_reestr = Points.Pref + "_data" + tableDelimiter + "efs_reestr";
            string table_sb_payment = namewebfon + tableDelimiter + "sb_payment";
            string table_sb_payment_counter = namewebfon + tableDelimiter + "sb_payment_counters";

            int numVsego = 0, numNot = 0, numErr = 0;
            List<string> list_err = new List<string>();
            List<string> list_war = new List<string>();

            if (!TempTableInWebCashe(conn_db, table_sb_payment))
            {
                list_err.Add("Информация об оплатах, полученных от веб-сервисов отсутствует в базе данных");
                numErr++;
            }

            if (!TempTableInWebCashe(conn_db, table_sb_payment_counter))
            {
                list_err.Add("Информация о счетчиках, полученных от веб-сервисов отсутствует в базе данных");
                numErr++;
            }


            //добавим информацию о протоколе загрузки в мои файлы
            ExcelRepClient dbRep = new ExcelRepClient();
            ret = dbRep.AddMyFile(new ExcelUtility()
            {
                nzp_user = finder.nzp_user,
                status = ExcelUtility.Statuses.InProcess,
                rep_name = "Протокол загрузки ежедневных файлов сверки",
                is_shared = 1
            });
            dbRep.Close();
            if (!ret.result)
            {
                sql = "update " + efs_reestr + " set status= " + ExcelUtility.Statuses.Failed.GetHashCode() +
                      ", comment = " + Utils.EStrNull("Ошибка выполнения операции") +
                      " where nzp_efs_reestr = " + finder.nzp_efs_reestr;
                ret = ExecSQL(conn_db, sql, true);
                return ret;
            }

            int nzpExc = ret.tag;
            sql = "update " + efs_reestr + " set nzp_exc= " + nzpExc +
                      " where nzp_efs_reestr = " + finder.nzp_efs_reestr;
            ret = ExecSQL(conn_db, sql, true);

            sql = " select date_uchet from " + table_reestr + " where nzp_efs_reestr = " + finder.nzp_efs_reestr;
            ret = ExecRead(conn_db, out reader, sql, true);
            if (!ret.result)
            {
                sql = "update " + efs_reestr + " set status= " + ExcelUtility.Statuses.Failed.GetHashCode() +
                      ", comment = " + Utils.EStrNull("Ошибка выполнения операции") +
                      " where nzp_efs_reestr = " + finder.nzp_efs_reestr;
                ret = ExecSQL(conn_db, sql, true);
                return ret;
            }

            if (reader.Read())
            {
                if (reader["date_uchet"] != DBNull.Value) finder.date_uchet = Convert.ToDateTime(reader["date_uchet"]).ToShortDateString();
            }
            reader.Close();

            if (finder.date_uchet == "")
            {
                sql = "update " + efs_reestr + " set status= " + ExcelUtility.Statuses.Failed.GetHashCode() +
                      ", comment = " + Utils.EStrNull("Не определена дата учета") +
                      " where nzp_efs_reestr = " + finder.nzp_efs_reestr;
                ret = ExecSQL(conn_db, sql, true);
                return new Returns(false, "Не определена дата учета");
            }

            string table_pay = Points.Pref + "_fin_" + (Convert.ToDateTime(finder.date_uchet).Year - 2000).ToString("00") + tableDelimiter + "efs_pay ";
            string table_cnt = Points.Pref + "_fin_" + (Convert.ToDateTime(finder.date_uchet).Year - 2000).ToString("00") + tableDelimiter + "efs_cnt ";

            sql = "select id_pay, id_serv, ls_num, summa, pay_date, barcode, address, plpor_num, plpor_date from " +
               table_pay + " where nzp_efs_reestr = " + finder.nzp_efs_reestr;
            ret = ExecRead(conn_db, out reader, sql, true);
            if (!ret.result)
            {
                sql = "update " + efs_reestr + " set status= " + ExcelUtility.Statuses.Failed.GetHashCode() +
                     ", comment = " + Utils.EStrNull("Ошибка выполнения операции") +
                     " where nzp_efs_reestr = " + finder.nzp_efs_reestr;
                ret = ExecSQL(conn_db, sql, true);
                return ret;
            }

            decimal pkod = 0, bank_cur_val = 0;
            int nzp_sb_payment = 0;

            while (reader.Read())
            {
                numVsego++;
                pkod = 0;
                bank_cur_val = 0;
                EFSPay efspay = new EFSPay();
                if (reader["plpor_date"] != DBNull.Value) efspay.plpor_date = Convert.ToDateTime(reader["plpor_date"]).ToShortDateString();
                if (reader["plpor_num"] != DBNull.Value) efspay.plpor_num = Convert.ToInt32(reader["plpor_num"]);
                if (reader["id_pay"] != DBNull.Value) efspay.id_pay = Convert.ToInt32(reader["id_pay"]);
                if (reader["id_serv"] != DBNull.Value) efspay.id_serv = Convert.ToString(reader["id_serv"]);
                if (reader["summa"] != DBNull.Value) efspay.summa = Convert.ToDecimal(reader["summa"]);
                if (reader["ls_num"] != DBNull.Value) efspay.ls_num = Convert.ToDecimal(reader["ls_num"]);
                if (efspay.plpor_date == "")
                {
                    list_err.Add("Нет даты платежного поручения. Уникальный идентификатор платежа: " + efspay.id_pay +
                        "; идентификатор задолженности: " + efspay.id_serv.Trim() +
                       "; платежный код ЛС: " + efspay.ls_num.ToString("N0") + "; сумма платежа: " + efspay.summa.ToString("N2"));
                    numErr++;
                }
                if (efspay.plpor_num <= 0)
                {
                    list_err.Add("Нет номера платежного поручения. Уникальный идентификатор платежа: " + efspay.id_pay +
                        "; идентификатор задолженности: " + efspay.id_serv.Trim() +
                       "; платежный код ЛС: " + efspay.ls_num.ToString("N0") + "; сумма платежа: " + efspay.summa.ToString("N2") +
                        "; дата платежа: " + efspay.plpor_date);
                    numErr++;
                }
                if (efspay.id_serv != "")
                {
                    string[] idserv = efspay.id_serv.Split("_");
                    if (idserv.Count() != 3)
                    {
                        list_err.Add("Идентификатор задолженности не соответствует формату: SERV_код услуги_код поставщика услуг. Уникальный идентификатор платежа: " + efspay.id_pay +
                            "; идентификатор задолженности: " + efspay.id_serv.Trim() +
                           "; платежный код ЛС: " + efspay.ls_num.ToString("N0") + "; сумма платежа: " + efspay.summa.ToString("N2"));
                        numErr++;
                    }
                    else
                    {
                        int nzp = 0;
                        if (!Int32.TryParse(idserv[1], out nzp))
                        {
                            list_err.Add("Идентификатор задолженности не соответствует формату: код услуги_код поставщика услуг." +
                                " Код услуги: " + idserv[1] + " должен быть числом. Уникальный идентификатор платежа: " + efspay.id_pay +
                           "; идентификатор задолженности: " + efspay.id_serv.Trim() +
                          "; платежный код ЛС: " + efspay.ls_num.ToString("N0") + "; сумма платежа: " + efspay.summa.ToString("N2"));
                            numErr++;
                        }

                        if (!Int32.TryParse(idserv[2], out nzp))
                        {
                            list_err.Add("Идентификатор задолженности не соответствует формату: код услуги_код поставщика услуг." +
                                " Код поставщика услуг: " + idserv[2] + " должен быть числом. Уникальный идентификатор платежа: " + efspay.id_pay +
                           "; идентификатор задолженности: " + efspay.id_serv.Trim() +
                          "; платежный код ЛС: " + efspay.ls_num.ToString("N0") + "; сумма платежа: " + efspay.summa.ToString("N2"));
                            numErr++;
                        }

                    }
                }

                if (TempTableInWebCashe(conn_db, table_sb_payment))
                {
                    sql = "select nzp_serv, nzp_supp, sum_pay, pkod, num_ls, nzp_sb_payment from " + table_sb_payment +
                        " where bank_operation_id = " + efspay.id_pay + " and 'SERV_'||nzp_serv||'_'||nzp_supp = " + Utils.EStrNull(efspay.id_serv) +
                        " and sum_pay = " + efspay.summa;
                    ret = ExecRead(conn_db, out reader2, sql, true);
                    if (!ret.result)
                    {
                        reader.Close();
                        sql = "update " + efs_reestr + " set status= " + ExcelUtility.Statuses.Failed.GetHashCode() +
                         ", comment = " + Utils.EStrNull("Ошибка выполнения операции") +
                         " where nzp_efs_reestr = " + finder.nzp_efs_reestr;
                        ret = ExecSQL(conn_db, sql, true);
                        return ret;
                    }

                    if (reader2.Read())
                    {
                        if (reader2["pkod"] != DBNull.Value) pkod = Convert.ToDecimal(reader2["pkod"]);
                        if (reader2["nzp_sb_payment"] != DBNull.Value) nzp_sb_payment = Convert.ToInt32(reader2["nzp_sb_payment"]);
                    }
                    else
                    {
                        list_err.Add("Не найдено совпадений в веб-сервисах: уникальный идентификатор платежа: " + efspay.id_pay +
                            "; идентификатор задолженности: " + efspay.id_serv.Trim() +
                           "; платежный код ЛС: " + efspay.ls_num.ToString("N0") + "; сумма платежа: " + efspay.summa.ToString("N2") +
                           "; дата платежа: " + efspay.plpor_date);
                        numNot++;
                        continue;
                    }
                    reader2.Close();

                    if (pkod != efspay.ls_num)
                    {
                        list_war.Add("Найдено совпадение в веб-сервисах, но платежные коды ЛС разные: уникальный идентификатор платежа: " +
                            efspay.id_pay + "; идентификатор задолженности: " + efspay.id_serv.Trim() +
                            "; платежный код ЛС: " + efspay.ls_num.ToString("N2") + "; сумма платежа: " + efspay.summa.ToString("N2") +
                            "; дата платежа: " + efspay.plpor_date);
                        numNot++; continue;
                    }
                }
                else
                {
                    numNot++;
                }

                if (TempTableInWebCashe(conn_db, table_sb_payment_counter))
                {
                    sql = "select cnt_val, cnt_num from " + table_cnt + " where nzp_efs_reestr = " + finder.nzp_efs_reestr +
                        " and id_pay = " + efspay.id_pay;
                    ret = ExecRead(conn_db, out reader2, sql, true);
                    if (!ret.result)
                    {
                        reader.Close();
                        sql = "update " + efs_reestr + " set status= " + ExcelUtility.Statuses.Failed.GetHashCode() +
                         ", comment = " + Utils.EStrNull("Ошибка выполнения операции") +
                         " where nzp_efs_reestr = " + finder.nzp_efs_reestr;
                        ret = ExecSQL(conn_db, sql, true);
                        return ret;
                    }
                    while (reader2.Read())
                    {
                        EFSCnt efscnt = new EFSCnt();
                        if (reader2["cnt_val"] != DBNull.Value) efscnt.cnt_val = Convert.ToDecimal(reader2["cnt_val"]);
                        if (reader2["cnt_num"] != DBNull.Value) efscnt.cnt_num = Convert.ToInt32(reader2["cnt_num"]);

                        sql = "select bank_cur_val from " + table_sb_payment_counter +
                            " where nzp_sb_payment = " + nzp_sb_payment + " and bank_cur_val = " + efscnt.cnt_val;
                        ret = ExecRead(conn_db, out reader3, sql, true);
                        if (!ret.result)
                        {
                            reader2.Close();
                            reader.Close();
                            sql = "update " + efs_reestr + " set status= " + ExcelUtility.Statuses.Failed.GetHashCode() +
                         ", comment = " + Utils.EStrNull("Ошибка выполнения операции") +
                         " where nzp_efs_reestr = " + finder.nzp_efs_reestr;
                            ret = ExecSQL(conn_db, sql, true);
                            return ret;
                        }

                        if (reader3.Read())
                        {
                            if (reader3["bank_cur_val"] != DBNull.Value) bank_cur_val = Convert.ToInt32(reader3["bank_cur_val"]);
                        }
                        else
                        {
                            list_war.Add("Найдено совпадение в веб-сервисах, отсутствуют показания в веб-сервисах: уникальный идентификатор платежа: " +
                                efspay.id_pay + "; идентификатор задолженности: " + efspay.id_serv.Trim() +
                           "; платежный код ЛС: " + efspay.ls_num.ToString("N0") + "; сумма платежа: " + efspay.summa.ToString("N2") +
                           "; дата платежа: " + efspay.plpor_date + "; Код счетчика " + efscnt.cnt_num + "; показание: " + efscnt.cnt_val);
                            numNot++; continue;
                        }
                        reader3.Close();

                        if (bank_cur_val != efscnt.cnt_val)
                        {
                            list_war.Add("Найдено совпадение в веб-сервисах, но показания счетчика разные: уникальный идентификатор платежа: " + efspay.id_pay + "; идентификатор задолженности: " + efspay.id_serv +
                                "; платежный код ЛС: " + efspay.ls_num.ToString("N0") +
                                "; сумма платежа: " + efspay.summa.ToString("N2") + "; дата платежа: " + efspay.plpor_date +
                                "; Код счетчика " + efscnt.cnt_num + "; показание: " + efscnt.cnt_val +
                                " не равно данным из веб-сервисов: " + bank_cur_val + ".");
                            numNot++; continue;
                        }
                    }
                    reader2.Close();
                }

            }
            reader.Close();

            //сохранение результатов в файл
            int k = 0;
            string filename = "prot_efs_" + finder.nzp_user + "_";

            while (System.IO.File.Exists(Constants.ExcelDir + filename + k + ".txt")) k++;

            filename = filename + k + ".txt";
            string fullfilename = Constants.ExcelDir + filename;
            using (System.IO.StreamWriter file = new System.IO.StreamWriter(Constants.ExcelDir + filename, true, Encoding.GetEncoding(1251)))
            {
                file.WriteLine("Протокол результатов ежедневной сверки  реестров от " + DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss"));

                file.WriteLine("Пользователь: " + finder.webLogin + " (код " + finder.nzp_user + ")");
                file.WriteLine("Всего найдено совпадений: " + (numVsego - numNot).ToString() + " из " + numVsego + ".");
                file.WriteLine("Другие ошибки: " + numErr + ".");
                if (list_err.Count > 0)
                {
                    file.WriteLine();
                    file.WriteLine("Список ошибок");
                    file.WriteLine();
                    foreach (string val in list_err)
                    {
                        file.WriteLine(val);
                    }
                }

                if (list_war.Count > 0)
                {
                    file.WriteLine();
                    file.WriteLine("Список предупреждений");
                    file.WriteLine();
                    foreach (string val in list_war)
                    {
                        file.WriteLine(val);
                    }
                }
            }

            if (InputOutput.useFtp) filename = InputOutput.SaveOutputFile(fullfilename);

            ExcelRepClient dbRep2 = new ExcelRepClient();
            dbRep2.SetMyFileProgress(new ExcelUtility() { nzp_exc = nzpExc, progress = 1 });
            dbRep2.SetMyFileState(new ExcelUtility() { nzp_exc = nzpExc, status = ExcelUtility.Statuses.Success, exc_path = filename });
            dbRep2.Close();

            sql = "update " + efs_reestr + " set status= " + ExcelUtility.Statuses.Success.GetHashCode() +
                     ", comment = " + Utils.EStrNull("Операция завершена") +
                     " where nzp_efs_reestr = " + finder.nzp_efs_reestr;
            ret = ExecSQL(conn_db, sql, true);

            return ret;
        }

    }
}
