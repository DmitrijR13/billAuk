using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Globalization;
using System.Data;
using STCLINE.KP50.Global;
using STCLINE.KP50.IFMX.Kernel.source.CommonType;
using STCLINE.KP50.Interfaces;
using System.IO;


#region Расчет и фоновые задачи
namespace STCLINE.KP50.DataBase
{
    #region  Здесь находятся классы для фоновых задач расчетов
    public partial class DbCalc : DbCalcClient
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

            CalcFon calcfon = new CalcFon();
            calcfon.nzp = Convert.ToInt32(_nzp_dom);
            calcfon.task = FonTaskTypeIds.taskWithReval;
            calcfon.nzpt = Points.GetPoint(pref).nzp_wp;

            RecordMonth rm = Points.GetCalcMonth(new CalcMonthParams(pref));
            calcfon.yy = rm.year_;
            calcfon.mm = rm.month_;
            calcfon.cur_yy = rm.year_;
            calcfon.cur_mm = rm.month_;

            calcfon.number = Points.GetCalcNum(pref); //определить номер потока расчета
            calcfon.status = FonTaskStatusId.InProcess; //контроль

            if (calcfon.number < CalcThreads.maxCalcThreads)
            {
                CheckCalcAndPutInFon(conn_web, calcfon, out ret);

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
        /// Проверяет наличие задач к выполнению
        /// </summary>
        /// <param name="number"></param>
        /// <param name="check"></param>
        /// <returns></returns>
        public bool CalcFonTasks(int number, bool check)
        {
            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            Returns ret = OpenDb(conn_web, true);
            if (!ret.result) return false;

            try
            {
                return CalcFonTasks(conn_web, number, check);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message, ex);
            }
            finally
            {
                conn_web.Close();
            }
        }

        /// <summary>
        /// Проверяет наличие задач к выполнению
        /// </summary>
        /// <param name="number"></param>
        /// <param name="check"></param>
        /// <returns></returns>
        public bool CalcFonTasks(IDbConnection conn_web, int number, bool check)
        {
            string tab = "calc_fon_" + number;
            Returns ret;

#if PG
            ExecSQL(conn_web, "set search_path to 'public'", true);
#endif

            if (check)
            {
                //проверить наличие таблицы
                CalcFon calcfon = new CalcFon(number);
                CheckCalcAndPutInFon(conn_web, calcfon, out ret);

                //при рестарте фоновых задач снова на расчет
                ret = ExecSQL(conn_web,
                    " Update " + tab +
                    " Set kod_info = 3 " +
                    " Where kod_info = 0 " +
                    "   and year_ = " + Points.CalcMonth.year_ +
                    "   and month_ = " + Points.CalcMonth.month_
                    , true);
                if (!ret.result)
                {
                    conn_web.Close();
                    return false;
                }
            }

            var obj = ExecScalar(conn_web, " Select count(*) as num From " + tab + " Where kod_info = " + (int)FonTaskStatusId.InQueue, out ret, true);
            if (!ret.result) return false;

            bool b;
            try
            {
                b = Convert.ToInt32(obj) > 0;
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("CalcFonTasks\n" + ex.Message, MonitorLog.typelog.Error, true);
                b = false;
            }

            return b;
        }
        
