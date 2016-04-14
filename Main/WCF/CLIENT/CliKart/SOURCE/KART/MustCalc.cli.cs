using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Interfaces;
using STCLINE.KP50.Global;
using System.Runtime.Serialization;
using System.ServiceModel;

namespace STCLINE.KP50.Client
{
    public class cli_MustCalc : I_MustCalc
    {
        I_MustCalc remoteObject;

        public cli_MustCalc(int nzp_server)
            : base()
        {
            _cli_MustCalc(nzp_server);
        }

        void _cli_MustCalc(int nzp_server)
        {
            string addrHost = "";
            //определить параметры доступа
            _RServer zap = MultiHost.GetServer(nzp_server);

            if (Points.IsMultiHost && nzp_server > 0)
            {
                addrHost = zap.ip_adr + WCFParams.AdresWcfWeb.srvMustCalc;
                remoteObject = HostChannel.CreateInstance<I_MustCalc>(zap.login, zap.pwd, addrHost);
            }
            else
            {
                //по-умолчанию
                addrHost = WCFParams.AdresWcfWeb.Adres + WCFParams.AdresWcfWeb.srvMustCalc;
                zap.rcentr = "<локально>";
                remoteObject = HostChannel.CreateInstance<I_MustCalc>(addrHost);
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

        ~cli_MustCalc()
        {
            try { if (remoteObject != null) HostChannel.CloseProxy(remoteObject); }
            catch { }
        }

        public Returns OperationsWithMustCalc(MustCalc finder, MustCalcOperations operation)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                ret = remoteObject.OperationsWithMustCalc(finder, operation);
                HostChannel.CloseProxy(remoteObject);
                return ret;
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = "Ошибка вызова функции";
                if (Constants.Viewerror) ret.text += ": " + ex.Message;
                if (Constants.Debug) MonitorLog.WriteLog("Ошибка OperationsWithMustCalc(" + operation.ToString() + ")\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                return ret;
            }
        }

        public Returns SaveSpLsMustCalc(MustCalc finder, List<Service> services)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                ret = remoteObject.SaveSpLsMustCalc(finder, services);
                HostChannel.CloseProxy(remoteObject);
                return ret;
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = "Ошибка вызова функции";
                if (Constants.Viewerror) ret.text += ": " + ex.Message;
                if (Constants.Debug) MonitorLog.WriteLog("Ошибка SaveSpLsMustCalc()\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                return ret;
            }
        }

         public List<MustCalc> LoadMustCalc(MustCalc finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<MustCalc> list = null;
            try
            {
                list = remoteObject.LoadMustCalc(finder, out ret);
                HostChannel.CloseProxy(remoteObject);
                return list;
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = "Ошибка вызова функции";
                if (Constants.Viewerror) ret.text += ": " + ex.Message;
                if (Constants.Debug) MonitorLog.WriteLog("Ошибка LoadMustCalc()\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                return null;
            }
        }

         public List<ProhibitedMustCalc> GetProhibitedMustCalc(ProhibitedMustCalc finder, out Returns ret)
         {
             ret = Utils.InitReturns();
             List<ProhibitedMustCalc> list = null;
             try
             {
                 list = remoteObject.GetProhibitedMustCalc(finder, out ret);
                 HostChannel.CloseProxy(remoteObject);
                 return list;
             }
             catch (Exception ex)
             {
                 ret.result = false;
                 ret.text = "Ошибка вызова функции";
                 if (Constants.Viewerror) ret.text += ": " + ex.Message;
                 if (Constants.Debug) MonitorLog.WriteLog("Ошибка LoadMustCalc()\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                 return null;
             }
         }

         public void SaveProhibitedMustCalc(ProhibitedMustCalc finder, out Returns ret)
         {
             ret = Utils.InitReturns();
             try
             {
                 remoteObject.SaveProhibitedMustCalc(finder, out ret);
                 HostChannel.CloseProxy(remoteObject);
                 return;
             }
             catch (Exception ex)
             {
                 ret.result = false;
                 ret.text = "Ошибка вызова функции";
                 if (Constants.Viewerror) ret.text += ": " + ex.Message;
                 if (Constants.Debug) MonitorLog.WriteLog("Ошибка SaveProhibitedMustCalc()\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                 return;
             }
         }

         public void DeleteProhibitedMustCalc(ProhibitedMustCalc finder, out Returns ret)
         {
             ret = Utils.InitReturns();
             try
             {
                 remoteObject.DeleteProhibitedMustCalc(finder, out ret);
                 HostChannel.CloseProxy(remoteObject);
                 return;
             }
             catch (Exception ex)
             {
                 ret.result = false;
                 ret.text = "Ошибка вызова функции";
                 if (Constants.Viewerror) ret.text += ": " + ex.Message;
                 if (Constants.Debug) MonitorLog.WriteLog("Ошибка DeleteProhibitedMustCalc()\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                 return;
             }
         }


        public Returns SaveDisableMustCalcTXXspls(MustCalc finder, List<Service> services)
        {
            var ret = Utils.InitReturns();
            try
            {
                remoteObject.SaveDisableMustCalcTXXspls(finder, services);
                HostChannel.CloseProxy(remoteObject);
                return ret;
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = "Ошибка вызова функции";
                if (Constants.Viewerror) ret.text += ": " + ex.Message;
                if (Constants.Debug) MonitorLog.WriteLog("Ошибка SaveDisableMustCalcTXXspls()\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                return ret;
            }
        }

        public List<Service> LoadServiceForDisableMustCalcLs(Service finder, out Returns ret)
        {
            var list = new List<Service>();
            ret = Utils.InitReturns();
            try
            {
                list = remoteObject.LoadServiceForDisableMustCalcLs(finder, out ret);
                HostChannel.CloseProxy(remoteObject);
                return list;
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = "Ошибка вызова функции";
                if (Constants.Viewerror) ret.text += ": " + ex.Message;
                if (Constants.Debug) MonitorLog.WriteLog("Ошибка LoadServiceForDisableMustCalcLs()\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                return list;
            }
        }

        public List<Service> LoadSuppliersForDisableMustCalcLs(Service finder, out Returns ret)
        {
            var list = new List<Service>();
            ret = Utils.InitReturns();
            try
            {
                list = remoteObject.LoadSuppliersForDisableMustCalcLs(finder, out ret);
                HostChannel.CloseProxy(remoteObject);
                return list;
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = "Ошибка вызова функции";
                if (Constants.Viewerror) ret.text += ": " + ex.Message;
                if (Constants.Debug) MonitorLog.WriteLog("Ошибка LoadServiceForDisableMustCalcLs()\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                return list;
            }
        }
    }
}
