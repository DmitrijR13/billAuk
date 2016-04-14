using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bars.KP50.Faktura.Source;

namespace Bars.KP50.Faktura.MariEl
{
    public class BillInstaller : BaseBillInstaller
    {
        protected override void RegisterBills()
        {
            Register<FakturaYoshkarOla>("1002");
        }
    }
}
