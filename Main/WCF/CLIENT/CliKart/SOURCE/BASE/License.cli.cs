using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using STCLINE.KP50.Interfaces;
using STCLINE.KP50.Global;
using System.ServiceModel;
using System.ComponentModel;

namespace STCLINE.KP50.Client
{
    /// <summary>
    /// Вызовы серверной части работы с лицензией
    /// </summary>
    public class cli_License : I_License
    {
        I_License remoteObject;

        public cli_License()
            : base()
        {
            _cli_License(0);
        }

        public cli_License(int nzp_server)
            : base()
        {
            _cli_License(nzp_server);
        }

        public cli_License(string conn, string login, string pass)
        {
            Constants.Login = login;
            Constants.Password = pass;
            remoteObject = HostChannel.CreateInstance<I_License>(login, pass, conn + WCFParams.AdresWcfWeb.srvLicense, "License");
        }

        void _cli_License(int nzp_server)
        {
            string addrHost = "";
            //определить параметры доступа
            _RServer zap = MultiHost.GetServer(nzp_server);

            if (Points.IsMultiHost && nzp_server > 0)
            {
                addrHost = zap.ip_adr + WCFParams.AdresWcfWeb.srvLicense;
                remoteObject = HostChannel.CreateInstance<I_License>(zap.login, zap.pwd, addrHost);
            }
            else
            {
                //по-умолчанию
                addrHost = WCFParams.AdresWcfWeb.Adres + WCFParams.AdresWcfWeb.srvLicense;
                zap.rcentr = "<локально>";
                remoteObject = HostChannel.CreateInstance<I_License>(Constants.Login, Constants.Password, addrHost, "License");
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

        public Returns CheckLicense(int nzp_user, string key)
        {
            try
            {
                Returns ret = remoteObject.CheckLicense(nzp_user, key);
                HostChannel.CloseProxy(remoteObject);
                return ret;
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка проверки лицензии : " + ex.Message, MonitorLog.typelog.Error, true);
                Returns ret = Utils.InitReturns();
                ret.result = false;
                return ret;
            }
        }

        public string GetRequestKey(int nzp_user)
        {
            try
            {
                string ret = remoteObject.GetRequestKey(nzp_user);
                HostChannel.CloseProxy(remoteObject);
                return ret;
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка получения ключа для проверки лицензии : " + ex.Message, MonitorLog.typelog.Error, true);
                return null;
            }
        }
    }
}
