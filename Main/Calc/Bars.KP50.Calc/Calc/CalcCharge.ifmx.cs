using System;
using System.Linq;
using System.Text;
using System.Data;
using System.Collections.Generic;
using System.Globalization;
using Bars.KP50.Utils;
using STCLINE.KP50.Global;
using STCLINE.KP50.IFMX.Kernel.source.CommonType;
using STCLINE.KP50.Interfaces;

namespace STCLINE.KP50.DataBase
{
    public partial class DbCalcCharge : DataBaseHead
    {
        #region Удаление временных таблиц для ChargeXX
        //--------------------------------------------------------------------------------
        void DropTempTablesCharge(IDbConnection conn_db)
        //--------------------------------------------------------------------------------
        {
#if PG
            ExecSQL(conn_db, " Drop table ishod_charge ", false);
#else
            ExecSQL(conn_db, " Drop table ishod_charge ", false);
#endif
            ExecSQL(conn_db, " Drop table reval_charge ", false);

            ExecSQL(conn_db, " Drop table t_delmin ", false);
            ExecSQL(conn_db, " Drop table t_delplu ", false);
            ExecSQL(conn_db, " Drop table t_itog ", false);
            ExecSQL(conn_db, " Drop table t_perek ", false);
            ExecSQL(conn_db, " Drop table t_fn_supp ", false);
            ExecSQL(conn_db, " Drop table t_from_supp ", false);
            ExecSQL(conn_db, " Drop table t_del_supp ", false);
            ExecSQL(conn_db, " Drop table t_prev_ch ", false);
            ExecSQL(conn_db, " Drop table t_calc_gku ", false);
            ExecSQL(conn_db, " Drop table t_ins_ch ", false);
            ExecSQL(conn_db, " Drop table t_ins_ch1 ", false);
            ExecSQL(conn_db, " Drop table t_nedo ", false);
            ExecSQL(conn_db, " Drop table t_lnk_reval ", false);
            ExecSQL(conn_db, " Drop table ttt_charge ", false);
            ExecSQL(conn_db, " Drop table t_fn_suppall ", false);
            ExecSQL(conn_db, " Drop table t_nedo_xx ", false);
            ExecSQL(conn_db, " Drop table t_reval_ku ", false);
        }
        #endregion Удаление временных таблиц для ChargeXX

        #region Создание таблицы начислений и других вспомогательные таблицы
        /// <summary>
        /// Создает таблицу начислений и другие вспомогательные таблицы
        /// </summary>
        /// <param name="conn_db2"></param>
        /// <param name="chargeXX">структура для определения имен таблиц для начислений</param>
        /// <param name="del_reval_xx">признак вызова в режиме перерасчета</param>
        /// <param name="ret"></param>
        void CreateChargeXX(IDbConnection conn_db2, CalcTypes.ChargeXX chargeXX, bool del_reval_xx, out Returns ret)
        {
            ret = Utils.InitReturns();

            //*****************************************************************
            //lnk_charge_xx
            //*****************************************************************
            if (TempTableInWebCashe(conn_db2, chargeXX.lnk_charge_xx))
            {
                if (del_reval_xx)
                {
                    ret = ExecSQL(conn_db2,
                            " Delete From " + chargeXX.lnk_charge_xx +
                            " Where " + chargeXX.where_kvar  //nzp_kvar in ( Select nzp_kvar From t_selkvar) "
                        , true);
                    if (!ret.result) { return; }

                    UpdateStatistics(false, chargeXX.paramcalc, chargeXX.lnk_tab, out ret);
                    if (!ret.result) { return; }
                }
            }
            else
            {
                string conn_kernel = Points.GetConnByPref(chargeXX.paramcalc.pref);
                IDbConnection conn_db = GetConnection(conn_kernel);

                ret = OpenDb(conn_db, true);
                if (!ret.result) { return; }

                string snmbd = chargeXX.paramcalc.pref + "_charge_" + (chargeXX.paramcalc.cur_yy - 2000).ToString("00");
                ret = ExecSQL(conn_db,
#if PG
 " set search_path to '" + snmbd + "'"
#else
                    " database " + snmbd
#endif
, true);
                if (!ret.result) { conn_db.Close(); return; }

                ret = ExecSQL(conn_db,
                    " Create table " + tbluser + chargeXX.lnk_tab +
                    "  (  nzp_kvar integer, " +
                    "     month_ integer, " +
                    "     year_ integer ) "
                    , true);
                if (!ret.result) { conn_db.Close(); return; }

                ret = ExecSQL(conn_db,
                    " Create index " + tbluser + "ilnk1_" +
                    chargeXX.paramcalc.alias + "_" + chargeXX.paramcalc.cur_mm.ToString("00") +
                    " on " + chargeXX.lnk_tab + " (nzp_kvar,year_,month_) ", true);
                if (!ret.result) { conn_db.Close(); return; }

                ExecSQL(conn_db, sUpdStat + " " + chargeXX.lnk_tab, true);

                conn_db.Close();
            }

            //*****************************************************************
            //kvar_calc_xx
            //*****************************************************************
            if (!TempTableInWebCashe(conn_db2, chargeXX.kvar_calc_xx))
            {
                string conn_kernel = Points.GetConnByPref(chargeXX.paramcalc.pref);
                IDbConnection conn_db = GetConnection(conn_kernel);

                ret = OpenDb(conn_db, true);
                if (!ret.result) { return; }

                string snmbd = chargeXX.paramcalc.pref + "_charge_" + (chargeXX.paramcalc.calc_yy - 2000).ToString("00");
                if (DBManager.SchemaExists(snmbd, conn_db))
                {
                    ret = ExecSQL(conn_db,
#if PG
 " set search_path to '" + snmbd + "'"
#else
                        " database " + snmbd
#endif
, true);
                    if (!ret.result) { return; }

                    ret = ExecSQL(conn_db,
                        " Create table " + tbluser + chargeXX.kvar_calc_tab +
                        "  (  nzp_kvar integer, " +
                        "     num_ls integer, " +
                        "     dat_calc date) "
                        , true);
                    if (!ret.result) { conn_db.Close(); return; }

                    ret = ExecSQL(conn_db,
                        " Create index " + tbluser + "ikct_"
                        + chargeXX.paramcalc.alias + "_" + chargeXX.paramcalc.calc_mm.ToString("00") +
                        " on " + chargeXX.kvar_calc_tab + " (nzp_kvar,num_ls,dat_calc) ", true);
                    if (!ret.result) { conn_db.Close(); return; }

                    ExecSQL(conn_db, sUpdStat + " " + chargeXX.kvar_calc_tab, true);

                    conn_db.Close();
                }
            }

            //*****************************************************************
            //reval_xx
            //*****************************************************************
            if (TempTableInWebCashe(conn_db2, chargeXX.reval_xx))
            {
                if (del_reval_xx)
                {
                    string p_where = " Where 1 = 1 ";
                    if (chargeXX.paramcalc.b_reval) //удалить по услуге
                        p_where = " Where 0 < ( Select count(*) From t_mustcalc b " +
                                              " Where " + chargeXX.reval_xx + ".nzp_kvar = b.nzp_kvar " +
                                              "   and " + chargeXX.reval_xx + ".nzp_serv = ( case when b.nzp_serv > 1 then b.nzp_serv else " + chargeXX.reval_xx + ".nzp_serv end ) ) ";

                    if (chargeXX.paramcalc.nzp_kvar > 0 || chargeXX.paramcalc.nzp_dom > 0)
                    {
                        ret = ExecSQL(conn_db2,
                            " Delete From " + chargeXX.reval_xx +
                                  p_where +
                                  " and " + chargeXX.where_kvar
                            , true);
                    }
                    else
                    {
                        ExecByStep(conn_db2, chargeXX.reval_xx, "nzp_reval",
                                " Delete From " + chargeXX.reval_xx +
                                  p_where +
                                  " and " + chargeXX.where_kvar
                                , 100000, " ", out ret);
                    }
                    if (!ret.result) { return; }

                    UpdateStatistics(false, chargeXX.paramcalc, chargeXX.reval_tab, out ret);
                    if (!ret.result) { return; }
                }
            }
            else
            {
                string conn_kernel = Points.GetConnByPref(chargeXX.paramcalc.pref);
                IDbConnection conn_db = GetConnection(conn_kernel);
                //IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
                ret = OpenDb(conn_db, true);
                if (!ret.result) { return; }

                string snmbd = chargeXX.paramcalc.pref + "_charge_" + (chargeXX.paramcalc.cur_yy - 2000).ToString("00");
                ret = ExecSQL(conn_db,
#if PG
 " set search_path to '" + snmbd + "'"
#else
                    " database " + snmbd
#endif
, true);
                if (!ret.result) { conn_db.Close(); return; }

                ret = ExecSQL(conn_db,
                    " Create table " + tbluser + chargeXX.reval_tab +
                    "  ( nzp_reval  serial not null, " +
                    "    nzp_kvar   integer not null, " +
                    "    nzp_serv   integer not null, " +
                    "    nzp_supp   integer not null, " +
                    "    year_      integer not null, " +
                    "    month_     integer not null, " +
                    "    delta1     " + sDecimalType + "(14,2) default 0.00, " +
                    "    delta2     " + sDecimalType + "(14,2) default 0.00, " +
                    "    reval      " + sDecimalType + "(14,2) default 0.00, " +
                    "    tarif        " + sDecimalType + "(14,4) default 0.0000 NOT NULL, " +
                    "    tarif_p      " + sDecimalType + "(14,4) default 0.0000 NOT NULL, " +
                    "    sum_tarif    " + sDecimalType + "(14,2) default 0.00 NOT NULL, " +
                    "    sum_tarif_p  " + sDecimalType + "(14,2) default 0.00 NOT NULL, " +
                    "    sum_nedop    " + sDecimalType + "(14,2) default 0.00 NOT NULL, " +
                    "    sum_nedop_p  " + sDecimalType + "(14,2) default 0.00 NOT NULL, " +
                    "    c_calc       " + sDecimalType + "(14,4) default 0.0000, " +
                    "    c_calc_p     " + sDecimalType + "(14,4) default 0.0000, " +
                    "    c_calcm_p    " + sDecimalType + "(14,4) default 0.0000, " +
                    "    nzp_frm      integer  default 0, " +
                    "    nzp_frm_p    integer  default 0, " +
                    "    type_rsh     integer  default 0, " +
                    "    kod_info     integer  default 0, " +
                    "    month_p      integer  default 0, " +
                    "    year_p       integer  default 0  " +
                    "  ) "
                    , true);
                if (!ret.result) { conn_db.Close(); return; }

                ret = ExecSQL(conn_db,
                    " Create unique index " + tbluser + "irev1_" +
                    chargeXX.paramcalc.alias + "_" + chargeXX.paramcalc.cur_mm.ToString("00") +
                    " on " + chargeXX.reval_tab + " (nzp_reval) ", true);
                if (!ret.result) { conn_db.Close(); return; }

                ret = ExecSQL(conn_db,
                " Create unique index " + tbluser + "irev2_" +
                    chargeXX.paramcalc.alias + "_" + chargeXX.paramcalc.cur_mm.ToString("00") +
                                        " on " + chargeXX.reval_tab + " (nzp_kvar,nzp_serv,nzp_supp,year_,month_) ", true);
                if (!ret.result) { conn_db.Close(); return; }

                ret = ExecSQL(conn_db,
                    " Create index " + tbluser + "irev3_" +
                    chargeXX.paramcalc.alias + "_" + chargeXX.paramcalc.cur_mm.ToString("00") +
                                        " on " + chargeXX.reval_tab + " (year_,month_) ", true);
                if (!ret.result) { conn_db.Close(); return; }

                ret = ExecSQL(conn_db,
                    " Create index " + tbluser + "irev4_" +
                    chargeXX.paramcalc.alias + "_" + chargeXX.paramcalc.cur_mm.ToString("00") +
                                        " on " + chargeXX.reval_tab + " (nzp_kvar,year_,month_) ", true);
                if (!ret.result) { conn_db.Close(); return; }

                ExecSQL(conn_db, sUpdStat + " " + chargeXX.reval_tab, true);

                conn_db.Close();
            }

            //*****************************************************************
            //delta_xx
            //*****************************************************************
            if (TempTableInWebCashe(conn_db2, chargeXX.delta_xx))
            {
                if (del_reval_xx)
                {
                    string p_where = " Where 1 = 1 ";
                    if (chargeXX.paramcalc.b_reval) //удалить по услуге
                        p_where = " Where 0 < ( Select count(*) From t_mustcalc b " +
                                              " Where " + chargeXX.delta_xx + ".nzp_kvar = b.nzp_kvar " +
                                              "   and " + chargeXX.delta_xx + ".nzp_serv = ( case when b.nzp_serv > 1 then b.nzp_serv else " + chargeXX.delta_xx + ".nzp_serv end ) ) ";

                    // выполнять только если текущий расчет
                    if (chargeXX.paramcalc.id_bill_pref.IsEmpty())
                    {
                        if (chargeXX.paramcalc.nzp_kvar > 0)
                        {
                            // если расчет ЛС, то учтенные дельты расходов нужно снимать равными значениями с обратным знаком!
                            ExecSQL(conn_db2, " drop table ttt_ddd ", false);
                            ret = ExecSQL(conn_db2,
                                " Select * " +
#if PG
 " Into temp ttt_ddd " +
#else
                                " " +
#endif
 " From " + chargeXX.delta_xx +
                                p_where +
                                " and stek = 3 and is_used = 1 and " + chargeXX.where_kvar +
#if PG
 " "
#else
                                " Into temp ttt_ddd With no log "
#endif
, true);
                            if (!ret.result) { return; }

                            ret = ExecSQL(conn_db2,
                                " Insert into " + chargeXX.delta_xx +
                                  " (nzp_kvar,nzp_dom,nzp_serv,year_,month_,stek,val1,val2,val3,cnt_stage,kod_info,valm,valm_p,dop87,dop87_p,is_used) " +
                                " Select " +
                                  "  nzp_kvar,nzp_dom,nzp_serv,year_,month_,33 stek,(-1)*val1,(-1)*val2,(-1)*val3,cnt_stage,-3 kod_info,valm,valm_p,dop87,dop87_p,is_used " +
                                " From ttt_ddd "
                                , true);
                            if (!ret.result) { return; }

                            ExecSQL(conn_db2, " drop table ttt_ddd ", false);

                        }
                        else
                        {
                            // удалить по месяцам в прошлом stek = 103
                            MyDataReader reader;
                            ret = ExecRead(conn_db2, out reader,
                                " Select year_, month_ From " + chargeXX.delta_xx +
                                p_where +
                                " and stek in (3,33) and is_used = 1 and " + chargeXX.where_kvar +
                                " Group by 1,2 Order by 1,2 "
                                , true);
                            if (!ret.result) { return; }

                            try
                            {
                                while (reader.Read())
                                {
                                    int imn = (int)reader["month_"];
                                    int iyr = (int)reader["year_"];

                                    string delta_xx_rab = chargeXX.paramcalc.pref + "_charge_" + (iyr - 2000).ToString("00") +
                                                          tableDelimiter + "delta_" + imn.ToString("00");
                                    ret = ExecSQL(conn_db2,
                                        " Delete From " + delta_xx_rab +
                                        p_where +
                                        " and stek = 103 and is_used = " + chargeXX.paramcalc.cur_yy + " * 100 + " + chargeXX.paramcalc.cur_mm +
                                        " and " + chargeXX.where_kvar
                                        , true);
                                    if (!ret.result) { return; }
                                }
                            }
                            catch (Exception ex)
                            {
                                ret.result = false;
                                ret.text = ex.Message;
                                return;
                            }
                            finally
                            {
                                reader.Close();
                            }
                            // снятие учтенных дельт расходов при расчете ЛС
                            ret = ExecSQL(conn_db2,
                                " Delete From " + chargeXX.delta_xx +
                                p_where +
                                " and stek = 33 and " + chargeXX.where_kvar
                                , true);
                            if (!ret.result) { return; }
                            //
                        }
                        // дельты расходов удалить по всем выбранным ЛС!
                        ret = ExecSQL(conn_db2,
                            " Delete From " + chargeXX.delta_xx +
                            p_where +
                            " and stek = 3 and " + chargeXX.where_kvar
                            , true);
                        if (!ret.result) { return; }
                    }
                    if (chargeXX.paramcalc.nzp_kvar > 0 || chargeXX.paramcalc.nzp_dom > 0 || chargeXX.paramcalc.list_dom)
                    {
                        ret = ExecSQL(conn_db2,
                            " Delete From " + chargeXX.delta_xx +
                            p_where +
                            " and stek = 1 and " + chargeXX.where_kvar
                            , true);
                    }
                    else
                    {
                        ExecByStep(conn_db2, chargeXX.delta_xx, "nzp_delta",
                                " Delete From " + chargeXX.delta_xx +
                                  p_where
                                , 100000, " ", out ret);
                    }
                    if (!ret.result) { return; }

                    UpdateStatistics(false, chargeXX.paramcalc, chargeXX.delta_tab, out ret);
                    if (!ret.result) { return; }
                }
            }
            else
            {
                string conn_kernel = Points.GetConnByPref(chargeXX.paramcalc.pref);
                IDbConnection conn_db = GetConnection(conn_kernel);
                //IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
                ret = OpenDb(conn_db, true);
                if (!ret.result) { return; }

                string snmbd = chargeXX.paramcalc.pref + "_charge_" + (chargeXX.paramcalc.cur_yy - 2000).ToString("00");
                ret = ExecSQL(conn_db,
#if PG
 " set search_path to '" + snmbd + "'"
#else
                    " database " + snmbd
#endif
, true);
                if (!ret.result) { conn_db.Close(); return; }

                ret = ExecSQL(conn_db,
                    " Create table " + tbluser + chargeXX.delta_tab +
                    "  ( nzp_delta  serial not null, " +
                    "    nzp_kvar   integer not null, " +
                    "    nzp_dom    integer not null, " +
                    "    nzp_serv   integer not null, " +
                    "    year_      integer not null, " +
                    "    month_     integer not null, " +
                    "    stek       integer not null, " +
                    "    val1       " + sDecimalType + "(15,7) default 0.00, " +
                    "    val2       " + sDecimalType + "(15,7) default 0.00, " +
                    "    val3       " + sDecimalType + "(15,7) default 0.00, " +
                    "    cnt_stage  integer not null, " +
                    "    kod_info   integer not null, " +
                    "    is_used    integer default 0, " +
                    "    dop87      " + sDecimalType + "(15,7) default 0.00, " +
                    "    dop87_p    " + sDecimalType + "(15,7) default 0.00, " +
                    "    valm       " + sDecimalType + "(15,7) default 0.00, " +
                    "    valm_p     " + sDecimalType + "(15,7) default 0.00  " +
                    "  ) "
                    , true);
                if (!ret.result) { conn_db.Close(); return; }

                ret = ExecSQL(conn_db,
                    " Create unique index " + tbluser + "idlt1_"
                    + chargeXX.paramcalc.alias + "_" + chargeXX.paramcalc.cur_mm.ToString("00") +
                                        " on " + chargeXX.delta_tab + " (nzp_delta) ", true);
                if (!ret.result) { conn_db.Close(); return; }

                ret = ExecSQL(conn_db,
                    " Create unique index " + tbluser + "idlt2_"
                    + chargeXX.paramcalc.alias + "_" + chargeXX.paramcalc.cur_mm.ToString("00") +
                                        " on " + chargeXX.delta_tab + " (nzp_kvar,nzp_serv,stek,year_,month_) ", true);
                if (!ret.result) { conn_db.Close(); return; }

                ret = ExecSQL(conn_db,
                    " Create index " + tbluser + "idlt3_" +
                    chargeXX.paramcalc.alias + "_" + chargeXX.paramcalc.cur_mm.ToString("00") +
                                        " on " + chargeXX.delta_tab + " (year_,month_) ", true);
                if (!ret.result) { conn_db.Close(); return; }

                ret = ExecSQL(conn_db,
                    " Create index " + tbluser + "idlt4_"
                    + chargeXX.paramcalc.alias + "_" + chargeXX.paramcalc.cur_mm.ToString("00") +
                                        " on " + chargeXX.delta_tab + " (nzp_kvar,year_,month_) ", true);
                if (!ret.result) { conn_db.Close(); return; }

                ret = ExecSQL(conn_db,
                    " Create index " + tbluser + "idlt5_"
                    + chargeXX.paramcalc.alias + "_" + chargeXX.paramcalc.cur_mm.ToString("00") +
                                        " on " + chargeXX.delta_tab + " (nzp_dom,nzp_serv,stek) ", true);
                if (!ret.result) { conn_db.Close(); return; }

                ExecSQL(conn_db, sUpdStat + chargeXX.delta_tab, true);
                conn_db.Close();
            }

            //*****************************************************************
            //charge_xx
            //*****************************************************************
            if (!TempTableInWebCashe(conn_db2, chargeXX.charge_xx))
            {
                string conn_kernel = Points.GetConnByPref(chargeXX.paramcalc.pref);
                IDbConnection conn_db = GetConnection(conn_kernel);
                //IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
                ret = OpenDb(conn_db, true);
                if (!ret.result) { return; }

                string snmbd = chargeXX.paramcalc.pref + "_charge_" + (chargeXX.paramcalc.calc_yy - 2000).ToString("00");
                ret = ExecSQL(conn_db,
#if PG
 " set search_path to '" + snmbd + "'"
#else
                    " Database " + snmbd
#endif
, true);
                if (!ret.result) { conn_db.Close(); return; }
                if (DBManager.SchemaExists(snmbd, conn_db))
                {
                    ret = ExecSQL(conn_db,
                        " Create table " + tbluser + chargeXX.charge_tab +
                        " (  nzp_charge     serial not null, " +
                        "    nzp_kvar       integer, " +
                        "    num_ls         integer, " +
                        "    nzp_serv       integer, " +
                        "    nzp_supp       integer, " +
                        "    nzp_frm        integer, " +
                        "    dat_charge     date, " +
                        "    tarif          " + sDecimalType + "(14,3) default 0.000 not null, " +
                        "    tarif_p        " + sDecimalType + "(14,3) default 0.000 not null, " +
                        "    gsum_tarif     " + sDecimalType + "(14,2) default 0.00  not null, " +
                        "    rsum_tarif     " + sDecimalType + "(14,2) default 0.00  not null, " +
                        "    rsum_lgota     " + sDecimalType + "(14,2) default 0.00  not null, " +
                        "    sum_tarif      " + sDecimalType + "(14,2) default 0.00  not null, " +
                        "    sum_dlt_tarif  " + sDecimalType + "(14,2) default 0.00  not null, " +
                        "    sum_dlt_tarif_p " + sDecimalType + "(14,2) default 0.00 not null, " +
                        "    sum_tarif_p     " + sDecimalType + "(14,2) default 0.00 not null, " +
                        "    sum_lgota       " + sDecimalType + "(14,2) default 0.00 not null, " +
                        "    sum_dlt_lgota   " + sDecimalType + "(14,2) default 0.00 not null, " +
                        "    sum_dlt_lgota_p " + sDecimalType + "(14,2) default 0.00 not null, " +
                        "    sum_lgota_p    " + sDecimalType + "(14,2) default 0.00  not null, " +
                        "    sum_nedop      " + sDecimalType + "(14,2) default 0.00  not null, " +
                        "    sum_nedop_p    " + sDecimalType + "(14,2) default 0.00  not null, " +
                        "    sum_real       " + sDecimalType + "(14,2) default 0.00  not null, " +
                        "    sum_charge     " + sDecimalType + "(14,2) default 0.00  not null, " +
                        "    reval          " + sDecimalType + "(14,2) default 0.00  not null, " +
                        "    real_pere      " + sDecimalType + "(14,2) default 0.00  not null, " +
                        "    sum_pere       " + sDecimalType + "(14,2) default 0.00  not null, " +
                        "    real_charge    " + sDecimalType + "(14,2) default 0.00  not null, " +
                        "    sum_money      " + sDecimalType + "(14,2) default 0.00  not null, " +
                        "    money_to       " + sDecimalType + "(14,2) default 0.00  not null, " +
                        "    money_from     " + sDecimalType + "(14,2) default 0.00  not null, " +
                        "    money_del      " + sDecimalType + "(14,2) default 0.00  not null, " +
                        "    sum_fakt       " + sDecimalType + "(14,2) default 0.00  not null, " +
                        "    fakt_to        " + sDecimalType + "(14,2) default 0.00  not null, " +
                        "    fakt_from      " + sDecimalType + "(14,2) default 0.00  not null, " +
                        "    fakt_del       " + sDecimalType + "(14,2) default 0.00  not null, " +
                        "    sum_insaldo    " + sDecimalType + "(14,2) default 0.00  not null, " +
                        "    izm_saldo         " + sDecimalType + "(14,2) default 0.00 not null, " +
                        "    sum_outsaldo      " + sDecimalType + "(14,2) default 0.00 not null, " +
                        "    sum_subsidy       " + sDecimalType + "(14,2) default 0.00 not null, " +
                        "    sum_subsidy_p     " + sDecimalType + "(14,2) default 0.00 not null, " +
                        "    sum_subsidy_reval " + sDecimalType + "(14,2) default 0.00 not null, " +
                        "    sum_subsidy_all   " + sDecimalType + "(14,2) default 0.00 not null, " +
                        "    isblocked      integer default 0, " +
                        "    is_device      integer default 0, " +
                        "    c_calc         " + sDecimalType + "(14,4) default 0, " +
                        "    c_sn           " + sDecimalType + "(14,4) default 0, " +
                        "    c_okaz         " + sDecimalType + "(14,2) default 0, " +
                        "    c_nedop        " + sDecimalType + "(14,2) default 0, " +
                        "    isdel          integer default 0, " +
                        "    c_reval            " + sDecimalType + "(14,2), " +
                        "    sum_tarif_f        " + sDecimalType + "(14,2) default 0.00 not null, " +
                        "    sum_tarif_f_p      " + sDecimalType + "(14,2) default 0.00 not null, " +
                        "    sum_tarif_sn_eot   " + sDecimalType + "(14,2) default 0.00 not null, " +
                        "    sum_tarif_sn_eot_p " + sDecimalType + "(14,2) default 0.00 not null, " +
                        "    sum_tarif_sn_f     " + sDecimalType + "(14,2) default 0.00 not null, " +
                        "    sum_tarif_sn_f_p   " + sDecimalType + "(14,2) default 0.00 not null, " +
                        "    tarif_f            " + sDecimalType + "(14,4) default 0.00 not null, " +
                        "    tarif_f_p          " + sDecimalType + "(14,4) default 0.00 not null, " +
                        "    reval_tarif     " + sDecimalType + "(14,2) default 0.00 not null, " +
                        "    reval_lgota     " + sDecimalType + "(14,2) default 0.00 not null, " +
                        "    sum_tarif_eot   " + sDecimalType + "(14,2) default 0.00 not null, " +
                        "    sum_tarif_eot_p " + sDecimalType + "(14,2) default 0.00 not null, " +
                        "    sum_lgota_eot   " + sDecimalType + "(14,2) default 0.00 not null, " +
                        "    sum_lgota_eot_p " + sDecimalType + "(14,2) default 0.00 not null, " +
                        "    sum_lgota_f     " + sDecimalType + "(14,2) default 0.00 not null, " +
                        "    sum_lgota_f_p   " + sDecimalType + "(14,2) default 0.00 not null, " +
                        "    sum_smo         " + sDecimalType + "(14,2) default 0.00 not null, " +
                        "    sum_smo_p       " + sDecimalType + "(14,2) default 0.00 not null, " +
                        "    rsum_subsidy    " + sDecimalType + "(14,2) default 0.00 not null, " +
                        "    order_print    integer default 0 ) "
                        , true);
                    if (!ret.result) { conn_db.Close(); return; }

                    ret = ExecSQL(conn_db,
                        " Create unique index " + tbluser + "ic1_"
                        + chargeXX.paramcalc.alias + "_" + chargeXX.paramcalc.calc_mm.ToString("00") +
                        " on " + chargeXX.charge_tab + " (nzp_charge) ", true);
                    if (!ret.result) { conn_db.Close(); return; }

                    ret = ExecSQL(conn_db,
                        " Create index " + tbluser + "ic2_"
                        + chargeXX.paramcalc.alias + "_" + chargeXX.paramcalc.calc_mm.ToString("00") +
                        " on " + chargeXX.charge_tab + " (nzp_kvar, nzp_serv, dat_charge) ", true);
                    if (!ret.result) { conn_db.Close(); return; }

                    ret = ExecSQL(conn_db,
                        " Create index " + tbluser + "ic3_"
                        + chargeXX.paramcalc.alias + "_" + chargeXX.paramcalc.calc_mm.ToString("00") +
                        " on " + chargeXX.charge_tab + " (nzp_kvar, nzp_serv, nzp_supp) ", true);
                    if (!ret.result) { conn_db.Close(); return; }

                    ret = ExecSQL(conn_db,
                        " Create index " + tbluser + "ic4_"
                        + chargeXX.paramcalc.alias + "_" + chargeXX.paramcalc.calc_mm.ToString("00") +
                        " on " + chargeXX.charge_tab + " (nzp_kvar, nzp_supp, dat_charge) ", true);
                    if (!ret.result) { conn_db.Close(); return; }

                    ret = ExecSQL(conn_db,
                        " Create index " + tbluser + "ic5_"
                        + chargeXX.paramcalc.alias + "_" + chargeXX.paramcalc.calc_mm.ToString("00") +
                        " on " + chargeXX.charge_tab + " (num_ls) ", true);
                    if (!ret.result) { conn_db.Close(); return; }

                    ret = ExecSQL(conn_db,
                        " Create index " + tbluser + "ic6_"
                        + chargeXX.paramcalc.alias + "_" + chargeXX.paramcalc.calc_mm.ToString("00") +
                        " on " + chargeXX.charge_tab + " (num_ls, nzp_serv, nzp_supp) ", true);
                    if (!ret.result) { conn_db.Close(); return; }

                    ret = ExecSQL(conn_db,
                        " Create index " + tbluser + "ic7_"
                        + chargeXX.paramcalc.alias + "_" + chargeXX.paramcalc.calc_mm.ToString("00") +
                        " on " + chargeXX.charge_tab + " (nzp_serv) ", true);
                    if (!ret.result) { conn_db.Close(); return; }

                    ExecSQL(conn_db, sUpdStat + " " + chargeXX.charge_tab, true);

                    conn_db.Close();
                }
            }
        }
        #endregion Создание таблицы начислений и других вспомогательные таблицы

