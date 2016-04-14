namespace Bars.KP50.Report
{
    using System.Collections.Generic;

    using Newtonsoft.Json.Linq;

    /// <summary>Управляющие компании</summary>
    public class PrincipalParameter : ComboBoxParameter
    {
        public PrincipalParameter(bool multiSelect = true)
            : base(multiSelect)
        {
            TypeValue = typeof(List<int>);
            Name = "Принципалы";
            Code = "Principal";
            Require = false;
            ComboBoxConfig = new
            {
                valueField = "Id",
                displayField = "Name",
                editable = false,
                mode = "remote",
                triggerAction = "all",
                store = new JRaw("new Ext.data.JsonStore({ url: 'PrincipalHandler.axd', idProperty: 'Id', fields: ['Id', 'Name'], autoLoad: true, root: 'data' })")
            };
        }

        public string Pref { get; private set; }

        public virtual PrincipalParameter SetPref(string pref)
        {
            Pref = pref;
            ComboBoxConfig = new
            {
                valueField = "Id",
                displayField = "Name",
                editable = false,
                mode = "remote",
                triggerAction = "all",
                store = new JRaw("new Ext.data.JsonStore({ url: 'PrincipalHandler.axd?pref=" + pref + "', idProperty: 'Id', fields: ['Id', 'Name'], autoLoad: true, root: 'data' })")
            };

            return this;
        }
    }
}