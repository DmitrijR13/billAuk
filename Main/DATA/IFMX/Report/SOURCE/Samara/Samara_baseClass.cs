using System;
using System.Data;
using STCLINE.KP50.DataBase;

namespace STCLINE.KP50.IFMX.Report.SOURCE.Samara
{
    public class SamaraBaseReportClass
    {
        public readonly IDbConnection ConnDb;
        public string TestSql;

        public string AdrTempTable { get; set; }


        public void RunSql(string sql, bool inlog)
        {
            if (ConnDb != null)
                DBManager.ExecSQL(ConnDb, sql, inlog);
            else if (inlog)
            {
                TestSql += sql + "; " + Environment.NewLine;
            }
        }
  

        public SamaraBaseReportClass(IDbConnection connDb, string adrTempTable)
        {
            ConnDb = connDb;
            AdrTempTable = adrTempTable;
        }

    
    }

}
