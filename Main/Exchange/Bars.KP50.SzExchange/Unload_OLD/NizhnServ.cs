using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;

namespace Bars.KP50.SzExchange.Unload
{
    class NizhnServ : DataBaseHeadServer
    {
        /// <summary>
        /// Функция замены услуги для Нижнекамска
        /// </summary>
        /// <param name="finder"></param>
        /// <param name="conn_db"></param>
        /// <param name="dats"></param>
        public Returns ReplaceServForNizhn(FilesImported finder, IDbConnection conn_db, string dats, bool stop_flag, string sAliasm)
        {
            Returns ret = Utils.InitReturns();
            string sql;
            string sChargeAlias = "_charge_" + dats.Substring(8, 2);
            //bool is_serv266 = false;

            sql =
                " SELECT * " +
                " FROM " + finder.bank + sChargeAlias + ".charge_" + dats.Substring(3, 2) +
                " WHERE nzp_serv IN (266, 284, 285)" +
                " AND dat_charge IS NULL";
            DataTable dt = ClassDBUtils.OpenSQL(sql, conn_db, ClassDBUtils.ExecMode.Exception).GetData();
            if (dt.Rows.Count != 0)
            {
              //  is_serv266 = true;

                sql =
                    " SELECT a.nzp_kvar, a.num_ls, a.nzp_serv,a.nzp_supp, " +
                    " a.nzp_frm, a.isdel, a.is_device, " +
                    " (case when a.sum_tarif_eot > 0 then a.tarif else 0 end) as tarif, " +
                    " (case when a.sum_tarif_eot > 0 then a.tarif_f else 0 end) as tarif_f, " +
                    " a.sum_insaldo, a.sum_tarif_sn_f, a.sum_tarif_eot, a.sum_subsidy, " +
                    " a.sum_lgaota, a.sum_smo, a.reval_lgota, a.sum_money, a.c_sn " +
                    " INTO TEMP t_charge266" +
                    " FROM " + finder.bank + sChargeAlias + ".charge_" + dats.Substring(3, 2) + " a, " +
                    " t_adres b" +
                    " WHERE a.serv IN (16, 266, 284, 285) " +
                    " AND nzp_supp > -999 " +
                    " AND dat_charge IS NULL " +
                    " AND a.nzp_kvar = b.nzp_kvar";
                DBManager.ExecSQL(conn_db, sql, true);

                sql =
                    " SELECT nzp_kvar, num_ls, isdel, MAX(nzp_frm) as nzp_frm, MAX(nzp_supp) as nzp_supp" +
                    " INTO TEMP t_cor266" +
                    " FROM t_charge266 a " +
                    " WHERE nzp_serv = 16" +
                    " GROUP BY 1,2,3";
                DBManager.ExecSQL(conn_db, sql, true);

                sql =
                    " UPDATE t_charge266 " +
                    " SET (nzp_supp, nzp_frm, nzp_Serv) = (" +
                    "  (SELECT nzp_supp, nzp_frm, 16 " +
                    "  FROM t_cor266 b " +
                    "  WHERE t_charge266.nzp_kvar = b.nzp_kvar " +
                    "  AND t_charge266.isdel = b.isdel)) " +
                    " WHERE nzp_serv IN (266, 284, 285)";
                DBManager.ExecSQL(conn_db, sql, true);

                sql =
                    " DROP TABLE t_cor266 ";
                DBManager.ExecSQL(conn_db, sql, false);

                sql =
                    " CREATE INDEX ix_tmpr0192 ON t_charge266(nzp_kvar, nzp_serv, npz_frm)";
                DBManager.ExecSQL(conn_db, sql, true);

                sql =
                    " ANALYZE t_charge266";
                DBManager.ExecSQL(conn_db, sql, false);
            }

            return ret;
        }
    }
}
