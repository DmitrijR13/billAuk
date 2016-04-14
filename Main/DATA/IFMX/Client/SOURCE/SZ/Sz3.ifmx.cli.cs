using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using STCLINE.KP50.Interfaces;
using STCLINE.KP50.Global;
using STCLINE.KP50.DataBase;
using System.Collections;
using System.Data;
using System.Globalization;

namespace STCLINE.KP50.DataBase
{
    //----------------------------------------------------------------------
    public partial class DbSzClient : DataBaseHead
    //----------------------------------------------------------------------
    {
        //----------------------------------------------------------------------
        public void FindSz(SzFinder finder, out Returns ret) //
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            if (finder.nzp_user < 1)
            {
                ret.result = false;
                ret.text = "Не определен пользователь";
                return;
            }


            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result) return;

            string tXX_sz = "t" + Convert.ToString(finder.nzp_user) + "_sz";

            if (TableInWebCashe(conn_web, tXX_sz))
            {
                ExecSQL(conn_web, " Drop table " + tXX_sz, false);
            }

            //создать таблицу webdata:tXX_sz
            ret = ExecSQL(conn_web,
                      " Create table " + tXX_sz +
                      " ( nzp_key  integer ) ", true);
            if (!ret.result)
            {
                return;
            }


            StringBuilder swhere = new StringBuilder();


            /*
                        list_mo = "";
                        list_raj = "";
                        list_uk = "";
                        list_uk_podr = "";
                        list_ul = "";
            */

            if (finder.list_mo != "") 
                swhere.Append(" and nzp_mo in (" + finder.list_mo + ")");
            if (finder.list_raj != "") 
                swhere.Append(" and nzp_raj in (" + finder.list_raj + ")");
            if (finder.list_uk != "") 
                swhere.Append(" and nzp_uk in (" + finder.list_uk + ")");
            if (finder.list_uk_podr != "") 
                swhere.Append(" and nzp_uk_podr in (" + finder.list_uk_podr + ")");
            if (finder.list_ul != "") 
                swhere.Append(" and nzp_ul2 in (" + finder.list_ul + ")");

            int i;

            if (finder.ndom_po != "")
            {
                i = Utils.GetInt(finder.ndom_po);
                if (i > 0)
                    swhere.Append(" and idom <= " + i.ToString());

                i = Utils.GetInt(finder.ndom);
                if (i > 0)
                    swhere.Append(" and idom >= " + i.ToString());
            }
            else
            {
                if (finder.ndom != "")
                {
                    swhere.Append(" and ndom = " + Utils.EStrNull(finder.ndom.ToUpper()));
                }
            }
            if (finder.nkvar_po != "")
            {
                i = Utils.GetInt(finder.nkvar_po);
                if (i > 0)
                    swhere.Append(" and ikvar <= " + i.ToString());

                i = Utils.GetInt(finder.nkvar);
                if (i > 0)
                    swhere.Append(" and ikvar >= " + i.ToString());
            }
            else
            {
                if (finder.nkvar != "")
                {
                    swhere.Append(" and nkvar = " + Utils.EStrNull(finder.nkvar));
                }
            }

            if (finder.pss != "")
            {
                swhere.Append(" and nzp_pretender = " + finder.pss);
            }
            if (finder.num_ls > 0 )
            {
                swhere.Append(" and num_ls = " + finder.num_ls);
            }

            string whereString = swhere.ToString();

            /*
            if (finder.RolesVal != null)
                if (finder.RolesVal.Count > 0)
                {
                    foreach (_RolesVal role in finder.RolesVal)
                    {
                        if (role.tip == Constants.role_sql & role.kod == Constants.role_sql_area)
                            whereString += " and nzp_area in (" + role.val + ")";
                    }
                }
            */

            string sql =
                " Insert into " + tXX_sz + " (nzp_key) " +
                " Select nzp_key From sz_lsdata " +
                " Where 1 = 1 " + whereString;

            //записать текст sql в лог-журнал
            int key = LogSQL(conn_web, finder.nzp_user, tXX_sz + ": " + whereString);

            ret = ExecSQL(conn_web, sql, true);
            if (!ret.result)
            {
                if (key > 0) LogSQL_Error(conn_web, key, ret.text);

                conn_web.Close();
                return;
            }

#if PG
            ret = ExecSQL(conn_web, " Create index ix1_" + tXX_sz + " on " + tXX_sz + " (nzp_key) ", true);
            if (ret.result) { ret = ExecSQL(conn_web, " analyze  " + tXX_sz, true); }
#else
  ret = ExecSQL(conn_web, " Create index ix1_" + tXX_sz + " on " + tXX_sz + " (nzp_key) ", true);
            if (ret.result) { ret = ExecSQL(conn_web, " Update statistics for table  " + tXX_sz, true); }
#endif
            conn_web.Close();

        }
    }
}
