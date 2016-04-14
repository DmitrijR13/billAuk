using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;

namespace STCLINE.KP50.DataBase
{
    public class DbCalcQueueClient : DbQueueClient
    {
        public DbCalcQueueClient() : base() { }
        public DbCalcQueueClient(CalcFonTask Task) : base(Task) { }

        public override Returns PrepareQueue(IDbConnection conn_web, TaskQueue queue)
        {
            return prepareQueue(conn_web, queue);
        }

        public static Returns PrepareQueue(IDbConnection conn_web, int queueNumber)
        {
            return prepareQueue(conn_web, new CalcQueue(FonProcessorCommands.None, queueNumber) { Number = queueNumber });
        }

        private static Returns prepareQueue(IDbConnection conn_web, TaskQueue queue)
        {
            string tab = "calc_fon_" + ((CalcQueue)queue).Number;

            string sql;
            Returns ret = Utils.InitReturns();
#if PG
            DBManager.ExecSQL(conn_web, "set search_path to 'public'", true);
#endif

            if (!DBManager.TempTableInWebCashe(conn_web, tab))
            {
                sql = " Create table " + tab +
                " ( nzp_key   serial  not null, " +
                "   nzp       integer default 0 not null, " +
                "   nzp_user  integer default 0 not null, " +
                "   nzpt      integer default 0 not null, " +
                "   year_     integer default 0 not null, " +
                "   month_    integer default 0 not null, " +
                "   task      integer default 0 not null, " +
                "   prior     integer default 0 not null, " +
                "   kod_info  integer default 0 not null, " +
                "   dat_in    " + DBManager.sDateTimeType + ", " +
                "   dat_work  " + DBManager.sDateTimeType + ",  " +
                "   dat_out   " + DBManager.sDateTimeType + ", " +
                "   dat_when  " + DBManager.sDateTimeType + ", " +
                "   parameters char(2000), " +
                "   txt       char(255), " +
                "   progress  " + sDecimalType + "(6,4) default 0," +
                "   ip_adr    char(15) " + sLockMode;

                ret = DBManager.ExecSQL(conn_web, sql, true);
                if (!ret.result)
                {
                    return ret;
                }

                if (ret.result) { ret = DBManager.ExecSQL(conn_web, " Create unique index ix_" + tab + "_1 on " + tab + " (nzp_key) ", true); }
                if (ret.result) { ret = DBManager.ExecSQL(conn_web, " Create        index ix_" + tab + "_2 on " + tab + " (nzp,nzpt,year_,month_,kod_info,nzp_user) ", true); }
                if (ret.result) { ret = DBManager.ExecSQL(conn_web, " Create        index ix_" + tab + "_3 on " + tab + " (nzpt,kod_info) ", true); }
                if (ret.result) { ret = DBManager.ExecSQL(conn_web, " Create        index ix_" + tab + "_4 on " + tab + " (prior) ", true); }

                if (ret.result) { ret = DBManager.ExecSQL(conn_web, DBManager.sUpdStat + " " + tab, true); }
                else
                {
                    return ret;
                }
            }

            ret = ReQueueOldTasks(conn_web, tab);

            return ret;
        }

        public override Returns AddTask(IDbConnection conn_web, IDbTransaction transaction, FonTask fonTask)
        {
            CalcFonTask calcfon = (CalcFonTask)fonTask;

            Returns ret = Utils.InitReturns();

            string tab = sDefaultSchema + "calc_fon_" + calcfon.QueueNumber;

            //#if PG
            //            DBManager.ExecSQL(conn_web, transaction, "set search_path to 'public'", true);
            //#else
            //#endif

            #region проверить, что аналогичные задания уже выполняются или в очереди

            ret = CheckTask(conn_web, calcfon);
            if (!ret.result) return ret;
            #endregion

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
                MonitorLog.WriteLog("AddTask(IDbConnection conn_web, IDbTransaction transaction, FonTask fonTask)\nОшибка при определении ip адреса\n" + ex, MonitorLog.typelog.Error, true);
                ipAdr = String.Empty;
            }
#endif

