using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using System.Web;
using System.Web.Configuration;
using System.Configuration;
using System.ServiceModel.Activation;

using STCLINE.KP50.Global;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Bars.Interfaces;

namespace STCLINE.KP50.Bars.Services
{
    [ServiceBehavior(Namespace = Constants.Linespace, Name = "ServiceCounter")]
    [AspNetCompatibilityRequirements(RequirementsMode = AspNetCompatibilityRequirementsMode.Required)]
    public class SrvCounter : ICounter
    {
        //-------------------------------------------------------------
        public SrvCounter()
        //-------------------------------------------------------------
        {
            /*
            //Constants.cons_Webdata = "data source=local;initial catalog=D:\\Komplat.Lite\\Kazan21\\DBKOM_kazan21.fdb;user id=SYSDBA;password=masterkey;dialect=3;port number=3051;connection lifetime=30;pooling=True;packet size=8192;isolation level=ReadCommitted;character set=WIN1251";
            Constants.cons_Webdata = "data source=RUBIN;initial catalog=D:\\Komplat.Lite\\kazan21\\DBKOM_kazan21.fdb;user id=SYSDBA;password=masterkey;dialect=3;port number=3050;connection lifetime=30;pooling=True;packet size=8192;isolation level=ReadCommitted;character set=WIN1251";
            Constants.Login = "Administrator"; //для записей в лог
            Constants.Password = "rubin";
            MonitorLog.StartLog("STCLINE.KP50.BARS", "Старт приложения");
            */
        }

        //-------------------------------------------------------------
        public CounterResult GetCounters(AdresID adresID)
        //-------------------------------------------------------------
        {
            Returns ret = Utils.InitReturns();
            CounterResult cr = new CounterResult();

            if (adresID.numID < 1 || adresID.numFlat == "")
            {
                ret.tag = Constants.svc_wrongdata;
                SrvUtils.BarsReturns(ref cr.retcode);
                cr.counters = null;
                return cr;
            }
            string pkod = adresID.numID.ToString();
            int kod_erc;
            int num_ls;

            //декодирование платежного кода
            ret = Utils.DecodePKod(pkod, out kod_erc, out num_ls);
            if (!ret.result)
            {
                ret.tag = Constants.svc_pk_Format;
                SrvUtils.BarsReturns(ref cr.retcode);
                cr.counters = null;
                return cr;
            }

            //обращение к базе
            //DbAdresBars.GetAdresString(out ret, kod_erc, num_ls, adresID.numFlat);
            if (!ret.result)
            {
                SrvUtils.BarsReturns(ref cr.retcode);
                cr.counters = null;
                return cr;
            }

            //cr.counters = DbCounterBars.GetCounters(out ret, kod_erc, num_ls, adresID.numFlat);
            SrvUtils.BarsReturns(ref cr.retcode);

            return cr;
        }
    }
}
