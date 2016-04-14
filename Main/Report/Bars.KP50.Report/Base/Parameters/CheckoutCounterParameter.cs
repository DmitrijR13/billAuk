namespace Bars.KP50.Report
{
    using Newtonsoft.Json.Linq;

    /// <summary>Параметр касса из ПС Финансы</summary>
    public class CheckoutCounterParameter : ComboBoxParameter
    {
        public CheckoutCounterParameter()
        {
            TypeValue = typeof(int);
            Name = "Касса";
            Code = "CheckoutCounter";
            Require = false;
            //JavascriptClassName = "Bars.KP50.report.FormingPlaceField";
            ComboBoxConfig = new
            {
                valueField = "Id",
                displayField = "Name",
                editable = false,
                mode = "local",
                triggerAction = "all",
                store = new JRaw("new Ext.data.JsonStore({ url: 'CheckoutCounterHandler.axd', idProperty: 'Id', fields: ['Id', 'Name'], autoLoad: true, root: 'data' })")
            };
        }
    }
}
