

namespace Bars.KP50.Report.Gubkin
{
    
    public class ReportsInstaller : BaseReportsInstaller
    {
        protected override void RegisterReports()
        {
            
            Register<Reports.Statistics_Nachisl>();

        }
    }
}