using Bars.KP50.Report.Samara.Reports;

namespace Bars.KP50.Report.Samara
{
    public class ReportsInstaller : BaseReportsInstaller
    {
        protected override void RegisterReports()
        {
            //Register<Agent_rasch>();
            //Register<Report3014>();
            //Register<Report3001003>();
            
            //Register<Report30110>(); 
            //Register<Report30114>();
            Register<Report3015>();
            //Непроверенные
            //Register<Report30110>();
            //Register<Report30111>();Register<Report30110>();
            //Register<Report30112>();
            //Register<Report30113>();
            //Register<Vipis_counters>();
            //Register<Sprav_nach_heat>();
            //Register<Turn_off_gku>();
            //Register<Sprav_nach_hot_water>();
            //Register<Report3016>();
            //Register<Report3001011>();
            //Register<Report3001015>();
            //Register<Report3001016>();
            //Register<Report3001017>();
            //Register<Report3001018>();
            //Register<Report3022>();
            //Register<Report3011>();
            //Register<Report3012>();
            //Register<Report3015>();
            //Register<Report3023>();
            //Register<Report2>();
            Register<ReportJudgment>();
            Register<ReportPeni>();
            Register<ReportPeniCheck>();
            //Register<ReportSupplier>();
            Register<ReportGenerator>();
            Register<ReportProtCalc>();
            //Register<ReportPack>();
            //Register<ReportPackMore>();
            //Register<ReportLsCharge>();
            //Register<ReportCounters>();
            //Register<ReportPackMoreTotal>();
            //Register<ReportChargePrih>();
            Register<Report300207>();
            Register<ReportSaldo>();
        }
    }
}