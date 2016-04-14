namespace Bars.KP50.Report
{
    using System.Collections.Generic;

    /// <summary>Параметр Формат печати</summary>
    public class ExportFormatParameter : ComboBoxParameter
    {
        public ExportFormatParameter()
        {
            TypeValue = typeof(string);
            Name = "Формат печати";
            Code = "ExportFormat";
            Require = true;
            Value = ExportFormat.Excel2007.ToString();

            StoreData = new List<object>
            {
                new { Id = "Pdf", Name = "Pdf" },
                new { Id = "Excel2007", Name = "Excel" },
                new { Id = "Html", Name = "Html" },
                new { Id = "Jpg", Name = "Jpg" },
                new { Id = "Gif", Name = "Gif" },
                new { Id = "Png", Name = "Png" },
                new { Id = "Txt", Name = "Txt" },
                new { Id = "Csv", Name = "Csv" }, 
                new { Id = "Dbf", Name = "Dbf" } 

            };
        }
    }
}