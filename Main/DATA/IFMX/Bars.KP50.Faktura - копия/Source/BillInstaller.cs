using Bars.KP50.DB.Faktura;
using Bars.KP50.Faktura.Source.Base;
using Bars.KP50.Faktura.Source.FAKTURA;

namespace Bars.KP50.Faktura.Source
{
    
    public class BillInstaller : BaseBillInstaller
    {
        protected override void RegisterBills()
        {

            Register<TulaNewFaktura>("10107");
            Register<InstalmentFaktura>("121");
            Register<KznUyutdLiftNewFaktura>("1101");

        }
    }
}