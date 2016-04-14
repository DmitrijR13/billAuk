namespace Bars.KP50.Report.Kapr
{
  
    
    public class ReportsInstaller : BaseReportsInstaller
    {
        protected override void RegisterReports()
        {
            Register<Kapr1>();
            Register<Kapr2>();           
        }
    }
     
}
