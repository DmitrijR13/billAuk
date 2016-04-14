using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using STCLINE.KP50.Global;
using System.Data;
using STCLINE.KP50.IFMX.Kernel.source.CommonType;
using STCLINE.KP50.Interfaces;

namespace STCLINE.KP50.DataBase
{
    public partial class DbCalcPack : DataBaseHead
    {
        //распределить fn_distrib_xx для записей в fn_distrib_dom_log
        //-----------------------------------------------------------------------------
        /*public void DistribPaXX_2(IDbConnection conn_db, out Returns ret, DbCalcQueueClient.SetTaskProgressDelegate setTaskProgress)
        //-----------------------------------------------------------------------------
        {
            ret = Utils.InitReturns();

            int yy = Points.DateOper.Year;
            int mm = Points.DateOper.Month;
            CalcTypes.ParamCalc paramcalc = new CalcTypes.ParamCalc(-1, -1, "", yy, mm, yy, mm);

           //IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
           //ret = OpenDb(conn_db, true);
           //if (!ret.result)
           //{
           //    return;
           //}
            ExecSQL(conn_db, " Drop table t_selkvar ", false);

#if PG
            string fn_operday_dom_mc = Points.Pref + "_fin_" + (paramcalc.calc_yy - 2000).ToString("00") + ".fn_operday_dom_mc";
#else
            string fn_operday_dom_mc = Points.Pref + "_fin_" + (paramcalc.calc_yy - 2000).ToString("00") + paramcalc.ol_srv + ":fn_operday_dom_mc";
#endif

            string sql_text = "select MIN(date_oper) as dat from " + fn_operday_dom_mc;
            DataTable dt = ClassDBUtils.OpenSQL(sql_text, conn_db).GetData();

            CalcTypes.PackXX fn_packXX = new CalcTypes.PackXX(paramcalc, 0, false);

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
                string fn_distrib = Points.Pref + "_fin_" + (paramcalc.calc_yy - 2000).ToString("00") + ".fn_distrib_dom_" + (Convert.ToDateTime(row["dat"]).Month).ToString("00");
#else
                string fn_distrib = Points.Pref + "_fin_" + (paramcalc.calc_yy - 2000).ToString("00") + paramcalc.ol_srv + ":fn_distrib_dom_" + (Convert.ToDateTime(row["dat"]).Month).ToString("00");
                    //ToString("00");
#endif

                sqlText = " Delete From " + fn_packXX.fn_distrib + " Where dat_oper >= '" + min_date + "'" + sConvToDate +
 " and nzp_dom in (select nzp_dom from " + fn_operday_dom_mc + ") ";
                ret = ExecSQL(conn_db, sqlText, true);

            }
            else
            {
                ret.result = false;
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
                return;
            }



            int j = 0;
            sql_text = "Insert into t_selkvar (nzp_dom) select distinct nzp_dom from  " + fn_operday_dom_mc;
            ret = ExecSQL(conn_db, sql_text, true);

            sql_text = "create " + sCrtTempTable + " table t_dom (nzp_dom integer) " + sUnlogTempTable;
            ret = ExecSQL(conn_db, sql_text, false);

            sql_text = "insert into t_dom (nzp_dom) select distinct nzp_dom from t_selkvar";
            ret = ExecSQL(conn_db, sql_text, false);


            while (dat <= lastDayDatCalc)
            {
                paramcalc.DateOper = dat;

                DistribPaXX(conn_db, paramcalc, out ret, setTaskProgress, 1, ref j);
                if (!ret.result)
                {
                    DropTempTablesPack(conn_db);
                    return;
                }
                dat = dat.AddDays(1);
            }
            sql_text = "drop  table t_dom";
            ret = ExecSQL(conn_db, sql_text, false);

            sql_text = "drop  table t_selkvar";
            ret = ExecSQL(conn_db, sql_text, false);

            sql_text = "delete from  t_selkvar";
            ret = ExecSQL(conn_db, sql_text, false);
        }*/

