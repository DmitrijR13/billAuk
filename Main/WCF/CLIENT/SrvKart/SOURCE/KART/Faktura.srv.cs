using System.ServiceModel;
using System.Collections.Generic;
using Bars.KP50.Faktura.Source.Base;
using STCLINE.KP50.Interfaces;
using STCLINE.KP50.Global;
using STCLINE.KP50.Client;


namespace STCLINE.KP50.Server
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.PerCall)]
    public class srv_Faktura : srv_Base, I_Faktura //
    {
        //----------------------------------------------------------------------
        public List<string> GetFaktura(Faktura finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            List<string> bill = new List<string>();

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Faktura cli = new cli_Faktura(WCFParams.AdresWcfHost.CurT_Server);
                bill = cli.GetFaktura(finder, out ret);
            }
            else
            {
                DbFaktura db = new DbFaktura();
                bill = db.GetFaktura(finder, out ret);
                db.Close();
            }
            return bill;
        }

        public List<string> GetListFaktura(out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            List<string> bill = new List<string>();

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Faktura cli = new cli_Faktura(WCFParams.AdresWcfHost.CurT_Server);
                bill = cli.GetListFaktura(out ret);
            }
            else
            {
                DbFaktura db = new DbFaktura();
                bill = db.GetListFaktura(out ret);
                db.Close();
            }
            return bill;
        }

        /// <summary>
        /// Формирует счет-фактуру для портала в фоновом режиме
        /// </summary>
        /// <param name="SessionID"></param>
        /// <returns></returns>
        public Returns GetFakturaWeb(int SessionID)
        //----------------------------------------------------------------------
        {
            DbFaktura db = new DbFaktura();
            return db.GetFakturaWeb(SessionID);
        }
    }
}
