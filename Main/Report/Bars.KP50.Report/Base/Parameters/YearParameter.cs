namespace Bars.KP50.Report
{
    using Newtonsoft.Json.Linq;
    using System;
    using System.Collections.Generic;

    /// <summary>Параметр год</summary>
    public class YearParameter : ComboBoxParameter
    {
        public YearParameter()
        {
            TypeValue = typeof(int);
            Name = "Год";
            Code = "Year";
            Require = true;
            ComboBoxConfig = new
            {
                valueField = "Id",
                displayField = "Name",
                editable = false,
                mode = "local",
                triggerAction = "all",
                store = new JRaw("new Ext.data.JsonStore({ idProperty: 'Id', fields: ['Id', 'Name'], autoLoad: true })")
             //   store = new JRaw("new Ext.data.JsonStore({ url: 'YearsHandler.axd', idProperty: 'Id', fields: ['Id', 'Name'], autoLoad: true, root: 'data' })")
            };
            var years = new List<object>();
            for (var year = 2006; year < (DateTime.Now.Year + 2); year++)
            {
                years.Add(new { Id = year, Name = year });
            }
            StoreData = years;

        }
    }
}