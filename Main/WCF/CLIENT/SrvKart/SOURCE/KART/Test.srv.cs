using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text;
using System.ServiceModel.Activation;

using STCLINE.KP50.DataBase;
using STCLINE.KP50.Interfaces;
using STCLINE.KP50.Global;
using STCLINE.KP50.Server;

namespace STCLINE.KP50.Server
{
    

    public class srv_HttpTest : srv_Base, I_HttpTest
    {
        public Location JSONData_1()
        {
            return new Location("123.414", "13124.555");
        }
    }

    public class cli_HttpTest : I_HttpTest
    {
        I_HttpTest remoteObject;


        public cli_HttpTest(int nzp_server)
            : base()
        {
            if (Points.IsMultiHost && nzp_server > 0)
            {
                //определить параметры доступа
                _RServer zap = MultiHost.GetServer(nzp_server);
                remoteObject = HttpHostBase.CreateInstance<srv_HttpTest, I_HttpTest>(zap.ip_adr + WCFParams.AdresWcfWeb.srvTest);
            }
            else
            {
                remoteObject = HttpHostBase.CreateInstance<srv_HttpTest, I_HttpTest>(WCFParams.AdresWcfWeb.HttpAdres + WCFParams.AdresWcfWeb.srvTest);
            }
        }

        ~cli_HttpTest()
        {
            try { if (remoteObject != null) HostBase.CloseProxy(remoteObject); }
            catch { }
        }

        public Location JSONData_1()
        {
            Location s = new Location();
            try
            {
                s = remoteObject.JSONData_1();
                HttpHostBase.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка JSONData \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return s;
        }
    }
}
