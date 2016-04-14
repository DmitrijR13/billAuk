namespace Bars.KP50.Report
{
    using Newtonsoft.Json.Linq;
    /// <summary>
    /// Параметр день
    /// </summary>
    public class DayParameter : ComboBoxParameter
    {
        public DayParameter()
        {
            TypeValue = typeof(int);
            Name = "День";
            Code = "Day";
            Require = true;
            ComboBoxConfig = new
            {
                valueField = "Id",
                displayField = "Name",
                editable = false,
                mode = "remote",
                triggerAction = "all",
                store = new JRaw("new Ext.data.JsonStore({ url: 'DaysHandler.axd', idProperty: 'Id', fields: ['Id', 'Name'], autoLoad: true, root: 'data' })")
            };
        }
    }
}
