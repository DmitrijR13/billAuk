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
        //---------------------------------------------------
        //расчет начислений charge_xx
        //---------------------------------------------------
        protected struct ChargeXX
        {
            public ParamCalc paramcalc;

            public string charge_xx;
            public string charge_xx_ishod; //с чем сравнивать при перерасчете

            public string charge_tab;
            public string prev_charge_xx;
            public string prev_charge_tab;
            public string calc_gku_xx;

            public string kvar_calc_tab;
            public string kvar_calc_xx;

            public string lnk_charge_xx;
            public string lnk_tab;

            public string reval_tab;
            public string reval_xx;
            public string delta_tab;
            public string delta_xx;

            public string calc_nedo_xx;
            public string fn_supplier;
            public string del_supplier;
            public string from_supplier;
            public string perekidka;
            public string report_xx;
            public string report_xx_dom;

            public string where_report, charge_cnts, charge_nedo, charge_g, charge_cnts_prev, charge_nedo_prev, counters_vals;
            public string counters_xx;

            public string where_kvar;

            public ChargeXX(ParamCalc _paramcalc)
            {
                paramcalc = _paramcalc;
                paramcalc.b_dom_in = true;
                string cur_bd = paramcalc.pref + "_charge_" + (paramcalc.cur_yy - 2000).ToString("00");
                string calc_bd  = paramcalc.pref + "_charge_" + (paramcalc.calc_yy - 2000).ToString("00");

                lnk_tab         = "lnk_charge" + paramcalc.alias_again + "_" + paramcalc.cur_mm.ToString("00");
                lnk_charge_xx = cur_bd + tableDelimiter + lnk_tab;

                reval_tab       = "reval" + paramcalc.alias_again + "_" + paramcalc.cur_mm.ToString("00");
                reval_xx = cur_bd + tableDelimiter + reval_tab;

                delta_tab       = "delta" + paramcalc.alias_again + "_" + paramcalc.cur_mm.ToString("00");
                delta_xx = cur_bd + tableDelimiter + delta_tab;

                counters_xx   = calc_bd + tableDelimiter + "counters" + paramcalc.alias + "_" + paramcalc.calc_mm.ToString("00");
                calc_nedo_xx  = calc_bd + tableDelimiter + "nedo" + paramcalc.alias + "_" + paramcalc.calc_mm.ToString("00");
                fn_supplier   = calc_bd + tableDelimiter + "fn_supplier" + paramcalc.calc_mm.ToString("00");
                from_supplier = calc_bd + tableDelimiter + "from_supplier";
                del_supplier  = calc_bd + tableDelimiter + "del_supplier";
                perekidka     = calc_bd + tableDelimiter + "perekidka";

                prev_charge_tab = "charge_" + paramcalc.prev_calc_mm.ToString("00");
                prev_charge_xx = paramcalc.pref + "_charge_" + (paramcalc.prev_calc_yy - 2000).ToString("00") + tableDelimiter + prev_charge_tab;

                charge_tab      = "charge" + paramcalc.alias + "_" + paramcalc.calc_mm.ToString("00");
                charge_xx = calc_bd + tableDelimiter + charge_tab;

                charge_xx_ishod = calc_bd + tableDelimiter + "charge_" + paramcalc.calc_mm.ToString("00"); //с чем сравнивать при  перерасчете
                calc_gku_xx     = calc_bd + tableDelimiter + "calc_gku" + paramcalc.alias + "_" + paramcalc.calc_mm.ToString("00");

                kvar_calc_tab   = "kvar_calc" + paramcalc.alias_again + "_" + paramcalc.calc_mm.ToString("00");
                kvar_calc_xx = calc_bd + tableDelimiter + kvar_calc_tab;

                where_report    = " and month_ = " + paramcalc.calc_mm + " and nzp_wp = " + paramcalc.nzp_wp;

                counters_vals = calc_bd + tableDelimiter + "counters_vals ";
                charge_cnts   = calc_bd + tableDelimiter + "charge_cnts ";
                charge_nedo   = calc_bd + tableDelimiter + "charge_nedo ";
                charge_g      = calc_bd + tableDelimiter + "charge_g ";

                string calc_bd_prev = paramcalc.pref + "_charge_" + (paramcalc.cur_yy - 2000).ToString("00");
                if (paramcalc.cur_mm == 1)
                    calc_bd_prev = paramcalc.pref + "_charge_" + (paramcalc.cur_yy - 2001).ToString("00");

                charge_cnts_prev = calc_bd_prev + tableDelimiter + "charge_cnts ";
                charge_nedo_prev = calc_bd_prev + tableDelimiter + "charge_nedo ";

#if PG
                string ol_srv = "";
#else
                string ol_srv = "";
                if (Points.IsFabric)
                {
                    foreach (_Server server in Points.Servers)
                    {
                        if (Points.Point.nzp_server == server.nzp_server)
                        {
                            ol_srv = "@" + server.ol_server;
                            break;
                        }
                    }
                }
#endif

                report_xx = Points.Pref + "_fin_" + (paramcalc.calc_yy - 2000).ToString("00") + ol_srv + tableDelimiter + "fn_ukrgucharge ";
                report_xx_dom = Points.Pref + "_fin_" + (paramcalc.calc_yy - 2000).ToString("00") + ol_srv + tableDelimiter + "fn_ukrgudom ";

                where_kvar = " nzp_kvar in ( Select nzp_kvar From t_selkvar)";
                if (paramcalc.nzp_kvar > 0)
                    where_kvar = " nzp_kvar = " + paramcalc.nzp_kvar;

            }
        }

        //--------------------------------------------------------------------------------
        void DropTempTablesCharge(IDbConnection conn_db)
        //--------------------------------------------------------------------------------
        {
#if PG
            ExecSQL(conn_db, " Drop view ishod_charge ", false);
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
        }

        /// <summary>
        /// Создает таблицу начислений и другие вспомогательные таблицы
        /// </summary>
        /// <param name="conn_db2"></param>
        /// <param name="chargeXX"></param>
        /// <param name="del_reval_xx"></param>
        /// <param name="ret"></param>
        void CreateChargeXX(IDbConnection conn_db2, ChargeXX chargeXX, bool del_reval_xx, out Returns ret)
        {
            ret = Utils.InitReturns();

            //*****************************************************************
            //lnk_charge_xx
            //*****************************************************************
            if (TempTableInWebCashe(conn_db2, chargeXX.lnk_charge_xx))
            {
                if (del_reval_xx)
                {
                    /*
                        ret = ExecSQL(conn_db,
                                " Delete From " + cur_chargeXX.lnk_charge_xx +
                                " Where nzp_kvar in ( Select nzp_kvar From t_selkvar) "
                            , true);
                        if (!ret.result)
                        {
                            //conn_db.Close();
                            return false;
                        }
                    */
                    ret = ExecSQL(conn_db2,
                            " Delete From " + chargeXX.lnk_charge_xx +
                            " Where " + chargeXX.where_kvar  //nzp_kvar in ( Select nzp_kvar From t_selkvar) "
                        , true);
                    if (!ret.result)
                    {
                        return;
                    }
                    UpdateStatistics(false, chargeXX.paramcalc, chargeXX.lnk_tab, out ret);
                    if (!ret.result)
                    {
                        return;
                    }
                }
            }
            else
            {
                string conn_kernel = Points.GetConnByPref(chargeXX.paramcalc.pref);
                IDbConnection conn_db = GetConnection(conn_kernel);
                
                ret = OpenDb(conn_db, true);
                if (!ret.result)
                {
                    return;
                }
#if PG
                ret = ExecSQL(conn_db, " set search_path to '" + chargeXX.paramcalc.pref + "_charge_" + (chargeXX.paramcalc.cur_yy - 2000).ToString("00")+"'", true);
#else
                ret = ExecSQL(conn_db, " database " + chargeXX.paramcalc.pref + "_charge_" + (chargeXX.paramcalc.cur_yy - 2000).ToString("00"), true);
#endif

                ret = ExecSQL(conn_db,
                    " Create table " + tbluser + chargeXX.lnk_tab +
                    "  (  nzp_kvar integer, " +
                    "     month_ integer, " +
                    "     year_ integer ) "
                    , true);
                if (!ret.result)
                {
                    conn_db.Close();
                    return;
                }
                ret = ExecSQL(conn_db,
                    " Create index " + tbluser + "ilnk1_" +
                    chargeXX.paramcalc.alias + "_" + chargeXX.paramcalc.cur_mm.ToString("00") + 
                    " on " + chargeXX.lnk_tab + " (nzp_kvar,year_,month_) ", true);
                if (!ret.result)
                {
                    conn_db.Close();
                    return;
                }
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
                if (!ret.result)
                {
                    return;
                }
                ret = ExecSQL(conn_db,
#if PG
                    " set search_path to '" + chargeXX.paramcalc.pref + "_charge_" + (chargeXX.paramcalc.calc_yy - 2000).ToString("00")+"'"
#else
                    " database " + chargeXX.paramcalc.pref + "_charge_" + (chargeXX.paramcalc.calc_yy - 2000).ToString("00")
#endif
                    , true);

                ret = ExecSQL(conn_db,
                    " Create table " + tbluser + chargeXX.kvar_calc_tab +
                    "  (  nzp_kvar integer, " +
                    "     num_ls integer, " +
                    "     dat_calc date) "
                    , true);
                if (!ret.result)
                {
                    conn_db.Close();
                    return;
                }
                ret = ExecSQL(conn_db,
                    " Create index " + tbluser + "ikct_" 
                    + chargeXX.paramcalc.alias + "_" + chargeXX.paramcalc.calc_mm.ToString("00") +
                    " on " + chargeXX.kvar_calc_tab + " (nzp_kvar,num_ls,dat_calc) ", true);
                if (!ret.result)
                {
                    conn_db.Close();
                    return;
                }
                ExecSQL(conn_db, sUpdStat + " " + chargeXX.kvar_calc_tab, true);

                conn_db.Close();
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
                                  p_where
                                , 100000, " ", out ret);
                    }
                    if (!ret.result)
                    {
                        return;
                    }
                    UpdateStatistics(false, chargeXX.paramcalc, chargeXX.reval_tab, out ret);
                    if (!ret.result)
                    {
                        return;
                    }
                }
            }
            else
            {
                string conn_kernel = Points.GetConnByPref(chargeXX.paramcalc.pref);
                IDbConnection conn_db = GetConnection(conn_kernel);
                //IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
                ret = OpenDb(conn_db, true);
                if (!ret.result)
                {
                    return;
                }
                ret = ExecSQL(conn_db,
#if PG
                    " set search_path to '" + chargeXX.paramcalc.pref + "_charge_" + (chargeXX.paramcalc.cur_yy - 2000).ToString("00")+"'"
#else
                    " database " + chargeXX.paramcalc.pref + "_charge_" + (chargeXX.paramcalc.cur_yy - 2000).ToString("00")
#endif
                    , true);

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
                if (!ret.result)
                {
                    conn_db.Close();
                    return;
                }
                ret = ExecSQL(conn_db,
                    " Create unique index " + tbluser + "irev1_" +
                    chargeXX.paramcalc.alias + "_" + chargeXX.paramcalc.cur_mm.ToString("00") +
                    " on " + chargeXX.reval_tab + " (nzp_reval) ", true);
                if (!ret.result)
                {
                    conn_db.Close();
                    return;
                }
                ret = ExecSQL(conn_db,
                " Create unique index " + tbluser + "irev2_" +
                    chargeXX.paramcalc.alias + "_" + chargeXX.paramcalc.cur_mm.ToString("00") +
                                        " on " + chargeXX.reval_tab + " (nzp_kvar,nzp_serv,nzp_supp,year_,month_) ", true);
                if (!ret.result)
                {
                    conn_db.Close();
                    return;
                }
                ret = ExecSQL(conn_db,
                    " Create index " + tbluser + "irev3_" +
                    chargeXX.paramcalc.alias + "_" + chargeXX.paramcalc.cur_mm.ToString("00") +
                                        " on " + chargeXX.reval_tab + " (year_,month_) ", true);
                if (!ret.result)
                {
                    conn_db.Close();
                    return;
                }
                ret = ExecSQL(conn_db,
                    " Create index " + tbluser + "irev4_" +
                    chargeXX.paramcalc.alias + "_" + chargeXX.paramcalc.cur_mm.ToString("00") +
                                        " on " + chargeXX.reval_tab + " (nzp_kvar,year_,month_) ", true);
                if (!ret.result)
                {
                    conn_db.Close();
                    return;
                }
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


                    /*
                    string p_where = " Where nzp_kvar in ( Select nzp_kvar From t_selkvar )";
                    if (chargeXX.paramcalc.b_reval) //удалить по услуге
                        p_where = " a Where 0 < ( Select count(*) From t_mustcalc b " +
                                                " Where a.nzp_kvar = b.nzp_kvar and a.nzp_serv = ( case when b.nzp_serv > 1 then b.nzp_serv else a.nzp_serv end ) ) ";

                    ExecByStep(conn_db2, chargeXX.delta_xx, "nzp_delta",
                            " Delete From " + chargeXX.delta_xx +
                              p_where
                            , 100000, " ", out ret);
                    */

                    string p_where = " Where 1 = 1 ";
                    if (chargeXX.paramcalc.b_reval) //удалить по услуге
                        p_where = " Where 0 < ( Select count(*) From t_mustcalc b " +
                                              " Where " + chargeXX.delta_xx + ".nzp_kvar = b.nzp_kvar " +
                                              "   and " + chargeXX.delta_xx + ".nzp_serv = ( case when b.nzp_serv > 1 then b.nzp_serv else " + chargeXX.delta_xx + ".nzp_serv end ) ) ";


                    // удалить по месяцам в прошлом stek = 2
                    IDataReader reader;
                    ret = ExecRead(conn_db2, out reader,
                          " Select year_, month_ From " + chargeXX.delta_xx +
                          p_where +
                          " and stek = 1 and " + chargeXX.where_kvar +
                          " Group by 1,2 Order by 1,2 "
                        , true);
                    if (!ret.result)
                    {
                        return;
                    }
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
                                " and year_=" + chargeXX.paramcalc.cur_yy + " and month_=" + chargeXX.paramcalc.cur_mm +
                                " and stek = 2 and " + chargeXX.where_kvar
                                , true);
                            if (!ret.result)
                            {
                                reader.Close();
                                return;
                            }
                        }
                        reader.Close();
                    }
                    catch (Exception ex)
                    {
                        reader.Close();
                        ret.result = false;
                        ret.text = ex.Message;
                        return;
                    }
                    //

                    if (chargeXX.paramcalc.nzp_kvar > 0 || chargeXX.paramcalc.nzp_dom > 0)
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

                    if (!ret.result)
                    {
                        return;
                    }
                    UpdateStatistics(false, chargeXX.paramcalc, chargeXX.delta_tab, out ret);
                    if (!ret.result)
                    {
                        return;
                    }
                }
            }
            else
            {
                string conn_kernel = Points.GetConnByPref(chargeXX.paramcalc.pref);
                IDbConnection conn_db = GetConnection(conn_kernel);
                //IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
                ret = OpenDb(conn_db, true);
                if (!ret.result)
                {
                    return;
                }
                ret = ExecSQL(conn_db,
#if PG
                        " set search_path to '" + chargeXX.paramcalc.pref + "_charge_" + (chargeXX.paramcalc.cur_yy - 2000).ToString("00")+"'"
#else
                        " database " + chargeXX.paramcalc.pref + "_charge_" + (chargeXX.paramcalc.cur_yy - 2000).ToString("00")
#endif
                    , true);

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
                if (!ret.result)
                {
                    conn_db.Close();
                    return;
                }
                ret = ExecSQL(conn_db,
                    " Create unique index " + tbluser + "idlt1_" 
                    + chargeXX.paramcalc.alias + "_" + chargeXX.paramcalc.cur_mm.ToString("00") +
                                        " on " + chargeXX.delta_tab + " (nzp_delta) ", true);
                if (!ret.result)
                {
                    conn_db.Close();
                    return;
                }
                ret = ExecSQL(conn_db,
                    " Create unique index " + tbluser + "idlt2_" 
                    + chargeXX.paramcalc.alias + "_" + chargeXX.paramcalc.cur_mm.ToString("00") +
                                        " on " + chargeXX.delta_tab + " (nzp_kvar,nzp_serv,stek,year_,month_) ", true);
                if (!ret.result)
                {
                    conn_db.Close();
                    return;
                }
                ret = ExecSQL(conn_db,
                    " Create index " + tbluser + "idlt3_" +
                    chargeXX.paramcalc.alias + "_" + chargeXX.paramcalc.cur_mm.ToString("00") +
                                        " on " + chargeXX.delta_tab + " (year_,month_) ", true);
                if (!ret.result)
                {
                    conn_db.Close();
                    return;
                }
                ret = ExecSQL(conn_db,
                    " Create index " + tbluser + "idlt4_" 
                    + chargeXX.paramcalc.alias + "_" + chargeXX.paramcalc.cur_mm.ToString("00") +
                                        " on " + chargeXX.delta_tab + " (nzp_kvar,year_,month_) ", true);
                if (!ret.result)
                {
                    conn_db.Close();
                    return;
                }
                ret = ExecSQL(conn_db,
                    " Create index " + tbluser + "idlt5_" 
                    + chargeXX.paramcalc.alias + "_" + chargeXX.paramcalc.cur_mm.ToString("00") +
                                        " on " + chargeXX.delta_tab + " (nzp_dom,nzp_serv,stek) ", true);
                if (!ret.result)
                {
                    conn_db.Close();
                    return;
                }
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
                if (!ret.result)
                {
                    return;
                }
                ret = ExecSQL(conn_db,
#if PG                    
                    " set search_path to '" + chargeXX.paramcalc.pref + "_charge_" + (chargeXX.paramcalc.calc_yy - 2000).ToString("00")+ "'"
#else
                    " Database " + chargeXX.paramcalc.pref + "_charge_" + (chargeXX.paramcalc.calc_yy - 2000).ToString("00")
#endif
                    , true);

                ret = ExecSQL(conn_db,
                    " Create table " + tbluser + chargeXX.charge_tab +
                    " (  nzp_charge     serial not null, " +
                    "    nzp_kvar       integer, " +
                    "    num_ls         integer, " +
                    "    nzp_serv       integer, " +
                    "    nzp_supp       integer, " +
                    "    nzp_frm        integer, " +
                    "    dat_charge     date, " +
                    "    tarif          " + sDecimalType + "(14,3) default 0.000, " +
                    "    tarif_p        " + sDecimalType + "(14,3) default 0.000, " +
                    "    gsum_tarif     " + sDecimalType + "(14,2) default 0.00, " +
                    "    rsum_tarif     " + sDecimalType + "(14,2) default 0.00, " +
                    "    rsum_lgota     " + sDecimalType + "(14,2) default 0.00, " +
                    "    sum_tarif      " + sDecimalType + "(14,2) default 0.00, " +
                    "    sum_dlt_tarif  " + sDecimalType + "(14,2) default 0.00, " +
                    "    sum_dlt_tarif_p " + sDecimalType + "(14,2) default 0.00, " +
                    "    sum_tarif_p     " + sDecimalType + "(14,2) default 0.00, " +
                    "    sum_lgota       " + sDecimalType + "(14,2) default 0.00, " +
                    "    sum_dlt_lgota   " + sDecimalType + "(14,2) default 0.00, " +
                    "    sum_dlt_lgota_p " + sDecimalType + "(14,2) default 0.00, " +
                    "    sum_lgota_p    " + sDecimalType + "(14,2) default 0.00, " +
                    "    sum_nedop      " + sDecimalType + "(14,2) default 0.00, " +
                    "    sum_nedop_p    " + sDecimalType + "(14,2) default 0.00, " +
                    "    sum_real       " + sDecimalType + "(14,2) default 0.00, " +
                    "    sum_charge     " + sDecimalType + "(14,2) default 0.00, " +
                    "    reval          " + sDecimalType + "(14,2) default 0.00, " +
                    "    real_pere      " + sDecimalType + "(14,2) default 0.00, " +
                    "    sum_pere       " + sDecimalType + "(14,2) default 0.00, " +
                    "    real_charge    " + sDecimalType + "(14,2) default 0.00, " +
                    "    sum_money      " + sDecimalType + "(14,2) default 0.00, " +
                    "    money_to       " + sDecimalType + "(14,2) default 0.00, " +
                    "    money_from     " + sDecimalType + "(14,2) default 0.00, " +
                    "    money_del      " + sDecimalType + "(14,2) default 0.00, " +
                    "    sum_fakt       " + sDecimalType + "(14,2) default 0.00, " +
                    "    fakt_to        " + sDecimalType + "(14,2) default 0.00, " +
                    "    fakt_from      " + sDecimalType + "(14,2) default 0.00, " +
                    "    fakt_del       " + sDecimalType + "(14,2) default 0.00, " +
                    "    sum_insaldo    " + sDecimalType + "(14,2) default 0.00, " +
                    "    izm_saldo         " + sDecimalType + "(14,2) default 0.00, " +
                    "    sum_outsaldo      " + sDecimalType + "(14,2) default 0.00, " +
                    "    sum_subsidy       " + sDecimalType + "(14,2) default 0.00, " +
                    "    sum_subsidy_p     " + sDecimalType + "(14,2) default 0.00, " +
                    "    sum_subsidy_reval " + sDecimalType + "(14,2) default 0.00, " +
                    "    sum_subsidy_all   " + sDecimalType + "(14,2) default 0.00, " +
                    "    isblocked      integer, " +
                    "    is_device      integer default 0, " +
                    "    c_calc         " + sDecimalType + "(14,2), " +
                    "    c_sn           " + sDecimalType + "(14,2) default 0, " +
                    "    c_okaz         " + sDecimalType + "(14,2), " +
                    "    c_nedop        " + sDecimalType + "(14,2), " +
                    "    isdel          integer, " +
                    "    c_reval            " + sDecimalType + "(14,2), " +
                    "    sum_tarif_f        " + sDecimalType + "(14,2) default 0.00 not null, " +
                    "    sum_tarif_f_p      " + sDecimalType + "(14,2) default 0.00 not null, " +
                    "    sum_tarif_sn_eot   " + sDecimalType + "(14,2) default 0.00 not null, " +
                    "    sum_tarif_sn_eot_p " + sDecimalType + "(14,2) default 0.00 not null, " +
                    "    sum_tarif_sn_f     " + sDecimalType + "(14,2) default 0.00 not null, " +
                    "    sum_tarif_sn_f_p   " + sDecimalType + "(14,2) default 0.00 not null, " +
                    "    tarif_f            " + sDecimalType + "(14,4) default 0.00 not null, " +
                    "    tarif_f_p          " + sDecimalType + "(14,4) default 0.00 not null, " +
                    "    order_print    integer default 0 ) "
                          , true);
                if (!ret.result)
                {
                    conn_db.Close();
                    return;
                }
                ret = ExecSQL(conn_db,
                    " Create unique index " + tbluser + "ic1_" 
                    + chargeXX.paramcalc.alias + "_" + chargeXX.paramcalc.calc_mm.ToString("00") +
                                        " on " + chargeXX.charge_tab + " (nzp_charge) ", true);
                if (!ret.result)
                {
                    conn_db.Close();
                    return;
                }
                ret = ExecSQL(conn_db,
                    " Create index " + tbluser + "ic2_"
                    + chargeXX.paramcalc.alias + "_" + chargeXX.paramcalc.calc_mm.ToString("00") + 
                                        " on " + chargeXX.charge_tab + " (nzp_kvar, nzp_serv, dat_charge) ", true);
                if (!ret.result)
                {
                    conn_db.Close();
                    return;
                }
                ret = ExecSQL(conn_db,
                    " Create index " + tbluser + "ic3_" 
                    + chargeXX.paramcalc.alias + "_" + chargeXX.paramcalc.calc_mm.ToString("00") + 
                                        " on " + chargeXX.charge_tab + " (nzp_kvar, nzp_serv, nzp_supp) ", true);
                if (!ret.result)
                {
                    conn_db.Close();
                    return;
                }
                ret = ExecSQL(conn_db,
                    " Create index " + tbluser + "ic4_" 
                    + chargeXX.paramcalc.alias + "_" + chargeXX.paramcalc.calc_mm.ToString("00") + 
                                        " on " + chargeXX.charge_tab + " (nzp_kvar, nzp_supp, dat_charge) ", true);
                if (!ret.result)
                {
                    conn_db.Close();
                    return;
                }
                ret = ExecSQL(conn_db,
                    " Create index " + tbluser + "ic5_" 
                    + chargeXX.paramcalc.alias + "_" + chargeXX.paramcalc.calc_mm.ToString("00") + 
                                        " on " + chargeXX.charge_tab + " (num_ls) ", true);
                if (!ret.result)
                {
                    conn_db.Close();
                    return;
                }
                ret = ExecSQL(conn_db,
                    " Create index " + tbluser + "ic6_" 
                    + chargeXX.paramcalc.alias + "_" + chargeXX.paramcalc.calc_mm.ToString("00") + 
                                        " on " + chargeXX.charge_tab + " (num_ls, nzp_serv, nzp_supp) ", true);
                if (!ret.result)
                {
                    conn_db.Close();
                    return;
                }
                ret = ExecSQL(conn_db,
                    " Create index " + tbluser + "ic7_" 
                    + chargeXX.paramcalc.alias + "_" + chargeXX.paramcalc.calc_mm.ToString("00") + 
                                        " on " + chargeXX.charge_tab + " (nzp_serv) ", true);
                if (!ret.result)
                {
                    conn_db.Close();
                    return;
                }

                ExecSQL(conn_db, sUpdStat + " " + chargeXX.charge_tab, true);

                conn_db.Close();
            }
        }


        #region Формирование протокола расчета
        //-----------------------------------------------------------------------------

        string ReplFrmValues(string pCurOpisFrm, string[] pNames, string[] pVals)
        {
            string sResOpisFrm = pCurOpisFrm;

            string sCurOpisFrm = pCurOpisFrm;
            int iPos = 1;

            while (iPos > 0)
            {
                string sNameValues = "";

                int iPosEnd = sCurOpisFrm.Length;
                //StringComparison.InvariantCulture;
                iPos = sCurOpisFrm.IndexOf('[');

                if (iPos > 0)
                {
                    bool bIgnore = false;

                    for (int i = iPos + 1; i <= sCurOpisFrm.Length; i++)
                    {
                        if (sCurOpisFrm[i] == '<') { bIgnore = true; }

                        if (sCurOpisFrm[i] == '>') { bIgnore = false; continue; }

                        if (bIgnore) { continue; }
                        //
                        if (sCurOpisFrm[i] == ']') { iPosEnd = i; break; }
                        sNameValues = sNameValues + sCurOpisFrm[i];
                    }
                    //
                    string sValue = ""; // FloatToStr(Formuls_Calc.Export_Data(sNameValues, False));
                    for (int i = 0; i < pNames.Length; i++)
                    {
                        if (sNameValues == pNames[i])
                        {
                            sValue = pVals[i]; break;
                        }
                    }
                    //

                    string sTmpPrm = sCurOpisFrm.Substring(iPos, iPosEnd - iPos);

                    string sTmpPrmRes = sTmpPrm.Replace(sNameValues, sValue);

                    sResOpisFrm = sResOpisFrm.Replace(sTmpPrm, sTmpPrmRes);

                    string sTmpBeg = sCurOpisFrm.Substring(0, iPos);
                    int ilength = (sCurOpisFrm.Trim()).Length - 1;
                    //string sTmpEnd = sCurOpisFrm.Substring(iPos + 1, ilength);
                    string sTmpEnd = sCurOpisFrm.Substring(iPos + 1);
                    sCurOpisFrm = sTmpBeg + " " + sTmpEnd;

                }

            }

            sResOpisFrm = sResOpisFrm.Replace('[', ' ');
            sResOpisFrm = sResOpisFrm.Replace(']', ' ');

            return sResOpisFrm;
        }

        string GetDescrFrm(IDbConnection conn_db, ChargeXX chargeXX, int nzp_frm, out Returns ret)
        { 
            IDataReader reader;

            string sText = "";
            ret = ExecRead(conn_db, out reader, " Select prot_html From " + chargeXX.paramcalc.kernel_alias + "frm_descr Where nzp_frm="
                + nzp_frm, true);
            if (!ret.result) 
            { 
                return sText;
            }

            try
            {
                if (reader.Read())
                {
                    if (reader["prot_html"] != DBNull.Value)
                      sText = ((string)reader["prot_html"]).Trim(); 
                }
                else
                { ret.tag = -1; }
            }
            catch (Exception ex)
            {
                reader.Close();
                ret.result = false;
                ret.text = ex.Message;

                return sText;
            }
            reader.Close();

            return sText;
        }

        bool _GetValPrm(IDbConnection conn_db, ParamCalc paramcalc, bool reg, string inumprm, string prm_nm, string nzp_nm, out Returns ret)
        {
            ret = Utils.InitReturns();
            IDataReader reader;

            string snzp_prm = "";
            if (reg)
            { snzp_prm = "(select " + prm_nm + " from tls where " + prm_nm + ">0 )"; }
            else
            { snzp_prm = prm_nm;}

            string snzp = "";
            if (prm_nm=="5")
            { snzp = "0"; }
            else
            { snzp = "(select " + nzp_nm + " from tls)"; }

            ret = ExecRead(conn_db, out reader,
                " Select p.val_prm,n.name_prm From " + paramcalc.data_alias + "prm_" + inumprm + " p, " + paramcalc.kernel_alias + "prm_name n" +
                " Where p.nzp_prm=n.nzp_prm and p.nzp_prm=" + snzp_prm + " and p.nzp= " + snzp +
                " and p.is_actual<>100" +
#if PG
                " and p.dat_s<=" + MDY(paramcalc.calc_mm, 28, paramcalc.calc_yy) +
                " and p.dat_po>=" + MDY(paramcalc.calc_mm, 1, paramcalc.calc_yy)
#else
                " and p.dat_s<=mdy(" + paramcalc.calc_mm + ",28," + paramcalc.calc_yy + ")" +
                " and p.dat_po>=mdy(" + paramcalc.calc_mm + ",1," + paramcalc.calc_yy + ")"
#endif
                , true);
            if (!ret.result) { return false; }

            try
            {
                if (reader.Read())
                {
                    if (reader["val_prm"] != DBNull.Value)
                    {
                        if (reg)
                        { ret.text = ((string)reader["val_prm"])+ "(" + ((string)reader["name_prm"]) + ")"; }
                        else
                        { ret.text = ((string)reader["val_prm"]); }
                    }    
                }
            }
            catch (Exception ex)
            {
                reader.Close();
                ret.result = false;
                ret.text = ex.Message;

                ExecSQL(conn_db, " Drop table tls ", false);
                return false;
            }
            reader.Close();

            return true;
        }

        //-----------------------------------------------------------------------------
        public bool MakeProtCalcForMonth(IDbConnection conn_db, ParamCalc paramcalc1, int nzp_serv, int nzp_supp, out Returns ret)
        //-----------------------------------------------------------------------------
        {
            ret = Utils.InitReturns();

            /*
            входящие параметры:
            1. параметры расчета - ParamCalc paramcalc = new ParamCalc(nzp_kvar, 0, pref, calc_yy, calc_mm, cur_yy, cur_mm);
               paramcalc.num_ls = <num_ls>
            2. nzp_serv - код услуги
            3. nzp_supp - код поставщика
             
            выходящие параметры:
            ret.tag = 0 & ret.result = true - нормальное завершение
            ret.text = html-текст для отображения
            */
            /* пока отложим... Thread???
            //Новая культура для дат------------------------
            CultureInfo newCI = (CultureInfo)Thread.CurrentThread.CurrentCulture.Clone();
            newCI.NumberFormat.NumberDecimalSeparator = ".";
            newCI.DateTimeFormat.ShortDatePattern = "dd.MM.yyyy";
            newCI.DateTimeFormat.LongTimePattern = "";
            Thread.CurrentThread.CurrentCulture = newCI;
            //Конец Новая культура для дат------------------------
            */
            if ( (paramcalc1.nzp_kvar > 0) && (nzp_serv > 0) && (nzp_supp > 0) )
            {
                #region выбираем начисления/тип расчета
                string[] month_names = new string[12];
                month_names[0]  = "январь";
                month_names[1]  = "февраль";
                month_names[2]  = "март";
                month_names[3]  = "апрель";
                month_names[4]  = "май";
                month_names[5]  = "июнь";
                month_names[6]  = "июль";
                month_names[7]  = "август";
                month_names[8]  = "сентябрь";
                month_names[9]  = "октябрь";
                month_names[10] = "ноябрь";
                month_names[11] = "декабрь";

                ParamCalc paramcalc = new ParamCalc(paramcalc1.nzp_kvar, 0, paramcalc1.pref, paramcalc1.calc_yy, paramcalc1.calc_mm, paramcalc1.cur_yy, paramcalc1.cur_mm);

                paramcalc.b_reval = false; //нельзя включать! - включается только в CalcRevalXX
                paramcalc.b_handl = !((paramcalc.cur_yy == paramcalc.calc_yy) && (paramcalc.cur_mm == paramcalc.calc_mm));

                paramcalc.b_report = false;
                paramcalc.b_must = false;

                ChargeXX chargeXX = new ChargeXX(paramcalc);

                // выбираем перечень параметров протокола для формулы/типа расчета для заголовка протокола
                string[] param_name = new string[9];
                string[] val_param = new string[9];

                param_name[0] = "calcmn";
                param_name[1] = "calcyr";
                param_name[2] = "recalc";

                param_name[3] = "service";
                param_name[4] = "name_supp";

                param_name[5] = "result";
                param_name[6] = "tarif";
                param_name[7] = "rashod";

                param_name[8] = "nedop";
                //
                val_param[0] = month_names[paramcalc.calc_mm-1];
                val_param[1] = paramcalc.calc_yy.ToString();

                val_param[2] = "";
                if (!paramcalc.b_cur)
                {
                    val_param[2] = " (Перерасчет за " + month_names[paramcalc.cur_mm-1]+ " " + paramcalc.cur_yy.ToString() + "г)";
                }
                //
                ExecSQL(conn_db, " Drop table tls ", false);

                string p_dat_charge = " '' ";
                if (chargeXX.paramcalc.b_cur)
                { 
                    p_dat_charge = "dat_charge is null"; 
                }
                else
                { 
                    p_dat_charge = "dat_charge=" + MDY(chargeXX.paramcalc.cur_mm, 28, chargeXX.paramcalc.cur_yy); 
                }

                // выбираем начисления/тип расчета по ЛС
                ret = ExecSQL(conn_db,
#if PG
                      " Select " +
                      "  k.nzp_dom,k.nzp_kvar,c.nzp_serv,c.nzp_supp,c.nzp_frm," +
                      "  c.tarif,c.rsum_tarif,c.sum_nedop,c.is_device," +
                      "  coalesce(f.nzp_frm_typ,0) nzp_frm_typ,coalesce(f.nzp_frm_typrs,0) nzp_frm_typrs," +
                      "  coalesce(f.nzp_prm_tarif_ls,0) nzp_prm_tarif_ls,coalesce(f.nzp_prm_tarif_lsp,0) nzp_prm_tarif_lsp," +
                      "  coalesce(f.nzp_prm_tarif_dm,0) nzp_prm_tarif_dm,coalesce(f.nzp_prm_tarif_su,0) nzp_prm_tarif_su,coalesce(f.nzp_prm_tarif_bd,0) nzp_prm_tarif_bd," +
                      "  coalesce(f.nzp_prm_rash,0) nzp_prm_rash,coalesce(f.nzp_prm_rash1,0) nzp_prm_rash1,coalesce(f.nzp_prm_rash2,0) nzp_prm_rash2" +
                      " into temp tls "+
                      " From " + chargeXX.charge_xx_ishod + " c"+
                      " left outer join " + chargeXX.paramcalc.pref + "_kernel.formuls_opis f on c.nzp_frm=f.nzp_frm, " +
                      chargeXX.paramcalc.pref + "_data.kvar k " +
                      " Where c.nzp_kvar=k.nzp_kvar" +
                      " and c." + chargeXX.where_kvar +
                      " and c.nzp_serv=" + nzp_serv + " and c.nzp_supp=" + nzp_supp + " and c." + p_dat_charge                  
                    
#else
                      " Select " +
                      "  k.nzp_dom,k.nzp_kvar,c.nzp_serv,c.nzp_supp,c.nzp_frm," +
                      "  c.tarif,c.rsum_tarif,c.sum_nedop,c.is_device," +
                      "  nvl(f.nzp_frm_typ,0) nzp_frm_typ,nvl(f.nzp_frm_typrs,0) nzp_frm_typrs," +
                      "  nvl(f.nzp_prm_tarif_ls,0) nzp_prm_tarif_ls,nvl(f.nzp_prm_tarif_lsp,0) nzp_prm_tarif_lsp," +
                      "  nvl(f.nzp_prm_tarif_dm,0) nzp_prm_tarif_dm,nvl(f.nzp_prm_tarif_su,0) nzp_prm_tarif_su,nvl(f.nzp_prm_tarif_bd,0) nzp_prm_tarif_bd," +
                      "  nvl(f.nzp_prm_rash,0) nzp_prm_rash,nvl(f.nzp_prm_rash1,0) nzp_prm_rash1,nvl(f.nzp_prm_rash2,0) nzp_prm_rash2" +
                      " From " + chargeXX.charge_xx_ishod + " c," + chargeXX.paramcalc.pref + "_data:kvar k," +
                      " outer " + chargeXX.paramcalc.pref + "_kernel:formuls_opis f " + 
                      " Where c.nzp_kvar=k.nzp_kvar" +
                      " and c." + chargeXX.where_kvar +
                      " and c.nzp_serv=" + nzp_serv + " and c.nzp_supp=" + nzp_supp + " and c." + p_dat_charge +
                      " and c.nzp_frm=f.nzp_frm " +
                      " into temp tls with no log " 
#endif
                    , true);
                if (!ret.result) { return false; }

                ExecSQL(conn_db, sUpdStat + " tls ", true);
                #endregion выбираем начисления/тип расчета

                #region выбираем наименования

                IDataReader reader;

                // выбираем наименования услуги/поставщика/формулы/ед.езмерения
                ret = ExecRead(conn_db, out reader,
#if PG
                    " Select coalesce(s.service,'') service,coalesce(p.name_supp,'') name_supp,coalesce(m.measure,'') measure,t.*" +
                    " From tls t "+
                    " left outer join " + chargeXX.paramcalc.pref + "_kernel.services s on t.nzp_serv=s.nzp_serv " +
                    " left outer join " + chargeXX.paramcalc.pref + "_kernel.supplier p on t.nzp_supp=p.nzp_supp " +
                    " left outer join " + chargeXX.paramcalc.pref + "_kernel.formuls f  " +
                        " left outer join " + chargeXX.paramcalc.pref + "_kernel.s_measure m on f.nzp_measure=m.nzp_measure" +
                    " on t.nzp_frm=f.nzp_frm" +
                    " Where 1=1 "
#else
                    " Select nvl(s.service,'') service,nvl(p.name_supp,'') name_supp,nvl(m.measure,'') measure,t.*" +
                    " From tls t, outer " + chargeXX.paramcalc.pref + "_kernel:services s, outer " + chargeXX.paramcalc.pref + "_kernel:supplier p," +
                    " outer (" + chargeXX.paramcalc.pref + "_kernel:formuls f, " + chargeXX.paramcalc.pref + "_kernel:s_measure m) " +
                    " Where t.nzp_serv=s.nzp_serv and t.nzp_supp=p.nzp_supp and t.nzp_frm=f.nzp_frm and f.nzp_measure=m.nzp_measure "
#endif
                    , true);
                if (!ret.result) { ExecSQL(conn_db, " Drop table tls ", false); return false; }

                int nzp_frm = 0;
                int nzp_frm_typ = 0;
                int nzp_frm_typrs = 0;
                decimal sum_nedop = 0;
                decimal rsum_tarif = 0;
                string service = "";
                string name_supp = "";
                string measure = "";
                int is_device = 0;
                try
                {
                    if (reader.Read())
                    {
                        if (reader["service"] != DBNull.Value)       service   = ((string)reader["service"]);
                        if (reader["name_supp"] != DBNull.Value)     name_supp = ((string)reader["name_supp"]);
                        if (reader["measure"] != DBNull.Value)       measure   = ((string)reader["measure"]);
                        if (reader["nzp_frm"] != DBNull.Value)       nzp_frm       = ((int)reader["nzp_frm"]);
                        if (reader["nzp_frm_typ"] != DBNull.Value)   nzp_frm_typ   = ((int)reader["nzp_frm_typ"]);
                        if (reader["nzp_frm_typrs"] != DBNull.Value) nzp_frm_typrs = ((int)reader["nzp_frm_typrs"]);
                        if (reader["rsum_tarif"] != DBNull.Value)    rsum_tarif    = ((decimal)reader["rsum_tarif"]);
                        if (reader["sum_nedop"] != DBNull.Value)     sum_nedop     = ((decimal)reader["sum_nedop"]);
                        if (reader["is_device"] != DBNull.Value)     is_device     = ((int)reader["is_device"]);
                    }
                }
                catch (Exception ex)
                {
                    reader.Close();
                    ret.result = false;
                    ret.text = ex.Message;

                    ExecSQL(conn_db, " Drop table tls ", false);
                    return false;
                }
                reader.Close();

                val_param[3] = service;
                val_param[4] = name_supp;
                val_param[5] = rsum_tarif.ToString();

                val_param[8] = "";
                #endregion выбираем наименования

                #region отображение недопоставки
                // отображение недопоставки если она была
                if (sum_nedop > 0) 
                {
                    val_param[8] = "Рассчитана недопоставка = " + sum_nedop.ToString() + ".";

                    ret = ExecRead(conn_db, out reader,
                          " Select " +
                          "  n.nzp_serv,n.nzp_kind,u.name,n.koef,n.cnts,n.cnts_del,n.tn,n.dat_s,n.dat_po,n.perc,n.kod_info " +
                          " From " + chargeXX.calc_nedo_xx + " n," + chargeXX.paramcalc.data_alias + "upg_s_kind_nedop u " +
                          " Where n.nzp_kind=u.nzp_kind and u.kod_kind=1 and n." + chargeXX.where_kvar + " and n.nzp_serv=" + nzp_serv
                        , true);
                    if (!ret.result) { ExecSQL(conn_db, " Drop table tls ", false); return false; }

                    try
                    {
                        if (reader.Read())
                        {
                            decimal koef = 0;
                            if (reader["koef"] != DBNull.Value) koef = ((decimal)reader["koef"]);
                            string cnts = "0";
                            if (reader["cnts"] != DBNull.Value) cnts = Convert.ToString(reader["cnts"]);
                                //((int)reader["cnts"]);
                            decimal perc = 0;
                            if (reader["perc"] != DBNull.Value) perc = ((decimal)reader["perc"]);
                            
                            val_param[8] = val_param[8].Trim() +
                                " Тип недопоставки:" + ((string)reader["name"]) + "." +
                                " Доля возврата = " + koef.ToString() + "." +
                                " Количество дней недопоставки для учета = " + cnts + "." +
                                " % возврата = " + perc.ToString() + "." +
                                " Период: с " + 
                                Convert.ToString(reader["dat_s"]).Trim() +
                                " по " +
                                Convert.ToString(reader["dat_po"]).Trim() + "." +
                                "";
                        }
                    }
                    catch (Exception ex)
                    {
                        reader.Close();
                        ret.result = false;
                        ret.text = ex.Message;

                        ExecSQL(conn_db, " Drop table tls ", false);
                        return false;
                    }
                    reader.Close();

                    val_param[8] = val_param[8].Trim() + "<br />";
                }
                #endregion отображение недопоставки

                #region выборка тарифа и расхода

                // для всех типов расчета - выборка тарифа и расхода
                decimal tarif = 0;
                decimal rashod = 0;
                ret = ExecRead(conn_db, out reader,
                      " Select " +
                      "  r.nzp_prm_tarif, r.nzp_prm_rashod, r.tarif, r.rashod, r.gil, r.squ, r.nzp_frm_typ, r.nzp_frm_typrs" +
                      " From " + chargeXX.calc_gku_xx + " r" +
                      " Where r." + chargeXX.where_kvar +
                      " and r.nzp_serv=" + nzp_serv + " and r.nzp_supp=" + nzp_supp + " and r.nzp_frm=" + nzp_frm
                    , true);
                if (!ret.result) { ret.tag = 1; return false; }

                try
                {
                    if (reader.Read())
                    {
                        if (reader["tarif"] != DBNull.Value)  tarif = ((decimal)reader["tarif"]);
                        if (reader["rashod"] != DBNull.Value) rashod = ((decimal)reader["rashod"]);
                    }
                }
                catch (Exception ex)
                {
                    reader.Close();
                    ret.result = false;
                    ret.text = ex.Message;

                    ExecSQL(conn_db, " Drop table tls ", false);
                    return false;
                }
                reader.Close();

                val_param[6] = tarif.ToString();
                val_param[7] = rashod.ToString();
                if (measure.Trim()!="") {val_param[7] = val_param[7]+ " (" + measure.Trim()+")"; }
                #endregion выборка тарифа и расхода

                #region заголовок html-файла
                // выбираем заголовок html-файла
                string sHeaderHtml = GetDescrFrm(conn_db, chargeXX, -1, out ret);
                if (!ret.result) { ExecSQL(conn_db, " Drop table tls ", false); return false; }

                // выбираем "охвосток" html-файла
                string sFooterHtml = GetDescrFrm(conn_db, chargeXX, -2, out ret);
                if (!ret.result) { ExecSQL(conn_db, " Drop table tls ", false); return false; }

                // выбираем текст протокола для формулы/типа расчета
                string sBodyHtml = GetDescrFrm(conn_db, chargeXX, nzp_frm, out ret);
                if (!ret.result) { ExecSQL(conn_db, " Drop table tls ", false); return false; }
                //if (sBodyHtml.Length <= 0) { sBodyHtml = "Описание расчета не найдено."; }

                // заполнение значений параметров в шаблоне заголовка html-файла 
                sHeaderHtml = ReplFrmValues(sHeaderHtml, param_name, val_param);

                // выбираем перечень параметров протокола для формулы/типа расчета для содержания/тела протокола
                string sBodyText = "<br />";
                #endregion заголовок html-файла

                bool bUseTypRS = true;

                // расшифровка тарифа по типу тарифа - nzp_frm_typ
                switch (nzp_frm_typ)
                {
                    case 1: // 2,12,26,40,101
                        #region расшифровка простого тарифа
                        {
                            sBodyText = sBodyText.Trim() + "Определение тарифа:";
                            sBodyText = sBodyText.Trim() + "<br />";

                            MakeValTarif(conn_db, paramcalc, out ret);
                            if (!ret.result) { ExecSQL(conn_db, " Drop table tls ", false); return false; }
                            sBodyText = sBodyText.Trim() + "Тариф на 1 " + measure + " = <font color='#0000FF'><b> " + ret.text.Trim() + "</b></font>";
                            sBodyText = sBodyText.Trim() + "<br />";
                        }
                        // если канализация и расход с куб.м
                        if ((nzp_serv == 7) && (nzp_frm_typrs == 390))
                        {
                            ret = ExecRead(conn_db, out reader,
                                  " Select nzp_kvar, " +
                                  " max(case when nzp_serv=6 then val1+val2 end) rashod_hv, " +
                                  " max(case when nzp_serv=9 then val1+val2 end) rashod_gv  " +
                                  " From " + chargeXX.counters_xx +
                                  " Where " + chargeXX.where_kvar + " and nzp_serv in (6,9) and stek=3 " +
                                  " group by nzp_kvar "
                                , true);
                            if (ret.result)
                            {
                                decimal rashod_hv = 0;
                                decimal rashod_gv = 0;
                                try
                                {
                                    if (reader.Read())
                                    {
                                        if (reader["rashod_hv"] != DBNull.Value) rashod_hv = ((decimal)reader["rashod_hv"]);
                                        if (reader["rashod_gv"] != DBNull.Value) rashod_gv = ((decimal)reader["rashod_gv"]);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    reader.Close();
                                    ret.result = false;
                                    ret.text = ex.Message;

                                    ExecSQL(conn_db, " Drop table tls ", false);
                                    return false;
                                }
                                reader.Close();
                                sBodyText = sBodyText.Trim() + "<br /> Определение расхода:";
                                sBodyText = sBodyText.Trim() + "<br />";

                                sBodyText = sBodyText.Trim() +
                                    "Расход по канализации (<font color='#0000FF'><b> " + rashod.ToString() + " </b></font>) = " +
                                    "Расход ХВС (<font color='#0000FF'><b> " + rashod_hv.ToString() + " </b></font>) + " +
                                    "Расход ГВС (<font color='#0000FF'><b> " + rashod_gv.ToString() + " </b></font>).";

                                sBodyText = sBodyText.Trim() + "<br />";
                            }
                            bUseTypRS = false;
                        }
                        break;
                        #endregion расшифровка простого тарифа
                    case 1140:
                        #region расшифровка расчета ГВС от ГКал
                        {
                            sBodyText = sBodyText.Trim() + "Определение тарифа:";
                            sBodyText = sBodyText.Trim() + "<br />";

                            MakeValTarif(conn_db, paramcalc, out ret);
                            if (!ret.result) { ExecSQL(conn_db, " Drop table tls ", false); return false; }

                            sBodyText = sBodyText.Trim() + "Тариф на 1 ГКал = <font color='#0000FF'><b> " + ret.text.Trim() + "</b></font>";
                            sBodyText = sBodyText.Trim() + "<br />";
                            sBodyText = sBodyText.Trim() + "<br /> Определение расхода:";
                            sBodyText = sBodyText.Trim() + "<br />";
                            sBodyText = sBodyText.Trim() +
                                "Расход в ГКал (<font color='#0000FF'><b>" + rashod + "</b></font>) = " +
                                "Норма в ГКал на подогрев 1 куб.метра воды (<font color='#0000FF'><b> ";

                            _GetValPrm(conn_db, paramcalc, false, "2", "436", "nzp_dom", out ret);
                            if (!ret.result) { ExecSQL(conn_db, " Drop table tls ", false); return false; }

                            if ((ret.text).Trim() != "")
                            {
                                sBodyText = sBodyText.Trim()+ " " + ret.text.Trim();
                            }
                            else 
                            {
                                sBodyText = sBodyText.Trim() + "-";
                            }
                            sBodyText = sBodyText.Trim() + " </b></font>) * ";

                            sBodyText = sBodyText.Trim() + " Расход ГВС в куб. метрах (<font color='#0000FF'><b> ";

                            if (is_device > 0)
                            {
                                sBodyText = sBodyText.Trim() + " Расход по ИПУ ";
                            }
                            else
                            {
                                sBodyText = sBodyText.Trim() + " Расход по нормативу ";
                            }

                            ret = ExecRead(conn_db, out reader,
                                  " Select nzp_kvar,nzp_serv, " +
                                  " max(case when stek=30 then val1 end) rashod_norm, " +
                                  " max(case when stek=30 and cnt1>0 then val1/cnt1 end) rashod_norm_ch, " +
                                  " max(case when stek=30 then cnt1 end) kol_gil, " +
                                  " max(case when stek=1  then val1 end) rashod_ipu, " +
                                  " max(case when stek=2  then val1 end) rashod_ipu_sr " +
                                  " From " + chargeXX.counters_xx + 
                                  " Where " + chargeXX.where_kvar + " and nzp_serv=" + nzp_serv + " and stek in (1,2,30) " +
                                  " group by nzp_kvar,nzp_serv "
                                , true);
                            if (ret.result)
                            {
                                decimal rashod_norm = 0;
                                decimal rashod_norm_ch = 0;
                                decimal rashod_ipu = 0;
                                decimal rashod_ipu_sr = 0;
                                int kol_gil = 0;
                                bool is_rashod_ipu = false;
                                try
                                {
                                    if (reader.Read())
                                    {
                                        if (reader["rashod_norm"]   != DBNull.Value) rashod_norm    = ((decimal)reader["rashod_norm"]);
                                        if (reader["rashod_norm_ch"]!= DBNull.Value) rashod_norm_ch = ((decimal)reader["rashod_norm_ch"]);
                                        if (reader["rashod_ipu"]    != DBNull.Value) { rashod_ipu = ((decimal)reader["rashod_ipu"]); is_rashod_ipu = true; };
                                        if (reader["rashod_ipu_sr"] != DBNull.Value) rashod_ipu_sr = ((decimal)reader["rashod_ipu_sr"]);
                                        if (reader["kol_gil"] != DBNull.Value) kol_gil = ((int)reader["kol_gil"]);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    reader.Close();
                                    ret.result = false;
                                    ret.text = ex.Message;

                                    ExecSQL(conn_db, " Drop table tls ", false);
                                    return false;
                                }
                                reader.Close();

                                if (is_device > 0)
                                {
                                    if (is_rashod_ipu)
                                    {
                                        sBodyText = sBodyText.Trim() + "= " + rashod_ipu.ToString();
                                    }
                                    else
                                    {
                                        sBodyText = sBodyText.Trim() + "(среднему расходу)= " + rashod_ipu_sr.ToString(); ;
                                    }
                                }
                                sBodyText = sBodyText.Trim() + " </b></font>). " +
                                    "<br /> Справочно:<br />" +
                                    "Норматив на ЛС в куб.м (" + rashod_norm.ToString() + ") = Норматив на человека в куб. метрах (" + rashod_norm_ch.ToString() +
                                    ") * Количество жильцов (" + kol_gil + ").";
                            }
                            else 
                            {
                                sBodyText = sBodyText.Trim() + " -- </b></font>) ";
                            }
                            bUseTypRS = false;
                        }
                        break;
                        #endregion расшифровка расчета ГВС от ГКал
                    default: 
                        { 
                        } 
                        break;
                }

                sBodyText = sBodyText.Trim() + "<br />";

                if (bUseTypRS) // расход расписан при расшифровки тарифа!
                {
                    // расшифровка расхода по типу расхода - nzp_frm_typrs
                    switch (nzp_frm_typrs)
                    {
                        case 3: // по жильцам
                            #region расшифровка расхода по жильцам
                            {
                                /*
                                select stek,cnt2,val5,val3, nzp_gil,cnt1,cnt3,dat_s,dat_po,'' fam,'' ima,'' otch,mdy(1,1,1900) dat_rog
                                from smr36_charge_12:gil_10 g
                                where g.stek=3 and g.nzp_kvar=(select nzp_kvar from tls)
                                union all
                                select g.stek,g.cnt2,g.val5,g.val3, g.nzp_gil,g.cnt1,g.cnt3,g.dat_s,g.dat_po,k.fam,k.ima,k.otch,k.dat_rog
                                from smr36_charge_12:gil_10 g,smr36_data:kart k
                                where g.nzp_gil=k.nzp_gil and g.nzp_kvar=(select nzp_kvar from tls) and k.isactual='1'
                                */
                            }
                            break;
                            #endregion расшифровка расхода по жильцам

                        case 5: // по расходу коммунальной услуги counters_xx
                            #region расшифровка расхода по коммунальной услуги - counters_xx
                            {
                                sBodyText = sBodyText.Trim() + "<br /> Определение расхода:";
                                sBodyText = sBodyText.Trim() + "<br />";
                                sBodyText = sBodyText.Trim() + " Расход в " + measure + " ";

                                string snzp_serv = nzp_serv.ToString();
                                if (nzp_serv < 500)
                                {
                                    if (is_device > 0)
                                    {
                                        sBodyText = sBodyText.Trim() + " Показания ИПУ ";
                                    }
                                    else
                                    {
                                        sBodyText = sBodyText.Trim() + " Норматив ";
                                    }
                                    if (nzp_serv == 14)
                                    {
                                        snzp_serv = "9";
                                    }
                                }
                                else
                                {
                                    snzp_serv = "(select nzp_serv_link from " + chargeXX.paramcalc.pref +
#if PG
                                            "_kernel.serv_odn where nzp_serv="
#else
                                            "_kernel:serv_odn where nzp_serv=" 
#endif
                                        + nzp_serv + ")";
                                }

                                ret = ExecRead(conn_db, out reader,
                                      " Select nzp_kvar,nzp_serv, " +
                                      " max(case when stek=30 then val1 end) rashod_norm, " +
                                      " max(case when stek=30 and cnt1>0 then val1/cnt1 end) rashod_norm_ch, " +
                                      " max(case when stek=30 then cnt1 end) kol_gil, " +
                                      " max(case when stek=3  then dop87 end) rashod_odn, " +
                                      " max(case when stek=3  then kf307 end) norma_odn, " +
                                      " max(case when stek=3  then squ1 end) pl_odn, " +
                                      " max(case when stek=1  then val1 end) rashod_ipu, " +
                                      " max(case when stek=2  then val1 end) rashod_ipu_sr " +
                                      " From " + chargeXX.counters_xx +
                                      " Where " + chargeXX.where_kvar + " and nzp_serv=" + snzp_serv + " and stek in (1,2,30,3) and nzp_type=3 " +
                                      " group by nzp_kvar,nzp_serv "
                                    , true);
                                if (ret.result)
                                {
                                    decimal rashod_norm = 0;
                                    decimal rashod_norm_ch = 0;
                                    decimal rashod_ipu = 0;
                                    decimal rashod_odn = 0;
                                    decimal norma_odn = 0;
                                    decimal pl_odn = 0;
                                    decimal rashod_ipu_sr = 0;
                                    int kol_gil = 0;
                                    bool is_rashod_ipu = false;
                                    try
                                    {
                                        if (reader.Read())
                                        {
                                            if (reader["rashod_norm"] != DBNull.Value) rashod_norm = ((decimal)reader["rashod_norm"]);
                                            if (reader["rashod_norm_ch"] != DBNull.Value) rashod_norm_ch = ((decimal)reader["rashod_norm_ch"]);
                                            if (reader["rashod_ipu"] != DBNull.Value) { rashod_ipu = ((decimal)reader["rashod_ipu"]); is_rashod_ipu = true; };
                                            if (reader["rashod_ipu_sr"] != DBNull.Value) rashod_ipu_sr = ((decimal)reader["rashod_ipu_sr"]);
                                            if (reader["kol_gil"] != DBNull.Value) kol_gil = ((int)reader["kol_gil"]);
                                            if (reader["rashod_odn"] != DBNull.Value) rashod_odn = ((decimal)reader["rashod_odn"]);
                                            if (reader["norma_odn"] != DBNull.Value) norma_odn = ((decimal)reader["norma_odn"]);
                                            if (reader["pl_odn"] != DBNull.Value) pl_odn = ((decimal)reader["pl_odn"]);
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        reader.Close();
                                        ret.result = false;
                                        ret.text = ex.Message;

                                        ExecSQL(conn_db, " Drop table tls ", false);
                                        return false;
                                    }
                                    reader.Close();

                                    if (nzp_serv < 500)
                                    {
                                        if ((is_device > 0) || (rashod_ipu > 0) || (rashod_ipu_sr > 0))
                                        {
                                            if (is_device == 0)
                                            {
                                                sBodyText = sBodyText.Trim() + " - по ИПУ!";
                                            }
                                            sBodyText = sBodyText.Trim() + "(<font color='#0000FF'><b>";
                                            if (is_rashod_ipu)
                                            {
                                                sBodyText = sBodyText.Trim() + "= " + rashod_ipu.ToString();
                                            }
                                            else
                                            {
                                                sBodyText = sBodyText.Trim() + "(среднему расходу)= " + rashod_ipu_sr.ToString(); ;
                                            }
                                            sBodyText = sBodyText.Trim() + " </b></font>). ";
                                        }
                                        sBodyText = sBodyText.Trim() +
                                            "<br /> Справочно:<br />" +
                                            "Норматив на ЛС в куб.м (" + rashod_norm.ToString() + ") = Норматив на человека в куб. метрах (" + rashod_norm_ch.ToString() +
                                            ") * Количество жильцов (" + kol_gil + ").";
                                    }
                                    else
                                    {
                                        sBodyText = sBodyText.Trim() + " (<font color='#0000FF'><b> " + rashod_odn.ToString() + " </b></font>) = " +
                                            "Общая площадь ЛС (<font color='#0000FF'><b> " + pl_odn.ToString() + " </b></font> кв.м) * " +
                                            "Норма расхода на 1 кв.м площади (<font color='#0000FF'><b> " + norma_odn.ToString() + " </b></font>)";

                                        sBodyText = sBodyText.Trim() + "<br /> Определение нормы расхода на 1 кв.м площади:";
                                        sBodyText = sBodyText.Trim() + "<br />";

                                        ret = ExecRead(conn_db, out reader,
                                              " Select nzp_kvar,nzp_serv, " +
                                              " max(case when stek=3  then (case when cnt_stage=0 then val3-val4 else rvirt-val4 end) end) rashod_norm_odn, " +
                                              " max(case when stek=3  then val3-val4 end) rashod_odnd, " +
                                              " max(case when stek=3  then vl210 end) norma_odn_kvm, " +
                                              " max(case when stek=3  then pu7kw end) pl_ls, " +
                                              " max(case when stek=354 then kf307f9-pu7kw end) squ_mop, " +
                                              " max(case when stek=354 then kf307f9 end) squ_dom, " +
                                              " max(case when stek=1  then val1 end) rashod_odpu, " +
                                              " max(case when stek=2  then val1 end) rashod_odpu_sr " +
                                              " From " + chargeXX.counters_xx +
                                              " Where nzp_dom in (select nzp_dom from tls) and nzp_serv=" + snzp_serv + " and stek in (1,2,3,354) and nzp_type=1 " +
                                              " group by nzp_kvar,nzp_serv "
                                            , true);
                                        if (ret.result)
                                        {

                                            decimal rashod_norm_odn = 0;
                                            decimal rashod_odnd = 0;
                                            decimal norma_odn_kvm = 0;
                                            decimal pl_ls = 0;
                                            decimal squ_dom = 0;
                                            decimal squ_mop = 0;
                                            decimal rashod_odpu = 0;
                                            decimal rashod_odpu_sr = 0;
                                            bool is_rashod_odpu = false;
                                            try
                                            {
                                                if (reader.Read())
                                                {
                                                    if (reader["rashod_norm_odn"] != DBNull.Value) rashod_norm_odn = ((decimal)reader["rashod_norm_odn"]);
                                                    if (reader["rashod_odnd"] != DBNull.Value) rashod_odnd = ((decimal)reader["rashod_odnd"]);
                                                    if (reader["norma_odn_kvm"] != DBNull.Value) norma_odn_kvm = ((decimal)reader["norma_odn_kvm"]);
                                                    if (reader["pl_ls"] != DBNull.Value) pl_ls = ((decimal)reader["pl_ls"]);
                                                    if (reader["squ_dom"] != DBNull.Value) squ_dom = ((decimal)reader["squ_dom"]);
                                                    if (reader["squ_mop"] != DBNull.Value) squ_mop = ((decimal)reader["squ_mop"]);
                                                    if (reader["rashod_odpu"] != DBNull.Value) { rashod_odpu = ((decimal)reader["rashod_odpu"]); is_rashod_odpu = true; };
                                                    if (reader["rashod_odpu_sr"] != DBNull.Value) { rashod_odpu_sr = ((decimal)reader["rashod_odpu_sr"]); is_rashod_odpu = true; };
                                                }
                                            }
                                            catch (Exception ex)
                                            {
                                                reader.Close();
                                                ret.result = false;
                                                ret.text = ex.Message;

                                                ExecSQL(conn_db, " Drop table tls ", false);
                                                return false;
                                            }
                                            reader.Close();

                                            sBodyText = sBodyText.Trim() + "Норма расхода на 1 кв.м = Расход ОДН";
                                            if (is_rashod_odpu)
                                            {
                                                sBodyText = sBodyText.Trim() + " по ОДПУ";
                                            }
                                            else
                                            {
                                                sBodyText = sBodyText.Trim() + " по нормативу";
                                            }
                                            sBodyText = sBodyText.Trim() + 
                                                " (<font color='#0000FF'><b> " + rashod_odnd.ToString() +" </b></font>) / " +
                                                "Площадь дома(<font color='#0000FF'><b> " + squ_dom.ToString() + " </b></font>) - " +
                                                "Площадь МОП дома(<font color='#0000FF'><b> " + squ_mop.ToString() + " </b></font>)";

                                            sBodyText = sBodyText.Trim() + "<br /> Справочно: <br />";
                                            sBodyText = sBodyText.Trim() +
                                                "Норматив ОДН (<font color='#0000FF'><b> " + rashod_norm_odn.ToString() + " </b></font>) = " +
                                                "Нормативный расход на 1 кв. метр площади МОП (<font color='#0000FF'><b> " + norma_odn_kvm.ToString() + " </b></font>) * " +
                                                "Площадь МОП дома (<font color='#0000FF'><b> " + squ_mop.ToString() + " </b></font>).";

                                        }
                                        else
                                        {
                                            sBodyText = sBodyText.Trim() + ret.text;
                                        }
                                        sBodyText = sBodyText.Trim() + "<br />";
                                    }
                                }
                                else
                                {
                                    sBodyText = sBodyText.Trim() + (ret.text).Trim() + "<br />";
                                }
                            }
                            break;
                            #endregion расшифровка расхода по коммунальной услуги - counters_xx

                        case 11:
                            #region расшифровка расхода по услугам с кв. метров
                            {
                                sBodyText = sBodyText.Trim() + "Определение расхода:";
                                sBodyText = sBodyText.Trim() + "<br />";

                                _GetValPrm(conn_db, paramcalc, false, "1", "3", "nzp_kvar", out ret);
                                if (!ret.result) { ExecSQL(conn_db, " Drop table tls ", false); return false; }

                                string sNmPrm = "nzp_prm_rash";
                                string sKommunal = "Для изолированных квартир берется";
                                if ((ret.text).Trim() == "2")
                                {
                                    sNmPrm = "nzp_prm_rash1";
                                    sKommunal = "Для коммунальных квартир берется";
                                }
                                sBodyText = sBodyText.Trim() + sKommunal;

                                _GetValPrm(conn_db, paramcalc, true, "1", sNmPrm, "nzp_kvar", out ret);
                                if (!ret.result) { ExecSQL(conn_db, " Drop table tls ", false); return false; }

                                if ((ret.text).Trim() != "")
                                {
                                    sBodyText = sBodyText.Trim() + " квартирный параметр: " + ret.text;
                                }

                            }
                            break;
                            #endregion расшифровка расхода по услугам с кв. метров
                        default:
                            {
                            }
                            break;
                    }
                }

                #region формируем html-файл
                // заполнение значений параметров в шаблоне тела html-файла 
                sBodyHtml = ReplFrmValues(sBodyHtml, param_name, val_param);

                // формируем html-файл для отображения 
                ret.text = sHeaderHtml + sBodyHtml + sBodyText + sFooterHtml;

                ExecSQL(conn_db, " Drop table tls ", false);
                #endregion формируем html-файл
            }
            else
            {
                ret.result = false;
                ret.tag = 2;
                ret.text = "Неопределены параметры для вывода протокола расчета. (" + paramcalc1.nzp_kvar + "/" + nzp_serv + "/" + nzp_supp + ")";
                MonitorLog.WriteLog(ret.text, MonitorLog.typelog.Info, 1, 2, true);
            }
           
            return true;
        }

        public bool MakeValTarif(IDbConnection conn_db, ParamCalc paramcalc, out Returns ret)
        //-----------------------------------------------------------------------------
        {
            ret = Utils.InitReturns();

            string sBodyText = "";

            if (Points.IsSmr)
            {
                _GetValPrm(conn_db, paramcalc, true, "5", "nzp_prm_tarif_bd", "0", out ret);
                if (!ret.result) { ExecSQL(conn_db, " Drop table tls ", false); return false; }

                if ((ret.text).Trim() != "")
                {
                    sBodyText = ret.text.Trim();
                }
            }
            else
            {
                int iTmp = 0;

                _GetValPrm(conn_db, paramcalc, true, "1", "nzp_prm_tarif_ls", "nzp_kvar", out ret);
                if (!ret.result) { ExecSQL(conn_db, " Drop table tls ", false); return false; }

                if ((ret.text).Trim() != "")
                {
                    iTmp++;
                    sBodyText = sBodyText + " во " + iTmp + "-х. Квартирный параметр: " + ret.text;
                }

                _GetValPrm(conn_db, paramcalc, true, "1", "nzp_prm_tarif_lsp", "nzp_kvar", out ret);
                if (!ret.result) { ExecSQL(conn_db, " Drop table tls ", false); return false; }

                if ((ret.text).Trim() != "")
                {
                    sBodyText = sBodyText + " + " + ret.text;
                }
                if (iTmp > 0) sBodyText = sBodyText.Trim() + "<br />";

                _GetValPrm(conn_db, paramcalc, true, "2", "nzp_prm_tarif_dm", "nzp_dom", out ret);
                if (!ret.result) { ExecSQL(conn_db, " Drop table tls ", false); return false; }

                if ((ret.text).Trim() != "")
                {
                    iTmp++;
                    sBodyText = sBodyText + " во " + iTmp + "-х. Домовой параметр: " + ret.text;
                    sBodyText = sBodyText.Trim() + "<br />";
                }

                _GetValPrm(conn_db, paramcalc, true, "11", "nzp_prm_tarif_su", "nzp_supp", out ret);
                if (!ret.result) { ExecSQL(conn_db, " Drop table tls ", false); return false; }

                if ((ret.text).Trim() != "")
                {
                    iTmp++;
                    sBodyText = sBodyText + " во " + iTmp + "-х. Параметр поставщика: " + ret.text;
                    sBodyText = sBodyText.Trim() + "<br />";
                }

                _GetValPrm(conn_db, paramcalc, true, "5", "nzp_prm_tarif_bd", "0", out ret);
                if (!ret.result) { ExecSQL(conn_db, " Drop table tls ", false); return false; }

                if ((ret.text).Trim() != "")
                {
                    iTmp++;
                    sBodyText = sBodyText + " во " + iTmp + "-х. Параметр для всех ЛС: " + ret.text;
                    sBodyText = sBodyText.Trim() + "<br />";
                }
            }
            ret.text = sBodyText.Trim();
            return true;
        }

        #endregion Формирование протокола расчета

        #region Запись корректировки входящего сальдо
        //-----------------------------------------------------------------------------
        public bool InsOutSaldoInPrevMonth(IDbConnection conn_db, ParamCalc paramcalc,int nzp_serv,int nzp_supp,decimal rsum, out Returns ret)
        //-----------------------------------------------------------------------------
        {
            ret = Utils.InitReturns();

            /*
            входящие параметры:
            1. параметры расчета - ParamCalc paramcalc = new ParamCalc(nzp_kvar, 0, pref, cur_yy, cur_mm, cur_yy, cur_mm);
               paramcalc.num_ls = <num_ls>
            2. nzp_serv - код услуги
            3. nzp_supp - код поставщика
            4. rsum - сумма входящего сальдо 
             
            выходящие параметры:
            ret.tag = 0 & ret.result = true - нормальное завершение
            ret.tag = 1 - нет prev_charge_xx
            ret.tag = 2 - неверные параметры для выполнения 
            */

            if ((paramcalc.nzp_kvar > 0) && (nzp_serv > 0) && (nzp_supp > 0))
            {

                ChargeXX chargeXX = new ChargeXX(paramcalc);

                // prev_charge_xx
                ret = ExecSQL(conn_db,
                        " Delete From " + chargeXX.prev_charge_xx +
                        " Where " + chargeXX.where_kvar + " and nzp_serv= " + nzp_serv + " and nzp_supp=" + nzp_supp
                    , true);
                if (!ret.result) { ret.tag = 1; return false; }

                if (Math.Abs(rsum) > Convert.ToDecimal(0.0000001))
                {

                    string sql =
                            " Insert into " + chargeXX.prev_charge_xx +
                            " ( nzp_charge, nzp_kvar, num_ls, nzp_serv, nzp_supp, nzp_frm, tarif, tarif_p, gsum_tarif, rsum_tarif, rsum_lgota, sum_tarif, sum_dlt_tarif, sum_dlt_tarif_p, " +
                              " sum_tarif_p,sum_lgota, sum_dlt_lgota, sum_dlt_lgota_p, sum_lgota_p, sum_nedop, sum_nedop_p, sum_real, sum_charge, reval, real_pere, sum_pere, " +
                              " real_charge, sum_money, money_to, money_from, money_del, sum_fakt, fakt_to, fakt_from, fakt_del, sum_insaldo, izm_saldo, " +
                              " sum_outsaldo, sum_subsidy, sum_subsidy_p, sum_subsidy_reval, sum_subsidy_all, isblocked, is_device, c_calc, c_sn, c_okaz, " +
                              " c_nedop, isdel, c_reval, order_print) " +
#if PG
                            " values( default, " +
#else
                            " values( 0, " +
#endif
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
        #endregion Запись корректировки входящего сальдо

        //-----------------------------------------------------------------------------
        public bool CalcChargeXX(IDbConnection conn_db, ParamCalc paramcalc, out Returns ret)
        //-----------------------------------------------------------------------------
        {
            ChargeXX chargeXX = new ChargeXX(paramcalc);

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
            CreateChargeXX(conn_db, chargeXX, false, out ret);
            if (!ret.result)
            {
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

            // ... для расчета дотаций ...
            /*
            // ... !!! временно до появления переменной в Point.* !!! ... 
            IfxCommand cmd1992 = new IfxCommand(
                " select count(*) cnt " +
                " from " + chargeXX.paramcalc.pref + "_data:prm_5 p " +
                " where p.nzp_prm=1992 and p.val_prm='1' " +
                " and p.is_actual<>100 and p.dat_s<" + chargeXX.paramcalc.dat_po + " and p.dat_po>=" + chargeXX.paramcalc.dat_s + " "
            , conn_db);
            */
            bool bIsCalcSubsidy = Points.IsCalcSubsidy; //false;
            /*
            try
            {
                string scntvals = Convert.ToString(cmd1992.ExecuteScalar());
                bIsCalcSubsidy = (Convert.ToInt32(scntvals) > 0);
            }
            catch
            {
                bIsCalcSubsidy = false;
            }
            */
            // ... !!! 1989|--- 5.0 - биллинг для Сахи|||bool||5|||| !!! ... 
            IDbCommand cmd1992 = DBManager.newDbCommand(
                " select count(*) cnt " +
                " from " + chargeXX.paramcalc.data_alias + "prm_5 p " +
                " where p.nzp_prm=1989 and p.is_actual<>100 "
            , conn_db);
            bool bIsCalcSubsidyBill = false;
            try
            {
                string scntvals = Convert.ToString(cmd1992.ExecuteScalar());
                bIsCalcSubsidyBill = (Convert.ToInt32(scntvals) > 0);
            }
            catch
            {
                bIsCalcSubsidyBill = false;
            }
            // ... Saha
            bool bIsSaha = false;
            // ... !!! 9000|--- Для САХИ ---||| <тип> || <№ prm_XX> |||| !!! ... 
            object count = ExecScalar(conn_db,
                " Select count(*) From " + chargeXX.paramcalc.data_alias + "prm_5 Where nzp_prm=9000 and is_actual<>100 ",
                out ret, true);
            if (ret.result)
            {
                try
                {
                    bIsSaha = (Convert.ToInt32(count) > 0);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = ex.Message;
                    MonitorLog.WriteLog(ret.text, MonitorLog.typelog.Error, 1, 222, true);
                    DropTempTablesRahod(conn_db, chargeXX.paramcalc.pref);
                    return false;
                }
            }


            string sql = 
                " Update " + chargeXX.charge_xx +
                " Set (nzp_frm, tarif, tarif_p, gsum_tarif, rsum_tarif, rsum_lgota, sum_tarif, sum_dlt_tarif, sum_dlt_tarif_p, sum_tarif_p, sum_lgota, " +
                     " sum_dlt_lgota, sum_dlt_lgota_p, sum_lgota_p, sum_nedop, sum_nedop_p, sum_real, sum_charge, reval, real_pere, sum_pere, " +
                     " real_charge, sum_money, money_to, money_from, money_del, sum_fakt, fakt_to, fakt_from, fakt_del, sum_insaldo, izm_saldo, " +
                     " sum_outsaldo, sum_subsidy, sum_subsidy_p, sum_subsidy_reval, sum_subsidy_all, isblocked, is_device, c_calc, c_sn, c_okaz, " +
                     " c_nedop, isdel, c_reval, order_print" +
                     ",sum_tarif_f,sum_tarif_f_p, sum_tarif_sn_eot,sum_tarif_sn_eot_p,sum_tarif_sn_f,sum_tarif_sn_f_p,tarif_f,tarif_f_p" +
                ") = " +
                " ( 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0" +
                ", 0, 0, 0, 0, 0, 0, 0, 0 ) " +
                " Where " + chargeXX.where_kvar + //nzp_kvar in ( Select nzp_kvar From t_selkvar ) " + 
                chargeXX.paramcalc.per_dat_charge;

            if (chargeXX.paramcalc.nzp_kvar > 0 || chargeXX.paramcalc.nzp_dom > 0)
            {
                ret = ExecSQL(conn_db, sql, true);
            }
            else
            {
                ExecByStep(conn_db, chargeXX.charge_xx, "nzp_charge", sql, 50000, " ", out ret);
            }
            
            if (!ret.result)
            {
                DropTempTablesCharge(conn_db);
                return false;
            }

            sql = 
                " Delete From " + chargeXX.charge_xx +
                " Where " + chargeXX.where_kvar + //nzp_kvar in ( Select nzp_kvar From t_selkvar ) " +
                "   and nzp_serv = 1 " +
                    chargeXX.paramcalc.per_dat_charge;

            if (chargeXX.paramcalc.nzp_kvar > 0 || chargeXX.paramcalc.nzp_dom > 0)
            {
                ret = ExecSQL(conn_db, sql, true);
            }
            else
            {
                ExecByStep(conn_db, chargeXX.charge_xx, "nzp_charge", sql, 50000, " ", out ret);
            }

            if (!ret.result)
            {
                DropTempTablesCharge(conn_db);
                return false;
            }

            UpdateStatistics(false, paramcalc, chargeXX.charge_tab, out ret);
            if (!ret.result)
            {
                DropTempTablesCharge(conn_db);
                return false;
            }

            //string tab = "";

            //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
            //при перерасчете исключить некоторые сальдовые таблицы (prekedka, del_supplier, etc)
            //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

            // недопоставки выберем отдельно - они потом понадобятся для расчета суммы недопоставки
            ExecSQL(conn_db, " Drop table t_nedo_xx ", false);
            ret = ExecSQL(conn_db,
                " Select k.nzp_kvar,k.num_ls,k.nzp_dom,k.nzp_area,k.nzp_geu," +
                " a.dat_charge,a.cur_zap,a.nzp_serv,a.nzp_kind,a.koef,a.tn,a.tn_min,a.tn_max,a.dat_s,a.dat_po,a.cnts,a.cnts_del,a.perc,a.kod_info" +
#if PG
                " Into temp t_nedo_xx " +
#else
                " " +
#endif
                " From " + chargeXX.calc_nedo_xx + " a, t_selkvar k" +
                " Where a.nzp_kvar=k.nzp_kvar and a.cur_zap <> -1 " +
#if PG
                " "
#else
                " Into temp t_nedo_xx With no log "
#endif
                , true);
            if (!ret.result)
            {
                DropTempTablesCharge(conn_db);
                return false;
            }

            ExecSQL(conn_db, " Create index ix1_t_nedo_xx on t_nedo_xx (nzp_kvar,nzp_serv) ", true);
            ExecSQL(conn_db, sUpdStat + " t_nedo_xx ", true);

            if (!chargeXX.paramcalc.b_reval)
            {
                sql = 
                    " Delete From " + chargeXX.del_supplier +
                    " Where num_ls in ( Select num_ls From t_selkvar ) " +
                    "   and kod_sum = 39 " +
                    "   and dat_account = " + sPublicForMDY + "MDY (" +
                    chargeXX.paramcalc.calc_mm + ",28," + chargeXX.paramcalc.calc_yy + ") ";

                if (chargeXX.paramcalc.nzp_kvar > 0 || chargeXX.paramcalc.nzp_dom > 0)
                {
                    ret = ExecSQL(conn_db, sql, true);
                }
                else
                {
                    ExecByStep(conn_db, chargeXX.del_supplier, "nzp_del", sql, 50000, " ", out ret);
                }
                if (!ret.result)
                {
                    DropTempTablesCharge(conn_db);
                    return false;
                }
                UpdateStatistics(false, paramcalc, "del_supplier", out ret);
                if (!ret.result)
                {
                    ret.text = "Ошибка выборки данных ";
                    DropTempTablesCharge(conn_db);
                    return false;
                }

                //---------------------------------------------------
                //добавить строки nzp_serv,nzp_supp из всех целевых таблиц, участвующих в сальдо
                //---------------------------------------------------
                //reval_xx
                ret = ExecSQL(conn_db,
#if PG
                    " Select distinct k.nzp_kvar,k.num_ls,nzp_supp,nzp_serv, " +
                    " sum(case when kod_info<>1 then reval else 0 end) as reval " +
                    " Into temp t_lnk_reval "+
                    " From " + chargeXX.reval_xx + " a, t_selkvar k " +
                    " Where a.nzp_kvar = k.nzp_kvar " +
                    "   and abs(reval) > 0.0001 " +
                    "   and nzp_serv <> 1 " +
                    " Group by 1,2,3,4 "
                    
#else
                    " Select unique k.nzp_kvar,k.num_ls,nzp_supp,nzp_serv, " +
                    " sum(case when kod_info<>1 then reval else 0 end) as reval " +
                    " From " + chargeXX.reval_xx + " a, t_selkvar k " +
                    " Where a.nzp_kvar = k.nzp_kvar " +
                    "   and abs(reval) > 0.0001 " +
                    "   and nzp_serv <> 1 " +
                    " Group by 1,2,3,4 " +
                    " Into temp t_lnk_reval With no log "
#endif
                    , true);
                if (!ret.result)
                {
                    DropTempTablesCharge(conn_db);
                    return false;
                }
                ExecSQL(conn_db, " Create unique index ix_t_lnk_reval on t_lnk_reval (nzp_kvar,nzp_serv,nzp_supp) ", true);
                ExecSQL(conn_db,sUpdStat + " t_lnk_reval ", true);

                //fn_supplier
                string sfind_tmp =
                    " Select num_ls,nzp_supp,nzp_serv,sum_prih " +
#if PG
                    " Into temp t_fn_suppall  "+
#else
                    "  "+
#endif
                    " From " + chargeXX.fn_supplier + 
                    " Where abs(sum_prih) > 0.0001 and nzp_serv <> 1 " +
#if PG
                    " ";
#else
                    " Into temp t_fn_suppall With no log ";
#endif

                // если расчет по кол-ву л/с меньше 100, то соединить с t_selkvar для min времени выборки
                if (bIsL100)
                {
                    sfind_tmp =
                    " Select a.num_ls,a.nzp_supp,a.nzp_serv,a.sum_prih " +
#if PG
                    " Into temp t_fn_suppall  "+
#else
                    "  "+
#endif
                    " From " + chargeXX.fn_supplier + " a, t_selkvar k" +
                    " Where a.num_ls = k.num_ls " +
                    " and abs(a.sum_prih) > 0.0001 and a.nzp_serv <> 1 " +
#if PG
                    "  ";
#else
                    " Into temp t_fn_suppall With no log ";
#endif
                }

                ret = ExecSQL(conn_db, sfind_tmp, true);
                if (!ret.result)
                {
                    DropTempTablesCharge(conn_db);
                    return false;
                }
                ExecSQL(conn_db, " Create index ix_t_fn_suppall on t_fn_suppall (num_ls) ", true);
                ExecSQL(conn_db,sUpdStat + " t_fn_suppall ", true);
                //tab = chargeXX.paramcalc.pref + "_charge_" + (chargeXX.paramcalc.calc_yy - 2000).ToString("00") + ":fn_supplier" + chargeXX.paramcalc.calc_mm.ToString("00");
                ret = ExecSQL(conn_db,
                    " Select k.nzp_kvar,k.num_ls,nzp_supp,nzp_serv,sum(sum_prih) as money_to " +
#if PG
                    " Into temp t_fn_supp "+
#else
                    " " +        
#endif
                    " From t_fn_suppall a, t_selkvar k " +
                    " Where a.num_ls = k.num_ls " +
                    " Group by 1,2,3,4 " +
#if PG
                    " "
#else
                    " Into temp t_fn_supp With no log "
#endif
                    , true);
                if (!ret.result)
                {
                    DropTempTablesCharge(conn_db);
                    return false;
                }
                ExecSQL(conn_db, " Create unique index ix_t_fn_supp on t_fn_supp (nzp_kvar,nzp_serv,nzp_supp) ", true);
                ExecSQL(conn_db,sUpdStat + " t_fn_supp ", true);

                //from_supplier
                //tab = chargeXX.paramcalc.pref + "_charge_" + (chargeXX.paramcalc.calc_yy - 2000).ToString("00") + ":from_supplier";
                ret = ExecSQL(conn_db,
                    " Select k.nzp_kvar,k.num_ls,nzp_supp,nzp_serv,sum(sum_prih) as money_from " +
#if PG
                   " Into temp t_from_supp  "+
#else
                   " "+
#endif
                    " From " + chargeXX.from_supplier + " a, t_selkvar k " +
                    " Where a.num_ls = k.num_ls " +
                    "   and dat_uchet >= " + chargeXX.paramcalc.dat_s +
                    "   and dat_uchet <= " + chargeXX.paramcalc.dat_po +
                    "   and abs(sum_prih) > 0.0001 " +
                    "   and nzp_serv <> 1 " +
                    " Group by 1,2,3,4 " +                    
#if PG
                   " "
#else
                   " Into temp t_from_supp With no log "
#endif
                    , true);
                if (!ret.result)
                {
                    DropTempTablesCharge(conn_db);
                    return false;
                }
                ExecSQL(conn_db, " Create unique index ix_t_from_supp on t_from_supp (nzp_kvar,nzp_serv,nzp_supp) ", true);
                ExecSQL(conn_db,sUpdStat + " t_from_supp ", true);

                //del_supplier
                //tab = chargeXX.paramcalc.pref + "_charge_" + (chargeXX.paramcalc.calc_yy - 2000).ToString("00") + ":del_supplier";
                ret = ExecSQL(conn_db,
                    " Select k.nzp_kvar,k.num_ls,nzp_supp,nzp_serv,sum(sum_prih) as money_del " +
#if PG
                    " Into temp t_del_supp " +
#else
                    "  " +
#endif
                    " From " + chargeXX.del_supplier + " a, t_selkvar k " +
                    " Where a.num_ls = k.num_ls " +
                    "   and dat_account >= " + chargeXX.paramcalc.dat_s +
                    "   and dat_account <= " + chargeXX.paramcalc.dat_po +
                    "   and abs(sum_prih) > 0.0001 " +
                    "   and nzp_serv <> 1 " +
                    " Group by 1,2,3,4 " +
#if PG
                    "  "
#else
                    " Into temp t_del_supp With no log "
#endif
                    , true);
                if (!ret.result)
                {
                    DropTempTablesCharge(conn_db);
                    return false;
                }
                ExecSQL(conn_db, " Create unique index ix_t_del_supp on t_del_supp (nzp_kvar,nzp_serv,nzp_supp) ", true);
                ExecSQL(conn_db,sUpdStat + " t_del_supp ", true);

                //prev_charge
                ret = ExecSQL(conn_db,
                    " Create temp table t_prev_ch " +
                    " ( nzp_kvar  integer, " +
                    "   num_ls    integer, " +
                    "   nzp_serv  integer, " +
                    "   nzp_supp  integer, " +
                    "   sum_outsaldo " + sDecimalType + "(14,2) " +
#if PG
                    " )  "
#else
                    " ) With no log "
#endif
                    , true);
                if (!ret.result)
                {
                    DropTempTablesCharge(conn_db);
                    return false;
                }

                if (chargeXX.paramcalc.nzp_kvar > 0 || chargeXX.paramcalc.nzp_dom > 0)
                {
                    ret = ExecSQL(conn_db,
                        " Insert into t_prev_ch ( nzp_kvar,num_ls,nzp_supp,nzp_serv,sum_outsaldo ) " +
                        " Select " + sUniqueWord +
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
                        " Select " + sUniqueWord +
                        " k.nzp_kvar,k.num_ls,nzp_supp,nzp_serv,sum(sum_outsaldo) as sum_outsaldo " +
                        " From " + chargeXX.prev_charge_xx + " a, t_selkvar k " +
                        " Where a.nzp_kvar = k.nzp_kvar " +
                        "   and abs(sum_outsaldo) > 0.0001 " +
                        "   and nzp_serv <> 1 "
                        , 20000, " Group by 1,2,3,4 ", out ret);
                }

                if (!ret.result)
                {
                    DropTempTablesCharge(conn_db);
                    return false;
                }
                ExecSQL(conn_db, " Create unique index ix_t_prev_ch on t_prev_ch (nzp_kvar,nzp_serv,nzp_supp) ", true);
                ExecSQL(conn_db, sUpdStat + " t_prev_ch ", true);

                //выбрать перекидки
                //tab = chargeXX.paramcalc.pref + "_charge_" + (chargeXX.paramcalc.calc_yy - 2000).ToString("00") + ":perekidka";
                ret = ExecSQL(conn_db,
                    " Select " + sUniqueWord + " k.nzp_kvar,k.num_ls,nzp_supp,nzp_serv,sum(sum_rcl) as real_charge " +
#if PG
                    " Into temp t_perek "+
#else
                    "  "+
#endif
                    " From " + chargeXX.perekidka + " a, t_selkvar k " +
                    " Where a.nzp_kvar = k.nzp_kvar " +
                    "   and month_ = " + chargeXX.paramcalc.calc_mm +
                    "   and abs(sum_rcl) > 0.0001 " +
                    "   and nzp_serv <> 1 " +
                    "   and nzp_supp > 0 " +
                    " Group by 1,2,3,4 " +
#if PG
                    "  "
#else
                    " Into temp t_perek With no log "
#endif
                    , true);
                if (!ret.result)
                {
                    DropTempTablesCharge(conn_db);
                    return false;
                }
                ExecSQL(conn_db, " Create unique index ix_t_perek on t_perek (nzp_kvar,nzp_supp,nzp_serv) ", true);
                ExecSQL(conn_db, sUpdStat + " t_perek ", true);
            }


            //выбрать nedo_xx
            //tab = chargeXX.paramcalc.pref + "_charge_" + (chargeXX.paramcalc.calc_yy - 2000).ToString("00") + ":nedo_" + (chargeXX.paramcalc.calc_mm).ToString("00");
            ret = ExecSQL(conn_db,
                " Select nzp_kvar,num_ls,nzp_serv, max(koef) as koef " +
#if PG
                " Into temp t_nedo  " +
#else
                " " +
#endif
                " From t_nedo_xx " +
                " Group by 1,2,3 " +
#if PG
                " "
#else
                " Into temp t_nedo With no log "
#endif
                , true, 6000);
            if (!ret.result)
            {
                DropTempTablesCharge(conn_db);
                return false;
            }
            ExecSQL(conn_db, " Create unique index ix_t_nedo on t_nedo (nzp_kvar,nzp_serv) ", true);
            ExecSQL(conn_db,sUpdStat + " t_nedo ", true);

            //выбрать calc_gku
            ret = ExecSQL(conn_db,
                " CREATE TEMP TABLE t_calc_gku( " +
                " nzp_kvar INTEGER, " +
                " num_ls   INTEGER, " +
                " nzp_serv INTEGER, " +
                " nzp_supp INTEGER, " +
                " nzp_frm  INTEGER default 0, " +
                " nzp_frm_typ      INTEGER default 0, " +
                " is_device        INTEGER default 0, " +
                " koef_gkal        " + sDecimalType + "(14,8) default 1," +
                " tarif            " + sDecimalType + "(17,7) default 0.00, " +
                " gsum_tarif       " + sDecimalType + "(14,2) default 0.00, " +
                " rsum_tarif       " + sDecimalType + "(14,2) default 0.00, " +
                " rsum_lgota       " + sDecimalType + "(14,2) default 0.00, " +
                " sum_tarif        " + sDecimalType + "(14,2) default 0.00, " +
                " c_calc           " + sDecimalType + "(14,4) default 0.00, " +
                " c_sn             " + sDecimalType + "(14,4) default 0.00, " +
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
                " real_pere        " + sDecimalType + "(14,2) default 0.00  " +
#if PG
                " )  "
#else
                " ) with no log "
#endif
                , true, 6000);
            if (!ret.result)
            {
                DropTempTablesCharge(conn_db);
                return false;
            }

            sql =
                " Insert Into t_calc_gku(nzp_kvar,num_ls,nzp_serv,nzp_supp,nzp_frm,nzp_frm_typ,koef_gkal,tarif,gsum_tarif,rsum_tarif,sum_tarif, " +
                " c_calc,c_sn,sum_tarif_sn_eot,tarif_f,sum_tarif_f,sum_tarif_sn_f,sum_subsidy_all,is_device,real_pere) " +
                " Select k.nzp_kvar,k.num_ls,nzp_serv,nzp_supp,nzp_frm,max(nzp_frm_typ),max(trf3), " +
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

            sql = sql + " ,max(is_device) is_device,sum(tarif * rsh3) as real_pere " +
                " From " + chargeXX.calc_gku_xx + " a, t_selkvar k " +
                " Where a.nzp_kvar = k.nzp_kvar " + chargeXX.paramcalc.per_dat_charge +
                "   and nzp_serv <> 1 " +
                " Group by 1,2,3,4,5 ";
            
            ret = ExecSQL(conn_db, sql, true, 6000);
            if (!ret.result)
            {
                DropTempTablesCharge(conn_db);
                return false;
            }
            ExecSQL(conn_db, " Create unique index ix_t_calc_gku on t_calc_gku (nzp_kvar,nzp_serv,nzp_supp,nzp_frm) ", true);
            ExecSQL(conn_db, sUpdStat + " t_calc_gku ", true);

            if (bIsCalcSubsidy)
            {
                if (bIsSaha && (!bIsCalcSubsidyBill) )
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
            //выбрать отсутствующие свзяки nzp_supp,nzp_serv
            //---------------------------------------------------
            ret = ExecSQL(conn_db,
                " Create temp table t_ins_ch " +
                " ( nzp_key   serial not null, " +
                "   nzp_kvar  integer, " +
                "   num_ls    integer, " +
                "   nzp_serv  integer, " +
                "   nzp_supp  integer, " +
                "   kod       integer  " +
#if PG
                " ) "
#else
                " ) With no log "
#endif
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
#if PG
                " ) "
#else
                " ) With no log "
#endif
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
                " Select " + sUniqueWord + " nzp_kvar,num_ls,nzp_supp,nzp_serv, 0 " +
                " From t_ins_ch1 "
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
                " Select a.nzp_kvar,a.nzp_serv,a.nzp_supp, max(1) as cnt " +
#if PG
                " Into temp t_ins_ch1 " +
#else
                "  " +
#endif
                " From " + chargeXX.charge_xx + " a, t_selkvar k" +
                " Where a.nzp_kvar=k.nzp_kvar " +
                    chargeXX.paramcalc.per_dat_charge +
                " Group by 1,2,3 " +
#if PG
                "  "
#else
                " Into temp t_ins_ch1 With no log "
#endif
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



            //вставка недостающих строк в charge_xx
            string p_dat_charge = DateNullString;
            if (!chargeXX.paramcalc.b_cur)
                p_dat_charge = MDY(chargeXX.paramcalc.cur_mm, 28, chargeXX.paramcalc.cur_yy);

#if PG
            string serial_fld = "";
            string serial_val = "";
#else
            string serial_fld = "nzp_charge,";
            string serial_val = "0,";
#endif
            sql =
                " Insert into " + chargeXX.charge_xx +
                   " ( " + serial_fld + "dat_charge, nzp_kvar, num_ls, nzp_serv, nzp_supp, nzp_frm, tarif, tarif_p, gsum_tarif, rsum_tarif, rsum_lgota, sum_tarif, sum_dlt_tarif, sum_dlt_tarif_p, " +
                     " sum_tarif_p,sum_lgota, sum_dlt_lgota, sum_dlt_lgota_p, sum_lgota_p, sum_nedop, sum_nedop_p, sum_real, sum_charge, reval, real_pere, sum_pere, " +
                     " real_charge, sum_money, money_to, money_from, money_del, sum_fakt, fakt_to, fakt_from, fakt_del, sum_insaldo, izm_saldo, " +
                     " sum_outsaldo, sum_subsidy, sum_subsidy_p, sum_subsidy_reval, sum_subsidy_all, isblocked, is_device, c_calc, c_sn, c_okaz, " +
                     " c_nedop, isdel, c_reval, order_print) " +
                " Select " + serial_val + p_dat_charge + ", nzp_kvar,num_ls, nzp_serv, nzp_supp, " +
                      "  0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 " +
                " From t_ins_ch Where kod = 0 ";
            if (chargeXX.paramcalc.nzp_kvar > 0 || chargeXX.paramcalc.nzp_dom > 0)
            {
                ret = ExecSQL(conn_db, sql, true);
            }
            else
            {
                ExecByStep(conn_db, "t_ins_ch", "nzp_key", sql, 50000, " ", out ret);
            }

            if (!ret.result)
            {
                DropTempTablesCharge(conn_db);
                return false;
            }
            UpdateStatistics(false, paramcalc, chargeXX.charge_tab, out ret);
            if (!ret.result)
            {
                DropTempTablesCharge(conn_db);
                return false;
            }

            //---------------------------------------------------
            //заполнить рассчитанные начислено, формулы и т.п...
            //---------------------------------------------------
            string sqql =
                    " Update " + chargeXX.charge_xx +
                    " Set " +
                        "  (tarif,gsum_tarif,rsum_tarif,sum_tarif,nzp_frm,c_calc,c_okaz,tarif_f,sum_tarif_f,sum_tarif_sn_eot,sum_tarif_sn_f,c_sn," +
                          " sum_subsidy_all,sum_subsidy,is_device,real_pere) = " +
#if PG
                        "  (gk.tarif,gk.sum_tarif,gk.sum_tarif,gk.sum_tarif,gk.nzp_frm,gk.c_calc,gk.c_okaz,gk.tarif_f,gk.sum_tarif_f,gk.sum_tarif_sn_eot," +
                           "gk.sum_tarif_sn_f,gk.c_sn,gk.sum_subsidy_all,gk.sum_subsidy,gk.is_device,gk.real_pere) " +
                        " From " +
                        " (Select max(tarif) tarif, sum(sum_tarif) sum_tarif, max(nzp_frm) nzp_frm, sum(c_calc) c_calc, " +
                        "  max( case when sum_tarif > 0 then " + DateTime.DaysInMonth(paramcalc.calc_yy, paramcalc.calc_mm).ToString() + " else 0 end ) c_okaz, " +
                        "  max(tarif_f) tarif_f, sum(sum_tarif_f) sum_tarif_f, sum(sum_tarif_sn_eot) sum_tarif_sn_eot, sum(sum_tarif_sn_f) sum_tarif_sn_f," +
                        "  sum(c_sn) c_sn, sum(sum_subsidy_all) sum_subsidy_all, sum(sum_subsidy) sum_subsidy, max(is_device) is_device,sum(real_pere) real_pere " +
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
                        "  sum(c_sn), sum(sum_subsidy_all), sum(sum_subsidy), max(is_device), sum(real_pere) " +
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
                        + " and " + chargeXX.where_kvar;   //nzp_kvar in ( Select nzp_kvar From t_selkvar ) ";

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
                        + " and " + chargeXX.where_kvar;   //nzp_kvar in ( Select nzp_kvar From t_selkvar ) ";

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
                        + " and " + chargeXX.where_kvar;   //nzp_kvar in ( Select nzp_kvar From t_selkvar ) ";

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
                        + " and " + chargeXX.where_kvar;   //nzp_kvar in ( Select nzp_kvar From t_selkvar ) ";

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
                        + " and " + chargeXX.where_kvar;   //nzp_kvar in ( Select nzp_kvar From t_selkvar ) ";

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
                        + " and " + chargeXX.where_kvar;   //nzp_kvar in ( Select nzp_kvar From t_selkvar ) ";

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
                   " Where 0 < ( Select count(*) From t_nedo_xx gk " +
                               " Where gk.nzp_kvar = " + chargeXX.charge_xx + ".nzp_kvar " +
                                   uni_serv +
                               " ) " + chargeXX.paramcalc.per_dat_charge +
                     " and " + chargeXX.where_kvar;   //nzp_kvar in ( Select nzp_kvar From t_selkvar ) ";

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
                        " case when " + chargeXX.charge_xx + ".nzp_serv = 7 and gk.nzp_kind = 1" +
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
                       " Where 0 < ( Select count(*) From t_nedo_xx gk " +
                                   " Where gk.nzp_kvar = " + chargeXX.charge_xx + ".nzp_kvar " +
                                   uni_serv +
                                   " ) " + chargeXX.paramcalc.per_dat_charge +
                         " and nzp_serv=7 " +
                         " and " + chargeXX.where_kvar;   //nzp_kvar in ( Select nzp_kvar From t_selkvar ) ";

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
                        " case when " + chargeXX.charge_xx + ".nzp_serv in (6,7) and gk.nzp_kind in (8,46, 2005) then 9 else " +
                            " case when " + chargeXX.charge_xx + ".nzp_serv in (6,7) and gk.nzp_kind = 75 and " + chargeXX.charge_xx + ".is_device = 0" +
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
                       " Where 0 < ( Select count(*) From t_nedo_xx gk " +
                                   " Where gk.nzp_kvar = " + chargeXX.charge_xx + ".nzp_kvar " +
                                   uni_serv +
                                   " ) " + chargeXX.paramcalc.per_dat_charge +
                         " and nzp_serv=7 " +
                         " and " + chargeXX.where_kvar;   //nzp_kvar in ( Select nzp_kvar From t_selkvar ) ";

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
                   "   and sum_nedop > 0 "
                    + chargeXX.paramcalc.per_dat_charge;

            if (chargeXX.paramcalc.nzp_kvar > 0 || chargeXX.paramcalc.nzp_dom > 0)
                ret = ExecSQL(conn_db, sqql, true);
            else
                ExecByStep(conn_db, chargeXX.charge_xx, "nzp_charge",
                    sqql, 50000, " ", out ret);

            sqql = " Update " + chargeXX.charge_xx +
                   " Set sum_tarif = rsum_tarif - sum_nedop" +
                      ", sum_tarif_f      = sum_tarif_f      * ((rsum_tarif - sum_nedop)/rsum_tarif)" +
                      ", sum_tarif_sn_eot = sum_tarif_sn_eot * ((rsum_tarif - sum_nedop)/rsum_tarif)" +
                      ", sum_tarif_sn_f   = sum_tarif_sn_f   * ((rsum_tarif - sum_nedop)/rsum_tarif) " +
                      ", sum_subsidy      = sum_subsidy      * ((rsum_tarif - sum_nedop)/rsum_tarif) " +
                      ",c_okaz = ( case when c_okaz - c_nedop > 0 then c_okaz - c_nedop else 0 end ) " +
                   " Where " + chargeXX.where_kvar + //nzp_kvar in ( Select nzp_kvar From t_selkvar ) " +
                   "  and sum_nedop > 0 "
                   + chargeXX.paramcalc.per_dat_charge;

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


            //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
            //при перерасчете исключить некоторые сальдовые таблицы (prekedka, del_supplier, etc)
            //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
            if (!chargeXX.paramcalc.b_reval)
            {
                //---------------------------------------------------
                //расчет сальдо
                //---------------------------------------------------
                sqql = " Update " + chargeXX.charge_xx +
                       " Set sum_money = money_to + money_from + money_del " +
                          " ,sum_outsaldo = sum_insaldo +  real_charge + sum_real + reval + izm_saldo - (money_to + money_from + money_del) " +
                    //" ,real_pere = sum_insaldo + izm_saldo + real_charge - (money_to + money_from + money_del) " +
                          " ,sum_pere  = sum_insaldo + reval + real_charge - (money_to + money_from + money_del) " +
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
                if (!ret.result)
                {
                    DropTempTablesCharge(conn_db);
                    return false;
                }
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
                        //" Create table are.t_perek " +
                        " Create temp table t_perek " +
                        " ( nzp_key    serial not null, " +
                        "   nzp_kvar   integer, " +
                        "   num_ls     integer, " +
                        "   nzp_serv   integer, " +
                        "   nzp_supp   integer, " +
                        "   nzp_charge integer, " +
                        "   sum_out    " + sDecimalType + "(14,2) default 0.00, " +
                        "   sum_del    " + sDecimalType + "(14,2) default 0.00 " +
#if PG
                        " )  "
#else
                        " ) With no log "
#endif
                        //" ) "
                        , true);
                    if (!ret.result)
                    {
                        DropTempTablesCharge(conn_db);
                        return false;
                    }
                    ret = ExecSQL(conn_db,
                        " Insert into t_perek (nzp_kvar,num_ls,nzp_supp,nzp_serv,nzp_charge, sum_del,sum_out) " +
                        " Select a.nzp_kvar,a.num_ls,a.nzp_supp,a.nzp_serv,a.nzp_charge, 0 sum_del, a.sum_outsaldo " +
                        " From " + chargeXX.charge_xx + " a, t_selkvar k " +
                        " Where a.nzp_kvar=k.nzp_kvar "
                         + chargeXX.paramcalc.per_dat_charge +
                        "   and a.sum_outsaldo < 0 "
                        , true);
                    if (!ret.result)
                    {
                        DropTempTablesCharge(conn_db);
                        return false;
                    }

                    ExecSQL(conn_db, " Create unique index ix_t_perek1 on t_perek (nzp_key) ", true);
                    ExecSQL(conn_db, " Create unique index ix_t_perek2 on t_perek (nzp_charge) ", true);
                    ExecSQL(conn_db, " Create unique index ix_t_perek3 on t_perek (nzp_kvar,nzp_serv,nzp_supp) ", true);
                    ExecSQL(conn_db, sUpdStat + " t_perek ", true);

                    //загоним положительные суммы по услуге
                    ExecSQL(conn_db, " Drop table t_ins_ch1 ", false);

                    ret = ExecSQL(conn_db,
                        " Select ch.nzp_kvar,ch.num_ls,ch.nzp_supp,ch.nzp_serv,ch.nzp_charge, 0 sum_del, ch.sum_outsaldo " +
#if PG
                        " Into temp t_ins_ch1 "+
#else
                        "  "+
#endif
                        " From " + chargeXX.charge_xx + " ch, t_selkvar k " +
                        " Where ch.nzp_kvar=k.nzp_kvar "
                         + chargeXX.paramcalc.per_dat_charge +
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
                    if (!ret.result)
                    {
                        DropTempTablesCharge(conn_db);
                        return false;
                    }
                    ret = ExecSQL(conn_db,
                        " Insert into t_perek (nzp_kvar,num_ls,nzp_supp,nzp_serv,nzp_charge, sum_del,sum_out) " +
                        " Select nzp_kvar,num_ls,nzp_supp,nzp_serv,nzp_charge, sum_del, sum_outsaldo " +
                        " From t_ins_ch1 "
                        , true);
                    if (!ret.result)
                    {
                        DropTempTablesCharge(conn_db);
                        return false;
                    }
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
                        " Into temp t_delmin "+
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
                        if (!ret.result)
                        {
                            DropTempTablesCharge(conn_db);
                            return false;
                        }
                        ExecSQL(conn_db, " Create unique index ix_t_delmin on t_delmin (nzp_kvar,nzp_serv) ", true);
                        ExecSQL(conn_db, sUpdStat + " t_delmin ", true);

                        //ищем максимальный плюс
                        ret = ExecSQL(conn_db,
                        " Select nzp_kvar, nzp_serv, max(nzp_charge) as nzp_charge, max(sum_out+sum_del) as sum_del " +
#if PG
                        " Into temp t_delplu "+
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
                        if (!ret.result)
                        {
                            DropTempTablesCharge(conn_db);
                            return false;
                        }
                        ExecSQL(conn_db, " Create unique index ix_t_delplu on t_delplu (nzp_kvar,nzp_serv) ", true);
                        ExecSQL(conn_db, sUpdStat + " t_delplu ", true);


                        //соединяем между собой эти таблицы и вычисляем дельты в пределах общих абсолютных значениях

                        ret = ExecSQL(conn_db,
                        " Select a.nzp_charge as nzp_min, b.nzp_charge as nzp_plu, " +
                           " case when a.sum_del + b.sum_del > 0 then abs(a.sum_del) else b.sum_del end as sum_dlt " +
#if PG
                        " Into temp t_ins_ch1 "+
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
                        if (!ret.result)
                        {
                            DropTempTablesCharge(conn_db);
                            return false;
                        }
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
                            if (!ret.result)
                            {
                                DropTempTablesCharge(conn_db);
                                return false;
                            }
                            ret = ExecSQL(conn_db,
                            " Update t_perek " +
                            " Set sum_del = sum_del - ( Select sum_dlt From t_ins_ch1 Where t_perek.nzp_charge = t_ins_ch1.nzp_plu ) " +
                            " Where nzp_charge in ( Select nzp_plu From t_ins_ch1 ) "
                            , true);
                            if (!ret.result)
                            {
                                DropTempTablesCharge(conn_db);
                                return false;
                            }

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
                            " Where nzp_charge in ( Select nzp_charge From t_perek ) "
                            , true);
                        if (!ret.result)
                        {
                            DropTempTablesCharge(conn_db);
                            return false;
                        }
                        ret = ExecSQL(conn_db,
                            " Update " + chargeXX.charge_xx +
                            " Set sum_money = money_to + money_from + money_del " +
                               " ,sum_outsaldo = sum_insaldo +  real_charge + sum_real + reval + izm_saldo - (money_to + money_from + money_del) " +
                            " Where nzp_charge in ( Select nzp_charge From t_perek ) "
                            , true);
                        if (!ret.result)
                        {
                            DropTempTablesCharge(conn_db);
                            return false;
                        }

                        //загнать следы в del_supplier
                        ret = ExecSQL(conn_db,
                            " Insert into " + chargeXX.paramcalc.pref + "_charge_" + (chargeXX.paramcalc.calc_yy - 2000).ToString("00") + tableDelimiter + "del_supplier" +
                              " (num_ls, nzp_serv, nzp_supp, sum_prih, kod_sum, dat_account, dat_calc) " +
#if PG
                            " Select num_ls, nzp_serv, nzp_supp, sum_del, 39, " + MDY(chargeXX.paramcalc.calc_mm, 28, chargeXX.paramcalc.calc_yy) + ", current_date " +
#else
                            " Select num_ls, nzp_serv, nzp_supp, sum_del, 39, MDY(" + chargeXX.paramcalc.calc_mm + ",28," + chargeXX.paramcalc.calc_yy + "), today " +
#endif
                            " From t_perek " +
                            " Where abs(sum_del) > 0.000001 "
                            , true);
                        if (!ret.result)
                        {
                            DropTempTablesCharge(conn_db);
                            return false;
                        }
                    }



                }
                // ... end anes - отмена перераспределения оплат

                //---------------------------------------------------
                //расчет итого
                //---------------------------------------------------

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

                //---------------------------------------------------
                //рассрочка, аааааааааааааа!
                //---------------------------------------------------
                
                CalcKreditXX(conn_db, paramcalc, out ret);
                if (!ret.result)
                {
                    DropTempTablesCharge(conn_db);
                    return false;
                }
                

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
#if PG
                    " )  "
#else
                    " ) With no log "
#endif
                    , true);

                if (!ret.result)
                {
                    DropTempTablesCharge(conn_db);
                    return false;
                }

#if PG
                serial_fld = "";
                serial_val = "";
#else
                serial_fld = ",nzp_key";
                serial_val = ",0";
#endif
                //ret = ExecSQL(conn_db,
                sqql =
                    " Insert into t_itog " +
                      "(nzp_kvar,num_ls,sum_nedop,sum_tarif,gsum_tarif,rsum_tarif,izm_saldo,real_pere,sum_pere,sum_money,money_to," +
                       " money_from,money_del,sum_outsaldo,sum_insaldo,real_charge,sum_real,reval,sum_charge,rsum_lgota,sum_dlt_tarif,sum_dlt_tarif_p," +
                       " sum_tarif_p,sum_lgota,sum_dlt_lgota,sum_dlt_lgota_p,sum_lgota_p,sum_nedop_p,sum_fakt,fakt_to,fakt_from,fakt_del,sum_subsidy," +
                       " sum_subsidy_p,sum_subsidy_reval,sum_subsidy_all,c_calc,c_sn,sum_tarif_f,sum_tarif_f_p,sum_tarif_sn_eot,sum_tarif_sn_eot_p," +
                       " sum_tarif_sn_f,sum_tarif_sn_f_p" + serial_fld + ")" +
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
                       serial_val +
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
                    //ExecByStep(conn_db, chargeXX.charge_xx + " k ", "k.nzp_kvar",
                    ExecByStep(conn_db, "t_selkvar k", "k.nzp_kvar",
                        sqql, 50000, " Group by 1,2 ", out ret);
                if (!ret.result)
                {
                    DropTempTablesCharge(conn_db);
                    return false;
                }
                ExecSQL(conn_db, " Create unique index ix1_t_itog on t_itog (nzp_key) ", true);
                ExecSQL(conn_db, " Create unique index ix2_t_itog on t_itog (nzp_kvar) ", true);
                ExecSQL(conn_db, sUpdStat + " t_itog ", true);


#if PG
                serial_fld = "";
                serial_val = "";
#else
                serial_fld = ",nzp_charge";
                serial_val = ",0";
#endif
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
                     serial_fld + ") " +
                " Select " + p_dat_charge + ", nzp_kvar,num_ls, 1 nzp_serv, 0 nzp_supp, 0, 0, 0,  " +
                     " rsum_lgota, sum_dlt_tarif, sum_dlt_tarif_p," +
                     " sum_tarif_p, sum_lgota, sum_dlt_lgota, sum_dlt_lgota_p, sum_lgota_p, sum_nedop_p," +
                     " sum_fakt, fakt_to, fakt_from, fakt_del, sum_subsidy, sum_subsidy_p, sum_subsidy_reval, sum_subsidy_all," +
                     " 0, 0, c_calc, c_sn, 0, 0, 0, 0, 0, " +

                     " sum_nedop, gsum_tarif, rsum_tarif, sum_tarif, izm_saldo, real_pere, sum_pere, " +
                     " sum_insaldo, sum_outsaldo, sum_real,  sum_charge, reval, real_charge, sum_money, money_to, money_from, money_del, " +
                     " sum_tarif_f, sum_tarif_f_p, sum_tarif_sn_eot, sum_tarif_sn_eot_p, sum_tarif_sn_f, sum_tarif_sn_f_p " +
                     serial_val +
                " From t_itog Where 1 = 1 ";

                if (chargeXX.paramcalc.nzp_kvar > 0 || chargeXX.paramcalc.nzp_dom > 0)
                    ret = ExecSQL(conn_db, sqql, true);
                else
                    ExecByStep(conn_db, "t_itog", "nzp_key",
                        sqql, 50000, " ", out ret);

                if (!ret.result)
                {
                    DropTempTablesCharge(conn_db);
                    return false;
                }



                //---------------------------------------------------
                //отметка о дате расчета
                //---------------------------------------------------
                sqql =
                    " Update " + chargeXX.kvar_calc_xx +
                    " Set dat_calc = " + sCurDate +
                    " Where " + chargeXX.where_kvar;

                ret = ExecSQL(conn_db, sqql, true);

                if (!ret.result)
                {
                    DropTempTablesCharge(conn_db);
                    return false;
                }

                sqql = 
                    " Insert into " + chargeXX.kvar_calc_xx + " (nzp_kvar, num_ls, dat_calc) " +
                    " Select nzp_kvar,num_ls," + sCurDate +
                    " From t_selkvar k " +
                    " Where 0 = ( Select count(*) From " + chargeXX.kvar_calc_xx + " f Where k.nzp_kvar=f.nzp_kvar ) ";

                ret = ExecSQL(conn_db, sqql, true);

                if (!ret.result)
                {
                    DropTempTablesCharge(conn_db);
                    return false;
                }

            }



            UpdateStatistics(true, paramcalc, chargeXX.charge_tab, out ret);
            if (!ret.result)
            {
                DropTempTablesCharge(conn_db);
                return false;
            }
            DropTempTablesCharge(conn_db);
            return true;
        }

        /// <summary>
        /// Учет оплат
        /// </summary>
        /// <param name="conn_db"></param>
        /// <param name="paramcalc"></param>
        /// <param name="ret"></param>
        /// <returns></returns>
        public Returns CalcChargeXXUchetOplat(IDbConnection conn_db, ParamCalc paramcalc)
        {
            if (Constants.Trace) Utility.ClassLog.WriteLog("Старт функции CalcChargeXXUchetOplat");

            if (!paramcalc.b_cur)
            {
                return new Returns(true, "В месяце перерасчета учет оплат не производится", -1);
            }

            ChargeXX chargeXX = new ChargeXX(paramcalc);

            // на момент вызова функции список лицевых счетов должен бюыть выбран в таблицу t_selkvar
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

            string sql =
                " Update " + chargeXX.charge_xx +
                " Set (sum_pere, sum_money, money_to, money_from, money_del, sum_outsaldo, sum_charge) = ( 0, 0, 0, 0, 0, 0, 0 ) " +
                " Where " + chargeXX.where_kvar +
                chargeXX.paramcalc.per_dat_charge;

            Returns ret;
            if (chargeXX.paramcalc.nzp_kvar > 0 || chargeXX.paramcalc.nzp_dom > 0 || chargeXX.paramcalc.nzp_pack_saldo > 0)
            {
                ret = ExecSQL(conn_db, sql, true);
            }
            else
            {
                ExecByStep(conn_db, chargeXX.charge_xx, "nzp_charge", sql, 50000, " ", out ret);
            }

            if (!ret.result) return ret;

            //UpdateStatistics(false, paramcalc, chargeXX.charge_tab, out ret);
            if (!ret.result) return ret;

            //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
            //при перерасчете исключить некоторые сальдовые таблицы (prekedka, del_supplier, etc)
            //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
            if (!chargeXX.paramcalc.b_reval)
            {
                #region удаление автоматических перераспределений сальдо
#if PG
                sql =
                    " Delete From " + chargeXX.del_supplier +
                    " Where num_ls in ( Select num_ls From t_selkvar ) " +
                    "   and kod_sum = " + (int)Faktura.Kinds.SaldoAutomaticRedistribution +
                    "   and dat_account = public.MDY (" + chargeXX.paramcalc.calc_mm + ",28," + chargeXX.paramcalc.calc_yy + ") ";
#else
sql =
                    " Delete From " + chargeXX.del_supplier +
                    " Where num_ls in ( Select num_ls From t_selkvar ) " +
                    "   and kod_sum = " + (int)Faktura.Kinds.SaldoAutomaticRedistribution +
                    "   and dat_account = MDY (" + chargeXX.paramcalc.calc_mm + ",28," + chargeXX.paramcalc.calc_yy + ") ";
#endif

                if (chargeXX.paramcalc.nzp_kvar > 0 || chargeXX.paramcalc.nzp_dom > 0 || chargeXX.paramcalc.nzp_pack_saldo > 0)
                {
                    ret = ExecSQL(conn_db, sql, true);
                }
                else
                {
                    ExecByStep(conn_db, chargeXX.del_supplier, "nzp_del", sql, 50000, " ", out ret);
                }
                if (!ret.result) return ret;

                UpdateStatistics(false, paramcalc, "del_supplier", out ret);
                if (!ret.result) return ret;
                #endregion

                #region заполнение money_to
                //fn_supplier
                // если расчет по кол-ву л/с меньше 100, то соединить с t_selkvar для min времени выборки
                ExecSQL(conn_db, " Drop table t_fn_suppall ", false);
                if (bIsL100)
                {
#if PG
                    sql =
                        " Select a.num_ls,a.nzp_supp,a.nzp_serv,a.sum_prih " +
                        " Into temp t_fn_suppall "+
                        " From " + chargeXX.fn_supplier + " a, t_selkvar k" +
                        " Where a.num_ls = k.num_ls " +
                        " and abs(a.sum_prih) > 0.0001 and a.nzp_serv <> 1 " ;
#else
 sql =
                    " Select a.num_ls,a.nzp_supp,a.nzp_serv,a.sum_prih " +
                    " From " + chargeXX.fn_supplier + " a, t_selkvar k" +
                    " Where a.num_ls = k.num_ls " +
                    " and abs(a.sum_prih) > 0.0001 and a.nzp_serv <> 1 " +
                    " Into temp t_fn_suppall With no log ";
#endif
                }
                else
                {
#if PG
                    sql =
                        " Select num_ls,nzp_supp,nzp_serv,sum_prih " +
                        " Into temp t_fn_suppall "+
                        " From " + chargeXX.fn_supplier +
                        " Where abs(sum_prih) > 0.0001 and nzp_serv <> 1 ";
#else
sql =
                    " Select num_ls,nzp_supp,nzp_serv,sum_prih " +
                    " From " + chargeXX.fn_supplier +
                    " Where abs(sum_prih) > 0.0001 and nzp_serv <> 1 " +
                    " Into temp t_fn_suppall With no log ";
#endif
                }

                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result) return ret;

                ExecSQL(conn_db, " Create index ix_t_fn_suppall on t_fn_suppall (num_ls) ", true);
#if PG
                ExecSQL(conn_db, " analyze t_fn_suppall ", true);
#else
ExecSQL(conn_db, " Update statistics for table t_fn_suppall ", true);
#endif

                ExecSQL(conn_db, " Drop table t_fn_supp ", false);
#if PG
                ret = ExecSQL(conn_db,
                                    " Select distinct k.nzp_kvar,k.num_ls,nzp_supp,nzp_serv,sum(sum_prih) as money_to " +
                                    " Into temp t_fn_supp "+
                                    " From t_fn_suppall a, t_selkvar k " +
                                    " Where a.num_ls = k.num_ls " +
                                    " Group by 1,2,3,4 ", true);
#else
ret = ExecSQL(conn_db,
                    " Select unique k.nzp_kvar,k.num_ls,nzp_supp,nzp_serv,sum(sum_prih) as money_to " +
                    " From t_fn_suppall a, t_selkvar k " +
                    " Where a.num_ls = k.num_ls " +
                    " Group by 1,2,3,4 " +
                    " Into temp t_fn_supp With no log "
                    , true);
#endif
                ExecSQL(conn_db, " Drop table t_fn_suppall ", false);
                if (!ret.result) return ret;

                ExecSQL(conn_db, " Create unique index ix_t_fn_supp on t_fn_supp (nzp_kvar,nzp_serv,nzp_supp) ", true);
#if PG
                ExecSQL(conn_db, " analyze t_fn_supp ", true);
#else
 ExecSQL(conn_db, " Update statistics for table t_fn_supp ", true);
#endif

#if PG
                sql = " Update " + chargeXX.charge_xx +
                                      " Set money_to = ( Select sum(money_to) From t_fn_supp gk " +
                                                       " Where gk.nzp_kvar = " + chargeXX.charge_xx + ".nzp_kvar " +
                                                       "   and gk.nzp_serv = " + chargeXX.charge_xx + ".nzp_serv " +
                                                       "   and gk.nzp_supp = " + chargeXX.charge_xx + ".nzp_supp ) " +
                                      " Where 0 < (     Select count(*) From t_fn_supp gk " +
                                                      " Where gk.nzp_kvar = " + chargeXX.charge_xx + ".nzp_kvar " +
                                                      "   and gk.nzp_serv = " + chargeXX.charge_xx + ".nzp_serv " +
                                                      "   and gk.nzp_supp = " + chargeXX.charge_xx + ".nzp_supp ) "
                                       + chargeXX.paramcalc.per_dat_charge
                                       + " and " + chargeXX.where_kvar;
#else
 sql = " Update " + chargeXX.charge_xx +
                       " Set money_to = ( Select sum(money_to) From t_fn_supp gk " +
                                        " Where gk.nzp_kvar = " + chargeXX.charge_xx + ".nzp_kvar " +
                                        "   and gk.nzp_serv = " + chargeXX.charge_xx + ".nzp_serv " +
                                        "   and gk.nzp_supp = " + chargeXX.charge_xx + ".nzp_supp ) " +
                       " Where 0 < (     Select count(*) From t_fn_supp gk " +
                                       " Where gk.nzp_kvar = " + chargeXX.charge_xx + ".nzp_kvar " +
                                       "   and gk.nzp_serv = " + chargeXX.charge_xx + ".nzp_serv " +
                                       "   and gk.nzp_supp = " + chargeXX.charge_xx + ".nzp_supp ) "
                        + chargeXX.paramcalc.per_dat_charge
                        + " and " + chargeXX.where_kvar;  
#endif //nzp_kvar in ( Select nzp_kvar From t_selkvar ) ";

                if (chargeXX.paramcalc.nzp_kvar > 0 || chargeXX.paramcalc.nzp_dom > 0 || chargeXX.paramcalc.nzp_pack_saldo > 0)
                    ret = ExecSQL(conn_db, sql, true);
                else
                    ExecByStep(conn_db, chargeXX.charge_xx, "nzp_charge",
                        sql, 50000, " ", out ret);
                ExecSQL(conn_db, " Drop table t_fn_supp ", false);
                if (!ret.result) return ret;
                #endregion

                #region заполнение money_from
                ExecSQL(conn_db, " Drop table t_from_supp ", false);
#if PG
                ret = ExecSQL(conn_db,
                                    " Select distinct k.nzp_kvar,k.num_ls,nzp_supp,nzp_serv,sum(sum_prih) as money_from " +
                                    " Into temp t_from_supp "+
                                    " From " + chargeXX.from_supplier + " a, t_selkvar k " +
                                    " Where a.num_ls = k.num_ls " +
                                    "   and dat_uchet >= " + chargeXX.paramcalc.dat_s +
                                    "   and dat_uchet <= " + chargeXX.paramcalc.dat_po +
                                    "   and abs(sum_prih) > 0.0001 " +
                                    "   and nzp_serv <> 1 " +
                                    " Group by 1,2,3,4 ", true);
#else
ret = ExecSQL(conn_db,
                    " Select unique k.nzp_kvar,k.num_ls,nzp_supp,nzp_serv,sum(sum_prih) as money_from " +
                    " From " + chargeXX.from_supplier + " a, t_selkvar k " +
                    " Where a.num_ls = k.num_ls " +
                    "   and dat_uchet >= " + chargeXX.paramcalc.dat_s +
                    "   and dat_uchet <= " + chargeXX.paramcalc.dat_po +
                    "   and abs(sum_prih) > 0.0001 " +
                    "   and nzp_serv <> 1 " +
                    " Group by 1,2,3,4 " +
                    " Into temp t_from_supp With no log "
                    , true);
#endif
                if (!ret.result) return ret;

                ExecSQL(conn_db, " Create unique index ix_t_from_supp on t_from_supp (nzp_kvar,nzp_serv,nzp_supp) ", true);
#if PG
                ExecSQL(conn_db, " analyze t_from_supp ", true);
#else
ExecSQL(conn_db, " Update statistics for table t_from_supp ", true);
#endif

#if PG
                sql = " Update " + chargeXX.charge_xx +
                                       " Set money_from = ( " +
                                                        " Select sum(money_from) From t_from_supp gk " +
                                                        " Where gk.nzp_kvar = " + chargeXX.charge_xx + ".nzp_kvar " +
                                                        "   and gk.nzp_serv = " + chargeXX.charge_xx + ".nzp_serv " +
                                                        "   and gk.nzp_supp = " + chargeXX.charge_xx + ".nzp_supp ) " +
                                       " Where 0 < ( Select count(*) From t_from_supp gk " +
                                                       " Where gk.nzp_kvar = " + chargeXX.charge_xx + ".nzp_kvar " +
                                                       "   and gk.nzp_serv = " + chargeXX.charge_xx + ".nzp_serv " +
                                                       "   and gk.nzp_supp = " + chargeXX.charge_xx + ".nzp_supp ) "
                                        + chargeXX.paramcalc.per_dat_charge
                                        + " and " + chargeXX.where_kvar;
#else
sql = " Update " + chargeXX.charge_xx +
                       " Set money_from = ( " +
                                        " Select sum(money_from) From t_from_supp gk " +
                                        " Where gk.nzp_kvar = " + chargeXX.charge_xx + ".nzp_kvar " +
                                        "   and gk.nzp_serv = " + chargeXX.charge_xx + ".nzp_serv " +
                                        "   and gk.nzp_supp = " + chargeXX.charge_xx + ".nzp_supp ) " +
                       " Where 0 < ( Select count(*) From t_from_supp gk " +
                                       " Where gk.nzp_kvar = " + chargeXX.charge_xx + ".nzp_kvar " +
                                       "   and gk.nzp_serv = " + chargeXX.charge_xx + ".nzp_serv " +
                                       "   and gk.nzp_supp = " + chargeXX.charge_xx + ".nzp_supp ) "
                        + chargeXX.paramcalc.per_dat_charge
                        + " and " + chargeXX.where_kvar; 
#endif  //nzp_kvar in ( Select nzp_kvar From t_selkvar ) ";

                if (chargeXX.paramcalc.nzp_kvar > 0 || chargeXX.paramcalc.nzp_dom > 0 || chargeXX.paramcalc.nzp_pack_saldo > 0)
                    ret = ExecSQL(conn_db, sql, true);
                else
                    ExecByStep(conn_db, chargeXX.charge_xx, "nzp_charge",
                        sql, 50000, " ", out ret);

                ExecSQL(conn_db, " Drop table t_from_supp ", false);
                if (!ret.result) return ret;
                #endregion

                #region заполнение money_del
                ExecSQL(conn_db, " Drop table t_del_supp ", false);
#if PG
                ret = ExecSQL(conn_db,
                                   " Select distinct k.nzp_kvar,k.num_ls,nzp_supp,nzp_serv,sum(sum_prih) as money_del " +
                                    " Into temp t_del_supp "+
                                   " From " + chargeXX.del_supplier + " a, t_selkvar k " +
                                   " Where a.num_ls = k.num_ls " +
                                   "   and dat_account >= " + chargeXX.paramcalc.dat_s +
                                   "   and dat_account <= " + chargeXX.paramcalc.dat_po +
                                   "   and abs(sum_prih) > 0.0001 " +
                                   "   and nzp_serv <> 1 " +
                                   " Group by 1,2,3,4 " , true);
#else
 ret = ExecSQL(conn_db,
                    " Select unique k.nzp_kvar,k.num_ls,nzp_supp,nzp_serv,sum(sum_prih) as money_del " +
                    " From " + chargeXX.del_supplier + " a, t_selkvar k " +
                    " Where a.num_ls = k.num_ls " +
                    "   and dat_account >= " + chargeXX.paramcalc.dat_s +
                    "   and dat_account <= " + chargeXX.paramcalc.dat_po +
                    "   and abs(sum_prih) > 0.0001 " +
                    "   and nzp_serv <> 1 " +
                    " Group by 1,2,3,4 " +
                    " Into temp t_del_supp With no log "
                    , true);
#endif
                if (!ret.result) return ret;

                ExecSQL(conn_db, " Create unique index ix_t_del_supp on t_del_supp (nzp_kvar,nzp_serv,nzp_supp) ", true);
#if PG
                ExecSQL(conn_db, " analyze t_del_supp ", true);
#else
 ExecSQL(conn_db, " Update statistics for table t_del_supp ", true);
#endif

#if PG
                sql = " Update " + chargeXX.charge_xx +
                                       " Set money_del = ( " +
                                                        " Select sum(money_del) From t_del_supp gk " +
                                                        " Where gk.nzp_kvar = " + chargeXX.charge_xx + ".nzp_kvar " +
                                                        "   and gk.nzp_serv = " + chargeXX.charge_xx + ".nzp_serv " +
                                                        "   and gk.nzp_supp = " + chargeXX.charge_xx + ".nzp_supp ) " +
                                       " Where 0 < ( Select count(*) From t_del_supp gk " +
                                                       " Where gk.nzp_kvar = " + chargeXX.charge_xx + ".nzp_kvar " +
                                                       "   and gk.nzp_serv = " + chargeXX.charge_xx + ".nzp_serv " +
                                                       "   and gk.nzp_supp = " + chargeXX.charge_xx + ".nzp_supp ) "
                                       + chargeXX.paramcalc.per_dat_charge
                                        + " and " + chargeXX.where_kvar;
#else
sql = " Update " + chargeXX.charge_xx +
                       " Set money_del = ( " +
                                        " Select sum(money_del) From t_del_supp gk " +
                                        " Where gk.nzp_kvar = " + chargeXX.charge_xx + ".nzp_kvar " +
                                        "   and gk.nzp_serv = " + chargeXX.charge_xx + ".nzp_serv " +
                                        "   and gk.nzp_supp = " + chargeXX.charge_xx + ".nzp_supp ) " +
                       " Where 0 < ( Select count(*) From t_del_supp gk " +
                                       " Where gk.nzp_kvar = " + chargeXX.charge_xx + ".nzp_kvar " +
                                       "   and gk.nzp_serv = " + chargeXX.charge_xx + ".nzp_serv " +
                                       "   and gk.nzp_supp = " + chargeXX.charge_xx + ".nzp_supp ) "
                       + chargeXX.paramcalc.per_dat_charge
                        + " and " + chargeXX.where_kvar; 
#endif  //nzp_kvar in ( Select nzp_kvar From t_selkvar ) ";

                if (chargeXX.paramcalc.nzp_kvar > 0 || chargeXX.paramcalc.nzp_dom > 0 || chargeXX.paramcalc.nzp_pack_saldo > 0)
                    ret = ExecSQL(conn_db, sql, true);
                else
                    ExecByStep(conn_db, chargeXX.charge_xx, "nzp_charge",
                        sql, 50000, " ", out ret);

                ExecSQL(conn_db, " Drop table t_del_supp ", false);
                if (!ret.result) return ret;
                #endregion

                #region заполнение sum_money, sum_outsaldo, sum_pere
                sql = " Update " + chargeXX.charge_xx +
                       " Set sum_money = money_to + money_from + money_del " +
                          " ,sum_outsaldo = sum_insaldo +  real_charge + sum_real + reval + izm_saldo - (money_to + money_from + money_del) " +
                          " ,sum_pere  = sum_insaldo + reval + real_charge - (money_to + money_from + money_del) " +
                       " Where " + chargeXX.where_kvar +
                        chargeXX.paramcalc.per_dat_charge;

                if (chargeXX.paramcalc.nzp_kvar > 0 || chargeXX.paramcalc.nzp_dom > 0 || chargeXX.paramcalc.nzp_pack_saldo > 0)
                    ret = ExecSQL(conn_db, sql, true);
                else
                    ExecByStep(conn_db, chargeXX.charge_xx, "nzp_charge",
                        sql, 50000, " ", out ret);

                if (!ret.result) return ret;
                #endregion

                //Перераспределение переплат
                ret = CalcChargeXXUchetPereplat(conn_db, chargeXX);

                #region расчет суммы к оплате (sum_charge)
                sql = " Update " + chargeXX.charge_xx +
                    " Set sum_charge = ";

                switch (Points.packDistributionParameters.chargeMethod)
                {
                    case PackDistributionParameters.ChargeMethods.Outsaldo:
                        sql = sql + " sum_outsaldo ";
                        break;
                    case PackDistributionParameters.ChargeMethods.PositiveOutsaldo:
                        sql = sql + " case when sum_outsaldo>0 then sum_outsaldo else 0 end ";
                        break;
                    case PackDistributionParameters.ChargeMethods.MonthlyCalculationWithChangesAndOverpayment:
                        sql = sql + " sum_real + real_charge + reval + (case when sum_insaldo<0 then sum_insaldo else 0 end)";
                        break;
                    case PackDistributionParameters.ChargeMethods.PositiveMonthlyCalculationWithChangesAndOverpayment:
                        sql = sql + " case when (sum_real + real_charge + reval + (case when sum_insaldo<0 then sum_insaldo else 0 end)) >0" +
                                          " then sum_real + real_charge + reval + (case when sum_insaldo<0 then sum_insaldo else 0 end)" +
                                          " else 0 end ";
                        break;
                    case PackDistributionParameters.ChargeMethods.MonthlyCalculationWithChanges:
                        sql = sql + " sum_real + real_charge + reval ";
                        break;
                    case PackDistributionParameters.ChargeMethods.PositiveMonthlyCalculationWithChanges:
                        sql = sql + " case when (sum_real + real_charge + reval) >0" +
                                          " then sum_real + real_charge + reval" +
                                          " else 0 end ";
                        break;
                    default: sql = sql + " sum_real + real_charge + reval ";
                        break;
                }

                sql = sql +
                       " Where " + chargeXX.where_kvar +
                        chargeXX.paramcalc.per_dat_charge;

                if (chargeXX.paramcalc.nzp_kvar > 0 || chargeXX.paramcalc.nzp_dom > 0 || chargeXX.paramcalc.nzp_pack_saldo > 0)
                    ret = ExecSQL(conn_db, sql, true);
                else
                    ExecByStep(conn_db, chargeXX.charge_xx, "nzp_charge",
                        sql, 50000, " ", out ret);

                if (!ret.result) return ret;
                #endregion

                #region расчет итоговой услуги nzp_serv = 1
                ExecSQL(conn_db, " Drop table t_itog ", false);

#if PG
                ret = ExecSQL(conn_db,
                    " Create temp table t_itog " +
                    " ( nzp_key    serial not null, " +
                    " nzp_kvar   integer, " +
                    " sum_pere numeric(14,2) default 0.00, " +
                    " sum_money numeric(14,2) default 0.00, " +
                    " money_to numeric(14,2) default 0.00, " +
                    " money_from numeric(14,2) default 0.00, " +
                    " money_del numeric(14,2) default 0.00, " +
                    " sum_outsaldo numeric(14,2) default 0.00, " +
                    " sum_charge numeric(14,2) default 0.00" +
                    " )  "
                    , true);
#else
                ret = ExecSQL(conn_db,
                    " Create temp table t_itog " +
                    " ( nzp_key    serial not null, " +
                    " nzp_kvar   integer, " +
                    " sum_pere decimal(14,2) default 0.00, " +
                    " sum_money decimal(14,2) default 0.00, " +
                    " money_to decimal(14,2) default 0.00, " +
                    " money_from decimal(14,2) default 0.00, " +
                    " money_del decimal(14,2) default 0.00, " +
                    " sum_outsaldo decimal(14,2) default 0.00, " +
                    " sum_charge decimal(14,2) default 0.00" +
                    " ) With no log "
                    , true);
#endif

                if (!ret.result) return ret;

                sql =
                    " Insert into t_itog " +
                    " Select 0, k.nzp_kvar, " +

                       " sum(sum_pere) as sum_pere, " +
                       " sum(sum_money) as sum_money, " +
                       " sum(money_to) as money_to, " +
                       " sum(money_from) as money_from, " +
                       " sum(money_del) as money_del, " +
                       " sum(sum_outsaldo) as sum_outsaldo, " +
                       " sum(sum_charge) as sum_charge" +

                    " From " + chargeXX.charge_xx + " ch, t_selkvar k " +
                    " Where ch.nzp_kvar = k.nzp_kvar "
                     + chargeXX.paramcalc.per_dat_charge +
                    "   and nzp_serv <> 1 ";
                if (chargeXX.paramcalc.nzp_kvar > 0 || chargeXX.paramcalc.nzp_dom > 0 || chargeXX.paramcalc.nzp_pack_saldo > 0)
                {
                    sql = sql + " Group by 1,2 ";
                    ret = ExecSQL(conn_db, sql, true);
                }
                else
                    ExecByStep(conn_db, "t_selkvar k", "k.nzp_kvar",
                        sql, 50000, " Group by 1,2 ", out ret);
                if (!ret.result)
                {
                    ExecSQL(conn_db, " Drop table t_itog ", false);
                    return ret;
                }

                //ExecSQL(conn_db, " Create unique index ix1_t_itog on t_itog (nzp_key) ", true);
                ExecSQL(conn_db, " Create unique index ix2_t_itog on t_itog (nzp_kvar) ", true);
                ExecSQL(conn_db, sUpdStat + " t_itog ", true);

                string p_dat_charge = DateNullString;
                if (!chargeXX.paramcalc.b_cur)
                    p_dat_charge = MDY(chargeXX.paramcalc.cur_mm, 28, chargeXX.paramcalc.cur_yy);

                sql = "update " + chargeXX.charge_xx + " set " +
#if PG
                    " sum_pere     =(select sum_pere     From t_itog a Where a.nzp_kvar = " + chargeXX.charge_xx + ".nzp_kvar)" +
                    ",sum_outsaldo =(select sum_outsaldo From t_itog a Where a.nzp_kvar = " + chargeXX.charge_xx + ".nzp_kvar)" +
                    ",sum_charge   =(select sum_charge   From t_itog a Where a.nzp_kvar = " + chargeXX.charge_xx + ".nzp_kvar)" +
                    ",sum_money    =(select sum_money    From t_itog a Where a.nzp_kvar = " + chargeXX.charge_xx + ".nzp_kvar)" +
                    ",money_to     =(select money_to     From t_itog a Where a.nzp_kvar = " + chargeXX.charge_xx + ".nzp_kvar)" +
                    ",money_from   =(select money_from   From t_itog a Where a.nzp_kvar = " + chargeXX.charge_xx + ".nzp_kvar)" +
                    ",money_del    =(select money_del    From t_itog a Where a.nzp_kvar = " + chargeXX.charge_xx + ".nzp_kvar)" +
#else
                    " (sum_pere, sum_outsaldo, sum_charge, sum_money, money_to, money_from, money_del) = " +
                    " ((select sum_pere, sum_outsaldo, sum_charge, sum_money, money_to, money_from, money_del " +
                    " From t_itog a Where a.nzp_kvar = " + chargeXX.charge_xx + ".nzp_kvar))" +
#endif
                    " where nzp_serv = 1 and 0 < (select count(*) from t_itog b where b.nzp_kvar = " + chargeXX.charge_xx + ".nzp_kvar)" +
                    chargeXX.paramcalc.per_dat_charge +
                    " and " + chargeXX.where_kvar;

                if (chargeXX.paramcalc.nzp_kvar > 0 || chargeXX.paramcalc.nzp_dom > 0 || chargeXX.paramcalc.nzp_pack_saldo > 0)
                    ret = ExecSQL(conn_db, sql, true);
                else
                    ExecByStep(conn_db, chargeXX.charge_xx, "nzp_charge",
                        sql, 50000, " ", out ret);

                ExecSQL(conn_db, " Drop table t_itog ", false);
                if (!ret.result) return ret;
                #endregion
            }

            UpdateStatistics(true, paramcalc, chargeXX.charge_tab, out ret);

            if (Constants.Trace) Utility.ClassLog.WriteLog("Стоп функции CalcChargeXXUchetOplat");
            return ret;
        }

        /// <summary>
        /// Перераспределение переплат
        /// </summary>
        /// <param name="conn_db"></param>
        /// <param name="paramcalc"></param>
        /// <param name="ret"></param>
        /// <returns></returns>
        protected Returns CalcChargeXXUchetPereplat(IDbConnection conn_db, ChargeXX chargeXX)
        {
            //Алгоритм следуюущий:
            // 1 - выбрать все строки с sum_outsaldo < 0
            // 2 - по этим лс выбрать суммы по услуге sum_outsaldo > 0
            // 3 - выбратьм max(sum_outsaldo>0), чтобы туда переложить минус
            // 4 - переложить в пределах суммы переплаты не более суммы > 0 (заполнить sum_del)
            // 5 - цикл повторяется (sum_del увеличивается), пока есть куда перекладывать
            // anes

            IDataReader reader;

#if PG
            Returns ret = ExecRead(conn_db, out reader,
                            " Select nzp_prm " +
                            " From " + chargeXX.paramcalc.pref + "_data.prm_5 p " +
                            " Where p.nzp_prm = 1996 and p.val_prm='1' " +
                            "   and p.is_actual <> 100 and p.dat_s  <= " + chargeXX.paramcalc.dat_po +
                            "   and p.dat_po >= " + chargeXX.paramcalc.dat_s + " "
                            , true);
#else
Returns ret = ExecRead(conn_db, out reader,
                " Select nzp_prm " +
                " From " + chargeXX.paramcalc.pref + "_data:prm_5 p " +
                " Where p.nzp_prm = 1996 and p.val_prm='1' " +
                "   and p.is_actual <> 100 and p.dat_s  <= " + chargeXX.paramcalc.dat_po +
                "   and p.dat_po >= " + chargeXX.paramcalc.dat_s + " "
                , true);
#endif
            if (!ret.result) return ret;

            bool b_make_del_supplier = false;
            if (reader.Read())
            {
                b_make_del_supplier = true;
            }
            reader.Close();
            reader.Dispose();

            if (!b_make_del_supplier) return ret;

            //загнать следы в del_supplier
            ExecSQL(conn_db, " Drop table t_perek ", false);

#if PG
            ret = ExecSQL(conn_db,
                //" Create table are.t_perek " +
                            " Create temp table t_perek " +
                            " ( nzp_key    serial not null, " +
                            "   nzp_kvar   integer, " +
                            "   num_ls     integer, " +
                            "   nzp_serv   integer, " +
                            "   nzp_supp   integer, " +
                            "   nzp_charge integer, " +
                            "   sum_out    numeric(14,2) default 0.00, " +
                            "   sum_del    numeric(14,2) default 0.00 " +
                            " ) "
                //" ) "
                            , true);
#else
ret = ExecSQL(conn_db,
                //" Create table are.t_perek " +
                " Create temp table t_perek " +
                " ( nzp_key    serial not null, " +
                "   nzp_kvar   integer, " +
                "   num_ls     integer, " +
                "   nzp_serv   integer, " +
                "   nzp_supp   integer, " +
                "   nzp_charge integer, " +
                "   sum_out    decimal(14,2) default 0.00, " +
                "   sum_del    decimal(14,2) default 0.00 " +
                " ) With no log "
                //" ) "
                , true);
#endif
            if (!ret.result) return ret;

            ret = ExecSQL(conn_db,
                " Insert into t_perek (nzp_kvar,num_ls,nzp_supp,nzp_serv,nzp_charge, sum_del,sum_out) " +
                " Select a.nzp_kvar,a.num_ls,a.nzp_supp,a.nzp_serv,a.nzp_charge, 0 sum_del, a.sum_outsaldo " +
                " From " + chargeXX.charge_xx + " a, t_selkvar k " +
                " Where a.nzp_kvar=k.nzp_kvar "
                 + chargeXX.paramcalc.per_dat_charge +
                "   and a.sum_outsaldo < 0 "
                , true);
            if (!ret.result)
            {
                ExecSQL(conn_db, " Drop table t_perek ", false);
                return ret;
            }

            ExecSQL(conn_db, " Create unique index ix_t_perek1 on t_perek (nzp_key) ", true);
            ExecSQL(conn_db, " Create unique index ix_t_perek2 on t_perek (nzp_charge) ", true);
            ExecSQL(conn_db, " Create unique index ix_t_perek3 on t_perek (nzp_kvar,nzp_serv,nzp_supp) ", true);
#if PG
            ExecSQL(conn_db, " analyze t_perek ", true);
#else
 ExecSQL(conn_db, " Update statistics for table t_perek ", true);
#endif

            try
            {
                //загоним положительные суммы по услуге
                ExecSQL(conn_db, " Drop table t_ins_ch1 ", false);

#if PG
                ret = ExecSQL(conn_db,
                                    " Select ch.nzp_kvar,ch.num_ls,ch.nzp_supp,ch.nzp_serv,ch.nzp_charge, 0 sum_del, ch.sum_outsaldo " +
                                      " Into temp t_ins_ch1  "+
                                    " From " + chargeXX.charge_xx + " ch, t_selkvar k " +
                                    " Where ch.nzp_kvar=k.nzp_kvar "
                                     + chargeXX.paramcalc.per_dat_charge +
                                    "   and ch.sum_outsaldo > 0 " +
                                    "   and 0 < ( Select count(*) From t_perek gk " +
                                                " Where gk.nzp_kvar = ch.nzp_kvar " +
                                                "   and gk.nzp_serv = ch.nzp_serv ) " 
                                    , true);
#else
ret = ExecSQL(conn_db,
                    " Select ch.nzp_kvar,ch.num_ls,ch.nzp_supp,ch.nzp_serv,ch.nzp_charge, 0 sum_del, ch.sum_outsaldo " +
                    " From " + chargeXX.charge_xx + " ch, t_selkvar k " +
                    " Where ch.nzp_kvar=k.nzp_kvar "
                     + chargeXX.paramcalc.per_dat_charge +
                    "   and ch.sum_outsaldo > 0 " +
                    "   and 0 < ( Select count(*) From t_perek gk " +
                                " Where gk.nzp_kvar = ch.nzp_kvar " +
                                "   and gk.nzp_serv = ch.nzp_serv ) " +
                    " Into temp t_ins_ch1 With no log "
                    , true);
#endif
                if (!ret.result) return ret;

                ret = ExecSQL(conn_db,
                    " Insert into t_perek (nzp_kvar,num_ls,nzp_supp,nzp_serv,nzp_charge, sum_del,sum_out) " +
                    " Select nzp_kvar,num_ls,nzp_supp,nzp_serv,nzp_charge, sum_del, sum_outsaldo " +
                    " From t_ins_ch1 "
                    , true);
                if (!ret.result) return ret;

#if PG
                ExecSQL(conn_db, " analyze t_perek ", true);
#else
ExecSQL(conn_db, " Update statistics for table t_perek ", true);
#endif

                //---------------------------------------------------
                //начинаем циклить, пока не все суммы взаимоучтем
                //---------------------------------------------------
                bool yes_del = false;

                while (true)
                {
                    ExecSQL(conn_db, " Drop table t_delmin ", false);
                    ExecSQL(conn_db, " Drop table t_delplu ", false);
                    ExecSQL(conn_db, " Drop table t_ins_ch1 ", false);

                    //ищем минимальный минус
#if PG
                    ret = ExecSQL(conn_db,
                                        " Select nzp_kvar, nzp_serv, max(nzp_charge) as nzp_charge, max(sum_out+sum_del) as sum_del " +
                                        " Into temp t_delmin "+
                                        " From t_perek a " +
                                        " Where (sum_out+sum_del) = ( Select min(sum_out+sum_del) From t_perek b " +
                                                                    " Where a.nzp_kvar = b.nzp_kvar " +
                                                                    "   and a.nzp_serv = b.nzp_serv " +
                                                                    "   and sum_out+sum_del < 0 ) " +
                                        "   and sum_out+sum_del < 0 " +
                                        " Group by 1,2 " 
                                        , true);
#else
ret = ExecSQL(conn_db,
                    " Select nzp_kvar, nzp_serv, max(nzp_charge) as nzp_charge, max(sum_out+sum_del) as sum_del " +
                    " From t_perek a " +
                    " Where (sum_out+sum_del) = ( Select min(sum_out+sum_del) From t_perek b " +
                                                " Where a.nzp_kvar = b.nzp_kvar " +
                                                "   and a.nzp_serv = b.nzp_serv " +
                                                "   and sum_out+sum_del < 0 ) " +
                    "   and sum_out+sum_del < 0 " +
                    " Group by 1,2 " +
                    " Into temp t_delmin With no log "
                    , true);
#endif
                    if (!ret.result) return ret;

                    ExecSQL(conn_db, " Create unique index ix_t_delmin on t_delmin (nzp_kvar,nzp_serv) ", true);
#if PG
                    ExecSQL(conn_db, " analyze t_delmin ", true);
#else
 ExecSQL(conn_db, " Update statistics for table t_delmin ", true);
#endif

                    //ищем максимальный плюс
#if PG
                    ret = ExecSQL(conn_db,
                                       " Select nzp_kvar, nzp_serv, max(nzp_charge) as nzp_charge, max(sum_out+sum_del) as sum_del " +
                                       " Into temp t_delplu " +
                                       " From t_perek a " +
                                       " Where (sum_out+sum_del) = ( Select max(sum_out+sum_del) From t_perek b " +
                                                                   " Where a.nzp_kvar = b.nzp_kvar " +
                                                                   "   and a.nzp_serv = b.nzp_serv " +
                                                                   "   and sum_out+sum_del > 0 ) " +
                                       "   and sum_out+sum_del > 0 " +
                                       " Group by 1,2 " 
                                       , true);
#else
 ret = ExecSQL(conn_db,
                    " Select nzp_kvar, nzp_serv, max(nzp_charge) as nzp_charge, max(sum_out+sum_del) as sum_del " +
                    " From t_perek a " +
                    " Where (sum_out+sum_del) = ( Select max(sum_out+sum_del) From t_perek b " +
                                                " Where a.nzp_kvar = b.nzp_kvar " +
                                                "   and a.nzp_serv = b.nzp_serv " +
                                                "   and sum_out+sum_del > 0 ) " +
                    "   and sum_out+sum_del > 0 " +
                    " Group by 1,2 " +
                    " Into temp t_delplu With no log "
                    , true);
#endif
                    if (!ret.result) return ret;

                    ExecSQL(conn_db, " Create unique index ix_t_delplu on t_delplu (nzp_kvar,nzp_serv) ", true);
#if PG
                    ExecSQL(conn_db, " analyze t_delplu ", true);
#else
 ExecSQL(conn_db, " Update statistics for table t_delplu ", true);
#endif


                    //соединяем между собой эти таблицы и вычисляем дельты в пределах общих абсолютных значениях

#if PG
                    ret = ExecSQL(conn_db,
                                        " Select a.nzp_charge as nzp_min, b.nzp_charge as nzp_plu, " +
                                           " case when a.sum_del + b.sum_del > 0 then abs(a.sum_del) else b.sum_del end as sum_dlt " +
                                           " Into temp t_ins_ch1 "+
                                        " From t_delmin a, t_delplu b " +
                                        " Where a.nzp_kvar = b.nzp_kvar " +
                                        "   and a.nzp_serv = b.nzp_serv " 
                                        
                                        , true);
#else
ret = ExecSQL(conn_db,
                    " Select a.nzp_charge as nzp_min, b.nzp_charge as nzp_plu, " +
                       " case when a.sum_del + b.sum_del > 0 then abs(a.sum_del) else b.sum_del end as sum_dlt " +
                    " From t_delmin a, t_delplu b " +
                    " Where a.nzp_kvar = b.nzp_kvar " +
                    "   and a.nzp_serv = b.nzp_serv " +
                    " Into temp t_ins_ch1 With no log "
                    , true);
#endif
                    if (!ret.result) return ret;

                    ExecSQL(conn_db, " Create index ix_t_ins_ch1_1 on t_ins_ch1 (nzp_min) ", true);
                    ExecSQL(conn_db, " Create index ix_t_ins_ch1_2 on t_ins_ch1 (nzp_plu) ", true);
#if PG
                    ExecSQL(conn_db, " analyze t_ins_ch1 ", true);
#else
 ExecSQL(conn_db, " Update statistics for table t_ins_ch1 ", true);
#endif

                    bool b = false;

                    ret = ExecRead(conn_db, out reader,
                        " Select count(*) as cnt From t_ins_ch1  "
                        , true);
                    if (!ret.result) return ret;

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
                        ret.result = false;
                        ret.text = ex.Message;
                        return ret;
                    }
                    finally
                    {
                        reader.Close();
                        reader.Dispose();
                    }


                    if (b)
                    {
                        //есть суммы для перекидки
                        ret = ExecSQL(conn_db,
                        " Update t_perek " +
                        " Set sum_del = sum_del + ( Select sum_dlt From t_ins_ch1 Where t_perek.nzp_charge = t_ins_ch1.nzp_min ) " +
                        " Where nzp_charge in ( Select nzp_min From t_ins_ch1 ) "
                        , true);
                        if (!ret.result) return ret;

                        ret = ExecSQL(conn_db,
                        " Update t_perek " +
                        " Set sum_del = sum_del - ( Select sum_dlt From t_ins_ch1 Where t_perek.nzp_charge = t_ins_ch1.nzp_plu ) " +
                        " Where nzp_charge in ( Select nzp_plu From t_ins_ch1 ) "
                        , true);
                        if (!ret.result) return ret;

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
                        " Where nzp_charge in ( Select nzp_charge From t_perek ) "
                        , true);
                    if (!ret.result) return ret;

                    ret = ExecSQL(conn_db,
                        " Update " + chargeXX.charge_xx +
                        " Set sum_money = money_to + money_from + money_del " +
                           " ,sum_outsaldo = sum_insaldo +  real_charge + sum_real + reval + izm_saldo - (money_to + money_from + money_del) " +
                        " Where nzp_charge in ( Select nzp_charge From t_perek ) "
                        , true);
                    if (!ret.result) return ret;

                    //загнать следы в del_supplier
#if PG
                    ret = ExecSQL(conn_db,
                                            " Insert into " + chargeXX.paramcalc.pref + "_charge_" + (chargeXX.paramcalc.calc_yy - 2000).ToString("00") + ".del_supplier" +
                                              " (num_ls, nzp_serv, nzp_supp, sum_prih, kod_sum, dat_account, dat_calc) " +
                                            " Select num_ls, nzp_serv, nzp_supp, sum_del, 39, " + MDY(chargeXX.paramcalc.calc_mm, 28, chargeXX.paramcalc.calc_yy) + ", current_date " +
                                            " From t_perek " +
                                            " Where abs(sum_del) > 0.000001 "
                                            , true);
#else
                    ret = ExecSQL(conn_db,
                        " Insert into " + chargeXX.paramcalc.pref + "_charge_" + (chargeXX.paramcalc.calc_yy - 2000).ToString("00") + ":del_supplier" +
                          " (num_ls, nzp_serv, nzp_supp, sum_prih, kod_sum, dat_account, dat_calc) " +
                        " Select num_ls, nzp_serv, nzp_supp, sum_del, 39, MDY(" + chargeXX.paramcalc.calc_mm + ",28," + chargeXX.paramcalc.calc_yy + "), today " +
                        " From t_perek " +
                        " Where abs(sum_del) > 0.000001 "
                        , true);
#endif
                    if (!ret.result) return ret;
                }
            }
            finally
            {
                ExecSQL(conn_db, " Drop table t_perek ", false);
                ExecSQL(conn_db, " Drop table t_ins_ch1 ", false);
                ExecSQL(conn_db, " Drop table t_delmin ", false);
                ExecSQL(conn_db, " Drop table t_delplu ", false);
                ExecSQL(conn_db, " Drop table t_ins_ch1 ", false);
            }

            return ret;
        }


        /// <summary>
        /// Выполняет учет оплат для заданной пачки оплат
        /// </summary>
        /// <param name="conn_db"></param>
        /// <param name="packXX"></param>
        /// <returns></returns>
        protected Returns CalcChargeXXUchetOplatForPack(IDbConnection conn_db, PackXX packXX)
        {
            if (Constants.Trace) Utility.ClassLog.WriteLog("Старт функции CalcChargeXXUchetOplatForPack");

            Returns ret = Utils.InitReturns();

            List<string> prefs = new List<string>();
            if (packXX.paramcalc.pref == "")
            {
#if PG
                string sql = "select distinct pref from " + Points.Pref + "_data.kvar k, " + Points.Pref + "_fin_" + (packXX.paramcalc.calc_yy % 100).ToString("00") + ".pack_ls p" +
                                    " where p.num_ls = k.num_ls and p.nzp_pack = " + packXX.nzp_pack;
#else
                string sql = "select unique pref from " + Points.Pref + "_data:kvar k, " + Points.Pref + "_fin_" + (packXX.paramcalc.calc_yy % 100).ToString("00") + ":pack_ls p" +
                    " where p.num_ls = k.num_ls and p.nzp_pack = " + packXX.nzp_pack;
#endif
                MyDataReader reader = null;
                ret = ExecRead(conn_db, out reader, sql, true);
                if (!ret.result) return ret;

                try
                {
                    while (reader.Read())
                    {
                        if (reader["pref"] != DBNull.Value) prefs.Add(Convert.ToString(reader["pref"]).Trim());
                    }
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = ex.Message;
                    MonitorLog.WriteLog("Ошибка в функции CalcChargeXXUchetOplatForPack\n" + ex.Message, MonitorLog.typelog.Error, true);
                    return ret;
                }
                finally
                {
                    if (reader != null) reader.Close();
                }
            }
            else prefs.Add(packXX.paramcalc.pref);

            foreach (string pref in prefs)
            {
                DropTempTablesPack(conn_db);
                packXX.paramcalc.pref = pref;
                packXX.paramcalc.nzp_pack_saldo = packXX.nzp_pack;
                
                packXX.paramcalc.nzp_kvar = 0; //остальные параметры по-умолчанию, на всякий случай
                packXX.paramcalc.nzp_dom = 0;
                packXX.paramcalc.b_reval = false;
                packXX.paramcalc.b_must = false;

                ChoiseTempKvar(conn_db, ref packXX.paramcalc, false, out ret);
                if (!ret.result) return ret;

                ret = CalcChargeXXUchetOplat(conn_db, packXX.paramcalc);
                if (!ret.result) return ret;
            }

            if (Constants.Trace) Utility.ClassLog.WriteLog("Стоп функции CalcChargeXXUchetOplatForPack");

            return ret;
        }

        /// <summary>
        /// Выполняет учет оплат для заданного лицевого счета
        /// </summary>
        /// <param name="conn_db"></param>
        /// <param name="packXX"></param>
        /// <returns></returns>
        public Returns CalcChargeXXUchetOplatForLs(IDbConnection conn_db, IDbTransaction transaction, Charge finder)
        {
            if (Constants.Trace) Utility.ClassLog.WriteLog("Старт функции CalcChargeXXUchetOplatForLs");

            if (finder.nzp_kvar < 1 && finder.num_ls < 1)
            {
                return new Returns(false, "Не задан лицевой счет");
            }

            DropTempTablesPack(conn_db);
            
            Returns ret;
            if (finder.nzp_kvar < 1)
            {
#if PG
                string sql = "select nzp_kvar from " + Points.Pref + "_data.kvar where num_ls = " + finder.num_ls;
#else
string sql = "select nzp_kvar from " + Points.Pref + "_data:kvar where num_ls = " + finder.num_ls;
#endif
                MyDataReader reader;
                ret = ExecRead(conn_db, out reader, sql, true);
                if (!ret.result) return ret;
                if (!reader.Read())
                {
                    reader.Close();
                    ret.result = false;
                    ret.text = "Лицевой счет " + finder.num_ls + " не найден";
                    return ret;
                }
                finder.nzp_kvar = Convert.ToInt32(reader["nzp_kvar"]);
                reader.Close();
            }

            ParamCalc paramCalc = new ParamCalc(finder.nzp_kvar, 0, finder.pref, finder.year_, finder.month_, finder.year_, finder.month_);

            paramCalc.b_reval = false;
            paramCalc.b_must = false;

            ChoiseTempKvar(conn_db, ref paramCalc, false, out ret);
            if (!ret.result) return ret;

            ret = CalcChargeXXUchetOplat(conn_db, paramCalc);
            if (!ret.result) return ret;

            if (Constants.Trace) Utility.ClassLog.WriteLog("Стоп функции CalcChargeXXUchetOplatForLs");

            return ret;
        }

        /// <summary>
        /// Выполняет учет оплат для заданной Управляющая организация
        /// </summary>
        /// <param name="conn_db"></param>
        /// <param name="packXX"></param>
        /// <returns></returns>
        public Returns CalcChargeXXUchetOplatForArea(IDbConnection conn_db, IDbTransaction transaction, Charge finder)
        {
            if (Constants.Trace) Utility.ClassLog.WriteLog("Старт функции CalcChargeXXUchetOplatForArea");

            //if (finder.nzp_area < 1)
            //{
            //    return new Returns(false, "Не задана Управляющая организация");
            //}
            
            Returns ret = new Returns();

            #region получить список префиксов
            //------------------------------------------------------------------------------------------------------------------------------------
            List<string> prefs = new List<string>();
            if (finder.pref == "")
            {
#if PG
                string sql = "select distinct pref from " + Points.Pref + "_data.kvar k " + (finder.nzp_area > 0 ? " where k.nzp_area = " + finder.nzp_area : "");
#else
                string sql = "select unique pref from " + Points.Pref + "_data:kvar k " + (finder.nzp_area > 0 ? " where k.nzp_area = " + finder.nzp_area : "");
#endif
                MyDataReader reader = null;
                ret = ExecRead(conn_db, out reader, sql, true);
                if (!ret.result) return ret;

                try
                {
                    while (reader.Read())
                    {
                        if (reader["pref"] != DBNull.Value) prefs.Add(Convert.ToString(reader["pref"]).Trim());
                    }
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = ex.Message;
                    MonitorLog.WriteLog("Ошибка в функции CalcChargeXXUchetOplatForArea\n" + ex.Message, MonitorLog.typelog.Error, true);
                    return ret;
                }
                finally
                {
                    if (reader != null) reader.Close();
                }
            }
            else prefs.Add(finder.pref);
            //------------------------------------------------------------------------------------------------------------------------------------
            #endregion

            #region
            //------------------------------------------------------------------------------------------------------------------------------------
            foreach (string pref in prefs)
            {
                DropTempTablesPack(conn_db);
                ParamCalc paramCalc = new ParamCalc(0, 0, pref, finder.year_, finder.month_, finder.year_, finder.month_);
                paramCalc.nzp_area = finder.nzp_area;

                paramCalc.b_reval = false;
                paramCalc.b_must = false;

                ChoiseTempKvar(conn_db, ref paramCalc, false, out ret);
                if (!ret.result) return ret;

                ret = CalcChargeXXUchetOplat(conn_db, paramCalc);
                if (!ret.result) return ret;
            }
            //------------------------------------------------------------------------------------------------------------------------------------
            #endregion

            if (Constants.Trace) Utility.ClassLog.WriteLog("Стоп функции CalcChargeXXUchetOplatForArea");

            return ret;
        }
    }
}
