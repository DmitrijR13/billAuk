using System;
using System.Collections.Generic;
using System.Data;
using System.ServiceModel;
using STCLINE.KP50.Client;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;
using Newtonsoft.Json;

namespace STCLINE.KP50.Server
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.PerCall)]
    public class srv_SendedMoney : srv_Base, I_SendedMoney
    {
        public string LoadSendedMoney(MoneySended finder)
        {
            string result = "";

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_SendedMoney cli = new cli_SendedMoney(WCFParams.AdresWcfHost.CurT_Server);
                result = cli.LoadSendedMoney(finder);
            }
            else
            {
                try
                {
                    using (var dbs = new DBSendedMoneyServer())
                    {
                        result = JsonConvert.SerializeObject(dbs.LoadSendedMoney(finder));
                    }
                }
                catch (Exception ex)
                {
                    MonitorLog.WriteLog("Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name + ")\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                    result = ""; // new ReturnsObjectType<DataTable>(null, false, ex.Message);
                }
            }
            return result;
        }

        public Returns SaveSendedMoney(List<MoneySended> list)
        {
            Returns ret = new Returns();

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_SendedMoney cli = new cli_SendedMoney(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.SaveSendedMoney(list);
            }
            else
            {
                try
                {
                    using (var dbs = new DBSendedMoneyServer())
                    {
                        ret = dbs.SaveSendedMoney(list);
                    }
                }
                catch (Exception ex)
                {
                    MonitorLog.WriteLog("Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name + ")\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                    ret = new Returns(false, ex.Message);
                }
            }
            return ret;
        }

        public string LoadSendedMoneyNew(MoneySended finder)
        {
            string rez = "";

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_SendedMoney cli = new cli_SendedMoney(WCFParams.AdresWcfHost.CurT_Server);
                rez = cli.LoadSendedMoneyNew(finder);
            }
            else
            {
                try
                {
                    using (var dbs = new DBSendedMoneyServer())
                    {
                        rez = JsonConvert.SerializeObject(dbs.LoadSendedMoneyNew(finder));
                    }
                }
                catch (Exception ex)
                {
                    MonitorLog.WriteLog("Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name + ")\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                    rez = "";
                }
            }
            return rez;
        }

        public Returns SaveSendedMoneyNew(List<MoneySended> list)
        {
            Returns ret = new Returns();

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_SendedMoney cli = new cli_SendedMoney(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.SaveSendedMoneyNew(list);
            }
            else
            {
                try
                {
                    using (var dbs = new DBSendedMoneyServer())
                    {
                        ret = dbs.SaveSendedMoneyNew(list);
                    }
                }
                catch (Exception ex)
                {
                    MonitorLog.WriteLog("Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name + ")\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                    ret = new Returns(false, ex.Message);
                }
            }
            return ret;
        }
    }
}
