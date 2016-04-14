using System;
using System.Collections.Generic;
using STCLINE.KP50.Client;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;

namespace STCLINE.KP50.Server
{
    public class srv_Odn : srv_Base, I_Odn
    {
        public List<Odn> GetOdn(OdnFinder finder, enSrvOper oper, out Returns ret)
        {
            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Odn cli = new cli_Odn(WCFParams.AdresWcfHost.CurT_Server);
                return cli.GetOdn(finder, oper, out ret);
            }
            else
            {
                ret = Utils.InitReturns();
                DbOdn db = new DbOdn();
                List<Odn> res = null;
                try
                {
                    switch (oper)
                    {
                        case enSrvOper.SrvFind:
                            db.FindOdn(finder, out ret);
                            if (ret.result) res = db.GetOdn(finder, out ret);
                            else res = null; 
                            break;
                        case enSrvOper.SrvGet: 
                            res = db.GetOdn(finder, out ret); 
                            break;
                        default: 
                            res = null; 
                            break;
                    }
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции";
                    if (Constants.Viewerror) ret.text += ": " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка CallSrvOdn()\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                    res = null;
                }
                db.Close();
                return res;
            }
        }
    }
}
