namespace Bars.KP50.Report
{
    using Newtonsoft.Json.Linq;

    /// <summary>Параметр расчетного счета из ПС Финансы</summary>
    public class AccountParameter : ComboBoxParameter
    {
        public AccountParameter()
        {
            TypeValue = typeof(int);
            Name = "Расчетный счет";
            Code = "RS";
            Require = false;
            ComboBoxConfig = new
            {
                valueField = "Id",
                displayField = "Name",
                editable = false,
                mode = "local",
                triggerAction = "all",
                store = new JRaw("new Ext.data.JsonStore({ url: 'AccountHandler.axd', idProperty: 'Id', fields: ['Id', 'Name'], autoLoad: true, root: 'data' })")
            };
        }
    }
}
