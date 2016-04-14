using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Globalization;
using System.Data;
using Bars.KP50.DB.Admin.Source.OrderSequence;
using Bars.KP50.SzExchange.Unload;
using Bars.KP50.SzExchange.UnloadForSZ;
using Bars.KP50.Utils;
using STCLINE.KP50.Global;
using STCLINE.KP50.IFMX.Kernel.source.CommonType;
using STCLINE.KP50.Interfaces;
using System.IO;
using Newtonsoft.Json;
using Bars.KP50.Faktura.Source.Base;
using Bars.KP50.SzExchange.LoadFromSz;


#region Расчет и фоновые задачи
namespace STCLINE.KP50.DataBase
{
    #region  Здесь находятся классы для фоновых задач расчетов
    public partial class DbCalc : DataBaseHead
    {
        #region Настройки

        public const bool REVAL = true; //true; //для отладки, в Самару пока надо ставить false
        #endregion Настройки



        #region Фоновые задачи
        public bool IsNowCalcCharge(long _nzp_dom, string pref, out Returns ret)
        {
            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result)
            {
                return true;
            }

            CalcFonTask calcfon = new CalcFonTask();
            calcfon.nzp = Convert.ToInt32(_nzp_dom);
            calcfon.TaskType = CalcFonTask.Types.taskWithReval;
            calcfon.nzpt = Points.GetPoint(pref).nzp_wp;

            RecordMonth rm = Points.GetCalcMonth(new CalcMonthParams(pref));
            calcfon.year_ = rm.year_;
            calcfon.month_ = rm.month_;

            calcfon.QueueNumber = Points.GetCalcNum(pref); //определить номер потока расчета

            if (calcfon.QueueNumber < CalcThreads.maxCalcThreads)
            {
                ret = DbCalcQueueClient.CheckTask(conn_web, calcfon);

                if (!ret.result || ret.tag == Constants.workinfon)
                {
                    conn_web.Close();
                    return true;
                }
            }
            conn_web.Close();

            return false;
        }

