using System.Collections.Generic;

namespace Bars.KP50.Report
{
    using Newtonsoft.Json.Linq;

    /// <summary>Параметр параметры дома из prm2</summary>
    public class DomParamsParameter : ComboBoxParameter
    {
        public DomParamsParameter(bool multiSelect = true)
            : base(multiSelect)
        {    
            TypeValue = typeof(List<int>);
            Name = "Параметры";
            Code = "DomParams";
            Require = false;
            //JavascriptClassName = "Bars.KP50.report.FormingPlaceField";
            ComboBoxConfig = new
            {
                valueField = "Id",
                displayField = "Name",
                editable = false,
                mode = "local",
                triggerAction = "all",
                store = new JRaw("new Ext.data.JsonStore({ url: 'DomParamsHandler.axd', idProperty: 'Id', fields: ['Id', 'Name'], autoLoad: true, root: 'data' })")
            };
        }
    }
}
