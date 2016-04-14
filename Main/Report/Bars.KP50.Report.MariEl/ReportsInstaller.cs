namespace Bars.KP50.Report.MariEl

{
    public class ReportsInstaller : BaseReportsInstaller
    {
        protected override void RegisterReports()
        {
            Register<Reports.Report120101>();
            Register<Reports.Report120102>();
            Register<Reports.Report120103>();
            Register<Reports.Report1205020>();
            Register<Reports.Report120104>();
            Register<Reports.Report120105>();
            Register<Reports.Report120106>();
            Register<Reports.Report120107>();
            Register<Reports.Report12331>();
            Register<Reports.Report1203004>();
            Register<Reports.Report120111>();
            Register<Reports.Report120305>();
            Register<Reports.Report120308>();
            Register<Reports.Report120307>();
            Register<Reports.Report120110>();
            Register<Reports.Report120114>();
        }
    }
}