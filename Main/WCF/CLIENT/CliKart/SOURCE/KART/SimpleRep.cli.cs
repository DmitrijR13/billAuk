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
    public class cli_SimpleRep : I_SimpleRep
    {
        I_SimpleRep remoteObject;

        public cli_SimpleRep(int nzp_server)
            : base()
        {
            remoteObject = HostChannel.CreateInstance<I_SimpleRep>(WCFParams.AdresWcfWeb.Adres + WCFParams.AdresWcfWeb.srvSimpleRep); 
        }

        void _cli_SimpleRep(int nzp_server)
        {
            string addrHost = "";
            //определить параметры доступа
            _RServer zap = MultiHost.GetServer(nzp_server);

            if (Points.IsMultiHost && nzp_server > 0)
            {
                addrHost = zap.ip_adr + WCFParams.AdresWcfWeb.srvSimpleRep;
                remoteObject = HostChannel.CreateInstance<I_SimpleRep>(zap.login, zap.pwd, addrHost);
            }
            else
            {
                //по-умолчанию
                addrHost = WCFParams.AdresWcfWeb.Adres + WCFParams.AdresWcfWeb.srvSimpleRep;
                zap.rcentr = "<локально>";
                remoteObject = HostChannel.CreateInstance<I_SimpleRep>(addrHost);
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

        ~cli_SimpleRep()
        {
            try { if (remoteObject != null) HostChannel.CloseProxy(remoteObject); }
            catch { }
        }




        /// <summary>
        /// Отчет:Выписка из лицевого счета по счетчикам
        /// </summary>
        /// <param name="prm"></param>
        /// <returns></returns>
        public DataTable GetCountersSprav(ReportPrm prm)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                DataTable DT = remoteObject.GetCountersSprav(prm);
                HostChannel.CloseProxy(remoteObject);
                return DT;
            }
            catch (Exception ex)
            {
                ret.result = false;
                if (ex is System.ServiceModel.EndpointNotFoundException)
                {
                    ret.text = Constants.access_error;
                    ret.tag = Constants.access_code;
                }
                else
                    ret.text = "Ошибка вызова сервиса создания простых отчетов";

                MonitorLog.WriteLog("Ошибка GetCountersSprav \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);

                return null;
            }
        }




        /// <summary>
        /// Возвращает набор данных для неотложного отчета
        /// </summary>
        /// <param name="prm"></param>
        /// <returns></returns>
        public DataTable GetReportTable(ReportPrm prm)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                DataTable DT = remoteObject.GetReportTable(prm);
                HostChannel.CloseProxy(remoteObject);
                return DT;
            }
            catch (Exception ex)
            {
                ret.result = false;
                if (ex is System.ServiceModel.EndpointNotFoundException)
                {
                    ret.text = Constants.access_error;
                    ret.tag = Constants.access_code;
                }
                else
                    ret.text = "Ошибка вызова сервиса создания простых отчетов";

                MonitorLog.WriteLog("Ошибка GetReportTable \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);

                return null;
            }
        }

        
  
    }
}
