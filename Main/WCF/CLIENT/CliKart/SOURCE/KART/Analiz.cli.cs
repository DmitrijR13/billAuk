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
    public class cli_Analiz : I_Analiz
    {
        I_Analiz remoteObject;

        public cli_Analiz(int nzp_server)
            : base()
        {
            _cli_Analiz(nzp_server);
        }

        void _cli_Analiz(int nzp_server)
        {
            string addrHost = "";
            //определить параметры доступа
            _RServer zap = MultiHost.GetServer(nzp_server);

            if (Points.IsMultiHost && nzp_server > 0)
            {
                addrHost = zap.ip_adr + WCFParams.AdresWcfWeb.srvAnaliz;
                remoteObject = HostChannel.CreateInstance<I_Analiz>(zap.login, zap.pwd, addrHost);
            }
            else
            {
                //по-умолчанию
                addrHost = WCFParams.AdresWcfWeb.Adres + WCFParams.AdresWcfWeb.srvAnaliz;
                zap.rcentr = "<локально>";
                remoteObject = HostChannel.CreateInstance<I_Analiz>(addrHost);
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

        ~cli_Analiz()
        {
            try { if (remoteObject != null) HostChannel.CloseProxy(remoteObject); }
            catch { }
        }

        //----------------------------------------------------------------------
        public void LoadAnaliz1(int year, out Returns ret, bool reload)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            try
            {
                remoteObject.LoadAnaliz1(year, out ret, reload);
                HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;

                MonitorLog.WriteLog("Ошибка LoadAnaliz1 " + err, MonitorLog.typelog.Error, 2, 100, true);
            }
        }
        //----------------------------------------------------------------------
        public void LoadAdres(Finder finder, out Returns ret, int year, bool reload)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            try
            {
                remoteObject.LoadAdres(finder, out ret, year, reload);
                HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;

                MonitorLog.WriteLog("Ошибка LoadAdres " + err, MonitorLog.typelog.Error, 2, 100, true);
            }
        }
        //----------------------------------------------------------------------
        public void LoadSupp(AnlSupp finder, out Returns ret, int year, bool reload)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            try
            {
                remoteObject.LoadSupp(finder, out ret, year, reload);
                HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;

                MonitorLog.WriteLog("Ошибка LoadSupp " + err, MonitorLog.typelog.Error, 2, 100, true);
            }
        }


        //----------------------------------------------------------------------
        public List<AnlXX> GetAnlXX(Finder finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            try
            {
                List<AnlXX> ls = new List<AnlXX>();
                ls = remoteObject.GetAnlXX(finder, out ret);
                HostChannel.CloseProxy(remoteObject);
                return ls;
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;

                MonitorLog.WriteLog("Ошибка GetAnlXX " + err, MonitorLog.typelog.Error, 2, 100, true);

                return null;
            }
        }
        //----------------------------------------------------------------------
        public List<AnlDom> GetAnlDom(Finder finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            try
            {
                List<AnlDom> ls = new List<AnlDom>();
                ls = remoteObject.GetAnlDom(finder, out ret);
                HostChannel.CloseProxy(remoteObject);
                return ls;
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;

                MonitorLog.WriteLog("Ошибка GetAnlDom " + err, MonitorLog.typelog.Error, 2, 100, true);

                return null;
            }
        }
        //----------------------------------------------------------------------
        public List<AnlSupp> GetAnlSupp(Finder finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            try
            {
                List<AnlSupp> ls = new List<AnlSupp>();
                ls = remoteObject.GetAnlSupp(finder, out ret);
                HostChannel.CloseProxy(remoteObject);
                return ls;
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;

                MonitorLog.WriteLog("Ошибка GetAnlSupp " + err, MonitorLog.typelog.Error, 2, 100, true);

                return null;
            }
        }


        public List<Dom> DbGetAdres(int level, int dtip, Dom finder, out Returns ret, int year)
        {
            DbAnalizClient db = new DbAnalizClient();
            List<Dom> list = db.GetAdres(level, dtip, finder, out ret, year);
            db.Close();
            return list;
        }

        public List<AnlSupp> DbGetSupp(int level, int dtip, AnlSupp finder, out Returns ret, int year)
        {
            DbAnalizClient db = new DbAnalizClient();
            List<AnlSupp> list = db.GetSupp(level, dtip, finder, out ret, year);
            db.Close();
            return list;
        }
    }
}