        #region Запись корректировки входящего сальдо - перенесено в Charge.ifmx.cs
        /*
        //-----------------------------------------------------------------------------
        public bool InsOutSaldoInPrevMonth(IDbConnection conn_db, CalcTypes.ParamCalc paramcalc, int nzp_serv, int nzp_supp, decimal rsum, out Returns ret)
        //-----------------------------------------------------------------------------
        {
            ret = Utils.InitReturns();

            //входящие параметры:
            //1. параметры расчета - ParamCalc paramcalc = new ParamCalc(nzp_kvar, 0, pref, cur_yy, cur_mm, cur_yy, cur_mm);
            //   paramcalc.num_ls = <num_ls>
            //2. nzp_serv - код услуги
            //3. nzp_supp - код поставщика
            //4. rsum - сумма входящего сальдо 
            //  
            //выходящие параметры:
            //ret.tag = 0 & ret.result = true - нормальное завершение
            //ret.tag = 1 - нет prev_charge_xx
            //ret.tag = 2 - неверные параметры для выполнения 

            if ((paramcalc.nzp_kvar > 0) && (nzp_serv > 0) && (nzp_supp > 0))
            {

                //CalcTypes.ChargeXX chargeXX = new CalcTypes.ChargeXX(paramcalc);

                // prev_charge_xx
                string prev_charge_tab = 
                    "charge_" + paramcalc.prev_calc_mm.ToString("00");
                string prev_charge_xx = 
                    paramcalc.pref + "_charge_" + (paramcalc.prev_calc_yy - 2000).ToString("00") + DBManager.tableDelimiter + prev_charge_tab;

                ret = ExecSQL(conn_db,
                        " Delete From " + prev_charge_xx +
                        " Where nzp_kvar = " + paramcalc.nzp_kvar + " and nzp_serv= " + nzp_serv + " and nzp_supp=" + nzp_supp
                    , true);
                if (!ret.result) { ret.tag = 1; return false; }

                if (Math.Abs(rsum) > Convert.ToDecimal(0.0000001))
                {

                    string sql =
                            " Insert into " + prev_charge_xx +
                            " ( nzp_charge, nzp_kvar, num_ls, nzp_serv, nzp_supp, nzp_frm, tarif, tarif_p, gsum_tarif, rsum_tarif, rsum_lgota, sum_tarif, sum_dlt_tarif, sum_dlt_tarif_p, " +
                              " sum_tarif_p,sum_lgota, sum_dlt_lgota, sum_dlt_lgota_p, sum_lgota_p, sum_nedop, sum_nedop_p, sum_real, sum_charge, reval, real_pere, sum_pere, " +
                              " real_charge, sum_money, money_to, money_from, money_del, sum_fakt, fakt_to, fakt_from, fakt_del, sum_insaldo, izm_saldo, " +
                              " sum_outsaldo, sum_subsidy, sum_subsidy_p, sum_subsidy_reval, sum_subsidy_all, isblocked, is_device, c_calc, c_sn, c_okaz, " +
                              " c_nedop, isdel, c_reval, order_print) " +
                            " values( " + DBManager.sSerialDefault + ", " + 
                            paramcalc.nzp_kvar + "," + paramcalc.num_ls + ", " + nzp_serv + ", " + nzp_supp + ", " +
                              "  0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, " + rsum + ", 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 ) ";

                    ret = ExecSQL(conn_db, sql, true);
                    if (!ret.result) { return false; }

                }
            }
            else
            {
                ret.result = false;
                ret.tag = 2;
                ret.text = "Неудачная запись корректировки входящего сальдо! (" + paramcalc.nzp_kvar + "/" + nzp_serv + "/" + nzp_supp + ")";
                MonitorLog.WriteLog(ret.text, MonitorLog.typelog.Info, 1, 2, true);
            }
            return true;
        }
*/
        #endregion Запись корректировки входящего сальдо - перенесено в Charge.ifmx.cs
        //-----------------------------------------------------------------------------
        public bool CalcChargeXX(IDbConnection conn_db, CalcTypes.ParamCalc paramcalc, out Returns ret, bool UpdateStat = true)
        //-----------------------------------------------------------------------------
        {
            //проверка на существование таблиц для запрета перерасчета
            CreateTempProhibitedRecalcForCharge(conn_db, paramcalc, out ret);
            if (!ret.result) { conn_db.Close(); return false; }

            #region Первоначальная чистка chargeXX и настройки

            CalcTypes.ChargeXX chargeXX = new CalcTypes.ChargeXX(paramcalc);

            //---------------------------------------------------
            //выбрать множество лицевых счетов
            //---------------------------------------------------
            if (!TempTableInWebCashe(conn_db, "t_selkvar"))
            {
                ChoiseTempKvar(conn_db, ref paramcalc, out ret);
                if (!ret.result) { conn_db.Close(); return false; }
            }
            CreateChargeXX(conn_db, chargeXX, false, out ret);
            if (!ret.result) { return false; }
            var where_prehibited_recalc = " and not exists (select 1 from temp_prohibited_recalc pr " +
                        " where " + chargeXX.charge_xx + ".nzp_serv = pr.nzp_serv and " + chargeXX.charge_xx + ".nzp_supp = pr.nzp_supp and " +
                        chargeXX.charge_xx + ".nzp_kvar = pr.nzp_kvar)";
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
            bool bIsL100 = (icountvals < 1000);

            // ... для расчета дотаций ...
            // nzp_prm=1992 -  pref_data:prm_5
            bool bIsCalcSubsidy = Points.IsCalcSubsidy;
            // ... !!! 1989|--- 5.0 - биллинг для Сахи|||bool||5|||| !!! ... 
            bool bIsCalcSubsidyBill =
                CheckValBoolPrm(conn_db, chargeXX.paramcalc.data_alias, 1989, "5");
            // ... Saha
            // ... !!! 9000|--- Для САХИ ---||| <тип> || <№ prm_XX> |||| !!! ... 
            bool bIsSaha =
                CheckValBoolPrm(conn_db, chargeXX.paramcalc.data_alias, 9000, "5");

            #region Если мало ЛС,то чистка дублей в chargeXX
            if (bIsL100 && !chargeXX.paramcalc.b_reval)
            {
                ExecSQL(conn_db, " drop table t_dubl_ch_all ", false);
                ExecSQL(conn_db, " drop table t_dubl_ch ", false);

                ret = ExecSQL(conn_db,
                    " Create temp table t_dubl_ch_all " +
                    " ( nzp_charge integer, " +
                    "   nzp_kvar   integer, " +
                    "   nzp_serv   integer, " +
                    "   nzp_supp   integer, " +
                    "   is_used    integer default 0 " +
                    " ) " + sUnlogTempTable
                    , true);
                if (!ret.result) { DropTempTablesCharge(conn_db); return false; }

                ret = ExecSQL(conn_db,
                    " Create temp table t_dubl_ch " +
                    " ( nzp_charge integer, " +
                    "   nzp_kvar   integer, " +
                    "   nzp_serv   integer, " +
                    "   nzp_supp   integer " +
                    " ) " + sUnlogTempTable
                    , true);
                if (!ret.result) { DropTempTablesCharge(conn_db); return false; }

                ret = ExecSQL(conn_db,
                    " Insert into t_dubl_ch ( nzp_kvar,nzp_supp,nzp_serv,nzp_charge ) " +
                    " Select " +
                    " nzp_kvar,nzp_supp,nzp_serv,max(nzp_charge) as nzp_charge " +
                    " From " + chargeXX.charge_xx +
                    " Where " + chargeXX.where_kvar + //nzp_kvar in ( Select nzp_kvar From t_selkvar ) " + 
                    chargeXX.paramcalc.per_dat_charge +
                    " Group by 1,2,3 having count(*)>1 "
                    , true);
                if (!ret.result) { DropTempTablesCharge(conn_db); return false; }

                ret = ExecSQL(conn_db, " Create unique index ix1_t_dubl_ch on t_dubl_ch (nzp_kvar,nzp_serv,nzp_supp) ", true);
                if (!ret.result) { DropTempTablesCharge(conn_db); return false; }

                ret = ExecSQL(conn_db, " Create unique index ix2_t_dubl_ch on t_dubl_ch (nzp_charge) ", true);
                if (!ret.result) { DropTempTablesCharge(conn_db); return false; }

                ret = ExecSQL(conn_db, sUpdStat + " t_dubl_ch ", true);
                if (!ret.result) { DropTempTablesCharge(conn_db); return false; }

                ret = ExecSQL(conn_db,
                    " Insert into t_dubl_ch_all ( nzp_kvar,nzp_supp,nzp_serv,nzp_charge ) " +
                    " Select " +
                    " nzp_kvar,nzp_supp,nzp_serv,nzp_charge " +
                    " From " + chargeXX.charge_xx +
                    " Where EXISTS " +
                    " (Select 1 from t_dubl_ch t Where" +
                    "       t.nzp_kvar=" + chargeXX.charge_xx + ".nzp_kvar" +
                    "   and t.nzp_serv=" + chargeXX.charge_xx + ".nzp_serv" +
                    "   and t.nzp_supp=" + chargeXX.charge_xx + ".nzp_supp" +
                    " ) " +
                    chargeXX.paramcalc.per_dat_charge
                    , true);
                if (!ret.result) { DropTempTablesCharge(conn_db); return false; }

                ret = ExecSQL(conn_db, " Create unique index ix_t_dubl_ch_all on t_dubl_ch_all (nzp_charge,is_used) ", true);
                if (!ret.result) { DropTempTablesCharge(conn_db); return false; }

                ret = ExecSQL(conn_db, sUpdStat + " t_dubl_ch_all ", true);
                if (!ret.result) { DropTempTablesCharge(conn_db); return false; }

                ret = ExecSQL(conn_db,
                    " update t_dubl_ch_all" +
                    " set is_used=1" +
                    " where 0<(select count(*) from t_dubl_ch t Where t_dubl_ch_all.nzp_charge=t.nzp_charge) "
                    , true);
                if (!ret.result) { DropTempTablesCharge(conn_db); return false; }

                ret = ExecSQL(conn_db,
                    " Delete " +
                    " From " + chargeXX.charge_xx +
                    " Where 0<" +
                    " (Select count(*) from t_dubl_ch_all t" +
                    "  Where t.is_used=0 and t.nzp_charge=" + chargeXX.charge_xx + ".nzp_charge" +
                    " ) " +
                    chargeXX.paramcalc.per_dat_charge
                    , true);
                if (!ret.result) { DropTempTablesCharge(conn_db); return false; }

                ret = ExecSQL(conn_db, " drop table t_dubl_ch ", true);
                if (!ret.result) { DropTempTablesCharge(conn_db); return false; }
                ret = ExecSQL(conn_db, " drop table t_dubl_ch_all ", true);
                if (!ret.result) { DropTempTablesCharge(conn_db); return false; }
            }
            #endregion Если мало ЛС,то чистка дублей в chargeXX

            string sql =
                " Update " + chargeXX.charge_xx +
                " Set (nzp_frm, tarif, tarif_p, gsum_tarif, rsum_tarif, rsum_lgota, sum_tarif, sum_dlt_tarif, sum_dlt_tarif_p, sum_tarif_p, sum_lgota, " +
                     " sum_dlt_lgota, sum_dlt_lgota_p, sum_lgota_p, sum_nedop, sum_nedop_p, sum_real, sum_charge, reval, real_pere, sum_pere, " +
                     " real_charge, sum_money, money_to, money_from, money_del, sum_fakt, fakt_to, fakt_from, fakt_del, sum_insaldo, izm_saldo, " +
                     " sum_outsaldo, sum_subsidy, sum_subsidy_p, sum_subsidy_reval, sum_subsidy_all, isblocked, is_device, c_calc, c_sn, c_okaz, " +
                     " c_nedop, isdel, c_reval, order_print" +
                     ",sum_tarif_f,sum_tarif_f_p, sum_tarif_sn_eot,sum_tarif_sn_eot_p,sum_tarif_sn_f,sum_tarif_sn_f_p,tarif_f,tarif_f_p" +
                ") = " +
                " ( 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0" +
                ", 0, 0, 0, 0, 0, 0, 0, 0 ) " +
                " Where " + chargeXX.where_kvar + where_prehibited_recalc +//nzp_kvar in ( Select nzp_kvar From t_selkvar ) " + 
                chargeXX.paramcalc.per_dat_charge;

            if (chargeXX.paramcalc.nzp_kvar > 0 || chargeXX.paramcalc.nzp_dom > 0)
            {
                ret = ExecSQL(conn_db, sql, true);
            }
            else
            {
                ExecByStep(conn_db, chargeXX.charge_xx, "nzp_charge", sql, 50000, " ", out ret);
            }

            if (!ret.result) { DropTempTablesCharge(conn_db); return false; }

            sql =
                " Delete From " + chargeXX.charge_xx +
                " Where " + chargeXX.where_kvar + //nzp_kvar in ( Select nzp_kvar From t_selkvar ) " +
                "   and nzp_serv = 1 " + where_prehibited_recalc +
                    chargeXX.paramcalc.per_dat_charge;

            if (chargeXX.paramcalc.nzp_kvar > 0 || chargeXX.paramcalc.nzp_dom > 0)
            {
                ret = ExecSQL(conn_db, sql, true);
            }
            else
            {
                ExecByStep(conn_db, chargeXX.charge_xx, "nzp_charge", sql, 50000, " ", out ret);
            }

            if (!ret.result) { DropTempTablesCharge(conn_db); return false; }

            UpdateStatistics(false, paramcalc, chargeXX.charge_tab, out ret);
            if (!ret.result) { DropTempTablesCharge(conn_db); return false; }

            //string tab = "";
            #endregion  Первоначальная чистка chargeXX и настройки

            //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
            //при перерасчете исключить некоторые сальдовые таблицы (prekedka, del_supplier, etc)
            //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
            #region вставка дополнительных записей в chargeXX

            #region выборка недопоставок

            // недопоставки выберем отдельно - они потом понадобятся для расчета суммы недопоставки
            ExecSQL(conn_db, " Drop table t_nedo_xx ", false);
            ret = ExecSQL(conn_db,
                " Create temp table t_nedo_xx " +
                " ( nzp_kvar  integer, " +
                "   num_ls    integer, " +
                "   nzp_dom     integer       not null, " +
                "   nzp_area    integer       default 0 not null, " +
                "   nzp_geu     integer       default 0 not null, " +
                "   dat_charge  date, " +
                "   cur_zap     integer       default 0 not null, " +
                "   nzp_serv    integer       not null, " +
                "   nzp_kind    integer, " +
                "   koef        " + sDecimalType + "(10,8) default 0.00, " +  //скидка за час
                "   tn          real, " +
                "   tn_min      " + sDecimalType + "(5,2), " +
                "   tn_max      " + sDecimalType + "(5,2), " +
#if PG
                "   dat_s    timestamp, " +
                "   dat_po   timestamp, " +
                "   cnts     interval hour, " +
                "   cnts_del interval hour, " +
#else
                "   dat_s    DATETIME YEAR to HOUR, " +
                "   dat_po   DATETIME YEAR to HOUR, " +
                "   cnts     INTERVAL HOUR(3) to HOUR, " +
                "   cnts_del INTERVAL HOUR(3) to HOUR, " +
#endif
                "   perc        " + sDecimalType + "(5,2)  default 0.00, " +
                "   kod_info    integer       default 0 " +
                " )  " + sUnlogTempTable
                , true);
            if (!ret.result) { DropTempTablesCharge(conn_db); return false; }

            ret = ExecSQL(conn_db,
                " Insert Into t_nedo_xx " +
                " (nzp_kvar,num_ls,nzp_dom,nzp_area,nzp_geu, " +
                "  dat_charge,cur_zap,nzp_serv,nzp_kind,koef,tn,tn_min,tn_max,dat_s,dat_po,cnts,cnts_del,perc,kod_info) " +
                " Select k.nzp_kvar,k.num_ls,k.nzp_dom,k.nzp_area,k.nzp_geu," +
                " a.dat_charge,a.cur_zap,a.nzp_serv,a.nzp_kind,a.koef,a.tn,a.tn_min,a.tn_max,a.dat_s,a.dat_po,a.cnts,a.cnts_del,a.perc,a.kod_info" +
                " From " + chargeXX.calc_nedo_xx + " a, t_selkvar k" +
                " Where a.nzp_kvar=k.nzp_kvar and a.cur_zap <> -1 "
                , true);
            if (!ret.result)
            {
                DropTempTablesCharge(conn_db);
                return false;
            }

            ExecSQL(conn_db, " Create index ix1_t_nedo_xx on t_nedo_xx (nzp_kvar,nzp_serv) ", true);
            ExecSQL(conn_db, sUpdStat + " t_nedo_xx ", true);

            #endregion выборка недопоставок

            if (!chargeXX.paramcalc.b_reval)
            {
                #region чистка del_supplier
                sql =
                    " Delete From " + chargeXX.del_supplier +
                    " Where num_ls in ( Select num_ls From t_selkvar ) " +
                    "   and kod_sum = 39 " +
                    "   and dat_account = " + sDefaultSchema + "MDY (" +
                    chargeXX.paramcalc.calc_mm + ",28," + chargeXX.paramcalc.calc_yy + ") ";

                if (chargeXX.paramcalc.nzp_kvar > 0 || chargeXX.paramcalc.nzp_dom > 0)
                {
                    ret = ExecSQL(conn_db, sql, true);
                }
                else
                {
                    ExecByStep(conn_db, chargeXX.del_supplier, "nzp_del", sql, 50000, " ", out ret);
                }
                if (!ret.result) { DropTempTablesCharge(conn_db); return false; }

                UpdateStatistics(false, paramcalc, "del_supplier", out ret);
                if (!ret.result)
                {
                    ret.text = "Ошибка выборки данных ";
                    DropTempTablesCharge(conn_db);
                    return false;
                }
                #endregion чистка del_supplier

                #region выборка перерасчетов

                //---------------------------------------------------
                //добавить строки nzp_serv,nzp_supp из всех целевых таблиц, участвующих в сальдо
                //---------------------------------------------------
                //reval_xx
                ret = ExecSQL(conn_db,
                    " Create temp table t_lnk_reval " +
                    " ( nzp_kvar  integer, " +
                    "   num_ls    integer, " +
                    "   nzp_supp  integer, " +
                    "   nzp_serv  integer, " +
                    "   reval " + sDecimalType + "(14,2) " +
                    " )  " + sUnlogTempTable
                    , true);
                if (!ret.result) { DropTempTablesCharge(conn_db); return false; }

                ret = ExecSQL(conn_db,
                    " Insert Into t_lnk_reval (nzp_kvar,num_ls,nzp_supp,nzp_serv,reval) " +
                    " Select k.nzp_kvar,k.num_ls,nzp_supp,nzp_serv, " +
                    " sum(case when kod_info<>1 then reval else 0 end) as reval " +
                    " From " + chargeXX.reval_xx + " a, t_selkvar k " +
                    " Where a.nzp_kvar = k.nzp_kvar " +
                    "   and abs(reval) > 0.0001 " +
                    "   and nzp_serv <> 1 " +
                    " Group by 1,2,3,4 "
                    , true);
                if (!ret.result) { DropTempTablesCharge(conn_db); return false; }

                ExecSQL(conn_db, " Create unique index ix_t_lnk_reval on t_lnk_reval (nzp_kvar,nzp_serv,nzp_supp) ", true);
                ExecSQL(conn_db, sUpdStat + " t_lnk_reval ", true);

                #endregion выборка перерасчетов

                #region выборка оплат

                //fn_supplier
                ret = ExecSQL(conn_db,
                    " Create temp table t_fn_suppall " +
                    " ( num_ls    integer, " +
                    "   nzp_supp  integer, " +
                    "   nzp_serv  integer, " +
                    "   sum_prih " + sDecimalType + "(14,2) " +
                    " )  " + sUnlogTempTable
                    , true);
                if (!ret.result) { DropTempTablesCharge(conn_db); return false; }

                string sfind_tmp =
                    " Insert Into t_fn_suppall (num_ls,nzp_supp,nzp_serv,sum_prih) " +
                    " Select num_ls,nzp_supp,nzp_serv,sum_prih " +
                    " From " + chargeXX.fn_supplier +
                    " Where abs(sum_prih) > 0.0001 and nzp_serv <> 1 ";

                // если расчет по кол-ву л/с меньше 100, то соединить с t_selkvar для min времени выборки
                if (bIsL100)
                {
                    sfind_tmp =
                    " Insert Into t_fn_suppall (num_ls,nzp_supp,nzp_serv,sum_prih) " +
                    " Select a.num_ls,a.nzp_supp,a.nzp_serv,a.sum_prih " +
                    " From " + chargeXX.fn_supplier + " a, t_selkvar k" +
                    " Where a.num_ls = k.num_ls " +
                    " and abs(a.sum_prih) > 0.0001 and a.nzp_serv <> 1 ";
                }

                ret = ExecSQL(conn_db, sfind_tmp, true);
                if (!ret.result) { DropTempTablesCharge(conn_db); return false; }

                ExecSQL(conn_db, " Create index ix_t_fn_suppall on t_fn_suppall (num_ls) ", true);
                ExecSQL(conn_db, sUpdStat + " t_fn_suppall ", true);
                //tab = chargeXX.paramcalc.pref + "_charge_" + (chargeXX.paramcalc.calc_yy - 2000).ToString("00") + ":fn_supplier" + chargeXX.paramcalc.calc_mm.ToString("00");
                ret = ExecSQL(conn_db,
                    " Create temp table t_fn_supp " +
                    " ( nzp_kvar  integer, " +
                    "   num_ls    integer, " +
                    "   nzp_supp  integer, " +
                    "   nzp_serv  integer, " +
                    "   money_to " + sDecimalType + "(14,2) " +
                    " )  " + sUnlogTempTable
                    , true);
                if (!ret.result) { DropTempTablesCharge(conn_db); return false; }

                ret = ExecSQL(conn_db,
                    " Insert Into t_fn_supp (nzp_kvar,num_ls,nzp_supp,nzp_serv,money_to) " +
                    " Select k.nzp_kvar,k.num_ls,nzp_supp,nzp_serv,sum(sum_prih) as money_to " +
                    " From t_fn_suppall a, t_selkvar k " +
                    " Where a.num_ls = k.num_ls " +
                    " Group by 1,2,3,4 "
                    , true);
                if (!ret.result) { DropTempTablesCharge(conn_db); return false; }

                ExecSQL(conn_db, " Create unique index ix_t_fn_supp on t_fn_supp (nzp_kvar,nzp_serv,nzp_supp) ", true);
                ExecSQL(conn_db, sUpdStat + " t_fn_supp ", true);

                //from_supplier
                //tab = chargeXX.paramcalc.pref + "_charge_" + (chargeXX.paramcalc.calc_yy - 2000).ToString("00") + ":from_supplier";
                ret = ExecSQL(conn_db,
                    " Create temp table t_from_supp " +
                    " ( nzp_kvar  integer, " +
                    "   num_ls    integer, " +
                    "   nzp_supp  integer, " +
                    "   nzp_serv  integer, " +
                    "   money_from " + sDecimalType + "(14,2) " +
                    " )  " + sUnlogTempTable
                    , true);
                if (!ret.result) { DropTempTablesCharge(conn_db); return false; }

                ret = ExecSQL(conn_db,
                    " Insert Into t_from_supp (nzp_kvar,num_ls,nzp_supp,nzp_serv,money_from) " +
                    " Select k.nzp_kvar,k.num_ls,nzp_supp,nzp_serv,sum(sum_prih) as money_from " +
                    " From " + chargeXX.from_supplier + " a, t_selkvar k " +
                    " Where a.num_ls = k.num_ls " +
                    "   and dat_uchet >= " + chargeXX.paramcalc.dat_s +
                    "   and dat_uchet <= " + chargeXX.paramcalc.dat_po +
                    "   and abs(sum_prih) > 0.0001 " +
                    "   and nzp_serv <> 1 " +
                    " Group by 1,2,3,4 "
                    , true);
                if (!ret.result) { DropTempTablesCharge(conn_db); return false; }

                ExecSQL(conn_db, " Create unique index ix_t_from_supp on t_from_supp (nzp_kvar,nzp_serv,nzp_supp) ", true);
                ExecSQL(conn_db, sUpdStat + " t_from_supp ", true);

                //del_supplier
                //tab = chargeXX.paramcalc.pref + "_charge_" + (chargeXX.paramcalc.calc_yy - 2000).ToString("00") + ":del_supplier";
                ret = ExecSQL(conn_db,
                    " Create temp table t_del_supp " +
                    " ( nzp_kvar  integer, " +
                    "   num_ls    integer, " +
                    "   nzp_supp  integer, " +
                    "   nzp_serv  integer, " +
                    "   money_del " + sDecimalType + "(14,2) " +
                    " )  " + sUnlogTempTable
                    , true);
                if (!ret.result) { DropTempTablesCharge(conn_db); return false; }

                ret = ExecSQL(conn_db,
                    " Insert Into t_del_supp (nzp_kvar,num_ls,nzp_supp,nzp_serv,money_del) " +
                    " Select k.nzp_kvar,k.num_ls,nzp_supp,nzp_serv,sum(sum_prih) as money_del " +
                    " From " + chargeXX.del_supplier + " a, t_selkvar k " +
                    " Where a.num_ls = k.num_ls " +
                    "   and dat_account >= " + chargeXX.paramcalc.dat_s +
                    "   and dat_account <= " + chargeXX.paramcalc.dat_po +
                    "   and abs(sum_prih) > 0.0001 " +
                    "   and nzp_serv <> 1 " +
                    " Group by 1,2,3,4 "
                    , true);
                if (!ret.result) { DropTempTablesCharge(conn_db); return false; }

                ExecSQL(conn_db, " Create unique index ix_t_del_supp on t_del_supp (nzp_kvar,nzp_serv,nzp_supp) ", true);
                ExecSQL(conn_db, sUpdStat + " t_del_supp ", true);

                #endregion выборка оплат

                #region выборка прошлых начислений

                //prev_charge
                ret = ExecSQL(conn_db,
                    " Create temp table t_prev_ch " +
                    " ( nzp_kvar  integer, " +
                    "   num_ls    integer, " +
                    "   nzp_serv  integer, " +
                    "   nzp_supp  integer, " +
                    "   sum_outsaldo " + sDecimalType + "(14,2) " +
                    " )  " + sUnlogTempTable
                    , true);
                if (!ret.result) { DropTempTablesCharge(conn_db); return false; }

                if (chargeXX.paramcalc.nzp_kvar > 0 || chargeXX.paramcalc.nzp_dom > 0)
                {
                    ret = ExecSQL(conn_db,
                        " Insert into t_prev_ch ( nzp_kvar,num_ls,nzp_supp,nzp_serv,sum_outsaldo ) " +
                        " Select " +
                        " k.nzp_kvar,k.num_ls,nzp_supp,nzp_serv,sum(sum_outsaldo) as sum_outsaldo " +
                        " From " + chargeXX.prev_charge_xx + " a, t_selkvar k " +
                        " Where a.nzp_kvar = k.nzp_kvar " +
                        "   and abs(sum_outsaldo) > 0.0001 " +
                        "   and nzp_serv <> 1 " +
                        " Group by 1,2,3,4 "
                        , true);
                }
                else
                {
                    ExecByStep(conn_db, "t_selkvar", "nzp_key",
                        " Insert into t_prev_ch ( nzp_kvar,num_ls,nzp_supp,nzp_serv,sum_outsaldo ) " +
                        " Select " +
                        " k.nzp_kvar,k.num_ls,nzp_supp,nzp_serv,sum(sum_outsaldo) as sum_outsaldo " +
                        " From " + chargeXX.prev_charge_xx + " a, t_selkvar k " +
                        " Where a.nzp_kvar = k.nzp_kvar " +
                        "   and abs(sum_outsaldo) > 0.0001 " +
                        "   and nzp_serv <> 1 "
                        , 20000, " Group by 1,2,3,4 ", out ret);
                }

                if (!ret.result) { DropTempTablesCharge(conn_db); return false; }

                ExecSQL(conn_db, " Create unique index ix_t_prev_ch on t_prev_ch (nzp_kvar,nzp_serv,nzp_supp) ", true);
                ExecSQL(conn_db, sUpdStat + " t_prev_ch ", true);

                #endregion выборка прошлых начислений

                #region выборка перекидок

                //выбрать перекидки
                //tab = chargeXX.paramcalc.pref + "_charge_" + (chargeXX.paramcalc.calc_yy - 2000).ToString("00") + ":perekidka";
                ret = ExecSQL(conn_db,
                    " Create temp table t_perek " +
                    " ( nzp_kvar  integer, " +
                    "   num_ls    integer, " +
                    "   nzp_supp  integer, " +
                    "   nzp_serv  integer, " +
                    "   real_charge " + sDecimalType + "(14,2) " +
                    " )  " + sUnlogTempTable
                    , true);
                if (!ret.result) { DropTempTablesCharge(conn_db); return false; }

                ret = ExecSQL(conn_db,
                    " Insert into t_perek ( nzp_kvar,num_ls,nzp_supp,nzp_serv,real_charge ) " +
                    " Select k.nzp_kvar,k.num_ls,nzp_supp,nzp_serv,sum(sum_rcl) as real_charge " +
                    " From " + chargeXX.perekidka + " a, t_selkvar k " +
                    " Where a.nzp_kvar = k.nzp_kvar " +
                    "   and month_ = " + chargeXX.paramcalc.calc_mm +
                    "   and abs(sum_rcl) > 0.0001 " +
                    "   and nzp_serv <> 1 " +
                    "   and nzp_supp > 0 " +
                    " Group by 1,2,3,4 "
                    , true);
                if (!ret.result) { DropTempTablesCharge(conn_db); return false; }

                ExecSQL(conn_db, " Create unique index ix_t_perek on t_perek (nzp_kvar,nzp_supp,nzp_serv) ", true);
                ExecSQL(conn_db, sUpdStat + " t_perek ", true);

                #endregion выборка перекидок
            }


            #region выборка уникальных недопоставок

            //выбрать nedo_xx
            //tab = chargeXX.paramcalc.pref + "_charge_" + (chargeXX.paramcalc.calc_yy - 2000).ToString("00") + ":nedo_" + (chargeXX.paramcalc.calc_mm).ToString("00");
            ret = ExecSQL(conn_db,
                " Create temp table t_nedo " +
                " ( nzp_kvar  integer, " +
                "   num_ls    integer, " +
                "   nzp_serv  integer not null, " +
                "   koef        " + sDecimalType + "(10,8) default 0.00 " +
                " )  " + sUnlogTempTable
                , true);
            if (!ret.result) { DropTempTablesCharge(conn_db); return false; }

            ret = ExecSQL(conn_db,
                " Insert into t_nedo ( nzp_kvar,num_ls,nzp_serv,koef ) " +
                " Select nzp_kvar,num_ls,nzp_serv, max(koef) as koef " +
                " From t_nedo_xx " +
                " Group by 1,2,3 "
                , true, 6000);
            if (!ret.result) { DropTempTablesCharge(conn_db); return false; }

            ExecSQL(conn_db, " Create unique index ix_t_nedo on t_nedo (nzp_kvar,nzp_serv) ", true);
            ExecSQL(conn_db, sUpdStat + " t_nedo ", true);

            #endregion выборка уникальных недопоставок

            #region выборка начислений из calc_gku

            //выбрать calc_gku
            ret = ExecSQL(conn_db,
                " CREATE TEMP TABLE t_calc_gku( " +
                " nzp_kvar INTEGER, " +
                " num_ls   INTEGER, " +
                " nzp_serv INTEGER, " +
                " nzp_supp INTEGER, " +
                " nzp_frm  INTEGER default 0, " +
                " nzp_frm_typ      INTEGER default 0, " +
                " nzp_frm_typrs    INTEGER default 0, " +
                " is_device        INTEGER default 0, " +
                " koef_gkal        " + sDecimalType + "(14,8) default 1," +
                " tarif            " + sDecimalType + "(17,7) default 0.00, " +
                " gsum_tarif       " + sDecimalType + "(14,2) default 0.00, " +
                " rsum_tarif       " + sDecimalType + "(14,2) default 0.00, " +
                " rsum_lgota       " + sDecimalType + "(14,2) default 0.00, " +
                " sum_tarif        " + sDecimalType + "(14,2) default 0.00, " +
                " c_calc           " + sDecimalType + "(14,7) default 0.00, " +
                " c_sn             " + sDecimalType + "(14,7) default 0.00, " +
                " sum_nedop        " + sDecimalType + "(14,2) default 0.00, " +
                " sum_real         " + sDecimalType + "(14,2) default 0.00, " +
                " sum_tarif_sn_eot " + sDecimalType + "(14,2) default 0.00, " +
                " sum_tarif_sn_f   " + sDecimalType + "(14,2) default 0.00, " +
                " sum_subsidy      " + sDecimalType + "(14,2) default 0.00, " +
                " sum_subsidy_all  " + sDecimalType + "(14,2) default 0.00, " +
                " sum_lgota        " + sDecimalType + "(14,2) default 0.00, " +
                " sum_smo          " + sDecimalType + "(14,2) default 0.00, " +
                " tarif_f          " + sDecimalType + "(14,4) default 0.00, " +
                " sum_tarif_f      " + sDecimalType + "(14,2) default 0.00, " +
                " real_pere        " + sDecimalType + "(14,2) default 0.00, " +
                " nzp_prm_tarif    INTEGER default 0, " +
                " nzp_clc          INTEGER  " +
                " )  " + sUnlogTempTable
                , true, 6000);
            if (!ret.result)
            {
                DropTempTablesCharge(conn_db);
                return false;
            }

            sql =
                " Insert Into t_calc_gku(nzp_kvar,num_ls,nzp_serv,nzp_supp,nzp_frm,nzp_frm_typ,nzp_frm_typrs,koef_gkal,tarif,gsum_tarif,rsum_tarif,sum_tarif, " +
                " c_calc,c_sn,sum_tarif_sn_eot,tarif_f,sum_tarif_f,sum_tarif_sn_f,sum_subsidy_all,is_device,real_pere,nzp_prm_tarif,nzp_clc) " +
                " Select k.nzp_kvar,k.num_ls,nzp_serv,nzp_supp,nzp_frm,max(nzp_frm_typ),max(nzp_frm_typrs),max(trf3), " +
                " max(tarif) as tarif, sum(tarif * rashod_g) as gsum_tarif, sum(tarif * rashod) as rsum_tarif, sum(tarif * rashod) as sum_tarif,  sum(rashod) as c_calc," +
                " sum(rashod_norm) as c_sn, sum(tarif * rashod_norm) as sum_tarif_sn_eot ";

            if (bIsCalcSubsidy)
            {
                sql = sql + ", max(tarif_f) as tarif_f, sum(tarif_f * rashod) as sum_tarif_f, sum(tarif_f * rashod_norm) as sum_tarif_sn_f, sum(trf4) sum_subsidy_all ";
            }
            else
            {
                sql = sql + ", max(tarif) as tarif_f, sum(tarif * rashod) as sum_tarif_f, sum(tarif * rashod_norm) as sum_tarif_sn_f, 0 sum_subsidy_all ";
            }

            sql = sql + " ,max(is_device) is_device,sum(tarif * rsh3) as real_pere,max(nzp_prm_tarif) as nzp_prm_tarif,max(nzp_clc) as nzp_clc " +
                " From " + chargeXX.calc_gku_xx + " a, t_selkvar k " +
                " Where a.nzp_kvar = k.nzp_kvar " + chargeXX.paramcalc.per_dat_charge +
                "   and nzp_serv <> 1 and stek=3 " +
                " Group by 1,2,3,4,5 ";

            ret = ExecSQL(conn_db, sql, true, 6000);
            if (!ret.result)
            {
                DropTempTablesCharge(conn_db);
                return false;
            }
            ExecSQL(conn_db, " Create unique index ix_t_calc_gku on t_calc_gku (nzp_kvar,nzp_serv,nzp_supp,nzp_frm) ", true);
            ExecSQL(conn_db, sUpdStat + " t_calc_gku ", true);

            #endregion выборка начислений из calc_gku

            #region заполнение параметров субсидий

            if (bIsCalcSubsidy)
            {
                if (bIsSaha && (!bIsCalcSubsidyBill))
                {
                    ret = ExecSQL(conn_db,
                        " update t_calc_gku set " +
                          " c_sn = c_calc " +
                        " where nzp_frm_typ = 514 "
                        , true, 6000);
                    if (!ret.result) { DropTempTablesCharge(conn_db); return false; }

                    ret = ExecSQL(conn_db,
                        " update t_calc_gku set " +
                          " rsum_tarif = rsum_tarif * koef_gkal, gsum_tarif = gsum_tarif * koef_gkal, sum_tarif = sum_tarif * koef_gkal, c_calc = c_calc * koef_gkal, " +
                          " sum_tarif_sn_eot = tarif * c_sn, sum_tarif_sn_f = tarif_f * c_sn " +
                        " where nzp_frm_typ = 514 "
                        , true, 6000);
                    if (!ret.result) { DropTempTablesCharge(conn_db); return false; }
                }

                ret = ExecSQL(conn_db, " update t_calc_gku set sum_tarif_f = sum_tarif where sum_tarif_f > sum_tarif ", true, 6000);
                if (!ret.result)
                {
                    DropTempTablesCharge(conn_db);
                    return false;
                }

                ret = ExecSQL(conn_db, " update t_calc_gku set sum_tarif_sn_eot = sum_tarif where sum_tarif_sn_eot > sum_tarif ", true, 6000);
                if (!ret.result)
                {
                    DropTempTablesCharge(conn_db);
                    return false;
                }

                ret = ExecSQL(conn_db, " update t_calc_gku set sum_tarif_sn_f = sum_tarif_sn_eot where sum_tarif_sn_f > sum_tarif_sn_eot ", true, 6000);
                if (!ret.result)
                {
                    DropTempTablesCharge(conn_db);
                    return false;
                }
                // начисление дотации
                ret = ExecSQL(conn_db, " update t_calc_gku set sum_subsidy = sum_tarif - sum_tarif_f where 1=1 ", true, 6000);
                if (!ret.result)
                {
                    DropTempTablesCharge(conn_db);
                    return false;
                }
            }

            //---------------------------------------------------
            // обнулить начисление по указанным услугам (спец.таблица + update)
            /*
            ExecSQL(conn_db, " Drop table t_not_calc_serv ", false);
            ret = ExecSQL(conn_db,
                " Create temp table t_not_calc_serv " +
                " ( nzp_kvar  integer, " +
                "   num_ls    integer, " +
                "   nzp_serv  integer, " +
                "   nzp_supp  integer  " +
#if PG
                " ) "
#else
                " ) With no log "
#endif
                , true);
            if (!ret.result) { DropTempTablesCharge(conn_db); return false; }

            ret = ExecSQL(conn_db,
                " Insert into t_not_calc_serv (nzp_kvar,num_ls,nzp_supp,nzp_serv)  " +
                " Select nzp_kvar,num_ls,nzp_supp,nzp_serv From t_calc_gku group by 1,2,3,4 "
            , true);
            if (!ret.result) { DropTempTablesCharge(conn_db); return false; }

            ExecSQL(conn_db, " Create index ix_t_not_calc_serv on t_not_calc_serv (kod) ", true);
            ExecSQL(conn_db, sUpdStat + " t_not_calc_serv ", true);
            if (!ret.result) { DropTempTablesCharge(conn_db); return false; }
            */
            //
            //---------------------------------------------------
            #endregion заполнение параметров субсидий

            #region формирование записей для вставки
            //---------------------------------------------------
            //выбрать отсутствующие связки nzp_supp,nzp_serv
            //---------------------------------------------------
            ret = ExecSQL(conn_db,
                " Create temp table t_ins_ch " +
                " ( nzp_key   serial not null, " +
                "   nzp_kvar  integer, " +
                "   num_ls    integer, " +
                "   nzp_serv  integer, " +
                "   nzp_supp  integer, " +
                "   kod       integer  " +
                " ) " + sUnlogTempTable
                , true);
            if (!ret.result)
            {
                DropTempTablesCharge(conn_db);
                return false;
            }

            ExecSQL(conn_db, " Drop table t_ins_ch1 ", false);
            ret = ExecSQL(conn_db,
                " Create temp table t_ins_ch1 " +
                " ( nzp_kvar  integer, " +
                "   num_ls    integer, " +
                "   nzp_serv  integer, " +
                "   nzp_supp  integer  " +
                " ) " + sUnlogTempTable
            , true);
            if (!ret.result) { DropTempTablesCharge(conn_db); return false; }

            ret = ExecSQL(conn_db,
                " Insert into t_ins_ch1 (nzp_kvar,num_ls,nzp_supp,nzp_serv)  " +
                " Select nzp_kvar,num_ls,nzp_supp,nzp_serv From t_calc_gku group by 1,2,3,4 "
            , true);
            if (!ret.result) { DropTempTablesCharge(conn_db); return false; }

            if (!chargeXX.paramcalc.b_reval)
            {
                ret = ExecSQL(conn_db,
                    " Insert into t_ins_ch1 (nzp_kvar,num_ls,nzp_supp,nzp_serv)  " +
                    " Select nzp_kvar,num_ls,nzp_supp,nzp_serv From t_fn_supp group by 1,2,3,4 "
                , true);
                if (!ret.result) { DropTempTablesCharge(conn_db); return false; }

                ret = ExecSQL(conn_db,
                    " Insert into t_ins_ch1 (nzp_kvar,num_ls,nzp_supp,nzp_serv)  " +
                    " Select nzp_kvar,num_ls,nzp_supp,nzp_serv From t_from_supp group by 1,2,3,4 "
                , true);
                if (!ret.result) { DropTempTablesCharge(conn_db); return false; }

                ret = ExecSQL(conn_db,
                    " Insert into t_ins_ch1 (nzp_kvar,num_ls,nzp_supp,nzp_serv)  " +
                    " Select nzp_kvar,num_ls,nzp_supp,nzp_serv From t_del_supp group by 1,2,3,4 "
                , true);
                if (!ret.result) { DropTempTablesCharge(conn_db); return false; }

                ret = ExecSQL(conn_db,
                    " Insert into t_ins_ch1 (nzp_kvar,num_ls,nzp_supp,nzp_serv)  " +
                    " Select nzp_kvar,num_ls,nzp_supp,nzp_serv From t_prev_ch group by 1,2,3,4 "
                , true);
                if (!ret.result) { DropTempTablesCharge(conn_db); return false; }

                ret = ExecSQL(conn_db,
                    " Insert into t_ins_ch1 (nzp_kvar,num_ls,nzp_supp,nzp_serv)  " +
                    " Select nzp_kvar,num_ls,nzp_supp,nzp_serv From t_perek group by 1,2,3,4 "
                , true);
                if (!ret.result) { DropTempTablesCharge(conn_db); return false; }

                ret = ExecSQL(conn_db,
                    " Insert into t_ins_ch1 (nzp_kvar,num_ls,nzp_supp,nzp_serv)  " +
                    " Select nzp_kvar,num_ls,nzp_supp,nzp_serv From t_lnk_reval group by 1,2,3,4 "
                , true);
                if (!ret.result) { DropTempTablesCharge(conn_db); return false; }
            }
            ExecSQL(conn_db, sUpdStat + " t_ins_ch1 ", true);

            ret = ExecSQL(conn_db,
                " Insert into t_ins_ch (nzp_kvar,num_ls,nzp_supp,nzp_serv, kod)  " +
                " Select nzp_kvar,num_ls,nzp_supp,nzp_serv, 0 " +
                " From t_ins_ch1 " +
                " Group by 1,2,3,4 "
                , true);
            if (!ret.result)
            {
                DropTempTablesCharge(conn_db);
                return false;
            }

            ExecSQL(conn_db, " Create unique index ix_t_ins_ch1 on t_ins_ch (nzp_key) ", true);
            ExecSQL(conn_db, " Create unique index ix_t_ins_ch2 on t_ins_ch (nzp_kvar,nzp_serv,nzp_supp) ", true);
            ExecSQL(conn_db, " Create        index ix_t_ins_ch3 on t_ins_ch (kod) ", true);
            ExecSQL(conn_db, sUpdStat + " t_ins_ch ", true);


            ExecSQL(conn_db, " Drop table t_ins_ch1 ", false);
            ret = ExecSQL(conn_db,
                " Create temp table t_ins_ch1 " +
                " ( nzp_kvar  integer, " +
                "   nzp_serv  integer, " +
                "   nzp_supp  integer, " +
                "   cnt       integer  " +
                " ) " + sUnlogTempTable
            , true);
            if (!ret.result) { DropTempTablesCharge(conn_db); return false; }

            ret = ExecSQL(conn_db,
                " Insert into t_ins_ch1 (nzp_kvar,nzp_serv,nzp_supp, cnt)  " +
                " Select a.nzp_kvar,a.nzp_serv,a.nzp_supp, 1 as cnt " +
                " From " + chargeXX.charge_xx + " a, t_selkvar k" +
                " Where a.nzp_kvar=k.nzp_kvar " +
                    chargeXX.paramcalc.per_dat_charge +
                " Group by 1,2,3 "
                , true);
            if (!ret.result)
            {
                DropTempTablesCharge(conn_db);
                return false;
            }

            ExecSQL(conn_db, " Create unique index ix1_t_ins_ch1 on t_ins_ch1 (nzp_kvar,nzp_serv,nzp_supp) ", true);
            ExecSQL(conn_db, sUpdStat + " t_ins_ch1 ", true);

            ret = ExecSQL(conn_db,
                " Update t_ins_ch " +
                " Set kod = 1 " +
                " Where 1 = ( Select cnt From t_ins_ch1 ch " +
                            " Where t_ins_ch.nzp_kvar = ch.nzp_kvar " +
                            "   and t_ins_ch.nzp_serv = ch.nzp_serv " +
                            "   and t_ins_ch.nzp_supp = ch.nzp_supp ) "
                , true);
            if (!ret.result)
            {
                DropTempTablesCharge(conn_db);
                return false;
            }

            ExecSQL(conn_db, " Drop table t_ins_ch1 ", false);

            #endregion формирование записей для вставки


            //вставка недостающих строк в charge_xx
            string p_dat_charge = DateNullString;
            if (!chargeXX.paramcalc.b_cur)
                p_dat_charge = MDY(chargeXX.paramcalc.cur_mm, 28, chargeXX.paramcalc.cur_yy);

            sql =
                " Insert into " + chargeXX.charge_xx +
                   " ( dat_charge, nzp_kvar, num_ls, nzp_serv, nzp_supp, nzp_frm, tarif, tarif_p, gsum_tarif, rsum_tarif, rsum_lgota, sum_tarif, sum_dlt_tarif, sum_dlt_tarif_p, " +
                     " sum_tarif_p,sum_lgota, sum_dlt_lgota, sum_dlt_lgota_p, sum_lgota_p, sum_nedop, sum_nedop_p, sum_real, sum_charge, reval, real_pere, sum_pere, " +
                     " real_charge, sum_money, money_to, money_from, money_del, sum_fakt, fakt_to, fakt_from, fakt_del, sum_insaldo, izm_saldo, " +
                     " sum_outsaldo, sum_subsidy, sum_subsidy_p, sum_subsidy_reval, sum_subsidy_all, isblocked, is_device, c_calc, c_sn, c_okaz, " +
                     " c_nedop, isdel, c_reval, order_print) " +
                " Select " + p_dat_charge + ", nzp_kvar,num_ls, nzp_serv, nzp_supp, " +
                      "  0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0 " +
                " From t_ins_ch Where kod = 0 and not exists (select 1 from temp_prohibited_recalc pr " +
                        " where t_ins_ch.nzp_serv = pr.nzp_serv and t_ins_ch.nzp_supp = pr.nzp_supp and t_ins_ch.nzp_kvar = pr.nzp_kvar)";
            if (chargeXX.paramcalc.nzp_kvar > 0 || chargeXX.paramcalc.nzp_dom > 0)
            {
                ret = ExecSQL(conn_db, sql, true);
            }
            else
            {
                ExecByStep(conn_db, "t_ins_ch", "nzp_key", sql, 50000, " ", out ret);
            }

            if (!ret.result) { DropTempTablesCharge(conn_db); return false; }

            UpdateStatistics(false, paramcalc, chargeXX.charge_tab, out ret);
            if (!ret.result) { DropTempTablesCharge(conn_db); return false; }

            #endregion вставка дополнительных записей в chargeXX

            #region заполнить рассчитанные начислено, формулы и т.п...
            //---------------------------------------------------
            //заполнить рассчитанные начислено, формулы и т.п...
            //---------------------------------------------------
            string sqql =
                    " Update " + chargeXX.charge_xx +
                    " Set " +
                        "  (tarif,gsum_tarif,rsum_tarif,sum_tarif,nzp_frm,c_calc,c_okaz,tarif_f,sum_tarif_f,sum_tarif_sn_eot,sum_tarif_sn_f,c_sn," +
                          " sum_subsidy_all,sum_subsidy,is_device,real_pere,isdel) = " +
#if PG
                        "  (gk.tarif,gk.sum_tarif,gk.sum_tarif,gk.sum_tarif,gk.nzp_frm,gk.c_calc,gk.c_okaz,gk.tarif_f,gk.sum_tarif_f,gk.sum_tarif_sn_eot," +
                           "gk.sum_tarif_sn_f,gk.c_sn,gk.sum_subsidy_all,gk.sum_subsidy,gk.is_device,gk.real_pere,gk.isdel) " +
                        " From " +
                        " (Select max(tarif) tarif, sum(sum_tarif) sum_tarif, max(nzp_frm) nzp_frm, sum(c_calc) c_calc, " +
                        "  max( case when sum_tarif > 0 then " + DateTime.DaysInMonth(paramcalc.calc_yy, paramcalc.calc_mm) + " else 0 end ) c_okaz, " +
                        "  max(tarif_f) tarif_f, sum(sum_tarif_f) sum_tarif_f, sum(sum_tarif_sn_eot) sum_tarif_sn_eot, sum(sum_tarif_sn_f) sum_tarif_sn_f," +
                        "  sum(c_sn) c_sn, sum(sum_subsidy_all) sum_subsidy_all, sum(sum_subsidy) sum_subsidy, max(is_device) is_device,sum(real_pere) real_pere, 0 isdel " +
                        ", nzp_kvar, nzp_serv, nzp_supp " +
                        " From t_calc_gku group by nzp_kvar, nzp_serv, nzp_supp) gk " +
                        " Where gk.nzp_kvar = " + chargeXX.charge_xx + ".nzp_kvar " +
                        "   and gk.nzp_serv = " + chargeXX.charge_xx + ".nzp_serv " +
                        "   and gk.nzp_supp = " + chargeXX.charge_xx + ".nzp_supp "
#else
                        " (( " +
                        " Select max(tarif), sum(sum_tarif), sum(sum_tarif), sum(sum_tarif), max(nzp_frm), sum(c_calc), " +
                        "  max( case when sum_tarif > 0 then " + DateTime.DaysInMonth(paramcalc.calc_yy, paramcalc.calc_mm).ToString() + " else 0 end )" +
                        ", max(tarif_f), sum(sum_tarif_f), sum(sum_tarif_sn_eot), sum(sum_tarif_sn_f)," +
                        "  sum(c_sn), sum(sum_subsidy_all), sum(sum_subsidy), max(is_device), sum(real_pere), 0 " +
                        " From t_calc_gku gk " +
                        " Where gk.nzp_kvar = " + chargeXX.charge_xx + ".nzp_kvar " +
                        "   and gk.nzp_serv = " + chargeXX.charge_xx + ".nzp_serv " +
                        "   and gk.nzp_supp = " + chargeXX.charge_xx + ".nzp_supp )) " +
                    " Where exists( Select 1 From t_calc_gku gk " +
                        " Where gk.nzp_kvar = " + chargeXX.charge_xx + ".nzp_kvar " +
                        "   and gk.nzp_serv = " + chargeXX.charge_xx + ".nzp_serv " +
                        "   and gk.nzp_supp = " + chargeXX.charge_xx + ".nzp_supp ) "
#endif
                    + chargeXX.paramcalc.per_dat_charge
                    + " and " + chargeXX.charge_xx + "." + chargeXX.where_kvar; //nzp_kvar in ( Select nzp_kvar From t_selkvar ) ";
            if (chargeXX.paramcalc.nzp_kvar > 0 || chargeXX.paramcalc.nzp_dom > 0)
                ret = ExecSQL(conn_db, sqql, true);
            else
                ExecByStep(conn_db, chargeXX.charge_xx, "nzp_charge",
                    sqql, 50000, " ", out ret);

            if (!ret.result)
            {
                DropTempTablesCharge(conn_db);
                return false;
            }

            //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
            //при перерасчете исключить некоторые сальдовые таблицы (prekedka, del_supplier, etc)
            //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
            if (!chargeXX.paramcalc.b_reval)
            {
                //заполнить вх. сальдо
                sqql = " Update " + chargeXX.charge_xx +
                       " Set sum_insaldo = ( " +
                                        " Select sum(sum_outsaldo) From t_prev_ch gk " +
                                        " Where gk.nzp_kvar = " + chargeXX.charge_xx + ".nzp_kvar " +
                                        "   and gk.nzp_serv = " + chargeXX.charge_xx + ".nzp_serv " +
                                        "   and gk.nzp_supp = " + chargeXX.charge_xx + ".nzp_supp ) " +
                       " Where exists( Select 1 From t_prev_ch gk " +
                                       " Where gk.nzp_kvar = " + chargeXX.charge_xx + ".nzp_kvar " +
                                       "   and gk.nzp_serv = " + chargeXX.charge_xx + ".nzp_serv " +
                                       "   and gk.nzp_supp = " + chargeXX.charge_xx + ".nzp_supp ) "
                        + chargeXX.paramcalc.per_dat_charge
                        + " and " + chargeXX.where_kvar + where_prehibited_recalc;   //nzp_kvar in ( Select nzp_kvar From t_selkvar ) ";

                if (chargeXX.paramcalc.nzp_kvar > 0 || chargeXX.paramcalc.nzp_dom > 0)
                    ret = ExecSQL(conn_db, sqql, true);
                else
                    ExecByStep(conn_db, chargeXX.charge_xx, "nzp_charge",
                        sqql, 50000, " ", out ret);

                if (!ret.result)
                {
                    DropTempTablesCharge(conn_db);
                    return false;
                }

                //учесть перерасчеты
                //reval_xx
                sqql = " Update " + chargeXX.charge_xx +
                       " Set reval = ( Select sum(reval) From t_lnk_reval gk " +
                                     " Where gk.nzp_kvar = " + chargeXX.charge_xx + ".nzp_kvar " +
                                     "   and gk.nzp_serv = " + chargeXX.charge_xx + ".nzp_serv " +
                                     "   and gk.nzp_supp = " + chargeXX.charge_xx + ".nzp_supp ) " +
                       " Where exists(  Select 1 From t_lnk_reval gk " +
                                    " Where gk.nzp_kvar = " + chargeXX.charge_xx + ".nzp_kvar " +
                                    "   and gk.nzp_serv = " + chargeXX.charge_xx + ".nzp_serv " +
                                    "   and gk.nzp_supp = " + chargeXX.charge_xx + ".nzp_supp ) "
                        + chargeXX.paramcalc.per_dat_charge
                        + " and " + chargeXX.where_kvar + where_prehibited_recalc;   //nzp_kvar in ( Select nzp_kvar From t_selkvar ) ";

                if (chargeXX.paramcalc.nzp_kvar > 0 || chargeXX.paramcalc.nzp_dom > 0)
                    ret = ExecSQL(conn_db, sqql, true);
                else
                    ExecByStep(conn_db, chargeXX.charge_xx, "nzp_charge",
                        sqql, 50000, " ", out ret);

                if (!ret.result)
                {
                    DropTempTablesCharge(conn_db);
                    return false;
                }

                //учесть оплаты
                //fn_supplier
                sqql = " Update " + chargeXX.charge_xx +
                       " Set money_to = ( Select sum(money_to) From t_fn_supp gk " +
                                        " Where gk.nzp_kvar = " + chargeXX.charge_xx + ".nzp_kvar " +
                                        "   and gk.nzp_serv = " + chargeXX.charge_xx + ".nzp_serv " +
                                        "   and gk.nzp_supp = " + chargeXX.charge_xx + ".nzp_supp ) " +

                       " Where exists(     Select 1 From t_fn_supp gk " +
                                       " Where gk.nzp_kvar = " + chargeXX.charge_xx + ".nzp_kvar " +
                                       "   and gk.nzp_serv = " + chargeXX.charge_xx + ".nzp_serv " +
                                       "   and gk.nzp_supp = " + chargeXX.charge_xx + ".nzp_supp ) "
                        + chargeXX.paramcalc.per_dat_charge
                        + " and " + chargeXX.where_kvar + where_prehibited_recalc;   //nzp_kvar in ( Select nzp_kvar From t_selkvar ) ";

                if (chargeXX.paramcalc.nzp_kvar > 0 || chargeXX.paramcalc.nzp_dom > 0)
                    ret = ExecSQL(conn_db, sqql, true);
                else
                    ExecByStep(conn_db, chargeXX.charge_xx, "nzp_charge",
                        sqql, 50000, " ", out ret);

                if (!ret.result)
                {
                    DropTempTablesCharge(conn_db);
                    return false;
                }
                //from_supplier
                sqql = " Update " + chargeXX.charge_xx +
                       " Set money_from = ( " +
                                        " Select sum(money_from) From t_from_supp gk " +
                                        " Where gk.nzp_kvar = " + chargeXX.charge_xx + ".nzp_kvar " +
                                        "   and gk.nzp_serv = " + chargeXX.charge_xx + ".nzp_serv " +
                                        "   and gk.nzp_supp = " + chargeXX.charge_xx + ".nzp_supp ) " +
                       " Where exists( Select 1 From t_from_supp gk " +
                                       " Where gk.nzp_kvar = " + chargeXX.charge_xx + ".nzp_kvar " +
                                       "   and gk.nzp_serv = " + chargeXX.charge_xx + ".nzp_serv " +
                                       "   and gk.nzp_supp = " + chargeXX.charge_xx + ".nzp_supp ) "
                        + chargeXX.paramcalc.per_dat_charge
                        + " and " + chargeXX.where_kvar + where_prehibited_recalc;   //nzp_kvar in ( Select nzp_kvar From t_selkvar ) ";

                if (chargeXX.paramcalc.nzp_kvar > 0 || chargeXX.paramcalc.nzp_dom > 0)
                    ret = ExecSQL(conn_db, sqql, true);
                else
                    ExecByStep(conn_db, chargeXX.charge_xx, "nzp_charge",
                        sqql, 50000, " ", out ret);

                if (!ret.result)
                {
                    DropTempTablesCharge(conn_db);
                    return false;
                }
                //del_supplier
                sqql = " Update " + chargeXX.charge_xx +
                       " Set money_del = ( " +
                                        " Select sum(money_del) From t_del_supp gk " +
                                        " Where gk.nzp_kvar = " + chargeXX.charge_xx + ".nzp_kvar " +
                                        "   and gk.nzp_serv = " + chargeXX.charge_xx + ".nzp_serv " +
                                        "   and gk.nzp_supp = " + chargeXX.charge_xx + ".nzp_supp ) " +
                       " Where exists( Select 1 From t_del_supp gk " +
                                       " Where gk.nzp_kvar = " + chargeXX.charge_xx + ".nzp_kvar " +
                                       "   and gk.nzp_serv = " + chargeXX.charge_xx + ".nzp_serv " +
                                       "   and gk.nzp_supp = " + chargeXX.charge_xx + ".nzp_supp ) "
                       + chargeXX.paramcalc.per_dat_charge
                        + " and " + chargeXX.where_kvar + where_prehibited_recalc;   //nzp_kvar in ( Select nzp_kvar From t_selkvar ) ";

                if (chargeXX.paramcalc.nzp_kvar > 0 || chargeXX.paramcalc.nzp_dom > 0)
                    ret = ExecSQL(conn_db, sqql, true);
                else
                    ExecByStep(conn_db, chargeXX.charge_xx, "nzp_charge",
                        sqql, 50000, " ", out ret);

                if (!ret.result)
                {
                    DropTempTablesCharge(conn_db);
                    return false;
                }
                //perekidka
                sqql = " Update " + chargeXX.charge_xx +
                       " Set real_charge = ( " +
                                        " Select sum(real_charge) From t_perek gk " +
                                        " Where gk.nzp_kvar = " + chargeXX.charge_xx + ".nzp_kvar " +
                                        "   and gk.nzp_serv = " + chargeXX.charge_xx + ".nzp_serv " +
                                        "   and gk.nzp_supp = " + chargeXX.charge_xx + ".nzp_supp ) " +
                       " Where exists( Select 1 From t_perek gk " +
                                       " Where gk.nzp_kvar = " + chargeXX.charge_xx + ".nzp_kvar " +
                                       "   and gk.nzp_serv = " + chargeXX.charge_xx + ".nzp_serv " +
                                       "   and gk.nzp_supp = " + chargeXX.charge_xx + ".nzp_supp ) "
                        + chargeXX.paramcalc.per_dat_charge
                        + " and " + chargeXX.where_kvar + where_prehibited_recalc;   //nzp_kvar in ( Select nzp_kvar From t_selkvar ) ";

                if (chargeXX.paramcalc.nzp_kvar > 0 || chargeXX.paramcalc.nzp_dom > 0)
                    ret = ExecSQL(conn_db, sqql, true);
                else
                    ExecByStep(conn_db, chargeXX.charge_xx, "nzp_charge",
                        sqql, 50000, " ", out ret);

                if (!ret.result)
                {
                    DropTempTablesCharge(conn_db);
                    return false;
                }
            }

            #endregion заполнить рассчитанные начислено, формулы и т.п...

            #region учесть недопоставки
            //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
            //учесть недопоставки: rsum_tarif * nedo_xx.koef
            //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

            // обработать связаные недопоставки - % недопоставки полностью совпадает со связной услугой
            // для: ХВС для ГВС, Обслуживание лифтов
            string uni_serv =
                    " and gk.nzp_serv = ( " +
                    " case when " + chargeXX.charge_xx + ".nzp_serv = 14 and gk.nzp_kind in (8,46, 11,12,41, 2005) then 9 else " +
                        " case when " + chargeXX.charge_xx + ".nzp_serv = 14 and gk.nzp_kind = 75 and " + chargeXX.charge_xx + ".is_device = 0 then 9 else " +
                            " case when " + chargeXX.charge_xx + ".nzp_serv = 242 and gk.nzp_kind in (72, 2022) then 243 else " +
                                chargeXX.charge_xx + ".nzp_serv " +
                            " end " +
                        " end " +
                    " end ) ";

            //  1998|--- Запретить связную недопоставку 
            int is_svaz_nedo = 0;
            IDbCommand cmd = DBManager.newDbCommand(
                " Select count(*) From " + paramcalc.data_alias + "prm_5 Where nzp_prm = 1998 and is_actual<>100" +
                " and dat_s  <= " + MDY(paramcalc.cur_mm, 1, paramcalc.cur_yy) +
                " and dat_po >= " + MDY(paramcalc.cur_mm, 1, paramcalc.cur_yy)
                , conn_db);
            try
            {
                string scountvals = Convert.ToString(cmd.ExecuteScalar());
                is_svaz_nedo = Convert.ToInt32(scountvals);
            }
            catch
            {
                is_svaz_nedo = 0;
            }
            if (is_svaz_nedo > 0)
            {
                // связаные недопоставки - запрещены
                // учет только по явно указанной услуге
                uni_serv = " and gk.nzp_serv = " + chargeXX.charge_xx + ".nzp_serv ";
            }

            //tab = chargeXX.paramcalc.pref + "_charge_" + (chargeXX.paramcalc.calc_yy - 2000).ToString("00") + ":nedo_" + (chargeXX.paramcalc.calc_mm).ToString("00");
            string sKolHourNedo =
#if PG
                " ( extract(days from (cnts - cnts_del))*24 + extract(hour from (cnts - cnts_del)) ) ";
#else
                chargeXX.paramcalc.data_alias + "sortnum((cnts - cnts_del)) ";
#endif

            sqql = " Update " + chargeXX.charge_xx +
                   " Set sum_nedop = rsum_tarif * ( " +
                                    " Select sum(koef * " + sKolHourNedo + " ) " +
                                    " From t_nedo_xx gk " +
                                    " Where gk.nzp_kvar = " + chargeXX.charge_xx + ".nzp_kvar " +
                                    uni_serv +
                                    " ) " +
                      " ,c_nedop =  ( " +
                                    " Select sum(" + sKolHourNedo + " / 24 ) " +
                                    " From t_nedo_xx gk " +
                                    " Where gk.nzp_kvar = " + chargeXX.charge_xx + ".nzp_kvar " +
                                    uni_serv +
                                    " ) " +
                   " Where exists ( Select 1 From t_nedo_xx gk " +
                               " Where gk.nzp_kvar = " + chargeXX.charge_xx + ".nzp_kvar " +
                                   uni_serv +
                               " ) " + chargeXX.paramcalc.per_dat_charge +
                     " and " + chargeXX.where_kvar + where_prehibited_recalc;   //nzp_kvar in ( Select nzp_kvar From t_selkvar ) ";

            if (chargeXX.paramcalc.nzp_kvar > 0 || chargeXX.paramcalc.nzp_dom > 0)
                ret = ExecSQL(conn_db, sqql, true);
            else
                ExecByStep(conn_db, chargeXX.charge_xx, "nzp_charge",
                    sqql, 50000, " ", out ret);

            if (!ret.result)
            {
                DropTempTablesCharge(conn_db);
                return false;
            }


            // если разрешена связная недопоставка - учет связных недопоставок раздельно
            // - только для канализации - ХВС отключено , т.к. в КП 5.0 по ХВС учитывается расход ХВС без ГВС
            if (is_svaz_nedo == 0)
            {

                // ... недопоставки по части расхода ХВС (real_pere) - для недопоставок по ГВС
                // -9991 - заведомо не существующая услуга для пропуска не перечисленных типов недопоставок
                uni_serv =
                        " and gk.nzp_serv = ( " +
                        " case when " + chargeXX.charge_xx + ".nzp_serv in (7,324,353) and gk.nzp_kind in (1,2002) " +
                             " then 6 else -9991 " +
                        " end ) ";

                sqql = " Update " + chargeXX.charge_xx +
                       " Set sum_nedop = sum_nedop + real_pere * ( " +
                                        " Select sum(koef * " + sKolHourNedo + " ) " +
                                        " From t_nedo_xx gk " +
                                        " Where gk.nzp_kvar = " + chargeXX.charge_xx + ".nzp_kvar " +
                                        uni_serv +
                                        " ) " +
                            " ,c_nedop =  ( " +
                                        " Select sum(" + sKolHourNedo + " / 24 ) " +
                                        " From t_nedo_xx gk " +
                                        " Where gk.nzp_kvar = " + chargeXX.charge_xx + ".nzp_kvar " +
                                        uni_serv +
                                        " ) " +
                       " Where exists ( Select 1 From t_nedo_xx gk " +
                                   " Where gk.nzp_kvar = " + chargeXX.charge_xx + ".nzp_kvar " +
                                   uni_serv +
                                   " ) " + chargeXX.paramcalc.per_dat_charge +
                         " and nzp_serv in (7,324,353) " +
                         " and " + chargeXX.where_kvar + where_prehibited_recalc;   //nzp_kvar in ( Select nzp_kvar From t_selkvar ) ";

                if (chargeXX.paramcalc.nzp_kvar > 0 || chargeXX.paramcalc.nzp_dom > 0)
                    ret = ExecSQL(conn_db, sqql, true);
                else
                    ExecByStep(conn_db, chargeXX.charge_xx, "nzp_charge",
                        sqql, 50000, " ", out ret);

                if (!ret.result)
                {
                    DropTempTablesCharge(conn_db);
                    return false;
                }

                // ... недопоставки по части расхода ГВС (rsum_tarif - real_pere) - для недопоставок по ХВС
                // -9991 - заведомо не существующая услуга для пропуска не перечисленных типов недопоставок
                uni_serv =
                        " and gk.nzp_serv = ( " +
                        " case when " + chargeXX.charge_xx + ".nzp_serv in (6,7,324,353) and gk.nzp_kind in (8,46, 2005) then 9 else " +
                            " case when " + chargeXX.charge_xx + ".nzp_serv in (6,7,324,353) and gk.nzp_kind = 75 and " + chargeXX.charge_xx + ".is_device = 0" +
                                 " then 9 else -9991 " +
                            " end " +
                        " end ) ";

                sqql = " Update " + chargeXX.charge_xx +
                       " Set sum_nedop = sum_nedop + (rsum_tarif - real_pere) * ( " +
                                        " Select sum(koef * " + sKolHourNedo + " ) " +
                                        " From t_nedo_xx gk " +
                                        " Where gk.nzp_kvar = " + chargeXX.charge_xx + ".nzp_kvar " +
                                        uni_serv +
                                        " ) " +
                            " ,c_nedop =  ( " +
                                        " Select sum(" + sKolHourNedo + " / 24 ) " +
                                        " From t_nedo_xx gk " +
                                        " Where gk.nzp_kvar = " + chargeXX.charge_xx + ".nzp_kvar " +
                                        uni_serv +
                                        " ) " +
                       " Where exists ( Select 1 From t_nedo_xx gk " +
                                   " Where gk.nzp_kvar = " + chargeXX.charge_xx + ".nzp_kvar " +
                                   uni_serv +
                                   " ) " + chargeXX.paramcalc.per_dat_charge +
                         " and nzp_serv in (7,324,353) " +
                         " and " + chargeXX.where_kvar + where_prehibited_recalc;   //nzp_kvar in ( Select nzp_kvar From t_selkvar ) ";

                if (chargeXX.paramcalc.nzp_kvar > 0 || chargeXX.paramcalc.nzp_dom > 0)
                    ret = ExecSQL(conn_db, sqql, true);
                else
                    ExecByStep(conn_db, chargeXX.charge_xx, "nzp_charge",
                        sqql, 50000, " ", out ret);

                if (!ret.result)
                {
                    DropTempTablesCharge(conn_db);
                    return false;
                }
            }
            // ..................................................................................
            sqql = " Update " + chargeXX.charge_xx +
                   " Set sum_nedop = case when (rsum_tarif - sum_nedop < 0) and (rsum_tarif >= 0) then rsum_tarif else sum_nedop end " +
                   " Where " + chargeXX.where_kvar +  //nzp_kvar in ( Select nzp_kvar From t_selkvar ) " +
                   "   and abs(sum_nedop) > 0 "
                    + chargeXX.paramcalc.per_dat_charge + where_prehibited_recalc;

            if (chargeXX.paramcalc.nzp_kvar > 0 || chargeXX.paramcalc.nzp_dom > 0)
                ret = ExecSQL(conn_db, sqql, true);
            else
                ExecByStep(conn_db, chargeXX.charge_xx, "nzp_charge",
                    sqql, 50000, " ", out ret);

            #region учет повышающих коэффициентов по нормативам

            ExecSQL(conn_db, " drop table t_nedop_pk ", false);
            ret = ExecSQL(conn_db,
                " CREATE TEMP TABLE t_nedop_pk( " +
                " nzp_kvar    INTEGER, " +
                " nzp_serv    INTEGER, " +
                " nzp_serv_pk INTEGER, " +
                " nzp_supp    INTEGER, " +
                " koef_nedo   " + sDecimalType + "(10,8) default 0.00," +
                " rsum_tarif  " + sDecimalType + "(14,2) default 0.00, " +
                " sum_nedop   " + sDecimalType + "(14,2) default 0.00, " +
                " is_pk_hv    INTEGER default 0, " +
                " is_pk_gv    INTEGER default 0  " +
                " )  " + sUnlogTempTable
                , true, 6000);
            if (!ret.result) { DropTempTablesCharge(conn_db); return false; }

            sql =
                " Insert Into t_nedop_pk(nzp_kvar,nzp_serv,nzp_serv_pk,nzp_supp,koef_nedo,rsum_tarif,sum_nedop) " +
                " Select k.nzp_kvar,a.nzp_serv,d.nzp_serv,a.nzp_supp,a.sum_nedop/a.rsum_tarif,a.rsum_tarif,a.sum_nedop " +
                " From " + chargeXX.charge_xx + " a, t_selkvar k, " + Points.Pref + "_kernel.serv_norm_koef d" +
                " Where a.nzp_kvar = k.nzp_kvar and a.nzp_serv > 1 and a.nzp_serv=d.nzp_serv_link " +
                "   and a." + chargeXX.where_kvar +  //nzp_kvar in ( Select nzp_kvar From t_selkvar ) " +
                "   and abs(a.sum_nedop) > 0.0001 and abs(a.rsum_tarif) > 0.0001 " +
                chargeXX.paramcalc.per_dat_charge;

            ret = ExecSQL(conn_db, sql, true, 6000);
            if (!ret.result) { DropTempTablesCharge(conn_db); return false; }

            ExecSQL(conn_db, " Create unique index ix_t_nedop_pk on t_nedop_pk (nzp_kvar,nzp_serv_pk,nzp_supp) ", true);
            ExecSQL(conn_db, sUpdStat + " t_nedop_pk ", true);

            // ... ПК...
            // если разрешена связная недопоставка - учет связных недопоставок раздельно
            // - только для канализации - ХВС отключено , т.к. в КП 5.0 по ХВС учитывается расход ХВС без ГВС
            if (is_svaz_nedo == 0)
            {
                sqql = " Update t_nedop_pk " +
                       " Set is_pk_hv = 1 " +
                       " Where exists ( Select 1 From " + chargeXX.charge_xx + " a, " + Points.Pref + "_kernel.serv_norm_koef d " +
                                   " Where a.nzp_kvar = t_nedop_pk.nzp_kvar and a.nzp_serv=d.nzp_serv and d.nzp_serv_link=6 ) " +
                         " and t_nedop_pk.nzp_serv in (7,324,353) ";
                ret = ExecSQL(conn_db, sqql, true);
                if (!ret.result) { DropTempTablesCharge(conn_db); return false; }

                sqql = " Update t_nedop_pk " +
                       " Set is_pk_gv = 1 " +
                       " Where exists ( Select 1 From " + chargeXX.charge_xx + " a, " + Points.Pref + "_kernel.serv_norm_koef d " +
                                   " Where a.nzp_kvar = t_nedop_pk.nzp_kvar and a.nzp_serv=d.nzp_serv and d.nzp_serv_link=9 ) " +
                         " and t_nedop_pk.nzp_serv in (7,324,353) ";
                ret = ExecSQL(conn_db, sqql, true);
                if (!ret.result) { DropTempTablesCharge(conn_db); return false; }

                sqql = " Update t_nedop_pk " +
                       " Set koef_nedo = 0 " +
                       " Where nzp_serv in (7,324,353) and (is_pk_hv+is_pk_gv)=1 ";
                ret = ExecSQL(conn_db, sqql, true);
                if (!ret.result) { DropTempTablesCharge(conn_db); return false; }

                // ... недопоставки по части расхода ХВС (real_pere) - для недопоставок по ГВС
                // -9991 - заведомо не существующая услуга для пропуска не перечисленных типов недопоставок
                uni_serv =
                        " and gk.nzp_serv = 6 and gk.nzp_kind in (1,2002) ";

                sqql = " Update t_nedop_pk " +
                       " Set koef_nedo = ( " +
                                        " Select sum(koef * " + sKolHourNedo + " ) " +
                                        " From t_nedo_xx gk " +
                                        " Where gk.nzp_kvar = t_nedop_pk.nzp_kvar " +
                                        uni_serv +
                                        " ) " +
                       " Where exists ( Select 1 From t_nedo_xx gk " +
                                   " Where gk.nzp_kvar = t_nedop_pk.nzp_kvar " +
                                   uni_serv +
                                   " ) " +
                         " and nzp_serv in (7,324,353) and is_pk_hv=1 and is_pk_gv=0 ";
                ret = ExecSQL(conn_db, sqql, true);
                if (!ret.result) { DropTempTablesCharge(conn_db); return false; }

                // ... недопоставки по части расхода ГВС (rsum_tarif - real_pere) - для недопоставок по ХВС
                // -9991 - заведомо не существующая услуга для пропуска не перечисленных типов недопоставок
                uni_serv =
                        " and gk.nzp_serv = 9 and gk.nzp_kind in (8,46, 2005, 75) ";

                sqql = " Update t_nedop_pk " +
                       " Set koef_nedo = ( " +
                                        " Select sum(koef * " + sKolHourNedo + " ) " +
                                        " From t_nedo_xx gk " +
                                        " Where gk.nzp_kvar = t_nedop_pk.nzp_kvar " +
                                        uni_serv +
                                        " ) " +
                       " Where exists ( Select 1 From t_nedo_xx gk " +
                                   " Where gk.nzp_kvar = t_nedop_pk.nzp_kvar " +
                                   uni_serv +
                                   " ) " +
                         " and nzp_serv in (7,324,353) and is_pk_hv=0 and is_pk_gv=1 ";
                ret = ExecSQL(conn_db, sqql, true);
                if (!ret.result) { DropTempTablesCharge(conn_db); return false; }
            }
            ret = ExecSQL(conn_db,
                " Update " + chargeXX.charge_xx +
                " Set sum_nedop = round(n.koef_nedo * " + chargeXX.charge_xx + ".rsum_tarif,2) " +
                " From t_nedop_pk n " +
                " Where " + chargeXX.charge_xx + ".nzp_kvar=n.nzp_kvar " +
                  " and " + chargeXX.charge_xx + ".nzp_serv=n.nzp_serv_pk " +
                chargeXX.paramcalc.per_dat_charge + where_prehibited_recalc
                , true);
            if (!ret.result) { DropTempTablesCharge(conn_db); return false; }

            ExecSQL(conn_db, " drop table t_nedop_pk ", false);

            #endregion учет повышающих коэффициентов по нормативам

            sqql = " Update " + chargeXX.charge_xx +
                   " Set sum_tarif = rsum_tarif - sum_nedop" +
                      ", sum_tarif_f      = CASE WHEN rsum_tarif>0 THEN sum_tarif_f      * ((rsum_tarif - sum_nedop)/rsum_tarif) ELSE 0 END" +
                      ", sum_tarif_sn_eot = CASE WHEN rsum_tarif>0 THEN sum_tarif_sn_eot * ((rsum_tarif - sum_nedop)/rsum_tarif) ELSE 0 END" +
                      ", sum_tarif_sn_f   = CASE WHEN rsum_tarif>0 THEN sum_tarif_sn_f   * ((rsum_tarif - sum_nedop)/rsum_tarif) ELSE 0 END " +
                      ", sum_subsidy      = CASE WHEN rsum_tarif>0 THEN sum_subsidy      * ((rsum_tarif - sum_nedop)/rsum_tarif) ELSE 0 END " +
                      ",c_okaz = ( case when c_okaz - c_nedop > 0 then c_okaz - c_nedop else 0 end ) " +
                   " Where " + chargeXX.where_kvar + //nzp_kvar in ( Select nzp_kvar From t_selkvar ) " +
                   "  and abs(sum_nedop) > 0 "
                   + chargeXX.paramcalc.per_dat_charge + where_prehibited_recalc;

            if (chargeXX.paramcalc.nzp_kvar > 0 || chargeXX.paramcalc.nzp_dom > 0)
                ret = ExecSQL(conn_db, sqql, true);
            else
                ExecByStep(conn_db, chargeXX.charge_xx, "nzp_charge",
                    sqql, 50000, " ", out ret);

            if (!ret.result)
            {
                DropTempTablesCharge(conn_db);
                return false;
            }
            #endregion учесть недопоставки

            #region сформировать начислено за месяц
            //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
            // сформировать начислено за месяц: sum_real
            //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

            sqql = " Update " + chargeXX.charge_xx + " Set ";

            if (bIsCalcSubsidy)
            {
                sqql = sqql + " sum_real = sum_subsidy ";
            }
            else
            {
                sqql = sqql + " sum_real = sum_tarif - sum_subsidy - sum_lgota ";
            }
            sqql = sqql +
                   " Where " + chargeXX.where_kvar + //nzp_kvar in ( Select nzp_kvar From t_selkvar ) " +
                //"   and sum_nedop > 0 " +
                   chargeXX.paramcalc.per_dat_charge + where_prehibited_recalc;

            if (chargeXX.paramcalc.nzp_kvar > 0 || chargeXX.paramcalc.nzp_dom > 0)
                ret = ExecSQL(conn_db, sqql, true);
            else
                ExecByStep(conn_db, chargeXX.charge_xx, "nzp_charge",
                    sqql, 50000, " ", out ret);

            if (!ret.result)
            {
                DropTempTablesCharge(conn_db);
                return false;
            }
            #endregion сформировать начислено за месяц

            //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
            //при перерасчете исключить некоторые сальдовые таблицы (prekedka, del_supplier, etc)
            //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
            if (!chargeXX.paramcalc.b_reval)
            {
                #region расчет сальдо
                //---------------------------------------------------
                //расчет сальдо
                //---------------------------------------------------
                sqql = " Update " + chargeXX.charge_xx +
                       " Set sum_money = money_to + money_from + money_del " +
                          " ,sum_outsaldo = sum_insaldo + real_charge + sum_real + reval + izm_saldo - (money_to + money_from + money_del) " +
                    //" ,real_pere = sum_insaldo + izm_saldo + real_charge - (money_to + money_from + money_del) " +
                          " ,sum_pere  = sum_insaldo + reval + real_charge - (money_to + money_from + money_del) " +
                       " Where " + chargeXX.where_kvar + //nzp_kvar in ( Select nzp_kvar From t_selkvar ) "
                        chargeXX.paramcalc.per_dat_charge + where_prehibited_recalc;

                if (chargeXX.paramcalc.nzp_kvar > 0 || chargeXX.paramcalc.nzp_dom > 0)
                    ret = ExecSQL(conn_db, sqql, true);
                else
                    ExecByStep(conn_db, chargeXX.charge_xx, "nzp_charge",
                        sqql, 50000, " ", out ret);

                if (!ret.result)
                {
                    DropTempTablesCharge(conn_db);
                    return false;
                }
                #endregion расчет сальдо

                #region перераспределение переплат
                //---------------------------------------------------
                //перераспределение переплат
                //---------------------------------------------------
                //Алгоритм следуюущий:
                // 1 - выбрать все строки с sum_outsaldo < 0
                // 2 - по этим лс выбрать суммы по услуге sum_outsaldo > 0
                // 3 - выбратьм max(sum_outsaldo>0), чтобы туда переложить минус
                // 4 - переложить в пределах суммы переплаты не более суммы > 0 (заполнить sum_del)
                // 5 - цикл повторяется (sum_del увеличивается), пока есть куда перекладывать
                // anes

                IDataReader reader;

                ret = ExecRead(conn_db, out reader,
                    " Select * " +
                    " From " + chargeXX.paramcalc.data_alias + "prm_5 p " +
                    " Where p.nzp_prm = 1996 and p.val_prm='1' " +
                    "   and p.is_actual <> 100 and p.dat_s  <= " + chargeXX.paramcalc.dat_po +
                    "   and p.dat_po >= " + chargeXX.paramcalc.dat_s + " "
                    , true);
                if (!ret.result) { DropTempTablesCharge(conn_db); return false; }

                bool b_make_del_supplier = false;
                if (reader.Read())
                {
                    b_make_del_supplier = true;
                }
                reader.Close();

                // ... beg anes - отмена перераспределения оплат
                if (b_make_del_supplier)
                {
                    //загнать следы в del_supplier
                    ExecSQL(conn_db, " Drop table t_perek ", false);

                    ret = ExecSQL(conn_db,
                        " Create temp table t_perek " +
                        " ( nzp_key    serial not null, " +
                        "   nzp_kvar   integer, " +
                        "   num_ls     integer, " +
                        "   nzp_serv   integer, " +
                        "   nzp_supp   integer, " +
                        "   nzp_charge integer, " +
                        "   sum_out    " + sDecimalType + "(14,2) default 0.00, " +
                        "   sum_del    " + sDecimalType + "(14,2) default 0.00 " +
                        " )  " + sUnlogTempTable
                        , true);
                    if (!ret.result) { DropTempTablesCharge(conn_db); return false; }

                    ret = ExecSQL(conn_db,
                        " Insert into t_perek (nzp_kvar,num_ls,nzp_supp,nzp_serv,nzp_charge, sum_del,sum_out) " +
                        " Select a.nzp_kvar,a.num_ls,a.nzp_supp,a.nzp_serv,a.nzp_charge, 0 sum_del, a.sum_outsaldo " +
                        " From " + chargeXX.charge_xx + " a, t_selkvar k " +
                        " Where a.nzp_kvar=k.nzp_kvar "
                         + chargeXX.paramcalc.per_dat_charge +
                        "   and a.sum_outsaldo < 0 "
                        , true);
                    if (!ret.result) { DropTempTablesCharge(conn_db); return false; }

                    ExecSQL(conn_db, " Create unique index ix_t_perek1 on t_perek (nzp_key) ", true);
                    ExecSQL(conn_db, " Create unique index ix_t_perek2 on t_perek (nzp_charge) ", true);
                    ExecSQL(conn_db, " Create unique index ix_t_perek3 on t_perek (nzp_kvar,nzp_serv,nzp_supp) ", true);
                    ExecSQL(conn_db, sUpdStat + " t_perek ", true);

                    //загоним положительные суммы по услуге
                    ExecSQL(conn_db, " Drop table t_ins_ch1 ", false);

                    ret = ExecSQL(conn_db,
                        " Select ch.nzp_kvar,ch.num_ls,ch.nzp_supp,ch.nzp_serv,ch.nzp_charge, 0 sum_del, ch.sum_outsaldo " +
#if PG
 " Into temp t_ins_ch1 " +
#else
                        "  "+
#endif
 " From " + chargeXX.charge_xx + " ch, t_selkvar k " +
                        " Where ch.nzp_kvar=k.nzp_kvar " + chargeXX.paramcalc.per_dat_charge +
                        "   and ch.sum_outsaldo > 0 " +
                        "   and 0 < ( Select count(*) From t_perek gk " +
                                    " Where gk.nzp_kvar = ch.nzp_kvar " +
                                    "   and gk.nzp_serv = ch.nzp_serv ) " +
#if PG
 " "
#else
                        " Into temp t_ins_ch1 With no log "
#endif
, true);
                    if (!ret.result) { DropTempTablesCharge(conn_db); return false; }

                    ret = ExecSQL(conn_db,
                        " Insert into t_perek (nzp_kvar,num_ls,nzp_supp,nzp_serv,nzp_charge, sum_del,sum_out) " +
                        " Select nzp_kvar,num_ls,nzp_supp,nzp_serv,nzp_charge, sum_del, sum_outsaldo " +
                        " From t_ins_ch1 "
                        , true);
                    if (!ret.result) { DropTempTablesCharge(conn_db); return false; }

                    ExecSQL(conn_db, sUpdStat + " t_perek ", true);


                    //---------------------------------------------------
                    //начинаем циклить, пока не все суммы взаимоучтем
                    //---------------------------------------------------
                    //IDataReader reader;
                    bool yes_del = false;

                    while (true)
                    {
                        ExecSQL(conn_db, " Drop table t_delmin ", false);
                        ExecSQL(conn_db, " Drop table t_delplu ", false);
                        ExecSQL(conn_db, " Drop table t_ins_ch1 ", false);

                        //ищем минимальный минус
                        ret = ExecSQL(conn_db,
                        " Select nzp_kvar, nzp_serv, max(nzp_charge) as nzp_charge, max(sum_out+sum_del) as sum_del " +
#if PG
 " Into temp t_delmin " +
#else
                        "  "+
#endif
 " From t_perek a " +
                        " Where (sum_out+sum_del) = ( Select min(sum_out+sum_del) From t_perek b " +
                                                    " Where a.nzp_kvar = b.nzp_kvar " +
                                                    "   and a.nzp_serv = b.nzp_serv " +
                                                    "   and sum_out+sum_del < 0 ) " +
                        "   and sum_out+sum_del < 0 " +
                        " Group by 1,2 " +
#if PG
 "  "
#else
                        " Into temp t_delmin With no log "
#endif
, true);
                        if (!ret.result) { DropTempTablesCharge(conn_db); return false; }

                        ExecSQL(conn_db, " Create unique index ix_t_delmin on t_delmin (nzp_kvar,nzp_serv) ", true);
                        ExecSQL(conn_db, sUpdStat + " t_delmin ", true);

                        //ищем максимальный плюс
                        ret = ExecSQL(conn_db,
                        " Select nzp_kvar, nzp_serv, max(nzp_charge) as nzp_charge, max(sum_out+sum_del) as sum_del " +
#if PG
 " Into temp t_delplu " +
#else
                        "  "+
#endif
 " From t_perek a " +
                        " Where (sum_out+sum_del) = ( Select max(sum_out+sum_del) From t_perek b " +
                                                    " Where a.nzp_kvar = b.nzp_kvar " +
                                                    "   and a.nzp_serv = b.nzp_serv " +
                                                    "   and sum_out+sum_del > 0 ) " +
                        "   and sum_out+sum_del > 0 " +
                        " Group by 1,2 " +
#if PG
 "  "
#else
                        " Into temp t_delplu With no log "
#endif
, true);
                        if (!ret.result) { DropTempTablesCharge(conn_db); return false; }

                        ExecSQL(conn_db, " Create unique index ix_t_delplu on t_delplu (nzp_kvar,nzp_serv) ", true);
                        ExecSQL(conn_db, sUpdStat + " t_delplu ", true);

                        //соединяем между собой эти таблицы и вычисляем дельты в пределах общих абсолютных значениях

                        ret = ExecSQL(conn_db,
                        " Select a.nzp_charge as nzp_min, b.nzp_charge as nzp_plu, " +
                           " case when a.sum_del + b.sum_del > 0 then abs(a.sum_del) else b.sum_del end as sum_dlt " +
#if PG
 " Into temp t_ins_ch1 " +
#else
                        "  "+
#endif
 " From t_delmin a, t_delplu b " +
                        " Where a.nzp_kvar = b.nzp_kvar " +
                        "   and a.nzp_serv = b.nzp_serv " +
#if PG
 "  "
#else
                        " Into temp t_ins_ch1 With no log "
#endif
, true);
                        if (!ret.result) { DropTempTablesCharge(conn_db); return false; }

                        ExecSQL(conn_db, " Create index ix_t_ins_ch1_1 on t_ins_ch1 (nzp_min) ", true);
                        ExecSQL(conn_db, " Create index ix_t_ins_ch1_2 on t_ins_ch1 (nzp_plu) ", true);
                        ExecSQL(conn_db, sUpdStat + " t_ins_ch1 ", true);

                        bool b = false;

                        ret = ExecRead(conn_db, out reader,
                            " Select count(*) as cnt From t_ins_ch1  "
                            , true);
                        if (!ret.result)
                        {
                            DropTempTablesNedo(conn_db);
                            return false;
                        }
                        try
                        {
                            if (reader.Read())
                            {
                                if (reader["cnt"] != DBNull.Value)
                                    b = Convert.ToInt32(reader["cnt"]) > 0;
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

                        if (b)
                        {
                            //есть суммы для перекидки
                            ret = ExecSQL(conn_db,
                            " Update t_perek " +
                            " Set sum_del = sum_del + ( Select sum_dlt From t_ins_ch1 Where t_perek.nzp_charge = t_ins_ch1.nzp_min ) " +
                            " Where nzp_charge in ( Select nzp_min From t_ins_ch1 ) "
                            , true);
                            if (!ret.result) { DropTempTablesCharge(conn_db); return false; }

                            ret = ExecSQL(conn_db,
                            " Update t_perek " +
                            " Set sum_del = sum_del - ( Select sum_dlt From t_ins_ch1 Where t_perek.nzp_charge = t_ins_ch1.nzp_plu ) " +
                            " Where nzp_charge in ( Select nzp_plu From t_ins_ch1 ) "
                            , true);
                            if (!ret.result) { DropTempTablesCharge(conn_db); return false; }

                            yes_del = true;
                        }
                        else
                            break; //нет, выходим из цикла
                    }

                    ExecSQL(conn_db, " Drop table t_delmin ", false);
                    ExecSQL(conn_db, " Drop table t_delplu ", false);
                    ExecSQL(conn_db, " Drop table t_ins_ch1 ", false);

                    if (yes_del)
                    {
                        //были произведены перекидки, надо изменить charge_xx и сохранить суммы в del_supplier
                        ret = ExecSQL(conn_db,
                            " Update " + chargeXX.charge_xx +
                            " Set money_del = money_del + ( Select (-1)*sum_del From t_perek a Where a.nzp_charge = " + chargeXX.charge_xx + ".nzp_charge  ) " +
                            " Where nzp_charge in ( Select nzp_charge From t_perek ) " + where_prehibited_recalc
                            , true);
                        if (!ret.result) { DropTempTablesCharge(conn_db); return false; }

                        ret = ExecSQL(conn_db,
                            " Update " + chargeXX.charge_xx +
                            " Set sum_money = money_to + money_from + money_del " +
                               " ,sum_outsaldo = sum_insaldo +  real_charge + sum_real + reval + izm_saldo - (money_to + money_from + money_del) " +
                            " Where nzp_charge in ( Select nzp_charge From t_perek ) " + where_prehibited_recalc
                            , true);
                        if (!ret.result) { DropTempTablesCharge(conn_db); return false; }

                        //загнать следы в del_supplier
                        ret = ExecSQL(conn_db,
                            " Insert into " + chargeXX.paramcalc.pref + "_charge_" + (chargeXX.paramcalc.calc_yy - 2000).ToString("00") + tableDelimiter + "del_supplier" +
                              " (num_ls, nzp_serv, nzp_supp, sum_prih, kod_sum, dat_account, dat_calc) " +
                            " Select num_ls, nzp_serv, nzp_supp, sum_del, 39, " + MDY(chargeXX.paramcalc.calc_mm, 28, chargeXX.paramcalc.calc_yy) + "," + sCurDate +
                            " From t_perek " +
                            " Where abs(sum_del) > 0.000001 "
                            , true);
                        if (!ret.result) { DropTempTablesCharge(conn_db); return false; }
                    }

                }
                // ... end anes - отмена перераспределения оплат
                #endregion перераспределение переплат

                //---------------------------------------------------
                //расчет итого
                //---------------------------------------------------

                #region начислено к оплате

                bool bDoExecStep = !(chargeXX.paramcalc.nzp_kvar > 0 || chargeXX.paramcalc.nzp_dom > 0);
                string sWhere = chargeXX.where_kvar; //nzp_kvar in ( Select nzp_kvar From t_selkvar ) "
                string sDatCharge = chargeXX.paramcalc.per_dat_charge;
                string sChargeName = chargeXX.charge_xx;

                CalcSumToPay(conn_db, chargeXX.paramcalc.cur_mm, chargeXX.paramcalc.cur_yy, chargeXX.paramcalc.pref, sChargeName, sWhere, sDatCharge, bDoExecStep, out ret);
                if (!ret.result) { DropTempTablesCharge(conn_db); return false; }
                /*
                // TEST
                sWhere = chargeXX.where_kvar + //nzp_kvar in ( Select nzp_kvar From t_selkvar ) "
                        "";
                SetPayToPrev(conn_db, chargeXX.paramcalc.cur_mm, chargeXX.paramcalc.cur_yy, chargeXX.paramcalc.pref, sChargeName, sWhere, bDoExecStep, out ret);
                */
                // TEST
                /*
                string sdataMain = Points.Pref + "_data" + tableDelimiter;

                // ... взять вид начислено к оплате ... 
                IDbCommand cmd_no = DBManager.newDbCommand(
                    " select max(p.val_prm) " +
                    " from " + sdataMain + "prm_10 p " +
                    " where p.nzp_prm=1134 " +
                    " and p.is_actual<>100 and p.dat_s<" + chargeXX.paramcalc.dat_po + " and p.dat_po>=" + chargeXX.paramcalc.dat_s + " "
                , conn_db);

                int iKodNachOpl = 0;
                try
                {
                    string snovals = Convert.ToString(cmd_no.ExecuteScalar());
                    iKodNachOpl = Convert.ToInt32(snovals);
                }
                catch
                {
                    iKodNachOpl = 0;
                }

                sqql = " Update " + chargeXX.charge_xx +
                    //" Set sum_charge = ( case when sum_outsaldo > 0 then sum_outsaldo else 0 end ) " +
                    //" Set sum_charge = ( case when sum_real + real_charge + reval > 0 then sum_real + real_charge + reval else 0 end ) " +
                       " Set sum_charge = ";

                switch (iKodNachOpl)
                {
                    case 1: sqql = sqql + " sum_outsaldo ";
                    break;
                    case 2: sqql = sqql + " case when sum_outsaldo>0 then sum_outsaldo else 0 end ";
                    break;
                    case 3: sqql = sqql + " sum_real + real_charge + reval + (case when sum_insaldo<0 then sum_insaldo else 0 end)";
                    break;
                    case 4: sqql = sqql + " case when (sum_real + real_charge + reval + (case when sum_insaldo<0 then sum_insaldo else 0 end)) >0" +
                                          " then sum_real + real_charge + reval + (case when sum_insaldo<0 then sum_insaldo else 0 end)" +
                                          " else 0 end ";
                    break;
                    case 5: sqql = sqql + " sum_real + real_charge + reval ";
                    break;
                    case 6: sqql = sqql + " case when (sum_real + real_charge + reval) >0" +
                                          " then sum_real + real_charge + reval" +
                                          " else 0 end ";
                        break;
                    default: sqql = sqql + " sum_real + real_charge + reval ";
                    break;
                }
                       
                sqql = sqql +
                       " Where " + chargeXX.where_kvar + //nzp_kvar in ( Select nzp_kvar From t_selkvar ) "
                        chargeXX.paramcalc.per_dat_charge;

                if (chargeXX.paramcalc.nzp_kvar > 0 || chargeXX.paramcalc.nzp_dom > 0)
                    ret = ExecSQL(conn_db, sqql, true);
                else
                    ExecByStep(conn_db, chargeXX.charge_xx, "nzp_charge",
                        sqql, 50000, " ", out ret);

                if (!ret.result)
                {
                    DropTempTablesCharge(conn_db);
                    return false;
                }
                */
                #endregion начислено к оплате

                #region расчет услуг в процентах от суммы начислений
                //
                CalcSumForAllServices(conn_db, chargeXX.paramcalc.cur_mm, chargeXX.paramcalc.cur_yy, chargeXX.paramcalc.pref, sChargeName, sWhere, bDoExecStep, out ret);
                if (!ret.result) { DropTempTablesCharge(conn_db); return false; }
                //
                #endregion расчет услуг в процентах от суммы начислений

                #region расчет рассрочки / для определения права на рассрочку
                //---------------------------------------------------
                //рассрочка, аааааааааааааа!
                //---------------------------------------------------

                if (paramcalc.id_bill_pref.IsEmpty())
                {
                    ret = CalcKreditData(conn_db, paramcalc);
                    if (!ret.result) { DropTempTablesCharge(conn_db); return false; }
                }

                #endregion расчет рассрочки / для определения права на рассрочку

                #region расчет итого - nzp_serv = 1
                //---------------------------------------------------
                //расчет итоговой услуги nzp_serv = 1
                //---------------------------------------------------
                ExecSQL(conn_db, " Drop table t_itog ", false);


                ret = ExecSQL(conn_db,
                    " Create temp table t_itog " +
                    " ( nzp_key    serial not null, " +
                    " nzp_kvar   integer, " +
                    " num_ls     integer, " +
                    " sum_nedop  " + sDecimalType + "(14,2) default 0.00, " +
                    " sum_tarif  " + sDecimalType + "(14,2) default 0.00, " +
                    " gsum_tarif " + sDecimalType + "(14,2) default 0.00, " +
                    " rsum_tarif " + sDecimalType + "(14,2) default 0.00, " +
                    " izm_saldo  " + sDecimalType + "(14,2) default 0.00, " +
                    " real_pere  " + sDecimalType + "(14,2) default 0.00, " +
                    " sum_pere   " + sDecimalType + "(14,2) default 0.00, " +
                    " sum_money  " + sDecimalType + "(14,2) default 0.00, " +
                    " money_to   " + sDecimalType + "(14,2) default 0.00, " +
                    " money_from " + sDecimalType + "(14,2) default 0.00, " +
                    " money_del  " + sDecimalType + "(14,2) default 0.00, " +
                    " sum_outsaldo " + sDecimalType + "(14,2) default 0.00, " +
                    " sum_insaldo  " + sDecimalType + "(14,2) default 0.00, " +
                    " real_charge  " + sDecimalType + "(14,2) default 0.00, " +
                    " sum_real     " + sDecimalType + "(14,2) default 0.00, " +
                    " reval        " + sDecimalType + "(14,2) default 0.00, " +
                    " sum_charge   " + sDecimalType + "(14,2) default 0.00," +
                    " rsum_lgota   " + sDecimalType + "(14,2) default 0.00," +
                    " sum_dlt_tarif   " + sDecimalType + "(14,2) default 0.00," +
                    " sum_dlt_tarif_p " + sDecimalType + "(14,2) default 0.00," +
                    " sum_tarif_p " + sDecimalType + "(14,2) default 0.00," +
                    " sum_lgota   " + sDecimalType + "(14,2) default 0.00," +
                    " sum_dlt_lgota   " + sDecimalType + "(14,2) default 0.00," +
                    " sum_dlt_lgota_p " + sDecimalType + "(14,2) default 0.00," +
                    " sum_lgota_p " + sDecimalType + "(14,2) default 0.00," +
                    " sum_nedop_p " + sDecimalType + "(14,2) default 0.00," +
                    " sum_fakt    " + sDecimalType + "(14,2) default 0.00," +
                    " fakt_to     " + sDecimalType + "(14,2) default 0.00," +
                    " fakt_from   " + sDecimalType + "(14,2) default 0.00," +
                    " fakt_del    " + sDecimalType + "(14,2) default 0.00," +
                    " sum_subsidy " + sDecimalType + "(14,2) default 0.00," +
                    " sum_subsidy_p     " + sDecimalType + "(14,2) default 0.00," +
                    " sum_subsidy_reval " + sDecimalType + "(14,2) default 0.00," +
                    " sum_subsidy_all   " + sDecimalType + "(14,2) default 0.00," +
                    " c_calc " + sDecimalType + "(14,2) default 0.00," +
                    " c_sn   " + sDecimalType + "(14,2) default 0.00," +
                    " sum_tarif_f   " + sDecimalType + "(14,2) default 0.00," +
                    " sum_tarif_f_p " + sDecimalType + "(14,2) default 0.00," +
                    " sum_tarif_sn_eot   " + sDecimalType + "(14,2) default 0.00," +
                    " sum_tarif_sn_eot_p " + sDecimalType + "(14,2) default 0.00," +
                    " sum_tarif_sn_f     " + sDecimalType + "(14,2) default 0.00," +
                    " sum_tarif_sn_f_p   " + sDecimalType + "(14,2) default 0.00" +
                    " )  " + sUnlogTempTable
                    , true);
                if (!ret.result) { DropTempTablesCharge(conn_db); return false; }

                //ret = ExecSQL(conn_db,
                sqql =
                    " Insert into t_itog " +
                      "(nzp_kvar,num_ls,sum_nedop,sum_tarif,gsum_tarif,rsum_tarif,izm_saldo,real_pere,sum_pere,sum_money,money_to," +
                       " money_from,money_del,sum_outsaldo,sum_insaldo,real_charge,sum_real,reval,sum_charge,rsum_lgota,sum_dlt_tarif,sum_dlt_tarif_p," +
                       " sum_tarif_p,sum_lgota,sum_dlt_lgota,sum_dlt_lgota_p,sum_lgota_p,sum_nedop_p,sum_fakt,fakt_to,fakt_from,fakt_del,sum_subsidy," +
                       " sum_subsidy_p,sum_subsidy_reval,sum_subsidy_all,c_calc,c_sn,sum_tarif_f,sum_tarif_f_p,sum_tarif_sn_eot,sum_tarif_sn_eot_p," +
                       " sum_tarif_sn_f,sum_tarif_sn_f_p)" +
                    " Select k.nzp_kvar, k.num_ls, " +
                       " sum(sum_nedop) as sum_nedop, " +
                       " sum(sum_tarif) as sum_tarif, " +
                       " sum(gsum_tarif) as gsum_tarif, " +
                       " sum(rsum_tarif) as rsum_tarif, " +
                       " sum(izm_saldo) as izm_saldo, " +
                       " sum(real_pere) as real_pere, " +
                       " sum(sum_pere) as sum_pere, " +
                       " sum(sum_money) as sum_money, " +
                       " sum(money_to) as money_to, " +
                       " sum(money_from) as money_from, " +
                       " sum(money_del) as money_del, " +
                       " sum(sum_outsaldo) as sum_outsaldo, " +
                       " sum(sum_insaldo) as sum_insaldo, " +
                       " sum(real_charge) as real_charge, " +
                       " sum(sum_real) as sum_real, " +
                       " sum(reval) as reval, " +
                       " sum(sum_charge) as sum_charge," +

                       " sum(rsum_lgota) rsum_lgota, sum(sum_dlt_tarif) sum_dlt_tarif, sum(sum_dlt_tarif_p) sum_dlt_tarif_p, sum(sum_tarif_p) sum_tarif_p," +
                       " sum(sum_lgota) sum_lgota, sum(sum_dlt_lgota) sum_dlt_lgota, sum(sum_dlt_lgota_p) sum_dlt_lgota_p, sum(sum_lgota_p) sum_lgota_p," +
                       " sum(sum_nedop_p) sum_nedop_p, sum(sum_fakt) sum_fakt, sum(fakt_to) fakt_to, sum(fakt_from) fakt_from, sum(fakt_del) fakt_del," +
                       " sum(sum_subsidy) sum_subsidy, sum(sum_subsidy_p) sum_subsidy_p, sum(sum_subsidy_reval) sum_subsidy_reval, sum(sum_subsidy_all) sum_subsidy_all," +
                       " sum(c_calc) c_calc, sum(c_sn) c_sn, sum(sum_tarif_f) sum_tarif_f, sum(sum_tarif_f_p) sum_tarif_f_p, sum(sum_tarif_sn_eot) sum_tarif_sn_eot," +
                       " sum(sum_tarif_sn_eot_p) sum_tarif_sn_eot_p, sum(sum_tarif_sn_f) sum_tarif_sn_f, sum(sum_tarif_sn_f_p) sum_tarif_sn_f_p" +
                    " From " + chargeXX.charge_xx + " ch, t_selkvar k " +
                    " Where ch.nzp_kvar = k.nzp_kvar "
                     + chargeXX.paramcalc.per_dat_charge +
                    "   and nzp_serv <> 1 ";
                //    , true, 600);
                if (chargeXX.paramcalc.nzp_kvar > 0 || chargeXX.paramcalc.nzp_dom > 0)
                {
                    sqql = sqql + " Group by 1,2 ";
                    ret = ExecSQL(conn_db, sqql, true);
                }
                else
                {
                    //ExecByStep(conn_db, chargeXX.charge_xx + " k ", "k.nzp_kvar",
                    ExecByStep(conn_db, "t_selkvar k", "k.nzp_kvar",
                        sqql, 50000, " Group by 1,2 ", out ret);
                }
                if (!ret.result) { DropTempTablesCharge(conn_db); return false; }

                ExecSQL(conn_db, " Create unique index ix1_t_itog on t_itog (nzp_key) ", true);
                ExecSQL(conn_db, " Create unique index ix2_t_itog on t_itog (nzp_kvar) ", true);
                ExecSQL(conn_db, sUpdStat + " t_itog ", true);


                sqql =
                " Insert into " + chargeXX.charge_xx +
                   " ( dat_charge, nzp_kvar, num_ls, nzp_serv, nzp_supp, nzp_frm, tarif, tarif_p," +
                     " rsum_lgota,  sum_dlt_tarif, sum_dlt_tarif_p, " +
                     " sum_tarif_p,sum_lgota, sum_dlt_lgota, sum_dlt_lgota_p, sum_lgota_p, sum_nedop_p,  " +
                     " sum_fakt, fakt_to, fakt_from, fakt_del,  sum_subsidy, sum_subsidy_p, sum_subsidy_reval, sum_subsidy_all, " +
                     " isblocked, is_device, c_calc, c_sn, c_okaz, c_nedop, isdel, c_reval, order_print,  " +

                     " sum_nedop, gsum_tarif, rsum_tarif, sum_tarif, izm_saldo, real_pere, sum_pere, " +
                     " sum_insaldo, sum_outsaldo, sum_real,  sum_charge, reval, real_charge, sum_money, money_to, money_from, money_del, " +
                     " sum_tarif_f, sum_tarif_f_p, sum_tarif_sn_eot, sum_tarif_sn_eot_p, sum_tarif_sn_f, sum_tarif_sn_f_p " +
                     ") " +
                " Select " + p_dat_charge + ", nzp_kvar,num_ls, 1 nzp_serv, 0 nzp_supp, 0, 0, 0,  " +
                     " rsum_lgota, sum_dlt_tarif, sum_dlt_tarif_p," +
                     " sum_tarif_p, sum_lgota, sum_dlt_lgota, sum_dlt_lgota_p, sum_lgota_p, sum_nedop_p," +
                     " sum_fakt, fakt_to, fakt_from, fakt_del, sum_subsidy, sum_subsidy_p, sum_subsidy_reval, sum_subsidy_all," +
                     " 0, 0, c_calc, c_sn, 0, 0, 0, 0, 0, " +

                     " sum_nedop, gsum_tarif, rsum_tarif, sum_tarif, izm_saldo, real_pere, sum_pere, " +
                     " sum_insaldo, sum_outsaldo, sum_real,  sum_charge, reval, real_charge, sum_money, money_to, money_from, money_del, " +
                     " sum_tarif_f, sum_tarif_f_p, sum_tarif_sn_eot, sum_tarif_sn_eot_p, sum_tarif_sn_f, sum_tarif_sn_f_p " +
                " From t_itog Where 1 = 1 ";

                if (chargeXX.paramcalc.nzp_kvar > 0 || chargeXX.paramcalc.nzp_dom > 0)
                    ret = ExecSQL(conn_db, sqql, true);
                else
                    ExecByStep(conn_db, "t_itog", "nzp_key",
                        sqql, 50000, " ", out ret);

                if (!ret.result) { DropTempTablesCharge(conn_db); return false; }

                #endregion расчет итого - nzp_serv = 1

                #region отметка о дате расчета
                //---------------------------------------------------
                //отметка о дате расчета
                //---------------------------------------------------
                sqql =
                    " Update " + chargeXX.kvar_calc_xx +
                    " Set dat_calc = " + sCurDateTime +
                    " Where " + chargeXX.where_kvar;
                ret = ExecSQL(conn_db, sqql, true);
                if (!ret.result) { DropTempTablesCharge(conn_db); return false; }

                sqql =
                    " Insert into " + chargeXX.kvar_calc_xx + " (nzp_kvar, num_ls, dat_calc) " +
                    " Select nzp_kvar,num_ls," + sCurDateTime +
                    " From t_selkvar k " +
                    " Where NOT EXISTS ( Select 1 From " + chargeXX.kvar_calc_xx + " f Where k.nzp_kvar=f.nzp_kvar )  ";
                ret = ExecSQL(conn_db, sqql, true);
                if (!ret.result) { DropTempTablesCharge(conn_db); return false; }

                #endregion отметка о дате расчета

            }

            #region обновление статистики

            if (UpdateStat)
            {
                UpdateStatistics(false, paramcalc, chargeXX.charge_tab, out ret);
                if (!ret.result) { DropTempTablesCharge(conn_db); return false; }
            }
            DropTempTablesCharge(conn_db);
            return true;

            #endregion обновление статистики
        }

        #region установка начислено к оплате

        public void CalcSumToPay(IDbConnection conn_db, int mm, int yy, string pLocalPref, string pChargeName, string pWhere, string pDatCharge, bool pDoExecStep, out Returns ret)
        {
            ret = Utils.InitReturns();

            string sdataMain = Points.Pref + "_data" + tableDelimiter;
            string skernelMain = Points.Pref + "_kernel" + tableDelimiter;
            string sdataLocal = pLocalPref + "_data" + tableDelimiter;
            string sChargeLocal = pLocalPref + "_charge_" + (yy - 2000).ToString() + tableDelimiter;
            string sDebtMain = Points.Pref + DBManager.sDebtAliasRest;

            ret = ExecSQL(conn_db, " select * from temp_table_tarif ", false);
            if (!ret.result)
            {
                string sql = " Select * " +
#if PG
 " Into temp temp_table_tarif " +
#endif
 " From " + sdataLocal + "tarif " +
                    " Where " + pWhere +
                    " and is_actual<>100 and dat_s<=" + MDY(mm, 28, yy) + " and dat_po>=" + MDY(mm, 1, yy);
#if !PG
                sql +=  " Into temp temp_table_tarif with no log ";
#endif

                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result) { DropTempTablesCharge(conn_db); return; }

                ret = ExecSQL(conn_db, " Create unique index ix1_temp_table_tariff on temp_table_tarif (nzp_tarif) ", true);
                ret = ExecSQL(conn_db, " Create index ix3_temp_table_tariff on temp_table_tarif (nzp_kvar, nzp_serv, nzp_supp, nzp_frm ) ", true);
                ret = ExecSQL(conn_db, " Create index ix4_temp_table_tarif on temp_table_tarif (nzp_kvar, dat_s, dat_po) ", true);
                ret = ExecSQL(conn_db, " Create index ix5_temp_table_tarif on temp_table_tarif (dat_s, dat_po) ", true);
                ExecSQL(conn_db, sUpdStat + " temp_table_tarif ", true);

                if (!ret.result) { DropTempTablesCharge(conn_db); return; }
            }

            ret = ExecSQL(conn_db, " drop table t_repay ", false);
            ret = ExecSQL(conn_db,
                //" create table are.t_repay (" +
                " create temp table t_repay (" +
                " nzp_kvar integer," +
                " nzp_serv integer," +
                " nzp_supp integer," +
                " nzp_serv_main integer," +
                " sum_repay " + sDecimalType + "(14,2) default 0, " +
                " sum_repay_dolg " + sDecimalType + "(14,2) default 0, " +
                " sum_repay_mn " + sDecimalType + "(14,2) default 0, " +
                " sum_repay_perc " + sDecimalType + "(14,2) default 0, " +
                " sum_indolg_main " + sDecimalType + "(14,2) default 0 " +
                " ) " + sUnlogTempTable
                , true);
            if (!ret.result) { DropTempTablesCharge(conn_db); return; }

            // выбрать ЭОТ тарифы по ЛС/услугам/поставщикам
            ret = ExecSQL(conn_db,
                " insert into t_repay (nzp_kvar,nzp_serv,nzp_supp,nzp_serv_main)" +
                " select t.nzp_kvar,s.nzp_serv_repay,t.nzp_supp,s.nzp_serv_link " +
                " from temp_table_tarif t," + skernelMain + "serv_odn s " +
                " where t.nzp_serv=s.nzp_serv_repay "
                , true);
            if (!ret.result) { DropTempTablesCharge(conn_db); return; }

            ret = ExecSQL(conn_db, " create index ixt_repay1 on t_repay(nzp_kvar,nzp_serv,nzp_supp) ", true);
            if (!ret.result) { DropTempTablesCharge(conn_db); return; }
            ret = ExecSQL(conn_db, " create index ixt_repay2 on t_repay(nzp_kvar,nzp_serv_main,nzp_supp) ", true);
            if (!ret.result) { DropTempTablesCharge(conn_db); return; }

            ret = ExecSQL(conn_db, sUpdStat + " t_repay ", true);
            if (!ret.result) { DropTempTablesCharge(conn_db); return; }

            ret = ExecSQL(conn_db,
                " update t_repay set " +
                " sum_repay = " +
                    " (select sum (k.sum_dolg)   from " + sDebtMain + "kredit k," + sDebtMain + "kredit_pay s " +
                    " Where t_repay.nzp_kvar=k.nzp_kvar and t_repay.nzp_serv=k.nzp_serv and k.nzp_kredit=s.nzp_kredit" +
                    " and s.calc_month = " + DBManager.MDY(mm, 1, yy) +
                    " and k.valid=1 and k.dat_s<" + MDY(mm, 28, yy) + " and k.dat_po>=" + MDY(mm, 1, yy) + ") " +
                ",sum_repay_dolg = " +
                    " (select sum (s.sum_indolg) from " + sDebtMain + "kredit k," + sDebtMain + "kredit_pay s " +
                    " Where t_repay.nzp_kvar=k.nzp_kvar and t_repay.nzp_serv=k.nzp_serv and k.nzp_kredit=s.nzp_kredit" +
                    " and s.calc_month = " + DBManager.MDY(mm, 1, yy) +
                    " and k.valid=1 and k.dat_s<" + MDY(mm, 28, yy) + " and k.dat_po>=" + MDY(mm, 1, yy) + ") " +
                ",sum_repay_mn = " +
                    " (select sum (s.sum_odna12) from " + sDebtMain + "kredit k," + sDebtMain + "kredit_pay s " +
                    " Where t_repay.nzp_kvar=k.nzp_kvar and t_repay.nzp_serv=k.nzp_serv and k.nzp_kredit=s.nzp_kredit" +
                    " and s.calc_month = " + DBManager.MDY(mm, 1, yy) +
                    " and k.valid=1 and k.dat_s<" + MDY(mm, 28, yy) + " and k.dat_po>=" + MDY(mm, 1, yy) + ") " +
                ",sum_repay_perc = " +
                    " (select sum (s.sum_perc)   from " + sDebtMain + "kredit k," + sDebtMain + "kredit_pay s " +
                    " Where t_repay.nzp_kvar=k.nzp_kvar and t_repay.nzp_serv=k.nzp_serv and k.nzp_kredit=s.nzp_kredit" +
                    " and s.calc_month = " + DBManager.MDY(mm, 1, yy) +
                    " and k.valid=1 and k.dat_s<" + MDY(mm, 28, yy) + " and k.dat_po>=" + MDY(mm, 1, yy) + ") " +
                ",sum_indolg_main = " +
                    " (select sum (s.sum_indolg_main)   from " + sDebtMain + "kredit k," + sDebtMain + "kredit_pay s " +
                    " Where t_repay.nzp_kvar=k.nzp_kvar and t_repay.nzp_serv=k.nzp_serv and k.nzp_kredit=s.nzp_kredit" +
                    " and s.calc_month = " + DBManager.MDY(mm, 1, yy) +
                    " and k.valid=1 and k.dat_s<" + MDY(mm, 28, yy) + " and k.dat_po>=" + MDY(mm, 1, yy) + ") " +
                " Where" +
                    " 0<(select count(*) from " + sDebtMain + "kredit k," + sDebtMain + "kredit_pay s " +
                    " Where t_repay.nzp_kvar=k.nzp_kvar and t_repay.nzp_serv=k.nzp_serv and k.nzp_kredit=s.nzp_kredit" +
                    " and s.calc_month = " + DBManager.MDY(mm, 1, yy) +
                    " and k.valid=1 and k.dat_s<" + MDY(mm, 28, yy) + " and k.dat_po>=" + MDY(mm, 1, yy) + ") "
                , true);
            if (!ret.result) { DropTempTablesCharge(conn_db); return; }

            var kod = GetFldSumToPay(conn_db, mm, yy, "", out ret);
            var exp = ret.text;
            string sqqlRepay;

            if (kod == 1 || kod == 2)
            {
                sqqlRepay = " Update " + pChargeName + " Set sum_dlt_lgota_p = " +
                   " (select sum(t.sum_repay_mn) - sum(t.sum_indolg_main) from t_repay t where t.nzp_kvar=" + pChargeName + ".nzp_kvar" +
                   " and t.nzp_serv_main=" + pChargeName + ".nzp_serv and t.nzp_supp=" + pChargeName + ".nzp_supp) " +
                   " Where " + pWhere + pDatCharge +
                   " and 0<(select count(*) from t_repay t where t.nzp_kvar=" + pChargeName + ".nzp_kvar" +
                         " and t.nzp_serv_main=" + pChargeName + ".nzp_serv and t.nzp_supp=" + pChargeName + ".nzp_supp) ";
            }
            else 
            // sum_dlt_lgota_p - рассрочка за месяц. добавляется к sum_real месяца расчета.
                sqqlRepay = " Update " + pChargeName + " Set sum_dlt_lgota_p = " +
                   " (select sum(t.sum_repay_mn) from t_repay t where t.nzp_kvar=" + pChargeName + ".nzp_kvar" +
                   " and t.nzp_serv_main=" + pChargeName + ".nzp_serv and t.nzp_supp=" + pChargeName + ".nzp_supp) " +
                   " Where " + pWhere + pDatCharge +
                   " and 0<(select count(*) from t_repay t where t.nzp_kvar=" + pChargeName + ".nzp_kvar" +
                         " and t.nzp_serv_main=" + pChargeName + ".nzp_serv and t.nzp_supp=" + pChargeName + ".nzp_supp) ";

            ret = ExecSQL(conn_db, sqqlRepay, true);
            if (!ret.result) { DropTempTablesCharge(conn_db); return; }

            string sqql = " Update " + pChargeName + " Set sum_charge = " + exp.Trim() + " Where " + pWhere + pDatCharge;

            if (!pDoExecStep)
                ret = ExecSQL(conn_db, sqql, true);
            else
                ExecByStep(conn_db, pChargeName, "nzp_charge",
                    sqql, 50000, " ", out ret);

            if (!ret.result) { DropTempTablesCharge(conn_db); return; }

            ret = ExecSQL(conn_db, " drop table t_repay ", true);
            if (!ret.result) { DropTempTablesCharge(conn_db); return; }
        }

        public int GetFldSumToPay(IDbConnection conn_db, int mm, int yy, string pAls, out Returns ret)
        {
            ret = Utils.InitReturns();

            string sdataMain = Points.Pref + "_data" + tableDelimiter;

            // ... взять вид начислено к оплате ... 
            IDbCommand cmd_no = DBManager.newDbCommand(
                " select max(p.val_prm) " +
                " from " + sdataMain + "prm_10 p " +
                " where p.nzp_prm=1134 " +
                " and p.is_actual<>100 and p.dat_s<=" + MDY(mm, 28, yy) + " and p.dat_po>=" + MDY(mm, 1, yy) + " "
            , conn_db);

            int iKodNachOpl = 0;
            try
            {
                string snovals = Convert.ToString(cmd_no.ExecuteScalar());
                iKodNachOpl = Convert.ToInt32(snovals);
            }
            catch
            {
                iKodNachOpl = 0;
            }

            switch (iKodNachOpl)
            {
                case 1: ret.text = pAls.Trim() + "sum_outsaldo + " + pAls.Trim() + "sum_dlt_lgota_p";
                    break;
                case 2: ret.text = "case when " + pAls.Trim() + "sum_outsaldo>0 then " + pAls.Trim() + "sum_outsaldo + " + pAls.Trim() + "sum_dlt_lgota_p else 0 end";
                    break;
                case 3: ret.text = pAls.Trim() + "sum_real + " + pAls.Trim() + "sum_dlt_lgota_p + " + pAls.Trim() + "real_charge + " + pAls.Trim() + "reval + (case when " + pAls.Trim() + "sum_insaldo<0 then " + pAls.Trim() + "sum_insaldo else 0 end)";
                    break;
                case 4: ret.text = "case when (" + pAls.Trim() + "sum_real + " + pAls.Trim() + "sum_dlt_lgota_p + " + pAls.Trim() + "real_charge + " + pAls.Trim() + "reval + (case when " + pAls.Trim() + "sum_insaldo<0 then " + pAls.Trim() + "sum_insaldo else 0 end)) >0" +
                                      " then " + pAls.Trim() + "sum_real + " + pAls.Trim() + "sum_dlt_lgota_p + " + pAls.Trim() + "real_charge + " + pAls.Trim() + "reval + (case when " + pAls.Trim() + "sum_insaldo<0 then " + pAls.Trim() + "sum_insaldo else 0 end)" +
                                      " else 0 end";
                    break;
                case 5: ret.text = pAls.Trim() + "sum_real + " + pAls.Trim() + "sum_dlt_lgota_p + " + pAls.Trim() + "real_charge + " + pAls.Trim() + "reval ";
                    break;
                case 6: ret.text = "case when (" + pAls.Trim() + "sum_real + " + pAls.Trim() + "sum_dlt_lgota_p + " + pAls.Trim() + "real_charge + " + pAls.Trim() + "reval) >0" +
                                      " then " + pAls.Trim() + "sum_real + " + pAls.Trim() + "sum_dlt_lgota_p + " + pAls.Trim() + "real_charge + " + pAls.Trim() + "reval" +
                                      " else 0 end";
                    break;
                default: ret.text = pAls.Trim() + "sum_real + " + pAls.Trim() + "sum_dlt_lgota_p + " + pAls.Trim() + "real_charge + " + pAls.Trim() + "reval";
                    break;
            }
            return iKodNachOpl;

        }

        #endregion установка начислено к оплате


        #region установка процента рассрочки в прошлый месяц - первый раз

        public void SetPayToPrev(IDbConnection conn_db, int mm, int yy, string pLocalPref, string pChargeName, string pWhere, bool pDoExecStep, out Returns ret)
        {
            ret = Utils.InitReturns();

            string skernelMain = Points.Pref + "_kernel" + tableDelimiter;
            string sdataLocal = pLocalPref + "_data" + tableDelimiter;
            string skernelLocal = pLocalPref + "_kernel" + tableDelimiter;
            string sChargeLocal = pLocalPref + "_charge_" + (yy - 2000).ToString() + tableDelimiter;
            string sDebtMain = Points.Pref + DBManager.sDebtAliasRest;

            ret = ExecSQL(conn_db, " drop table temp_table_tarif ", true);
            string ssql =
                " Select k.nzp_dom,t.* " +

#if PG
 " Into temp temp_table_tarif " +
#endif
 " From " + sdataLocal + "tarif t," + sdataLocal + "kvar k," + skernelMain + "serv_odn s " +
                " Where t.nzp_kvar=k.nzp_kvar and t." + pWhere.Trim() +
                " and t.is_actual<>100 and t.dat_s<=" + MDY(mm, 28, yy) + " and t.dat_po>=" + MDY(mm, 1, yy) +
                " and t.nzp_serv=s.nzp_serv_repay ";

#if !PG
            ssql += " Into temp temp_table_tarif with no log ";
#endif
            ret = ExecSQL(conn_db, ssql, true);
            if (!ret.result) { ret = ExecSQL(conn_db, " drop table temp_table_tarif ", false); return; }

            ret = ExecSQL(conn_db, " Create index ix3_temp_table_tariff on temp_table_tarif (nzp_kvar, nzp_serv, nzp_supp, nzp_frm ) ", true);
            ExecSQL(conn_db, sUpdStat + " temp_table_tarif ", true);

            if (!ret.result) { ret = ExecSQL(conn_db, " drop table temp_table_tarif ", false); return; }


            ret = ExecSQL(conn_db, " drop table t_repay ", false);
            ssql = //" create table are.t_repay (" +
                " create temp table t_repay (" +
                " nzp_dom  integer," +
                " nzp_kvar integer," +
                " num_ls   integer," +
                " nzp_serv integer," +
                " nzp_supp integer," +
                " nzp_frm  integer," +
                " sum_perc " + sDecimalType + "(14,2) default 0) " + DBManager.sUnlogTempTable;
            ret = ExecSQL(conn_db, ssql, true);
            if (!ret.result) { DropTempTablesCharge(conn_db); return; }

            // выбрать рассрочку ЛС/услугам/поставщикам
            ssql = " insert into t_repay (nzp_dom,nzp_kvar,num_ls,nzp_serv,nzp_supp,nzp_frm,sum_perc)" +
                " Select " +
                " t.nzp_dom,t.nzp_kvar,t.num_ls,t.nzp_serv,t.nzp_supp,t.nzp_frm,sum(s.sum_perc)" +
                " from temp_table_tarif t," + skernelLocal + "formuls_opis f," + sDebtMain + "kredit k," + sDebtMain + "kredit_pay s " +
                " where t.nzp_frm=f.nzp_frm and f.nzp_frm_typ=1999" +
                " and s.calc_month = " + DBManager.MDY(mm, 1, yy) +
                " and t.nzp_kvar=k.nzp_kvar and t.nzp_serv=k.nzp_serv and k.nzp_kredit=s.nzp_kredit" +
                " and k.valid=1 and k.dat_s<" + MDY(mm, 28, yy) + " and k.dat_po>=" + MDY(mm, 1, yy) +
                " group by 1,2,3,4,5,6 ";

            ret = ExecSQL(conn_db, ssql, true);
            if (!ret.result) { DropTempTablesCharge(conn_db); return; }

            ret = ExecSQL(conn_db, " create index ixt_repay1 on t_repay(nzp_kvar,nzp_serv,nzp_supp) ", true);
            if (!ret.result) { DropTempTablesCharge(conn_db); return; }

            ret = ExecSQL(conn_db, sUpdStat + " t_repay ", true);
            if (!ret.result) { DropTempTablesCharge(conn_db); return; }

            // вставка расчета в БД
            string sKGnm = sChargeLocal + "calc_gku_" + mm.ToString("00");
            ssql =
                " delete from " + sKGnm +
                " Where 0<( Select count(*) " +
                " from t_repay t Where t.nzp_kvar=" + sKGnm + ".nzp_kvar and t.nzp_serv=" + sKGnm + ".nzp_serv and t.nzp_supp=" + sKGnm + ".nzp_supp) ";
            ret = ExecSQL(conn_db, ssql, true);
            if (!ret.result) { ret = ExecSQL(conn_db, " drop table temp_table_tarif ", false); return; }

            ssql =
                " Insert into " + sChargeLocal + "calc_gku_" + mm.ToString("00") +
                " ( nzp_dom,nzp_kvar,nzp_serv,nzp_supp,nzp_frm," +
                "nzp_frm_typ,gil,gil_g,squ,nzp_prm_tarif,tarif,trf1,trf2,trf3,trf4," +
                " nzp_frm_typrs,nzp_prm_rashod,rashod,rashod_g,rsh1,rsh2,rsh3,nzp_prm_trf_dt,tarif_f,rashod_norm,valm,dop87,is_device,rash_norm_one,dlt_reval ) " +
                " Select " +
                " t.nzp_dom,t.nzp_kvar,t.nzp_serv,t.nzp_supp,t.nzp_frm," +
                "1999,0,0,0,0,t.sum_perc,0,0,0,0," +
                "4,0,1,1,0,0,0,0,0,0,1,0,0,1,0 " +
                " from t_repay t ";

            ret = ExecSQL(conn_db, ssql, true);
            if (!ret.result) { ret = ExecSQL(conn_db, " drop table temp_table_tarif ", false); return; }

            // вставка нчислений в БД
            ssql =
                " delete from " + pChargeName +
                " Where 0<( Select count(*) " +
                " from t_repay t Where t.nzp_kvar=" + pChargeName + ".nzp_kvar and t.nzp_serv=" + pChargeName + ".nzp_serv and t.nzp_supp=" + pChargeName + ".nzp_supp) ";
            ret = ExecSQL(conn_db, ssql, true);
            if (!ret.result) { ret = ExecSQL(conn_db, " drop table temp_table_tarif ", false); return; }
            ssql =
            " Insert into " + pChargeName +
               " ( nzp_kvar, num_ls, nzp_serv, nzp_supp, nzp_frm, tarif, tarif_p," +
                 " rsum_lgota,  sum_dlt_tarif, sum_dlt_tarif_p, " +
                 " sum_tarif_p,sum_lgota, sum_dlt_lgota, sum_dlt_lgota_p, sum_lgota_p, sum_nedop_p,  " +
                 " sum_fakt, fakt_to, fakt_from, fakt_del,  sum_subsidy, sum_subsidy_p, sum_subsidy_reval, sum_subsidy_all, " +
                 " isblocked, is_device, c_calc, c_sn, c_okaz, c_nedop, isdel, c_reval, order_print,  " +

                 " sum_nedop, gsum_tarif, rsum_tarif, sum_tarif, izm_saldo, real_pere, sum_pere, " +
                 " sum_insaldo, sum_outsaldo, sum_real,  sum_charge, reval, real_charge, sum_money, money_to, money_from, money_del, " +
                 " sum_tarif_f, sum_tarif_f_p, sum_tarif_sn_eot, sum_tarif_sn_eot_p, sum_tarif_sn_f, sum_tarif_sn_f_p ) " +
            " Select t.nzp_kvar,t.num_ls,t.nzp_serv,t.nzp_supp, t.nzp_frm,t.sum_perc tarif,0 tarif_p,  " +
                 " 0 rsum_lgota,0 sum_dlt_tarif,0 sum_dlt_tarif_p," +
                 " 0 sum_tarif_p,0 sum_lgota,0 sum_dlt_lgota,0 sum_dlt_lgota_p,0 sum_lgota_p,0 sum_nedop_p," +
                 " 0 sum_fakt,0 fakt_to,0 fakt_from,0 fakt_del,0 sum_subsidy,0 sum_subsidy_p,0 sum_subsidy_reval,0 sum_subsidy_all," +
                 " 0, 0,1 c_calc,1 c_sn, 0, 0, 0, 0, 0, " +

                 " 0 sum_nedop,t.sum_perc gsum_tarif,t.sum_perc rsum_tarif,t.sum_perc sum_tarif, 0 izm_saldo, 0 real_pere, 0 sum_pere, " +
                 " 0 sum_insaldo,t.sum_perc sum_outsaldo,t.sum_perc sum_real,t.sum_perc sum_charge,0 reval,0 real_charge,0 sum_money,0 money_to,0 money_from,0 money_del, " +
                 " 0 sum_tarif_f,0 sum_tarif_f_p,0 sum_tarif_sn_eot,0 sum_tarif_sn_eot_p,0 sum_tarif_sn_f,0 sum_tarif_sn_f_p " +

                " from t_repay t ";

            ret = ExecSQL(conn_db, ssql, true);
            if (!ret.result) { ret = ExecSQL(conn_db, " drop table temp_table_tarif ", false); return; }

            ret = ExecSQL(conn_db, " drop table t_repay ", false);

            // установить начислено к оплате по основной услуге
            CalcSumToPay(conn_db, mm, yy, pLocalPref, pChargeName, pWhere, " and dat_charge is null ", pDoExecStep, out ret);

            ret = ExecSQL(conn_db, " drop table temp_table_tarif ", true);
        }
        #endregion установка процента рассрочки в прошлый месяц - первый раз

        #region обновление статистики вызов
        private void UpdateStatistics(bool p1, CalcTypes.ParamCalc paramcalc, string p2, out Returns ret)
        {
            WorkTempKvar wk = new WorkTempKvar();
            wk.UpdateStatistics(p1, paramcalc, p2, out ret);
            wk.Close();
        }
        #endregion обновление статистики вызов

    }
}