            #region Установка задания в очередь
            int nzp_dom = calcfon.nzp;
            var defaultStatus = FonTask.Statuses.InQueue;
            //для распределения прежде удалим задания
            switch (calcfon.TaskType)
            {
                case CalcFonTask.Types.SetNotNull:
                case CalcFonTask.Types.AddForeignKey:
                case CalcFonTask.Types.AddIndexes:
                case CalcFonTask.Types.AddPrimaryKey:
                case CalcFonTask.Types.OrderSequences:
                case CalcFonTask.Types.taskCalculateAnalytics:
                case CalcFonTask.Types.taskUpdateAddress:
                case CalcFonTask.Types.taskRecalcDistribSumOutSaldo:
                case CalcFonTask.Types.taskCalcSaldoSubsidy:
                case CalcFonTask.Types.taskStartControlPays:
                case CalcFonTask.Types.taskCalcSubsidyRequest:
                case CalcFonTask.Types.DistributeOneLs:
                case CalcFonTask.Types.CancelDistributionAndDeletePack:
                case CalcFonTask.Types.CancelPackDistribution:
                case CalcFonTask.Types.DistributePack:
                    {
                        ret = DBManager.ExecSQL(conn_web,
                            " Delete From " + tab +
                            " Where nzp   = " + calcfon.nzp +
                            " and nzpt  = " + calcfon.nzpt +
                            " and year_ = " + calcfon.year_ +
                            " and month_= " + calcfon.month_ +
                            " and task  = " + (int)calcfon.TaskType
                            , true);
                        if (!ret.result) return ret;
                    }
                    break;
                case CalcFonTask.Types.taskAutomaticallyChangeOperDay:
                    {
                        ret = DBManager.ExecSQL(conn_web,
                            " Delete From " + tab +
                            " Where task  = " + (int)calcfon.Task +
                            " and dat_when > " + sCurDateTime
                            , true);
                        if (!ret.result) return ret;
                    }
                    break;
                case CalcFonTask.Types.taskWithRevalOntoListHouses:
                    {
                        //чтобы расчет начался только после команды из вызывающего потока
                        defaultStatus = FonTask.Statuses.New;
                    }
                    break;
                case CalcFonTask.Types.taskDisassembleFile:
                case CalcFonTask.Types.taskLoadFile:
                case CalcFonTask.Types.taskLoadFileFromSZ:
                case CalcFonTask.Types.taskLoadFileFromSZpss:
                case CalcFonTask.Types.taskLoadFileOnly:
                case CalcFonTask.Types.taskCheckStep:
                case CalcFonTask.Types.taskUnloadFileForSZ:
                case CalcFonTask.Types.taskLoadKladr:
                case CalcFonTask.Types.taskGeneratePkod:
                case CalcFonTask.Types.taskToTransfer:
                case CalcFonTask.Types.ReCalcKomiss:
                case CalcFonTask.Types.taskPreparePrintInvoices:
                case CalcFonTask.Types.uchetOplatArea:
                case CalcFonTask.Types.uchetOplatBank:
                case CalcFonTask.Types.taskChangeOperDay:
                case CalcFonTask.Types.taskCalcChargeForReestr:
                case CalcFonTask.Types.taskBalanceSelect:
                case CalcFonTask.Types.taskBalanceRedistr:
                case CalcFonTask.Types.CheckBeforeClosingMonth:
                case CalcFonTask.Types.TaskRefreshLSTarif:
                case CalcFonTask.Types.taskCalcChargeForDelReestr:
                case CalcFonTask.Types.taskExportParam:
                case CalcFonTask.Types.taskGenLsPu:
                    break;
                default:
                    {
                        calcfon.txt = CalcFonComment(conn_web, calcfon.QueueNumber, calcfon.nzpt, nzp_dom, false);
                        break;
                    }
            }
         
