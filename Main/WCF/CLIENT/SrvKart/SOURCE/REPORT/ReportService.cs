using Bars.KP50.Report;

namespace STCLINE.KP50.SrvKart.SOURCE.REPORT
{
    
    using STCLINE.KP50.Interfaces;
    using Globals.SOURCE.INTF.Report;
    using Server;
    using STCLINE.KP50.Global;

    public class ReportService: srv_Base, IReportService
    {
        public ReturnsObjectType<ReportResult> PrintReport(string reportId, string userParams, string userFilters, int curReportKind, string userName, int nzpObject)
        {
            var reportSerice = new ReportServiceHelper();

            return reportSerice.PrintReport(reportId, userParams, userFilters, curReportKind, userName, nzpObject);
        }
    }
}
