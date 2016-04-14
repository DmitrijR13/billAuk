using System;
using System.Collections.Generic;
using STCLINE.KP50.Client;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;
using System.ServiceModel;

namespace Bars.KP50.CLI.Faktura.Cli
{
    //----------------------------------------------------------------------
    public class cli_Faktura: cli_Base, I_Faktura  //реализация клиента 
    //----------------------------------------------------------------------
    {

        //I_Faktura remoteObject;

        //----------------------------------------------------------------------
        public cli_Faktura(int nzpServer)
            : base()
        {
            //remoteObject = HostChannel.CreateInstance<I_Faktura>(WCFParams.Adres + WCFParams.srvFaktura); 
            //remoteObject = HostChannel.CreateInstance<I_Faktura>(WCFParams.AdresWcfWeb.Adres + WCFParams.AdresWcfWeb.srvFaktura); 
        }

        IFakturaRemoteObject getRemoteObject()
        {
            return getRemoteObject<IFakturaRemoteObject>(WCFParams.AdresWcfWeb.srvFaktura);
        }
        //----------------------------------------------------------------------

        //~cli_Faktura()
        //{
        //    try { if (remoteObject != null) HostChannel.CloseProxy(remoteObject); }
        //    catch { }
        //}

        //----------------------------------------------------------------------
        public List<string> GetFaktura(STCLINE.KP50.Interfaces.Faktura finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            List<string> bill = new List<string>();
            try
            {
                using (var ro = getRemoteObject())
                {
                    bill = ro.GetFaktura(finder, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetFaktura\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка GetFaktura\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
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
                using (var ro = getRemoteObject())
                {
                    bill = ro.GetListFaktura(out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetListFaktura\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка при формировании счета";
                MonitorLog.WriteLog("Ошибка GetListFaktura\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return bill;
        }

        public string GetCustomTextFaktura(string facturaName, out Returns ret)
        {
            ret = Utils.InitReturns();

            string customText = "";
            try
            {
                using (var ro = getRemoteObject())
                {
                    customText = ro.GetCustomTextFaktura(facturaName, out ret);
                }
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = "Ошибка при формировании счета";

                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;

                MonitorLog.WriteLog("Ошибка GetCustomTextFaktura " + err, MonitorLog.typelog.Error, 2, 100, true);
            }
            return customText;
        }

        public void SaveCustomTextFaktura(string facturaName, string newCustomText, out Returns ret)
        {
            ret = Utils.InitReturns();

            try
            {
                using (var ro = getRemoteObject())
                {
                    ro.SaveCustomTextFaktura(facturaName, newCustomText, out ret);
                }
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = "Ошибка при формировании счета";

                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;

                MonitorLog.WriteLog("Ошибка SaveCustomTextFaktura " + err, MonitorLog.typelog.Error, 2, 100, true);
            }
        }


        public int GetDefaultFaktura(Ls finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();

            int FakturaKod = 0;
            try
            {
                using (var ro = getRemoteObject())
                {
                    FakturaKod = ro.GetDefaultFaktura(finder, out ret);
                }
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = "Ошибка при формировании счета";

                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;

                MonitorLog.WriteLog("Ошибка GetDefaultFaktura " + err, MonitorLog.typelog.Error, 2, 100, true);
            }
            return FakturaKod;
        }

        public Returns GetFakturaWeb(int sessionId)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.GetFakturaWeb(sessionId);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetFakturaWeb\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка при формировании счета";
                MonitorLog.WriteLog("Ошибка GetFakturaWeb\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }


      
      
        //----------------------------------------------------------------------
        public List<Charge> GetBillCharge(ChargeFind finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            List<Charge> bill = new List<Charge>();
            try
            {
                using (var ro = getRemoteObject())
                {
                    bill = ro.GetBillCharge(finder, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetBillCharge\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка при формировании счета";
                MonitorLog.WriteLog("Ошибка GetBillCharge\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return bill;
        }


        public List<Charge> GetNewBillCharge(ChargeFind finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            List<Charge> bill = new List<Charge>();
            try
            {
                using (var ro = getRemoteObject())
                {
                    bill = ro.GetNewBillCharge(finder, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetNewBillCharge\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка при формировании счета";
                MonitorLog.WriteLog("Ошибка GetNewBillCharge\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return bill;
        }
    }
}