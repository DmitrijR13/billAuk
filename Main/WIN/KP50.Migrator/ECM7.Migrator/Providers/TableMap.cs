using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KP50.DataBase.Migrator.Providers
{
    using DataBase = KP50.DataBase.Migrator.Framework.DataBase;

    public class TableMap
    {
        public string AssemblyKey { get; set; }

        public string Table { get; set; }

        public IList<long> Versions { get; set; }

        public TableMap(string key, string prefix)
        {
            AssemblyKey = key;
            Table = prefix;
            Versions = new List<long>();
        }
    }
}
