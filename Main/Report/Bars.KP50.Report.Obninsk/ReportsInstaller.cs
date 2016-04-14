namespace Bars.KP50.Report.Obninsk
{
    public class ReportsInstaller : BaseReportsInstaller
    {
        protected override void RegisterReports()
        {
            Register<Reports.Report40GenDomParams>();
        }
    }
}