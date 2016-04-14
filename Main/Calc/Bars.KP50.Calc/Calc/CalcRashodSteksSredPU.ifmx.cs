// Подсчет расходов

#region Подключаемые модули

using System;
using System.Data;

using STCLINE.KP50.Global;

#endregion Подключаемые модули

#region здесь производится подсчет расходов

namespace STCLINE.KP50.DataBase
{

    //здась находятся классы для подсчета расходов
    public partial class DbCalcCharge : DataBaseHead
    {
        public bool SetSteksSredPU(IDbConnection conn_db, Rashod rashod, bool b_calc_kvar, string p_dat_charge, out Returns ret)
        //--------------------------------------------------------------------------------
        {
            #region стек 2 - отсутствие показаний в лс в расчетном месяце, но есть ПУ (норма или среднее)

            //----------------------------------------------------------------
            //стек 2 - отсутствие показаний в лс в расчетном месяце, но есть ПУ
            //----------------------------------------------------------------
            ExecSQL(conn_db, " Drop table ttt_ans_kpu ", false);
            ret = ExecSQL(conn_db,
                " Create temp table ttt_ans_kpu " +
                " ( nzp_kvar    integer, " +
                "   nzp_dom     integer, " +
                "   nzp_type    integer default 3," +
                "   nzp_counter integer, " +
                "   date_from   date, "+
                "   date_to     date, " +
                "   sq          " + sDecimalType + "(15,7) default 0.00, " +
                "   sqot        " + sDecimalType + "(15,7) default 0.00, " +
                "   is_gkal     integer default 0," +
                "   nzp_serv    integer, " +
                "   val         " + sDecimalType + "(15,7) " +
                " ) " + sUnlogTempTable
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            // !!! prm_17 нельзя брать через подготовленную временную таблицу для месяца,
            // т.к. средние по ИПУ считаются прямо при учете расходов на всю БД !!!

            //при отсутствии параметра проставляем по умолчанию 3 месяца
            var countMonthsWithAvg = 3;
            // nzp_prm=1448 -	Количество месяцев учета среднего после выхода из строя ПУ
            var calcMonth = new DateTime(rashod.paramcalc.calc_yy, rashod.paramcalc.calc_mm, 1);
          
            var countMonthsWithAvgObj = ExecScalar(conn_db, string.Format("SELECT max(val_prm) FROM {0}{1}prm_10 WHERE nzp_prm=1448 AND is_actual<>100 AND " +
                " '{2}'" + " BETWEEN dat_s and dat_po", rashod.paramcalc.pref, sDataAliasRest,
                calcMonth.ToShortDateString()), out ret, true);
            if (countMonthsWithAvgObj != DBNull.Value && countMonthsWithAvgObj != null)
            {
                countMonthsWithAvg = Convert.ToInt32(countMonthsWithAvgObj);
            }
           

            var dateCloseWithInterval = (rashod.paramcalc.enableAvgOnClosedPU //разрешен учет среднего по закрытым ПУ
                ? sNvlWord + "(t.dat_close," + MDY(1, 1, 3000) + ") + interval '" + countMonthsWithAvg + " month'"//в течении N месяцев после закрытия
                : sNvlWord + "(t.dat_close," + MDY(1, 1, 3000) + ") "); //запрет учета среднего по закрытому ПУ

            ret = ExecSQL(conn_db,
                " Insert into ttt_ans_kpu (nzp_kvar, nzp_dom, nzp_counter, nzp_serv, is_gkal, sq, sqot, val,date_from, date_to) " +
                " Select k.nzp_kvar, k.nzp_dom, t.nzp_counter, t.nzp_serv, " +
                " t.is_gkal, max(k.squ1), max(k.squ2), " +
                " max(replace( " + sNvlWord + "(a.val_prm,'0'), ',', '.')" + sConvToNum + ") as val,  " +
                 rashod.paramcalc.dat_s + ",LEAST(" + rashod.paramcalc.dat_po + ", MAX("+dateCloseWithInterval+")- interval '1 day')"+
                " From " + rashod.paramcalc.data_alias + "prm_17 a, ttt_counters_xx k, temp_cnt_spis t " +
                " Where k.nzp_kvar = t.nzp and t.nzp_type = 3 and k.kod_info = 0 " +
                "   and t.nzp_counter = a.nzp " +
                "   and a.nzp_prm = 979 and a.is_actual=1 " +
                "   and a.dat_s  <= " + rashod.paramcalc.dat_po +
                "   and a.dat_po >= " + rashod.paramcalc.dat_s +
                "   AND " +dateCloseWithInterval+ " >"+ Utils.EStrNull(calcMonth.ToShortDateString()) +
                "   and not exists ( " +
                "     Select 1 From aid_i" + rashod.paramcalc.pref + " n" +
                "     Where a.nzp = n.nzp_counter" +
                "     and (case when a.dat_po>=" + rashod.paramcalc.dat_s + " then " + rashod.paramcalc.dat_po +
                " else a.dat_po end) >= n.dat_s " +
                "     + interval '" + countMonthsWithAvg + " month' " +
                      " and a.dat_s <= n.dat_po" +
                "     ) " +
                " Group by 1,2,3,4,5 "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ViewTbl(conn_db, " select * from ttt_counters_xx order by nzp_counter,nzp_serv ");
            ViewTbl(conn_db, " select * from aid_i" + rashod.paramcalc.pref + " order by nzp_counter,dat_s ");
            ViewTbl(conn_db, " select * from ttt_ans_kpu order by nzp_counter,nzp_serv ");

            if (!b_calc_kvar)
            {
                // ОДПУ
                ret = ExecSQL(conn_db,
                    " Insert into ttt_ans_kpu (nzp_kvar, nzp_dom, nzp_counter, nzp_serv, is_gkal, nzp_type, sq, sqot, val, date_from, date_to) " +
                    " Select 0 nzp_kvar,k.nzp_dom, t.nzp_counter, t.nzp_serv," +
                    " t.is_gkal, t.nzp_type,0 sq,0 sqot, " +
                    " max(replace( " + sNvlWord + "(a.val_prm,'0'), ',', '.')" + sConvToNum + ") as val,  " +
                    rashod.paramcalc.dat_s + "," + rashod.paramcalc.dat_po +
                    " From " + rashod.paramcalc.data_alias + "prm_17 a, t_opn k, temp_cnt_spis t " +
                    " Where k.nzp_dom = t.nzp and t.nzp_type in (1,2) " +
                    "   and t.nzp_counter = a.nzp " +
                    "   and a.nzp_prm = 979 and a.is_actual=1 "  +
                    "   and a.dat_s  <= " + rashod.paramcalc.dat_po +
                    "   and a.dat_po >= " + rashod.paramcalc.dat_s +
                     "   AND " + dateCloseWithInterval + " >" + Utils.EStrNull(calcMonth.ToShortDateString()) +
                    "   and not exists ( " +
                    "     Select 1 From aid_i" + rashod.paramcalc.pref + " n" +
                    "     Where a.nzp = n.nzp_counter " +
                    "     and (case when a.dat_po>=" + rashod.paramcalc.dat_s + " " +
                    "               then " + rashod.paramcalc.dat_po + " " +
                    "               else a.dat_po end) >= n.dat_s  + interval '" + countMonthsWithAvg + " month'" +
                    "     and a.dat_s <= n.dat_po" +
                    "     ) " +
                    " Group by 1,2,3,4,5,6 "
                    , true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }
            }

            ExecSQL(conn_db, " Create index ix_ttt_ans_kpu on ttt_ans_kpu (nzp_serv,nzp_type) ", true);
            ExecSQL(conn_db, sUpdStat + " ttt_ans_kpu ", true);

            // для отопления val1 - на квартиру, val3 - на 1 кв.метр
            ret = ExecSQL(conn_db,
              " Insert into " + rashod.counters_xx +
              " ( stek,nzp_type,dat_charge,nzp_kvar,nzp_dom,nzp_counter,dat_s,dat_po, squ1, squ2, nzp_serv, val1, val3, kod_info ) " +
              " Select " + sUniqueWord +
              " 2,3, " + p_dat_charge + " , nzp_kvar, nzp_dom, nzp_counter,date_from,date_to," +
              " sq, sqot, nzp_serv, val, case when sqot > 0  then val / sqot else 0 end, is_gkal " +
              " From ttt_ans_kpu where nzp_serv = 8 and nzp_type = 3 "
              , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            // остальные услуги
            ret = ExecSQL(conn_db,
              " Insert into " + rashod.counters_xx +
              " ( stek,nzp_type,dat_charge,nzp_kvar,nzp_dom,nzp_counter,dat_s,dat_po, squ1, squ2, nzp_serv, val1, kod_info ) " +
              " Select " + sUniqueWord +
              " 2,3, " + p_dat_charge + " , nzp_kvar, nzp_dom, nzp_counter,date_from,date_to," +
              " sq, sqot, nzp_serv, val, is_gkal " +
              " From ttt_ans_kpu where nzp_serv <> 8 and nzp_type = 3 "
              , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            // для ОДПУ
            ret = ExecSQL(conn_db,
              " Insert into " + rashod.counters_xx +
              " ( stek,nzp_type,dat_charge,nzp_kvar,nzp_dom,nzp_counter,dat_s,dat_po, squ1, squ2, nzp_serv, val1, kod_info ) " +
              " Select " + sUniqueWord +
              " 2,nzp_type, " + p_dat_charge + " , nzp_kvar, nzp_dom, nzp_counter," + rashod.paramcalc.dat_s + "," + rashod.paramcalc.dat_po + "," +
              " sq, sqot, nzp_serv, val, is_gkal " +
              " From ttt_ans_kpu where nzp_type in (1,2) "
              , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            #endregion стек 2 - отсутствие показаний в лс в расчетном месяце, но есть ПУ (норма или среднее)

            UpdateStatistics(false, rashod.paramcalc, rashod.counters_tab, out ret);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            return true;
        }

        /// <summary>
        /// Получить объемы расходов по услугам из параметров
        /// </summary>
        /// <param name="conn_db"></param>
        /// <param name="rashod"></param>
        /// <param name="p_dat_charge"></param>
        /// <param name="ret"></param>
        /// <returns></returns>
        private bool SetSteksSredLs(IDbConnection conn_db, Rashod rashod, string p_dat_charge, out Returns ret)
        {
            #region стек 4 - средние значения: выбрать расходы

            //----------------------------------------------------------------
            //стек 4 - средние значения: выбрать расходы
            //----------------------------------------------------------------
            ExecSQL(conn_db, " Drop table ttt_aid_norma ", false);
            ret = ExecSQL(conn_db,
                " Create temp table ttt_aid_norma " +
                " ( no       serial, " +
                "   nzp_kvar integer, " +
                "   nzp_dom  integer, " +
                "   nzp_serv integer, " +
                "   val1     " + sDecimalType + "(12,4)," +
                "   sq       " + sDecimalType + "(12,4)," +
                "   sqot     " + sDecimalType + "(12,4), " +
                "   dat_s     DATE, " +
                "   dat_po     DATE " +
                " ) " + sUnlogTempTable
                , true);
            if (!ret.result)
            {
                DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                return false;
            }

            /*
            25 22	Среднее значение счетчика электроэнергии	        1		float	0	1
            6  135	Среднее значение счетчика хол.воды	                1		float	0	1
            9  136	Среднее значение счетчика гор.воды	                1		float	0	1
            10 137	Среднее значение счетчика газа	                    0		float	0	1
            8  138	Среднее значение счетчика отопления	                1		float	0	1
            7  139	Среднее значение счетчика канализации	            1		float	0	1
            210 380	Среднее значение счетчика электроэнергии	        1		float	0	1
            200 412	Среднее значение счетчика полив         	        1		float	0	1
            706 253	Изменение счетчика питьевой воды                    0    	float	0	1

            228	Среднее значение домового счетчика по отоплению	        1	    float	0	2
            373	Среднее значение домового счетчика по хол.воде	        1	    float	0	2
            374	Среднее значение домового счетчика по гор.воде	        1	    float	0	2
            375	Среднее значение домового счетчика по канализации	    1	    float	0	2
            376	Среднее значение домового счетчика по электроэнергии	1       float	0	2
            */
            // s_counts.nzp_prm_sred - Среднее счетчика на ЛС
            ret = ExecSQL(conn_db,
                " Insert into ttt_aid_norma (nzp_kvar,nzp_dom,nzp_serv,dat_s,dat_po,sq,sqot, val1) " +
                " Select c.nzp_kvar,c.nzp_dom, c.nzp_serv,t.dp, t.dp_end, max(c.squ1), max(c.squ2), max(p.val_prm" + sConvToNum + ") as val1 " +
                " From ttt_counters_xx c, ttt_prm_1d p, t_gku_periods t," + rashod.paramcalc.kernel_alias + "s_counts s " +
                " Where p.nzp    = c.nzp_kvar and p.nzp=t.nzp_kvar " +
                  " and p.nzp_prm=s.nzp_prm_sred and c.nzp_serv=s.nzp_serv " +
                " and (p.dat_s,p.dat_po) OVERLAPS (t.dp, t.dp_end) " +
                " Group by 1,2,3,4,5 "
                 , true);
            if (!ret.result)
            {
                DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                return false;
            }

            ExecSQL(conn_db, " Create unique index ix_aid00_n on ttt_aid_norma (no) ", true);
            ExecSQL(conn_db, " Create        index ix_aid11_n on ttt_aid_norma (nzp_kvar) ", true);
            ExecSQL(conn_db, sUpdStat + " ttt_aid_norma ", true);

            string sql =
                    " Insert into " + rashod.counters_xx +
                    " ( stek,nzp_type,dat_charge,nzp_kvar,nzp_dom,nzp_counter, squ1, squ2,val1,val3,dat_s,dat_po,nzp_serv ) " +
                    " Select 4,3, " + p_dat_charge + " ,nzp_kvar,nzp_dom, 0, sq, sqot," +
                    " case when nzp_serv=8 then val1 * sqot else val1 end, case when nzp_serv=8 then val1 else 0 end," +
                    " dat_s,dat_po, nzp_serv " +
                    " From ttt_aid_norma Where 1=1 ";

            if (rashod.paramcalc.nzp_kvar > 0 || rashod.paramcalc.nzp_dom > 0)
            {
                ret = ExecSQL(conn_db, sql, true);
            }
            else
            {
                ExecByStep(conn_db, "ttt_aid_norma", "no", sql, 100000, " ", out ret);
            }
            if (!ret.result)
            {
                DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                return false;
            }

            #endregion стек 4 - средние значения: выбрать расходы

            #region стек 5 - изменения счетчика: выбрать расходы

            //----------------------------------------------------------------
            //стек 5 - изменения счетчика: выбрать расходы
            //----------------------------------------------------------------
            ExecSQL(conn_db, " Drop table ttt_aid_norma ", false);

            ret = ExecSQL(conn_db,
                " Create temp table ttt_aid_norma " +
                " ( no       serial, " +
                "   nzp_kvar integer, " +
                "   nzp_dom  integer, " +
                "   nzp_serv integer, " +
                "   val1     " + sDecimalType + "(12,4)," +
                "   sq       " + sDecimalType + "(12,4)," +
                "   sqot     " + sDecimalType + "(12,4) " +
                " ) " + sUnlogTempTable
                , true);
            if (!ret.result)
            {
                DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                return false;
            }
            /*
            25 60	Изменение счетчика эл/эн	                    0    	float	0	1	0	1000000	7
            6  112	Изменение счетчика хол.воды	                    0    	float	0	1	0	1000000	7
            9  113	Изменение счетчика гор.воды	                    0    	float	0	1	0	1000000	7
            10 114	Изменение счетчика газа	                        0    	float	0	1	0	1000000	7
            8  115	Изменение счетчика отопления	                0    	float	0	1	0	1000000	7
            7  116	Изменение счетчика канализации	                0    	float	0	1	0	1000000	7

            210 381	Изменение счетчика ночное эл/эн	                0    	float	0	1	0	1000000	7
                389	Изменение счетчика по Эл/Эн лифтов (квт/час)	1    	float	0	1	0	1000000	7
            200 413	Изменение счетчика полив	                    0    	float	0	1	0	1000000	7
                442	Изменение счетчика бани	                        0    	float	0	1	0	1000000	7
                473	Изменение счетчика ХП эл/эн	                    0    	float	0	1	0	1000000	7
            253 707	Изменение счетчика питьевой воды                0    	float	0	1	0	1000000	7

               229	Изменение домового счетчика по отоплению	    1    	float	0	2	0	1000000	7
            */
            // s_counts.nzp_prm_val - изменение счетчика на ЛС
            ret = ExecSQL(conn_db,
                " Insert into ttt_aid_norma (nzp_kvar,nzp_dom,nzp_serv,sq,sqot, val1) " +
                " Select c.nzp_kvar,c.nzp_dom, c.nzp_serv, max(c.squ1), max(c.squ2), max(p.val_prm" + sConvToNum + ") as val1 " +
                " From ttt_counters_xx c, ttt_prm_1 p," + rashod.paramcalc.kernel_alias + "s_counts s " +
                " Where p.nzp     = c.nzp_kvar " +
                  " and p.nzp_prm = s.nzp_prm_val and c.nzp_serv = s.nzp_serv " +
                " Group by 1,2,3 "
                 , true);
            if (!ret.result)
            {
                DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                return false;
            }

            ExecSQL(conn_db, " Create unique index ix_aid00_n on ttt_aid_norma (no) ", true);
            ExecSQL(conn_db, " Create        index ix_aid11_n on ttt_aid_norma (nzp_kvar) ", true);
            ExecSQL(conn_db, sUpdStat + " ttt_aid_norma ", true);

            sql =
                    " Insert into " + rashod.counters_xx +
                    " ( stek,nzp_type,dat_charge,nzp_kvar,nzp_dom,nzp_counter, squ1, squ2,val1,val3,dat_s,dat_po,nzp_serv ) " +
                    " Select 5,3, " + p_dat_charge + " ,nzp_kvar,nzp_dom, 0, sq, sqot," +
                    " case when nzp_serv=8 then val1 * sqot else val1 end, case when nzp_serv=8 then val1 else 0 end," +
                    rashod.paramcalc.dat_s + "," + rashod.paramcalc.dat_po + ", nzp_serv " +
                    " From ttt_aid_norma Where 1=1 ";

            if (rashod.paramcalc.nzp_kvar > 0 || rashod.paramcalc.nzp_dom > 0)
            {
                ret = ExecSQL(conn_db, sql, true);
            }
            else
            {
                ExecByStep(conn_db, "ttt_aid_norma", "no", sql, 100000, " ", out ret);
            }
            if (!ret.result)
            {
                DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                return false;
            }

            #endregion стек 5 - изменения счетчика: выбрать расходы

            return true;
        }

        #region проставить средние и изменения в нормативы - stek = 3 и nzp_type = 3
        public bool SetSredPUInStek3(IDbConnection conn_db, Rashod rashod, out Returns ret)
        //--------------------------------------------------------------------------------
        {
            //----------------------------------------------------------------
            // проставить средние и изменения в нормативы - stek = 3 и nzp_type = 3
            //----------------------------------------------------------------
            ExecSQL(conn_db, " Drop table ttt_aid_sred ", false);

            ret = ExecSQL(conn_db,
                  " Create temp table ttt_aid_sred " +
                  " ( no       serial,  " +
                  "   nzp_kvar integer, " +
                  "   nzp_serv integer, " +
                  "   val1     " + sDecimalType + "(12,4)," +
                  "   val3     " + sDecimalType + "(12,4), " +
                  "   dat_s     DATE, " +
                  "   dat_po    DATE " +
                  " ) " + sUnlogTempTable
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ret = ExecSQL(conn_db,
                " Insert into ttt_aid_sred (nzp_kvar,nzp_serv,dat_s,dat_po,val1,val3) " +
                " Select nzp_kvar, nzp_serv,dat_s,dat_po, max(val1), max(val3) " +
                " From " + rashod.counters_xx +
                " Where nzp_type = 3 and stek=5 and " + rashod.where_dom + rashod.where_kvar + rashod.paramcalc.per_dat_charge +
                " group by 1,2,3,4 "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ExecSQL(conn_db, " Create unique index ix_aid00_sred on ttt_aid_sred (no) ", true);
            ExecSQL(conn_db, " Create        index ix_aid11_sred on ttt_aid_sred (nzp_kvar) ", true);
            ExecSQL(conn_db, " Create        index ix_aid12_sred on ttt_aid_sred (nzp_kvar,nzp_serv,dat_s,dat_po) ", true);
            ExecSQL(conn_db, sUpdStat + " ttt_aid_sred ", true);

            ExecSQL(conn_db, " Drop table ttt_aid_c1xx ", false);
            ret = ExecSQL(conn_db,
                " create temp table ttt_aid_c1xx (nzp_kvar integer, nzp_serv integer) " + sUnlogTempTable
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ret = ExecSQL(conn_db,
                " Insert into ttt_aid_c1xx (nzp_kvar, nzp_serv)" +
                " Select nzp_kvar, nzp_serv From ttt_aid_sred group by 1,2"
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ExecSQL(conn_db, " Create unique index ix_ttt_aid_c1xx on ttt_aid_c1xx (nzp_kvar, nzp_serv) ", true);
            ExecSQL(conn_db, sUpdStat + " ttt_aid_c1xx ", true);

            ret = ExecSQL(conn_db,
                " Insert into ttt_aid_sred (nzp_kvar,nzp_serv,dat_s,dat_po,val1,val3) " +
                " Select a.nzp_kvar, a.nzp_serv,a.dat_s,a.dat_po, max(a.val1), max(a.val3) " +
                " From " + rashod.counters_xx + " a,  t_selkvar t  " +
                " Where a.nzp_type = 3 and a.stek=4  "+ rashod.paramcalc.per_dat_charge +
                " and not exists (select 1 from ttt_aid_c1xx b where a.nzp_kvar=b.nzp_kvar and a.nzp_serv=b.nzp_serv)" +
                " and t.nzp_kvar=a.nzp_kvar" +
                " group by 1,2,3,4 "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ExecSQL(conn_db, sUpdStat + " ttt_aid_sred ", true);

            ExecSQL(conn_db, " Drop table ttt_aid_c1xx ", false);
            ret = ExecSQL(conn_db,
                " create temp table ttt_aid_c1xx (nzp_kvar integer, nzp_serv integer) " + sUnlogTempTable
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            // определяем дату, до которой значение среднего по ИПУ более приоритетное, чем расход по ИПУ (для картиночных месяцев)
            DateTime forcedAverageIPUValueEndDate;
            var forcedAverageIPUValue = false;
            var forcedAverageIPUValueDateString = ExecScalar(conn_db, string.Format(@"
SELECT MAX(val_prm)
FROM {0}prm_10
WHERE nzp_prm = 2200 AND is_actual <> 100 AND dat_s <= {1} AND dat_po >= {2}", rashod.paramcalc.data_alias,
                DBManager.sCurDate, DBManager.sCurDate), out ret, true) as string;
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            if (forcedAverageIPUValueDateString != null && DateTime.TryParse(forcedAverageIPUValueDateString, out forcedAverageIPUValueEndDate))
            {
                if (forcedAverageIPUValueEndDate > new DateTime(rashod.paramcalc.calc_yy, rashod.paramcalc.calc_mm, 1))
                {
                    forcedAverageIPUValue = true;
                }
            }



            // выполнять установку средних по ЛС, только для нормативных ЛС/услуг
            ret = ExecSQL(conn_db,
                " Insert into ttt_aid_c1xx (nzp_kvar, nzp_serv) " +
                " Select a.nzp_kvar, a.nzp_serv From ttt_counters_xx a, ttt_aid_sred b " +
                " Where a.nzp_kvar=b.nzp_kvar and a.nzp_serv=b.nzp_serv and a.stek = 3 "+(forcedAverageIPUValue ? string.Empty : " and a.cnt_stage = 0 ") +
                " Group by 1,2 "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ExecSQL(conn_db, " Create unique index ix_ttt_aid_c1xx on ttt_aid_c1xx (nzp_kvar, nzp_serv) ", true);
            ExecSQL(conn_db, sUpdStat + " ttt_aid_c1xx ", true);

            ret = ExecSQL(conn_db,
                 " Update ttt_counters_xx t " +
                 " Set" +
                 " kod_info=(case when t.nzp_serv=210 then 210 else t.kod_info end)," +
                 " val1 =  a.val1 * (t.cntd * 1.00 / t.cntd_mn), " +
                 " val1_source =  a.val1 * (t.cntd * 1.00 / t.cntd_mn) " +
                // чистим расход по ИПУ при более приоритетном среднем
                // для канализации ставим для нормативщиков признак расхода по среднему ИПУ (чтобы отключить корректировку расхода канализации далее)
                 (forcedAverageIPUValue
                     ? " , val2 = 0, cnt_stage = CASE WHEN t.cnt_stage = 0 AND t.nzp_serv = 7 THEN 9 ELSE t.cnt_stage END"
                     : string.Empty) +
                 " , val1_g = a.val1 * (t.cntd * 1.00 / t.cntd_mn) " +
                 " , val4 = least ( a.val1 * (t.cntd * 1.00 / t.cntd_mn), val4) " +
                 ", nzp_counter=-9 " + //расчет по среднему на ЛС
                 " From ttt_aid_sred a, ttt_aid_c1xx c " +
                 " Where t.nzp_kvar = a.nzp_kvar " +
                 "  and t.nzp_serv = a.nzp_serv " +
                // по стекам 3 и 10!
                 "  and a.nzp_kvar = c.nzp_kvar " +
                 "  and a.nzp_serv = c.nzp_serv" +
                 "  and a.dat_s=t.dat_s " +
                 "  and a.dat_po= t.dat_po "

                 , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            // для отопления среднее и изменеие ИПУ - val1 = расход на квартиру & val3 = на 1 кв.метр!
            ret = ExecSQL(conn_db,
                " Update ttt_counters_xx t " +
                " Set val3 =  a.val3 * (cntd * 1.00 / cntd_mn), nzp_counter=-9  "+
                   " From ttt_aid_sred a, ttt_aid_c1xx c " +
                 " Where t.nzp_kvar = a.nzp_kvar " +
                 "  and t.nzp_serv = a.nzp_serv " +
                // по стекам 3 и 10!
                 "  and a.nzp_kvar = c.nzp_kvar " +
                 "  and a.nzp_serv = c.nzp_serv" +
                 "  and a.dat_s=t.dat_s " +
                 "  and a.dat_po= t.dat_po "+
                "   and t.nzp_serv = 8 "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ExecSQL(conn_db, " Drop table ttt_aid_c1xx ", false);
            return true;
        }
        #endregion проставить средние и изменения в нормативы - stek = 3 и nzp_type = 3

    }

}

#endregion здесь производится подсчет расходов

