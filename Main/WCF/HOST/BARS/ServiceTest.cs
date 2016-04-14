using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Security.Cryptography.X509Certificates;

using STCLINE.KP50.Global;
using STCLINE.KP50.Bars.Interfaces;


namespace BARS
{
    public partial class ServiceTest : Form
    {
        public ServiceTest()
        {
            InitializeComponent();

            //пока явно определим
            Constants.cons_Webdata = "data source=MAMBA;initial catalog=D:\\Komplat.Lite\\DBKOM_kazan21.fdb;user id=SYSDBA;password=masterkey;dialect=3;port number=3051;connection lifetime=30;pooling=True;packet size=8192;isolation level=ReadCommitted;character set=WIN1251";

            Constants.Login = "Administrator"; //для записей в лог
            Constants.Password = "rubin";

            MonitorLog.StartLog("STCLINE.KP50.BARS", "Старт приложения");
        }


        string CallSrv1(AdresID acc)
        {
            return null;
            /*
            AdresResult ar = new AdresResult();
            AdresClient client = new AdresClient();

            acc.numID = 5132006292;
            acc.numFlat = "162";

            //client.ClientCredentials.ServiceCertificate.Authentication.CertificateValidationMode =
            //    System.ServiceModel.Security.X509CertificateValidationMode.PeerTrust;
            
            client.ClientCredentials.ClientCertificate.SetCertificate(
                StoreLocation.CurrentUser,
                StoreName.My,
                X509FindType.FindBySubjectName,
                "webxp Client"
                );
            
            ar = client.GetAdresString(acc);
            client.Close();
            
            return ar.adres;
             */
        }

        string CallSrv2()
        {
            return null;
            /*
            AboutResult ar = new AboutResult();
            AboutCompanyClient client = new AboutCompanyClient();

            //client.ClientCredentials.ServiceCertificate.Authentication.CertificateValidationMode =
            //    System.ServiceModel.Security.X509CertificateValidationMode.PeerTrust;
            client.ClientCredentials.ClientCertificate.SetCertificate(
                StoreLocation.CurrentUser,
                StoreName.My,
                X509FindType.FindBySubjectName,
                "bars Client"
                );

            ar = client.GetAboutCompany();
            client.Close();

            return ar.about.nameCompany + " " + ar.about.nameSoft + " " + ar.about.version;
             */
        }

        string CallSrv(AdresID acc)
        {
            /*
            AdresResult ar = new AdresResult();
            AdresClient client = new AdresClient();

            acc.numID = 5132006292;
            acc.numFlat = "162";

            ar = client.GetAdresString(acc);

            return ar.adres;
             */
            return null;
        }


        string CallBase(AdresID acc)
        {
            //Returns ret;
            //string s = DbAdres.GetAdresString(0,0, "", out ret);
            return "";
        }

        private void button1_Click(object sender, EventArgs e)
        {
            AdresID acc = new AdresID();
            //acc.numID = Convert.ToInt64(txtAcc.Text);
            //acc.numFlat = txtFlat.Text;

            //lblRes.Text = Utils.EncodePKod("5132", 629).ToString();
            //lblRes.Text = CallBase(acc);
            lblRes.Text = CallSrv1(acc);
            lblCompany.Text = CallSrv2();
        }
    }
}
