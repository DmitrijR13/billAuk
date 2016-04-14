using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace webroles
{
    public class WatchGeneratedRows
    {
        public string TableName { get; private set; }
        public string ScriptRow { get; private set; }
       public int IdColumnValue { get; private set; }
       public string Comment { get; private set; }

        public WatchGeneratedRows(string tableName, string scriptRow, int idColumnValue,  string comment)
        {
            TableName = tableName;
            ScriptRow = scriptRow;
            IdColumnValue = idColumnValue;
            Comment = comment;
        }


    }
}
