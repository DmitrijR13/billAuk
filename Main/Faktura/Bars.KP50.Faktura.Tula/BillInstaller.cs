
using Bars.KP50.Faktura.Source;
using Bars.KP50.Faktura.Source.FAKTURA;

namespace Bars.KP50.Faktura.Kaluga
{
    
    public class BillInstaller : BaseBillInstaller
    {
        protected override void RegisterBills()
        {
            Register<KalugaFaktura2>("1001");


        }
    }
}