            string sql = " Insert into " + tab +
                         " (nzp,task, nzpt,year_,month_,kod_info,dat_in, txt, nzp_user, dat_when, parameters" +
                         (ipAdr.Trim() != String.Empty ? ", ip_adr" : String.Empty) + ") " +
                         " Values (" + calcfon.nzp +
                         ", " + (int)calcfon.TaskType +
                         ", " + calcfon.nzpt +
                         ", " + calcfon.year_ +
                         ", " + calcfon.month_ +
                         ", " + (int)defaultStatus +
                         "," + sCurDateTime +
                         ", " + Utils.EStrNull(calcfon.txt) +
                         ", " + calcfon.nzp_user +
                         ", " +
                         (calcfon.dat_when != null && calcfon.dat_when.Trim() == ""
                             ? "null"
                             : Utils.EStrNull(calcfon.dat_when)) +
                         ", " + Utils.EStrNull(calcfon.parameters, 2000, "NULL") +
                         (ipAdr.Trim() != String.Empty ? ", '" + ipAdr.Trim() + "'" : String.Empty) +
                         ")";

            ret = DBManager.ExecSQL(conn_web, sql, true);
            if (!ret.result)
            {
                ret.text = "Ошибка добавления задания";
                return ret;
            }
            else
            {
                ret.tag = DBManager.GetSerialValue(conn_web);
                ret.text = calcfon.QueueNumber.ToString();
            }
            #endregion
            if (calcfon.TaskType != CalcFonTask.Types.taskGeneratePkod)
                ret.text = "Расчет выполняется, ждите!";

            return ret;
        }

        public override Returns CloseTask(IDbConnection conn_web, IDbTransaction transaction, FonTask fonTask)
        {
            CalcFonTask calcfon = (CalcFonTask)fonTask;

            Returns ret = Utils.InitReturns();

            string tab = sDefaultSchema + "calc_fon_" + calcfon.QueueNumber;

            //#if PG
            //            DBManager.ExecSQL(conn_web, "set search_path to 'public'", true);
            //#else
            //#endif

            ret = DBManager.ExecSQL(conn_web,
              " Update " + tab +
              " Set dat_out = " + sCurDateTime +
                 ", txt = trim(txt) || trim(" + Utils.EStrNull(calcfon.txt, " ") + ")" +
                 ", kod_info = " + (int)calcfon.Status +
              " Where nzp_key   = " + calcfon.nzp_key
              , true);
            return ret;
        }