        //-----------------------------------------------------------------------------
        /*void DistribPaXX(IDbConnection conn_db, CalcTypes.ParamCalc paramcalc, out Returns ret, DbCalcQueueClient.SetTaskProgressDelegate setTaskProgress, int pCountIteration, ref int pCurrIteration)
        //-----------------------------------------------------------------------------
        {
            //sCrtTempTable = "";
            //sUnlogTempTable = "";


            MonitorLog.WriteLog("Расчёт сальдо по перечислениям за " + paramcalc.dat_oper, MonitorLog.typelog.Info, 1, 222, true);

            string sql_text;
            ret = Utils.InitReturns();
            Returns r = Utils.InitReturns();

            //return;   
            CalcTypes.PackXX fn_packXX = new CalcTypes.PackXX(paramcalc, 0, false);

#if PG
            ret = ExecSQL(conn_db, " set search_path to '" + Points.Pref + "_kernel '", true);
#endif


            fn_packXX.is_local = false;
            fn_packXX.paramcalc.pref = Points.Pref;

            //sql_text="drop table t_dom ";
            //ret = ExecSQL(conn_db, sql_text, false);

            //sql_text="create "+sCrtTempTable+" table t_dom (nzp_dom integer) "+sUnlogTempTable;
            //ret = ExecSQL(conn_db, sql_text, false);

            //sql_text="insert into t_dom (nzp_dom) select distinct nzp_dom from t_selkvar";
            //ret = ExecSQL(conn_db, sql_text, false);          

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

            if (setTaskProgress != null)
            {
                pCurrIteration++;
                setTaskProgress(((decimal)pCurrIteration) / pCountIteration);
            }



            CreatePaXX(conn_db, fn_packXX, false, out ret);
            if (!ret.result)
            {
                return;
            }


            //MessageInPackLog(conn_db, fn_packXX, "Начало расчета сальдо по перечислениям", false, out r);
            MonitorLog.WriteLog(".....Начало расчета сальдо по перечислениям", MonitorLog.typelog.Info, 1, 222, true);

            string sqlText;

            sqlText = " Delete From " + fn_packXX.fn_pa_xx + " Where dat_oper >= " + fn_packXX.paramcalc.dat_oper + sConvToDate;

            if ((paramcalc.nzp_pack != 0) || (paramcalc.nzp_dom != 0))
            {
                sqlText = sqlText + " and nzp_dom in (select nzp_dom from t_dom)";
            }

            ret = ExecSQL(conn_db,
                sqlText, true);
            if (!ret.result)
            {
                //MessageInPackLog(conn_db, fn_packXX, "Расчёт сальдо перечисления невозможен. Обнаружена взаимоблокировка при очистке таблицы "+fn_packXX.fn_pa_xx, false, out r);
                MonitorLog.WriteLog(".....Расчёт сальдо перечисления невозможен. Обнаружена взаимоблокировка при очистке таблицы", MonitorLog.typelog.Info, 1, 222, true);
                return;
            }

            //MessageInPackLog(conn_db, fn_packXX, "Очистка предыдущего распределения начата...", false, out r);
            MonitorLog.WriteLog(".....Очистка предыдущего распределения начата", MonitorLog.typelog.Info, 1, 222, true);

            sqlText = " Delete From " + fn_packXX.fn_distrib + " Where dat_oper >= " + fn_packXX.paramcalc.dat_oper + sConvToDate;
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

            ret = ExecSQL(conn_db, sqlText, true);
            if (!ret.result)
            {
                return;
            }

            if (setTaskProgress != null)
            {
                pCurrIteration++;
                setTaskProgress(((decimal)pCurrIteration) / pCountIteration);
            }
            //MessageInPackLog(conn_db, fn_packXX, "Очистка предыдущего распределения завершена", false, out r);
            MonitorLog.WriteLog(".....Очистка предыдущего распределения завершена", MonitorLog.typelog.Info, 1, 222, true);
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

                    CalcTypes.PackXX local_packXX = new CalcTypes.PackXX(paramcalc, 0, false);
                    MessageInPackLog(conn_db, fn_packXX, "Загрузка оплат из банка данных " + fn_packXX, false, out r);
                    LoadLocalPaXX(conn_db, local_packXX, true, out ret);
                    if (!ret.result)
                    {
                        return;
                    }
                }
            }

            if (setTaskProgress != null)
            {
                pCurrIteration++;
                setTaskProgress(((decimal)pCurrIteration) / pCountIteration);
            }

            // Сохранить запись в журнал событий
            int nzp_event = SaveEvent(7427, conn_db, paramcalc.nzp_user, paramcalc.nzp_pack, "Операционный день " + fn_packXX.paramcalc.dat_oper);
            if ((nzp_event > 0) && (isDebug))
            {
                ret = ExecSQL(conn_db, "insert into " + Points.Pref + "_data" + tableDelimiter + "sys_event_detail(nzp_event, table_, nzp) select " + nzp_event + ",'" + fn_packXX.pack_ls + "',nzp_pack_ls from t_selkvar", true);
            }

            if (!isAutoDistribPaXX)
            {
                ret = ExecSQL(conn_db, " delete from  " + fn_packXX.fn_operday_log + " where date_oper =" + paramcalc.dat_oper + sConvToDate + " and nzp_dom in (select  nzp_dom from t_dom) ", true);
            }


            //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
            //расчет вход. сальдо
            //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
            ExecSQL(conn_db, " Drop table ttt_dis_prev ", false);

            DateTime d = fn_packXX.paramcalc.DateOper.AddDays(-1);

            sql_text = " Create " + sCrtTempTable + " table ttt_dis_prev (nzp_supp integer,nzp_payer integer, nzp_area integer, nzp_dom integer, nzp_serv integer, nzp_bank integer,  sum_in " + sDecimalType + "(14,2) default 0.00) " + sUnlogTempTable;

            ret = ExecSQL(conn_db, sql_text, true);


            sqlText = " insert Into  ttt_dis_prev (nzp_supp ,nzp_payer, nzp_area, nzp_dom,nzp_serv, nzp_bank,  sum_in) " +
                    " Select a.nzp_supp, a.nzp_payer, a.nzp_area, a.nzp_dom,a.nzp_serv, a.nzp_bank, sum(a.sum_out) as sum_in " +
                    " From " + fn_packXX.fn_distrib_prev + " a,  t_dom d " +
                    " Where a.dat_oper = '" + d.ToShortDateString() + "' " + sConvToDate + " and a.nzp_dom = d.nzp_dom " +
                    " Group by a.nzp_supp, a.nzp_payer, a.nzp_area, a.nzp_dom,a.nzp_serv, a.nzp_bank " +
                    " ";


            ret = ExecSQL(conn_db, sqlText, true);
            if (!ret.result)
            {
                return;
            }
            if (isUseSupplier)
            {

                sql_text =
                        " Insert into " + fn_packXX.fn_distrib +
                        "  ( nzp_supp,nzp_payer, nzp_area, nzp_dom,nzp_serv, nzp_bank, dat_oper, sum_in ) " +
                        " Select nzp_supp,nzp_payer, 0, nzp_dom,nzp_serv, nzp_bank, " + fn_packXX.paramcalc.dat_oper + sConvToDate + ", sum_in " +
                        " From ttt_dis_prev ";
            }
            else
            {
                sql_text =
                        " Insert into " + fn_packXX.fn_distrib +
                        "  ( nzp_supp,nzp_payer, nzp_area, nzp_dom,nzp_serv, nzp_bank, dat_oper, sum_in ) " +
                        " Select nzp_supp,nzp_payer, nzp_area, nzp_dom,nzp_serv, nzp_bank, " + fn_packXX.paramcalc.dat_oper + sConvToDate + ", sum_in " +
                        " From ttt_dis_prev ";

            }
            ret = ExecSQL(conn_db, sql_text, true);
            ret = ExecSQL(conn_db, " Drop table ttt_dis_prev ", false);
            if (!ret.result)
            {
                return;
            }
            if (setTaskProgress != null)
            {
                pCurrIteration++;
                setTaskProgress(((decimal)pCurrIteration) / pCountIteration);
            }
            //MessageInPackLog(conn_db, fn_packXX, "Расчёт входящего сальдо завершено" , false, out r);
            MonitorLog.WriteLog(".....Расчёт входящего сальдо завершен", MonitorLog.typelog.Info, 1, 222, true);
            //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
            //учет fn_pa_xx
            //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
            ExecSQL(conn_db, " Drop table ttt_paxx ", false);

            ret = ExecSQL(conn_db, " Create " + sCrtTempTable + " table ttt_paxx (" +
                                   " nzp_supp integer default 0 ," +
                                   " nzp_payer integer, " +
                                   " nzp_area integer, " +
                                   " nzp_dom integer, " +
                                   " nzp_serv integer," +
                                   " nzp_bank integer, " +
                                   " kod integer, " +
                                   " sum_prih " + sDecimalType + "(14,2) " +
                                   " default 0.00) " + sUnlogTempTable, true);
            if (isUseSupplier)
            {
                sqlText = "insert into ttt_paxx ( nzp_supp, nzp_payer, nzp_area, nzp_dom, nzp_serv, nzp_bank,  kod,  sum_prih) " +
                    " Select a.nzp_supp, b.nzp_payer_princip as nzp_payer, 0  nzp_area, a.nzp_dom, a.nzp_serv, a.nzp_bank,  1 as kod, sum(sum_prih) as sum_prih " +
                    " From " + fn_packXX.fn_pa_xx + " a,  t_dom d, " + Points.Pref + "_kernel" + tableDelimiter + "supplier" + " b " +
                    " Where  a.nzp_dom = d.nzp_dom " +
                    "   and a.dat_oper = " + fn_packXX.paramcalc.dat_oper + sConvToDate + " and a.nzp_supp = b.nzp_supp and  b.nzp_payer_princip is not null " +
                    " Group by 1,2,3,4,5,6 ";
            }
            else
            {

                sqlText = "insert into ttt_paxx ( nzp_payer, nzp_area, nzp_dom, nzp_serv, nzp_bank, nzp_supp, kod,  sum_prih) " +
                         " Select b.nzp_payer, a.nzp_area, a.nzp_dom, a.nzp_serv, a.nzp_bank, a.nzp_supp, 1 as kod, sum(sum_prih) as sum_prih " +
                         " From " + fn_packXX.fn_pa_xx + " a, " + fn_packXX.s_payer + " b, t_dom d " +
                         " Where a.nzp_supp = b.nzp_supp and a.nzp_dom = d.nzp_dom " +
                         "   and dat_oper = " + fn_packXX.paramcalc.dat_oper + sConvToDate +
                         " Group by 1,2,3,4,5,6,7 ";

            }

            ret = ExecSQL(conn_db, sqlText, true);

            if (!ret.result)
            {
                return;
            }
            // MessageInPackLog(conn_db, fn_packXX, "Учёт распределения завершен", false, out r);
            MonitorLog.WriteLog(".....Учёт распределения завершен", MonitorLog.typelog.Info, 1, 222, true);
            //ExecSQL(conn_db, " Create unique index ix1_ttt_paxx on ttt_paxx (nzp_payer, nzp_area, nzp_dom, nzp_serv, nzp_bank, nzp_supp) ", true);            
            ExecSQL(conn_db, " Create  index ix1_ttt_paxx on ttt_paxx (nzp_supp, " +
                             " nzp_payer, nzp_area, nzp_dom, nzp_serv, nzp_bank) ", true);


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
                return;
            }

            sqlText = " insert into t_helpsdistrib(nzp_supp, nzp_payer, nzp_area, nzp_dom, nzp_serv, nzp_bank)" +
                      " select nzp_supp, nzp_payer, nzp_area, nzp_dom, nzp_serv, nzp_bank " +
                      " From " + fn_packXX.fn_distrib + " a " +
                      " Where dat_oper = " + fn_packXX.paramcalc.dat_oper + sConvToDate;
            ret = ExecSQL(conn_db, sqlText, true);

            if (!ret.result)
            {
                return;
            }

            ExecSQL(conn_db, " Create  index ix1_t_helpsdistrib on t_helpsdistrib (nzp_supp, " +
                 " nzp_payer, nzp_area, nzp_dom, nzp_serv, nzp_bank) ", true);

            ExecSQL(conn_db, DBManager.sUpdStat + "  t_helpsdistrib", true);
            // Messa

            ret = ExecSQL(conn_db,
                " Update ttt_paxx " +
                " Set kod = 0 " +
                " Where exists ( Select 1 From t_helpsdistrib a " +
                            " Where  " +
                            "       a.nzp_supp= ttt_paxx.nzp_supp " +
                            "   and a.nzp_payer= ttt_paxx.nzp_payer " +
                            "   and a.nzp_area = ttt_paxx.nzp_area " +
                            "   and a.nzp_dom = ttt_paxx.nzp_dom " +
                            "   and a.nzp_serv = ttt_paxx.nzp_serv " +
                            "   and a.nzp_bank = ttt_paxx.nzp_bank " +
                            " ) "
                , true);
            if (!ret.result)
            {
                return;
            }
            ExecSQL(conn_db, "drop table t_helpsdistrib", true);

            ExecSQL(conn_db, " Create index ix2_ttt_paxx on ttt_paxx (kod) ", true);
            if (setTaskProgress != null)
            {
                pCurrIteration++;
                setTaskProgress(((decimal)pCurrIteration) / pCountIteration);
            }

            ExecSQL(conn_db, DBManager.sUpdStat + " ttt_paxx ", true);

            ret = ExecSQL(conn_db,
                " Insert into " + fn_packXX.fn_distrib + " ( nzp_supp,nzp_payer,nzp_area, nzp_dom,nzp_serv,nzp_bank,dat_oper ) " +
                " Select nzp_supp,nzp_payer,nzp_area, nzp_dom, nzp_serv,nzp_bank, " + fn_packXX.paramcalc.dat_oper + sConvToDate +
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
                            "   and a.nzp_supp = " + fn_packXX.fn_distrib + ".nzp_supp " +
                            "   and a.nzp_area = " + fn_packXX.fn_distrib + ".nzp_area " +
                            "   and a.nzp_dom = " + fn_packXX.fn_distrib + ".nzp_dom " +
                            "   and a.nzp_serv = " + fn_packXX.fn_distrib + ".nzp_serv " +
                            "   and a.nzp_bank = " + fn_packXX.fn_distrib + ".nzp_bank " +
                            " ) " +
                " Where dat_oper = " + fn_packXX.paramcalc.dat_oper + sConvToDate + " and nzp_dom in (select  nzp_dom from t_dom) " +
                "   and 0 < ( Select count(*) From ttt_paxx a " +
                            " Where a.nzp_payer= " + fn_packXX.fn_distrib + ".nzp_payer " +
                            "   and a.nzp_supp = " + fn_packXX.fn_distrib + ".nzp_supp " +
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
            if (setTaskProgress != null)
            {
                pCurrIteration++;
                setTaskProgress(((decimal)pCurrIteration) / pCountIteration);
            }
            //MessageInPackLog(conn_db, fn_packXX, "Учёт оплат завершен", false, out r);
            MonitorLog.WriteLog(".....Учёт оплат завершен", MonitorLog.typelog.Info, 1, 222, true);
            //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
            //проверить, что все распределенные оплаты учтены!

            //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
            //учет процентов удержаний
            //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
            DateTime dat_oper;
            string str = fn_packXX.paramcalc.dat_oper.Trim().Replace("'", "");
            //bool res = DateTime.ParseExact(str, "dd.MM.yyyy", new CultureInfo("ru-RU"), DateTimeStyles.None, out dat_oper);
            dat_oper = Convert.ToDateTime(str);

            CalcUderFndistrib(conn_db, dat_oper, 0, out ret);

            if (setTaskProgress != null)
            {
                pCurrIteration++;
                setTaskProgress(((decimal)pCurrIteration) / pCountIteration);
            }

            //MessageInPackLog(conn_db, fn_packXX, "Сохранение сумм перечисления завершено", false, out r);            
            //            MonitorLog.WriteLog(".....Сохранение сумм перечисления завершено", MonitorLog.typelog.Info, 1, 222, true);
            //ExecSQL(conn_db, " Drop table ttt_paxx ", false);

            //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
            //учет fn_reval
            //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
            IDbTransaction transactionID = conn_db.BeginTransaction();
            MonitorLog.WriteLog(".....Учёт перекидок начат", MonitorLog.typelog.Info, 1, 222, true);
            //MessageInPackLog(conn_db, fn_packXX, "Начало учёта перекидок", false, out r);            
            Update_reval_supp(conn_db, transactionID, fn_packXX, false, out ret);
            //ExecSQL(conn_db, " Create index ix2_ttt_paxx on ttt_paxx (kod) ", true);
            if (setTaskProgress != null)
            {
                pCurrIteration++;
                setTaskProgress(((decimal)pCurrIteration) / pCountIteration);
            }
            //MessageInPackLog(conn_db, fn_packXX, "Учёт перекидок завершен", false, out r);
            MonitorLog.WriteLog(".....Учёт перекидок завершен", MonitorLog.typelog.Info, 1, 222, true);
            transactionID.Commit();

            //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
            //учет fn_send
            //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~            
            transactionID = conn_db.BeginTransaction();
            MonitorLog.WriteLog(".....Учёт перечислений начат", MonitorLog.typelog.Info, 1, 222, true);
            Update_Send(conn_db, transactionID, fn_packXX, false, out ret);
            //MessageInPackLog(conn_db, fn_packXX, "Учёт перекидок завершен", false, out r);
            MonitorLog.WriteLog(".....Учёт перечислений завершен", MonitorLog.typelog.Info, 1, 222, true);
            transactionID.Commit();
            if (setTaskProgress != null)
            {
                pCurrIteration++;
                setTaskProgress(((decimal)pCurrIteration) / pCountIteration);
            }


            //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
            //расчет итогового сальдо
            //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
            ret = ExecSQL(conn_db,
                " Update " + fn_packXX.fn_distrib +
                " Set sum_out = sum_in + sum_rasp - sum_ud + sum_naud + sum_reval - sum_send " +
                "   ,sum_charge = sum_rasp - sum_ud + sum_naud + sum_reval " +
                " Where dat_oper = " + fn_packXX.paramcalc.dat_oper + sConvToDate + " and nzp_dom in (select  nzp_dom from t_dom) "
                , true);
            if (!ret.result)
            {
                return;
            }
            if (setTaskProgress != null)
            {
                pCurrIteration++;
                setTaskProgress(((decimal)pCurrIteration) / pCountIteration);
            }
            //MessageInPackLog(conn_db, fn_packXX, "Расчёт итогового сальдо завершено", false, out r);
            MonitorLog.WriteLog(".....Расчёт итогового сальдо завершено за " + paramcalc.dat_oper, MonitorLog.typelog.Info, 1, 222, true);

            //if (ret.result)
            //{
            //    ret = ExecSQL(conn_db,
            //        " Delete From " + fn_packXX.fn_operday_log +
            //        " Where date_oper = " + fn_packXX.paramcalc.dat_oper
            //        , true);
            //}

            //sql_text = "drop  table t_dom";
            //ret = ExecSQL(conn_db, sql_text, false);

            //sql_text = "drop  table t_selkvar";
            //ret = ExecSQL(conn_db, sql_text, false);

            MessageInPackLog(conn_db, fn_packXX, "Окончание расчета сальдо по перечислениям", false, out r);
            MonitorLog.WriteLog("Окончание расчёта сальдо перечисления за " + paramcalc.dat_oper, MonitorLog.typelog.Info, 1, 222, true);

        }*/

