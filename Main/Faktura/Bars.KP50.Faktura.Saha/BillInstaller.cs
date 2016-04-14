
using Bars.KP50.Faktura.Source;

namespace Bars.KP50.Faktura.Saha
{
    public class BillInstaller : BaseBillInstaller
    {
        protected override void RegisterBills()
        {
            Register<FakturaYakutsk>("1003");
            Register<FakturaSaha>("1004");
        }
    }
}
