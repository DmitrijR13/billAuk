namespace Bars.KP50.Report
{
    using System.Collections.Generic;

    using Newtonsoft.Json.Linq;

    /// <summary>Управляющие компании</summary>
    public class FileNameParameter : ComboBoxParameter
    {
        public FileNameParameter(bool multiSelect = false)
            : base(multiSelect)
        {
            TypeValue = typeof(List<int>);
            Name = "Загруженные файлы";
            Code = "FileName";
            Require = false;
            ComboBoxConfig = new
            {
                valueField = "Id",
                displayField = "Name",
                editable = false,
                mode = "remote",
                triggerAction = "all",
                store = new JRaw("new Ext.data.JsonStore({ url: 'FileHandler.axd', idProperty: 'Id', fields: ['Id', 'Name'], autoLoad: true, root: 'data' })")
            };
        }

        public string Pref { get; private set; }

        public virtual FileNameParameter SetPref(string pref)
        {
            Pref = pref;
            ComboBoxConfig = new
            {
                valueField = "Id",
                displayField = "Name",
                editable = false,
                mode = "remote",
                triggerAction = "all",
                store = new JRaw("new Ext.data.JsonStore({ url: 'FileHandler.axd?pref=" + pref + "', idProperty: 'Id', fields: ['Id', 'Name'], autoLoad: true, root: 'data' })")
            };

            return this;
        }
    }
}