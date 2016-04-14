using System;
using System.Data;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using System.Net;
using System.Security.Principal;
using System.Collections;
using System.Collections.Generic;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Interfaces;
using STCLINE.KP50.Global;

using STCLINE.KP50.Utility;

namespace STCLINE.KP50.Client
{
    public class cli_AdminHard : I_AdminHard
    {
        private I_AdminHard remoteObject;

        public cli_AdminHard(int nzp_server)
            : base()
        {
            _cli_AdminHard(nzp_server);
        }

        private void _cli_AdminHard(int nzp_server)
        {
            _RServer zap = MultiHost.GetServer(nzp_server);
            string addrHost = "";

            if (Points.IsMultiHost && nzp_server > 0)
            {
                //определить параметры доступа
                addrHost = zap.ip_adr + WCFParams.AdresWcfWeb.srvAdminHard;
                remoteObject = HostChannel.CreateInstance<I_AdminHard>(zap.login, zap.pwd, addrHost);
            }
            else
            {
                //по-умолчанию
                addrHost = WCFParams.AdresWcfWeb.Adres + WCFParams.AdresWcfWeb.srvAdminHard;
                zap.rcentr = "<локально>";
                remoteObject = HostChannel.CreateInstance<I_AdminHard>(addrHost);
            }

            //Попытка открыть канал связи
            try
            {
                ICommunicationObject proxy = remoteObject as ICommunicationObject;
                proxy.Open();
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog(
                    string.Format(
                        "Ошибка открытия Proxy {0} по адресу {1}. Сервер {2} (Код РЦ {3}) (Код сервера {4}): {5}",
                        System.Reflection.MethodBase.GetCurrentMethod().Name,
                        addrHost,
                        zap.rcentr,
                        zap.nzp_rc,
                        nzp_server,
                        ex.Message),
                    MonitorLog.typelog.Error, 2, 100, true);
            }
        }

        ~cli_AdminHard()
        {
            try
            {
                if (remoteObject != null) HostChannel.CloseProxy(remoteObject);
            }
            catch
            {
            }
        }

        /// <summary>
        /// Подготовить данные для печати ЛС
        /// </summary>
        /// <param name="finder">объект поиска</param>
        /// <returns>результат</returns>
        public Returns PreparePrintInvoices(List<PointForPrepare> finder)
        {
            var ret = new Returns();
            try
            {
                ret = remoteObject.PreparePrintInvoices(finder);
                HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = "\n " + ex.Message;

                MonitorLog.WriteLog("Ошибка PreparePrintInvoices" + err, MonitorLog.typelog.Error, 2, 100, true);
            }

            return ret;
        }

        public Returns UploadInDb(FilesImported finder, UploadOperations operation, UploadMode mode)
        {
            Returns ret;
            try
            {
                ret = remoteObject.UploadInDb(finder, operation, mode);
                HostChannel.CloseProxy(remoteObject);
                return ret;
            }
            catch (Exception ex)
            {
                ret = new Returns(false, "");

                if (ex is System.ServiceModel.EndpointNotFoundException)
                {
                    ret.text = Constants.access_error;
                    ret.tag = Constants.access_code;
                }
                else ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка UploadInDb(" + operation + ")\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                return ret;
            }
        }
    }
}
