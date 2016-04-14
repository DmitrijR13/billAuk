using System;
using System.Data;
using System.Linq;
using System.Xml.Linq;
using System.IO;
using System.Text;
using System.Collections.Generic;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;
using System.Collections;

namespace STCLINE.KP50.DataBase
{
    public partial class DbNedopClient : DataBaseHead
    {
        public static string MakeWhereString(Nedop finder, out Returns ret)
        {
            DateTime ds = DateTime.MinValue, dpo = DateTime.MaxValue;
            if (finder.dat_s != "" && !DateTime.TryParse(finder.dat_s, out ds))
            {
                ret = new Returns(false, "Неправильно введена дата начала периода недопоставки", -1);
                return "0";
            }
            if (finder.dat_po != "" && !DateTime.TryParse(finder.dat_po, out dpo))
            {
                ret = new Returns(false, "Неправильно введена дата окончания периода недопоставки", -1);
                return "0";
            }

            ret = Utils.InitReturns();

            string sql = "";
            if (finder.nzp_serv > 0) sql += " and nd.nzp_serv = " + finder.nzp_serv;
            if (finder.nzp_supp > 0) sql += " and nd.nzp_supp = " + finder.nzp_supp;
            if (finder.nzp_kind > 0) sql += " and nd.nzp_kind = " + finder.nzp_kind;

            if (ds != DateTime.MinValue || dpo != DateTime.MaxValue)
            {
                if (ds == DateTime.MinValue) ds = dpo;
                if (dpo == DateTime.MaxValue) dpo = ds;
                dpo = dpo.AddDays(1);
                sql += " and nd.dat_s < " + Utils.EStrNull(dpo.ToString("yyyy-MM-dd HH:mm"));
                sql += " and nd.dat_po > " + Utils.EStrNull(ds.ToString("yyyy-MM-dd HH:mm"));
            }

            if (sql != "")
            {
#if PG
                sql = "select 1 From PREFX_data.nedop_kvar nd Where nd.nzp_kvar = k.nzp_kvar" + sql;
#else
                sql = "select 1 From PREFX_data:nedop_kvar nd Where nd.nzp_kvar = k.nzp_kvar" + sql;
#endif
            }

            return sql;
        }

    }
}
