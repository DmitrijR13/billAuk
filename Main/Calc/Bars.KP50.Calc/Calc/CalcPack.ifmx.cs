using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Globalization;

using STCLINE.KP50.Global;
using STCLINE.KP50.IFMX.Kernel.source.CommonType;
using STCLINE.KP50.Interfaces;
using STCLINE.KP50.DataBase;
using System.Data;

namespace STCLINE.KP50.DataBase    
{
    //здась находятся классы для распределения оплат
    public partial class DbCalc : DbCalcClient
    {
        // !!! Добавил Марат 17.12.2012 
        bool isDebug = false; // Признак отладки (формируются постоянные таблицы для распределения t_selkvar, t_opl, t_iopl, t_charge)
        public bool isAutoDistribPaXX = false; // Распределеять автоматически сальдо перечисление при распределении оплат        
        string sqlText = "";  // Вспомогательная переменна для динамической сборки запросов
        #if PG
                string strWherePackLsIsNotDistrib = "  and dat_uchet is null and (coalesce(alg::int,0) =0  or inbasket=1) ";  // Признак того, что оплата не распределена и готова к распределению
        #else
                string strWherePackLsIsNotDistrib = "  and dat_uchet is null and (NVL(alg,0) =0  or inbasket=1) ";  // Признак того, что оплата не распределена и готова к распределению
        #endif
        #if PG
                string strWherePackLsIsDistrib = "  and p.dat_uchet is not null and p.inbasket<>1 and coalesce(p.alg::int,0) <>0 ";  // Признак того, что оплата распределена
        #else
                string strWherePackLsIsDistrib = "  and p.dat_uchet is not null and p.inbasket<>1 and NVL(p.alg,0) <>0 ";  // Признак того, что оплата распределена
        #endif
        string strKodSumForCharge_MM="23,33,41,50";  // Ежемесячные квитанции 
        string strKodSumForCharge_X = "57,55,83,93,94";   // Специальные формы квитанций

        string field_for_etalon = "sum_charge";
        string field_for_etalon_descr = "Начислено к оплате";

        bool isDistributionForInSaldo = false;  // Распределение по исходящему сальдо
        int pack_type = 10; // Тип платежа  (pack_type: 10- средства РЦ,  20 - чужие средства)
        int nzp_supp = 0;   // Код поставщика для чужих площадей    
        //---------------------------------------------------
        public struct PackXX
        //---------------------------------------------------
        {
            public CalcTypes.ParamCalc paramcalc;

            public string fn_pa_tab;
            public string fn_pa_xx
            {
                get
                {
                    if (is_local)
                    #if PG
                        return paramcalc.pref + "_charge_" + (paramcalc.calc_yy - 2000).ToString("00") + paramcalc.ol_srv + "." + fn_pa_tab;
                    #else
                        return paramcalc.pref + "_charge_" + (paramcalc.calc_yy - 2000).ToString("00") + paramcalc.ol_srv + ":" + fn_pa_tab;
                    #endif
                    else
                    #if PG
                        return Points.Pref + "_fin_" + (paramcalc.calc_yy - 2000).ToString("00") + paramcalc.ol_srv + "." + fn_pa_tab;
                    #else
                        return Points.Pref + "_fin_" + (paramcalc.calc_yy - 2000).ToString("00") + paramcalc.ol_srv + ":" + fn_pa_tab;
                    #endif
                }
            }
            public string fn_distrib_prev
            {
                get
                {
                    if (paramcalc.DateOper.Day == 1)
                    {
                        if (paramcalc.DateOper.Month == 1)
                        #if PG
                                return Points.Pref + "_fin_" + (paramcalc.calc_yy - 2001).ToString("00") + paramcalc.ol_srv + ".fn_distrib_dom_12";
                        #else
                                return Points.Pref + "_fin_" + (paramcalc.calc_yy - 2001).ToString("00") + paramcalc.ol_srv + ":fn_distrib_dom_12";
                        #endif
                        else
                            #if PG
                                return Points.Pref + "_fin_" + (paramcalc.calc_yy - 2000).ToString("00") + paramcalc.ol_srv + ".fn_distrib_dom_" + (paramcalc.calc_mm - 1).ToString("00");
                            #else
                                return Points.Pref + "_fin_" + (paramcalc.calc_yy - 2000).ToString("00") + paramcalc.ol_srv + ":fn_distrib_dom_" + (paramcalc.calc_mm - 1).ToString("00");
                        #endif
                    }
                    else
                        return fn_distrib;
                }
            }

            public string fn_supplier_tab;
            public string fn_supplier;
            public string fn_operday_log;
            public string fn_distrib_tab;
            public string fn_distrib;
            public string fn_naud;
            public string fn_perc;
            public string charge_xx;
            public string fn_sended;
            public string fn_reval;

            public string s_bank;
            public string s_payer;
            public string pack;
            public string pack_log;
            public string pack_log_tab;

            public string pack_ls;
            public string where_pack_ls;
            public string where_pack;

            public int nzp_pack_ls;


            public int nzp_pack
            {
                get
                {
                    return paramcalc.nzp_pack;
                }
            }

            public bool is_local;
            public bool all_opermonth;
            public string where_dat_oper
            {
                get
                {
                    if (all_opermonth)
                        return paramcalc.between_dat_oper;
                    else
                        return " = " + paramcalc.dat_oper;
                }
            }

            public PackXX(CalcTypes.ParamCalc _paramcalc, int _nzp_pack_ls, bool _local)
            {
                paramcalc = _paramcalc;

                all_opermonth = false;

                nzp_pack_ls = _nzp_pack_ls;
                is_local = _local;

                fn_supplier_tab = "fn" + paramcalc.alias + "_supplier" + paramcalc.calc_mm.ToString("00");
                #if PG
                        fn_supplier = paramcalc.pref + "_charge_" + (paramcalc.calc_yy - 2000).ToString("00") + paramcalc.ol_srv + "." + fn_supplier_tab;
                        charge_xx = paramcalc.pref + "_charge_" + (paramcalc.calc_yy - 2000).ToString("00") + ".charge_" + paramcalc.calc_mm.ToString("00");
                        pack = Points.Pref + "_fin_" + (paramcalc.calc_yy - 2000).ToString("00") + paramcalc.ol_srv + ".pack ";
                        pack_ls = Points.Pref + "_fin_" + (paramcalc.calc_yy - 2000).ToString("00") + paramcalc.ol_srv + ".pack_ls ";
                #else
                        fn_supplier = paramcalc.pref + "_charge_" + (paramcalc.calc_yy - 2000).ToString("00") + paramcalc.ol_srv + ":" + fn_supplier_tab;
                        charge_xx = paramcalc.pref + "_charge_" + (paramcalc.calc_yy - 2000).ToString("00") + ":charge_" + paramcalc.calc_mm.ToString("00");
                        pack = Points.Pref + "_fin_" + (paramcalc.calc_yy - 2000).ToString("00") + paramcalc.ol_srv + ":pack ";
                        pack_ls = Points.Pref + "_fin_" + (paramcalc.calc_yy - 2000).ToString("00") + paramcalc.ol_srv + ":pack_ls ";
                #endif

                pack_log_tab = "pack_log";
                #if PG
                    pack_log = Points.Pref + "_fin_" + (paramcalc.calc_yy - 2000).ToString("00") + paramcalc.ol_srv + "." + pack_log_tab;
                    fn_operday_log = Points.Pref + "_fin_" + (paramcalc.calc_yy - 2000).ToString("00") + paramcalc.ol_srv + ".fn_operday_dom_mc";
                #else
                    pack_log = Points.Pref + "_fin_" + (paramcalc.calc_yy - 2000).ToString("00") + paramcalc.ol_srv + ":" + pack_log_tab;
                    fn_operday_log = Points.Pref + "_fin_" + (paramcalc.calc_yy - 2000).ToString("00") + paramcalc.ol_srv + ":fn_operday_dom_mc";
                #endif
                fn_pa_tab = "fn_pa_dom_" + (paramcalc.calc_mm).ToString("00");

                #if PG
                        fn_perc = Points.Pref + "_fin_" + (paramcalc.calc_yy - 2000).ToString("00") + paramcalc.ol_srv + ".fn_perc_dom";
                        fn_naud = Points.Pref + "_fin_" + (paramcalc.calc_yy - 2000).ToString("00") + paramcalc.ol_srv + ".fn_naud_dom";
                        fn_sended = Points.Pref + "_fin_" + (paramcalc.calc_yy - 2000).ToString("00") + paramcalc.ol_srv + ".fn_sended_dom";
                        fn_reval = Points.Pref + "_fin_" + (paramcalc.calc_yy - 2000).ToString("00") + paramcalc.ol_srv + ".fn_reval_dom";
                #else
                        fn_perc = Points.Pref + "_fin_" + (paramcalc.calc_yy - 2000).ToString("00") + paramcalc.ol_srv + ":fn_perc_dom";
                        fn_naud = Points.Pref + "_fin_" + (paramcalc.calc_yy - 2000).ToString("00") + paramcalc.ol_srv + ":fn_naud_dom";
                        fn_sended = Points.Pref + "_fin_" + (paramcalc.calc_yy - 2000).ToString("00") + paramcalc.ol_srv + ":fn_sended_dom";
                        fn_reval = Points.Pref + "_fin_" + (paramcalc.calc_yy - 2000).ToString("00") + paramcalc.ol_srv + ":fn_reval_dom";
                #endif

                fn_distrib_tab = "fn_distrib_dom_" + (paramcalc.calc_mm).ToString("00");
                #if PG
                    fn_distrib = Points.Pref + "_fin_" + (paramcalc.calc_yy - 2000).ToString("00") + paramcalc.ol_srv + "." + fn_distrib_tab;
                    s_bank = Points.Pref + "_kernel.s_bank ";
                    s_payer = Points.Pref + "_kernel.s_payer ";
                #else
                    fn_distrib = Points.Pref + "_fin_" + (paramcalc.calc_yy - 2000).ToString("00") + paramcalc.ol_srv + ":" + fn_distrib_tab;
                    s_bank = Points.Pref + "_kernel:s_bank ";
                    s_payer = Points.Pref + "_kernel:s_payer ";
                #endif

                where_pack_ls = "nzp_pack_ls > 0";
                if (nzp_pack_ls > 0)
                    where_pack_ls = "nzp_pack_ls = " + nzp_pack_ls;

                where_pack = "nzp_pack > 0";
                if (nzp_pack > 0)
                    where_pack = "nzp_pack = " + nzp_pack;

            }
        }

        //--------------------------------------------------------------------------------
        void DropTempTablesPack(IDbConnection conn_db)
        //--------------------------------------------------------------------------------
        {
            ExecSQL(conn_db, " Drop table t_selkvar ", false);
            ExecSQL(conn_db, " Drop table t_charge ", false);
            ExecSQL(conn_db, " Drop table t_itog ", false);
            ExecSQL(conn_db, " Drop table t_ostatok ", false);
            ExecSQL(conn_db, " Drop table t_opl ", false);
            ExecSQL(conn_db, " Drop table t_gil_sums ", false);
            ExecSQL(conn_db, " Drop table t_iopl ", false);
        }

        /// <summary>
        /// Добавляет сообщение об операциях с пачкой оплат или оплатой
        /// </summary>
        /// <param name="conn_db"></param>
        /// <param name="packXX"></param>
        /// <param name="msg"></param>
        /// <param name="err"></param>
        /// <param name="ret"></param>
        public void MessageInPackLog(IDbConnection conn_db, PackXX packXX, string msg, bool err, out Returns ret)
        {
            ret = MessageInPackLog(conn_db, null, new PackLog()
            {
                tableName = packXX.pack_log,
                nzp_pack = packXX.nzp_pack,
                nzp_pack_ls = packXX.nzp_pack_ls,
                dat_oper = packXX.paramcalc.DateOper.ToShortDateString(),
                nzp_wp = packXX.paramcalc.nzp_wp,
                message = msg,
                err = err
            });
        }

        /// <summary>
        /// Процедура записи в лог оплат
        /// </summary>
        /// <param name="conn_db">подключение к базе</param>
        /// <param name="tran">транзакция</param>
        /// <param name="pLog"></param>
        /// <returns></returns>
        public Returns MessageInPackLog(IDbConnection connection, IDbTransaction transaction, PackLog pLog)
        {
            return ExecSQL(connection, transaction,
                " Insert into " + pLog.tableName +
                    " (nzp_pack,nzp_pack_ls,dat_oper,dat_log,nzp_wp, txt_log,tip_log) " +
                " Values ( " + pLog.nzp_pack + "," +
                               pLog.nzp_pack_ls + "," +
            #if PG
                                "'" + pLog.dat_oper + "', now(), " +
            #else
                                "'" + pLog.dat_oper + "', current, " +
            #endif
                               pLog.nzp_wp + ", " +
                               Utils.EStrNull(pLog.message) + "," +
                               (pLog.err ? "1" : "0") + " ) "
                , true);
        }

        //-----------------------------------------------------------------------------
        void CreatePackLog(IDbConnection conn_db2, PackXX packXX, out Returns ret)
        //-----------------------------------------------------------------------------
        {
            ret = Utils.InitReturns();

            if (TempTableInWebCashe(conn_db2, packXX.pack_log))
            {

                return;
            }

            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result)
            {
                ret.result = false;
                return;
            }

            #if PG
            ret = ExecSQL(conn_db, " set search_path to '" + Points.Pref + "_kernel " + Points.Pref + "_fin_" + (packXX.paramcalc.calc_yy - 2000).ToString("00") + "'", true);
            #else
                ret = ExecSQL(conn_db, " Database " + Points.Pref + "_fin_" + (packXX.paramcalc.calc_yy - 2000).ToString("00"), true);
            #endif

            #if PG
                ret = ExecSQL(conn_db,
                           " Create table "+
                #if PG
                 " "
                #else
                " are." 
                #endif
                           + packXX.pack_log_tab +
                           " (  nzp_plog serial not null, " +
                              " nzp_pack integer default 0, " +
                              " nzp_pack_ls integer default 0, " +
                              " nzp_wp integer default 0, " +
                              " dat_oper date, " +
                              " dat_log timestamp, " +
                              " txt_log char(255), " +
                              " tip_log integer default 0 ) "
                                 , true);
             #else
                    ret = ExecSQL(conn_db,
                    " Create table are." + packXX.pack_log_tab +
                    " (  nzp_plog serial not null, " +
                   " nzp_pack integer default 0, " +
                   " nzp_pack_ls integer default 0, " +
                   " nzp_wp integer default 0, " +
                   " dat_oper date, " +
                   " dat_log datetime year to minute, " +
                   " txt_log char(255), " +
                   " tip_log integer default 0 ) "
                      , true);
            #endif
            if (!ret.result)
            {
                conn_db.Close();
                return;
            }

            ret = ExecSQL(conn_db, " create unique index  " +
#if PG
 ""
#else
"are."
#endif
 + "ix1_" + packXX.pack_log_tab + " on " + packXX.pack_log_tab + " (nzp_plog) ", true);
            if (!ret.result)
            {
                conn_db.Close();
                return;
            }
            ret = ExecSQL(conn_db, " create index  " +
#if PG
 ""
#else
"are."
#endif
 + "ix2_" + packXX.pack_log_tab + " on " + packXX.pack_log_tab + " (nzp_pack, nzp_wp) ", true);
            if (!ret.result)
            {
                conn_db.Close();
                return;
            }
            ret = ExecSQL(conn_db, " create index  " +
#if PG
 ""
#else
"are."
#endif
 + "ix3_" + packXX.pack_log_tab + " on " + packXX.pack_log_tab + " (nzp_pack_ls) ", true);
            if (!ret.result)
            {
                conn_db.Close();
                return;
            }
            ret = ExecSQL(conn_db, " create index  " +
#if PG
 ""
#else
"are."
#endif
 + "ix4_" + packXX.pack_log_tab + " on " + packXX.pack_log_tab + " (dat_oper,tip_log) ", true);
            if (!ret.result)
            {
                conn_db.Close();
                return;
            }
            ret = ExecSQL(conn_db, " create index  " +
#if PG
 ""
#else
"are."
#endif
 + "ix5_" + packXX.pack_log_tab + " on " + packXX.pack_log_tab + " (nzp_wp) ", true);
            if (!ret.result)
            {
                conn_db.Close();
                return;
            }

#if PG
            ExecSQL(conn_db, " analyze " + packXX.pack_log_tab, true);
            ret = ExecSQL(conn_db, "set search_path to '" + Points.Pref + "_kernel'", true);
#else
ExecSQL(conn_db, " update statistics for table " + packXX.pack_log_tab, true);
            ret = ExecSQL(conn_db, " Database " + Points.Pref + "_kernel", true); 
#endif

            conn_db.Close();
        }

        //распределение пачки
        public bool CalcPackXX(CalcTypes.ParamCalc paramcalc, bool isManualDistribution, out Returns ret)
        {
            string conn_kernel = Points.GetConnByPref(paramcalc.pref);
            IDbConnection conn_db = GetConnection(conn_kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result)
            {
                return false;
            }

            bool result = CalcPackXX(conn_db, paramcalc, isManualDistribution, out ret);

            conn_db.Close();

            return result;
        }

        public bool CalcPackXX(IDbConnection conn_db, CalcTypes.ParamCalc paramcalc, bool isManualDistribution, out Returns ret)
        //-----------------------------------------------------------------------------
        {
            return CalcPackXX(conn_db, paramcalc, 0, true, isManualDistribution, out ret);
        }


        //-----------------------------------------------------------------------------
        bool CreateSelKvar(IDbConnection conn_db, PackXX packXX, out Returns ret)
        //-----------------------------------------------------------------------------
        {
            return CreateSelKvar(conn_db, packXX, false, out ret);
        }
        //-----------------------------------------------------------------------------
        bool CreateSelKvar(IDbConnection conn_db, PackXX packXX, bool cur_dat_uchet, out Returns ret)
        //-----------------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            Returns r = Utils.InitReturns();

            //выбрать мно-во лицевых счетов
            #if PG
                sqlText = " Create temp table t_selkvar " +
                          " ( nzp_key     serial not null, " +
                          "   nzp_kvar    integer, " +
                          "   num_ls      integer, " +
                          "   nzp_pack_ls integer, " +
                          "   kod_sum     integer, " +
                          "   nzp_wp      integer, " +
                          "   nzp_dom     integer, " +
                          "   nzp_pack    integer, " +
                          "   nzp_area    integer, " +
                          "   nzp_geu     integer, " +
                          "   dat_uchet   date, " +
                          "   dat_month   date, " +
                          "   distr_month date, " +
                          "   dat_vvod    date, " +
                          "   id_bill     integer," +
                          "   is_open     integer default 1," +
                          "   sum_etalon  numeric(14,2) default 0," +
                          "   alg         integer default 0," +
                          "   g_sum_ls    numeric(14,2) default 0 " +
                          " )  ";
          #else
                sqlText = " Create temp table t_selkvar " +
                " ( nzp_key     serial not null, " +
                "   nzp_kvar    integer, " +
                "   num_ls      integer, " +
                "   nzp_pack_ls integer, " +
                "   kod_sum     integer, " +
                "   nzp_wp      integer, " +
                "   nzp_dom     integer, " +
                "   nzp_pack    integer, " +
                "   nzp_area    integer, " +
                "   nzp_geu     integer, " +
                "   dat_uchet   date, " +
                "   dat_month   date, " +
                "   distr_month date, " +
                "   dat_vvod    date, " +
                "   id_bill     integer,"+
                "   is_open     integer default 1," +
                "   sum_etalon  decimal(14,2) default 0," +
                "   alg         integer default 0," +
                "   g_sum_ls    decimal(14,2) default 0 " +
                " ) With no log ";
          #endif
          if (isDebug)
          {
               
                #if PG
                     sqlText = sqlText.Replace("temp table", "table");

                #else
                    sqlText = sqlText.Replace("temp table", "table");
                    sqlText = sqlText.Replace("With no log", "");
                #endif
            }

            ret = ExecSQL(conn_db,sqlText, true);
            if (!ret.result)
            {
                DropTempTablesPack(conn_db);
                MessageInPackLog(conn_db, packXX, "Ошибка выборки 1.1", true, out r);
                return false;
            }

            string s_dat_uchet = " "; //0 - по умолчанию

            if (cur_dat_uchet) 
                s_dat_uchet = " and p.dat_uchet " + packXX.where_dat_oper; //в текущем операционном дне или месяце

            #if PG
                sqlText =
                            " Insert into t_selkvar (nzp_kvar, num_ls, nzp_wp, nzp_dom, nzp_area, nzp_geu, nzp_pack, nzp_pack_ls, kod_sum, dat_uchet, dat_month, dat_vvod, g_sum_ls, id_bill)  " +
                            " Select k.nzp_kvar, k.num_ls, " + packXX.paramcalc.nzp_wp + ", k.nzp_dom, k.nzp_area, k.nzp_geu, p.nzp_pack, p.nzp_pack_ls, p.kod_sum, p.dat_uchet, p.dat_month, p.dat_vvod, p.g_sum_ls, p.id_bill  " +
                            " From " + packXX.paramcalc.pref + "_data.kvar k, " + packXX.pack_ls + " p " +
                            " Where k.num_ls = p.num_ls " +
                                s_dat_uchet + //фильтрация по dat_uchet
                            "   and k." + packXX.paramcalc.where_z +
                            "   and p." + packXX.where_pack +
                            "   and p." + packXX.where_pack_ls;
            #else
                sqlText =
                    " Insert into t_selkvar (nzp_kvar, num_ls, nzp_wp, nzp_dom, nzp_area, nzp_geu, nzp_pack, nzp_pack_ls, kod_sum, dat_uchet, dat_month, dat_vvod, g_sum_ls, id_bill)  " +
                    " Select k.nzp_kvar, k.num_ls, " + packXX .paramcalc.nzp_wp+ ", k.nzp_dom, k.nzp_area, k.nzp_geu, p.nzp_pack, p.nzp_pack_ls, p.kod_sum, p.dat_uchet, p.dat_month, p.dat_vvod, p.g_sum_ls, p.id_bill  " +
                    " From " + packXX.paramcalc.pref + "_data:kvar k, " + packXX.pack_ls + " p " +
                    " Where k.num_ls = p.num_ls " +
                        s_dat_uchet + //фильтрация по dat_uchet
                    "   and k." + packXX.paramcalc.where_z +
                    "   and p." + packXX.where_pack +
                    "   and p." + packXX.where_pack_ls;
            #endif
            ret = ExecSQL(conn_db, sqlText , true);
            if (!ret.result)
            {
                DropTempTablesPack(conn_db);
                MessageInPackLog(conn_db, packXX, "Ошибка выборки 1.2", true, out r);
                return false;
            }

            ExecSQL(conn_db, " Create unique index ix1_t_selkvar on t_selkvar (nzp_key) ", true);
            ExecSQL(conn_db, " Create        index ix2_t_selkvar on t_selkvar (nzp_kvar) ", true);
            ExecSQL(conn_db, " Create        index ix3_t_selkvar on t_selkvar (num_ls) ", true);
            ExecSQL(conn_db, " Create        index ix4_t_selkvar on t_selkvar (nzp_pack_ls) ", true);            
            #if PG
                ExecSQL(conn_db, " analyze t_selkvar ", true);
            #else
                ExecSQL(conn_db, " Update statistics for table t_selkvar ", true);
            #endif
            return true;
        }
        //-----------------------------------------------------------------------------
        public bool CalcPackXX(IDbConnection conn_db, CalcTypes.ParamCalc paramcalc, int nzp_pack_ls, bool checkPackDistrb, bool isManualDistribution, out Returns ret)
        //-----------------------------------------------------------------------------
        {
            if ((paramcalc.nzp_pack == 0) && (nzp_pack_ls == 0))
            {
                if (Constants.Trace) Utility.ClassLog.WriteLog("Отказано в распределении из-за отсутствия кодов пачки и кода квитанции ");
                ret = new Returns(false,"При распределении не указана оплата (оплаты)",-1) ;
                return false;
            }
            if (Constants.Trace) Utility.ClassLog.WriteLog("Старт функции CalcPackXX для пачки " + paramcalc.nzp_pack);

            //isManualDistribution = true;
            //Constants.Trace = false;

            DropTempTablesPack(conn_db); 

           //isDebug = true;
            
            if (Constants.Trace)
            {
                Utility.ClassLog.WriteLog("Включён режим отладки");
                //isDebug = false;
            }

            //Points.packDistributionParameters.strategy = PackDistributionParameters.Strategies.NoPriority;
            if ( (Points.IsSmr)||(Points.Region==Regions.Region.Belgorodskaya_obl) ) //признак Самары
            {
               Points.packDistributionParameters.strategy = PackDistributionParameters.Strategies.Samara;
            }
            
            if (Points.packDistributionParameters.strategy == PackDistributionParameters.Strategies.Samara)
            {
                isDistributionForInSaldo=true;
            };

            if (nzp_pack_ls > 0) 
            { 
               Points.packDistributionParameters.EnableLog = true;
            }

            //Points.packDistributionParameters.chargeMethod = PackDistributionParameters.ChargeMethods.MonthlyCalculationWithChangesAndOverpayment;
            //Points.packDistributionParameters.chargeMethod = PackDistributionParameters.ChargeMethods.MonthlyCalculationWithChanges ;
            //Points.packDistributionParameters.chargeMethod = PackDistributionParameters.ChargeMethods.MonthlyCalculationWithChangesAndOverpayment ; 
            //Points.packDistributionParameters.chargeMethod = PackDistributionParameters.ChargeMethods.PositiveMonthlyCalculationWithChanges ; 
            //Points.packDistributionParameters.chargeMethod = PackDistributionParameters.ChargeMethods.PositiveMonthlyCalculationWithChangesAndOverpayment ;
            //Points.packDistributionParameters.chargeMethod = PackDistributionParameters.ChargeMethods.Outsaldo ;
            //Points.packDistributionParameters.chargeMethod = PackDistributionParameters.ChargeMethods.PositiveOutsaldo;
            

            int count;
            int count_err=0;
            int count_special = 0;
            DataTable dt;
            DataRow row;
            string s_okr="0";

            int nzp_user = paramcalc.nzp_user; // !!! Потом определить код пользователя 

            ret = Utils.InitReturns();
            Returns r = Utils.InitReturns();

            PackXX packXX = new PackXX(paramcalc, nzp_pack_ls, true);


            CreatePackLog(conn_db, packXX, out ret);
            if (!ret.result)
            {
                return false;
            }

            MessageInPackLog(conn_db, packXX, "Начало распределения", false, out ret);
            Returns ret2 = new Returns();
            ret2.tag = 0;

            IDataReader reader;
            bool b;
            //checkPackDistrb = false;
            //для начала проверим, что пачка не распределена (если надо)
            if (checkPackDistrb)
            {
                ret = ExecRead(conn_db, out reader,
                    " Select nzp_pack From " + packXX.pack +
                    " Where nzp_pack = " + packXX.nzp_pack                   
                    , true);
                if (!ret.result)
                {
                    return false;
                }
                b = true;
                try
                {
                    if (reader.Read())
                    {
                        b = false;
                    }
                    else
                        ret.text = "Пачка распределена или не найдена (код пачки="+packXX.nzp_pack+") !";
                }
                catch (Exception ex)
                {
                    ret.text = ex.Message;
                }
                reader.Close();

                if (b)
                {
                    ret.result = false;
                    MonitorLog.WriteLog(ret.text, MonitorLog.typelog.Error, 1, 222, true);
                    MessageInPackLog(conn_db, packXX, ret.text, true, out r);
                    return false;
                }
            }

            string distr_month;
            string distr_month2;

            var dat_month_local = Points.GetCalcMonth(new CalcMonthParams(paramcalc.pref));
            RecordMonth dat_month_local_prev;
            
            dat_month_local_prev.year_ = dat_month_local.year_;
            dat_month_local_prev.month_ = dat_month_local.month_;

            dat_month_local_prev.month_ = dat_month_local.month_ -1;
            if (dat_month_local_prev.month_ == 0)
            {
                dat_month_local_prev.year_ = dat_month_local.year_ - 1;
                dat_month_local_prev.month_ = 12;
            }


           
            string baseName;
            string baseName_t;
            
            if (isDistributionForInSaldo)
            {
                field_for_etalon = "sum_insaldo";
                field_for_etalon_descr = "Входящее сальдо";
                baseName = paramcalc.pref + "_charge_" + (dat_month_local.year_ - 2000).ToString("00") + tableDelimiter + "charge_" + dat_month_local.month_.ToString("00");
                baseName_t = paramcalc.pref + "_charge_" + (dat_month_local.year_ - 2000).ToString("00") + tableDelimiter + "charge_" + dat_month_local.month_.ToString("00");
                distr_month = "01." + dat_month_local.month_.ToString("00") + "." + (dat_month_local.year_ ).ToString("00");
                distr_month2 = distr_month;

                //distr_month = "01." + paramcalc.calc_mm.ToString("00") + "." + (paramcalc.calc_yy ).ToString("00");
                //distr_month2 = "01." + paramcalc.calc_mm.ToString("00") + "." + (paramcalc.calc_yy ).ToString("00");

            }
            else
            {
                baseName = paramcalc.pref + "_charge_" + (dat_month_local_prev.year_ - 2000).ToString("00") + tableDelimiter + "charge_" + dat_month_local_prev.month_.ToString("00");
                baseName_t = paramcalc.pref + "_charge_" + (dat_month_local.year_ - 2000).ToString("00") + tableDelimiter + "charge_" + dat_month_local.month_.ToString("00");
                distr_month = "01." + dat_month_local_prev.month_.ToString("00") + "." + (dat_month_local_prev.year_ ).ToString("00");
                distr_month2 = distr_month;

                //distr_month = "01." + paramcalc.prev_calc_mm.ToString("00") + "." + (paramcalc.prev_calc_yy ).ToString("00");
                //distr_month2 = "01." + paramcalc.calc_mm.ToString("00") + "." + (paramcalc.calc_yy ).ToString("00");

            }

            MessageInPackLog(conn_db, packXX, "Сальдовый месяц для распределения: " + distr_month, false, out r);
    
            // Получить тип оплаты  (pack_type: 10 - деньги РЦ, 20 - Чужие деньги)
             sqlText=
                    " Select pack_type, nzp_supp From " + packXX.pack +
                    " Where nzp_pack = " + packXX.nzp_pack;                    
            DataTable dtpack_type = ClassDBUtils.OpenSQL(sqlText, conn_db).GetData();
            DataRow row_pack_type;
            row_pack_type = dtpack_type.Rows[0];
            if (row_pack_type["pack_type"] !=DBNull.Value)
            {
                pack_type=Convert.ToInt32(row_pack_type["pack_type"]);                
                if (pack_type==20)
                {
                    packXX.fn_supplier=packXX.fn_supplier.Replace(packXX.fn_supplier_tab,"from_supplier");
                }
            }
            if (row_pack_type["nzp_supp"] != DBNull.Value)
            {
                nzp_supp = Convert.ToInt32(row_pack_type["nzp_supp"]);
            }
            else
            {
                nzp_supp = 0;
            }
            // ---------------- Если ручное распределенеи оплаты (распределение уже было произведено в gil_sums) ---------------------------------------------------------
            if ((isManualDistribution) && (packXX.nzp_pack_ls > 0))
            {
                //----------------------- СОХРАНЕНИЕ -----------------------
                sqlText=  " Select  p.kod_sum,  p.dat_vvod, p.dat_month, p.g_sum_ls, k.nzp_dom " +
                  " From " +  packXX.pack_ls + " p, " +Points.Pref+"_data" + DBManager.tableDelimiter + "kvar k "+
                  " Where p.nzp_pack_ls =  " + packXX.nzp_pack_ls.ToString() + " and p.num_ls = k.num_ls";                   
            
                string dat_vvod="";                
                int kod_sum=0;
                decimal g_sum_ls=0;
                int nzp_dom = 0;

                DataTable dtpack_ls = ClassDBUtils.OpenSQL(sqlText, conn_db).GetData();
                DataRow row_pack_ls;
                row_pack_ls = dtpack_ls.Rows[0];

                if (row_pack_ls["kod_sum"] != null)
                {
                    #region  Загрузить  исходные данные по квитанции
                    kod_sum = Convert.ToInt32(row_pack_ls["kod_sum"]);
                    dat_vvod = Convert.ToDateTime(row_pack_ls["dat_vvod"]).ToShortDateString();
                    g_sum_ls=Convert.ToDecimal(row_pack_ls["g_sum_ls"]);
                    nzp_dom = Convert.ToInt32(row_pack_ls["nzp_dom"]); ;

                    /*
                    if (row_pack_ls["dat_month"] != DBNull.Value)
                    {
                        distr_month = Convert.ToDateTime(row_pack_ls["dat_month"]).ToShortDateString();
                    }
                    else
                    {
                        distr_month = "01." + paramcalc.prev_calc_mm.ToString("00") + "." + paramcalc.prev_calc_yy;
                    }
                    */ 

                    if (pack_type==20)  // Чужие средства
                    {
                            string tableSupplier = paramcalc.pref + "_charge_" + (paramcalc.calc_yy.ToString("yyyy").Substring(2, 2)) + tableDelimiter+ "from_supplier";
                    } else
                    {
                            string tableSupplier = paramcalc.pref + "_charge_" + (paramcalc.calc_yy.ToString("yyyy").Substring(2, 2)) + tableDelimiter + "fn_supplier" + paramcalc.calc_mm.ToString("00");
                    }

                    string tableGilsums = Points.Pref + "_fin_" + (paramcalc.calc_yy - 2000).ToString("00") + paramcalc.ol_srv + tableDelimiter + "gil_sums a";

                    #endregion Загрузить  исходные данные по квитанции                 
                   
                        if (pack_type == 20)  // Чужие средства
                        {
                            sqlText =
                            " Insert into " + packXX.fn_supplier + " (  num_ls, nzp_pack_ls, nzp_serv, nzp_supp, sum_prih, kod_sum, dat_month, dat_prih, dat_uchet) " +
                            " Select  a.num_ls, a.nzp_pack_ls, a.nzp_serv, a.nzp_supp, a.sum_oplat," + kod_sum + ",a.dat_month, '" + dat_vvod + "', " + packXX.paramcalc.dat_oper + 
                            " From  " + tableGilsums +
                            " Where  a.nzp_pack_ls = " + packXX.nzp_pack_ls.ToString() + " and sum_oplat <>0 ";
                        }
                        else
                        {
                            sqlText =
                            " Insert into " + packXX.fn_supplier + " (  num_ls, nzp_pack_ls, nzp_serv, nzp_supp, sum_prih, kod_sum, dat_month, dat_prih, dat_uchet, s_dolg, s_user, s_forw ) " +
                            " Select  a.num_ls, a.nzp_pack_ls, a.nzp_serv, a.nzp_supp, a.sum_oplat," + kod_sum + ",a.dat_month, '" + dat_vvod + "', " + packXX.paramcalc.dat_oper + ", 0, a.sum_oplat, 0 " +
                            " From  " + tableGilsums +
                            " Where  a.nzp_pack_ls = " + packXX.nzp_pack_ls.ToString() + " and sum_oplat <>0 ";
                        }

                    ret = ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log).GetReturnsType().GetReturns();
                    if (!ret.result)
                    {
                        ret.result = false;
                        MonitorLog.WriteLog(ret.text, MonitorLog.typelog.Error, 1, 222, true);
                        MessageInPackLog(conn_db, packXX, "Ошибка сохранения ручного распределения 2.7", true, out r);
                        return false;
                    }
                 
                    #region Проверить был ли полностью выполнено ручное распределение
                    
                    DataTable dtgil_sums = ClassDBUtils.OpenSQL("select sum(sum_oplat) as sum_oplat, MIN(dat_month) as distr_month from " + tableGilsums+" where nzp_pack_ls =  " + packXX.nzp_pack_ls, conn_db).GetData();
                    DataRow row_gil_sums;
                    decimal sum_oplat_total=0;
                    row_gil_sums = dtgil_sums.Rows[0];                    
                    if (row_gil_sums["sum_oplat"] != null)
                    {
                        sum_oplat_total=Convert.ToDecimal(row_gil_sums["sum_oplat"]);
                        if (sum_oplat_total > g_sum_ls)
                        {
                            MessageInPackLog(conn_db, packXX, "Ошибка сохранения ручного распределения 2.7", true, out r);
                            // Нет распределённых оплат
                            ret.text = "Распределение не было произведено. Сумма ручного распределения " + sum_oplat_total + " руб превосходит сумму оплаты " + g_sum_ls;
                            ret.tag = -1;
                            ret.result = false;
                            return true;

                        }
                        if (row_gil_sums["distr_month"] != null)
                        {
                            if (Convert.ToString(row_gil_sums["distr_month"]).Trim() != "")
                            {
                                distr_month = Convert.ToDateTime(row_gil_sums["distr_month"]).ToShortDateString();
                            }
                        }
                    } else
                    {
                        sum_oplat_total=0;
                    }
                    float dlt0 = Decimal.ToSingle(Math.Abs(sum_oplat_total - g_sum_ls));
                    if (dlt0 > 0.001)
                    {
                        MessageInPackLog(conn_db, packXX, "Ошибка сохранения ручного распределения 2.7", true, out r);
                        // Нет распределённых оплат
                        ret.text = "Распределение не было произведено. Сумма ручного распределения "+sum_oplat_total+" руб не соответствует оплате "+g_sum_ls;
                        ret.tag = -1;                        
                        ret.result=false;
                        return true;

                    }

                    #endregion Проверить был ли полностью выполнено ручное распределение

                    sqlText =
                        " Update " + packXX.pack_ls +
                        " Set dat_uchet = " + packXX.paramcalc.dat_oper +
                        #if PG
                            ", date_distr = now(),  inbasket =0, alg = 5, nzp_user = " + nzp_user +
                        #else
                            ", date_distr = current,  inbasket =0, alg = 5, nzp_user = " + nzp_user +
                        #endif
                            ", distr_month = '" + distr_month + "'" +
                        " Where nzp_pack_ls = "+packXX.nzp_pack_ls.ToString();
                    IntfResultType retres = ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log);
                    ret = retres.GetReturnsType().GetReturns();
                    if (!ret.result)
                    {
                        ret.result = false;
                        MonitorLog.WriteLog(ret.text, MonitorLog.typelog.Error, 1, 222, true);
                        MessageInPackLog(conn_db, packXX, "Ошибка фиксирования ручного распределения 2.8", true, out r);
                        return false;
                    }
                    #if PG
                        count = retres.resultAffectedRows;
                    #else
                        count = ClassDBUtils.GetAffectedRowsCount(conn_db);
                    #endif
                    if (count == 0 && count_special == 0)
                    {
                        // Нет распределённых оплат
                        ret.text = "Распределение не было произведено";
                        ret.tag = -1;
                        return true;
                    } else
                    {
                        //сразу запустить расчет сальдо по nzp_pack
                        MessageInPackLog(conn_db, packXX, "Расчет сальдо лицевых счетов", false, out r);
                        ret = CalcChargeXXUchetOplatForPack(conn_db, packXX);
                        if (!ret.result)
                        {
                            DropTempTablesPack(conn_db);
                            return false;
                        }

                        #region Сохранить показания приборов учёта
                        SaveCountersValsFromPackls(conn_db, nzp_user, paramcalc, packXX.nzp_pack_ls, out ret);
                        #endregion Сохранить показания приборов учёта
                    }


                    // Сохранить признак о том, что нужно пересчитать дом в сальдо по перечислениям
                    Save_In_fn_operday_dom_mc(conn_db, 0, 0, nzp_dom, out ret);

                } else
                {
                    ret.result = false;
                    MonitorLog.WriteLog(ret.text, MonitorLog.typelog.Error, 1, 222, true);
                    MessageInPackLog(conn_db, packXX, "Ошибка сохранения ручного распределения 2.11", true, out r);
                    return false;
                }                             
            }
            else
            {   // ---------------- Если автоматическое распределение оплаты согласно заложенным алгоритмам ---------------------------------------------------------

                DropTempTablesPack(conn_db);
                // Получить лицевые счета для распределения
                packXX.where_pack_ls = packXX.where_pack_ls + strWherePackLsIsNotDistrib;  // !!! Марат 17.12.2012.  Распределять только такие оплаты
                if (packXX.paramcalc.pref.Trim() == "")
                {
                    return false;
                }
                CreateSelKvar(conn_db, packXX, out ret);
                if   (!ret.result) 
                {
                    return false;
                }

                //----------------------- ПРОВЕРКИ -----------------------
                //проверим, что dat_uchet пустые
                ret = ExecRead(conn_db, out reader,
                    " Select num_ls From t_selkvar a Where dat_uchet is not null  "
                    , true);
                if (!ret.result)
                {
                    return false;
                }
                b = false;
                try
                {
                    if (reader.Read())
                    {
                        ret.text = "Лицевые счета были уже распределены (#1)!";
                        b = true;
                    }
                }
                catch (Exception ex)
                {
                    ret.text = ex.Message;
                }
                reader.Close();
                if (b)
                {
                    ret.result = false;
                    MonitorLog.WriteLog(ret.text, MonitorLog.typelog.Error, 1, 222, true);
                    MessageInPackLog(conn_db, packXX, ret.text, true, out r);
                    return false;
                }

                //----------------------- РАСПРЕДЕЛЕНИЕ -----------------------
                //выберем основания для распределения - сальдо текущего месяца
                #if PG
                    sqlText = " Create temp table t_charge " +
                                    " ( nzp_key     serial not null, " +
                                    "   nzp_kvar    integer, " +
                                    "   num_ls      integer, " +
                                    "   nzp_pack_ls numeric(14,0) default 0, " +
                                    "   nzp_serv    integer, " +
                                    "   nzp_supp    integer, " +
                                    "   real_pere     numeric(14,2) default 0, " +
                                    "   sum_insaldo   numeric(14,2) default 0, " +
                                    "   sum_insaldop  numeric(14,2) default 0, " +
                                    "   real_charge   numeric(14,2) default 0, " +
                                    "   sum_tarif     numeric(14,2) default 0, " +
                                    "   c_calc        numeric(14,2) default 0, " +
                                    "   sum_charge_prev    numeric(14,2) default 0, " +
                                    "   sum_outsaldo_prev     numeric(14,2) default 0, " +
                    // !!! Добавлены поля, которые нужны мне (Марату) для распределения. 17.12.2012
                                    "   rsum_tarif    numeric(14,2) default 0, " +
                                    "   sum_charge    numeric(14,2) default 0, " +
                                    "   sum_money     numeric(14,2) default 0, " +
                                    "   sum_outsaldo  numeric(14,2) default 0, " +
                                    "   isum_charge_isdel_1  numeric(14,2) default 0, " +
                                    "   isum_charge_isdel_0  numeric(14,2) default 0, " +
                                    "   distr_month date," +
                                    "   isdel         integer default 0 " +
                                    " )  ";
                #else
                        sqlText = " Create temp table t_charge " +
                    " ( nzp_key     serial not null, " +
                    "   nzp_kvar    integer, " +
                    "   num_ls      integer, " +
                    "   nzp_pack_ls decimal(14,0) default 0, " +
                    "   nzp_serv    integer, " +
                    "   nzp_supp    integer, " +
                    "   real_pere     decimal(14,2) default 0, " +
                    "   sum_insaldo   decimal(14,2) default 0, " +
                    "   sum_insaldop  decimal(14,2) default 0, " +
                    "   real_charge   decimal(14,2) default 0, " +
                    "   sum_tarif     decimal(14,2) default 0, " +
                    "   c_calc        decimal(14,2) default 0, " +
                    "   sum_charge_prev    decimal(14,2) default 0, " +
                    "   sum_outsaldo_prev     decimal(14,2) default 0, " +
                    // !!! Добавлены поля, которые нужны мне (Марату) для распределения. 17.12.2012
                    "   rsum_tarif    decimal(14,2) default 0, " +
                    "   sum_charge    decimal(14,2) default 0, " +
                    "   sum_money     decimal(14,2) default 0, " +
                    "   sum_outsaldo  decimal(14,2) default 0, " +
                    "   isum_charge_isdel_1  decimal(14,2) default 0, " +
                    "   isum_charge_isdel_0  decimal(14,2) default 0, " +
                    "   distr_month date," +
                    "   isdel         integer default 0 " +
                    " ) With no log ";
                #endif
                if (isDebug)
                {
                   
                    #if PG
                         sqlText = sqlText.Replace("temp table", "table");                  
                    #else
                         sqlText = sqlText.Replace("temp table", "table");
                         sqlText = sqlText.Replace("With no log", "");
                    #endif
                }

                ret = ExecSQL(conn_db, sqlText, true);
                if (!ret.result)
                {
                    DropTempTablesPack(conn_db);
                    MessageInPackLog(conn_db, packXX, "Ошибка распределения 1.3", true, out r);
                    return false;
                }
                //string baseName;
                sqlText = " update t_selkvar set distr_month =  '" + distr_month + "'";
                ret = ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log).GetReturnsType().GetReturns();
                if (!ret.result)
                {
                    DropTempTablesPack(conn_db);
                    MessageInPackLog(conn_db, packXX, "Ошибка распределения 1.4.1.5", true, out r);
                    return false;
                }

                #if PG
                    sqlText = " select p.bd_kernel, p.nzp_wp from " + Points.Pref + "_kernel.s_point p where p.nzp_wp in ( select distinct nzp_wp from  t_selkvar) ";
                #else
                    sqlText = " select p.bd_kernel, p.nzp_wp from " + Points.Pref + "_kernel:s_point p where p.nzp_wp in ( select distinct nzp_wp from  t_selkvar) ";
                #endif
                
                               

                // Предварительно очистить ранее распределённые оплаты по выбранным квитанциям
                if (paramcalc.pref != Points.Pref)
                {
                    if (pack_type == 20)  // Чужие средства
                    {
                                sqlText =
                                    " delete from  " + paramcalc.pref + "_charge_" + (paramcalc.calc_yy - 2000).ToString("00") + tableDelimiter + "from_supplier" +
                                    " where nzp_pack_ls in (select k.nzp_pack_ls From t_selkvar k )";
                    }
                    else
                    {
                                sqlText =
                                    " delete from  " + paramcalc.pref + "_charge_" + (paramcalc.calc_yy - 2000).ToString("00") + tableDelimiter + "fn_supplier" + paramcalc.calc_mm.ToString("00") +
                                    " where nzp_pack_ls in (select k.nzp_pack_ls From t_selkvar k )";
                    }

                    ret = ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log).GetReturnsType().GetReturns();
                    if (!ret.result)
                    {
                        DropTempTablesPack(conn_db);
                        MessageInPackLog(conn_db, packXX, "Ошибка распределения 1.4.1.2", true, out r);
                        return false;
                    }
                }
                else
                {
                    sqlText =
                        " Update " + packXX.pack_ls +
                        " Set inbasket = 1, dat_uchet = null, alg =0, nzp_user = " + nzp_user +
                        " Where nzp_pack =  " + packXX.nzp_pack +
                        "   and num_ls = 0  and inbasket = 0  ";
                    ret = ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log).GetReturnsType().GetReturns();
                    if (!ret.result)
                    {
                        DropTempTablesPack(conn_db);
                        MessageInPackLog(conn_db, packXX, "Ошибка распределения 2.10", true, out r);
                        return false;
                    }

                }

                
                #region Заполнить шаблон для распределения по выбранному банку
                string filterForSupplier="";
                if (nzp_supp >0)
                {
                    filterForSupplier = " and nzp_supp = " + nzp_supp;
                }


                if (isDistributionForInSaldo)
                {
                    
                    sqlText =
                    " Insert into t_charge ( nzp_kvar, nzp_pack_ls, num_ls, nzp_serv, nzp_supp,isdel, " +
                            " real_pere,real_charge,sum_tarif,sum_outsaldo,sum_outsaldo_prev,c_calc,sum_insaldo,sum_insaldop, rsum_tarif, sum_charge,sum_charge_prev, isum_charge_isdel_1, isum_charge_isdel_0 , distr_month)  " +
                    " Select k.nzp_kvar, k.nzp_pack_ls, k.num_ls, c.nzp_serv, c.nzp_supp, c.isdel,  " +
                            " sum(c.real_pere),sum(c.real_charge),sum(c.sum_tarif),sum(c.sum_outsaldo),sum(c.sum_outsaldo),sum(c.c_calc), " +
                            " sum(sum_insaldo), sum(sum_insaldo), " +                            
                            " sum(c.rsum_tarif), sum(c.sum_charge),sum(c."+field_for_etalon+"), " +
                            " sum(case when isdel = 1 and sum_charge>0 then sum_charge else 0 end)," +
                            " sum(case when isdel = 0 and sum_charge>0 then sum_charge else 0 end), " +
                            "'" + distr_month + "' " +
                    " From t_selkvar k, " + baseName + " c " +
                    " Where k.nzp_kvar = c.nzp_kvar " +
                    "   and c.dat_charge is null " +
                    filterForSupplier+
                    "   and c.nzp_serv > 1 " +
                    "   and k.kod_sum in (" + strKodSumForCharge_MM + ") " +
                    " Group by 1,2,3,4,5,6 ";
                } else
                {
                    sqlText =
                    " Insert into t_charge ( nzp_kvar, nzp_pack_ls, num_ls, nzp_serv, nzp_supp,isdel, " +
                            " real_pere,real_charge,sum_tarif,sum_outsaldo,sum_outsaldo_prev,c_calc,sum_insaldo,sum_insaldop, rsum_tarif, sum_charge,sum_charge_prev, isum_charge_isdel_1, isum_charge_isdel_0 , distr_month)  " +
                    " Select k.nzp_kvar, k.nzp_pack_ls, k.num_ls, c.nzp_serv, c.nzp_supp, c.isdel,  " +
                            " sum(c.real_pere),sum(c.real_charge),sum(c.sum_tarif),sum(c.sum_outsaldo),sum(c.sum_outsaldo),sum(c.c_calc), " +
                            " sum(c.sum_insaldo), sum(c.sum_insaldo), " +
                            " sum(c.rsum_tarif), sum(c.sum_charge),sum(c."+field_for_etalon+"), " +
                            " sum(case when isdel = 1 and sum_charge>0 then sum_charge else 0 end)," +
                            " sum(case when isdel = 0 and sum_charge>0 then sum_charge else 0 end), " +
                            "'" + distr_month + "' " +
                    " From t_selkvar k, " + baseName + " c " +
                    " Where k.nzp_kvar = c.nzp_kvar " +
                    "   and c.dat_charge is null " +
                    filterForSupplier +
                    "   and c.nzp_serv > 1 " +
                    "   and k.kod_sum in (" + strKodSumForCharge_MM + ") " +
                    " Group by 1,2,3,4,5,6 ";

                }
                ret = ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log).GetReturnsType().GetReturns();
                if (!ret.result)
                {
                    DropTempTablesPack(conn_db);
                    MessageInPackLog(conn_db, packXX, "Ошибка распределения 1.4.1.1", true, out r);
                    return false;
                }

                // Распределение по предварительным счетам (авансовым)
                    sqlText =
                    " Insert into t_charge ( nzp_kvar, nzp_pack_ls, num_ls, nzp_serv, nzp_supp,isdel, " +
                            " real_pere,real_charge,sum_tarif,sum_outsaldo,sum_outsaldo_prev,c_calc,sum_insaldo,sum_insaldop, rsum_tarif, sum_charge,sum_charge_prev, isum_charge_isdel_1, isum_charge_isdel_0 , distr_month)  " +
                    " Select k.nzp_kvar, k.nzp_pack_ls, k.num_ls, c.nzp_serv, c.nzp_supp, c.isdel,  " +
                            " sum(c.real_pere),sum(c.real_charge),sum(c.sum_tarif),sum(c.sum_outsaldo),sum(c.sum_outsaldo),sum(c.c_calc), " +
                            " sum(sum_insaldo), sum(sum_insaldo), " +                            
                            " sum(c.rsum_tarif), sum(c.sum_charge),sum(c."+field_for_etalon+"), " +
                            " sum(case when isdel = 1 and sum_charge>0 then sum_charge else 0 end)," +
                            " sum(case when isdel = 0 and sum_charge>0 then sum_charge else 0 end), " +
                            "'" + distr_month + "' " +
                    " From t_selkvar k, " + baseName_t.Trim() + "_T c " +
                    " Where k.nzp_kvar = c.nzp_kvar " +
                    "   and c.dat_charge is null " +
                    filterForSupplier+
                    "   and c.nzp_serv > 1 " +
                    "   and k.kod_sum in (81) " +
                    " Group by 1,2,3,4,5,6 ";
                    ret = ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log).GetReturnsType().GetReturns();
                    if (!ret.result)
                    {
                        //DropTempTablesPack(conn_db);
                        //MessageInPackLog(conn_db, packXX, "Отсутствуют структуры CHARGE_MM_T", true, out r);
                        //return false;
                    }

                #endregion Заполнить шаблон для распределения по выбранному банку
                /*
                #if PG
                    ret = ClassDBUtils.ExecSQL("update " + Points.Pref + "_fin_" + (Points.DateOper.Year % 100).ToString("00") + ".pack_ls set distr_month = '" + distr_month + "' where nzp_pack_ls = " + packXX.nzp_pack_ls, conn_db, ClassDBUtils.ExecMode.Log).GetReturnsType().GetReturns();
                #else
                    ret = ClassDBUtils.ExecSQL("update " + Points.Pref + "_fin_" + (Points.DateOper.Year % 100).ToString("00") + "@" + DBManager.getServer(conn_db) + tableDelimiter+"pack_ls set distr_month = '" + distr_month + "' where nzp_pack_ls = " + packXX.nzp_pack_ls, conn_db, ClassDBUtils.ExecMode.Log).GetReturnsType().GetReturns();
                #endif*/
                ret = ClassDBUtils.ExecSQL("update " + Points.Pref + "_fin_" + (Points.DateOper.Year % 100).ToString("00") + "@" + DBManager.getServer(conn_db) + tableDelimiter+"pack_ls set distr_month = '" + distr_month + "' "+sConvToDate+ " where nzp_pack_ls = " + packXX.nzp_pack_ls, conn_db, ClassDBUtils.ExecMode.Log).GetReturnsType().GetReturns();
                
                sqlText = " update t_selkvar set distr_month =  '" + distr_month + "'";
                ret = ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log).GetReturnsType().GetReturns();
                if (!ret.result)
                {
                        DropTempTablesPack(conn_db);
                        MessageInPackLog(conn_db, packXX, "Ошибка распределения 1.4.1.5", true, out r);
                        return false;
                }
                #if PG
                    sqlText =
                    " update t_selkvar   set is_open = 0 where t_selkvar.nzp_kvar not in (  " +
                    " Select k.nzp  " +
                    " From " + paramcalc.pref + "_data.prm_3 k " +
                    " Where k.nzp = t_selkvar.nzp_kvar  AND k.nzp_prm = 51 and k.val_prm = '1' and '01." + (Points.DateOper.Month % 100).ToString("00") + "." + (Points.DateOper.Year).ToString("00") + "' between k.dat_s and k.dat_po ) ";
                #else
                    sqlText =
                    " update t_selkvar   set is_open = 0 where t_selkvar.nzp_kvar not in (  " +
                    " Select k.nzp  " +
                    " From " + paramcalc.pref + "_data:prm_3 k " +
                    " Where k.nzp = t_selkvar.nzp_kvar  AND k.nzp_prm = 51 and k.val_prm = '1' and '01." + (Points.DateOper.Month % 100).ToString("00") + "." + (Points.DateOper.Year).ToString("00") + "' between k.dat_s and k.dat_po ) ";
                #endif
                ret = ExecSQL(conn_db, sqlText, true);
                if (!ret.result)
                {
                    DropTempTablesPack(conn_db);
                    MessageInPackLog(conn_db, packXX, "Ошибка выборки 1.3", true, out r);
                    return false;
                }


                //Удалить сообщения об ошибках, которые были ранее сохраены
                sqlText =
                    " delete from " + (packXX.pack_ls).Trim() + "_err  " +
                    " where nzp_pack_ls in (select a.nzp_pack_ls from t_selkvar a)  ";
                ret = ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log).GetReturnsType().GetReturns();
                if (!ret.result)
                {
                    DropTempTablesPack(conn_db);
                    MessageInPackLog(conn_db, packXX, "Ошибка распределения 1.4.2", true, out r);
                    return false;
                }

                //определить ранее распределённые оплаты по услугам 
                sqlText =
                    " update t_charge set sum_money = (select sum(sum_prih) from   " + packXX.fn_supplier + " a " +
                    " where t_charge.num_ls = a.num_ls and t_charge.nzp_serv = a.nzp_serv and t_charge.nzp_supp = a.nzp_supp )  ";
                ret = ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log).GetReturnsType().GetReturns();
                if (!ret.result)
                {
                    DropTempTablesPack(conn_db);
                    MessageInPackLog(conn_db, packXX, "Ошибка распределения 1.4.2", true, out r);
                    return false;
                }
                //рассчитать новые суммы sum_charge и sum_outsaldo, sum_insaldo по услугам 
                #if PG
                    sqlText =
                    " update t_charge set sum_insaldop =sum_insaldo , sum_insaldo = sum_insaldo-coalesce(sum_money,0), "+
                    " sum_outsaldo_prev =sum_outsaldo , sum_outsaldo = sum_outsaldo-coalesce(sum_money,0), sum_charge_prev=sum_charge, "+
                    " sum_charge= sum_charge- coalesce(sum_money,0),isum_charge_isdel_0= case when isum_charge_isdel_0<>0 then "+
                    " isum_charge_isdel_0- coalesce(sum_money,0) else 0 end,isum_charge_isdel_1= case when isum_charge_isdel_1<>0 "+
                    " then isum_charge_isdel_1- coalesce(sum_money,0) else 0 end  ";
                #else
                    sqlText =
                        " update t_charge set sum_insaldop =sum_insaldo , sum_insaldo = sum_insaldo-NVL(sum_money,0), sum_outsaldo_prev =sum_outsaldo , sum_outsaldo = sum_outsaldo-NVL(sum_money,0), sum_charge_prev=sum_charge, sum_charge= sum_charge- NVL(sum_money,0),isum_charge_isdel_0= case when isum_charge_isdel_0<>0 then  isum_charge_isdel_0- NVL(sum_money,0) else 0 end,isum_charge_isdel_1= case when isum_charge_isdel_1<>0 then isum_charge_isdel_1- NVL(sum_money,0) else 0 end  ";
                #endif
                #if PG
                        IntfResultType retres = ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log);
                        ret = retres.GetReturnsType().GetReturns();               
                #else
                        ret = ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log).GetReturnsType().GetReturns();
                #endif
                if (!ret.result)
                {
                    DropTempTablesPack(conn_db);
                    MessageInPackLog(conn_db, packXX, "Ошибка распределения 1.4.2", true, out r);
                    return false;
                }
                #if PG
                    count = retres.resultAffectedRows;
                #else
                    count = ClassDBUtils.GetAffectedRowsCount(conn_db);
                #endif

                if (Points.packDistributionParameters.EnableLog & count > 0 & packXX.nzp_pack_ls > 0)
                {
                    string s1;
                  //  count = ClassDBUtils.GetAffectedRowsCount(conn_db);
                    if (Points.packDistributionParameters.EnableLog & count > 0 & packXX.nzp_pack_ls > 0)
                    {
                        sqlText = "select sum(sum_money) as sum_ from t_charge ";
                        dt = ClassDBUtils.OpenSQL(sqlText, conn_db).GetData();
                        row = dt.Rows[0];
                        s1 = Convert.ToString(row["sum_"]);
                        if (s1.Trim() == "")
                        {
                            s1 = "0.00";
                        }
                        if ((s1 != "0.00") & (s1 != "0"))
                        {
                            MessageInPackLog(conn_db, packXX, "Ранее распределено по лицевому счёту: " + s1 + " руб", false, out r);
                            sqlText = "select sum(sum_charge) as sum_charge, sum(sum_insaldo) as sum_insaldo, sum( case when sum_charge>0 then sum_charge else 0 end) as sum_charge_plus,sum(sum_outsaldo) as sum_outsaldo, sum(case when sum_outsaldo>0 then sum_outsaldo else 0 end) as sum_outsaldo_plus  from t_charge ";
                            dt = ClassDBUtils.OpenSQL(sqlText, conn_db).GetData();
                            row = dt.Rows[0];

                            if (isDistributionForInSaldo)
                            {
                                s1 = Convert.ToString(row["sum_insaldo"]);
                                if (s1.Trim() == "")
                                {
                                    s1 = "0.00";
                                }
                                if ((s1 != "0.00") & (s1 != "0"))
                                {
                                        MessageInPackLog(conn_db, packXX, "Откорректирована сумма 'Входящее сальдо' с учётом ранее поступивших платежей: " + s1 + " руб", false, out r);
                                }
                            }
                            else
                            {
                                s1 = Convert.ToString(row["sum_outsaldo"]);
                                if (s1.Trim() == "")
                                {
                                    s1 = "0.00";
                                }
                                if ((s1 != "0.00") & (s1 != "0"))
                                {
                                     MessageInPackLog(conn_db, packXX, "Откорректирована сумма 'Исходящее сальдо' с учётом ранее поступивших платежей: " + s1 + " руб", false, out r);
                                }                                
                           }
                           s1 = Convert.ToString(row["sum_charge"]);
                           if (s1.Trim() == "")
                           {
                                s1 = "0.00";
                           }
                           if ((s1 != "0.00") & (s1 != "0"))
                           {
                                MessageInPackLog(conn_db, packXX, "Откорректирована сумма 'Начислено к оплате' с учётом ранее поступивших платежей: " + s1 + " руб", false, out r);
                           }
                        }
                    }
                }

                // Определить итоговую сумму эталона к распределению
                #if PG
                    sqlText =
                    " update t_selkvar set sum_etalon =  coalesce((select sum(" + field_for_etalon + ") from t_charge where t_charge.nzp_pack_ls = t_selkvar.nzp_pack_ls),0) " +
                    "   where t_selkvar.kod_sum in (" + strKodSumForCharge_MM + ",81) ";
                #else
                    sqlText =
                    " update t_selkvar set sum_etalon =  nvl((select sum(" + field_for_etalon + ") from t_charge where t_charge.nzp_pack_ls = t_selkvar.nzp_pack_ls),0) " +
                    "   where t_selkvar.kod_sum in (" + strKodSumForCharge_MM + ",81) ";
                #endif
                ret = ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log).GetReturnsType().GetReturns();
                if (!ret.result)
                {
                    DropTempTablesPack(conn_db);
                    MessageInPackLog(conn_db, packXX, "Ошибка определения итоговой суммы эталона к распределению", true, out r);
                    return false;
                }

                ExecSQL(conn_db, " Create unique index ix1_t_charge on t_charge (nzp_key) ", true);
                ExecSQL(conn_db, " Create unique index ix2_t_charge on t_charge (nzp_kvar,nzp_pack_ls,nzp_serv,nzp_supp) ", true);
                ExecSQL(conn_db, " Create unique index ix3_t_charge on t_charge (num_ls,nzp_pack_ls,nzp_serv,nzp_supp) ", true);
                #if PG
                    ExecSQL(conn_db, " analyze t_charge ", true);
                #else
                    ExecSQL(conn_db, " Update statistics for table t_charge ", true);
                #endif
                //итоговые начисления
                #if PG
                    sqlText = " Select c.nzp_kvar, k.nzp_pack_ls, " +
                            " sum(c.real_pere) real_pere,sum(c.real_charge) real_charge,sum(c.sum_tarif) sum_tarif, " +
                            " sum(c.sum_outsaldo) sum_outsaldo,sum(c.c_calc) c_calc, sum(c.sum_insaldo) sum_insaldo, sum(c.sum_insaldop) sum_insaldop,sum(case when c.rsum_tarif>0 then c.rsum_tarif else 0 end ) rsum_tarif, " +
                            " sum(c.sum_charge) sum_charge, sum(c.isum_charge_isdel_1) isum_charge_isdel_1,sum(c.isum_charge_isdel_0) isum_charge_isdel_0 " +
                            "  Into temp t_itog "+
                    " From t_charge c, t_selkvar k where k.nzp_kvar = c.nzp_kvar and k.nzp_pack_ls = c.nzp_pack_ls " +
                    " Group by 1,2 ";
                #else
                    sqlText = " Select c.nzp_kvar, k.nzp_pack_ls, " +
                            " sum(c.real_pere) real_pere,sum(c.real_charge) real_charge,sum(c.sum_tarif) sum_tarif, " +
                            " sum(c.sum_outsaldo) sum_outsaldo,sum(c.c_calc) c_calc, sum(c.sum_insaldo) sum_insaldo, sum(c.sum_insaldop) sum_insaldop,sum(case when c.rsum_tarif>0 then c.rsum_tarif else 0 end ) rsum_tarif, " +
                            " sum(c.sum_charge) sum_charge, sum(c.isum_charge_isdel_1) isum_charge_isdel_1,sum(c.isum_charge_isdel_0) isum_charge_isdel_0 " +
                    " From t_charge c, t_selkvar k where k.nzp_kvar = c.nzp_kvar and k.nzp_pack_ls = c.nzp_pack_ls " +
                    " Group by 1,2 Into temp t_itog With no log ";
                #endif
                if (isDebug)
                {
                    #if PG
                        ret = ExecSQL(conn_db, " Create table t_itog (nzp_kvar integer,nzp_pack_ls numeric(14,0), real_pere numeric(14,2),real_charge numeric(14,2), sum_tarif numeric(14,2), sum_outsaldo numeric(14,2), c_calc numeric(14,2) , sum_insaldo numeric(14,2),  sum_insaldop numeric(14,2),  rsum_tarif numeric(14,2),sum_charge numeric(14,2),isum_charge_isdel_1 numeric(14,2),isum_charge_isdel_0 numeric(14,2) ) ", true);
                        sqlText = sqlText.Replace("Into temp t_itog", "");
                    #else
                        ret = ExecSQL(conn_db, " Create table t_itog (nzp_kvar integer,nzp_pack_ls decimal(14,0), real_pere decimal(14,2),real_charge decimal(14,2), sum_tarif decimal(14,2), sum_outsaldo decimal(14,2), c_calc decimal(14,2) , sum_insaldo decimal(14,2),  sum_insaldop decimal(14,2),  rsum_tarif decimal(14,2),sum_charge decimal(14,2),isum_charge_isdel_1 decimal(14,2),isum_charge_isdel_0 decimal(14,2) ) ", true);
                        sqlText = sqlText.Replace("Into temp t_itog With no log", "");

                    #endif

                    sqlText = "insert into t_itog (nzp_kvar, nzp_pack_ls, real_pere,real_charge,sum_tarif,  sum_outsaldo, c_calc, sum_insaldo, sum_insaldop, rsum_tarif, sum_charge, isum_charge_isdel_1, isum_charge_isdel_0) " + sqlText;
                }
                ret = ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log).GetReturnsType().GetReturns();

                if (!ret.result)
                {
                    DropTempTablesPack(conn_db);
                    MessageInPackLog(conn_db, packXX, "Ошибка распределения 1.5", true, out r);
                    return false;
                }
                ExecSQL(conn_db, " Create unique index ix1_t_itog on t_itog (nzp_kvar, nzp_pack_ls) ", true);
                #if PG
                    ExecSQL(conn_db, " analyze t_itog ", true);
                #else
                    ExecSQL(conn_db, " Update statistics for table t_itog ", true);
                #endif


                //выбрать все цифры для распределения
                #if PG
                        sqlText =
                                    " Create temp table t_opl " +
                                    " ( nzp_key     integer, " +
                                    "   nzp_kvar    integer, " +
                                    "   num_ls      integer, " +
                                    "   nzp_serv    integer, " +
                                    "   nzp_supp    integer, " +
                                    "   sum_tarif     numeric(14,2) default 0, " +
                                    "   isum_tarif    numeric(14,2) default 0, " +
                                    "   sum_outsaldo_prev   numeric(14,2) default 0, " +
                                    "   sum_outsaldo   numeric(14,2) default 0, " +
                                    "   isum_outsaldo  numeric(14,2) default 0, " +
                                    "   rsum_tarif    numeric(14,2) default 0, " +
                                    "   irsum_tarif   numeric(14,2) default 0, " +
                                    "   sum_insaldo   numeric(14,2) default 0, " +
                                    "   sum_insaldop   numeric(14,2) default 0, " +
                                    "   isum_insaldo   numeric(14,2) default 0, " +
                                    "   kod_sum       integer, " +
                                    "   nzp_pack_ls   integer, " +
                                    "   dat_month     date, " +
                                    "   dat_vvod      date, " +
                                    "   g_sum_ls_first  numeric(14,2) default 0, " +
                                    "   g_sum_ls      numeric(14,2) default 0, " +
                                    "   g_sum_ls_ost   numeric(14,2) default 0, " +
                                    "   kod_info      integer, " +
                                    "   sum_prih      numeric(14,2) default 0, " +
                    // !!! Новые поля, необходимые для распределения. Марат 18.12.2012
                                    "   sum_prih_u    numeric(14,2) default 0, " +
                                    "   sum_prih_d    numeric(14,2) default 0, " +
                                    "   sum_prih_a    numeric(14,2) default 0, " +
                                    "   sum_prih_s    numeric(14,2) default 0, " +
                                    "   gil_sums      numeric(14,2) default 0, " +
                                    "   isdel         integer default 0, " +
                                    "   sum_charge_prev   numeric(14,2) default 0, " +
                                    "   sum_charge   numeric(14,2) default 0, " +
                                    "   isum_charge   numeric(14,2) default 0, " +
                                    "   isum_charge_isdel_1  numeric(14,2) default 0, " +
                                    "   znak integer default 1 , " +
                                    "   isum_charge_isdel_0  numeric(14,2) default 0 " +
                                    " )  ";
            #else
                    sqlText =
                    " Create temp table t_opl " +
                    " ( nzp_key     integer, " +
                    "   nzp_kvar    integer, " +
                    "   num_ls      integer, " +
                    "   nzp_serv    integer, " +
                    "   nzp_supp    integer, " +
                    "   sum_tarif     decimal(14,2) default 0, " +
                    "   isum_tarif    decimal(14,2) default 0, " +
                    "   sum_outsaldo_prev   decimal(14,2) default 0, " +
                    "   sum_outsaldo   decimal(14,2) default 0, " +
                    "   isum_outsaldo  decimal(14,2) default 0, " +
                    "   rsum_tarif    decimal(14,2) default 0, " +
                    "   irsum_tarif   decimal(14,2) default 0, " +
                    "   sum_insaldo   decimal(14,2) default 0, " +
                    "   sum_insaldop   decimal(14,2) default 0, " +
                    "   isum_insaldo   decimal(14,2) default 0, " +
                    "   kod_sum       integer, " +
                    "   nzp_pack_ls   integer, " +
                    "   dat_month     date, " +
                    "   dat_vvod      date, " +
                    "   g_sum_ls_first  decimal(14,2) default 0, " +
                    "   g_sum_ls      decimal(14,2) default 0, " +
                    "   g_sum_ls_ost   decimal(14,2) default 0, " +
                    "   kod_info      integer, " +
                    "   sum_prih      decimal(14,2) default 0, " +
                    // !!! Новые поля, необходимые для распределения. Марат 18.12.2012
                    "   sum_prih_u    decimal(14,2) default 0, " +
                    "   sum_prih_d    decimal(14,2) default 0, " +
                    "   sum_prih_a    decimal(14,2) default 0, " +
                    "   sum_prih_s    decimal(14,2) default 0, " +
                    "   gil_sums      decimal(14,2) default 0, " +
                    "   isdel         integer default 0, " +
                    "   sum_charge_prev   decimal(14,2) default 0, " +
                    "   sum_charge   decimal(14,2) default 0, " +
                    "   isum_charge   decimal(14,2) default 0, " +
                    "   isum_charge_isdel_1  decimal(14,2) default 0, " +
                    "   znak integer default 1 , "+
                    "   isum_charge_isdel_0  decimal(14,2) default 0 " +
                    " ) With no log ";
                #endif
                if (isDebug)
                {
                    #if PG
                        sqlText = sqlText.Replace("temp table", "table");
                    #else
                        sqlText = sqlText.Replace("temp table", "table");
                        sqlText = sqlText.Replace("With no log", "");
                    #endif                    
                }
                ret = ExecSQL(conn_db, sqlText, true);
                if (!ret.result)
                {
                    DropTempTablesPack(conn_db);
                    MessageInPackLog(conn_db, packXX, "Ошибка распределения 1.6", true, out r);
                    return false;
                }

                if (isDistributionForInSaldo)
                {
                    sqlText =
                        " Insert into t_opl ( nzp_key,nzp_kvar,num_ls, nzp_serv,nzp_supp,sum_tarif,isum_tarif, sum_insaldop,sum_insaldo, isum_insaldo, rsum_tarif, irsum_tarif, isdel, kod_sum, nzp_pack_ls, dat_month, dat_vvod, g_sum_ls_first,g_sum_ls, " +
                                "  kod_info, sum_prih, sum_charge_prev,sum_charge, isum_charge,isum_charge_isdel_1,isum_charge_isdel_0 ) " +
                        " Select s.nzp_key,s.nzp_kvar,f.num_ls, s.nzp_serv,s.nzp_supp, " +
                                "  s.sum_tarif, i.sum_tarif as isum_tarif,  " +
                                "  s.sum_insaldop as sum_insaldop, s.sum_insaldo as sum_insaldo, i.sum_insaldo as isum_insaldo,s.rsum_tarif, i.rsum_tarif as irsum_tarif, s.isdel, " +
                                "  f.kod_sum, f.nzp_pack_ls, f.dat_month, f.dat_vvod, f.g_sum_ls,f.g_sum_ls, " +
                                "  0 as kod_info, 0.00, s.sum_charge_prev as sum_charge_prev, s.sum_charge as sum_charge,i.sum_charge as isum_charge, i.isum_charge_isdel_1,i.isum_charge_isdel_0 " +
                        " From t_charge s, t_itog i, t_selkvar f " +
                        " Where s.nzp_pack_ls = i.nzp_pack_ls " +
                        "   and s.nzp_pack_ls = f.nzp_pack_ls ";
                }
                else
                {
                    sqlText =
                        " Insert into t_opl ( nzp_key,nzp_kvar,num_ls, nzp_serv,nzp_supp,sum_tarif,isum_tarif, sum_outsaldo_prev,sum_outsaldo, isum_outsaldo, rsum_tarif, irsum_tarif, isdel, kod_sum, nzp_pack_ls, dat_month, dat_vvod, g_sum_ls_first,g_sum_ls, " +
                                "  kod_info, sum_prih, sum_charge_prev,sum_charge, isum_charge,isum_charge_isdel_1,isum_charge_isdel_0 ) " +
                        " Select s.nzp_key,s.nzp_kvar,f.num_ls, s.nzp_serv,s.nzp_supp, " +
                                "  s.sum_tarif, i.sum_tarif as isum_tarif,  " +
                                "  s.sum_outsaldo_prev as sum_outsaldo_prev, s.sum_outsaldo as sum_outsaldo, i.sum_outsaldo as isum_outsaldo,s.rsum_tarif, i.rsum_tarif as irsum_tarif, s.isdel, " +
                                "  f.kod_sum, f.nzp_pack_ls, f.dat_month, f.dat_vvod, f.g_sum_ls,f.g_sum_ls, " +
                                "  0 as kod_info, 0.00, s.sum_charge_prev as sum_charge_prev, s.sum_charge as sum_charge,i.sum_charge as isum_charge, i.isum_charge_isdel_1,i.isum_charge_isdel_0 " +
                        " From t_charge s, t_itog i, t_selkvar f " +
                        " Where s.nzp_pack_ls = i.nzp_pack_ls " +
                        "   and s.nzp_pack_ls = f.nzp_pack_ls ";

                }

                

                ret = ExecSQL(conn_db, sqlText, true);



                if (!ret.result)
                {
                    DropTempTablesPack(conn_db);
                    MessageInPackLog(conn_db, packXX, "Ошибка распределения 1.7", true, out r);
                    return false;
                }
                //DataTable dt4 = ClassDBUtils.OpenSQL("select * from t_charge", conn_db).GetData();
                ExecSQL(conn_db, " Create index ix1_t_opl on t_opl (nzp_key) ", true);
                ExecSQL(conn_db, " Create index ix4_t_opl on t_opl (nzp_pack_ls) ", true);
                #if PG
                    ExecSQL(conn_db, " analyze t_opl ", true);
                #else
                    ExecSQL(conn_db, " Update statistics for table t_opl ", true);
                #endif

                // Установить знак для отрицательных оплат
                sqlText = "update t_opl set g_sum_ls = (-1)* g_sum_ls, g_sum_ls_first = (-1)* g_sum_ls_first, znak = -1 where g_sum_ls<0 ";
                ExecSQL(conn_db, sqlText, true);

                #if PG
                    sqlText =
                        " Create temp table t_gil_sums " +
                        " ( nzp_key     integer, " +
                        "   nzp_pack_ls    integer, " +
                        "   nzp_serv      integer, " +
                        "   nzp_supp    integer, " +
                        "   isdel    integer, " +
                        "   koeff   numeric(15,6), " +
                        "   sum_charge   numeric(14,2) default 0, " +
                        "   isum_charge   numeric(14,2) default 0, " +
                        "   sum_prih  numeric(14,2) default 0, " +
                        "   sum_prih_u  numeric(14,2) default 0 " +
                        " ) ";
                #else
                    sqlText =
                    " Create temp table t_gil_sums " +
                    " ( nzp_key     integer, " +
                    "   nzp_pack_ls    integer, " +
                    "   nzp_serv      integer, " +
                    "   nzp_supp    integer, " +
                    "   isdel    integer, " +
                    "   koeff   decimal(15,6), " +
                    "   sum_charge   decimal(14,2) default 0, " +
                    "   isum_charge   decimal(14,2) default 0, " +
                    "   sum_prih  decimal(14,2) default 0, " +
                    "   sum_prih_u  decimal(14,2) default 0 " +
                    " ) With no log ";
                #endif
                if (isDebug)
                {
                    #if PG
                        sqlText = sqlText.Replace("temp table", "table");
                        //sqlText = sqlText.Replace("With no log", "");
                    #else
                        sqlText = sqlText.Replace("temp table", "table");
                        sqlText = sqlText.Replace("With no log", "");
                    #endif
                }
                ret = ExecSQL(conn_db, sqlText, true);
                if (!ret.result)
                {
                    DropTempTablesPack(conn_db);
                    MessageInPackLog(conn_db, packXX, "Ошибка распределения 1.6.2", true, out r);
                    return false;
                }

                ExecSQL(conn_db, " Create index ix1_t_gil_sums on t_gil_sums (nzp_key) ", true);
                ExecSQL(conn_db, " Create index ix4_t_gil_sums on t_gil_sums (nzp_pack_ls) ", true);
                #if PG
                    ExecSQL(conn_db, " analyze t_gil_sums ", true);
                #else
                    ExecSQL(conn_db, " Update statistics for table t_gil_sums ", true);
                #endif



                //итоговые остатки
                #if PG
                    sqlText = " select nzp_pack_ls, g_sum_ls,g_sum_ls_ost, sum(sum_prih_d+sum_prih_u+sum_prih_a+sum_prih_s) as sum_prih Into temp t_ostatok "+
                        "from t_opl " +" group by 1,2,3  ";
                #else
                    sqlText = " select nzp_pack_ls, g_sum_ls,g_sum_ls_ost, sum(sum_prih_d+sum_prih_u+sum_prih_a+sum_prih_s) as sum_prih from t_opl " +
                        " group by 1,2,3  Into temp t_ostatok With no log ";
                #endif
                if (isDebug)
                {
                    #if PG
                        ret = ExecSQL(conn_db, " create table t_ostatok (nzp_pack_ls integer, g_sum_ls numeric(14,2)  default 0, sum_prih numeric(14,2) default 0, g_sum_ls_ost numeric(14,2) default 0 ) ", true);
                    #else
                        ret = ExecSQL(conn_db, " create table t_ostatok (nzp_pack_ls integer, g_sum_ls decimal(14,2)  default 0, sum_prih decimal(14,2) default 0, g_sum_ls_ost decimal(14,2) default 0 ) ", true);
                    #endif
                    #if PG
                        sqlText = sqlText.Replace("Into temp t_ostatok", "");
                    #else
                        sqlText = sqlText.Replace("Into temp t_ostatok With no log", "");
                    #endif
                    sqlText = "insert into t_ostatok (nzp_pack_ls, g_sum_ls, sum_prih, g_sum_ls_ost) " + sqlText;
                }
                ret = ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log).GetReturnsType().GetReturns();

                if (!ret.result)
                {
                    DropTempTablesPack(conn_db);
                    MessageInPackLog(conn_db, packXX, "Ошибка распределения 1.5", true, out r);
                    return false;
                }
                ExecSQL(conn_db, " Create unique index ix1_t_ostatok on t_ostatok (nzp_pack_ls) ", true);
                #if PG
                    ExecSQL(conn_db, " analyze t_ostatok ", true);
                #else
                    ExecSQL(conn_db, " Update statistics for table t_ostatok ", true);
                #endif

                // Зачесть оплаты в 1 копейку
                #if PG
                    sqlText =
                        " update t_opl set kod_info = 102, sum_prih = g_sum_ls, sum_prih_d = g_sum_ls where kod_info = 0 and g_sum_ls = 0.01 and " +
                        "  nzp_key = (select  MAX(a.nzp_key) from " + Points.Pref + "_kernel.t_charge a where a.nzp_pack_ls =  t_opl.nzp_pack_ls and a.sum_insaldo >0 )";
                #else
                    sqlText =
                        " update t_opl set kod_info = 102, sum_prih = g_sum_ls, sum_prih_d = g_sum_ls where kod_info = 0 and g_sum_ls = 0.01 and " +
                        "  nzp_key = (select  MAX(a.nzp_key) from "+ Points.Pref + "_kernel:t_charge a where a.nzp_pack_ls =  t_opl.nzp_pack_ls and a.sum_insaldo >0 )";
                #endif
                ret = ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log).GetReturnsType().GetReturns();

                //----------------------- ОСНОВАНИЕ -----------------------

                if (isDistributionForInSaldo)
                {
                    OplSamara(conn_db, packXX, out ret); 
                } else
                {
                    StandartReparation(conn_db, packXX, out ret);
                }
                /*
                switch (Points.packDistributionParameters.strategy)
                {
                    case PackDistributionParameters.Strategies.Samara: OplSamara(conn_db, packXX, out ret); break;
                    default: StandartReparation(conn_db, packXX, out ret); break;

                }*/

                //StandartReparation(conn_db, packXX, out ret);
                bool b2 = false;
                b2 = StandardSpecial(conn_db, packXX, out ret);
                if (b2)
                {
                    count_special = 1;
                };

                if (!ret.result)
                {
                    DropTempTablesPack(conn_db);
                    return false;
                }


                //выровним копейки
                #if PG
                    sqlText =
                                   " Select nzp_kvar, nzp_pack_ls, max(case when sum_prih > 0.20 then nzp_key else 0 end) as nzp_key, sum(sum_prih) as sum_prih, max(g_sum_ls_first) as g_sum_ls " +
                                    " Into temp t_iopl "+
                                   " From t_opl " +
                                   " Where kod_info > 0 " +
                                   "   and sum_prih <> 0 and isdel=0 " +
                                   " Group by 1,2 " ;
                #else
                    sqlText =
                        " Select nzp_kvar, nzp_pack_ls, max(case when sum_prih > 0.20 then nzp_key else 0 end) as nzp_key, sum(sum_prih) as sum_prih, max(g_sum_ls_first) as g_sum_ls " +
                        " From t_opl " +
                        " Where kod_info > 0 " +
                        "   and sum_prih <> 0 and isdel=0 " +
                        " Group by 1,2 " +
                        " Into temp t_iopl With no log ";
                #endif
                if (isDebug)
                {
                    #if PG
                        ret = ExecSQL(conn_db, " Create table t_iopl (nzp_kvar  integer, nzp_pack_ls  integer,  nzp_key integer, sum_prih numeric(14,2),  g_sum_ls numeric(14,2)) ", true);
                    #else
                        ret = ExecSQL(conn_db, " Create table t_iopl (nzp_kvar  integer, nzp_pack_ls  integer,  nzp_key integer, sum_prih decimal(14,2),  g_sum_ls decimal(14,2)) ", true);
                    #endif

                    #if PG
                        sqlText = sqlText.Replace("Into temp t_iopl", "");
                    #else
                        sqlText = sqlText.Replace("Into temp t_iopl With no log", "");
                    #endif
                    sqlText = "insert into t_iopl (nzp_kvar, nzp_pack_ls,  nzp_key , sum_prih,  g_sum_ls) " + sqlText;
                }
                #if PG
                    retres = ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log);
                    ret = retres.GetReturnsType().GetReturns();
                #else
                    ret = ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log).GetReturnsType().GetReturns();
                #endif

                if (!ret.result)
                {
                    DropTempTablesPack(conn_db);
                    MessageInPackLog(conn_db, packXX, "Ошибка распределения 2.1", true, out r);
                    return false;
                }
                #if PG
                    count = retres.resultAffectedRows;
                #else
                    count = ClassDBUtils.GetAffectedRowsCount(conn_db);
                #endif
                if (count == 0)
                {
                    ret = ExecSQL(conn_db, " drop table t_iopl ", true);
                    #if PG
                        sqlText =
                            " Select nzp_kvar, nzp_pack_ls, max(case when sum_prih > 0.20 then nzp_key else 0 end) as nzp_key, sum(sum_prih) as sum_prih, max(g_sum_ls_first) as g_sum_ls " +
                            "  Into temp t_iopl "+
                            " From t_opl " +
                            " Where kod_info > 0 " +
                            "   and sum_prih <> 0 and isdel=1 " +
                            " Group by 1,2 ";
                    #else
                        sqlText =
                                        " Select nzp_kvar, nzp_pack_ls, max(case when sum_prih > 0.20 then nzp_key else 0 end) as nzp_key, sum(sum_prih) as sum_prih, max(g_sum_ls_first) as g_sum_ls " +
                                        " From t_opl " +
                                        " Where kod_info > 0 " +
                                        "   and sum_prih <> 0 and isdel=1 " +
                                        " Group by 1,2 " +
                                        " Into temp t_iopl With no log ";
                    #endif
                    if (isDebug)
                    {
                        #if PG
                            ret = ExecSQL(conn_db, " Create table t_iopl (nzp_kvar  integer, nzp_pack_ls  integer,  nzp_key integer, sum_prih numeric(14,2),  g_sum_ls numeric(14,2)) ", true);
                        #else
                            ret = ExecSQL(conn_db, " Create table t_iopl (nzp_kvar  integer, nzp_pack_ls  integer,  nzp_key integer, sum_prih decimal(14,2),  g_sum_ls decimal(14,2)) ", true);
                        #endif
                        #if PG
                            sqlText = sqlText.Replace("Into temp t_iopl ", "");
                        #else
                            sqlText = sqlText.Replace("Into temp t_iopl With no log", "");
                        #endif
                        sqlText = "insert into t_iopl (nzp_kvar, nzp_pack_ls,  nzp_key , sum_prih,  g_sum_ls) " + sqlText;
                    }
                    ret = ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log).GetReturnsType().GetReturns();

                    if (!ret.result)
                    {
                        DropTempTablesPack(conn_db);
                        MessageInPackLog(conn_db, packXX, "Ошибка распределения 2.1", true, out r);
                        return false;
                    }
                }
                ExecSQL(conn_db, " Create unique index ix2_t_iopl on t_iopl (nzp_key,nzp_pack_ls) ", true);
                ExecSQL(conn_db, " Create unique index ix3_t_iopl on t_iopl (nzp_pack_ls) ", true);
                #if PG
                    ExecSQL(conn_db, " analyze t_iopl ", true);
                #else
                    ExecSQL(conn_db, " Update statistics for table t_iopl ", true);
                #endif
                
                // Сновая подсчитать сумму распределённой оплаты
                sqlText =
                    " update t_iopl set sum_prih = (select SUM(sum_prih) from t_opl where t_opl.nzp_pack_ls = t_iopl.nzp_pack_ls and kod_info >0 and kod_info <> 110)   ";
                ret = ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log).GetReturnsType().GetReturns();
                if (!ret.result)
                {
                    DropTempTablesPack(conn_db);
                    MessageInPackLog(conn_db, packXX, "Ошибка распределения 2.2", true, out r);
                    return false;
                }
                if (Points.packDistributionParameters.EnableLog & packXX.nzp_pack_ls > 0)
                {
                    sqlText =
                        " select g_sum_ls- sum_prih as sum_1, g_sum_ls, sum_prih from t_iopl where nzp_pack_ls = t_iopl.nzp_pack_ls    ";
                    dt = ClassDBUtils.OpenSQL(sqlText, conn_db).GetData();
                    if (dt.Rows.Count > 0)
                    {
                        row = dt.Rows[0];
                        if (row["sum_1"] != DBNull.Value)
                        {
                            if (Convert.ToDecimal(row["sum_1"]) != 0)
                            {
                                s_okr = Convert.ToString(row["sum_1"]);
                                MessageInPackLog(conn_db, packXX, "Сумма распределения (" + Convert.ToString(row["sum_prih"]) + " руб) не соответствует оплате (" + Convert.ToString(row["g_sum_ls"]) + " руб) на " + Convert.ToString(row["sum_1"]), false, out r);
                            }
                        }
                    }
                }

                sqlText = "Update t_iopl  Set nzp_key = ( Select MAX( nzp_key) From t_opl a Where a.nzp_pack_ls = t_iopl.nzp_pack_ls  and a.sum_prih = (select MAX(b.sum_prih) from t_opl b where a.nzp_pack_ls = b.nzp_pack_ls and b.isdel=0))  " +
                " Where  g_sum_ls <> sum_prih and sum_prih <>0 ";
                ret = ExecSQL(conn_db, sqlText, true);
                if (!ret.result)
                {
                    DropTempTablesPack(conn_db);
                    MessageInPackLog(conn_db, packXX, "Ошибка распределения 2.3", true, out r);
                    return false;
                }
                sqlText =
                    " Update t_opl " +
                    " Set sum_prih = sum_prih + " +
                             " (( Select sum(g_sum_ls - sum_prih) From t_iopl i " +
                                " Where i.nzp_key = t_opl.nzp_key and i.nzp_pack_ls = t_opl.nzp_pack_ls and t_opl.isdel=0 " +
                                "   and abs(i.sum_prih - i.g_sum_ls) > 0.0001 )) , " +
                                " sum_prih_d = sum_prih - sum_prih_u - sum_prih_a-sum_prih_s " +
                    " Where 0 < ( Select count(*) From t_iopl i " +
                                " Where i.nzp_key = t_opl.nzp_key and i.nzp_pack_ls = t_opl.nzp_pack_ls " +
                                "   and abs(i.sum_prih - i.g_sum_ls) > 0.0001  " +
                                "   and abs(i.sum_prih - i.g_sum_ls) < 0.20 ) "; ;
                #if PG
                    retres = ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log);
                    ret = retres.GetReturnsType().GetReturns();
                #else
                    ret = ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log).GetReturnsType().GetReturns();
                #endif
                if (!ret.result)
                {
                    DropTempTablesPack(conn_db);
                    MessageInPackLog(conn_db, packXX, "Ошибка распределения 2.4", true, out r);
                    return false;
                }
                #if PG
                    count = retres.resultAffectedRows;
                #else
                    count = ClassDBUtils.GetAffectedRowsCount(conn_db);
                #endif

                if (Points.packDistributionParameters.EnableLog & packXX.nzp_pack_ls > 0 & count > 0)
                {
                    if (s_okr.Trim() != "0")
                    {
                        #if PG
                            sqlText =
                            " select a.service from " + Points.Pref + "_kernel.services a, t_opl b, t_iopl t where a.nzp_serv = b.nzp_serv and b.nzp_key = t.nzp_key  ";
                        #else
                            sqlText =
                            " select a.service from " + Points.Pref + "_kernel:services a, t_opl b, t_iopl t where a.nzp_serv = b.nzp_serv and b.nzp_key = t.nzp_key  ";
                        #endif
                        dt = ClassDBUtils.OpenSQL(sqlText, conn_db).GetData();
                        if (dt.Rows.Count > 0)
                        {
                            row = dt.Rows[0];
                            MessageInPackLog(conn_db, packXX, "Ошибка округления " + s_okr + " зачтена на услугу '" + Convert.ToString(row["service"]) + "'", false, out r);
                        }
                    }
                }

                sqlText =
                    " Update t_opl " +
                    " Set sum_prih_d = sum_prih - sum_prih_u - sum_prih_a-sum_prih_s where nzp_key = (select nzp_key from t_iopl where t_opl.nzp_pack_ls = t_iopl.nzp_pack_ls) and kod_info in (101,102,103,106,107,108,109,110) ";
                ret = ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log).GetReturnsType().GetReturns();
                if (!ret.result)
                {
                    DropTempTablesPack(conn_db);
                    MessageInPackLog(conn_db, packXX, "Ошибка распределения 2.4.1", true, out r);
                    return false;
                }

                sqlText =
                    " Update t_opl " +
                    " Set sum_prih_a = sum_prih - sum_prih_u - sum_prih_d-sum_prih_s where nzp_key = (select nzp_key from t_iopl where t_opl.nzp_pack_ls = t_iopl.nzp_pack_ls) and kod_info in (104) ";
                ret = ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log).GetReturnsType().GetReturns();
                if (!ret.result)
                {
                    DropTempTablesPack(conn_db);
                    MessageInPackLog(conn_db, packXX, "Ошибка распределения 2.4.2", true, out r);
                    return false;
                }
                // Сновая подсчитать сумму распределённой оплаты
                sqlText =
                    " update t_iopl set sum_prih = (select SUM(sum_prih) from t_opl where t_opl.nzp_pack_ls = t_iopl.nzp_pack_ls and kod_info >0)   ";
                ret = ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log).GetReturnsType().GetReturns();
                if (!ret.result)
                {
                    DropTempTablesPack(conn_db);
                    MessageInPackLog(conn_db, packXX, "Ошибка распределения 2.5", true, out r);
                    return false;
                }
                sqlText =
                    " update t_opl set kod_info = 0 where kod_info >0 and nzp_pack_ls in (select i.nzp_pack_ls from t_iopl i where i.sum_prih <> i.g_sum_ls) and kod_info <> 110 ";
                ret = ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log).GetReturnsType().GetReturns();
                if (!ret.result)
                {
                    DropTempTablesPack(conn_db);
                    MessageInPackLog(conn_db, packXX, "Ошибка распределения 2.6", true, out r);
                    return false;
                }

                sqlText =
                    " update t_opl set kod_info = 0 where kod_info >0 and kod_info <> 110 and nzp_pack_ls not in (select i.nzp_pack_ls from t_iopl i )  ";
                ret = ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log).GetReturnsType().GetReturns();
                if (!ret.result)
                {
                    DropTempTablesPack(conn_db);
                    MessageInPackLog(conn_db, packXX, "Ошибка распределения 2.6", true, out r);
                    return false;
                }


                // Поправить знак для отрицательных оплат
                sqlText = "update t_opl set g_sum_ls = (-1)*g_sum_ls,  g_sum_ls_first = (-1)*g_sum_ls_first, sum_prih = (-1)*sum_prih, sum_prih_d = (-1)*sum_prih_d, sum_prih_s = (-1)*sum_prih_s,sum_prih_u = (-1)*sum_prih_u,sum_prih_a = (-1)*sum_prih_a where znak = -1 and kod_info >0 and kod_info <>110 ";
                ExecSQL(conn_db, sqlText, true);


                //----------------------- СОХРАНЕНИЕ -----------------------

                #if PG
                    if (pack_type == 20)
                    {
                        string tableSupplier = paramcalc.pref + "_charge_" + (paramcalc.calc_yy.ToString("yyyy").Substring(2, 2)) + "." + "from_supplier";
                        ExecByStep(conn_db, "t_opl a", "a.nzp_key",
                        " Insert into " + packXX.fn_supplier + " ( nzp_charge, num_ls, nzp_pack_ls, nzp_serv, nzp_supp, sum_prih, kod_sum, dat_month, dat_prih, dat_uchet) " +
                        " Select a.kod_info, a.num_ls, a.nzp_pack_ls, a.nzp_serv, a.nzp_supp, a.sum_prih,  a.kod_sum, a.dat_month, a.dat_vvod, " + packXX.paramcalc.dat_oper +
                        " From t_opl a, t_selkvar b " +
                        " Where  a.kod_info > 0 and abs(a.sum_prih) > 0.0001 and a.nzp_pack_ls = b.nzp_pack_ls ", 1000000, "", out ret);
                    } else
                    {
                        string tableSupplier = paramcalc.pref + "_charge_" + (paramcalc.calc_yy.ToString("yyyy").Substring(2, 2)) + "." + "fn_supplier" + paramcalc.calc_mm.ToString("00");
                        ExecByStep(conn_db, "t_opl a", "a.nzp_key",
                        " Insert into " + packXX.fn_supplier + " ( nzp_charge, num_ls, nzp_pack_ls, nzp_serv, nzp_supp, sum_prih, kod_sum, dat_month, dat_prih, dat_uchet, s_dolg, s_user, s_forw ) " +
                        " Select a.kod_info, a.num_ls, a.nzp_pack_ls, a.nzp_serv, a.nzp_supp, a.sum_prih,  a.kod_sum, a.dat_month, a.dat_vvod, " + packXX.paramcalc.dat_oper + ", a.sum_prih_d+a.sum_prih_s, a.sum_prih_u, a.sum_prih_a " +
                        " From t_opl a, t_selkvar b " +
                        " Where  a.kod_info > 0 and abs(a.sum_prih) > 0.0001 and a.nzp_pack_ls = b.nzp_pack_ls ", 1000000, "", out ret);
                    }
                #else
                    if (pack_type == 20)
                    {
                        string tableSupplier = paramcalc.pref + "_charge_" + (paramcalc.calc_yy.ToString("yyyy").Substring(2, 2)) + ":" + "from_supplier" ;
                        ExecByStep(conn_db, "t_opl a", "a.nzp_key",
                        " Insert into " + packXX.fn_supplier + " ( nzp_charge, num_ls, nzp_pack_ls, nzp_serv, nzp_supp, sum_prih, kod_sum, dat_month, dat_prih, dat_uchet ) " +
                        " Select a.kod_info, a.num_ls, a.nzp_pack_ls, a.nzp_serv, a.nzp_supp, a.sum_prih,  a.kod_sum, a.dat_month, a.dat_vvod, " + packXX.paramcalc.dat_oper + 
                        " From t_opl a, t_selkvar b " +
                        " Where  a.kod_info > 0 and abs(a.sum_prih) > 0.0001 and a.nzp_pack_ls = b.nzp_pack_ls ", 1000000, "", out ret);
                    } else
                    {
                        string tableSupplier = paramcalc.pref + "_charge_" + (paramcalc.calc_yy.ToString("yyyy").Substring(2, 2)) + ":" + "fn_supplier" + paramcalc.calc_mm.ToString("00");
                        ExecByStep(conn_db, "t_opl a", "a.nzp_key",
                        " Insert into " + packXX.fn_supplier + " ( nzp_charge, num_ls, nzp_pack_ls, nzp_serv, nzp_supp, sum_prih, kod_sum, dat_month, dat_prih, dat_uchet, s_dolg, s_user, s_forw ) " +
                        " Select a.kod_info, a.num_ls, a.nzp_pack_ls, a.nzp_serv, a.nzp_supp, a.sum_prih,  a.kod_sum, a.dat_month, a.dat_vvod, " + packXX.paramcalc.dat_oper + ", a.sum_prih_d+a.sum_prih_s, a.sum_prih_u, a.sum_prih_a " +
                        " From t_opl a, t_selkvar b " +
                        " Where  a.kod_info > 0 and abs(a.sum_prih) > 0.0001 and a.nzp_pack_ls = b.nzp_pack_ls ", 1000000, "", out ret);
                    }
                #endif
                if (!ret.result)
                {
                    DropTempTablesPack(conn_db);
                    MessageInPackLog(conn_db, packXX, "Ошибка распределения 2.7", true, out r);
                    return false;
                }

                #if PG
                    sqlText =
                    " Update " + packXX.pack_ls +
                    " Set dat_uchet = " + packXX.paramcalc.dat_oper +
                    ", date_distr = now(), inbasket =0, nzp_user = " + nzp_user +
                    " Where nzp_pack_ls in ( Select nzp_pack_ls From t_opl Where kod_info > 0 ) ";
                    retres = ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log);
                    ret = retres.GetReturnsType().GetReturns();
                #else
                    sqlText =
                    " Update " + packXX.pack_ls +
                    " Set dat_uchet = " + packXX.paramcalc.dat_oper +
                      ", date_distr = current, inbasket =0, nzp_user = " + nzp_user +
                    " Where nzp_pack_ls in ( Select nzp_pack_ls From t_opl Where kod_info > 0 ) ";
                ret = ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log).GetReturnsType().GetReturns();
                #endif

                if (!ret.result)
                {
                    DropTempTablesPack(conn_db);
                    MessageInPackLog(conn_db, packXX, "Ошибка распределения 2.8", true, out r);
                    return false;
                }

                #if PG
                    count = retres.resultAffectedRows;
                #else
                    count = ClassDBUtils.GetAffectedRowsCount(conn_db);
                #endif
                if (count == 0 && count_special == 0)
                {
                    // Нет распределённых оплат
                    ret.text = "Распределение не было произведено";
                    ret.tag = -1;
                    //return false;
                }
                else
                {
                    #region Сохранить показания приборов учёта
                    SaveCountersValsFromPackls(conn_db, nzp_user, paramcalc,0, out ret);
                    #endregion Сохранить показания приборов учёта


                    int count_total = 0;
                    DataTable dt5 = ClassDBUtils.OpenSQL("select count(*) as c from t_selkvar", conn_db).GetData();
                    DataRow row5;
                    row5 = dt5.Rows[0];

                    if (row5["c"] != null)
                    {
                        count_total = Convert.ToInt32(row5["c"]);
                    }
                    else
                    {
                        count_total = 0;
                    }
                    if (count + count_special != count_total)
                    {
                        ret.text = "Распределение было произведено не полностью";
                        ret.tag = -1;
                        //return true;

                    }

                    // Сохранить запись в журнал событий
                    int nzp_event = SaveEvent(6611, conn_db, paramcalc.nzp_user, paramcalc.nzp_pack, "Операционный день " + packXX.paramcalc.dat_oper);

                    if ( (nzp_event > 0) && (isDebug) )
                    {
                        ret = ExecSQL(conn_db, "insert into " + Points.Pref + "_data"+tableDelimiter+"sys_event_detail(nzp_event, table_, nzp) select distinct " + nzp_event + ",'" + packXX.pack_ls + "',nzp_pack_ls from t_opl where kod_info>0 ", true);
                    }


                }

                if (Points.packDistributionParameters.EnableLog & packXX.nzp_pack_ls > 0)
                {
                    // Сохранить вспомогательную таблицу t_opl в базе
                    sqlText =
                        " delete from t_opl_log where nzp_pack_ls =  " + packXX.nzp_pack_ls;
                    ret = ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log).GetReturnsType().GetReturns();
                    if (!ret.result)
                    {
                        MessageInPackLog(conn_db, packXX, "Создание таблицы t_opl_log", true, out r);
                        #if PG
                            sqlText =
                            " Create  table t_opl_log " +
                            " ( nzp_key     serial, " +
                            "   nzp_kvar    integer, " +
                            "   num_ls      integer, " +
                            "   nzp_serv    integer, " +
                            "   nzp_supp    integer, " +
                            "   sum_outsaldo_prev   numeric(14,2) default 0, " +
                            "   sum_outsaldo   numeric(14,2) default 0, " +
                            "   isum_outsaldo  numeric(14,2) default 0, " +
                            "   rsum_tarif    numeric(14,2) default 0, " +
                            "   irsum_tarif   numeric(14,2) default 0, " +
                            "   kod_sum       integer, " +
                            "   nzp_pack_ls   integer, " +
                            "   dat_month     date, " +
                            "   dat_vvod      date, " +
                            "   g_sum_ls      numeric(14,2) default 0, " +
                            "   g_sum_ls_ost   numeric(14,2) default 0, " +
                            "   kod_info      integer, " +
                            "   sum_prih      numeric(14,2) default 0, " +
                            // !!! Новые поля, необходимые для распределения. Марат 18.12.2012
                            "   sum_prih_u    numeric(14,2) default 0, " +
                            "   sum_prih_d    numeric(14,2) default 0, " +
                            "   sum_prih_a    numeric(14,2) default 0, " +
                            "   sum_prih_s    numeric(14,2) default 0, " +
                            "   gil_sums      numeric(14,2) default 0, " +
                            "   isdel         integer default 0, " +
                            "   sum_charge_prev   numeric(14,2) default 0, " +
                            "   sum_charge   numeric(14,2) default 0, " +
                            "   isum_charge   numeric(14,2) default 0, " +
                            "   isum_charge_isdel_1  numeric(14,2) default 0, " +
                            "   isum_charge_isdel_0  numeric(14,2) default 0 " +
                            " )  ";
                        #else
                            sqlText =
                            " Create  table t_opl_log " +
                            " ( nzp_key     serial, " +
                            "   nzp_kvar    integer, " +
                            "   num_ls      integer, " +
                            "   nzp_serv    integer, " +
                            "   nzp_supp    integer, " +
                            "   sum_outsaldo_prev   decimal(14,2) default 0, " +
                            "   sum_outsaldo   decimal(14,2) default 0, " +
                            "   isum_outsaldo  decimal(14,2) default 0, " +
                            "   rsum_tarif    decimal(14,2) default 0, " +
                            "   irsum_tarif   decimal(14,2) default 0, " +
                            "   kod_sum       integer, " +
                            "   nzp_pack_ls   integer, " +
                            "   dat_month     date, " +
                            "   dat_vvod      date, " +
                            "   g_sum_ls      decimal(14,2) default 0, " +
                            "   g_sum_ls_ost   decimal(14,2) default 0, " +
                            "   kod_info      integer, " +
                            "   sum_prih      decimal(14,2) default 0, " +
                            // !!! Новые поля, необходимые для распределения. Марат 18.12.2012
                            "   sum_prih_u    decimal(14,2) default 0, " +
                            "   sum_prih_d    decimal(14,2) default 0, " +
                            "   sum_prih_a    decimal(14,2) default 0, " +
                            "   sum_prih_s    decimal(14,2) default 0, " +
                            "   gil_sums      decimal(14,2) default 0, " +
                            "   isdel         integer default 0, " +
                            "   sum_charge_prev   decimal(14,2) default 0, " +
                            "   sum_charge   decimal(14,2) default 0, " +
                            "   isum_charge   decimal(14,2) default 0, " +
                            "   isum_charge_isdel_1  decimal(14,2) default 0, " +
                            "   isum_charge_isdel_0  decimal(14,2) default 0 " +
                            " )  ";
                        #endif

                        MessageInPackLog(conn_db, packXX, "Создание таблицы t_opl_log", true, out r);
                        ret = ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log).GetReturnsType().GetReturns();
                        if (!ret.result)
                        {
                            MessageInPackLog(conn_db, packXX, "Ошибка создания таблицы t_opl_log", true, out r);
                            DropTempTablesPack(conn_db);
                            return false;
                        }
                    }
                    sqlText =
                                " insert into  t_opl_log " +
                                " ( nzp_key, " +
                                "   nzp_kvar, " +
                                "   num_ls  , " +
                                "   nzp_serv, " +
                                "   nzp_supp, " +
                                "   sum_outsaldo_prev, " +
                                "   sum_outsaldo, " +
                                "   isum_outsaldo , " +
                                "   rsum_tarif , " +
                                "   irsum_tarif, " +
                                "   kod_sum   , " +
                                "   nzp_pack_ls, " +
                                "   dat_month, " +
                                "   dat_vvod, " +
                                "   g_sum_ls, " +
                                "   g_sum_ls_ost, " +
                                "   kod_info, " +
                                "   sum_prih," +
                                "   sum_prih_u , " +
                                "   sum_prih_d , " +
                                "   sum_prih_a , " +
                                "   sum_prih_s , " +
                                "   gil_sums   , " +
                                "   isdel, " +
                                "   sum_charge_prev, " +
                                "   sum_charge, " +
                                "   isum_charge, " +
                                "   isum_charge_isdel_1," +
                                "   isum_charge_isdel_0" +
                                " )  select  " +
                                "   nzp_key, " +
                                "   nzp_kvar, " +
                                "   num_ls  , " +
                                "   nzp_serv, " +
                                "   nzp_supp, " +
                                "   sum_outsaldo_prev, " +
                                "   sum_outsaldo, " +
                                "   isum_outsaldo , " +
                                "   rsum_tarif , " +
                                "   irsum_tarif, " +
                                "   kod_sum   , " +
                                "   nzp_pack_ls, " +
                                "   dat_month, " +
                                "   dat_vvod, " +
                                "   g_sum_ls, " +
                                "   g_sum_ls_ost, " +
                                "   kod_info, " +
                                "   sum_prih," +
                                "   sum_prih_u , " +
                                "   sum_prih_d , " +
                                "   sum_prih_a , " +
                                "   sum_prih_s , " +
                                "   gil_sums   , " +
                                "   isdel, " +
                                "   sum_charge_prev, " +
                                "   sum_charge, " +
                                "   isum_charge, " +
                                "   isum_charge_isdel_1," +
                                "   isum_charge_isdel_0 " +
                                " from t_opl where nzp_pack_ls = " + packXX.nzp_pack_ls;
                    ret = ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log).GetReturnsType().GetReturns();
                }

                //определить нераспределенные оплаты

                sqlText =
                    " Update " + packXX.pack_ls +
                    " Set inbasket = 1, dat_uchet = null, alg =0, nzp_user = " + nzp_user +
                    " Where nzp_pack =  " +packXX.nzp_pack +
                    "   and num_ls = 0  and inbasket = 0  ";
                #if PG
                    retres = ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log);
                    ret = retres.GetReturnsType().GetReturns();
                #else
                    ret = ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log).GetReturnsType().GetReturns();
                #endif
                if (!ret.result)
                {
                    DropTempTablesPack(conn_db);
                    MessageInPackLog(conn_db, packXX, "Ошибка распределения 2.10", true, out r);
                    return false;
                }

                #if PG
                    count = retres.resultAffectedRows;
                #else
                    count = ClassDBUtils.GetAffectedRowsCount(conn_db);
                #endif
                if (count > 0)
                {
                    count_err = count_err + 1;
                }


                sqlText =
                    " Update " + packXX.pack_ls +
                    " Set inbasket = 1, dat_uchet = null, alg=0, nzp_user = " + nzp_user +
                    " Where nzp_pack_ls not in ( Select nzp_pack_ls From t_opl Where kod_info > 0 ) and nzp_pack_ls in ( Select a.nzp_pack_ls From t_opl  a  ) ";
                #if PG
                    retres = ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log);
                    ret = retres.GetReturnsType().GetReturns();
                #else
                    ret = ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log).GetReturnsType().GetReturns();
                #endif
               
                if (!ret.result)
                {
                    DropTempTablesPack(conn_db);
                    MessageInPackLog(conn_db, packXX, "Ошибка распределения 2.9", true, out r);
                    return false;
                }
                #if PG
                    count = retres.resultAffectedRows;
                #else
                    count = ClassDBUtils.GetAffectedRowsCount(conn_db);
                #endif
                if (count > 0)
                {
                    count_err = count_err + 1;
                }

                sqlText =
                    " Update " + packXX.pack_ls +
                    " Set inbasket = 1, dat_uchet = null, alg =0, nzp_user = " + nzp_user +
                    " Where nzp_pack_ls in ( Select nzp_pack_ls From t_selkvar ) " +
                    "   and nzp_pack_ls not in ( Select nzp_pack_ls From t_opl  ) ";
                #if PG
                    retres = ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log);
                    ret = retres.GetReturnsType().GetReturns();
                #else
                    ret = ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log).GetReturnsType().GetReturns();
                #endif
                if (!ret.result)
                {
                    DropTempTablesPack(conn_db);
                    MessageInPackLog(conn_db, packXX, "Ошибка распределения 2.10", true, out r);
                    return false;
                }
                #if PG
                    count = retres.resultAffectedRows;
                #else
                    count = ClassDBUtils.GetAffectedRowsCount(conn_db);
                #endif
                if (count > 0)
                {
                    count_err = count_err + 1;
                }

                if (isDistributionForInSaldo)
                {

                    //ошибка - отсутствие суммы к оплате по закрытому лицевому счёту
                #if PG
                    sqlText =
                                            " Insert into " + packXX.pack_log +
                                            " ( nzp_pack,nzp_pack_ls,dat_oper,dat_log,nzp_wp, txt_log,tip_log ) " +
                                            " Select nzp_pack,nzp_pack_ls, " + packXX.paramcalc.dat_oper + ", now(), " +
                                                packXX.paramcalc.nzp_wp + ", 'отсутствуют сумма входящего сальдо по закрытому лицевому счёту', 1001 " +
                                            " From t_selkvar " +
                                            " Where nzp_kvar not in ( Select nzp_kvar From t_charge where sum_insaldo<>0  ) and kod_sum in (" + strKodSumForCharge_MM + ",81) and is_open =  0 ";
                #else
                    sqlText =
                        " Insert into " + packXX.pack_log +
                        " ( nzp_pack,nzp_pack_ls,dat_oper,dat_log,nzp_wp, txt_log,tip_log ) " +
                        " Select nzp_pack,nzp_pack_ls, " + packXX.paramcalc.dat_oper + ", current, " +
                            packXX.paramcalc.nzp_wp + ", 'отсутствуют сумма входящего сальдо по закрытому лицевому счёту', 1001 " +
                        " From t_selkvar " +
                        " Where nzp_kvar not in ( Select nzp_kvar From t_charge where sum_insaldo<>0  ) and kod_sum in (" + strKodSumForCharge_MM + ",81) and is_open =  0 ";
                #endif
                ret = ExecSQL(conn_db, sqlText, true);
                if (!ret.result)
                {
                        DropTempTablesPack(conn_db);
                        return false;
                }
                    
               //ошибка - отсутствие начислений
                #if PG
                    sqlText =
                                           " Insert into " + packXX.pack_log +
                                           " ( nzp_pack,nzp_pack_ls,dat_oper,dat_log,nzp_wp, txt_log,tip_log ) " +
                                           " Select nzp_pack,nzp_pack_ls, " + packXX.paramcalc.dat_oper + ", now(), " +
                                               packXX.paramcalc.nzp_wp + ", 'отсутствуют начисления', 1 " +
                                           " From t_selkvar " +
                                           " Where nzp_kvar not in ( Select nzp_kvar From t_charge where sum_charge>0 or sum_insaldo>0 ) and kod_sum in (" + strKodSumForCharge_MM + ",81) and is_open = 1  ";
                #else
                    sqlText =
                        " Insert into " + packXX.pack_log +
                        " ( nzp_pack,nzp_pack_ls,dat_oper,dat_log,nzp_wp, txt_log,tip_log ) " +
                        " Select nzp_pack,nzp_pack_ls, " + packXX.paramcalc.dat_oper + ", current, " +
                            packXX.paramcalc.nzp_wp + ", 'отсутствуют начисления', 1 " +
                        " From t_selkvar " +
                        " Where nzp_kvar not in ( Select nzp_kvar From t_charge where sum_charge>0 or sum_insaldo>0 ) and kod_sum in (" + strKodSumForCharge_MM + ",81) and is_open = 1  ";
                #endif
                ret = ExecSQL(conn_db, sqlText, true);
                if (!ret.result)
                    {
                        DropTempTablesPack(conn_db);
                        return false;
                    }

                }
                else
                {
                    //ошибка - отсутствие суммы к оплате по закрытому лицевому счёту
                #if PG
                    sqlText =
                                         " Insert into " + packXX.pack_log +
                                         " ( nzp_pack,nzp_pack_ls,dat_oper,dat_log,nzp_wp, txt_log,tip_log ) " +
                                         " Select nzp_pack,nzp_pack_ls, " + packXX.paramcalc.dat_oper + ", now(), " +
                                             packXX.paramcalc.nzp_wp + ", 'отсутствуют сумма к оплате по закрытому лицевому счёту', 1001 " +
                                         " From t_selkvar " +
                                         " Where nzp_kvar not in ( Select nzp_kvar From t_charge where sum_charge>0  ) and kod_sum in (" + strKodSumForCharge_MM + ",81) and is_open =  0 ";
               #else
                    sqlText =
                        " Insert into " + packXX.pack_log +
                        " ( nzp_pack,nzp_pack_ls,dat_oper,dat_log,nzp_wp, txt_log,tip_log ) " +
                        " Select nzp_pack,nzp_pack_ls, " + packXX.paramcalc.dat_oper + ", current, " +
                            packXX.paramcalc.nzp_wp + ", 'отсутствуют сумма к оплате по закрытому лицевому счёту', 1001 " +
                        " From t_selkvar " +
                        " Where nzp_kvar not in ( Select nzp_kvar From t_charge where sum_charge>0  ) and kod_sum in (" + strKodSumForCharge_MM + ",81) and is_open =  0 ";
               #endif
               ret = ExecSQL(conn_db, sqlText, true);
               if (!ret.result)
               {
                    DropTempTablesPack(conn_db);
                   return false;
               }

               //ошибка - отсутствие начислений
               #if PG
                    sqlText =
                                           " Insert into " + packXX.pack_log +
                                           " ( nzp_pack,nzp_pack_ls,dat_oper,dat_log,nzp_wp, txt_log,tip_log ) " +
                                           " Select nzp_pack,nzp_pack_ls, " + packXX.paramcalc.dat_oper + ", now(), " +
                                               packXX.paramcalc.nzp_wp + ", 'отсутствуют начисления', 1 " +
                                           " From t_selkvar " +
                                           " Where nzp_kvar not in ( Select nzp_kvar From t_charge where sum_charge>0 or rsum_tarif>0 ) and kod_sum in (" + strKodSumForCharge_MM + ",81) and is_open = 1  ";
               #else
                    sqlText =
                        " Insert into " + packXX.pack_log +
                        " ( nzp_pack,nzp_pack_ls,dat_oper,dat_log,nzp_wp, txt_log,tip_log ) " +
                        " Select nzp_pack,nzp_pack_ls, " + packXX.paramcalc.dat_oper + ", current, " +
                            packXX.paramcalc.nzp_wp + ", 'отсутствуют начисления', 1 " +
                        " From t_selkvar " +
                        " Where nzp_kvar not in ( Select nzp_kvar From t_charge where sum_charge>0 or rsum_tarif>0 ) and kod_sum in (" + strKodSumForCharge_MM + ",81) and is_open = 1  ";
               #endif
               ret = ExecSQL(conn_db, sqlText, true);
               if (!ret.result)
               {
                        DropTempTablesPack(conn_db);
                        return false;
                    }

               }

               if (count_err > 0)
               {
                    // Есть оплата помещённая в корзину
                    if (packXX.nzp_pack_ls > 0)
                    {
                        ret2.text = "Распределение не было произведено. Нераспределённая оплата была помещена в корзину. ";
                    }
                    else
                    {
                        ret2.text = "Распределение не было произведено. Нераспределённые оплаты были помещены в корзину. ";
                    }
                    ret2.tag = -1;
                    //return false;
                }

                #region Заполнить комментарии к ошибкам нераспределённых оплат



                // 0. Недопустимый код квитанции 
                #if PG
                    ret = ExecSQL(conn_db,
                                   " Insert into " + Points.Pref + "_fin_" + (Points.DateOper.Year % 100).ToString("00") +
                                  ".pack_ls_err ( nzp_pack_ls,nzp_err,note ) " +
                                   " Select nzp_pack_ls, 99,'Недопустимый код квитанции'" +
                                   " From t_selkvar " +
                                   " Where kod_sum not in (" + strKodSumForCharge_MM + "," + strKodSumForCharge_X + ",81)"
                                   , true);
                #else
                    ret = ExecSQL(conn_db,
                    " Insert into " + Points.Pref + "_fin_" + (Points.DateOper.Year % 100).ToString("00") + "@" + DBManager.getServer(conn_db) +
                   ":pack_ls_err ( nzp_pack_ls,nzp_err,note ) " +
                    " Select nzp_pack_ls, 99,'Недопустимый код квитанции'" +
                    " From t_selkvar " +
                    " Where kod_sum not in (" + strKodSumForCharge_MM + "," + strKodSumForCharge_X + ",81)"
                    , true);
                #endif
                if (!ret.result)
                {
                    DropTempTablesPack(conn_db);
                    return false;
                }

                if (isDistributionForInSaldo)
                {

                    // 1. Отсутствуют начисления 
                    //ошибка - отсутствие начислено к оплате по закрытому лицевому счёту
                    #if PG
                        ret = ExecSQL(conn_db,
                            " Insert into " + Points.Pref + "_fin_" + (Points.DateOper.Year % 100).ToString("00") +
                            ".pack_ls_err ( nzp_pack_ls,nzp_err,note ) " +
                            " Select nzp_pack_ls, 1001,'за '||" +
                                    "case when substr('" + distr_month.Trim() + "',4,2)='01' then 'январь '" +
                                    "when substr('" + distr_month.Trim() + "',4,2)='02' then 'февраль ' " +
                                    "when substr('" + distr_month.Trim() + "',4,2)='03' then 'март ' " +
                                    "when substr('" + distr_month.Trim() + "',4,2)='04' then 'апрель ' " +
                                    "when substr('" + distr_month.Trim() + "',4,2)='05' then 'май ' " +
                                    "when substr('" + distr_month.Trim() + "',4,2)='06' then 'июнь ' " +
                                    "when substr('" + distr_month.Trim() + "',4,2)='07' then 'июль '" +
                                    "when substr('" + distr_month.Trim() + "',4,2)='08' then 'август '" +
                                    "when substr('" + distr_month.Trim() + "',4,2)='09' then 'сентябрь '" +
                                    "when substr('" + distr_month.Trim() + "',4,2)='10' then 'октябрь '" +
                                    "when substr('" + distr_month.Trim() + "',4,2)='11' then 'ноябрь '" +
                                    "when substr('" + distr_month.Trim() + "',4,2)='12' then 'декабрь '" +
                                    "end || substr('" + distr_month.Trim() + "',7,10)|| ' г.'" +
                            " From t_selkvar " +
                            " Where nzp_kvar not in ( Select nzp_kvar From t_itog where sum_insaldo>0 or rsum_tarif>0 ) and is_open = 0 and kod_sum not in (" + strKodSumForCharge_X + ")"
                            , true);
                    #else
                        ret = ExecSQL(conn_db,
                        " Insert into " + Points.Pref + "_fin_" + (Points.DateOper.Year % 100).ToString("00") + "@" + DBManager.getServer(conn_db) +
                       ":pack_ls_err ( nzp_pack_ls,nzp_err,note ) " +
                        " Select nzp_pack_ls, 1001,'за '||" +
                                "case when substr('"+distr_month.Trim()+"',4,2)='01' then 'январь '" +
                                "when substr('" + distr_month.Trim() + "',4,2)='02' then 'февраль ' " +
                                "when substr('" + distr_month.Trim() + "',4,2)='03' then 'март ' " +
                                "when substr('" + distr_month.Trim() + "',4,2)='04' then 'апрель ' " +
                                "when substr('" + distr_month.Trim() + "',4,2)='05' then 'май ' " +
                                "when substr('" + distr_month.Trim() + "',4,2)='06' then 'июнь ' " +
                                "when substr('" + distr_month.Trim() + "',4,2)='07' then 'июль '" +
                                "when substr('" + distr_month.Trim() + "',4,2)='08' then 'август '" +
                                "when substr('" + distr_month.Trim() + "',4,2)='09' then 'сентябрь '" +
                                "when substr('" + distr_month.Trim() + "',4,2)='10' then 'октябрь '" +
                                "when substr('" + distr_month.Trim() + "',4,2)='11' then 'ноябрь '" +
                                "when substr('" + distr_month.Trim() + "',4,2)='12' then 'декабрь '" +
                                "end || substr('" + distr_month.Trim() + "',7,10)|| ' г.'" +
                        " From t_selkvar " +
                        " Where nzp_kvar not in ( Select nzp_kvar From t_itog where sum_insaldo>0 or rsum_tarif>0 ) and is_open = 0 and kod_sum not in (" + strKodSumForCharge_X + ")"
                        , true);
                    #endif
                    if (!ret.result)
                    {
                        DropTempTablesPack(conn_db);
                        return false;
                    }

                    //3.ошибка - отсутствие начислено к оплате по закрытому лицевому счёту
                    #if PG
                            sqlText = " Insert into " + Points.Pref + "_fin_" + (Points.DateOper.Year % 100).ToString("00")  +
                                       ".pack_ls_err ( nzp_pack_ls,nzp_err,note ) " +
                                        " Select nzp_pack_ls, 1,'за '||" +
                                                "case when substr('" + distr_month.Trim() + "',4,2)='01' then 'январь '" +
                                                "when substr('" + distr_month.Trim() + "',4,2)='02' then 'февраль ' " +
                                                "when substr('" + distr_month.Trim() + "',4,2)='03' then 'март ' " +
                                                "when substr('" + distr_month.Trim() + "',4,2)='04' then 'апрель ' " +
                                                "when substr('" + distr_month.Trim() + "',4,2)='05' then 'май ' " +
                                                "when substr('" + distr_month.Trim() + "',4,2)='06' then 'июнь ' " +
                                                "when substr('" + distr_month.Trim() + "',4,2)='07' then 'июль '" +
                                                "when substr('" + distr_month.Trim() + "',4,2)='08' then 'август '" +
                                                "when substr('" + distr_month.Trim() + "',4,2)='09' then 'сентябрь '" +
                                                "when substr('" + distr_month.Trim() + "',4,2)='10' then 'октябрь '" +
                                                "when substr('" + distr_month.Trim() + "',4,2)='11' then 'ноябрь '" +
                                                "when substr('" + distr_month.Trim() + "',4,2)='12' then 'декабрь '" +
                                                "end || substr('" + distr_month.Trim() + "',7,10)|| ' г.'" +
                                        " From t_selkvar " +
                                        " Where  nzp_kvar not in ( Select nzp_kvar From t_charge where sum_insaldo>0 or rsum_tarif>0 ) and kod_sum not in (" + strKodSumForCharge_X + ") and " +
                                        "nzp_pack_ls not in ( Select nzp_pack_ls From " + Points.Pref + "_fin_" + (Points.DateOper.Year % 100).ToString("00") + 
                                       ".pack_ls_err  where nzp_err not in ( 1, 1001)) ";
                    #else
                        sqlText = " Insert into " + Points.Pref + "_fin_" + (Points.DateOper.Year % 100).ToString("00") + "@" + DBManager.getServer(conn_db) +
                       ":pack_ls_err ( nzp_pack_ls,nzp_err,note ) " +
                        " Select nzp_pack_ls, 1,'за '||" +
                            "case when substr('" + distr_month.Trim() + "',4,2)='01' then 'январь '" +
                            "when substr('" + distr_month.Trim() + "',4,2)='02' then 'февраль ' " +
                            "when substr('" + distr_month.Trim() + "',4,2)='03' then 'март ' " +
                            "when substr('" + distr_month.Trim() + "',4,2)='04' then 'апрель ' " +
                            "when substr('" + distr_month.Trim() + "',4,2)='05' then 'май ' " +
                            "when substr('" + distr_month.Trim() + "',4,2)='06' then 'июнь ' " +
                            "when substr('" + distr_month.Trim() + "',4,2)='07' then 'июль '" +
                            "when substr('" + distr_month.Trim() + "',4,2)='08' then 'август '" +
                            "when substr('" + distr_month.Trim() + "',4,2)='09' then 'сентябрь '" +
                            "when substr('" + distr_month.Trim() + "',4,2)='10' then 'октябрь '" +
                            "when substr('" + distr_month.Trim() + "',4,2)='11' then 'ноябрь '" +
                            "when substr('" + distr_month.Trim() + "',4,2)='12' then 'декабрь '" +
                            "end || substr('" + distr_month.Trim() + "',7,10)|| ' г.'" +
                    " From t_selkvar " +
                    " Where  nzp_kvar not in ( Select nzp_kvar From t_charge where sum_insaldo>0 or rsum_tarif>0 ) and kod_sum not in (" + strKodSumForCharge_X + ") and " +
                    "nzp_pack_ls not in ( Select nzp_pack_ls From " + Points.Pref + "_fin_" + (Points.DateOper.Year % 100).ToString("00") + "@" + DBManager.getServer(conn_db) +
                   ":pack_ls_err  where nzp_err not in ( 1, 1001)) ";
                    #endif
                    ret = ExecSQL(conn_db, sqlText, true);
                    if (!ret.result)
                    {
                        DropTempTablesPack(conn_db);
                        return false;
                    }
                }
                else
                {
                    // 1. Отсутствуют начисления 
                    //ошибка - отсутствие начислено к оплате по закрытому лицевому счёту
                    #if PG
                        ret = ExecSQL(conn_db,
                                           " Insert into " + Points.Pref + "_fin_" + (Points.DateOper.Year % 100).ToString("00") +
                                          ".pack_ls_err ( nzp_pack_ls,nzp_err,note ) " +
                                           " Select nzp_pack_ls, 1001,'за '||" +
                                                   "case when substr('" + distr_month.Trim() + "',4,2)='01' then 'январь '" +
                                                   "when substr('" + distr_month.Trim() + "',4,2)='02' then 'февраль ' " +
                                                   "when substr('" + distr_month.Trim() + "',4,2)='03' then 'март ' " +
                                                   "when substr('" + distr_month.Trim() + "',4,2)='04' then 'апрель ' " +
                                                   "when substr('" + distr_month.Trim() + "',4,2)='05' then 'май ' " +
                                                   "when substr('" + distr_month.Trim() + "',4,2)='06' then 'июнь ' " +
                                                   "when substr('" + distr_month.Trim() + "',4,2)='07' then 'июль '" +
                                                   "when substr('" + distr_month.Trim() + "',4,2)='08' then 'август '" +
                                                   "when substr('" + distr_month.Trim() + "',4,2)='09' then 'сентябрь '" +
                                                   "when substr('" + distr_month.Trim() + "',4,2)='10' then 'октябрь '" +
                                                   "when substr('" + distr_month.Trim() + "',4,2)='11' then 'ноябрь '" +
                                                   "when substr('" + distr_month.Trim() + "',4,2)='12' then 'декабрь '" +
                                                   "end || substr('" + distr_month.Trim() + "',7,10)|| ' г.'" +
                                           " From t_selkvar " +
                                           " Where nzp_kvar not in ( Select nzp_kvar From t_itog where sum_charge>0 ) and is_open = 0 and kod_sum not in (" + strKodSumForCharge_X + ")"
                                           , true);
                   #else
                        ret = ExecSQL(conn_db,
                        " Insert into " + Points.Pref + "_fin_" + (Points.DateOper.Year % 100).ToString("00") + "@" + DBManager.getServer(conn_db) +
                       ":pack_ls_err ( nzp_pack_ls,nzp_err,note ) " +
                        " Select nzp_pack_ls, 1001,'за '||" +
                                "case when substr('" + distr_month.Trim() + "',4,2)='01' then 'январь '" +
                                "when substr('" + distr_month.Trim() + "',4,2)='02' then 'февраль ' " +
                                "when substr('" + distr_month.Trim() + "',4,2)='03' then 'март ' " +
                                "when substr('" + distr_month.Trim() + "',4,2)='04' then 'апрель ' " +
                                "when substr('" + distr_month.Trim() + "',4,2)='05' then 'май ' " +
                                "when substr('" + distr_month.Trim() + "',4,2)='06' then 'июнь ' " +
                                "when substr('" + distr_month.Trim() + "',4,2)='07' then 'июль '" +
                                "when substr('" + distr_month.Trim() + "',4,2)='08' then 'август '" +
                                "when substr('" + distr_month.Trim() + "',4,2)='09' then 'сентябрь '" +
                                "when substr('" + distr_month.Trim() + "',4,2)='10' then 'октябрь '" +
                                "when substr('" + distr_month.Trim() + "',4,2)='11' then 'ноябрь '" +
                                "when substr('" + distr_month.Trim() + "',4,2)='12' then 'декабрь '" +
                                "end || substr('" + distr_month.Trim() + "',7,10)|| ' г.'" +
                        " From t_selkvar " +
                        " Where nzp_kvar not in ( Select nzp_kvar From t_itog where sum_charge>0 ) and is_open = 0 and kod_sum not in (" + strKodSumForCharge_X + ")"
                        , true);
                  #endif
                  if (!ret.result)
                  {
                            DropTempTablesPack(conn_db);
                            return false;
                  }

                    //3.ошибка - отсутствие начислено к оплате по закрытому лицевому счёту
                 #if PG
                    sqlText = " Insert into " + Points.Pref + "_fin_" + (Points.DateOper.Year % 100).ToString("00")  +
                                     ".pack_ls_err ( nzp_pack_ls,nzp_err,note ) " +
                                      " Select nzp_pack_ls, 1,'за '||" +
                                              "case when substr('" + distr_month.Trim() + "',4,2)='01' then 'январь '" +
                                              "when substr('" + distr_month.Trim() + "',4,2)='02' then 'февраль ' " +
                                              "when substr('" + distr_month.Trim() + "',4,2)='03' then 'март ' " +
                                              "when substr('" + distr_month.Trim() + "',4,2)='04' then 'апрель ' " +
                                              "when substr('" + distr_month.Trim() + "',4,2)='05' then 'май ' " +
                                              "when substr('" + distr_month.Trim() + "',4,2)='06' then 'июнь ' " +
                                              "when substr('" + distr_month.Trim() + "',4,2)='07' then 'июль '" +
                                              "when substr('" + distr_month.Trim() + "',4,2)='08' then 'август '" +
                                              "when substr('" + distr_month.Trim() + "',4,2)='09' then 'сентябрь '" +
                                              "when substr('" + distr_month.Trim() + "',4,2)='10' then 'октябрь '" +
                                              "when substr('" + distr_month.Trim() + "',4,2)='11' then 'ноябрь '" +
                                              "when substr('" + distr_month.Trim() + "',4,2)='12' then 'декабрь '" +
                                              "end || substr('" + distr_month.Trim() + "',7,10)|| ' г.'" +
                                      " From t_selkvar " +
                                      " Where  nzp_kvar not in ( Select nzp_kvar From t_charge where sum_charge>0 or rsum_tarif>0 ) and kod_sum not in (" + strKodSumForCharge_X + ") and " +
                                      "nzp_pack_ls not in ( Select nzp_pack_ls From " + Points.Pref + "_fin_" + (Points.DateOper.Year % 100).ToString("00") + 
                #if PG
                        "" + 
                #else
                        "@" + DBManager.getServer(conn_db) +
                #endif
                                     ".pack_ls_err  where nzp_err not in ( 1, 1001)) ";
                #else
                        sqlText = " Insert into " + Points.Pref + "_fin_" + (Points.DateOper.Year % 100).ToString("00") + "@" + DBManager.getServer(conn_db) +
                            ":pack_ls_err ( nzp_pack_ls,nzp_err,note ) " +
                            " Select nzp_pack_ls, 1,'за '||" +
                            "case when substr('" + distr_month.Trim() + "',4,2)='01' then 'январь '" +
                            "when substr('" + distr_month.Trim() + "',4,2)='02' then 'февраль ' " +
                            "when substr('" + distr_month.Trim() + "',4,2)='03' then 'март ' " +
                            "when substr('" + distr_month.Trim() + "',4,2)='04' then 'апрель ' " +
                            "when substr('" + distr_month.Trim() + "',4,2)='05' then 'май ' " +
                            "when substr('" + distr_month.Trim() + "',4,2)='06' then 'июнь ' " +
                            "when substr('" + distr_month.Trim() + "',4,2)='07' then 'июль '" +
                            "when substr('" + distr_month.Trim() + "',4,2)='08' then 'август '" +
                            "when substr('" + distr_month.Trim() + "',4,2)='09' then 'сентябрь '" +
                            "when substr('" + distr_month.Trim() + "',4,2)='10' then 'октябрь '" +
                            "when substr('" + distr_month.Trim() + "',4,2)='11' then 'ноябрь '" +
                            "when substr('" + distr_month.Trim() + "',4,2)='12' then 'декабрь '" +
                            "end || substr('" + distr_month.Trim() + "',7,10)|| ' г.'" +
                    " From t_selkvar " +
                    " Where  nzp_kvar not in ( Select nzp_kvar From t_charge where sum_charge>0 or rsum_tarif>0 ) and kod_sum not in (" + strKodSumForCharge_X + ") and " +
                    "nzp_pack_ls not in ( Select nzp_pack_ls From " + Points.Pref + "_fin_" + (Points.DateOper.Year % 100).ToString("00") + "@" + DBManager.getServer(conn_db) +
                   ":pack_ls_err  where nzp_err not in ( 1, 1001)) ";
                    #endif
                    ret = ExecSQL(conn_db, sqlText, true);
                    if (!ret.result)
                    {
                        DropTempTablesPack(conn_db);
                        return false;
                    }

                }

                // 4. Отказано в распределении
                #if PG
                    sqlText =
                                   " Insert into " + Points.Pref + "_fin_" + (Points.DateOper.Year % 100).ToString("00") + 
                                  ".pack_ls_err ( nzp_pack_ls,nzp_err,note ) " +
                                   " Select nzp_pack_ls, 6,'Подробности см.в журнале распределения' " +
                                   " From t_selkvar " +
                                   " Where nzp_kvar in ( Select nzp_kvar From t_opl where kod_info =0 ) and nzp_pack_ls not in (select a.nzp_pack_ls from  " +
                                    Points.Pref + "_fin_" + (Points.DateOper.Year % 100).ToString("00") +  ".pack_ls_err a where a.nzp_pack_ls = nzp_pack_ls)";
                #else
                    sqlText =
                        " Insert into " + Points.Pref + "_fin_" + (Points.DateOper.Year % 100).ToString("00") + "@" + DBManager.getServer(conn_db) +
                        ":pack_ls_err ( nzp_pack_ls,nzp_err,note ) " +
                        " Select nzp_pack_ls, 6,'Подробности см.в журнале распределения' " +
                        " From t_selkvar " +
                        " Where nzp_kvar in ( Select nzp_kvar From t_opl where kod_info =0 ) and nzp_pack_ls not in (select a.nzp_pack_ls from  " +
                         Points.Pref + "_fin_" + (Points.DateOper.Year % 100).ToString("00") + "@" + DBManager.getServer(conn_db) + ":pack_ls_err a where a.nzp_pack_ls = nzp_pack_ls)";
                #endif
                ret = ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log).GetReturnsType().GetReturns();
                if (!ret.result)
                {
                    DropTempTablesPack(conn_db);
                    return false;
                }
                #endregion


                #region Выставить коды алгоритмов распределения
                #region Стандарнтые алгоритмы
                sqlText =
                    " Update " + packXX.pack_ls +
                    " Set alg = 1, " +
                    " distr_month = '" + distr_month + "'" +
                    " Where nzp_pack_ls in ( Select nzp_pack_ls From t_opl Where kod_info =101 ) and inbasket =0 and dat_uchet is not null ";
                ret = ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log).GetReturnsType().GetReturns();
                if (!ret.result)
                {
                    DropTempTablesPack(conn_db);
                    MessageInPackLog(conn_db, packXX, "Ошибка распределения 2.11", true, out r);
                    return false;
                }
                sqlText =
                    " Update " + packXX.pack_ls +
                    " Set alg = 2, " +
                    " distr_month = '" + distr_month + "'" +
                    " Where nzp_pack_ls in ( Select nzp_pack_ls From t_opl Where kod_info =102 ) and inbasket =0 and dat_uchet is not null ";
                ret = ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log).GetReturnsType().GetReturns();
                if (!ret.result)
                {
                    DropTempTablesPack(conn_db);
                    MessageInPackLog(conn_db, packXX, "Ошибка распределения 2.11", true, out r);
                    return false;
                }
                sqlText =
                    " Update " + packXX.pack_ls +
                    " Set alg = 3, " +
                    " distr_month = '" + distr_month + "'" +
                    " Where nzp_pack_ls in ( Select nzp_pack_ls From t_opl Where kod_info in (103,106,107,108,109) ) and inbasket =0 and dat_uchet is not null ";
                ret = ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log).GetReturnsType().GetReturns();
                if (!ret.result)
                {
                    DropTempTablesPack(conn_db);
                    MessageInPackLog(conn_db, packXX, "Ошибка распределения 2.11", true, out r);
                    return false;
                }

                sqlText =
                " Update " + packXX.pack_ls +
                " Set alg = 4, " +
                " distr_month = '" + distr_month + "'" +
                " Where nzp_pack_ls in ( Select nzp_pack_ls From t_opl Where kod_info =104 ) and inbasket =0 and dat_uchet is not null ";
                ret = ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log).GetReturnsType().GetReturns();
                if (!ret.result)
                {
                    DropTempTablesPack(conn_db);
                    MessageInPackLog(conn_db, packXX, "Ошибка распределения 2.12", true, out r);
                    return false;
                }

                sqlText =
                " Update " + packXX.pack_ls +
                " Set alg = 6, " +
                " distr_month = '" + distr_month + "'" +
                " Where nzp_pack_ls in ( Select nzp_pack_ls From t_opl Where kod_info =114 ) and inbasket =0 and dat_uchet is not null ";
                ret = ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log).GetReturnsType().GetReturns();
                if (!ret.result)
                {
                    DropTempTablesPack(conn_db);
                    MessageInPackLog(conn_db, packXX, "Ошибка распределения 2.12", true, out r);
                    return false;
                }

                sqlText =
                    " Update " + packXX.pack_ls +
                    " Set alg = 7, " +
                    " distr_month = '" + distr_month2 + "'" +
                    " Where nzp_pack_ls in ( Select nzp_pack_ls From t_opl Where kod_info <>0 ) and inbasket =0 and dat_uchet is not null and nzp_pack_ls in (select nzp_pack_ls from t_selkvar where alg = 7)";
                ret = ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log).GetReturnsType().GetReturns();
                if (!ret.result)
                {
                    DropTempTablesPack(conn_db);
                    MessageInPackLog(conn_db, packXX, "Ошибка распределения 2.11", true, out r);
                    return false;
                }

                sqlText =
                    " Update " + packXX.pack_ls +
                    " Set alg = 8, " +
                    " distr_month = '" + distr_month + "'" +
                    " Where nzp_pack_ls in ( Select nzp_pack_ls From t_opl Where kod_info =110 ) and inbasket =0 and dat_uchet is not null ";
                ret = ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log).GetReturnsType().GetReturns();
                if (!ret.result)
                {
                    DropTempTablesPack(conn_db);
                    MessageInPackLog(conn_db, packXX, "Ошибка распределения 2.11", true, out r);
                    return false;
                }

                #endregion Стандарнтые алгоритмы
                #endregion Выставить коды алгоритмов распределения
            }

            if (Constants.Trace) Utility.ClassLog.WriteLog("Распределение завершено. Старт функции LoadLocalPaXX");

            packXX.where_pack_ls = packXX.where_pack_ls.Replace(strWherePackLsIsNotDistrib, strWherePackLsIsDistrib);
            //заполнить локальные fn_pa_xx
            //LoadLocalPaXX(conn_db, packXX, true, out ret);

            // Обновить инорфмацию в fn_distrib_dom_MM

            DateTime dat = Points.DateOper;
            DateTime lastDayDatCalc = Convert.ToDateTime(DateTime.DaysInMonth(Points.DateOper.Year, Points.DateOper.Month).ToString("00") +
                 "." + Points.DateOper.Month.ToString() + "." + Points.DateOper.Year.ToString());
            
            while (dat <= lastDayDatCalc)
            {
                paramcalc.DateOper = dat;
                if (isAutoDistribPaXX)
                {
                    DistribPaXX(conn_db, paramcalc, out ret);
                    if (!ret.result)
                    {
                        DropTempTablesPack(conn_db);
                        return false;
                    }
                }
                else
                {
                    ret = ExecSQL(conn_db, " insert into  " + packXX.fn_operday_log + "(nzp_dom, date_oper) select distinct nzp_dom,'" + dat.ToShortDateString() + "'" + sConvToDate + " from t_selkvar where nzp_dom >0 ", true);
                    break;
                }
                dat = dat.AddDays(1);

            }
            
            //DistribPaXX(conn_db, paramcalc, out ret);

            

            if (Constants.Trace) Utility.ClassLog.WriteLog("Стоп функции LoadLocalPaXX. Старт функции CalcSaldoPack");

            //сразу запустить расчет сальдо по nzp_pack
            MessageInPackLog(conn_db, packXX, "Расчет сальдо лицевых счетов", false, out r);
            ret = CalcChargeXXUchetOplatForPack(conn_db, packXX);
            if (!ret.result)
            {
                DropTempTablesPack(conn_db);
                return false;
            }
            
            MessageInPackLog(conn_db, packXX, "Конец распределения", false, out r);
            DropTempTablesPack(conn_db);

            if (ret2.tag == -1)
            {
                ret.tag = ret2.tag;
                ret.text = ret2.text;
            }
            if (Constants.Trace) Utility.ClassLog.WriteLog("Конец функции CalcPackXX");
            return true;
        }

        
        //-----------------------------------------------------------------------------
        bool OplSamara(IDbConnection conn_db, PackXX packXX, out Returns ret)
        //-----------------------------------------------------------------------------
        {
            DataTable dt;
            DataRow row;

            string s1, s2;
            int count = 0;

            // САМАРА
            ret = Utils.InitReturns();
            Returns r = Utils.InitReturns();

            if (Points.packDistributionParameters.EnableLog) { MessageInPackLog(conn_db, packXX, "Установлена специальная стратегия распределения (по входящему сальдо)", false, out r); }

            ExecSQL(conn_db, " Create index ix2_t_opl on t_opl (kod_info) ", true);

#if PG
            ExecSQL(conn_db, " analyze t_opl ", true);
#else
                ExecSQL(conn_db, " Update statistics for table t_opl ", true);
#endif


            #region Распределить оплаты у которых исходящее сальдо = сумме оплаты внезависимости от знака
            // -------------------------------------------------------------------------------------------------------------------------------------------------------
#if PG
            sqlText = "update t_opl set isum_insaldo = (select coalesce(sum_insaldo,0) from t_itog a where a.nzp_kvar = t_opl.nzp_kvar and a.nzp_pack_ls = t_opl.nzp_pack_ls ),isum_charge = (select coalesce(sum_charge,0) from t_itog a where a.nzp_kvar = t_opl.nzp_kvar and a.nzp_pack_Ls = t_opl.nzp_pack_Ls  )    ";
#else
                    sqlText ="update t_opl set isum_insaldo = (select NVL(sum_insaldo,0) from t_itog a where a.nzp_kvar = t_opl.nzp_kvar and a.nzp_pack_ls = t_opl.nzp_pack_ls ),isum_charge = (select NVL(sum_charge,0) from t_itog a where a.nzp_kvar = t_opl.nzp_kvar and a.nzp_pack_Ls = t_opl.nzp_pack_Ls  )    ";
#endif
            ret = ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log).GetReturnsType().GetReturns();
            if (!ret.result)
            {
                DropTempTablesPack(conn_db);
                MessageInPackLog(conn_db, packXX, "Ошибка распределения 7.2", true, out r);
                Utility.ExceptionUtility.OnException(new Exception(ret.text), System.Reflection.MethodBase.GetCurrentMethod().Name);
                return false;
            }

            sqlText = " Update t_opl " +
                     " Set kod_info = 110, " +
                     "  sum_prih_d = sum_insaldo" + // Погашение долга
                     " where isum_insaldo = (-1)*g_sum_ls and kod_sum in (" + strKodSumForCharge_MM + ",81) and kod_info = 0  and znak=-1";
#if PG
            IntfResultType retres = ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log);
            ret = retres.GetReturnsType().GetReturns();
#else
                    ret = ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log).GetReturnsType().GetReturns();
#endif

            if (!ret.result)
            {
                DropTempTablesPack(conn_db);
                MessageInPackLog(conn_db, packXX, "Ошибка распределения 8.1", true, out r);
                Utility.ExceptionUtility.OnException(new Exception(ret.text), System.Reflection.MethodBase.GetCurrentMethod().Name);
                return false;
            };

            #endregion // ------------------------------------------------------------------------------------------------------------------------------------------
/*
            sqlText = " Update t_opl " +
                     " Set sum_insaldo = 0 " +
                     " where sum_insaldo <0  and kod_sum in (" + strKodSumForCharge_MM + ") and kod_info = 0  and g_sum_ls >0";
            ret = ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log).GetReturnsType().GetReturns();
            if (!ret.result)
            {
                DropTempTablesPack(conn_db);
                MessageInPackLog(conn_db, packXX, "Ошибка распределения 8.1", true, out r);
                Utility.ExceptionUtility.OnException(new Exception(ret.text), System.Reflection.MethodBase.GetCurrentMethod().Name);
                return false;
            }
            sqlText = " Update t_opl " +
                     " Set sum_charge = 0 " +
                     " where sum_charge <0  and kod_sum in (" + strKodSumForCharge_MM + ") and kod_info = 0  and g_sum_ls >0 ";
            ret = ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log).GetReturnsType().GetReturns();
            if (!ret.result)
            {
                DropTempTablesPack(conn_db);
                MessageInPackLog(conn_db, packXX, "Ошибка распределения 8.1", true, out r);
                Utility.ExceptionUtility.OnException(new Exception(ret.text), System.Reflection.MethodBase.GetCurrentMethod().Name);
                return false;
            }
*/

            // Подсчитать откорректированную итоговую сумму исходящего сальдо и начислено к оплате
            sqlText =
            " update t_itog set sum_insaldo = (select sum(sum_insaldo) from t_opl a where a.nzp_kvar = t_itog.nzp_kvar and a.nzp_pack_Ls = t_itog.nzp_pack_Ls ), sum_charge = (select sum(sum_charge) from t_opl a where a.nzp_kvar = t_itog.nzp_kvar and a.nzp_pack_Ls = t_itog.nzp_pack_Ls )";
            //" update t_itog set sum_insaldo = (select sum(sum_insaldo) from t_opl a where a.nzp_kvar = t_itog.nzp_kvar and a.nzp_pack_Ls = t_itog.nzp_pack_Ls and a.sum_insaldo>0), sum_charge = (select sum(sum_charge) from t_opl a where a.nzp_kvar = t_itog.nzp_kvar and a.nzp_pack_Ls = t_itog.nzp_pack_Ls and a.sum_charge>0)";
            ret = ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log).GetReturnsType().GetReturns();
            if (!ret.result)
            {
                DropTempTablesPack(conn_db);
                MessageInPackLog(conn_db, packXX, "Ошибка распределения 7.1", true, out r);
                Utility.ExceptionUtility.OnException(new Exception(ret.text), System.Reflection.MethodBase.GetCurrentMethod().Name);
                return false;
            }
#if PG
            sqlText = "update t_opl set isum_insaldo = (select coalesce(sum_insaldo,0) from t_itog a where a.nzp_kvar = t_opl.nzp_kvar and a.nzp_pack_ls = t_opl.nzp_pack_ls ),isum_charge = (select coalesce(sum_charge,0) from t_itog a where a.nzp_kvar = t_opl.nzp_kvar and a.nzp_pack_Ls = t_opl.nzp_pack_Ls  )    ";
#else
                  sqlText = "update t_opl set isum_insaldo = (select NVL(sum_insaldo,0) from t_itog a where a.nzp_kvar = t_opl.nzp_kvar and a.nzp_pack_ls = t_opl.nzp_pack_ls ),isum_charge = (select NVL(sum_charge,0) from t_itog a where a.nzp_kvar = t_opl.nzp_kvar and a.nzp_pack_Ls = t_opl.nzp_pack_Ls  )    ";
#endif

            ret = ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log).GetReturnsType().GetReturns();
            if (!ret.result)
            {
                DropTempTablesPack(conn_db);
                MessageInPackLog(conn_db, packXX, "Ошибка распределения 7.2", true, out r);
                Utility.ExceptionUtility.OnException(new Exception(ret.text), System.Reflection.MethodBase.GetCurrentMethod().Name);
                return false;
            }


            #region 1. Если входящее сальдо >0 и оплата = Входящему сальдо
            // Пометить такие оплаты
            sqlText = " Update t_opl " +
                     " Set kod_info = 101, " +
                     "  sum_prih_d = sum_insaldo" + // Погашение долга
                     " where isum_insaldo = g_sum_ls and kod_sum in (" + strKodSumForCharge_MM + ",81) and kod_info = 0  ";
#if PG
            
                IntfResultType retres2 = ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log);
                ret = retres2.GetReturnsType().GetReturns();
            
#else
                   ret = ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log).GetReturnsType().GetReturns();
#endif
            if (!ret.result)
            {
                DropTempTablesPack(conn_db);
                MessageInPackLog(conn_db, packXX, "Ошибка распределения 8.1", true, out r);
                Utility.ExceptionUtility.OnException(new Exception(ret.text), System.Reflection.MethodBase.GetCurrentMethod().Name);
                return false;
            }
            else
            {
#if PG
                count = retres.resultAffectedRows;
#else
                  count = ClassDBUtils.GetAffectedRowsCount(conn_db);
#endif

                if (Points.packDistributionParameters.EnableLog & count > 0 & packXX.nzp_pack_ls > 0)
                {
                    sqlText = "select SUM(sum_prih_d) sum_1 from t_opl where kod_info=101  and nzp_pack_ls=" + packXX.nzp_pack_ls;
                    dt = ClassDBUtils.OpenSQL(sqlText, conn_db).GetData();
                    row = dt.Rows[0];
                    s1 = Convert.ToString(row["sum_1"]);
                    if (s1.Trim() != "0")
                    {
                        MessageInPackLog(conn_db, packXX, "Алгоритм распределения № 1 (оплачено " + s1.Trim() + " руб) = входящее сальдо (" + s1.Trim() + " руб)", true, out r);
                    }
                }
            }
            #endregion 1.

            #region 2. Если входящее сальдо >0 и оплата < Входящего сальдо
            // 2 Пометить такие оплаты
#if PG
            sqlText = " Update t_opl " +
                         " Set kod_info = 102, " +
                         "      sum_prih_d = " + Points.Pref + "_kernel.getSumPrih(sum_insaldo,  g_sum_ls * (sum_insaldo/isum_insaldo), isdel )  " +
                         " where isum_insaldo > g_sum_ls and kod_sum in (" + strKodSumForCharge_MM + ",81) and kod_info = 0  and isum_insaldo<>0 ";

            retres = ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log);
            ret = retres.GetReturnsType().GetReturns();
#else
                   sqlText = " Update t_opl " +
                     " Set kod_info = 102, " +
                     "      sum_prih_d = " + Points.Pref + "_kernel:getSumPrih(sum_insaldo,  g_sum_ls * (sum_insaldo/isum_insaldo), isdel )  " +
                     " where isum_insaldo > g_sum_ls and kod_sum in (" + strKodSumForCharge_MM + ",81) and kod_info = 0  and isum_insaldo<>0 ";
                   ret = ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log).GetReturnsType().GetReturns();
#endif

            if (!ret.result)
            {
                DropTempTablesPack(conn_db);
                MessageInPackLog(conn_db, packXX, "Ошибка распределения 8.2", true, out r);
                Utility.ExceptionUtility.OnException(new Exception(ret.text), System.Reflection.MethodBase.GetCurrentMethod().Name);
                return false;
            }
            else
            {
#if PG
                count = retres.resultAffectedRows;
#else
                count = ClassDBUtils.GetAffectedRowsCount(conn_db);
#endif

                if (Points.packDistributionParameters.EnableLog & count > 0 & packXX.nzp_pack_ls > 0)
                {
                    sqlText = "select g_sum_ls, isum_insaldo  from t_opl where nzp_pack_ls=" + packXX.nzp_pack_ls;
                    dt = ClassDBUtils.OpenSQL(sqlText, conn_db).GetData();
                    row = dt.Rows[0];
                    s1 = Convert.ToString(row["g_sum_ls"]);
                    s2 = Convert.ToString(row["isum_insaldo"]);
                    if (s1.Trim() != "0")
                    {
                        MessageInPackLog(conn_db, packXX, "Алгоритм распределения № 2 (оплачено " + s1.Trim() + " руб < входящее сальдо " + s2.Trim() + ")", true, out r);
                        sqlText = "select SUM(sum_prih_d) as sum_1, sum(sum_insaldo) as sum_2  from t_opl where kod_info=102  and nzp_pack_ls=" + packXX.nzp_pack_ls;
                        dt = ClassDBUtils.OpenSQL(sqlText, conn_db).GetData();
                        row = dt.Rows[0];
                        s1 = Convert.ToString(row["sum_1"]);
                        s2 = Convert.ToString(row["sum_2"]);
                        if (s2.Trim() != "0")
                        {
                            MessageInPackLog(conn_db, packXX, "Из " + s2 + " руб входящего сальдо погашено " + s1 + " руб", false, out r);
                        }
                    }

                }
            }
            #endregion 2.

            #region 3. Если входящее сальдо >0 и оплата > Входящего сальдо
            // 2 Пометить такие оплаты
            sqlText = " Update t_opl " +
                     " Set kod_info = 103 " +
                     " where isum_insaldo < g_sum_ls and kod_sum in (" + strKodSumForCharge_MM + ",81) and kod_info = 0 and isum_insaldo >0 ";
#if PG
            retres = ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log);
            ret = retres.GetReturnsType().GetReturns();
#else
                    ret = ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log).GetReturnsType().GetReturns();
#endif
            if (!ret.result)
            {
                DropTempTablesPack(conn_db);
                MessageInPackLog(conn_db, packXX, "Ошибка распределения 8.3", true, out r);
                Utility.ExceptionUtility.OnException(new Exception(ret.text), System.Reflection.MethodBase.GetCurrentMethod().Name);
                return false;
            }
            else
            {
#if PG
                count = retres.resultAffectedRows;
#else
                    count = ClassDBUtils.GetAffectedRowsCount(conn_db);
#endif
                if (Points.packDistributionParameters.EnableLog & count > 0 & packXX.nzp_pack_ls > 0)
                {
                    sqlText = "select g_sum_ls, isum_insaldo  from t_opl where nzp_pack_ls=" + packXX.nzp_pack_ls;
                    dt = ClassDBUtils.OpenSQL(sqlText, conn_db).GetData();
                    row = dt.Rows[0];
                    s1 = Convert.ToString(row["g_sum_ls"]);
                    s2 = Convert.ToString(row["isum_insaldo"]);
                    if (s1.Trim() != "0")
                    {
                        MessageInPackLog(conn_db, packXX, "Алгоритм распределения № 3 (оплачено " + s1.Trim() + " руб > входящее сальдо " + s2.Trim() + ")", true, out r);
                    }
                }
            }

            // Погасить входящее сальдо
            sqlText = " Update t_opl " +
                     " Set sum_prih_d  = sum_insaldo " +
                     " where kod_sum in (" + strKodSumForCharge_MM + ",81) and kod_info = 103 ";//"and sum_insaldo >0  ";
#if PG
            retres = ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log);
            ret = retres.GetReturnsType().GetReturns();
#else
                    ret = ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log).GetReturnsType().GetReturns();
#endif
            if (!ret.result)
            {
                DropTempTablesPack(conn_db);
                MessageInPackLog(conn_db, packXX, "Ошибка распределения 8.4", true, out r);
                Utility.ExceptionUtility.OnException(new Exception(ret.text), System.Reflection.MethodBase.GetCurrentMethod().Name);
                return false;
            }
            else
            {
#if PG
                count = retres.resultAffectedRows;
#else
                    count = ClassDBUtils.GetAffectedRowsCount(conn_db);
#endif
                if (Points.packDistributionParameters.EnableLog & count > 0 & packXX.nzp_pack_ls > 0)
                {
                    sqlText = "select SUM(sum_prih_d) as sum_1, sum(sum_insaldo) as sum_2  from t_opl where kod_info=103  and nzp_pack_ls=" + packXX.nzp_pack_ls;
                    dt = ClassDBUtils.OpenSQL(sqlText, conn_db).GetData();
                    row = dt.Rows[0];
                    s1 = Convert.ToString(row["sum_1"]);
                    s2 = Convert.ToString(row["sum_2"]);
                    if (s2.Trim() != "0")
                    {
                        MessageInPackLog(conn_db, packXX, "Погашение входящего сальдо " + s2 + " руб. Погашено " + s1 + " руб", false, out r);
                    }
                }
            }

            // Определить остаток от погашения
#if PG
            sqlText = " update t_ostatok set g_sum_ls_ost = " + Points.Pref + "_kernel.getSumOstatok(nzp_pack_ls)  where nzp_pack_ls  in (select distinct nzp_pack_ls from t_opl where kod_info = 0)   ";
#else
                    sqlText = " update t_ostatok set g_sum_ls_ost = " + Points.Pref + "_kernel:getSumOstatok(nzp_pack_ls)   ";
#endif
            ret = ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log).GetReturnsType().GetReturns();
            if (!ret.result)
            {
                DropTempTablesPack(conn_db);
                MessageInPackLog(conn_db, packXX, "Ошибка распределения 8.4", true, out r);
                Utility.ExceptionUtility.OnException(new Exception(ret.text), System.Reflection.MethodBase.GetCurrentMethod().Name);
                return false;
            }
            else
            {
                if (Points.packDistributionParameters.EnableLog & count > 0 & packXX.nzp_pack_ls > 0)
                {
                    sqlText = "select g_sum_ls_ost as sum_1 from t_ostatok where  nzp_pack_ls=" + packXX.nzp_pack_ls;
                    dt = ClassDBUtils.OpenSQL(sqlText, conn_db).GetData();
                    row = dt.Rows[0];
                    s1 = Convert.ToString(row["sum_1"]);
                    if (s1.Trim() != "0")
                    {
                        MessageInPackLog(conn_db, packXX, "Остаток от погашения " + s1 + " руб распределить по 'Начислено к оплате'", false, out r);
                    }
                }
            }



            sqlText = " Update t_opl " +
                                " SET  g_sum_ls_ost = (select g_sum_ls_ost from t_ostatok where t_ostatok.nzp_pack_ls = t_opl.nzp_pack_ls)" +
                                " where  kod_sum in (" + strKodSumForCharge_MM + ",81) and kod_info in (103,0)  and sum_charge>0";
            ret = ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log).GetReturnsType().GetReturns();
            if (!ret.result)
            {
                DropTempTablesPack(conn_db);
                MessageInPackLog(conn_db, packXX, "Ошибка распределения 8.5", true, out r);
                Utility.ExceptionUtility.OnException(new Exception(ret.text), System.Reflection.MethodBase.GetCurrentMethod().Name);
                return false;
            }

            // Пометить оплаты по которым необходимо распределить по авансовую сумму
            sqlText = " update t_opl set kod_info = 106 where g_sum_ls_ost>0 and kod_info in (0,103)   ";
            ret = ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log).GetReturnsType().GetReturns();
            if (!ret.result)
            {
                DropTempTablesPack(conn_db);
                MessageInPackLog(conn_db, packXX, "Ошибка распределения 8.4", true, out r);
                Utility.ExceptionUtility.OnException(new Exception(ret.text), System.Reflection.MethodBase.GetCurrentMethod().Name);
                return false;
            }

#if PG
            sqlText =
                       " Update t_opl " +
                       " Set  " +
                        "      sum_prih_a = " + Points.Pref + "_kernel.getSumPrih(sum_charge,  g_sum_ls_ost * (sum_charge/isum_charge), isdel )  " +
                       " where  g_sum_ls_ost > 0 and kod_sum in (" + strKodSumForCharge_MM + ",81) and kod_info in (103,106)  and sum_charge>0";
#else
            sqlText =
                    " Update t_opl " +
                    " Set  " +
                     "      sum_prih_a = " + Points.Pref + "_kernel:getSumPrih(sum_charge,  g_sum_ls_ost * (sum_charge/isum_charge), isdel )  " +
                    " where  g_sum_ls_ost > 0 and kod_sum in (" + strKodSumForCharge_MM + ",81) and kod_info in (103,106)  and sum_charge>0";
#endif
            ret = ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log).GetReturnsType().GetReturns();
            if (!ret.result)
            {
                DropTempTablesPack(conn_db);
                MessageInPackLog(conn_db, packXX, "Ошибка распределения 8.6", true, out r);
                Utility.ExceptionUtility.OnException(new Exception(ret.text), System.Reflection.MethodBase.GetCurrentMethod().Name);
                return false;
            }
            else
            {
                if (Points.packDistributionParameters.EnableLog & count > 0 & packXX.nzp_pack_ls > 0)
                {
                    sqlText = "select SUM(sum_prih_a) as sum_1  from t_opl where kod_info in (103,106)  and nzp_pack_ls=" + packXX.nzp_pack_ls;
                    dt = ClassDBUtils.OpenSQL(sqlText, conn_db).GetData();
                    row = dt.Rows[0];
                    s1 = Convert.ToString(row["sum_1"]);
                    if (s1.Trim() != "0")
                    {
                        MessageInPackLog(conn_db, packXX, "Остаток (" + s1 + " руб) распределён", false, out r);
                    }
                }
            }
            // Определить остаток от погашения
#if PG
            sqlText = " update t_ostatok set g_sum_ls_ost = " + Points.Pref + "_kernel.getSumOstatok(nzp_pack_ls)  ";
#else
                    sqlText = " update t_ostatok set g_sum_ls_ost = " + Points.Pref + "_kernel:getSumOstatok(nzp_pack_ls)  ";
#endif
            ret = ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log).GetReturnsType().GetReturns();
            if (!ret.result)
            {
                DropTempTablesPack(conn_db);
                MessageInPackLog(conn_db, packXX, "Ошибка распределения 8.4", true, out r);
                Utility.ExceptionUtility.OnException(new Exception(ret.text), System.Reflection.MethodBase.GetCurrentMethod().Name);
                return false;
            }
            else
            {
                if (Points.packDistributionParameters.EnableLog & count > 0 & packXX.nzp_pack_ls > 0)
                {
                    sqlText = "select g_sum_ls_ost as sum_1 from t_ostatok where  nzp_pack_ls=" + packXX.nzp_pack_ls;
                    dt = ClassDBUtils.OpenSQL(sqlText, conn_db).GetData();
                    row = dt.Rows[0];
                    s1 = Convert.ToString(row["sum_1"]);
                    if (s1.Trim() != "0")
                    {
                        MessageInPackLog(conn_db, packXX, "Остаток от погашения " + s1 + " руб распределить по 'Входящему сальдо'", false, out r);
                    }
                }
            }

            sqlText = " Update t_opl " +
                                " SET  g_sum_ls_ost = (select g_sum_ls_ost from t_ostatok where t_ostatok.nzp_pack_ls = t_opl.nzp_pack_ls)" +
                                " where  kod_sum in (" + strKodSumForCharge_MM + ",81) and kod_info in (103,106)  and sum_insaldo>0";
            ret = ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log).GetReturnsType().GetReturns();
            if (!ret.result)
            {
                DropTempTablesPack(conn_db);
                MessageInPackLog(conn_db, packXX, "Ошибка распределения 8.5", true, out r);
                Utility.ExceptionUtility.OnException(new Exception(ret.text), System.Reflection.MethodBase.GetCurrentMethod().Name);
                return false;
            }

#if PG
            sqlText =
                  " Update t_opl " +
                  " Set  " +
                    "     sum_prih_a = sum_prih_a+" + Points.Pref + "_kernel.getSumPrih(sum_insaldo, g_sum_ls_ost * (sum_insaldo/isum_insaldo), isdel )  " +

                  " where  g_sum_ls_ost > 0 and kod_sum in (" + strKodSumForCharge_MM + ",81) and kod_info in (103,106)  and sum_insaldo>0";
#else
            sqlText =
                    " Update t_opl " +
                    " Set  " +
                      "     sum_prih_a = sum_prih_a+" + Points.Pref + "_kernel:getSumPrih(sum_insaldo, g_sum_ls_ost * (sum_insaldo/isum_insaldo), isdel )  " +
                    " where  g_sum_ls_ost > 0 and kod_sum in (" + strKodSumForCharge_MM + ",81) and kod_info in (103,106)  and sum_insaldo>0";
#endif
            ret = ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log).GetReturnsType().GetReturns();

            if (!ret.result)
            {
                DropTempTablesPack(conn_db);
                MessageInPackLog(conn_db, packXX, "Ошибка распределения 8.6", true, out r);
                Utility.ExceptionUtility.OnException(new Exception(ret.text), System.Reflection.MethodBase.GetCurrentMethod().Name);
                return false;
            }
            else
            {
                if (Points.packDistributionParameters.EnableLog & count > 0 & packXX.nzp_pack_ls > 0)
                {
                    sqlText = "select SUM(sum_prih_a) as sum_1  from t_opl where kod_info in (103,106)  and nzp_pack_ls=" + packXX.nzp_pack_ls;
                    dt = ClassDBUtils.OpenSQL(sqlText, conn_db).GetData();
                    row = dt.Rows[0];
                    s1 = Convert.ToString(row["sum_1"]);
                    if (s1.Trim() != "0")
                    {
                        MessageInPackLog(conn_db, packXX, "Остаток (" + s1 + " руб) распределён", false, out r);
                    }
                }
            }

            #endregion 3.

            // Если ещё остались нераспределённые оплаты, 
            #region 4. Распределить остаток то распределить всё по первоначальной сумме входящего сальдо

            // Пометить оплаты по которым необходимо распределить по первоначальной сумме входящего сальдо
            sqlText = " update t_opl set kod_info = 107, sum_insaldo= sum_insaldop where  kod_info = 0   and sum_insaldo>0 ";
            ret = ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log).GetReturnsType().GetReturns();
            if (!ret.result)
            {
                DropTempTablesPack(conn_db);
                MessageInPackLog(conn_db, packXX, "Ошибка распределения 8.7", true, out r);
                Utility.ExceptionUtility.OnException(new Exception(ret.text), System.Reflection.MethodBase.GetCurrentMethod().Name);
                return false;
            }

            // Подсчитать откорректированную итоговую сумму исходящего сальдо и начислено к оплате
            sqlText =
            " update t_itog set sum_insaldo = (select sum(sum_insaldop) from t_opl a where a.nzp_kvar = t_itog.nzp_kvar and a.nzp_pack_Ls = t_itog.nzp_pack_Ls and a.sum_insaldo>0)";
            ret = ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log).GetReturnsType().GetReturns();
            if (!ret.result)
            {
                DropTempTablesPack(conn_db);
                MessageInPackLog(conn_db, packXX, "Ошибка распределения 8.8", true, out r);
                Utility.ExceptionUtility.OnException(new Exception(ret.text), System.Reflection.MethodBase.GetCurrentMethod().Name);
                return false;
            }
            sqlText =
                "update t_opl set isum_insaldo = (select sum_insaldo from t_itog a where a.nzp_kvar = t_opl.nzp_kvar and a.nzp_pack_ls = t_opl.nzp_pack_ls ) ";
            ret = ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log).GetReturnsType().GetReturns();
            if (!ret.result)
            {
                DropTempTablesPack(conn_db);
                MessageInPackLog(conn_db, packXX, "Ошибка распределения 8.9", true, out r);
                Utility.ExceptionUtility.OnException(new Exception(ret.text), System.Reflection.MethodBase.GetCurrentMethod().Name);
                return false;
            }


            // Определить остаток от погашения
#if PG
            sqlText = " update t_ostatok set g_sum_ls_ost = " + Points.Pref + "_kernel.getSumOstatok(nzp_pack_ls)  ";
#else
                sqlText = " update t_ostatok set g_sum_ls_ost = " + Points.Pref + "_kernel:getSumOstatok(nzp_pack_ls)  ";
#endif
            ret = ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log).GetReturnsType().GetReturns();
            if (!ret.result)
            {
                DropTempTablesPack(conn_db);
                MessageInPackLog(conn_db, packXX, "Ошибка распределения 8.10", true, out r);
                Utility.ExceptionUtility.OnException(new Exception(ret.text), System.Reflection.MethodBase.GetCurrentMethod().Name);
                return false;
            }
            else
            {
                if (Points.packDistributionParameters.EnableLog & count > 0 & packXX.nzp_pack_ls > 0)
                {
                    sqlText = "select g_sum_ls_ost as sum_1 from t_ostatok where  nzp_pack_ls=" + packXX.nzp_pack_ls;
                    dt = ClassDBUtils.OpenSQL(sqlText, conn_db).GetData();
                    row = dt.Rows[0];
                    s1 = Convert.ToString(row["sum_1"]);
                    if (s1.Trim() != "0")
                    {
                        MessageInPackLog(conn_db, packXX, "Остаток от погашения " + s1 + " руб распределить по первоначальной сумме 'Входящее сальдо'", false, out r);
                    }
                }
            }

            sqlText = " Update t_opl " +
                                " SET  g_sum_ls_ost = (select g_sum_ls_ost from t_ostatok where t_ostatok.nzp_pack_ls = t_opl.nzp_pack_ls)" +
                                " where  kod_sum in (" + strKodSumForCharge_MM + ",81) and kod_info in (107)  ";
            ret = ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log).GetReturnsType().GetReturns();
            if (!ret.result)
            {
                DropTempTablesPack(conn_db);
                MessageInPackLog(conn_db, packXX, "Ошибка распределения 8.11", true, out r);
                Utility.ExceptionUtility.OnException(new Exception(ret.text), System.Reflection.MethodBase.GetCurrentMethod().Name);
                return false;
            }

#if PG
            sqlText =
                " Update t_opl " +
                " Set  " +
                  "     sum_prih_s = " + Points.Pref + "_kernel.getSumPrih(sum_insaldop, g_sum_ls_ost * (sum_insaldop/isum_insaldo), isdel )  " +
                " where  g_sum_ls_ost > 0 and kod_sum in (" + strKodSumForCharge_MM + ",81) and kod_info in (107)  ";
#else
                sqlText =
                    " Update t_opl " +
                    " Set  " +
                      "     sum_prih_s = " + Points.Pref + "_kernel:getSumPrih(sum_insaldop, g_sum_ls_ost * (sum_insaldop/isum_insaldo), isdel )  " +
                    " where  g_sum_ls_ost > 0 and kod_sum in (" + strKodSumForCharge_MM + ",81) and kod_info in (107)  ";
#endif
            ret = ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log).GetReturnsType().GetReturns();
            if (!ret.result)
            {
                DropTempTablesPack(conn_db);
                MessageInPackLog(conn_db, packXX, "Ошибка распределения 8.12", true, out r);
                Utility.ExceptionUtility.OnException(new Exception(ret.text), System.Reflection.MethodBase.GetCurrentMethod().Name);
                return false;
            }
            else
            {
                if (Points.packDistributionParameters.EnableLog & count > 0 & packXX.nzp_pack_ls > 0)
                {
                    sqlText = "select SUM(sum_prih_s) as sum_1  from t_opl where kod_info in (107)  and nzp_pack_ls=" + packXX.nzp_pack_ls;
                    dt = ClassDBUtils.OpenSQL(sqlText, conn_db).GetData();
                    row = dt.Rows[0];
                    s1 = Convert.ToString(row["sum_1"]);
                    if (s1.Trim() != "0")
                    {
                        MessageInPackLog(conn_db, packXX, "Остаток (" + s1 + " руб) распределён", false, out r);
                    }
                }
            }

            #endregion 4.


            #region 5. Распределить оплаты у которых откорректированное входящее сальдо <0, но есть положительное входящее сальдо

            // Пометить оплаты по которым необходимо распределить по первоначальной сумме входящего сальдо
            sqlText = " update t_opl set kod_info = 108, sum_insaldo= sum_insaldop where  kod_info = 0 and sum_insaldop>0 ";
            ret = ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log).GetReturnsType().GetReturns();
            if (!ret.result)
            {
                DropTempTablesPack(conn_db);
                MessageInPackLog(conn_db, packXX, "Ошибка распределения 8.15", true, out r);
                Utility.ExceptionUtility.OnException(new Exception(ret.text), System.Reflection.MethodBase.GetCurrentMethod().Name);
                return false;
            }

            // Подсчитать откорректированную итоговую сумму исходящего сальдо и начислено к оплате
            sqlText =
            " update t_itog set sum_insaldo = (select sum(sum_insaldo) from t_opl a where a.nzp_kvar = t_itog.nzp_kvar and a.nzp_pack_Ls = t_itog.nzp_pack_Ls and a.sum_insaldop>0)";
            ret = ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log).GetReturnsType().GetReturns();
            if (!ret.result)
            {
                DropTempTablesPack(conn_db);
                MessageInPackLog(conn_db, packXX, "Ошибка распределения 8.16", true, out r);
                Utility.ExceptionUtility.OnException(new Exception(ret.text), System.Reflection.MethodBase.GetCurrentMethod().Name);
                return false;
            }
            sqlText =
                "update t_opl set isum_insaldo = (select sum_insaldo from t_itog a where a.nzp_kvar = t_opl.nzp_kvar and a.nzp_pack_ls = t_opl.nzp_pack_ls ) where  kod_info = 108";
            ret = ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log).GetReturnsType().GetReturns();
            if (!ret.result)
            {
                DropTempTablesPack(conn_db);
                MessageInPackLog(conn_db, packXX, "Ошибка распределения 8.17", true, out r);
                Utility.ExceptionUtility.OnException(new Exception(ret.text), System.Reflection.MethodBase.GetCurrentMethod().Name);
                return false;
            }


            // Определить остаток от погашения
#if PG
            sqlText = " update t_ostatok set g_sum_ls_ost = " + Points.Pref + "_kernel.getSumOstatok(nzp_pack_ls)  ";
#else
                sqlText = " update t_ostatok set g_sum_ls_ost = " + Points.Pref + "_kernel:getSumOstatok(nzp_pack_ls)  ";
#endif
            ret = ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log).GetReturnsType().GetReturns();
            if (!ret.result)
            {
                DropTempTablesPack(conn_db);
                MessageInPackLog(conn_db, packXX, "Ошибка распределения 8.18", true, out r);
                Utility.ExceptionUtility.OnException(new Exception(ret.text), System.Reflection.MethodBase.GetCurrentMethod().Name);
                return false;
            }
            else
            {
                if (Points.packDistributionParameters.EnableLog & count > 0 & packXX.nzp_pack_ls > 0)
                {
                    sqlText = "select g_sum_ls_ost as sum_1 from t_ostatok where  nzp_pack_ls=" + packXX.nzp_pack_ls;
                    dt = ClassDBUtils.OpenSQL(sqlText, conn_db).GetData();
                    row = dt.Rows[0];
                    s1 = Convert.ToString(row["sum_1"]);
                    if (s1.Trim() != "0")
                    {
                        MessageInPackLog(conn_db, packXX, "Остаток от погашения " + s1 + " руб распределить по первоначальной сумме 'Входящее сальдо'", false, out r);
                    }
                }
            }

            sqlText = " Update t_opl " +
                                " SET  g_sum_ls_ost = (select g_sum_ls_ost from t_ostatok where t_ostatok.nzp_pack_ls = t_opl.nzp_pack_ls)" +
                                " where  kod_sum in (" + strKodSumForCharge_MM + ",81) and kod_info in (108)  ";
            ret = ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log).GetReturnsType().GetReturns();
            if (!ret.result)
            {
                DropTempTablesPack(conn_db);
                MessageInPackLog(conn_db, packXX, "Ошибка распределения 8.19", true, out r);
                Utility.ExceptionUtility.OnException(new Exception(ret.text), System.Reflection.MethodBase.GetCurrentMethod().Name);
                return false;
            }

#if PG
            sqlText =
                " Update t_opl " +
                " Set  " +
                 "     sum_prih_s = " + Points.Pref + "_kernel.getSumPrih(sum_insaldop, g_sum_ls_ost * (sum_insaldop/isum_insaldo), isdel )  " +
                " where  g_sum_ls_ost > 0 and kod_sum in (" + strKodSumForCharge_MM + ",81) and kod_info in (108)  ";
#else
                sqlText =
                    " Update t_opl " +
                    " Set  " +
                     "     sum_prih_s = " + Points.Pref + "_kernel:getSumPrih(sum_insaldop, g_sum_ls_ost * (sum_insaldop/isum_insaldo), isdel )  " +
                    " where  g_sum_ls_ost > 0 and kod_sum in (" + strKodSumForCharge_MM + ",81) and kod_info in (108)  ";
#endif
            ret = ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log).GetReturnsType().GetReturns();
            if (!ret.result)
            {
                DropTempTablesPack(conn_db);
                MessageInPackLog(conn_db, packXX, "Ошибка распределения 8.20", true, out r);
                Utility.ExceptionUtility.OnException(new Exception(ret.text), System.Reflection.MethodBase.GetCurrentMethod().Name);
                return false;
            }
            else
            {
                if (Points.packDistributionParameters.EnableLog & count > 0 & packXX.nzp_pack_ls > 0)
                {
                    sqlText = "select SUM(sum_prih_s) as sum_1  from t_opl where kod_info in (108)  and nzp_pack_ls=" + packXX.nzp_pack_ls;
                    dt = ClassDBUtils.OpenSQL(sqlText, conn_db).GetData();
                    row = dt.Rows[0];
                    s1 = Convert.ToString(row["sum_1"]);
                    if (s1.Trim() != "0")
                    {
                        MessageInPackLog(conn_db, packXX, "Остаток (" + s1 + " руб) распределён по первоначальному входящему сальдо (положительной части)", false, out r);
                    }
                }
            }
            #endregion 5.

            #region 6. Распределить оплаты у которых откорректированное входящее сальдо <0, но есть положительное начислено к оплате

            // Пометить оплаты по которым необходимо распределить по первоначальной сумме входящего сальдо
            sqlText = " update t_opl set kod_info = 109, sum_charge= sum_charge_prev where  kod_info = 0 and sum_charge_prev>0 ";
            ret = ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log).GetReturnsType().GetReturns();
            if (!ret.result)
            {
                DropTempTablesPack(conn_db);
                MessageInPackLog(conn_db, packXX, "Ошибка распределения 8.21", true, out r);
                Utility.ExceptionUtility.OnException(new Exception(ret.text), System.Reflection.MethodBase.GetCurrentMethod().Name);
                return false;
            }

            // Подсчитать откорректированную итоговую сумму исходящего сальдо и начислено к оплате
            sqlText =
            " update t_itog set sum_charge = (select sum(sum_charge) from t_opl a where a.nzp_kvar = t_itog.nzp_kvar and a.nzp_pack_Ls = t_itog.nzp_pack_Ls and a.sum_charge_prev>0)";
            ret = ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log).GetReturnsType().GetReturns();
            if (!ret.result)
            {
                DropTempTablesPack(conn_db);
                MessageInPackLog(conn_db, packXX, "Ошибка распределения 8.22", true, out r);
                Utility.ExceptionUtility.OnException(new Exception(ret.text), System.Reflection.MethodBase.GetCurrentMethod().Name);
                return false;
            }
            sqlText =
                "update t_opl set isum_charge = (select sum_charge from t_itog a where a.nzp_kvar = t_opl.nzp_kvar and a.nzp_pack_ls = t_opl.nzp_pack_ls ) where kod_info = 109";
            ret = ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log).GetReturnsType().GetReturns();
            if (!ret.result)
            {
                DropTempTablesPack(conn_db);
                MessageInPackLog(conn_db, packXX, "Ошибка распределения 8.17", true, out r);
                Utility.ExceptionUtility.OnException(new Exception(ret.text), System.Reflection.MethodBase.GetCurrentMethod().Name);
                return false;
            }


            // Определить остаток от погашения
#if PG
            sqlText = " update t_ostatok set g_sum_ls_ost = " + Points.Pref + "_kernel.getSumOstatok(nzp_pack_ls)  ";
#else
                sqlText = " update t_ostatok set g_sum_ls_ost = " + Points.Pref + "_kernel:getSumOstatok(nzp_pack_ls)  ";
#endif
            ret = ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log).GetReturnsType().GetReturns();
            if (!ret.result)
            {
                DropTempTablesPack(conn_db);
                MessageInPackLog(conn_db, packXX, "Ошибка распределения 8.23", true, out r);
                Utility.ExceptionUtility.OnException(new Exception(ret.text), System.Reflection.MethodBase.GetCurrentMethod().Name);
                return false;
            }
            else
            {
                if (Points.packDistributionParameters.EnableLog & count > 0 & packXX.nzp_pack_ls > 0)
                {
                    sqlText = "select g_sum_ls_ost as sum_1 from t_ostatok where  nzp_pack_ls=" + packXX.nzp_pack_ls;
                    dt = ClassDBUtils.OpenSQL(sqlText, conn_db).GetData();
                    row = dt.Rows[0];
                    s1 = Convert.ToString(row["sum_1"]);
                    if (s1.Trim() != "0")
                    {
                        MessageInPackLog(conn_db, packXX, "Остаток от погашения " + s1 + " руб распределить по первоначальной сумме 'Начислено к оплате'", false, out r);
                    }
                }
            }
            sqlText = " Update t_opl " +
                                " SET  g_sum_ls_ost = (select g_sum_ls_ost from t_ostatok where t_ostatok.nzp_pack_ls = t_opl.nzp_pack_ls)" +
                                " where  kod_sum in (" + strKodSumForCharge_MM + ",81) and kod_info in (109)  ";
            ret = ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log).GetReturnsType().GetReturns();
            if (!ret.result)
            {
                DropTempTablesPack(conn_db);
                MessageInPackLog(conn_db, packXX, "Ошибка распределения 8.24", true, out r);
                Utility.ExceptionUtility.OnException(new Exception(ret.text), System.Reflection.MethodBase.GetCurrentMethod().Name);
                return false;
            }

#if PG
            sqlText =
                " Update t_opl " +
                " Set  " +
                 "     sum_prih_s = " + Points.Pref + "_kernel.getSumPrih(sum_charge, g_sum_ls_ost * (sum_charge/isum_charge), isdel )  " +
                " where  g_sum_ls_ost > 0 and kod_sum in (" + strKodSumForCharge_MM + ",81) and kod_info in (109)  ";
#else
                sqlText =
                    " Update t_opl " +
                    " Set  " +
                     "     sum_prih_s = " + Points.Pref + "_kernel:getSumPrih(sum_charge, g_sum_ls_ost * (sum_charge/isum_charge), isdel )  " +
                    " where  g_sum_ls_ost > 0 and kod_sum in (" + strKodSumForCharge_MM + ",81) and kod_info in (109)  ";
#endif
            ret = ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log).GetReturnsType().GetReturns();
            if (!ret.result)
            {
                DropTempTablesPack(conn_db);
                MessageInPackLog(conn_db, packXX, "Ошибка распределения 8.25", true, out r);
                Utility.ExceptionUtility.OnException(new Exception(ret.text), System.Reflection.MethodBase.GetCurrentMethod().Name);
                return false;
            }
            else
            {
                if (Points.packDistributionParameters.EnableLog & count > 0 & packXX.nzp_pack_ls > 0)
                {
                    sqlText = "select SUM(sum_prih_s) as sum_1  from t_opl where kod_info in (109)  and nzp_pack_ls=" + packXX.nzp_pack_ls;
                    dt = ClassDBUtils.OpenSQL(sqlText, conn_db).GetData();
                    row = dt.Rows[0];
                    s1 = Convert.ToString(row["sum_1"]);
                    if (s1.Trim() != "0")
                    {
                        MessageInPackLog(conn_db, packXX, "Остаток (" + s1 + " руб) распределён по первоначальному начислено к оплате (положительной части)", false, out r);
                    }
                }
            }
            #endregion 6.

            // Подсчитать итоговые суммы оплаты
            ret = ExecSQL(conn_db,
                " Update t_opl " +
                " Set sum_prih = sum_prih_d+sum_prih_u+sum_prih_a+sum_prih_s  " +
                " Where kod_info in (101,102,103,106,107,108,109,110) "
                , true);
            if (!ret.result)
            {
                DropTempTablesPack(conn_db);
                MessageInPackLog(conn_db, packXX, "Ошибка распределения 8.99", true, out r);
                return false;
            }
            return true;
        }
        //-----------------------------------------------------------------------------
        bool StandartReparation(IDbConnection conn_db, PackXX packXX, out Returns ret)
        //-----------------------------------------------------------------------------
        {
            // Cтандартная схема, погашение действующих поставщиков приоритетно
            Returns r = Utils.InitReturns();
            string sqlText;
            string s1, s2;
            DataTable dt;
            DataRow row;

             
            string Koeff1 = "sum_charge/isum_charge";
            string Koeff2 = "sum_charge/isum_charge";

            string isdel_first = "(0,1)";
            string isdel_second = "(0,1)";

            string str_where_for_Koeff1 = " isum_charge<>0 ";
            string str_where_for_Koeff2 = " isum_charge<>0 ";


            if (isDistributionForInSaldo)
            {
                MessageInPackLog(conn_db, packXX, "Выбрана схема распределения по исходящему сальдо", false, out r);
            }
            else
            {
                MessageInPackLog(conn_db, packXX, "Выбрана схема распределения по начислено к оплате", false, out r);
            }

            //if (Points.packDistributionParameters.EnableLog) { MessageInPackLog(conn_db, packXX, "Установлена стратегия распределения равноправно на действующих и недействующих поставщиков", false, out r); }

            if (Points.packDistributionParameters.strategy == PackDistributionParameters.Strategies.InactiveServicesFirst)
            {
                Koeff1 = "sum_charge/isum_charge_isdel_1";
                Koeff2 = "sum_charge/isum_charge_isdel_0";

                str_where_for_Koeff1 = " isum_charge_isdel_1<>0 ";
                str_where_for_Koeff2 = " isum_charge_isdel_0<>0 ";

                isdel_first = "(1)";
                isdel_second = "(0)";
                if (Points.packDistributionParameters.EnableLog) { MessageInPackLog(conn_db, packXX, "Установлена стратегия распределения с приоритетом на недействующих поставщиков", false, out r); }

            } else
                if (Points.packDistributionParameters.strategy == PackDistributionParameters.Strategies.ActiveServicesFirst)
            {
                Koeff1 = "sum_charge/isum_charge_isdel_0";
                Koeff2 = "sum_charge/isum_charge_isdel_1";

                str_where_for_Koeff1 = "and isum_charge_isdel_0<>0 ";
                str_where_for_Koeff2 = "and isum_charge_isdel_1<>0 ";

                isdel_first = "(0)";
                isdel_second = "(1)";
                if (Points.packDistributionParameters.EnableLog) { MessageInPackLog(conn_db, packXX, "Установлена стратегия распределения с приоритетом на действующих поставщиков", false, out r); }
            }
            if (isDistributionForInSaldo != true)
            {

                if (Points.packDistributionParameters.EnableLog & packXX.nzp_pack_ls > 0)
                {
                    s1 = "Установлена опция определять 'Начислено к оплате' как '";
                    if (Points.packDistributionParameters.chargeMethod == PackDistributionParameters.ChargeMethods.MonthlyCalculationWithChanges)
                    { s1 = s1 + "'Начисления за месяц с учетом перерасчетов, недопоставок и изменений сальдо'"; }
                    else
                        if (Points.packDistributionParameters.chargeMethod == PackDistributionParameters.ChargeMethods.MonthlyCalculationWithChangesAndOverpayment)
                        { s1 = s1 + "'Начисления за месяц с учетом перерасчетов, недопоставок, изменений сальдо и переплат'"; }
                        else
                            if (Points.packDistributionParameters.chargeMethod == PackDistributionParameters.ChargeMethods.PositiveMonthlyCalculationWithChanges)
                            { s1 = s1 + "'Положительная часть начислений за месяц с учетом перерасчетов, недопоставок и изменений сальдо'"; }
                            else
                                if (Points.packDistributionParameters.chargeMethod == PackDistributionParameters.ChargeMethods.PositiveMonthlyCalculationWithChangesAndOverpayment)
                                { s1 = s1 + "'Положительная часть начислений за месяц с учетом перерасчетов, недопоставок, изменений сальдо и переплат'"; }
                                else
                                    if (Points.packDistributionParameters.chargeMethod == PackDistributionParameters.ChargeMethods.Outsaldo)
                                    { s1 = s1 + "'Исходящее сальдо'"; }
                                    else
                                        if (Points.packDistributionParameters.chargeMethod == PackDistributionParameters.ChargeMethods.PositiveOutsaldo)
                                        { s1 = s1 + "'Положительная часть исходящего сальдо'"; }
                    MessageInPackLog(conn_db, packXX, s1, false, out r);

                }
            }
            // 
            #region 1. Вначале распределить оплаты на услуги по которым определён приоритет в погашении
            //string str_serv_prioritet = "500";
            //sqlText =
            //        " Update t_opl " +
            //        " Set  " +
            //        "     sum_prih_d = sum_charge , " + // Погашение долга
            //        "     g_sum_ls = g_sum_ls -sum_charge " +
            //        " where sum_charge <= g_sum_ls and kod_sum in (" + strKodSumForCharge_MM + ") and kod_info = 0 and nzp_serv in "+str_serv_prioritet;



            #endregion 1. Вначале распределить оплаты на услуги по которым определён приоритет в погашении


            #region 2. Затем распределить оплаты по графе "Указано жильцом"
//            #if PG
//sqlText = "select nzp_pack_ls, nzp_serv , sum_oplat, is_union,a.nzp_supp from " + Points.Pref + "_fin_" + (Convert.ToDateTime(Points.DateOper).Year - 2000).ToString("00") + ".gil_sums a where coalesce(a.nzp_supp,0) =0 and  a.nzp_pack_ls in (select a.nzp_pack_ls from  t_opl a ) ";
//#else
            //sqlText = "select nzp_pack_ls, nzp_serv , sum_oplat, is_union,a.nzp_supp from " + Points.Pref + "_fin_" + (Convert.ToDateTime(Points.DateOper).Year - 2000).ToString("00") + ":gil_sums a where nvl(a.nzp_supp,0) =0 and  a.nzp_pack_ls in (select a.nzp_pack_ls from  t_opl a ) ";
//#endif
            sqlText = "select nzp_pack_ls, nzp_serv , sum_oplat, is_union,nzp_supp, nzp_sums from " + Points.Pref + "_fin_" + (Convert.ToDateTime(Points.DateOper).Year - 2000).ToString("00") + tableDelimiter+"gil_sums a where   a.nzp_pack_ls in (select a.nzp_pack_ls from  t_opl a ) ";
            string nzp_pack_ls, nzp_serv, sum_oplat, is_union;
            dt = ClassDBUtils.OpenSQL(sqlText, conn_db).GetData();
            foreach (DataRow rowGilSums in dt.Rows)
            {
                //row = dt.Rows[0];
                if (rowGilSums["nzp_pack_ls"] != DBNull.Value)
                {
                    nzp_pack_ls = Convert.ToString(rowGilSums["nzp_pack_ls"]);
                    nzp_serv = Convert.ToString(rowGilSums["nzp_serv"]);
                    sum_oplat = Convert.ToString(rowGilSums["sum_oplat"]);
                    is_union = Convert.ToString(rowGilSums["is_union"]);
                    string nzp_supp = "0";
                    if (rowGilSums["nzp_supp"] != DBNull.Value)
                    {
                        nzp_supp = Convert.ToString(rowGilSums["nzp_supp"]);
                    }

                    if (nzp_supp.Trim() == "")
                    {
                        nzp_supp = "0";
                    }

                    //string straregyMode;
                    //if (Points.packDistributionParameters.strategy == PackDistributionParameters.Strategies.InactiveServicesFirst)
                    //{
                    //    straregyMode = "1";
                    //}
                    //else
                    //{
                    //    if (Points.packDistributionParameters.strategy == PackDistributionParameters.Strategies.ActiveServicesFirst)
                    //    {
                    //        straregyMode = "2";
                    //    }
                    //    else
                    //    {
                    //        straregyMode = "3";
                    //    }
                    //}
                    if (nzp_supp != "0")
                   {
                       ExecSQL(conn_db, " update t_opl set sum_prih_u = (select nvl(a.sum_oplat,0) from "+Points.Pref + "_fin_" + (Convert.ToDateTime(Points.DateOper).Year - 2000).ToString("00") + tableDelimiter+"gil_sums a a where a.nzp_pack_ls = t_opl.nzp_pack_ls and a.nzp_serv = t_opl.nzp_serv and a.nzp_supp = t_opl.nzp_supp) where nzp_pack_ls = " + nzp_pack_ls+" and nzp_serv =" +nzp_serv+" and nzp_supp = "+nzp_supp, true);
                       ExecSQL(conn_db, " insert into t_gil_sums (nzp_pack_ls, nzp_serv, sum_prih,sum_prih_u, nzp_supp) select nzp_pack_ls, nzp_serv, sum_oplat, sum_oplat,nzp_supp from " + Points.Pref + "_fin_" + (Convert.ToDateTime(Points.DateOper).Year - 2000).ToString("00") +
                           tableDelimiter + "gil_sums a a where a.nzp_sums = " + Convert.ToString(rowGilSums["nzp_sums"]), true);
                   } else
                    {
                   #if PG
 sqlText =
                    //    " execute procedure " + Points.Pref + "_kernel.reparation_gil_sum(" + nzp_pack_ls + "," + nzp_serv + " ," + sum_oplat + "," + is_union + "," + straregyMode + ") ";
                    //  Распределение по 14 графе с учётом стратегий сделать позже
                    " execute procedure " + Points.Pref + "_kernel.reparation_gil_sum(" + nzp_pack_ls + "," + nzp_serv + " ," + sum_oplat + "," + is_union + ") ";
#else
 sqlText =
                    //    " execute procedure " + Points.Pref + "_kernel:reparation_gil_sum(" + nzp_pack_ls + "," + nzp_serv + " ," + sum_oplat + "," + is_union + "," + straregyMode + ") ";
                    //  Распределение по 14 графе с учётом стратегий сделать позже
                    " execute procedure " + Points.Pref + "_kernel:reparation_gil_sum(" + nzp_pack_ls + "," + nzp_serv + " ," + sum_oplat + "," + is_union + ") ";
#endif
                    ret = ExecSQL(conn_db, sqlText, true);
                    if (!ret.result)
                    {
                        DropTempTablesPack(conn_db);
                        MessageInPackLog(conn_db, packXX, "Ошибка распределения графы 'Оплачиваю'", true, out r);
                        return false;
                    }
                    }

                }
            }

           #if PG
 ExecSQL(conn_db, " update t_opl set gil_sums = (select coalesce(sum(a.sum_prih_u),0) from t_gil_sums a where a.nzp_pack_ls = t_opl.nzp_pack_ls)", true);
#else
 ExecSQL(conn_db, " update t_opl set gil_sums = (select NVL(sum(a.sum_prih_u),0) from t_gil_sums a where a.nzp_pack_ls = t_opl.nzp_pack_ls)", true);
#endif
            // Пометить  оплаты у которых есть графа "Оплачиваю"
            sqlText = 
#if PG
"  Update t_opl  Set  isum_charge = isum_charge - (select coalesce(sum(sum_charge),0) from t_gil_sums a where a.nzp_pack_ls = t_opl.nzp_pack_ls ),   " +
#else
"  Update t_opl  Set  isum_charge = isum_charge - (select NVL(sum(sum_charge),0) from t_gil_sums a where a.nzp_pack_ls = t_opl.nzp_pack_ls ),   " +
#endif
               //"g_sum_ls = g_sum_ls_first - gil_sums,  " +
#if PG
               "isum_charge_isdel_0 = isum_charge_isdel_0 - (select coalesce(sum(sum_charge),0) from t_gil_sums a where  a.isdel =0 and a.nzp_pack_ls = t_opl.nzp_pack_ls ),  " +
               "isum_charge_isdel_1 = isum_charge_isdel_1 - (select coalesce(sum(sum_charge),0) from t_gil_sums a where  a.isdel =1 and a.nzp_pack_ls = t_opl.nzp_pack_ls )   "+
               "  ";
#else
"isum_charge_isdel_0 = isum_charge_isdel_0 - (select NVL(sum(sum_charge),0) from t_gil_sums a where  a.isdel =0 and a.nzp_pack_ls = t_opl.nzp_pack_ls ),  " +
               "isum_charge_isdel_1 = isum_charge_isdel_1 - (select NVL(sum(sum_charge),0) from t_gil_sums a where  a.isdel =1 and a.nzp_pack_ls = t_opl.nzp_pack_ls )   "+
               "  ";
#endif 
            ret = ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log).GetReturnsType().GetReturns();


            sqlText = " update t_opl set   kod_info = 114 where sum_prih_u <>0; ";
            ret = ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log).GetReturnsType().GetReturns();

           
            #endregion 2. Затем распределить оплаты по графе "Указано жильцом"

            #region  3. Вначале обработать оплаты у которых начислено к оплате совпадает 100 % с оплатой
            sqlText =
                    " Update t_opl " +
                    " Set kod_info = 101, " +
                    "     sum_prih_d = sum_charge" + // Погашение долга
                    " where isum_charge = g_sum_ls-gil_sums and kod_sum in (" + strKodSumForCharge_MM + ",81) and kod_info = 0  ";

            IntfResultType retres = ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log);
            ret = retres.GetReturnsType().GetReturns();
            if (!ret.result)
            {
                DropTempTablesPack(conn_db);
                MessageInPackLog(conn_db, packXX, "Ошибка распределения 4.1", true, out r);
                Utility.ExceptionUtility.OnException(new Exception(ret.text), System.Reflection.MethodBase.GetCurrentMethod().Name);
                return false;
            }
            #if PG
                int count = retres.resultAffectedRows;
            #else
                int count = ClassDBUtils.GetAffectedRowsCount(conn_db);
            #endif
            if (Points.packDistributionParameters.EnableLog & count > 0 & packXX.nzp_pack_ls > 0)
            {
                MessageInPackLog(conn_db, packXX, "Алгоритм распределения № 1 (оплачено = выставлено)", true, out r);
            }

            #endregion Вначале обработать оплаты у которых начислено к оплате совпадает 100 % с оплатой

  
            #region 4. Обработать оплаты у которых сумма платежа меньше чем выставлено

            // 2.1 Пометить такие оплаты
            sqlText = " Update t_opl " +
                     " Set kod_info = 102 " +
                     " where isum_charge > g_sum_ls-gil_sums and kod_sum in (" + strKodSumForCharge_MM + ",81) and kod_info = 0 and sum_prih_u = 0 ";
            retres = ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log);
            ret = retres.GetReturnsType().GetReturns();

            if (!ret.result)
            {
                DropTempTablesPack(conn_db);
                MessageInPackLog(conn_db, packXX, "Ошибка распределения 6.1", true, out r);
                Utility.ExceptionUtility.OnException(new Exception(ret.text), System.Reflection.MethodBase.GetCurrentMethod().Name);
                return false;
            }
            #if PG
                  count = retres.resultAffectedRows;
            #else
                  count = ClassDBUtils.GetAffectedRowsCount(conn_db);
            #endif
            if (count > 0)
            {
                if (Points.packDistributionParameters.EnableLog & count > 0 & packXX.nzp_pack_ls > 0)
                {
                    #if PG
                           sqlText = "select  g_sum_ls as sum_ from t_opl where nzp_pack_ls=" + packXX.nzp_pack_ls + " limit 1";
                    #else
                           sqlText = "select first 1 g_sum_ls as sum_ from t_opl where nzp_pack_ls=" + packXX.nzp_pack_ls;
                    #endif
                    dt = ClassDBUtils.OpenSQL(sqlText, conn_db).GetData();
                    row = dt.Rows[0];
                    s1 = Convert.ToString(row["sum_"]);

                    sqlText = "select sum(sum_charge) as sum_ from t_opl where nzp_pack_ls=" + packXX.nzp_pack_ls;
                    dt = ClassDBUtils.OpenSQL(sqlText, conn_db).GetData();
                    row = dt.Rows[0];
                    s2 = Convert.ToString(row["sum_"]);

                    MessageInPackLog(conn_db, packXX, "Алгоритм распределения № 2 (оплачено: " + s1 + " руб меньше чем итого по '"+field_for_etalon_descr+"': " + s2 + " руб)", false, out r);
                }

                // 2.2 Первоначально погасить долг по поставщикам согласно выбранной стратегии (по sum_charge)
                sqlText =
                        " Update t_opl " +
                        " Set " +
                #if PG
                        "     sum_prih_d = " + Points.Pref + "_kernel.getSumPrih(sum_charge,  (g_sum_ls-gil_sums)*" + Koeff1 + ", isdel )  " +
                #else
                        "     sum_prih_d = " + Points.Pref + "_kernel:getSumPrih(sum_charge,  (g_sum_ls-gil_sums)*" + Koeff1 + ", isdel )  " +
                #endif
                        " where sum_prih_u = 0 and kod_sum in (" + strKodSumForCharge_MM + ",81) and kod_info = 102  and isdel in " + isdel_first + " and " + str_where_for_Koeff1;
                retres = ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log);
                ret = retres.GetReturnsType().GetReturns();
                if (!ret.result)
                {
                    DropTempTablesPack(conn_db);
                    MessageInPackLog(conn_db, packXX, "Ошибка распределения 6.2", true, out r);
                    Utility.ExceptionUtility.OnException(new Exception(ret.text), System.Reflection.MethodBase.GetCurrentMethod().Name);
                    return false;
                }
                #if PG
                       count = retres.resultAffectedRows;
                #else
                       count = ClassDBUtils.GetAffectedRowsCount(conn_db);
                #endif
                if (Points.packDistributionParameters.EnableLog & packXX.nzp_pack_ls > 0)
                {
                    sqlText = "select sum(sum_prih_d) as sum_1, sum(sum_charge) as sum_2 from t_opl where kod_info=102 and isdel in " + isdel_first + " and nzp_pack_ls=" + packXX.nzp_pack_ls;
                    dt = ClassDBUtils.OpenSQL(sqlText, conn_db).GetData();
                    row = dt.Rows[0];
                    s1 = Convert.ToString(row["sum_1"]);
                    s2 = Convert.ToString(row["sum_2"]);
                    if (Points.packDistributionParameters.strategy == PackDistributionParameters.Strategies.NoPriority)
                    {
                        MessageInPackLog(conn_db, packXX, "Погашение долга поставщиков (действующих и недействующих). Из " + s2 + " руб погашено " + s1 + " руб по '" + field_for_etalon_descr + "'", false, out r);
                    }
                    else
                    {
                        MessageInPackLog(conn_db, packXX, "Погашение долга поставщиков первой очереди. Из " + s2 + " руб погашено " + s1 + " руб по '" + field_for_etalon_descr + "'", false, out r);
                    }
                    
                }

                // 2.3. Определить остаток от погашения
                #if PG
                    sqlText = " update t_ostatok set g_sum_ls_ost = " + Points.Pref + "_kernel.getSumOstatok(nzp_pack_ls)  ";      
                #else
                    sqlText = " update t_ostatok set g_sum_ls_ost = " + Points.Pref + "_kernel:getSumOstatok(nzp_pack_ls)  ";      
                #endif                 
 
                ret = ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log).GetReturnsType().GetReturns();
                if (!ret.result)
                {
                    DropTempTablesPack(conn_db);
                    MessageInPackLog(conn_db, packXX, "Ошибка распределения 6.3", true, out r);
                    Utility.ExceptionUtility.OnException(new Exception(ret.text), System.Reflection.MethodBase.GetCurrentMethod().Name);
                    return false;
                }
                if (Points.packDistributionParameters.strategy != PackDistributionParameters.Strategies.NoPriority)
                {
                    sqlText =
                            " Update t_ostatok " +
                            " Set  " +
                            "     g_sum_ls_ost =0 " +
                            " where g_sum_ls_ost < 0 ";
                    ret = ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log).GetReturnsType().GetReturns();
                    if (!ret.result)
                    {
                        DropTempTablesPack(conn_db);
                        MessageInPackLog(conn_db, packXX, "Ошибка распределения 6.4", true, out r);
                        Utility.ExceptionUtility.OnException(new Exception(ret.text), System.Reflection.MethodBase.GetCurrentMethod().Name);
                        return false;
                    }
                    
                    // 2.4. Остатоком погасить долг по поставщикам второй очереди по sum_charge
                    sqlText =
                            " Update t_opl " +
                            " Set  " +
                            "     sum_prih_d = (select case when  g_sum_ls_ost>0 then  g_sum_ls_ost else 0 end from t_ostatok where nzp_pack_ls = t_opl.nzp_pack_ls) * (" + Koeff2 + ")" +
                            " where sum_prih_u = 0 and  kod_info = 102 and isdel in " + isdel_second + "  and sum_charge>0  and " + str_where_for_Koeff2;
                    retres = ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log);
                    ret = retres.GetReturnsType().GetReturns();
                    if (!ret.result)
                    {
                        DropTempTablesPack(conn_db);
                        MessageInPackLog(conn_db, packXX, "Ошибка распределения 6.5", true, out r);
                        Utility.ExceptionUtility.OnException(new Exception(ret.text), System.Reflection.MethodBase.GetCurrentMethod().Name);
                        return false;
                    }
                    #if PG
                        count = retres.resultAffectedRows;
                    #else
                        count = ClassDBUtils.GetAffectedRowsCount(conn_db);
                    #endif
                    if (Points.packDistributionParameters.EnableLog & count > 0 & packXX.nzp_pack_ls > 0)
                    {
                        sqlText = "select SUM(sum_prih_d) as sum_1, sum(sum_charge) as sum_2  from t_opl where isdel in " + isdel_second + " and kod_info=102  and nzp_pack_ls=" + packXX.nzp_pack_ls;
                        dt = ClassDBUtils.OpenSQL(sqlText, conn_db).GetData();
                        row = dt.Rows[0];
                        s1 = Convert.ToString(row["sum_1"]);
                        s2 = Convert.ToString(row["sum_2"]);

                        if (s2.Trim() != "0")
                        {
                            MessageInPackLog(conn_db, packXX, "Погашение долга поставщиков второй очереди. Из " + s2 + " руб погашено " + s1 + " руб по '" + field_for_etalon_descr + "'", false, out r);
                        }
                    }
                }

            }
            #endregion 2. Обработать оплаты у которых сумма платежа меньше чем выставлено

            #region 5. Затем обработать все оплаты, которые превосходят начисления

            // 3.1 Пометить такие оплаты
            sqlText = " Update t_opl " +
                     " Set kod_info = 103 " +
                     " where isum_charge < g_sum_ls-gil_sums and kod_sum in (" + strKodSumForCharge_MM + ",81) and kod_info = 0 and sum_prih_u = 0  ";//and isum_charge<>0";
            retres = ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log);
            ret = retres.GetReturnsType().GetReturns();

            if (!ret.result)
            {
                DropTempTablesPack(conn_db);
                MessageInPackLog(conn_db, packXX, "Ошибка распределения 5.1", true, out r);
                Utility.ExceptionUtility.OnException(new Exception(ret.text), System.Reflection.MethodBase.GetCurrentMethod().Name);
                return false;
            }
            #if PG
                count = retres.resultAffectedRows;
            #else
                count = ClassDBUtils.GetAffectedRowsCount(conn_db);
            #endif
            if (Points.packDistributionParameters.EnableLog & count > 0 & packXX.nzp_pack_ls > 0)
            {
                #if PG
                    sqlText = "select  g_sum_ls as sum_ from t_opl where nzp_pack_ls=" + packXX.nzp_pack_ls + " limit 1";
                #else
                    sqlText = "select first 1 g_sum_ls as sum_ from t_opl where nzp_pack_ls=" + packXX.nzp_pack_ls;
                #endif
                dt = ClassDBUtils.OpenSQL(sqlText, conn_db).GetData();
                row = dt.Rows[0];
                s1 = Convert.ToString(row["sum_"]);

                sqlText = "select sum(sum_charge) as sum_ from t_opl where nzp_pack_ls=" + packXX.nzp_pack_ls;
                dt = ClassDBUtils.OpenSQL(sqlText, conn_db).GetData();
                row = dt.Rows[0];
                s2 = Convert.ToString(row["sum_"]);

                MessageInPackLog(conn_db, packXX, "Алгоритм распределения № 3 (оплачено: " + s1 + " руб больше чем итого по '" + field_for_etalon_descr + "': " + s2 + " руб)", false, out r);
            }


            if ( // Способ начисления sum_charge по методу C,D,E,F
                  (Points.packDistributionParameters.chargeMethod == PackDistributionParameters.ChargeMethods.MonthlyCalculationWithChanges) ||
                  (Points.packDistributionParameters.chargeMethod == PackDistributionParameters.ChargeMethods.MonthlyCalculationWithChangesAndOverpayment) ||
                  (Points.packDistributionParameters.chargeMethod == PackDistributionParameters.ChargeMethods.PositiveMonthlyCalculationWithChanges) ||
                  (Points.packDistributionParameters.chargeMethod == PackDistributionParameters.ChargeMethods.PositiveMonthlyCalculationWithChangesAndOverpayment)
                )
            {
               // Если по всем услугам отрицательные суммы в sum_charge (по лицевому счёту всё погашено) 
                sqlText = " update t_opl set kod_info = 104 where (select sum(b.sum_outsaldo) from t_charge b where t_opl.nzp_kvar = b.nzp_kvar and b.sum_outsaldo<0  ) = isum_outsaldo and kod_info = 103 ";

            } else // Способ начисления sum_charge по методу A,B
            {
                               // Если по всем услугам отрицательные суммы в sum_charge (по лицевому счёту всё погашено) 
                sqlText = " update t_opl set kod_info = 104 where (select sum(b.sum_charge) from t_charge b where t_opl.nzp_kvar = b.nzp_kvar and b.sum_charge<0) = isum_charge and kod_info = 103 ";

            }

            retres = ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log);
            ret = retres.GetReturnsType().GetReturns();
            if (!ret.result)
            {
                DropTempTablesPack(conn_db);
                MessageInPackLog(conn_db, packXX, "Ошибка распределения 5.2", true, out r);
                Utility.ExceptionUtility.OnException(new Exception(ret.text), System.Reflection.MethodBase.GetCurrentMethod().Name);
                return false;
            }
            #if PG
                count = retres.resultAffectedRows;
            #else
                count = ClassDBUtils.GetAffectedRowsCount(conn_db);
            #endif
            if (Points.packDistributionParameters.EnableLog & count > 0 & packXX.nzp_pack_ls > 0)
            {
                sqlText = "select sum(sum_charge) as sum_ from t_opl where nzp_pack_ls=" + packXX.nzp_pack_ls;
                dt = ClassDBUtils.OpenSQL(sqlText, conn_db).GetData();
                row = dt.Rows[0];
                s1 = Convert.ToString(row["sum_"]);

                MessageInPackLog(conn_db, packXX, "По лицевому счёту только переплаты. Распределение будет произведено как авансовый платёж по 'Начислено за месяц без недопоставки'", false, out r);
            }          
            // 3.2 Первоначально погасить долг (по sum_charge) (неважно действующий или нет, т.к. оплаты хватает)
            sqlText =
                    " Update t_opl " +
                    " Set " +
                    "     sum_prih_d =  sum_charge" +
                    " where isum_charge < g_sum_ls-gil_sums and kod_sum in (" + strKodSumForCharge_MM + ",81) and kod_info = 103 and sum_prih_u = 0  ";
            retres = ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log);
            ret = retres.GetReturnsType().GetReturns();
            if (!ret.result)
            {
                DropTempTablesPack(conn_db);
                MessageInPackLog(conn_db, packXX, "Ошибка распределения 5.2", true, out r);
                Utility.ExceptionUtility.OnException(new Exception(ret.text), System.Reflection.MethodBase.GetCurrentMethod().Name);
                return false;
            }
            #if PG
                count = retres.resultAffectedRows;
            #else
                count = ClassDBUtils.GetAffectedRowsCount(conn_db);
            #endif
            if (Points.packDistributionParameters.EnableLog & count > 0 & packXX.nzp_pack_ls > 0)
            {
                sqlText = "select sum(sum_charge) as sum_ from t_opl where nzp_pack_ls=" + packXX.nzp_pack_ls;
                dt = ClassDBUtils.OpenSQL(sqlText, conn_db).GetData();
                row = dt.Rows[0];
                s1 = Convert.ToString(row["sum_"]);

                MessageInPackLog(conn_db, packXX, "Погашение основного долга " + s1 + " руб по '" + field_for_etalon_descr + "'", false, out r);
            }
            // 3.4. Остаток распределить по исходящему сальдо
            if  ( 
                 ( // Способ начисления sum_charge по методу C,D,E,F
                  (Points.packDistributionParameters.chargeMethod == PackDistributionParameters.ChargeMethods.MonthlyCalculationWithChanges) ||
                  (Points.packDistributionParameters.chargeMethod == PackDistributionParameters.ChargeMethods.MonthlyCalculationWithChangesAndOverpayment) ||
                  (Points.packDistributionParameters.chargeMethod == PackDistributionParameters.ChargeMethods.PositiveMonthlyCalculationWithChanges) ||
                  (Points.packDistributionParameters.chargeMethod == PackDistributionParameters.ChargeMethods.PositiveMonthlyCalculationWithChangesAndOverpayment)
                 )
                &
                 (isDistributionForInSaldo!=true) 
                )
            {
                // 3.4.0 Определить остаток от погашения
                sqlText = " Update t_opl " +
                        " SET  g_sum_ls_ost = g_sum_ls-isum_charge" +
                        " where  kod_sum in (" + strKodSumForCharge_MM + ",81) and kod_info = 103 and isdel in " + isdel_second + " and sum_outsaldo>0";

                
                retres = ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log);
                ret = retres.GetReturnsType().GetReturns();
                if (!ret.result)
                {
                    DropTempTablesPack(conn_db);
                    MessageInPackLog(conn_db, packXX, "Ошибка распределения 5.3", true, out r);
                    Utility.ExceptionUtility.OnException(new Exception(ret.text), System.Reflection.MethodBase.GetCurrentMethod().Name);
                    return false;
                }
                #if PG
                    count = retres.resultAffectedRows;
                #else
                    count = ClassDBUtils.GetAffectedRowsCount(conn_db);
                #endif
                if (Points.packDistributionParameters.EnableLog & packXX.nzp_pack_ls > 0 & count>0)
                {
                    #if PG
                        sqlText = "select  g_sum_ls_ost as sum_, kod_info from t_opl where  nzp_pack_ls=" + packXX.nzp_pack_ls + " limit 1";
                    #else
                        sqlText = "select first 1 g_sum_ls_ost as sum_, kod_info from t_opl where  nzp_pack_ls=" + packXX.nzp_pack_ls;
                    #endif
                    dt = ClassDBUtils.OpenSQL(sqlText, conn_db).GetData();
                    row = dt.Rows[0];
                    s1 = Convert.ToString(row["sum_"]);
                    if ((s1.Trim() != "") && (s1.Trim() != "0.00") && (s1.Trim() != "0") && (Convert.ToString(row["kod_info"]) == "103"))
                    {
                        MessageInPackLog(conn_db, packXX, "Остаток от погашения долга " + s1 + " распределять по исходящему сальдо откорректированному на погашение долга", false, out r);
                    }
                }

                // 3.4.1  Откорректировать сумму исходящего сальдо с учётом ранее распределённой суммы
                sqlText =
                    " update t_opl set (sum_outsaldo) = ( sum_outsaldo - (select sum(sum_money) from t_charge a where a.nzp_kvar = t_opl.nzp_kvar and a.nzp_serv = t_opl.nzp_serv and a.nzp_supp = t_opl.nzp_supp))   ";
                //ret = ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log).GetReturnsType().GetReturns();
                retres = ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log);
                ret = retres.GetReturnsType().GetReturns();
                if (!ret.result)
                {
                    DropTempTablesPack(conn_db);
                    MessageInPackLog(conn_db, packXX, "Ошибка распределения 5.4", true, out r);
                    Utility.ExceptionUtility.OnException(new Exception(ret.text), System.Reflection.MethodBase.GetCurrentMethod().Name);
                    return false;
                }
                // 3.4.2  подсчитать итоговую сумму исходящего сальдо 
                sqlText =
                " update t_itog set sum_outsaldo = (select sum(sum_outsaldo) from t_opl a where a.nzp_kvar = t_itog.nzp_kvar and a.sum_outsaldo>0 and isdel=0)";
                ret = ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log).GetReturnsType().GetReturns();
                if (!ret.result)
                {
                    DropTempTablesPack(conn_db);
                    MessageInPackLog(conn_db, packXX, "Ошибка распределения 5.5", true, out r);
                    Utility.ExceptionUtility.OnException(new Exception(ret.text), System.Reflection.MethodBase.GetCurrentMethod().Name);
                    return false;
                }
                sqlText =
                    "update t_opl set isum_outsaldo = (select sum_outsaldo from t_itog a where a.nzp_pack_ls = t_opl.nzp_pack_ls )   ";
                ret = ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log).GetReturnsType().GetReturns();
                if (!ret.result)
                {
                    DropTempTablesPack(conn_db);
                    MessageInPackLog(conn_db, packXX, "Ошибка распределения 5.6", true, out r);
                    Utility.ExceptionUtility.OnException(new Exception(ret.text), System.Reflection.MethodBase.GetCurrentMethod().Name);
                    return false;
                }
                // 3.4.4. Определить остаток от погашения
                // 2.3. Определить остаток от погашения
                #if PG
                    sqlText = " update t_ostatok set g_sum_ls_ost = " + Points.Pref + "_kernel.getSumOstatok(nzp_pack_ls) ";
                #else
                    sqlText = " update t_ostatok set g_sum_ls_ost = " + Points.Pref + "_kernel:getSumOstatok(nzp_pack_ls) ";
                #endif

                ret = ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.LogTrace).GetReturnsType().GetReturns();
                if (!ret.result)
                {
                    DropTempTablesPack(conn_db);
                    MessageInPackLog(conn_db, packXX, "Ошибка распределения 5.7", true, out r);
                    Utility.ExceptionUtility.OnException(new Exception(ret.text), System.Reflection.MethodBase.GetCurrentMethod().Name);
                    return false;
                }
                sqlText = " update t_opl set g_sum_ls_ost = (select g_sum_ls_ost from t_ostatok where t_opl.nzp_pack_ls = t_ostatok.nzp_pack_ls and  kod_info = 103 ) " +
                    " where  kod_sum in (" + strKodSumForCharge_MM + ",81) and kod_info = 103 and isdel in " + isdel_second + " and sum_outsaldo>0";

                ret = ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log).GetReturnsType().GetReturns();
                if (!ret.result)
                {
                    DropTempTablesPack(conn_db);
                    MessageInPackLog(conn_db, packXX, "Ошибка распределения 5.7", true, out r);
                    Utility.ExceptionUtility.OnException(new Exception(ret.text), System.Reflection.MethodBase.GetCurrentMethod().Name);
                    return false;
                }

                sqlText = " Update t_opl " +
                        " SET  g_sum_ls_ost =0, sum_outsaldo =0  " +
                        " where  kod_sum in (" + strKodSumForCharge_MM + ",81) and kod_info = 103 and isdel<>0   ";
                ret = ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log).GetReturnsType().GetReturns();
                if (!ret.result)
                {
                    DropTempTablesPack(conn_db);
                    MessageInPackLog(conn_db, packXX, "Ошибка распределения 5.8", true, out r);
                    Utility.ExceptionUtility.OnException(new Exception(ret.text), System.Reflection.MethodBase.GetCurrentMethod().Name);
                    return false;
                }
                sqlText = " Update t_opl " +
                        " SET  g_sum_ls_ost =0, sum_outsaldo =0  " +
                        " where  kod_sum in (" + strKodSumForCharge_MM + ",81) and kod_info = 103 and  sum_outsaldo<=0 ";
                ret = ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log).GetReturnsType().GetReturns();
                if (!ret.result)
                {
                    DropTempTablesPack(conn_db);
                    MessageInPackLog(conn_db, packXX, "Ошибка распределения 5.9", true, out r);
                    Utility.ExceptionUtility.OnException(new Exception(ret.text), System.Reflection.MethodBase.GetCurrentMethod().Name);
                    return false;
                }
                sqlText = " Update t_opl " +
                         " SET  isum_outsaldo =0  " +
                         " where  kod_sum in (" + strKodSumForCharge_MM + ",81) and kod_info = 103 and  sum_outsaldo<=0 ";
                ret = ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log).GetReturnsType().GetReturns();
                if (!ret.result)
                {
                    DropTempTablesPack(conn_db);
                    MessageInPackLog(conn_db, packXX, "Ошибка распределения 5.10", true, out r);
                    Utility.ExceptionUtility.OnException(new Exception(ret.text), System.Reflection.MethodBase.GetCurrentMethod().Name);
                    return false;
                }
                // 3.4.5  Распределить остаток по откорректированному исходящему сальдо (по положительной части) 
                #if PG
                     sqlText =
                        " Update t_opl " +
                        " Set  " +
                       "   sum_prih_s = " + Points.Pref + "_kernel.getSumPrih(sum_outsaldo,  g_sum_ls_ost * (sum_outsaldo/isum_outsaldo),1 )" +
                        " where  g_sum_ls_ost > 0 and kod_sum in (" + strKodSumForCharge_MM + ",81) and kod_info = 103 and isdel =0 and sum_outsaldo>0";
                #else
                    sqlText =
                        " Update t_opl " +
                        " Set  " +
                       "   sum_prih_s = " + Points.Pref + "_kernel:getSumPrih(sum_outsaldo,  g_sum_ls_ost * (sum_outsaldo/isum_outsaldo),1 )" +
                        " where  g_sum_ls_ost > 0 and kod_sum in (" + strKodSumForCharge_MM + ",81) and kod_info = 103 and isdel =0 and sum_outsaldo>0";
                #endif
                retres = ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log);
                ret = retres.GetReturnsType().GetReturns();
                if (!ret.result)
                {
                    DropTempTablesPack(conn_db);
                    MessageInPackLog(conn_db, packXX, "Ошибка распределения 5.11", true, out r);
                    Utility.ExceptionUtility.OnException(new Exception(ret.text), System.Reflection.MethodBase.GetCurrentMethod().Name);
                    return false;
                }
                #if PG
                    count = retres.resultAffectedRows;
                #else
                    count = ClassDBUtils.GetAffectedRowsCount(conn_db);
                #endif
                if (Points.packDistributionParameters.EnableLog & count > 0 & packXX.nzp_pack_ls > 0)
                {
                    #if PG
                        sqlText = "select  g_sum_ls_ost as sum_ from t_opl where g_sum_ls_ost>0 and nzp_pack_ls=" + packXX.nzp_pack_ls + " limit 1";
                    #else
                        sqlText = "select first 1 g_sum_ls_ost as sum_ from t_opl where g_sum_ls_ost>0 and nzp_pack_ls=" + packXX.nzp_pack_ls;
                    #endif
                    dt = ClassDBUtils.OpenSQL(sqlText, conn_db).GetData();
                    row = dt.Rows[0];
                    s1 = Convert.ToString(row["sum_"]);
                    sqlText = "select sum(sum_prih_s) as sum_ from t_opl where sum_prih_s>0 and nzp_pack_ls=" + packXX.nzp_pack_ls;
                    dt = ClassDBUtils.OpenSQL(sqlText, conn_db).GetData();
                    row = dt.Rows[0];
                    s2 = Convert.ToString(row["sum_"]);
                    MessageInPackLog(conn_db, packXX, "По откорректированному исходящему сальдо распределено " + s2 + " руб", false, out r);
                }
            }
            // 3.4.6 Определить остаток от погашения
            #if PG
                sqlText = " update t_ostatok set g_sum_ls_ost = " + Points.Pref + "_kernel.getSumOstatok(nzp_pack_ls)  ";
            #else
                sqlText = " update t_ostatok set g_sum_ls_ost = " + Points.Pref + "_kernel:getSumOstatok(nzp_pack_ls) ";
            #endif

                ret = ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log).GetReturnsType().GetReturns();
                if (!ret.result)
                {
                    DropTempTablesPack(conn_db);
                    MessageInPackLog(conn_db, packXX, "Ошибка распределения 5.12", true, out r);
                    Utility.ExceptionUtility.OnException(new Exception(ret.text), System.Reflection.MethodBase.GetCurrentMethod().Name);
                    return false;
                }
                                         
                if (isDistributionForInSaldo != true)
                {
                    sqlText = " Update t_opl " +
                                        " SET  g_sum_ls_ost = (select g_sum_ls_ost from t_ostatok where t_ostatok.nzp_pack_ls = t_opl.nzp_pack_ls)" +
                                        " where  kod_sum in (" + strKodSumForCharge_MM + ",81) and kod_info in (103,104,114)  and isdel=0  and rsum_tarif>0";
                    ret = ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log).GetReturnsType().GetReturns();
                    if (!ret.result)
                    {
                        DropTempTablesPack(conn_db);
                        MessageInPackLog(conn_db, packXX, "Ошибка распределения 5.13", true, out r);
                        Utility.ExceptionUtility.OnException(new Exception(ret.text), System.Reflection.MethodBase.GetCurrentMethod().Name);
                        return false;
                    }
                }
                sqlText =
                            " Update t_opl " +
                            " Set  " +
                            "     sum_prih_a = g_sum_ls_ost * (rsum_tarif/irsum_tarif)" +
                            " where  g_sum_ls_ost > 0 and kod_sum in (" + strKodSumForCharge_MM + ",81) and kod_info in (103,104,114) and g_sum_ls_ost>0 and isdel=0  and rsum_tarif>0";                    

                retres = ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log);
                ret = retres.GetReturnsType().GetReturns();
                if (!ret.result)
                {
                    DropTempTablesPack(conn_db);
                    MessageInPackLog(conn_db, packXX, "Ошибка распределения 5.6", true, out r);
                    Utility.ExceptionUtility.OnException(new Exception(ret.text), System.Reflection.MethodBase.GetCurrentMethod().Name);
                    return false;
                }

                // Если есть ещё остаток

#if PG
                sqlText = " update t_ostatok set g_sum_ls_ost = " + Points.Pref + "_kernel.getSumOstatok(nzp_pack_ls)  ";
#else
                sqlText = " update t_ostatok set g_sum_ls_ost = " + Points.Pref + "_kernel:getSumOstatok(nzp_pack_ls) ";
#endif

                ret = ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log).GetReturnsType().GetReturns();
                if (!ret.result)
                {
                    DropTempTablesPack(conn_db);
                    MessageInPackLog(conn_db, packXX, "Ошибка распределения 5.12", true, out r);
                    Utility.ExceptionUtility.OnException(new Exception(ret.text), System.Reflection.MethodBase.GetCurrentMethod().Name);
                    return false;
                }
#if PG
                count = retres.resultAffectedRows;
#else
                count = ClassDBUtils.GetAffectedRowsCount(conn_db);
#endif
                if (Points.packDistributionParameters.EnableLog & count > 0 & packXX.nzp_pack_ls > 0)
                {
#if PG
                    sqlText = "select g_sum_ls_ost as sum_ from t_opl where g_sum_ls_ost>0 and nzp_pack_ls=" + packXX.nzp_pack_ls + " limit 1";
#else
                    sqlText = "select first 1 g_sum_ls_ost as sum_ from t_opl where g_sum_ls_ost>0 and nzp_pack_ls=" + packXX.nzp_pack_ls;
#endif
                    dt = ClassDBUtils.OpenSQL(sqlText, conn_db).GetData();
#if PG
                    count = retres.resultAffectedRows;
#else
                    count = ClassDBUtils.GetAffectedRowsCount(conn_db);
#endif

                    if (count>0)
                    { 
                    row = dt.Rows[0];
                    s1 = Convert.ToString(row["sum_"]);
                    MessageInPackLog(conn_db, packXX, "Распределение аванса " + s1 + " руб по 'Начислено за месяц без недопоставки'", false, out r);
                    }
                }
 
                sqlText = " Update t_opl " +
                                        " SET  g_sum_ls_ost = (select g_sum_ls_ost from t_ostatok where t_ostatok.nzp_pack_ls = t_opl.nzp_pack_ls)" +
                                        " where  kod_sum in (" + strKodSumForCharge_MM + ",81) and kod_info in (103,104,114)  and isdel=0  and sum_outsaldo>0";
                ret = ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log).GetReturnsType().GetReturns();
                if (!ret.result)
                    {
                        DropTempTablesPack(conn_db);
                        MessageInPackLog(conn_db, packXX, "Ошибка распределения 5.13", true, out r);
                        Utility.ExceptionUtility.OnException(new Exception(ret.text), System.Reflection.MethodBase.GetCurrentMethod().Name);
                        return false;
                }

                sqlText =
                            " Update t_opl " +
                            " Set  " +
                            "     sum_prih_a = sum_prih_a+g_sum_ls_ost * (sum_outsaldo/isum_outsaldo)" +
                            " where  g_sum_ls_ost > 0 and kod_sum in (" + strKodSumForCharge_MM + ",81) and kod_info in (103,104,114) and g_sum_ls_ost>0 and isdel=0  and sum_outsaldo>0";

                retres = ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log);
                ret = retres.GetReturnsType().GetReturns();
                if (!ret.result)
                {
                    DropTempTablesPack(conn_db);
                    MessageInPackLog(conn_db, packXX, "Ошибка распределения 5.6", true, out r);
                    Utility.ExceptionUtility.OnException(new Exception(ret.text), System.Reflection.MethodBase.GetCurrentMethod().Name);
                    return false;
                }

                #if PG
                    count = retres.resultAffectedRows;
                #else
                    count = ClassDBUtils.GetAffectedRowsCount(conn_db);
                #endif
                if (Points.packDistributionParameters.EnableLog & count > 0 & packXX.nzp_pack_ls > 0)
                {
                    #if PG
                        sqlText = "select g_sum_ls_ost as sum_ from t_opl where g_sum_ls_ost>0 and nzp_pack_ls=" + packXX.nzp_pack_ls + " limit 1";
                    #else
                        sqlText = "select first 1 g_sum_ls_ost as sum_ from t_opl where g_sum_ls_ost>0 and nzp_pack_ls=" + packXX.nzp_pack_ls;
                    #endif
                    dt = ClassDBUtils.OpenSQL(sqlText, conn_db).GetData();
                    row = dt.Rows[0];
                    s1 = Convert.ToString(row["sum_"]);
                    MessageInPackLog(conn_db, packXX, "Распределение аванса " + s1 + " руб по 'Исходящему сальдо (+)'", false, out r);
                }
            

            #endregion 3. Затем обработать все оплаты, которые превосходят начисления

            // Подсчитать итоговую сумму распределения
            sqlText =
                    " Update t_opl " +
                    " Set  " +
                    "     sum_prih = sum_prih_a+sum_prih_u+sum_prih_d+sum_prih_s " +
                    " where   kod_info >0 ";
            retres = ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log);
            ret = retres.GetReturnsType().GetReturns();

            if (!ret.result)
            {
                DropTempTablesPack(conn_db);
                MessageInPackLog(conn_db, packXX, "Ошибка распределения 6.6", true, out r);
                Utility.ExceptionUtility.OnException(new Exception(ret.text), System.Reflection.MethodBase.GetCurrentMethod().Name);
                return false;
            }

            #if PG
                count = retres.resultAffectedRows;
            #else
                count = ClassDBUtils.GetAffectedRowsCount(conn_db);
            #endif
            if (Points.packDistributionParameters.EnableLog & count > 0 & packXX.nzp_pack_ls > 0)
            {
                sqlText = "select sum(sum_prih) as sum_ from t_opl where kod_info>0 and nzp_pack_ls=" + packXX.nzp_pack_ls;
                dt = ClassDBUtils.OpenSQL(sqlText, conn_db).GetData();
                row = dt.Rows[0];
                s1 = Convert.ToString(row["sum_"]);
                MessageInPackLog(conn_db, packXX, "Итого распределено " + s1 + " руб", false, out r);
            }

            ExecSQL(conn_db, " Create index ix2_t_opl on t_opl (kod_info) ", true);
            #if PG
                ExecSQL(conn_db, " analyze t_opl ", true);
            #else
                ExecSQL(conn_db, " Update statistics for table t_opl ", true);
            #endif
                return true;
        }

        //-----------------------------------------------------------------------------
        bool StandardActiveAndInactiveSuppliers(IDbConnection conn_db, PackXX packXX, out Returns ret)
        //-----------------------------------------------------------------------------
        {
            // Cтандартная схема, погашение действующих и недействующих поставщиков равноправно
            ret = Utils.InitReturns();
            return false;
        }

        //-----------------------------------------------------------------------------
        bool StandardSpecial(IDbConnection conn_db, PackXX packXX, out Returns ret)
        //-----------------------------------------------------------------------------
        {
            // Cтандартная схема, погашение специальных квитанций (57,55,83,93,94)
            string sqlText="";
            string str_where="";
            DataTable dt;
            DataTable dt2;            
            DataRow row2;
            int count;
            ret = Utils.InitReturns();
            bool result = false;

            #region Распределить специальные квитанции
            sqlText =
                    " select * from t_selkvar where kod_sum in ("+strKodSumForCharge_X+")" ;
            
            dt = ClassDBUtils.OpenSQL(sqlText, conn_db).GetData();
            foreach (DataRow row in dt.Rows)
            {
                packXX.nzp_pack_ls = Convert.ToInt32(row["nzp_pack_ls"]);

                Ls kvar = new Ls();
                kvar.num_ls = Convert.ToInt32(row["num_ls"]);
                DbAdres dbadres = new DbAdres();
                ReturnsObjectType<Ls> pref = dbadres.GetLsLocation(kvar, conn_db);
                dbadres.Close();
                pref.ThrowExceptionIfError();                

                string baseName = pref.returnsData.pref + "_charge_" + (Convert.ToDateTime(row["dat_month"]).Year - 2000).ToString("00") ;
                dbadres.Close();

                string tableName="";
                string fieldName="";
                string schetName="";
                                
                if (row["kod_sum"].ToString() == "55") // Интернетовские счета
                {
                    #if PG
                    tableName=".charge_g";
                    #else
                    tableName=":charge_g";
                    #endif
                    fieldName="sum_fakt";
                    schetName = "счет получен через Интернет";

                    str_where=
                              " where kod_sum ="+row["kod_sum"].ToString()+" and " +
                              "  num_ls = "+row["num_ls"].ToString()+" AND nzp_serv > 1 "+
                              " AND dat_charge = dat_calc and dat_calc = '"+Convert.ToDateTime(row["dat_month"]).ToShortDateString()+"'  and nzp_supp <> -999 and nzp_supp > 0 and "+
                              " id_bill = "+row["id_bill"].ToString();
                }

                if (row["kod_sum"].ToString() == "57") // Счета по требованию
                {
                    #if PG
                        tableName=".charge_k57";
                        #else
                        tableName=":charge_k57";
                        #endif
                    fieldName="sum_charge";
                    schetName = "счет получен по требованию жильца";

                    str_where=
                              " where kod_sum ="+row["kod_sum"].ToString()+" and " +
                              "  num_ls = "+row["num_ls"].ToString()+" AND nzp_serv > 1 "+
                              " AND dat_charge = dat_calc and dat_calc = '"+Convert.ToDateTime(row["dat_month"]).ToShortDateString()+"'  and nzp_supp <> -999 and nzp_supp > 0 and "+                              
                              " id_bill = "+row["id_bill"].ToString();

                }

                if (row["kod_sum"].ToString() == "83") // Счета на оплату вперёд
                {
                   #if PG
                        tableName=".charge_t";
                    #else
                        tableName=":charge_t";
                    #endif
                    fieldName="sum_charge";
                    schetName = "счет на оплату вперёд";
                    str_where=
                              " where " +
                              "  num_ls = "+row["num_ls"].ToString()+" AND nzp_serv > 1 "+
                              " AND dat_charge = '"+Convert.ToDateTime(row["dat_month"]).ToShortDateString()+"'  and nzp_supp <> -999 and nzp_supp > 0 and "+
                              " id_bill = "+row["id_bill"].ToString();

                }

                if (row["kod_sum"].ToString() == "93" || row["kod_sum"].ToString() == "94")  // Долговые счета
                {
                    #if PG
                        tableName=".charge_d";
                    #else
                        tableName=":charge_d";
                    #endif
                    fieldName="sum_charge";
                    schetName = "счет погашение долга";

                    str_where=
                              " where " +
                              "  num_ls = "+row["num_ls"].ToString()+" AND nzp_serv > 1 "+
                              " AND dat_charge = '"+Convert.ToDateTime(row["dat_month"]).ToShortDateString()+"'  and nzp_supp <> -999 and nzp_supp > 0 and "+
                              " id_bill = "+row["id_bill"].ToString();
                }

                if (Points.packDistributionParameters.EnableLog )
                {
                       MessageInPackLog(conn_db, packXX, "Алгоритм распределения № "+row["kod_sum"].ToString()+". Распределение оплаты по счету "+row["kod_sum"].ToString()+" ("+schetName.Trim()+") № "+row["id_bill"]+" )", false, out ret);
                }


                sqlText =
                        " select sum("+fieldName+") sum_1 from " + baseName+tableName + str_where;
                dt2 = ClassDBUtils.OpenSQL(sqlText, conn_db).GetData();
                row2 = dt2.Rows[0];
                decimal sum_charge = 0;

                if (row2["sum_1"] != DBNull.Value)
                {
                    sum_charge = Convert.ToDecimal(row2["sum_1"]);
                };
                if (Convert.ToDecimal(row["g_sum_ls"]) == sum_charge)
                {
                        // Сумма по квитанции совпадает с суммой оплаты
                      #if PG
                        sqlText =
                        " Insert into " + pref.returnsData.pref + "_charge_" + (Convert.ToDateTime(Points.DateOper).Year % 100).ToString("00")+
                        "." + "fn_supplier" + (Convert.ToDateTime(Points.DateOper).Month % 100).ToString("00") +
                        " ( num_ls, nzp_pack_ls, nzp_serv, nzp_supp, sum_prih, kod_sum, dat_month, dat_prih, dat_uchet, s_dolg, s_user, s_forw ) " +
                        " Select num_ls, "+row["nzp_pack_ls"]+", nzp_serv, nzp_supp, "+fieldName+",  "+row["kod_sum"].ToString()+", '" + Convert.ToDateTime(row["dat_month"]).ToShortDateString() + "', '"+Convert.ToDateTime(Points.DateOper).ToShortDateString()+"', '" + Convert.ToDateTime(Points.DateOper).ToShortDateString() + "', 0, "+fieldName+", 0 " +
                        " From  " + pref.returnsData.pref + "_charge_" + (Convert.ToDateTime(Points.DateOper).Year % 100).ToString("00") + tableName +
                        str_where;
                    #else
                        sqlText =
                        " Insert into " + pref.returnsData.pref + "_charge_" + (Convert.ToDateTime(Points.DateOper).Year % 100).ToString("00")+
                        ":" + "fn_supplier" + (Convert.ToDateTime(Points.DateOper).Month % 100).ToString("00") +
                        " ( num_ls, nzp_pack_ls, nzp_serv, nzp_supp, sum_prih, kod_sum, dat_month, dat_prih, dat_uchet, s_dolg, s_user, s_forw ) " +
                        " Select num_ls, "+row["nzp_pack_ls"]+", nzp_serv, nzp_supp, "+fieldName+",  "+row["kod_sum"].ToString()+", '" + Convert.ToDateTime(row["dat_month"]).ToShortDateString() + "', '"+Convert.ToDateTime(Points.DateOper).ToShortDateString()+"', '" + Convert.ToDateTime(Points.DateOper).ToShortDateString() + "', 0, "+fieldName+", 0 " +
                        " From  " + pref.returnsData.pref + "_charge_" + (Convert.ToDateTime(Points.DateOper).Year % 100).ToString("00") + tableName +
                        str_where;
                    #endif
                    IntfResultType retres = ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log);
                    ret = retres.GetReturnsType().GetReturns();
                    if (!ret.result)
                    {
                            DropTempTablesPack(conn_db);
                            MessageInPackLog(conn_db, packXX, "Ошибка распределения 7."+row["kod_sum"].ToString()+".1", true, out ret);
                            Utility.ExceptionUtility.OnException(new Exception(ret.text), System.Reflection.MethodBase.GetCurrentMethod().Name);
                            return false;
                    }

                    #if PG
                        count = retres.resultAffectedRows;
                    #else
                        count = ClassDBUtils.GetAffectedRowsCount(conn_db);
                    #endif
                    if (count > 0)
                    {
                            if (Points.packDistributionParameters.EnableLog)
                            {
                                MessageInPackLog(conn_db, packXX, "Распределено " + row["g_sum_ls"] + " руб ", false, out ret);
                            }

                        }
                    #if PG
                        sqlText =
                        " update " + Points.Pref + "_fin_" + (Convert.ToDateTime(Points.DateOper).Year - 2000).ToString("00") + ".pack_ls set inbasket = 0, alg = "+row["kod_sum"].ToString()+", dat_uchet = '"+Convert.ToDateTime(Points.DateOper).ToShortDateString()+"' where nzp_pack_ls = " +
                        row["nzp_pack_ls"];
                    #else
                        sqlText =
                        " update " + Points.Pref + "_fin_" + (Convert.ToDateTime(Points.DateOper).Year - 2000).ToString("00") + ":pack_ls set inbasket = 0, alg = "+row["kod_sum"].ToString()+", dat_uchet = '"+Convert.ToDateTime(Points.DateOper).ToShortDateString()+"' where nzp_pack_ls = " +
                        row["nzp_pack_ls"];
                    #endif

                    ret = ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log).GetReturnsType().GetReturns();
                    if (!ret.result)
                    {
                            DropTempTablesPack(conn_db);
                            MessageInPackLog(conn_db, packXX, "Ошибка распределения 7." + row["kod_sum"].ToString() + ".2", true, out ret);
                            Utility.ExceptionUtility.OnException(new Exception(ret.text), System.Reflection.MethodBase.GetCurrentMethod().Name);
                            return false;
                     }

                     #if PG
                            sqlText = " delete from  " + Points.Pref + "_fin_" + (Convert.ToDateTime(Points.DateOper).Year - 2000).ToString("00") + ".pack_ls_err  where nzp_pack_ls = " +
                                    row["nzp_pack_ls"] + " and nzp_err = 600 ";
                     #else
                            sqlText = " delete from  " + Points.Pref + "_fin_" + (Convert.ToDateTime(Points.DateOper).Year - 2000).ToString("00") + ":pack_ls_err  where nzp_pack_ls = " +
                                    row["nzp_pack_ls"] + " and nzp_err = 600 ";
                     #endif
                     ret = ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log).GetReturnsType().GetReturns();

                     return true;
                }
                else
                {
                       #if PG
                            sqlText =
                            " update " + Points.Pref + "_fin_" + (Convert.ToDateTime(Points.DateOper).Year - 2000).ToString("00") + ".pack_ls set inbasket = 1, alg = 0, dat_uchet = null where nzp_pack_ls = " +
                            row["nzp_pack_ls"];
                       #else
                            sqlText =
                            " update " + Points.Pref + "_fin_" + (Convert.ToDateTime(Points.DateOper).Year - 2000).ToString("00") + ":pack_ls set inbasket = 1, alg = 0, dat_uchet = null where nzp_pack_ls = " +
                            row["nzp_pack_ls"];
                       #endif
                        ret = ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log).GetReturnsType().GetReturns();
                        if (!ret.result)
                        {
                            DropTempTablesPack(conn_db);
                            MessageInPackLog(conn_db, packXX, "Ошибка распределения 7." + row["kod_sum"].ToString() + ".3", true, out ret);
                            Utility.ExceptionUtility.OnException(new Exception(ret.text), System.Reflection.MethodBase.GetCurrentMethod().Name);
                            return false;
                        }
                         
                        #if PG
                            sqlText = " delete from  " + Points.Pref + "_fin_" + (Convert.ToDateTime(Points.DateOper).Year - 2000).ToString("00") + ".pack_ls_err  where nzp_pack_ls = " +
                                    row["nzp_pack_ls"]+" and nzp_err in (1, 600) ";
                        #else
                            sqlText = " delete from  " + Points.Pref + "_fin_" + (Convert.ToDateTime(Points.DateOper).Year - 2000).ToString("00") + ":pack_ls_err  where nzp_pack_ls = " +
                                    row["nzp_pack_ls"]+" and nzp_err in (1, 600) ";
                        #endif
                        ret = ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log).GetReturnsType().GetReturns();
                        #if PG
                            sqlText =
                            " Insert into " + Points.Pref + "_fin_" + (Convert.ToDateTime(Points.DateOper).Year - 2000).ToString("00") + ".pack_ls_err " +
                            " (  nzp_pack_ls, nzp_err, note ) " +
                            " values(" + row["nzp_pack_ls"] + ",600,'Для счёта с кодом " + row["kod_sum"].ToString() + "(" + schetName.Trim() + ") выставлено sum_charge, сумма платежа " + row["g_sum_ls"] + "')";
                        #else
                            sqlText =
                            " Insert into " + Points.Pref + "_fin_" + (Convert.ToDateTime(Points.DateOper).Year - 2000).ToString("00") + ":pack_ls_err " +
                            " (  nzp_pack_ls, nzp_err, note ) " +
                            " values(" + row["nzp_pack_ls"] + ",600,'Для счёта с кодом " + row["kod_sum"].ToString() + "(" + schetName.Trim() + ") выставлено sum_charge, сумма платежа " + row["g_sum_ls"] + "')";
                        #endif
                        ret = ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log).GetReturnsType().GetReturns();
                        if (Points.packDistributionParameters.EnableLog)
                        {
                            MessageInPackLog(conn_db, packXX, "Для счёта с кодом " + row["kod_sum"].ToString() + "(" + schetName.Trim() + ") выставлено sum_charge, сумма платежа " + row["g_sum_ls"], false, out ret);
                            MessageInPackLog(conn_db, packXX, "Оплата помещена в корзину", false, out ret);
                        }                   
                }
            }
            return result;
            #endregion Распределить специальные квитанции
        }

        //откат пачки и удаление
        public bool CalcPackDel(CalcTypes.ParamCalc paramcalc, out Returns ret)
        {
            string conn_kernel = Points.GetConnByPref(paramcalc.pref);
            IDbConnection conn_db = GetConnection(conn_kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result)
            {
                return false;
            }

            bool result = CalcPackDel(conn_db, paramcalc, out ret);

            conn_db.Close();

            return result;
        }

        public bool CalcPackDel(IDbConnection conn_db, CalcTypes.ParamCalc paramcalc, out Returns ret)
        {
            ret = Utils.InitReturns();
            Returns r = Utils.InitReturns();

            bool b = CalcPackOt(conn_db, paramcalc, 0, out ret);
            if (!b) return false;

            PackXX packXX = new PackXX(paramcalc, 0, true);

            MessageInPackLog(conn_db, packXX, "Начало удаления пачки", false, out ret);

            IDataReader reader;
            ret = ExecRead(conn_db, out reader,
                " Select par_pack From " + packXX.pack +
                " Where nzp_pack = " + packXX.nzp_pack
                , true);
            if (!ret.result)
            {
                MessageInPackLog(conn_db, packXX, "Ошибка удаления 1.1", true, out r);
                return false;
            }

            int par_pack = 0;
            try
            {
                if (reader.Read())
                {
                    if (reader["par_pack"] != DBNull.Value)
                        par_pack = (int)reader["par_pack"];
                }
            }
            catch
            {
            }

            //нельзя сразу удалять pack_ls, надо проверить, что пачка откачена во всех локальных банках!!!
            //foreach (_Point zap in Points.PointList)
            //{
            //}

            ret = ExecSQL(conn_db,
                " Set isolation dirty read "
                , true);
            if (!ret.result)
            {
                MessageInPackLog(conn_db, packXX, "Ошибка удаления 1.2", true, out r);
                return false;
            }


            //проверим, что все dat_uchet'ы пустые - т.е. лицевые счета откачены!
            ret = ExecRead(conn_db, out reader,
                " Select nzp_pack_ls From " + packXX.pack_ls +
                " Where nzp_pack = " + packXX.nzp_pack +
                "   and dat_uchet is not null "
                , true);
            if (!ret.result)
            {
                MessageInPackLog(conn_db, packXX, "Ошибка удаления 1.3", true, out r);
                return false;
            }

            try
            {
                if (reader.Read())
                {
                    //значит еще не все откатаны, удалять сейчас нельзя, выходим
                    return true;
                }
            }
            catch
            {
            }


         #if PG
                ret = ExecSQL(conn_db,
                " Delete From " +Points.Pref + "_fin_" + (paramcalc.calc_yy - 2000).ToString("00") + paramcalc.ol_srv + ".gil_sums where nzp_pack_ls in (select nzp_pack_ls from " + packXX.pack_ls +
                " Where nzp_pack = " + packXX.nzp_pack+")"
                , true);
         #else
                ret = ExecSQL(conn_db,
                " Delete From " +Points.Pref + "_fin_" + (paramcalc.calc_yy - 2000).ToString("00") + paramcalc.ol_srv + ":gil_sums where nzp_pack_ls in (select nzp_pack_ls from " + packXX.pack_ls +
                " Where nzp_pack = " + packXX.nzp_pack+")"
                , true);
         #endif
         if (!ret.result)
         {
                MessageInPackLog(conn_db, packXX, "Ошибка удаления 1.4", true, out r);
                return false;
         }

         ret = ExecSQL(conn_db,
                " Delete From " + packXX.pack_ls +
                " Where nzp_pack = " + packXX.nzp_pack
                , true);
         if (!ret.result)
         {
                MessageInPackLog(conn_db, packXX, "Ошибка удаления 1.4", true, out r);
                return false;
         }

         ret = ExecSQL(conn_db,
                " Delete From " + packXX.pack +
                " Where nzp_pack = " + packXX.nzp_pack
                , true);
            if (!ret.result)
            {
                MessageInPackLog(conn_db, packXX, "Ошибка удаления 1.5", true, out r);
                return false;
            }

            MessageInPackLog(conn_db, packXX, "Окончание удаления пачки", false, out ret);

            //ищем подпачки у суперпачки
            if (par_pack > 0)
            {
                ret = ExecRead(conn_db, out reader,
                    " Select nzp_pack From " + packXX.pack +
                    " Where par_pack = " + par_pack +
                    "   and nzp_pack <> par_pack "
                    //"   and nzp_pack not in ( Select nzp_pack From " + packXX.pack + ")"
                    , true);
                if (!ret.result)
                {
                    MessageInPackLog(conn_db, packXX, "Ошибка удаления 1.6", true, out r);
                    return false;
                }

                b = true;
                try
                {
                    if (reader.Read())
                    {
                        //значит еще есть неоткаченные пачки, не трогаем суперпачку
                        b = false; 
                    }
                }
                catch
                {
                }
                if (b)
                {
                    //нет подпачек, удаляем суперпачку
                    ret = ExecSQL(conn_db,
                        " Delete From " + packXX.pack +
                        " Where nzp_pack = " + par_pack +
                        "   and nzp_pack = par_pack "
                        , true);
                    if (!ret.result)
                    {
                        MessageInPackLog(conn_db, packXX, "Ошибка удаления 1.7", true, out r);
                        return false;
                    }

                    MessageInPackLog(conn_db, packXX, "Суперпачка удалена", false, out ret);
                }
            }
            return true;
        }


        /// <summary>
        /// Отмена распределения пачки оплат
        /// </summary>
        /// <param name="paramcalc"></param>
        /// <param name="ret"></param>
        /// <returns></returns>
        public bool CalcPackOt(CalcTypes.ParamCalc paramcalc, out Returns ret)
        {
            string conn_kernel = Points.GetConnByPref(paramcalc.pref);
            IDbConnection conn_db = GetConnection(conn_kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result)
            {
                return false;
            }

            bool result = CalcPackOt(conn_db, paramcalc, 0, out ret);

            conn_db.Close();

            return result;
        }

        public bool CalcPackOt(IDbConnection conn_db, CalcTypes.ParamCalc paramcalc, out Returns ret)
        {
            return CalcPackOt(conn_db, paramcalc, 0, out ret);
        }

        public bool CalcPackOt(IDbConnection conn_db, CalcTypes.ParamCalc paramcalc, int nzp_pack_ls, out Returns ret)
        {

            if (Constants.Trace) Utility.ClassLog.WriteLog("Старт CalcPackOt");
            ret = Utils.InitReturns();
            string sql_text = "";

           // Заполнение информации для портала Небо для реестре №  nzp_nebo_reestr за период pDat_uchet_from gj DateTime pDat_uchet_to           
           //bool b=FillSumOplForNebo(conn_db, 1, "04.10.2013", "04.10.2013", out ret);
            DropTempTablesPack(conn_db);
            ret = ExecSQL(conn_db,"delete from t_selkvar", false);

            Returns r = Utils.InitReturns();
            PackXX packXX = new PackXX(paramcalc, nzp_pack_ls, true);

            MessageInPackLog(conn_db, packXX, "Начало отката распределения", false, out ret);


            //выбрать мно-во лицевых счетов

            if (paramcalc.pref.Trim() == "")
            {
                #if PG
                    sql_text =
                    " update  " + packXX.pack_ls + "  set inbasket = 0 , alg = 0, dat_uchet = null " +
                    " Where  nzp_pack = " + packXX.nzp_pack + " and coalesce(num_ls,0)=0 " ;
                #else
                    sql_text =
                    " update  " + packXX.pack_ls + "  set inbasket = 0 , alg = 0, dat_uchet = null " +
                    " Where  nzp_pack = " + packXX.nzp_pack + " and NVL(num_ls,0)=0 " ;
                #endif
                ret = ExecSQL(conn_db,
                    sql_text
                    , true);
                if (!ret.result)
                {
                    MessageInPackLog(conn_db, packXX, "Ошибка отката 1.1", true, out r);
                    DropTempTablesPack(conn_db);
                    return false;
                }
                ret.result = true;
                return true;
            }



            int yy = Points.DateOper.Year;
            int mm = Points.DateOper.Month;

            string first_day = "01." + mm + "." + yy;
            string last_day = DateTime.DaysInMonth(yy, mm).ToString() + "." +
            mm + "." + yy;


            //kand2
            //if (Constants.Trace) Utility.ClassLog.WriteLog("Старт Выборка оплат");
            #if PG
                sql_text =
                    " Select distinct p.nzp_pack_ls, k.nzp_dom,p.nzp_pack, k.nzp_area, k.nzp_geu "+
                      " Into temp t_opl "+
                      " From " + paramcalc.pref + "_data.kvar k, " + packXX.pack_ls + " p " +
                    " Where (k.num_ls = p.num_ls " +
                    "   and p.nzp_pack = " + packXX.nzp_pack +
                    "   and k." + packXX.paramcalc.where_z +
                    "   and p." + packXX.where_pack_ls +
                    "   and  ( (p.dat_uchet between '" + first_day + "' and '" + last_day + "') or (p.inbasket=1))  )" +
                    " union all " +
                    " Select distinct p.nzp_pack_ls,0 nzp_dom,p.nzp_pack, 0 nzp_area, 0 nzp_geu From " + packXX.pack_ls + " p " +
                    " Where  p.nzp_pack = " + packXX.nzp_pack + " and coalesce(p.num_ls,0)=0 ";
#else
            sql_text =
                " Select unique p.nzp_pack_ls, k.nzp_dom,p.nzp_pack, k.nzp_area, k.nzp_geu From " + paramcalc.pref + "_data:kvar k, " + packXX.pack_ls + " p " +
                " Where (k.num_ls = p.num_ls " +
                "   and p.nzp_pack = " + packXX.nzp_pack +
                "   and k." + packXX.paramcalc.where_z +
                "   and p." + packXX.where_pack_ls +
                "   and  ( (p.dat_uchet between '" + first_day + "' and '" + last_day + "') or (p.inbasket=1))  )" +
                " union all " +
                " Select unique p.nzp_pack_ls,0 nzp_dom,p.nzp_pack, 0 nzp_area, 0 nzp_geu From " + packXX.pack_ls + " p " +
                " Where  p.nzp_pack = " + packXX.nzp_pack +" and NVL(p.num_ls,0)=0 "+
                " Into temp t_opl With no log ";
            #endif
            #if PG
                IntfResultType retres = ClassDBUtils.ExecSQL(sql_text, conn_db, ClassDBUtils.ExecMode.Log);
                ret = retres.GetReturnsType().GetReturns();
            #else
                ret = ExecSQL(conn_db,
                sql_text
                , true);
            #endif
            //if (Constants.Trace) Utility.ClassLog.WriteLog("Стоп Выборка оплат");
            if (!ret.result)
            {
                MessageInPackLog(conn_db, packXX, "Ошибка отката 1.1", true, out r);
                DropTempTablesPack(conn_db);
                return false;
            }
            #if PG
                int count = retres.resultAffectedRows;
            #else
                int count = ClassDBUtils.GetAffectedRowsCount(conn_db);
            #endif
           
            if (count==0)
            {
                    string month_="";
                    switch (mm)
                    {
                           case 1: month_ = "январе"; break;
                           case 2: month_ = "феврале"; break;
                           case 3: month_ = "марте"; break;
                           case 4: month_ = "апреле"; break;
                           case 5: month_ = "мае"; break;
                           case 6: month_ = "июне"; break;
                           case 7: month_ = "июле"; break;
                           case 8: month_ = "августе"; break;
                           case 9: month_ = "сентябре"; break;
                           case 10: month_ = "октябре"; break;
                           case 11: month_ = "ноябре"; break;
                           case 12: month_ = "декабре"; break;
                  }
                    ret.tag = -1;
                    ret.text = "Отсутствуют оплаты для отмены распределения. Данная операция может быть произведена только над оплатами, распределёнными в " + month_ + " " + yy + " г.";
                    return false;
            }

            ExecSQL(conn_db, " Create unique index ix4_t_opl on t_opl (nzp_pack_ls) ", true);
            #if PG
                ExecSQL(conn_db, " analyze t_opl ", true);
            #else
                ExecSQL(conn_db, " Update statistics for table t_opl ", true);
            #endif

            //откат распределения
            //string connectionString;
            //if (Constants.Trace) Utility.ClassLog.WriteLog("Старт Удаление fn_supplier");
            string tabname_supplier;

            // Получить тип оплаты  (pack_type: 10 - деньги РЦ, 20 - Чужие деньги)
            sqlText =
                   " Select pack_type, nzp_supp From " + packXX.pack +
                   " Where nzp_pack = " + packXX.nzp_pack;
            DataTable dtpack_type = ClassDBUtils.OpenSQL(sqlText, conn_db).GetData();
            DataRow row_pack_type;
            row_pack_type = dtpack_type.Rows[0];
            if (row_pack_type["pack_type"] != DBNull.Value)
            {
                pack_type = Convert.ToInt32(row_pack_type["pack_type"]);
            }
            if (row_pack_type["nzp_supp"] != DBNull.Value)
            {
                nzp_supp = Convert.ToInt32(row_pack_type["nzp_supp"]);
            }
            else
            {
                nzp_supp = 0;
            }



            foreach (_Point zap in Points.PointList)
            {
                #if PG
                {
                    if (pack_type == 20)
                    {
                        tabname_supplier = zap.pref + "_charge_" + (Points.DateOper.Year - 2000).ToString("00") + ".from_supplier";
                        packXX.fn_supplier = packXX.fn_supplier.Replace(packXX.fn_supplier_tab, "from_supplier");
                    } else
                    {
                        tabname_supplier = zap.pref + "_charge_" + (Points.DateOper.Year - 2000).ToString("00") + ".fn_supplier" + Points.DateOper.Month.ToString("00");
                    }                    
                }
                #else
                    if (pack_type == 20)
                    {
                        tabname_supplier = zap.pref + "_charge_" + (Points.DateOper.Year - 2000).ToString("00") + ":from_supplier";
                    }
                    else
                    {
                        tabname_supplier = zap.pref + "_charge_" + (Points.DateOper.Year - 2000).ToString("00") + ":fn_supplier" + Points.DateOper.Month.ToString("00");
                    }
                #endif
                #if PG
                    ret = ExecSQL(conn_db, " Delete From " + tabname_supplier +
                                " Where nzp_pack_ls in ( Select nzp_pack_ls From t_opl )  ", true);
                #else
                    ret=ExecSQL(conn_db, " Delete From " + tabname_supplier +
                            " Where nzp_pack_ls in ( Select nzp_pack_ls From "+Points.Pref+"_kernel:t_opl )  ", true);
                #endif



            }
            UpdateStatistics(true, packXX.paramcalc, packXX.fn_supplier_tab, out ret);


            #region Пересчитать сальдо поставщиков для всех затронутых оплат, по датам, больше чем минимальная дата распределения в пачке
                    #if PG
                                sql_text =
                                    " Select distinct nzp_pack_ls, nzp_dom, nzp_pack, nzp_area, nzp_geu "+
                                        " Into temp t_selkvar "+
                                        " From t_opl p ";
                    #else
                                sql_text =
                                        " Select unique nzp_pack_ls,  nzp_dom,nzp_pack, nzp_area, nzp_geu From t_opl  " +
                                        " Into temp t_selkvar With no log ";
                    #endif
                    #if PG
                                                        //IntfResultType 
                                                        retres = ClassDBUtils.ExecSQL(sql_text, conn_db, ClassDBUtils.ExecMode.Log);
                                                        ret = retres.GetReturnsType().GetReturns();
                    #else
                                ret = ExecSQL(conn_db,
                                sql_text
                                , true);
                    #endif

                    DataTable dt;
                    DataRow row;


                    sql_text = "select MIN(dat_uchet) as dat from " + packXX.pack_ls + " Where nzp_pack_ls in ( Select nzp_pack_ls From t_opl ) ";
                    dt = ClassDBUtils.OpenSQL(sql_text, conn_db).GetData();
                    row = dt.Rows[0];
                    string min_date = "";
                    if (row["dat"] != DBNull.Value)
                    {
                        min_date = Convert.ToDateTime(row["dat"]).ToShortDateString();
                    }
                    /*
                    sql_text = " delete from " + packXX.fn_operday_log + " where date_oper =  '" + min_date + "'";                        
                    Points.ret = ExecSQL(conn_db, sql_text, true);

                    #if PG
                    ret = ExecSQL(conn_db,
                                        " Insert into " + packXX.fn_operday_log + " (date_oper, nzp_user, date_inp, faza) " +
                                        " select distinct dat_uchet,1,now(),3 from pack_ls where nzp_pack_ls in ( Select nzp_pack_ls From t_opl ) "
                                        , true);

                    #else

                    sql_text = " Insert into " + packXX.fn_operday_log + " (date_oper, nzp_user, date_inp, faza) " +
                        " select distinct dat_oper,"+packXX.paramcalc.nzp_user+",current,3 from   " + packXX.fn_distrib + " where nzp_dom in (select distinct nzp_dom from t_selkvar) and dat_oper>='" + min_date + "'";
                    ret = ExecSQL(conn_db, sql_text, true);

                    #endif
                    */
            #endregion Пересчитать сальдо поставщиков для всех затронутых оплат, по датам, больше чем минимальная дата распределения в пачке
            
            ret = ExecSQL(conn_db,
                " Update " + packXX.pack_ls +
                " Set dat_uchet = null " +
            #if PG
                ", date_rdistr = now(), alg = 0, " +
            #else
                ", date_rdistr = current, alg = 0, " +
            #endif
                  "inbasket = 0 " +
                " Where nzp_pack_ls in ( Select nzp_pack_ls From t_opl ) "
                , true);
            if (!ret.result)
            {
                MessageInPackLog(conn_db, packXX, "Ошибка отката 1.3", true, out r);
                DropTempTablesPack(conn_db);
                return false;
            }

            // Сохранить запись в журнал событий
            if (min_date.Trim() != "")
            {
                int nzp_event = SaveEvent(6612, conn_db, paramcalc.nzp_user, paramcalc.nzp_pack, "Операционный день " + packXX.paramcalc.dat_oper);
                if ( (nzp_event > 0) && (isDebug) )
                {
                    ret = ExecSQL(conn_db, "insert into " + Points.Pref + "_data"+tableDelimiter+"sys_event_detail(nzp_event, table_, nzp) select distinct " + nzp_event + ",'" + packXX.pack_ls + "',nzp_pack_ls from t_opl  ", true);
                }




                // Пересчитать сальдо по перечислениям
                //sql_text = "select distinct date_oper from " + packXX.fn_operday_log + " where faza=3 ";

                DateTime dat = Convert.ToDateTime(min_date);
                DateTime lastDayDatCalc = Convert.ToDateTime(DateTime.DaysInMonth(Points.DateOper.Year, Points.DateOper.Month).ToString("00") +
                     "." + Points.DateOper.Month.ToString() + "." + Points.DateOper.Year.ToString());

                while (dat <= lastDayDatCalc)
                {
                    paramcalc.DateOper = dat;
                    if (isAutoDistribPaXX)
                    {
                        DistribPaXX(conn_db, paramcalc, out ret);
                        if (!ret.result)
                        {
                            DropTempTablesPack(conn_db);
                            return false;
                        }
                    }
                    else
                    {
                        ret = ExecSQL(conn_db, " insert into  " + packXX.fn_operday_log + "(nzp_dom, date_oper) select distinct nzp_dom,'" + dat.ToShortDateString() + "'" + sConvToDate + " from t_selkvar where nzp_dom>0 ", true);
                        break;
                    }
                    dat = dat.AddDays(1);

                }

            }

            //сразу запустить расчет сальдо по nzp_pack
            MessageInPackLog(conn_db, packXX, "Расчет сальдо лицевых счетов", false, out r);
            //if (Constants.Trace) Utility.ClassLog.WriteLog("Старт CalcChargeXXUchetOplatForPack");
            ret = CalcChargeXXUchetOplatForPack(conn_db, packXX);
            //if (Constants.Trace) Utility.ClassLog.WriteLog("Стоп CalcChargeXXUchetOplatForPack");

            if (!ret.result)
            {
                DropTempTablesPack(conn_db);
                return false;
            }

            MessageInPackLog(conn_db, packXX, "Конец отката распределения", false, out r);
            DropTempTablesPack(conn_db);
            if (Constants.Trace) Utility.ClassLog.WriteLog("Стоп CalcPackOt");
            return true;
        }

        public bool CalcPackLs2(int nzp_pack_ls, DateTime date, bool to_dis, bool is_manual, out Returns ret, int nzp_user)
        {
            ret = Utils.InitReturns();

            int yy = date.Year;
            int mm = date.Month;


#if PG
            string kvar = Points.Pref + "_data.kvar ";
            string pack_ls = Points.Pref + "_fin_" + (yy - 2000).ToString("00") + ".pack_ls ";
#else
            string kvar = Points.Pref + "_data:kvar ";
            string pack_ls = Points.Pref + "_fin_" + (yy - 2000).ToString("00") + ":pack_ls ";
#endif

            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result)
            {
                return false;
            }

            IDataReader reader;
#if PG
            ret = ExecRead(conn_db, out reader,
                            " Select distinct k.pref, k.nzp_kvar, p.nzp_pack, p.dat_uchet " +
                            " From " + kvar + " k, " + pack_ls + " p" +
                            " Where k.num_ls = p.num_ls and p.nzp_pack_ls = " + nzp_pack_ls
                            , true);
#else
            ret = ExecRead(conn_db, out reader,
                " Select unique k.pref, k.nzp_kvar, p.nzp_pack, p.dat_uchet " +
                " From " + kvar + " k, " + pack_ls + " p" +
                " Where k.num_ls = p.num_ls and p.nzp_pack_ls = " + nzp_pack_ls
                , true);
#endif
            if (!ret.result)
            {
                conn_db.Close();
                return false;
            }


            string pref = "";
            int nzp_pack = 0;
            int nzp_kvar = 0;

            try
            {
                if (reader.Read())
                {
                    if (reader["pref"] != DBNull.Value)
                        pref = (string)reader["pref"];
                    if (reader["nzp_pack"] != DBNull.Value)
                        nzp_pack = (int)reader["nzp_pack"];
                    if (reader["nzp_kvar"] != DBNull.Value)
                        nzp_kvar = (int)reader["nzp_kvar"];
                }
            }
            catch (Exception ex)
            {
                reader.Close();
                ret.result = false;
                ret.text = ex.Message;
                MonitorLog.WriteLog(ret.text, MonitorLog.typelog.Error, 1, 222, true);
                return false;
            }

            reader.Close();

            bool b = false;

            if (nzp_kvar > 0)
            {
                CalcTypes.ParamCalc paramcalc = new CalcTypes.ParamCalc(nzp_kvar, nzp_pack, pref.Trim(), yy, mm, yy, mm, nzp_user);
                paramcalc.b_pack = true;
                paramcalc.b_packOt = !to_dis;

                
                paramcalc.DateOper = date;
               

                bool isManualDistribution = is_manual;  //признак, что распределение ручное

                if (paramcalc.b_packOt)
                    b = CalcPackOt(conn_db, paramcalc, nzp_pack_ls, out ret);        //откат распределения 
                else
                {
                    b = CalcPackXX(conn_db, paramcalc, nzp_pack_ls, false, isManualDistribution, out ret); //распределение оплаты
                    // Обновить fn_distrib_dom_XX
                    //DistribPaXX(conn_db, paramcalc, out ret);
                }


            }
            else
            {
                if (nzp_pack_ls > 0)
                {
#if PG
                    ret = ExecSQL(conn_db,
                            " delete from " + Points.Pref + "_fin_" + (Points.DateOper.Year % 100).ToString("00") +
                            ".pack_ls_err where nzp_pack_ls=" + nzp_pack_ls
                            , true);
#else
                    ret = ExecSQL(conn_db,
                        " delete from " + Points.Pref + "_fin_" + (Points.DateOper.Year % 100).ToString("00") + "@" + DBManager.getServer(conn_db) +
                       ":pack_ls_err where nzp_pack_ls=" + nzp_pack_ls
                        , true);
#endif
#if PG
                    ret = ExecSQL(conn_db,
                                   " Insert into " + Points.Pref + "_fin_" + (Points.DateOper.Year % 100).ToString("00") +
                                  ".pack_ls_err ( nzp_pack_ls,nzp_err,note ) " +
                                   " values (" + nzp_pack_ls + ",666,'Недопустимый платёжный код')"
                                   , true);
#else
                    ret = ExecSQL(conn_db,
                    " Insert into " + Points.Pref + "_fin_" + (Points.DateOper.Year % 100).ToString("00") + "@" + DBManager.getServer(conn_db) +
                   ":pack_ls_err ( nzp_pack_ls,nzp_err,note ) " +
                    " values (" + nzp_pack_ls + ",666,'Недопустимый платёжный код')"
                    , true);
#endif

                    if (!ret.result)
                    {
                        DropTempTablesPack(conn_db);
                        return false;
                    }

                }

            }


            conn_db.Close();
            return b;
        }

        /// <summary>
        /// распределение одного лс
        /// </summary>
        /// <param name="nzp_pack_ls"></param>
        /// <param name="date"></param>
        /// <param name="to_dis"></param>
        /// <param name="ret"></param>
        /// <returns></returns>
        public bool CalcPackLs(int nzp_pack_ls, DateTime date, bool to_dis, bool is_manual, out Returns ret, int nzp_user)
        {
            ret = Utils.InitReturns();

            // !!! Исправил Марат 17.12.2012. Распределение оплаты (через корзину например) должно производится текущим оперднём
            //int yy = date.Year;
            //int mm = date.Month;

            int yy = Points.DateOper.Year;
            int mm = Points.DateOper.Month;



#if PG
            string kvar = Points.Pref + "_data.kvar ";
            string pack_ls = Points.Pref + "_fin_" + (yy - 2000).ToString("00") + ".pack_ls ";
#else
            string kvar = Points.Pref + "_data:kvar ";
            string pack_ls = Points.Pref + "_fin_" + (yy - 2000).ToString("00") + ":pack_ls ";
#endif

            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result)
            {
                return false;
            }

            IDataReader reader;
#if PG
            ret = ExecRead(conn_db, out reader,
                            " Select distinct k.pref, k.nzp_kvar, p.nzp_pack, p.dat_uchet " +
                            " From " + kvar + " k, " + pack_ls + " p" +
                            " Where k.num_ls = p.num_ls and p.nzp_pack_ls = " + nzp_pack_ls
                            , true);
#else
            ret = ExecRead(conn_db, out reader,
                " Select unique k.pref, k.nzp_kvar, p.nzp_pack, p.dat_uchet " +
                " From " + kvar + " k, " + pack_ls + " p" +
                " Where k.num_ls = p.num_ls and p.nzp_pack_ls = " + nzp_pack_ls
                , true);
#endif
            if (!ret.result)
            {
                conn_db.Close();
                return false;
            }

            
            string pref = "";
            int nzp_pack = 0;
            int nzp_kvar = 0;

            try
            {
                if (reader.Read())
                {
                    if (reader["pref"] != DBNull.Value)
                        pref = (string)reader["pref"];
                    if (reader["nzp_pack"] != DBNull.Value)
                        nzp_pack = (int)reader["nzp_pack"];
                    if (reader["nzp_kvar"] != DBNull.Value)
                        nzp_kvar = (int)reader["nzp_kvar"];
                }
            }
            catch (Exception ex)
            {
                reader.Close();
                ret.result = false;
                ret.text = ex.Message;
                MonitorLog.WriteLog(ret.text, MonitorLog.typelog.Error, 1, 222, true);
                return false;
            }

            reader.Close();

            bool b = false;

            if (nzp_kvar > 0)
            {
                CalcTypes.ParamCalc paramcalc = new CalcTypes.ParamCalc(nzp_kvar, nzp_pack, pref.Trim(), yy, mm, yy, mm, nzp_user);
                paramcalc.b_pack = true;
                paramcalc.b_packOt = !to_dis;

                // !!! Исправил Марат 17.12.2012. Распределение оплаты (через корзину например) должно производится текущим оперднём                
                //paramcalc.DateOper = date;
                paramcalc.DateOper = Points.DateOper;

                bool isManualDistribution = is_manual;  //признак, что распределение ручное

                if (paramcalc.b_packOt)
                    b = CalcPackOt(conn_db, paramcalc, nzp_pack_ls, out ret);        //откат распределения 
                else
                {
                    b = CalcPackXX(conn_db, paramcalc, nzp_pack_ls, false, isManualDistribution, out ret); //распределение оплаты
                    // Обновить fn_distrib_dom_XX
                    //DistribPaXX(conn_db, paramcalc, out ret);
                }


            }
            else
            {
                if (nzp_pack_ls >0 )
                {
#if PG
                    ret = ExecSQL(conn_db,
                            " delete from " + Points.Pref + "_fin_" + (Points.DateOper.Year % 100).ToString("00") +
                            ".pack_ls_err where nzp_pack_ls=" + nzp_pack_ls
                            , true);
#else
                    ret = ExecSQL(conn_db,
                        " delete from " + Points.Pref + "_fin_" + (Points.DateOper.Year % 100).ToString("00") + "@" + DBManager.getServer(conn_db) +
                       ":pack_ls_err where nzp_pack_ls=" + nzp_pack_ls
                        , true);
#endif
#if PG
                    ret = ExecSQL(conn_db,
                                   " Insert into " + Points.Pref + "_fin_" + (Points.DateOper.Year % 100).ToString("00") +
                                  ".pack_ls_err ( nzp_pack_ls,nzp_err,note ) " +
                                   " values (" + nzp_pack_ls + ",666,'Недопустимый платёжный код')"
                                   , true);
#else
                ret = ExecSQL(conn_db,
                " Insert into " + Points.Pref + "_fin_" + (Points.DateOper.Year % 100).ToString("00") + "@" + DBManager.getServer(conn_db) +
               ":pack_ls_err ( nzp_pack_ls,nzp_err,note ) " +
                " values ("+nzp_pack_ls+",666,'Недопустимый платёжный код')"
                , true);
#endif

            if (!ret.result)
            {
                DropTempTablesPack(conn_db);
                return false;
            }

                }

            }


            conn_db.Close();
            return b;
        }

        public void PackFonTasks(int nzp_pack, FonTaskTypeIds task, out Returns ret)
        {
            PackFonTasks(nzp_pack, 0, task, out ret);
        }

        /// <summary>
        /// распределение или откат пачки через фоновую задачу
        /// </summary>
        /// <param name="nzp_pack"></param>
        /// <param name="task"></param>
        /// <param name="ret"></param>
        public void PackFonTasks(int nzp_pack, int nzp_user, FonTaskTypeIds task, out Returns ret)
        {                        
            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result)
            {
                return;
            }
            PackFonTasks(nzp_pack, nzp_user, task, out ret, conn_db, null);
          
            conn_db.Close();
        }

        public void PackFonTasks(int nzp_pack, int nzp_user, FonTaskTypeIds task, out Returns ret, IDbConnection conn_db, IDbTransaction transaction)
        {
            if (task == FonTaskTypeIds.Unknown)
            {
                ret = new Returns(false, "Неверный код операции");
            }
            else if (task == FonTaskTypeIds.UpdatePackStatus)
            {
                CalcFon calcfon = new CalcFon(Points.GetCalcNum(0));
                calcfon.task = task;
                calcfon.status = FonTaskStatusId.New; //на выполнение 
                calcfon.nzpt = 0;
                calcfon.nzp = nzp_pack;
                calcfon.nzp_user = nzp_user;
                calcfon.txt = "'Обновление статуса пачки (код = " + nzp_pack + ", год = " + Points.DateOper.Year + ")'";

                ret = AddTask(calcfon);
            }
            else
            {
                ret = Utils.InitReturns();

                int yy = Points.CalcMonth.year_;
                string pack_ls = Points.Pref + "_fin_" + (yy - 2000).ToString("00") + tableDelimiter + "pack_ls ";
                string kvar = Points.Pref + "_data" + tableDelimiter + "kvar ";

                //для начала найдем пачку и определим все затронутые префиксы
                MyDataReader reader;

                //определим данные пачки
                string pack_text = "";
                string pack = Points.Pref + "_fin_" + (yy - 2000).ToString("00") + tableDelimiter + "pack ";
                ret = ExecRead(conn_db, transaction, out reader,
                    " Select num_pack, bank From " + pack + " p, " + Points.Pref + "_kernel" + tableDelimiter + "s_bank b " +
                    " Where p.nzp_bank = b.nzp_bank " +
                    "   and p.nzp_pack = " + nzp_pack
                    , true);

                if (!ret.result) return;

                try
                {
                    if (reader.Read())
                    {
                        if (reader["num_pack"] != DBNull.Value)
                            pack_text = " Пачка № " + (string)reader["num_pack"];
                        pack_text = pack_text.Trim();

                        if (reader["bank"] != DBNull.Value)
                            pack_text += " от " + (string)reader["bank"];
                        pack_text = pack_text.Trim();
                    }
                }
                catch (Exception ex)
                {
                    MonitorLog.WriteLog(ret.text, MonitorLog.typelog.Error, 1, 222, true);
                    ret.result = false;
                    ret.text = ex.Message;
                    return;
                }
                finally
                {
                    reader.Close();
                }

                //затем выставим задание на распределение или откат
                CalcFon clcfon = new CalcFon(Points.GetCalcNum(0));
                clcfon.task = task;
                clcfon.status = FonTaskStatusId.New; //на выполнение 
                clcfon.nzpt = 0;
                clcfon.nzp = nzp_pack;
                clcfon.txt = "'" + pack_text + "'";
                clcfon.nzp_user = nzp_user;

                ret = AddTask(clcfon);
            }
        }

        //проверить, что вся пачка распределена или откачена
        //-----------------------------------------------------------------------------
        public void PackFonTasks_21(CalcFon calcfon, out Returns ret)
        {
            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result)
            {
                return;
            }

            PackFonTasks_21(conn_web, calcfon, out ret);

            conn_web.Close();
            //conn_web.Dispose();
        }

        public void PackFonTasks_21(IDbConnection conn_web, CalcFon calcfon, out Returns ret)
        {
            ret = Utils.InitReturns();
            IDataReader reader;

            int flag = 21; //пачка распределена или распределена с ошибками = 22
            string dat_uchet = ",dat_uchet = '" + Points.DateOper.ToShortDateString() + "'";

            if (calcfon.task == FonTaskTypeIds.CancelPackDistribution)
            {
                flag = 23; //пачка не распределена
                dat_uchet = ",dat_uchet = NULL ";
            }

            //для этого обходим все calc_fonXX и проверяем задания по nzp_pack
            for (int i = 0; i < CalcThreads.maxCalcThreads; i++)
            {
                string tab = "calc_fon_" + i;
                if (!TempTableInWebCashe(conn_web, tab)) continue;

                ret = ExecRead(conn_web, out reader,
                    " Select kod_info From " + tab +
                    " Where nzp   = " + calcfon.nzp +
                      //" and nzpt  = " + calcfon.nzpt +
                      " and year_ = " + calcfon.yy +
                      " and month_= " + calcfon.mm +
                      " and task  = " + (int)calcfon.task
                    , true);
                if (!ret.result)
                {
                    conn_web.Close();
                    return;
                }

                try
                {
                    while (reader.Read())
                    {
                        if (reader["kod_info"] != DBNull.Value)
                        {
                            int k = (int)reader["kod_info"];
                            if (k == -1)
                            {
                                flag = 22; //пачка распределена с ошибками
                                break;
                            }
                            if (k == 0 || k == 3)
                            {
                                //пачка еще не обработана
                                return;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = ex.Message;
                    MonitorLog.WriteLog(ret.text, MonitorLog.typelog.Error, 1, 222, true);
                    return;
                }

            }

            #if PG
string pack    = Points.Pref + "_fin_" + (calcfon.yy - 2000).ToString("00") + ".pack ";
            string pack_ls = Points.Pref + "_fin_" + (calcfon.yy - 2000).ToString("00") + ".pack_ls ";
#else
string pack    = Points.Pref + "_fin_" + (calcfon.yy - 2000).ToString("00") + ":pack ";
            string pack_ls = Points.Pref + "_fin_" + (calcfon.yy - 2000).ToString("00") + ":pack_ls ";
#endif

            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result)
            {
                return;
            }

            ret = ExecSQL(conn_db, 
                " Update " + pack +
                " Set flag = " + flag + dat_uchet +
                " Where nzp_pack = " + calcfon.nzp
                , true);
            if (!ret.result)
            {
                conn_db.Close();
                return;
            }

            //пачка распределена с ошибками
            if (calcfon.task == FonTaskTypeIds.DistributePack)
            {
                ret = ExecSQL(conn_db, 
                    " Update " + pack +
                    " Set flag = 22 " +
                    " Where nzp_pack = " + calcfon.nzp +
                    "   and nzp_pack in ( Select nzp_pack From " + pack_ls + " Where inbasket = 1 ) "
                    , true);
                if (!ret.result)
                {
                    conn_db.Close();
                    return;
                }
            }

            //скорректировать sum_rasp, sum_nrasp
            ret = ExecSQL(conn_db,
#if PG
            " Update " + pack +
                " Set sum_rasp = (Select sum(case when dat_uchet is not null and inbasket=0 then g_sum_ls else 0 end) " +
                        " From " + pack_ls +
                        " Where nzp_pack = " + calcfon.nzp+"), sum_nrasp = ( " +
                        " Select sum(case when dat_uchet is not null and inbasket=0 then 0 else g_sum_ls end)  " +
                        " From " + pack_ls +
                        " Where nzp_pack = " + calcfon.nzp +
                        " ) " +
                " Where nzp_pack = " + calcfon.nzp
#else
        " Update " + pack +
                " Set (sum_rasp,sum_nrasp) = (( " +
                        " Select sum(case when dat_uchet is not null and inbasket=0 then g_sum_ls else 0 end), " +
                               " sum(case when dat_uchet is not null and inbasket=0 then 0 else g_sum_ls end)  " +
                        " From " + pack_ls +
                        " Where nzp_pack = " + calcfon.nzp +
                        " )) " +
                " Where nzp_pack = " + calcfon.nzp
#endif
                , true);
            if (!ret.result)
            {
                conn_db.Close();
                return;
            }

            //обработка суперпачки
            ret = ExecRead(conn_db, out reader,
                " Select par_pack From " + pack +
                " Where nzp_pack = " + calcfon.nzp
                , true);
            if (!ret.result)
            {
                conn_db.Close();
                return;
            }

            int par_pack = 0;
            if (reader.Read())
            {
                try
                {
                    if (reader["par_pack"] != DBNull.Value)
                        par_pack = (int)reader["par_pack"];
                }
                catch
                {
                }
            }

            if (par_pack > 0)
            {
                //обновить данные суперпачки
                ExecSQL(conn_db, " Drop table ttt_par_pack ", false);

                ret = ExecSQL(conn_db,
#if PG
                    " Select max(dat_uchet) as dat_uchet, max(flag) as flag, sum(sum_rasp) as sum_rasp, sum(sum_nrasp) as sum_nrasp " +
                    " Into temp ttt_par_pack " +
                    " From " + pack +
                    " Where par_pack = " + par_pack +
                    "   and nzp_pack <>  " + par_pack                   
#else
                    " Select max(dat_uchet) as dat_uchet, max(flag) as flag, sum(sum_rasp) as sum_rasp, sum(sum_nrasp) as sum_nrasp " +
                    " From " + pack +
                    " Where par_pack = " + par_pack +
                    "   and nzp_pack <>  " + par_pack +
                    " Into temp ttt_par_pack With no log "
#endif
                    , true);
                if (!ret.result)
                {
                    ExecSQL(conn_db, " Drop table ttt_par_pack ", false);
                    conn_db.Close();
                    return;
                }

                ret = ExecSQL(conn_db,
#if PG
 " Update " + pack +
                    " Set dat_uchet = (Select dat_uchet From ttt_par_pack),flag = (Select flag From ttt_par_pack), sum_rasp=(Select sum_rasp From ttt_par_pack),sum_nrasp = (Select sum_nrasp From ttt_par_pack) " +
                    " Where nzp_pack = " + par_pack +
                    "   and 1 = ( Select 1 From ttt_par_pack ) "
#else
" Update " + pack +
                    " Set (dat_uchet,flag,sum_rasp,sum_nrasp) = (( Select dat_uchet,flag,sum_rasp,sum_nrasp From ttt_par_pack )) " +
                    " Where nzp_pack = " + par_pack +
                    "   and 1 = ( Select 1 From ttt_par_pack ) "
#endif
                    , true);
                if (!ret.result)
                {
                    ExecSQL(conn_db, " Drop table ttt_par_pack ", false);
                    conn_db.Close();
                    return;
                }

                ExecSQL(conn_db, " Drop table ttt_par_pack ", false);
            }
            
            conn_db.Close();
        }

        //собрать локальные fn_pa_xx
        //-----------------------------------------------------------------------------
        void LoadLocalPaXX(IDbConnection conn_db, PackXX packXX, bool load, out Returns ret)
        //-----------------------------------------------------------------------------
        {
            //if (Constants.Trace) Utility.ClassLog.WriteLog("Старт LoadLocalPaXX");
            ret = Utils.InitReturns();
            Returns r = Utils.InitReturns();
            packXX.nzp_pack_ls = 0;
            packXX.is_local    = false;
            if (!load) return;
            string sqlText;

            /*
#if PG
                            sqlText = " Insert into " + packXX.fn_pa_xx + " (  nzp_dom,nzp_supp,nzp_serv,nzp_area, nzp_geu,nzp_bank,dat_oper,sum_prih ) " +
                                " From  " +Points.Pref+"_data.kvar k,"+ packXX.fn_supplier + " s, " + packXX.pack + " p, " + packXX.pack_ls + " pl, "+packXX.s_bank + " b " +
                                " Where k.num_ls = s.num_ls " +
                                "   and p.nzp_pack = pl.nzp_pack " +
                                "   and pl.nzp_pack_ls = s.nzp_pack_ls " +
                                "   and p.nzp_bank = b.nzp_bank " +
                                "   and k.nzp_dom in (select  d.nzp_dom from t_dom d) "+
                                "   and s.dat_uchet = " + packXX.paramcalc.dat_oper +
                                " Group by 1,2,3,4,5,6,7 ";

                ret = ExecSQL(conn_db,sqlText, true);
#else
            {
                sqlText = " Insert into " + packXX.fn_pa_xx + " (  nzp_dom,nzp_supp,nzp_serv,nzp_area, nzp_geu,nzp_bank,dat_oper,sum_prih ) " +
                                " Select  k.nzp_dom, s.nzp_supp, s.nzp_serv, k.nzp_area, k.nzp_geu, b.nzp_payer, s.dat_uchet, sum(sum_prih) " +
                                " From  " +Points.Pref+"_data:kvar k,"+ packXX.fn_supplier + " s, " + packXX.pack + " p, " + packXX.pack_ls + " pl, "+packXX.s_bank + " b " +
                                " Where k.num_ls = s.num_ls " +
                                "   and p.nzp_pack = pl.nzp_pack " +
                                "   and pl.nzp_pack_ls = s.nzp_pack_ls " +
                                "   and p.nzp_bank = b.nzp_bank " +
                                "   and k.nzp_dom in (select d.nzp_dom from t_dom d) "+
                                "   and s.dat_uchet = " + packXX.paramcalc.dat_oper +
                                " Group by 1,2,3,4,5,6,7 ";
                 
                ret = ExecSQL(conn_db,sqlText, true);
            }
#endif
            */

            sqlText = " Insert into " + packXX.fn_pa_xx + " (  nzp_dom,nzp_supp,nzp_serv,nzp_area, nzp_geu,nzp_bank,dat_oper,sum_prih ) " +
           " Select  k.nzp_dom, s.nzp_supp, s.nzp_serv, k.nzp_area, k.nzp_geu, b.nzp_payer, s.dat_uchet, sum(sum_prih) " +
                " From  t_dom d, " + packXX.paramcalc.pref + "_data" + tableDelimiter + "kvar k," + packXX.fn_supplier + " s, " + packXX.pack + " p, " + packXX.pack_ls + " pl, " + packXX.s_bank + " b " +
                " Where k.nzp_dom = d.nzp_dom and k.num_ls = s.num_ls " +
                "   and p.nzp_pack = pl.nzp_pack " +
                "   and pl.nzp_pack_ls = s.nzp_pack_ls " +
                "   and p.nzp_bank = b.nzp_bank " +
                "   and s.dat_uchet = " + packXX.paramcalc.dat_oper +sConvToDate+" "+
                " Group by 1,2,3,4,5,6,7 ";

            ret = ExecSQL(conn_db, sqlText, true);

            if (!ret.result)
            {
                DropTempTablesPack(conn_db);
                MessageInPackLog(conn_db, packXX, "Ошибка распределения 4.4", true, out r);
                return;
            }
        }
        //-----------------------------------------------------------------------------
        void CreatePaXX(IDbConnection conn_db2, PackXX packXX, out Returns ret)
        //-----------------------------------------------------------------------------
        {
            CreatePaXX(conn_db2, packXX, true, out ret);
        }
        //-----------------------------------------------------------------------------
        void CreatePaXX(IDbConnection conn_db2, PackXX packXX, bool delete, out Returns ret)
        //-----------------------------------------------------------------------------
        {
            ret = Utils.InitReturns();

            if (TempTableInWebCashe(conn_db2, packXX.fn_pa_xx))
            {
                if (delete)
                {
                    string p_where = " Where dat_oper " + packXX.where_dat_oper;

                    if (packXX.is_local && packXX.nzp_pack > 0)
                        p_where = " Where nzp_pack = " + packXX.nzp_pack;

                    ExecByStep(conn_db2, packXX.fn_pa_xx, "nzp_pk",
                            " Delete From " + packXX.fn_pa_xx +
                              p_where
                            , 100000, " ", out ret);

                    UpdateStatistics(true, packXX.paramcalc, packXX.fn_pa_tab, out ret);
                }

                return;
            }

            string conn_kernel = Points.GetConnByPref(packXX.paramcalc.pref);
            IDbConnection conn_db = GetConnection(conn_kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result)
            {
                return;
            }

            if (packXX.is_local)
            {
                #if PG
                ret = ExecSQL(conn_db, " set search_path to '" + packXX.paramcalc.pref + "_charge_" + (packXX.paramcalc.calc_yy - 2000).ToString("00") + "'", true);
                ret = ExecSQL(conn_db,
                    " Create table "+
#if PG
 ""
#else
"are."
#endif
                    + packXX.fn_pa_tab +
                    " (  nzp_pk serial not null, " +
                       " nzp_dom integer, " +
                       " nzp_supp integer, " +
                       " nzp_serv integer, " +
                       " nzp_area integer, " +
                       " nzp_geu integer, " +
                       " sum_prih numeric(14,2) default 0 not null," +
                       " dat_oper date, " +
                       " nzp_pack integer default 0, " +
                       " nzp_bank integer default 0 ) "
                          , true);
#else
ret = ExecSQL(conn_db, " Database " + packXX.paramcalc.pref + "_charge_" + (packXX.paramcalc.calc_yy - 2000).ToString("00"), true);
                ret = ExecSQL(conn_db,
                    " Create table are." + packXX.fn_pa_tab +
                    " (  nzp_pk serial not null, " +
                       " nzp_dom integer, " +
                       " nzp_supp integer, " +
                       " nzp_serv integer, " +
                       " nzp_area integer, " +
                       " nzp_geu integer, " +
                       " sum_prih decimal(14,2) default 0 not null," +
                       " dat_oper date, " +
                       " nzp_pack integer default 0, " +
                       " nzp_bank integer default 0 ) "
                          , true);
#endif
                if (!ret.result)
                {
                    conn_db.Close();
                    return;
                }
            }
            else
            {
                #if PG
                ret = ExecSQL(conn_db, " set search_path to '" + Points.Pref + "_fin_" + (packXX.paramcalc.calc_yy - 2000).ToString("00") + "'", true);
                ret = ExecSQL(conn_db,
                    " Create table "+
#if PG
 ""
#else
  "are."
#endif
                    + packXX.fn_pa_tab +
                    " (  nzp_pk serial not null, " +
                       " nzp_dom integer, " +
                       " nzp_supp integer, " +
                       " nzp_serv integer, " +
                       " nzp_area integer, " +
                       " nzp_geu integer, " +
                       " sum_prih numeric(14,2) default 0 not null," +
                       " sum_prih_r numeric(14,2) default 0 not null, " +
                       " sum_prih_g numeric(14,2) default 0 not null, " +
                       " dat_oper date, " +
                       " nzp_bl integer, " +
                       " nzp_supp_w integer default 0, " +
                       " nzp_area_w integer default 0, " +
                       " nzp_bank integer default 0 ) "
                          , true);
#else
ret = ExecSQL(conn_db, " Database " + Points.Pref + "_fin_" + (packXX.paramcalc.calc_yy - 2000).ToString("00"), true);
                ret = ExecSQL(conn_db,
                    " Create table are." + packXX.fn_pa_tab +
                    " (  nzp_pk serial not null, " +
                       " nzp_dom integer, " +
                       " nzp_supp integer, " +
                       " nzp_serv integer, " +
                       " nzp_area integer, " +
                       " nzp_geu integer, " +
                       " sum_prih decimal(14,2) default 0 not null," +
                       " sum_prih_r decimal(14,2) default 0 not null, " +
                       " sum_prih_g decimal(14,2) default 0 not null, " +
                       " dat_oper date, " +
                       " nzp_bl integer, " +
                       " nzp_supp_w integer default 0, " +
                       " nzp_area_w integer default 0, " +
                       " nzp_bank integer default 0 ) "
                          , true);
#endif
                if (!ret.result)
                {
                    conn_db.Close();
                    return;
                }
            }



            ret = ExecSQL(conn_db, " create unique index "+
#if PG
 ""
#else
"are."
#endif
                +"ix1_" + packXX.fn_pa_tab + " on " + packXX.fn_pa_tab + " (nzp_pk) ", true);
            if (!ret.result)
            {
                conn_db.Close();
                return;
            }
            ret = ExecSQL(conn_db, " create index " +
#if PG
 ""
#else
"are."
#endif
 + "ix2_" + packXX.fn_pa_tab + " on " + packXX.fn_pa_tab + " (nzp_supp, nzp_serv) ", true);
            if (!ret.result)
            {
                conn_db.Close();
                return;
            }
            ret = ExecSQL(conn_db, " create index " +
#if PG
 ""
#else
"are."
#endif
 + "ix3_" + packXX.fn_pa_tab + " on " + packXX.fn_pa_tab + " (nzp_serv) ", true);
            if (!ret.result)
            {
                conn_db.Close();
                return;
            }
            ret = ExecSQL(conn_db, " create index  " +
#if PG
 ""
#else
"are."
#endif
 + "ix4_" + packXX.fn_pa_tab + " on " + packXX.fn_pa_tab + " (dat_oper, nzp_area, nzp_geu, nzp_supp, nzp_serv) ", true);
            if (!ret.result)
            {
                conn_db.Close();
                return;
            }
            ret = ExecSQL(conn_db, " create index " +
#if PG
 ""
#else
"are."
#endif
 + "ix5_" + packXX.fn_pa_tab + " on " + packXX.fn_pa_tab + " (dat_oper, nzp_serv) ", true);
            if (!ret.result)
            {
                conn_db.Close();
                return;
            }
            if (packXX.is_local)
            {
                ret = ExecSQL(conn_db, " create index  " +
#if PG
 ""
#else
"are."
#endif
 + "ix6_" + packXX.fn_pa_tab + " on " + packXX.fn_pa_tab + " (nzp_pack) ", true);
                if (!ret.result)
                {
                    conn_db.Close();
                    return;
                }
            }
            else
            {
                ret = ExecSQL(conn_db, " create index "+
#if PG
 "" +
#else
"are."+
#endif
                    "ix6_" + packXX.fn_pa_tab + " on " + packXX.fn_pa_tab + " (dat_oper, nzp_supp, nzp_serv, nzp_bl) ", true);
                if (!ret.result)
                {
                    conn_db.Close();
                    return;
                }
            }
/*
           #if PG
 ExecSQL(conn_db, " analyze " + packXX.fn_pa_tab, true);
#else
 ExecSQL(conn_db, " update statistics for table " + packXX.fn_pa_tab, true);
#endif
            */

            #if PG
 ret = ExecSQL(conn_db, " set search_path to '" + Points.Pref + "_kernel'", true); //!!!!
#else
ret = ExecSQL(conn_db, " Database " + Points.Pref + "_kernel", true); //!!!!
#endif

            conn_db.Close();
        }
        //распределить fn_distrib_xx
        //-----------------------------------------------------------------------------
        public void DistribPaXX_1(DateTime dat_s, DateTime dat_po, out Returns ret)
        //-----------------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            
            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);

            ret = OpenDb(conn_db, true);
            if (!ret.result)
            {
                return;
            }

            //DistribPaXX_2(conn_db,out  ret);
            //if (ret.result)
            //{
            //    return;
            //}


            List<int> points = new List<int>();  //в какиех nzp_wp надо пересчитать сальдо

            DateTime dat = dat_s;
            DateTime lastDayDatCalc = Convert.ToDateTime(DateTime.DaysInMonth(Points.DateOper.Year, Points.DateOper.Month).ToString("00") +
                 "." + Points.DateOper.Month.ToString() + "." + Points.DateOper.Year.ToString());

            if (dat_po < lastDayDatCalc)
            {
                dat_po = lastDayDatCalc;
            }
            while (dat <= dat_po)
            {
                int yy = dat.Year;
                int mm = dat.Month;
                CalcTypes.ParamCalc paramcalc = new CalcTypes.ParamCalc(0, 0, "", yy, mm, yy, mm);
                paramcalc.DateOper = dat;

                #if PG
                ret = ExecSQL(conn_db, " set search_path to '" + Points.Pref + "_fin_" + (paramcalc.calc_yy - 2000).ToString("00") + "'", true);
#else
ret = ExecSQL(conn_db, " Database " + Points.Pref + "_fin_" + (paramcalc.calc_yy - 2000).ToString("00"), true);
#endif
                if (!ret.result)
                {
                    conn_db.Close();
                    return;
                }
                /*
                //вызвать поиск ошибок за опердень
                PackError(conn_db, false, dat, ref points, out ret);
                if (!ret.result)
                {
                    conn_db.Close();
                    return;
                }
                */

                //расчет сальдо
                DistribPaXX(conn_db, paramcalc, out ret);
                if (!ret.result)
                {
                    conn_db.Close();
                    return;
                }

                dat = dat.AddDays(1);
            }

            #if PG
ExecSQL(conn_db, " set search_path to '" + Points.Pref + "_kernel'", true);
#else
ExecSQL(conn_db, " Database " + Points.Pref + "_kernel", true);
#endif
            conn_db.Close();


            if (points.Count > 0)
            {
                //пересчитать сальдо
                foreach (_Point zap in Points.PointList)
                {
                    if (points.Contains(zap.nzp_wp))
                    {
                        //задание на фоновый расчет сальдо!
                        CalcSaldo(zap.nzp_wp, out ret);
                        if (!ret.result)
                        {
                            //conn_db.Close();
                            //return;
                        }
                    }
                }

            }
        }

        //распределить fn_distrib_xx для записей в fn_distrib_dom_log
        //-----------------------------------------------------------------------------
        public void DistribPaXX_2(IDbConnection conn_db, out Returns ret)
        //-----------------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
           
            int yy = Points.DateOper.Year;
            int mm = Points.DateOper.Month;
            CalcTypes.ParamCalc paramcalc = new CalcTypes.ParamCalc(-1, -1, "", yy, mm, yy, mm);
            /* 
           IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
           ret = OpenDb(conn_db, true);
           if (!ret.result)
           {
               return;
           }
           */
            ExecSQL(conn_db, " Drop table t_selkvar ", false);

#if PG
            string fn_operday_dom_mc = Points.Pref + "_fin_" + (paramcalc.calc_yy - 2000).ToString("00") + ".fn_operday_dom_mc";
#else
            string fn_operday_dom_mc = Points.Pref + "_fin_" + (paramcalc.calc_yy - 2000).ToString("00") + paramcalc.ol_srv + ":fn_operday_dom_mc";
#endif

            string sql_text = "select MIN(date_oper) as dat from " + fn_operday_dom_mc;
            DataTable  dt = ClassDBUtils.OpenSQL(sql_text, conn_db).GetData();

            PackXX fn_packXX = new PackXX(paramcalc, 0, false);

            fn_packXX.is_local = false;
            fn_packXX.paramcalc.pref = Points.Pref;
           //fn_packXX.paramcalc.pref = Points.Pref;
           // fn_packXX.paramcalc.nzp_pack = -1;
            
            DateTime dat = Points.DateOper;

            DataRow row = dt.Rows[0];

            string min_date = "";
            if (row["dat"] != DBNull.Value)
            {
                min_date = Convert.ToDateTime(row["dat"]).ToShortDateString();
#if PG
                string fn_distrib = Points.Pref + "_fin_" + (paramcalc.calc_yy - 2000).ToString("00") + ".fn_distrib_dom_"+(Convert.ToDateTime(row["dat"]).Month).ToString("00");
#else
                string fn_distrib = Points.Pref + "_fin_" + (paramcalc.calc_yy - 2000).ToString("00") + paramcalc.ol_srv + ":fn_distrib_dom_" + (Convert.ToDateTime(row["dat"]).Month).ToString("00");
                    //ToString("00");
#endif

                sqlText = " Delete From " + fn_packXX.fn_distrib + " Where dat_oper >= '" + min_date + "'" + sConvToDate +
 " and nzp_dom in (select nzp_dom from " + fn_operday_dom_mc + ") ";
                ret = ExecSQL(conn_db, sqlText, true);

            } else
            {
                ret.result=false;
                ret.tag = -1;
                ret.text = "Перерасчёт не требуется";
                return;
            }
            dat = Convert.ToDateTime(min_date);
            DateTime lastDayDatCalc = Convert.ToDateTime(DateTime.DaysInMonth(Points.DateOper.Year, Points.DateOper.Month).ToString("00") +
                 "." + Points.DateOper.Month.ToString() + "." + Points.DateOper.Year.ToString());

            //DropTempTablesPack(conn_db);
            
            
            //CreateSelKvar(conn_db, fn_packXX, out ret);

            //DataTable drr1 = ViewTbl(conn_db, " select * from t_selkvar  ");

#if PG
                sqlText = " Create temp table t_selkvar " +
                          " ( nzp_key     serial not null, " +
                          "   nzp_kvar    integer, " +
                          "   num_ls      integer, " +
                          "   nzp_pack_ls integer, " +
                          "   kod_sum     integer, " +
                          "   nzp_wp      integer, " +
                          "   nzp_dom     integer, " +
                          "   nzp_pack    integer, " +
                          "   nzp_area    integer, " +
                          "   nzp_geu     integer, " +
                          "   dat_uchet   date, " +
                          "   dat_month   date, " +
                          "   distr_month date, " +
                          "   dat_vvod    date, " +
                          "   id_bill     integer," +
                          "   is_open     integer default 1," +
                          "   sum_etalon  numeric(14,2) default 0," +
                          "   alg         integer default 0," +
                          "   g_sum_ls    numeric(14,2) default 0 " +
                          " )  ";
#else
            sqlText = " Create temp table t_selkvar " +
            " ( nzp_key     serial not null, " +
            "   nzp_kvar    integer, " +
            "   num_ls      integer, " +
            "   nzp_dom     integer " +
            " ) With no log ";
#endif
            if (isDebug)
            {

#if PG
                     sqlText = sqlText.Replace("temp table", "table");

#else
                sqlText = sqlText.Replace("temp table", "table");
                sqlText = sqlText.Replace("With no log", "");
#endif
            }

            ret = ExecSQL(conn_db, sqlText, true);
            if (!ret.result)
            {
                DropTempTablesPack(conn_db);
                return ;
            }




            sql_text = "Insert into t_selkvar (nzp_dom) select distinct nzp_dom from  " + fn_operday_dom_mc;
            ret = ExecSQL(conn_db, sql_text, true);

            sql_text = "create " + sCrtTempTable + " table t_dom (nzp_dom integer) " + sUnlogTempTable;
            ret = ExecSQL(conn_db, sql_text, false);

            sql_text = "insert into t_dom (nzp_dom) select distinct nzp_dom from t_selkvar";
            ret = ExecSQL(conn_db, sql_text, false);


            while (dat <= lastDayDatCalc)
            {
                paramcalc.DateOper = dat;
                
                DistribPaXX(conn_db, paramcalc, out ret);
                if (!ret.result)
                {
                     DropTempTablesPack(conn_db);
                     return ;
                 }
                 dat = dat.AddDays(1);
            }
            sql_text = "drop  table t_dom";
            ret = ExecSQL(conn_db, sql_text, false);

            sql_text = "drop  table t_selkvar";
            ret = ExecSQL(conn_db, sql_text, false);

            sql_text = "delete from  t_selkvar";
            ret = ExecSQL(conn_db, sql_text, false);


        }


        //-----------------------------------------------------------------------------
        void DistribPaXX(IDbConnection conn_db, CalcTypes.ParamCalc paramcalc, out Returns ret)
        //-----------------------------------------------------------------------------
        {
         
            //isDebug = true;
            //sCrtTempTable = "";
            //sUnlogTempTable = "";

            string sql_text;
            ret = Utils.InitReturns();
            Returns r = Utils.InitReturns();

            //return;   
            PackXX fn_packXX = new PackXX(paramcalc, 0, false);


            fn_packXX.is_local = false;
            fn_packXX.paramcalc.pref = Points.Pref;

            /*
            sql_text="drop table t_dom ";
            ret = ExecSQL(conn_db, sql_text, false);

            sql_text="create "+sCrtTempTable+" table t_dom (nzp_dom integer) "+sUnlogTempTable;
            ret = ExecSQL(conn_db, sql_text, false);

            sql_text="insert into t_dom (nzp_dom) select distinct nzp_dom from t_selkvar";
            ret = ExecSQL(conn_db, sql_text, false);          
            */
            sql_text = "select * from t_dom";
            ret = ExecSQL(conn_db, sql_text, false);          
            if (!ret.result)
            {
                DropTempTablesPack(conn_db);
                CreateSelKvar(conn_db, fn_packXX, out ret);
                sql_text = "create " + sCrtTempTable + " table t_dom (nzp_dom integer) " + sUnlogTempTable;
                ret = ExecSQL(conn_db, sql_text, false);
                sql_text = "insert into t_dom (nzp_dom) select distinct nzp_dom from t_selkvar";
                ret = ExecSQL(conn_db, sql_text, false);
                sql_text = "create index idx_dom_t_dom on t_dom (nzp_dom)";
                ret = ExecSQL(conn_db, sql_text, false);
            }

            


            CreatePaXX(conn_db, fn_packXX, false, out ret);
            if (!ret.result)
            {
                return;
            }
                       

            MessageInPackLog(conn_db, fn_packXX, "Начало расчета сальдо по перечислениям", false, out r);

            string sqlText;

            sqlText = " Delete From " + fn_packXX.fn_pa_xx + " Where dat_oper = " + fn_packXX.paramcalc.dat_oper + sConvToDate;

            if  ( (paramcalc.nzp_pack != 0)||(paramcalc.nzp_dom != 0))
            {
                sqlText = sqlText + " and nzp_dom in (select nzp_dom from t_dom)";
            }

            ret = ExecSQL(conn_db, 
                sqlText , true);
            if (!ret.result)
            {
                return;
            }

            sqlText = " Delete From " + fn_packXX.fn_distrib + " Where dat_oper = " + fn_packXX.paramcalc.dat_oper + sConvToDate;
            if ((paramcalc.nzp_pack != 0) || (paramcalc.nzp_dom != 0))
            {
                sqlText = sqlText + " and nzp_dom in (select nzp_dom from t_dom)";
            }
            ret = ExecSQL(conn_db, sqlText, true);
            if (!ret.result)
            {
                return;
            }

            sqlText = " Delete From " + fn_packXX.fn_perc + " Where dat_oper = " + fn_packXX.paramcalc.dat_oper + sConvToDate;
            if ((paramcalc.nzp_pack != 0) || (paramcalc.nzp_dom != 0))
            {
                sqlText = sqlText + " and nzp_dom in (select  nzp_dom from t_dom)";
            }
            ret = ExecSQL(conn_db, sqlText, true);
            if (!ret.result)
            {
                return;
            }

            sqlText = " Delete From " + fn_packXX.fn_naud + " Where dat_oper = " + fn_packXX.paramcalc.dat_oper + sConvToDate;

            if ((paramcalc.nzp_pack != 0) || (paramcalc.nzp_dom != 0))
            {
                sqlText = sqlText + " and nzp_dom in (select  nzp_dom from t_dom)";
            }

            ret = ExecSQL(conn_db,sqlText, true);
            if (!ret.result)
            {
                return;
            }
            //int count;
            
            //count=GetCount(conn_db," select count(*) as f1 from t_dom ");

            //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
            // Заполнить центальный fn_pa_dom_XX
            //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
            sqlText = " select distinct a.pref from " + Points.Pref + "_data" + tableDelimiter + "kvar a where a.nzp_dom in (select  b.nzp_dom from  t_dom b )";

            DataTable dt = ClassDBUtils.OpenSQL(sqlText, conn_db).GetData();
            foreach (DataRow rowPref in dt.Rows)
            {
                if (rowPref["pref"] != DBNull.Value)
                {
                    paramcalc.pref = Convert.ToString(rowPref["pref"]).Trim();

                    PackXX local_packXX = new PackXX(paramcalc, 0, false);
                    LoadLocalPaXX(conn_db, local_packXX, true, out ret);
                    if (!ret.result)
                    {
                        return;
                    }
                }
            }

            // Сохранить запись в журнал событий
            int nzp_event = SaveEvent(7427, conn_db, paramcalc.nzp_user, paramcalc.nzp_pack, "Операционный день " + fn_packXX.paramcalc.dat_oper);
            if ( (nzp_event > 0) && (isDebug) )
            {
                ret = ExecSQL(conn_db, "insert into " + Points.Pref + "_data" + tableDelimiter + "sys_event_detail(nzp_event, table_, nzp) select " + nzp_event + ",'" + fn_packXX.pack_ls + "',nzp_pack_ls from t_selkvar", true);                 
            }

            if (!isAutoDistribPaXX)
            {
                ret = ExecSQL(conn_db, " delete from  " + fn_packXX.fn_operday_log + " where date_oper =" + paramcalc.dat_oper +sConvToDate+ " and nzp_dom in (select  nzp_dom from t_dom) ", true);                 
            }
                

            //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
            //расчет вход. сальдо
            //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
            ExecSQL(conn_db, " Drop table ttt_dis_prev ", false);

            DateTime d = fn_packXX.paramcalc.DateOper.AddDays(-1);

            /*
           #if PG
 ret = ExecSQL(conn_db,
                " Select a.nzp_payer, a.nzp_area, a.nzp_dom,a.nzp_serv, a.nzp_bank, sum(a.sum_out) as sum_in " +
                " Into temp ttt_dis_prev "+
                " From " + fn_packXX.fn_distrib_prev +" a "+
                " Where a.dat_oper = '" + d.ToShortDateString() + "' and a.nzp_dom in (select  nzp_dom from t_dom) " +
                " Group by a.nzp_payer, a.nzp_area, a.nzp_dom,a.nzp_serv, a.nzp_bank "
                , true);
#else
            
            sqlText =
                " Select a.nzp_payer, a.nzp_area, a.nzp_dom,a.nzp_serv, a.nzp_bank, sum(a.sum_out) as sum_in " +
                " From " + fn_packXX.fn_distrib_prev + " a " +
                " Where a.dat_oper = '" + d.ToShortDateString() + "' and a.nzp_dom in (select  nzp_dom from t_dom) " +
                " Group by a.nzp_payer, a.nzp_area, a.nzp_dom,a.nzp_serv, a.nzp_bank " +
                " Into temp ttt_dis_prev With no log ";
            

            ret = ExecSQL(conn_db,sqlText , true);
#endif
             */
            ret = ExecSQL(conn_db, " Create " + sCrtTempTable + " table ttt_dis_prev (nzp_payer integer, nzp_area integer, nzp_dom integer, nzp_serv integer, nzp_bank integer,  sum_in " + sDecimalType + "(14,2) default 0.00) " + sUnlogTempTable, true);

            sqlText = " insert Into  ttt_dis_prev (nzp_payer, nzp_area, nzp_dom,nzp_serv, nzp_bank,  sum_in) " +
                " Select a.nzp_payer, a.nzp_area, a.nzp_dom,a.nzp_serv, a.nzp_bank, sum(a.sum_out) as sum_in " +
                " From " + fn_packXX.fn_distrib_prev + " a,  t_dom d " +
                " Where a.dat_oper = '" + d.ToShortDateString() + "' "+sConvToDate+" and a.nzp_dom = d.nzp_dom " +
                " Group by a.nzp_payer, a.nzp_area, a.nzp_dom,a.nzp_serv, a.nzp_bank " +
                " ";


            ret = ExecSQL(conn_db,sqlText , true);
            if (!ret.result)
            {
                return;
            }

            ret = ExecSQL(conn_db,
                " Insert into " + fn_packXX.fn_distrib +
                "  ( nzp_payer, nzp_area, nzp_dom,nzp_serv, nzp_bank, dat_oper, sum_in ) " +
                " Select nzp_payer, nzp_area, nzp_dom,nzp_serv, nzp_bank, " + fn_packXX.paramcalc.dat_oper +sConvToDate+ ", sum_in " +
                " From ttt_dis_prev "
                , true);
            ret = ExecSQL(conn_db, " Drop table ttt_dis_prev ", false);
            if (!ret.result)
            {
                return;
            }
            //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
            //учет fn_pa_xx
            //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
            ExecSQL(conn_db, " Drop table ttt_paxx ", false);

            /*
            if (isDebug)
            {
                ret = ExecSQL(conn_db,
                                " create table  ttt_paxx (nzp_payer integer, nzp_area integer, nzp_dom integer, nzp_serv integer, nzp_bank integer, nzp_supp integer, kod integer, sum_prih decimal(14,2)) "
                                , true);
            }
#if PG
 ret = ExecSQL(conn_db,
                " Select b.nzp_payer, a.nzp_area, a.nzp_dom, a.nzp_serv, a.nzp_bank, a.nzp_supp, 1 as kod, sum(sum_prih) as sum_prih " +
                 " Into temp ttt_paxx "+
                " From " + fn_packXX.fn_pa_xx + " a, " + fn_packXX.s_payer + " b " +
                " Where a.nzp_supp = b.nzp_supp and a.nzp_dom and a.nzp_dom in (select  nzp_dom from t_dom)" +
                "   and dat_oper = " + fn_packXX.paramcalc.dat_oper +
                " Group by 1,2,3,4,5,6 " , true);
#else
            sqlText = " Select b.nzp_payer, a.nzp_area, a.nzp_dom, a.nzp_serv, a.nzp_bank, a.nzp_supp, 1 as kod, sum(sum_prih) as sum_prih " +
                                " From " + fn_packXX.fn_pa_xx + " a, " + fn_packXX.s_payer + " b " +
                                " Where a.nzp_supp = b.nzp_supp and a.nzp_dom in (select  nzp_dom from t_dom) " +
                                "   and dat_oper = " + fn_packXX.paramcalc.dat_oper +
                                " Group by 1,2,3,4,5,6 ";
            if (isDebug)
            {
                sqlText="insert into ttt_paxx"+sqlText;
            } else
            {
                sqlText = sqlText + " Into temp ttt_paxx With no log ";
            }
      


 
#endif
             */

            ret=ExecSQL(conn_db, " Create " + sCrtTempTable + " table ttt_paxx (nzp_payer integer, nzp_area integer, nzp_dom integer, nzp_serv integer, nzp_bank integer, kod integer, nzp_supp integer, sum_prih " + sDecimalType+"(14,2) default 0.00) " + sUnlogTempTable, true);
            sqlText=   "insert into ttt_paxx ( nzp_payer, nzp_area, nzp_dom, nzp_serv, nzp_bank, nzp_supp, kod,  sum_prih) "+
                " Select b.nzp_payer, a.nzp_area, a.nzp_dom, a.nzp_serv, a.nzp_bank, a.nzp_supp, 1 as kod, sum(sum_prih) as sum_prih " +                 
                " From " + fn_packXX.fn_pa_xx + " a, " + fn_packXX.s_payer + " b, t_dom d " +
                " Where a.nzp_supp = b.nzp_supp and a.nzp_dom = d.nzp_dom " +
                "   and dat_oper = " + fn_packXX.paramcalc.dat_oper +sConvToDate+
                " Group by 1,2,3,4,5,6 ";
            ret= ExecSQL(conn_db, sqlText, true);

            if (!ret.result)
            {
                return;
            }            

            ExecSQL(conn_db, " Create unique index ix1_ttt_paxx on ttt_paxx (nzp_payer, nzp_area, nzp_dom, nzp_serv, nzp_bank, nzp_supp) ", true);            

            ret = ExecSQL(conn_db,
                " Update ttt_paxx " +
                " Set kod = 0 " +
                " Where 0 < ( Select count(*) From " + fn_packXX.fn_distrib + " a " +
                            " Where a.nzp_payer= ttt_paxx.nzp_payer " +
                            "   and a.nzp_area = ttt_paxx.nzp_area " +
                            "   and a.nzp_dom = ttt_paxx.nzp_dom " +
                            "   and a.nzp_serv = ttt_paxx.nzp_serv " +
                            "   and a.nzp_bank = ttt_paxx.nzp_bank " +
                            "   and a.dat_oper = " + fn_packXX.paramcalc.dat_oper +sConvToDate+
                            " ) "
                , true);
            if (!ret.result)
            {
                return;
            }

            ExecSQL(conn_db, " Create index ix2_ttt_paxx on ttt_paxx (kod) ", true);
            #if PG
ExecSQL(conn_db, " analyze ttt_paxx ", true);
#else
ExecSQL(conn_db, " Update statistics for table ttt_paxx ", true);
#endif

            ret = ExecSQL(conn_db,
                " Insert into " + fn_packXX.fn_distrib + " ( nzp_payer,nzp_area, nzp_dom,nzp_serv,nzp_bank,dat_oper ) " +
                " Select nzp_payer,nzp_area, nzp_dom, nzp_serv,nzp_bank, " + fn_packXX.paramcalc.dat_oper + sConvToDate+
                " From ttt_paxx Where kod = 1 "
                , true);
            if (!ret.result)
            {
                return;
            }

            ret = ExecSQL(conn_db,
                " Update " + fn_packXX.fn_distrib +
                " Set sum_rasp = ( " +
                            " Select sum(sum_prih) From ttt_paxx a " +
                            " Where a.nzp_payer= " + fn_packXX.fn_distrib + ".nzp_payer " +
                            "   and a.nzp_area = " + fn_packXX.fn_distrib + ".nzp_area " +
                            "   and a.nzp_dom = " + fn_packXX.fn_distrib + ".nzp_dom " +
                            "   and a.nzp_serv = " + fn_packXX.fn_distrib + ".nzp_serv " +
                            "   and a.nzp_bank = " + fn_packXX.fn_distrib + ".nzp_bank " +
                            " ) " +
                " Where dat_oper = " + fn_packXX.paramcalc.dat_oper + sConvToDate+ " and nzp_dom in (select  nzp_dom from t_dom) "+
                "   and 0 < ( Select count(*) From ttt_paxx a " +
                            " Where a.nzp_payer= " + fn_packXX.fn_distrib + ".nzp_payer " +
                            "   and a.nzp_area = " + fn_packXX.fn_distrib + ".nzp_area " +
                            "   and a.nzp_dom = " + fn_packXX.fn_distrib + ".nzp_dom " +
                            "   and a.nzp_serv = " + fn_packXX.fn_distrib + ".nzp_serv " +
                            "   and a.nzp_bank = " + fn_packXX.fn_distrib + ".nzp_bank " +
                            " ) "
                , true);
            if (!ret.result)
            {
                return;
            }

            //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
            //проверить, что все распределенные оплаты учтены!
            //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
            IDataReader reader;
            /*
                        ret = ExecRead(conn_db, out reader,


            #if PG
                            " Select 1 as kod, sum(a.g_sum_ls) as sum1 From " + fn_packXX.pack_ls +" a, "+fn_packXX.pack+" b "+
                            " Where a.nzp_pack = b.nzp_pack and b.pack_type <> 20 and a.dat_uchet = " + fn_packXX.paramcalc.dat_oper + "   and a.inbasket = 0 and coalesce(a.alg::int,0)>0 " +
                            " Group by 1 " +
                            " Union all " +
                            " Select 2 as kod, sum(sum_rasp) as sum1 From " + fn_packXX.fn_distrib +
                            " Where dat_oper = " + fn_packXX.paramcalc.dat_oper +
                            " Group by 1 "
            #else
                
             " Select 1 as kod, sum(a.g_sum_ls) as sum1 From " + fn_packXX.pack_ls +" a, "+fn_packXX.pack+" b "+
                            " Where a.nzp_pack = b.nzp_pack  and b.pack_type <> 20 and a.dat_uchet = " + fn_packXX.paramcalc.dat_oper + "   and a.inbasket = 0 and nvl(a.alg,0)>0 " +
                            " Group by 1 " +
                            " Union all " +
                            " Select 2 as kod, sum(sum_rasp) as sum1 From " + fn_packXX.fn_distrib +
                            " Where dat_oper = " + fn_packXX.paramcalc.dat_oper +
                            " Group by 1 "
                 
            #endif
 
                            , true);
                        if (!ret.result)
                        {
                            return;
                        }
                        try
                        {
                            decimal f1 = 0;
                            decimal f2 = 0;
                            while (reader.Read())
                            {
                                if ((int)reader["kod"] == 1)
                                    f1 = (decimal)reader["sum1"];
                                else
                                    f2 = (decimal)reader["sum1"];
                            }
                            reader.Close();

                            float dlt = Decimal.ToSingle(Math.Abs(f1 - f2));
                            if (dlt > 0.001)
                            {
                                ret.text = fn_packXX.paramcalc.dat_oper + ": Ошибка pack_ls<>fn_distrib: f1=" + f1.ToString("00000000.00") + " f2=" + f2.ToString("00000000.00");
                                MonitorLog.WriteLog(ret.text, MonitorLog.typelog.Error, 1, 222, true);
                                ret.result = false;
                                ret.text = "Несоответствие суммы оплат по квитанциям (" + f1.ToString("0.00").Trim() + " руб) сумме распределения олат по домам (" + f2.ToString("0.00").Trim() + " руб) за " + fn_packXX.paramcalc.dat_oper + " на " + dlt.ToString("0.00").Trim() + " руб";
                                ret.tag = -1;
                                return;
                            }
                        }
                        catch (Exception ex)
                        {
                            reader.Close();
                            ret.text = ex.Message;
                            MonitorLog.WriteLog(ret.text, MonitorLog.typelog.Error, 1, 222, true);
                        }

            */

            //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
            //учет процентов удержаний
            //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

            //выбрать nzp_payer_naud,perc_ud из fn_percent, который удерживает проценты из суммы в ttt_paxx: [nzp_payer, nzp_area, nzp_serv, nzp_bank, nzp_supp]
            ExecSQL(conn_db, " Drop table ttt_perc_ud ", false);

           #if PG
 ret = ExecSQL(conn_db,
                " Create temp table ttt_perc_ud " +
                " ( nzp_key   serial  not null, " +
                "   nzp_payer integer default 0 not null, " +
                "   nzp_area  integer default 0 not null, " +
                "   nzp_dom   integer default 0 not null, " +
                "   nzp_serv  integer default 0 not null, " +
                "   nzp_bank  integer default 0 not null, " +
                "   nzp_supp  integer default 0 not null, " +
                "   sum_prih        numeric(14,2) default 0.00 not null, " +
                "   nzp_payer_naud  integer default 0 not null, " +
                "   sum_naud        numeric(14,2) default 0.00 not null, " +
                "   kod       integer default 1 not null, " +
                "   perc_ud   numeric(5,2) default 0.00 not null " +
                " )  ", true);
#else
            ret = ExecSQL(conn_db,
                " Create temp table ttt_perc_ud " +
                " ( nzp_key   serial  not null, " +
                "   nzp_payer integer default 0 not null, " +
                "   nzp_area  integer default 0 not null, " +
                "   nzp_dom  integer default 0 not null, " +
                "   nzp_serv  integer default 0 not null, " +
                "   nzp_bank  integer default 0 not null, " +
                "   nzp_supp  integer default 0 not null, " +
                "   sum_prih        decimal(14,2) default 0.00 not null, " +
                "   nzp_payer_naud  integer default 0 not null, " +
                "   sum_naud        decimal(14,2) default 0.00 not null, " +
                "   kod       integer default 1 not null, " +
                "   perc_ud   decimal(5,2) default 0.00 not null " +
                " ) With no log ", true);
#endif
            if (!ret.result)
            {
                return;
            }

            ret = ExecSQL(conn_db,
                " Insert into ttt_perc_ud ( nzp_payer, nzp_area, nzp_dom,nzp_serv, nzp_bank, nzp_supp, sum_prih, nzp_payer_naud, sum_naud, kod, perc_ud ) " +
                " Select a.nzp_payer, a.nzp_area, a.nzp_dom, a.nzp_serv, a.nzp_bank, a.nzp_supp, a.sum_prih, " +
                       " b.nzp_payer as nzp_payer_naud, a.sum_prih as sum_naud, 1 as kod, max(b.perc_ud) as perc_ud " +
#if PG
                    " From ttt_paxx a, " + Points.Pref + "_data.fn_percent b " +
#else
                    " From ttt_paxx a, " + Points.Pref + "_data:fn_percent b " +
#endif
                " Where b.nzp_supp in (-1,a.nzp_supp) " +                
                "   and b.nzp_serv in (-1,a.nzp_serv) " +
                "   and b.nzp_area in (-1,a.nzp_area) " +
                "   and b.nzp_bank in (-1,a.nzp_bank) " +
                "   and b.dat_s  <= " + fn_packXX.paramcalc.dat_oper +sConvToDate+
                "   and b.dat_po >= " + fn_packXX.paramcalc.dat_oper +sConvToDate+
                "   and perc_ud > 0.001 " +
                " Group by 1,2,3,4,5,6,7,8,9,10 "
                , true);
            if (!ret.result)
            {
                return;
            }

            ExecSQL(conn_db, " Create index ix0_ttt_pud on ttt_perc_ud (nzp_key) ", true);
           #if PG
 ExecSQL(conn_db, " analyze ttt_perc_ud ", true);
#else
 ExecSQL(conn_db, " Update statistics for table ttt_perc_ud ", true);
#endif

            //расчет суммы удержания
            ret = ExecSQL(conn_db,
                " Update ttt_perc_ud " +
                " Set sum_naud = sum_prih * perc_ud / 100 "
                , true);
            if (!ret.result)
            {
                return;
            }



            //надо выровнить копейки при 100% удержании
            ExecSQL(conn_db, " Drop table ttt_perc_ud_2 ", false);

          #if PG
  ret = ExecSQL(conn_db,
                " Select nzp_payer, nzp_area, nzp_serv, nzp_bank, max(sum_prih) as sum_prih, sum(sum_naud) as sum_naud, sum(perc_ud) as perc_ud " +
                  " Into temp ttt_perc_ud_2 "+
                " From ttt_perc_ud " +
                " Where abs(sum_naud) > 0.001 " +
                " Group by 1,2,3,4 " 
              
                , true);
#else
  ret = ExecSQL(conn_db,
                " Select nzp_payer, nzp_area, nzp_serv, nzp_bank, max(sum_prih) as sum_prih, sum(sum_naud) as sum_naud, sum(perc_ud) as perc_ud " +
                " From ttt_perc_ud " +
                " Where abs(sum_naud) > 0.001 " +
                " Group by 1,2,3,4 " +
                " Into temp ttt_perc_ud_2 With no log "
                , true);
#endif
            if (!ret.result)
            {
                return;
            }

            ret = ExecRead(conn_db, out reader,
                " Select *, sum_prih - sum_naud as sum_dlt " +
                " From ttt_perc_ud_2 " +
                " Where abs(100 - perc_ud) < 0.001 and abs(sum_prih - sum_naud) > 0.0001 "
                , true);
            if (!ret.result)
            {
                return;
            }
            try
            {
                while (reader.Read())
                {
                    int nzp_payer= (int)reader["nzp_payer"];
                    int nzp_area = (int)reader["nzp_area"];
                    int nzp_serv = (int)reader["nzp_serv"];
                    int nzp_bank = (int)reader["nzp_bank"];
                    decimal dlt  = (decimal)reader["sum_dlt"];

                    IDataReader reader2;
                    ret = ExecRead(conn_db, out reader2,
                        " Select nzp_key From ttt_perc_ud " +
                        " Where nzp_payer = " + nzp_payer +
                        "   and nzp_area = " + nzp_area +
                        "   and nzp_serv = " + nzp_serv +
                        "   and nzp_bank = " + nzp_bank +
                        " Order by sum_naud desc "
                        , true);
                    if (!ret.result)
                    {
                        return;
                    }

                    //сторнировать дельту
                    if (reader2.Read())
                    {
                        int nzp_key = (int)reader2["nzp_key"];

                        ret = ExecSQL(conn_db,
                            " Update ttt_perc_ud " +
                            " Set sum_naud = sum_naud + " + dlt.ToString() +
                            " Where nzp_key = " + nzp_key
                            , true);
                        if (!ret.result)
                        {
                            return;
                        }
                    }
                    reader2.Close();
                }
                reader.Close();

            }
            catch (Exception ex)
            {
                reader.Close();
                ret.text = ex.Message;
                MonitorLog.WriteLog(ret.text, MonitorLog.typelog.Error, 1, 222, true);
            }

            ExecSQL(conn_db, " Drop table ttt_perc_ud_2 ", false);



            //сохранить суммы в fn_perc и fn_naud
            ret = ExecSQL(conn_db,
                " Insert into " + fn_packXX.fn_perc + " (nzp_supp, nzp_payer, nzp_dom, nzp_serv, nzp_area, nzp_geu, sum_prih, sum_perc, perc_ud, dat_oper, nzp_bank) " +
                " Select nzp_supp, nzp_payer_naud, nzp_dom,nzp_serv, nzp_area, -1 nzp_geu, sum_prih, sum_naud, perc_ud, " + fn_packXX.paramcalc.dat_oper + ", nzp_bank " +
                " From ttt_perc_ud " +
                " Where abs(sum_naud) > 0.001 "
                , true);
            if (!ret.result)
            {
                return;
            }

            //fn_naud: удержанные суммы
            ret = ExecSQL(conn_db,
                " Insert into " + fn_packXX.fn_naud + " (dat_oper, nzp_payer, nzp_dom, nzp_serv, nzp_payer_2, sum_ud, sum_naud, nzp_area, nzp_geu, nzp_bank) " +
                " Select " + fn_packXX.paramcalc.dat_oper + sConvToDate+", nzp_payer, nzp_dom, nzp_serv, nzp_payer_naud, sum_naud, 0 as sum_naud, nzp_area, 0 nzp_geu, nzp_bank " +
                " From ttt_perc_ud " +
                " Where abs(sum_naud) > 0.001 "
                , true);
            if (!ret.result)
            {
                return;
            }
            //fn_naud: начисленные суммы
            ret = ExecSQL(conn_db,
                " Insert into " + fn_packXX.fn_naud + " (dat_oper, nzp_payer, nzp_dom,nzp_serv, nzp_payer_2, sum_ud, sum_naud, nzp_area, nzp_geu, nzp_bank) " +
                " Select " + fn_packXX.paramcalc.dat_oper +sConvToDate+ ", nzp_payer_naud, nzp_dom, nzp_serv, nzp_payer, 0 sum_naud, sum_naud, nzp_area, 0 nzp_geu, nzp_bank " +
                " From ttt_perc_ud " +
                " Where abs(sum_naud) > 0.001 "
                , true);
            if (!ret.result)
            {
                return;
            }
            
            ExecSQL(conn_db, " Create index ix1_ttt_pud on ttt_perc_ud (nzp_payer,nzp_area,nzp_serv,nzp_bank) ", true);
            ExecSQL(conn_db, " Create index ix2_ttt_pud on ttt_perc_ud (nzp_payer_naud,nzp_area,nzp_serv,nzp_bank) ", true);
           #if PG
 ExecSQL(conn_db, " analyze ttt_perc_ud ", true);
#else
 ExecSQL(conn_db, " Update statistics for table ttt_perc_ud ", true);
#endif


            //добавить отсутв.строки в fn_distrib
            ret = ExecSQL(conn_db,
                " Update ttt_perc_ud " +
                " Set kod = 0 " +
                " Where 0 < ( Select count(*) From " + fn_packXX.fn_distrib + " a " +
                            " Where a.nzp_payer= ttt_perc_ud.nzp_payer_naud " +
                            "   and a.nzp_area = ttt_perc_ud.nzp_area " +
                            "   and a.nzp_dom = ttt_perc_ud.nzp_dom " +
                            "   and a.nzp_serv = ttt_perc_ud.nzp_serv " +
                            "   and a.nzp_bank = ttt_perc_ud.nzp_bank " +
                            "   and a.dat_oper = " + fn_packXX.paramcalc.dat_oper +sConvToDate+
                            " ) "
                , true);
            if (!ret.result)
            {
                return;
            }

            ExecSQL(conn_db, " Create index ix3_ttt_pud on ttt_perc_ud (kod) ", true);
           #if PG
 ExecSQL(conn_db, " analyze ttt_perc_ud ", true);
#else
 ExecSQL(conn_db, " Update statistics for table ttt_perc_ud ", true);
#endif

            ret = ExecSQL(conn_db,
                " Insert into " + fn_packXX.fn_distrib + " ( nzp_payer,nzp_area, nzp_dom,nzp_serv,nzp_bank,dat_oper ) " +
#if PG
                " Select distinct nzp_payer_naud,nzp_area, nzp_dom,nzp_serv,nzp_bank, to_date(" + fn_packXX.paramcalc.dat_oper + ", 'dd.mm.yyyy')" +
#else
 " Select unique nzp_payer_naud,nzp_area, nzp_dom,nzp_serv,nzp_bank, " + fn_packXX.paramcalc.dat_oper +
#endif
                " From ttt_perc_ud Where kod = 1 "
                , true);
            if (!ret.result)
            {
                return;
            }
            /*
            #if PG
ExecSQL(conn_db, " analyze " + fn_packXX.fn_distrib, true);
#else
ExecSQL(conn_db, " Update statistics for table " + fn_packXX.fn_distrib, true);
#endif
            */

            //занести в fn_distrib_xx
            ret = ExecSQL(conn_db,
                " Update " + fn_packXX.fn_distrib +
                " Set sum_ud = ( " +
                            " Select sum(sum_naud) From ttt_perc_ud a " +
                            " Where a.nzp_payer= " + fn_packXX.fn_distrib + ".nzp_payer " +
                            "   and a.nzp_area = " + fn_packXX.fn_distrib + ".nzp_area " +
                            "   and a.nzp_dom = " + fn_packXX.fn_distrib + ".nzp_dom " +
                            "   and a.nzp_serv = " + fn_packXX.fn_distrib + ".nzp_serv " +
                            "   and a.nzp_bank = " + fn_packXX.fn_distrib + ".nzp_bank " +
                            " ) " +
                " Where dat_oper = " + fn_packXX.paramcalc.dat_oper +sConvToDate+ " and  nzp_dom in (select  nzp_dom from t_dom) "+
                "   and 0 < ( Select count(*) From ttt_perc_ud a " +
                            " Where a.nzp_payer= " + fn_packXX.fn_distrib + ".nzp_payer " +
                            "   and a.nzp_area = " + fn_packXX.fn_distrib + ".nzp_area " +
                            "   and a.nzp_dom = " + fn_packXX.fn_distrib + ".nzp_dom " +
                            "   and a.nzp_serv = " + fn_packXX.fn_distrib + ".nzp_serv " +
                            "   and a.nzp_bank = " + fn_packXX.fn_distrib + ".nzp_bank " +
                            " ) "
                , true);
            if (!ret.result)
            {
                return;
            }

            ret = ExecSQL(conn_db,
                " Update " + fn_packXX.fn_distrib +
                " Set sum_naud = ( " +
                            " Select sum(sum_naud) From ttt_perc_ud a " +
                            " Where a.nzp_payer_naud= " + fn_packXX.fn_distrib + ".nzp_payer " +
                            "   and a.nzp_area = " + fn_packXX.fn_distrib + ".nzp_area " +
                            "   and a.nzp_dom = " + fn_packXX.fn_distrib + ".nzp_dom " +
                            "   and a.nzp_serv = " + fn_packXX.fn_distrib + ".nzp_serv " +
                            "   and a.nzp_bank = " + fn_packXX.fn_distrib + ".nzp_bank " +
                            " ) " +
                " Where dat_oper = " + fn_packXX.paramcalc.dat_oper +sConvToDate+ " and nzp_dom in (select  nzp_dom from t_dom) "+
                "   and 0 < ( Select count(*) From ttt_perc_ud a " +
                            " Where a.nzp_payer_naud= " + fn_packXX.fn_distrib + ".nzp_payer " +
                            "   and a.nzp_area = " + fn_packXX.fn_distrib + ".nzp_area " +
                            "   and a.nzp_dom = " + fn_packXX.fn_distrib + ".nzp_dom " +
                            "   and a.nzp_serv = " + fn_packXX.fn_distrib + ".nzp_serv " +
                            "   and a.nzp_bank = " + fn_packXX.fn_distrib + ".nzp_bank " +
                            " ) "
                , true);
            if (!ret.result)
            {
                return;
            }

            ExecSQL(conn_db, " Drop table ttt_perc_ud ", false);


            //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
            //учет fn_sended
            //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
            ExecSQL(conn_db, " Drop table ttt_paxx ", false);

          #if PG
  ret = ExecSQL(conn_db,
                " Select nzp_payer, nzp_area,nzp_dom,nzp_serv, -1 as nzp_bank, 1 as kod, sum(sum_send) as sum_send " +
                 " Into temp ttt_paxx  "+
                " From " + fn_packXX.fn_sended +
                " Where dat_oper = " + fn_packXX.paramcalc.dat_oper +
                " Group by 1,2,3,4,5 ", true);
#else
  ret = ExecSQL(conn_db,
                " Select nzp_payer, nzp_area,nzp_dom,nzp_serv, -1 as nzp_bank, 1 as kod, sum(sum_send) as sum_send " +
                " From " + fn_packXX.fn_sended +
                " Where dat_oper = " + fn_packXX.paramcalc.dat_oper +sConvToDate+ " and nzp_dom in (select  nzp_dom from t_dom) "+
                " Group by 1,2,3,4,5 " +
                " Into temp ttt_paxx With no log "
                , true);
#endif
            if (!ret.result)
            {
                return;
            }

            ExecSQL(conn_db, " Create unique index ix1_ttt_paxx on ttt_paxx (nzp_payer, nzp_area, nzp_dom,nzp_serv, nzp_bank) ", true);
            #if PG
ExecSQL(conn_db, " analyze ttt_paxx ", true);
#else
ExecSQL(conn_db, " Update statistics for table ttt_paxx ", true);
#endif

            ret = ExecSQL(conn_db,
                " Update ttt_paxx " +
                " Set kod = 0 " +
                " Where 0 < ( Select count(*) From " + fn_packXX.fn_distrib + " a " +
                            " Where a.nzp_payer= ttt_paxx.nzp_payer " +
                            "   and a.nzp_area = ttt_paxx.nzp_area " +
                            "   and a.nzp_dom = ttt_paxx.nzp_dom " +
                            "   and a.nzp_serv = ttt_paxx.nzp_serv " +
                            "   and a.nzp_bank = ttt_paxx.nzp_bank " +
                            "   and a.dat_oper = " + fn_packXX.paramcalc.dat_oper +sConvToDate+
                            " ) "
                , true);
            if (!ret.result)
            {
                return;
            }

            ExecSQL(conn_db, " Create index ix2_ttt_paxx on ttt_paxx (kod) ", true);
           #if PG
 ExecSQL(conn_db, " analyze ttt_paxx ", true);
#else
 ExecSQL(conn_db, " Update statistics for table ttt_paxx ", true);
#endif

            ret = ExecSQL(conn_db,
                " Insert into " + fn_packXX.fn_distrib + " ( nzp_payer,nzp_area, nzp_dom,nzp_serv,nzp_bank,dat_oper ) " +
                " Select nzp_payer,nzp_area, nzp_dom, nzp_serv,nzp_bank, " + fn_packXX.paramcalc.dat_oper +sConvToDate+
                " From ttt_paxx Where kod = 1 "
                , true);
            if (!ret.result)
            {
                return;
            }
            /*
           #if PG
 ExecSQL(conn_db, " analyze " + fn_packXX.fn_distrib, true);
#else
 ExecSQL(conn_db, " Update statistics for table " + fn_packXX.fn_distrib, true);
#endif
            */
            ret = ExecSQL(conn_db,
                " Update " + fn_packXX.fn_distrib +
                " Set sum_send = ( " +
                            " Select sum(sum_send) From ttt_paxx a " +
                            " Where a.nzp_payer= " + fn_packXX.fn_distrib + ".nzp_payer " +
                            "   and a.nzp_area = " + fn_packXX.fn_distrib + ".nzp_area " +
                            "   and a.nzp_dom = " + fn_packXX.fn_distrib + ".nzp_dom " +
                            "   and a.nzp_serv = " + fn_packXX.fn_distrib + ".nzp_serv " +
                            "   and a.nzp_bank = " + fn_packXX.fn_distrib + ".nzp_bank " +
                            " ) " +
                " Where dat_oper = " + fn_packXX.paramcalc.dat_oper +sConvToDate+ " and nzp_dom in (select  nzp_dom from t_dom) "+
                "   and 0 < ( Select count(*) From ttt_paxx a " +
                            " Where a.nzp_payer= " + fn_packXX.fn_distrib + ".nzp_payer " +
                            "   and a.nzp_area = " + fn_packXX.fn_distrib + ".nzp_area " +
                            "   and a.nzp_dom = " + fn_packXX.fn_distrib + ".nzp_dom " +
                            "   and a.nzp_serv = " + fn_packXX.fn_distrib + ".nzp_serv " +
                            "   and a.nzp_bank = " + fn_packXX.fn_distrib + ".nzp_bank " +
                            " ) "
                , true);
            if (!ret.result)
            {
                return;
            }

            //ExecSQL(conn_db, " Drop table ttt_paxx ", false);


            //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
            //учет fn_reval
            //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
            //ExecSQL(conn_db, " Drop table ttt_paxx ", false);
            IDbTransaction transactionID = conn_db.BeginTransaction();
            Update_reval(conn_db, transactionID, fn_packXX, false, out ret);
            transactionID.Commit();
/*
#if PG
    ret = ExecSQL(conn_db,
                " Select nzp_payer, nzp_area, nzp_dom,nzp_serv, -1 as nzp_bank, 1 as kod, sum(sum_reval) as sum_reval " +
                " Into temp ttt_paxx  "+
                " From " + fn_packXX.fn_reval +
                " Where dat_oper = " + fn_packXX.paramcalc.dat_oper +
                " Group by 1,2,3,4,5 " , true);
#else
            sql_text = " Select nzp_payer, nzp_area, nzp_dom,nzp_serv, -1 as nzp_bank, 1 as kod, sum(sum_reval) as sum_reval " +
                " From " + fn_packXX.fn_reval +
                " Where dat_oper = " + fn_packXX.paramcalc.dat_oper + " and nzp_dom in (select  nzp_dom from t_dom) " +
                " Group by 1,2,3,4,5 " +
                " Into temp ttt_paxx With no log ";

    ret = ExecSQL(conn_db,sql_text , true);
#endif
            if (!ret.result)
            {
                return;
            }

            ExecSQL(conn_db, " Create unique index ix1_ttt_paxx on ttt_paxx (nzp_payer, nzp_area, nzp_dom, nzp_serv, nzp_bank) ", true);
           #if PG
 ExecSQL(conn_db, " analyze ttt_paxx ", true);
#else
 ExecSQL(conn_db, " Update statistics for table ttt_paxx ", true);
#endif

 //DataTable drr2 = ViewTbl(conn_db, " select * from ttt_paxx  ");
            sql_text= " Update ttt_paxx " +
                " Set kod = 0 " +
                " Where 0 < ( Select count(*) From " + fn_packXX.fn_distrib + " a " +
                            " Where a.nzp_payer= ttt_paxx.nzp_payer " +
                            "   and a.nzp_area = ttt_paxx.nzp_area " +
                            "   and a.nzp_dom = ttt_paxx.nzp_dom " +
                            "   and a.nzp_serv = ttt_paxx.nzp_serv " +
                            "   and a.nzp_bank = ttt_paxx.nzp_bank " +
                            "   and a.dat_oper = " + fn_packXX.paramcalc.dat_oper +
                            " ) ";

            ret = ExecSQL(conn_db,sql_text, true);
            if (!ret.result)
            {
                return;
            }

            ExecSQL(conn_db, " Create index ix2_ttt_paxx on ttt_paxx (kod) ", true);
           #if PG
 ExecSQL(conn_db, " analyze ttt_paxx ", true);
#else
 ExecSQL(conn_db, " Update statistics for table ttt_paxx ", true);
#endif

 DataTable drr2 = ViewTbl(conn_db, " select * from ttt_paxx  ");
            sql_text=" Insert into " + fn_packXX.fn_distrib + " ( nzp_payer,nzp_area, nzp_dom,nzp_serv,nzp_bank,dat_oper ) " +
                " Select nzp_payer,nzp_area, nzp_dom,nzp_serv,nzp_bank, " + fn_packXX.paramcalc.dat_oper +
                " From ttt_paxx Where kod = 1 ";
            ret = ExecSQL(conn_db,sql_text , true);
            if (!ret.result)
            {
                return;
            }
            sql_text =
                " Update " + fn_packXX.fn_distrib +
                " Set sum_reval = ( " +
                            " Select sum(sum_reval) From ttt_paxx a " +
                            " Where a.nzp_payer= " + fn_packXX.fn_distrib + ".nzp_payer " +
                            "   and a.nzp_area = " + fn_packXX.fn_distrib + ".nzp_area " +
                            "   and a.nzp_dom = " + fn_packXX.fn_distrib + ".nzp_dom " +
                            "   and a.nzp_serv = " + fn_packXX.fn_distrib + ".nzp_serv " +
                            "   and a.nzp_bank = " + fn_packXX.fn_distrib + ".nzp_bank " +
                            " ) " +
                " Where dat_oper = " + fn_packXX.paramcalc.dat_oper + " and nzp_dom in (select  nzp_dom from t_dom) " +
                "   and 0 < ( Select count(*) From ttt_paxx a " +
                            " Where a.nzp_payer= " + fn_packXX.fn_distrib + ".nzp_payer " +
                            "   and a.nzp_area = " + fn_packXX.fn_distrib + ".nzp_area " +
                            "   and a.nzp_dom = " + fn_packXX.fn_distrib + ".nzp_dom " +
                            "   and a.nzp_serv = " + fn_packXX.fn_distrib + ".nzp_serv " +
                            "   and a.nzp_bank = " + fn_packXX.fn_distrib + ".nzp_bank " +
                            " ) ";

            ret = ExecSQL(conn_db,sql_text, true);
            if (!ret.result)
            {
                return;
            }

            ExecSQL(conn_db, " Drop table ttt_paxx ", false);
            */
            //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
            //расчет итогового сальдо
            //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
            ret = ExecSQL(conn_db,
                " Update " + fn_packXX.fn_distrib +
                " Set sum_out = sum_in + sum_rasp - sum_ud + sum_naud + sum_reval - sum_send " +
                "   ,sum_charge = sum_rasp - sum_ud + sum_naud + sum_reval " +
                " Where dat_oper = " + fn_packXX.paramcalc.dat_oper + sConvToDate+" and nzp_dom in (select  nzp_dom from t_dom) "
                , true);
            if (!ret.result)
            {
                return;
            }
            /*
            if (ret.result)
            {
                ret = ExecSQL(conn_db,
                    " Delete From " + fn_packXX.fn_operday_log +
                    " Where date_oper = " + fn_packXX.paramcalc.dat_oper
                    , true);
            }
             */

            /*
            sql_text = "drop  table t_dom";
            ret = ExecSQL(conn_db, sql_text, false);

            sql_text = "drop  table t_selkvar";
            ret = ExecSQL(conn_db, sql_text, false);
            */
            MessageInPackLog(conn_db, fn_packXX, "Окончание расчета сальдо по перечислениям", false, out r);


        }

        //поиск ошибок за опердень fn_packXX.paramcalc.dat_oper
        //--------------------------------------------------------------------------------
        bool PackError(IDbConnection conn_db, bool all_opermonth, DateTime dat_oper, ref List<int> points, out Returns ret)
        //--------------------------------------------------------------------------------
        {
            float dlt1 =0;
            float dlt2 = 0;
            float dlt3 = 0;

            decimal f1 = 0;
            decimal f2 = 0;
            decimal f3 = 0;

            ret = Utils.InitReturns();
            Returns r = Utils.InitReturns();

            bool yes_erros = false;

            int cur_yy = dat_oper.Year;
            int cur_mm = dat_oper.Month;


            CalcTypes.ParamCalc paramcalc = new CalcTypes.ParamCalc(0, 0, Points.Pref, cur_yy, cur_mm, cur_yy, cur_mm);
            PackXX fn_packXX = new PackXX(paramcalc, 0, false);
            fn_packXX.all_opermonth = all_opermonth;
            MessageInPackLog(conn_db, fn_packXX, "Начало поиска ошибок", false, out r);

            string ol_server = "";
            IDataReader reader;

            //перебрать все локальные банки
            foreach (_Point zap in Points.PointList)
            {
                paramcalc.pref = zap.pref;
#if PG
                string table_kvar = zap.pref + "_data" + ol_server + ".kvar ";
#else
                string table_kvar = zap.pref + "_data" + ol_server + ":kvar ";
#endif

                PackXX local_packXX = new PackXX(paramcalc, 0, false);
                local_packXX.all_opermonth = all_opermonth;
                //CreatePaXX(conn_db, local_packXX, false, out ret); //навсякий случай создать локальный fn_pa_xx

                bool b_err = false;
                //даем 3 попытки для поиска и исправление
                for (int i = 1; i <= 3; i++)
                {
                    //начинаем сверять: pack_ls = fn_supplierXX  local_fn_pa
                    ret = ExecRead(conn_db, out reader,
#if PG
 " Select max(1) as kod, sum(g_sum_ls) as sum_rasp From " + fn_packXX.pack + " p,"+ fn_packXX.pack_ls + " a, " + table_kvar + " b " +
                        " Where a.num_ls = b.num_ls " +
                        "   and a.dat_uchet " + fn_packXX.where_dat_oper +
                        "   and a.nzp_pack = p.nzp_pack and p.nzp_pack <> 20 "+
                        "   and a.inbasket = 0 " +
                        "   and coalesce(a.alg::int,0) > 0 " +
                        " Union " +
                        " Select max(2) as kod, sum(sum_prih) as sum_rasp From " + local_packXX.fn_supplier +
                                                " Where dat_uchet " + fn_packXX.where_dat_oper +
                        " Union " +
                        " Select max(3) as kod, sum(sum_prih) as sum_rasp From " + local_packXX.fn_pa_xx +                       
                        " Where dat_oper " + fn_packXX.where_dat_oper
#else
 " Select max(1) as kod, sum(g_sum_ls) as sum_rasp From " + fn_packXX.pack + " p,"+ fn_packXX.pack_ls + " a, " + table_kvar + " b " +
                        " Where a.num_ls = b.num_ls " +
                        "   and a.dat_uchet " + fn_packXX.where_dat_oper +
                        "   and a.nzp_pack = p.nzp_pack and p.nzp_pack <> 20 " +
                        "   and a.inbasket = 0 " +
                        "   and nvl(a.alg,0) > 0 " +
                        " Union " +
                        " Select max(2) as kod, sum(sum_prih) as sum_rasp From " + local_packXX.fn_supplier +                        
                        " Where dat_uchet " + fn_packXX.where_dat_oper +
                        " Union " +
                        " Select max(3) as kod, sum(sum_prih) as sum_rasp From " + local_packXX.fn_pa_xx +
                                              " Where dat_oper " + fn_packXX.where_dat_oper
#endif
                        , true);
                    if (!ret.result)
                    {
                        return false;
                    }
                    try
                    {
                        dlt1 = 0;
                        dlt2 = 0;
                        dlt3 = 0;

                        f1 = 0;
                        f2 = 0;
                        f3 = 0;

                        while (reader.Read())
                        {
                            if (reader["kod"] != DBNull.Value)
                            {
                                if ((int)reader["kod"] == 1 && reader["sum_rasp"] != DBNull.Value)
                                    f1 = Convert.ToDecimal(reader["sum_rasp"]);
                                if ((int)reader["kod"] == 2 && reader["sum_rasp"] != DBNull.Value)
                                    f2 = Convert.ToDecimal(reader["sum_rasp"]);
                                if ((int)reader["kod"] == 3 && reader["sum_rasp"] != DBNull.Value)
                                    f3 = Convert.ToDecimal(reader["sum_rasp"]);

                            }
                        }
                        reader.Close();

                        if (dlt1 > 0.001 || dlt2 > 0.001 || dlt3 > 0.001)
                        {
                            b_err = true;
                            yes_erros = true;
                            
                            dlt1 = Decimal.ToSingle(Math.Abs(f1 - f2));
                            dlt2 = Decimal.ToSingle(Math.Abs(f1 - f3));
                            dlt3 = Decimal.ToSingle(Math.Abs(f2 - f3));

                            //обнаружены ошибки, попробуем исправить
                            string serr = "Обнаружены ошибки: " + zap.pref +
                                " f1=" + f1.ToString("00000000.00") +
                                " f2=" + f2.ToString("00000000.00") +
                                " f3=" + f3.ToString("00000000.00") +
                                " d1=" + dlt1.ToString("00000000.00") +
                                " d2=" + dlt2.ToString("00000000.00") +
                                " d3=" + dlt3.ToString("00000000.00");
                            MessageInPackLog(conn_db, fn_packXX, serr, true, out r);
                            MonitorLog.WriteLog(serr, MonitorLog.typelog.Error, 1, 222, true);

                            //исправление ошибок
                            /*if (dlt1 > 0.001 || dlt2 > 0.001) //pack_ls != fn_supplier
                            {
                                //надо найти ошибочные nzp_pack_ls и кинуть их в корзину, а распределение удалить
                                EqualPack(conn_db, local_packXX, out ret);
                                if (!ret.result)
                                {
                                    return false;
                                }
                                
                            }*/

                            //перезалить local_packXX.fn_pa_xx
                            //LoadLocalPaXX(conn_db, local_packXX, true, out ret);
                            LoadLocalPaXX(conn_db, fn_packXX, true, out ret);
                            if (!ret.result)
                            {
                                return false;
                            }

                            //пересчитать сальдо
                            if (points != null)
                                points.Add(zap.nzp_wp);


                            /*
                            //скорректировать sum_rasp, sum_nrasp
                            ret = ExecRead(conn_db, out reader,
                                , true);
                            if (!ret.result)
                            {
                                conn_db.Close();
                                return;
                            }
                            */

                            continue; //продолжаем проверять!
                        }
                    }
                    catch (Exception ex)
                    {
                        reader.Close();
                        ret.text = ex.Message;
                        ret.result = false;

                        return false;
                    }

                    b_err = false; //ошибок нет
                    break;
                }


                if (b_err)
                {
                    //значит ошибка не исправлена, надо обратиться к разработчикам
                    string serr = "Ошибки распределения не исправлены, обратитесь к разработчикам (" + zap.pref + ")";
                    MessageInPackLog(conn_db, fn_packXX, serr, true, out r);
                    MonitorLog.WriteLog(serr, MonitorLog.typelog.Error, 1, 222, true);

                    ret.result = false;
                    ret.tag = -1;
                    ret.text = "Несоответсвие суммы распределения в центральном офисе с распределениями по управляющим организациям (" + zap.point + ")<br>" +
                    "Обнаружены ошибки: " + zap.pref +
                                " Распределен f1=" + f1.ToString("00000000.00") +"<br>"+
                                " Распределен f2=" + f2.ToString("00000000.00") + "<br>" +
                                " Распределен f3=" + f3.ToString("00000000.00") + "<br>" +
                                " Разница d1=" + dlt1.ToString("00000000.00") + "<br>" +
                                " Разница d2=" + dlt2.ToString("00000000.00") + "<br>" +
                                " Разница d3=" + dlt3.ToString("00000000.00");
                    
                    return false;
                }

            }


            //скорректировать sum_rasp, sum_nrasp
            /*ret = ExecSQL(conn_db,
                " Update " + fn_packXX.pack +
                " Set (sum_rasp,sum_nrasp) = (( " +
                        " Select sum(case when dat_uchet is not null and inbasket=0 and nvl(alg,0)>0 then g_sum_ls else 0 end), " +
                               " sum(case when dat_uchet is not null and inbasket=0 and nvl(alg,0)>0 then 0 else g_sum_ls end)  " +
                        " From " + fn_packXX.pack_ls +
                        " Where nzp_pack in ( Select nzp_pack From " + fn_packXX.pack_ls + " Where dat_uchet " + fn_packXX.where_dat_oper + " ) " +
                        " )) " +
                " Where nzp_pack in ( " +
                    " Select nzp_pack From " + fn_packXX.pack_ls +
                    " Where dat_uchet " + fn_packXX.where_dat_oper + " ) "
                , true);
            if (!ret.result)
            {
                conn_db.Close();
                return false;
            }*/

            //пачка распределена или распределена с ошибками, где есть распределенные оплаты
            /*ret = ExecSQL(conn_db,
                " Update " + fn_packXX.pack +
                " Set flag = ( " +
                        " Select max(case when dat_uchet is not null and inbasket=0 then 21 else 22 end) " +
                        " From " + fn_packXX.pack_ls +
                        " Where nzp_pack in ( Select nzp_pack From " + fn_packXX.pack_ls + " Where dat_uchet " + fn_packXX.where_dat_oper + " ) " +
                        " ) " +
                " Where nzp_pack in ( Select nzp_pack From " + fn_packXX.pack_ls + " Where dat_uchet " + fn_packXX.where_dat_oper + " ) "
                , true);
            if (!ret.result)
            {
                conn_db.Close();
                return false;
            }*/


            /*if (fn_packXX.all_opermonth)
            {

                //обработать ситуацию, когда в пачке одна нераспределенная оплата
                //проверить update на практике !!!
                ret = ExecSQL(conn_db,
                    " Update " + fn_packXX.pack +
                    " Set sum_rasp = 0, sum_nrasp = 0, flag = 23 " +
                    " Where nzp_pack in ( Select nzp_pack From " + fn_packXX.pack_ls + " Where dat_uchet is null ) " +
                    "   and nzp_pack not in ( Select nzp_pack From " + fn_packXX.pack_ls + " Where dat_uchet is not null ) " +
                    "   and flag <> 23 "
                    , true);
                if (!ret.result)
                {
                    conn_db.Close();
                    return false;
                }
            }*/

            MessageInPackLog(conn_db, fn_packXX, "Поиск ошибок завершен", false, out r);

            return yes_erros;
        }

        //--------------------------------------------------------------------------------
        void EqualPack(IDbConnection conn_db, PackXX packXX, out Returns ret)
        //--------------------------------------------------------------------------------
        {
            ret = Utils.InitReturns();

            //выбрать мно-во nzp_pack_ls: t_selkvar
            ExecSQL(conn_db, " Drop table t_selkvar ", false);

            CreateSelKvar(conn_db, packXX, true, out ret);
            if (!ret.result)
            {
                return;
            }

            //удалить распределение, где отсутствуют nzp_pack_ls
            /*
            ret = ExecSQL(conn_db,
                " Delete From " + packXX.fn_supplier +
                " Where dat_uchet " + packXX.where_dat_oper +
                "   and nzp_pack_ls not in ( Select nzp_pack_ls From t_selkvar ) "
                , true);
            */
            ExecByStep(conn_db, packXX.fn_supplier, "nzp_to",
                " Delete From " + packXX.fn_supplier +
                " Where dat_uchet " + packXX.where_dat_oper +
                "   and nzp_pack_ls not in ( Select nzp_pack_ls From t_selkvar ) "
                    , 100000, " ", out ret);
            if (!ret.result)
            {
                return;
            }
            UpdateStatistics(true, packXX.paramcalc, packXX.fn_supplier_tab, out ret);


            //собрать сумму по fn_supplier
            ExecSQL(conn_db, " Drop table ttt_errp ", false);

           #if PG
 ret = ExecSQL(conn_db,
                " Select a.nzp_pack_ls, sum(a.sum_prih) as sum_prih, sum(b.g_sum_ls) as g_sum_ls " +
                 " Into "+
#if PG
 " temp " +
#else
" temp "+
#endif
                 "ttt_errp  "+
                " From " + packXX.fn_supplier + " a, t_selkvar b " +
                " Where a.nzp_pack_ls = b.nzp_pack_ls " +
                " Group by 1 " , true);
#else
 ret = ExecSQL(conn_db,
                " Select a.nzp_pack_ls, sum(a.sum_prih) as sum_prih, sum(b.g_sum_ls) as g_sum_ls " +
                " From " + packXX.fn_supplier + " a, t_selkvar b " +
                " Where a.nzp_pack_ls = b.nzp_pack_ls " +
                " Group by 1 " +
                " Into temp ttt_errp With no log "
                , true);
#endif
            if (!ret.result)
            {
                return;
            }

            ExecSQL(conn_db, " Create unique index ix1_ttt_errp on ttt_errp (nzp_pack_ls) ", true);
           #if PG
 ExecSQL(conn_db, " analyze ttt_errp ", true);
#else
 ExecSQL(conn_db, " Update statistics for table ttt_errp ", true);
#endif

            //удалить распределение, где суммы распределения не совпадают
            /*
            ret = ExecSQL(conn_db,
                " Delete From " + packXX.fn_supplier +
                " Where nzp_pack_ls in ( Select nzp_pack_ls From ttt_errp Where abs(sum_prih - g_sum_ls) > 0.001 ) "
                , true);
            */
            ExecByStep(conn_db, packXX.fn_supplier, "nzp_to",
                    " Delete From " + packXX.fn_supplier +
                    " Where nzp_pack_ls in ( Select nzp_pack_ls From ttt_errp Where abs(sum_prih - g_sum_ls) > 0.001 ) "
                    , 100000, " ", out ret);
            if (!ret.result)
            {
                return;
            }
            UpdateStatistics(true, packXX.paramcalc, packXX.fn_supplier_tab, out ret);


            ExecSQL(conn_db, " Drop table ttt_errp ", false);

            //кинуть в корзину оплат, где нет распределение
            ret = ExecSQL(conn_db,
                " Update " + packXX.pack_ls +
                " Set inbasket = 1, dat_uchet = null " +
                " Where nzp_pack_ls in ( Select nzp_pack_ls From t_selkvar ) " +
                "   and nzp_pack_ls not in ( Select nzp_pack_ls From " + packXX.fn_supplier + " ) "
                , true);
            if (!ret.result)
            {
                return;
            }

            //вроде, все 
        }

        //поиск ошибок за месяц
        //--------------------------------------------------------------------------------
        public void PackErrorMonth(out Returns ret)
        //--------------------------------------------------------------------------------
        {
            ret = Utils.InitReturns();

            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result)
            {
                return;
            }

            List<int> l = new List<int>();
            PackError(conn_db, true, Points.DateOper, ref l, out ret);

            conn_db.Close();
        }


        //проверка, что все оплаты учтены
        //--------------------------------------------------------------------------------
        public string CheckCalcMoney(int cur_yy, int cur_mm)
        //--------------------------------------------------------------------------------
        {
            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
            Returns ret = OpenDb(conn_db, true);
            if (!ret.result)
            {
                return "Ошибка выполнения";
            }

            bool b = CalcAllMoney(conn_db, cur_yy, cur_mm, out ret);
            conn_db.Close();

            if (!ret.result)
            {
                return "Ошибка выполнения";
            }
            else
            {
                if (b)
                    return "Все оплаты учтены";
                else
                    return "Найдены неучтенные оплаты - запущена процедура расчета сальдо";
            }

        }

        //--------------------------------------------------------------------------------
        public bool CalcAllMoney(IDbConnection conn_db, int cur_yy, int cur_mm, out Returns ret)
        //--------------------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            Returns r = Utils.InitReturns();

            CalcTypes.ParamCalc paramcalc = new CalcTypes.ParamCalc(0, 0, Points.Pref, cur_yy, cur_mm, cur_yy, cur_mm);
            PackXX fn_packXX = new PackXX(paramcalc, 0, false);

            IDataReader reader;
            bool b_calc = true;

            //перебрать все локальные банки
            foreach (_Point zap in Points.PointList)
            {
                paramcalc.pref = zap.pref;
                PackXX local_packXX = new PackXX(paramcalc, 0, false);
                
                //начинаем сверять: fn_supplierXX  == charge_xx.money_to
                ret = ExecRead(conn_db, out reader,
                    " Select max(1) as kod, sum(sum_prih) as sum_rasp From " + local_packXX.fn_supplier +
                    " Where nzp_serv > 1 " +
                    " Union " +
                    " Select max(2) as kod, sum(money_to) as sum_rasp From " + local_packXX.charge_xx +
                    " Where nzp_serv > 1 and dat_charge is null"
                    , true);
                if (!ret.result)
                {
                    return false;
                }
                try
                {
                    decimal f1 = 0;
                    decimal f2 = 0;

                    while (reader.Read())
                    {
                        if (reader["kod"] != DBNull.Value)
                        {
                            if ((int)reader["kod"] == 1 && reader["sum_rasp"] != DBNull.Value)
                                f1 = (decimal)reader["sum_rasp"];
                            if ((int)reader["kod"] == 2 && reader["sum_rasp"] != DBNull.Value)
                                f2 = (decimal)reader["sum_rasp"];
                        }
                    }
                    reader.Close();

                    float dlt1 = Decimal.ToSingle(Math.Abs(f1 - f2));
                    if (dlt1 > 0.001)
                    {
                        b_calc = false;

                        //обнаружены ошибки, попробуем исправить
                        string serr = "Обнаружены ошибки учета оплат: " + zap.pref +
                            " f1=" + f1.ToString("00000000.00") +
                            " f2=" + f2.ToString("00000000.00") +
                            " dlt=" + dlt1.ToString("00000000.00") +
                            " - запущена процедура расчета сальдо ";
                        //MessageInPackLog(conn_db, fn_packXX, serr, true, out r);
                        MonitorLog.WriteLog(serr, MonitorLog.typelog.Error, 1, 222, true);

                        //вызвать задание расчета сальдо
                        CalcSaldo(zap.nzp_wp, out ret);
                        if (!ret.result)
                        {
                            return false;
                        }


                    }
                }
                catch (Exception ex)
                {
                    reader.Close();
                    ret.text = ex.Message;
                    ret.result = false;

                    return false;
                }
            }


            return b_calc;
        }


        //тестовый метод для распределения оплат
        //--------------------------------------------------------------------------------
        public bool TestPackXX2(int nzp_kvar, int nzp_pack, string pref, int cur_yy, int cur_mm, out Returns ret)
        //--------------------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            CalcTypes.ParamCalc paramcalc = new CalcTypes.ParamCalc(nzp_kvar, nzp_pack, pref, cur_yy, cur_mm, cur_yy, cur_mm);
            paramcalc.b_pack = true;

            string conn_kernel = Points.GetConnByPref(paramcalc.pref);
            IDbConnection conn_db = GetConnection(conn_kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result)
            {
                return false;
            }

            bool b = CalcPackXX(conn_db, paramcalc, false, out ret);

            // Обновить инорфмацию в fn_distrib_dom_MM
            DistribPaXX(conn_db, paramcalc, out ret);

            conn_db.Close();

            return b;
        }


        public void PutTasksDistribOneLs(Dictionary<int, int> listPackLs, bool to_dis, string comment, out Returns ret)
        {
            //затем выставим задание на распределение или откат
            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result) return;

            CalcFon calcfon = new CalcFon(0);

            foreach (KeyValuePair<int, int> recLs in listPackLs)
            {
                calcfon.task = FonTaskTypeIds.DistributeOneLs;
                calcfon.status = FonTaskStatusId.New; //на выполнение 
                calcfon.nzpt = 1;
                calcfon.nzp_key = 0;
                calcfon.nzp = recLs.Key;
                calcfon.yy = recLs.Value;
                calcfon.mm = 0;

                calcfon.txt = "'" + recLs.Key + "'";

                CheckCalcAndPutInFon(conn_web, calcfon, out ret);
                if (!ret.result)
                {
                    break;
                }
            }

            conn_web.Close();
        }

        public bool CalcDistribPackLs(CalcFon calcfon, out Returns ret)
        {
            CalcPackLs(System.Convert.ToInt32(calcfon.nzp), Points.DateOper, true,false, out ret,calcfon.nzp_user);
            return ret.result;
        }

        public Returns CreatePackOverPayment(Pack finder)
        {
            #region проверка вx параметров
            if (finder.nzp_user < 0) return new Returns(false, "Не задан пользователь", -1);
            #endregion

            #region соединение conn_web
            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            Returns ret = OpenDb(conn_web, true);
            if (!ret.result) return ret;
            #endregion

            #region соединение conn_db
            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return ret;
            #endregion

            string tXX_overpayment = "t" + Convert.ToString(finder.nzp_user) + "_overpayment";
#if PG
            string tXX_overpayment_full = "public." + tXX_overpayment;
#else
            string tXX_overpayment_full = conn_web.Database + "@" + DBManager.getServer(conn_web) + ":" + tXX_overpayment;
#endif
            DataTable dt;
            DataTable dt2;
            
            string erc_code = "";
            string baseFin = Points.Pref + "_fin_" + Points.DateOper.ToString("yyyy").Substring(2, 2);
            string fn_supplier_tab;
            string charge_xx;

            Int32 nzp_pack_from = 0;
            Int32 nzp_pack_to = 0;
            Int32 nzp_pack_ls_from = 0;
            Int32 nzp_pack_ls_to = 0;
            Int32 par_pack = 0;

            
            Decimal sum_total=0;
            Decimal sum_total_pack = 0;
            int count_total = 0;
            float dlt0 = 0;



            //проверка: таблица с переплатами создана
            if (!TableInWebCashe(conn_web, tXX_overpayment)) return new Returns(false, "Нет выбранных переплат", -1);


            #if PG
string sql = "select count(*) cnt from " + tXX_overpayment_full + " where coalesce(nzp_kvar_to,0)>0 and sum_overpay<>0  and mark = 1 and coalesce(nzp_kvar_from,0)>0  ";
#else
string sql = "select count(*) cnt from " + tXX_overpayment_full + " where NVL(nzp_kvar_to,0)>0 and sum_overpay<>0  and mark = 1 and NVL(nzp_kvar_from,0)>0  ";
#endif
            dt = ClassDBUtils.OpenSQL(sql, conn_web).GetData();
            Int32 cnt = Convert.ToInt32(dt.Rows[0]["cnt"]);
            if (cnt > 0)
            {

                

                // Есть данные для переноса !
                #if PG
sql = " select erc_code from " + Points.Pref + "_kernel.s_erc_code where is_now() = 1 ";
#else
sql = " select erc_code from " + Points.Pref + "_kernel:s_erc_code where is_current = 1 ";
#endif
                dt = ClassDBUtils.OpenSQL(sql, conn_db).GetData();
                foreach (DataRow row2 in dt.Rows)
                {
                    erc_code = row2["erc_code"].ToString().Trim().Substring(0, 12);
                    break;
                }
                finder.dat_uchet = Points.DateOper.ToShortDateString();
                IDbTransaction transaction;
                // Создать суперпачку
                #if PG
sql = "insert into " + baseFin + ".pack (nzp_pack, nzp_bank, num_pack, dat_pack, dat_uchet, count_kv, sum_pack, flag, erc_code, pack_type, sum_rasp, sum_nrasp, file_name )" +
                        " values (default, 1000, '" + finder.num_pack + "', " + Utils.EStrNull(finder.dat_pack) + "," +
                         Utils.EStrNull(finder.dat_uchet) +
                        ", 2, 0, 22," + Utils.EStrNull(erc_code, "") + ",10,0,0,'Перенос переплат')";
#else
sql = "insert into " + baseFin + ":pack (nzp_pack, nzp_bank, num_pack, dat_pack, dat_uchet, count_kv, sum_pack, flag, erc_code, pack_type, sum_rasp, sum_nrasp, file_name )" +
                        " values (0, 1000, '" + finder.num_pack + "', " + Utils.EStrNull(finder.dat_pack) + "," +
                         Utils.EStrNull(finder.dat_uchet) +
                        ", 2, 0, 22," + Utils.EStrNull(erc_code, "") + ",10,0,0,'Перенос переплат')";
#endif
                ret = ExecSQL(conn_db, null, sql, true);
                if (!ret.result)
                {
                    return ret;
                }
                par_pack = GetSerialValue(conn_db, null);

                #if PG
sql = "update " + baseFin + ".pack set par_pack = nzp_pack  where nzp_pack = " + par_pack;
#else
sql = "update " + baseFin + ":pack set par_pack = nzp_pack  where nzp_pack = " + par_pack;
#endif
                ret = ExecSQL(conn_db, null, sql, true);
                if (!ret.result)
                {
                    return ret;
                }



               #if PG
 sql = "insert into " + baseFin + ".pack (nzp_pack, par_pack, nzp_bank, num_pack, dat_pack, dat_uchet, count_kv, sum_pack, flag, erc_code, file_name)" +
                        " values (default, " + par_pack + ", " + finder.nzp_bank + ",'" + finder.num_pack + "', " + Utils.EStrNull(finder.dat_pack) +
                        "," + Utils.EStrNull(finder.dat_uchet) +
                        ", 0, 0, " + Pack.Statuses.Open.GetHashCode() + "," + Utils.EStrNull(erc_code, "") + ",'Перенос переплат')";
#else
 sql = "insert into " + baseFin + ":pack (nzp_pack, par_pack, nzp_bank, num_pack, dat_pack, dat_uchet, count_kv, sum_pack, flag, erc_code, file_name)" +
                        " values (0, " + par_pack + ", " + finder.nzp_bank + ",'" + finder.num_pack + "', " + Utils.EStrNull(finder.dat_pack) +
                        "," + Utils.EStrNull(finder.dat_uchet) +
                        ", 0, 0, " + Pack.Statuses.Open.GetHashCode() + "," + Utils.EStrNull(erc_code, "") + ",'Перенос переплат')";
#endif
                ret = ExecSQL(conn_db, null, sql, true);
                if (!ret.result)
                {
                    return ret;
                }
                nzp_pack_from = GetSerialValue(conn_db, null);


#if PG
sql = "insert into " + baseFin + ".pack (nzp_pack, par_pack, nzp_bank, num_pack, dat_pack, dat_uchet,count_kv, sum_pack, flag, erc_code, file_name)" +
                        " values (default, " + par_pack + ", " + finder.nzp_bank + ",'" + finder.num_pack + "', " + Utils.EStrNull(finder.dat_pack) + "," + Utils.EStrNull(finder.dat_uchet) +
                        ", 0, 0, " + Pack.Statuses.Open.GetHashCode() + "," + Utils.EStrNull(erc_code, "") + ",'Перенос переплат')";
#else
sql = "insert into " + baseFin + ":pack (nzp_pack, par_pack, nzp_bank, num_pack, dat_pack, dat_uchet,count_kv, sum_pack, flag, erc_code, file_name)" +
                        " values (0, " + par_pack + ", " + finder.nzp_bank + ",'" + finder.num_pack + "', " + Utils.EStrNull(finder.dat_pack) + "," + Utils.EStrNull(finder.dat_uchet) +
                        ", 0, 0, " + Pack.Statuses.Open.GetHashCode() + "," + Utils.EStrNull(erc_code, "") + ",'Перенос переплат')";
#endif
                ret = ExecSQL(conn_db, null, sql, true);
                if (!ret.result)
                {
                    return ret;
                }
                nzp_pack_to = GetSerialValue(conn_db, null);
                sum_total_pack = 0;
                sql = "select * from " + tXX_overpayment_full +
#if PG
                        " where coalesce(nzp_kvar_to,0)>0 and sum_overpay<>0  and mark = 1 and coalesce(nzp_kvar_from,0)>0 ";
#else
                        " where NVL(nzp_kvar_to,0)>0 and sum_overpay<>0  and mark = 1 and NVL(nzp_kvar_from,0)>0 ";
#endif
                dt = ClassDBUtils.OpenSQL(sql, conn_web).GetData();
                foreach (DataRow row3 in dt.Rows)
                {
                    count_total = count_total + 1;
                    transaction = conn_db.BeginTransaction();

                    #region 1. Снять переплаты с закрытых лицевых счетов
                  #if PG
  sql = "INSERT into " + baseFin + ".pack_ls(nzp_pack, prefix_ls,  num_ls, g_sum_ls, geton_ls," +
                    "sum_ls, dat_month, kod_sum, paysource, dat_vvod, anketa, inbasket, erc_code, unl, info_num, alg, dat_uchet) " +
                    "VALUES(" + nzp_pack_from + ",0," +
                    row3["num_ls"].ToString().Trim() + "," +
                    row3["sum_overpay"].ToString().Trim() + ",0,0,'" + finder.dat_pack + "',33,0," +
                    "'" + finder.dat_pack.Trim().Trim() + "',0,0," + erc_code + ",0," + count_total + ",1, '" + finder.dat_uchet + "')";
#else
  sql = "INSERT into " + baseFin + ":pack_ls(nzp_pack, prefix_ls,  num_ls, g_sum_ls, geton_ls," +
                    "sum_ls, dat_month, kod_sum, paysource, dat_vvod, anketa, inbasket, erc_code, unl, info_num, alg, dat_uchet) " +
                    "VALUES(" + nzp_pack_from + ",0," +
                    row3["num_ls"].ToString().Trim() + "," +
                    row3["sum_overpay"].ToString().Trim() + ",0,0,'" + finder.dat_pack + "',33,0," +
                    "'" + finder.dat_pack.Trim().Trim() + "',0,0," + erc_code + ",0," + count_total + ",1, '" + finder.dat_uchet + "')";
#endif

                    ret = ExecSQL(conn_db, transaction, sql, true);
                    if (!ret.result)
                    {
                        return ret;
                    }
                    nzp_pack_ls_from = GetSerialValue(conn_db, transaction);

                   #if PG
 fn_supplier_tab = row3["pref_from"].ToString().Trim() + "_charge_" + (Points.DateOper.Year - 2000).ToString("00") + ".fn_supplier" + Points.DateOper.Month.ToString("00");
                    charge_xx = row3["pref_from"].ToString().Trim() + "_charge_" + (Points.DateOper.Year - 2000).ToString("00") + ".charge_" + Points.DateOper.Month.ToString("00");
#else
 fn_supplier_tab = row3["pref_from"].ToString().Trim() + "_charge_" + (Points.DateOper.Year - 2000).ToString("00") + ":fn_supplier" + Points.DateOper.Month.ToString("00");
                    charge_xx = row3["pref_from"].ToString().Trim() + "_charge_" + (Points.DateOper.Year - 2000).ToString("00") + ":charge_" + Points.DateOper.Month.ToString("00");
#endif

                    sql = "select * from " + charge_xx + " where nzp_kvar = " + row3["nzp_kvar_from"].ToString().Trim() + " and nzp_serv >1 and dat_charge is null ";
                    dt2 = ClassDBUtils.OpenSQL(sql, conn_db, transaction).GetData();
                    sum_total = 0;
                    foreach (DataRow row4 in dt2.Rows)
                    {

                      #if PG
  sql = "INSERT into " + baseFin + ".gil_sums(nzp_pack_ls, num_ls, nzp_serv, nzp_supp, sum_oplat, dat_uchet) " +
                        "VALUES(" +
                        nzp_pack_ls_from + "," +
                        row4["num_ls"].ToString().Trim() + "," +
                        row4["nzp_serv"].ToString().Trim() + "," +
                        row4["nzp_supp"].ToString().Trim() + "," +
                        "" + row4["sum_outsaldo"].ToString().Trim() + ",'" + Points.DateOper.ToShortDateString().Trim() + "')";
#else
  sql = "INSERT into " + baseFin + ":gil_sums(nzp_pack_ls, num_ls, nzp_serv, nzp_supp, sum_oplat, dat_uchet) " +
                        "VALUES(" +
                        nzp_pack_ls_from + "," +
                        row4["num_ls"].ToString().Trim() + "," +
                        row4["nzp_serv"].ToString().Trim() + "," +
                        row4["nzp_supp"].ToString().Trim() + "," +
                        "" + row4["sum_outsaldo"].ToString().Trim() + ",'" + Points.DateOper.ToShortDateString().Trim() + "')";
#endif
                        ret = ExecSQL(conn_db, transaction, sql, true);
                        sum_total = sum_total + Convert.ToDecimal(row4["sum_outsaldo"]);

                      #if PG
  sql = "INSERT into " + fn_supplier_tab + "(nzp_pack_ls, num_ls,sum_prih, s_dolg, s_user, s_forw, nzp_serv, nzp_supp, dat_uchet) " +
                        "VALUES(" +
                        nzp_pack_ls_from + "," +
                        row4["num_ls"].ToString().Trim() + "," +
                        row4["sum_outsaldo"].ToString().Trim() + ",0," +
                        row4["sum_outsaldo"].ToString().Trim() + ",0," +
                        row4["nzp_serv"].ToString().Trim() + "," +
                        row4["nzp_supp"].ToString().Trim() + "," +
                        "'" + finder.dat_uchet.Trim().Trim() + "')";
#else
  sql = "INSERT into " + fn_supplier_tab + "(nzp_pack_ls, num_ls,sum_prih, s_dolg, s_user, s_forw, nzp_serv, nzp_supp, dat_uchet) " +
                        "VALUES(" +
                        nzp_pack_ls_from + "," +
                        row4["num_ls"].ToString().Trim() + "," +
                        row4["sum_outsaldo"].ToString().Trim() + ",0," +
                        row4["sum_outsaldo"].ToString().Trim() + ",0," +
                        row4["nzp_serv"].ToString().Trim() + "," +
                        row4["nzp_supp"].ToString().Trim() + "," +
                        "'" + finder.dat_uchet.Trim().Trim() + "')";
#endif
                        ret = ExecSQL(conn_db, transaction, sql, true);
                        if (!ret.result)
                        {
                            if (transaction != null) transaction.Rollback();
                            return ret;
                        }
                    }

                    dlt0 = Decimal.ToSingle(Math.Abs(sum_total - Convert.ToDecimal(row3["sum_overpay"])));
                    if (dlt0 > 0.001)
                    {
                        sql = "delete " + fn_supplier_tab + " where nzp_pack_ls = " + nzp_pack_ls_from;
                        ret = ExecSQL(conn_db, transaction, sql, true);
                        if (!ret.result)
                        {
                            if (transaction != null) transaction.Rollback();
                            return ret;
                        }

                      #if PG
  sql = "delete from  " + baseFin + ".gil_sums  where nzp_pack_ls = " + nzp_pack_ls_from;
#else
  sql = "delete from  " + baseFin + ":gil_sums  where nzp_pack_ls = " + nzp_pack_ls_from;
#endif
                        ret = ExecSQL(conn_db, transaction, sql, true);
                        if (!ret.result)
                        {
                            if (transaction != null) transaction.Rollback();
                            return ret;
                        }

                       #if PG
 sql = "delete from  " + baseFin + ".pack_ls  where nzp_pack_ls = " + nzp_pack_ls_from;
#else
 sql = "delete from  " + baseFin + ":pack_ls  where nzp_pack_ls = " + nzp_pack_ls_from;
#endif
                        ret = ExecSQL(conn_db, transaction, sql, true);
                        if (!ret.result)
                        {
                            if (transaction != null) transaction.Rollback();
                            return ret;
                        }
                        continue;
                    }

               #if PG
     sql = "update " + baseFin + ".pack_ls set g_sum_ls = " + sum_total + " where nzp_pack_ls = " + nzp_pack_ls_from;
#else
     sql = "update " + baseFin + ":pack_ls set g_sum_ls = " + sum_total + " where nzp_pack_ls = " + nzp_pack_ls_from;
#endif
                    ret = ExecSQL(conn_db, transaction, sql, true);
                    if (!ret.result)
                    {
                        if (transaction != null) transaction.Rollback();
                        return ret;
                    }

                    #endregion 1.


                    #region 2. Перенести переплаты на открытые лицевые счета
                  #if PG
  sql = "INSERT into " + baseFin + ".pack_ls(nzp_pack, prefix_ls,  num_ls, g_sum_ls, geton_ls," +
                    "sum_ls, dat_month, kod_sum, paysource, dat_vvod, anketa, inbasket, erc_code, unl, info_num, alg, dat_uchet) " +
                    "VALUES(" + nzp_pack_to + ",0," +
                    row3["num_ls_to"].ToString().Trim() + "," +
                    "(-1)*" + row3["sum_overpay"].ToString().Trim() + ",0,0,'" + finder.dat_pack.Trim() + "',33,0," +
                    "'" + Points.DateOper.ToShortDateString().Trim() + "',0,0," + erc_code + ",0," + count_total + ",0,null)";
#else
  sql = "INSERT into " + baseFin + ":pack_ls(nzp_pack, prefix_ls,  num_ls, g_sum_ls, geton_ls," +
                    "sum_ls, dat_month, kod_sum, paysource, dat_vvod, anketa, inbasket, erc_code, unl, info_num, alg, dat_uchet) " +
                    "VALUES(" + nzp_pack_to + ",0," +
                    row3["num_ls_to"].ToString().Trim() + "," +
                    "(-1)*" + row3["sum_overpay"].ToString().Trim() + ",0,0,'" + finder.dat_pack.Trim() + "',33,0," +
                    "'" + Points.DateOper.ToShortDateString().Trim() + "',0,0," + erc_code + ",0," + count_total + ",0,null)";
#endif

                    ret = ExecSQL(conn_db, transaction, sql, true);
                    nzp_pack_ls_to = GetSerialValue(conn_db, transaction);

                    #endregion 2.
                    sum_total = sum_total + Convert.ToDecimal(row3["sum_overpay"]);
                    sum_total_pack = sum_total_pack + Convert.ToDecimal(row3["sum_overpay"]);

                    if (transaction != null) transaction.Commit();


                }
                // Подвести итоги по пачке со СНЯТИЕМ
               #if PG
 sql = "update  " + baseFin + ".pack set flag = 21, sum_pack = (select sum(a.g_sum_ls) from " + baseFin + ".pack_ls a WHERE a.nzp_pack = " + baseFin + ".pack.nzp_pack), sum_rasp = (select sum(a.g_sum_ls) from " + baseFin + ".pack_ls a WHERE a.nzp_pack = " + baseFin + ".pack.nzp_pack), sum_nrasp = 0, " +
                    "count_kv = (select count(*) from " + baseFin + ".pack_ls a WHERE a.nzp_pack = " + baseFin + ".pack.nzp_pack) where nzp_pack =  " + nzp_pack_from;
#else
 sql = "update  " + baseFin + ":pack set flag = 21, sum_pack = (select sum(a.g_sum_ls) from " + baseFin + ":pack_ls a WHERE a.nzp_pack = " + baseFin + ":pack.nzp_pack), sum_rasp = (select sum(a.g_sum_ls) from " + baseFin + ":pack_ls a WHERE a.nzp_pack = " + baseFin + ":pack.nzp_pack), sum_nrasp = 0, " +
                    "count_kv = (select count(*) from " + baseFin + ":pack_ls a WHERE a.nzp_pack = " + baseFin + ":pack.nzp_pack) where nzp_pack =  " + nzp_pack_from;
#endif
                ret = ExecSQL(conn_db, null, sql, true);
                if (!ret.result)
                {
                    return ret;
                }

                // Подвести итоги по пачке со ПЕРЕПЛАТОЙ
                #if PG
sql = "update  " + baseFin + ".pack set flag = 11, sum_pack = (select sum(a.g_sum_ls) from " + baseFin + ".pack_ls a WHERE a.nzp_pack = " + baseFin + ".pack.nzp_pack), sum_rasp = 0, sum_nrasp = " + "(select sum(a.g_sum_ls) from " + baseFin + ".pack_ls a WHERE a.nzp_pack = " + baseFin + ".pack.nzp_pack), " +
                    "count_kv = (select count(*) from " + baseFin + ".pack_ls a WHERE a.nzp_pack = " + baseFin + ".pack.nzp_pack) where nzp_pack =  " + nzp_pack_to;
#else
sql = "update  " + baseFin + ":pack set flag = 11, sum_pack = (select sum(a.g_sum_ls) from " + baseFin + ":pack_ls a WHERE a.nzp_pack = " + baseFin + ":pack.nzp_pack), sum_rasp = 0, sum_nrasp = " + "(select sum(a.g_sum_ls) from " + baseFin + ":pack_ls a WHERE a.nzp_pack = " + baseFin + ":pack.nzp_pack), " +
                    "count_kv = (select count(*) from " + baseFin + ":pack_ls a WHERE a.nzp_pack = " + baseFin + ":pack.nzp_pack) where nzp_pack =  " + nzp_pack_to;
#endif
                ret = ExecSQL(conn_db, null, sql, true);
                if (!ret.result)
                {
                    return ret;
                }

                #region Распределить указанные пачки
                DbCalc db1 = new DbCalc();
                DbCalc db2 = new DbCalc();
                Returns ret2;


                db1.PackFonTasks(nzp_pack_to, FonTaskTypeIds.DistributePack, out ret2);  // Отдаем пачку с переплатами на распределение
                db1.Close();
                if (!ret2.result)
                {
                    ret.result = false;
                    ret.tag = -1;
                    ret.text = "Пачка по зачислению переплат создана, но не распределена." + (ret2.tag < 0 ? " " + ret2.text : "");

                }
                /*
                db2.PackFonTasks(nzp_pack_to, FonTaskTypeIds.DistributePack, out ret2);  // Отдаем пачку со снятием переплат на распределение
                db2.Close();
                if (!ret2.result)
                {
                    ret.result = false;
                    ret.tag = -1;
                    ret.text = ret.text.Trim()+ "Пачка по зачислению переплат создана, но не распределена." + (ret2.tag < 0 ? " " + ret2.text : "");                    
                }
                */

                PackXX packXX = new PackXX();
                packXX.paramcalc = new CalcTypes.ParamCalc(0, nzp_pack_from, "", Points.DateOper.Year, Points.DateOper.Month, Points.DateOper.Year, Points.DateOper.Month);
                packXX.paramcalc.b_pack = true;
                CreateSelKvar(conn_db, packXX, out ret);
                ret = CalcChargeXXUchetOplatForPack(conn_db, packXX);
                if (!ret.result)
                {
                    DropTempTablesPack(conn_db);
                    return ret;
                }
                #endregion
                #region Вызвать распределение пачки с оплатами


                #endregion

            }
            else
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "По указанному условию переплаты необнаружены";
            }
            //данные с переплатами в tXX_overpayment (XX - код пользователя), создавать пачку для записей, где mark = 1
            if (ret.result)
            {
                ret.tag = (-1) * par_pack;
                sum_total_pack = (-1) * sum_total_pack;
                ret.text = "Переплата на сумму " + sum_total_pack.ToString("N2") + " руб успешно перенесена. Подробную информацию смотрите в суперпачке № " + finder.num_pack + " от " + finder.dat_pack;                    
            }
            return ret;
        }


        // Заполнение информации для портала Небо для реестре №  nzp_nebo_reestr за период pDat_uchet_from gj DateTime pDat_uchet_to
        public bool FillSumOplForNebo(IDbConnection conn_db, int nzp_nebo_reestr, string pDat_uchet_from, string pDat_uchet_to, out Returns ret)
        {
            ret = Utils.InitReturns();
            string sql_text = "";

            int yy = Points.DateOper.Year;
            int mm = Points.DateOper.Month;

            string table_nebo = Points.Pref + "_fin_" + (yy - 2000).ToString("00") + ":" + "nebo_rfnsupp ";
            sql_text = "delete from "+table_nebo+" where nzp_nebo_reestr = " + nzp_nebo_reestr;
            ret = ExecSQL(conn_db, sql_text, true);          
            if (!ret.result)
            {
                return false;
            }


            sql_text = "drop table tmp_nebo_kvar";
            ret = ExecSQL(conn_db, sql_text, true);

            sql_text = "select typek, pkod, num_ls, nzp_dom, nzp_kvar from "+Points.Pref+"_data:kvar where typek=1 into temp tmp_nebo_kvar with no log";
            ret = ExecSQL(conn_db, sql_text, true);          

            ExecSQL(conn_db, " create index idx_tmp_nebo_kvar on  tmp_nebo_kvar(num_ls, nzp_dom);", true);
            ExecSQL(conn_db, " Update statistics for table tmp_nebo_kvar ", true);

            sql_text = "drop table tmp_nebo_kvar_3";
            ret = ExecSQL(conn_db, sql_text, true);

            sql_text = "select typek, pkod, num_ls, nzp_dom, nzp_kvar from "+Points.Pref+"_data:kvar where nvl(typek,1)=3  into temp tmp_nebo_kvar_3 with no log";
            ret = ExecSQL(conn_db, sql_text, true);          

            ExecSQL(conn_db, " create index idx_tmp_nebo_kvar_3 on  tmp_nebo_kvar_3(num_ls, nzp_dom);", true);
            ExecSQL(conn_db, " Update statistics for table tmp_nebo_kvar_3 ", true);

            string tab_supplier = "";
            foreach (_Point zap in Points.PointList)
            {
                 tab_supplier = zap.pref + "_charge_" + (Points.DateOper.Year - 2000).ToString("00") + ":fn_supplier" + Points.DateOper.Month.ToString("00");
                 sql_text=
                        "insert into "+table_nebo+"( nzp_nebo_reestr, nzp_dom, dat_uchet,  typek, nzp_supp, nzp_serv,  sum_prih)  "+
                        "select 1, nzp_dom, dat_uchet, 1, nzp_supp, nzp_serv, sum(sum_prih) "+
                        "from "+tab_supplier+" a, tmp_nebo_kvar b  "+
                        "where a.num_ls = b.num_ls and a.sum_prih <>0 " +
                        "and dat_uchet between  '" + pDat_uchet_from.Trim() + "' and '" + pDat_uchet_to.Trim() + "' " +
                        "group by 1,2,3,4,5,6;  ";
                 ret = ExecSQL(conn_db, sql_text, true);
                 sql_text=
                        "insert into " + table_nebo + "( nzp_nebo_reestr, nzp_dom, dat_uchet,  typek, nzp_supp, nzp_serv, num_ls,   pkod, sum_prih)  " +
                        "select '1' as nzp_nebo_reestr , b.nzp_dom, a.dat_uchet, '3' as typek, a.nzp_supp, a.nzp_serv, b.num_ls, b.pkod, sum(a.sum_prih) "+
                        "from " + tab_supplier + " a, tmp_nebo_kvar_3 b  " +
                        "where a.num_ls = b.num_ls and a.sum_prih <>0 " +
                        "and dat_uchet between  '" + pDat_uchet_from.Trim() + "' and '" + pDat_uchet_to.Trim() + "' " +
                        "group by 1,2,3,4,5,6,7,8 ";
                 ret = ExecSQL(conn_db,sql_text,  true);

            }

            sql_text = "drop table tmp_nebo_kvar";
            ret = ExecSQL(conn_db, sql_text, true);

            sql_text = "drop table tmp_nebo_kvar_3";
            ret = ExecSQL(conn_db, sql_text, true);

            if (!ret.result)
            {
                return false;
            }
            else   return true;


        }

        // Проверить количество
        public int GetCount(IDbConnection conn_db, string psqlText)
        {
            DataTable dt = ClassDBUtils.OpenSQL(psqlText, conn_db).GetData();
            DataRow row_;
            row_ = dt.Rows[0];
            if (row_["f1"] != DBNull.Value)
            {
                return Convert.ToInt32(row_["f1"]);    
            }
            return 0;
        }

        // Сохранить запись в журнал событий
        public int SaveEvent(int nzp_dict,IDbConnection conn_db, int nzp_user, int nzp, string note)
        {
            string sql_text = "insert into " + Points.Pref + "_data"+tableDelimiter+"sys_events(date_, nzp_user, nzp_dict_event, nzp, note) values (" + sCurDateTime + "," + nzp_user + "," + nzp_dict + "," + nzp + ",'" + note.Replace("'", "") + "') ";
            Returns ret = ExecSQL(conn_db, sql_text, true);

            if (!ret.result)
            {
                return 0;
            }
            else return GetSerialValue(conn_db, null); ;            
        }
        
        // Сохранить показания приборов учёта
        public bool SaveCountersValsFromPackls(IDbConnection conn_db, int nzp_user, CalcTypes.ParamCalc paramcalc, int nzp_pack_ls, out Returns ret)
        {
            List<int> list_pack_ls = new List<int>();

            if (nzp_pack_ls==0)
            { 
            string sqlText = " select distinct nzp_pack_ls from t_opl where kod_info>0";

            DataTable dt = ClassDBUtils.OpenSQL(sqlText, conn_db).GetData();
            foreach (DataRow rowPack_ls in dt.Rows)
            {
                if (rowPack_ls["nzp_pack_ls"] != DBNull.Value)
                {
                    list_pack_ls.Add(Convert.ToInt32(rowPack_ls["nzp_pack_ls"]));
                }
            }
            } else
            {
                list_pack_ls.Add(nzp_pack_ls);
            }

            DbCounter dbCounter = new DbCounter();
            ret = dbCounter.PuValsToCountersVals(conn_db, null, list_pack_ls, nzp_user, paramcalc.calc_yy, paramcalc.calc_mm);
            return ret.result;    
           
        }

        // Сохранить пометку fn_operday_dom_mc
        public bool Save_In_fn_operday_dom_mc(IDbConnection conn_db, int nzp_pack, int nzp_pack_ls, int nzp_dom, out Returns ret)
        {
            string fn_operday_dom;
#if PG
            fn_operday_dom = Points.Pref + "_fin_" + (Points.DateOper.Year - 2000).ToString("00") + ".fn_operday_dom_mc";
#else
            fn_operday_dom = Points.Pref + "_fin_" + (Points.DateOper.Year - 2000).ToString("00") + ":fn_operday_dom_mc";
#endif

            ret = ExecSQL(conn_db, " insert into  " + fn_operday_dom + "(nzp_dom, date_oper) values (" + nzp_dom + ",'" + Points.DateOper.ToShortDateString() + "'"+sConvToDate+" ) ", true);            
            return ret.result;

        }

    

        // Сохранить пометку fn_operday_dom_mc
        public bool Update_reval(IDbConnection conn_db, IDbTransaction transactionID, PackXX fn_packXX, bool flgUpdateTotal, out Returns ret)
        {

            string sql_text;
            ExecSQL(conn_db,transactionID, " Drop table ttt_paxx ", false);
#if PG
            sql_text = " Select nzp_payer, nzp_area, nzp_dom,nzp_serv, -1 as nzp_bank, 1 as kod, sum(sum_reval) as sum_reval " +
                " Into temp ttt_paxx  " +
                " From " + fn_packXX.fn_reval +
                " Where dat_oper = " + fn_packXX.paramcalc.dat_oper +
                " Group by 1,2,3,4,5 ";

    //ret = ExecSQL(conn_db,transactionID,sql_text, true);
#else
            sql_text = " Select nzp_payer, nzp_area, nzp_dom,nzp_serv, -1 as nzp_bank, 1 as kod, sum(sum_reval) as sum_reval " +
                " From " + fn_packXX.fn_reval +
                " Where dat_oper = " + fn_packXX.paramcalc.dat_oper + "  " +
                " Group by 1,2,3,4,5 " +
                " Into temp ttt_paxx With no log ";

            ret = ExecSQL(conn_db,transactionID, sql_text, true);    
           
            if (!ret.result)
            {
                return false;
            }
#endif
 
            int count=0;
#if PG 
#warning Проверить на postgree

            IntfResultType retres = ClassDBUtils.ExecSQL(sql_text, conn_db, ClassDBUtils.ExecMode.Log);
            ret = retres.GetReturnsType().GetReturns();
            count = retres.resultAffectedRows;
#else
            count = ClassDBUtils.GetAffectedRowsCount(conn_db, transactionID);
#endif
            if ( (!ret.result)||(count==0) )
            {

                ret = ExecSQL(conn_db, transactionID,
                    " Update " + fn_packXX.fn_distrib +
                    " Set sum_reval = 0 " +
                    " Where dat_oper = " + fn_packXX.paramcalc.dat_oper
                    , true);


            }


            ExecSQL(conn_db, transactionID, " Create unique index ix1_ttt_paxx on ttt_paxx (nzp_payer, nzp_area, nzp_dom, nzp_serv, nzp_bank) ", true);
           #if PG
 ExecSQL(conn_db,transactionID, " analyze ttt_paxx ", true);
#else
            ExecSQL(conn_db,transactionID, " Update statistics for table ttt_paxx ", true);
#endif

 //DataTable drr2 = ViewTbl(conn_db, " select * from ttt_paxx  ");
            sql_text= " Update ttt_paxx " +
                " Set kod = 0 " +
                " Where 0 < ( Select count(*) From " + fn_packXX.fn_distrib + " a " +
                            " Where a.nzp_payer= ttt_paxx.nzp_payer " +
                            "   and a.nzp_area = ttt_paxx.nzp_area " +
                            "   and a.nzp_dom = ttt_paxx.nzp_dom " +
                            "   and a.nzp_serv = ttt_paxx.nzp_serv " +
                            "   and a.nzp_bank = ttt_paxx.nzp_bank " +
                            "   and a.dat_oper = " + fn_packXX.paramcalc.dat_oper +
                            " ) ";

            ret = ExecSQL(conn_db, transactionID,sql_text, true);
            if (!ret.result)
            {
                return false;
            }

            ExecSQL(conn_db, transactionID, " Create index ix2_ttt_paxx on ttt_paxx (kod) ", true);
           #if PG
 ExecSQL(conn_db, transactionID, " analyze ttt_paxx ", true);
#else
            ExecSQL(conn_db,transactionID, " Update statistics for table ttt_paxx ", true);
#endif
 //DataTable drr2 = ViewTbl(conn_db, " select * from ttt_paxx  ");
            sql_text=" Insert into " + fn_packXX.fn_distrib + " ( nzp_payer,nzp_area, nzp_dom,nzp_serv,nzp_bank,dat_oper ) " +
                " Select nzp_payer,nzp_area, nzp_dom,nzp_serv,nzp_bank, " + fn_packXX.paramcalc.dat_oper +
                " From ttt_paxx Where kod = 1 ";
            ret = ExecSQL(conn_db, transactionID,sql_text, true);

            if (!ret.result)
            {
                return false;
            }
            sql_text =
                " Update " + fn_packXX.fn_distrib +
                " Set sum_reval = ( " +
                            " Select sum(sum_reval) From ttt_paxx a " +
                            " Where a.nzp_payer= " + fn_packXX.fn_distrib + ".nzp_payer " +
                            "   and a.nzp_area = " + fn_packXX.fn_distrib + ".nzp_area " +
                            "   and a.nzp_dom = " + fn_packXX.fn_distrib + ".nzp_dom " +
                            "   and a.nzp_serv = " + fn_packXX.fn_distrib + ".nzp_serv " +
                            "   and a.nzp_bank = " + fn_packXX.fn_distrib + ".nzp_bank " +
                            " ) " +
                " Where dat_oper = " + fn_packXX.paramcalc.dat_oper + 
                "   and 0 < ( Select count(*) From ttt_paxx a " +
                            " Where a.nzp_payer= " + fn_packXX.fn_distrib + ".nzp_payer " +
                            "   and a.nzp_area = " + fn_packXX.fn_distrib + ".nzp_area " +
                            "   and a.nzp_dom = " + fn_packXX.fn_distrib + ".nzp_dom " +
                            "   and a.nzp_serv = " + fn_packXX.fn_distrib + ".nzp_serv " +
                            "   and a.nzp_bank = " + fn_packXX.fn_distrib + ".nzp_bank " +
                            " ) ";

            ret = ExecSQL(conn_db,transactionID, sql_text, true);
            if (!ret.result)
            {
                return false;
            }

            ExecSQL(conn_db, transactionID," Drop table ttt_paxx ", false);

            //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
            //расчет итогового сальдо
            //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
            if (flgUpdateTotal)
            { 
            ret = ExecSQL(conn_db,transactionID,
                " Update " + fn_packXX.fn_distrib +
                " Set sum_out = sum_in + sum_rasp - sum_ud + sum_naud + sum_reval - sum_send " +
                "   ,sum_charge = sum_rasp - sum_ud + sum_naud + sum_reval " +
                " Where dat_oper = " + fn_packXX.paramcalc.dat_oper 
                , true);
            if (!ret.result)
            {
                return false;
            }
            }

            return true;
         }
     
    }
}