using System;
using System.Collections.Generic;
using System.IO;
using STCLINE.KP50.Global;

using STCLINE.KP50.Interfaces;
using STCLINE.KP50.DataBase;
using System.Data;

namespace STCLINE.KP50.Client
{
    //----------------------------------------------------------------------
    public class cli_Faktura: I_Faktura  //реализация клиента 
    //----------------------------------------------------------------------
    {

        I_Faktura remoteObject;

        //----------------------------------------------------------------------
        public cli_Faktura(int nzp_server)
            : base()
        //----------------------------------------------------------------------
        {
            //remoteObject = HostChannel.CreateInstance<I_Faktura>(WCFParams.Adres + WCFParams.srvFaktura); 
            remoteObject = HostChannel.CreateInstance<I_Faktura>(WCFParams.AdresWcfWeb.Adres + WCFParams.AdresWcfWeb.srvFaktura); 
        }
        //----------------------------------------------------------------------

        ~cli_Faktura()
        {
            try { if (remoteObject != null) HostChannel.CloseProxy(remoteObject); }
            catch { }
        }

        //----------------------------------------------------------------------
        public List<string> GetFaktura(Faktura finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();

            List<string> bill = new List<string>();
            try
            {
                bill = remoteObject.GetFaktura(finder, out ret);
                HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = "Ошибка при формировании счета";

                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;

                MonitorLog.WriteLog("Ошибка GetFaktura " + err, MonitorLog.typelog.Error, 2, 100, true);
            }
            return bill;
        }

        //----------------------------------------------------------------------
        public List<string> GetListFaktura(out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();

            List<string> bill = new List<string>();
            try
            {
                bill = remoteObject.GetListFaktura( out ret);
                HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = "Ошибка при формировании счета";

                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;

                MonitorLog.WriteLog("Ошибка GetFaktura " + err, MonitorLog.typelog.Error, 2, 100, true);
            }
            return bill;
        }

        public Returns GetFakturaWeb(int SessionID)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                ret = remoteObject.GetFakturaWeb(SessionID);
                HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = "Ошибка при формировании счета";

                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;

                MonitorLog.WriteLog("Ошибка GetFakturaWeb " + err, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }
    }
}