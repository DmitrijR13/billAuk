using System;
using System.Data;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using System.Net;
using System.Security.Principal;
using System.Collections;
using System.Collections.Generic;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Interfaces;
using STCLINE.KP50.Global;

using STCLINE.KP50.Utility;

namespace STCLINE.KP50.Client
{
    public class cli_Fon : I_FonTask
    {
        I_FonTask remoteObject;

        public cli_Fon(int nzp_server)
            : base()
        {
            _cli_Fon("", nzp_server);
        }

        public cli_Fon(string timespan, int nzp_server)
            : base()
        {
            _cli_Fon(timespan, nzp_server);
        }

        void _cli_Fon(string timespan, int nzp_server)
        {
            if (timespan != "") HostChannel.timespan = timespan;
            string addrHost = "";
            //определить параметры доступа
            _RServer zap = MultiHost.GetServer(nzp_server);

            if (Points.IsMultiHost && nzp_server > 0)
            {
                addrHost = zap.ip_adr + WCFParams.AdresWcfWeb.srvFon;
                remoteObject = HostChannel.CreateInstance<I_FonTask>(zap.login, zap.pwd, addrHost);
            }
            else
            {
                //по-умолчанию
                addrHost = WCFParams.AdresWcfWeb.Adres + WCFParams.AdresWcfWeb.srvFon;
                zap.rcentr = "<локально>";
                remoteObject = HostChannel.CreateInstance<I_FonTask>(addrHost);
            }


            //Попытка открыть канал связи
            try
            {
                ICommunicationObject proxy = remoteObject as ICommunicationObject;
                proxy.Open();
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog(string.Format("Ошибка открытия Proxy {0} по адресу {1}. Сервер {2} (Код РЦ {3}) (Код сервера {4}): {5}",
                                    System.Reflection.MethodBase.GetCurrentMethod().Name,
                                    addrHost,
                                    zap.rcentr,
                                    zap.nzp_rc,
                                    nzp_server,
                                    ex.Message),
                                    MonitorLog.typelog.Error, 2, 100, true);
            }
        }

        ~cli_Fon()
        {
            try { if (remoteObject != null) HostChannel.CloseProxy(remoteObject); }
            catch { }
        }
        //----------------------------------------------------------------------
        public FonProcessorStates PutState(FonProcessorCommands command, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            FonProcessorStates state = FonProcessorStates.Stopped;
            try
            {
                state = remoteObject.PutState(command, out ret);
                HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;

                MonitorLog.WriteLog("Ошибка PutState " + err, MonitorLog.typelog.Error, 2, 100, true);
            }
            return state;
        }

        public FonProcessorStates GetState(out Returns ret)
        {
            ret = Utils.InitReturns();
            FonProcessorStates state = FonProcessorStates.Stopped;
            try
            {
                state = remoteObject.GetState(out ret);
                HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;

                MonitorLog.WriteLog("Ошибка GetState " + err, MonitorLog.typelog.Error, 2, 100, true);
            }
            return state;
        }

        //----------------------------------------------------------------------
        public void CalcFonTask(int number)
        //----------------------------------------------------------------------
        {
            try
            {
                remoteObject.CalcFonTask(number);
                HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;

                MonitorLog.WriteLog("Ошибка CalcFonTask " + number + " " + err, MonitorLog.typelog.Error, 2, 100, true);
            }
        }
        
        /// <summary>
        /// Постановка задачи в очередь
        /// </summary>
        /// <param name="finder"></param>
        /// <returns></returns>
        public static Returns AddTask(CalcFonTask finder)
        {
            finder.Status = FonTask.Statuses.New; //на выполнение 
            finder.QueueNumber = Points.GetCalcNum(0); //определить номер потока расчета
            Returns ret = Utils.InitReturns();

            DbCalcQueueClient cl = new DbCalcQueueClient();
            try
            {
                ret = cl.AddTask(finder);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка AddTask " + finder.QueueNumber + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            finally
            {
                cl.Close();
            }
            return ret;
        }

        /// <summary>
        /// Удаление задачи из очереди
        /// </summary>
        /// <param name="finder"></param>
        /// <returns></returns>
        public static Returns DeleteTasks(CalcFonTask finder)
        {
            // по умолчанию все задачи ставятся в нулевую очередь
            finder.QueueNumber = 0;
            
            Returns ret = Utils.InitReturns();

            DbCalcQueueClient cl = new DbCalcQueueClient();
            try
            {
                ret = cl.DeleteTasks(finder);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка DeleteTasks " + finder.QueueNumber + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            finally
            {
                cl.Close();
            }

            return ret;
        }

        /// <summary>
        /// Проверка наличия аналогичной задачи в очереди
        /// </summary>
        /// <param name="finder"></param>
        /// <returns></returns>
        public static Returns CheckTask(CalcFonTask finder)
        {
            Returns ret = Utils.InitReturns();
            DbCalcQueueClient cl = new DbCalcQueueClient();
            try
            {
                ret = cl.CheckTask(finder);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка CheckTask " + finder.QueueNumber + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            finally
            {
                cl.Close();
            }
            return ret;
        }

    }
}
