using System;
using System.Data;
using System.Linq;
using System.Xml.Linq;
using System.IO;
using System.Text;
using System.Collections.Generic;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;

namespace STCLINE.KP50.DataBase
{
    //----------------------------------------------------------------------
    public partial class DbOdnClient : DataBaseHead
    //----------------------------------------------------------------------
    {
        /// <summary> Сформировать условие отбора записей для поиска ЛС, домов и т.д.
        /// </summary>
        public string MakeWhereString(OdnFinder finder, out Returns ret, enDopFindType tip)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();

            StringBuilder sql = new StringBuilder();
            string whereString = "";

            if (Points.IsSmr || Points.Is50)
            {
                //поиск по counters_xx
                whereString = WhereStringForFindDom_CountersXX(finder);
                if (whereString == "") return "";
            }
            else
            {
                //поиск по counters_correct
                string aliasA = "odn";
                string aliasB = "odn2";

                WhereStringForFindDom(finder, aliasA, ref whereString);
                if (whereString == "") return "";
#if PG
                sql.Append(" Select count(*) From PREFX_data.counters_correct " + aliasA);
                sql.Append(" Where " + aliasA + ".dat_charge = (select max(" + aliasB + ".dat_charge) from PREFX_data.counters_correct " + aliasB +
                            " where " + aliasA + ".dat_month = " + aliasB + ".dat_month and " + aliasA + ".nzp_dom = " + aliasB + ".nzp_dom and " + aliasA + ".nzp_serv = " + aliasB + ".nzp_serv) ");
#else
sql.Append(" Select count(*) From PREFX_data:counters_correct " + aliasA);
                sql.Append(" Where " + aliasA + ".dat_charge = (select max(" + aliasB + ".dat_charge) from PREFX_data:counters_correct " + aliasB +
                            " where " + aliasA + ".dat_month = " + aliasB + ".dat_month and " + aliasA + ".nzp_dom = " + aliasB + ".nzp_dom and " + aliasA + ".nzp_serv = " + aliasB + ".nzp_serv) ");
#endif
                sql.Append(whereString);
                sql.Append(" and " + aliasA + ".nzp_dom = k.nzp_dom ");

                whereString = sql.ToString();

            }

            switch (tip)
            {
                case enDopFindType.dft_CntDom: return whereString;
            }

            return "";
        }

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        string WhereStringForFindDom_CountersXX(OdnFinder finder)
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        {
            StringBuilder swhere = new StringBuilder();
            int yy = Points.CalcMonth.year_;
            int mm = Points.CalcMonth.month_;

            finder.dat_month = Utils.FormatDate(finder.dat_month);
            if (finder.dat_month != "")
            {
                DateTime d = new DateTime();
                if (DateTime.TryParse(finder.dat_month, out d))
                {
                    yy = d.Year;
                    mm = d.Month;
                }
            }

#if PG
            string counters_xx = "PREFX_charge_" + (yy - 2000).ToString("00") + ".counters_" + mm.ToString("00");
            swhere.Append(" Select count(*) From " + counters_xx + " cntx ");
            swhere.Append(" Where cntx.nzp_dom = d.nzp_dom and cntx.nzp_type = 1 and cntx.stek = 3 and cntx.nzp_kvar = 0 and cntx.dat_charge is null ");
#else
string counters_xx = "PREFX_charge_" + (yy - 2000).ToString("00") + ":counters_" + mm.ToString("00");
            swhere.Append(" Select count(*) From " + counters_xx + " cntx ");
            swhere.Append(" Where cntx.nzp_dom = d.nzp_dom and cntx.nzp_type = 1 and cntx.stek = 3 and cntx.nzp_kvar = 0 and cntx.dat_charge is null ");
#endif
            if (finder.RolesVal != null)
            {
                if (finder.RolesVal.Count > 0)
                {
                    foreach (_RolesVal role in finder.RolesVal)
                    {
                        if (role.tip == Constants.role_sql && role.kod == Constants.role_sql_serv)
                            swhere.Append(" and cntx.nzp_serv in (" + role.val + ") ");
                    }
                }
            }

            if (finder.nzp_serv > 0)
                swhere.Append(" and cntx.nzp_serv = " + finder.nzp_serv.ToString());


            finder.rval = Utils.EFlo0(finder.rval, "");
            finder.rval_po = Utils.EFlo0(finder.rval_po, "");
            if ((finder.rval != "") && (finder.rval_po == "" || finder.rval == finder.rval_po))
                swhere.Append(" and abs(cntx.kf307 - " + finder.rval + ") < 0.0001 ");
            else
            {
                if (finder.rval != "")
                    swhere.Append(" and cntx.kf307 >= " + finder.rval);
                if (finder.rval_po != "")
                    swhere.Append(" and cntx.kf307 <= " + finder.rval_po);
            }

            //коэф-т по площади
            /*
            finder.rval_real = Utils.EFlo0(finder.rval_real, "");
            finder.rval_real_po = Utils.EFlo0(finder.rval_real_po, "");
            if ((finder.rval_real != "") && (finder.rval_real_po == "" || finder.rval_real == finder.rval_real_po)) 
                swhere.Append(" and cntx.rval_real = " + finder.rval_real);
            else
            {
                if (finder.rval_real != "") 
                    swhere.Append(" and cntx.rval_real >= " + finder.rval_real);
                if (finder.rval_real_po != "") 
                    swhere.Append(" and cntx.rval_real <= " + finder.rval_real_po);
            }
            */

            finder.rvaldlt = Utils.EFlo0(finder.rvaldlt, "");
            finder.rvaldlt_po = Utils.EFlo0(finder.rvaldlt_po, "");
            if ((finder.rvaldlt != "") && (finder.rvaldlt_po == "" || finder.rvaldlt == finder.rvaldlt_po))
                swhere.Append(" and abs(cntx.val1 - " + finder.rvaldlt + ") < 0.0001 ");
            else
            {
                if (finder.rvaldlt != "")
                    swhere.Append(" and cntx.val1 >= " + finder.rvaldlt);
                if (finder.rvaldlt_po != "")
                    swhere.Append(" and cntx.val1 <= " + finder.rvaldlt_po);
            }

            finder.sum_ls_val = Utils.EFlo0(finder.sum_ls_val, "");
            finder.sum_ls_val_po = Utils.EFlo0(finder.sum_ls_val_po, "");
            if ((finder.sum_ls_val != "") && (finder.sum_ls_val_po == "" || finder.sum_ls_val == finder.sum_ls_val_po))
                swhere.Append(" and abs(cntx.val2 - " + finder.sum_ls_val + ") < 0.00001 ");
            else
            {
                if (finder.sum_ls_val != "")
                    swhere.Append(" and cntx.val2 >= " + finder.sum_ls_val);
                if (finder.sum_ls_val_po != "")
                    swhere.Append(" and cntx.val2 <= " + finder.sum_ls_val_po);
            }

            finder.sum_ls_norm = Utils.EFlo0(finder.sum_ls_norm, "");
            finder.sum_ls_norm_po = Utils.EFlo0(finder.sum_ls_norm_po, "");
            if ((finder.sum_ls_norm != "") && (finder.sum_ls_norm_po == "" || finder.sum_ls_norm == finder.sum_ls_norm_po))
                swhere.Append(" and abs(cntxt.val3 - " + finder.sum_ls_norm + ") < 0.0001 ");
            else
            {
                if (finder.sum_ls_norm != "")
                    swhere.Append(" and cntx.val3 >= " + finder.sum_ls_norm);
                if (finder.sum_ls_norm_po != "")
                    swhere.Append(" and cntx.val3 <= " + finder.sum_ls_norm_po);
            }


            if (finder.nzp_type_alg > 0)
                swhere.Append(" and cntx.cnt5 = " + finder.nzp_type_alg.ToString());

            return swhere.ToString();
        }
        /// <summary> Сформировать условие отбора данных
        /// </summary>
        void WhereStringForFindDom(OdnFinder finder, string alias, ref string whereString)
        {
            StringBuilder swhere = new StringBuilder();

            WhereStringForFind(finder, alias, ref whereString);

            //if (finder.get_koss) swhere.Append(" and " + alias + ".nzp_serv in (25,210,11,242) ");
            if (finder.RolesVal != null)
            {
                if (finder.RolesVal.Count > 0)
                {
                    foreach (_RolesVal role in finder.RolesVal)
                    {
                        if (role.tip == Constants.role_sql && role.kod == Constants.role_sql_serv)
                            swhere.Append(" and " + alias + ".nzp_serv in (" + role.val + ") ");
                    }
                }
            }

            if (finder.nzp_serv > 0) swhere.Append(" and " + alias + ".nzp_serv = " + finder.nzp_serv.ToString());
            if (finder.pref != "") swhere.Append(" and a.pref = '" + finder.pref.ToString() + "'");

            finder.dat_month = Utils.FormatDate(finder.dat_month);
            finder.dat_month_po = Utils.FormatDate(finder.dat_month_po);
            if ((finder.dat_month != "") && (finder.dat_month_po == "" || finder.dat_month == finder.dat_month_po)) swhere.Append(" and " + alias + ".dat_month = '" + finder.dat_month + "'");
            else
            {
                if (finder.dat_month != "") swhere.Append(" and " + alias + ".dat_month >= '" + finder.dat_month + "'");
                if (finder.dat_month_po != "") swhere.Append(" and " + alias + ".dat_month <= '" + finder.dat_month_po + "'");
            }

            finder.rval = Utils.EFlo0(finder.rval, "");
            finder.rval_po = Utils.EFlo0(finder.rval_po, "");
            if ((finder.rval != "") && (finder.rval_po == "" || finder.rval == finder.rval_po)) swhere.Append(" and " + alias + ".rval = " + finder.rval);
            else
            {
                if (finder.rval != "") swhere.Append(" and " + alias + ".rval >= " + finder.rval);
                if (finder.rval_po != "") swhere.Append(" and " + alias + ".rval <= " + finder.rval_po);
            }

            finder.rval_real = Utils.EFlo0(finder.rval_real, "");
            finder.rval_real_po = Utils.EFlo0(finder.rval_real_po, "");
            if ((finder.rval_real != "") && (finder.rval_real_po == "" || finder.rval_real == finder.rval_real_po)) swhere.Append(" and " + alias + ".rval_real = " + finder.rval_real);
            else
            {
                if (finder.rval_real != "") swhere.Append(" and " + alias + ".rval_real >= " + finder.rval_real);
                if (finder.rval_real_po != "") swhere.Append(" and " + alias + ".rval_real <= " + finder.rval_real_po);
            }

            finder.rvaldlt = Utils.EFlo0(finder.rvaldlt, "");
            finder.rvaldlt_po = Utils.EFlo0(finder.rvaldlt_po, "");
            if ((finder.rvaldlt != "") && (finder.rvaldlt_po == "" || finder.rvaldlt == finder.rvaldlt_po)) swhere.Append(" and " + alias + ".rvaldlt = " + finder.rvaldlt);
            else
            {
                if (finder.rvaldlt != "") swhere.Append(" and " + alias + ".rvaldlt >= " + finder.rvaldlt);
                if (finder.rvaldlt_po != "") swhere.Append(" and " + alias + ".rvaldlt <= " + finder.rvaldlt_po);
            }

            finder.sum_ls_norm = Utils.EFlo0(finder.sum_ls_norm, "");
            finder.sum_ls_norm_po = Utils.EFlo0(finder.sum_ls_norm_po, "");
            if ((finder.sum_ls_norm != "") && (finder.sum_ls_norm_po == "" || finder.sum_ls_norm == finder.sum_ls_norm_po)) swhere.Append(" and " + alias + ".sum_ls_norm = " + finder.sum_ls_norm);
            else
            {
                if (finder.sum_ls_norm != "") swhere.Append(" and " + alias + ".sum_ls_norm >= " + finder.sum_ls_norm);
                if (finder.sum_ls_norm_po != "") swhere.Append(" and " + alias + ".sum_ls_norm <= " + finder.sum_ls_norm_po);
            }

            finder.sum_ls_val = Utils.EFlo0(finder.sum_ls_val, "");
            finder.sum_ls_val_po = Utils.EFlo0(finder.sum_ls_val_po, "");
            if ((finder.sum_ls_val != "") && (finder.sum_ls_val_po == "" || finder.sum_ls_val == finder.sum_ls_val_po)) swhere.Append(" and " + alias + ".sum_ls_val = " + finder.sum_ls_val);
            else
            {
                if (finder.sum_ls_val != "") swhere.Append(" and " + alias + ".sum_ls_val >= " + finder.sum_ls_val);
                if (finder.sum_ls_val_po != "") swhere.Append(" and " + alias + ".sum_ls_val <= " + finder.sum_ls_val_po);
            }

            if (finder.nzp_type_alg > 0) swhere.Append(" and " + alias + ".nzp_type_alg = " + finder.nzp_type_alg.ToString());

            whereString += swhere.ToString();
        }


        /// <summary> Сформировать условие отбора данных
        /// </summary>
        protected void WhereStringForFind(OdnFinder finder, string alias, ref string whereString)
        {
            StringBuilder swhere = new StringBuilder();
            if (finder.nzp_dom > 0) swhere.Append(" and " + alias + ".nzp_dom = " + finder.nzp_dom.ToString());
            whereString += swhere.ToString();
        }


    }

}

