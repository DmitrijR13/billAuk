namespace Bars.KP50.Report
{
    using System.Collections.Generic;

    using Newtonsoft.Json.Linq;

    /// <summary>Районы</summary>
    public class RaionsParameter : ComboBoxParameter
    {
        public RaionsParameter(bool multiSelect = true)
            : base(multiSelect)
        {
            TypeValue = typeof(List<int>);
            Name = "Районы";
            Code = "Raions";
            Require = false;
            ComboBoxConfig = new
            {
                valueField = "Id",
                displayField = "Name",
                editable = false,
                mode = "remote",
                triggerAction = "all",
                store = new JRaw("new Ext.data.JsonStore({ url: 'RaionsHandler.axd', idProperty: 'Id', fields: ['Id', 'Name'], autoLoad: true, root: 'data' })")
            };
        }

        public string Pref { get; private set; }

        public virtual RaionsParameter SetPref(string pref)
        {
            Pref = pref;
            ComboBoxConfig = new
            {
                valueField = "Id",
                displayField = "Name",
                editable = false,
                mode = "remote",
                triggerAction = "all",
                store = new JRaw("new Ext.data.JsonStore({ url: 'RaionsHandler.axd?pref=" + pref + "', idProperty: 'Id', fields: ['Id', 'Name'], autoLoad: true, root: 'data' })")
            };

            return this;
        } 
    }
}