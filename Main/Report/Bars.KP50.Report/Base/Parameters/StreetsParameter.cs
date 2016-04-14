namespace Bars.KP50.Report
{
    using System.Collections.Generic;

    using Newtonsoft.Json.Linq;

    /// <summary>Улицы</summary>
    public class StreetsParameter : ComboBoxParameter
    {
        public StreetsParameter(bool multiSelect = true)
            : base(multiSelect)
        {
            TypeValue = typeof(List<int>);
            Name = "Улицы";
            Code = "Streets";
            Require = false;
            ComboBoxConfig = new
            {
                valueField = "Id",
                displayField = "Name",
                editable = false,
                mode = "remote",
                triggerAction = "all",
                store = new JRaw("new Ext.data.JsonStore({ url: 'StreetsHandler.axd', idProperty: 'Id', fields: ['Id', 'Name'], autoLoad: true, root: 'data' })")
            };
        }

        public string Pref { get; private set; }

        public virtual StreetsParameter SetPref(string pref)
        {
            Pref = pref;
            ComboBoxConfig = new
            {
                valueField = "Id",
                displayField = "Name",
                editable = false,
                mode = "remote",
                triggerAction = "all",
                store = new JRaw("new Ext.data.JsonStore({ url: 'StreetsHandler.axd?pref=" + pref + "', idProperty: 'Id', fields: ['Id', 'Name'], autoLoad: true, root: 'data' })")
            };

            return this;
        } 
    }
}