        public Returns DeleteTasks(CalcFonTask calcfon)
        {
            if (calcfon.TaskType == CalcFonTask.Types.Unknown) return new Returns(false, "Не указан тип задачи");

            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            Returns ret = OpenDb(conn_web, true);

            if (!ret.result) return ret;

            try
            {
                string tab = "calc_fon_" + calcfon.QueueNumber;

                string sql = " Delete From " + tab +
                    " Where task = " + (int)calcfon.TaskType +
                    (calcfon.year_ > 0 ? " and year_ = " + calcfon.year_ : "") +
                    (calcfon.month_ > 0 ? " and month_ = " + calcfon.month_ : "") +
                    (calcfon.nzpt > 0 ? " and nzpt = " + calcfon.nzpt : "") +
                    (calcfon.nzp > 0 ? " and nzp = " + calcfon.nzpt : "") +
                    (calcfon.dat_when != null && calcfon.dat_when.Trim() != "" ? " and dat_when > " + sCurDateTime : "") +
                    (calcfon.nzp_key > 0 ? " and nzp_key = " + calcfon.nzp_key : "") +
                    " and kod_info in (" + (int)FonTask.Statuses.InProcess + "," + (int)FonTask.Statuses.InQueue + ") ";

                ret = ExecSQL(conn_web, sql, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Необработанное исключение в DeleteTask\n" + ex.Message, MonitorLog.typelog.Error, true);
            }
            finally
            {
                conn_web.Close();
            }
            return ret;
        }

        public Returns CheckTask(CalcFonTask calcfon)
        {
            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            Returns ret = OpenDb(conn_web, true);
            if (!ret.result) return ret;

            ret = CheckTask(conn_web, calcfon);

            conn_web.Close();

            return ret;
        }

        /// <summary>
        /// Проверка на выполнение аналогичных задач 
        /// </summary>
        /// <param name="conn_web">подключение</param>
        /// <param name="calcfon"></param>
        /// <returns>результат</returns>
        public static Returns CheckTask(IDbConnection conn_web, CalcFonTask calcfon)
        {
            StringBuilder sql = new StringBuilder();
            Returns ret = Utils.InitReturns();

            try
            {
                //#if PG
                //                DBManager.ExecSQL(conn_web, "set search_path to 'public'", true);
                //#endif

                if (calcfon.TaskType == CalcFonTask.Types.taskRePrepareProvOnClosedCalcMonth ) return ret;

                string tab = sDefaultSchema + "calc_fon_" + calcfon.QueueNumber;

                sql.AppendFormat(" Select {0}(kod_info,{1}) as kod_info, progress From {2}", DBManager.sNvlWord, FonTask.Statuses.None.GetHashCode(), tab);

                if (CalcFonTask.TaskCalc(calcfon.TaskType))
                {
                    sql.AppendFormat(" Where task in ({0}", (int)CalcFonTask.Types.taskDefault);
                    sql.AppendFormat(",{0}", (int)CalcFonTask.Types.taskFull);
                    sql.AppendFormat(",{0}", (int)CalcFonTask.Types.taskSaldo);
                    sql.AppendFormat(",{0}", (int)CalcFonTask.Types.taskWithReval);
                    sql.AppendFormat(",{0}", (int)CalcFonTask.Types.taskWithRevalOntoListHouses);
                    sql.AppendFormat(",{0}", (int)CalcFonTask.Types.taskCalcGil);
                    sql.AppendFormat(",{0}", (int)CalcFonTask.Types.taskCalcRashod);
                    sql.AppendFormat(",{0}", (int)CalcFonTask.Types.taskCalcNedo);
                    sql.AppendFormat(",{0}", (int)CalcFonTask.Types.taskCalcGku);
                    sql.AppendFormat(",{0}", (int)CalcFonTask.Types.taskCalcCharge);
                    sql.AppendFormat(",{0})", (int)CalcFonTask.Types.taskCalcReport);
                    sql.AppendFormat(" and (nzp = 0 or nzp = {0} )", calcfon.nzp);
                }
                else
                {
                    sql.AppendFormat(" Where task = {0}", (int)calcfon.TaskType);
                    sql.AppendFormat(" and nzp = {0}", calcfon.nzp);
                }

                sql.AppendFormat(" and year_ ={0}", calcfon.year_);
                sql.AppendFormat(" and month_= {0}", calcfon.month_);
                sql.AppendFormat(" and nzpt = {0}", calcfon.nzpt);
                sql.AppendFormat(" and kod_info in ({0},{1}) ", (int)FonTask.Statuses.InProcess, (int)FonTask.Statuses.InQueue);
                sql.Append((calcfon.dat_when != null && calcfon.dat_when.Trim() != "")
                    ? " and dat_when = " + Utils.EStrNull(calcfon.dat_when)
                    : "");

                DataTable dt = DBManager.ExecSQLToTable(conn_web, sql.ToString());
                //var kod_info = DBManager.ExecScalar(conn_web, sql.ToString(), out ret, true);
                //if (!ret.result) return ret;

                if (dt.Rows.Count > 0)
                {
                    DataRow row = dt.Rows[0];

                    switch (FonTask.GetStatusById(Convert.ToInt32(row["kod_info"] != DBNull.Value ? row["kod_info"] : FonTask.Statuses.None.GetHashCode())))
                    {
                        case FonTask.Statuses.InProcess:
                            {
                                ret.text = "Выполняется фоновый процесс расчета. Пожалуйста, проверьте данные позднее.";
                                break;
                            }
                        case FonTask.Statuses.InQueue:
                            {
                                ret.text = "Задание уже находится в очереди. Пожалуйста, проверьте данные позднее.";
                                break;
                            }
                        default:
                            {
                                ret.text = "";
                                break;
                            }
                    }
                    ret.result = false;
                    ret.tag = Constants.workinfon;
                    calcfon.progress = Convert.ToDecimal(row["progress"] != DBNull.Value ? row["progress"] : 0);
                }

                return ret;
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = "Ошибка проверки фоновых процессов на выполнение аналогичных задач.";
                MonitorLog.WriteException(ret.text, ex);
                return ret;
            }
        }


        public static string CalcFonComment(IDbConnection conn_web, int number, int nzp_wp, int nzp, bool pack)
        {
            MyDataReader reader;
            if (!pack)
            {
                if (nzp == 0)
                {
                    foreach (_Point zap in Points.PointList)
                    {
                        if (zap.nzp_wp != nzp_wp) continue;
                        return "'" + zap.point.Replace("'", " ") + "'";
                    }
                    return "'???'";
                }
                else
                {
                    if (!DBManager.TempTableInWebCashe(conn_web, sDefaultSchema + "anl" + Points.CalcMonth.year_ + "_dom"))
                    {
                        MonitorLog.WriteLog(
                            "Ошибка CalcFonComment : таблица " + sDefaultSchema + "anl" + Points.CalcMonth.year_ +
                            "_dom не существует", MonitorLog.typelog.Error, 20, 201, true);
                        return " 'Дом: адрес не определен' ";
                    }
                    //вытащить дом
                    string sql =
                        " Select ulica, ndom, point " +
                        " From " + sDefaultSchema + "anl" + Points.CalcMonth.year_ + "_dom " +
                        " Where nzp_dom = " + nzp;
                    if (!DBManager.ExecRead(conn_web, out reader, sql, true).result)
                    {
                        return " 'Дом: адрес не определен' ";
                    }
                    try
                    {
                        string s = "";
                        string s1 = "";
                        if (reader.Read())
                        {
                            if (reader["ulica"] != DBNull.Value)
                            {
                                s1 = (string)reader["ulica"];
                                //s += " ул. " + s1.Trim();
                            }
                            if (reader["ndom"] != DBNull.Value)
                            {
                                s1 = (string)reader["ndom"];
                                s += " " + s1.Trim();
                            }
                            if (reader["point"] != DBNull.Value)
                            {
                                s1 = (string)reader["point"];
                                s += " " + s1.Trim();
                            }
                        }
                        else
                        {
                            s = " Дом: адрес не определен ";
                        }
                        reader.Close();

                        return " '" + s + "' ";
                    }
                    catch (Exception ex)
                    {
                        reader.Close();
                        //conn_web.Close();

                        string err;
                        if (Constants.Viewerror)
                            err = " \n " + ex.Message;
                        else
                            err = "";

                        MonitorLog.WriteLog("Ошибка CalcFonComment " + err, MonitorLog.typelog.Error, 20, 201, true);

                        return " 'Дом: адрес не определен' ";
                    }
                }
            }
            return "'???'";
        }

        /// <summary>
        /// Постановка в очередь задания на расчет сальдо
        /// </summary>
        /// <param name="nzp_wp">Код банка данных (необязательный)</param>
        /// <param name="ret"></param>
        public void CalcSaldo(int nzp_wp, out Returns ret)
        {
            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result) return;

            //цикл по всем локальным банкам 
            foreach (_Point zap in Points.PointList)
            {
                if (nzp_wp > 0 && zap.nzp_wp != nzp_wp) continue;

                //загнать задания на подсчет сальдо
                CalcFonTask calcfon = new CalcFonTask(Points.GetCalcNum(zap.nzp_wp));
                calcfon.TaskType = CalcFonTask.Types.taskSaldo; //расчет сальдо
                calcfon.Status = FonTask.Statuses.New;
                calcfon.nzpt = zap.nzp_wp;
                calcfon.nzp = 0;

                ret = AddTask(conn_web, null, calcfon);
                if (!ret.result)
                {
                    break;
                }
            }
            conn_web.Close();
        }

