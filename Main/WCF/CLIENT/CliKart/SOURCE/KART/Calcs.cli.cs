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
    public class cli_Calcs : I_Calcs
    {
        I_Calcs remoteObject;

        public cli_Calcs(int nzp_server)
            : base()
        {
            _cli_Calcs(nzp_server);
        }

        void _cli_Calcs(int nzp_server)
        {
            string addrHost = "";
            //определить параметры доступа
            _RServer zap = MultiHost.GetServer(nzp_server);

            if (Points.IsMultiHost && nzp_server > 0)
            {
                addrHost = zap.ip_adr + WCFParams.AdresWcfWeb.srvCalcs;
                remoteObject = HostChannel.CreateInstance<I_Calcs>(zap.login, zap.pwd, addrHost);
            }
            else
            {
                //по-умолчанию
                addrHost = WCFParams.AdresWcfWeb.Adres + WCFParams.AdresWcfWeb.srvCalcs;
                zap.rcentr = "<локально>";
                remoteObject = HostChannel.CreateInstance<I_Calcs>(addrHost);
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

        ~cli_Calcs()
        {
            try { if (remoteObject != null) HostChannel.CloseProxy(remoteObject); }
            catch { }
        }

        List<Calcs> list = new List<Calcs>();

        //----------------------------------------------------------------------
        public List<Calcs> GetDomCalcsCollection(Calcs finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            try
            {
                list = remoteObject.GetDomCalcsCollection(finder, out ret);
                HostChannel.CloseProxy(remoteObject);
                return list;
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = "Ошибка при поиске данных";
                //ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;

                MonitorLog.WriteLog("Ошибка GetDomCalcsCollection " + err, MonitorLog.typelog.Error, 2, 100, true);

                return null;
            }
        }

        public List<Calcs> GetGrPuCalcsCollection(Calcs finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            try
            {
                list = remoteObject.GetGrPuCalcsCollection(finder, out ret);
                HostChannel.CloseProxy(remoteObject);
                return list;
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = "Ошибка при поиске данных";
                //ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;

                MonitorLog.WriteLog("Ошибка GetGrPuCalcsCollection " + err, MonitorLog.typelog.Error, 2, 100, true);

                return null;
            }
        }


        //----------------------------------------------------------------------
        public List<Calcs> GetKvarCalcsCollection(Calcs finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            try
            {
                list = remoteObject.GetKvarCalcsCollection(finder, out ret);
                HostChannel.CloseProxy(remoteObject);
                return list;
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = "Ошибка при поиске данных";
                //ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;

                MonitorLog.WriteLog("Ошибка GetKvarCalcsCollection " + err, MonitorLog.typelog.Error, 2, 100, true);

                return null;
            }
        }

        //----------------------------------------------------------------------
        public Returns CheckDatabaseExist(string pref, enDataBaseType en, string year_form, string year_to)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                ret = remoteObject.CheckDatabaseExist(pref, en, year_form, year_to);
                HostChannel.CloseProxy(remoteObject);
                return ret;
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
                    ret.text = "Ошибка";

                MonitorLog.WriteLog("Ошибка  CheckDatabaseExist\n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);

                return ret;
            }
        }
        //----------------------------------------------------------------------
    }
}