        /// <summary>
        /// Cчитывает очередное задачи из очереди и вызывает обработчик задания.
        /// В случае успешной обработки задания возвращает true
        /// </summary>
        /// <param name="number">Номер очереди заданий</param>
        /// <returns>Возвращает true, если была считана и успешно обработана задача для обработки, false - задачи не было, функция отработала вхолостую</returns>
        public bool CalcFonProc(int number)
        {
            bool taskExists = false;
            string tab = sDefaultSchema + "calc_fon_" + number;
            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            Returns ret = OpenDb(conn_web, true);
            if (!ret.result) return taskExists;

            string ipAdr = String.Empty;
#if DEBUG
            try
            {
                System.Net.IPAddress[] ips = System.Net.Dns.GetHostAddresses(System.Net.Dns.GetHostName());
                foreach (System.Net.IPAddress ip in ips)
                {
                    if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                    {
                        ipAdr = ip.ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("CalcFonProc(int number)\nОшибка при определении ip адреса\n" + ex, MonitorLog.typelog.Error, true);
                ipAdr = String.Empty;
            }
#endif

            MyDataReader reader = null;
            try
            {
                //задание уже висит на выполнении, выходим
                //#if PG
                //                ExecSQL(conn_web, "set search_path to 'public'", false);
                //#endif

                bool isAnyTaskBeingProcessed = DbCalcQueueClient.IsAnyTaskBeingProcessed(conn_web, tab, out ret);

                if (!ret.result || isAnyTaskBeingProcessed)
                {
                    return taskExists;
                }

                string sql = " Select * From " + tab +
                    " Where kod_info = 3 " +
                    "   and " + sNvlWord + "(dat_when, " + MDY(1, 1, 1900) + ") < " + sCurDateTime +
                    (ipAdr.Trim() != String.Empty ? " and ip_adr = '" + ipAdr.Trim() + "'" : String.Empty) +
                    " Order by prior, nzp, nzpt, year_, month_, task ";

                ret = ExecRead(conn_web, out reader, sql, true);
                if (!ret.result)
                {
                    return taskExists;
                }

                if (reader.Read())
                {
                    taskExists = true;
                    //есть задание, выполняем!

                    CalcFonTask calcfon = new CalcFonTask();
                    calcfon.QueueNumber = number;
                    calcfon.nzp = 0;
                    calcfon.year_ = 0;
                    calcfon.month_ = 0;
                    calcfon.nzp_key = 0;

                    try
                    {
                        if (reader["nzp_key"] != DBNull.Value) calcfon.nzp_key = (int)reader["nzp_key"];
                        if (reader["nzp"] != DBNull.Value) calcfon.nzp = (int)reader["nzp"];
                        if (reader["nzp_user"] != DBNull.Value) calcfon.nzp_user = (int)reader["nzp_user"];
                        if (reader["nzpt"] != DBNull.Value) calcfon.nzpt = (int)reader["nzpt"];
                        if (reader["task"] != DBNull.Value) calcfon.Task = (int)reader["task"];
                        if (reader["year_"] != DBNull.Value) calcfon.year_ = (int)reader["year_"];
                        if (reader["month_"] != DBNull.Value) calcfon.month_ = (int)reader["month_"];
                        if (reader["dat_when"] != DBNull.Value) calcfon.dat_when = Convert.ToDateTime(reader["dat_when"]).ToString();
                        if (reader["parameters"] != DBNull.Value) calcfon.parameters = Convert.ToString(reader["parameters"]);
                    }
                    catch
                    {
                        return taskExists;
                        //break;
                    }
                    //reader.Close();

                    if (calcfon.year_ > 0 && calcfon.month_ >= 0)
                    {
                        //проверяем можно ли начать выполнение задачи
                        var @continue = CheckBeforeCalcTask(conn_web, calcfon);
                        if (!@continue)
                        {
                            return true;
                        }
                        
                        ret = ExecSQL(conn_web, " Update " + tab + " Set kod_info = 0, dat_work = " + sCurDateTime + " Where nzp_key = " + calcfon.nzp_key, true);
                        if (!ret.result)
                        {
                            return taskExists;
                            //break;
                        }

                        //+++++++++++++++++++++++++++++++++++++++++++
                        //вызов модуля расчета и распределения
                        //+++++++++++++++++++++++++++++++++++++++++++
                        try
                        {
                            CalcProc(calcfon, out ret);
                        }
                        catch (Exception ex)
                        {
                            ret.result = false;
                            ret.text = ex.Message;
                            MonitorLog.WriteLog(ex.Message, MonitorLog.typelog.Error, true);
                        }

                        //задание отработано или ошибка - установить флаг!
                        GetCalcFonStatus(ret, calcfon);

                        var db = new DbCalcQueueClient();
                        ret = db.CloseTask(conn_web, null, calcfon);
                        db.Close();
                        if (!ret.result)
                        {
                            return taskExists;
                        }

                        if (calcfon.callReportAlone)
                        {
                            //calcReport через отдельный поток #0
                            calcfon.TaskType = CalcFonTask.Types.taskCalcReport;
                            calcfon.QueueNumber = 0;
                            calcfon.Status = FonTask.Statuses.New; //установка задания

                            var dbCalcQueue = new DbCalcQueueClient();
                            ret = dbCalcQueue.AddTask(conn_web, null, calcfon);
                            dbCalcQueue.Close();
                        }
                    }
                    else
                    {
                        ret.result = false;
                        calcfon.Status = FonTask.Statuses.Failed;
                        calcfon.txt = "Не задан год и месяц для выполнения задачи!";

                        var db = new DbCalcQueueClient();
                        ret = db.CloseTask(conn_web, null, calcfon);
                        db.Close();
                        if (!ret.result)
                        {
                            return taskExists;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog(ex.Message, MonitorLog.typelog.Error, true);
                return false;
            }
            finally
            {
                if (reader != null)
                {
                    reader.Close();
                    reader = null;
                }
                conn_web.Close();
            }

            return taskExists;
        }

        /// <summary>
        /// Выполняем проверку перед началом выполнения задачи
        /// </summary>
        /// <param name="calcfon"></param>
        /// <returns></returns>
        private bool CheckBeforeCalcTask(IDbConnection conn_web, CalcFonTask calcfon)
        {
            string pref = Points.GetPref(calcfon.nzpt);
            var paramcalc = new CalcTypes.ParamCalc(0, Convert.ToInt32(calcfon.nzp), pref, calcfon.year_,
                calcfon.month_, calcfon.year_, calcfon.month_, calcfon.nzp_user, calcfon.nzp_key, calcfon);

            switch (calcfon.TaskType)
            {
                case CalcFonTask.Types.taskCalcCharge:
                case CalcFonTask.Types.taskCalcGil:
                case CalcFonTask.Types.taskCalcGku:
                case CalcFonTask.Types.taskCalcNedo:
                case CalcFonTask.Types.taskCalcRashod:
                case CalcFonTask.Types.taskFull:
                case CalcFonTask.Types.taskKvar:
                case CalcFonTask.Types.taskSaldo:
                case CalcFonTask.Types.taskWithReval:
                case CalcFonTask.Types.taskWithRevalOntoListHouses:
                    {
                        break;
                    }
                default:
                    return true;
            }

            switch (calcfon.TaskType)
            {
                case CalcFonTask.Types.taskWithRevalOntoListHouses:
                    paramcalc.list_dom = true; break; // признак расчета по списку домов
            }

            var tableLotForCalc = "t_lot_for_calc_" + DateTime.Now.Ticks;
            try
            {
                var ret = GetLotForCalc(conn_web, paramcalc, tableLotForCalc);
                //определяем кол-во лс, которые сейчас рассчитываются
                var count = DBManager.ExecScalar<int>(conn_web, " SELECT COUNT(1) FROM " + tableLotForCalc + " l " +
                                                      " WHERE EXISTS (SELECT 1 FROM " + sDefaultSchema + "CalculatedPersonalAccounts c" +
                                                      "                WHERE l.nzp_kvar=c.personalAccountId " +
                                                      "                AND NOW() BETWEEN DateStart and (DateStart + INTERVAL '3 minute'))",
                    out ret, true);
                if (count > 0)
                {
                    var tab = sDefaultSchema + "calc_fon_" + calcfon.QueueNumber;
                    var text = DbCalcQueueClient.CalcFonComment(conn_web, 0, calcfon.nzpt, paramcalc.nzp_dom < 0 ? 0 : paramcalc.nzp_dom, false) +
                               " Ожидание завершения расчета " + count + " ЛС ";
                    ExecSQL(conn_web, " UPDATE " + tab + " SET txt = " + Utils.EStrNull(text) + " WHERE nzp_key   = " + calcfon.nzp_key, true);
                    return false;
                }
            }
            finally
            {
                ExecSQL(conn_web, " DROP TABLE " + tableLotForCalc, false);
            }

            return true;
        }

        /// <summary>
        /// Получить множество лс для расчета
        /// </summary>
        /// <param name="conn_db"></param>
        /// <param name="paramcalc"></param>
        /// <returns></returns>
        public Returns GetLotForCalc(IDbConnection conn_db, CalcTypes.ParamCalc paramcalc, string TargetTable)
        //--------------------------------------------------------------------------------
        {
            var ssql =
                " CREATE TEMP TABLE " + TargetTable + " " + " (nzp_kvar integer);";
            var ret = ExecSQL(conn_db, ssql, true);
            if (!ret.result) { return ret; }

            #region выборка всех ЛС (открытых/закрытых/неопределенных)
            //+++++++++++++++++++++++++++++++++++++++++++++++++++
            //выбрать множество лицевых счетов
            //+++++++++++++++++++++++++++++++++++++++++++++++++++
            string s_find =
                " From " + paramcalc.data_alias + "kvar k " +
                " Where " + paramcalc.where_z;

            if (paramcalc.nzp_dom > 0 || paramcalc.list_dom)
            {
                if (!paramcalc.list_dom)
                {
                    s_find =
                        " From " + paramcalc.data_alias + "kvar k " +
                        " Where k.nzp_dom=" + paramcalc.nzp_dom;

                    // для расчета связанные дома 
                    var nzp_dom_base = DBManager.ExecScalar<int>(conn_db,
                        " Select max(nzp_dom_base) From " + paramcalc.data_alias + "link_dom_lit p " +
                        " Where nzp_dom=" + paramcalc.nzp_dom + " ", out ret, true);
                    if (nzp_dom_base > 0)
                    {
                        s_find =
                            " From " + paramcalc.data_alias + "kvar k, " + paramcalc.data_alias + "link_dom_lit l " +
                            " Where k.nzp_dom=l.nzp_dom and l.nzp_dom_base=" + nzp_dom_base;
                    }
                }
                else //расчет по списку домов, в которых оказались связанные дома
                {
                    //список базовых домов
                    ExecSQL(conn_db, "drop table t_list_base_houses", false);
                    var sql = "create temp table t_list_base_houses (nzp_dom integer)";
                    ret = ExecSQL(conn_db, sql, true);
                    if (!ret.result) { return ret; }

                    //необходимый список домов 
                    ExecSQL(conn_db, "drop table t_list_need_houses", false);
                    sql = "create temp table t_list_need_houses (nzp_dom integer)";
                    ret = ExecSQL(conn_db, sql, true);
                    if (!ret.result) { return ret; }

                    //получили список базовых домов
                    sql = " insert into t_list_base_houses (nzp_dom ) " +
                          " select l.nzp_dom_base from " + paramcalc.data_alias + "link_dom_lit l where " +
                          paramcalc.where_z + " group by 1";
                    ret = ExecSQL(conn_db, sql, true);
                    if (!ret.result) { return ret; }

                    //записали в список домов связанные дома 
                    sql = " insert into t_list_need_houses (nzp_dom) " +
                          " select nzp_dom from " + paramcalc.data_alias + "link_dom_lit " +
                          " where nzp_dom_base in (select nzp_dom from t_list_base_houses)";
                    ret = ExecSQL(conn_db, sql, true);
                    if (!ret.result) { return ret; }

                    //записали выбранные дома
                    sql = " insert into t_list_need_houses (nzp_dom) " +
                          " select nzp_dom from " + paramcalc.data_alias + "dom " +
                          " where " + paramcalc.where_z;
                    ret = ExecSQL(conn_db, sql, true);
                    if (!ret.result) { return ret; }

                    //конечный список домов 
                    ExecSQL(conn_db, "drop table t_list_final_houses", false);
                    sql = "create temp table t_list_final_houses (nzp_dom integer)";
                    ret = ExecSQL(conn_db, sql, true);
                    if (!ret.result) { return ret; }

                    //получили окончательный список
                    sql = " insert into t_list_final_houses (nzp_dom) " +
                          " select nzp_dom from t_list_need_houses group by 1";
                    ret = ExecSQL(conn_db, sql, true);
                    if (!ret.result) { return ret; }

                    ExecSQL(conn_db, " Create unique index ix1_t_list_final_houses on t_list_final_houses (nzp_dom) ", false);

                    s_find = " From " + paramcalc.data_alias + "kvar k, t_list_final_houses l " +
                       " Where k.nzp_dom=l.nzp_dom ";
                }
            }

            ret = ExecSQL(conn_db,
                " Insert into " + TargetTable + " (nzp_kvar) " +
                " Select DISTINCT k.nzp_kvar " + s_find
                , true);
            if (!ret.result) { return ret; }
            ret = ExecSQL(conn_db, " Create unique index ix1_" + TargetTable + " on " + TargetTable + " (nzp_kvar) ", true);
            #endregion выборка всех ЛС (открытых/закрытых/неопределенных) - t_selkvar

            return ret;
        }


        private void GetCalcFonStatus(Returns ret, CalcFonTask calcfon)
        {
            if (!ret.result)
            {
                calcfon.Status = FonTask.Statuses.Failed;
                calcfon.txt = "Ошибка выполнения! Смотрите журнал ошибок.";
                if (ret.tag == -1)
                {
                    calcfon.txt = ret.text;
                }
            }
            else
            {
                if ((calcfon.TaskType == CalcFonTask.Types.taskWithReval
                     || calcfon.TaskType == CalcFonTask.Types.taskWithRevalOntoListHouses)
                    && ret.tag == (int)FonTask.Statuses.WithErrors)
                {
                    calcfon.Status = FonTask.Statuses.WithErrors;
                    calcfon.txt =
                        " При расчете выявлены ЛС с большими показаниями ПУ," +
                        " их перечень находится в группе лицевых счетов " +
                        "\"П-большие расходы ПУ полученные при расчете\"";
                }
                else
                {
                    calcfon.Status = FonTask.Statuses.Completed;
                }
            }
        }

        //--------------------------------------------------------------------------------
        //определение calc_fon_xx и установка задание на выполнение
        //--------------------------------------------------------------------------------
        public void CalcOnFon(int _nzp_dom, string pref/*, int calc_yy, int calc_mm, int cur_yy, int cur_mm*/, bool reval, out Returns ret)
        //--------------------------------------------------------------------------------
        {
            CalcFonTask.Types task = CalcFonTask.Types.taskFull;
            if (reval)
                task = CalcFonTask.Types.taskWithReval;

            CalcOnFon(_nzp_dom, pref/*, calc_yy, calc_mm, cur_yy, cur_mm*/, task, out ret);
        }

        public void CalcOnFon(int _nzp_dom, string pref /*, int calc_yy, int calc_mm, int cur_yy, int cur_mm*/,
            CalcFonTask.Types task, out Returns ret)
        {

            CalcOnFon(_nzp_dom, pref, 0, task, out ret);
        }

        public void CalcOnFon(int _nzp_dom, string pref, int nzp_user, CalcFonTask.Types task, out Returns ret)
        {
            ret = Utils.InitReturns();

            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result)
            {
                return;
            }

            CalcFonTask calcfon = new CalcFonTask();
            calcfon.nzp = _nzp_dom;
            //calcfon.yy      = calc_yy;
            //calcfon.mm      = calc_mm;
            //calcfon.cur_yy  = cur_yy; //надо явно подставить тек. расчетный месяц, что случайно не испортить данные через базу!!!!
            //calcfon.cur_mm  = cur_mm;
            calcfon.TaskType = task;
            /*
            calcfon.yy      = 2012;
            calcfon.mm      = 2;
            calcfon.cur_yy  = 2012;
            calcfon.cur_mm  = 2;
            */

            int numTot = 0;
            int numSuccess = 0;

            if (true)
            {
                //сначала проверим, что нет ли выполняемых заданий
                //calcfon.status = FonTask.Statuses.InProcess; //контроль

                //foreach (_Point zap in Points.PointList)
                //{
                //    if (pref != "AllBases")
                //    {
                //        if (zap.pref != pref) continue;
                //        RecordMonth rm = Points.GetCalcMonth(new CalcMonthParams(zap.pref))
                //    }

                //    calcfon.nzpt   = zap.nzp_wp;
                //    calcfon.number = Points.GetCalcNum(zap.nzp_wp); //определить номер потока расчета

                //    CheckCalcAndPutInFon(conn_web, calcfon, out ret);
                //    if (!ret.result || ret.tag == Constants.workinfon)
                //    {
                //        //ошибка, либо есть задания в расчете
                //        conn_web.Close();
                //        return;
                //    }
                //}

                //затем выставим задание на расчет
                calcfon.Status = FonTask.Statuses.New; //на расчет

                DbAdminClient dba = new DbAdminClient();

                try
                {
                    foreach (_Point zap in Points.PointList)
                    {
                        numTot++;

                        if (pref != "AllBases")
                        {
                            if (zap.pref != pref) continue;
                        }

                        bool allow = dba.IsAllowCalcByPref(zap.pref, out ret);

                        if (!ret.result) return;

                        if (!allow)
                        {
                            MonitorLog.WriteLog("Задание на расчет начислений для банка данных " + pref + " не добавлено. Операция не разрешена.", MonitorLog.typelog.Warn, true);
                            continue;
                        }

                        RecordMonth rm = Points.GetCalcMonth(new CalcMonthParams(zap.pref));
                        calcfon.year_ = rm.year_;
                        calcfon.month_ = rm.month_;
                        calcfon.nzpt = zap.nzp_wp;
                        calcfon.QueueNumber = Points.GetCalcNum(zap.nzp_wp); //определить номер потока расчета
                        calcfon.nzp_user = nzp_user;

                        var db = new DbCalcQueueClient();
                        ret = db.AddTask(conn_web, null, calcfon);
                        db.Close();

                        if (!ret.result)
                        {
                            if (ret.tag == Constants.workinfon)
                            {
                                continue;
                            }
                            else
                            {
                                return;
                            }
                        }
                        numSuccess++;
                    }
                }
                catch (Exception ex)
                {
                    MonitorLog.WriteLog("Ошибка CalcChargeDom " + ex.Message, MonitorLog.typelog.Error, 30, 301, true);
                }
                finally
                {
                    conn_web.Close();
                    dba.Close();
                }
            }
            else
            {
                //пока прямой расчет!
                //TestCalc0(_nzp_dom, pref, calc_yy, calc_mm, cur_yy, cur_mm, clc, out ret);

            }

            if (ret.result)
            {
                if (pref == "AllBases") ret.text = "Добавлено заданий на расчет начислений по " + numSuccess + " из " + numTot + " банков данных";
                else if (numSuccess > 0) ret.text = "Задание на расчет начислений добавлено";
                else ret.text = "Задание на расчет не добавлено";
                ret.tag = CalcFonTask.Types.taskWithRevalOntoListHouses == task ? ret.tag : -1;
            }
        }

        #endregion Фоновые задачи



        #region расчет всей базы или дома или квартиры


        /// <summary>
        /// Функция получает наименование префикса таблиц для тестового расчета
        /// </summary>
        /// <param name="parameters">поле parametrs из calc_fon_x</param>
        /// <returns></returns>
        public static string GetTestCalcPref(string parameters)
        {

            if (String.IsNullOrEmpty(parameters)) return String.Empty;
            if (parameters.IndexOf("testCalcPref", StringComparison.Ordinal) < 0)
                return String.Empty;

            string result = parameters.Substring(
                parameters.IndexOf("testCalcPref", StringComparison.Ordinal) +
                ("testCalcPref\":\"").Length);

            return result.Length > 0 ?
                    result.Substring(0, result.IndexOf("\"",
                    StringComparison.Ordinal)) : String.Empty;

        }



        /// <summary>
        ///  Анализирует тип задачи и вызывает соответствующий обработчик,
        ///  задачи берутся из таблицы calc_fon_
        /// </summary>
        /// <param name="calcfon"> задача из calc_fon</param>
        /// <param name="ret"></param>
        public void CalcProc(CalcFonTask calcfon, out Returns ret)
        {
            IDataBaseCommon idb = null;
            ret = Utils.InitReturns();
            MonitorLog.WriteLog("Старт обработки фоновой задачи:" + calcfon, MonitorLog.typelog.Info, true);

            try
            {
                if (calcfon.TaskType == CalcFonTask.Types.taskKvar)
                {
                    //+++++++++++++++++++++++++++++++++++++++++++++++++++
                    //расчет одного лицевого счета через фоновую задачу
                    //+++++++++++++++++++++++++++++++++++++++++++++++++++

                    CalcLs(calcfon, out ret);
                    return;
                }
                else if (calcfon.TaskType == CalcFonTask.Types.UpdatePackStatus)
                {
                    DbPack dbPack = new DbPack();
                    PackFinder finder = new PackFinder();
                    finder.nzp_user = 1;
                    finder.year_ = calcfon.year_;
                    finder.nzp_pack = Convert.ToInt32(calcfon.nzp);

                    ReturnsType ret2 = dbPack.Upd_SUM_RASP_and_SUM_NRASP(finder);
                    ret.result = ret2.result;
                    ret.sql_error = ret2.sql_error;
                    ret.text = ret2.text;
                    ret.tag = ret2.tag;
                    dbPack.Close();
                    return;
                }
                else if (calcfon.TaskType == CalcFonTask.Types.DistributeOneLs)
                {
                    DbCalcPack db = new DbCalcPack();
                    db.CalcDistribPackLs(calcfon, out ret);
                    db.Close();
                    return;
                }

                else if (calcfon.TaskType == CalcFonTask.Types.taskCalcChargeForDelReestr)
                {
                    DbCharge db = new DbCharge();
                    ParamsForGroupPerekidki finder = new ParamsForGroupPerekidki();
                    finder.nzp_reestr = Convert.ToInt32(calcfon.nzp);
                    finder.nzp_user = 1;
                    finder.dat_uchet = (new DateTime(calcfon.year_, calcfon.month_, 1)).ToShortDateString();
                    ret = db.DeleteFromReestrPerekidok(finder);
                    db.Close();
                    return;
                }

                else if (calcfon.TaskType == CalcFonTask.Types.uchetOplatArea)
                {
                    IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
                    ret = OpenDb(conn_db, true);
                    if (!ret.result) return;

                    Charge charge = new Charge();
                    charge.nzp_area = Convert.ToInt32(calcfon.nzp);
                    charge.year_ = calcfon.year_;
                    charge.month_ = calcfon.month_;

                    using (DbCalcQueueClient dbc = new DbCalcQueueClient(calcfon))
                    {
                        using (DbCalcCharge db = new DbCalcCharge())
                        {
                            db.CalcChargeXXUchetOplatForArea(conn_db, null, charge, dbc.SetTaskProgress);
                        }
                    }

                    conn_db.Close();
                    return;
                }

                else if (calcfon.TaskType == CalcFonTask.Types.taskBalanceSelect)
                {
                    var finder = JsonConvert.DeserializeObject<OverPaymentsParams>(calcfon.parameters);
                    using (DbCalcQueueClient dbc = new DbCalcQueueClient(calcfon))
                    {
                        using (var db = new DbCalcPack())
                        {
                            ret = db.InsertOverPayments(finder, dbc.SetTaskProgress);
                        }
                    }
                    return;
                }
                else if (calcfon.TaskType == CalcFonTask.Types.taskBalanceRedistr)
                {
                    var finder = JsonConvert.DeserializeObject<DistrOverPaymentsParams>(calcfon.parameters);
                    using (DbCalcQueueClient dbc1 = new DbCalcQueueClient(calcfon))
                    {
                        using (var dbc = new DbCalcPack())
                        {
                            ret = dbc.DistribOverPayments(finder, dbc1.SetTaskProgress);
                            if (ret.result)
                            {
                                ret = dbc.GetCurOverPaymentsProcId();
                                if (!ret.result)
                                    MonitorLog.WriteLog("Ошибка проставления статуса calcfon.TaskType == " +
                                                        "CalcFonTask.Types.taskBalanceRedistr " + ret.text,
                                        MonitorLog.typelog.Error, true);
                                else
                                {
                                    var finderFon = new OverpaymentStatusFinder
                                    {
                                        id = ret.tag,
                                        nzp_status = (int) OverpaymentStatusFinder.Statuses.overpDistrib,
                                        nzp_fon_distrib = calcfon.nzp_key,
                                        nzp_user = finder.nzp_user,
                                        is_actual = 100
                                    };
                                    ret = dbc.SetStatusOverpaymentManProc(finderFon);
                                }
                            }
                        }
                    }
                    return;
                }
                else if (calcfon.TaskType == CalcFonTask.Types.taskCalcChargeForReestr)
                {
                    IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
                    ret = OpenDb(conn_db, true);
                    if (!ret.result) return;

                    ParamsForGroupPerekidki reestr = new ParamsForGroupPerekidki();
                    reestr.nzp_reestr = Convert.ToInt32(calcfon.nzp);
                    reestr.dat_uchet = (new DateTime(calcfon.year_, calcfon.month_, 1)).ToShortDateString();

                    using (DbCalcQueueClient dbc = new DbCalcQueueClient(calcfon))
                    {
                        using (DbCalcCharge db = new DbCalcCharge())
                        {
                            db.CalcChargeXXForReestrPerekidok(conn_db, null, reestr, dbc.SetTaskProgress);
                        }
                    }

                    conn_db.Close();
                    return;
                }

                else if (calcfon.TaskType == CalcFonTask.Types.uchetOplatBank)
                {
                    IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
                    ret = OpenDb(conn_db, true);
                    if (!ret.result) return;
                    Pack p = new Pack();
                    if (calcfon.nzp > 0)
                        p.par_pack = calcfon.nzp;
                    using (DbCalcCharge db = new DbCalcCharge())
                    {
                        db.CalcChargeXXUchetOplatForBank(conn_db, Points.GetPref(calcfon.nzpt), p);
                    }

                    conn_db.Close();
                    return;
                }

                else if (calcfon.TaskType == CalcFonTask.Types.ReCalcKomiss)
                {
                    IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
                    ret = OpenDb(conn_db, true);
                    if (!ret.result) return;

                    DateTime dt = new DateTime(calcfon.year_, calcfon.month_, calcfon.nzpt);

                    DbCalcPack db = new DbCalcPack();
                    DbCalcQueueClient dbc = new DbCalcQueueClient(calcfon);
                    try
                    {
                        db.RecalcUderFndistrib(conn_db, dt, out ret, dbc.SetTaskProgress);
                    }
                    catch (Exception ex)
                    {
                        throw ex;
                    }
                    finally
                    {
                        dbc.Close();
                        db.Close();
                    }

                    conn_db.Close();
                    return;
                }

                else if (calcfon.TaskType == CalcFonTask.Types.taskGetFakturaWeb)
                {
                    ret = Utils.InitReturns();
                    IDbConnection con_db = GetConnection(Constants.cons_Kernel);

                    try
                    {
                        ret = OpenDb(con_db, true);
                        IDataReader reader = null;
                        string strWBFDatabase = "";
#if PG
                        //string strSQLQuery = String.Format("SELECT (TRIM({0}_kernel.s_baselist.dbname) || '.session_ls') AS wbf_set search_path to 'FROM {0}_kernel.s_baselist WHERE {0}_kernel.s_baselist.idtype = 8 limit 1';", Points.Pref);
                        string strSQLQuery = String.Format(" SELECT  TRIM (dbname)|| '.session_ls' AS wbf_database  " +
                                                           " FROM {0}_kernel.s_baselist " +
                                                           " WHERE {0}_kernel.s_baselist.idtype = 8 ;", Points.Pref);
#else
                            string strSQLQuery = String.Format("SELECT FIRST 1 (TRIM({0}_kernel:s_baselist.dbname) || '@' || TRIM({0}_kernel:s_baselist.dbserver) || ':session_ls') AS wbf_database FROM {0}_kernel:s_baselist WHERE {0}_kernel:s_baselist.idtype = 8;", Points.Pref);
#endif
                        ret = ExecRead(con_db, out reader, strSQLQuery, true);
                        if (!ret.result)
                        {
                            MonitorLog.WriteLog("Ошибка выполнения запроса", MonitorLog.typelog.Error, true);
                            ret.result = false;
                            ret.text = " Ошибка выполнения запроса. ";
                            ret.tag = -1;
                            throw new Exception();
                        }
                        while (reader.Read())
                            if (reader["wbf_database"] != DBNull.Value)
                            {
#if PG
                                strWBFDatabase = reader["wbf_database"].ToString().Replace("@.", ".");
#else
                                    strWBFDatabase = reader["wbf_database"].ToString().Replace("@:", ":");
#endif
                                break;
                            }

                        strSQLQuery = String.Format(
#if PG
"SELECT {0}.pref AS wbf_pref, {0}.id_bill AS wbf_id_bill, {0}.dat_when AS wbf_dat_when, {0}.cur_oper AS wbf_cur_oper, {1}_data.kvar.nzp_kvar AS dat_nzp_kvar, {1}_data.kvar.nzp_dom AS dat_nzp_dom, {1}_data.kvar.pkod AS dat_pkod, {1}_kernel.s_listfactura.kind AS krn_kind, 301 AS sys_nzp_user FROM {0}, {1}_data.kvar, {1}_kernel.s_listfactura WHERE {0}.nzp_session = {2} AND {0}.num_ls = {1}_data.kvar.num_ls AND {1}_kernel.s_listfactura.default_ = 1 limit 1;"
#else
"SELECT FIRST 1 {0}.pref AS wbf_pref, {0}.id_bill AS wbf_id_bill, {0}.dat_when AS wbf_dat_when, {0}.cur_oper AS wbf_cur_oper, {1}_data:kvar.nzp_kvar AS dat_nzp_kvar, {1}_data:kvar.nzp_dom AS dat_nzp_dom, {1}_data:kvar.pkod AS dat_pkod, {1}_kernel:s_listfactura.kind AS krn_kind, 301 AS sys_nzp_user FROM {0}, {1}_data:kvar, {1}_kernel:s_listfactura WHERE {0}.nzp_session = {2} AND {0}.num_ls = {1}_data:kvar.num_ls AND {1}_kernel:s_listfactura.default_ = 1;"
#endif
, strWBFDatabase, Points.Pref, calcfon.nzpt);
                        ret = ExecRead(con_db, out reader, strSQLQuery, true);
                        if (!ret.result)
                        {
                            MonitorLog.WriteLog("Ошибка выполнения запроса", MonitorLog.typelog.Error, true);
                            ret.result = false;
                            ret.text = " Ошибка выполнения запроса. ";
                            ret.tag = -1;
                            throw new Exception();
                        }

                        string strWBFPref = "";
                        int intWBFIdBill = 0;
                        DateTime dtWBFDateWhen = new DateTime();
                        int intWBFCurOper = 0;
                        int intDATNzpKvar = 0;
                        int intDATNzpDom = 0;
                        long longDATPkod = 0;
                        int intKRNKind = 0;
                        int intSYSNzpUser = 0;

                        Utils.setCulture();
                        while (reader.Read())
                        {
                            if (reader["wbf_pref"] != DBNull.Value) strWBFPref = reader["wbf_pref"].ToString();
                            if (reader["wbf_id_bill"] != DBNull.Value)
                                intWBFIdBill = Convert.ToInt32(reader["wbf_id_bill"]);
                            if (reader["wbf_dat_when"] != DBNull.Value)
                                dtWBFDateWhen = Convert.ToDateTime(reader["wbf_dat_when"]);
                            if (reader["wbf_cur_oper"] != DBNull.Value)
                                intWBFCurOper = Convert.ToInt32(reader["wbf_cur_oper"]);
                            if (reader["dat_nzp_kvar"] != DBNull.Value)
                                intDATNzpKvar = Convert.ToInt32(reader["dat_nzp_kvar"]);
                            if (reader["dat_nzp_dom"] != DBNull.Value)
                                intDATNzpDom = Convert.ToInt32(reader["dat_nzp_dom"]);
                            if (reader["dat_pkod"] != DBNull.Value) longDATPkod = Convert.ToInt64(reader["dat_pkod"]);
                            if (reader["krn_kind"] != DBNull.Value) intKRNKind = Convert.ToInt32(reader["krn_kind"]);
                            if (reader["sys_nzp_user"] != DBNull.Value)
                                intSYSNzpUser = Convert.ToInt32(reader["sys_nzp_user"]);
                        }

                        Faktura finder = new Faktura();
                        finder.pref = strWBFPref;
                        finder.nzp_kvar = intDATNzpKvar;
                        finder.nzp_dom = intDATNzpDom;
                        finder.idFaktura = intKRNKind;
                        finder.nzp_user = intSYSNzpUser;
                        finder.workRegim = Faktura.WorkFakturaRegims.Web;
                        finder.withDolg = true;
                        finder.month_ = calcfon.month_;
                        finder.year_ = calcfon.year_;
                        finder.resultFileType = Faktura.FakturaFileTypes.PDF;
                        finder.destFileName = String.Format("{0}_{1}{2}_{3}", longDATPkod,
                            calcfon.year_.ToString("0000"), calcfon.month_.ToString("00"), intWBFIdBill);

                        DbFaktura dbFaktura = new DbFaktura();
                        List<string> fName = dbFaktura.GetFaktura(finder, out ret);

                        strSQLQuery =
                              String.Format(
                                  "UPDATE {0} SET cur_oper = {1}, erc12 = '{2}', erc28 = '{3}', pdf_filename = '{4}' WHERE nzp_session = {5};",
                                  strWBFDatabase, ret.result ? "34" : "-5", ret.result ? ret.text.Split('|')[0] : "",
                                  ret.result ? ret.text.Split('|')[1] : "", Path.GetFileName(fName.FirstOrDefault()) ?? "", calcfon.nzpt);
                        ret = ExecSQL(con_db, strSQLQuery, true);
                        if (!ret.result)
                        {
                            MonitorLog.WriteLog("Ошибка выполнения taskGetFakturaWeb", MonitorLog.typelog.Error, true);
                            ret.result = false;
                            ret.text = " Ошибка выполнения taskGetFakturaWeb. ";
                            ret.tag = -1;
                            throw new Exception();
                        }
                        else
                        {
                            ret.result = true;
                            ret.tag = 0;
                        }
                    }
                    catch (Exception ex)
                    {
                        MonitorLog.WriteLog("Ошибка выполнения процедуры GetFakturaWeb : " + ex.Message,
                            MonitorLog.typelog.Error, true);
                        ret.result = false;
                        ret.tag = -1;
                    }
                    finally
                    {
                        con_db.Close();
                    }

                    return;
                }
                else if (calcfon.TaskType == CalcFonTask.Types.taskToTransfer)
                {
                    var finder = new TransferBalanceFinder();
                    finder = JsonConvert.DeserializeObject<TransferBalanceFinder>(calcfon.parameters);

                    DateTime dat_s = Convert.ToDateTime(finder.dat_s);
                    DateTime dat_po = Convert.ToDateTime(finder.dat_po); ;

                    DbCalcPack dbc = new DbCalcPack();
                    DbCalcQueueClient dbc2 = new DbCalcQueueClient(calcfon);
                    try
                    {
                        dbc.DistribPaXX_1(finder, out ret, dbc2.SetTaskProgress);
                    }
                    catch (Exception Ex)
                    {
                        throw Ex;
                    }
                    finally
                    {
                        dbc2.Close();
                        dbc.Close();
                    }
                    return;
                }
                //Генерация платежного кода
                else if (calcfon.TaskType == CalcFonTask.Types.taskGeneratePkod)
                {
                    DbAdres db = new DbAdres();

                    if (GlobalSettings.NewGeneratePkodMode)
                    {
                        Finder finder = new Finder();
                        finder.nzp_user = calcfon.nzp_user;
                        finder.dopFind = new List<string>();
                        finder.dopFind.Add(calcfon.QueueNumber + calcfon.nzp_key.ToString());
                        ret = db.NewGeneratePkod(finder);
                    }
                    else
                    {
                        ret = db.GeneratePkodFon(new Finder() { nzp_user = calcfon.nzp_user });
                    }
                    IDbConnection conndb = GetConnection(Constants.cons_Kernel);
                    ret = OpenDb(conndb, true);
                    if (!ret.result) return;

                    ExecSQL(conndb, "drop table public.t" + calcfon.QueueNumber + calcfon.nzp_key, false);
                    conndb.Close();
                    db.Close();
                    return;
                }

                string pref = Points.GetPref(calcfon.nzpt);
                CalcTypes.ParamCalc paramcalc = new CalcTypes.ParamCalc(0, Convert.ToInt32(calcfon.nzp), pref, calcfon.year_,
                    calcfon.month_, calcfon.year_, calcfon.month_, calcfon.nzp_user, calcfon.nzp_key, calcfon);

                #region Заполняем параметр для paramcalc для тестового расчета
                //Сергей 16.12.2014
                paramcalc.id_bill_pref = GetTestCalcPref(calcfon.parameters);

                #endregion





                if (calcfon.TaskType == CalcFonTask.Types.CancelDistributionAndDeletePack ||
                    calcfon.TaskType == CalcFonTask.Types.CancelPackDistribution ||
                    calcfon.TaskType == CalcFonTask.Types.DistributePack)
                {
                    IDataReader reader;
                    string kvar = Points.Pref + "_data" + tableDelimiter + "kvar ";
                    int yy = Points.CalcMonth.year_;
                    string pack_ls = Points.Pref + "_fin_" + (yy - 2000).ToString("00") + tableDelimiter + "pack_ls ";

                    IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
                    ret = OpenDb(conn_db, true);
                    if (!ret.result) return;

                    ret = ExecRead(conn_db, out reader,
                        " Select pref, count(*) cnt From " + kvar +
                        " Where num_ls in ( Select num_ls From " + pack_ls + " Where nzp_pack = " + calcfon.nzp + ") group by 1"
                        , true);

                    if (!ret.result)
                    {
                        conn_db.Close();
                        return;
                    }

                    List<string> list = new List<string>();
                    List<int> cnt = new List<int>();
                    int N = 0;
                    try
                    {
                        while (reader.Read())
                        {
                            if (reader["pref"] != DBNull.Value) list.Add((string) reader["pref"]);
                            if (reader["cnt"] != DBNull.Value) cnt.Add(Convert.ToInt32(reader["cnt"]));
                            N += Convert.ToInt32(reader["cnt"]);
                        }
                    }
                    catch (Exception ex)
                    {
                        reader.Close();
                        conn_db.Close();
                        ret.result = false;
                        ret.text = ex.Message;
                        MonitorLog.WriteLog(ret.text, MonitorLog.typelog.Error, 1, 222, true);
                        return;
                    }

                    //reader.Close();

                    using (DbCalcQueueClient dbc = new DbCalcQueueClient(calcfon))
                    {
                        if (list.Count > 0)
                        {
                            int n = 0;
                            for (int i = 0; i < list.Count; i++)
                            {
                                foreach (_Point zap in Points.PointList)
                                {
                                    string prf = list[i];
                                    if (zap.pref != prf.Trim()) continue;

                                    calcfon.nzpt = zap.nzp_wp;
                                    paramcalc.pref = prf.Trim();


                                    try
                                    {
                                        ret = list.Count > 1
                                            ? Dop(calcfon, paramcalc, null)
                                            : Dop(calcfon, paramcalc, dbc.SetTaskProgress);
                                    }
                                    catch (Exception ex)
                                    {
                                        throw ex;
                                    }
                                    if (list.Count > 1)
                                    {
                                        n += cnt[i];
                                        if (N > 0)
                                            dbc.SetTaskProgress(calcfon.QueueNumber, calcfon.nzp_key, ((decimal) n)/N);
                                    }
                                    break;
                                }
                            }
                        }
                    }


                    Returns rets = ExecRead(conn_db, out reader,
                        " Select * From " + pack_ls + " Where nzp_pack = " + calcfon.nzp + " and (num_ls is null or num_ls = 0)", true);

                    if (!rets.result)
                    {
                        conn_db.Close();
                        return;
                    }
                    bool is_Exist = false;
                    if (reader.Read()) is_Exist = true;
                    reader.Close();
                    conn_db.Close();

                    if (list.Count == 0 || is_Exist)
                    {
                        calcfon.nzpt = 0;
                        paramcalc.pref = "";

                        DbCalcQueueClient dbc2 = new DbCalcQueueClient(calcfon);
                        try
                        {
                            ret = Dop(calcfon, paramcalc, dbc2.SetTaskProgress);
                        }
                        catch (Exception ex)
                        {
                            throw ex;
                        }
                        finally
                        {
                            dbc2.Close();
                        }

                    }


                    if (!ret.result ||
                        calcfon.TaskType == CalcFonTask.Types.CancelPackDistribution ||
                        calcfon.TaskType == CalcFonTask.Types.DistributePack)
                    {
                        Returns ret2;
                        calcfon.TaskType = CalcFonTask.Types.UpdatePackStatus;
                        CalcProc(calcfon, out ret2);
                    }

                }
                else if (calcfon.TaskType == CalcFonTask.Types.taskPreparePrintInvoices)
                {
                    //выполнить контрольные проверки
                    DbCharge dbCharge = new DbCharge();
                    ReturnsType retType = dbCharge.MakeChecksBeforeCloseCalcMonth(new Finder { nzp_user = calcfon.nzp_user }, new List<string>() { pref }, calcfon);
                    dbCharge.Close();
                    // retType.result = false; 
                    if (!retType.result)
                    {
                        #region запись ошибок в текстовый файл
                        StringBuilder sql = new StringBuilder();
                        IDbConnection conn_db1 = GetConnection(Constants.cons_Kernel);
                        string fileName = Constants.Directories.ReportDir + "//Протокол_ошибок_" +
                                          DateTime.Now.ToShortDateString() + "_" + DateTime.Now.Ticks;
                        //постановка на поток
                        ExcelRepClient excelRep = new ExcelRepClient();
                        ret = excelRep.AddMyFile(new ExcelUtility()
                        {
                            nzp_user = calcfon.nzp_user,
                            status = ExcelUtility.Statuses.InProcess,
                            rep_name = "Протокол ошибок от " + DateTime.Now.ToShortDateString()
                        });
                        if (!ret.result) return;
                        var nzp_exc = ret.tag;

                        ret = OpenDb(conn_db1, true);
                        if (!ret.result) return;

                        MyDataReader reader;
                        string ErrorText = "";
                        sql.Append(" select dat_check, note, name_prov from ");
                        sql.Append(Points.Pref + DBManager.sDataAliasRest + "checkchmon c ");
                        sql.Append(" where c.status_=2 and c.dat_check='" + DateTime.Now.ToShortDateString() + "'");
                        ret = ExecRead(conn_db1, out reader, sql.ToString(), true);
                        if (!ret.result) return;

                        while (reader.Read())
                        {
                            ErrorText += "Дата:" + reader["dat_check"].ToString().ToLower().Trim() + "  Текст ошибки:" +
                                         reader["note"].ToString().ToLower().Trim() + ".  " +
                                         reader["name_prov"].ToString().ToLower().Trim() + ".  Название проверки" +
                                         Environment.NewLine;
                        }
                        conn_db1.Close();
                        StreamWriter sw = File.CreateText(fileName + ".txt");
                        sw.Write(ErrorText);
                        sw.Flush();
                        sw.Close();


                        //смена статуса
                        IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
                        ret = OpenDb(conn_web, true);
                        if (!ret.result) return;


                        sql = new StringBuilder();
                        sql.Append(" update " + sDefaultSchema + "excel_utility set stats = " + (int)ExcelUtility.Statuses.Success);
                        sql.Append(", dat_out = " + Utils.EStrNull(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")));
                        if (fileName != "") sql.Append(", exc_path = '" + fileName + "'");
                        sql.Append(" where nzp_exc =" + nzp_exc);
                        ret = ExecSQL(conn_web, sql.ToString(), true);
                        conn_web.Close();


                        ret.result = retType.result;
                        ret.text = retType.text;
                        ret = excelRep.SetMyFileState(new ExcelUtility()
                        {
                            nzp_exc = nzp_exc,
                            status = ExcelUtility.Statuses.Success,
                            exc_path = fileName,
                            nzp_user = calcfon.nzp_user,

                            rep_name = "Протокол ошибок от " + DateTime.Now.ToShortDateString()
                        });


                        //отметка об ошибочном выполнении вцелом
                        ret.result = retType.result;
                        //ret.text = ret.text;
                        return;
                        #endregion
                    }

                    //выполняется подготовка данных
                    bool fPrepareData = calcfon.nzp == 1 ? true : false;
                    if (fPrepareData)
                    {
                        ret = this.PrepaeDataForPrintInvoces(pref, calcfon.year_, calcfon.month_);
                        if (!ret.result)
                        {
                            return;
                        }
                        }

                    var finder = new Finder { nzp_user = calcfon.nzp_user, pref = pref, nzp_wp = calcfon.nzp_wp };
                    //передвинуть расчетный месяц банка
                    IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
                    ret = OpenDb(conn_db, true);
                    if (!ret.result) return;
                    dbCharge = new DbCharge();
                    retType = dbCharge.CloseCalcMonth_actions(conn_db, pref, new Finder());
                    if (!retType.result)
                    {
                        ret.result = retType.result;
                        ret.text = retType.text;
                        return;
                    }
                    dbCharge.Close();
                    conn_db.Close();

                    //обновить список локальных банков
                    DbSprav dbSprav = new DbSprav();
                    bool res = dbSprav.PointLoad(GlobalSettings.WorkOnlyWithCentralBank, out ret);
                    dbSprav.Close();
                    try
                    {
                        using (var db = new DbAdmin())
                        {
                           
                            finder.nzp_wp = Points.PointList.Where(x => x.pref == finder.pref).Select(x => x.nzp_wp).FirstOrDefault();
                            //запись проводок
                            var retprov = db.GetProvForClosedMonth(finder);
                            if (!retprov.result)
                            {
                                MonitorLog.WriteLog("Ошибка записи проводок по банку данных:" + finder.pref, MonitorLog.typelog.Error, true);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MonitorLog.WriteLog("Ошибка записи проводок по банку данных:" + pref, MonitorLog.typelog.Error, true);
                        MonitorLog.WriteLog("Ошибка записи проводок по банку данных:" + ex.Message, MonitorLog.typelog.Error, true);
                    }
                    #region Добавление задачи на расчет
                    Returns ret2;
                    CalcOnFon(0, pref, true, out ret2);

                    //if (ret2.result)
                    //{
                    //    try
                    //    {
                    //        using (var admin = new DbAdmin())
                    //        {
                    //            admin.InsertSysEvent(new SysEvents()
                    //            {
                    //                pref = pref,
                    //                nzp_user = calcfon.nzp_user,
                    //                nzp_dict = 6594,
                    //                nzp_obj = 0,
                    //                note = "Добавлена задача на расчет начислений банка данных " + Points.GetPoint(pref).point
                    //            });
                    //        }
                    //    }
                    //    catch (Exception ex)
                    //    {
                    //        MonitorLog.WriteLog(ex.Message, MonitorLog.typelog.Error, true);
                    //    }
                    //}
                    #endregion
                }

                    //обработчик задачи разбора
                else if (calcfon.TaskType == CalcFonTask.Types.taskDisassembleFile)
                {
                    ret = Utils.InitReturns();
                    var finder = new FilesDisassemble();

                    #region Подключение к БД
                    IDbConnection con_db = DBManager.GetConnection(Global.Constants.cons_Kernel);
                    ret = DBManager.OpenDb(con_db, true);
                    if (!ret.result)
                    {
                        MonitorLog.WriteLog("Ошибка открытия соединения в ф-ции CalcProc " +
                                            "при  при обработке задачи разбора файла (taskDisassembleFile)",
                            MonitorLog.typelog.Error, true);
                        return;
                    }
                    #endregion Подключение к БД

                    try
                    {
                        finder = JsonConvert.DeserializeObject<FilesDisassemble>(calcfon.parameters);
                        DbDisassembleFile disFile = new DbDisassembleFile(con_db);
                        ret = Utils.InitReturns();
                        ret = disFile.SelectDissMethod(finder, ref ret);
                    }
                    catch (Exception ex)
                    {
                        MonitorLog.WriteLog("Ошибка выполнения процедуры CalcProc " +
                                            "при обработке задачи разбора файла (taskDisassembleFile)" + Environment.NewLine +
                                            ex.Message + ex.StackTrace,
                            MonitorLog.typelog.Error, true);
                        ret.text = "Ошибка вызова ф-ции разбора.";
                        ret.result = false;
                        ret.tag = -1;
                    }
                    finally
                    {
                        con_db.Close();
                    }
                    return;
                }

                else if (calcfon.TaskType == CalcFonTask.Types.taskDisassembleFile)
                {
                    ret = Utils.InitReturns();
                    var finder = new FilesDisassemble();

                    #region Подключение к БД
                    IDbConnection con_db = DBManager.GetConnection(Global.Constants.cons_Kernel);
                    ret = DBManager.OpenDb(con_db, true);
                    if (!ret.result)
                    {
                        MonitorLog.WriteLog("Ошибка открытия соединения в ф-ции CalcProc " +
                                            "при  при обработке задачи разбора файла (taskDisassembleFile)",
                            MonitorLog.typelog.Error, true);
                        return;
                    }
                    #endregion Подключение к БД

                    try
                    {
                        finder = JsonConvert.DeserializeObject<FilesDisassemble>(calcfon.parameters);
                        DbDisassembleFile disFile = new DbDisassembleFile(con_db);
                        ret = Utils.InitReturns();
                        ret = disFile.SelectDissMethod(finder, ref ret);
                    }
                    catch (Exception ex)
                    {
                        MonitorLog.WriteLog("Ошибка выполнения процедуры CalcProc " +
                                            "при обработке задачи разбора файла (taskDisassembleFile)" + Environment.NewLine +
                                            ex.Message + ex.StackTrace,
                            MonitorLog.typelog.Error, true);
                        ret.text = "Ошибка вызова ф-ции разбора.";
                        ret.result = false;
                        ret.tag = -1;
                    }
                    finally
                    {
                        con_db.Close();
                    }
                    return;
                }
                else if (calcfon.TaskType == CalcFonTask.Types.taskLoadFile)
                {
                    ret = Utils.InitReturns();
                    var finder = new FilesImported();

                    #region Подключение к БД
                    IDbConnection con_db = DBManager.GetConnection(Global.Constants.cons_Kernel);
                    ret = DBManager.OpenDb(con_db, true);
                    if (!ret.result)
                    {
                        MonitorLog.WriteLog("Ошибка открытия соединения в ф-ции CalcProc " +
                                            "при  при обработке задачи разбора файла (taskLoadFile)",
                            MonitorLog.typelog.Error, true);
                        return;
                    }
                    #endregion Подключение к БД

                    try
                    {
                        finder = JsonConvert.DeserializeObject<FilesImported>(calcfon.parameters);
                        DbFileLoader fl = new DbFileLoader(con_db);
                        ret = Utils.InitReturns();
                        ret = fl.LoadFile(finder, ref ret);
                    }
                    catch (Exception ex)
                    {
                        MonitorLog.WriteLog("Ошибка выполнения процедуры CalcProc " +
                                            "при обработке задачи разбора файла (taskLoadFile)" + Environment.NewLine +
                                            ex.Message + ex.StackTrace,
                            MonitorLog.typelog.Error, true);
                        ret.text = "Ошибка вызова ф-ции загрузки.";
                        ret.result = false;
                        ret.tag = -1;
                    }
                    finally
                    {
                        con_db.Close();
                    }
                    return;
                }
                else if (calcfon.TaskType == CalcFonTask.Types.taskLoadFileOnly)
                {
                    ret = Utils.InitReturns();
                    var finder = new FilesImported();

                    #region Подключение к БД
                    IDbConnection con_db = DBManager.GetConnection(Global.Constants.cons_Kernel);
                    ret = DBManager.OpenDb(con_db, true);
                    if (!ret.result)
                    {
                        MonitorLog.WriteLog("Ошибка открытия соединения в ф-ции CalcProc " +
                                            "при  при обработке задачи разбора файла (taskLoadFileOnly)",
                            MonitorLog.typelog.Error, true);
                        return;
                    }
                    #endregion Подключение к БД

                    try
                    {
                        finder = JsonConvert.DeserializeObject<FilesImported>(calcfon.parameters);
                        DbFileLoader fl = new DbFileLoader(con_db);
                        ret = Utils.InitReturns();
                        ret = fl.LoadingStep(finder, ref ret);
                    }
                    catch (Exception ex)
                    {
                        MonitorLog.WriteLog("Ошибка выполнения процедуры CalcProc " +
                                            "при обработке задачи разбора файла (taskLoadFileOnly)" + Environment.NewLine +
                                            ex.Message + ex.StackTrace,
                            MonitorLog.typelog.Error, true);
                        ret.text = "Ошибка вызова ф-ции загрузки.";
                        ret.result = false;
                        ret.tag = -1;
                    }
                    finally
                    {
                        con_db.Close();
                    }
                    return;
                }
                else if (calcfon.TaskType == CalcFonTask.Types.taskCheckStep)
                {
                    ret = Utils.InitReturns();
                    var finder = new FilesImported();

                    #region Подключение к БД
                    IDbConnection con_db = DBManager.GetConnection(Global.Constants.cons_Kernel);
                    ret = DBManager.OpenDb(con_db, true);
                    if (!ret.result)
                    {
                        MonitorLog.WriteLog("Ошибка открытия соединения в ф-ции CalcProc " +
                                            "при  при обработке задачи разбора файла (taskCheckStep)",
                            MonitorLog.typelog.Error, true);
                        return;
                    }
                    #endregion Подключение к БД

                    try
                    {
                        finder = JsonConvert.DeserializeObject<FilesImported>(calcfon.parameters);
                        DbFileLoader fl = new DbFileLoader(con_db);
                        ret = Utils.InitReturns();
                        ret = fl.CheckStep(finder, ref ret, con_db);
                    }
                    catch (Exception ex)
                    {
                        MonitorLog.WriteLog("Ошибка выполнения процедуры CalcProc " +
                                            "при обработке задачи разбора файла (taskLoadFileOnly)" + Environment.NewLine +
                                            ex.Message + ex.StackTrace,
                            MonitorLog.typelog.Error, true);
                        ret.text = "Ошибка вызова ф-ции загрузки.";
                        ret.result = false;
                        ret.tag = -1;
                    }
                    finally
                    {
                        con_db.Close();
                    }
                    return;
                }
                else if (calcfon.TaskType == CalcFonTask.Types.taskLoadFileFromSZ)
                {
                    //загрузка файла из СЗ
                    IDbConnection conn_db = GetConnection();
                    ret = Utils.InitReturns();
                    var finder = new FilesImported();
                    try
                    {
                        finder = JsonConvert.DeserializeObject<FilesImported>(calcfon.parameters);
                        LoadFileFromSz iSz = new LoadFileFromSz();
                        DbLoadFileFromSZpss lPss = new DbLoadFileFromSZpss();
                        ret = Utils.InitReturns();
                        //Начисленные субсидии
                        if (finder.type_load == 11)
                        {
                            ret = iSz.LoadFile(finder, ref ret, conn_db);
                        }
                        //Ответ поставщику
                        if (finder.type_load == 10)
                        {
                            ret = lPss.LoadFileFromSz_PSS(finder, ref ret, conn_db);
                        }

                    }
                    catch (Exception ex)
                    {
                        MonitorLog.WriteLog("Ошибка выполнения процедуры CalcProc " +
                                            "при обработке загрузки файла из СЗ (taskLoadFileFromSZ)" +
                                            Environment.NewLine +
                                            ex.Message + ex.StackTrace,
                            MonitorLog.typelog.Error, true);
                        ret.text = "Ошибка вызова ф-ции загрузки файла из СЗ.";
                        ret.result = false;
                        ret.tag = -1;
                    }
                    return;
                }
                else if (calcfon.TaskType == CalcFonTask.Types.taskLoadFileFromSZpss)
                {
                    //загрузка файла из СЗ
                    IDbConnection conn_db = GetConnection();
                    ret = Utils.InitReturns();
                    var finder = new FilesImported();

                    try
                    {
                        finder = JsonConvert.DeserializeObject<FilesImported>(calcfon.parameters);
                        DbLoadFileFromSZpss iSz = new DbLoadFileFromSZpss();
                        ret = Utils.InitReturns();
                        ret = iSz.LoadFileFromSz_PSS(finder, ref ret, conn_db);
                    }
                    catch (Exception ex)
                    {
                        MonitorLog.WriteLog("Ошибка выполнения процедуры CalcProc " +
                                            "при обработке загрузки файла из СЗ (taskLoadFileFromSZpss)" +
                                            Environment.NewLine +
                                            ex.Message + ex.StackTrace,
                            MonitorLog.typelog.Error, true);
                        ret.text = "Ошибка вызова ф-ции загрузки файла из СЗ.";
                        ret.result = false;
                        ret.tag = -1;
                    }
                    return;
                }
                else if (calcfon.TaskType == CalcFonTask.Types.CheckBeforeClosingMonth)
                {
                    //проверки перед закрытием месяца
                    ret = Utils.InitReturns();
                    var finder = new Finder();
                    try
                    {
                        finder = JsonConvert.DeserializeObject<FilesImported>(calcfon.parameters);
                        ret = Utils.InitReturns();
                        using (var db = new DbCharge())
                        {
                            db.MakeChecksBeforeCloseCalcMonth(finder, finder.dopFind, calcfon);
                        }
                    }
                    catch (Exception ex)
                    {
                        MonitorLog.WriteLog("Ошибка выполнения процедуры CalcProc " +
                                            "при проверках перед закрытием месяца (CheckBeforeClosingMonth)" +
                                            Environment.NewLine +
                                            ex.Message + ex.StackTrace,
                            MonitorLog.typelog.Error, true);
                        ret.text = "Ошибка вызова ф-ции проверки перед закрытием месяца.";
                        ret.result = false;
                        ret.tag = -1;
                    }
                    return;
                }
                else if (calcfon.TaskType == CalcFonTask.Types.taskExportParam)
                {
                    //проверки перед закрытием месяца
                    ret = Utils.InitReturns();
                    var finder = new ExportParamsFinder();
                    try
                    {
                        finder = JsonConvert.DeserializeObject<ExportParamsFinder>(calcfon.parameters);
                        ret = Utils.InitReturns();
                        using (var db = new DbExchange())
                        {
                            db.ExportParam(finder);
                        }
                    }
                    catch (Exception ex)
                    {
                        MonitorLog.WriteLog("Ошибка выполнения процедуры CalcProc " +
                                            "при экспорте параметров (ExportParam)" +
                                            Environment.NewLine +
                                            ex.Message + ex.StackTrace,
                            MonitorLog.typelog.Error, true);
                        ret.text = " Ошибка вызова ф-ции экспорта параметров.";
                        ret.result = false;
                        ret.tag = -1;
                    }
                    return;
                }
                else if (calcfon.TaskType == CalcFonTask.Types.taskGenLsPu)
                {
                    //генерация ИПУ
                    ret = Utils.InitReturns();
                    try
                    {
                        var finder = JsonConvert.DeserializeObject<Finder>(calcfon.parameters);
                        ret = Utils.InitReturns();
                        using (var db = new DbAdresHard())
                        {
                            ret = db.GenerateGroupLsPu(finder, calcfon);
                        }
                    }
                    catch (Exception ex)
                    {
                        MonitorLog.WriteLog("Ошибка выполнения процедуры CalcProc " +
                                            "при генерации ИПУ через групповые операции (taskGenLsPu)" +
                                            Environment.NewLine +
                                            ex.Message + ex.StackTrace,
                            MonitorLog.typelog.Error, true);
                        ret.text = " Ошибка генерации ИПУ через групповые операции. Смотрите журнал логов. ";
                        ret.result = false;
                        ret.tag = -1;
                    }
                    return;
                }
                else if (calcfon.TaskType == CalcFonTask.Types.taskUnloadFileForSZ)
                {
                    //выгрузка файла для СЗ
                    ret = Utils.InitReturns();
                    var finder = new FilesImported();
                    try
                    {
                        finder = JsonConvert.DeserializeObject<FilesImported>(calcfon.parameters);
                        StartUnl su = new StartUnl();
                        ret = Utils.InitReturns();
                        ret = su.Run(finder);
                    }
                    catch (Exception ex)
                    {
                        MonitorLog.WriteLog("Ошибка выполнения процедуры CalcProc " +
                                            "при обработке выгрузки файла для СЗ (taskUnloadFileForSZ)" +
                                            Environment.NewLine +
                                            ex.Message + ex.StackTrace,
                            MonitorLog.typelog.Error, true);
                        ret.text = "Ошибка вызова ф-ции выгрузки файла для СЗ.";
                        ret.result = false;
                        ret.tag = -1;
                    }
                    return;
                }
                else if (calcfon.TaskType == CalcFonTask.Types.taskLoadKladr)
                {
                    //загрузка КЛАДР
                    ret = Utils.InitReturns();
                    var finder = new FilesImported();

                    #region Подключение к БД

                    IDbConnection con_db = DBManager.GetConnection(Global.Constants.cons_Kernel);
                    ret = DBManager.OpenDb(con_db, true);
                    if (!ret.result)
                    {
                        MonitorLog.WriteLog("Ошибка открытия соединения в ф-ции CalcProc " +
                                            "при  при обработке задачи разбора файла (taskLoadKladr)",
                            MonitorLog.typelog.Error, true);
                        return;
                    }

                    #endregion Подключение к БД

                    try
                    {
                        finder = JsonConvert.DeserializeObject<FilesImported>(calcfon.parameters);
                        DbKladr kl = new DbKladr(con_db);
                        ret = Utils.InitReturns();
                        ret = kl.RefreshKLADRFile(finder, ref ret);
                    }
                    catch (Exception ex)
                    {
                        MonitorLog.WriteLog("Ошибка выполнения процедуры CalcProc " +
                                            "при обработке задачи разбора файла (taskLoadKladr)" + Environment.NewLine +
                                            ex.Message + ex.StackTrace,
                            MonitorLog.typelog.Error, true);
                        ret.result = false;
                        ret.tag = -1;
                    }
                    finally
                    {
                        con_db.Close();
                    }
                    return;
                }
                else if (calcfon.TaskType == CalcFonTask.Types.taskWithReval && REVAL)
                {
                    //+++++++++++++++++++++++++++++++++++++++++++++++++++
                    //расчет с перерасчетом
                    //+++++++++++++++++++++++++++++++++++++++++++++++++++

                    //paramcalc.b_again = false;
                    paramcalc.b_report = false;
                    paramcalc.b_reval = true;
                    paramcalc.b_must = true;
                    DbCalcCharge db = new DbCalcCharge();
                    bool b = db.CalcRevalXX(paramcalc, out ret);
                    //при наличии сверхбольших показаний ПУ выводим статус "С ошибками" и соответствующий текст
                    ret.tag = (int)db.status;
                    db.Close();

                }
                else if (calcfon.TaskType == CalcFonTask.Types.taskWithRevalOntoListHouses)
                {
                    //+++++++++++++++++++++++++++++++++++++++++++++++++++
                    //расчет с перерасчетом по списку домов
                    //+++++++++++++++++++++++++++++++++++++++++++++++++++

                    paramcalc.b_report = false;
                    paramcalc.b_reval = true;
                    paramcalc.b_must = true;
                    paramcalc.list_dom = true; // признак расчета по списку домов
                    DbCalcCharge db = new DbCalcCharge();
                    bool b = db.CalcRevalXX(paramcalc, out ret);
                    //при наличии сверхбольших показаний ПУ выводим статус "С ошибками" и соответствующий текст
                    ret.tag = (int)db.status;

                    db.Close();

                }
                else if (calcfon.TaskType == CalcFonTask.Types.taskAutomaticallyChangeOperDay)
                {
                    // добавить задачу на смену операционного дня
                    //-------------------------------------------------------------------------------------------
                    DateTime dat_when;

                    if (DateTime.TryParse(calcfon.dat_when, out dat_when))
                        dat_when = Convert.ToDateTime(calcfon.dat_when);
                    else dat_when = DateTime.Today;

                    CalcFonTask newCalcFon = new CalcFonTask(Points.GetCalcNum(0));
                    newCalcFon.TaskType = CalcFonTask.Types.taskAutomaticallyChangeOperDay;
                    newCalcFon.Status = FonTask.Statuses.New; //на выполнение    

                    newCalcFon.txt = "Автоматическая смена операционного дня в " + dat_when.Hour.ToString("00") + ":" +
                                     dat_when.Minute.ToString("00");
                    newCalcFon.year_ = DateTime.Today.Year;
                    newCalcFon.month_ = 0;
                    newCalcFon.dat_when = dat_when.AddDays(1).ToString("yyyy-MM-dd HH:mm");

                    DbCalcQueueClient dbCalc = new DbCalcQueueClient();
                    ret = dbCalc.AddTask(newCalcFon);
                    dbCalc.Close();
                    if (!ret.result) return;

                    // проверить, что операционный день можно поменять
                    //-------------------------------------------------------------------------------------------
                    OperDayFinder finder = new OperDayFinder();
                    finder.nzp_user = 1;
                    finder.mode = OperDayFinder.Mode.CloseOperDay.GetHashCode();

                    string date_oper = ""; //операционный день
                    string filename; //имя файла отчета
                    RecordMonth cm; //расчетный месяц

                    DbPack dbpack = new DbPack();
                    ret = dbpack.ChangeOperDay(finder, out date_oper, out filename, out cm);

                    if (!ret.result || ret.tag < 0) return;

                    DateTime new_date_oper;
                    if (DateTime.TryParse(date_oper, out new_date_oper))
                    {
                        // Points.DateOper = new_date_oper;
                    }

                }
                else if (calcfon.TaskType == CalcFonTask.Types.taskStartControlPays)
                {
                    ret = Utils.InitReturns();
                    var finder = new Payments();
                    // проверить, что операционный день можно поменять
                    //-------------------------------------------------------------------------------------------
                    try
                    {
                        finder = JsonConvert.DeserializeObject<Payments>(calcfon.parameters);
                        ret = Utils.InitReturns();
                        using (DbCalcQueueClient dbc = new DbCalcQueueClient(calcfon))
                        {
                            using (var db = new DbPack())
                            {
                                db.GenConDistrPaymentsPDF(finder, dbc.SetTaskProgress);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MonitorLog.WriteLog("Ошибка выполнения процедуры CalcProc " +
                                            "при проверках перед закрытием месяца (CheckBeforeClosingMonth)" +
                                            Environment.NewLine +
                                            ex.Message + ex.StackTrace,
                            MonitorLog.typelog.Error, true);
                        ret.text = "Ошибка вызова ф-ции проверки перед закрытием месяца.";
                        ret.result = false;
                        ret.tag = -1;
                    }

                }
                else if (calcfon.TaskType == CalcFonTask.Types.taskChangeOperDay)
                {
                    ret = Utils.InitReturns();
                    DateTime old = Points.DateOper;
                    var finder = new OperDayFinder();
                    string date_oper = "";
                    // проверить, что операционный день можно поменять
                    //-------------------------------------------------------------------------------------------
                    try
                    {
                        finder = JsonConvert.DeserializeObject<OperDayFinder>(calcfon.parameters);
                        ret = Utils.InitReturns();
                        using (DbCalcQueueClient dbc = new DbCalcQueueClient(calcfon))
                        {
                            using (var db = new DbPack())
                            {
                                ret = db.ChangeOperDay(finder, out date_oper, dbc.SetTaskProgress);
                            }
                        }
                        DateTime new_date_oper;
                        if (DateTime.TryParse(date_oper, out new_date_oper))
                        {
                            if (new_date_oper == old)
                            {
                                if (ret.tag < 0) calcfon.txt = ". " + ret.text;
                                else calcfon.txt = ". Смены операционного дня не произошло. Смотрите отчет на странице Контроль распределения оплат.";
                            }
                            else
                                calcfon.txt = ". Операционный день изменился на " + new_date_oper.ToShortDateString() + ".";
                        }
                        else
                        {
                            if (ret.tag < 0) calcfon.txt = ". " + ret.text;
                            else calcfon.txt = ". Смены операционного дня не произошло. Смотрите отчет на странице Контроль распределения оплат.";
                        }
                    }
                    catch (Exception ex)
                    {
                        MonitorLog.WriteLog("Ошибка выполнения процедуры CalcProc " +
                                            "при проверках перед операционного дня (ChangeOperDay)" +
                                            Environment.NewLine +
                                            ex.Message + ex.StackTrace,
                            MonitorLog.typelog.Error, true);
                        ret.text = "Ошибка вызова ф-ции проверки перед закрытием операционного дня.";
                        ret.result = false;
                        ret.tag = -1;
                    }
                }
                else if (calcfon.TaskType == CalcFonTask.Types.taskRecalcDistribSumOutSaldo)
                {
                    DateTime dat_oper;

                    if (DateTime.TryParse(calcfon.parameters, out dat_oper))
                        dat_oper = Convert.ToDateTime(calcfon.parameters);
                    else dat_oper = Points.DateOper;

                    int nzp_payer = calcfon.nzp;

                    using (DbCalcPack db2 = new DbCalcPack())
                    {
                        db2.UpdateSaldoFndistrib(dat_oper, nzp_payer, 0, out ret);
                    }
                }
                else if (calcfon.TaskType == CalcFonTask.Types.taskUpdateAddress)
                {
                    // обновление адресного пространства
                    Finder finder = new Finder();
                    using (DbAdres dbAdres = new DbAdres())
                    {
                        ret = dbAdres.RefreshAP(finder);
                    }
                }
                else if (calcfon.TaskType == CalcFonTask.Types.taskCalculateAnalytics)
                {
                    // подсчет аналитики
                    int year_ = calcfon.year_;

                    using (DbAnaliz dbAnaliz = new DbAnaliz())
                    {
                        dbAnaliz.LoadAnaliz1(out ret, year_, true);
                    }
                }
                else if (calcfon.TaskType == CalcFonTask.Types.OrderSequences)
                {
                    // упорядочивание последовательностей
                    using (OrderingSequence db = new OrderingSequence())
                    {
                        ret = db.DoOrderSequences();
                    }
                }
                else if (calcfon.TaskType == CalcFonTask.Types.AddPrimaryKey)
                {
                    using (var db = new DbAdmin())
                    {
                        ret = db.AddPrimaryKey(calcfon.nzp_user);
                    }
                }
                else if (calcfon.TaskType == CalcFonTask.Types.AddIndexes)
                {
                    using (var db = new DbAdmin())
                    {
                        ret = db.AddIndexes(calcfon.nzp_user, calcfon.parameters);
                    }
                }
                else if (calcfon.TaskType == CalcFonTask.Types.AddForeignKey)
                {
                    using (var db = new DbAdmin())
                    {
                        ret = db.AddForeignKey(calcfon.nzp_user);
                    }
                }
                else if (calcfon.TaskType == CalcFonTask.Types.SetNotNull)
                {
                    using (var db = new DbAdmin())
                    {
                        ret = db.AddNotNull(calcfon.nzp_user, calcfon.parameters);
                    }
                }
                else if (calcfon.TaskType == CalcFonTask.Types.TaskRefreshLSTarif)
                {
                    using (var db = new Supg())
                    {
                        ret.result = db.FillLSTarif(out ret);
                    }
                }
                else if (calcfon.TaskType == CalcFonTask.Types.taskRePrepareProvOnClosedCalcMonth)
                {
                    RePrepareProvsInFone(calcfon, ref ret);
                }
                else if (calcfon.TaskType == CalcFonTask.Types.taskInsertProvOnClosedOperDay)
                {
                    InsertProvOnClosedOperDayInFone(calcfon, ref ret);
                }
                else if (calcfon.TaskType == CalcFonTask.Types.taskInsertProvOnClosedCalcMonth)
                {
                    InsertProvOnClosedCalcMonthInFone(calcfon, ref ret);
                }
                else
                {
                    //+++++++++++++++++++++++++++++++++++++++++++++++++++
                    //расчет без перерасчета
                    //+++++++++++++++++++++++++++++++++++++++++++++++++++
                    paramcalc.calcfon = calcfon;
                    //paramcalc.b_again = false;
                    paramcalc.b_reval = false;
                    paramcalc.b_must = false;

                    /*
                            public const int taskDefault    = 0;   //полный цикл, по-умолчанию
                            public const int taskFull       = 1;   //полный расчет (CalcReportXX в отдельный процесс)
                            public const int taskSaldo      = 2;   //расчет только сальдо (CalcReportXX в отдельный процесс)
                            public const int taskCalcGil    = 101; //CalcGilXX      
                            public const int taskCalcRashod = 111; //CalcRashod     
                            public const int taskCalcNedo   = 121; //CalcNedo       
                            public const int taskCalcGku    = 131; //CalcGkuXX      
                            public const int taskCalcCharge = 141; //CalcChargeXX, после выполнения вызывает CalcReportXX
                            public const int taskCalcReport = 200; //CalcReportXX   
             
                            public bool b_gil;
                            public bool b_rashod;
                            public bool b_nedo;
                            public bool b_gku;
                            public bool b_charge;
                            public bool b_report;
                            public bool b_again;
                            public bool b_reval;
                            public bool b_must;
                    */

                    paramcalc.b_gil = (calcfon.calcFull || calcfon.TaskType == CalcFonTask.Types.taskCalcGil);
                    paramcalc.b_rashod = (calcfon.calcFull || calcfon.TaskType == CalcFonTask.Types.taskCalcRashod);
                    paramcalc.b_nedo = (calcfon.calcFull || calcfon.TaskType == CalcFonTask.Types.taskCalcNedo);
                    paramcalc.b_gku = (calcfon.calcFull || calcfon.TaskType == CalcFonTask.Types.taskCalcGku);
                    paramcalc.b_charge = (calcfon.calcFull || calcfon.TaskType == CalcFonTask.Types.taskCalcCharge ||
                                          calcfon.TaskType == CalcFonTask.Types.taskSaldo);
                    paramcalc.b_report = (calcfon.calcFull || calcfon.TaskType == CalcFonTask.Types.taskCalcReport ||
                                          calcfon.TaskType == CalcFonTask.Types.taskSaldo);

                    paramcalc.b_refresh = (calcfon.TaskType == CalcFonTask.Types.taskRefreshAP); //обновлние АП

                    if (paramcalc.b_refresh)
                    {
                        string s = paramcalc.pref + " " + paramcalc.nzp_kvar + "/" + paramcalc.nzp_dom + "/" +
                                   paramcalc.calc_yy + "/" + paramcalc.calc_mm + "/" +
                                   paramcalc.cur_yy + "/" + paramcalc.cur_mm;

                        MonitorLog.WriteLog("Старт RefreshAP: " + s, MonitorLog.typelog.Info, 1, 2, true);


                        string conn_kernel = Points.GetConnByPref(paramcalc.pref);
                        IDbConnection conn_db = GetConnection(conn_kernel);
                        ret = OpenDb(conn_db, true);
                        try
                        {


                            if (ret.result)
                            {
                                DbAdres db = new DbAdres();
                                bool b = db.RefreshAP(conn_db, paramcalc.pref, out ret);
                                db.Close();

                                if (!b)
                                {
                                    MonitorLog.WriteLog("Ошибка RefreshAP: " + ret.text, MonitorLog.typelog.Error, 222, 222,
                                        true);
                                    conn_db.Close();
                                    return;
                                }
                            }

                        }
                        finally
                        {
                            conn_db.Close();
                        }

                    }

                    DbCalcCharge dbc = new DbCalcCharge();
                    dbc.CalcFull(paramcalc, out ret);
                    dbc.Close();
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                if (idb != null) idb.Close();
            }

        }



        /// <summary>
        ///  Анализирует тип задачи и вызывает соответствующий обработчик,
        ///  задачи берутся из таблицы calc_fon_
        /// </summary>
        /// <param name="calcfon"> задача из calc_fon</param>
        /// <param name="ret"></param>
        public Returns CalcProc2(CalcFonTask calcfon, out Returns ret)
        {
            ret = Utils.InitReturns();
            MonitorLog.WriteLog("Старт обработки фоновой задачи:" + calcfon, MonitorLog.typelog.Info, true);
            try
            {

                using (var makeFonTask = new MakeFonTask())
                {
                    using (var task = makeFonTask.GetFonTask(calcfon))
                    {
                        ret = task.StartTask();
                    }
                }

            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Возникла ошибка при выполнении фоновой задачи:" +
                                    ex + Environment.NewLine +
                                    " Исходные параметры для задачи " +
                                    Environment.NewLine + calcfon,
                    MonitorLog.typelog.Error, true);

            }
            return ret;
        }


        private void InsertProvOnClosedOperDayInFone(CalcFonTask calcfon, ref Returns ret)
        {
            try
            {
                using (var db = new DbAdmin())
                {
                    var finder = JsonConvert.DeserializeObject<Finder>(calcfon.parameters);
                    if (finder.listNumber == OperDayFinder.Mode.CloseOperDay.GetHashCode())
                        ret = db.GetProvForClosedOperDay(finder);
                    if (finder.listNumber == OperDayFinder.Mode.GoBackOperDay.GetHashCode())
                    {
                        //Архивация проводок по опердню который откатили
                        ret = db.GetProvForClosedOperDay(finder, true);
                    }
                    if (finder.listNumber == OperDayFinder.Mode.GoDefinedOperDay.GetHashCode())
                    {
                        ret = db.GetProvForDefinedDay(finder, DateTime.Parse(finder.DateOper));
                    }
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры CalcProc " +
                                    "при записи проводок по закрытому опер.дню (GetProvForClosedOperDay)" +
                                    Environment.NewLine +
                                    ex.Message + ex.StackTrace,
                    MonitorLog.typelog.Error, true);
                ret.text = "Ошибка функции записи проводок";
                ret.result = false;
                ret.tag = -1;
            }
        }

        private void InsertProvOnClosedCalcMonthInFone(CalcFonTask calcfon, ref Returns ret)
        {
            try
            {
                using (var db = new DbAdmin())
                {
                    var finder = JsonConvert.DeserializeObject<Finder>(calcfon.parameters);
                    if (finder.listNumber == (int)ChargeOperations.CloseCalcMonth)
                        ret = db.GetProvForClosedMonth(finder);
                    if (finder.listNumber == (int)ChargeOperations.OpenCalcMonth)
                    {
                        //Архивация проводок по месяцу который откатили
                        ret = db.GetProvForClosedMonth(finder, true);
                    }
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры CalcProc " +
                                    "при записи проводок по закрытому расчетному месяцу (GetProvForClosedMonth)" +
                                    Environment.NewLine +
                                    ex.Message + ex.StackTrace,
                    MonitorLog.typelog.Error, true);
                ret.text = "Ошибка функции записи проводок";
                ret.result = false;
                ret.tag = -1;
            }
        }


        private void RePrepareProvsInFone(CalcFonTask calcfon, ref Returns ret)
        {
            try
            {
                using (var db = new DbAdmin())
                {
                    var finder = JsonConvert.DeserializeObject<Ls>(calcfon.parameters);
                    ret = db.RePrepareProvs(finder, idCalcFonTask: calcfon.nzp_key);
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры CalcProc " +
                                    "при записи проводок по закрытому расчетному месяцу (RePrepareProvs)" +
                                    Environment.NewLine +
                                    ex.Message + ex.StackTrace,
                    MonitorLog.typelog.Error, true);
                ret.text = "Ошибка функции записи проводок";
                ret.result = false;
                ret.tag = -1;
            }
        }

        public Returns Dop(CalcFonTask calcfon, CalcTypes.ParamCalc paramcalc, DbCalcQueueClient.SetTaskProgressDelegate setTaskProgress)
        {
            Returns ret = Utils.InitReturns();
            if (calcfon.TaskType == CalcFonTask.Types.CancelDistributionAndDeletePack)
            {
                paramcalc.b_pack = true;
                paramcalc.b_packDel = true;
                DbCalcPack db = new DbCalcPack();
                db.CalcPackDel(paramcalc, out ret, setTaskProgress);
                db.Close();
            }
            else if (calcfon.TaskType == CalcFonTask.Types.CancelPackDistribution)
            {
                paramcalc.b_pack = true;
                paramcalc.b_packOt = true;
                DbCalcPack db = new DbCalcPack();
                db.CalcPackOt(paramcalc, out ret, setTaskProgress);
                Returns ret2;
                db.PackFonTasks_21(calcfon, out ret2);
                db.Close();
            }
            else
            {
                paramcalc.b_pack = true;
                DbCalcPack db = new DbCalcPack();
                db.CalcPackXX(paramcalc, calcfon.year_, false, out ret, setTaskProgress); //откат пачки
                Returns ret2;
                db.PackFonTasks_21(calcfon, out ret2);
                db.Close();

            }
            return ret;
        }
        //--------------------------------------------------------------------------------
        public void CalcDom(long _nzp_dom, string pref, int calc_yy, int calc_mm, int cur_yy, int cur_mm, out Returns ret)

        //--------------------------------------------------------------------------------
        {
            CalcTypes.ParamCalc paramcalc = new CalcTypes.ParamCalc(0, Convert.ToInt32(_nzp_dom), pref, calc_yy, calc_mm, cur_yy, cur_mm);
            //paramcalc.b_again   = false;
            paramcalc.b_reval = false;
            paramcalc.b_must = false;
            DbCalcCharge db = new DbCalcCharge();
            db.CalcFull(paramcalc, out ret);
            db.Close();
        }
        public Returns PrepaeDataForPrintInvoces(string pref, int year, int month)
        {
            string mo = month.ToString("00");
            //int Year = Convert.ToInt32(year);
            Returns ret = Utils.InitReturns();
            StringBuilder sql = new StringBuilder();
            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return ret;
            //sql.Append("select* from ");
            //sql.Append(pref  + "_charge_" + (Year - 2000).ToString("00") + DBManager.tableDelimiter + "charge_" +Month);

            try
            {


                #region проверка существования табл charge_XX_T

                sql.Append("select * from ");
                sql.Append(pref + "_charge_" + (year - 2000).ToString("00") +
                           DBManager.tableDelimiter + "charge_" + month.ToString("00") + "_T ");
                ret = ExecSQL(conn_db, sql.ToString(), false);
                if (ret.result)
                {
                    //таблица есть, удаляем все данные
                    sql.Remove(0, sql.Length);
                    sql.Append(" DELETE FROM ");
                    sql.Append(pref + "_charge_" + (year - 2000).ToString("00") +
                               DBManager.tableDelimiter + "charge_" + month.ToString("00") + "_T ");
                    ret = ExecSQL(conn_db, sql.ToString(), true);

                    //todo check result!
                    if (!ret.result)
                    {
                        conn_db.Close();
                        return ret;
                    }
                }
                else
                {
                    //таблицы нет, создаем таблицу
                    sql.Remove(0, sql.Length);
#if PG
                    sql.Append(" SET search_path TO  " + pref + "_charge_" + (year - 2000).ToString("00") + ";");

#else

                    sql.Append(" DATABASE  " + pref + "_charge_" + (year - 2000).ToString("00") + ";");
#endif
                    ret = ExecSQL(conn_db, sql.ToString(), true);


                    if (!ret.result)
                    {
                        conn_db.Close();
                        return ret;
                    }

                    sql.Remove(0, sql.Length);
                    sql.Append("CREATE TABLE charge_" + month.ToString("00") + "_T (");
                    sql.Append(
                        "nzp_charge SERIAL NOT NULL,   nzp_kvar INTEGER,   num_ls INTEGER,   nzp_serv INTEGER,   nzp_supp INTEGER,   nzp_frm INTEGER,   dat_charge DATE, ");
                    sql.Append(
                        "tarif DECIMAL(14,3),   tarif_p DECIMAL(14,3),   rsum_tarif DECIMAL(14,2),   rsum_lgota DECIMAL(14,2),   sum_tarif DECIMAL(14,2),   sum_dlt_tarif DECIMAL(14,2), ");
                    sql.Append(
                        " sum_dlt_tarif_p DECIMAL(14,2),   sum_tarif_p DECIMAL(14,2),   sum_lgota DECIMAL(14,2),   sum_dlt_lgota DECIMAL(14,2),   sum_dlt_lgota_p DECIMAL(14,2),");
                    sql.Append(
                        "  sum_lgota_p DECIMAL(14,2),   sum_nedop DECIMAL(14,2),   sum_nedop_p DECIMAL(14,2),   sum_real DECIMAL(14,2),   sum_charge DECIMAL(14,2),");
                    sql.Append(
                        "  reval DECIMAL(14,2),   real_pere DECIMAL(14,2),   sum_pere DECIMAL(14,2),   real_charge DECIMAL(14,2),   sum_money DECIMAL(14,2),   money_to DECIMAL(14,2),");
                    sql.Append(
                        "   money_from DECIMAL(14,2),   money_del DECIMAL(14,2),   sum_fakt DECIMAL(14,2),   fakt_to DECIMAL(14,2),   fakt_from DECIMAL(14,2),   fakt_del DECIMAL(14,2),");
                    sql.Append(
                        "   sum_insaldo DECIMAL(14,2),   izm_saldo DECIMAL(14,2),   sum_outsaldo DECIMAL(14,2),   isblocked INTEGER,   is_device INTEGER default 0,   c_calc DECIMAL(14,2),");
                    sql.Append(
                        "   c_sn DECIMAL(14,2) default 0.00,   c_okaz DECIMAL(14,2),   c_nedop DECIMAL(14,2),   isdel INTEGER,   c_reval DECIMAL(14,2),  ");
                    sql.Append(
                        "     tarif_f DECIMAL(14,3),      sum_tarif_sn_eot DECIMAL(14,2) default 0.00,");
                    sql.Append(
                        "  sum_tarif_sn_f DECIMAL(14,2) default 0.00,     sum_subsidy DECIMAL(14,2) default 0.00,   sum_subsidy_p DECIMAL(14,2) default 0.00,");
                    sql.Append(
                        "   sum_subsidy_reval DECIMAL(14,2) default 0.00,   sum_subsidy_all DECIMAL(14,2) default 0.00,   ");
                    sql.Append(
                        "    tarif_f_p DECIMAL(14,3),    ");
                    sql.Append(
                        "   sum_tarif_sn_f_p DECIMAL(14,2) default 0.00,  ");
                    sql.Append(
                        "  order_print INTEGER default 0,   sum_tarif_f DECIMAL(14,2) default 0.00 NOT NULL,     gsum_tarif DECIMAL(14,2) default 0.00 NOT NULL)");
                    ret = ExecSQL(conn_db, sql.ToString(), true);
                    if (!ret.result)
                    {
                        conn_db.Close();
                        return ret;
                    }

                }

                #endregion

                #region проверка существования табл to_supplierXX
                sql.Remove(0, sql.Length);
                sql.Append("select * from ");
                sql.Append(pref + "_charge_" + (year - 2000).ToString("00") +
                           DBManager.tableDelimiter + "to_supplier" + month.ToString("00"));
                ret = ExecSQL(conn_db, sql.ToString(), false);
                if (ret.result)
                {
                    //таблица есть, удаляем все данные
                    sql.Remove(0, sql.Length);
                    sql.Append(" DELETE FROM ");
                    sql.Append(pref + "_charge_" + (year - 2000).ToString("00") +
                               DBManager.tableDelimiter + "to_supplier" + month.ToString("00"));
                    ret = ExecSQL(conn_db, sql.ToString(), true);

                    if (!ret.result)
                    {
                        conn_db.Close();
                        return ret;
                    }
                }
                else
                {
                    //таблицы нет, создаем таблицу
                    sql.Remove(0, sql.Length);

#if PG
                    sql.Append(" SET search_path TO  " + pref + "_charge_" + (year - 2000).ToString("00") + ";");
#else
         sql.Append(" DATABASE  " + pref + "_charge_" + (year - 2000).ToString("00") + ";");
#endif
                    ret = ExecSQL(conn_db, sql.ToString(), true);
                    if (!ret.result)
                    {
                        conn_db.Close();
                        return ret;
                    }
                    sql.Remove(0, sql.Length);
                    sql.Append(" CREATE TABLE to_supplier" + month.ToString("00") + " ( ");
                    sql.Append(
                        " nzp_to SERIAL NOT NULL,   nzp_serv INTEGER,   nzp_supp INTEGER,   nzp_pack_ls INTEGER,   nzp_charge INTEGER,   num_charge SMALLINT,   num_ls INTEGER, ");
                    sql.Append(
                        "sum_prih FLOAT,   kod_sum SMALLINT,   dat_month DATE,   dat_prih DATE,   dat_uchet DATE,   dat_plat DATE,   s_user FLOAT,   s_dolg FLOAT,   s_forw FLOAT) ");
                    // sql.Append("nzp_rs INTEGER default 1 NOT NULL) ");
                    ret = ExecSQL(conn_db, sql.ToString(), true);

                    if (!ret.result)
                    {
                        conn_db.Close();
                        return ret;
                    }
                }

                #endregion

                #region вставка в charge_XX_T

                sql.Remove(0, sql.Length);
                sql.Append("insert into " + pref + "_charge_" + (year - 2000).ToString("00") +
                           DBManager.tableDelimiter + "charge_" + month.ToString("00") + "_T ( ");
                sql.Append("nzp_charge  , nzp_kvar  , num_ls  , nzp_serv  , nzp_supp  , nzp_frm  , dat_charge, ");
                sql.Append("tarif  , tarif_p  , rsum_tarif  , rsum_lgota  , sum_tarif  , sum_dlt_tarif  , ");
                sql.Append(" sum_dlt_tarif_p  , sum_tarif_p  , sum_lgota  , sum_dlt_lgota  , sum_dlt_lgota_p  ,");
                sql.Append("  sum_lgota_p  , sum_nedop  , sum_nedop_p  , sum_real  , sum_charge  ,");
                sql.Append("  reval  , real_pere  , sum_pere  , real_charge  , sum_money  , money_to  ,");
                sql.Append(" money_from  , money_del  , sum_fakt  , fakt_to  , fakt_from  , fakt_del  ,");
                sql.Append(" sum_insaldo  , izm_saldo  , sum_outsaldo  , isblocked  , is_device  , c_calc  ,");
                sql.Append(" c_sn  , c_okaz  , c_nedop  , isdel  , c_reval  ,  ");
                sql.Append("    tarif_f  ,   sum_tarif_sn_eot  ,");
                sql.Append("  sum_tarif_sn_f  ,   sum_subsidy  , sum_subsidy_p  , ");
                sql.Append(" sum_subsidy_reval  , sum_subsidy_all  ,    ");
                sql.Append("  tarif_f_p  ,  ");
                sql.Append(" sum_tarif_sn_f_p  ,    ");
                sql.Append("  order_print  , sum_tarif_f  ,   gsum_tarif  )");

                sql.Append("select ");
                sql.Append(" nzp_charge  , nzp_kvar  , num_ls  , nzp_serv  , nzp_supp  , nzp_frm  , dat_charge , ");
                sql.Append(" tarif  , tarif_p  , rsum_tarif  , rsum_lgota  , sum_tarif  , sum_dlt_tarif  , ");
                sql.Append(" sum_dlt_tarif_p  , sum_tarif_p  , sum_lgota  , sum_dlt_lgota  , sum_dlt_lgota_p  ,");
                sql.Append("  sum_lgota_p  , sum_nedop  , sum_nedop_p  , sum_real  , sum_charge  ,");
                sql.Append("  reval  , real_pere  , sum_pere  , real_charge  , sum_money  , money_to  ,");
                sql.Append(" money_from  , money_del  , sum_fakt  , fakt_to  , fakt_from  , fakt_del  ,");
                sql.Append(" sum_insaldo  , izm_saldo  , sum_outsaldo  , isblocked  , is_device  , c_calc  ,");
                sql.Append(" c_sn  , c_okaz  , c_nedop  , isdel  , c_reval , ");
                sql.Append("    tarif_f  ,   sum_tarif_sn_eot  ,");
                sql.Append("  sum_tarif_sn_f  ,  sum_subsidy  , sum_subsidy_p  ,");
                sql.Append(" sum_subsidy_reval  , sum_subsidy_all  ,    ");
                sql.Append("   tarif_f_p  , ");
                sql.Append(" sum_tarif_sn_f_p  ,  ");
                sql.Append("  order_print  , sum_tarif_f  ,   gsum_tarif  ");
                sql.Append(" from ");
                sql.Append(pref + "_charge_" + (year - 2000).ToString("00") +
                           DBManager.tableDelimiter + "charge_" + month.ToString("00"));
                ret = ExecSQL(conn_db, sql.ToString(), true);
                if (!ret.result)
                {
                    conn_db.Close();
                    return ret;
                }
                DBManager.CreateIndexIfNotExists(conn_db, "ix_t_ch" + mo + "_supp", pref + "_charge_" + (year - 2000).ToString("00") + DBManager.tableDelimiter + "charge_" + month.ToString("00") + "_T", "nzp_supp", true, null);
                DBManager.CreateIndexIfNotExists(conn_db, "ix_t_kvsrdt_ch" + mo, pref + "_charge_" + (year - 2000).ToString("00") + DBManager.tableDelimiter + "charge_" + month.ToString("00") + "_T", "nzp_kvar, nzp_serv, dat_charge", true, null);
                DBManager.CreateIndexIfNotExists(conn_db, "ix_t_kvsrv_ch" + mo, pref + "_charge_" + (year - 2000).ToString("00") + DBManager.tableDelimiter + "charge_" + month.ToString("00") + "_T", "nzp_kvar, nzp_serv, nzp_supp", true, null);
                DBManager.CreateIndexIfNotExists(conn_db, "ix_t_kvsup_ch" + mo, pref + "_charge_" + (year - 2000).ToString("00") + DBManager.tableDelimiter + "charge_" + month.ToString("00") + "_T", "nzp_kvar, nzp_supp, dat_charge", true, null);
                DBManager.CreateIndexIfNotExists(conn_db, "ix_t_ls_ch" + mo, pref + "_charge_" + (year - 2000).ToString("00") + DBManager.tableDelimiter + "charge_" + month.ToString("00") + "_T", "num_ls", true, null);
                DBManager.CreateIndexIfNotExists(conn_db, "ix_t_lssrv_ch" + mo, pref + "_charge_" + (year - 2000).ToString("00") + DBManager.tableDelimiter + "charge_" + month.ToString("00") + "_T", "num_ls, nzp_serv, nzp_supp", true, null);
                DBManager.CreateIndexIfNotExists(conn_db, "ix_t_nzpch_ch" + mo, pref + "_charge_" + (year - 2000).ToString("00") + DBManager.tableDelimiter + "charge_" + month.ToString("00") + "_T", "nzp_charge", true, null);
                DBManager.CreateIndexIfNotExists(conn_db, "ix_t_ser_ch" + mo + "_supp", pref + "_charge_" + (year - 2000).ToString("00") + DBManager.tableDelimiter + "charge_" + month.ToString("00") + "_T", "nzp_serv", true, null);

                // sql.Remove(0, sql.Length);
                // sql.Append("create index tmp_charge_subs_2 on t_charge (pref)");            




                sql.Remove(0, sql.Length);
                sql.Append(DBManager.sUpdStat + " " + pref + "_charge_" + (year - 2000).ToString("00") + DBManager.tableDelimiter + "charge_" + month.ToString("00") + "_T");
                ret = ExecSQL(conn_db, sql.ToString(), true);
                if (!ret.result)
                {
                    conn_db.Close();
                    return ret;
                }

                #endregion

                #region вставка в to_supplierXX

                sql.Remove(0, sql.Length);
                sql.Append("insert into ");
                sql.Append(pref + "_charge_" + (year - 2000).ToString("00"));
                sql.Append(DBManager.tableDelimiter + "to_supplier" + month.ToString("00") + " (");
                sql.Append(" nzp_to, nzp_serv, nzp_supp , nzp_pack_ls , nzp_charge, num_charge , num_ls , ");
                sql.Append(
                    "sum_prih, kod_sum , dat_month, dat_prih , dat_uchet, dat_plat,s_user, s_dolg ,s_forw )");
                //sql.Append("nzp_rs ) ");
                sql.Append(" select ");
                sql.Append(" nzp_to, nzp_serv, nzp_supp , nzp_pack_ls , nzp_charge ,  num_charge , num_ls , ");
                sql.Append("sum_prih, kod_sum , dat_month, dat_prih , dat_uchet, dat_plat, s_user, s_dolg , s_forw ");
                //sql.Append("nzp_rs  ");
                sql.Append(" FROM " + pref + "_charge_" + (year - 2000).ToString("00"));
                sql.Append(DBManager.tableDelimiter + "fn_supplier" + month.ToString("00") + " ");
                ret = ExecSQL(conn_db, sql.ToString(), true);
                if (!ret.result)
                {
                    conn_db.Close();
                    return ret;
                }


                DBManager.CreateIndexIfNotExists(conn_db, "ix_t_fnts" + mo + "_1", pref + "_charge_" + (year - 2000).ToString("00") + DBManager.tableDelimiter + "to_supplier" + mo, "nzp_to", true, null);
                DBManager.CreateIndexIfNotExists(conn_db, "ix_t_fnts" + mo + "_2", pref + "_charge_" + (year - 2000).ToString("00") + DBManager.tableDelimiter + "to_supplier" + mo, "nzp_serv", true, null);
                DBManager.CreateIndexIfNotExists(conn_db, "ix_t_fnts" + mo + "_3", pref + "_charge_" + (year - 2000).ToString("00") + DBManager.tableDelimiter + "to_supplier" + mo, "dat_uchet, nzp_supp, num_ls, sum_prih", true, null);
                DBManager.CreateIndexIfNotExists(conn_db, "ix_t_fnts" + mo + "_4", pref + "_charge_" + (year - 2000).ToString("00") + DBManager.tableDelimiter + "to_supplier" + mo, "nzp_supp", true, null);
                DBManager.CreateIndexIfNotExists(conn_db, "ix_t_fnts" + mo + "_5", pref + "_charge_" + (year - 2000).ToString("00") + DBManager.tableDelimiter + "to_supplier" + mo, "num_ls", true, null);
                DBManager.CreateIndexIfNotExists(conn_db, "ix_t_fnts" + mo + "_6", pref + "_charge_" + (year - 2000).ToString("00") + DBManager.tableDelimiter + "to_supplier" + mo, "dat_prih", true, null);
                DBManager.CreateIndexIfNotExists(conn_db, "ix_t_fnts" + mo + "_7", pref + "_charge_" + (year - 2000).ToString("00") + DBManager.tableDelimiter + "to_supplier" + mo, "dat_uchet", true, null);
                DBManager.CreateIndexIfNotExists(conn_db, "ix_t_fnts" + mo + "_8", pref + "_charge_" + (year - 2000).ToString("00") + DBManager.tableDelimiter + "to_supplier" + mo, "num_ls, nzp_serv, dat_uchet", true, null);
                DBManager.CreateIndexIfNotExists(conn_db, "ix_t_fnts" + mo + "_80", pref + "_charge_" + (year - 2000).ToString("00") + DBManager.tableDelimiter + "to_supplier" + mo, "num_ls, nzp_serv, nzp_supp, dat_uchet", true, null);
                DBManager.CreateIndexIfNotExists(conn_db, "ix_t_fnts" + mo + "_9", pref + "_charge_" + (year - 2000).ToString("00") + DBManager.tableDelimiter + "to_supplier" + mo, "nzp_pack_ls", true, null);

                sql.Remove(0, sql.Length);
                sql.Append(DBManager.sUpdStat + " " + pref + "_charge_" + (year - 2000).ToString("00") + DBManager.tableDelimiter + "to_supplier" + mo + "; ");
                ret = ExecSQL(conn_db, sql.ToString(), true);
                if (!ret.result)
                {
                    conn_db.Close();
                    return ret;
                }

                #endregion

                return ret;
            }
            catch
            {
                //todo add code 
                ret.result = false;
                return ret;
            }
            finally
            {
                conn_db.Close();
            }
        }
        #endregion расчет всей базы или дома

        #region расчет одного лс через фоновую задачу
        //--------------------------------------------------------------------------------
        public void CalcLs(CalcFonTask calcfon, out Returns ret)
        //--------------------------------------------------------------------------------
        {
            string pref = Points.GetPref(calcfon.nzpt);
            CalcTypes.ParamCalc paramcalc = new CalcTypes.ParamCalc(Convert.ToInt32(calcfon.nzp), 0, pref, calcfon.year_, calcfon.month_, calcfon.year_, calcfon.month_);
            #region Заполняем параметр для paramcalc для тестового расчета
            //Сергей 16.12.2014
            paramcalc.id_bill_pref = GetTestCalcPref(calcfon.parameters);

            #endregion
            paramcalc.b_report = false;
            paramcalc.b_reval = true;
            paramcalc.b_must = true;
            DbCalcCharge db = new DbCalcCharge();
            bool b = db.CalcRevalXX(paramcalc, out ret);
            db.Close();
        }

        #endregion расчет одного лс через фоновую задачу

        #region расчет одного лс
        public Returns CalcLsWithRightCheck(Ls finder, bool reval, bool again, string alias, int id_bill)
        {
            #region Проверка разрешений на расчет
            Returns ret;
            DbAdminClient dba = new DbAdminClient();
            bool allow = dba.IsAllowCalcByPref(finder.pref, out ret);
            dba.Close();

            if (!ret.result) return ret;
            #endregion

            if (allow)
            {
                CalcLs(finder.nzp_kvar,
                    finder.pref,
                    Points.CalcMonth.year_, Points.CalcMonth.month_,
                    Points.CalcMonth.year_, Points.CalcMonth.month_,
                    reval,
                    again,
                    alias, id_bill,
                    out ret);
            }
            else
            {
                ret.result = false;
                ret.text = "Расчет начислений не доступен";
                ret.tag = -1;
            }
            return ret;
        }

        //--------------------------------------------------------------------------------
        public void CalcLs(
            int nzp_kvar, string pref, int calc_yy, int calc_mm, int cur_yy, int cur_mm,
            bool reval, bool data, string alias, int id_bill,
            out Returns ret)
        //--------------------------------------------------------------------------------
        {
            CalcTypes.ParamCalc paramcalc = new CalcTypes.ParamCalc(nzp_kvar, 0, pref, calc_yy, calc_mm, cur_yy, cur_mm);

            paramcalc.b_data = data;
            paramcalc.b_reval = false; //нельзя включать! - включается только в CalcRevalXX
            paramcalc.b_report = false;
            paramcalc.b_must = false;

            paramcalc.id_bill_pref = alias;
            paramcalc.id_bill = id_bill;

            string conn_kernel = Points.GetConnByPref(paramcalc.pref);
            IDbConnection conn_db = GetConnection(conn_kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result)
            {
                return;
            }

            //проверяем не рассчитывается ли уже этот лс
            if (IsNowCalcThisLs(nzp_kvar, ref ret, conn_db, paramcalc)) return;

            //во избежание deadlock'a проверяем не выполняется ли расчет банка/дома/списка домов
            if (IsNowCalcCharge(paramcalc.nzp_dom, paramcalc.pref, out ret)) return;

            //если все проверки прошли - отмечаем его как рассчитывающийся и начинаем расчет
            var sql = " INSERT INTO " + sDefaultSchema + "CalculatedPersonalAccounts(PersonalAccountId,DateStart) " +
                " values (" + nzp_kvar + ",now()) ";
            ret = ExecSQL(conn_db, sql);
            if (!ret.result) return;

            DbCalcCharge db = new DbCalcCharge();
            if (reval && REVAL)
            {
                bool b = db.CalcRevalXX(paramcalc, out ret);
            }
            else
            {
                db.CalcFull(paramcalc, out ret);
            }

            //после расчета снимаем признак рассчета
            sql = " DELETE FROM " + sDefaultSchema + "CalculatedPersonalAccounts " +
                         " WHERE personalAccountId = " + nzp_kvar;
            ExecSQL(conn_db, sql);

            db.Close();
            conn_db.Close();
        }

        /// <summary>
        /// Проверка на расчет лс в текущий момент
        /// </summary>
        /// <param name="nzp_kvar"></param>
        /// <param name="ret"></param>
        /// <param name="conn_db"></param>
        /// <param name="paramcalc"></param>
        /// <returns></returns>
        private bool IsNowCalcThisLs(int nzp_kvar, ref Returns ret, IDbConnection conn_db, CalcTypes.ParamCalc paramcalc)
        {
            var sql =
                " SELECT COUNT(1)>0 FROM " + sDefaultSchema + "CalculatedPersonalAccounts WHERE" +
                " now() BETWEEN DateStart AND (DateStart + INTERVAL '3 minute') " +
                " AND personalAccountId = " + nzp_kvar;
            var calculating = DBManager.ExecScalar<bool>(conn_db, sql, out ret, true);
            if (calculating)
            {
                ret.result = false;
                ret.tag = Constants.workinfon;
                ret.text = "Текущий лицевой счет в данный момент уже рассчитывается. Подождите пожалуйста..";
                return true;
            }
            return false;
        }

        #endregion расчет одного лс

        #region перерасчет
        //--------------------------------------------------------------------------------
        public bool CalcBaseWithReval(int nzp_kvar, long nzp_dom, string pref, int cur_yy, int cur_mm, out Returns ret)
        //--------------------------------------------------------------------------------
        {
            CalcTypes.ParamCalc paramcalc = new CalcTypes.ParamCalc(nzp_kvar, Convert.ToInt32(nzp_dom), pref, cur_yy, cur_mm, cur_yy, cur_mm);

            //paramcalc.b_again  = false;
            paramcalc.b_report = false;
            paramcalc.b_reval = true;
            paramcalc.b_must = true;
            DbCalcCharge db = new DbCalcCharge();
            bool b = db.CalcRevalXX(paramcalc, out ret);
            db.Close();
            return b;
        }
        #endregion перерасчет

        #region Расчет
        #region Признаки запуска функций расчета по маске clc
        //clc:
        //0 - CalcGilXX
        //1 - CalcRashod 
        //2 - CalcNedo
        //3 - CalcGkuXX
        //4 - CalcChargeXX 
        //5 - CalcReportXX 
        //6 - again - заново пересчитать пред. месяц с текущими изменениями (charge2_xx)
        //7 - reval - запись в перерасчетные таблицы
        //8 - must - учесть must_calc при выборке лицевых счетов 
        //--------------------------------------------------------------------------------
        #endregion Признаки запуска функций расчета по маске


        #endregion Расчет

        #region Распределение оплат
        //----------------------------------------------------------------------
        public bool RefreshAP(string pref, out Returns ret) //выполнить обновление АП через calcfon
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();

            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result)
            {
                return false;
            }

            //выполнить через calc_fon_0
            foreach (_Point zap in Points.PointList)
            {
                if (zap.nzp_wp == 1) continue;
                if (!string.IsNullOrEmpty(pref) && zap.pref != pref.Trim()) continue;

                CalcFonTask calcfon = new CalcFonTask(0);

                calcfon.TaskType = CalcFonTask.Types.taskRefreshAP; //refresh
                calcfon.Status = FonTask.Statuses.New; //на выполнение 

                calcfon.nzpt = zap.nzp_wp;
                calcfon.nzp = 0;
                //calcfon.number = Points.GetCalcNum(zap.nzp_wp); //определить номер потока расчета

                calcfon.txt = "'" + zap.point + "'";

                var db = new DbCalcQueueClient();
                ret = db.AddTask(conn_web, null, calcfon);
                db.Close();

                if (!ret.result)
                {
                    //ошибка
                    conn_web.Close();
                    return false;
                }

            }
            conn_web.Close();

            return true;
        }
        #endregion Распределение оплат
    }
    #endregion  Здесь находятся классы для фоновых задач расчетов
}
#endregion Расчет и фоновые задачи