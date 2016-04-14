using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Globalization;
using FastReport.FastQueryBuilder;
using Microsoft.Office.Interop.Excel;
using Newtonsoft.Json;
using STCLINE.KP50.Global;
using STCLINE.KP50.IFMX.Kernel.source.CommonType;
using STCLINE.KP50.Interfaces;
using STCLINE.KP50.DataBase;
using System.Data;
using Constants = STCLINE.KP50.Global.Constants;
using DataTable = System.Data.DataTable;
using Points = STCLINE.KP50.Interfaces.Points;

namespace STCLINE.KP50.DataBase
{
    //здась находятся классы для распределения оплат
    public partial class DbCalcPack : DataBaseHead
    {
        // !!! Добавил Марат 17.12.2012 
        private bool isDebug = false;
            // Признак отладки (формируются постоянные таблицы для распределения t_selkvar, t_opl, t_iopl, t_charge)

        private bool isUseSupplier = true; // Признак использования новой схемы учёта поступелений по договорам



        public bool isAutoDistribPaXX = false;
            // Распределеять автоматически сальдо перечисление при распределении оплат        

        private string sqlText = ""; // Вспомогательная переменна для динамической сборки запросов
        private StringBuilder sql;
#if PG
        private string strWherePackLsIsNotDistrib =
            "  and dat_uchet is null and (coalesce(p.alg,'0') = '0'  or inbasket=1) ";
            // Признак того, что оплата не распределена и готова к распределению
#else
                string strWherePackLsIsNotDistrib = "  and dat_uchet is null and (NVL(alg,0) =0  or inbasket=1) ";  // Признак того, что оплата не распределена и готова к распределению
#endif
#if PG
        private string strWherePackLsIsDistrib =
            "  and p.dat_uchet is not null and p.inbasket<>1 and coalesce(p.alg,'0') <>'0' ";
            // Признак того, что оплата распределена
#else
                string strWherePackLsIsDistrib = "  and p.dat_uchet is not null and p.inbasket<>1 and NVL(p.alg,0) <>0 ";  // Признак того, что оплата распределена
#endif
        private string strKodSumForCharge_MM = "23,33,41,40,49,50,35"; // Ежемесячные квитанции //???
        private string strKodSumForCharge_X = "57,55,83,93,94"; // Специальные формы квитанций

        private string field_for_etalon = "sum_charge";
        private string field_for_etalon_descr = "Начислено к оплате";

        private bool isDistributionForInSaldo = false; // Распределение по исходящему сальдо
        private int pack_type = 10; // Тип платежа  (pack_type: 10- средства РЦ,  20 - чужие средства)
        //int nzp_supp = 0;   // Код поставщика для чужих площадей    
        //---------------------------------------------------


        //--------------------------------------------------------------------------------
        private void DropTempTablesPack(IDbConnection conn_db)
            //--------------------------------------------------------------------------------
        {
            ExecSQL(conn_db, " Drop table t_selkvar ", false);
            ExecSQL(conn_db, " Drop table t_charge ", false);
            ExecSQL(conn_db, " Drop table t_itog ", false);
            ExecSQL(conn_db, " Drop table t_ostatok ", false);
            ExecSQL(conn_db, " Drop table t_opl ", false);
            ExecSQL(conn_db, " Drop table t_gil_sums ", false);
            ExecSQL(conn_db, " Drop table t_iopl ", false);
            ExecSQL(conn_db, " Drop table t_iopl_102 ", false);
        }

        /// <summary>
        /// Добавляет сообщение об операциях с пачкой оплат или оплатой
        /// </summary>
        /// <param name="conn_db"></param>
        /// <param name="packXX"></param>
        /// <param name="msg"></param>
        /// <param name="err"></param>
        /// <param name="ret"></param>
        public void MessageInPackLog(IDbConnection conn_db, CalcTypes.PackXX packXX, string msg, bool err,
            out Returns ret)
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
        private void CreatePackLog(IDbConnection conn_db2, CalcTypes.PackXX packXX, out Returns ret)
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
            ret = ExecSQL(conn_db,
                " set search_path to '" + Points.Pref + "_kernel " + Points.Pref + "_fin_" +
                (packXX.paramcalc.calc_yy - 2000).ToString("00") + "'", true);
#else
                ret = ExecSQL(conn_db, " Database " + Points.Pref + "_fin_" + (packXX.paramcalc.calc_yy - 2000).ToString("00"), true);
#endif

#if PG
            ret = ExecSQL(conn_db,
                " Create table " +
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
                                   + "ix2_" + packXX.pack_log_tab + " on " + packXX.pack_log_tab +
                                   " (nzp_pack, nzp_wp) ", true);
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
                                   + "ix3_" + packXX.pack_log_tab + " on " + packXX.pack_log_tab + " (nzp_pack_ls) ",
                true);
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
                                   + "ix4_" + packXX.pack_log_tab + " on " + packXX.pack_log_tab +
                                   " (dat_oper,tip_log) ", true);
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
        public bool CalcPackXX(CalcTypes.ParamCalc paramcalc, int fin_year, bool isManualDistribution, out Returns ret,
            DbCalcQueueClient.SetTaskProgressDelegate setTaskProgress)
        {
            string conn_kernel = Points.GetConnByPref(paramcalc.pref);
            IDbConnection conn_db = GetConnection(conn_kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result)
            {
                return false;
            }

            bool result = CalcPackXX(conn_db, paramcalc, fin_year, isManualDistribution, out ret, setTaskProgress);

            conn_db.Close();

            return result;
        }

        public bool CalcPackXX(IDbConnection conn_db, CalcTypes.ParamCalc paramcalc, int fin_year,
            bool isManualDistribution, out Returns ret)
        {
            return CalcPackXX(conn_db, paramcalc, fin_year, isManualDistribution, out ret, null);
        }

        public bool CalcPackXX(IDbConnection conn_db, CalcTypes.ParamCalc paramcalc, int fin_year,
            bool isManualDistribution, out Returns ret, DbCalcQueueClient.SetTaskProgressDelegate setTaskProgress)
            //-----------------------------------------------------------------------------
        {
            return CalcPackXX(conn_db, paramcalc, 0, fin_year, true, isManualDistribution, out ret, setTaskProgress);
        }


        //-----------------------------------------------------------------------------
        public bool CreateSelKvar(IDbConnection conn_db, CalcTypes.PackXX packXX, out Returns ret)
            //-----------------------------------------------------------------------------
        {
            return CreateSelKvar(conn_db, packXX, false, out ret);
        }

        //-----------------------------------------------------------------------------
        public bool CreateSelKvar(IDbConnection conn_db, CalcTypes.PackXX packXX, bool cur_dat_uchet, out Returns ret)
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
                      "   g_sum_ls    numeric(14,2) default 0, " +
                      "   nzp_supp integer default 0, " +
                      "   nzp_payer integer default 0, " +
                      "   pkod numeric(18,0) default 0  " +

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
                "   g_sum_ls    decimal(14,2) default 0, " +
                "   nzp_supp integer default 0, "+
                "   nzp_payer integer default 0, " +
                "   pkod decimal(18,0) default 0  " +
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
                MessageInPackLog(conn_db, packXX, "Ошибка выборки 1.1", true, out r);
                return false;
            }

            string s_dat_uchet = " "; //0 - по умолчанию

            if (cur_dat_uchet)
                s_dat_uchet = " and p.dat_uchet " + packXX.where_dat_oper; //в текущем операционном дне или месяце

#if PG
            sqlText =
                " Insert into t_selkvar (nzp_kvar, num_ls, nzp_wp, nzp_dom, nzp_area, nzp_geu, nzp_pack, nzp_pack_ls, kod_sum, dat_uchet, dat_month, dat_vvod, g_sum_ls, id_bill,nzp_supp,nzp_payer)  " +
                " Select k.nzp_kvar, k.num_ls, " + packXX.paramcalc.nzp_wp +
                ", k.nzp_dom, k.nzp_area, k.nzp_geu, p.nzp_pack, p.nzp_pack_ls, p.kod_sum, p.dat_uchet, p.dat_month, p.dat_vvod, p.g_sum_ls, p.id_bill," +
                " p.nzp_supp,p.nzp_payer  " +
                " From " + packXX.paramcalc.pref + "_data.kvar k, " + packXX.pack_ls + " p " +
                " Where k.num_ls = p.num_ls " +
                s_dat_uchet + //фильтрация по dat_uchet
                "   and k." + packXX.paramcalc.where_z +
                "   and p." + packXX.where_pack +
                "   and p." + packXX.where_pack_ls;
#else
                sqlText =
                    " Insert into t_selkvar (nzp_kvar, num_ls, nzp_wp, nzp_dom, nzp_area, nzp_geu, nzp_pack, nzp_pack_ls, kod_sum, dat_uchet, dat_month, dat_vvod, g_sum_ls, id_bill, nzp_supp,nzp_payer, pkod)  " +
                    " Select k.nzp_kvar, k.num_ls, " + packXX .paramcalc.nzp_wp+ ", k.nzp_dom, k.nzp_area, k.nzp_geu, p.nzp_pack, p.nzp_pack_ls, p.kod_sum, p.dat_uchet, p.dat_month, p.dat_vvod, p.g_sum_ls, p.id_bill,p.nzp_supp,p.nzp_payer, p.pkod  " +
                    " From " + packXX.paramcalc.pref + "_data:kvar k, " + packXX.pack_ls + " p " +
                    " Where k.num_ls = p.num_ls " +
                        s_dat_uchet + //фильтрация по dat_uchet
                    "   and k." + packXX.paramcalc.where_z +
                    "   and p." + packXX.where_pack +
                    "   and p." + packXX.where_pack_ls;
#endif

            if ((packXX.nzp_pack > 0) & (pack_type == 20))
                sqlText = sqlText + " and p.kod_sum in (40,49,50,35) "; //???
            ret = ExecSQL(conn_db, sqlText, true);
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

        private Returns DeletePrevDistribution(IDbConnection connDb, string pref, int year, int month, int nzpPackLs = 0)
        {
            Returns ret = Utils.InitReturns();
            // Предварительно очистить ранее распределённые оплаты по выбранным квитанциям
            if (pref != Points.Pref)
            {
                var swhere = " nzp_pack_ls in (select k.nzp_pack_ls From t_selkvar k ) ";
                if (nzpPackLs > 0) swhere = " nzp_pack_ls = " + nzpPackLs;
                sqlText =
                    " delete from  " + pref + "_charge_" + (year % 100).ToString("00") + tableDelimiter + "from_supplier" +
                    " where " + swhere;
                ret = ClassDBUtils.ExecSQL(sqlText, connDb, ClassDBUtils.ExecMode.Log).GetReturnsType().GetReturns();
                if (!ret.result)
                {
                    ret.tag = -1;
                    ret.text = "Ошибка при удалении ранее распределённых оплат";
                    return ret;
                }

                sqlText =
                    " delete from  " + pref + "_charge_" + (year % 100).ToString("00") + tableDelimiter + "fn_supplier" + month.ToString("00") +
                    " where " + swhere;
                ret = ClassDBUtils.ExecSQL(sqlText, connDb, ClassDBUtils.ExecMode.Log).GetReturnsType().GetReturns();
                if (!ret.result)
                {
                    ret.tag = -1;
                    ret.text = "Ошибка при удалении ранее распределённых оплат";
                    return ret;
                }
            }
            return ret;
        }

        public bool CalcPackXX(IDbConnection conn_db, CalcTypes.ParamCalc paramcalc, int nzp_pack_ls, int fin_year,
            bool checkPackDistrb, bool isManualDistribution, out Returns ret,
            DbCalcQueueClient.SetTaskProgressDelegate setTaskProgress, bool isCurrentMonth = false)
        {
            IntfResultType ret21;
            int cnt = 0;
            if ((paramcalc.nzp_pack == 0) && (nzp_pack_ls == 0))
            {
                if (Constants.Trace)
                    Utility.ClassLog.WriteLog("Отказано в распределении из-за отсутствия кодов пачки и кода квитанции ");
                ret = new Returns(false, "При распределении не указана оплата (оплаты)", -1);
                return false;
            }
            if (Constants.Trace) Utility.ClassLog.WriteLog("Старт функции CalcPackXX для пачки " + paramcalc.nzp_pack);

            //isManualDistribution = true;
            //Constants.Trace = false;
#if PG
            ret = ExecSQL(conn_db, " set search_path to '" + Points.Pref + "_kernel'", true);
#endif
            DropTempTablesPack(conn_db);


            if (isDebug)
            {
#if PG
#else
      
                ret = ExecSQL(conn_db, "DATABASE  " + Points.Pref + "_kernel", true);
#endif
            }

            if (Constants.Trace)
            {
                Utility.ClassLog.WriteLog("Включён режим отладки");
            }

            //Points.packDistributionParameters.strategy = PackDistributionParameters.Strategies.NoPriority;
            /*
            if ((Points.IsSmr) || (Points.Region == Regions.Region.Belgorodskaya_obl)) //признак Самары
            {
                Points.packDistributionParameters.strategy = PackDistributionParameters.Strategies.Samara;
            }

            if (Points.packDistributionParameters.strategy == PackDistributionParameters.Strategies.Samara)
            {
                isDistributionForInSaldo = true;
            };
            */
            if ((Points.packDistributionParameters.distributionMethod ==
                 PackDistributionParameters.PaymentDistributionMethods.CurrentMonthPositiveSumInsaldo) ||
                (Points.packDistributionParameters.distributionMethod ==
                 PackDistributionParameters.PaymentDistributionMethods.CurrentMonthSumInsaldo))
            {
                isDistributionForInSaldo = true;
            }

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
            int count_err = 0;
            int count_special = 0;
            DataTable dt;
            DataRow row;
            string s_okr = "0";

            int nzp_user = paramcalc.nzp_user; // !!! Потом определить код пользователя 

            ret = Utils.InitReturns();
            Returns r = Utils.InitReturns();

            CalcTypes.PackXX packXX = new CalcTypes.PackXX(paramcalc, nzp_pack_ls, true);


            CreatePackLog(conn_db, packXX, out ret);
            if (!ret.result)
            {
                return false;
            }

            bool isCurrFinPeriod = Points.DateOper.Year == fin_year;
            if (!isCurrFinPeriod)
            {
                MessageInPackLog(conn_db, packXX, "Пачка находится в закрытом финансовом периоде", true, out r);
                if (Constants.Trace)
                    Utility.ClassLog.WriteLog("Отказано в распределении. Пачка находится в закрытом финансовом периоде");
                ret = new Returns(false, "Отказано в распределении. Пачка находится в закрытом финансовом периоде", -1);
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
                b = true;

                ret = ExecRead(conn_db, out reader,
                    " Select nzp_pack From " + packXX.pack +
                    " Where nzp_pack = " + packXX.nzp_pack
                    , true);
                if (!ret.result)
                {
                    return false;
                }

                try
                {
                    if (reader.Read())
                    {
                        b = false;
                    }
                    else
                    {
                        ret.text = "Пачка распределена или не найдена (код пачки=" + packXX.nzp_pack + ") !";
                        var packLs = new Pack_ls
                        {
                            nzp_pack = packXX.nzp_pack
                        };

                        //  bool isCurrFinPeriod = IsCurrFinPeriod(conn_db, null, packLs, out r);
                        if (!r.result) MessageInPackLog(conn_db, packXX, "Пачка не найдена", true, out r);
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
            }

            string distr_month;
            string distr_month2;

            var dat_month_local = Points.GetCalcMonth(new CalcMonthParams(paramcalc.pref));
            
            #region Запретить расщепление, если месяц локального банка отстает от текущего операционного дня
            if (new DateTime(Points.DateOper.Year, Points.DateOper.Month, 1) > new DateTime(dat_month_local.year_, dat_month_local.month_, 1) )
            {
                MessageInPackLog(conn_db, packXX, "Отказано в распределении, т.к. расчетный месяц раньше финансового месяца", true, out r);
                if (Constants.Trace)
                    Utility.ClassLog.WriteLog("Отказано в распределении, т.к. расчетный месяц раньше финансового месяца");
                ret = new Returns(false, "Отказано в распределении, т.к. расчетный месяц раньше финансового месяца", -1);
                return false;
            }

            #endregion
            RecordMonth dat_month_local_prev;

            dat_month_local_prev.year_ = dat_month_local.year_;
            dat_month_local_prev.month_ = dat_month_local.month_;

            dat_month_local_prev.month_ = dat_month_local.month_ - 1;
            if (dat_month_local_prev.month_ == 0)
            {
                dat_month_local_prev.year_ = dat_month_local.year_ - 1;
                dat_month_local_prev.month_ = 12;
            }

            string baseName;
            string baseName_t;

            switch (Points.packDistributionParameters.distributionMethod)
            {
                case PackDistributionParameters.PaymentDistributionMethods.LastMonthPositiveSumOutsaldo:
                    field_for_etalon = "sum_outsaldo";
                    field_for_etalon_descr = "Положительная часть исходящего сальдо {0} месяца";
                    break;
                case PackDistributionParameters.PaymentDistributionMethods.CurrentMonthSumInsaldo:
                    field_for_etalon = "sum_outsaldo";
                    field_for_etalon_descr = "Исходящее сальдо {0} месяца";
                    break;
                case PackDistributionParameters.PaymentDistributionMethods.LastMonthSumCharge:
                    field_for_etalon = "sum_charge";
                    field_for_etalon_descr = "Начислено к оплате {0} месяца";
                    break;
                case PackDistributionParameters.PaymentDistributionMethods.LastMonthPositiveSumCharge:
                    field_for_etalon = "sum_charge";
                    field_for_etalon_descr = "Положительная часть начислено к оплате {0} месяца";
                    break;
            }
            field_for_etalon_descr = field_for_etalon_descr.Replace("{0}", isCurrentMonth ? "текущего" : "прошлого");


            if (isDistributionForInSaldo)
            {
                field_for_etalon = "sum_insaldo";
                field_for_etalon_descr = "Входящее сальдо";
                baseName = paramcalc.pref + "_charge_" + (dat_month_local.year_ - 2000).ToString("00") + tableDelimiter +
                           "charge_" + dat_month_local.month_.ToString("00");
                baseName_t = paramcalc.pref + "_charge_" + (dat_month_local.year_ - 2000).ToString("00") +
                             tableDelimiter + "charge_" + dat_month_local.month_.ToString("00");
                distr_month = "01." + dat_month_local.month_.ToString("00") + "." +
                              (dat_month_local.year_).ToString("00");
                distr_month2 = distr_month;

                //distr_month = "01." + paramcalc.calc_mm.ToString("00") + "." + (paramcalc.calc_yy ).ToString("00");
                //distr_month2 = "01." + paramcalc.calc_mm.ToString("00") + "." + (paramcalc.calc_yy ).ToString("00");

            }
            else
            {
                baseName = paramcalc.pref + "_charge_" + (dat_month_local_prev.year_ - 2000).ToString("00") +
                           tableDelimiter + "charge_" + dat_month_local_prev.month_.ToString("00");
                baseName_t = paramcalc.pref + "_charge_" + (dat_month_local.year_ - 2000).ToString("00") +
                             tableDelimiter + "charge_" + dat_month_local.month_.ToString("00") + "";
                //baseName_t = paramcalc.pref + "_charge_" + (dat_month_local_prev.year_ - 2000).ToString("00") + tableDelimiter + "charge_" + dat_month_local_prev.month_.ToString("00") + "";
                distr_month = "01." + dat_month_local_prev.month_.ToString("00") + "." +
                              (dat_month_local_prev.year_).ToString("00");

                distr_month2 = distr_month;

                //distr_month = "01." + paramcalc.prev_calc_mm.ToString("00") + "." + (paramcalc.prev_calc_yy ).ToString("00");
                //distr_month2 = "01." + paramcalc.calc_mm.ToString("00") + "." + (paramcalc.calc_yy ).ToString("00");

            }

            //isCurrentMonth = true;
            if (isCurrentMonth) // Если вызывается функция с параметром распределения по текущему месяцу
            {
                distr_month = "01." + dat_month_local.month_.ToString("00") + "." +
                              (dat_month_local.year_).ToString("00");
                distr_month2 = distr_month;
                baseName = paramcalc.pref + "_charge_" + (dat_month_local.year_ - 2000).ToString("00") + tableDelimiter +
                           "charge_" + dat_month_local.month_.ToString("00");
                baseName_t = paramcalc.pref + "_charge_" + (dat_month_local.year_ - 2000).ToString("00") +
                             tableDelimiter + "charge_" + dat_month_local.month_.ToString("00");

            }

      

            MessageInPackLog(conn_db, packXX, "Сальдовый месяц для распределения: " + distr_month, false, out r);

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
                if (pack_type == 20)
                {
                    packXX.fn_supplier = packXX.fn_supplier.Replace(packXX.fn_supplier_tab, "from_supplier");
                }
            }
            
            //if (row_pack_type["nzp_supp"] != DBNull.Value)
            //{
            //    nzp_supp = Convert.ToInt32(row_pack_type["nzp_supp"]);
            //}
            //else
            //{
            //    nzp_supp = 0;
            //}
            // ---------------- Если ручное распределенеи оплаты (распределение уже было произведено в gil_sums) ---------------------------------------------------------
            //проверить уточнена ли оплата по услугам и можно распределить ее вручную

            string tableGilsums = Points.Pref + "_fin_" + (paramcalc.calc_yy - 2000).ToString("00") + paramcalc.ol_srv +
                                  tableDelimiter + "gil_sums a";

            if (!isManualDistribution && (packXX.nzp_pack_ls > 0))
            {
                try
                {
                    sqlText = "select sum(a.sum_oplat) sum_oplat from " + tableGilsums + "  where a.nzp_pack_ls = " +
                              packXX.nzp_pack_ls.ToString();
                    DataTable dtpack_ls1 = ClassDBUtils.OpenSQL(sqlText, conn_db).GetData();
                    DataRow row_pack_ls1;
                    row_pack_ls1 = dtpack_ls1.Rows[0];
                    decimal sum_oplat = 0;
                    if (row_pack_ls1["sum_oplat"] != DBNull.Value)
                        sum_oplat = Convert.ToDecimal(row_pack_ls1["sum_oplat"]);
                    dtpack_ls1 = null;
                    row_pack_ls1 = null;
                    sqlText = "select g_sum_ls from " + packXX.pack_ls + " where nzp_pack_ls = " +
                              packXX.nzp_pack_ls.ToString();
                    dtpack_ls1 = ClassDBUtils.OpenSQL(sqlText, conn_db).GetData();
                    row_pack_ls1 = dtpack_ls1.Rows[0];
                    decimal sum = 0;
                    if (row_pack_ls1["g_sum_ls"] != DBNull.Value) sum = Convert.ToDecimal(row_pack_ls1["g_sum_ls"]);
                    if (sum == sum_oplat) isManualDistribution = true;
                }
                catch
                {
                }
            }

            if ((isManualDistribution) && (packXX.nzp_pack_ls > 0))
            {
                //----------------------- СОХРАНЕНИЕ -----------------------
                sqlText = " Select  p.kod_sum,  p.dat_vvod, p.dat_month, p.g_sum_ls, k.nzp_dom " +
                          " From " + packXX.pack_ls + " p, " + Points.Pref + "_data" + DBManager.tableDelimiter +
                          "kvar k " +
                          " Where p.nzp_pack_ls =  " + packXX.nzp_pack_ls.ToString() + " and p.num_ls = k.num_ls";

                string dat_vvod = "";
                int kod_sum = 0;
                decimal g_sum_ls = 0;
                int nzp_dom = 0;

                DataTable dtpack_ls = ClassDBUtils.OpenSQL(sqlText, conn_db).GetData();
                DataRow row_pack_ls;
                row_pack_ls = dtpack_ls.Rows[0];

                if (row_pack_ls["kod_sum"] != null)
                {
                    #region  Загрузить  исходные данные по квитанции

                    kod_sum = Convert.ToInt32(row_pack_ls["kod_sum"]);
                    dat_vvod = Convert.ToDateTime(row_pack_ls["dat_vvod"]).ToShortDateString();
                    g_sum_ls = Convert.ToDecimal(row_pack_ls["g_sum_ls"]);
                    nzp_dom = Convert.ToInt32(row_pack_ls["nzp_dom"]);
                    ;

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

                    if (pack_type == 20) // Чужие средства
                    {
                        string tableSupplier = paramcalc.pref + "_charge_" +
                                               (paramcalc.calc_yy.ToString("yyyy").Substring(2, 2)) + tableDelimiter +
                                               "from_supplier";
                    }
                    else
                    {
                        //string tableSupplier = paramcalc.pref + "_charge_" + (paramcalc.calc_yy.ToString("yyyy").Substring(2, 2)) + tableDelimiter + "fn_supplier" + paramcalc.calc_mm.ToString("00");
                        string tableSupplier = paramcalc.pref + "_charge_" +
                                               (Points.DateOper.Year.ToString("yyyy").Substring(2, 2)) + tableDelimiter +
                                               "fn_supplier" + Points.DateOper.Month.ToString("00");
                    }


                    #endregion Загрузить  исходные данные по квитанции
                    // Предварительно очистить ранее распределённые оплаты по выбранным квитанциям
                    ret = DeletePrevDistribution(conn_db, paramcalc.pref, paramcalc.calc_yy, paramcalc.calc_mm, packXX.nzp_pack_ls);
                    if (!ret.result)
                    {
                        DropTempTablesPack(conn_db);
                        MessageInPackLog(conn_db, packXX, (ret.tag == -1) ? ret.text : "Ошибка распределения 1.4.1.2", true, out r);
                        return false;
                    }
                
                    if (pack_type == 20) // Чужие средства
                    {
                        sqlText =
                            " Insert into " + packXX.fn_supplier +
                            " (  num_ls, nzp_pack_ls, nzp_serv, nzp_supp, sum_prih, kod_sum, dat_month, dat_prih, dat_uchet, calc_month) " +
                            " Select  a.num_ls, a.nzp_pack_ls, a.nzp_serv, a.nzp_supp, a.sum_oplat," + kod_sum +
                            ",a.dat_month, cast ('" + dat_vvod + "' as DATE) , CAST( " + packXX.paramcalc.dat_oper +
                            " as DATE), cast('" + (new DateTime(dat_month_local.year_, dat_month_local.month_, 1)).ToShortDateString() + "' as DATE)" +
                            " From  " + tableGilsums +
                            " Where  a.nzp_pack_ls = " + packXX.nzp_pack_ls.ToString() + " and sum_oplat <>0 ";
                    }
                    else
                    {
                        sqlText =
                            " Insert into " + packXX.fn_supplier +
                            " (  num_ls, nzp_pack_ls, nzp_serv, nzp_supp, sum_prih, kod_sum, dat_month, dat_prih, dat_uchet, s_dolg, s_user, s_forw, calc_month ) " +
                            " Select distinct  a.num_ls, a.nzp_pack_ls, a.nzp_serv, a.nzp_supp, a.sum_oplat," + kod_sum +
                            ",a.dat_month, cast('" + dat_vvod + "' as date), cast(" + packXX.paramcalc.dat_oper +
                            " as date), 0, a.sum_oplat, 0, cast('" + (new DateTime(dat_month_local.year_, dat_month_local.month_, 1)).ToShortDateString() + "' as DATE) " +
                            " From  " + tableGilsums +
                            " Where  a.nzp_pack_ls = " + packXX.nzp_pack_ls.ToString() + " and sum_oplat <>0 ";
                    }

                    ret =
                        ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log).GetReturnsType().GetReturns();
                    if (!ret.result)
                    {
                        ret.result = false;
                        MonitorLog.WriteLog(ret.text, MonitorLog.typelog.Error, 1, 222, true);
                        MessageInPackLog(conn_db, packXX, "Ошибка сохранения ручного распределения 2.7", true, out r);
                        return false;
                    }

                    #region Проверить был ли полностью выполнено ручное распределение

                    DataTable dtgil_sums =
                        ClassDBUtils.OpenSQL(
                            "select sum(sum_oplat) as sum_oplat, MIN(dat_month) as distr_month from " + tableGilsums +
                            " where nzp_pack_ls =  " + packXX.nzp_pack_ls, conn_db).GetData();
                    DataRow row_gil_sums;
                    decimal sum_oplat_total = 0;
                    row_gil_sums = dtgil_sums.Rows[0];
                    if (row_gil_sums["sum_oplat"] != null)
                    {
                        if (row_gil_sums["sum_oplat"] != DBNull.Value)
                            sum_oplat_total = Convert.ToDecimal(row_gil_sums["sum_oplat"]);
                        if (sum_oplat_total > g_sum_ls)
                        {
                            MessageInPackLog(conn_db, packXX, "Ошибка сохранения ручного распределения 2.7", true, out r);
                            // Нет распределённых оплат
                            ret.text = "Распределение не было произведено. Сумма ручного распределения " +
                                       sum_oplat_total + " руб превосходит сумму оплаты " + g_sum_ls;
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
                    }
                    else
                    {
                        sum_oplat_total = 0;
                    }
                    float dlt0 = Decimal.ToSingle(Math.Abs(sum_oplat_total - g_sum_ls));
                    if (dlt0 > 0.001)
                    {
                        MessageInPackLog(conn_db, packXX, "Ошибка сохранения ручного распределения 2.7", true, out r);
                        // Нет распределённых оплат
                        ret.text = "Распределение не было произведено. Сумма ручного распределения " + sum_oplat_total +
                                   " руб не соответствует оплате " + g_sum_ls;
                        ret.tag = -1;
                        ret.result = false;
                        return true;

                    }

                    #endregion Проверить был ли полностью выполнено ручное распределение

                    sqlText =
                        " Update " + packXX.pack_ls +
                        " Set dat_uchet = " + packXX.paramcalc.dat_oper +
                        ", calc_month = cast('" + (new DateTime(dat_month_local.year_, dat_month_local.month_, 1)).ToShortDateString() + "' as DATE) " +
#if PG
                            ", date_distr = now(),  inbasket =0, alg = 5, nzp_user = " + nzp_user +
#else
                            ", date_distr = current,  inbasket =0, alg = 5, nzp_user = " + nzp_user +
#endif
                            ", distr_month = '" + distr_month + "'" +
                        " Where nzp_pack_ls = " + packXX.nzp_pack_ls.ToString();
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
                        ret.result = false;
                        ret.text = "Распределение не было произведено";
                        ret.tag = -1;
                        return true;
                    }
                    else
                    {
                        //сразу запустить расчет сальдо по nzp_pack
                        MessageInPackLog(conn_db, packXX, "Расчет сальдо лицевых счетов", false, out r);
                        DbCalcCharge db = new DbCalcCharge();
                        ret = db.CalcChargeXXUchetOplatForPack(conn_db, packXX);
                        db.Close();
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

                }
                else
                {
                    ret.result = false;
                    MonitorLog.WriteLog(ret.text, MonitorLog.typelog.Error, 1, 222, true);
                    MessageInPackLog(conn_db, packXX, "Ошибка сохранения ручного распределения 2.11", true, out r);
                    return false;
                }
            }
            else
            {
                // ---------------- Если автоматическое распределение оплаты согласно заложенным алгоритмам ---------------------------------------------------------

                DropTempTablesPack(conn_db);
                // Получить лицевые счета для распределения
                packXX.where_pack_ls = packXX.where_pack_ls + strWherePackLsIsNotDistrib;
                    // !!! Марат 17.12.2012.  Распределять только такие оплаты
                if (packXX.paramcalc.pref.Trim() == "")
                {
                    sqlText =
                        " Update " + packXX.pack_ls +
                        " Set inbasket = 1, dat_uchet = null, calc_month = null, alg =0, nzp_user = " + nzp_user +
                        " Where nzp_pack =  " + packXX.nzp_pack + " and num_ls <=0 ";


                    ret21 = ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log);
                    ret = ret21.GetReturnsType().GetReturns();
                    if (!ret.result)
                    {
                        DropTempTablesPack(conn_db);
                        MessageInPackLog(conn_db, packXX, "Ошибка распределения 2.10", true, out r);
                        return false;
                    }
#if PG
                    cnt = ret21.resultAffectedRows;
#else
                    cnt = ClassDBUtils.GetAffectedRowsCount(conn_db);
#endif
                    if (cnt > 0)
                    {
                        MessageInPackLog(conn_db, packXX, "В корзину поместить оплаты, у которых не определен номер ЛС",
                            true, out r);

                        r = ExecSQL(conn_db,
                            " Insert into " + Points.Pref + "_fin_" + (Points.DateOper.Year%100).ToString("00") +
                            tableDelimiter + "pack_ls_err ( nzp_pack_ls,nzp_err,note ) " +
                            " Select nzp_pack_ls, 2,'В корзину поместить оплату, у которой не определен номер ЛС'" +
                            " From " + packXX.pack_ls +
                            " Where nzp_pack =  " + packXX.nzp_pack + " and num_ls <=0 "
                            , true);


                        r = ExecSQL(conn_db, " Insert into " + packXX.pack_log +
                                             " ( nzp_pack,nzp_pack_ls,dat_oper,dat_log,nzp_wp, txt_log,tip_log ) " +
                                             " Select nzp_pack,nzp_pack_ls, " + packXX.paramcalc.dat_oper + ", now(), " +
                                             "(select nzp_wp from " + Points.Pref + "_data" + tableDelimiter +
                                             "kvar where num_ls = pls.num_ls) " +
                                             " , 'В корзину поместить оплату, у которой не определен номер ЛС', 2 " +
                                             " From " + packXX.pack_ls + " pls " +
                                             " Where  nzp_pack =  " + packXX.nzp_pack + " and num_ls <=0 ",
                            true);


                    }
                    return false;
                }
#if PG
                ret = ExecSQL(conn_db, " set search_path to '" + Points.Pref + "_kernel'", true);
#endif
                CreateSelKvar(conn_db, packXX, out ret);
                // Установить nzp_supp у квитанций, у которых нет nzp_supp по коду из пачки
                sqlText = "update t_selkvar set nzp_supp = (select p.nzp_supp from " + Points.Pref + "_fin_" +
                          (Points.DateOper.Year%100).ToString("00") +
                          ".pack p where p.nzp_pack = t_selkvar.nzp_pack ) where kod_sum = 50 and coalesce(nzp_supp,0) = 0";
                ret = ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log).GetReturnsType().GetReturns();
                // Установить nzp_payer у квитанций, у которых нет nzp_payer по коду из пачки
                sqlText = "update t_selkvar set nzp_payer = (select p.nzp_payer from " + Points.Pref + "_fin_" +
                          (Points.DateOper.Year%100).ToString("00") +
                          ".pack p where p.nzp_pack = t_selkvar.nzp_pack ) where kod_sum = 49 and coalesce(nzp_payer,0) = 0";
                ret = ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log).GetReturnsType().GetReturns();

                if (!ret.result)
                {
                    return false;
                }

                //----------------------- ПРОВЕРКИ -----------------------
                // Есть ли записи в t_selkvar
                IntfResultType retres;
                retres = ClassDBUtils.ExecSQL(" Select * from t_selkvar ", conn_db, ClassDBUtils.ExecMode.Log);

                sqlText =
                    " Select count(*) cnt from t_selkvar ";
                dt = ClassDBUtils.OpenSQL(sqlText, conn_db).GetData();
                if (dt.Rows.Count > 0)
                {
                    row = dt.Rows[0];
                    if (row["cnt"] != DBNull.Value)
                    {
                        if (Convert.ToDecimal(row["cnt"]) == 0)
                        {
                            DropTempTablesPack(conn_db);
                            return false;
                        }
                    }
                }


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
                          //пени
                          "   sum_charge_peni numeric(14,2) default 0, " +
                          "   sum_charge_no_peni numeric(14,2) default 0, " +
                          "   isum_charge_peni_isdel_1  numeric(14,2) default 0, " +
                          "   isum_charge_peni_isdel_0  numeric(14,2) default 0, " +
                          "   isum_charge_no_peni_isdel_1  numeric(14,2) default 0, " +
                          "   isum_charge_no_peni_isdel_0  numeric(14,2) default 0, " +
                          "   is_peni integer default 0, " +
                          //пени
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
                 //пени
                            "   sum_charge_peni numeric(14,2) default 0, " +
                            "   sum_charge_no_peni numeric(14,2) default 0, " +
                "   isum_charge_peni_isdel_1  numeric(14,2) default 0, " +
                            "   isum_charge_peni_isdel_0  numeric(14,2) default 0, " +
                            "   isum_charge_no_peni_isdel_1  numeric(14,2) default 0, " +
                            "   isum_charge_no_peni_isdel_0  numeric(14,2) default 0, " +
                            "   is_peni integer default 0, " +
                //пени
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
                sqlText = " select p.bd_kernel, p.nzp_wp from " + Points.Pref +
                          "_kernel.s_point p where p.nzp_wp in ( select distinct nzp_wp from  t_selkvar) ";
#else
                    sqlText = " select p.bd_kernel, p.nzp_wp from " + Points.Pref + "_kernel:s_point p where p.nzp_wp in ( select distinct nzp_wp from  t_selkvar) ";
#endif


                if (setTaskProgress != null)
                {
                    setTaskProgress(((decimal) 1)/10);
                }
                
                // Предварительно очистить ранее распределённые оплаты по выбранным квитанциям
                ret = DeletePrevDistribution(conn_db, paramcalc.pref, paramcalc.calc_yy, paramcalc.calc_mm);
                if (!ret.result)
                {
                    DropTempTablesPack(conn_db);
                    MessageInPackLog(conn_db, packXX, (ret.tag==-1)?ret.text:"Ошибка распределения 1.4.1.2", true, out r);
                    return false;
                }
                
                #region Заполнить шаблон для распределения по выбранному банку

                string filterForSupplier49 = "";
                string filterForSupplier50 = "";
                string wherenzp_pack_ls = "";
                if (packXX.nzp_pack_ls > 0) wherenzp_pack_ls = " and nzp_pack_ls = " + packXX.nzp_pack_ls;
                sqlText =
                    " Update " + packXX.pack_ls +
                    " Set inbasket = 1, dat_uchet = null, calc_month = null, alg =0, nzp_user = " + nzp_user +
                    " Where nzp_pack =  " + packXX.nzp_pack +
                    "   and num_ls <= 0    " + wherenzp_pack_ls;
                ret21 = ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log);
                ret = ret21.GetReturnsType().GetReturns();
                if (!ret.result)
                {
                    DropTempTablesPack(conn_db);
                    MessageInPackLog(conn_db, packXX, "Ошибка распределения 2.10", true, out r);
                    return false;
                }
#if PG
                cnt = ret21.resultAffectedRows;
#else
                cnt = ClassDBUtils.GetAffectedRowsCount(conn_db);
#endif
                if (cnt > 0)
                {
                    MessageInPackLog(conn_db, packXX, "В корзину поместить оплаты, у которых не определен номер ЛС",
                        true, out r);
                }

                if (pack_type == 20)
                {
                    //filterForSupplier = " and c.nzp_supp = " + nzp_supp;
                    filterForSupplier49 =
                        " and (case when k.kod_sum  in (35, 40) then true else " +
                        " k.kod_sum=49 and " +
                        " c.nzp_supp in (select nzp_supp from  " + Points.Pref + sKernelAliasRest +
                        "supplier where nzp_payer_princip = k.nzp_payer ) end)";
                    filterForSupplier50 = " and k.kod_sum=50  and c.nzp_supp  = k.nzp_supp ";
                }


                //string strFilterForSupplierCode_1 = "";
                //string strFilterForSupplierCode_2 = "";

                #region Установить фильтр по pkod

                //strFilterForSupplierCode_1 = " and c.nzp_supp in (select sk.nzp_supp from  " + paramcalc.pref + "_data" + tableDelimiter + "supplier_codes sk where sk.pkod_supp = k.pkod and sk.nzp_supp = c.nzp_supp )";
                //strFilterForSupplierCode_2 = " and c.nzp_supp not in (select sk.nzp_supp from  " + paramcalc.pref + "_data" + tableDelimiter + "supplier_codes sk where sk.nzp_kvar = k.nzp_kvar and  sk.nzp_supp = c.nzp_supp) and k.nzp_kvar not in (select t.nzp_kvar from t_charge t )";

                #endregion Установить фильтр по pkod


                #region Установить фильтр по pkod

                //strFilterForSupplierCode_1 = " and c.nzp_supp in (select sk.nzp_supp from  " + paramcalc.pref + "_data" + tableDelimiter + "supplier_codes sk where sk.pkod_supp = k.pkod and sk.nzp_supp = c.nzp_supp )";
                //strFilterForSupplierCode_2 = " and c.nzp_supp not in (select sk.nzp_supp from  " + paramcalc.pref + "_data" + tableDelimiter + "supplier_codes sk where sk.nzp_kvar = k.nzp_kvar and  sk.nzp_supp = c.nzp_supp) and k.nzp_kvar not in (select t.nzp_kvar from t_charge t )";

                #endregion Установить фильтр по pkod

                // Установить фильтр по лицевым счетам у которых статус "неопределено"

                if (isDistributionForInSaldo)
                {

                    sqlText =
                        " Insert into t_charge ( nzp_kvar, nzp_pack_ls, num_ls, nzp_serv, nzp_supp,isdel, " +
                        " real_pere,real_charge,sum_tarif,sum_outsaldo,sum_outsaldo_prev,c_calc,sum_insaldo,sum_insaldop, " +
                        "rsum_tarif, sum_charge,sum_charge_prev, distr_month)  " +
                        " Select k.nzp_kvar, k.nzp_pack_ls, k.num_ls, c.nzp_serv, c.nzp_supp, c.isdel,  " +
                        " sum(c.real_pere),sum(c.real_charge),sum(c.sum_tarif),sum(c.sum_outsaldo),sum(c.sum_outsaldo),sum(c.c_calc), " +
                        " sum(sum_insaldo), sum(sum_insaldo), " +
                        " sum(c.rsum_tarif), sum(c.sum_charge),sum(c." + field_for_etalon + "), " +
                        "'" + distr_month + "' " +
                        " From t_selkvar k, " + baseName + " c " +
                        " Where k.nzp_kvar = c.nzp_kvar " +
                        "   and c.dat_charge is null " +
                        filterForSupplier49 +
                        //strFilterForSupplierCode_1 +
                        "   and c.nzp_serv > 1 " +
                        "   and k.kod_sum in (" + strKodSumForCharge_MM + ") " +
                        " Group by 1,2,3,4,5,6 ";
                    //ret = ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log).GetReturnsType().GetReturns();
                    //if (!ret.result)
                    //{
                    //    DropTempTablesPack(conn_db);
                    //    MessageInPackLog(conn_db, packXX, "Ошибка распределения 1.4.1.1", true, out r);
                    //    return false;
                    //}

                }
                else
                {
                    sqlText =
                        " Insert into t_charge ( nzp_kvar, nzp_pack_ls, num_ls, nzp_serv, nzp_supp,isdel, " +
                        " real_pere,real_charge,sum_tarif,sum_outsaldo,sum_outsaldo_prev,c_calc,sum_insaldo," +
                        "sum_insaldop, rsum_tarif, sum_charge,sum_charge_prev, distr_month)  " +
                        " Select k.nzp_kvar, k.nzp_pack_ls, k.num_ls, c.nzp_serv, c.nzp_supp, c.isdel,  " +
                        " sum(c.real_pere),sum(c.real_charge),sum(c.sum_tarif),sum(c.sum_outsaldo),sum(c.sum_outsaldo),sum(c.c_calc), " +
                        " sum(c.sum_insaldo), sum(c.sum_insaldo), " +
                        " sum(c.rsum_tarif), sum(c." + field_for_etalon + "),sum(c." + field_for_etalon + "), " +
                        "'" + distr_month + "' " +
                        " From t_selkvar k, " + baseName + " c " +
                        " Where k.nzp_kvar = c.nzp_kvar " +
                        "   and c.dat_charge is null " +
                        filterForSupplier49 +
                        //strFilterForSupplierCode_1 +
                        "   and c.nzp_serv > 1 " +
                        "   and k.kod_sum in (" + strKodSumForCharge_MM + ") ";
                    sqlText = sqlText + " Group by 1,2,3,4,5,6 ";
                }

                //kod_sum=49
                ret = ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log).GetReturnsType().GetReturns();
                if (!ret.result)
                {
                    DropTempTablesPack(conn_db);
                    MessageInPackLog(conn_db, packXX, "Ошибка распределения 1.4.1.1", true, out r);
                    return false;
                }

                /*
                // Заполнить charge для тех платёжных кодов, которых нет в supplier_Codes
                sqlText = sqlText.Replace(strFilterForSupplierCode_1, strFilterForSupplierCode_2);
                ret = ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log).GetReturnsType().GetReturns();
                if (!ret.result)
                {
                    DropTempTablesPack(conn_db);
                    MessageInPackLog(conn_db, packXX, "Ошибка распределения 1.4.1.1", true, out r);
                    return false;
                }
                */
                string sqlText2 = "";
                //kod_sum=50
                if (filterForSupplier50.Length > 0)
                {
                    sqlText2 = sqlText.Replace(filterForSupplier49, filterForSupplier50);
                    ret =
                        ClassDBUtils.ExecSQL(sqlText2, conn_db, ClassDBUtils.ExecMode.Log).GetReturnsType().GetReturns();
                    if (!ret.result)
                    {
                        DropTempTablesPack(conn_db);
                        MessageInPackLog(conn_db, packXX, "Ошибка распределения 1.4.1.1", true, out r);
                        return false;
                    }
                }

                // Распределение по предварительным счетам (авансовым)
                sqlText =
                    " Insert into t_charge ( nzp_kvar, nzp_pack_ls, num_ls, nzp_serv, nzp_supp,isdel, " +
                    " real_pere,real_charge,sum_tarif,sum_outsaldo,sum_outsaldo_prev,c_calc,sum_insaldo," +
                    "sum_insaldop, rsum_tarif, sum_charge,sum_charge_prev, distr_month)  " +
                    " Select k.nzp_kvar, k.nzp_pack_ls, k.num_ls, c.nzp_serv, c.nzp_supp, c.isdel,  " +
                    " sum(c.real_pere),sum(c.real_charge),sum(c.sum_tarif),sum(c.sum_outsaldo),sum(c.sum_outsaldo),sum(c.c_calc), " +
                    " sum(sum_insaldo), sum(sum_insaldo), " +
                    " sum(c.rsum_tarif), sum(c.sum_charge),sum(c." + field_for_etalon + "), " +
                    "'" + distr_month + "' " +
                    " From t_selkvar k, " + baseName_t.Trim() + "_T c " +
                    " Where k.nzp_kvar = c.nzp_kvar " +
                    "   and c.dat_charge is null " +
                    //             filterForSupplier49 +
                    "   and c.nzp_serv > 1 " +
                    "   and k.kod_sum in (81) ";

                /*
                if (Points.packDistributionParameters.distributionMethod == PackDistributionParameters.PaymentDistributionMethods.CurrentMonthPositiveSumInsaldo)
                {
                    sqlText =sqlText +" and sum_insaldo>0";
                }
                if (Points.packDistributionParameters.distributionMethod == PackDistributionParameters.PaymentDistributionMethods.LastMonthPositiveSumCharge)
                {
                    sqlText =sqlText +" and sum_charge>0";
                }
                */
                sqlText = sqlText + " Group by 1,2,3,4,5,6 ";
                ret = ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log).GetReturnsType().GetReturns();
                if (!ret.result)
                {
                    //DropTempTablesPack(conn_db);
                    //MessageInPackLog(conn_db, packXX, "Отсутствуют структуры CHARGE_MM_T", true, out r);
                    //return false;
                }

                ret = UchetPrevOplats(conn_db, paramcalc, packXX);
                if (!ret.result)
                {
                    DropTempTablesPack(conn_db);
                    MessageInPackLog(conn_db, packXX, "Ошибка распределения: Учет предыдущих оплат.", true, out r);
                    return false;
                }

                #endregion Заполнить шаблон для распределения по выбранному банку

                if (Points.packDistributionParameters.distributionMethod ==
                     PackDistributionParameters.PaymentDistributionMethods.CurrentMonthPositiveSumInsaldo ||
                    Points.packDistributionParameters.distributionMethod ==
                     PackDistributionParameters.PaymentDistributionMethods.LastMonthPositiveSumCharge||
                    Points.packDistributionParameters.distributionMethod ==
                     PackDistributionParameters.PaymentDistributionMethods.LastMonthPositiveSumOutsaldo
                    )
                {
                    sqlText = " update t_charge set sum_insaldo=0  where sum_insaldo<0 ";
                    ret =
                        ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log).GetReturnsType().GetReturns();
                    if (!ret.result)
                    {
                        DropTempTablesPack(conn_db);
                        MessageInPackLog(conn_db, packXX, "Ошибка распределения 1.4.1.5", true, out r);
                        return false;
                    }

                    sqlText = " update t_charge set sum_charge=0 where sum_charge<0 ";
                    ret =
                        ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log).GetReturnsType().GetReturns();
                    if (!ret.result)
                    {
                        DropTempTablesPack(conn_db);
                        MessageInPackLog(conn_db, packXX, "Ошибка распределения 1.4.1.6", true, out r);
                        return false;
                    }

                }

                //определить услуги с пенями
                sqlText = "update t_charge ch set is_peni = 1 where exists (select 1 from " + Points.Pref + "_kernel" + tableDelimiter + "peni_settings ps where ch.nzp_serv = ps.nzp_peni_serv)";
                ret = ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log).GetReturnsType().GetReturns();
                if (!ret.result)
                {
                    DropTempTablesPack(conn_db);
                    MessageInPackLog(conn_db, packXX, "Ошибка в процессе определения пеней", true, out r);
                    return false;
                }

                sqlText = " update t_charge c set " +
                          " isum_charge_isdel_1 = (case when isdel = 1 then sum_charge else 0 end) ," +
                          " isum_charge_isdel_0 =  (case when isdel = 0  then sum_charge else 0 end), " +
                          " sum_charge_peni = (case when is_peni = 1 then c.sum_charge else 0 end), " +
                          " sum_charge_no_peni = (case when is_peni <> 1 then c.sum_charge else 0 end), " +
                          " isum_charge_peni_isdel_1 = (case when isdel = 1 and is_peni=1 then c.sum_charge else 0 end)," +
                          " isum_charge_peni_isdel_0 = (case when isdel = 0 and is_peni=1 then c.sum_charge else 0 end), " +
                          " isum_charge_no_peni_isdel_1 = (case when isdel = 1 and is_peni<>1 then c.sum_charge else 0 end)," +
                          " isum_charge_no_peni_isdel_0 = (case when isdel = 0 and is_peni<>1 then c.sum_charge else 0 end) ";
                ret = ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log).GetReturnsType().GetReturns();
                if (!ret.result)
                {
                    DropTempTablesPack(conn_db);
                    MessageInPackLog(conn_db, packXX, "Ошибка распределения 1.4.1.5", true, out r);
                    return false;
                }


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
                    " Where k.nzp = t_selkvar.nzp_kvar  AND k.nzp_prm = 51 and k.val_prm = '1' and '01." +
                    (Points.DateOper.Month%100).ToString("00") + "." + (Points.DateOper.Year).ToString("00") +
                    "' between k.dat_s and k.dat_po and k.is_actual<>100) ";
#else
                    sqlText =
                    " update t_selkvar   set is_open = 0 where t_selkvar.nzp_kvar not in (  " +
                    " Select k.nzp  " +
                    " From " + paramcalc.pref + "_data:prm_3 k " +
                    " Where k.nzp = t_selkvar.nzp_kvar  AND k.nzp_prm = 51 and k.val_prm = '1' and '01." + (Points.DateOper.Month % 100).ToString("00") + "." + (Points.DateOper.Year).ToString("00") + "' between k.dat_s and k.dat_po and k.is_actual<>100 ) ";
#endif
                ret = ExecSQL(conn_db, sqlText, true);
                if (!ret.result)
                {
                    DropTempTablesPack(conn_db);
                    MessageInPackLog(conn_db, packXX, "Ошибка выборки 1.3", true, out r);
                    return false;
                }

                sqlText = "update t_selkvar set is_open = -1 where  " +
                          "  t_selkvar.nzp_kvar in (  " +
                          " Select k.nzp  " +
                          " From " + paramcalc.pref + "_data" + tableDelimiter + "prm_3 k " +
                          " Where k.nzp = t_selkvar.nzp_kvar  AND k.nzp_prm = 51 and k.val_prm = '3' and '01." +
                          (Points.DateOper.Month%100).ToString("00") + "." + (Points.DateOper.Year).ToString("00") +
                          "' between k.dat_s and k.dat_po and k.is_actual<>100 ) ";

                ret = ExecSQL(conn_db, sqlText, true);
                if (!ret.result)
                {
                    DropTempTablesPack(conn_db);
                    MessageInPackLog(conn_db, packXX, "Ошибка выборки 1.3", true, out r);
                    return false;
                }
                if (setTaskProgress != null)
                {
                    setTaskProgress(((decimal) 2)/10);
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




                if (setTaskProgress != null)
                {
                    setTaskProgress(((decimal) 3)/10);
                }

                ExecSQL(conn_db, " Create unique index ix1_t_charge on t_charge (nzp_key) ", true);
                ExecSQL(conn_db,
                    " Create unique index ix2_t_charge on t_charge (nzp_kvar,nzp_pack_ls,nzp_serv,nzp_supp) ", true);
                ExecSQL(conn_db, " Create unique index ix3_t_charge on t_charge (num_ls,nzp_pack_ls,nzp_serv,nzp_supp) ",
                    true);
                ExecSQL(conn_db, sUpdStat + "  t_charge ", true);

                // Определить итоговую сумму эталона к распределению
//#if PG
//                sqlText =
//                    " update t_selkvar set sum_etalon =  coalesce((select sum(sum_charge) from t_charge where t_charge.nzp_pack_ls = t_selkvar.nzp_pack_ls),0) " +
//                    "   where t_selkvar.kod_sum in (" + strKodSumForCharge_MM + ",81) ";
//#else
//                    sqlText =
//                    " update t_selkvar set sum_etalon =  nvl((select sum(" + field_for_etalon + ") from t_charge where t_charge.nzp_pack_ls = t_selkvar.nzp_pack_ls),0) " +
//                    "   where t_selkvar.kod_sum in (" + strKodSumForCharge_MM + ",81) ";
//#endif
//                ret = ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log).GetReturnsType().GetReturns();
//                if (!ret.result)
//                {
//                    DropTempTablesPack(conn_db);
//                    MessageInPackLog(conn_db, packXX, "Ошибка определения итоговой суммы эталона к распределению", true,
//                        out r);
//                    return false;
//                }

                //итоговые начисления
#if PG
                sqlText = " Select c.nzp_kvar, k.nzp_pack_ls, " +
                          " sum(c.real_pere) real_pere,sum(c.real_charge) real_charge,sum(c.sum_tarif) sum_tarif, " +
                          " sum(c.sum_outsaldo) sum_outsaldo,sum(case when c.sum_outsaldo>0 then c.sum_outsaldo else 0 end) sum_outsaldo_pol," +
                          " sum(case when c.sum_outsaldo>0 and isdel = 0 then c.sum_outsaldo else 0 end) sum_outsaldo_pol_isdel_0,sum(c.c_calc) c_calc, " +
                          " sum(c.sum_insaldo) sum_insaldo, sum(c.sum_insaldop) sum_insaldop,sum(case when c.rsum_tarif>0 and c.is_peni<>1 then c.rsum_tarif else 0 end ) rsum_tarif, " +
                          " sum(c.sum_charge) sum_charge, sum(c.isum_charge_isdel_1) isum_charge_isdel_1," +
                          " sum(c.isum_charge_isdel_0) isum_charge_isdel_0, " +
                          " sum(c.sum_charge_peni) isum_charge_peni, " +
                          " sum(c.sum_charge_no_peni) isum_charge_no_peni, " +
                          " sum(c.isum_charge_peni_isdel_1) isum_charge_peni_isdel_1," +
                          " sum(c.isum_charge_peni_isdel_0) isum_charge_peni_isdel_0, " +
                          " sum(c.isum_charge_no_peni_isdel_1) isum_charge_no_peni_isdel_1," +
                          " sum(c.isum_charge_no_peni_isdel_0) isum_charge_no_peni_isdel_0 " +
                          "  Into temp t_itog " +
                          " From t_charge c, t_selkvar k where k.nzp_kvar = c.nzp_kvar and k.nzp_pack_ls = c.nzp_pack_ls " +
                          " Group by 1,2 ";
#else
                    sqlText = " Select c.nzp_kvar, k.nzp_pack_ls, " +
                            " sum(c.real_pere) real_pere,sum(c.real_charge) real_charge,sum(c.sum_tarif) sum_tarif, " +
                            " sum(c.sum_outsaldo) sum_outsaldo,sum(c.c_calc) c_calc, sum(c.sum_insaldo) sum_insaldo, sum(c.sum_insaldop) sum_insaldop,sum(case when c.rsum_tarif>0 then c.rsum_tarif else 0 end ) rsum_tarif, " +
                            " sum(c.sum_charge) sum_charge, sum(c.isum_charge_isdel_1) isum_charge_isdel_1,sum(c.isum_charge_isdel_0) isum_charge_isdel_0 " +
                              " sum(c.sum_charge_peni) isum_charge_peni, " +
                       " sum(c.sum_charge_no_peni) isum_charge_no_peni, " +
                " sum(c.isum_charge_peni_isdel_1) isum_charge_peni_isdel_1," +
                        " sum(c.isum_charge_peni_isdel_0) isum_charge_peni_isdel_0, " +
                        " sum(c.isum_charge_no_peni_isdel_1) isum_charge_no_peni_isdel_1," +
                        " sum(c.isum_charge_no_peni_isdel_0) isum_charge_no_peni_isdel_0 " +
                " From t_charge c, t_selkvar k where k.nzp_kvar = c.nzp_kvar and k.nzp_pack_ls = c.nzp_pack_ls " +
                    " Group by 1,2 Into temp t_itog With no log ";
#endif
                if (isDebug)
                {
#if PG
                    ret = ExecSQL(conn_db, " Create table t_itog (nzp_kvar integer,nzp_pack_ls numeric(14,0), " +
                                           "real_pere numeric(14,2),real_charge numeric(14,2), sum_tarif numeric(14,2), " +
                                           "sum_outsaldo numeric(14,2),sum_outsaldo_pol numeric(14,2), sum_outsaldo_pol_isdel_0 numeric(14,2), c_calc numeric(14,2) , sum_insaldo numeric(14,2),  " +
                                           "sum_insaldop numeric(14,2),  rsum_tarif numeric(14,2),sum_charge numeric(14,2)," +
                                           "isum_charge_isdel_1 numeric(14,2),isum_charge_isdel_0 numeric(14,2), " +
                                           "isum_charge_peni numeric(14,2), isum_charge_no_peni numeric(14,2), " +
                                           "isum_charge_peni_isdel_1 numeric(14,2), isum_charge_peni_isdel_0 numeric(14,2), " +
                                           "isum_charge_no_peni_isdel_1 numeric(14,2), isum_charge_no_peni_isdel_0 numeric(14,2) " +
                                           ") ", true);
                    sqlText = sqlText.Replace("Into temp t_itog", "");
#else
                        ret = ExecSQL(conn_db, " Create table t_itog (nzp_kvar integer,nzp_pack_ls decimal(14,0),"+
" real_pere decimal(14,2),real_charge decimal(14,2), sum_tarif decimal(14,2), sum_outsaldo decimal(14,2), "+
"c_calc decimal(14,2) , sum_insaldo decimal(14,2),  sum_insaldop decimal(14,2),  rsum_tarif decimal(14,2),"+
"sum_charge decimal(14,2), isum_charge_peni decimal(14,2), isum_charge_no_peni decimal(14,2), "+
                    "isum_charge_peni_isdel_1 numeric(14,2), isum_charge_peni_isdel_0 numeric(14,2), " +
                         "isum_charge_no_peni_isdel_1 numeric(14,2), isum_charge_no_peni_isdel_0 numeric(14,2) " +
                    ") ", true);
                        sqlText = sqlText.Replace("Into temp t_itog With no log", "");

#endif

                    sqlText = "insert into t_itog (nzp_kvar, nzp_pack_ls, real_pere,real_charge,sum_tarif, " +
                              " sum_outsaldo, sum_outsaldo_pol, sum_outsaldo_pol_isdel_0, c_calc, sum_insaldo, sum_insaldop, rsum_tarif, sum_charge, " +
                              "isum_charge_isdel_1, isum_charge_isdel_0, isum_charge_peni, isum_charge_no_peni," +
                              "isum_charge_peni_isdel_1, isum_charge_peni_isdel_0, isum_charge_no_peni_isdel_1," +
                              " isum_charge_no_peni_isdel_0" +
                              " ) " + sqlText;
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
                    "   isum_outsaldo_isdel_0  numeric(14,2) default 0, " +
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
                    "   znak_sum_prih_u  integer default 1 ," +
                    "   isdel         integer default 0, " +
                    "   sum_charge_prev   numeric(14,2) default 0, " +
                    "   sum_charge   numeric(14,2) default 0, " +

                    //пени
                    "   isum_charge_peni numeric(14,2) default 0, " +
                    "   isum_charge_no_peni numeric(14,2) default 0, " +
                    "   ig_sum_ls_peni numeric(14,2) default 0, " +
                    "   ig_sum_ls_no_peni numeric(14,2) default 0, " +
                    "   ig_sum_ls_peni_isdel_1 numeric(14,2) default 0, " +
                    "   ig_sum_ls_no_peni_isdel_1 numeric(14,2) default 0, " +
                    "   ig_sum_ls_peni_isdel_0 numeric(14,2) default 0, " +
                    "   ig_sum_ls_no_peni_isdel_0 numeric(14,2) default 0, " +
                    "   g_sum_ls_peni_ost numeric(14,2) default 0, " +
                    "   g_sum_ls_no_peni_ost numeric(14,2) default 0, " +
                    "   isum_charge_peni_isdel_1  numeric(14,2) default 0, " +
                    "   isum_charge_peni_isdel_0  numeric(14,2) default 0, " +
                    "   isum_charge_no_peni_isdel_1  numeric(14,2) default 0, " +
                    "   isum_charge_no_peni_isdel_0  numeric(14,2) default 0, " +
                    "   is_peni         integer default 0, " +
                    //пени

                    "   isum_charge   numeric(14,2) default 0, " +
                    "   isum_charge_isdel_1  numeric(14,2) default 0, " +
                    "   znak integer default 1 , " +
                    "   ngroup integer," +
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

                    //пени
                    "   isum_charge_peni decimal(14,2) default 0, " +
                    "   isum_charge_no_peni decimal(14,2) default 0, " +
                    "   ig_sum_ls_peni decimal(14,2) default 0, " +
                    "   ig_sum_ls_no_peni decimal(14,2) default 0, " +
                    "   ig_sum_ls_peni_ost decimal(14,2) default 0, " +
                    "   ig_sum_ls_no_peni_ost decimal(14,2) default 0, " +
                    "   isum_charge_peni_isdel_1  decimal(14,2) default 0, " +
                    "   isum_charge_peni_isdel_0  decimal(14,2) default 0, " +
                    "   isum_charge_no_peni_isdel_1  decimal(14,2) default 0, " +
                    "   isum_charge_no_peni_isdel_0  decimal(14,2) default 0, " +
                    //пени

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
                        "  kod_info, sum_prih, sum_charge_prev,sum_charge, isum_charge," +
                        "isum_charge_isdel_1,isum_charge_isdel_0, isum_charge_peni, isum_charge_no_peni," +
                        "isum_charge_peni_isdel_1,isum_charge_peni_isdel_0,isum_charge_no_peni_isdel_1,isum_charge_no_peni_isdel_0, is_peni ) " +
                        " Select s.nzp_key,s.nzp_kvar,f.num_ls, s.nzp_serv,s.nzp_supp, " +
                        "  s.sum_tarif, i.sum_tarif as isum_tarif,  " +
                        "  s.sum_insaldop as sum_insaldop, s.sum_insaldo as sum_insaldo, i.sum_insaldo as isum_insaldo,s.rsum_tarif, i.rsum_tarif as irsum_tarif, s.isdel, " +
                        "  f.kod_sum, f.nzp_pack_ls, f.dat_month, f.dat_vvod, f.g_sum_ls,f.g_sum_ls, " +
                        "  0 as kod_info, 0.00, s.sum_charge_prev as sum_charge_prev, s.sum_charge as sum_charge,i.sum_charge as isum_charge," +
                        " i.isum_charge_isdel_1,i.isum_charge_isdel_0, i.isum_charge_peni, i.isum_charge_no_peni, " +
                        "  i.isum_charge_peni_isdel_1,i.isum_charge_peni_isdel_0, i.isum_charge_no_peni_isdel_1,i.isum_charge_no_peni_isdel_0, is_peni " +
                        " From t_charge s, t_itog i, t_selkvar f " +
                        " Where s.nzp_pack_ls = i.nzp_pack_ls " +
                        "   and s.nzp_pack_ls = f.nzp_pack_ls ";
                }
                else
                {
                    sqlText =
                        " Insert into t_opl ( nzp_key,nzp_kvar,num_ls, nzp_serv,nzp_supp,sum_tarif,isum_tarif, sum_outsaldo_prev,sum_outsaldo, isum_outsaldo, isum_outsaldo_isdel_0, rsum_tarif, irsum_tarif, isdel, kod_sum, nzp_pack_ls, dat_month, dat_vvod, g_sum_ls_first,g_sum_ls, " +
                        "  kod_info, sum_prih, sum_charge_prev,sum_charge, isum_charge," +
                        "isum_charge_isdel_1,isum_charge_isdel_0, isum_charge_peni, isum_charge_no_peni, " +
                        "isum_charge_peni_isdel_1,isum_charge_peni_isdel_0,isum_charge_no_peni_isdel_1,isum_charge_no_peni_isdel_0, is_peni ) " +
                        " Select s.nzp_key,s.nzp_kvar,f.num_ls, s.nzp_serv,s.nzp_supp, " +
                        "  s.sum_tarif, i.sum_tarif as isum_tarif,  " +
                        "  s.sum_outsaldo_prev as sum_outsaldo_prev, s.sum_outsaldo as sum_outsaldo," +
                        " i.sum_outsaldo_pol as isum_outsaldo, i.sum_outsaldo_pol_isdel_0 as isum_outsaldo_isdel_0,s.rsum_tarif, i.rsum_tarif as irsum_tarif, s.isdel, " +
                        "  f.kod_sum, f.nzp_pack_ls, f.dat_month, f.dat_vvod, f.g_sum_ls,f.g_sum_ls, " +
                        "  0 as kod_info, 0.00, s.sum_charge_prev as sum_charge_prev," +
                        " s.sum_charge as sum_charge,i.sum_charge as isum_charge, " +
                        "i.isum_charge_isdel_1,i.isum_charge_isdel_0, i.isum_charge_peni, i.isum_charge_no_peni, " +
                        "  i.isum_charge_peni_isdel_1,i.isum_charge_peni_isdel_0, i.isum_charge_no_peni_isdel_1,i.isum_charge_no_peni_isdel_0, is_peni " +
                        " From t_charge s, t_itog i, t_selkvar f " +
                        " Where s.nzp_pack_ls = i.nzp_pack_ls " +
                        "   and s.nzp_pack_ls = f.nzp_pack_ls ";

                }



                ret = ExecSQL(conn_db, sqlText, true);


                if (setTaskProgress != null)
                {
                    setTaskProgress(((decimal) 4)/10);
                }


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
                ExecSQL(conn_db, " Create index ix3_t_gil_sums on t_gil_sums (nzp_pack_ls, nzp_serv, nzp_supp) ", true);
#if PG
                ExecSQL(conn_db, " analyze t_gil_sums ", true);
#else
                    ExecSQL(conn_db, " Update statistics for table t_gil_sums ", true);
#endif

                // Установить знак для отрицательных оплат
                sqlText =
                    "update t_opl set g_sum_ls = (-1)* g_sum_ls, g_sum_ls_first = (-1)* g_sum_ls_first, znak = -1 where g_sum_ls<0  ";
                ExecSQL(conn_db, sqlText, true);

                sqlText =
                    "  update t_opl set kod_info = 177 where nzp_pack_ls in  (Select nzp_pack_ls From t_selkvar Where is_open = -1) ";
                ExecSQL(conn_db, sqlText, true);


                //итоговые остатки
#if PG
                sqlText = " select nzp_pack_ls, g_sum_ls,g_sum_ls_ost,g_sum_ls_peni_ost, g_sum_ls_no_peni_ost," +
                          " sum(sum_prih_d+sum_prih_u+sum_prih_a+sum_prih_s) as sum_prih " +
                          "" +
                          "Into temp t_ostatok " +
                          "from t_opl " + " group by 1,2,3,4,5  ";
#else
                    sqlText = " select nzp_pack_ls, g_sum_ls,g_sum_ls_ost, sum(sum_prih_d+sum_prih_u+sum_prih_a+sum_prih_s) as sum_prih from t_opl " +
                        " group by 1,2,3  Into temp t_ostatok With no log ";
#endif
                if (isDebug)
                {
#if PG
                    ret = ExecSQL(conn_db,
                        " create table t_ostatok (nzp_pack_ls integer, g_sum_ls numeric(14,2)  default 0, " +
                        "sum_prih numeric(14,2) default 0, g_sum_ls_ost numeric(14,2) default 0," +
                        "g_sum_ls_peni_ost numeric(14,2) default 0, g_sum_ls_no_peni_ost  numeric(14,2) default 0 " +
                        ") ", true);
#else
                        ret = ExecSQL(conn_db, " create table t_ostatok (nzp_pack_ls integer, g_sum_ls decimal(14,2)  default 0, "+
                            "sum_prih decimal(14,2) default 0, g_sum_ls_ost decimal(14,2) default 0  ," +
                            "g_sum_ls_peni_ost numeric(14,2) default 0, g_sum_ls_no_peni_ost  numeric(14,2) default 0 " +
                            ") ", true);
#endif
#if PG
                    sqlText = sqlText.Replace("Into temp t_ostatok", "");
#else
                        sqlText = sqlText.Replace("Into temp t_ostatok With no log", "");
#endif
                    sqlText = "insert into t_ostatok (nzp_pack_ls, g_sum_ls, g_sum_ls_ost, " +
                              "g_sum_ls_peni_ost, g_sum_ls_no_peni_ost, sum_prih) " + sqlText;
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
                    "  nzp_key = (select  MAX(a.nzp_key) from " +
                    " t_charge a where a.nzp_pack_ls =  t_opl.nzp_pack_ls and a.sum_insaldo >0 )";
#else
                    sqlText =
                        " update t_opl set kod_info = 102, sum_prih = g_sum_ls, sum_prih_d = g_sum_ls where kod_info = 0 and g_sum_ls = 0.01 and " +
                        "  nzp_key = (select  MAX(a.nzp_key) from "+ " t_charge a where a.nzp_pack_ls =  t_opl.nzp_pack_ls and a.sum_insaldo >0 )";
#endif
                ret = ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log).GetReturnsType().GetReturns();

                if (setTaskProgress != null)
                {
                    setTaskProgress(((decimal) 5)/10);
                }

                sqlText =
                    " Create temp table t_iopl_102 (nzp_kvar  integer, nzp_pack_ls  integer, sum_prih numeric(14,2),  g_sum_ls numeric(14,2), ngroup integer," +
                    "nzp_key integer, sum_ost_dolg numeric(14,2)) ";
                if (isDebug)
                {
                    sqlText = sqlText.Replace("temp", "");
                }
                ret = ExecSQL(conn_db, sqlText.ToString(), true);
                if (!ret.result)
                {
                    DropTempTablesPack(conn_db);
                    MessageInPackLog(conn_db, packXX, "Ошибка распределения 2.102", true, out ret);
                    return false;
                }

                ExecSQL(conn_db, " Create unique index ix3_t_iopl_102 on t_iopl_102 (nzp_pack_ls,ngroup) ", true);


                //----------------------- ОСНОВАНИЕ -----------------------

                if (isDistributionForInSaldo)
                {
                    OplSamara(conn_db, packXX, out ret);
                }
                else
                {
                    StandartReparation(conn_db, packXX, out ret);
                }

                if (setTaskProgress != null)
                {
                    setTaskProgress(((decimal) 6)/10);
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
                }
                ;

                if (!ret.result)
                {
                    DropTempTablesPack(conn_db);
                    return false;
                }

                if (setTaskProgress != null)
                {
                    setTaskProgress(((decimal) 7)/10);
                }


                //выровним копейки
#if PG
                sqlText =
                    " Select nzp_kvar, nzp_pack_ls, max(case when sum_prih > 0.20 then nzp_key else 0 end) as nzp_key, sum(sum_prih) as sum_prih, max(znak*g_sum_ls_first) as g_sum_ls " +
                    " Into temp t_iopl " +
                    " From t_opl " +
                    " Where kod_info > 0 " +
                    //"   and sum_prih <> 0 and isdel=0 " +
                    "   and sum_prih <> 0 " +
                    " Group by 1,2 ";
#else
                    sqlText =
                        " Select nzp_kvar, nzp_pack_ls, max(case when sum_prih > 0.20 then nzp_key else 0 end) as nzp_key, sum(sum_prih) as sum_prih, max(znak*g_sum_ls_first) as g_sum_ls " +
                        " From t_opl " +
                        " Where kod_info > 0 " +
                        //"   and sum_prih <> 0 and isdel=0 " +
                        "   and sum_prih <> 0 " +
                        " Group by 1,2 " +
                        " Into temp t_iopl With no log ";
#endif
                if (isDebug)
                {
#if PG
                    ret = ExecSQL(conn_db,
                        " Create table t_iopl (nzp_kvar  integer, nzp_pack_ls  integer,  nzp_key integer, sum_prih numeric(14,2),  g_sum_ls numeric(14,2)) ",
                        true);
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
                        " Select nzp_kvar, nzp_pack_ls, max(case when sum_prih > 0.20 then nzp_key else 0 end) as nzp_key, sum(sum_prih) as sum_prih, max(znak*g_sum_ls_first) as g_sum_ls " +
                        "  Into temp t_iopl " +
                        " From t_opl " +
                        " Where kod_info > 0 " +
                        "   and sum_prih <> 0 and isdel=1 " +
                        " Group by 1,2 ";
#else
                        sqlText =
                                        " Select nzp_kvar, nzp_pack_ls, max(case when sum_prih > 0.20 then nzp_key else 0 end) as nzp_key, sum(sum_prih) as sum_prih, max(znak*g_sum_ls_first) as g_sum_ls " +
                                        " From t_opl " +
                                        " Where kod_info > 0 " +
                                        "   and sum_prih <> 0 and isdel=1 " +
                                        " Group by 1,2 " +
                                        " Into temp t_iopl With no log ";
#endif
                    if (isDebug)
                    {
#if PG
                        ret = ExecSQL(conn_db,
                            " Create table t_iopl (nzp_kvar  integer, nzp_pack_ls  integer,  nzp_key integer, sum_prih numeric(14,2),  g_sum_ls numeric(14,2)) ",
                            true);
#else
                            ret = ExecSQL(conn_db, " Create table t_iopl (nzp_kvar  integer, nzp_pack_ls  integer,  nzp_key integer, sum_prih decimal(14,2),  g_sum_ls decimal(14,2)) ", true);
#endif
#if PG
                        sqlText = sqlText.Replace("Into temp t_iopl ", "");
#else
                            sqlText = sqlText.Replace("Into temp t_iopl With no log", "");
#endif
                        sqlText = "insert into t_iopl (nzp_kvar, nzp_pack_ls,  nzp_key , sum_prih,  g_sum_ls) " +
                                  sqlText;
                    }
                    ret =
                        ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log).GetReturnsType().GetReturns();

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

                if (setTaskProgress != null)
                {
                    setTaskProgress(((decimal) 8)/10);
                }

                sqlText =
                    " update t_ostatok set g_sum_ls = (-1) * g_sum_ls where nzp_pack_Ls in (select nzp_pack_ls from t_opl where  kod_info >0 and znak = -1)  ";
                ret = ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log).GetReturnsType().GetReturns();
                if (!ret.result)
                {
                    DropTempTablesPack(conn_db);
                    MessageInPackLog(conn_db, packXX, "Ошибка распределения 2.2", true, out r);
                    return false;
                }

                sqlText =
                    " update t_opl set g_sum_ls = (-1) * g_sum_ls, sum_prih_d=(-1)* sum_prih_d, sum_prih_a=(-1)* sum_prih_a, sum_prih_s=(-1)* sum_prih_s where  kod_info >0 and znak = -1 ";
                ret = ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log).GetReturnsType().GetReturns();
                if (!ret.result)
                {
                    DropTempTablesPack(conn_db);
                    MessageInPackLog(conn_db, packXX, "Ошибка распределения 2.2", true, out r);
                    return false;
                }

                sqlText =
                    " update t_opl set sum_prih= sum_prih_a+sum_prih_u+sum_prih_d+sum_prih_s  where  kod_info >0 and znak = -1 ";

                ret = ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log).GetReturnsType().GetReturns();
                if (!ret.result)
                {
                    DropTempTablesPack(conn_db);
                    MessageInPackLog(conn_db, packXX, "Ошибка распределения 2.2", true, out r);
                    return false;
                }


                // Сновая подсчитать сумму распределённой оплаты
                sqlText =
                    " update t_iopl set sum_prih = (select SUM(sum_prih) from t_opl where t_opl.nzp_pack_ls = t_iopl.nzp_pack_ls and kod_info >0 and kod_info not in (110,177))   ";
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
                                MessageInPackLog(conn_db, packXX,
                                    "Сумма распределения (" + Convert.ToString(row["sum_prih"]) +
                                    " руб) не соответствует оплате (" + Convert.ToString(row["g_sum_ls"]) + " руб) на " +
                                    Convert.ToString(row["sum_1"]), false, out r);
                            }
                        }
                    }
                }

                string whereExcludeServ = "";
                if (Points.packDistributionParameters.repayPeni == PackDistributionParameters.OrderRepayPeni.Last)
                {
                    whereExcludeServ = " and is_peni <> 1 ";
                }

                // 1. Выравнить округление на открытые услуги
                sqlText =
                    "Update t_iopl  Set nzp_key = ( Select MAX( nzp_key) From t_opl a Where a.nzp_pack_ls = t_iopl.nzp_pack_ls  and a.sum_prih = (select MAX(b.sum_prih) from t_opl b where a.nzp_pack_ls = b.nzp_pack_ls and b.isdel=0 " +
                    whereExcludeServ + "))  " +
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
                    " Where i.nzp_key = t_opl.nzp_key and i.nzp_pack_ls = t_opl.nzp_pack_ls  " +
                    "   and abs(i.sum_prih - i.g_sum_ls) > 0.0001 )) , " +
                    " sum_prih_d = sum_prih - sum_prih_u - sum_prih_a-sum_prih_s " +
                    " Where 0 < ( Select count(*) From t_iopl i " +
                    " Where i.nzp_key = t_opl.nzp_key and i.nzp_pack_ls = t_opl.nzp_pack_ls " +
                    "   and abs(i.sum_prih - i.g_sum_ls) > 0.0001  " +
                    "   and abs(i.sum_prih - i.g_sum_ls) < 0.20 ) ";
                ;
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
                            " select a.service from " + Points.Pref +
                            "_kernel.services a, t_opl b, t_iopl t where a.nzp_serv = b.nzp_serv and b.nzp_key = t.nzp_key  ";
#else
                            sqlText =
                            " select a.service from " + Points.Pref + "_kernel:services a, t_opl b, t_iopl t where a.nzp_serv = b.nzp_serv and b.nzp_key = t.nzp_key  ";
#endif
                        dt = ClassDBUtils.OpenSQL(sqlText, conn_db).GetData();
                        if (dt.Rows.Count > 0)
                        {
                            row = dt.Rows[0];
                            MessageInPackLog(conn_db, packXX,
                                "Ошибка округления " + s_okr + " зачтена на услугу '" + Convert.ToString(row["service"]) +
                                "'", false, out r);
                        }
                    }
                }

                //sqlText = "Update t_iopl  Set nzp_key = 0 ";
                //ret = ExecSQL(conn_db, sqlText, true);

                // Сновая подсчитать сумму распределённой оплаты
                sqlText =
                    " update t_iopl set sum_prih = (select SUM(sum_prih) from t_opl where t_opl.nzp_pack_ls = t_iopl.nzp_pack_ls and kod_info >0 and kod_info not in (110,177))   ";
                ret = ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log).GetReturnsType().GetReturns();
                if (!ret.result)
                {
                    DropTempTablesPack(conn_db);
                    MessageInPackLog(conn_db, packXX, "Ошибка распределения 2.2", true, out r);
                    return false;
                }

                // 2. Выравнить округление на закрытые услуги
                sqlText =
                    "Update t_iopl  Set nzp_key = ( Select MAX( nzp_key) From t_opl a Where a.nzp_pack_ls = t_iopl.nzp_pack_ls  and a.sum_prih = (select MAX(b.sum_prih) from t_opl b where a.nzp_pack_ls = b.nzp_pack_ls " +
                    whereExcludeServ + "))  " +
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
                    " Where i.nzp_key = t_opl.nzp_key and i.nzp_pack_ls = t_opl.nzp_pack_ls  " +
                    "   and abs(i.sum_prih - i.g_sum_ls) > 0.0001 )) , " +
                    " sum_prih_d = sum_prih - sum_prih_u - sum_prih_a-sum_prih_s " +
                    " Where 0 < ( Select count(*) From t_iopl i " +
                    " Where i.nzp_key = t_opl.nzp_key and i.nzp_pack_ls = t_opl.nzp_pack_ls " +
                    "   and abs(i.sum_prih - i.g_sum_ls) > 0.0001  " +
                    "   and abs(i.sum_prih - i.g_sum_ls) < 0.20 ) ";
                ;
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
                    " update t_opl set kod_info = 0 where kod_info >0 and nzp_pack_ls in (select i.nzp_pack_ls from t_iopl i where i.sum_prih <> i.g_sum_ls) and kod_info not in (110,177,115) ";
                ret = ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log).GetReturnsType().GetReturns();
                if (!ret.result)
                {
                    DropTempTablesPack(conn_db);
                    MessageInPackLog(conn_db, packXX, "Ошибка распределения 2.6", true, out r);
                    return false;
                }

                sqlText =
                    " update t_opl set kod_info = 0 where kod_info >0 and kod_info not in (110,177,115) and nzp_pack_ls not in (select i.nzp_pack_ls from t_iopl i )  ";
                ret = ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log).GetReturnsType().GetReturns();
                if (!ret.result)
                {
                    DropTempTablesPack(conn_db);
                    MessageInPackLog(conn_db, packXX, "Ошибка распределения 2.6", true, out r);
                    return false;
                }


                // Поправить знак для отрицательных оплат
                //sqlText = "update t_opl set g_sum_ls = (-1)*g_sum_ls,  g_sum_ls_first = (-1)*g_sum_ls_first, sum_prih = (-1)*sum_prih, sum_prih_d = (-1)*sum_prih_d, sum_prih_s = (-1)*sum_prih_s,sum_prih_u = (-1)*sum_prih_u,sum_prih_a = (-1)*sum_prih_a where znak = -1 and kod_info >0 and kod_info not in (110,177) ";
                //ExecSQL(conn_db, sqlText, true);

                if (setTaskProgress != null)
                {
                    setTaskProgress(((decimal) 9)/10);
                }


                //----------------------- СОХРАНЕНИЕ -----------------------

                //Марат сказал миллионные оплаты скинуть в корзину
                sqlText = "update t_opl set kod_info = 0 where abs(sum_prih) > 1000000";
                retres = ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log);
                ret = retres.GetReturnsType().GetReturns();
                if (!ret.result)
                {
                    DropTempTablesPack(conn_db);
                    MessageInPackLog(conn_db, packXX, "Ошибка распределения 2.6.1", true, out r);
                    return false;
                }

#if PG
                count = retres.resultAffectedRows;
#else
                count = ClassDBUtils.GetAffectedRowsCount(conn_db);
#endif
                if (count > 0)
                {
                    MessageInPackLog(conn_db, packXX, "Слишком большая распределенная сумма", true, out r);
                    count_err = count_err + 1;
                }
                //------------------------------------------------


                #region Контроль соответствия типа пачки и квитанции об оплате
                sqlText = "select 1 from " + packXX.pack_ls + " where nzp_pack = " + packXX.nzp_pack +
                    " and kod_sum " + (pack_type == 10 ? "<>" : "=") + " 33 limit 1";
                ret = ExecRead(conn_db, out reader, sqlText, true);
                if (!ret.result)
                {
                    ret.result = false;
                    MonitorLog.WriteLog(ret.text, MonitorLog.typelog.Error, 1, 222, true);
                    return false;
                }
                if (reader.Read())
                {
                    sqlText =
                       " update t_opl pls set kod_info = 0 " +
                       " Where " + 
                       "   exists (select 1 from " + packXX.pack_ls + " pls1 where pls1.nzp_pack = " + packXX.nzp_pack +
                    " and pls1.kod_sum " + (pack_type == 10 ? "<>" : "=") + " 33 and pls.nzp_pack_ls = pls1.nzp_pack_ls)";
                    ret21 = ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log);
                    ret = ret21.GetReturnsType().GetReturns();
                    if (!ret.result)
                    {
                        DropTempTablesPack(conn_db);
                        MessageInPackLog(conn_db, packXX, "Ошибка распределения ", true, out r);
                        return false;
                    }
                    MessageInPackLog(conn_db, packXX, "В корзину поместить квитанции, которые не соответствуют типу пачки", true, out r);

                }
                reader.Close();
                
                if (pack_type != 20)
                {
                    #region скинуть оплату с 35 кодом и не 20 пачкой в корзину
                    sqlText = "update t_opl set kod_info = 0 where kod_sum=35 ";
                    retres = ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log);
                    ret = retres.GetReturnsType().GetReturns();
                    if (!ret.result)
                    {
                        DropTempTablesPack(conn_db);
                        MessageInPackLog(conn_db, packXX, "Ошибка распределения 2.6.1", true, out r);
                        return false;
                    }

#if PG
                    count = retres.resultAffectedRows;
#else
                count = ClassDBUtils.GetAffectedRowsCount(conn_db);
#endif
                    if (count > 0)
                    {
                        MessageInPackLog(conn_db, packXX,
                            "Тип пачки " + pack_type + " не соответствует 35 коду квитанции", true, out r);
                        count_err = count_err + 1;
                    }
                    #endregion

                    #region скинуть оплату с 40 кодом и не 20 пачкой в корзину
                    sqlText = "update t_opl set kod_info = 0 where kod_sum=40 ";
                    retres = ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log);
                    ret = retres.GetReturnsType().GetReturns();
                    if (!ret.result)
                    {
                        DropTempTablesPack(conn_db);
                        MessageInPackLog(conn_db, packXX, "Ошибка распределения 2.6.1", true, out r);
                        return false;
                    }

#if PG
                    count = retres.resultAffectedRows;
#else
                count = ClassDBUtils.GetAffectedRowsCount(conn_db);
#endif
                    if (count > 0)
                    {
                        MessageInPackLog(conn_db, packXX,
                            "Тип пачки " + pack_type + " не соответствует 40 коду квитанции", true, out r);
                        count_err = count_err + 1;
                    }
                    #endregion
                }
                #endregion
                //---------------------------------------------------

                #region Сохранение распределения
                // Предварительно очистить ранее распределённые оплаты по выбранным квитанциям
                ret = DeletePrevDistribution(conn_db, paramcalc.pref, paramcalc.calc_yy, paramcalc.calc_mm);
                if (!ret.result)
                {
                    DropTempTablesPack(conn_db);
                    MessageInPackLog(conn_db, packXX, (ret.tag == -1) ? ret.text : "Ошибка распределения 1.4.1.2", true, out r);
                    return false;
                }
                
#if PG
                if (pack_type == 20)
                {
                    string tableSupplier = paramcalc.pref + "_charge_" +
                                           (paramcalc.calc_yy.ToString("yyyy").Substring(2, 2)) + "." + "from_supplier";
                    ExecByStep(conn_db, "t_opl a", "a.nzp_key",
                        " Insert into " + packXX.fn_supplier +
                        " ( nzp_charge, num_ls, nzp_pack_ls, nzp_serv, nzp_supp, sum_prih, kod_sum, dat_month, dat_prih, dat_uchet, calc_month) " +
                        " Select a.kod_info, a.num_ls, a.nzp_pack_ls, a.nzp_serv, a.nzp_supp, a.sum_prih,  a.kod_sum, a.dat_month, a.dat_vvod, " +
                        packXX.paramcalc.dat_oper + ", cast('" + (new DateTime(dat_month_local.year_, dat_month_local.month_, 1)).ToShortDateString() + "' as DATE) " +
                        " From t_opl a, t_selkvar b " +
                        " Where  a.kod_info > 0 and abs(a.sum_prih) > 0.0001 and a.nzp_pack_ls = b.nzp_pack_ls ",
                        1000000, "", out ret);
                }
                else
                {
                    string tableSupplier = paramcalc.pref + "_charge_" +
                                           (paramcalc.calc_yy.ToString("yyyy").Substring(2, 2)) + "." + "fn_supplier" +
                                           paramcalc.calc_mm.ToString("00");
                    ExecByStep(conn_db, "t_opl a", "a.nzp_key",
                        " Insert into " + packXX.fn_supplier +
                        " ( nzp_charge, num_ls, nzp_pack_ls, nzp_serv, nzp_supp, sum_prih, kod_sum, dat_month, dat_prih, dat_uchet, s_dolg, s_user, s_forw, calc_month) " +
                        " Select a.kod_info, a.num_ls, a.nzp_pack_ls, a.nzp_serv, a.nzp_supp, a.sum_prih,  a.kod_sum, a.dat_month, a.dat_vvod, " +
                        packXX.paramcalc.dat_oper + ", a.sum_prih_d+a.sum_prih_s, a.sum_prih_u, a.sum_prih_a, cast('" + (new DateTime(dat_month_local.year_, dat_month_local.month_, 1)).ToShortDateString() + "' as DATE)" +
                        " From t_opl a, t_selkvar b " +
                        " Where  a.kod_info > 0 and abs(a.sum_prih) > 0.0001 and a.nzp_pack_ls = b.nzp_pack_ls ",
                        1000000, "", out ret);
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
                #endregion

#if PG
                sqlText =
                    " Update " + packXX.pack_ls +
                    " Set dat_uchet = " + packXX.paramcalc.dat_oper +
                    ", calc_month = cast('" + (new DateTime(dat_month_local.year_, dat_month_local.month_, 1)).ToShortDateString() + "' as DATE), " +
                    "date_distr = now(), inbasket =0, nzp_user = " + nzp_user +
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
                    ret2.result = false;
                    ret2.text = "Распределение не было произведено";
                    ret2.tag = -1;
                    MessageInPackLog(conn_db, packXX, ret2.text, true, out r);
                    //return false;
                }
                else
                {
                    #region Сохранить показания приборов учёта

                    SaveCountersValsFromPackls(conn_db, nzp_user, paramcalc, 0, out ret);

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
                        ret.result = false;
                        ret.text = "Распределение было произведено не полностью";
                        ret.tag = -1;
                        //return true;
                        MessageInPackLog(conn_db, packXX, ret.text, true, out r);
                    }

                    // Сохранить запись в журнал событий
                    int nzp_event = SaveEvent(6611, conn_db, paramcalc.nzp_user, paramcalc.nzp_pack,
                        "Операционный день " + packXX.paramcalc.dat_oper);

                    if ((nzp_event > 0) && (isDebug))
                    {
                        ret = ExecSQL(conn_db,
                            "insert into " + Points.Pref + "_data" + tableDelimiter +
                            "sys_event_detail(nzp_event, table_, nzp) select distinct " + nzp_event + ",'" +
                            packXX.pack_ls + "',nzp_pack_ls from t_opl where kod_info>0 ", true);
                    }


                }

                #region сохранение  t_opl_log
                if (Points.packDistributionParameters.EnableLog & packXX.nzp_pack_ls > 0)
                {
                    // Сохранить вспомогательную таблицу t_opl в базе
                    sqlText =
                        " delete from t_opl_log where nzp_pack_ls =  " + packXX.nzp_pack_ls;
                    ret =
                        ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log).GetReturnsType().GetReturns();
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
                        ret =
                            ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log)
                                .GetReturnsType()
                                .GetReturns();
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
                    ret =
                        ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log).GetReturnsType().GetReturns();
                }
                #endregion

                //определить нераспределенные оплаты

                sqlText =
                    " Update " + packXX.pack_ls +
                    " Set inbasket = 1, dat_uchet = null, calc_month=null, alg =0, nzp_user = " + nzp_user +
                    " Where nzp_pack =  " + packXX.nzp_pack +
                    "   and num_ls <= 0  and inbasket = 0  " + wherenzp_pack_ls;
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
                    MessageInPackLog(conn_db, packXX,
                        "В корзину положить оплаты из пачки у которых не определен лицевой счет и они еще не в корзине",
                        true, out r);
                    count_err = count_err + 1;
                }

                /*
                                sqlText =
                                    " Update " + packXX.pack_ls +
                                    " Set inbasket = 1, dat_uchet = null, alg=0, nzp_user = " + nzp_user +
                                    " Where nzp_pack_ls  in ( Select nzp_pack_ls From t_selkvar where coalesce(num_ls,0) <0  ) ";
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
                */
                sqlText =
                    " Update " + packXX.pack_ls +
                    " Set inbasket = 1, dat_uchet = null, calc_month=null, alg =0, nzp_user = " + nzp_user +
                    " Where " +
                    "    nzp_pack_ls  in ( Select nzp_pack_ls From t_opl where kod_info =0  ) ";
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
                    MessageInPackLog(conn_db, packXX,
                        "В корзину положить оплаты, которым не выставился алгоритм распределения", true, out r);
                    count_err = count_err + 1;
                }


                //пока комментирую этот Update
                sqlText =
                    " Update " + packXX.pack_ls +
                    " Set inbasket = 1, dat_uchet = null, calc_month=null, alg =0, nzp_user = " + nzp_user +
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
                        packXX.paramcalc.nzp_wp +
                        ", 'отсутствуют сумма входящего сальдо по закрытому лицевому счёту', 1001 " +
                        " From t_selkvar " +
                        " Where nzp_kvar not in ( Select nzp_kvar From t_charge where sum_insaldo<>0  ) and kod_sum in (" +
                        strKodSumForCharge_MM + ",81) and is_open =  0 ";
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
                        " Where nzp_kvar not in ( Select nzp_kvar From t_charge where sum_charge>0 or sum_insaldo>0 ) and kod_sum in (" +
                        strKodSumForCharge_MM + ",81) and is_open = 1  ";
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
                        " Where nzp_kvar not in ( Select nzp_kvar From t_charge where sum_charge>0  ) and kod_sum in (" +
                        strKodSumForCharge_MM + ",81) and is_open =  0 ";
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
                        " Where nzp_kvar not in ( Select nzp_kvar From t_charge where sum_charge>0 or rsum_tarif>0 ) and kod_sum in (" +
                        strKodSumForCharge_MM + ",81) and is_open = 1  ";
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
                        ret2.result = false;
                        ret2.text =
                            "Распределение не было произведено. Нераспределённая оплата была помещена в корзину. ";
                    }
                    else
                    {
                        ret2.result = false;
                        ret2.text =
                            "Распределение не было произведено. Нераспределённые оплаты были помещены в корзину. ";
                    }
                    ret2.tag = -1;
                    //return false;

                }

                #region Заполнить комментарии к ошибкам нераспределённых оплат



                // 0. Недопустимый код квитанции 
#if PG
                ret = ExecSQL(conn_db,
                    " Insert into " + Points.Pref + "_fin_" + (Points.DateOper.Year%100).ToString("00") +
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
                    ret = ExecSQL(conn_db,
                        " Insert into " + Points.Pref + "_fin_" + (Points.DateOper.Year%100).ToString("00") +
                        tableDelimiter + "pack_ls_err ( nzp_pack_ls,nzp_err,note ) " +
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
                        " Where nzp_kvar not in ( Select nzp_kvar From t_itog where sum_insaldo>0 or rsum_tarif>0 ) and is_open = 0 and kod_sum not in (" +
                        strKodSumForCharge_X + ")"
                        , true);
                    if (!ret.result)
                    {
                        DropTempTablesPack(conn_db);
                        return false;
                    }

                    ret = ExecSQL(conn_db,
                        " Insert into " + Points.Pref + "_fin_" + (Points.DateOper.Year%100).ToString("00") +
                        tableDelimiter + "pack_ls_err ( nzp_pack_ls,nzp_err,note ) " +
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
                        " From t_opl " +
                        " Where kod_info = 0 and isum_charge=0 and irsum_tarif=0"
                        , true);
                    if (!ret.result)
                    {
                        DropTempTablesPack(conn_db);
                        return false;
                    }
                    //3.ошибка - отсутствие начислено к оплате по закрытому лицевому счёту
#if PG
                    sqlText = " Insert into " + Points.Pref + "_fin_" + (Points.DateOper.Year%100).ToString("00") +
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
                              " Where  nzp_kvar not in ( Select nzp_kvar From t_charge where sum_insaldo>0 or rsum_tarif>0 ) and kod_sum not in (" +
                              strKodSumForCharge_X + ") and " +
                              "nzp_pack_ls not in ( Select nzp_pack_ls From " + Points.Pref + "_fin_" +
                              (Points.DateOper.Year%100).ToString("00") +
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
                        " Insert into " + Points.Pref + "_fin_" + (Points.DateOper.Year%100).ToString("00") +
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
                        " Where nzp_kvar not in ( Select nzp_kvar From t_itog where sum_charge>0 ) and is_open = 0 and kod_sum not in (" +
                        strKodSumForCharge_X + ")"
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
                    sqlText = " Insert into " + Points.Pref + "_fin_" + (Points.DateOper.Year%100).ToString("00") +
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
                              " Where  nzp_kvar not in ( Select nzp_kvar From t_charge where sum_charge>0 or rsum_tarif>0 ) and kod_sum not in (" +
                              strKodSumForCharge_X + ") and " +
                              "nzp_pack_ls not in ( Select nzp_pack_ls From " + Points.Pref + "_fin_" +
                              (Points.DateOper.Year%100).ToString("00") +
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

                // 4. запрет на распределение на лицевые счета со статусом неопределено 
                sqlText =
                    " Insert into " + Points.Pref + "_fin_" + (Points.DateOper.Year%100).ToString("00") +
                    ".pack_ls_err ( nzp_pack_ls,nzp_err,note ) " +
                    " Select nzp_pack_ls, 99,'Отказано в распределении на л/сч со статусом Неопределено'" +
                    " From t_selkvar " +
                    " Where nzp_kvar in ( Select nzp_kvar From t_opl where kod_info =0 ) and nzp_pack_ls not in (select a.nzp_pack_ls from  " +
                    Points.Pref + "_fin_" + (Points.DateOper.Year%100).ToString("00") + tableDelimiter +
                    "pack_ls_err a where a.nzp_pack_ls = nzp_pack_ls)";

                ret = ExecSQL(conn_db, sqlText, true);

                sqlText =
                    " Insert into " + Points.Pref + "_fin_" + (Points.DateOper.Year%100).ToString("00") +
                    ".pack_ls_err ( nzp_pack_ls,nzp_err,note ) " +
                    " Select nzp_pack_ls, 99,'Отсутствует шаблон к распределению'" +
                    " From t_selkvar " +
                    " Where nzp_kvar in ( Select nzp_kvar From t_opl where kod_info >0 ) and nzp_pack_ls not in (select a.nzp_pack_ls from t_iopl a where a.sum_prih<>0) ";

                ret = ExecSQL(conn_db, sqlText, true);

                if (!ret.result)
                {
                    DropTempTablesPack(conn_db);
                    return false;
                }


                sqlText =
                    " Update " + packXX.pack_ls +
                    " Set alg = 0, " +
                    " dat_uchet = null, calc_month = null , inbasket = 1 " +
                    " Where nzp_pack_ls in ( Select nzp_pack_ls From t_opl Where kod_info =177 )  ";
                ret21 = ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log);
                ret = ret21.GetReturnsType().GetReturns();
                if (!ret.result)
                {
                    DropTempTablesPack(conn_db);
                    MessageInPackLog(conn_db, packXX, "Ошибка распределения 2.11", true, out r);
                    return false;
                }

#if PG
                cnt = ret21.resultAffectedRows;
#else
                cnt = ClassDBUtils.GetAffectedRowsCount(conn_db);
#endif
                if (cnt > 0)
                {
                    MessageInPackLog(conn_db, packXX, "В корзину поместить оплаты с ЛС со статусом не определен", true,
                        out r);
                }

                sqlText =
                    " Update " + packXX.pack_ls +
                    " Set alg = 0, " +
                    " dat_uchet = null, calc_month = null , inbasket = 1 " +
                    " Where inbasket = 0 and nzp_pack_ls in ( Select nzp_pack_ls From t_opl where kod_info >0 and kod_info <> 115) " +
                    "and nzp_pack_ls not in (select a.nzp_pack_ls from t_iopl a where a.sum_prih<>0 or g_sum_ls = 0) ";

                ret21 = ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log);
                ret = ret21.GetReturnsType().GetReturns();
                if (!ret.result)
                {
                    DropTempTablesPack(conn_db);
                    MessageInPackLog(conn_db, packXX, "Ошибка распределения 2.11", true, out r);
                    return false;
                }

#if PG
                cnt = ret21.resultAffectedRows;
#else
                cnt = ClassDBUtils.GetAffectedRowsCount(conn_db);
#endif
                if (cnt > 0)
                {
                    MessageInPackLog(conn_db, packXX,
                        "В корзину поместить оплаты, которые не удалось автоматически распределить", true, out r);
                }
                sqlText =
                    " Update " + packXX.pack_ls +
                    " Set alg = 0, " +
                    " dat_uchet = null, calc_month=null , inbasket = 1 " +
                    " Where inbasket = 0 and nzp_pack_ls in ( Select nzp_pack_ls From t_opl where kod_info =0 )" +
                    " and nzp_pack_ls not in (select a.nzp_pack_ls from t_iopl a where a.sum_prih<>0) ";

                ret21 = ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log);
                ret = ret21.GetReturnsType().GetReturns();
                if (!ret.result)
                {
                    DropTempTablesPack(conn_db);
                    MessageInPackLog(conn_db, packXX, "Ошибка распределения 2.11", true, out r);
                    return false;
                }

#if PG
                cnt = ret21.resultAffectedRows;
#else
                cnt = ClassDBUtils.GetAffectedRowsCount(conn_db);
#endif
                if (cnt > 0)
                {
                    MessageInPackLog(conn_db, packXX,
                        "В корзину поместить оплаты, которые не удалось автоматически распределить ", true, out r);
                }
                // 4. Отказано в распределении
#if PG
                sqlText =
                    " Insert into " + Points.Pref + "_fin_" + (Points.DateOper.Year%100).ToString("00") +
                    ".pack_ls_err ( nzp_pack_ls,nzp_err,note ) " +
                    " Select nzp_pack_ls, 6,'Подробности см.в журнале распределения' " +
                    " From t_selkvar " +
                    " Where nzp_kvar in ( Select nzp_kvar From t_opl where kod_info =0 ) and nzp_pack_ls not in (select a.nzp_pack_ls from  " +
                    Points.Pref + "_fin_" + (Points.DateOper.Year%100).ToString("00") +
                    ".pack_ls_err a where a.nzp_pack_ls = nzp_pack_ls)";
#else
                sqlText =
                    " Insert into " + Points.Pref + "_fin_" + (Points.DateOper.Year % 100).ToString("00") + "@" + DBManager.getServer(conn_db) +
                    ":pack_ls_err ( nzp_pack_ls,nzp_err,note ) " +
                    " Select nzp_pack_ls, 6,'Подробности см.в журнале распределения' " +
                    " From t_selkvar " +
                    " Where nzp_kvar in ( Select nzp_kvar From t_opl where kod_info  = 0 ) and nzp_pack_ls not in (select a.nzp_pack_ls from  " +
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

                sqlText =
                    " Update " + packXX.pack_ls +
                    " Set alg = 9, " +
                    " distr_month = '" + distr_month + "'" +
                    " Where nzp_pack_ls in ( Select nzp_pack_ls From t_opl Where kod_info =115 ) and inbasket =0 and dat_uchet is not null ";
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
            /*
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
            */
            //DistribPaXX(conn_db, paramcalc, out ret);

            /*  sqlText =
                   " Update " + packXX.pack_ls +
                   " Set inbasket = 1, dat_uchet = null, alg =0, nzp_user = " + nzp_user +
                   " Where nzp_pack =  " + packXX.nzp_pack +
                   "   and alg ='0' and dat_uchet is null    ";
               ret = ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log).GetReturnsType().GetReturns();
               if (!ret.result)
               {
                   DropTempTablesPack(conn_db);
                   MessageInPackLog(conn_db, packXX, "Ошибка распределения 2.10", true, out r);
                   return false;
               }
               */
            if (Constants.Trace) Utility.ClassLog.WriteLog("Стоп функции LoadLocalPaXX. Старт функции CalcSaldoPack");

            //сразу запустить расчет сальдо по nzp_pack
            MessageInPackLog(conn_db, packXX, "Расчет сальдо лицевых счетов", false, out r);
            DbCalcCharge dbc = new DbCalcCharge();
            ret = dbc.CalcChargeXXUchetOplatForPack(conn_db, packXX);
            dbc.Close();
            if (!ret.result)
            {
                DropTempTablesPack(conn_db);
                return false;
            }

            if (setTaskProgress != null)
            {
                setTaskProgress(((decimal) 10)/10);
            }


            MessageInPackLog(conn_db, packXX, "Конец распределения", false, out r);
            DropTempTablesPack(conn_db);

            if (ret2.tag == -1)
            {
                ret.result = ret2.result;
                ret.tag = ret2.tag;
                ret.text = ret2.text;
            }
            if (Constants.Trace) Utility.ClassLog.WriteLog("Конец функции CalcPackXX");
            return true;
        }

        /// <summary>
        /// Корректировка эталонных сумм, с учетом ранее распределенных оплат
        /// </summary>
        private Returns UchetPrevOplats(IDbConnection conn_db, CalcTypes.ParamCalc paramcalc, CalcTypes.PackXX packXX)
        {
            //определить ранее распределённые оплаты по услугам 
            var tableFnSupplier = new StringBuilder();
            tableFnSupplier.AppendFormat("{0}_charge_{1}{2}fn_supplier{3}", paramcalc.pref,
                (paramcalc.calc_yy.ToString("0000").Substring(2, 2)), tableDelimiter, paramcalc.calc_mm.ToString("00"));
            var tableFromSupplier = new StringBuilder();
            tableFromSupplier.AppendFormat("{0}_charge_{1}{2}from_supplier", paramcalc.pref,
                (paramcalc.calc_yy.ToString("0000").Substring(2, 2)), tableDelimiter);

            Returns ret = Utils.InitReturns();
            if (paramcalc.calc_yy == Points.DateOper.Year && paramcalc.calc_mm == Points.DateOper.Month) return ret;
            var dat = new DateTime(paramcalc.calc_yy, paramcalc.calc_mm, 1);

            //оптимизация запроса
            //sql = new StringBuilder();
            //sql.AppendFormat(" update t_charge set sum_money = {1}((select sum(sum_prih) from  {0} a ", tableFnSupplier,
            //    sNvlWord);
            //sql.Append(
            //    " where t_charge.num_ls = a.num_ls and t_charge.nzp_serv = a.nzp_serv and t_charge.nzp_supp = a.nzp_supp ),0) + ");
            //sql.AppendFormat(" {1}((select sum(sum_prih) from {0} a ", tableFromSupplier, sNvlWord);
            //sql.AppendFormat(
            //    " where t_charge.num_ls = a.num_ls and t_charge.nzp_serv = a.nzp_serv and t_charge.nzp_supp = a.nzp_supp ");
            //sql.AppendFormat(" and dat_uchet between '{0}' and '{1}'), 0)", dat.ToShortDateString(),
            //    (new DateTime(paramcalc.calc_yy, paramcalc.calc_mm, DateTime.DaysInMonth(dat.Year, dat.Month)))
            //        .ToShortDateString());
            //ret = ClassDBUtils.ExecSQL(sql.ToString(), conn_db, ClassDBUtils.ExecMode.Log).GetReturnsType().GetReturns();
            //if (!ret.result) return ret;

            ExecSQL(conn_db, " Drop table tprevsums ", false);
            sql = new StringBuilder("create temp table tprevsums(sum_prih numeric(14,2) default 0, nzp_serv integer, nzp_supp integer, num_ls integer)");
            ret = ClassDBUtils.ExecSQL(sql.ToString(), conn_db, ClassDBUtils.ExecMode.Log).GetReturnsType().GetReturns();
            if (!ret.result) return ret;

            sql = new StringBuilder("insert into tprevsums(sum_prih, nzp_serv, nzp_supp, num_ls)");
            sql.AppendFormat(" select sum(sum_prih), a.nzp_serv, a.nzp_supp, a.num_ls from {0} a ", tableFnSupplier);
            sql.Append(" group by 2,3,4 ");
            ret = ClassDBUtils.ExecSQL(sql.ToString(), conn_db, ClassDBUtils.ExecMode.Log).GetReturnsType().GetReturns();
            if (!ret.result) return ret;

            sql = new StringBuilder("insert into tprevsums(sum_prih, nzp_serv, nzp_supp, num_ls)");
            sql.AppendFormat(" select sum(sum_prih), a.nzp_serv, a.nzp_supp, a.num_ls from {0} a ", tableFromSupplier);
            sql.AppendFormat(" where dat_uchet between '{0}' and '{1}' ", dat.ToShortDateString(),
                (new DateTime(paramcalc.calc_yy, paramcalc.calc_mm, DateTime.DaysInMonth(dat.Year, dat.Month)))
                    .ToShortDateString());
            sql.Append(" group by 2,3,4 ");
            ret = ClassDBUtils.ExecSQL(sql.ToString(), conn_db, ClassDBUtils.ExecMode.Log).GetReturnsType().GetReturns();
            if (!ret.result) return ret;

            ExecSQL(conn_db, " Drop table ttprevsums ", false);
            sql = new StringBuilder("create temp table ttprevsums(sum_prih numeric(14,2) default 0, nzp_serv integer, nzp_supp integer, num_ls integer)");
            ret = ClassDBUtils.ExecSQL(sql.ToString(), conn_db, ClassDBUtils.ExecMode.Log).GetReturnsType().GetReturns();
            if (!ret.result) return ret;

            sql = new StringBuilder("insert into ttprevsums (sum_prih, nzp_serv, nzp_supp, num_ls)");
            sql.Append(" select sum(sum_prih), nzp_serv, nzp_supp, num_ls from tprevsums ");
            sql.Append(" group by 2,3,4 ");
            ret = ClassDBUtils.ExecSQL(sql.ToString(), conn_db, ClassDBUtils.ExecMode.Log).GetReturnsType().GetReturns();
            if (!ret.result) return ret;

            sql = new StringBuilder("create index ix_ttprevsums on ttprevsums(nzp_serv, nzp_supp, num_ls)");
            ret = ClassDBUtils.ExecSQL(sql.ToString(), conn_db, ClassDBUtils.ExecMode.Log).GetReturnsType().GetReturns();
            if (!ret.result) return ret;

            sql = new StringBuilder(" update t_charge set ");
            sql.Append(" sum_money = coalesce(a.sum_prih,0) ");
            sql.Append(" from ttprevsums a ");
            sql.Append(" where t_charge.num_ls = a.num_ls and t_charge.nzp_serv = a.nzp_serv and t_charge.nzp_supp = a.nzp_supp ");
            ret = ClassDBUtils.ExecSQL(sql.ToString(), conn_db, ClassDBUtils.ExecMode.Log).GetReturnsType().GetReturns();
            if (!ret.result) return ret;

            ExecSQL(conn_db, " Drop table tprevsums ", false);
            ExecSQL(conn_db, " Drop table ttprevsums ", false);

            //рассчитать новые суммы sum_charge и sum_outsaldo, sum_insaldo по услугам 
            sql = new StringBuilder(" update t_charge set sum_insaldop = sum_insaldo,");
            sql.Append(" sum_insaldo = sum_insaldo - coalesce(sum_money,0), ");
            sql.Append(" sum_outsaldo_prev = sum_outsaldo, ");
            sql.Append(" sum_outsaldo = sum_outsaldo-coalesce(sum_money,0), ");
            sql.Append(" sum_charge_prev = sum_charge, ");
            sql.Append(" sum_charge = sum_charge - coalesce(sum_money,0) ");
            IntfResultType retres = ClassDBUtils.ExecSQL(sql.ToString(), conn_db, ClassDBUtils.ExecMode.Log);
            ret = retres.GetReturnsType().GetReturns();
            if (!ret.result) return ret;

            int count = retres.resultAffectedRows;

            if (Points.packDistributionParameters.EnableLog & count > 0 & packXX.nzp_pack_ls > 0)
            {
                decimal s1 = 0;
                Returns r;

                sql = new StringBuilder("select sum(sum_money) as sum_ from t_charge ");
                DataTable dt = ClassDBUtils.OpenSQL(sql.ToString(), conn_db).GetData();
                DataRow row = dt.Rows[0];
                if (row["sum_"] != DBNull.Value) Decimal.TryParse(row["sum_"].ToString(), out s1);
                if (s1 != 0)
                {
                    MessageInPackLog(conn_db, packXX,
                        "Ранее распределено по лицевому счёту: " + s1.ToString("0.00") + " руб", false, out r);

                    sql = new StringBuilder("select sum(sum_charge) as sum_charge, ");
                    sql.Append(" sum(sum_insaldo) as sum_insaldo, ");
                    sql.Append(" sum(case when sum_charge>0 then sum_charge else 0 end) as sum_charge_plus,");
                    sql.Append(" sum(sum_outsaldo) as sum_outsaldo, ");
                    sql.Append(" sum(case when sum_outsaldo>0 then sum_outsaldo else 0 end) as sum_outsaldo_plus ");
                    sql.Append(" from t_charge ");
                    dt = ClassDBUtils.OpenSQL(sql.ToString(), conn_db).GetData();
                    row = dt.Rows[0];

                    if (isDistributionForInSaldo)
                    {
                        s1 = 0;
                        if (row["sum_insaldo"] != DBNull.Value) Decimal.TryParse(row["sum_insaldo"].ToString(), out s1);
                        if (s1 != 0)
                            MessageInPackLog(conn_db, packXX,
                                "Откорректирована сумма 'Входящее сальдо' с учётом ранее поступивших платежей: " +
                                s1.ToString("0.00") + " руб", false, out r);
                    }
                    else
                    {
                        s1 = 0;
                        if (row["sum_outsaldo"] != DBNull.Value)
                            Decimal.TryParse(row["sum_outsaldo"].ToString(), out s1);
                        if (s1 != 0)
                            MessageInPackLog(conn_db, packXX,
                                "Откорректирована сумма 'Исходящее сальдо' с учётом ранее поступивших платежей: " +
                                s1.ToString("0.00") + " руб", false, out r);
                    }

                    s1 = 0;
                    if (row["sum_charge"] != DBNull.Value) Decimal.TryParse(row["sum_charge"].ToString(), out s1);
                    if (s1 != 0)
                    {
                        MessageInPackLog(conn_db, packXX,
                            "Откорректирована сумма 'Начислено к оплате' с учётом ранее поступивших платежей: " +
                            s1.ToString("0.00") + " руб", false, out r);
                    }
                }
            }

            return ret;
        }

        //-----------------------------------------------------------------------------
        private bool OplSamara(IDbConnection conn_db, CalcTypes.PackXX packXX, out Returns ret)
            //-----------------------------------------------------------------------------
        {
            DataTable dt;
            DataRow row;

            string s1, s2;
            int count = 0;

            // САМАРА
            ret = Utils.InitReturns();
            Returns r = Utils.InitReturns();

            if (Points.packDistributionParameters.EnableLog)
            {
                MessageInPackLog(conn_db, packXX,
                    "Установлена специальная стратегия распределения (по входящему сальдо)", false, out r);
            }

            ExecSQL(conn_db, " Create index ix2_t_opl on t_opl (kod_info) ", true);

#if PG
            ExecSQL(conn_db, " analyze t_opl ", true);
#else
                ExecSQL(conn_db, " Update statistics for table t_opl ", true);
#endif


            #region Распределить оплаты у которых исходящее сальдо = сумме оплаты внезависимости от знака

            // -------------------------------------------------------------------------------------------------------------------------------------------------------
#if PG
            sqlText =
                "update t_opl set isum_insaldo = (select coalesce(sum_insaldo,0) from t_itog a where a.nzp_kvar = t_opl.nzp_kvar and a.nzp_pack_ls = t_opl.nzp_pack_ls ),isum_charge = (select coalesce(sum_charge,0) from t_itog a where a.nzp_kvar = t_opl.nzp_kvar and a.nzp_pack_Ls = t_opl.nzp_pack_Ls  )    ";
#else
                    sqlText ="update t_opl set isum_insaldo = (select NVL(sum_insaldo,0) from t_itog a where a.nzp_kvar = t_opl.nzp_kvar and a.nzp_pack_ls = t_opl.nzp_pack_ls ),isum_charge = (select NVL(sum_charge,0) from t_itog a where a.nzp_kvar = t_opl.nzp_kvar and a.nzp_pack_Ls = t_opl.nzp_pack_Ls  )    ";
#endif
            ret = ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log).GetReturnsType().GetReturns();
            if (!ret.result)
            {
                DropTempTablesPack(conn_db);
                MessageInPackLog(conn_db, packXX, "Ошибка распределения 7.2", true, out r);
                Utility.ExceptionUtility.OnException(new Exception(ret.text),
                    System.Reflection.MethodBase.GetCurrentMethod().Name);
                return false;
            }

            sqlText = " Update t_opl " +
                      " Set kod_info = 110, " +
                      "  sum_prih_d = sum_insaldo" + // Погашение долга
                      " where isum_insaldo = (-1)*g_sum_ls and kod_sum in (" + strKodSumForCharge_MM +
                      ",81) and kod_info = 0  and znak=-1";
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
                Utility.ExceptionUtility.OnException(new Exception(ret.text),
                    System.Reflection.MethodBase.GetCurrentMethod().Name);
                return false;
            }
            ;

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
                Utility.ExceptionUtility.OnException(new Exception(ret.text),
                    System.Reflection.MethodBase.GetCurrentMethod().Name);
                return false;
            }
#if PG
            sqlText =
                "update t_opl set isum_insaldo = (select coalesce(sum_insaldo,0) from t_itog a where a.nzp_kvar = t_opl.nzp_kvar and a.nzp_pack_ls = t_opl.nzp_pack_ls ),isum_charge = (select coalesce(sum_charge,0) from t_itog a where a.nzp_kvar = t_opl.nzp_kvar and a.nzp_pack_Ls = t_opl.nzp_pack_Ls  )    ";
#else
                  sqlText = "update t_opl set isum_insaldo = (select NVL(sum_insaldo,0) from t_itog a where a.nzp_kvar = t_opl.nzp_kvar and a.nzp_pack_ls = t_opl.nzp_pack_ls ),isum_charge = (select NVL(sum_charge,0) from t_itog a where a.nzp_kvar = t_opl.nzp_kvar and a.nzp_pack_Ls = t_opl.nzp_pack_Ls  )    ";
#endif

            ret = ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log).GetReturnsType().GetReturns();
            if (!ret.result)
            {
                DropTempTablesPack(conn_db);
                MessageInPackLog(conn_db, packXX, "Ошибка распределения 7.2", true, out r);
                Utility.ExceptionUtility.OnException(new Exception(ret.text),
                    System.Reflection.MethodBase.GetCurrentMethod().Name);
                return false;
            }


            #region 1. Если входящее сальдо >0 и оплата = Входящему сальдо

            // Пометить такие оплаты
            sqlText = " Update t_opl " +
                      " Set kod_info = 101, " +
                      "  sum_prih_d = sum_insaldo" + // Погашение долга
                      " where isum_insaldo = g_sum_ls and kod_sum in (" + strKodSumForCharge_MM +
                      ",81) and kod_info = 0  ";
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
                Utility.ExceptionUtility.OnException(new Exception(ret.text),
                    System.Reflection.MethodBase.GetCurrentMethod().Name);
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
                    sqlText = "select SUM(sum_prih_d) sum_1 from t_opl where kod_info=101  and nzp_pack_ls=" +
                              packXX.nzp_pack_ls;
                    dt = ClassDBUtils.OpenSQL(sqlText, conn_db).GetData();
                    row = dt.Rows[0];
                    s1 = Convert.ToString(row["sum_1"]);
                    if (s1.Trim() != "0")
                    {
                        MessageInPackLog(conn_db, packXX,
                            "Алгоритм распределения № 1 (оплачено " + s1.Trim() + " руб) = входящее сальдо (" +
                            s1.Trim() + " руб)", true, out r);
                    }
                }
            }

            #endregion 1.

            #region 2. Если входящее сальдо >0 и оплата < Входящего сальдо

            // 2 Пометить такие оплаты
#if PG
            sqlText = " Update t_opl " +
                      " Set kod_info = 102, " +
                      "      sum_prih_d = " + Points.Pref +
                      "_kernel.getSumPrih(sum_insaldo,  g_sum_ls * (sum_insaldo/isum_insaldo), isdel )  " +
                      " where isum_insaldo > g_sum_ls and kod_sum in (" + strKodSumForCharge_MM +
                      ",81) and kod_info = 0  and isum_insaldo<>0 ";

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
                Utility.ExceptionUtility.OnException(new Exception(ret.text),
                    System.Reflection.MethodBase.GetCurrentMethod().Name);
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
                        MessageInPackLog(conn_db, packXX,
                            "Алгоритм распределения № 2 (оплачено " + s1.Trim() + " руб < входящее сальдо " + s2.Trim() +
                            ")", true, out r);
                        sqlText =
                            "select SUM(sum_prih_d) as sum_1, sum(sum_insaldo) as sum_2  from t_opl where kod_info=102  and nzp_pack_ls=" +
                            packXX.nzp_pack_ls;
                        dt = ClassDBUtils.OpenSQL(sqlText, conn_db).GetData();
                        row = dt.Rows[0];
                        s1 = Convert.ToString(row["sum_1"]);
                        s2 = Convert.ToString(row["sum_2"]);
                        if (s2.Trim() != "0")
                        {
                            MessageInPackLog(conn_db, packXX,
                                "Из " + s2 + " руб входящего сальдо погашено " + s1 + " руб", false, out r);
                        }
                    }

                }
            }

            #endregion 2.

            #region 3. Если входящее сальдо >0 и оплата > Входящего сальдо

            // 2 Пометить такие оплаты
            sqlText = " Update t_opl " +
                      " Set kod_info = 103 " +
                      " where isum_insaldo < g_sum_ls and kod_sum in (" + strKodSumForCharge_MM +
                      ",81) and kod_info = 0 and isum_insaldo >0 ";
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
                Utility.ExceptionUtility.OnException(new Exception(ret.text),
                    System.Reflection.MethodBase.GetCurrentMethod().Name);
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
                        MessageInPackLog(conn_db, packXX,
                            "Алгоритм распределения № 3 (оплачено " + s1.Trim() + " руб > входящее сальдо " + s2.Trim() +
                            ")", true, out r);
                    }
                }
            }

            // Погасить входящее сальдо
            sqlText = " Update t_opl " +
                      " Set sum_prih_d  = sum_insaldo " +
                      " where kod_sum in (" + strKodSumForCharge_MM + ",81) and kod_info = 103 ";
                //"and sum_insaldo >0  ";
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
                Utility.ExceptionUtility.OnException(new Exception(ret.text),
                    System.Reflection.MethodBase.GetCurrentMethod().Name);
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
                    sqlText =
                        "select SUM(sum_prih_d) as sum_1, sum(sum_insaldo) as sum_2  from t_opl where kod_info=103  and nzp_pack_ls=" +
                        packXX.nzp_pack_ls;
                    dt = ClassDBUtils.OpenSQL(sqlText, conn_db).GetData();
                    row = dt.Rows[0];
                    s1 = Convert.ToString(row["sum_1"]);
                    s2 = Convert.ToString(row["sum_2"]);
                    if (s2.Trim() != "0")
                    {
                        MessageInPackLog(conn_db, packXX,
                            "Погашение входящего сальдо " + s2 + " руб. Погашено " + s1 + " руб", false, out r);
                    }
                }
            }

            // Определить остаток от погашения
#if PG
            sqlText = " update t_ostatok set g_sum_ls_ost = " + Points.Pref +
                      "_kernel.getSumOstatok(nzp_pack_ls)  where nzp_pack_ls  in (select distinct nzp_pack_ls from t_opl where kod_info = 0)   ";
#else
                    sqlText = " update t_ostatok set g_sum_ls_ost = " + Points.Pref + "_kernel:getSumOstatok(nzp_pack_ls)   ";
#endif
            ret = ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log).GetReturnsType().GetReturns();
            if (!ret.result)
            {
                DropTempTablesPack(conn_db);
                MessageInPackLog(conn_db, packXX, "Ошибка распределения 8.4", true, out r);
                Utility.ExceptionUtility.OnException(new Exception(ret.text),
                    System.Reflection.MethodBase.GetCurrentMethod().Name);
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
                        MessageInPackLog(conn_db, packXX,
                            "Остаток от погашения " + s1 + " руб распределить по 'Начислено к оплате'", false, out r);
                    }
                }
            }



            sqlText = " Update t_opl " +
                      " SET  g_sum_ls_ost = (select g_sum_ls_ost from t_ostatok where t_ostatok.nzp_pack_ls = t_opl.nzp_pack_ls)" +
                      " where  kod_sum in (" + strKodSumForCharge_MM + ",81) and kod_info in (103,0)  and rsum_tarif>0";
            ret = ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log).GetReturnsType().GetReturns();
            if (!ret.result)
            {
                DropTempTablesPack(conn_db);
                MessageInPackLog(conn_db, packXX, "Ошибка распределения 8.5", true, out r);
                Utility.ExceptionUtility.OnException(new Exception(ret.text),
                    System.Reflection.MethodBase.GetCurrentMethod().Name);
                return false;
            }

            // Пометить оплаты по которым необходимо распределить по авансовую сумму
            sqlText = " update t_opl set kod_info = 106 where g_sum_ls_ost>0 and kod_info in (0,103)   ";
            ret = ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log).GetReturnsType().GetReturns();
            if (!ret.result)
            {
                DropTempTablesPack(conn_db);
                MessageInPackLog(conn_db, packXX, "Ошибка распределения 8.4", true, out r);
                Utility.ExceptionUtility.OnException(new Exception(ret.text),
                    System.Reflection.MethodBase.GetCurrentMethod().Name);
                return false;
            }

#if PG
            sqlText =
                " Update t_opl " +
                " Set  " +
                "      sum_prih_a = " + Points.Pref +
                "_kernel.getSumPrih(sum_charge,  g_sum_ls_ost * (sum_charge/isum_charge), isdel )  " +
                " where  g_sum_ls_ost > 0 and kod_sum in (" + strKodSumForCharge_MM +
                ",81) and kod_info in (103,106)  and isum_charge>0";
#else
            sqlText =
                    " Update t_opl " +
                    " Set  " +
                     "      sum_prih_a = " + Points.Pref + "_kernel:getSumPrih(sum_charge,  g_sum_ls_ost * (rsum_tarif/irsum_tarif), isdel )  " +
                    " where  g_sum_ls_ost > 0 and kod_sum in (" + strKodSumForCharge_MM + ",81) and kod_info in (103,106)  and irsum_tarif>0";
#endif
            ret = ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log).GetReturnsType().GetReturns();
            if (!ret.result)
            {
                DropTempTablesPack(conn_db);
                MessageInPackLog(conn_db, packXX, "Ошибка распределения 8.6", true, out r);
                Utility.ExceptionUtility.OnException(new Exception(ret.text),
                    System.Reflection.MethodBase.GetCurrentMethod().Name);
                return false;
            }
            else
            {
                if (Points.packDistributionParameters.EnableLog & count > 0 & packXX.nzp_pack_ls > 0)
                {
                    sqlText =
                        "select SUM(sum_prih_a) as sum_1  from t_opl where kod_info in (103,106)  and nzp_pack_ls=" +
                        packXX.nzp_pack_ls;
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
                Utility.ExceptionUtility.OnException(new Exception(ret.text),
                    System.Reflection.MethodBase.GetCurrentMethod().Name);
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
                        MessageInPackLog(conn_db, packXX,
                            "Остаток от погашения " + s1 + " руб распределить по 'Входящему сальдо'", false, out r);
                    }
                }
            }

            sqlText = " Update t_opl " +
                      " SET  g_sum_ls_ost = (select g_sum_ls_ost from t_ostatok where t_ostatok.nzp_pack_ls = t_opl.nzp_pack_ls)" +
                      " where  kod_sum in (" + strKodSumForCharge_MM +
                      ",81) and kod_info in (103,106)  and sum_insaldo>0";
            ret = ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log).GetReturnsType().GetReturns();
            if (!ret.result)
            {
                DropTempTablesPack(conn_db);
                MessageInPackLog(conn_db, packXX, "Ошибка распределения 8.5", true, out r);
                Utility.ExceptionUtility.OnException(new Exception(ret.text),
                    System.Reflection.MethodBase.GetCurrentMethod().Name);
                return false;
            }

#if PG
            sqlText =
                " Update t_opl " +
                " Set  " +
                "     sum_prih_a = sum_prih_a+" + Points.Pref +
                "_kernel.getSumPrih(sum_insaldo, g_sum_ls_ost * (sum_insaldo/isum_insaldo), isdel )  " +

                " where  g_sum_ls_ost > 0 and kod_sum in (" + strKodSumForCharge_MM +
                ",81) and kod_info in (103,106)  and sum_insaldo>0";
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
                Utility.ExceptionUtility.OnException(new Exception(ret.text),
                    System.Reflection.MethodBase.GetCurrentMethod().Name);
                return false;
            }
            else
            {
                if (Points.packDistributionParameters.EnableLog & count > 0 & packXX.nzp_pack_ls > 0)
                {
                    sqlText =
                        "select SUM(sum_prih_a) as sum_1  from t_opl where kod_info in (103,106)  and nzp_pack_ls=" +
                        packXX.nzp_pack_ls;
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
            sqlText =
                " update t_opl set kod_info = 107, sum_insaldo= sum_insaldop where  kod_info = 0   and sum_insaldo>0 ";
            ret = ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log).GetReturnsType().GetReturns();
            if (!ret.result)
            {
                DropTempTablesPack(conn_db);
                MessageInPackLog(conn_db, packXX, "Ошибка распределения 8.7", true, out r);
                Utility.ExceptionUtility.OnException(new Exception(ret.text),
                    System.Reflection.MethodBase.GetCurrentMethod().Name);
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
                Utility.ExceptionUtility.OnException(new Exception(ret.text),
                    System.Reflection.MethodBase.GetCurrentMethod().Name);
                return false;
            }
            sqlText =
                "update t_opl set isum_insaldo = (select sum_insaldo from t_itog a where a.nzp_kvar = t_opl.nzp_kvar and a.nzp_pack_ls = t_opl.nzp_pack_ls ) ";
            ret = ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log).GetReturnsType().GetReturns();
            if (!ret.result)
            {
                DropTempTablesPack(conn_db);
                MessageInPackLog(conn_db, packXX, "Ошибка распределения 8.9", true, out r);
                Utility.ExceptionUtility.OnException(new Exception(ret.text),
                    System.Reflection.MethodBase.GetCurrentMethod().Name);
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
                Utility.ExceptionUtility.OnException(new Exception(ret.text),
                    System.Reflection.MethodBase.GetCurrentMethod().Name);
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
                        MessageInPackLog(conn_db, packXX,
                            "Остаток от погашения " + s1 + " руб распределить по первоначальной сумме 'Входящее сальдо'",
                            false, out r);
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
                Utility.ExceptionUtility.OnException(new Exception(ret.text),
                    System.Reflection.MethodBase.GetCurrentMethod().Name);
                return false;
            }

#if PG
            sqlText =
                " Update t_opl " +
                " Set  " +
                "     sum_prih_s = " + Points.Pref +
                "_kernel.getSumPrih(sum_insaldop, g_sum_ls_ost * (sum_insaldop/isum_insaldo), isdel )  " +
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
                Utility.ExceptionUtility.OnException(new Exception(ret.text),
                    System.Reflection.MethodBase.GetCurrentMethod().Name);
                return false;
            }
            else
            {
                if (Points.packDistributionParameters.EnableLog & count > 0 & packXX.nzp_pack_ls > 0)
                {
                    sqlText = "select SUM(sum_prih_s) as sum_1  from t_opl where kod_info in (107)  and nzp_pack_ls=" +
                              packXX.nzp_pack_ls;
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
            sqlText =
                " update t_opl set kod_info = 108, sum_insaldo= sum_insaldop where  kod_info = 0 and sum_insaldop>0 ";
            ret = ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log).GetReturnsType().GetReturns();
            if (!ret.result)
            {
                DropTempTablesPack(conn_db);
                MessageInPackLog(conn_db, packXX, "Ошибка распределения 8.15", true, out r);
                Utility.ExceptionUtility.OnException(new Exception(ret.text),
                    System.Reflection.MethodBase.GetCurrentMethod().Name);
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
                Utility.ExceptionUtility.OnException(new Exception(ret.text),
                    System.Reflection.MethodBase.GetCurrentMethod().Name);
                return false;
            }
            sqlText =
                "update t_opl set isum_insaldo = (select sum_insaldo from t_itog a where a.nzp_kvar = t_opl.nzp_kvar and a.nzp_pack_ls = t_opl.nzp_pack_ls ) where  kod_info = 108";
            ret = ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log).GetReturnsType().GetReturns();
            if (!ret.result)
            {
                DropTempTablesPack(conn_db);
                MessageInPackLog(conn_db, packXX, "Ошибка распределения 8.17", true, out r);
                Utility.ExceptionUtility.OnException(new Exception(ret.text),
                    System.Reflection.MethodBase.GetCurrentMethod().Name);
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
                Utility.ExceptionUtility.OnException(new Exception(ret.text),
                    System.Reflection.MethodBase.GetCurrentMethod().Name);
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
                        MessageInPackLog(conn_db, packXX,
                            "Остаток от погашения " + s1 + " руб распределить по первоначальной сумме 'Входящее сальдо'",
                            false, out r);
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
                Utility.ExceptionUtility.OnException(new Exception(ret.text),
                    System.Reflection.MethodBase.GetCurrentMethod().Name);
                return false;
            }

#if PG
            sqlText =
                " Update t_opl " +
                " Set  " +
                "     sum_prih_s = " + Points.Pref +
                "_kernel.getSumPrih(sum_insaldop, g_sum_ls_ost * (sum_insaldop/isum_insaldo), isdel )  " +
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
                Utility.ExceptionUtility.OnException(new Exception(ret.text),
                    System.Reflection.MethodBase.GetCurrentMethod().Name);
                return false;
            }
            else
            {
                if (Points.packDistributionParameters.EnableLog & count > 0 & packXX.nzp_pack_ls > 0)
                {
                    sqlText = "select SUM(sum_prih_s) as sum_1  from t_opl where kod_info in (108)  and nzp_pack_ls=" +
                              packXX.nzp_pack_ls;
                    dt = ClassDBUtils.OpenSQL(sqlText, conn_db).GetData();
                    row = dt.Rows[0];
                    s1 = Convert.ToString(row["sum_1"]);
                    if (s1.Trim() != "0")
                    {
                        MessageInPackLog(conn_db, packXX,
                            "Остаток (" + s1 +
                            " руб) распределён по первоначальному входящему сальдо (положительной части)", false, out r);
                    }
                }
            }

            #endregion 5.

            #region 6. Распределить оплаты у которых откорректированное входящее сальдо <0, но есть положительное начислено к оплате

            // Пометить оплаты по которым необходимо распределить по первоначальной сумме входящего сальдо
            sqlText =
                " update t_opl set kod_info = 109, sum_charge= sum_charge_prev where  kod_info = 0 and sum_charge_prev>0 ";
            ret = ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log).GetReturnsType().GetReturns();
            if (!ret.result)
            {
                DropTempTablesPack(conn_db);
                MessageInPackLog(conn_db, packXX, "Ошибка распределения 8.21", true, out r);
                Utility.ExceptionUtility.OnException(new Exception(ret.text),
                    System.Reflection.MethodBase.GetCurrentMethod().Name);
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
                Utility.ExceptionUtility.OnException(new Exception(ret.text),
                    System.Reflection.MethodBase.GetCurrentMethod().Name);
                return false;
            }
            sqlText =
                "update t_opl set isum_charge = (select sum_charge from t_itog a where a.nzp_kvar = t_opl.nzp_kvar and a.nzp_pack_ls = t_opl.nzp_pack_ls ) where kod_info = 109";
            ret = ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log).GetReturnsType().GetReturns();
            if (!ret.result)
            {
                DropTempTablesPack(conn_db);
                MessageInPackLog(conn_db, packXX, "Ошибка распределения 8.17", true, out r);
                Utility.ExceptionUtility.OnException(new Exception(ret.text),
                    System.Reflection.MethodBase.GetCurrentMethod().Name);
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
                Utility.ExceptionUtility.OnException(new Exception(ret.text),
                    System.Reflection.MethodBase.GetCurrentMethod().Name);
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
                        MessageInPackLog(conn_db, packXX,
                            "Остаток от погашения " + s1 +
                            " руб распределить по первоначальной сумме 'Начислено к оплате'", false, out r);
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
                Utility.ExceptionUtility.OnException(new Exception(ret.text),
                    System.Reflection.MethodBase.GetCurrentMethod().Name);
                return false;
            }

#if PG
            sqlText =
                " Update t_opl " +
                " Set  " +
                "     sum_prih_s = " + Points.Pref +
                "_kernel.getSumPrih(sum_charge, g_sum_ls_ost * (sum_charge/isum_charge), isdel )  " +
                " where  g_sum_ls_ost > 0 and kod_sum in (" + strKodSumForCharge_MM +
                ",81) and kod_info in (109) and isum_charge>0 ";
#else
            sqlText =
                    " Update t_opl " +
                    " Set  " +
                     "     sum_prih_s = " + Points.Pref + "_kernel:getSumPrih(sum_charge, g_sum_ls_ost * (sum_charge/isum_charge), isdel )  " +
                    " where  g_sum_ls_ost > 0 and kod_sum in (" + strKodSumForCharge_MM + ",81) and kod_info in (109)   and isum_charge>0 ";
#endif
            ret = ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log).GetReturnsType().GetReturns();
            if (!ret.result)
            {
                DropTempTablesPack(conn_db);
                MessageInPackLog(conn_db, packXX, "Ошибка распределения 8.25", true, out r);
                Utility.ExceptionUtility.OnException(new Exception(ret.text),
                    System.Reflection.MethodBase.GetCurrentMethod().Name);
                return false;
            }
            else
            {
                if (Points.packDistributionParameters.EnableLog & count > 0 & packXX.nzp_pack_ls > 0)
                {
                    sqlText = "select SUM(sum_prih_s) as sum_1  from t_opl where kod_info in (109)  and nzp_pack_ls=" +
                              packXX.nzp_pack_ls;
                    dt = ClassDBUtils.OpenSQL(sqlText, conn_db).GetData();
                    row = dt.Rows[0];
                    s1 = Convert.ToString(row["sum_1"]);
                    if (s1.Trim() != "0")
                    {
                        MessageInPackLog(conn_db, packXX,
                            "Остаток (" + s1 +
                            " руб) распределён по первоначальному начислено к оплате (положительной части)", false,
                            out r);
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
        private bool StandartReparation(IDbConnection conn_db, CalcTypes.PackXX packXX, out Returns ret)
            //-----------------------------------------------------------------------------
        {
            // Cтандартная схема, погашение действующих поставщиков приоритетно
            Returns r = Utils.InitReturns();
            string sqlText;
            string s1, s2;
            DataTable dt;
            DataRow row;

            var fieldDenominator1 = "isum_charge";
            var fieldDenominator2 = "isum_charge";
            var fieldNumenator = "sum_charge";

            //пени
            var fieldDenominator_peni_1 = "isum_charge_peni";
            var fieldDenominator_peni_2 = "isum_charge_peni";
            var fieldDenominator_no_peni_1 = "isum_charge_no_peni";
            var fieldDenominator_no_peni_2 = "isum_charge_no_peni";
            var str_where_for_Koeff_peni_1 = " isum_charge_peni<>0 ";
            var str_where_for_Koeff_peni_2 = " isum_charge_peni<>0 ";
            var str_where_for_Koeff_no_peni_1 = " isum_charge_no_peni<>0 ";
            var str_where_for_Koeff_no_peni_2 = " isum_charge_no_peni<>0 ";
            var g_sum_ls_peni_1 = "ig_sum_ls_peni";
            var g_sum_ls_peni_2 = "ig_sum_ls_peni";
            var g_sum_ls_no_peni_1 = "ig_sum_ls_no_peni";
            var g_sum_ls_no_peni_2 = "ig_sum_ls_no_peni";
            //пени

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

            if (Points.packDistributionParameters.strategy ==
                PackDistributionParameters.Strategies.InactiveServicesFirst)
            {
                fieldDenominator1 = "isum_charge_isdel_1";
                fieldDenominator2 = "isum_charge_isdel_0";

                fieldNumenator = "sum_charge";

                str_where_for_Koeff1 = " isum_charge_isdel_1<>0 ";
                str_where_for_Koeff2 = " isum_charge_isdel_0<>0 ";

                //пени
                fieldDenominator_peni_1 = "isum_charge_peni_isdel_1";
                fieldDenominator_peni_2 = "isum_charge_peni_isdel_0";
                fieldDenominator_no_peni_1 = "isum_charge_no_peni_isdel_1";
                fieldDenominator_no_peni_2 = "isum_charge_no_peni_isdel_0";
                str_where_for_Koeff_peni_1 = " isum_charge_peni_isdel_1<>0 ";
                str_where_for_Koeff_peni_2 = " isum_charge_peni_isdel_0<>0 ";
                str_where_for_Koeff_no_peni_1 = " isum_charge_no_peni_isdel_1<>0 ";
                str_where_for_Koeff_no_peni_2 = " isum_charge_no_peni_isdel_0<>0 ";
                g_sum_ls_peni_1 = "ig_sum_ls_peni_isdel_1";
                g_sum_ls_peni_2 = "ig_sum_ls_peni_isdel_0";
                g_sum_ls_no_peni_1 = "ig_sum_ls_no_peni_isdel_1";
                g_sum_ls_no_peni_2 = "ig_sum_ls_no_peni_isdel_0";
                //пени

                isdel_first = "(1)";
                isdel_second = "(0)";
                if (Points.packDistributionParameters.EnableLog)
                {
                    MessageInPackLog(conn_db, packXX,
                        "Установлена стратегия распределения с приоритетом на недействующих поставщиков", false, out r);
                }
            }
            else if (Points.packDistributionParameters.strategy ==
                     PackDistributionParameters.Strategies.ActiveServicesFirst)
            {
                fieldDenominator1 = "isum_charge_isdel_0";
                fieldDenominator2 = "isum_charge_isdel_1";

                fieldNumenator = "sum_charge";

                str_where_for_Koeff1 = " isum_charge_isdel_0<>0 ";
                str_where_for_Koeff2 = " isum_charge_isdel_1<>0 ";

                //пени
                fieldDenominator_peni_1 = "isum_charge_peni_isdel_0";
                fieldDenominator_peni_2 = "isum_charge_peni_isdel_1";
                fieldDenominator_no_peni_1 = "isum_charge_no_peni_isdel_0";
                fieldDenominator_no_peni_2 = "isum_charge_no_peni_isdel_1";
                str_where_for_Koeff_peni_1 = " isum_charge_peni_isdel_0<>0 ";
                str_where_for_Koeff_peni_2 = " isum_charge_peni_isdel_1<>0 ";
                str_where_for_Koeff_no_peni_1 = " isum_charge_no_peni_isdel_0<>0 ";
                str_where_for_Koeff_no_peni_2 = " isum_charge_no_peni_isdel_1<>0 ";
                g_sum_ls_peni_1 = "ig_sum_ls_peni_isdel_0";
                g_sum_ls_peni_2 = "ig_sum_ls_peni_isdel_1";
                g_sum_ls_no_peni_1 = "ig_sum_ls_no_peni_isdel_0";
                g_sum_ls_no_peni_2 = "ig_sum_ls_no_peni_isdel_1";
                //пени

                isdel_first = "(0)";
                isdel_second = "(1)";
                if (Points.packDistributionParameters.EnableLog)
                {
                    MessageInPackLog(conn_db, packXX,
                        "Установлена стратегия распределения с приоритетом на действующих поставщиков", false, out r);
                }
            }

            string Koeff1 = fieldNumenator + "/" + fieldDenominator1;
            string Koeff2 = fieldNumenator + "/" + fieldDenominator2;

            //пени
            string Koeff_peni_1 = fieldNumenator + "/" + fieldDenominator_peni_1;
            string Koeff_peni_2 = fieldNumenator + "/" + fieldDenominator_peni_2;
            string Koeff_no_peni_1 = fieldNumenator + "/" + fieldDenominator_no_peni_1;
            string Koeff_no_peni_2 = fieldNumenator + "/" + fieldDenominator_no_peni_2;
            //пени

            if (isDistributionForInSaldo != true)
            {

                if (Points.packDistributionParameters.EnableLog & packXX.nzp_pack_ls > 0)
                {
                    s1 = "Распределять по ";


                    if (Points.packDistributionParameters.distributionMethod ==
                        PackDistributionParameters.PaymentDistributionMethods.CurrentMonthPositiveSumInsaldo
                        ||
                        Points.packDistributionParameters.distributionMethod ==
                        PackDistributionParameters.PaymentDistributionMethods.LastMonthPositiveSumOutsaldo)
                    {
                        s1 = s1 + "'Положительная часть исходящего сальдо'";
                    }
                    else if (Points.packDistributionParameters.distributionMethod ==
                             PackDistributionParameters.PaymentDistributionMethods.CurrentMonthSumInsaldo)
                    {
                        s1 = s1 + "'Исходящеее сальдо'";
                    }
                    else if (Points.packDistributionParameters.distributionMethod ==
                             PackDistributionParameters.PaymentDistributionMethods.LastMonthPositiveSumCharge)
                    {
                        s1 = s1 + "'Положительная часть начислено к оплате'";
                    }
                    else if (Points.packDistributionParameters.distributionMethod ==
                             PackDistributionParameters.PaymentDistributionMethods.LastMonthSumCharge)
                    {
                        s1 = s1 + "'Начислено к оплате'";
                    }

                    #region

                    /*
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
                     */

                    #endregion

                    MessageInPackLog(conn_db, packXX, s1, false, out r);

                }
            }
            // 

            #region 6. Обработать нулевые оплаты с показаниями
            // 6.1 Пометить такие оплаты
            sqlText = " Update t_opl " +
                      " Set kod_info = 115 " +
                      " where g_sum_ls = 0 " +
                      " and exists (select 1 from " + Points.Pref + "_fin_" + (Points.DateOper.Year % 100).ToString("00") + tableDelimiter + "pu_vals pv" +
                      "   where pv.nzp_pack_ls = t_opl.nzp_pack_ls) " +
                      " and not exists (select 1 from " + Points.Pref + "_fin_" + (Points.DateOper.Year % 100).ToString("00") + tableDelimiter + "gil_sums gs" +
                      "   where gs.nzp_pack_ls = t_opl.nzp_pack_ls)";
            IntfResultType retres = ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log);
            ret = retres.GetReturnsType().GetReturns();
            if (!ret.result)
            {
                DropTempTablesPack(conn_db);
                MessageInPackLog(conn_db, packXX, "Ошибка распределения при обработке нулевых показаний", true, out r);
                Utility.ExceptionUtility.OnException(new Exception(ret.text),
                    System.Reflection.MethodBase.GetCurrentMethod().Name);
                return false;
            }
#if PG
            int count = retres.resultAffectedRows;
#else
            int count = ClassDBUtils.GetAffectedRowsCount(conn_db);
#endif
            if (Points.packDistributionParameters.EnableLog & count > 0 & packXX.nzp_pack_ls > 0)
            {
                MessageInPackLog(conn_db, packXX,
                   "Алгоритм распределения № 9 (нулевая оплата с показанием)", false, out r);
            }
            #endregion

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
            #region выбрать уточнения c nzp_supp
            //заполнить таблицу с уточнениями жильцов по оплатам с договором ЖКУ
            sqlText = " insert into t_gil_sums (nzp_pack_ls, nzp_serv, sum_prih,sum_prih_u, nzp_supp) "+
                      " select nzp_pack_ls, nzp_serv, sum_oplat, sum_oplat,nzp_supp from " +
                       Points.Pref + "_fin_" + (Convert.ToDateTime(Points.DateOper).Year - 2000).ToString("00") + tableDelimiter + "gil_sums a" +
                      " where a.nzp_serv > 0 and a.nzp_supp > 0 and exists (select 1 from t_opl where nzp_pack_ls = a.nzp_pack_ls) ";
            ret = ExecSQL(conn_db, sqlText, true);
            if (!ret.result)
            {
                DropTempTablesPack(conn_db);
                MessageInPackLog(conn_db, packXX, "Ошибка распределения  Указано жильцом 2.1", true, out r);
                Utility.ExceptionUtility.OnException(new Exception(ret.text),
                    System.Reflection.MethodBase.GetCurrentMethod().Name);
                return false;
            }

            ExecSQL(conn_db, sUpdStat + " t_gil_sums ", true);

            //Добавить в таблицу для распределения t_opl недостающие строки из gil_sums
            sqlText =
                " insert into t_opl (nzp_serv, nzp_supp, num_ls,nzp_kvar, nzp_pack_ls, sum_prih_u, kod_sum, dat_month, dat_vvod, g_sum_ls,isdel,znak, g_sum_ls_first) " +
                " select a.nzp_serv, a.nzp_supp, b.num_ls, k.nzp_kvar, a.nzp_pack_ls, SUM(" + sNvlWord +
                "(a.sum_prih,0)), b.kod_sum, b.dat_month, b.dat_vvod, b.g_sum_ls, 0, 1, b.g_sum_ls from t_gil_sums a, " +
                Points.Pref + "_fin_" + (Convert.ToDateTime(Points.DateOper).Year - 2000).ToString("00") + tableDelimiter + "pack_ls b, " +
                Points.Pref + "_data" + tableDelimiter + "kvar k " +
                " where a.nzp_pack_ls = b.nzp_pack_ls and b.num_ls = k.num_ls " +
                " and not exists (select 1 from t_opl where nzp_pack_ls = a.nzp_pack_ls and nzp_serv = a.nzp_serv and nzp_supp = a.nzp_supp)  " +              
                " group by 1,2,3,4,5,7,8,9,10,11,12,13";
            ret = ExecSQL(conn_db, sqlText, true);
            if (!ret.result)
            {
                DropTempTablesPack(conn_db);
                MessageInPackLog(conn_db, packXX, "Ошибка распределения  Указано жильцом 2.2", true, out r);
                Utility.ExceptionUtility.OnException(new Exception(ret.text),
                    System.Reflection.MethodBase.GetCurrentMethod().Name);
                return false;
            }

            //Обновить поле указано жильцом в t_opl
            sqlText = " update t_opl set sum_prih_u = (select SUM(" + DBManager.sNvlWord + "(a.sum_prih,0)) from t_gil_sums a " + 
                      " where a.nzp_pack_ls = t_opl.nzp_pack_ls and a.nzp_serv = t_opl.nzp_serv and a.nzp_supp = t_opl.nzp_supp) where " +
                      " exists (select 1 from t_gil_sums where nzp_pack_ls = t_opl.nzp_pack_ls and nzp_serv = t_opl.nzp_serv and nzp_supp = t_opl.nzp_supp)";
            ret = ExecSQL(conn_db, sqlText, true);
            if (!ret.result)
            {
                DropTempTablesPack(conn_db);
                MessageInPackLog(conn_db, packXX, "Ошибка распределения  Указано жильцом 2.3", true, out r);
                Utility.ExceptionUtility.OnException(new Exception(ret.text),
                    System.Reflection.MethodBase.GetCurrentMethod().Name);
                return false;
            }
            #endregion


            //выбрать уточнения без nzp_supp
           sqlText = "select nzp_pack_ls, nzp_serv , sum_oplat, is_union,nzp_supp, nzp_sums, num_ls from " +
                      Points.Pref + "_fin_" + (Convert.ToDateTime(Points.DateOper).Year - 2000).ToString("00") +
                      tableDelimiter +
                      "gil_sums a where a.nzp_serv>0 and " + sNvlWord + "(a.nzp_supp, 0) <= 0 and exists (select 1 from t_opl where nzp_pack_ls = a.nzp_pack_ls) ";
            string nzp_pack_ls, nzp_serv, sum_oplat, is_union, numls;
            dt = ClassDBUtils.OpenSQL(sqlText, conn_db).GetData();
            foreach (DataRow rowGilSums in dt.Rows)
            {
                //row = dt.Rows[0];
                if (rowGilSums["nzp_pack_ls"] != DBNull.Value)
                {
                    nzp_pack_ls = Convert.ToString(rowGilSums["nzp_pack_ls"]);
                    numls = Convert.ToString(rowGilSums["num_ls"]);
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

                        sqlText =
                            " insert into t_gil_sums (nzp_pack_ls, nzp_serv, sum_prih,sum_prih_u, nzp_supp) select nzp_pack_ls, nzp_serv, sum_oplat, sum_oplat,nzp_supp from " +
                            Points.Pref + "_fin_" + (Convert.ToDateTime(Points.DateOper).Year - 2000).ToString("00") +
                            tableDelimiter + "gil_sums a where a.nzp_sums = " + Convert.ToString(rowGilSums["nzp_sums"]);
                        ExecSQL(conn_db, sqlText, true);

                        sqlText = "select count(*) as cnt from t_opl where nzp_pack_ls= " + nzp_pack_ls +
                                  " and nzp_serv = " + nzp_serv + " and nzp_supp = " + nzp_supp;

                        DataTable dt3;
                        DataRow row3;
                        dt3 = ClassDBUtils.OpenSQL(sqlText, conn_db).GetData();
                        row3 = dt3.Rows[0];
                        if (Convert.ToInt32(row3["cnt"]) == 0)
                        {
                            // sqlText = " update t_opl set nzp_supp = " + nzp_supp + ", sum_prih_u = (select SUM(" + DBManager.sNvlWord + "(a.sum_oplat,0)) from " + Points.Pref + "_fin_" + (Convert.ToDateTime(Points.DateOper).Year - 2000).ToString("00") + tableDelimiter + "gil_sums a where a.nzp_pack_ls = t_opl.nzp_pack_ls and a.nzp_serv = t_opl.nzp_serv ) where nzp_pack_ls = " + nzp_pack_ls + " and nzp_serv =" + nzp_serv;
                            sqlText =
                                " insert into t_opl (nzp_serv, nzp_supp, num_ls,nzp_kvar, nzp_pack_ls, sum_prih_u, kod_sum, dat_month, dat_vvod, g_sum_ls,isdel,znak, g_sum_ls_first) " +
                                " select " + nzp_serv + "," + nzp_supp + "," + numls + ", nzp_kvar," + nzp_pack_ls + "," +
                                "(select SUM(" + DBManager.sNvlWord + "(a.sum_oplat,0)) from " +
                                Points.Pref + "_fin_" + (Convert.ToDateTime(Points.DateOper).Year - 2000).ToString("00") +
                                tableDelimiter + "gil_sums a where a.nzp_pack_ls = " + nzp_pack_ls +
                                " and a.nzp_serv = " + nzp_serv + " and a.nzp_supp = " + nzp_supp +
                                " ), kod_sum, dat_month, dat_vvod, g_sum_ls,1,znak, g_sum_ls_first from t_opl where nzp_pack_ls = " +
                                nzp_pack_ls + " limit 1";

                            ////sqlText = " insert into t_opl (nzp_serv, nzp_supp, num_ls, nzp_pack_ls, sum_prih_u) " + 
                            ////          " values (" + nzp_serv + "," +nzp_supp + "," + numls + "," + nzp_pack_ls +","+ 
                            ////          "(select SUM(" + DBManager.sNvlWord + "(a.sum_oplat,0)) from " +
                            ////          Points.Pref + "_fin_" + (Convert.ToDateTime(Points.DateOper).Year - 2000).ToString("00") + tableDelimiter + "gil_sums a where a.nzp_pack_ls = " + nzp_pack_ls +
                            ////          " and a.nzp_serv = " +nzp_serv + " and a.nzp_supp = " +nzp_supp+ " ) )";
                            ExecSQL(conn_db, sqlText, true);
                        }
                        else
                        {

                            sqlText = " update t_opl set sum_prih_u = (select SUM(" + DBManager.sNvlWord +
                                      "(a.sum_oplat,0)) from " + Points.Pref + "_fin_" +
                                      (Convert.ToDateTime(Points.DateOper).Year - 2000).ToString("00") + tableDelimiter +
                                      "gil_sums a where a.nzp_pack_ls = t_opl.nzp_pack_ls and a.nzp_serv = t_opl.nzp_serv and a.nzp_supp = t_opl.nzp_supp) where nzp_pack_ls = " +
                                      nzp_pack_ls + " and nzp_serv =" + nzp_serv + " and nzp_supp = " + nzp_supp;
                            ExecSQL(conn_db, sqlText, true);
                        }
                    }
                    else
                    {
#if PG
                        sqlText =
                            //    " execute procedure " + Points.Pref + "_kernel.reparation_gil_sum(" + nzp_pack_ls + "," + nzp_serv + " ," + sum_oplat + "," + is_union + "," + straregyMode + ") ";
                            //  Распределение по 14 графе с учётом стратегий сделать позже
                            " select " + Points.Pref + "_kernel.reparation_gil_sum(" + nzp_pack_ls + "," + nzp_serv +
                            " ," + sum_oplat + "," + is_union + ") ";
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

                        /*
                        #if PG
                                ExecSQL(conn_db, " update t_opl set gil_sums = gil_sums+(select coalesce(sum(a.sum_prih_u),0) from t_gil_sums a where a.nzp_pack_ls = t_opl.nzp_pack_ls)", true);
                        #else
                                ExecSQL(conn_db, " update t_opl set gil_sums = gil_sums+(select NVL(sum(a.sum_prih_u),0) from t_gil_sums a where a.nzp_pack_ls = t_opl.nzp_pack_ls)", true);
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
                            "    isum_charge_isdel_0 = isum_charge_isdel_0 - (select coalesce(sum(sum_charge),0) from t_gil_sums a where  a.isdel =0 and a.nzp_pack_ls = t_opl.nzp_pack_ls ),  " +
                            "isum_charge_isdel_1 = isum_charge_isdel_1 - (select coalesce(sum(sum_charge),0) from t_gil_sums a where  a.isdel =1 and a.nzp_pack_ls = t_opl.nzp_pack_ls )   "+
                        "  ";
                        #else
                            //"sum_charge = sum_charge- sum_u,  " +
                            "isum_charge_isdel_0 = isum_charge_isdel_0 - (select NVL(sum(sum_charge),0) from t_gil_sums a where  a.isdel =0 and a.nzp_pack_ls = t_opl.nzp_pack_ls ),  " +
                            "isum_charge_isdel_1 = isum_charge_isdel_1 - (select NVL(sum(sum_charge),0) from t_gil_sums a where  a.isdel =1 and a.nzp_pack_ls = t_opl.nzp_pack_ls )   " +
                            "  ";
                        #endif
                         
                        ret = ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log).GetReturnsType().GetReturns();
                         */
                    }


                   
                }
 
            }
            sqlText =
                "update t_opl set sum_prih_u = (-1)* sum_prih_u, znak_sum_prih_u = -1 where sum_prih_u<0 and " +
                " (select sum(sum_oplat) from " + Points.Pref + "_fin_" +
                (Convert.ToDateTime(Points.DateOper).Year - 2000).ToString("00") + tableDelimiter + "gil_sums " +
                " where t_opl.nzp_pack_ls = nzp_pack_ls) <> 0";
            ExecSQL(conn_db, sqlText, true);

            //рассчитать новые суммы sum_charge и sum_outsaldo, sum_insaldo по услугам 
            //sqlText= " update t_opl set sum_charge =sum_charge - sum_prih_u, g_sum_ls = g_sum_ls - gil_sums   ";
            //ret = ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log).GetReturnsType().GetReturns();

            /*
            sqlText =
            #if PG
                "  Update t_opl  Set  isum_charge = isum_charge - (select coalesce(sum(sum_charge),0) from t_gil_sums a where a.nzp_pack_ls = t_opl.nzp_pack_ls ),   " +
            #else
                "  Update t_opl  Set  isum_charge = isum_charge - (select NVL(sum(sum_charge),0) from t_gil_sums a where a.nzp_pack_ls = t_opl.nzp_pack_ls ),   " +
            #endif
            //"g_sum_ls = g_sum_ls_first - gil_sums,  " +
            #if PG
                "    isum_charge_isdel_0 = isum_charge_isdel_0 - (select coalesce(sum(sum_charge),0) from t_gil_sums a where  a.isdel =0 and a.nzp_pack_ls = t_opl.nzp_pack_ls ),  " +
                "isum_charge_isdel_1 = isum_charge_isdel_1 - (select coalesce(sum(sum_charge),0) from t_gil_sums a where  a.isdel =1 and a.nzp_pack_ls = t_opl.nzp_pack_ls )   "+
            "  ";
            #else
            "isum_charge_isdel_0 = isum_charge_isdel_0 - (select NVL(sum(sum_charge),0) from t_gil_sums a where  a.isdel =0 and a.nzp_pack_ls = t_opl.nzp_pack_ls ),  " +
            "isum_charge_isdel_1 = isum_charge_isdel_1 - (select NVL(sum(sum_charge),0) from t_gil_sums a where  a.isdel =1 and a.nzp_pack_ls = t_opl.nzp_pack_ls )   " +
            "  ";
            #endif
            ret = ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log).GetReturnsType().GetReturns();
            #if PG
                    ExecSQL(conn_db, " update t_opl set gil_sums = (select coalesce(sum(a.sum_prih_u),0) from t_gil_sums a where a.nzp_pack_ls = t_opl.nzp_pack_ls)", true);
            #else
                    ExecSQL(conn_db, " update t_opl set gil_sums = (select NVL(sum(a.sum_prih_u),0) from t_gil_sums a where a.nzp_pack_ls = t_opl.nzp_pack_ls)", true);
            #endif
            */

            // 2.3. Определить остаток от погашения
            sqlText = " update t_ostatok set g_sum_ls_ost = " + Points.Pref + "_kernel" + tableDelimiter +
                      "getSumOstatok(nzp_pack_ls)  ";

            //sqlText = " update t_ostatok t set g_sum_ls_ost = (select AVG(g_sum_ls*znak)-SUM(sum_prih_d+sum_prih_a+sum_prih_u+sum_prih_s) " +
            //          " from t_opl where nzp_pack_ls = t.nzp_pack_ls)";
            ret = ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log).GetReturnsType().GetReturns();

            sqlText =
                " update t_opl set   g_sum_ls_ost = (select a.g_sum_ls_ost from t_ostatok a where a.nzp_pack_ls = t_opl.nzp_pack_ls) where nzp_pack_ls in (select b.nzp_pack_ls from t_gil_sums b)  and sum_prih_u=0 ";
            ret = ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log).GetReturnsType().GetReturns();

            sqlText = " update t_opl set  kod_info = 114 where sum_prih_u <>0  ";
            ret = ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log).GetReturnsType().GetReturns();

            //sqlText =
            //    "update t_opl set" +
            //    " irsum_tarif = (select sum(rsum_tarif) from t_charge t where t_opl.nzp_pack_ls  = t.nzp_pack_ls " +
            //    " and not exists (select 1 from t_opl a where t.nzp_key = a.nzp_key " +
            //    " and a.nzp_pack_ls = t_opl.nzp_pack_ls and a.sum_prih_u <>0) ) " +
            //    " where   exists (select 1 from t_gil_sums b where t_opl.nzp_pack_ls = b.nzp_pack_ls) ";
            sqlText =
                "update t_opl a set" +
                " irsum_tarif = (select sum(case when a.sum_prih_u = 0 then b.rsum_tarif else 0 end) " +
                " from t_opl b where a.nzp_pack_ls  = b.nzp_pack_ls) " +
                " where   exists (select 1 from t_gil_sums b where a.nzp_pack_ls = b.nzp_pack_ls) ";
            ret = ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log).GetReturnsType().GetReturns();
            sqlText =
                " Update t_opl " +
                " Set  " +
                "     sum_prih_a = g_sum_ls_ost * (rsum_tarif/irsum_tarif)" +
                " where  g_sum_ls_ost > 0 and kod_info in (114, 0) and is_peni<>1 and  irsum_tarif<>0 and   nzp_pack_ls in (select b.nzp_pack_ls from t_gil_sums b) and sum_prih_u=0 ";

            ret = ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log).GetReturnsType().GetReturns();
            if (!ret.result)
            {
                DropTempTablesPack(conn_db);
                MessageInPackLog(conn_db, packXX, "Ошибка распределения 5.6", true, out r);
                Utility.ExceptionUtility.OnException(new Exception(ret.text),
                    System.Reflection.MethodBase.GetCurrentMethod().Name);
                return false;
            }


            sqlText =
                " update t_opl set  kod_info = 114 where   nzp_pack_ls in (select b.nzp_pack_ls from t_gil_sums b) ";
            ret = ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log).GetReturnsType().GetReturns();


            /*
            sqlText = " update t_opl set g_sum_ls = (select g_sum_ls_ost from t_ostatok where t_opl.nzp_pack_ls = t_ostatok.nzp_pack_ls and  kod_info in (0,114) ) " +
                                " where  kod_sum in (" + strKodSumForCharge_MM + ",81) and rsum_tarif >0 and nzp_pack_ls in (select nzp_pack_ls from t_gil_sums)";

            ret = ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log).GetReturnsType().GetReturns();
            if (!ret.result)
            {
                DropTempTablesPack(conn_db);
                MessageInPackLog(conn_db, packXX, "Ошибка распределения 5.7", true, out r);
                Utility.ExceptionUtility.OnException(new Exception(ret.text), System.Reflection.MethodBase.GetCurrentMethod().Name);
                return false;
            }

            sqlText = " update t_opl set g_sum_ls_ost = g_sum_ls where nzp_pack_ls in (select nzp_pack_ls from t_gil_sums)";

            ret = ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log).GetReturnsType().GetReturns();
            if (!ret.result)
            {
                DropTempTablesPack(conn_db);
                MessageInPackLog(conn_db, packXX, "Ошибка распределения 5.7", true, out r);
                Utility.ExceptionUtility.OnException(new Exception(ret.text), System.Reflection.MethodBase.GetCurrentMethod().Name);
                return false;
            }            

            //sqlText = " update t_opl set g_sum_ls = g_sum_ls - gil_sums";
            */

            #endregion 2. Затем распределить оплаты по графе "Указано жильцом"

            #region  3. Вначале обработать оплаты у которых начислено к оплате совпадает 100 % с оплатой

            sqlText =
                " Update t_opl " +
                " Set kod_info = 101, " +
                "     sum_prih_d = sum_charge" + // Погашение долга
                " where isum_charge = g_sum_ls-gil_sums and kod_sum in (" + strKodSumForCharge_MM +
                ",81) and kod_info = 0  and  g_sum_ls-gil_sums>0 ";

            retres = ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log);
            ret = retres.GetReturnsType().GetReturns();
            if (!ret.result)
            {
                DropTempTablesPack(conn_db);
                MessageInPackLog(conn_db, packXX, "Ошибка распределения 4.1", true, out r);
                Utility.ExceptionUtility.OnException(new Exception(ret.text),
                    System.Reflection.MethodBase.GetCurrentMethod().Name);
                return false;
            }
#if PG
            count = retres.resultAffectedRows;
#else
                count = ClassDBUtils.GetAffectedRowsCount(conn_db);
#endif
            if (Points.packDistributionParameters.EnableLog & count > 0 & packXX.nzp_pack_ls > 0)
            {
                MessageInPackLog(conn_db, packXX, "Алгоритм распределения № 1 (оплачено = выставлено)", true, out r);
            }

            #endregion Вначале обработать оплаты у которых начислено к оплате совпадает 100 % с оплатой


            #region 4. Обработать оплаты у которых сумма платежа меньше чем выставлено

            #region Пометить оплаты кодом 102, у которых сумма платежа меньше чем выставлено
            sqlText = " Update t_opl " +
                      " Set kod_info = 102 " +
                      " where isum_charge > g_sum_ls-gil_sums and kod_sum in (" +
                      strKodSumForCharge_MM + ",81) and kod_info = 0 and sum_prih_u = 0 ";
            retres = ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log);
            ret = retres.GetReturnsType().GetReturns();

            if (!ret.result)
            {
                DropTempTablesPack(conn_db);
                MessageInPackLog(conn_db, packXX, "Ошибка распределения 6.1", true, out r);
                Utility.ExceptionUtility.OnException(new Exception(ret.text),
                    System.Reflection.MethodBase.GetCurrentMethod().Name);
                return false;
            }

            #region Комментарий в журнал: Алгоритм распределения № 2
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

                    MessageInPackLog(conn_db, packXX,
                        "Алгоритм распределения № 2 (оплачено: " + s1 + " руб меньше чем итого по '" +
                        field_for_etalon_descr + "': " + s2 + " руб)", false, out r);
                }
            }
            #endregion
            #endregion

            // 2.2 Первоначально погасить долг по поставщикам согласно выбранной стратегии (по sum_charge)

            #region погасить пени последними
            if (Points.packDistributionParameters.repayPeni == PackDistributionParameters.OrderRepayPeni.Last)
            {
                #region Разделить сумму оплаты на сумму, идующую на погашение долга по пеням (ig_sum_ls_peni) и сумму, идущую на погашение остальных услуг (ig_sum_ls_no_peni)
                sqlText = " update t_opl set ig_sum_ls_peni =" +
                          " (case when g_sum_ls > isum_charge_no_peni then g_sum_ls - isum_charge_no_peni else 0 end), " +
                          " ig_sum_ls_no_peni = (case when g_sum_ls > isum_charge_no_peni then isum_charge_no_peni else g_sum_ls end) ";
                ret =
                    ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log).GetReturnsType().GetReturns();
                if (!ret.result)
                {
                    DropTempTablesPack(conn_db);
                    MessageInPackLog(conn_db, packXX, "Ошибка распределения 6.3", true, out r);
                    Utility.ExceptionUtility.OnException(new Exception(ret.text),
                        System.Reflection.MethodBase.GetCurrentMethod().Name);
                    return false;
                }
                #endregion

                if (Points.packDistributionParameters.strategy != PackDistributionParameters.Strategies.NoPriority)
                {
                    sqlText = " update t_opl set " +
                              g_sum_ls_no_peni_2 + " = (case when ig_sum_ls_no_peni > " + fieldDenominator_no_peni_1 +
                              " then ig_sum_ls_no_peni - " + fieldDenominator_no_peni_1 + " else 0 end), " +
                              g_sum_ls_no_peni_1 + " = (case when ig_sum_ls_no_peni > " + fieldDenominator_no_peni_1 +
                              " then " + fieldDenominator_no_peni_1 + " else ig_sum_ls_no_peni end) ";
                    ret =
                        ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log).GetReturnsType().GetReturns();
                    if (!ret.result)
                    {
                        DropTempTablesPack(conn_db);
                        MessageInPackLog(conn_db, packXX, "Ошибка распределения 6.3", true, out r);
                        Utility.ExceptionUtility.OnException(new Exception(ret.text),
                            System.Reflection.MethodBase.GetCurrentMethod().Name);
                        return false;
                    }

                    sqlText = " update t_opl set " +
                              g_sum_ls_peni_2 + " = (case when ig_sum_ls_peni > " + fieldDenominator_peni_1 +
                              " then ig_sum_ls_peni - " + fieldDenominator_peni_1 + " else 0 end), " +
                              g_sum_ls_peni_1 + " = (case when ig_sum_ls_peni > " + fieldDenominator_peni_1 + " then " +
                              fieldDenominator_peni_1 + " else ig_sum_ls_peni end) ";
                    ret =
                        ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log).GetReturnsType().GetReturns();
                    if (!ret.result)
                    {
                        DropTempTablesPack(conn_db);
                        MessageInPackLog(conn_db, packXX, "Ошибка распределения 6.3", true, out r);
                        Utility.ExceptionUtility.OnException(new Exception(ret.text),
                            System.Reflection.MethodBase.GetCurrentMethod().Name);
                        return false;
                    }
                }

                //sqlText =
                //    " Update t_opl " +
                //    " Set " +
                //    "     sum_prih_d = " +
                //    Points.Pref + "_kernel" + tableDelimiter +
                //    "getSumPrih(sum_charge,  case when nzp_serv<>500 then (ig_sum_ls_no_peni-gil_sums)*" + Koeff_no_peni_1 +
                //    " else  (ig_sum_ls_peni-gil_sums)*" + Koeff_peni_1 + " end " +
                //    ", isdel )  " +
                //    " where sum_prih_u = 0 and kod_sum in (" + strKodSumForCharge_MM +
                //    ",81) and kod_info = 102  and isdel in " +
                //    isdel_first + " and ((nzp_serv <> 500 and " + str_where_for_Koeff_no_peni_1 + ") or (nzp_serv = 500 and " + str_where_for_Koeff_peni_1 + "))";

                //обработка действующих услуг исключая пени
                sqlText =
                    " Update t_opl " +
                    " Set " +
                    "     sum_prih_d = " +
                    Points.Pref + "_kernel" + tableDelimiter +
                    "getSumPrih(sum_charge, (" + g_sum_ls_no_peni_1 + "-gil_sums)*" + Koeff_no_peni_1 +
                    ", 1 ) , ngroup=1 " +
                    " where sum_prih_u = 0 and kod_sum in (" + strKodSumForCharge_MM + ",81) " +
                    " and kod_info = 102  and isdel in " + isdel_first +
                    " and is_peni <> 1 and " + str_where_for_Koeff_no_peni_1;
                retres = ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log);
                ret = retres.GetReturnsType().GetReturns();
                if (!ret.result)
                {
                    DropTempTablesPack(conn_db);
                    MessageInPackLog(conn_db, packXX, "Ошибка распределения 6.2", true, out r);
                    Utility.ExceptionUtility.OnException(new Exception(ret.text),
                        System.Reflection.MethodBase.GetCurrentMethod().Name);
                    return false;
                }

                if (Points.packDistributionParameters.strategy != PackDistributionParameters.Strategies.NoPriority)
                {
                    //обработка не действующих услуг исключая пени
                    sqlText =
                        " Update t_opl " +
                        " Set " +
                        "     sum_prih_d = " +
                        Points.Pref + "_kernel" + tableDelimiter +
                        "getSumPrih(sum_charge, (" + g_sum_ls_no_peni_2 + "-gil_sums)*" + Koeff_no_peni_2 +
                        ", 1 ) , ngroup=2 " +
                        " where sum_prih_u = 0 and kod_sum in (" + strKodSumForCharge_MM + ",81) " +
                        " and kod_info = 102  and isdel in " + isdel_second +
                        " and is_peni <> 1 and " + str_where_for_Koeff_no_peni_2;
                    retres = ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log);
                    ret = retres.GetReturnsType().GetReturns();
                    if (!ret.result)
                    {
                        DropTempTablesPack(conn_db);
                        MessageInPackLog(conn_db, packXX, "Ошибка распределения 6.2", true, out r);
                        Utility.ExceptionUtility.OnException(new Exception(ret.text),
                            System.Reflection.MethodBase.GetCurrentMethod().Name);
                        return false;
                    }
                }

                //обработка действующих пени
                sqlText =
                    " Update t_opl " +
                    " Set " +
                    "     sum_prih_d = " +
                    Points.Pref + "_kernel" + tableDelimiter +
                    "getSumPrih(sum_charge, (" + g_sum_ls_peni_1 + "-gil_sums)*" + Koeff_peni_1 + ", 1 ) , ngroup=3 " +
                    " where sum_prih_u = 0 and kod_sum in (" + strKodSumForCharge_MM + ",81) " +
                    " and kod_info = 102  and isdel in " + isdel_first +
                    " and is_peni = 1 and " + str_where_for_Koeff_peni_1;
                retres = ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log);
                ret = retres.GetReturnsType().GetReturns();
                if (!ret.result)
                {
                    DropTempTablesPack(conn_db);
                    MessageInPackLog(conn_db, packXX, "Ошибка распределения 6.2", true, out r);
                    Utility.ExceptionUtility.OnException(new Exception(ret.text),
                        System.Reflection.MethodBase.GetCurrentMethod().Name);
                    return false;
                }

                if (Points.packDistributionParameters.strategy != PackDistributionParameters.Strategies.NoPriority)
                {
                    //обработка не действующих пени
                    sqlText =
                        " Update t_opl " +
                        " Set " +
                        "     sum_prih_d = " +
                        Points.Pref + "_kernel" + tableDelimiter +
                        "getSumPrih(sum_charge, (" + g_sum_ls_peni_2 + "-gil_sums)*" + Koeff_peni_2 +
                        ", 1 ), ngroup=4  " +
                        " where sum_prih_u = 0 and kod_sum in (" + strKodSumForCharge_MM + ",81) " +
                        " and kod_info = 102  and isdel in " + isdel_second +
                        " and is_peni = 1 and " + str_where_for_Koeff_peni_2;
                    retres = ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log);
                    ret = retres.GetReturnsType().GetReturns();
                    if (!ret.result)
                    {
                        DropTempTablesPack(conn_db);
                        MessageInPackLog(conn_db, packXX, "Ошибка распределения 6.2", true, out r);
                        Utility.ExceptionUtility.OnException(new Exception(ret.text),
                            System.Reflection.MethodBase.GetCurrentMethod().Name);
                        return false;
                    }
                }

                ret = Points.packDistributionParameters.strategy == PackDistributionParameters.Strategies.NoPriority ? ReversalFor102NoPriority(conn_db, packXX) : ReversalFor102(conn_db, packXX);
                if (!ret.result)
                {
                    DropTempTablesPack(conn_db);
                    MessageInPackLog(conn_db, packXX, "Ошибка распределения 6.2", true, out r);
                    Utility.ExceptionUtility.OnException(new Exception(ret.text),
                        System.Reflection.MethodBase.GetCurrentMethod().Name);
                    return false;
                }

                if (Points.packDistributionParameters.EnableLog & packXX.nzp_pack_ls > 0)
                {
                    sqlText =
                        "select sum(sum_prih_d) as sum_1, sum(sum_charge) as sum_2 from t_opl where kod_info=102 and isdel in " +
                        isdel_first + "  and nzp_pack_ls=" + packXX.nzp_pack_ls;
                    dt = ClassDBUtils.OpenSQL(sqlText, conn_db).GetData();
                    row = dt.Rows[0];
                    s1 = Convert.ToString(row["sum_1"]);
                    s2 = Convert.ToString(row["sum_2"]);
                    if (Points.packDistributionParameters.strategy == PackDistributionParameters.Strategies.NoPriority)
                    {
                        MessageInPackLog(conn_db, packXX,
                            "Погашение долга поставщиков (действующих и недействующих). Из " + s2 + " руб погашено " +
                            s1 + " руб по '" + field_for_etalon_descr + "'", false, out r);
                    }
                    else
                    {
                        MessageInPackLog(conn_db, packXX,
                            "Погашение долга поставщиков первой очереди. Из " + s2 + " руб погашено " + s1 +
                            " руб по '" + field_for_etalon_descr + "'", false, out r);
                    }
                }


                // 2.3. Определить остаток от погашения
                //sqlText = " update t_ostatok set g_sum_ls_ost = " + Points.Pref + "_kernel" + tableDelimiter +
                //          "getSumOstatok(nzp_pack_ls)";
                //ret = ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log).GetReturnsType().GetReturns();
                //if (!ret.result)
                //{
                //    DropTempTablesPack(conn_db);
                //    MessageInPackLog(conn_db, packXX, "Ошибка распределения 6.3", true, out r);
                //    Utility.ExceptionUtility.OnException(new Exception(ret.text),
                //        System.Reflection.MethodBase.GetCurrentMethod().Name);
                //    return false;
                //}

                //sqlText = " update t_ostatok set " +
                //         " g_sum_ls_no_peni_ost = g_sum_ls_ost - (SELECT AVG(" +
                //         fieldDenominator_no_peni_1 + ") " +
                //         " from t_opl where nzp_pack_ls = t_ostatok.nzp_pack_ls and nzp_serv<>500) where g_sum_ls_ost >= (SELECT AVG(" +
                //         fieldDenominator_no_peni_1 + ") " +
                //         " from t_opl where nzp_pack_ls = t_ostatok.nzp_pack_ls and nzp_serv<>500) ";
                //ret = ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log).GetReturnsType().GetReturns();
                //if (!ret.result)
                //{
                //    DropTempTablesPack(conn_db);
                //    MessageInPackLog(conn_db, packXX, "Ошибка распределения 6.3", true, out r);
                //    Utility.ExceptionUtility.OnException(new Exception(ret.text),
                //        System.Reflection.MethodBase.GetCurrentMethod().Name);
                //    return false;
                //}

                //sqlText = " update t_ostatok set " +
                //         " g_sum_ls_peni_ost =  g_sum_ls_ost - g_sum_ls_no_peni_ost - (SELECT AVG(" +
                //         fieldDenominator_peni_1 + ") " +
                //         " from t_opl where nzp_pack_ls = t_ostatok.nzp_pack_ls and nzp_serv=500)  where g_sum_ls_ost >=(SELECT AVG(" +
                //         fieldDenominator_peni_1 + ") " +
                //         " from t_opl where nzp_pack_ls = t_ostatok.nzp_pack_ls and nzp_serv=500) ";
                //ret = ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log).GetReturnsType().GetReturns();
                //if (!ret.result)
                //{
                //    DropTempTablesPack(conn_db);
                //    MessageInPackLog(conn_db, packXX, "Ошибка распределения 6.3", true, out r);
                //    Utility.ExceptionUtility.OnException(new Exception(ret.text),
                //        System.Reflection.MethodBase.GetCurrentMethod().Name);
                //    return false;
                //}

                if (Points.packDistributionParameters.strategy != PackDistributionParameters.Strategies.NoPriority)
                {
//                    sqlText =
//                        " Update t_ostatok " +
//                        " Set  " +
//                        "    g_sum_ls_ost = case when g_sum_ls_ost < 0 then 0 else g_sum_ls_ost end, " +
//                        "    g_sum_ls_no_peni_ost = case when g_sum_ls_no_peni_ost < 0 then 0 else g_sum_ls_no_peni_ost end, " +
//                        "    g_sum_ls_peni_ost = case when g_sum_ls_peni_ost < 0 then 0 else g_sum_ls_peni_ost end " +
//                        " where g_sum_ls_ost < 0 or g_sum_ls_no_peni_ost < 0 or g_sum_ls_peni_ost < 0";
//                    ret =
//                        ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log)
//                            .GetReturnsType()
//                            .GetReturns();
//                    if (!ret.result)
//                    {
//                        DropTempTablesPack(conn_db);
//                        MessageInPackLog(conn_db, packXX, "Ошибка распределения 6.4", true, out r);
//                        Utility.ExceptionUtility.OnException(new Exception(ret.text),
//                            System.Reflection.MethodBase.GetCurrentMethod().Name);
//                        return false;
//                    }

//                    //Обнулить переплаты
//                    sqlText = " Update t_opl " +
//                    " Set " +
//                    "     sum_prih_d = " +
//                              " case when sum_prih_d >= 0 " +
//                                " then case when sum_prih_d > sum_charge then sum_charge else sum_prih_d end" +
//                                " else case when sum_prih_d < sum_charge then sum_charge else sum_prih_d end" +
//                              " end" +

//                    " where sum_prih_u = 0 and kod_sum in (" + strKodSumForCharge_MM +
//                    ",81) and kod_info = 102  and isdel in " +
//                    isdel_first + " and ((nzp_serv <> 500 and " + str_where_for_Koeff_no_peni_1 + ") or (nzp_serv = 500 and " + str_where_for_Koeff_peni_1 + "))";
//                    retres = ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log);
//                    ret = retres.GetReturnsType().GetReturns();
//                    if (!ret.result)
//                    {
//                        DropTempTablesPack(conn_db);
//                        MessageInPackLog(conn_db, packXX, "Ошибка распределения 6.4.1", true, out r);
//                        Utility.ExceptionUtility.OnException(new Exception(ret.text),
//                            System.Reflection.MethodBase.GetCurrentMethod().Name);
//                        return false;
//                    }

//                    // 2.4. Остатком погасить долг по поставщикам второй очереди по sum_charge
//                    sqlText =
//                        " Update t_opl " +
//                        " Set  " +
//                        "     sum_prih_d = " +
//                        " case when nzp_serv <> 500" +
//                        " then (select case when g_sum_ls_no_peni_ost > 0 then g_sum_ls_no_peni_ost else 0 end from t_ostatok where nzp_pack_ls = t_opl.nzp_pack_ls) * (" +
//                        Koeff_no_peni_2 + ")" +
//                        " else (select case when g_sum_ls_peni_ost > 0 then g_sum_ls_peni_ost else 0 end from t_ostatok where nzp_pack_ls = t_opl.nzp_pack_ls) * (" +
//                        Koeff_peni_2 + ")" +
//                        " end" +
//                        " where sum_prih_u = 0 and  kod_info = 102 and isdel in " + isdel_second +
//                        "  and sum_charge > 0 and ((nzp_serv <> 500 and " + str_where_for_Koeff_no_peni_2 +
//                        ") or (nzp_serv = 500 and " + str_where_for_Koeff_peni_2 + "))";
//                    retres = ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log);
//                    ret = retres.GetReturnsType().GetReturns();
//                    if (!ret.result)
//                    {
//                        DropTempTablesPack(conn_db);
//                        MessageInPackLog(conn_db, packXX, "Ошибка распределения 6.5", true, out r);
//                        Utility.ExceptionUtility.OnException(new Exception(ret.text),
//                            System.Reflection.MethodBase.GetCurrentMethod().Name);
//                        return false;
//                    }
//#if PG
//                    count = retres.resultAffectedRows;
//#else
//                    count = ClassDBUtils.GetAffectedRowsCount(conn_db);
//#endif
                    if (Points.packDistributionParameters.EnableLog & count > 0 & packXX.nzp_pack_ls > 0)
                    {
                        sqlText =
                            "select SUM(sum_prih_d) as sum_1, sum(sum_charge) as sum_2  from t_opl where isdel in " +
                            isdel_second + " and kod_info=102  and nzp_pack_ls=" + packXX.nzp_pack_ls;

                        dt = ClassDBUtils.OpenSQL(sqlText, conn_db).GetData();
                        row = dt.Rows[0];
                        s1 = Convert.ToString(row["sum_1"]);
                        s2 = Convert.ToString(row["sum_2"]);

                        if (s2.Trim() != "0")
                        {
                            MessageInPackLog(conn_db, packXX,
                                "Погашение долга поставщиков второй очереди. Из " + s2 + " руб погашено " + s1 +
                                " руб по '" + field_for_etalon_descr + "'", false, out r);
                        }
                    }
                }


            }
                #endregion
                #region пени гасятся наравне с остальными услугами

            else
            {
                sqlText =
                    " Update t_opl " +
                    " Set " +
                    "     sum_prih_d = " + Points.Pref + "_kernel" + tableDelimiter +
                    "getSumPrih(sum_charge,  (g_sum_ls-gil_sums)*" + Koeff1 + ", isdel )  " +
                    " where sum_prih_u = 0 and kod_sum in (" + strKodSumForCharge_MM +
                    ",81) and kod_info = 102  and isdel in " +
                    isdel_first + " and " + str_where_for_Koeff1;
                retres = ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log);
                ret = retres.GetReturnsType().GetReturns();
                if (!ret.result)
                {
                    DropTempTablesPack(conn_db);
                    MessageInPackLog(conn_db, packXX, "Ошибка распределения 6.2", true, out r);
                    Utility.ExceptionUtility.OnException(new Exception(ret.text),
                        System.Reflection.MethodBase.GetCurrentMethod().Name);
                    return false;
                }
#if PG
                count = retres.resultAffectedRows;
#else
                    count = ClassDBUtils.GetAffectedRowsCount(conn_db);
#endif


                if (Points.packDistributionParameters.EnableLog & packXX.nzp_pack_ls > 0)
                {
                    sqlText =
                        "select sum(sum_prih_d) as sum_1, sum(sum_charge) as sum_2 from t_opl where kod_info=102 and isdel in " +
                        isdel_first + "  and nzp_pack_ls=" + packXX.nzp_pack_ls +
                        (Points.packDistributionParameters.repayPeni ==
                         PackDistributionParameters.OrderRepayPeni.Last
                            ? " and is_peni <> 1"
                            : "");
                    dt = ClassDBUtils.OpenSQL(sqlText, conn_db).GetData();
                    row = dt.Rows[0];
                    s1 = Convert.ToString(row["sum_1"]);
                    s2 = Convert.ToString(row["sum_2"]);
                    if (Points.packDistributionParameters.strategy ==
                        PackDistributionParameters.Strategies.NoPriority)
                    {
                        MessageInPackLog(conn_db, packXX,
                            "Погашение долга поставщиков (действующих и недействующих). Из " + s2 + " руб погашено " +
                            s1 + " руб по '" + field_for_etalon_descr + "'", false, out r);
                    }
                    else
                    {
                        MessageInPackLog(conn_db, packXX,
                            "Погашение долга поставщиков первой очереди. Из " + s2 + " руб погашено " + s1 +
                            " руб по '" + field_for_etalon_descr + "'", false, out r);
                    }

                }

                // 2.3. Определить остаток от погашения
#if PG
                sqlText = " update t_ostatok set g_sum_ls_ost = " + Points.Pref +
                          "_kernel.getSumOstatok(nzp_pack_ls)  ";
#else
                sqlText = " update t_ostatok set g_sum_ls_ost = " + Points.Pref + "_kernel:getSumOstatok(nzp_pack_ls)  ";      
#endif

                ret =
                    ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log).GetReturnsType().GetReturns();
                if (!ret.result)
                {
                    DropTempTablesPack(conn_db);
                    MessageInPackLog(conn_db, packXX, "Ошибка распределения 6.3", true, out r);
                    Utility.ExceptionUtility.OnException(new Exception(ret.text),
                        System.Reflection.MethodBase.GetCurrentMethod().Name);
                    return false;
                }

                if (Points.packDistributionParameters.strategy != PackDistributionParameters.Strategies.NoPriority)
                {
                    sqlText =
                        " Update t_ostatok " +
                        " Set  " +
                        "     g_sum_ls_ost =0 " +
                        " where g_sum_ls_ost < 0 ";
                    ret =
                        ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log)
                            .GetReturnsType()
                            .GetReturns();
                    if (!ret.result)
                    {
                        DropTempTablesPack(conn_db);
                        MessageInPackLog(conn_db, packXX, "Ошибка распределения 6.4", true, out r);
                        Utility.ExceptionUtility.OnException(new Exception(ret.text),
                            System.Reflection.MethodBase.GetCurrentMethod().Name);
                        return false;
                    }

                    // 2.4. Остатоком погасить долг по поставщикам второй очереди по sum_charge
                    sqlText =
                        " Update t_opl " +
                        " Set  " +
                        "     sum_prih_d = (select case when  g_sum_ls_ost>0 then  g_sum_ls_ost else 0 end from t_ostatok where nzp_pack_ls = t_opl.nzp_pack_ls) * (" +
                        Koeff2 + ")" +
                        " where sum_prih_u = 0 and  kod_info = 102 and isdel in " + isdel_second +
                        "  and sum_charge>0  and " + str_where_for_Koeff2;

                    retres = ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log);
                    ret = retres.GetReturnsType().GetReturns();
                    if (!ret.result)
                    {
                        DropTempTablesPack(conn_db);
                        MessageInPackLog(conn_db, packXX, "Ошибка распределения 6.5", true, out r);
                        Utility.ExceptionUtility.OnException(new Exception(ret.text),
                            System.Reflection.MethodBase.GetCurrentMethod().Name);
                        return false;
                    }
#if PG
                    count = retres.resultAffectedRows;
#else
                    count = ClassDBUtils.GetAffectedRowsCount(conn_db);
#endif
                    if (Points.packDistributionParameters.EnableLog & count > 0 & packXX.nzp_pack_ls > 0)
                    {
                        sqlText =
                            "select SUM(sum_prih_d) as sum_1, sum(sum_charge) as sum_2  from t_opl where isdel in " +
                            isdel_second + " and kod_info=102  and nzp_pack_ls=" + packXX.nzp_pack_ls +
                            (Points.packDistributionParameters.repayPeni ==
                             PackDistributionParameters.OrderRepayPeni.Last
                                ? " and is_peni <> 1"
                                : "");
                        dt = ClassDBUtils.OpenSQL(sqlText, conn_db).GetData();
                        row = dt.Rows[0];
                        s1 = Convert.ToString(row["sum_1"]);
                        s2 = Convert.ToString(row["sum_2"]);

                        if (s2.Trim() != "0")
                        {
                            MessageInPackLog(conn_db, packXX,
                                "Погашение долга поставщиков второй очереди. Из " + s2 + " руб погашено " + s1 +
                                " руб по '" + field_for_etalon_descr + "'", false, out r);
                        }
                    }
                }
            }

            #endregion

            #endregion 2. Обработать оплаты у которых сумма платежа меньше чем выставлено

            #region 5. Затем обработать все оплаты, которые превосходят начисления

            // 3.1 Пометить такие оплаты
            sqlText = " Update t_opl " +
                      " Set kod_info = 103 " +
                      " where isum_charge < g_sum_ls-gil_sums and kod_sum in (" + strKodSumForCharge_MM +
                      ",81) and kod_info = 0 and sum_prih_u = 0  "; //and isum_charge<>0";
            retres = ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log);
            ret = retres.GetReturnsType().GetReturns();

            if (!ret.result)
            {
                DropTempTablesPack(conn_db);
                MessageInPackLog(conn_db, packXX, "Ошибка распределения 5.1", true, out r);
                Utility.ExceptionUtility.OnException(new Exception(ret.text),
                    System.Reflection.MethodBase.GetCurrentMethod().Name);
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

                MessageInPackLog(conn_db, packXX,
                    "Алгоритм распределения № 3 (оплачено: " + s1 + " руб больше чем итого по '" +
                    field_for_etalon_descr + "': " + s2 + " руб)", false, out r);
            }
            /*
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
            */

            if ( // Способ начисления sum_charge по методу C,D,E,F
                (Points.packDistributionParameters.distributionMethod ==
                 PackDistributionParameters.PaymentDistributionMethods.CurrentMonthSumInsaldo) ||
                (Points.packDistributionParameters.distributionMethod ==
                 PackDistributionParameters.PaymentDistributionMethods.LastMonthSumCharge)
                )
            {
                // Если по всем услугам отрицательные суммы в sum_charge (по лицевому счёту всё погашено) 
                sqlText =
                    " update t_opl set kod_info = 104 where (select sum(b.sum_outsaldo) from t_charge b where t_opl.nzp_kvar = b.nzp_kvar and b.sum_outsaldo<0  ) = isum_outsaldo and kod_info = 103 ";

            }
            else // Способ начисления sum_charge по методу A,B
            {
                // Если по всем услугам отрицательные суммы в sum_charge (по лицевому счёту всё погашено) 
                sqlText =
                    " update t_opl set kod_info = 104 where (select sum(b.sum_charge) from t_charge b where t_opl.nzp_kvar = b.nzp_kvar and b.sum_charge<0) = isum_charge and kod_info = 103 ";

            }

            retres = ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log);
            ret = retres.GetReturnsType().GetReturns();
            if (!ret.result)
            {
                DropTempTablesPack(conn_db);
                MessageInPackLog(conn_db, packXX, "Ошибка распределения 5.2", true, out r);
                Utility.ExceptionUtility.OnException(new Exception(ret.text),
                    System.Reflection.MethodBase.GetCurrentMethod().Name);
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

                MessageInPackLog(conn_db, packXX,
                    "По лицевому счёту только переплаты. Распределение будет произведено как авансовый платёж по 'Начислено за месяц без недопоставки'",
                    false, out r);
            }
            // 3.2 Первоначально погасить долг (по sum_charge) (неважно действующий или нет, т.к. оплаты хватает)
            sqlText =
                " Update t_opl " +
                " Set " +
                "     sum_prih_d =  sum_charge" +
                " where isum_charge < g_sum_ls-gil_sums and kod_sum in (" + strKodSumForCharge_MM +
                ",81) and kod_info = 103 and sum_prih_u = 0  ";
            retres = ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log);
            ret = retres.GetReturnsType().GetReturns();
            if (!ret.result)
            {
                DropTempTablesPack(conn_db);
                MessageInPackLog(conn_db, packXX, "Ошибка распределения 5.2", true, out r);
                Utility.ExceptionUtility.OnException(new Exception(ret.text),
                    System.Reflection.MethodBase.GetCurrentMethod().Name);
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

                MessageInPackLog(conn_db, packXX,
                    "Погашение основного долга " + s1 + " руб по '" + field_for_etalon_descr + "'", false, out r);
            }
            // 3.4. Остаток распределить по исходящему сальдо
            if (
                ( // Способ начисления sum_charge по методу C,D,E,F
                    (Points.packDistributionParameters.distributionMethod ==
                     PackDistributionParameters.PaymentDistributionMethods.CurrentMonthSumInsaldo) ||
                    (Points.packDistributionParameters.distributionMethod ==
                     PackDistributionParameters.PaymentDistributionMethods.LastMonthSumCharge)
                    )
                &
                (isDistributionForInSaldo != true)
                )
            {
                // 3.4.0 Определить остаток от погашения
                sqlText = " Update t_opl " +
                          " SET  g_sum_ls_ost = g_sum_ls-isum_charge" +
                          " where  kod_sum in (" + strKodSumForCharge_MM + ",81) and kod_info = 103 and isdel in " +
                          isdel_second + " and sum_outsaldo>0";


                retres = ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log);
                ret = retres.GetReturnsType().GetReturns();
                if (!ret.result)
                {
                    DropTempTablesPack(conn_db);
                    MessageInPackLog(conn_db, packXX, "Ошибка распределения 5.3", true, out r);
                    Utility.ExceptionUtility.OnException(new Exception(ret.text),
                        System.Reflection.MethodBase.GetCurrentMethod().Name);
                    return false;
                }
#if PG
                count = retres.resultAffectedRows;
#else
                    count = ClassDBUtils.GetAffectedRowsCount(conn_db);
#endif
                if (Points.packDistributionParameters.EnableLog & packXX.nzp_pack_ls > 0 & count > 0)
                {
#if PG
                    sqlText = "select  g_sum_ls_ost as sum_, kod_info from t_opl where  nzp_pack_ls=" +
                              packXX.nzp_pack_ls + " limit 1";
#else
                        sqlText = "select first 1 g_sum_ls_ost as sum_, kod_info from t_opl where  nzp_pack_ls=" + packXX.nzp_pack_ls;
#endif
                    dt = ClassDBUtils.OpenSQL(sqlText, conn_db).GetData();
                    row = dt.Rows[0];
                    s1 = Convert.ToString(row["sum_"]);
                    if ((s1.Trim() != "") && (s1.Trim() != "0.00") && (s1.Trim() != "0") &&
                        (Convert.ToString(row["kod_info"]) == "103"))
                    {
                        MessageInPackLog(conn_db, packXX,
                            "Остаток от погашения долга " + s1 +
                            " распределять по исходящему сальдо откорректированному на погашение долга", false, out r);
                    }
                }

                // 3.4.1  Откорректировать сумму исходящего сальдо с учётом ранее распределённой суммы
                sqlText =
                    " update t_opl set sum_outsaldo = ( sum_outsaldo - (select sum(sum_money) from t_charge a where a.nzp_kvar = t_opl.nzp_kvar and a.nzp_serv = t_opl.nzp_serv and a.nzp_supp = t_opl.nzp_supp)) ," +
                    "  isum_outsaldo = ( isum_outsaldo - (select sum(sum_money) from t_charge a where a.nzp_kvar = t_opl.nzp_kvar and a.nzp_serv = t_opl.nzp_serv and a.nzp_supp = t_opl.nzp_supp)) ," +
                    "  isum_outsaldo_isdel_0 = ( isum_outsaldo_isdel_0 - (select sum(sum_money) from t_charge a where a.nzp_kvar = t_opl.nzp_kvar and a.nzp_serv = t_opl.nzp_serv and a.nzp_supp = t_opl.nzp_supp))  ";
                //ret = ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log).GetReturnsType().GetReturns();
                retres = ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log);
                ret = retres.GetReturnsType().GetReturns();
                if (!ret.result)
                {
                    DropTempTablesPack(conn_db);
                    MessageInPackLog(conn_db, packXX, "Ошибка распределения 5.4", true, out r);
                    Utility.ExceptionUtility.OnException(new Exception(ret.text),
                        System.Reflection.MethodBase.GetCurrentMethod().Name);
                    return false;
                }
                // 3.4.2  подсчитать итоговую сумму исходящего сальдо 
                sqlText =
                    " update t_itog set sum_outsaldo = (select sum(sum_outsaldo) from t_opl a where a.nzp_kvar = t_itog.nzp_kvar and a.sum_outsaldo>0 and isdel=0), " +
                    " isum_outsaldo =  (select sum(isum_outsaldo) from t_opl a where a.nzp_kvar = t_itog.nzp_kvar and a.isum_outsaldo>0 and isdel=0)," +
                    " isum_outsaldo_isdel_0 =  (select sum(isum_outsaldo_isdel_0) from t_opl a where a.nzp_kvar = t_itog.nzp_kvar and a.isum_outsaldo_isdel_0>0 and isdel=0) ";
                ret = ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log).GetReturnsType().GetReturns();
                if (!ret.result)
                {
                    DropTempTablesPack(conn_db);
                    MessageInPackLog(conn_db, packXX, "Ошибка распределения 5.5", true, out r);
                    Utility.ExceptionUtility.OnException(new Exception(ret.text),
                        System.Reflection.MethodBase.GetCurrentMethod().Name);
                    return false;
                }
                sqlText =
                    "update t_opl set isum_outsaldo = (select sum_outsaldo from t_itog a where a.nzp_pack_ls = t_opl.nzp_pack_ls )   ";
                ret = ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log).GetReturnsType().GetReturns();
                if (!ret.result)
                {
                    DropTempTablesPack(conn_db);
                    MessageInPackLog(conn_db, packXX, "Ошибка распределения 5.6", true, out r);
                    Utility.ExceptionUtility.OnException(new Exception(ret.text),
                        System.Reflection.MethodBase.GetCurrentMethod().Name);
                    return false;
                }
                // 3.4.4. Определить остаток от погашения
                // 2.3. Определить остаток от погашения
#if PG
                sqlText = " update t_ostatok set g_sum_ls_ost = " + Points.Pref + "_kernel.getSumOstatok(nzp_pack_ls) ";
#else
                    sqlText = " update t_ostatok set g_sum_ls_ost = " + Points.Pref + "_kernel:getSumOstatok(nzp_pack_ls) ";
#endif

                ret =
                    ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.LogTrace).GetReturnsType().GetReturns();
                if (!ret.result)
                {
                    DropTempTablesPack(conn_db);
                    MessageInPackLog(conn_db, packXX, "Ошибка распределения 5.7", true, out r);
                    Utility.ExceptionUtility.OnException(new Exception(ret.text),
                        System.Reflection.MethodBase.GetCurrentMethod().Name);
                    return false;
                }
                sqlText =
                    " update t_opl set g_sum_ls_ost = (select g_sum_ls_ost from t_ostatok where t_opl.nzp_pack_ls = t_ostatok.nzp_pack_ls and  kod_info = 103 ) " +
                    " where  kod_sum in (" + strKodSumForCharge_MM + ",81) and kod_info = 103 and isdel in " +
                    isdel_second + " and sum_outsaldo>0";

                ret = ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log).GetReturnsType().GetReturns();
                if (!ret.result)
                {
                    DropTempTablesPack(conn_db);
                    MessageInPackLog(conn_db, packXX, "Ошибка распределения 5.7", true, out r);
                    Utility.ExceptionUtility.OnException(new Exception(ret.text),
                        System.Reflection.MethodBase.GetCurrentMethod().Name);
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
                    Utility.ExceptionUtility.OnException(new Exception(ret.text),
                        System.Reflection.MethodBase.GetCurrentMethod().Name);
                    return false;
                }
                sqlText = " Update t_opl " +
                          " SET  g_sum_ls_ost =0, sum_outsaldo =0  " +
                          " where  kod_sum in (" + strKodSumForCharge_MM +
                          ",81) and kod_info = 103 and  sum_outsaldo<=0 ";
                ret = ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log).GetReturnsType().GetReturns();
                if (!ret.result)
                {
                    DropTempTablesPack(conn_db);
                    MessageInPackLog(conn_db, packXX, "Ошибка распределения 5.9", true, out r);
                    Utility.ExceptionUtility.OnException(new Exception(ret.text),
                        System.Reflection.MethodBase.GetCurrentMethod().Name);
                    return false;
                }
                sqlText = " Update t_opl " +
                          " SET  isum_outsaldo =0  " +
                          " where  kod_sum in (" + strKodSumForCharge_MM +
                          ",81) and kod_info = 103 and  sum_outsaldo<=0 ";
                ret = ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log).GetReturnsType().GetReturns();
                if (!ret.result)
                {
                    DropTempTablesPack(conn_db);
                    MessageInPackLog(conn_db, packXX, "Ошибка распределения 5.10", true, out r);
                    Utility.ExceptionUtility.OnException(new Exception(ret.text),
                        System.Reflection.MethodBase.GetCurrentMethod().Name);
                    return false;
                }
                // 3.4.5  Распределить остаток по откорректированному исходящему сальдо (по положительной части) 
#if PG
                sqlText =
                    " Update t_opl " +
                    " Set  " +
                    "   sum_prih_s = " + Points.Pref +
                    "_kernel.getSumPrih(sum_outsaldo,  g_sum_ls_ost * (sum_outsaldo/isum_outsaldo),1 )" +
                    " where  g_sum_ls_ost > 0 and kod_sum in (" + strKodSumForCharge_MM +
                    ",81) and kod_info = 103 and isdel =0 and isum_outsaldo>0";
#else
                sqlText =
                        " Update t_opl " +
                        " Set  " +
                       "   sum_prih_s = " + Points.Pref + "_kernel:getSumPrih(sum_outsaldo,  g_sum_ls_ost * (sum_outsaldo/isum_outsaldo),1 )" +
                        " where  g_sum_ls_ost > 0 and kod_sum in (" + strKodSumForCharge_MM + ",81) and kod_info = 103 and isdel =0 and isum_outsaldo>0";
#endif
                retres = ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log);
                ret = retres.GetReturnsType().GetReturns();
                if (!ret.result)
                {
                    DropTempTablesPack(conn_db);
                    MessageInPackLog(conn_db, packXX, "Ошибка распределения 5.11", true, out r);
                    Utility.ExceptionUtility.OnException(new Exception(ret.text),
                        System.Reflection.MethodBase.GetCurrentMethod().Name);
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
                    sqlText = "select  g_sum_ls_ost as sum_ from t_opl where g_sum_ls_ost>0 and nzp_pack_ls=" +
                              packXX.nzp_pack_ls + " limit 1";
#else
                        sqlText = "select first 1 g_sum_ls_ost as sum_ from t_opl where g_sum_ls_ost>0 and nzp_pack_ls=" + packXX.nzp_pack_ls;
#endif
                    dt = ClassDBUtils.OpenSQL(sqlText, conn_db).GetData();
                    row = dt.Rows[0];
                    s1 = Convert.ToString(row["sum_"]);
                    sqlText = "select sum(sum_prih_s) as sum_ from t_opl where sum_prih_s>0 and nzp_pack_ls=" +
                              packXX.nzp_pack_ls;
                    dt = ClassDBUtils.OpenSQL(sqlText, conn_db).GetData();
                    row = dt.Rows[0];
                    s2 = Convert.ToString(row["sum_"]);
                    MessageInPackLog(conn_db, packXX,
                        "По откорректированному исходящему сальдо распределено " + s2 + " руб", false, out r);
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
                Utility.ExceptionUtility.OnException(new Exception(ret.text),
                    System.Reflection.MethodBase.GetCurrentMethod().Name);
                return false;
            }

            //if (isDistributionForInSaldo != true)
            //{
            sqlText = " Update t_opl " +
                      " SET  g_sum_ls_ost = (select g_sum_ls_ost from t_ostatok where t_ostatok.nzp_pack_ls = t_opl.nzp_pack_ls)" +
                      " where  kod_sum in (" + strKodSumForCharge_MM +
                      ",81) and kod_info in (103,104,114)  and isdel=0  and rsum_tarif>0";
            ret = ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log).GetReturnsType().GetReturns();
            if (!ret.result)
            {
                DropTempTablesPack(conn_db);
                MessageInPackLog(conn_db, packXX, "Ошибка распределения 5.13", true, out r);
                Utility.ExceptionUtility.OnException(new Exception(ret.text),
                    System.Reflection.MethodBase.GetCurrentMethod().Name);
                return false;
            }
            //}
            sqlText =
                " Update t_opl " +
                " Set  " +
                "     sum_prih_a = g_sum_ls_ost * (rsum_tarif/irsum_tarif)" +
                " where  g_sum_ls_ost > 0 and kod_sum in (" + strKodSumForCharge_MM +
                ",81) and kod_info in (103,104,114) and g_sum_ls_ost>0 and isdel=0  and irsum_tarif>0 and is_peni<>1";

            retres = ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log);
            ret = retres.GetReturnsType().GetReturns();
            if (!ret.result)
            {
                DropTempTablesPack(conn_db);
                MessageInPackLog(conn_db, packXX, "Ошибка распределения 5.6", true, out r);
                Utility.ExceptionUtility.OnException(new Exception(ret.text),
                    System.Reflection.MethodBase.GetCurrentMethod().Name);
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
                Utility.ExceptionUtility.OnException(new Exception(ret.text),
                    System.Reflection.MethodBase.GetCurrentMethod().Name);
                return false;
            }

            sqlText = " Update t_opl " +
                      " SET  g_sum_ls_ost = (select g_sum_ls_ost from t_ostatok where t_ostatok.nzp_pack_ls = t_opl.nzp_pack_ls)" +
                      " where  kod_sum in (" + strKodSumForCharge_MM + ",81) and kod_info in (103,104,114) ";
            ret = ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log).GetReturnsType().GetReturns();
            if (!ret.result)
            {
                DropTempTablesPack(conn_db);
                MessageInPackLog(conn_db, packXX, "Ошибка распределения 5.13", true, out r);
                Utility.ExceptionUtility.OnException(new Exception(ret.text),
                    System.Reflection.MethodBase.GetCurrentMethod().Name);
                return false;
            }

#if PG
            count = retres.resultAffectedRows;
#else
                count = ClassDBUtils.GetAffectedRowsCount(conn_db);
#endif
            count = 0;
            if (Points.packDistributionParameters.EnableLog & count > 0 & packXX.nzp_pack_ls > 0)
            {
#if PG
                sqlText = "select g_sum_ls_ost as sum_ from t_opl where g_sum_ls_ost>0 and nzp_pack_ls=" +
                          packXX.nzp_pack_ls + " limit 1";
#else
                    sqlText = "select first 1 g_sum_ls_ost as sum_ from t_opl where g_sum_ls_ost>0 and nzp_pack_ls=" + packXX.nzp_pack_ls;
#endif
                dt = ClassDBUtils.OpenSQL(sqlText, conn_db).GetData();
#if PG
                count = retres.resultAffectedRows;
#else
                    count = ClassDBUtils.GetAffectedRowsCount(conn_db);
#endif

                if (count > 0)
                {
                    row = dt.Rows[0];
                    s1 = Convert.ToString(row["sum_"]);
                    MessageInPackLog(conn_db, packXX,
                        "Распределение аванса " + s1 + " руб по 'Начислено за месяц без недопоставки'", false, out r);
                }
            }

            // Если есть ещё остаток, то распределить по исходящему сальдо
            sqlText = " Update t_opl " +
                      " SET  g_sum_ls_ost = (select g_sum_ls_ost from t_ostatok where t_ostatok.nzp_pack_ls = t_opl.nzp_pack_ls)" +
                      " where  kod_sum in (" + strKodSumForCharge_MM +
                      ",81) and kod_info in (102,103,104,114) and isum_outsaldo>0 ";
            ret = ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log).GetReturnsType().GetReturns();
            if (!ret.result)
            {
                DropTempTablesPack(conn_db);
                MessageInPackLog(conn_db, packXX, "Ошибка распределения 5.13", true, out r);
                Utility.ExceptionUtility.OnException(new Exception(ret.text),
                    System.Reflection.MethodBase.GetCurrentMethod().Name);
                return false;
            }

            sqlText =
                " Update t_opl " +
                " Set  " +
                "     sum_prih_a = sum_prih_a+g_sum_ls_ost * (sum_outsaldo/isum_outsaldo_isdel_0)" +
                " where  g_sum_ls_ost > 0 and kod_sum in (" + strKodSumForCharge_MM +
                ",81) and kod_info in (102,103,104,114) and g_sum_ls_ost>0  and isum_outsaldo_isdel_0>0 and isdel=0 and sum_outsaldo> 0 ";

            retres = ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log);
            ret = retres.GetReturnsType().GetReturns();
            if (!ret.result)
            {
                DropTempTablesPack(conn_db);
                MessageInPackLog(conn_db, packXX, "Ошибка распределения 5.6", true, out r);
                Utility.ExceptionUtility.OnException(new Exception(ret.text),
                    System.Reflection.MethodBase.GetCurrentMethod().Name);
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
                sqlText = "select g_sum_ls_ost as sum_ from t_opl where g_sum_ls_ost>0 and nzp_pack_ls=" +
                          packXX.nzp_pack_ls + " limit 1";
#else
                        sqlText = "select first 1 g_sum_ls_ost as sum_ from t_opl where g_sum_ls_ost>0 and nzp_pack_ls=" + packXX.nzp_pack_ls;
#endif
                dt = ClassDBUtils.OpenSQL(sqlText, conn_db).GetData();
                row = dt.Rows[0];
                s1 = Convert.ToString(row["sum_"]);
                MessageInPackLog(conn_db, packXX, "Распределение аванса " + s1 + " руб по 'Исходящему сальдо (+)'",
                    false, out r);
            }


            // Если есть ещё остаток, то распределить по входящему сальдо
            sqlText = " Update t_opl " +
                      " SET  g_sum_ls_ost = (select g_sum_ls_ost from t_ostatok where t_ostatok.nzp_pack_ls = t_opl.nzp_pack_ls)" +
                      " where  kod_sum in (" + strKodSumForCharge_MM +
                      ",81) and kod_info in (103,104,114)  and isdel=0  and sum_insaldo>0";
            ret = ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log).GetReturnsType().GetReturns();
            if (!ret.result)
            {
                DropTempTablesPack(conn_db);
                MessageInPackLog(conn_db, packXX, "Ошибка распределения 5.13", true, out r);
                Utility.ExceptionUtility.OnException(new Exception(ret.text),
                    System.Reflection.MethodBase.GetCurrentMethod().Name);
                return false;
            }

            sqlText =
                " Update t_opl " +
                " Set  " +
                "     sum_prih_a = sum_prih_a+g_sum_ls_ost * (sum_insaldo/isum_insaldo)" +
                " where  g_sum_ls_ost > 0 and kod_sum in (" + strKodSumForCharge_MM +
                ",81) and kod_info in (103,104,114) and g_sum_ls_ost>0 and isdel=0  and isum_insaldo>0";

            retres = ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log);
            ret = retres.GetReturnsType().GetReturns();
            if (!ret.result)
            {
                DropTempTablesPack(conn_db);
                MessageInPackLog(conn_db, packXX, "Ошибка распределения 5.6", true, out r);
                Utility.ExceptionUtility.OnException(new Exception(ret.text),
                    System.Reflection.MethodBase.GetCurrentMethod().Name);
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
                sqlText = "select g_sum_ls_ost as sum_ from t_opl where g_sum_ls_ost>0 and nzp_pack_ls=" +
                          packXX.nzp_pack_ls + " limit 1";
#else
                    sqlText = "select first 1 g_sum_ls_ost as sum_ from t_opl where g_sum_ls_ost>0 and nzp_pack_ls=" + packXX.nzp_pack_ls;
#endif
                dt = ClassDBUtils.OpenSQL(sqlText, conn_db).GetData();
                row = dt.Rows[0];
                s1 = Convert.ToString(row["sum_"]);
                MessageInPackLog(conn_db, packXX, "Распределение аванса " + s1 + " руб по 'Входящему сальдо (+)'", false,
                    out r);
            }


            #endregion 3. Затем обработать все оплаты, которые превосходят начисления
                       
            // Подсчитать итоговую сумму распределения
            sqlText =
                " Update t_opl " +
                " Set  " +
                "     sum_prih = sum_prih_a+sum_prih_u*znak_sum_prih_u+sum_prih_d+sum_prih_s " +
                " where   kod_info >0 ";
            retres = ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log);
            ret = retres.GetReturnsType().GetReturns();

            if (!ret.result)
            {
                DropTempTablesPack(conn_db);
                MessageInPackLog(conn_db, packXX, "Ошибка распределения 6.6", true, out r);
                Utility.ExceptionUtility.OnException(new Exception(ret.text),
                    System.Reflection.MethodBase.GetCurrentMethod().Name);
                return false;
            }

#if PG
            count = retres.resultAffectedRows;
#else
                count = ClassDBUtils.GetysAffectedRowsCount(conn_db);
#endif
            if (Points.packDistributionParameters.EnableLog & count > 0 & packXX.nzp_pack_ls > 0)
            {
                sqlText = "select sum(sum_prih) as sum_ from t_opl where kod_info>0 and nzp_pack_ls=" +
                          packXX.nzp_pack_ls;
                dt = ClassDBUtils.OpenSQL(sqlText, conn_db).GetData();
                row = dt.Rows[0];
                s1 = Convert.ToString(row["sum_"]);
                MessageInPackLog(conn_db, packXX, "Итого распределено " + s1 + " руб", false, out r);
            }

            sqlText = "update t_opl set sum_prih_u = znak_sum_prih_u* sum_prih_u, znak_sum_prih_u = 1 ";
            ExecSQL(conn_db, sqlText, true);

            ExecSQL(conn_db, " Create index ix2_t_opl on t_opl (kod_info) ", true);
#if PG
            ExecSQL(conn_db, " analyze t_opl ", true);
#else
                ExecSQL(conn_db, " Update statistics for table t_opl ", true);
#endif
            return true;
        }

        private Returns ReversalFor102NoPriority(IDbConnection conn_db, CalcTypes.PackXX packXX)
        {
            int count = 0;

            Returns ret = ReversalFor102_0(conn_db, "ig_sum_ls_no_peni", " and is_peni <> 1",
                packXX);
            if (!ret.result) return ret;
            count += ret.tag;

            ret = ReversalFor102_0(conn_db, "ig_sum_ls_peni", " and is_peni = 1", packXX);
            if (!ret.result) return ret;
            count += ret.tag;

            int it = 0;
            while (count > 0 && it < 10)
            {
                ret = ReversalFor102_1(conn_db, packXX);
                if (!ret.result) return ret;
                ExecSQL(conn_db, "truncate t_iopl_102", true);
                count = 0;
                ret = ReversalFor102_0(conn_db, "ig_sum_ls_no_peni", " and is_peni <> 1",
                    packXX);
                if (!ret.result) return ret;
                count += ret.tag;

                ret = ReversalFor102_0(conn_db, "ig_sum_ls_peni", " and is_peni = 1", packXX);
                if (!ret.result) return ret;
                count += ret.tag;
                it++;
            }
            return ret;
        }

        private Returns ReversalFor102(IDbConnection conn_db, CalcTypes.PackXX packXX)
        {
            int count = 0;

            Returns ret = ReversalFor102_0(conn_db, "ig_sum_ls_no_peni_isdel_1", " and is_peni <> 1 and isdel = 1",
                packXX);
            if (!ret.result) return ret;
            count += ret.tag;
            ret = ReversalFor102_0(conn_db, "ig_sum_ls_no_peni_isdel_0", " and is_peni <> 1 and isdel = 0", packXX);
            if (!ret.result) return ret;
            count += ret.tag;
            ret = ReversalFor102_0(conn_db, "ig_sum_ls_peni_isdel_1", " and is_peni = 1 and isdel = 1", packXX);
            if (!ret.result) return ret;
            count += ret.tag;
            ret = ReversalFor102_0(conn_db, "ig_sum_ls_peni_isdel_0", " and is_peni = 1 and isdel = 0", packXX);
            if (!ret.result) return ret;
            count += ret.tag;
            int it = 0;
            while (count > 0 && it < 10)
            {
                ret = ReversalFor102_1(conn_db, packXX);
                if (!ret.result) return ret;
                ExecSQL(conn_db, "truncate t_iopl_102", true);
                count = 0;
                ret = ReversalFor102_0(conn_db, "ig_sum_ls_no_peni_isdel_1", " and is_peni <> 1 and isdel = 1",
                    packXX);
                if (!ret.result) return ret;
                count += ret.tag;
                ret = ReversalFor102_0(conn_db, "ig_sum_ls_no_peni_isdel_0", " and is_peni <> 1 and isdel = 0",
                    packXX);
                if (!ret.result) return ret;
                count += ret.tag;
                ret = ReversalFor102_0(conn_db, "ig_sum_ls_peni_isdel_1", " and is_peni = 1 and isdel = 1", packXX);
                if (!ret.result) return ret;
                count += ret.tag;
                ret = ReversalFor102_0(conn_db, "ig_sum_ls_peni_isdel_0", " and is_peni = 1 and isdel = 0", packXX);
                if (!ret.result) return ret;
                count += ret.tag;
                it++;
            }
            return ret;
        }

        private Returns ReversalFor102_0(IDbConnection conn_db, string g_sum_ls, string where, CalcTypes.PackXX packXX)
        {
            Returns ret;

            var sqlText = new StringBuilder();
            sqlText.Append("insert into t_iopl_102 (nzp_kvar, nzp_pack_ls, ngroup, sum_prih,  g_sum_ls, sum_ost_dolg) ");
            sqlText.Append("Select nzp_kvar, nzp_pack_ls, ngroup, sum(sum_prih_d) as sum_prih, ");
            sqlText.AppendFormat("max({0}) as g_sum_ls, max(sum_charge - sum_prih_d) ", g_sum_ls);
            sqlText.Append(" From t_opl ");
            sqlText.Append(" Where kod_info =102 and sum_prih_d <> 0 " + where);
            sqlText.Append(" Group by 1,2,3  having  abs(sum(sum_prih_d) - max(" + g_sum_ls + "))>0.0001");
            IntfResultType retres = ClassDBUtils.ExecSQL(sqlText.ToString(), conn_db, ClassDBUtils.ExecMode.Log);
            ret = retres.GetReturnsType().GetReturns();
            if (!ret.result)
            {
                DropTempTablesPack(conn_db);
                MessageInPackLog(conn_db, packXX, "Ошибка распределения 2.102", true, out ret);
                return ret;
            }

#if PG
            ret.tag = retres.resultAffectedRows;
#else
            ret.tag = ClassDBUtils.GetysAffectedRowsCount(conn_db);
#endif
            return ret;
        }

        private Returns ReversalFor102_1(IDbConnection conn_db, CalcTypes.PackXX packXX)
        {
            var sqlText = new StringBuilder();
            sqlText.Append(
                "Update t_iopl_102 a set nzp_key = (select max(nzp_key) from t_opl where kod_info = 102 and ngroup = a.ngroup" +
                " and nzp_pack_ls = a.nzp_pack_ls and abs(sum_charge - sum_prih_d - sum_ost_dolg) < 0.0001)");
            Returns ret = ExecSQL(conn_db, sqlText.ToString(), true);
            if (!ret.result)
            {
                DropTempTablesPack(conn_db);
                MessageInPackLog(conn_db, packXX, "Ошибка распределения 2.102", true, out ret);
                return ret;
            }

#if PG
            ExecSQL(conn_db, " analyze t_iopl_102 ", true);
#else
            ExecSQL(conn_db, " Update statistics for table t_iopl_102 ", true);
#endif
            sqlText = new StringBuilder();
            sqlText.Append(
                " Update t_opl a set sum_prih_d = sum_prih_d + " +
                " (select case when g_sum_ls - sum_prih > sum_ost_dolg then sum_ost_dolg else g_sum_ls - sum_prih end" +
                " from t_iopl_102 where nzp_key = a.nzp_key) where exists (select 1 from t_iopl_102 where nzp_key = a.nzp_key)");
            ret = ExecSQL(conn_db, sqlText.ToString(), true);
            if (!ret.result)
            {
                DropTempTablesPack(conn_db);
                MessageInPackLog(conn_db, packXX, "Ошибка распределения 2.102", true, out ret);
                return ret;
            }
            return ret;
        }

        //-----------------------------------------------------------------------------
        private bool StandardActiveAndInactiveSuppliers(IDbConnection conn_db, CalcTypes.PackXX packXX, out Returns ret)
            //-----------------------------------------------------------------------------
        {
            // Cтандартная схема, погашение действующих и недействующих поставщиков равноправно
            ret = Utils.InitReturns();
            return false;
        }

        //-----------------------------------------------------------------------------
        private bool StandardSpecial(IDbConnection conn_db, CalcTypes.PackXX packXX, out Returns ret)
            //-----------------------------------------------------------------------------
        {
            // Cтандартная схема, погашение специальных квитанций (57,55,83,93,94)
            string sqlText = "";
            string str_where = "";
            DataTable dt;
            DataTable dt2;
            DataRow row2;
            int count;
            ret = Utils.InitReturns();
            bool result = false;

            #region Распределить специальные квитанции

            sqlText =
                " select * from t_selkvar where kod_sum in (" + strKodSumForCharge_X + ")";

            dt = ClassDBUtils.OpenSQL(sqlText, conn_db).GetData();
            foreach (DataRow row in dt.Rows)
            {
                packXX.nzp_pack_ls = Convert.ToInt32(row["nzp_pack_ls"]);

                Ls kvar = new Ls();
                kvar.num_ls = Convert.ToInt32(row["num_ls"]);
                WorkTempKvar dbadres = new WorkTempKvar();
                ReturnsObjectType<Ls> pref = dbadres.GetLsLocation(kvar, conn_db, null);
                dbadres.Close();
                pref.ThrowExceptionIfError();

                var dat_month_local = Points.GetCalcMonth(new CalcMonthParams(pref.returnsData.pref));

                string baseName = pref.returnsData.pref + "_charge_" +
                                  (Convert.ToDateTime(row["dat_month"]).Year - 2000).ToString("00");
                dbadres.Close();

                string tableName = "";
                string fieldName = "";
                string schetName = "";

                if (row["kod_sum"].ToString() == "55") // Интернетовские счета
                {
#if PG
                    tableName = ".charge_g";
#else
                    tableName=":charge_g";
#endif
                    fieldName = "sum_fakt";
                    schetName = "счет получен через Интернет";

                    str_where =
                        " where kod_sum =" + row["kod_sum"].ToString() + " and " +
                        "  num_ls = " + row["num_ls"].ToString() + " AND nzp_serv > 1 " +
                        " AND dat_charge = dat_calc and dat_calc = '" +
                        Convert.ToDateTime(row["dat_month"]).ToShortDateString() +
                        "'  and nzp_supp <> -999 and nzp_supp > 0 and " +
                        " id_bill = " + row["id_bill"].ToString();
                }

                if (row["kod_sum"].ToString() == "57") // Счета по требованию
                {
#if PG
                    tableName = ".charge_k57";
#else
                        tableName=":charge_k57";
#endif
                    fieldName = "sum_charge";
                    schetName = "счет получен по требованию жильца";

                    str_where =
                        " where kod_sum =" + row["kod_sum"].ToString() + " and " +
                        "  num_ls = " + row["num_ls"].ToString() + " AND nzp_serv > 1 " +
                        " AND dat_charge = dat_calc and dat_calc = '" +
                        Convert.ToDateTime(row["dat_month"]).ToShortDateString() +
                        "'  and nzp_supp <> -999 and nzp_supp > 0 and " +
                        " id_bill = " + row["id_bill"].ToString();

                }

                if (row["kod_sum"].ToString() == "83") // Счета на оплату вперёд
                {
#if PG
                    tableName = ".charge_t";
#else
                        tableName=":charge_t";
#endif
                    fieldName = "sum_charge";
                    schetName = "счет на оплату вперёд";
                    str_where =
                        " where " +
                        "  num_ls = " + row["num_ls"].ToString() + " AND nzp_serv > 1 " +
                        " AND dat_charge = '" + Convert.ToDateTime(row["dat_month"]).ToShortDateString() +
                        "'  and nzp_supp <> -999 and nzp_supp > 0 and " +
                        " id_bill = " + row["id_bill"].ToString();

                }

                if (row["kod_sum"].ToString() == "93" || row["kod_sum"].ToString() == "94") // Долговые счета
                {
#if PG
                    tableName = ".charge_d";
#else
                        tableName=":charge_d";
#endif
                    fieldName = "sum_charge";
                    schetName = "счет погашение долга";

                    str_where =
                        " where " +
                        "  num_ls = " + row["num_ls"].ToString() + " AND nzp_serv > 1 " +
                        " AND dat_charge = '" + Convert.ToDateTime(row["dat_month"]).ToShortDateString() +
                        "'  and nzp_supp <> -999 and nzp_supp > 0 and " +
                        " id_bill = " + row["id_bill"].ToString();
                }

                if (Points.packDistributionParameters.EnableLog)
                {
                    MessageInPackLog(conn_db, packXX,
                        "Алгоритм распределения № " + row["kod_sum"].ToString() + ". Распределение оплаты по счету " +
                        row["kod_sum"].ToString() + " (" + schetName.Trim() + ") № " + row["id_bill"] + " )", false,
                        out ret);
                }


                sqlText =
                    " select sum(" + fieldName + ") sum_1 from " + baseName + tableName + str_where;
                dt2 = ClassDBUtils.OpenSQL(sqlText, conn_db).GetData();
                row2 = dt2.Rows[0];
                decimal sum_charge = 0;

                if (row2["sum_1"] != DBNull.Value)
                {
                    sum_charge = Convert.ToDecimal(row2["sum_1"]);
                }
                ;
                if (Convert.ToDecimal(row["g_sum_ls"]) == sum_charge)
                {
                    // Сумма по квитанции совпадает с суммой оплаты
#if PG
                    sqlText =
                        " Insert into " + pref.returnsData.pref + "_charge_" +
                        (Convert.ToDateTime(Points.DateOper).Year%100).ToString("00") +
                        "." + "fn_supplier" + (Convert.ToDateTime(Points.DateOper).Month%100).ToString("00") +
                        " ( num_ls, nzp_pack_ls, nzp_serv, nzp_supp, sum_prih, kod_sum, dat_month, dat_prih, dat_uchet, s_dolg, s_user, s_forw, calc_month) " +
                        " Select num_ls, " + row["nzp_pack_ls"] + ", nzp_serv, nzp_supp, " + fieldName + ",  " +
                        row["kod_sum"].ToString() + ", '" + Convert.ToDateTime(row["dat_month"]).ToShortDateString() +
                        "', '" + Convert.ToDateTime(Points.DateOper).ToShortDateString() + "', '" +
                        Convert.ToDateTime(Points.DateOper).ToShortDateString() + "', 0, " + fieldName + ", 0, cast('" + (new DateTime(dat_month_local.year_, dat_month_local.month_, 1)).ToShortDateString() + "' as DATE) " +
                        " From  " + pref.returnsData.pref + "_charge_" +
                        (Convert.ToDateTime(Points.DateOper).Year%100).ToString("00") + tableName +
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
                        MessageInPackLog(conn_db, packXX, "Ошибка распределения 7." + row["kod_sum"].ToString() + ".1",
                            true, out ret);
                        Utility.ExceptionUtility.OnException(new Exception(ret.text),
                            System.Reflection.MethodBase.GetCurrentMethod().Name);
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
                            MessageInPackLog(conn_db, packXX, "Распределено " + row["g_sum_ls"] + " руб ", false,
                                out ret);
                        }

                    }
#if PG
                    sqlText =
                        " update " + Points.Pref + "_fin_" +
                        (Convert.ToDateTime(Points.DateOper).Year - 2000).ToString("00") +
                        ".pack_ls set inbasket = 0, alg = " + row["kod_sum"].ToString() + ", dat_uchet = '" +
                        Convert.ToDateTime(Points.DateOper).ToShortDateString() + "'," +
                        " cal_month = cast('" + (new DateTime(dat_month_local.year_, dat_month_local.month_, 1)).ToShortDateString() + "' as DATE) " +
                        "where nzp_pack_ls = " +
                        row["nzp_pack_ls"];
#else
                        sqlText =
                        " update " + Points.Pref + "_fin_" + (Convert.ToDateTime(Points.DateOper).Year - 2000).ToString("00") + ":pack_ls set inbasket = 0, alg = "+row["kod_sum"].ToString()+", dat_uchet = '"+Convert.ToDateTime(Points.DateOper).ToShortDateString()+"' where nzp_pack_ls = " +
                        row["nzp_pack_ls"];
#endif

                    ret =
                        ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log).GetReturnsType().GetReturns();
                    if (!ret.result)
                    {
                        DropTempTablesPack(conn_db);
                        MessageInPackLog(conn_db, packXX, "Ошибка распределения 7." + row["kod_sum"].ToString() + ".2",
                            true, out ret);
                        Utility.ExceptionUtility.OnException(new Exception(ret.text),
                            System.Reflection.MethodBase.GetCurrentMethod().Name);
                        return false;
                    }

#if PG
                    sqlText = " delete from  " + Points.Pref + "_fin_" +
                              (Convert.ToDateTime(Points.DateOper).Year - 2000).ToString("00") +
                              ".pack_ls_err  where nzp_pack_ls = " +
                              row["nzp_pack_ls"] + " and nzp_err = 600 ";
#else
                            sqlText = " delete from  " + Points.Pref + "_fin_" + (Convert.ToDateTime(Points.DateOper).Year - 2000).ToString("00") + ":pack_ls_err  where nzp_pack_ls = " +
                                    row["nzp_pack_ls"] + " and nzp_err = 600 ";
#endif
                    ret =
                        ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log).GetReturnsType().GetReturns();

                    return true;
                }
                else
                {
#if PG
                    sqlText =
                        " update " + Points.Pref + "_fin_" +
                        (Convert.ToDateTime(Points.DateOper).Year - 2000).ToString("00") +
                        ".pack_ls set inbasket = 1, alg = 0, dat_uchet = null, calc_month = null where nzp_pack_ls = " +
                        row["nzp_pack_ls"];
#else
                            sqlText =
                            " update " + Points.Pref + "_fin_" + (Convert.ToDateTime(Points.DateOper).Year - 2000).ToString("00") + ":pack_ls set inbasket = 1, alg = 0, dat_uchet = null where nzp_pack_ls = " +
                            row["nzp_pack_ls"];
#endif
                    ret =
                        ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log).GetReturnsType().GetReturns();
                    if (!ret.result)
                    {
                        DropTempTablesPack(conn_db);
                        MessageInPackLog(conn_db, packXX, "Ошибка распределения 7." + row["kod_sum"].ToString() + ".3",
                            true, out ret);
                        Utility.ExceptionUtility.OnException(new Exception(ret.text),
                            System.Reflection.MethodBase.GetCurrentMethod().Name);
                        return false;
                    }

#if PG
                    sqlText = " delete from  " + Points.Pref + "_fin_" +
                              (Convert.ToDateTime(Points.DateOper).Year - 2000).ToString("00") +
                              ".pack_ls_err  where nzp_pack_ls = " +
                              row["nzp_pack_ls"] + " and nzp_err in (1, 600) ";
#else
                            sqlText = " delete from  " + Points.Pref + "_fin_" + (Convert.ToDateTime(Points.DateOper).Year - 2000).ToString("00") + ":pack_ls_err  where nzp_pack_ls = " +
                                    row["nzp_pack_ls"]+" and nzp_err in (1, 600) ";
#endif
                    ret =
                        ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log).GetReturnsType().GetReturns();
#if PG
                    sqlText =
                        " Insert into " + Points.Pref + "_fin_" +
                        (Convert.ToDateTime(Points.DateOper).Year - 2000).ToString("00") + ".pack_ls_err " +
                        " (  nzp_pack_ls, nzp_err, note ) " +
                        " values(" + row["nzp_pack_ls"] + ",600,'Для счёта с кодом " + row["kod_sum"].ToString() + "(" +
                        schetName.Trim() + ") выставлено sum_charge, сумма платежа " + row["g_sum_ls"] + "')";
#else
                            sqlText =
                            " Insert into " + Points.Pref + "_fin_" + (Convert.ToDateTime(Points.DateOper).Year - 2000).ToString("00") + ":pack_ls_err " +
                            " (  nzp_pack_ls, nzp_err, note ) " +
                            " values(" + row["nzp_pack_ls"] + ",600,'Для счёта с кодом " + row["kod_sum"].ToString() + "(" + schetName.Trim() + ") выставлено sum_charge, сумма платежа " + row["g_sum_ls"] + "')";
#endif
                    ret =
                        ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log).GetReturnsType().GetReturns();
                    if (Points.packDistributionParameters.EnableLog)
                    {
                        MessageInPackLog(conn_db, packXX,
                            "Для счёта с кодом " + row["kod_sum"].ToString() + "(" + schetName.Trim() +
                            ") выставлено sum_charge, сумма платежа " + row["g_sum_ls"], false, out ret);
                        MessageInPackLog(conn_db, packXX, "Оплата помещена в корзину", false, out ret);
                    }
                }
            }
            return result;

            #endregion Распределить специальные квитанции
        }

        //откат пачки и удаление
        public bool CalcPackDel(CalcTypes.ParamCalc paramcalc, out Returns ret,
            DbCalcQueueClient.SetTaskProgressDelegate setTaskProgress)
        {
            string conn_kernel = Points.GetConnByPref(paramcalc.pref);
            IDbConnection conn_db = GetConnection(conn_kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result)
            {
                return false;
            }

            bool result = CalcPackDel(conn_db, paramcalc, out ret, setTaskProgress);

            conn_db.Close();

            return result;
        }

        public bool CalcPackDel(IDbConnection conn_db, CalcTypes.ParamCalc paramcalc, out Returns ret,
            DbCalcQueueClient.SetTaskProgressDelegate setTaskProgress)
        {
            ret = Utils.InitReturns();
            Returns r = Utils.InitReturns();

            bool b = CalcPackOt(conn_db, paramcalc, 0, out ret, setTaskProgress);
            if (!b) return false;

            CalcTypes.PackXX packXX = new CalcTypes.PackXX(paramcalc, 0, true);

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
                        par_pack = (int) reader["par_pack"];
                }
            }
            catch
            {
            }

            //нельзя сразу удалять pack_ls, надо проверить, что пачка откачена во всех локальных банках!!!
            //foreach (_Point zap in Points.PointList)
            //{
            //}

#if PG
#else
            ret = ExecSQL(conn_db,
                " Set isolation dirty read "
                , true);
            if (!ret.result)
            {
                MessageInPackLog(conn_db, packXX, "Ошибка удаления 1.2", true, out r);
                return false;
            }
#endif



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
                " Delete From " + Points.Pref + "_fin_" + (paramcalc.calc_yy - 2000).ToString("00") + paramcalc.ol_srv +
                ".gil_sums where nzp_pack_ls in (select nzp_pack_ls from " + packXX.pack_ls +
                " Where nzp_pack = " + packXX.nzp_pack + ")"
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
        /// Учесть оплаты в фоне
        /// </summary>
        /// <param name="dat_s">дата начала</param>
        /// <param name="dat_po">дата окончания</param>
        /// <param name="nzp_user">пользователь</param>
        /// <param name="ret">результат</param>
        public void CalcDistribFon(TransferBalanceFinder finder, out Returns ret)
        {
            CalcFonTask calcfon = new CalcFonTask(Points.GetCalcNum(0));
            calcfon.TaskType = CalcFonTask.Types.taskToTransfer;
            calcfon.Status = FonTask.Statuses.New; //на выполнение                         
            calcfon.nzp_user = finder.nzp_user;
            DateTime d1, d2;
            DateTime.TryParse(finder.dat_s, out d1);
            DateTime.TryParse(finder.dat_po, out d2);
            calcfon.nzp = Convert.ToInt32(d1.ToString("yyyyMMdd"));
            calcfon.nzpt = Convert.ToInt32(d2.ToString("yyyyMMdd"));
            calcfon.txt = "Учет к перечислению за период с " + d1.ToShortDateString();
            calcfon.parameters = JsonConvert.SerializeObject(finder);

            var db = new DbCalcQueueClient();
            ret = db.AddTask(calcfon);
            db.Close();
        }


        /// <summary>
        /// Отмена распределения пачки оплат
        /// </summary>
        /// <param name="paramcalc"></param>
        /// <param name="ret"></param>
        /// <returns></returns>
        public bool CalcPackOt(CalcTypes.ParamCalc paramcalc, out Returns ret,
            DbCalcQueueClient.SetTaskProgressDelegate setTaskProgress)
        {
            string conn_kernel = Points.GetConnByPref(paramcalc.pref);
            IDbConnection conn_db = GetConnection(conn_kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result)
            {
                return false;
            }

            bool result = CalcPackOt(conn_db, paramcalc, 0, out ret, setTaskProgress);

            conn_db.Close();

            return result;
        }
        
        public bool CalcPackOt(IDbConnection conn_db, CalcTypes.ParamCalc paramcalc, int nzpPackLs, out Returns ret,
            DbCalcQueueClient.SetTaskProgressDelegate setTaskProgress)
        {
            if (Constants.Trace) Utility.ClassLog.WriteLog("Старт CalcPackOt");

            var temptableopl = "temptableopl" + DateTime.Now.Ticks;
            ExecSQL(conn_db, "drop table " + temptableopl, false);

            var packXx = new CalcTypes.PackXX(paramcalc, nzpPackLs, true);
            Returns r;

            MessageInPackLog(conn_db, packXx, "Начало отката распределения", false, out ret);
            MyDataReader reader = null;

            try
            {
                string s;
                
                if (nzpPackLs <= 0)
                {
                    s = " update " + packXx.pack_ls + " set inbasket = 0 , alg = 0" +
                        " Where nzp_pack = " + packXx.nzp_pack + " and dat_uchet is null";
                    ret = ExecSQL(conn_db, s, true);
                    if (!ret.result) return false;
                }

                //выбрать мно-во лицевых счетов
                if (paramcalc.pref.Trim() == "")
                {
                    s = " update " + packXx.pack_ls +
                        " set inbasket = 0, alg = 0, dat_uchet = null, calc_month = null " +
                        " Where nzp_pack = " + packXx.nzp_pack + 
                        " and " + sNvlWord + "(num_ls,0)=0" +
                        " and dat_uchet = '" + Points.DateOper.ToShortDateString() + "'" + sConvToDate;
                    ret = ExecSQL(conn_db, s, true);
                    return ret.result;
                }

                s =
                    " Select distinct p.nzp_pack_ls, k.nzp_dom,p.nzp_pack, k.nzp_area, k.nzp_geu, " +
                    " 0::int allow_cancel, p.dat_uchet, p.calc_month, k.pref " +
                    " Into temp " + temptableopl +
                    " From " + Points.Pref + "_data.kvar k, " + packXx.pack_ls + " p " +
                    " Where k.num_ls = p.num_ls " +
                    "   and p.nzp_pack = " + packXx.nzp_pack +
                    "   and k." + packXx.paramcalc.where_z +
                    "   and p." + packXx.where_pack_ls +
                    "   and (p.dat_uchet ='" + Points.DateOper.ToShortDateString() + "'" + sConvToDate +
                    " or p.inbasket=1)" +
                    " union all " +
                    " Select distinct p.nzp_pack_ls,0 nzp_dom,p.nzp_pack, 0 nzp_area, 0 nzp_geu, 0::int allow_cancel, p.dat_uchet, p.calc_month, '' pref From " +
                    packXx.pack_ls + " p " +
                    " Where p.nzp_pack = " + packXx.nzp_pack + 
                    " and coalesce(p.num_ls,0)=0 " +
                    " and p." +
                    packXx.where_pack_ls;
#if PG
                var retres = ClassDBUtils.ExecSQL(s, conn_db, ClassDBUtils.ExecMode.Log);
                ret = retres.GetReturnsType().GetReturns();
#else
                ret = ExecSQL(conn_db, s, true);
#endif
                if (!ret.result) return false;
#if PG
                var count = retres.resultAffectedRows;
#else
                int count = ClassDBUtils.GetAffectedRowsCount(conn_db);
#endif

                if (count == 0)
                {
                    ret = new Returns(false, "Отсутствуют оплаты для отмены распределения. Данная операция может быть произведена только над оплатами, распределёнными за " +
                        Points.DateOper.ToShortDateString(), -1);
                    MonitorLog.WriteLog(ret.text, MonitorLog.typelog.Info, 1, 222, true);
                    return false;
                }

                ExecSQL(conn_db, " Create unique index ix4_" + temptableopl + " on " + temptableopl + " (nzp_pack_ls) ", true);
                ExecSQL(conn_db, sUpdStat + " " + temptableopl, true);

                s = "select distinct pref from " + temptableopl;
                ret = ExecRead(conn_db, out reader, s, true);
                if (!ret.result) return false;

                while (reader.Read())
                {
                    var lpref = reader["pref"] != DBNull.Value ? reader["pref"].ToString().Trim() : "";
                    if (lpref == "") continue;

                    var rM = Points.GetCalcMonth(new CalcMonthParams(lpref));
                    var curMonth = new DateTime(rM.year_, rM.month_, 1);
                    var curMonth2 = new DateTime(rM.year_, rM.month_, DateTime.DaysInMonth(rM.year_, rM.month_));
                    s = " update " + temptableopl + " set allow_cancel = 1 where "+sNvlWord+"(pref, '') = ''" +
                        " and dat_uchet = " + Utils.EStrNull(Points.DateOper.ToShortDateString());
                    ret = ExecSQL(conn_db, s, true);
                    if (!ret.result)
                    {
                        MonitorLog.WriteLog(ret.text, MonitorLog.typelog.Info, 1, 222, true);
                        ExecSQL(conn_db, "drop table " + temptableopl, false);
                        return false;
                    }

                    s = "update " + temptableopl + " set allow_cancel = 1 where " +
                        " dat_uchet = " + Utils.EStrNull(Points.DateOper.ToShortDateString()) +
                        " and case when calc_month is null then true else calc_month between " + Utils.EStrNull(curMonth.ToShortDateString()) + " and " +
                        Utils.EStrNull(curMonth2.ToShortDateString()) + " end";
                    ret = ExecSQL(conn_db, s, true);
                    if (ret.result) continue;
                    MonitorLog.WriteLog(ret.text, MonitorLog.typelog.Info, 1, 222, true);
                    ExecSQL(conn_db, "drop table " + temptableopl, false);
                    return false;
                }
                reader.Close();

                var i = Points.PointList.Count + 2;
                var j = 0;
                foreach (var zap in Points.PointList)
                {
                    ret = ExecSQL(conn_db,
                        " Delete From " + 
                        zap.pref + "_charge_" + (Points.DateOper.Year - 2000).ToString("00") + tableDelimiter + "fn_supplier" + Points.DateOper.Month.ToString("00") + " f" +
                        " Where exists (Select 1 From " + temptableopl + " t where t.nzp_pack_ls = f.nzp_pack_ls and allow_cancel = 1)", true);
                    if (!ret.result) return false;

                    ret = ExecSQL(conn_db,
                        " Delete From " + 
                        zap.pref + "_charge_" + (Points.DateOper.Year - 2000).ToString("00") + tableDelimiter + "from_supplier" + " f" +
                        " Where exists ( Select nzp_pack_ls From " + temptableopl + " t where t.nzp_pack_ls = f.nzp_pack_ls and allow_cancel = 1)", true);
                    if (!ret.result) return false;

                    if (setTaskProgress == null) continue;
                    j++;
                    setTaskProgress(((decimal) j)/i);
                }
                UpdateStatistics(true, packXx.paramcalc, packXx.fn_supplier_tab, out ret);

                j++;
                if (setTaskProgress != null) setTaskProgress(((decimal) j)/i);

                s = " Update " + packXx.pack_ls + " pls " +
                    " Set dat_uchet = null, calc_month = null, " +
                    " date_rdistr = " + sCurDateTime + ", alg = 0, " +
                    " inbasket = 0 " +
                    " Where exists ( Select 1 From " + temptableopl +
                    " t where t.nzp_pack_ls = pls.nzp_pack_ls and allow_cancel = 1)";
#if PG
                retres = ClassDBUtils.ExecSQL(s, conn_db, ClassDBUtils.ExecMode.Log);
                ret = retres.GetReturnsType().GetReturns();
#else
                ret = ExecSQL(conn_db, s, true);
#endif
                if (!ret.result) return false;
#if PG
                count = retres.resultAffectedRows;
#else
                count = ClassDBUtils.GetAffectedRowsCount(conn_db);
#endif
                if (count == 0)
                {
                    ret = new Returns(false, "Оплаты не были отмены.", -1);
                    return false;
                }

                // Сохранить запись в журнал событий
                var nzpEvent = SaveEvent(6612, conn_db, paramcalc.nzp_user, paramcalc.nzp_pack, "Операционный день " + packXx.paramcalc.dat_oper);
                if ((nzpEvent > 0) && (isDebug))
                {
                    ExecSQL(conn_db,
                        " insert into " + Points.Pref + "_data" + tableDelimiter + "sys_event_detail(nzp_event, table_, nzp) " +
                        " select distinct " + nzpEvent + ",'" + packXx.pack_ls + "',nzp_pack_ls from " + temptableopl, true);
                }

                //сразу запустить расчет сальдо по nzp_pack
                MessageInPackLog(conn_db, packXx, "Расчет сальдо лицевых счетов", false, out r);

                using (var db = new DbCalcCharge())
                {
                    ret = db.CalcChargeXXUchetOplatForPack(conn_db, packXx);
                }

                j++;
                if (setTaskProgress != null) setTaskProgress(((decimal) j)/i);

                MessageInPackLog(conn_db, packXx, "Конец отката распределения", false, out r);
            }
            catch
            {
                if (reader != null) reader.Close();
                MessageInPackLog(conn_db, packXx, "Ошибка отката", true, out r);
                MonitorLog.WriteLog(ret.text, MonitorLog.typelog.Info, 1, 222, true);
                return false;
            }
            finally
            {
                ExecSQL(conn_db, "drop table " + temptableopl, false);
            }
            if (Constants.Trace) Utility.ClassLog.WriteLog("Стоп CalcPackOt");
            return true;
        }

        public bool CalcPackLs2(int nzp_pack_ls, int fin_year, DateTime date, bool to_dis, bool is_manual,
            out Returns ret, int nzp_user)
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
                        pref = (string) reader["pref"];
                    if (reader["nzp_pack"] != DBNull.Value)
                        nzp_pack = (int) reader["nzp_pack"];
                    if (reader["nzp_kvar"] != DBNull.Value)
                        nzp_kvar = (int) reader["nzp_kvar"];
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
                CalcTypes.ParamCalc paramcalc = new CalcTypes.ParamCalc(nzp_kvar, nzp_pack, pref.Trim(), yy, mm, yy, mm,
                    nzp_user);
                paramcalc.b_pack = true;
                paramcalc.b_packOt = !to_dis;


                paramcalc.DateOper = date;


                bool isManualDistribution = is_manual; //признак, что распределение ручное

                if (paramcalc.b_packOt)
                    b = CalcPackOt(conn_db, paramcalc, nzp_pack_ls, out ret, null); //откат распределения 
                else
                {
                    b = CalcPackXX(conn_db, paramcalc, nzp_pack_ls, fin_year, false, isManualDistribution, out ret, null);
                        //распределение оплаты
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
                        " delete from " + Points.Pref + "_fin_" + (Points.DateOper.Year%100).ToString("00") +
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
                        " Insert into " + Points.Pref + "_fin_" + (Points.DateOper.Year%100).ToString("00") +
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
        public bool CalcPackLs(int nzp_pack_ls, int fin_year, DateTime date, bool to_dis, bool is_manual,
            out Returns ret, int nzp_user)
        {
            return CalcPackLs(nzp_pack_ls, fin_year, date, to_dis, is_manual, out ret, nzp_user, false);
        }

        public bool CalcPackLs(int nzp_pack_ls, int fin_year, DateTime date, bool to_dis, bool is_manual,
            out Returns ret, int nzp_user, bool isCurrentMonth)
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
            int nzp_wp = 0, nzp_pack = 0;
            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result)
            {
                return false;
            }
            IDataReader reader;
            if (date.Year < Points.DateOper.Year)
            {
                StringBuilder sb =
                    new StringBuilder("select nzp_pack, nzp_wp from " + Points.Pref + "_fin_" +
                                      (date.Year%100).ToString("00") +
                                      tableDelimiter + "pack_ls pls, " + Points.Pref + "_data" + tableDelimiter +
                                      "kvar k where " +
                                      " k.num_ls = pls.num_ls and nzp_pack_ls = " + nzp_pack_ls);
                ret = ExecRead(conn_db, out reader, sb.ToString(), true);
                if (!ret.result)
                {
                    conn_db.Close();
                    return false;
                }

                if (reader.Read())
                {
                    if (reader["nzp_wp"] != DBNull.Value) nzp_wp = Convert.ToInt32(reader["nzp_wp"]);
                    if (reader["nzp_pack"] != DBNull.Value) nzp_pack = Convert.ToInt32(reader["nzp_pack"]);
                }
                reader.Close();
                ret = MessageInPackLog(conn_db, null, new PackLog()
                {
                    tableName = Points.Pref + "_fin_" + (date.Year%100).ToString("00") + tableDelimiter + "pack_log",
                    nzp_pack = nzp_pack,
                    nzp_pack_ls = nzp_pack_ls,
                    dat_oper = Points.DateOper.ToShortDateString(),
                    nzp_wp = nzp_wp,
                    message = "Оплата находится в закрытом финансовом периоде",
                    err = true
                });
                return false;
            }

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
            int nzp_kvar = 0;

            try
            {
                if (reader.Read())
                {
                    if (reader["pref"] != DBNull.Value)
                        pref = (string) reader["pref"];
                    if (reader["nzp_pack"] != DBNull.Value)
                        nzp_pack = (int) reader["nzp_pack"];
                    if (reader["nzp_kvar"] != DBNull.Value)
                        nzp_kvar = (int) reader["nzp_kvar"];
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
                CalcTypes.ParamCalc paramcalc = new CalcTypes.ParamCalc(nzp_kvar, nzp_pack, pref.Trim(), yy, mm, yy, mm,
                    nzp_user);
                paramcalc.b_pack = true;
                paramcalc.b_packOt = !to_dis;

                // !!! Исправил Марат 17.12.2012. Распределение оплаты (через корзину например) должно производится текущим оперднём                
                //paramcalc.DateOper = date;
                paramcalc.DateOper = Points.DateOper;

                bool isManualDistribution = is_manual; //признак, что распределение ручное

                if (paramcalc.b_packOt)
                    b = CalcPackOt(conn_db, paramcalc, nzp_pack_ls, out ret, null); //откат распределения 
                else
                {
                    b = CalcPackXX(conn_db, paramcalc, nzp_pack_ls, fin_year, false, isManualDistribution, out ret, null,
                        isCurrentMonth); //распределение оплаты
                    if (b)
                    {
                        if (!ret.result) b = false;
                    }
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
                        " delete from " + Points.Pref + "_fin_" + (Points.DateOper.Year%100).ToString("00") +
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
                        " Insert into " + Points.Pref + "_fin_" + (Points.DateOper.Year%100).ToString("00") +
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

        public void PackFonTasks(int nzp_pack, int fin_year, CalcFonTask.Types task, out Returns ret)
        {
            PackFonTasks(nzp_pack, fin_year, 0, task, out ret);
        }

        /// <summary>
        /// распределение или откат пачки через фоновую задачу
        /// </summary>
        /// <param name="nzp_pack"></param>
        /// <param name="task"></param>
        /// <param name="ret"></param>
        public void PackFonTasks(int nzp_pack, int fin_year, int nzp_user, CalcFonTask.Types task, out Returns ret)
        {
            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result)
            {
                return;
            }
            PackFonTasks(nzp_pack, fin_year, nzp_user, task, out ret, conn_db, null);

            conn_db.Close();
        }

        public void PackFonTasks(int nzp_pack, int fin_year, int nzp_user, CalcFonTask.Types task, out Returns ret,
            IDbConnection conn_db, IDbTransaction transaction)
        {
            if (task == CalcFonTask.Types.Unknown)
            {
                ret = new Returns(false, "Неверный код операции");
            }
            else if (task == CalcFonTask.Types.UpdatePackStatus)
            {
                CalcFonTask calcfon = new CalcFonTask(Points.GetCalcNum(0));
                calcfon.TaskType = task;
                calcfon.Status = FonTask.Statuses.New; //на выполнение 
                calcfon.nzpt = 0;
                calcfon.nzp = nzp_pack;
                calcfon.year_ = fin_year;
                calcfon.nzp_user = nzp_user;
                calcfon.txt = "'Обновление статуса пачки (код = " + nzp_pack + ", год = " + Points.DateOper.Year + ")'";

                var db = new DbCalcQueueClient();
                ret = db.AddTask(calcfon);
                db.Close();
            }
            else
            {
                ret = Utils.InitReturns();

                int yy = fin_year; // Points.CalcMonth.year_;
                string pack_ls = Points.Pref + "_fin_" + (yy - 2000).ToString("00") + tableDelimiter + "pack_ls ";
                string kvar = Points.Pref + "_data" + tableDelimiter + "kvar ";

                //для начала найдем пачку и определим все затронутые префиксы
                MyDataReader reader;

                //определим данные пачки
                string pack_text = "";
                string pack = Points.Pref + "_fin_" + (yy - 2000).ToString("00") + tableDelimiter + "pack ";
                ret = ExecRead(conn_db, transaction, out reader,
                    " Select num_pack, bank From " + pack + " p, " + Points.Pref + "_kernel" + tableDelimiter +
                    "s_bank b " +
                    " Where p.nzp_bank = b.nzp_bank " +
                    "   and p.nzp_pack = " + nzp_pack
                    , true);

                if (!ret.result) return;

                try
                {
                    if (reader.Read())
                    {
                        if (reader["num_pack"] != DBNull.Value)
                            pack_text = " Пачка № " + (string) reader["num_pack"];
                        pack_text = pack_text.Trim();

                        if (reader["bank"] != DBNull.Value)
                            pack_text += " от " + (string) reader["bank"];
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
                CalcFonTask clcfon = new CalcFonTask(Points.GetCalcNum(0));
                clcfon.TaskType = task;
                clcfon.Status = FonTask.Statuses.New; //на выполнение 
                clcfon.nzpt = 0;
                clcfon.nzp = nzp_pack;
                clcfon.year_ = fin_year;
                clcfon.txt = "'" + pack_text + "'";
                clcfon.nzp_user = nzp_user;

                var db2 = new DbCalcQueueClient();
                ret = db2.AddTask(clcfon);
                db2.Close();
            }
        }

        //проверить, что вся пачка распределена или откачена
        //-----------------------------------------------------------------------------
        public void PackFonTasks_21(CalcFonTask calcfon, out Returns ret)
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

        public void PackFonTasks_21(IDbConnection conn_web, CalcFonTask calcfon, out Returns ret)
        {
            ret = Utils.InitReturns();
            IDataReader reader;

            int flag = 21; //пачка распределена или распределена с ошибками = 22
            string dat_uchet = ",dat_uchet = '" + Points.DateOper.ToShortDateString() + "'";

            if (calcfon.TaskType == CalcFonTask.Types.CancelPackDistribution)
            {
                flag = 23; //пачка не распределена
                //dat_uchet = ",dat_uchet = NULL ";
                dat_uchet = " ";
            }

            //для этого обходим все calc_fonXX и проверяем задания по nzp_pack
            for (int i = 0; i < CalcThreads.maxCalcThreads; i++)
            {
                string tab = sDefaultSchema + "calc_fon_" + i;
                if (!TempTableInWebCashe(conn_web, tab)) continue;

                ret = ExecRead(conn_web, out reader,
                    " Select kod_info From " + tab +
                    " Where nzp   = " + calcfon.nzp +
                    //" and nzpt  = " + calcfon.nzpt +
                    " and year_ = " + calcfon.year_ +
                    " and month_= " + calcfon.month_ +
                    " and task  = " + (int) calcfon.TaskType
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
                            int k = (int) reader["kod_info"];
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

            string pack = Points.Pref + "_fin_" + (calcfon.year_ - 2000).ToString("00") + tableDelimiter + "pack ";
            string pack_ls = Points.Pref + "_fin_" + (calcfon.year_ - 2000).ToString("00") + tableDelimiter + "pack_ls ";

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
            if (calcfon.TaskType == CalcFonTask.Types.DistributePack)
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
                " Where nzp_pack = " + calcfon.nzp + "), sum_nrasp = ( " +
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
                        par_pack = (int) reader["par_pack"];
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
        private void LoadLocalPaXX(IDbConnection conn_db, CalcTypes.PackXX packXX, bool load, out Returns ret)
            //-----------------------------------------------------------------------------
        {
            //if (Constants.Trace) Utility.ClassLog.WriteLog("Старт LoadLocalPaXX");
            ret = Utils.InitReturns();
            Returns r = Utils.InitReturns();
            packXX.nzp_pack_ls = 0;
            packXX.is_local = false;
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

            sqlText = " Insert into " + packXX.fn_pa_xx +
                      " (  nzp_dom,nzp_supp,nzp_serv,nzp_area, nzp_geu,nzp_bank,dat_oper,sum_prih ) " +
                      " Select  k.nzp_dom, s.nzp_supp, s.nzp_serv, k.nzp_area, k.nzp_geu, b.nzp_payer, s.dat_uchet, sum(sum_prih) " +
                      " From  t_dom d, " + packXX.paramcalc.pref + "_data" + tableDelimiter + "kvar k," +
                      packXX.fn_supplier + " s, " + packXX.pack + " p, " + packXX.pack_ls + " pl, " + packXX.s_bank +
                      " b " +
                      " Where k.nzp_dom = d.nzp_dom and k.num_ls = s.num_ls " +
                      "   and p.nzp_pack = pl.nzp_pack " +
                      "   and pl.nzp_pack_ls = s.nzp_pack_ls " +
                      "   and p.nzp_bank = b.nzp_bank " +
                      "   and s.dat_uchet = " + packXX.paramcalc.dat_oper + sConvToDate + " " +
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
        private void CreatePaXX(IDbConnection conn_db2, CalcTypes.PackXX packXX, out Returns ret)
            //-----------------------------------------------------------------------------
        {
            CreatePaXX(conn_db2, packXX, true, out ret);
        }

        //-----------------------------------------------------------------------------
        public void CreatePaXX(IDbConnection conn_db2, CalcTypes.PackXX packXX, bool delete, out Returns ret)
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
                ret = ExecSQL(conn_db,
                    " set search_path to '" + packXX.paramcalc.pref + "_charge_" +
                    (packXX.paramcalc.calc_yy - 2000).ToString("00") + "'", true);
                ret = ExecSQL(conn_db,
                    " Create table " +
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
                ret = ExecSQL(conn_db,
                    " set search_path to '" + Points.Pref + "_fin_" + (packXX.paramcalc.calc_yy - 2000).ToString("00") +
                    "'", true);
                ret = ExecSQL(conn_db,
                    " Create table " +
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



            ret = ExecSQL(conn_db, " create unique index " +
#if PG
                ""
#else
"are."
#endif
                                   + "ix1_" + packXX.fn_pa_tab + " on " + packXX.fn_pa_tab + " (nzp_pk) ", true);
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
                                   + "ix2_" + packXX.fn_pa_tab + " on " + packXX.fn_pa_tab + " (nzp_supp, nzp_serv) ",
                true);
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
                                   + "ix4_" + packXX.fn_pa_tab + " on " + packXX.fn_pa_tab +
                                   " (dat_oper, nzp_area, nzp_geu, nzp_supp, nzp_serv) ", true);
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
                                   + "ix5_" + packXX.fn_pa_tab + " on " + packXX.fn_pa_tab + " (dat_oper, nzp_serv) ",
                true);
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
                ret = ExecSQL(conn_db, " create index " +
#if PG
                    "" +
#else
"are."+
#endif
                    "ix6_" + packXX.fn_pa_tab + " on " + packXX.fn_pa_tab + " (dat_oper, nzp_supp, nzp_serv, nzp_bl) ",
                    true);
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
        public void DistribPaXX_1(TransferBalanceFinder finder, out Returns ret,
            DbCalcQueueClient.SetTaskProgressDelegate setTaskProgress)
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


            List<int> points = new List<int>(); //в какиех nzp_wp надо пересчитать сальдо

            DateTime dat = Convert.ToDateTime(finder.dat_s);
            DateTime dat2 = Convert.ToDateTime(finder.dat_s);
            DateTime dat_po = Convert.ToDateTime(finder.dat_po);

            DateTime lastDayDatCalc;
            lastDayDatCalc = Points.DateOper;
            //lastDayDatCalc = Convert.ToDateTime(DateTime.DaysInMonth(Points.DateOper.Year, Points.DateOper.Month).ToString("00") +
            //"." + Points.DateOper.Month.ToString() + "." + Points.DateOper.Year.ToString());
            //if (lastDayDatCalc < dat_po)
            //{
            //     lastDayDatCalc = Convert.ToDateTime(DateTime.DaysInMonth(Points.DateOper.Year, Points.DateOper.Month).ToString("00") +
            //     "." + Points.DateOper.Month.ToString() + "." + Points.DateOper.Year.ToString());
            // }

            if (dat_po < lastDayDatCalc)
            {
                dat_po = lastDayDatCalc;
            }

            int i = 0;
            while (dat2 <= dat_po)
            {
                i++;
                dat2 = dat2.AddDays(1);
            }

            int j = 0;
            i = i*10;

            while (dat <= dat_po)
            {
                //расчет сальдо
                DistribPaXX(conn_db, dat, finder.nzp_wp, finder.nzp_user, out ret, setTaskProgress, i, ref j);
                if (!ret.result)
                {
                    conn_db.Close();
                    return;
                }

                dat = dat.AddDays(1);

            }

            conn_db.Close();

            /*
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
            */
        }

        //поиск ошибок за опердень fn_packXX.paramcalc.dat_oper
        //--------------------------------------------------------------------------------
        private bool PackError(IDbConnection conn_db, bool all_opermonth, DateTime dat_oper, ref List<int> points,
            out Returns ret)
            //--------------------------------------------------------------------------------
        {
            float dlt1 = 0;
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
            CalcTypes.PackXX fn_packXX = new CalcTypes.PackXX(paramcalc, 0, false);
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

                CalcTypes.PackXX local_packXX = new CalcTypes.PackXX(paramcalc, 0, false);
                local_packXX.all_opermonth = all_opermonth;
                //CreatePaXX(conn_db, local_packXX, false, out ret); //навсякий случай создать локальный fn_pa_xx

                bool b_err = false;
                //даем 3 попытки для поиска и исправление
                for (int i = 1; i <= 3; i++)
                {
                    //начинаем сверять: pack_ls = fn_supplierXX  local_fn_pa
                    ret = ExecRead(conn_db, out reader,
#if PG
                        " Select max(1) as kod, sum(g_sum_ls) as sum_rasp From " + fn_packXX.pack + " p," +
                        fn_packXX.pack_ls + " a, " + table_kvar + " b " +
                        " Where a.num_ls = b.num_ls " +
                        "   and a.dat_uchet " + fn_packXX.where_dat_oper +
                        "   and a.nzp_pack = p.nzp_pack and p.nzp_pack <> 20 " +
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
                                if ((int) reader["kod"] == 1 && reader["sum_rasp"] != DBNull.Value)
                                    f1 = Convert.ToDecimal(reader["sum_rasp"]);
                                if ((int) reader["kod"] == 2 && reader["sum_rasp"] != DBNull.Value)
                                    f2 = Convert.ToDecimal(reader["sum_rasp"]);
                                if ((int) reader["kod"] == 3 && reader["sum_rasp"] != DBNull.Value)
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
                    ret.text =
                        "Несоответсвие суммы распределения в центральном офисе с распределениями по управляющим организациям (" +
                        zap.point + ")<br>" +
                        "Обнаружены ошибки: " + zap.pref +
                        " Распределен f1=" + f1.ToString("00000000.00") + "<br>" +
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
        private void EqualPack(IDbConnection conn_db, CalcTypes.PackXX packXX, out Returns ret)
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
                " Into " +
#if PG
                    " temp " +
#else
" temp "+
#endif
                    "ttt_errp  " +
                " From " + packXX.fn_supplier + " a, t_selkvar b " +
                " Where a.nzp_pack_ls = b.nzp_pack_ls " +
                " Group by 1 ", true);
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

        private void UpdateStatistics(bool p1, CalcTypes.ParamCalc paramCalc, string p2, out Returns ret)
        {
            WorkTempKvar wk = new WorkTempKvar();
            wk.UpdateStatistics(p1, paramCalc, p2, out ret);
            wk.Close();
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
            CalcTypes.PackXX fn_packXX = new CalcTypes.PackXX(paramcalc, 0, false);

            IDataReader reader;
            bool b_calc = true;

            //перебрать все локальные банки
            foreach (_Point zap in Points.PointList)
            {
                paramcalc.pref = zap.pref;
                CalcTypes.PackXX local_packXX = new CalcTypes.PackXX(paramcalc, 0, false);

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
                            if ((int) reader["kod"] == 1 && reader["sum_rasp"] != DBNull.Value)
                                f1 = (decimal) reader["sum_rasp"];
                            if ((int) reader["kod"] == 2 && reader["sum_rasp"] != DBNull.Value)
                                f2 = (decimal) reader["sum_rasp"];
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
                        var db = new DbCalcQueueClient();
                        db.CalcSaldo(zap.nzp_wp, out ret);
                        db.Close();

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
        public bool TestPackXX2(int nzp_kvar, int nzp_pack, int fin_year, string pref, int cur_yy, int cur_mm,
            out Returns ret)
            //--------------------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            CalcTypes.ParamCalc paramcalc = new CalcTypes.ParamCalc(nzp_kvar, nzp_pack, pref, cur_yy, cur_mm, cur_yy,
                cur_mm);
            paramcalc.b_pack = true;

            string conn_kernel = Points.GetConnByPref(paramcalc.pref);
            IDbConnection conn_db = GetConnection(conn_kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result)
            {
                return false;
            }

            bool b = CalcPackXX(conn_db, paramcalc, fin_year, false, out ret);

            // Обновить инорфмацию в fn_distrib_dom_MM
            //DistribPaXX(conn_db, paramcalc, out ret,);

            conn_db.Close();

            return b;
        }


        public void PutTasksDistribOneLs(Dictionary<int, int> listPackLs, bool to_dis, string comment, out Returns ret)
        {
            //затем выставим задание на распределение или откат
            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result) return;

            CalcFonTask calcfon = new CalcFonTask(0);

            foreach (KeyValuePair<int, int> recLs in listPackLs)
            {
                calcfon.TaskType = CalcFonTask.Types.DistributeOneLs;
                calcfon.Status = FonTask.Statuses.New; //на выполнение 
                calcfon.nzpt = 1;
                calcfon.nzp_key = 0;
                calcfon.nzp = recLs.Key;
                calcfon.year_ = recLs.Value;
                calcfon.month_ = 0;

                calcfon.txt = "'" + recLs.Key + "'";

                var db = new DbCalcQueueClient();
                ret = db.AddTask(conn_web, null, calcfon);
                db.Close();

                if (!ret.result)
                {
                    break;
                }
            }

            conn_web.Close();
        }

        public bool CalcDistribPackLs(CalcFonTask calcfon, out Returns ret)
        {
            CalcPackLs(System.Convert.ToInt32(calcfon.nzp), calcfon.year_, Points.DateOper, true, false, out ret,
                calcfon.nzp_user);
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


            Decimal sum_total = 0;
            Decimal sum_total_pack = 0;
            int count_total = 0;
            float dlt0 = 0;



            //проверка: таблица с переплатами создана
            if (!TableInWebCashe(conn_web, tXX_overpayment)) return new Returns(false, "Нет выбранных переплат", -1);


#if PG
            string sql = "select count(*) cnt from " + tXX_overpayment_full +
                         " where coalesce(nzp_kvar_to,0)>0 and sum_overpay<>0  and mark = 1 and coalesce(nzp_kvar_from,0)>0  ";
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
                sql = "insert into " + baseFin +
                      ".pack (nzp_pack, nzp_bank, num_pack, dat_pack, dat_uchet, count_kv, sum_pack, flag, erc_code, pack_type, sum_rasp, sum_nrasp, file_name )" +
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
                sql = "insert into " + baseFin +
                      ".pack (nzp_pack, par_pack, nzp_bank, num_pack, dat_pack, dat_uchet, count_kv, sum_pack, flag, erc_code, file_name)" +
                      " values (default, " + par_pack + ", " + finder.nzp_bank + ",'" + finder.num_pack + "', " +
                      Utils.EStrNull(finder.dat_pack) +
                      "," + Utils.EStrNull(finder.dat_uchet) +
                      ", 0, 0, " + Pack.Statuses.Open.GetHashCode() + "," + Utils.EStrNull(erc_code, "") +
                      ",'Перенос переплат')";
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
                sql = "insert into " + baseFin +
                      ".pack (nzp_pack, par_pack, nzp_bank, num_pack, dat_pack, dat_uchet,count_kv, sum_pack, flag, erc_code, file_name)" +
                      " values (default, " + par_pack + ", " + finder.nzp_bank + ",'" + finder.num_pack + "', " +
                      Utils.EStrNull(finder.dat_pack) + "," + Utils.EStrNull(finder.dat_uchet) +
                      ", 0, 0, " + Pack.Statuses.Open.GetHashCode() + "," + Utils.EStrNull(erc_code, "") +
                      ",'Перенос переплат')";
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
                          "'" + finder.dat_pack.Trim().Trim() + "',0,0," + erc_code + ",0," + count_total + ",1, '" +
                          finder.dat_uchet + "')";
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
                    fn_supplier_tab = row3["pref_from"].ToString().Trim() + "_charge_" +
                                      (Points.DateOper.Year - 2000).ToString("00") + ".fn_supplier" +
                                      Points.DateOper.Month.ToString("00");
                    charge_xx = row3["pref_from"].ToString().Trim() + "_charge_" +
                                (Points.DateOper.Year - 2000).ToString("00") + ".charge_" +
                                Points.DateOper.Month.ToString("00");
#else
 fn_supplier_tab = row3["pref_from"].ToString().Trim() + "_charge_" + (Points.DateOper.Year - 2000).ToString("00") + ":fn_supplier" + Points.DateOper.Month.ToString("00");
                    charge_xx = row3["pref_from"].ToString().Trim() + "_charge_" + (Points.DateOper.Year - 2000).ToString("00") + ":charge_" + Points.DateOper.Month.ToString("00");
#endif

                    sql = "select * from " + charge_xx + " where nzp_kvar = " + row3["nzp_kvar_from"].ToString().Trim() +
                          " and nzp_serv >1 and dat_charge is null ";
                    dt2 = ClassDBUtils.OpenSQL(sql, conn_db, transaction).GetData();
                    sum_total = 0;
                    foreach (DataRow row4 in dt2.Rows)
                    {

#if PG
                        sql = "INSERT into " + baseFin +
                              ".gil_sums(nzp_pack_ls, num_ls, nzp_serv, nzp_supp, sum_oplat, dat_uchet) " +
                              "VALUES(" +
                              nzp_pack_ls_from + "," +
                              row4["num_ls"].ToString().Trim() + "," +
                              row4["nzp_serv"].ToString().Trim() + "," +
                              row4["nzp_supp"].ToString().Trim() + "," +
                              "" + row4["sum_outsaldo"].ToString().Trim() + ",'" +
                              Points.DateOper.ToShortDateString().Trim() + "')";
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
                        sql = "INSERT into " + fn_supplier_tab +
                              "(nzp_pack_ls, num_ls,sum_prih, s_dolg, s_user, s_forw, nzp_serv, nzp_supp, dat_uchet) " +
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
                    sql = "update " + baseFin + ".pack_ls set g_sum_ls = " + sum_total + " where nzp_pack_ls = " +
                          nzp_pack_ls_from;
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
                          "(-1)*" + row3["sum_overpay"].ToString().Trim() + ",0,0,'" + finder.dat_pack.Trim() +
                          "',33,0," +
                          "'" + Points.DateOper.ToShortDateString().Trim() + "',0,0," + erc_code + ",0," + count_total +
                          ",0,null)";
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
                sql = "update  " + baseFin + ".pack set flag = 21, sum_pack = (select sum(a.g_sum_ls) from " + baseFin +
                      ".pack_ls a WHERE a.nzp_pack = " + baseFin +
                      ".pack.nzp_pack), sum_rasp = (select sum(a.g_sum_ls) from " + baseFin +
                      ".pack_ls a WHERE a.nzp_pack = " + baseFin + ".pack.nzp_pack), sum_nrasp = 0, " +
                      "count_kv = (select count(*) from " + baseFin + ".pack_ls a WHERE a.nzp_pack = " + baseFin +
                      ".pack.nzp_pack) where nzp_pack =  " + nzp_pack_from;
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
                sql = "update  " + baseFin + ".pack set flag = 11, sum_pack = (select sum(a.g_sum_ls) from " + baseFin +
                      ".pack_ls a WHERE a.nzp_pack = " + baseFin + ".pack.nzp_pack), sum_rasp = 0, sum_nrasp = " +
                      "(select sum(a.g_sum_ls) from " + baseFin + ".pack_ls a WHERE a.nzp_pack = " + baseFin +
                      ".pack.nzp_pack), " +
                      "count_kv = (select count(*) from " + baseFin + ".pack_ls a WHERE a.nzp_pack = " + baseFin +
                      ".pack.nzp_pack) where nzp_pack =  " + nzp_pack_to;
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

                DbCalcPack db1 = new DbCalcPack();
                DbCalcPack db2 = new DbCalcPack();
                Returns ret2;


                db1.PackFonTasks(nzp_pack_to, Points.DateOper.Year, CalcFonTask.Types.DistributePack, out ret2);
                    // Отдаем пачку с переплатами на распределение
                db1.Close();
                if (!ret2.result)
                {
                    ret.result = false;
                    ret.tag = -1;
                    ret.text = "Пачка по зачислению переплат создана, но не распределена." +
                               (ret2.tag < 0 ? " " + ret2.text : "");

                }
                /*
                db2.PackFonTasks(nzp_pack_to, CalcFonTask.Types.DistributePack, out ret2);  // Отдаем пачку со снятием переплат на распределение
                db2.Close();
                if (!ret2.result)
                {
                    ret.result = false;
                    ret.tag = -1;
                    ret.text = ret.text.Trim()+ "Пачка по зачислению переплат создана, но не распределена." + (ret2.tag < 0 ? " " + ret2.text : "");                    
                }
                */

                CalcTypes.PackXX packXX = new CalcTypes.PackXX();
                packXX.paramcalc = new CalcTypes.ParamCalc(0, nzp_pack_from, "", Points.DateOper.Year,
                    Points.DateOper.Month, Points.DateOper.Year, Points.DateOper.Month);
                packXX.paramcalc.b_pack = true;
                CreateSelKvar(conn_db, packXX, out ret);
                DbCalcCharge db = new DbCalcCharge();
                ret = db.CalcChargeXXUchetOplatForPack(conn_db, packXX);
                db.Close();
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
                ret.tag = (-1)*par_pack;
                sum_total_pack = (-1)*sum_total_pack;
                ret.text = "Переплата на сумму " + sum_total_pack.ToString("N2") +
                           " руб успешно перенесена. Подробную информацию смотрите в суперпачке № " + finder.num_pack +
                           " от " + finder.dat_pack;
            }
            return ret;
        }


        // Заполнение информации для портала Небо для реестре №  nzp_nebo_reestr за период pDat_uchet_from gj DateTime pDat_uchet_to
        public bool FillSumOplForNebo(IDbConnection conn_db, int nzp_nebo_reestr, string pDat_uchet_from,
            string pDat_uchet_to, out Returns ret)
        {
            ret = Utils.InitReturns();
            string sql_text = "";

            int yy = Points.DateOper.Year;
            int mm = Points.DateOper.Month;

            string table_nebo = Points.Pref + "_fin_" + (yy - 2000).ToString("00") + ":" + "nebo_rfnsupp ";
            sql_text = "delete from " + table_nebo + " where nzp_nebo_reestr = " + nzp_nebo_reestr;
            ret = ExecSQL(conn_db, sql_text, true);
            if (!ret.result)
            {
                return false;
            }


            sql_text = "drop table tmp_nebo_kvar";
            ret = ExecSQL(conn_db, sql_text, true);

            sql_text = "select typek, pkod, num_ls, nzp_dom, nzp_kvar from " + Points.Pref +
                       "_data:kvar where typek=1 into temp tmp_nebo_kvar with no log";
            ret = ExecSQL(conn_db, sql_text, true);

            ExecSQL(conn_db, " create index idx_tmp_nebo_kvar on  tmp_nebo_kvar(num_ls, nzp_dom);", true);
            ExecSQL(conn_db, " Update statistics for table tmp_nebo_kvar ", true);

            sql_text = "drop table tmp_nebo_kvar_3";
            ret = ExecSQL(conn_db, sql_text, true);

            sql_text = "select typek, pkod, num_ls, nzp_dom, nzp_kvar from " + Points.Pref +
                       "_data:kvar where nvl(typek,1)=3  into temp tmp_nebo_kvar_3 with no log";
            ret = ExecSQL(conn_db, sql_text, true);

            ExecSQL(conn_db, " create index idx_tmp_nebo_kvar_3 on  tmp_nebo_kvar_3(num_ls, nzp_dom);", true);
            ExecSQL(conn_db, " Update statistics for table tmp_nebo_kvar_3 ", true);

            string tab_supplier = "";
            foreach (_Point zap in Points.PointList)
            {
                tab_supplier = zap.pref + "_charge_" + (Points.DateOper.Year - 2000).ToString("00") + ":fn_supplier" +
                               Points.DateOper.Month.ToString("00");
                sql_text =
                    "insert into " + table_nebo +
                    "( nzp_nebo_reestr, nzp_dom, dat_uchet,  typek, nzp_supp, nzp_serv,  sum_prih)  " +
                    "select 1, nzp_dom, dat_uchet, 1, nzp_supp, nzp_serv, sum(sum_prih) " +
                    "from " + tab_supplier + " a, tmp_nebo_kvar b  " +
                    "where a.num_ls = b.num_ls and a.sum_prih <>0 " +
                    "and dat_uchet between  '" + pDat_uchet_from.Trim() + "' and '" + pDat_uchet_to.Trim() + "' " +
                    "group by 1,2,3,4,5,6;  ";
                ret = ExecSQL(conn_db, sql_text, true);
                sql_text =
                    "insert into " + table_nebo +
                    "( nzp_nebo_reestr, nzp_dom, dat_uchet,  typek, nzp_supp, nzp_serv, num_ls,   pkod, sum_prih)  " +
                    "select '1' as nzp_nebo_reestr , b.nzp_dom, a.dat_uchet, '3' as typek, a.nzp_supp, a.nzp_serv, b.num_ls, b.pkod, sum(a.sum_prih) " +
                    "from " + tab_supplier + " a, tmp_nebo_kvar_3 b  " +
                    "where a.num_ls = b.num_ls and a.sum_prih <>0 " +
                    "and dat_uchet between  '" + pDat_uchet_from.Trim() + "' and '" + pDat_uchet_to.Trim() + "' " +
                    "group by 1,2,3,4,5,6,7,8 ";
                ret = ExecSQL(conn_db, sql_text, true);

            }

            sql_text = "drop table tmp_nebo_kvar";
            ret = ExecSQL(conn_db, sql_text, true);

            sql_text = "drop table tmp_nebo_kvar_3";
            ret = ExecSQL(conn_db, sql_text, true);

            if (!ret.result)
            {
                return false;
            }
            else return true;


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
        public int SaveEvent(int nzp_dict, IDbConnection conn_db, int nzp_user, int nzp, string note)
        {
            string sql_text = "insert into " + Points.Pref + "_data" + tableDelimiter +
                              "sys_events(date_, nzp_user, nzp_dict_event, nzp, note) values (" + sCurDateTime + "," +
                              nzp_user + "," + nzp_dict + "," + nzp + ",'" + note.Replace("'", "") + "') ";
            Returns ret = ExecSQL(conn_db, sql_text, true);

            if (!ret.result)
            {
                return 0;
            }
            else return GetSerialValue(conn_db, null);
            ;
        }

        // Сохранить показания приборов учёта
        public bool SaveCountersValsFromPackls(IDbConnection conn_db, int nzp_user, CalcTypes.ParamCalc paramcalc,
            int nzp_pack_ls, out Returns ret)
        {
            List<int> list_pack_ls = new List<int>();

            if (nzp_pack_ls == 0)
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
            }
            else
            {
                list_pack_ls.Add(nzp_pack_ls);
            }

            DbCounterKernel dbCounter = new DbCounterKernel();
            ret = dbCounter.PuValsToCountersVals(conn_db, null, list_pack_ls, nzp_user, paramcalc);
            return ret.result;

        }

        // Сохранить пометку fn_operday_dom_mc
        public bool Save_In_fn_operday_dom_mc(IDbConnection conn_db, int nzp_pack, int nzp_pack_ls, int nzp_dom,
            out Returns ret)
        {
            string fn_operday_dom;
#if PG
            fn_operday_dom = Points.Pref + "_fin_" + (Points.DateOper.Year - 2000).ToString("00") + ".fn_operday_dom_mc";
#else
            fn_operday_dom = Points.Pref + "_fin_" + (Points.DateOper.Year - 2000).ToString("00") + ":fn_operday_dom_mc";
#endif

            ret = ExecSQL(conn_db,
                " insert into  " + fn_operday_dom + "(nzp_dom, date_oper) values (" + nzp_dom + ",'" +
                Points.DateOper.ToShortDateString() + "'" + sConvToDate + " ) ", true);
            return ret.result;

        }



        // Учесть перекидки
        public bool Update_reval(IDbConnection conn_db, IDbTransaction transactionID, CalcTypes.PackXX fn_packXX,
            bool flgUpdateTotal, out Returns ret)
        {

            string sql_text;
            ExecSQL(conn_db, transactionID, " Drop table ttt_paxx ", false);
#if PG
            sql_text =
                " Select nzp_payer, nzp_area, nzp_dom,nzp_serv, -1 as nzp_bank, 1 as kod, sum(sum_reval) as sum_reval " +
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

            int count = 0;
#if PG
#warning Проверить на postgree

            IntfResultType retres = ClassDBUtils.ExecSQL(sql_text, conn_db, ClassDBUtils.ExecMode.Log);
            ret = retres.GetReturnsType().GetReturns();
            count = retres.resultAffectedRows;
#else
            count = ClassDBUtils.GetAffectedRowsCount(conn_db, transactionID);
#endif
            if ((!ret.result) || (count == 0))
            {

                ret = ExecSQL(conn_db, transactionID,
                    " Update " + fn_packXX.fn_distrib +
                    " Set sum_reval = 0 " +
                    " Where dat_oper = " + fn_packXX.paramcalc.dat_oper
                    , true);


            }


            //ExecSQL(conn_db, transactionID, " Create unique index ix1_ttt_paxx on ttt_paxx (nzp_payer, nzp_area, nzp_dom, nzp_serv, nzp_bank) ", true);
            ExecSQL(conn_db, transactionID,
                " Create  index ix1_ttt_paxx on ttt_paxx (nzp_payer, nzp_area, nzp_dom, nzp_serv, nzp_bank) ", true);

            ExecSQL(conn_db, transactionID, DBManager.sUpdStat + " ttt_paxx ", true);




            ExecSQL(conn_db, "drop table t_helpsdistrib", false);

            sqlText = "Create temp table t_helpsdistrib(" +
                      " nzp_supp integer," +
                      " nzp_payer integer," +
                      " nzp_area integer," +
                      " nzp_dom integer," +
                      " nzp_serv integer," +
                      " nzp_bank integer)" + DBManager.sUnlogTempTable;
            ret = ExecSQL(conn_db, sqlText, true);

            if (!ret.result)
            {
                return false;
            }

            sqlText = " insert into t_helpsdistrib( nzp_payer, nzp_area, nzp_dom, nzp_serv, nzp_bank)" +
                      " select  distinct nzp_payer, nzp_area, nzp_dom, nzp_serv, nzp_bank " +
                      " From " + fn_packXX.fn_distrib + " a " +
                      " Where dat_oper = " + fn_packXX.paramcalc.dat_oper + sConvToDate;
            ret = ExecSQL(conn_db, sqlText, true);

            if (!ret.result)
            {
                return false;
            }

            ExecSQL(conn_db, " Create  index ix1_t_helpsdistrib on t_helpsdistrib (nzp_supp, " +
                             " nzp_payer, nzp_area, nzp_dom, nzp_serv, nzp_bank) ", true);

            ExecSQL(conn_db, DBManager.sUpdStat + "  t_helpsdistrib", true);


            sql_text = " Update ttt_paxx " +
                       " Set kod = 0 " +
                       " Where 0 < ( Select count(*) From t_helpsdistrib a " +
                       " Where a.nzp_payer= ttt_paxx.nzp_payer " +
                       "   and a.nzp_area = ttt_paxx.nzp_area " +
                       "   and a.nzp_dom = ttt_paxx.nzp_dom " +
                       "   and a.nzp_serv = ttt_paxx.nzp_serv " +
                       "   and a.nzp_bank = ttt_paxx.nzp_bank " +
                       " ) ";

            ret = ExecSQL(conn_db, transactionID, sql_text, true);
            if (!ret.result)
            {
                return false;
            }
            ExecSQL(conn_db, "drop table t_helpsdistrib", true);

            ExecSQL(conn_db, transactionID, " Create index ix2_ttt_paxx on ttt_paxx (kod) ", true);
#if PG
            ExecSQL(conn_db, transactionID, " analyze ttt_paxx ", true);
#else
            ExecSQL(conn_db,transactionID, " Update statistics for table ttt_paxx ", true);
#endif
            //DataTable drr2 = ViewTbl(conn_db, " select * from ttt_paxx  ");
            sql_text = " Insert into " + fn_packXX.fn_distrib +
                       " ( nzp_payer,nzp_area, nzp_dom,nzp_serv,nzp_bank,dat_oper ) " +
                       " Select nzp_payer,nzp_area, nzp_dom,nzp_serv,nzp_bank, " + fn_packXX.paramcalc.dat_oper +
                       " From ttt_paxx Where kod = 1 ";
            ret = ExecSQL(conn_db, transactionID, sql_text, true);

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

            ret = ExecSQL(conn_db, transactionID, sql_text, true);
            if (!ret.result)
            {
                return false;
            }

            ExecSQL(conn_db, transactionID, " Drop table ttt_paxx ", false);

            //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
            //расчет итогового сальдо
            //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
            if (flgUpdateTotal)
            {
                ret = ExecSQL(conn_db, transactionID,
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

        public void UpdateSaldoFndistrib(DateTime min_dat_oper, int nzp_payer, int nzp_area, out Returns ret)
            //-----------------------------------------------------------------------------
        {
            ret = new Returns(true);

            DateTime dat = min_dat_oper;
            DateTime lastDayDatCalc = Points.DateOper;

            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return;

            try
            {
                //Обновить сальдо sum_out в fn_distrib_dom с указанной даты по текущий день
                while (dat <= lastDayDatCalc)
                {
                    ret = UpdateSumOutSaldo(conn_db, dat, nzp_payer);
                    if (!ret.result) throw new Exception(ret.text);
                    dat = dat.AddDays(1);
                }
            }
            catch (Exception ex)
            {
                ret.result = false;
                MonitorLog.WriteLog("Ошибка при учете перечисления: " + ex.Message, MonitorLog.typelog.Info, 1, 222,
                    true);
                ret.text = "Ошибка при учете перечисления";
            }
        }

        //Пересчитать проценты удержания с текущей даты
        //-----------------------------------------------------------------------------
        public void RecalcUderFndistrib(IDbConnection conn_db, DateTime min_dat_oper, out Returns ret,
            DbCalcQueueClient.SetTaskProgressDelegate setTaskProgress)
            //-----------------------------------------------------------------------------
        {
            ret = Utils.InitReturns();


            //dbQueueClient.SetTaskProgress();

            DateTime dat = min_dat_oper;
            DateTime dat2 = min_dat_oper;

            //DateTime lastDayDatCalc = Convert.ToDateTime(DateTime.DaysInMonth(Points.DateOper.Year, Points.DateOper.Month).ToString("00") +
            //     "." + Points.DateOper.Month.ToString() + "." + Points.DateOper.Year.ToString());
            DateTime lastDayDatCalc = Points.DateOper;

            int yy = min_dat_oper.Year;
            int mm = min_dat_oper.Month;
            CalcTypes.ParamCalc paramcalc = new CalcTypes.ParamCalc(-1, -1, "", yy, mm, yy, mm);

            int i = 0;

            while (dat2 <= lastDayDatCalc)
            {
                dat2 = dat2.AddDays(1);
                i = i + 1;
            }

            int j = 0;
            while (dat <= lastDayDatCalc)
            {
                RecalcCommission(conn_db, dat);
                dat = dat.AddDays(1);
                if (setTaskProgress != null)
                {
                    j++;
                    setTaskProgress(((decimal) j)/i);
                }
                //dbQueueClient.SetTaskProgress();
            }
        }

        public Returns UpdatePackStatus(Pack finder)
        {
            string connectionString = Points.GetConnByPref(Points.Pref);
            IDbConnection conn_db = GetConnection(connectionString);
            Returns ret = OpenDb(conn_db, true);
            if (!ret.result) return ret;

            ret = UpdatePackStatus(finder, conn_db, null);

            conn_db.Close();
            return ret;
        }

        public Returns UpdatePackStatus(Pack finder, IDbConnection conn_db, IDbTransaction transaction)
        {
            #region Проверка входных параметров

            if (finder.nzp_user < 1) return new Returns(false, "Пользователь не определен");
            if (finder.nzp_pack < 1) return new Returns(false, "Пачка не определена");
            if (finder.year_ < 1) return new Returns(false, "Не задан год");

            #endregion


#if PG
            string table = Points.Pref + "_fin_" + (finder.year_%100).ToString("00") + ".pack";
#else
            string table = Points.Pref + "_fin_" + (finder.year_ % 100).ToString("00") + ":pack";
#endif
            string sql = "update " + table + " set flag = " + finder.flag + " where nzp_pack = " + finder.nzp_pack;
            Returns ret = ExecSQL(conn_db, transaction, sql, true);

            return ret;
        }

        public Returns SelectOverPayments(OverPaymentsParams finder)
        {
            var connectionString = Points.GetConnByPref(Points.Pref);
            var connDb = GetConnection(connectionString);
            var ret = OpenDb(connDb, true);
            if (!ret.result) return ret;

            try
            {
                var calcfon = new CalcFonTask(Points.GetCalcNum(0))
                {
                    TaskType = CalcFonTask.Types.taskBalanceSelect,
                    Status = FonTask.Statuses.New,
                    nzpt = 0,
                    parameters = JsonConvert.SerializeObject(finder),
                    nzp_user = finder.nzp_user,
                    txt = "Отбор переплат"
                };
                using (var db = new DbCalcQueueClient())
                {
                    ret = db.AddTask(calcfon);
                }
                if (ret.result)
                {
                    var finderFon = new OverpaymentStatusFinder
                    {
                        nzp_status = (int) OverpaymentStatusFinder.Statuses.overpSelection,
                        nzp_fon_selection = ret.tag,
                        nzp_user = finder.nzp_user
                    };
                    ret = SetStatusOverpaymentManProc(finderFon);
                }
                else return ret;
            }
            finally
            {
                connDb.Close();
            }
            return ret;
        }

        public Returns InsertOverPayments(OverPaymentsParams finder, DbCalcQueueClient.SetTaskProgressDelegate setTaskProgress)
        {
            var connectionString = Points.GetConnByPref(Points.Pref);
            var connDb = GetConnection(connectionString);
            var ret = OpenDb(connDb, true);
            if (!ret.result) return ret;

            var tOverpayments = Points.Pref + sDataAliasRest + "overpayment";
            ret = ExecSQL(connDb, "truncate " + tOverpayments, false);
            if (!ret.result)
            {
                connDb.Close();
                return ret;
            }
            var cnt = 10;
            var i = 0.1;
            StringBuilder sql;
            var nzpWps = "";
            int k = 0;
            foreach (var pref in finder.prefs)
            {
                var rM = Points.GetCalcMonth(new CalcMonthParams(pref));

                if (nzpWps == "") nzpWps += Points.GetPoint(pref).nzp_wp.ToString();
                else nzpWps += "," + Points.GetPoint(pref).nzp_wp;

                //переплаты
                sql = new StringBuilder("insert into ");
                sql.AppendFormat(
                    " {0} (pref, num_ls, nzp_kvar, nzp_serv, nzp_supp, nzp_payer, isdel, sum_outsaldo, rsum_tarif, sum_negative_outsaldo_payer)",
                    tOverpayments);
                sql.AppendFormat(" select '{0}', ch.num_ls, ch.nzp_kvar,", pref);
                sql.Append(" ch.nzp_serv, ch.nzp_supp, b.nzp_payer, ch.isdel, ch.sum_outsaldo, ch.rsum_tarif, 0 ");
                sql.AppendFormat(" from {0}_charge_{1}{2}charge_{3} ch,", pref, (rM.year_%100).ToString("00"),
                    tableDelimiter, rM.month_.ToString("00"));
                sql.AppendFormat(" {0}_kernel{1}supplier s, ", Points.Pref, tableDelimiter);
                sql.AppendFormat(" {0}_data{1}fn_dogovor_bank_lnk lnk, ", Points.Pref, tableDelimiter);
                sql.AppendFormat(" {0}_data{1}fn_bank b ", Points.Pref, tableDelimiter);
                sql.Append(
                    " where s.nzp_supp = ch.nzp_supp and s.fn_dogovor_bank_lnk_id = lnk.id and lnk.nzp_fb = b.nzp_fb ");
                sql.Append(" and ch.dat_charge is null and ch.nzp_serv > 1 ");
                if (finder.select_only_isdel)
                    sql.Append(" and ch.isdel = 1 "); //если есть это условие, то отобрать переплату по закрытым услугам
                if (finder.uchet_peni)
                    sql.Append(" and not exists (select 1 from " + Points.Pref + "_kernel" + tableDelimiter + "peni_settings ps where ch.nzp_serv = ps.nzp_peni_serv) ");
                        //если есть это условие, то не стоит галка Учитывать переплату по пени
                sql.Append(" and ch.sum_outsaldo < 0  "); //отрицательное исходящее сальдо
                sql.Append(" and s.allow_overpayments = 1 ");
                ret = ExecSQL(connDb, sql.ToString(), true);
                if (!ret.result)
                {
                    connDb.Close();
                    return ret;
                }
                k++;
                if (setTaskProgress != null) setTaskProgress(((decimal)0.5)/finder.prefs.Count / cnt*k);
                ExecSQL(connDb, sUpdStat + " " + tOverpayments, true);

                //долги по выбранным ЛС
                sql = new StringBuilder("insert into ");
                sql.AppendFormat(
                    " {0} (pref, num_ls, nzp_kvar, nzp_serv, nzp_supp, nzp_payer, isdel, sum_outsaldo, rsum_tarif, sum_negative_outsaldo_payer)",
                    tOverpayments);
                sql.AppendFormat(" select '{0}', ch.num_ls, ch.nzp_kvar, ", pref);
                sql.Append(" ch.nzp_serv, ch.nzp_supp, b.nzp_payer, ch.isdel, ch.sum_outsaldo, ch.rsum_tarif , 0 ");
                sql.AppendFormat(" from {0}_charge_{1}{2}charge_{3} ch,", pref, (rM.year_%100).ToString("00"),
                    tableDelimiter, rM.month_.ToString("00"));
                sql.AppendFormat(" {0}_kernel{1}supplier s, ", Points.Pref, tableDelimiter);
                sql.AppendFormat(" {0}_data{1}fn_dogovor_bank_lnk lnk, ", Points.Pref, tableDelimiter);
                sql.AppendFormat(" {0}_data{1}fn_bank b ", Points.Pref, tableDelimiter);
                sql.Append(" where ");
                sql.Append(" ch.dat_charge is null and ch.nzp_serv > 1 and ");
                sql.Append("s.nzp_supp = ch.nzp_supp and s.fn_dogovor_bank_lnk_id = lnk.id and lnk.nzp_fb = b.nzp_fb ");
                sql.Append(" and ch.sum_outsaldo > 0 "); //положительное исходящее сальдо
                sql.AppendFormat(" and exists (select 1 from {0} where ch.nzp_kvar = nzp_kvar and pref = '{1}') ",
                    tOverpayments, pref);
                ret = ExecSQL(connDb, sql.ToString(), true);
                if (!ret.result)
                {
                    connDb.Close();
                    return ret;
                }
                k++;
                if (setTaskProgress != null) setTaskProgress(((decimal)0.5) / finder.prefs.Count / cnt * k);
            }
            i = 1;     
            
            //удалить ЛС где нет долга
            sql = new StringBuilder(" delete from ");
            sql.AppendFormat(" {0} t where not exists (select 1 from {0} where t.nzp_kvar = nzp_kvar and sum_outsaldo > 0 ) ", tOverpayments);
            ret = ExecSQL(connDb, sql.ToString(), true);
            if (!ret.result)
            {
                connDb.Close();
                return ret;
            }
            if (setTaskProgress != null) setTaskProgress(((decimal)i) / cnt);
            i++;

            //Записать  сумму переплаты по получателю
            sql = new StringBuilder(" drop table ttt ");
            ret = ExecSQL(connDb, sql.ToString(), true);
            //if (!ret.result)
            //{
            //    connDb.Close();
            //    return ret;
            //}

            sql = new StringBuilder(" select nzp_payer, sum(sum_outsaldo) sum_ into temp ttt from ");
            sql.AppendFormat(" {0} where sum_outsaldo < 0 ", tOverpayments);
            sql.Append(" group by nzp_payer");
            ret = ExecSQL(connDb, sql.ToString(), true);
            if (!ret.result)
            {
                connDb.Close();
                return ret;
            }
       

            //Записать  сумму переплаты по получателю
            sql = new StringBuilder(" update ");
            sql.AppendFormat(" {0} t set sum_negative_outsaldo_payer = ", tOverpayments);
            sql.AppendFormat(" (select sum_ from ttt where nzp_payer = t.nzp_payer) where exists (select 1 from ttt where ttt.nzp_payer = t.nzp_payer)");
            ret = ExecSQL(connDb, sql.ToString(), true);
            if (!ret.result)
            {
                connDb.Close();
                return ret;
            }
            if (setTaskProgress != null) setTaskProgress(((decimal)i) / cnt);
            i++;

            sql = new StringBuilder(" drop table ttt ");
            ExecSQL(connDb, sql.ToString(), false);

          
            
            //определить средства получателей
            sql = new StringBuilder(" drop table tempSumPayer ");
            ExecSQL(connDb, sql.ToString(), false);

            sql = new StringBuilder(" create temp table tempSumPayer(nzp_payer integer, sum_rasp numeric(17,2))");
            ret = ExecSQL(connDb, sql.ToString(), true);
            if (!ret.result)
            {
                connDb.Close();
                return ret;
            }
            if (setTaskProgress != null) setTaskProgress(((decimal)i) / cnt);
            i++;

            sql = new StringBuilder(" insert into tempSumPayer(nzp_payer) ");
            sql.Append(" select distinct nzp_payer from ");
            sql.Append(tOverpayments);
            ret = ExecSQL(connDb, sql.ToString(), true);
            if (!ret.result)
            {
                connDb.Close();
                return ret;
            }

            sql = new StringBuilder(" create index ix1_tempSumPayer on tempSumPayer (nzp_payer)");
            ExecSQL(connDb, sql.ToString(), false);

            sql = new StringBuilder(" drop table tempSumPayerPrefs ");
            ExecSQL(connDb, sql.ToString(), false);

            sql = new StringBuilder(" create temp table tempSumPayerPrefs(pref character(20), nzp_payer integer, sum_rasp numeric(17,2))");
            ret = ExecSQL(connDb, sql.ToString(), true);
            if (!ret.result)
            {
                connDb.Close();
                return ret;
            }

            MyDataReader reader;

            sql = new StringBuilder("select nzp_payer from tempSumPayer");
            ret = ExecRead(connDb, out reader, sql.ToString(), true);
            if (!ret.result)
            {
                connDb.Close();
                return ret;
            }
            if (setTaskProgress != null) setTaskProgress(((decimal)i) / cnt);
            i=6;
            while (reader.Read())
            {
                var nzpPayer = (reader["nzp_payer"] != DBNull.Value) ? Convert.ToInt32(reader["nzp_payer"]) : 0;
                if (nzpPayer == 0) continue;

                foreach (var pref in finder.prefs)
                {
                    var rM = Points.GetCalcMonth(new CalcMonthParams(pref));
                    var curMonth = new DateTime(rM.year_, rM.month_, 1);
                    if (finder.dat_s == DateTime.MinValue) finder.dat_s = curMonth.AddMonths(-1);
                    if (finder.dat_po == DateTime.MinValue) finder.dat_po = curMonth;

                    for (var y = finder.dat_s.Year; y < finder.dat_po.Year + 1; y++)
                    {
                        for (var m = 1; m < 13; m++)
                        {
                            if (y == finder.dat_s.Year && m < finder.dat_s.Month) continue;
                            if (y == finder.dat_po.Year && m > finder.dat_po.Month) continue;
                            
                            var whereDatUchet = " ";
                            if (finder.dat_s == finder.dat_po) whereDatUchet = " and f.dat_uchet = '" + finder.dat_s.ToShortDateString() +"'";
                            else
                            {
                                if (y == finder.dat_s.Year && m == finder.dat_s.Month) whereDatUchet = " and f.dat_uchet >= '"+finder.dat_s.ToShortDateString()+"'";
                                if (y == finder.dat_po.Year && m == finder.dat_po.Month) whereDatUchet = " and f.dat_uchet <= '" + finder.dat_po.ToShortDateString() + "'";
                            }
                            var tname = pref + "_charge_" + (y%100).ToString("00") + tableDelimiter + "fn_supplier" +
                                        m.ToString("00");
                            if (!TempTableInWebCashe(connDb, tname)) continue;

                            sql = new StringBuilder("insert into tempSumPayerPrefs(pref, nzp_payer, sum_rasp) ");
                            sql.AppendFormat(" select '{0}', b.nzp_payer, sum(sum_prih) from ", pref);
                            sql.AppendFormat(" {0} f, ", tname);
                            sql.AppendFormat(" {0}_kernel{1}supplier s, ", Points.Pref, tableDelimiter);
                            sql.AppendFormat(" {0}_data{1}fn_dogovor_bank_lnk lnk, ", Points.Pref, tableDelimiter);
                            sql.AppendFormat(" {0}_data{1}fn_bank b ", Points.Pref, tableDelimiter);
                            sql.Append(" where s.nzp_supp = f.nzp_supp and s.fn_dogovor_bank_lnk_id = lnk.id ");
                            sql.Append(" and lnk.nzp_fb = b.nzp_fb ");
                            sql.AppendFormat(" and b.nzp_payer = {0} ", nzpPayer);
                            sql.Append(whereDatUchet);
                            sql.Append(" group by 1, 2");
                            ret = ExecSQL(connDb, sql.ToString(), true);
                            if (!ret.result)
                            {
                                reader.Close();
                                connDb.Close();
                                return ret;
                            }
                        }
                    }
                }
            }
            i = 7;
            if (setTaskProgress != null) setTaskProgress(((decimal)i) / cnt);
            reader.Close();

            ret = ExecSQL(connDb, "truncate tempSumPayer", true);
            if (!ret.result)
            {
                connDb.Close();
                return ret;
            }

            sql = new StringBuilder("insert into tempSumPayer ");
            sql.Append(" select nzp_payer, sum(sum_rasp) from tempSumPayerPrefs ");
            sql.Append(" group by nzp_payer");
            ret = ExecSQL(connDb, sql.ToString(), true);
            if (!ret.result)
            {
                connDb.Close();
                return ret;
            }

            //Записать распределенную сумму по получателю
            sql = new StringBuilder(" update ");
            sql.AppendFormat("{0} t set sum_payer = (select sum_rasp from tempSumPayer where nzp_payer = t.nzp_payer)", tOverpayments);
             ret = ExecSQL(connDb, sql.ToString(), true);
            if (!ret.result)
            {
                connDb.Close();
                return ret;
            }
            if (setTaskProgress != null) setTaskProgress(((decimal)i) / cnt);
            i++;

            #region заполняем табличку для интерфейса
            using (DbSprav db = new DbSprav())
            {
                try
                {
                    ret = db.PrepareOverpaymentDataForInterface(connDb);
                }
                catch (Exception ex)
                {
                    MonitorLog.WriteLog("Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name +
                                        "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
            }
            #endregion
            if (setTaskProgress != null) setTaskProgress(((decimal)i) / cnt);
            return ret;
        }

        public List<OverpaymentStatusFinder> GetOverpaymentManStatus(Finder finder, out Returns ret)
        {
            var list = new List<OverpaymentStatusFinder>();

            string connectionString = Points.GetConnByPref(Points.Pref);
            IDbConnection conn_db = GetConnection(connectionString);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return list;

            try
            {
                string sql = 
                    " SELECT o.*, u.comment as user, s.status as status " +
                    " FROM " + Points.Pref + sDataAliasRest + "overpaymentman_status o" +
                    " LEFT OUTER JOIN " + Points.Pref + sDataAliasRest + "users u ON o.nzp_user = u.nzp_user" +
                    " LEFT OUTER JOIN " + Points.Pref + sKernelAliasRest + "s_overpaymentman_statuses s ON s.nzp_status = o.nzp_status " +
                    " WHERE o.is_actual <> 100 ";
                DataTable dt = DBManager.ExecSQLToTable(conn_db, sql);
                foreach (DataRow row in dt.Rows)
                {
                    var s = new OverpaymentStatusFinder();
                    if (row["id"] != DBNull.Value) s.id = Convert.ToInt32(row["id"]);
                    if (row["nzp_status"] != DBNull.Value) s.nzp_status = Convert.ToInt32(row["nzp_status"]);
                    if (row["nzp_user"] != DBNull.Value) s.nzp_user = Convert.ToInt32(row["nzp_user"]);
                    if (row["nzp_fon_selection"] != DBNull.Value) s.nzp_fon_selection = Convert.ToInt32(row["nzp_fon_selection"]);
                    if (row["nzp_fon_distrib"] != DBNull.Value) s.nzp_fon_distrib = Convert.ToInt32(row["nzp_fon_distrib"]);
                    if (row["dat_when"] != DBNull.Value) s.dat_when = Convert.ToDateTime(row["dat_when"]);
                    if (row["is_actual"] != DBNull.Value) s.is_actual = Convert.ToInt32(row["is_actual"]);
                    if (row["user"] != DBNull.Value) s.user = Convert.ToString(row["user"]).Trim();
                    if (row["status"] != DBNull.Value) s.status = Convert.ToString(row["status"]).Trim();
                    list.Add(s);
                }
            }
            catch (Exception ex)
            {
                ret = new Returns(false);
                MonitorLog.WriteLog("Ошибка GetOverpaymentManStatus: " + ex.Message + ex.StackTrace,
                    MonitorLog.typelog.Info, true);
                return new List<OverpaymentStatusFinder>();
            }
            finally
            {
                conn_db.Close();
                
            }
            return list;
        }

        public Returns SetStatusOverpaymentManProc(OverpaymentStatusFinder finder)
        {
            Returns ret = Utils.InitReturns();

            if (finder.nzp_user < 1)
                return new Returns(false, "Не определен пользователь", -1);
            if (finder.nzp_status < 1)
                return new Returns(false, "Не определен статус", -1);

            string connectionString = Points.GetConnByPref(Points.Pref);
            IDbConnection connDB = GetConnection(connectionString);
            ret = OpenDb(connDB, true);
            if (!ret.result) return ret;
            

            try
            {
                string sql;
                if (finder.id > 0)
                {
                    sql =
                        " SELECT id FROM " + Points.Pref + sDataAliasRest + "overpaymentman_status " +
                        " WHERE is_actual <> 100";
                    DataTable dtActual = DBManager.ExecSQLToTable(connDB, sql);
                    if (dtActual.Rows.Count == 0)
                        return new Returns(false, "Нет актуального процесса", -1);
                    if (dtActual.Rows.Count > 1)
                        return new Returns(false, "Найдено больше одного актуального процесса", -1);
                    if (dtActual.Rows[0]["id"].ToString() != finder.id.ToString())
                        return new Returns(false, "Код текущего процесса не совпадает с текущим актуальным процессом", -1);

                    if (finder.nzp_status != (int)OverpaymentStatusFinder.Statuses.overpDistrib)
                        return new Returns(false, "Неправильный статус - должен быть 'Распределение оплат'", -1);
                    if (finder.nzp_fon_distrib < 1)
                        return new Returns(false, "Не заполнен код фонового задания 'Распределение оплат'", -1);

                    if (finder.is_actual == 100)
                    {
                        //заканчиваем процесс
                        sql =
                            " UPDATE " + Points.Pref + sDataAliasRest + "overpaymentman_status " +
                            " SET dat_when = " + sCurDateTime + "," +
                            " is_actual = " + finder.is_actual + " " +
                            " WHERE id = " + finder.id;
                        ret = ExecSQL(connDB, sql);
                    }
                    else
                    {
                        //переходим от отбора оплат к распределению
                        sql =
                            " UPDATE " + Points.Pref + sDataAliasRest + "overpaymentman_status " +
                            " SET nzp_status = " + finder.nzp_status + ", " +
                            " nzp_user = " + finder.nzp_user + "," +
                            " nzp_fon_distrib = " + finder.nzp_fon_distrib + "," +
                            " dat_when = " + sCurDateTime + " " +
                            " WHERE id = " + finder.id;
                        ret = ExecSQL(connDB, sql);
                    }
                }
                else
                {
                    //начинаем процесс с отбора оплат
                    if (finder.nzp_status != (int)OverpaymentStatusFinder.Statuses.overpSelection)
                        return new Returns(false, "Неправильный статус - должен быть 'Отбор оплат'", -1);
                    if (finder.nzp_fon_selection < 1)
                        return new Returns(false, "Не заполнен код фонового задания 'Отбор оплат'", -1);

                    //всем имеющимся ставим is_actual = 100, добавляем новую строчку
                    sql =
                        " UPDATE " + Points.Pref + sDataAliasRest + "overpaymentman_status " +
                        " SET is_actual = 100 ";
                    ret = ExecSQL(connDB, sql);

                    sql =
                        " INSERT INTO " + Points.Pref + sDataAliasRest + "overpaymentman_status " +
                        " (nzp_status, nzp_user, nzp_fon_selection, nzp_fon_distrib, dat_when, is_actual)" +
                        " VALUES" +
                        " (" + finder.nzp_status + ", " + finder.nzp_user + ", " + finder.nzp_fon_selection + "," +
                        " 0, " + sCurDateTime + ", 1)";
                    ret = ExecSQL(connDB, sql);
                }
            }
            catch (Exception ex)
            {
                ret.result = false;
                MonitorLog.WriteLog("Ошибка в функции " + System.Reflection.MethodBase.GetCurrentMethod().Name +
                    ":\n " + ex.Message, MonitorLog.typelog.Error, true);
            }
            finally
            {
                connDB.Close();
            }
            return ret;
        }

        public Returns GetCurOverPaymentsProcId()
        {
            Returns ret = Utils.InitReturns();
            string connectionString = Points.GetConnByPref(Points.Pref);
            IDbConnection conn_db = GetConnection(connectionString);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return ret;

            try
            {
                string sql =
                    " SELECT id " +
                    " FROM " + Points.Pref + sDataAliasRest + "overpaymentman_status" +
                    " WHERE is_actual <> 100 ";
                DataTable dt = DBManager.ExecSQLToTable(conn_db, sql);
                if (dt.Rows.Count != 1)
                    return new Returns(false, "Ошибка получения актуального процесса управления переплатами:" +
                                              " количество действующих не равно 1");
                else if (dt.Rows[0]["id"] == null || ! Int32.TryParse(dt.Rows[0]["id"].ToString(), out ret.tag))
                    return new Returns(false, "Ошибка получения актуального процесса управления переплатами:" +
                                              " значение пустое");
            }
            catch (Exception ex)
            {
                ret = new Returns(false);
                MonitorLog.WriteLog("Ошибка GetOverpaymentManStatus: " + ex.Message + ex.StackTrace, MonitorLog.typelog.Info, true);
            }
            finally
            {
                conn_db.Close();
            }
            return ret;
        }

        public Returns DistribOverPayments(DistrOverPaymentsParams finder, DbCalcQueueClient.SetTaskProgressDelegate setTaskProgress)
        {
            var connectionString = Points.GetConnByPref(Points.Pref);
            var connDb = GetConnection(connectionString);
            var ret = OpenDb(connDb, true);
            if (!ret.result) return ret;

            var tOverpayments = "tOverpayments";
            var overpayment = Points.Pref + sDataAliasRest + "overpayment";

            var sql = new StringBuilder("drop table " + tOverpayments);
            ExecSQL(connDb, sql.ToString(), false);

            //Выбрать отмеченные
            sql = new StringBuilder("update ");
            sql.AppendFormat(" {0} o set mark = (select case when mark = true then 1 else 0 end from {1}_data{2}joined_overpayments j where ",
                overpayment, Points.Pref, tableDelimiter); 
            sql.Append(" o.nzp_supp = j.nzp_supp and o.nzp_serv = j.nzp_serv and o.sum_negative_outsaldo_payer = j.sum_negative_outsaldo_payer and j.sum_outsaldo >= 0)");
            sql.Append(" where o.sum_outsaldo >= 0");
            ret = ExecSQL(connDb, sql.ToString(), true);
            if (!ret.result)
            {
                connDb.Close();
                return ret;
            }

            sql = new StringBuilder(" update ");
            sql.AppendFormat(" {0} o set mark = (select case when mark = true then 1 else 0 end from {1}_data{2}joined_overpayments j where ",
                overpayment, Points.Pref, tableDelimiter);
            sql.Append(" o.nzp_supp = j.nzp_supp and o.nzp_serv = j.nzp_serv and o.sum_negative_outsaldo_payer = j.sum_negative_outsaldo_payer and j.sum_outsaldo < 0) ");
            sql.Append(" where o.sum_outsaldo < 0 ");
            ret = ExecSQL(connDb, sql.ToString(), true);
            if (!ret.result)
            {
                connDb.Close();
                return ret;
            }

            //Выбрать отмеченные записи 
            sql = new StringBuilder("select * into temp "+tOverpayments+" from " + overpayment + " where mark = 1");
            ret = ExecSQL(connDb, sql.ToString(), true);
            if (!ret.result)
            {
                connDb.Close();
                return ret;
            }
            if (setTaskProgress != null) setTaskProgress(((decimal)1) / 10);
            sql = new StringBuilder("create index i1_tOverpayments on " + tOverpayments + "(nzp_kvar)");
            ret = ExecSQL(connDb, sql.ToString(), true);
            if (!ret.result)
            {
                connDb.Close();
                return ret;
            }

            sql = new StringBuilder(" update ");
            sql.AppendFormat(" {0} t set sum_payer = 0 where sum_payer is null ", tOverpayments);
            ret = ExecSQL(connDb, sql.ToString(), true);
            if (!ret.result)
            {
                connDb.Close();
                return ret;
            }

      //      #region Подготовка к распределению
            //по каждому ЛС записать сумму долга
            sql = new StringBuilder(" update ");
            sql.AppendFormat(" {0} t set sum_dolg_ls = coalesce((select sum(sum_outsaldo) from {0} where sum_outsaldo > 0 and t.nzp_kvar = nzp_kvar),0), ", tOverpayments);
            //по каждому ЛС записать сумму переплаты
            sql.AppendFormat("sum_over_ls = coalesce((select sum(sum_outsaldo)*(-1) from {0} where sum_outsaldo < 0 and t.nzp_kvar = nzp_kvar),0), ", tOverpayments);
            sql.AppendFormat("rsum_tarif_ls = coalesce((select sum(rsum_tarif) from {0} where t.nzp_kvar = nzp_kvar " +
                             " and not exists (select 1 from " + Points.Pref + "_kernel" + tableDelimiter + "peni_settings ps where t.nzp_serv = ps.nzp_peni_serv) " +
                             " and isdel = 0 and rsum_tarif > 0 and sum_outsaldo > 0 ),0)", tOverpayments);
            ret = ExecSQL(connDb, sql.ToString(), true);
            if (!ret.result)
            {
                connDb.Close();
                return ret;
            }

            //сумма которую реально будем распределять
            //если в пределах долга
            if (finder.distr_within_dolg)
            {
                sql = new StringBuilder("update");
                sql.AppendFormat(" {0} t set sum_change = (case when sum_dolg_ls < sum_over_ls then sum_dolg_ls else sum_over_ls end)", tOverpayments);
                ret = ExecSQL(connDb, sql.ToString(), true);
                if (!ret.result)
                {
                    connDb.Close();
                    return ret;
                }
            }
            else
            {
                //если не в пределах долга
                sql = new StringBuilder("update");
                sql.AppendFormat(" {0} t set sum_change = sum_over_ls", tOverpayments);
                ret = ExecSQL(connDb, sql.ToString(), true);
                if (!ret.result)
                {
                    connDb.Close();
                    return ret;
                }
            }

            //определяем часть суммы идущей на погашение долга и аванса
            sql = new StringBuilder("update");
            sql.AppendFormat(" {0} t set", tOverpayments);
            sql.Append(" sum_dolg  =  (case when sum_dolg_ls < sum_change then sum_dolg_ls else sum_change end), ");
            sql.Append(" sum_avans = (case when sum_dolg_ls < sum_change then sum_change - sum_dolg_ls else 0 end)"); 
            ret = ExecSQL(connDb, sql.ToString(), true);
            if (!ret.result)
            {
                connDb.Close();
                return ret;
            }
            if (setTaskProgress != null) setTaskProgress(((decimal)2) / 10);
            //исправим сумму аванса: в ЛС где нет начислений
            sql = new StringBuilder("update");
            sql.AppendFormat(" {0}  set ", tOverpayments);
            sql.Append("sum_avans = 0, sum_change = sum_change - sum_avans ");
            sql.Append(" where rsum_tarif_ls = 0");
            ret = ExecSQL(connDb, sql.ToString(), true);
            if (!ret.result)
            {
                connDb.Close();
                return ret;
            }

            //  распределение по долгу
            sql = new StringBuilder("update");
            sql.AppendFormat(" {0} t set", tOverpayments);
            sql.Append(" sum_dolg_d = sum_dolg*sum_outsaldo/sum_dolg_ls");
            sql.Append(" where sum_dolg_ls > 0 and sum_outsaldo > 0");
            ret = ExecSQL(connDb, sql.ToString(), true);
            if (!ret.result)
            {
                connDb.Close();
                return ret;
            }

            //распределение аванса
            sql = new StringBuilder("update");
            sql.AppendFormat(" {0} t set", tOverpayments);
            sql.Append(" sum_avans_d = sum_avans*rsum_tarif/rsum_tarif_ls");
            sql.Append(" where rsum_tarif_ls > 0 and sum_outsaldo > 0 and rsum_tarif > 0 "+
                " and not exists (select 1 from " + Points.Pref + "_kernel" + tableDelimiter + "peni_settings ps where t.nzp_serv = ps.nzp_peni_serv) ");
            ret = ExecSQL(connDb, sql.ToString(), true);
            if (!ret.result)
            {
                connDb.Close();
                return ret;
            }
            if (setTaskProgress != null) setTaskProgress(((decimal)3) / 10);
            //определение остатка
            sql = new StringBuilder("update");
            sql.AppendFormat(" {0} t set", tOverpayments);
            sql.AppendFormat(
                " sum_dolg_d_ost = sum_dolg - coalesce((select sum(sum_dolg_d) from {0} where t.nzp_kvar = nzp_kvar),0),",
                tOverpayments);
            sql.AppendFormat(
                " sum_avans_d_ost = sum_avans - coalesce((select sum(sum_avans_d) from {0} where t.nzp_kvar = nzp_kvar),0) ",
                tOverpayments);
            sql.Append(" where sum_outsaldo > 0 ");
            ret = ExecSQL(connDb, sql.ToString(), true);
            if (!ret.result)
            {
                connDb.Close();
                return ret;
            }

            MyDataReader reader;
            
            //проверка на наличие положительно остатка долга
            sql = new StringBuilder(" select 1 from ");
            sql.AppendFormat(" {0} where sum_dolg_d_ost>0 limit 1 ", tOverpayments);
            ret = ExecRead(connDb, out reader, sql.ToString(), true);
            if (!ret.result)
            {
                connDb.Close();
                return ret;
            }
            if (setTaskProgress != null) setTaskProgress(((decimal)4) / 10);
            var i = 0;//на всякий случай, чтобы избежать зацикливания
            while (reader.Read() && i < 3)//цикл
            {
                reader.Close();

                //выравнивание остатка
                sql = new StringBuilder("update");
                sql.AppendFormat(" {0} t set", tOverpayments);
                sql.Append(" sum_dolg_d = case when sum_dolg_d + sum_dolg_d_ost < sum_outsaldo then sum_dolg_d + sum_dolg_d_ost else sum_outsaldo end ");
                sql.Append(" where sum_dolg_d_ost > 0 and sum_outsaldo > 0 ");
                sql.AppendFormat(" and nzp_key = (select max(nzp_key) from {0} t1  where t.nzp_kvar = t1.nzp_kvar and sum_outsaldo - sum_dolg_d =  ", tOverpayments);
                sql.AppendFormat(" (select max(sum_outsaldo - sum_dolg_d) from {0} t2 where t2.nzp_kvar = t1.nzp_kvar and sum_dolg_d_ost > 0))", tOverpayments);
                ret = ExecSQL(connDb, sql.ToString(), true);
                if (!ret.result)
                {
                    connDb.Close();
                    return ret;
                }

                //определение остатка
                sql = new StringBuilder("update");
                sql.AppendFormat(" {0} t set", tOverpayments);
                sql.Append(" sum_dolg_d_ost = sum_dolg -  ");
                sql.AppendFormat(" coalesce((select sum(sum_dolg_d) from {0} where t.nzp_kvar = nzp_kvar),0) ",
                    tOverpayments);
                sql.AppendFormat(" where sum_outsaldo > 0");
                ret = ExecSQL(connDb, sql.ToString(), true);
                if (!ret.result)
                {
                    connDb.Close();
                    return ret;
                }

                //проверка на наличие положительно остатка долга
                sql = new StringBuilder(" select 1 from ");
                sql.AppendFormat(" {0} where sum_dolg_d_ost>0 limit 1 ", tOverpayments);
                ret = ExecRead(connDb, out reader, sql.ToString(), true);
                if (!ret.result)
                {
                    connDb.Close();
                    return ret;
                }

                i++;
            } //цикл конец
            reader.Close();
            
            //проверка на наличие отрицательного остатка
            sql = new StringBuilder(" select 1 from ");
            sql.AppendFormat(" {0} where sum_dolg_d_ost<0 limit 1", tOverpayments);
            ret = ExecRead(connDb, out reader, sql.ToString(), true);
            if (!ret.result)
            {
                connDb.Close();
                return ret;
            }

            i = 0;
            while(reader.Read() && i < 3)//цикл
            {
                //выравнивание остатка
                sql = new StringBuilder("update");
                sql.AppendFormat(" {0} t set", tOverpayments);
                sql.Append(" sum_dolg_d = case when sum_dolg_d + sum_dolg_d_ost > 0 then sum_dolg_d + sum_dolg_d_ost else 0 end ");
                sql.Append(" where sum_dolg_d_ost < 0 and sum_outsaldo > 0 ");
                sql.AppendFormat(" and nzp_key = (select max(nzp_key) from {0} t1 where t.nzp_kvar = t1.nzp_kvar and ", tOverpayments);
                sql.AppendFormat(" sum_dolg_d = (select max(sum_dolg_d) from {0} t2 where t2.nzp_kvar = t1.nzp_kvar and sum_dolg_d_ost < 0  ))", tOverpayments);
                ret = ExecSQL(connDb, sql.ToString(), true);
                if (!ret.result)
                {
                    connDb.Close();
                    return ret;
                }

                //определение остатка
                sql = new StringBuilder("update");
                sql.AppendFormat(" {0} t set", tOverpayments);
                sql.AppendFormat(" sum_dolg_d_ost = sum_dolg - coalesce((select sum(sum_dolg_d) from {0} where t.nzp_kvar = nzp_kvar),0) ", tOverpayments);
                sql.Append(" where sum_outsaldo > 0");
                ret = ExecSQL(connDb, sql.ToString(), true);
                if (!ret.result)
                {
                    connDb.Close();
                    return ret;
                }

                //проверка на наличие отрицательного остатка
                sql = new StringBuilder(" select 1 from ");
                sql.AppendFormat(" {0} where sum_dolg_d_ost<0 limit 1", tOverpayments);
                ret = ExecRead(connDb, out reader, sql.ToString(), true);
                if (!ret.result)
                {
                    connDb.Close();
                    return ret;
                }

                i++;
            }//цикл конец
            reader.Close();

            //проверка на наличие положительно остатка аванса
            sql = new StringBuilder(" select 1 from ");
            sql.AppendFormat(" {0} where sum_avans_d_ost>0 limit 1", tOverpayments);
            ret = ExecRead(connDb, out reader, sql.ToString(), true);
            if (!ret.result)
            {
                connDb.Close();
                return ret;
            }

            if (reader.Read())
            {
                var nopeni = " and not exists (select 1 from " + Points.Pref + "_kernel" + tableDelimiter +
                           "peni_settings ps where {ALIAS}.nzp_serv = ps.nzp_peni_serv) ";
                //выравнивание остатка аванса
                sql = new StringBuilder("update");
                sql.AppendFormat(" {0} t set", tOverpayments);
                sql.Append(" sum_avans_d = sum_avans_d + sum_avans_d_ost ");
                sql.Append(" where sum_avans_d_ost > 0 and sum_outsaldo > 0 and isdel = 0 " + nopeni.Replace("{ALIAS}", "t"));
                sql.AppendFormat(" and nzp_key = (select max(nzp_key) from {0} t1 where t.nzp_kvar = t1.nzp_kvar " +
                                 " and t1.sum_avans_d_ost > 0 and t1.sum_outsaldo > 0 and t1.isdel = 0 " + nopeni.Replace("{ALIAS}", "t1") +
                                 " and  sum_avans_d = (select max(sum_avans_d) from {0} t2 where t2.nzp_kvar = t1.nzp_kvar " +
                                 " and t2.sum_avans_d_ost > 0  and t2.sum_outsaldo > 0 and t2.isdel = 0 " + nopeni.Replace("{ALIAS}", "t2") + "))", tOverpayments);
                ret = ExecSQL(connDb, sql.ToString(), true);
                if (!ret.result)
                {
                    connDb.Close();
                    return ret;
                }

                //определение остатка
                sql = new StringBuilder("update");
                sql.AppendFormat(" {0} t set", tOverpayments);
                sql.AppendFormat(" sum_avans_d_ost = sum_avans - coalesce((select sum(sum_avans_d) from {0} where t.nzp_kvar = nzp_kvar),0) ", tOverpayments);
                sql.Append(" where sum_outsaldo > 0 and  sum_avans_d_ost > 0");
                ret = ExecSQL(connDb, sql.ToString(), true);
                if (!ret.result)
                {
                    connDb.Close();
                    return ret;
                }
            }
            reader.Close();

            //цикл
            //проверка на наличие отрицательного остатка
            sql = new StringBuilder(" select 1 from ");
            sql.AppendFormat("{0} where sum_avans_d_ost<0 ", tOverpayments);
            ret = ExecRead(connDb, out reader, sql.ToString(), true);
            if (!ret.result)
            {
                connDb.Close();
                return ret;
            }

            i = 0;
            while (reader.Read() && i < 3)
            {
                reader.Close();
                //выравнивание остатка
                sql = new StringBuilder("update");
                sql.AppendFormat(" {0} t set", tOverpayments);
                sql.Append(" sum_avans_d = case when sum_avans_d + sum_avans_d_ost > 0 then sum_avans_d + sum_avans_d_ost else 0 end ");
                sql.Append(" where sum_avans_d_ost < 0 and sum_outsaldo > 0 and isdel = 0 ");
                sql.AppendFormat(" and nzp_key = (select max(nzp_key) from {0} t1 where t.nzp_kvar = nzp_kvar ",
                    tOverpayments);
                sql.AppendFormat(" and sum_avans_d = (select max(sum_avans_d) from {0} t2 where t2.nzp_kvar = t1.nzp_kvar and sum_avans_d_ost < 0))", tOverpayments);
                ret = ExecSQL(connDb, sql.ToString(), true);
                if (!ret.result)
                {
                    connDb.Close();
                    return ret;
                }

                //определение остатка
                sql = new StringBuilder("update");
                sql.AppendFormat(" {0} t set", tOverpayments);
                sql.AppendFormat(" sum_avans_d_ost = sum_avans - coalesce((select sum(sum_avans_d) from {0} where t.nzp_kvar = nzp_kvar),0) ",
                    tOverpayments);
                sql.Append(" where sum_outsaldo > 0");
                ret = ExecSQL(connDb, sql.ToString(), true);
                if (!ret.result)
                {
                    connDb.Close();
                    return ret;
                }

                //проверка на наличие отрицательного остатка
                sql = new StringBuilder(" select 1 from ");
                sql.AppendFormat("{0} where sum_avans_d_ost<0 limit 1 ", tOverpayments);
                ret = ExecRead(connDb, out reader, sql.ToString(), true);
                if (!ret.result)
                {
                    connDb.Close();
                    return ret;
                }
                i++;

            }//цикл конец
            if (setTaskProgress != null) setTaskProgress(((decimal)5) / 10);
            //распределение отрицательного сальдо
            sql = new StringBuilder("update");
            sql.AppendFormat(" {0} t set", tOverpayments);
            sql.Append(" sum_over_d = sum_change*sum_outsaldo/sum_over_ls ");
            sql.Append(" where sum_over_ls > 0 and sum_outsaldo < 0");
            ret = ExecSQL(connDb, sql.ToString(), true);
            if (!ret.result)
            {
                connDb.Close();
                return ret;
            }

            //определение остатка
            sql = new StringBuilder("update");
            sql.AppendFormat(" {0} t set", tOverpayments);
            sql.AppendFormat(" sum_over_d_ost = - sum_change - coalesce((select sum(sum_over_d) from {0} where t.nzp_kvar = nzp_kvar),0) ",
                tOverpayments);
            sql.Append(" where sum_outsaldo < 0");
            ret = ExecSQL(connDb, sql.ToString(), true);
            if (!ret.result)
            {
                connDb.Close();
                return ret;
            }
            
            //проверка на наличие положительно остатка долга
            sql = new StringBuilder(" select 1 from ");
            sql.AppendFormat(" {0} where sum_over_d_ost>0 ", tOverpayments);
            ret = ExecRead(connDb, out reader, sql.ToString(), true);
            if (!ret.result)
            {
                connDb.Close();
                return ret;
            }
            i = 0;
            while(reader.Read() && i < 3)//цикл
            {
                //выравнивание остатка
                sql = new StringBuilder("update");
                sql.AppendFormat(" {0} t set", tOverpayments);
                sql.Append(" sum_over_d = case when sum_over_d + sum_over_d_ost < 0 then sum_over_d + sum_over_d_ost else 0 end ");
                sql.Append(" where sum_over_d_ost > 0 and sum_outsaldo < 0 ");
                sql.AppendFormat(" and nzp_key = (select max(nzp_key) from {0} t1  where t.nzp_kvar = t1.nzp_kvar  and t1.sum_outsaldo < 0 ", tOverpayments);
                sql.AppendFormat(" and sum_over_d = (select min(sum_over_d) from {0} t2 where t2.nzp_kvar = t1.nzp_kvar and sum_over_d_ost > 0 and t1.sum_outsaldo < 0 ))", tOverpayments);
                ret = ExecSQL(connDb, sql.ToString(), true);
                if (!ret.result)
                {
                    connDb.Close();
                    return ret;
                }

                //определение остатка
                sql = new StringBuilder("update");
                sql.AppendFormat(" {0} t set", tOverpayments);
                sql.AppendFormat(" sum_over_d_ost = - sum_change - coalesce((select sum(sum_over_d) from {0} where t.nzp_kvar = nzp_kvar),0) ",
                    tOverpayments);
                sql.Append(" where sum_outsaldo < 0 ");
                ret = ExecSQL(connDb, sql.ToString(), true);
                if (!ret.result)
                {
                    connDb.Close();
                    return ret;
                }

                 //проверка на наличие положительно остатка долга
                sql = new StringBuilder(" select 1 from ");
                sql.AppendFormat(" {0} where sum_over_d_ost>0 limit 1 ", tOverpayments);
                ret = ExecRead(connDb, out reader, sql.ToString(), true);
                if (!ret.result)
                {
                    connDb.Close();
                    return ret;
                }
                i++;
            }//цикл конец

            reader.Close();

            //цикл
            //проверка на наличие отрицательного остатка
            sql = new StringBuilder(" select 1 from ");
            sql.AppendFormat(" {0} where sum_over_d_ost<0 limit 1 ", tOverpayments);
            ret = ExecRead(connDb, out reader, sql.ToString(), true);
            if (!ret.result)
            {
                connDb.Close();
                return ret;
            }

            i = 0;
            while (reader.Read() && i < 3)
            {
                //выравнивание остатка
                sql = new StringBuilder("update");
                sql.AppendFormat(" {0} t set", tOverpayments);
                sql.Append(" sum_over_d = case when sum_over_d + sum_over_d_ost > sum_outsaldo then sum_over_d + sum_over_d_ost else sum_outsaldo end ");
                sql.Append(" where sum_over_d_ost < 0 and sum_outsaldo < 0 ");
                sql.AppendFormat(" and nzp_key = (select max(nzp_key) from {0} t1 where t.nzp_kvar = t1.nzp_kvar and t1.sum_over_d_ost < 0 and t1.sum_outsaldo < 0 ", tOverpayments);
                sql.AppendFormat(" and sum_outsaldo - sum_over_d = (select min(sum_outsaldo - sum_over_d) from {0} t2 where t2.nzp_kvar = t1.nzp_kvar and t2.sum_over_d_ost < 0 and t2.sum_outsaldo < 0 ))",tOverpayments);
                ret = ExecRead(connDb, out reader, sql.ToString(), true);
                if (!ret.result)
                {
                    connDb.Close();
                    return ret;
                }

                //определение остатка
                sql = new StringBuilder("update");
                sql.AppendFormat(" {0} t set", tOverpayments);
                sql.AppendFormat(" sum_over_d_ost = - sum_change - coalesce((select sum(sum_over_d) from {0} where t.nzp_kvar = nzp_kvar),0) ", tOverpayments);
                sql.Append(" where sum_outsaldo < 0");
                ret = ExecRead(connDb, out reader, sql.ToString(), true);
                if (!ret.result)
                {
                    connDb.Close();
                    return ret;
                }

                //проверка на наличие отрицательного остатка
                sql = new StringBuilder(" select 1 from ");
                sql.AppendFormat(" {0} where sum_over_d_ost<0 limit 1", tOverpayments);
                ret = ExecRead(connDb, out reader, sql.ToString(), true);
                if (!ret.result)
                {
                    connDb.Close();
                    return ret;
                }
                i++;
            }//цикл конец
            reader.Close();

            string tableForpack = tOverpayments;
            if (setTaskProgress != null) setTaskProgress(((decimal)6) / 10);
            if (finder.remove_within_distr)
            {
                //формирование отсортированного списка
                sql = new StringBuilder(" drop table tkvar");
                ExecSQL(connDb, sql.ToString(), false);

                sql = new StringBuilder("create temp table tkvar(nzp_kvar integer, id serial not null, sum_change numeric(14,2))"); 
                ret = ExecSQL(connDb, sql.ToString(), false);
                if (!ret.result)
                {
                    connDb.Close();
                    return ret;
                }
                
                sql = new StringBuilder(" insert into  tkvar (nzp_kvar, sum_change) ");
                sql.AppendFormat(" select distinct nzp_kvar, sum_change from {0}", tOverpayments);
                sql.Append(" order by sum_change " + (finder.ordering == 2 ? " asc " : " desc ") + ", nzp_kvar");
                ret = ExecSQL(connDb, sql.ToString(), false);
                if (!ret.result)
                {
                    connDb.Close();
                    return ret;
                }

                sql = new StringBuilder("create index ix1_tkvar on tkvar (nzp_kvar)");
                ret = ExecSQL(connDb, sql.ToString(), false);
                if (!ret.result)
                {
                    connDb.Close();
                    return ret;
                }
                if (setTaskProgress != null) setTaskProgress(((decimal)7) / 10);
                sql = new StringBuilder("update ");
                sql.AppendFormat(" {0} t set ordering_down = (select id from tkvar where t.nzp_kvar = nzp_kvar)", tOverpayments);
                ret = ExecSQL(connDb, sql.ToString(), false);
                if (!ret.result)
                {
                    connDb.Close();
                    return ret;
                }

                sql = new StringBuilder("drop table tkvar");
                ret = ExecSQL(connDb, sql.ToString(), false);
                if (!ret.result)
                {
                    connDb.Close();
                    return ret;
                }

                sql = new StringBuilder("create index ix7_overpaymenths on "+tOverpayments+" (ordering_down)");
                ret = ExecSQL(connDb, sql.ToString(), false);
                if (!ret.result)
                {
                    connDb.Close();
                    return ret;
                }

                var sign = "<="; //в порядке уменьшения переплаты
                if (finder.ordering == 2) sign = ">="; //в порядке увеличения


                sql = new StringBuilder("select max(ordering_down) ");
                sql.AppendFormat(" from {0} ", tOverpayments);
                object obj = ExecScalar(connDb, sql.ToString(), out ret, true);
                if (!ret.result)
                {
                    connDb.Close();
                    return ret;
                }

                sql = new StringBuilder("create index i2_tOverpayments on " + tOverpayments + "(ordering_down)");
                ret = ExecSQL(connDb, sql.ToString(), true);
                if (!ret.result)
                {
                    connDb.Close();
                    return ret;
                }
                if (obj != DBNull.Value)
                {
                    int prefcnt = Convert.ToInt32(Math.Ceiling(Convert.ToDecimal(obj)/1000));
                    if (finder.ordering == 1)
                    {
                        for (var a = 0; a < prefcnt; a++)
                        {
                            sql = new StringBuilder(" update ");
                            sql.AppendFormat(
                                " {0} t set sum_up = (select max(case when ordering_down = {2} then sum_up else 0 end) + " +
                                " sum(case when ordering_down > {2} then sum_over_d else 0 end) from {0} where ordering_down <= t.ordering_down and ordering_down <= {1} and ordering_down >= {2}) " +
                                "where ordering_down <= {1} and ordering_down > {2}",
                                tOverpayments, (a + 1)*1000, a*1000);
                            ret = ExecSQL(connDb, sql.ToString(), false);
                            if (!ret.result)
                            {
                                connDb.Close();
                                return ret;
                            }
                           
                            if (setTaskProgress != null)
                                setTaskProgress((decimal) 0.7 + ((decimal) a)/prefcnt/10);
                        }
                    }
                }

                tableForpack = "new_" + tOverpayments;
                sql = new StringBuilder(" drop table " + tableForpack);
                ExecSQL(connDb, sql.ToString(), false);

                sql = new StringBuilder("select * into " + tableForpack + " from " + tOverpayments + " where sum_up <= sum_payer");
                ret = ExecSQL(connDb, sql.ToString(), false);
                if (!ret.result)
                {
                    connDb.Close();
                    return ret;
                }
            }
            if (setTaskProgress != null) setTaskProgress(((decimal)8) / 10);
            //формирование pack_ls и gil_sums
            ret = FormPacks(connDb, tableForpack, finder);
            if (!ret.result)
            {
                connDb.Close();
                return ret;
            }

            sql = new StringBuilder(" drop table " + tableForpack);
            ExecSQL(connDb, sql.ToString(), false);

            sql = new StringBuilder(" drop table " + tOverpayments);
            ExecSQL(connDb, sql.ToString(), false);
            if (setTaskProgress != null) setTaskProgress(((decimal)9) / 10);
            //удаление отобранных переплат
            sql = new StringBuilder(" truncate " + overpayment);
            ExecSQL(connDb, sql.ToString(), false);

            sql = new StringBuilder(" truncate " + Points.Pref + "_data" + tableDelimiter + "joined_overpayments");
            ExecSQL(connDb, sql.ToString(), false);
            return ret;

        }
        
        private Returns FormPacks(IDbConnection connDb, string tOverpayments, DistrOverPaymentsParams finder)
        {
            MyDataReader reader, reader2;
            
            //суперпачка
            var parPack = 0;
            var sql = new StringBuilder("select 1 from " + tOverpayments + " limit 1");
            var ret = ExecRead(connDb, out reader, sql.ToString(), true);
            if (!ret.result) return ret;
            if (reader.Read())
            {
                reader.Close();
                sql = new StringBuilder("insert into ");
                sql.AppendFormat(" {0}_fin_{1}{2}pack ", Points.Pref, (Points.DateOper.Year % 100).ToString("00"), tableDelimiter);
                sql.Append(" (par_pack, pack_type, num_pack, dat_uchet, dat_pack, flag, dat_vvod, nzp_rs, file_name, nzp_bank) ");
                sql.AppendFormat(" values ({4}, 10, '{0}', '{1}', '{2}', 23, now(), 1, 'Переплаты {3}', 79998) ",
                    DateTime.Now.Day.ToString("00") + DateTime.Now.Month.ToString("00") + DateTime.Now.Year,
                    Points.DateOper.ToShortDateString(), Points.DateOper.ToShortDateString(), Points.GetPoint(reader["pref"].ToString()).point, parPack);
                ret = ExecSQL(connDb, sql.ToString(), true);
                if (!ret.result) return ret;
                parPack = GetSerialValue(connDb);

                sql = new StringBuilder("update ");
                sql.AppendFormat("{0}_fin_{1}{2}pack set par_pack = {3} where nzp_pack = {3}", Points.Pref, (Points.DateOper.Year % 100).ToString("00"), tableDelimiter, parPack);
                ret = ExecSQL(connDb, sql.ToString(), true);
                if (!ret.result) return ret;
            }
            else reader.Close();

            sql = new StringBuilder("select distinct pref from " + tOverpayments);
            ret = ExecRead(connDb, out reader, sql.ToString(), true);
            if (!ret.result) return ret;

            var ercCode = "";
            using (var db = new DbPack())
            {
                ercCode = db.GetErcCode(out ret, connDb);
            }
            
            while (reader.Read())
            {
                //Создать пачку
                sql = new StringBuilder("insert into ");
                sql.AppendFormat(" {0}_fin_{1}{2}pack ", Points.Pref, (Points.DateOper.Year%100).ToString("00"), tableDelimiter);
                sql.Append(" (par_pack, pack_type, num_pack, dat_uchet, dat_pack, flag, dat_vvod, nzp_rs, file_name, nzp_bank) ");
                sql.AppendFormat(" values ({4}, 10, '{0}', '{1}', '{2}', 23, now(), 1, 'Переплаты {3}', 79998) ",
                    DateTime.Now.Day.ToString("00") + DateTime.Now.Month.ToString("00") + DateTime.Now.Year, 
                    Points.DateOper.ToShortDateString(), Points.DateOper.ToShortDateString(), Points.GetPoint(reader["pref"].ToString()).point, parPack);
                ret = ExecSQL(connDb, sql.ToString(), true);
                if (!ret.result)
                {
                    reader.Close();
                    return ret;
                }
                int nzpPack = GetSerialValue(connDb);

                sql = new StringBuilder("drop SEQUENCE shipments_ship_id_seq_");
                ExecSQL(connDb, sql.ToString(), false);
                sql = new StringBuilder(" CREATE SEQUENCE shipments_ship_id_seq_ START 1 INCREMENT 1");
                ExecSQL(connDb, sql.ToString(), false);

                //Создать оплату
                sql = new StringBuilder("insert into ");
                sql.AppendFormat(" {0}_fin_{1}{2}pack_ls ", Points.Pref, (Points.DateOper.Year % 100).ToString("00"), tableDelimiter);
                sql.AppendFormat(" (nzp_pack, num_ls, g_sum_ls, erc_code, dat_month, kod_sum, paysource, dat_vvod, info_num, nzp_user) ");
                sql.AppendFormat("select {0}, num_ls, sum(sum_dolg_d+sum_avans_d+sum_over_d), {1}, '{2}', 33, 1, now(), nextval('shipments_ship_id_seq_'), {3} from ", 
                    nzpPack, ercCode, Points.DateOper.ToShortDateString(), finder.nzp_user);
                sql.Append(tOverpayments + " t ");
                sql.AppendFormat(" where pref = '{0}' ", reader["pref"]);
                sql.AppendFormat(" and exists (select 1 ");
                sql.AppendFormat(" from {0}", tOverpayments);
                sql.Append(" where nzp_kvar = t.nzp_kvar and sum_over_d <> 0) ");
                sql.Append(" group by num_ls");
                ret = ExecSQL(connDb, sql.ToString(), true);
                if (!ret.result)
                {
                    reader.Close();
                    return ret;
                }

                sql = new StringBuilder("update ");
                sql.AppendFormat(" {0} t set nzp_pack_ls = (select nzp_pack_ls from", tOverpayments);
                sql.AppendFormat(" {0}_fin_{1}{2}pack_ls ", Points.Pref, (Points.DateOper.Year % 100).ToString("00"), tableDelimiter);
                sql.AppendFormat(" where nzp_pack = {0} and num_ls = t.num_ls) where pref = '{1}' ", nzpPack, reader["pref"]);
                ret = ExecSQL(connDb, sql.ToString(), true);
                if (!ret.result)
                {
                    reader.Close();
                    return ret;
                }

                //Создать уточнение
                sql = new StringBuilder("insert into ");
                sql.AppendFormat(" {0}_fin_{1}{2}gil_sums ", Points.Pref, (Points.DateOper.Year%100).ToString("00"),
                    tableDelimiter);
                sql.Append("(nzp_pack_ls, num_ls, nzp_serv, nzp_supp, sum_oplat, dat_month)");
                sql.AppendFormat(" select nzp_pack_ls, num_ls, nzp_serv, nzp_supp, sum_dolg_d+sum_avans_d+sum_over_d, '{0}'",
                    Points.DateOper.ToShortDateString());
                sql.AppendFormat(" from {0}", tOverpayments);
                sql.AppendFormat(" where pref = '{0}' and nzp_pack_ls is not null", reader["pref"].ToString().Trim());
                ret = ExecSQL(connDb, sql.ToString(), true);
                if (!ret.result)
                {
                    reader.Close();
                    return ret;
                }

                sql = new StringBuilder("update ");
                sql.AppendFormat(" {0}_fin_{1}{2}pack ", Points.Pref, (Points.DateOper.Year % 100).ToString("00"), tableDelimiter);
                sql.AppendFormat(" set count_kv = (select count(*) from {0}_fin{1}pack_ls where nzp_pack = {2}) ",
                    Points.Pref, "_" + (Points.DateOper.Year % 100).ToString("00") + tableDelimiter, nzpPack);
                sql.AppendFormat(" where nzp_pack = {0}", nzpPack);
                ret = ExecSQL(connDb, sql.ToString(), true);
                if (!ret.result)
                {
                    reader.Close();
                    return ret;
                }
            }
            reader.Close();

            return ret;
        }

        public Returns CheckChoosenOverPyment(OverpaymentStatusFinder finder)
        {
            string connectionString = Points.GetConnByPref(Points.Pref);
            IDbConnection connDB = GetConnection(connectionString);
            Returns ret = OpenDb(connDB, true);
            if (!ret.result) return ret;

            sql = new StringBuilder("select count(*) from ");
            sql.AppendFormat("{0}_data{1}joined_overpayments j where ", Points.Pref, tableDelimiter);
            sql.Append(" j.sum_outsaldo >= 0 and mark = true ");
            var obj = ExecScalar(connDB, sql.ToString(), out ret, true);
            if (!ret.result)
            {
                connDB.Close();
                return ret;
            }

            if (Convert.ToInt32(obj) == 0)
            {
                connDB.Close();
                return new Returns(false, "Выберите договора ЖКУ/услуги, по которым необходимо устранить переплату", -1);
            }

            sql = new StringBuilder("select count(*) from ");
            sql.AppendFormat("{0}_data{1}joined_overpayments j where ", Points.Pref, tableDelimiter);
            sql.Append(" j.sum_outsaldo < 0 and mark = true ");
            obj = ExecScalar(connDB, sql.ToString(), out ret, true);
            if (!ret.result)
            {
                connDB.Close();
                return ret;
            }

            if (Convert.ToInt32(obj) == 0)
            {
                connDB.Close();
                return new Returns(false, "Выберите договора ЖКУ/услуги, на которые будет происходить распределение переплаты", -1);
            }

            connDB.Close();
            return ret;
        }
    }
}

