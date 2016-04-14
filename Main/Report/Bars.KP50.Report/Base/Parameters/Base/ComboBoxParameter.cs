namespace Bars.KP50.Report
{
    using System.Collections;

    using Newtonsoft.Json.Linq;

    /// <summary>Параметр выбор</summary>
    public class ComboBoxParameter : UserParam
    {
        public ComboBoxParameter() : this(false)
        {
        }

        public ComboBoxParameter(bool multiSelect)
        {
            MultiSelect = multiSelect;
            TypeValue = typeof(int);
            JavascriptClassName = MultiSelect ? "Bars.KP50.report.MultiSelectField" : "Bars.KP50.report.ComboBox";
            ComboBoxConfig = new
            {
                valueField = "Id",
                displayField = "Name",
                editable = false,
                mode = "local",
                triggerAction = "all",
                store = new JRaw("new Ext.data.JsonStore({ idProperty: 'Id', fields: ['Id', 'Name'], autoLoad: false })")
            };
        }

        public bool MultiSelect { get; set; }

        public object ComboBoxConfig { get; set; }

        public IEnumerable StoreData { get; set; }
    }
}