        /// <summary>
        /// Обновляет процент выполнения задания
        /// </summary>
        /// <param name="finder"></param>
        /// <returns></returns>
        public Returns SetTaskProgress(int queueNumber, int taskId, decimal progress)
        {
            return SetTaskProgress(taskId, progress, "calc_fon_" + queueNumber);
        }

        /// <summary>
        /// Обновляет процент выполнения задачи
        /// </summary>
        /// <param name="progress">Этап выполнения задачи от 0 до 1</param>
        /// <returns></returns>
        public delegate Returns SetTaskProgressDelegate(decimal progress);

        public Returns SetTaskProgress(decimal progress)
        {
            return SetTaskProgress(((CalcFonTask)task).QueueNumber, task.nzp_key, progress);
        }

        /// <summary> Получить список фоновых процессов расчета начислений
        /// </summary>
        public List<CalcFonTask> GetProcessCalc(CalcFonTask finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            string where = makeWhereForProcess(finder, "p", ref ret);
            if (where != "") where = " Where 1=1 " + where;
            if (!ret.result) return null;

            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result) return null;

            string skip = "";
#if PG
            if (finder.skip > 0) skip = " offset " + finder.skip.ToString();
#else
            if (finder.skip > 0) skip = " skip " + finder.skip.ToString();
#endif

