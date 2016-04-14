namespace Bars.KP50.Report
{
    using System.Configuration;

    using Bars.KP50.Utils;

    public class ManagerParams
    {
        public string WebBdConnectionString { get; private set; }

        public string HostBdConnectionString { get; private set; }

        public string MainSchema { get; private set; }

        public string ReportSavePath { get; private set; }

        public bool UseFtp { get; private set; }

        public string FtpAddress { get; private set; }

        public string FtpUserName { get; private set; }

        public string FtpUserPassword { get; private set; }

        public bool FtpUseProxy { get; private set; }

        public string FtpProxyAddress { get; private set; }

        public string FtpProxyUserName { get; private set; }

        public string FtpProxyUserPassword { get; private set; }

        public string FtpProxyDomain { get; private set; }

        public static ManagerParams GetParams()
        {
            return new ManagerParams
            {
                WebBdConnectionString = ConfigurationManager.AppSettings["web-bd-connection-string"],
                HostBdConnectionString = ConfigurationManager.AppSettings["host-bd-connection-string"],
                MainSchema = ConfigurationManager.AppSettings["main-schema"],
                ReportSavePath = ConfigurationManager.AppSettings["report-save-path"],
                UseFtp = ConfigurationManager.AppSettings["use-ftp"].ToBool(),
                FtpAddress = ConfigurationManager.AppSettings["ftp-adress"],
                FtpUserName = ConfigurationManager.AppSettings["ftp-user-name"],
                FtpUserPassword = ConfigurationManager.AppSettings["ftp-user-password"],
                FtpUseProxy = ConfigurationManager.AppSettings["ftp-use-proxy"].ToBool(),
                FtpProxyAddress = ConfigurationManager.AppSettings["ftp-proxy-address"],
                FtpProxyUserName = ConfigurationManager.AppSettings["ftp-proxy-user"],
                FtpProxyUserPassword = ConfigurationManager.AppSettings["ftp-proxy-password"],
                FtpProxyDomain = ConfigurationManager.AppSettings["ftp-proxy-domain"]
            };
        }
    }
}