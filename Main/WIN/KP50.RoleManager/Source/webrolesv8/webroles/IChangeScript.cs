using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace webroles
{
    public interface IChangeScript
    {
        void GenerateUpdateStateMent(DataTable dt);
    }
}
