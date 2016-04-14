using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace webroles.TransferData
{
    struct Returns
    {
        public bool Result;
        public string SqlError;

        public Returns(bool result, string error)
        {
            Result = result;
            SqlError = error;
        }
    }
}