            string table, sql, fields = "";

            if (finder.QueueNumber != Constants._ZERO_)
            {
                table = "calc_fon_" + finder.QueueNumber;

                if (!TableInWebCashe(conn_web, table))
                {
                    conn_web.Close();
                    return new List<CalcFonTask>();
                }
            }
            else
            {
                string tmpTable = "t" + finder.nzp_user + "_tmp_calc_fon";
                sql = "drop table " + tmpTable;
                ExecSQL(conn_web, sql, false);

                sql = "CREATE temp TABLE " + tmpTable + " (" +
                      " nzp_key INTEGER NOT NULL" +
                      ", queue INTEGER NOT NULL" +
                      ", nzp INTEGER default 0 NOT NULL" +
                      ", nzpt INTEGER default 0 NOT NULL" +
                      ", year_ INTEGER default 0 NOT NULL" +
                      ", month_ INTEGER default 0 NOT NULL" +
                      ", task INTEGER default 0 NOT NULL" +
                      ", prior INTEGER default 0 NOT NULL" +
                      ", kod_info INTEGER default 0 NOT NULL" +
                      ", dat_in " + sDateTimeType +
                      ", dat_work " + sDateTimeType +
                      ", dat_out " + sDateTimeType +
                      ", txt CHAR(255)" +
                      ", progress " + sDecimalType + "(6,4))" + sUnlogTempTable;

                ret = ExecSQL(conn_web, sql, true);

                if (!ret.result)
                {
                    conn_web.Close();
                    return null;
                }

                for (int i = 0; i < 5; i++)
                {
                    table = "calc_fon_" + i;
                    if (!TableInWebCashe(conn_web, table)) continue;

                    sql = "Insert into " + tmpTable +
                          " (nzp_key, queue, nzp, nzpt, year_, month_, task, prior, kod_info, dat_in, dat_work, dat_out, txt, progress)" +
                          " Select p.nzp_key, " + i + ", p.nzp, p.nzpt, p.year_, p.month_, p.task, p.prior, p.kod_info, p.dat_in, p.dat_work, p.dat_out, p.txt, p.progress" +
                          " From " + table + " p" +
                          where;
                    ret = ExecSQL(conn_web, sql, true);
                    if (!ret.result)
                    {
                        conn_web.Close();
                        return null;
                    }
                }

                table = tmpTable;
                where = "";
                fields = ", queue";
            }

