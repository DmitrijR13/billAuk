
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using SevenZip;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;
using System.IO;
using System.Threading;

namespace Bars.KP50.Faktura.Source.Base
{
    public class DbFakturaCover
    {

        private object _curThreadinProcess = 0;
        private const int MaxThreadCount = 3;
        private List<Thread> threadPooList;


        public float GetFakturaFreeMem()
        {
            try
            {
                var Ram = new PerformanceCounter("Memory", "Available MBytes");
                return Ram.NextValue();
            }
            catch (Exception)
            {

                return 10000;
            }

        }

        public float GetFreeDiskSpace(string drive)
        {
            try
            {

                var disk = new DriveInfo(drive);
                return disk.TotalFreeSpace/1048576;
            }
            catch (Exception)
            {

                return 10000;
            }

        }


        public bool ProcessBillFon()
        {
            threadPooList = new List<Thread>();
            IDbConnection connWeb = DBManager.GetConnection(Constants.cons_Webdata);
            Returns ret = DBManager.OpenDb(connWeb, true);
            if (!ret.result) return false;

            string ipAdr = String.Empty;

            #if DEBUG
            try
            {
                System.Net.IPAddress[] ips = System.Net.Dns.GetHostAddresses(System.Net.Dns.GetHostName());
                foreach (System.Net.IPAddress ip in ips)
                {
                    if (ip.AddressFamily == AddressFamily.InterNetwork)
                    {
                        ipAdr = ip.ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("ProcessBillFon\nОшибка при определении ip адреса\n" + ex, MonitorLog.typelog.Error, true);
                ipAdr = String.Empty;
            }
            #endif

            string tableBillFon = DBManager.GetFullBaseName(connWeb) + DBManager.tableDelimiter + "bill_fon";
            MyDataReader reader = null;
            int nzp_key = 0;

            try
            {
                ret = DBManager.ExecRead(connWeb, out reader,
                    " Select * " +
                    " From " + tableBillFon + " " +
                    " Where year_ is not null and year_ > 0 " +
                    " and month_ is not null and month_ > 0 " +
                    " and kod_info = " + (int) FonTask.Statuses.InQueue +
                    (ipAdr.Trim() != String.Empty ? " and ip_adr = '" + ipAdr.Trim() + "'" : String.Empty) +
                    " Order by dat_in, nzp_key", true);
                if (!ret.result)
                {
                    return false;
                }



                while (reader.Read())
                {
                    //Забираем задание
                    ret = DBManager.ExecSQL(connWeb,
                        " Update " + tableBillFon +
                        " Set kod_info = " + (int) FonTask.Statuses.InProcess + "," +
                        " dat_work =  " + DBManager.sCurDateTime +
                        " Where nzp_key = " + (int) reader["nzp_key"], true);
                    if (!ret.result)
                        throw new Exception("Ошибка обновления задачи формирования платежных документов");
                            // return false;



                    var bill = new BillFonTask();
                    Int16 iWithDolg = 0, iWithUk = 0, iWithGeu = 0, iWithUchastok = 0;

                    if (reader["nzp_key"] != DBNull.Value) bill.nzp_key = (int) reader["nzp_key"];
                    if (reader["nzp_wp"] != DBNull.Value) bill.nzp_wp = (int) reader["nzp_wp"];
                    if (reader["nzp_area"] != DBNull.Value) bill.nzp_area = (int) reader["nzp_area"];
                    if (reader["nzp_geu"] != DBNull.Value) bill.nzp_geu = (int) reader["nzp_geu"];
                    if (reader["year_"] != DBNull.Value) bill.year_ = (int) reader["year_"];
                    if (reader["month_"] != DBNull.Value) bill.month_ = (int) reader["month_"];
                    if (reader["nzp_user"] != DBNull.Value) bill.nzp_user = (int) reader["nzp_user"];
                    if (reader["count_list_in_pack"] != DBNull.Value)
                        bill.count_list_in_pack = (int) reader["count_list_in_pack"];
                    if (reader["kod_sum_faktura"] != DBNull.Value)
                        bill.kod_sum_faktura = (int) reader["kod_sum_faktura"];
                    if (reader["result_file_type"] != DBNull.Value)
                        bill.result_file_type = (string) reader["result_file_type"];
                    if (reader["id_faktura"] != DBNull.Value) bill.id_faktura = (int) reader["id_faktura"];
                    if (reader["with_dolg"] != DBNull.Value) iWithDolg = (Int16) reader["with_dolg"];
                    if (reader["with_uk"] != DBNull.Value) iWithUk = (Int16)reader["with_uk"];
                    if (reader["with_geu"] != DBNull.Value) iWithGeu = (Int16)reader["with_geu"];
                    if (reader["with_uchastok"] != DBNull.Value) iWithUchastok = (Int16)reader["with_uchastok"];

                    if (iWithDolg == 1) bill.with_dolg = true;
                    if (iWithUk == 1) bill.with_uk = true;
                    if (iWithGeu == 1) bill.with_geu = true;
                    if (iWithUchastok == 1) bill.with_uchastok = true;

                    if (bill.year_ <= 0 || bill.month_ <= 0)
                    {
                        ret.result = false;
                        MonitorLog.WriteLog("Неверные параметры задачи " + bill, MonitorLog.typelog.Error, true);
                    }
                    else
                    {
                        RunBill(bill, tableBillFon);
                    }
                }
                reader.Close();

                #region Ожидание завершения процессов формирования счетов

                if (threadPooList.Count > 0)
                {
                    bool hasAliveThread = true;
                    while (hasAliveThread)
                    {
                        hasAliveThread = false;
                        Thread.Sleep(20000);
                        foreach (Thread tr in threadPooList)
                        {
                            hasAliveThread = hasAliveThread || tr.IsAlive;
                        }
                    }
                    threadPooList.Clear();
                }

                #endregion

            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;
                MonitorLog.WriteLog("ProcessBillFon\n" + ex.Message, MonitorLog.typelog.Error, true);

                return false;
            }
            finally
            {
                if (reader != null) reader.Close();
                connWeb.Close();
            }

            return true;

        }

        /// <summary>
        /// Ожидание запуска процессов формирования счетов
        /// </summary>
        /// <param name="bill"></param>
        /// <param name="table"></param>
        public void RunBill(BillFonTask bill, string table)
        {

            try
            {

                #region Проверка оперативной памяти

                //Памяти должно быть не менне 200кб на сформированный счет
                if (GetFakturaFreeMem() < bill.count_list_in_pack*0.2)
                {
                    MonitorLog.WriteLog(
                        "Недостаточно оперативной памяти (" + GetFakturaFreeMem() + ")для формирования счета " +
                        bill + " требуется " +
                        bill.count_list_in_pack*0.2,
                        MonitorLog.typelog.Error, true);
                    return;
                }

                #endregion


                #region Проверка свободного места на диске

                if (GetFreeDiskSpace(Path.GetPathRoot(Constants.ExcelDir)) < bill.count_list_in_pack*0.2)
                {
                    MonitorLog.WriteLog(
                        "Недостаточно дисковой памяти (" + GetFreeDiskSpace(Path.GetPathRoot(Constants.ExcelDir)) +
                        ") для формирования счета " +
                        bill + " требуется " +
                        bill.count_list_in_pack*0.2 + " Мб",
                        MonitorLog.typelog.Error, true);
                    return;
                }

                #endregion

            }
            catch (Exception)
            {

            }


            bool beginStart = false;
            int longProcessCounter = 0;
            while (!beginStart && longProcessCounter < 320) //два часа
            {
                lock (_curThreadinProcess)
                {
                    if ((int) _curThreadinProcess < MaxThreadCount)
                    {
                        _curThreadinProcess = (int) _curThreadinProcess + 1;

                        beginStart = true;
                    }
                }
                if (beginStart)
                {
                    var tr = new Thread(MakeOneBillProcess);
                    threadPooList.Add(tr);
                    tr.Start(bill);
                    Thread.Sleep(1);
                    while (!tr.IsAlive) ;
                }
                else
                {
                    Thread.Sleep(20000);
                    longProcessCounter++;
                }
            }


        }

        /// <summary>
        /// Запуск одного процесса формирования счетов
        /// </summary>
        /// <param name="bills">Задача</param>
        private void MakeOneBillProcess(object bills)
        {
            BillFonTask bill = (BillFonTask) bills;

            STCLINE.KP50.Global.Utils.setCulture();

            IDbConnection connWeb = DBManager.GetConnection(Constants.cons_Webdata);
            Returns ret = DBManager.OpenDb(connWeb, true);
            if (!ret.result) return;

            string tableBillFon = DBManager.GetFullBaseName(connWeb) + DBManager.tableDelimiter + "bill_fon";

            //есть задание, выполняем! УРА!!!
            try
            {


                string pref = Points.GetPref(bill.nzp_wp);

                var finder = new STCLINE.KP50.Interfaces.Faktura
                {
                    pref = pref,
                    nzp_area = bill.nzp_area,
                    nzp_geu = bill.nzp_geu,
                    destFileName =
                        "Bank_" + pref + "_" + bill.year_ + bill.month_.ToString("00") + "_" + bill.nzp_wp +
                        bill.nzp_area + bill.nzp_geu + bill.nzp_key,
                    nzp_kvar = 0,
                    nzp_dom = 0,
                    nzp_user = bill.nzp_user,
                    workRegim = STCLINE.KP50.Interfaces.Faktura.WorkFakturaRegims.Bank,
                    month_ = bill.month_,
                    year_ = bill.year_
                };

                finder.YM.year_ = finder.year_;
                finder.YM.month_ = finder.month_;

                finder.countListInPack = bill.count_list_in_pack;
                finder.kodSumFaktura = (STCLINE.KP50.Interfaces.Faktura.Kinds) bill.kod_sum_faktura;
                finder.pm_note = bill.nzp_key.ToString(CultureInfo.InvariantCulture);

                string fileType = bill.result_file_type;

                finder.resultFileType = fileType.Trim() == "FPX"
                    ? STCLINE.KP50.Interfaces.Faktura.FakturaFileTypes.FPX
                    : STCLINE.KP50.Interfaces.Faktura.FakturaFileTypes.PDF;

                finder.idFaktura = bill.id_faktura;
                finder.withDolg = bill.with_dolg;
                finder.withUk = bill.with_uk;
                finder.withGeu = bill.with_geu;
                finder.withUchastok = bill.with_uchastok;
                List<string> fName;
                var faktura = new DbFaktura();
                try
                {
                    fName = faktura.GetFaktura(finder, out ret);

                }
                finally
                {
                    faktura.Close();
                }

                if (ret.result && fName != null && fName.Count > 0)
                {
                    var excelRep = new ExcelRepClient();
                    ret = excelRep.AddMyFile(new ExcelUtility
                    {
                        nzp_user = 1,
                        status = ExcelUtility.Statuses.Success,
                        dat_out = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                        rep_name = "Платежные документы за " + finder.YM.name_month + " " + finder.year_ + "г.",
                        is_shared = 1,
                        exc_path = fName[0]
                    });
                    excelRep.Close();

                    if (ret.result)
                    {
                        ret = DBManager.ExecSQL(connWeb,
                            " Update " + tableBillFon +
                            " Set kod_info = " + (int) FonTask.Statuses.Completed +
                            ", dat_out = " + DBManager.sCurDateTime +
                            ", file_name = '" + ret.tag + "'" +
                            " Where nzp_key = " + bill.nzp_key, true);
                    }

                    if (!ret.result)
                        throw new Exception("Ошибка формирования платежных документов " + bill); // return false;

                    if (fName.Count == 0)
                    {
                        throw new Exception("Отсутствует файлq сформированного платежного документа " + bill);
                    }
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка при формирования счета " + bill + " " +
                                    Environment.NewLine + ex, MonitorLog.typelog.Error, true);
            }
            finally
            {
                if (!ret.result && bill.nzp_key > 0)
                {
                    DBManager.ExecSQL(connWeb,
                        " Update " + tableBillFon +
                        " Set kod_info = " + (int) FonTask.Statuses.Failed + ", " +
                        " dat_out = " + DBManager.sCurDateTime +
                        " Where nzp_key = " + bill.nzp_key, true);
                }

                connWeb.Close();

                lock (_curThreadinProcess)
                {
                    if ((int) _curThreadinProcess > 0) _curThreadinProcess = (int) _curThreadinProcess - 1;
                }
            }
        }
    }




    public partial class DbFaktura
        {

        public List<string> GetFakturaFiles(Prm prm, out Returns ret, string nzpUser)
        {
            var finder = new STCLINE.KP50.Interfaces.Faktura
            {
                nzp_kvar = 0,
                nzp_dom = 0,
                nzp_user = Int32.Parse(nzpUser),
                workRegim = STCLINE.KP50.Interfaces.Faktura.WorkFakturaRegims.Group,
                month_ = prm.month_,
                year_ = prm.year_
            };
            //finder.pref = pref;
            //finder.destFileName = "Group_" + pref + "_" + prm.year_.ToString() + prm.month_.ToString("00");

            finder.YM.year_ = finder.year_;
            finder.YM.month_ = finder.month_;
            var dbFaktura = new DbFaktura();
            List<string> fName = dbFaktura.GetFaktura(finder, out ret);
            dbFaktura.Close();

            return fName.ToList();
        }

        public Returns GetFakturaWeb(int sessionID)
        {
            Returns ret = STCLINE.KP50.Global.Utils.InitReturns();
            using (IDbConnection conDb = DBManager.GetConnection(Constants.cons_Kernel))
            {
                try
                {
                    ret = DBManager.OpenDb(conDb, true);
                    MyDataReader reader;

                    StringBuilder strSQLQuery = new StringBuilder();
                    strSQLQuery.AppendFormat("SELECT dbname, dbserver FROM {0}{1}s_baselist WHERE idtype = 8",
                         Points.Pref, DBManager.sKernelAliasRest);
                    string strWbfDatabase = "";
                    ret = DBManager.ExecRead(conDb, out reader, strSQLQuery.ToString(), true);
                    if (!ret.result)
                    {
                        MonitorLog.WriteLog("Ошибка выполнения запроса", MonitorLog.typelog.Error, true);
                        ret.text = " Ошибка выполнения запроса. ";
                        ret.tag = -1;
                        return ret;
                    }

                    string dbName = null;
                    string dbServer = null;
                    while (reader.Read())
                    {
                        if (reader["dbname"] != DBNull.Value) dbName = reader["dbname"].ToString().Trim();
                        if (reader["dbserver"] != DBNull.Value) dbServer = reader["dbserver"].ToString().Trim();
                    }

                    strWbfDatabase = (!string.IsNullOrEmpty(dbServer)) ?
                        string.Format("{0}@{1}{2}{3}", dbName, dbServer, DBManager.tableDelimiter, "session_ls") :
                        DBManager.GetFullBaseName(conDb, dbName, "session_ls");

                    strSQLQuery = new StringBuilder();
                    strSQLQuery.Append("SELECT a.nzp_session AS wbf_nzp_session, b.nzp_dom AS dat_nzp_dom, a.dat_charge AS wbf_dat_charge ");
                    strSQLQuery.AppendFormat("FROM {0} a ", strWbfDatabase);
                    strSQLQuery.AppendFormat("INNER JOIN {0} b ON (a.num_ls = b.num_ls) ", DBManager.GetFullBaseName(conDb, Points.Pref + "_data", "kvar"));
                    strSQLQuery.AppendFormat("WHERE a.nzp_session = {0}", sessionID);

                    /*
                    strSQLQuery = " SELECT a.nzp_session AS wbf_nzp_session, " +
                                  "     b.nzp_dom AS dat_nzp_dom, " +
                                  "     a.dat_charge AS wbf_dat_charge " +
                                  " FROM " + strWbfDatabase + " , " + Points.Pref + DBManager.sDataAliasRest + "kvar b" +
                                  " WHERE a.num_ls = b.num_ls " +
                                  "     AND a.nzp_session = " + sessionID;
                    */
                    ret = DBManager.ExecRead(conDb, out reader, strSQLQuery.ToString(), true);
                    if (!ret.result)
                    {
                        MonitorLog.WriteLog("Ошибка выполнения запроса", MonitorLog.typelog.Error, true);
                        ret.text = " Ошибка выполнения запроса. ";
                        ret.tag = -1;
                        return ret;
                    }

                    int intWBFNzpSession = 0;
                    int intDatNzpDom = 0;
                    var dtWBFDatCharge = new DateTime();

                    STCLINE.KP50.Global.Utils.setCulture();
                    while (reader.Read())
                    {
                        if (reader["wbf_nzp_session"] != DBNull.Value) intWBFNzpSession = Convert.ToInt32(reader["wbf_nzp_session"]);
                        if (reader["dat_nzp_dom"] != DBNull.Value) intDatNzpDom = Convert.ToInt32(reader["dat_nzp_dom"]);
                        if (reader["wbf_dat_charge"] != DBNull.Value) dtWBFDatCharge = Convert.ToDateTime(reader["wbf_dat_charge"]);
                    }

                    if (intWBFNzpSession == 0 && intDatNzpDom == 0)
                    {
                        strSQLQuery = new StringBuilder();
                        strSQLQuery.AppendFormat("UPDATE {0} SET cur_oper = -5 WHERE nzp_session = {1};", strWbfDatabase, sessionID);
                        ret = DBManager.ExecSQL(conDb, strSQLQuery.ToString(), true);
                        MonitorLog.WriteLog("Ошибка GetFakturaWeb: По заданному Session ID не найдено ни одной записи.", MonitorLog.typelog.Info, true);
                        ret.result = false;
                        ret.text = "По заданному Session ID не найдено ни одной записи.";
                        ret.tag = -1;
                        return ret;
                    }

                    var cf = new CalcFonTask
                    {
                        nzp = intDatNzpDom,
                        nzpt = intWBFNzpSession,
                        TaskType = CalcFonTask.Types.taskGetFakturaWeb,
                        year_ = dtWBFDatCharge.Year,
                        month_ = dtWBFDatCharge.Month,
                        Status = FonTask.Statuses.New,
                        QueueNumber = 3
                    };

                    var dcClient = new DbCalcQueueClient();
                    if (!dcClient.AddTask(cf).result)
                    {
                        dcClient.Close();
                        strSQLQuery = new StringBuilder();
                        strSQLQuery.AppendFormat("UPDATE {0} SET cur_oper = -5 WHERE nzp_session = {1};", strWbfDatabase, sessionID);
                        ret = DBManager.ExecSQL(conDb, strSQLQuery.ToString(), true);
                        MonitorLog.WriteLog("Ошибка GetFakturaWeb: Ошибка добавления задания в очередь.", MonitorLog.typelog.Error, true);
                        ret.result = false;
                        ret.text = "Ошибка добавления задания в очередь.";
                        ret.tag = -1;
                        return ret;
                    }
                    dcClient.Close();

                    string strWebDatabase = "";
                    foreach (string str in Constants.cons_Webdata.Split(';'))
                        if (str.Split('=')[0].ToLower() == "database") { strWebDatabase = str.Split('=')[1]; break; }

                    strSQLQuery = new StringBuilder();
                    strSQLQuery.AppendFormat("SELECT MAX(nzp_key) AS nzp_key FROM {0};",
                        DBManager.GetFullBaseName(conDb, strWebDatabase, "calc_fon_3"));
                    ret = DBManager.ExecRead(conDb, out reader, strSQLQuery.ToString(), true);
                    if (!ret.result)
                    {
                        MonitorLog.WriteLog("Ошибка выполнения запроса", MonitorLog.typelog.Error, true);
                        ret.text = " Ошибка выполнения запроса. ";
                        ret.tag = -1;
                        return ret;
                    }

                    int intTaskID = -1;
                    while (reader.Read()) if (reader["nzp_key"] != DBNull.Value)
                        { intTaskID = Convert.ToInt32(reader["nzp_key"]); break; }

                    ret.text = "TaskID";
                    ret.tag = intTaskID;
                }
                catch (Exception ex)
                {
                    MonitorLog.WriteLog("Ошибка выполнения процедуры GetFakturaWeb : " + ex.Message, MonitorLog.typelog.Error, true);
                    ret.result = false;
                    ret.tag = -1;
                }
                finally
                {
                    conDb.Close();
                }
                return ret;
            }
        }
    }
}
