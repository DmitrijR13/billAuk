using System;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using System.Net;
using System.Security.Principal;

using System.Threading;

using System.Collections.Generic;
using Bars.KP50.Faktura.Source.Base;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Interfaces;
using STCLINE.KP50.Global;
using Bars.KP50.DB.Faktura;

namespace STCLINE.KP50.Server
{
    /// <summary>
    /// Класс обработки входящих WCF сообщений
    /// </summary>
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.PerCall)]
    public class srv_Fon : srv_Base, I_FonTask //
    {
        public FonProcessorStates PutState(FonProcessorCommands command, out Returns ret)
        {
            ret = Utils.InitReturns();
            TaskQueues.SetCommand(command);
            return TaskQueues.fon_bill.cur_state;
        }

        public FonProcessorStates GetState(out Returns ret)
        {
            ret = Utils.InitReturns();
            return TaskQueues.fon_bill.cur_state;
        }
        
        public FonProcessorStates GetState(int target, out Returns ret)
        {
            ret = Utils.InitReturns();
            return TaskQueues.fon_bill.cur_state;
        }
        
        public void CalcFonTask(int number)
        {
            DbCalc db = new DbCalc();
            try
            {
                db.CalcFonProc(number);
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка srv_Fon.CalcFonTask\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            finally
            {
                db.Close();
            }
        }
    }

    /// <summary>
    /// Очереди фоновых задач
    /// </summary>
    public static class TaskQueues
    {
        public static TaskQueue fon_saldo;
        public static TaskQueue fon_bill;
        public static CalcQueue[] fon_calc = new CalcQueue[CalcThreads.maxCalcThreads];

        public static bool IsSaldoBeingProcessed()
        {
            if (fon_saldo == null) return false;
            else return fon_saldo.cur_state != FonProcessorStates.Work;
        }

        public static bool IsBillBeingProcessed()
        {
            if (fon_bill == null) return false;
            else return fon_bill.cur_state != FonProcessorStates.Work;
        }

        public static void SetCommand(FonProcessorCommands command)
        {
            if (fon_saldo != null) fon_saldo.act_state = command;
            if (fon_bill != null) fon_bill.act_state = command;
            foreach (CalcQueue queue in fon_calc)
                if (queue != null) queue.act_state = command;
        }
    }

    /// <summary>
    /// Базовый обработчик очереди заданий
    /// </summary>
    public abstract class QueueProcessor
    {
        protected TaskQueue queue;

        private QueueProcessor()
        {
        }

        public QueueProcessor(TaskQueue Queue)
        {
            queue = Queue;
        }

        /// <summary>
        /// Считывает и обрабатывает одну задачу. Возвращает true, если задача считана и успешно обработана
        /// </summary>
        /// <returns>Возвращает true, если задача считана и успешно обработана</returns>
        protected abstract bool ProcessOneTask();

        /// <summary>
        /// Основная функция: организует цикл обработки заданий
        /// </summary>
        public virtual void ProcessQueue()
        {
            while (true) //открываем цикл обработки заданий
            {
                if (queue.act_state == FonProcessorCommands.Stop)
                {
                    queue.cur_state = FonProcessorStates.Stopped;
                    queue.act_state = FonProcessorCommands.None;
                }
                else if (queue.act_state == FonProcessorCommands.Start)
                {
                    queue.cur_state = FonProcessorStates.Work;
                    queue.act_state = FonProcessorCommands.None;
                }

                //если состояние в работе, то выполняем задания
                if (queue.cur_state == FonProcessorStates.Work)
                {
                    //if (ProcessOneTask()) continue;
                    ProcessOneTask();
                }

                Thread.Sleep(5000);
            }
        }
    }

    /// <summary>
    /// Предназначен для обработки очереди заданий на расчет сальдо УК, поставщиков и др. там, где нет Финансов 2.0
    /// </summary>
    public class SaldoQueueProcessor: QueueProcessor
    {
        DbCharge db;

        public SaldoQueueProcessor(TaskQueue Queue)
            : base(Queue) 
        {
            DbSaldoQueueClient db = new DbSaldoQueueClient();
            try
            {
                db.PrepareQueue(Queue);
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("SaldoQueueProcessor.SaldoQueueProcessor\n" + ex.Message, MonitorLog.typelog.Error, true);
            }
            finally
            {
                db.Close();
            }
        }

        protected override bool ProcessOneTask()
        {
            db = new DbCharge();
            try
            {
                Returns ret;
                if (db.SaldoFon(out ret)) return true;
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("SaldoQueueProcessor.ProcessOneTask\n" + ex.Message, MonitorLog.typelog.Error, true);
            }
            finally
            {
                db.Close();
            }
            return false;
        }

        /*
        //----------------------------------
        public void FonSaldo()
        //----------------------------------
        {
            DbCharge db = new DbCharge();
            db.SaldoFonTasks(true);

            while (true) //открываем цикл обработки заданий
            {
                if (Fon.fon_saldo.act_state == enFonState.act_break)
                {
                    Fon.fon_saldo.cur_state = enFonState.cur_stop;
                    db.Close();
                    return;
                }
                if (Fon.fon_saldo.act_state == enFonState.act_start)
                {
                    Fon.fon_saldo.cur_state = enFonState.cur_work;
                    Fon.fon_saldo.act_state = enFonState.none;
                }

                //если состояние в работе, то выполняем задания
                if (Fon.fon_saldo.cur_state == enFonState.cur_work)
                {
                    if (db.SaldoFonTasks(false))
                    {
                        db.SaldoFonOther();
                    }
                }
                Thread.Sleep(5000);
            }
        }
        //----------------------------------
        public void FonSaldo()
        //----------------------------------
        {
            while (true) //открываем цикл обработки заданий
            {
                if (Fon.fon_saldo.act_state == enFonState.act_break)
                {
                    Fon.fon_saldo.cur_state = enFonState.cur_stop;
                    return;
                }
                if (Fon.fon_saldo.act_state == enFonState.act_start)
                {
                    Fon.fon_saldo.cur_state = enFonState.cur_work;
                    Fon.fon_saldo.act_state = enFonState.none;
                }

                //если состояние в работе, то выполняем задания
                if (Fon.fon_saldo.cur_state == enFonState.cur_work)
                {
                }
                Thread.Sleep(5000);
            }
        }
        */
    }

    /// <summary>
    /// Предназначен для обработки очереди заданий (расчета, распределения оплат и др.)
    /// </summary>
    public class CalcQueueProcessor: QueueProcessor
    {
        DbCalc db;

        public CalcQueueProcessor(CalcQueue Queue)
            : base(Queue) 
        {
            DbCalcQueueClient db = new DbCalcQueueClient();
            try
            {
                db.PrepareQueue(Queue);
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("CalcQueueProcessor.CalcQueueProcessor\n" + ex.Message, MonitorLog.typelog.Error, true);
            }
            finally
            {
                db.Close();
            }
        }

        protected override bool ProcessOneTask()
        {
            db = new DbCalc();
            try
            {
                if (db.CalcFonProc(((CalcQueue)queue).Number)) return true;
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("CalcQueueProcessor.ProcessOneTask\n" + ex.Message, MonitorLog.typelog.Error, true);
            }
            finally
            {
                db.Close();
            }
            return false;
        }
    }

    /// <summary>
    /// Предназначен для обработки очереди заданий на формирование платежных документов
    /// </summary>
    public class BillQueueProcessor: QueueProcessor
    {
        DbFakturaCover _db;

        public BillQueueProcessor(TaskQueue Queue)
            : base(Queue)
        {
            var db = new DbBillQueueClient();
            try
            {
                db.PrepareQueue(Queue);
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("BillQueueProcessor.BillQueueProcessor\n" + ex.Message, MonitorLog.typelog.Error, true);
            }
            finally
            {
                db.Close();
            }
        }

        protected override bool ProcessOneTask()
        {
            _db = new DbFakturaCover();
            try
            {
                if (_db.ProcessBillFon()) return true;
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("BillQueueProcessor.ProcessOneTask\n" + ex.Message, MonitorLog.typelog.Error, true);
            }
         

            return false;
        }

    }
}
