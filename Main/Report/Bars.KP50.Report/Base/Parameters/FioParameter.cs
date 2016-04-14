namespace Bars.KP50.Report
{
    using Newtonsoft.Json.Linq;
    using System;
    using System.Collections.Generic;

    /// <summary>Параметр ФИО жильцов из карты</summary>
    public class FioParameter : ComboBoxParameter
    {
        public FioParameter()
        {
            TypeValue = typeof(int);
            Name = "ФИО";
            Code = "FIO";
            Require = true;
            ComboBoxConfig = new
            {
                valueField = "Id",
                displayField = "Name",
                editable = false,
                mode = "local",
                triggerAction = "all",
                store = new JRaw("new Ext.data.JsonStore({ url: 'FioHandler.axd', idProperty: 'Id', fields: ['Id', 'Name'], autoLoad: true, root: 'data' })")
            };
        }
    }
}