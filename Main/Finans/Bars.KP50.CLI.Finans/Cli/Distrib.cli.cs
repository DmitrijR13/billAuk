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
    public class cli_Distrib : cli_Base, I_Distrib
    {
        public cli_Distrib(int nzp_server) : base(nzp_server) { }

        protected IDistribRemoteObject getRemoteObject()
        {
            return getRemoteObject<IDistribRemoteObject>(WCFParams.AdresWcfWeb.srvDistrib);
        }

        public List<MoneyDistrib> GetMoneyDistrib(MoneyDistrib finder, enSrvOper oper, out Returns ret)
        {
            List<MoneyDistrib> list = null;

            try
            {
                if (oper == enSrvOper.SrvGet)
                {
                    using (var db = new DbDistribDomSuppClient(finder.nzp_user))
                    {
                        list = db.GetDistribDom(finder, out ret);
                    }
                }
                else
                {
                    using (var ro = getRemoteObject())
                    {
                        list = ro.GetMoneyDistrib(finder, oper, out ret);
                    }
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

            return list;
        }

        public List<MoneyNaud> GetMoneyNaud(MoneyNaud finder, enSrvOper oper, out Returns ret)
        {
            List<MoneyNaud> list = null;

            try
            {
                using (var ro = getRemoteObject())
                {
                    list = ro.GetMoneyNaud(finder, oper, out ret);
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

            return list;
        }

        public List<MoneyDistrib> GetMoneyDistribNew(MoneyDistrib finder, enSrvOper oper, out Returns ret)
        {
            List<MoneyDistrib> list = null;

            try
            {
                if (oper == enSrvOper.SrvGet)
                {
                    using (var db = new NewDbDistribDomSuppClient(finder.nzp_user))
                    {
                        list = db.GetDistribDom(finder, out ret);
                    }
                }
                else
                {
                    using (var ro = getRemoteObject())
                    {
                        list = ro.GetMoneyDistribNew(finder, oper, out ret);
                    }
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

            return list;
        }

        public List<MoneyNaud> GetMoneyNaudNew(MoneyNaud finder, enSrvOper oper, out Returns ret)
        {
            List<MoneyNaud> list = null;

            try
            {
                using (var ro = getRemoteObject())
                {
                    list = ro.GetMoneyNaudNew(finder, oper, out ret);
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

            return list;
        }
    }
}
