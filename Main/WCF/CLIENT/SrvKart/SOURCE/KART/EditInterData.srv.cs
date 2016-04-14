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
    public class srv_EditInterData : srv_Base, I_EditInterData //сервис 
    //----------------------------------------------------------------------
    {
        //----------------------------------------------------------------------
        public void Saver (EditInterData editData, out Returns ret)
        //----------------------------------------------------------------------
        {
            DbEditInterData db = new DbEditInterData();
            ret = Utils.InitReturns();
            db.Saver(editData, out ret);
            db.Close();
        }
    }


    //----------------------------------------------------------------------
    public class cli_EditInterData : I_EditInterData  //реализация клиента сервиса 
    //----------------------------------------------------------------------
    {
        I_EditInterData remoteObject;

        //----------------------------------------------------------------------
        public cli_EditInterData(int nzp_server)
        //----------------------------------------------------------------------
        {
            if (Points.IsMultiHost && nzp_server > 0)
            {
                //определить параметры доступа
                _RServer zap = MultiHost.GetServer(nzp_server);
                remoteObject = HostBase.CreateInstance<srv_EditInterData, I_EditInterData>(zap.login, zap.pwd, zap.ip_adr + WCFParams.AdresWcfWeb.srvEditInterData);
            }
            else
            {
                remoteObject = HostBase.CreateInstance<srv_EditInterData, I_EditInterData>(WCFParams.AdresWcfWeb.Adres + WCFParams.AdresWcfWeb.srvEditInterData);
            }
        }

        ~cli_EditInterData()
        {
            try { if (remoteObject != null) HostBase.CloseProxy(remoteObject); }
            catch { }
        }

        //----------------------------------------------------------------------
        public void Saver (EditInterData editData, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            try
            {
                remoteObject.Saver(editData, out ret);

                HostBase.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = "Ошибка при сохранении данных"; //ex.Message;

                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;

                MonitorLog.WriteLog("Ошибка cli_EditInterData " + err, MonitorLog.typelog.Error, 2, 100, true);
            }
        }
    }

}