        /*public bool Update_Send(IDbConnection conn_db, IDbTransaction transactionID, CalcTypes.PackXX fn_packXX, bool flgUpdateTotal, out Returns ret)
        {
            ret = Utils.InitReturns();

            ExecSQL(conn_db, " Drop table ttt_paxx ", false);

#if PG
            ret = ExecSQL(conn_db,
                " Update " + fn_packXX.fn_distrib + " set sum_send = 0 " +
               " Where dat_oper = " + fn_packXX.paramcalc.dat_oper, true);
            if (!ret.result)
            {
                return false;
            }

            ret = ExecSQL(conn_db,
                          " Select nzp_supp,nzp_payer, nzp_dom,nzp_serv, -1 as nzp_bank, 1 as kod, sum(sum_send) as sum_send " +
                           " Into temp ttt_paxx  " +
                          " From " + fn_packXX.fn_sended +
                          " Where dat_oper = " + fn_packXX.paramcalc.dat_oper +
                          " Group by 1,2,3,4,5 ", true);
#else
  ret = ExecSQL(conn_db,
                " Select  nzp_supp,nzp_payer, nzp_dom,nzp_serv, -1 as nzp_bank, 1 as kod, sum(sum_send) as sum_send " +
                " From " + fn_packXX.fn_sended +
                " Where dat_oper = " + fn_packXX.paramcalc.dat_oper +sConvToDate+ " and nzp_dom in (select  nzp_dom from t_dom) "+
                " Group by 1,2,3,4,5 " +
                " Into temp ttt_paxx With no log "
                , true);
#endif
            if (!ret.result)
            {
                return false;
            }

            //ExecSQL(conn_db, " Create unique index ix1_ttt_paxx on ttt_paxx (nzp_payer, nzp_area, nzp_dom,nzp_serv, nzp_bank) ", true);
            ExecSQL(conn_db, " Create index ix1_ttt_paxx on ttt_paxx (nzp_payer, nzp_supp, nzp_dom,nzp_serv, nzp_bank) ", true);
            ExecSQL(conn_db, DBManager.sUpdStat + "  ttt_paxx ", true);


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

            sqlText = " insert into t_helpsdistrib(nzp_supp, nzp_payer,  nzp_dom, nzp_serv, nzp_bank)" +
                      " select distinct nzp_supp, nzp_payer,  nzp_dom, nzp_serv, nzp_bank " +
                      " From " + fn_packXX.fn_distrib + " a " +
                      " Where dat_oper = " + fn_packXX.paramcalc.dat_oper + sConvToDate;
            ret = ExecSQL(conn_db, sqlText, true);

            if (!ret.result)
            {
                return false;
            }

            ExecSQL(conn_db, " Create  index ix1_t_helpsdistrib on t_helpsdistrib (nzp_supp, " +
                 " nzp_payer,  nzp_dom, nzp_serv, nzp_bank) ", true);

            ExecSQL(conn_db, DBManager.sUpdStat + "  t_helpsdistrib", true);



            ret = ExecSQL(conn_db,
                " Update ttt_paxx " +
                " Set kod = 0 " +
                " Where 0 < ( Select count(*) From t_helpsdistrib a " +
                            " Where a.nzp_payer= ttt_paxx.nzp_payer " +
                            "   and a.nzp_supp = ttt_paxx.nzp_supp " +
                //"   and a.nzp_area = ttt_paxx.nzp_area " +
                            "   and a.nzp_dom = ttt_paxx.nzp_dom " +
                            "   and a.nzp_serv = ttt_paxx.nzp_serv " +
                            "   and a.nzp_bank = ttt_paxx.nzp_bank " +
                            " ) "
                , true);
            if (!ret.result)
            {
                return false;
            }
            ExecSQL(conn_db, "drop table t_helpsdistrib", true);
            ExecSQL(conn_db, DBManager.sUpdStat + " ttt_paxx ", true);

            ret = ExecSQL(conn_db,
                " Insert into " + fn_packXX.fn_distrib + " ( nzp_payer,nzp_supp, nzp_dom,nzp_serv,nzp_bank,dat_oper ) " +
                " Select nzp_payer,nzp_supp, nzp_dom, nzp_serv,nzp_bank, " + fn_packXX.paramcalc.dat_oper + sConvToDate +
                " From ttt_paxx Where kod = 1 "
                , true);
            if (!ret.result)
            {
                return false;
            }

            ret = ExecSQL(conn_db,
                " Update " + fn_packXX.fn_distrib +
                " Set sum_send = ( " +
                            " Select sum(sum_send) From ttt_paxx a " +
                            " Where a.nzp_payer= " + fn_packXX.fn_distrib + ".nzp_payer " +
                            "   and a.nzp_supp = " + fn_packXX.fn_distrib + ".nzp_supp " +
                //"   and a.nzp_area = " + fn_packXX.fn_distrib + ".nzp_area " +
                            "   and a.nzp_dom = " + fn_packXX.fn_distrib + ".nzp_dom " +
                            "   and a.nzp_serv = " + fn_packXX.fn_distrib + ".nzp_serv " +
                            "   and a.nzp_bank = " + fn_packXX.fn_distrib + ".nzp_bank " +
                            " ) " +
                " Where dat_oper = " + fn_packXX.paramcalc.dat_oper + sConvToDate +
                "   and 0 < ( Select count(*) From ttt_paxx a " +
                            " Where a.nzp_payer= " + fn_packXX.fn_distrib + ".nzp_payer " +
                            "   and a.nzp_supp = " + fn_packXX.fn_distrib + ".nzp_supp " +
                //"   and a.nzp_area = " + fn_packXX.fn_distrib + ".nzp_area " +
                            "   and a.nzp_dom = " + fn_packXX.fn_distrib + ".nzp_dom " +
                            "   and a.nzp_serv = " + fn_packXX.fn_distrib + ".nzp_serv " +
                            "   and a.nzp_bank = " + fn_packXX.fn_distrib + ".nzp_bank " +
                            " ) "
                , true);
            if (!ret.result)
            {
                return false;
            }


            return true;
        }*/

