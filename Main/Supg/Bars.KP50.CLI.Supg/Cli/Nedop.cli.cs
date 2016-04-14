using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;
using STCLINE.KP50.DataBase;
using System.ServiceModel;


namespace STCLINE.KP50.Client
{
    public class cli_Nedop : cli_Base, I_Nedop
    {
        //I_Nedop remoteObject;

        INedopRemoteObject getRemoteObject()
        {
            return getRemoteObject<INedopRemoteObject>(WCFParams.AdresWcfWeb.srvNedop);
        }

        public cli_Nedop(int nzp_server)
            : base()
        {
            //_cli_Nedop(nzp_server);
        }

        //void _cli_Nedop(int nzp_server)
        //{
        //    if (Points.IsMultiHost && nzp_server > 0)
        //    {
        //        //определить параметры доступа
        //        _RServer zap = MultiHost.GetServer(nzp_server);
        //        remoteObject = HostChannel.CreateInstance<I_Nedop>(zap.login, zap.pwd, zap.ip_adr + WCFParams.AdresWcfWeb.srvNedop);
        //    }
        //    else
        //    {
        //        //по-умолчанию
        //        remoteObject = HostChannel.CreateInstance<I_Nedop>(WCFParams.AdresWcfWeb.Adres + WCFParams.AdresWcfWeb.srvNedop);
        //    }
        //}

        //~cli_Nedop()
        //{
        //    try { if (remoteObject != null) HostChannel.CloseProxy(remoteObject); }
        //    catch { }
        //}

        public List<Nedop> GetNedop(Nedop finder, enSrvOper oper, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<Nedop> listNedop = null;
            try
            {
                using (var ro = getRemoteObject())
                {
                    listNedop = ro.GetNedop(finder, oper, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetNedop\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка GetNedop\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return listNedop;
        }

        public List<_Service> GetServicesForNedop(Nedop finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<_Service> listServ = null;
            try
            {
                using (var ro = getRemoteObject())
                {
                    listServ = ro.GetServicesForNedop(finder, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetServicesForNedop\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка GetServicesForNedop\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return listServ;
        }

        public List<NedopType> GetNedopTypeForNedop(Nedop finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<NedopType> listNedopType = null;
            try
            {
                using (var ro = getRemoteObject())
                {
                    listNedopType = ro.GetNedopTypeForNedop(finder, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetNedopTypeForNedop\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка GetNedopTypeForNedop\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return listNedopType;
        }

        public List<NedopType> GetNedopWorkType(Nedop finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<NedopType> listNedopType = null;
            try
            {
                using (var ro = getRemoteObject())
                {
                    listNedopType = ro.GetNedopWorkType(finder, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetNedopWorkType\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка GetNedopWorkType\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return listNedopType;
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
                using (var ro = getRemoteObject())
                {
                    ro.SaveNedop(finder, additionalFinder, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка SaveNedop\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка SaveNedop\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
        }

        public Returns UnlockNedop(Nedop finder)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.UnlockNedop(finder);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка UnlockNedop\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка UnlockNedop\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        public static string DbMakeWhereString(Nedop finder, out Returns ret)
        {
            return DbNedopClient.MakeWhereString(finder, out ret);
        }

        public Returns FindLSDomFromDomNedop(Nedop finder)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.FindLSDomFromDomNedop(finder);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка FindLSDomFromDomNedop\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка FindLSDomFromDomNedop\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }
    }
}
