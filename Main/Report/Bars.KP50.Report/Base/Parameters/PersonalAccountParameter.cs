namespace Bars.KP50.Report
{
    using Newtonsoft.Json.Linq;
    class PersonalAccountParameter : ComboBoxParameter
    {
        /// <summary>
        /// Параметр лицевого счета
        /// </summary>
        public PersonalAccountParameter()
        {
            TypeValue = typeof(int);
            Name = "Лицевой счет";
            Code = "Num_ls";
            Require = true;
            ComboBoxConfig = new
            {
                valueField = "Id",
                displayField = "Name",
                editable = false,
                mode = "remote",
                triggerAction = "all",
                store = new JRaw("new Ext.data.JsonStore({ url: 'PersonalAccountHandler.axd', idProperty: 'Id', fields: ['Id', 'Name'], autoLoad: true, root: 'data' })")
            };
        }
    }
}
