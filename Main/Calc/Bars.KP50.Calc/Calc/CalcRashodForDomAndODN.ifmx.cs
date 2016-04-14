// Подсчет расходов

using System.Collections.Generic;

#region Подключаемые модули

using System;
using System.Data;

using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;
using STCLINE.KP50.IFMX.Kernel.source.CommonType;

#endregion Подключаемые модули

#region здесь производится подсчет расходов

namespace STCLINE.KP50.DataBase
{

    public partial class DbCalcCharge : DataBaseHead
    {

        #region итого по дому - stek=3/39 & nzp_type = 1 - суммы по ЛС
        //--------------------------------------------------------------------------------
        // для ОДПУ от ГКал расходы проставляются!
        public bool UpdLsValsToODPU(IDbConnection conn_db, Rashod rashod, out Returns ret)
        //--------------------------------------------------------------------------------
        {
            ret = Utils.InitReturns();

            ExecSQL(conn_db, " Drop table ttt_aid_virt0 ", false);

            ret = ExecSQL(conn_db,
                " Create temp table ttt_aid_virt0 " +
                " ( nzp_dom  integer, " +
                "   nzp_serv integer, " +
                "   cnt1     integer, " +
                "   gil1   " + sDecimalType + "(15,7) default 0.00," +
                "   gil2   " + sDecimalType + "(15,7) default 0.00," +
                "   val1   " + sDecimalType + "(15,7) default 0.00," +
                "   val1_source   " + sDecimalType + "(15,7) default 0.00," +
                "   val2   " + sDecimalType + "(15,7) default 0.00," +
                "   squ1   " + sDecimalType + "(15,7) default 0.00," +
                "   squ2   " + sDecimalType + "(15,7) default 0.00," +
                "   cls1     integer, " +
                "   cls2     integer, " +
                "   rvirt  " + sDecimalType + "(15,7) default 0.00 " +
                " ) " + sUnlogTempTable
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }



            #region Проверка на переполнение поля numeric - сверхбольшой расход по ИПУ

            var sql2 = " SELECT count(*) from t_selkvar s, " + rashod.counters_xx + " t" +
                        " WHERE s.nzp_kvar=t.nzp_kvar AND t.val2>1000000 " +
                        " AND t.nzp_kvar NOT IN (SELECT nzp FROM " + rashod.paramcalc.pref + sDataAliasRest +
                        " link_group WHERE nzp_group=13)";
            var count_over_rash = CastValue<int>(ExecScalar(conn_db, sql2, out ret, true));
            if (count_over_rash > 0)
            {
                //пишем лс с большими показаниями ипу 
                sql2 = " INSERT INTO " + rashod.paramcalc.pref + sDataAliasRest + "link_group (nzp_group, nzp) " +
                          " SELECT 13, s.nzp_kvar from t_selkvar s, " + rashod.counters_xx + " t" +
                          " WHERE s.nzp_kvar=t.nzp_kvar AND t.val2>1000000 " +
                          " AND t.nzp_kvar NOT IN (SELECT nzp FROM " + rashod.paramcalc.pref + sDataAliasRest +
                          " link_group WHERE nzp_group=13)";
                ret = ExecSQL(conn_db, sql2, true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }


                //сигнализирует в списке заданий о выполнении расчета с ошибками 
                status = FonTask.Statuses.WithErrors;
            }





            #endregion Проверка на переполнение поля numeric - сверхбольшой расход по ИПУ

            // !!! перейти на учет только из стека 3 по Where условию !!!
            ret = ExecSQL(conn_db,
                " Insert into ttt_aid_virt0 (nzp_dom,nzp_serv,cnt1,gil1,gil2,val1,val1_source,val2,squ1,squ2,cls1,cls2,rvirt) " +
                " Select nzp_dom, nzp_serv, " +
                //кол-во жильцов с учетом вр.выб.
                       " sum(cnt1) as cnt1, " + // целое
                       " sum(gil1) as gil1, " + // дробное

                       //кол-во жильцов по лс без ПУ
                       " sum(case when cnt_stage = 0 then gil1 else 0 end) as gil2, " +

                       //расход по нормативу - пока норматив | изменения | средние - c учетом повышающего коэф-та
                       " sum(val1) as val1, " +

                        //расход по нормативу - пока норматив | изменения | средние - без учета пов.коэф.
                       " sum(val1_source) as val1_source, " +

                       //расход или средние по ИПУ
                       " sum(val2) as val2, " +

                       //площадь по всем лс
                       " sum(case when nzp_serv=8 then squ2 else squ1 end) as squ1, " +

                       //площадь по лс без ПУ
                       " sum(case when cnt_stage = 0 then (case when nzp_serv=8 then squ2 else squ1 end) else 0 end) as squ2, " +

                       //кол-во лс по услуге
                       " count(*) as cls1, " +

                       //кол-во лс по услуге без ПУ
                       " sum(case when cnt_stage = 0 then 1 else 0 end) as cls2, " +

                       //виртуальный расход
                       " sum(rvirt) as rvirt  " +
                " From " + rashod.counters_xx +
                " Where nzp_type = 3 and stek = 3 and " + rashod.where_dom + rashod.paramcalc.per_dat_charge +
                " Group by 1,2 "
                , true);
            if (!ret.result)
            { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ExecSQL(conn_db, " Create unique index ix_aid33_v110 on ttt_aid_virt0 (nzp_dom,nzp_serv) ", true);
            ExecSQL(conn_db, sUpdStat + " ttt_aid_virt0 ", true);

            string sql =
                " Update " + rashod.counters_xx +
                " Set (cnt1,gil1,gil2,val1,val1_source,val2,squ1,squ2,rvirt,cls1,cls2) = ( " +
#if PG
 "(Select cnt1 From ttt_aid_virt0 a " +
                    " Where " + rashod.counters_xx + ".nzp_dom  = a.nzp_dom   " +
                    "   and " + rashod.counters_xx + ".nzp_serv = a.nzp_serv)," +
                    "(Select gil1 From ttt_aid_virt0 a " +
                    " Where " + rashod.counters_xx + ".nzp_dom  = a.nzp_dom   " +
                    "   and " + rashod.counters_xx + ".nzp_serv = a.nzp_serv)," +
                    "(Select gil2 From ttt_aid_virt0 a " +
                    " Where " + rashod.counters_xx + ".nzp_dom  = a.nzp_dom   " +
                    "   and " + rashod.counters_xx + ".nzp_serv = a.nzp_serv)," +
                    "(Select val1 From ttt_aid_virt0 a " +
                    " Where " + rashod.counters_xx + ".nzp_dom  = a.nzp_dom   " +
                    "   and " + rashod.counters_xx + ".nzp_serv = a.nzp_serv)," +
                    "(Select val1_source From ttt_aid_virt0 a " +
                    " Where " + rashod.counters_xx + ".nzp_dom  = a.nzp_dom   " +
                    "   and " + rashod.counters_xx + ".nzp_serv = a.nzp_serv)," +
                    "(Select (case when val2>" + Constants.max_val_for_pu +
                    " then " + Constants.max_val_for_pu + " else val2 end) as val2 " + //защита от переполнения поля  numeric
                    "  From ttt_aid_virt0 a " +
                    " Where " + rashod.counters_xx + ".nzp_dom  = a.nzp_dom   " +
                    "   and " + rashod.counters_xx + ".nzp_serv = a.nzp_serv)," +
                    "(Select squ1 From ttt_aid_virt0 a " +
                    " Where " + rashod.counters_xx + ".nzp_dom  = a.nzp_dom   " +
                    "   and " + rashod.counters_xx + ".nzp_serv = a.nzp_serv)," +
                    "(Select squ2 From ttt_aid_virt0 a " +
                    " Where " + rashod.counters_xx + ".nzp_dom  = a.nzp_dom   " +
                    "   and " + rashod.counters_xx + ".nzp_serv = a.nzp_serv)," +
                    "(Select rvirt From ttt_aid_virt0 a " +
                    " Where " + rashod.counters_xx + ".nzp_dom  = a.nzp_dom   " +
                    "   and " + rashod.counters_xx + ".nzp_serv = a.nzp_serv)," +
                    "(Select cls1 From ttt_aid_virt0 a " +
                    " Where " + rashod.counters_xx + ".nzp_dom  = a.nzp_dom   " +
                    "   and " + rashod.counters_xx + ".nzp_serv = a.nzp_serv)," +
                    "(Select cls2 From ttt_aid_virt0 a " +
                    " Where " + rashod.counters_xx + ".nzp_dom  = a.nzp_dom   " +
                    "   and " + rashod.counters_xx + ".nzp_serv = a.nzp_serv) " +
#else
                    "(Select cnt1,gil1,gil2,val1,val1_source,val2,squ1,squ2,rvirt,cls1,cls2 From ttt_aid_virt0 a " +
                    " Where " + rashod.counters_xx + ".nzp_dom  = a.nzp_dom   " +
                    "   and " + rashod.counters_xx + ".nzp_serv = a.nzp_serv) " +
#endif
 " ) " +
                " Where nzp_type = 1 and stek in (3,39) and " + rashod.where_dom + rashod.paramcalc.per_dat_charge +
                  " and exists ( Select 1 From ttt_aid_virt0 a " +
                            " Where " + rashod.counters_xx + ".nzp_dom  = a.nzp_dom  " +
                            "   and " + rashod.counters_xx + ".nzp_serv = a.nzp_serv " +
                            " ) ";
            ret = ExecSQL(conn_db, sql, true);
            if (!ret.result)
            { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ret = ExecSQL(conn_db, " Drop table ttt_aid_virt0 ", true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            #region Расчет суммарных значений для ЛС дома по ЛС без ИПУ в итоговом расходе дома

            ExecSQL(conn_db, "drop table t_summ_properties_select;", false);

            sql =
              " Select c_xx.nzp_dom,c_xx.nzp_serv,sum(c_xx.gil1) sum_gil1,sum(c_xx.squ1) sum_squ1,sum(c_xx.squ2) sum_squ2,count(c_xx.nzp_kvar) ls_count " +
              " into temp t_summ_properties_select " +
              " from " + rashod.counters_xx + " c_xx " +
              " where c_xx.stek = 3 and c_xx.nzp_type = 3 and c_xx.cnt_stage not in (1,9) and " + rashod.where_dom + rashod.paramcalc.per_dat_charge + " group by 1,2; ";
            ret = ExecSQL(conn_db, sql, true);
            if (!ret.result)
            {
                DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false;
            }

            ExecSQL(conn_db, "Create index ix_t_summ_properties_select_01 on t_summ_properties_select (nzp_dom,nzp_serv);", true);

            sql =
                " Update " + rashod.counters_xx + " set gil2 = sq.sum_gil1,pu7kw = sq.sum_squ1,gl7kw = sq.sum_squ2,cls2 = sq.ls_count " +
                " from t_summ_properties_select sq " +
                " where " + rashod.counters_xx + ".stek = 3 " +
                " and sq.nzp_dom = " + rashod.counters_xx + ".nzp_dom and sq.nzp_serv = " + rashod.counters_xx + ".nzp_serv and ("
                + rashod.counters_xx + ".nzp_type = 1 or " + rashod.counters_xx + ".nzp_type = 2) ";
            ret = ExecSQL(conn_db, sql, true);
            if (!ret.result)
            {
                DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false;
            }
            #endregion

            return true;
        }

        #endregion итого по дому - stek=3/39 & nzp_type = 1 - суммы по ЛС

        #region итого по ГрПУ - stek=3/39 & nzp_type = 2 - суммы по ЛС
        //--------------------------------------------------------------------------------
        // для ГрПУ от ГКал расходы проставляются!
        public bool UpdLsValsToGrPU(IDbConnection conn_db, Rashod rashod, out Returns ret)
        //--------------------------------------------------------------------------------
        {
            ret = Utils.InitReturns();

            ExecSQL(conn_db, " Drop table ttt_ans_virt1 ", false);

            ret = ExecSQL(conn_db,
                " Create temp table ttt_ans_virt1 " +
                " ( nzp_counter integer, " +
                "   nzp_serv    integer  " +
                " ) " + sUnlogTempTable
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            // !!! перейти на учет только из стека 3 по Where условию !!!
            ret = ExecSQL(conn_db,
                " Insert into ttt_ans_virt1 (nzp_counter,nzp_serv) " +
                " Select nzp_counter,nzp_serv " +
                " From " + rashod.counters_xx +
                " Where nzp_type = 2 and stek in (3,39) and " + rashod.where_dom + rashod.paramcalc.per_dat_charge +
                " Group by 1,2 "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ExecSQL(conn_db, " Create unique index ix_ttt_ans_virt1 on ttt_ans_virt1 (nzp_counter) ", true);
            ExecSQL(conn_db, sUpdStat + " ttt_ans_virt1 ", true);

            ExecSQL(conn_db, " Drop table ttt_aid_virt0 ", false);

            ret = ExecSQL(conn_db,
                " Create temp table ttt_aid_virt0 " +
                " ( nzp_counter integer, " +
                "   cnt1        integer, " +
                "   gil1   " + sDecimalType + "(15,7) default 0.00," +
                "   gil2   " + sDecimalType + "(15,7) default 0.00," +
                "   val1   " + sDecimalType + "(15,7) default 0.00," +
                "   val1_source   " + sDecimalType + "(15,7) default 0.00," +
                "   val2   " + sDecimalType + "(15,7) default 0.00," +
                "   squ1   " + sDecimalType + "(15,7) default 0.00," +
                "   squ2   " + sDecimalType + "(15,7) default 0.00," +
                "   cls1     integer, " +
                "   cls2     integer, " +
                "   rvirt  " + sDecimalType + "(15,7) default 0.00 " +
                " ) " + sUnlogTempTable
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            // !!! перейти на учет только из стека 3 по Where условию !!!
            ret = ExecSQL(conn_db,
                " Insert into ttt_aid_virt0 (nzp_counter,cnt1,gil1,gil2,val1,val1_source,val2,squ1,squ2,cls1,cls2,rvirt) " +
                " Select v.nzp_counter, " +
                //кол-во жильцов с учетом вр.выб.
                       " sum(a.cnt1) as cnt1, " + // целое
                       " sum(a.gil1) as gil1, " + // дробное

                       //кол-во жильцов по лс без ПУ
                       " sum(case when a.cnt_stage = 0 then a.gil1 else 0 end) as gil2, " +

                       //расход по нормативу - пока норматив | изменения | средние - с учетом повышающего коэф.
                       " sum(case when a.cnt_stage = 0 then a.val1 else 0 end) as val1, " +

                        //расход по нормативу - пока норматив | изменения | средние - без учета пов. коэф.
                       " sum(case when a.cnt_stage = 0 then a.val1_source else 0 end) as val1_source, " +

                       //расход или средние по ИПУ
                       " sum(case when a.cnt_stage > 0 then a.val2 else 0 end) as val2, " +

                       //площадь по всем лс
                       " sum(case when a.nzp_serv=8 then a.squ2 else a.squ1 end) as squ1, " +

                       //площадь по лс без ПУ
                       " sum(case when a.cnt_stage = 0 then (case when a.nzp_serv=8 then a.squ2 else a.squ1 end) else 0 end) as squ2, " +

                       //кол-во лс по услуге
                       " count(*) as cls1, " +

                       //кол-во лс по услуге без ПУ
                       " sum(case when a.cnt_stage = 0 then 1 else 0 end) as cls2, " +

                       //виртуальный расход
                       " sum(a.rvirt) as rvirt  " +
                " From " + rashod.counters_xx + " a, ttt_ans_virt1 v, temp_counters_link b " +
                " Where a.nzp_kvar=b.nzp_kvar and a.nzp_serv=v.nzp_serv and v.nzp_counter=b.nzp_counter " +
                " and a.nzp_type = 3 and a.stek = 3 " +
                " Group by 1 "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ExecSQL(conn_db, " Create unique index ix_aid33_v110 on ttt_aid_virt0 (nzp_counter) ", true);
            ExecSQL(conn_db, sUpdStat + " ttt_aid_virt0 ", true);

            string sql =
                " Update " + rashod.counters_xx +
                " Set (cnt1,gil1,gil2,val1,val1_source,val2,squ1,squ2,rvirt,cls1,cls2) = ( " +
#if PG
 "(Select cnt1 From ttt_aid_virt0 a " +
                    " Where " + rashod.counters_xx + ".nzp_counter  = a.nzp_counter)," +
                    "(Select gil1 From ttt_aid_virt0 a " +
                    " Where " + rashod.counters_xx + ".nzp_counter  = a.nzp_counter)," +
                    "(Select gil2 From ttt_aid_virt0 a " +
                    " Where " + rashod.counters_xx + ".nzp_counter  = a.nzp_counter)," +
                    "(Select val1 From ttt_aid_virt0 a " +
                    " Where " + rashod.counters_xx + ".nzp_counter  = a.nzp_counter)," +
                     "(Select val1_source From ttt_aid_virt0 a " +
                    " Where " + rashod.counters_xx + ".nzp_counter  = a.nzp_counter)," +
                    "(Select val2 From ttt_aid_virt0 a " +
                    " Where " + rashod.counters_xx + ".nzp_counter  = a.nzp_counter)," +
                    "(Select squ1 From ttt_aid_virt0 a " +
                    " Where " + rashod.counters_xx + ".nzp_counter  = a.nzp_counter)," +
                    "(Select squ2 From ttt_aid_virt0 a " +
                    " Where " + rashod.counters_xx + ".nzp_counter  = a.nzp_counter)," +
                    "(Select rvirt From ttt_aid_virt0 a " +
                    " Where " + rashod.counters_xx + ".nzp_counter  = a.nzp_counter)," +
                    "(Select cls1 From ttt_aid_virt0 a " +
                    " Where " + rashod.counters_xx + ".nzp_counter  = a.nzp_counter)," +
                    "(Select cls2 From ttt_aid_virt0 a " +
                    " Where " + rashod.counters_xx + ".nzp_counter  = a.nzp_counter) " +
#else
                    "(Select cnt1,gil1,gil2,val1,val1_source,val2,squ1,squ2,rvirt,cls1,cls2 From ttt_aid_virt0 a " +
                    " Where " + rashod.counters_xx + ".nzp_counter  = a.nzp_counter) " +
#endif
 " ) " +
                " Where nzp_type = 2 and stek in (3,39) and " + rashod.where_dom + rashod.paramcalc.per_dat_charge +
                  " and 0 < ( Select count(*) From ttt_aid_virt0 a " +
                            " Where " + rashod.counters_xx + ".nzp_counter  = a.nzp_counter) ";
            ret = ExecSQL(conn_db, sql, true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ret = ExecSQL(conn_db, " Drop table ttt_aid_virt0 ", true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ret = ExecSQL(conn_db, " Drop table ttt_ans_virt1 ", true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            return true;
        }

        #endregion итого по ГрПУ - stek=3/39 & nzp_type = 2 - суммы по ЛС

        #region  проставить расход ОДПУ - stek=3/39 & nzp_type = 1
        //--------------------------------------------------------------------------------
        // для ОДПУ от ГКал расходы проставляются! - связь по nzp_dom!
        public bool SetValODPU(IDbConnection conn_db, Rashod rashod, out Returns ret)
        //--------------------------------------------------------------------------------
        {
            ret = Utils.InitReturns();

            ExecSQL(conn_db, " Drop table ttt_aid_dpu ", false);

            ret = ExecSQL(conn_db,
                " Create temp table ttt_aid_dpu " +
                " ( nzp_dom     integer, " +
                "   nzp_serv    integer, " +
                "   nzp_counter integer default 0, " +
                "   is_odn_pu   integer default 0, " +
                "   val1   " + sDecimalType + "(15,7) default 0.00," +
                "   val_ls " + sDecimalType + "(15,7) default 0.00" +
                " ) " + sUnlogTempTable
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ret = ExecSQL(conn_db,
                " Insert into ttt_aid_dpu (nzp_dom,nzp_serv,nzp_counter,val1) " +
                " Select nzp_dom, nzp_serv, nzp_counter, sum(val1) as val1 " +
                " From " + rashod.counters_xx +
                " Where nzp_type = 1 and stek = 1 and " + rashod.where_dom + rashod.paramcalc.per_dat_charge +
                " Group by 1,2,3 "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ExecSQL(conn_db, sUpdStat + " ttt_aid_dpu ", true);

            //ViewTbl(conn_db, " select * from ttt_aid_dpu ");

            ExecSQL(conn_db, " Drop table ttt_ans_dpux ", false);
            //получаем ПУ по которым есть показания
            ret = ExecSQL(conn_db,
                " CREATE TEMP TABLE ttt_ans_dpux AS " +
                " Select nzp_dom, nzp_serv, nzp_counter " +
                " From ttt_aid_dpu group by 1,2,3 "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }
            ExecSQL(conn_db, " Create index ix_ans_dpux on ttt_ans_dpux (nzp_dom,nzp_serv,nzp_counter) ", false);
            ExecSQL(conn_db, sUpdStat + " ttt_ans_dpux ", false);

            //дописываем связанные ПУ, по заменам для которых есть показания
            //считаем так: если есть показание по одному ПУ, то есть показания по всем связанным ПУ
            var sql = " INSERT INTO ttt_ans_dpux (nzp_dom,nzp_serv,nzp_counter)" +
                      " SELECT DISTINCT cs.nzp, cs.nzp_serv, r.nzp_counter FROM t_old_new_counters r, temp_cnt_spis cs" +
                      " WHERE r.nzp_counter=cs.nzp_counter AND cs.nzp_type=1" +
                      " AND EXISTS (SELECT 1 FROM t_old_new_counters rr, ttt_aid_dpu ii" +
                      "              WHERE rr.nzp_counter=ii.nzp_counter " +
                      "              AND rr.nzp_counter_old=r.nzp_counter_old)";
            ret = ExecSQL(conn_db, sql, true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ExecSQL(conn_db, " Create index ix1_ttt_ans_dpux on ttt_ans_dpux (nzp_counter) ", true);

            // дополнение расходов ДПУ  из стека средних значений по ОДПУ nzp_type = 1 and stek = 2
            ret = ExecSQL(conn_db,
                " insert into ttt_aid_dpu (nzp_dom, nzp_serv, nzp_counter, val1) " +
                " Select nzp_dom, nzp_serv, nzp_counter, sum(val1) as val1 " +
                " From " + rashod.counters_xx + " c" +
                " Where nzp_type = 1 and stek = 2 and kod_info = 0 and " + rashod.where_dom + rashod.paramcalc.per_dat_charge +
                " and not exists (select 1 from ttt_ans_dpux t where c.nzp_dom=t.nzp_dom" +
                       " and c.nzp_serv=t.nzp_serv and c.nzp_counter=t.nzp_counter) " +
                " Group by 1,2,3 "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            //ViewTbl(conn_db, " select * from ttt_aid_dpux ");

            //ViewTbl(conn_db, " select * from ttt_aid_dpu ");

            ExecSQL(conn_db, " Drop table ttt_ans_dpux ", false);
            ret = ExecSQL(conn_db,
                " Create temp table ttt_ans_dpux " +
                " ( nzp_dom     integer, " +
                "   nzp_serv    integer  " +
                " ) " + sUnlogTempTable
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ret = ExecSQL(conn_db,
                " Insert into ttt_ans_dpux (nzp_dom,nzp_serv) " +
                " Select nzp_dom, nzp_serv From ttt_aid_dpu group by 1,2 "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }
            ExecSQL(conn_db, " Create index ix_ans_dpux on ttt_ans_dpux (nzp_dom,nzp_serv) ", false);
            ExecSQL(conn_db, sUpdStat + " ttt_ans_dpux ", false);

            // дополнение расходов ДПУ  из стека средних значений nzp_type = 1 and stek = 4
            ret = ExecSQL(conn_db,
                " insert into ttt_aid_dpu (nzp_dom, nzp_serv, nzp_counter, val1) " +
                " Select nzp_dom, nzp_serv, nzp_counter, sum(val1) as val1 " +
                " From " + rashod.counters_xx + " c" +
                " Where nzp_type = 1 and stek = 4 and " + rashod.where_dom + rashod.paramcalc.per_dat_charge +
                " and not exists (select 1 from ttt_ans_dpux t where c.nzp_dom=t.nzp_dom and c.nzp_serv=t.nzp_serv) " +
                " Group by 1,2,3 "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            //ViewTbl(conn_db, " select * from ttt_aid_dpux ");

            //ViewTbl(conn_db, " select * from ttt_aid_dpu ");

            ExecSQL(conn_db, " Drop table ttt_ans_dpux ", false);

            ExecSQL(conn_db, " Create index ix_aid_dpu on ttt_aid_dpu (nzp_dom,nzp_serv) ", false);
            ExecSQL(conn_db, sUpdStat + " ttt_aid_dpu ", false);

            // дополнение расходов ДПУ только на ОДН расходом по ЛС
            ret = ExecSQL(conn_db,
                " update ttt_aid_dpu set is_odn_pu = 1 " +
                " Where exists (select 1 from " + rashod.paramcalc.data_alias + "prm_17 p " +
                " where ttt_aid_dpu.nzp_counter=p.nzp and p.nzp_prm=2068 and p.val_prm='1' " +
                " and p.is_actual<>100 and p.dat_s<" + rashod.paramcalc.dat_po + " and p.dat_po>=" + rashod.paramcalc.dat_s + ") "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ret = ExecSQL(conn_db,
                " update ttt_aid_dpu set val_ls = ( " +
                              " Select sum(a.val1 + a.val2 + a.dlt_reval + a.dlt_real_charge) From " + rashod.counters_xx + " a " +
                              " Where ttt_aid_dpu.nzp_dom  = a.nzp_dom  " +
                              "   and ttt_aid_dpu.nzp_serv = a.nzp_serv " +
                              "   and nzp_type = 1 and stek = 3 ) " +
                " Where is_odn_pu = 1 " +
                  " and exists ( Select 1 From " + rashod.counters_xx + " a " +
                              " Where ttt_aid_dpu.nzp_dom  = a.nzp_dom  " +
                              "   and ttt_aid_dpu.nzp_serv = a.nzp_serv " +
                              "   and nzp_type = 1 and stek = 3 ) "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            //
            //ViewTbl(conn_db, " select * from ttt_aid_dpu ");

            // проставить расход ДПУ в stek = 3 - для расчета ОДН и учета ДПУ / cur_zap - признак ОДПУ только на ОДН!
             sql =
                " Update " + rashod.counters_xx +
                " Set cnt_stage=1,(val3,cur_zap) = (( " +
                             " Select sum(val1) + max(val_ls) From ttt_aid_dpu a " +
                             " Where " + rashod.counters_xx + ".nzp_dom  = a.nzp_dom  " +
                             "   and " + rashod.counters_xx + ".nzp_serv = a.nzp_serv " +
                             " )," +
                             " (Select max(is_odn_pu) From ttt_aid_dpu a " +
                             " Where " + rashod.counters_xx + ".nzp_dom  = a.nzp_dom  " +
                             "   and " + rashod.counters_xx + ".nzp_serv = a.nzp_serv " +
                             " )) " +
                " Where nzp_type = 1 and stek in (3,39) and " + rashod.where_dom + rashod.paramcalc.per_dat_charge +
                " and exists ( Select 1 From ttt_aid_dpu a " +
                            " Where " + rashod.counters_xx + ".nzp_dom  = a.nzp_dom  " +
                            "   and " + rashod.counters_xx + ".nzp_serv = a.nzp_serv " +
                            " ) ";

            if (rashod.paramcalc.nzp_kvar > 0 || rashod.paramcalc.nzp_dom > 0)
            {
                ret = ExecSQL(conn_db, sql, true);
            }
            else
            {
                ExecByStep(conn_db, rashod.counters_xx, "nzp_cntx", sql, 100000, " ", out ret);
            }
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ret = ExecSQL(conn_db, " Drop table ttt_ans_dpu ", true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ret = ExecSQL(conn_db, " Drop table ttt_ans_dpux ", true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            return true;
        }

        #endregion проставить расход ОДПУ - stek=3/39 & nzp_type = 1

        #region  проставить расход ГрПУ - stek=3 & nzp_type = 2
        //--------------------------------------------------------------------------------
        // для ГрПУ от ГКал расходы не проставляются! - нет связи ПУ-ПУ!
        public bool SetValGrPU(IDbConnection conn_db, Rashod rashod, out Returns ret)
        //--------------------------------------------------------------------------------
        {
            ret = Utils.InitReturns();

            ExecSQL(conn_db, " Drop table ttt_aid_dpu ", false);

            ret = ExecSQL(conn_db,
                " Create temp table ttt_aid_dpu " +
                " ( nzp_dom     integer, " +
                "   nzp_serv    integer, " +
                "   nzp_counter integer default 0, " +
                "   is_odn_pu   integer default 0, " +
                "   val1   " + sDecimalType + "(15,7) default 0.00," +
                "   val_ls " + sDecimalType + "(15,7) default 0.00" +
                " ) " + sUnlogTempTable
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ret = ExecSQL(conn_db,
                " Insert into ttt_aid_dpu (nzp_dom,nzp_serv,nzp_counter,val1) " +
                " Select nzp_dom, nzp_serv, nzp_counter, sum(val1) as val1 " +
                " From " + rashod.counters_xx +
                " Where nzp_type = 2 and stek = 1 and " + rashod.where_dom + rashod.paramcalc.per_dat_charge +
                " Group by 1,2,3 "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ExecSQL(conn_db, sUpdStat + " ttt_aid_dpu ", true);

            ExecSQL(conn_db, " Drop table ttt_ans_dpux ", false);
        

            ret = ExecSQL(conn_db,
                " CREATE TEMP TABLE ttt_ans_dpux AS " +
                " Select nzp_counter" +
                " From ttt_aid_dpu group by 1 "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            //дописываем связанные ПУ, по заменам для которых есть показания
            //считаем так: если есть показание по одному ПУ, то есть показания по всем связанным ПУ
            var sql = " INSERT INTO ttt_ans_dpux (nzp_counter)" +
                      " SELECT DISTINCT r.nzp_counter FROM t_old_new_counters r" +
                      " WHERE EXISTS (SELECT 1 FROM t_old_new_counters rr, ttt_aid_dpu ii" +
                      "              WHERE rr.nzp_counter=ii.nzp_counter " +
                      "              AND rr.nzp_counter_old=r.nzp_counter_old)";
            ret = ExecSQL(conn_db, sql, true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ExecSQL(conn_db, " Create index ix_ans_dpux on ttt_ans_dpux (nzp_counter) ", false);
            ExecSQL(conn_db, sUpdStat + " ttt_ans_dpux ", false);

            // дополнение расходов ДПУ  из стека средних значений по ОДПУ nzp_type = 1 and stek = 2
            ret = ExecSQL(conn_db,
                " insert into ttt_aid_dpu (nzp_dom, nzp_serv, nzp_counter, val1) " +
                " Select nzp_dom, nzp_serv, nzp_counter, sum(val1) as val1 " +
                " From " + rashod.counters_xx + " c" +
                " Where nzp_type = 2 and stek = 2 and kod_info = 0 and " + rashod.where_dom + rashod.paramcalc.per_dat_charge +
                " and not exists (select 1 from ttt_ans_dpux t where c.nzp_counter=t.nzp_counter) " +
                " Group by 1,2,3 "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ExecSQL(conn_db, " Drop table ttt_ans_dpux ", false);
            ret = ExecSQL(conn_db,
                " Create temp table ttt_ans_dpux " +
                " ( nzp_counter integer " +
                " ) " + sUnlogTempTable
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ret = ExecSQL(conn_db,
                " Insert into ttt_ans_dpux (nzp_counter) " +
                " Select nzp_counter From ttt_aid_dpu group by 1 "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }
            ExecSQL(conn_db, " Create index ix_ans_dpux on ttt_ans_dpux (nzp_counter) ", false);
            ExecSQL(conn_db, sUpdStat + " ttt_ans_dpux ", false);

            // дополнение расходов ДПУ  из стека средних значений stek = 4
            ret = ExecSQL(conn_db,
                " insert into ttt_aid_dpu (nzp_dom, nzp_serv, nzp_counter, val1) " +
                " Select nzp_dom, nzp_serv, nzp_counter, sum(val1) as val1 " +
                " From " + rashod.counters_xx + " c" +
                " Where nzp_type = 2 and stek = 4 and " + rashod.where_dom + rashod.paramcalc.per_dat_charge +
                " and not exists (select 1 from ttt_ans_dpux t where c.nzp_counter=t.nzp_counter) " +
                " Group by 1,2,3 "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            //ViewTbl(conn_db, " select * from ttt_aid_dpux ");

            //ViewTbl(conn_db, " select * from ttt_aid_dpu ");

            ExecSQL(conn_db, " Create index ix_aid_dpu on ttt_aid_dpu (nzp_dom,nzp_serv) ", false);
            ExecSQL(conn_db, sUpdStat + " ttt_aid_dpu ", false);

            // дополнение расходов ДПУ только на ОДН расходом по ЛС
            ret = ExecSQL(conn_db,
                " update ttt_aid_dpu set is_odn_pu = 1 " +
                " Where exists (select 1 from " + rashod.paramcalc.data_alias + "prm_17 p " +
                " where ttt_aid_dpu.nzp_counter=p.nzp and p.nzp_prm=2068 and p.val_prm='1' " +
                " and p.is_actual<>100 and p.dat_s<" + rashod.paramcalc.dat_po + " and p.dat_po>=" + rashod.paramcalc.dat_s + ") "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ret = ExecSQL(conn_db,
                " update ttt_aid_dpu set val_ls = ( " +
                              " Select sum(a.val1 + a.val2 + a.dlt_reval + a.dlt_real_charge) From " + rashod.counters_xx + " a " +
                              " Where ttt_aid_dpu.nzp_counter  = a.nzp_counter  " +
                              "   and a.nzp_type = 2 and a.stek = 3 ) " +
                " Where is_odn_pu = 1 " +
                  " and 0 < ( Select count(*) From " + rashod.counters_xx + " a " +
                              " Where ttt_aid_dpu.nzp_counter  = a.nzp_counter  " +
                              "   and a.nzp_type = 2 and a.stek = 3 ) "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            //ViewTbl(conn_db, " select * from ttt_aid_dpu ");

            // проставить расход ДПУ в stek = 3 - для расчета ОДН и учета ДПУ / cur_zap - признак ОДПУ только на ОДН!
            sql =
                " Update " + rashod.counters_xx +
                " Set cnt_stage=1,(val3,cur_zap) = (( " +
                            " Select sum(val1) + max(val_ls) From ttt_aid_dpu a " +
                            " Where " + rashod.counters_xx + ".nzp_counter  = a.nzp_counter  " +
                            " )," +
                            " (Select max(is_odn_pu) From ttt_aid_dpu a " +
                            " Where " + rashod.counters_xx + ".nzp_counter  = a.nzp_counter  " +
                            " )) " +
                " Where nzp_type = 2 and stek = 3 and " + rashod.where_dom + rashod.paramcalc.per_dat_charge +
                  " and 0 < ( Select count(*) From ttt_aid_dpu a " +
                            " Where " + rashod.counters_xx + ".nzp_counter  = a.nzp_counter  " +
                            " ) ";
            if (rashod.paramcalc.nzp_kvar > 0 || rashod.paramcalc.nzp_dom > 0)
            {
                ret = ExecSQL(conn_db, sql, true);
            }
            else
            {
                ExecByStep(conn_db, rashod.counters_xx, "nzp_cntx", sql, 100000, " ", out ret);
            }
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ret = ExecSQL(conn_db, " Drop table ttt_ans_dpu ", true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ret = ExecSQL(conn_db, " Drop table ttt_ans_dpux ", true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            return true;
        }

        #endregion проставить расход ГрПУ - stek=3 & nzp_type = 2

        #region сохранить стек 3 для домов (nzp_type=1 & 2 and stek=3) в стек 354 с расчетом по Пост.№354 (+ нормативы ОДН)

        #region выбрать домовые расходы для расчета ОДН
        public bool Stek354SelPU(IDbConnection conn_db, Rashod rashod, string pType, out Returns ret)
        //--------------------------------------------------------------------------------
        {
            ret = Utils.InitReturns();

            ExecSQL(conn_db, " Drop table ttt_aid_kpu ", false);

            ret = ExecSQL(conn_db,
                //" CREATE TABLE are.ttt_aid_kpu(       "+
                " CREATE temp TABLE ttt_aid_kpu(        " +
                "   nzp_dom INTEGER NOT NULL,           " +
                "   nzp_kvar INTEGER default 0 NOT NULL," +
                "   nzp_type INTEGER NOT NULL,          " +
                "   nzp_serv INTEGER NOT NULL,          " +
                "   dat_charge DATE,                    " +
                "   cur_zap INTEGER default 0 NOT NULL, " +
                "   nzp_counter INTEGER default 0,      " +
                "   cnt_stage INTEGER default 0,        " +
                "   mmnog " + sDecimalType + "(15,7) default 1.0000000," +
                "   stek INTEGER default 0 NOT NULL,    " +
                "   rashod " + sDecimalType + "(15,7) default 0.0000000 NOT NULL, " +
                "   dat_s DATE NOT NULL,                " +
                "   val_s " + sDecimalType + "(15,7) default 0.0000000 NOT NULL, " +
                "   dat_po DATE NOT NULL,               " +
                "   val_po " + sDecimalType + "(15,7) default 0.0000000 NOT NULL, " +
                "   val1 " + sDecimalType + "(15,7) default 0.0000000 NOT NULL, " +
                "   val2 " + sDecimalType + "(15,7) default 0.0000000 NOT NULL, " +
                "   val3 " + sDecimalType + "(15,7) default 0.0000000, " +
                "   val4 " + sDecimalType + "(15,7) default 0.0000000, " +
                "   rvirt " + sDecimalType + "(15,7) default 0.0000000, " +
                "   squ1 " + sDecimalType + "(15,7) default 0.0000000, " +
                "   squ2 " + sDecimalType + "(15,7) default 0.0000000, " +
                "   cls1 INTEGER default 0 NOT NULL, " +
                "   cls2 INTEGER default 0 NOT NULL, " +
                "   gil1 " + sDecimalType + "(15,7) default 0.0000000, " +
                "   gil2 " + sDecimalType + "(15,7) default 0.0000000, " +
                "   cnt1 INTEGER default 0 NOT NULL, " +
                "   cnt2 INTEGER default 0 NOT NULL, " +
                "   cnt3 INTEGER default 0,          " +
                "   cnt4 INTEGER default 0,          " +
                "   cnt5 INTEGER default 0,          " +
                "   dop87 " + sDecimalType + "(15,7) default 0.0000000, " +
                "   dop87_source " + sDecimalType + "(15,7) default 0.0000000, " +
                "   pu7kw " + sDecimalType + "(15,7) default 0.0000000, " +
                "   gl7kw " + sDecimalType + "(15,7) default 0.0000000, " +
                "   vl210 " + sDecimalType + "(15,7) default 0.0000000, " +
                "   kf307 " + sDecimalType + "(15,7) default 0.0000000, " +
                "   kf307n " + sDecimalType + "(15,7) default 0.0000000, " +
                "   kf307f9 " + sDecimalType + "(15,7) default 0.0000000, " +
                "   kf_dpu_kg " + sDecimalType + "(15,7) default 0.0000000, " +
                "   kf_dpu_plob " + sDecimalType + "(15,7) default 0.0000000, " +
                "   kf_dpu_plot " + sDecimalType + "(15,7) default 0.0000000, " +
                "   kf_dpu_ls " + sDecimalType + "(15,7) default 0.0000000, " +
                "   dlt_in " + sDecimalType + "(15,7) default 0.0000000, " +
                "   dlt_cur " + sDecimalType + "(15,7) default 0.0000000, " +
                "   dlt_reval " + sDecimalType + "(15,7) default 0.0000000, " +
                "   dlt_real_charge " + sDecimalType + "(15,7) default 0.0000000, " +
                "   dlt_calc " + sDecimalType + "(15,7) default 0.0000000,     " +
                "   dlt_out " + sDecimalType + "(15,7) default 0.0000000,     " +
                "   val_norm_odn " + sDecimalType + "(15,7) default 0.0000000, " +
                "   val_norm_odn_source " + sDecimalType + "(15,7) default 0.0000000, " + //норматив ОДН без учета повышающего коэффициента
                "   is_norm  INTEGER default 0, " +
                "   kod_info INTEGER default 0," +
                "   i21      INTEGER default 0," +

                "   nzp_serv_l1 INTEGER default 0 NOT NULL,               " +
                "   squ1_l1 " + sDecimalType + "(15,7) default 0.0000000, " +
                "   squ2_l1 " + sDecimalType + "(15,7) default 0.0000000, " +
                "   cls1_l1 INTEGER default 0 NOT NULL, " +
                "   cls2_l1 INTEGER default 0 NOT NULL, " +
                "   gil1_l1 " + sDecimalType + "(15,7) default 0.0000000, " +
                "   gil2_l1 " + sDecimalType + "(15,7) default 0.0000000, " +
                "   val1_l1 " + sDecimalType + "(15,7) default 0.0000000 NOT NULL, " +
                "   val2_l1 " + sDecimalType + "(15,7) default 0.0000000 NOT NULL, " +
                "   dlt_reval_l1 " + sDecimalType + "(15,7) default 0.0000000, " +
                "   dlt_real_charge_l1 " + sDecimalType + "(15,7) default 0.0000000, " +

                "   nzp_serv_l2 INTEGER default 0 NOT NULL,               " +
                "   squ1_l2 " + sDecimalType + "(15,7) default 0.0000000, " +
                "   squ2_l2 " + sDecimalType + "(15,7) default 0.0000000, " +
                "   cls1_l2 INTEGER default 0 NOT NULL, " +
                "   cls2_l2 INTEGER default 0 NOT NULL, " +
                "   gil1_l2 " + sDecimalType + "(15,7) default 0.0000000, " +
                "   gil2_l2 " + sDecimalType + "(15,7) default 0.0000000, " +
                "   val1_l2 " + sDecimalType + "(15,7) default 0.0000000 NOT NULL, " +
                "   val2_l2 " + sDecimalType + "(15,7) default 0.0000000 NOT NULL, " +
                "   dlt_reval_l2 " + sDecimalType + "(15,7) default 0.0000000, " +
                "   dlt_real_charge_l2 " + sDecimalType + "(15,7) default 0.0000000, " +

                "   rash_link " + sDecimalType + "(15,7) default 0.0000000  ," +
                "   val1_source " + sDecimalType + "(15,7) default 0.0000000 NOT NULL, " +
                "   up_kf " + sDecimalType + "(15,7) default 1.0000000  " + //повышающий коэф-т
                " ) " + sUnlogTempTable
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ret = ExecSQL(conn_db,
                " insert into ttt_aid_kpu (nzp_dom, nzp_kvar, nzp_type, nzp_serv, dat_charge, cur_zap, nzp_counter, cnt_stage, mmnog, stek, rashod, " +
                      " dat_s, val_s, dat_po, val_po, val1, val2, val3, val4, rvirt, squ1, squ2, gil1, gil2, cnt1, cnt2, cnt3, cnt4, " +
                      " cnt5, dop87, pu7kw, gl7kw, vl210, kf307, kod_info,dop87_source,up_kf " +
                      " ,cls1, cls2, kf_dpu_kg, kf_dpu_plob, kf_dpu_plot, kf_dpu_ls, kf307n, kf307f9, dlt_cur, dlt_reval, dlt_real_charge) " +
                " Select nzp_dom, nzp_kvar, nzp_type, nzp_serv, dat_charge, cur_zap, nzp_counter, cnt_stage, mmnog, stek, rashod, " +
                      " dat_s, val_s, dat_po, val_po, val1, val2, val3, val4, rvirt, squ1, squ2, gil1, gil2, cnt1, cnt2, cnt3, cnt4, " +
                      " cnt5, dop87, pu7kw, gl7kw, vl210, kf307, kod_info,dop87_source,up_kf " +
                      " ,cls1, cls2, kf_dpu_kg, kf_dpu_plob, kf_dpu_plot, kf_dpu_ls, kf307n, kf307f9, dlt_cur, dlt_reval, dlt_real_charge " +
                      " From " + rashod.counters_xx +
                " Where nzp_type = " + pType + " and stek = 3 and " + rashod.where_dom + rashod.where_kvar + rashod.paramcalc.per_dat_charge +
                " and nzp_serv in (6,8,9,25,10,210,410) "
                , true);

            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ExecSQL(conn_db, " create index ix_ttt_aid_kpu on ttt_aid_kpu (nzp_dom,nzp_serv,i21) ", true);
            ExecSQL(conn_db, sUpdStat + " ttt_aid_kpu ", true);

            ViewTbl(conn_db, "select * from ttt_aid_kpu");

            return true;
        }
        #endregion выбрать домовые расходы для расчета ОДН

        #region добавить расходы по связным услугам для расчета ОДН
        public bool Stek354AddLinkServ(IDbConnection conn_db, Rashod rashod, out Returns ret)
        //--------------------------------------------------------------------------------
        {
            ret = Utils.InitReturns();

            ExecSQL(conn_db, " Drop table ttt_aid_kpux ", false);

            ret = ExecSQL(conn_db,
                " CREATE temp TABLE ttt_aid_kpux(        " +
                "   nzp_dom INTEGER NOT NULL,           " +
                "   nzp_kvar INTEGER default 0 NOT NULL," +
                "   nzp_type INTEGER NOT NULL,          " +
                "   nzp_serv INTEGER NOT NULL,          " +
                "   dat_charge DATE,                    " +
                "   cur_zap INTEGER default 0 NOT NULL, " +
                "   nzp_counter INTEGER default 0,      " +
                "   cnt_stage INTEGER default 0,        " +
                "   mmnog " + sDecimalType + "(15,7) default 1.0000000," +
                "   stek INTEGER default 0 NOT NULL,    " +
                "   rashod " + sDecimalType + "(15,7) default 0.0000000 NOT NULL, " +
                "   dat_s DATE NOT NULL,                " +
                "   val_s " + sDecimalType + "(15,7) default 0.0000000 NOT NULL, " +
                "   dat_po DATE NOT NULL,               " +
                "   val_po " + sDecimalType + "(15,7) default 0.0000000 NOT NULL, " +
                "   val1 " + sDecimalType + "(15,7) default 0.0000000 NOT NULL, " +
                "   val2 " + sDecimalType + "(15,7) default 0.0000000 NOT NULL, " +
                "   val3 " + sDecimalType + "(15,7) default 0.0000000, " +
                "   val4 " + sDecimalType + "(15,7) default 0.0000000, " +
                "   rvirt " + sDecimalType + "(15,7) default 0.0000000, " +
                "   squ1 " + sDecimalType + "(15,7) default 0.0000000, " +
                "   squ2 " + sDecimalType + "(15,7) default 0.0000000, " +
                "   cls1 INTEGER default 0 NOT NULL, " +
                "   cls2 INTEGER default 0 NOT NULL, " +
                "   gil1 " + sDecimalType + "(15,7) default 0.0000000, " +
                "   gil2 " + sDecimalType + "(15,7) default 0.0000000, " +
                "   cnt1 INTEGER default 0 NOT NULL, " +
                "   cnt2 INTEGER default 0 NOT NULL, " +
                "   cnt3 INTEGER default 0,          " +
                "   cnt4 INTEGER default 0,          " +
                "   cnt5 INTEGER default 0,          " +
                "   dop87 " + sDecimalType + "(15,7) default 0.0000000, " +
                "   pu7kw " + sDecimalType + "(15,7) default 0.0000000, " +
                "   gl7kw " + sDecimalType + "(15,7) default 0.0000000, " +
                "   vl210 " + sDecimalType + "(15,7) default 0.0000000, " +
                "   kf307 " + sDecimalType + "(15,7) default 0.0000000, " +
                "   kf307n " + sDecimalType + "(15,7) default 0.0000000, " +
                "   kf307f9 " + sDecimalType + "(15,7) default 0.0000000, " +
                "   kf_dpu_kg " + sDecimalType + "(15,7) default 0.0000000, " +
                "   kf_dpu_plob " + sDecimalType + "(15,7) default 0.0000000, " +
                "   kf_dpu_plot " + sDecimalType + "(15,7) default 0.0000000, " +
                "   kf_dpu_ls " + sDecimalType + "(15,7) default 0.0000000, " +
                "   dlt_cur " + sDecimalType + "(15,7) default 0.0000000, " +
                "   dlt_reval " + sDecimalType + "(15,7) default 0.0000000, " +
                "   dlt_real_charge " + sDecimalType + "(15,7) default 0.0000000, " +
                "   kod_info INTEGER default 0," +
                "   i21      INTEGER default 0 " +
                " ) " + sUnlogTempTable
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ret = ExecSQL(conn_db,
                " Insert into ttt_aid_kpux " +
                      " (nzp_dom, nzp_kvar, nzp_type, nzp_serv, dat_charge, cur_zap, nzp_counter, cnt_stage, mmnog, stek, rashod, " +
                      " dat_s, val_s, dat_po, val_po, val1, val2, val3, val4, rvirt, squ1, squ2, cls1, cls2, gil1, gil2," +
                      " cnt1, cnt2, cnt3, cnt4, cnt5, dop87, pu7kw, gl7kw, vl210, kf307, kf307n, kf307f9, " +
                      " kf_dpu_kg, kf_dpu_plob, kf_dpu_plot, kf_dpu_ls, dlt_cur, dlt_reval, dlt_real_charge, kod_info " +
                      " ,i21) " +
                " Select nzp_dom, nzp_kvar, nzp_type, nzp_serv, dat_charge, cur_zap, nzp_counter, cnt_stage, mmnog, stek, rashod, " +
                      " dat_s, val_s, dat_po, val_po, val1, val2, val3, val4, rvirt, squ1, squ2, cls1, cls2, gil1, gil2," +
                      " cnt1, cnt2, cnt3, cnt4, cnt5, dop87, pu7kw, gl7kw, vl210, kf307, kf307n, kf307f9, " +
                      " kf_dpu_kg, kf_dpu_plob, kf_dpu_plot, kf_dpu_ls, dlt_cur, dlt_reval, dlt_real_charge, kod_info " +
                      " ,1 as i21" +
                " From ttt_aid_kpu " +
                " Where nzp_serv in (6,25, 9,210,410) "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ExecSQL(conn_db, " create index ix_ttt_aid_kpux on ttt_aid_kpux (nzp_dom,nzp_serv) ", true);
            ExecSQL(conn_db, sUpdStat + " ttt_aid_kpux ", true);

            // для ЭЭ - заготовка для nzp_type_alg=21 с вычетом ЭЭ ночного(nzp_serv=210) и полупикового(nzp_serv=410)
            ret = ExecSQL(conn_db,
                " update ttt_aid_kpu set " +
                " (nzp_serv_l1,val1_l1,val2_l1,squ1_l1,squ2_l1,gil1_l1,gil2_l1,cls1_l1,cls2_l1,dlt_reval_l1,dlt_real_charge_l1) = (" +
                  " ( Select v.nzp_serv        From ttt_aid_kpux v where v.nzp_dom=ttt_aid_kpu.nzp_dom and v.nzp_serv=210 )," +
                  " ( Select v.val1            From ttt_aid_kpux v where v.nzp_dom=ttt_aid_kpu.nzp_dom and v.nzp_serv=210 )," +
                  " ( Select v.val2            From ttt_aid_kpux v where v.nzp_dom=ttt_aid_kpu.nzp_dom and v.nzp_serv=210 )," +
                  " ( Select v.squ1            From ttt_aid_kpux v where v.nzp_dom=ttt_aid_kpu.nzp_dom and v.nzp_serv=210 )," +
                  " ( Select v.squ2            From ttt_aid_kpux v where v.nzp_dom=ttt_aid_kpu.nzp_dom and v.nzp_serv=210 )," +
                  " ( Select v.gil1            From ttt_aid_kpux v where v.nzp_dom=ttt_aid_kpu.nzp_dom and v.nzp_serv=210 )," +
                  " ( Select v.gil2            From ttt_aid_kpux v where v.nzp_dom=ttt_aid_kpu.nzp_dom and v.nzp_serv=210 )," +
                  " ( Select v.cls1            From ttt_aid_kpux v where v.nzp_dom=ttt_aid_kpu.nzp_dom and v.nzp_serv=210 )," +
                  " ( Select v.cls2            From ttt_aid_kpux v where v.nzp_dom=ttt_aid_kpu.nzp_dom and v.nzp_serv=210 )," +
                  " ( Select v.dlt_reval       From ttt_aid_kpux v where v.nzp_dom=ttt_aid_kpu.nzp_dom and v.nzp_serv=210 )," +
                  " ( Select v.dlt_real_charge From ttt_aid_kpux v where v.nzp_dom=ttt_aid_kpu.nzp_dom and v.nzp_serv=210 ) " +
                ") where nzp_serv=25" +
                      " and 0<(select count(*) from ttt_aid_kpux v where v.nzp_dom=ttt_aid_kpu.nzp_dom and v.nzp_serv=210) "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            // для ЭЭ - заготовка для nzp_type_alg=21 с вычетом ЭЭ ночного(nzp_serv=210) и полупикового(nzp_serv=410)
            ret = ExecSQL(conn_db,
                " update ttt_aid_kpu set " +
                " (nzp_serv_l2,val1_l2,val2_l2,squ1_l2,squ2_l2,gil1_l2,gil2_l2,cls1_l2,cls2_l2,dlt_reval_l2,dlt_real_charge_l2) = (" +
                  " ( Select v.nzp_serv        From ttt_aid_kpux v where v.nzp_dom=ttt_aid_kpu.nzp_dom and v.nzp_serv=410 )," +
                  " ( Select v.val1            From ttt_aid_kpux v where v.nzp_dom=ttt_aid_kpu.nzp_dom and v.nzp_serv=410 )," +
                  " ( Select v.val2            From ttt_aid_kpux v where v.nzp_dom=ttt_aid_kpu.nzp_dom and v.nzp_serv=410 )," +
                  " ( Select v.squ1            From ttt_aid_kpux v where v.nzp_dom=ttt_aid_kpu.nzp_dom and v.nzp_serv=410 )," +
                  " ( Select v.squ2            From ttt_aid_kpux v where v.nzp_dom=ttt_aid_kpu.nzp_dom and v.nzp_serv=410 )," +
                  " ( Select v.gil1            From ttt_aid_kpux v where v.nzp_dom=ttt_aid_kpu.nzp_dom and v.nzp_serv=410 )," +
                  " ( Select v.gil2            From ttt_aid_kpux v where v.nzp_dom=ttt_aid_kpu.nzp_dom and v.nzp_serv=410 )," +
                  " ( Select v.cls1            From ttt_aid_kpux v where v.nzp_dom=ttt_aid_kpu.nzp_dom and v.nzp_serv=410 )," +
                  " ( Select v.cls2            From ttt_aid_kpux v where v.nzp_dom=ttt_aid_kpu.nzp_dom and v.nzp_serv=410 )," +
                  " ( Select v.dlt_reval       From ttt_aid_kpux v where v.nzp_dom=ttt_aid_kpu.nzp_dom and v.nzp_serv=410 )," +
                  " ( Select v.dlt_real_charge From ttt_aid_kpux v where v.nzp_dom=ttt_aid_kpu.nzp_dom and v.nzp_serv=410 ) " +
                ") where nzp_serv=25" +
                      " and 0<(select count(*) from ttt_aid_kpux v where v.nzp_dom=ttt_aid_kpu.nzp_dom and v.nzp_serv=410) "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            // для ХВС - заготовка для nzp_type_alg=26 с вычетом ГВС
            ret = ExecSQL(conn_db,
                " update ttt_aid_kpu set " +
                " (nzp_serv_l1,val1_l1,val2_l1,squ1_l1,squ2_l1,gil1_l1,gil2_l1,cls1_l1,cls2_l1,dlt_reval_l1,dlt_real_charge_l1) = (" +
                  " ( Select v.nzp_serv        From ttt_aid_kpux v where v.nzp_dom=ttt_aid_kpu.nzp_dom and v.nzp_serv=9 )," +
                  " ( Select v.val1            From ttt_aid_kpux v where v.nzp_dom=ttt_aid_kpu.nzp_dom and v.nzp_serv=9 )," +
                  " ( Select v.val2            From ttt_aid_kpux v where v.nzp_dom=ttt_aid_kpu.nzp_dom and v.nzp_serv=9 )," +
                  " ( Select v.squ1            From ttt_aid_kpux v where v.nzp_dom=ttt_aid_kpu.nzp_dom and v.nzp_serv=9 )," +
                  " ( Select v.squ2            From ttt_aid_kpux v where v.nzp_dom=ttt_aid_kpu.nzp_dom and v.nzp_serv=9 )," +
                  " ( Select v.gil1            From ttt_aid_kpux v where v.nzp_dom=ttt_aid_kpu.nzp_dom and v.nzp_serv=9 )," +
                  " ( Select v.gil2            From ttt_aid_kpux v where v.nzp_dom=ttt_aid_kpu.nzp_dom and v.nzp_serv=9 )," +
                  " ( Select v.cls1            From ttt_aid_kpux v where v.nzp_dom=ttt_aid_kpu.nzp_dom and v.nzp_serv=9 )," +
                  " ( Select v.cls2            From ttt_aid_kpux v where v.nzp_dom=ttt_aid_kpu.nzp_dom and v.nzp_serv=9 )," +
                  " ( Select v.dlt_reval       From ttt_aid_kpux v where v.nzp_dom=ttt_aid_kpu.nzp_dom and v.nzp_serv=9 )," +
                  " ( Select v.dlt_real_charge From ttt_aid_kpux v where v.nzp_dom=ttt_aid_kpu.nzp_dom and v.nzp_serv=9 ) " +
                ") where nzp_serv=6" +
                      " and 0<(select count(*) from ttt_aid_kpux v where v.nzp_dom=ttt_aid_kpu.nzp_dom and v.nzp_serv=9) "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ret = ExecSQL(conn_db,
                " update ttt_aid_kpu set " +
                " rash_link = val1_l1 + val2_l1 + dlt_reval_l1 + dlt_real_charge_l1 + " +
                             "val1_l2 + val2_l2 + dlt_reval_l2 + dlt_real_charge_l2" +
                " where nzp_serv in (6,25) "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            // удвоим ХВС и ЭЭ с i21=1 ! - заготовка для nzp_type_alg=21 (только ХВС) и 22 (только дневное ЭЭ)
            ret = ExecSQL(conn_db,
                " insert into ttt_aid_kpu" +
                     " ( nzp_dom, nzp_kvar, nzp_type, nzp_serv, dat_charge, cur_zap, nzp_counter, cnt_stage, mmnog, stek, rashod, " +
                      " dat_s, val_s, dat_po, val_po, val1, val2, val3, val4, rvirt, squ1, squ2, gil1, gil2, cnt1, cnt2, cnt3, cnt4, " +
                      " cnt5, dop87, pu7kw, gl7kw, vl210, kf307, kod_info " +
                      " ,cls1, cls2, kf_dpu_kg, kf_dpu_plob, kf_dpu_plot, kf_dpu_ls, kf307n, kf307f9, dlt_cur, dlt_reval, dlt_real_charge " +
                      " ,i21 ) " +
                " Select nzp_dom, nzp_kvar, nzp_type, nzp_serv, dat_charge, cur_zap, nzp_counter, cnt_stage, mmnog, stek, rashod, " +
                      " dat_s, val_s, dat_po, val_po, val1, val2, val3, val4, rvirt, squ1, squ2, gil1, gil2, cnt1, cnt2, cnt3, cnt4, " +
                      " cnt5, dop87, pu7kw, gl7kw, vl210, kf307, kod_info " +
                      " ,cls1, cls2, kf_dpu_kg, kf_dpu_plob, kf_dpu_plot, kf_dpu_ls, kf307n, kf307f9, dlt_cur, dlt_reval, dlt_real_charge " +
                      " ,i21 " +
                " From ttt_aid_kpux " +
                " Where nzp_serv in (6,25) "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ViewTbl(conn_db, "select * from ttt_aid_kpux");

            ViewTbl(conn_db, "select * from ttt_aid_kpu");

            return true;
        }
        #endregion добавить расходы по связным услугам для расчета ОДН

        #region добавить расходы по связным услугам для расчета ОДН для ГрПУ
        public bool Stek354AddLinkServGrPU(IDbConnection conn_db, Rashod rashod, out Returns ret)
        //--------------------------------------------------------------------------------
        {
            ret = Utils.InitReturns();

            ExecSQL(conn_db, " Drop table ttt_aid_kpux ", false);

            ret = ExecSQL(conn_db,
                " CREATE temp TABLE ttt_aid_kpux(        " +
                "   nzp_dom INTEGER NOT NULL,           " +
                "   nzp_kvar INTEGER default 0 NOT NULL," +
                "   nzp_type INTEGER NOT NULL,          " +
                "   nzp_serv INTEGER NOT NULL,          " +
                "   dat_charge DATE,                    " +
                "   cur_zap INTEGER default 0 NOT NULL, " +
                "   nzp_counter INTEGER default 0,      " +
                "   cnt_stage INTEGER default 0,        " +
                "   mmnog " + sDecimalType + "(15,7) default 1.0000000," +
                "   stek INTEGER default 0 NOT NULL,    " +
                "   rashod " + sDecimalType + "(15,7) default 0.0000000 NOT NULL, " +
                "   dat_s DATE NOT NULL,                " +
                "   val_s " + sDecimalType + "(15,7) default 0.0000000 NOT NULL, " +
                "   dat_po DATE NOT NULL,               " +
                "   val_po " + sDecimalType + "(15,7) default 0.0000000 NOT NULL, " +
                "   val1 " + sDecimalType + "(15,7) default 0.0000000 NOT NULL, " +
                "   val2 " + sDecimalType + "(15,7) default 0.0000000 NOT NULL, " +
                "   val3 " + sDecimalType + "(15,7) default 0.0000000, " +
                "   val4 " + sDecimalType + "(15,7) default 0.0000000, " +
                "   rvirt " + sDecimalType + "(15,7) default 0.0000000, " +
                "   squ1 " + sDecimalType + "(15,7) default 0.0000000, " +
                "   squ2 " + sDecimalType + "(15,7) default 0.0000000, " +
                "   cls1 INTEGER default 0 NOT NULL, " +
                "   cls2 INTEGER default 0 NOT NULL, " +
                "   gil1 " + sDecimalType + "(15,7) default 0.0000000, " +
                "   gil2 " + sDecimalType + "(15,7) default 0.0000000, " +
                "   cnt1 INTEGER default 0 NOT NULL, " +
                "   cnt2 INTEGER default 0 NOT NULL, " +
                "   cnt3 INTEGER default 0,          " +
                "   cnt4 INTEGER default 0,          " +
                "   cnt5 INTEGER default 0,          " +
                "   dop87 " + sDecimalType + "(15,7) default 0.0000000, " +
                "   pu7kw " + sDecimalType + "(15,7) default 0.0000000, " +
                "   gl7kw " + sDecimalType + "(15,7) default 0.0000000, " +
                "   vl210 " + sDecimalType + "(15,7) default 0.0000000, " +
                "   kf307 " + sDecimalType + "(15,7) default 0.0000000, " +
                "   kf307n " + sDecimalType + "(15,7) default 0.0000000, " +
                "   kf307f9 " + sDecimalType + "(15,7) default 0.0000000, " +
                "   kf_dpu_kg " + sDecimalType + "(15,7) default 0.0000000, " +
                "   kf_dpu_plob " + sDecimalType + "(15,7) default 0.0000000, " +
                "   kf_dpu_plot " + sDecimalType + "(15,7) default 0.0000000, " +
                "   kf_dpu_ls " + sDecimalType + "(15,7) default 0.0000000, " +
                "   dlt_cur " + sDecimalType + "(15,7) default 0.0000000, " +
                "   dlt_reval " + sDecimalType + "(15,7) default 0.0000000, " +
                "   dlt_real_charge " + sDecimalType + "(15,7) default 0.0000000, " +
                "   kod_info INTEGER default 0," +
                "   i21      INTEGER default 0 " +
                " ) " + sUnlogTempTable
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ret = ExecSQL(conn_db,
                " Insert into ttt_aid_kpux " +
                      " (nzp_dom, nzp_counter, nzp_serv, nzp_type, nzp_kvar, dat_charge, cur_zap, cnt_stage, mmnog, stek, rashod, " +
                      " dat_s, val_s, dat_po, val_po, val1, val2, val3, val4, rvirt, squ1, squ2, cls1, cls2, gil1, gil2," +
                      " cnt1, cnt2, cnt3, cnt4, cnt5, dop87, pu7kw, gl7kw, vl210, kf307, kf307n, kf307f9," +
                      " kf_dpu_kg, kf_dpu_plob, kf_dpu_plot, kf_dpu_ls, dlt_cur, dlt_reval, dlt_real_charge, kod_info " +
                      " ,i21) " +
                " Select v.nzp_dom, v.nzp_counter, v.nzp_serv, max(v.nzp_type), 0 nzp_kvar, max(v.dat_charge), max(v.cur_zap), max(v.cnt_stage), max(v.mmnog)," +
                      " max(a.stek),sum(a.rashod), " +
                      " max(v.dat_s), max(v.val_s), max(v.dat_po), max(v.val_po), sum(a.val1), sum(a.val2), sum(a.val3), sum(a.val4), sum(a.rvirt)," +
                      " sum(case when a.nzp_serv=8 then a.squ2 else a.squ1 end) as squ1, " +
                      " sum(case when a.cnt_stage = 0 then (case when a.nzp_serv=8 then a.squ2 else a.squ1 end) else 0 end) as squ2," +
                      " count(*) as cls1,sum(case when a.cnt_stage = 0 then 1 else 0 end) as cls2," +
                      " sum(a.gil1), sum(case when a.cnt_stage = 0 then a.gil1 else 0 end) as gil2," +
                      " sum(a.cnt1), max(a.cnt2), max(a.cnt3), max(a.cnt4), max(a.cnt5), sum(a.dop87), sum(a.pu7kw), sum(a.gl7kw), sum(a.vl210)," +
                      " max(a.kf307), max(a.kf307n), max(a.kf307f9), " +
                      " 0 kf_dpu_kg,0 kf_dpu_plob,0 kf_dpu_plot,0 kf_dpu_ls,0 dlt_cur,sum(a.dlt_reval),sum(a.dlt_real_charge), max(a.kod_info) " +
                      " ,1 as i21" +
                " From " + rashod.counters_xx + " a, ttt_aid_kpu v, temp_counters_link b " +
                " Where a.nzp_kvar=b.nzp_kvar and v.nzp_counter=b.nzp_counter " +
                " and a.nzp_serv in (9,210,410) " +
                " and a.nzp_type = 3 and a.stek = 3 " +
                " Group by 1,2,3 "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ExecSQL(conn_db, " create index ix_ttt_aid_kpux on ttt_aid_kpux (nzp_counter) ", true);
            ExecSQL(conn_db, sUpdStat + " ttt_aid_kpux ", true);

            // для ЭЭ - заготовка для nzp_type_alg=21 с вычетом ЭЭ ночного(nzp_serv=210) и полупикового(nzp_serv=410)
            ret = ExecSQL(conn_db,
                " update ttt_aid_kpu set " +
                " (nzp_serv_l1,val1_l1,val2_l1,squ1_l1,squ2_l1,gil1_l1,gil2_l1,cls1_l1,cls2_l1,dlt_reval_l1,dlt_real_charge_l1) = (" +
                  " ( Select v.nzp_serv        From ttt_aid_kpux v where v.nzp_counter=ttt_aid_kpu.nzp_counter and v.nzp_serv=210 )," +
                  " ( Select v.val1            From ttt_aid_kpux v where v.nzp_counter=ttt_aid_kpu.nzp_counter and v.nzp_serv=210 )," +
                  " ( Select v.val2            From ttt_aid_kpux v where v.nzp_counter=ttt_aid_kpu.nzp_counter and v.nzp_serv=210 )," +
                  " ( Select v.squ1            From ttt_aid_kpux v where v.nzp_counter=ttt_aid_kpu.nzp_counter and v.nzp_serv=210 )," +
                  " ( Select v.squ2            From ttt_aid_kpux v where v.nzp_counter=ttt_aid_kpu.nzp_counter and v.nzp_serv=210 )," +
                  " ( Select v.gil1            From ttt_aid_kpux v where v.nzp_counter=ttt_aid_kpu.nzp_counter and v.nzp_serv=210 )," +
                  " ( Select v.gil2            From ttt_aid_kpux v where v.nzp_counter=ttt_aid_kpu.nzp_counter and v.nzp_serv=210 )," +
                  " ( Select v.cls1            From ttt_aid_kpux v where v.nzp_counter=ttt_aid_kpu.nzp_counter and v.nzp_serv=210 )," +
                  " ( Select v.cls2            From ttt_aid_kpux v where v.nzp_counter=ttt_aid_kpu.nzp_counter and v.nzp_serv=210 )," +
                  " ( Select v.dlt_reval       From ttt_aid_kpux v where v.nzp_counter=ttt_aid_kpu.nzp_counter and v.nzp_serv=210 )," +
                  " ( Select v.dlt_real_charge From ttt_aid_kpux v where v.nzp_counter=ttt_aid_kpu.nzp_counter and v.nzp_serv=210 ) " +
                ") where nzp_serv=25" +
                      " and 0<(select count(*) from ttt_aid_kpux v where v.nzp_counter=ttt_aid_kpu.nzp_counter and v.nzp_serv=210 ) "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            // для ЭЭ - заготовка для nzp_type_alg=21 с вычетом ЭЭ ночного(nzp_serv=210) и полупикового(nzp_serv=410)
            ret = ExecSQL(conn_db,
                " update ttt_aid_kpu set " +
                " (nzp_serv_l2,val1_l2,val2_l2,squ1_l2,squ2_l2,gil1_l2,gil2_l2,cls1_l2,cls2_l2,dlt_reval_l2,dlt_real_charge_l2) = (" +
                  " ( Select v.nzp_serv        From ttt_aid_kpux v where v.nzp_counter=ttt_aid_kpu.nzp_counter and v.nzp_serv=410 )," +
                  " ( Select v.val1            From ttt_aid_kpux v where v.nzp_counter=ttt_aid_kpu.nzp_counter and v.nzp_serv=410 )," +
                  " ( Select v.val2            From ttt_aid_kpux v where v.nzp_counter=ttt_aid_kpu.nzp_counter and v.nzp_serv=410 )," +
                  " ( Select v.squ1            From ttt_aid_kpux v where v.nzp_counter=ttt_aid_kpu.nzp_counter and v.nzp_serv=410 )," +
                  " ( Select v.squ2            From ttt_aid_kpux v where v.nzp_counter=ttt_aid_kpu.nzp_counter and v.nzp_serv=410 )," +
                  " ( Select v.gil1            From ttt_aid_kpux v where v.nzp_counter=ttt_aid_kpu.nzp_counter and v.nzp_serv=410 )," +
                  " ( Select v.gil2            From ttt_aid_kpux v where v.nzp_counter=ttt_aid_kpu.nzp_counter and v.nzp_serv=410 )," +
                  " ( Select v.cls1            From ttt_aid_kpux v where v.nzp_counter=ttt_aid_kpu.nzp_counter and v.nzp_serv=410 )," +
                  " ( Select v.cls2            From ttt_aid_kpux v where v.nzp_counter=ttt_aid_kpu.nzp_counter and v.nzp_serv=410 )," +
                  " ( Select v.dlt_reval       From ttt_aid_kpux v where v.nzp_counter=ttt_aid_kpu.nzp_counter and v.nzp_serv=410 )," +
                  " ( Select v.dlt_real_charge From ttt_aid_kpux v where v.nzp_counter=ttt_aid_kpu.nzp_counter and v.nzp_serv=410 ) " +
                ") where nzp_serv=25" +
                      " and 0<(select count(*) from ttt_aid_kpux v where v.nzp_counter=ttt_aid_kpu.nzp_counter and v.nzp_serv=410) "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            // для ХВС - заготовка для nzp_type_alg=26 с вычетом ГВС
            ret = ExecSQL(conn_db,
                " update ttt_aid_kpu set " +
                " (nzp_serv_l1,val1_l1,val2_l1,squ1_l1,squ2_l1,gil1_l1,gil2_l1,cls1_l1,cls2_l1,dlt_reval_l1,dlt_real_charge_l1) = (" +
                  " ( Select v.nzp_serv        From ttt_aid_kpux v where v.nzp_counter=ttt_aid_kpu.nzp_counter and v.nzp_serv=9 )," +
                  " ( Select v.val1            From ttt_aid_kpux v where v.nzp_counter=ttt_aid_kpu.nzp_counter and v.nzp_serv=9 )," +
                  " ( Select v.val2            From ttt_aid_kpux v where v.nzp_counter=ttt_aid_kpu.nzp_counter and v.nzp_serv=9 )," +
                  " ( Select v.squ1            From ttt_aid_kpux v where v.nzp_counter=ttt_aid_kpu.nzp_counter and v.nzp_serv=9 )," +
                  " ( Select v.squ2            From ttt_aid_kpux v where v.nzp_counter=ttt_aid_kpu.nzp_counter and v.nzp_serv=9 )," +
                  " ( Select v.gil1            From ttt_aid_kpux v where v.nzp_counter=ttt_aid_kpu.nzp_counter and v.nzp_serv=9 )," +
                  " ( Select v.gil2            From ttt_aid_kpux v where v.nzp_counter=ttt_aid_kpu.nzp_counter and v.nzp_serv=9 )," +
                  " ( Select v.cls1            From ttt_aid_kpux v where v.nzp_counter=ttt_aid_kpu.nzp_counter and v.nzp_serv=9 )," +
                  " ( Select v.cls2            From ttt_aid_kpux v where v.nzp_counter=ttt_aid_kpu.nzp_counter and v.nzp_serv=9 )," +
                  " ( Select v.dlt_reval       From ttt_aid_kpux v where v.nzp_counter=ttt_aid_kpu.nzp_counter and v.nzp_serv=9 )," +
                  " ( Select v.dlt_real_charge From ttt_aid_kpux v where v.nzp_counter=ttt_aid_kpu.nzp_counter and v.nzp_serv=9 ) " +
                ") where nzp_serv=6" +
                      " and 0<(select count(*) from ttt_aid_kpux v where v.nzp_counter=ttt_aid_kpu.nzp_counter and v.nzp_serv=9) "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ret = ExecSQL(conn_db,
                " update ttt_aid_kpu set " +
                " rash_link = val1_l1 + val2_l1 + dlt_reval_l1 + dlt_real_charge_l1 + " +
                             "val1_l2 + val2_l2 + dlt_reval_l2 + dlt_real_charge_l2" +
                " where nzp_serv in (6,25) "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            // удвоим ХВС и ЭЭ с i21=1 ! - заготовка для nzp_type_alg=21 (только ХВС) и 22 (только дневное ЭЭ)
            ret = ExecSQL(conn_db,
                " delete from ttt_aid_kpux where 1=1 "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ret = ExecSQL(conn_db,
                " Insert into ttt_aid_kpux " +
                      " (nzp_dom, nzp_kvar, nzp_type, nzp_serv, dat_charge, cur_zap, nzp_counter, cnt_stage, mmnog, stek, rashod, " +
                      " dat_s, val_s, dat_po, val_po, val1, val2, val3, val4, rvirt, squ1, squ2, cls1, cls2, gil1, gil2," +
                      " cnt1, cnt2, cnt3, cnt4, cnt5, dop87, pu7kw, gl7kw, vl210, kf307, kf307n, kf307f9, " +
                      " kf_dpu_kg, kf_dpu_plob, kf_dpu_plot, kf_dpu_ls, dlt_cur, dlt_reval, dlt_real_charge, kod_info " +
                      " ,i21) " +
                " Select nzp_dom, nzp_kvar, nzp_type, nzp_serv, dat_charge, cur_zap, nzp_counter, cnt_stage, mmnog, stek, rashod, " +
                      " dat_s, val_s, dat_po, val_po, val1, val2, val3, val4, rvirt, squ1, squ2, cls1, cls2, gil1, gil2," +
                      " cnt1, cnt2, cnt3, cnt4, cnt5, dop87, pu7kw, gl7kw, vl210, kf307, kf307n, kf307f9, " +
                      " kf_dpu_kg, kf_dpu_plob, kf_dpu_plot, kf_dpu_ls, dlt_cur, dlt_reval, dlt_real_charge, kod_info " +
                      " ,1 as i21" +
                " From ttt_aid_kpu " +
                " Where nzp_serv in (6,25) "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ret = ExecSQL(conn_db,
                " insert into ttt_aid_kpu" +
                     " ( nzp_dom, nzp_kvar, nzp_type, nzp_serv, dat_charge, cur_zap, nzp_counter, cnt_stage, mmnog, stek, rashod, " +
                      " dat_s, val_s, dat_po, val_po, val1, val2, val3, val4, rvirt, squ1, squ2, gil1, gil2, cnt1, cnt2, cnt3, cnt4, " +
                      " cnt5, dop87, pu7kw, gl7kw, vl210, kf307, kod_info " +
                      " ,cls1, cls2, kf_dpu_kg, kf_dpu_plob, kf_dpu_plot, kf_dpu_ls, kf307n, kf307f9, dlt_cur, dlt_reval, dlt_real_charge " +
                      " ,i21 ) " +
                " Select nzp_dom, nzp_kvar, nzp_type, nzp_serv, dat_charge, cur_zap, nzp_counter, cnt_stage, mmnog, stek, rashod, " +
                      " dat_s, val_s, dat_po, val_po, val1, val2, val3, val4, rvirt, squ1, squ2, gil1, gil2, cnt1, cnt2, cnt3, cnt4, " +
                      " cnt5, dop87, pu7kw, gl7kw, vl210, kf307, kod_info " +
                      " ,cls1, cls2, kf_dpu_kg, kf_dpu_plob, kf_dpu_plot, kf_dpu_ls, kf307n, kf307f9, dlt_cur, dlt_reval, dlt_real_charge " +
                      " ,i21 " +
                " From ttt_aid_kpux " +
                " Where nzp_serv in (6,25) "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }
            /*
            sql =
                " select nzp_dom, nzp_kvar, nzp_type, rash_link," +
                " nzp_serv_l1,squ1_l1,squ2_l1,cls1_l1,cls2_l1,gil1_l1,gil2_l1,val1_l1,val2_l1,dlt_reval_l1,dlt_real_charge_l1 " +
                " from ttt_aid_kpu where nzp_serv=25";
            DataTable DtTbl = new DataTable();
            DtTbl = ViewTbl(conn_db, sql);
            */
            return true;
        }
        #endregion добавить расходы по связным услугам для расчета ОДН для ГрПУ

        #region нормативный расход ОДН по услугам
        public bool Stek354NormODN(IDbConnection conn_db, Rashod rashod, out Returns ret)
        //--------------------------------------------------------------------------------
        {
            ret = Utils.InitReturns();

            MyDataReader reader;

            // вставить норматив ОДН для домов без ОДПУ по Пост.354
            ExecSQL(conn_db, " Drop table ttt_aid_virt0 ", false);
            ret = ExecSQL(conn_db,
                  " Create temp table ttt_aid_virt0 " +
                  " ( nzp_dom  integer, " +
                  "   nzp_serv integer, " +
                  "   cnt_stage integer, " +
                  "   squ_dom  " + sDecimalType + "(15,7) default 0.0000000," +
                  "   squ1     " + sDecimalType + "(15,7) default 0.0000000," +
                  "   squ2     " + sDecimalType + "(15,7) default 0.0000000," +
                  "   squ_mop  " + sDecimalType + "(15,7) default 0.0000000," +
                  "   norm_odn " + sDecimalType + "(15,7) default 0.0000000," +
                  "   dat_post date, " +
                  "   is_hvobor   integer    default 0, " +
                  "   is_liftobor integer    default 0, " +
                  "   is_obor     integer    default 0, " +
                  "   is_gvobor   integer    default 0, " +
                  "   is_otobor   integer    default 0, " +
                  "   is_zapir    integer    default 0, " +
                  "   is_anten    integer    default 0, " +
                  "   is_ppa      integer    default 0, " +
                  "   etag     integer       default 1, " +
                  "   nom_str  integer       default 1, " +
                  "   nom_kol  integer       default 1, " +
                  "   i21      integer       default 0, " +
                  "   is_odn   integer       default 0, " +
                  "   is_cgvs  integer       default 0, " +
                  "   is_liftonly integer    default 0, " +
                  "   val3     " + sDecimalType + "(15,7) default 0.0000," +
                  "   norm_type_id integer," +
                  "   norm_tables_id integer " + // id норматива - по нему можно получить набор влияющих пар-в и их знач.
                  " ) " + sUnlogTempTable
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            // взять ОДПУ без показаний - из-за Самары рассчитать норматив для всех домов
            ret = ExecSQL(conn_db,
                " Insert into ttt_aid_virt0 (nzp_dom,nzp_serv,i21,cnt_stage,squ1,squ2) " +
                " Select nzp_dom, nzp_serv, i21, max(cnt_stage), max(squ1), max(squ1) From ttt_aid_kpu Where 1=1" +
                " group by 1,2,3 "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ExecSQL(conn_db, " Create index ix_ttt_aid_virt1 on ttt_aid_virt0 (nzp_dom,nzp_serv,i21) ", true);
            ExecSQL(conn_db, " Create index ix_ttt_aid_virt2 on ttt_aid_virt0 (is_odn,nzp_serv,dat_post) ", true);
            ExecSQL(conn_db, " Create index ix_ttt_aid_virt3 on ttt_aid_virt0 (cnt_stage) ", true);
            ExecSQL(conn_db, sUpdStat + " ttt_aid_virt0 ", true);

            //Новые нормативы ОДН для начислений ЖКУ
            var isNewNormODN =
                   CheckValBoolPrmWithVal(conn_db, rashod.paramcalc.data_alias, 1978, "5", "1", rashod.paramcalc.dat_s, rashod.paramcalc.dat_s);
            if (isNewNormODN)
            {
                string p_dat_charge = DateNullString;
                if (!rashod.paramcalc.b_cur)
                    p_dat_charge = MDY(rashod.paramcalc.cur_mm, 28, rashod.paramcalc.cur_yy);
                GetNewNormsForODN(conn_db, rashod, p_dat_charge, out ret);
            }
            //Старые нормативы ОДН для начислений ЖКУ
            else
            {
                if (Points.IsSmr)
                {
                    #region Для Самары - норматив ОДН по услугам

                    // Для Самары

                    // проставить домовые параметры для расчета норматива ОДН - пока только электроэнергия!
                    ret = ExecSQL(conn_db,
                        " update ttt_aid_virt0 set (squ_dom,squ_mop,nom_str)= " +
#if PG
 "((Select max( case when nzp_prm=  40 then coalesce(val_prm,'0')::numeric else 0 end ) as squ From ttt_prm_2 p " +
                        " Where ttt_aid_virt0.nzp_dom = p.nzp " +
                        "   and p.nzp_prm in (40,2049,2050) " +
                        " )," +
                        "(Select max( case when nzp_prm=2049 then coalesce(val_prm,'0')::numeric else 0 end ) as squ_mop From ttt_prm_2 p " +
                        " Where ttt_aid_virt0.nzp_dom = p.nzp " +
                        "   and p.nzp_prm in (40,2049,2050) " +
                        " )," +
                        "(Select max( case when nzp_prm=2050 then coalesce(val_prm,'0')::numeric else 0 end ) as nom_str" +
                        " From ttt_prm_2 p " +
                        " Where ttt_aid_virt0.nzp_dom = p.nzp " +
                        "   and p.nzp_prm in (40,2049,2050) " +
                        " ))" +
#else
                    "((Select " +
                    "   max( case when nzp_prm=  40 then nvl(val_prm,'0')+0 else 0 end ) as squ," +
                    "   max( case when nzp_prm=2049 then nvl(val_prm,'0')+0 else 0 end ) as squ_mop," +
                    "   max( case when nzp_prm=2050 then nvl(val_prm,'0')+0 else 0 end ) as nom_str" +
                    " From ttt_prm_2 p " +
                    " Where ttt_aid_virt0.nzp_dom = p.nzp " +
                    "   and p.nzp_prm in (40,2049,2050) " +
                    " ))"+
#endif
 " where 1=1 "
                        , true);
                    if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                    // ОДН есть если есть площадь МОП = общая площадь дома - сумма площадей ЛС
                    ret = ExecSQL(conn_db,
                          " update ttt_aid_virt0 set is_odn=1,squ1=squ_dom-squ_mop where squ_dom-squ_mop>0.000001 "
                        , true);
                    if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                    ExecSQL(conn_db, sUpdStat + " ttt_aid_virt0 ", true);
                    // норматив ОДН по электроснабжению
                    ret = ExecSQL(conn_db,
                          " Update ttt_aid_virt0 set norm_odn = (select r.value from " + rashod.paramcalc.kernel_alias + "res_values r " +
                             "   where r.nzp_res=3010 and r.nzp_y=ttt_aid_virt0.nom_str and r.nzp_x=1 )" + sConvToNum +
                          " Where is_odn=1 and nzp_serv=25 and 0<(select count(*) from " + rashod.paramcalc.kernel_alias + "res_values r " +
                             "   where r.nzp_res=3010 and r.nzp_y=ttt_aid_virt0.nom_str and r.nzp_x=1 ) "
                        , true);
                    if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                    // норматив ОДН по ХВС и ГВС
                    ret = ExecRead(conn_db, out reader,
                    " Select val_prm" + sConvToInt + " val_prm From " + rashod.paramcalc.data_alias + "prm_13 " +
                    " Where nzp_prm = 185 and is_actual <> 100 and dat_s  <= " + rashod.paramcalc.dat_po + " and dat_po >= " + rashod.paramcalc.dat_s
                    , true);
                    if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                    int iNzpRes = 0;
                    if (reader.Read())
                    {
                        iNzpRes = Convert.ToInt32(reader["val_prm"]);
                    }
                    reader.Close();

                    if (iNzpRes != 0)
                    {
                        string sql =
                        " Update ttt_aid_virt0 set norm_odn = (select r.value from " + rashod.paramcalc.kernel_alias + "res_values r " +
                        "   where r.nzp_res=" + iNzpRes + " and r.nzp_y=1 and r.nzp_x=(case when nzp_serv=6 then 1 else 2 end) )" + sConvToNum +
                        " Where is_odn=1 and nzp_serv in (6,9) and 0<(select count(*) from " + rashod.paramcalc.kernel_alias + "res_values r " +
                        "   where r.nzp_res=" + iNzpRes + " and r.nzp_y=1 and r.nzp_x=(case when nzp_serv=6 then 1 else 2 end) ) ";
                        ret = ExecSQL(conn_db, sql, true);
                        if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }
                    }

                    #endregion Для Самары - норматив ОДН по услугам
                }
                else
                {
                    #region Для РТ - норматив ОДН по услугам

                    // Для РТ

                    // проставить домовые параметры для расчета норматива ОДН
                    ret = ExecSQL(conn_db,
                        " update ttt_aid_virt0 set (etag,squ_dom,dat_post,is_cgvs,is_hvobor,is_liftobor,is_obor, is_gvobor,is_otobor,is_zapir,is_anten,is_ppa,is_liftonly)=( " +
#if PG
 "coalesce((Select max( replace( val_prm, ',', '.')" + sConvToInt + ") as etg From ttt_prm_2 p " +
                          " Where ttt_aid_virt0.nzp_dom = p.nzp and p.nzp_prm=37 ),0)," +
                          "coalesce((Select max( replace( val_prm, ',', '.')" + sConvToNum + ") as squ From ttt_prm_2 p " +
                          " Where ttt_aid_virt0.nzp_dom = p.nzp and p.nzp_prm=40 ),0)," +

                          " cast(coalesce((Select max( replace( val_prm, ',', '.'))  as dpost From ttt_prm_2 p " +
                          " Where ttt_aid_virt0.nzp_dom = p.nzp  and p.nzp_prm=150 ),'01.01.1900') as date)," +

                          "coalesce((Select max( replace( val_prm, ',', '.')" + sConvToInt + ") as is_cgvs     From ttt_prm_2 p " +
                          " Where ttt_aid_virt0.nzp_dom = p.nzp and p.nzp_prm=27   ),0)," +
                          "coalesce((Select max( replace( val_prm, ',', '.')" + sConvToInt + ") as is_hvobor   From ttt_prm_2 p " +
                          " Where ttt_aid_virt0.nzp_dom = p.nzp and p.nzp_prm=1080 ),0)," +
                          "coalesce((Select max( replace( val_prm, ',', '.')" + sConvToInt + ") as is_liftobor From ttt_prm_2 p " +
                          " Where ttt_aid_virt0.nzp_dom = p.nzp and p.nzp_prm=1081 ),0)," +
                          "coalesce((Select max( replace( val_prm, ',', '.')" + sConvToInt + ") as is_obor     From ttt_prm_2 p " +
                          " Where ttt_aid_virt0.nzp_dom = p.nzp and p.nzp_prm=1082 ),0)," +
                          "coalesce((Select max( replace( val_prm, ',', '.')" + sConvToInt + ") as is_gvobor   From ttt_prm_2 p " +
                          " Where ttt_aid_virt0.nzp_dom = p.nzp and p.nzp_prm=1341 ),0)," +
                          "coalesce((Select max( replace( val_prm, ',', '.')" + sConvToInt + ") as is_otobor   From ttt_prm_2 p " +
                          " Where ttt_aid_virt0.nzp_dom = p.nzp and p.nzp_prm=1342 ),0)," +
                          "coalesce((Select max( replace( val_prm, ',', '.')" + sConvToInt + ") as is_zapir    From ttt_prm_2 p " +
                          " Where ttt_aid_virt0.nzp_dom = p.nzp and p.nzp_prm=1343 ),0)," +
                          "coalesce((Select max( replace( val_prm, ',', '.')" + sConvToInt + ") as is_anten    From ttt_prm_2 p " +
                          " Where ttt_aid_virt0.nzp_dom = p.nzp and p.nzp_prm=1344 ),0)," +
                          "coalesce((Select max( replace( val_prm, ',', '.')" + sConvToInt + ") as is_ppa      From ttt_prm_2 p " +
                          " Where ttt_aid_virt0.nzp_dom = p.nzp and p.nzp_prm=1345 ),0)," +
                          "coalesce((Select max( replace( val_prm, ',', '.')" + sConvToInt + ") as is_liftonly From ttt_prm_2 p " +
                          " Where ttt_aid_virt0.nzp_dom = p.nzp and p.nzp_prm=1417 ),0) " +
#else
                    "(Select " +
                      "   nvl(max( case when nzp_prm=  37 then replace( val_prm, ',', '.')+0 else 0 end ),0) as etg," +
                      "   nvl(max( case when nzp_prm=  40 then replace( val_prm, ',', '.')+0 else 0 end ),0) as squ," +
                      "   nvl(max( case when nzp_prm= 150 then replace( val_prm, ',', '.')  else '' end ),0) as dpost," +
                      "   nvl(max( case when nzp_prm=  27 then replace( val_prm, ',', '.')+0 else 0 end ),0) as is_cgvs," +
                      "   nvl(max( case when nzp_prm=1080 then replace( val_prm, ',', '.')+0 else 0 end ),0) as is_hvobor," +
                      "   nvl(max( case when nzp_prm=1081 then replace( val_prm, ',', '.')+0 else 0 end ),0) as is_liftobor," +
                      "   nvl(max( case when nzp_prm=1082 then replace( val_prm, ',', '.')+0 else 0 end ),0) as is_obor," +
                      "   nvl(max( case when nzp_prm=1341 then replace( val_prm, ',', '.')+0 else 0 end ),0) as is_gvobor," +
                      "   nvl(max( case when nzp_prm=1342 then replace( val_prm, ',', '.')+0 else 0 end ),0) as is_otobor," +
                      "   nvl(max( case when nzp_prm=1343 then replace( val_prm, ',', '.')+0 else 0 end ),0) as is_zapir," +
                      "   nvl(max( case when nzp_prm=1344 then replace( val_prm, ',', '.')+0 else 0 end ),0) as is_anten," +
                      "   nvl(max( case when nzp_prm=1345 then replace( val_prm, ',', '.')+0 else 0 end ),0) as is_ppa," +
                      "   nvl(max( case when nzp_prm=1417 then replace( val_prm, ',', '.')+0 else 0 end ),0) as is_liftonly" +
                      " From ttt_prm_2 p " +
                      " Where ttt_aid_virt0.nzp_dom = p.nzp " +
                      "   and p.nzp_prm in (27,37,40,150,1080,1081,1082,1341,1342,1343,1344,1345,1417) " +
                    " ) "+
#endif
 ") where 1=1 "
                        , true);
                    if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                    // площадь МОП = параметр дома "Площадь МОП..."

                    //2465|Если нет Площади МОП на ОДН по услугам - НЕ использовать для дома|||bool||5||||
                    bool bUchetMopODN =
                        CheckValBoolPrmWithVal(conn_db, rashod.paramcalc.data_alias, 2465, "5", "1", rashod.paramcalc.dat_s, rashod.paramcalc.dat_s);

                    string ssql = "1 = 1";
                    if (bUchetMopODN)
                    {
                        ssql = "nzp_serv = 25";
                    }
                    // ... для эл/энергии
                    ret = ExecSQL(conn_db,
                        " update ttt_aid_virt0 set is_odn=1, squ_mop = " +
                        " (Select max( replace( " + sNvlWord + "(val_prm,'0'), ',', '.')" + sConvToNum + " ) as squ " +
                        " From ttt_prm_2 p " +
                        " Where ttt_aid_virt0.nzp_dom = p.nzp and p.nzp_prm=2049 " +
                        " )" +
                        " where " + ssql +
                        " and exists (Select 1 From ttt_prm_2 p Where ttt_aid_virt0.nzp_dom = p.nzp and p.nzp_prm=2049 ) "
                        , true);
                    if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }
                    // ... для ХВС
                    ret = ExecSQL(conn_db,
                        " update ttt_aid_virt0 set is_odn=1, squ_mop = " +
                        " (Select max( replace( " + sNvlWord + "(val_prm,'0'), ',', '.')" + sConvToNum + " ) as squ " +
                        " From ttt_prm_2 p " +
                        " Where ttt_aid_virt0.nzp_dom = p.nzp and p.nzp_prm=2474 " +
                        " ) " +
                        "where nzp_serv = 6 " +
                        " and exists (Select 1 From ttt_prm_2 p Where ttt_aid_virt0.nzp_dom = p.nzp and p.nzp_prm=2474 ) "
                        , true);
                    if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }
                    // ... для ГВС
                    ret = ExecSQL(conn_db,
                        " update ttt_aid_virt0 set is_odn=1, squ_mop = " +
                        " (Select max( replace( " + sNvlWord + "(val_prm,'0'), ',', '.')" + sConvToNum + " ) as squ " +
                        " From ttt_prm_2 p " +
                        " Where ttt_aid_virt0.nzp_dom = p.nzp and p.nzp_prm=2475 " +
                        " ) " +
                        " where nzp_serv = 9 " +
                        " and exists (Select 1 From ttt_prm_2 p Where ttt_aid_virt0.nzp_dom = p.nzp and p.nzp_prm=2475 ) "
                        , true);
                    if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                    ViewTbl(conn_db, " select * from ttt_aid_virt0 ");

                    // ОДН есть если есть площадь МОП = общая площадь дома - сумма площадей ЛС и НЕ был установлен параметр дома "Площадь МОП..."
                    ret = ExecSQL(conn_db,
                        " update ttt_aid_virt0 set is_odn=1, squ_mop = squ_dom - squ1 where squ_dom-squ1>0.000001 and is_odn=0 "
                        , true);
                    if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                    ExecSQL(conn_db, sUpdStat + " ttt_aid_virt0 ", true);

                    // 1982,'Норматив ОДН ХВС(Приказ №47) для Тулы','bool' ,Null,5,Null,Null,Null);
                    bool bNormODN6tula =
                        CheckValBoolPrmWithVal(conn_db, rashod.paramcalc.data_alias, 1982, "5", "1", rashod.paramcalc.dat_s, rashod.paramcalc.dat_po);

                    string sNumColGVS = "";
                    if (bNormODN6tula)
                    {
                        // Для Тулы - от ЦГВС
                        // nzp_x=1 для ХВС
                        // nzp_x=2 для ГВС
                        ret = ExecSQL(conn_db,
                            " update ttt_aid_virt0 set nom_kol=2 where is_odn=1 and nzp_serv in (6,9) and is_cgvs=0 "
                            , true);
                        if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }
                        sNumColGVS = "ttt_aid_virt0. nom_kol";

                        ret = ExecSQL(conn_db,
                            " update ttt_aid_virt0 set nom_str = " +
                              sNvlWord + "(case" +
                                " when nzp_serv=6 then " +
                                   "(Select max(val_prm)" + sConvToInt + " From ttt_prm_2 p  Where ttt_aid_virt0.nzp_dom = p.nzp and p.nzp_prm=1981 )" +
                                " when nzp_serv=9 then " +
                                   "(Select max(val_prm)" + sConvToInt + " From ttt_prm_2 p  Where ttt_aid_virt0.nzp_dom = p.nzp and p.nzp_prm=1980 ) " +
                              " end,0) " +
                            " where is_odn=1 and nzp_serv in (6,9) "
                            , true);
                        if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }
                    }
                    else
                    {
                        // Для РТ
                        // nzp_x=1 для ХВС
                        // nzp_x=2 для ГВС
                        ret = ExecSQL(conn_db,
                            " update ttt_aid_virt0 set nom_kol=2 where is_odn=1 and nzp_serv=9 "
                            , true);
                        if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }
                        sNumColGVS = "2";

                        ret = ExecSQL(conn_db,
                            " update ttt_aid_virt0 set nom_str = " +
                            "   case when etag<=0" +
                                   " then 1" +
                                   " else (case when etag>10 then 10 else etag end) " +
                            "   end " +
                            " where is_odn=1 and nzp_serv in (6,9) "
                            , true);
                        if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }
                    }

                    ExecSQL(conn_db, sUpdStat + " ttt_aid_virt0 ", true);

                    // для Отопления: nzp_x=1  для даты постройки < 1999; nzp_x=2  для даты постройки >= 1999
                    ret = ExecSQL(conn_db,
                        " update ttt_aid_virt0 set nom_kol=2 where is_odn=1 and nzp_serv=8 and dat_post>= cast ('01.01.1999' as date) "
                        , true);
                    if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                    // таблица нормативов ОДН для ЭЭ
                    ret = ExecRead(conn_db, out reader,
                    " Select val_prm" + sConvToInt + " val_prm From " + rashod.paramcalc.data_alias + "prm_13 " +
                    " Where nzp_prm = 184 and is_actual <> 100 and dat_s  <= " + rashod.paramcalc.dat_po + " and dat_po >= " + rashod.paramcalc.dat_s
                    , true);
                    if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                    int iNzpRes25 = 0;
                    if (reader.Read())
                    {
                        iNzpRes25 = Convert.ToInt32(reader["val_prm"]);
                    }
                    reader.Close();

                    //1985|Норматив ОДН для Тулы|||bool||5||||
                    bool bNormODN25tula =
                        CheckValBoolPrm(conn_db, rashod.paramcalc.data_alias, 1985, "5");

                    int iKolEtag25;
                    if (bNormODN25tula)
                    {
                        iKolEtag25 = 12;
                    }
                    else
                    {
                        iKolEtag25 = 10;
                    }

                    // для Отопления: nzp_y(этаж)<=16 для остальных услуг nzp_y<=10
                    ret = ExecSQL(conn_db,
                        " update ttt_aid_virt0 set nom_str = " +
                        "   case when etag<=0 then 1 else " +
                        "     case when nzp_serv=8" +
                        "       then (case when etag>16 then 16 else etag end) " +
                        "       else (" +
                                    " case when nzp_serv=25" +
                                    " then " +
                                      " (case when etag>" + iKolEtag25 + " then " + iKolEtag25 + " else etag end) " +
                                    " else " +
                                      " (case when etag>10 then 10 else etag end) " +
                                    " end" +
                                    ") " +
                        "     end " +
                        "   end " +
                        " where is_odn=1 and nzp_serv not in (6,9) "
                        , true);
                    if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                    if (bNormODN25tula)
                    {
                        // ... Для Тулы норматив ОДН для ЭЭ другой!
                        ret = ExecSQL(conn_db,
                            " update ttt_aid_virt0 set nom_kol= 1" +
                            " + (case when is_liftobor=1 then   2 else 0 end) " +
                            " + (case when is_hvobor  =1 then   4 else 0 end) " +
                            " + (case when is_gvobor  =1 then   8 else 0 end) " +
                            " + (case when is_otobor  =1 then  16 else 0 end) " +
                            " + (case when is_zapir   =1 then  32 else 0 end) " +
                            " + (case when is_anten   =1 then  64 else 0 end) " +
                            " + (case when is_ppa     =1 then 128 else 0 end) " +
                            " + (case when is_liftonly=1 then 256 else 0 end) " +
                            " where is_odn=1 and nzp_serv=25 "
                            , true);
                        if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                        // проставить норматив ОДН
                        ret = ExecSQL(conn_db,
                            " update ttt_aid_virt0" +
                            " set norm_odn=" +
                            sNvlWord + "( (select r.value" + sConvToNum + " from " + rashod.paramcalc.kernel_alias + "res_values r where r.nzp_res= " + iNzpRes25 + " and r.nzp_y=ttt_aid_virt0.nom_str and r.nzp_x=1 ) ,0)" +

                            " + (case when is_liftobor=1 then " +
                            sNvlWord + "( (select r.value" + sConvToNum + " from " + rashod.paramcalc.kernel_alias + "res_values r where r.nzp_res= " + iNzpRes25 + " and r.nzp_y=ttt_aid_virt0.nom_str and r.nzp_x=2 ) ,0)" +
                            " else 0 end) " +
                            " + (case when is_hvobor=1 then " +
                            sNvlWord + "( (select r.value" + sConvToNum + " from " + rashod.paramcalc.kernel_alias + "res_values r where r.nzp_res= " + iNzpRes25 + " and r.nzp_y=ttt_aid_virt0.nom_str and r.nzp_x=3 ) ,0)" +
                            " else 0 end) " +
                            " + (case when is_gvobor=1 then " +
                            sNvlWord + "( (select r.value" + sConvToNum + " from " + rashod.paramcalc.kernel_alias + "res_values r where r.nzp_res= " + iNzpRes25 + " and r.nzp_y=ttt_aid_virt0.nom_str and r.nzp_x=4 ) ,0)" +
                            " else 0 end) " +
                            " + (case when is_otobor=1 then " +
                            sNvlWord + "( (select r.value" + sConvToNum + " from " + rashod.paramcalc.kernel_alias + "res_values r where r.nzp_res= " + iNzpRes25 + " and r.nzp_y=ttt_aid_virt0.nom_str and r.nzp_x=5 ) ,0)" +
                            " else 0 end) " +
                            " + (case when is_zapir=1 and is_liftonly=0 then " +
                            sNvlWord + "( (select r.value" + sConvToNum + " from " + rashod.paramcalc.kernel_alias + "res_values r where r.nzp_res= " + iNzpRes25 + " and r.nzp_y=ttt_aid_virt0.nom_str and r.nzp_x=6 ) ,0)" +
                            " else 0 end) " +
                            " + (case when is_zapir=1 and is_liftonly=1 then " +
                            sNvlWord + "( (select r.value" + sConvToNum + " from " + rashod.paramcalc.kernel_alias + "res_values r where r.nzp_res= " + iNzpRes25 + " and r.nzp_y=ttt_aid_virt0.nom_str and r.nzp_x=7 ) ,0)" +
                            " else 0 end) " +
                            " + (case when is_anten=1 and is_liftonly=0 then " +
                            sNvlWord + "( (select r.value" + sConvToNum + " from " + rashod.paramcalc.kernel_alias + "res_values r where r.nzp_res= " + iNzpRes25 + " and r.nzp_y=ttt_aid_virt0.nom_str and r.nzp_x=8 ) ,0)" +
                            " else 0 end) " +
                            " + (case when is_anten=1 and is_liftonly=1 then " +
                            sNvlWord + "( (select r.value" + sConvToNum + " from " + rashod.paramcalc.kernel_alias + "res_values r where r.nzp_res= " + iNzpRes25 + " and r.nzp_y=ttt_aid_virt0.nom_str and r.nzp_x=9 ) ,0)" +
                            " else 0 end) " +
                            " + (case when is_ppa=1 then " +
                            sNvlWord + "( (select r.value" + sConvToNum + " from " + rashod.paramcalc.kernel_alias + "res_values r where r.nzp_res= " + iNzpRes25 + " and r.nzp_y=ttt_aid_virt0.nom_str and r.nzp_x=10) ,0)" +
                            " else 0 end) " +
                            " + (case when is_liftonly=1 then " +
                            sNvlWord + "( (select r.value" + sConvToNum + " from " + rashod.paramcalc.kernel_alias + "res_values r where r.nzp_res= " + iNzpRes25 + " and r.nzp_y=ttt_aid_virt0.nom_str and r.nzp_x=11) ,0)" +
                            " else 0 end) " +

                            " where is_odn=1 and nzp_serv=25 "
                            , true);
                        if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }
                    }
                    else
                    {
                        // для РТ Эл/эн: nzp_x=1..5  по лифт.оборуд./ХВ обруд./Оборуд.для э/э
                        ret = ExecSQL(conn_db,
                            " update ttt_aid_virt0 set nom_kol= " +
                            " case when is_hvobor+is_liftobor=2 then 5 else " +
                            "   case when is_hvobor+is_liftobor=1" +
                            "     then " +
                            "       case when is_hvobor=1 then 3 else 4 end " +
                            "     else " +
                            "       case when is_obor  =1 then 2 else 1 end " +
                            "   end " +
                            " end " +
                            " where is_odn=1 and nzp_serv=25 "
                            , true);
                        if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }


                        // проставить норматив ОДН
                        ret = ExecSQL(conn_db,
                            " update ttt_aid_virt0" +
                            " set norm_odn=" + sNvlWord + "(" +

                            " (select r.value" + sConvToNum +
                            " from " + rashod.paramcalc.kernel_alias + "res_values r " +
                            " where r.nzp_res= " + iNzpRes25 +
                            " and r.nzp_y=ttt_aid_virt0.nom_str and r.nzp_x=ttt_aid_virt0.nom_kol ) " +

                            ",0)" +
                            " where is_odn=1 and nzp_serv=25 "
                            , true);
                        if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }
                    }

                    // проставить норматив ОДН
                    ret = ExecSQL(conn_db,
                        " update ttt_aid_virt0" +
                        " set norm_odn=" + sNvlWord + "(" +

                        " (select r.value" + sConvToNum +
                        " from " + rashod.paramcalc.kernel_alias + "res_values r " +
                        " where r.nzp_res= " +
                        "  (select max(val_prm)" + sConvToNum +
                        "   from " + rashod.paramcalc.data_alias + "prm_13 p " +
                        "   Where p.nzp_prm=" +
                        "    (case when ttt_aid_virt0.nzp_serv in (6,9) then 185 else" +
                        "       case when ttt_aid_virt0.nzp_serv=8 then 186 else 184 end " +
                        "     end) " +
                        "    and p.is_actual <> 100 " +
                        "    and p.dat_s  <= " + rashod.paramcalc.dat_po +
                        "    and p.dat_po >= " + rashod.paramcalc.dat_s +
                        "  )" +
                        " and r.nzp_y=ttt_aid_virt0.nom_str and r.nzp_x=ttt_aid_virt0.nom_kol ) " +

                        ",0)" +
                        " where is_odn=1 and nzp_serv<>25 "
                        , true);
                    if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                    // для i21=0 и nzp_serv=6 добавить норматив ОДН на ГВС - nzp_type_alg=26
                    ret = ExecSQL(conn_db,
                        " update ttt_aid_virt0" +
                        " set norm_odn= norm_odn + " + sNvlWord + "(" +
                        "(select r.value" + sConvToNum + " from " +
                        rashod.paramcalc.kernel_alias + "res_values r " +
                        " where r.nzp_res= " +
                         "  (select max(val_prm)" + sConvToNum + " from " + rashod.paramcalc.data_alias + "prm_13 p " +
                        "    Where p.nzp_prm=185 " +
                        "    and p.is_actual <> 100 " +
                        "    and p.dat_s  <= " + rashod.paramcalc.dat_po +
                        "    and p.dat_po >= " + rashod.paramcalc.dat_s + " ) " +
                        " and r.nzp_y=ttt_aid_virt0.nom_str and r.nzp_x=" + sNumColGVS + " )" +
                        ",0) " +
                        " where is_odn=1 and nzp_serv=6 and i21=0 "
                        , true);
                    if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                    #endregion Для РТ - норматив ОДН по услугам
                }
            }
            ViewTbl(conn_db, " select * from ttt_aid_virt0 ");

            return true;
        }
        #endregion нормативный расход ОДН по услугам

        #region нормативный расход ОДН по услугам для ГрПУ
        public bool Stek354NormODNGrPU(IDbConnection conn_db, Rashod rashod, out Returns ret)
        //--------------------------------------------------------------------------------
        {
            ret = Utils.InitReturns();

            MyDataReader reader;

            // вставить норматив ОДН для ГрПУ по Пост.354
            ExecSQL(conn_db, " Drop table ttt_aid_virt0 ", false);
            ret = ExecSQL(conn_db,
                  " Create temp table ttt_aid_virt0 " +
                  " ( nzp_dom  integer, " +
                  "   nzp_serv integer, " +
                  "   nzp_counter INTEGER default 0, " +
                  "   cnt_stage integer, " +
                  "   squ_dom  " + sDecimalType + "(15,7) default 0.0000000," +
                  "   squ1     " + sDecimalType + "(15,7) default 0.0000000," +
                  "   squ2     " + sDecimalType + "(15,7) default 0.0000000," +
                  "   squ_mop  " + sDecimalType + "(15,7) default 0.0000000," +
                  "   norm_odn " + sDecimalType + "(15,7) default 0.0000000," +
                  "   dat_post date, " +
                  "   is_hvobor   integer    default 0, " +
                  "   is_liftobor integer    default 0, " +
                  "   is_obor     integer    default 0, " +
                  "   is_gvobor   integer    default 0, " +
                  "   is_otobor   integer    default 0, " +
                  "   is_zapir    integer    default 0, " +
                  "   is_anten    integer    default 0, " +
                  "   is_ppa      integer    default 0, " +
                  "   etag     integer       default 1, " +
                  "   nom_str  integer       default 1, " +
                  "   nom_kol  integer       default 1, " +
                  "   i21      integer       default 0, " +
                  "   is_odn   integer       default 0, " +
                  "   val3     " + sDecimalType + "(15,7) default 0.0000," +
                  "   norm_type_id integer," +
                  "   norm_tables_id integer " + // id норматива - по нему можно получить набор влияющих пар-в и их знач.
                  " ) " + sUnlogTempTable
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            // взять ОДПУ без показаний - из-за Самары рассчитать норматив для всех домов
            ret = ExecSQL(conn_db,
                " Insert into ttt_aid_virt0 (nzp_dom,nzp_serv,nzp_counter,i21,cnt_stage,squ1,squ2) " +
                " Select nzp_dom, nzp_serv, nzp_counter, i21, max(cnt_stage), max(squ1), max(squ1) From ttt_aid_kpu Where 1=1" +
                " group by 1,2,3,4 "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ExecSQL(conn_db, " Create index ix_ttt_aid_virt1 on ttt_aid_virt0 (nzp_dom,nzp_serv,i21) ", true);
            ExecSQL(conn_db, " Create index ix_ttt_aid_virt2 on ttt_aid_virt0 (is_odn,nzp_serv,dat_post) ", true);
            ExecSQL(conn_db, " Create index ix_ttt_aid_virt3 on ttt_aid_virt0 (nzp_counter) ", true);
            ExecSQL(conn_db, sUpdStat + " ttt_aid_virt0 ", true);

            //Новые нормативы ОДН для начислений ЖКУ
            var isNewNormODN =
                   CheckValBoolPrmWithVal(conn_db, rashod.paramcalc.data_alias, 1978, "5", "1", rashod.paramcalc.dat_s, rashod.paramcalc.dat_s);
            if (isNewNormODN)
            {
                string p_dat_charge = DateNullString;
                if (!rashod.paramcalc.b_cur)
                    p_dat_charge = MDY(rashod.paramcalc.cur_mm, 28, rashod.paramcalc.cur_yy);
                GetNewNormsForODNCountersGroup(conn_db, rashod, p_dat_charge, out ret);
            }
            else
            {
                if (Points.IsSmr)
                {
                    #region Для Самары - норматив ОДН по услугам
                    // Для Самары

                    // проставить домовые параметры для расчета норматива ОДН - пока только электроэнергия!
                    ret = ExecSQL(conn_db,
                        " update ttt_aid_virt0 set (squ_dom,squ_mop,nom_str)= " +
                        // общая площадь
                        "(" + sNvlWord + "((Select max( val_prm ) as squ From ttt_prm_17 p" +
                        " Where ttt_aid_virt0.nzp_counter = p.nzp and p.nzp_prm=1152 ),'0')" + sConvToNum + "," +
                        // площадь МОП
                              sNvlWord + "((Select max( val_prm ) as squ_mop From ttt_prm_17 p " +
                        " Where ttt_aid_virt0.nzp_counter = p.nzp and p.nzp_prm=2471 ),'0')" + sConvToNum + "," +
                        // ОДН ЭЭ-Тип норматива
                              sNvlWord + "((Select max( val_prm ) as nom_str From ttt_prm_2 p " +
                        " Where ttt_aid_virt0.nzp_dom = p.nzp and p.nzp_prm=2050 ),'0')" + sConvToNum + ")" +
                        " where 1=1 "
                        , true);
                    if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                    // ОДН есть если есть площадь МОП = общая площадь дома - сумма площадей ЛС
                    ret = ExecSQL(conn_db,
                          " update ttt_aid_virt0 set is_odn=1,squ1=squ_dom-squ_mop where squ_dom-squ_mop>0.000001 "
                        , true);
                    if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                    ExecSQL(conn_db, sUpdStat + " ttt_aid_virt0 ", true);
                    // норматив ОДН по электроснабжению
                    ret = ExecSQL(conn_db,
                        " Update ttt_aid_virt0 set norm_odn = (select r.value from " + rashod.paramcalc.kernel_alias + "res_values r " +
                           "   where r.nzp_res=3010 and r.nzp_y=ttt_aid_virt0.nom_str and r.nzp_x=1 )" + sConvToNum +
                        " Where is_odn=1 and nzp_serv=25 and 0<(select count(*) from " + rashod.paramcalc.kernel_alias + "res_values r " +
                           "   where r.nzp_res=3010 and r.nzp_y=ttt_aid_virt0.nom_str and r.nzp_x=1 ) "
                        , true);
                    if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                    // норматив ОДН по ХВС и ГВС
                    ret = ExecRead(conn_db, out reader,
                    " Select val_prm" + sConvToInt + " val_prm From " + rashod.paramcalc.data_alias + "prm_13 " +
                    " Where nzp_prm = 185 and is_actual <> 100 and dat_s  <= " + rashod.paramcalc.dat_po + " and dat_po >= " + rashod.paramcalc.dat_s
                    , true);
                    if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                    int iNzpRes = 0;
                    if (reader.Read())
                    {
                        iNzpRes = Convert.ToInt32(reader["val_prm"]);
                    }
                    reader.Close();

                    if (iNzpRes != 0)
                    {
                        string sql =
                        " Update ttt_aid_virt0 set norm_odn = (select r.value from " + rashod.paramcalc.kernel_alias + "res_values r " +
                        "   where r.nzp_res=" + iNzpRes + " and r.nzp_y=1 and r.nzp_x=(case when nzp_serv=6 then 1 else 2 end) )" + sConvToNum +
                        " Where is_odn=1 and nzp_serv in (6,9) and 0<(select count(*) from " + rashod.paramcalc.kernel_alias + "res_values r " +
                        "   where r.nzp_res=" + iNzpRes + " and r.nzp_y=1 and r.nzp_x=(case when nzp_serv=6 then 1 else 2 end) ) ";
                        ret = ExecSQL(conn_db, sql, true);
                        if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }
                    }
                    #endregion Для Самары - норматив ОДН по услугам
                }
                else
                {
                    #region Для РТ - норматив ОДН по услугам
                    // Для РТ

                    // проставить домовые параметры для расчета норматива ОДН
                    ret = ExecSQL(conn_db,
                        " update ttt_aid_virt0 set (etag,squ_dom,dat_post,is_hvobor,is_liftobor,is_obor, is_gvobor,is_otobor,is_zapir,is_anten,is_ppa)= " +
                        // количество этажей
                        "(" + sNvlWord + "((Select max( replace( val_prm, ',', '.')" + sConvToInt + " ) as etg From ttt_prm_17 p " +
                          " Where ttt_aid_virt0.nzp_counter = p.nzp and p.nzp_prm=1157 ),'0')," +
                        // общая площадь
                              sNvlWord + "((Select max( replace( val_prm, ',', '.')" + sConvToNum + " ) as squ From ttt_prm_17 p " +
                          " Where ttt_aid_virt0.nzp_counter = p.nzp and p.nzp_prm=1152 ),'0')," +
                        // дата постройки дома
                          " cast(" + sNvlWord + "((Select max( replace( val_prm, ',', '.') ) as dpost From ttt_prm_2 p " +
                          " Where ttt_aid_virt0.nzp_dom = p.nzp and p.nzp_prm=150 ),'01.01.1900') as date)," +
                        // Дом(электроснабжение) с насосным оборудованием ХВС и ГВС
                              sNvlWord + "((Select max( replace( val_prm, ',', '.')" + sConvToInt + " ) as is_hvobor From ttt_prm_2 p " +
                          " Where ttt_aid_virt0.nzp_dom = p.nzp and p.nzp_prm=1080 ),'0')," +
                        // Дом(электроснабжение) с силовым оборудованием лифтов
                              sNvlWord + "((Select max( replace( val_prm, ',', '.')" + sConvToInt + " ) as liftobor From ttt_prm_2 p " +
                          " Where ttt_aid_virt0.nzp_dom = p.nzp  and p.nzp_prm=1081 ),'0')," +
                        // Дом(электроснабжение) с оборудованием для нужд электроснабжения
                              sNvlWord + "((Select max( replace( val_prm, ',', '.')" + sConvToInt + " ) as is_obor From ttt_prm_2 p " +
                          " Where ttt_aid_virt0.nzp_dom = p.nzp and p.nzp_prm=1082 ),'0')," +

                              sNvlWord + "((Select max( replace( val_prm, ',', '.')" + sConvToInt + " ) as is_gvobor From ttt_prm_2 p " +
                          " Where ttt_aid_virt0.nzp_dom = p.nzp and p.nzp_prm=1341 ),'0')," +
                              sNvlWord + "((Select max( replace( val_prm, ',', '.')" + sConvToInt + " ) as is_otobor From ttt_prm_2 p " +
                          " Where ttt_aid_virt0.nzp_dom = p.nzp and p.nzp_prm=1342 ),'0')," +
                              sNvlWord + "((Select max( replace( val_prm, ',', '.')" + sConvToInt + " ) as is_zapir From ttt_prm_2 p " +
                          " Where ttt_aid_virt0.nzp_dom = p.nzp and p.nzp_prm=1343 ),'0')," +
                              sNvlWord + "((Select max( replace( val_prm, ',', '.')" + sConvToInt + " ) as is_anten From ttt_prm_2 p " +
                          " Where ttt_aid_virt0.nzp_dom = p.nzp and p.nzp_prm=1344 ),'0')," +
                              sNvlWord + "((Select max( replace( val_prm, ',', '.')" + sConvToInt + " ) as is_ppa From ttt_prm_2 p " +
                          " Where ttt_aid_virt0.nzp_dom = p.nzp and p.nzp_prm=1345 ),'0') " +
                        " ) " +
                        " where 1=1 "
                        , true);
                    if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                    // площадь МОП = параметр дома "Площадь МОП..."

                    //2465|Если нет Площади МОП на ОДН по услугам - НЕ использовать для дома|||bool||5||||
                    bool bUchetMopODN =
                        CheckValBoolPrmWithVal(conn_db, rashod.paramcalc.data_alias, 2465, "5", "1", rashod.paramcalc.dat_s, rashod.paramcalc.dat_s);

                    string ssql = "1 = 1";
                    if (bUchetMopODN)
                    {
                        ssql = "nzp_serv = 25";
                    }
                    // ... для эл/энергии - Площадь МОП
                    ret = ExecSQL(conn_db,
                        " update ttt_aid_virt0 set is_odn=1, squ_mop = (" +
                        " Select max( replace( " + sNvlWord + "(val_prm,'0'), ',', '.')" + sConvToNum + " ) as squ " +
                        " From ttt_prm_17 p " +
                        " Where ttt_aid_virt0.nzp_counter = p.nzp and p.nzp_prm=2471 " +
                        " ) " +
                        " where " + ssql +
                        " and 0<(Select count(*) From ttt_prm_17 p Where ttt_aid_virt0.nzp_counter = p.nzp and p.nzp_prm=2471 ) "
                        , true);
                    if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }
                    // ... для ХВС - Площадь МОП
                    ret = ExecSQL(conn_db,
                        " update ttt_aid_virt0 set is_odn=1, squ_mop = (" +
                        " Select max( replace( " + sNvlWord + "(val_prm,'0'), ',', '.')" + sConvToNum + " ) as squ " +
                        " From ttt_prm_17 p " +
                        " Where ttt_aid_virt0.nzp_counter = p.nzp and p.nzp_prm=2472 " +
                        " ) " +
                        " where nzp_serv = 6 " +
                        " and 0<(Select count(*) From ttt_prm_17 p Where ttt_aid_virt0.nzp_counter = p.nzp and p.nzp_prm=2472 ) "
                        , true);
                    if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }
                    // ... для ГВС - Площадь МОП
                    ret = ExecSQL(conn_db,
                        " update ttt_aid_virt0 set is_odn=1, squ_mop = (" +
                        " Select max( replace( " + sNvlWord + "(val_prm,'0'), ',', '.')" + sConvToNum + " ) as squ " +
                        " From ttt_prm_17 p " +
                        " Where ttt_aid_virt0.nzp_counter = p.nzp and p.nzp_prm=2473 " +
                        " ) " +
                        " where nzp_serv = 9 " +
                        " and 0<(Select count(*) From ttt_prm_17 p Where ttt_aid_virt0.nzp_counter = p.nzp and p.nzp_prm=2473 ) "
                        , true);
                    if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                    //ViewTbl(conn_db, " select * from ttt_aid_virt0 ");

                    // ОДН есть если есть площадь МОП = общая площадь дома - сумма площадей ЛС и НЕ был установлен параметр дома "Площадь МОП..."
                    ret = ExecSQL(conn_db,
                        " update ttt_aid_virt0 set is_odn=1, squ_mop = squ_dom - squ1 where squ_dom-squ1>0.000001 and is_odn=0 "
                        , true);
                    if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                    ExecSQL(conn_db, sUpdStat + " ttt_aid_virt0 ", true);
                    // nzp_x=1 для ХВС
                    // nzp_x=2 для ГВС
                    ret = ExecSQL(conn_db,
                        " update ttt_aid_virt0 set nom_kol=2 where is_odn=1 and nzp_serv=9 "
                        , true);
                    if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                    // для Отопления: nzp_x=1  для даты постройки < 1999; nzp_x=2  для даты постройки >= 1999
                    ret = ExecSQL(conn_db,
                        " update ttt_aid_virt0 set nom_kol=2 where is_odn=1 and nzp_serv=8 and dat_post>= cast ('01.01.1999' as date) "
                        , true);
                    if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                    // таблица нормативов ОДН для ЭЭ
                    ret = ExecRead(conn_db, out reader,
                    " Select val_prm" + sConvToInt + " val_prm From " + rashod.paramcalc.data_alias + "prm_13 " +
                    " Where nzp_prm = 184 and is_actual <> 100 and dat_s  <= " + rashod.paramcalc.dat_po + " and dat_po >= " + rashod.paramcalc.dat_s
                    , true);
                    if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                    int iNzpRes25 = 0;
                    if (reader.Read())
                    {
                        iNzpRes25 = Convert.ToInt32(reader["val_prm"]);
                    }
                    reader.Close();

                    //1985|Норматив ОДН для Тулы|||bool||5||||
                    bool bNormODN25tula =
                        CheckValBoolPrm(conn_db, rashod.paramcalc.data_alias, 1985, "5");

                    int iKolEtag25;
                    if (bNormODN25tula)
                    {
                        iKolEtag25 = 12;
                    }
                    else
                    {
                        iKolEtag25 = 10;
                    }

                    // для Отопления: nzp_y(этаж)<=16 для остальных услуг nzp_y<=10
                    ret = ExecSQL(conn_db,
                        " update ttt_aid_virt0 set nom_str = " +
                        "   case when etag<=0 then 1 else " +
                        "     case when nzp_serv=8" +
                        "       then (case when etag>16 then 16 else etag end) " +
                        "       else (" +
                                    " case when nzp_serv=25" +
                                    " then " +
                                      " (case when etag>" + iKolEtag25 + " then " + iKolEtag25 + " else etag end) " +
                                    " else " +
                                      " (case when etag>10 then 10 else etag end) " +
                                    " end" +
                                    ") " +
                        "     end " +
                        "   end " +
                        " where is_odn=1 "
                        , true);
                    if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                    if (bNormODN25tula)
                    {
                        // ... Для Тулы норматив ОДН для ЭЭ другой!
                        // для Эл/эн: nzp_x=1..5  по лифт.оборуд./ХВ обруд./Оборуд.для э/э
                        ret = ExecSQL(conn_db,
                            " update ttt_aid_virt0 set nom_kol= 1" +
                            " + (case when is_liftobor=1 then   2 else 0 end) " +
                            " + (case when is_hvobor  =1 then   4 else 0 end) " +
                            " + (case when is_gvobor  =1 then   8 else 0 end) " +
                            " + (case when is_otobor  =1 then  16 else 0 end) " +
                            " + (case when is_zapir   =1 then  32 else 0 end) " +
                            " + (case when is_anten   =1 then  64 else 0 end) " +
                            " + (case when is_ppa     =1 then 128 else 0 end) " +
                            " where is_odn=1 and nzp_serv=25 "
                            , true);
                        if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                        // проставить норматив ОДН
                        ret = ExecSQL(conn_db,
                            " update ttt_aid_virt0" +
                            " set norm_odn=" +
                            sNvlWord + "( (select r.value" + sConvToNum + " from " + rashod.paramcalc.kernel_alias + "res_values r where r.nzp_res= " + iNzpRes25 + " and r.nzp_y=ttt_aid_virt0.nom_str and r.nzp_x=1 ) ,0)" +

                            " + (case when is_liftobor=1 then " +
                            sNvlWord + "( (select r.value" + sConvToNum + " from " + rashod.paramcalc.kernel_alias + "res_values r where r.nzp_res= " + iNzpRes25 + " and r.nzp_y=ttt_aid_virt0.nom_str and r.nzp_x=2 ) ,0)" +
                            " else 0 end) " +
                            " + (case when is_hvobor=1 then " +
                            sNvlWord + "( (select r.value" + sConvToNum + " from " + rashod.paramcalc.kernel_alias + "res_values r where r.nzp_res= " + iNzpRes25 + " and r.nzp_y=ttt_aid_virt0.nom_str and r.nzp_x=3 ) ,0)" +
                            " else 0 end) " +
                            " + (case when is_gvobor=1 then " +
                            sNvlWord + "( (select r.value" + sConvToNum + " from " + rashod.paramcalc.kernel_alias + "res_values r where r.nzp_res= " + iNzpRes25 + " and r.nzp_y=ttt_aid_virt0.nom_str and r.nzp_x=4 ) ,0)" +
                            " else 0 end) " +
                            " + (case when is_otobor=1 then " +
                            sNvlWord + "( (select r.value" + sConvToNum + " from " + rashod.paramcalc.kernel_alias + "res_values r where r.nzp_res= " + iNzpRes25 + " and r.nzp_y=ttt_aid_virt0.nom_str and r.nzp_x=5 ) ,0)" +
                            " else 0 end) " +
                            " + (case when is_zapir=1 and is_liftobor=0 then " +
                            sNvlWord + "( (select r.value" + sConvToNum + " from " + rashod.paramcalc.kernel_alias + "res_values r where r.nzp_res= " + iNzpRes25 + " and r.nzp_y=ttt_aid_virt0.nom_str and r.nzp_x=6 ) ,0)" +
                            " else 0 end) " +
                            " + (case when is_zapir=1 and is_liftobor=1 then " +
                            sNvlWord + "( (select r.value" + sConvToNum + " from " + rashod.paramcalc.kernel_alias + "res_values r where r.nzp_res= " + iNzpRes25 + " and r.nzp_y=ttt_aid_virt0.nom_str and r.nzp_x=7 ) ,0)" +
                            " else 0 end) " +
                            " + (case when is_anten=1 and is_liftobor=0 then " +
                            sNvlWord + "( (select r.value" + sConvToNum + " from " + rashod.paramcalc.kernel_alias + "res_values r where r.nzp_res= " + iNzpRes25 + " and r.nzp_y=ttt_aid_virt0.nom_str and r.nzp_x=8 ) ,0)" +
                            " else 0 end) " +
                            " + (case when is_anten=1 and is_liftobor=1 then " +
                            sNvlWord + "( (select r.value" + sConvToNum + " from " + rashod.paramcalc.kernel_alias + "res_values r where r.nzp_res= " + iNzpRes25 + " and r.nzp_y=ttt_aid_virt0.nom_str and r.nzp_x=9 ) ,0)" +
                            " else 0 end) " +
                            " + (case when is_ppa=1 then " +
                            sNvlWord + "( (select r.value" + sConvToNum + " from " + rashod.paramcalc.kernel_alias + "res_values r where r.nzp_res= " + iNzpRes25 + " and r.nzp_y=ttt_aid_virt0.nom_str and r.nzp_x=10) ,0)" +
                            " else 0 end) " +

                            " where is_odn=1 and nzp_serv=25 "
                            , true);
                        if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }
                    }
                    else
                    {
                        // для Эл/эн: nzp_x=1..5  по лифт.оборуд./ХВ обруд./Оборуд.для э/э
                        ret = ExecSQL(conn_db,
                            " update ttt_aid_virt0 set nom_kol= " +
                            " case when is_hvobor+is_liftobor=2 then 5 else " +
                            "   case when is_hvobor+is_liftobor=1" +
                            "     then " +
                            "       case when is_hvobor=1 then 3 else 4 end " +
                            "     else " +
                            "       case when is_obor  =1 then 2 else 1 end " +
                            "   end " +
                            " end " +
                            " where is_odn=1 and nzp_serv=25 "
                            , true);
                        if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                        // проставить норматив ОДН
                        ret = ExecSQL(conn_db,
                            " update ttt_aid_virt0" +
                            " set norm_odn=" + sNvlWord + "(" +

                            " (select r.value" + sConvToNum +
                            " from " + rashod.paramcalc.kernel_alias + "res_values r " +
                            " where r.nzp_res= " + iNzpRes25 +
                            " and r.nzp_y=ttt_aid_virt0.nom_str and r.nzp_x=ttt_aid_virt0.nom_kol ) " +

                            ",0)" +
                            " where is_odn=1 and nzp_serv=25 "
                            , true);
                        if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }
                    }

                    // проставить норматив ОДН
                    ret = ExecSQL(conn_db,
                        " update ttt_aid_virt0" +
                        " set norm_odn=" + sNvlWord + "(" +

                        " (select r.value" + sConvToNum +
                        " from " + rashod.paramcalc.kernel_alias + "res_values r " +
                        " where r.nzp_res= " +
                        "  (select max(val_prm)" + sConvToNum +
                        "   from " + rashod.paramcalc.data_alias + "prm_13 p " +
                        "   Where p.nzp_prm=" +
                        "    (case when ttt_aid_virt0.nzp_serv in (6,9) then 185 else" +
                        "       case when ttt_aid_virt0.nzp_serv=8 then 186 else 184 end " +
                        "     end) " +
                        "    and p.is_actual <> 100 " +
                        "    and p.dat_s  <= " + rashod.paramcalc.dat_po +
                        "    and p.dat_po >= " + rashod.paramcalc.dat_s +
                        "  )" +
                        " and r.nzp_y=ttt_aid_virt0.nom_str and r.nzp_x=ttt_aid_virt0.nom_kol ) " +

                        ",0)" +
                        " where is_odn=1 and nzp_serv<>25 "
                        , true);
                    if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                    // для i21=0 и nzp_serv=6 добавить норматив ОДН на ГВС - nzp_type_alg=26
                    ret = ExecSQL(conn_db,
                        " update ttt_aid_virt0" +
                        " set norm_odn= norm_odn + (select " + sNvlWord + "(r.value" + sConvToNum + ",0) from " +
                        rashod.paramcalc.kernel_alias + "res_values r " +
                        " where r.nzp_res= " +
                         "  (select max(val_prm)" + sConvToNum + " from " + rashod.paramcalc.data_alias + "prm_13 p " +
                        "    Where p.nzp_prm=185 " +
                        "    and p.is_actual <> 100 " +
                        "    and p.dat_s  <= " + rashod.paramcalc.dat_po +
                        "    and p.dat_po >= " + rashod.paramcalc.dat_s +
                        "  )" +
                        " and r.nzp_y=ttt_aid_virt0.nom_str and r.nzp_x=2 ) " +
                        " where is_odn=1 and nzp_serv=6 and i21=0 "
                        , true);
                    if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                    #endregion Для РТ - норматив ОДН по услугам
                }
            }
            //ViewTbl(conn_db, " select * from ttt_aid_virt0 ");

            return true;
        }
        #endregion нормативный расход ОДН по услугам для ГрПУ

        #region расчет коэффициентов коррекции расхода на ОДН
        public bool Stek354CalcKoef(IDbConnection conn_db, Rashod rashod, string pType, out Returns ret)
        //--------------------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            var sql = "";
            // рассчитать нормативный расход ОДН по услугам
            ret = ExecSQL(conn_db,
                  " update ttt_aid_virt0 set val3 = squ_mop * norm_odn where is_odn = 1 "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ViewTbl(conn_db, " select * from ttt_aid_virt0 ");
            var where_nzp_counter = "";
            //если Гр.ПУ, то добавляем связку по nzp_counter
            if (pType == "2")
            {
                where_nzp_counter = " and v.nzp_counter=ttt_aid_kpu.nzp_counter";
            }

            // перенести рассчитанный норматив ОДН на дом
            ret = ExecSQL(conn_db,
                  " update ttt_aid_kpu set is_norm=1," +
                  " (val_norm_odn,kf307f9,squ1,squ2,pu7kw,vl210) = ( " +
                  "(select v.val3 from ttt_aid_virt0 v" +
                  " where v.is_odn=1 and v.nzp_dom=ttt_aid_kpu.nzp_dom and v.nzp_serv=ttt_aid_kpu.nzp_serv and v.i21=ttt_aid_kpu.i21 " + where_nzp_counter + ")," +
                  "(select v.squ_dom from ttt_aid_virt0 v" +
                  " where v.is_odn=1 and v.nzp_dom=ttt_aid_kpu.nzp_dom and v.nzp_serv=ttt_aid_kpu.nzp_serv and v.i21=ttt_aid_kpu.i21 " + where_nzp_counter + ")," +
                  "(select v.squ1 from ttt_aid_virt0 v" +
                  " where v.is_odn=1 and v.nzp_dom=ttt_aid_kpu.nzp_dom and v.nzp_serv=ttt_aid_kpu.nzp_serv and v.i21=ttt_aid_kpu.i21 " + where_nzp_counter + ")," +
                  "(select v.squ2 from ttt_aid_virt0 v" +
                  " where v.is_odn=1 and v.nzp_dom=ttt_aid_kpu.nzp_dom and v.nzp_serv=ttt_aid_kpu.nzp_serv and v.i21=ttt_aid_kpu.i21 " + where_nzp_counter + ")," +
                  "(select v.squ_mop from ttt_aid_virt0 v" +
                  " where v.is_odn=1 and v.nzp_dom=ttt_aid_kpu.nzp_dom and v.nzp_serv=ttt_aid_kpu.nzp_serv and v.i21=ttt_aid_kpu.i21 " + where_nzp_counter + ")," +
                  "(select v.norm_odn from ttt_aid_virt0 v" +
                  " where v.is_odn=1 and v.nzp_dom=ttt_aid_kpu.nzp_dom and v.nzp_serv=ttt_aid_kpu.nzp_serv and v.i21=ttt_aid_kpu.i21 " + where_nzp_counter + ") )" +
                  " where exists (select 1 from ttt_aid_virt0 v" +
                  " where v.is_odn=1 and v.nzp_dom=ttt_aid_kpu.nzp_dom and v.nzp_serv=ttt_aid_kpu.nzp_serv and v.i21=ttt_aid_kpu.i21 " + where_nzp_counter + ") "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ViewTbl(conn_db, " select * from ttt_aid_kpu ");

            //сохраняем исходные значения 
            CopySourceVal(conn_db, rashod, out ret, true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            //применяем повышающие коэф-ты
            ApplyUpKoef(conn_db, rashod, out ret, true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            // ОДН есть если есть площадь МОП = общая площадь дома - сумма площадей ЛС 
            // - без ОДПУ - коэф-ты коррекции рассчитаны сразу!
            ret = ExecSQL(conn_db,
                " update ttt_aid_kpu set kod_info=" +
                     " case when nzp_serv=6 and i21=0" +
                     " then 26" +
                     " else " +
                        "(case when (nzp_serv=25 and i21=1) or (nzp_serv in (210,410))" +
                        " then 22 else 21" +
                        " end)" +
                     " end," +
                // ОДН без обрезания нормативом по Пост.344
                " dlt_cur   = val_norm_odn," +
                " kf_dpu_kg = val_norm_odn," +
                // ОДН норматив по Пост.344
                " kf_dpu_ls = val_norm_odn," +
                // домовой расход обрезанный нормативом по Пост.344
                " val3 = val_norm_odn + val1 + val2 + dlt_reval + dlt_real_charge + rash_link," +
                // полный домовой расход без обрезания нормативом по Пост.344
                " val4 = val_norm_odn + val1 + val2 + dlt_reval + dlt_real_charge + rash_link," +
                // норма ОДН на 1 кв.м общей площади - домовой расход обрезанный нормативом по Пост.344
                " kf307  = case when squ1>0.000001 then val_norm_odn / squ1 else 0 end," +
                " kf307n = case when squ1>0.000001 then val_norm_odn / squ1 else 0 end," +
                // норма ОДН на 1 кв.м общей площади - норматив по Пост.344
                " gl7kw  = case when squ1>0.000001 then val_norm_odn / squ1 else 0 end," +
                // норма ОДН на 1 кв.м общей площади - полный домовой расход без обрезания нормативом по Пост.344
                " kf_dpu_plob = case when squ1>0.000001 then val_norm_odn / squ1 else 0 end " +
                " where cnt_stage=0 and is_norm=1 and nzp_serv<>8 "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ViewTbl(conn_db, " select * from ttt_aid_kpu ");

            // - с ОДПУ
            ret = ExecSQL(conn_db,
                " update ttt_aid_kpu set " +
                " dlt_cur   = val3 - (val1 + val2 + rash_link) - (dlt_reval + dlt_real_charge)," +
                " kf_dpu_kg = val3 - (val1 + val2 + rash_link) - (dlt_reval + dlt_real_charge)," +
                " rvirt = 0 + val1 + val2 + rash_link + (dlt_reval + dlt_real_charge)," +
                " val4  = val3 " +
                " where cnt_stage=1 and is_norm=0 " // нет нормы! -> расход = сумме по ЛС
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ret = ExecSQL(conn_db,
                " update ttt_aid_kpu set " +
                " dlt_cur   = val3 - (val1 + val2 + rash_link) - (dlt_reval + dlt_real_charge)," +
                " kf_dpu_kg = val3 - (val1 + val2 + rash_link) - (dlt_reval + dlt_real_charge)," +
                " kf_dpu_ls = val_norm_odn," +
                " rvirt = val_norm_odn + val1 + val2 + rash_link + (dlt_reval + dlt_real_charge)," +
                " val4  = val3," +
                " gl7kw = case when squ1>0.000001 then val_norm_odn / squ1 else 0 end, " +
                " kf_dpu_plob   =" +
                 "( case when (val3 - (val1 + val2 + rash_link) - (dlt_reval + dlt_real_charge))>0.000001" +
                  " then (case when squ1>0.000001" +
                        " then (val3 - (val1 + val2 + rash_link) - (dlt_reval + dlt_real_charge))/squ1 else 0 end)" +
                  " else (case when gil1>0.000001 and (val3 - (val1 + val2 + rash_link) - (dlt_reval + dlt_real_charge))<-0.000001" +
                        " then (val3 - (val1 + val2 + rash_link) - (dlt_reval + dlt_real_charge))/gil1 else 0 end)" +
                  " end )," +
                " val_norm_odn_source = val3 - (val1 + val2 + rash_link) - (dlt_reval + dlt_real_charge) " +
                " where cnt_stage=1 and is_norm=1 "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ViewTbl(conn_db, " select * from ttt_aid_kpu ");

            if ((Convert.ToDateTime("01." + rashod.paramcalc.calc_mm + "." + rashod.paramcalc.calc_yy) >= Convert.ToDateTime("01.06.2013"))
                ) // !!! after'01.06.2013' !!!
            {
                // обрезание расхода ОДПУ на ОДН по Пост344 если нет признака разрешающего превышение!
                // ... для ХВС
                ret = ExecSQL(conn_db,
                    " Update ttt_aid_kpu Set val3 = rvirt " +
                    " Where cnt_stage=1 and nzp_serv = 6 and val3 > rvirt " +
                    " and not exists (Select 1 From ttt_prm_2 p Where ttt_aid_kpu.nzp_dom = p.nzp and p.nzp_prm=1214 ) "
                    , true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }
                // ... для ГВС
                ret = ExecSQL(conn_db,
                    " Update ttt_aid_kpu Set val3 = rvirt " +
                    " Where cnt_stage=1 and nzp_serv = 9 and val3 > rvirt " +
                    " and not exists (Select 1 From ttt_prm_2 p Where ttt_aid_kpu.nzp_dom = p.nzp and p.nzp_prm=1215 ) "
                    , true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }
                // ... для эл/энергии
                ret = ExecSQL(conn_db,
                    " Update ttt_aid_kpu Set val3 = rvirt " +
                    " Where cnt_stage=1 and nzp_serv in (25,210,410) and val3 > rvirt " +
                    " and not exists (Select 1 From ttt_prm_2 p Where ttt_aid_kpu.nzp_dom = p.nzp and p.nzp_prm=1216 ) "
                    , true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }
                // ... для газа
                ret = ExecSQL(conn_db,
                    " Update ttt_aid_kpu Set val3 = rvirt " +
                    " Where cnt_stage=1 and nzp_serv =10 and val3 > rvirt " +
                    " and not exists (Select 1 From ttt_prm_2 p Where ttt_aid_kpu.nzp_dom = p.nzp and p.nzp_prm=1217 ) "
                    , true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }
            }

            ViewTbl(conn_db, " select * from ttt_aid_kpu ");


            //обрезаем значения показаний ОДПУ и ГрПУ для избежания переполнения поля numeric
            ret = ExecSQL(conn_db,
                    " Update ttt_aid_kpu Set " +
                    " val3 = case when val3>" + Constants.max_val_for_pu + " then " + Constants.max_val_for_pu + "  else val3 end, " +
                    " val4 = case when val4>" + Constants.max_val_for_pu + " then " + Constants.max_val_for_pu + "  else val4 end "
            , true);
            if (!ret.result)
            { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            //  проставить коэффициенты коррекции расходов по Пост.№354 для домов с ОДПУ! если нет ОДПУ коэф-ты уже вычислены!
            ret = ExecSQL(conn_db,
                " update ttt_aid_kpu set " +
                " kf307   =( case when (val3 - (val1 + val2 + dlt_reval + dlt_real_charge + rash_link))>0.000001" +
                                " then (case when squ1>0.000001" +
                                      " then (val3 - (val1 + val2 + dlt_reval + dlt_real_charge + rash_link)) / squ1 else 0 end)" +
                                " else (case when gil1>0.000001 and (val3 - (val1 + val2 + dlt_reval + dlt_real_charge + rash_link))<-0.000001" +
                                      " then (val3 - (val1 + val2 + dlt_reval + dlt_real_charge + rash_link)) / gil1 else 0 end)" +
                           " end )," +
                " kf307n  =( case when (val3 - (val1 + val2 + dlt_reval + dlt_real_charge + rash_link))>0.000001" +
                                " then (case when squ1>0.0001" +
                                      " then (val3 - (val1 + val2 + dlt_reval + dlt_real_charge + rash_link)) / squ1 else 0 end)" +
                                " else (case when gil1>0.000001 and (val3 - (val1 + val2 + dlt_reval + dlt_real_charge + rash_link))<-0.000001" +
                                      " then (val3 - (val1 + val2 + dlt_reval + dlt_real_charge + rash_link)) / gil1 else 0 end)" +
                           " end )," +
                " kod_info=" +

                          "( case when (val3 - (val1 + val2 + dlt_reval + dlt_real_charge + rash_link))>0.000001" +
                                " then " +

                                       " case when nzp_serv=6 and i21=0" +
                                       " then 26" +
                                       " else " +
                                          "(case when (nzp_serv=25 and i21=1) or (nzp_serv in (210,410))" +
                                          " then 22 else 21" +
                                          " end)" +
                                       " end" +

                                " else (case when (val3 - (val1 + val2 + dlt_reval + dlt_real_charge + rash_link))<-0.000001" +
                                      " then" +

                                       " case when nzp_serv=6 and i21=0" +
                                       " then 36" +
                                       " else " +
                                          "(case when (nzp_serv=25 and i21=1) or (nzp_serv in (210,410))" +
                                          " then 32 else 31" +
                                          " end)" +
                                       " end" +

                                      " else 0 end)" +
                                " end )" +


                " where cnt_stage=1 and nzp_serv in (6,9,25,10,210,410) "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }
            //
            ret = ExecSQL(conn_db,
                " update ttt_aid_kpu set " +
                " kf307   =( case when abs(val3 - (val1 + val2 + dlt_reval + dlt_real_charge))>0.000001 and squ1>0.000001" +
                                  " then  (val3 - (val1 + val2 + dlt_reval + dlt_real_charge)) / squ1 else 0 end )," +
                " kf307n  =( case when abs(val3 - (val1 + val2 + dlt_reval + dlt_real_charge))>0.000001 and squ1>0.000001" +
                                  " then  (val3 - (val1 + val2 + dlt_reval + dlt_real_charge)) / squ1 else 0 end )," +
                " kod_info=( case when    (val3 - (val1 + val2 + dlt_reval + dlt_real_charge))>0.000001" +
                        " then 23" +
                        " else (case when (val3 - (val1 + val2 + dlt_reval + dlt_real_charge))<-0.000001 then 33 else 0 end)" +
                        " end )" +
                " where cnt_stage=1 and nzp_serv in (8) "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ViewTbl(conn_db, " select * from ttt_aid_kpu ");

            return true;
        }
        #endregion расчет коэффициентов коррекции расхода на ОДН

        #region обработка домов с суммарным начислением ОДН (секционных)
        public bool Stek354LinkDoms(IDbConnection conn_db, Rashod rashod, out Returns ret)
        //--------------------------------------------------------------------------------
        {
            ret = Utils.InitReturns();

            ExecSQL(conn_db, " Drop table ttt_aid_c1 ", false);

            ret = ExecSQL(conn_db,
                " Create temp table ttt_aid_c1 " +
                //" Create table are.ttt_aid_c1 " +
                " ( nzp_dom_base integer," +
                "   nzp_serv     integer," +
                "   kf307     " + sDecimalType + "(15,7) default 0.00," +
                "   kf307n    " + sDecimalType + "(15,7) default 0.00," +
                "   kf307f9   " + sDecimalType + "(15,7) default 0.00," +
                "   kod_info  INTEGER default 0,         " +
                "   cnt_stage INTEGER default 0,         " +
                "   dlt_cur   " + sDecimalType + "(15,7) default 0.0000," +
                "   dlt_reval " + sDecimalType + "(15,7) default 0.0000," +
                "   dlt_real_charge " + sDecimalType + "(12,4) default 0.0000," +
                "   squ1 " + sDecimalType + "(15,7) default 0.0000, " +
                "   gil1 " + sDecimalType + "(15,7) default 0.0000, " +
                "   val1 " + sDecimalType + "(15,7) default 0.0000," +
                "   val2 " + sDecimalType + "(15,7) default 0.0000," +
                "   val3 " + sDecimalType + "(15,7) default 0.0000," +
                "   val4 " + sDecimalType + "(15,7) default 0.0000," +
                "   rvirt " + sDecimalType + "(15,7) default 0.0000," +
                "   gl7kw " + sDecimalType + "(15,7) default 0.0000000,      " +
                "   kf_dpu_kg " + sDecimalType + "(15,7) default 0.0000000,  " +
                "   kf_dpu_plob " + sDecimalType + "(15,7) default 0.0000000," +
                "   kf_dpu_ls " + sDecimalType + "(15,7) default 0.0000000,  " +

                "   i21      INTEGER default 0," +
                "   rash_link " + sDecimalType + "(15,7) default 0.0000000   " +
                " )  " + sUnlogTempTable
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            // hvs (помним! - по ХВ есть дублирование по домам 21 и 22 типы)
            ret = ExecSQL(conn_db,
                " insert into ttt_aid_c1" +
                " (nzp_dom_base,i21,nzp_serv,squ1,gil1,val1,val2,val3,val4,dlt_reval,dlt_real_charge,kf_dpu_kg,kf_dpu_ls,rvirt,cnt_stage,rash_link) " +
                " select l.nzp_dom_base,d.i21, 6 nzp_serv,sum(d.squ1),sum(d.gil1),sum(d.val1),sum(d.val2),sum(d.val3),sum(d.val4)," +
                " sum(d.dlt_reval),sum(d.dlt_real_charge),sum(d.kf_dpu_kg),sum(d.kf_dpu_ls),sum(d.rvirt),max(d.cnt_stage),sum(rash_link) " +
                " from " + rashod.paramcalc.data_alias + "link_dom_lit l, ttt_aid_kpu d " +
                " where d.nzp_serv= 6 and l.nzp_dom=d.nzp_dom and l.nzp_dom_base in " +
                  "(select b.nzp_dom_base" +
                  " from " + rashod.paramcalc.data_alias + "link_dom_lit b, ttt_prm_2 p " +
                  " where b.nzp_dom=p.nzp and p.nzp_prm=2069 and p.val_prm='1') " +
                " group by 1,2 "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            // gvs
            ret = ExecSQL(conn_db,
                " insert into ttt_aid_c1" +
                " (nzp_dom_base,nzp_serv,squ1,gil1,val1,val2,val3,val4,dlt_reval,dlt_real_charge,kf_dpu_kg,kf_dpu_ls,rvirt,cnt_stage,rash_link) " +
                " select l.nzp_dom_base, 9 nzp_serv,sum(d.squ1),sum(d.gil1),sum(d.val1),sum(d.val2),sum(d.val3),sum(d.val4)," +
                " sum(d.dlt_reval),sum(d.dlt_real_charge),sum(d.kf_dpu_kg),sum(d.kf_dpu_ls),sum(d.rvirt),max(d.cnt_stage),sum(rash_link) " +
                " from " + rashod.paramcalc.data_alias + "link_dom_lit l, ttt_aid_kpu d " +
                " where d.nzp_serv= 9 and l.nzp_dom=d.nzp_dom and l.nzp_dom_base in " +
                  "(select b.nzp_dom_base" +
                  " from " + rashod.paramcalc.data_alias + "link_dom_lit b, ttt_prm_2 p " +
                  " where b.nzp_dom=p.nzp and p.nzp_prm=2070 and p.val_prm='1') " +
                " group by 1 "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            // ee (помним! - по ЭЭ есть дублирование по домам 21 и 22 типы)
            ret = ExecSQL(conn_db,
                " insert into ttt_aid_c1" +
                " (nzp_dom_base,i21,nzp_serv,squ1,gil1,val1,val2,val3,val4,dlt_reval,dlt_real_charge,kf_dpu_kg,kf_dpu_ls,rvirt,cnt_stage,rash_link) " +
                " select l.nzp_dom_base,d.i21,25 nzp_serv,sum(d.squ1),sum(d.gil1),sum(d.val1),sum(d.val2),sum(d.val3),sum(d.val4)," +
                " sum(d.dlt_reval),sum(d.dlt_real_charge),sum(d.kf_dpu_kg),sum(d.kf_dpu_ls),sum(d.rvirt),max(d.cnt_stage),sum(rash_link) " +
                " from " + rashod.paramcalc.data_alias + "link_dom_lit l, ttt_aid_kpu d " +
                " where d.nzp_serv=25 and l.nzp_dom=d.nzp_dom and l.nzp_dom_base in " +
                  "(select b.nzp_dom_base" +
                  " from " + rashod.paramcalc.data_alias + "link_dom_lit b, ttt_prm_2 p " +
                  " where b.nzp_dom=p.nzp and p.nzp_prm=2071 and p.val_prm='1') " +
                " group by 1,2 "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            // gas
            ret = ExecSQL(conn_db,
                " insert into ttt_aid_c1" +
                " (nzp_dom_base,nzp_serv,squ1,gil1,val1,val2,val3,val4,dlt_reval,dlt_real_charge,kf_dpu_kg,kf_dpu_ls,rvirt,cnt_stage,rash_link) " +
                " select l.nzp_dom_base,10 nzp_serv,sum(d.squ1),sum(d.gil1),sum(d.val1),sum(d.val2),sum(d.val3),sum(d.val4)," +
                " sum(d.dlt_reval),sum(d.dlt_real_charge),sum(d.kf_dpu_kg),sum(d.kf_dpu_ls),sum(d.rvirt),max(d.cnt_stage),sum(rash_link) " +
                " from " + rashod.paramcalc.data_alias + "link_dom_lit l, ttt_aid_kpu d " +
                " where d.nzp_serv=10 and l.nzp_dom=d.nzp_dom and l.nzp_dom_base in " +
                  "(select b.nzp_dom_base" +
                  " from " + rashod.paramcalc.data_alias + "link_dom_lit b, ttt_prm_2 p " +
                  " where b.nzp_dom=p.nzp and p.nzp_prm=2072 and p.val_prm='1') " +
                " group by 1 "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }


            ExecSQL(conn_db, " create index ix1_ttt_ttt_aid_c1 on ttt_aid_c1 (nzp_dom_base,nzp_serv) ", true);
            ExecSQL(conn_db, " create index ix2_ttt_ttt_aid_c1 on ttt_aid_c1 (nzp_serv) ", true);
            ExecSQL(conn_db, sUpdStat + " ttt_aid_c1 ", true);

            if ((Convert.ToDateTime("01." + rashod.paramcalc.calc_mm + "." + rashod.paramcalc.calc_yy.ToString()) >= Convert.ToDateTime("01.06.2013"))
                ) // !!! after'01.06.2013' !!!
            {
                // восстановить необрезанный расход по ОДПУ (ранее был испорчен при расчете отдельно по домам)
                ret = ExecSQL(conn_db,
                      " Update ttt_aid_c1 Set val3 = val4 " +
                      " Where cnt_stage=1  "
                    , true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }
                // обрезание расхода ОДПУ на ОДН по Пост344 если нет признака разрешающего превышение!
                // ... для ХВС
                ret = ExecSQL(conn_db,
                      " Update ttt_aid_c1 Set val3 = rvirt " +
                      " Where cnt_stage=1 and nzp_serv = 6 and val3 > rvirt " +
                      " and 0=(Select count(*) From ttt_prm_2 p Where ttt_aid_c1.nzp_dom_base = p.nzp and p.nzp_prm=1214 ) "
                    , true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }
                // ... для ГВС
                ret = ExecSQL(conn_db,
                      " Update ttt_aid_c1 Set val3 = rvirt " +
                      " Where cnt_stage=1 and nzp_serv = 9 and val3 > rvirt " +
                      " and 0=(Select count(*) From ttt_prm_2 p Where ttt_aid_c1.nzp_dom_base = p.nzp and p.nzp_prm=1215 ) "
                    , true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }
                // ... для эл/энергии
                ret = ExecSQL(conn_db,
                      " Update ttt_aid_c1 Set val3 = rvirt " +
                      " Where cnt_stage=1 and nzp_serv =25 and val3 > rvirt " +
                      " and 0=(Select count(*) From ttt_prm_2 p Where ttt_aid_c1.nzp_dom_base = p.nzp and p.nzp_prm=1216 ) "
                    , true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }
                // ... для газа
                ret = ExecSQL(conn_db,
                      " Update ttt_aid_c1 Set val3 = rvirt " +
                      " Where cnt_stage=1 and nzp_serv =10 and val3 > rvirt " +
                      " and 0=(Select count(*) From ttt_prm_2 p Where ttt_aid_c1.nzp_dom_base = p.nzp and p.nzp_prm=1217 ) "
                    , true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }
            }

            //  проставить коэффициенты коррекции расходов по Пост.№354 для домов с суммируемым ОДПУ
            ret = ExecSQL(conn_db,
                  " update ttt_aid_c1 set dlt_cur = val3 - (val1 + val2 + dlt_reval + dlt_real_charge + rash_link)," +
                  " kf307   =( case when (val3-(val1+val2 + dlt_reval + dlt_real_charge + rash_link))>0.0001" +
                                  " then (case when squ1>0.0001 then (val3-(val1+val2 + dlt_reval + dlt_real_charge + rash_link))/squ1 else 0 end)" +
                                  " else (case when gil1>0.0001 and (val3-(val1+val2 + dlt_reval + dlt_real_charge + rash_link))<-0.0001" +
                                        " then (val3-(val1 + val2 + dlt_reval + dlt_real_charge + rash_link))/gil1 else 0 end)" +
                             " end )," +
                  " kf307n  =( case when (val3-(val1 + val2 + dlt_reval + dlt_real_charge + rash_link))>0.0001" +
                                  " then (case when squ1>0.0001 then (val3-(val1 + val2 + dlt_reval + dlt_real_charge + rash_link))/squ1 else 0 end)" +
                                  " else (case when gil1>0.0001 and (val3-(val1 + val2 + dlt_reval + dlt_real_charge + rash_link))<-0.0001" +
                                        " then (val3-(val1 + val2 + dlt_reval + dlt_real_charge + rash_link))/gil1 else 0 end)" +
                             " end )," +
                  " kod_info=" +

                            "( case when (val3 - (val1 + val2 + dlt_reval + dlt_real_charge + rash_link))>0.000001" +
                                  " then " +

                                         " case when nzp_serv=6 and i21=0" +
                                         " then 26" +
                                         " else " +
                                            "(case when (nzp_serv=25 and i21=1) or (nzp_serv in (210,410))" +
                                            " then 22 else 21" +
                                            " end)" +
                                         " end" +

                                  " else (case when (val3 - (val1 + val2 + dlt_reval + dlt_real_charge + rash_link))<-0.000001" +
                                        " then" +

                                         " case when nzp_serv=6 and i21=0" +
                                         " then 36" +
                                         " else " +
                                            "(case when (nzp_serv=25 and i21=1) or (nzp_serv in (210,410))" +
                                            " then 32 else 31" +
                                            " end)" +
                                         " end" +

                                        " else 0 end)" +
                                  " end )," +
                  " gl7kw = case when squ1>0.000001 then kf_dpu_ls / squ1 else 0 end, " +
                  " kf_dpu_plob   =" +
                   "( case when (val4 - (val1 + val2 + dlt_reval + dlt_real_charge + rash_link))>0.000001" +
                    " then (case when squ1>0.000001" +
                          " then (val4 - (val1 + val2 + dlt_reval + dlt_real_charge + rash_link))/squ1 else 0 end)" +
                    " else (case when gil1>0.000001 and (val3 - (val1 + val2 + dlt_reval + dlt_real_charge + rash_link))<-0.000001" +
                          " then (val4 - (val1 + val2 + dlt_reval + dlt_real_charge + rash_link))/gil1 else 0 end)" +
                    " end ) " +
                  " where 1=1 "
                  , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            string sql =
                  " update ttt_aid_kpu set " +
#if PG
 " val1=(select v.val1 from ttt_aid_c1 v," + rashod.paramcalc.data_alias + "link_dom_lit l where v.nzp_dom_base=l.nzp_dom_base and l.nzp_dom=ttt_aid_kpu.nzp_dom and v.nzp_serv=ttt_aid_kpu.nzp_serv and v.i21=ttt_aid_kpu.i21 and v.kod_info>0 ) " +
                  ",val2=(select v.val2 from ttt_aid_c1 v," + rashod.paramcalc.data_alias + "link_dom_lit l where v.nzp_dom_base=l.nzp_dom_base and l.nzp_dom=ttt_aid_kpu.nzp_dom and v.nzp_serv=ttt_aid_kpu.nzp_serv and v.i21=ttt_aid_kpu.i21 and v.kod_info>0 ) " +
                  ",val3=(select v.val3 from ttt_aid_c1 v," + rashod.paramcalc.data_alias + "link_dom_lit l where v.nzp_dom_base=l.nzp_dom_base and l.nzp_dom=ttt_aid_kpu.nzp_dom and v.nzp_serv=ttt_aid_kpu.nzp_serv and v.i21=ttt_aid_kpu.i21 and v.kod_info>0 ) " +
                  ",val4=(select v.val4 from ttt_aid_c1 v," + rashod.paramcalc.data_alias + "link_dom_lit l where v.nzp_dom_base=l.nzp_dom_base and l.nzp_dom=ttt_aid_kpu.nzp_dom and v.nzp_serv=ttt_aid_kpu.nzp_serv and v.i21=ttt_aid_kpu.i21 and v.kod_info>0 ) " +
                  ",dlt_reval      =(select v.dlt_reval       from ttt_aid_c1 v," + rashod.paramcalc.data_alias + "link_dom_lit l where v.nzp_dom_base=l.nzp_dom_base and l.nzp_dom=ttt_aid_kpu.nzp_dom and v.nzp_serv=ttt_aid_kpu.nzp_serv and v.i21=ttt_aid_kpu.i21 and v.kod_info>0 ) " +
                  ",dlt_real_charge=(select v.dlt_real_charge from ttt_aid_c1 v," + rashod.paramcalc.data_alias + "link_dom_lit l where v.nzp_dom_base=l.nzp_dom_base and l.nzp_dom=ttt_aid_kpu.nzp_dom and v.nzp_serv=ttt_aid_kpu.nzp_serv and v.i21=ttt_aid_kpu.i21 and v.kod_info>0 ) " +
                  ",dlt_cur        =(select v.dlt_cur         from ttt_aid_c1 v," + rashod.paramcalc.data_alias + "link_dom_lit l where v.nzp_dom_base=l.nzp_dom_base and l.nzp_dom=ttt_aid_kpu.nzp_dom and v.nzp_serv=ttt_aid_kpu.nzp_serv and v.i21=ttt_aid_kpu.i21 and v.kod_info>0 ) " +
                  ",squ1=(select v.squ1 from ttt_aid_c1 v," + rashod.paramcalc.data_alias + "link_dom_lit l where v.nzp_dom_base=l.nzp_dom_base and l.nzp_dom=ttt_aid_kpu.nzp_dom and v.nzp_serv=ttt_aid_kpu.nzp_serv and v.i21=ttt_aid_kpu.i21 and v.kod_info>0 ) " +
                  ",gil1=(select v.gil1 from ttt_aid_c1 v," + rashod.paramcalc.data_alias + "link_dom_lit l where v.nzp_dom_base=l.nzp_dom_base and l.nzp_dom=ttt_aid_kpu.nzp_dom and v.nzp_serv=ttt_aid_kpu.nzp_serv and v.i21=ttt_aid_kpu.i21 and v.kod_info>0 ) " +
                  ",kf307 =(select v.kf307  from ttt_aid_c1 v," + rashod.paramcalc.data_alias + "link_dom_lit l where v.nzp_dom_base=l.nzp_dom_base and l.nzp_dom=ttt_aid_kpu.nzp_dom and v.nzp_serv=ttt_aid_kpu.nzp_serv and v.i21=ttt_aid_kpu.i21 and v.kod_info>0 ) " +
                  ",kf307n=(select v.kf307n from ttt_aid_c1 v," + rashod.paramcalc.data_alias + "link_dom_lit l where v.nzp_dom_base=l.nzp_dom_base and l.nzp_dom=ttt_aid_kpu.nzp_dom and v.nzp_serv=ttt_aid_kpu.nzp_serv and v.i21=ttt_aid_kpu.i21 and v.kod_info>0 ) " +
                  ",kod_info=(select v.kod_info from ttt_aid_c1 v," + rashod.paramcalc.data_alias + "link_dom_lit l where v.nzp_dom_base=l.nzp_dom_base and l.nzp_dom=ttt_aid_kpu.nzp_dom and v.nzp_serv=ttt_aid_kpu.nzp_serv and v.i21=ttt_aid_kpu.i21 and v.kod_info>0 ) " +
                  ",kf_dpu_kg  =(select v.kf_dpu_kg   from ttt_aid_c1 v," + rashod.paramcalc.data_alias + "link_dom_lit l where v.nzp_dom_base=l.nzp_dom_base and l.nzp_dom=ttt_aid_kpu.nzp_dom and v.nzp_serv=ttt_aid_kpu.nzp_serv and v.i21=ttt_aid_kpu.i21 and v.kod_info>0 ) " +
                  ",kf_dpu_ls  =(select v.kf_dpu_ls   from ttt_aid_c1 v," + rashod.paramcalc.data_alias + "link_dom_lit l where v.nzp_dom_base=l.nzp_dom_base and l.nzp_dom=ttt_aid_kpu.nzp_dom and v.nzp_serv=ttt_aid_kpu.nzp_serv and v.i21=ttt_aid_kpu.i21 and v.kod_info>0 ) " +
                  ",kf_dpu_plob=(select v.kf_dpu_plob from ttt_aid_c1 v," + rashod.paramcalc.data_alias + "link_dom_lit l where v.nzp_dom_base=l.nzp_dom_base and l.nzp_dom=ttt_aid_kpu.nzp_dom and v.nzp_serv=ttt_aid_kpu.nzp_serv and v.i21=ttt_aid_kpu.i21 and v.kod_info>0 ) " +
                  ",gl7kw=(select v.gl7kw from ttt_aid_c1 v," + rashod.paramcalc.data_alias + "link_dom_lit l where v.nzp_dom_base=l.nzp_dom_base and l.nzp_dom=ttt_aid_kpu.nzp_dom and v.nzp_serv=ttt_aid_kpu.nzp_serv and v.i21=ttt_aid_kpu.i21 and v.kod_info>0 ) " +
                  ",rvirt=(select v.rvirt from ttt_aid_c1 v," + rashod.paramcalc.data_alias + "link_dom_lit l where v.nzp_dom_base=l.nzp_dom_base and l.nzp_dom=ttt_aid_kpu.nzp_dom and v.nzp_serv=ttt_aid_kpu.nzp_serv and v.i21=ttt_aid_kpu.i21 and v.kod_info>0 ) " +
#else
                  " (val1,val2,val3,val4,dlt_reval,dlt_real_charge,dlt_cur,squ1,gil1,kf307,kf307n,kod_info,kf_dpu_kg,kf_dpu_ls,kf_dpu_plob,gl7kw,rvirt)=((" +
                   " select v.val1,v.val2,v.val3,v.val4,v.dlt_reval,v.dlt_real_charge,v.dlt_cur,v.squ1,v.gil1,v.kf307,v.kf307n,v.kod_info,v.kf_dpu_kg,v.kf_dpu_ls,v.kf_dpu_plob,v.gl7kw,v.rvirt" +
                   " from ttt_aid_c1 v," + rashod.paramcalc.data_alias + "link_dom_lit l" +
                   " where v.nzp_dom_base=l.nzp_dom_base and l.nzp_dom=ttt_aid_kpu.nzp_dom and v.nzp_serv=ttt_aid_kpu.nzp_serv and v.i21=ttt_aid_kpu.i21 and v.kod_info>0  " +
                  " ))" +
#endif
 " where 0<( select count(*)" +
                   " from ttt_aid_c1 v," + rashod.paramcalc.data_alias + "link_dom_lit l" +
                   " where v.nzp_dom_base=l.nzp_dom_base and l.nzp_dom=ttt_aid_kpu.nzp_dom and v.nzp_serv=ttt_aid_kpu.nzp_serv and v.i21=ttt_aid_kpu.i21 and v.kod_info>0  " +
                  " ) ";
            ret = ExecSQL(conn_db, sql, true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            return true;
        }
        #endregion обработка домов с суммарным начислением ОДН (секционных)

        #region Взять количество знаков округления коэф-та на ОДН и округлить
        public bool Stek354GetRound(IDbConnection conn_db, Rashod rashod, out Returns ret)
        //--------------------------------------------------------------------------------
        {
            ret = Utils.InitReturns();

            MyDataReader reader;
            //
            int iCntTruncODN = 0;
            //1301|БД-Количество знаков округления коэф.на ОДН|||int||10|0|100|3|
            //MyDataReader reader1;
            ret = ExecRead(conn_db, out reader,
                " select max(" + sNvlWord + "(p.val_prm,'0')" + sConvToInt + ") kz " +
                " from " + rashod.paramcalc.data_alias + "prm_10 p " +
                " where p.nzp_prm=1301 and p.is_actual<>100 and p.dat_s <= " + rashod.paramcalc.dat_po + " and p.dat_po >= " + rashod.paramcalc.dat_s
                , true);
            if (!ret.result)
            {
                if (Points.IsSmr) { iCntTruncODN = 4; } else { iCntTruncODN = 0; }
            }
            else
            {
                if (reader.Read())
                {
                    try
                    {
                        iCntTruncODN = Convert.ToInt32(reader["kz"]);
                    }
                    catch
                    {
                        if (Points.IsSmr) { iCntTruncODN = 4; } else { iCntTruncODN = 0; }
                    }
                }
                reader.Close();
            }

            if (iCntTruncODN > 0)
            {
                // для Самары округлить коэф-т коррекции до 4х знаков
                ret = ExecSQL(conn_db,
                      " update ttt_aid_kpu set " +
                      " kf307   = round( kf307 , " + iCntTruncODN + " )," +
                      " kf307n  = round( kf307n, " + iCntTruncODN + " ) " +
                      " where kod_info>0 "
                    , true);
                if (!ret.result)
                {
                    DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                    return false;
                }
            }

            return true;
        }
        #endregion Взять количество знаков округления коэф-та на ОДН и округлить

        #region сохранить стек 354 для домов
        public bool Stek354Save(IDbConnection conn_db, Rashod rashod, out Returns ret)
        //--------------------------------------------------------------------------------
        {
            ret = Utils.InitReturns();

            //-- вставка в стек 354 --------------------------------------------------------------
            ret = ExecSQL(conn_db,
                  " Insert into " + rashod.counters_xx +
                        " ( nzp_dom, nzp_kvar, nzp_type, nzp_serv, dat_charge, cur_zap, nzp_counter, cnt_stage, mmnog, stek, rashod, " +
                        "   dat_s, val_s, dat_po, val_po, val1, val2, val3, val4, rvirt, squ1, squ2, gil1, gil2, cnt1, cnt2, cnt3, cnt4, " +
                        "   cnt5, dop87, pu7kw, gl7kw, vl210, kf307, kod_info, val4_source,up_kf " +
                        "   ,cls1, cls2, kf_dpu_kg, kf_dpu_plob, kf_dpu_plot, kf_dpu_ls, kf307n, kf307f9, dlt_cur, dlt_reval, dlt_real_charge) " +
                  " Select nzp_dom, nzp_kvar, nzp_type, nzp_serv, dat_charge, cur_zap, nzp_counter, cnt_stage, mmnog, 354 stek, rashod, " +
                        "  dat_s, val_s, dat_po, val_po, val1, val2, val3, val4, rvirt, squ1, squ2, gil1, gil2, cnt1, cnt2, cnt3, cnt4, " +
                        "  cnt5, dop87, pu7kw, gl7kw, vl210, kf307, kod_info, val_norm_odn_source,up_kf " +
                        " ,cls1, cls2, kf_dpu_kg, kf_dpu_plob,   rash_link, kf_dpu_ls, kf307n, kf307f9, dlt_cur, dlt_reval, dlt_real_charge " +
                  " From ttt_aid_kpu where kod_info > 0 "
                , true);
            if (!ret.result)
            { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ret = ExecSQL(conn_db,
                  " Insert into " + rashod.countlnk_xx +
                        " (nzp_dom, nzp_kvar, nzp_type, nzp_serv, dat_charge, cur_zap, stek, kod_info, " +
                          "nzp_serv_l1,val1_l1,val2_l1,squ1_l1,squ2_l1,gil1_l1,gil2_l1,cls1_l1,cls2_l1,dlt_reval_l1,dlt_real_charge_l1," +
                          "nzp_serv_l2,val1_l2,val2_l2,squ1_l2,squ2_l2,gil1_l2,gil2_l2,cls1_l2,cls2_l2,dlt_reval_l2,dlt_real_charge_l2)" +
                  " Select" +
                        "  nzp_dom, nzp_kvar, nzp_type, nzp_serv, dat_charge, i21, 354 stek, kod_info, " +
                          "nzp_serv_l1,val1_l1,val2_l1,squ1_l1,squ2_l1,gil1_l1,gil2_l1,cls1_l1,cls2_l1,dlt_reval_l1,dlt_real_charge_l1," +
                          "nzp_serv_l2,val1_l2,val2_l2,squ1_l2,squ2_l2,gil1_l2,gil2_l2,cls1_l2,cls2_l2,dlt_reval_l2,dlt_real_charge_l2 " +
                  " From ttt_aid_kpu where kod_info > 0 and abs(rash_link)>0 and (nzp_serv_l1>0 or nzp_serv_l2>0) "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            return true;
        }
        #endregion сохранить стек 354 для домов

        #region коррекция стека стек 354 для домов с расчетом Тулы с ограничением нормы для ИПУ
        public bool Stek354KorrForIPU(IDbConnection conn_db, Rashod rashod, out Returns ret)
        //--------------------------------------------------------------------------------
        {
            ret = Utils.InitReturns();

            bool bIsCalcCurMonth =
                ((rashod.paramcalc.cur_yy == rashod.paramcalc.calc_yy) &&
                 (rashod.paramcalc.cur_mm == rashod.paramcalc.calc_mm));
            // если не текущий расчетный месяц, то не выполнять!
            if (!bIsCalcCurMonth)
            {
                return true;
            }

            ret = ExecSQL(conn_db, " drop table ttt_korr_ipu ", true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            // выбрать только ХВС по указанным домам, где коэф > 0
            ret = ExecSQL(conn_db,
                  " Select nzp_cntx, nzp_dom, nzp_kvar, nzp_type, nzp_serv, dat_charge, cur_zap, nzp_counter, cnt_stage, mmnog, stek, rashod, " +
                        "  dat_s, val_s, dat_po, val_po, val1, val2, val3, val4, rvirt, squ1, squ2, gil1, gil2, cnt1, cnt2, cnt3, cnt4, " +
                        "  cnt5, dop87, pu7kw, gl7kw, vl210, kf307, kod_info, " +
                        "  cls1, cls2, kf_dpu_kg, kf_dpu_plob as rash_link, kf_dpu_plot, kf_dpu_ls, kf307n, kf307f9, dlt_cur, dlt_reval, dlt_real_charge " +
                        " ,cnt1 as i21 " +
                  " into temp ttt_korr_ipu " +
                  " From " + rashod.counters_xx +
                  " where nzp_type=1 and nzp_serv=6 and stek=354 and kod_info = 21 " +
                  " and 0 < ( Select count(*) From ttt_prm_2 p " +
                          " Where " + rashod.counters_xx + ".nzp_dom  = p.nzp and p.nzp_prm = 2031 " +
                          "   and p.val_prm='5' " +
                          " ) "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ExecSQL(conn_db, " create index ix1_ttt_korr_ipu on ttt_korr_ipu (nzp_cntx) ", false);
            ExecSQL(conn_db, " create index ix2_ttt_korr_ipu on ttt_korr_ipu (nzp_dom,nzp_serv) ", false);
            ExecSQL(conn_db, sUpdStat + " ttt_korr_ipu ", false);

            // взять площадь нежилых помещений дома
            ret = ExecSQL(conn_db, " Update ttt_korr_ipu Set squ2 = 0 ", true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ret = ExecSQL(conn_db,
                  " Update ttt_korr_ipu " +
                  " Set squ2 = " + sNvlWord + "(p.val_prm,'0')" + sConvToNum +
                  " From ttt_prm_2 p " +
                  " Where p.nzp_prm=2051 and p.nzp=ttt_korr_ipu.nzp_dom "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            string scharge_alias =
                rashod.paramcalc.pref + "_charge_" + (rashod.paramcalc.cur_yy - 2000).ToString("00") + tableDelimiter;
            // взять объемные перекидки
            ret = ExecSQL(conn_db,
                  " Update ttt_korr_ipu " +
                  " Set dlt_real_charge =" +
                  "(select sum(" + sNvlWord + "(p.volum,0)) " +
                  " from " + scharge_alias + "perekidka p, t_selkvar k " +
                  " where k.nzp_dom=ttt_korr_ipu.nzp_dom and p.nzp_serv=ttt_korr_ipu.nzp_serv " +
                  " and p.nzp_kvar=k.nzp_kvar" +
                  " and p.type_rcl=163 and p.month_= " + rashod.paramcalc.cur_mm +
                  ") " +
                  " Where " +
                  " exists (select 1 " +
                  " from " + scharge_alias + "perekidka p, t_selkvar k " +
                  " where k.nzp_dom=ttt_korr_ipu.nzp_dom and p.nzp_serv=ttt_korr_ipu.nzp_serv " +
                  " and p.nzp_kvar=k.nzp_kvar" +
                  " and p.type_rcl=163 and p.month_= " + rashod.paramcalc.cur_mm +
                  ") "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            // взять перерасчет программы
            ret = ExecSQL(conn_db,
                  " Update ttt_korr_ipu " +
                  " Set dlt_reval =" +
                  "(select" +
                    " sum(case when tarif  >0 then sum_tarif  /tarif   else 0 end) - " +
                    " sum(case when tarif_p>0 then sum_tarif_p/tarif_p else 0 end) " +
                  " from " + scharge_alias + "reval_" + rashod.paramcalc.cur_mm.ToString("00") + " p, t_selkvar k " +
                  " where k.nzp_dom=ttt_korr_ipu.nzp_dom and p.nzp_serv=ttt_korr_ipu.nzp_serv " +
                  " and p.nzp_kvar=k.nzp_kvar" +
                  ") " +
                  " Where " +
                  " exists (select 1 " +
                  " from " + scharge_alias + "reval_" + rashod.paramcalc.cur_mm.ToString("00") + " p, t_selkvar k " +
                  " where k.nzp_dom=ttt_korr_ipu.nzp_dom and p.nzp_serv=ttt_korr_ipu.nzp_serv " +
                  " and p.nzp_kvar=k.nzp_kvar" +
                  ") "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ret = ExecSQL(conn_db,
                  " Update ttt_korr_ipu Set " +
                  " squ1 = squ1 + squ2," +
                  " val3 = (case when cnt_stage=1 then val4 else val3 end)," +
                  " i21 = 1," +
                  " cnt4 = 221 "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            //  проставить коэффициенты коррекции расходов по Пост.№354 для домов с ОДПУ! если нет ОДПУ коэф-ты уже вычислены!
            ret = ExecSQL(conn_db,
                " update ttt_korr_ipu set " +
                " kf307   =( case when (val3 - (val1 + val2 + dlt_reval + dlt_real_charge + rash_link))>0.000001" +
                                " then (case when squ1>0.000001" +
                                      " then (val3 - (val1 + val2 + dlt_reval + dlt_real_charge + rash_link)) / squ1 else 0 end)" +
                                " else (case when gil1>0.000001 and (val3 - (val1 + val2 + dlt_reval + dlt_real_charge + rash_link))<-0.000001" +
                                      " then (val3 - (val1 + val2 + dlt_reval + dlt_real_charge + rash_link)) / gil1 else 0 end)" +
                           " end )," +
                " kf307n  =( case when (val3 - (val1 + val2 + dlt_reval + dlt_real_charge + rash_link))>0.000001" +
                                " then (case when squ1>0.0001" +
                                      " then (val3 - (val1 + val2 + dlt_reval + dlt_real_charge + rash_link)) / squ1 else 0 end)" +
                                " else (case when gil1>0.000001 and (val3 - (val1 + val2 + dlt_reval + dlt_real_charge + rash_link))<-0.000001" +
                                      " then (val3 - (val1 + val2 + dlt_reval + dlt_real_charge + rash_link)) / gil1 else 0 end)" +
                           " end )," +
                " kod_info=" +

                          "( case when (val3 - (val1 + val2 + dlt_reval + dlt_real_charge + rash_link))>0.000001" +
                                " then " +

                                       " case when nzp_serv=6 and i21=0" +
                                       " then 26" +
                                       " else " +
                                          "(case when (nzp_serv=25 and i21=1) or (nzp_serv in (210,410))" +
                                          " then 22 else 21" +
                                          " end)" +
                                       " end" +

                                " else (case when (val3 - (val1 + val2 + dlt_reval + dlt_real_charge + rash_link))<-0.000001" +
                                      " then" +

                                       " case when nzp_serv=6 and i21=0" +
                                       " then 36" +
                                       " else " +
                                          "(case when (nzp_serv=25 and i21=1) or (nzp_serv in (210,410))" +
                                          " then 32 else 31" +
                                          " end)" +
                                       " end" +

                                      " else 0 end)" +
                                " end )" +


                " where nzp_serv in (6,9,25,10,210,410) " //cnt_stage=1 and 
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ret = ExecSQL(conn_db,
                  " Update ttt_korr_ipu" +
                  " Set kf307 = (kf_dpu_ls/squ1), cnt5 = 1 " +
                  " Where kod_info in (21,22,26) and kf307 > (kf_dpu_ls/squ1) and squ1 > 0 "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ret = ExecSQL(conn_db,
                  " Update " + rashod.counters_xx +
                  " Set kf307=a.kf307,kf307n=a.kf307n,kod_info=a.kod_info," +
                    "squ1=a.squ1,squ2=a.squ2,val3=a.val3," +
                    "dlt_real_charge=a.dlt_real_charge,dlt_reval=a.dlt_reval," +
                    "cnt4=a.cnt4,cnt5=a.cnt5" +
                  " From ttt_korr_ipu a where a.nzp_cntx=" + rashod.counters_xx + ".nzp_cntx "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ret = ExecSQL(conn_db, " drop table ttt_korr_ipu ", true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            return true;
        }
        #endregion коррекция стека стек 354 для домов с расчетом Тулы с ограничением нормы для ИПУ

        // сохранить стек 3 для ОДПУ (nzp_type=1 and stek=3) в стек 354 с расчетом по Пост.№354 (+ нормативы ОДН)
        public bool Calc354ODPU(IDbConnection conn_db, Rashod rashod, out Returns ret)
        //--------------------------------------------------------------------------------
        {
            ret = Utils.InitReturns();

            //----------------------------------------------------------------
            // сохранить стек 3 для домов (nzp_type=1 and stek=3) в стек 354 для расчетов по Пост.№354
            //----------------------------------------------------------------
            //-- выбрать домовые расходы для расчета ОДН
            Stek354SelPU(conn_db, rashod, "1", out ret);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }
            //-
            //-- добавить расходы по связным услугам для расчета ОДН
            Stek354AddLinkServ(conn_db, rashod, out ret);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }
            //-
            //-- нормативный расход ОДН по услугам
            Stek354NormODN(conn_db, rashod, out ret);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            //-- расчет коэффициентов коррекции расхода на ОДН
            Stek354CalcKoef(conn_db, rashod, "1", out ret);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            //-- обработка домов с суммарным начислением ОДН (секционных)
            Stek354LinkDoms(conn_db, rashod, out ret);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            //--  Взять количество знаков округления коэф-та на ОДН и округлить
            Stek354GetRound(conn_db, rashod, out ret);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            //-- вставка в стек 354 --------------------------------------------------------------
            Stek354Save(conn_db, rashod, out ret);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            // коррекция стека стек 354 для домов с расчетом Тулы с ограничением нормы для ИПУ
            Stek354KorrForIPU(conn_db, rashod, out ret);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            return true;
        }

        // сохранить стек 3 для ГрПУ (nzp_type=2 and stek=3) в стек 354 с расчетом по Пост.№354 (+ нормативы ОДН)
        public bool Calc354GrPU(IDbConnection conn_db, Rashod rashod, out Returns ret)
        //--------------------------------------------------------------------------------
        {
            ret = Utils.InitReturns();

            //----------------------------------------------------------------
            // сохранить стек 3 для домов (nzp_type=1 and stek=3) в стек 354 для расчетов по Пост.№354
            //----------------------------------------------------------------
            //-- выбрать домовые расходы для расчета ОДН
            Stek354SelPU(conn_db, rashod, "2", out ret);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            //-- добавить расходы по связным услугам для расчета ОДН
            Stek354AddLinkServGrPU(conn_db, rashod, out ret);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            //-- нормативный расход ОДН по услугам
            Stek354NormODNGrPU(conn_db, rashod, out ret);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            //-- расчет коэффициентов коррекции расхода на ОДН
            Stek354CalcKoef(conn_db, rashod, "2", out ret);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            //-- обработка домов с суммарным начислением ОДН (секционных)
            //Stek354LinkDoms(conn_db, rashod, out ret);
            //if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            //--  Взять количество знаков округления коэф-та на ОДН и округлить
            Stek354GetRound(conn_db, rashod, out ret);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            //-- вставка в стек 354 --------------------------------------------------------------
            Stek354Save(conn_db, rashod, out ret);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            return true;
        }

        #endregion сохранить стек 3 для домов (nzp_type=1 & 2 and stek=3) в стек 354 с расчетом по Пост.№354 (+ нормативы ОДН)

        #region сохранить стек 3 для домов (nzp_type=1 and stek=3) в стек 3540 с расчетом по Пост.№354 - спец.расчет для бойлеров
        public bool Stek3540Make(IDbConnection conn_db, Rashod rashod, out Returns ret)
        //--------------------------------------------------------------------------------
        {
            ret = Utils.InitReturns();

            ExecSQL(conn_db, " Drop table ttt_aid_c1 ", false);

            //-- Используется ранее созданная ttt_aid_kpu для домоых ПУ! в Calc354ODPU(...)!
            ret = ExecSQL(conn_db,
                " Select nzp_dom, nzp_kvar, nzp_type, nzp_serv, dat_charge, cur_zap, nzp_counter, cnt_stage, mmnog, 3540 stek, rashod, " +
                       "   dat_s, val_s, dat_po, val_po, val1, val2, val3, val4, rvirt, squ1, squ2, gil1, gil2, cnt1, cnt2, cnt3, cnt4, " +
                       "   cnt5, dop87, pu7kw, gl7kw, vl210,0 kf307,0 kod_info " +
                       "  ,cls1, cls2, kf_dpu_kg, kf_dpu_plob, kf_dpu_plot, kf_dpu_ls,0 kf307n,0 kf307f9,0 dlt_cur " +
#if PG
 " into temp ttt_aid_c1 " +
                " From ttt_aid_kpu where nzp_serv in (8,9) "
#else
                " From ttt_aid_kpu where nzp_serv in (8,9) "
                +" into temp ttt_aid_c1 with no log "
#endif
, true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ExecSQL(conn_db, " create index ix_ttt_ttt_aid_c1 on ttt_aid_c1 (nzp_dom,nzp_serv) ", true);
            ExecSQL(conn_db, sUpdStat + " ttt_aid_c1 ", true);

            // проставить домовые параметры для расчета расходов по бойлерам
            string sql =
                  " update ttt_aid_c1 set kod_info=24," +
                  " val3 = ( Select " +
                  "   max( case when nzp_prm=1104 then replace( " + sNvlWord + "(val_prm,'0'), ',', '.')" + sConvToNum + " else 0 end ) as val3" +
                  " From ttt_prm_2 p Where ttt_aid_c1.nzp_dom = p.nzp and p.nzp_prm in (1104,1105) ) " +
                  ",vl210 = ( Select " +
                  "   max( case when nzp_prm=1105 then replace( " + sNvlWord + "(val_prm,'0'), ',', '.')" + sConvToNum + " else 0 end ) as vl210 " +
                  " From ttt_prm_2 p Where ttt_aid_c1.nzp_dom = p.nzp and p.nzp_prm in (1104,1105) ) " +
                  " where nzp_serv=8 and 0< " +
                  " (Select count(*) " +
                  "  From ttt_prm_2 p Where ttt_aid_c1.nzp_dom = p.nzp and p.nzp_prm in (1104,1105) ) ";
            ret = ExecSQL(conn_db, sql, true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            sql =
                  " update ttt_aid_c1 set kod_info=25," +
                  " val3 = ( Select " +
                    "   max( case when nzp_prm=1106 then replace( " + sNvlWord + "(val_prm,'0'), ',', '.')" + sConvToNum + " else 0 end ) as val3 " +
                  "  From ttt_prm_2 p Where ttt_aid_c1.nzp_dom = p.nzp and p.nzp_prm in (1106,1107) ) " +
                  ",vl210 = ( Select " +
                    "   max( case when nzp_prm=1107 then replace( " + sNvlWord + "(val_prm,'0'), ',', '.')" + sConvToNum + " else 0 end ) as vl210 " +
                  "  From ttt_prm_2 p Where ttt_aid_c1.nzp_dom = p.nzp and p.nzp_prm in (1106,1107) ) " +
                  " where nzp_serv=9 and 0< " +
                  " (Select count(*) " +
                  "  From ttt_prm_2 p Where ttt_aid_c1.nzp_dom = p.nzp and p.nzp_prm in (1106,1107) ) ";
            ret = ExecSQL(conn_db, sql, true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            // расчет расходов по Пост.№354 для бойлеров
            ret = ExecSQL(conn_db,
                  " update ttt_aid_c1 set " +
                  " kf307  = val3 / squ1," +
                  " kf307n = val3 / squ1" +
                  " where kod_info=24 and nzp_serv in (8) and squ1>0.0001 "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ret = ExecSQL(conn_db,
                  " update ttt_aid_c1 set " +
                  " kf307  = val3 / (val1+val2) ," +
                  " kf307n = val3 / (val1+val2) " +
                  " where kod_info=25 and nzp_serv in (9) and (val1+val2)>0.0001 "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            //-- вставка в стек 3540 --------------------------------------------------------------
            ret = ExecSQL(conn_db,
                  " Insert into " + rashod.counters_xx +
                        " ( nzp_dom, nzp_kvar, nzp_type, nzp_serv, dat_charge, cur_zap, nzp_counter, cnt_stage, mmnog, stek, rashod, " +
                        "   dat_s, val_s, dat_po, val_po, val1, val2, val3, val4, rvirt, squ1, squ2, gil1, gil2, cnt1, cnt2, cnt3, cnt4, " +
                        "   cnt5, dop87, pu7kw, gl7kw, vl210, kf307, kod_info " +
                        "   ,cls1, cls2, kf_dpu_kg, kf_dpu_plob, kf_dpu_plot, kf_dpu_ls, kf307n, kf307f9, dlt_cur) " +
                  " Select nzp_dom, nzp_kvar, nzp_type, nzp_serv, dat_charge, cur_zap, nzp_counter, cnt_stage, mmnog, 3540 stek, rashod, " +
                        "   dat_s, val_s, dat_po, val_po, val1, val2, val3, val4, rvirt, squ1, squ2, gil1, gil2, cnt1, cnt2, cnt3, cnt4, " +
                        "   cnt5, dop87, pu7kw, gl7kw, vl210, kf307, kod_info " +
                        "   ,cls1, cls2, kf_dpu_kg, kf_dpu_plob, kf_dpu_plot, kf_dpu_ls, kf307n, kf307f9, dlt_cur " +
                  " From ttt_aid_c1 "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            //

            ExecSQL(conn_db, " Drop table ttt_aid_dpu ", false);
            ExecSQL(conn_db, " Drop table ttt_aid_virt0 ", false);
            ExecSQL(conn_db, " Drop table ttt_aid_c1 ", false);

            return true;
        }
        #endregion сохранить стек 3 для домов (nzp_type=1 and stek=3) в стек 3540 с расчетом по Пост.№354 - спец.расчет для бойлеров

        #region расчет ОДПУ в домах без ИПУ в стек = 3 (пропорция) - для nzp_type in (1,2)
        public bool Calc307Props(IDbConnection conn_db, Rashod rashod, bool bIsCalcODN, bool bIsCalcCurMonth, out Returns ret)
        //--------------------------------------------------------------------------------
        {
            ret = Utils.InitReturns();

            if (bIsCalcODN || bIsCalcCurMonth)
            {
                //--------------------------103
                //есть ДПУ и нет КПУ
                //ДПУ в пропорции людей (или площадь на людей для отопления)

                //ДПУ в пропорции людей
                ret = ExecSQL(conn_db,
                    " Update " + rashod.counters_xx +
                    " Set kf_dpu_kg = val3 / gil1  " +
                    " Where nzp_type in (1,2) and stek = 3 and " + rashod.where_dom + rashod.paramcalc.per_dat_charge +
                    "   and nzp_serv <> 8 " +
                    "   and abs(val2)<0.001 and abs(val3)>0 and gil1>0 "
                    , true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                //ДПУ в пропорции сумме общих площадей
                ret = ExecSQL(conn_db,
                    " Update " + rashod.counters_xx +
                    " Set kf_dpu_plob = val3 / squ1  " +
                    " Where nzp_type in (1,2) and stek = 3 and " + rashod.where_dom + rashod.paramcalc.per_dat_charge +
                    "   and nzp_serv <> 8 " +
                    "   and abs(val2)<0.001 and abs(val3)>0 and squ1>0 "
                    , true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }
            }
            //if (bIsCalcODN || bIsCalcCurMonth) - для отопления пускать всегда! м.б. перерасчет по итогам года
            {
                // kf_dpu_plot - коэфициент ДПУ для распределения пропорционально сумме отапливаемых площадей
                ret = ExecSQL(conn_db,
                    " Update " + rashod.counters_xx +
                    " Set kf_dpu_plot = val3 / squ1  " +
                    " Where nzp_type in (1,2) and stek = 3 and " + rashod.where_dom + rashod.paramcalc.per_dat_charge +
                    "   and nzp_serv = 8 " +
                    "   and abs(val3)>0 and squ1>0 " // and abs(val2)<0.001 - учет наличия расхода ИПУ убран! по Пост354 - считать по ОДПУ.
                    , true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }
            }
            if (bIsCalcODN || bIsCalcCurMonth)
            {
                //ДПУ в пропорции кол-ву л/с
                ret = ExecSQL(conn_db,
                    " Update " + rashod.counters_xx +
                    " Set kf_dpu_ls = val3 / cls1  " +
                    " Where nzp_type in (1,2) and stek = 3 and " + rashod.where_dom + rashod.paramcalc.per_dat_charge +
                    "   and nzp_serv <> 8 " +
                    "   and abs(val2)<0.001 and abs(val3)>0 and cls1>0 "
                    , true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                //--------------------------109
                //есть ДПУ и КПУ (+нормативы) - преславутая 9 формула

                //коэфициент 9 формула
                ret = ExecSQL(conn_db,
                    " Update " + rashod.counters_xx +
                    " Set kf307f9 = val3 / (val2 + val1 + dlt_reval + dlt_real_charge)  " + //коэфициент
                    " Where nzp_type in (1,2) and stek = 3 and " + rashod.where_dom + rashod.paramcalc.per_dat_charge +
                    "   and abs(val2)>0.001 and abs(val3)>0.001 and (val1 + val2 + dlt_reval + dlt_real_charge) > 0 "
                    , true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                //-------------------------------------------------------------------

                #region Расчет коэффициентов пропорции на все единицы измерения для учета при расчете начислений
                //Параметры: услуга - код параметра
                var paramDict = new Dictionary<string, int>
                {
                    {"6",2034},
                    {"9",2038},
                    {"25",2042},
                    {"8,322,325",2048},
                    {"10",2065},
                    {"7,324",2094}
                };
                foreach (var param in paramDict)
                {
                    //Параметры: тип расчета ОДН - код параметра
                    string sPos, sType;
                    switch (param.Value)
                    {
                        case 2034: { sPos = "6"; sType = "2031"; } break;
                        case 2038: { sPos = "5"; sType = "2035"; } break;
                        case 2042: { sPos = "5"; sType = "2039"; } break;
                        case 2048: { sPos = "5"; sType = "2045"; } break;
                        case 2065: { sPos = "4"; sType = "2062"; } break;
                        case 2094: { sPos = "6"; sType = "2091"; } break;
                        default: { sPos = "6"; sType = "2031"; } break;
                    }

                    // - по ЛС
                    string sql =
                        " Update " + rashod.counters_xx + " set " +
                        " kf_dpu_kg  = (case when gil2 = 0 then 0 else (val3 - val2) / gil2 end) , " +
                        " kf_dpu_plob = (case when pu7kw = 0 then 0 else (val3 - val2) / pu7kw end) , " +
                        " kf_dpu_plot = (case when gl7kw = 0 then 0 else (val3 - val2) / gl7kw end) , " +
                        " kf_dpu_ls = (case when cls2 = 0 then 0 else (val3 - val2) / cls2 end) " +
                        " where stek = 3 and (nzp_type = 1 or nzp_type = 2) and nzp_serv in (" + param.Key + ") " +
                        " and exists ( Select 1 From ttt_prm_2 p " +
                        " Where " + rashod.counters_xx + ".nzp_dom  = p.nzp and p.nzp_prm = " + sType + " and p.val_prm='" + sPos + "' ) " +
                        " and " + rashod.where_dom + rashod.paramcalc.per_dat_charge;

                    ret = ExecSQL(conn_db, sql, true);
                    if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }
                }
                #endregion
            }

            return true;
        }
        #endregion расчет ОДПУ в домах без ИПУ в стек = 3 (пропорция) - для nzp_type in (1,2)

        #region учет ОДН по 354 постановлению в стек = 3 & nzp_type = 1 & 2

        // выбрать результат расчета из стека 354 / 3540
        // выборка ttt_aid_virt0 - рассчитанных ранее Пост.354 домов и ГрПУ
        public bool Stek354SelForUchet(IDbConnection conn_db, Rashod rashod, string pType, out Returns ret)
        //--------------------------------------------------------------------------------
        {
            ret = Utils.InitReturns();

            ExecSQL(conn_db, " Drop table ttt_aid_virt0 ", false);
            ret = ExecSQL(conn_db,
                  " Create temp table ttt_aid_virt0 " +
                  " ( nzp_dom  integer, " +
                  "   nzp_serv integer, " +
                  "   nzp_counter integer, " +
                  "   kf307    " + sDecimalType + "(15,7)," +
                  "   kf307n   " + sDecimalType + "(15,7)," +
                  "   kf307f9  " + sDecimalType + "(15,7)," +
                  "   val1     " + sDecimalType + "(15,7)," +
                  "   val2     " + sDecimalType + "(15,7)," +
                  "   squ1     " + sDecimalType + "(15,7), " +
                  "   squ2     " + sDecimalType + "(15,7), " +
                  "   gil1     " + sDecimalType + "(15,7), " +
                  "   val3     " + sDecimalType + "(15,7)," +
                  "   val4     " + sDecimalType + "(15,7)," +
                  "   vl210    " + sDecimalType + "(15,7)," +
                  "   dlt_cur  " + sDecimalType + "(15,7)," +
                  "   rvirt " + sDecimalType + "(15,7) default 0.0000000,           " +
                  "   pu7kw " + sDecimalType + "(15,7) default 0.0000000,           " +
                  "   gl7kw " + sDecimalType + "(15,7) default 0.0000000,           " +
                  "   kf_dpu_kg   " + sDecimalType + "(15,7) default 0.0000000,       " +
                  "   kf_dpu_plob " + sDecimalType + "(15,7) default 0.0000000,     " +
                  "   kf_dpu_ls   " + sDecimalType + "(15,7) default 0.0000000,       " +
                  "   dlt_reval   " + sDecimalType + "(15,7) default 0.0000000,       " +
                  "   dlt_real_charge " + sDecimalType + "(15,7) default 0.0000000, " +
                  "   kod_info integer," +
                  "   stek     integer," +
                  "   is_uchet integer default 0," +
                  "   val4_source     " + sDecimalType + "(15,7) default 0.0000000," +
                  "   up_kf     " + sDecimalType + "(15,7) default 1" +
                  " ) " + sUnlogTempTable
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ret = ExecSQL(conn_db,
                " Insert into ttt_aid_virt0 (nzp_dom,nzp_serv,nzp_counter,kf307,kf307n,kf307f9,val3,val4,vl210,dlt_cur,kod_info,stek ,val1,val2,squ1,squ2,gil1," +
                  " rvirt,pu7kw,gl7kw,kf_dpu_kg,kf_dpu_plob,kf_dpu_ls,dlt_reval,dlt_real_charge,val4_source,up_kf)" +
                " Select nzp_dom,nzp_serv,nzp_counter,kf307,kf307n,kf307f9,val3,val4,vl210,dlt_cur,kod_info,stek ,val1,val2,squ1,squ2,gil1," +
                  " rvirt,pu7kw,gl7kw,kf_dpu_kg,kf_dpu_plob,kf_dpu_ls,dlt_reval,dlt_real_charge,val4_source,up_kf" +
                " From " + rashod.counters_xx +
                " where nzp_type=" + pType + " and stek=354 and " + rashod.where_dom + rashod.paramcalc.per_dat_charge
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ret = ExecSQL(conn_db,
                " Insert into ttt_aid_virt0 (nzp_dom,nzp_serv,nzp_counter,kf307,kf307n,kf307f9,val3,val4,vl210,dlt_cur,kod_info,stek ,val1,val2,squ1,squ2,gil1," +
                  " rvirt,pu7kw,gl7kw,kf_dpu_kg,kf_dpu_plob,kf_dpu_ls,dlt_reval,dlt_real_charge)" +
                " Select nzp_dom,nzp_serv,nzp_counter,kf307,kf307n,kf307f9,val3,val4,vl210,dlt_cur,kod_info,stek ,val1,val2,squ1,squ2,gil1," +
                  " rvirt,pu7kw,gl7kw,kf_dpu_kg,kf_dpu_plob,kf_dpu_ls,dlt_reval,dlt_real_charge" +
                " From " + rashod.counters_xx +
                " where nzp_type=" + pType + " and stek=3540 and " + rashod.where_dom + rashod.paramcalc.per_dat_charge
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ExecSQL(conn_db, " Create index ix_ttt_aid_virt1 on ttt_aid_virt0 (nzp_dom,nzp_serv) ", true);
            ExecSQL(conn_db, " Create index ix_ttt_aid_virt2 on ttt_aid_virt0 (nzp_serv,stek) ", true);
            ExecSQL(conn_db, " Create index ix_ttt_aid_virt3 on ttt_aid_virt0 (is_uchet) ", true);
            ExecSQL(conn_db, " Create index ix_ttt_aid_virt4 on ttt_aid_virt0 (nzp_counter) ", true);
            ExecSQL(conn_db, sUpdStat + " ttt_aid_virt0 ", true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            return true;
        }

        // выбрать расчет по установленным параметрам расчета ОДН дома
        public bool Stek354UchetParams(IDbConnection conn_db, Rashod rashod, out Returns ret)
        //--------------------------------------------------------------------------------
        {
            ret = Utils.InitReturns();

            // HV формула 11 - Пост.№ 354: val_prm=3 - Пост354 / val_prm=5 - для Тулы ограничение на ИПУ по норме
            ret = ExecSQL(conn_db,
                " update ttt_aid_virt0 set " +
                " is_uchet = 1" +
                " Where nzp_serv=6 and stek=354 and kod_info in (21,31) " +
                " and 0 < ( Select count(*) From ttt_prm_2 p " +
                          " Where ttt_aid_virt0.nzp_dom  = p.nzp and p.nzp_prm = 2031 " +
                          "   and p.val_prm in ('3','5') " +
                          " ) "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            // HV формула 11 - Пост.№ 354 с вычетом ГВС
            ret = ExecSQL(conn_db,
                " update ttt_aid_virt0 set " +
                " is_uchet = 1" +
                " Where nzp_serv=6 and stek=354 and kod_info in (26,36) " +
                " and 0 < ( Select count(*) From ttt_prm_2 p " +
                          " Where ttt_aid_virt0.nzp_dom  = p.nzp and p.nzp_prm = 2031 " +
                          "   and p.val_prm='4' " +
                          " ) "
                , true);
            if (!ret.result)
            {
                DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                return false;
            }
            // HV<-GV формула 20 - Пост.№ 354
            ret = ExecSQL(conn_db,
                " update ttt_aid_virt0 set " +
                " is_uchet = 1" +
                " Where nzp_serv=6 and stek=3540 " +
                " and 0 < ( Select count(*) From ttt_prm_2 p " +
                          " Where ttt_aid_virt0.nzp_dom  = p.nzp and p.nzp_prm = 2035 " +
                          "   and p.val_prm='4' " +
                          " ) "
                , true);
            if (!ret.result)
            {
                DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                return false;
            }
            // GV формула 11 - Пост.№ 354
            ret = ExecSQL(conn_db,
                " update ttt_aid_virt0 set " +
                " is_uchet = 1" +
                " Where nzp_serv=9 and stek=354 " +
                " and 0 < ( Select count(*) From ttt_prm_2 p " +
                          " Where ttt_aid_virt0.nzp_dom  = p.nzp and p.nzp_prm = 2035 " +
                          "   and p.val_prm='3' " +
                          " ) "
                , true);
            if (!ret.result)
            {
                DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                return false;
            }
            // GV формула 20 - Пост.№ 354
            ret = ExecSQL(conn_db,
                " update ttt_aid_virt0 set " +
                " is_uchet = 1" +
                " Where nzp_serv=9 and stek=3540 " +
                " and 0 < ( Select count(*) From ttt_prm_2 p " +
                          " Where ttt_aid_virt0.nzp_dom  = p.nzp and p.nzp_prm = 2035 " +
                          "   and p.val_prm='4' " +
                          " ) "
                , true);
            if (!ret.result)
            {
                DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                return false;
            }
            // ElEn формула 11 - Пост.№ 354
            ret = ExecSQL(conn_db,
                " update ttt_aid_virt0 set " +
                " is_uchet = 1" +
                " Where nzp_serv=25 and stek=354 and kod_info in (21,31) " +
                " and 0 < ( Select count(*) From ttt_prm_2 p " +
                          " Where ttt_aid_virt0.nzp_dom  = p.nzp and p.nzp_prm = 2039 " +
                          "   and p.val_prm='3' " +
                          " ) "
                , true);
            if (!ret.result)
            {
                DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                return false;
            }
            // ElEn формула 11 - Пост.№ 354 -> 2-х тарифный
            ret = ExecSQL(conn_db,
                " update ttt_aid_virt0 set " +
                " is_uchet = 1" +
                " Where nzp_serv=25 and stek=354 and kod_info in (22,32) " +
                " and 0 < ( Select count(*) From ttt_prm_2 p " +
                          " Where ttt_aid_virt0.nzp_dom  = p.nzp and p.nzp_prm = 2039 " +
                          "   and p.val_prm='4' " +
                          " ) "
                , true);
            if (!ret.result)
            {
                DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                return false;
            }
            // ElEn формула 11 - Пост.№ 354 -> 2-х тарифный
            ret = ExecSQL(conn_db,
                " update ttt_aid_virt0 set " +
                " is_uchet = 1" +
                " Where nzp_serv=210 and stek=354 " +
                " and 0 < ( Select count(*) From ttt_prm_2 p " +
                          " Where ttt_aid_virt0.nzp_dom  = p.nzp and p.nzp_prm = 2039 " +
                          "   and p.val_prm='4'  " +
                          " ) "
                , true);
            if (!ret.result)
            {
                DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                return false;
            }
            // ElEn формула 11 - Пост.№ 354 -> 3-х тарифный
            ret = ExecSQL(conn_db,
                " update ttt_aid_virt0 set " +
                " is_uchet = 1" +
                " Where nzp_serv=410 and stek=354 " +
                " and 0 < ( Select count(*) From ttt_prm_2 p " +
                          " Where ttt_aid_virt0.nzp_dom  = p.nzp and p.nzp_prm = 2039 " +
                          "   and p.val_prm='4'  " +
                          " ) "
                , true);
            if (!ret.result)
            {
                DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                return false;
            }
            // Otopl формула 11 - Пост.№ 354
            ret = ExecSQL(conn_db,
                " update ttt_aid_virt0 set " +
                " is_uchet = 1" +
                " Where nzp_serv=8 and stek=354 " +
                " and 0 < ( Select count(*) From ttt_prm_2 p " +
                          " Where ttt_aid_virt0.nzp_dom  = p.nzp and p.nzp_prm = 2045 " +
                          "   and p.val_prm='3' " +
                          " ) "
                , true);
            if (!ret.result)
            {
                DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                return false;
            }
            // Otopl формула 18 - Пост.№ 354
            ret = ExecSQL(conn_db,
                " update ttt_aid_virt0 set " +
                " is_uchet = 1" +
                " Where nzp_serv=8 and stek=3540 " +
                " and 0 < ( Select count(*) From ttt_prm_2 p " +
                          " Where ttt_aid_virt0.nzp_dom  = p.nzp and p.nzp_prm = 2045 " +
                          "   and p.val_prm='4' " +
                          " ) "
                , true);
            if (!ret.result)
            {
                DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                return false;
            }
            // GAS формула 11 - Пост.№ 354
            ret = ExecSQL(conn_db,
                " update ttt_aid_virt0 set " +
                " is_uchet = 1" +
                " Where nzp_serv=10 and stek=354 " +
                " and 0 < ( Select count(*) From ttt_prm_2 p " +
                          " Where ttt_aid_virt0.nzp_dom  = p.nzp and p.nzp_prm = 2062 " +
                          "   and p.val_prm='3' " +
                          " ) "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            return true;
        }

        // сохранить учет -> Пост.№ 354
        public bool Stek354SaveForUchet(IDbConnection conn_db, Rashod rashod, string pType, out Returns ret)
        //--------------------------------------------------------------------------------
        {
            ret = Utils.InitReturns();

            string
                sJoinSQL = rashod.counters_xx + ".nzp_dom  = v.nzp_dom and " + rashod.counters_xx + ".nzp_serv  = v.nzp_serv";
            if (pType == "2")
            {
                sJoinSQL = rashod.counters_xx + ".nzp_counter  = v.nzp_counter";
            }

            ret = ExecSQL(conn_db,
                " Update " + rashod.counters_xx + " Set " +
#if PG
 " kf307       = (select v.kf307       from ttt_aid_virt0 v where v.is_uchet=1 and " + sJoinSQL + ")" +
                ",kf307n      = (select v.kf307n      from ttt_aid_virt0 v where v.is_uchet=1 and " + sJoinSQL + ")" +
                ",kf307f9     = (select v.kf307f9     from ttt_aid_virt0 v where v.is_uchet=1 and " + sJoinSQL + ")" +
                ",val3        = (select v.val3        from ttt_aid_virt0 v where v.is_uchet=1 and " + sJoinSQL + ")" +
                ",val4        = (select v.val4        from ttt_aid_virt0 v where v.is_uchet=1 and " + sJoinSQL + ")" +
                ",vl210       = (select v.vl210       from ttt_aid_virt0 v where v.is_uchet=1 and " + sJoinSQL + ")" +
                ",dlt_cur     = (select v.dlt_cur     from ttt_aid_virt0 v where v.is_uchet=1 and " + sJoinSQL + ")" +
                ",kod_info    = (select v.kod_info    from ttt_aid_virt0 v where v.is_uchet=1 and " + sJoinSQL + ")" +
                ",val1        = (select v.val1        from ttt_aid_virt0 v where v.is_uchet=1 and " + sJoinSQL + ")" +
                ",val2        = (select v.val2        from ttt_aid_virt0 v where v.is_uchet=1 and " + sJoinSQL + ")" +
                ",squ1        = (select v.squ1        from ttt_aid_virt0 v where v.is_uchet=1 and " + sJoinSQL + ")" +
                ",squ2        = (select v.squ2        from ttt_aid_virt0 v where v.is_uchet=1 and " + sJoinSQL + ")" +
                ",gil1        = (select v.gil1        from ttt_aid_virt0 v where v.is_uchet=1 and " + sJoinSQL + ")" +
                ",cnt1        = (select round(v.gil1) from ttt_aid_virt0 v where v.is_uchet=1 and " + sJoinSQL + ")" +
                ",rvirt       = (select v.rvirt       from ttt_aid_virt0 v where v.is_uchet=1 and " + sJoinSQL + ")" +
                ",pu7kw       = (select v.pu7kw       from ttt_aid_virt0 v where v.is_uchet=1 and " + sJoinSQL + ")" +
                ",gl7kw       = (select v.gl7kw       from ttt_aid_virt0 v where v.is_uchet=1 and " + sJoinSQL + ")" +
                ",kf_dpu_kg   = (select v.kf_dpu_kg   from ttt_aid_virt0 v where v.is_uchet=1 and " + sJoinSQL + ")" +
                ",kf_dpu_plob = (select v.kf_dpu_plob from ttt_aid_virt0 v where v.is_uchet=1 and " + sJoinSQL + ")" +
                ",kf_dpu_ls   = (select v.kf_dpu_ls   from ttt_aid_virt0 v where v.is_uchet=1 and " + sJoinSQL + ")" +
                ",dlt_reval   = (select v.dlt_reval   from ttt_aid_virt0 v where v.is_uchet=1 and " + sJoinSQL + ")" +
                ",dlt_real_charge = (select v.dlt_real_charge from ttt_aid_virt0 v where v.is_uchet=1 and " + sJoinSQL + ")" +
                ",val4_source = (select v.val4_source     from ttt_aid_virt0 v where v.is_uchet=1 and " + sJoinSQL + ")" +
                ",up_kf = (select v.up_kf  from ttt_aid_virt0 v where v.is_uchet=1 and " + sJoinSQL + ")" +
#else
                " (kf307,kf307n,kf307f9,val3,val4,vl210,dlt_cur,kod_info ,val1,val2,squ1,squ2,gil1,cnt1," +
                " rvirt,pu7kw,gl7kw,kf_dpu_kg,kf_dpu_plob,kf_dpu_ls,dlt_reval,dlt_real_charge,val1_source) = " +
                " ((select v.kf307,v.kf307n,v.kf307f9,v.val3,v.val4,v.vl210,v.dlt_cur,v.kod_info ,v.val1,v.val2,v.squ1,v.squ2,v.gil1,round(v.gil1)," +
                  "v.rvirt,v.pu7kw,v.gl7kw,v.kf_dpu_kg,v.kf_dpu_plob,v.kf_dpu_ls,v.dlt_reval,v.dlt_real_charge,v.val4_source, v.up_kf" +
                "   from ttt_aid_virt0 v where v.is_uchet=1 and " + sJoinSQL + 
                " ))" +
#endif
 " Where nzp_type = " + pType + " and stek = 3 " +
                "   and exists (select 1 " +
                "   from ttt_aid_virt0 v where v.is_uchet=1 and " + sJoinSQL +
                " )"
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            return true;
        }

        // учет -> Пост.№ 354
        public bool Stek354UchetInDom(IDbConnection conn_db, Rashod rashod, bool bIsCalcODN, bool bIsCalcCurMonth, out Returns ret)
        //--------------------------------------------------------------------------------
        {
            ret = Utils.InitReturns();

            if (bIsCalcODN || bIsCalcCurMonth)
            {
                // учет -> Пост.№ 354 по ОДПУ

                //-------------------------------------------------------------------
                // выбрать результат расчета из стека 354 / 3540
                // выборка ttt_aid_virt0 - рассчитанных ранее Пост.354 домов
                //-------------------------------------------------------------------
                Stek354SelForUchet(conn_db, rashod, "1", out ret);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                // выбрать расчет по установленным параметрам расчета ОДН дома
                Stek354UchetParams(conn_db, rashod, out ret);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                // сохранить учет -> Пост.№ 354
                Stek354SaveForUchet(conn_db, rashod, "1", out ret);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                // учет -> Пост.№ 354 по ГрПУ

                //-------------------------------------------------------------------
                // выбрать результат расчета из стека 354 / 3540
                // выборка ttt_aid_virt0 - рассчитанных ранее Пост.354 ГрПУ
                //-------------------------------------------------------------------
                Stek354SelForUchet(conn_db, rashod, "2", out ret);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                // выбрать расчет по установленным параметрам расчета ОДН дома
                Stek354UchetParams(conn_db, rashod, out ret);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                // сохранить учет -> Пост.№ 354
                Stek354SaveForUchet(conn_db, rashod, "2", out ret);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                ExecSQL(conn_db, " Drop table ttt_aid_virt0 ", false);
            }
            return true;
        }
        #endregion учет ОДН по 354 постановлению в стек = 3 & nzp_type = 1 & 2

        #region учет ОДН по 307 постановлению в стек = 3 and nzp_type in (1,2)
        public bool Stek3Uchet307InDom(IDbConnection conn_db, Rashod rashod, bool bIsCalcODN, bool bIsCalcCurMonth, out Returns ret)
        //--------------------------------------------------------------------------------
        {
            ret = Utils.InitReturns();

            string sql = "";

            if (bIsCalcODN || bIsCalcCurMonth)
            {
                //------------------------------------------------------------------
                // Пост.№ 307
                //------------------------------------------------------------------
                // hvs - odn
                sql =
                        " Update " + rashod.counters_xx +
                        " Set kf307 = kf307f9, kf307n = kf307f9, kod_info = 6 " +
                        " Where kf307f9>0 and nzp_type in (1,2) and stek = 3 and nzp_serv=6 and " + rashod.where_dom + rashod.paramcalc.per_dat_charge +
                          " and 0 < ( Select count(*) From ttt_prm_2 p " +
                                    " Where " + rashod.counters_xx + ".nzp_dom  = p.nzp and p.nzp_prm = 2031 and p.val_prm='1' " +
                                    " ) " +
                          " and ( kf307f9>1 or ( kf307f9<1 and 0 = ( Select count(*) From ttt_prm_2 p " +
                                    " Where " + rashod.counters_xx + ".nzp_dom  = p.nzp and p.nzp_prm = 2032 " +
                                    " ) ) ) ";

                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                // сохранить коэф=1 если указано, что не применять меньше 0
                sql =
                        " Update " + rashod.counters_xx +
                        " Set kf307 = 1, kf307n = 1, kod_info = 6 " +
                        " Where kf307f9>0 and nzp_type in (1,2) and stek = 3 and nzp_serv=6 and " + rashod.where_dom + rashod.paramcalc.per_dat_charge +
                          " and 0 < ( Select count(*) From ttt_prm_2 p " +
                                    " Where " + rashod.counters_xx + ".nzp_dom  = p.nzp and p.nzp_prm = 2031 and p.val_prm='1' " +
                                    " ) " +
                          " and kf307f9<1 and 0 < ( Select count(*) From ttt_prm_2 p " +
                                    " Where " + rashod.counters_xx + ".nzp_dom  = p.nzp and p.nzp_prm = 2032 " +
                                    " ) ";
                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                // gvs - odn
                sql =
                        " Update " + rashod.counters_xx +
                        " Set kf307 = kf307f9, kf307n = kf307f9, kod_info = 6 " +
                        " Where kf307f9>0 and nzp_type in (1,2) and stek = 3 and nzp_serv=9 and " + rashod.where_dom + rashod.paramcalc.per_dat_charge +
                          " and 0 < ( Select count(*) From ttt_prm_2 p " +
                                    " Where " + rashod.counters_xx + ".nzp_dom  = p.nzp and p.nzp_prm = 2035 and p.val_prm='1' " +
                                    " ) " +
                          " and ( kf307f9>1 or ( kf307f9<1 and 0 = ( Select count(*) From ttt_prm_2 p " +
                                    " Where " + rashod.counters_xx + ".nzp_dom  = p.nzp and p.nzp_prm = 2036 " +
                                    " ) ) ) ";

                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                // сохранить коэф=1 если указано, что не применять меньше 0
                sql =
                        " Update " + rashod.counters_xx +
                        " Set kf307 = 1, kf307n = 1, kod_info = 6 " +
                        " Where kf307f9>0 and nzp_type in (1,2) and stek = 3 and nzp_serv=9 and " + rashod.where_dom + rashod.paramcalc.per_dat_charge +
                          " and 0 < ( Select count(*) From ttt_prm_2 p " +
                                    " Where " + rashod.counters_xx + ".nzp_dom  = p.nzp and p.nzp_prm = 2035 and p.val_prm='1' " +
                                    " ) " +
                          " and kf307f9<1 and 0 < ( Select count(*) From ttt_prm_2 p " +
                                    " Where " + rashod.counters_xx + ".nzp_dom  = p.nzp and p.nzp_prm = 2036 " +
                                    " ) ";

                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                // el/en - odn
                sql =
                        " Update " + rashod.counters_xx +
                        " Set kf307 = kf307f9, kf307n = kf307f9, kod_info = 6 " +
                        " Where kf307f9>0 and nzp_type in (1,2) and stek = 3 and nzp_serv=25 and " + rashod.where_dom + rashod.paramcalc.per_dat_charge +
                          " and 0 < ( Select count(*) From ttt_prm_2 p " +
                                    " Where " + rashod.counters_xx + ".nzp_dom  = p.nzp and p.nzp_prm = 2039 and p.val_prm='1' " +
                                    " ) " +
                          " and ( kf307f9>1 or ( kf307f9<1 and 0 = ( Select count(*) From ttt_prm_2 p " +
                                    " Where " + rashod.counters_xx + ".nzp_dom  = p.nzp and p.nzp_prm = 2040 " +
                                    " ) ) ) ";

                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                // сохранить коэф=1 если указано, что не применять меньше 0
                sql =
                        " Update " + rashod.counters_xx +
                        " Set kf307 = 1, kf307n = 1, kod_info = 6 " +
                        " Where kf307f9>0 and nzp_type in (1,2) and stek = 3 and nzp_serv=25 and " + rashod.where_dom + rashod.paramcalc.per_dat_charge +
                          " and 0 < ( Select count(*) From ttt_prm_2 p " +
                                    " Where " + rashod.counters_xx + ".nzp_dom  = p.nzp and p.nzp_prm = 2039 and p.val_prm='1' " +
                                    " ) " +
                          " and kf307f9<1 and 0 < ( Select count(*) From ttt_prm_2 p " +
                                    " Where " + rashod.counters_xx + ".nzp_dom  = p.nzp and p.nzp_prm = 2040 " +
                                    " ) ";

                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                // gas - odn
                sql =
                        " Update " + rashod.counters_xx +
                        " Set kf307 = kf307f9, kf307n = kf307f9, kod_info = 6 " +
                        " Where kf307f9>0 and nzp_type in (1,2) and stek = 3 and nzp_serv=10 and " + rashod.where_dom + rashod.paramcalc.per_dat_charge +
                          " and 0 < ( Select count(*) From ttt_prm_2 p " +
                                    " Where " + rashod.counters_xx + ".nzp_dom  = p.nzp and p.nzp_prm = 2062 and p.val_prm='1' " +
                                    " ) " +
                          " and ( kf307f9>1 or ( kf307f9<1 and 0 = ( Select count(*) From ttt_prm_2 p " +
                                    " Where " + rashod.counters_xx + ".nzp_dom  = p.nzp and p.nzp_prm = 2063 " +
                                    " ) ) ) ";

                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                // сохранить коэф=1 если указано, что не применять меньше 0
                sql =
                        " Update " + rashod.counters_xx +
                        " Set kf307 = 1, kf307n = 1, kod_info = 6 " +
                        " Where kf307f9>0 and nzp_type in (1,2) and stek = 3 and nzp_serv=10 and " + rashod.where_dom + rashod.paramcalc.per_dat_charge +
                          " and 0 < ( Select count(*) From ttt_prm_2 p " +
                                    " Where " + rashod.counters_xx + ".nzp_dom  = p.nzp and p.nzp_prm = 2062 and p.val_prm='1' " +
                                    " ) " +
                          " and kf307f9<1 and 0 < ( Select count(*) From ttt_prm_2 p " +
                                    " Where " + rashod.counters_xx + ".nzp_dom  = p.nzp and p.nzp_prm = 2063 " +
                                    " ) ";

                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                // kan - odn
                sql =
                        " Update " + rashod.counters_xx +
                        " Set kf307 = kf307f9, kf307n = kf307f9, kod_info = 6 " +
                        " Where kf307f9>0 and nzp_type in (1,2) and stek = 3 and nzp_serv in (7,324) and " + rashod.where_dom + rashod.paramcalc.per_dat_charge +
                          " and 0 < ( Select count(*) From ttt_prm_2 p " +
                                    " Where " + rashod.counters_xx + ".nzp_dom  = p.nzp and p.nzp_prm = 2091 and p.val_prm='1' " +
                                    " ) " +
                          " and ( kf307f9>1 or ( kf307f9<1 and 0 = ( Select count(*) From ttt_prm_2 p " +
                                    " Where " + rashod.counters_xx + ".nzp_dom  = p.nzp and p.nzp_prm = 2092 " +
                                    " ) ) ) ";

                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                // сохранить коэф=1 если указано, что не применять меньше 0
                sql =
                        " Update " + rashod.counters_xx +
                        " Set kf307 = 1, kf307n = 1, kod_info = 6 " +
                        " Where kf307f9>0 and nzp_type in (1,2) and stek = 3 and nzp_serv in (7,324) and " + rashod.where_dom + rashod.paramcalc.per_dat_charge +
                          " and 0 < ( Select count(*) From ttt_prm_2 p " +
                                    " Where " + rashod.counters_xx + ".nzp_dom  = p.nzp and p.nzp_prm = 2091 and p.val_prm='1' " +
                                    " ) " +
                          " and kf307f9<1 and 0 < ( Select count(*) From ttt_prm_2 p " +
                                    " Where " + rashod.counters_xx + ".nzp_dom  = p.nzp and p.nzp_prm = 2092 " +
                                    " ) ";

                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                // kan - odn
                sql =
                        " Update " + rashod.counters_xx +
                        " Set kf307 = kf307f9, kf307n = kf307f9, kod_info = 6 " +
                        " Where kf307f9>0 and nzp_type in (1,2) and stek = 3 and nzp_serv in (200) and " + rashod.where_dom + rashod.paramcalc.per_dat_charge +
                          " and 0 < ( Select count(*) From ttt_prm_2 p " +
                                    " Where " + rashod.counters_xx + ".nzp_dom  = p.nzp and p.nzp_prm = 2102 and p.val_prm='1' " +
                                    " ) " +
                          " and ( kf307f9>1 or ( kf307f9<1 and 0 = ( Select count(*) From ttt_prm_2 p " +
                                    " Where " + rashod.counters_xx + ".nzp_dom  = p.nzp and p.nzp_prm = 2103 " +
                                    " ) ) ) ";

                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                // сохранить коэф=1 если указано, что не применять меньше 0
                sql =
                        " Update " + rashod.counters_xx +
                        " Set kf307 = 1, kf307n = 1, kod_info = 6 " +
                        " Where kf307f9>0 and nzp_type in (1,2) and stek = 3 and nzp_serv in (200) and " + rashod.where_dom + rashod.paramcalc.per_dat_charge +
                          " and 0 < ( Select count(*) From ttt_prm_2 p " +
                                    " Where " + rashod.counters_xx + ".nzp_dom  = p.nzp and p.nzp_prm = 2102 and p.val_prm='1' " +
                                    " ) " +
                          " and kf307f9<1 and 0 < ( Select count(*) From ttt_prm_2 p " +
                                    " Where " + rashod.counters_xx + ".nzp_dom  = p.nzp and p.nzp_prm = 2103 " +
                                    " ) ";

                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                // hvs - dpu kg
                sql =
                        " Update " + rashod.counters_xx +
                        " Set kf307 = kf_dpu_kg, kf307n = kf_dpu_kg, kod_info = 101 " +
                        " Where (kf_dpu_kg>0 or (abs(kf_dpu_kg)<0.000001 and cnt_stage in (1,9))) and nzp_type in (1,2) and stek = 3 and nzp_serv=6 and " + rashod.where_dom + rashod.paramcalc.per_dat_charge +
                          " and 0 < ( Select count(*) From ttt_prm_2 p " +
                                    " Where " + rashod.counters_xx + ".nzp_dom  = p.nzp and p.nzp_prm = 2031 and p.val_prm='2' " +
                                    " ) ";

                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                // gvs - dpu kg
                sql =
                        " Update " + rashod.counters_xx +
                        " Set kf307 = kf_dpu_kg, kf307n = kf_dpu_kg, kod_info = 101 " +
                        " Where (kf_dpu_kg>0 or (abs(kf_dpu_kg)<0.000001 and cnt_stage in (1,9))) and nzp_type in (1,2) and stek = 3 and nzp_serv=9 and " + rashod.where_dom + rashod.paramcalc.per_dat_charge +
                          " and 0 < ( Select count(*) From ttt_prm_2 p " +
                                    " Where " + rashod.counters_xx + ".nzp_dom  = p.nzp and p.nzp_prm = 2035 and p.val_prm='2' " +
                                    " ) ";

                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                // el/en - dpu kg
                sql =
                        " Update " + rashod.counters_xx +
                        " Set kf307 = kf_dpu_kg, kf307n = kf_dpu_kg, kod_info = 101 " +
                        " Where (kf_dpu_kg>0 or (abs(kf_dpu_kg)<0.000001 and cnt_stage in (1,9))) and nzp_type in (1,2) and stek = 3 and nzp_serv=25 and " + rashod.where_dom + rashod.paramcalc.per_dat_charge +
                          " and 0 < ( Select count(*) From ttt_prm_2 p " +
                                    " Where " + rashod.counters_xx + ".nzp_dom  = p.nzp and p.nzp_prm = 2039 and p.val_prm='2' " +
                                    " ) ";

                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                // gas - dpu kg
                sql =
                        " Update " + rashod.counters_xx +
                        " Set kf307 = kf_dpu_kg, kf307n = kf_dpu_kg, kod_info = 101 " +
                        " Where (kf_dpu_kg>0 or (abs(kf_dpu_kg)<0.000001 and cnt_stage in (1,9))) and nzp_type in (1,2) and stek = 3 and nzp_serv=10 and " + rashod.where_dom + rashod.paramcalc.per_dat_charge +
                          " and 0 < ( Select count(*) From ttt_prm_2 p " +
                                    " Where " + rashod.counters_xx + ".nzp_dom  = p.nzp and p.nzp_prm = 2062 and p.val_prm='2' " +
                                    " ) ";

                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                // kan - dpu kg
                sql =
                        " Update " + rashod.counters_xx +
                        " Set kf307 = kf_dpu_kg, kf307n = kf_dpu_kg, kod_info = 101 " +
                        " Where (kf_dpu_kg>0 or (abs(kf_dpu_kg)<0.000001 and cnt_stage in (1,9))) and nzp_type in (1,2) and stek = 3 and nzp_serv in (7,324) and " + rashod.where_dom + rashod.paramcalc.per_dat_charge +
                          " and 0 < ( Select count(*) From ttt_prm_2 p " +
                                    " Where " + rashod.counters_xx + ".nzp_dom  = p.nzp and p.nzp_prm = 2091 and p.val_prm='2' " +
                                    " ) ";

                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                // полив - dpu kg
                sql =
                        " Update " + rashod.counters_xx +
                        " Set kf307 = kf_dpu_kg, kf307n = kf_dpu_kg, kod_info = 101 " +
                        " Where (kf_dpu_kg>0 or (abs(kf_dpu_kg)<0.000001 and cnt_stage in (1,9))) and nzp_type in (1,2) and stek = 3 and nzp_serv in (200) and " + rashod.where_dom + rashod.paramcalc.per_dat_charge +
                          " and 0 < ( Select count(*) From ttt_prm_2 p " +
                                    " Where " + rashod.counters_xx + ".nzp_dom  = p.nzp and p.nzp_prm = 2102 and p.val_prm='1' " +
                                    " ) ";

                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                // hvs - dpu plos
                sql =
                        " Update " + rashod.counters_xx +
                        " Set kf307 = kf_dpu_plob, kf307n = kf_dpu_plob, kod_info = 102 " +
                        " Where (kf_dpu_plob>0 or (abs(kf_dpu_plob)<0.000001 and cnt_stage in (1,9))) and nzp_type in (1,2) and stek = 3 and nzp_serv=6 and " + rashod.where_dom + rashod.paramcalc.per_dat_charge +
                          " and 0 < ( Select count(*) From ttt_prm_2 p " +
                                    " Where " + rashod.counters_xx + ".nzp_dom  = p.nzp and p.nzp_prm = 2031 and p.val_prm='2' " +
                                    " ) " +
                          " and 0 < ( Select count(*) From ttt_prm_2 p " + //rashod.paramcalc.pref + "_data:prm_2 p " +
                                    " Where " + rashod.counters_xx + ".nzp_dom  = p.nzp and p.nzp_prm = 2034 and p.val_prm='2' " +
                                    " ) ";

                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                // gvs - dpu plos
                sql =
                        " Update " + rashod.counters_xx +
                        " Set kf307 = kf_dpu_plob, kf307n = kf_dpu_plob, kod_info = 102 " +
                        " Where (kf_dpu_plob>0 or (abs(kf_dpu_plob)<0.000001 and cnt_stage in (1,9))) and nzp_type in (1,2) and stek = 3 and nzp_serv=9 and " + rashod.where_dom + rashod.paramcalc.per_dat_charge +
                          " and 0 < ( Select count(*) From ttt_prm_2 p " +
                                    " Where " + rashod.counters_xx + ".nzp_dom  = p.nzp and p.nzp_prm = 2035 and p.val_prm='2' " +
                                    " ) " +
                          " and 0 < ( Select count(*) From ttt_prm_2 p " + //rashod.paramcalc.pref + "_data:prm_2 p " +
                                    " Where " + rashod.counters_xx + ".nzp_dom  = p.nzp and p.nzp_prm = 2038 and p.val_prm='2' " +
                                    " ) ";

                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                // el/en - dpu plos
                sql =
                        " Update " + rashod.counters_xx +
                        " Set kf307 = kf_dpu_plob, kf307n = kf_dpu_plob, kod_info = 102 " +
                        " Where (kf_dpu_plob>0 or (abs(kf_dpu_plob)<0.000001 and cnt_stage in (1,9))) and nzp_type in (1,2) and stek = 3 and nzp_serv=25 and " + rashod.where_dom + rashod.paramcalc.per_dat_charge +
                          " and 0 < ( Select count(*) From ttt_prm_2 p " +
                                    " Where " + rashod.counters_xx + ".nzp_dom  = p.nzp and p.nzp_prm = 2039 and p.val_prm='2' " +
                                    " ) " +
                          " and 0 < ( Select count(*) From ttt_prm_2 p " + //rashod.paramcalc.pref + "_data:prm_2 p " +
                                    " Where " + rashod.counters_xx + ".nzp_dom  = p.nzp and p.nzp_prm = 2042 and p.val_prm='2' " +
                                    " ) ";

                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            }
            //if (bIsCalcODN || bIsCalcCurMonth) - для отопления пускать всегда! м.б. перерасчет по итогам года
            {
                // otopl - dpu plos. Для отопления только по площади !!!
                sql =
                        " Update " + rashod.counters_xx +
                        " Set kf307 = kf_dpu_plot, kf307n = kf_dpu_plot, kod_info = 102 " +
                        " Where (kf_dpu_plot>0 or (abs(kf_dpu_plot)<0.000001 and cnt_stage in (1,9))) and nzp_type in (1,2) and stek = 3 and nzp_serv=8 and " + rashod.where_dom + rashod.paramcalc.per_dat_charge +
                          " and exists ( Select 1 From ttt_prm_2 p " +
                                    " Where " + rashod.counters_xx + ".nzp_dom  = p.nzp and p.nzp_prm = 2045 and p.val_prm='2' " +
                                    " ) " +
                          " and exists ( Select 1 From ttt_prm_2 p " +
                                    " Where " + rashod.counters_xx + ".nzp_dom  = p.nzp and p.nzp_prm = 2048 and p.val_prm='3' " +
                                    " ) ";

                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                // gasOt/eeOt - dpu ob.plos
                sql =
                        " Update " + rashod.counters_xx +
                        " Set kf307 = kf_dpu_plob, kf307n = kf_dpu_plob, kod_info = 102 " +
                        " Where (kf_dpu_plob>0 or (abs(kf_dpu_plob)<0.000001 and cnt_stage in (1,9))) and nzp_type in (1,2) and stek = 3 and nzp_serv in (322,325) and " + rashod.where_dom + rashod.paramcalc.per_dat_charge +
                          " and exists ( Select 1 From ttt_prm_2 p " +
                                    " Where " + rashod.counters_xx + ".nzp_dom  = p.nzp and p.nzp_prm = 2045 and p.val_prm='2' " +
                                    " ) " +
                          " and exists ( Select 1 From ttt_prm_2 p " +
                                    " Where " + rashod.counters_xx + ".nzp_dom  = p.nzp and p.nzp_prm = 2048 and p.val_prm='2' " +
                                    " ) ";

                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                // gasOt/eeOt - dpu ot.plos
                sql =
                        " Update " + rashod.counters_xx +
                        " Set kf307 = kf_dpu_plob, kf307n = kf_dpu_plob, kod_info = 102 " +
                        " Where (kf_dpu_plob>0 or (abs(kf_dpu_plob)<0.000001 and cnt_stage in (1,9))) and nzp_type in (1,2) and stek = 3 and nzp_serv in (322,325) and " + rashod.where_dom + rashod.paramcalc.per_dat_charge +
                          " and exists ( Select 1 From ttt_prm_2 p " +
                                    " Where " + rashod.counters_xx + ".nzp_dom  = p.nzp and p.nzp_prm = 2045 and p.val_prm='2' " +
                                    " ) " +
                          " and exists ( Select 1 From ttt_prm_2 p " +
                                    " Where " + rashod.counters_xx + ".nzp_dom  = p.nzp and p.nzp_prm = 2048 and p.val_prm='3' " +
                                    " ) ";

                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }
            }
            if (bIsCalcODN || bIsCalcCurMonth)
            {
                // gas - dpu plos
                sql =
                        " Update " + rashod.counters_xx +
                        " Set kf307 = kf_dpu_plob, kf307n = kf_dpu_plob, kod_info = 102 " +
                        " Where (kf_dpu_plob>0 or (abs(kf_dpu_plob)<0.000001 and cnt_stage in (1,9))) and nzp_type in (1,2) and stek = 3 and nzp_serv=10 and " + rashod.where_dom + rashod.paramcalc.per_dat_charge +
                          " and 0 < ( Select count(*) From ttt_prm_2 p " +
                                    " Where " + rashod.counters_xx + ".nzp_dom  = p.nzp and p.nzp_prm = 2062 and p.val_prm='2' " +
                                    " ) " +
                          " and 0 < ( Select count(*) From ttt_prm_2 p " +
                                    " Where " + rashod.counters_xx + ".nzp_dom  = p.nzp and p.nzp_prm = 2065 and p.val_prm='2' " +
                                    " ) ";

                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                // kan - dpu plos
                sql =
                        " Update " + rashod.counters_xx +
                        " Set kf307 = kf_dpu_plob, kf307n = kf_dpu_plob, kod_info = 102 " +
                        " Where (kf_dpu_plob>0 or (abs(kf_dpu_plob)<0.000001 and cnt_stage in (1,9))) and nzp_type in (1,2) and stek = 3 and nzp_serv in (7,324) and " + rashod.where_dom + rashod.paramcalc.per_dat_charge +
                          " and 0 < ( Select count(*) From ttt_prm_2 p " +
                                    " Where " + rashod.counters_xx + ".nzp_dom  = p.nzp and p.nzp_prm = 2091 and p.val_prm='2' " +
                                    " ) " +
                          " and 0 < ( Select count(*) From ttt_prm_2 p " +
                                    " Where " + rashod.counters_xx + ".nzp_dom  = p.nzp and p.nzp_prm = 2094 and p.val_prm='2' " +
                                    " ) ";

                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                // полив - dpu plos
                sql =
                        " Update " + rashod.counters_xx +
                        " Set kf307 = kf_dpu_plob, kf307n = kf_dpu_plob, kod_info = 102 " +
                        " Where (kf_dpu_plob>0 or (abs(kf_dpu_plob)<0.000001 and cnt_stage in (1,9))) and nzp_type in (1,2) and stek = 3 and nzp_serv in (200) and " + rashod.where_dom + rashod.paramcalc.per_dat_charge +
                          " and 0 < ( Select count(*) From ttt_prm_2 p " +
                                    " Where " + rashod.counters_xx + ".nzp_dom  = p.nzp and p.nzp_prm = 2102 and p.val_prm='1' " +
                                    " ) " +
                          " and 0 < ( Select count(*) From ttt_prm_2 p " +
                                    " Where " + rashod.counters_xx + ".nzp_dom  = p.nzp and p.nzp_prm = 2105 and p.val_prm='2' " +
                                    " ) ";

                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                // hvs - dpu ls
                sql =
                        " Update " + rashod.counters_xx +
                        " Set kf307 = kf_dpu_ls, kf307n = kf_dpu_ls, kod_info = 104 " +
                        " Where (kf_dpu_ls>0 or (abs(kf_dpu_ls)<0.000001 and cnt_stage in (1,9))) and nzp_type in (1,2) and stek = 3 and nzp_serv=6 and " + rashod.where_dom + rashod.paramcalc.per_dat_charge +
                          " and 0 < ( Select count(*) From ttt_prm_2 p " +
                                    " Where " + rashod.counters_xx + ".nzp_dom  = p.nzp and p.nzp_prm = 2031 and p.val_prm='2' " +
                                    " ) " +
                          " and 0 < ( Select count(*) From ttt_prm_2 p " +
                                    " Where " + rashod.counters_xx + ".nzp_dom  = p.nzp and p.nzp_prm = 2034 and p.val_prm='4' " +
                                    " ) ";

                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                // gvs - dpu ls
                sql =
                        " Update " + rashod.counters_xx +
                        " Set kf307 = kf_dpu_ls, kf307n = kf_dpu_ls, kod_info = 104 " +
                        " Where (kf_dpu_ls>0 or (abs(kf_dpu_ls)<0.000001 and cnt_stage in (1,9))) and nzp_type in (1,2) and stek = 3 and nzp_serv=9 and " + rashod.where_dom + rashod.paramcalc.per_dat_charge +
                          " and 0 < ( Select count(*) From ttt_prm_2 p " +
                                    " Where " + rashod.counters_xx + ".nzp_dom  = p.nzp and p.nzp_prm = 2035 and p.val_prm='2' " +
                                    " ) " +
                          " and 0 < ( Select count(*) From ttt_prm_2 p " +
                                    " Where " + rashod.counters_xx + ".nzp_dom  = p.nzp and p.nzp_prm = 2038 and p.val_prm='4' " +
                                    " ) ";

                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                // el/en - dpu ls
                sql =
                        " Update " + rashod.counters_xx +
                        " Set kf307 = kf_dpu_ls, kf307n = kf_dpu_ls, kod_info = 104 " +
                        " Where (kf_dpu_ls>0 or (abs(kf_dpu_ls)<0.000001 and cnt_stage in (1,9))) and nzp_type in (1,2) and stek = 3 and nzp_serv=25 and " + rashod.where_dom + rashod.paramcalc.per_dat_charge +
                          " and 0 < ( Select count(*) From ttt_prm_2 p " +
                                    " Where " + rashod.counters_xx + ".nzp_dom  = p.nzp and p.nzp_prm = 2039 and p.val_prm='2' " +
                                    " ) " +
                          " and 0 < ( Select count(*) From ttt_prm_2 p " +
                                    " Where " + rashod.counters_xx + ".nzp_dom  = p.nzp and p.nzp_prm = 2042 and p.val_prm='4' " +
                                    " ) ";

                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                // gas - dpu ls
                sql =
                        " Update " + rashod.counters_xx +
                        " Set kf307 = kf_dpu_ls, kf307n = kf_dpu_ls, kod_info = 104 " +
                        " Where (kf_dpu_ls>0 or (abs(kf_dpu_ls)<0.000001 and cnt_stage in (1,9))) and nzp_type in (1,2) and stek = 3 and nzp_serv=10 and " + rashod.where_dom + rashod.paramcalc.per_dat_charge +
                          " and 0 < ( Select count(*) From ttt_prm_2 p " +
                                    " Where " + rashod.counters_xx + ".nzp_dom  = p.nzp and p.nzp_prm = 2062 and p.val_prm='2' " +
                                    " ) " +
                          " and 0 < ( Select count(*) From ttt_prm_2 p " +
                                    " Where " + rashod.counters_xx + ".nzp_dom  = p.nzp and p.nzp_prm = 2065 and p.val_prm='4' " +
                                    " ) ";

                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                // kan - dpu ls
                sql =
                        " Update " + rashod.counters_xx +
                        " Set kf307 = kf_dpu_ls, kf307n = kf_dpu_ls, kod_info = 104 " +
                        " Where (kf_dpu_ls>0 or (abs(kf_dpu_ls)<0.000001 and cnt_stage in (1,9))) and nzp_type in (1,2) and stek = 3 and nzp_serv in (7,324) and " + rashod.where_dom + rashod.paramcalc.per_dat_charge +
                          " and 0 < ( Select count(*) From ttt_prm_2 p " +
                                    " Where " + rashod.counters_xx + ".nzp_dom  = p.nzp and p.nzp_prm = 2091 and p.val_prm='2' " +
                                    " ) " +
                          " and 0 < ( Select count(*) From ttt_prm_2 p " +
                                    " Where " + rashod.counters_xx + ".nzp_dom  = p.nzp and p.nzp_prm = 2094 and p.val_prm='4' " +
                                    " ) ";

                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                // полив - dpu ls
                sql =
                        " Update " + rashod.counters_xx +
                        " Set kf307 = kf_dpu_ls, kf307n = kf_dpu_ls, kod_info = 104 " +
                        " Where kf_dpu_ls>0 and nzp_type in (1,2) and stek = 3 and nzp_serv in (200) and " + rashod.where_dom + rashod.paramcalc.per_dat_charge +
                          " and 0 < ( Select count(*) From ttt_prm_2 p " +
                                    " Where " + rashod.counters_xx + ".nzp_dom  = p.nzp and p.nzp_prm = 2102 and p.val_prm='1' " +
                                    " ) " +
                          " and 0 < ( Select count(*) From ttt_prm_2 p " +
                                    " Where " + rashod.counters_xx + ".nzp_dom  = p.nzp and p.nzp_prm = 2105 and p.val_prm='4' " +
                                    " ) ";

                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                // hvs - vid 307 vse ls? - kpu
                ret = ExecSQL(conn_db,
                    " Update " + rashod.counters_xx +
                    " Set kf307n = 0 " +
                    " Where kod_info > 100 and nzp_type in (1,2) and stek = 3 and nzp_serv=6 and " + rashod.where_dom + rashod.paramcalc.per_dat_charge +
                      " and 0 < ( Select count(*) From ttt_prm_2 p " +
                                " Where " + rashod.counters_xx + ".nzp_dom  = p.nzp and p.nzp_prm = 2033 and p.val_prm='2' " +
                                " ) "
                    , true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                // gvs - vid 307 vse ls? - kpu
                ret = ExecSQL(conn_db,
                    " Update " + rashod.counters_xx +
                    " Set kf307n = 0 " +
                    " Where kod_info > 100 and nzp_type in (1,2) and stek = 3 and nzp_serv=9 and " + rashod.where_dom + rashod.paramcalc.per_dat_charge +
                      " and 0 < ( Select count(*) From ttt_prm_2 p " +
                                " Where " + rashod.counters_xx + ".nzp_dom  = p.nzp and p.nzp_prm = 2037 and p.val_prm='2' " +
                                " ) "
                    , true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                // el/en - vid 307 vse ls? - kpu
                ret = ExecSQL(conn_db,
                    " Update " + rashod.counters_xx +
                    " Set kf307n = 0 " +
                    " Where kod_info > 100 and nzp_type in (1,2) and stek = 3 and nzp_serv=25 and " + rashod.where_dom + rashod.paramcalc.per_dat_charge +
                      " and 0 < ( Select count(*) From ttt_prm_2 p " +
                                " Where " + rashod.counters_xx + ".nzp_dom  = p.nzp and p.nzp_prm = 2041 and p.val_prm='2' " +
                                " ) "
                    , true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                // gas - vid 307 vse ls? - kpu
                ret = ExecSQL(conn_db,
                    " Update " + rashod.counters_xx +
                    " Set kf307n = 0 " +
                    " Where kod_info > 100 and nzp_type in (1,2) and stek = 3 and nzp_serv=10 and " + rashod.where_dom + rashod.paramcalc.per_dat_charge +
                      " and 0 < ( Select count(*) From ttt_prm_2 p " +
                                " Where " + rashod.counters_xx + ".nzp_dom  = p.nzp and p.nzp_prm = 2064 and p.val_prm='2' " +
                                " ) "
                    , true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                // kan - vid 307 vse ls? - kpu
                ret = ExecSQL(conn_db,
                    " Update " + rashod.counters_xx +
                    " Set kf307n = 0 " +
                    " Where kod_info > 100 and nzp_type in (1,2) and stek = 3 and nzp_serv in (7,324) and " + rashod.where_dom + rashod.paramcalc.per_dat_charge +
                      " and 0 < ( Select count(*) From ttt_prm_2 p " +
                                " Where " + rashod.counters_xx + ".nzp_dom  = p.nzp and p.nzp_prm = 2093 and p.val_prm='2' " +
                                " ) "
                    , true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                // полив - vid 307 vse ls? - kpu
                ret = ExecSQL(conn_db,
                    " Update " + rashod.counters_xx +
                    " Set kf307n = 0 " +
                    " Where kod_info > 100 and nzp_type in (1,2) and stek = 3 and nzp_serv in (200) and " + rashod.where_dom + rashod.paramcalc.per_dat_charge +
                      " and 0 < ( Select count(*) From ttt_prm_2 p " +
                                " Where " + rashod.counters_xx + ".nzp_dom  = p.nzp and p.nzp_prm = 2104 and p.val_prm='2' " +
                                " ) "
                    , true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                // hvs - vid 307 vse ls? - norma
                ret = ExecSQL(conn_db,
                    " Update " + rashod.counters_xx +
                    " Set kf307 = 0 " +
                    " Where kod_info > 100 and nzp_type in (1,2) and stek = 3 and nzp_serv=6 and " + rashod.where_dom + rashod.paramcalc.per_dat_charge +
                      " and 0 < ( Select count(*) From ttt_prm_2 p " +
                                " Where " + rashod.counters_xx + ".nzp_dom  = p.nzp and p.nzp_prm = 2033 and p.val_prm='3' " +
                                " ) "
                    , true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                // gvs - vid 307 vse ls? -  norma
                ret = ExecSQL(conn_db,
                    " Update " + rashod.counters_xx +
                    " Set kf307 = 0 " +
                    " Where kod_info > 100 and nzp_type in (1,2) and stek = 3 and nzp_serv=9 and " + rashod.where_dom + rashod.paramcalc.per_dat_charge +
                      " and 0 < ( Select count(*) From ttt_prm_2 p " +
                                " Where " + rashod.counters_xx + ".nzp_dom  = p.nzp and p.nzp_prm = 2037 and p.val_prm='3' " +
                                " ) "
                    , true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                // el/en - vid 307 vse ls? -  norma
                ret = ExecSQL(conn_db,
                    " Update " + rashod.counters_xx +
                    " Set kf307 = 0 " +
                    " Where kod_info > 100 and nzp_type in (1,2) and stek = 3 and nzp_serv=25 and " + rashod.where_dom + rashod.paramcalc.per_dat_charge +
                      " and 0 < ( Select count(*) From ttt_prm_2 p " +
                                " Where " + rashod.counters_xx + ".nzp_dom  = p.nzp and p.nzp_prm = 2041 and p.val_prm='3' " +
                                " ) "
                    , true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                // gas - vid 307 vse ls? -  norma
                ret = ExecSQL(conn_db,
                    " Update " + rashod.counters_xx +
                    " Set kf307 = 0 " +
                    " Where kod_info > 100 and nzp_type in (1,2) and stek = 3 and nzp_serv=10 and " + rashod.where_dom + rashod.paramcalc.per_dat_charge +
                      " and 0 < ( Select count(*) From ttt_prm_2 p " +
                                " Where " + rashod.counters_xx + ".nzp_dom  = p.nzp and p.nzp_prm = 2064 and p.val_prm='3' " +
                                " ) "
                    , true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                // gas - vid 307 vse ls? -  norma
                ret = ExecSQL(conn_db,
                    " Update " + rashod.counters_xx +
                    " Set kf307 = 0 " +
                    " Where kod_info > 100 and nzp_type in (1,2) and stek = 3 and nzp_serv in (7,324) and " + rashod.where_dom + rashod.paramcalc.per_dat_charge +
                      " and 0 < ( Select count(*) From ttt_prm_2 p " +
                                " Where " + rashod.counters_xx + ".nzp_dom  = p.nzp and p.nzp_prm = 2093 and p.val_prm='3' " +
                                " ) "
                    , true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }


                // Полив - vid 307 vse ls? -  norma
                ret = ExecSQL(conn_db,
                    " Update " + rashod.counters_xx +
                    " Set kf307 = 0 " +
                    " Where kod_info > 100 and nzp_type in (1,2) and stek = 3 and nzp_serv in (200) and " + rashod.where_dom + rashod.paramcalc.per_dat_charge +
                      " and 0 < ( Select count(*) From ttt_prm_2 p " +
                                " Where " + rashod.counters_xx + ".nzp_dom  = p.nzp and p.nzp_prm = 2104 and p.val_prm='3' " +
                                " ) "
                    , true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                #region Учет коэффициентов на выбранную единицу измерения для учета для расчета начислений
                //Параметры: услуга - код параметра
                var paramDict = new Dictionary<string, int>
                {
                    {"6",2034},
                    {"9",2038},
                    {"25",2042},
                    {"8,322,325",2048},
                    {"10",2065},
                    {"7,324",2094}
                };
                foreach (var param in paramDict)
                {
                    //Параметры: тип расчета ОДН - код параметра
                    string sPos, sType;
                    switch (param.Value)
                    {
                        case 2034: { sPos = "6"; sType = "2031"; } break;
                        case 2038: { sPos = "5"; sType = "2035"; } break;
                        case 2042: { sPos = "5"; sType = "2039"; } break;
                        case 2048: { sPos = "5"; sType = "2045"; } break;
                        case 2065: { sPos = "4"; sType = "2062"; } break;
                        case 2094: { sPos = "6"; sType = "2091"; } break;
                        default: { sPos = "6"; sType = "2031"; } break;
                    }

                    // - по жильцам
                    sql =
                        " Update " + rashod.counters_xx +
                        " Set kf307 = 0, kf307n = kf_dpu_kg, kod_info = 201 " +
                        " Where (kf_dpu_plob>0 or (abs(kf_dpu_plob)<0.000001 and cnt_stage in (1,9))) and nzp_type in (1,2) and stek = 3 and nzp_serv in (" + param.Key + ") and " + rashod.where_dom +
                        rashod.paramcalc.per_dat_charge +
                        " and exists ( Select 1 From ttt_prm_2 p " +
                        " Where " + rashod.counters_xx + ".nzp_dom  = p.nzp and p.nzp_prm = " + sType + " and p.val_prm='" + sPos + "' " +
                        " ) " +
                        " and exists ( Select 1 From ttt_prm_2 p " +
                        " Where " + rashod.counters_xx + ".nzp_dom  = p.nzp and p.nzp_prm = " + param.Value + " and p.val_prm='1' " +
                        " ) ";

                    ret = ExecSQL(conn_db, sql, true);
                    if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                    // - по общей площади
                    sql =
                        " Update " + rashod.counters_xx +
                        " Set kf307 = 0, kf307n = kf_dpu_plob, kod_info = 202 " +
                        " Where (kf_dpu_plob>0 or (abs(kf_dpu_plob)<0.000001 and cnt_stage in (1,9))) and nzp_type in (1,2) and stek = 3 and nzp_serv in (" + param.Key + ") and " + rashod.where_dom +
                        rashod.paramcalc.per_dat_charge +
                        " and exists ( Select 1 From ttt_prm_2 p " +
                        " Where " + rashod.counters_xx + ".nzp_dom  = p.nzp and p.nzp_prm = " + sType + " and p.val_prm='" + sPos + "' " +
                        " ) " +
                        " and exists ( Select 1 From ttt_prm_2 p " +
                        " Where " + rashod.counters_xx + ".nzp_dom  = p.nzp and p.nzp_prm = " + param.Value + " and p.val_prm = '2' " +
                        " ) ";

                    ret = ExecSQL(conn_db, sql, true);
                    if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                    // - по отапливаемой площади
                    sql =
                        " Update " + rashod.counters_xx +
                        " Set kf307 = 0, kf307n = kf_dpu_plot, kod_info = 203 " +
                        " Where (kf_dpu_plob>0 or (abs(kf_dpu_plob)<0.000001 and cnt_stage in (1,9))) and nzp_type in (1,2) and stek = 3 and nzp_serv in (" + param.Key + ") and " + rashod.where_dom +
                        rashod.paramcalc.per_dat_charge +
                        " and exists ( Select 1 From ttt_prm_2 p " +
                        " Where " + rashod.counters_xx + ".nzp_dom  = p.nzp and p.nzp_prm = " + sType + " and p.val_prm='" + sPos + "' " +
                        " ) " +
                        " and exists ( Select 1 From ttt_prm_2 p " +
                        " Where " + rashod.counters_xx + ".nzp_dom  = p.nzp and p.nzp_prm = " + param.Value + " and p.val_prm = '3' " +
                        " ) ";

                    ret = ExecSQL(conn_db, sql, true);
                    if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                    // - по ЛС
                    sql =
                        " Update " + rashod.counters_xx +
                        " Set kf307 = 0, kf307n = kf_dpu_ls, kod_info = 204 " +
                        " Where (kf_dpu_plob>0 or (abs(kf_dpu_plob)<0.000001 and cnt_stage in (1,9))) and nzp_type in (1,2) and stek = 3 and nzp_serv in (" + param.Key + ") and " + rashod.where_dom +
                        rashod.paramcalc.per_dat_charge +
                        " and exists ( Select 1 From ttt_prm_2 p " +
                        " Where " + rashod.counters_xx + ".nzp_dom  = p.nzp and p.nzp_prm = " + sType + " and p.val_prm='" + sPos + "' " +
                        " ) " +
                        " and exists ( Select 1 From ttt_prm_2 p " +
                        " Where " + rashod.counters_xx + ".nzp_dom  = p.nzp and p.nzp_prm = " + param.Value + " and p.val_prm = '4' " +
                        " ) ";

                    ret = ExecSQL(conn_db, sql, true);
                    if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }
                }
                #endregion

            }

            return true;
        }
        #endregion учет ОДН по 307 постановлению в стек = 3 and nzp_type in (1,2)

        #region учет ОДН по домам из прошлого, если он был рассчитан - запись в стек = 3 & nzp_type = 1
        public bool Stek3UchetODNLast(IDbConnection conn_db, Rashod rashod, bool bIsCalcODN, bool bIsCalcCurMonth, out Returns ret)
        //--------------------------------------------------------------------------------
        {
            ret = Utils.InitReturns();

            if (bIsCalcODN || bIsCalcCurMonth)
            {
            }
            else
            {
                //
                // если не текущий месяц (т.е. - перерасчет) и запрещен перерасчет ОДН, 
                // то поискать домовые расходы в прошлом и взять готовые
                //
                string cur_counters_xx = "";
                cur_counters_xx =
                    rashod.paramcalc.pref + "_charge_" + (rashod.paramcalc.calc_yy - 2000).ToString("00") + tableDelimiter +
                    "counters_" + rashod.paramcalc.calc_mm.ToString("00");
                //
                // месяц последнего рассчитанного расхода по дому - первый расчетный за этот месяц/год
                //
                ExecSQL(conn_db, " Drop table ttt_aid_stat ", false);
                ret = ExecSQL(conn_db,
                    " Create temp table ttt_aid_stat " +
                    " ( nzp_dom integer, " +
                    "   nzp_serv integer, " +
                    "   nzp_type integer, " +
                    "   nzp_counter integer, " +
                    "   kf307       " + sDecimalType + "(15,7) default 0.00 , " +  //коэфициент 307 для КПУ или коэфициент 87 для нормативщиков
                    "   kf307n      " + sDecimalType + "(15,7) default 0.00 , " +  //коэфициент 307 для нормативщиков
                    "   kod_info    integer default 0, " +            //выбранный способ учета (=1)
                    "   squ1        " + sDecimalType + "(15,7), " +
                    "   val4_source " + sDecimalType + "(15,7) default 0.0000000," +
                    "   cnt_stage   integer default 0, " +
                    "   up_kf       " + sDecimalType + "(15,7) default 1.0000000," +
                    "   kf_dpu_ls   " + sDecimalType + "(15,7) default 0.0000000," +
                    "   val3        " + sDecimalType + "(15,7) default 0.0000000," +
                    "   val4        " + sDecimalType + "(15,7) default 0.0000000," +
                    "   is_not_calc integer default 0  " +            //если рассчитан по ДПУ =0 / иначе "левый" коэф. корр. =1
                    " )  " + sUnlogTempTable
                    , true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                ret = ExecSQL(conn_db,
                  " insert into ttt_aid_stat (nzp_dom, nzp_serv, nzp_type, nzp_counter, kf307, kf307n, kod_info, cnt_stage, squ1, val4_source,up_kf,kf_dpu_ls,val3,val4) " +
                  " Select nzp_dom, nzp_serv, nzp_type, nzp_counter, max(kf307) kf307, max(kf307n) kf307n, max(kod_info) kod_info, max(cnt_stage)," +
                    " max(squ1), max(val4_source),max(up_kf),max(kf_dpu_ls),max(val3),max(val4) " +
                  " From " + cur_counters_xx +
                  " Where nzp_type in (1,2) and stek = 3 and kod_info>0 and " + rashod.where_dom +
                  " group by 1,2,3,4 "
                , true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                ExecSQL(conn_db, " create index ix_ttt_aid_stat on ttt_aid_stat (nzp_dom,nzp_serv,nzp_type,nzp_counter) ", true);
                ExecSQL(conn_db, sUpdStat + " ttt_aid_stat ", true);

                string kf = "*1";
                 // Это Тула?
                if (Points.Region==Regions.Region.Tulskaya_obl && (rashod.paramcalc.calc_yy == 2015) && (rashod.paramcalc.calc_mm == 7))
                {
                    kf = " * (case when a.cnt_stage=0 then 1.129 else 1 end)";
                }

                ret = ExecSQL(conn_db,
                    " Update " + rashod.counters_xx + " t " +
                    " Set " +
                        " kf307      = a.kf307" + kf + ", " +
                        " kf307n     = a.kf307n" + kf + "," +
                        " cnt_stage  = a.cnt_stage," +
                        " squ1       = a.squ1," +
                        " val4_source= a.val4_source," +
                        " up_kf      = a.up_kf" + kf + "," +
                        " kf_dpu_ls  = a.kf_dpu_ls" + kf + "," +
                        " val3       = a.val3" + kf + "," +
                        " val4       = a.val4" + kf + "," +
                        " kod_info   = a.kod_info " +
                    " From ttt_aid_stat a " +
                    " Where t.nzp_type in (1,2) and t.stek = 3 and t." + rashod.where_dom +
                    "   and t.nzp_dom  = a.nzp_dom  " +
                    "   and t.nzp_serv = a.nzp_serv " +
                    "   and t.nzp_type = a.nzp_type" +
                    "   and t.nzp_counter=a.nzp_counter "
                    , true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }
                //    
            }

            return true;
        }
        #endregion учет ОДН по домам из прошлого, если он был рассчитан - запись в стек = 3 & nzp_type = 1

        #region расчет ПУ от ГКал по домам - запись в стек = 39
        bool Stek39UchetPUgkal(IDbConnection conn_db, Rashod rashod, out Returns ret)
        //--------------------------------------------------------------------------------
        {
            ret = Utils.InitReturns();

            #region Выборка ПУ для расчета Отопления и ГВС - kod_info = 201 / 202 / 203 / 204

            ExecSQL(conn_db, " Drop table ttt_pu_gkal ", false);

            ret = ExecSQL(conn_db,
                " CREATE temp TABLE ttt_pu_gkal(        " +
                "   nzp_cntx INTEGER NOT NULL,          " +
                "   nzp_dom  INTEGER NOT NULL,          " +
                "   nzp_kvar INTEGER default 0 NOT NULL," +
                "   nzp_type INTEGER NOT NULL,          " +
                "   nzp_serv INTEGER NOT NULL,          " +
                "   dat_charge DATE,                    " +
                "   cur_zap INTEGER default 0 NOT NULL, " +
                "   nzp_counter INTEGER default 0,      " +
                "   cnt_stage INTEGER default 0,        " +
                "   mmnog " + sDecimalType + "(15,7) default 1.0000000," +
                "   stek INTEGER default 0 NOT NULL,    " +
                "   rashod " + sDecimalType + "(15,7) default 0.0000000 NOT NULL, " +
                "   dat_s DATE NOT NULL,                " +
                "   val_s " + sDecimalType + "(15,7) default 0.0000000 NOT NULL, " +
                "   dat_po DATE NOT NULL,               " +
                "   val_po " + sDecimalType + "(15,7) default 0.0000000 NOT NULL, " +
                "   val1 " + sDecimalType + "(15,7) default 0.0000000 NOT NULL, " +
                "   val2 " + sDecimalType + "(15,7) default 0.0000000 NOT NULL, " +
                "   val3 " + sDecimalType + "(15,7) default 0.0000000, " +
                "   val4 " + sDecimalType + "(15,7) default 0.0000000, " +
                "   rvirt " + sDecimalType + "(15,7) default 0.0000000, " +
                "   squ1 " + sDecimalType + "(15,7) default 0.0000000, " +
                "   squ2 " + sDecimalType + "(15,7) default 0.0000000, " +
                "   cls1 INTEGER default 0 NOT NULL, " +
                "   cls2 INTEGER default 0 NOT NULL, " +
                "   gil1 " + sDecimalType + "(15,7) default 0.0000000, " +
                "   gil2 " + sDecimalType + "(15,7) default 0.0000000, " +
                "   cnt1 INTEGER default 0 NOT NULL, " +
                "   cnt2 INTEGER default 0 NOT NULL, " +
                "   cnt3 INTEGER default 0,          " +
                "   cnt4 INTEGER default 0,          " +
                "   cnt5 INTEGER default 0,          " +
                "   dop87 " + sDecimalType + "(15,7) default 0.0000000, " +
                "   pu7kw " + sDecimalType + "(15,7) default 0.0000000, " +
                "   gl7kw " + sDecimalType + "(15,7) default 0.0000000, " +
                "   vl210 " + sDecimalType + "(15,7) default 0.0000000, " +
                "   kf307 " + sDecimalType + "(15,7) default 0.0000000, " +
                "   kf307n " + sDecimalType + "(15,7) default 0.0000000, " +
                "   kf307f9 " + sDecimalType + "(15,7) default 0.0000000, " +
                "   kf_dpu_kg " + sDecimalType + "(15,7) default 0.0000000, " +
                "   kf_dpu_plob " + sDecimalType + "(15,7) default 0.0000000, " +
                "   kf_dpu_plot " + sDecimalType + "(15,7) default 0.0000000, " +
                "   kf_dpu_ls " + sDecimalType + "(15,7) default 0.0000000, " +
                "   norm_gkal_gvs    " + sDecimalType + "(15,7) default 0.0000000 NOT NULL, " +
                "   dlt_in " + sDecimalType + "(15,7) default 0.0000000, " +
                "   dlt_cur " + sDecimalType + "(15,7) default 0.0000000, " +
                "   dlt_reval " + sDecimalType + "(15,7) default 0.0000000, " +
                "   dlt_real_charge " + sDecimalType + "(15,7) default 0.0000000, " +
                "   dlt_calc " + sDecimalType + "(15,7) default 0.0000000,     " +
                "   dlt_out " + sDecimalType + "(15,7) default 0.0000000,     " +
                "   kod_info INTEGER default 0 " +
                " ) " + sUnlogTempTable
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ret = ExecSQL(conn_db,
                " insert into ttt_pu_gkal" +
                      "(nzp_cntx, nzp_dom, nzp_kvar, nzp_type, nzp_serv, dat_charge, cur_zap, nzp_counter, cnt_stage, mmnog, stek, rashod, " +
                      " dat_s, val_s, dat_po, val_po, val1, val2, val3, val4, rvirt, squ1, squ2, gil1, gil2, cnt1, cnt2, cnt3, cnt4, " +
                      " cnt5, dop87, pu7kw, gl7kw, vl210, kf307, kod_info " +
                      " ,cls1, cls2, kf_dpu_kg, kf_dpu_plob, kf_dpu_plot, kf_dpu_ls, kf307n, kf307f9, dlt_cur, dlt_reval, dlt_real_charge) " +
                " Select" +
                      " nzp_cntx, nzp_dom, nzp_kvar, nzp_type, nzp_serv, dat_charge, cur_zap, nzp_counter, cnt_stage, mmnog, stek, rashod, " +
                      " dat_s, val_po, dat_po, val_po, val1, val2, val3, val4, rvirt, squ1, squ2, gil1, gil2, cnt1, cnt2, cnt3,0 cnt4, " +
                      " cnt5, dop87, pu7kw, gl7kw, vl210, kf307, kod_info " +
                      " ,cls1, cls2, kf_dpu_kg, kf_dpu_plob, kf_dpu_plot, kf_dpu_ls, kf307n, kf307f9, dlt_cur, dlt_reval, dlt_real_charge " +
                      " From " + rashod.counters_xx +
                " Where nzp_type in (1,2) and nzp_serv in (8,9) and stek=39 and " + rashod.where_dom + rashod.where_kvar + rashod.paramcalc.per_dat_charge +
                " "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ExecSQL(conn_db, " create index ix0_ttt_pu_gkal on ttt_pu_gkal (nzp_cntx) ", true);
            ExecSQL(conn_db, " create index ix1_ttt_pu_gkal on ttt_pu_gkal (nzp_dom,nzp_serv) ", true);
            ExecSQL(conn_db, " create index ix2_ttt_pu_gkal on ttt_pu_gkal (nzp_counter,nzp_serv) ", true);
            ExecSQL(conn_db, sUpdStat + " ttt_pu_gkal ", true);

            #endregion Выборка ПУ для расчета Отопления и ГВС - kod_info = 201 / 202 / 203 / 204

            #region Установка расчета Отопления и ГВС - kod_info = 201 / 202 / 203 / 204
            // Признак алгоритма учета ПУ от ГКал - используется,что он ОДИН на дом!
            ret = ExecSQL(conn_db,
                " update ttt_pu_gkal " +
                " set cnt4 = ( Select max( " + sNvlWord + "(p.val_prm,'0')" + sConvToInt + " ) From ttt_prm_2 p " +
                          " Where ttt_pu_gkal.nzp_dom  = p.nzp and p.nzp_prm = 1397 ) " +
                " Where exists ( Select 1 From ttt_prm_2 p " +
                          " Where ttt_pu_gkal.nzp_dom  = p.nzp and p.nzp_prm = 1397 ) "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            // kod_info = 201 - Расчет ГВС по ГКал / kod_info = 202 - Расчет ГВС по ГКал(по ДПУ).
            // Этот ОДПУ по ГВС!
            ret = ExecSQL(conn_db,
                " update ttt_pu_gkal " +
                " set kod_info = cnt4 + 200 " +
                " Where nzp_serv=9 and nzp_type=1 and cnt4 in (1,2) "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            // ГрПУ от ГКал не применим в случае если на доме установлен тип расчета от ГКал - "Расчет ГВС по ГКал(по ДПУ)",
            // т.к. невозможно сопоставить ГрПУ от ГКал с ГрПУ по куб.м.!
            ret = ExecSQL(conn_db,
                " update ttt_pu_gkal " +
                " set kod_info = cnt4 + 200 " +
                " Where nzp_serv=9 and nzp_type=2 and cnt4=1 "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            // kod_info = 204 - Расчет Отопления и ГВС по ГКал / kod_info = 203 - Расчет Отопления и ГВС(норматив) по ГКал.
            // Этот ОДПУ по Отоплению!
            ret = ExecSQL(conn_db,
                " update ttt_pu_gkal " +
                " set kod_info = cnt4 + 200 " +
                " Where nzp_serv=8 and cnt4 in (3,4) "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ExecSQL(conn_db, " create index ix3_ttt_pu_gkal on ttt_pu_gkal (kod_info) ", true);
            ExecSQL(conn_db, sUpdStat + " ttt_pu_gkal ", true);

            // ПУ от ГКал для ГВС по Пост354 - "Расчет ОДН ГВС по ГКал - Пост354 (расход ЛС по норме в ГКал)",
            ret = ExecSQL(conn_db,
                " update ttt_pu_gkal " +
                " set kod_info = 21 " +
                " Where nzp_serv=9 and nzp_type in (1,2) and cnt4=5 "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            #endregion Выборка ПУ для расчета Отопления и ГВС - kod_info = 201 / 202 / 203 / 204

            #region Расчет ГВС - kod_info = 201 / 202

            // не применимо если нет расходов по ЛС
            ret = ExecSQL(conn_db,
                " update ttt_pu_gkal " +
                " set kod_info = -201 " +
                " Where kod_info = 201 and (val1 + val2)<=0.000001 "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            // не применимо если нет расходов по ОДПУ
            ret = ExecSQL(conn_db,
                " update ttt_pu_gkal " +
                " set kod_info = -202 " +
                " Where kod_info = 202 and val3<=0.000001 "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            // Расчет нормы на подогрев 1 куб.м
            ret = ExecSQL(conn_db,
                " update ttt_pu_gkal " +
                " set " +
                " kf307  = val_po / (case when kod_info=201 then val1 + val2 else val3 end) " +
                ",kf307n = val_po / (case when kod_info=201 then val1 + val2 else val3 end) " +
                " Where kod_info in (201,202) "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            #endregion Расчет ГВС - kod_info = 201 / 202

            #region Расчет Отопления и ГВС - kod_info = 203 / 204

            #region Выбрать норму по ЛС и реальный расход ГВС по ЛС

            ExecSQL(conn_db, " Drop table ttt_ls_gkal ", false);

            ret = ExecSQL(conn_db,
                " CREATE temp TABLE ttt_ls_gkal(        " +
                "   nzp_cntx INTEGER NOT NULL,          " +
                "   nzp_dom  INTEGER NOT NULL,          " +
                "   nzp_kvar INTEGER default 0 NOT NULL," +
                "   nzp_serv INTEGER NOT NULL,          " +
                "   rashod_m3        " + sDecimalType + "(15,7) default 0.0000000 NOT NULL, " +
                "   rashod_gkal_m3   " + sDecimalType + "(15,7) default 0.0000000 NOT NULL, " +
                "   rashod_gkal_norm " + sDecimalType + "(15,7) default 0.0000000 NOT NULL, " +
                "   norm_gkal        " + sDecimalType + "(15,7) default 0.0000000 NOT NULL, " +
                "   koef_gkal        " + sDecimalType + "(15,7) default 1.0000000 NOT NULL, " +
                "   norm_gkal_gvs    " + sDecimalType + "(15,7) default 0.0000000 NOT NULL, " +
                "   koef_gkal_gvs    " + sDecimalType + "(15,7) default 1.0000000 NOT NULL, " +
                "   dat_s  DATE NOT NULL,               " +
                "   dat_po DATE NOT NULL,               " +
                "   val1  " + sDecimalType + "(15,7) default 0.0000000 NOT NULL, " +
                "   valm  " + sDecimalType + "(15,7) default 0.0000000 NOT NULL, " +
                "   valmg " + sDecimalType + "(15,7) default 0.0000000 NOT NULL, " +
                "   squ1  " + sDecimalType + "(15,7) default 0.0000000, " +
                "   is_poloten INTEGER default 0 NOT NULL, " +
                "   is_neiztrp INTEGER default 0 NOT NULL  " +
                " ) " + sUnlogTempTable
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            // для отопления нормативный расход Гкал = val3, площадь отапливаемая = squ2, для ГВС - вычислим норму позже!
            ret = ExecSQL(conn_db,
                " insert into ttt_ls_gkal" +
                  "(nzp_cntx, nzp_dom, nzp_kvar, nzp_serv, dat_s, dat_po, val1, squ1, norm_gkal) " +
                " Select" +
                  " b.nzp_cntx, b.nzp_dom, b.nzp_kvar, b.nzp_serv, b.dat_s, b.dat_po, b.val1, " +
                  " case when nzp_serv=8 then b.squ2 else b.squ1 end," +
                  " case when nzp_serv=8 then b.val3 else 0      end " +
                " From " + rashod.counters_xx + " b " +
                " Where b.nzp_type=3 and b.nzp_serv in (8,9) and b.stek=30 " +
                " and exists (select 1 from ttt_pu_gkal a where b.nzp_dom=a.nzp_dom and a.kod_info in (203,204,21)) "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ExecSQL(conn_db, " create index ix0_ttt_ls_gkal on ttt_ls_gkal (nzp_cntx) ", true);
            ExecSQL(conn_db, " create index ix1_ttt_ls_gkal on ttt_ls_gkal (nzp_kvar,nzp_serv) ", true);
            ExecSQL(conn_db, " create index ix2_ttt_ls_gkal on ttt_ls_gkal (nzp_serv) ", true);
            ExecSQL(conn_db, sUpdStat + " ttt_ls_gkal ", true);

            // для ГВС - реальный расход в куб.м для начисления
            ret = ExecSQL(conn_db,
                " update ttt_ls_gkal " +
                " set" +
                  " rashod_m3 = " +
                  " (Select max(case when b.rashod>0 then b.rashod else 0 end) " +
                  " From " + rashod.counters_xx + " b " +
                  " Where ttt_ls_gkal.nzp_kvar=b.nzp_kvar and b.nzp_serv=9 and b.nzp_type=3 and b.stek=3)," +
                  " valm = " +
                  " (Select max(b.val1 + b.val2 + b.dlt_reval + b.dlt_real_charge) " +
                  " From " + rashod.counters_xx + " b " +
                  " Where ttt_ls_gkal.nzp_kvar=b.nzp_kvar and b.nzp_serv=9 and b.nzp_type=3 and b.stek=3) " +
                " where nzp_serv=9 " +
                  " and exists (Select 1 " +
                  " From " + rashod.counters_xx + " b " +
                  " Where ttt_ls_gkal.nzp_kvar=b.nzp_kvar and b.nzp_serv=9 and b.nzp_type=3 and b.stek=3) "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            #endregion Выбрать норму по ЛС и реальный расход ГВС по ЛС

            #region Выбрать норму Гкал из параметра БД + установка: полотенцесушитель / неизолированный трубопровод
            // Выбрать параметр норма Гкал на базу данных для всех формул с Гкал: nzp_frm_typ in (40,440,1140)
            ret = ExecSQL(conn_db,
                " update ttt_ls_gkal set" +
                " norm_gkal=(" +
                  " select max(p.val_prm" + sConvToNum + ") from " + rashod.paramcalc.data_alias + "prm_5 p where p.nzp_prm=253" +
                  " and p.is_actual<>100 and p.dat_s <= ttt_ls_gkal.dat_po and p.dat_po >= ttt_ls_gkal.dat_s ) " +
                " where nzp_serv = 9 and " +
                  " exists (select 1 from " + rashod.paramcalc.data_alias + "prm_5 p where p.nzp_prm=253" +
                  " and p.is_actual<>100 and p.dat_s <= ttt_ls_gkal.dat_po and p.dat_po >= ttt_ls_gkal.dat_s ) "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            // Выбрать норму Гкалл для учета нормы ОДН в ГКал на базу данных
            ret = ExecSQL(conn_db,
                " update ttt_pu_gkal set" +
                " norm_gkal_gvs=(" +
                  " select max(p.val_prm" + sConvToNum + ") from " + rashod.paramcalc.data_alias + "prm_5 p where p.nzp_prm=253" +
                  " and p.is_actual<>100 and p.dat_s <= " + rashod.paramcalc.dat_po + " and p.dat_po >= " + rashod.paramcalc.dat_s + " ) " +
                " where nzp_serv = 9 and " +
                  " exists (select 1 from " + rashod.paramcalc.data_alias + "prm_5 p where p.nzp_prm=253" +
                  " and p.is_actual<>100 and p.dat_s <= " + rashod.paramcalc.dat_po + " and p.dat_po >= " + rashod.paramcalc.dat_s + " ) "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            // Выставить полотенцесушитель (только горячая вода типы 40,440,1140)
            ret = ExecSQL(conn_db,
                " update ttt_ls_gkal set is_poloten=1" +
                " where nzp_serv = 9 and exists (select 1 from ttt_prm_1 p where ttt_ls_gkal.nzp_kvar=p.nzp and p.nzp_prm= 59) "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            // Выставить неизолированный трубопровод (только горячая вода типы 40,440,1140)
            ret = ExecSQL(conn_db,
                " update ttt_ls_gkal set is_neiztrp=1" +
                " where nzp_serv = 9 and exists (select 1 from ttt_prm_1 p where ttt_ls_gkal.nzp_kvar=p.nzp and p.nzp_prm=327) "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }
            #endregion Выбрать норму Гкал из параметра БД + установка: полотенцесушитель / неизолированный трубопровод

            #region Применение повышающего коэффициента  (только горячая вода nzp_serv = 9)

            // запрещено применять повышающий коэффициент для ГВС ?
            bool bNotUseKoefGVS =
                CheckValBoolPrmWithVal(conn_db, rashod.paramcalc.data_alias, 1173, "5", "1", rashod.paramcalc.dat_s, rashod.paramcalc.dat_po);

            // параметр 1173 на  базу - запрещено применять повышающий коэффициент 
            if (!bNotUseKoefGVS)
            {
                ret = ExecSQL(conn_db, " update ttt_ls_gkal set koef_gkal=1.2 where nzp_serv = 9 ", true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                ret = ExecSQL(conn_db, " update ttt_ls_gkal set koef_gkal=1.1 where nzp_serv = 9 and is_neiztrp=0 and is_neiztrp=0 ", true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                ret = ExecSQL(conn_db, " update ttt_ls_gkal set koef_gkal=1.3 where nzp_serv = 9 and is_neiztrp=1 and is_neiztrp=1 ", true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }
            }
            ret = ExecSQL(conn_db,
                " update ttt_ls_gkal" +
                " set koef_gkal_gvs = koef_gkal, norm_gkal_gvs = norm_gkal" +
                " where nzp_serv = 9 "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            #endregion Применение повышающего коэффициента  (только горячая вода nzp_serv = 9)

            #region Выбрать норму Гкал из домового параметра или параметра на лицевой счет для ГВС

            // Выбрать норму Гкалл из домового параметра ( prm_2:  nzp_prm =436 по nzp_serv = 9 )
            ret = ExecSQL(conn_db,
                " update ttt_ls_gkal set" +
                " norm_gkal_gvs=" +
                  " (select max(p.val_prm" + sConvToNum + ") from ttt_prm_2 p where p.nzp=ttt_ls_gkal.nzp_dom and p.nzp_prm=436), " +
                " koef_gkal_gvs=1 " +
                " where nzp_serv = 9 " +
                "  and exists (select 1 from ttt_prm_2 p where p.nzp=ttt_ls_gkal.nzp_dom and p.nzp_prm=436) "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            // Выбрать норму Гкалл для учета нормы ОДН в ГКал из домового параметра ( prm_2:  nzp_prm =436 по nzp_serv = 9 )
            ret = ExecSQL(conn_db,
                " update ttt_pu_gkal set" +
                " norm_gkal_gvs=" +
                  " (select max(p.val_prm" + sConvToNum + ") from ttt_prm_2 p where p.nzp=ttt_pu_gkal.nzp_dom and p.nzp_prm=436)  " +
                " where nzp_serv = 9 " +
                "  and exists (select 1 from ttt_prm_2 p where p.nzp=ttt_pu_gkal.nzp_dom and p.nzp_prm=436) "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            // Выбрать норму Гкал на 1 м3 воды на лицевой счет prm_1 nzp_prm =894: nzp_serv = 9
            ret = ExecSQL(conn_db,
                " update ttt_ls_gkal set" +
                " norm_gkal_gvs=" +
                  " (select max(p.val_prm" + sConvToNum + ") from ttt_prm_1 p where p.nzp=ttt_ls_gkal.nzp_kvar and p.nzp_prm=894), " +
                " koef_gkal_gvs=1 " +
                " where nzp_serv = 9 " +
                "  and exists (select 1 from ttt_prm_1 p where p.nzp=ttt_ls_gkal.nzp_kvar and p.nzp_prm=894) "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            #endregion Выбрать норму Гкал из домового параметра или параметра на лицевой счет для ГВС

            #region Расчет расхода в ГКал по ЛС

            ret = ExecSQL(conn_db,
                " update ttt_ls_gkal" +
                " set" +
                  " rashod_gkal_norm = norm_gkal * ( case when nzp_serv = 9 then koef_gkal * val1 else squ1 end )," +
                  " rashod_gkal_m3   = norm_gkal_gvs * ( case when nzp_serv = 9 then koef_gkal_gvs * rashod_m3 else 0 end )," +
                  " valmg            = norm_gkal_gvs * ( case when nzp_serv = 9 then koef_gkal_gvs * valm      else 0 end ) " +
                " where 1 = 1 "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            #endregion Расчет расхода в ГКал по ЛС

            #region Выборка расходов Отопления в ГКал (kf_dpu_plot) и ГВС в ГКал (kf_dpu_plob) и куб.м (kf_dpu_ls)

            // Выборка расходов Отопления в ГКал
            ret = ExecSQL(conn_db,
                " update ttt_pu_gkal " +
                // суммарный нормативный расход в ГКал по ЛС для Отопления
                " set kf_dpu_plot = ( Select sum(p.rashod_gkal_norm) " +
                               " From ttt_ls_gkal p " +
                               " Where ttt_pu_gkal.nzp_dom = p.nzp_dom and p.nzp_serv = 8 )," +
                // сумма отапливаемых площадей по ЛС для Отопления
                    " squ1  = ( Select sum(squ1) " +
                               " From ttt_ls_gkal p " +
                               " Where ttt_pu_gkal.nzp_dom = p.nzp_dom and p.nzp_serv = 8 )," +
                // виртуальный расход в ГКал на 1 кв.м для Отопления
                    " vl210  = ( Select case when sum(squ1)>0.00001 then sum(p.rashod_gkal_norm)/sum(squ1) else 0 end " +
                               " From ttt_ls_gkal p " +
                               " Where ttt_pu_gkal.nzp_dom = p.nzp_dom and p.nzp_serv = 8 ) " +
                " Where nzp_type=1 and kod_info in (203,204) " +
                  " and exists ( Select 1 " +
                               " From ttt_ls_gkal p " +
                               " Where ttt_pu_gkal.nzp_dom = p.nzp_dom and p.nzp_serv = 8 ) "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ret = ExecSQL(conn_db,
                " update ttt_pu_gkal " +
                " set kf_dpu_plot = ( Select sum(p.rashod_gkal_norm) " +
                               " From ttt_ls_gkal p, temp_counters_link l " +
                               " Where ttt_pu_gkal.nzp_counter = l.nzp_counter and  ttt_pu_gkal.nzp_dom = p.nzp_dom" +
                               "   and p.nzp_kvar=l.nzp_kvar and p.nzp_serv = 8 )," +
                    " squ1  = ( Select sum(squ1) " +
                               " From ttt_ls_gkal p, temp_counters_link l " +
                               " Where ttt_pu_gkal.nzp_counter = l.nzp_counter and  ttt_pu_gkal.nzp_dom = p.nzp_dom" +
                               "   and p.nzp_kvar=l.nzp_kvar and p.nzp_serv = 8 )," +
                    " vl210  = ( Select case when sum(squ1)>0.00001 then sum(p.rashod_gkal_norm)/sum(squ1) else 0 end " +
                               " From ttt_ls_gkal p, temp_counters_link l " +
                               " Where ttt_pu_gkal.nzp_counter = l.nzp_counter and  ttt_pu_gkal.nzp_dom = p.nzp_dom" +
                               "   and p.nzp_kvar=l.nzp_kvar and p.nzp_serv = 8 ) " +
                " Where nzp_type=2 and kod_info in (203,204) " +
                  " and exists ( Select 1 " +
                               " From ttt_ls_gkal p, temp_counters_link l " +
                               " Where ttt_pu_gkal.nzp_counter = l.nzp_counter and  ttt_pu_gkal.nzp_dom = p.nzp_dom" +
                               "   and p.nzp_kvar=l.nzp_kvar and p.nzp_serv = 8 ) "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            // Выборка расходов ГВС в ГКал и куб.м
            ret = ExecSQL(conn_db,
                " update ttt_pu_gkal " +
                // суммарный нормативный (kod_info=204) / реальный (kod_info=203) - расход в ГКал по ЛС для ГВС
                " set kf_dpu_plob = ( Select sum(case when kod_info = 204 then p.rashod_gkal_norm else p.rashod_gkal_m3 end)" +
                                 " From ttt_ls_gkal p " +
                                 " Where ttt_pu_gkal.nzp_dom = p.nzp_dom and p.nzp_serv = 9 )," +
                // суммарный реальный расход в куб.м по ЛС для ГВС
                  " kf_dpu_ls = ( Select sum(p.rashod_m3)" +
                                 " From ttt_ls_gkal p " +
                                 " Where ttt_pu_gkal.nzp_dom = p.nzp_dom and p.nzp_serv = 9 )," +
                // суммарный нормативный расход в куб.м по ЛС для ГВС
                  " kf_dpu_kg = ( Select sum(p.val1)" +
                                 " From ttt_ls_gkal p " +
                                 " Where ttt_pu_gkal.nzp_dom = p.nzp_dom and p.nzp_serv = 9 )," +
                // виртуальный расход в ГКал на 1 куб.м для ГВС
                     " gl7kw = ( Select case when sum(p.val1)>0.00001" +
                                    " then sum(case when kod_info = 204 then p.rashod_gkal_norm else p.rashod_gkal_m3 end)/sum(p.val1) " +
                                    " else 0 end " +
                                 " From ttt_ls_gkal p " +
                                 " Where ttt_pu_gkal.nzp_dom = p.nzp_dom and p.nzp_serv = 9 ) " +
                " Where nzp_type=1 and kod_info in (203,204)" +
                  " and exists ( Select 1" +
                                 " From ttt_ls_gkal p " +
                                 " Where ttt_pu_gkal.nzp_dom = p.nzp_dom and p.nzp_serv = 9 ) "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ret = ExecSQL(conn_db,
                " update ttt_pu_gkal " +
                " set kf_dpu_plob = ( Select sum(case when kod_info = 204 then p.rashod_gkal_norm else p.rashod_gkal_m3 end)" +
                               " From ttt_ls_gkal p, temp_counters_link l " +
                               " Where ttt_pu_gkal.nzp_counter = l.nzp_counter and  ttt_pu_gkal.nzp_dom = p.nzp_dom" +
                               "   and p.nzp_kvar=l.nzp_kvar and p.nzp_serv = 9 )," +
                   " kf_dpu_ls = ( Select sum(p.rashod_m3)" +
                               " From ttt_ls_gkal p, temp_counters_link l " +
                               " Where ttt_pu_gkal.nzp_counter = l.nzp_counter and  ttt_pu_gkal.nzp_dom = p.nzp_dom" +
                               "   and p.nzp_kvar=l.nzp_kvar and p.nzp_serv = 9 )," +
                   " kf_dpu_kg = ( Select sum(p.val1)" +
                               " From ttt_ls_gkal p, temp_counters_link l " +
                               " Where ttt_pu_gkal.nzp_counter = l.nzp_counter and  ttt_pu_gkal.nzp_dom = p.nzp_dom" +
                               "   and p.nzp_kvar=l.nzp_kvar and p.nzp_serv = 9 )," +
                       " gl7kw = ( Select case when sum(p.val1)>0.00001" +
                                        " then sum(case when kod_info = 204 then p.rashod_gkal_norm else p.rashod_gkal_m3 end)/sum(p.val1)" +
                                        " else 0 end" +
                               " From ttt_ls_gkal p, temp_counters_link l " +
                               " Where ttt_pu_gkal.nzp_counter = l.nzp_counter and  ttt_pu_gkal.nzp_dom = p.nzp_dom" +
                               "   and p.nzp_kvar=l.nzp_kvar and p.nzp_serv = 9 ) " +
                " Where nzp_type=2 and kod_info in (203,204)" +
                  " and exists ( Select 1 " +
                               " From ttt_ls_gkal p, temp_counters_link l " +
                               " Where ttt_pu_gkal.nzp_counter = l.nzp_counter and  ttt_pu_gkal.nzp_dom = p.nzp_dom" +
                               "   and p.nzp_kvar=l.nzp_kvar and p.nzp_serv = 9 ) "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            // Выборка расходов ГВС в ГКал и куб.м
            ret = ExecSQL(conn_db,
                " update ttt_pu_gkal " +
                // реальный (kod_info=21) - расход в ГКал по ЛС для ГВС
                " set kf_dpu_plob = ( Select sum(p.valmg)" +
                                 " From ttt_ls_gkal p " +
                                 " Where ttt_pu_gkal.nzp_dom = p.nzp_dom and p.nzp_serv = 9 )," +
                // суммарный реальный расход в куб.м по ЛС для ГВС
                    " kf_dpu_plot = ( Select sum(p.valm)" +
                                 " From ttt_ls_gkal p " +
                                 " Where ttt_pu_gkal.nzp_dom = p.nzp_dom and p.nzp_serv = 9 ) " +
                " Where nzp_type = 1 and kod_info = 21" +
                  " and exists ( Select 1" +
                                 " From ttt_ls_gkal p " +
                                 " Where ttt_pu_gkal.nzp_dom = p.nzp_dom and p.nzp_serv = 9 ) "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ret = ExecSQL(conn_db,
                " update ttt_pu_gkal " +
                " set kf_dpu_plob = ( Select sum(p.valmg)" +
                               " From ttt_ls_gkal p, temp_counters_link l " +
                               " Where ttt_pu_gkal.nzp_counter = l.nzp_counter and  ttt_pu_gkal.nzp_dom = p.nzp_dom" +
                               "   and p.nzp_kvar=l.nzp_kvar and p.nzp_serv = 9 )," +
                // суммарный реальный расход в куб.м по ЛС для ГВС
                    " kf_dpu_plot = ( Select sum(p.valm)" +
                               " From ttt_ls_gkal p, temp_counters_link l " +
                               " Where ttt_pu_gkal.nzp_counter = l.nzp_counter and  ttt_pu_gkal.nzp_dom = p.nzp_dom" +
                               "   and p.nzp_kvar=l.nzp_kvar and p.nzp_serv = 9 ) " +
                " Where nzp_type = 2 and kod_info = 21" +
                  " and exists ( Select 1 " +
                               " From ttt_ls_gkal p, temp_counters_link l " +
                               " Where ttt_pu_gkal.nzp_counter = l.nzp_counter and  ttt_pu_gkal.nzp_dom = p.nzp_dom" +
                               "   and p.nzp_kvar=l.nzp_kvar and p.nzp_serv = 9 ) "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            #endregion Выборка расходов Отопления в ГКал (kf_dpu_plot) и ГВС в ГКал (kf_dpu_plob) и куб.м (kf_dpu_ls)

            #region Расчет норм расходов на единицу Отопления и ГВС

            // не применимо если нет площадей по ЛС
            ret = ExecSQL(conn_db,
                " update ttt_pu_gkal " +
                " set kod_info = (-1) * kod_info " +
                " Where kod_info in (203,204) and squ1<=0.000001 "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            // не применимо если нет расходов или площадей по ЛС
            ret = ExecSQL(conn_db,
                " update ttt_pu_gkal " +
                " set kod_info = -204 " +
                " Where kod_info = 204 and (kf_dpu_plob + kf_dpu_plot)<=0.000001 "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ret = ExecSQL(conn_db,
                " update ttt_pu_gkal " +
                // суммарный реальный ГВС в ГКал (ГВС не изменяется!)
                " set rashod = kf_dpu_plob, " +
                // отстаток на Отопление = Расход ПУ "Отопл+ГВС" - объем ГВС в ГКал
                "     rvirt  = case when val_po - kf_dpu_plob > 0 then val_po - kf_dpu_plob else 0 end " +
                " Where kod_info = 203 "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ret = ExecSQL(conn_db,
                " update ttt_pu_gkal " +
                // доля расхода на ГВС в ГКал по пропорции от суммы нормативов ГВС и Отопления
                " set rashod = val_po * kf_dpu_plob / (kf_dpu_plob + kf_dpu_plot), " +
                // доля расхода на Отопления в ГКал по пропорции от суммы нормативов ГВС и Отопления
                "     rvirt  = val_po * kf_dpu_plot / (kf_dpu_plob + kf_dpu_plot)  " +
                " Where kod_info = 204 "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            // Расчет нормы на подогрев 1 куб.м
            ret = ExecSQL(conn_db,
                " update ttt_pu_gkal " +
                " set " +
                " kf307  = rashod / kf_dpu_ls " + // для ГВС - для kod_info=203 не используется!
                ",kf307n = rvirt  / squ1 " +      // для Отопления  
                " Where kod_info in (203,204) "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            #endregion Расчет норм расходов на единицу Отопления и ГВС

            #endregion Расчет Отопления и ГВС - kod_info = 203 / 204

            #region Расчет расходов ОДН для ГВС - kod_info = 21 / 31

            // стек=3 для дома есть всегда!
            ret = ExecSQL(conn_db,
                " Update ttt_pu_gkal" +
                " Set" +
                  " rvirt = " +
                  " (select max(a.rvirt) From " + rashod.counters_xx + " a " +
                  " Where a.nzp_type=1 and a.nzp_serv=9 and a.stek=3 and a.nzp_dom=ttt_pu_gkal.nzp_dom)," +
                // перерасчеты, учтенные при расчете ОДН
                  " dlt_reval = " +
                  " (select max(a.dlt_reval) From " + rashod.counters_xx + " a " +
                  " Where a.nzp_type=1 and a.nzp_serv=9 and a.stek=3 and a.nzp_dom=ttt_pu_gkal.nzp_dom)," +
                  " dlt_real_charge = " +
                  " (select max(a.dlt_real_charge) From " + rashod.counters_xx + " a " +
                  " Where a.nzp_type=1 and a.nzp_serv=9 and a.stek=3 and a.nzp_dom=ttt_pu_gkal.nzp_dom)," +
                // норма ОДН в куб.м
                  " kf_dpu_ls = " +
                  " (select max(a.kf_dpu_ls) From " + rashod.counters_xx + " a " +
                  " Where a.nzp_type=1 and a.nzp_serv=9 and a.stek=3 and a.nzp_dom=ttt_pu_gkal.nzp_dom) " +
                " Where nzp_type = 1 and nzp_serv = 9 and kod_info = 21" +
                  " and exists " +
                  " (select 1 From " + rashod.counters_xx + " a " +
                  " Where a.nzp_type=1 and a.nzp_serv=9 and a.stek=3 and a.nzp_dom=ttt_pu_gkal.nzp_dom) "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            // стек=3 для ГрПУ ищем по пересечению ЛС для ГрПУ от ГКал и куб.м!
            ret = ExecSQL(conn_db,
                " Update ttt_pu_gkal" +
                " Set" +
                  " rvirt = " +
                  " (select max(a.rvirt) From " + rashod.counters_xx + " a, temp_counters_link l, temp_counters_link t " +
                  " Where a.nzp_type=2 and a.nzp_serv=9 and a.stek=3 " +
                    " and l.nzp_kvar=t.nzp_kvar and a.nzp_counter=t.nzp_counter and l.nzp_counter=ttt_pu_gkal.nzp_counter)," +
                // перерасчеты, учтенные при расчете ОДН
                  " dlt_reval = " +
                  " (select max(a.dlt_reval) From " + rashod.counters_xx + " a, temp_counters_link l, temp_counters_link t " +
                  " Where a.nzp_type=2 and a.nzp_serv=9 and a.stek=3 " +
                    " and l.nzp_kvar=t.nzp_kvar and a.nzp_counter=t.nzp_counter and l.nzp_counter=ttt_pu_gkal.nzp_counter)," +
                  " dlt_real_charge = " +
                  " (select max(a.dlt_real_charge) From " + rashod.counters_xx + " a, temp_counters_link l, temp_counters_link t " +
                  " Where a.nzp_type=2 and a.nzp_serv=9 and a.stek=3 " +
                    " and l.nzp_kvar=t.nzp_kvar and a.nzp_counter=t.nzp_counter and l.nzp_counter=ttt_pu_gkal.nzp_counter)," +
                // норма ОДН в куб.м
                  " kf_dpu_ls = " +
                  " (select max(a.kf_dpu_ls) From " + rashod.counters_xx + " a, temp_counters_link l, temp_counters_link t " +
                  " Where a.nzp_type=2 and a.nzp_serv=9 and a.stek=3 " +
                    " and l.nzp_kvar=t.nzp_kvar and a.nzp_counter=t.nzp_counter and l.nzp_counter=ttt_pu_gkal.nzp_counter) " +
                " Where nzp_type = 2 and nzp_serv = 9 and kod_info = 21" +
                  " and exists " +
                  " (select 1 From " + rashod.counters_xx + " a, temp_counters_link l, temp_counters_link t " +
                  " Where a.nzp_type=2 and a.nzp_serv=9 and a.stek=3 " +
                    " and l.nzp_kvar=t.nzp_kvar and a.nzp_counter=t.nzp_counter and l.nzp_counter=ttt_pu_gkal.nzp_counter) "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ret = ExecSQL(conn_db,
                " Update ttt_pu_gkal" +
                " Set val_po = (kf_dpu_plob + kf_dpu_ls * norm_gkal_gvs) " +
                " Where cnt_stage=1 and nzp_serv = 9" +
                " and val_po > (kf_dpu_plob + kf_dpu_ls * norm_gkal_gvs) " +
                " and not exists (Select 1 From ttt_prm_2 p Where ttt_pu_gkal.nzp_dom = p.nzp and p.nzp_prm=1215 ) "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            //  проставить коэффициенты коррекции расходов по Пост.№354 для домов с ОДПУ! если нет ОДПУ коэф-ты уже вычислены!
            ret = ExecSQL(conn_db,
                " update ttt_pu_gkal set " +
                " kf307   =( case when (val_po - kf_dpu_plob)>0.000001" +
                                " then (case when squ1>0.000001" +
                                      " then (val_po - kf_dpu_plob) / squ1 else 0 end)" +
                                " else (case when gil1>0.000001 and (val_po - kf_dpu_plob)<-0.000001" +
                                      " then (val_po - kf_dpu_plob) / gil1 else 0 end)" +
                           " end )," +
                " kf307n  =( case when (val_po - kf_dpu_plob)>0.000001" +
                                " then (case when squ1>0.0001" +
                                      " then (val_po - kf_dpu_plob) / squ1 else 0 end)" +
                                " else (case when gil1>0.000001 and (val_po - kf_dpu_plob)<-0.000001" +
                                      " then (val_po - kf_dpu_plob) / gil1 else 0 end)" +
                           " end )," +
                " kod_info=" +

                          "( case when (val_po - kf_dpu_plob)>0.000001" +
                                " then 21 " +
                                " else (case when (val_po - kf_dpu_plob)<-0.000001" +
                                      " then 31 " +
                                      " else 0 end)" +
                                " end )" +
                " where kod_info = 21 "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            #endregion Расчет расходов ОДН для ГВС - kod_info = 21 / 31

            #region запись результатов расчетов в БД

            #region сохранить в counters_XX
            // 1. сохранить в counters_XX

            // записать домовые значения ОДН по всем типам
            ret = ExecSQL(conn_db,
                " Update " + rashod.counters_xx +
                " Set val_s = a.val_s, val_po = a.val_po," +
                " kf307 = a.kf307, kf307n = a.kf307n, kod_info = a.kod_info, cnt4 = a.cnt4," +
                " kf_dpu_kg = a.kf_dpu_kg, kf_dpu_plot = a.kf_dpu_plot, kf_dpu_plob = a.kf_dpu_plob, kf_dpu_ls = a.kf_dpu_ls, " +
                " squ1 = a.squ1, gil1 = a.gil1, gl7kw = a.norm_gkal_gvs, vl210 = a.vl210, rashod = a.rashod, rvirt = a.rvirt, " +
                " dlt_reval = a.dlt_reval, dlt_real_charge = a.dlt_real_charge " +
                " From ttt_pu_gkal a " +
                " Where a.nzp_cntx = " + rashod.counters_xx + ".nzp_cntx "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            // записать значения ОДН по типу = 21/31 по ЛС в стек=3
            ExecSQL(conn_db, " Drop table ttt_gkal_ls_odn3 ", false);
            ret = ExecSQL(conn_db,
                " create temp table ttt_gkal_ls_odn3 (" +
                " nzp_cntx    integer," +
                " nzp_kvar    integer," +
                " nzp_counter integer default 0," +
                " kod_info    integer default 0," +
                " kf307    " + sDecimalType + "(14,7) default 0.00, " +  //кол-во жильцов в лс
                " dop87    " + sDecimalType + "(14,7) default 0.00, " +  // ОДН в ГКал
                " squ1     " + sDecimalType + "(14,7) default 0.00, " +  //площадь лс
                " gil1     " + sDecimalType + "(14,7) default 0.00 " +  //кол-во жильцов в лс
                " ) " + sUnlogTempTable
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            // выберем в 1ю очередь ГрПУ
            ret = ExecSQL(conn_db,
                " insert into ttt_gkal_ls_odn3 " +
                  " (nzp_cntx,nzp_kvar,nzp_counter,kod_info,kf307,squ1,gil1) " +
                " select " +
                   " c.nzp_cntx,c.nzp_kvar,a.nzp_counter,max(a.kod_info),max(a.kf307),max(c.squ1),max(c.gil1) " +
                " From ttt_pu_gkal a, temp_counters_link l, " + rashod.counters_xx + " c " +
                " Where a.nzp_counter = l.nzp_counter and l.nzp_kvar=c.nzp_kvar " +
                " and c.nzp_serv = 9 and c.stek = 3 and c.nzp_type = 3 and a.nzp_type = 2 and a.kod_info in (21,31) " +
                " group by 1,2,3 "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ExecSQL(conn_db, " Drop table ttt_sel_ls_odn ", false);
            ret = ExecSQL(conn_db, " create temp table ttt_sel_ls_odn (nzp_kvar integer) " + sUnlogTempTable, true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ret = ExecSQL(conn_db, " insert into ttt_sel_ls_odn (nzp_kvar) select distinct nzp_kvar from ttt_gkal_ls_odn3 ", true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ExecSQL(conn_db, " create index ix_ttt_sel_ls_odn on ttt_sel_ls_odn (nzp_kvar) ", true);
            ExecSQL(conn_db, sUpdStat + " ttt_sel_ls_odn ", true);

            // выберем в 2ю очередь ОДПУ, чтобы ЛС не дублировались
            ret = ExecSQL(conn_db,
                " insert into ttt_gkal_ls_odn3 " +
                  " (nzp_cntx,nzp_kvar,kod_info,kf307,squ1,gil1) " +
                " select " +
                   " c.nzp_cntx,c.nzp_kvar,a.kod_info,a.kf307,c.squ1,c.gil1 " +
                " From ttt_pu_gkal a, " + rashod.counters_xx + " c " +
                " Where a.nzp_dom = c.nzp_dom " +
                " and c.nzp_serv = 9 and c.stek = 3 and c.nzp_type = 3 and a.nzp_type = 1 and a.kod_info in (21,31) " +
                " and not exists (select 1 from ttt_sel_ls_odn s where s.nzp_kvar=c.nzp_kvar) "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ExecSQL(conn_db, " create index ix1_ttt_gkal_ls_odn3 on ttt_gkal_ls_odn3 (nzp_cntx) ", true);
            ExecSQL(conn_db, " create index ix2_ttt_gkal_ls_odn3 on ttt_gkal_ls_odn3 (nzp_kvar) ", true);
            ExecSQL(conn_db, sUpdStat + " ttt_gkal_ls_odn3 ", true);

            ret = ExecSQL(conn_db,
                " update ttt_gkal_ls_odn3 " +
                " set dop87 = (case when kod_info = 21 then squ1 else gil1 end) * kf307 " +
                " Where 1=1 "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            // записать значения ОДН по типу = 21/31 по ЛС в стек=20
            ExecSQL(conn_db, " Drop table ttt_gkal_ls_odn20 ", false);
            ret = ExecSQL(conn_db,
                " create temp table ttt_gkal_ls_odn20 (" +
                " nzp_cntx  integer," +
                " nzp_kvar integer," +
                " kod_info integer default 0," +
                " kf307    " + sDecimalType + "(14,7) default 0.00, " +  //кол-во жильцов в лс
                " dop87    " + sDecimalType + "(14,7) default 0.00, " +  // ОДН в ГКал
                " dop87_stek3 " + sDecimalType + "(14,7) default 0.00, " +  // ОДН в ГКал
                " dat_s    DATE," +
                " dat_po   DATE," +
                " cntd     integer default 0, " +
                " cntd_mn  integer default 0 " +
                " ) " + sUnlogTempTable
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ret = ExecSQL(conn_db,
                " insert into ttt_gkal_ls_odn20 " +
                  " (nzp_cntx,nzp_kvar,kod_info,kf307,dop87_stek3,dat_s,dat_po,cntd,cntd_mn) " +
                " select " +
                   " s.nzp_cntx,s.nzp_kvar,a.kod_info,a.kf307,a.dop87,s.dat_s,s.dat_po,k.cntd,k.cntd_mn " +
                " From ttt_gkal_ls_odn3 a, " + rashod.counters_xx + " s, t_gku_periods k " +
                " Where a.nzp_kvar = s.nzp_kvar and s.nzp_kvar = k.nzp_kvar and s.dat_s=k.dp and s.dat_po=k.dp_end " +
                " and s.nzp_serv = 9 and s.stek = 20 and s.nzp_type = 3 "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ExecSQL(conn_db, " create index ix1_ttt_gkal_ls_odn20 on ttt_gkal_ls_odn20 (nzp_cntx) ", true);
            ExecSQL(conn_db, sUpdStat + " ttt_gkal_ls_odn20 ", true);

            ret = ExecSQL(conn_db,
                " Update ttt_gkal_ls_odn20 " +
                " set dop87 = dop87_stek3 * (cntd * 1.00/ cntd_mn) " +
                " Where 1=1 "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            // записать значения ОДН по типу = 21/31 по ЛС в стек=3 в базу
            ret = ExecSQL(conn_db,
                " Update " + rashod.counters_xx +
                " set cls2 = t.kod_info, pu7kw = t.dop87, gl7kw = t.kf307 " +
                " from ttt_gkal_ls_odn3 t where t.nzp_cntx = " + rashod.counters_xx + ".nzp_cntx "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            // записать значения ОДН по типу = 21/31 по ЛС в стек=20 в базу
            ret = ExecSQL(conn_db,
                " Update " + rashod.counters_xx +
                " set cls2 = t.kod_info, pu7kw = t.dop87, gl7kw = t.kf307 " +
                " from ttt_gkal_ls_odn20 t where t.nzp_cntx = " + rashod.counters_xx + ".nzp_cntx "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ExecSQL(conn_db, " Drop table ttt_sel_ls_odn ", false);
            ExecSQL(conn_db, " Drop table ttt_gkal_ls_odn3 ", false);
            ExecSQL(conn_db, " Drop table ttt_gkal_ls_odn20 ", false);

            #endregion сохранить в counters_XX

            // 2. сохранить в prm_2 домовые параметры

            #region выберем дома для вставки нормы на 1 куб.м для ОДПУ - ttt_cur_doms

            ret = ExecSQL(conn_db, " Drop table ttt_cur_doms ", true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ret = ExecSQL(conn_db,
                " Create Temp table ttt_cur_doms (nzp integer) " + sUnlogTempTable
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ret = ExecSQL(conn_db,
                " Insert into ttt_cur_doms (nzp)" +
                " Select nzp_dom from ttt_pu_gkal where nzp_type = 1 and kod_info in (201,202,204) Group by 1 "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ExecSQL(conn_db, " create index ix_ttt_cur_doms on ttt_cur_doms (nzp) ", false);
            ExecSQL(conn_db, sUpdStat + " ttt_cur_doms ", false);

            #endregion выберем дома для вставки нормы на 1 куб.м для ОДПУ - ttt_cur_doms

            // чистка нормы на 1 куб.м перед вставкой по домам
            CutPeriods(conn_db, rashod.paramcalc.data_alias, "prm_2", "436", "ttt_cur_doms",
                rashod.paramcalc.dat_s, rashod.paramcalc.dat_po, out ret);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            #region выберем ЛС для вставки нормы на 1 куб.м для ГрПУ - ttt_cur_lss

            ret = ExecSQL(conn_db, " Drop table ttt_cur_lss ", true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ret = ExecSQL(conn_db,
                " Create Temp table ttt_cur_lss (nzp integer) " + sUnlogTempTable
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ret = ExecSQL(conn_db,
                " Insert into ttt_cur_lss (nzp)" +
                " Select b.nzp_kvar " +
                " From ttt_pu_gkal a, temp_counters_link b " +
                " Where a.kod_info in (201,202,204) and a.nzp_type = 2 " +
                  " and a.nzp_counter=b.nzp_counter " +
                " Group by 1 "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ExecSQL(conn_db, " create index ix_ttt_cur_lss on ttt_cur_lss (nzp) ", false);
            ExecSQL(conn_db, sUpdStat + " ttt_cur_lss ", false);

            #endregion выберем ЛС для вставки нормы на 1 куб.м для ГрПУ - ttt_cur_lss

            // чистка нормы на 1 куб.м перед вставкой по ЛС
            CutPeriods(conn_db, rashod.paramcalc.data_alias, "prm_1", "894", "ttt_cur_lss",
                rashod.paramcalc.dat_s, rashod.paramcalc.dat_po, out ret);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            #region выберем дома для вставки нормы на 1 кв.м для ОДПУ - ttt_cur_doms

            ret = ExecSQL(conn_db, " Drop table ttt_cur_doms ", true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ret = ExecSQL(conn_db,
                " Create Temp table ttt_cur_doms (nzp integer) " + sUnlogTempTable
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ret = ExecSQL(conn_db,
                " Insert into ttt_cur_doms (nzp)" +
                " Select nzp_dom from ttt_pu_gkal where nzp_type = 1 and kod_info in (203,204) Group by 1 "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ExecSQL(conn_db, " create index ix_ttt_cur_doms on ttt_cur_doms (nzp) ", false);
            ExecSQL(conn_db, sUpdStat + " ttt_cur_doms ", false);

            #endregion выберем дома для вставки нормы на 1 кв.м для ОДПУ - ttt_cur_doms

            // чистка нормы на 1 кв.м перед вставкой по домам
            CutPeriods(conn_db, rashod.paramcalc.data_alias, "prm_2", "723", "ttt_cur_doms",
                rashod.paramcalc.dat_s, rashod.paramcalc.dat_po, out ret);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            #region выберем ЛС для вставки нормы на 1 кв.м для ГрПУ - ttt_cur_lss

            ret = ExecSQL(conn_db, " Drop table ttt_cur_lss ", true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ret = ExecSQL(conn_db,
                " Create Temp table ttt_cur_lss (nzp integer) " + sUnlogTempTable
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ret = ExecSQL(conn_db,
                " Insert into ttt_cur_lss (nzp)" +
                " Select b.nzp_kvar " +
                " From ttt_pu_gkal a, temp_counters_link b " +
                " Where a.kod_info in (203,204) and a.nzp_type = 2 " +
                  " and a.nzp_counter=b.nzp_counter " +
                " Group by 1 "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ExecSQL(conn_db, " create index ix_ttt_cur_lss on ttt_cur_lss (nzp) ", false);
            ExecSQL(conn_db, sUpdStat + " ttt_cur_lss ", false);

            #endregion выберем ЛС для вставки нормы на 1 кв.м для ГрПУ - ttt_cur_lss

            // чистка нормы на 1 кв.м перед вставкой по ЛС
            CutPeriods(conn_db, rashod.paramcalc.data_alias, "prm_1", "2463", "ttt_cur_lss",
                rashod.paramcalc.dat_s, rashod.paramcalc.dat_po, out ret);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            #region вставка домовых параметров в БД

            ret = ExecSQL(conn_db, " Drop table iii_prm_2 ", true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ret = ExecSQL(conn_db,
                " CREATE TEMP TABLE iii_prm_2 (" +
                "   nzp     integer," +
                "   nzp_prm integer," +
                "   val_prm character(20)," +
                "   dat_s      date," +
                "   dat_po     date," +
                "   is_actual integer," +
                "   nzp_user  integer," +
                "   dat_when  date " +
                " ) " + sUnlogTempTable
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            // для ОДПУ по ГКал - вставка по домам нормы в ГКал на 1 куб.м для ГВС
            ret = ExecSQL(conn_db,
                " insert into iii_prm_2 " +
                " (nzp,nzp_prm,dat_s,dat_po,val_prm,is_actual,nzp_user,dat_when) " +
                " Select a.nzp_dom, 436, " + rashod.paramcalc.dat_s + "," + rashod.paramcalc.dat_po + ", a.kf307,1," +
                  rashod.paramcalc.nzp_user + "," + sCurDate +
                " From ttt_pu_gkal a " +
                " Where a.kod_info in (201,202,204) and a.nzp_type = 1 " +
                " Group by 1,5 "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            // для ОДПУ по ГКал - вставка по домам нормы в ГКал на 1 кв.м для отопления
            ret = ExecSQL(conn_db,
                " insert into iii_prm_2 " +
                " (nzp,nzp_prm,dat_s,dat_po,val_prm,is_actual,nzp_user,dat_when) " +
                " Select a.nzp_dom, 723, " + rashod.paramcalc.dat_s + "," + rashod.paramcalc.dat_po + ", a.kf307n,1," +
                  rashod.paramcalc.nzp_user + "," + sCurDate +
                " From ttt_pu_gkal a " +
                " Where a.kod_info in (203,204) and a.nzp_type = 1 " +
                " Group by 1,5 "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ExecSQL(conn_db, " Create index ix1_iii_prm_2 on iii_prm_2 (nzp) ", true);
            ExecSQL(conn_db, sUpdStat + " iii_prm_2 ", true);

            ret = ExecSQL(conn_db,
                " insert into " + rashod.paramcalc.data_alias + "prm_2 " +
                " (nzp,nzp_prm,dat_s,dat_po,val_prm,is_actual,nzp_user,dat_when) " +
                " Select nzp,nzp_prm,dat_s,dat_po,val_prm,is_actual,nzp_user,dat_when" +
                " From iii_prm_2 " +
                " Where 1 = 1 "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            #endregion вставка домовых параметров в БД

            #region вставка квартирных параметров в БД

            ret = ExecSQL(conn_db, " Drop table iii_prm_1 ", true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ret = ExecSQL(conn_db,
                " CREATE TEMP TABLE iii_prm_1 (" +
                "   nzp     integer," +
                "   nzp_prm integer," +
                "   val_prm character(20)," +
                "   dat_s      date," +
                "   dat_po     date," +
                "   is_actual integer," +
                "   nzp_user  integer," +
                "   dat_when  date " +
                " ) " + sUnlogTempTable
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            // для ГрПУ по ГКал - вставка по ЛС нормы в ГКал на 1 куб.м для ГВС
            ret = ExecSQL(conn_db,
                " insert into iii_prm_1 " +
                " (nzp,nzp_prm,dat_s,dat_po,val_prm,is_actual,nzp_user,dat_when) " +
                " Select b.nzp_kvar, 894, " + rashod.paramcalc.dat_s + "," + rashod.paramcalc.dat_po + ", a.kf307,1," +
                  rashod.paramcalc.nzp_user + "," + sCurDate +
                " From ttt_pu_gkal a, temp_counters_link b " +
                " Where a.kod_info in (201,202,204) and a.nzp_type = 2 " +
                  " and a.nzp_counter=b.nzp_counter " +
                " Group by 1,5 "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            //2463|Квартирный норматив на 1 ГКал/кв.м для отопления|1||float||1|0|1000000|7|
            // для ГрПУ по ГКал - вставка по ЛС нормы в ГКал на 1 куб.м для ГВС
            ret = ExecSQL(conn_db,
                " insert into iii_prm_1 " +
                " (nzp,nzp_prm,dat_s,dat_po,val_prm,is_actual,nzp_user,dat_when) " +
                " Select b.nzp_kvar, 2463, " + rashod.paramcalc.dat_s + "," + rashod.paramcalc.dat_po + ", a.kf307n,1," +
                  rashod.paramcalc.nzp_user + "," + sCurDate +
                " From ttt_pu_gkal a, temp_counters_link b " +
                " Where a.kod_info in (203,204) and a.nzp_type = 2 " +
                  " and a.nzp_counter=b.nzp_counter " +
                " Group by 1,5 "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ExecSQL(conn_db, " Create index ix1_iii_prm_1 on iii_prm_1 (nzp) ", true);
            ExecSQL(conn_db, sUpdStat + " iii_prm_1 ", true);

            ret = ExecSQL(conn_db,
                " insert into " + rashod.paramcalc.data_alias + "prm_1 " +
                " (nzp,nzp_prm,dat_s,dat_po,val_prm,is_actual,nzp_user,dat_when) " +
                " Select nzp,nzp_prm,dat_s,dat_po,val_prm,is_actual,nzp_user,dat_when" +
                " From iii_prm_1 " +
                " Where 1 = 1 "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            #endregion вставка квартирных параметров в БД

            #endregion запись результатов расчетов в БД

            #region перевыбрать временные таблицы для расчетов из БД

            ret = ExecSQL(conn_db, " delete from ttt_prm_2  where nzp_prm in (436,723) and Exists (select 1 from iii_prm_2 b Where ttt_prm_2.nzp = b.nzp) ", true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }
            ret = ExecSQL(conn_db, " delete from ttt_prm_2d where nzp_prm in (436,723) and Exists (select 1 from iii_prm_2 b Where ttt_prm_2d.nzp = b.nzp) ", true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }
            ret = ExecSQL(conn_db, " delete from ttt_prm_2f where nzp_prm in (436,723)" +
                                   " and Exists (select 1 from iii_prm_2 b Where ttt_prm_2f.nzp = b.nzp) " +
                                   " and dat_s  <= " + rashod.paramcalc.dat_po +
                                   " and dat_po >= " + rashod.paramcalc.dat_s , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ret = ExecSQL(conn_db, " delete from ttt_prm_1  where nzp_prm in (894,2463) and Exists (select 1 from iii_prm_1 b Where ttt_prm_1.nzp = b.nzp) ", true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }
            ret = ExecSQL(conn_db, " delete from ttt_prm_1d where nzp_prm in (894,2463) and Exists (select 1 from iii_prm_1 b Where ttt_prm_1d.nzp = b.nzp) ", true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }
            ret = ExecSQL(conn_db, " delete from ttt_prm_1f where nzp_prm in (894,2463) and" +
                                   " Exists (select 1 from iii_prm_1 b Where ttt_prm_1f.nzp = b.nzp) " +
                                   " and dat_s  <= " + rashod.paramcalc.dat_po +
                                   " and dat_po >= " + rashod.paramcalc.dat_s, true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            string s;

            #region перевыбрать домовые временные таблицы для расчетов из БД

            s = " and Exists (select 1 from iii_prm_2 b Where a.nzp = b.nzp) ";

            ret = ExecSQL(conn_db,
                " insert into ttt_prm_2f (nzp_key,nzp,nzp_prm,val_prm,dat_s,dat_po) " +
                " Select min(a.nzp_key) nzp_key,a.nzp,a.nzp_prm,a.val_prm,a.dat_s,a.dat_po " +
                " From " + rashod.paramcalc.data_alias + "prm_2 a " +
                " where a.nzp_prm in (436,723) and a.is_actual<>100 " + s +
                "   and a.dat_s  <= " + rashod.paramcalc.dat_po +
                "   and a.dat_po >= " + rashod.paramcalc.dat_s +
                " group by 2,3,4,5,6 "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ExecSQL(conn_db, sUpdStat + " ttt_prm_2f ", false);

            ret = ExecSQL(conn_db,
                " insert into ttt_prm_2d (nzp_key,nzp,nzp_prm,val_prm,dat_s,dat_po) " +
                " Select min(a.nzp_key) nzp_key,a.nzp,a.nzp_prm,a.val_prm,a.dat_s,a.dat_po " +
                " From ttt_prm_2f a " +
                " where a.nzp_prm in (436,723) " + s +
                "   and a.dat_s  <= " + rashod.paramcalc.dat_po +
                "   and a.dat_po >= " + rashod.paramcalc.dat_s +
                " group by 2,3,4,5,6 "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ExecSQL(conn_db, sUpdStat + " ttt_prm_2d ", false);

            ret = ExecSQL(conn_db,
                " insert into ttt_prm_2 (nzp_key,nzp,nzp_prm,val_prm,dat_s,dat_po) " +
                " Select min(a.nzp_key) nzp_key,a.nzp,a.nzp_prm,max(a.val_prm) val_prm,min(a.dat_s) dat_s,max(a.dat_po) dat_po " +
                " From ttt_prm_2d a " +
                " where a.nzp_prm in (436,723) " + s +
                " group by 2,3 "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ExecSQL(conn_db, sUpdStat + " ttt_prm_2 ", true);

            #endregion перевыбрать домовые временные таблицы для расчетов из БД

            #region перевыбрать квартирные временные таблицы для расчетов из БД

            s = " and Exists (select 1 from iii_prm_1 b Where a.nzp = b.nzp) ";

            ret = ExecSQL(conn_db,
                " insert into ttt_prm_1f (nzp_key,nzp,nzp_prm,val_prm,dat_s,dat_po) " +
                " Select min(a.nzp_key) nzp_key,a.nzp,a.nzp_prm,a.val_prm,a.dat_s,a.dat_po " +
                " From " + rashod.paramcalc.data_alias + "prm_1 a " +
                " where a.nzp_prm in (894,2463) and a.is_actual<>100 " + s +
                "   and a.dat_s  <= " + rashod.paramcalc.dat_po +
                "   and a.dat_po >= " + rashod.paramcalc.dat_s +
                " group by 2,3,4,5,6 "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ExecSQL(conn_db, sUpdStat + " ttt_prm_1f ", false);

            ret = ExecSQL(conn_db,
                " insert into ttt_prm_1d (nzp_key,nzp,nzp_prm,val_prm,dat_s,dat_po) " +
                " Select min(a.nzp_key) nzp_key,a.nzp,a.nzp_prm,a.val_prm,a.dat_s,a.dat_po " +
                " From ttt_prm_1f a " +
                " where a.nzp_prm in (894,2463) " + s +
                "   and a.dat_s  <= " + rashod.paramcalc.dat_po +
                "   and a.dat_po >= " + rashod.paramcalc.dat_s +
                " group by 2,3,4,5,6 "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ExecSQL(conn_db, sUpdStat + " ttt_prm_1d ", false);

            ret = ExecSQL(conn_db,
                " insert into ttt_prm_1 (nzp_key,nzp,nzp_prm,val_prm,dat_s,dat_po) " +
                " Select min(a.nzp_key) nzp_key,a.nzp,a.nzp_prm,max(a.val_prm) val_prm,min(a.dat_s) dat_s,max(a.dat_po) dat_po " +
                " From ttt_prm_1d a " +
                " where a.nzp_prm in (894,2463) " + s +
                " group by 2,3 "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ExecSQL(conn_db, sUpdStat + " ttt_prm_1 ", true);

            #endregion перевыбрать квартирные временные таблицы для расчетов из БД

            ret = ExecSQL(conn_db, " Drop table iii_prm_1 ", true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }
            ret = ExecSQL(conn_db, " Drop table iii_prm_2 ", true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ret = ExecSQL(conn_db, " Drop table ttt_cur_lss ", true);
            ret = ExecSQL(conn_db, " Drop table ttt_cur_doms ", true);

            #endregion перевыбрать временные таблицы для расчетов из БД

            return true;
        }
        #region учет ПУ от ГКал по ЛС - запись в стек = 3/20
        bool Stek39UchetPUgkalLS(IDbConnection conn_db, Rashod rashod, out Returns ret)
        //--------------------------------------------------------------------------------
        {
            ret = Utils.InitReturns();

            #region Выборка ПУ для расчета ГВС - kod_info = 21 / 31

            ExecSQL(conn_db, " Drop table ttt_pu_gkal ", false);

            ret = ExecSQL(conn_db,
                " CREATE temp TABLE ttt_pu_gkal(        " +
                "   nzp_cntx INTEGER NOT NULL,          " +
                "   nzp_dom  INTEGER NOT NULL,          " +
                "   nzp_kvar INTEGER default 0 NOT NULL," +
                "   nzp_type INTEGER NOT NULL,          " +
                "   nzp_serv INTEGER NOT NULL,          " +
                "   dat_charge DATE,                    " +
                "   cur_zap INTEGER default 0 NOT NULL, " +
                "   nzp_counter INTEGER default 0,      " +
                "   cnt_stage INTEGER default 0,        " +
                "   mmnog " + sDecimalType + "(15,7) default 1.0000000," +
                "   stek INTEGER default 0 NOT NULL,    " +
                "   rashod " + sDecimalType + "(15,7) default 0.0000000 NOT NULL, " +
                "   dat_s DATE NOT NULL,                " +
                "   val_s " + sDecimalType + "(15,7) default 0.0000000 NOT NULL, " +
                "   dat_po DATE NOT NULL,               " +
                "   val_po " + sDecimalType + "(15,7) default 0.0000000 NOT NULL, " +
                "   val1 " + sDecimalType + "(15,7) default 0.0000000 NOT NULL, " +
                "   val2 " + sDecimalType + "(15,7) default 0.0000000 NOT NULL, " +
                "   val3 " + sDecimalType + "(15,7) default 0.0000000, " +
                "   val4 " + sDecimalType + "(15,7) default 0.0000000, " +
                "   rvirt " + sDecimalType + "(15,7) default 0.0000000, " +
                "   squ1 " + sDecimalType + "(15,7) default 0.0000000, " +
                "   squ2 " + sDecimalType + "(15,7) default 0.0000000, " +
                "   cls1 INTEGER default 0 NOT NULL, " +
                "   cls2 INTEGER default 0 NOT NULL, " +
                "   gil1 " + sDecimalType + "(15,7) default 0.0000000, " +
                "   gil2 " + sDecimalType + "(15,7) default 0.0000000, " +
                "   cnt1 INTEGER default 0 NOT NULL, " +
                "   cnt2 INTEGER default 0 NOT NULL, " +
                "   cnt3 INTEGER default 0,          " +
                "   cnt4 INTEGER default 0,          " +
                "   cnt5 INTEGER default 0,          " +
                "   dop87 " + sDecimalType + "(15,7) default 0.0000000, " +
                "   pu7kw " + sDecimalType + "(15,7) default 0.0000000, " +
                "   gl7kw " + sDecimalType + "(15,7) default 0.0000000, " +
                "   vl210 " + sDecimalType + "(15,7) default 0.0000000, " +
                "   kf307 " + sDecimalType + "(15,7) default 0.0000000, " +
                "   kf307n " + sDecimalType + "(15,7) default 0.0000000, " +
                "   kf307f9 " + sDecimalType + "(15,7) default 0.0000000, " +
                "   kf_dpu_kg " + sDecimalType + "(15,7) default 0.0000000, " +
                "   kf_dpu_plob " + sDecimalType + "(15,7) default 0.0000000, " +
                "   kf_dpu_plot " + sDecimalType + "(15,7) default 0.0000000, " +
                "   kf_dpu_ls " + sDecimalType + "(15,7) default 0.0000000, " +
                "   dlt_in " + sDecimalType + "(15,7) default 0.0000000, " +
                "   dlt_cur " + sDecimalType + "(15,7) default 0.0000000, " +
                "   dlt_reval " + sDecimalType + "(15,7) default 0.0000000, " +
                "   dlt_real_charge " + sDecimalType + "(15,7) default 0.0000000, " +
                "   dlt_calc " + sDecimalType + "(15,7) default 0.0000000,     " +
                "   dlt_out " + sDecimalType + "(15,7) default 0.0000000,     " +
                "   kod_info INTEGER default 0 " +
                " ) " + sUnlogTempTable
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ret = ExecSQL(conn_db,
                " insert into ttt_pu_gkal" +
                      "(nzp_cntx, nzp_dom, nzp_kvar, nzp_type, nzp_serv, dat_charge, cur_zap, nzp_counter, cnt_stage, mmnog, stek, rashod, " +
                      " dat_s, val_s, dat_po, val_po, val1, val2, val3, val4, rvirt, squ1, squ2, gil1, gil2, cnt1, cnt2, cnt3, cnt4, " +
                      " cnt5, dop87, pu7kw, gl7kw, vl210, kf307, kod_info " +
                      " ,cls1, cls2, kf_dpu_kg, kf_dpu_plob, kf_dpu_plot, kf_dpu_ls, kf307n, kf307f9, dlt_cur, dlt_reval, dlt_real_charge) " +
                " Select" +
                      " nzp_cntx, nzp_dom, nzp_kvar, nzp_type, nzp_serv, dat_charge, cur_zap, nzp_counter, cnt_stage, mmnog, stek, rashod, " +
                      " dat_s, val_s, dat_po, val_po, val1, val2, val3, val4, rvirt, squ1, squ2, gil1, gil2, cnt1, cnt2, cnt3,0 cnt4, " +
                      " cnt5, dop87, pu7kw, gl7kw, vl210, kf307, kod_info " +
                      " ,cls1, cls2, kf_dpu_kg, kf_dpu_plob, kf_dpu_plot, kf_dpu_ls, kf307n, kf307f9, dlt_cur, dlt_reval, dlt_real_charge " +
                      " From " + rashod.counters_xx +
                " Where nzp_type in (1,2) and nzp_serv in (9) and stek=39 and kod_info in (21,31) and " + rashod.where_dom + rashod.paramcalc.per_dat_charge +
                " "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ExecSQL(conn_db, " create index ix0_ttt_pu_gkal on ttt_pu_gkal (nzp_cntx) ", true);
            ExecSQL(conn_db, " create index ix1_ttt_pu_gkal on ttt_pu_gkal (nzp_dom,nzp_serv) ", true);
            ExecSQL(conn_db, " create index ix2_ttt_pu_gkal on ttt_pu_gkal (nzp_counter,nzp_serv) ", true);
            ExecSQL(conn_db, sUpdStat + " ttt_pu_gkal ", true);

            #endregion Выборка ПУ для расчета ГВС - kod_info = 21 / 31

            #region сохранить в counters_XX
            // 1. сохранить в counters_XX

            // записать значения ОДН по типу = 21/31 по ЛС в стек=3
            ExecSQL(conn_db, " Drop table ttt_gkal_ls_odn3 ", false);
            ret = ExecSQL(conn_db,
                " create temp table ttt_gkal_ls_odn3 (" +
                " nzp_cntx    integer," +
                " nzp_kvar    integer," +
                " nzp_counter integer default 0," +
                " kod_info    integer default 0," +
                " kf307    " + sDecimalType + "(14,7) default 0.00, " +  //кол-во жильцов в лс
                " dop87    " + sDecimalType + "(14,7) default 0.00, " +  // ОДН в ГКал
                " squ1     " + sDecimalType + "(14,7) default 0.00, " +  //площадь лс
                " gil1     " + sDecimalType + "(14,7) default 0.00 " +  //кол-во жильцов в лс
                " ) " + sUnlogTempTable
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            // выберем в 1ю очередь ГрПУ
            ret = ExecSQL(conn_db,
                " insert into ttt_gkal_ls_odn3 " +
                  " (nzp_cntx,nzp_kvar,nzp_counter,kod_info,kf307,squ1,gil1) " +
                " select " +
                   " k.nzp_cntx,k.nzp_kvar,a.nzp_counter,max(a.kod_info),max(a.kf307),max(k.squ1),max(k.gil1) " +
                " From ttt_pu_gkal a, temp_counters_link l, " + rashod.counters_xx + " k " +
                " Where a.nzp_counter = l.nzp_counter and l.nzp_kvar=k.nzp_kvar " + rashod.where_kvarK +
                " and k.nzp_serv = 9 and k.stek = 3 and k.nzp_type = 3 and a.nzp_type = 2 and a.kod_info in (21,31) " +
                " group by 1,2,3 "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ExecSQL(conn_db, " Drop table ttt_sel_ls_odn ", false);
            ret = ExecSQL(conn_db, " create temp table ttt_sel_ls_odn (nzp_kvar integer) " + sUnlogTempTable, true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ret = ExecSQL(conn_db, " insert into ttt_sel_ls_odn (nzp_kvar) select distinct nzp_kvar from ttt_gkal_ls_odn3 ", true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ExecSQL(conn_db, " create index ix_ttt_sel_ls_odn on ttt_sel_ls_odn (nzp_kvar) ", true);
            ExecSQL(conn_db, sUpdStat + " ttt_sel_ls_odn ", true);

            // выберем в 2ю очередь ОДПУ, чтобы ЛС не дублировались
            ret = ExecSQL(conn_db,
                " insert into ttt_gkal_ls_odn3 " +
                  " (nzp_cntx,nzp_kvar,kod_info,kf307,squ1,gil1) " +
                " select " +
                   " k.nzp_cntx,k.nzp_kvar,a.kod_info,a.kf307,k.squ1,k.gil1 " +
                " From ttt_pu_gkal a, " + rashod.counters_xx + " k " +
                " Where a.nzp_dom = k.nzp_dom " + rashod.where_kvarK +
                " and k.nzp_serv = 9 and k.stek = 3 and k.nzp_type = 3 and a.nzp_type = 1 and a.kod_info in (21,31) " +
                " and not exists (select 1 from ttt_sel_ls_odn s where s.nzp_kvar=k.nzp_kvar) "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ExecSQL(conn_db, " create index ix1_ttt_gkal_ls_odn3 on ttt_gkal_ls_odn3 (nzp_cntx) ", true);
            ExecSQL(conn_db, " create index ix2_ttt_gkal_ls_odn3 on ttt_gkal_ls_odn3 (nzp_kvar) ", true);
            ExecSQL(conn_db, sUpdStat + " ttt_gkal_ls_odn3 ", true);

            ret = ExecSQL(conn_db,
                " update ttt_gkal_ls_odn3 " +
                " set dop87 = (case when kod_info = 21 then squ1 else gil1 end) * kf307 " +
                " Where 1=1 "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            // записать значения ОДН по типу = 21/31 по ЛС в стек=20
            ExecSQL(conn_db, " Drop table ttt_gkal_ls_odn20 ", false);
            ret = ExecSQL(conn_db,
                " create temp table ttt_gkal_ls_odn20 (" +
                " nzp_cntx  integer," +
                " nzp_kvar integer," +
                " kod_info integer default 0," +
                " kf307    " + sDecimalType + "(14,7) default 0.00, " +  //кол-во жильцов в лс
                " dop87    " + sDecimalType + "(14,7) default 0.00, " +  // ОДН в ГКал
                " dop87_stek3 " + sDecimalType + "(14,7) default 0.00, " +  // ОДН в ГКал
                " dat_s    DATE," +
                " dat_po   DATE," +
                " cntd     integer default 0, " +
                " cntd_mn  integer default 0 " +
                " ) " + sUnlogTempTable
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ret = ExecSQL(conn_db,
                " insert into ttt_gkal_ls_odn20 " +
                  " (nzp_cntx,nzp_kvar,kod_info,kf307,dop87_stek3,dat_s,dat_po,cntd,cntd_mn) " +
                " select " +
                   " s.nzp_cntx,s.nzp_kvar,a.kod_info,a.kf307,a.dop87,s.dat_s,s.dat_po,k.cntd,k.cntd_mn " +
                " From ttt_gkal_ls_odn3 a, " + rashod.counters_xx + " s, t_gku_periods k " +
                " Where a.nzp_kvar = s.nzp_kvar and s.nzp_kvar = k.nzp_kvar and s.dat_s=k.dp and s.dat_po=k.dp_end " +
                " and s.nzp_serv = 9 and s.stek = 20 and s.nzp_type = 3 "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ExecSQL(conn_db, " create index ix1_ttt_gkal_ls_odn20 on ttt_gkal_ls_odn20 (nzp_cntx) ", true);
            ExecSQL(conn_db, sUpdStat + " ttt_gkal_ls_odn20 ", true);

            ret = ExecSQL(conn_db,
                " Update ttt_gkal_ls_odn20 " +
                " set dop87 = dop87_stek3 * (cntd * 1.00/ cntd_mn) " +
                " Where 1=1 "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            // записать значения ОДН по типу = 21/31 по ЛС в стек=3 в базу
            ret = ExecSQL(conn_db,
                " Update " + rashod.counters_xx +
                " set cls2 = t.kod_info, pu7kw = t.dop87, gl7kw = t.kf307 " +
                " from ttt_gkal_ls_odn3 t where t.nzp_cntx = " + rashod.counters_xx + ".nzp_cntx "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            // записать значения ОДН по типу = 21/31 по ЛС в стек=20 в базу
            ret = ExecSQL(conn_db,
                " Update " + rashod.counters_xx +
                " set cls2 = t.kod_info, pu7kw = t.dop87, gl7kw = t.kf307 " +
                " from ttt_gkal_ls_odn20 t where t.nzp_cntx = " + rashod.counters_xx + ".nzp_cntx "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ExecSQL(conn_db, " Drop table ttt_sel_ls_odn ", false);
            ExecSQL(conn_db, " Drop table ttt_gkal_ls_odn3 ", false);
            ExecSQL(conn_db, " Drop table ttt_gkal_ls_odn20 ", false);
            ExecSQL(conn_db, " Drop table ttt_pu_gkal ", false);

            #endregion сохранить в counters_XX

            return true;
        }
        #endregion учет ПУ от ГКал по ЛС - запись в стек = 3/20

        bool ClearStek39UchetPUgkal(IDbConnection conn_db, Rashod rashod, out Returns ret)
        //--------------------------------------------------------------------------------
        {
            ret = Utils.InitReturns();

            #region Выборка ПУ для расчета Отопления и ГВС - kod_info = 201 / 202 / 203 / 204

            ExecSQL(conn_db, " Drop table ddd_pu_gkal ", false);

            ret = ExecSQL(conn_db,
                " CREATE temp TABLE ddd_pu_gkal(        " +
                "   nzp_dom  INTEGER NOT NULL,          " +
                "   nzp_kvar INTEGER default 0 NOT NULL," +
                "   nzp_type INTEGER NOT NULL,          " +
                "   nzp_serv INTEGER NOT NULL,          " +
                "   nzp_counter INTEGER default 0,      " +
                "   cnt4 INTEGER default 0,          " +
                "   kod_info INTEGER default 0 " +
                " ) " + sUnlogTempTable
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ret = ExecSQL(conn_db,
                " insert into ddd_pu_gkal (nzp_dom, nzp_kvar, nzp_type, nzp_serv, nzp_counter, cnt4, kod_info) " +
                " Select nzp_dom, nzp_kvar, nzp_type, nzp_serv, nzp_counter, cnt4, kod_info " +
                " From " + rashod.counters_xx +
                " Where nzp_type in (1,2) and nzp_serv in (8,9) and stek=39 and " + rashod.where_dom + rashod.where_kvar + rashod.paramcalc.per_dat_charge +
                " "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ExecSQL(conn_db, " create index ix1_ddd_pu_gkal on ddd_pu_gkal (nzp_dom,nzp_serv) ", true);
            ExecSQL(conn_db, " create index ix2_ddd_pu_gkal on ddd_pu_gkal (nzp_counter,nzp_serv) ", true);
            ExecSQL(conn_db, sUpdStat + " ddd_pu_gkal ", true);

            #endregion Выборка ПУ для расчета Отопления и ГВС - kod_info = 201 / 202 / 203 / 204

            #region Установка расчета Отопления и ГВС - kod_info = 201 / 202 / 203 / 204
            // Признак алгоритма учета ПУ от ГКал - используется,что он ОДИН на дом!
            ret = ExecSQL(conn_db,
                " update ddd_pu_gkal " +
                " set cnt4 = ( Select max( " + sNvlWord + "(p.val_prm,'0')" + sConvToInt + " ) From ttt_prm_2 p " +
                          " Where ddd_pu_gkal.nzp_dom  = p.nzp and p.nzp_prm = 1397 ) " +
                " Where 0 < ( Select count(*) From ttt_prm_2 p " +
                          " Where ddd_pu_gkal.nzp_dom  = p.nzp and p.nzp_prm = 1397 ) "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            // kod_info = 201 - Расчет ГВС по ГКал / kod_info = 202 - Расчет ГВС по ГКал(по ДПУ).
            // Этот ОДПУ по ГВС!
            ret = ExecSQL(conn_db,
                " update ddd_pu_gkal " +
                " set kod_info = cnt4 + 200 " +
                " Where nzp_serv=9 and nzp_type=1 and cnt4 in (1,2) "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            // ГрПУ от ГКал не применим в случае если на доме установлен тип расчета от ГКал - "Расчет ГВС по ГКал(по ДПУ)",
            // т.к. невозможно сопоставить ГрПУ от ГКал с ГрПУ по куб.м.!
            ret = ExecSQL(conn_db,
                " update ddd_pu_gkal " +
                " set kod_info = cnt4 + 200 " +
                " Where nzp_serv=9 and nzp_type=2 and cnt4=1 "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            // kod_info = 204 - Расчет Отопления и ГВС по ГКал / kod_info = 203 - Расчет Отопления и ГВС(норматив) по ГКал.
            // Этот ОДПУ по Отоплению!
            ret = ExecSQL(conn_db,
                " update ddd_pu_gkal " +
                " set kod_info = cnt4 + 200 " +
                " Where nzp_serv=8 and cnt4 in (3,4) "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ExecSQL(conn_db, " create index ix3_ddd_pu_gkal on ddd_pu_gkal (kod_info) ", true);
            ExecSQL(conn_db, sUpdStat + " ddd_pu_gkal ", true);

            #endregion Выборка ПУ для расчета Отопления и ГВС - kod_info = 201 / 202 / 203 / 204

            #region удаление результатов расчетов ПУ от ГКал из временных таблиц параметров

            #region выберем дома для вставки нормы на 1 куб.м для ОДПУ - ttt_cur_doms

            ret = ExecSQL(conn_db, " Drop table ttt_cur_doms ", true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ret = ExecSQL(conn_db,
                " Create Temp table ttt_cur_doms (nzp integer) " + sUnlogTempTable
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ret = ExecSQL(conn_db,
                " Insert into ttt_cur_doms (nzp)" +
                " Select nzp_dom from ddd_pu_gkal where nzp_type = 1 and kod_info in (201,202,204) Group by 1 "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ExecSQL(conn_db, " create index ix_ttt_cur_doms on ttt_cur_doms (nzp) ", false);
            ExecSQL(conn_db, sUpdStat + " ttt_cur_doms ", false);

            #endregion выберем дома для вставки нормы на 1 куб.м для ОДПУ - ttt_cur_doms

            // чистка нормы на 1 куб.м перед вставкой по домам
            ret = ExecSQL(conn_db, " delete from ttt_prm_2  where nzp_prm=436 and Exists (select 1 from ttt_cur_doms b Where ttt_prm_2.nzp = b.nzp) ", true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }
            ret = ExecSQL(conn_db, " delete from ttt_prm_2d where nzp_prm=436 and Exists (select 1 from ttt_cur_doms b Where ttt_prm_2d.nzp = b.nzp) ", true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            #region выберем ЛС для вставки нормы на 1 куб.м для ГрПУ - ttt_cur_lss

            ret = ExecSQL(conn_db, " Drop table ttt_cur_lss ", true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ret = ExecSQL(conn_db,
                " Create Temp table ttt_cur_lss (nzp integer) " + sUnlogTempTable
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ret = ExecSQL(conn_db,
                " Insert into ttt_cur_lss (nzp)" +
                " Select b.nzp_kvar " +
                " From ddd_pu_gkal a, temp_counters_link b " +
                " Where a.kod_info in (201,202,204) and a.nzp_type = 2 " +
                  " and a.nzp_counter=b.nzp_counter " +
                " Group by 1 "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ExecSQL(conn_db, " create index ix_ttt_cur_lss on ttt_cur_lss (nzp) ", false);
            ExecSQL(conn_db, sUpdStat + " ttt_cur_lss ", false);

            #endregion выберем ЛС для вставки нормы на 1 куб.м для ГрПУ - ttt_cur_lss

            // чистка нормы на 1 куб.м перед вставкой по ЛС
            ret = ExecSQL(conn_db, " delete from ttt_prm_1  where nzp_prm=894 and Exists (select 1 from ttt_cur_lss b Where ttt_prm_1.nzp = b.nzp) ", true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }
            ret = ExecSQL(conn_db, " delete from ttt_prm_1d where nzp_prm=894 and Exists (select 1 from ttt_cur_lss b Where ttt_prm_1d.nzp = b.nzp) ", true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            #region выберем дома для вставки нормы на 1 кв.м для ОДПУ - ttt_cur_doms

            ret = ExecSQL(conn_db, " Drop table ttt_cur_doms ", true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ret = ExecSQL(conn_db,
                " Create Temp table ttt_cur_doms (nzp integer) " + sUnlogTempTable
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ret = ExecSQL(conn_db,
                " Insert into ttt_cur_doms (nzp)" +
                " Select nzp_dom from ddd_pu_gkal where nzp_type = 1 and kod_info in (203,204) Group by 1 "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ExecSQL(conn_db, " create index ix_ttt_cur_doms on ttt_cur_doms (nzp) ", false);
            ExecSQL(conn_db, sUpdStat + " ttt_cur_doms ", false);

            #endregion выберем дома для вставки нормы на 1 кв.м для ОДПУ - ttt_cur_doms

            // чистка нормы на 1 кв.м перед вставкой по домам
            ret = ExecSQL(conn_db, " delete from ttt_prm_2  where nzp_prm=723 and Exists (select 1 from ttt_cur_doms b Where ttt_prm_2.nzp = b.nzp) ", true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }
            ret = ExecSQL(conn_db, " delete from ttt_prm_2d where nzp_prm=723 and Exists (select 1 from ttt_cur_doms b Where ttt_prm_2d.nzp = b.nzp) ", true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            #region выберем ЛС для вставки нормы на 1 кв.м для ГрПУ - ttt_cur_lss

            ret = ExecSQL(conn_db, " Drop table ttt_cur_lss ", true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ret = ExecSQL(conn_db,
                " Create Temp table ttt_cur_lss (nzp integer) " + sUnlogTempTable
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ret = ExecSQL(conn_db,
                " Insert into ttt_cur_lss (nzp)" +
                " Select b.nzp_kvar " +
                " From ddd_pu_gkal a, temp_counters_link b " +
                " Where a.kod_info in (203,204) and a.nzp_type = 2 " +
                  " and a.nzp_counter=b.nzp_counter " +
                " Group by 1 "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ExecSQL(conn_db, " create index ix_ttt_cur_lss on ttt_cur_lss (nzp) ", false);
            ExecSQL(conn_db, sUpdStat + " ttt_cur_lss ", false);

            #endregion выберем ЛС для вставки нормы на 1 кв.м для ГрПУ - ttt_cur_lss

            // чистка нормы на 1 кв.м перед вставкой по ЛС
            ret = ExecSQL(conn_db, " delete from ttt_prm_1  where nzp_prm=2463 and Exists (select 1 from ttt_cur_lss b Where ttt_prm_1.nzp = b.nzp) ", true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }
            ret = ExecSQL(conn_db, " delete from ttt_prm_1d where nzp_prm=2463 and Exists (select 1 from ttt_cur_lss b Where ttt_prm_1d.nzp = b.nzp) ", true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            #endregion удаление результатов расчетов ПУ от ГКал из временных таблиц параметров

            #region удаление временных таблиц

            ret = ExecSQL(conn_db, " Drop table ttt_cur_lss ", true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }
            ret = ExecSQL(conn_db, " Drop table ttt_cur_doms ", true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }
            ret = ExecSQL(conn_db, " Drop table ddd_pu_gkal ", true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            #endregion удаление временных таблиц

            return true;
        }

        #endregion расчет ПУ от ГКал по домам - запись в стек = 39

        #region чистка нормы на 1 куб.м перед вставкой
        bool CutPeriods(IDbConnection conn_db, string palias, string ptbl, string pnzp_prm, string pIncTbl,
            string dtS, string dtPo, out Returns ret)
        //--------------------------------------------------------------------------------
        {
            ret = Utils.InitReturns();

            // [dtS,dtPo] - расчетный месяц & [dat_s,dat_po] - период действия параметра
            // [dat_s,dat_po] внутри [dtS,dtPo]
            ret = ExecSQL(conn_db,
                " Update " + palias + ptbl + " Set is_actual=100 " +
                " Where nzp_prm=" + pnzp_prm + " and is_actual<>100" +
                  " and dat_s>=" + dtS + " and dat_po<=" + dtPo +
                  " and exists (select 1 from " + pIncTbl + " a where a.nzp=" + palias + ptbl + ".nzp) "
                , true);
            if (!ret.result) { return false; }

#if PG
            string sOneDay = " interval '1 day' ";
#else
            string sOneDay = " 1 ";
#endif

            // [dat_s,dat_po] пересекает слева [dtS,dtPo]
            ret = ExecSQL(conn_db,
                " Update " + palias + ptbl + " Set dat_po=" + dtS + " - " + sOneDay +
                " Where nzp_prm=" + pnzp_prm + " and is_actual<>100" +
                  " and dat_s<" + dtS + " and dat_po>=" + dtS + " and dat_po<=" + dtPo +
                  " and exists (select 1 from " + pIncTbl + " a where a.nzp=" + palias + ptbl + ".nzp) "
                , true);
            if (!ret.result) { return false; }

            // [dat_s,dat_po] пересекает справа [dtS,dtPo]
            ret = ExecSQL(conn_db,
                " Update " + palias + ptbl + " Set dat_s=" + dtPo + " + " + sOneDay +
                " Where nzp_prm=" + pnzp_prm + " and is_actual<>100" +
                  " and dat_s>=" + dtS + " and dat_s<=" + dtPo + " and dat_po>" + dtPo +
                  " and exists (select 1 from " + pIncTbl + " a where a.nzp=" + palias + ptbl + ".nzp) "
                , true);
            if (!ret.result) { return false; }

            // [dat_s,dat_po] включает [dtS,dtPo]

            ret = ExecSQL(conn_db, " Drop table ttt_prms_inc ", true);
            if (!ret.result) { return false; }

            ret = ExecSQL(conn_db,
                " Create Temp table ttt_prms_inc (nzp integer,val_prm char(20),dat_po date) " + sUnlogTempTable
                , true);
            if (!ret.result) { return false; }

            ret = ExecSQL(conn_db,
                " Insert into ttt_prms_inc (nzp,val_prm,dat_po)" +
                " Select nzp,val_prm,dat_po" +
                " from " + palias + ptbl +
                " Where nzp_prm=" + pnzp_prm + " and is_actual<>100" +
                  " and dat_s<" + dtS + " and dat_po>" + dtPo +
                  " and exists (select 1 from " + pIncTbl + " a where a.nzp=" + palias + ptbl + ".nzp) "
                , true);
            if (!ret.result) { return false; }

            ExecSQL(conn_db, sUpdStat + " ttt_prms_inc ", false);

            ret = ExecSQL(conn_db,
                " Update " + palias + ptbl + " Set dat_po=" + dtS + " - " + sOneDay +
                " Where nzp_prm=" + pnzp_prm + " and is_actual<>100" +
                  " and dat_s<" + dtS + " and dat_po>" + dtPo +
                  " and exists (select 1 from " + pIncTbl + " a where a.nzp=" + palias + ptbl + ".nzp) "
                , true);
            if (!ret.result) { return false; }

            ret = ExecSQL(conn_db,
                " Insert into " + palias + ptbl + " " +
                " (nzp,nzp_prm,val_prm,dat_s,dat_po,is_actual,nzp_user,dat_when)" + //,month_calc
                " Select nzp," + pnzp_prm + ",val_prm," + dtPo + " + " + sOneDay + ",dat_po,1,1," + sCurDate +
                " from ttt_prms_inc "
                , true);
            if (!ret.result) { return false; }

            return true;
        }
        #endregion чистка нормы на 1 куб.м перед вставкой

        #region Функция расчета расходов по пост 87 и 262 (РТ) НЕ вызывается!
        //--------------------------------------------------------------------------------
        bool Calc87(IDbConnection conn_db, Rashod rashod, Call87 call87, CalcTypes.ParamCalc paramcalc, out Returns ret)
        //--------------------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            //----------------------------------------------------------------
            //Постановление 87 и 262 по дому - stek=6 & nzp_type = 1,3
            //----------------------------------------------------------------
            // cnt1   - кол-во жильцов с КПУ
            // cnt2   - кол-во жильцов без КПУ
            // gil1   - дробное кол-во жильцов с КПУ
            // gil2   - дробное кол-во жильцов без КПУ

            // val1 - нормативщики (Vnn)
            // val2 - расходы КПУ (Vnp)
            // val3 - расход ДПУ (Vd)
            // squ2 - площадь лс без КПУ (Sd)
            // rvirt - вирт. расход (на всякий случай)
            // dop87 - 7 кВт или добавка (87 П)
            // gl7kw - 7 кВт КПУ (учитывая корректировку)
            // vl210- расход 210
            // pu7kw- 7 кВт или откорректированный множитель
            // kf307- коэфициент

            //выбрать дома, удовлетворяющие критерию 87 (есть ДПУ, КПУ и нормативщики )
            ExecSQL(conn_db, " Drop table ttt_aid_stat ", false);

            ret = ExecSQL(conn_db,
            " Select 6 stek,1 nzp_type,dat_charge, 0 nzp_kvar,nzp_dom,1870 nzp_counter, dat_s,dat_po, " + call87.nzp_serv + " nzp_serv, " +

                " sum(case when nzp_type = 3 and nzp_serv = " + call87.nzp_serv + " and cnt_stage = 0 " + " then val1 else 0 end) as val1,  " + //нормативы (Vnn)
                " sum(case when nzp_type = 3 and nzp_serv = " + call87.nzp_serv + " then val2 else 0 end) as val2,  " + //КПУ       (Vnp)
                " sum(case when nzp_type = 1 and nzp_serv = " + call87.nzp_serv + " then val3 else 0 end) as val3,  " + //ДПУ       (Vd)

                " sum(case when nzp_type = 1 and nzp_serv = " + call87.nzp_serv + " and dlt_cur>0 " + " then val3 else 0 end) as dlt_cur, " + //дельта расхода (когда ДПУ > КПУ и нормативы)

                " sum(case when nzp_type = 3 and nzp_serv = " + call87.nzp_serv + " and cnt_stage = 0 " + call87.mmnog0 + " then squ1 else 0 end) as squ2,  " + //площадь лс без КПУ (Sd)
                " sum(case when nzp_type = 3 and nzp_serv = " + call87.nzp_serv + call87.mmnog0 + " then rvirt else 0 end) as rvirt,  " + //вирт. расход

                " sum(case when nzp_type = 3 and nzp_serv = " + call87.nzp_serv + " and cnt_stage = 1 " + call87.mmnog0 + " then gil1 * " + call87.norma + " else 0 end) as gl7kw," + //7кВт c КПУ

                " sum(case when nzp_type = 3 and nzp_serv = " + call87.nzp_serv + " and cnt_stage = 1 " + call87.mmnog0 + " then gil1   else 0 end) as gil1," + //жильцов c КПУ
                " sum(case when nzp_type = 3 and nzp_serv = " + call87.nzp_serv + " and cnt_stage = 0 " + call87.mmnog0 + " then gil1   else 0 end) as gil2," +  //жильцов без КПУ - нормативщики

                //210 услуга вычленить расход, чтобы потом убрать из общего расхода - выберается только когда nzp_serv = 25, иначе 0
                " sum(case when nzp_type = 3 and nzp_serv = 210 then case when cnt_stage = 0 then val1 else val2 end else 0 end ) as vl210  " + //расход 210
#if PG
                " Into temp ttt_aid_stat  " +
#else
                "  "+
#endif
 " From " + rashod.counters_xx + " a " +
            " Where nzp_type in (1,3) and stek = 3 and " + rashod.where_dom + rashod.paramcalc.per_dat_charge +
                call87.and_serv +
            " Group by 1,2,3,4,5,6,7,8,9 " +
#if PG
 "  "
#else
            " Into temp ttt_aid_stat With no log "
#endif
, true);
            if (!ret.result)
            {
                return false;
            }
            ret = ExecSQL(conn_db,
                " Create unique index ix_aid44_st on ttt_aid_stat (nzp_dom,nzp_serv) ", true);
            if (!ret.result)
            {
                ExecSQL(conn_db, " Drop table ttt_aid_stat ", false);
                return false;
            }
            ret = ExecSQL(conn_db,
                " Create        index ix_aid45_st on ttt_aid_stat (nzp_type) ", true);
            if (!ret.result)
            {
                ExecSQL(conn_db, " Drop table ttt_aid_stat ", false);
                return false;
            }

            string sql =
                " Insert into " + rashod.counters_xx +
                " ( stek,nzp_type,dat_charge,nzp_kvar,nzp_dom,nzp_counter,dat_s,dat_po, nzp_serv,  gil2,gil1, val1,val2,val3,squ2,rvirt,gl7kw,vl210,pu7kw ) " +
                " Select 6,1,    dat_charge,0 nzp_kvar,nzp_dom,nzp_counter,dat_s,dat_po,nzp_serv,  gil2,gil1, val1,val2,val3,squ2,rvirt,gl7kw,vl210, " + call87.norma + " " +
                " From ttt_aid_stat " +
                " Where abs(val1)>0.0001 and abs(val2)>0.0001 " + //где есть КПУ и нормативщики
                "   and dlt_cur > 0 "; //есть дельта расхода

            if (rashod.paramcalc.nzp_kvar > 0 || rashod.paramcalc.nzp_dom > 0)
            {
                ret = ExecSQL(conn_db, sql, true);
            }
            else
            {
                ExecByStep(conn_db, "ttt_aid_stat", "nzp_dom", sql, 10000, "", out ret);
            }

            if (!ret.result)
            {
                ExecSQL(conn_db, " Drop table ttt_aid_stat ", false);
                return false;
            }

            UpdateStatistics(false, paramcalc, rashod.counters_tab, out ret);
            //UpdateStatisticsCounters_xx(rashod, out ret);
            if (!ret.result)
            {
                ExecSQL(conn_db, " Drop table ttt_aid_stat ", false);
                return false;
            }

            //текущая дельта
            sql =
                " Update " + rashod.counters_xx +
                " Set dlt_cur = (val3-vl210) - (val2+gl7kw) - val1 " + // ДПУ - 210 - КПУ с учетом 7 кВт - нормативщики
                " Where nzp_type = 1 and stek = 6 and " + rashod.where_dom + rashod.paramcalc.per_dat_charge;

            if (rashod.paramcalc.nzp_kvar > 0 || rashod.paramcalc.nzp_dom > 0)
            {
                ret = ExecSQL(conn_db, sql, true);
            }
            else
            {
                ExecByStep(conn_db, rashod.counters_xx, "nzp_cntx", sql, 100000, "", out ret);
            }
            if (!ret.result)
            {
                ExecSQL(conn_db, " Drop table ttt_aid_stat ", false);
                return false;
            }

            //коэфициент
            sql =
                " Update " + rashod.counters_xx +
                " Set kf307 = dlt_cur / squ2  " + //
                " Where nzp_type = 1 and stek = 6 and " + rashod.where_dom + rashod.paramcalc.per_dat_charge +
                "   and abs(squ2) > 0 ";

            if (rashod.paramcalc.nzp_kvar > 0 || rashod.paramcalc.nzp_dom > 0)
            {
                ret = ExecSQL(conn_db, sql, true);
            }
            else
            {
                ExecByStep(conn_db, rashod.counters_xx, "nzp_cntx", sql, 100000, "", out ret);
            }
            if (!ret.result)
            {
                ExecSQL(conn_db, " Drop table ttt_aid_stat ", false);
                return false;
            }

            //----------------------------------------------------------------
            //если dlt_cur < 0, то надо скорректировать по письму НЧ №  5089 от 14.05.2010
            //----------------------------------------------------------------
            MyDataReader reader;
            ret = ExecRead(conn_db, out reader,
            " Select * From " + rashod.counters_xx + " a " +
            " Where nzp_type = 1 and stek = 6 and " + rashod.where_dom + rashod.paramcalc.per_dat_charge +
                call87.and_serv +
            "   and dlt_cur < 0 "
                , true);
            if (!ret.result)
            {
                ExecSQL(conn_db, " Drop table ttt_aid_stat ", false);
                return false;
            }
            try
            {
                //dlt_cur = (val3-vl210) - (val2+gl7kw) - val1 "+ // ДПУ - 210 - КПУ с учетом 7 кВт - нормативщики(в т.ч. 7 кВт)
                while (reader.Read())
                {
                    decimal k = (decimal)reader["dlt_cur"] + (decimal)reader["gl7kw"];
                    if (k > 0)
                    {
                        //т.е. достаточно уменьшать 7 кВт ДПУ (gl7kw) - письмо НЧ № 7248 от 17.08.2009
                        decimal n = k / (int)reader["gil1"]; //новое значение 7 кВт КПУ

                        ret = ExecSQL(conn_db,
                        " Update " + rashod.counters_xx +
                        " Set gl7kw   = " + k.ToString() +
                            ", pu7kw   = " + n.ToString() +
                            ", dlt_cur = (val3-vl210) - (val2+" + k.ToString() + ") - val1 " + //=0
                            ", nzp_counter = 1871 " + //оставили признак корректировки
                        " Where nzp_cntx  = " + (int)reader["nzp_cntx"]
                            , true);
                        if (!ret.result)
                        {
                            reader.Close();
                            ExecSQL(conn_db, " Drop table ttt_aid_stat ", false);
                            return false;
                        }

                        ret = ExecSQL(conn_db,
                        " Update " + rashod.counters_xx +
                        " Set kf307 = dlt_cur / squ2  " + //коэфициент
                        " Where nzp_cntx  = " + (int)reader["nzp_cntx"]
                            , true);
                        if (!ret.result)
                        {
                            reader.Close();
                            ExecSQL(conn_db, " Drop table ttt_aid_stat ", false);
                            return false;
                        }
                    }
                    else
                    {
                        //сложный случай, надо корректировать еще и нормативщиков и только в пределах 7 кВт
                        //(val3-vl210) - (val2+0) - val1 < 0

                        decimal n = 0;
                        decimal.TryParse(call87.norma, out n);
                        n = n * (int)reader["gil2"]; //предел уменьшения норматива

                        //мы должны уменьшить норматив val1 в пределах 7кВт на жильца, т.е. д.б. отрицательный dop87!
                        string tab;
                        if (k + n > 0)
                            tab = "1872";    //k:=k (n покрывает k)
                        else
                        {
                            k = -n;       //n НЕ покрывает k, вычитаем все 7 кВт
                            tab = "1873";
                        }

                        n = k / (decimal)reader["squ2"]; //новое значение 7 кВт для нормативщиков

                        ret = ExecSQL(conn_db,
                        " Update " + rashod.counters_xx +
                        " Set gl7kw   = 0 " +  //
                            ", pu7kw   = 0 " +
                            ", kf307   = " + n.ToString() +
                            ", val1    =  val1 + (" + k.ToString() + ")" +
                            ", dlt_cur = (val3-vl210) - (val2+0) - (val1 + (" + k.ToString() + ") ) " + //=0
                            ", nzp_counter = " + tab + //оставили признак корректировки
                        " Where nzp_cntx  = " + (int)reader["nzp_cntx"]
                            , true);
                        if (!ret.result)
                        {
                            reader.Close();
                            ExecSQL(conn_db, " Drop table ttt_aid_stat ", false);
                            return false;
                        }
                    }
                }
                reader.Close();
            }
            catch (Exception ex)
            {
                reader.Close();
                ret.result = false;
                ret.text = ex.Message;
                return false;
            }

            //вставить лс
            ret = ExecRead(conn_db, out reader,
                " Select * From " + rashod.counters_xx +
                " Where nzp_type = 1 and stek = 6 and " + rashod.where_dom + rashod.paramcalc.per_dat_charge
                //"   and abs(kf307) > 1 "; //применять только когда коэфициент > 1
                , true);
            if (!ret.result)
            {
                ExecSQL(conn_db, " Drop table ttt_aid_stat ", false);
                return false;
            }
            try
            {
                //решил оставить на стеке = 3, но в других полях (dop87) - добавок к основному расходу (7кВт или проп.норматив (87 П) )
                while (reader.Read())
                {
                    decimal kf307 = (decimal)reader["kf307"];
                    decimal pu7kw = (decimal)reader["pu7kw"];

                    //перезаписать расход 307
                    if (rashod.nzp_type_alg == 8)
                    {
                        sql =
                            " Update " + rashod.counters_xx +
                            " Set rashod = case when cnt_stage = 0" +
                                    " then val1 " + kf307.ToString() + " * squ1 " + //начислить норматив
                                    " else val2 " + pu7kw.ToString() + " * gil1 " + //КПУ
                                    " end " +
                            " Where nzp_type = 3 and stek = 3 and nzp_dom = " + (int)reader["nzp_dom"] + rashod.paramcalc.per_dat_charge +
                            "   and nzp_serv = " + (int)reader["nzp_serv"];

                        if (rashod.paramcalc.nzp_kvar > 0 || rashod.paramcalc.nzp_dom > 0)
                        {
                            ret = ExecSQL(conn_db, sql, true);
                        }
                        else
                        {
                            ExecByStep(conn_db, rashod.counters_xx, "nzp_cntx", sql, 100000, "", out ret);
                        }
                        if (!ret.result)
                        {
                            reader.Close();
                            ExecSQL(conn_db, " Drop table ttt_aid_stat ", false);
                            return false;
                        }
                    }

                    sql =
                          " Update " + rashod.counters_xx +
                          " Set dop87 = case when cnt_stage = 0 " +
                                     " then " + kf307.ToString() + " * squ1 " +
                                     " else " + pu7kw.ToString() + " * gil1 " +
                                     " end " +
                             ",pu7kw = case when cnt_stage = 0 " +  //примененный к-фт 87
                                     " then " + kf307.ToString() +
                                     " else " + pu7kw.ToString() +
                                     " end " +
                          " Where nzp_type = 3 and stek = 3 and nzp_dom = " + (int)reader["nzp_dom"] + rashod.paramcalc.per_dat_charge +
                          "   and nzp_serv = " + (int)reader["nzp_serv"]
                           + call87.mmnog0; //или кроме коммуналок, или по всем

                    if (rashod.paramcalc.nzp_kvar > 0 || rashod.paramcalc.nzp_dom > 0)
                    {
                        ret = ExecSQL(conn_db, sql, true);
                    }
                    else
                    {
                        ExecByStep(conn_db, rashod.counters_xx, "nzp_cntx", sql, 100000, "", out ret);
                    }
                    if (!ret.result)
                    {
                        reader.Close();
                        ExecSQL(conn_db, " Drop table ttt_aid_stat ", false);
                        return false;
                    }

                }
                reader.Close();
            }
            catch (Exception ex)
            {
                reader.Close();
                ret.result = false;
                ret.text = ex.Message;
                return false;
            }


            ExecSQL(conn_db, " Drop table ttt_aid_stat ", false);
            return true;
        }
        #endregion Функция расчета расходов по пост 87 и 262 (РТ) НЕ вызывается!

    }
}

#endregion здесь производится подсчет расходов

