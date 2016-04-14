

namespace Bars.KP50.Report.Main
{
    
    public class ReportsInstaller : BaseReportsInstaller
    {
        protected override void RegisterReports()
        {
           
            Register<Reports.Report501>();
            Register<Reports.Report502>();
        
            
        }
    }
}