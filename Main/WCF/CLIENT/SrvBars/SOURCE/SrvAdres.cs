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

    [ServiceBehavior(Namespace = Constants.Linespace, Name = "ServiceAbout")]
    [AspNetCompatibilityRequirements(RequirementsMode = AspNetCompatibilityRequirementsMode.Required)]
    public class SrvAboutCompany : IAboutCompany
    {
        //-------------------------------------------------------------
        public SrvAboutCompany()
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
        public AboutResult GetAboutCompany()
        //-------------------------------------------------------------
        {
            AboutResult ar = new AboutResult();

            //ar.about = DbAdresBars.GetAboutCompany(out ar.retcode);
            if (HttpContext.Current.Request.IsSecureConnection)
            {
                //бизнес-логика
                ar.about.nameCompany = " about SN=" + Utils.GetSN(HttpContext.Current.Request.ClientCertificate.SerialNumber);
            }
            else
            {
                ar.about.nameCompany = " Test Company ";
            }

            SrvUtils.BarsReturns(ref ar.retcode);
            return ar;
        }
    }


    [ServiceBehavior(Namespace = Constants.Linespace, Name = "ServiceAdres")]
    [AspNetCompatibilityRequirements(RequirementsMode = AspNetCompatibilityRequirementsMode.Required)]
    public class SrvAdres : IAdres
    {
        //-------------------------------------------------------------
        public SrvAdres()
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
        public AdresResult GetAdresString(AdresID adresID)
        //-------------------------------------------------------------
        {
            AdresResult ar = new AdresResult();

            if (adresID.numID < 1 || adresID.numFlat == "")
            {
                ar.retcode.tag = Constants.svc_wrongdata;
                SrvUtils.BarsReturns(ref ar.retcode);
                ar.adres = "-";
                return ar;
            }
            string pkod = adresID.numID.ToString();
            int kod_erc;
            int num_ls;

            //декодирование платежного кода
            ar.retcode = Utils.DecodePKod(pkod, out kod_erc, out num_ls);
            if (!ar.retcode.result)
            {
                ar.retcode.tag = Constants.svc_pk_Format;
                SrvUtils.BarsReturns(ref ar.retcode);
                ar.adres = "-";
                return ar;
            }

            //обращение к базе
            //ar.adres = DbAdresBars.GetAdresString(out ar.retcode, kod_erc, num_ls, adresID.numFlat);
            ar.adres = "AdresString: ";//а так будет вызов локальной службы (SrvKart) 

            //вытащим данные сертификата
            if (HttpContext.Current.Request.IsSecureConnection)
            {
                ar.adres += " SN=" + Utils.GetSN(HttpContext.Current.Request.ClientCertificate.SerialNumber);
            }
            else
            {
                ar.adres = " Test Address ";
            }

            SrvUtils.BarsReturns(ref ar.retcode);
            return ar;
        }

        //-------------------------------------------------------------
        public AdresResult ValidNumID(string numID)
        //-------------------------------------------------------------
        {
            AdresResult ar = new AdresResult();

            int kod_erc;
            int num_ls;

            //декодирование платежного кода
            ar.retcode = Utils.DecodePKod(numID, out kod_erc, out num_ls);

            SrvUtils.BarsReturns(ref ar.retcode);
            return ar;
        }
    }

    //----------------------------------------------------------------------
    static public class SrvUtils //сервисные утилиты
    //----------------------------------------------------------------------
    {
        //----------------------------------------------------------------------
        static public void BarsReturns(ref Returns ret) //инициализация переменной Returns для возврата в сервисах
        //----------------------------------------------------------------------
        {
            if (!ret.result)
            {
                int i = Constants.svc_sqlerror;
                if (ret.tag < 0)
                {
                    i = Math.Abs(ret.tag);

                    if (i > Constants.svc_errors.Length)
                        i = Constants.svc_sqlerror;
                }
                ret.text = Constants.svc_errors[i];
            }
            else
                ret =  Utils.InitReturns();

        }
    }

}
