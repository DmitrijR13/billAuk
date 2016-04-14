namespace STCLINE.KP50.CliKart.SOURCE.REPORT
{
    using System;
    using System.ServiceModel;
    using Globals.SOURCE.INTF.Report;
    using STCLINE.KP50.Client;
    using STCLINE.KP50.Global;
    using STCLINE.KP50.Interfaces;

    public class cli_BaseReport: IReportService
    {
        readonly IReportService _remoteObject;

        public cli_BaseReport() :this(0)
        {
        }

        public cli_BaseReport(int nzp_server)
        {
            string addrHost = "";
            //определить параметры доступа
            _RServer zap = MultiHost.GetServer(nzp_server);

            if (Points.IsMultiHost && nzp_server > 0)
            {
                addrHost = zap.ip_adr + WCFParams.AdresWcfWeb.srvBaseReport;
                _remoteObject = HostChannel.CreateInstance<cli_BaseReport>(zap.login, zap.pwd, addrHost);
            }
            else
            {
                //по-умолчанию
                addrHost = WCFParams.AdresWcfWeb.Adres + WCFParams.AdresWcfWeb.srvBaseReport;
                zap.rcentr = "<локально>";
                _remoteObject = HostChannel.CreateInstance<IReportService>(Constants.Login, Constants.Password, addrHost, "BaseReport");
            }


            //Попытка открыть канал связи
            try
            {
                ICommunicationObject proxy = _remoteObject as ICommunicationObject;
                proxy.Open();
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog(string.Format("Ошибка открытия Proxy {0} по адресу {1}. Сервер {2} (Код РЦ {3}) (Код сервера {4}): {5}",
                                    System.Reflection.MethodBase.GetCurrentMethod().Name,
                                    addrHost,
                                    zap.rcentr,
                                    zap.nzp_rc,
                                    nzp_server,
                                    ex.Message),
                                    MonitorLog.typelog.Error, 2, 100, true);
            }
        }

        public cli_BaseReport(string conn, string login, string pass)
        {
            Constants.Login = login;
            Constants.Password = pass;
            _remoteObject = HostChannel.CreateInstance<IReportService>(login, pass, conn + WCFParams.AdresWcfWeb.srvBaseReport, "BaseReport");
        }

        public ReturnsObjectType<ReportResult> PrintReport(string reportId, string userParams, string userFilters, int curReportKind,string userName, int nzpObject)
        {
            try
            {
                var ret = _remoteObject.PrintReport(reportId, userParams, userFilters, curReportKind, userName, nzpObject);
                HostChannel.CloseProxy(_remoteObject);
                return ret;
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка формирования отчета: " + ex.Message, MonitorLog.typelog.Error, true);
                return new ReturnsObjectType<ReportResult>(null, false, "Ошибка формирования отчета: " + ex.Message);
            }
        }
    }
}
