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
    public class cli_Multi : I_Multi
    {
        I_Multi remoteObject;

        public cli_Multi(int nzp_server)
            : base()
        {
            _cli_Multi(nzp_server);
        }

        void _cli_Multi(int nzp_server)
        {
            string addrHost = "";
            //определить параметры доступа
            _RServer zap = MultiHost.GetServer(nzp_server);

            if (Points.IsMultiHost && nzp_server > 0)
            {
                addrHost = zap.ip_adr + WCFParams.AdresWcfWeb.srvMulti;
                remoteObject = HostChannel.CreateInstance<I_Multi>(zap.login, zap.pwd, addrHost);
            }
            else
            {
                //по-умолчанию
                addrHost = WCFParams.AdresWcfWeb.Adres + WCFParams.AdresWcfWeb.srvMulti;
                zap.rcentr = "<локально>";
                remoteObject = HostChannel.CreateInstance<I_Multi>(addrHost);
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

        ~cli_Multi()
        {
            try { if (remoteObject != null) HostChannel.CloseProxy(remoteObject); }
            catch { }
        }

        List<UserStateOfServers> res = new List<UserStateOfServers>();

        //----------------------------------------------------------------------
        public List<UserStateOfServers> ServersStateByUser(int nzp_user, enSrvOper oper, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            try
            {
                res = remoteObject.ServersStateByUser(nzp_user, oper, out ret);
                HostChannel.CloseProxy(remoteObject);
                return res;
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = "Ошибка вызова функции";
                if (Constants.Viewerror) ret.text += ": " + ex.Message;
                if (Constants.Debug) MonitorLog.WriteLog("Ошибка ServersStateByUser(" + oper.ToString() + ")\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                return null;
            }
        }
    }
}
