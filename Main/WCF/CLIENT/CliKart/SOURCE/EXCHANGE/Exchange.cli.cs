namespace STCLINE.KP50.Client
{
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


    public class cli_Exchange : I_Exchange
    {
        I_Exchange remoteObject;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="nzp_server">Код удаленного сервера</param>
        /// <param name="nzp_role">Код подсистемы</param>
        public cli_Exchange(int nzp_server)
            : base()
        {
            _cli_Exchange(nzp_server, Constants.roleDebt);
        }

        void _cli_Exchange(int nzp_server, int nzp_role)
        {
            string addrHost = "";
            //определить параметры доступа
            _RServer zap = MultiHost.GetServer(nzp_server);

            if (Points.IsMultiHost && nzp_server > 0)
            {
                addrHost = zap.ip_adr + WCFParams.AdresWcfWeb.srvExchange;
                remoteObject = HostChannel.CreateInstance<I_Exchange>(zap.login, zap.pwd, addrHost);
            }
            else
            {
                //по-умолчанию
                if (nzp_role == Constants.roleDebt)
                    addrHost = WCFParams.AdresWcfWeb.Adres + WCFParams.AdresWcfWeb.srvExchange;
                else
                    addrHost = WCFParams.AdresWcfWeb.Adres + WCFParams.AdresWcfWeb.srvLicense;
                zap.rcentr = "<локально>";
                remoteObject = HostChannel.CreateInstance<I_Exchange>(Constants.Login, Constants.Password, addrHost, "Exchange");
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

        ~cli_Exchange()
        {
            try { if (remoteObject != null) HostChannel.CloseProxy(remoteObject); }
            catch { }
        }

        /// <summary>
        /// Загрузка оплат от ВТБ24
        /// </summary>
        /// <param name="finder"></param>
        /// <returns></returns>
        public Returns UploadVTB24(FilesImported finder)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                ret = remoteObject.UploadVTB24(finder);
                HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = "\n " + ex.Message;

                MonitorLog.WriteLog("Ошибка UploadVTB24" + err, MonitorLog.typelog.Error, 2, 100, true);
            }

            return ret;
        }

        public List<VTB24Info> GetReestrsVTB24(ExFinder finder, out Returns ret)
        {

            ret = Utils.InitReturns();
            List<VTB24Info> res = new List<VTB24Info>();
            try
            {
                res = remoteObject.GetReestrsVTB24(finder, out ret);
                HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = "\n " + ex.Message;
                MonitorLog.WriteLog("Ошибка GetReestrsVTB24" + err, MonitorLog.typelog.Error, 2, 100, true);
                return null;
            }

            return res;
        }


        public Returns DeleteReestrVTB24(ExFinder finder)
        {

            Returns ret = Utils.InitReturns();           
            try
            {
                ret = remoteObject.DeleteReestrVTB24(finder);
                HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = "\n " + ex.Message;
                MonitorLog.WriteLog("Ошибка DeleteReestrVTB24" + err, MonitorLog.typelog.Error, 2, 100, true);
                return ret;
            }

            return ret;
        }

        public bool CanDistr(ExFinder finder, out Returns ret)
        {

            ret = Utils.InitReturns();
            bool res;
            try
            {
                res = remoteObject.CanDistr(finder, out ret);
                HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = "\n " + ex.Message;
                MonitorLog.WriteLog("Ошибка CanDistr" + err, MonitorLog.typelog.Error, 2, 100, true);
                return false;
            }

            return res;
        }

    }
}