        //учет процентов удержаний
        //-----------------------------------------------------------------------------
        /*public void CalcUderFndistrib(IDbConnection conn_db, DateTime dat_oper, int ptype, out Returns ret)
        //-----------------------------------------------------------------------------
        {
            string TmpTablePref = "";
            if (isDebug)
            {
                TmpTablePref = Points.Pref + "_kernel:";
                sCrtTempTable = "";
                sUnlogTempTable = "";

                ret = ExecSQL(conn_db, "database " + Points.Pref + "_kernel", false);
            }

            MonitorLog.WriteLog(".....Расчёт комиссии за обслуживание за " + dat_oper, MonitorLog.typelog.Info, 1, 222, true);
            ret = Utils.InitReturns();
            string sql_text = "";
            string TablePercent;
            if (ptype == 0)
            {
                TablePercent = Points.Pref + "_data" + tableDelimiter + "fn_percent_dom";
            }
            else
            {
                TablePercent = Points.Pref + "_data" + tableDelimiter + "fn_percent";
            }



            IDataReader reader;

            int yy = dat_oper.Year;
            int mm = dat_oper.Month;

            string sDat_oper = "'" + dat_oper.ToShortDateString() + "'";

            CalcTypes.ParamCalc paramcalc = new CalcTypes.ParamCalc(-1, -1, "", yy, mm, yy, mm);
            CalcTypes.PackXX fn_packXX = new CalcTypes.PackXX(paramcalc, 0, false);


            //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
            //учет fn_pa_xx
            //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~


            sqlText = "select nzp_payer from ttt_paxx ";
            ret = ExecSQL(conn_db, sqlText, false);
            if (!ret.result)
            {

                sql_text = "drop  table t_dom ";
                ret = ExecSQL(conn_db, sql_text, false);

                sql_text = "create " + sCrtTempTable + " table t_dom (nzp_dom integer) " + sUnlogTempTable;
                ret = ExecSQL(conn_db, sql_text, false);

                sql_text = "insert into t_dom (nzp_dom) select distinct nzp_dom from " + fn_packXX.fn_pa_xx;
                ret = ExecSQL(conn_db, sql_text, false);

                MonitorLog.WriteLog("..........Очистка предыдущей комиссии за обслуживание начата", MonitorLog.typelog.Info, 1, 222, true);

                sql_text =
                    " Update " + fn_packXX.fn_distrib +
                    " Set sum_ud = 0  " +
                    " Where dat_oper = " + sDat_oper + sConvToDate + " and sum_ud <>0 ";// " and  nzp_dom in (select  nzp_dom from t_dom)";
                ret = ExecSQL(conn_db, sql_text, true);
                if (!ret.result)
                {
                    return;
                }

                sql_text =
                    " Update " + fn_packXX.fn_distrib +
                    " Set sum_naud = 0   " +
                    " Where dat_oper = " + sDat_oper + sConvToDate + " and sum_naud <>0 ";// " and  nzp_dom in (select  nzp_dom from t_dom)";
                ret = ExecSQL(conn_db, sql_text, true);
                if (!ret.result)
                {
                    return;
                }
                //очистить суммы в fn_perc и fn_naud
                sql_text = " delete from " + fn_packXX.fn_perc + " where dat_oper =  " + sDat_oper + " and nzp_dom in (select  nzp_dom from t_dom)  ";
                ret = ExecSQL(conn_db, sql_text, true);
                if (!ret.result)
                {
                    return;
                }


                //очистить суммы в fn_perc и fn_naud
                sql_text = " delete from " + fn_packXX.fn_naud + " where dat_oper =  " + sDat_oper + " and nzp_dom in (select  nzp_dom from t_dom)  ";
                ret = ExecSQL(conn_db, sql_text, true);
                if (!ret.result)
                {
                    return;
                }
                MonitorLog.WriteLog("..........Очистка предыдущей комиссии за обслуживание завершена", MonitorLog.typelog.Info, 1, 222, true);

                MonitorLog.WriteLog("..........Заполнение вспомогательных таблиц для расчёт комиссии начата", MonitorLog.typelog.Info, 1, 222, true);
                ExecSQL(conn_db, " Drop table ttt_perc_ud ", false);

                sql_text = " Create " + sCrtTempTable + " table ttt_paxx (nzp_payer integer, nzp_area integer, nzp_dom integer, nzp_serv integer, nzp_bank integer, kod integer, nzp_supp integer, sum_prih " + sDecimalType + "(14,2) default 0.00) " + sUnlogTempTable;
                if (isDebug)
                {

#if PG
                    sql_text = sql_text.Replace("temp table", "table");

#else
                        sql_text = sql_text.Replace("temp table", "table");
                        sql_text = sql_text.Replace("With no log", "");
                        sql_text = sql_text.Replace("with no log", "");
#endif
                }

                ret = ExecSQL(conn_db, sql_text, true);
                
                //sqlText = "insert into ttt_paxx ( nzp_payer, nzp_area, nzp_dom, nzp_serv, nzp_bank, nzp_supp, kod,  sum_prih) " +
                //    " Select b.nzp_payer, a.nzp_area, a.nzp_dom, a.nzp_serv, a.nzp_bank, a.nzp_supp, 1 as kod, sum(sum_prih) as sum_prih " +
                //    " From " + fn_packXX.fn_pa_xx + " a, " + fn_packXX.s_payer + " b, t_dom d " +
                //    " Where a.nzp_supp = b.nzp_supp and a.nzp_dom = d.nzp_dom " +
                //    "   and dat_oper = " + sDat_oper + sConvToDate +
                //    " Group by 1,2,3,4,5,6 ";
                //ret = ExecSQL(conn_db, sqlText, true);
                
                sqlText = "insert into ttt_paxx ( nzp_payer,  nzp_dom, nzp_serv, nzp_bank, nzp_supp, kod,  sum_prih) " +
                    " Select b.nzp_payer_princip,  a.nzp_dom, a.nzp_serv, a.nzp_bank, a.nzp_supp, 1 as kod, sum(sum_prih) as sum_prih " +
                    " From " + fn_packXX.fn_pa_xx + " a, " + Points.Pref + "_kernel" + tableDelimiter + "supplier b, t_dom d " +
                    " Where a.nzp_supp = b.nzp_supp and a.nzp_dom = d.nzp_dom " +
                    "   and dat_oper = " + sDat_oper + sConvToDate +
                    " Group by 1,2,3,4,5,6 ";
                ret = ExecSQL(conn_db, sqlText, true);

                sql_text = "select count(*) cnt from ttt_paxx  where sum_prih <>0 ";


                DataTable dt1 = ClassDBUtils.OpenSQL(sql_text, conn_db).GetData();
                DataRow row1;
                row1 = dt1.Rows[0];

                if (row1["cnt"] != null)
                {
                    if (Convert.ToInt32(row1["cnt"]) == 0)
                    {
                        ExecSQL(conn_db, " Drop table ttt_perc_ud ", false);
                        ExecSQL(conn_db, " Drop table ttt_paxx ", false);
                        //ExecSQL(conn_db, " Drop table t_dom ", true);

                        sql_text = " Update " + fn_packXX.fn_distrib +
                        " Set sum_out = sum_in + sum_rasp - sum_ud + sum_naud + sum_reval - sum_send " +
                        "   ,sum_charge = sum_rasp - sum_ud + sum_naud + sum_reval " +
                        " Where dat_oper = " + sDat_oper + " " + sConvToDate;
                        ret = ExecSQL(conn_db, sql_text, true);
                        return;
                    }
                }

                if (!ret.result)
                {
                    return;
                }

                ExecSQL(conn_db, " Create index ix1_ttt_paxx on ttt_paxx (nzp_payer, nzp_supp, nzp_dom, nzp_serv, nzp_bank) ", true);
                //ExecSQL(conn_db, " Create unique index ix1_ttt_paxx on ttt_paxx (nzp_payer, nzp_area, nzp_dom, nzp_serv, nzp_bank, nzp_supp) ", true);

                ExecSQL(conn_db, "drop table t_helpsdistrib", false);

                sqlText = "Create temp table t_helpsdistrib(" +
                          " nzp_payer integer," +
                          " nzp_dom integer," +
                          " nzp_serv integer," +
                          " nzp_bank integer)" + DBManager.sUnlogTempTable;
                ret = ExecSQL(conn_db, sqlText, true);

                if (!ret.result)
                {
                    return;
                }

                sqlText = " insert into t_helpsdistrib( nzp_payer,  nzp_dom, nzp_serv, nzp_bank)" +
                          " select  distinct nzp_payer,  nzp_dom, nzp_serv, nzp_bank " +
                          " From " + fn_packXX.fn_distrib + " a " +
                          " Where dat_oper = " + fn_packXX.paramcalc.dat_oper + sConvToDate;
                ret = ExecSQL(conn_db, sqlText, true);

                if (!ret.result)
                {
                    return;
                }

                ExecSQL(conn_db, " Create  index ix1_t_helpsdistrib on t_helpsdistrib ( " +
                     " nzp_payer,  nzp_dom, nzp_serv, nzp_bank) ", true);

                ExecSQL(conn_db, DBManager.sUpdStat + "  t_helpsdistrib", true);

                sql_text = " Update ttt_paxx " +
                    " Set kod = 0 " +
                    " Where 0 < ( Select count(*) From t_helpsdistrib a " +
                                " Where a.nzp_payer= ttt_paxx.nzp_payer " +
                    //"   and a.nzp_area = ttt_paxx.nzp_area " +
                                "   and a.nzp_dom = ttt_paxx.nzp_dom " +
                                "   and a.nzp_serv = ttt_paxx.nzp_serv " +
                                "   and a.nzp_bank = ttt_paxx.nzp_bank " +
                                " ) ";
                ret = ExecSQL(conn_db, sql_text, true);
                if (!ret.result)
                {
                    return;
                }
                ExecSQL(conn_db, "drop table t_helpsdistrib", true);

                ExecSQL(conn_db, " Create index ix2_ttt_paxx on ttt_paxx (kod) ", true);
#if PG
                ExecSQL(conn_db, " analyze ttt_paxx ", true);
#else
                ExecSQL(conn_db, " Update statistics for table ttt_paxx ", true);
#endif
            }
            //выбрать nzp_payer_naud,perc_ud из fn_percent, который удерживает проценты из суммы в ttt_paxx: [nzp_payer, nzp_area, nzp_serv, nzp_bank, nzp_supp]
            ExecSQL(conn_db, " Drop table ttt_perc_ud ", false);

#if PG
            sql_text =
                " Create temp table ttt_perc_ud " +
                " ( nzp_key   serial  not null, " +
                "   nzp_payer integer default 0 not null, " +
                //"   nzp_area  integer default 0 not null, " +
                "   nzp_dom   integer default 0 not null, " +
                "   nzp_serv  integer default 0 not null, " +
                // "   nzp_serv_naud  integer default 0 not null, " +
                "   nzp_bank  integer default 0 not null, " +
                "   nzp_supp  integer default 0 not null, " +
                "   sum_prih        numeric(14,2) default 0.00 not null, " +
                "   nzp_payer_naud  integer default 0 not null, " +
                "   sum_naud        numeric(14,2) default 0.00 not null, " +
                "   kod       integer default 1 not null, " +
                "   perc_ud   numeric(5,2) default 0.00 not null " +
                " )  ";
#else

            sql_text =
                " Create temp table ttt_perc_ud " +
                " ( nzp_key   serial  not null, " +
                "   nzp_payer integer default 0 not null, " +
                //"   nzp_area  integer default 0 not null, " +
                "   nzp_dom  integer default 0 not null, " +
                "   nzp_serv  integer default 0 not null, " +
                "   nzp_bank  integer default 0 not null, " +
                "   nzp_supp  integer default 0 not null, " +
                "   sum_prih        decimal(14,2) default 0.00 not null, " +
        //    "   nzp_serv_naud  integer default 0 not null, " +
                "   nzp_payer_naud  integer default 0 not null, " +
                "   sum_naud        decimal(14,2) default 0.00 not null, " +
                "   kod       integer default 1 not null, " +
                "   perc_ud   decimal(5,2) default 0.00 not null " +
                " ) With no log ";
#endif
            if (isDebug)
            {

#if PG
                sql_text = sql_text.Replace("temp table", "table");

#else
                    sql_text = sql_text.Replace("temp table", "table");
                    sql_text = sql_text.Replace("With no log", "");
                    sql_text = sql_text.Replace("with no log", "");
#endif
            }

            ret = ExecSQL(conn_db, sql_text, true);

            if (!ret.result)
            {
                return;
            }
            sql_text =
                " Insert into ttt_perc_ud ( nzp_payer,  nzp_dom,nzp_serv, nzp_bank, nzp_supp, sum_prih, nzp_payer_naud, sum_naud, kod, perc_ud ) " +
                " Select a.nzp_payer,  a.nzp_dom, a.nzp_serv, a.nzp_bank, a.nzp_supp, a.sum_prih, " +
                       " b.nzp_payer as nzp_payer_naud, a.sum_prih as sum_naud, 1 as kod, max(b.perc_ud) as perc_ud " +
                 " From ttt_paxx a, " + TablePercent + " b ," + Points.Pref + "_kernel" + tableDelimiter + "s_bank c " +
                " Where b.nzp_supp in (-1,a.nzp_supp) " +
                "   and b.nzp_serv_from in (-1,a.nzp_serv) " +

                //"   and b.nzp_area in (-1,a.nzp_area) " +
                "   and c.nzp_payer in (-1,a.nzp_bank) " +
                "   and b.nzp_bank in  (-1,c.nzp_bank)  " +
                "   and b.nzp_dom in  (-1,a.nzp_dom)   " +
                "   and " + sDat_oper + sConvToDate + " between b.dat_s  and b.dat_po " +
                //"   and b.dat_s  <= " + sDat_oper + sConvToDate +
                //"   and b.dat_po >= " + sDat_oper + sConvToDate +
                "   and perc_ud > 0.001 " +
                " and b.nzp_serv <=0 and b.nzp_supp_snyat <=0 " +
                " Group by 1,2,3,4,5,6,7,8,9 ";

            ret = ExecSQL(conn_db, sql_text, true);
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
                " Set sum_naud = sum_prih * perc_ud / 100.0 "
                , true);
            if (!ret.result)
            {
                MonitorLog.WriteLog(ret.text, MonitorLog.typelog.Error, 1, 222, true);
                return;
            }

            MonitorLog.WriteLog("..........Заполнение вспомогательных таблиц для расчёт комиссии завершена", MonitorLog.typelog.Info, 1, 222, true);

            //надо выровнить копейки при 100% удержании
            ExecSQL(conn_db, " Drop table ttt_perc_ud_2 ", false);


            MonitorLog.WriteLog("..........Расчёт простых правил удержания начато", MonitorLog.typelog.Info, 1, 222, true);
#if PG
            ret = ExecSQL(conn_db,
                          " Select nzp_payer, nzp_supp,nzp_serv, nzp_bank, max(sum_prih) as sum_prih, sum(sum_naud) as sum_naud, sum(perc_ud) as perc_ud " +
                            " Into temp ttt_perc_ud_2 " +
                          " From ttt_perc_ud " +
                          " Where abs(sum_naud) > 0.001 " +
                          " Group by 1,2,3,4 "

                          , true);
#else
            ret = ExecSQL(conn_db,
                          " Select nzp_payer, nzp_supp, nzp_serv, nzp_bank, max(sum_prih) as sum_prih, sum(sum_naud) as sum_naud, sum(perc_ud) as perc_ud " +
                          " From ttt_perc_ud " +
                          " Where abs(sum_naud) > 0.001 " +
                          " Group by 1,2,3,4 " +
                          " Into temp ttt_perc_ud_2 With no log "
                          , true);
#endif
            // ret = ExecSQL(conn_db, sql_text, true);               
            if (!ret.result)
            {
                MonitorLog.WriteLog(ret.text, MonitorLog.typelog.Error, 1, 222, true);
                return;
            }

            ret = ExecRead(conn_db, out reader,
                " Select *, sum_prih - sum_naud as sum_dlt " +
                " From ttt_perc_ud_2 " +
                " Where abs(100 - perc_ud) < 0.001 and abs(sum_prih - sum_naud) > 0.0001 "
                , true);
            if (!ret.result)
            {
                MonitorLog.WriteLog(ret.text, MonitorLog.typelog.Error, 1, 222, true);
                return;
            }
            try
            {
                while (reader.Read())
                {
                    int nzp_payer = (int)reader["nzp_payer"];
                    //int nzp_area = (int)reader["nzp_area"];
                    int nzp_serv = (int)reader["nzp_serv"];
                    int nzp_bank = (int)reader["nzp_bank"];
                    int nzp_supp = (int)reader["nzp_supp"];
                    decimal dlt = (decimal)reader["sum_dlt"];

                    IDataReader reader2;
                    ret = ExecRead(conn_db, out reader2,
                        " Select nzp_key From ttt_perc_ud " +
                        " Where nzp_payer = " + nzp_payer +
                        //"   and nzp_area = " + nzp_area +
                        "   and nzp_supp = " + nzp_supp +
                        "   and nzp_serv = " + nzp_serv +
                        "   and nzp_bank = " + nzp_bank +
                        " Order by sum_naud desc "
                        , true);
                    if (!ret.result)
                    {
                        MonitorLog.WriteLog(ret.text, MonitorLog.typelog.Error, 1, 222, true);
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
                            MonitorLog.WriteLog(ret.text, MonitorLog.typelog.Error, 1, 222, true);
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
            MonitorLog.WriteLog("..........Расчёт простых правил удержания завершено", MonitorLog.typelog.Info, 1, 222, true);

            MonitorLog.WriteLog("..........Расчёт зависимых правил удержания начато", MonitorLog.typelog.Info, 1, 222, true);
            #region Рассчитать проценты удержания
            sql_text = "select * from  " + TablePercent + " where nzp_serv >0 or nzp_supp_snyat>0 and " + sDat_oper + sConvToDate + " between dat_s and dat_po";
            ret = ExecRead(conn_db, out reader, sql_text, true);
            if (!ret.result)
            {
                return;
            }
            try
            {
                while (reader.Read())
                {
                    int nzp_payer_to = (int)reader["nzp_payer"];
                    //int nzp_area = (int)reader["nzp_area"];
                    int nzp_serv_from = (int)reader["nzp_serv_from"];
                    int nzp_bank = (int)reader["nzp_bank"];
                    int nzp_serv = (int)reader["nzp_serv"];
                    int nzp_supp = (int)reader["nzp_supp"];

                    //int nzp_serv_snyat = 0;
                    //if (reader["nzp_serv_snyat"]!= DBNull.Value) nzp_serv_snyat = (int)reader["nzp_serv_snyat"];
                    int nzp_supp_snyat = 0;
                    if (reader["nzp_supp_snyat"] != DBNull.Value) nzp_supp_snyat = (int)reader["nzp_supp_snyat"];

                    decimal perc_ud = (decimal)reader["perc_ud"];


                    IDataReader reader2;
                    //sql_text = "select p.nzp_payer_principal as nzp_payer, s.nzp_supp from " + Points.Pref + "_data" + tableDelimiter + "supplier s, " + Points.Pref + "_kernel" + tableDelimiter + "s_payer p where   s.nzp_supp  = " + nzp_supp + "  ";
                    sql_text = "select s.nzp_payer_princip as nzp_payer, s.nzp_supp as nzp_supp from " + Points.Pref + "_kernel" + tableDelimiter + "supplier s where   s.nzp_supp  = " + nzp_supp + "  ";
                    ret = ExecRead(conn_db, out reader2, sql_text, true);
                    if (!ret.result)
                    {
                        MonitorLog.WriteLog(ret.text, MonitorLog.typelog.Error, 1, 222, true);
                        return;
                    }
                    reader2.Read();
                    int nzp_payer_from = (int)reader2["nzp_payer"]; // принципал
                    int nzp_supp_from = (int)reader2["nzp_supp"];
                    reader2.Close();

                    if ((nzp_payer_from > 0) & (nzp_supp > 0))
                    {
                        sql_text = " drop table t_ud_from_uk2 ";
                        ret = ExecSQL(conn_db, sql_text, false);

                        sql_text = "create " + sCrtTempTable + " table t_ud_from_uk2 (nzp_supp integer, nzp_payer integer, nzp_dom integer, nzp_serv integer, sum_prih " + DBManager.sDecimalType + "(20,5) default 0 ,sum_ud " + DBManager.sDecimalType + "(20,5) default 0, nzp_bank integer) " + sUnlogTempTable;
                        ret = ExecSQL(conn_db, sql_text, true);
                        if (!ret.result)
                        {
                            MonitorLog.WriteLog(ret.text, MonitorLog.typelog.Error, 1, 222, true);
                            return;
                        }

                        sql_text = " insert into t_ud_from_uk2(nzp_supp , nzp_payer, nzp_dom, nzp_serv, nzp_bank,  sum_prih) " +
                        " Select nzp_supp, nzp_payer, nzp_dom, nzp_serv,nzp_bank,sum(sum_rasp) sum_prih From  " + fn_packXX.fn_distrib +
                        " Where  nzp_supp in (select s.nzp_supp from " + Points.Pref + "_kernel" + tableDelimiter + "supplier s where s.nzp_payer_princip = " + nzp_payer_from + ") and dat_oper = " + sDat_oper + sConvToDate +
                        " and nzp_dom in (select nzp_dom from t_dom) ";

                        if (nzp_serv_from > 0)
                        {
                            sql_text = sql_text + "   and nzp_serv = " + nzp_serv_from;
                        }
                        if (nzp_bank > 0)
                        {
                            sql_text = sql_text + "   and nzp_bank = " + nzp_bank;
                        }
                        //nzp_dom
                        sql_text = sql_text + " group by 1,2,3,4,5 having sum(sum_rasp) >0 ";
                        ret = ExecSQL(conn_db, sql_text, true);
                        if (!ret.result)
                        {
                            MonitorLog.WriteLog(ret.text, MonitorLog.typelog.Error, 1, 222, true);
                            return;
                        }
                        sql_text = " update  t_ud_from_uk2 set sum_ud = sum_prih * (" + perc_ud + "/100.0) ";
                        ret = ExecSQL(conn_db, sql_text, true);
                        if (!ret.result)
                        {
                            MonitorLog.WriteLog(ret.text, MonitorLog.typelog.Error, 1, 222, true);
                            return;
                        }
                        string nzp_serv_naud = " nzp_serv, ";
                        if (nzp_serv > 0) nzp_serv_naud = nzp_serv + ",";

                        if (nzp_supp_snyat > 0)
                        {
                            sql_text = " drop table t_ud_from_uk3 ";
                            ret = ExecSQL(conn_db, sql_text, false);

                            sql_text = "create " + sCrtTempTable + " table t_ud_from_uk3 (id serial not null, nzp_supp integer, nzp_payer integer, nzp_dom integer, nzp_serv integer, sum_prih " + DBManager.sDecimalType + "(20,5) default 0 ,sum_ud " + DBManager.sDecimalType + "(20,2) default 0, nzp_bank integer," +
                               " sum_ud_itogo " + DBManager.sDecimalType + "(20,2) default 0, sum_itogo " + DBManager.sDecimalType + "(20,5) default 0) " + sUnlogTempTable;
                            ret = ExecSQL(conn_db, sql_text, true);
                            if (!ret.result)
                            {
                                MonitorLog.WriteLog(ret.text, MonitorLog.typelog.Error, 1, 222, true);
                                return;
                            }

                            sql_text = " insert into t_ud_from_uk3(nzp_supp , nzp_payer, nzp_dom, nzp_serv, nzp_bank,  sum_prih) " +
                            " Select nzp_supp, nzp_payer, nzp_dom, nzp_serv,nzp_bank,sum(sum_rasp) sum_prih From  " + fn_packXX.fn_distrib +
                            " Where  nzp_supp =" + nzp_supp_snyat + " and dat_oper = " + sDat_oper + sConvToDate +
                            " and nzp_dom in (select nzp_dom from t_dom) ";

                            if (nzp_serv > 0)
                            {
                                sql_text += "   and nzp_serv = " + nzp_serv;
                            }
                            sql_text = sql_text + " group by 1,2,3,4,5 having sum(sum_rasp) >0 ";
                            ret = ExecSQL(conn_db, sql_text, true);
                            if (!ret.result)
                            {
                                MonitorLog.WriteLog(ret.text, MonitorLog.typelog.Error, 1, 222, true);
                                return;
                            }

                            sql_text = "update t_ud_from_uk3 set sum_itogo = (select sum(sum_prih) from t_ud_from_uk3), sum_ud_itogo = (select sum(sum_ud) from t_ud_from_uk2); ";
                            ret = ExecSQL(conn_db, sql_text, true);
                            if (!ret.result)
                            {
                                MonitorLog.WriteLog(ret.text, MonitorLog.typelog.Error, 1, 222, true);
                                return;
                            }

                            sql_text = "update t_ud_from_uk3 set sum_ud_itogo =  case when sum_ud_itogo > sum_itogo then sum_itogo else sum_ud_itogo end;";
                            ret = ExecSQL(conn_db, sql_text, true);
                            if (!ret.result)
                            {
                                MonitorLog.WriteLog(ret.text, MonitorLog.typelog.Error, 1, 222, true);
                                return;
                            }

                            sql_text = "delete from t_ud_from_uk3 where sum_itogo = 0;";
                            ret = ExecSQL(conn_db, sql_text, true);
                            if (!ret.result)
                            {
                                MonitorLog.WriteLog(ret.text, MonitorLog.typelog.Error, 1, 222, true);
                                return;
                            }

                            sql_text = "update t_ud_from_uk3 set sum_ud = sum_prih/sum_itogo*sum_ud_itogo;";
                            ret = ExecSQL(conn_db, sql_text, true);
                            if (!ret.result)
                            {
                                MonitorLog.WriteLog(ret.text, MonitorLog.typelog.Error, 1, 222, true);
                                return;
                            }

                            sql_text = "select sum(sum_ud) sum_ud_1, max(sum_ud_itogo) sum_ud_itogo from t_ud_from_uk3";
                            ret = ExecRead(conn_db, out reader2, sql_text, true);
                            if (!ret.result)
                            {
                                MonitorLog.WriteLog(ret.text, MonitorLog.typelog.Error, 1, 222, true);
                                return;
                            }
                            reader2.Read();
                            decimal sum_ud_1 = 0;
                            if (reader2["sum_ud_1"] != DBNull.Value) sum_ud_1 = Convert.ToDecimal(reader2["sum_ud_1"]);
                            decimal sum_ud_itogo = 0;
                            if (reader2["sum_ud_itogo"] != DBNull.Value) sum_ud_itogo = Convert.ToDecimal(reader2["sum_ud_itogo"]);
                            reader2.Close();

                            if (sum_ud_1 != sum_ud_itogo & sum_ud_itogo != 0)
                            {
                                decimal sumdlt = sum_ud_1 - sum_ud_itogo;
                                if (sumdlt != 0)
                                {
                                    sql_text = "select id, sum_ud from t_ud_from_uk3 order by sum_prih desc limit 1";
                                    ret = ExecRead(conn_db, out reader2, sql_text, true);
                                    if (!ret.result)
                                    {
                                        MonitorLog.WriteLog(ret.text, MonitorLog.typelog.Error, 1, 222, true);
                                        return;
                                    }
                                    reader2.Read();
                                    int id = Convert.ToInt32(reader2["id"]);
                                    decimal sum_ud = Convert.ToDecimal(reader2["sum_ud"]);
                                    reader2.Close();


                                    sql_text = "update t_ud_from_uk3 set sum_ud= sum_ud - " + sumdlt + " where id = " + id;
                                    ret = ExecSQL(conn_db, sql_text, true);
                                    if (!ret.result)
                                    {
                                        MonitorLog.WriteLog(ret.text, MonitorLog.typelog.Error, 1, 222, true);
                                        return;
                                    }
                                }
                            }

                            // Удержаннаые суммы
                            sql_text =
                                        " Insert into " + TmpTablePref + "ttt_perc_ud (nzp_payer,nzp_payer_naud,nzp_supp, nzp_serv, nzp_dom, nzp_bank, perc_ud, sum_prih,  sum_naud) " +
                                        " Select " + nzp_payer_from + "," + nzp_payer_to + ",nzp_supp, nzp_serv, " +
                                        " nzp_dom,nzp_bank, " + perc_ud + ", SUM(sum_prih),  SUM(sum_ud)  From t_ud_from_uk3  group by 1,2,3,4,5,6,7 having  SUM(sum_ud)>0";
                            ret = ExecSQL(conn_db, sql_text, true);
                            if (!ret.result)
                            {
                                MonitorLog.WriteLog(ret.text, MonitorLog.typelog.Error, 1, 222, true);
                                return;
                            };
                        }
                        else
                        {
                            // Удержаннаые суммы
                            sql_text = " Insert into  " + TmpTablePref + "ttt_perc_ud  (nzp_payer,nzp_payer_naud,nzp_supp, nzp_serv,  nzp_dom, nzp_bank, perc_ud, sum_prih,  sum_naud) " +
                                " Select   " + nzp_payer_from + "," + nzp_payer_to + "," + nzp_supp_from + "," + nzp_serv + "," + "nzp_dom,nzp_bank, " + perc_ud + ", SUM(sum_prih)" + ",  SUM(sum_ud) " +
                                " From t_ud_from_uk2" +
                                "  group by 1,2,3,4,5,6,7 having  SUM(sum_ud)>0";
                            ret = ExecSQL(conn_db, sql_text, true);
                            if (!ret.result)
                            {
                                MonitorLog.WriteLog(ret.text, MonitorLog.typelog.Error, 1, 222, true);
                                return;
                            };
                        }
                    }
                }
                reader.Close();

            }
            catch (Exception ex)
            {
                reader.Close();
                ret.text = ex.Message;
                MonitorLog.WriteLog(ret.text, MonitorLog.typelog.Error, 1, 222, true);
            }


            #endregion Рассчитать проценты удержания

            MonitorLog.WriteLog("..........Расчёт зависимых правил удержания завершено", MonitorLog.typelog.Info, 1, 222, true);

            MonitorLog.WriteLog("..........Сохранение комиссии начато", MonitorLog.typelog.Info, 1, 222, true);

            ret = ExecSQL(conn_db, "create index idx_ttt_perc_ud_1 on ttt_perc_ud(sum_naud)", true);
            if (!ret.result)
            {
                return;
            }
            //сохранить суммы в fn_perc и fn_naud
            sql_text = " Insert into " + fn_packXX.fn_perc + " (nzp_supp, nzp_payer, nzp_dom, nzp_serv,  nzp_geu, sum_prih, sum_perc, perc_ud, dat_oper, nzp_bank) " +
                " Select nzp_supp, nzp_payer_naud, nzp_dom,nzp_serv,  -1 nzp_geu, sum_prih, sum_naud, perc_ud, " + sDat_oper + sConvToDate + ", nzp_bank " +
                " From ttt_perc_ud " +
                " Where abs(sum_naud) > 0.001 ";
            ret = ExecSQL(conn_db, sql_text, true);
            if (!ret.result)
            {
                return;
            }

            //sql_text = " Insert into " + fn_packXX.fn_perc + " (nzp_supp, nzp_payer, nzp_dom, nzp_serv,  nzp_geu, sum_prih, sum_perc, perc_ud, dat_oper, nzp_bank) " +
            //    " Select nzp_supp, nzp_payer, nzp_dom,nzp_serv,  -1 nzp_geu, sum_prih, sum_naud, perc_ud, " + sDat_oper + sConvToDate + ", nzp_bank " +
            //    " From ttt_perc_ud " +
            //    " Where abs(sum_naud) > 0.001 ";
            //ret = ExecSQL(conn_db, sql_text, true);
            //if (!ret.result)
            //{
            //    return;
            //}

            //fn_naud: удержанные суммы
            sql_text = " Insert into " + fn_packXX.fn_naud + " (dat_oper, nzp_payer, nzp_dom, nzp_serv, nzp_payer_2, sum_prih, perc_ud, sum_ud, sum_naud,  nzp_supp, nzp_geu, nzp_bank) " +
                " Select " + sDat_oper + sConvToDate + ", nzp_payer, nzp_dom, nzp_serv, nzp_payer_naud, sum_prih, perc_ud, sum_naud, 0 as sum_naud, nzp_supp,0 nzp_geu, nzp_bank " +
                " From ttt_perc_ud " +
                " Where abs(sum_naud) > 0.001 ";
            ret = ExecSQL(conn_db, sql_text, true);
            if (!ret.result)
            {
                return;
            }
            //fn_naud: начисленные суммы
            sql_text = " Insert into " + fn_packXX.fn_naud + " (dat_oper, nzp_payer, nzp_dom,nzp_serv, nzp_payer_2, sum_prih, perc_ud, sum_ud, sum_naud, nzp_supp, nzp_geu, nzp_bank) " +
                " Select " + sDat_oper + sConvToDate + ", nzp_payer_naud, nzp_dom, nzp_serv, nzp_payer,sum_prih, perc_ud, 0 sum_naud, sum_naud, nzp_supp,0 nzp_geu, nzp_bank " +
                " From ttt_perc_ud " +
                " Where abs(sum_naud) > 0.001 ";
            ret = ExecSQL(conn_db, sql_text, true);
            if (!ret.result)
            {
                return;
            }

            ExecSQL(conn_db, " Create index ix1_ttt_pud on ttt_perc_ud (nzp_payer,nzp_supp,nzp_dom,nzp_serv,nzp_bank) ", true);
            ExecSQL(conn_db, " Create index ix2_ttt_pud on ttt_perc_ud (nzp_payer_naud,nzp_supp,nzp_dom,nzp_serv,nzp_bank) ", true);
            ExecSQL(conn_db, DBManager.sUpdStat + "  ttt_perc_ud ", true);

            ExecSQL(conn_db, " drop table t_helpnaud ", false);

            sql_text = "Create temp table t_helpnaud(" +
                       " nzp_payer integer," +
                       " nzp_supp integer," +
                       " nzp_dom integer," +
                       " nzp_serv integer," +
                       " nzp_bank integer)" + DBManager.sUnlogTempTable;
            ret = ExecSQL(conn_db, sql_text, true);
            if (!ret.result)
            {
                return;
            }

            sql_text = "insert into t_helpnaud(nzp_payer, nzp_supp, nzp_dom, nzp_serv, nzp_bank)" +
                       " select  nzp_payer, nzp_supp, nzp_dom, nzp_serv, nzp_bank" +
                       " from " + fn_packXX.fn_distrib +
                       " where dat_oper = " + sDat_oper + sConvToDate;
            ret = ExecSQL(conn_db, sql_text, true);
            if (!ret.result)
            {
                return;
            }

            ExecSQL(conn_db, " Create index ix2_t_helpnaud on t_helpnaud (nzp_payer,nzp_supp,nzp_dom,nzp_serv,nzp_bank) ", true);
            ExecSQL(conn_db, DBManager.sUpdStat + "  t_helpnaud ", true);

            //добавить отсутв.строки в fn_distrib
            sql_text = " Update ttt_perc_ud " +
                " Set kod = 0 " +
                " Where exists ( Select 1 From t_helpnaud a " +
                            " Where a.nzp_payer= ttt_perc_ud.nzp_payer_naud " +
                //"   and a.nzp_area = ttt_perc_ud.nzp_area " +
                            "   and a.nzp_supp = ttt_perc_ud.nzp_supp " +
                            "   and a.nzp_dom = ttt_perc_ud.nzp_dom " +
                            "   and a.nzp_serv = ttt_perc_ud.nzp_serv " +
                            "   and a.nzp_bank = ttt_perc_ud.nzp_bank " +
                            " ) ";
            ret = ExecSQL(conn_db, sql_text, true);
            if (!ret.result)
            {
                return;
            }

            ExecSQL(conn_db, " drop table t_helpnaud ", true);
            ExecSQL(conn_db, " Create index ix3_ttt_pud on ttt_perc_ud (kod) ", true);
            ExecSQL(conn_db, DBManager.sUpdStat + "  ttt_perc_ud ", true);

            sql_text =
                " Insert into " + fn_packXX.fn_distrib + " ( nzp_payer, nzp_supp,nzp_dom,nzp_serv,nzp_bank,dat_oper ) " +
#if PG
 " Select distinct nzp_payer_naud, nzp_supp,nzp_dom,nzp_serv,nzp_bank, to_date(" + sDat_oper + ", 'dd.mm.yyyy')" +
#else
 " Select unique nzp_payer_naud, nzp_supp,nzp_dom,nzp_serv,nzp_bank, " + sDat_oper +
#endif
 " From ttt_perc_ud Where kod = 1 ";

            ret = ExecSQL(conn_db, sql_text, true);
            if (!ret.result)
            {
                return;
            }

            //занести в fn_distrib_xx
            sql_text =
                " Update " + fn_packXX.fn_distrib +
                " Set sum_ud = ( " +
                            " Select sum(sum_naud) From ttt_perc_ud a " +
                            " Where a.nzp_payer= " + fn_packXX.fn_distrib + ".nzp_payer " +
                //"   and a.nzp_area = " + fn_packXX.fn_distrib + ".nzp_area " +
                            "   and a.nzp_supp = " + fn_packXX.fn_distrib + ".nzp_supp " +
                            "   and a.nzp_dom = " + fn_packXX.fn_distrib + ".nzp_dom " +
                            "   and a.nzp_serv = " + fn_packXX.fn_distrib + ".nzp_serv " +
                            "   and a.nzp_bank = " + fn_packXX.fn_distrib + ".nzp_bank " +
                            " ) " +
                " Where dat_oper = " + sDat_oper + sConvToDate + " and  nzp_dom in (select  nzp_dom from t_dom) " +
                "   and 0 < ( Select count(*) From ttt_perc_ud a " +
                            " Where a.nzp_payer= " + fn_packXX.fn_distrib + ".nzp_payer " +
                //"   and a.nzp_area = " + fn_packXX.fn_distrib + ".nzp_area " +
                            "   and a.nzp_supp = " + fn_packXX.fn_distrib + ".nzp_supp " +
                            "   and a.nzp_dom = " + fn_packXX.fn_distrib + ".nzp_dom " +
                            "   and a.nzp_serv = " + fn_packXX.fn_distrib + ".nzp_serv " +
                            "   and a.nzp_bank = " + fn_packXX.fn_distrib + ".nzp_bank " +
                            " ) ";
            ret = ExecSQL(conn_db, sql_text, true);
            if (!ret.result)
            {
                return;
            }

            sql_text =
                " Update " + fn_packXX.fn_distrib +
                " Set sum_naud = ( " +
                            " Select sum(sum_naud) From ttt_perc_ud a " +
                            " Where a.nzp_payer_naud= " + fn_packXX.fn_distrib + ".nzp_payer " +
                //"   and a.nzp_area = " + fn_packXX.fn_distrib + ".nzp_area " +
                            "   and a.nzp_supp = " + fn_packXX.fn_distrib + ".nzp_supp " +
                            "   and a.nzp_dom = " + fn_packXX.fn_distrib + ".nzp_dom " +
                            "   and a.nzp_serv = " + fn_packXX.fn_distrib + ".nzp_serv " +
                            "   and a.nzp_bank = " + fn_packXX.fn_distrib + ".nzp_bank " +
                            " ) " +
                " Where dat_oper = " + sDat_oper + sConvToDate + " and nzp_dom in (select  nzp_dom from t_dom) " +
                "   and 0 < ( Select count(*) From ttt_perc_ud a " +
                            " Where a.nzp_payer_naud= " + fn_packXX.fn_distrib + ".nzp_payer " +
                //"   and a.nzp_area = " + fn_packXX.fn_distrib + ".nzp_area " +
                            "   and a.nzp_supp = " + fn_packXX.fn_distrib + ".nzp_supp " +
                            "   and a.nzp_dom = " + fn_packXX.fn_distrib + ".nzp_dom " +
                            "   and a.nzp_serv = " + fn_packXX.fn_distrib + ".nzp_serv " +
                            "   and a.nzp_bank = " + fn_packXX.fn_distrib + ".nzp_bank " +
                            " ) ";

            ret = ExecSQL(conn_db, sql_text, true);
            if (!ret.result)
            {
                return;
            }

            ExecSQL(conn_db, " Drop table ttt_perc_ud ", true);
            ExecSQL(conn_db, " Drop table ttt_paxx ", true);
            //ExecSQL(conn_db, " Drop table t_dom ", true);
            MonitorLog.WriteLog("..........Обновление сальдо", MonitorLog.typelog.Info, 1, 222, true);
            sql_text = " Update " + fn_packXX.fn_distrib +
            " Set sum_out = sum_in + sum_rasp - sum_ud + sum_naud + sum_reval - sum_send " +
            "   ,sum_charge = sum_rasp - sum_ud + sum_naud + sum_reval " +
            " Where dat_oper = " + sDat_oper + " " + sConvToDate + " and nzp_dom in (select nzp_dom from t_dom) ";
            ret = ExecSQL(conn_db, sql_text, true);
            if (!ret.result)
            {
                return;
            }
            MonitorLog.WriteLog("..........Сохранение комиссии завершено", MonitorLog.typelog.Info, 1, 222, true);
        }*/


