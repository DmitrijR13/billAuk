using Bars.KP50.Report;

namespace Bars.Report.Soft
{
    public class ReportInstaller : BaseReportsInstaller
    {
        protected override void RegisterReports()
        {
            Register<Processor>();
        }
    }
}
