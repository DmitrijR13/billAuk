using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;
using STCLINE.KP50.DataBase;
using System.Data;
using System.ServiceModel;

namespace STCLINE.KP50.Client
{
    public class cli_SendedMoney : cli_Base, I_SendedMoney
    {
        public cli_SendedMoney(int nzp_server) : base(nzp_server) { }

        protected ISendedMoneyRemoteObject getRemoteObject()
        {
            return getRemoteObject<ISendedMoneyRemoteObject>(WCFParams.AdresWcfWeb.srvSendedMoney);
        }

        public string LoadSendedMoney(MoneySended finder)
        {
            string rez = null;    
        
            try
            {
                using (var ro = getRemoteObject())
                {
                    rez = ro.LoadSendedMoney(finder);
                }
                
            }
            //catch (CommunicationObjectFaultedException ex)
            //{
            //    rez = new ReturnsObjectType<DataTable>(null, false, "");
            //    MonitorLog.WriteLog("Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            //}
            catch (Exception ex)
            {
                rez = "";
                MonitorLog.WriteLog("Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }

            return rez;
        }

        public Returns SaveSendedMoney(List<MoneySended> list)
        {
            Returns ret = new Returns();

            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.SaveSendedMoney(list);
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

        public string LoadSendedMoneyNew(MoneySended finder)
        {
            string rez = null;

            try
            {
                using (var ro = getRemoteObject())
                {
                    rez = ro.LoadSendedMoneyNew(finder);
                }

            }
            catch (CommunicationObjectFaultedException ex)
            {
                rez = null; //rez = new ReturnsObjectType<DataTable>(null, false, "");
                MonitorLog.WriteLog("Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                rez = null; // = new ReturnsObjectType<DataTable>(null, false, ex.Message);
                MonitorLog.WriteLog("Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }

            return rez;
        }

        public Returns SaveSendedMoneyNew(List<MoneySended> list)
        {
            Returns ret = new Returns();

            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.SaveSendedMoneyNew(list);
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
    }
}
