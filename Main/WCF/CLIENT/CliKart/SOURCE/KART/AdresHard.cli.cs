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
    public class cli_AdresHard : cli_Base
    {
        //private I_AdresHard remoteObject;

        public cli_AdresHard(int nzp_server)
            : base(nzp_server)
        {
            //_cli_AdresHard(nzp_server);
        }

        //private void _cli_AdresHard(int nzp_server)
        //{
        //    _RServer zap = MultiHost.GetServer(nzp_server);
        //    string addrHost = "";

        //    if (Points.IsMultiHost && nzp_server > 0)
        //    {
        //        //определить параметры доступа
        //        addrHost = zap.ip_adr + WCFParams.AdresWcfWeb.srvAdresHard;
        //        remoteObject = HostChannel.CreateInstance<I_AdresHard>(zap.login, zap.pwd, addrHost);
        //    }
        //    else
        //    {
        //        //по-умолчанию
        //        addrHost = WCFParams.AdresWcfWeb.Adres + WCFParams.AdresWcfWeb.srvAdresHard;
        //        zap.rcentr = "<локально>";
        //        remoteObject = HostChannel.CreateInstance<I_AdresHard>(addrHost);
        //    }

        //    //Попытка открыть канал связи

        //    DateTime beforeOpen = DateTime.Now;
        //    try
        //    {
        //        ICommunicationObject proxy = remoteObject as ICommunicationObject;
        //        //MonitorLog.WriteLog("До открытия соединения объекта " + this + " " + DateTime.Now, MonitorLog.typelog.Warn, true);
        //        proxy.Open();
        //        //MonitorLog.WriteLog("Поток " + System.Threading.Thread.CurrentThread.ManagedThreadId + ". Успешное открытие соединения объекта " + this + " с " + beforeOpen.TimeOfDay + " по " + DateTime.Now.TimeOfDay, MonitorLog.typelog.Warn, true);
        //    }
        //    catch (Exception ex)
        //    {
        //        //MonitorLog.WriteLog("Поток " + System.Threading.Thread.CurrentThread.ManagedThreadId + ". Неуспешное открытие соединения объекта " + this + " с " + beforeOpen.TimeOfDay + " по " + DateTime.Now.TimeOfDay, MonitorLog.typelog.Warn, true);

        //        MonitorLog.WriteLog(
        //            string.Format(
        //                "Ошибка открытия Proxy {0} по адресу {1}. Сервер {2} (Код РЦ {3}) (Код сервера {4}): {5}",
        //                System.Reflection.MethodBase.GetCurrentMethod().Name,
        //                addrHost,
        //                zap.rcentr,
        //                zap.nzp_rc,
        //                nzp_server,
        //                ex.Message),
        //            MonitorLog.typelog.Error, 2, 100, true);
        //    }
        //}


        public cli_AdresHard(int nzp_server, int nzp_role)
            : base(nzp_server)
        {
            //_cli_Adres(nzp_server, nzp_role);
        }

        //void _cli_Adres(int nzp_server, int nzp_role)
        //{
        //    if (Points.IsMultiHost && nzp_server > 0)
        //    {
        //        //определить параметры доступа
        //        _RServer zap = MultiHost.GetServer(nzp_server);
        //        remoteObject = HostChannel.CreateInstance<I_AdresHard>(zap.login, zap.pwd, zap.ip_adr + WCFParams.AdresWcfWeb.srvAdresHard);
        //    }
        //    else
        //    {
        //        if (nzp_role == Constants.roleSupg)
        //            remoteObject = HostChannel.CreateInstance<I_AdresHard>(WCFParams.AdresWcfWeb.SupgAdres + WCFParams.AdresWcfWeb.srvAdresHard);
        //        else
        //            remoteObject = HostChannel.CreateInstance<I_AdresHard>(WCFParams.AdresWcfWeb.Adres + WCFParams.AdresWcfWeb.srvAdresHard);
        //    }
        //}


        //~cli_AdresHard()
        //{
        //    try
        //    {
        //        if (remoteObject != null) HostChannel.CloseProxy(remoteObject);
        //    }
        //    catch
        //    {
        //    }
        //}

        List<_Area> arSpis = new List<_Area>();
        List<_Geu> geuSpis = new List<_Geu>();
        List<Ls> lsSpis = new List<Ls>();


        //----------------------------------------------------------------------
        public List<Ls> LoadLs(Ls finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            List<Ls> LsInf = null;
            ret = Utils.InitReturns();
            try
            {
                using (var remoteObject = getRemoteObject())
                {
                    LsInf = remoteObject.LoadLs(finder, out ret);
                }
                //HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                ret.result = false;
                if (ex is System.ServiceModel.EndpointNotFoundException)
                {
                    ret.text = Constants.access_error;
                    ret.tag = Constants.access_code;
                }
                else ret.text = "Ошибка вызова функции получения информации о лицевом счете";

                MonitorLog.WriteLog("Ошибка LoadLs\n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return LsInf;
        }

        public int UpdateDom(Dom finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            int b = 0;
            try
            {
                using (var remoteObject = getRemoteObject())
                {
                    b = remoteObject.UpdateDom(finder, out ret);
                }
                //HostChannel.CloseProxy(remoteObject);
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
                    ret.text = "Ошибка вызова сервиса изменения домов";

                MonitorLog.WriteLog("Ошибка UpdateDom \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return b;
        }

        public List<SplitLsParams> ExecuteSplitLS(List<SplitLsParams> listPrm, List<Perekidka> listPerekidka,  List<Kart> listGilec, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<SplitLsParams> b = null;
            try
            {
                using (var remoteObject = getRemoteObject())
                {
                    b = remoteObject.ExecuteSplitLS(listPrm, listPerekidka, listGilec, out ret);
                }
                //HostChannel.CloseProxy(remoteObject);
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
                    ret.text = "Ошибка вызова сервиса разделения ЛС";

                MonitorLog.WriteLog("Ошибка ExecuteSplitLS \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return b;
        }

        //----------------------------------------------------------------------
        public int UpdateLs(Ls finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            int res = 0;
            try
            {
                using (var remoteObject = getRemoteObject())
                {
                    res = remoteObject.UpdateLs(finder, out ret);
                }
                //HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                ret = new Returns(false, "Ошибка вызова сервиса изменения лицевых счетов");
                if (ex is System.ServiceModel.EndpointNotFoundException)
                {
                    ret.text = Constants.access_error;
                    ret.tag = Constants.access_code;
                }

                MonitorLog.WriteLog("Ошибка UpdateLs \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return res;
        }
        
        public Returns GenerateLsPu(Ls finder, List<Counter> CounterList)
        {
            Returns ret;
            try
            {
                using (var remoteObject = getRemoteObject())
                {
                    ret = remoteObject.GenerateLsPu(finder, CounterList);
                }
                //HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                ret = new Returns(false, "Ошибка вызова сервиса группового добавления лицевых счетов");
                if (ex is System.ServiceModel.EndpointNotFoundException)
                {
                    ret.text = Constants.access_error;
                    ret.tag = Constants.access_code;
                }

                MonitorLog.WriteLog("Ошибка GenerateLsPu \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }
        //----------------------------------------------------------------------


        //----------------------------------------------------------------------
        public Returns GenerateLsPu(List<Counter> CounterList)
        //----------------------------------------------------------------------
        {
            return GenerateLsPu(null, CounterList);
        }
        //----------------------------------------------------------------------
        public Returns GenerateLsPu(Ls finder)
        //----------------------------------------------------------------------
        {
            return GenerateLsPu(finder, null);
        }


        public List<Ls> GetLs(Ls finder, enSrvOper srv, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            try
            {
                using (var remoteObject = getRemoteObject())
                {
                    lsSpis = remoteObject.GetLs(finder, srv, out ret);
                }
                //HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                ret.result = false;
                if (ex is System.ServiceModel.EndpointNotFoundException)
                {
                    ret.text = Constants.access_error;
                    ret.tag = Constants.access_code;
                }
                else ret.text = "Ошибка вызова сервиса лицевого счета";

                MonitorLog.WriteLog("Ошибка GetLs (" + srv + ") \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                lsSpis = null;
            }

            return lsSpis;
        }

        public List<Ls> GetLs2(Ls finder, Service servfinder, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            try
            {
                using (var remoteObject = getRemoteObject())
                {
                    lsSpis = remoteObject.GetLs2(finder, servfinder, out ret);
                }
                //HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                ret.result = false;
                if (ex is System.ServiceModel.EndpointNotFoundException)
                {
                    ret.text = Constants.access_error;
                    ret.tag = Constants.access_code;
                }
                else ret.text = "Ошибка вызова сервиса лицевого счета";

                MonitorLog.WriteLog("Ошибка GetLs2 () \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                lsSpis = null;
            }

            return lsSpis;
        }

        //----------------------------------------------------------------------
        public string GetFakturaName(Ls finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            string s = "";
            try
            {
                using (var remoteObject = getRemoteObject())
                {
                    s = remoteObject.GetFakturaName(finder, out ret);
                }
                //HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                ret.result = false;
                if (ex is System.ServiceModel.EndpointNotFoundException)
                {
                    ret.text = Constants.access_error;
                    ret.tag = Constants.access_code;
                }
                else ret.text = "Ошибка вызова сервиса счета-фактуры";

                MonitorLog.WriteLog("Ошибка GetFakturaName \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return s;
        }

        protected IAdresRemoteObject getRemoteObject()
        {
            return getRemoteObject<IAdresRemoteObject>(WCFParams.AdresWcfWeb.srvAdresHard);
        }

        public List<_Area> GetArea(Finder finder, out Returns ret, out DateTime bt, out DateTime et)
        {
            bt = et = DateTime.MinValue;
                
            ret = Utils.InitReturns();
            Areas spis = new Areas();

            try
            {
                BeforeStartQuery("GetArea");

                using (var ro = getRemoteObject())
                {
                    spis.AreaList = ro.GetArea(finder, out ret, out bt, out et);
                }
                
                //spis.AreaList = remoteObject.GetArea();
                //HostChannel.CloseProxy(remoteObject);

                AfterStopQuery(bt, et);
            }
            catch (Exception ex)
            {
                ret.result = false;
                if (ex is System.ServiceModel.EndpointNotFoundException)
                {
                    ret.text = Constants.access_error;
                    ret.tag = Constants.access_code;
                }
                else ret.text = "Ошибка сервиса списка УК";

                MonitorLog.WriteLog("Ошибка GetArea \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                spis.AreaList = null;
            }

            return spis.AreaList;
        }

        public List<_Area> GetArea(Finder finder, out Returns ret)
        {
            DateTime bt, et;
            return GetArea(finder, out ret, out bt, out et);
        }

        //----------------------------------------------------------------------
        public List<_Area> LoadAreaForKvar(Finder finder, out Returns ret)
            //----------------------------------------------------------------------
        {
            List<_Area> result;
            ret = Utils.InitReturns();
            try
            {
                using (var remoteObject = getRemoteObject())
                {
                    result = remoteObject.LoadAreaForKvar(finder, out ret);
                }
            }
            catch (Exception ex)
            {
                result = new List<_Area>();
                ret.result = false;
                if (ex is EndpointNotFoundException)
                {
                    ret.text = Constants.access_error;
                    ret.tag = Constants.access_code;
                }
                else ret.text = "Ошибка сервиса карт";

                MonitorLog.WriteLog("Ошибка LoadAreaForKvar \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return result;
        }

        //public List<_Area> LoadAreaPayer(Finder finder, out Returns ret)
        //{
        //    List<_Area> result;
        //    ret = Utils.InitReturns();
        //    try
        //    {
        //        using (var remoteObject = getRemoteObject())
        //        {
        //            result = new List<_Area>(); //(remoteObject.LoadAreaPayer(finder, out ret));

        //            if (finder.rows > 0) result = remoteObject.LoadAreaPayer(finder, out ret);
        //            else
        //            {
        //                finder.rows = 500;
        //                for (ret = new Returns(true, null, finder.skip + 1); finder.skip < ret.tag && ret.result; finder.skip += finder.rows)
        //                {
        //                    IList<_Area> tmplist = remoteObject.LoadAreaPayer(finder, out ret);
        //                    if (tmplist != null) result.AddRange(tmplist);
        //                }

        //            }
        //        }

        //        //result = remoteObject.GetMapObjects(finder, out ret);
        //        //HostChannel.CloseProxy(remoteObject);
        //    }
        //    catch (Exception ex)
        //    {
        //        result = new List<_Area>();
        //        ret.result = false;
        //        if (ex is System.ServiceModel.EndpointNotFoundException)
        //        {
        //            ret.text = Constants.access_error;
        //            ret.tag = Constants.access_code;
        //        }
        //        else ret.text = "Ошибка";

        //        MonitorLog.WriteLog("Ошибка LoadAreaPayer \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
        //    }
        //    return result;
        //}

        //----------------------------------------------------------------------


        public List<_Area> LoadAreaPayer(Finder finder, out Returns ret)
        {
            List<_Area> result = new List<_Area>();
            ret = Utils.InitReturns();
            try
            {
                using (var remoteObject = getRemoteObject())
                {
                    result = remoteObject.LoadAreaPayer(finder, out ret);
                }
            }
            catch (Exception ex)
            {
                result = new List<_Area>();
                ret.result = false;
                if (ex is System.ServiceModel.EndpointNotFoundException)
                {
                    ret.text = Constants.access_error;
                    ret.tag = Constants.access_code;
                }
                else ret.text = "Ошибка";

                MonitorLog.WriteLog("Ошибка LoadAreaPayer \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return result;
        }


        public List<MapObject> GetMapObjects(MapObject finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            List<MapObject> result;
            ret = Utils.InitReturns();
            try
            {
                using (var remoteObject = getRemoteObject())
                {
                    result = remoteObject.GetMapObjects(finder, out ret);
                }

                //result = remoteObject.GetMapObjects(finder, out ret);
                //HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                result = new List<MapObject>();
                ret.result = false;
                if (ex is System.ServiceModel.EndpointNotFoundException)
                {
                    ret.text = Constants.access_error;
                    ret.tag = Constants.access_code;
                }
                else ret.text = "Ошибка сервиса карт";

                MonitorLog.WriteLog("Ошибка GetMapObjects \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return result;
        }

      


        //----------------------------------------------------------------------
        public Returns UpdateLsInCache(Ls finder)
        //----------------------------------------------------------------------
        {
            Returns ret = Utils.InitReturns();
            try
            {
                using (var remoteObject = getRemoteObject())
                {
                    ret = remoteObject.UpdateLsInCache(finder);
                }
                //HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                ret.result = false;
                if (ex is System.ServiceModel.EndpointNotFoundException)
                {
                    ret.text = Constants.access_error;
                    ret.tag = Constants.access_code;
                }
                else ret.text = "Ошибка вызова сервиса";

                MonitorLog.WriteLog("Ошибка UpdateLsInCache() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

     
        public Returns SaveGeu(Geu finder)
        {
            Returns ret;
            try
            {
                using (var remoteObject = getRemoteObject())
                {
                    ret = remoteObject.SaveGeu(finder);
                }
                //HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                ret = new Returns(false);
                if (ex is System.ServiceModel.EndpointNotFoundException)
                {
                    ret.text = Constants.access_error;
                    ret.tag = Constants.access_code;
                }
                else ret.text = "Ошибка сохранения отделения";

                MonitorLog.WriteLog("Ошибка SaveGeu\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }


        public Returns SaveUlica(Ulica finder)
        {
            Returns ret;
            try
            {
                using (var remoteObject = getRemoteObject())
                {
                    ret = remoteObject.SaveUlica(finder);
                }
                //HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                ret = new Returns(false);
                if (ex is System.ServiceModel.EndpointNotFoundException)
                {
                    ret.text = Constants.access_error;
                    ret.tag = Constants.access_code;
                }
                else ret.text = "Ошибка сохранения улиц";

                MonitorLog.WriteLog("Ошибка SaveUlica\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        public Returns GeneratePkodFon(Finder finder)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                using (var remoteObject = getRemoteObject())
                {
                    ret = remoteObject.GeneratePkodFon(finder);
                }
                //HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = "\n " + ex.Message;

                MonitorLog.WriteLog("Ошибка GeneratePkodFon" + err, MonitorLog.typelog.Error, 2, 100, true);
            }

            return ret;
        }
    }
}
