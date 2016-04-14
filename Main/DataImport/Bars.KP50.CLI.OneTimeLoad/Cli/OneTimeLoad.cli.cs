using System;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;
using System.ServiceModel;

namespace STCLINE.KP50.Client
{
    public class cli_OneTimeLoad : cli_Base, I_OneTimeLoad
    {
        //I_OneTimeLoad remoteObject;

        public cli_OneTimeLoad(int nzp_server)
            : base()
        {
            //_cli_OneTimeLoad(nzp_server);
        }

        //void _cli_OneTimeLoad(int nzp_server)
        //{
        //    _RServer zap = MultiHost.GetServer(nzp_server);
        //    string addrHost = "";

        //    if (Points.IsMultiHost && nzp_server > 0)
        //    {
        //        //определить параметры доступа
        //        addrHost = zap.ip_adr + WCFParams.AdresWcfWeb.srvOneTimeLoad;
        //        remoteObject = HostChannel.CreateInstance<I_OneTimeLoad>(zap.login, zap.pwd, addrHost);
        //    }
        //    else
        //    {
        //        //по-умолчанию
        //        addrHost = WCFParams.AdresWcfWeb.Adres + WCFParams.AdresWcfWeb.srvOneTimeLoad;
        //        zap.rcentr = "<локально>";
        //        remoteObject = HostChannel.CreateInstance<I_OneTimeLoad>(addrHost);
        //    }

        //    //Попытка открыть канал связи
        //    try
        //    {
        //        ICommunicationObject proxy = remoteObject as ICommunicationObject;
        //        proxy.Open();
        //    }
        //    catch (Exception ex)
        //    {
        //        MonitorLog.WriteLog(string.Format("Ошибка открытия Proxy {0} по адресу {1}. Сервер {2} (Код РЦ {3}) (Код сервера {4}): {5}",
        //                            System.Reflection.MethodBase.GetCurrentMethod().Name,
        //                            addrHost,
        //                            zap.rcentr,
        //                            zap.nzp_rc,
        //                            nzp_server,
        //                            ex.Message),
        //                            MonitorLog.typelog.Error, 2, 100, true);
        //    }
        //}

        //~cli_OneTimeLoad()
        //{
        //    try { if (remoteObject != null) HostChannel.CloseProxy(remoteObject); }
        //    catch { }
        //}

        IOneTimeLoadRemoteObject getRemoteObject()
        {
            return getRemoteObject<IOneTimeLoadRemoteObject>(WCFParams.AdresWcfWeb.srvOneTimeLoad);
        }

        public Returns UploadUESCharge(FilesImported finder)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.UploadUESCharge(finder);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка UploadUESCharge\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка UploadUESCharge\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        public Returns UploadMURCPayment(FilesImported finder)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.UploadMURCPayment(finder);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка UploadMURCPayment\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка UploadMURCPayment\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        
        public Returns ReadReestrFromCbb(FilesImported finderpack, FilesImported finder, string connectionString)
        {
            var ret = new Returns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.ReadReestrFromCbb(finderpack, finder, connectionString);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка ReadReestrFromCbb\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка ReadReestrFromCbb\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }
    }
}