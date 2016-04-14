using System;
using System.Collections.Generic;
using System.Data;
using System.ServiceModel;
using STCLINE.KP50.Client;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;

namespace STCLINE.KP50.Server
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.PerCall)]
    public class srv_Distrib : srv_Base, I_Distrib
    {
        public List<MoneyDistrib> GetMoneyDistrib(MoneyDistrib finder, enSrvOper oper, out Returns ret)
        {
            List<MoneyDistrib> list = null;

            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Charge cli = new cli_Charge(WCFParams.AdresWcfHost.CurT_Server);
                list = cli.GetMoneyDistribDom(finder, oper, out ret);
            }
            else
            {
                try
                {
                    switch (oper)
                    {
                        case enSrvOper.SrvFind:
                            using (var dbs = new DbDistribDomSuppServer())
                            {
                                dbs.FindDistribDom(finder, out ret);
                            }
                            if (ret.result)
                            {
                                using (var dbc = new DbDistribDomSuppClient(finder.nzp_user))
                                {
                                    list = dbc.GetDistribDom(finder, out ret);
                                }
                            }
                            break;
                        case enSrvOper.SrvGet:
                            using (var dbc = new DbDistribDomSuppClient(finder.nzp_user))
                            {
                                list = dbc.GetDistribDom(finder, out ret);
                            }
                            break;
                        default:
                            ret = new Returns(false, "Неверное наименование операции");
                            break;
                    }
                }
                catch (Exception ex)
                {
                    MonitorLog.WriteLog("Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name + "(" + oper.ToString() + ")\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                    ret = new Returns(false, ex.Message);
                }
            }
            return list;
        }

        public List<MoneyNaud> GetMoneyNaud(MoneyNaud finder, enSrvOper oper, out Returns ret)
        {
            List<MoneyNaud> list = null;

            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Distrib cli = new cli_Distrib(WCFParams.AdresWcfHost.CurT_Server);
                list = cli.GetMoneyNaud(finder, oper, out ret);
            }
            else
            {
                try
                {
                    using (var db = new DbDistribDomSuppServer())
                    {
                        switch (oper)
                        {
                            case enSrvOper.SrvFind:
                                list = db.FindMoneyNaud(finder, out ret);
                                break;
                            default:
                                ret = new Returns(false, "Неверное наименование операции");
                                break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    MonitorLog.WriteLog("Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name + "(" + oper.ToString() + ") " + (Constants.Viewerror ? "\n" + ex.Message : ""), MonitorLog.typelog.Error, 2, 100, true);
                    ret = new Returns(false, ex.Message);
                }
            }
            return list;
        }

        public List<MoneyDistrib> GetMoneyDistribNew(MoneyDistrib finder, enSrvOper oper, out Returns ret)
        {
            List<MoneyDistrib> list = null;

            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Charge cli = new cli_Charge(WCFParams.AdresWcfHost.CurT_Server);
                list = cli.GetMoneyDistribDom(finder, oper, out ret);
            }
            else
            {
                try
                {
                    switch (oper)
                    {
                        case enSrvOper.SrvFind:
                            using (var dbs = new NewDbDistribDomSuppServer())
                            {
                                dbs.FindDistribDom(finder, out ret);
                            }
                            if (ret.result)
                            {
                                using (var dbc = new NewDbDistribDomSuppClient(finder.nzp_user))
                                {
                                    list = dbc.GetDistribDom(finder, out ret);
                                }
                            }
                            break;
                        case enSrvOper.SrvGet:
                            using (var dbc = new NewDbDistribDomSuppClient(finder.nzp_user))
                            {
                                list = dbc.GetDistribDom(finder, out ret);
                            }
                            break;
                        default:
                            ret = new Returns(false, "Неверное наименование операции");
                            break;
                    }
                }
                catch (Exception ex)
                {
                    MonitorLog.WriteLog("Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name + "(" + oper.ToString() + ")\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                    ret = new Returns(false, ex.Message);
                }
            }
            return list;
        }

        public List<MoneyNaud> GetMoneyNaudNew(MoneyNaud finder, enSrvOper oper, out Returns ret)
        {
            List<MoneyNaud> list = null;

            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Distrib cli = new cli_Distrib(WCFParams.AdresWcfHost.CurT_Server);
                list = cli.GetMoneyNaudNew(finder, oper, out ret);
            }
            else
            {
                try
                {
                    using (var db = new NewDbDistribDomSuppServer())
                    {
                        switch (oper)
                        {
                            case enSrvOper.SrvFind:
                                list = db.FindMoneyNaud(finder, out ret);
                                break;
                            default:
                                ret = new Returns(false, "Неверное наименование операции");
                                break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    MonitorLog.WriteLog("Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name + "(" + oper.ToString() + ") " + (Constants.Viewerror ? "\n" + ex.Message : ""), MonitorLog.typelog.Error, 2, 100, true);
                    ret = new Returns(false, ex.Message);
                }
            }
            return list;
        }
    }
}
