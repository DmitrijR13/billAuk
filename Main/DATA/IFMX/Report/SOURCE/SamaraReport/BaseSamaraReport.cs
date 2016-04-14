using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace STCLINE.KP50.IFMX.Report.SOURCE.SamaraReport
{
    public class BaseSamaraReport
    {
        protected readonly ExcelNewLoader ExcelL;
        protected readonly IDbConnection _connDb;

        protected string _fileName;
        protected readonly int _month;
        protected readonly int _year;

        public BaseSamaraReport(IDbConnection connDb, ExcelNewLoader excelL, int month, int year)
        {
            ExcelL = excelL;
            _connDb = connDb;
            _month = month;
            _year = year;
        }
    }
}
