using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;
using STCLINE.KP50.DataBase;

namespace STCLINE.KP50.Client
{
    public class cli_Nedop : I_Nedop
    {
        I_Nedop remoteObject;

        public cli_Nedop(int nzp_server)
            : base()
        {
            _cli_Nedop(nzp_server);
        }

        void _cli_Nedop(int nzp_server)
        {
            if (Points.IsMultiHost && nzp_server > 0)
            {
                //определить параметры доступа
                _RServer zap = MultiHost.GetServer(nzp_server);
                remoteObject = HostChannel.CreateInstance<I_Nedop>(zap.login, zap.pwd, zap.ip_adr + WCFParams.AdresWcfWeb.srvNedop);
            }
            else
            {
                //по-умолчанию
                remoteObject = HostChannel.CreateInstance<I_Nedop>(WCFParams.AdresWcfWeb.Adres + WCFParams.AdresWcfWeb.srvNedop);
            }
        }

        ~cli_Nedop()
        {
            try { if (remoteObject != null) HostChannel.CloseProxy(remoteObject); }
            catch { }
        }

        public List<Nedop> GetNedop(Nedop finder, enSrvOper oper, out Returns ret)
        {
            try
            {
                List<Nedop> listNedop = remoteObject.GetNedop(finder, oper, out ret);
                HostChannel.CloseProxy(remoteObject);
                return listNedop;
            }
            catch (Exception ex)
            {
                if (ex is System.ServiceModel.EndpointNotFoundException) ret = new Returns(false, Constants.access_error, Constants.access_code);
                else ret = new Returns(false, ex.Message);

                MonitorLog.WriteLog("Ошибка GetNedop(" + oper + ") " + (Constants.Viewerror ? "\n " + ex.Message : ""), MonitorLog.typelog.Error, 2, 100, true);
                return null;
            }
        }

        public List<_Service> GetServicesForNedop(Nedop finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            try
            {
                List<_Service> listServ = remoteObject.GetServicesForNedop(finder, out ret);
                HostChannel.CloseProxy(remoteObject);
                return listServ;
            }
            catch (Exception ex)
            {
                ret.result = false;
                if (ex is System.ServiceModel.EndpointNotFoundException)
                {
                    ret.text = Constants.access_error;
                    ret.tag = Constants.access_code;
                }
                else ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;

                MonitorLog.WriteLog("Ошибка GetServicesForNedop() " + err, MonitorLog.typelog.Error, 2, 100, true);
                return null;
            }
        }

        public List<NedopType> GetNedopTypeForNedop(Nedop finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            try
            {
                List<NedopType> listNedopType = remoteObject.GetNedopTypeForNedop(finder, out ret);
                HostChannel.CloseProxy(remoteObject);
                return listNedopType;
            }
            catch (Exception ex)
            {
                ret.result = false;
                if (ex is System.ServiceModel.EndpointNotFoundException)
                {
                    ret.text = Constants.access_error;
                    ret.tag = Constants.access_code;
                }
                else ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;

                MonitorLog.WriteLog("Ошибка GetNedopTypeForNedop() " + err, MonitorLog.typelog.Error, 2, 100, true);
                return null;
            }
        }

        public List<NedopType> GetNedopWorkType(Nedop finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            try
            {
                List<NedopType> listNedopType = remoteObject.GetNedopWorkType(finder, out ret);
                HostChannel.CloseProxy(remoteObject);
                return listNedopType;
            }
            catch (Exception ex)
            {
                ret.result = false;
                if (ex is System.ServiceModel.EndpointNotFoundException)
                {
                    ret.text = Constants.access_error;
                    ret.tag = Constants.access_code;
                }
                else ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;

                MonitorLog.WriteLog("Ошибка GetNedopWorkType() " + err, MonitorLog.typelog.Error, 2, 100, true);
                return null;
            }
        }

        public void SaveNedop(Nedop finder, out Returns ret)
        {
            SaveNedop(finder, null, out ret);
        }

        public void SaveNedop(Nedop finder, Nedop additionalFinder, out Returns ret)
        {
            ret = Utils.InitReturns();
            try
            {
                remoteObject.SaveNedop(finder, additionalFinder, out ret);
                HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                ret.result = false;
                if (ex is System.ServiceModel.EndpointNotFoundException)
                {
                    ret.text = Constants.access_error;
                    ret.tag = Constants.access_code;
                }
                else ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;

                MonitorLog.WriteLog("Ошибка SaveNedop() " + err, MonitorLog.typelog.Error, 2, 100, true);
                return;
            }
        }

        public Returns UnlockNedop(Nedop finder)
        {
            Returns ret;
            try
            {
                ret = remoteObject.UnlockNedop(finder);
                HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                if (ex is System.ServiceModel.EndpointNotFoundException) ret = new Returns(false, Constants.access_error, Constants.access_code);
                else ret = new Returns(false, ex.Message);

                MonitorLog.WriteLog("Ошибка UnlockNedop() " + (Constants.Viewerror ? "\n " + ex.Message : ""), MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        public static string DbMakeWhereString(Nedop finder, out Returns ret)
        {
            return DbNedopClient.MakeWhereString(finder, out ret);
        }

        public Returns FindLSDomFromDomNedop(Nedop finder)
        {
            Returns ret;
            try
            {
                ret = remoteObject.FindLSDomFromDomNedop(finder);
                HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                ret = new Returns(false, ex.Message);
                MonitorLog.WriteLog("Ошибка FindLSDomFromDomNedop()" + (Constants.Viewerror ? "\n" + ex.Message : ""), MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }
    }
}
