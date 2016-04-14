using Bars.KP50.Report.Astra.Reports;

namespace Bars.KP50.Report.Astra
{
    public class ReportsInstaller : BaseReportsInstaller
    {
        protected override void RegisterReports()
        {   
            Register<Report3121>();
            Register<Report3131>();
            Register<Report3141>();
        }
    }
}