using System;
using System.Linq;
using System.Text;
using System.Data;
using System.Collections.Generic;
using System.Globalization;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;

namespace STCLINE.KP50.DataBase
{
    public partial class DbCalc : DbCalcClient
    {
        //--------------------------------------------------------------------------------
        public void AlterTableReport(out Returns ret)
        //--------------------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            if (!Points.isFinances) return;

            for (int yy = 2011; yy <= Points.CalcMonth.year_; yy++)
            {
                ParamCalc paramcalc = new ParamCalc(0, 0, "", yy, 1, yy, 1);
                ChargeXX chargeXX = new ChargeXX(paramcalc);

                AlterTableReport(chargeXX, out ret);
            }
        }
        //--------------------------------------------------------------------------------
        void AlterTableReport(ChargeXX chargeXX, out Returns ret)
        //--------------------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            IDbConnection conn_db2 = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(conn_db2, true);
            if (!ret.result)
            {
                return;
            }
            string sbd_fin = Points.Pref + "_fin_" + (chargeXX.paramcalc.calc_yy - 2000).ToString("00");
#if PG
            string sbd_fin_pref = sbd_fin.Trim() + ".";
            string stab_pack = sbd_fin_pref.Trim() + "pack";
            string sTypeDecimal = "numeric";
            string sTypeUnique = "distinct";
            string sTBLUser = "";
#else
            string sbd_fin_pref = "";
            string stab_pack = sbd_fin.Trim() + ":pack";
            string sTypeDecimal = "Decimal";
            string sTypeUnique = "unique";
            string sTBLUser = "are.";

            ret = ExecSQL(conn_db2, " Database " + sbd_fin, true);
            if (!ret.result)
            {
                conn_db2.Close();
                return;
            }
#endif
            if (!TempTableInWebCashe(conn_db2, stab_pack))
            {
                ret.result = false;
                ret.text = "БД " + sbd_fin + " не существует";
                return;
            }
#if PG
            ret = ExecSQL(conn_db2, " alter table " + sbd_fin_pref + "fn_ukrgudom add sum_tarif_f " + sTypeDecimal + "(14,2) default 0 ", false);
            ret = ExecSQL(conn_db2, " alter table " + sbd_fin_pref + "fn_ukrgucharge add sum_tarif_f " + sTypeDecimal + "(14,2) default 0 ", false);
            ret = ExecSQL(conn_db2, " alter table " + sbd_fin_pref + "fn_ukrgudom add sum_tarif_f_p " + sTypeDecimal + "(14,2) default 0 ", false);
            ret = ExecSQL(conn_db2, " alter table " + sbd_fin_pref + "fn_ukrgucharge add sum_tarif_f_p " + sTypeDecimal + "(14,2) default 0 ", false);
            ret = ExecSQL(conn_db2, " alter table " + sbd_fin_pref + "fn_ukrgudom add real_charge_k " + sTypeDecimal + "(14,2) default 0 ", false);
            ret = ExecSQL(conn_db2, " alter table " + sbd_fin_pref + "fn_ukrgudom add real_charge_d " + sTypeDecimal + "(14,2) default 0 ", false);
            ret = ExecSQL(conn_db2, " alter table " + sbd_fin_pref + "fn_ukrgucharge add real_charge_k " + sTypeDecimal + "(14,2) default 0 ", false);
            ret = ExecSQL(conn_db2, " alter table " + sbd_fin_pref + "fn_ukrgucharge add real_charge_d " + sTypeDecimal + "(14,2) default 0 ", false);
            ret = ExecSQL(conn_db2, " alter table " + sbd_fin_pref + "fn_ukrgudom add reval_k " + sTypeDecimal + "(14,2) default 0 ", false);
            ret = ExecSQL(conn_db2, " alter table " + sbd_fin_pref + "fn_ukrgudom add reval_d " + sTypeDecimal + "(14,2) default 0 ", false);
            ret = ExecSQL(conn_db2, " alter table " + sbd_fin_pref + "fn_ukrgucharge add reval_k " + sTypeDecimal + "(14,2) default 0 ", false);
            ret = ExecSQL(conn_db2, " alter table " + sbd_fin_pref + "fn_ukrgucharge add reval_d " + sTypeDecimal + "(14,2) default 0 ", false);
#else
            ret = ExecSQL(conn_db2, " alter table " + sbd_fin_pref + "fn_ukrgudom add sum_tarif_f " + sTypeDecimal + "(14,2) default 0 before sum_tarif_p ", false);
            ret = ExecSQL(conn_db2, " alter table " + sbd_fin_pref + "fn_ukrgucharge add sum_tarif_f " + sTypeDecimal + "(14,2) default 0 before sum_tarif_p ", false);
            ret = ExecSQL(conn_db2, " alter table " + sbd_fin_pref + "fn_ukrgudom add sum_tarif_f_p " + sTypeDecimal + "(14,2) default 0 before sum_tarif_p ", false);
            ret = ExecSQL(conn_db2, " alter table " + sbd_fin_pref + "fn_ukrgucharge add sum_tarif_f_p " + sTypeDecimal + "(14,2) default 0 before sum_tarif_p ", false);
            ret = ExecSQL(conn_db2, " alter table " + sbd_fin_pref + "fn_ukrgudom add real_charge_k " + sTypeDecimal + "(14,2) default 0 before sum_izm ", false);
            ret = ExecSQL(conn_db2, " alter table " + sbd_fin_pref + "fn_ukrgudom add real_charge_d " + sTypeDecimal + "(14,2) default 0 before sum_izm ", false);
            ret = ExecSQL(conn_db2, " alter table " + sbd_fin_pref + "fn_ukrgucharge add real_charge_k " + sTypeDecimal + "(14,2) default 0 before sum_izm ", false);
            ret = ExecSQL(conn_db2, " alter table " + sbd_fin_pref + "fn_ukrgucharge add real_charge_d " + sTypeDecimal + "(14,2) default 0 before sum_izm ", false);
            ret = ExecSQL(conn_db2, " alter table " + sbd_fin_pref + "fn_ukrgudom add reval_k " + sTypeDecimal + "(14,2) default 0 before sum_izm ", false);
            ret = ExecSQL(conn_db2, " alter table " + sbd_fin_pref + "fn_ukrgudom add reval_d " + sTypeDecimal + "(14,2) default 0 before sum_izm ", false);
            ret = ExecSQL(conn_db2, " alter table " + sbd_fin_pref + "fn_ukrgucharge add reval_k " + sTypeDecimal + "(14,2) default 0 before sum_izm ", false);
            ret = ExecSQL(conn_db2, " alter table " + sbd_fin_pref + "fn_ukrgucharge add reval_d " + sTypeDecimal + "(14,2) default 0 before sum_izm ", false);
#endif
            //построить индекс по дому
            ret = ExecSQL(conn_db2, " CREATE " + sTypeUnique + " INDEX " + sTBLUser + "ix_frud_07 ON fn_ukrgudom   (nzp_dom,nzp_area,nzp_geu,nzp_serv,nzp_supp,nzp_wp,month_)", false);
            ret = ExecSQL(conn_db2, " CREATE " + sTypeUnique + " INDEX " + sTBLUser + "ix_fru_05  ON fn_ukrgucharge(nzp_area,nzp_geu,nzp_serv,nzp_supp,nzp_wp,month_)", false);

#if PG

            ret = ExecSQL(conn_db2, " analyze " + sbd_fin_pref + "fn_ukrgudom ", true);
            ret = ExecSQL(conn_db2, " analyze " + sbd_fin_pref + "fn_ukrgucharge ", true);
#else
            ret = ExecSQL(conn_db2, " update statistics for table fn_ukrgudom ", true);
            ret = ExecSQL(conn_db2, " update statistics for table fn_ukrgucharge ", true);

            ret = ExecSQL(conn_db2, " Database " + Points.Pref + "_kernel", true);
            if (!ret.result)
            {
                conn_db2.Close();
                return;
            }
#endif

