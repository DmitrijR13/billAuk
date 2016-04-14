using System;
using System.Data;
using System.Globalization;
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
    public class DbLsServicesClient : DataBaseHead
    //----------------------------------------------------------------------
    {
        public static string MakeWhereString(Service finder, out Returns ret)
        {
            DateTime ds = DateTime.MinValue, dpo = DateTime.MaxValue;
            if (finder.dat_s != "" && !DateTime.TryParse(finder.dat_s, out ds))
            {
                ret = new Returns(false, "Неправильно введена дата начала периода", -1);
                return "0";
            }
            if (finder.dat_po != "" && !DateTime.TryParse(finder.dat_po, out dpo))
            {
                ret = new Returns(false, "Неправильно введена дата окончания периода", -1);
                return "0";
            }

            ret = Utils.InitReturns();

            string sql = "";
            string where = " and t.is_actual <> 100";
            if (ds != DateTime.MinValue || dpo != DateTime.MaxValue)
            {
                if (ds == DateTime.MinValue) ds = dpo;
                else if (dpo == DateTime.MaxValue) dpo = ds;

                where += " and t.dat_s <= " + Utils.EStrNull(dpo.ToShortDateString());
                where += " and t.dat_po >= " + Utils.EStrNull(ds.ToShortDateString());
            }

            if (finder.nzp_serv > 0 || finder.nzp_supp > 0 || finder.nzp_frm > 0 || (finder.nzp_serv == 0 && finder.nzp_supp == 0 && finder.nzp_frm == 0))
            {
                if (finder.nzp_serv > 0) where += " and t.nzp_serv = " + finder.nzp_serv;
                else if (finder.nzp_serv < 0) where += " and t.nzp_serv <> " + -finder.nzp_serv;
                //if (finder.nzp_supp > 0) where += " and t.nzp_supp = " + finder.nzp_supp;
                //else if (finder.nzp_supp < 0) where += " and t.nzp_supp <> " + -finder.nzp_supp;
                if (finder.nzp_payer_agent > 0) 
                    where += " and t.nzp_supp in (select nzp_supp from  "+
                        "CNTRPRFX_kernel" + tableDelimiter + "supplier where nzp_payer_agent =" + finder.nzp_payer_agent + ")";
                else if (finder.nzp_payer_agent < 0) where += " and t.nzp_supp not in (select nzp_supp from  " +
                        "CNTRPRFX_kernel" + tableDelimiter + "supplier where nzp_payer_agent =" + -finder.nzp_payer_agent + ")";

                if (finder.nzp_payer_princip > 0)
                    where += " and t.nzp_supp in (select nzp_supp from  " +
                        "CNTRPRFX_kernel" + tableDelimiter + "supplier where nzp_payer_princip =" + finder.nzp_payer_princip + ")";
                else if (finder.nzp_payer_princip < 0) where += " and t.nzp_supp not in (select nzp_supp from  " +
                        "CNTRPRFX_kernel" + tableDelimiter + "supplier where nzp_payer_princip =" + -finder.nzp_payer_princip + ")";

                if (finder.nzp_payer_supp > 0)
                    where += " and t.nzp_supp in (select nzp_supp from  " +
                        "CNTRPRFX_kernel" + tableDelimiter + "supplier where nzp_payer_supp =" + finder.nzp_payer_supp + ")";
                else if (finder.nzp_payer_supp < 0) where += " and t.nzp_supp not in (select nzp_supp from  " +
                        "CNTRPRFX_kernel" + tableDelimiter + "supplier where nzp_payer_supp =" + -finder.nzp_payer_supp + ")";

                if (finder.nzp_frm > 0) where += " and t.nzp_frm = " + finder.nzp_frm;
                else if (finder.nzp_frm < 0) where += " and t.nzp_frm <> " + -finder.nzp_frm;
            }
            else
            {
                if (finder.nzp_serv < 0) where += " and t.nzp_serv = " + -finder.nzp_serv;
              //  if (finder.nzp_supp < 0) where += " and t.nzp_supp = " + -finder.nzp_supp;
                if (finder.nzp_frm < 0) where += " and t.nzp_frm = " + -finder.nzp_frm;

                if (finder.nzp_payer_supp < 0) where += " and t.nzp_supp not in (select nzp_supp from  " +
                        "CNTRPRFX_kernel" + tableDelimiter + "supplier where nzp_payer_supp =" + -finder.nzp_payer_supp + ")";
                if (finder.nzp_payer_princip < 0) where += " and t.nzp_supp not in (select nzp_supp from  " +
                        "CNTRPRFX_kernel" + tableDelimiter + "supplier where nzp_payer_princip =" + -finder.nzp_payer_princip + ")";
                if (finder.nzp_payer_agent < 0) where += " and t.nzp_supp not in (select nzp_supp from  " +
                        "CNTRPRFX_kernel" + tableDelimiter + "supplier where nzp_payer_agent =" + -finder.nzp_payer_agent + ")";
            }

#if PG
if (Utils.GetParams(finder.prms, Constants.page_spisls))
            {
                if (finder.nzp_serv > 0 || finder.nzp_supp > 0 || finder.nzp_frm > 0 || (finder.nzp_serv == 0 && finder.nzp_supp == 0 && finder.nzp_frm == 0))
                {
                    sql = "Select count(*) From PREFX_data.tarif t Where t.nzp_kvar = k.nzp_kvar" + where;
                }
                else
                {
                    sql = "Select count(*) From PREFX_data.kvar k1 Where k1.nzp_kvar = k.nzp_kvar and k1.nzp_kvar not in (Select t.nzp_kvar from PREFX_data.tarif t where t.nzp_kvar = k1.nzp_kvar" + where + ")";
                }
            }
            else if (Utils.GetParams(finder.prms, Constants.page_spisdom))
            {
                if (finder.nzp_serv > 0 || finder.nzp_supp > 0 || finder.nzp_frm > 0 || (finder.nzp_serv == 0 && finder.nzp_supp == 0 && finder.nzp_frm == 0))
                {
                    sql = "Select count(*) From PREFX_data.kvar k1, PREFX_data.tarif t Where k1.nzp_dom = d.nzp_dom and t.nzp_kvar = k1.nzp_kvar" + where;
                }
                else
                {
                    sql = "Select count(*) From PREFX_data.kvar k1 Where k1.nzp_dom = d.nzp_dom and k1.nzp_kvar not in (Select t.nzp_kvar from PREFX_data.tarif t where t.nzp_kvar = k1.nzp_kvar" + where + ")";
                }
            }
#else
            if (Utils.GetParams(finder.prms, Constants.page_spisls))
            {
                if (finder.nzp_serv > 0 || finder.nzp_supp > 0 || finder.nzp_frm > 0 || (finder.nzp_serv == 0 && finder.nzp_supp == 0 && finder.nzp_frm == 0))
                {
                    sql = "Select count(*) From PREFX_data:tarif t Where t.nzp_kvar = k.nzp_kvar" + where;
                }
                else
                {
                    sql = "Select count(*) From PREFX_data:kvar k1 Where k1.nzp_kvar = k.nzp_kvar and k1.nzp_kvar not in (Select t.nzp_kvar from PREFX_data:tarif t where t.nzp_kvar = k1.nzp_kvar" + where + ")";
                }
            }
            else if (Utils.GetParams(finder.prms, Constants.page_spisdom))
            {
                if (finder.nzp_serv > 0 || finder.nzp_supp > 0 || finder.nzp_frm > 0 || (finder.nzp_serv == 0 && finder.nzp_supp == 0 && finder.nzp_frm == 0))
                {
                    sql = "Select count(*) From PREFX_data:kvar k1, PREFX_data:tarif t Where k1.nzp_dom = d.nzp_dom and t.nzp_kvar = k1.nzp_kvar" + where;
                }
                else
                {
                    sql = "Select count(*) From PREFX_data:kvar k1 Where k1.nzp_dom = d.nzp_dom and k1.nzp_kvar not in (Select t.nzp_kvar from PREFX_data:tarif t where t.nzp_kvar = k1.nzp_kvar" + where + ")";
                }
            }
#endif

            return sql;
        }
    }

}