        /*public bool Update_reval_supp(IDbConnection conn_db, IDbTransaction transactionID, CalcTypes.PackXX fn_packXX, bool flgUpdateTotal, out Returns ret)
        {

            string sql_text;
            ExecSQL(conn_db, transactionID, " Drop table ttt_paxx ", false);
#if PG
            sql_text = " Select nzp_payer, nzp_supp, nzp_dom,nzp_serv, -1 as nzp_bank, 1 as kod, sum(sum_reval) as sum_reval " +
                " Into temp ttt_paxx  " +
                " From " + fn_packXX.fn_reval +
                " Where dat_oper = " + fn_packXX.paramcalc.dat_oper +
                " Group by 1,2,3,4,5 ";

            //ret = ExecSQL(conn_db,transactionID,sql_text, true);
#else
            sql_text = " Select nzp_payer, nzp_supp, nzp_dom,nzp_serv, -1 as nzp_bank, 1 as kod, sum(sum_reval) as sum_reval " +
                " From " + fn_packXX.fn_reval +
                " Where dat_oper = " + fn_packXX.paramcalc.dat_oper + "  " +
                " Group by 1,2,3,4,5 " +
                " Into temp ttt_paxx With no log ";

            ret = ExecSQL(conn_db, transactionID, sql_text, true);

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
                    " Where sum_reval<> 0 and dat_oper = " + fn_packXX.paramcalc.dat_oper
                    , true);
                ExecSQL(conn_db, transactionID, " Drop table ttt_paxx ", false);
                return true;


            }


            //ExecSQL(conn_db, transactionID, " Create unique index ix1_ttt_paxx on ttt_paxx (nzp_payer, nzp_area, nzp_dom, nzp_serv, nzp_bank) ", true);
            ExecSQL(conn_db, transactionID, " Create  index ix1_ttt_paxx on ttt_paxx (nzp_payer, nzp_supp, nzp_dom, nzp_serv, nzp_bank) ", true);
#if PG
            ExecSQL(conn_db, transactionID, " analyze ttt_paxx ", true);
#else
            ExecSQL(conn_db, transactionID, " Update statistics for table ttt_paxx ", true);
#endif

            ExecSQL(conn_db, "drop table t_helpsdistrib", false);

            sqlText = "Create temp table t_helpsdistrib(" +
                      " nzp_supp integer," +
                      " nzp_payer integer," +
                      " nzp_dom integer," +
                      " nzp_serv integer," +
                      " nzp_bank integer)" + DBManager.sUnlogTempTable;
            ret = ExecSQL(conn_db, sqlText, true);

            if (!ret.result)
            {
                return false;
            }

            sqlText = " insert into t_helpsdistrib(nzp_supp, nzp_payer,  nzp_dom, nzp_serv, nzp_bank)" +
                      " select nzp_supp, nzp_payer,  nzp_dom, nzp_serv, nzp_bank " +
                      " From " + fn_packXX.fn_distrib + " a " +
                      " Where dat_oper = " + fn_packXX.paramcalc.dat_oper + sConvToDate;
            ret = ExecSQL(conn_db, sqlText, true);

            if (!ret.result)
            {
                return false;
            }

            ExecSQL(conn_db, " Create  index ix1_t_helpsdistrib on t_helpsdistrib (nzp_supp, " +
                 " nzp_payer,  nzp_dom, nzp_serv, nzp_bank) ", true);

            ExecSQL(conn_db, DBManager.sUpdStat + "  t_helpsdistrib", true);
            sql_text = " Update ttt_paxx " +
                " Set kod = 0 " +
                " Where 0 < ( Select count(*) From t_helpsdistrib a " +
                            " Where a.nzp_payer= ttt_paxx.nzp_payer " +
                            "   and a.nzp_supp = ttt_paxx.nzp_supp " +
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
            ExecSQL(conn_db, transactionID, DBManager.sUpdStat + " ttt_paxx ", true);

            //DataTable drr2 = ViewTbl(conn_db, " select * from ttt_paxx  ");
            sql_text = " Insert into " + fn_packXX.fn_distrib + " ( nzp_payer,nzp_supp, nzp_dom,nzp_serv,nzp_bank,dat_oper ) " +
                " Select nzp_payer,nzp_supp, nzp_dom,nzp_serv,nzp_bank, " + fn_packXX.paramcalc.dat_oper +
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
                            "   and a.nzp_supp = " + fn_packXX.fn_distrib + ".nzp_supp " +
                            "   and a.nzp_dom = " + fn_packXX.fn_distrib + ".nzp_dom " +
                            "   and a.nzp_serv = " + fn_packXX.fn_distrib + ".nzp_serv " +
                            "   and a.nzp_bank = " + fn_packXX.fn_distrib + ".nzp_bank " +
                            " ) " +
                " Where dat_oper = " + fn_packXX.paramcalc.dat_oper +
                "   and 0 < ( Select count(*) From ttt_paxx a " +
                            " Where a.nzp_payer= " + fn_packXX.fn_distrib + ".nzp_payer " +
                            "   and a.nzp_supp = " + fn_packXX.fn_distrib + ".nzp_supp " +
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
        }*/
    }
}
