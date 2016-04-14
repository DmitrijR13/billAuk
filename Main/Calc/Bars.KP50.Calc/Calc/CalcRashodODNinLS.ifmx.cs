// Подсчет расходов

#region Подключаемые модули

using System;
using System.Data;

using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;

#endregion Подключаемые модули

#region здесь производится подсчет расходов

namespace STCLINE.KP50.DataBase
{

    //здась находятся классы для подсчета расходов
    public partial class DbCalcCharge : DataBaseHead
    {

        public bool CalcRashodSetODNInStek3forLS(IDbConnection conn_db, Rashod rashod, bool b_calc_kvar, string p_dat_charge, out Returns ret)
        //--------------------------------------------------------------------------------
        {
            ret = Utils.InitReturns();

            // установка способа расчета ОДН по ЛС, если он был рассчитан - запись в стек = 3 & nzp_type = 3
            Stek3ODNToLs(conn_db, rashod, b_calc_kvar, p_dat_charge, out ret);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            // учет по ЛС в стек 3 установленного способа расчета ОДН по ЛС - запись в стек = 3 & nzp_type = 3
            SetRashodODNInStek3forLS(conn_db, rashod, out ret);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            // учет стека 29 - доли расхода коммуналки в расходе квартиры по Пост 354
            SetRashodODKommunal(conn_db, rashod, out ret);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            return true;
        }

        #region установка способа расчета ОДН по ЛС, если он был рассчитан - запись в стек = 3 & nzp_type = 3