            int total_record_count = 0;
            object count = ExecScalar(conn_web, " Select count(*) From " + table + " p" + where, out ret, true);
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


#if PG
            sql = " Select " +
#else
            sql = " Select " + skip +
#endif
 " p.nzp_key, p.nzp, p.nzpt, p.year_, p.month_, p.task, p.prior, p.kod_info, p.dat_in, p.dat_work, p.dat_out, p.txt, p.progress" + fields +
                " From " + table + " p" +
                  where +
                  " Order by dat_in desc, year_, month_" +
#if PG
 skip;
#else
                "";
#endif

            MyDataReader reader;

            ret = ExecRead(conn_web, out reader, sql, true);
            if (!ret.result)
            {
                return null;
            }
            try
            {
                List<CalcFonTask> Spis = new List<CalcFonTask>();

                int i = 0;
                while (reader.Read())
                {
                    CalcFonTask zap = new CalcFonTask();

                    zap.num = ++i + finder.skip;

                    if (reader["nzp_key"] != DBNull.Value) zap.nzp_key = Convert.ToInt32(reader["nzp_key"]);
                    if (reader["nzp"] != DBNull.Value) zap.nzp = Convert.ToInt32(reader["nzp"]);
                    if (reader["nzpt"] != DBNull.Value) zap.nzpt = Convert.ToInt32(reader["nzpt"]);
                    if (reader["year_"] != DBNull.Value) zap.year_ = Convert.ToInt32(reader["year_"]);
                    if (reader["month_"] != DBNull.Value) zap.month_ = Convert.ToInt32(reader["month_"]);
                    if (reader["task"] != DBNull.Value) zap.Task = Convert.ToInt32(reader["task"]);
                    if (reader["prior"] != DBNull.Value) zap.prior = Convert.ToInt32(reader["prior"]);
                    if (reader["kod_info"] != DBNull.Value) zap.KodInfo = Convert.ToInt32(reader["kod_info"]);
                    if (reader["dat_in"] != DBNull.Value)
                        zap.dat_in = String.Format("{0:dd.MM.yyyy HH:mm}", reader["dat_in"]);
                    if (reader["dat_work"] != DBNull.Value)
                        zap.dat_work = String.Format("{0:dd.MM.yyyy HH:mm}", reader["dat_work"]);
                    if (reader["dat_out"] != DBNull.Value)
                        zap.dat_out = String.Format("{0:dd.MM.yyyy HH:mm}", reader["dat_out"]);
                    if (reader["txt"] != DBNull.Value) zap.txt = Convert.ToString(reader["txt"]).Trim();

                    if (finder.QueueNumber != Constants._ZERO_) zap.QueueNumber = finder.QueueNumber;
                    else if (reader["queue"] != DBNull.Value) zap.QueueNumber = Convert.ToInt32(reader["queue"]);

                    if (reader["progress"] != DBNull.Value) zap.progress = Convert.ToDecimal(reader["progress"]);

                    Spis.Add(zap);

                    if (i >= finder.rows) break;
                }

                reader.Close();
                ret.tag = total_record_count;
                return Spis;
            }
            catch (Exception ex)
            {
                reader.Close();

                ret.result = false;
                ret.text = ex.Message;

                MonitorLog.WriteLog(
                    "Ошибка в функции GetProcessCalc заполнения списка процессов " +
                    (Constants.Viewerror ? "\n" + ex.Message : ""), MonitorLog.typelog.Error, 20, 201, true);

                return null;
            }
            finally
            {
                conn_web.Close();
            }
        }

        private string makeWhereForProcess(CalcFonTask finder, string alias, ref Returns ret)
        {
            string where = makeWhereForProcess(finder as FonTaskWithYearMonth, alias, ref ret);

            if (alias != "") alias = alias + ".";

            if (finder.nzp >= 0) where += " and " + alias + "nzp = " + finder.nzp;
            if (finder.nzpt > 0) where += " and " + alias + "nzpt = " + finder.nzpt;
            if (finder.TaskType != CalcFonTask.Types.Unknown && finder.TaskType != CalcFonTask.Types.taskDefault) where += " and " + alias + "task = " + (int)finder.TaskType;
            if (finder.prior > 0) where += " and " + alias + "prior = " + finder.prior;

            return where;
        }

        public Returns DeleteProcessCalc(CalcFonTask proc)
        {
            Returns ret = Utils.InitReturns();
            string where = makeWhereForProcess(proc, "", ref ret);
            if (!ret.result) return ret;

            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result) return ret;

            string table;
            for (int i = 0; i < 5; i++)
            {
                if (proc.QueueNumber != Constants._ZERO_ && proc.QueueNumber != i) continue;
                table = "calc_fon_" + i;
                if (!TableInWebCashe(conn_web, table)) continue;

                string sql = "delete from " + table + " Where 1=1 " + where;
                ret = ExecSQL(conn_web, sql, true);
                if (!ret.result)
                {
                    conn_web.Close();
                    return ret;
                }
            }

            ExecSQL(conn_web, "drop table public.t" + proc.QueueNumber + proc.nzp_key, false);

            conn_web.Close();
            return ret;
        }

        public Returns SaveProcessCalc(CalcFonTask proc)
        {
            if (proc.nzp_user < 1) return new Returns(false, "Не определен пользователь");

            Returns ret;

            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result) return ret;

            string table = "calc_fon_" + proc.QueueNumber;

            if (!TableInWebCashe(conn_web, table))
            {
                conn_web.Close();
                return new Returns(false, "Не найдена таблица для сохранения задания");
            }

            string sql;

            if (proc.nzp_key > 0)
            {
                sql = "Update " + table + " set txt = " + Utils.EStrNull(proc.txt.Trim(), "") +
                      " Where nzp_key = " + proc.nzp_key;
                ret = ExecSQL(conn_web, sql, true);
            }
            else
            {
                IDbTransaction transaction;
                try
                {
                    transaction = conn_web.BeginTransaction();
                }
                catch
                {
                    transaction = null;
                }

                int numMonths;
                if (proc.year_ == proc.year_po)
                    numMonths = proc.month_po - proc.month_ + 1;
                else if (proc.year_po > proc.year_)
                    numMonths = proc.year_po * 12 + proc.month_po - proc.year_ * 12 - proc.month_ + 1;
                else
                    numMonths = 0;

                int y = proc.year_, m = proc.month_;
                for (int i = 0; i < numMonths; i++)
                {
                    sql = "insert into " + table + " (nzp, nzpt, year_, month_, task, prior, kod_info, dat_in, txt)" +
                          " values (" + proc.nzp + ", " + proc.nzpt + ", " + y + ", " + m + ", " + (int)proc.TaskType + ", " +
                          proc.prior + ", " + FonTask.getKodInfo(Constants.act_process_in_queue) +
                          ", current, " + Utils.EStrNull(proc.txt.Trim(), "") + ")";
                    ret = ExecSQL(conn_web, transaction, sql, true);
                    if (!ret.result)
                    {
                        if (transaction != null) transaction.Rollback();
                        conn_web.Close();
                        return ret;
                    }

                    if (m < 12) m++;
                    else
                    {
                        m = 1;
                        y++;
                    }
                }

                if (transaction != null) transaction.Commit();
            }
            conn_web.Close();

            return ret;
        }
    }
}
