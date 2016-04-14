using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using STCLINE.KP50.Global;
using FastReport;

using STCLINE.KP50.Interfaces;

namespace STCLINE.KP50.DataBase
{
    public partial class ExcelRep : ExcelRepClient
    {
        public DataTable GetSpravSoderg9(Prm prm, out Returns ret, string Nzp_user)
        {
            ret = Utils.InitReturns();
            return GetSpravSodergOdn9(prm, out ret, Nzp_user);
        }
    }
}
