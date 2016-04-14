using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using STCLINE.KP50.Interfaces;
using STCLINE.KP50.Global;

namespace Bars.KP50.DB.Exchange.Unload
{
    public class IndividualSybsidyRegister : BaseUnload20
    {
        public override string Name
        {
            get { return "IndividualSybsidyRegister"; }
        }

        public override string NameText
        {
            get { return "Реестр начисленных сумм субсидий и льгот по жильцам"; }
        }

        public override int Code
        {
            get { return 25; }
        }

        public override List<FieldsUnload> Data { get; set; }

        public override void Start()
        {

        }

        public override void Start(string pref)
        {
        }

        public override void StartSelect()
        {
        }

        public override void CreateTempTable()
        { }

        public override void DropTempTable()
        {
        }
    }
}
