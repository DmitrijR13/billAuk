namespace STCLINE.KP50.Client
{
    using System;
    using System.ServiceModel;
    using System.Collections.Generic;
    using Interfaces;
    using Global;


    public partial class cli_Exchange : cli_Base, I_Exchange
    {
        //I_Exchange remoteObject;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="nzp_server">Код удаленного сервера</param>
        /// <param name="nzp_role">Код подсистемы</param>
        public cli_Exchange(int nzp_server)
            : base()
        {
            //_cli_Exchange(nzp_server, Constants.roleDebt);
        }

        IExchangeRemoteObject getRemoteObject()
        {
            return getRemoteObject<IExchangeRemoteObject>(WCFParams.AdresWcfWeb.srvExchange);
        }


        //void _cli_Exchange(int nzp_server, int nzp_role)
        //{
        //    string addrHost = "";
        //    //определить параметры доступа
        //    _RServer zap = MultiHost.GetServer(nzp_server);

        //    if (Points.IsMultiHost && nzp_server > 0)
        //    {
        //        addrHost = zap.ip_adr + WCFParams.AdresWcfWeb.srvExchange;
        //        remoteObject = HostChannel.CreateInstance<I_Exchange>(zap.login, zap.pwd, addrHost);
        //    }
        //    else
        //    {
        //        //по-умолчанию
        //        if (nzp_role == Constants.roleDebt)
        //            addrHost = WCFParams.AdresWcfWeb.Adres + WCFParams.AdresWcfWeb.srvExchange;
        //        else
        //            addrHost = WCFParams.AdresWcfWeb.Adres + WCFParams.AdresWcfWeb.srvLicense;
        //        zap.rcentr = "<локально>";
        //        remoteObject = HostChannel.CreateInstance<I_Exchange>(Constants.Login, Constants.Password, addrHost, "Exchange");
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

        //~cli_Exchange()
        //{
        //    try { if (remoteObject != null) HostChannel.CloseProxy(remoteObject); }
        //    catch { }
        //}


        #region ВТБ24
        /// <summary>
        /// Загрузка оплат от ВТБ24
        /// </summary>
        /// <param name="finder"></param>
        /// <returns></returns>
        public Returns CheckVtb24(FilesImported finder)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.CheckVtb24(finder);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка UploadVTB24\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка UploadVTB24\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        public List<VTB24Info> GetReestrsVTB24(ExFinder finder, out Returns ret)
        {

            ret = Utils.InitReturns();
            List<VTB24Info> res = null;
            try
            {
                using (var ro = getRemoteObject())
                {
                    res = ro.GetReestrsVTB24(finder, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetReestrsVTB24\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка GetReestrsVTB24\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return res;
        }

        public Returns DeleteReestrVTB24(ExFinder finder)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.DeleteReestrVTB24(finder);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка DeleteReestrVTB24\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка DeleteReestrVTB24\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        public bool CanDistr(ExFinder finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            bool res = false;
            try
            {
                using (var ro = getRemoteObject())
                {
                    res = ro.CanDistr(finder, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка CanDistr\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка CanDistr\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return res;
        }
        #endregion 

        #region Обмен с поставщиками
        public List<IReestrExSuppSyncLs> GetReestrSyncLs(ExFinder finder, out Returns ret)
        {

            ret = Utils.InitReturns();
            List<IReestrExSuppSyncLs> res = null;
            try
            {
                using (var ro = getRemoteObject())
                {
                    res = ro.GetReestrSyncLs(finder, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetReestrSyncLs\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка GetReestrSyncLs\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return res;
        }

        public Returns DeleteReestrRow(ExFinder finder)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.DeleteReestrRow(finder);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка DeleteReestrRow\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка DeleteReestrRow\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        public List<IReestrExSuppChangeLs> GetReestrChangeLs(ExFinder finder, out Returns ret)
        {

            ret = Utils.InitReturns();
            List<IReestrExSuppChangeLs> res = null;
            try
            {
                using (var ro = getRemoteObject())
                {
                    res = ro.GetReestrChangeLs(finder, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetReestrChangeLs\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка GetReestrChangeLs\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return res;
        }
        #endregion 


        #region Обмен с соцзащитой

        public Returns DeleteFromExchangeSZ(Finder finder, int nzp_ex_sz)
        {
            var ret = new Returns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.DeleteFromExchangeSZ(finder, nzp_ex_sz);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret = new Returns(false, Constants.access_error, Constants.access_code);
                MonitorLog.WriteLog("Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret = new Returns(false, ex.Message);
                MonitorLog.WriteLog("Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }

            return ret;
        }
        #endregion 

        public List<SimpleLoadClass> GetSimpleLoadData(FilesImported finder, out Returns ret)
        {

            ret = Utils.InitReturns();
            List<SimpleLoadClass> res = null;
            try
            {
                using (var ro = getRemoteObject())
                {
                    res = ro.GetSimpleLoadData(finder, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetSimpleLoadData\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка GetSimpleLoadData\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return res;
        }

        public Returns Delete(SimpleLoadClass finder)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.Delete(finder);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка Delete\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка Delete\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        public Returns CheckSimpleLoadFileExixsts(FilesImported finder)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.CheckSimpleLoadFileExixsts(finder);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка CheckSimpleLoadFileExixsts\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка CheckSimpleLoadFileExixsts\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }
    }
}
