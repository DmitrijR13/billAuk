using System;
using System.Collections;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Web;
using System.Web.Security;
using System.Web.SessionState;
using System.Xml.Linq;
using System.Web.UI.WebControls;
using STCLINE.KP50.Global;
using System.Web.Configuration;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.SrvBase;


namespace STCLINE.KP50.WebServ
{
    public class Global : System.Web.HttpApplication
    {
        protected void Application_Start(object sender, EventArgs e)
        {
            Constants.Debug        = true; //
            Constants.Viewerror    = true; //режим раскрытия ошибки

            try
            {
                Constants.cons_Webdata = WebConfigurationManager.ConnectionStrings[DataBaseHead.ConfPref + "1"].ConnectionString; //Webdata
                Constants.cons_User    = WebConfigurationManager.ConnectionStrings[DataBaseHead.ConfPref + "2"].ConnectionString; //User
                AdresWCF.Adres         = WebConfigurationManager.ConnectionStrings[DataBaseHead.ConfPref + "3"].ConnectionString; //Adres

                Constants.cons_Webdata = Encryptor.Decrypt(Constants.cons_Webdata,null);
                Constants.cons_User    = Encryptor.Decrypt(Constants.cons_User, null);
                AdresWCF.Adres         = Encryptor.Decrypt(AdresWCF.Adres, null);
                
                Utils.UserLogin();
            }
            catch
            {
                Constants.Login = "";
                return;
            }

            if ( Constants.cons_Webdata == null ||
                 Constants.Login        == ""   ||
                 Constants.Password     == ""   ||
                 AdresWCF.Adres         == ""
               )
            {
                Constants.Login = "";
                return;
            }

            MonitorLog.StartLog("STCLINE.KP50.WebServ", "Старт приложения");
            //SrvRun.WebHostingStart();
            //DbSprav.StartApp();
        }


        protected void Session_Start(object sender, EventArgs e)
        {
            //
        }

        protected void Application_BeginRequest(object sender, EventArgs e)
        {

        }

        protected void Application_AuthenticateRequest(object sender, EventArgs e)
        {

        }

        protected void Application_Error(object sender, EventArgs e)
        {

        }

        protected void Session_End(object sender, EventArgs e)
        {

        }

        protected void Application_End(object sender, EventArgs e)
        {
            MonitorLog.Close("Остановка приложения");
        }

    }
}