        // выбрать способ расчета ОДН для дома
        public bool Stek3ODNToLsLoadKoefsForDom(IDbConnection conn_db, Rashod rashod, out Returns ret)
        //--------------------------------------------------------------------------------
        {
            ret = Utils.InitReturns();

            ret = ExecSQL(conn_db,
                " insert into ttt_aid_stat (nzp_dom, nzp_serv, kf307, kf307n, kod_info,val4_source,squ_sum,gl7kw ) " +
                " Select nzp_dom, nzp_serv, max(kf307) kf307, max(kf307n) kf307n, max(kod_info) kod_info, max(val4_source) val4_source," +
                " max(squ1) squ_sum, max(gl7kw) gl7kw " +
                " From " + rashod.counters_xx +
                " Where nzp_type = 1 and stek = 3 and kod_info>0 and " + rashod.where_dom + rashod.paramcalc.per_dat_charge +
                " group by 1,2 "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ExecSQL(conn_db, sUpdStat + " ttt_aid_stat ", false);

            ExecSQL(conn_db, " Drop table ttt_aid_statx ", false);
            ret = ExecSQL(conn_db,
                " Create temp table ttt_aid_statx " +
                " ( nzp_dom integer, " +
                "   nzp_serv integer " +
                " )  " + sUnlogTempTable
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ret = ExecSQL(conn_db,
                " Insert into ttt_aid_statx (nzp_dom, nzp_serv) " +
                " Select nzp_dom, nzp_serv From ttt_aid_stat "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ExecSQL(conn_db, " Create index ix_ttt_aid_statx on ttt_aid_statx (nzp_dom,nzp_serv) ", false);
            ExecSQL(conn_db, sUpdStat + " ttt_aid_statx ", false);

            // если не было домовых расходов по дому в текущем расчетном месяце ,
            // но есть коэф-т коррекции для программы 2.0, то использовать его
            ret = ExecSQL(conn_db,
                " insert into ttt_aid_stat (nzp_dom, nzp_serv, kf307, kf307n, kod_info, is_not_calc,gl7kw) " +
                " Select nzp_dom, nzp_serv, max(rval) kf307, max(rval_real) kf307n, max(nzp_type_alg) kod_info, 1,max(rval) gl7kw " +
                " From " + rashod.paramcalc.data_alias + "counters_correct f" +
                " Where f.nzp_counter = 0 and " + rashod.where_dom +
                " and f.dat_month = " + rashod.paramcalc.dat_s +
                " and f.dat_charge = " +
                " (Select max(b.dat_charge)" +
                 " From " + rashod.paramcalc.data_alias + "counters_correct b" +
                 " Where f.nzp_dom=b.nzp_dom and f.nzp_serv=b.nzp_serv and b.dat_month=" + rashod.paramcalc.dat_s + ")" +
                " and 0 = ( Select count(*) From ttt_aid_statx a " +
                              " Where f.nzp_dom  = a.nzp_dom   " +
                              "   and f.nzp_serv = a.nzp_serv )" +
                " group by 1,2 "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ExecSQL(conn_db, sUpdStat + " ttt_aid_stat ", false);
            ExecSQL(conn_db, " Drop table ttt_aid_statx ", false);

            ExecSQL(conn_db, " Drop table ttt_aid_kpu ", false);
            ret = ExecSQL(conn_db,
                " Create temp table ttt_aid_kpu " +
                " ( nzp_dom integer, " +
                "   nzp_serv integer, " +
                "   kf307       " + sDecimalType + "(15,7) default 0.00 , " +
                "   kf307n      " + sDecimalType + "(15,7) default 0.00 , " +
                "   gl7kw       " + sDecimalType + "(15,7) default 0.00 , " +  //коэфициент 354 для ОДН по нормативу
                "   kod_info    integer default 0, " +
                "   is_not_calc integer default 0  " +
                " )  " + sUnlogTempTable
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ret = ExecSQL(conn_db,
                " insert into ttt_aid_kpu (nzp_dom, nzp_serv, kf307, kf307n, kod_info, is_not_calc, gl7kw) " +
                " Select nzp_dom,14 nzp_serv, kf307, kf307n, kod_info, 1, gl7kw " +
                " From ttt_aid_stat " +
                " Where is_not_calc=1 and nzp_serv=9 "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ret = ExecSQL(conn_db,
                " insert into ttt_aid_kpu (nzp_dom, nzp_serv, kf307, kf307n, kod_info, is_not_calc, gl7kw) " +
                " Select nzp_dom,210 nzp_serv, kf307, kf307n, kod_info, 1, gl7kw " +
                " From ttt_aid_stat " +
                " Where is_not_calc=1 and nzp_serv=25 and kod_info in (5,6,7,16,21,31) "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ret = ExecSQL(conn_db,
                " insert into ttt_aid_kpu (nzp_dom, nzp_serv, kf307, kf307n, kod_info, is_not_calc, gl7kw) " +
                " Select nzp_dom,410 nzp_serv, kf307, kf307n, kod_info, 1, gl7kw " +
                " From ttt_aid_stat " +
                " Where is_not_calc=1 and nzp_serv=25 and kod_info in (5,6,7,16,21,31) "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ExecSQL(conn_db, " Create unique index ix_aid33_kpu on ttt_aid_kpu (nzp_dom,nzp_serv) ", true);
            ExecSQL(conn_db, sUpdStat + " ttt_aid_kpu ", true);

            ret = ExecSQL(conn_db,
                " insert into ttt_aid_stat (nzp_dom, nzp_serv, kf307, kf307n, kod_info, is_not_calc, gl7kw) " +
                " Select nzp_dom, nzp_serv, kf307, kf307n, kod_info, is_not_calc, gl7kw From ttt_aid_kpu Where 1=1 "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ret = ExecSQL(conn_db,
                " Update " + rashod.counters_xx +
                " Set kf307  = a.kf307," +
                    " kf307n = a.kf307n," +
                    " gl7kw  = a.gl7kw, " +
                    " kod_info = a.kod_info " +
                " From ttt_aid_stat a " +
                " Where " + rashod.counters_xx + ".nzp_type = 1 and " + rashod.counters_xx + ".stek = 3 and " + rashod.counters_xx + "." + rashod.where_dom +
                "   and " + rashod.counters_xx + ".nzp_dom  = a.nzp_dom  " +
                "   and " + rashod.counters_xx + ".nzp_serv = a.nzp_serv " +
                "   and a.is_not_calc = 1 and a.nzp_counter = 0 "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ExecSQL(conn_db, " Drop table ttt_aid_kpu ", false);

            return true;
        }

        // выбрать способ расчета ОДН для ГрПУ
        public bool Stek3ODNToLsLoadKoefsForGrPU(IDbConnection conn_db, Rashod rashod, string p_dat_charge, out Returns ret)
        //--------------------------------------------------------------------------------
        {
            ret = Utils.InitReturns();

            ret = ExecSQL(conn_db,
                " insert into ttt_aid_stat (nzp_dom, nzp_serv, nzp_counter, kf307, kf307n, kod_info, val4_source,squ_sum, gl7kw) " +
                " Select nzp_dom, nzp_serv, nzp_counter, max(kf307) kf307, max(kf307n) kf307n, max(kod_info) kod_info," +
                " max(val4_source) val4_source, max(squ1) squ_sum, max(gl7kw) gl7kw " +
                " From " + rashod.counters_xx +
                " Where nzp_type = 2 and stek = 3 and kod_info>0 and " + rashod.where_dom + rashod.paramcalc.per_dat_charge +
                " group by 1,2,3 "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ExecSQL(conn_db, sUpdStat + " ttt_aid_stat ", false);

            ExecSQL(conn_db, " Drop table ttt_aid_statx ", false);
            ret = ExecSQL(conn_db,
                " Create temp table ttt_aid_statx " +
                " ( nzp_dom integer, " +
                "   nzp_serv integer " +
                " )  " + sUnlogTempTable
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ret = ExecSQL(conn_db,
                " Insert into ttt_aid_statx (nzp_dom, nzp_serv) " +
                " Select nzp_dom, nzp_serv From ttt_aid_stat group by 1,2 "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ExecSQL(conn_db, " Create index ix_ttt_aid_statx on ttt_aid_statx (nzp_dom,nzp_serv) ", false);
            ExecSQL(conn_db, sUpdStat + " ttt_aid_statx ", false);

            // если не было домовых расходов по дому в текущем расчетном месяце ,
            // но есть коэф-т коррекции для программы 2.0, то использовать его
            ret = ExecSQL(conn_db,
              " insert into ttt_aid_stat (nzp_dom, nzp_serv, nzp_counter, kf307, kf307n, kod_info, is_not_calc, gl7kw) " +
              " Select nzp_dom, nzp_serv, nzp_counter, max(rval) kf307, max(rval_real) kf307n, max(nzp_type_alg) kod_info, 1, max(rval) kf307 " +
              " From " + rashod.paramcalc.data_alias + "counters_correct f" +
              " Where f.nzp_counter > 0 " +
              " and 0<(select count(*) from t_selkvar k, temp_counters_link l" +
                     " where k.nzp_kvar=l.nzp_kvar and f.nzp_counter=l.nzp_counter and k." + rashod.where_dom + ")" +
              " and f.dat_month = " + rashod.paramcalc.dat_s +
              " and f.dat_charge = " +
              " (Select max(b.dat_charge)" +
              "  From " + rashod.paramcalc.data_alias + "counters_correct b" +
              "  Where f.nzp_counter=b.nzp_counter and b.dat_month=" + rashod.paramcalc.dat_s + ")" +
              " and 0 = ( Select count(*) From ttt_aid_statx a " +
                            " Where f.nzp_dom  = a.nzp_dom   " +
                            "   and f.nzp_serv = a.nzp_serv )" +
              " group by 1,2,3 "
            , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ExecSQL(conn_db, sUpdStat + " ttt_aid_stat ", false);
            ExecSQL(conn_db, " Drop table ttt_aid_statx ", false);

            ExecSQL(conn_db, " Drop table ttt_aid_kpu ", false);
            ret = ExecSQL(conn_db,
                " Create temp table ttt_aid_kpu " +
                " ( nzp_dom integer, " +
                "   nzp_serv integer, " +
                "   nzp_counter integer, " +
                "   kf307       " + sDecimalType + "(15,7) default 0.00 , " +
                "   kf307n      " + sDecimalType + "(15,7) default 0.00 , " +
                "   gl7kw       " + sDecimalType + "(15,7) default 0.00 , " +  //коэфициент 354 для ОДН по нормативу
                "   kod_info    integer default 0, " +
                "   is_not_calc integer default 0  " +
                " )  " + sUnlogTempTable
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ret = ExecSQL(conn_db,
              " insert into ttt_aid_kpu (nzp_dom, nzp_serv, nzp_counter, kf307, kf307n, kod_info, is_not_calc, gl7kw) " +
              " Select nzp_dom,14 nzp_serv, nzp_counter, kf307, kf307n, kod_info, 1, gl7kw " +
              " From ttt_aid_stat " +
              " Where nzp_counter > 0 and is_not_calc=1 and nzp_serv=9 "
            , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ret = ExecSQL(conn_db,
              " insert into ttt_aid_kpu (nzp_dom, nzp_serv, nzp_counter, kf307, kf307n, kod_info, is_not_calc, gl7kw) " +
              " Select nzp_dom,210 nzp_serv, nzp_counter, kf307, kf307n, kod_info, 1, gl7kw " +
              " From ttt_aid_stat " +
              " Where nzp_counter > 0 and is_not_calc=1 and nzp_serv=25 and kod_info in (5,6,7,16,21,31) "
            , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ret = ExecSQL(conn_db,
              " insert into ttt_aid_kpu (nzp_dom, nzp_serv, nzp_counter, kf307, kf307n, kod_info, is_not_calc, gl7kw) " +
              " Select nzp_dom,410 nzp_serv, nzp_counter, kf307, kf307n, kod_info, 1, gl7kw " +
              " From ttt_aid_stat " +
              " Where nzp_counter > 0 and is_not_calc=1 and nzp_serv=25 and kod_info in (5,6,7,16,21,31) "
            , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ExecSQL(conn_db, " Create index ix_aid33_kpu on ttt_aid_kpu (nzp_dom,nzp_serv,nzp_counter) ", true);
            ExecSQL(conn_db, sUpdStat + " ttt_aid_kpu ", true);

            ret = ExecSQL(conn_db,
              " insert into ttt_aid_stat (nzp_dom, nzp_serv, nzp_counter, kf307, kf307n, kod_info, is_not_calc, gl7kw) " +
              " Select nzp_dom, nzp_serv, nzp_counter, kf307, kf307n, kod_info, is_not_calc, gl7kw From ttt_aid_kpu Where 1=1 "
            , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }


            ret = ExecSQL(conn_db,
                " Insert into " + rashod.counters_xx +
                " ( stek,nzp_type,dat_charge,nzp_kvar,nzp_dom,nzp_serv,nzp_counter,dat_s,dat_po, kf307,kf307n,kod_info,cur_zap,gl7kw ) " +
                " Select 3,2, " + p_dat_charge + ",0 nzp_kvar, nzp_dom, nzp_serv, nzp_counter, " +
                rashod.paramcalc.dat_s + "," + rashod.paramcalc.dat_po + ", kf307, kf307n, kod_info,-100, gl7kw " +
                " From ttt_aid_stat" +
                " Where nzp_counter > 0 and is_not_calc=1 "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ExecSQL(conn_db, " Drop table ttt_aid_kpu ", false);

            return true;
        }

        // выборка кеоэффициентов коррекции в ttt_aid_stat
        public bool Stek3ODNToLsLoadKoefs(IDbConnection conn_db, Rashod rashod, bool b_calc_kvar, string p_dat_charge, out Returns ret)
        //--------------------------------------------------------------------------------
        {
            ret = Utils.InitReturns();

            ExecSQL(conn_db, " Drop table ttt_aid_stat ", false);

            ret = ExecSQL(conn_db,
                " Create temp table ttt_aid_stat " +
                " ( nzp_dom integer, " +
                "   nzp_serv integer, " +
                "   nzp_counter integer default 0, " +
                "   kf307       " + sDecimalType + "(15,7) default 0.00 , " +  //коэфициент 307 для КПУ или коэфициент 87 для нормативщиков
                "   kf307n      " + sDecimalType + "(15,7) default 0.00 , " +  //коэфициент 307 для нормативщиков
                "   gl7kw       " + sDecimalType + "(15,7) default 0.00 , " +  //коэфициент 354 для ОДН по нормативу
                "   kod_info    integer default 0, " +            //выбранный способ учета (=1)
                "   is_not_calc integer default 0,  " +            //если рассчитан по ДПУ =0 / иначе "левый" коэф. корр. =1
                "   val4_source      " + sDecimalType + "(15,7) default 0.00,  " +  //норматив на дом без учета повышающего коэф-та
                "   squ_sum      " + sDecimalType + "(15,7) default 0.00  " +  //сумма площадей по всем лс дома
                " )  " + sUnlogTempTable
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ExecSQL(conn_db, " Create index ix1_aid44_st on ttt_aid_stat (nzp_dom,nzp_serv,nzp_counter) ", false);
            ExecSQL(conn_db, " Create index ix2_aid44_st on ttt_aid_stat (is_not_calc,nzp_counter) ", false);
            ExecSQL(conn_db, " Create index ix3_aid44_st on ttt_aid_stat (nzp_counter) ", false);

            bool b_cur_cur = rashod.paramcalc.b_cur;

            //if (!b_calc_kvar || rashod.paramcalc.b_cur)
            if (!b_calc_kvar || b_cur_cur)
            {
                //
                // если расчет/перерасчет по дому или всей базе и в текущем расчетном месяце - всегда есть расчет расходов по дому!

                // выбрать способы расчета ОДН для дома
                Stek3ODNToLsLoadKoefsForDom(conn_db, rashod, out ret);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                // выбрать способы расчета ОДН для ГрПУ
                Stek3ODNToLsLoadKoefsForGrPU(conn_db, rashod, p_dat_charge, out ret);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                //
            }
            else
            {
                //
                // если перерасчет по 1му л/с, то нужно поискать домовые расходы в прошлом
                //
                //позиционируемся на список расчетных месяцев данного nzp_wp
                int ipos = -1;
                for (int i = 0; i < Points.PointList.Count; i++)
                {
                    if (Points.PointList[i].nzp_wp == rashod.paramcalc.nzp_wp)
                    {
                        ipos = i;
                        break;
                    }
                }
                // если nzp_wp найден
                if (ipos >= 0)
                {

                    string cur_counters_alias = "";
                    string cur_counters_tab = "";
                    string cur_counters_xx = "";
                    int icountvals = 0;
                    // по найденному текущему nzp_wp с учетом отсортированности по убыванию месяцев (из будущего в прошлое до текущего месяца перерасчета)
                    for (int j = 0; j < Points.PointList[ipos].CalcMonths.Count; j++)
                    {
                        //проверим, есть ли данный месяц среди перерасчитываемых данных
                        int yy = Points.PointList[ipos].CalcMonths[j].year_;
                        int mm = Points.PointList[ipos].CalcMonths[j].month_;

                        if (yy == 0 || mm == 0) continue;

                        //отсечем перерасчеты в прошлом до текущего месяца перерасчета
                        if (yy < rashod.paramcalc.calc_yy) continue;
                        if (yy == rashod.paramcalc.calc_yy && mm < rashod.paramcalc.calc_mm) continue;

                        //
                        if (yy == rashod.paramcalc.calc_yy && mm == rashod.paramcalc.calc_mm)
                        {
                            cur_counters_alias = "";
                        }
                        else
                        {
                            cur_counters_alias = (yy - 2000).ToString("00") + mm.ToString("00");
                        }
                        cur_counters_tab = "counters" + cur_counters_alias + "_" +
                                           rashod.paramcalc.calc_mm.ToString("00");
                        cur_counters_xx = rashod.paramcalc.pref + "_charge_" +
                                          (rashod.paramcalc.calc_yy - 2000).ToString("00") +
                                          tableDelimiter + cur_counters_tab;

                        // во-первых, поищем ОДН по ГрПУ


                        IDbCommand cmd = DBManager.newDbCommand(
                            " Select count(*) From " + cur_counters_xx + " a, temp_counters_link l" +
                            " Where a.nzp_counter = l.nzp_counter" +
                            " and a.nzp_type = 2 and a.stek = 3 and " + rashod.where_dom +
                            " and l.nzp_kvar = " + rashod.paramcalc.nzp_kvar
                            , conn_db);
                        try
                        {
                            string scountvals = Convert.ToString(cmd.ExecuteScalar());
                            icountvals = Convert.ToInt32(scountvals);
                        }
                        catch
                        {
                            icountvals = 0;
                        }
                        string sType = "1";
                        if (icountvals > 0)
                        {
                            sType = "2";
                        }
                        else
                        {
                            // во-вторых, поищем ОДН по ОДПУ
                            cmd = DBManager.newDbCommand(
                                " Select count(*) From " + cur_counters_xx + " Where nzp_type = 1 and stek = 3 and " +
                                rashod.where_dom
                                , conn_db);
                            try
                            {
                                string scountvals = Convert.ToString(cmd.ExecuteScalar());
                                icountvals = Convert.ToInt32(scountvals);
                            }
                            catch
                            {
                                icountvals = 0;
                            }
                        }

                        if (icountvals > 0)
                        {
                            //
                            // найден месяц последнего рассчитанного расхода по дому
                            //
                            ret = ExecSQL(conn_db,
                              " insert into ttt_aid_stat (nzp_dom, nzp_serv, kf307, kf307n, kod_info,val4_source,squ_sum, gl7kw) " +
                              " Select nzp_dom, nzp_serv, max(kf307) kf307, max(kf307n) kf307n, max(kod_info) kod_info," +
                                    " max(val4_source) val4_source,max(squ1) squ_sum, max(gl7kw) " +
                              " From " + cur_counters_xx +
                              " Where nzp_type = " + sType + " and stek = 3 and kod_info>0 and " + rashod.where_dom +
                              " group by 1,2 "
                            , true);
                            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                            break;
                        }
                        //
                    }
                }
                //    
            }

            ExecSQL(conn_db, sUpdStat + " ttt_aid_stat ", false);

            return true;
        }

        // установка кеоэффициентов коррекции по ЛС из ttt_aid_stat
        public bool Stek3ODNToLsSetVals(IDbConnection conn_db, Rashod rashod, out Returns ret)
        //--------------------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            string sql;

            // установка кеоэффициентов коррекции по ЛС из ttt_aid_stat по ОДПУ в вторую очередь

            sql =
                    " Update " + rashod.counters_xx +
                    " Set kf307  = a.kf307," +
                        " kf307n = a.kf307n," +
                        " kf_dpu_plob = a.gl7kw," +
                        " kod_info = a.kod_info," +
                        " dop87_source = squ1 * (case when a.squ_sum = 0 then 0 else a.val4_source/a.squ_sum end)  " +
                      " From ttt_aid_stat a " +
                      " Where " + rashod.counters_xx + ".nzp_type = 3 and " + rashod.counters_xx + ".stek = 3 and " + rashod.counters_xx + "." + rashod.where_dom +
                      "   and " + rashod.counters_xx + ".nzp_dom  = a.nzp_dom  " +
                      "   and " + rashod.counters_xx + ".nzp_serv = a.nzp_serv " +
                      "   and a.nzp_counter = 0 ";

            if (rashod.paramcalc.nzp_kvar > 0 || rashod.paramcalc.nzp_dom > 0)
            {
                ret = ExecSQL(conn_db, sql, true);
            }
            else
            {
                ExecByStep(conn_db, rashod.counters_xx, "nzp_cntx", sql, 100000, " ", out ret);
            }
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            // установка кеоэффициентов коррекции по ЛС из ttt_aid_stat по ГрПУ в первую очередь

            ExecSQL(conn_db, " Drop table ttt_aid_kpu ", false);
            ret = ExecSQL(conn_db,
                " Create temp table ttt_aid_kpu " +
                " ( nzp_kvar integer, " +
                "   nzp_serv integer, " +
                "   nzp_counter integer " +
                " )  " + sUnlogTempTable
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ret = ExecSQL(conn_db,
                " insert into ttt_aid_kpu (nzp_kvar, nzp_serv, nzp_counter) " +
                " Select l.nzp_kvar, a.nzp_serv, max(a.nzp_counter) " +
                " From ttt_aid_stat a,temp_counters_link l " +
                " Where a.nzp_counter = l.nzp_counter " +
                "   and a.nzp_counter > 0 " +
                " group by 1,2 "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ExecSQL(conn_db, " Create index ix_aid33_kpu on ttt_aid_kpu (nzp_kvar,nzp_serv,nzp_counter) ", true);
            ExecSQL(conn_db, sUpdStat + " ttt_aid_kpu ", true);

            sql =
                    " Update " + rashod.counters_xx +
                    " Set kf307  = a.kf307," +
                        " kf307n = a.kf307n," +
                        " kf_dpu_plob = a.gl7kw," +
                        " kod_info = a.kod_info " +
                    " From ttt_aid_stat a,ttt_aid_kpu l " +
                    " Where " + rashod.counters_xx + ".nzp_type = 3 and " + rashod.counters_xx + ".stek = 3 and " + rashod.counters_xx + "." + rashod.where_dom + 
                    "   and " + rashod.counters_xx + ".nzp_kvar = l.nzp_kvar  " +
                    "   and " + rashod.counters_xx + ".nzp_serv = l.nzp_serv " +
                    "   and a.nzp_counter = l.nzp_counter ";

            if (rashod.paramcalc.nzp_kvar > 0 || rashod.paramcalc.nzp_dom > 0)
            {
                ret = ExecSQL(conn_db, sql, true);
            }
            else
            {
                ExecByStep(conn_db, rashod.counters_xx, "nzp_cntx", sql, 100000, " ", out ret);
            }
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ExecSQL(conn_db, " Drop table ttt_aid_kpu ", false);

            return true;
        }

        // установка способа расчета ОДН по ЛС
        public bool Stek3ODNToLs(IDbConnection conn_db, Rashod rashod, bool b_calc_kvar, string p_dat_charge, out Returns ret)
        //--------------------------------------------------------------------------------
        {
            ret = Utils.InitReturns();

            // выборка кеоэффициентов коррекции в ttt_aid_stat
            Stek3ODNToLsLoadKoefs(conn_db, rashod, b_calc_kvar, p_dat_charge, out ret);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            // установка кеоэффициентов коррекции по ЛС из ttt_aid_stat
            Stek3ODNToLsSetVals(conn_db, rashod, out ret);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            return true;
        }
        #endregion установка способа расчета ОДН по ЛС, если он был рассчитан - запись в стек = 3 & nzp_type = 3

        #region учет по ЛС в стек 3 установленного способа расчета ОДН по ЛС - запись в стек = 3 & nzp_type = 3
        public bool SetRashodODNInStek3forLS(IDbConnection conn_db, Rashod rashod, out Returns ret)
        {
            ret = Utils.InitReturns();

            #region Одним ударом семь постновлений убил: вывести расход по л/с
            // вывести расход по л/с
            // odn
            string sql =
                " Update " + rashod.counters_xx +
                // доп. расход есть в val2 для нормы и в val1 для ИПУ!
                " Set rashod = (case when cnt_stage = 0 then val2 else val1 end) + " +
                //case when cnt_stage = 0 then val1 * kf307n " + //начислить норматив
                //" else val2 * kf307 " + // расход КПУ
                //" end, nzp_counter = 109 " +

                " case when cnt_stage = 0" +
                " then " + //начислить норматив

                    " case when nzp_serv in (210,410) then " +

                    " (case when kod_info= 5 then val1 " +
                    "       when kod_info= 6 then val1 * kf307 " +
                    "       when kod_info= 7 then val1 " +
                    "       when kod_info= 8 then val1 " +
                    "       when kod_info=10 then val1 " +
                    "       when kod_info=14 then val1 * kf307n " +
                    "       when kod_info=15 then val1 " +
                    "       when kod_info=16 then val1 * kf307n " +

                    "       when kod_info=21 then val1 " +
                    "       when kod_info=22 then val1 + squ1 * kf307n " +
                    "       when kod_info=31 then val1 " +
                    "       when kod_info=32 then val1 + gil1 * kf307n " +

                    "       else val1 end)" +

                    " else " +

                    "(case when kod_info= 5 then val1 " +
                    "      when kod_info= 6 then val1 * kf307 " +
                    "      when kod_info= 7 then val1 + squ1 * kf307n " +
                    "      when kod_info= 8 then val1 + squ1 * kf307n " +
                    "      when kod_info=10 then val1 + squ1 * kf307n " +
                    "      when kod_info=14 then val1 * kf307 " +
                    "      when kod_info=15 then val1 + squ1 * kf307n " +
                    "      when kod_info=16 then val1 * kf307 " +

                    "      when kod_info=21 then val1 + squ1 * kf307n " +
                    "      when kod_info=22 then val1 + squ1 * kf307n " +
                    "      when kod_info=23 then val1 + squ1 * kf307n " +
                    "      when kod_info=24 then val1 " +
                    "      when kod_info=25 then val1 " +
                    "      when kod_info=26 then val1 + squ1 * kf307n " +
                    "      when kod_info=27 then val1 + squ1 * kf307n " +
                    "      when kod_info=31 then val1 + gil1 * kf307n " +
                    "      when kod_info=32 then val1 + gil1 * kf307n " +
                    "      when kod_info=33 then val1 + squ1 * kf307n " +

                    "      else val1 end)" +

                    "end " +

                " else " +  //расход КПУ

                    " case when nzp_serv in (210,410) then " +

                    "(case when kod_info= 5 then val2 * kf307 " +
                    "      when kod_info= 6 then val2 * kf307 " +
                    "      when kod_info= 7 then val2 * kf307 " +
                    "      when kod_info= 8 then val2 " +
                    "      when kod_info=10 then val2 " +
                    "      when kod_info=14 then val2 * kf307 " +
                    "      when kod_info=15 then val2 " +
                    "      when kod_info=16 then val2 * kf307 " +

                    "      when kod_info=21 then val2 " +
                    "      when kod_info=22 then val2 + squ1 * kf307 " +
                    "      when kod_info=31 then val2 " +
                    "      when kod_info=32 then val2 + gil1 * kf307 " +

                    "      else val2 end)" +

                    " else " +

                    "(case when kod_info= 5 then val2 * kf307 " +
                    "      when kod_info= 6 then val2 * kf307 " +
                    "      when kod_info= 7 then val2 * kf307 " +
                    "      when kod_info= 8 then val2 + gil1 * kf307 " +
                    "      when kod_info=10 then val2 * kf307 " +
                    "      when kod_info=14 then val2 * kf307 " +
                    "      when kod_info=15 then val2 + gil1 * kf307 " +
                    "      when kod_info=16 then val2 * kf307 " +

                    "      when kod_info=21 then val2 + squ1 * kf307 " +
                    "      when kod_info=22 then val2 + squ1 * kf307" +
                    "      when kod_info=23 then val2 + squ1 * kf307 " +
                    "      when kod_info=24 then val2 " +
                    "      when kod_info=25 then val2 " +
                    "      when kod_info=26 then val2 + squ1 * kf307 " +
                    "      when kod_info=27 then val2 + squ1 * kf307 " +
                    "      when kod_info=31 then val2 + gil1 * kf307 " +
                    "      when kod_info=32 then val2 + gil1 * kf307 " +
                    "      when kod_info=33 then val2 + squ1 * kf307 " +

                    "      else val2 end)" +

                    "end " +

                "end, nzp_counter = 0, " +
                //
                " dop87 = " +

                " case when cnt_stage = 0" +
                " then " + //начислить норматив

                    " case when nzp_serv in (210,410) then " +

                    " (case when  kod_info= 5 then 0 " +
                    "       when  kod_info= 6 then val1 * kf307 - val1 " +
                    "       when  kod_info= 7 then 0 " +
                    "       when  kod_info= 8 then 0 " +
                    "       when  kod_info=10 then 0 " +
                    "       when  kod_info=14 then val1 * kf307n - val1 " +
                    "       when  kod_info=15 then 0 " +
                    "       when  kod_info=16 then val1 * kf307n - val1 " +

                    "       when kod_info=21 then 0 " +
                    "       when kod_info=22 then squ1 * kf307n " +
                    "       when kod_info=31 then 0 " +
                    "       when kod_info=32 then gil1 * kf307n " +

                    "       else 0 end)" +

                    " else " +

                    "(case when kod_info= 5 then 0 " +
                    "      when kod_info= 6 then val1 * kf307 - val1 " +
                    "      when kod_info= 7 then squ1 * kf307n " +
                    "      when kod_info= 8 then squ1 * kf307n " +
                    "      when kod_info=10 then squ1 * kf307n " +
                    "      when kod_info=14 then val1 * kf307 - val1 " +
                    "      when kod_info=15 then squ1 * kf307n " +
                    "      when kod_info=16 then val1 * kf307 - val1 " +

                    "      when kod_info=21 then squ1 * kf307n " +
                    "      when kod_info=22 then squ1 * kf307n " +
                    "      when kod_info=23 then squ1 * kf307n " +
                    "      when kod_info=24 then 0 " +
                    "      when kod_info=25 then 0 " +
                    "      when kod_info=26 then squ1 * kf307n " +
                    "      when kod_info=31 then gil1 * kf307n " +
                    "      when kod_info=32 then gil1 * kf307n " +
                    "      when kod_info=33 then gil1 * kf307n " +

                    "      else 0 end)" +

                    "end " +

                " else " +  //расход КПУ

                    " case when nzp_serv in (210,410) then " +

                    "(case when kod_info= 5 then val2 * kf307  - val2 " +
                    "      when kod_info= 6 then val2 * kf307  - val2 " +
                    "      when kod_info= 7 then val2 * kf307  - val2 " +
                    "      when kod_info= 8 then 0 " +
                    "      when kod_info=10 then 0 " +
                    "      when kod_info=14 then val2 * kf307  - val2 " +
                    "      when kod_info=15 then 0 " +
                    "      when kod_info=16 then val2 * kf307  - val2 " +

                    "      when kod_info=21 then 0 " +
                    "      when kod_info=22 then squ1 * kf307 " +
                    "      when kod_info=31 then 0 " +
                    "      when kod_info=32 then gil1 * kf307 " +

                    "      else 0 end)" +

                    " else " +

                    "(case when  kod_info= 5 then val2 * kf307  - val2 " +
                    "      when  kod_info= 6 then val2 * kf307  - val2 " +
                    "      when  kod_info= 7 then val2 * kf307  - val2 " +
                    "      when  kod_info= 8 then gil1 * kf307 " +
                    "      when  kod_info=10 then val2 * kf307  - val2 " +
                    "      when  kod_info=14 then val2 * kf307  - val2 " +
                    "      when  kod_info=15 then gil1 * kf307 " +
                    "      when  kod_info=16 then val2 * kf307  - val2 " +

                    "      when kod_info=21 then squ1 * kf307 " +
                    "      when kod_info=22 then squ1 * kf307" +
                    "      when kod_info=23 then squ1 * kf307 " +
                    "      when kod_info=24 then 0 " +
                    "      when kod_info=25 then 0 " +
                    "      when kod_info=26 then squ1 * kf307 " +
                    "      when kod_info=31 then gil1 * kf307 " +
                    "      when kod_info=32 then gil1 * kf307 " +
                    "      when kod_info=33 then gil1 * kf307 " +

                    "      else 0 end)" +

                    "end " +

                "end " +
                //                
                " Where nzp_type = 3 and stek = 3 and kod_info>0 and kod_info<100 and " + rashod.where_dom + rashod.where_kvar + rashod.paramcalc.per_dat_charge +
                " and nzp_serv <> 8 ";

            if (rashod.paramcalc.nzp_kvar > 0 || rashod.paramcalc.nzp_dom > 0)
            {
                ret = ExecSQL(conn_db, sql, true);
            }
            else
            {
                ExecByStep(conn_db, rashod.counters_xx, "nzp_cntx", sql, 100000, " ", out ret);
            }
            if (!ret.result)
            {
                DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                return false;
            }

            #endregion Одним ударом семь постновлений убил: вывести расход по л/с

            #region ОДН по ДПУ - kod_info=101
            // dpu
            sql =
                    " Update " + rashod.counters_xx +
                    " Set rashod = case when cnt_stage = 0 then gil1 * kf307n " + //начислить норматив
                                 " else gil1 * kf307 " + // расход КПУ
                                 " end, nzp_counter = 0 " +
                    "   , val1_g = case when cnt_stage = 0 then gil1_g * kf307n " + // расход без учета вр.выбывших!
                                 " else gil1_g * kf307 " +
                                 " end " +
                    " Where nzp_type = 3 and stek = 3 and kod_info=101 and " + rashod.where_dom + rashod.where_kvar + rashod.paramcalc.per_dat_charge +
                    " and nzp_serv <> 8 ";

            if (rashod.paramcalc.nzp_kvar > 0 || rashod.paramcalc.nzp_dom > 0)
            {
                ret = ExecSQL(conn_db, sql, true);
            }
            else
            {
                ExecByStep(conn_db, rashod.counters_xx, "nzp_cntx", sql, 100000, " ", out ret);
            }
            if (!ret.result)
            {
                DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                return false;
            }

            #endregion ОДН по ДПУ - kod_info=101

            #region ОДН по ДПУ - kod_info=102. Учесть ОДН отопление , пока 1 случай когда учитыается отопление!
            // пока 1 случай когда учитыается отопление!
            sql =
                    " Update " + rashod.counters_xx +
                    " Set rashod = case when cnt_stage = 0 then (case when nzp_serv=8 then squ2 else squ1 end) * kf307n " + //начислить норматив
                                 " else (case when nzp_serv=8 then squ2 else squ1 end) * kf307 " + // расход КПУ
                                 " end, nzp_counter = 0 " +
                    "   , val1_g = case when cnt_stage = 0 then (case when nzp_serv=8 then squ2 else squ1 end) * kf307n " +
                                 " else (case when nzp_serv=8 then squ2 else squ1 end) * kf307 " + // расход без учета вр.выбывших совпадает с расходом по услугЕ!
                                 " end " +
                    " Where nzp_type = 3 and stek = 3 and kod_info=102 and " + rashod.where_dom + rashod.where_kvar + rashod.paramcalc.per_dat_charge;

            if (rashod.paramcalc.nzp_kvar > 0 || rashod.paramcalc.nzp_dom > 0)
            {
                ret = ExecSQL(conn_db, sql, true);
            }
            else
            {
                ExecByStep(conn_db, rashod.counters_xx, "nzp_cntx", sql, 100000, " ", out ret);
            }
            if (!ret.result)
            {
                DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                return false;
            }

            #endregion ОДН по ДПУ - kod_info=102. Учесть ОДН отопление , пока 1 случай когда учитыается отопление!

            #region ОДН по ДПУ - kod_info=104
            sql =
                    " Update " + rashod.counters_xx +
                    " Set rashod = case when cnt_stage = 0 then 1 * kf307n " + //начислить норматив
                                 " else 1 * kf307 " + // расход КПУ
                                 " end, nzp_counter = 0 " +
                    "   , val1_g = case when cnt_stage = 0 then 1 * kf307n " +
                                 " else 1 * kf307 " + // расход без учета вр.выбывших совпадает с расходом по услугЕ!
                                 " end " +
                    " Where nzp_type = 3 and stek = 3 and kod_info=104 and " + rashod.where_dom + rashod.where_kvar + rashod.paramcalc.per_dat_charge +
                    " and nzp_serv <> 8 ";

            if (rashod.paramcalc.nzp_kvar > 0 || rashod.paramcalc.nzp_dom > 0)
            {
                ret = ExecSQL(conn_db, sql, true);
            }
            else
            {
                ExecByStep(conn_db, rashod.counters_xx, "nzp_cntx", sql, 100000, " ", out ret);
            }
            if (!ret.result)
            {
                DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                return false;
            }

            #endregion ОДН по ДПУ - kod_info=104

            #region Расчет расходов по ЛС в стек 3 для установленного способа расчета ОДН
            //для пропорции по количеству жильцов
            sql = "    Update " + rashod.counters_xx +
                    " set rashod = kf307n * gil1 " +
                    " where stek=3 and nzp_type = 3 and cnt_stage not in (1,9) and kod_info = 201 and " +
                    rashod.where_dom + rashod.where_kvar + rashod.paramcalc.per_dat_charge;
            if (rashod.paramcalc.nzp_kvar > 0 || rashod.paramcalc.nzp_dom > 0)
            {
                ret = ExecSQL(conn_db, sql, true);
            }
            else
            {
                ExecByStep(conn_db, rashod.counters_xx, "nzp_cntx", sql, 100000, " ", out ret);
            }
            if (!ret.result)
            {
                DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                return false;
            }
            //для пропорции по общей площади 
            sql = "    Update " + rashod.counters_xx +
                  " set rashod = kf307n * squ1 " +
                  " where stek=3 and nzp_type = 3 and cnt_stage not in (1,9) and kod_info = 202 and " +
                  rashod.where_dom + rashod.where_kvar + rashod.paramcalc.per_dat_charge;
            if (rashod.paramcalc.nzp_kvar > 0 || rashod.paramcalc.nzp_dom > 0)
            {
                ret = ExecSQL(conn_db, sql, true);
            }
            else
            {
                ExecByStep(conn_db, rashod.counters_xx, "nzp_cntx", sql, 100000, " ", out ret);
            }
            if (!ret.result)
            {
                DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                return false;
            }
            //для пропорции по отапливаемой площади 
            sql = "    Update " + rashod.counters_xx +
                  " set rashod = kf307n * squ2  " +
                  " where stek=3 and nzp_type = 3 and cnt_stage not in (1,9) and kod_info = 203 and " +
                  rashod.where_dom + rashod.where_kvar + rashod.paramcalc.per_dat_charge;
            if (rashod.paramcalc.nzp_kvar > 0 || rashod.paramcalc.nzp_dom > 0)
            {
                ret = ExecSQL(conn_db, sql, true);
            }
            else
            {
                ExecByStep(conn_db, rashod.counters_xx, "nzp_cntx", sql, 100000, " ", out ret);
            }
            if (!ret.result)
            {
                DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                return false;
            }
            //для пропорции по количеству лицевых счетов 
            sql = "    Update " + rashod.counters_xx +
                  " set rashod = kf307n" +
                  " where stek=3 and nzp_type = 3 and cnt_stage not in (1,9) and kod_info = 204 and " +
                  rashod.where_dom + rashod.where_kvar + rashod.paramcalc.per_dat_charge;
            if (rashod.paramcalc.nzp_kvar > 0 || rashod.paramcalc.nzp_dom > 0)
            {
                ret = ExecSQL(conn_db, sql, true);
            }
            else
            {
                ExecByStep(conn_db, rashod.counters_xx, "nzp_cntx", sql, 100000, " ", out ret);
            }
            if (!ret.result)
            {
                DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                return false;
            }

            sql = "    Update " + rashod.counters_xx +
                  " set val1_g = (case when kod_info <> 204 then rashod else kf307n * gil1_g end)" +
                  " where stek=3 and nzp_type = 3 and cnt_stage not in (1,9) and kod_info in (201,202,203,204) and " +
                  rashod.where_dom + rashod.where_kvar + rashod.paramcalc.per_dat_charge;
            if (rashod.paramcalc.nzp_kvar > 0 || rashod.paramcalc.nzp_dom > 0)
            {
                ret = ExecSQL(conn_db, sql, true);
            }
            else
            {
                ExecByStep(conn_db, rashod.counters_xx, "nzp_cntx", sql, 100000, " ", out ret);
            }
            if (!ret.result)
            {
                DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                return false;
            }
            #endregion

            return true;
        }
        #endregion учет по ЛС в стек 3 установленного способа расчета ОДН по ЛС - запись в стек = 3 & nzp_type = 3

        #region учет стека 29 - доли расхода коммуналки в расходе квартиры по Пост 354
        public bool SetRashodODKommunal(IDbConnection conn_db, Rashod rashod, out Returns ret)
        {
            ret = Utils.InitReturns();
            //----------------------------------------------------------------
            // учет стека 29 - доли расхода коммуналки в расходе квартиры по Пост 354
            //----------------------------------------------------------------

            // по квартирам, где есть коммуналки kf = squ2/val2 по жилым площадям
            // учтем долю коммуналки если был расчет по Пост 354
            ExecSQL(conn_db, " Drop table t_ans_itog ", false);
            ret = ExecSQL(conn_db,
                " Create temp table t_ans_itog (" +
                " nzp_cntx integer not null," +
                " nzp_dom integer,  " +
                " nzp_serv integer, " +
                " kf307  " + sDecimalType + "(14,7) default 0.0000000, " +
                " kf354  " + sDecimalType + "(14,7) default 0.0000000, " +
                " squ1   " + sDecimalType + "(14,7) default 0.0000000, " +
                " squ2   " + sDecimalType + "(14,7) default 0.0000000, " +
                " rnorm  " + sDecimalType + "(14,7) default 0.0000000, " +
                " rpu    " + sDecimalType + "(14,7) default 0.0000000, " +
                " up_kf  " + sDecimalType + "(15,7) default 1.0000000  " +
                " )  " + sUnlogTempTable
                , true);
            if (!ret.result)
            {
                DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                return false;
            }

            ret = ExecSQL(conn_db,
                " Insert into t_ans_itog (nzp_dom, nzp_serv, kf307,squ1,squ2, kf354,rnorm,rpu,nzp_cntx)" +
                " Select c.nzp_dom, c.nzp_serv, k.kf307,k.val1,k.squ2,c.kf307 kf354,c.val1 rnorm,c.val2 rpu,c.nzp_cntx " +
                " From t_ans_kommunal k, " + rashod.counters_xx + " c " +
                " where k.nzp_kvar=c.nzp_kvar and c.nzp_type = 3 and c.stek = 3 and c.kod_info in (21,22,23,26,27) "
                , true);
            if (!ret.result)
            {
                DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                return false;
            }
            ExecSQL(conn_db, " Create unique index ix_t_ans_itog on t_ans_itog (nzp_cntx) ", true);
            ExecSQL(conn_db, sUpdStat + " t_ans_itog ", true);

            ret = ExecSQL(conn_db,
                " UPDATE t_ans_itog a "+ 
                " SET up_kf = CASE WHEN c.val4_source>0 THEN "+
                                  " CASE WHEN (c.kf_dpu_ls/c.val4_source)>1 THEN c.kf_dpu_ls/c.val4_source ELSE 1 END " +
                            " ELSE 1 END " +
                " FROM " + rashod.counters_xx + " c " +
                " WHERE c.nzp_dom=a.nzp_dom and a.nzp_serv=c.nzp_serv and c.nzp_type = 1 and c.stek = 3 and c.kod_info in (21,22,23,26,27) "
                , true);
            if (!ret.result)
            {
                DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                return false;
            }

            string sqlq = "a.squ1 * a.kf307";
            if (Points.IsSmr)
            {
                // для самары приведенную площадь для ОДН для коммуналок округлить до 2х знаков
                sqlq = "Round( a.squ1 * a.kf307 * 100 )/100";
            }

            var sql = " UPDATE " + rashod.counters_xx + " c " +
                        " SET  rashod =rnorm + rpu + (" + sqlq + " * kf354) ," +
                        " dop87 = " + sqlq + " * a.kf354," +
                        " dop87_source= (" + sqlq + " * a.kf354) / a.up_kf, " +
                        " squ2=a.squ2, " +
                        " kf_dpu_ls = a.kf307," +
                        " squ1 = a.squ1  " +
                        " FROM t_ans_itog a " +
                        " WHERE c.nzp_cntx = a.nzp_cntx";
            if (rashod.paramcalc.nzp_kvar > 0 || rashod.paramcalc.nzp_dom > 0)
            {
                ret = ExecSQL(conn_db, sql, true);
            }
            else
            {
                ExecByStep(conn_db, rashod.counters_xx, "nzp_cntx", sql, 100000, " ", out ret);
            }
            if (!ret.result)
            {
                DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                return false;
            }

            return true;
        }
        #endregion учет стека 29 - доли расхода коммуналки в расходе квартиры по Пост 354

        #region учет ОДН по ЛС в стек 20 из стека 3 - запись в стек = 20 & nzp_type = 3
        public bool CalcRashodSetODNInStek20forLS(IDbConnection conn_db, Rashod rashod, out Returns ret)
        {
            ret = Utils.InitReturns();

            ExecSQL(conn_db, " Drop table ttt_aid_kpud ", false);
            ret = ExecSQL(conn_db,
                " Create temp table ttt_aid_kpud " +
                " ( nzp_kvar  integer, " +
                "   nzp_serv  integer, " +
                "   cntd_sum  integer  " +
                " )  " + sUnlogTempTable
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            // собрать все ЛС, по которым есть ИПУ по дням
            ret = ExecSQL(conn_db,
                  " Insert into ttt_aid_kpud (nzp_kvar, nzp_serv, cntd_sum) " +
                  " Select nzp_kvar, nzp_serv, sum(cntd) " +
                  " From ttt_counters_xx " +
                  " Where stek=20 " +
                  " Group by 1,2 "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ExecSQL(conn_db, " Create unique index ix_ttt_aid_kpud on ttt_aid_kpud (nzp_kvar,nzp_serv) ", true);
            ExecSQL(conn_db, sUpdStat + " ttt_aid_kpud ", true);

            ViewTbl(conn_db, " select * from ttt_aid_kpud order by nzp_kvar,nzp_serv ");

            ExecSQL(conn_db, " Drop table ttt_counters_stek3 ", false);
            // создадим пустышку... структура совпадает с counters_xx!

            ret = ExecSQL(conn_db,
                " Create Temp table ttt_counters_stek3" +
                  " (  nzp_kvar    integer       default 0 not null , " +
                  "    nzp_serv    integer       not null, " +
                  "    rashod      " + sDecimalType + "(15,7) default 0.00 not null, " +  //общий расход в зависимости от stek
                  "    dop87       " + sDecimalType + "(15,7) default 0.00         , " +  //доп.значение 87 постановления (7кВт или добавок к нормативу  (87 П) )
                  "    dop87_source       " + sDecimalType + "(15,7) default 0.00         , " +  //доп.значение 87 постановления (7кВт или добавок к нормативу  (87 П) )
                  "    kf307       " + sDecimalType + "(15,7) default 0.00         , " +  //коэфициент 307 для КПУ или коэфициент 87 для нормативщиков
                  "    kf307n      " + sDecimalType + "(15,7) default 0.00         , " +  //коэфициент 307 для нормативщиков
                  "    kf_dpu_plob " + sDecimalType + "(15,7) default 0.00         , " +  //коэфициент 354 для нормы на ОДН
                  "    kod_info    integer default 0," +
                  "    nzp_period  integer not null, " +
                  "    dp          date not null, " +
                  "    dp_end      date not null, " +
                  "    cntd integer," +
                  "    cntd_sum integer," +
                  "    cntd_mn integer " +
                  " ) " + sUnlogTempTable
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ret = ExecSQL(conn_db,
                " insert into ttt_counters_stek3 (nzp_kvar,nzp_serv,rashod,dop87,dop87_source,kf307,kf307n,kf_dpu_plob,kod_info, " +
                " nzp_period,dp,dp_end,cntd,cntd_mn,cntd_sum) " +
                " Select a.nzp_kvar,a.nzp_serv,a.rashod,a.dop87,a.dop87_source,a.kf307,a.kf307n,a.kf_dpu_plob,a.kod_info, " +
                " k.nzp_period,k.dp,k.dp_end,k.cntd,k.cntd_mn,d.cntd_sum " +
                " From " + rashod.counters_xx + " a,t_gku_periods k, ttt_aid_kpud d " +
                " Where a.nzp_kvar = k.nzp_kvar and a.nzp_type = 3 and a.stek = 3 and a.kod_info>0 and a." + rashod.where_dom + rashod.where_kvarA +
                "   and a.nzp_kvar = d.nzp_kvar and a.nzp_serv = d.nzp_serv "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ExecSQL(conn_db, " Create index ix_ttt_counters_stek3 on ttt_counters_stek3 (nzp_kvar,nzp_serv,dp,dp_end) ", true);

            var tempMaxCounterStek3 = "max_ttt_counters_stek3_" + DateTime.Now.Ticks;

            var sql = " CREATE TEMP TABLE " + tempMaxCounterStek3 + " AS " +
                      " SELECT t.nzp_kvar, t.nzp_serv,t.dp, t.dp_end, " +
                      " MAX(t.dop87*(t.cntd*1.00/t.cntd_sum)) as dop87," +
                      " MAX(t.kf307) as kf307, " +
                      " MAX(t.kf307n) as kf307n," +
                      " MAX(t.kf_dpu_plob) as kf_dpu_plob, " +
                      " MAX(t.kod_info) as kod_info, " +
                      " MAX(t.dop87_source*(t.cntd*1.00/t.cntd_sum)) as dop87_source" +
                      " FROM ttt_counters_stek3 t, " + rashod.counters_xx + " c " +
                      " WHERE t.nzp_kvar = c.nzp_kvar" +
                      " and t.nzp_serv = c.nzp_serv" +
                      " and t.dp     <= c.dat_po" +
                      " and t.dp_end >= c.dat_s" +
                     " and t.kod_info in (21,22,23,26,27) " +
                      " and c.nzp_type = 3 and c.stek = 20" +
                      " GROUP BY 1,2,3,4";
            ret = ExecSQL(conn_db, sql, true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ExecSQL(conn_db, " Create index ix" + tempMaxCounterStek3 + " on " + tempMaxCounterStek3 + " (nzp_kvar,nzp_serv,dp,dp_end) ", true);
            ExecSQL(conn_db, sUpdStat + " " + tempMaxCounterStek3, true);

            sql = " UPDATE " + rashod.counters_xx + " c" +
                  " SET dop87=t.dop87, " +
                  " kf307=t.kf307," +
                  " kf307n=t.kf307n, " +
                  " kf_dpu_plob=t.kf_dpu_plob," +
                  " kod_info=t.kod_info," +
                  " dop87_source=t.dop87_source" +
                  " FROM " + tempMaxCounterStek3 + " t" +
                  " WHERE t.nzp_kvar = c.nzp_kvar" +
                  " and t.nzp_serv = c.nzp_serv" +
                  " and t.dp     <= c.dat_po" +
                  " and t.dp_end >= c.dat_s" +
                     " and t.kod_info in (21,22,23,26,27) " +
                  " and c.nzp_type = 3 and c.stek = 20";
            ret = ExecSQL(conn_db, sql, true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ExecSQL(conn_db, " DROP TABLE " + tempMaxCounterStek3, false);

            var tempMinCounterStek3 = "min_ttt_counters_stek3_" + DateTime.Now.Ticks;

            sql = " CREATE TEMP TABLE " + tempMinCounterStek3 + " AS " +
                  " SELECT t.nzp_kvar, t.nzp_serv,t.dp, t.dp_end, " +
                  " MIN(t.dop87*(t.cntd*1.00/t.cntd_sum)) as dop87," +
                  " MAX(c.rashod) + MIN(t.dop87*(t.cntd*1.00/t.cntd_sum)) as rashod," +
                  " MAX(t.rashod) as rashod_stek3 ,MAX(t.dop87) as dop87_stek3 ," +
                  " MAX(t.rashod) as rashod_stek20,MAX(t.dop87) as dop87_stek20," + // заготовки для сторнирования суммарных расходов из стека 3 в стек 20
                  " MAX(t.kf307) as kf307, " +
                  " MAX(t.kf307n) as kf307n," +
                  " MAX(t.kf_dpu_plob) as kf_dpu_plob, " +
                  " MAX(t.kod_info) as kod_info, " +
                  " MIN(t.dop87_source*(t.cntd*1.00/t.cntd_sum)) as dop87_source" +
                  " FROM ttt_counters_stek3 t, " + rashod.counters_xx + " c " +
                  " WHERE t.nzp_kvar = c.nzp_kvar" +
                  " AND t.nzp_serv = c.nzp_serv" +
                  " AND t.dp     <= c.dat_po" +
                  " AND t.dp_end >= c.dat_s" +
                  " AND NOT(t.kod_info in (21,22,23,26,27)) " +
                  " AND c.nzp_type = 3 and c.stek = 20" +
                  " GROUP BY 1,2,3,4";
            ret = ExecSQL(conn_db, sql, true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ExecSQL(conn_db, " Create index ix" + tempMinCounterStek3 + " on " + tempMinCounterStek3 + " (nzp_kvar,nzp_serv,dp,dp_end) ", true);
            ExecSQL(conn_db, sUpdStat + " " + tempMinCounterStek3, true);

            ExecSQL(conn_db, " DROP TABLE s" + tempMaxCounterStek3, false);
            sql = " CREATE TEMP TABLE s" + tempMinCounterStek3 + " AS " +
                  " SELECT nzp_kvar, nzp_serv, sum(dop87) as dop87_stek20, sum(rashod) as rashod_stek20" +
                  " FROM " + tempMinCounterStek3 +
                  " GROUP BY 1,2 ";
            ret = ExecSQL(conn_db, sql, true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ExecSQL(conn_db, " Create index ixs" + tempMinCounterStek3 + " on s" + tempMinCounterStek3 + " (nzp_kvar,nzp_serv) ", true);
            ExecSQL(conn_db, sUpdStat + " s" + tempMinCounterStek3, true);

            // ... сторнировать на равенство в rashod_stek3
            ret = ExecSQL(conn_db,
                " Update " + tempMinCounterStek3 + " a " +
                " Set rashod_stek20 = b.rashod_stek20, dop87_stek20 = b.dop87_stek20 " +
                " From s" + tempMinCounterStek3 + " b " +
                " Where a.nzp_kvar=b.nzp_kvar and a.nzp_serv=b.nzp_serv "
                , true); 
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ret = ExecSQL(conn_db,
                " Update " + tempMinCounterStek3 + " a " +
                " Set " +
                " rashod = rashod * (case when abs(rashod_stek20)>0.0000001 then rashod_stek3 / rashod_stek20 else 1 end)," +
                " dop87  = dop87  * (case when abs(dop87_stek20 )>0.0000001 then dop87_stek3  / dop87_stek20  else 1 end) "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }
            // ...
            sql = " UPDATE " + rashod.counters_xx + " c" +
                  " SET dop87=t.dop87, " +
                  " kf307=t.kf307," +
                  " rashod=t.rashod," +
                  " kf307n=t.kf307n, " +
                  " kf_dpu_plob=t.kf_dpu_plob," +
                  " kod_info=t.kod_info," +
                  " dop87_source=t.dop87_source" +
                  " FROM " + tempMinCounterStek3 + " t" +
                  " WHERE t.nzp_kvar = c.nzp_kvar" +
                  " AND t.nzp_serv = c.nzp_serv" +
                  " AND t.dp     <= c.dat_po" +
                  " AND t.dp_end >= c.dat_s" +
                  " AND NOT(t.kod_info in (21,22,23,26,27)) " +
                  " AND c.nzp_type = 3 and c.stek = 20";
            ret = ExecSQL(conn_db, sql, true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ExecSQL(conn_db, " DROP TABLE " + tempMinCounterStek3, false);
         
            return true;
        }
        #endregion учет ОДН по ЛС в стек 20 из стека 3 - запись в стек = 20 & nzp_type = 3

    }

}

#endregion здесь производится подсчет расходов
