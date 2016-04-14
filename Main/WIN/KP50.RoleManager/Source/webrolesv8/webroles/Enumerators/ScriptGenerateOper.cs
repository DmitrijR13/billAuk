using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace webroles
{
    public enum ScriptGenerateOper
    {
        none,
        Insert = 1,
        Update = 2,
        Delete = 3,
        InsertWhole = 4,
        DeleteWhole = 5
    }
}
