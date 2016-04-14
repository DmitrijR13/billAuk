namespace Bars.KP50.Report
{
    using Newtonsoft.Json.Linq;

    /// <summary>Параметр места формирования из ПС Финансы</summary>
    public class FormingPlaceParameter : ComboBoxParameter
    {
        public FormingPlaceParameter()
        {
            TypeValue = typeof(int);
            Name = "Места формирования";
            Code = "Place";
            Require = false;
            //JavascriptClassName = "Bars.KP50.report.FormingPlaceField";
            ComboBoxConfig = new
            {
                valueField = "Id",
                displayField = "Name",
                editable = false,
                mode = "local",
                triggerAction = "all",
                store = new JRaw("new Ext.data.JsonStore({ url: 'FormingPlaceHandler.axd', idProperty: 'Id', fields: ['Id', 'Name'], autoLoad: true, root: 'data' })")
            };
        }
    }
}
