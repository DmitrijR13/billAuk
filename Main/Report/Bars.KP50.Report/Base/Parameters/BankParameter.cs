using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;

namespace Bars.KP50.Report.Base.Parameters
{
    public class BankParameter : ComboBoxParameter
    {
        public BankParameter(bool multiSelect = true)
            : base(multiSelect)
        {
            TypeValue = typeof(int);
            Name = "Территория";
            Code = "Banks";
            Require = false;
            ComboBoxConfig = new
            {
                valueField = "Id",
                displayField = "Name",
                editable = false,
                mode = "local",
                triggerAction = "all",
                store = new JRaw("new Ext.data.JsonStore({ url: 'BankHandler.axd', idProperty: 'Id', fields: ['Id', 'Name'], autoLoad: true, root: 'data' })")
            };
        }
    }
}
