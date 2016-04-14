using STCLINE.KP50.Interfaces;

namespace Bars.KP50.Report
{
    using Newtonsoft.Json.Linq;
    using System.Collections.Generic;

    /// <summary>Параметр месяц</summary>
    public class MonthParameter : ComboBoxParameter
    {
        public MonthParameter()
        {
            
            TypeValue = typeof(int);
            Name = "Месяц";
            Code = "Month";
            Require = true;
            ComboBoxConfig = new
            {
                valueField = "Id",
                displayField = "Name",
                editable = false,
                mode = "local",
                triggerAction = "all",
                store = new JRaw("new Ext.data.JsonStore({ idProperty: 'Id', fields: ['Id', 'Name'], autoLoad: true })")
                //store =new JRaw("new Ext.data.JsonStore({ url: 'MonthsHandler.axd', idProperty: 'Id', fields: ['Id', 'Name'], autoLoad: true, root: 'data' })")
               
            };
            StoreData = new List<object>
            {
                new {Id = 1, Name = "январь"},
                new {Id = 2, Name = "февраль"},
                new {Id = 3, Name = "март"},
                new {Id = 4, Name = "апрель"},
                new {Id = 5, Name = "май"},
                new {Id = 6, Name = "июнь"},
                new {Id = 7, Name = "июль"},
                new {Id = 8, Name = "август"},
                new {Id = 9, Name = "сентябрь"},
                new {Id = 10, Name = "октябрь"},
                new {Id = 11, Name = "ноябрь"},
                new {Id = 12, Name = "декабрь"}
            };
        }
    }
}