using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using STCLINE.KP50.Interfaces;
using STCLINE.KP50.Global;
using System.ServiceModel;

namespace STCLINE.KP50.Client
{
    public class cli_Archive : I_Archive
    {
        I_Archive remoteObject;

        #region Конструкторы
        public cli_Archive()
            : base()
        {
            _cli_Archive(0);
        }

        public cli_Archive(int nzp_server)
            : base()
        {
            _cli_Archive(nzp_server);
        }

        public cli_Archive(string conn, string login, string pass)
        {
            Constants.Login = login;
            Constants.Password = pass;
            remoteObject = HostChannel.CreateInstance<I_Archive>(login, pass, conn + WCFParams.AdresWcfHost.srvArchive, "Archive");
        }

        void _cli_Archive(int nzp_server)
        {
            string addrHost = "";
            //определить параметры доступа
            _RServer zap = MultiHost.GetServer(nzp_server);

            if (Points.IsMultiHost && nzp_server > 0)
            {
                addrHost = zap.ip_adr + WCFParams.AdresWcfHost.srvArchive;
                remoteObject = HostChannel.CreateInstance<I_Archive>(zap.login, zap.pwd, addrHost);
            }
            else
            {
                //по-умолчанию
                addrHost = WCFParams.AdresWcfHost.Adres + WCFParams.AdresWcfHost.srvArchive;
                zap.rcentr = "<локально>";
                remoteObject = HostChannel.CreateInstance<I_Archive>(Constants.Login, Constants.Password, addrHost, "Archive");
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
        #endregion

        public Returns MakeArchive(Finder finder)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                ret = remoteObject.MakeArchive(finder);
                HostChannel.CloseProxy(remoteObject);
            }
            catch(Exception ex)
            {
                ret.result = false;
                ret.text = "Ошибка архивации";
                MonitorLog.WriteLog("Ошибка архивации: " + ex.Message, MonitorLog.typelog.Error, true);
            }
            return ret;
        }
    }
}
