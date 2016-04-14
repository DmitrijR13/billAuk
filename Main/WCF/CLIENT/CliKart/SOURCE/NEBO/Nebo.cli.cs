using System;
using System.Collections.Generic;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;
using STCLINE.KP50.DataBase;
using System.ServiceModel;
using System.Data;

namespace STCLINE.KP50.Client
{
    public class cli_Nebo : I_Nebo
    {
        I_Nebo remoteObject;

        public cli_Nebo(int nzp_server)
            : base()
        {
            _cli_Nebo(nzp_server);
        }

        void _cli_Nebo(int nzp_server)
        {
            _RServer zap = MultiHost.GetServer(nzp_server);
            string addrHost = "";

            if (Points.IsMultiHost && nzp_server > 0)
            {
                //определить параметры доступа
                addrHost = zap.ip_adr + WCFParams.AdresWcfWeb.srvNebo;
                remoteObject = HostChannel.CreateInstance<I_Nebo>(zap.login, zap.pwd, addrHost);
            }
            else
            {
                //по-умолчанию
                addrHost = WCFParams.AdresWcfWeb.Adres + WCFParams.AdresWcfWeb.srvNebo;
                zap.rcentr = "<локально>";
                remoteObject = HostChannel.CreateInstance<I_Nebo>(addrHost);
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

        ~cli_Nebo()
        {
            try { if (remoteObject != null) HostChannel.CloseProxy(remoteObject); }
            catch { }
        }

        public Returns CreateReestrNebo(int nzp_type, string dat_s, string dat_po)
        {
            var ret = new Returns();
            try
            {
                ret = remoteObject.CreateReestrNebo(nzp_type,dat_s,dat_po);
                HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = "\n " + ex.Message;

                MonitorLog.WriteLog("Ошибка CreateReestrNebo" + err, MonitorLog.typelog.Error, 2, 100, true);
            }

            return ret;
        }

        public IntfResultObjectType<List<NeboService>> GetServiceList(int nzp_area, RequestPaging paging)
        {
            IntfResultObjectType<List<NeboService>> ret = null;
            try
            {
                ret = remoteObject.GetServiceList(nzp_area, paging);
                HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                ret = new IntfResultObjectType<List<NeboService>>(-1, ex.Message);

                string err = "";
                if (Constants.Viewerror) err = "\n " + ex.Message;

                MonitorLog.WriteLog("Ошибка GetServiceList" + err, MonitorLog.typelog.Error, 2, 100, true);
            }

            return ret;
        }

        public IntfResultObjectType<List<NeboDom>> GetDomList(int nzp_area, RequestPaging paging)
        {
            IntfResultObjectType<List<NeboDom>> ret = null;
            try
            {
                ret = remoteObject.GetDomList(nzp_area, paging);
                HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                ret = new IntfResultObjectType<List<NeboDom>>(-1, ex.Message);

                string err = "";
                if (Constants.Viewerror) err = "\n " + ex.Message;

                MonitorLog.WriteLog("Ошибка GetDomList" + err, MonitorLog.typelog.Error, 2, 100, true);
            }

            return ret;
        }

        public IntfResultObjectType<List<NeboSupplier>> GetSupplierList(int nzp_area, RequestPaging paging)
        {
            IntfResultObjectType<List<NeboSupplier>> ret = null;
            try
            {
                ret = remoteObject.GetSupplierList(nzp_area, paging);
                HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                ret = new IntfResultObjectType<List<NeboSupplier>>(-1, ex.Message);

                string err = "";
                if (Constants.Viewerror) err = "\n " + ex.Message;

                MonitorLog.WriteLog("Ошибка GetSupplierList" + err, MonitorLog.typelog.Error, 2, 100, true);
            }

            return ret;
        }

        public IntfResultObjectType<List<NeboRenters>> GetRentersList(int nzp_area, RequestPaging paging)
        {
            IntfResultObjectType<List<NeboRenters>> ret = null;
            try
            {
                ret = remoteObject.GetRentersList(nzp_area, paging);
                HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                ret = new IntfResultObjectType<List<NeboRenters>>(-1, ex.Message);

                string err = "";
                if (Constants.Viewerror) err = "\n " + ex.Message;

                MonitorLog.WriteLog("Ошибка GetRentersList" + err, MonitorLog.typelog.Error, 2, 100, true);
            }

            return ret;
        }

        public IntfResultObjectType<List<NeboArea>> GetAreaList(int nzp_area)
        {
            IntfResultObjectType<List<NeboArea>> ret = null;
            try
            {
                ret = remoteObject.GetAreaList(nzp_area);
                HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                ret = new IntfResultObjectType<List<NeboArea>>(-1, ex.Message);

                string err = "";
                if (Constants.Viewerror) err = "\n " + ex.Message;

                MonitorLog.WriteLog("Ошибка GetAreaList" + err, MonitorLog.typelog.Error, 2, 100, true);
            }

            return ret;
        }

        public IntfResultObjectType<List<NeboReestr>> GetReestrInfo(int nzp_nebo_reestr)
        {
            IntfResultObjectType<List<NeboReestr>> ret = null;
            try
            {
                ret = remoteObject.GetReestrInfo(nzp_nebo_reestr);
                HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                ret = new IntfResultObjectType<List<NeboReestr>>(-1, ex.Message);

                string err = "";
                if (Constants.Viewerror) err = "\n " + ex.Message;

                MonitorLog.WriteLog("Ошибка GetReestrInfo" + err, MonitorLog.typelog.Error, 2, 100, true);
            }

            return ret;
        }

        public IntfResultObjectType<List<NeboSaldo>> GetSaldoReestr(NeboSaldo neboSaldo, RequestPaging paging)
        {
            IntfResultObjectType<List<NeboSaldo>> ret = null;
            try
            {
                ret = remoteObject.GetSaldoReestr(neboSaldo, paging);
                HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                ret = new IntfResultObjectType<List<NeboSaldo>>(-1, ex.Message);

                string err = "";
                if (Constants.Viewerror) err = "\n " + ex.Message;

                MonitorLog.WriteLog("Ошибка GetSaldoReestr" + err, MonitorLog.typelog.Error, 2, 100, true);
            }

            return ret;
        }

        public IntfResultObjectType<List<NeboSupp>> GetSuppReestr(NeboSupp neboSupp)
        {
            IntfResultObjectType<List<NeboSupp>> ret = null;
            try
            {
                ret = remoteObject.GetSuppReestr(neboSupp);
                HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                ret = new IntfResultObjectType<List<NeboSupp>>(-1, ex.Message);

                string err = "";
                if (Constants.Viewerror) err = "\n " + ex.Message;

                MonitorLog.WriteLog("Ошибка GetSuppReestr" + err, MonitorLog.typelog.Error, 2, 100, true);
            }

            return ret;
        }

        public IntfResultObjectType<List<NeboPaymentReestr>> GetPaymentReestr(int nzp_nebo_reestr, int nzp_area, RequestPaging paging)
        {
            IntfResultObjectType<List<NeboPaymentReestr>> ret = null;
            try
            {
                ret = remoteObject.GetPaymentReestr(nzp_nebo_reestr, nzp_area, paging);
                HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                ret = new IntfResultObjectType<List<NeboPaymentReestr>>(-1, ex.Message);

                string err = "";
                if (Constants.Viewerror) err = "\n " + ex.Message;

                MonitorLog.WriteLog("Ошибка GetSaldoReestr" + err, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }
    }
}