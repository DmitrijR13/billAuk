namespace Bars.KP50.Report
{
    using System.Collections.Generic;

    using Newtonsoft.Json.Linq;

    /// <summary>Список услуг(недопоставки)</summary>
    public class ServiceParameter : ComboBoxParameter
    {
        public ServiceParameter(bool withStatus = false, bool multiSelect = true)
            : base(multiSelect)
        {
            TypeValue = typeof(List<int>);
            Name = "Услуги";
            Code = "Services";
            Require = false;
            if (withStatus) JavascriptClassName = "Bars.KP50.report.ServiceField";
            ComboBoxConfig = new
            {
                valueField = "Id",
                displayField = "Name",
                editable = false,
                mode = "remote",
                triggerAction = "all",
                store = new JRaw("new Ext.data.JsonStore({ url: 'ServiceHandler.axd', idProperty: 'Id', fields: ['Id', 'Name'], autoLoad: true, root: 'data' })")
            };
            if (withStatus)
            {
                StatusConfig = new
                {
                    valueField = "Id",
                    displayField = "Name",
                    editable = false,
                    mode = "local",
                    triggerAction = "all",
                    store =
                        new JRaw("new Ext.data.JsonStore({ idProperty: 'Id', fields: ['Id', 'Name'], autoLoad: false })")
                };
                StoreData = new List<object>
                {
                    new {Id = 0, Name = "Оказываются"},
                    new {Id = 1, Name = "Не оказываются"}
                };
                Value = 0;
            }
        }

        public string Pref { get; private set; }

        public object StatusConfig { get; private set; }

        public virtual ServiceParameter SetPref(string pref)
        {
            Pref = pref;
            ComboBoxConfig = new
            {
                valueField = "Id",
                displayField = "Name",
                editable = false,
                mode = "remote",
                triggerAction = "all",
                store = new JRaw("new Ext.data.JsonStore({ url: 'ServiceHandler.axd?pref=" + pref + "', idProperty: 'Id', fields: ['Id', 'Name'], autoLoad: true, root: 'data' })")
            };

            return this;
        }
    }
}