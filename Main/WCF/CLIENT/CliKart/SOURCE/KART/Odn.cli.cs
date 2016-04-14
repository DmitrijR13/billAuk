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
    public class cli_Odn : I_Odn
    {
        I_Odn remoteObject;

        public cli_Odn(int nzp_server)
            : base()
        {
            _cli_Odn(nzp_server);
        }

        void _cli_Odn(int nzp_server)
        {
            string addrHost = "";
            //определить параметры доступа
            _RServer zap = MultiHost.GetServer(nzp_server);

            if (Points.IsMultiHost && nzp_server > 0)
            {
                addrHost = zap.ip_adr + WCFParams.AdresWcfWeb.srvOdn;
                remoteObject = HostChannel.CreateInstance<I_Odn>(zap.login, zap.pwd, addrHost);
            }
            else
            {
                //по-умолчанию
                addrHost = WCFParams.AdresWcfWeb.Adres + WCFParams.AdresWcfWeb.srvOdn;
                zap.rcentr = "<локально>";
                remoteObject = HostChannel.CreateInstance<I_Odn>(addrHost);
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

        ~cli_Odn()
        {
            try { if (remoteObject != null) HostChannel.CloseProxy(remoteObject); }
            catch { }
        }

        List<Odn> odnSpis = new List<Odn>();

        //----------------------------------------------------------------------
        public List<Odn> GetOdn(OdnFinder finder, enSrvOper oper, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            try
            {
                odnSpis = remoteObject.GetOdn(finder, oper, out ret);
                HostChannel.CloseProxy(remoteObject);
                return odnSpis;
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = "Ошибка вызова функции";
                if (Constants.Viewerror) ret.text += ": " + ex.Message;
                if (Constants.Debug) MonitorLog.WriteLog("Ошибка GetOdn(" + oper.ToString() + ")\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                return null;
            }
        }

        //----------------------------------------------------------------------
        public string DbMakeWhereString(OdnFinder finder, out Returns ret, enDopFindType tip)
        //----------------------------------------------------------------------
        {
            DbOdnClient db = new DbOdnClient();
            string res = db.MakeWhereString(finder, out ret, tip);
            db.Close();
            return res;
        }
    }
}
