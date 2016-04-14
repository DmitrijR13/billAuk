using System.ServiceModel;
using System.Collections.Generic;
using System.Threading;
using Bars.KP50.Faktura.Source.Base;
using STCLINE.KP50.Interfaces;
using STCLINE.KP50.Global;
using Bars.KP50.CLI.Faktura.Cli;
using Bars.KP50.DB.Faktura;


namespace STCLINE.KP50.Server
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.PerCall)]
    public class srv_Faktura : srv_Base, I_Faktura //
    {
        //----------------------------------------------------------------------
        public List<string> GetFaktura(Faktura finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            
            List<string> bill;

            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                var cli = new cli_Faktura(WCFParams.AdresWcfHost.CurT_Server);
                bill = cli.GetFaktura(finder, out ret);
            }
            else
            {
                var db = new DbFaktura();
                bill = db.GetFaktura(finder, out ret);
                db.Close();
            }
            return bill;
        }

        public List<string> GetListFaktura(out Returns ret)
        //----------------------------------------------------------------------
        {

            List<string> bill;

            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                var cli = new cli_Faktura(WCFParams.AdresWcfHost.CurT_Server);
                bill = cli.GetListFaktura(out ret);
            }
            else
            {
                var db = new DbFaktura();
                bill = db.GetListFaktura(out ret);
                db.Close();
            }
            return bill;
        }

        public int GetDefaultFaktura(Ls finder, out Returns ret)
        //----------------------------------------------------------------------
        {

            int FakturaKod = 0;

            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                var cli = new cli_Faktura(WCFParams.AdresWcfHost.CurT_Server);
                FakturaKod = cli.GetDefaultFaktura(finder, out ret);
            }
            else
            {
                var db = new DbFaktura();
                FakturaKod = db.GetDefaultFaktura(finder, out  ret);
                db.Close();
            }
            return FakturaKod;
        }

        /// <summary>
        /// Формирует счет-фактуру для портала в фоновом режиме
        /// </summary>
        /// <param name="sessionId"></param>
        /// <returns></returns>
        public Returns GetFakturaWeb(int sessionId)
        //----------------------------------------------------------------------
        {
            var db = new DbFaktura();
            return db.GetFakturaWeb(sessionId);
        }


        public List<Charge> GetBillCharge(ChargeFind finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            List<Charge> bill;

            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                var cli = new cli_Faktura(WCFParams.AdresWcfHost.CurT_Server);
                bill = cli.GetBillCharge(finder, out ret);
            }
            else
            {
                var db = new OldRTFaktura();
                bill = db.GetBillCharge(finder, out ret);

            }
            return bill;
        }


        public List<Charge> GetNewBillCharge(ChargeFind finder, out Returns ret)
        //----------------------------------------------------------------------
        {

            List<Charge> bill;

            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                var cli = new cli_Faktura(WCFParams.AdresWcfHost.CurT_Server);
                bill = cli.GetNewBillCharge(finder, out ret);
            }
            else
            {
                var db = new OldRTFaktura();
                bill = db.GetNewBillCharge(finder, out ret);

            }
            return bill;
        }
        /// <summary>
        /// Получение произвольного текста для платежного документа
        /// </summary>
        /// <param name="facturaName">Название платежного документа</param>
        /// <param name="ret"></param>
        /// <returns></returns>
        public string GetCustomTextFaktura(string facturaName, out Returns ret)
        {
            string customText = "";

            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                var cli = new cli_Faktura(WCFParams.AdresWcfHost.CurT_Server);
                customText = cli.GetCustomTextFaktura(facturaName, out ret);
            }
            else
            {
                var db = new DbFaktura();
                customText = db.GetCustomTextFaktura(facturaName, out  ret);
                db.Close();
            }
            return customText;
        }

        /// <summary>
        /// Получение произвольного текста для платежного документа
        /// </summary>
        /// <param name="facturaName">Название платежного документа</param>
        /// <param name="newCustomText"></param>
        /// <param name="ret"></param>
        /// <returns></returns>
        public void SaveCustomTextFaktura(string facturaName, string newCustomText, out Returns ret)
        {
            string customText = "";

            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                var cli = new cli_Faktura(WCFParams.AdresWcfHost.CurT_Server);
                cli.SaveCustomTextFaktura(facturaName, newCustomText, out ret);
            }
            else
            {
                var db = new DbFaktura();
                db.SaveCustomTextFaktura(facturaName, newCustomText, out  ret);
                db.Close();
            }
        }
    }
}
