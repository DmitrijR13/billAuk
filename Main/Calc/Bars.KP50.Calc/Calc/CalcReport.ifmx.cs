using System;
using System.Linq;
using System.Text;
using System.Data;
using System.Collections.Generic;
using System.Globalization;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;
using STCLINE.KP50.IFMX.Kernel.source.CommonType;

namespace STCLINE.KP50.DataBase
{
    public partial class DbCalcCharge : DataBaseHead
    {
        //--------------------------------------------------------------------------------
        public void AlterTableReport(out Returns ret)
        //--------------------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            if (!Points.isFinances) return;

            for (int yy = 2011; yy <= Points.CalcMonth.year_; yy++)
            {
                CalcTypes.ParamCalc paramcalc = new CalcTypes.ParamCalc(0, 0, "", yy, 1, yy, 1);
                CalcTypes.ChargeXX chargeXX = new CalcTypes.ChargeXX(paramcalc);

                AlterTableReport(chargeXX, out ret);
            }
        }
        //--------------------------------------------------------------------------------
        void AlterTableReport(CalcTypes.ChargeXX chargeXX, out Returns ret)
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
        public bool CalcReportXX(IDbConnection conn_db, CalcTypes.ParamCalc paramcalc, out Returns ret)
        //-----------------------------------------------------------------------------
        {
            DbCalcReportStat dbCalcReportStat = new DbCalcReportStat(conn_db);
            ret = dbCalcReportStat.CalcReportXX(paramcalc.calc_mm, paramcalc.calc_yy,
                paramcalc.pref, paramcalc.nzp_wp, paramcalc.nzp_dom);
            return ret.result; 
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
