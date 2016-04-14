
using Castle.Windsor;
using Globals.SOURCE.Config;
using Globals.SOURCE.Container;

namespace Bars.KP50.Report.Main
{
    
    public class ReportsInstaller : BaseReportsInstaller
    {
        protected override void RegisterReports()
        {
            Register<Reports.Contragents>();
            Register<Reports.Report53>();
            Register<Reports.SaldoRepLS>();
            Register<Reports.SaldoReport>();
            Register<Reports.SaldoReportRajon>();
            Register<Reports.GenNach2>();
            Register<Reports.GenNach>();
            Register<Reports.GenBigIPU>();
            Register<Reports.Report515>();
            Register<Reports.Report611>();
            Register<Reports.Unload>();
            Register<Reports.ReportChanges>();
            Register<Reports.IncomeRevise>();
            Register<Reports.ReportPU004016>();
            Register<Reports.ReportPU004018>();
            Register<Reports.Report7135>();
            Register<Reports.ProverkaKorektZnachParam>();
            Register<Reports.Report_CheckPayments>();
        }
    }
}