        /// <summary>
        /// Cчитывает очередное задачи из очереди и вызывает функции обработчика
        /// </summary>
        /// <param name="number">номер очереди заданий</param>
        /// <returns>Возвращает true, если была считана задача для обработки, false - задачи не было, функция отработала вхолостую</returns>
        public bool CalcFonProc(int number)
        {
            bool taskExists = false;
            string tab = "calc_fon_" + number;
            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            Returns ret = OpenDb(conn_web, true);
            if (!ret.result) return taskExists;

            MyDataReader reader = null;
            try
            {
                //задание уже висит на выполнении, выходим
                ExecSQL(conn_web, "set search_path to 'public'", false);
                ret = ExecRead(conn_web, out reader,
                    " Select * From " + tab +
                    " Where kod_info = 0 and " + sNvlWord + "(dat_when, " + MDY(1,1,1900)+ ") < " + sCurDateTime , true);
                if (!ret.result)
                {
                    return taskExists;
                }
                if (reader.Read())
                {
                    ret.result = false;
                    ret.text = "Другой фоновый процесс уже выполнятеся. Подождите, пожалуйста!";
                    return taskExists;
                }
                reader.Close();

                string sql = " Select * From " + tab +
                    " Where kod_info = 3 " + 
                    "   and " + sNvlWord + "(dat_when, " + MDY(1,1,1900)+ ") < " + sCurDateTime +
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

                    CalcFon calcfon = new CalcFon();
                    calcfon.number = number;
                    calcfon.nzp = 0;
                    calcfon.yy = 0;
                    calcfon.mm = 0;

                    try
                    {
                        if (reader["nzp"] != DBNull.Value) calcfon.nzp = (int)reader["nzp"];
                        if (reader["nzp_user"] != DBNull.Value) calcfon.nzp_user = (int)reader["nzp_user"];
                        if (reader["nzpt"] != DBNull.Value) calcfon.nzpt = (int)reader["nzpt"];
                        if (reader["task"] != DBNull.Value) calcfon.task = FonTaskType.GetIdByInt((int)reader["task"]);
                        if (reader["year_"] != DBNull.Value) calcfon.yy = (int)reader["year_"];
                        if (reader["month_"] != DBNull.Value) calcfon.mm = (int)reader["month_"];
                        if (reader["dat_when"] != DBNull.Value) calcfon.dat_when = Convert.ToDateTime(reader["dat_when"]).ToString();
                        if (reader["parameters"] != DBNull.Value) calcfon.parameters = Convert.ToString(reader["parameters"]);
                    }
                    catch
                    {
                        return taskExists;
                        //break;
                    }
                    //reader.Close();

                    if (calcfon.yy > 0 && calcfon.mm >= 0)
                    {
                        long nzp_dom = calcfon.nzp;

                        string txt = "";
                        if (calcfon.task == FonTaskTypeIds.DistributePack ||
                            calcfon.task == FonTaskTypeIds.CancelPackDistribution ||
                            calcfon.task == FonTaskTypeIds.CancelDistributionAndDeletePack ||
                            calcfon.task == FonTaskTypeIds.UpdatePackStatus ||
                            calcfon.task == FonTaskTypeIds.DistributeOneLs ||
                            calcfon.task == FonTaskTypeIds.taskAutomaticallyChangeOperDay)
                        {
                        }
                        else if ((calcfon.task == FonTaskTypeIds.taskCalcSubsidyRequest) || (calcfon.task == FonTaskTypeIds.taskCalcSaldoSubsidy))
                        {
                        }
                        else if (calcfon.task == FonTaskTypeIds.taskDisassembleFile)
                        {
                        }
                        else
                        {
                            txt = ", txt = " + CalcFonComment(conn_web, number, calcfon.nzpt, nzp_dom, false);
                        }

                        
                        ret = ExecSQL(conn_web,
                                " Update " + tab +
#if PG
                                " Set kod_info = 0, dat_work = now() " +
#else
 " Set kod_info = 0, dat_work = current " +
#endif
 " Where nzp    = " + calcfon.nzp +
                                  " and nzpt   = " + calcfon.nzpt +
                                  " and year_  = " + calcfon.yy +
                                  " and month_ = " + calcfon.mm +
                                  " and task   = " + (int)calcfon.task +
                                  " and kod_info = 3 ", true);
                        if (!ret.result)
                        {
                            return taskExists;
                            //break;
                        }

                        //+++++++++++++++++++++++++++++++++++++++++++
                        //вызов модуля расчета и распределения
                        //+++++++++++++++++++++++++++++++++++++++++++
                        CalcProc(calcfon, out ret);

                        //задание отработано или ошибка - установить флаг!
                        if (!ret.result)
                        {
                            calcfon.status = FonTaskStatusId.Failed;
                            calcfon.txt = "Ошибка выполнения! Смотрите журнал ошибок."; //ret.text;
                        }
                        else
                        {
                            calcfon.status = FonTaskStatusId.Completed;
                        }

                        CheckCalcAndPutInFon(conn_web, calcfon, out ret);
                        if (!ret.result)
                        {
                            return taskExists;
                        }

                        if (calcfon.callReportAlone)
                        {
                            //calcReport через отдельный поток #0
                            calcfon.task = FonTaskTypeIds.taskCalcReport;
                            calcfon.number = 0;
                            calcfon.status = FonTaskStatusId.New; //установка задания

                            CheckCalcAndPutInFon(conn_web, calcfon, out ret);
                        }

                        //Решил не вывзвать автоматический пересчет сальдо после каждого распределения пачки - слишком накладно!
                        //Вызываться будет при расчете fn_distrib_xx  
                        /*
                        if (calcfon.task == taskCalcPack || calcfon.task == taskCalcPackOt)
                        {
                            //после окончания распределения вызвать расчет сальдо
                            calcfon.task = taskSaldo;
                            calcfon.nzp = 0;
                            calcfon.kod_info = 1; //установка задания

                            CheckCalcAndPutInFon(conn_web, calcfon, out ret);
                        }
                        else
                        {
                            if (calcfon.callReportAlone)
                            {
                                //calcReport через отдельный поток #0
                                calcfon.task = taskCalcReport;
                                calcfon.number = 0;
                                calcfon.kod_info = 1; //установка задания

                                CheckCalcAndPutInFon(conn_web, calcfon, out ret);
                            }
                        }
                        */
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

        //--------------------------------------------------------------------------------
        //определение calc_fon_xx и установка задание на выполнение
        //--------------------------------------------------------------------------------
        public void CalcOnFon(long _nzp_dom, string pref/*, int calc_yy, int calc_mm, int cur_yy, int cur_mm*/, bool reval, out Returns ret)
        //--------------------------------------------------------------------------------
        {
            FonTaskTypeIds task = FonTaskTypeIds.taskFull;
            if (reval)
                task = FonTaskTypeIds.taskWithReval;

            CalcOnFon(_nzp_dom, pref/*, calc_yy, calc_mm, cur_yy, cur_mm*/, task, out ret);
        }

        public void CalcOnFon(long _nzp_dom, string pref/*, int calc_yy, int calc_mm, int cur_yy, int cur_mm*/, FonTaskTypeIds task, out Returns ret)
        {
            ret = Utils.InitReturns();

            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result)
            {
                return;
            }

            CalcFon calcfon = new CalcFon();
            calcfon.nzp     = _nzp_dom;
            //calcfon.yy      = calc_yy;
            //calcfon.mm      = calc_mm;
            //calcfon.cur_yy  = cur_yy; //надо явно подставить тек. расчетный месяц, что случайно не испортить данные через базу!!!!
            //calcfon.cur_mm  = cur_mm;
            calcfon.task    = task;
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
                //calcfon.status = FonTaskStatusId.InProcess; //контроль

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
                calcfon.status = FonTaskStatusId.New; //на расчет
                
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
                        calcfon.yy = rm.year_;
                        calcfon.mm = rm.month_;
                        calcfon.cur_yy = rm.year_;
                        calcfon.cur_mm = rm.month_; 
                        calcfon.nzpt = zap.nzp_wp;
                        calcfon.number = Points.GetCalcNum(zap.nzp_wp); //определить номер потока расчета

                        CheckCalcAndPutInFon(conn_web, calcfon, out ret);
                        
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
                ret.tag = -1;
            }
        }

        #endregion Фоновые задачи

        #region Выборка во временные таблицы

        //--------------------------------------------------------------------------------
        void ChoiseTempMustCalc(IDbConnection conn_db, CalcTypes.ParamCalc paramcalc, out Returns ret)
        //--------------------------------------------------------------------------------
        {
            ExecSQL(conn_db, " Drop table t_mustcalc ", false);

            if (!paramcalc.b_reval)
            {
                ret = ExecSQL(conn_db,
                    " Create temp table t_mustcalc " +
                    " ( nzp_kvar integer," +
                    "   nzp_serv integer " +
#if PG
                    " ) "
#else
                    " ) With no log "
#endif
                    , true);
                if (!ret.result)
                {
                    return;
                }
            }
            else 
            {
                string portal_must_calc = "";

                if (paramcalc.isPortal)
                {
                    ChargeXX calc_chargeXX = new ChargeXX(paramcalc); //расчетная таблица начислений

                    portal_must_calc =
                        " Union " +
                        " Select nzp_kvar, nzp_serv From " + calc_chargeXX.charge_cnts +
                        " Where nzp_kvar  = " + paramcalc.nzp_kvar +
                        "   and id_bill = " + paramcalc.id_bill +
                        "   and dat_charge = " + paramcalc.portal_dat_charge;
                }

                //пока сделал так, что если задан конкретный дом, то пересчитываются все лицевые счета дома (т.е. must_calc игнорируется)
                //+++++++++++++++++++++++++++++++++++++++++++++++++++
                //выбрать мно-во лицевых счетов из must_calc!
                //+++++++++++++++++++++++++++++++++++++++++++++++++++
                //" Insert into t_mustcalc (nzp_kvar,nzp_serv) " + //не проходит
                ret = ExecSQL(conn_db,
                    " Select " + sUniqueWord + " k.nzp_kvar, m.nzp_serv " +
#if PG
                    " Into temp t_mustcalc "+
#else
#endif
                    " From " + paramcalc.data_alias + "must_calc m, " + paramcalc.data_alias + "kvar k " +
                    " Where k.nzp_kvar = m.nzp_kvar " +
                    "   and k." + paramcalc.where_z +
                    "   and m.year_ = " + paramcalc.cur_yy +
                    "   and m.month_ = " + paramcalc.cur_mm +
                    "   and m.dat_s  <= " + sPublicForMDY + "MDY(" + paramcalc.calc_mm + "," + 
                    DateTime.DaysInMonth(paramcalc.calc_yy, paramcalc.calc_mm) + "," + paramcalc.calc_yy + ")" +
                    "   and m.dat_po >= "+sPublicForMDY+"MDY(" + paramcalc.calc_mm + ",1," + paramcalc.calc_yy + ")"
                    + portal_must_calc 
#if PG
#else
                    + " Into temp t_mustcalc With no log "
#endif
                    , true);
                if (!ret.result)
                {
                    return;
                }
            }

            ExecSQL(conn_db, " Create unique index ix_t_mustcalc on t_mustcalc (nzp_kvar,nzp_serv) ", true);
            ExecSQL(conn_db, sUpdStat + " t_mustcalc ", true);
        }
        //--------------------------------------------------------------------------------
        void ChoiseTempKvar(IDbConnection conn_db, ref CalcTypes.ParamCalc paramcalc, out Returns ret)
        //--------------------------------------------------------------------------------
        {
            ChoiseTempKvar(conn_db, ref paramcalc, true, out ret);
        }
        //--------------------------------------------------------------------------------
        void ChoiseTempKvar(IDbConnection conn_db, ref CalcTypes.ParamCalc paramcalc, bool must_calc, out Returns ret)
        //--------------------------------------------------------------------------------
        {
            if (Constants.Trace) Utility.ClassLog.WriteLog("Старт функции ChoiseTempKvar");

            string ssql = "";
            ret = Utils.InitReturns();
            
            ExecSQL(conn_db, " Drop table t_opn ", false);  // CASCADE
            ExecSQL(conn_db, " Drop table t_selkvar ", false); // CASCADE
            //препарированные таблицы
            if (paramcalc.b_loadtemp)
            {
                DropTempTables(conn_db, paramcalc.pref);

            }

            ret = ExecSQL(conn_db, " select * from t_selkvar ", false);
            if (!ret.result)
            {
                ssql =
                    " Create temp table t_selkvar " +
                    //" Create table are.t_selkvar " +
                    " ( nzp_key   serial not null, " +
                    "   nzp_kvar  integer, " +
                    "   num_ls    integer, " +
                    "   nzp_dom   integer, " +
                    "   nzp_area  integer, " +
                    "   nzp_geu   integer  " +
#if PG
                    " )";
#else
                    //" ) "
                    " ) With no log ";
#endif
            }
            else
            {
                ssql = " truncate table t_selkvar ";
            }
            ret = ExecSQL(conn_db, ssql, true);
            if (!ret.result)
            {
                return;
            }

            ret = ExecSQL(conn_db, " select * from t_opn ", false);
            if (!ret.result)
            {
                ssql =
                    " Create temp table t_opn " +
                    " ( nzp_kvar integer," +
                    "   nzp_dom  integer," +
                    "   num_ls   integer, " +
                    "   dat_s    date not null," +
                    "   dat_po   date not null" +
#if PG
                    " )";
#else
                    //" ) "
                    " ) With no log ";
#endif
            }
            else
            {
                ssql = " truncate table t_opn ";
            }
            ret = ExecSQL(conn_db, ssql, true);
            if (!ret.result)
            {
                return;
            }

            string s_must = "";

            if (must_calc)
            {
                ChoiseTempMustCalc(conn_db, paramcalc, out ret);
                if (!ret.result)
                {
                    return;
                }
            }

            if (paramcalc.b_must && must_calc) //доп. ограничение на выборку данных
                s_must = " and k.nzp_kvar in ( Select nzp_kvar From t_mustcalc ) ";

            //расчет сальдо после распределения пачки
            if (paramcalc.nzp_pack_saldo > 0)
            {
                string pack_ls_table = Points.Pref + "_fin_" + 
                    (paramcalc.calc_yy - 2000).ToString("00") + paramcalc.ol_srv + tableDelimiter + "pack_ls ";
                s_must = " and k.num_ls in ( Select num_ls From " + pack_ls_table + 
                    " Where nzp_pack = " + paramcalc.nzp_pack_saldo + " ) ";
            }

            //+++++++++++++++++++++++++++++++++++++++++++++++++++
            //выбрать множество лицевых счетов
            //+++++++++++++++++++++++++++++++++++++++++++++++++++
            string s_find =
                " From " + paramcalc.data_alias + "kvar k " +
                " Where " + paramcalc.where_z + s_must;
            
            if (paramcalc.nzp_dom > 0) 
            {
                s_find =
                    " From " + paramcalc.data_alias + "kvar k " +
                    " Where k.nzp_dom=" + paramcalc.nzp_dom.ToString() + s_must;

                // ... для расчета связанные дома ... 
                IDbCommand cmd_link = DBManager.newDbCommand(
                    " Select max(nzp_dom_base) From " + paramcalc.data_alias + "link_dom_lit p " +
                    " Where nzp_dom=" + paramcalc.nzp_dom.ToString() + " "
                , conn_db);

                int nzp_dom_base = 0;
                try
                {
                    string sndb = Convert.ToString(cmd_link.ExecuteScalar());
                    nzp_dom_base = Convert.ToInt32(sndb);
                }
                catch
                {
                    nzp_dom_base = 0;
                }

                if (nzp_dom_base > 0)
                {

                    s_find =
                    " From " + paramcalc.data_alias + "kvar k, " + paramcalc.data_alias + "link_dom_lit l " +
                    " Where k.nzp_dom=l.nzp_dom and l.nzp_dom_base=" + nzp_dom_base + s_must;
                }
            }
            if (paramcalc.nzp_kvar > 0) 
            {
                s_find =
                    " From " + paramcalc.data_alias + "kvar k " +
                    " Where k.nzp_kvar=" + paramcalc.nzp_kvar.ToString() + s_must;
            }

            ret = ExecSQL(conn_db,
                " Insert into t_selkvar (nzp_kvar,num_ls,nzp_dom, nzp_area,nzp_geu) " +
                " Select k.nzp_kvar,k.num_ls,k.nzp_dom,k.nzp_area,k.nzp_geu " + s_find
                , true);
            if (!ret.result)
            {
                return;
            }
            ExecSQL(conn_db, " Create unique index ix1_t_selkvar on t_selkvar (nzp_key) ", false);
            ExecSQL(conn_db, " Create unique index ix2_t_selkvar on t_selkvar (nzp_kvar) ", false);
            ExecSQL(conn_db, " Create        index ix3_t_selkvar on t_selkvar (num_ls) ", false);
            ExecSQL(conn_db, " Create        index ix4_t_selkvar on t_selkvar (nzp_dom) ", false);
            ExecSQL(conn_db, sUpdStat + " t_selkvar ", true);

            //+++++++++++++++++++++++++++++++++++++++++++++++++++
            // выбрать открытые лицевые счета по t_selkvar
            //+++++++++++++++++++++++++++++++++++++++++++++++++++
            ret = ExecSQL(conn_db,
            " Insert into t_opn (nzp_kvar, nzp_dom, num_ls, dat_s, dat_po)" +
                " Select k.nzp_kvar, k.nzp_dom, k.num_ls," +
                " min(" + sNvlWord + "(p.dat_s," + MDY(12, 31, 1899) + ")), max(" + sNvlWord + "(p.dat_po," + MDY(1, 1, 3000) + ")) " +
                " From t_selkvar k," + paramcalc.data_alias + "prm_3 p" +
                " Where k.nzp_kvar=p.nzp and p.nzp_prm=51 and p.val_prm in ('1','3') " +
                "   and p.is_actual<>100 " +
                "   and p.dat_s  < " + MDY(paramcalc.calc_mm, 28, paramcalc.calc_yy) +
                "   and p.dat_po >=" + MDY(paramcalc.calc_mm, 28, paramcalc.calc_yy) +
                " group by 1,2,3 "
                , true);
            if (!ret.result)
            {
                return;
            }

            ExecSQL(conn_db, " Create unique index ix1_t_opn on t_opn (nzp_kvar) ", false);
            ExecSQL(conn_db, " Create        index ix2_t_opn on t_opn (num_ls) ", false);
            ExecSQL(conn_db, " Create        index ix3_t_opn on t_opn (nzp_dom) ", false);
            ExecSQL(conn_db, sUpdStat + " t_opn ", true);

            //переопределим nzp_dom
            if (paramcalc.nzp_kvar > 0)
            {
                IDataReader reader;
                ret = ExecRead(conn_db, out reader,
                    " Select nzp_dom From t_selkvar Where nzp_kvar = " + paramcalc.nzp_kvar
                    , true);
                if (!ret.result)
                {
                    return;
                }
                try
                {
                    if (reader.Read())
                    {
                        paramcalc.nzp_dom = (int)reader["nzp_dom"];
                    }
                }
                catch
                {
                    ret.result = false;
                    reader.Close();
                    return;
                }
                reader.Close();
            }

            if (Constants.Trace) Utility.ClassLog.WriteLog("Стоп функции ChoiseTempKvar");
        }

        #endregion Выборка во временные таблицы

        #region расчет всей базы или дома или квартиры

        
        /// <summary>
        ///  Анализирует тип задачи и вызывает соответствующий обработчик,
        ///  задачи берутся из таблицы calc_fon_
        /// </summary>
        /// <param name="calcfon"> задача из calc_fon</param>
        /// <param name="ret"></param>
        public void CalcProc(CalcFon calcfon, out Returns ret)
        {           
            if (calcfon.task == FonTaskTypeIds.taskKvar)
            {
                //+++++++++++++++++++++++++++++++++++++++++++++++++++
                //расчет одного лицевого счета через фоновую задачу
                //+++++++++++++++++++++++++++++++++++++++++++++++++++

                CalcLs(calcfon, out ret);
                return;
            }
            else if (calcfon.task == FonTaskTypeIds.UpdatePackStatus)
            {
                DbPack dbPack = new DbPack();
                PackFinder finder = new PackFinder();
                finder.nzp_user = 1;
                finder.year_ = calcfon.yy;
                finder.nzp_pack = Convert.ToInt32(calcfon.nzp);

                ReturnsType ret2 = dbPack.Upd_SUM_RASP_and_SUM_NRASP(finder);
                ret.result = ret2.result;
                ret.sql_error = ret2.sql_error;
                ret.text = ret2.text;
                ret.tag = ret2.tag;
                dbPack.Close();
                return;
            }
            else if (calcfon.task == FonTaskTypeIds.DistributeOneLs)
            {
                CalcDistribPackLs(calcfon, out ret);
                return;
            }
            else if (calcfon.task == FonTaskTypeIds.taskCalcSubsidyRequest)
            {
                CalcSubsidyRequest(calcfon, out ret);
                CheckSubsidyRequestTask(calcfon, out ret);
                return;
            }
            else if (calcfon.task == FonTaskTypeIds.taskCalcSaldoSubsidy)
            {
                CalcSaldoSubsidy(calcfon, out ret);
                return;
            }
            else if (calcfon.task == FonTaskTypeIds.uchetOplatArea)
            { 
                IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
                ret = OpenDb(conn_db, true);
                if (!ret.result) return;
                
                Charge charge = new Charge();
                charge.nzp_area = Convert.ToInt32(calcfon.nzp);
                charge.year_ = calcfon.yy;
                charge.month_ = calcfon.mm;

                CalcChargeXXUchetOplatForArea(conn_db, null, charge);

                conn_db.Close();
            }
            else if (calcfon.task == FonTaskTypeIds.taskGetFakturaWeb)
            {
                ret = Utils.InitReturns();
                using (IDbConnection con_db = GetConnection(Constants.cons_Kernel))
                {
                    try
                    {
                        ret = OpenDb(con_db, true);
                        IDataReader reader = null;
                        string strWBFDatabase = "";
#if PG
                        string strSQLQuery = String.Format("SELECT (TRIM({0}_kernel.s_baselist.dbname) || '.session_ls') AS wbf_set search_path to 'FROM {0}_kernel.s_baselist WHERE {0}_kernel.s_baselist.idtype = 8 limit 1';", Points.Pref);
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
                        while (reader.Read()) if (reader["wbf_database"] != DBNull.Value) {
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
                            if (reader["wbf_id_bill"] != DBNull.Value) intWBFIdBill = Convert.ToInt32(reader["wbf_id_bill"]);
                            if (reader["wbf_dat_when"] != DBNull.Value) dtWBFDateWhen = Convert.ToDateTime(reader["wbf_dat_when"]);
                            if (reader["wbf_cur_oper"] != DBNull.Value) intWBFCurOper = Convert.ToInt32(reader["wbf_cur_oper"]);
                            if (reader["dat_nzp_kvar"] != DBNull.Value) intDATNzpKvar = Convert.ToInt32(reader["dat_nzp_kvar"]);
                            if (reader["dat_nzp_dom"] != DBNull.Value) intDATNzpDom = Convert.ToInt32(reader["dat_nzp_dom"]);
                            if (reader["dat_pkod"] != DBNull.Value) longDATPkod = Convert.ToInt64(reader["dat_pkod"]);
                            if (reader["krn_kind"] != DBNull.Value) intKRNKind = Convert.ToInt32(reader["krn_kind"]);
                            if (reader["sys_nzp_user"] != DBNull.Value) intSYSNzpUser = Convert.ToInt32(reader["sys_nzp_user"]);
                        }

                        Faktura finder = new Faktura();
                        finder.pref = strWBFPref;
                        finder.nzp_kvar = intDATNzpKvar;
                        finder.nzp_dom = intDATNzpDom;
                        finder.idFaktura = intKRNKind;
                        finder.nzp_user = intSYSNzpUser;
                        finder.workRegim = Faktura.WorkFakturaRegims.Web;
                        finder.withDolg = true;
                        finder.month_ = calcfon.mm;
                        finder.year_ = calcfon.yy;
                        finder.resultFileType = Faktura.FakturaFileTypes.PDF;
                        finder.destFileName = String.Format("{0}_{1}{2}_{3}", longDATPkod, calcfon.yy.ToString("0000"), calcfon.mm.ToString("00"), intWBFIdBill);

                        DbFaktura dbFaktura = new DbFaktura();
                        List<string> fName = dbFaktura.GetFaktura(finder, out ret);

                        strSQLQuery = String.Format("UPDATE {0} SET cur_oper = {1}, erc12 = '{2}', erc28 = '{3}' WHERE nzp_session = {4};", strWBFDatabase, ret.result ? "34" : "-5", ret.result ? ret.text.Split('|')[0] : "", ret.result ? ret.text.Split('|')[1] : "", calcfon.nzpt);
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
                        MonitorLog.WriteLog("Ошибка выполнения процедуры GetFakturaWeb : " + ex.Message, MonitorLog.typelog.Error, true);
                        ret.result = false;
                        ret.tag = -1;
                    }
                    finally { con_db.Close(); }
                }
                return;
            } else if (calcfon.task == FonTaskTypeIds.taskToTransfer)
            {

                DateTime dat_s = DateTime.ParseExact(calcfon.nzp.ToString(), "yyyyMMdd", CultureInfo.InvariantCulture);
                DateTime dat_po = DateTime.ParseExact(calcfon.nzpt.ToString(), "yyyyMMdd", CultureInfo.InvariantCulture);

                DbCalc dbc = new DbCalc();
                dbc.DistribPaXX_1(dat_s, dat_po, out ret);
                dbc.Close();
            }

            string pref = Points.GetPref(calcfon.nzpt);
            CalcTypes.ParamCalc paramcalc = new CalcTypes.ParamCalc(0, Convert.ToInt32(calcfon.nzp), pref, calcfon.yy, calcfon.mm, calcfon.yy, calcfon.mm, calcfon.nzp_user);

            if (calcfon.task == FonTaskTypeIds.CancelDistributionAndDeletePack ||
                calcfon.task == FonTaskTypeIds.CancelPackDistribution ||
                calcfon.task == FonTaskTypeIds.DistributePack)
            {
                IDataReader reader;
                string kvar = Points.Pref + "_data" + tableDelimiter + "kvar ";
                int yy = Points.CalcMonth.year_;
                string pack_ls = Points.Pref + "_fin_" + (yy - 2000).ToString("00") + tableDelimiter  + "pack_ls ";

                IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
                ret = OpenDb(conn_db, true);
                if (!ret.result)  return;

                ret = ExecRead(conn_db, out reader,
                    " Select " + sUniqueWord + " pref From " + kvar +
                    " Where num_ls in ( Select num_ls From " + pack_ls + " Where nzp_pack = " + calcfon.nzp + ")"
                    , true);

                if (!ret.result)
                {
                    conn_db.Close();
                    return;
                }

                List<string> list = new List<string>();
                try
                {
                    while (reader.Read())
                        if (reader["pref"] != DBNull.Value) list.Add((string)reader["pref"]);                   
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


                if (list.Count > 0)
                {
                   
                    foreach (string prf in list)
                    {
                        foreach (_Point zap in Points.PointList)
                        {
                            if (zap.pref != prf.Trim()) continue;

                            calcfon.nzpt = zap.nzp_wp;
                            paramcalc.pref = prf.Trim();

                            ret = Dop(calcfon, paramcalc);
                        }
                    }
                }

               
                ret = ExecRead(conn_db, out reader,
                    " Select * From " + pack_ls + " Where nzp_pack = " + calcfon.nzp + " and (num_ls is null or num_ls = 0)", true);

                if (!ret.result)
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
                    ret = Dop(calcfon, paramcalc);
                  
                }

               
                if (!ret.result ||
                    calcfon.task == FonTaskTypeIds.CancelPackDistribution ||
                    calcfon.task == FonTaskTypeIds.DistributePack)
                {
                    Returns ret2;
                    calcfon.task = FonTaskTypeIds.UpdatePackStatus;
                    CalcProc(calcfon, out ret2);
                }
                
            }
            else if (calcfon.task == FonTaskTypeIds.taskPreparePrintInvoices)
            {
                //todo Test! Only for one house 

                //выполнить контрольные проверки
                DbCharge dbCharge = new DbCharge();
                ReturnsType retType = dbCharge.MakeChecksBeforeCloseCalcMonth();
                dbCharge.Close();
               // retType.result = false; 
                if (!retType.result)
                {
                    #region запись ошибок в текстовый файл

                    StringBuilder sql = new StringBuilder();
                    IDbConnection conn_db1 = GetConnection(Constants.cons_Kernel);
                    string fileName = Constants.Directories.ReportDir.ToString() + "//Протокол_ошибок_" +
                                      DateTime.Now.ToShortDateString()+"_"+DateTime.Now.Ticks.ToString();
                    //постановка на поток
                    ExcelRepClient excelRep = new ExcelRepClient();
                    ret = excelRep.AddMyFile(new ExcelUtility()
                    {
                        nzp_user = calcfon.nzp_user,
                        status = ExcelUtility.Statuses.InProcess,
                        rep_name ="Протокол ошибок от "+ DateTime.Now.ToShortDateString()
                    });
                    if (!ret.result)
                    {
                        return;
                    }
                    var nzp_exc = ret.tag;
                    //////////////////////////////////////  

                    ret = OpenDb(conn_db1, true);
                    if (!ret.result) return;
                    MyDataReader reader;
                    string ErrorText = "";
                    sql.Append(" select dat_check, note, name_prov from ");
                    sql.Append(Points.Pref + DBManager.sDataAliasRest + "checkchmon c ");
                    sql.Append(" where c.status_=2 and c.dat_check='" + DateTime.Now.ToShortDateString() + "'");
                    ret = ExecRead(conn_db1, out reader, sql.ToString(), true);
                    if (!ret.result)
                    {
                        return;
                    }

                    while (reader.Read())
                    {
                        ErrorText = ErrorText +"Дата:"+ reader["dat_check"].ToString().ToLower().Trim() +"  Текст ошибки:"+
                                    reader["note"].ToString().ToLower().Trim() + ".  " +
                                    reader["name_prov"].ToString().ToLower().Trim() + ".  Название проверки" + Environment.NewLine;
                    }
                    conn_db1.Close();
                    StreamWriter sw = File.CreateText(fileName + ".txt");
                    sw.Write(ErrorText);
                    sw.Flush();
                    sw.Close();


                    //смена статуса

                    IDbConnection conn_db3 = GetConnection(Constants.cons_Webdata);
                    ret = OpenDb(conn_db3, true);
                    if (!ret.result) return;
                    
                    
                    StringBuilder sql3 = new StringBuilder();
                  
                    sql3.Append(" update " + sPublicForMDY + "excel_utility set stats = " + (int)ExcelUtility.Statuses.Success);
                    sql3.Append(", dat_out = " + Utils.EStrNull(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")));
                    


                    if (fileName!= "") sql3.Append(", exc_path = " + fileName);
                    sql3.Append(" where nzp_exc =" + nzp_exc);
                    ret = ExecSQL(conn_db3, sql3.ToString(), true);
                    conn_db3.Close();
                    sql3.Remove(0, sql3.Length);
                    #endregion

                    ret.result = retType.result;
                    ret.text = retType.text;
                    excelRep.SetMyFileState(new ExcelUtility()
                    {
                        nzp_exc = nzp_exc,
                        status = ExcelUtility.Statuses.Success,                        
                        exc_path = fileName,
                         nzp_user = calcfon.nzp_user,
                       
                        rep_name ="Протокол ошибок от "+ DateTime.Now.ToShortDateString()
                    });
                }

                //выполняется подготовка данных
                bool fPrepareData = calcfon.nzp == 1 ? true : false;
                if (fPrepareData)
                {
                    ret = this.PrepaeDataForPrintInvoces(pref, calcfon.yy, calcfon.mm);
                    if (!ret.result)
                    {
                        return;
                    }
                }            
    
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


                #region Расчет банка
                int y = calcfon.yy;
                int m = calcfon.mm;

                if (m < 12) ++m;
                else
                {
                    m = 1;
                    ++y;
                }

                paramcalc = new ParamCalc(0, Convert.ToInt32(calcfon.nzp), pref, y, m, y, m, calcfon.nzp_user);

                paramcalc.b_report = false;
                paramcalc.b_reval = true;
                paramcalc.b_must = true;

                calcfon.nzp = 0; //не по конкретному дому

                bool b = CalcRevalXX(paramcalc, out ret);
                if (!b) return;
                #endregion
            }

                //обработчик задачи разбора
            else if (calcfon.task == FonTaskTypeIds.taskDisassembleFile)
            {
                ret = Utils.InitReturns();
                var finder = new FilesDisassemble();
                
                try
                {
                    #region Подключение к БД
                    IDbConnection con_db = DBManager.GetConnection(Global.Constants.cons_Kernel);
                    ret = DBManager.OpenDb(con_db, true);
                    if (!ret.result)
                    {
                        MonitorLog.WriteLog("Ошибка открытия соединения в ф-ции CalcProc " +
                                            "при  при обработке задачи разбора файла (taskDisassembleFile)", MonitorLog.typelog.Error, true);
                        return;
                    }
                    #endregion Подключение к БД
                    
                    //sqlText = 
                    //    " UPDATE calc_fon_" + calcfon.number+ " SET txt = " +
                    //    " 'Разбор файла ХАРАКТЕРИСТИКИ ЖИЛОГО ФОНДА И НАЧИСЛЕНИЯ ЖКУ' " +
                    //    " WHERE task = 305 and parameters = " + calcfon.parameters +
                    //    "       AND nzp_user = "+ calcfon.nzp_user; 
                    //ClassDBUtils.ExecSQL(sqlText, con_db, ClassDBUtils.ExecMode.Log);

                    finder = JsonConvert.DeserializeObject<FilesDisassemble>(calcfon.parameters);
                    
                    DbDisassembleFile disFile = new DbDisassembleFile(con_db);
                    ret = disFile.DisassembleFile(finder, ref ret);
                }
                catch (Exception ex)
                {
                    MonitorLog.WriteLog("Ошибка выполнения процедуры CalcProc " +
                                        "при обработке задачи разбора файла (taskDisassembleFile)" + Environment.NewLine + ex.Message + ex.StackTrace,
                        MonitorLog.typelog.Error, true);
                    ret.text = "Ошибка вызова ф-ции разбора.";
                    ret.result = false;
                    ret.tag = -1;
                }
               return;
            }
            else if (calcfon.task == FonTaskTypeIds.taskWithReval && REVAL)
            {
                //+++++++++++++++++++++++++++++++++++++++++++++++++++
                //расчет с перерасчетом
                //+++++++++++++++++++++++++++++++++++++++++++++++++++

                //paramcalc.b_again = false;
                paramcalc.b_report = false;
                paramcalc.b_reval = true;
                paramcalc.b_must = true;

                bool b = CalcRevalXX(paramcalc, out ret);
            }
            else if (calcfon.task == FonTaskTypeIds.taskAutomaticallyChangeOperDay)
            {
                // добавить задачу на смену операционного дня
                //-------------------------------------------------------------------------------------------
                DateTime dat_when;

                if (DateTime.TryParse(calcfon.dat_when, out dat_when)) dat_when = Convert.ToDateTime(calcfon.dat_when);
                else dat_when = DateTime.Today;

                CalcFon newCalcFon = new CalcFon(Points.GetCalcNum(0));
                newCalcFon.task = FonTaskTypeIds.taskAutomaticallyChangeOperDay;
                newCalcFon.status = FonTaskStatusId.New; //на выполнение    

                newCalcFon.txt = "Автоматическая смена операционного дня в " + dat_when.Hour.ToString("00") + ":" +
                                 dat_when.Minute.ToString("00");
                newCalcFon.yy = DateTime.Today.Year;
                newCalcFon.mm = 0;
                newCalcFon.dat_when = dat_when.AddDays(1).ToString("yyyy-MM-dd HH:mm");

                DbCalc dbCalc = new DbCalc();
                ret = dbCalc.AddTask(newCalcFon);
                if (!ret.result) return;

                // проверить, что операционный день можно поменять
                //-------------------------------------------------------------------------------------------
                DbPack dbpack = new DbPack();

                Payments pay = new Payments();
                pay.dat_s = (new DateTime(Points.DateOper.Year, Points.DateOper.Month, 1)).ToShortDateString();
                pay.dat_po = Points.DateOper.ToShortDateString();
                pay.checkCanChangeOperDay = true;
                // не подгототавливать отчет "Контроль распределения оплат"
                pay.prepareContrDistribReport = false;

                ret = dbpack.GenConDistrPayments(pay);
                if (!ret.result) return;

                if (ret.tag == -7)
                {
                    ret.text = "Операционный день нельзя менять";
                    return;
                }

                // поменять операционный день
                //-------------------------------------------------------------------------------------------
                Pack finder = new Pack();
                finder.nzp_user = 1;
                finder.dat_uchet = Points.DateOper.AddDays(1).ToShortDateString();

                ret = dbpack.SaveOperDay(finder);

                if (!ret.result) return;

                Points.DateOper = Points.DateOper.AddDays(1);
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

                paramcalc.b_gil = (calcfon.calcFull || calcfon.task == FonTaskTypeIds.taskCalcGil);
                paramcalc.b_rashod = (calcfon.calcFull || calcfon.task == FonTaskTypeIds.taskCalcRashod);
                paramcalc.b_nedo = (calcfon.calcFull || calcfon.task == FonTaskTypeIds.taskCalcNedo);
                paramcalc.b_gku = (calcfon.calcFull || calcfon.task == FonTaskTypeIds.taskCalcGku);
                paramcalc.b_charge = (calcfon.calcFull || calcfon.task == FonTaskTypeIds.taskCalcCharge ||
                                      calcfon.task == FonTaskTypeIds.taskSaldo);
                paramcalc.b_report = (calcfon.calcFull || calcfon.task == FonTaskTypeIds.taskCalcReport ||
                                      calcfon.task == FonTaskTypeIds.taskSaldo);

                paramcalc.b_refresh = (calcfon.task == FonTaskTypeIds.taskRefreshAP); //обновлние АП

                CalcFull(paramcalc, out ret);
            }

        }



        private Returns Dop(CalcFon calcfon, CalcTypes.ParamCalc paramcalc)
        {
            Returns ret = Utils.InitReturns();
            if (calcfon.task == FonTaskTypeIds.CancelDistributionAndDeletePack)
            {
                paramcalc.b_pack = true;
                paramcalc.b_packDel = true;
                CalcPackDel(paramcalc, out ret);
            }
            else if (calcfon.task == FonTaskTypeIds.CancelPackDistribution)
            {
                paramcalc.b_pack = true;
                paramcalc.b_packOt = true;
                CalcPackOt(paramcalc, out ret);
                Returns ret2;
                PackFonTasks_21(calcfon, out ret2);
            }
            else
            {
                paramcalc.b_pack = true;
                CalcPackXX(paramcalc, false, out ret); //откат пачки
                Returns ret2;
                PackFonTasks_21(calcfon, out ret2);
            }
            return ret;
        }
        //--------------------------------------------------------------------------------
        public void CalcDom(long _nzp_dom, string pref, int calc_yy, int calc_mm, int cur_yy, int cur_mm, out Returns ret)

        //--------------------------------------------------------------------------------
        {
            CalcTypes.ParamCalc paramcalc = new CalcTypes.ParamCalc(0, Convert.ToInt32(_nzp_dom), pref, calc_yy, calc_mm, cur_yy, cur_mm);
            //paramcalc.b_again   = false;
            paramcalc.b_reval   = false;
            paramcalc.b_must    = false;

            CalcFull(paramcalc, out ret);
        }
        public Returns PrepaeDataForPrintInvoces(string pref, int year, int month)
        {
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
                    sql.Append(" DATABASE  " + pref + "_charge_" + (year - 2000).ToString("00") + ";");
                    ret = ExecSQL(conn_db, sql.ToString(), true);

                    //todo check result!
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
                        "   c_sn DECIMAL(14,2) default 0.00,   c_okaz DECIMAL(14,2),   c_nedop DECIMAL(14,2),   isdel INTEGER,   c_reval DECIMAL(14,2),   reval_tarif DECIMAL(14,2) default 0.00,");
                    sql.Append(
                        "  reval_lgota DECIMAL(14,2) default 0.00,   tarif_f DECIMAL(14,3),   sum_tarif_eot DECIMAL(14,2) default 0.00,   sum_tarif_sn_eot DECIMAL(14,2) default 0.00,");
                    sql.Append(
                        "  sum_tarif_sn_f DECIMAL(14,2) default 0.00,   rsum_subsidy DECIMAL(14,2) default 0.00,   sum_subsidy DECIMAL(14,2) default 0.00,   sum_subsidy_p DECIMAL(14,2) default 0.00,");
                    sql.Append(
                        "   sum_subsidy_reval DECIMAL(14,2) default 0.00,   sum_subsidy_all DECIMAL(14,2) default 0.00,   sum_lgota_eot DECIMAL(14,2) default 0.00,   sum_lgota_f DECIMAL(14,2) default 0.00,");
                    sql.Append(
                        "   sum_smo DECIMAL(14,2) default 0.00,   tarif_f_p DECIMAL(14,3),   sum_tarif_eot_p DECIMAL(14,2) default 0.00,   sum_tarif_sn_eot_p DECIMAL(14,2) default 0.00,");
                    sql.Append(
                        "   sum_tarif_sn_f_p DECIMAL(14,2) default 0.00,   sum_lgota_eot_p DECIMAL(14,2) default 0.00,   sum_lgota_f_p DECIMAL(14,2) default 0.00,   sum_smo_p DECIMAL(14,2) default 0.00,");
                    sql.Append(
                        "  order_print INTEGER default 0,   sum_tarif_f DECIMAL(14,2) default 0.00 NOT NULL,   sum_tarif_f_p DECIMAL(14,2) default 0.00 NOT NULL,   gsum_tarif DECIMAL(14,2) default 0.00 NOT NULL)");
                    ret = ExecSQL(conn_db, sql.ToString(), true);

                    //todo check result!
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
                    sql.Append(" DATABASE  " + pref + "_charge_" + (year - 2000).ToString("00") + ";");
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
                sql.Append(" c_sn  , c_okaz  , c_nedop  , isdel  , c_reval  , reval_tarif  ,");
                sql.Append("  reval_lgota  , tarif_f  , sum_tarif_eot  , sum_tarif_sn_eot  ,");
                sql.Append("  sum_tarif_sn_f  , rsum_subsidy  , sum_subsidy  , sum_subsidy_p  ,");
                sql.Append(" sum_subsidy_reval  , sum_subsidy_all  , sum_lgota_eot  , sum_lgota_f  ,");
                sql.Append(" sum_smo  , tarif_f_p  , sum_tarif_eot_p  , sum_tarif_sn_eot_p  ,");
                sql.Append(" sum_tarif_sn_f_p  , sum_lgota_eot_p  , sum_lgota_f_p  , sum_smo_p  ,");
                sql.Append("  order_print  , sum_tarif_f  , sum_tarif_f_p  , gsum_tarif  )");

                sql.Append("select ");
                sql.Append(" nzp_charge  , nzp_kvar  , num_ls  , nzp_serv  , nzp_supp  , nzp_frm  , dat_charge , ");
                sql.Append(" tarif  , tarif_p  , rsum_tarif  , rsum_lgota  , sum_tarif  , sum_dlt_tarif  , ");
                sql.Append(" sum_dlt_tarif_p  , sum_tarif_p  , sum_lgota  , sum_dlt_lgota  , sum_dlt_lgota_p  ,");
                sql.Append("  sum_lgota_p  , sum_nedop  , sum_nedop_p  , sum_real  , sum_charge  ,");
                sql.Append("  reval  , real_pere  , sum_pere  , real_charge  , sum_money  , money_to  ,");
                sql.Append(" money_from  , money_del  , sum_fakt  , fakt_to  , fakt_from  , fakt_del  ,");
                sql.Append(" sum_insaldo  , izm_saldo  , sum_outsaldo  , isblocked  , is_device  , c_calc  ,");
                sql.Append(" c_sn  , c_okaz  , c_nedop  , isdel  , c_reval  , reval_tarif  ,");
                sql.Append("  reval_lgota  , tarif_f  , sum_tarif_eot  , sum_tarif_sn_eot  ,");
                sql.Append("  sum_tarif_sn_f  , rsum_subsidy  , sum_subsidy  , sum_subsidy_p  ,");
                sql.Append(" sum_subsidy_reval  , sum_subsidy_all  , sum_lgota_eot  , sum_lgota_f  ,");
                sql.Append(" sum_smo  , tarif_f_p  , sum_tarif_eot_p  , sum_tarif_sn_eot_p  ,");
                sql.Append(" sum_tarif_sn_f_p  , sum_lgota_eot_p  , sum_lgota_f_p  , sum_smo_p  ,");
                sql.Append("  order_print  , sum_tarif_f  , sum_tarif_f_p  , gsum_tarif  ");
                sql.Append(" from ");
                sql.Append(pref + "_charge_" + (year - 2000).ToString("00") +
                           DBManager.tableDelimiter + "charge_" + month.ToString("00"));
                ret = ExecSQL(conn_db, sql.ToString(), true);

                //todo check result!
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

                //todo check result!
                if (!ret.result)
                {
                    conn_db.Close();
                    return ret;
                }

                #endregion

                return ret;
            }
            catch (Exception ex)
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
        void CalcLs(CalcFon calcfon, out Returns ret)
        //--------------------------------------------------------------------------------
        {
            string pref = Points.GetPref(calcfon.nzpt);
            CalcTypes.ParamCalc paramcalc = new CalcTypes.ParamCalc(Convert.ToInt32(calcfon.nzp), 0, pref, calcfon.yy, calcfon.mm, calcfon.yy, calcfon.mm);

            paramcalc.b_report = false;
            paramcalc.b_reval = true;
            paramcalc.b_must = true;

            bool b = CalcRevalXX(paramcalc, out ret);
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

            paramcalc.b_data    = data;
            paramcalc.b_reval   = false; //нельзя включать! - включается только в CalcRevalXX
            paramcalc.b_report  = false;
            paramcalc.b_must    = false;

            paramcalc.id_bill_pref  = alias;
            paramcalc.id_bill       = id_bill;

            //CalcFull(paramcalc, out ret);
            //return;

            if (reval && REVAL)
            {
                bool b = CalcRevalXX(paramcalc, out ret);
            }
            else
            {
                CalcFull(paramcalc, out ret);
            }
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
            paramcalc.b_reval  = true;
            paramcalc.b_must   = true;

            bool b = CalcRevalXX(paramcalc, out ret);
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
        
        public void CalcFull(int nzp_kvar, string pref, int calc_yy, int calc_mm, int cur_yy, int cur_mm, bool[] clc, out Returns ret)
        //--------------------------------------------------------------------------------
        {
            CalcTypes.ParamCalc paramcalc = new CalcTypes.ParamCalc(nzp_kvar, 0, pref, calc_yy, calc_mm, cur_yy, cur_mm);
            //paramcalc.b_again = clc[6];
            paramcalc.b_reval = clc[7];
            paramcalc.b_must = false;
            if (clc.Count() > 8)
                paramcalc.b_must = clc[8];

            CalcFull(paramcalc, out ret);
        }
        //--------------------------------------------------------------------------------
        void CalcFull(CalcTypes.ParamCalc paramcalc, out Returns ret)
        //--------------------------------------------------------------------------------
        {
            #region Получить данные по соединению 
            string conn_kernel = Points.GetConnByPref(paramcalc.pref);
            IDbConnection conn_db = GetConnection(conn_kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result)
            {
                return;
            }
            #endregion Получить данные по соединению
            #region выбрать мно-во лицевых счетов
            ChoiseTempKvar(conn_db, ref paramcalc, out ret);
            if (!ret.result)
            {
                conn_db.Close();
                return;
            }
            #endregion выбрать мно-во лицевых счетов
            #region Выполнить расчет
            CalcFull(conn_db, paramcalc, out ret);
            #endregion Выполнить расчет
            #region Удалить временные таблицы и закрыть соединение
            ExecSQL(conn_db, " Drop table t_opn ", false);
            ExecSQL(conn_db, " Drop table t_selkvar ", false);
            ExecSQL(conn_db, " Drop table t_mustcalc ", false);

            DropTempTables(conn_db, paramcalc.pref);


            conn_db.Close();
            #endregion Удалить временные таблицы
        }
        //--------------------------------------------------------------------------------
        void CalcFull(IDbConnection conn_db, CalcTypes.ParamCalc paramcalc, out Returns ret)
        //--------------------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            try
            {

                string s = paramcalc.pref + " " + paramcalc.nzp_kvar + "/" + paramcalc.nzp_dom + "/" +
                           paramcalc.calc_yy + "/" + paramcalc.calc_mm + "/" +
                           paramcalc.cur_yy + "/" + paramcalc.cur_mm;

                MonitorLog.WriteLog("Старт! " + s, MonitorLog.typelog.Info, 1, 2, true);

                if (paramcalc.b_refresh)
                {
                    //распределение оплат 
                    MonitorLog.WriteLog("Старт RefreshAP: " + s, MonitorLog.typelog.Info, 1, 2, true);

                    DbAdres db = new DbAdres();
                    bool b = db.RefreshAP(conn_db, paramcalc.pref, out ret);
                    db.Close();

                    if (!b)
                    {
                        MonitorLog.WriteLog("Ошибка RefreshAP: " + ret.text, MonitorLog.typelog.Error, 222, 222, true);
                        conn_db.Close();
                        return;
                    }
                }
                else
                {
                    //расчеты
                    if (paramcalc.b_gil)
                    {
                        MonitorLog.WriteLog("Старт CalcGilXX: " + s, MonitorLog.typelog.Info, 1, 2, true);
                        if (!CalcGilXX(conn_db, paramcalc, out ret))
                        {
                            conn_db.Close();
                            return;
                        }
                    }
                    if (paramcalc.b_rashod)
                    {
                        MonitorLog.WriteLog("Старт CalcRashod: " + s, MonitorLog.typelog.Info, 1, 2, true);
                        if (!CalcRashod(conn_db, paramcalc, out ret))
                        {
                            conn_db.Close();
                            return;
                        }
                    }
                    if (paramcalc.b_nedo)
                    {
                        MonitorLog.WriteLog("Старт CalcNedo: " + s, MonitorLog.typelog.Info, 1, 2, true);
                        if (!CalcNedoXX(conn_db, paramcalc, out ret))
                        {
                            conn_db.Close();
                            return;
                        }
                    }
                    if (paramcalc.b_gku)
                    {
                        MonitorLog.WriteLog("Старт CalcGkuXX: " + s, MonitorLog.typelog.Info, 1, 2, true);
                        if (!CalcGkuXX(conn_db, paramcalc, out ret))
                        {
                            conn_db.Close();
                            return;
                        }

                        #region Вызов расчета ПЕНИ

                        MonitorLog.WriteLog("Старт CalcRasPeni: " + s, MonitorLog.typelog.Info, 1, 2, true);
                        // Важное условие входа функцию , расчет текущего сальдового месяца , а не перерасчетных месяцев
                        if ((paramcalc.cur_yy == paramcalc.calc_yy) && (paramcalc.cur_mm == paramcalc.calc_mm))
                        {

                            if (!CalcRasPeni(conn_db, paramcalc, out ret))
                            {
                                conn_db.Close();
                                return;
                            }

                        }

                        #endregion Вызов расчета ПЕНИ
                    }

                    if (paramcalc.b_charge)
                    {
                        MonitorLog.WriteLog("Старт CalcChargeXX: " + s, MonitorLog.typelog.Info, 1, 2, true);
                        if (!CalcChargeXX(conn_db, paramcalc, out ret))
                        {
                            conn_db.Close();
                            return;
                        }
                    }

                    if (paramcalc.b_report)
                    {
                        if (paramcalc.calcfon.callReportAlone)
                        {
                            //вызывется после всех через отдельный поток
                        }
                        else
                        {
                            if (!paramcalc.b_reval)
                            {
                                MonitorLog.WriteLog("Старт CalcReportXX: " + s, MonitorLog.typelog.Info, 1, 2, true);
                                CalcReportXX(conn_db, paramcalc, out ret);
                            }
                        }
                    }
                }
                MonitorLog.WriteLog("Стоп! " + s, MonitorLog.typelog.Info, 1, 2, true);
            }
            catch (Exception ex)
            {

                Returns ret11 = STCLINE.KP50.Utility.ExceptionUtility.OnException(ex, System.Reflection.MethodBase.GetCurrentMethod().Name);
            }

        }

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

                CalcFon calcfon = new CalcFon(0);

                calcfon.task = FonTaskTypeIds.taskRefreshAP; //refresh
                calcfon.status = FonTaskStatusId.New; //на выполнение 

                calcfon.nzpt = zap.nzp_wp;
                calcfon.nzp = 0;
                //calcfon.number = Points.GetCalcNum(zap.nzp_wp); //определить номер потока расчета

                calcfon.txt = "'" + zap.point + "'";

                CheckCalcAndPutInFon(conn_web, calcfon, out ret);
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