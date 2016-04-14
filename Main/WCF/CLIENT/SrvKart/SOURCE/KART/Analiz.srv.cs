using System;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using System.Net;
using System.Security.Principal;

using System.Collections.Generic;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Interfaces;
using STCLINE.KP50.Global;
using STCLINE.KP50.Client;

namespace STCLINE.KP50.Server
{
    //----------------------------------------------------------------------
    public class srv_Analiz : srv_Base, I_Analiz //сервис справочников
    //----------------------------------------------------------------------
    {
        //----------------------------------------------------------------------
        public void LoadAnaliz1(int year, out Returns ret, bool reload)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Analiz cli = new cli_Analiz(WCFParams.AdresWcfHost.CurT_Server);
                cli.LoadAnaliz1(year, out ret, reload);
            }
            else
            {
                using (var db = new DbAnaliz())
                {
                    db.LoadAnaliz1(out ret, year, reload);
                }
            }
        }
        //----------------------------------------------------------------------
        public void LoadAdres(Finder finder, out Returns ret, int year, bool reload)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Analiz cli = new cli_Analiz(WCFParams.AdresWcfHost.CurT_Server);
                cli.LoadAdres(finder, out ret, year, reload);
            }
            else
            {
                using (var db = new DbAnaliz())
                {

                    db.LoadAdres(finder, out ret, year, reload);
                }
            }
        }
        //----------------------------------------------------------------------
        public void LoadSupp(AnlSupp finder, out Returns ret, int year, bool reload)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Analiz cli = new cli_Analiz(WCFParams.AdresWcfHost.CurT_Server);
                cli.LoadSupp(finder, out ret, year, reload);
            }
            else
            {
                using (var db = new DbAnaliz())
                {
                    db.LoadSupp(finder, out ret, year, reload);
                }
            }
        }

        //----------------------------------------------------------------------
        public List<AnlXX> GetAnlXX(Finder finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            List<AnlXX> res = new List<AnlXX>();

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Analiz cli = new cli_Analiz(WCFParams.AdresWcfHost.CurT_Server);


                try
                {
                    res = cli.GetAnlXX(finder, out ret);
                }
                catch (Exception ex)
                {
                    MonitorLog.WriteLog("Ошибка GetAnlXX " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }

                
                
            }
            else
            {
                using (var db = new DbAnaliz())
                {
                    res = db.GetAnlXX(finder, out ret);
                }
            }
            return res;
        }
        //----------------------------------------------------------------------
        public List<AnlDom> GetAnlDom(Finder finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            List<AnlDom> res = new List<AnlDom>();

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Analiz cli = new cli_Analiz(WCFParams.AdresWcfHost.CurT_Server);
                res = cli.GetAnlDom(finder, out ret);
            }
            else
            {
                using (var db = new DbAnaliz())
                {
                    res = db.GetAnlDom(finder, out ret);
                }
            }
            return res;
        }
        //----------------------------------------------------------------------
        public List<AnlSupp> GetAnlSupp(Finder finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            List<AnlSupp> res = new List<AnlSupp>();

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Analiz cli = new cli_Analiz(WCFParams.AdresWcfHost.CurT_Server);
                res = cli.GetAnlSupp(finder, out ret); ;
            }
            else
            {
                using (var db = new DbAnaliz())
                {
                    res = db.GetAnlSupp(finder, out ret);
                }
            }
            return res;
        }
    }
}