            conn_db2.Close();
        }

        //-----------------------------------------------------------------------------
        public bool CalcReportXX(IDbConnection conn_db, ParamCalc paramcalc, out Returns ret)
        //-----------------------------------------------------------------------------
        {
            DropTempTablesCharge(conn_db);

            ChargeXX chargeXX = new ChargeXX(paramcalc);
            AlterTableReport(chargeXX, out ret);

            //---------------------------------------------------
            //выбрать множество лицевых счетов
            //---------------------------------------------------
            if (!TempTableInWebCashe(conn_db, "t_selkvar"))
            {
                ChoiseTempKvar(conn_db, ref paramcalc, out ret);
                if (!ret.result)
                {
                    conn_db.Close();
                    return false;
                }
            }

            //report_xx = Points.Pref + "_fin_" + (calc_yy - 2000).ToString("00") + ".fn_ukrgucharge ";
            //report_xx_dom = Points.Pref + "_fin_" + (calc_yy - 2000).ToString("00") + ".fn_ukrgudom ";

#if PG

            ret = ExecSQL(conn_db, " Update " + chargeXX.report_xx_dom + " Set rsum_tarif     = 0 Where " + chargeXX.paramcalc.where_z + chargeXX.where_report, true);
            ret = ExecSQL(conn_db, " Update " + chargeXX.report_xx_dom + " Set sum_tarif      = 0 Where " + chargeXX.paramcalc.where_z + chargeXX.where_report, true);
            ret = ExecSQL(conn_db, " Update " + chargeXX.report_xx_dom + " Set sum_tarif_f    = 0 Where " + chargeXX.paramcalc.where_z + chargeXX.where_report, true);
            ret = ExecSQL(conn_db, " Update " + chargeXX.report_xx_dom + " Set sum_tarif_p    = 0 Where " + chargeXX.paramcalc.where_z + chargeXX.where_report, true);
            ret = ExecSQL(conn_db, " Update " + chargeXX.report_xx_dom + " Set rsum_lgota     = 0 Where " + chargeXX.paramcalc.where_z + chargeXX.where_report, true);
            ret = ExecSQL(conn_db, " Update " + chargeXX.report_xx_dom + " Set sum_lgota      = 0 Where " + chargeXX.paramcalc.where_z + chargeXX.where_report, true);
            ret = ExecSQL(conn_db, " Update " + chargeXX.report_xx_dom + " Set sum_real       = 0 Where " + chargeXX.paramcalc.where_z + chargeXX.where_report, true);
            ret = ExecSQL(conn_db, " Update " + chargeXX.report_xx_dom + " Set old_money      = 0 Where " + chargeXX.paramcalc.where_z + chargeXX.where_report, true);
            ret = ExecSQL(conn_db, " Update " + chargeXX.report_xx_dom + " Set sum_money      = 0 Where " + chargeXX.paramcalc.where_z + chargeXX.where_report, true);
            ret = ExecSQL(conn_db, " Update " + chargeXX.report_xx_dom + " Set money_to       = 0 Where " + chargeXX.paramcalc.where_z + chargeXX.where_report, true);
            ret = ExecSQL(conn_db, " Update " + chargeXX.report_xx_dom + " Set money_from     = 0 Where " + chargeXX.paramcalc.where_z + chargeXX.where_report, true);
            ret = ExecSQL(conn_db, " Update " + chargeXX.report_xx_dom + " Set money_del      = 0 Where " + chargeXX.paramcalc.where_z + chargeXX.where_report, true);
            ret = ExecSQL(conn_db, " Update " + chargeXX.report_xx_dom + " Set reval          = 0 Where " + chargeXX.paramcalc.where_z + chargeXX.where_report, true);
            ret = ExecSQL(conn_db, " Update " + chargeXX.report_xx_dom + " Set real_charge    = 0 Where " + chargeXX.paramcalc.where_z + chargeXX.where_report, true);
            ret = ExecSQL(conn_db, " Update " + chargeXX.report_xx_dom + " Set sum_nedop      = 0 Where " + chargeXX.paramcalc.where_z + chargeXX.where_report, true);
            ret = ExecSQL(conn_db, " Update " + chargeXX.report_xx_dom + " Set sum_nedop_p    = 0 Where " + chargeXX.paramcalc.where_z + chargeXX.where_report, true);
            ret = ExecSQL(conn_db, " Update " + chargeXX.report_xx_dom + " Set sum_pere       = 0 Where " + chargeXX.paramcalc.where_z + chargeXX.where_report, true);
            ret = ExecSQL(conn_db, " Update " + chargeXX.report_xx_dom + " Set sum_insaldo    = 0 Where " + chargeXX.paramcalc.where_z + chargeXX.where_report, true);
            ret = ExecSQL(conn_db, " Update " + chargeXX.report_xx_dom + " Set sum_outsaldo   = 0 Where " + chargeXX.paramcalc.where_z + chargeXX.where_report, true);
            ret = ExecSQL(conn_db, " Update " + chargeXX.report_xx_dom + " Set old_charge     = 0 Where " + chargeXX.paramcalc.where_z + chargeXX.where_report, true);
            ret = ExecSQL(conn_db, " Update " + chargeXX.report_xx_dom + " Set sum_nach       = 0 Where " + chargeXX.paramcalc.where_z + chargeXX.where_report, true);
            ret = ExecSQL(conn_db, " Update " + chargeXX.report_xx_dom + " Set sum_insaldo_k  = 0 Where " + chargeXX.paramcalc.where_z + chargeXX.where_report, true);
            ret = ExecSQL(conn_db, " Update " + chargeXX.report_xx_dom + " Set sum_insaldo_d  = 0 Where " + chargeXX.paramcalc.where_z + chargeXX.where_report, true);
            ret = ExecSQL(conn_db, " Update " + chargeXX.report_xx_dom + " Set sum_outsaldo_k = 0 Where " + chargeXX.paramcalc.where_z + chargeXX.where_report, true);
            ret = ExecSQL(conn_db, " Update " + chargeXX.report_xx_dom + " Set sum_outsaldo_d = 0 Where " + chargeXX.paramcalc.where_z + chargeXX.where_report, true);
            ret = ExecSQL(conn_db, " Update " + chargeXX.report_xx_dom + " Set sum_pere_d     = 0 Where " + chargeXX.paramcalc.where_z + chargeXX.where_report, true);
            ret = ExecSQL(conn_db, " Update " + chargeXX.report_xx_dom + " Set sum_izm        = 0 Where " + chargeXX.paramcalc.where_z + chargeXX.where_report, true);

#else
ret = ExecSQL(conn_db,
                " Update " + chargeXX.report_xx_dom +
                " Set ( rsum_tarif, sum_tarif, sum_tarif_f, sum_tarif_p, rsum_lgota, sum_lgota, sum_real, old_money, sum_money, money_to, money_from, money_del, " +
                      " reval, real_charge, sum_nedop, sum_nedop_p, sum_pere, sum_insaldo, sum_outsaldo, old_charge, sum_nach, sum_insaldo_k, sum_insaldo_d, " +
                      " sum_outsaldo_k, sum_outsaldo_d, sum_pere_d, sum_izm) = " +
                     " ( 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 ) " +
                " Where " + chargeXX.paramcalc.where_z + chargeXX.where_report
                , true);
#endif
            if (!ret.result)
            {
                DropTempTablesCharge(conn_db);
                return false;
            }
#if PG

            ret = ExecSQL(conn_db, " Update " + chargeXX.report_xx + " Set rsum_tarif     = 0 Where 1 = 1 " + chargeXX.where_report, true);
            ret = ExecSQL(conn_db, " Update " + chargeXX.report_xx + " Set sum_tarif      = 0 Where 1 = 1 " + chargeXX.where_report, true);
            ret = ExecSQL(conn_db, " Update " + chargeXX.report_xx + " Set sum_tarif_f    = 0 Where 1 = 1 " + chargeXX.where_report, true);
            ret = ExecSQL(conn_db, " Update " + chargeXX.report_xx + " Set sum_tarif_p    = 0 Where 1 = 1 " + chargeXX.where_report, true);
            ret = ExecSQL(conn_db, " Update " + chargeXX.report_xx + " Set rsum_lgota     = 0 Where 1 = 1 " + chargeXX.where_report, true);
            ret = ExecSQL(conn_db, " Update " + chargeXX.report_xx + " Set sum_lgota      = 0 Where 1 = 1 " + chargeXX.where_report, true);
            ret = ExecSQL(conn_db, " Update " + chargeXX.report_xx + " Set sum_real       = 0 Where 1 = 1 " + chargeXX.where_report, true);
            ret = ExecSQL(conn_db, " Update " + chargeXX.report_xx + " Set old_money      = 0 Where 1 = 1 " + chargeXX.where_report, true);
            ret = ExecSQL(conn_db, " Update " + chargeXX.report_xx + " Set sum_money      = 0 Where 1 = 1 " + chargeXX.where_report, true);
            ret = ExecSQL(conn_db, " Update " + chargeXX.report_xx + " Set money_to       = 0 Where 1 = 1 " + chargeXX.where_report, true);
            ret = ExecSQL(conn_db, " Update " + chargeXX.report_xx + " Set money_from     = 0 Where 1 = 1 " + chargeXX.where_report, true);
            ret = ExecSQL(conn_db, " Update " + chargeXX.report_xx + " Set money_del      = 0 Where 1 = 1 " + chargeXX.where_report, true);
            ret = ExecSQL(conn_db, " Update " + chargeXX.report_xx + " Set reval          = 0 Where 1 = 1 " + chargeXX.where_report, true);
            ret = ExecSQL(conn_db, " Update " + chargeXX.report_xx + " Set real_charge    = 0 Where 1 = 1 " + chargeXX.where_report, true);
            ret = ExecSQL(conn_db, " Update " + chargeXX.report_xx + " Set sum_nedop      = 0 Where 1 = 1 " + chargeXX.where_report, true);
            ret = ExecSQL(conn_db, " Update " + chargeXX.report_xx + " Set sum_nedop_p    = 0 Where 1 = 1 " + chargeXX.where_report, true);
            ret = ExecSQL(conn_db, " Update " + chargeXX.report_xx + " Set sum_pere       = 0 Where 1 = 1 " + chargeXX.where_report, true);
            ret = ExecSQL(conn_db, " Update " + chargeXX.report_xx + " Set sum_insaldo    = 0 Where 1 = 1 " + chargeXX.where_report, true);
            ret = ExecSQL(conn_db, " Update " + chargeXX.report_xx + " Set sum_outsaldo   = 0 Where 1 = 1 " + chargeXX.where_report, true);
            ret = ExecSQL(conn_db, " Update " + chargeXX.report_xx + " Set old_charge     = 0 Where 1 = 1 " + chargeXX.where_report, true);
            ret = ExecSQL(conn_db, " Update " + chargeXX.report_xx + " Set sum_nach       = 0 Where 1 = 1 " + chargeXX.where_report, true);
            ret = ExecSQL(conn_db, " Update " + chargeXX.report_xx + " Set sum_insaldo_k  = 0 Where 1 = 1 " + chargeXX.where_report, true);
            ret = ExecSQL(conn_db, " Update " + chargeXX.report_xx + " Set sum_insaldo_d  = 0 Where 1 = 1 " + chargeXX.where_report, true);
            ret = ExecSQL(conn_db, " Update " + chargeXX.report_xx + " Set sum_outsaldo_k = 0 Where 1 = 1 " + chargeXX.where_report, true);
            ret = ExecSQL(conn_db, " Update " + chargeXX.report_xx + " Set sum_outsaldo_d = 0 Where 1 = 1 " + chargeXX.where_report, true);
            ret = ExecSQL(conn_db, " Update " + chargeXX.report_xx + " Set sum_pere_d     = 0 Where 1 = 1 " + chargeXX.where_report, true);
            ret = ExecSQL(conn_db, " Update " + chargeXX.report_xx + " Set sum_izm        = 0 Where 1 = 1 " + chargeXX.where_report, true);

#else
 ret = ExecSQL(conn_db,
                " Update " + chargeXX.report_xx +
                " Set ( rsum_tarif, sum_tarif, sum_tarif_f, sum_tarif_p, rsum_lgota, sum_lgota, sum_real, old_money, sum_money, money_to, money_from, money_del, " +
                      " reval, real_charge, sum_nedop, sum_nedop_p, sum_pere, sum_insaldo, sum_outsaldo, old_charge, sum_nach, sum_insaldo_k, " +
                      " sum_insaldo_d, sum_outsaldo_k, sum_outsaldo_d, sum_pere_d, sum_izm) = " +
                     " ( 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 ) " +
                " Where 1 = 1 " + chargeXX.where_report
                , true);
#endif
            if (!ret.result)
            {
                DropTempTablesCharge(conn_db);
                return false;
            }

            UpdateStatisticsFin(chargeXX.paramcalc.calc_yy, out ret);

            //---------------------------------------------------
            //выборка сумм по домам
            //---------------------------------------------------
            string tab = chargeXX.paramcalc.pref + "_charge_" + (chargeXX.paramcalc.calc_yy - 2000).ToString("00");

#if PG
            tab = tab.Trim() + ".charge_" + chargeXX.paramcalc.calc_mm.ToString("00");
            string ssql = " Create temp table t_ins_ch ";
            string sTypDecimal = "numeric";
#else
            tab = tab.Trim() + ":charge_" + chargeXX.paramcalc.calc_mm.ToString("00");
            string ssql = " Create temp table t_ins_ch ";
            string sTypDecimal = "Decimal";
#endif

            ssql = ssql.Trim() +
                "  ( nzp_key  serial not null, " +
                   " nzp_area integer, " +
                   " nzp_geu  integer, " +
                   " nzp_dom  integer, " +
                   " nzp_serv integer, " +
                   " nzp_supp integer, " +
                   " nzp_wp   integer, " +
                   " kod      integer default 0, " +
                   " rsum_tarif " + sTypDecimal + "(14,2) default 0, " +
                   " sum_tarif " + sTypDecimal + "(14,2) default 0, " +
                   " sum_tarif_f " + sTypDecimal + "(14,2) default 0, " +
                   " sum_tarif_p " + sTypDecimal + "(14,2) default 0, " +
                   " sum_tarif_f_p " + sTypDecimal + "(14,2) default 0, " +
                   " rsum_lgota " + sTypDecimal + "(14,2) default 0, " +
                   " sum_lgota " + sTypDecimal + "(14,2) default 0, " +
                   " sum_real " + sTypDecimal + "(14,2) default 0, " +
                   " sum_izm " + sTypDecimal + "(14,2) default 0, " +
                   " old_money " + sTypDecimal + "(14,2) default 0, " +
                   " sum_money " + sTypDecimal + "(14,2) default 0, " +
                   " money_to " + sTypDecimal + "(14,2) default 0, " +
                   " money_from " + sTypDecimal + "(14,2) default 0, " +
                   " money_del " + sTypDecimal + "(14,2) default 0, " +
                   " reval_k " + sTypDecimal + "(14,2) default 0, " +
                   " reval_d " + sTypDecimal + "(14,2) default 0, " +
                   " reval " + sTypDecimal + "(14,2) default 0, " +
                   " real_charge_k " + sTypDecimal + "(14,2) default 0, " +
                   " real_charge_d " + sTypDecimal + "(14,2) default 0, " +
                   " real_charge " + sTypDecimal + "(14,2) default 0, " +
                   " sum_nedop " + sTypDecimal + "(14,2) default 0, " +
                   " sum_nedop_p " + sTypDecimal + "(14,2) default 0, " +
                   " sum_pere " + sTypDecimal + "(14,2)  default 0, " +
                   " sum_insaldo " + sTypDecimal + "(14,2) default 0, " +
                   " sum_outsaldo " + sTypDecimal + "(14,2) default 0, " +
                   " sum_insaldo_k " + sTypDecimal + "(14,2) default 0, " +
                   " sum_outsaldo_k " + sTypDecimal + "(14,2) default 0, " +
                   " sum_insaldo_d " + sTypDecimal + "(14,2) default 0, " +
                   " sum_outsaldo_d " + sTypDecimal + "(14,2) default 0, " +
                   " sum_pere_d " + sTypDecimal + "(14,2) default 0, " +
                   " old_charge " + sTypDecimal + "(14,2) default 0, " +
                   " sum_nach " + sTypDecimal + "(14,2) default 0 " +
#if PG
                " ) ";
#else
                " ) With no log ";
#endif

            ret = ExecSQL(conn_db, ssql, true);
            if (!ret.result)
            {
                DropTempTablesCharge(conn_db);
                return false;
            }

            int icountvals = 0;
            IDbCommand cmd1000 = DBManager.newDbCommand(" Select count(*) From t_selkvar Where 1 = 1 ", conn_db);
            try
            {
                string scountvals = Convert.ToString(cmd1000.ExecuteScalar());
                icountvals = Convert.ToInt32(scountvals);
            }
            catch
            {
                icountvals = 0;
            }
            bool bIsL100 = (icountvals < 100);

            string sfind_tmp =
                " Select nzp_kvar,nzp_supp,nzp_serv, " +
                       " rsum_tarif,sum_tarif,sum_tarif_f,rsum_lgota,sum_lgota,sum_real,sum_charge, " +
                       " reval,real_charge,sum_nedop,sum_pere,sum_insaldo," +
                       " sum_outsaldo,sum_money,money_to,money_from,money_del,izm_saldo ";

#if PG
            sfind_tmp = sfind_tmp.Trim() +
                " into temp ttt_charge " +
                " From " + tab +
                " Where nzp_serv <> 1 and dat_charge is null ";
#else
            sfind_tmp = sfind_tmp.Trim() +
                " From " + tab +
                " Where nzp_serv <> 1 and dat_charge is null " +
                " into temp ttt_charge with no log ";
#endif

            // если расчет по кол-ву л/с меньше 100, то соединить с t_selkvar для min времени выборки
            if (bIsL100)
            {
                sfind_tmp =
                " Select a.nzp_kvar,a.nzp_supp,a.nzp_serv, " +
                       " a.rsum_tarif,a.sum_tarif,a.sum_tarif_f,a.rsum_lgota,a.sum_lgota,a.sum_real,a.sum_charge, " +
                       " a.reval,a.real_charge,a.sum_nedop,a.sum_pere,a.sum_insaldo," +
                       " a.sum_outsaldo,a.sum_money,a.money_to,a.money_from,a.money_del,a.izm_saldo ";
#if PG
                sfind_tmp = sfind_tmp.Trim() +
                " into temp ttt_charge " +
                " From " + tab + " a, t_selkvar k" +
                " Where a.nzp_kvar=k.nzp_kvar and a.nzp_serv <> 1 and a.dat_charge is null ";
#else
                sfind_tmp = sfind_tmp.Trim() +
                " From " + tab + " a, t_selkvar k" +
                " Where a.nzp_kvar=k.nzp_kvar and a.nzp_serv <> 1 and a.dat_charge is null " +
                " into temp ttt_charge with no log ";
#endif
            }

            ret = ExecSQL(conn_db, sfind_tmp, true);
            if (!ret.result)
            {
                DropTempTablesCharge(conn_db);
                return false;
            }

            ExecSQL(conn_db, " Create index ix_ttt_charge1 on ttt_charge (nzp_kvar) ", true);
            //ExecSQL(conn_db, " Create index ix_ttt_charge2 on ttt_charge (nzp_dom) ", true);
            ExecSQL(conn_db, " Create index ix_ttt_charge3 on ttt_charge (nzp_supp,nzp_serv) ", true);
#if PG
            ExecSQL(conn_db, " analyze ttt_charge ", true);
#else
            ExecSQL(conn_db, " Update statistics for table ttt_charge ", true);
#endif

            ret = ExecSQL(conn_db,
                " Insert into t_ins_ch (nzp_wp, nzp_dom, nzp_area,nzp_geu,nzp_supp,nzp_serv,rsum_tarif, sum_tarif, sum_tarif_f,rsum_lgota,sum_lgota,sum_real,old_charge,sum_nach, " +
                       " reval,real_charge,sum_nedop,sum_pere,sum_insaldo,sum_outsaldo, old_money,sum_money,money_to,money_from,money_del, " +
                       " sum_insaldo_k,sum_insaldo_d,sum_outsaldo_k,sum_outsaldo_d, sum_izm, reval_k, reval_d, real_charge_k, real_charge_d ) " +
                " Select " + chargeXX.paramcalc.nzp_wp + ",k.nzp_dom,k.nzp_area,k.nzp_geu, nzp_supp,nzp_serv, " +
                       " sum(rsum_tarif) rsum_tarif,sum(sum_tarif) sum_tarif,sum(sum_tarif_f) as sum_tarif, sum(rsum_lgota) rsum_lgota,sum(sum_lgota) sum_lgota," +
                       " sum(sum_real) sum_real,sum(sum_charge) sum_charge,sum(sum_charge) sum_charge, " +
                       " sum(reval) reval,sum(real_charge) real_charge,sum(sum_nedop) sum_nedop,sum(sum_pere) sum_pere,sum(sum_insaldo) sum_insaldo," +
                       " sum(sum_outsaldo) sum_outsaldo, sum(sum_money) as old_money, sum(sum_money) as sum_money, sum(money_to) money_to, " +
                       " sum(money_from) money_from,sum(money_del) money_del, " +
                       " sum(case when sum_insaldo<0 then sum_insaldo else 0 end) sum_insaldo_k, " +
                       " sum(case when sum_insaldo<0 then 0 else sum_insaldo end) sum_insaldo_d, " +
                       " sum(case when sum_outsaldo<0 then sum_outsaldo else 0 end) sum_outsaldo_k, " +
                       " sum(case when sum_outsaldo<0 then 0 else sum_outsaldo end) sum_outsaldo_d, sum(izm_saldo), " +
                       " sum(case when reval<0 then reval else 0 end) reval_k, " +
                       " sum(case when reval<0 then 0 else reval end) reval_d, " +
                       " sum(case when real_charge<0 then real_charge else 0 end) real_charge_k, " +
                       " sum(case when real_charge<0 then 0 else real_charge end) real_charge_d " +
                " From ttt_charge a, t_selkvar k " +
                " Where a.nzp_kvar = k.nzp_kvar " +
                " Group by 1,2,3,4,5,6 "
                , true, 6000);

            if (!ret.result)
            {
                DropTempTablesCharge(conn_db);
                return false;
            }
            ExecSQL(conn_db, " Create unique index ix_t_ins_ch1 on t_ins_ch (nzp_key) ", true);
            //ExecSQL(conn_db, " Create unique index ix_t_ins_ch2 on t_ins_ch (nzp_dom,nzp_area,nzp_geu,nzp_serv,nzp_supp,nzp_wp) ", true);
            ExecSQL(conn_db, " Create        index ix_t_ins_ch2 on t_ins_ch (nzp_dom,nzp_area,nzp_geu,nzp_serv,nzp_supp,nzp_wp) ", true); //из-за перерасчетов
            ExecSQL(conn_db, " Create        index ix_t_ins_ch3 on t_ins_ch (kod) ", true);
#if PG
            ExecSQL(conn_db, " analyze t_ins_ch ", true);
#else
            ExecSQL(conn_db, " Update statistics for table t_ins_ch ", true);
#endif
            //считаем перерасчеты
            ExecSQL(conn_db, " Drop table t_prev_ch ", false);

#if PG
            ret = ExecSQL(conn_db,
                " Create temp table t_prev_ch " +
                "  ( nzp_area integer, " +
                   " nzp_geu  integer, " +
                   " nzp_dom  integer, " +
                   " nzp_serv integer, " +
                   " nzp_supp integer, " +
                   " sum_tarif numeric(14,2)," +
                   " sum_tarif_f numeric(14,2)," +
                   " sum_nedop numeric(14,2) " +
                " ) "
                , true);
#else
            ret = ExecSQL(conn_db,
                " Create temp table t_prev_ch " +
                "  ( nzp_area integer, " +
                   " nzp_geu  integer, " +
                   " nzp_dom  integer, " +
                   " nzp_serv integer, " +
                   " nzp_supp integer, " +
                   " sum_tarif decimal(14,2)," +
                   " sum_tarif_f decimal(14,2)," +
                   " sum_nedop decimal(14,2) " +
                " ) With no log "
                , true);
#endif
            if (!ret.result)
            {
                DropTempTablesCharge(conn_db);
                return false;
            }

#if PG
            tab = chargeXX.paramcalc.pref + "_charge_" + (chargeXX.paramcalc.calc_yy - 2000).ToString("00") + ".lnk_charge_" + chargeXX.paramcalc.calc_mm.ToString("00");
            string sKernelDBName = chargeXX.paramcalc.pref + "_kernel.";
#else
            tab = chargeXX.paramcalc.pref + "_charge_" + (chargeXX.paramcalc.calc_yy - 2000).ToString("00") + ":lnk_charge_" + chargeXX.paramcalc.calc_mm.ToString("00");
            string sKernelDBName = chargeXX.paramcalc.pref + "_kernel:";
#endif

            IDataReader reader;
            ret = ExecRead(conn_db, out reader,
                " Select month_, dbname, dbserver, year_ From " + tab + " b," +
                        sKernelDBName.Trim() + "s_baselist a, t_selkvar c, " +
                        sKernelDBName.Trim() + "logtodb ld, " +
                        sKernelDBName.Trim() + "s_logicdblist sl " +
                " Where a.yearr = b.year_ and b.nzp_kvar = c.nzp_kvar " +
                "   and ld.nzp_bl = a.nzp_bl and sl.nzp_ldb = ld.nzp_ldb and sl.ldbname = 'RT' " +
                "   and idtype=1 " +
                " Group by 1,2,3,4 Order by 2,1  "
                , true);

            if (!ret.result)
            {
                DropTempTablesNedo(conn_db);
                return false;
            }
            try
            {
                while (reader.Read())
                {
                    if (reader["dbname"] != DBNull.Value)
                    {
                        Int16 year_ = (Int16)reader["year_"];
                        if (year_ < Points.BeginCalc.year_)
                            continue;

                        string dbname = (string)reader["dbname"];
                        Int16 mn = (Int16)reader["month_"];

#if PG
                        dbname = dbname.Trim() + ".charge_" + mn.ToString("00");
                        //string sMDYpref = "public.";    
#else
                        dbname = dbname.Trim() + ":charge_" + mn.ToString("00");
                        //string sMDYpref = "";    
#endif
                        ret = ExecSQL(conn_db,
                            " Insert into t_prev_ch (nzp_area,nzp_geu,nzp_dom,nzp_serv,nzp_supp,sum_tarif, sum_tarif_f, sum_nedop) " +
                            " Select nzp_area,nzp_geu,k.nzp_dom,c.nzp_serv, nzp_supp," +
                                  " sum(sum_tarif)-sum(sum_tarif_p), " +
                                  " sum(sum_tarif_f)-sum(sum_tarif_f_p), " +
                                  " sum(sum_nedop)-sum(sum_nedop_p)  " +
                            " From " + dbname + " c, t_selkvar k " +
                            " Where c.dat_charge= " + MDY(chargeXX.paramcalc.calc_mm, 28, chargeXX.paramcalc.calc_yy) +
                            "   and  c.nzp_kvar=k.nzp_kvar and nzp_serv>1 " +
                            " Group by 1,2,3,4,5 "
                            , true);
                        if (!ret.result)
                        {
                            reader.Close();
                            DropTempTablesCharge(conn_db);
                            return false;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                reader.Close();
                ret.result = false;
                ret.text = ex.Message;

                DropTempTablesNedo(conn_db);
                return false;
            }
            reader.Close();

            ret = ExecSQL(conn_db,
                " Insert into t_ins_ch (nzp_wp, nzp_area,nzp_geu,nzp_dom,nzp_serv,nzp_supp, sum_tarif_p, sum_tarif_f_p, sum_nedop_p) " +
                " Select " + chargeXX.paramcalc.nzp_wp + ",nzp_area,nzp_geu,nzp_dom,nzp_serv,nzp_supp, sum(sum_tarif), sum(sum_tarif_f), " +
                " sum(sum_nedop) " +
                " From t_prev_ch group by 1,2,3,4,5,6 "
                , true);
            if (!ret.result)
            {
                DropTempTablesCharge(conn_db);
                return false;
            }
#if PG
            ExecSQL(conn_db, " analyze t_ins_ch ", true);
#else
            ExecSQL(conn_db, " Update statistics for table t_ins_ch ", true);
#endif
            //ExecSQL(conn_db, " Create unique index ix_t_ins_ch2 on t_ins_ch (nzp_dom,nzp_area,nzp_geu,nzp_serv,nzp_supp) ", true);
            ExecByStep(conn_db, "t_ins_ch", "nzp_key",
                " Update t_ins_ch " +
                " Set kod = 1 " +
                " Where 0 < ( Select count(*) From " + chargeXX.report_xx_dom + " ch " +
                            " Where t_ins_ch.nzp_dom  = ch.nzp_dom " +
                            "   and t_ins_ch.nzp_area = ch.nzp_area " +
                            "   and t_ins_ch.nzp_geu  = ch.nzp_geu " +
                            "   and t_ins_ch.nzp_serv = ch.nzp_serv " +
                            "   and t_ins_ch.nzp_supp = ch.nzp_supp " +
                            "   and t_ins_ch.nzp_wp   = ch.nzp_wp " +
                            "   and " + chargeXX.paramcalc.where_z + chargeXX.where_report +
                            " ) "
                , 10000, " ", out ret);
            if (!ret.result)
            {
                DropTempTablesCharge(conn_db);
                return false;
            }

            //вставка недостающих строк в report_xx_dom
            ExecByStep(conn_db, "t_ins_ch", "nzp_key",
                " Insert into " + chargeXX.report_xx_dom +
                   " ( nzp_wp,year_,month_,nzp_area,nzp_geu,nzp_dom,nzp_supp,nzp_serv," +
                   " rsum_tarif, sum_tarif, sum_tarif_f, sum_tarif_p, rsum_lgota, sum_lgota, sum_real, " +
                     " sum_izm, old_money, sum_money, money_to, money_from, money_del, reval, real_charge, " +
                     " sum_nedop, sum_nedop_p, sum_pere, sum_insaldo, " +
                     " sum_outsaldo, old_charge, sum_nach, sum_insaldo_k, sum_insaldo_d, sum_outsaldo_k, sum_outsaldo_d, sum_pere_d ) " +
                " Select nzp_wp," + chargeXX.paramcalc.calc_yy + "," + chargeXX.paramcalc.calc_mm + ", nzp_area,nzp_geu,nzp_dom,nzp_supp,nzp_serv, " +
                       " 0, 0, 0, 0, 0, 0, 0,  0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0  " +
                " From t_ins_ch Where kod = 0 "
                , 50000, " group by 1,2,3,4,5,6,7,8 ", out ret);
            if (!ret.result)
            {
                DropTempTablesCharge(conn_db);
                return false;
            }
            UpdateStatisticsFin(chargeXX.paramcalc.calc_yy, out ret);

            ExecByStep(conn_db, "t_ins_ch", "nzp_key",
                " Update t_ins_ch " +
                " Set kod = 2 " +
                " Where 0 < ( Select count(*) From " + chargeXX.report_xx + " ch " +
                            " Where t_ins_ch.nzp_area = ch.nzp_area " +
                            "   and t_ins_ch.nzp_geu  = ch.nzp_geu " +
                            "   and t_ins_ch.nzp_serv = ch.nzp_serv " +
                            "   and t_ins_ch.nzp_supp = ch.nzp_supp " +
                            "   and t_ins_ch.nzp_wp   = ch.nzp_wp " +
                              chargeXX.where_report +
                            " ) "
                , 10000, " ", out ret);
            if (!ret.result)
            {
                DropTempTablesCharge(conn_db);
                return false;
            }

            //вставка недостающих строк в report_xx
            ExecByStep(conn_db, "t_ins_ch", "nzp_key",
                " Insert into " + chargeXX.report_xx +
                   " ( nzp_wp,year_,month_,nzp_area,nzp_geu,nzp_supp,nzp_serv, rsum_tarif, sum_tarif, sum_tarif_f, sum_tarif_p, rsum_lgota, sum_lgota, sum_real, " +
                     " old_money, sum_money, money_to, money_from, money_del, reval, real_charge, sum_nedop, sum_nedop_p, sum_pere, sum_insaldo, " +
                     " sum_outsaldo, old_charge, sum_nach, sum_insaldo_k, sum_insaldo_d, sum_outsaldo_k, sum_outsaldo_d, sum_pere_d, sum_izm ) " +
                " Select nzp_wp," + chargeXX.paramcalc.calc_yy + "," + chargeXX.paramcalc.calc_mm + ", nzp_area,nzp_geu,nzp_supp,nzp_serv, " +
                       " 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 " +
                " From t_ins_ch Where kod <> 2 "
                , 50000, " group by 1,2,3,4,5,6,7 ", out ret);
            if (!ret.result)
            {
                DropTempTablesCharge(conn_db);
                return false;
            }
            UpdateStatisticsFin(chargeXX.paramcalc.calc_yy, out ret);


            //собираем суммы report_xx_dom
            string ssqlfirst = " Update " + chargeXX.report_xx_dom + " Set";
            string ssqlmiddle = " From t_ins_ch gk " +
                                    " Where gk.nzp_dom  = " + chargeXX.report_xx_dom + ".nzp_dom " +
                                    "   and gk.nzp_area = " + chargeXX.report_xx_dom + ".nzp_area " +
                                    "   and gk.nzp_geu  = " + chargeXX.report_xx_dom + ".nzp_geu " +
                                    "   and gk.nzp_serv = " + chargeXX.report_xx_dom + ".nzp_serv " +
                                    "   and gk.nzp_supp = " + chargeXX.report_xx_dom + ".nzp_supp " +
                                    "   and gk.nzp_wp   = " + chargeXX.report_xx_dom + ".nzp_wp ) ";
            string ssqllast = " Where 0 < ( Select count(*) " +
                                    " From t_ins_ch gk " +
                                    " Where gk.nzp_dom  = " + chargeXX.report_xx_dom + ".nzp_dom " +
                                    "   and gk.nzp_area = " + chargeXX.report_xx_dom + ".nzp_area " +
                                    "   and gk.nzp_geu  = " + chargeXX.report_xx_dom + ".nzp_geu " +
                                    "   and gk.nzp_serv = " + chargeXX.report_xx_dom + ".nzp_serv " +
                                    "   and gk.nzp_supp = " + chargeXX.report_xx_dom + ".nzp_supp " +
                                    "   and gk.nzp_wp   = " + chargeXX.report_xx_dom + ".nzp_wp ) " +
                                " and " + chargeXX.paramcalc.where_z + chargeXX.where_report;
#if PG
            ssqllast = ssqlmiddle.Trim() + ssqllast.Trim();
            ret = ExecSQL(conn_db, ssqlfirst.Trim() + " rsum_tarif = (Select sum(rsum_tarif) "         + ssqllast.Trim(), true);
            ret = ExecSQL(conn_db, ssqlfirst.Trim() + " sum_tarif = (Select sum(sum_tarif) "           + ssqllast.Trim(), true);
            ret = ExecSQL(conn_db, ssqlfirst.Trim() + " sum_tarif_f = (Select sum(sum_tarif_f) "       + ssqllast.Trim(), true);
            ret = ExecSQL(conn_db, ssqlfirst.Trim() + " sum_tarif_p = (Select sum(sum_tarif_p) "       + ssqllast.Trim(), true);
            ret = ExecSQL(conn_db, ssqlfirst.Trim() + " rsum_lgota = (Select sum(rsum_lgota) "         + ssqllast.Trim(), true);
            ret = ExecSQL(conn_db, ssqlfirst.Trim() + " sum_lgota = (Select sum(sum_lgota) "           + ssqllast.Trim(), true);
            ret = ExecSQL(conn_db, ssqlfirst.Trim() + " sum_real = (Select sum(sum_real) "             + ssqllast.Trim(), true);
            ret = ExecSQL(conn_db, ssqlfirst.Trim() + " old_money = (Select sum(old_money) "           + ssqllast.Trim(), true);
            ret = ExecSQL(conn_db, ssqlfirst.Trim() + " sum_money = (Select sum(sum_money) "           + ssqllast.Trim(), true);
            ret = ExecSQL(conn_db, ssqlfirst.Trim() + " money_to = (Select sum(money_to) "             + ssqllast.Trim(), true);
            ret = ExecSQL(conn_db, ssqlfirst.Trim() + " money_from = (Select sum(money_from) "         + ssqllast.Trim(), true);
            ret = ExecSQL(conn_db, ssqlfirst.Trim() + " money_del = (Select sum(money_del) "           + ssqllast.Trim(), true);
            ret = ExecSQL(conn_db, ssqlfirst.Trim() + " reval = (Select sum(reval) "                   + ssqllast.Trim(), true);
            ret = ExecSQL(conn_db, ssqlfirst.Trim() + " real_charge = (Select sum(real_charge) "       + ssqllast.Trim(), true);
            ret = ExecSQL(conn_db, ssqlfirst.Trim() + " sum_nedop = (Select sum(sum_nedop) "           + ssqllast.Trim(), true);
            ret = ExecSQL(conn_db, ssqlfirst.Trim() + " sum_nedop_p = (Select sum(sum_nedop_p) "       + ssqllast.Trim(), true);
            ret = ExecSQL(conn_db, ssqlfirst.Trim() + " sum_pere = (Select sum(sum_pere) "             + ssqllast.Trim(), true);
            ret = ExecSQL(conn_db, ssqlfirst.Trim() + " sum_insaldo = (Select sum(sum_insaldo) "       + ssqllast.Trim(), true);
            ret = ExecSQL(conn_db, ssqlfirst.Trim() + " sum_outsaldo = (Select sum(sum_outsaldo) "     + ssqllast.Trim(), true);
            ret = ExecSQL(conn_db, ssqlfirst.Trim() + " old_charge = (Select sum(old_charge) "         + ssqllast.Trim(), true);
            ret = ExecSQL(conn_db, ssqlfirst.Trim() + " sum_nach = (Select sum(sum_nach) "             + ssqllast.Trim(), true);
            ret = ExecSQL(conn_db, ssqlfirst.Trim() + " sum_insaldo_k = (Select sum(sum_insaldo_k) "   + ssqllast.Trim(), true);
            ret = ExecSQL(conn_db, ssqlfirst.Trim() + " sum_insaldo_d = (Select sum(sum_insaldo_d) "   + ssqllast.Trim(), true);
            ret = ExecSQL(conn_db, ssqlfirst.Trim() + " sum_outsaldo_k = (Select sum(sum_outsaldo_k) " + ssqllast.Trim(), true);
            ret = ExecSQL(conn_db, ssqlfirst.Trim() + " sum_outsaldo_d = (Select sum(sum_outsaldo_d) " + ssqllast.Trim(), true);
            ret = ExecSQL(conn_db, ssqlfirst.Trim() + " sum_pere_d = (Select sum(sum_pere_d) "         + ssqllast.Trim(), true);
            ret = ExecSQL(conn_db, ssqlfirst.Trim() + " reval_k = (Select sum(reval_k) "               + ssqllast.Trim(), true);
            ret = ExecSQL(conn_db, ssqlfirst.Trim() + " reval_d = (Select sum(reval_d) "               + ssqllast.Trim(), true);
            ret = ExecSQL(conn_db, ssqlfirst.Trim() + " real_charge_k = (Select sum(real_charge_k) "   + ssqllast.Trim(), true);
            ret = ExecSQL(conn_db, ssqlfirst.Trim() + " real_charge_d = (Select sum(real_charge_d) "   + ssqllast.Trim(), true);
            ret = ExecSQL(conn_db, ssqlfirst.Trim() + " sum_izm = (Select sum(sum_izm) "               + ssqllast.Trim(), true);

#else
            ssqllast = ssqlmiddle.Trim() + ") " + ssqllast.Trim();
            ret = ExecSQL(conn_db, ssqlfirst.Trim() +
                   " ( rsum_tarif, sum_tarif, sum_tarif_f, sum_tarif_p, rsum_lgota, sum_lgota, sum_real, " +
                         " old_money, sum_money, money_to, money_from, money_del, reval, real_charge, sum_nedop, sum_nedop_p, sum_pere, sum_insaldo, " +
                         " sum_outsaldo, old_charge, sum_nach, sum_insaldo_k, sum_insaldo_d, sum_outsaldo_k, sum_outsaldo_d, sum_pere_d, " +
                         " reval_k, reval_d, real_charge_k, real_charge_d, sum_izm ) = (( " +
                         " Select sum(rsum_tarif), sum(sum_tarif), sum(sum_tarif_f), sum(sum_tarif_p), sum(rsum_lgota), sum(sum_lgota), sum(sum_real), " +
                                " sum(old_money), sum(sum_money), sum(money_to), sum(money_from), sum(money_del), sum(reval), sum(real_charge), " +
                                " sum(sum_nedop), sum(sum_nedop_p), sum(sum_pere), sum(sum_insaldo), sum(sum_outsaldo), sum(old_charge), sum(sum_nach), " +
                                " sum(sum_insaldo_k), sum(sum_insaldo_d), sum(sum_outsaldo_k), sum(sum_outsaldo_d), sum(sum_pere_d)," +
                                " sum(reval_k), sum(reval_d), sum(real_charge_k), sum(real_charge_d), sum(sum_izm) " +
            ssqllast.Trim(), true);
#endif
            if (!ret.result)
            {
                DropTempTablesCharge(conn_db);
                return false;
            }


            //обновим суммы report_xx
            //но возьмем суммы из report_xx_dom !!! 
            ExecSQL(conn_db, " Drop table t_prev_ch ", false);

            ssqlfirst =
                " Select " + chargeXX.paramcalc.nzp_wp + " as nzp_wp, nzp_area, nzp_geu, nzp_supp,nzp_serv, " +
                       " sum(rsum_tarif) rsum_tarif,sum(sum_tarif) sum_tarif, sum(sum_tarif_f) sum_tarif_f,sum(sum_tarif_p) sum_tarif_p,sum(rsum_lgota) rsum_lgota," +
                       " sum(sum_lgota) sum_lgota,sum(sum_real) sum_real,sum(old_money) old_money, " +
                       " sum(sum_money) sum_money,sum(money_to) money_to,sum(money_from) money_from,sum(money_del) money_del,sum(reval) reval," +
                       " sum(real_charge) real_charge, sum(sum_nedop) as sum_nedop, sum(sum_nedop_p) as sum_nedop_p, sum(sum_pere) sum_pere, " +
                       " sum(sum_insaldo) sum_insaldo,sum(sum_outsaldo) sum_outsaldo, " +
                       " sum(old_charge) old_charge, " +
                       " sum(sum_nach) sum_nach,sum(sum_insaldo_k) sum_insaldo_k, " +
                       " sum(sum_insaldo_d) sum_insaldo_d,sum(sum_outsaldo_k) sum_outsaldo_k, " +
                       " sum(sum_outsaldo_d) sum_outsaldo_d,sum(sum_pere_d) sum_pere_d,  sum(reval_k) as reval_k, " +
                       " sum(reval_d) as reval_d, sum(real_charge_k) as real_charge_k, " +
                       " sum(real_charge_d) as real_charge_d,sum(sum_izm) as sum_izm ";
#if PG
            ssqlfirst = ssqlfirst.Trim() +
                " Into temp t_prev_ch " +
                " From " + chargeXX.report_xx_dom +
                " Where 1 = 1 " + chargeXX.where_report +
                " Group by 1,2,3,4,5 ";
#else
            ssqlfirst = ssqlfirst.Trim() +
                " From " + chargeXX.report_xx_dom +
                " Where 1 = 1 " + chargeXX.where_report +
                " Group by 1,2,3,4,5 " +
                " Into temp t_prev_ch With no log ";
#endif
            ret = ExecSQL(conn_db, ssqlfirst, true);
            if (!ret.result)
            {
                DropTempTablesCharge(conn_db);
                return false;
            }
            ExecSQL(conn_db, " Create unique index ix_t_prev_ch on t_prev_ch (nzp_area,nzp_geu,nzp_serv,nzp_supp,nzp_wp) ", true);
#if PG
            ExecSQL(conn_db, " analyze t_prev_ch ", true);
#else
            ExecSQL(conn_db, " Update statistics for table t_prev_ch ", true);
#endif

            ssqlfirst = " Update " + chargeXX.report_xx + " Set";
            ssqlmiddle =
                        " From t_prev_ch gk " +
                        " Where gk.nzp_area = " + chargeXX.report_xx + ".nzp_area " +
                        "   and gk.nzp_geu  = " + chargeXX.report_xx + ".nzp_geu " +
                        "   and gk.nzp_serv = " + chargeXX.report_xx + ".nzp_serv " +
                        "   and gk.nzp_supp = " + chargeXX.report_xx + ".nzp_supp " +
                        "   and gk.nzp_wp   = " + chargeXX.report_xx + ".nzp_wp ) ";
            ssqllast =
                        " Where 0 < ( Select count(*) " +
                                    " From t_prev_ch gk " +
                                    " Where gk.nzp_area = " + chargeXX.report_xx + ".nzp_area " +
                                    "   and gk.nzp_geu  = " + chargeXX.report_xx + ".nzp_geu " +
                                    "   and gk.nzp_serv = " + chargeXX.report_xx + ".nzp_serv " +
                                    "   and gk.nzp_supp = " + chargeXX.report_xx + ".nzp_supp " +
                                    "   and gk.nzp_wp   = " + chargeXX.report_xx + ".nzp_wp ) " +
                                   chargeXX.where_report;

#if PG

            ssqllast = ssqlmiddle.Trim() + ssqllast.Trim();
            ret = ExecSQL(conn_db, ssqlfirst.Trim() + " rsum_tarif = (Select sum(rsum_tarif) " + ssqllast.Trim(), true);
            ret = ExecSQL(conn_db, ssqlfirst.Trim() + " sum_tarif = (Select sum(sum_tarif) " + ssqllast.Trim(), true);
            ret = ExecSQL(conn_db, ssqlfirst.Trim() + " sum_tarif_f = (Select sum(sum_tarif_f) " + ssqllast.Trim(), true);
            ret = ExecSQL(conn_db, ssqlfirst.Trim() + " sum_tarif_p = (Select sum(sum_tarif_p) " + ssqllast.Trim(), true);
            ret = ExecSQL(conn_db, ssqlfirst.Trim() + " rsum_lgota = (Select sum(rsum_lgota) " + ssqllast.Trim(), true);
            ret = ExecSQL(conn_db, ssqlfirst.Trim() + " sum_lgota = (Select sum(sum_lgota) " + ssqllast.Trim(), true);
            ret = ExecSQL(conn_db, ssqlfirst.Trim() + " sum_real = (Select sum(sum_real) " + ssqllast.Trim(), true);
            ret = ExecSQL(conn_db, ssqlfirst.Trim() + " old_money = (Select sum(old_money) " + ssqllast.Trim(), true);
            ret = ExecSQL(conn_db, ssqlfirst.Trim() + " sum_money = (Select sum(sum_money) " + ssqllast.Trim(), true);
            ret = ExecSQL(conn_db, ssqlfirst.Trim() + " money_to = (Select sum(money_to) " + ssqllast.Trim(), true);
            ret = ExecSQL(conn_db, ssqlfirst.Trim() + " money_from = (Select sum(money_from) " + ssqllast.Trim(), true);
            ret = ExecSQL(conn_db, ssqlfirst.Trim() + " money_del = (Select sum(money_del) " + ssqllast.Trim(), true);
            ret = ExecSQL(conn_db, ssqlfirst.Trim() + " reval = (Select sum(reval) " + ssqllast.Trim(), true);
            ret = ExecSQL(conn_db, ssqlfirst.Trim() + " real_charge = (Select sum(real_charge) " + ssqllast.Trim(), true);
            ret = ExecSQL(conn_db, ssqlfirst.Trim() + " sum_nedop = (Select sum(sum_nedop) " + ssqllast.Trim(), true);
            ret = ExecSQL(conn_db, ssqlfirst.Trim() + " sum_nedop_p = (Select sum(sum_nedop_p) " + ssqllast.Trim(), true);
            ret = ExecSQL(conn_db, ssqlfirst.Trim() + " sum_pere = (Select sum(sum_pere) " + ssqllast.Trim(), true);
            ret = ExecSQL(conn_db, ssqlfirst.Trim() + " sum_insaldo = (Select sum(sum_insaldo) " + ssqllast.Trim(), true);
            ret = ExecSQL(conn_db, ssqlfirst.Trim() + " sum_outsaldo = (Select sum(sum_outsaldo) " + ssqllast.Trim(), true);
            ret = ExecSQL(conn_db, ssqlfirst.Trim() + " old_charge = (Select sum(old_charge) " + ssqllast.Trim(), true);
            ret = ExecSQL(conn_db, ssqlfirst.Trim() + " sum_nach = (Select sum(sum_nach) " + ssqllast.Trim(), true);
            ret = ExecSQL(conn_db, ssqlfirst.Trim() + " sum_insaldo_k = (Select sum(sum_insaldo_k) " + ssqllast.Trim(), true);
            ret = ExecSQL(conn_db, ssqlfirst.Trim() + " sum_insaldo_d = (Select sum(sum_insaldo_d) " + ssqllast.Trim(), true);
            ret = ExecSQL(conn_db, ssqlfirst.Trim() + " sum_outsaldo_k = (Select sum(sum_outsaldo_k) " + ssqllast.Trim(), true);
            ret = ExecSQL(conn_db, ssqlfirst.Trim() + " sum_outsaldo_d = (Select sum(sum_outsaldo_d) " + ssqllast.Trim(), true);
            ret = ExecSQL(conn_db, ssqlfirst.Trim() + " sum_pere_d = (Select sum(sum_pere_d) " + ssqllast.Trim(), true);
            ret = ExecSQL(conn_db, ssqlfirst.Trim() + " reval_k = (Select sum(reval_k) " + ssqllast.Trim(), true);
            ret = ExecSQL(conn_db, ssqlfirst.Trim() + " reval_d = (Select sum(reval_d) " + ssqllast.Trim(), true);
            ret = ExecSQL(conn_db, ssqlfirst.Trim() + " real_charge_k = (Select sum(real_charge_k) " + ssqllast.Trim(), true);
            ret = ExecSQL(conn_db, ssqlfirst.Trim() + " real_charge_d = (Select sum(real_charge_d) " + ssqllast.Trim(), true);
            ret = ExecSQL(conn_db, ssqlfirst.Trim() + " sum_izm = (Select sum(sum_izm) " + ssqllast.Trim(), true);
            
#else
            ssqlfirst = ssqlfirst.Trim() +
                   " ( rsum_tarif, sum_tarif, sum_tarif_f, sum_tarif_p, rsum_lgota, sum_lgota, sum_real, old_money, sum_money, money_to, money_from, money_del, reval, real_charge, " +
                     " sum_nedop, sum_nedop_p, sum_pere, sum_insaldo, sum_outsaldo, old_charge, sum_nach, sum_insaldo_k, sum_insaldo_d, sum_outsaldo_k, " +
                     " sum_outsaldo_d, sum_pere_d, reval_k, reval_d, real_charge_k, real_charge_d, sum_izm ) = (( " +
                     " Select sum(rsum_tarif) rsum_tarif,sum(sum_tarif) sum_tarif,sum(sum_tarif_f) sum_tarif_f,sum(sum_tarif_p) sum_tarif_p,sum(rsum_lgota) rsum_lgota," +
                            " sum(sum_lgota) sum_lgota,sum(sum_real) sum_real,sum(old_money) old_money, " +
                            " sum(sum_money) sum_money,sum(money_to) money_to,sum(money_from) money_from,sum(money_del) money_del,sum(reval) reval," +
                            " sum(real_charge) real_charge, sum(sum_nedop) as sum_nedop, sum(sum_nedop_p) as sum_nedop_p, sum(sum_pere) sum_pere, " +
                            " sum(sum_insaldo) sum_insaldo,sum(sum_outsaldo) sum_outsaldo, " +
                            " sum(old_charge) old_charge, " +
                            " sum(sum_nach) sum_nach,sum(sum_insaldo_k) sum_insaldo_k, " +
                            " sum(sum_insaldo_d) sum_insaldo_d,sum(sum_outsaldo_k) sum_outsaldo_k, " +
                            " sum(sum_outsaldo_d) sum_outsaldo_d,sum(sum_pere_d) sum_pere_d, sum(reval_k), sum(reval_d), " +
                            " sum(real_charge_k), sum(real_charge_d), sum(sum_izm) as sum_izm " +
                   ssqlmiddle.Trim() + ") " + ssqllast.Trim();
            ret = ExecSQL(conn_db, ssqlfirst, true);
#endif
            if (!ret.result)
            {
                DropTempTablesCharge(conn_db);
                return false;
            }


            DropTempTablesCharge(conn_db);
            return true;
        }

        //--------------------------------------------------------------------------------
        void UpdateStatisticsFin(int yy, out Returns ret)
        //--------------------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            //string conn_kernel = Points.GetConnByPref(paramcalc.pref);
            //IDbConnection conn_db2 = GetConnection(conn_kernel);
            IDbConnection conn_db2 = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(conn_db2, true);
            if (!ret.result)
            {
                return;
            }
            string sDB = Points.Pref + "_fin_" + (yy - 2000).ToString("00");
#if PG
            ret = ExecSQL(conn_db2, " analyze " + sDB.Trim() + ".fn_ukrgudom ", true);
            if (!ret.result)
            {
                conn_db2.Close();
                return;
            }
            ret = ExecSQL(conn_db2, " analyze " + sDB.Trim() + ".fn_ukrgucharge ", true);
            if (!ret.result)
            {
                conn_db2.Close();
                return;
            }
#else
            ret = ExecSQL(conn_db2, " Database " + sDB, true);
            if (!ret.result)
            {
                conn_db2.Close();
                return;
            }
            ret = ExecSQL(conn_db2, " update statistics for table fn_ukrgudom ", true);
            if (!ret.result)
            {
                conn_db2.Close();
                return;
            }
            ret = ExecSQL(conn_db2, " update statistics for table fn_ukrgucharge ", true);
            if (!ret.result)
            {
                conn_db2.Close();
                return;
            }
#endif

            conn_db2.Close();
        }
    }
}
